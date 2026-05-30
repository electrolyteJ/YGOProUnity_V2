using System.Collections.Generic;

namespace YGOSharp
{
    internal sealed class DeckLoadResult
    {
        public readonly List<int> Main = new List<int>();
        public readonly List<int> Extra = new List<int>();
        public readonly List<int> Side = new List<int>();
    }
}
