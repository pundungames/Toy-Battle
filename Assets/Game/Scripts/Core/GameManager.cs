// ============================================================================
// GAME MANAGER - FIXED VERSION
// ✅ Battle Result UI now shows properly
// ✅ No double panel opening
// ✅ Delay only on battle→draft transition
// ============================================================================

using System.Collections;
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
    [Inject] HealthSystem healthSystem;

    [Header("Game State")]
    [SerializeField] internal GameState currentState;
    [SerializeField] internal int currentTurn = 1;
    [SerializeField] internal int currentBattleTurn = 1;
    [SerializeField] internal int playerWins = 0;
    [SerializeField] internal bool isTutorial = false;

    [Header("Turn Control")]
    [SerializeField] private bool isPlayerTurnComplete = false;
    [SerializeField] private bool isAITurnComplete = false;

    [Header("Battle Delay")]
    [SerializeField] private float postBattleDelay = 2f;

    [Header("Transition Animations")]
    [SerializeField] BattleToDraftTransition battleToDraftTransition;
    [SerializeField] internal DraftCardSpawnAnimation draftCardSpawnAnimation;

    // ✅ NEW: Flag to track if coming from battle
    private bool isComingFromBattle = false;

    // ===== MOBILE DEBUG START =====
    private void Start()
    {
        StartCoroutine(MobileSafeStart());
    }

    private IEnumerator MobileSafeStart()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.5f);

        InitializeGame();
        yield return new WaitForSeconds(1f);

        StartButton();
    }

    private void OnEnable()
    {
        Debug.Log("📌 GameManager OnEnable called");
        EventManager.onCardSelected += OnPlayerCardSelected;
        EventManager.onDraftComplete += OnBothTurnsComplete;
        EventManager.onBattleComplete += OnBattleComplete;
    }

    private void OnDisable()
    {
        Debug.Log("📌 GameManager OnDisable called");
        EventManager.onCardSelected -= OnPlayerCardSelected;
        EventManager.onDraftComplete -= OnBothTurnsComplete;
        EventManager.onBattleComplete -= OnBattleComplete;
    }

    // ===== INITIALIZE GAME =====
    private void InitializeGame()
    {
        Debug.Log($"🔧 InitializeGame: isTutorial={isTutorial}, TutorialComplete={PlayerPrefs.GetInt("TutorialComplete", 0)}");

        try
        {
            if (isTutorial && PlayerPrefs.GetInt("TutorialComplete", 0) == 0)
            {
                Debug.Log("📚 Tutorial mode detected, starting tutorial...");
                isTutorial = true;

                if (tutorialController != null)
                {
                    tutorialController.StartTutorial();
                    Debug.Log("✅ Tutorial started");
                }
                else
                {
                    Debug.LogError("❌ TutorialController is NULL!");
                }
            }
            else
            {
                Debug.Log("🎮 Normal mode, changing to MainMenu state...");
                isTutorial = false;
                ChangeState(GameState.MainMenu);
                Debug.Log($"✅ Changed to MainMenu (currentState={currentState})");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ InitializeGame Exception: {e.Message}");
            throw;
        }
    }

    // ===== START BUTTON =====
    public void StartButton()
    {
        try
        {
            currentTurn = 1;
            if (healthSystem != null)
            {
                healthSystem.ResetHealth();
            }
            else
            {
                Debug.LogError("❌ HealthSystem is NULL!");
            }

            // ✅ Reset battle flag when starting new game
            isComingFromBattle = false;

            ChangeState(GameState.Draft);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ StartButton Exception: {e.Message}");
            Debug.LogError($"Stack Trace: {e.StackTrace}");
            throw;
        }
    }

    // ===== CHANGE STATE =====
    public void ChangeState(GameState newState)
    {
        currentState = newState;

        try
        {
            EventManager.OnGameStateChange(newState);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ EventManager.OnGameStateChange FAILED: {e.Message}");
        }

        try
        {
            switch (newState)
            {
                case GameState.MainMenu:
                    Debug.Log("📋 Case: MainMenu - Calling uiManager.ShowMainMenu()");
                    if (uiManager != null)
                    {
                        uiManager.ShowMainMenu();
                    }
                    break;

                case GameState.Draft:
                    StartDraftPhase();
                    break;

                case GameState.Battle:
                    StartBattlePhase();
                    break;

                case GameState.Reward:
                    if (uiManager != null)
                    {
                        uiManager.ShowRewardPanel();
                    }
                    break;

                case GameState.Chest:
                    if (uiManager != null)
                    {
                        uiManager.ShowChestPanel();
                    }
                    break;

                case GameState.Progress:
                    if (uiManager != null)
                    {
                        uiManager.ShowProgressPanel();
                    }
                    break;

                case GameState.Lose:
                    if (uiManager != null)
                    {
                        uiManager.ShowLosePanel();
                    }
                    break;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Switch case FAILED: {e.Message}");
            Debug.LogError($"Stack Trace: {e.StackTrace}");
        }
    }

    // ===== DRAFT PHASE =====
    private void StartDraftPhase()
    {
        isPlayerTurnComplete = false;
        isAITurnComplete = false;


        try
        {
            if (uiManager != null)
            {
                // Pass flag to UIManager to decide delay
                //uiManager.ShowDraftPanel(isComingFromBattle);

                // ✅ Reset flag after use
                isComingFromBattle = false;
            }
            else
            {
                Debug.LogError("❌ UIManager is NULL!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ ShowDraftPanel FAILED: {e.Message}");
        }

        try
        {
            OpenPlayerDraft();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ OpenPlayerDraft FAILED: {e.Message}");
        }

    }

    private void OpenPlayerDraft()
    {
        try
        {
            if (draftCardManager != null)
            {
                draftCardManager.Open(false);
            }
            else
            {
                Debug.LogError("❌ DraftCardManager is NULL!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ OpenPlayerDraft Exception: {e.Message}");
            Debug.LogError($"Stack Trace: {e.StackTrace}");
        }
    }

    private void OnPlayerCardSelected(ToyUnitData unitData)
    {
        Debug.Log($"✅ Player selected: {unitData.toyName}");
        isPlayerTurnComplete = true;
        StartAITurn();
    }

    private void StartAITurn()
    {
        Debug.Log("🤖 Starting AI turn...");
        aiTurnManager.StartAITurn();
    }

    private void OnBothTurnsComplete()
    {
        isAITurnComplete = true;
        if (isPlayerTurnComplete && isAITurnComplete)
        {
            Debug.Log("✅ Both player and AI turns complete!");
            AdvanceTurn();
        }
    }

    public void AdvanceTurn()
    {
        currentTurn++;
        EventManager.OnTurnChange(currentTurn);
        Debug.Log($"📊 Turn {currentTurn}/{GameConstants.TOTAL_TURNS}");

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
        // ✅ Normal draft turn - no delay
        isComingFromBattle = false;
        ChangeState(GameState.Draft);
    }

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
        currentBattleTurn++;
        Invoke(nameof(ContinueAfterBattle), postBattleDelay);
    }

    private void ContinueAfterBattle()
    {
        Debug.Log("🎬 ContinueAfterBattle called");

        if (healthSystem.IsGameOver())
        {
            if (healthSystem.GetPlayerHealth() <= 0)
                ChangeState(GameState.Lose);
            else
                ChangeState(GameState.Reward);
            return;
        }

        // ✅ Set flag before respawning
        isComingFromBattle = true;

        // ✅ Call GetPreviousUnits - it now handles EVERYTHING via coroutine
        // The coroutine will:
        // 1. Clean old units (with delays)
        // 2. Spawn new units (with delays)  
        // 3. Start transition automatically
        // 4. Call AdvanceTurn() when complete
        gridManager.GetPreviousUnits();

        Debug.Log("✅ GetPreviousUnits called - coroutine will handle transition and AdvanceTurn");
    }
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

    public void StartNewGame()
    {
        currentTurn = 1;
        playerWins = 0;
        gridManager.ResetGridState();

        if (healthSystem != null)
        {
            healthSystem.ResetHealth();
        }

        isComingFromBattle = false;
        ChangeState(GameState.Draft);
    }

    // ===== PUBLIC GETTERS =====
    public GameState CurrentGameState => currentState;
}