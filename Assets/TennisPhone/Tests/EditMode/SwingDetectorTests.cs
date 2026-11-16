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
                MinDuration = 0.03f,
                MaxDuration = 0.25f,
                Cooldown = 0.35f,
                PowerAccelerationMin = 12f,
                PowerAccelerationMax = 52f,
                PowerRotationMin = 220f,
                PowerRotationMax = 1020f,
                PowerRotationWeight = 0.5f,
                SpinRollRateFull = 500f,
                SpinDeadZone = 0.15f,
            };
        }

        /// <summary>Geste en cloche : monte puis redescend en « frames » mesures.</summary>
        static void AddGesture(List<MotionFrame> frames, ref double time, int count,
            float peakAcceleration, float peakRotation, float yawSign, float rollRate)
        {
            for (int i = 0; i < count; i++)
            {
                float shape = 1f - System.Math.Abs((i + 0.5f) / count * 2f - 1f);
                frames.Add(new MotionFrame
                {
                    TimeSeconds = time,
                    Acceleration = peakAcceleration * shape,
                    RotationSpeed = peakRotation * shape,
                    YawRate = peakRotation * shape * yawSign,
                    RollRate = rollRate,
                });
                time += FrameSeconds;
            }
        }

        static void AddRest(List<MotionFrame> frames, ref double time, int count)
        {
            for (int i = 0; i < count; i++)
            {
                frames.Add(new MotionFrame { TimeSeconds = time, Acceleration = 0.4f, RotationSpeed = 8f });
                time += FrameSeconds;
            }
        }

        static List<SwingEvent> Run(List<MotionFrame> frames, SwingSettings settings)
        {
            var detector = new SwingDetector(settings);
            var swings = new List<SwingEvent>();
            foreach (MotionFrame frame in frames)
            {
                if (detector.Process(frame, out SwingEvent swing))
                {
                    swings.Add(swing);
                }
            }

            return swings;
        }

        [Test]
        public void Rest_ProducesNoSwing()
        {
            var frames = new List<MotionFrame>();
            double time = 0.0;
            AddRest(frames, ref time, 300);

            Assert.IsEmpty(Run(frames, Settings()));
        }

        [Test]
        public void SingleGesture_ProducesOneForehand()
        {
            var frames = new List<MotionFrame>();
            double time = 0.0;
            AddRest(frames, ref time, 30);
            AddGesture(frames, ref time, 12, 32f, 620f, 1f, 0f);
            AddRest(frames, ref time, 60);

            List<SwingEvent> swings = Run(frames, Settings());

            Assert.AreEqual(1, swings.Count);
            Assert.AreEqual(SwingSide.Forehand, swings[0].Side);
            Assert.AreEqual(0f, swings[0].Spin);
            Assert.Greater(swings[0].Power, 0.3f);
            Assert.Less(swings[0].Power, 0.7f);
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

            Assert.AreEqual(SwingSide.Backhand, Run(frames, Settings())[0].Side);
            Assert.AreEqual(SwingSide.Forehand, Run(frames, leftHanded)[0].Side);
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

            float softPower = Run(soft, Settings())[0].Power;
            float hardPower = Run(hard, Settings())[0].Power;

            Assert.Greater(hardPower, softPower);
            Assert.AreEqual(1f, hardPower, 1e-4f);
        }

        [Test]
        public void WristRoll_GivesTopspinOrSlice_MirroredOnBackhand()
        {
            var forehandRoll = new List<MotionFrame>();
            var backhandRoll = new List<MotionFrame>();
            double time = 0.0;
            AddGesture(forehandRoll, ref time, 12, 32f, 620f, 1f, 400f);
            AddRest(forehandRoll, ref time, 10);
            time = 0.0;
            AddGesture(backhandRoll, ref time, 12, 32f, 620f, -1f, 400f);
            AddRest(backhandRoll, ref time, 10);

            float forehandSpin = Run(forehandRoll, Settings())[0].Spin;
            float backhandSpin = Run(backhandRoll, Settings())[0].Spin;

            Assert.AreEqual(0.8f, forehandSpin, 1e-3f);
            Assert.AreEqual(-0.8f, backhandSpin, 1e-3f);
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

            Assert.AreEqual(2, Run(frames, Settings()).Count);
        }

        [Test]
        public void ShakeWithoutRotation_IsNotASwing()
        {
            var frames = new List<MotionFrame>();
            double time = 0.0;
            AddGesture(frames, ref time, 12, 40f, 60f, 1f, 0f);
            AddRest(frames, ref time, 30);

            Assert.IsEmpty(Run(frames, Settings()));
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

            List<SwingEvent> swings = Run(frames, Settings());

            Assert.AreEqual(1, swings.Count);
            Assert.AreEqual(0.25f, swings[0].DurationSeconds, 0.02f);
        }

        [Test]
        public void Replay_ParsesRecorderFormat()
        {
            string[] lines =
            {
                SensorRecorder.Header,
                "1000.0,5000.0,1.000,2.000,3.000,4.000,5.000,6.000,7.000,8.000,9.000,10.000,11.000,12.000,3",
                "ligne illisible",
            };

            List<KeyValuePair<SensorSample, int>> samples = SwingReplay.Parse(lines);

            Assert.AreEqual(1, samples.Count);
            Assert.AreEqual(3, samples[0].Value);
            Assert.AreEqual(1000.0, samples[0].Key.PhoneTimeMs, 1e-9);
            Assert.AreEqual(5000.0, samples[0].Key.ServerTimeMs, 1e-9);
            Assert.AreEqual(12f, samples[0].Key.Orientation.z, 1e-6f);
        }
    }
}
