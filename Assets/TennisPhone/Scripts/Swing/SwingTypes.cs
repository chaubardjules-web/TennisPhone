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

    /// <summary>Ce que le détecteur vient de produire pour une mesure.</summary>
    public enum SwingUpdate
    {
        None,

        /// <summary>Un swing vient d'être reconnu : c'est le moment de frapper la balle.</summary>
        Impact,

        /// <summary>L'effet du dernier swing est précisé par la fin du geste (slice).</summary>
        Refined,
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

        /// <summary>
        /// Orientation de l'écran par rapport au ciel : 1 quand l'écran regarde vers le haut,
        /// -1 vers le bas, 0 quand il est vertical.
        /// </summary>
        public float FaceUp;

        /// <summary>
        /// Inclinaison du haut du téléphone, en degrés : 90 quand il pointe vers le ciel,
        /// 0 à l'horizontale, -90 vers le sol.
        /// </summary>
        public float TopElevation;
    }

    /// <summary>
    /// Un swing détecté.
    /// </summary>
    public struct SwingEvent
    {
        /// <summary>Numéro du swing, repris quand son effet est précisé.</summary>
        public int Id;

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
        public float ImpactFallRatio;
        public float MinDuration;
        public float MaxDuration;
        public float FollowThroughDuration;
        public float Cooldown;
        public float PowerAccelerationMin;
        public float PowerAccelerationMax;
        public float PowerRotationMin;
        public float PowerRotationMax;
        public float PowerRotationWeight;
        public float LiftRollRateMin;
        public float LiftRollRateFull;
        public float SliceFaceOpeningMin;
        public float SliceFaceOpeningFull;
        public bool InvertSide;
        public bool InvertLift;
        public bool InvertSlice;
    }
}
