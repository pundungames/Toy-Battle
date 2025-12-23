// ============================================================================
// BATTLE MANAGER - WITH SPLIT ABILITY SUPPORT
// ✅ Winner celebrates before units are cleared
// ✅ Battle result UI shown
// ✅ Smooth transition to draft
// ✅ FIXED: Delayed battle end check for split abilities (Golem split)
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

public class BattleManager : MonoBehaviour
{
    [Inject] GridManager gridManager;
    [Inject] UIManager uiManager;
    [Inject] DraftCardManager draftCardManager;

    [Header("Battle State")]
    [SerializeField] bool isBattleActive = false;
    [SerializeField] float poisonTickTimer = 0f;
    [SerializeField] float poisonTickInterval = 1f;

    [Header("Formation Settings")]
    [SerializeField] float formationWaitTime = 1.0f;

    [Header("Victory Settings")]
    [SerializeField] float victoryCelebrationDuration = .7f;
    [SerializeField] float battleResultUIDuration = .7f;

    [Header("Battle End Delay (for Split Abilities)")]
    [SerializeField] float battleEndCheckDelay = 0.3f; // ✅ Delay before checking battle end
    private float lastDeathTime = -999f; // Track when last unit died
    private bool isPendingBattleEndCheck = false; // Flag for delayed check

    [SerializeField] List<RuntimeUnit> playerUnits = new List<RuntimeUnit>();
    [SerializeField] List<RuntimeUnit> enemyUnits = new List<RuntimeUnit>();

    // ===== PUBLIC GETTERS =====

    public List<RuntimeUnit> GetPlayerUnits() => playerUnits;
    public List<RuntimeUnit> GetEnemyUnits() => enemyUnits;

    // ===== ADD UNITS DYNAMICALLY (for split abilities) =====

    /// <summary>
    /// Add a unit to the battle mid-fight (e.g., mini golems from split)
    /// </summary>
    public void AddPlayerUnit(RuntimeUnit unit)
    {
        if (unit != null && !playerUnits.Contains(unit))
        {
            playerUnits.Add(unit);
            Debug.Log($"➕ Added {unit.data.toyName} to player units (now {playerUnits.Count} units)");
        }
    }

    /// <summary>
    /// Add a unit to the battle mid-fight (e.g., mini golems from split)
    /// </summary>
    public void AddEnemyUnit(RuntimeUnit unit)
    {
        if (unit != null && !enemyUnits.Contains(unit))
        {
            enemyUnits.Add(unit);
            Debug.Log($"➕ Added {unit.data.toyName} to enemy units (now {enemyUnits.Count} units)");
        }
    }

    // ===== START BATTLE =====

    public void StartBattle()
    {
        uiManager.ShowBattlePanel();
        StartCoroutine(BattleFormationSequence());
    }

    private IEnumerator BattleFormationSequence()
    {
        Debug.Log("🎯 Starting formation sequence...");

        playerUnits = gridManager.GetPlayerUnits();
        enemyUnits = gridManager.GetEnemyUnits();

        Debug.Log($"📊 Battle units: {playerUnits.Count} player vs {enemyUnits.Count} enemy");

        ApplyPreBattleEffects();

        gridManager.ArrangeUnitsInFormation(isPlayer: true);
        gridManager.ArrangeUnitsInFormation(isPlayer: false);

        Debug.Log($"⏳ Waiting {formationWaitTime}s for formation animation...");

        yield return new WaitForSeconds(formationWaitTime);

        foreach (var unit in playerUnits)
        {
            unit.StartBattle();
        }

        foreach (var unit in enemyUnits)
        {
            unit.StartBattle();
        }

        isBattleActive = true;
        poisonTickTimer = 0f;

        EventManager.OnBattleStart();

        Debug.Log("⚔️ Battle started! Units are now fighting!");
    }

    // ===== PRE-BATTLE EFFECTS =====

    private void ApplyPreBattleEffects()
    {
        foreach (var unit in playerUnits)
        {
            if (unit.data.hasTeleport)
            {
                TeleportAssassin(unit, enemyUnits);
            }
        }

        foreach (var unit in enemyUnits)
        {
            if (unit.data.hasTeleport)
            {
                TeleportAssassin(unit, playerUnits);
            }
        }
    }

    private void TeleportAssassin(RuntimeUnit assassin, List<RuntimeUnit> enemies)
    {
        if (enemies.Count == 0) return;

        RuntimeUnit backRowEnemy = enemies.Find(e => e != null && e.gridSlot >= 6);

        if (backRowEnemy != null)
        {
            Vector3 teleportPos = backRowEnemy.transform.position +
                (assassin.isPlayerUnit ? Vector3.back : Vector3.forward) * 1f;

            assassin.transform.position = teleportPos;

            Debug.Log($"⚡ {assassin.data.toyName} teleported to back line!");
        }
    }

    // ===== UPDATE - POISON & END CHECK WITH SPLIT SUPPORT =====

    private void Update()
    {
        if (!isBattleActive) return;

        // Poison tick
        poisonTickTimer += Time.deltaTime;
        if (poisonTickTimer >= poisonTickInterval)
        {
            poisonTickTimer = 0f;
            ApplyPoisonDamage();
        }

        // ✅ Get counts BEFORE cleanup (to detect deaths)
        int playerCountBefore = playerUnits.Count(u => u != null && u.IsAlive());
        int enemyCountBefore = enemyUnits.Count(u => u != null && u.IsAlive());

        // Remove dead units
        playerUnits.RemoveAll(u => u == null || !u.IsAlive());
        enemyUnits.RemoveAll(u => u == null || !u.IsAlive());

        // ✅ Get counts AFTER cleanup
        int playerCountAfter = playerUnits.Count;
        int enemyCountAfter = enemyUnits.Count;

        // ✅ Check if units DIED this frame (DECREASED, not just changed)
        bool unitsActuallyDied = (playerCountAfter < playerCountBefore) || (enemyCountAfter < enemyCountBefore);

        if (unitsActuallyDied)
        {
            // Units died, mark time and set pending flag
            lastDeathTime = Time.time;
            isPendingBattleEndCheck = true;

            Debug.Log($"💀 Unit death detected. Waiting {battleEndCheckDelay}s for split abilities... " +
                     $"(P: {playerCountBefore}→{playerCountAfter}, E: {enemyCountBefore}→{enemyCountAfter})");
        }

        // ✅ Delayed battle end check (gives time for split/spawn abilities)
        if (isPendingBattleEndCheck && Time.time >= lastDeathTime + battleEndCheckDelay)
        {
            isPendingBattleEndCheck = false;

            // NOW check battle end after delay
            if (IsBattleOver())
            {
                Debug.Log("⚔️ Battle end confirmed after split delay");
                EndBattle();
            }
            else
            {
                Debug.Log($"✅ Battle continues (new units spawned from split) - P:{playerUnits.Count}, E:{enemyUnits.Count}");
            }
        }
        // ✅ ALSO check immediately if no pending check (normal battle flow)
        else if (!isPendingBattleEndCheck && IsBattleOver())
        {
            Debug.Log("⚔️ Battle end detected (normal check)");
            EndBattle();
        }
    }

    // ===== POISON DAMAGE =====

    private void ApplyPoisonDamage()
    {
        ApplyPoisonToList(playerUnits);
        ApplyPoisonToList(enemyUnits);
    }

    private void ApplyPoisonToList(List<RuntimeUnit> units)
    {
        foreach (var unit in units)
        {
            if (unit != null && unit.poisonTicks > 0)
            {
                unit.TakeDamage(5);
                unit.poisonTicks--;
            }
        }
    }

    // ===== BATTLE END =====

    private bool IsBattleOver()
    {
        return playerUnits.Count == 0 || enemyUnits.Count == 0;
    }

    private void EndBattle()
    {
        isBattleActive = false;

        bool playerWon = playerUnits.Count > 0;

        Debug.Log($"⚔️ Battle ended! Winner: {(playerWon ? "PLAYER" : "ENEMY")}");

        // Start victory sequence
        StartCoroutine(VictorySequence(playerWon));
    }

    // ===== VICTORY SEQUENCE =====

    private IEnumerator VictorySequence(bool playerWon)
    {
        Debug.Log("🎉 Starting victory sequence...");

        // 1. Stop all units
        List<RuntimeUnit> winners = playerWon ? playerUnits : enemyUnits;
        List<RuntimeUnit> losers = playerWon ? enemyUnits : playerUnits;

        foreach (var unit in winners)
        {
            if (unit != null) unit.StopBattle();
        }

        foreach (var unit in losers)
        {
            if (unit != null) unit.StopBattle();
        }

        // 2. Play victory animations
        foreach (var winner in winners)
        {
            if (winner != null && winner.animator != null)
            {
                winner.animator.SetTrigger("Victory");
            }
        }

        // 3. Show battle result UI
        uiManager.ShowBattleResultUI(playerWon);

        // 4. Wait for celebration
        yield return new WaitForSeconds(victoryCelebrationDuration);

        // 5. Hide battle result UI
        uiManager.HideBattleResultUI();

        // 6. Wait a bit more for UI
        yield return new WaitForSeconds(battleResultUIDuration);

        // 7. Clear the scene
        gridManager.ClearSceneObjects();

        // 8. Notify game manager
        EventManager.OnBattleComplete(playerWon);
        draftCardManager.ResetStamina();

        Debug.Log("✅ Victory sequence complete!");
    }
}