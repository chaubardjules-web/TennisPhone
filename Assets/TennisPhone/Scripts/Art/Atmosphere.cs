using UnityEngine;
using UnityEngine.Rendering;

namespace TennisPhone
{
    /// <summary>
    /// Lumière, ciel de repli et brume d'une ambiance. Appliqué par SceneBuilder quand la scène
    /// est créée, puis au lancement selon l'ambiance choisie.
    /// </summary>
    public static class Atmosphere
    {
        public static void Apply(Ambiance ambiance, Camera view, Light sun)
        {
            bool day = ambiance == Ambiance.Jour;

            if (view != null)
            {
                // Le cadrage ne change pas : seuls le fond et la profondeur de vue sont réglés.
                view.clearFlags = CameraClearFlags.SolidColor;
                view.backgroundColor = day ? Palette.CielJourHorizon : Palette.CielHorizon;
                view.farClipPlane = 180f;
            }

            if (sun != null)
            {
                // Le soleil est derrière la caméra, à gauche : tout ce qu'elle voit de face est
                // éclairé, et les ombres du décor restent sur les abords.
                sun.type = LightType.Directional;
                sun.transform.rotation = Quaternion.Euler(day ? 50f : 38f, 28f, 0f);
                sun.color = day ? Palette.SoleilJour : Palette.Soleil;
                // Soleil et lumière d'ambiance s'additionnent : leur somme sur le sol reste proche
                // de 1, pour que les couleurs de la palette arrivent telles quelles à l'écran.
                sun.intensity = day ? 0.9f : 0.85f;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.6f;
                sun.shadowBias = 0.05f;
                sun.shadowNormalBias = 0.4f;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = day ? Palette.AmbianceJourCiel : Palette.AmbianceCiel;
            RenderSettings.ambientEquatorColor = day ? Palette.AmbianceJourHorizon : Palette.AmbianceHorizon;
            RenderSettings.ambientGroundColor = day ? Palette.AmbianceJourSol : Palette.AmbianceSol;
            RenderSettings.reflectionIntensity = 0f;

            // La brume commence derrière le fond du court : elle ne touche ni le court ni les joueurs.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = day ? Palette.BrumeJour : Palette.Brume;
            RenderSettings.fogStartDistance = 45f;
            RenderSettings.fogEndDistance = 160f;
        }

        /// <summary>Portée des ombres, réglée au lancement pour tous les niveaux de qualité.</summary>
        public static void ApplyShadowQuality()
        {
            QualitySettings.shadowDistance = 70f;
            QualitySettings.shadowCascades = 2;
        }
    }
}
