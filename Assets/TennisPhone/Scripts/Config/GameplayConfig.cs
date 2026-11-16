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

        [Header("Court (mètres, règles officielles)")]
        [Tooltip("Longueur totale du court.")]
        [Min(1f)] public float courtLength = 23.77f;

        [Tooltip("Largeur du court en simple.")]
        [Min(1f)] public float courtWidth = 8.23f;

        [Tooltip("Distance entre le filet et la ligne de service.")]
        [Min(0.5f)] public float serviceLineDistance = 6.4f;

        [Tooltip("Hauteur du filet au centre.")]
        [Min(0.1f)] public float netHeightCenter = 0.914f;

        [Tooltip("Hauteur du filet aux poteaux.")]
        [Min(0.1f)] public float netHeightPosts = 1.07f;

        [Tooltip("Distance entre la ligne de côté et le poteau du filet.")]
        [Min(0f)] public float netPostOffset = 0.914f;

        [Header("Balle : vol")]
        [Tooltip("Gravité (m/s²).")]
        [Min(0f)] public float gravity = 9.81f;

        [Tooltip("Rayon de la balle (m).")]
        [Min(0.005f)] public float ballRadius = 0.033f;

        [Tooltip("Masse de la balle (kg).")]
        [Min(0.001f)] public float ballMass = 0.0577f;

        [Tooltip("Densité de l'air (kg/m³).")]
        [Min(0f)] public float airDensity = 1.225f;

        [Tooltip("Coefficient de traînée : plus il est grand, plus la balle ralentit en l'air.")]
        [Min(0f)] public float dragCoefficient = 0.31f;

        [Tooltip("Coefficient de portance maximal : limite la courbure donnée par l'effet.")]
        [Min(0f)] public float liftCoefficientMax = 0.36f;

        [Tooltip("Temps (s) au bout duquel la rotation de la balle a nettement diminué.")]
        [Min(0.1f)] public float spinDecayTime = 7.4f;

        [Header("Balle : rebond et filet")]
        [Tooltip("Part de la vitesse verticale conservée au rebond (0 à 1).")]
        [Range(0f, 1f)] public float bounceRestitution = 0.74f;

        [Tooltip("Frottement du sol au rebond : plus il est grand, plus la balle est freinée.")]
        [Range(0f, 1.5f)] public float bounceFriction = 0.56f;

        [Tooltip("Répartition de la masse de la balle (0,4 = pleine, 0,67 = creuse). Règle l'échange entre vitesse et rotation au rebond.")]
        [Range(0.3f, 0.7f)] public float ballInertiaFactor = 0.55f;

        [Tooltip("Part de la vitesse conservée par une balle qui touche le filet.")]
        [Range(0f, 0.5f)] public float netRestitution = 0.1f;

        [Header("Frappe : timing et direction")]
        [Tooltip("Écart maximal (s) entre le swing et l'instant idéal pour toucher la balle.")]
        [Min(0.02f)] public float timingWindow = 0.18f;

        [Tooltip("Décalage latéral (m) du point visé pour un swing en bord de fenêtre : tôt = croisé, tard = long de ligne. "
            + "Au-delà de la demi-largeur du court (4,1 m), un swing très décalé envoie la balle en faute.")]
        [Min(0f)] public float aimMaxOffset = 4.8f;

        [Header("Frappe : vitesse et profondeur")]
        [Tooltip("Vitesse de la balle (m/s) pour un swing de puissance 0.")]
        [Min(1f)] public float shotSpeedMin = 14f;

        [Tooltip("Vitesse de la balle (m/s) pour un swing de puissance 1.")]
        [Min(1f)] public float shotSpeedMax = 27f;

        [Tooltip("Distance au filet (m) du point visé pour un swing de puissance 0.")]
        [Min(1f)] public float shotDepthMin = 6.5f;

        [Tooltip("Distance au filet (m) du point visé pour un swing de puissance 1.")]
        [Min(1f)] public float shotDepthMax = 10.8f;

        [Tooltip("Marge (m) au-dessus du filet visée pour une frappe à plat.")]
        [Min(0f)] public float shotNetMargin = 0.3f;

        [Header("Frappe : effets")]
        [Tooltip("Rotation de la balle (tours par minute) pour un lift maximal.")]
        [Min(0f)] public float liftRpmMax = 3000f;

        [Tooltip("Marge supplémentaire (m) au-dessus du filet pour un lift maximal.")]
        [Min(0f)] public float liftNetMarginBonus = 0.5f;

        [Tooltip("Vitesse d'un lift maximal par rapport à une frappe à plat.")]
        [Range(0.5f, 1.2f)] public float liftSpeedFactor = 0.95f;

        [Tooltip("Rotation de la balle (tours par minute) pour un slice maximal.")]
        [Min(0f)] public float sliceRpmMax = 2500f;

        [Tooltip("Vitesse d'un slice maximal par rapport à une frappe à plat.")]
        [Range(0.5f, 1.2f)] public float sliceSpeedFactor = 0.85f;

        [Header("Joueur : déplacement automatique")]
        [Tooltip("Vitesse de course (m/s).")]
        [Min(0.5f)] public float playerMoveSpeed = 5.5f;

        [Tooltip("Distance maximale (m) entre le joueur et la balle pour pouvoir la frapper.")]
        [Min(0.3f)] public float playerReach = 1.5f;

        [Tooltip("Hauteur (m) à laquelle le joueur cherche à frapper la balle après le rebond.")]
        [Min(0.2f)] public float contactHeight = 0.9f;

        [Tooltip("Distance maximale (m) derrière la ligne de fond où la balle est frappée. "
            + "Une balle longue est prise plus tôt, en montée, plutôt que de reculer davantage.")]
        [Min(0f)] public float contactMaxDepth = 2f;

        [Tooltip("Écart latéral (m) entre le joueur et la balle au moment de la frappe.")]
        [Min(0f)] public float playerSideOffset = 0.55f;

        [Tooltip("Distance (m) derrière la ligne de fond où le joueur se replace.")]
        [Min(0f)] public float playerHomeDepth = 0.8f;

        [Header("Engagement (en attendant le service)")]
        [Tooltip("Délai (s) entre la fin d'un point et l'engagement suivant.")]
        [Min(0.2f)] public float feederInterval = 1.5f;

        [Tooltip("Vitesse (m/s) de la balle d'engagement.")]
        [Min(5f)] public float feederSpeed = 16f;

        [Tooltip("Profondeur minimale (m depuis le filet) du rebond de la balle d'engagement.")]
        [Min(1f)] public float feederDepthMin = 6.5f;

        [Tooltip("Profondeur maximale (m depuis le filet) du rebond de la balle d'engagement.")]
        [Min(1f)] public float feederDepthMax = 10f;

        [Tooltip("Demi-largeur (m) de la zone visée à l'engagement.")]
        [Min(0f)] public float feederHalfWidth = 3f;

        [Header("Adversaire")]
        [Tooltip("Niveau de l'adversaire au lancement. La touche D le change en jeu.")]
        public AiDifficulty aiDifficulty = AiDifficulty.Normal;

        public AiLevel aiEasy = new AiLevel
        {
            reactionDelay = 0.6f,
            positionError = 1f,
            speedMultiplier = 0.7f,
            shotSpeedMin = 14f,
            shotSpeedMax = 18f,
            aimErrorDegrees = 12f,
            sloppyShotRate = 0.45f,
            settleTime = 0.45f,
            stretchedErrorFactor = 1.5f,
            awayProbability = 0.4f,
        };

        public AiLevel aiNormal = new AiLevel
        {
            reactionDelay = 0.33f,
            positionError = 0.5f,
            speedMultiplier = 0.85f,
            shotSpeedMin = 16f,
            shotSpeedMax = 22f,
            aimErrorDegrees = 9f,
            sloppyShotRate = 0.3f,
            settleTime = 0.3f,
            stretchedErrorFactor = 1.5f,
            awayProbability = 0.6f,
        };

        public AiLevel aiHard = new AiLevel
        {
            reactionDelay = 0.12f,
            positionError = 0.2f,
            speedMultiplier = 0.97f,
            shotSpeedMin = 19f,
            shotSpeedMax = 26f,
            aimErrorDegrees = 6f,
            sloppyShotRate = 0.15f,
            settleTime = 0.2f,
            stretchedErrorFactor = 1.5f,
            awayProbability = 0.8f,
        };

        public AiLevel GetAiLevel(AiDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AiDifficulty.Easy: return aiEasy;
                case AiDifficulty.Hard: return aiHard;
                default: return aiNormal;
            }
        }

        public CourtDimensions ToCourt()
        {
            return new CourtDimensions
            {
                HalfLength = courtLength * 0.5f,
                HalfWidth = courtWidth * 0.5f,
                ServiceLine = serviceLineDistance,
                NetHeightCenter = netHeightCenter,
                NetHeightPosts = netHeightPosts,
                NetHalfWidth = courtWidth * 0.5f + netPostOffset,
            };
        }

        public BallSettings ToBallSettings()
        {
            return new BallSettings
            {
                Gravity = gravity,
                Radius = ballRadius,
                AirFactor = airDensity * Mathf.PI * ballRadius * ballRadius / (2f * ballMass),
                DragCoefficient = dragCoefficient,
                LiftCoefficientMax = liftCoefficientMax,
                SpinDecayTime = spinDecayTime,
                BounceRestitution = bounceRestitution,
                BounceFriction = bounceFriction,
                InertiaFactor = ballInertiaFactor,
                NetRestitution = netRestitution,
            };
        }

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
