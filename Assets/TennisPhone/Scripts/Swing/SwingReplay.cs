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
        public struct Entry
        {
            public SensorSample Sample;

            /// <summary>Numéro de série du geste demandé (voir SensorRecorder).</summary>
            public int Label;

            /// <summary>Numéro du geste dans sa série.</summary>
            public int Repetition;
        }

        public struct Result
        {
            /// <summary>Le swing, avec son effet final (après l'accompagnement).</summary>
            public SwingEvent Swing;

            /// <summary>Étiquette et numéro du geste au moment de l'impact.</summary>
            public int Label;
            public int Repetition;

            /// <summary>Vrai si l'effet a été précisé après l'impact.</summary>
            public bool Refined;
        }

        /// <summary>Lit les lignes d'un fichier CSV. Les lignes illisibles sont ignorées.</summary>
        public static List<Entry> Parse(IEnumerable<string> csvLines)
        {
            var entries = new List<Entry>();
            foreach (string csvLine in csvLines)
            {
                if (string.IsNullOrWhiteSpace(csvLine) || csvLine.StartsWith("phoneMs"))
                {
                    continue;
                }

                // Colonnes : phoneMs, serverMs, puis 12 valeurs, puis l'étiquette
                // (et, dans les fichiers récents, le numéro du geste dans sa série).
                string[] parts = csvLine.Trim().Split(',');
                if ((parts.Length != 15 && parts.Length != 16)
                    || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double serverMs)
                    || !int.TryParse(parts[14], NumberStyles.Integer, CultureInfo.InvariantCulture, out int label))
                {
                    continue;
                }

                int repetition = 0;
                if (parts.Length == 16)
                {
                    int.TryParse(parts[15], NumberStyles.Integer, CultureInfo.InvariantCulture, out repetition);
                }

                string sampleLine = SensorSample.Prefix + parts[0] + "," + string.Join(",", parts, 2, 12);
                if (SensorSample.TryParse(sampleLine, serverMs, out SensorSample sample))
                {
                    entries.Add(new Entry { Sample = sample, Label = label, Repetition = repetition });
                }
            }

            return entries;
        }

        /// <summary>Passe les mesures dans un détecteur neuf et renvoie les swings trouvés.</summary>
        public static List<Result> Run(IList<Entry> entries, SwingSettings settings)
        {
            var detector = new SwingDetector(settings);
            var results = new List<Result>();
            foreach (Entry entry in entries)
            {
                SwingUpdate update = detector.Process(PhoneMotion.ToFrame(entry.Sample), out SwingEvent swing);
                if (update == SwingUpdate.Impact)
                {
                    results.Add(new Result { Swing = swing, Label = entry.Label, Repetition = entry.Repetition });
                }
                else if (update == SwingUpdate.Refined && results.Count > 0
                    && results[results.Count - 1].Swing.Id == swing.Id)
                {
                    Result last = results[results.Count - 1];
                    last.Swing = swing;
                    last.Refined = true;
                    results[results.Count - 1] = last;
                }
            }

            return results;
        }
    }
}
