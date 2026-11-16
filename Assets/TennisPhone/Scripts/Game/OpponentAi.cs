using System;
using UnityEngine;

namespace TennisPhone
{
    public enum AiDifficulty
    {
        Easy,
        Normal,
        Hard,
    }

    /// <summary>Réglages d'un niveau de difficulté de l'adversaire.</summary>
    [Serializable]
    public sealed class AiLevel
    {
        [Tooltip("Temps (s) entre la frappe du joueur et le départ de l'adversaire.")]
        [Min(0f)] public float reactionDelay = 0.33f;

        [Tooltip("Erreur (m) de lecture du point de chute au départ. Elle se corrige pendant que la balle approche.")]
        [Min(0f)] public float positionError = 0.5f;

        [Tooltip("Vitesse de course par rapport à celle du joueur.")]
        [Range(0.3f, 1.5f)] public float speedMultiplier = 1f;

        [Tooltip("Vitesse minimale (m/s) de ses frappes.")]
        [Min(1f)] public float shotSpeedMin = 16f;

        [Tooltip("Vitesse maximale (m/s) de ses frappes.")]
        [Min(1f)] public float shotSpeedMax = 22f;

        [Tooltip("Erreur maximale de visée (degrés) sur un coup imprécis.")]
        [Min(0f)] public float aimErrorDegrees = 3.5f;

        [Tooltip("Proportion de coups imprécis quand il est bien placé.")]
        [Range(0f, 1f)] public float sloppyShotRate = 0.2f;

        [Tooltip("Multiplicateur de l'erreur de visée quand il frappe en bout de course.")]
        [Min(1f)] public float stretchedErrorFactor = 2f;

        [Tooltip("Temps (s) dont il a besoin, une fois placé, pour frapper proprement. "
            + "S'il arrive plus tard que ça, il frappe en bout de course.")]
        [Min(0f)] public float settleTime = 0.3f;

        [Tooltip("Probabilité de viser le côté opposé à celui où se trouve le joueur.")]
        [Range(0f, 1f)] public float awayProbability = 0.6f;
    }

    /// <summary>Une frappe choisie par l'adversaire.</summary>
    public struct AiShot
    {
        public Vector3 Target;
        public ShotSolution Solution;

        /// <summary>Vrai si le coup a été joué avec l'erreur de visée complète.</summary>
        public bool Sloppy;
    }

    /// <summary>
    /// Cerveau de l'adversaire : lit la trajectoire avec un temps de réaction et une erreur,
    /// court vers la balle, puis choisit une frappe plus ou moins précise.
    /// Sans dépendance à la scène, pour pouvoir le tester et le simuler.
    /// </summary>
    public sealed class OpponentAi
    {
        const float InPositionDistance = 0.6f;

        readonly System.Random random;
        Vector3 readError;
        Vector3 contactPoint;
        double planTime;
        double contactTime;
        double arrivedAt;
        bool tracking;
        float hittingSide = 1f;

        public OpponentAi(int seed)
        {
            random = new System.Random(seed);
        }

        public Vector3 Position { get; private set; }

        /// <summary>Côté (en x) où il frappe la balle par rapport à son corps.</summary>
        public float HittingSide => hittingSide;

        public void Reset(Vector3 home)
        {
            Position = home;
            tracking = false;
        }

        /// <summary>La balle vient d'être frappée vers lui : il mémorise où et quand la jouer.</summary>
        public void OnIncoming(Vector3 predictedContact, double predictedTime, double now, AiLevel level)
        {
            tracking = true;
            arrivedAt = double.NaN;
            contactPoint = predictedContact;
            contactTime = predictedTime;
            planTime = now;

            float angle = Range(0f, Mathf.PI * 2f);
            float radius = Range(0f, level.positionError);
            readError = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            hittingSide = predictedContact.x >= Position.x ? 1f : -1f;
        }

        /// <summary>Il n'a plus de balle à jouer : il retourne se replacer.</summary>
        public void StopTracking()
        {
            tracking = false;
        }

        /// <summary>Déplace l'adversaire pendant « deltaTime » secondes.</summary>
        public void Move(double now, float deltaTime, Vector3 home, float baseSpeed, float sideOffset, AiLevel level)
        {
            Vector3 target = home;
            if (tracking)
            {
                if (now - planTime < level.reactionDelay)
                {
                    return;
                }

                target = IdealPosition(now, sideOffset);
            }

            Position = Vector3.MoveTowards(Position, target, baseSpeed * level.speedMultiplier * deltaTime);

            // On note quand il arrive en place : frapper juste après, c'est frapper sous pression.
            if (tracking && double.IsNaN(arrivedAt) && Vector3.Distance(Position, target) <= InPositionDistance)
            {
                arrivedAt = now;
            }
        }

        /// <summary>
        /// Position où il veut se trouver. Son erreur de lecture diminue à mesure que la balle approche.
        /// </summary>
        Vector3 IdealPosition(double now, float sideOffset)
        {
            double flight = Math.Max(contactTime - planTime, 0.05);
            float remaining = Mathf.Clamp01((float)((contactTime - now) / flight));
            Vector3 perceived = contactPoint + readError * remaining;

            // Il se place à côté de la balle et un peu derrière elle (z plus grand, de son côté).
            return new Vector3(perceived.x - hittingSide * sideOffset, 0f, Mathf.Max(perceived.z + 0.25f, 1.5f));
        }

        /// <summary>Point d'où part sa raquette, pour savoir s'il peut atteindre la balle.</summary>
        public Vector3 HandPosition(float sideOffset)
        {
            return Position + new Vector3(hittingSide * sideOffset, 0f, 0f);
        }

        public bool CanReach(Vector3 ballPosition, float sideOffset, float reach)
        {
            Vector3 hand = HandPosition(sideOffset);
            float dx = hand.x - ballPosition.x;
            float dz = hand.z - ballPosition.z;
            return dx * dx + dz * dz <= reach * reach;
        }

        /// <summary>
        /// Vrai s'il frappe en bout de course : pas encore arrivé à sa position idéale,
        /// ou arrivé depuis trop peu de temps pour s'installer.
        /// </summary>
        public bool IsStretched(double now, float sideOffset, AiLevel level)
        {
            Vector3 ideal = IdealPosition(now, sideOffset);
            if (Vector3.Distance(Position, ideal) > InPositionDistance || double.IsNaN(arrivedAt))
            {
                return true;
            }

            return now - arrivedAt < level.settleTime;
        }

        /// <summary>
        /// Choisit une frappe : de préférence loin du joueur, avec une erreur de visée qui dépend
        /// du niveau et de sa position.
        /// </summary>
        public AiShot ChooseShot(Vector3 ballPosition, Vector3 playerPosition, bool stretched, AiLevel level,
            GameplayConfig config, in BallSettings ball, in CourtDimensions court)
        {
            // Côté visé : le plus souvent à l'opposé du joueur.
            float playerSide = Mathf.Abs(playerPosition.x) < 0.5f ? (Chance(0.5f) ? 1f : -1f) : Mathf.Sign(playerPosition.x);
            float aimSide = Chance(level.awayProbability) ? -playerSide : playerSide;

            var target = new Vector3(
                aimSide * Range(1.2f, court.HalfWidth - 0.9f),
                0f,
                -Range(court.HalfLength * 0.62f, court.HalfLength - 1.3f));

            float speed = Range(level.shotSpeedMin, level.shotSpeedMax);
            float spinRate = Range(400f, 2200f) * 2f * Mathf.PI / 60f;
            ShotSolution solution = ShotSolver.Solve(ballPosition, target, speed, spinRate, config.shotNetMargin + 0.15f, ball, court);

            // Erreur de visée : complète en bout de course ou sur un coup imprécis, réduite sinon.
            bool sloppy = stretched || Chance(level.sloppyShotRate);
            float error = level.aimErrorDegrees * (stretched ? level.stretchedErrorFactor : 1f) * (sloppy ? 1f : 0.3f);
            float yaw = Range(-error, error);
            float pitch = Range(-error, error) * 0.7f;

            Vector3 velocity = solution.Velocity;
            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            Vector3 pitchAxis = horizontal.sqrMagnitude > 1e-6f ? Vector3.Cross(Vector3.up, horizontal.normalized) : Vector3.right;
            Quaternion rotation = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(-pitch, pitchAxis);
            solution.Velocity = rotation * velocity;
            solution.Spin = rotation * solution.Spin;

            return new AiShot { Target = target, Solution = solution, Sloppy = sloppy };
        }

        float Range(float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }

        bool Chance(float probability)
        {
            return random.NextDouble() < probability;
        }
    }
}
