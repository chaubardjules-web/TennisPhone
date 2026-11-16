using System.Globalization;
using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Une mesure des capteurs du téléphone, telle qu'envoyée par la page manette.
    /// Les axes sont ceux du téléphone tenu en portrait : x vers la droite, y vers le haut
    /// de l'écran, z sortant de l'écran.
    /// </summary>
    public struct SensorSample
    {
        public const string Prefix = "m,";
        const int FieldCount = 14;

        /// <summary>Horloge du téléphone (performance.now), en millisecondes.</summary>
        public double PhoneTimeMs;

        /// <summary>Horloge du serveur à la réception, en millisecondes.</summary>
        public double ServerTimeMs;

        /// <summary>Accélération sans la gravité, en m/s².</summary>
        public Vector3 Acceleration;

        /// <summary>Accélération avec la gravité, en m/s².</summary>
        public Vector3 AccelerationWithGravity;

        /// <summary>Vitesse angulaire en degrés par seconde : x = alpha (axe z), y = beta (axe x), z = gamma (axe y).</summary>
        public Vector3 RotationRate;

        /// <summary>Orientation en degrés : x = alpha, y = beta, z = gamma.</summary>
        public Vector3 Orientation;

        /// <summary>
        /// Lit une ligne « m,t,ax,ay,az,gx,gy,gz,ra,rb,rg,oa,ob,og ».
        /// </summary>
        public static bool TryParse(string line, double serverTimeMs, out SensorSample sample)
        {
            sample = default;
            if (string.IsNullOrEmpty(line) || !line.StartsWith(Prefix))
            {
                return false;
            }

            string[] parts = line.Split(',');
            if (parts.Length != FieldCount)
            {
                return false;
            }

            double[] values = new double[FieldCount - 1];
            for (int i = 0; i < values.Length; i++)
            {
                if (!double.TryParse(parts[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out values[i])
                    || double.IsNaN(values[i]) || double.IsInfinity(values[i]))
                {
                    return false;
                }
            }

            sample.PhoneTimeMs = values[0];
            sample.ServerTimeMs = serverTimeMs;
            sample.Acceleration = new Vector3((float)values[1], (float)values[2], (float)values[3]);
            sample.AccelerationWithGravity = new Vector3((float)values[4], (float)values[5], (float)values[6]);
            sample.RotationRate = new Vector3((float)values[7], (float)values[8], (float)values[9]);
            sample.Orientation = new Vector3((float)values[10], (float)values[11], (float)values[12]);
            return true;
        }
    }
}
