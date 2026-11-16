using System.Collections.Generic;
using System.Globalization;

namespace TennisPhone
{
    /// <summary>
    /// Rejoue un enregistrement de capteurs (format de SensorRecorder) dans le détecteur de swing.
    /// Sert à régler les seuils et à tester la détection sur de vrais gestes.
    /// </summary>
    public static class SwingReplay
    {
        public struct Result
        {
            public SwingEvent Swing;

            /// <summary>Étiquette du geste au moment du pic (voir SensorRecorder.Labels).</summary>
            public int Label;
        }

        /// <summary>Lit les lignes d'un fichier CSV. Les lignes illisibles sont ignorées.</summary>
        public static List<KeyValuePair<SensorSample, int>> Parse(IEnumerable<string> csvLines)
        {
            var samples = new List<KeyValuePair<SensorSample, int>>();
            foreach (string csvLine in csvLines)
            {
                if (string.IsNullOrWhiteSpace(csvLine) || csvLine.StartsWith("phoneMs"))
                {
                    continue;
                }

                // Colonnes : phoneMs, serverMs, puis 12 valeurs, puis l'étiquette.
                string[] parts = csvLine.Trim().Split(',');
                if (parts.Length != 15
                    || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double serverMs)
                    || !int.TryParse(parts[14], NumberStyles.Integer, CultureInfo.InvariantCulture, out int label))
                {
                    continue;
                }

                string sampleLine = SensorSample.Prefix + parts[0] + "," + string.Join(",", parts, 2, 12);
                if (SensorSample.TryParse(sampleLine, serverMs, out SensorSample sample))
                {
                    samples.Add(new KeyValuePair<SensorSample, int>(sample, label));
                }
            }

            return samples;
        }

        /// <summary>Passe les mesures dans un détecteur neuf et renvoie les swings trouvés.</summary>
        public static List<Result> Run(IList<KeyValuePair<SensorSample, int>> samples, SwingSettings settings)
        {
            var detector = new SwingDetector(settings);
            var results = new List<Result>();
            foreach (KeyValuePair<SensorSample, int> entry in samples)
            {
                if (detector.Process(PhoneMotion.ToFrame(entry.Key), out SwingEvent swing))
                {
                    results.Add(new Result { Swing = swing, Label = entry.Value });
                }
            }

            return results;
        }
    }
}
