using NUnit.Framework;

namespace TennisPhone.Tests
{
    public class MatchScoreTests
    {
        static void Win(MatchScore score, int side, int points)
        {
            for (int i = 0; i < points; i++)
            {
                score.PointWonBy(side);
            }
        }

        [Test]
        public void Points_GoFifteenThirtyForty()
        {
            var score = new MatchScore(3, false, 0);

            Assert.AreEqual("0 - 0", score.Call());
            score.PointWonBy(0);
            Assert.AreEqual("15 - 0", score.Call());
            score.PointWonBy(0);
            score.PointWonBy(1);
            Assert.AreEqual("30 - 15", score.Call());
            score.PointWonBy(0);
            Assert.AreEqual("40", score.PointText(0));
            Assert.AreEqual("15", score.PointText(1));
        }

        [Test]
        public void FourthPoint_WinsTheGame_AndServiceChangesSide()
        {
            var score = new MatchScore(3, false, 0);
            Win(score, 0, 3);

            ScoreEvent result = score.PointWonBy(0);

            Assert.AreEqual(ScoreEvent.Game, result);
            Assert.AreEqual(1, score.Games(0));
            Assert.AreEqual(0, score.Points(0));
            Assert.AreEqual(1, score.Server, "le service passe à l'autre camp");
        }

        [Test]
        public void Deuce_NeedsTwoPointsInARow()
        {
            var score = new MatchScore(3, false, 0);
            Win(score, 0, 3);
            Win(score, 1, 3);
            Assert.IsTrue(score.IsDeuce);
            Assert.AreEqual("Égalité", score.Call());

            Assert.AreEqual(ScoreEvent.Point, score.PointWonBy(1));
            Assert.AreEqual(1, score.Advantage);
            Assert.AreEqual("Avantage retour", score.Call());
            Assert.AreEqual("Av.", score.PointText(1));
            Assert.AreEqual("40", score.PointText(0));

            Assert.AreEqual(ScoreEvent.Point, score.PointWonBy(0));
            Assert.IsTrue(score.IsDeuce, "retour à égalité");

            score.PointWonBy(0);
            Assert.AreEqual("Avantage service", score.Call());
            Assert.AreEqual(ScoreEvent.Game, score.PointWonBy(0));
        }

        [Test]
        public void Set_IsWonAtTheRequiredNumberOfGames()
        {
            var score = new MatchScore(3, false, 0);
            Win(score, 1, 4);
            Win(score, 0, 4);
            Win(score, 1, 4);
            Assert.IsFalse(score.IsOver);

            Win(score, 1, 3);
            ScoreEvent result = score.PointWonBy(1);

            Assert.AreEqual(ScoreEvent.Set, result);
            Assert.IsTrue(score.IsOver);
            Assert.AreEqual(1, score.Winner);
            Assert.AreEqual(3, score.Games(1));
            Assert.AreEqual(1, score.Games(0));
        }

        [Test]
        public void TwoGameLead_KeepsTheSetGoing()
        {
            var score = new MatchScore(2, true, 0);
            Win(score, 0, 4);
            Win(score, 1, 4);
            Win(score, 0, 4);
            Assert.IsFalse(score.IsOver, "2 jeux à 1 : il faut deux jeux d'écart");

            Win(score, 0, 4);
            Assert.IsTrue(score.IsOver);
            Assert.AreEqual(0, score.Winner);
        }

        [Test]
        public void ServeCourt_AlternatesEveryPoint()
        {
            var score = new MatchScore(3, false, 1);

            Assert.AreEqual(1, score.Server);
            Assert.AreEqual(0, score.ServeCourt);
            score.PointWonBy(0);
            Assert.AreEqual(1, score.ServeCourt);
            score.PointWonBy(1);
            Assert.AreEqual(0, score.ServeCourt);
        }

        [Test]
        public void PointsAfterTheSet_AreIgnored()
        {
            var score = new MatchScore(1, false, 0);
            Win(score, 0, 4);

            Assert.AreEqual(ScoreEvent.Set, score.PointWonBy(1));
            Assert.AreEqual(0, score.Winner);
            Assert.AreEqual(0, score.Games(1));
        }
    }

    public class TossDetectorTests
    {
        [Test]
        public void RaisingThePhone_FiresOnce()
        {
            var detector = new TossDetector { ArmElevation = 30f, RaiseElevation = 55f };
            int tosses = 0;

            // Le téléphone part du bas, monte jusqu'à pointer vers le haut, puis y reste.
            for (float elevation = -30f; elevation <= 85f; elevation += 5f)
            {
                tosses += detector.Process(elevation) ? 1 : 0;
            }

            for (int i = 0; i < 30; i++)
            {
                tosses += detector.Process(80f) ? 1 : 0;
            }

            Assert.AreEqual(1, tosses);
        }

        [Test]
        public void PhoneAlreadyRaised_DoesNotFireUntilLoweredFirst()
        {
            var detector = new TossDetector { ArmElevation = 30f, RaiseElevation = 55f };

            Assert.IsFalse(detector.Process(70f), "téléphone déjà levé au départ");
            Assert.IsFalse(detector.Process(80f));
            Assert.IsFalse(detector.IsArmed);

            Assert.IsFalse(detector.Process(10f));
            Assert.IsTrue(detector.IsArmed);
            Assert.IsTrue(detector.Process(60f));
        }

        [Test]
        public void SmallMovements_DoNotFire()
        {
            var detector = new TossDetector { ArmElevation = 30f, RaiseElevation = 55f };
            int tosses = 0;

            for (int i = 0; i < 200; i++)
            {
                tosses += detector.Process(20f + 25f * UnityEngine.Mathf.Sin(i * 0.3f)) ? 1 : 0;
            }

            Assert.AreEqual(0, tosses, "le téléphone oscille entre -5° et 45°, jamais assez haut");
        }

        [Test]
        public void PhoneMotion_ReportsTheElevationOfThePhone()
        {
            // beta = 90° : téléphone debout, haut vers le ciel. beta = 0° : à plat.
            var upright = new SensorSample { Orientation = new UnityEngine.Vector3(0f, 90f, 0f) };
            var flat = new SensorSample { Orientation = new UnityEngine.Vector3(0f, 0f, 0f) };
            var down = new SensorSample { Orientation = new UnityEngine.Vector3(0f, -40f, 0f) };

            Assert.AreEqual(90f, PhoneMotion.ToFrame(upright).TopElevation, 0.5f);
            Assert.AreEqual(0f, PhoneMotion.ToFrame(flat).TopElevation, 0.5f);
            Assert.AreEqual(-40f, PhoneMotion.ToFrame(down).TopElevation, 0.5f);
        }
    }
}
