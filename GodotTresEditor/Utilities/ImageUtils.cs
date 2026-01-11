using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GodotTresEditor.Utilities
{
    public static class ImageUtils
    {

        public static (int w, int h) GetWebpDimensions(byte[] data)
        {
            if (data.Length < 30) return (0, 0);

            if (data[0] != 'R' || data[1] != 'I' || data[2] != 'F' || data[3] != 'F' ||
                data[8] != 'W' || data[9] != 'E' || data[10] != 'B' || data[11] != 'P')
            {
                return (0, 0);
            }

            string type = Encoding.ASCII.GetString(data, 12, 4);

            if (type == "VP8X") // Extended Format (Ваш випадок)
            {
                int w = (data[24] | (data[25] << 8) | (data[26] << 16)) + 1;
                int h = (data[27] | (data[28] << 8) | (data[29] << 16)) + 1;
                return (w, h);
            }
            else if (type == "VP8L") // Lossless
            {
                uint bits = BitConverter.ToUInt32(data, 21);
                int w = (int)(bits & 0x3FFF) + 1;
                int h = (int)((bits >> 14) & 0x3FFF) + 1;
                return (w, h);
            }
            else if (type == "VP8 ") // Lossy
            {
                int w = (data[26] | (data[27] << 8)) & 0x3FFF;
                int h = (data[28] | (data[29] << 8)) & 0x3FFF;
                return (w, h);
            }

            return (0, 0);
        }
    }
}
