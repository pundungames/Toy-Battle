// ============================================================================
// BATTLE MANAGER - Combat loop'u yönetir
// 0.5s tick sistemi ile unit attack/death/end kontrolü yapar
// ============================================================================

using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class BattleManager : MonoBehaviour
{
    [Inject] GridManager gridManager;
    [Inject] UIManager uiManager;
    [Inject] SkillSystem skillSystem;

    [Header("Battle State")]
    [SerializeField] bool isBattleActive = false;
    [SerializeField] float combatTimer = 0f;

    private List<RuntimeUnit> playerUnits = new List<RuntimeUnit>();
    private List<RuntimeUnit> enemyUnits = new List<RuntimeUnit>();

    // ===== START BATTLE =====

    public void StartBattle()
    {
        uiManager.ShowBattlePanel();

        // Collect units from grid
        playerUnits = gridManager.GetPlayerUnits();
        enemyUnits = gridManager.GetEnemyUnits();

        // Apply pre-battle effects
        ApplyPreBattleEffects();

        // Apply skill buffs
        skillSystem.ApplyActiveSkillBuffs(playerUnits);

        // Start combat loop
        isBattleActive = true;
        combatTimer = 0f;

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
        // Teleport to back row (slot 3, 4, 5)
        Debug.Log($"{assassin.data.toyName} teleported to enemy back line!");

        // Visual: Move sprite to back enemy position
        if (assassin.visualObject != null && enemies.Count > 0)
        {
            // Simple teleport effect can be added here
        }
    }

    // ===== COMBAT TICK =====

    private void Update()
    {
        if (!isBattleActive) return;

        combatTimer += Time.deltaTime;

        if (combatTimer >= GameConstants.COMBAT_TICK_INTERVAL)
        {
            combatTimer = 0f;
            ExecuteCombatTick();
        }
    }

    private void ExecuteCombatTick()
    {
        // 1. Apply poison damage
        ApplyPoisonDamage();

        // 2. Execute attacks
        ExecuteAttacks(playerUnits, enemyUnits);
        ExecuteAttacks(enemyUnits, playerUnits);

        // 3. Remove dead units
        RemoveDeadUnits();

        // 4. Check battle end
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
            if (unit.poisonTicks > 0)
            {
                unit.TakeDamage(5);
                unit.poisonTicks--;
            }
        }
    }

    // ===== EXECUTE ATTACKS =====

    private void ExecuteAttacks(List<RuntimeUnit> attackers, List<RuntimeUnit> defenders)
    {
        foreach (var attacker in attackers)
        {
            if (attacker == null || !attacker.IsAlive()) continue;

            RuntimeUnit target = FindTarget(attacker, defenders);

            if (target != null)
            {
                // Check first attack cancel
                if (target.hasFirstAttackCancel)
                {
                    target.hasFirstAttackCancel = false;
                    Debug.Log($"⚔️ {target.data.toyName} blocked first attack!");
                    continue;
                }

                // ✅ Unit kendi attack animasyonunu oynatır
                attacker.PlayAttackAnimation();

                // ✅ Hedef kendi TakeDamage()'i ile damage alır
                target.TakeDamage(attacker.GetFinalDamage());

                // Unit kendi OnDeath() ile destroy olur
                // BattleManager hiçbir şeyi destroy etmez!
            }
        }
    }

    // ===== TARGET SELECTION =====

    private RuntimeUnit FindTarget(RuntimeUnit attacker, List<RuntimeUnit> enemies)
    {
        if (enemies.Count == 0) return null;

        switch (attacker.data.unitType)
        {
            case UnitType.Melee:
                return FindMeleeTarget(attacker, enemies);

            case UnitType.Ranged:
                return FindRangedTarget(attacker, enemies);

            case UnitType.Assassin:
                return FindAssassinTarget(attacker, enemies);

            case UnitType.Explosive:
                return FindMeleeTarget(attacker, enemies); // Explosive uses melee logic

            default:
                return FindMeleeTarget(attacker, enemies);
        }
    }

    // ===== MOBILE VERTICAL LAYOUT - LANE SYSTEM =====
    // Slot 0-2 = Front Row (CLOSE)
    // Slot 3-5 = Back Row (FAR)

    private int GetLane(int slotIndex)
    {
        return slotIndex < 3 ? 0 : 1; // 0 = Front, 1 = Back
    }

    private int GetColumn(int slotIndex)
    {
        return slotIndex % 3; // 0 = Left, 1 = Middle, 2 = Right
    }

    // ===== MELEE TARGET (Front row priority) =====

    private RuntimeUnit FindMeleeTarget(RuntimeUnit attacker, List<RuntimeUnit> enemies)
    {
        int attackerColumn = GetColumn(attacker.gridSlot);

        // 1. Önce karşı tarafın FRONT ROW'una bak (en yakın)
        RuntimeUnit frontTarget = FindInFrontRow(attackerColumn, enemies);
        if (frontTarget != null)
            return frontTarget;

        // 2. Front row boşsa BACK ROW'a geç
        RuntimeUnit backTarget = FindInBackRow(attackerColumn, enemies);
        if (backTarget != null)
            return backTarget;

        // 3. İlk bulduğu canlı enemy
        return enemies.Find(e => e != null && e.IsAlive());
    }

    private RuntimeUnit FindInFrontRow(int column, List<RuntimeUnit> enemies)
    {
        // Önce aynı column
        RuntimeUnit sameColumn = enemies.Find(e =>
            e != null && e.IsAlive() &&
            GetLane(e.gridSlot) == 0 &&
            GetColumn(e.gridSlot) == column);

        if (sameColumn != null)
            return sameColumn;

        // Yan columnlar
        return enemies.Find(e =>
            e != null && e.IsAlive() &&
            GetLane(e.gridSlot) == 0);
    }

    private RuntimeUnit FindInBackRow(int column, List<RuntimeUnit> enemies)
    {
        // Önce aynı column
        RuntimeUnit sameColumn = enemies.Find(e =>
            e != null && e.IsAlive() &&
            GetLane(e.gridSlot) == 1 &&
            GetColumn(e.gridSlot) == column);

        if (sameColumn != null)
            return sameColumn;

        // Yan columnlar
        return enemies.Find(e =>
            e != null && e.IsAlive() &&
            GetLane(e.gridSlot) == 1);
    }

    // ===== RANGED TARGET (Can hit any row) =====

    private RuntimeUnit FindRangedTarget(RuntimeUnit attacker, List<RuntimeUnit> enemies)
    {
        int attackerColumn = GetColumn(attacker.gridSlot);

        // Aynı column'daki herhangi bir enemy (front ya da back)
        RuntimeUnit sameColumnTarget = enemies.Find(e =>
            e != null && e.IsAlive() &&
            GetColumn(e.gridSlot) == attackerColumn);

        if (sameColumnTarget != null)
            return sameColumnTarget;

        // Herhangi bir canlı enemy
        return enemies.Find(e => e != null && e.IsAlive());
    }

    // ===== ASSASSIN TARGET (Back row priority) =====

    private RuntimeUnit FindAssassinTarget(RuntimeUnit attacker, List<RuntimeUnit> enemies)
    {
        // Back row öncelikli
        RuntimeUnit backTarget = enemies.Find(e =>
            e != null && e.IsAlive() && GetLane(e.gridSlot) == 1);

        if (backTarget != null)
            return backTarget;

        // Back row boşsa front row
        return enemies.Find(e => e != null && e.IsAlive());
    }

    // ===== EXPLOSION DAMAGE =====

    private void ApplyExplosionDamage(RuntimeUnit exploder, List<RuntimeUnit> enemies)
    {
        int damage = exploder.data.explosionDamage;

        // Apply AoE damage to all enemies in same lane
        int exploderLane = exploder.gridSlot < 3 ? 0 : 1;

        foreach (var enemy in enemies)
        {
            int enemyLane = enemy.gridSlot < 3 ? 0 : 1;

            if (enemyLane == exploderLane)
            {
                enemy.TakeDamage(damage);
            }
        }

        Debug.Log($"{exploder.data.toyName} exploded for {damage} AoE damage!");
    }

    // ===== REMOVE DEAD UNITS =====

    private void RemoveDeadUnits()
    {
        // ✅ Sadece listeden çıkar, destroy etme - unit kendi kendini destroy ediyor
        playerUnits.RemoveAll(u => u == null || !u.IsAlive());
        enemyUnits.RemoveAll(u => u == null || !u.IsAlive());
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

        // ✅ Scene'deki objeleri temizle AMA state'i koru
        gridManager.ClearSceneObjects();

        // Clear skill buffs
        skillSystem.ClearActiveSkill();

        // Notify game manager
        EventManager.OnBattleComplete(playerWon);
    }
}