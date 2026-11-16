using NUnit.Framework;
using UnityEngine;

namespace TennisPhone.Tests
{
    public class OpponentAiTests
    {
        const float Step = 1f / 120f;
        const float BaseSpeed = 7.5f;
        const float SideOffset = 0.55f;

        GameplayConfig config;

        [SetUp]
        public void CreateConfig()
        {
            config = ScriptableObject.CreateInstance<GameplayConfig>();
        }

        [TearDown]
        public void DestroyConfig()
        {
            Object.DestroyImmediate(config);
        }

        static Vector3 Home => new Vector3(0f, 0f, 12.7f);

        /// <summary>Fait courir l'adversaire de « from » à « to » (en secondes de simulation).</summary>
        static void Run(OpponentAi ai, double from, double to, AiLevel level)
        {
            for (double now = from; now < to; now += Step)
            {
                ai.Move(now, Step, Home, BaseSpeed, SideOffset, level);
            }
        }

        [Test]
        public void StaysStillDuringReactionDelay_ThenRunsToTheBall()
        {
            var level = new AiLevel { reactionDelay = 0.4f, positionError = 0f, speedMultiplier = 1f };
            var ai = new OpponentAi(1);
            ai.Reset(Home);
            ai.OnIncoming(new Vector3(3f, 0.9f, 10f), 2.0, 0.0, level);

            Run(ai, 0.0, 0.39, level);
            Assert.AreEqual(Home, ai.Position, "il ne bouge pas avant la fin de son temps de réaction");

            Run(ai, 0.39, 2.0, level);
            Assert.IsTrue(ai.CanReach(new Vector3(3f, 0.9f, 10f), SideOffset, 1.5f));
            Assert.IsFalse(ai.IsStretched(2.0, SideOffset, level), "arrivé bien avant la balle, il a eu le temps de s'installer");
        }

        [Test]
        public void SlowOpponent_CannotReachAFarBall()
        {
            var level = new AiLevel { reactionDelay = 0.6f, positionError = 0f, speedMultiplier = 0.5f };
            var ai = new OpponentAi(1);
            ai.Reset(new Vector3(-3.5f, 0f, 12.7f));
            ai.OnIncoming(new Vector3(3.8f, 0.9f, 8f), 1.2, 0.0, level);

            Run(ai, 0.0, 1.2, level);

            Assert.IsFalse(ai.CanReach(new Vector3(3.8f, 0.9f, 8f), SideOffset, 1.5f));
        }

        [Test]
        public void ReadError_FadesAsTheBallArrives()
        {
            var level = new AiLevel { reactionDelay = 0f, positionError = 2f, speedMultiplier = 3f };
            var contact = new Vector3(1f, 0.9f, 10f);

            // Quel que soit le tirage de l'erreur de lecture, il finit bien placé.
            for (int seed = 0; seed < 20; seed++)
            {
                var ai = new OpponentAi(seed);
                ai.Reset(Home);
                ai.OnIncoming(contact, 2.0, 0.0, level);
                Run(ai, 0.0, 2.0, level);

                Assert.IsTrue(ai.CanReach(contact, SideOffset, 0.8f), "graine " + seed);
            }
        }

        [Test]
        public void GoesBackHome_WhenThereIsNoBallToPlay()
        {
            var level = new AiLevel { reactionDelay = 0f, positionError = 0f, speedMultiplier = 1f };
            var ai = new OpponentAi(1);
            ai.Reset(new Vector3(3f, 0f, 9f));
            ai.StopTracking();

            Run(ai, 0.0, 2.0, level);

            Assert.Less(Vector3.Distance(Home, ai.Position), 0.01f);
        }

        [Test]
        public void AccurateShots_LandInThePlayersCourt()
        {
            var level = new AiLevel { aimErrorDegrees = 0f, sloppyShotRate = 0f, shotSpeedMin = 17f, shotSpeedMax = 22f, awayProbability = 1f };
            var ai = new OpponentAi(7);
            ai.Reset(Home);
            CourtDimensions court = config.ToCourt();
            BallSettings ball = config.ToBallSettings();

            for (int i = 0; i < 25; i++)
            {
                var ballPosition = new Vector3(-3f + i * 0.25f, 0.9f, 10f);
                var playerPosition = new Vector3(2f, 0f, -12.7f);

                AiShot shot = ai.ChooseShot(ballPosition, playerPosition, false, level, config, ball, court);
                var state = new BallState { Position = ballPosition, Velocity = shot.Solution.Velocity, Spin = shot.Solution.Spin };
                BallPrediction prediction = BallPhysics.Predict(state, ball, court);

                Assert.IsFalse(prediction.HitsNet, "frappe " + i);
                Assert.Less(prediction.Landing.z, 0f, "frappe " + i);
                Assert.IsTrue(court.IsInside(prediction.Landing.x, prediction.Landing.z), "frappe " + i);
                Assert.Less(shot.Target.x, 0f, "il vise à l'opposé du joueur, qui est à droite");
            }
        }

        [Test]
        public void StretchedShots_AreLessAccurateThanSetShots()
        {
            var level = new AiLevel { aimErrorDegrees = 4f, sloppyShotRate = 0f, stretchedErrorFactor = 2f, shotSpeedMin = 18f, shotSpeedMax = 20f };
            CourtDimensions court = config.ToCourt();
            BallSettings ball = config.ToBallSettings();
            var ballPosition = new Vector3(0f, 0.9f, 10f);
            var playerPosition = new Vector3(0f, 0f, -12.7f);

            float setError = 0f;
            float stretchedError = 0f;
            const int shots = 60;
            var setAi = new OpponentAi(3);
            var stretchedAi = new OpponentAi(3);
            for (int i = 0; i < shots; i++)
            {
                setError += MissDistance(setAi.ChooseShot(ballPosition, playerPosition, false, level, config, ball, court), ballPosition, ball, court);
                stretchedError += MissDistance(stretchedAi.ChooseShot(ballPosition, playerPosition, true, level, config, ball, court), ballPosition, ball, court);
            }

            Assert.Greater(stretchedError / shots, setError / shots * 2f);
        }

        static float MissDistance(AiShot shot, Vector3 start, in BallSettings ball, in CourtDimensions court)
        {
            var state = new BallState { Position = start, Velocity = shot.Solution.Velocity, Spin = shot.Solution.Spin };
            BallPrediction prediction = BallPhysics.Predict(state, ball, court, false);
            return Vector3.Distance(new Vector3(prediction.Landing.x, 0f, prediction.Landing.z), shot.Target);
        }

        [Test]
        public void HarderLevels_ReactFasterAndAimBetter()
        {
            Assert.Greater(config.aiEasy.reactionDelay, config.aiNormal.reactionDelay);
            Assert.Greater(config.aiNormal.reactionDelay, config.aiHard.reactionDelay);
            Assert.Greater(config.aiEasy.aimErrorDegrees, config.aiHard.aimErrorDegrees);
            Assert.Less(config.aiEasy.speedMultiplier, config.aiHard.speedMultiplier);
            Assert.AreSame(config.aiHard, config.GetAiLevel(AiDifficulty.Hard));
        }
    }
}
