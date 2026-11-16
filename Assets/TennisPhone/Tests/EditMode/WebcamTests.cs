using NUnit.Framework;

namespace TennisPhone.Tests
{
    public class BodyTrackerTests
    {
        const int Width = 64;
        const int Height = 36;
        const float FrameTime = 1f / 30f;

        static BodyTrackerSettings Settings => new BodyTrackerSettings
        {
            MotionThreshold = 18f,
            MemorySeconds = 0.5f,
            MinCoverage = 0.03f,
            MaxCoverage = 0.6f,
            SmoothingSeconds = 0.12f,
            HoldSeconds = 2f,
        };

        /// <summary>Image grise avec une « personne » : une bande claire de 8 colonnes centrée sur cette colonne.</summary>
        static byte[] Frame(float personColumn, int armColumn = -1)
        {
            var luma = new byte[Width * Height];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    bool body = y >= 6 && System.Math.Abs(x - personColumn) <= 4f;
                    bool arm = armColumn >= 0 && y >= 12 && y <= 14 && System.Math.Abs(x - armColumn) <= 3;
                    luma[y * Width + x] = (byte)(body || arm ? 200 : 60);
                }
            }

            return luma;
        }

        static float ColumnToX(float column)
        {
            return column / (Width - 1) * 2f - 1f;
        }

        [Test]
        public void StillScene_HasNoTarget()
        {
            var tracker = new BodyTracker(Width, Height);
            for (int i = 0; i < 30; i++)
            {
                tracker.Process(Frame(32f), i * FrameTime, FrameTime, false, Settings);
            }

            Assert.IsFalse(tracker.HasTarget, "rien ne bouge : personne à suivre");
        }

        [Test]
        public void PersonWalkingRight_IsFollowed()
        {
            var tracker = new BodyTracker(Width, Height);
            float column = 16f;
            double time = 0.0;
            for (int i = 0; i < 60; i++)
            {
                column += 0.5f;
                time += FrameTime;
                tracker.Process(Frame(column), time, FrameTime, false, Settings);
            }

            Assert.IsTrue(tracker.HasTarget);
            Assert.AreEqual(ColumnToX(column), tracker.X, 0.2f, "la position suit la personne, avec un léger retard");
            Assert.Greater(tracker.X, ColumnToX(32f), "elle a dépassé le milieu de l'image");
        }

        [Test]
        public void PersonWhoStops_KeepsLastPosition_ThenIsLost()
        {
            var tracker = new BodyTracker(Width, Height);
            float column = 20f;
            double time = 0.0;
            for (int i = 0; i < 30; i++)
            {
                column += 0.5f;
                time += FrameTime;
                tracker.Process(Frame(column), time, FrameTime, false, Settings);
            }

            // Une seconde sans bouger : la position se pose sur la personne et n'en bouge plus.
            for (int i = 0; i < 30; i++)
            {
                time += FrameTime;
                tracker.Process(Frame(column), time, FrameTime, false, Settings);
            }

            Assert.IsTrue(tracker.HasTarget, "toujours suivie pendant le délai de maintien");
            Assert.AreEqual(ColumnToX(column), tracker.X, 0.1f);

            // Trois secondes de plus : au-delà du délai, elle est considérée comme partie.
            for (int i = 0; i < 90; i++)
            {
                time += FrameTime;
                tracker.Process(Frame(column), time, FrameTime, false, Settings);
            }

            Assert.IsFalse(tracker.HasTarget);
        }

        [Test]
        public void SwingWhileFrozen_DoesNotMoveThePosition()
        {
            var tracker = new BodyTracker(Width, Height);
            float column = 24f;
            double time = 0.0;
            for (int i = 0; i < 30; i++)
            {
                column += 0.4f;
                time += FrameTime;
                tracker.Process(Frame(column), time, FrameTime, false, Settings);
            }

            float before = tracker.X;

            // Le bras balaie l'image loin du corps pendant un swing : le suivi est gelé.
            for (int i = 0; i < 12; i++)
            {
                time += FrameTime;
                tracker.Process(Frame(column, 44 + i), time, FrameTime, true, Settings);
            }

            Assert.AreEqual(before, tracker.X, 1e-4f);
            Assert.IsTrue(tracker.HasTarget);

            // Après le geste, bras revenu : la position n'a pas sauté.
            for (int i = 0; i < 10; i++)
            {
                time += FrameTime;
                tracker.Process(Frame(column), time, FrameTime, false, Settings);
            }

            Assert.AreEqual(before, tracker.X, 0.08f);
        }

        [Test]
        public void WholeImageChanging_IsIgnored()
        {
            var tracker = new BodyTracker(Width, Height);
            float column = 20f;
            double time = 0.0;
            for (int i = 0; i < 30; i++)
            {
                column += 0.5f;
                time += FrameTime;
                tracker.Process(Frame(column), time, FrameTime, false, Settings);
            }

            float before = tracker.X;

            // La lumière de la pièce change : toute l'image s'éclaircit d'un coup.
            var bright = new byte[Width * Height];
            for (int i = 0; i < bright.Length; i++)
            {
                bright[i] = 250;
            }

            time += FrameTime;
            tracker.Process(bright, time, FrameTime, false, Settings);

            Assert.AreEqual(before, tracker.X, 1e-4f);
        }
    }

    public class TossRateTests
    {
        static float RaiseAt(float degreesPerSecond)
        {
            var detector = new TossDetector { ArmElevation = 30f, RaiseElevation = 55f, RateWindow = 0.15f };
            const double step = 1.0 / 60.0;
            double time = 0.0;
            float elevation = 0f;
            for (int i = 0; i < 600; i++)
            {
                time += step;
                elevation += degreesPerSecond * (float)step;
                if (detector.Process(elevation, time))
                {
                    return detector.RaiseRate;
                }
            }

            Assert.Fail("le lever n'a pas été détecté");
            return 0f;
        }

        [Test]
        public void RaiseRate_MatchesTheSpeedOfTheGesture()
        {
            Assert.AreEqual(80f, RaiseAt(80f), 8f);
            Assert.AreEqual(300f, RaiseAt(300f), 30f);
        }

        [Test]
        public void FasterRaise_GivesHigherRate()
        {
            Assert.Greater(RaiseAt(250f), RaiseAt(100f));
        }

        [Test]
        public void WithoutTimestamps_RateIsZero()
        {
            var detector = new TossDetector { ArmElevation = 30f, RaiseElevation = 55f };
            detector.Process(0f);
            Assert.IsTrue(detector.Process(70f));
            Assert.AreEqual(0f, detector.RaiseRate);
        }
    }
}
