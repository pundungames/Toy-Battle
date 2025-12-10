// ============================================================================
// GAME MANAGER - Ana oyun state machine'i
// ✅ AI Turn integration - Player seçim yaptıktan sonra AI seçim yapar
// ✅ Respawn Previous Units - Battle sonrası karakterler tam canla geri gelir
// ============================================================================

using UnityEngine;
using Zenject;

public class GameManager : MonoBehaviour
{
    [Inject] UIManager uiManager;
    [Inject] DraftCardManager draftCardManager;
    [Inject] BattleManager battleManager;
    [Inject] CurrencyManager currencyManager;
    [Inject] TutorialController tutorialController;
    [Inject] AITurnManager aiTurnManager;
    [Inject] GridManager gridManager; // ✅ ADDED

    [Header("Game State")]
    [SerializeField] internal GameState currentState;
    [SerializeField] internal int currentTurn = 1;
    [SerializeField] internal int playerWins = 0;
    [SerializeField] internal bool isTutorial = false;

    [Header("Turn Control")]
    [SerializeField] private bool isPlayerTurnComplete = false;
    [SerializeField] private bool isAITurnComplete = false;

    private void Start()
    {
        InitializeGame();
    }

    private void OnEnable()
    {
        EventManager.onCardSelected += OnPlayerCardSelected;
        EventManager.onDraftComplete += OnBothTurnsComplete;
        EventManager.onBattleComplete += OnBattleComplete;
    }

    private void OnDisable()
    {
        EventManager.onCardSelected -= OnPlayerCardSelected;
        EventManager.onDraftComplete -= OnBothTurnsComplete;
        EventManager.onBattleComplete -= OnBattleComplete;
    }

    private void InitializeGame()
    {
        // Tutorial check
        if (isTutorial && PlayerPrefs.GetInt("TutorialComplete", 0) == 0)
        {
            isTutorial = true;
            tutorialController.StartTutorial();
        }
        else
        {
            isTutorial = false;
            ChangeState(GameState.MainMenu);
        }
    }

    public void StartButton()
    {
        currentTurn = 1;
        ChangeState(GameState.Draft);
    }

    public void ChangeState(GameState newState)
    {
        currentState = newState;
        EventManager.OnGameStateChange(newState);

        switch (newState)
        {
            case GameState.MainMenu:
                uiManager.ShowMainMenu();
                break;

            case GameState.Draft:
                StartDraftPhase();
                break;

            case GameState.Battle:
                StartBattlePhase();
                break;

            case GameState.Reward:
                uiManager.ShowRewardPanel();
                break;

            case GameState.Chest:
                uiManager.ShowChestPanel();
                break;

            case GameState.Progress:
                uiManager.ShowProgressPanel();
                break;
        }
    }

    private void StartDraftPhase()
    {
        isPlayerTurnComplete = false;
        isAITurnComplete = false;

        Debug.Log($"🎴 Starting Draft Phase - Turn {currentTurn}");

        // ✅ Önceki karakterleri TAM CANLA geri getir (Turn 1 hariç)
        if (currentTurn > 1)
        {
            gridManager.RespawnPreviousUnits();
        }

        // Show UI
        uiManager.ShowDraftPanel();

        // Skill selection turns: 8, 16, 24
        if (currentTurn == 8 || currentTurn == 16 || currentTurn == 24)
        {
            uiManager.ShowSkillSelection();
        }
        else
        {
            // ✅ Direkt çağır
            OpenPlayerDraft();
        }
    }

    private void OpenPlayerDraft()
    {
        Debug.Log("🎴 OpenPlayerDraft() - Opening cards...");
        draftCardManager.Open(false);
    }

    // ===== PLAYER CARD SELECTED =====

    private void OnPlayerCardSelected(ToyUnitData unitData)
    {
        Debug.Log($"✅ Player selected: {unitData.toyName}");
        isPlayerTurnComplete = true;

        // Player seçim yaptı, şimdi AI'ın sırası
        StartAITurn();
    }

    // ===== START AI TURN =====

    private void StartAITurn()
    {
        Debug.Log("🤖 Starting AI turn...");
        aiTurnManager.StartAITurn();
    }

    // ===== BOTH TURNS COMPLETE =====

    private void OnBothTurnsComplete()
    {
        // AI turn complete olduğunda EventManager.OnDraftComplete() çağrılır
        isAITurnComplete = true;

        if (isPlayerTurnComplete && isAITurnComplete)
        {
            Debug.Log("✅ Both player and AI turns complete!");
            AdvanceTurn();
        }
    }

    // ===== ADVANCE TURN =====

    private void AdvanceTurn()
    {
        currentTurn++;
        EventManager.OnTurnChange(currentTurn);

        Debug.Log($"📊 Turn {currentTurn}/{GameConstants.TOTAL_TURNS}");

        // Battle turns: 5, 10, 15, 20, 25, 30
        if (currentTurn == 5 || currentTurn == 10 || currentTurn == 15 ||
            currentTurn == 20 || currentTurn == 25 || currentTurn == 30)
        {
            ChangeState(GameState.Battle);
        }
        else if (currentTurn > GameConstants.TOTAL_TURNS)
        {
            EndMatch();
        }
        else
        {
            // ✅ FIX: Yeni turn başlatmadan önce kısa delay
            Invoke(nameof(StartNextDraftTurn), 0.5f);
        }
    }

    private void StartNextDraftTurn()
    {
        ChangeState(GameState.Draft);
    }

    private void StartBattlePhase()
    {
        battleManager.StartBattle();
    }

    private void OnBattleComplete(bool playerWon)
    {
        if (playerWon)
        {
            playerWins++;
            currencyManager.UpdateCashAndSave(GameConstants.WIN_GOLD);
        }
        else
        {
            currencyManager.UpdateCashAndSave(GameConstants.LOSE_GOLD);
        }

        // ✅ Final battle mi?
        if (currentTurn == GameConstants.TOTAL_TURNS)
        {
            // MAÇ BİTTİ → Result ekranı
            ChangeState(GameState.Reward);
        }
        else
        {
            // DEVAMKE → Yeni turn
            AdvanceTurn();
        }
    }
    // RewardPanel'de "Continue" butonuna basınca:
    public void OnRewardContinue()
    {
        // Chest drop check
        if (Random.value < GameConstants.CHEST_DROP_CHANCE)
        {
            ChangeState(GameState.Chest);
        }
        else
        {
            ChangeState(GameState.Progress);
        }
    }
    private void EndMatch()
    {
        ChangeState(GameState.Reward);
    }

    public void StartNewGame()
    {
        currentTurn = 1;
        playerWins = 0;

        // ✅ Grid state'i tamamen sıfırla
        gridManager.ResetGridState();

        ChangeState(GameState.Draft);
    }
}