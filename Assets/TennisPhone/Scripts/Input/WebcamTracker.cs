using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Lit la webcam du PC et en tire la position latérale du joueur devant l'écran.
    /// Fournit aussi un petit aperçu pour vérifier ce que la caméra détecte.
    /// Tout le traitement reste sur le PC : aucune image n'est enregistrée ni envoyée.
    /// La caméra ne tourne que tant que ce composant est activé.
    /// </summary>
    public sealed class WebcamTracker : MonoBehaviour
    {
        const int GridWidth = 64;
        const int GridHeight = 36;
        const int RequestedWidth = 320;
        const int RequestedHeight = 180;
        const int RequestedFps = 30;

        [SerializeField] GameplayConfig config;

        WebCamTexture webcam;
        BodyTracker tracker;
        Color32[] pixels;
        byte[] luma;
        Color32[] previewPixels;
        Texture2D preview;
        RacketInput input;
        float lastFrameTime;
        float frozenUntil;

        /// <summary>Vrai quand la webcam fonctionne.</summary>
        public bool IsRunning => webcam != null && webcam.isPlaying;

        /// <summary>Message à afficher si la webcam n'a pas pu démarrer.</summary>
        public string Error { get; private set; }

        public string DeviceName { get; private set; }

        /// <summary>Vrai tant que le joueur a été vu récemment.</summary>
        public bool HasTarget => tracker != null && tracker.HasTarget;

        /// <summary>
        /// Position du joueur dans la largeur de l'image, de son propre point de vue :
        /// -0,5 au bord gauche, +0,5 au bord droit (la caméra lui fait face, d'où le retournement).
        /// </summary>
        public float PlayerX { get; private set; }

        /// <summary>Vrai pendant un swing : la position est figée.</summary>
        public bool IsFrozen => Time.unscaledTime < frozenUntil;

        /// <summary>Part de l'image en mouvement (0 à 1), pour le réglage.</summary>
        public float Coverage => tracker != null ? tracker.Coverage : 0f;

        /// <summary>Aperçu : image de la caméra en miroir, mouvement détecté et position retenue.</summary>
        public Texture Preview => preview;

        public void SetConfig(GameplayConfig value)
        {
            config = value;
        }

        void Awake()
        {
            input = GetComponent<RacketInput>();
            if (config == null && input != null)
            {
                config = input.Config;
            }
        }

        void OnEnable()
        {
            StartCamera();
        }

        void OnDisable()
        {
            if (webcam != null)
            {
                webcam.Stop();
                Destroy(webcam);
                webcam = null;
            }
        }

        void StartCamera()
        {
            Error = null;
            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices == null || devices.Length == 0)
            {
                Error = "Aucune webcam détectée sur ce PC.";
                return;
            }

            DeviceName = devices[0].name;
            webcam = new WebCamTexture(DeviceName, RequestedWidth, RequestedHeight, RequestedFps);
            webcam.Play();

            tracker = new BodyTracker(GridWidth, GridHeight);
            luma = new byte[GridWidth * GridHeight];
            previewPixels = new Color32[GridWidth * GridHeight];
            preview = new Texture2D(GridWidth, GridHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            lastFrameTime = Time.unscaledTime;
        }

        void Update()
        {
            if (webcam == null || config == null)
            {
                return;
            }

            if (!webcam.isPlaying)
            {
                Error = "La webcam s'est arrêtée (utilisée par une autre application ?).";
                return;
            }

            // La taille réelle n'est connue qu'après la première image.
            if (!webcam.didUpdateThisFrame || webcam.width <= 16 || webcam.height <= 16)
            {
                return;
            }

            if (pixels == null || pixels.Length != webcam.width * webcam.height)
            {
                pixels = new Color32[webcam.width * webcam.height];
            }

            webcam.GetPixels32(pixels);
            ReduceToGrid(webcam.width, webcam.height);

            float now = Time.unscaledTime;
            float deltaTime = Mathf.Clamp(now - lastFrameTime, 0.005f, 0.25f);
            lastFrameTime = now;

            var settings = new BodyTrackerSettings
            {
                MotionThreshold = config.webcamMotionThreshold,
                MemorySeconds = config.webcamMemorySeconds,
                MinCoverage = config.webcamMinCoverage,
                MaxCoverage = 0.6f,
                SmoothingSeconds = config.webcamSmoothingSeconds,
                HoldSeconds = config.webcamHoldSeconds,
            };
            // Un swing agite le bras sans déplacer le corps : le téléphone le signale, on fige la position.
            if (input != null && input.HasPhone && input.HasFrame
                && (input.IsTrackingSwing || input.LatestFrame.RotationSpeed >= config.webcamFreezeRotation))
            {
                frozenUntil = now + config.webcamFreezeSeconds;
            }

            tracker.Process(luma, now, deltaTime, now < frozenUntil, settings);

            // La caméra fait face au joueur : sa droite est à gauche de l'image, d'où le signe moins.
            PlayerX = -tracker.X * 0.5f;

            UpdatePreview();
        }

        /// <summary>Réduit l'image de la caméra à une petite grille de luminosité.</summary>
        void ReduceToGrid(int sourceWidth, int sourceHeight)
        {
            for (int gy = 0; gy < GridHeight; gy++)
            {
                // Deux lignes échantillonnées par case, pour moyenner un peu le bruit.
                int y0 = (gy * 4 + 1) * sourceHeight / (GridHeight * 4);
                int y1 = (gy * 4 + 3) * sourceHeight / (GridHeight * 4);
                for (int gx = 0; gx < GridWidth; gx++)
                {
                    int x0 = (gx * 4 + 1) * sourceWidth / (GridWidth * 4);
                    int x1 = (gx * 4 + 3) * sourceWidth / (GridWidth * 4);
                    int sum = Luma(pixels[y0 * sourceWidth + x0]) + Luma(pixels[y0 * sourceWidth + x1])
                        + Luma(pixels[y1 * sourceWidth + x0]) + Luma(pixels[y1 * sourceWidth + x1]);
                    luma[gy * GridWidth + gx] = (byte)(sum >> 2);
                }
            }
        }

        static int Luma(Color32 color)
        {
            return (color.r * 77 + color.g * 150 + color.b * 29) >> 8;
        }

        void UpdatePreview()
        {
            float[] memory = tracker.Memory;

            // Colonne de la position retenue, dans l'image (avant retournement en miroir).
            int marker = Mathf.RoundToInt((tracker.X + 1f) * 0.5f * (GridWidth - 1));

            for (int gy = 0; gy < GridHeight; gy++)
            {
                for (int gx = 0; gx < GridWidth; gx++)
                {
                    int index = gy * GridWidth + gx;
                    byte gray = (byte)(luma[index] * 0.6f);
                    float motion = memory[index];

                    Color32 color;
                    if (tracker.HasTarget && gx == marker)
                    {
                        color = IsFrozen ? new Color32(140, 170, 255, 255) : new Color32(255, 217, 77, 255);
                    }
                    else
                    {
                        color = new Color32(
                            (byte)Mathf.Min(255f, gray + motion * 40f),
                            (byte)Mathf.Min(255f, gray + motion * 160f),
                            (byte)Mathf.Min(255f, gray + motion * 70f),
                            255);
                    }

                    // Retournement gauche-droite : l'aperçu se lit comme un miroir.
                    previewPixels[gy * GridWidth + (GridWidth - 1 - gx)] = color;
                }
            }

            preview.SetPixels32(previewPixels);
            preview.Apply(false);
        }

        void OnDestroy()
        {
            if (preview != null)
            {
                Destroy(preview);
            }
        }
    }
}
