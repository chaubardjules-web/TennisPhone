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

        [Header("Joueur : déplacement par webcam")]
        [Tooltip("Si coché, les pas de côté du joueur devant la webcam du PC déplacent son personnage pendant l'échange. "
            + "La touche C et le bouton « Caméra » du téléphone le changent en jeu.")]
        public bool webcamMovement = true;

        [Tooltip("Largeur de court (m) parcourue quand le joueur traverse toute l'image de la webcam. "
            + "Plus elle est grande, moins il faut bouger.")]
        [Min(1f)] public float webcamSensitivity = 15f;

        [Tooltip("Aide au placement (0 à 1) quand une balle arrive : 0, le joueur se place seul ; 1, le jeu le place entièrement.")]
        [Range(0f, 1f)] public float webcamAssist = 0.25f;

        [Tooltip("Distance (m) au-delà des lignes de côté jusqu'où le joueur peut aller.")]
        [Min(0f)] public float webcamSideMargin = 2f;

        [Tooltip("Écart de luminosité (0 à 255) entre deux images pour qu'un point de l'image compte comme en mouvement.")]
        [Range(4f, 80f)] public float webcamMotionThreshold = 18f;

        [Tooltip("Durée (s) pendant laquelle un point qui a bougé reste pris en compte.")]
        [Min(0.05f)] public float webcamMemorySeconds = 0.5f;

        [Tooltip("Part de l'image (0 à 1) qui doit avoir bougé pour mettre la position à jour : "
            + "assez haute pour qu'un bras seul ne compte pas.")]
        [Range(0.002f, 0.2f)] public float webcamMinCoverage = 0.03f;

        [Tooltip("Temps de lissage (s) de la position vue par la webcam.")]
        [Min(0f)] public float webcamSmoothingSeconds = 0.12f;

        [Tooltip("Durée (s) sans mouvement devant la webcam au bout de laquelle le placement redevient automatique.")]
        [Min(1f)] public float webcamHoldSeconds = 20f;

        [Tooltip("Vitesse de rotation du téléphone (degrés par seconde) à partir de laquelle la position est figée : "
            + "le bras bouge, pas le corps.")]
        [Min(0f)] public float webcamFreezeRotation = 200f;

        [Tooltip("Durée (s) pendant laquelle la position reste figée après un geste du bras.")]
        [Min(0f)] public float webcamFreezeSeconds = 0.35f;

        [Header("Match")]
        [Tooltip("Nombre de jeux à gagner pour remporter le set.")]
        [Min(1)] public int gamesToWinSet = 3;

        [Tooltip("Si coché, il faut aussi deux jeux d'écart pour gagner le set.")]
        public bool setRequiresTwoGameLead;

        [Tooltip("Si coché, le joueur sert le premier jeu.")]
        public bool playerServesFirst = true;

        [Tooltip("Pause (s) entre la fin d'un point et le service suivant.")]
        [Min(0.3f)] public float pointPause = 2.2f;

        [Header("Service : lancer")]
        [Tooltip("Inclinaison (degrés) sous laquelle le téléphone doit repasser avant un nouveau lancer.")]
        [Range(-30f, 60f)] public float tossArmElevation = 30f;

        [Tooltip("Inclinaison (degrés) du téléphone qui déclenche le lancer : il pointe vers le haut.")]
        [Range(20f, 85f)] public float tossRaiseElevation = 55f;

        [Tooltip("Hauteur (m) d'où la balle est lancée.")]
        [Min(0.5f)] public float tossReleaseHeight = 1.3f;

        [Tooltip("Vitesse verticale (m/s) du lancer pour un lever lent du téléphone : la balle monte peu et retombe vite.")]
        [Min(2f)] public float tossSpeedMin = 6.1f;

        [Tooltip("Vitesse verticale (m/s) du lancer pour un lever vif : la balle monte haut et laisse plus de temps.")]
        [Min(2f)] public float tossSpeedMax = 7.6f;

        [Tooltip("Vitesse du lever du téléphone (degrés par seconde) qui donne le lancer le plus bas.")]
        [Min(0f)] public float tossRateMin = 60f;

        [Tooltip("Vitesse du lever du téléphone (degrés par seconde) qui donne le lancer le plus haut.")]
        [Min(1f)] public float tossRateMax = 300f;

        [Tooltip("Vigueur (0 à 1) du lancer fait avec le bouton du téléphone ou le clavier, et de celui de l'adversaire.")]
        [Range(0f, 1f)] public float tossDefaultStrength = 0.35f;

        [Tooltip("Hauteur (m) sous laquelle un lancer non frappé est perdu et doit être refait.")]
        [Min(0.3f)] public float tossMinHeight = 1.5f;

        [Header("Service : frappe")]
        [Tooltip("Hauteur (m) où la balle lancée doit être frappée : l'instant idéal est celui où elle y redescend.")]
        [Min(1f)] public float serveContactHeight = 3.1f;

        [Tooltip("Écart maximal (s) autour du délai idéal pour que le service parte.")]
        [Min(0.05f)] public float serveTimingWindow = 0.4f;

        [Tooltip("Vitesse du service (m/s) pour un swing de puissance 0.")]
        [Min(5f)] public float serveSpeedMin = 22f;

        [Tooltip("Vitesse du service (m/s) pour un swing de puissance 1.")]
        [Min(5f)] public float serveSpeedMax = 38f;

        [Tooltip("Marge (m) au-dessus du filet visée au service.")]
        [Min(0f)] public float serveNetMargin = 0.25f;

        [Tooltip("Distance (m) entre le point visé et la ligne de service, pour un timing parfait.")]
        [Min(0f)] public float serveDepthMargin = 1f;

        [Tooltip("Décalage latéral (m) du point visé pour un swing en bord de fenêtre.")]
        [Min(0f)] public float serveAimSpread = 1.7f;

        [Tooltip("Allongement (m) du service pour un swing en bord de fenêtre : un mauvais timing envoie la balle trop longue.")]
        [Min(0f)] public float serveLongError = 2f;

        [Tooltip("Distance (m) entre le serveur et la marque centrale.")]
        [Min(0.2f)] public float serverOffset = 1.2f;

        [Tooltip("Délai (s) avant que l'adversaire lance sa balle de service.")]
        [Min(0.3f)] public float aiServeDelay = 1.6f;

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
            serveSpeed = 24f,
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
            serveSpeed = 29f,
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
            serveSpeed = 34f,
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
