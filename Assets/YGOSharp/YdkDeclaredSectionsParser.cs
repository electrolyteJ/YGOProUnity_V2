using System;
using System.Collections.Generic;

namespace YGOSharp
{
    internal static class YdkDeclaredSectionsParser
    {
        internal sealed class Sections
        {
            public readonly List<int> Main = new List<int>();
            public readonly List<int> Extra = new List<int>();
            public readonly List<int> Side = new List<int>();
        }

        internal static Sections Parse(string text)
        {
            Sections sections = new Sections();
            string normalizedText = text.Replace("\r", "");
            string[] lines = normalizedText.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            int flag = -1;

            foreach (string line in lines)
            {
                if (line == "#main")
                {
                    flag = 1;
                }
                else if (line == "#extra")
                {
                    flag = 2;
                }
                else if (line == "!side")
                {
                    flag = 3;
                }
                else
                {
                    int code = 0;
                    try
                    {
                        code = Int32.Parse(line);
                    }
                    catch (Exception)
                    {
                    }

                    if (code <= 100)
                    {
                        continue;
                    }

                    switch (flag)
                    {
                        case 1:
                            sections.Main.Add(code);
                            break;
                        case 2:
                            sections.Extra.Add(code);
                            break;
                        case 3:
                            sections.Side.Add(code);
                            break;
                    }
                }
            }

            return sections;
        }
    }
}
