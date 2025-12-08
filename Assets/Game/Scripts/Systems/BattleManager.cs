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
            if (!attacker.IsAlive()) continue;
            
            RuntimeUnit target = FindTarget(attacker, defenders);
            
            if (target != null)
            {
                // Check first attack cancel
                if (target.hasFirstAttackCancel)
                {
                    target.hasFirstAttackCancel = false;
                    continue;
                }
                
                // Deal damage
                target.TakeDamage(attacker.GetFinalDamage());
                
                // Check explosive death
                if (attacker.data.isExplosive && !attacker.IsAlive())
                {
                    ApplyExplosionDamage(attacker, defenders);
                }
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
                return FindClosestInLane(attacker, enemies);
            
            case UnitType.Ranged:
                return FindClosestInLane(attacker, enemies);
            
            case UnitType.Assassin:
                return FindClosestInLane(attacker, enemies);
            
            case UnitType.Explosive:
                return FindClosestInLane(attacker, enemies);
            
            default:
                return enemies[0];
        }
    }
    
    private RuntimeUnit FindClosestInLane(RuntimeUnit attacker, List<RuntimeUnit> enemies)
    {
        // Simple: Return first alive enemy in same lane
        int attackerLane = attacker.gridSlot < 3 ? 0 : 1; // 0-2 = front, 3-5 = back
        
        foreach (var enemy in enemies)
        {
            int enemyLane = enemy.gridSlot < 3 ? 0 : 1;
            
            if (attackerLane == enemyLane && enemy.IsAlive())
            {
                return enemy;
            }
        }
        
        // No target in same lane, return any alive enemy
        foreach (var enemy in enemies)
        {
            if (enemy.IsAlive()) return enemy;
        }
        
        return null;
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
        playerUnits.RemoveAll(u => !u.IsAlive());
        enemyUnits.RemoveAll(u => !u.IsAlive());
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
        
        // Clear grid
        gridManager.ClearGrid();
        
        // Clear skill buffs
        skillSystem.ClearActiveSkill();
        
        // Notify game manager
        EventManager.OnBattleComplete(playerWon);
    }
}