using UnityEngine;

namespace TennisPhone
{
    /// <summary>
    /// Toutes les constantes de gameplay, réglables dans l'inspecteur sans toucher au code.
    /// L'asset se trouve dans Assets/TennisPhone/Config/GameplayConfig.asset.
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
        [Min(0f)] public float swingRotationThreshold = 220f;

        [Tooltip("Le swing se termine quand l'accélération ou la rotation retombe sous cette fraction de son seuil.")]
        [Range(0.1f, 0.95f)] public float swingReleaseRatio = 0.6f;

        [Tooltip("Durée minimale d'un geste (secondes). En dessous, c'est une simple secousse.")]
        [Min(0f)] public float swingMinDuration = 0.03f;

        [Tooltip("Durée maximale analysée (secondes). Le swing est validé à cette durée même s'il continue.")]
        [Min(0.05f)] public float swingMaxDuration = 0.25f;

        [Tooltip("Pause après un swing (secondes) pendant laquelle aucun autre swing n'est compté.")]
        [Min(0f)] public float swingCooldown = 0.35f;

        [Tooltip("À cocher si coup droit et revers sont inversés.")]
        public bool invertSwingSide;

        [Header("Swing : puissance")]
        [Tooltip("Pic d'accélération (m/s²) qui donne une puissance de 0.")]
        public float powerAccelerationMin = 12f;

        [Tooltip("Pic d'accélération (m/s²) qui donne une puissance de 1.")]
        public float powerAccelerationMax = 55f;

        [Tooltip("Pic de vitesse angulaire (°/s) qui donne une puissance de 0.")]
        public float powerRotationMin = 220f;

        [Tooltip("Pic de vitesse angulaire (°/s) qui donne une puissance de 1.")]
        public float powerRotationMax = 1100f;

        [Tooltip("Part de la vitesse angulaire dans la puissance (le reste vient de l'accélération).")]
        [Range(0f, 1f)] public float powerRotationWeight = 0.5f;

        [Header("Swing : effet (lift / slice)")]
        [Tooltip("Vitesse de rotation du poignet (°/s) qui donne un effet maximal.")]
        [Min(1f)] public float spinRollRateFull = 500f;

        [Tooltip("En dessous de cette valeur d'effet (0 à 1), la frappe est considérée à plat.")]
        [Range(0f, 0.9f)] public float spinDeadZone = 0.15f;

        [Tooltip("À cocher si lift et slice sont inversés.")]
        public bool invertSpin;

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
                MinDuration = swingMinDuration,
                MaxDuration = swingMaxDuration,
                Cooldown = swingCooldown,
                PowerAccelerationMin = powerAccelerationMin,
                PowerAccelerationMax = powerAccelerationMax,
                PowerRotationMin = powerRotationMin,
                PowerRotationMax = powerRotationMax,
                PowerRotationWeight = powerRotationWeight,
                SpinRollRateFull = spinRollRateFull,
                SpinDeadZone = spinDeadZone,
                InvertSide = invertSwingSide,
                InvertSpin = invertSpin,
            };
        }
    }
}
