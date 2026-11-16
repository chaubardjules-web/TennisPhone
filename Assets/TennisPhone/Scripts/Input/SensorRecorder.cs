using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Enregistre les mesures brutes du téléphone dans un fichier CSV, pour régler la détection
    /// du swing sur de vrais gestes. Touche C : démarrer ou arrêter. Touches 0 à 5 : étiquette
    /// du geste en cours. Les fichiers vont dans le dossier Recordings, à la racine du projet.
    /// </summary>
    [RequireComponent(typeof(PhoneLink))]
    public sealed class SensorRecorder : MonoBehaviour
    {
        public static readonly string[] Labels =
        {
            "0 repos et déplacements",
            "1 coup droit",
            "2 revers",
            "3 coup droit lifté",
            "4 coup droit slicé",
            "5 service (lancer puis frappe)",
        };

        public const string Header = "phoneMs,serverMs,ax,ay,az,gx,gy,gz,ra,rb,rg,oa,ob,og,label";

        PhoneLink link;
        StreamWriter writer;
        readonly StringBuilder line = new StringBuilder(160);

        public bool IsRecording => writer != null;

        public int Label { get; private set; }

        public int RecordedSamples { get; private set; }

        public string FilePath { get; private set; }

        void Awake()
        {
            link = GetComponent<PhoneLink>();
        }

        void OnEnable()
        {
            link.SampleReceived += OnSample;
        }

        void OnDisable()
        {
            link.SampleReceived -= OnSample;
            StopRecording();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                if (IsRecording)
                {
                    StopRecording();
                }
                else
                {
                    StartRecording();
                }
            }

            for (int i = 0; i < Labels.Length; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
                {
                    Label = i;
                }
            }

            // Alternative qui ne dépend pas de la disposition du clavier.
            if (Input.GetKeyDown(KeyCode.PageUp))
            {
                Label = (Label + 1) % Labels.Length;
            }
            else if (Input.GetKeyDown(KeyCode.PageDown))
            {
                Label = (Label + Labels.Length - 1) % Labels.Length;
            }
        }

        void StartRecording()
        {
            try
            {
                string directory = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Recordings");
                Directory.CreateDirectory(directory);
                FilePath = Path.Combine(directory, "capteurs-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv");
                writer = new StreamWriter(FilePath, false, new UTF8Encoding(false));
                writer.WriteLine(Header);
                RecordedSamples = 0;
                Debug.Log("[TennisPhone] Enregistrement démarré : " + FilePath);
            }
            catch (Exception exception)
            {
                writer = null;
                Debug.LogError("[TennisPhone] Enregistrement impossible : " + exception.Message);
            }
        }

        void StopRecording()
        {
            if (writer == null)
            {
                return;
            }

            writer.Dispose();
            writer = null;
            Debug.Log("[TennisPhone] Enregistrement terminé : " + RecordedSamples + " mesures dans " + FilePath);
        }

        void OnSample(SensorSample sample)
        {
            if (writer == null)
            {
                return;
            }

            CultureInfo invariant = CultureInfo.InvariantCulture;
            line.Length = 0;
            line.Append(sample.PhoneTimeMs.ToString("F1", invariant)).Append(',');
            line.Append(sample.ServerTimeMs.ToString("F1", invariant)).Append(',');
            AppendVector(sample.Acceleration, invariant);
            AppendVector(sample.AccelerationWithGravity, invariant);
            AppendVector(sample.RotationRate, invariant);
            AppendVector(sample.Orientation, invariant);
            line.Append(Label.ToString(invariant));
            writer.WriteLine(line.ToString());
            RecordedSamples++;
        }

        void AppendVector(Vector3 value, CultureInfo culture)
        {
            line.Append(value.x.ToString("F3", culture)).Append(',');
            line.Append(value.y.ToString("F3", culture)).Append(',');
            line.Append(value.z.ToString("F3", culture)).Append(',');
        }
    }
}
