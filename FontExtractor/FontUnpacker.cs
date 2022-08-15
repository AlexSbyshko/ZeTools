using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using Decoder = SevenZip.Compression.LZMA.Decoder;

namespace FontExtractor;

public static class FontUnpacker
{
    public static void Unpack(string filePath, string outputDirectoryPath, out FontFileInfo fontFileInfo)
    {
        fontFileInfo = new FontFileInfo();
        
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        outputDirectoryPath = Path.Combine(outputDirectoryPath, fileName);
        Directory.CreateDirectory(outputDirectoryPath);

        using var reader = new BinaryReader(File.Open(filePath, FileMode.Open));

        var data1 = reader.ReadInt32();
        var data2 = reader.ReadInt32();
        var nameLength = reader.ReadInt32();
        var nameBytes = reader.ReadBytes(nameLength);

        var name2Length = reader.ReadInt32();
        var name2Bytes = reader.ReadBytes(name2Length);

        var name = Encoding.UTF8.GetString(nameBytes);
        var name2 = Encoding.UTF8.GetString(name2Bytes);

        fontFileInfo.CharactersCountOffset = reader.BaseStream.Position;
        var characterCount = reader.ReadInt32();
        for (var i = 0; i < characterCount; i++)
        {
            var unicode = reader.ReadInt32();
            var ch = Convert.ToChar(unicode);
            var index = reader.ReadInt32();
            
            fontFileInfo.Fonts.Add(new FontInfo
            {
                Index = index,
                Character = ch,
            });
        }

        var data3 = reader.ReadInt32();
        var fontSize = reader.ReadInt32();
        var data5 = reader.ReadInt32();
        var data6 = reader.ReadInt32();

        var unknownBytes = reader.ReadBytes(18);
        fontFileInfo.StartCharactersOffset = reader.BaseStream.Position;
        for (int i = 0; i < characterCount; i++)
        {
            var height = reader.ReadByte();
            var right = reader.ReadByte();
            var width = reader.ReadByte();
            var dataSize = reader.ReadInt16();

            var characterData = reader.ReadBytes(dataSize);

            if (height > 0)
            {
                var decompressed = DecompressCharacter(characterData, height, width);
                decompressed.Save(Path.Combine(outputDirectoryPath, $"Character_{i + 1}.bmp"));
            }

            var un1 = reader.ReadBytes(11);
            var left = reader.ReadByte();
            var top = reader.ReadByte();

            var fontInfo = fontFileInfo.Fonts.Single(f => f.Index == i + 1);
            fontInfo.Height = height;
            fontInfo.Right = right;
            fontInfo.Width = width;
            fontInfo.Left = left;
            fontInfo.Top = top;
        }

        var data7 = reader.ReadBytes(4);

        Console.WriteLine("Done!");
    }

    
    private static Bitmap DecompressCharacter(byte[] data, byte height, byte width)
    {
        var decoderProperties = data[..5];
        data = data[5..];
        long outSize = height * width;
        using var inputStream = new MemoryStream(data);
        using var outputStream = new MemoryStream();
        var decoder = new Decoder();
        decoder.SetDecoderProperties(decoderProperties);
        decoder.Code(inputStream, outputStream, data.Length, outSize, null);
        var decompressed = outputStream.ToArray();

        var bitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
        ColorPalette palette = bitmap.Palette;
        for (int i = 0; i < 256; i++)
        {
            palette.Entries[i] = Color.FromArgb(i, i, i);
        }

        bitmap.Palette = palette;
        var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly, bitmap.PixelFormat);
        byte[] array3 = new byte[bitmapData.Stride * bitmapData.Height];
        for (int j = 0; j < bitmap.Height; j++)
        {
            for (int k = 0; k < bitmapData.Stride; k++)
            {
                if (k < bitmap.Width)
                {
                    array3[j * bitmapData.Stride + k] = decompressed[j * bitmap.Width + k];
                }
                else
                {
                    array3[j * bitmapData.Stride + k] = 0;
                }
            }
        }

        Marshal.Copy(array3, 0, bitmapData.Scan0, array3.Length);
        bitmap.UnlockBits(bitmapData);

        return bitmap;
    }

    public class FontFileInfo
    {
        public long StartCharactersOffset { get; set; }
        public long CharactersCountOffset { get; set; }

        public List<FontInfo> Fonts { get; set; } = new();
    }

    public class FontInfo
    {
        public int Index { get; set; }
        public char Character { get; set; }
        public byte Height { get; set; }
        public byte Right { get; set; }
        public byte Width { get; set; }
        public byte Left { get; set; }
        public byte Top { get; set; }
    }
}