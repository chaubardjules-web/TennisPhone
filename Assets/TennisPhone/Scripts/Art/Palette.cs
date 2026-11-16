using System.Collections.Generic;
using UnityEngine;

namespace TennisPhone
{
    /// <summary>Teinte de la surface de jeu. La physique du rebond n'en dépend pas.</summary>
    public enum Surface
    {
        Sarcelle,
        Gazon,
    }

    /// <summary>Ambiance du décor et de la lumière.</summary>
    public enum Ambiance
    {
        /// <summary>Stade de club en plein jour : gradins autour du court, ciel bleu.</summary>
        Jour,

        /// <summary>« Soir des lampions » : haies, peupliers, lumière dorée.</summary>
        Soir,
    }

    /// <summary>
    /// Toutes les couleurs du jeu (charte « Soir des lampions », ART_DIRECTION.md) et les
    /// matériaux partagés. Un matériau est créé une seule fois par recette et par couleur.
    /// Le projet est en espace Gamma : un code hexadécimal donne la couleur attendue.
    /// </summary>
    public static class Palette
    {
        /// <summary>Surface du court, à choisir avant de construire le court.</summary>
        public static Surface Surface = Surface.Sarcelle;

        // --- Court et terrain ---
        public static readonly Color Sarcelle = Hex(0x1F8391);
        public static readonly Color SarcelleProfonde = Hex(0x1B7885);
        public static readonly Color Gazon = Hex(0x3F8F4A);
        public static readonly Color GazonTondu = Hex(0x378242);
        public static readonly Color Brique = Hex(0x9E4A31);
        public static readonly Color Creme = Hex(0xFFF4DC);
        public static readonly Color Encre = Hex(0x17262B);
        public static readonly Color BoisFonce = Hex(0x7A4E2D);
        public static readonly Color Pre = Hex(0x6E8C4A);

        // --- Décor et ciel ---
        public static readonly Color Haie = Hex(0x2C5446);
        public static readonly Color HaieClaire = Hex(0x3B6B55);
        public static readonly Color PeuplierSombre = Hex(0x24483C);
        public static readonly Color Peuplier = Hex(0x2F5D4A);
        public static readonly Color Tilleul = Hex(0x3D7357);
        public static readonly Color Tronc = Hex(0x5A3F2C);
        public static readonly Color BoisClair = Hex(0xD9A066);
        public static readonly Color BoisMiel = Hex(0xC98F58);
        public static readonly Color Corail = Hex(0xFF6B4A);
        public static readonly Color Rose = Hex(0xFF8FB0);
        public static readonly Color CremeChaude = Hex(0xFFF1D0);
        public static readonly Color Turquoise = Hex(0x3FD0C9);
        public static readonly Color CollineProche = Hex(0x7FA0A8);
        public static readonly Color CollineLointaine = Hex(0x9DB7B5);
        public static readonly Color Nuage = Hex(0xFFE6D2);
        public static readonly Color CielHorizon = Hex(0xFFD9A8);
        public static readonly Color CielMilieu = Hex(0xF9C39A);
        public static readonly Color CielHaut = Hex(0x9CBFD4);
        public static readonly Color Brume = Hex(0xF3CBA4);

        // --- Lumière du soir ---
        // Teintes de la charte, assombries : avec le soleil, la somme reçue par le sol doit
        // rester proche de 1, sinon le court « sombre et froid » devient turquoise vif.
        public static readonly Color Soleil = Hex(0xFFE3BD);
        public static readonly Color AmbianceCiel = Hex(0x657585);
        public static readonly Color AmbianceHorizon = Hex(0x806E5B);
        public static readonly Color AmbianceSol = Hex(0x4B3B32);

        // --- Stade de jour : ciel, lumière et tribunes ---
        public static readonly Color PreJour = Hex(0x5C8F52);
        public static readonly Color CielJourHorizon = Hex(0xCFE8F5);
        public static readonly Color CielJourHaut = Hex(0x5BA8E0);
        public static readonly Color NuageJour = Hex(0xFFFFFF);
        public static readonly Color BrumeJour = Hex(0xD5E8F0);
        public static readonly Color SoleilJour = Hex(0xFFF4E0);
        public static readonly Color AmbianceJourCiel = Hex(0x5C6B80);
        public static readonly Color AmbianceJourHorizon = Hex(0x6E7478);
        public static readonly Color AmbianceJourSol = Hex(0x4A4A40);
        public static readonly Color TribuneSombre = Hex(0x5E3D26);

        // --- Personnages et raquettes ---
        public static readonly Color BoisNaturel = Hex(0xE2B88A);
        public static readonly Color Framboise = Hex(0xE0457B);
        public static readonly Color Lavande = Hex(0xC4B0FF);
        public static readonly Color Prune = Hex(0x4A3580);
        public static readonly Color PruneNuit = Hex(0x2E2350);
        public static readonly Color Cordage = Hex(0xF4EFE2);
        public static readonly Color Blanc = Hex(0xFFFFFF);

        // --- Balle et effets ---
        public static readonly Color JauneBalle = Hex(0xD9F233);
        public static readonly Color EmissionBalle = Hex(0x4A5212);
        public static readonly Color BalleMorte = Hex(0x9AA08A);
        public static readonly Color Ombre = Hex(0x0B1D22, 0.45f);
        public static readonly Color TraineePlat = Hex(0xFFF4DC);
        public static readonly Color TraineeLift = Hex(0xFF9475);
        public static readonly Color TraineeSlice = Hex(0xBDF2EE);
        public static readonly Color MentheClaire = Hex(0x7BE8A8);

        // --- Interface ---
        public static readonly Color Ardoise = Hex(0x1F2A30);
        public static readonly Color Cellule = Hex(0x31424B);
        public static readonly Color Piste = Hex(0x45585F);
        public static readonly Color GrisClair = Hex(0xB8C6C4);
        public static readonly Color Moutarde = Hex(0xFFC53D);
        public static readonly Color Menthe = Hex(0x4FD08A);
        public static readonly Color Orange = Hex(0xFF9F45);
        public static readonly Color Rouge = Hex(0xB8322A);

        /// <summary>Bandes paires du court, selon la surface choisie.</summary>
        public static Color Court => Surface == Surface.Gazon ? Gazon : Sarcelle;

        /// <summary>Bandes impaires du court, selon la surface choisie.</summary>
        public static Color CourtStripe => Surface == Surface.Gazon ? GazonTondu : SarcelleProfonde;

        /// <summary>Couleur de la traînée et de l'anneau d'impact selon l'effet (de -1, slice, à +1, lift).</summary>
        public static Color TrailFor(float spin)
        {
            if (spin > 0.25f)
            {
                return TraineeLift;
            }

            return spin < -0.25f ? TraineeSlice : TraineePlat;
        }

        public static Color Hex(uint rgb, float alpha = 1f)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        // ---------------------------------------------------------------- Matériaux

        const string LitShader = "Standard";
        const string VeilShader = "Sprites/Default";

        static readonly Dictionary<uint, Material> MatCache = new Dictionary<uint, Material>();
        static readonly Dictionary<ulong, Material> LuminousCache = new Dictionary<ulong, Material>();
        static readonly Dictionary<uint, Material> VeilCache = new Dictionary<uint, Material>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCaches()
        {
            MatCache.Clear();
            LuminousCache.Clear();
            VeilCache.Clear();
        }

        /// <summary>Recette « Mat » : matériau éclairé, sans reflet. Partagé par couleur.</summary>
        public static Material Mat(Color color)
        {
            uint key = Key(color);
            if (!MatCache.TryGetValue(key, out Material material) || material == null)
            {
                material = NewMat(color);
                MatCache[key] = material;
            }

            return material;
        }

        /// <summary>Recette « Lumineux » : Mat avec une émission. Partagé par couple de couleurs.</summary>
        public static Material Lumineux(Color color, Color emission)
        {
            ulong key = ((ulong)Key(color) << 32) | Key(emission);
            if (!LuminousCache.TryGetValue(key, out Material material) || material == null)
            {
                material = NewLumineux(color, emission);
                LuminousCache[key] = material;
            }

            return material;
        }

        /// <summary>Recette « Voile » : non éclairé, transparent, sans brume. Partagé par couleur.</summary>
        public static Material Voile(Color color)
        {
            uint key = Key(color);
            if (!VeilCache.TryGetValue(key, out Material material) || material == null)
            {
                material = NewVoile(color);
                VeilCache[key] = material;
            }

            return material;
        }

        /// <summary>Matériau Mat qui n'appartient qu'à un objet (sa couleur va changer).</summary>
        public static Material NewMat(Color color)
        {
            var material = new Material(Shader.Find(LitShader)) { name = "Mat " + ColorUtility.ToHtmlStringRGB(color) };
            material.color = color;
            material.SetFloat("_Glossiness", 0.08f);
            material.SetFloat("_Metallic", 0f);
            return material;
        }

        /// <summary>Matériau Lumineux qui n'appartient qu'à un objet (son émission va changer).</summary>
        public static Material NewLumineux(Color color, Color emission)
        {
            Material material = NewMat(color);
            material.name = "Lumineux " + ColorUtility.ToHtmlStringRGB(color);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            material.SetColor("_EmissionColor", emission);
            return material;
        }

        /// <summary>Matériau Voile qui n'appartient qu'à un objet (sa transparence va changer).</summary>
        public static Material NewVoile(Color color)
        {
            var material = new Material(Shader.Find(VeilShader)) { name = "Voile " + ColorUtility.ToHtmlStringRGBA(color) };
            material.color = color;
            return material;
        }

        /// <summary>Prépare un objet en recette Voile : il ne porte ni ne reçoit d'ombre.</summary>
        public static void AsVeil(Renderer renderer, Material material)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        static uint Key(Color color)
        {
            Color32 c = color;
            return ((uint)c.r << 24) | ((uint)c.g << 16) | ((uint)c.b << 8) | c.a;
        }
    }
}
