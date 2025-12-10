// ============================================================================
// GAME MANAGER - Ana oyun state machine'i
// ✅ FIXED: Battle state management
// ✅ FIXED: Respawn timing - AFTER battle complete, BEFORE next turn
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
    [Inject] GridManager gridManager;

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

    // ===== DRAFT PHASE =====

    private void StartDraftPhase()
    {
        isPlayerTurnComplete = false;
        isAITurnComplete = false;

        Debug.Log($"🎴 Starting Draft Phase - Turn {currentTurn}");

        uiManager.ShowDraftPanel();

        // Skill selection turns: 8, 16, 24
        if (currentTurn == 8 || currentTurn == 16 || currentTurn == 24)
        {
            uiManager.ShowSkillSelection();
        }
        else
        {
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
            Invoke(nameof(StartNextDraftTurn), 0.5f);
        }
    }

    private void StartNextDraftTurn()
    {
        ChangeState(GameState.Draft);
    }

    // ===== BATTLE PHASE =====

    private void StartBattlePhase()
    {
        battleManager.StartBattle();
    }

    // ===== BATTLE COMPLETE =====

    private void OnBattleComplete(bool playerWon)
    {
        Debug.Log($"⚔️ Battle complete! Winner: {(playerWon ? "PLAYER" : "ENEMY")}");

        if (playerWon)
        {
            playerWins++;
            currencyManager.UpdateCashAndSave(GameConstants.WIN_GOLD);
        }
        else
        {
            currencyManager.UpdateCashAndSave(GameConstants.LOSE_GOLD);
        }

        // ✅ FIX: Clear scene objects IMMEDIATELY after battle
        gridManager.ClearSceneObjects();

        Debug.Log("🧹 Scene cleared, state preserved");

        // ✅ FIX: Final battle check
        if (currentTurn == GameConstants.TOTAL_TURNS)
        {
            // Match ended
            ChangeState(GameState.Reward);
        }
        else
        {
            // ✅ FIX: Respawn BEFORE advancing turn
            gridManager.RespawnPreviousUnits();
            Debug.Log("♻️ Units respawned for next draft");

            // Continue to next turn
            AdvanceTurn();
        }
    }

    // ===== REWARD =====

    public void OnRewardContinue()
    {
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

    // ===== NEW GAME =====

    public void StartNewGame()
    {
        currentTurn = 1;
        playerWins = 0;

        // Reset grid completely
        gridManager.ResetGridState();

        ChangeState(GameState.Draft);
    }
}