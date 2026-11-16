namespace TennisPhone
{
    /// <summary>
    /// Détecte le geste de lancer du service : le téléphone, tenu bas, est levé jusqu'à pointer
    /// vers le haut. Ce geste est lent, donc invisible pour le détecteur de swing ; on regarde
    /// l'inclinaison du téléphone plutôt que sa vitesse.
    /// Classe sans dépendance à Unity.
    /// </summary>
    public sealed class TossDetector
    {
        bool armed;

        /// <summary>Inclinaison (degrés) sous laquelle le téléphone doit repasser avant un nouveau lancer.</summary>
        public float ArmElevation = 30f;

        /// <summary>Inclinaison (degrés) à partir de laquelle le lancer est déclenché.</summary>
        public float RaiseElevation = 55f;

        /// <summary>Vrai quand le téléphone est assez bas pour qu'un lever déclenche le lancer.</summary>
        public bool IsArmed => armed;

        public void Reset()
        {
            armed = false;
        }

        /// <summary>Traite une mesure. Renvoie true à l'instant où le téléphone est levé.</summary>
        /// <param name="topElevation">Inclinaison du haut du téléphone, en degrés (90 : pointé vers le ciel).</param>
        public bool Process(float topElevation)
        {
            if (topElevation <= ArmElevation)
            {
                armed = true;
                return false;
            }

            if (armed && topElevation >= RaiseElevation)
            {
                armed = false;
                return true;
            }

            return false;
        }
    }
}
