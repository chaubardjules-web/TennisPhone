using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Construit le court à partir de formes simples : sol, surface de jeu, lignes, filet et poteaux.
    /// Les dimensions viennent de GameplayConfig, donc un changement de réglage se voit au lancement.
    /// </summary>
    public static class CourtBuilder
    {
        const float LineWidth = 0.05f;
        const float BaselineWidth = 0.1f;
        const float LineHeight = 0.004f;

        static readonly Color SurroundColor = new Color(0.18f, 0.36f, 0.24f);
        static readonly Color CourtColor = new Color(0.25f, 0.52f, 0.3f);
        static readonly Color CourtStripeColor = new Color(0.29f, 0.58f, 0.33f);
        static readonly Color LineColor = new Color(0.96f, 0.96f, 0.93f);
        static readonly Color NetColor = new Color(0.1f, 0.12f, 0.13f);
        static readonly Color PostColor = new Color(0.2f, 0.22f, 0.24f);

        public static Transform Build(CourtDimensions court)
        {
            var root = new GameObject("Court").transform;

            float halfLength = court.HalfLength;
            float halfWidth = court.HalfWidth;

            Box(root, "Abords", new Vector3(0f, -0.05f, 0f),
                new Vector3(halfWidth * 2f + 14f, 0.1f, halfLength * 2f + 22f), SurroundColor);

            // Surface de jeu, en bandes alternées pour donner du relief à la profondeur.
            const int stripes = 12;
            float stripeLength = halfLength * 2f / stripes;
            for (int i = 0; i < stripes; i++)
            {
                float z = -halfLength + stripeLength * (i + 0.5f);
                Box(root, "Bande " + (i + 1), new Vector3(0f, 0.001f, z),
                    new Vector3(halfWidth * 2f, 0.002f, stripeLength), i % 2 == 0 ? CourtColor : CourtStripeColor);
            }

            // Lignes : fond, côtés, service, centre.
            Line(root, "Ligne de fond (joueur)", 0f, -halfLength + BaselineWidth * 0.5f, halfWidth * 2f, BaselineWidth);
            Line(root, "Ligne de fond (adversaire)", 0f, halfLength - BaselineWidth * 0.5f, halfWidth * 2f, BaselineWidth);
            Line(root, "Ligne de côté gauche", -halfWidth + LineWidth * 0.5f, 0f, LineWidth, halfLength * 2f);
            Line(root, "Ligne de côté droite", halfWidth - LineWidth * 0.5f, 0f, LineWidth, halfLength * 2f);
            Line(root, "Ligne de service (joueur)", 0f, -court.ServiceLine, halfWidth * 2f, LineWidth);
            Line(root, "Ligne de service (adversaire)", 0f, court.ServiceLine, halfWidth * 2f, LineWidth);
            Line(root, "Ligne centrale", 0f, 0f, LineWidth, court.ServiceLine * 2f);
            Line(root, "Marque centrale (joueur)", 0f, -halfLength + 0.15f, LineWidth, 0.3f);
            Line(root, "Marque centrale (adversaire)", 0f, halfLength - 0.15f, LineWidth, 0.3f);

            BuildNet(root, court);
            return root;
        }

        static void BuildNet(Transform root, CourtDimensions court)
        {
            var net = new GameObject("Filet").transform;
            net.SetParent(root, false);

            float halfWidth = court.NetHalfWidth;
            Box(net, "Poteau gauche", new Vector3(-halfWidth, court.NetHeightPosts * 0.5f, 0f),
                new Vector3(0.08f, court.NetHeightPosts, 0.08f), PostColor);
            Box(net, "Poteau droit", new Vector3(halfWidth, court.NetHeightPosts * 0.5f, 0f),
                new Vector3(0.08f, court.NetHeightPosts, 0.08f), PostColor);

            // Le filet est découpé en panneaux pour suivre sa courbe : plus bas au centre.
            const int panels = 16;
            float panelWidth = halfWidth * 2f / panels;
            for (int i = 0; i < panels; i++)
            {
                float x = -halfWidth + panelWidth * (i + 0.5f);
                float height = court.NetHeightAt(x);

                // Mailles : fines bandes horizontales espacées, pour voir à travers.
                const int strands = 7;
                for (int s = 0; s < strands; s++)
                {
                    float y = height * (s + 0.5f) / (strands + 1);
                    Box(net, "Maille", new Vector3(x, y, 0f), new Vector3(panelWidth, 0.018f, 0.01f), NetColor);
                }

                Box(net, "Montant", new Vector3(x - panelWidth * 0.5f, height * 0.5f, 0f),
                    new Vector3(0.012f, height, 0.01f), NetColor);
                Box(net, "Bande", new Vector3(x, height - 0.03f, 0f), new Vector3(panelWidth, 0.06f, 0.02f), LineColor);
            }

            Box(net, "Sangle centrale", new Vector3(0f, court.NetHeightCenter * 0.5f, 0f),
                new Vector3(0.05f, court.NetHeightCenter, 0.015f), LineColor);
        }

        static void Line(Transform parent, string name, float x, float z, float width, float length)
        {
            Box(parent, name, new Vector3(x, LineHeight, z), new Vector3(width, LineHeight, length), LineColor);
        }

        /// <summary>Crée un pavé sans collision, de la couleur donnée.</summary>
        public static Transform Box(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            return Primitive(PrimitiveType.Cube, parent, name, position, Quaternion.identity, scale, color);
        }

        public static Transform Primitive(PrimitiveType type, Transform parent, string name, Vector3 position,
            Quaternion rotation, Vector3 scale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            Object.Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localRotation = rotation;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().material.color = color;
            return part.transform;
        }
    }
}
