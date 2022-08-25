// See https://aka.ms/new-console-template for more information

using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using TextExtractor;

if (args.Length < 1)
{
    Console.WriteLine("Not enough params.");
    return;
}

if (args[0] == "pack")
{
    if (args.Length < 3)
    {
        Console.WriteLine("Not enough params.");
        return;
    }
    
    var translatedDirectoryPath = args[1];
    var originalDirectoryPath = args[2];

    foreach (var file in new DirectoryInfo(translatedDirectoryPath).EnumerateFiles("*.*"))
    {
        var destFile = Path.Combine(originalDirectoryPath, $"{Path.GetFileNameWithoutExtension(file.Name)}.dat");
        if (!File.Exists(destFile))
        {
            Console.WriteLine($"File {destFile} not found. Skipping.");
            continue;
        }
        
        var fileText = File.ReadAllText(file.FullName);
        var translationItems = JsonSerializer.Deserialize<List<TranslationItem>>(fileText)!;
        var text = translationItems
            .Select(i => string.IsNullOrEmpty(i.T) ? i.O + i.E : i.T + i.E).ToList();
        Console.WriteLine($"replacing {Path.GetFileName(destFile)}");
        TextReplacer.Replace(destFile, text);
    }
    
    Console.WriteLine("Done!");
}
else if (args[0] == "unpack")
{
    if (args.Length < 3)
    {
        Console.WriteLine("Not enough params.");
        return;
    }
    
    var originalDirectoryPath = args[1];
    var translatedDirectoryPath = args[2];
    var headerPattern = args.Length > 3 ? args[3] : "script\\language";
    Directory.CreateDirectory(translatedDirectoryPath);

    foreach (var file in new DirectoryInfo(originalDirectoryPath).EnumerateFiles("*.*"))
    {
        if (IsScriptFile(file.FullName, headerPattern, out var isEnglish))
        {
            var text = TextFinder.FindText(file.FullName, out _, out _);
            if (!text.Any())
            {
                continue;
            }
            
            var translationItems = text
                .Select(i => new TranslationItem { O = i })
                .Select(PhraseAnalyzer.FillEndingsAndAutoTranslations)
                .ToList();
            
            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            };

            var translationFileContent = JsonSerializer.Serialize(translationItems, options);

            var langDir = isEnglish ? "en" : "jp";
            var translationSubDir = Path.Combine(translatedDirectoryPath, langDir);
            Directory.CreateDirectory(translationSubDir);
            var translationFilePath = Path.Combine(translationSubDir, $"{Path.GetFileNameWithoutExtension(file.Name)}.json");
            
            Console.WriteLine($"Extracted {file.Name}");
            File.WriteAllText(translationFilePath, translationFileContent);
        }
    }
    
    Console.WriteLine("Done!");
}
else if (args[0] == "stats")
{
    if (args.Length < 2)
    {
        Console.WriteLine("Not enough params.");
        return;
    }
    
    StatsPrinter.Print(args[1]);
}


bool IsScriptFile(string filePath, string headerPattern, out bool isEnglish)
{
    // var luaHeader = new byte[] {0x1B, 0x4C, 0x75, 0x61, 0x51};
    //
    using var reader = new BinaryReader(File.Open(filePath, FileMode.Open));
    // foreach (var t in luaHeader)
    // {
    //     if (reader.ReadByte() != t)
    //     {
    //         return false;
    //     }
    // }
    //
    // return true;

    var startBytes = reader.ReadBytes(128);
    var text = Encoding.UTF8.GetString(startBytes);
    if (text.Contains(headerPattern))
    {
        isEnglish = text.Contains("_en_us");
        return true;
    }

    isEnglish = false;
    return false;
}