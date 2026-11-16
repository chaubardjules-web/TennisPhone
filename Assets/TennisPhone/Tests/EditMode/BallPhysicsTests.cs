using NUnit.Framework;
using UnityEngine;

namespace TennisPhone.Tests
{
    public class BallPhysicsTests
    {
        static CourtDimensions Court()
        {
            return new CourtDimensions
            {
                HalfLength = 11.885f,
                HalfWidth = 4.115f,
                ServiceLine = 6.4f,
                NetHeightCenter = 0.914f,
                NetHeightPosts = 1.07f,
                NetHalfWidth = 5.029f,
            };
        }

        static BallSettings Ball(bool air = true)
        {
            const float radius = 0.033f;
            return new BallSettings
            {
                Gravity = 9.81f,
                Radius = radius,
                AirFactor = air ? 1.225f * Mathf.PI * radius * radius / (2f * 0.0577f) : 0f,
                DragCoefficient = 0.31f,
                LiftCoefficientMax = 0.36f,
                SpinDecayTime = 7.4f,
                BounceRestitution = 0.74f,
                BounceFriction = 0.56f,
                InertiaFactor = 0.55f,
                NetRestitution = 0.1f,
            };
        }

        static float Rpm(float rpm)
        {
            return rpm * 2f * Mathf.PI / 60f;
        }

        [Test]
        public void WithoutAir_FlightMatchesTheParabola()
        {
            // Lancée à 1 m de haut, 20 m/s à l'horizontale, loin du filet.
            var state = new BallState { Position = new Vector3(0f, 1f, 1f), Velocity = new Vector3(0f, 0f, 20f) };

            BallPrediction prediction = BallPhysics.Predict(state, Ball(false), Court());

            float fallTime = Mathf.Sqrt(2f * (1f - 0.033f) / 9.81f);
            Assert.IsTrue(prediction.Lands);
            Assert.AreEqual(fallTime, prediction.LandingTime, 0.02f);
            Assert.AreEqual(1f + 20f * fallTime, prediction.Landing.z, 0.25f);
        }

        [Test]
        public void Drag_ShortensTheFlight()
        {
            var state = new BallState { Position = new Vector3(0f, 1f, 1f), Velocity = new Vector3(0f, 6f, 25f) };

            float withAir = BallPhysics.Predict(state, Ball(), Court()).Landing.z;
            float withoutAir = BallPhysics.Predict(state, Ball(false), Court()).Landing.z;

            Assert.Less(withAir, withoutAir - 1f);
        }

        [Test]
        public void Topspin_DipsSooner_AndBackspinCarriesFurther()
        {
            Vector3 position = new Vector3(0f, 1f, 1f);
            Vector3 velocity = new Vector3(0f, 5f, 22f);

            // Balle qui va vers z positif : le lift tourne autour de l'axe x positif.
            float flat = BallPhysics.Predict(new BallState { Position = position, Velocity = velocity }, Ball(), Court()).Landing.z;
            float topspin = BallPhysics.Predict(new BallState { Position = position, Velocity = velocity, Spin = new Vector3(Rpm(3000f), 0f, 0f) }, Ball(), Court()).Landing.z;
            float backspin = BallPhysics.Predict(new BallState { Position = position, Velocity = velocity, Spin = new Vector3(-Rpm(3000f), 0f, 0f) }, Ball(), Court()).Landing.z;

            Assert.Less(topspin, flat - 0.5f);
            Assert.Greater(backspin, flat + 0.5f);
        }

        [Test]
        public void Bounce_KeepsPartOfTheVerticalSpeed_AndSlowsTheBall()
        {
            var state = new BallState { Position = new Vector3(0f, 0.04f, 3f), Velocity = new Vector3(0f, -6f, 12f) };

            BallEvent ballEvent = BallPhysics.Step(ref state, BallPhysics.TimeStep, Ball(false), Court());

            Assert.AreEqual(BallEvent.Bounce, ballEvent);
            Assert.AreEqual(6f * 0.74f, state.Velocity.y, 0.15f);
            Assert.Less(state.Velocity.z, 12f);
            Assert.Greater(state.Velocity.z, 6f);
            Assert.Greater(state.Spin.x, 0f, "le frottement donne de la rotation vers l'avant");
        }

        [Test]
        public void Topspin_KicksForward_AndBackspinBrakes_AtTheBounce()
        {
            Vector3 position = new Vector3(0f, 0.04f, 3f);
            Vector3 velocity = new Vector3(0f, -6f, 12f);
            var topspin = new BallState { Position = position, Velocity = velocity, Spin = new Vector3(Rpm(3000f), 0f, 0f) };
            var backspin = new BallState { Position = position, Velocity = velocity, Spin = new Vector3(-Rpm(3000f), 0f, 0f) };

            BallPhysics.Step(ref topspin, BallPhysics.TimeStep, Ball(false), Court());
            BallPhysics.Step(ref backspin, BallPhysics.TimeStep, Ball(false), Court());

            Assert.Greater(topspin.Velocity.z, backspin.Velocity.z + 1f);
        }

        [Test]
        public void LowBall_StopsInTheNet_AndHighBallPasses()
        {
            var low = new BallState { Position = new Vector3(0f, 0.6f, -1f), Velocity = new Vector3(0f, 0f, 20f) };
            var high = new BallState { Position = new Vector3(0f, 1.6f, -1f), Velocity = new Vector3(0f, 1f, 20f) };

            BallPrediction lowPrediction = BallPhysics.Predict(low, Ball(), Court());
            BallPrediction highPrediction = BallPhysics.Predict(high, Ball(), Court());

            Assert.IsTrue(lowPrediction.HitsNet);
            Assert.Less(lowPrediction.Landing.z, 0f, "la balle retombe du côté de celui qui l'a frappée");
            Assert.IsFalse(highPrediction.HitsNet);
            Assert.IsTrue(highPrediction.CrossesNet);
            Assert.Greater(highPrediction.NetClearance, 0f);
            Assert.Greater(highPrediction.Landing.z, 0f);
        }

        [Test]
        public void BallOutsideThePosts_IsNotStoppedByTheNet()
        {
            var state = new BallState { Position = new Vector3(6f, 0.5f, -1f), Velocity = new Vector3(0f, 2f, 20f) };

            Assert.IsFalse(BallPhysics.Predict(state, Ball(), Court()).HitsNet);
        }

        [Test]
        public void Court_KnowsItsLimits()
        {
            CourtDimensions court = Court();

            Assert.IsTrue(court.IsInside(4.115f, 11.885f), "les lignes sont dans le court");
            Assert.IsFalse(court.IsInside(4.2f, 5f));
            Assert.IsFalse(court.IsInside(0f, 12f));
            Assert.AreEqual(0.914f, court.NetHeightAt(0f), 1e-4f);
            Assert.AreEqual(1.07f, court.NetHeightAt(5.029f), 1e-4f);

            // Carré de service de l'adversaire (z positif), moitié droite (x positif).
            Assert.IsTrue(court.IsInServiceBox(2f, 3f, 1, 1));
            Assert.IsFalse(court.IsInServiceBox(-2f, 3f, 1, 1));
            Assert.IsFalse(court.IsInServiceBox(2f, 7f, 1, 1));
            Assert.IsFalse(court.IsInServiceBox(2f, -3f, 1, 1));
        }

        [TestCase(0f, 20f)]
        [TestCase(3000f, 20f)]
        [TestCase(-2500f, 17f)]
        [TestCase(0f, 27f)]
        public void Solver_LandsOnTheTarget_AndClearsTheNet(float rpm, float speed)
        {
            var start = new Vector3(1.5f, 0.9f, -11f);
            var target = new Vector3(-2.5f, 0f, 9f);

            ShotSolution solution = ShotSolver.Solve(start, target, speed, Rpm(rpm), 0.3f, Ball(), Court());
            var state = new BallState { Position = start, Velocity = solution.Velocity, Spin = solution.Spin };
            BallPrediction prediction = BallPhysics.Predict(state, Ball(), Court());

            Assert.IsTrue(solution.ClearsNet);
            Assert.IsFalse(prediction.HitsNet);
            Assert.GreaterOrEqual(prediction.NetClearance, 0.29f);
            Assert.Less(Vector3.Distance(new Vector3(prediction.Landing.x, 0f, prediction.Landing.z), target), 0.15f);
        }

        [Test]
        public void Solver_PrefersTheRequestedSpeed()
        {
            var start = new Vector3(0f, 0.9f, -11f);
            var target = new Vector3(0f, 0f, 9f);

            float slow = ShotSolver.Solve(start, target, 16f, 0f, 0.3f, Ball(), Court()).Speed;
            float fast = ShotSolver.Solve(start, target, 26f, 0f, 0.3f, Ball(), Court()).Speed;

            Assert.Less(slow, fast);
            Assert.AreEqual(26f, fast, 2.5f);
        }

        [Test]
        public void Planner_AimsCrossCourtWhenEarly_AndDownTheLineWhenLate()
        {
            var config = ScriptableObject.CreateInstance<GameplayConfig>();
            var forehand = new SwingEvent { Side = SwingSide.Forehand, Power = 0.5f };
            var backhand = new SwingEvent { Side = SwingSide.Backhand, Power = 0.5f };

            // Joueur droitier, qui envoie vers z positif : sa raquette de coup droit est côté x positif.
            Assert.Less(ShotPlanner.Build(forehand, -1f, 1, config).Target.x, -1f, "coup droit tôt : croisé, vers la gauche");
            Assert.Greater(ShotPlanner.Build(forehand, 1f, 1, config).Target.x, 1f, "coup droit tard : long de ligne, à droite");
            Assert.Greater(ShotPlanner.Build(backhand, -1f, 1, config).Target.x, 1f, "revers tôt : croisé, vers la droite");
            Assert.AreEqual(0f, ShotPlanner.Build(forehand, 0f, 1, config).Target.x, 1e-4f);

            // Plus de puissance : balle plus rapide et plus profonde.
            var soft = new SwingEvent { Side = SwingSide.Forehand, Power = 0f };
            var hard = new SwingEvent { Side = SwingSide.Forehand, Power = 1f };
            Assert.Less(ShotPlanner.Build(soft, 0f, 1, config).Speed, ShotPlanner.Build(hard, 0f, 1, config).Speed);
            Assert.Less(ShotPlanner.Build(soft, 0f, 1, config).Target.z, ShotPlanner.Build(hard, 0f, 1, config).Target.z);

            Object.DestroyImmediate(config);
        }
    }
}
