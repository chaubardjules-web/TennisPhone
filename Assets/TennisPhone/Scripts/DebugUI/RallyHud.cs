using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Affichage provisoire de l'étape c : résultat de chaque balle, timing du swing,
    /// vitesse, effet et compteurs. Suit aussi le joueur avec la caméra.
    /// </summary>
    [RequireComponent(typeof(RallyController))]
    public sealed class RallyHud : MonoBehaviour
    {
        const float ReferenceHeight = 720f;
        const float OutcomeDuration = 2.2f;

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

        void Awake()
        {
            rally = GetComponent<RallyController>();
            input = GetComponent<RacketInput>();
            link = GetComponent<PhoneLink>();
        }

        void OnGUI()
        {
            EnsureStyles();

            float scale = Screen.height / ReferenceHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;

            DrawStatus(new Rect(16f, 16f, 300f, 250f));
            DrawCounters(new Rect(width - 256f, 16f, 240f, 150f));
            DrawOutcome(new Rect(width * 0.5f - 280f, 70f, 560f, 110f));
            DrawTiming(new Rect(width * 0.5f - 260f, ReferenceHeight - 96f, 520f, 88f));
        }

        void DrawStatus(Rect area)
        {
            GUI.Box(area, GUIContent.none);
            GUILayout.BeginArea(new Rect(area.x + 12f, area.y + 8f, area.width - 24f, area.height - 16f));
            GUILayout.Label("Entraînement", titleStyle);

            if (link != null && !string.IsNullOrEmpty(link.Error))
            {
                GUILayout.Label(link.Error, textStyle);
            }
            else if (link != null && link.ClientCount > 0)
            {
                GUILayout.Label("Téléphone connecté : " + link.SampleRateHz.ToString("F0") + " Hz", textStyle);
                GUILayout.Label("R : recentrer la raquette", textStyle);
            }
            else if (link != null)
            {
                GUILayout.Label("Aucun téléphone : scanne ce code.", textStyle);
                DrawQr(link.ControllerUrl, 110f);
            }

            GUILayout.Label("Souris : clic gauche coup droit, clic droit revers.\nFlèche haut : lift, flèche bas : slice.\n"
                + "T : démonstration " + (rally.AutoSwing ? "(activée)" : "(le jeu frappe tout seul)"), textStyle);
            GUILayout.EndArea();
        }

        void DrawCounters(Rect area)
        {
            GUI.Box(area, GUIContent.none);
            GUILayout.BeginArea(new Rect(area.x + 12f, area.y + 8f, area.width - 24f, area.height - 16f));
            GUILayout.Label("Balles frappées : " + rally.Hits, titleStyle);
            GUILayout.Label("Dans le court : " + rally.InCount, textStyle);
            GUILayout.Label("Fautes : " + rally.OutCount, textStyle);
            GUILayout.Label("Filet : " + rally.NetCount, textStyle);
            GUILayout.Label("Non jouées : " + rally.MissCount, textStyle);
            GUILayout.EndArea();
        }

        void DrawOutcome(Rect area)
        {
            float age = Time.unscaledTime - rally.LastOutcomeTime;
            if (rally.LastOutcome == RallyController.Outcome.None || age > OutcomeDuration)
            {
                return;
            }

            string title;
            Color color;
            switch (rally.LastOutcome)
            {
                case RallyController.Outcome.In:
                    title = "DANS LE COURT";
                    color = new Color(0.45f, 0.95f, 0.55f);
                    break;
                case RallyController.Outcome.Out:
                    title = "FAUTE";
                    color = new Color(1f, 0.5f, 0.4f);
                    break;
                case RallyController.Outcome.Net:
                    title = "FILET";
                    color = new Color(1f, 0.5f, 0.4f);
                    break;
                case RallyController.Outcome.Missed:
                    title = "MANQUÉE";
                    color = new Color(1f, 0.75f, 0.4f);
                    break;
                default:
                    title = "DANS LE VIDE";
                    color = new Color(1f, 0.75f, 0.4f);
                    break;
            }

            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01((OutcomeDuration - age) / 0.5f));
            GUI.Box(area, GUIContent.none);
            hugeStyle.normal.textColor = color;
            GUI.Label(new Rect(area.x, area.y + 6f, area.width, 58f), title, hugeStyle);
            GUI.Label(new Rect(area.x, area.y + 66f, area.width, 34f), rally.LastOutcomeDetail, centeredStyle);
            GUI.color = previous;
        }

        void DrawTiming(Rect area)
        {
            GUI.Box(area, GUIContent.none);
            GUILayout.BeginArea(new Rect(area.x + 14f, area.y + 6f, area.width - 28f, area.height - 12f));

            if (rally.HasLastHit)
            {
                SwingEvent swing = rally.LastSwing;
                int milliseconds = Mathf.RoundToInt(rally.LastTimingSeconds * 1000f);
                string timing = Mathf.Abs(milliseconds) < 25
                    ? "parfait"
                    : (milliseconds < 0 ? "tôt de " + -milliseconds + " ms (croisé)" : "tard de " + milliseconds + " ms (long de ligne)");
                string spin = swing.Spin > 0f ? "lifté" : (swing.Spin < 0f ? "slicé" : "à plat");
                GUILayout.Label((swing.Side == SwingSide.Forehand ? "Coup droit " : "Revers ") + spin + " · "
                    + Mathf.RoundToInt(rally.LastShotSpeed * 3.6f) + " km/h · timing " + timing, centeredStyle);
            }
            else
            {
                GUILayout.Label("Frappe la balle après son rebond, quand elle arrive à ta hauteur.", centeredStyle);
            }

            // Barre de timing : centre = instant idéal, bords = limites de la fenêtre.
            Rect track = GUILayoutUtility.GetRect(10f, 16f, GUILayout.ExpandWidth(true));
            Fill(track, new Color(1f, 1f, 1f, 0.14f));
            Fill(new Rect(track.center.x - track.width * 0.07f, track.y, track.width * 0.14f, track.height), new Color(0.3f, 0.8f, 0.4f, 0.5f));
            Fill(new Rect(track.center.x - 1f, track.y - 3f, 2f, track.height + 6f), Color.white);

            if (rally.HasLastHit)
            {
                float position = Mathf.Clamp(rally.LastTimingSeconds / rally.TimingWindow, -1f, 1f);
                Fill(new Rect(track.center.x + position * track.width * 0.5f - 3f, track.y - 4f, 6f, track.height + 8f), new Color(1f, 0.85f, 0.3f));
            }

            float remaining = rally.TimeToContact;
            if (!float.IsNaN(remaining) && Mathf.Abs(remaining) <= rally.TimingWindow * 3f)
            {
                // Curseur de la balle en approche : il traverse la barre de gauche à droite.
                float approach = Mathf.Clamp(-remaining / rally.TimingWindow, -1f, 1f);
                Fill(new Rect(track.center.x + approach * track.width * 0.5f - 2f, track.y, 4f, track.height), new Color(0.85f, 0.95f, 0.2f));
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

            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold };
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            hugeStyle = new GUIStyle(GUI.skin.label) { fontSize = 42, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            centeredStyle = new GUIStyle(textStyle) { fontSize = 15, alignment = TextAnchor.MiddleCenter };
            rightStyle = new GUIStyle(textStyle) { alignment = TextAnchor.UpperRight };
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
