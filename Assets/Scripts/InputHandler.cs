using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleClick();
        }
    }

    void HandleClick()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

        if (hit.collider != null)
        {
            // Try to find a card
            CardController card = hit.collider.GetComponent<CardController>();
            if (card != null)
            {
                card.SimulateClick();
                return;
            }

            // Try to find a clickable position
            ClickablePosition position = hit.collider.GetComponent<ClickablePosition>();
            if (position != null)
            {
                position.OnClicked();
                return;
            }
        }
    }
}
