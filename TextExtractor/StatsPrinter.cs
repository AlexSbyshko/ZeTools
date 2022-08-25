using System.Text.Json;

namespace TextExtractor;

public static class StatsPrinter
{
    public static void Print(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            Console.WriteLine("Invalid path.");
            return;
        }
        
        var attr = File.GetAttributes(path);

        List<TranslationItem> items;
        
        if (attr.HasFlag(FileAttributes.Directory))
        {
            items = new DirectoryInfo(path)
                .EnumerateFiles("*.json")
                .SelectMany(f => GetFileItems(f.FullName))
                .ToList();
        }
        else
        {
            items = GetFileItems(path);
        }
        
        PrintStats(items);
    }

    private static List<TranslationItem> GetFileItems(string filePath)
    {
        var fileText = File.ReadAllText(filePath);
        var translationItems = JsonSerializer.Deserialize<List<TranslationItem>>(fileText)!;
        return translationItems;
    }

    private static void PrintStats(IReadOnlyCollection<TranslationItem> items)
    {
        var nonSystemItems = items.Where(i => !i.O.Contains('_')).ToList();
        var totalItems = nonSystemItems.Count;
        var nonTranslatedItems = nonSystemItems.Count(i => string.IsNullOrEmpty(i.T) && !string.IsNullOrEmpty(i.O));
        var translatedItems = totalItems - nonTranslatedItems;
        
        Console.WriteLine($"Translated: {translatedItems}/{totalItems} ({(double)translatedItems/totalItems * 100:0.##}%)");
        Console.WriteLine($"{nonTranslatedItems} left.");
    }
}