namespace TextExtractor;

public class PhraseAnalyzer
{
    private static string[] Endings = { "<K><N><N>", "<K><P>", "<K><N>", " <K>" };
    
    public static TranslationItem FillEndingsAndAutoTranslations(TranslationItem item)
    {
        foreach (var ending in Endings)
        {
            if (item.O.EndsWith(ending))
            {
                item.E = ending;
                item.O = item.O[..^ending.Length];
                break;
            }
        }

        if (item.O.Contains('_'))
        {
            item.T = item.O;
        }

        return item;
    }
}