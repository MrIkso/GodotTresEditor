using GodotTresEditor.Core.Models;

namespace GodotTresEditor.Core
{
    public class TextureParser
    {
        private const uint GST2_MAGIC = 0x32545347; // 'GST2'
        private const uint GDST_MAGIC = 0x54534447; // 'GDST'

        // Godot 4 Enums
        public enum DataFormat : uint
        {
            Image = 0,
            Png = 1,
            Webp = 2,
            BasisUniversal = 3
        }

        public enum GodotVersion
        {
            V4,
            V3,
        }
        public enum CtexFlags : uint
        {
            Stream = 1 << 22,
            HasMipmaps = 1 << 23,
            Detect3D = 1 << 24,
            DetectRoughness = 1 << 25,
            DetectNormal = 1 << 26
        }

        public enum GodotImageFormat : int
        {
            FORMAT_L8, // Luminance
            FORMAT_LA8, // Luminance-Alpha
            FORMAT_R8,
            FORMAT_RG8,
            FORMAT_RGB8,
            FORMAT_RGBA8,
            FORMAT_RGBA4444,
            FORMAT_RGB565,
            FORMAT_RF, // Float
            FORMAT_RGF,
            FORMAT_RGBF,
            FORMAT_RGBAF,
            FORMAT_RH, // Half
            FORMAT_RGH,
            FORMAT_RGBH,
            FORMAT_RGBAH,
            FORMAT_RGBE9995,
            FORMAT_DXT1, // BC1
            FORMAT_DXT3, // BC2
            FORMAT_DXT5, // BC3
            FORMAT_RGTC_R, // BC4
            FORMAT_RGTC_RG, // BC5
            FORMAT_BPTC_RGBA, // BC7
            FORMAT_BPTC_RGBF, // BC6 Signed
            FORMAT_BPTC_RGBFU, // BC6 Unsigned
            FORMAT_ETC, // ETC1
            FORMAT_ETC2_R11,
            FORMAT_ETC2_R11S, // Signed, NOT srgb.
            FORMAT_ETC2_RG11,
            FORMAT_ETC2_RG11S, // Signed, NOT srgb.
            FORMAT_ETC2_RGB8,
            FORMAT_ETC2_RGBA8,
            FORMAT_ETC2_RGB8A1,
            FORMAT_ETC2_RA_AS_RG, // ETC2 RGBA with a RA-RG swizzle for normal maps.
            FORMAT_DXT5_RA_AS_RG, // BC3 with a RA-RG swizzle for normal maps.
            FORMAT_ASTC_4x4,
            FORMAT_ASTC_4x4_HDR,
            FORMAT_ASTC_8x8,
            FORMAT_ASTC_8x8_HDR,
            FORMAT_R16,
            FORMAT_RG16,
            FORMAT_RGB16,
            FORMAT_RGBA16,
            FORMAT_R16I,
            FORMAT_RG16I,
            FORMAT_RGB16I,
            FORMAT_RGBA16I,
            FORMAT_MAX
        }

        private const uint FORMAT_VERSION = 1;

        public TextureResult DecompressTexture(byte[] data)
        {
            using var memoryStream = new MemoryStream(data);
            using var binaryReader = new BinaryReader(memoryStream);

            uint magic = binaryReader.ReadUInt32();

            if (magic == GST2_MAGIC)
            {
                // Godot 4 CompressedTexture2D
                return ExtractCtexImageV4(binaryReader);
            }
            else if (magic == GDST_MAGIC)
            {
                // Godot 3 STEX
                return ExtractStexImageV3(binaryReader);
            }
            else
            {
                throw new Exception("Unknown texture format");
            }
        }

        private TextureResult ExtractCtexImageV4(BinaryReader reader)
        {
            // Format: GST2 + version + width + height + flags + mipmap_limit + reserved (3) + data_format + ...

            uint version = reader.ReadUInt32();
            if (version != FORMAT_VERSION)
            {
                throw new Exception($"Unsupported Godot 4 CompressedTexture2D version: {version}");
            }

            // global parameters
            uint width = reader.ReadUInt32();
            uint height = reader.ReadUInt32();
            uint flags = reader.ReadUInt32();
            uint mipmapLimit = reader.ReadUInt32();

            // reserved (12 bytes)
            reader.BaseStream.Seek(12, SeekOrigin.Current);

            // data format
            uint dataFormatRaw = reader.ReadUInt32();
            DataFormat dataFormat = (DataFormat)dataFormatRaw;

            // image parameters
            ushort imgWidth = reader.ReadUInt16();
            ushort imgHeight = reader.ReadUInt16();
            uint mipmapCount = reader.ReadUInt32();
            uint godotFormat = reader.ReadUInt32();

            string extension = "bin";
            byte[] imageData;

            switch (dataFormat)
            {
                case DataFormat.Png:
                    extension = "png";
                    uint pngSize = reader.ReadUInt32();
                    imageData = reader.ReadBytes((int)pngSize);
                    break;

                case DataFormat.Webp:
                    extension = "webp";
                    uint webpSize = reader.ReadUInt32();
                    imageData = reader.ReadBytes((int)webpSize);
                    break;

                case DataFormat.BasisUniversal:
                    extension = "basis";
                    uint basisSize = reader.ReadUInt32();
                    imageData = reader.ReadBytes((int)basisSize);
                    break;

                case DataFormat.Image:
                    extension = GetExtensionForFormat((GodotImageFormat)godotFormat);
                    var remaining = reader.BaseStream.Length - reader.BaseStream.Position;
                    imageData = reader.ReadBytes((int)remaining);
                    break;

                default:
                    throw new Exception($"Unknown DataFormat: {dataFormatRaw}");
            }

            return new TextureResult
            {
                Data = imageData,
                Extension = extension,
                FormatName = (GodotImageFormat)godotFormat,
                GodotVersion = GodotVersion.V4,
                Width = (int)width,
                Height = (int)height
            };
        }

        private TextureResult ExtractStexImageV3(BinaryReader reader)
        {
            // Godot 3 STEX format: GDST magic + header
            ushort width = reader.ReadUInt16();
            ushort widthPo2 = reader.ReadUInt16();
            ushort height = reader.ReadUInt16();
            ushort heightPo2 = reader.ReadUInt16();
            uint flags = reader.ReadUInt32();
            uint godotFormat = reader.ReadUInt32();

            // Format bits for Godot 3.6
            const uint FORMAT_BIT_PNG = 1 << 20;
            const uint FORMAT_BIT_WEBP = 1 << 21;
            const uint FORMAT_BIT_LOSSLESS = 1 << 20;  // Godot 3.2
            const uint FORMAT_BIT_LOSSY = 1 << 21;      // Godot 3.2

            bool isPNG = (godotFormat & FORMAT_BIT_PNG) != 0;
            bool isWebP = (godotFormat & FORMAT_BIT_WEBP) != 0;
            bool isLossless = (godotFormat & FORMAT_BIT_LOSSLESS) != 0;
            bool isLossy = (godotFormat & FORMAT_BIT_LOSSY) != 0;
            string extension = "bin";
            if (isPNG)
                extension = "png";
            else if (isWebP)
                extension = "webp";

            byte[] imageData;
            if (isPNG || isWebP || isLossless || isLossy)
            {
                // Compressed format
                uint mipmapCount = reader.ReadUInt32();

                // Read first mipmap (the full image)
                uint dataSize = reader.ReadUInt32();
                reader.ReadBytes(4); // format name
                imageData = reader.ReadBytes((int)dataSize - 4);
            }
            else
            {
                // VRAM compressed or uncompressed - return as-is
                var remaining = reader.BaseStream.Length - reader.BaseStream.Position;
                imageData = reader.ReadBytes((int)remaining);
            }
            return new TextureResult
            {
                Data = imageData,
                Extension = extension,
                FormatName = (GodotImageFormat)godotFormat,
                GodotVersion = GodotVersion.V3,
                Width = width,
                Height = height
            };
        }

        private string GetExtensionForFormat(GodotImageFormat format)
        {
            switch (format)
            {
                case GodotImageFormat.FORMAT_L8:
                case GodotImageFormat.FORMAT_LA8:
                case GodotImageFormat.FORMAT_R8:
                case GodotImageFormat.FORMAT_RG8:
                case GodotImageFormat.FORMAT_RGB8:
                case GodotImageFormat.FORMAT_RGBA8:
                case GodotImageFormat.FORMAT_RGBA4444:
                case GodotImageFormat.FORMAT_RGB565:
                case GodotImageFormat.FORMAT_DXT1:
                case GodotImageFormat.FORMAT_DXT3:
                case GodotImageFormat.FORMAT_DXT5:
                    case GodotImageFormat.FORMAT_DXT5_RA_AS_RG:
                    return "dds"; // DXT formats
                default:
                    return "bin"; // Other formats
            }

        }

        public byte[] CreateCtexV4(byte[] fileBytes, int width, int height, bool isWebp, bool streamable = true, bool hasMipmaps = false, bool detect3d = false)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            // header
            writer.Write(GST2_MAGIC);
            writer.Write((uint)1); // Version
            writer.Write((uint)width);
            writer.Write((uint)height);

            // Flags 
            uint flags = 0;
            if (streamable) flags |= (uint)CtexFlags.Stream;
            if (hasMipmaps) flags |= (uint)CtexFlags.HasMipmaps;
            if (detect3d) flags |= (uint)CtexFlags.Detect3D;

            writer.Write(flags);
            writer.Write((uint)0); // Mipmap limit

            // Reserved (3 * uint32)
            writer.Write((uint)0);
            writer.Write((uint)0);
            writer.Write((uint)0);

            writer.Write(isWebp ? (uint)DataFormat.Webp : (uint)DataFormat.Png);

            writer.Write((ushort)width);
            writer.Write((ushort)height);
            writer.Write((uint)0);

            // format 
            uint format = (uint)GodotImageFormat.FORMAT_RGBA8; // RGBA8
            writer.Write(format);

            writer.Write((uint)fileBytes.Length);
            writer.Write(fileBytes);

            return ms.ToArray();
        }

        public byte[] CreateStexV3(byte[] fileBytes, int width, int height, bool isWebp, bool streamable = true, bool hasMipmaps = false, bool detect3d = false)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            writer.Write(GDST_MAGIC);
            writer.Write((ushort)width);
            writer.Write((ushort)CreaeteFlagsV3((uint)width));
            writer.Write((ushort)height);
            writer.Write((ushort)CreaeteFlagsV3((uint)height));

            // Flags 
            uint flags = 0;
            if (streamable) flags |= (uint)CtexFlags.Stream;
            if (hasMipmaps) flags |= (uint)CtexFlags.HasMipmaps;
            if (detect3d) flags |= (uint)CtexFlags.Detect3D;

            writer.Write(flags); // Flags

            uint format = (uint)GodotImageFormat.FORMAT_RGBA8; // RGBA8
            if (isWebp) format |= (1 << 21);
            else format |= (1 << 20);

            writer.Write(format);

            writer.Write((uint)1); // Mipmaps count
            writer.Write((uint)fileBytes.Length);
            writer.Write(fileBytes);

            return ms.ToArray();
        }

        private uint CreaeteFlagsV3(uint v)
        {
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;
            return v;
        }

    }
}
