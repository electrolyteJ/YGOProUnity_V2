using System;
using System.Collections.Generic;
public static class InterString
{
    const string TranslationSeparator = "->";

    static Dictionary<string, string> translations = new Dictionary<string, string>();

    static string path;

    static string FormatTranslation(string text)
    {
        return text.Replace("@n", "\r\n").Replace("@ui", "");
    }

    public static bool loaded = false;

    public static void initialize(string path)
    {
        InterString.path = path;
        try
        {
            RuntimeTextFile.EnsureFileExists(path);
            string[] lines = RuntimeTextFile.ReadNormalizedLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                string original;
                string translated;
                if (RuntimeTextFile.TrySplitPair(lines[i], TranslationSeparator, out original, out translated))
                {
                    if (!translations.ContainsKey(original))
                    {
                        translations.Add(original, translated);
                    }
                }
            }
        }
        catch (Exception e)
        {
            RuntimeStatus.NoAccess = true;
            RuntimeLog.Exception(e);
        }
        InterStringBootstrap.Apply(Get);
        loaded = true;
    }

    public static string Get(string original)
    {
        
        string return_value = original;
        if (translations.TryGetValue(original, out return_value))
        {
            return FormatTranslation(return_value);
        }
        else if (original != "")
        {
            try
            {
                RuntimeTextFile.AppendLine(path, RuntimeTextFile.FormatPair(original, TranslationSeparator, original));
            }
            catch
            {
                RuntimeStatus.NoAccess = true;
            }
            translations.Add(original, original);
            return FormatTranslation(original);
        }
        else
            return original;
    }

    public static string Get(string original, string replace)
    {
        return Get(original).Replace("[?]", replace);
    }

}
