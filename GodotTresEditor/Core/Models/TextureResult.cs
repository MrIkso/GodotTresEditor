using static GodotTresEditor.Core.TextureParser;

namespace GodotTresEditor.Core.Models
{
    public class TextureResult
    {
        public byte[] Data { get; set; }
        public string Extension { get; set; } // "png", "webp", "basis" or "bin"
        public int Width { get; set; }
        public int Height { get; set; }
        public GodotImageFormat FormatName { get; set; }
        public GodotVersion GodotVersion { get; set; }

    }
}
