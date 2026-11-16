using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Raquette en formes simples. Axes locaux : Z du manche vers la tête, Y normale du tamis.
    /// L'origine est au bout du manche, là où la main le tient.
    /// </summary>
    public static class RacketModel
    {
        /// <summary>Distance du bout du manche au centre du tamis, en mètres.</summary>
        public const float HeadDistance = 0.47f;

        /// <summary>Part de la couleur du cordage renvoyée sous la lumière, et part émise.</summary>
        public const float StringBody = 0.8f;
        public const float StringGlow = 0.3f;

        public static Color StringColor => Palette.Cordage * StringBody;

        /// <summary>Construit une raquette aux couleurs du joueur. « strings » reçoit le cordage.</summary>
        public static Transform Build(out Renderer strings)
        {
            return Build(Palette.Framboise, out strings);
        }

        /// <summary>
        /// Construit la raquette et renvoie sa racine. Le cœur et le cadre prennent la couleur du
        /// camp ; « strings » reçoit le cordage, la partie la plus visible, avec un matériau à lui.
        /// </summary>
        public static Transform Build(Color teamColor, out Renderer strings)
        {
            var root = new GameObject("Raquette").transform;

            CourtBuilder.Primitive(PrimitiveType.Cylinder, root, "Manche", new Vector3(0f, 0f, 0.1f),
                Quaternion.Euler(90f, 0f, 0f), new Vector3(0.032f, 0.1f, 0.032f), Palette.Encre);
            CourtBuilder.Primitive(PrimitiveType.Cylinder, root, "Cœur", new Vector3(0f, 0f, 0.26f),
                Quaternion.Euler(90f, 0f, 0f), new Vector3(0.022f, 0.07f, 0.022f), teamColor);
            CourtBuilder.Primitive(PrimitiveType.Cylinder, root, "Cadre", new Vector3(0f, 0f, HeadDistance),
                Quaternion.identity, new Vector3(0.27f, 0.011f, 0.33f), teamColor);
            // Un peu lumineux : le tamis reste clair même vu par sa face à l'ombre.
            Transform cordage = CourtBuilder.Primitive(PrimitiveType.Cylinder, root, "Cordage", new Vector3(0f, 0f, HeadDistance),
                Quaternion.identity, new Vector3(0.24f, 0.013f, 0.3f),
                Palette.NewLumineux(Palette.Cordage * StringBody, Palette.Cordage * StringGlow));

            strings = cordage.GetComponent<Renderer>();
            return root;
        }
    }
}
