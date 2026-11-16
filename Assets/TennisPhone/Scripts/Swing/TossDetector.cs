namespace TennisPhone
{
    /// <summary>
    /// Détecte le geste de lancer du service : le téléphone, tenu bas, est levé jusqu'à pointer
    /// vers le haut. Ce geste est lent, donc invisible pour le détecteur de swing ; on regarde
    /// l'inclinaison du téléphone plutôt que son accélération.
    /// Mesure aussi la vitesse du lever, qui règle la hauteur du lancer.
    /// Classe sans dépendance à Unity.
    /// </summary>
    public sealed class TossDetector
    {
        const int HistorySize = 48;

        readonly double[] times = new double[HistorySize];
        readonly float[] elevations = new float[HistorySize];
        int count;
        int next;
        bool armed;

        /// <summary>Inclinaison (degrés) sous laquelle le téléphone doit repasser avant un nouveau lancer.</summary>
        public float ArmElevation = 30f;

        /// <summary>Inclinaison (degrés) à partir de laquelle le lancer est déclenché.</summary>
        public float RaiseElevation = 55f;

        /// <summary>Durée (s) sur laquelle la vitesse du lever est mesurée, juste avant le déclenchement.</summary>
        public float RateWindow = 0.15f;

        /// <summary>Vrai quand le téléphone est assez bas pour qu'un lever déclenche le lancer.</summary>
        public bool IsArmed => armed;

        /// <summary>Vitesse du dernier lever, en degrés par seconde (0 si elle n'a pas pu être mesurée).</summary>
        public float RaiseRate { get; private set; }

        public void Reset()
        {
            armed = false;
            count = 0;
            next = 0;
            RaiseRate = 0f;
        }

        /// <summary>Traite une mesure sans horodatage : la vitesse du lever n'est pas mesurée.</summary>
        public bool Process(float topElevation)
        {
            return Process(topElevation, double.NaN);
        }

        /// <summary>Traite une mesure. Renvoie true à l'instant où le téléphone est levé.</summary>
        /// <param name="topElevation">Inclinaison du haut du téléphone, en degrés (90 : pointé vers le ciel).</param>
        /// <param name="timeSeconds">Horloge du téléphone, en secondes.</param>
        public bool Process(float topElevation, double timeSeconds)
        {
            bool timed = !double.IsNaN(timeSeconds);
            if (timed)
            {
                times[next] = timeSeconds;
                elevations[next] = topElevation;
                next = (next + 1) % HistorySize;
                if (count < HistorySize)
                {
                    count++;
                }
            }

            if (topElevation <= ArmElevation)
            {
                armed = true;
                return false;
            }

            if (armed && topElevation >= RaiseElevation)
            {
                armed = false;
                RaiseRate = timed ? MeasureRate(topElevation, timeSeconds) : 0f;
                return true;
            }

            return false;
        }

        /// <summary>Vitesse de montée entre la mesure la plus ancienne de la fenêtre et la mesure actuelle.</summary>
        float MeasureRate(float elevation, double time)
        {
            double oldestTime = time;
            float oldestElevation = elevation;

            // Parcours du plus récent au plus ancien, tant qu'on reste dans la fenêtre.
            for (int i = 1; i < count; i++)
            {
                int index = (next - 1 - i + 2 * HistorySize) % HistorySize;
                double age = time - times[index];
                if (age < 0.0 || age > RateWindow)
                {
                    break;
                }

                oldestTime = times[index];
                oldestElevation = elevations[index];
            }

            double duration = time - oldestTime;
            if (duration < 0.02)
            {
                return 0f;
            }

            float rate = (float)((elevation - oldestElevation) / duration);
            return rate > 0f ? rate : 0f;
        }
    }
}
