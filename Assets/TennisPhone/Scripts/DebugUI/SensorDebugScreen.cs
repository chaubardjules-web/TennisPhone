using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Écran de debug de l'étape a : état de la connexion, QR codes, valeurs des capteurs
    /// en direct, et un modèle de téléphone qui suit l'orientation du vrai.
    /// Touches : R recentre le modèle, V envoie une vibration de test.
    /// </summary>
    [RequireComponent(typeof(PhoneLink))]
    public sealed class SensorDebugScreen : MonoBehaviour
    {
        const int HistoryLength = 240;
        const float ReferenceHeight = 720f;
        const int QrQuietZone = 4;

        [Tooltip("Pleine échelle du graphe d'accélération, en m/s².")]
        [SerializeField] float accelerationScale = 40f;

        [Tooltip("Pleine échelle du graphe de vitesse angulaire, en degrés par seconde.")]
        [SerializeField] float rotationScale = 1000f;

        PhoneLink link;
        Transform phoneModel;
        Quaternion yawOffset = Quaternion.identity;

        readonly float[] accelerationHistory = new float[HistoryLength];
        readonly float[] rotationHistory = new float[HistoryLength];
        int historyIndex;

        Texture2D controllerQr;
        Texture2D onboardingQr;
        string controllerQrUrl;
        string onboardingQrUrl;
        GUIStyle titleStyle;
        GUIStyle textStyle;
        GUIStyle errorStyle;

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
        }

        void Start()
        {
            phoneModel = BuildPhoneModel();
        }

        void OnSample(SensorSample sample)
        {
            accelerationHistory[historyIndex] = sample.Acceleration.magnitude;
            rotationHistory[historyIndex] = sample.RotationRate.magnitude;
            historyIndex = (historyIndex + 1) % HistoryLength;
        }

        void Update()
        {
            if (link.HasSample)
            {
                Quaternion raw = DeviceOrientationMath.ToUnityRotation(link.Latest.Orientation);
                if (Input.GetKeyDown(KeyCode.R))
                {
                    // Annule le cap actuel : le haut du téléphone pointe vers le fond de la scène.
                    float yaw = raw.eulerAngles.y;
                    yawOffset = Quaternion.Euler(0f, -yaw, 0f);
                }

                phoneModel.rotation = yawOffset * raw;
            }

            if (Input.GetKeyDown(KeyCode.V))
            {
                link.SendVibration(40);
            }
        }

        static Transform BuildPhoneModel()
        {
            var root = new GameObject("Modèle du téléphone").transform;

            // Axes locaux : X = droite du téléphone, Y = normale de l'écran, Z = haut du téléphone.
            Transform body = CreateBox(root, "Corps", new Vector3(0f, 0f, 0f),
                new Vector3(0.075f, 0.009f, 0.15f), new Color(0.13f, 0.14f, 0.17f));
            CreateBox(body.parent, "Écran", new Vector3(0f, 0.0048f, 0f),
                new Vector3(0.066f, 0.001f, 0.138f), new Color(0.2f, 0.55f, 0.95f));
            CreateBox(body.parent, "Repère du haut", new Vector3(0f, 0.0055f, 0.062f),
                new Vector3(0.03f, 0.002f, 0.008f), new Color(1f, 0.8f, 0.2f));

            return root;
        }

        static Transform CreateBox(Transform parent, string name, Vector3 localPosition, Vector3 scale, Color color)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            Destroy(box.GetComponent<Collider>());
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = scale;

            Renderer boxRenderer = box.GetComponent<Renderer>();
            boxRenderer.material.color = color;
            return box.transform;
        }

        void OnGUI()
        {
            EnsureStyles();

            float scale = Screen.height / ReferenceHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;

            DrawConnectionPanel(new Rect(16f, 16f, 400f, ReferenceHeight - 32f));
            DrawSensorPanel(new Rect(width - 416f, 16f, 400f, ReferenceHeight - 32f));
        }

        void DrawConnectionPanel(Rect area)
        {
            GUI.Box(area, GUIContent.none);
            GUILayout.BeginArea(new Rect(area.x + 12f, area.y + 10f, area.width - 24f, area.height - 20f));

            GUILayout.Label("Connexion du téléphone", titleStyle);

            if (!string.IsNullOrEmpty(link.Error))
            {
                GUILayout.Label(link.Error, errorStyle);
                GUILayout.EndArea();
                return;
            }

            string state = link.ClientCount > 0
                ? "Téléphone connecté (" + link.ClientCount + ")"
                : (link.IsServerRunning ? "En attente du téléphone…" : "Serveur arrêté");
            GUILayout.Label("État : " + state, textStyle);
            GUILayout.Label("Fréquence : " + link.SampleRateHz.ToString("F0") + " Hz    Aller-retour : "
                + (link.RoundTripMs >= 0f ? link.RoundTripMs.ToString("F0") + " ms" : "—"), textStyle);

            if (link.ClientCount == 0 && link.TlsFailures > 0)
            {
                GUILayout.Label("Connexions HTTPS refusées par le téléphone : " + link.TlsFailures
                    + ". Le certificat n'est pas encore approuvé : fais d'abord l'étape 1.", errorStyle);
            }

            GUILayout.Space(8f);
            GUILayout.Label("1. Première fois : installer le certificat", textStyle);
            GUILayout.Label(link.OnboardingUrl, textStyle);
            DrawQr(ref onboardingQr, ref onboardingQrUrl, link.OnboardingUrl, 190f);

            GUILayout.Space(8f);
            GUILayout.Label("2. Ouvrir la manette", textStyle);
            GUILayout.Label(link.ControllerUrl, textStyle);
            DrawQr(ref controllerQr, ref controllerQrUrl, link.ControllerUrl, 190f);

            GUILayout.FlexibleSpace();
            if (!string.IsNullOrEmpty(link.LastMessage))
            {
                GUILayout.Label(link.LastMessage, textStyle);
            }

            GUILayout.Label("R : recentrer le modèle    V : vibration de test", textStyle);
            GUILayout.EndArea();
        }

        void DrawSensorPanel(Rect area)
        {
            GUI.Box(area, GUIContent.none);
            GUILayout.BeginArea(new Rect(area.x + 12f, area.y + 10f, area.width - 24f, area.height - 20f));

            GUILayout.Label("Capteurs en direct", titleStyle);

            if (!link.HasSample)
            {
                GUILayout.Label("Aucune mesure reçue.", textStyle);
                GUILayout.EndArea();
                return;
            }

            SensorSample sample = link.Latest;
            DrawVector("Accélération sans gravité (m/s²)", sample.Acceleration, accelerationScale);
            DrawVector("Accélération avec gravité (m/s²)", sample.AccelerationWithGravity, accelerationScale);
            DrawVector("Vitesse angulaire (°/s) : alpha, beta, gamma", sample.RotationRate, rotationScale);
            DrawVector("Orientation (°) : alpha, beta, gamma", sample.Orientation, 360f);

            GUILayout.Space(6f);
            GUILayout.Label("Norme de l'accélération : " + sample.Acceleration.magnitude.ToString("F1") + " m/s²", textStyle);
            DrawHistory(accelerationHistory, accelerationScale, new Color(0.35f, 0.8f, 0.45f));

            GUILayout.Label("Norme de la vitesse angulaire : " + sample.RotationRate.magnitude.ToString("F0") + " °/s", textStyle);
            DrawHistory(rotationHistory, rotationScale, new Color(0.95f, 0.65f, 0.25f));

            GUILayout.Label("Mesures reçues : " + link.TotalSamples, textStyle);
            GUILayout.EndArea();
        }

        void DrawVector(string label, Vector3 value, float fullScale)
        {
            GUILayout.Label(label, textStyle);
            DrawAxisBar("x", value.x, fullScale, new Color(0.9f, 0.35f, 0.35f));
            DrawAxisBar("y", value.y, fullScale, new Color(0.4f, 0.8f, 0.4f));
            DrawAxisBar("z", value.z, fullScale, new Color(0.4f, 0.6f, 0.95f));
        }

        void DrawAxisBar(string axis, float value, float fullScale, Color color)
        {
            Rect row = GUILayoutUtility.GetRect(10f, 16f, GUILayout.ExpandWidth(true));
            GUI.Label(new Rect(row.x, row.y - 2f, 16f, 18f), axis, textStyle);

            var track = new Rect(row.x + 18f, row.y + 3f, row.width - 96f, 10f);
            Fill(track, new Color(1f, 1f, 1f, 0.12f));

            float half = track.width * 0.5f;
            float extent = Mathf.Clamp(value / fullScale, -1f, 1f) * half;
            var bar = extent >= 0f
                ? new Rect(track.x + half, track.y, extent, track.height)
                : new Rect(track.x + half + extent, track.y, -extent, track.height);
            Fill(bar, color);

            GUI.Label(new Rect(track.xMax + 6f, row.y - 2f, 72f, 18f), value.ToString("F2"), textStyle);
        }

        void DrawHistory(float[] history, float fullScale, Color color)
        {
            Rect area = GUILayoutUtility.GetRect(10f, 70f, GUILayout.ExpandWidth(true));
            Fill(area, new Color(1f, 1f, 1f, 0.08f));

            float columnWidth = area.width / HistoryLength;
            for (int i = 0; i < HistoryLength; i++)
            {
                float value = history[(historyIndex + i) % HistoryLength];
                float height = Mathf.Clamp01(value / fullScale) * area.height;
                if (height < 0.5f)
                {
                    continue;
                }

                Fill(new Rect(area.x + i * columnWidth, area.yMax - height, columnWidth + 0.5f, height), color);
            }
        }

        void DrawQr(ref Texture2D texture, ref string textureUrl, string url, float size)
        {
            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            if (texture == null || textureUrl != url)
            {
                if (texture != null)
                {
                    Destroy(texture);
                }

                texture = BuildQrTexture(url);
                textureUrl = url;
            }

            Rect area = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
            GUI.DrawTexture(area, texture, ScaleMode.ScaleToFit);
        }

        /// <summary>Texture d'un QR code, sombre sur blanc, avec sa zone de silence.</summary>
        public static Texture2D BuildQrTexture(string text)
        {
            bool[,] modules = QrCode.Encode(text);
            int size = modules.GetLength(0);
            int full = size + 2 * QrQuietZone;

            var texture = new Texture2D(full, full, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color32[full * full];
            var white = new Color32(255, 255, 255, 255);
            var black = new Color32(0, 0, 0, 255);
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = white;
            }

            for (int row = 0; row < size; row++)
            {
                for (int column = 0; column < size; column++)
                {
                    if (modules[row, column])
                    {
                        // La ligne 0 du QR code est en haut ; la ligne 0 d'une texture est en bas.
                        int y = full - 1 - (row + QrQuietZone);
                        pixels[y * full + column + QrQuietZone] = black;
                    }
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        static void Fill(Rect area, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            errorStyle = new GUIStyle(textStyle);
            errorStyle.normal.textColor = new Color(1f, 0.55f, 0.45f);
        }

        void OnDestroy()
        {
            if (controllerQr != null)
            {
                Destroy(controllerQr);
            }

            if (onboardingQr != null)
            {
                Destroy(onboardingQr);
            }
        }
    }
}
