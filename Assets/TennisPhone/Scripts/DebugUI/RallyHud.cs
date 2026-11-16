using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Affichage du match : tableau de score, consignes de service, annonces (point, jeu, set,
    /// faute), barre de timing du swing et état de la connexion du téléphone.
    /// Suit aussi le joueur avec la caméra.
    /// </summary>
    [RequireComponent(typeof(RallyController))]
    public sealed class RallyHud : MonoBehaviour
    {
        const float ReferenceHeight = 720f;
        const float OutcomeDuration = 2.6f;

        static readonly Color PanelColor = new Color(0.06f, 0.09f, 0.13f, 0.82f);
        static readonly Color AccentColor = new Color(1f, 0.85f, 0.3f);
        static readonly Color GoodColor = new Color(0.45f, 0.95f, 0.55f);
        static readonly Color BadColor = new Color(1f, 0.5f, 0.4f);
        static readonly Color NeutralColor = new Color(1f, 0.78f, 0.42f);

        RallyController rally;
        RacketInput input;
        PhoneLink link;
        Texture2D controllerQr;
        string controllerQrUrl;
        GUIStyle titleStyle;
        GUIStyle textStyle;
        GUIStyle hugeStyle;
        GUIStyle centeredStyle;
        GUIStyle rightStyle;
        GUIStyle scoreNameStyle;
        GUIStyle scoreValueStyle;
        GUIStyle promptStyle;

        void Awake()
        {
            rally = GetComponent<RallyController>();
            input = GetComponent<RacketInput>();
            link = GetComponent<PhoneLink>();
        }

        void OnGUI()
        {
            if (rally.Score == null)
            {
                return;
            }

            EnsureStyles();

            float scale = Screen.height / ReferenceHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;

            DrawScoreboard(new Rect(width * 0.5f - 190f, 14f, 380f, 84f));
            DrawPrompt(new Rect(width * 0.5f - 330f, 104f, 660f, 34f));
            DrawOutcome(new Rect(width * 0.5f - 300f, 150f, 600f, 104f));
            DrawStatus(new Rect(14f, 14f, 250f, 206f));
            DrawInfo(new Rect(width - 254f, 14f, 240f, 104f));
            DrawTiming(new Rect(width * 0.5f - 260f, ReferenceHeight - 96f, 520f, 88f));
        }

        // ---------------------------------------------------------------- Score

        void DrawScoreboard(Rect area)
        {
            Fill(area, PanelColor);
            MatchScore score = rally.Score;
            DrawScoreRow(new Rect(area.x, area.y + 4f, area.width, 38f), "Toi", 0, score);
            DrawScoreRow(new Rect(area.x, area.y + 42f, area.width, 38f), "Adversaire", 1, score);
        }

        void DrawScoreRow(Rect row, string name, int side, MatchScore score)
        {
            // Pastille du serveur.
            if (score.Server == side && !score.IsOver)
            {
                Fill(new Rect(row.x + 14f, row.center.y - 6f, 12f, 12f), AccentColor);
            }

            GUI.Label(new Rect(row.x + 38f, row.y, 170f, row.height), name, scoreNameStyle);

            // Jeux gagnés, puis points du jeu en cours dans un cadre plus clair.
            GUI.Label(new Rect(row.x + 214f, row.y, 60f, row.height), score.Games(side).ToString(), scoreValueStyle);
            var pointsBox = new Rect(row.x + 286f, row.y + 3f, 80f, row.height - 6f);
            Fill(pointsBox, new Color(1f, 1f, 1f, 0.12f));
            GUI.Label(pointsBox, score.IsOver ? "—" : score.PointText(side), scoreValueStyle);
        }

        void DrawPrompt(Rect area)
        {
            string prompt = PromptText();
            if (string.IsNullOrEmpty(prompt))
            {
                return;
            }

            Fill(area, new Color(PanelColor.r, PanelColor.g, PanelColor.b, 0.7f));
            GUI.Label(area, prompt, promptStyle);
        }

        string PromptText()
        {
            switch (rally.CurrentPhase)
            {
                case RallyController.Phase.PreServe:
                    if (!rally.PlayerServing)
                    {
                        return "L'adversaire sert" + (rally.FaultCount > 0 ? " sa deuxième balle" : string.Empty) + "…";
                    }

                    string ball = rally.FaultCount > 0 ? "Deuxième balle : " : "À toi de servir : ";
                    if (!rally.PlayerReadyToToss)
                    {
                        return ball + "en place…";
                    }

                    if (input.HasPhone && !input.IsTossArmed)
                    {
                        return ball + "baisse le téléphone, puis lève-le pour lancer la balle";
                    }

                    return ball + (input.HasPhone ? "lève le téléphone pour lancer la balle" : "flèche haut pour lancer la balle");

                case RallyController.Phase.Toss:
                    return rally.PlayerServing ? "Frappe quand la balle est en haut !" : string.Empty;

                case RallyController.Phase.SetOver:
                    return "Fais un swing (ou appuie sur Entrée) pour rejouer un set";

                default:
                    return string.Empty;
            }
        }

        // ---------------------------------------------------------------- Annonces

        void DrawOutcome(Rect area)
        {
            bool setOver = rally.CurrentPhase == RallyController.Phase.SetOver;
            float age = Time.unscaledTime - rally.LastOutcomeTime;
            if (rally.LastOutcome == RallyController.Outcome.None || string.IsNullOrEmpty(rally.LastOutcomeTitle)
                || (!setOver && age > OutcomeDuration))
            {
                return;
            }

            Color color;
            switch (rally.LastOutcome)
            {
                case RallyController.Outcome.PointWon:
                    color = GoodColor;
                    break;
                case RallyController.Outcome.PointLost:
                case RallyController.Outcome.Fault:
                    color = BadColor;
                    break;
                default:
                    color = NeutralColor;
                    break;
            }

            float alpha = setOver ? 1f : Mathf.Clamp01((OutcomeDuration - age) / 0.5f);
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            Fill(area, PanelColor);
            hugeStyle.normal.textColor = color;
            GUI.Label(new Rect(area.x, area.y + 4f, area.width, 56f), rally.LastOutcomeTitle, hugeStyle);
            GUI.Label(new Rect(area.x + 10f, area.y + 62f, area.width - 20f, 36f), rally.LastOutcomeDetail, centeredStyle);
            GUI.color = previous;
        }

        // ---------------------------------------------------------------- Panneaux latéraux

        void DrawStatus(Rect area)
        {
            bool connected = link != null && link.ClientCount > 0;
            bool hasError = link != null && !string.IsNullOrEmpty(link.Error);
            if (connected && !hasError)
            {
                area.height = 84f;
            }

            Fill(area, PanelColor);
            GUILayout.BeginArea(new Rect(area.x + 12f, area.y + 8f, area.width - 24f, area.height - 16f));

            if (hasError)
            {
                GUILayout.Label("Téléphone", titleStyle);
                GUILayout.Label(link.Error, textStyle);
            }
            else if (connected)
            {
                GUILayout.Label("Téléphone connecté", titleStyle);
                GUILayout.Label(link.SampleRateHz.ToString("F0") + " mesures/s · R : recentrer la raquette", textStyle);
            }
            else
            {
                GUILayout.Label("Aucun téléphone", titleStyle);
                if (link != null)
                {
                    DrawQr(link.ControllerUrl, 96f);
                }

                GUILayout.Label("Souris : clic gauche coup droit, clic droit revers. Flèche haut : lancer.", textStyle);
            }

            GUILayout.EndArea();
        }

        void DrawInfo(Rect area)
        {
            Fill(area, PanelColor);
            GUILayout.BeginArea(new Rect(area.x + 12f, area.y + 8f, area.width - 24f, area.height - 16f));
            GUILayout.Label("Niveau " + DifficultyName(rally.Difficulty), titleStyle);
            GUILayout.Label("D : changer · T : démonstration" + (rally.AutoSwing ? " (activée)" : string.Empty), textStyle);
            GUILayout.Label("Échange : " + rally.RallyShots + " frappes", textStyle);
            GUILayout.EndArea();
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

        // ---------------------------------------------------------------- Timing

        void DrawTiming(Rect area)
        {
            Fill(area, PanelColor);
            GUILayout.BeginArea(new Rect(area.x + 14f, area.y + 6f, area.width - 28f, area.height - 12f));

            if (rally.HasLastHit)
            {
                SwingEvent swing = rally.LastSwing;
                int milliseconds = Mathf.RoundToInt(rally.LastTimingSeconds * 1000f);
                string timing = Mathf.Abs(milliseconds) < 25
                    ? "parfait"
                    : (milliseconds < 0 ? "tôt de " + -milliseconds + " ms" : "tard de " + milliseconds + " ms");
                string speed = Mathf.RoundToInt(rally.LastShotSpeed * 3.6f) + " km/h";

                if (rally.LastHitWasServe)
                {
                    GUILayout.Label("Service · " + speed + " · timing " + timing, centeredStyle);
                }
                else
                {
                    string spin = swing.Spin > 0f ? "lifté" : (swing.Spin < 0f ? "slicé" : "à plat");
                    GUILayout.Label((swing.Side == SwingSide.Forehand ? "Coup droit " : "Revers ") + spin + " · "
                        + speed + " · timing " + timing, centeredStyle);
                }
            }
            else
            {
                GUILayout.Label("Frappe la balle après son rebond, quand elle arrive à ta hauteur.", centeredStyle);
            }

            // Barre de timing : centre = instant idéal, bords = limites de la fenêtre.
            Rect track = GUILayoutUtility.GetRect(10f, 16f, GUILayout.ExpandWidth(true));
            Fill(track, new Color(1f, 1f, 1f, 0.14f));
            Fill(new Rect(track.center.x - track.width * 0.07f, track.y, track.width * 0.14f, track.height),
                new Color(0.3f, 0.8f, 0.4f, 0.5f));
            Fill(new Rect(track.center.x - 1f, track.y - 3f, 2f, track.height + 6f), Color.white);

            if (rally.HasLastHit)
            {
                float position = Mathf.Clamp(rally.LastTimingSeconds / rally.LastTimingWindow, -1f, 1f);
                Fill(new Rect(track.center.x + position * track.width * 0.5f - 3f, track.y - 4f, 6f, track.height + 8f), AccentColor);
            }

            float remaining = rally.TimeToIdeal;
            float window = rally.ActiveTimingWindow;
            if (!float.IsNaN(remaining) && Mathf.Abs(remaining) <= window * 3f)
            {
                // Curseur de la balle en approche : il traverse la barre de gauche à droite.
                float approach = Mathf.Clamp(-remaining / window, -1f, 1f);
                Fill(new Rect(track.center.x + approach * track.width * 0.5f - 2f, track.y, 4f, track.height),
                    new Color(0.85f, 0.95f, 0.2f));
            }

            Rect legend = GUILayoutUtility.GetRect(10f, 20f, GUILayout.ExpandWidth(true));
            GUI.Label(new Rect(legend.x, legend.y, 160f, legend.height), "tôt · croisé", textStyle);
            GUI.Label(new Rect(legend.center.x - 40f, legend.y, 80f, legend.height), "idéal", centeredStyle);
            GUI.Label(new Rect(legend.xMax - 160f, legend.y, 160f, legend.height), "tard · long de ligne", rightStyle);
            GUILayout.EndArea();
        }

        void LateUpdate()
        {
            Camera view = Camera.main;
            if (view == null || rally == null)
            {
                return;
            }

            // La caméra accompagne un peu le joueur sur la largeur.
            Vector3 position = view.transform.position;
            position.x = Mathf.Lerp(position.x, rally.PlayerPosition.x * 0.45f, 1f - Mathf.Exp(-4f * Time.deltaTime));
            view.transform.position = position;
        }

        // ---------------------------------------------------------------- Outils

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

            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            hugeStyle = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            centeredStyle = new GUIStyle(textStyle) { fontSize = 15, alignment = TextAnchor.MiddleCenter };
            rightStyle = new GUIStyle(textStyle) { alignment = TextAnchor.UpperRight };
            scoreNameStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            scoreValueStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            promptStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            promptStyle.normal.textColor = AccentColor;
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
