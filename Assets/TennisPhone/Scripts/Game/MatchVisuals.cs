using UnityEngine;
using UnityEngine.Rendering;

namespace TennisPhone
{
    /// <summary>
    /// Tout ce qui se voit du match : court, décor, lumière, personnages, raquettes, balle, ombre,
    /// traînée, marques de rebond et caméra. Lit l'état de RallyController et s'abonne à ses
    /// événements ; ne décide de rien.
    /// </summary>
    [RequireComponent(typeof(RallyController))]
    public sealed class MatchVisuals : MonoBehaviour
    {
        // La balle est affichée plus grosse que nature pour rester lisible de loin.
        const float BallVisualScale = 2.4f;

        // La balle tourne à un cinquième de sa vitesse réelle : sa bande reste lisible.
        const float BallSpinScale = 0.2f;
        const float DeadBallFade = 0.2f;

        const float ShadowHeight = 0.009f;
        const float ShadowFullHeight = 4f;
        const float TrailMinSpeed = 12f;
        const float MarkDuration = 0.6f;
        const float CrossDuration = 1.2f;
        const float WhiffTrailDuration = 0.3f;

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
        HitEffects effects;

        Transform ballRoot;
        Transform ballVisual;
        Material ballMaterial;
        TrailRenderer ballTrail;
        Transform ballShadow;
        Material shadowMaterial;
        float ballDiameter;
        float deadBlend;
        bool ballWasActive;

        Transform bounceMark;
        float bounceMarkAge = float.MaxValue;
        Transform faultCross;
        float faultCrossAge = float.MaxValue;

        CharacterModel player;
        CharacterModel opponent;
        Transform racketVisual;
        Transform opponentRacket;
        Renderer playerStrings;
        Renderer opponentStrings;
        TrailRenderer racketTrail;
        float whiffAge = float.MaxValue;
        Vector3 lastPlayerPosition;
        Vector3 lastOpponentPosition;
        bool hasLastPositions;

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
            effects = new HitEffects(view, court);

            BuildBall();
            BuildMarks();
            BuildPlayers();
        }

        void OnEnable()
        {
            rally.BallHit += OnBallHit;
            rally.ShotRefined += OnShotRefined;
            rally.Bounced += OnBounced;
            rally.NetTouched += OnNetTouched;
            rally.Whiffed += OnWhiffed;
            rally.TossStarted += OnTossStarted;
            rally.ServePrepared += OnServePrepared;
            rally.PointEnded += OnPointEnded;
        }

        void OnDisable()
        {
            rally.BallHit -= OnBallHit;
            rally.ShotRefined -= OnShotRefined;
            rally.Bounced -= OnBounced;
            rally.NetTouched -= OnNetTouched;
            rally.Whiffed -= OnWhiffed;
            rally.TossStarted -= OnTossStarted;
            rally.ServePrepared -= OnServePrepared;
            rally.PointEnded -= OnPointEnded;
        }

        // ---------------------------------------------------------------- Construction

        void BuildBall()
        {
            ballDiameter = config.ballRadius * 2f * BallVisualScale;
            ballRoot = new GameObject("Balle").transform;

            // Un peu lumineuse, pour rester vive à l'ombre. Aucune ombre portée réelle : seule
            // l'ombre ronde, toujours à sa verticale, dit où elle se trouve au-dessus du sol.
            ballMaterial = Palette.NewLumineux(Palette.JauneBalle, Palette.EmissionBalle);
            ballVisual = CourtBuilder.Primitive(PrimitiveType.Sphere, ballRoot, "Sphère", Vector3.zero, Quaternion.identity,
                Vector3.one * ballDiameter, ballMaterial);
            ballVisual.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            // La bande blanche montre la rotation de la balle : on lit le lift et le slice.
            Transform band = CourtBuilder.Primitive(PrimitiveType.Cylinder, ballVisual, "Bande", Vector3.zero,
                Quaternion.Euler(0f, 0f, 25f), new Vector3(1.01f, 0.03f, 1.01f), Palette.Blanc);
            band.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            ballTrail = ballRoot.gameObject.AddComponent<TrailRenderer>();
            SetupTrail(ballTrail, 0.18f, 0.14f);
            SetTrailColor(ballTrail, Palette.TraineePlat, 0.7f);

            shadowMaterial = Palette.NewVoile(Palette.Ombre);
            ballShadow = CourtBuilder.Primitive(PrimitiveType.Cylinder, null, "Ombre de la balle", Vector3.zero,
                Quaternion.identity, new Vector3(ballDiameter * 1.1f, 0.001f, ballDiameter * 1.1f), shadowMaterial);
            Palette.AsVeil(ballShadow.GetComponent<Renderer>(), shadowMaterial);

            ballRoot.gameObject.SetActive(false);
            ballShadow.gameObject.SetActive(false);
        }

        /// <summary>Marques au sol : un rond pour un rebond dans le court, une croix pour une faute.</summary>
        void BuildMarks()
        {
            Material mint = Palette.Voile(Palette.MentheClaire);
            bounceMark = CourtBuilder.Primitive(PrimitiveType.Cylinder, null, "Marque du rebond", Vector3.zero,
                Quaternion.identity, new Vector3(0.35f, 0.001f, 0.35f), mint);
            Palette.AsVeil(bounceMark.GetComponent<Renderer>(), mint);
            bounceMark.gameObject.SetActive(false);

            faultCross = new GameObject("Croix de faute").transform;
            foreach (float angle in new[] { -45f, 45f })
            {
                Transform bar = CourtBuilder.Primitive(PrimitiveType.Cube, faultCross, "Barre", Vector3.zero,
                    Quaternion.Euler(0f, angle, 0f), new Vector3(0.5f, 0.004f, 0.09f), Palette.Creme);
                bar.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }

            faultCross.gameObject.SetActive(false);
        }

        void BuildPlayers()
        {
            player = CharacterModel.BuildPlayer(config.handedness);
            racketVisual = RacketModel.Build(Palette.Framboise, out playerStrings);
            racketVisual.name = "Raquette du joueur";
            opponent = CharacterModel.BuildOpponent();
            opponentRacket = RacketModel.Build(Palette.Prune, out opponentStrings);
            opponentRacket.name = "Raquette de l'adversaire";

            // Traînée grise de la raquette, visible seulement après un swing dans le vide.
            var head = new GameObject("Traînée du tamis").transform;
            head.SetParent(racketVisual, false);
            head.localPosition = new Vector3(0f, 0f, RacketModel.HeadDistance);
            racketTrail = head.gameObject.AddComponent<TrailRenderer>();
            SetupTrail(racketTrail, 0.15f, 0.12f);
            SetTrailColor(racketTrail, Palette.GrisClair, 0.6f);
        }

        static void SetupTrail(TrailRenderer trail, float time, float width)
        {
            trail.sharedMaterial = Palette.Voile(Color.white);
            trail.time = time;
            trail.startWidth = width;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.05f;
            trail.numCapVertices = 2;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.emitting = false;
        }

        static void SetTrailColor(TrailRenderer trail, Color color, float opacity)
        {
            trail.startColor = Palette.WithAlpha(color, opacity);
            trail.endColor = Palette.WithAlpha(color, 0f);
        }

        // ---------------------------------------------------------------- Événements du match

        void OnBallHit(HitInfo hit)
        {
            Color color = Palette.TrailFor(hit.Spin);
            SetTrailColor(ballTrail, color, 0.7f);
            faultCross.gameObject.SetActive(false);

            effects.FlashStrings(hit.ByPlayer ? playerStrings : opponentStrings);
            effects.Impact(hit.Point, color, hit.ByPlayer ? 1f : 0.6f);
            if (hit.ByPlayer)
            {
                effects.Recoil(hit.Power);
                if (hit.Perfect)
                {
                    effects.Sparks(hit.Point);
                }
            }
        }

        void OnShotRefined(float spin)
        {
            SetTrailColor(ballTrail, Palette.TrailFor(spin), 0.7f);
        }

        void OnBounced(BounceInfo bounce)
        {
            if (!bounce.Live || !bounce.First)
            {
                return;
            }

            // Un rond pour une balle bonne, une croix pour une faute : pas seulement une couleur.
            if (bounce.Inside)
            {
                bounceMark.position = new Vector3(bounce.Point.x, 0.007f, bounce.Point.z);
                bounceMark.gameObject.SetActive(true);
                bounceMarkAge = 0f;
            }
            else
            {
                faultCross.position = new Vector3(bounce.Point.x, 0.008f, bounce.Point.z);
                faultCross.gameObject.SetActive(true);
                faultCrossAge = 0f;
                effects.BlinkNearestLine(bounce.Point);
            }
        }

        void OnNetTouched(Vector3 position)
        {
            effects.PushNet(position);
        }

        void OnWhiffed(string detail)
        {
            whiffAge = 0f;
        }

        void OnTossStarted(bool byPlayer)
        {
            deadBlend = 0f;
            ApplyBallColor();
            ballTrail.Clear();
        }

        void OnServePrepared()
        {
            bounceMark.gameObject.SetActive(false);
            faultCross.gameObject.SetActive(false);
            ballTrail.Clear();
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
            float deltaTime = Mathf.Max(Time.deltaTime, 1e-4f);
            UpdateBall(deltaTime);
            UpdateMarks(deltaTime);
            UpdatePlayers(deltaTime);
            effects.Update(deltaTime);
            UpdateCamera(deltaTime);
        }

        void UpdateBall(float deltaTime)
        {
            BallState ball = rally.Ball;
            bool active = rally.BallActive;
            ballRoot.gameObject.SetActive(active);
            ballShadow.gameObject.SetActive(active);
            if (!active)
            {
                ballWasActive = false;
                return;
            }

            ballRoot.position = ball.Position;
            if (!ballWasActive)
            {
                // Nouvelle balle : pas de trait entre l'ancienne position et la nouvelle.
                ballWasActive = true;
                ballTrail.Clear();
            }

            float spinRate = ball.Spin.magnitude;
            if (spinRate > 0.5f)
            {
                ballVisual.Rotate(ball.Spin / spinRate, spinRate * Mathf.Rad2Deg * BallSpinScale * deltaTime, Space.World);
            }

            bool dead = rally.BallDead;
            ballTrail.emitting = !dead && ball.Velocity.magnitude > TrailMinSpeed;

            // Balle morte : elle ternit et s'éteint.
            float targetBlend = dead ? 1f : 0f;
            if (!Mathf.Approximately(deadBlend, targetBlend))
            {
                deadBlend = Mathf.MoveTowards(deadBlend, targetBlend, deltaTime / DeadBallFade);
                ApplyBallColor();
            }

            // Ombre ronde à la verticale : plus large et plus pâle quand la balle est haute.
            float height = Mathf.Clamp01(ball.Position.y / ShadowFullHeight);
            float size = ballDiameter * Mathf.Lerp(1.1f, 1.6f, height);
            ballShadow.position = new Vector3(ball.Position.x, ShadowHeight, ball.Position.z);
            ballShadow.localScale = new Vector3(size, 0.001f, size);
            shadowMaterial.color = Palette.WithAlpha(Palette.Ombre, Mathf.Lerp(0.55f, 0.25f, height));
        }

        void ApplyBallColor()
        {
            ballMaterial.color = Color.Lerp(Palette.JauneBalle, Palette.BalleMorte, deadBlend);
            ballMaterial.SetColor("_EmissionColor", Color.Lerp(Palette.EmissionBalle, Color.black, deadBlend));
        }

        void UpdateMarks(float deltaTime)
        {
            if (bounceMarkAge < MarkDuration)
            {
                // Le rond rétrécit jusqu'à disparaître.
                bounceMarkAge += deltaTime;
                float size = 0.35f * (1f - Mathf.Clamp01(bounceMarkAge / MarkDuration));
                bounceMark.localScale = new Vector3(size, 0.001f, size);
                if (bounceMarkAge >= MarkDuration)
                {
                    bounceMark.gameObject.SetActive(false);
                }
            }

            if (faultCrossAge < CrossDuration)
            {
                faultCrossAge += deltaTime;
                if (faultCrossAge >= CrossDuration)
                {
                    faultCross.gameObject.SetActive(false);
                }
            }
        }

        void UpdatePlayers(float deltaTime)
        {
            Vector3 playerPosition = rally.PlayerPosition;
            Vector3 opponentPosition = rally.OpponentPosition;
            if (!hasLastPositions)
            {
                lastPlayerPosition = playerPosition;
                lastOpponentPosition = opponentPosition;
                hasLastPositions = true;
            }

            player.Move(playerPosition, (playerPosition - lastPlayerPosition) / deltaTime, deltaTime);
            opponent.Move(opponentPosition, (opponentPosition - lastOpponentPosition) / deltaTime, deltaTime);
            lastPlayerPosition = playerPosition;
            lastOpponentPosition = opponentPosition;

            // La raquette est tenue du côté où la balle arrive, à hauteur de hanche ; le bras la suit.
            Vector3 hand = playerPosition + new Vector3(rally.HittingSide * 0.35f, 0.95f, 0.15f);
            racketVisual.SetPositionAndRotation(hand, rally.RacketRotation);
            player.Pose(hand);

            whiffAge += deltaTime;
            racketTrail.emitting = whiffAge < WhiffTrailDuration;

            // L'adversaire regarde vers le joueur ; sa raquette balaie brièvement quand il frappe.
            float opponentSide = rally.OpponentHittingSide;
            float swingProgress = Mathf.Clamp01((Time.unscaledTime - rally.OpponentSwingTime) / 0.3f);
            float sweep = Mathf.Lerp(-70f, 70f, Mathf.SmoothStep(0f, 1f, swingProgress)) * -opponentSide;
            Vector3 opponentHand = opponentPosition + new Vector3(opponentSide * 0.35f, 0.95f, -0.15f);
            opponentRacket.SetPositionAndRotation(opponentHand,
                Quaternion.Euler(0f, 180f + (swingProgress < 1f ? sweep : 0f), 0f) * Quaternion.Euler(-55f, 0f, 0f));
            opponent.Pose(opponentHand);
        }

        /// <summary>La caméra accompagne un peu le joueur sur la largeur, et recule à peine à l'impact.</summary>
        void UpdateCamera(float deltaTime)
        {
            if (view == null)
            {
                return;
            }

            cameraOffset = Mathf.Lerp(cameraOffset, rally.PlayerPosition.x * 0.45f, 1f - Mathf.Exp(-4f * deltaTime));
            view.transform.position = cameraHome + new Vector3(cameraOffset, 0f, 0f) + effects.CameraOffset;
        }
    }
}
