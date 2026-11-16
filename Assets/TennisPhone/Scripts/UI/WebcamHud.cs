using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Petit aperçu de la webcam en bas à gauche de l'écran, au-dessus du compteur d'échange :
    /// ce que la caméra voit bouger et la position retenue pour le joueur. Sert à se placer et à
    /// vérifier que le suivi fonctionne. Affiché seulement quand le déplacement par webcam est activé.
    /// </summary>
    [RequireComponent(typeof(RallyController))]
    public sealed class WebcamHud : MonoBehaviour
    {
        const float ReferenceHeight = 720f;
        const float Margin = 24f;
        const float PanelWidth = 232f;
        const float ImageHeight = 117f;
        const float CaptionHeight = 34f;

        RallyController rally;
        WebcamTracker tracker;

        void Awake()
        {
            rally = GetComponent<RallyController>();
            tracker = GetComponent<WebcamTracker>();
        }

        void OnGUI()
        {
            if (tracker == null || rally.Score == null || !rally.WebcamMovement)
            {
                return;
            }

            float scale = Screen.height / ReferenceHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            HudSkin.Alpha = 1f;

            // Au-dessus de la pastille « Échange », qui occupe les 36 derniers points du bas.
            float height = ImageHeight + CaptionHeight + 24f;
            var panel = new Rect(Margin, ReferenceHeight - Margin - 36f - 12f - height, PanelWidth, height);
            HudSkin.Panel(panel, Palette.WithAlpha(Palette.Ardoise, 0.72f), 16f);

            var image = new Rect(panel.x + 12f, panel.y + 12f, PanelWidth - 24f, ImageHeight);
            string caption;
            if (!string.IsNullOrEmpty(tracker.Error))
            {
                caption = tracker.Error;
            }
            else if (!tracker.IsRunning || tracker.Preview == null)
            {
                caption = "Démarrage de la webcam…";
            }
            else
            {
                GUI.DrawTexture(image, tracker.Preview, ScaleMode.StretchToFill);
                if (!tracker.HasTarget)
                {
                    caption = "Bouge devant la caméra";
                }
                else if (Mathf.Abs(tracker.PlayerX) > 0.38f)
                {
                    caption = "Reviens vers le milieu de l'image";
                }
                else
                {
                    caption = rally.AutoSwing ? "Démonstration : placement automatique" : "Tes pas déplacent le joueur";
                }
            }

            HudSkin.Label(new Rect(panel.x + 10f, image.yMax + 4f, PanelWidth - 20f, CaptionHeight), caption,
                HudSkin.Text(HudFont.Medium, 15, TextAnchor.MiddleCenter, true), Palette.GrisClair);
        }
    }
}
