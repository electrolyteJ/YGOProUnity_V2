using System;
using System.Collections.Generic;
public static class Config
{
    public static uint ClientVersion = 0x1362;

    const string TranslationSeparator = "->";
    const string UiSeparator = "=";

    class oneString
    {
        public string original = "";
        public string translated = "";
    }

    static List<oneString> translations = new List<oneString>();

    static List<oneString> uits = new List<oneString>();    

    static string path;

    static oneString CreateString(string original, string translated)
    {
        oneString s = new oneString();
        s.original = original;
        s.translated = translated;
        return s;
    }

    static void LoadStrings(string[] lines, string separator, List<oneString> target)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            string original;
            string translated;
            if (RuntimeTextFile.TrySplitPair(lines[i], separator, out original, out translated))
            {
                target.Add(CreateString(original, translated));
            }
        }
    }

    static string[] BuildTranslationLines()
    {
        string[] lines = new string[translations.Count];
        for (int i = 0; i < translations.Count; i++)
        {
            lines[i] = RuntimeTextFile.FormatPair(translations[i].original, TranslationSeparator, translations[i].translated);
        }

        return lines;
    }

    public static bool getEffectON(string raw)
    {
        return true;
    }

    public static void initialize(string path)
    {
        Config.path = path;
        try
        {
            RuntimeTextFile.EnsureFileExists(path);
            LoadStrings(RuntimeTextFile.ReadNormalizedLines(path), TranslationSeparator, translations);
        }
        catch (Exception e)
        {
            RuntimeStatus.NoAccess = true;
            RuntimeLog.Exception(e);
        }
    }

    static bool loaded = false;

    public static string Getui(string original)
    {
        if (loaded == false)
        {
            loaded = true;
            string uiConfigPath = RuntimePaths.GetFilePath(RuntimeDirectory.Texture, "ui", "config.txt");
            LoadStrings(RuntimeTextFile.ReadNormalizedLines(uiConfigPath, true), UiSeparator, uits);
        }
        string return_value = "";
        for (int i = 0; i < uits.Count; i++)
        {
            if (uits[i].original == original)
            {
                return_value = uits[i].translated;
                break;
            }
        }
        return return_value;
    }

    public static float getFloat(string v)
    {
        int getted = 0;
        try
        {
            getted = Int32.Parse(Get(v, "0"));
        }
        catch (Exception)   
        {
        }
        return ((float)getted) / 100000f;
    }

    public static void setFloat(string v,float f) 
    {
        Set(v,((int)(f* 100000f)).ToString());
    }

    public static string Get(string original,string defau)  
    {
        string return_value = defau;
        bool finded = false;
        for (int i = 0; i < translations.Count; i++)
        {
            if (translations[i].original == original)
            {
                return_value = translations[i].translated;
                finded = true;
                break;
            }
        }
        if (finded == false)
        {
            if (path != null)
            {
                try
                {
                    RuntimeTextFile.AppendLine(path, RuntimeTextFile.FormatPair(original, TranslationSeparator, defau));
                }
                catch (Exception e)
                {
                    RuntimeStatus.NoAccess = true;
                    RuntimeLog.Exception(e);
                }
                oneString s = CreateString(original, defau);
                return_value = defau;
                translations.Add(s);
            }
        }
        return return_value;
    }

    public static void Set(string original,string setted)
    {
        bool finded = false;
        for (int i = 0; i < translations.Count; i++)
        {
            if (translations[i].original == original)
            {
                finded = true;
                translations[i].translated = setted;
            }
        }
        if (finded == false)
        {
            translations.Add(CreateString(original, setted));
        }
        try
        {
            RuntimeTextFile.WriteLinesWithWindowsLineEndings(path, BuildTranslationLines());
        }
        catch (Exception e)
        {
            RuntimeStatus.NoAccess = true;
            RuntimeLog.Exception(e);
        }
    }
}
