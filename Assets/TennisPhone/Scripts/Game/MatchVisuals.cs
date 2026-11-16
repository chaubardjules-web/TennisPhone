using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Tout ce qui se voit du match : court, décor, lumière, personnages, raquettes, balle et
    /// caméra. Lit l'état de RallyController et s'abonne à ses événements ; ne décide de rien.
    /// </summary>
    [RequireComponent(typeof(RallyController))]
    public sealed class MatchVisuals : MonoBehaviour
    {
        // La balle est affichée plus grosse que nature pour rester lisible de loin.
        const float BallVisualScale = 2.4f;

        [Tooltip("Décor et lumière : stade de jour, ou soir des lampions.")]
        [SerializeField] Ambiance ambiance = Ambiance.Jour;

        [Tooltip("Teinte de la surface de jeu. Le rebond ne change pas : il est réglé dans GameplayConfig.")]
        [SerializeField] Surface surface = Surface.Gazon;

        [SerializeField] Light sun;

        [Tooltip("Matériaux modèles : ils gardent les shaders et leurs variantes dans un build autonome.")]
        [SerializeField] Material[] shaderTemplates;

        RallyController rally;
        GameplayConfig config;
        Camera view;
        Vector3 cameraHome;
        float cameraOffset;

        CourtVisual court;
        DecorVisual decor;
        DecorAnimator decorAnimator;

        Transform ballVisual;
        Transform ballShadow;
        Transform playerVisual;
        Transform racketVisual;
        Transform opponentVisual;
        Transform opponentRacket;
        Transform landingMarker;
        Renderer landingRenderer;

        public Ambiance Ambiance => ambiance;

        /// <summary>Réglages posés par SceneBuilder quand il crée la scène.</summary>
        public void Configure(Ambiance newAmbiance, Surface newSurface, Light sunLight, Material[] templates)
        {
            ambiance = newAmbiance;
            surface = newSurface;
            sun = sunLight;
            shaderTemplates = templates;
        }

        void Awake()
        {
            rally = GetComponent<RallyController>();
            config = GetComponent<RacketInput>().Config;
            view = Camera.main;
            if (view != null)
            {
                cameraHome = view.transform.position;
            }

            Palette.Surface = surface;
            Atmosphere.Apply(ambiance, view, sun);
            Atmosphere.ApplyShadowQuality();

            court = CourtBuilder.Build(config.ToCourt());
            decor = DecorBuilder.Build(view, ambiance);
            decorAnimator = gameObject.AddComponent<DecorAnimator>();
            decorAnimator.Init(decor);

            BuildActors();
        }

        void OnEnable()
        {
            rally.Bounced += OnBounced;
            rally.BallHit += OnBallHit;
            rally.ServePrepared += OnServePrepared;
            rally.PointEnded += OnPointEnded;
        }

        void OnDisable()
        {
            rally.Bounced -= OnBounced;
            rally.BallHit -= OnBallHit;
            rally.ServePrepared -= OnServePrepared;
            rally.PointEnded -= OnPointEnded;
        }

        void BuildActors()
        {
            float diameter = config.ballRadius * 2f * BallVisualScale;
            ballVisual = CourtBuilder.Primitive(PrimitiveType.Sphere, null, "Balle", Vector3.zero, Quaternion.identity,
                Vector3.one * diameter, Palette.JauneBalle);
            ballShadow = CourtBuilder.Primitive(PrimitiveType.Cylinder, null, "Ombre de la balle", Vector3.zero,
                Quaternion.identity, new Vector3(diameter * 1.1f, 0.001f, diameter * 1.1f), Palette.Encre);
            landingMarker = CourtBuilder.Primitive(PrimitiveType.Cylinder, null, "Marque du rebond", Vector3.zero,
                Quaternion.identity, new Vector3(0.35f, 0.002f, 0.35f), Palette.NewMat(Color.white));
            landingRenderer = landingMarker.GetComponent<Renderer>();
            landingMarker.gameObject.SetActive(false);
            ballVisual.gameObject.SetActive(false);
            ballShadow.gameObject.SetActive(false);

            playerVisual = BuildCharacter("Joueur", Palette.Framboise);
            racketVisual = RacketModel.Build(out Renderer _);
            opponentVisual = BuildCharacter("Adversaire", Palette.Lavande);
            opponentRacket = RacketModel.Build(out Renderer _);
            opponentRacket.name = "Raquette de l'adversaire";
        }

        static Transform BuildCharacter(string name, Color color)
        {
            Transform root = new GameObject(name).transform;
            CourtBuilder.Primitive(PrimitiveType.Capsule, root, "Corps", new Vector3(0f, 0.75f, 0f),
                Quaternion.identity, new Vector3(0.45f, 0.75f, 0.35f), color);
            CourtBuilder.Primitive(PrimitiveType.Sphere, root, "Tête", new Vector3(0f, 1.65f, 0f),
                Quaternion.identity, Vector3.one * 0.3f, Palette.BoisNaturel);
            return root;
        }

        // ---------------------------------------------------------------- Événements du match

        void OnBounced(BounceInfo bounce)
        {
            if (bounce.Live && bounce.First && bounce.ByPlayer)
            {
                landingMarker.position = new Vector3(bounce.Point.x, 0.007f, bounce.Point.z);
                landingRenderer.sharedMaterial.color = bounce.Inside ? Palette.MentheClaire : Palette.Rouge;
                landingMarker.gameObject.SetActive(true);
            }
        }

        void OnBallHit(HitInfo hit)
        {
            if (hit.ByPlayer)
            {
                landingMarker.gameObject.SetActive(false);
            }
        }

        void OnServePrepared()
        {
            landingMarker.gameObject.SetActive(false);
        }

        void OnPointEnded(PointInfo point)
        {
            if (point.Result == ScoreEvent.Set)
            {
                decorAnimator.Pulse();
            }
        }

        // ---------------------------------------------------------------- Chaque image

        void LateUpdate()
        {
            UpdateVisuals();
            UpdateCamera();
        }

        void UpdateVisuals()
        {
            BallState ball = rally.Ball;
            bool ballActive = rally.BallActive;
            ballVisual.gameObject.SetActive(ballActive);
            ballShadow.gameObject.SetActive(ballActive);
            if (ballActive)
            {
                ballVisual.position = ball.Position;
                ballShadow.position = new Vector3(ball.Position.x, 0.009f, ball.Position.z);
            }

            Vector3 playerPosition = rally.PlayerPosition;
            playerVisual.position = playerPosition;

            // La raquette est tenue du côté où la balle arrive, à hauteur de hanche.
            Vector3 hand = playerPosition + new Vector3(rally.HittingSide * 0.35f, 0.95f, 0.15f);
            racketVisual.SetPositionAndRotation(hand, rally.RacketRotation);

            // L'adversaire regarde vers le joueur ; sa raquette balaie brièvement quand il frappe.
            Vector3 opponentPosition = rally.OpponentPosition;
            float opponentSide = rally.OpponentHittingSide;
            opponentVisual.position = opponentPosition;
            float swingProgress = Mathf.Clamp01((Time.unscaledTime - rally.OpponentSwingTime) / 0.3f);
            float sweep = Mathf.Lerp(-70f, 70f, Mathf.SmoothStep(0f, 1f, swingProgress)) * -opponentSide;
            Vector3 opponentHand = opponentPosition + new Vector3(opponentSide * 0.35f, 0.95f, -0.15f);
            opponentRacket.SetPositionAndRotation(opponentHand,
                Quaternion.Euler(0f, 180f + (swingProgress < 1f ? sweep : 0f), 0f) * Quaternion.Euler(-55f, 0f, 0f));
        }

        /// <summary>La caméra accompagne un peu le joueur sur la largeur.</summary>
        void UpdateCamera()
        {
            if (view == null)
            {
                return;
            }

            cameraOffset = Mathf.Lerp(cameraOffset, rally.PlayerPosition.x * 0.45f, 1f - Mathf.Exp(-4f * Time.deltaTime));
            view.transform.position = cameraHome + new Vector3(cameraOffset, 0f, 0f);
        }
    }
}
