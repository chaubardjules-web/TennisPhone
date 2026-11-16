using System.Collections.Generic;
using UnityEngine;

namespace TennisPhone
{
    /// <summary>Ce que le reste de l'affichage a besoin de retrouver dans le court construit.</summary>
    public sealed class CourtVisual
    {
        /// <summary>Une ligne du court et la zone qu'elle couvre au sol (x et z, en mètres).</summary>
        public struct Line
        {
            public Renderer Renderer;
            public Rect Area;
        }

        public Transform Root;
        public readonly List<Line> Lines = new List<Line>();

        /// <summary>Panneaux du filet, de gauche à droite.</summary>
        public Transform[] NetPanels;

        public float NetHalfWidth;

        /// <summary>Ligne la plus proche de ce point du sol.</summary>
        public Renderer NearestLine(Vector3 point)
        {
            Renderer nearest = null;
            float best = float.MaxValue;
            foreach (Line line in Lines)
            {
                float dx = Mathf.Max(line.Area.xMin - point.x, 0f, point.x - line.Area.xMax);
                float dz = Mathf.Max(line.Area.yMin - point.z, 0f, point.z - line.Area.yMax);
                float distance = dx * dx + dz * dz;
                if (distance < best)
                {
                    best = distance;
                    nearest = line.Renderer;
                }
            }

            return nearest;
        }

        /// <summary>Panneau du filet à cette position en largeur.</summary>
        public Transform NetPanelAt(float x)
        {
            if (NetPanels == null || NetPanels.Length == 0 || NetHalfWidth <= 0f)
            {
                return null;
            }

            float ratio = Mathf.InverseLerp(-NetHalfWidth, NetHalfWidth, x);
            return NetPanels[Mathf.Clamp(Mathf.FloorToInt(ratio * NetPanels.Length), 0, NetPanels.Length - 1)];
        }
    }

    /// <summary>
    /// Construit le court à partir de formes simples : sol, surface de jeu, lignes, filet et poteaux.
    /// Les dimensions viennent de GameplayConfig, donc un changement de réglage se voit au lancement.
    /// Les couleurs viennent de Palette ; les matériaux sont partagés entre les objets.
    /// </summary>
    public static class CourtBuilder
    {
        const float LineWidth = 0.05f;
        const float BaselineWidth = 0.1f;
        const float LineHeight = 0.004f;

        public static CourtVisual Build(CourtDimensions court)
        {
            var visual = new CourtVisual { Root = new GameObject("Court").transform };
            Transform root = visual.Root;

            float halfLength = court.HalfLength;
            float halfWidth = court.HalfWidth;

            Box(root, "Abords", new Vector3(0f, -0.05f, 0f),
                new Vector3(halfWidth * 2f + 14f, 0.1f, halfLength * 2f + 22f), Palette.Brique);

            // Surface de jeu, en bandes alternées pour donner du relief à la profondeur.
            const int stripes = 12;
            float stripeLength = halfLength * 2f / stripes;
            for (int i = 0; i < stripes; i++)
            {
                float z = -halfLength + stripeLength * (i + 0.5f);
                Box(root, "Bande " + (i + 1), new Vector3(0f, 0.001f, z),
                    new Vector3(halfWidth * 2f, 0.002f, stripeLength), i % 2 == 0 ? Palette.Court : Palette.CourtStripe);
            }

            // Lignes : fond, côtés, service, centre.
            Line(visual, "Ligne de fond (joueur)", 0f, -halfLength + BaselineWidth * 0.5f, halfWidth * 2f, BaselineWidth);
            Line(visual, "Ligne de fond (adversaire)", 0f, halfLength - BaselineWidth * 0.5f, halfWidth * 2f, BaselineWidth);
            Line(visual, "Ligne de côté gauche", -halfWidth + LineWidth * 0.5f, 0f, LineWidth, halfLength * 2f);
            Line(visual, "Ligne de côté droite", halfWidth - LineWidth * 0.5f, 0f, LineWidth, halfLength * 2f);
            Line(visual, "Ligne de service (joueur)", 0f, -court.ServiceLine, halfWidth * 2f, LineWidth);
            Line(visual, "Ligne de service (adversaire)", 0f, court.ServiceLine, halfWidth * 2f, LineWidth);
            Line(visual, "Ligne centrale", 0f, 0f, LineWidth, court.ServiceLine * 2f);
            Line(visual, "Marque centrale (joueur)", 0f, -halfLength + 0.15f, LineWidth, 0.3f);
            Line(visual, "Marque centrale (adversaire)", 0f, halfLength - 0.15f, LineWidth, 0.3f);

            BuildNet(visual, court);
            return visual;
        }

        static void BuildNet(CourtVisual visual, CourtDimensions court)
        {
            var net = new GameObject("Filet").transform;
            net.SetParent(visual.Root, false);

            float halfWidth = court.NetHalfWidth;
            visual.NetHalfWidth = halfWidth;

            foreach (float side in new[] { -1f, 1f })
            {
                string suffix = side < 0f ? " gauche" : " droit";
                Box(net, "Poteau" + suffix, new Vector3(side * halfWidth, court.NetHeightPosts * 0.5f, 0f),
                    new Vector3(0.08f, court.NetHeightPosts, 0.08f), Palette.BoisFonce);
                Primitive(PrimitiveType.Sphere, net, "Boule de poteau" + suffix,
                    new Vector3(side * halfWidth, court.NetHeightPosts + 0.03f, 0f), Quaternion.identity,
                    Vector3.one * 0.11f, Palette.Creme);
            }

            // Le filet est découpé en panneaux pour suivre sa courbe : plus bas au centre.
            const int panels = 16;
            float panelWidth = halfWidth * 2f / panels;
            visual.NetPanels = new Transform[panels];
            for (int i = 0; i < panels; i++)
            {
                float x = -halfWidth + panelWidth * (i + 0.5f);
                float height = court.NetHeightAt(x);

                var panel = new GameObject("Panneau " + (i + 1)).transform;
                panel.SetParent(net, false);
                panel.localPosition = new Vector3(x, 0f, 0f);
                visual.NetPanels[i] = panel;

                // Mailles : fines bandes horizontales espacées, pour voir à travers.
                const int strands = 7;
                for (int s = 0; s < strands; s++)
                {
                    float y = height * (s + 0.5f) / (strands + 1);
                    Box(panel, "Maille", new Vector3(0f, y, 0f), new Vector3(panelWidth, 0.018f, 0.01f), Palette.Encre);
                }

                Box(panel, "Montant", new Vector3(-panelWidth * 0.5f, height * 0.5f, 0f),
                    new Vector3(0.012f, height, 0.01f), Palette.Encre);
                Box(panel, "Bande", new Vector3(0f, height - 0.03f, 0f), new Vector3(panelWidth, 0.06f, 0.02f), Palette.Creme);
            }

            Box(net, "Sangle centrale", new Vector3(0f, court.NetHeightCenter * 0.5f, 0f),
                new Vector3(0.05f, court.NetHeightCenter, 0.015f), Palette.Creme);
        }

        static void Line(CourtVisual visual, string name, float x, float z, float width, float length)
        {
            Transform line = Box(visual.Root, name, new Vector3(x, LineHeight, z), new Vector3(width, LineHeight, length), Palette.Creme);
            visual.Lines.Add(new CourtVisual.Line
            {
                Renderer = line.GetComponent<Renderer>(),
                Area = new Rect(x - width * 0.5f, z - length * 0.5f, width, length),
            });
        }

        /// <summary>Crée un pavé sans collision, de la couleur donnée.</summary>
        public static Transform Box(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            return Primitive(PrimitiveType.Cube, parent, name, position, Quaternion.identity, scale, color);
        }

        /// <summary>Crée une forme sans collision, en matériau Mat partagé de la couleur donnée.</summary>
        public static Transform Primitive(PrimitiveType type, Transform parent, string name, Vector3 position,
            Quaternion rotation, Vector3 scale, Color color)
        {
            return Primitive(type, parent, name, position, rotation, scale, Palette.Mat(color));
        }

        /// <summary>Crée une forme sans collision, avec le matériau partagé donné.</summary>
        public static Transform Primitive(PrimitiveType type, Transform parent, string name, Vector3 position,
            Quaternion rotation, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            Object.Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localRotation = rotation;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part.transform;
        }
    }
}
