// See https://aka.ms/new-console-template for more information

using System.Text.Encodings.Web;
using System.Text.Json;
using TextExtractor;

if (args.Length < 3)
{
    Console.WriteLine("Not enough params.");
    return;
}

if (args[0] == "pack")
{
    var translatedDirectoryPath = args[1];
    var originalDirectoryPath = args[2];

    foreach (var file in new DirectoryInfo(translatedDirectoryPath).EnumerateFiles("*.*"))
    {
        var destFile = Path.Combine(originalDirectoryPath, $"{Path.GetFileNameWithoutExtension(file.Name)}.dat");
        if (!File.Exists(destFile))
        {
            Console.WriteLine($"File {destFile} not found.");
            return;
        }
        
        var fileText = File.ReadAllText(file.FullName);
        var translationItems = JsonSerializer.Deserialize<List<TranslationItem>>(fileText)!;
        var text = translationItems.Select(i => string.IsNullOrEmpty(i.T) ? i.O : i.T).ToList();
        Console.WriteLine($"replacing {Path.GetFileName(destFile)}");
        TextReplacer.Replace(destFile, text);
    }
    
    Console.WriteLine("Done!");
}
else if (args[0] == "unpack")
{
    var originalDirectoryPath = args[1];
    var translatedDirectoryPath = args[2];
    Directory.CreateDirectory(translatedDirectoryPath);

    foreach (var file in new DirectoryInfo(originalDirectoryPath).EnumerateFiles("*.*"))
    {
        if (IsLuaFile(file.FullName))
        {
            var text = TextFinder.FindText(file.FullName, out _, out _);
            if (!text.Any())
            {
                continue;
            }
            
            var translationItems = text.Select(i => new TranslationItem
            {
                O = i
            })
            .ToList();
            
            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            };

            var translationFileContent = JsonSerializer.Serialize(translationItems, options);
            
            var translationFilePath = Path.Combine(translatedDirectoryPath,
                $"{Path.GetFileNameWithoutExtension(file.Name)}.json");
            
            Console.WriteLine($"Extracted {file.Name}");
            File.WriteAllText(translationFilePath, translationFileContent);
        }
    }
    
    Console.WriteLine("Done!");
}

bool IsLuaFile(string filePath)
{
    var luaHeader = new byte[] {0x1B, 0x4C, 0x75, 0x61, 0x51};
    
    using var reader = new BinaryReader(File.Open(filePath, FileMode.Open));
    foreach (var t in luaHeader)
    {
        if (reader.ReadByte() != t)
        {
            return false;
        }
    }

    return true;
}