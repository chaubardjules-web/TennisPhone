using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Enregistre les mesures brutes du téléphone dans un fichier CSV, pour régler la détection
    /// du swing sur de vrais gestes. Les fichiers vont dans le dossier Recordings, à la racine
    /// du projet (non versionné).
    /// Touche G : enregistrement guidé, où l'écran annonce chaque geste à faire et l'étiquette
    /// tout seul. Touche C : enregistrement libre, étiqueté 0.
    /// </summary>
    [RequireComponent(typeof(PhoneLink))]
    public sealed class SensorRecorder : MonoBehaviour
    {
        /// <summary>Étiquette des pauses entre deux séries de l'enregistrement guidé.</summary>
        public const int TransitionLabel = 9;

        public const string Header = "phoneMs,serverMs,ax,ay,az,gx,gy,gz,ra,rb,rg,oa,ob,og,label,rep";

        const float PrepareSeconds = 5f;
        const float PromptFlashSeconds = 0.6f;

        struct GuidedStep
        {
            public int Label;
            public string Title;
            public string Hint;
            public int Repetitions;
            public float Interval;
        }

        static readonly GuidedStep[] GuidedSteps =
        {
            new GuidedStep { Label = 1, Title = "COUP DROIT", Hint = "Un coup droit normal à chaque signal.", Repetitions = 5, Interval = 2.5f },
            new GuidedStep { Label = 2, Title = "REVERS", Hint = "Un revers normal à chaque signal.", Repetitions = 5, Interval = 2.5f },
            new GuidedStep { Label = 3, Title = "COUP DROIT LIFTÉ", Hint = "Coup droit en tournant le poignet par-dessus la balle.", Repetitions = 5, Interval = 2.5f },
            new GuidedStep { Label = 4, Title = "COUP DROIT SLICÉ", Hint = "Coup droit en coupant sous la balle.", Repetitions = 5, Interval = 2.5f },
            new GuidedStep { Label = 5, Title = "SERVICE", Hint = "Un geste vers le haut pour lancer, puis une frappe.", Repetitions = 5, Interval = 4f },
            new GuidedStep { Label = 0, Title = "SANS FRAPPER", Hint = "Marche, replace-toi, bouge le téléphone normalement, sans swing.", Repetitions = 1, Interval = 15f },
        };

        PhoneLink link;
        StreamWriter writer;
        Coroutine guidedRoutine;
        readonly StringBuilder line = new StringBuilder(160);
        float promptTime = -10f;
        float phaseEnd;

        public bool IsRecording => writer != null;

        public bool IsGuided => guidedRoutine != null;

        /// <summary>Étiquette écrite avec chaque mesure.</summary>
        public int Label { get; private set; }

        /// <summary>Numéro du geste dans la série en cours (0 hors série).</summary>
        public int Repetition { get; private set; }

        public int RecordedSamples { get; private set; }

        public string FilePath { get; private set; }

        /// <summary>Titre du geste demandé pendant l'enregistrement guidé.</summary>
        public string GuidedTitle { get; private set; }

        public string GuidedHint { get; private set; }

        /// <summary>Texte d'avancement, par exemple « Geste 3 sur 5 ».</summary>
        public string GuidedProgress { get; private set; }

        /// <summary>Vrai pendant la pause de préparation avant une série.</summary>
        public bool GuidedPreparing { get; private set; }

        /// <summary>Vrai juste après un signal de frappe.</summary>
        public bool PromptFlash => Time.unscaledTime - promptTime < PromptFlashSeconds;

        /// <summary>Secondes restantes dans la phase en cours.</summary>
        public float PhaseRemaining => Mathf.Max(0f, phaseEnd - Time.unscaledTime);

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
            StopGuided();
            StopRecording();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                if (IsGuided)
                {
                    StopGuided();
                    StopRecording();
                }
                else if (!IsRecording)
                {
                    guidedRoutine = StartCoroutine(RunGuided());
                }
            }
            else if (Input.GetKeyDown(KeyCode.C) && !IsGuided)
            {
                if (IsRecording)
                {
                    StopRecording();
                }
                else
                {
                    Label = 0;
                    Repetition = 0;
                    StartRecording("libre");
                }
            }
        }

        IEnumerator RunGuided()
        {
            if (!StartRecording("guide"))
            {
                guidedRoutine = null;
                yield break;
            }

            for (int stepIndex = 0; stepIndex < GuidedSteps.Length; stepIndex++)
            {
                GuidedStep step = GuidedSteps[stepIndex];
                GuidedTitle = step.Title;
                GuidedHint = step.Hint;

                // Pause de préparation : le temps de lire la consigne.
                Label = TransitionLabel;
                Repetition = 0;
                GuidedPreparing = true;
                GuidedProgress = "Série " + (stepIndex + 1) + " sur " + GuidedSteps.Length + " : prépare-toi";
                phaseEnd = Time.unscaledTime + PrepareSeconds;
                yield return new WaitForSecondsRealtime(PrepareSeconds);
                GuidedPreparing = false;

                for (int repetition = 1; repetition <= step.Repetitions; repetition++)
                {
                    Label = step.Label;
                    Repetition = repetition;
                    GuidedProgress = step.Repetitions > 1
                        ? "Geste " + repetition + " sur " + step.Repetitions
                        : "Continue jusqu'à la fin du décompte";
                    promptTime = Time.unscaledTime;
                    phaseEnd = Time.unscaledTime + step.Interval;

                    // Petit signal sur le téléphone à chaque geste demandé.
                    link.SendVibration(25);
                    yield return new WaitForSecondsRealtime(step.Interval);
                }
            }

            GuidedTitle = "TERMINÉ";
            GuidedHint = "Merci ! L'enregistrement est sauvegardé.";
            GuidedProgress = string.Empty;
            StopRecording();
            guidedRoutine = null;
        }

        void StopGuided()
        {
            if (guidedRoutine != null)
            {
                StopCoroutine(guidedRoutine);
                guidedRoutine = null;
                GuidedTitle = "ARRÊTÉ";
                GuidedHint = "Enregistrement guidé interrompu.";
                GuidedProgress = string.Empty;
                GuidedPreparing = false;
            }
        }

        bool StartRecording(string kind)
        {
            try
            {
                string directory = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Recordings");
                Directory.CreateDirectory(directory);
                FilePath = Path.Combine(directory,
                    "capteurs-" + kind + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv");
                writer = new StreamWriter(FilePath, false, new UTF8Encoding(false));
                writer.WriteLine(Header);
                RecordedSamples = 0;
                Debug.Log("[TennisPhone] Enregistrement démarré : " + FilePath);
                return true;
            }
            catch (Exception exception)
            {
                writer = null;
                Debug.LogError("[TennisPhone] Enregistrement impossible : " + exception.Message);
                return false;
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
            line.Append(Label.ToString(invariant)).Append(',');
            line.Append(Repetition.ToString(invariant));
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
