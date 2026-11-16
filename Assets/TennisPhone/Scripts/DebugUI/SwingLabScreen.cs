using System.Collections.Generic;
using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Écran de l'étape b : une raquette seule qui suit le téléphone, et l'affichage de chaque
    /// swing détecté (côté, puissance, effet). Sert aussi à piloter l'enregistreur de capteurs.
    /// </summary>
    [RequireComponent(typeof(RacketInput))]
    public sealed class SwingLabScreen : MonoBehaviour
    {
        const float ReferenceHeight = 720f;
        const int MaxHistory = 8;
        const float ManualSwingDuration = 0.28f;
        const float HighlightDuration = 1.2f;

        static readonly Vector3 RacketPosition = new Vector3(0f, 1.05f, 0f);

        RacketInput input;
        PhoneLink link;
        SensorRecorder recorder;
        Transform racket;
        Renderer headRenderer;
        Color headColor;

        readonly List<SwingEvent> history = new List<SwingEvent>();
        SwingEvent lastSwing;
        bool hasSwing;
        float lastSwingTime = -10f;
        float manualSwingStart = -10f;
        float manualSwingSign = 1f;

        Texture2D controllerQr;
        string controllerQrUrl;
        GUIStyle titleStyle;
        GUIStyle textStyle;
        GUIStyle bigStyle;
        GUIStyle hugeStyle;
        GUIStyle centeredStyle;
        GUIStyle errorStyle;

        void Awake()
        {
            input = GetComponent<RacketInput>();
            link = GetComponent<PhoneLink>();
            recorder = GetComponent<SensorRecorder>();
        }

        void OnEnable()
        {
            input.Swung += OnSwing;
            input.SwingRefined += OnSwingRefined;
        }

        void OnDisable()
        {
            input.Swung -= OnSwing;
            input.SwingRefined -= OnSwingRefined;
        }

        /// <summary>La fin du geste a changé l'effet du coup : on corrige l'affichage.</summary>
        void OnSwingRefined(SwingEvent swing)
        {
            if (hasSwing && lastSwing.Id == swing.Id)
            {
                lastSwing = swing;
                lastSwingTime = Time.unscaledTime;
            }

            for (int i = 0; i < history.Count; i++)
            {
                if (history[i].Id == swing.Id)
                {
                    history[i] = swing;
                }
            }
        }

        void Start()
        {
            racket = BuildRacket();
        }

        void OnSwing(SwingEvent swing)
        {
            lastSwing = swing;
            hasSwing = true;
            lastSwingTime = Time.unscaledTime;

            history.Insert(0, swing);
            if (history.Count > MaxHistory)
            {
                history.RemoveAt(history.Count - 1);
            }

            if (swing.Source == SwingSource.KeyboardMouse)
            {
                manualSwingStart = Time.unscaledTime;
                bool rightHanded = input.Config.handedness == Handedness.Right;
                bool sweepsLeft = (swing.Side == SwingSide.Forehand) == rightHanded;
                manualSwingSign = sweepsLeft ? -1f : 1f;
            }

            if (link != null)
            {
                link.SendVibration(Mathf.RoundToInt(Mathf.Lerp(20f, 60f, swing.Power)));
            }
        }

        void Update()
        {
            Quaternion rotation = input.RacketRotation;

            // Sans téléphone, le swing au clavier ou à la souris est mimé par un balayage.
            float manualProgress = (Time.unscaledTime - manualSwingStart) / ManualSwingDuration;
            if (!input.HasPhone && manualProgress >= 0f && manualProgress < 1f)
            {
                float angle = Mathf.Lerp(-75f, 75f, Mathf.SmoothStep(0f, 1f, manualProgress)) * manualSwingSign;
                rotation = Quaternion.Euler(0f, angle, 0f) * rotation;
            }

            racket.SetPositionAndRotation(RacketPosition, rotation);

            float highlight = Mathf.Clamp01(1f - (Time.unscaledTime - lastSwingTime) / 0.35f);
            headRenderer.material.color = Color.Lerp(headColor, Color.white, highlight);
        }

        Transform BuildRacket()
        {
            var root = new GameObject("Raquette").transform;

            // Axes locaux : Z du manche vers la tête, Y normale du tamis.
            CreatePart(root, PrimitiveType.Cylinder, "Manche", new Vector3(0f, 0f, 0.1f),
                Quaternion.Euler(90f, 0f, 0f), new Vector3(0.032f, 0.1f, 0.032f), new Color(0.15f, 0.15f, 0.17f));
            CreatePart(root, PrimitiveType.Cylinder, "Cœur", new Vector3(0f, 0f, 0.26f),
                Quaternion.Euler(90f, 0f, 0f), new Vector3(0.022f, 0.07f, 0.022f), new Color(0.85f, 0.25f, 0.2f));
            CreatePart(root, PrimitiveType.Cylinder, "Cadre", new Vector3(0f, 0f, 0.47f),
                Quaternion.identity, new Vector3(0.27f, 0.011f, 0.33f), new Color(0.85f, 0.25f, 0.2f));

            headColor = new Color(0.95f, 0.92f, 0.75f);
            Transform strings = CreatePart(root, PrimitiveType.Cylinder, "Cordage", new Vector3(0f, 0f, 0.47f),
                Quaternion.identity, new Vector3(0.24f, 0.013f, 0.3f), headColor);
            headRenderer = strings.GetComponent<Renderer>();

            return root;
        }

        static Transform CreatePart(Transform parent, PrimitiveType type, string name, Vector3 localPosition,
            Quaternion localRotation, Vector3 scale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = localRotation;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().material.color = color;
            return part.transform;
        }

        void OnGUI()
        {
            EnsureStyles();

            float scale = Screen.height / ReferenceHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;

            DrawLeftPanel(new Rect(16f, 16f, 330f, ReferenceHeight - 32f));
            DrawRightPanel(new Rect(width - 346f, 16f, 330f, ReferenceHeight - 32f));
            DrawLastSwing(new Rect(width * 0.5f - 260f, ReferenceHeight - 150f, 520f, 134f));
            DrawGuidedPrompt(new Rect(width * 0.5f - 300f, 24f, 600f, 190f));
        }

        void DrawGuidedPrompt(Rect area)
        {
            if (recorder == null || string.IsNullOrEmpty(recorder.GuidedTitle))
            {
                return;
            }

            bool flash = recorder.IsGuided && !recorder.GuidedPreparing && recorder.PromptFlash;
            Color previous = GUI.color;
            if (flash)
            {
                Fill(area, new Color(0.25f, 0.75f, 0.4f, 0.9f));
            }

            GUI.Box(area, GUIContent.none);
            GUILayout.BeginArea(new Rect(area.x + 14f, area.y + 8f, area.width - 28f, area.height - 16f));
            GUILayout.Label(recorder.GuidedTitle, hugeStyle);
            GUILayout.Label(recorder.GuidedHint, centeredStyle);

            if (recorder.IsGuided)
            {
                string action = recorder.GuidedPreparing
                    ? "Début dans " + Mathf.CeilToInt(recorder.PhaseRemaining) + " s"
                    : (flash ? "MAINTENANT !" : "…");
                GUILayout.Label(recorder.GuidedProgress + "    " + action, bigStyle);
                if (!recorder.GuidedPreparing && recorder.Label == 0)
                {
                    GUILayout.Label("Encore " + Mathf.CeilToInt(recorder.PhaseRemaining) + " s", centeredStyle);
                }
            }

            GUILayout.EndArea();
            GUI.color = previous;
        }

        void DrawLeftPanel(Rect area)
        {
            GUI.Box(area, GUIContent.none);
            GUILayout.BeginArea(new Rect(area.x + 12f, area.y + 10f, area.width - 24f, area.height - 20f));
            GUILayout.Label("Raquette", titleStyle);

            if (link != null && !string.IsNullOrEmpty(link.Error))
            {
                GUILayout.Label(link.Error, errorStyle);
            }
            else if (link != null && link.ClientCount > 0)
            {
                GUILayout.Label("Téléphone connecté : " + link.SampleRateHz.ToString("F0") + " Hz, aller-retour "
                    + (link.RoundTripMs >= 0f ? link.RoundTripMs.ToString("F0") + " ms" : "—"), textStyle);
            }
            else if (link != null)
            {
                GUILayout.Label("Aucun téléphone. Scanne ce code pour ouvrir la manette, ou joue au clavier et à la souris.", textStyle);
                DrawQr(link.ControllerUrl, 150f);
            }

            GUILayout.Space(8f);
            GUILayout.Label("Clavier et souris", titleStyle);
            GUILayout.Label("Clic gauche : coup droit\nClic droit : revers\nEspace : coup droit, puissance fixe\n"
                + "Flèche haut maintenue : lift\nFlèche bas maintenue : slice\nR : recentrer la raquette", textStyle);

            if (recorder != null)
            {
                GUILayout.Space(8f);
                GUILayout.Label("Enregistrement des capteurs", titleStyle);
                GUILayout.Label("G : enregistrement guidé (environ 2 minutes). L'écran annonce chaque geste, "
                    + "il n'y a rien d'autre à toucher.\nC : enregistrement libre.", textStyle);
                GUILayout.Label(recorder.IsRecording
                    ? "EN COURS : " + recorder.RecordedSamples + " mesures"
                    : "Arrêté", recorder.IsRecording ? errorStyle : textStyle);
            }

            GUILayout.EndArea();
        }

        void DrawRightPanel(Rect area)
        {
            GUI.Box(area, GUIContent.none);
            GUILayout.BeginArea(new Rect(area.x + 12f, area.y + 10f, area.width - 24f, area.height - 20f));
            GUILayout.Label("Mouvement en direct", titleStyle);

            GameplayConfig config = input.Config;
            if (input.HasPhone && input.HasFrame)
            {
                MotionFrame frame = input.LatestFrame;
                DrawMeter("Accélération (m/s²)", frame.Acceleration, config.powerAccelerationMax,
                    config.swingAccelerationThreshold, new Color(0.35f, 0.8f, 0.45f));
                DrawMeter("Vitesse angulaire (°/s)", frame.RotationSpeed, config.powerRotationMax,
                    config.swingRotationThreshold, new Color(0.95f, 0.65f, 0.25f));
                DrawSignedMeter("Rotation autour de la verticale (°/s)", frame.YawRate, config.powerRotationMax,
                    new Color(0.4f, 0.6f, 0.95f));
                DrawSignedMeter("Rotation du poignet (°/s)", frame.RollRate, config.liftRollRateFull,
                    new Color(0.8f, 0.45f, 0.9f));
                DrawSignedMeter("Écran vers le sol ← → vers le ciel", frame.FaceUp, 1f,
                    new Color(0.95f, 0.85f, 0.35f));
                GUILayout.Label(input.IsTrackingSwing ? "Geste en cours d'analyse…" : " ", textStyle);
            }
            else
            {
                GUILayout.Label("Vitesse de la souris : " + input.MouseSpeed.ToString("F0") + " px/s", textStyle);
                DrawMeter("Puissance au clic", Mathf.Clamp01(input.MouseSpeed / config.mouseSpeedForFullPower), 1f,
                    config.mouseMinPower, new Color(0.35f, 0.8f, 0.45f));
            }

            GUILayout.Space(8f);
            GUILayout.Label("Derniers swings", titleStyle);
            if (history.Count == 0)
            {
                GUILayout.Label("Aucun swing pour l'instant.", textStyle);
            }

            foreach (SwingEvent swing in history)
            {
                GUILayout.Label(Describe(swing), textStyle);
            }

            GUILayout.EndArea();
        }

        void DrawLastSwing(Rect area)
        {
            if (!hasSwing)
            {
                return;
            }

            float age = Time.unscaledTime - lastSwingTime;
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0.45f, Mathf.Clamp01(age / HighlightDuration)));

            GUI.Box(area, GUIContent.none);
            GUILayout.BeginArea(new Rect(area.x + 14f, area.y + 8f, area.width - 28f, area.height - 16f));
            GUILayout.Label(SideName(lastSwing.Side) + " " + SpinName(lastSwing.Spin), bigStyle);
            DrawMeter("Puissance : " + Mathf.RoundToInt(lastSwing.Power * 100f) + " %", lastSwing.Power, 1f, -1f,
                new Color(0.35f, 0.8f, 0.45f));
            DrawSignedMeter("Effet : slice ← → lift", lastSwing.Spin, 1f, new Color(0.8f, 0.45f, 0.9f));
            GUILayout.EndArea();

            GUI.color = previous;
        }

        static string Describe(SwingEvent swing)
        {
            string text = SideName(swing.Side) + " " + SpinName(swing.Spin) + "  "
                + Mathf.RoundToInt(swing.Power * 100f) + " %";
            if (swing.Source == SwingSource.Phone)
            {
                text += "  (" + swing.PeakAcceleration.ToString("F0") + " m/s², "
                    + swing.PeakRotationSpeed.ToString("F0") + " °/s, vu après "
                    + Mathf.RoundToInt(swing.AgeSeconds * 1000f) + " ms)";
            }
            else
            {
                text += "  (clavier/souris)";
            }

            return text;
        }

        static string SideName(SwingSide side)
        {
            return side == SwingSide.Forehand ? "Coup droit" : "Revers";
        }

        static string SpinName(float spin)
        {
            if (spin > 0f)
            {
                return "lifté";
            }

            return spin < 0f ? "slicé" : "à plat";
        }

        void DrawMeter(string label, float value, float fullScale, float threshold, Color color)
        {
            GUILayout.Label(label + (fullScale > 1.5f ? " : " + value.ToString("F0") : string.Empty), textStyle);
            Rect track = GUILayoutUtility.GetRect(10f, 12f, GUILayout.ExpandWidth(true));
            Fill(track, new Color(1f, 1f, 1f, 0.12f));
            Fill(new Rect(track.x, track.y, track.width * Mathf.Clamp01(value / fullScale), track.height), color);
            if (threshold >= 0f)
            {
                float x = track.x + track.width * Mathf.Clamp01(threshold / fullScale);
                Fill(new Rect(x - 1f, track.y - 2f, 2f, track.height + 4f), Color.white);
            }
        }

        void DrawSignedMeter(string label, float value, float fullScale, Color color)
        {
            GUILayout.Label(label + (fullScale > 1.5f ? " : " + value.ToString("F0") : string.Empty), textStyle);
            Rect track = GUILayoutUtility.GetRect(10f, 12f, GUILayout.ExpandWidth(true));
            Fill(track, new Color(1f, 1f, 1f, 0.12f));

            float half = track.width * 0.5f;
            float extent = Mathf.Clamp(value / fullScale, -1f, 1f) * half;
            Fill(extent >= 0f
                ? new Rect(track.x + half, track.y, extent, track.height)
                : new Rect(track.x + half + extent, track.y, -extent, track.height), color);
            Fill(new Rect(track.x + half - 0.5f, track.y - 2f, 1f, track.height + 4f), Color.white);
        }

        void DrawQr(string url, float size)
        {
            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            if (controllerQr == null || controllerQrUrl != url)
            {
                if (controllerQr != null)
                {
                    Destroy(controllerQr);
                }

                controllerQr = SensorDebugScreen.BuildQrTexture(url);
                controllerQrUrl = url;
            }

            Rect area = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
            GUI.DrawTexture(area, controllerQr, ScaleMode.ScaleToFit);
        }

        static void Fill(Rect area, Color color)
        {
            Color previous = GUI.color;
            GUI.color = new Color(color.r, color.g, color.b, color.a * previous.a);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold };
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            bigStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            hugeStyle = new GUIStyle(GUI.skin.label) { fontSize = 44, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            centeredStyle = new GUIStyle(textStyle) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            errorStyle = new GUIStyle(textStyle);
            errorStyle.normal.textColor = new Color(1f, 0.55f, 0.45f);
        }

        void OnDestroy()
        {
            if (controllerQr != null)
            {
                Destroy(controllerQr);
            }
        }
    }
}
