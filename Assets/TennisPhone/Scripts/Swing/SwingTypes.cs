namespace TennisPhone
{
    public enum Handedness
    {
        Right,
        Left,
    }

    public enum SwingSide
    {
        Forehand,
        Backhand,
    }

    public enum SwingSource
    {
        Phone,
        KeyboardMouse,
    }

    /// <summary>
    /// Mouvement du téléphone à un instant, réduit aux grandeurs utiles à la détection du swing.
    /// </summary>
    public struct MotionFrame
    {
        /// <summary>Horloge de la source, en secondes.</summary>
        public double TimeSeconds;

        /// <summary>Norme de l'accélération sans gravité, en m/s².</summary>
        public float Acceleration;

        /// <summary>Norme de la vitesse angulaire, en degrés par seconde.</summary>
        public float RotationSpeed;

        /// <summary>
        /// Vitesse de rotation autour de la verticale, en degrés par seconde.
        /// Positive dans le sens inverse des aiguilles d'une montre, vu de dessus.
        /// </summary>
        public float YawRate;

        /// <summary>Vitesse de rotation autour du grand axe du téléphone (le poignet qui tourne), en degrés par seconde.</summary>
        public float RollRate;
    }

    /// <summary>
    /// Un swing détecté.
    /// </summary>
    public struct SwingEvent
    {
        public SwingSource Source;
        public SwingSide Side;

        /// <summary>Puissance entre 0 et 1.</summary>
        public float Power;

        /// <summary>Effet entre -1 (slice) et +1 (lift) ; 0 pour une frappe à plat.</summary>
        public float Spin;

        /// <summary>Instant du pic du geste, sur l'horloge de la source, en secondes.</summary>
        public double PeakTimeSeconds;

        /// <summary>Temps écoulé entre le pic du geste et sa prise en compte par le jeu, en secondes.</summary>
        public float AgeSeconds;

        public float DurationSeconds;
        public float PeakAcceleration;
        public float PeakRotationSpeed;
    }

    /// <summary>
    /// Réglages du détecteur, copiés depuis GameplayConfig.
    /// </summary>
    public struct SwingSettings
    {
        public Handedness Handedness;
        public float AccelerationThreshold;
        public float RotationThreshold;
        public float ReleaseRatio;
        public float MinDuration;
        public float MaxDuration;
        public float Cooldown;
        public float PowerAccelerationMin;
        public float PowerAccelerationMax;
        public float PowerRotationMin;
        public float PowerRotationMax;
        public float PowerRotationWeight;
        public float SpinRollRateFull;
        public float SpinDeadZone;
        public bool InvertSide;
        public bool InvertSpin;
    }
}
