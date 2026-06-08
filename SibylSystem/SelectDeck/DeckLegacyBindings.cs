using App.Screens.Deck.Services;
using App.Platform;

public static class DeckLegacyBindings
{
    public static DeckFlowService CreateFlowService()
    {
        return new DeckFlowService(new RuntimePlatformPaths(), new RuntimeFileStorage());
    }

    public static DeckSortMode ReadSortMode(string configValue)
    {
        return configValue == "1" ? DeckSortMode.ByTime : DeckSortMode.ByName;
    }

    public static string WriteSortMode(DeckSortMode sortMode)
    {
        return sortMode == DeckSortMode.ByTime ? "1" : "0";
    }

    public static DeckSortMode ToggleSortMode(DeckSortMode sortMode)
    {
        return sortMode == DeckSortMode.ByTime ? DeckSortMode.ByName : DeckSortMode.ByTime;
    }
}
