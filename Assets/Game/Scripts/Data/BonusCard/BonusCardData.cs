// ============================================================================
// BONUS CARD DATA - ScriptableObject
// Bonus kartların verilerini tutar (+20% damage, poison, vb.)
// ============================================================================

using UnityEngine;

[CreateAssetMenu(fileName = "BonusCard", menuName = "CardGame/Bonus Card Data")]
public class BonusCardData : ScriptableObject
{
    [Header("Basic Info")]
    public string bonusID;
    public string bonusName;
    [TextArea] public string description;
    
    [Header("Cost")]
    public int pipCost; // 1 or 2
    
    [Header("Effect")]
    public BonusEffectType effectType;
    public float effectValue; // Örn: 0.2f = %20
    public UnitType targetUnitType; // GroupBuff için
    
    [Header("Visual")]
    public Sprite cardSprite;
}