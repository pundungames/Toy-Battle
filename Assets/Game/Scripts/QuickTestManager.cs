// ============================================================================
// QUICK TEST MANAGER - Geliştirme sırasında hızlı test için
// Hierarchy'de GameManager'ın altına koy, Inspector'dan test et
// ============================================================================

using UnityEngine;
using Zenject;

public class QuickTestManager : MonoBehaviour
{
    [Inject] GameManager gameManager;
    [Inject] DraftCardManager draftManager;
    [Inject] GridManager gridManager;

    [Header("Quick Test Buttons")]
    [SerializeField] bool autoStartDraft = false;

    private void Start()
    {
        if (autoStartDraft)
        {
            Invoke(nameof(TestStartDraft), 1f);
        }
    }

    // ===== CONTEXT MENU TEST METHODS =====

    [ContextMenu("1. Start Draft (Turn 1)")]
    public void TestStartDraft()
    {
        gameManager.currentTurn = 1;
        gameManager.ChangeState(GameState.Draft);
        Debug.Log("✅ TEST: Draft started at Turn 1");
    }

    [ContextMenu("2. Jump to Battle Turn (Turn 4 → 5)")]
    public void TestJumpToBattle()
    {
        gameManager.currentTurn = 4;
        gameManager.ChangeState(GameState.Draft);
        Debug.Log("✅ TEST: Next turn will be Turn 5 (Battle)");
    }

    [ContextMenu("3. Skip to Skill Turn (Turn 7 → 8)")]
    public void TestSkillTurn()
    {
        gameManager.currentTurn = 7;
        gameManager.ChangeState(GameState.Draft);
        Debug.Log("✅ TEST: Next turn will be Turn 8 (Skill Selection)");
    }

    [ContextMenu("4. Force Battle Now")]
    public void TestForceBattle()
    {
        gameManager.ChangeState(GameState.Battle);
        Debug.Log("✅ TEST: Battle forced");
    }

    [ContextMenu("5. Show Chest")]
    public void TestShowChest()
    {
        gameManager.ChangeState(GameState.Chest);
        Debug.Log("✅ TEST: Chest screen");
    }

    [ContextMenu("6. Add 100 Gold")]
    public void TestAddGold()
    {
        EventManager.OnGoldChanged(100);
        Debug.Log("✅ TEST: Added 100 gold");
    }

    [ContextMenu("7. Spawn Test Unit (Player)")]
    public void TestSpawnPlayerUnit()
    {
        // Bu çalışması için en az 1 ToyUnitData olmalı
        var testUnit = Resources.Load<ToyUnitData>("Units/HeMa n");
        if (testUnit != null)
        {
            gridManager.SpawnUnit(testUnit, true);
            Debug.Log("✅ TEST: Player unit spawned");
        }
        else
        {
            Debug.LogError("❌ TEST: He-Man unit not found in Resources/Units/");
        }
    }

    [ContextMenu("8. Spawn Test Unit (Enemy)")]
    public void TestSpawnEnemyUnit()
    {
        var testUnit = Resources.Load<ToyUnitData>("Units/ToySoldier");
        if (testUnit != null)
        {
            gridManager.SpawnUnit(testUnit, false);
            Debug.Log("✅ TEST: Enemy unit spawned");
        }
        else
        {
            Debug.LogError("❌ TEST: Toy Soldier unit not found in Resources/Units/");
        }
    }

    [ContextMenu("9. Clear Grid")]
    public void TestClearGrid()
    {
        gridManager.ClearGrid();
        Debug.Log("✅ TEST: Grid cleared");
    }

    [ContextMenu("10. Reset Game")]
    public void TestResetGame()
    {
        gameManager.currentTurn = 1;
        gameManager.playerWins = 0;
        gridManager.ClearGrid();
        gameManager.ChangeState(GameState.MainMenu);
        Debug.Log("✅ TEST: Game reset to Main Menu");
    }

    // ===== DEBUG INFO =====

    [ContextMenu("Debug: Show Current State")]
    public void DebugShowState()
    {
        Debug.Log($"=== GAME STATE ===\n" +
                  $"State: {gameManager.currentState}\n" +
                  $"Turn: {gameManager.currentTurn}/30\n" +
                  $"Wins: {gameManager.playerWins}\n" +
                  $"Tutorial: {gameManager.isTutorial}");
    }

    [ContextMenu("Debug: Show Grid Status")]
    public void DebugShowGrid()
    {
        var playerUnits = gridManager.GetPlayerUnits();
        var enemyUnits = gridManager.GetEnemyUnits();

        Debug.Log($"=== GRID STATUS ===\n" +
                  $"Player Units: {playerUnits.Count}\n" +
                  $"Enemy Units: {enemyUnits.Count}");

        foreach (var unit in playerUnits)
        {
            Debug.Log($"  - {unit.data.toyName} (HP: {unit.currentHP}/{unit.data.GetScaledHP()}, Slot: {unit.gridSlot})");
        }
    }

    // ===== KEYBOARD SHORTCUTS (Geliştirme için) =====

    private void Update()
    {
        // F1: Start Draft
        if (Input.GetKeyDown(KeyCode.F1))
        {
            TestStartDraft();
        }

        // F2: Jump to Battle
        if (Input.GetKeyDown(KeyCode.F2))
        {
            TestJumpToBattle();
        }

        // F3: Force Battle
        if (Input.GetKeyDown(KeyCode.F3))
        {
            TestForceBattle();
        }

        // F4: Show Chest
        if (Input.GetKeyDown(KeyCode.F4))
        {
            TestShowChest();
        }

        // F5: Add Gold
        if (Input.GetKeyDown(KeyCode.F5))
        {
            TestAddGold();
        }

        // F9: Clear Grid
        if (Input.GetKeyDown(KeyCode.F9))
        {
            TestClearGrid();
        }

        // F10: Reset Game
        if (Input.GetKeyDown(KeyCode.F10))
        {
            TestResetGame();
        }

        // F12: Debug Info
        if (Input.GetKeyDown(KeyCode.F12))
        {
            DebugShowState();
            DebugShowGrid();
        }
    }
}