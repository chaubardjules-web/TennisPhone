using System;
using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Entrée « raquette » du jeu. Réunit deux sources, actives en même temps :
    /// - le téléphone, dont les capteurs passent par le détecteur de swing ;
    /// - le clavier et la souris, pour tester sans téléphone
    ///   (clic gauche : coup droit, clic droit : revers, espace : coup droit de puissance fixe,
    ///   flèche haut maintenue : lift, flèche bas maintenue : slice).
    /// </summary>
    public sealed class RacketInput : MonoBehaviour
    {
        const float MouseSpeedSmoothing = 0.12f;

        [SerializeField] GameplayConfig config;

        PhoneLink link;
        SwingDetector detector;
        readonly TossDetector tossDetector = new TossDetector();
        Quaternion yawOffset = Quaternion.identity;
        Vector3 lastMousePosition;
        float mouseSpeed;

        // Numéros négatifs pour ne jamais croiser ceux du détecteur.
        int nextManualId = -1;

        /// <summary>Déclenché sur le thread principal à chaque swing, quelle que soit la source.</summary>
        public event Action<SwingEvent> Swung;

        /// <summary>
        /// Déclenché quand le joueur fait le geste de lancer du service (téléphone levé, ou flèche haut).
        /// La valeur est la vigueur du geste, de 0 (lever lent : lancer bas) à 1 (lever vif : lancer haut).
        /// </summary>
        public event Action<float> Tossed;

        /// <summary>
        /// Déclenché peu après Swung quand la fin du geste change l'effet du coup (slice).
        /// L'événement porte le même Id que le swing d'origine.
        /// </summary>
        public event Action<SwingEvent> SwingRefined;

        public GameplayConfig Config => config;

        /// <summary>Vrai quand un téléphone envoie des mesures.</summary>
        public bool HasPhone => link != null && link.HasSample;

        public bool HasFrame { get; private set; }

        /// <summary>Dernier mouvement du téléphone.</summary>
        public MotionFrame LatestFrame { get; private set; }

        public bool IsTrackingSwing => detector != null && detector.IsTracking;

        /// <summary>Vrai quand le téléphone est tenu assez bas pour qu'un lever déclenche le lancer.</summary>
        public bool IsTossArmed => tossDetector.IsArmed;

        /// <summary>Vitesse du dernier lever du téléphone pour lancer la balle, en degrés par seconde.</summary>
        public float LastTossRate { get; private set; }

        /// <summary>Vitesse lissée de la souris, en pixels par seconde.</summary>
        public float MouseSpeed => mouseSpeed;

        /// <summary>
        /// Orientation de la raquette : celle du téléphone s'il est connecté, sinon une pose de repos.
        /// L'axe local Z est le manche vers la tête, l'axe local Y la normale du tamis.
        /// </summary>
        public Quaternion RacketRotation
        {
            get
            {
                if (HasPhone)
                {
                    return yawOffset * DeviceOrientationMath.ToUnityRotation(link.Latest.Orientation);
                }

                return Quaternion.Euler(-55f, 0f, 0f);
            }
        }

        public void SetConfig(GameplayConfig value)
        {
            config = value;
        }

        void Awake()
        {
            link = GetComponent<PhoneLink>();
            if (config == null)
            {
                Debug.LogError("[TennisPhone] RacketInput n'a pas de GameplayConfig.");
                enabled = false;
                return;
            }

            detector = new SwingDetector(config.ToSwingSettings());
        }

        void OnEnable()
        {
            if (link != null)
            {
                link.SampleReceived += OnSample;
            }

            lastMousePosition = Input.mousePosition;
        }

        void OnDisable()
        {
            if (link != null)
            {
                link.SampleReceived -= OnSample;
            }
        }

        void OnSample(SensorSample sample)
        {
            // Relu à chaque mesure : un réglage modifié dans l'inspecteur s'applique tout de suite.
            detector.Settings = config.ToSwingSettings();

            MotionFrame frame = PhoneMotion.ToFrame(sample);
            LatestFrame = frame;
            HasFrame = true;

            tossDetector.ArmElevation = config.tossArmElevation;
            tossDetector.RaiseElevation = config.tossRaiseElevation;
            if (tossDetector.Process(frame.TopElevation, frame.TimeSeconds))
            {
                LastTossRate = tossDetector.RaiseRate;
                Tossed?.Invoke(Mathf.InverseLerp(config.tossRateMin, config.tossRateMax, tossDetector.RaiseRate));
            }

            SwingUpdate update = detector.Process(frame, out SwingEvent swing);
            if (update == SwingUpdate.None)
            {
                return;
            }

            // Âge du pic : via la synchronisation d'horloge si elle est disponible,
            // sinon d'après l'horloge du téléphone seule (sans le délai réseau).
            if (link.RoundTripMs >= 0f)
            {
                double peakServerMs = swing.PeakTimeSeconds * 1000.0 + link.ClockOffsetMs;
                swing.AgeSeconds = Mathf.Max(0f, (float)((link.ServerNowMs - peakServerMs) / 1000.0));
            }
            else
            {
                swing.AgeSeconds = Mathf.Max(0f, (float)(frame.TimeSeconds - swing.PeakTimeSeconds));
            }

            if (update == SwingUpdate.Impact)
            {
                Swung?.Invoke(swing);
            }
            else
            {
                SwingRefined?.Invoke(swing);
            }
        }

        void Update()
        {
            UpdateMouseSpeed();

            if (Input.GetKeyDown(KeyCode.R))
            {
                Recenter();
            }

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                // Au clavier, la flèche haut lance la balle de service, à hauteur moyenne.
                Tossed?.Invoke(config.tossDefaultStrength);
            }

            if (Input.GetMouseButtonDown(0))
            {
                RaiseManualSwing(SwingSide.Forehand, MousePower());
            }
            else if (Input.GetMouseButtonDown(1))
            {
                RaiseManualSwing(SwingSide.Backhand, MousePower());
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                RaiseManualSwing(SwingSide.Forehand, config.keyboardPower);
            }
        }

        /// <summary>
        /// Recentre la raquette : la direction où pointe le téléphone devient le fond du court
        /// (touche R, ou bouton « Recentrer » du téléphone).
        /// </summary>
        public void Recenter()
        {
            if (!HasPhone)
            {
                return;
            }

            float yaw = DeviceOrientationMath.ToUnityRotation(link.Latest.Orientation).eulerAngles.y;
            yawOffset = Quaternion.Euler(0f, -yaw, 0f);
        }

        void UpdateMouseSpeed()
        {
            Vector3 position = Input.mousePosition;
            float deltaTime = Time.unscaledDeltaTime;
            if (deltaTime > 0f)
            {
                float instantSpeed = (position - lastMousePosition).magnitude / deltaTime;
                float blend = 1f - Mathf.Exp(-deltaTime / MouseSpeedSmoothing);
                mouseSpeed = Mathf.Lerp(mouseSpeed, instantSpeed, blend);
            }

            lastMousePosition = position;
        }

        float MousePower()
        {
            float fromSpeed = Mathf.Clamp01(mouseSpeed / config.mouseSpeedForFullPower);
            return Mathf.Max(config.mouseMinPower, fromSpeed);
        }

        void RaiseManualSwing(SwingSide side, float power)
        {
            float spin = 0f;
            if (Input.GetKey(KeyCode.UpArrow))
            {
                spin = config.keyboardSpin;
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                spin = -config.keyboardSpin;
            }

            Swung?.Invoke(new SwingEvent
            {
                Id = nextManualId--,
                Source = SwingSource.KeyboardMouse,
                Side = side,
                Power = Mathf.Clamp01(power),
                Spin = spin,
                PeakTimeSeconds = Time.unscaledTimeAsDouble,
                AgeSeconds = 0f,
            });
        }
    }
}
