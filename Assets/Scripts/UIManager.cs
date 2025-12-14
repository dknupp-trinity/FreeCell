using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject rulesPanel;

    [Header("Buttons")]
    [SerializeField] Button restartButton;
    [SerializeField] Button returnToMenuButton;
    [SerializeField] Button rulesButton;
    [SerializeField] Button closeRulesButton;
    [SerializeField] Button simulateWinButton;

    [Header("Other")]
    [SerializeField] Image tableBackgroundImage;
    [SerializeField] GameManager gameManager;
    [SerializeField] string mainMenuSceneName = "MainMenu";

    void Start()
    {
        ApplySelectedBackground();
        
        restartButton.onClick.AddListener(OnRestart);
        returnToMenuButton.onClick.AddListener(OnReturnToMenu);
        rulesButton.onClick.AddListener(ShowRules);
        closeRulesButton.onClick.AddListener(HideRules);
        simulateWinButton.onClick.AddListener(() => gameManager.ForceWin());

        if (gameManager != null)
        {
            gameManager.OnInvalidMove += ShowInvalidFeedback;
        }

        rulesPanel.SetActive(false);
    }

    void OnDestroy()
    {
        restartButton.onClick.RemoveListener(OnRestart);
        returnToMenuButton.onClick.RemoveListener(OnReturnToMenu);
        rulesButton.onClick.RemoveListener(ShowRules);
        closeRulesButton.onClick.RemoveListener(HideRules);
        simulateWinButton.onClick.RemoveListener(() => gameManager.ForceWin());

        if (gameManager != null)
        {
            gameManager.OnInvalidMove -= ShowInvalidFeedback;
        }
    }

    public void OnRestart()
    {
        gameManager.RestartGame();
    }

    public void OnReturnToMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void ShowRules() => rulesPanel.SetActive(true);
    public void HideRules() => rulesPanel.SetActive(false);


    void ShowInvalidFeedback()
    {
        Debug.Log("Invalid move attempted");
    }
    
    public void ApplySelectedBackground()
    {
        if (tableBackgroundImage == null) return;

        if (GameSettings.SelectedBackgroundSprite != null)
        {
            tableBackgroundImage.sprite = GameSettings.SelectedBackgroundSprite;
        }
        else if (!string.IsNullOrEmpty(GameSettings.SelectedBackgroundName))
        {
            // optional fallback: try load from Resources/Backgrounds/<name>
            Sprite s = Resources.Load<Sprite>($"Backgrounds/{GameSettings.SelectedBackgroundName}");
            if (s != null) tableBackgroundImage.sprite = s;
        }
    }
}
