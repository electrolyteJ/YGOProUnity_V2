using System.Collections.Generic;
using System.Text;

namespace YGOSharp
{
    internal static class YdkDeckSerializer
    {
        internal static string Serialize(IList<int> main, IList<int> extra, IList<int> side)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("#created by ygopro2\r\n");
            builder.Append("#main\r\n");
            AppendCards(builder, main);
            builder.Append("#extra\r\n");
            AppendCards(builder, extra);
            builder.Append("!side\r\n");
            AppendCards(builder, side);
            return builder.ToString();
        }

        private static void AppendCards(StringBuilder builder, IList<int> cards)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                builder.Append(cards[i]);
                builder.Append("\r\n");
            }
        }
    }
}
