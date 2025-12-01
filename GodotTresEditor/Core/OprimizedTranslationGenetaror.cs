using GodotTresEditor.Core.Models;
using System.Text;

namespace GodotTresEditor.Core
{
    public static class OptimizedTranslationGenerator
    {
        private class CompressedString
        {
            public int Offset;
            public int CompSize;
            public int UncompSize;
            public byte[] Data;
        }

        private class BucketEntry
        {
            public int Index;        // idx
            public byte[] KeyBytes;  // UTF-8
        }

        public static GeneratedTranslationData Generate(IDictionary<string, string> map)
        {

            var keys = map.Keys.Distinct().ToList();
            int count = keys.Count;
            int size = GetGodotPrime(count);

            var buckets = new List<BucketEntry>[size];
            for (int i = 0; i < size; i++)
                buckets[i] = new List<BucketEntry>();

            var compressed = new CompressedString[count];

            int totalSize = 0;
            int idx = 0;

            foreach (var key in keys)
            {
                string value = map[key] ?? string.Empty;

                byte[] keyBytes = Encoding.UTF8.GetBytes(key);
                byte[] valueBytes = Encoding.UTF8.GetBytes(value);

                CompressedString cs = CompressValue(valueBytes, totalSize);

                compressed[idx] = cs;
                totalSize += cs.CompSize;

                uint h = GodotHash(0, keyBytes);
                int bucketIndex = (int)(h % (uint)size);
                buckets[bucketIndex].Add(new BucketEntry
                {
                    Index = idx,
                    KeyBytes = keyBytes
                });

                idx++;
            }

            var hashTable = new int[size];
            var bucketTableList = new List<int>();

            for (int i = 0; i < size; i++)
            {
                var bucket = buckets[i];
                if (bucket.Count == 0)
                {
                    hashTable[i] = -1; // 0xFFFFFFFF
                    continue;
                }

                hashTable[i] = bucketTableList.Count;


                int d = 1;
                var used = new HashSet<uint>();

                while (true)
                {
                    used.Clear();
                    bool collision = false;

                    foreach (var entry in bucket)
                    {
                        uint slot = GodotHash((uint)d, entry.KeyBytes);
                        if (!used.Add(slot))
                        {
                            collision = true;
                            break;
                        }
                    }

                    if (!collision)
                        break;

                    d++;
                }

                bucketTableList.Add(bucket.Count); // size
                bucketTableList.Add(d);           // func (seed)
                foreach (var entry in bucket)
                {
                    uint keyHash = GodotHash((uint)d, entry.KeyBytes);
                    var cs = compressed[entry.Index];

                    bucketTableList.Add(unchecked((int)keyHash)); // key
                    bucketTableList.Add(cs.Offset);               // str_offset
                    bucketTableList.Add(cs.CompSize);             // comp_size
                    bucketTableList.Add(cs.UncompSize);           // uncomp_size
                }
            }

            var strings = new byte[totalSize];
            foreach (var cs in compressed)
            {
                Buffer.BlockCopy(cs.Data, 0, strings, cs.Offset, cs.CompSize);
            }

            return new GeneratedTranslationData
            {
                HashTable = hashTable,
                BucketTable = bucketTableList.ToArray(),
                Strings = strings
            };
        }

        private static CompressedString CompressValue(byte[] src, int currentOffset)
        {
            if (src.Length == 0)
            {
                return new CompressedString
                {
                    Offset = currentOffset,
                    CompSize = 1,
                    UncompSize = 1,
                    Data = new byte[] { 0 }
                };
            }

            byte[] outBuf = new byte[src.Length];
            byte[] compressed = Smaz.Compress(src);

            bool useCompressed = compressed.Length < src.Length;
            byte[] finalBytes = useCompressed ? compressed : src;

            return new CompressedString
            {
                Offset = currentOffset,
                CompSize = finalBytes.Length,
                UncompSize = src.Length,
                Data = finalBytes
            };
        }

        private static uint GodotHash(uint d, byte[] str)
        {
            // 0x1000193 = 16777619 (FNV Prime)
            if (d == 0) d = 0x1000193;

            for (int i = 0; i < str.Length; i++)
            {
                //C++: d = (d * 0x1000193) ^ uint32_t(*p_str);
                unchecked
                {
                    d = (d * 0x1000193) ^ (uint)str[i];
                }
            }
            return d;
        }


        private static readonly uint[] GodotPrimes = new uint[]
        {
            5,
            13,
            23,
            47,
            97,
            193,
            389,
            769,
            1543,
            3079,
            6151,
            12289,
            24593,
            49157,
            98317,
            196613,
            393241,
            786433,
            1572869,
            3145739,
            6291469,
            12582917,
            25165843,
            50331653,
            100663319,
            201326611,
            402653189,
            805306457,
            1610612741,
       };

        // Math::larger_prime(uint32_t p_val)
        private static int GetGodotPrime(int p_val)
        {
            for (int i = 0; i < GodotPrimes.Length; i++)
            {
                if (GodotPrimes[i] > p_val)
                {
                    return (int)GodotPrimes[i];
                }
            }

            
            throw new Exception($"Too many strings! Cannot find a larger prime for {p_val}. Max supported is 1610612741.");
        }
    }
}
