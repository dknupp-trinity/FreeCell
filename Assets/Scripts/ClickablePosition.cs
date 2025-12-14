using UnityEngine;

/// <summary>
/// Makes a position clickable for moving cards to freecells, foundations, or empty tableau spots.
/// Place this on empty GameObjects that represent card positions.
/// </summary>
public class ClickablePosition : MonoBehaviour
{
    [SerializeField] GameManager gameManager;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        // Add a sprite renderer for visibility (optional, can be very transparent)
        if (GetComponent<SpriteRenderer>() == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.color = new Color(1, 1, 1, 0.1f); // Almost invisible placeholder
        }
        else
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Add a box collider if missing
        if (GetComponent<BoxCollider2D>() == null)
        {
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1, 1.5f); // Standard card size
        }
    }

    public void OnClicked()
    {
        if (gameManager != null)
        {
            gameManager.TryMoveToPosition(this.transform);
        }
    }
}
