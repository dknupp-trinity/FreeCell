using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class deckGenerator : MonoBehaviour
{
    [SerializeField] CardController cardPrefab;
    [SerializeField] Sprite cardBackSprite;
    //[SerializeField] private Sprite[] inspectorSprites;
    [SerializeField] string inspectorDeckOverride = "";


    private Sprite[] allSprites;


    public List<CardController> GenerateDeck()
    {
        // Load all sprites from deck sprite folder
        LoadSprites();

        List<CardController> deck = new List<CardController>();

        // Define suit mappings
        Dictionary<char, Suit> suitMap = new Dictionary<char, Suit>()
        {
            { 'C', Suit.Clubs },
            { 'D', Suit.Diamonds },
            { 'H', Suit.Hearts },
            { 'S', Suit.Spades }
        };

        // Create a card for each sprite (except jokers)
        foreach (Sprite sprite in allSprites)
        {
            string spriteName = sprite.name.ToUpper();

            // Skip jokers
            if (spriteName.Contains("JOKER"))
                continue;

            // Parse sprite name (e.g., "C01", "H13", "S10")
            if (spriteName.Length < 3 || !suitMap.ContainsKey(spriteName[0]))
                continue;

            char suitChar = spriteName[0];
            string rankStr = spriteName.Substring(1);

            if (!int.TryParse(rankStr, out int rank) || rank < 1 || rank > 13)
                continue;

            // Create CardData
            CardData cardData = new CardData(rank, suitMap[suitChar], sprite);

            // Create card object
            CardController card = Instantiate(cardPrefab);
            card.SetCard(cardData, cardBackSprite, false);
            card.gameObject.name = cardData.DisplayName;

            deck.Add(card);
        }

        if (deck.Count != 52)
        {
            Debug.LogWarning($"Generated {deck.Count} cards instead of 52. Check sprite naming.");
        }

        // Shuffle deck
        deck = deck.OrderBy(x => Random.value).ToList();

        return deck;
    }

    private void LoadSprites()
    {
        string deckName =
            !string.IsNullOrEmpty(inspectorDeckOverride)
                ? inspectorDeckOverride
                : GameSettings.SelectedDeckName;

        if (string.IsNullOrWhiteSpace(deckName))
        {
            Debug.LogWarning("deckGenerator: No deck selected. Falling back to Standard.");
            deckName = "Standard";
        }

        string spriteFolder = $"Decks/{deckName}";

        Debug.Log($"deckGenerator: Loading deck from Resources/{spriteFolder}");

        allSprites = Resources.LoadAll<Sprite>(spriteFolder);

        if (allSprites == null || allSprites.Length == 0)
        {
            Debug.LogError($"deckGenerator: No sprites found in Resources/{spriteFolder}");
            allSprites = new Sprite[0];
        }
        else
        {
            Debug.Log($"deckGenerator: Loaded {allSprites.Length} sprites from Resources/{spriteFolder}");
        }
    }

}
