using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Déroulement du match entre le joueur et l'adversaire : service en deux gestes, échange,
    /// fautes, attribution des points, des jeux et du set.
    /// Gère la balle (vol, rebonds, filet, limites du court), le placement automatique du joueur,
    /// sa frappe (timing, direction, puissance, effet) et l'adversaire.
    /// Le joueur est du côté z négatif, l'adversaire du côté z positif.
    /// Ne dessine rien : l'affichage (MatchVisuals), le HUD (RallyHud) et le son (SoundPlayer)
    /// lisent son état et s'abonnent à ses événements.
    /// </summary>
    [RequireComponent(typeof(RacketInput))]
    public sealed class RallyController : MonoBehaviour
    {
        public enum Phase
        {
            /// <summary>Les joueurs se placent ; le serveur n'a pas encore lancé la balle.</summary>
            PreServe,

            /// <summary>La balle de service est en l'air.</summary>
            Toss,
            Rally,
            PointOver,
            SetOver,
        }

        /// <summary>Camp d'un joueur : signe de sa coordonnée z.</summary>
        enum Side
        {
            Player = -1,
            Opponent = 1,
        }

        /// <summary>Écart au timing idéal (s) sous lequel une frappe est « parfaite ».</summary>
        public const float PerfectTiming = 0.025f;

        const float RefineWindow = 0.4f;
        const float FaultPause = 1.3f;
        const float MaxFlightTime = 9f;
        const float InPlaceDistance = 0.35f;

        RacketInput input;
        PhoneLink link;
        GameplayConfig config;
        CourtDimensions court;
        BallSettings ballSettings;
        OpponentAi opponent;
        MatchScore score;

        Phase phase;
        double phaseTime;
        BallState ball;
        bool ballActive;
        bool ballDead;
        Side lastHitter;
        int bounceCount;
        int rallyShots;
        double simTime;
        double launchTime;
        float accumulator;

        // Service en cours.
        bool isServe;
        int serveBoxSide;
        int faultCount;
        double tossTime;
        bool secondServePending;

        // Balle à jouer par le camp qui reçoit : où et quand la frapper.
        bool hasContact;
        Vector3 contactPoint;
        double contactTime;
        float hittingSide = 1f;

        Vector3 playerPosition;
        Vector3 playerTarget;
        Vector3 opponentSpot;
        float opponentSwingTime = -10f;

        bool autoSwingPlanned;
        float autoSwingOffset;
        int autoSwingId = -100000;

        int lastHitSwingId;
        double lastHitTime;
        float lastHitTimingNormalized;
        SwingEvent lastHitSwing;

        // --- Événements pour l'affichage et le son ---

        /// <summary>Une frappe (service compris) vient de toucher la balle.</summary>
        public event System.Action<HitInfo> BallHit;

        /// <summary>L'effet de la dernière frappe du joueur vient d'être précisé (slice) : nouvel effet, de -1 à +1.</summary>
        public event System.Action<float> ShotRefined;

        public event System.Action<BounceInfo> Bounced;

        /// <summary>La balle vient de toucher le filet, à cette position.</summary>
        public event System.Action<Vector3> NetTouched;

        /// <summary>Le joueur a fait un swing sans toucher de balle ; le texte dit pourquoi.</summary>
        public event System.Action<string> Whiffed;

        /// <summary>La balle de service vient d'être lancée (vrai : par le joueur).</summary>
        public event System.Action<bool> TossStarted;

        /// <summary>Les joueurs se replacent pour un service : la balle précédente disparaît.</summary>
        public event System.Action ServePrepared;

        /// <summary>Première faute de service : une deuxième balle suit.</summary>
        public event System.Action ServiceFaulted;

        public event System.Action<PointInfo> PointEnded;

        /// <summary>Une annonce à afficher balle morte (point, faute, jeu, set, consigne).</summary>
        public event System.Action<Announcement> Announced;

        // --- État lu par l'affichage ---

        public GameplayConfig Config => config;

        public BallState Ball => ball;

        /// <summary>Vrai tant qu'une balle est en jeu ou roule encore.</summary>
        public bool BallActive => ballActive;

        /// <summary>Vrai quand le point est joué : la balle ne compte plus.</summary>
        public bool BallDead => ballDead;

        /// <summary>Côté (en x) où le joueur tient sa raquette : +1 à droite, -1 à gauche.</summary>
        public float HittingSide => hittingSide;

        public Vector3 OpponentPosition => opponent != null ? opponent.Position : Vector3.zero;

        public float OpponentHittingSide => opponent != null ? opponent.HittingSide : 1f;

        /// <summary>Instant (Time.unscaledTime) de la dernière frappe de l'adversaire.</summary>
        public float OpponentSwingTime => opponentSwingTime;

        /// <summary>Orientation de la raquette du joueur (téléphone, ou pose de repos).</summary>
        public Quaternion RacketRotation => input.RacketRotation;

        public MatchScore Score => score;

        public Phase CurrentPhase => phase;

        /// <summary>Vrai quand c'est au joueur de servir.</summary>
        public bool PlayerServing => score != null && score.Server == 0;

        /// <summary>Nombre de fautes de service sur le point en cours (0 ou 1).</summary>
        public int FaultCount => faultCount;

        public bool HasLastHit { get; private set; }

        /// <summary>Écart du dernier swing réussi à l'instant idéal, en secondes (négatif : tôt).</summary>
        public float LastTimingSeconds { get; private set; }

        /// <summary>Fenêtre de timing qui s'appliquait à ce swing, en secondes.</summary>
        public float LastTimingWindow { get; private set; } = 0.18f;

        public bool LastHitWasServe { get; private set; }

        public float LastShotSpeed { get; private set; }

        public SwingEvent LastSwing => lastHitSwing;

        /// <summary>Nombre de frappes de l'échange en cours ou du dernier échange.</summary>
        public int RallyShots => rallyShots;

        public int LongestRally { get; private set; }

        /// <summary>Frappes de l'adversaire depuis le lancement, et celles qui ont fini en faute.</summary>
        public int OpponentShots { get; private set; }

        public int OpponentFaults { get; private set; }

        public int SetsPlayed { get; private set; }

        public AiDifficulty Difficulty { get; set; }

        /// <summary>Mode démonstration : le jeu joue tout seul à la place du joueur (touche T).</summary>
        public bool AutoSwing { get; set; }

        public Vector3 PlayerPosition => playerPosition;

        /// <summary>Vrai si le joueur est à sa place de service et peut lancer la balle.</summary>
        public bool PlayerReadyToToss => phase == Phase.PreServe && PlayerServing
            && Vector3.Distance(playerPosition, playerTarget) <= InPlaceDistance;

        /// <summary>
        /// Temps restant avant l'instant idéal de frappe du joueur, en secondes (NaN s'il n'a rien à frapper).
        /// </summary>
        public float TimeToIdeal
        {
            get
            {
                if (phase == Phase.Toss && PlayerServing)
                {
                    return (float)(tossTime + config.serveIdealDelay - simTime);
                }

                if (phase == Phase.Rally && hasContact && lastHitter == Side.Opponent)
                {
                    return (float)(contactTime - simTime);
                }

                return float.NaN;
            }
        }

        /// <summary>Fenêtre de timing en vigueur pour la prochaine frappe du joueur, en secondes.</summary>
        public float ActiveTimingWindow => phase == Phase.Toss || phase == Phase.PreServe
            ? config.serveTimingWindow
            : config.timingWindow;

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

            playerPosition = HomePosition(Side.Player);
            playerTarget = playerPosition;
            opponent = new OpponentAi(System.Environment.TickCount);
            opponent.Reset(HomePosition(Side.Opponent));
            opponentSpot = opponent.Position;

            StartSet();
        }

        void OnEnable()
        {
            input.Swung += OnSwing;
            input.SwingRefined += OnSwingRefined;
            input.Tossed += OnTossGesture;
        }

        void OnDisable()
        {
            input.Swung -= OnSwing;
            input.SwingRefined -= OnSwingRefined;
            input.Tossed -= OnTossGesture;
        }

        Vector3 HomePosition(Side side)
        {
            return new Vector3(0f, 0f, (int)side * (court.HalfLength + config.playerHomeDepth));
        }

        static Side Other(Side side)
        {
            return side == Side.Player ? Side.Opponent : Side.Player;
        }

        static int Index(Side side)
        {
            return side == Side.Player ? 0 : 1;
        }

        Side Server => score.Server == 0 ? Side.Player : Side.Opponent;

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

            if (phase == Phase.SetOver && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                StartSet();
            }

            accumulator += Mathf.Min(Time.deltaTime, 0.1f);
            while (accumulator >= BallPhysics.TimeStep)
            {
                accumulator -= BallPhysics.TimeStep;
                simTime += BallPhysics.TimeStep;
                StepBall();
                StepOpponent(BallPhysics.TimeStep);
                StepPhase();
            }

            if (AutoSwing)
            {
                UpdateAutoPlay();
            }

            playerPosition = Vector3.MoveTowards(playerPosition, playerTarget, config.playerMoveSpeed * Time.deltaTime);
        }

        // ---------------------------------------------------------------- Déroulement

        void StartSet()
        {
            score = new MatchScore(config.gamesToWinSet, config.setRequiresTwoGameLead, config.playerServesFirst ? 0 : 1);
            LongestRally = 0;
            faultCount = 0;
            Announce(MessageTone.Neutral, PlayerServing ? "À TOI DE SERVIR" : "L'ADVERSAIRE SERT",
                "Premier à " + config.gamesToWinSet + " jeux", 1.6f);
            BeginServe();
        }

        /// <summary>Place les deux joueurs pour le service du point en cours.</summary>
        void BeginServe()
        {
            phase = Phase.PreServe;
            phaseTime = simTime;
            ballActive = false;
            ballDead = false;
            isServe = false;
            hasContact = false;
            secondServePending = false;
            rallyShots = 0;
            ServePrepared?.Invoke();
            opponent.StopTracking();

            Side server = Server;
            Side receiver = Other(server);

            // La droite d'un joueur qui regarde vers z positif est x positif ; pour celui d'en face, l'inverse.
            float serverRight = -(int)server;
            float courtSign = score.ServeCourt == 0 ? 1f : -1f;
            float serverX = serverRight * courtSign * config.serverOffset;

            // Le service part en diagonale : le carré visé est du côté opposé en x.
            serveBoxSide = serverX > 0f ? -1 : 1;

            var serverSpot = new Vector3(serverX, 0f, (int)server * (court.HalfLength + 0.3f));
            var receiverSpot = new Vector3(serveBoxSide * court.HalfWidth * 0.6f, 0f,
                (int)receiver * (court.HalfLength + config.playerHomeDepth));

            if (server == Side.Player)
            {
                playerTarget = serverSpot;
                opponentSpot = receiverSpot;
                hittingSide = config.handedness == Handedness.Right ? 1f : -1f;
            }
            else
            {
                playerTarget = receiverSpot;
                opponentSpot = serverSpot;
            }
        }

        void StepPhase()
        {
            double elapsed = simTime - phaseTime;
            switch (phase)
            {
                case Phase.PreServe:
                    // L'adversaire sert tout seul, une fois en place.
                    if (Server == Side.Opponent && elapsed >= config.aiServeDelay
                        && Vector3.Distance(opponent.Position, opponentSpot) <= InPlaceDistance)
                    {
                        StartToss(Side.Opponent);
                    }

                    break;

                case Phase.Toss:
                    if (Server == Side.Opponent && simTime >= tossTime + config.serveIdealDelay)
                    {
                        OpponentServe();
                    }
                    else if (ball.Velocity.y < 0f && ball.Position.y < config.tossMinHeight)
                    {
                        // Lancer non frappé : ce n'est pas une faute, on recommence.
                        ballActive = false;
                        phase = Phase.PreServe;
                        phaseTime = simTime;
                        if (Server == Side.Player)
                        {
                            Announce(MessageTone.Warning, "LANCER À REFAIRE", "Frappe la balle quand elle est en haut", 1.6f);
                        }
                    }

                    break;

                case Phase.Rally:
                    if (ballDead && secondServePending && elapsed >= FaultPause)
                    {
                        BeginServe();
                    }

                    break;

                case Phase.PointOver:
                    if (elapsed >= config.pointPause)
                    {
                        if (score.IsOver)
                        {
                            phase = Phase.SetOver;
                            phaseTime = simTime;
                            ballActive = false;
                            SetsPlayed++;
                        }
                        else
                        {
                            BeginServe();
                        }
                    }

                    break;

                case Phase.SetOver:
                    // En démonstration, le set suivant repart tout seul.
                    if (AutoSwing && elapsed >= 4.0)
                    {
                        StartSet();
                    }

                    break;
            }
        }

        // ---------------------------------------------------------------- Service

        void OnTossGesture()
        {
            if (phase == Phase.SetOver)
            {
                StartSet();
                return;
            }

            if (PlayerReadyToToss)
            {
                StartToss(Side.Player);
            }
        }

        void StartToss(Side server)
        {
            Vector3 serverPosition = server == Side.Player ? playerPosition : opponent.Position;
            float towardNet = -(int)server;

            ball = new BallState
            {
                Position = serverPosition + new Vector3(0.25f * towardNet, config.tossReleaseHeight, 0.35f * towardNet),
                Velocity = new Vector3(0f, config.tossSpeed, 0f),
            };

            ballActive = true;
            ballDead = false;
            lastHitter = server;
            bounceCount = 0;
            launchTime = simTime;
            tossTime = simTime;
            phase = Phase.Toss;
            phaseTime = simTime;
            TossStarted?.Invoke(server == Side.Player);
        }

        /// <summary>Le serveur frappe la balle lancée vers le carré de service opposé.</summary>
        /// <param name="timing">Écart au timing idéal, de -1 à +1.</param>
        /// <param name="aimErrorDegrees">Erreur de visée aléatoire, en degrés (adversaire).</param>
        void ServeBall(Side server, float speed, float timing, float aimErrorDegrees)
        {
            Side receiver = Other(server);

            // Un mauvais timing décale le service sur le côté et l'allonge.
            float depth = court.ServiceLine - config.serveDepthMargin + timing * timing * config.serveLongError;
            var target = new Vector3(
                serveBoxSide * court.HalfWidth * 0.5f + timing * config.serveAimSpread,
                0f,
                (int)receiver * depth);

            float topspin = 600f * 2f * Mathf.PI / 60f;
            ShotSolution solution = ShotSolver.Solve(ball.Position, target, speed, topspin, config.serveNetMargin, ballSettings, court);

            if (aimErrorDegrees > 0f)
            {
                Vector3 horizontal = new Vector3(solution.Velocity.x, 0f, solution.Velocity.z);
                Vector3 pitchAxis = Vector3.Cross(Vector3.up, horizontal.normalized);
                Quaternion error = Quaternion.AngleAxis(Random.Range(-aimErrorDegrees, aimErrorDegrees), Vector3.up)
                    * Quaternion.AngleAxis(Random.Range(-aimErrorDegrees, aimErrorDegrees) * 0.7f, pitchAxis);
                solution.Velocity = error * solution.Velocity;
                solution.Spin = error * solution.Spin;
            }

            ball.Velocity = solution.Velocity;
            ball.Spin = solution.Spin;
            LastShotSpeed = solution.Speed;

            isServe = true;
            phase = Phase.Rally;
            phaseTime = simTime;
            OnBallHit(server);

            bool byPlayer = server == Side.Player;
            BallHit?.Invoke(new HitInfo
            {
                ByPlayer = byPlayer,
                IsServe = true,
                Power = byPlayer ? Mathf.Clamp01(lastHitSwing.Power) : Mathf.InverseLerp(config.serveSpeedMin, config.serveSpeedMax, speed),
                Spin = 0f,
                Speed = solution.Speed,
                Point = ball.Position,
                Perfect = byPlayer && Mathf.Abs(LastTimingSeconds) < PerfectTiming,
            });
        }

        void OpponentServe()
        {
            AiLevel level = config.GetAiLevel(Difficulty);

            // Deuxième balle : plus lente et plus sûre.
            bool second = faultCount > 0;
            float speed = level.serveSpeed * (second ? 0.8f : 1f);
            float error = level.aimErrorDegrees * (second ? 0.12f : 0.4f);

            opponentSwingTime = Time.unscaledTime;
            ServeBall(Side.Opponent, speed, Random.Range(-0.3f, 0.3f), error);
        }

        void ServiceFault(string detail)
        {
            Side server = lastHitter;
            isServe = false;
            faultCount++;
            if (server == Side.Opponent)
            {
                OpponentFaults++;
            }

            if (faultCount >= 2)
            {
                AwardPoint(Other(server), PointReason.DoubleFault,
                    server == Side.Player ? "Double faute." : "Double faute de l'adversaire.");
                return;
            }

            Announce(MessageTone.Bad, "FAUTE", Trim(detail) + " · 2e service", 1.4f);
            ServiceFaulted?.Invoke();
            ballDead = true;
            hasContact = false;
            secondServePending = true;
            phaseTime = simTime;
            opponent.StopTracking();
        }

        // ---------------------------------------------------------------- Balle

        void StepBall()
        {
            if (!ballActive)
            {
                return;
            }

            float impactSpeed = Mathf.Max(0f, -ball.Velocity.y);
            BallEvent ballEvent = BallPhysics.Step(ref ball, BallPhysics.TimeStep, ballSettings, court);
            if (phase == Phase.Toss || ballDead)
            {
                // Balle déjà jouée : ses rebonds ne comptent plus, mais ils s'entendent encore.
                if (ballDead && ballEvent == BallEvent.Bounce && impactSpeed > 0.8f)
                {
                    Bounced?.Invoke(new BounceInfo { Point = ball.Position, ImpactSpeed = impactSpeed });
                }

                return;
            }

            if (ballEvent == BallEvent.Bounce)
            {
                OnBounce(impactSpeed);
            }
            else if (ballEvent == BallEvent.Net)
            {
                NetTouched?.Invoke(ball.Position);
                if (isServe)
                {
                    ServiceFault(lastHitter == Side.Player ? "Ton service reste dans le filet." : "Service de l'adversaire dans le filet.");
                }
                else
                {
                    if (lastHitter == Side.Opponent)
                    {
                        OpponentFaults++;
                    }

                    AwardPoint(Other(lastHitter), PointReason.Net, lastHitter == Side.Player
                        ? "Ta balle reste dans le filet."
                        : "L'adversaire met la balle dans le filet.");
                }
            }

            bool lost = Mathf.Abs(ball.Position.x) > 25f || Mathf.Abs(ball.Position.z) > 35f
                || simTime - launchTime > MaxFlightTime;
            if (lost && !ballDead)
            {
                if (isServe)
                {
                    ServiceFault("Service perdu.");
                }
                else
                {
                    AwardPoint(Other(lastHitter), PointReason.Other, "Balle perdue.");
                }
            }
        }

        void OnBounce(float impactSpeed)
        {
            bounceCount++;
            Side receiver = Other(lastHitter);
            var bounce = new BounceInfo
            {
                Point = ball.Position,
                ImpactSpeed = impactSpeed,
                Live = true,
                First = bounceCount == 1,
                IsServe = isServe,
                ByPlayer = lastHitter == Side.Player,
            };

            if (bounceCount == 1)
            {
                bool wasServe = isServe;
                bool inside = wasServe
                    ? court.IsInServiceBox(ball.Position.x, ball.Position.z, (int)receiver, serveBoxSide)
                    : ball.Position.z * (int)receiver > 0f && court.IsInside(ball.Position.x, ball.Position.z);

                bounce.Inside = inside;
                Bounced?.Invoke(bounce);

                if (inside)
                {
                    isServe = false;
                    return;
                }

                if (wasServe)
                {
                    ServiceFault(lastHitter == Side.Player ? "Ton service est dehors." : "Service de l'adversaire dehors.");
                    return;
                }

                string detail;
                if (ball.Position.z * (int)receiver <= 0f)
                {
                    detail = lastHitter == Side.Player ? "Ta balle retombe dans ton camp." : "La balle de l'adversaire retombe dans son camp.";
                }
                else
                {
                    string distance = DistanceOutside(ball.Position).ToString("F1") + " m";
                    detail = lastHitter == Side.Player ? "Ta balle est dehors de " + distance + "." : "L'adversaire sort la balle de " + distance + ".";
                }

                if (lastHitter == Side.Opponent)
                {
                    OpponentFaults++;
                }

                AwardPoint(receiver, PointReason.Out, detail);
            }
            else
            {
                // Deuxième rebond : celui qui devait jouer la balle perd le point.
                Bounced?.Invoke(bounce);
                AwardPoint(lastHitter, PointReason.DoubleBounce, receiver == Side.Player
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

        void AwardPoint(Side winner, PointReason reason, string detail)
        {
            ScoreEvent scoreEvent = score.PointWonBy(Index(winner));
            AnnouncePoint(winner == Side.Player, reason, scoreEvent, detail);
            LongestRally = Mathf.Max(LongestRally, rallyShots);
            faultCount = 0;
            isServe = false;

            bool won = winner == Side.Player;
            ballDead = true;
            hasContact = false;
            secondServePending = false;
            phase = Phase.PointOver;
            phaseTime = simTime;
            playerTarget = HomePosition(Side.Player);
            opponentSpot = HomePosition(Side.Opponent);
            opponent.StopTracking();

            PointEnded?.Invoke(new PointInfo { WonByPlayer = won, Result = scoreEvent, Reason = reason });
        }

        /// <summary>
        /// Annonces de fin de point : d'abord ce qui s'est passé, puis, s'il y a lieu, le jeu, le
        /// set, l'égalité ou l'avantage.
        /// </summary>
        void AnnouncePoint(bool won, PointReason reason, ScoreEvent scoreEvent, string detail)
        {
            string verdict = won ? "Point pour toi" : "Point perdu";
            bool followed = scoreEvent != ScoreEvent.Point || score.IsDeuce || score.Advantage >= 0;

            string title;
            string subtitle;
            MessageTone tone = MessageTone.Bad;
            switch (reason)
            {
                case PointReason.Out:
                    title = "DEHORS";
                    subtitle = verdict + " · " + Trim(detail);
                    break;
                case PointReason.Net:
                    title = "FILET";
                    subtitle = verdict + " · " + Trim(detail);
                    break;
                case PointReason.DoubleFault:
                    title = "DOUBLE FAUTE";
                    subtitle = verdict;
                    break;
                default:
                    title = won ? "POINT POUR TOI" : "POINT PERDU";
                    subtitle = Trim(detail);
                    tone = won ? MessageTone.Good : MessageTone.Neutral;
                    break;
            }

            if (!followed)
            {
                subtitle += " · " + score.Call().Replace(" - ", " – ");
            }

            Announce(tone, title, subtitle, followed ? 1.1f : 1.6f);

            string games = score.Games(0) + " – " + score.Games(1);
            if (scoreEvent == ScoreEvent.Set)
            {
                Announce(MessageTone.Accent, "SET",
                    won ? "Tu gagnes " + games : "L'adversaire gagne " + score.Games(1) + " – " + score.Games(0), 3.5f, -1, true);
            }
            else if (scoreEvent == ScoreEvent.Game)
            {
                Announce(MessageTone.Accent, "JEU", "Toi " + games + " Adversaire", 2.2f);
            }
            else if (score.IsDeuce)
            {
                Announce(MessageTone.Neutral, "ÉGALITÉ", string.Empty, 1.6f);
            }
            else if (score.Advantage >= 0)
            {
                Announce(MessageTone.Neutral, "AVANTAGE", score.Advantage == 0 ? "Toi" : "Adversaire", 1.6f, score.Advantage);
            }
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
            opponentSpot = HomePosition(Side.Opponent);

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
                else if (!isServe)
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
        /// son camp (dans le carré de service s'il s'agit d'un service), quand la balle redescend à
        /// la hauteur de frappe. Renvoie false si la balle n'est pas jouable.
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
                    bool inside = isServe
                        ? court.IsInServiceBox(state.Position.x, state.Position.z, (int)receiver, serveBoxSide)
                        : state.Position.z * (int)receiver > 0f && court.IsInside(state.Position.x, state.Position.z);
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
            switch (phase)
            {
                case Phase.SetOver:
                    StartSet();
                    return;

                case Phase.Toss:
                    if (Server == Side.Player)
                    {
                        PlayerServe(swing);
                    }

                    return;

                case Phase.PreServe:
                    if (Server == Side.Player)
                    {
                        Announce(MessageTone.Warning, "LANCE D'ABORD LA BALLE",
                            "Baisse le téléphone, puis lève-le vers le haut (ou flèche haut)", 1.8f);
                    }

                    return;

                case Phase.Rally:
                    PlayerRallyShot(swing);
                    return;
            }
        }

        void PlayerServe(SwingEvent swing)
        {
            double swingTime = simTime - swing.AgeSeconds;
            float timing = (float)(swingTime - (tossTime + config.serveIdealDelay));
            if (Mathf.Abs(timing) > config.serveTimingWindow)
            {
                Whiff((timing < 0f ? "Trop tôt de " : "Trop tard de ")
                    + Mathf.RoundToInt(Mathf.Abs(timing) * 1000f) + " ms.");
                return;
            }

            RecordPlayerHit(swing, timing, config.serveTimingWindow, true);
            float speed = Mathf.Lerp(config.serveSpeedMin, config.serveSpeedMax, Mathf.Clamp01(swing.Power));
            ServeBall(Side.Player, speed, lastHitTimingNormalized, 0f);
            Vibrate(swing.Power);
        }

        void PlayerRallyShot(SwingEvent swing)
        {
            if (!ballActive || ballDead || lastHitter != Side.Opponent || !hasContact)
            {
                Whiff("Pas de balle à jouer.");
                return;
            }

            double swingTime = simTime - swing.AgeSeconds;
            float timing = (float)(swingTime - contactTime);
            if (Mathf.Abs(timing) > config.timingWindow)
            {
                Whiff((timing < 0f ? "Trop tôt de " : "Trop tard de ")
                    + Mathf.RoundToInt(Mathf.Abs(timing) * 1000f) + " ms.");
                return;
            }

            Vector3 hand = playerPosition + new Vector3(hittingSide * config.playerSideOffset, 0f, 0f);
            float distance = Vector2.Distance(new Vector2(hand.x, hand.z), new Vector2(ball.Position.x, ball.Position.z));
            if (distance > config.playerReach)
            {
                Whiff("Balle hors de portée (" + distance.ToString("F1") + " m).");
                return;
            }

            RecordPlayerHit(swing, timing, config.timingWindow, false);
            isServe = false;
            ApplyPlayerShot(swing);
            OnBallHit(Side.Player);
            BallHit?.Invoke(new HitInfo
            {
                ByPlayer = true,
                Power = Mathf.Clamp01(swing.Power),
                Spin = swing.Spin,
                Speed = LastShotSpeed,
                Point = ball.Position,
                Perfect = Mathf.Abs(timing) < PerfectTiming,
            });
            Vibrate(swing.Power);
        }

        void RecordPlayerHit(SwingEvent swing, float timing, float window, bool serve)
        {
            lastHitSwing = swing;
            lastHitSwingId = swing.Id;
            lastHitTime = simTime;
            lastHitTimingNormalized = Mathf.Clamp(timing / window, -1f, 1f);
            LastTimingSeconds = timing;
            LastTimingWindow = window;
            LastHitWasServe = serve;
            HasLastHit = true;
        }

        void Vibrate(float power)
        {
            if (link != null)
            {
                link.SendVibration(Mathf.RoundToInt(Mathf.Lerp(25f, 70f, power)));
            }
        }

        /// <summary>La fin du geste a précisé l'effet (slice) : la trajectoire est recalculée en vol.</summary>
        void OnSwingRefined(SwingEvent swing)
        {
            bool stillFlying = phase == Phase.Rally && ballActive && !ballDead && lastHitter == Side.Player
                && bounceCount == 0 && !LastHitWasServe;
            if (!stillFlying || swing.Id != lastHitSwingId || simTime - lastHitTime > RefineWindow || ball.Position.z > -0.5f)
            {
                return;
            }

            lastHitSwing = swing;
            ApplyPlayerShot(swing);

            // La trajectoire a changé : l'adversaire relit la balle, sans compter une frappe de plus.
            rallyShots--;
            OnBallHit(Side.Player);
            ShotRefined?.Invoke(swing.Spin);
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
        /// Mode démonstration (touche T) : le jeu sert et frappe tout seul, avec un timing, une
        /// puissance et un effet tirés au hasard. Sert à voir un match sans téléphone.
        /// </summary>
        void UpdateAutoPlay()
        {
            if (PlayerReadyToToss && simTime - phaseTime >= 1.0)
            {
                StartToss(Side.Player);
                autoSwingPlanned = false;
                return;
            }

            bool serving = phase == Phase.Toss && Server == Side.Player;
            bool rallying = phase == Phase.Rally && ballActive && !ballDead && lastHitter == Side.Opponent && hasContact;
            if (!serving && !rallying)
            {
                autoSwingPlanned = false;
                return;
            }

            double ideal = serving ? tossTime + config.serveIdealDelay : contactTime;
            float window = serving ? config.serveTimingWindow : config.timingWindow;
            if (!autoSwingPlanned)
            {
                // Timing le plus souvent bon, parfois franchement décalé, comme un vrai joueur.
                autoSwingPlanned = true;
                autoSwingOffset = (Random.Range(-1.1f, 1.1f) + Random.Range(-1.1f, 1.1f)) * 0.5f * window;
            }

            if (simTime < ideal + autoSwingOffset)
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
                Spin = serving ? 0f : spins[Random.Range(0, spins.Length)],
                PeakTimeSeconds = simTime,
                AgeSeconds = 0f,
            });
        }

        // ---------------------------------------------------------------- Adversaire

        void StepOpponent(float deltaTime)
        {
            AiLevel level = config.GetAiLevel(Difficulty);
            opponent.Move(simTime, deltaTime, opponentSpot, config.playerMoveSpeed, config.playerSideOffset, level);

            bool ballToPlay = phase == Phase.Rally && ballActive && !ballDead && lastHitter == Side.Player
                && hasContact && bounceCount == 1;
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
            BallHit?.Invoke(new HitInfo
            {
                ByPlayer = false,
                Power = Mathf.InverseLerp(config.shotSpeedMin, config.shotSpeedMax, shot.Solution.Speed),
                Spin = SpinSign(shot.Solution),
                Speed = shot.Solution.Speed,
                Point = ball.Position,
            });
        }

        /// <summary>Effet d'une trajectoire, de -1 (slice) à +1 (lift), d'après le sens de rotation de la balle.</summary>
        float SpinSign(ShotSolution solution)
        {
            var horizontal = new Vector3(solution.Velocity.x, 0f, solution.Velocity.z);
            if (horizontal.sqrMagnitude < 1e-4f)
            {
                return 0f;
            }

            // Une balle liftée tourne autour de l'axe « haut × direction ».
            float rate = Vector3.Dot(solution.Spin, Vector3.Cross(Vector3.up, horizontal.normalized));
            float full = Mathf.Max(config.liftRpmMax, 1f) * 2f * Mathf.PI / 60f;
            return Mathf.Clamp(rate / full, -1f, 1f);
        }

        // ---------------------------------------------------------------- Annonces

        void Announce(MessageTone tone, string title, string subtitle, float duration, int side = -1, bool persistent = false)
        {
            Announced?.Invoke(new Announcement
            {
                Title = title,
                Subtitle = subtitle,
                Tone = tone,
                Duration = duration,
                Side = side,
                Persistent = persistent,
            });
        }

        /// <summary>Swing du joueur qui ne touche pas de balle.</summary>
        void Whiff(string detail)
        {
            Whiffed?.Invoke(detail);
        }

        static string Trim(string sentence)
        {
            return sentence.TrimEnd('.', ' ');
        }
    }
}
