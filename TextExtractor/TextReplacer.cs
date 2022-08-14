using System.Text;

namespace TextExtractor;

public static class TextReplacer
{
    private const byte ContinueFlag = 0x04;
    private const byte StopFlag = 0x00;
    
    public static void Replace(string filePath, List<string> newStrings)
    {
        var data = File.ReadAllBytes(filePath);
        var currentText = TextFinder.FindText(filePath, out var startTextOffset, out var remainingBytesOffset);
        if (startTextOffset == -1)
        {
            return;
        }

        if (currentText.Count != newStrings.Count)
        {
            Console.WriteLine($"[ERR] Original file {Path.GetFileName(filePath)} has {currentText.Count} string," +
                              $" but the new text has {newStrings.Count} strings.");
        }

        using var reader = new BinaryReader(new MemoryStream(data));
        
        reader.BaseStream.Seek(0, SeekOrigin.Begin);
        var startBytes = reader.ReadBytes((int)startTextOffset);
        
        reader.BaseStream.Seek(remainingBytesOffset, SeekOrigin.Begin);
        var endBytes = reader.ReadBytes(data.Length - (int)remainingBytesOffset);

        using var writer = new BinaryWriter(File.Open(filePath, FileMode.Truncate, FileAccess.Write));

        writer.Write(startBytes);

        for (var i = 0; i < newStrings.Count; i++)
        {
            var str = newStrings[i];
            var bytes = Encoding.UTF8.GetBytes(str);
            
            writer.Write(bytes.Length + 1);
            writer.Write(bytes);
            writer.Write((byte)0x00);

            if (i < newStrings.Count - 1)
            {
                writer.Write(ContinueFlag);
            }
            else
            {
                writer.Write(StopFlag);
            }
        }
        
        writer.Write(endBytes);
    }
}