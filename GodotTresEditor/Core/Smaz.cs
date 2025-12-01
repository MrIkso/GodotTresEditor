using System;
using System.IO;
using System.Text;

namespace GodotTresEditor.Core
{
    public static class Smaz
    {
        #region Lookup Data

        // Smaz_cb (Compression Codebook)
        private static readonly byte[][] encoderLookup = {
            new byte[] {2, 115, 44, 182}, new byte[] {3, 104, 97, 100, 154, 2, 108, 101, 87}, new byte[] {3, 111, 110, 32, 142}, null, new byte[] {1, 121, 83},
            new byte[] {2, 109, 97, 173, 2, 108, 105, 151}, new byte[] {3, 111, 114, 32, 176}, null, new byte[] {2, 108, 108, 152, 3, 115, 32, 116, 191},
            new byte[] {4, 102, 114, 111, 109, 103, 2, 109, 101, 108}, null, new byte[] {3, 105, 116, 115, 218}, new byte[] {1, 122, 219},
            new byte[] {3, 105, 110, 103, 70}, new byte[] {1, 62, 222}, new byte[] {1, 32, 0, 3, 32, 32, 32, 40, 2, 110, 99, 228},
            new byte[] {2, 110, 100, 61, 3, 32, 111, 110, 202}, new byte[] {2, 110, 101, 139, 3, 104, 97, 116, 190, 3, 114, 101, 32, 113}, null,
            new byte[] {2, 110, 103, 84, 3, 104, 101, 114, 122, 4, 104, 97, 118, 101, 198, 3, 115, 32, 111, 149}, null,
            new byte[] {3, 105, 111, 110, 107, 3, 115, 32, 97, 172, 2, 108, 121, 234}, new byte[] {3, 104, 105, 115, 76, 3, 32, 105, 110, 78, 3, 32, 98, 101, 170},
            null, new byte[] {3, 32, 102, 111, 213, 3, 32, 111, 102, 32, 3, 32, 104, 97, 201}, null, new byte[] {2, 111, 102, 5},
            new byte[] {3, 32, 99, 111, 161, 2, 110, 111, 183, 3, 32, 109, 97, 248}, null, null,
            new byte[] {3, 32, 99, 108, 238, 3, 101, 110, 116, 97, 3, 32, 97, 110, 55}, new byte[] {2, 110, 115, 192, 1, 34, 101},
            new byte[] {3, 110, 32, 116, 143, 2, 110, 116, 80, 3, 115, 44, 32, 133}, new byte[] {2, 112, 101, 208, 3, 32, 119, 101, 233, 2, 111, 109, 147},
            new byte[] {2, 111, 110, 31}, null, new byte[] {2, 121, 32, 71}, new byte[] {3, 32, 119, 97, 185}, new byte[] {3, 32, 114, 101, 209, 2, 111, 114, 42},
            null, new byte[] {2, 61, 34, 169, 2, 111, 116, 223}, new byte[] {3, 102, 111, 114, 68, 2, 111, 117, 91}, new byte[] {3, 32, 116, 111, 82},
            new byte[] {3, 32, 116, 104, 13}, new byte[] {3, 32, 105, 116, 246}, new byte[] {3, 98, 117, 116, 177, 2, 114, 97, 130, 3, 32, 119, 105, 243, 2, 60, 47, 241},
            new byte[] {3, 32, 119, 104, 159}, new byte[] {2, 32, 32, 52}, new byte[] {3, 110, 100, 32, 63}, new byte[] {2, 114, 101, 33}, null,
            new byte[] {3, 110, 103, 32, 99}, null, new byte[] {3, 108, 121, 32, 199, 3, 97, 115, 115, 211, 1, 97, 4, 2, 114, 105, 114}, null, null, null,
            new byte[] {2, 115, 101, 95}, new byte[] {3, 111, 102, 32, 34}, new byte[] {3, 100, 105, 118, 244, 2, 114, 111, 115, 3, 101, 114, 101, 160}, null,
            new byte[] {2, 116, 97, 200, 1, 98, 90, 2, 115, 105, 212}, null, new byte[] {3, 97, 110, 100, 7, 2, 114, 115, 221}, new byte[] {2, 114, 116, 242},
            new byte[] {2, 116, 101, 69}, new byte[] {3, 97, 116, 105, 206}, new byte[] {2, 115, 111, 179}, new byte[] {2, 116, 104, 17},
            new byte[] {2, 116, 105, 74, 1, 99, 28, 3, 97, 108, 108, 112}, new byte[] {3, 97, 116, 101, 229}, new byte[] {2, 115, 115, 166}, new byte[] {2, 115, 116, 77},
            null, new byte[] {2, 62, 60, 230}, new byte[] {2, 116, 111, 20}, new byte[] {3, 97, 114, 101, 119}, new byte[] {1, 100, 24}, new byte[] {2, 116, 114, 195},
            null, new byte[] {1, 10, 49, 3, 32, 97, 32, 146}, new byte[] {3, 102, 32, 116, 118, 2, 118, 101, 111}, new byte[] {2, 117, 110, 224}, null,
            new byte[] {3, 101, 32, 111, 162}, new byte[] {2, 97, 32, 163, 2, 119, 97, 214, 1, 101, 2}, new byte[] {2, 117, 114, 150, 3, 101, 32, 97, 188},
            new byte[] {2, 117, 115, 164, 3, 10, 13, 10, 167}, new byte[] {2, 117, 116, 196, 3, 101, 32, 99, 251}, new byte[] {2, 119, 101, 145}, null, null,
            new byte[] {2, 119, 104, 194}, new byte[] {1, 102, 44}, null, null, null, new byte[] {3, 100, 32, 116, 134}, null, null, new byte[] {3, 116, 104, 32, 227},
            new byte[] {1, 103, 59}, null, null, new byte[] {1, 13, 57, 3, 101, 32, 115, 181}, new byte[] {3, 101, 32, 116, 156}, null, new byte[] {3, 116, 111, 32, 89},
            new byte[] {3, 101, 13, 10, 158}, new byte[] {2, 100, 32, 30, 1, 104, 18}, null, new byte[] {1, 44, 81}, new byte[] {2, 32, 97, 25},
            new byte[] {2, 32, 98, 94}, new byte[] {2, 13, 10, 21, 2, 32, 99, 73}, new byte[] {2, 32, 100, 165}, new byte[] {2, 32, 101, 171},
            new byte[] {2, 32, 102, 104, 1, 105, 8, 2, 101, 32, 11}, null, new byte[] {2, 32, 104, 85, 1, 45, 204}, new byte[] {2, 32, 105, 56}, null, null,
            new byte[] {2, 32, 108, 205}, new byte[] {2, 32, 109, 123}, new byte[] {2, 102, 32, 58, 2, 32, 110, 236}, new byte[] {2, 32, 111, 29},
            new byte[] {2, 32, 112, 125, 1, 46, 110, 3, 13, 10, 13, 168}, null, new byte[] {2, 32, 114, 189}, new byte[] {2, 32, 115, 62}, new byte[] {2, 32, 116, 14},
            null, new byte[] {2, 103, 32, 157, 5, 119, 104, 105, 99, 104, 43, 3, 119, 104, 105, 247}, new byte[] {2, 32, 119, 53}, new byte[] {1, 47, 197},
            new byte[] {3, 97, 115, 32, 140}, new byte[] {3, 97, 116, 32, 135}, null, new byte[] {3, 119, 104, 111, 217}, null, new byte[] {1, 108, 22, 2, 104, 32, 138},
            null, new byte[] {2, 44, 32, 36}, null, new byte[] {4, 119, 105, 116, 104, 86}, null, null, null, new byte[] {1, 109, 45}, null, null,
            new byte[] {2, 97, 99, 239}, new byte[] {2, 97, 100, 232}, new byte[] {3, 84, 104, 101, 72}, null, null, new byte[] {4, 116, 104, 105, 115, 155, 1, 110, 9},
            null, new byte[] {2, 46, 32, 121}, null, new byte[] {2, 97, 108, 88, 3, 101, 44, 32, 245}, new byte[] {3, 116, 105, 111, 141, 2, 98, 101, 92},
            new byte[] {2, 97, 110, 26, 3, 118, 101, 114, 231}, null, new byte[] {4, 116, 104, 97, 116, 48, 3, 116, 104, 97, 203, 1, 111, 6},
            new byte[] {3, 119, 97, 115, 50}, new byte[] {2, 97, 114, 79}, new byte[] {2, 97, 115, 46},
            new byte[] {2, 97, 116, 39, 3, 116, 104, 101, 1, 4, 116, 104, 101, 121, 128, 5, 116, 104, 101, 114, 101, 210, 5, 116, 104, 101, 105, 114, 100},
            new byte[] {2, 99, 101, 136}, new byte[] {4, 119, 101, 114, 101, 93}, null, new byte[] {2, 99, 104, 153, 2, 108, 32, 180, 1, 112, 60}, null, null,
            new byte[] {3, 111, 110, 101, 174}, null, new byte[] {3, 104, 101, 32, 19, 2, 100, 101, 106}, new byte[] {3, 116, 101, 114, 184},
            new byte[] {2, 99, 111, 117}, null, new byte[] {2, 98, 121, 127, 2, 100, 105, 129, 2, 101, 97, 120}, null, new byte[] {2, 101, 99, 215},
            new byte[] {2, 101, 100, 66}, new byte[] {2, 101, 101, 235}, null, null, new byte[] {1, 114, 12, 2, 110, 32, 41}, null, null, null,
            new byte[] {2, 101, 108, 178}, null, new byte[] {3, 105, 110, 32, 105, 2, 101, 110, 51}, null, new byte[] {2, 111, 32, 96, 1, 115, 10}, null,
            new byte[] {2, 101, 114, 27}, new byte[] {3, 105, 115, 32, 116, 2, 101, 115, 54}, null, new byte[] {2, 103, 101, 249}, new byte[] {4, 46, 99, 111, 109, 253},
            new byte[] {2, 102, 111, 220, 3, 111, 117, 114, 216}, new byte[] {3, 99, 104, 32, 193, 1, 116, 3}, new byte[] {2, 104, 97, 98}, null,
            new byte[] {3, 109, 101, 110, 252}, null, new byte[] {2, 104, 101, 16}, null, null, new byte[] {1, 117, 38}, new byte[] {2, 104, 105, 102}, null,
            new byte[] {3, 110, 111, 116, 132, 2, 105, 99, 131}, new byte[] {3, 101, 100, 32, 64, 2, 105, 100, 237}, null, null, new byte[] {2, 104, 111, 187},
            new byte[] {2, 114, 32, 75, 1, 118, 109}, null, null, null, new byte[] {3, 116, 32, 116, 175, 2, 105, 108, 240}, new byte[] {2, 105, 109, 226},
            new byte[] {3, 101, 110, 32, 207, 2, 105, 110, 15}, new byte[] {2, 105, 111, 144}, new byte[] {2, 115, 32, 23, 1, 119, 65}, null,
            new byte[] {3, 101, 114, 32, 124}, new byte[] {3, 101, 115, 32, 126, 2, 105, 115, 37}, new byte[] {2, 105, 116, 47}, null, new byte[] {2, 105, 118, 186},
            null, new byte[] {2, 116, 32, 35, 7, 104, 116, 116, 112, 58, 47, 47, 67, 1, 120, 250}, new byte[] {2, 108, 97, 137}, new byte[] {1, 60, 225},
            new byte[] {3, 44, 32, 97, 148}
        };

        // Smaz_rcb (Reverse Compression Codebook)
        private static readonly byte[][] decoderLookup =
        {
            new byte[] {32}, new byte[] {116, 104, 101}, new byte[] {101}, new byte[] {116}, new byte[] {97}, new byte[] {111, 102}, new byte[] {111},
            new byte[] {97, 110, 100}, new byte[] {105}, new byte[] {110}, new byte[] {115}, new byte[] {101, 32}, new byte[] {114},
            new byte[] {32, 116, 104}, new byte[] {32, 116}, new byte[] {105, 110}, new byte[] {104, 101}, new byte[] {116, 104}, new byte[] {104},
            new byte[] {104, 101, 32}, new byte[] {116, 111}, new byte[] {13, 10}, new byte[] {108}, new byte[] {115, 32}, new byte[] {100},
            new byte[] {32, 97}, new byte[] {97, 110}, new byte[] {101, 114}, new byte[] {99}, new byte[] {32, 111}, new byte[] {100, 32},
            new byte[] {111, 110}, new byte[] {32, 111, 102}, new byte[] {114, 101}, new byte[] {111, 102, 32}, new byte[] {116, 32},
            new byte[] {44, 32}, new byte[] {105, 115}, new byte[] {117}, new byte[] {97, 116}, new byte[] {32, 32, 32}, new byte[] {110, 32},
            new byte[] {111, 114}, new byte[] {119, 104, 105, 99, 104}, new byte[] {102}, new byte[] {109}, new byte[] {97, 115},
            new byte[] {105, 116}, new byte[] {116, 104, 97, 116}, new byte[] {10}, new byte[] {119, 97, 115}, new byte[] {101, 110},
            new byte[] {32, 32}, new byte[] {32, 119}, new byte[] {101, 115}, new byte[] {32, 97, 110}, new byte[] {32, 105}, new byte[] {13},
            new byte[] {102, 32}, new byte[] {103}, new byte[] {112}, new byte[] {110, 100}, new byte[] {32, 115}, new byte[] {110, 100, 32},
            new byte[] {101, 100, 32}, new byte[] {119}, new byte[] {101, 100}, new byte[] {104, 116, 116, 112, 58, 47, 47}, new byte[] {102, 111, 114},
            new byte[] {116, 101}, new byte[] {105, 110, 103}, new byte[] {121, 32}, new byte[] {84, 104, 101}, new byte[] {32, 99},
            new byte[] {116, 105}, new byte[] {114, 32}, new byte[] {104, 105, 115}, new byte[] {115, 116}, new byte[] {32, 105, 110},
            new byte[] {97, 114}, new byte[] {110, 116}, new byte[] {44}, new byte[] {32, 116, 111}, new byte[] {121}, new byte[] {110, 103},
            new byte[] {32, 104}, new byte[] {119, 105, 116, 104}, new byte[] {108, 101}, new byte[] {97, 108}, new byte[] {116, 111, 32},
            new byte[] {98}, new byte[] {111, 117}, new byte[] {98, 101}, new byte[] {119, 101, 114, 101}, new byte[] {32, 98},
            new byte[] {115, 101}, new byte[] {111, 32}, new byte[] {101, 110, 116}, new byte[] {104, 97}, new byte[] {110, 103, 32},
            new byte[] {116, 104, 101, 105, 114}, new byte[] {34}, new byte[] {104, 105}, new byte[] {102, 114, 111, 109}, new byte[] {32, 102},
            new byte[] {105, 110, 32}, new byte[] {100, 101}, new byte[] {105, 111, 110}, new byte[] {109, 101}, new byte[] {118}, new byte[] {46},
            new byte[] {118, 101}, new byte[] {97, 108, 108}, new byte[] {114, 101, 32}, new byte[] {114, 105}, new byte[] {114, 111},
            new byte[] {105, 115, 32}, new byte[] {99, 111}, new byte[] {102, 32, 116}, new byte[] {97, 114, 101}, new byte[] {101, 97},
            new byte[] {46, 32}, new byte[] {104, 101, 114}, new byte[] {32, 109}, new byte[] {101, 114, 32}, new byte[] {32, 112},
            new byte[] {101, 115, 32}, new byte[] {98, 121}, new byte[] {116, 104, 101, 121}, new byte[] {100, 105}, new byte[] {114, 97},
            new byte[] {105, 99}, new byte[] {110, 111, 116}, new byte[] {115, 44, 32}, new byte[] {100, 32, 116}, new byte[] {97, 116, 32},
            new byte[] {99, 101}, new byte[] {108, 97}, new byte[] {104, 32}, new byte[] {110, 101}, new byte[] {97, 115, 32}, new byte[] {116, 105, 111},
            new byte[] {111, 110, 32}, new byte[] {110, 32, 116}, new byte[] {105, 111}, new byte[] {119, 101}, new byte[] {32, 97, 32},
            new byte[] {111, 109}, new byte[] {44, 32, 97}, new byte[] {115, 32, 111}, new byte[] {117, 114}, new byte[] {108, 105},
            new byte[] {108, 108}, new byte[] {99, 104}, new byte[] {104, 97, 100}, new byte[] {116, 104, 105, 115}, new byte[] {101, 32, 116},
            new byte[] {103, 32}, new byte[] {101, 13, 10}, new byte[] {32, 119, 104}, new byte[] {101, 114, 101}, new byte[] {32, 99, 111},
            new byte[] {101, 32, 111}, new byte[] {97, 32}, new byte[] {117, 115}, new byte[] {32, 100}, new byte[] {115, 115}, new byte[] {10, 13, 10},
            new byte[] {13, 10, 13}, new byte[] {61, 34}, new byte[] {32, 98, 101}, new byte[] {32, 101}, new byte[] {115, 32, 97}, new byte[] {109, 97},
            new byte[] {111, 110, 101}, new byte[] {116, 32, 116}, new byte[] {111, 114, 32}, new byte[] {98, 117, 116}, new byte[] {101, 108},
            new byte[] {115, 111}, new byte[] {108, 32}, new byte[] {101, 32, 115}, new byte[] {115, 44}, new byte[] {110, 111}, new byte[] {116, 101, 114},
            new byte[] {32, 119, 97}, new byte[] {105, 118}, new byte[] {104, 111}, new byte[] {101, 32, 97}, new byte[] {32, 114}, new byte[] {104, 97, 116},
            new byte[] {115, 32, 116}, new byte[] {110, 115}, new byte[] {99, 104, 32}, new byte[] {119, 104}, new byte[] {116, 114}, new byte[] {117, 116},
            new byte[] {47}, new byte[] {104, 97, 118, 101}, new byte[] {108, 121, 32}, new byte[] {116, 97}, new byte[] {32, 104, 97}, new byte[] {32, 111, 110},
            new byte[] {116, 104, 97}, new byte[] {45}, new byte[] {32, 108}, new byte[] {97, 116, 105}, new byte[] {101, 110, 32}, new byte[] {112, 101},
            new byte[] {32, 114, 101}, new byte[] {116, 104, 101, 114, 101}, new byte[] {97, 115, 115}, new byte[] {115, 105}, new byte[] {32, 102, 111},
            new byte[] {119, 97}, new byte[] {101, 99}, new byte[] {111, 117, 114}, new byte[] {119, 104, 111}, new byte[] {105, 116, 115}, new byte[] {122},
            new byte[] {102, 111}, new byte[] {114, 115}, new byte[] {62}, new byte[] {111, 116}, new byte[] {117, 110}, new byte[] {60},
            new byte[] {105, 109}, new byte[] {116, 104, 32}, new byte[] {110, 99}, new byte[] {97, 116, 101}, new byte[] {62, 60},
            new byte[] {118, 101, 114}, new byte[] {97, 100}, new byte[] {32, 119, 101}, new byte[] {108, 121}, new byte[] {101, 101}, new byte[] {32, 110},
            new byte[] {105, 100}, new byte[] {32, 99, 108}, new byte[] {97, 99}, new byte[] {105, 108}, new byte[] {60, 47}, new byte[] {114, 116},
            new byte[] {32, 119, 105}, new byte[] {100, 105, 118}, new byte[] {101, 44, 32}, new byte[] {32, 105, 116}, new byte[] {119, 104, 105},
            new byte[] {32, 109, 97}, new byte[] {103, 101}, new byte[] {120}, new byte[] {101, 32, 99}, new byte[] {109, 101, 110}, new byte[] {46, 99, 111, 109}
        };
        #endregion

        /// <summary>
        /// Compresses a byte array using Smaz encoding (identical to original C implementation)
        /// </summary>
        public static byte[] Compress(byte[] input)
        {
            // Initial buffer size estimate (safe upper bound)
            byte[] output = new byte[input.Length * 2 + 10];
            int outPos = 0;
            int inPos = 0;
            int inLen = input.Length;

            // Verbatim buffer logic
            int verbStart = 0;
            int verbLen = 0;

            while (inLen > 0)
            {
                int j = 7;
                if (j > inLen) j = inLen;

                bool matchFound = false;

                // Hash calculation (smaz_compress.c)
                // h1 = h2 = in[0]<<3;
                uint h1 = (uint)(input[inPos] << 3);
                uint h2 = h1;
                uint h3 = 0;

                if (inLen > 1) h2 += input[inPos + 1];
                if (inLen > 2) h3 = h2 ^ input[inPos + 2];

                // Search for substrings
                for (; j > 0; j--)
                {
                    byte[] slot;
                    switch (j)
                    {
                        case 1: slot = encoderLookup[h1 % 241]; break;
                        case 2: slot = encoderLookup[h2 % 241]; break;
                        default: slot = encoderLookup[h3 % 241]; break;
                    }

                    if (slot == null) continue;

                    // Iterate through slot items
                    // Структура слота: [Len, Byte1, ..., Code, Len, ...]
                    int slotIdx = 0;
                    while (slotIdx < slot.Length)
                    {
                        int entryLen = slot[slotIdx];
                        // if (slot[0] == j && memcmp(slot+1,in,j) == 0)
                        if (entryLen == j)
                        {
                            // Compare bytes
                            bool equal = true;
                            for (int k = 0; k < j; k++)
                            {
                                if (slot[slotIdx + 1 + k] != input[inPos + k])
                                {
                                    equal = false;
                                    break;
                                }
                            }

                            if (equal)
                            {
                                // Match found!
                                // 1. Flush verbatim if needed
                                if (verbLen > 0)
                                {
                                    FlushVerbatim(ref output, ref outPos, input, verbStart, verbLen);
                                    verbLen = 0;
                                }

                                // 2. Emit the byte code
                                // C: out[0] = slot[slot[0]+1];
                                if (outPos >= output.Length) Array.Resize(ref output, output.Length * 2);
                                output[outPos++] = slot[slotIdx + 1 + j]; // Code is after the bytes

                                inLen -= j;
                                inPos += j;
                                matchFound = true;
                                verbStart = inPos; // Reset verbStart
                                goto OutOfLoops;
                            }
                        }

                        // Move to next entry in this slot: Len byte + Data bytes + Code byte
                        slotIdx += 1 + entryLen + 1;
                    }
                }

            OutOfLoops:
                if (!matchFound)
                {
                    // Add byte to verbatim
                    if (verbLen == 0) verbStart = inPos;
                    verbLen++;
                    inLen--;
                    inPos++;
                }

                // Check flush limit
                // C: if (!flush && (verblen == 256 || (verblen > 0 && inlen == 0)))
                if (verbLen == 256 || (verbLen > 0 && inLen == 0))
                {
                    FlushVerbatim(ref output, ref outPos, input, verbStart, verbLen);
                    verbLen = 0;
                    verbStart = inPos;
                }
            }

            // Shrink to fit
            byte[] result = new byte[outPos];
            Array.Copy(output, result, outPos);
            return result;
        }

        private static void FlushVerbatim(ref byte[] output, ref int outPos, byte[] input, int start, int len)
        {
            // Resize if needed
            int needed = (len == 1) ? 2 : 2 + len;
            if (outPos + needed > output.Length)
            {
                Array.Resize(ref output, Math.Max(output.Length * 2, outPos + needed));
            }

            if (len == 1)
            {
                output[outPos++] = 254;
                output[outPos++] = input[start];
            }
            else
            {
                output[outPos++] = 255;
                output[outPos++] = (byte)(len - 1);
                Array.Copy(input, start, output, outPos, len);
                outPos += len;
            }
        }

        /// <summary>
        /// Decompresses a Smaz encoded buffer slice
        /// </summary>
        public static string Decompress(byte[] data, int offset, int length)
        {
            if (length == 0) return string.Empty;

            using (MemoryStream output = new MemoryStream(length * 2))
            {
                int end = offset + length;
                for (int i = offset; i < end;)
                {
                    byte b = data[i];
                    if (b == 254)
                    {
                        // Verbatim Byte
                        if (i + 1 < end)
                        {
                            output.WriteByte(data[i + 1]);
                            i += 2;
                        }
                        else break;
                    }
                    else if (b == 255)
                    {
                        // Verbatim Bytes
                        if (i + 1 < end)
                        {
                            int len = data[i + 1] + 1;
                            if (i + 2 + len <= end + 2)
                            {
                                output.Write(data, i + 2, len);
                                i += 2 + len;
                            }
                            else break;
                        }
                        else break;
                    }
                    else
                    {
                        // Codebook entry
                        if (b < decoderLookup.Length)
                        {
                            var w = decoderLookup[b];
                            if (w != null)
                            {
                                output.Write(w, 0, w.Length);
                            }
                        }
                        i++;
                    }
                }
                return Encoding.UTF8.GetString(output.ToArray());
            }
        }

        public static string Decompress(byte[] input)
        {
            return Decompress(input, 0, input.Length);
        }
    }
}