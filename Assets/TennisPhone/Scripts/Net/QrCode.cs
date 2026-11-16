using System;
using System.Text;

namespace TennisPhone
{
    /// <summary>
    /// Générateur de QR code minimal : mode octets, correction d'erreur de niveau L,
    /// versions 1 à 5 (jusqu'à 106 octets), ce qui suffit pour une URL locale.
    /// </summary>
    public static class QrCode
    {
        public const int MaxBytes = 106;

        const int MinVersion = 1;
        const int MaxVersion = 5;
        const int FormatBitsLevelL = 1;

        // Indexés par version. Un seul bloc de correction pour les versions 1 à 5 au niveau L.
        static readonly int[] DataCodewords = { 0, 19, 34, 55, 80, 108 };
        static readonly int[] EccCodewords = { 0, 7, 10, 15, 20, 26 };

        /// <summary>
        /// Encode un texte. Le résultat est indexé [ligne, colonne], true = module sombre.
        /// La zone de silence autour du code n'est pas incluse.
        /// </summary>
        public static bool[,] Encode(string text)
        {
            byte[] data = Encoding.UTF8.GetBytes(text ?? string.Empty);

            int version = 0;
            for (int v = MinVersion; v <= MaxVersion; v++)
            {
                // 4 bits de mode + 8 bits de longueur + les données doivent tenir.
                if (data.Length <= DataCodewords[v] - 2)
                {
                    version = v;
                    break;
                }
            }

            if (version == 0)
            {
                throw new ArgumentException("Texte trop long pour un QR code (maximum " + MaxBytes + " octets).");
            }

            byte[] dataCodewords = BuildDataCodewords(data, DataCodewords[version]);
            byte[] ecc = ReedSolomonRemainder(dataCodewords, EccCodewords[version]);
            byte[] all = new byte[dataCodewords.Length + ecc.Length];
            Buffer.BlockCopy(dataCodewords, 0, all, 0, dataCodewords.Length);
            Buffer.BlockCopy(ecc, 0, all, dataCodewords.Length, ecc.Length);

            int size = version * 4 + 17;
            bool[,] modules = new bool[size, size];
            bool[,] isFunction = new bool[size, size];

            DrawFunctionPatterns(modules, isFunction, size, version);
            DrawCodewords(modules, isFunction, size, all);

            int bestMask = 0;
            int bestPenalty = int.MaxValue;
            for (int mask = 0; mask < 8; mask++)
            {
                ApplyMask(modules, isFunction, size, mask);
                DrawFormatBits(modules, isFunction, size, mask);
                int penalty = ComputePenalty(modules, size);
                if (penalty < bestPenalty)
                {
                    bestPenalty = penalty;
                    bestMask = mask;
                }

                ApplyMask(modules, isFunction, size, mask);
            }

            ApplyMask(modules, isFunction, size, bestMask);
            DrawFormatBits(modules, isFunction, size, bestMask);
            return modules;
        }

        static byte[] BuildDataCodewords(byte[] data, int capacity)
        {
            byte[] codewords = new byte[capacity];
            int bitPosition = 0;

            void Put(int value, int count)
            {
                for (int i = count - 1; i >= 0; i--)
                {
                    if (((value >> i) & 1) != 0)
                    {
                        codewords[bitPosition >> 3] |= (byte)(0x80 >> (bitPosition & 7));
                    }

                    bitPosition++;
                }
            }

            Put(0x4, 4);
            Put(data.Length, 8);
            foreach (byte b in data)
            {
                Put(b, 8);
            }

            // Terminateur (jusqu'à 4 zéros), puis alignement sur l'octet.
            bitPosition += Math.Min(4, capacity * 8 - bitPosition);
            bitPosition = (bitPosition + 7) & ~7;

            for (int pad = 0xEC; bitPosition < capacity * 8; pad ^= 0xEC ^ 0x11)
            {
                Put(pad, 8);
            }

            return codewords;
        }

        static void DrawFunctionPatterns(bool[,] modules, bool[,] isFunction, int size, int version)
        {
            for (int i = 0; i < size; i++)
            {
                SetFunction(modules, isFunction, 6, i, i % 2 == 0);
                SetFunction(modules, isFunction, i, 6, i % 2 == 0);
            }

            DrawFinder(modules, isFunction, size, 3, 3);
            DrawFinder(modules, isFunction, size, size - 4, 3);
            DrawFinder(modules, isFunction, size, 3, size - 4);

            if (version >= 2)
            {
                int center = size - 7;
                for (int dy = -2; dy <= 2; dy++)
                {
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        bool dark = Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1;
                        SetFunction(modules, isFunction, center + dx, center + dy, dark);
                    }
                }
            }

            // Réserve les zones d'information de format.
            DrawFormatBits(modules, isFunction, size, 0);
        }

        static void DrawFinder(bool[,] modules, bool[,] isFunction, int size, int centerX, int centerY)
        {
            for (int dy = -4; dy <= 4; dy++)
            {
                for (int dx = -4; dx <= 4; dx++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;
                    if (x < 0 || x >= size || y < 0 || y >= size)
                    {
                        continue;
                    }

                    int distance = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    SetFunction(modules, isFunction, x, y, distance != 2 && distance != 4);
                }
            }
        }

        static void DrawFormatBits(bool[,] modules, bool[,] isFunction, int size, int mask)
        {
            int data = (FormatBitsLevelL << 3) | mask;
            int remainder = data;
            for (int i = 0; i < 10; i++)
            {
                remainder = (remainder << 1) ^ ((remainder >> 9) * 0x537);
            }

            int bits = ((data << 10) | remainder) ^ 0x5412;

            // Première copie, autour du motif de repérage en haut à gauche.
            for (int i = 0; i <= 5; i++)
            {
                SetFunction(modules, isFunction, 8, i, GetBit(bits, i));
            }

            SetFunction(modules, isFunction, 8, 7, GetBit(bits, 6));
            SetFunction(modules, isFunction, 8, 8, GetBit(bits, 7));
            SetFunction(modules, isFunction, 7, 8, GetBit(bits, 8));
            for (int i = 9; i < 15; i++)
            {
                SetFunction(modules, isFunction, 14 - i, 8, GetBit(bits, i));
            }

            // Seconde copie, répartie entre les deux autres motifs de repérage.
            for (int i = 0; i < 8; i++)
            {
                SetFunction(modules, isFunction, size - 1 - i, 8, GetBit(bits, i));
            }

            for (int i = 8; i < 15; i++)
            {
                SetFunction(modules, isFunction, 8, size - 15 + i, GetBit(bits, i));
            }

            // Module toujours sombre.
            SetFunction(modules, isFunction, 8, size - 8, true);
        }

        static void DrawCodewords(bool[,] modules, bool[,] isFunction, int size, byte[] codewords)
        {
            int bitIndex = 0;
            int totalBits = codewords.Length * 8;

            for (int right = size - 1; right >= 1; right -= 2)
            {
                if (right == 6)
                {
                    right = 5;
                }

                for (int vertical = 0; vertical < size; vertical++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        int x = right - j;
                        bool upward = ((right + 1) & 2) == 0;
                        int y = upward ? size - 1 - vertical : vertical;

                        if (!isFunction[y, x] && bitIndex < totalBits)
                        {
                            modules[y, x] = ((codewords[bitIndex >> 3] >> (7 - (bitIndex & 7))) & 1) != 0;
                            bitIndex++;
                        }
                    }
                }
            }
        }

        static void ApplyMask(bool[,] modules, bool[,] isFunction, int size, int mask)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (isFunction[y, x])
                    {
                        continue;
                    }

                    bool invert;
                    switch (mask)
                    {
                        case 0: invert = (x + y) % 2 == 0; break;
                        case 1: invert = y % 2 == 0; break;
                        case 2: invert = x % 3 == 0; break;
                        case 3: invert = (x + y) % 3 == 0; break;
                        case 4: invert = (x / 3 + y / 2) % 2 == 0; break;
                        case 5: invert = x * y % 2 + x * y % 3 == 0; break;
                        case 6: invert = (x * y % 2 + x * y % 3) % 2 == 0; break;
                        default: invert = ((x + y) % 2 + x * y % 3) % 2 == 0; break;
                    }

                    if (invert)
                    {
                        modules[y, x] = !modules[y, x];
                    }
                }
            }
        }

        static int ComputePenalty(bool[,] modules, int size)
        {
            int penalty = 0;

            // Règle 1 : suites de 5 modules ou plus de même couleur, en ligne puis en colonne.
            for (int pass = 0; pass < 2; pass++)
            {
                for (int a = 0; a < size; a++)
                {
                    int run = 1;
                    for (int b = 1; b < size; b++)
                    {
                        bool current = pass == 0 ? modules[a, b] : modules[b, a];
                        bool previous = pass == 0 ? modules[a, b - 1] : modules[b - 1, a];
                        if (current == previous)
                        {
                            run++;
                            if (run == 5)
                            {
                                penalty += 3;
                            }
                            else if (run > 5)
                            {
                                penalty++;
                            }
                        }
                        else
                        {
                            run = 1;
                        }
                    }
                }
            }

            // Règle 2 : blocs 2 x 2 de même couleur.
            for (int y = 0; y < size - 1; y++)
            {
                for (int x = 0; x < size - 1; x++)
                {
                    bool color = modules[y, x];
                    if (color == modules[y, x + 1] && color == modules[y + 1, x] && color == modules[y + 1, x + 1])
                    {
                        penalty += 3;
                    }
                }
            }

            // Règle 3 : motifs qui ressemblent à un motif de repérage (1011101 bordé de 4 clairs).
            bool[] patternA = { true, false, true, true, true, false, true, false, false, false, false };
            bool[] patternB = { false, false, false, false, true, false, true, true, true, false, true };
            for (int pass = 0; pass < 2; pass++)
            {
                for (int a = 0; a < size; a++)
                {
                    for (int b = 0; b + 11 <= size; b++)
                    {
                        bool matchA = true;
                        bool matchB = true;
                        for (int k = 0; k < 11 && (matchA || matchB); k++)
                        {
                            bool value = pass == 0 ? modules[a, b + k] : modules[b + k, a];
                            if (value != patternA[k])
                            {
                                matchA = false;
                            }

                            if (value != patternB[k])
                            {
                                matchB = false;
                            }
                        }

                        if (matchA)
                        {
                            penalty += 40;
                        }

                        if (matchB)
                        {
                            penalty += 40;
                        }
                    }
                }
            }

            // Règle 4 : équilibre entre modules sombres et clairs.
            int dark = 0;
            foreach (bool module in modules)
            {
                if (module)
                {
                    dark++;
                }
            }

            int total = size * size;
            int k4 = (Math.Abs(dark * 20 - total * 10) + total - 1) / total - 1;
            penalty += Math.Max(0, k4) * 10;

            return penalty;
        }

        static void SetFunction(bool[,] modules, bool[,] isFunction, int x, int y, bool dark)
        {
            modules[y, x] = dark;
            isFunction[y, x] = true;
        }

        static bool GetBit(int value, int index)
        {
            return ((value >> index) & 1) != 0;
        }

        static byte[] ReedSolomonRemainder(byte[] data, int degree)
        {
            // Polynôme générateur : produit des (x - 2^i) pour i de 0 à degree - 1.
            byte[] divisor = new byte[degree];
            divisor[degree - 1] = 1;
            int root = 1;
            for (int i = 0; i < degree; i++)
            {
                for (int j = 0; j < degree; j++)
                {
                    divisor[j] = Multiply(divisor[j], root);
                    if (j + 1 < degree)
                    {
                        divisor[j] ^= divisor[j + 1];
                    }
                }

                root = Multiply(root, 0x02);
            }

            byte[] result = new byte[degree];
            foreach (byte b in data)
            {
                int factor = b ^ result[0];
                Array.Copy(result, 1, result, 0, degree - 1);
                result[degree - 1] = 0;
                for (int i = 0; i < degree; i++)
                {
                    result[i] ^= Multiply(divisor[i], factor);
                }
            }

            return result;
        }

        // Multiplication dans GF(2^8) modulo 0x11D.
        static byte Multiply(int x, int y)
        {
            int z = 0;
            for (int i = 7; i >= 0; i--)
            {
                z = (z << 1) ^ ((z >> 7) * 0x11D);
                z ^= ((y >> i) & 1) * x;
            }

            return (byte)z;
        }
    }
}
