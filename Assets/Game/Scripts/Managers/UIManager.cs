// ============================================================================
// UI MANAGER - Tüm panel geçişlerini yönetir (DOTween ile)
// ✅ Enemy Render Panel control (visible during Draft only)
// ❌ REMOVED: Skill Selection Panel
// ============================================================================

using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

public class UIManager : MonoBehaviour
{
    [Inject] CurrencyManager currencyManager;
    [Inject] AudioManager audioManager;

    [Header("Main Panels")]
    [SerializeField] GameObject menuPanel;
    [SerializeField] GameObject draftPanel;
    [SerializeField] GameObject battlePanel;
    [SerializeField] GameObject battleCam;
    [SerializeField] GameObject rewardPanel;
    [SerializeField] GameObject losePanel;
    [SerializeField] GameObject chestPanel;
    [SerializeField] GameObject progressPanel;
    // ❌ REMOVED: [SerializeField] GameObject skillSelectionPanel;
    [SerializeField] GameObject tutorialPanel;

    [Header("Persistent Panels")]
    [SerializeField] GameObject enemyRenderPanel; // ✅ Visible during draft only

    [Header("UI Elements")]
    [SerializeField] TextMeshProUGUI goldText;
    [SerializeField] TextMeshProUGUI turnText;

    [Header("Wave Indicator")]
    [SerializeField] Transform waveIndicator;
    [SerializeField] TextMeshProUGUI waveText;


    [Header("Battle Result")]
    [SerializeField] BattleResultUI battleResultUI;

    internal bool battleDraft;
    // UIManager.Awake() içinde:
    void Awake()
    {
        DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
        DOTween.defaultAutoPlay = AutoPlay.All;
        DOTween.defaultAutoKill = true;
    }
    private void Start()
    {
        HideAllPanels();
    }
    public void Retry()
    {
        SceneManager.LoadScene(1);
    }
    private void OnEnable()
    {
        EventManager.onGameStateChange += OnGameStateChange;
        EventManager.onGoldChanged += UpdateGoldUI;
        EventManager.onTurnChange += UpdateTurnUI;
    }

    private void OnDisable()
    {
        EventManager.onGameStateChange -= OnGameStateChange;
        EventManager.onGoldChanged -= UpdateGoldUI;
        EventManager.onTurnChange -= UpdateTurnUI;
    }

    private void HideAllPanels()
    {
        menuPanel.SetActive(false);
        draftPanel.SetActive(false);
        battlePanel.SetActive(false);
        battleCam.SetActive(false);
        rewardPanel.SetActive(false);
        losePanel.SetActive(false);
        chestPanel.SetActive(false);
        progressPanel.SetActive(false);
        // ❌ REMOVED: skillSelectionPanel.SetActive(false);
        tutorialPanel.SetActive(false);
        enemyRenderPanel.SetActive(false);
    }

    private void OnGameStateChange(GameState newState)
    {
        switch (newState)
        {
            case GameState.Draft:
                ShowDraftPanel();
                break;
        }
    }

    // ===== PANEL SHOW METHODS =====

    public void ShowMainMenu()
    {
        HideAllPanels();
        menuPanel.SetActive(true);
        AnimatePanelIn(menuPanel.transform);

        // ✅ Hide enemy render in main menu
        SetEnemyRenderPanelVisibility(false);
    }

    public void ShowDraftPanel()
    {
        HideAllPanels();

        // ✅ Show enemy render during draft
        SetEnemyRenderPanelVisibility(true);

        // ✅ Delay only if coming from battle
        if (battleDraft)
        {
            battleDraft = false;
            Invoke(nameof(DelayShowDraftPanel), 1.5f);
        }
        else
        {
            DelayShowDraftPanel(); // Show immediately
        }
    }

    void DelayShowDraftPanel()
    {
        draftPanel.SetActive(true);
        AnimatePanelIn(draftPanel.transform);
        Debug.Log("✅ Draft panel shown");
    }
    public void ShowBattlePanel()
    {
        HideAllPanels();
        battlePanel.SetActive(true);
        battleCam.SetActive(true);
        AnimatePanelIn(battlePanel.transform);

        // ✅ Hide enemy render during battle
        SetEnemyRenderPanelVisibility(false);
    }

    public void ShowRewardPanel()
    {
        HideAllPanels();
        rewardPanel.SetActive(true);
        AnimatePanelIn(rewardPanel.transform);

        // ✅ Hide enemy render in reward
        SetEnemyRenderPanelVisibility(false);

        Taptic.Success();
        audioManager?.Play("Win");
    }
    public void ShowLosePanel()
    {
        HideAllPanels();
        losePanel.SetActive(true);
        AnimatePanelIn(losePanel.transform);

        // ✅ Hide enemy render in reward
        SetEnemyRenderPanelVisibility(false);

        Taptic.Failure();
    }

    public void ShowChestPanel()
    {
        HideAllPanels();
        chestPanel.SetActive(true);
        AnimatePanelIn(chestPanel.transform);

        // ✅ Hide enemy render in chest
        SetEnemyRenderPanelVisibility(false);
    }

    public void ShowProgressPanel()
    {
        HideAllPanels();
        progressPanel.SetActive(true);
        AnimatePanelIn(progressPanel.transform);

        // ✅ Hide enemy render in progress
        SetEnemyRenderPanelVisibility(false);
    }

    // ❌ REMOVED: ShowSkillSelection()

    public void ShowTutorialPanel()
    {
        HideAllPanels();
        tutorialPanel.SetActive(true);
        AnimatePanelIn(tutorialPanel.transform);

        // ✅ Hide enemy render in tutorial
        SetEnemyRenderPanelVisibility(false);
    }

    // ===== ENEMY RENDER PANEL CONTROL =====

    /// <summary>
    /// ✅ Control Enemy Render Panel visibility
    /// Show during: Draft ONLY
    /// Hide during: MainMenu, Battle, Reward, Chest, Progress, Tutorial
    /// </summary>
    private void SetEnemyRenderPanelVisibility(bool isVisible)
    {
        if (enemyRenderPanel == null)
        {
            Debug.LogWarning("⚠️ Enemy Render Panel not assigned in UIManager!");
            return;
        }

        enemyRenderPanel.SetActive(isVisible);

        if (isVisible)
        {
            Debug.Log("👁️ Enemy Render Panel: VISIBLE");
        }
        else
        {
            Debug.Log("🙈 Enemy Render Panel: HIDDEN");
        }
    }

    /// <summary>
    /// ✅ Public method to manually control enemy render panel (if needed)
    /// </summary>
    public void ShowEnemyRenderPanel()
    {
        SetEnemyRenderPanelVisibility(true);
    }

    public void HideEnemyRenderPanel()
    {
        SetEnemyRenderPanelVisibility(false);
    }

    // ===== WAVE INDICATOR =====

    public void ShowWaveIndicator(int waveNumber)
    {
        waveText.text = $"WAVE {waveNumber}";

        waveIndicator.localScale = new Vector3(1, 0, 1);
        waveIndicator.gameObject.SetActive(true);

        waveIndicator.DOScale(Vector3.one, 0.4f)
            .SetEase(Ease.OutBack)
            .SetDelay(0.5f)
            .OnComplete(() =>
            {
                waveIndicator.DOScale(new Vector3(1, 0, 1), 0.4f)
                    .SetEase(Ease.InBack)
                    .SetDelay(2f);
            });
    }
    // Add these methods:

    public void ShowBattleResultUI(bool playerWon)
    {
        battleDraft = true;
        if (battleResultUI != null)
        {
            battleResultUI.ShowResult(playerWon);
            Debug.Log($"📊 Battle result UI shown: {(playerWon ? "VICTORY" : "DEFEAT")}");
        }
        else
        {
            Debug.LogWarning("⚠️ BattleResultUI not assigned in UIManager!");
        }
    }

    public void HideBattleResultUI()
    {
        if (battleResultUI != null)
        {
            battleResultUI.HideResult();
            Debug.Log("👋 Battle result UI hidden");
        }
    }
    // ===== UI UPDATE METHODS =====

    private void UpdateGoldUI(int goldAmount)
    {
        if (goldText != null)
        {
            goldText.text = goldAmount.ToString();

            // Bounce effect
            goldText.transform.DOScale(1.2f, 0.1f)
                .OnComplete(() => goldText.transform.DOScale(1f, 0.1f));
        }
    }

    private void UpdateTurnUI(int turnNumber)
    {
        if (turnText != null)
        {
            turnText.text = $"Turn {turnNumber}/{GameConstants.TOTAL_TURNS}";
        }
    }

    // ===== ANIMATION HELPERS =====

    private void AnimatePanelIn(Transform panel)
    {
        // Mobil için önce kesin scale set et
        panel.localScale = Vector3.one;

#if UNITY_EDITOR
        // Sadece Editor'de animasyon
        panel.localScale = Vector3.zero;
        panel.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
#endif
    }

    private void AnimatePanelOut(Transform panel, System.Action onComplete = null)
    {
        panel.DOScale(Vector3.zero, 0.2f)
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    // ===== BUTTON CALLBACKS =====

    public void OnStartGameButton()
    {
        Taptic.Light();
        EventManager.OnGameStateChange(GameState.Draft);
    }

    public void OnMainMenuButton()
    {
        Taptic.Light();
        EventManager.OnGameStateChange(GameState.MainMenu);
    }
}