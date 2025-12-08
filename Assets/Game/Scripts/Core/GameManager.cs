// ============================================================================
// GAME MANAGER - Ana oyun state machine'i
// Tüm turn progression ve state geçişlerini kontrol eder
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

    [Header("Game State")]
    [SerializeField] internal GameState currentState;
    [SerializeField] internal int currentTurn = 1;
    [SerializeField] internal int playerWins = 0;
    [SerializeField] internal bool isTutorial = true;

    private void Start()
    {
        InitializeGame();
    }

    private void OnEnable()
    {
        EventManager.onDraftComplete += OnDraftComplete;
        EventManager.onBattleComplete += OnBattleComplete;
        EventManager.onGameStateChange += ChangeState;
    }

    private void OnDisable()
    {
        EventManager.onDraftComplete -= OnDraftComplete;
        EventManager.onBattleComplete -= OnBattleComplete;
       EventManager.onGameStateChange -= ChangeState;
    }

    private void InitializeGame()
    {
        // Tutorial check
        if (!isTutorial && PlayerPrefs.GetInt("TutorialComplete", 0) == 0)
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
        // Skill selection turns: 8, 16, 24
        if (currentTurn == 8 || currentTurn == 16 || currentTurn == 24)
        {
            uiManager.ShowSkillSelection();
        }
        else
        {
            draftCardManager.Open(false); // false = not shop mode
        }
    }

    private void StartBattlePhase()
    {
        battleManager.StartBattle();
    }

    private void OnDraftComplete()
    {
        currentTurn++;
        EventManager.OnTurnChange(currentTurn);

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
            ChangeState(GameState.Draft);
        }
    }

    private void OnBattleComplete(bool playerWon)
    {
        if (playerWon)
        {
            playerWins++;
            currencyManager.UpdateCash(GameConstants.WIN_GOLD);
        }
        else
        {
            currencyManager.UpdateCash(GameConstants.LOSE_GOLD);
        }

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
        ChangeState(GameState.Draft);
    }
}