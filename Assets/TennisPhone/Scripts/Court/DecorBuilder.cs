using System.Collections.Generic;
using UnityEngine;

namespace TennisPhone
{
    /// <summary>Ce que l'animation du décor a besoin de retrouver.</summary>
    public sealed class DecorVisual
    {
        public Transform Root;

        /// <summary>Points d'accroche des lampions : chacun se balance autour du sien.</summary>
        public readonly List<Transform> LanternPivots = new List<Transform>();

        /// <summary>Un matériau lumineux par couleur de lampion, et sa couleur.</summary>
        public Material[] LanternMaterials;

        public Color[] LanternColors;

        /// <summary>Émission d'un lampion au repos, en part de sa couleur.</summary>
        public float LanternGlow;
    }

    /// <summary>
    /// Décor du court, en deux ambiances.
    /// « Soir des lampions » : pré, haies, peupliers, tilleuls, gradins vides, parasols.
    /// « Stade de jour » : tribunes en bois sur trois côtés, sièges de couleur, arbres, ciel bleu.
    /// Dans les deux : chaise d'arbitre, guirlandes de lampions, collines, nuages et ciel.
    /// Aucun spectateur, aucun logo. Tout est fait de formes simples sans collision : rien ne
    /// change à la physique de la balle.
    /// </summary>
    public static class DecorBuilder
    {
        const float GarlandHeight = 5.5f;
        const float LanternDrop = 0.22f;
        const float LanternDiameter = 0.34f;
        const int CableSegments = 8;

        public static DecorVisual Build(Camera view, Ambiance ambiance)
        {
            bool day = ambiance == Ambiance.Jour;
            var decor = new DecorVisual { Root = new GameObject("Décor").transform };
            Transform root = decor.Root;

            CourtBuilder.Box(root, "Pré", new Vector3(0f, -0.11f, 40f), new Vector3(360f, 0.1f, 300f),
                day ? Palette.PreJour : Palette.Pre);

            BuildHedges(Group(root, "Haies"), !day);
            if (day)
            {
                BuildStadium(Group(root, "Tribunes"));
                BuildStadiumTrees(Group(root, "Arbres"));
            }
            else
            {
                BuildPoplars(Group(root, "Peupliers"));
                BuildLindens(Group(root, "Tilleuls"));
                BuildBleachers(Group(root, "Gradins"));
                BuildParasols(Group(root, "Parasols"));
            }

            BuildUmpireChair(Group(root, "Chaise d'arbitre"));

            // De jour, les lampions sont presque éteints et leurs poteaux passent derrière les tribunes.
            decor.LanternGlow = day ? 0.25f : 0.9f;
            BuildLanterns(decor, Group(root, "Lampions"), day ? 13.5f : 11.6f);
            BuildDistance(Group(root, "Lointain"), day);
            if (view != null)
            {
                BuildSky(view, day);
            }

            return decor;
        }

        static Transform Group(Transform parent, string name)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        static Transform Part(PrimitiveType type, Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            return CourtBuilder.Primitive(type, parent, name, position, Quaternion.identity, scale, color);
        }

        // ---------------------------------------------------------------- Fond sombre

        /// <summary>La haie du fond est le fond sombre sur lequel la balle se détache.</summary>
        static void BuildHedges(Transform parent, bool sides)
        {
            CourtBuilder.Box(parent, "Haie du fond", new Vector3(0f, 1.3f, 24.6f), new Vector3(30f, 2.6f, 1.4f), Palette.Haie);
            CourtBuilder.Box(parent, "Dessus de la haie du fond", new Vector3(0f, 2.63f, 24.6f), new Vector3(30f, 0.06f, 1.4f), Palette.HaieClaire);
            if (!sides)
            {
                return;
            }

            foreach (float side in new[] { -1f, 1f })
            {
                CourtBuilder.Box(parent, "Haie de côté", new Vector3(side * 12.6f, 0.9f, 1f), new Vector3(1.2f, 1.8f, 48f), Palette.Haie);
                CourtBuilder.Box(parent, "Dessus de la haie de côté", new Vector3(side * 12.6f, 1.83f, 1f), new Vector3(1.2f, 0.06f, 48f), Palette.HaieClaire);
            }
        }

        /// <summary>Rideau de peupliers en deux rangs décalés : il ferme le fond jusqu'en haut de l'écran.</summary>
        static void BuildPoplars(Transform parent)
        {
            // Graine fixe : le décor est identique à chaque lancement.
            var random = new System.Random(7);
            int index = 0;
            for (int row = 0; row < 2; row++)
            {
                float z = row == 0 ? 29f : 33f;
                float firstX = row == 0 ? -16f : -14f;
                int count = row == 0 ? 9 : 8;
                for (int i = 0; i < count; i++)
                {
                    float x = firstX + 4f * i;
                    float k = 0.92f + 0.16f * (float)random.NextDouble();
                    Part(PrimitiveType.Cylinder, parent, "Tronc", new Vector3(x, 1.2f, z), new Vector3(0.5f, 1.2f, 0.5f), Palette.Tronc);
                    Part(PrimitiveType.Capsule, parent, "Feuillage", new Vector3(x, 6.6f, z), new Vector3(3.4f, 4.6f * k, 3.4f),
                        index % 2 == 0 ? Palette.PeuplierSombre : Palette.Peuplier);
                    index++;
                }
            }
        }

        static void BuildLindens(Transform parent)
        {
            var spots = new[]
            {
                new Vector2(-19f, -4f), new Vector2(-21f, 9f), new Vector2(-19f, 21f),
                new Vector2(19f, 3f), new Vector2(21f, 15f),
            };

            foreach (Vector2 spot in spots)
            {
                Part(PrimitiveType.Cylinder, parent, "Tronc", new Vector3(spot.x, 1.5f, spot.y), new Vector3(0.6f, 1.5f, 0.6f), Palette.Tronc);
                Part(PrimitiveType.Sphere, parent, "Feuillage", new Vector3(spot.x, 5.5f, spot.y), new Vector3(6f, 5.5f, 6f), Palette.Tilleul);
            }
        }

        // ---------------------------------------------------------------- Stade de jour

        /// <summary>
        /// Tribunes en bois sur les deux côtés et au fond, sans spectateurs : des rangées de
        /// sièges de couleur. La tribune du fond est sombre, pour que la balle s'en détache.
        /// </summary>
        static void BuildStadium(Transform parent)
        {
            var random = new System.Random(7);
            Color[] light = { Palette.BoisClair, Palette.BoisMiel };
            Color[] dark = { Palette.BoisFonce, Palette.TribuneSombre };

            Stand(Group(parent, "Tribune de gauche"), new Vector3(-8.15f, 0f, 4f), Vector3.left, 36f, 5, 0f, 0.45f,
                light, Palette.BoisFonce, random);
            Stand(Group(parent, "Tribune de droite"), new Vector3(8.15f, 0f, 4f), Vector3.right, 36f, 5, 0f, 0.45f,
                light, Palette.BoisFonce, random);

            // Posée sur un socle : seul ce qui dépasse de la haie du fond se voit.
            Stand(Group(parent, "Tribune du fond"), new Vector3(0f, 0f, 26f), Vector3.forward, 30f, 6, 2.4f, 0.5f,
                dark, Palette.TribuneSombre, random);
        }

        /// <summary>Une tribune : des marches qui montent vers l'extérieur, des sièges, un mur de fond.</summary>
        /// <param name="inner">Milieu du bord intérieur, au sol.</param>
        /// <param name="outward">Direction dans laquelle les marches montent.</param>
        static void Stand(Transform parent, Vector3 inner, Vector3 outward, float length, int tiers, float baseHeight,
            float rise, Color[] tierColors, Color wallColor, System.Random random)
        {
            const float depth = 0.9f;
            const float seatSpacing = 0.9f;
            Color[] seatColors = { Palette.Corail, Palette.Turquoise, Palette.CremeChaude, Palette.Rose, Palette.Lavande };

            Quaternion rotation = Quaternion.LookRotation(outward);
            Vector3 along = rotation * Vector3.right;
            int seats = Mathf.FloorToInt(length / seatSpacing) - 1;

            for (int tier = 0; tier < tiers; tier++)
            {
                float top = baseHeight + rise * (tier + 1);
                Vector3 center = inner + outward * (depth * (tier + 0.5f));
                CourtBuilder.Primitive(PrimitiveType.Cube, parent, "Marche " + (tier + 1), center + Vector3.up * (top * 0.5f),
                    rotation, new Vector3(length, top, depth), tierColors[tier % tierColors.Length]);

                // Sièges par petits groupes de même couleur, avec quelques places vides.
                Color color = seatColors[0];
                for (int seat = 0; seat < seats; seat++)
                {
                    if (seat % 4 == 0)
                    {
                        color = seatColors[random.Next(seatColors.Length)];
                    }

                    if (random.Next(9) == 0)
                    {
                        continue;
                    }

                    Vector3 position = center + along * ((seat - (seats - 1) * 0.5f) * seatSpacing) + Vector3.up * (top + 0.035f);
                    Transform cushion = CourtBuilder.Primitive(PrimitiveType.Cube, parent, "Siège", position, rotation,
                        new Vector3(0.42f, 0.07f, 0.38f), color);
                    cushion.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }

            float wallHeight = baseHeight + rise * tiers + 0.9f;
            CourtBuilder.Primitive(PrimitiveType.Cube, parent, "Mur de fond",
                inner + outward * (depth * tiers + 0.1f) + Vector3.up * (wallHeight * 0.5f), rotation,
                new Vector3(length, wallHeight, 0.2f), wallColor);
        }

        /// <summary>Arbres ronds derrière les tribunes, avec quelques peupliers : le ciel reste visible entre eux.</summary>
        static void BuildStadiumTrees(Transform parent)
        {
            var lindens = new[]
            {
                new Vector2(-18f, -4f), new Vector2(-19.5f, 7f), new Vector2(-18f, 18f), new Vector2(-20f, 29f),
                new Vector2(18f, -1f), new Vector2(19.5f, 10f), new Vector2(18f, 21f), new Vector2(20f, 31f),
                new Vector2(-11f, 38f), new Vector2(-1f, 39f), new Vector2(9f, 38f),
            };

            var random = new System.Random(7);
            foreach (Vector2 spot in lindens)
            {
                float k = 0.9f + 0.25f * (float)random.NextDouble();
                Part(PrimitiveType.Cylinder, parent, "Tronc", new Vector3(spot.x, 1.5f, spot.y), new Vector3(0.6f, 1.5f, 0.6f), Palette.Tronc);
                Part(PrimitiveType.Sphere, parent, "Feuillage", new Vector3(spot.x, 2.6f + 2.75f * k, spot.y),
                    new Vector3(6f * k, 5.5f * k, 6f * k), Palette.Tilleul);
            }

            foreach (float x in new[] { -15.5f, -6f, 4f, 14.5f })
            {
                Part(PrimitiveType.Cylinder, parent, "Tronc", new Vector3(x, 1.2f, 43f), new Vector3(0.5f, 1.2f, 0.5f), Palette.Tronc);
                Part(PrimitiveType.Capsule, parent, "Feuillage", new Vector3(x, 6f, 43f), new Vector3(3.2f, 4.2f, 3.2f), Palette.Peuplier);
            }
        }

        // ---------------------------------------------------------------- Mobilier

        /// <summary>Trois marches en bois le long du court, sans spectateurs, avec des coussins.</summary>
        static void BuildBleachers(Transform parent)
        {
            float[] stepX = { -8.6f, -9.5f, -10.4f };
            float[] stepTop = { 0.45f, 0.9f, 1.35f };
            Color[] stepColors = { Palette.BoisClair, Palette.BoisMiel, Palette.BoisClair };
            for (int i = 0; i < 3; i++)
            {
                CourtBuilder.Box(parent, "Marche " + (i + 1), new Vector3(stepX[i], stepTop[i] * 0.5f, 0f),
                    new Vector3(0.9f, stepTop[i], 14f), stepColors[i]);
            }

            Color[] cushionColors = { Palette.Corail, Palette.Turquoise, Palette.CremeChaude, Palette.Rose, Palette.Lavande };
            var random = new System.Random(7);
            for (int i = 0; i < 16; i++)
            {
                int step = random.Next(3);
                CourtBuilder.Box(parent, "Coussin", new Vector3(stepX[step], stepTop[step] + 0.035f, -6.3f + 0.84f * i),
                    new Vector3(0.42f, 0.07f, 0.38f), cushionColors[i % cushionColors.Length]);
            }
        }

        static void BuildParasols(Transform parent)
        {
            const float x = 9.6f;
            float[] spots = { -5f, 1f, 7f };
            Color[] canvas = { Palette.Corail, Palette.CremeChaude, Palette.Turquoise };
            for (int i = 0; i < spots.Length; i++)
            {
                float z = spots[i];
                Part(PrimitiveType.Cylinder, parent, "Mât", new Vector3(x, 1.25f, z), new Vector3(0.07f, 1.25f, 0.07f), Palette.BoisFonce);
                Part(PrimitiveType.Cylinder, parent, "Toile basse", new Vector3(x, 2.42f, z), new Vector3(2.6f, 0.05f, 2.6f), canvas[i]);
                Part(PrimitiveType.Cylinder, parent, "Toile haute", new Vector3(x, 2.54f, z), new Vector3(1.5f, 0.07f, 1.5f), canvas[i]);
                Part(PrimitiveType.Sphere, parent, "Pommeau", new Vector3(x, 2.66f, z), Vector3.one * 0.16f, Palette.Creme);
                Part(PrimitiveType.Cylinder, parent, "Table", new Vector3(x, 0.74f, z), new Vector3(0.9f, 0.02f, 0.9f), Palette.BoisClair);
                Part(PrimitiveType.Cylinder, parent, "Tabouret", new Vector3(x, 0.225f, z - 0.75f), new Vector3(0.36f, 0.225f, 0.36f), Palette.BoisMiel);
                Part(PrimitiveType.Cylinder, parent, "Tabouret", new Vector3(x, 0.225f, z + 0.75f), new Vector3(0.36f, 0.225f, 0.36f), Palette.BoisMiel);
            }
        }

        /// <summary>Chaise d'arbitre vide, dans l'axe du filet.</summary>
        static void BuildUmpireChair(Transform parent)
        {
            const float x = 6.3f;
            foreach (float dx in new[] { -0.3f, 0.3f })
            {
                foreach (float dz in new[] { -0.3f, 0.3f })
                {
                    CourtBuilder.Box(parent, "Montant", new Vector3(x + dx, 0.95f, dz), new Vector3(0.06f, 1.9f, 0.06f), Palette.BoisClair);
                }
            }

            foreach (float y in new[] { 0.5f, 1f, 1.5f })
            {
                CourtBuilder.Box(parent, "Barreau", new Vector3(x, y, -0.3f), new Vector3(0.6f, 0.04f, 0.04f), Palette.BoisClair);
            }

            CourtBuilder.Box(parent, "Assise", new Vector3(x, 1.9f, 0f), new Vector3(0.7f, 0.06f, 0.7f), Palette.Creme);
            CourtBuilder.Box(parent, "Dossier", new Vector3(x + 0.32f, 2.25f, 0f), new Vector3(0.06f, 0.7f, 0.7f), Palette.Creme);
        }

        // ---------------------------------------------------------------- Lampions

        /// <summary>
        /// Guirlandes de lampions : des sphères émissives, aucune source de lumière réelle.
        /// Jamais de jaune ni de vert : la balle reste le seul objet jaune-vert de la scène.
        /// </summary>
        static void BuildLanterns(DecorVisual decor, Transform parent, float x)
        {
            Color[] colors = { Palette.Corail, Palette.CremeChaude, Palette.Turquoise, Palette.Rose };
            decor.LanternColors = colors;
            decor.LanternMaterials = new Material[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                // Un matériau par couleur, propre au décor : son émission pulse à la fin d'un set.
                // Plus le lampion éclaire, moins il renvoie la lumière du soleil : sa couleur reste franche.
                Color body = colors[i] * (1f - 0.85f * decor.LanternGlow);
                body.a = 1f;
                decor.LanternMaterials[i] = Palette.NewLumineux(body, colors[i] * decor.LanternGlow);
            }

            float[] postZ = { -16f, 2f, 20f };
            foreach (float side in new[] { -1f, 1f })
            {
                foreach (float z in postZ)
                {
                    Part(PrimitiveType.Cylinder, parent, "Poteau de guirlande", new Vector3(side * x, 2.8f, z),
                        new Vector3(0.14f, 2.8f, 0.14f), Palette.BoisFonce);
                }
            }

            int index = 0;
            foreach (float side in new[] { -1f, 1f })
            {
                for (int span = 0; span < 2; span++)
                {
                    Garland(decor, parent, new Vector3(side * x, GarlandHeight, postZ[span]),
                        new Vector3(side * x, GarlandHeight, postZ[span + 1]), 0.7f, 8, ref index);
                }
            }

            Garland(decor, parent, new Vector3(-x, GarlandHeight, postZ[2]), new Vector3(x, GarlandHeight, postZ[2]), 0.9f, 10, ref index);
        }

        static void Garland(DecorVisual decor, Transform parent, Vector3 from, Vector3 to, float sag, int lanterns, ref int index)
        {
            // Câble : des segments droits tendus le long de la courbe.
            for (int i = 0; i < CableSegments; i++)
            {
                Vector3 a = CablePoint(from, to, sag, i / (float)CableSegments);
                Vector3 b = CablePoint(from, to, sag, (i + 1) / (float)CableSegments);
                CourtBuilder.Primitive(PrimitiveType.Cube, parent, "Câble", (a + b) * 0.5f,
                    Quaternion.LookRotation(b - a), new Vector3(0.02f, 0.02f, (b - a).magnitude), Palette.Encre);
            }

            for (int i = 0; i < lanterns; i++)
            {
                var pivot = new GameObject("Lampion").transform;
                pivot.SetParent(parent, false);
                pivot.localPosition = CablePoint(from, to, sag, (i + 0.5f) / lanterns);

                Material material = decor.LanternMaterials[index % decor.LanternMaterials.Length];
                Transform globe = CourtBuilder.Primitive(PrimitiveType.Sphere, pivot, "Globe", new Vector3(0f, -LanternDrop, 0f),
                    Quaternion.identity, Vector3.one * LanternDiameter, material);
                globe.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Part(PrimitiveType.Cylinder, pivot, "Capuchon", new Vector3(0f, -LanternDrop + LanternDiameter * 0.5f + 0.015f, 0f),
                    new Vector3(0.1f, 0.03f, 0.1f), Palette.Encre);

                decor.LanternPivots.Add(pivot);
                index++;
            }
        }

        static Vector3 CablePoint(Vector3 from, Vector3 to, float sag, float u)
        {
            Vector3 point = Vector3.Lerp(from, to, u);
            point.y -= sag * 4f * u * (1f - u);
            return point;
        }

        // ---------------------------------------------------------------- Lointain et ciel

        static void BuildDistance(Transform parent, bool day)
        {
            Part(PrimitiveType.Sphere, parent, "Colline de gauche", new Vector3(-55f, -2f, 110f), new Vector3(120f, 22f, 40f), Palette.CollineProche);
            Part(PrimitiveType.Sphere, parent, "Colline de droite", new Vector3(45f, -3f, 120f), new Vector3(150f, 30f, 40f), Palette.CollineLointaine);

            // Les nuages sont au-delà de la brume : leur matériau ne doit pas la subir.
            Material cloud = Palette.Voile(day ? Palette.NuageJour : Palette.Nuage);
            Vector3[] centers = day
                ? new[]
                {
                    new Vector3(-70f, 13f, 125f), new Vector3(20f, 15f, 125f), new Vector3(80f, 12f, 125f),
                    new Vector3(-25f, 17f, 140f), new Vector3(52f, 18f, 145f),
                }
                : new[] { new Vector3(-70f, 12f, 125f), new Vector3(20f, 13f, 125f), new Vector3(80f, 11f, 125f) };
            var sizes = new[] { new Vector3(14f, 2.2f, 4f), new Vector3(20f, 3f, 4f), new Vector3(17f, 2.6f, 4f) };
            foreach (Vector3 center in centers)
            {
                for (int i = 0; i < 3; i++)
                {
                    Transform puff = CourtBuilder.Primitive(PrimitiveType.Sphere, parent, "Nuage",
                        center + new Vector3((i - 1) * 6f, i == 1 ? 0.6f : 0f, 0f), Quaternion.identity, sizes[i], cloud);
                    Palette.AsVeil(puff.GetComponent<Renderer>(), cloud);
                }
            }
        }

        /// <summary>
        /// Ciel : un grand panneau fixé à la caméra, peint d'un dégradé. Valable parce que la
        /// caméra ne tourne pas : l'horizon est à 87,5 % de la hauteur de l'image.
        /// </summary>
        static void BuildSky(Camera view, bool day)
        {
            Color horizon = day ? Palette.CielJourHorizon : Palette.CielHorizon;
            Color middle = day ? Color.Lerp(Palette.CielJourHorizon, Palette.CielJourHaut, 0.45f) : Palette.CielMilieu;
            Color top = day ? Palette.CielJourHaut : Palette.CielHaut;

            const int height = 256;
            var texture = new Texture2D(4, height, TextureFormat.RGBA32, false)
            {
                name = "Dégradé du ciel",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var pixels = new Color[4 * height];
            for (int y = 0; y < height; y++)
            {
                float v = y / (height - 1f);
                Color color = v <= 0.875f
                    ? horizon
                    : v <= 0.93f
                        ? Color.Lerp(horizon, middle, Mathf.InverseLerp(0.875f, 0.93f, v))
                        : Color.Lerp(middle, top, Mathf.InverseLerp(0.93f, 1f, v));
                for (int x = 0; x < 4; x++)
                {
                    pixels[y * 4 + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);

            Material material = Palette.NewVoile(Color.white);
            material.name = "Ciel";
            material.mainTexture = texture;

            // Dessiné en premier, derrière tout le reste.
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Background;

            Transform sky = CourtBuilder.Primitive(PrimitiveType.Quad, view.transform, "Ciel", new Vector3(0f, 0f, 170f),
                Quaternion.identity, new Vector3(300f, 125f, 1f), material);
            Palette.AsVeil(sky.GetComponent<Renderer>(), material);
        }
    }

    /// <summary>
    /// Anime les lampions : léger balancement permanent, et pulsation de leur lumière à la fin d'un set.
    /// </summary>
    public sealed class DecorAnimator : MonoBehaviour
    {
        const float SwayDegrees = 3f;
        const float SwayFrequency = 0.4f;
        const float PulseDuration = 1.5f;
        const float PulseGlow = 1.6f;

        DecorVisual decor;
        float pulseStart = -10f;
        bool pulsing;

        public void Init(DecorVisual visual)
        {
            decor = visual;
        }

        /// <summary>Fait pulser la lumière de tous les lampions trois fois en 1,5 s.</summary>
        public void Pulse()
        {
            pulseStart = Time.time;
            pulsing = true;
        }

        void Update()
        {
            if (decor == null)
            {
                return;
            }

            float time = Time.time;
            for (int i = 0; i < decor.LanternPivots.Count; i++)
            {
                float angle = SwayDegrees * Mathf.Sin(2f * Mathf.PI * SwayFrequency * time + i * 1.7f);
                decor.LanternPivots[i].localRotation = Quaternion.Euler(angle, 0f, angle * 0.6f);
            }

            if (!pulsing)
            {
                return;
            }

            float progress = (time - pulseStart) / PulseDuration;
            pulsing = progress < 1f;
            float wave = pulsing ? 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * 3f * progress) : 0f;
            SetGlow(Mathf.Lerp(decor.LanternGlow, PulseGlow, wave));
        }

        void SetGlow(float glow)
        {
            for (int i = 0; i < decor.LanternMaterials.Length; i++)
            {
                decor.LanternMaterials[i].SetColor("_EmissionColor", decor.LanternColors[i] * glow);
            }
        }

    }
}
