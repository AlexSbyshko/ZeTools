using System.Text;

namespace BinExtractor;

public class Unpacker
{
    public void Unpack(string filePath, string outputDirectoryPath)
    {
        var key = Helper.nonary_calculate_key("ZeroEscapeTNG");

        int size = 0x20;
        byte[] xorKey;
        uint ret = 0;

        SetKey();

        using var fileReader = new BinaryReader(File.Open(filePath, FileMode.Open));
        fileReader.BaseStream.Seek(0, SeekOrigin.Begin);

        var memoryFile = fileReader.ReadBytes(size);
        Helper.Xor(memoryFile, xorKey);

        using var currentReader = new BinaryReader(new MemoryStream(memoryFile));
        currentReader.BaseStream.Seek(0, SeekOrigin.Begin);

        var headerBytes = currentReader.ReadBytes(4);

        var header = Encoding.ASCII.GetString(headerBytes);
        var offset1 = currentReader.ReadUInt32();
        var offset2 = currentReader.ReadUInt32();
        var offset3 = currentReader.ReadUInt64();
        var offset4 = currentReader.ReadUInt64();

        var offset = offset2;

        size = (int) (offset4 - offset);

        ret = offset;

        SetKey();

        fileReader.BaseStream.Seek(offset, SeekOrigin.Begin);
        memoryFile = fileReader.ReadBytes(size);
        Helper.Xor(memoryFile, xorKey);

        using var currentReader2 = new BinaryReader(new MemoryStream(memoryFile));

        var fileListOffset = currentReader2.ReadUInt32();

        var filesCount = currentReader2.ReadUInt32();

        currentReader2.BaseStream.Seek(fileListOffset, SeekOrigin.Begin);

        for (int i = 0; i < filesCount; i++)
        {
            var fileOffset = currentReader2.ReadUInt64() + offset4;
            key = currentReader2.ReadUInt32();
            size = currentReader2.ReadInt32();
            var xSize = currentReader2.ReadUInt32();
            var id = currentReader2.ReadUInt32();
            var flags = currentReader2.ReadUInt32();
            var dummy = currentReader2.ReadInt32();

            ret = 0;
            SetKey();

            fileReader.BaseStream.Seek((long) fileOffset, SeekOrigin.Begin);
            var fileContent = fileReader.ReadBytes(size);
            Helper.Xor(fileContent, xorKey);

            var curFilePath = Path.Combine(outputDirectoryPath, $"{id:x8}.dat");
            Console.WriteLine($"Extracting {id:x8}.dat");
            File.WriteAllBytes(curFilePath, fileContent);
        }
        
        Console.WriteLine("done!");

        void SetKey()
        {
            xorKey = new byte[size];
            ret = Helper.nonary_crypt(xorKey, size, key, ret);
        }
    }
}