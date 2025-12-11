// ============================================================================
// UI MANAGER - Tüm panel geçişlerini yönetir (DOTween ile)
// ✅ NEW: Enemy Render Panel control (visible during Draft & Skill Selection)
// ============================================================================

using DG.Tweening;
using TMPro;
using UnityEngine;
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
    [SerializeField] GameObject chestPanel;
    [SerializeField] GameObject progressPanel;
    [SerializeField] GameObject skillSelectionPanel;
    [SerializeField] GameObject tutorialPanel;

    [Header("Persistent Panels")]
    [SerializeField] GameObject enemyRenderPanel; // ✅ NEW: Always visible during draft/skill

    [Header("UI Elements")]
    [SerializeField] TextMeshProUGUI goldText;
    [SerializeField] TextMeshProUGUI turnText;

    [Header("Wave Indicator")]
    [SerializeField] Transform waveIndicator;
    [SerializeField] TextMeshProUGUI waveText;

    private void Start()
    {
        HideAllPanels();
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
        chestPanel.SetActive(false);
        progressPanel.SetActive(false);
        skillSelectionPanel.SetActive(false);
        tutorialPanel.SetActive(false);
        enemyRenderPanel.SetActive(false); // ✅ Start hidden
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
        draftPanel.SetActive(true);

        // ✅ DraftCardManager'ı aktif et
        DraftCardManager draftManager = draftPanel.GetComponentInChildren<DraftCardManager>(true);
        if (draftManager != null)
        {
            draftManager.gameObject.SetActive(true);
        }

        AnimatePanelIn(draftPanel.transform);

        // ✅ Show enemy render during draft
        SetEnemyRenderPanelVisibility(true);
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

    public void ShowSkillSelection()
    {
        HideAllPanels();
        skillSelectionPanel.SetActive(true);
        AnimatePanelIn(skillSelectionPanel.transform);

        // ✅ Show enemy render during skill selection
        SetEnemyRenderPanelVisibility(true);
    }

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
    /// Show during: Draft, Skill Selection
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
        panel.localScale = Vector3.zero;
        panel.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
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