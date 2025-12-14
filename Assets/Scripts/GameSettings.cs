using UnityEngine;
public static class GameSettings
{
    // Deck folder under Resources/Decks/<SelectedDeckName>
    public static string SelectedDeckName = "Standard";

    // Backgrounds folder under Resources/Backgrounds/<SelectedBackgroundName>
    public static string SelectedBackgroundName = "Red1";    // e.g. "GreenFelt"
    public static Sprite SelectedBackgroundSprite = null;
}
