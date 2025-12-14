using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject rulesPanel;
    [SerializeField] GameObject stylePanel;

    [Header("Buttons")]
    [SerializeField] Button startGameButton;
    [SerializeField] Button rulesButton;
    [SerializeField] Button styleButton;
    [SerializeField] Button closeRulesButton;
    [SerializeField] Button closeStyleButton;

    [Header("Deck selection (Toggles)")]
    [Tooltip("Names of deck folders under Resources/Decks (e.g. 'Standard')")]
    [SerializeField] string[] deckNames;
    [SerializeField] Toggle[] deckToggles;
    [SerializeField] ToggleGroup deckToggleGroup;

    [Header("Background selection (Toggles)")]
    [SerializeField] string[] backgroundNames;
    [SerializeField] Sprite[] backgroundSprites;      // same length as backgroundNames
    [SerializeField] Toggle[] backgroundToggles;
    [SerializeField] Image menuBackgroundPreview;

    [Header("Startup")]
    [SerializeField] string gameSceneName = "Game";

    void Start()
    {
        // Wire UI
        startGameButton.onClick.AddListener(OnStartGame);
        rulesButton.onClick.AddListener(ShowRules);
        styleButton.onClick.AddListener(ShowStyle);
        closeRulesButton.onClick.AddListener(HideRules);
        closeStyleButton.onClick.AddListener(HideStyle);

        SetupDeckToggles();
        SetupBackgroundToggles();

        ShowMainMenu();
    }

    void OnDestroy()
    {
        startGameButton.onClick.RemoveListener(OnStartGame);
        rulesButton.onClick.RemoveListener(ShowRules);
        styleButton.onClick.RemoveListener(ShowStyle);
        closeRulesButton.onClick.RemoveListener(HideRules);
        closeStyleButton.onClick.RemoveListener(HideStyle);
    }

    void ShowMainMenu()
    {
        rulesPanel.SetActive(false);
        stylePanel.SetActive(false);
    }

    public void OnStartGame()
    {
        if (string.IsNullOrEmpty(GameSettings.SelectedDeckName))
            GameSettings.SelectedDeckName = deckNames.Length > 0 ? deckNames[0] : "Standard";

        SceneManager.LoadScene(gameSceneName);
    }

    public void ShowRules() => rulesPanel.SetActive(true);
    public void HideRules() => rulesPanel.SetActive(false);
    public void ShowStyle() => stylePanel.SetActive(true);
    public void HideStyle() => stylePanel.SetActive(false);

    void SetupDeckToggles()
    {
        if (deckToggles == null || deckNames == null) return;

        // ensure group doesn't allow all off
        if (deckToggleGroup != null)
            deckToggleGroup.allowSwitchOff = false;

        for (int i = 0; i < deckToggles.Length; i++)
        {
            int idx = i;
            Toggle t = deckToggles[i];

            // Remove previous listeners if any
            t.onValueChanged.RemoveAllListeners();
            t.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                    SelectDeck(idx);
            });
        }

        // Initialize UI to match GameSettings (or default)
        int selectedIndex = Mathf.Max(0, System.Array.IndexOf(deckNames, GameSettings.SelectedDeckName));
        if (selectedIndex < 0 || selectedIndex >= deckToggles.Length) selectedIndex = 0;
        deckToggles[selectedIndex].isOn = true;
    }

    public void SelectDeck(int index)
    {
        if (index < 0 || index >= deckNames.Length) return;
        GameSettings.SelectedDeckName = deckNames[index];
        Debug.Log($"Selected deck: {GameSettings.SelectedDeckName}");
    }

    // --- Background toggles ---
    void SetupBackgroundToggles()
    {
        if (backgroundToggles == null || backgroundSprites == null) return;

        for (int i = 0; i < backgroundToggles.Length; i++)
        {
            int idx = i;
            Toggle t = backgroundToggles[i];

            t.onValueChanged.RemoveAllListeners();
            t.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                    SelectBackground(idx);
            });
        }

        // Initialize to current GameSettings selection if possible
        int initial = -1;
        if (!string.IsNullOrEmpty(GameSettings.SelectedBackgroundName))
            initial = System.Array.IndexOf(backgroundNames, GameSettings.SelectedBackgroundName);

        if (initial < 0 && GameSettings.SelectedBackgroundSprite != null)
        {
            // try to match by sprite name
            initial = System.Array.FindIndex(backgroundSprites, s => s != null && s.name == GameSettings.SelectedBackgroundSprite.name);
        }

        if (initial < 0) initial = 0;
        backgroundToggles[Mathf.Clamp(initial, 0, backgroundToggles.Length - 1)].isOn = true;
    }

    public void SelectBackground(int index)
    {
        if (index < 0 || index >= backgroundSprites.Length) return;

        GameSettings.SelectedBackgroundName = (backgroundNames != null && index < backgroundNames.Length) ? backgroundNames[index] : backgroundSprites[index].name;
        GameSettings.SelectedBackgroundSprite = backgroundSprites[index];

        // update preview immediately
        if (menuBackgroundPreview != null)
            menuBackgroundPreview.sprite = GameSettings.SelectedBackgroundSprite;

        Debug.Log($"Selected background: {GameSettings.SelectedBackgroundName}");
    }
}
