using System.Collections.Generic;
using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Affichage du match, en panneaux « ardoise » (ART_DIRECTION.md, section 7) : ardoise de
    /// score en haut au centre, annonces balle morte, barre de timing du swing, consignes de
    /// service, état du téléphone et aide. Lit l'état de RallyController et s'abonne à ses
    /// événements ; ne décide de rien.
    /// </summary>
    [RequireComponent(typeof(RallyController))]
    public sealed class RallyHud : MonoBehaviour
    {
        const float ReferenceHeight = 720f;
        const float Margin = 24f;
        // Les annonces passent sous l'ardoise de score et son étiquette, au centre de l'écran.
        const float MessageTop = 208f;
        const float MaxSubtitleWidth = 700f;
        const float MessagePop = 0.12f;
        const float MessageFade = 0.3f;
        const float GameFlashDuration = 1f;
        const float ShakeDuration = 0.2f;

        enum Verdict
        {
            None,
            Perfect,
            Early,
            Late,
            Whiff,
        }

        RallyController rally;
        RacketInput input;
        PhoneLink link;
        Texture2D controllerQr;
        string controllerQrUrl;

        // Annonces : une à la fois, les suivantes attendent leur tour.
        readonly List<Announcement> queue = new List<Announcement>();
        Announcement message;
        bool hasMessage;
        float messageStart;
        float messageEnd;

        // Dernier swing du joueur, rappelé dans la barre de timing jusqu'au suivant.
        Verdict verdict;
        string swingSummary = string.Empty;
        float whiffTime = -10f;

        int gameFlashSide = -1;
        float gameFlashTime = -10f;

        void Awake()
        {
            rally = GetComponent<RallyController>();
            input = GetComponent<RacketInput>();
            link = GetComponent<PhoneLink>();
        }

        void OnEnable()
        {
            rally.Announced += OnAnnounced;
            rally.BallHit += OnBallHit;
            rally.ShotRefined += OnShotRefined;
            rally.Whiffed += OnWhiffed;
            rally.PointEnded += OnPointEnded;
        }

        void OnDisable()
        {
            rally.Announced -= OnAnnounced;
            rally.BallHit -= OnBallHit;
            rally.ShotRefined -= OnShotRefined;
            rally.Whiffed -= OnWhiffed;
            rally.PointEnded -= OnPointEnded;
        }

        // ---------------------------------------------------------------- Événements du match

        void OnAnnounced(Announcement announcement)
        {
            // Une annonce qui restait à l'écran (fin de set) cède la place.
            if (hasMessage && message.Persistent)
            {
                hasMessage = false;
            }

            // Même annonce répétée (swings avant le lancer) : on ne l'empile pas.
            Announcement last = queue.Count > 0 ? queue[queue.Count - 1] : message;
            if ((queue.Count > 0 || hasMessage) && last.Title == announcement.Title && last.Subtitle == announcement.Subtitle)
            {
                if (queue.Count == 0)
                {
                    messageEnd = Mathf.Max(messageEnd, Time.unscaledTime + announcement.Duration * 0.6f);
                }

                return;
            }

            queue.Add(announcement);
        }

        void OnBallHit(HitInfo hit)
        {
            if (hit.ByPlayer)
            {
                DescribeLastHit();
            }
        }

        void OnShotRefined(float spin)
        {
            DescribeLastHit();
        }

        void DescribeLastHit()
        {
            int milliseconds = Mathf.RoundToInt(rally.LastTimingSeconds * 1000f);
            bool perfect = Mathf.Abs(rally.LastTimingSeconds) < RallyController.PerfectTiming;
            verdict = perfect ? Verdict.Perfect : (milliseconds < 0 ? Verdict.Early : Verdict.Late);

            SwingEvent swing = rally.LastSwing;
            string shot;
            if (rally.LastHitWasServe)
            {
                shot = "Service";
            }
            else
            {
                string spin = swing.Spin > 0f ? "lifté" : (swing.Spin < 0f ? "slicé" : "à plat");
                shot = (swing.Side == SwingSide.Forehand ? "Coup droit " : "Revers ") + spin;
            }

            swingSummary = shot + " · " + Mathf.RoundToInt(rally.LastShotSpeed * 3.6f) + " km/h";
            if (!perfect)
            {
                swingSummary += " · " + Mathf.Abs(milliseconds) + " ms";
            }
        }

        void OnWhiffed(string detail)
        {
            verdict = Verdict.Whiff;
            swingSummary = detail;
            whiffTime = Time.unscaledTime;
        }

        void OnPointEnded(PointInfo point)
        {
            if (point.Result != ScoreEvent.Point)
            {
                gameFlashSide = point.WonByPlayer ? 0 : 1;
                gameFlashTime = Time.unscaledTime;
            }
        }

        // ---------------------------------------------------------------- Annonces : enchaînement

        bool BallLive => !rally.Paused && (rally.CurrentPhase == RallyController.Phase.Toss
            || (rally.CurrentPhase == RallyController.Phase.Rally && !rally.BallDead));

        void Update()
        {
            float now = Time.unscaledTime;

            // Les annonces couvrent le camp adverse : dès que la balle est en jeu, elles s'effacent.
            if (BallLive)
            {
                queue.Clear();
                if (hasMessage)
                {
                    message.Persistent = false;
                    messageEnd = Mathf.Min(messageEnd, now + MessageFade);
                }
            }

            if (hasMessage && !message.Persistent && now >= messageEnd)
            {
                hasMessage = false;
            }

            if (!hasMessage && queue.Count > 0)
            {
                message = queue[0];
                queue.RemoveAt(0);
                hasMessage = true;
                messageStart = now;
                messageEnd = now + message.Duration;
            }
        }

        // ---------------------------------------------------------------- Dessin

        void OnGUI()
        {
            if (rally.Score == null)
            {
                return;
            }

            float scale = Screen.height / ReferenceHeight;
            Matrix4x4 matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUI.matrix = matrix;
            float width = Screen.width / scale;
            HudSkin.Alpha = 1f;

            DrawScoreboard(new Rect(width * 0.5f - 202f, Margin, 404f, 128f));
            DrawServiceLabel(width * 0.5f, 160f);
            DrawRallyCount();
            DrawPhone(width);
            DrawHelp();
            DrawBottomPanel(new Rect(width * 0.5f - 350f, 600f, 700f, 104f));
            DrawMessage(width, matrix);
            HudSkin.Alpha = 1f;
        }

        // ---------------------------------------------------------------- A. Ardoise de score

        void DrawScoreboard(Rect area)
        {
            HudSkin.Panel(area, HudSkin.Slate, 16f);
            DrawScoreRow(new Rect(area.x + 14f, area.y + 12f, area.width - 28f, 52f), "Toi", 0, Palette.Framboise);
            DrawScoreRow(new Rect(area.x + 14f, area.y + 64f, area.width - 28f, 52f), "Adversaire", 1, Palette.Lavande);
        }

        void DrawScoreRow(Rect row, string name, int side, Color teamColor)
        {
            MatchScore score = rally.Score;
            float middle = row.center.y;

            // Le serveur est donné deux fois : par ce point jaune, et par l'étiquette sous l'ardoise.
            if (score.Server == side && !score.IsOver)
            {
                HudSkin.Disc(new Vector2(row.x + 10f, middle), 16f, Palette.JauneBalle);
            }

            HudSkin.Panel(new Rect(row.x + 28f, middle - 16f, 8f, 32f), teamColor, 4f);
            HudSkin.Label(new Rect(row.x + 46f, row.y, 186f, row.height), name,
                HudSkin.Text(HudFont.SemiBold, 26, TextAnchor.MiddleLeft), Palette.Creme);

            // Cases de largeur fixe : le panneau ne bouge pas quand le score change.
            var gamesBox = new Rect(row.x + 232f, middle - 20f, 52f, 40f);
            bool flash = gameFlashSide == side && Time.unscaledTime - gameFlashTime < GameFlashDuration;
            HudSkin.Panel(gamesBox, flash ? Palette.Moutarde : Palette.Cellule, 8f);
            HudSkin.Label(gamesBox, score.Games(side).ToString(), HudSkin.Text(HudFont.ExtraBold, 30, TextAnchor.MiddleCenter),
                flash ? Palette.Ardoise : Palette.Creme);

            var pointsBox = new Rect(gamesBox.xMax + 8f, middle - 20f, 84f, 40f);
            HudSkin.Panel(pointsBox, Palette.Moutarde, 8f);
            string points = score.IsOver ? "–" : score.PointText(side).TrimEnd('.');
            HudSkin.Label(pointsBox, points, HudSkin.Text(HudFont.ExtraBold, 32, TextAnchor.MiddleCenter), Palette.Ardoise);
        }

        /// <summary>Étiquette sous l'ardoise : quelle balle de service, ou le format du set.</summary>
        void DrawServiceLabel(float centerX, float y)
        {
            string text;
            RallyController.Phase phase = rally.CurrentPhase;
            if (phase == RallyController.Phase.PreServe || phase == RallyController.Phase.Toss)
            {
                string ball = rally.FaultCount > 0 ? "2e service" : "1er service";
                text = rally.PlayerServing ? ball : "L'adversaire sert · " + ball;
            }
            else
            {
                text = "Set en " + rally.Config.gamesToWinSet + " jeux · niveau " + DifficultyName(rally.Difficulty);
            }

            GUIStyle style = HudSkin.Text(HudFont.Medium, 20, TextAnchor.MiddleCenter);
            float width = HudSkin.Measure(text, style).x + 40f;
            HudSkin.Pill(new Rect(centerX - width * 0.5f, y, width, 36f), text, style, HudSkin.Slate, Palette.Creme);
        }

        static string DifficultyName(AiDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AiDifficulty.Easy: return "facile";
                case AiDifficulty.Hard: return "difficile";
                default: return "normal";
            }
        }

        // ---------------------------------------------------------------- B. Annonces

        void DrawMessage(float width, Matrix4x4 matrix)
        {
            if (!hasMessage || string.IsNullOrEmpty(message.Title))
            {
                return;
            }

            float now = Time.unscaledTime;
            float pop = Mathf.Clamp01((now - messageStart) / MessagePop);
            float fade = message.Persistent ? 1f : Mathf.Clamp01((messageEnd - now) / MessageFade);
            HudSkin.Alpha = Mathf.Min(pop, fade);

            Color background;
            Color textColor;
            switch (message.Tone)
            {
                case MessageTone.Good:
                    background = Palette.Menthe;
                    textColor = Palette.Ardoise;
                    break;
                case MessageTone.Bad:
                    background = Palette.Rouge;
                    textColor = Palette.Creme;
                    break;
                case MessageTone.Accent:
                    background = Palette.Moutarde;
                    textColor = Palette.Ardoise;
                    break;
                case MessageTone.Warning:
                    background = Palette.Orange;
                    textColor = Palette.Ardoise;
                    break;
                default:
                    background = HudSkin.Slate;
                    textColor = Palette.Creme;
                    break;
            }

            // Le titre tient toujours dans le panneau : un titre long est écrit plus petit.
            int size = message.Persistent ? 72 : 60;
            GUIStyle titleStyle = HudSkin.Text(HudFont.ExtraBold, size, TextAnchor.MiddleCenter);
            float textWidth = HudSkin.Measure(message.Title, titleStyle).x;
            const float maxText = 900f - 96f;
            if (textWidth > maxText)
            {
                size = Mathf.Max(30, Mathf.FloorToInt(size * maxText / textWidth));
                titleStyle = HudSkin.Text(HudFont.ExtraBold, size, TextAnchor.MiddleCenter);
                textWidth = HudSkin.Measure(message.Title, titleStyle).x;
            }

            float panelWidth = Mathf.Clamp(textWidth + 96f, 380f, 900f);
            var panel = new Rect(width * 0.5f - panelWidth * 0.5f, MessageTop, panelWidth, 92f);

            // Apparition : le panneau grandit de 80 % à 100 %.
            float grow = Mathf.Lerp(0.8f, 1f, pop);
            Vector3 pivot = panel.center;
            GUI.matrix = matrix * Matrix4x4.TRS(pivot, Quaternion.identity, new Vector3(grow, grow, 1f))
                * Matrix4x4.Translate(-pivot);

            HudSkin.Panel(panel, background, 46f);
            HudSkin.Label(panel, message.Title, titleStyle, textColor);

            if (!string.IsNullOrEmpty(message.Subtitle))
            {
                GUIStyle subtitleStyle = HudSkin.Text(HudFont.SemiBold, 26, TextAnchor.MiddleLeft);
                float dot = message.Side >= 0 ? 26f : 0f;
                float subtitleWidth = HudSkin.Measure(message.Subtitle, subtitleStyle).x;

                // Un sous-titre long est écrit plus petit : il ne déborde pas sur les panneaux des coins.
                float maxSubtitle = MaxSubtitleWidth - 64f - dot;
                if (subtitleWidth > maxSubtitle)
                {
                    int subtitleSize = Mathf.Max(18, Mathf.FloorToInt(26f * maxSubtitle / subtitleWidth));
                    subtitleStyle = HudSkin.Text(HudFont.SemiBold, subtitleSize, TextAnchor.MiddleLeft);
                    subtitleWidth = HudSkin.Measure(message.Subtitle, subtitleStyle).x;
                }

                float pillWidth = subtitleWidth + dot + 64f;
                var pill = new Rect(width * 0.5f - pillWidth * 0.5f, MessageTop + 100f, pillWidth, 46f);
                HudSkin.Panel(pill, HudSkin.Slate, 23f);
                if (message.Side >= 0)
                {
                    // Pastille du camp nommé.
                    HudSkin.Disc(new Vector2(pill.x + 32f + 8f, pill.center.y), 16f,
                        message.Side == 0 ? Palette.Framboise : Palette.Lavande);
                }

                HudSkin.Label(new Rect(pill.x + 32f + dot, pill.y, pill.width - 64f - dot, pill.height), message.Subtitle,
                    subtitleStyle, Palette.Creme);
            }

            GUI.matrix = matrix;
            HudSkin.Alpha = 1f;
        }

        // ---------------------------------------------------------------- C et D. Panneau du bas

        void DrawBottomPanel(Rect area)
        {
            RallyController.Phase phase = rally.CurrentPhase;
            bool serving = phase == RallyController.Phase.PreServe || phase == RallyController.Phase.Toss;

            // La barre tremble un instant après un swing dans le vide.
            float shake = Time.unscaledTime - whiffTime;
            if (shake >= 0f && shake < ShakeDuration)
            {
                area.x += 6f * Mathf.Sin(shake * 80f) * (1f - shake / ShakeDuration);
            }

            HudSkin.Panel(area, HudSkin.Slate, 16f);

            if (phase == RallyController.Phase.SetOver)
            {
                HudSkin.Label(area, "Fais un swing (ou appuie sur Entrée) pour rejouer un set",
                    HudSkin.Text(HudFont.SemiBold, 24, TextAnchor.MiddleCenter), Palette.Creme);
            }
            else if (serving && rally.PlayerServing)
            {
                DrawServeSteps(area);
            }
            else if (serving)
            {
                HudSkin.Label(area, "L'adversaire sert" + (rally.FaultCount > 0 ? " sa deuxième balle" : string.Empty) + "…",
                    HudSkin.Text(HudFont.SemiBold, 24, TextAnchor.MiddleCenter), Palette.Creme);
            }
            else
            {
                DrawTiming(area);
            }
        }

        /// <summary>Barre de timing : le centre est l'instant idéal, les bords les limites de la fenêtre.</summary>
        void DrawTiming(Rect area)
        {
            DrawSwingSummary(new Rect(area.x + 16f, area.y + 10f, area.width - 32f, 30f));

            var track = new Rect(area.x + 40f, area.y + 48f, 620f, 22f);
            HudSkin.Panel(track, Palette.Piste, 11f);

            // La piste ressort peu sur l'ardoise : ses deux extrémités sont marquées d'un trait.
            HudSkin.Panel(new Rect(track.x, track.y, 3f, track.height), Palette.Creme, 1f);
            HudSkin.Panel(new Rect(track.xMax - 3f, track.y, 3f, track.height), Palette.Creme, 1f);

            float window = rally.ActiveTimingWindow;
            float perfectWidth = track.width * Mathf.Clamp01(RallyController.PerfectTiming / Mathf.Max(window, 0.01f));
            HudSkin.Panel(new Rect(track.center.x - perfectWidth * 0.5f, track.y, perfectWidth, track.height), Palette.Menthe, 3f);
            HudSkin.Panel(new Rect(track.center.x - 1f, track.y - 4f, 2f, track.height + 8f), Palette.Creme, 1f);

            if (rally.HasLastHit && verdict != Verdict.Whiff && verdict != Verdict.None)
            {
                // Repère du dernier swing : il reste en place jusqu'au suivant.
                float position = Mathf.Clamp(rally.LastTimingSeconds / rally.LastTimingWindow, -1f, 1f);
                HudSkin.Marker(track.center.x + position * track.width * 0.5f, track, 6f, 6f, Palette.Moutarde);
            }

            float remaining = rally.TimeToIdeal;
            if (!float.IsNaN(remaining) && Mathf.Abs(remaining) <= window * 3f)
            {
                // Curseur de la balle en approche : il traverse la barre de gauche à droite.
                float approach = Mathf.Clamp(-remaining / window, -1f, 1f);
                HudSkin.Marker(track.center.x + approach * track.width * 0.5f, track, 8f, 4f, Palette.Creme);
            }

            GUIStyle legend = HudSkin.Text(HudFont.Medium, 20, TextAnchor.MiddleLeft);
            var legends = new Rect(track.x, area.y + 76f, track.width, 22f);
            HudSkin.Label(legends, "tôt · croisé", legend, Palette.GrisClair);
            HudSkin.Label(legends, "idéal", HudSkin.Text(HudFont.Medium, 20, TextAnchor.MiddleCenter), Palette.GrisClair);
            HudSkin.Label(legends, "tard · long de ligne", HudSkin.Text(HudFont.Medium, 20, TextAnchor.MiddleRight), Palette.GrisClair);
        }

        /// <summary>Verdict du dernier swing dans une pastille, puis le résumé du coup.</summary>
        void DrawSwingSummary(Rect line)
        {
            if (verdict == Verdict.None)
            {
                HudSkin.Label(line, "Frappe la balle après son rebond, quand elle arrive à ta hauteur.",
                    HudSkin.Text(HudFont.Medium, 20, TextAnchor.MiddleCenter), Palette.Creme);
                return;
            }

            string word;
            Color background;
            Color textColor;
            switch (verdict)
            {
                case Verdict.Perfect:
                    word = "PARFAIT";
                    background = Palette.Menthe;
                    textColor = Palette.Ardoise;
                    break;
                case Verdict.Whiff:
                    word = "DANS LE VIDE";
                    background = Palette.Orange;
                    textColor = Palette.Ardoise;
                    break;
                default:
                    word = verdict == Verdict.Early ? "TÔT" : "TARD";
                    background = Palette.Cellule;
                    textColor = Palette.Creme;
                    break;
            }

            GUIStyle verdictStyle = HudSkin.Text(HudFont.ExtraBold, 22, TextAnchor.MiddleCenter);
            GUIStyle summaryStyle = HudSkin.Text(HudFont.SemiBold, 22, TextAnchor.MiddleLeft);
            float pillWidth = HudSkin.Measure(word, verdictStyle).x + 28f;
            float summaryWidth = HudSkin.Measure(swingSummary, summaryStyle).x;
            float x = line.center.x - (pillWidth + 12f + summaryWidth) * 0.5f;

            HudSkin.Pill(new Rect(x, line.y, pillWidth, line.height), word, verdictStyle, background, textColor);
            HudSkin.Label(new Rect(x + pillWidth + 12f, line.y, summaryWidth + 4f, line.height), swingSummary, summaryStyle, Palette.Creme);
        }

        /// <summary>Service du joueur : deux étapes, puis la jauge de hauteur de la balle lancée.</summary>
        void DrawServeSteps(Rect area)
        {
            bool tossed = rally.CurrentPhase == RallyController.Phase.Toss;

            string first;
            if (!rally.PlayerReadyToToss && !tossed)
            {
                first = "En place…";
            }
            else if (!input.HasPhone)
            {
                first = "Flèche haut : la balle part";
            }
            else if (!input.IsTossArmed && !tossed)
            {
                first = "Baisse le téléphone, puis lève-le : la balle part";
            }
            else
            {
                first = "Lève le téléphone : plus vite, plus haut";
            }

            string second = input.HasPhone ? "Frappe quand elle redescend dans le vert" : "Clic quand elle redescend dans le vert";
            DrawStep(new Rect(area.x + 16f, area.y + 10f, 330f, 56f), "1", first, !tossed);
            DrawStep(new Rect(area.x + 354f, area.y + 10f, 330f, 56f), "2", second, tossed);

            // Jauge de lancer : hauteur de la balle entre la main et le sommet. Le bon moment est
            // celui où la balle redescend dans la zone verte, placée à la hauteur de frappe : plus le
            // lancer est haut, plus elle est loin du sommet et plus la balle met de temps à y revenir.
            GameplayConfig config = rally.Config;
            float span = Mathf.Max(rally.TossApexHeight - config.tossReleaseHeight, 0.1f);
            float zone = Mathf.Clamp((config.serveContactHeight - config.tossReleaseHeight) / span, 0.09f, 0.91f);
            var track = new Rect(area.x + 40f, area.y + 76f, 620f, 14f);
            HudSkin.Panel(track, Palette.Piste, 7f);
            HudSkin.Panel(new Rect(track.x + (zone - 0.09f) * track.width, track.y, track.width * 0.18f, track.height), Palette.Menthe, 3f);
            HudSkin.Panel(new Rect(track.x, track.y, 3f, track.height), Palette.Creme, 1f);
            HudSkin.Panel(new Rect(track.xMax - 3f, track.y, 3f, track.height), Palette.Creme, 1f);

            if (tossed)
            {
                float height = Mathf.Clamp01((rally.Ball.Position.y - config.tossReleaseHeight) / span);
                HudSkin.Marker(track.x + height * track.width, track, 8f, 4f, Palette.Creme);
            }
        }

        void DrawStep(Rect area, string number, string text, bool active)
        {
            float alpha = HudSkin.Alpha;
            if (active)
            {
                HudSkin.Panel(area, Palette.Cellule, 12f);
                HudSkin.Outline(area, Palette.Moutarde, 12f, 2f);
            }
            else
            {
                HudSkin.Alpha = alpha * 0.45f;
            }

            HudSkin.Disc(new Vector2(area.x + 28f, area.center.y), 36f, Palette.Moutarde);
            HudSkin.Label(new Rect(area.x + 10f, area.y, 36f, area.height), number,
                HudSkin.Text(HudFont.ExtraBold, 24, TextAnchor.MiddleCenter), Palette.Ardoise);
            HudSkin.Label(new Rect(area.x + 56f, area.y + 2f, area.width - 64f, area.height - 4f), text,
                HudSkin.Text(HudFont.SemiBold, 20, TextAnchor.MiddleLeft, true), Palette.Creme);
            HudSkin.Alpha = alpha;
        }

        // ---------------------------------------------------------------- E et F. Téléphone, échange, aide

        void DrawPhone(float width)
        {
            bool connected = link != null && link.ClientCount > 0;
            bool hasError = link != null && !string.IsNullOrEmpty(link.Error);
            GUIStyle style = HudSkin.Text(HudFont.Medium, 20, TextAnchor.MiddleLeft);

            if (hasError)
            {
                GUIStyle wrapped = HudSkin.Text(HudFont.Medium, 20, TextAnchor.MiddleLeft, true);
                const float errorWidth = 380f;
                Content.text = link.Error;
                float height = Mathf.Max(36f, wrapped.CalcHeight(Content, errorWidth - 36f) + 16f);
                var area = new Rect(width - Margin - errorWidth, Margin, errorWidth, height);
                HudSkin.Panel(area, Palette.Orange, 18f);
                HudSkin.Label(new Rect(area.x + 18f, area.y, area.width - 36f, area.height), link.Error, wrapped, Palette.Ardoise);
            }
            else if (connected)
            {
                string text = "Raquette connectée · " + link.SampleRateHz.ToString("F0") + " mesures/s";
                float pillWidth = HudSkin.Measure(text, style).x + 58f;
                var area = new Rect(width - Margin - pillWidth, Margin, pillWidth, 36f);
                HudSkin.Panel(area, HudSkin.Slate, 18f);
                HudSkin.Disc(new Vector2(area.x + 22f, area.center.y), 12f, Palette.Menthe);
                HudSkin.Label(new Rect(area.x + 38f, area.y, area.width - 48f, area.height), text, style, Palette.Creme);
            }
            else if (link != null && !string.IsNullOrEmpty(link.ControllerUrl))
            {
                // Carte claire : le code QR reste sombre sur fond blanc pour être lu par l'iPhone.
                var card = new Rect(width - Margin - 260f, Margin, 260f, 316f);
                HudSkin.Panel(card, Palette.Creme, 16f);
                var qr = new Rect(card.x + 20f, card.y + 20f, 220f, 220f);
                HudSkin.Panel(qr, Color.white, 8f);
                if (Event.current.type == EventType.Repaint)
                {
                    GUI.DrawTexture(new Rect(qr.x + 6f, qr.y + 6f, qr.width - 12f, qr.height - 12f), QrTexture(link.ControllerUrl),
                        ScaleMode.ScaleToFit);
                }

                HudSkin.Label(new Rect(card.x + 16f, card.y + 246f, card.width - 32f, 60f), "Scanne pour connecter ta raquette",
                    HudSkin.Text(HudFont.SemiBold, 20, TextAnchor.MiddleCenter, true), Palette.Ardoise);
            }
        }

        /// <summary>Nombre de frappes de l'échange, en bas à gauche.</summary>
        void DrawRallyCount()
        {
            if (rally.RallyShots <= 0)
            {
                return;
            }

            GUIStyle style = HudSkin.Text(HudFont.Medium, 20, TextAnchor.MiddleCenter);
            string text = "Échange : " + rally.RallyShots + (rally.RallyShots > 1 ? " frappes" : " frappe");
            float width = HudSkin.Measure(text, style).x + 40f;
            HudSkin.Pill(new Rect(Margin, ReferenceHeight - Margin - 36f, width, 36f), text, style, HudSkin.Slate, Palette.Creme);
        }

        void DrawHelp()
        {
            GUIStyle style = HudSkin.Text(HudFont.Medium, 18, TextAnchor.UpperLeft, true);
            string text = "D : niveau " + DifficultyName(rally.Difficulty) + " · T : démonstration" + (rally.AutoSwing ? " (activée)" : string.Empty) + "\n"
                + "P : pause · C : caméra " + (rally.WebcamMovement ? "(activée)" : "(coupée)") + "\n"
                + (input.HasPhone
                    ? "R : recentrer la raquette"
                    : "Souris : clic gauche coup droit, clic droit revers. Flèche haut : lancer.");

            // En haut à gauche : le bas de l'écran est pris par la barre de timing.
            const float panelWidth = 330f;
            Content.text = text;
            float height = style.CalcHeight(Content, panelWidth - 32f) + 20f;
            var area = new Rect(Margin, Margin, panelWidth, height);
            HudSkin.Panel(area, Palette.WithAlpha(Palette.Ardoise, 0.72f), 16f);
            HudSkin.Label(new Rect(area.x + 16f, area.y + 10f, area.width - 32f, area.height - 20f), text, style, Palette.GrisClair);
        }

        // ---------------------------------------------------------------- Outils

        static readonly GUIContent Content = new GUIContent();

        Texture2D QrTexture(string url)
        {
            if (controllerQr == null || controllerQrUrl != url)
            {
                if (controllerQr != null)
                {
                    Destroy(controllerQr);
                }

                controllerQr = SensorDebugScreen.BuildQrTexture(url);
                controllerQrUrl = url;
            }

            return controllerQr;
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
