using GodotTresEditor.Core.Models;
using GodotTresEditor.Utilities.Extensions;
using System.Text;

namespace GodotTresEditor.Core;

public static class TresUpdater
{
    public static GeneratedTranslationData GenEditedStrings(TresData data, List<string> editedStrings)
    {
        int[] hashTable = data.GetProperty<int[]>("hash_table");
        int[] bucketTable = data.GetProperty<int[]>("bucket_table");

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
        UpdateByteArrayInFile(filePath, "bucket_table", writer => WriteIntArray(writer, newData.BucketTable));
        UpdateByteArrayInFile(filePath, "strings", writer => WriteByteArray(writer, newData.Strings, format));
    }

    public static void UpdateFontFile(string filePath, byte[] newFontData, int format)
    {
        UpdateByteArrayInFile(filePath, "data", writer => WriteByteArray(writer, newFontData, format));
    }

    public static bool UpdateTresProperty(string tresPath, string propertyName, string escapedValue)
    {
        if (!File.Exists(tresPath))
            return false;

        string[] lines = File.ReadAllLines(tresPath);
        var outputLines = new List<string>();
        bool propertyUpdated = false;
        int i = 0;

        while (i < lines.Length)
        {
            string line = lines[i];
            string trimmedLine = line.Trim();

            if (!propertyUpdated && trimmedLine.StartsWith(propertyName) && trimmedLine.Contains("="))
            {
                int equalIndex = line.IndexOf('=');
                string keyPart = line.Substring(0, equalIndex + 1);

                outputLines.Add($"{keyPart} \"{escapedValue}\"");
                propertyUpdated = true;

                string rawValue = line.Substring(equalIndex + 1).Trim();
                if (rawValue.StartsWith("\"") && !StringExtentions.IsStringClosed(rawValue))
                {
                    i++;
                    while (i < lines.Length)
                    {
                        if (StringExtentions.IsStringClosed(lines[i]))
                        {
                            break;
                        }
                        i++;
                    }
                }
            }
            else
            {
                outputLines.Add(line);
            }
            i++;
        }

        if (propertyUpdated)
        {
            File.WriteAllLines(tresPath, outputLines, new UTF8Encoding(false));
        }

        return propertyUpdated;
    }

    private static void UpdateByteArrayInFile(string filePath, string targetProperty, Action<StreamWriter> writeValueAction)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        string tempPath = filePath + ".tmp";

        using (var reader = new StreamReader(filePath, Encoding.ASCII))
        using (var writer = new StreamWriter(tempPath, false, Encoding.ASCII))
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                string trimmedLine = line.TrimStart();
                if (trimmedLine.StartsWith($"{targetProperty} ="))
                {
                    writer.Write($"{targetProperty} = ");
                    writeValueAction(writer);
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