using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public enum Suit { Clubs, Diamonds, Hearts, Spades }

public class CardData
{
    public int rank;
    public Suit suit;
    public Sprite frontSprite;

    public CardData(int rank, Suit suit, Sprite sprite)
    {
        this.rank = rank;
        this.suit = suit;
        this.frontSprite = sprite;
    }

    public string DisplayName
    {
        get
        {
            string r = rank == 1 ? "A" :
                       rank == 11 ? "J" :
                       rank == 12 ? "Q" :
                       rank == 13 ? "K" :
                       rank.ToString();
            return $"{r} of {suit}";
        }
    }

    public bool IsRed => suit == Suit.Hearts || suit == Suit.Diamonds;
}

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class CardController : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] CardData cardData;
    [SerializeField] Sprite backSprite;


    [SerializeField] bool startFaceUp = false;
    [SerializeField] Color highlightColor = Color.green;

    [Header("Sounds")]
    public AudioSource audioSource;
    public AudioClip flipCardSoundClip;
    public AudioClip placeCardSoundClip;
    public AudioClip invalidMoveSoundClip;

    SpriteRenderer sr;
    bool faceUp;
    bool animating;
    Color originalColor;
    bool isHighlighted = false;

    public event System.Action<CardController> OnClicked;

    public int Rank => cardData != null ? cardData.rank : 0;
    public Suit Suit => cardData != null ? cardData.suit : Suit.Clubs;
    public bool IsRed => cardData != null && cardData.IsRed;
    public CardData Data => cardData;

    Coroutine continuousFlipCoroutine = null;
    ParticleSystem particleSystemInstance = null;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;
        faceUp = startFaceUp;
        UpdateSprite();

        particleSystemInstance = GetComponentInChildren<ParticleSystem>(includeInactive: true);
        if (particleSystemInstance != null)
        {
            particleSystemInstance.gameObject.SetActive(false);
        }
    }

    void UpdateSprite()
    {
        sr.sprite = faceUp ? cardData.frontSprite : backSprite;
    }

    public void SetCard(CardData data, Sprite back = null, bool showFace = false)
    {
        cardData = data;
        if (back != null) backSprite = back;
        faceUp = showFace;
        UpdateSprite();
    }

    public void SetFaceUpImmediate(bool show)
    {
        if (animating) return;
        faceUp = show;
        UpdateSprite();
    }

    public void Flip()
    {
        SetFaceUpAnimated(!faceUp);
    }

    public void SetFaceUpAnimated(bool show, float duration = 0.35f)
    {
        if (animating || faceUp == show) return;
        StartCoroutine(FlipRoutine(show, duration));
    }



    IEnumerator FlipRoutine(bool show, float duration)
    {
        animating = true;

        float half = duration * 0.5f;
        float t = 0f;

        Vector3 original = transform.localScale;
        Vector3 thin = new Vector3(original.x * 0.05f, original.y, original.z);

        // shrink
        while (t < half)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / half);
            transform.localScale = Vector3.Lerp(original, thin, p);
            yield return null;
        }

        // swap sprite at midpoint
        faceUp = show;
        UpdateSprite();

        // expand
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / half);
            transform.localScale = Vector3.Lerp(thin, original, p);
            yield return null;
        }

        transform.localScale = original;
        animating = false;
    }

    // flash coroutine handle so we don't stack multiple flashes
    Coroutine invalidFlashCoroutine = null;


    public void PlayInvalidFeedback(float flashDuration = 0.35f)
    {

        if (audioSource != null)
            audioSource.PlayOneShot(invalidMoveSoundClip);

        if (invalidFlashCoroutine != null)
            StopCoroutine(invalidFlashCoroutine);
        invalidFlashCoroutine = StartCoroutine(FlashInvalidRoutine(flashDuration));
    }

    IEnumerator FlashInvalidRoutine(float duration)
    {
        if (sr == null)
        {
            invalidFlashCoroutine = null;
            yield break;
        }

        sr.color = Color.red;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        sr.color = isHighlighted ? highlightColor : originalColor;

        invalidFlashCoroutine = null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClicked?.Invoke(this);
    }

    public void SimulateClick()
    {
        OnClicked?.Invoke(this);
    }

    public void SetHighlight(bool highlight)
    {
        isHighlighted = highlight;

        if (invalidFlashCoroutine != null)
            return;

        sr.color = isHighlighted ? highlightColor : originalColor;
    }

    // Particle & flipping effects for winning animation
    public void PlayWinParticles()
    {
        if (particleSystemInstance == null) return;

        // Enable the GO then play
        particleSystemInstance.gameObject.SetActive(true);
        particleSystemInstance.Play(true);
    }

    public void StopWinParticles()
    {
        if (particleSystemInstance == null) return;
        particleSystemInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particleSystemInstance.gameObject.SetActive(false);
    }

    public void StartContinuousFlip(float flipDuration = 0.35f)
    {
        // If already running, restart
        if (continuousFlipCoroutine != null)
            StopCoroutine(continuousFlipCoroutine);

        continuousFlipCoroutine = StartCoroutine(ContinuousFlipRoutine(flipDuration));
    }

    public void StopContinuousFlip()
    {
        if (continuousFlipCoroutine != null)
        {
            StopCoroutine(continuousFlipCoroutine);
            continuousFlipCoroutine = null;
        }

        SetFaceUpImmediate(true);
    }

    IEnumerator ContinuousFlipRoutine(float flipDuration)
    {
        while (true)
        {
            SetFaceUpAnimated(!faceUp, flipDuration);
            yield return new WaitForSeconds(flipDuration + 0.05f);
        }
    }

    public override string ToString() => cardData != null ? cardData.DisplayName : "Unassigned Card";
}
