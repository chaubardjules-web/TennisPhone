using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Étape c : un lanceur envoie des balles, le joueur se place tout seul et renvoie la balle
    /// d'un swing. Gère la balle (vol, rebonds, filet, limites du court), le placement automatique
    /// du joueur et la frappe (timing, direction, puissance, effet).
    /// </summary>
    [RequireComponent(typeof(RacketInput))]
    public sealed class RallyController : MonoBehaviour
    {
        public enum Outcome
        {
            None,
            In,
            Out,
            Net,
            Missed,
            Whiff,
        }

        enum Hitter
        {
            Feeder,
            Player,
        }

        // La balle est affichée plus grosse que nature pour rester lisible de loin.
        const float BallVisualScale = 2.4f;
        const float RefineWindow = 0.4f;
        const float DeadBallLinger = 1.2f;
        const float MaxFlightTime = 9f;

        RacketInput input;
        PhoneLink link;
        GameplayConfig config;
        CourtDimensions court;
        BallSettings ballSettings;

        Transform ballVisual;
        Transform ballShadow;
        Transform playerVisual;
        Transform racketVisual;
        Transform landingMarker;
        Renderer landingRenderer;

        BallState ball;
        bool ballActive;
        bool ballDead;
        Hitter lastHitter;
        int bounceCount;
        double simTime;
        double launchTime;
        double deadTime;
        double nextFeedTime;
        float accumulator;

        bool hasContact;
        Vector3 contactPoint;
        double contactTime;
        float hittingSide = 1f;

        Vector3 playerPosition;
        Vector3 playerTarget;

        bool autoSwingPlanned;
        float autoSwingOffset;
        int autoSwingId = -100000;

        int lastHitSwingId;
        double lastHitTime;
        float lastHitTimingNormalized;
        SwingEvent lastHitSwing;

        // --- État lu par l'affichage ---

        public Outcome LastOutcome { get; private set; }

        /// <summary>Instant (Time.unscaledTime) du dernier résultat affiché.</summary>
        public float LastOutcomeTime { get; private set; } = -10f;

        public string LastOutcomeDetail { get; private set; } = string.Empty;

        public bool HasLastHit { get; private set; }

        /// <summary>Écart du dernier swing réussi à l'instant idéal, en secondes (négatif : tôt).</summary>
        public float LastTimingSeconds { get; private set; }

        public float LastShotSpeed { get; private set; }

        public SwingEvent LastSwing => lastHitSwing;

        public int Hits { get; private set; }

        public int InCount { get; private set; }

        public int OutCount { get; private set; }

        public int NetCount { get; private set; }

        public int MissCount { get; private set; }

        /// <summary>Mode démonstration : le jeu frappe tout seul (touche T).</summary>
        public bool AutoSwing { get; set; }

        public float TimingWindow => config.timingWindow;

        public Vector3 PlayerPosition => playerPosition;

        /// <summary>Temps restant avant l'instant idéal de frappe, en secondes (NaN s'il n'y a pas de balle à jouer).</summary>
        public float TimeToContact => hasContact && lastHitter == Hitter.Feeder ? (float)(contactTime - simTime) : float.NaN;

        void Awake()
        {
            input = GetComponent<RacketInput>();
            link = GetComponent<PhoneLink>();
        }

        void Start()
        {
            config = input.Config;
            court = config.ToCourt();
            ballSettings = config.ToBallSettings();

            CourtBuilder.Build(court);
            BuildActors();

            playerPosition = HomePosition();
            playerTarget = playerPosition;
            nextFeedTime = 1.5;
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

        void BuildActors()
        {
            float diameter = config.ballRadius * 2f * BallVisualScale;
            ballVisual = CourtBuilder.Primitive(PrimitiveType.Sphere, null, "Balle", Vector3.zero, Quaternion.identity,
                Vector3.one * diameter, new Color(0.85f, 0.95f, 0.2f));
            ballShadow = CourtBuilder.Primitive(PrimitiveType.Cylinder, null, "Ombre de la balle", Vector3.zero,
                Quaternion.identity, new Vector3(diameter * 1.1f, 0.001f, diameter * 1.1f), new Color(0.05f, 0.1f, 0.06f));
            landingMarker = CourtBuilder.Primitive(PrimitiveType.Cylinder, null, "Marque du rebond", Vector3.zero,
                Quaternion.identity, new Vector3(0.35f, 0.002f, 0.35f), Color.white);
            landingRenderer = landingMarker.GetComponent<Renderer>();
            landingMarker.gameObject.SetActive(false);
            ballVisual.gameObject.SetActive(false);
            ballShadow.gameObject.SetActive(false);

            playerVisual = new GameObject("Joueur").transform;
            CourtBuilder.Primitive(PrimitiveType.Capsule, playerVisual, "Corps", new Vector3(0f, 0.75f, 0f),
                Quaternion.identity, new Vector3(0.45f, 0.75f, 0.35f), new Color(0.2f, 0.45f, 0.85f));
            CourtBuilder.Primitive(PrimitiveType.Sphere, playerVisual, "Tête", new Vector3(0f, 1.65f, 0f),
                Quaternion.identity, Vector3.one * 0.3f, new Color(0.95f, 0.8f, 0.65f));

            racketVisual = RacketModel.Build(out Renderer _);
        }

        Vector3 HomePosition()
        {
            return new Vector3(0f, 0f, -(court.HalfLength + config.playerHomeDepth));
        }

        void Update()
        {
            // Relus à chaque image : un réglage modifié dans l'inspecteur s'applique tout de suite.
            court = config.ToCourt();
            ballSettings = config.ToBallSettings();

            accumulator += Mathf.Min(Time.deltaTime, 0.1f);
            while (accumulator >= BallPhysics.TimeStep)
            {
                accumulator -= BallPhysics.TimeStep;
                simTime += BallPhysics.TimeStep;
                StepBall();
            }

            if (!ballActive && simTime >= nextFeedTime)
            {
                Feed();
            }

            if (Input.GetKeyDown(KeyCode.T))
            {
                AutoSwing = !AutoSwing;
            }

            if (AutoSwing)
            {
                UpdateAutoSwing();
            }

            MovePlayer();
            UpdateVisuals();
        }

        void StepBall()
        {
            if (!ballActive)
            {
                return;
            }

            BallEvent ballEvent = BallPhysics.Step(ref ball, BallPhysics.TimeStep, ballSettings, court);
            if (ballEvent == BallEvent.Bounce)
            {
                OnBounce();
            }
            else if (ballEvent == BallEvent.Net && !ballDead)
            {
                if (lastHitter == Hitter.Player)
                {
                    NetCount++;
                    SetOutcome(Outcome.Net, "La balle reste dans le filet.");
                }

                KillBall();
            }

            bool lost = Mathf.Abs(ball.Position.x) > 25f || Mathf.Abs(ball.Position.z) > 35f
                || simTime - launchTime > MaxFlightTime;
            if (lost && !ballDead)
            {
                KillBall();
            }

            if (ballDead && simTime - deadTime > DeadBallLinger)
            {
                ballActive = false;
                nextFeedTime = simTime + config.feederInterval;
            }
        }

        void OnBounce()
        {
            if (ballDead)
            {
                return;
            }

            bounceCount++;
            if (lastHitter == Hitter.Player)
            {
                if (bounceCount == 1)
                {
                    bool inside = ball.Position.z > 0f && court.IsInside(ball.Position.x, ball.Position.z);
                    ShowLanding(ball.Position, inside);
                    if (inside)
                    {
                        InCount++;
                        SetOutcome(Outcome.In, "Bonne balle, à "
                            + DistanceToNearestLine(ball.Position).ToString("F1") + " m de la ligne.");
                    }
                    else
                    {
                        OutCount++;
                        SetOutcome(Outcome.Out, ball.Position.z <= 0f
                            ? "La balle retombe dans ton camp."
                            : "Faute de " + DistanceOutside(ball.Position).ToString("F1") + " m.");
                    }
                }

                // L'échange s'arrête au rebond tant qu'il n'y a pas d'adversaire (étape d).
                KillBall();
            }
            else if (bounceCount >= 2)
            {
                MissCount++;
                SetOutcome(Outcome.Missed, "Balle non jouée : deux rebonds.");
                KillBall();
            }
        }

        float DistanceToNearestLine(Vector3 position)
        {
            return Mathf.Min(court.HalfWidth - Mathf.Abs(position.x), court.HalfLength - Mathf.Abs(position.z));
        }

        float DistanceOutside(Vector3 position)
        {
            float lateral = Mathf.Max(0f, Mathf.Abs(position.x) - court.HalfWidth);
            float depth = Mathf.Max(0f, Mathf.Abs(position.z) - court.HalfLength);
            return Mathf.Sqrt(lateral * lateral + depth * depth);
        }

        void KillBall()
        {
            ballDead = true;
            deadTime = simTime;
            hasContact = false;
            playerTarget = HomePosition();
        }

        /// <summary>Le lanceur envoie une nouvelle balle vers le camp du joueur.</summary>
        void Feed()
        {
            var start = new Vector3(Random.Range(-2.5f, 2.5f), 1f, court.HalfLength - 0.5f);
            var target = new Vector3(
                Random.Range(-config.feederHalfWidth, config.feederHalfWidth),
                0f,
                -Random.Range(config.feederDepthMin, config.feederDepthMax));

            float topspin = 800f * 2f * Mathf.PI / 60f;
            ShotSolution solution = ShotSolver.Solve(start, target, config.feederSpeed, topspin, 0.5f, ballSettings, court);

            ball = new BallState { Position = start, Velocity = solution.Velocity, Spin = solution.Spin };
            ballActive = true;
            ballDead = false;
            lastHitter = Hitter.Feeder;
            bounceCount = 0;
            launchTime = simTime;
            landingMarker.gameObject.SetActive(false);

            PlanContact();
        }

        /// <summary>
        /// Prédit où et quand le joueur doit frapper : après le premier rebond, quand la balle
        /// redescend à la hauteur de frappe (ou à son sommet si elle ne monte pas jusque-là).
        /// </summary>
        void PlanContact()
        {
            hasContact = false;
            BallState state = ball;
            float time = 0f;
            bool bounced = false;

            while (time < MaxFlightTime)
            {
                BallEvent ballEvent = BallPhysics.Step(ref state, BallPhysics.TimeStep, ballSettings, court);
                time += BallPhysics.TimeStep;

                if (ballEvent == BallEvent.Net)
                {
                    return;
                }

                if (ballEvent == BallEvent.Bounce)
                {
                    if (bounced || state.Position.z >= 0f)
                    {
                        return;
                    }

                    bounced = true;
                }
                else if (bounced && state.Velocity.y <= 0f && state.Position.y <= config.contactHeight)
                {
                    // La balle redescend sous la hauteur de frappe. Si son sommet était déjà plus bas,
                    // cette condition est vraie dès le sommet.
                    hasContact = true;
                    contactPoint = state.Position;
                    contactTime = simTime + time;
                    break;
                }
            }

            if (!hasContact)
            {
                return;
            }

            // Le joueur se place à côté de la balle, du côté le plus proche de lui.
            hittingSide = contactPoint.x >= playerPosition.x ? 1f : -1f;
            playerTarget = new Vector3(
                contactPoint.x - hittingSide * config.playerSideOffset,
                0f,
                Mathf.Min(contactPoint.z - 0.25f, -1.5f));
        }

        /// <summary>
        /// Mode démonstration (touche T) : le jeu frappe tout seul, avec un timing, une puissance
        /// et un effet tirés au hasard. Sert à voir les trajectoires sans téléphone.
        /// </summary>
        void UpdateAutoSwing()
        {
            if (!ballActive || ballDead || lastHitter != Hitter.Feeder || !hasContact)
            {
                autoSwingPlanned = false;
                return;
            }

            if (!autoSwingPlanned)
            {
                autoSwingPlanned = true;
                autoSwingOffset = Random.Range(-0.8f, 0.8f) * config.timingWindow;
            }

            if (simTime < contactTime + autoSwingOffset)
            {
                return;
            }

            autoSwingPlanned = false;
            bool rightHanded = config.handedness == Handedness.Right;
            float[] spins = { -0.9f, 0f, 0f, 0.9f };
            OnSwing(new SwingEvent
            {
                Id = autoSwingId--,
                Source = SwingSource.KeyboardMouse,
                Side = (hittingSide > 0f) == rightHanded ? SwingSide.Forehand : SwingSide.Backhand,
                Power = Random.Range(0.2f, 1f),
                Spin = spins[Random.Range(0, spins.Length)],
                PeakTimeSeconds = simTime,
                AgeSeconds = 0f,
            });
        }

        void MovePlayer()
        {
            playerPosition = Vector3.MoveTowards(playerPosition, playerTarget, config.playerMoveSpeed * Time.deltaTime);
        }

        void OnSwing(SwingEvent swing)
        {
            if (!ballActive || ballDead || lastHitter != Hitter.Feeder || !hasContact)
            {
                SetOutcome(Outcome.Whiff, "Swing dans le vide : pas de balle à jouer.");
                return;
            }

            double swingTime = simTime - swing.AgeSeconds;
            float timing = (float)(swingTime - contactTime);
            if (Mathf.Abs(timing) > config.timingWindow)
            {
                SetOutcome(Outcome.Whiff, (timing < 0f ? "Trop tôt de " : "Trop tard de ")
                    + Mathf.RoundToInt(Mathf.Abs(timing) * 1000f) + " ms.");
                return;
            }

            Vector3 hand = playerPosition + new Vector3(hittingSide * config.playerSideOffset, 0f, 0f);
            float distance = Vector2.Distance(new Vector2(hand.x, hand.z), new Vector2(ball.Position.x, ball.Position.z));
            if (distance > config.playerReach)
            {
                SetOutcome(Outcome.Whiff, "Balle hors de portée (" + distance.ToString("F1") + " m).");
                return;
            }

            lastHitSwing = swing;
            lastHitSwingId = swing.Id;
            lastHitTime = simTime;
            lastHitTimingNormalized = timing / config.timingWindow;
            LastTimingSeconds = timing;
            HasLastHit = true;
            Hits++;

            ApplyShot(swing);

            lastHitter = Hitter.Player;
            bounceCount = 0;
            hasContact = false;
            launchTime = simTime;
            playerTarget = HomePosition();
            landingMarker.gameObject.SetActive(false);
            SetOutcome(Outcome.None, string.Empty);

            if (link != null)
            {
                link.SendVibration(Mathf.RoundToInt(Mathf.Lerp(25f, 70f, swing.Power)));
            }
        }

        /// <summary>La fin du geste a précisé l'effet (slice) : la trajectoire est recalculée en vol.</summary>
        void OnSwingRefined(SwingEvent swing)
        {
            bool stillFlying = ballActive && !ballDead && lastHitter == Hitter.Player && bounceCount == 0;
            if (!stillFlying || swing.Id != lastHitSwingId || simTime - lastHitTime > RefineWindow || ball.Position.z > -0.5f)
            {
                return;
            }

            lastHitSwing = swing;
            ApplyShot(swing);
        }

        void ApplyShot(SwingEvent swing)
        {
            ShotPlanner.Plan plan = ShotPlanner.Build(swing, lastHitTimingNormalized, 1, config);
            ShotSolution solution = ShotSolver.Solve(ball.Position, plan.Target, plan.Speed, plan.SpinRate,
                plan.NetMargin, ballSettings, court);
            ball.Velocity = solution.Velocity;
            ball.Spin = solution.Spin;
            LastShotSpeed = solution.Speed;
        }

        void SetOutcome(Outcome outcome, string detail)
        {
            LastOutcome = outcome;
            LastOutcomeDetail = detail;
            LastOutcomeTime = Time.unscaledTime;
        }

        void ShowLanding(Vector3 position, bool inside)
        {
            landingMarker.position = new Vector3(position.x, 0.006f, position.z);
            landingRenderer.material.color = inside ? new Color(0.3f, 0.9f, 0.4f) : new Color(0.95f, 0.3f, 0.25f);
            landingMarker.gameObject.SetActive(true);
        }

        void UpdateVisuals()
        {
            ballVisual.gameObject.SetActive(ballActive);
            ballShadow.gameObject.SetActive(ballActive);
            if (ballActive)
            {
                ballVisual.position = ball.Position;
                ballShadow.position = new Vector3(ball.Position.x, 0.005f, ball.Position.z);
            }

            playerVisual.position = playerPosition;

            // La raquette est tenue du côté où la balle arrive, à hauteur de hanche.
            Vector3 hand = playerPosition + new Vector3(hittingSide * 0.35f, 0.95f, 0.15f);
            racketVisual.SetPositionAndRotation(hand, input.RacketRotation);
        }
    }
}
