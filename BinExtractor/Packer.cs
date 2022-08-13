using System.Text;

namespace BinExtractor;

public class Packer
{
    public void Pack(string modifiedContentDirectoryPath, string originalFilePath, string destinationFilePath)
    {
        File.Copy(originalFilePath, destinationFilePath, overwrite: true);

        var destFileSize = new FileInfo(destinationFilePath).Length;
        var modifiedFilesInfos = new DirectoryInfo(modifiedContentDirectoryPath)
            .EnumerateFiles()
            .ToDictionary(f => Path.GetFileNameWithoutExtension(f.Name), f => new {f.Length, f.FullName});


        var key = Helper.nonary_calculate_key("ZeroEscapeTNG");

        int size = 0x20;
        byte[] xorKey;
        uint ret = 0;

        SetKey();

        using var fileReader = new BinaryReader(File.Open(originalFilePath, FileMode.Open));
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
        var fileListMemory = fileReader.ReadBytes(size);
        Helper.Xor(fileListMemory, xorKey);

        var fileListMemoryXorKey = xorKey.ToArray();
        var fileListMemoryOffset = offset;
        var modifiedFileListMemory = fileListMemory.ToArray();
        using var modifiedFileListWriter = new BinaryWriter(new MemoryStream(modifiedFileListMemory));

        using var currentReader2 = new BinaryReader(new MemoryStream(fileListMemory));

        var fileListOffset = currentReader2.ReadUInt32();
        var filesCount = currentReader2.ReadUInt32();

        currentReader2.BaseStream.Seek(fileListOffset, SeekOrigin.Begin);

        List<FileToAppendInfo> filesToAppend = new();
        long appendedFilesOffset = destFileSize - (long)offset4;

        using (var fileWriter = new BinaryWriter(File.Open(destinationFilePath, FileMode.OpenOrCreate)))
        {
            for (int i = 0; i < filesCount; i++)
            {
                var offsetOffset = currentReader2.BaseStream.Position;
                var fileOffset = currentReader2.ReadUInt64();
                var globalFileOffset = fileOffset + offset4;
                key = currentReader2.ReadUInt32();

                var sizeOffset = currentReader2.BaseStream.Position;
                size = currentReader2.ReadInt32();
                var xSize = currentReader2.ReadUInt32();
                var id = currentReader2.ReadUInt32();
                var flags = currentReader2.ReadUInt32();
                var dummy = currentReader2.ReadInt32();

                if (!modifiedFilesInfos.TryGetValue($"{id:x8}", out var fileInfo))
                {
                    continue;
                }

                ret = 0;
                SetKey();

                if (fileInfo.Length <= size)
                {
                    Console.WriteLine($"Rewriting {Path.GetFileName(fileInfo.FullName)}");
                    
                    var contentBuffer = new byte[size];
                    var modifiedFileContent = File.ReadAllBytes(fileInfo.FullName);
                    Array.Copy(modifiedFileContent, contentBuffer, modifiedFileContent.Length);
                    Helper.Xor(contentBuffer, xorKey);
                    fileWriter.BaseStream.Seek((long) globalFileOffset, SeekOrigin.Begin);
                    fileWriter.Write(contentBuffer);
                }
                else
                {
                    Console.WriteLine($"Skipping {Path.GetFileName(fileInfo.FullName)}");

                    modifiedFileListWriter.Seek((int)offsetOffset, SeekOrigin.Begin);
                    modifiedFileListWriter.Write(appendedFilesOffset);

                    modifiedFileListWriter.Seek((int)sizeOffset, SeekOrigin.Begin);
                    modifiedFileListWriter.Write(fileInfo.Length);
                    
                    filesToAppend.Add(new FileToAppendInfo
                    {
                        FullName = fileInfo.FullName,
                        Key = key,
                        Size = fileInfo.Length
                    });
                    appendedFilesOffset += fileInfo.Length;
                }
            }
            
            Helper.Xor(modifiedFileListMemory, fileListMemoryXorKey);
            fileWriter.Seek((int) fileListMemoryOffset, SeekOrigin.Begin);
            fileWriter.Write(modifiedFileListMemory);
        }

        using (var fileWriter = new BinaryWriter(File.Open(destinationFilePath, FileMode.Append, FileAccess.Write)))
        {
            foreach (var fileToAppend in filesToAppend)
            {
                Console.WriteLine($"Appending {Path.GetFileName(fileToAppend.FullName)}");
                
                ret = 0;
                size = (int)fileToAppend.Size;
                key = fileToAppend.Key;
                SetKey();
                var fileContent = File.ReadAllBytes(fileToAppend.FullName);
                Helper.Xor(fileContent, xorKey);
                fileWriter.Write(fileContent);
            }
        }
        
        Console.WriteLine("done!");

        void SetKey()
        {
            xorKey = new byte[size];
            ret = Helper.nonary_crypt(xorKey, size, key, ret);
        }
    }

    private class FileToAppendInfo
    {
        public string FullName { get; set; } = string.Empty;
        public uint Key { get; set; }
        public long Size { get; set; }
    }
}