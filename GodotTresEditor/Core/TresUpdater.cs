using GodotTresEditor.Core.Models;
using System.Text;

namespace GodotTresEditor.Core
{
    public static class TresUpdater
    {
        public static GeneratedTranslationData GenEditedStrings(TresData data, List<string> editedStrings)
        {
            int[] hashTable = data.GetProperty<int[]>("hash_table");
            int[] bucketTable = data.GetProperty<int[]>("bucket_table");
            // byte[] oldStrings = data.GetProperty<byte[]>("strings");

            var newBucket = new int[bucketTable.Length];
            var newStringsList = new List<byte>();
            int editedIndex = 0;

            int iBT = 0;
            while (iBT < bucketTable.Length)
            {
                int size = bucketTable[iBT];
                int func = bucketTable[iBT + 1];

                newBucket[iBT] = size;
                newBucket[iBT + 1] = func;

                int elemBase = iBT + 2;
                for (int j = 0; j < size; j++)
                {
                    int p = elemBase + j * 4;
                    int keyHash = bucketTable[p];

                    string txt = editedStrings[editedIndex++];

                    byte[] utf8 = Encoding.UTF8.GetBytes(txt + "\0");
                    var cs = CompressString(utf8, newStringsList.Count);

                    newBucket[p] = keyHash;
                    newBucket[p + 1] = cs.Offset;
                    newBucket[p + 2] = cs.CompSize;
                    newBucket[p + 3] = cs.UncompSize;

                    newStringsList.AddRange(cs.Data);
                }

                iBT = elemBase + size * 4;
            }

            return new GeneratedTranslationData
            {
                HashTable = hashTable,
                BucketTable = newBucket,
                Strings = newStringsList.ToArray()
            };

        }

        private static (int Offset, int CompSize, int UncompSize, byte[] Data) CompressString(byte[] src, int currentOffset)
        {
            if (src.Length == 0)
            {
                return (currentOffset, 1, 1, new byte[] { 0 });
            }

            byte[] compressed = Smaz.Compress(src);
            bool useCompressed = compressed.Length < src.Length;
            byte[] finalBytes = useCompressed ? compressed : src;

            return (currentOffset, finalBytes.Length, src.Length, finalBytes);
        }

        public static void UpdateTranslationFile(string filePath, GeneratedTranslationData newData, int format)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            string tempPath = filePath + ".tmp";


            using (var reader = new StreamReader(filePath, Encoding.ASCII))
            using (var writer = new StreamWriter(tempPath, false, Encoding.ASCII))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string trimmedLine = line.TrimStart();

                    /*if (trimmedLine.StartsWith("hash_table ="))
                    {
                        writer.Write("hash_table = ");
                        WriteIntArray(writer, newData.HashTable);
                        writer.WriteLine();
                    }*/
                    if (trimmedLine.StartsWith("bucket_table ="))
                    {
                        writer.Write("bucket_table = ");
                        WriteIntArray(writer, newData.BucketTable);
                        writer.WriteLine();
                    }
                    else if (trimmedLine.StartsWith("strings ="))
                    {
                        writer.Write("strings = ");
                        WriteByteArray(writer, newData.Strings, format);
                        writer.WriteLine();
                    }
                    else
                    {
                        writer.WriteLine(line);
                    }
                }
            }

            File.Delete(filePath);
            File.Move(tempPath, filePath);
        }

        public static void UpdateFontFile(string filePath, byte[] newFontData, int format)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }
            string tempPath = filePath + ".tmp";

            using (var reader = new StreamReader(filePath, Encoding.ASCII))
            using (var writer = new StreamWriter(tempPath, false, Encoding.ASCII))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string trimmedLine = line.TrimStart();
                    if (trimmedLine.StartsWith("data ="))
                    {
                        writer.Write("data = ");
                        WriteByteArray(writer, newFontData, format);
                        writer.WriteLine();
                    }
                    else
                    {
                        writer.WriteLine(line);
                    }
                }
            }
            File.Delete(filePath);
            File.Move(tempPath, filePath);
        }

        private static void WriteIntArray(StreamWriter writer, int[] data)
        {
            writer.Write("PackedInt32Array(");
            for (int i = 0; i < data.Length; i++)
            {
                writer.Write(data[i]);
                if (i < data.Length - 1)
                {
                    writer.Write(", ");
                }
            }
            writer.Write(")");
        }

        private static void WriteByteArray(StreamWriter writer, byte[] data, int format)
        {
            writer.Write("PackedByteArray(");

            if (format == 3)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    writer.Write(data[i]);
                    if (i < data.Length - 1)
                    {
                        writer.Write(", ");
                    }
                }
            }
            else if (format == 4)
            {
                string base64 = Convert.ToBase64String(data);
                writer.Write($"\"{base64}\"");
            }
            else
            {
                throw new ArgumentException("Invalid format specified for byte array writing.");
            }
            writer.Write(")");
        }
    }
}

