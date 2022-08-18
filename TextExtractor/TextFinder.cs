using System.Text;

namespace TextExtractor;

public static class TextFinder
{
    private static byte[] Start = {0x00, 0x01, 0x20, 0x00, 0x80, 0x00};

    private const byte ContinueFlag = 0x04;
    private const byte StopFlag = 0x00;

    public static List<string> FindText(string filePath, out long startTextOffset, out long remainingBytesOffset)
    {
        var data = File.ReadAllBytes(filePath);
        List<string> result = new();

        startTextOffset = FindTextStartOffset(data);
        
        if (startTextOffset == -1)
        {
            remainingBytesOffset = -1;
            return result;
        }

        using var reader = new BinaryReader(new MemoryStream(data));
        reader.BaseStream.Seek(startTextOffset, SeekOrigin.Begin);

        byte flag;
        do
        {
            var size = reader.ReadInt32();
            var content =  reader.ReadBytes(size);
            flag = reader.ReadByte();

            var textString = Encoding.UTF8.GetString(content[..^1]);
            result.Add(textString);
        }
        while (flag == ContinueFlag);

        remainingBytesOffset = reader.BaseStream.Position;

        return result;
    }

    private static int FindTextStartOffset(byte[] source)
    {
        int start = 0;
        while (true)
        {
            int offset = FindSequence(source, Start, start);
            if (offset == -1)
            {
                return -1;
            }

            if (source[offset + 2 + Start.Length] != 0x00)
            {
                start += 1;
                continue;
            }
            if (source[offset + 2 + Start.Length + 1] != 0x00)
            {
                start += 1;
                continue;
            }

            if (source[offset + 2 + Start.Length + 2] != 0x04)
            {
                start += 1;
                continue;
            }

            return offset + Start.Length + 5;
        }
    }
    
    private static int FindSequence(byte[] source, byte[] seq, int s)
    {
        var start = -1;
        for (var i = s; i < source.Length - seq.Length + 1 && start == -1; i++)
        {
            var j = 0;
            for (; j < seq.Length && source[i+j] == seq[j]; j++) {}
            if (j == seq.Length) start = i;
        }
        return start;
    }
}