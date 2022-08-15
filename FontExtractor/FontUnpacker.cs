using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Decoder = SevenZip.Compression.LZMA.Decoder;

namespace FontExtractor;

public static class FontUnpacker
{
    public static void Unpack(string filePath, string outputDirectoryPath)
    {
        var fontFileInfo = LoadFileInfo(filePath);
        
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        outputDirectoryPath = Path.Combine(outputDirectoryPath, fileName);
        Directory.CreateDirectory(outputDirectoryPath);

        var characterFileInfos = fontFileInfo.CharacterInfos
            .OrderBy(c => c.Index)
            .Select(c => new CharacterFileInfo
            {
                Character = Convert.ToChar(c.Unicode),
                Index = c.Index
            })
            .ToList();

        for (var i = 0; i < fontFileInfo.CharacterDataList.Count; i++)
        {
            var characterData = fontFileInfo.CharacterDataList[i];
            if (characterData.Height > 0)
            {
                var decompressed = DecompressCharacter(characterData.DataBytes, characterData.Height, characterData.Width);
                decompressed.Save(Path.Combine(outputDirectoryPath, $"{i+1}.bmp"));
            }

            var characterFileInfo = characterFileInfos.Single(c => c.Index == i+1);
            characterFileInfo.Height = characterData.Height;
            characterFileInfo.Width = characterData.Width;
            characterFileInfo.Right = characterData.Right;
            characterFileInfo.Left = characterData.Left;
            characterFileInfo.Top = characterData.Top;
        }

        var options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };
        
        var name = Encoding.UTF8.GetString(fontFileInfo.NameBytes);
        var name2 = Encoding.UTF8.GetString(fontFileInfo.Name2Bytes);
        
        var charactersFileContent = JsonSerializer.Serialize(characterFileInfos, options);
        File.WriteAllText(Path.Combine(outputDirectoryPath, "characters.json"), charactersFileContent);
        File.WriteAllText(Path.Combine(outputDirectoryPath, $"!{name}_{name2}_{fontFileInfo.FontSize}"), "");

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

    public static FontFileInfo LoadFileInfo(string filePath)
    {
        using var reader = new BinaryReader(File.Open(filePath, FileMode.Open));
        
        var fileInfo = new FontFileInfo();
        
        fileInfo.Data1 = reader.ReadInt32();
        fileInfo.Data2 = reader.ReadInt32();
        fileInfo.NameLength = reader.ReadInt32();
        fileInfo.NameBytes = reader.ReadBytes(fileInfo.NameLength);

        fileInfo.Name2Length = reader.ReadInt32();
        fileInfo.Name2Bytes = reader.ReadBytes(fileInfo.Name2Length);

        fileInfo.CharactersCount = reader.ReadInt32();
        for (var i = 0; i < fileInfo.CharactersCount; i++)
        {
            fileInfo.CharacterInfos.Add(new CharacterInfo
            {
                Unicode = reader.ReadInt32(),
                Index = reader.ReadInt32()
            });
        }

        fileInfo.Data3 = reader.ReadInt32();
        fileInfo.FontSize = reader.ReadInt32();
        fileInfo.Data4 = reader.ReadInt32();
        fileInfo.Data5 = reader.ReadInt32();

        fileInfo.Data6 = reader.ReadBytes(18);
        for (int i = 0; i < fileInfo.CharactersCount; i++)
        {
            CharacterData data = new();
            
            data.Height = reader.ReadByte();
            data.Right = reader.ReadByte();
            data.Width = reader.ReadByte();
            data.DataSize = reader.ReadInt16();
            data.DataBytes = reader.ReadBytes(data.DataSize);
            data.Data1 = reader.ReadBytes(11);
            data.Left = reader.ReadByte();
            data.Top = reader.ReadByte();

            fileInfo.CharacterDataList.Add(data);
        }

        fileInfo.Data7 = reader.ReadBytes(4);

        return fileInfo;
    }

    public class FontFileInfo
    {
        public int Data1 { get; set; }
        public int Data2 { get; set; }

        public int NameLength { get; set; }
        public byte[] NameBytes { get; set; } = null!;

        public int Name2Length { get; set; }
        public byte[] Name2Bytes { get; set; } = null!;

        public int CharactersCount { get; set; }
        public List<CharacterInfo> CharacterInfos { get; set; } = new();

        public int Data3 { get; set; }
        public int FontSize { get; set; }
        public int Data4 { get; set; }
        public int Data5 { get; set; }

        public byte[] Data6 { get; set; } = null!;

        public List<CharacterData> CharacterDataList { get; set; } = new();
        public byte[] Data7 { get; set; } = null!;
    }

    public class CharacterInfo
    {
        public int Unicode { get; set; }
        public int Index { get; set; }
    }

    public class CharacterData
    {
        public byte Height { get; set; }
        public byte Right { get; set; }
        public byte Width { get; set; }
        public short DataSize { get; set; }
        public byte[] DataBytes { get; set; } = null!;
        public byte[] Data1 { get; set; } = null!;
        public byte Left { get; set; }
        public byte Top { get; set; }
    }

    public class CharacterFileInfo
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