// ============================================================================
// BATTLE MANAGER - WITH VICTORY CELEBRATION
// ✅ Winner celebrates before units are cleared
// ✅ Battle result UI shown
// ✅ Smooth transition to draft
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class BattleManager : MonoBehaviour
{
    [Inject] GridManager gridManager;
    [Inject] UIManager uiManager;

    [Header("Battle State")]
    [SerializeField] bool isBattleActive = false;
    [SerializeField] float poisonTickTimer = 0f;
    [SerializeField] float poisonTickInterval = 1f;

    [Header("Formation Settings")]
    [SerializeField] float formationWaitTime = 1.0f;

    [Header("Victory Settings")]
    [SerializeField] float victoryCelebrationDuration = .7f; // ✅ Winners celebrate for 2s
    [SerializeField] float battleResultUIDuration = .7f; // ✅ Show result UI for 2s

    private List<RuntimeUnit> playerUnits = new List<RuntimeUnit>();
    private List<RuntimeUnit> enemyUnits = new List<RuntimeUnit>();

    // ===== PUBLIC GETTERS =====

    public List<RuntimeUnit> GetPlayerUnits() => playerUnits;
    public List<RuntimeUnit> GetEnemyUnits() => enemyUnits;

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

    // ===== UPDATE - POISON & END CHECK =====

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

        // Remove dead units
        playerUnits.RemoveAll(u => u == null || !u.IsAlive());
        enemyUnits.RemoveAll(u => u == null || !u.IsAlive());

        // Check battle end
        if (IsBattleOver())
        {
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

        // ✅ NEW: Start victory sequence instead of immediate cleanup
        StartCoroutine(VictorySequence(playerWon));
    }

    // ===== NEW: VICTORY SEQUENCE =====

    private IEnumerator VictorySequence(bool playerWon)
    {
        Debug.Log("🎉 Starting victory sequence...");

        // 1. Stop all units (but keep them alive for celebration)
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

        // 2. Play victory animations on winners
        foreach (var winner in winners)
        {
            if (winner != null && winner.animator != null)
            {
                winner.animator.SetTrigger("Victory"); // If you have victory animation
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

        // 7. NOW clear the scene
        gridManager.ClearSceneObjects();

        // 8. Notify game manager
        EventManager.OnBattleComplete(playerWon);

        Debug.Log("✅ Victory sequence complete!");
    }
}