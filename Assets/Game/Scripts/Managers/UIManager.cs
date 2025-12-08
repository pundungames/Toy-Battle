// ============================================================================
// UI MANAGER - Tüm panel geçiþlerini yönetir (DOTween ile)
// ============================================================================

using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Zenject.Asteroids;

public class UIManager : MonoBehaviour
{
    [Inject] CurrencyManager currencyManager;
    [Inject] AudioManager audioManager;

    [Header("Main Panels")]
    [SerializeField] GameObject menuPanel;
    [SerializeField] GameObject draftPanel;
    [SerializeField] GameObject battlePanel;
    [SerializeField] GameObject rewardPanel;
    [SerializeField] GameObject chestPanel;
    [SerializeField] GameObject progressPanel;
    [SerializeField] GameObject skillSelectionPanel;
    [SerializeField] GameObject tutorialPanel;

    [Header("UI Elements")]
    [SerializeField] TextMeshProUGUI goldText;
    [SerializeField] TextMeshProUGUI turnText;

    [Header("Wave Indicator")]
    [SerializeField] Transform waveIndicator;
    [SerializeField] TextMeshProUGUI waveText;

    [SerializeField] GameState gameState;

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
        rewardPanel.SetActive(false);
        chestPanel.SetActive(false);
        progressPanel.SetActive(false);
        skillSelectionPanel.SetActive(false);
        tutorialPanel.SetActive(false);
    }
    public void ModeChangeButton()
    {
        if (gameState == GameState.Draft) EventManager.OnGameStateChange(GameState.Battle);
        else EventManager.OnGameStateChange(GameState.Draft);
    }
    private void OnGameStateChange(GameState newState)
    {
        gameState = newState;
        // State deðiþimlerinde otomatik panel gösterimi yapýlabilir
    }

    // ===== PANEL SHOW METHODS =====

    public void ShowMainMenu()
    {
        HideAllPanels();
        menuPanel.SetActive(true);
        AnimatePanelIn(menuPanel.transform);
    }

    public void ShowDraftPanel()
    {
        HideAllPanels();
        draftPanel.SetActive(true);
        AnimatePanelIn(draftPanel.transform);
    }

    public void ShowBattlePanel()
    {
        HideAllPanels();
        battlePanel.SetActive(true);
        AnimatePanelIn(battlePanel.transform);
    }

    public void ShowRewardPanel()
    {
        HideAllPanels();
        rewardPanel.SetActive(true);
        AnimatePanelIn(rewardPanel.transform);

        Taptic.Success();
        audioManager?.Play("Win");
    }

    public void ShowChestPanel()
    {
        HideAllPanels();
        chestPanel.SetActive(true);
        AnimatePanelIn(chestPanel.transform);
    }

    public void ShowProgressPanel()
    {
        HideAllPanels();
        progressPanel.SetActive(true);
        AnimatePanelIn(progressPanel.transform);
    }

    public void ShowSkillSelection()
    {
        HideAllPanels();
        skillSelectionPanel.SetActive(true);
        AnimatePanelIn(skillSelectionPanel.transform);
    }

    public void ShowTutorialPanel()
    {
        HideAllPanels();
        tutorialPanel.SetActive(true);
        AnimatePanelIn(tutorialPanel.transform);
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