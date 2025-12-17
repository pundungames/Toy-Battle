// ============================================================================
// GAME MANAGER - FIXED WITH HEALTH SYSTEM CHECK
// ✅ Checks if game is over before respawning
// ✅ 2 second delay before respawn (better visual)
// ✅ Stops game flow when health depleted
// ============================================================================

using System.Collections.Generic;
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
    [Inject] HealthSystem healthSystem; // ✅ INJECT HEALTH SYSTEM

    [Header("Game State")]
    [SerializeField] internal GameState currentState;
    [SerializeField] internal int currentTurn = 1;
    [SerializeField] internal int playerWins = 0;
    [SerializeField] internal bool isTutorial = false;

    [Header("Turn Control")]
    [SerializeField] private bool isPlayerTurnComplete = false;
    [SerializeField] private bool isAITurnComplete = false;

    [Header("Battle Delay")]
    [SerializeField] private float postBattleDelay = 2f; // ✅ Delay before respawn


    [Header("Transition Animations")]
    [SerializeField] BattleToDraftTransition battleToDraftTransition;
    [SerializeField] internal DraftCardSpawnAnimation draftCardSpawnAnimation;
    private void Start()
    {
        InitializeGame();
        StartButton();
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

        // ✅ Reset health when starting new game
        if (healthSystem != null)
        {
            healthSystem.ResetHealth();
        }

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
            case GameState.Lose:
                uiManager.ShowLosePanel();
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

        // ✅ ALL TURNS: Normal draft (no skill turns)
        OpenPlayerDraft();
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

    // ===== BATTLE COMPLETE (FIXED!) =====
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

        // Clear battle scene (but don't respawn yet!)
        gridManager.ClearSceneObjects();

        // ✅ NEW: Wait for delay, then continue
        Invoke(nameof(ContinueAfterBattle), postBattleDelay);
    }

    private void ContinueAfterBattle()
    {
        // Check game over
        if (healthSystem.IsGameOver())
        {
            if (healthSystem.GetPlayerHealth() <= 0)
                ChangeState(GameState.Lose);
            else
                ChangeState(GameState.Reward);
            return;
        }

        // ✅ NEW: Get units to spawn
        List<RuntimeUnit> unitsToSpawn = gridManager.GetPreviousUnits();

        // ✅ NEW: Use transition animation instead of instant respawn
        if (battleToDraftTransition != null)
        {
            battleToDraftTransition.StartTransition(unitsToSpawn, () =>
            {
                // After animation completes:
                AdvanceTurn();
                ChangeState(GameState.Draft);
            });
        }
        else
        {
            // Fallback: Old instant respawn
            gridManager.RespawnPreviousUnits();
            AdvanceTurn();
            ChangeState(GameState.Draft);
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

        // ✅ Reset health
        if (healthSystem != null)
        {
            healthSystem.ResetHealth();
        }

        ChangeState(GameState.Draft);
    }
}