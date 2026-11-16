using UnityEngine;

namespace TennisPhone
{
    /// <summary>État de la balle : position (m), vitesse (m/s) et rotation (rad/s).</summary>
    public struct BallState
    {
        public Vector3 Position;
        public Vector3 Velocity;

        /// <summary>Vecteur rotation, en radians par seconde. Sa direction est l'axe de rotation.</summary>
        public Vector3 Spin;
    }

    /// <summary>Réglages physiques de la balle, copiés depuis GameplayConfig.</summary>
    public struct BallSettings
    {
        public float Gravity;
        public float Radius;

        /// <summary>Facteur aérodynamique : densité de l'air × section de la balle / (2 × masse), en 1/m.</summary>
        public float AirFactor;

        public float DragCoefficient;
        public float LiftCoefficientMax;
        public float SpinDecayTime;
        public float BounceRestitution;
        public float BounceFriction;
        public float InertiaFactor;
        public float NetRestitution;
    }

    public enum BallEvent
    {
        None,
        Bounce,
        Net,
    }

    /// <summary>Résultat d'une prédiction de trajectoire.</summary>
    public struct BallPrediction
    {
        /// <summary>Vrai si la balle touche le sol avant la fin de la simulation.</summary>
        public bool Lands;

        public Vector3 Landing;
        public float LandingTime;

        /// <summary>Vrai si la balle touche le filet avant de toucher le sol.</summary>
        public bool HitsNet;

        /// <summary>Vrai si la balle franchit le plan du filet avant de toucher le sol.</summary>
        public bool CrossesNet;

        /// <summary>Hauteur du bas de la balle au-dessus du filet au franchissement (négative si elle le touche).</summary>
        public float NetClearance;

        public float ApexHeight;
    }

    /// <summary>
    /// Physique de la balle : gravité, traînée, effet Magnus, rebond avec frottement, filet.
    /// Sans état et sans dépendance à la scène, pour pouvoir prédire des trajectoires et tester.
    /// </summary>
    public static class BallPhysics
    {
        /// <summary>Pas de simulation, en secondes.</summary>
        public const float TimeStep = 1f / 120f;

        /// <summary>
        /// Avance la balle d'un pas. Renvoie l'événement survenu pendant ce pas.
        /// </summary>
        public static BallEvent Step(ref BallState state, float deltaTime, in BallSettings ball,
            in CourtDimensions court, bool netEnabled = true)
        {
            Vector3 previous = state.Position;
            Vector3 velocity = state.Velocity;
            float speed = velocity.magnitude;

            var acceleration = new Vector3(0f, -ball.Gravity, 0f);
            if (speed > 1e-4f)
            {
                // Traînée : opposée à la vitesse, proportionnelle à son carré.
                acceleration -= velocity * (ball.AirFactor * ball.DragCoefficient * speed);

                // Effet Magnus : perpendiculaire à la vitesse et à l'axe de rotation.
                float spinRate = state.Spin.magnitude;
                if (spinRate > 1f)
                {
                    float spinRatio = ball.Radius * spinRate / speed;
                    float liftCoefficient = Mathf.Min(1f / (2f + 1f / spinRatio), ball.LiftCoefficientMax);
                    acceleration += Vector3.Cross(state.Spin, velocity)
                        * (ball.AirFactor * liftCoefficient * speed / spinRate);
                }
            }

            velocity += acceleration * deltaTime;
            Vector3 position = previous + velocity * deltaTime;
            state.Spin *= Mathf.Exp(-deltaTime / Mathf.Max(ball.SpinDecayTime, 0.01f));

            BallEvent result = BallEvent.None;

            // Filet : la balle franchit le plan z = 0 trop bas, entre les poteaux.
            if (netEnabled && previous.z != 0f && previous.z * position.z <= 0f)
            {
                float t = previous.z / (previous.z - position.z);
                Vector3 crossing = Vector3.Lerp(previous, position, t);
                if (Mathf.Abs(crossing.x) <= court.NetHalfWidth
                    && crossing.y - ball.Radius < court.NetHeightAt(crossing.x))
                {
                    float side = Mathf.Sign(previous.z);
                    position = new Vector3(crossing.x, Mathf.Max(crossing.y, ball.Radius), side * ball.Radius);
                    velocity = new Vector3(
                        velocity.x * 0.3f,
                        velocity.y > 0f ? velocity.y * 0.3f : velocity.y,
                        -velocity.z * ball.NetRestitution);
                    state.Spin *= 0.2f;
                    result = BallEvent.Net;
                }
            }

            // Sol : rebond avec perte verticale et frottement horizontal.
            if (position.y <= ball.Radius && velocity.y < 0f)
            {
                position.y = ball.Radius;
                Bounce(ref velocity, ref state.Spin, ball);
                if (result == BallEvent.None)
                {
                    result = BallEvent.Bounce;
                }
            }

            state.Position = position;
            state.Velocity = velocity;
            return result;
        }

        /// <summary>
        /// Rebond sur le sol. Le frottement freine le glissement du point de contact : un lift
        /// repart vite et bas, un slice est freiné.
        /// </summary>
        static void Bounce(ref Vector3 velocity, ref Vector3 spin, in BallSettings ball)
        {
            float impactSpeed = -velocity.y;
            velocity.y = impactSpeed * ball.BounceRestitution;

            // Vitesse de glissement du point de contact sur le sol.
            Vector3 up = Vector3.up;
            Vector3 slip = new Vector3(velocity.x, 0f, velocity.z) - Vector3.Cross(spin, up) * ball.Radius;
            slip.y = 0f;
            float slipSpeed = slip.magnitude;
            if (slipSpeed < 1e-4f)
            {
                return;
            }

            // Le frottement est limité par la force du choc ; il ne peut pas inverser le glissement.
            float grip = ball.InertiaFactor / (1f + ball.InertiaFactor);
            float change = Mathf.Min(ball.BounceFriction * (1f + ball.BounceRestitution) * impactSpeed, slipSpeed * grip);
            Vector3 deltaVelocity = -slip / slipSpeed * change;

            velocity += deltaVelocity;
            spin -= Vector3.Cross(up, deltaVelocity) / (ball.InertiaFactor * ball.Radius);
        }

        /// <summary>
        /// Simule la trajectoire jusqu'au premier contact avec le sol.
        /// </summary>
        public static BallPrediction Predict(BallState state, in BallSettings ball, in CourtDimensions court,
            bool netEnabled = true, float maxTime = 8f)
        {
            var prediction = new BallPrediction { ApexHeight = state.Position.y, NetClearance = float.NaN };
            float time = 0f;

            while (time < maxTime)
            {
                Vector3 previous = state.Position;
                BallEvent ballEvent = Step(ref state, TimeStep, ball, court, netEnabled);
                time += TimeStep;

                if (state.Position.y > prediction.ApexHeight)
                {
                    prediction.ApexHeight = state.Position.y;
                }

                if (!prediction.CrossesNet && previous.z != 0f && previous.z * state.Position.z <= 0f
                    && ballEvent != BallEvent.Net)
                {
                    float t = previous.z / (previous.z - state.Position.z);
                    Vector3 crossing = Vector3.Lerp(previous, state.Position, t);
                    prediction.CrossesNet = true;
                    prediction.NetClearance = crossing.y - ball.Radius - court.NetHeightAt(crossing.x);
                }

                if (ballEvent == BallEvent.Net)
                {
                    prediction.HitsNet = true;
                }
                else if (ballEvent == BallEvent.Bounce)
                {
                    prediction.Lands = true;
                    prediction.Landing = state.Position;
                    prediction.LandingTime = time;
                    return prediction;
                }
            }

            return prediction;
        }
    }
}
