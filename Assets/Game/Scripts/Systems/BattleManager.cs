// ============================================================================
// BATTLE MANAGER - SIMPLIFIED (Units handle their own movement)
// ✅ No more tick system
// ✅ Units autonomously move and attack
// ✅ Only handles battle start/end and poison
// ❌ REMOVED: Skill system buffs
// ============================================================================

using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class BattleManager : MonoBehaviour
{
    [Inject] GridManager gridManager;
    [Inject] UIManager uiManager;
    // ❌ REMOVED: [Inject] SkillSystem skillSystem;

    [Header("Battle State")]
    [SerializeField] bool isBattleActive = false;
    [SerializeField] float poisonTickTimer = 0f;
    [SerializeField] float poisonTickInterval = 1f; // Poison her 1 saniyede bir

    private List<RuntimeUnit> playerUnits = new List<RuntimeUnit>();
    private List<RuntimeUnit> enemyUnits = new List<RuntimeUnit>();

    // ===== PUBLIC GETTERS (for RuntimeUnit to find enemies) =====

    public List<RuntimeUnit> GetPlayerUnits() => playerUnits;
    public List<RuntimeUnit> GetEnemyUnits() => enemyUnits;

    // ===== START BATTLE =====

    public void StartBattle()
    {
        uiManager.ShowBattlePanel();

        // Collect units from grid
        playerUnits = gridManager.GetPlayerUnits();
        enemyUnits = gridManager.GetEnemyUnits();

        // Apply pre-battle effects
        ApplyPreBattleEffects();

        // ❌ REMOVED: Apply skill buffs
        // skillSystem.ApplyActiveSkillBuffs(playerUnits);

        // ✅ Tell all units to start battle (autonomous movement)
        foreach (var unit in playerUnits)
        {
            unit.StartBattle();
        }

        foreach (var unit in enemyUnits)
        {
            unit.StartBattle();
        }

        // Start battle
        isBattleActive = true;
        poisonTickTimer = 0f;

        EventManager.OnBattleStart();
    }

    // ===== PRE-BATTLE EFFECTS =====

    private void ApplyPreBattleEffects()
    {
        // Assassin teleport
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

        // Find back row enemy position
        RuntimeUnit backRowEnemy = enemies.Find(e => e != null && e.gridSlot >= 3);

        if (backRowEnemy != null)
        {
            // Teleport to near back row enemy
            Vector3 teleportPos = backRowEnemy.transform.position +
                (assassin.isPlayerUnit ? Vector3.back : Vector3.forward) * 1f;

            assassin.transform.position = teleportPos;

            Debug.Log($"⚡ {assassin.data.toyName} teleported to back line!");
        }
    }

    // ===== UPDATE - ONLY FOR POISON & END CHECK =====

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

        // 2. Remove dead units from lists (cleanup)
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

        // Clear scene objects
        gridManager.ClearSceneObjects();

        // ❌ REMOVED: Clear skill buffs
        // skillSystem.ClearActiveSkill();

        // Notify game manager
        EventManager.OnBattleComplete(playerWon);
    }
}