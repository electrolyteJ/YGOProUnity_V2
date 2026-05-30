using System;
using System.Collections.Generic;
using YGOSharp.OCGWrapper;
using YGOSharp.OCGWrapper.Enums;

namespace YGOSharp
{
    public class Deck
    {
        public IList<int> Main { get; private set; }
        public IList<int> Extra { get; private set; }
        public IList<int> Side { get; private set; }

        public D Deck_O { get; private set; }

        public IList<MonoCardInDeckManager> IMain { get; private set; }
        public IList<MonoCardInDeckManager> IExtra { get; private set; }
        public IList<MonoCardInDeckManager> ISide { get; private set; }
        public IList<MonoCardInDeckManager> IRemoved { get; private set; }

        public class D
        {
            public IList<int> Main = new List<int>();
            public IList<int> Extra = new List<int>();
            public IList<int> Side = new List<int>();
        }

        private void InitializeCollections()
        {
            Main = new List<int>();
            Extra = new List<int>();
            Side = new List<int>();
            Deck_O = new D();
            IMain = new List<MonoCardInDeckManager>();
            IExtra = new List<MonoCardInDeckManager>();
            IRemoved = new List<MonoCardInDeckManager>();
            ISide = new List<MonoCardInDeckManager>();
        }

        public Deck()
        {
            InitializeCollections();
        }

        public Deck(string path)
            : this()
        {
            try
            {
                CopyFrom(YdkDeckImporter.LoadDeck(path, false));
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.Log(e);
            }
        }

        private void CopyFrom(Deck source)
        {
            if (source == null)
            {
                return;
            }

            CopyCards(source.Main, Main);
            CopyCards(source.Extra, Extra);
            CopyCards(source.Side, Side);
            CopyCards(source.Deck_O.Main, Deck_O.Main);
            CopyCards(source.Deck_O.Extra, Deck_O.Extra);
            CopyCards(source.Deck_O.Side, Deck_O.Side);
        }

        private static void CopyCards(IList<int> source, IList<int> destination)
        {
            destination.Clear();
            for (int index = 0; index < source.Count; index++)
            {
                destination.Add(source[index]);
            }
        }

        public int Check(Banlist ban, bool ocg, bool tcg)
        {
            if (Main.Count < 40 ||
                Main.Count > 60 ||
                Extra.Count > 15 ||
                Side.Count > 15)
                return 1;

            IDictionary<int, int> cards = new Dictionary<int, int>();

            IList<int>[] stacks = { Main, Extra, Side };
            foreach (IList<int> stack in stacks)
            {
                foreach (int id in stack)
                {
                    Card card = Card.Get(id);
                    AddToCards(cards, card);
                    if (!ocg && card.Ot == 1 || !tcg && card.Ot == 2)
                        return id;
                    if (card.HasType(CardType.Token))
                        return id;
                }
            }

            if (ban == null)
                return 0;

            foreach (var pair in cards)
            {
                int max = ban.GetQuantity(pair.Key);
                if (pair.Value > max)
                    return pair.Key;
            }

            return 0;
        }

        public int GetCardCount(int code)
        {
            int al = 0;
            try
            {
                al = YGOSharp.CardsManager.Get(code).Alias;
            }
            catch (Exception)
            {
            }
            int returnValue = 0;
            IList<MonoCardInDeckManager>[] stacks = { IMain, IExtra, ISide };
            foreach (var stack in stacks)
            {
                foreach (var item in stack)
                {
                    if (item.cardData.Id == code && item.getIfAlive())
                    {
                        returnValue++;
                        continue;
                    }
                    if (item.cardData.Alias == code && item.getIfAlive())
                    {
                        returnValue++;
                        continue;
                    }
                    if (item.cardData.Id == al && item.getIfAlive())
                    {
                        returnValue++;
                        continue;
                    }
                    if (item.cardData.Alias == al && item.getIfAlive() && al > 0)
                    {
                        returnValue++;
                        continue;
                    }
                }
            }
            return returnValue;
        }

        public bool Check(Deck deck)
        {
            if (deck.Main.Count != Main.Count || deck.Extra.Count != Extra.Count)
                return false;

            IDictionary<int, int> cards = new Dictionary<int, int>();
            IDictionary<int, int> ncards = new Dictionary<int, int>();
            IList<int>[] stacks = { Main, Extra, Side };
            foreach (IList<int> stack in stacks)
            {
                foreach (int id in stack)
                {
                    if (!cards.ContainsKey(id))
                        cards.Add(id, 1);
                    else
                        cards[id]++;
                }
            }
            stacks = new[] { deck.Main, deck.Extra, deck.Side };
            foreach (var stack in stacks)
            {
                foreach (int id in stack)
                {
                    if (!ncards.ContainsKey(id))
                        ncards.Add(id, 1);
                    else
                        ncards[id]++;
                }
            }
            foreach (var pair in cards)
            {
                if (!ncards.ContainsKey(pair.Key))
                    return false;
                if (ncards[pair.Key] != pair.Value)
                    return false;
            }
            return true;
        }

        private static void AddToCards(IDictionary<int, int> cards, Card card)
        {
            int id = card.Id;
            if (card.Alias != 0)
                id = card.Alias;
            if (cards.ContainsKey(id))
                cards[id]++;
            else
                cards.Add(id, 1);
        }

        public List<MonoCardInDeckManager> getAllObjectCardAndDeload()
        {
            List<MonoCardInDeckManager> r = new List<MonoCardInDeckManager>();
            IList<MonoCardInDeckManager>[] stacks = { IMain, IExtra, ISide,IRemoved };
            foreach (var stack in stacks)
            {
                foreach (var item in stack)
                {
                    r.Add(item);
                }
                stack.Clear();
            }
            return r;
        }

        public List<MonoCardInDeckManager> getAllObjectCard()
        {
            List<MonoCardInDeckManager> r = new List<MonoCardInDeckManager>();
            IList<MonoCardInDeckManager>[] stacks = { IMain, IExtra, ISide, IRemoved };
            foreach (var stack in stacks)
            {
                foreach (var item in stack)
                {
                    r.Add(item);
                }
            }
            return r;
        }

        public static void sort(List<MonoCardInDeckManager> cards)
        {
            cards.Sort(comparisonOfCard());
        }

        internal static Comparison<MonoCardInDeckManager> comparisonOfCard()
        {
            return (left, right) =>
            {
                return CardsManager.comparisonOfCard()(left.cardData, right.cardData);
            };
        }

        public static void rand(List<MonoCardInDeckManager> cards)
        {
            System.Random rand = new System.Random();
            for (int i = 0; i < cards.Count; i++)
            {
                int random_index = rand.Next() % cards.Count;
                var t = cards[i];
                cards[i] = cards[random_index];
                cards[random_index] = t;
            }
        }



    }
}
