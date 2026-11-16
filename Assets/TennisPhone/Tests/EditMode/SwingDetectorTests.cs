using System.Collections.Generic;
using NUnit.Framework;

namespace TennisPhone.Tests
{
    public class SwingDetectorTests
    {
        const double FrameSeconds = 1.0 / 60.0;

        static SwingSettings Settings()
        {
            return new SwingSettings
            {
                Handedness = Handedness.Right,
                AccelerationThreshold = 12f,
                RotationThreshold = 220f,
                ReleaseRatio = 0.6f,
                ImpactFallRatio = 0.85f,
                MinDuration = 0.03f,
                MaxDuration = 0.25f,
                FollowThroughDuration = 0.3f,
                Cooldown = 0.35f,
                PowerAccelerationMin = 12f,
                PowerAccelerationMax = 52f,
                PowerRotationMin = 220f,
                PowerRotationMax = 1020f,
                PowerRotationWeight = 0.5f,
                LiftRollRateMin = 200f,
                LiftRollRateFull = 600f,
                SliceFaceOpeningMin = 0.5f,
                SliceFaceOpeningFull = 1f,
            };
        }

        /// <summary>
        /// Geste en cloche sur « count » mesures. L'écran passe de faceStart à faceEnd pendant le geste.
        /// </summary>
        static void AddGesture(List<MotionFrame> frames, ref double time, int count, float peakAcceleration,
            float peakRotation, float yawSign, float rollRate, float faceStart = 0f, float faceEnd = 0f)
        {
            for (int i = 0; i < count; i++)
            {
                float progress = (i + 0.5f) / count;
                float shape = 1f - System.Math.Abs(progress * 2f - 1f);
                frames.Add(new MotionFrame
                {
                    TimeSeconds = time,
                    Acceleration = peakAcceleration * shape,
                    RotationSpeed = peakRotation * shape,
                    YawRate = peakRotation * shape * yawSign,
                    RollRate = rollRate,
                    FaceUp = faceStart + (faceEnd - faceStart) * progress,
                });
                time += FrameSeconds;
            }
        }

        static void AddRest(List<MotionFrame> frames, ref double time, int count, float faceUp = 0f)
        {
            for (int i = 0; i < count; i++)
            {
                frames.Add(new MotionFrame { TimeSeconds = time, Acceleration = 0.4f, RotationSpeed = 8f, FaceUp = faceUp });
                time += FrameSeconds;
            }
        }

        struct Outcome
        {
            public List<SwingEvent> Impacts;
            public List<SwingEvent> Refinements;

            /// <summary>Swings avec leur effet final.</summary>
            public List<SwingEvent> Final;
        }

        static Outcome Run(List<MotionFrame> frames, SwingSettings settings)
        {
            var detector = new SwingDetector(settings);
            var outcome = new Outcome
            {
                Impacts = new List<SwingEvent>(),
                Refinements = new List<SwingEvent>(),
                Final = new List<SwingEvent>(),
            };

            foreach (MotionFrame frame in frames)
            {
                SwingUpdate update = detector.Process(frame, out SwingEvent swing);
                if (update == SwingUpdate.Impact)
                {
                    outcome.Impacts.Add(swing);
                    outcome.Final.Add(swing);
                }
                else if (update == SwingUpdate.Refined)
                {
                    outcome.Refinements.Add(swing);
                    outcome.Final[outcome.Final.Count - 1] = swing;
                }
            }

            return outcome;
        }

        [Test]
        public void Rest_ProducesNoSwing()
        {
            var frames = new List<MotionFrame>();
            double time = 0.0;
            AddRest(frames, ref time, 300);

            Assert.IsEmpty(Run(frames, Settings()).Impacts);
        }

        [Test]
        public void SingleGesture_ProducesOneFlatForehand()
        {
            var frames = new List<MotionFrame>();
            double time = 0.0;
            AddRest(frames, ref time, 30);
            AddGesture(frames, ref time, 12, 32f, 620f, 1f, 0f);
            AddRest(frames, ref time, 60);

            Outcome outcome = Run(frames, Settings());

            Assert.AreEqual(1, outcome.Impacts.Count);
            Assert.IsEmpty(outcome.Refinements);
            Assert.AreEqual(SwingSide.Forehand, outcome.Impacts[0].Side);
            Assert.AreEqual(0f, outcome.Impacts[0].Spin);
            Assert.Greater(outcome.Impacts[0].Power, 0.3f);
            Assert.Less(outcome.Impacts[0].Power, 0.7f);
        }

        [Test]
        public void Impact_IsReportedShortlyAfterThePeak()
        {
            var frames = new List<MotionFrame>();
            double time = 0.0;
            AddGesture(frames, ref time, 24, 32f, 620f, 1f, 0f);
            AddRest(frames, ref time, 30);

            var detector = new SwingDetector(Settings());
            double impactTime = -1.0;
            SwingEvent impact = default;
            foreach (MotionFrame frame in frames)
            {
                if (detector.Process(frame, out SwingEvent swing) == SwingUpdate.Impact)
                {
                    impactTime = frame.TimeSeconds;
                    impact = swing;
                }
            }

            // Le pic de la cloche est au milieu des 24 mesures ; l'impact doit suivre de près,
            // bien avant la fin du geste.
            Assert.Greater(impactTime, 0.0);
            Assert.Less(impactTime - impact.PeakTimeSeconds, 0.09);
        }

        [Test]
        public void ClockwiseGesture_IsBackhandForRightHander_AndForehandForLeftHander()
        {
            var frames = new List<MotionFrame>();
            double time = 0.0;
            AddRest(frames, ref time, 10);
            AddGesture(frames, ref time, 12, 32f, 620f, -1f, 0f);
            AddRest(frames, ref time, 30);

            SwingSettings leftHanded = Settings();
            leftHanded.Handedness = Handedness.Left;

            Assert.AreEqual(SwingSide.Backhand, Run(frames, Settings()).Impacts[0].Side);
            Assert.AreEqual(SwingSide.Forehand, Run(frames, leftHanded).Impacts[0].Side);
        }

        [Test]
        public void StrongerGesture_HasMorePower()
        {
            var soft = new List<MotionFrame>();
            var hard = new List<MotionFrame>();
            double time = 0.0;
            AddGesture(soft, ref time, 12, 22f, 400f, 1f, 0f);
            AddRest(soft, ref time, 10);
            time = 0.0;
            AddGesture(hard, ref time, 12, 70f, 1400f, 1f, 0f);
            AddRest(hard, ref time, 10);

            float softPower = Run(soft, Settings()).Impacts[0].Power;
            float hardPower = Run(hard, Settings()).Impacts[0].Power;

            Assert.Greater(hardPower, softPower);
            Assert.AreEqual(1f, hardPower, 1e-4f);
        }

        [Test]
        public void WristRoll_GivesLift_MirroredOnBackhand()
        {
            var forehand = new List<MotionFrame>();
            var backhandMirrored = new List<MotionFrame>();
            var backhandSameRoll = new List<MotionFrame>();
            double time = 0.0;
            AddGesture(forehand, ref time, 12, 32f, 620f, 1f, 400f);
            AddRest(forehand, ref time, 30);
            time = 0.0;
            AddGesture(backhandMirrored, ref time, 12, 32f, 620f, -1f, -400f);
            AddRest(backhandMirrored, ref time, 30);
            time = 0.0;
            AddGesture(backhandSameRoll, ref time, 12, 32f, 620f, -1f, 400f);
            AddRest(backhandSameRoll, ref time, 30);

            Assert.AreEqual(0.5f, Run(forehand, Settings()).Final[0].Spin, 1e-3f);
            Assert.AreEqual(0.5f, Run(backhandMirrored, Settings()).Final[0].Spin, 1e-3f);
            Assert.AreEqual(0f, Run(backhandSameRoll, Settings()).Final[0].Spin, 1e-3f);
        }

        [Test]
        public void FaceOpeningDuringFollowThrough_RefinesTheSwingIntoASlice()
        {
            var frames = new List<MotionFrame>();
            double time = 0.0;
            AddRest(frames, ref time, 10, 0f);
            AddGesture(frames, ref time, 12, 32f, 620f, 1f, 0f);

            // Après le geste, le tamis s'ouvre vers le ciel.
            for (int i = 0; i < 12; i++)
            {
                frames.Add(new MotionFrame { TimeSeconds = time, Acceleration = 0.4f, RotationSpeed = 8f, FaceUp = (i + 1) / 12f });
                time += FrameSeconds;
            }

            AddRest(frames, ref time, 30, 1f);

            Outcome outcome = Run(frames, Settings());

            Assert.AreEqual(1, outcome.Impacts.Count);
            Assert.AreEqual(0f, outcome.Impacts[0].Spin, "à l'impact, le coup est encore à plat");
            Assert.AreEqual(1, outcome.Refinements.Count);
            Assert.AreEqual(outcome.Impacts[0].Id, outcome.Refinements[0].Id);
            Assert.AreEqual(-1f, outcome.Final[0].Spin, 1e-3f);
        }

        [Test]
        public void FaceClosing_DoesNotMakeASlice_ButOpensOneOnBackhand()
        {
            var forehand = new List<MotionFrame>();
            var backhand = new List<MotionFrame>();
            double time = 0.0;
            AddGesture(forehand, ref time, 12, 32f, 620f, 1f, 0f, 0f, -1f);
            AddRest(forehand, ref time, 40, -1f);
            time = 0.0;
            AddGesture(backhand, ref time, 12, 32f, 620f, -1f, 0f, 0f, -1f);
            AddRest(backhand, ref time, 40, -1f);

            Assert.AreEqual(0f, Run(forehand, Settings()).Final[0].Spin);
            Assert.Less(Run(backhand, Settings()).Final[0].Spin, -0.5f);
        }

        [Test]
        public void SecondGestureDuringCooldown_IsIgnored()
        {
            var frames = new List<MotionFrame>();
            double time = 0.0;
            AddGesture(frames, ref time, 12, 32f, 620f, 1f, 0f);
            AddRest(frames, ref time, 3);
            AddGesture(frames, ref time, 12, 32f, 620f, 1f, 0f);
            AddRest(frames, ref time, 60);
            AddGesture(frames, ref time, 12, 32f, 620f, 1f, 0f);
            AddRest(frames, ref time, 10);

            Assert.AreEqual(2, Run(frames, Settings()).Impacts.Count);
        }

        [Test]
        public void ShakeWithoutRotation_IsNotASwing()
        {
            var frames = new List<MotionFrame>();
            double time = 0.0;
            AddGesture(frames, ref time, 12, 40f, 60f, 1f, 0f);
            AddRest(frames, ref time, 30);

            Assert.IsEmpty(Run(frames, Settings()).Impacts);
        }

        [Test]
        public void LongGesture_IsReportedAtMaxDuration()
        {
            var frames = new List<MotionFrame>();
            double time = 0.0;
            for (int i = 0; i < 60; i++)
            {
                frames.Add(new MotionFrame { TimeSeconds = time, Acceleration = 30f, RotationSpeed = 600f, YawRate = 600f });
                time += FrameSeconds;
            }

            Outcome outcome = Run(frames, Settings());

            Assert.AreEqual(1, outcome.Impacts.Count);
            Assert.AreEqual(0.25f, outcome.Impacts[0].DurationSeconds, 0.02f);
        }

        [Test]
        public void Replay_ParsesRecorderFormats()
        {
            string[] lines =
            {
                SensorRecorder.Header,
                "1000.0,5000.0,1.000,2.000,3.000,4.000,5.000,6.000,7.000,8.000,9.000,10.000,11.000,12.000,3,4",
                "2000.0,6000.0,1.000,2.000,3.000,4.000,5.000,6.000,7.000,8.000,9.000,10.000,11.000,12.000,2",
                "ligne illisible",
            };

            List<SwingReplay.Entry> entries = SwingReplay.Parse(lines);

            Assert.AreEqual(2, entries.Count);
            Assert.AreEqual(3, entries[0].Label);
            Assert.AreEqual(4, entries[0].Repetition);
            Assert.AreEqual(1000.0, entries[0].Sample.PhoneTimeMs, 1e-9);
            Assert.AreEqual(5000.0, entries[0].Sample.ServerTimeMs, 1e-9);
            Assert.AreEqual(12f, entries[0].Sample.Orientation.z, 1e-6f);
            Assert.AreEqual(2, entries[1].Label);
            Assert.AreEqual(0, entries[1].Repetition);
        }
    }
}
