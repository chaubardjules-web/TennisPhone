using UnityEngine;

namespace TennisPhone
{
    /// <summary>Une frappe qui a touché la balle.</summary>
    public struct HitInfo
    {
        /// <summary>Vrai pour une frappe du joueur, faux pour l'adversaire.</summary>
        public bool ByPlayer;

        public bool IsServe;

        /// <summary>Puissance de la frappe, de 0 à 1.</summary>
        public float Power;

        /// <summary>Effet, de -1 (slice) à +1 (lift) ; 0 pour une frappe à plat.</summary>
        public float Spin;

        /// <summary>Vitesse de la balle au départ, en m/s.</summary>
        public float Speed;

        /// <summary>Position de la balle au moment de la frappe.</summary>
        public Vector3 Point;

        /// <summary>Vrai si le swing du joueur est tombé à moins de 25 ms de l'instant idéal.</summary>
        public bool Perfect;
    }

    /// <summary>Un rebond de la balle au sol.</summary>
    public struct BounceInfo
    {
        public Vector3 Point;

        /// <summary>Vitesse verticale de la balle juste avant le rebond, en m/s.</summary>
        public float ImpactSpeed;

        /// <summary>Faux quand le point est déjà joué : la balle roule encore, sans conséquence.</summary>
        public bool Live;

        /// <summary>Vrai pour le premier rebond après une frappe : celui qui décide si la balle est bonne.</summary>
        public bool First;

        /// <summary>Vrai si ce premier rebond est dans les limites.</summary>
        public bool Inside;

        public bool IsServe;

        /// <summary>Vrai si la balle venait du joueur.</summary>
        public bool ByPlayer;
    }

    /// <summary>Pourquoi un point s'est terminé.</summary>
    public enum PointReason
    {
        Other,
        Out,
        Net,
        DoubleFault,
        DoubleBounce,
    }

    /// <summary>La fin d'un point.</summary>
    public struct PointInfo
    {
        public bool WonByPlayer;

        /// <summary>Ce que le point conclut : un point, un jeu ou le set.</summary>
        public ScoreEvent Result;

        public PointReason Reason;
    }

    /// <summary>Ton d'une annonce : il choisit sa couleur.</summary>
    public enum MessageTone
    {
        /// <summary>Information : panneau sombre.</summary>
        Neutral,

        /// <summary>Point pour le joueur.</summary>
        Good,

        /// <summary>Faute, balle dehors, filet.</summary>
        Bad,

        /// <summary>Jeu ou set.</summary>
        Accent,

        /// <summary>Avertissement : geste à refaire.</summary>
        Warning,
    }

    /// <summary>Une annonce à afficher balle morte. Plusieurs annonces se suivent, sans se superposer.</summary>
    public struct Announcement
    {
        public string Title;
        public string Subtitle;
        public MessageTone Tone;

        /// <summary>Durée d'affichage, en secondes.</summary>
        public float Duration;

        /// <summary>Camp nommé par le sous-titre : 0 pour le joueur, 1 pour l'adversaire, -1 sinon.</summary>
        public int Side;

        /// <summary>Vrai si l'annonce reste affichée jusqu'à la suivante (fin du set).</summary>
        public bool Persistent;
    }
}
