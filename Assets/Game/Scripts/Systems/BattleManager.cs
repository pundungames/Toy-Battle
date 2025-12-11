// ============================================================================
// BATTLE MANAGER - WITH FORMATION SYSTEM
// ✅ StartBattle() calls ArrangeUnitsInFormation()
// ✅ 1 second wait for formation animation
// ✅ Then units start autonomous combat
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
    [SerializeField] float formationWaitTime = 1.0f; // Wait for formation animation

    private List<RuntimeUnit> playerUnits = new List<RuntimeUnit>();
    private List<RuntimeUnit> enemyUnits = new List<RuntimeUnit>();

    // ===== PUBLIC GETTERS =====

    public List<RuntimeUnit> GetPlayerUnits() => playerUnits;
    public List<RuntimeUnit> GetEnemyUnits() => enemyUnits;

    // ===== START BATTLE =====

    public void StartBattle()
    {
        uiManager.ShowBattlePanel();

        // Start formation sequence
        StartCoroutine(BattleFormationSequence());
    }

    /// <summary>
    /// ✅ NEW: Formation sequence before battle starts
    /// </summary>
    private IEnumerator BattleFormationSequence()
    {
        Debug.Log("🎯 Starting formation sequence...");

        // 1. Collect units from grid
        playerUnits = gridManager.GetPlayerUnits();
        enemyUnits = gridManager.GetEnemyUnits();

        Debug.Log($"📊 Battle units: {playerUnits.Count} player vs {enemyUnits.Count} enemy");

        // 2. Apply pre-battle effects (teleport, etc)
        ApplyPreBattleEffects();

        // 3. Arrange formations (animated)
        gridManager.ArrangeUnitsInFormation(isPlayer: true);
        gridManager.ArrangeUnitsInFormation(isPlayer: false);

        Debug.Log($"⏳ Waiting {formationWaitTime}s for formation animation...");

        // 4. Wait for formation animation to complete
        yield return new WaitForSeconds(formationWaitTime);

        // 5. Tell units to start battle
        foreach (var unit in playerUnits)
        {
            unit.StartBattle();
        }

        foreach (var unit in enemyUnits)
        {
            unit.StartBattle();
        }

        // 6. Start battle
        isBattleActive = true;
        poisonTickTimer = 0f;

        EventManager.OnBattleStart();

        Debug.Log("⚔️ Battle started! Units are now fighting!");
    }

    // ===== PRE-BATTLE EFFECTS =====

    private void ApplyPreBattleEffects()
    {
        // Assassin teleport (if still needed)
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

        // Find back row enemy
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

        // 1. Poison tick
        poisonTickTimer += Time.deltaTime;
        if (poisonTickTimer >= poisonTickInterval)
        {
            poisonTickTimer = 0f;
            ApplyPoisonDamage();
        }

        // 2. Remove dead units
        playerUnits.RemoveAll(u => u == null || !u.IsAlive());
        enemyUnits.RemoveAll(u => u == null || !u.IsAlive());

        // 3. Check battle end
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

        // Stop all units
        foreach (var unit in playerUnits)
        {
            if (unit != null) unit.StopBattle();
        }

        foreach (var unit in enemyUnits)
        {
            if (unit != null) unit.StopBattle();
        }

        // Clear scene
        gridManager.ClearSceneObjects();

        // Notify game manager
        EventManager.OnBattleComplete(playerWon);
    }
}