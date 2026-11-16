using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Déroulement d'un échange entre le joueur et l'adversaire.
    /// Gère la balle (vol, rebonds, filet, limites du court), le placement automatique du joueur,
    /// sa frappe (timing, direction, puissance, effet), l'adversaire et l'attribution du point.
    /// Le joueur est du côté z négatif, l'adversaire du côté z positif.
    /// </summary>
    [RequireComponent(typeof(RacketInput))]
    public sealed class RallyController : MonoBehaviour
    {
        public enum Outcome
        {
            None,
            PointWon,
            PointLost,
            Whiff,
        }

        /// <summary>Camp d'un joueur : signe de sa coordonnée z.</summary>
        enum Side
        {
            Player = -1,
            Opponent = 1,
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
        OpponentAi opponent;

        Transform ballVisual;
        Transform ballShadow;
        Transform playerVisual;
        Transform racketVisual;
        Transform opponentVisual;
        Transform opponentRacket;
        Transform landingMarker;
        Renderer landingRenderer;

        BallState ball;
        bool ballActive;
        bool ballDead;
        Side lastHitter;
        int bounceCount;
        int rallyShots;
        double simTime;
        double launchTime;
        double deadTime;
        double nextFeedTime;
        float accumulator;

        // Balle à jouer par le camp qui reçoit : où et quand la frapper.
        bool hasContact;
        Vector3 contactPoint;
        double contactTime;
        float hittingSide = 1f;

        Vector3 playerPosition;
        Vector3 playerTarget;
        float opponentSwingTime = -10f;

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

        public int PlayerPoints { get; private set; }

        public int OpponentPoints { get; private set; }

        /// <summary>Nombre de frappes de l'échange en cours ou du dernier échange.</summary>
        public int RallyShots => rallyShots;

        public int LongestRally { get; private set; }

        /// <summary>Frappes de l'adversaire depuis le lancement, et celles qui ont fini en faute.</summary>
        public int OpponentShots { get; private set; }

        public int OpponentFaults { get; private set; }

        public AiDifficulty Difficulty { get; set; }

        /// <summary>Mode démonstration : le jeu frappe tout seul à la place du joueur (touche T).</summary>
        public bool AutoSwing { get; set; }

        public float TimingWindow => config.timingWindow;

        public Vector3 PlayerPosition => playerPosition;

        /// <summary>Temps restant avant l'instant idéal de frappe du joueur, en secondes (NaN s'il n'a pas de balle à jouer).</summary>
        public float TimeToContact => hasContact && lastHitter == Side.Opponent ? (float)(contactTime - simTime) : float.NaN;

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
            Difficulty = config.aiDifficulty;

            CourtBuilder.Build(court);
            BuildActors();

            playerPosition = HomePosition(Side.Player);
            playerTarget = playerPosition;
            opponent = new OpponentAi(System.Environment.TickCount);
            opponent.Reset(HomePosition(Side.Opponent));
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

            playerVisual = BuildCharacter("Joueur", new Color(0.2f, 0.45f, 0.85f));
            racketVisual = RacketModel.Build(out Renderer _);
            opponentVisual = BuildCharacter("Adversaire", new Color(0.9f, 0.45f, 0.2f));
            opponentRacket = RacketModel.Build(out Renderer _);
            opponentRacket.name = "Raquette de l'adversaire";
        }

        static Transform BuildCharacter(string name, Color color)
        {
            Transform root = new GameObject(name).transform;
            CourtBuilder.Primitive(PrimitiveType.Capsule, root, "Corps", new Vector3(0f, 0.75f, 0f),
                Quaternion.identity, new Vector3(0.45f, 0.75f, 0.35f), color);
            CourtBuilder.Primitive(PrimitiveType.Sphere, root, "Tête", new Vector3(0f, 1.65f, 0f),
                Quaternion.identity, Vector3.one * 0.3f, new Color(0.95f, 0.8f, 0.65f));
            return root;
        }

        Vector3 HomePosition(Side side)
        {
            return new Vector3(0f, 0f, (int)side * (court.HalfLength + config.playerHomeDepth));
        }

        static Side Other(Side side)
        {
            return side == Side.Player ? Side.Opponent : Side.Player;
        }

        void Update()
        {
            // Relus à chaque image : un réglage modifié dans l'inspecteur s'applique tout de suite.
            court = config.ToCourt();
            ballSettings = config.ToBallSettings();

            if (Input.GetKeyDown(KeyCode.T))
            {
                AutoSwing = !AutoSwing;
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                Difficulty = (AiDifficulty)(((int)Difficulty + 1) % 3);
            }

            accumulator += Mathf.Min(Time.deltaTime, 0.1f);
            while (accumulator >= BallPhysics.TimeStep)
            {
                accumulator -= BallPhysics.TimeStep;
                simTime += BallPhysics.TimeStep;
                StepBall();
                StepOpponent(BallPhysics.TimeStep);
            }

            if (!ballActive && simTime >= nextFeedTime)
            {
                Feed();
            }

            if (AutoSwing)
            {
                UpdateAutoSwing();
            }

            playerPosition = Vector3.MoveTowards(playerPosition, playerTarget, config.playerMoveSpeed * Time.deltaTime);
            UpdateVisuals();
        }

        // ---------------------------------------------------------------- Balle

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
                AwardPoint(Other(lastHitter), lastHitter == Side.Player
                    ? "Ta balle reste dans le filet."
                    : "L'adversaire met la balle dans le filet.");
            }

            bool lost = Mathf.Abs(ball.Position.x) > 25f || Mathf.Abs(ball.Position.z) > 35f
                || simTime - launchTime > MaxFlightTime;
            if (lost && !ballDead)
            {
                AwardPoint(Other(lastHitter), "Balle perdue.");
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
            Side receiver = Other(lastHitter);

            if (bounceCount == 1)
            {
                bool inside = ball.Position.z * (int)receiver > 0f && court.IsInside(ball.Position.x, ball.Position.z);
                if (lastHitter == Side.Player)
                {
                    ShowLanding(ball.Position, inside);
                }

                if (!inside)
                {
                    string detail;
                    if (ball.Position.z * (int)receiver <= 0f)
                    {
                        detail = lastHitter == Side.Player ? "Ta balle retombe dans ton camp." : "La balle de l'adversaire retombe dans son camp.";
                    }
                    else
                    {
                        string distance = DistanceOutside(ball.Position).ToString("F1") + " m";
                        detail = lastHitter == Side.Player ? "Faute : balle dehors de " + distance + "." : "Faute de l'adversaire : dehors de " + distance + ".";
                    }

                    AwardPoint(receiver, detail);
                }
            }
            else
            {
                // Deuxième rebond : celui qui devait jouer la balle perd le point.
                AwardPoint(lastHitter, receiver == Side.Player
                    ? "Balle non jouée : deux rebonds."
                    : "L'adversaire est débordé.");
            }
        }

        float DistanceOutside(Vector3 position)
        {
            float lateral = Mathf.Max(0f, Mathf.Abs(position.x) - court.HalfWidth);
            float depth = Mathf.Max(0f, Mathf.Abs(position.z) - court.HalfLength);
            return Mathf.Sqrt(lateral * lateral + depth * depth);
        }

        void AwardPoint(Side winner, string detail)
        {
            if (winner == Side.Player)
            {
                PlayerPoints++;
            }
            else
            {
                OpponentPoints++;
            }

            if (lastHitter == Side.Opponent && winner == Side.Player && bounceCount <= 1)
            {
                // Point gagné sur une faute directe de l'adversaire (filet ou dehors).
                OpponentFaults++;
            }

            LongestRally = Mathf.Max(LongestRally, rallyShots);
            SetOutcome(winner == Side.Player ? Outcome.PointWon : Outcome.PointLost, detail);

            ballDead = true;
            deadTime = simTime;
            hasContact = false;
            playerTarget = HomePosition(Side.Player);
            opponent.StopTracking();
        }

        /// <summary>
        /// Engagement : l'adversaire met la balle en jeu depuis le fond de son camp.
        /// Remplacé par un vrai service à l'étape e.
        /// </summary>
        void Feed()
        {
            Vector3 start = opponent.Position + new Vector3(0.4f, 1f, -0.3f);
            var target = new Vector3(
                Random.Range(-config.feederHalfWidth, config.feederHalfWidth),
                0f,
                -Random.Range(config.feederDepthMin, config.feederDepthMax));

            float topspin = 800f * 2f * Mathf.PI / 60f;
            ShotSolution solution = ShotSolver.Solve(start, target, config.feederSpeed, topspin, 0.5f, ballSettings, court);

            ball = new BallState { Position = start, Velocity = solution.Velocity, Spin = solution.Spin };
            ballActive = true;
            ballDead = false;
            rallyShots = 0;
            landingMarker.gameObject.SetActive(false);
            opponentSwingTime = Time.unscaledTime;

            OnBallHit(Side.Opponent);
        }

        /// <summary>La balle vient d'être frappée par ce camp : l'autre camp se prépare à la jouer.</summary>
        void OnBallHit(Side hitter)
        {
            lastHitter = hitter;
            bounceCount = 0;
            launchTime = simTime;
            rallyShots++;

            Side receiver = Other(hitter);
            hasContact = PredictContact(receiver, out contactPoint, out double delay);
            contactTime = simTime + delay;

            if (receiver == Side.Player)
            {
                opponent.StopTracking();
                if (hasContact)
                {
                    // Le joueur se place à côté de la balle, du côté le plus proche de lui.
                    hittingSide = contactPoint.x >= playerPosition.x ? 1f : -1f;
                    playerTarget = new Vector3(
                        contactPoint.x - hittingSide * config.playerSideOffset,
                        0f,
                        Mathf.Min(contactPoint.z - 0.25f, -1.5f));
                }
                else
                {
                    playerTarget = HomePosition(Side.Player);
                }
            }
            else
            {
                playerTarget = HomePosition(Side.Player);
                if (hasContact)
                {
                    opponent.OnIncoming(contactPoint, contactTime, simTime, config.GetAiLevel(Difficulty));
                }
                else
                {
                    // Balle annoncée dans le filet ou dehors : l'adversaire la laisse passer.
                    opponent.StopTracking();
                }
            }
        }

        /// <summary>
        /// Prédit où et quand le camp qui reçoit doit frapper : après un premier rebond valable dans
        /// son camp, quand la balle redescend à la hauteur de frappe (ou à son sommet si elle ne
        /// monte pas jusque-là). Renvoie false si la balle n'est pas jouable.
        /// </summary>
        bool PredictContact(Side receiver, out Vector3 point, out double delay)
        {
            point = default;
            delay = 0.0;
            BallState state = ball;
            float time = 0f;
            bool bounced = false;

            while (time < MaxFlightTime)
            {
                BallEvent ballEvent = BallPhysics.Step(ref state, BallPhysics.TimeStep, ballSettings, court);
                time += BallPhysics.TimeStep;

                if (ballEvent == BallEvent.Net)
                {
                    return false;
                }

                if (ballEvent == BallEvent.Bounce)
                {
                    bool inside = state.Position.z * (int)receiver > 0f && court.IsInside(state.Position.x, state.Position.z);
                    if (bounced || !inside)
                    {
                        return false;
                    }

                    bounced = true;
                }
                else if (bounced)
                {
                    // La balle redescend sous la hauteur de frappe. Si son sommet était déjà plus bas,
                    // cette condition est vraie dès le sommet.
                    bool comfortable = state.Velocity.y <= 0f && state.Position.y <= config.contactHeight;

                    // Balle longue : on la prend plus tôt plutôt que de reculer trop loin.
                    bool tooDeep = Mathf.Abs(state.Position.z) >= court.HalfLength + config.contactMaxDepth;

                    if (comfortable || tooDeep)
                    {
                        point = state.Position;
                        delay = time;
                        return true;
                    }
                }
            }

            return false;
        }

        // ---------------------------------------------------------------- Joueur

        void OnSwing(SwingEvent swing)
        {
            if (!ballActive || ballDead || lastHitter != Side.Opponent || !hasContact)
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

            ApplyPlayerShot(swing);
            landingMarker.gameObject.SetActive(false);
            SetOutcome(Outcome.None, string.Empty);
            OnBallHit(Side.Player);

            if (link != null)
            {
                link.SendVibration(Mathf.RoundToInt(Mathf.Lerp(25f, 70f, swing.Power)));
            }
        }

        /// <summary>La fin du geste a précisé l'effet (slice) : la trajectoire est recalculée en vol.</summary>
        void OnSwingRefined(SwingEvent swing)
        {
            bool stillFlying = ballActive && !ballDead && lastHitter == Side.Player && bounceCount == 0;
            if (!stillFlying || swing.Id != lastHitSwingId || simTime - lastHitTime > RefineWindow || ball.Position.z > -0.5f)
            {
                return;
            }

            lastHitSwing = swing;
            ApplyPlayerShot(swing);

            // La trajectoire a changé : l'adversaire relit la balle, sans compter une frappe de plus.
            rallyShots--;
            OnBallHit(Side.Player);
        }

        void ApplyPlayerShot(SwingEvent swing)
        {
            ShotPlanner.Plan plan = ShotPlanner.Build(swing, lastHitTimingNormalized, (int)Side.Opponent, config);
            ShotSolution solution = ShotSolver.Solve(ball.Position, plan.Target, plan.Speed, plan.SpinRate,
                plan.NetMargin, ballSettings, court);
            ball.Velocity = solution.Velocity;
            ball.Spin = solution.Spin;
            LastShotSpeed = solution.Speed;
        }

        /// <summary>
        /// Mode démonstration (touche T) : le jeu frappe tout seul, avec un timing, une puissance
        /// et un effet tirés au hasard. Sert à voir des échanges sans téléphone.
        /// </summary>
        void UpdateAutoSwing()
        {
            if (!ballActive || ballDead || lastHitter != Side.Opponent || !hasContact)
            {
                autoSwingPlanned = false;
                return;
            }

            if (!autoSwingPlanned)
            {
                autoSwingPlanned = true;
                // Timing le plus souvent bon, parfois franchement décalé, comme un vrai joueur.
                autoSwingOffset = (Random.Range(-1.1f, 1.1f) + Random.Range(-1.1f, 1.1f)) * 0.5f * config.timingWindow;
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

        // ---------------------------------------------------------------- Adversaire

        void StepOpponent(float deltaTime)
        {
            AiLevel level = config.GetAiLevel(Difficulty);
            opponent.Move(simTime, deltaTime, HomePosition(Side.Opponent), config.playerMoveSpeed,
                config.playerSideOffset, level);

            bool ballToPlay = ballActive && !ballDead && lastHitter == Side.Player && hasContact && bounceCount == 1;
            if (!ballToPlay || simTime < contactTime)
            {
                return;
            }

            // Une seule tentative par balle : s'il n'y arrive pas, la balle rebondit une seconde fois.
            hasContact = false;
            if (!opponent.CanReach(ball.Position, config.playerSideOffset, config.playerReach))
            {
                return;
            }

            bool stretched = opponent.IsStretched(simTime, config.playerSideOffset, level);
            AiShot shot = opponent.ChooseShot(ball.Position, playerPosition, stretched, level, config, ballSettings, court);
            ball.Velocity = shot.Solution.Velocity;
            ball.Spin = shot.Solution.Spin;
            OpponentShots++;
            opponentSwingTime = Time.unscaledTime;
            OnBallHit(Side.Opponent);
        }

        // ---------------------------------------------------------------- Affichage

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

            // L'adversaire regarde vers le joueur ; sa raquette balaie brièvement quand il frappe.
            opponentVisual.position = opponent.Position;
            float swingProgress = Mathf.Clamp01((Time.unscaledTime - opponentSwingTime) / 0.3f);
            float sweep = Mathf.Lerp(-70f, 70f, Mathf.SmoothStep(0f, 1f, swingProgress)) * -opponent.HittingSide;
            Vector3 opponentHand = opponent.Position + new Vector3(opponent.HittingSide * 0.35f, 0.95f, -0.15f);
            opponentRacket.SetPositionAndRotation(opponentHand,
                Quaternion.Euler(0f, 180f + (swingProgress < 1f ? sweep : 0f), 0f) * Quaternion.Euler(-55f, 0f, 0f));
        }
    }
}
