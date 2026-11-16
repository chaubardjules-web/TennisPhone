using UnityEngine;

namespace TennisPhone
{
    /// <summary>Une frappe calculée : vitesse et rotation à donner à la balle.</summary>
    public struct ShotSolution
    {
        public Vector3 Velocity;
        public Vector3 Spin;

        /// <summary>Faux si aucune trajectoire ne passe le filet avec la marge demandée.</summary>
        public bool ClearsNet;

        public float Speed;
    }

    /// <summary>
    /// Calcule la frappe qui envoie la balle vers un point visé.
    /// Pour chaque angle de départ, on cherche la vitesse qui fait retomber la balle sur la cible,
    /// puis on garde la trajectoire qui passe le filet et dont la vitesse est la plus proche
    /// de celle demandée.
    /// </summary>
    public static class ShotSolver
    {
        const float MinAngle = -10f;
        const float MaxAngle = 55f;
        const float AngleStep = 1.5f;
        const float MinSpeed = 4f;
        const float MaxSpeed = 70f;
        const int SpeedIterations = 14;

        /// <param name="start">Position de la balle au moment de la frappe.</param>
        /// <param name="target">Point visé au sol (seuls x et z comptent).</param>
        /// <param name="desiredSpeed">Vitesse souhaitée (m/s).</param>
        /// <param name="spinRate">Rotation en rad/s : positive pour un lift, négative pour un slice.</param>
        /// <param name="netMargin">Marge souhaitée au-dessus du filet (m).</param>
        public static ShotSolution Solve(Vector3 start, Vector3 target, float desiredSpeed, float spinRate,
            float netMargin, in BallSettings ball, in CourtDimensions court)
        {
            Vector3 toTarget = new Vector3(target.x - start.x, 0f, target.z - start.z);
            float distance = toTarget.magnitude;
            Vector3 direction = distance > 1e-3f ? toTarget / distance : Vector3.forward;

            // Lift : le dessus de la balle tourne vers l'avant.
            Vector3 spin = Vector3.Cross(Vector3.up, direction) * spinRate;
            bool mustCrossNet = start.z * target.z < 0f;

            var best = new ShotSolution();
            float bestScore = float.PositiveInfinity;
            var fallback = new ShotSolution();
            float fallbackClearance = float.NegativeInfinity;

            for (float angle = MinAngle; angle <= MaxAngle; angle += AngleStep)
            {
                float radians = angle * Mathf.Deg2Rad;
                Vector3 launch = direction * Mathf.Cos(radians) + Vector3.up * Mathf.Sin(radians);

                // La distance parcourue augmente avec la vitesse : recherche par dichotomie.
                float low = MinSpeed;
                float high = MaxSpeed;
                if (Reach(start, launch * high, spin, direction, ball, court) < distance)
                {
                    continue;
                }

                for (int i = 0; i < SpeedIterations; i++)
                {
                    float middle = (low + high) * 0.5f;
                    if (Reach(start, launch * middle, spin, direction, ball, court) < distance)
                    {
                        low = middle;
                    }
                    else
                    {
                        high = middle;
                    }
                }

                float speed = (low + high) * 0.5f;
                var state = new BallState { Position = start, Velocity = launch * speed, Spin = spin };
                BallPrediction prediction = BallPhysics.Predict(state, ball, court, false);
                if (!prediction.Lands)
                {
                    continue;
                }

                float clearance = mustCrossNet
                    ? (prediction.CrossesNet ? prediction.NetClearance : float.NegativeInfinity)
                    : float.PositiveInfinity;

                var candidate = new ShotSolution { Velocity = state.Velocity, Spin = spin, Speed = speed };
                if (clearance >= netMargin)
                {
                    float score = Mathf.Abs(speed - desiredSpeed);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = candidate;
                        best.ClearsNet = true;
                    }
                }
                else if (clearance > fallbackClearance)
                {
                    fallbackClearance = clearance;
                    fallback = candidate;
                    fallback.ClearsNet = clearance >= 0f;
                }
            }

            return bestScore < float.PositiveInfinity ? best : fallback;
        }

        /// <summary>Distance horizontale parcourue jusqu'au premier rebond, le long de la direction visée.</summary>
        static float Reach(Vector3 start, Vector3 velocity, Vector3 spin, Vector3 direction,
            in BallSettings ball, in CourtDimensions court)
        {
            var state = new BallState { Position = start, Velocity = velocity, Spin = spin };
            BallPrediction prediction = BallPhysics.Predict(state, ball, court, false);
            if (!prediction.Lands)
            {
                return float.PositiveInfinity;
            }

            return Vector3.Dot(prediction.Landing - start, direction);
        }
    }
}
