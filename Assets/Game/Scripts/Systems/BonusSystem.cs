// ============================================================================
// BONUS SYSTEM - Bonus kartların etkilerini uygular
// 1 Pip: +20% damage, shield, first attack cancel, poison
// 2 Pip: Double deploy, group buff, ultimate shield, expand slot
// ============================================================================

using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class BonusSystem : MonoBehaviour
{
    [Inject] GridManager gridManager;
    
    [Header("Active Bonuses")]
    [SerializeField] bool doubleDeployActive = false;
    
    // ===== APPLY BONUS =====
    
    public void ApplyBonus(BonusCardData bonus)
    {
        switch (bonus.effectType)
        {
            case BonusEffectType.DamageBoost:
                ApplyDamageBoost(bonus.effectValue);
                break;
            
            case BonusEffectType.Shield:
                ApplyShield(bonus.effectValue);
                break;
            
            case BonusEffectType.FirstAttackCancel:
                ApplyFirstAttackCancel();
                break;
            
            case BonusEffectType.Poison:
                ApplyPoison();
                break;
            
            case BonusEffectType.DoubleDeploy:
                ApplyDoubleDeploy();
                break;
            
            case BonusEffectType.GroupBuff:
                ApplyGroupBuff(bonus.targetUnitType, bonus.effectValue);
                break;
            
            case BonusEffectType.UltimateShield:
                ApplyUltimateShield(bonus.effectValue);
                break;
            
            case BonusEffectType.ExpandSlot:
                gridManager.IncreaseDeployLimit();
                break;
        }
        
        Debug.Log($"Bonus applied: {bonus.bonusName}");
    }
    
    // ===== 1 PIP BONUSES =====
    
    private void ApplyDamageBoost(float amount)
    {
        List<RuntimeUnit> units = gridManager.GetPlayerUnits();
        
        foreach (var unit in units)
        {
            unit.damageMultiplier += amount; // +0.2 = +20%
        }
        
        Debug.Log($"+{amount * 100}% Damage boost applied to all units!");
    }
    
    private void ApplyShield(float amount)
    {
        List<RuntimeUnit> units = gridManager.GetPlayerUnits();
        
        foreach (var unit in units)
        {
            float shieldValue = unit.currentHP * amount; // %20 of current HP
            unit.shieldAmount += shieldValue;
        }
        
        Debug.Log($"+{amount * 100}% Shield applied!");
    }
    
    private void ApplyFirstAttackCancel()
    {
        List<RuntimeUnit> units = gridManager.GetPlayerUnits();
        
        foreach (var unit in units)
        {
            unit.hasFirstAttackCancel = true;
        }
        
        Debug.Log("First attack cancel applied to all units!");
    }
    
    private void ApplyPoison()
    {
        List<RuntimeUnit> enemies = gridManager.GetEnemyUnits();
        
        foreach (var enemy in enemies)
        {
            enemy.poisonTicks = 5; // 5 ticks of poison
        }
        
        Debug.Log("Poison applied to all enemies!");
    }
    
    // ===== 2 PIP BONUSES =====
    
    private void ApplyDoubleDeploy()
    {
        // Next spawn will be doubled
        doubleDeployActive = true;
        Debug.Log("Double deploy activated for next spawn!");
    }
    
    private void ApplyGroupBuff(UnitType targetType, float amount)
    {
        List<RuntimeUnit> units = gridManager.GetPlayerUnits();
        
        int count = 0;
        foreach (var unit in units)
        {
            if (unit.data.unitType == targetType)
            {
                unit.damageMultiplier += amount;
                unit.shieldAmount += unit.currentHP * 0.1f; // +10% HP shield
                count++;
            }
        }
        
        Debug.Log($"Group buff applied to {count} {targetType} units!");
    }
    
    private void ApplyUltimateShield(float amount)
    {
        List<RuntimeUnit> units = gridManager.GetPlayerUnits();
        
        foreach (var unit in units)
        {
            unit.shieldAmount += 25; // Flat +25 shield
        }
        
        Debug.Log("Ultimate shield lite applied!");
    }
    
    // ===== CHECK DOUBLE DEPLOY =====
    
    public bool IsDoubleDeployActive()
    {
        return doubleDeployActive;
    }
    
    public void ConsumeDoubleDeploy()
    {
        doubleDeployActive = false;
    }
}