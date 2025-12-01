using GodotTresEditor.Core.Models;
using System.Text;

namespace GodotTresEditor.Core
{
    public static class OptimizedTranslationParser
    {
        public static List<string> GetTranslatedMessages(TresData data)
        {
            var keys = new List<string>();

            if (data.BaseType != "OptimizedTranslation")
                return keys;

            int[] hashTable = data.GetProperty<int[]>("hash_table");
            int[] bucketTable = data.GetProperty<int[]>("bucket_table");
            byte[] strings = data.GetProperty<byte[]>("strings");

            if (hashTable == null || bucketTable == null || strings == null)
                return keys;

            for (int i = 0; i < hashTable.Length; i++)
            {
                int bucketIndex = hashTable[i];
                if (bucketIndex != -1)
                {
                    if (bucketIndex >= bucketTable.Length)
                        continue;

                    int bucketSize = bucketTable[bucketIndex];
                    int elementStartIndex = bucketIndex + 2;

                    for (int j = 0; j < bucketSize; j++)
                    {
                        int p = elementStartIndex + (j * 4);

                        if (p + 3 >= bucketTable.Length)
                            break;

                        int strOffset = bucketTable[p + 1];
                        int compSize = bucketTable[p + 2];
                        int uncompSize = bucketTable[p + 3];

                        if (strOffset + compSize > strings.Length)
                            continue;

                        string resultString;

                        if (compSize == uncompSize)
                        {
                            resultString = Encoding.UTF8.GetString(strings, strOffset, uncompSize);
                        }
                        else
                        {
                            resultString = Smaz.Decompress(strings, strOffset, compSize);
                        }

                        if (resultString.Length > 0 && resultString[resultString.Length - 1] == '\0')
                        {
                            resultString = resultString.Substring(0, resultString.Length - 1);
                        }

                        keys.Add(resultString);
                    }
                }
            }

            return keys;
        }
    }
}