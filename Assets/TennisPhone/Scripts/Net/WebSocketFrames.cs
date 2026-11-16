using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TennisPhone
{
    /// <summary>
    /// Trames WebSocket (RFC 6455) côté serveur : poignée de main, lecture et écriture.
    /// </summary>
    public static class WebSocketFrames
    {
        public const byte OpcodeContinuation = 0x0;
        public const byte OpcodeText = 0x1;
        public const byte OpcodeBinary = 0x2;
        public const byte OpcodeClose = 0x8;
        public const byte OpcodePing = 0x9;
        public const byte OpcodePong = 0xA;

        public const int MaxPayloadBytes = 64 * 1024;

        const string HandshakeGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        /// <summary>Valeur de l'en-tête Sec-WebSocket-Accept pour une clé cliente.</summary>
        public static string ComputeAcceptKey(string clientKey)
        {
            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] hash = sha1.ComputeHash(Encoding.ASCII.GetBytes(clientKey.Trim() + HandshakeGuid));
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>Trame serveur vers client (non masquée, en un seul fragment).</summary>
        public static byte[] EncodeFrame(byte opcode, byte[] payload)
        {
            int length = payload.Length;
            int headerLength = length < 126 ? 2 : (length <= ushort.MaxValue ? 4 : 10);
            byte[] frame = new byte[headerLength + length];
            frame[0] = (byte)(0x80 | (opcode & 0x0F));

            if (length < 126)
            {
                frame[1] = (byte)length;
            }
            else if (length <= ushort.MaxValue)
            {
                frame[1] = 126;
                frame[2] = (byte)(length >> 8);
                frame[3] = (byte)length;
            }
            else
            {
                frame[1] = 127;
                ulong longLength = (ulong)length;
                for (int i = 0; i < 8; i++)
                {
                    frame[2 + i] = (byte)(longLength >> (8 * (7 - i)));
                }
            }

            Buffer.BlockCopy(payload, 0, frame, headerLength, length);
            return frame;
        }

        public static byte[] EncodeText(string text)
        {
            return EncodeFrame(OpcodeText, Encoding.UTF8.GetBytes(text));
        }

        /// <summary>
        /// Lit une trame (appel bloquant). Renvoie false si le flux est fermé.
        /// Lève InvalidDataException si la trame est trop grande.
        /// </summary>
        public static bool TryReadFrame(Stream stream, out byte opcode, out bool final, out byte[] payload)
        {
            opcode = 0;
            final = false;
            payload = null;

            byte[] header = new byte[2];
            if (!ReadExactly(stream, header, 2))
            {
                return false;
            }

            final = (header[0] & 0x80) != 0;
            opcode = (byte)(header[0] & 0x0F);
            bool masked = (header[1] & 0x80) != 0;
            ulong length = (ulong)(header[1] & 0x7F);

            if (length == 126)
            {
                byte[] extended = new byte[2];
                if (!ReadExactly(stream, extended, 2))
                {
                    return false;
                }

                length = (ulong)((extended[0] << 8) | extended[1]);
            }
            else if (length == 127)
            {
                byte[] extended = new byte[8];
                if (!ReadExactly(stream, extended, 8))
                {
                    return false;
                }

                length = 0;
                for (int i = 0; i < 8; i++)
                {
                    length = (length << 8) | extended[i];
                }
            }

            if (length > MaxPayloadBytes)
            {
                throw new InvalidDataException("Trame WebSocket trop grande : " + length + " octets.");
            }

            byte[] mask = null;
            if (masked)
            {
                mask = new byte[4];
                if (!ReadExactly(stream, mask, 4))
                {
                    return false;
                }
            }

            payload = new byte[(int)length];
            if (payload.Length > 0 && !ReadExactly(stream, payload, payload.Length))
            {
                return false;
            }

            if (masked)
            {
                for (int i = 0; i < payload.Length; i++)
                {
                    payload[i] ^= mask[i & 3];
                }
            }

            return true;
        }

        static bool ReadExactly(Stream stream, byte[] buffer, int count)
        {
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(buffer, offset, count - offset);
                if (read <= 0)
                {
                    return false;
                }

                offset += read;
            }

            return true;
        }
    }
}
