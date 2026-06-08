using UnityEngine;
using YGOSharp;

/// <summary>
/// Compatibility stub — former ArtSystem.MonoCardInDeckManager class.
/// Card-in-deck management has moved to App.Screens.Deck services.
/// </summary>
public class MonoCardInDeckManager : MonoBehaviour
{
    public int CardId;
    public string CardName;
    public Card cardData;

    public MonoCardInDeckManager getIfAlive() => this;
}