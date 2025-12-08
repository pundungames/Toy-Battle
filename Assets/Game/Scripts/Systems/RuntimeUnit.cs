// ============================================================================
// RUNTIME UNIT
// Battle sırasında aktif olan unit instance'ı
// Her battle yeni HP/Damage ile başlar (battle-scoped)
// ============================================================================

using UnityEngine;

public class RuntimeUnit
{
    public ToyUnitData data;
    public int currentHP;
    public int currentDamage;
    public int gridSlot;
    public bool isPlayerUnit;
    public GameObject visualObject; // Scene'deki GameObject referansı
    
    // Battle-only buffs (Her battle sonunda sıfırlanır)
    public float damageMultiplier = 1f;
    public float shieldAmount = 0f;
    public bool hasFirstAttackCancel = false;
    public int poisonTicks = 0;
    
    public RuntimeUnit(ToyUnitData unitData, int slot, bool isPlayer)
    {
        data = unitData;
        currentHP = unitData.GetScaledHP();
        currentDamage = unitData.GetScaledDamage();
        gridSlot = slot;
        isPlayerUnit = isPlayer;
    }
    
    public bool IsAlive() => currentHP > 0;
    
    public void TakeDamage(int damage)
    {
        int actualDamage = Mathf.Max(0, damage - Mathf.RoundToInt(shieldAmount));
        currentHP -= actualDamage;
        
        if (currentHP <= 0)
        {
            currentHP = 0;
            OnDeath();
        }
    }
    
    public int GetFinalDamage()
    {
        return Mathf.RoundToInt(currentDamage * damageMultiplier);
    }
    
    public void ApplyBuff(float damageBonus, float shieldBonus)
    {
        damageMultiplier += damageBonus;
        shieldAmount += shieldBonus;
    }
    
    private void OnDeath()
    {
        EventManager.OnUnitDeath(this);
        
        // Explosive unit check
        if (data.isExplosive)
        {
            Debug.Log($"{data.toyName} exploded!");
        }
    }
    
    // Battle sonunda tüm temporary buff'ları temizle
    public void ResetBattleBuffs()
    {
        damageMultiplier = 1f;
        shieldAmount = 0f;
        hasFirstAttackCancel = false;
        poisonTicks = 0;
    }
}