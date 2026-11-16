using System;

namespace TennisPhone
{
    /// <summary>
    /// Détecte les swings dans le flux de mouvements du téléphone.
    /// Un swing commence quand l'accélération ET la vitesse angulaire dépassent leurs seuils,
    /// et se termine quand le geste retombe ou dure trop longtemps. Une pause empêche ensuite
    /// de compter deux fois le même geste.
    /// Classe sans dépendance à Unity, pour pouvoir la tester et rejouer des enregistrements.
    /// </summary>
    public sealed class SwingDetector
    {
        enum State
        {
            Idle,
            Tracking,
            Cooldown,
        }

        State state = State.Idle;
        double startTime;
        double peakTime;
        double cooldownUntil;
        double lastTime = double.NegativeInfinity;
        float peakScore;
        float peakAcceleration;
        float peakRotationSpeed;
        double yawSum;
        double rollSum;
        int frameCount;

        public SwingSettings Settings;

        public SwingDetector(SwingSettings settings)
        {
            Settings = settings;
        }

        /// <summary>Vrai pendant qu'un geste est en cours d'analyse.</summary>
        public bool IsTracking => state == State.Tracking;

        public void Reset()
        {
            state = State.Idle;
            lastTime = double.NegativeInfinity;
        }

        /// <summary>
        /// Traite une mesure. Renvoie true quand un swing vient de se terminer.
        /// </summary>
        public bool Process(MotionFrame frame, out SwingEvent swing)
        {
            swing = default;

            // Horloge qui recule (reconnexion du téléphone) : on repart de zéro.
            if (frame.TimeSeconds < lastTime - 1.0)
            {
                Reset();
            }

            lastTime = frame.TimeSeconds;

            bool above = frame.Acceleration >= Settings.AccelerationThreshold
                && frame.RotationSpeed >= Settings.RotationThreshold;
            bool released = frame.Acceleration < Settings.AccelerationThreshold * Settings.ReleaseRatio
                || frame.RotationSpeed < Settings.RotationThreshold * Settings.ReleaseRatio;

            switch (state)
            {
                case State.Idle:
                    if (above)
                    {
                        state = State.Tracking;
                        startTime = frame.TimeSeconds;
                        peakScore = 0f;
                        yawSum = 0.0;
                        rollSum = 0.0;
                        frameCount = 0;
                        Accumulate(frame);
                    }

                    return false;

                case State.Tracking:
                    Accumulate(frame);
                    double duration = frame.TimeSeconds - startTime;
                    if (duration >= Settings.MaxDuration || (released && duration >= Settings.MinDuration))
                    {
                        swing = BuildSwing((float)duration);
                        state = State.Cooldown;
                        cooldownUntil = frame.TimeSeconds + Settings.Cooldown;
                        return true;
                    }

                    if (released)
                    {
                        // Secousse trop brève pour être un swing.
                        state = State.Idle;
                    }

                    return false;

                default:
                    if (frame.TimeSeconds >= cooldownUntil && released)
                    {
                        state = State.Idle;
                    }

                    return false;
            }
        }

        void Accumulate(MotionFrame frame)
        {
            float score = frame.Acceleration / Math.Max(Settings.AccelerationThreshold, 0.001f)
                + frame.RotationSpeed / Math.Max(Settings.RotationThreshold, 0.001f);
            if (score > peakScore)
            {
                peakScore = score;
                peakTime = frame.TimeSeconds;
            }

            peakAcceleration = frameCount == 0 ? frame.Acceleration : Math.Max(peakAcceleration, frame.Acceleration);
            peakRotationSpeed = frameCount == 0 ? frame.RotationSpeed : Math.Max(peakRotationSpeed, frame.RotationSpeed);
            yawSum += frame.YawRate;
            rollSum += frame.RollRate;
            frameCount++;
        }

        SwingEvent BuildSwing(float duration)
        {
            float handSign = Settings.Handedness == Handedness.Right ? 1f : -1f;
            if (Settings.InvertSide)
            {
                handSign = -handSign;
            }

            // Pour un droitier, un coup droit balaie de la droite vers la gauche :
            // rotation dans le sens inverse des aiguilles d'une montre vue de dessus.
            bool forehand = yawSum * handSign >= 0.0;

            float averageRoll = frameCount > 0 ? (float)(rollSum / frameCount) : 0f;
            float spin = averageRoll / Math.Max(Settings.SpinRollRateFull, 0.001f);

            // Le sens du poignet qui « couvre » la balle s'inverse entre coup droit et revers.
            spin *= forehand ? 1f : -1f;
            spin *= Settings.Handedness == Handedness.Right ? 1f : -1f;
            if (Settings.InvertSpin)
            {
                spin = -spin;
            }

            spin = Clamp(spin, -1f, 1f);
            if (Math.Abs(spin) < Settings.SpinDeadZone)
            {
                spin = 0f;
            }

            float accelerationPower = InverseLerp(Settings.PowerAccelerationMin, Settings.PowerAccelerationMax, peakAcceleration);
            float rotationPower = InverseLerp(Settings.PowerRotationMin, Settings.PowerRotationMax, peakRotationSpeed);
            float weight = Clamp(Settings.PowerRotationWeight, 0f, 1f);

            return new SwingEvent
            {
                Source = SwingSource.Phone,
                Side = forehand ? SwingSide.Forehand : SwingSide.Backhand,
                Power = accelerationPower * (1f - weight) + rotationPower * weight,
                Spin = spin,
                PeakTimeSeconds = peakTime,
                DurationSeconds = duration,
                PeakAcceleration = peakAcceleration,
                PeakRotationSpeed = peakRotationSpeed,
            };
        }

        static float InverseLerp(float min, float max, float value)
        {
            if (max <= min)
            {
                return value >= max ? 1f : 0f;
            }

            return Clamp((value - min) / (max - min), 0f, 1f);
        }

        static float Clamp(float value, float min, float max)
        {
            return value < min ? min : (value > max ? max : value);
        }
    }
}
