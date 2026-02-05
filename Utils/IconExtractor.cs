using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WineBridgePlugin.Utils
{
    internal class IconDirEntry
    {
        public int Index { get; set; }
        public byte Width { get; set; }
        public byte Height { get; set; }
        public byte ColorCount { get; set; }
        public byte Reserved { get; set; }
        public ushort Planes { get; set; }
        public ushort BitCount { get; set; }
        public int BytesInRes { get; set; }
        public int ImageOffset { get; set; }
    }

    internal class IconFrameInfo
    {
        public uint RealWidth { get; set; }
        public uint RealHeight { get; set; }
        public int BitsPerPixel { get; set; }
        public bool IsPng { get; set; }
        public IconDirEntry Entry { get; set; }

        public long PixelCountIncludingColor => RealWidth * RealHeight * BitsPerPixel;
    }

    public static class IconExtractor
    {
        public static Stream StripIconToTheHighestQualityFrame(Stream stream)
        {
            var br = new BinaryReader(stream);
            // ICONDIR
            var reserved = br.ReadUInt16(); // 0
            var type = br.ReadUInt16(); // 1=ICO, 2=CUR
            var count = br.ReadUInt16();

            if (reserved != 0 || (type != 1 && type != 2))
            {
                return stream;
            }

            if (count <= 1)
            {
                return stream;
            }

            var entries = new List<IconDirEntry>();
            for (var i = 0; i < count; i++)
            {
                var w = br.ReadByte();
                var h = br.ReadByte();
                var colorCount = br.ReadByte();
                var res2 = br.ReadByte();
                var planes = br.ReadUInt16();
                var bitCount = br.ReadUInt16();
                var bytesInRes = br.ReadInt32();
                var imageOffset = br.ReadInt32();

                entries.Add(new IconDirEntry
                {
                    Index = i,
                    Width = w,
                    Height = h,
                    ColorCount = colorCount,
                    Reserved = res2,
                    Planes = planes,
                    BitCount = bitCount,
                    BytesInRes = bytesInRes,
                    ImageOffset = imageOffset
                });
            }

            var result = new List<IconFrameInfo>(count);

            foreach (var entry in entries)
            {
                stream.Position = entry.ImageOffset;

                var isPng = IsPng(br);

                stream.Position = entry.ImageOffset;

                result.Add(isPng ? ReadPngFrameInfo(br, entry) : ReadBmpFrameInfo(br, entry));
            }

            var bestFrame = result
                .OrderByDescending(e => e.PixelCountIncludingColor)
                .ThenByDescending(e => e.Entry.BytesInRes)
                .ThenByDescending(e => e.Entry.Index)
                .First();
            var bestFrameEntry = bestFrame.Entry;

            var imageBytes = new byte[bestFrameEntry.BytesInRes];
            stream.Position = bestFrameEntry.ImageOffset;
            ReadExact(stream, imageBytes, 0, imageBytes.Length);

            var ms = new MemoryStream(6 + 16 + imageBytes.Length);
            WriteLittleEndianUInt16(ms, 0); // reserved
            WriteLittleEndianUInt16(ms, 1); // type = icon
            WriteLittleEndianUInt16(ms, 1); // count = 1
            ms.WriteByte(bestFrameEntry.Width);
            ms.WriteByte(bestFrameEntry.Height);
            ms.WriteByte(bestFrameEntry.ColorCount);
            ms.WriteByte(bestFrameEntry.Reserved); // reserved
            WriteLittleEndianUInt16(ms, bestFrameEntry.Planes);
            WriteLittleEndianUInt16(ms, bestFrameEntry.BitCount);
            WriteLittleEndianUInt32(ms, (uint)imageBytes.Length);
            WriteLittleEndianUInt32(ms, 6u + 16u); // image offset = 22
            ms.Write(imageBytes, 0, imageBytes.Length);
            return ms;
        }

        private static IconFrameInfo ReadBmpFrameInfo(BinaryReader br, IconDirEntry entry)
        {
            br.BaseStream.Position = entry.ImageOffset;

            // BITMAPINFOHEADER (at least 40 bytes) starts immediately for DIB icons
            var biSize = br.ReadInt32();
            if (biSize < 40)
            {
                throw new InvalidDataException("Unsupported BITMAP header size in ICO frame.");
            }

            br.ReadInt32();
            br.ReadInt32();
            br.ReadUInt16();
            var biBitCount = br.ReadUInt16();

            var realWidth = entry.Width == 0 ? 256 : entry.Width;
            var realHeight = entry.Height == 0 ? 256 : entry.Height;

            return new IconFrameInfo
            {
                RealWidth = (uint)realWidth,
                RealHeight = (uint)realHeight,
                BitsPerPixel = biBitCount,
                IsPng = false,
                Entry = entry
            };
        }

        private static IconFrameInfo ReadPngFrameInfo(BinaryReader br, IconDirEntry entry)
        {
            br.BaseStream.Position = entry.ImageOffset;

            // PNG signature
            var sig = br.ReadBytes(8);
            if (sig.Length != 8)
            {
                throw new EndOfStreamException();
            }

            // First chunk should be IHDR
            ReadBigEndianUInt32(br);
            var type = Encoding.ASCII.GetString(br.ReadBytes(4));
            if (type != "IHDR")
            {
                throw new InvalidDataException("PNG in ICO does not start with IHDR!");
            }

            // IHDR data
            var realWidth = ReadBigEndianUInt32(br);
            var realHeight = ReadBigEndianUInt32(br);
            var bitDepth = br.ReadByte(); // 1,2,4,8,16
            var colorType = br.ReadByte(); // 0,2,3,4,6

            // Convert PNG bit depth + color type to “bits per pixel” (stored samples)
            // This is a practical bpp estimate:
            // 0 grayscale: bitDepth
            // 2 truecolor: 3*bitDepth
            // 3 indexed: bitDepth
            // 4 grayscale+alpha: 2*bitDepth
            // 6 rgba: 4*bitDepth
            int channels;
            switch (colorType)
            {
                case 0: channels = 1; break;
                case 2: channels = 3; break;
                case 3: channels = 1; break;
                case 4: channels = 2; break;
                case 6: channels = 4; break;
                default: throw new InvalidDataException("Unknown PNG color type!");
            }

            return new IconFrameInfo
            {
                RealWidth = realWidth,
                RealHeight = realHeight,
                BitsPerPixel = bitDepth * channels,
                IsPng = true,
                Entry = entry,
            };
        }

        private static bool IsPng(BinaryReader br)
        {
            byte[] sig = br.ReadBytes(8);

            return sig.Length == 8 &&
                   sig[0] == 0x89 && sig[1] == 0x50 && sig[2] == 0x4E && sig[3] == 0x47 &&
                   sig[4] == 0x0D && sig[5] == 0x0A && sig[6] == 0x1A && sig[7] == 0x0A;
        }

        private static void ReadExact(Stream s, byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                var n = s.Read(buffer, offset, count);
                if (n <= 0) throw new EndOfStreamException();
                offset += n;
                count -= n;
            }
        }

        private static void WriteLittleEndianUInt16(Stream s, ushort v)
        {
            s.WriteByte((byte)(v & 0xFF));
            s.WriteByte((byte)((v >> 8) & 0xFF));
        }

        private static void WriteLittleEndianUInt32(Stream s, uint v)
        {
            s.WriteByte((byte)(v & 0xFF));
            s.WriteByte((byte)((v >> 8) & 0xFF));
            s.WriteByte((byte)((v >> 16) & 0xFF));
            s.WriteByte((byte)((v >> 24) & 0xFF));
        }

        private static uint ReadBigEndianUInt32(BinaryReader br)
        {
            var b = br.ReadBytes(4);
            if (b.Length != 4) throw new EndOfStreamException();
            return (uint)(b[0] << 24 | b[1] << 16 | b[2] << 8 | b[3]);
        }
    }
}