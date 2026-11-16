using System.Collections.Generic;
using UnityEngine;

namespace TennisPhone
{
    /// <summary>Graisse d'un texte du HUD.</summary>
    public enum HudFont
    {
        /// <summary>Légendes, aide.</summary>
        Medium,

        /// <summary>Noms, consignes, sous-titres.</summary>
        SemiBold,

        /// <summary>Chiffres du score, messages, verdicts.</summary>
        ExtraBold,
    }

    /// <summary>
    /// Habillage du HUD en IMGUI : panneaux « ardoise » aux coins arrondis, pastilles, textes.
    /// Les couleurs viennent de Palette. Toutes les tailles sont en pixels de la référence de
    /// 720 px de haut ; aucun texte ne descend sous 18 px.
    /// Si une police est posée dans Resources/Fonts (Baloo2-Medium, Baloo2-SemiBold,
    /// Baloo2-ExtraBold), elle est utilisée ; sinon la police par défaut d'Unity sert de repli.
    /// </summary>
    public static class HudSkin
    {
        /// <summary>Opacité de l'ardoise, fond de tous les panneaux.</summary>
        public const float SlateOpacity = 0.92f;

        /// <summary>Opacité appliquée à tout ce qui est dessiné ensuite (fondus).</summary>
        public static float Alpha = 1f;

        static readonly string[] FontNames = { "Fonts/Baloo2-Medium", "Fonts/Baloo2-SemiBold", "Fonts/Baloo2-ExtraBold" };
        static readonly Dictionary<int, GUIStyle> Styles = new Dictionary<int, GUIStyle>();
        static readonly GUIContent Content = new GUIContent();
        static Font[] fonts;

        public static Color Slate => Palette.WithAlpha(Palette.Ardoise, SlateOpacity);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            Styles.Clear();
            fonts = null;
            Alpha = 1f;
        }

        /// <summary>Style de texte. À appeler depuis OnGUI.</summary>
        public static GUIStyle Text(HudFont font, int size, TextAnchor anchor, bool wrap = false)
        {
            int key = ((int)font << 24) | (size << 8) | ((int)anchor << 1) | (wrap ? 1 : 0);
            if (Styles.TryGetValue(key, out GUIStyle style))
            {
                return style;
            }

            if (fonts == null)
            {
                fonts = new Font[FontNames.Length];
                for (int i = 0; i < fonts.Length; i++)
                {
                    fonts[i] = Resources.Load<Font>(FontNames[i]);
                }
            }

            style = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                alignment = anchor,
                wordWrap = wrap,
                richText = false,
                clipping = TextClipping.Overflow,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
            };

            Font custom = fonts[(int)font];
            if (custom != null)
            {
                style.font = custom;
                style.fontStyle = FontStyle.Normal;
            }
            else
            {
                // Police par défaut : le gras tient lieu de graisses fortes.
                style.fontStyle = font == HudFont.Medium ? FontStyle.Normal : FontStyle.Bold;
            }

            style.normal.textColor = Color.white;
            Styles[key] = style;
            return style;
        }

        public static Vector2 Measure(string text, GUIStyle style)
        {
            Content.text = text;
            return style.CalcSize(Content);
        }

        /// <summary>Rectangle plein aux coins arrondis.</summary>
        public static void Panel(Rect area, Color color, float radius)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            color.a *= Alpha;
            GUI.DrawTexture(area, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, 0f,
                Mathf.Min(radius, Mathf.Min(area.width, area.height) * 0.5f));
        }

        /// <summary>Contour aux coins arrondis.</summary>
        public static void Outline(Rect area, Color color, float radius, float width)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            color.a *= Alpha;
            GUI.DrawTexture(area, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, width,
                Mathf.Min(radius, Mathf.Min(area.width, area.height) * 0.5f));
        }

        /// <summary>Disque plein.</summary>
        public static void Disc(Vector2 center, float diameter, Color color)
        {
            Panel(new Rect(center.x - diameter * 0.5f, center.y - diameter * 0.5f, diameter, diameter), color, diameter * 0.5f);
        }

        /// <summary>Barre verticale avec un contour sombre : curseurs et repères des jauges.</summary>
        public static void Marker(float x, Rect track, float width, float overflow, Color color)
        {
            var bar = new Rect(x - width * 0.5f, track.y - overflow, width, track.height + overflow * 2f);
            Panel(new Rect(bar.x - 2f, bar.y - 2f, bar.width + 4f, bar.height + 4f), Palette.Encre, 3f);
            Panel(bar, color, 2f);
        }

        public static void Label(Rect area, string text, GUIStyle style, Color color)
        {
            Color previous = GUI.contentColor;
            color.a *= Alpha;
            GUI.contentColor = color;
            GUI.Label(area, text, style);
            GUI.contentColor = previous;
        }

        /// <summary>Texte centré dans une pastille de couleur.</summary>
        public static void Pill(Rect area, string text, GUIStyle style, Color background, Color textColor)
        {
            Panel(area, background, area.height * 0.5f);
            Label(area, text, style, textColor);
        }
    }
}
