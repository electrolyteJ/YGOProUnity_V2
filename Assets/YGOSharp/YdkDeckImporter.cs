namespace YGOSharp
{
    internal static class YdkDeckImporter
    {
        internal static DeckLoadResult ParseDeclaredSections(string text)
        {
            DeckLoadResult result = new DeckLoadResult();
            YdkDeclaredSectionsParser.Sections sections = YdkDeclaredSectionsParser.Parse(text);

            AppendSection(result.Main, sections.Main);
            AppendSection(result.Extra, sections.Extra);
            AppendSection(result.Side, sections.Side);
            return result;
        }

        internal static void FillDeck(DeckLoadResult source, Deck target, bool classifyExtraWithCardData)
        {
            if (source == null || target == null)
            {
                return;
            }

            ClearDeck(target);
            AppendSection(source.Main, target, classifyExtraWithCardData, false, false);
            AppendSection(source.Extra, target, classifyExtraWithCardData, true, false);
            AppendSection(source.Side, target, classifyExtraWithCardData, false, true);
        }

        internal static Deck LoadDeck(string path, bool classifyExtraWithCardData)
        {
            Deck deck = new Deck();
            FillDeck(LoadDeckData(path), deck, classifyExtraWithCardData);
            return deck;
        }

        internal static DeckLoadResult LoadDeckData(string path)
        {
            return ParseDeclaredSections(RuntimeTextFile.ReadAllText(path));
        }

        private static void ClearDeck(Deck deck)
        {
            deck.Main.Clear();
            deck.Extra.Clear();
            deck.Side.Clear();
            deck.Deck_O.Main.Clear();
            deck.Deck_O.Extra.Clear();
            deck.Deck_O.Side.Clear();
        }

        private static void AppendSection(System.Collections.Generic.ICollection<int> target, System.Collections.Generic.IList<int> source)
        {
            for (int index = 0; index < source.Count; index++)
            {
                int code = source[index];
                if (code > 100)
                {
                    target.Add(code);
                }
            }
        }

        private static void AppendSection(System.Collections.Generic.IList<int> source, Deck target, bool classifyExtraWithCardData, bool sourceIsExtraSection, bool sideSection)
        {
            for (int index = 0; index < source.Count; index++)
            {
                AppendCode(source[index], target, classifyExtraWithCardData, sourceIsExtraSection, sideSection);
            }
        }

        private static void AppendCode(int code, Deck target, bool classifyExtraWithCardData, bool sourceIsExtraSection, bool sideSection)
        {
            if (code <= 100)
            {
                return;
            }

            if (!sideSection && classifyExtraWithCardData)
            {
                Card card = CardsManager.Get(code);
                if (card != null && card.Id > 0)
                {
                    if (card.IsExtraCard())
                    {
                        AddExtra(target, code);
                    }
                    else
                    {
                        AddMain(target, code);
                    }
                    return;
                }
            }

            if (sideSection)
            {
                AddSide(target, code);
            }
            else
            {
                AddDeclaredSectionCard(target, code, sourceIsExtraSection);
            }
        }

        private static void AddDeclaredSectionCard(Deck target, int code, bool sourceIsExtraSection)
        {
            if (sourceIsExtraSection)
            {
                AddExtra(target, code);
            }
            else
            {
                AddMain(target, code);
            }
        }
        private static void AddMain(Deck target, int code)
        {
            target.Main.Add(code);
            target.Deck_O.Main.Add(code);
        }

        private static void AddExtra(Deck target, int code)
        {
            target.Extra.Add(code);
            target.Deck_O.Extra.Add(code);
        }

        private static void AddSide(Deck target, int code)
        {
            target.Side.Add(code);
            target.Deck_O.Side.Add(code);
        }
    }
}
