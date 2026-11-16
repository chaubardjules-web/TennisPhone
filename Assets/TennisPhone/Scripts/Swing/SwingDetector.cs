using System;

namespace TennisPhone
{
    /// <summary>
    /// Détecte les swings dans le flux de mouvements du téléphone, en deux temps.
    /// 1. Impact : le geste dépasse les seuils d'accélération ET de vitesse angulaire, puis son
    ///    pic passe. Le côté, la puissance et le lift sont connus à cet instant.
    /// 2. Accompagnement : pendant un court moment après l'impact, on regarde si le tamis
    ///    s'ouvre vers le ciel. Si oui, le coup devient un slice.
    /// Une pause empêche ensuite de compter deux fois le même geste.
    /// Classe sans dépendance à Unity, pour pouvoir la tester et rejouer des enregistrements.
    /// </summary>
    public sealed class SwingDetector
    {
        enum State
        {
            Idle,
            Rising,
            FollowThrough,
            Cooldown,
        }

        // Délai minimal après le maximum avant de considérer que le pic est passé.
        const double PeakConfirmSeconds = 0.03;

        State state = State.Idle;
        double startTime;
        double peakTime;
        double followUntil;
        double cooldownUntil;
        double lastTime = double.NegativeInfinity;
        float peakScore;
        float peakAcceleration;
        float peakRotationSpeed;
        float startFaceUp;
        float faceSign;
        float maxFaceOpening;
        double yawSum;
        double rollSum;
        int frameCount;
        int nextId = 1;
        float announcedSpin;
        SwingEvent current;

        public SwingSettings Settings;

        public SwingDetector(SwingSettings settings)
        {
            Settings = settings;
        }

        /// <summary>Vrai pendant qu'un geste est en cours d'analyse.</summary>
        public bool IsTracking => state == State.Rising || state == State.FollowThrough;

        public void Reset()
        {
            state = State.Idle;
            lastTime = double.NegativeInfinity;
        }

        /// <summary>
        /// Traite une mesure. Renvoie Impact quand un swing vient d'être reconnu, Refined quand
        /// l'effet du dernier swing vient d'être précisé (même Id), None sinon.
        /// </summary>
        public SwingUpdate Process(MotionFrame frame, out SwingEvent swing)
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
                        state = State.Rising;
                        startTime = frame.TimeSeconds;
                        startFaceUp = frame.FaceUp;
                        peakScore = 0f;
                        yawSum = 0.0;
                        rollSum = 0.0;
                        frameCount = 0;
                        Accumulate(frame);
                    }

                    return SwingUpdate.None;

                case State.Rising:
                    float score = Accumulate(frame);
                    double duration = frame.TimeSeconds - startTime;

                    // Le pic est passé : le geste est retombé sous son maximum.
                    bool peakPassed = Settings.ImpactFallRatio > 0f
                        && score <= peakScore * Settings.ImpactFallRatio
                        && frame.TimeSeconds - peakTime >= PeakConfirmSeconds;

                    if (duration >= Settings.MaxDuration
                        || ((released || peakPassed) && duration >= Settings.MinDuration))
                    {
                        current = BuildSwing((float)duration);
                        maxFaceOpening = float.NegativeInfinity;
                        UpdateSlice(frame);
                        announcedSpin = current.Spin;

                        state = State.FollowThrough;
                        followUntil = frame.TimeSeconds + Settings.FollowThroughDuration;
                        cooldownUntil = frame.TimeSeconds + Math.Max(Settings.Cooldown, Settings.FollowThroughDuration);
                        swing = current;
                        return SwingUpdate.Impact;
                    }

                    if (released)
                    {
                        // Secousse trop brève pour être un swing.
                        state = State.Idle;
                    }

                    return SwingUpdate.None;

                case State.FollowThrough:
                    UpdateSlice(frame);
                    bool finished = frame.TimeSeconds >= followUntil;
                    bool fullyOpen = maxFaceOpening >= Settings.SliceFaceOpeningFull;

                    if (finished)
                    {
                        state = State.Cooldown;
                    }

                    // Le nouvel effet est annoncé dès que le tamis est complètement ouvert,
                    // ou à la fin de l'accompagnement s'il ne s'est ouvert qu'en partie.
                    if ((fullyOpen || finished) && current.Spin != announcedSpin)
                    {
                        announcedSpin = current.Spin;
                        swing = current;
                        return SwingUpdate.Refined;
                    }

                    return SwingUpdate.None;

                default:
                    if (frame.TimeSeconds >= cooldownUntil && released)
                    {
                        state = State.Idle;
                    }

                    return SwingUpdate.None;
            }
        }

        /// <summary>Ajoute une mesure au geste en cours et renvoie son intensité.</summary>
        float Accumulate(MotionFrame frame)
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
            return score;
        }

        /// <summary>
        /// Met à jour l'ouverture du tamis et, si elle dépasse le seuil, transforme le coup en slice.
        /// </summary>
        void UpdateSlice(MotionFrame frame)
        {
            float opening = (frame.FaceUp - startFaceUp) * faceSign;
            if (opening <= maxFaceOpening)
            {
                return;
            }

            maxFaceOpening = opening;
            float slice = InverseLerp(Settings.SliceFaceOpeningMin, Settings.SliceFaceOpeningFull, opening);
            if (slice > 0f && -slice < current.Spin)
            {
                current.Spin = -slice;
            }
        }

        SwingEvent BuildSwing(float duration)
        {
            float handSign = Settings.Handedness == Handedness.Right ? 1f : -1f;

            // Pour un droitier, un coup droit balaie de la droite vers la gauche :
            // rotation dans le sens inverse des aiguilles d'une montre vue de dessus.
            bool forehand = yawSum * handSign * (Settings.InvertSide ? -1f : 1f) >= 0.0;

            // Le sens du poignet et la face du tamis qui s'ouvre s'inversent entre coup droit
            // et revers, et entre droitier et gaucher.
            float mirror = (forehand ? 1f : -1f) * handSign;
            faceSign = mirror * (Settings.InvertSlice ? -1f : 1f);

            float averageRoll = frameCount > 0 ? (float)(rollSum / frameCount) : 0f;
            float signedRoll = averageRoll * mirror * (Settings.InvertLift ? -1f : 1f);
            float lift = InverseLerp(Settings.LiftRollRateMin, Settings.LiftRollRateFull, signedRoll);

            float accelerationPower = InverseLerp(Settings.PowerAccelerationMin, Settings.PowerAccelerationMax, peakAcceleration);
            float rotationPower = InverseLerp(Settings.PowerRotationMin, Settings.PowerRotationMax, peakRotationSpeed);
            float weight = Clamp(Settings.PowerRotationWeight, 0f, 1f);

            return new SwingEvent
            {
                Id = nextId++,
                Source = SwingSource.Phone,
                Side = forehand ? SwingSide.Forehand : SwingSide.Backhand,
                Power = accelerationPower * (1f - weight) + rotationPower * weight,
                Spin = lift,
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
