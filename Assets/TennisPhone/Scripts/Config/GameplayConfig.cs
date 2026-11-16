using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Toutes les constantes de gameplay, réglables dans l'inspecteur sans toucher au code.
    /// L'asset se trouve dans Assets/TennisPhone/Config/GameplayConfig.asset.
    /// Les valeurs par défaut du swing viennent d'enregistrements réels sur iPhone.
    /// </summary>
    [CreateAssetMenu(menuName = "TennisPhone/Gameplay Config", fileName = "GameplayConfig")]
    public sealed class GameplayConfig : ScriptableObject
    {
        [Header("Joueur")]
        [Tooltip("Main qui tient le téléphone. Sert à distinguer coup droit et revers.")]
        public Handedness handedness = Handedness.Right;

        [Header("Swing : détection")]
        [Tooltip("Accélération minimale (m/s², sans la gravité) pour qu'un geste compte comme un swing.")]
        [Min(0f)] public float swingAccelerationThreshold = 12f;

        [Tooltip("Vitesse angulaire minimale (degrés par seconde) pour qu'un geste compte comme un swing.")]
        [Min(0f)] public float swingRotationThreshold = 280f;

        [Tooltip("Le geste est abandonné ou terminé quand l'accélération ou la rotation retombe sous cette fraction de son seuil.")]
        [Range(0.1f, 0.95f)] public float swingReleaseRatio = 0.6f;

        [Tooltip("L'impact est déclenché dès que le geste retombe sous cette fraction de son maximum. "
            + "Plus haut : frappe plus réactive, mais puissance parfois sous-estimée.")]
        [Range(0.3f, 0.98f)] public float swingImpactFallRatio = 0.85f;

        [Tooltip("Durée minimale d'un geste (secondes). En dessous, c'est une simple secousse.")]
        [Min(0f)] public float swingMinDuration = 0.05f;

        [Tooltip("Durée maximale de la montée du geste (secondes). L'impact est déclenché à cette durée au plus tard.")]
        [Min(0.05f)] public float swingMaxDuration = 0.6f;

        [Tooltip("Durée de l'accompagnement après l'impact (secondes), pendant laquelle un slice peut encore être reconnu.")]
        [Min(0f)] public float swingFollowThroughDuration = 0.3f;

        [Tooltip("Pause après un impact (secondes) pendant laquelle aucun autre swing n'est compté.")]
        [Min(0f)] public float swingCooldown = 0.4f;

        [Tooltip("À cocher si coup droit et revers sont inversés.")]
        public bool invertSwingSide;

        [Header("Swing : puissance")]
        [Tooltip("Pic d'accélération (m/s²) qui donne une puissance de 0.")]
        public float powerAccelerationMin = 12f;

        [Tooltip("Pic d'accélération (m/s²) qui donne une puissance de 1.")]
        public float powerAccelerationMax = 45f;

        [Tooltip("Pic de vitesse angulaire (°/s) qui donne une puissance de 0.")]
        public float powerRotationMin = 280f;

        [Tooltip("Pic de vitesse angulaire (°/s) qui donne une puissance de 1.")]
        public float powerRotationMax = 1000f;

        [Tooltip("Part de la vitesse angulaire dans la puissance (le reste vient de l'accélération).")]
        [Range(0f, 1f)] public float powerRotationWeight = 0.5f;

        [Header("Swing : lift (le poignet roule par-dessus la balle)")]
        [Tooltip("Vitesse moyenne de rotation du poignet (°/s) à partir de laquelle le coup commence à être lifté.")]
        [Min(0f)] public float liftRollRateMin = 250f;

        [Tooltip("Vitesse moyenne de rotation du poignet (°/s) qui donne un lift maximal.")]
        [Min(1f)] public float liftRollRateFull = 550f;

        [Tooltip("À cocher si le lift se déclenche en tournant le poignet dans le mauvais sens.")]
        public bool invertLift;

        [Header("Swing : slice (le tamis s'ouvre vers le ciel en fin de geste)")]
        [Tooltip("Ouverture du tamis (0 à 2) à partir de laquelle le coup commence à être slicé. "
            + "1 correspond à un quart de tour vers le ciel.")]
        [Range(0f, 2f)] public float sliceFaceOpeningMin = 0.6f;

        [Tooltip("Ouverture du tamis (0 à 2) qui donne un slice maximal.")]
        [Range(0.1f, 2f)] public float sliceFaceOpeningFull = 1f;

        [Tooltip("À cocher si le slice se déclenche en fermant le tamis au lieu de l'ouvrir "
            + "(téléphone tenu avec l'écran de l'autre côté).")]
        public bool invertSlice;

        [Header("Clavier / souris (test sans téléphone)")]
        [Tooltip("Vitesse de la souris (pixels par seconde) qui donne une puissance de 1 au clic.")]
        [Min(1f)] public float mouseSpeedForFullPower = 3000f;

        [Tooltip("Puissance minimale d'un swing à la souris, même sans bouger.")]
        [Range(0f, 1f)] public float mouseMinPower = 0.35f;

        [Tooltip("Puissance d'un swing déclenché avec la barre d'espace.")]
        [Range(0f, 1f)] public float keyboardPower = 0.6f;

        [Tooltip("Effet appliqué quand la flèche haut (lift) ou bas (slice) est maintenue.")]
        [Range(0f, 1f)] public float keyboardSpin = 0.7f;

        public SwingSettings ToSwingSettings()
        {
            return new SwingSettings
            {
                Handedness = handedness,
                AccelerationThreshold = swingAccelerationThreshold,
                RotationThreshold = swingRotationThreshold,
                ReleaseRatio = swingReleaseRatio,
                ImpactFallRatio = swingImpactFallRatio,
                MinDuration = swingMinDuration,
                MaxDuration = swingMaxDuration,
                FollowThroughDuration = swingFollowThroughDuration,
                Cooldown = swingCooldown,
                PowerAccelerationMin = powerAccelerationMin,
                PowerAccelerationMax = powerAccelerationMax,
                PowerRotationMin = powerRotationMin,
                PowerRotationMax = powerRotationMax,
                PowerRotationWeight = powerRotationWeight,
                LiftRollRateMin = liftRollRateMin,
                LiftRollRateFull = liftRollRateFull,
                SliceFaceOpeningMin = sliceFaceOpeningMin,
                SliceFaceOpeningFull = sliceFaceOpeningFull,
                InvertSide = invertSwingSide,
                InvertLift = invertLift,
                InvertSlice = invertSlice,
            };
        }
    }
}
