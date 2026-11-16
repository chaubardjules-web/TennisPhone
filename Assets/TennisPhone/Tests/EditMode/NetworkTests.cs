using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace TennisPhone.Tests
{
    public class WebSocketFramesTests
    {
        [Test]
        public void AcceptKey_MatchesRfc6455Example()
        {
            Assert.AreEqual("s3pPLMBiTxaQ9kYGzzhZRbK+xOo=",
                WebSocketFrames.ComputeAcceptKey("dGhlIHNhbXBsZSBub25jZQ=="));
        }

        [Test]
        public void ReadFrame_UnmasksClientText()
        {
            // Exemple de la RFC 6455 : « Hello » masqué.
            byte[] frame = { 0x81, 0x85, 0x37, 0xfa, 0x21, 0x3d, 0x7f, 0x9f, 0x4d, 0x51, 0x58 };

            bool read = WebSocketFrames.TryReadFrame(new MemoryStream(frame), out byte opcode, out bool final, out byte[] payload);

            Assert.IsTrue(read);
            Assert.IsTrue(final);
            Assert.AreEqual(WebSocketFrames.OpcodeText, opcode);
            Assert.AreEqual("Hello", Encoding.UTF8.GetString(payload));
        }

        [Test]
        public void EncodeText_RoundTripsThroughReader()
        {
            string text = new string('é', 300);

            byte[] frame = WebSocketFrames.EncodeText(text);
            bool read = WebSocketFrames.TryReadFrame(new MemoryStream(frame), out byte opcode, out bool _, out byte[] payload);

            Assert.IsTrue(read);
            Assert.AreEqual(WebSocketFrames.OpcodeText, opcode);
            Assert.AreEqual(text, Encoding.UTF8.GetString(payload));
        }

        [Test]
        public void ReadFrame_ReturnsFalseOnTruncatedStream()
        {
            byte[] truncated = { 0x81, 0x85, 0x37, 0xfa };

            Assert.IsFalse(WebSocketFrames.TryReadFrame(new MemoryStream(truncated), out byte _, out bool _, out byte[] _));
        }
    }

    public class SensorSampleTests
    {
        [Test]
        public void TryParse_ReadsAllFields()
        {
            bool parsed = SensorSample.TryParse("m,1234.5,1,2,3,4,5,6,7,8,9,10,11,12", 42.0, out SensorSample sample);

            Assert.IsTrue(parsed);
            Assert.AreEqual(1234.5, sample.PhoneTimeMs, 1e-9);
            Assert.AreEqual(42.0, sample.ServerTimeMs, 1e-9);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), sample.Acceleration);
            Assert.AreEqual(new Vector3(4f, 5f, 6f), sample.AccelerationWithGravity);
            Assert.AreEqual(new Vector3(7f, 8f, 9f), sample.RotationRate);
            Assert.AreEqual(new Vector3(10f, 11f, 12f), sample.Orientation);
        }

        [TestCase("")]
        [TestCase("x,1,2,3")]
        [TestCase("m,1,2,3")]
        [TestCase("m,1,2,3,4,5,6,7,8,9,10,11,douze")]
        [TestCase("m,1,2,3,4,5,6,7,8,9,10,11,NaN")]
        public void TryParse_RejectsMalformedLines(string line)
        {
            Assert.IsFalse(SensorSample.TryParse(line, 0.0, out SensorSample _));
        }
    }

    public class DeviceOrientationMathTests
    {
        static void AssertDirection(Vector3 expected, Vector3 actual)
        {
            Assert.Less(Vector3.Distance(expected, actual), 1e-4f, "attendu " + expected + ", obtenu " + actual);
        }

        [Test]
        public void FlatPhone_IsIdentity()
        {
            Quaternion rotation = DeviceOrientationMath.ToUnityRotation(Vector3.zero);

            AssertDirection(Vector3.forward, rotation * Vector3.forward);
            AssertDirection(Vector3.up, rotation * Vector3.up);
        }

        [Test]
        public void UprightPhone_TopPointsUp()
        {
            // beta = 90° : téléphone debout, écran face à l'utilisateur.
            Quaternion rotation = DeviceOrientationMath.ToUnityRotation(new Vector3(0f, 90f, 0f));

            AssertDirection(Vector3.up, rotation * Vector3.forward);
            AssertDirection(Vector3.back, rotation * Vector3.up);
        }

        [Test]
        public void QuarterTurnLeft_TopPointsWest()
        {
            // alpha = 90° : rotation d'un quart de tour dans le sens inverse des aiguilles, vue de dessus.
            Quaternion rotation = DeviceOrientationMath.ToUnityRotation(new Vector3(90f, 0f, 0f));

            AssertDirection(Vector3.left, rotation * Vector3.forward);
            AssertDirection(Vector3.up, rotation * Vector3.up);
        }
    }

    public class QrCodeTests
    {
        [Test]
        public void Encode_PicksSmallestVersion()
        {
            Assert.AreEqual(21, QrCode.Encode("bonjour").GetLength(0));
            Assert.AreEqual(25, QrCode.Encode("https://192.168.144.64:8443/").GetLength(0));
        }

        [Test]
        public void Encode_DrawsFinderPatterns()
        {
            bool[,] modules = QrCode.Encode("https://192.168.1.20:8443/");
            int size = modules.GetLength(0);

            // Coins extérieurs et centres sombres, anneau intermédiaire clair.
            Assert.IsTrue(modules[0, 0]);
            Assert.IsFalse(modules[1, 1]);
            Assert.IsTrue(modules[3, 3]);
            Assert.IsTrue(modules[0, size - 1]);
            Assert.IsTrue(modules[3, size - 4]);
            Assert.IsTrue(modules[size - 1, 0]);
            Assert.IsTrue(modules[size - 4, 3]);

            // Module toujours sombre.
            Assert.IsTrue(modules[size - 8, 8]);
        }

        [Test]
        public void Encode_RejectsTooLongText()
        {
            Assert.Throws<System.ArgumentException>(() => QrCode.Encode(new string('a', QrCode.MaxBytes + 1)));
        }
    }
}
