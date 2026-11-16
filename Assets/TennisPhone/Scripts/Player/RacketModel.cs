using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Raquette en formes simples. Axes locaux : Z du manche vers la tête, Y normale du tamis.
    /// L'origine est au bout du manche, là où la main le tient.
    /// </summary>
    public static class RacketModel
    {
        public static readonly Color StringColor = new Color(0.95f, 0.92f, 0.75f);

        static readonly Color GripColor = new Color(0.15f, 0.15f, 0.17f);
        static readonly Color FrameColor = new Color(0.85f, 0.25f, 0.2f);

        /// <summary>Construit la raquette et renvoie sa racine. « strings » reçoit le cordage.</summary>
        public static Transform Build(out Renderer strings)
        {
            var root = new GameObject("Raquette").transform;

            CourtBuilder.Primitive(PrimitiveType.Cylinder, root, "Manche", new Vector3(0f, 0f, 0.1f),
                Quaternion.Euler(90f, 0f, 0f), new Vector3(0.032f, 0.1f, 0.032f), GripColor);
            CourtBuilder.Primitive(PrimitiveType.Cylinder, root, "Cœur", new Vector3(0f, 0f, 0.26f),
                Quaternion.Euler(90f, 0f, 0f), new Vector3(0.022f, 0.07f, 0.022f), FrameColor);
            CourtBuilder.Primitive(PrimitiveType.Cylinder, root, "Cadre", new Vector3(0f, 0f, 0.47f),
                Quaternion.identity, new Vector3(0.27f, 0.011f, 0.33f), FrameColor);
            Transform cordage = CourtBuilder.Primitive(PrimitiveType.Cylinder, root, "Cordage", new Vector3(0f, 0f, 0.47f),
                Quaternion.identity, new Vector3(0.24f, 0.013f, 0.3f), StringColor);

            strings = cordage.GetComponent<Renderer>();
            return root;
        }
    }
}
