using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public deckGenerator deckGenerator;

    [SerializeField] private Transform[] freecellPositions = new Transform[4];
    [SerializeField] private Transform[] foundationPositions = new Transform[4];
    [SerializeField] private Transform[] tableauPositions = new Transform[8];

    [Header("Deal animation")]
    [SerializeField] private Transform deckOrigin;
    [SerializeField] private Transform deckParent;
    [SerializeField] private float flipDuration = 0.2f;
    [SerializeField] private float dealDelay = 0.01f;
    [SerializeField] private float cardMoveSpeed = 300f;

    [SerializeField] private float invalidFlashDuration = 0.35f;

    [SerializeField] private List<CardController>[] freecells = new List<CardController>[4];
    [SerializeField] private List<CardController>[] foundations = new List<CardController>[4];
    [SerializeField] private List<CardController>[] tableaus = new List<CardController>[8];
    [SerializeField] private GameObject skipDealTextObject;

    [Header("Win celebration settings")]
    [SerializeField] private float winMoveMinDuration = 0.8f;
    [SerializeField] private float winMoveMaxDuration = 1.8f;
    [SerializeField] private float winFlipDuration = 0.35f;
    [SerializeField] private float winSpawnMargin = 0.08f;
    [SerializeField] private GameObject VictoryTextObject;

    [Header("Win Audio")]
    [SerializeField] private AudioSource mainAudio;
    [SerializeField] private AudioClip winCelebrationClip;

    private CardController selectedCard;
    private InputHandler inputHandler;
    private bool gameWon = false;

    private bool isDealing = false;
    private bool fastForwardRequested = false;
    public event System.Action OnGameWon;
    public event System.Action OnInvalidMove;

    void Start()
    {
        if (deckGenerator == null)
        {
            Debug.LogError("DeckGenerator not assigned in GM");
            return;
        }

        inputHandler = GetComponent<InputHandler>();
        if (inputHandler == null)
            inputHandler = gameObject.AddComponent<InputHandler>();

        InitializeContainers();
        StartCoroutine(GenerateAndDealDeckCoroutine());
    }

    void Update()
    {
        // If currently dealing, allow player to press Space to fast-forward (because dealing animation takes forever))

        if (isDealing && Input.GetKeyDown(KeyCode.Space))
        {
            fastForwardRequested = true;
            Debug.Log("Fast-forward requested: skipping deal delays.");
        }
    }


    void InitializeContainers()
    {
        for (int i = 0; i < 4; i++)
        {
            freecells[i] = new List<CardController>();
            foundations[i] = new List<CardController>();
        }
        for (int i = 0; i < 8; i++)
        {
            tableaus[i] = new List<CardController>();
        }
    }

    // wrapper coroutine so we can run the animated deal
    IEnumerator GenerateAndDealDeckCoroutine()
    {
        List<CardController> deck = deckGenerator.GenerateDeck();

        if (deck.Count == 0)
        {
            Debug.LogError("Failed to generate deck");
            yield break;
        }

        // Add cards to tableau lists (FreeCell deal: 7 to first 4 columns, 6 to last 4)
        int cardIndex = 0;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 7; j++)
            {
                if (cardIndex < deck.Count)
                {
                    CardController card = deck[cardIndex++];
                    tableaus[i].Add(card);
                    card.OnClicked += OnCardClicked;
                }
            }
        }

        for (int i = 4; i < 8; i++)
        {
            for (int j = 0; j < 6; j++)
            {
                if (cardIndex < deck.Count)
                {
                    CardController card = deck[cardIndex++];
                    tableaus[i].Add(card);
                    card.OnClicked += OnCardClicked;
                }
            }
        }

        // Place all cards stacked at deckOrigin, facedown, parented to deckParent
        foreach (var c in deck)
        {
            if (deckParent != null)
                c.transform.SetParent(deckParent, worldPositionStays: true);
            else
                c.transform.SetParent(this.transform, worldPositionStays: true);

            if (deckOrigin != null)
                c.transform.position = deckOrigin.position;
            else
                c.transform.position = Vector3.zero;

            // ensure start face-down and no animation
            c.SetFaceUpImmediate(false);

            // disable collider during deal so player can't click cards while they fly around
            var col = c.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }

        // Run the animated deal
        skipDealTextObject?.SetActive(true);
        isDealing = true;
        fastForwardRequested = false;
        yield return StartCoroutine(DealAllTableauCards());
        isDealing = false;
        fastForwardRequested = false;
        skipDealTextObject?.SetActive(false);

        // After dealing finishes, enable colliders so cards are interactive
        foreach (var c in deck)
        {
            var col = c.GetComponent<Collider2D>();
            if (col != null) col.enabled = true;
        }
    }

    IEnumerator LiftCard(CardController card, float targetY = 0f)
    {
        Vector3 startPos = card.transform.position;
        Vector3 targetPos = new Vector3(startPos.x, targetY, startPos.z);

        float elapsed = 0f;
        float duration = 0.1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            card.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        card.transform.position = targetPos;
    }

    // Deal coroutine: flip at origin then move each card to its tableau slot.
    IEnumerator DealAllTableauCards()
    {
        // Iterate tableau columns left-to-right
        for (int col = 0; col < 8; col++)
        {
            int cardCount = tableaus[col].Count;
            for (int idx = 0; idx < cardCount; idx++)
            {
                CardController card = tableaus[col][idx];

                if (card == null) continue;
                if (tableauPositions == null || tableauPositions.Length <= col) continue;

                Transform destTransform = tableauPositions[col];
                int cardIndexInDest = idx;

                // 1) Lift card off deck
                // play flip sound
                if (card.audioSource != null && card.flipCardSoundClip != null)
                    card.audioSource.PlayOneShot(card.flipCardSoundClip);

                if (fastForwardRequested)
                {
                    // Instant lift + instant flip (no animation)
                    Vector3 liftTarget = new Vector3(card.transform.position.x, 0f, card.transform.position.z);
                    card.transform.position = liftTarget;
                    card.SetFaceUpImmediate(true);
                }
                else
                {
                    // animated lift, then animated flip
                    yield return StartCoroutine(LiftCard(card, 0f));
                    // start flip animation and wait its duration
                    card.SetFaceUpAnimated(true, flipDuration);
                    yield return new WaitForSeconds(flipDuration);
                }

                // 3) Move to tableau
                if (fastForwardRequested)
                {
                    // Instant move: parent to dest and snap into final local position
                    Vector3 targetPos = destTransform.position;
                    if (cardIndexInDest > 0)
                        targetPos += new Vector3(0, -0.3f * cardIndexInDest, -0.01f * cardIndexInDest);
                    else
                        targetPos += new Vector3(0, 0, -0.01f * cardIndexInDest);

                    card.transform.SetParent(destTransform, worldPositionStays: true);
                    card.transform.position = targetPos;
                    card.transform.localPosition = new Vector3(0, -0.3f * cardIndexInDest, -0.01f * cardIndexInDest);
                }
                else
                {
                    // animated move
                    yield return StartCoroutine(AnimateCardMove(card, destTransform, cardIndexInDest));
                }

                // place sound if present
                if (card.audioSource != null && card.placeCardSoundClip != null)
                    card.audioSource.PlayOneShot(card.placeCardSoundClip);

                // small delay between cards (skip if fast-forward requested)
                if (!fastForwardRequested && dealDelay > 0f)
                    yield return new WaitForSeconds(dealDelay);
            }
        }
    }


    void TriggerInvalidMoveFor(CardController oldSelected)
    {
        // Play card-local feedback if possible
        if (oldSelected != null)
        {
            oldSelected.PlayInvalidFeedback(invalidFlashDuration);
            // schedule deselect of that specific card after the flash
            StartCoroutine(DelayedDeselectOld(oldSelected, invalidFlashDuration));
        }
        OnInvalidMove?.Invoke();
    }

    IEnumerator DelayedDeselectOld(CardController oldSelected, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Only clear if that same card is still selected (player might have changed selection meanwhile)
        if (selectedCard == oldSelected)
        {
            selectedCard.SetHighlight(false);
            selectedCard = null;
        }
    }

    void OnCardClicked(CardController card)
    {
        if (gameWon) return;

        if (selectedCard == null)
        {
            // Try to select this card
            if (CanSelectCard(card))
            {
                selectedCard = card;
                selectedCard.SetHighlight(true);
            }
        }
        else if (selectedCard == card)
        {
            // Deselect
            selectedCard.SetHighlight(false);
            selectedCard = null;
        }
        else
        {
            // Attempt move
            if (TryMoveCard(selectedCard, card))
            {
                selectedCard.SetHighlight(false);
                selectedCard = null;
                CheckWinCondition();
            }
            else
            {
                // Invalid move, trigger feedback and deselect
                var oldSelected = selectedCard;
                TriggerInvalidMoveFor(oldSelected);

                if (oldSelected != null)
                {
                    oldSelected.SetHighlight(false);
                }
                selectedCard = null;
            }
        }
    }


    bool CanSelectCard(CardController card)
    {
        // Check if card is in a valid location and not blocked
        return IsCardOnTop(card) && !IsCardBlocked(card);
    }

    bool IsCardOnTop(CardController card)
    {
        // Check freecells
        for (int i = 0; i < 4; i++)
        {
            if (freecells[i].Count > 0 && freecells[i][freecells[i].Count - 1] == card)
                return true;
        }

        // Check foundations
        for (int i = 0; i < 4; i++)
        {
            if (foundations[i].Count > 0 && foundations[i][foundations[i].Count - 1] == card)
                return true;
        }

        // Check tableaus
        for (int i = 0; i < 8; i++)
        {
            if (tableaus[i].Count > 0 && tableaus[i][tableaus[i].Count - 1] == card)
                return true;
        }

        return false;
    }

    bool IsCardBlocked(CardController card)
    {
        // Check all containers for this card
        for (int i = 0; i < 8; i++)
        {
            int index = tableaus[i].IndexOf(card);
            if (index != -1 && index < tableaus[i].Count - 1)
                return true; // Cards above it exist
        }
        return false;
    }

    bool TryMoveCard(CardController card, CardController targetCard)
    {
        if (card == null) return false;

        if (targetCard != null)
        {
            if (TryMoveToTableau(card, targetCard))
                return true;

            return false;
        }

        if (TryMoveToFoundation(card)) return true;
        if (TryMoveToFreecell(card)) return true;

        return false;
    }

    bool TryMoveToFreecell(CardController card)
    {
        // Find first empty freecell
        for (int i = 0; i < 4; i++)
        {
            if (freecells[i].Count == 0)
            {
                MoveCard(card, freecells[i], freecellPositions[i]);
                return true;
            }
        }
        return false;
    }

    bool TryMoveToFoundation(CardController card)
    {
        int suitIndex = (int)card.Suit;

        // Check if card can go on foundation
        if (foundations[suitIndex].Count == 0)
        {
            // Foundation empty, must be Ace
            if (card.Rank != 1) return false;
        }
        else
        {
            // Must be one rank higher and same suit
            CardController topCard = foundations[suitIndex][foundations[suitIndex].Count - 1];
            if (card.Suit != topCard.Suit || card.Rank != topCard.Rank + 1)
                return false;
        }

        MoveCard(card, foundations[suitIndex], foundationPositions[suitIndex]);
        return true;
    }

    bool TryMoveToTableau(CardController card, CardController targetCard)
    {
        // Find which tableau the target card is in
        for (int i = 0; i < 8; i++)
        {
            if (tableaus[i].Count == 0) continue;
            if (tableaus[i][tableaus[i].Count - 1] != targetCard)
                continue;

            // Found target tableau
            if (CanPlaceOnTableau(card, targetCard))
            {
                MoveCard(card, tableaus[i], tableauPositions[i]);
                return true;
            }
        }
        return false;
    }

    bool CanPlaceOnTableau(CardController card, CardController targetCard)
    {
        // Must be one rank lower and opposite color
        if (card.Rank != targetCard.Rank - 1)
            return false;

        return card.IsRed != targetCard.IsRed;
    }

    void MoveCard(CardController card, List<CardController> destination, Transform destTransform)
    {
        // Remove from current location
        for (int i = 0; i < 4; i++)
        {
            if (freecells[i].Remove(card)) break;
        }
        for (int i = 0; i < 4; i++)
        {
            if (foundations[i].Remove(card)) break;
        }
        for (int i = 0; i < 8; i++)
        {
            if (tableaus[i].Remove(card)) break;
        }

        // Add to destination
        destination.Add(card);

        // Animate
        card.audioSource.PlayOneShot(card.placeCardSoundClip);
        StartCoroutine(AnimateCardMove(card, destTransform, destination.Count - 1));
    }

    IEnumerator AnimateCardMove(CardController card, Transform destTransform, int cardIndexInDest)
    {
        Vector3 targetPos = destTransform.position;
        if (cardIndexInDest > 0)
        {
            targetPos += new Vector3(0, -0.3f * cardIndexInDest, -0.01f * cardIndexInDest);
        }
        else
        {
            targetPos += new Vector3(0, 0, -0.01f * cardIndexInDest);
        }

        // Parent to destination but preserve world position so Lerp starts from current world pos
        card.transform.SetParent(destTransform, worldPositionStays: true);

        Vector3 startPos = card.transform.position;
        float elapsed = 0f;
        float duration = Vector3.Distance(startPos, targetPos) / cardMoveSpeed;
        duration = Mathf.Max(0.2f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            card.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        card.transform.position = targetPos;
        card.transform.localPosition = new Vector3(0, -0.3f * cardIndexInDest, -0.01f * cardIndexInDest);
    }

    void CheckWinCondition()
    {
        // Win if all foundations have 13 cards
        for (int i = 0; i < 4; i++)
        {
            if (foundations[i].Count != 13)
                return;
        }

        gameWon = true;
        OnGameWon?.Invoke();
        StartCoroutine(WinCelebration());
    }

    public void ForceWin()
    {
        if (gameWon) return;
        gameWon = true;
        OnGameWon?.Invoke();
        StartCoroutine(WinCelebration());
    }

    public void TryMoveToPosition(Transform targetPosition)
    {
        if (selectedCard == null) return;

        // Check if it's a freecell position
        for (int i = 0; i < 4; i++)
        {
            if (freecellPositions[i] == targetPosition)
            {
                if (freecells[i].Count == 0)
                {
                    MoveCard(selectedCard, freecells[i], freecellPositions[i]);
                    selectedCard.SetHighlight(false);
                    selectedCard = null;
                    CheckWinCondition();
                    return;
                }
            }
        }

        // Check if it's a foundation position
        for (int i = 0; i < 4; i++)
        {
            if (foundationPositions[i] == targetPosition)
            {
                if (TryMoveToFoundation(selectedCard))
                {
                    selectedCard.SetHighlight(false);
                    selectedCard = null;
                    CheckWinCondition();
                    return;
                }
            }
        }

        // Check if it's a tableau position
        for (int i = 0; i < 8; i++)
        {
            if (tableauPositions[i] == targetPosition)
            {
                if (tableaus[i].Count == 0)
                {
                    // Empty tableau - King only
                    if (selectedCard.Rank == 13)
                    {
                        MoveCard(selectedCard, tableaus[i], tableauPositions[i]);
                        selectedCard.SetHighlight(false);
                        selectedCard = null;
                        CheckWinCondition();
                        return;
                    }
                }
                else
                {
                    // Tableau with cards
                    CardController topCard = tableaus[i][tableaus[i].Count - 1];
                    if (CanPlaceOnTableau(selectedCard, topCard))
                    {
                        MoveCard(selectedCard, tableaus[i], tableauPositions[i]);
                        selectedCard.SetHighlight(false);
                        selectedCard = null;
                        CheckWinCondition();
                        return;
                    }
                }
            }
        }

        // Invalid move
        TriggerInvalidMoveFor(selectedCard);
    }

    public void RestartGame()
    {
        gameWon = false;
        selectedCard = null;

        // Destroy all card objects
        foreach (Transform pos in tableauPositions)
        {
            foreach (Transform child in pos)
            {
                Destroy(child.gameObject);
            }
        }
        foreach (Transform pos in freecellPositions)
        {
            foreach (Transform child in pos)
            {
                Destroy(child.gameObject);
            }
        }
        foreach (Transform pos in foundationPositions)
        {
            foreach (Transform child in pos)
            {
                Destroy(child.gameObject);
            }
        }

        // Reinitialize game
        InitializeContainers();
        StartCoroutine(GenerateAndDealDeckCoroutine());
    }

    IEnumerator WinCelebration()
    {
        VictoryTextObject?.SetActive(true);
        mainAudio?.PlayOneShot(winCelebrationClip);

        CardController[] allCards = FindObjectsByType<CardController>(FindObjectsSortMode.None);

        if (allCards == null || allCards.Length == 0) yield break;

        Camera cam = Camera.main;

        // Disable interaction on cards and un-parent them so they can move freely
        foreach (var c in allCards)
        {
            var col = c.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            c.transform.SetParent(null, worldPositionStays: true);

            c.PlayWinParticles();

            c.StartContinuousFlip(winFlipDuration);
        }

        List<Coroutine> moveCoroutines = new List<Coroutine>();
        foreach (var c in allCards)
        {
            // compute a random viewport coordinate inside margins
            float vx = Random.Range(winSpawnMargin, 1f - winSpawnMargin);
            float vy = Random.Range(winSpawnMargin, 1f - winSpawnMargin);

            // convert to world position at the same z as the card
            float zDistance = Mathf.Abs(cam.transform.position.z - c.transform.position.z);
            Vector3 viewportPoint = new Vector3(vx, vy, zDistance);
            Vector3 worldTarget = cam.ViewportToWorldPoint(viewportPoint);
            worldTarget.z = c.transform.position.z; // preserve original z (important for 2D)

            float duration = Random.Range(winMoveMinDuration, winMoveMaxDuration);

            // Optionally stagger starts a little for nicer spread
            float startDelay = Random.Range(0f, 0.25f);

            moveCoroutines.Add(StartCoroutine(AnimateMoveToPosition(c, worldTarget, duration, startDelay)));
        }

        // Wait for all moves to complete
        foreach (var mc in moveCoroutines)
            yield return mc;

        yield break;
    }

    IEnumerator AnimateMoveToPosition(CardController card, Vector3 targetWorldPos, float duration, float delay = 0f)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        Vector3 start = card.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // optional ease-out curve
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            card.transform.position = Vector3.Lerp(start, targetWorldPos, ease);
            yield return null;
        }

        card.transform.position = targetWorldPos;
    }
}
