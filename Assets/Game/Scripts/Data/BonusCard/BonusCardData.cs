// ============================================================================
// BONUS CARD DATA - WITH DRAFT SYSTEM METADATA
// ✅ Added bonusCategory, battleOnly flag
// ✅ Maintains compatibility with existing system
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
    [Tooltip("Stamina cost (was pipCost)")]
    public int staminaCost = 1;

    [Header("Effect")]
    public BonusEffectType effectType;
    public float effectValue;
    public UnitType targetUnitType;

    [Header("Draft System Metadata")]
    [Tooltip("Category for offer diversity")]
    public BonusCategory bonusCategory = BonusCategory.Power;

    [Tooltip("If true, effects only last for current battle")]
    public bool battleOnly = true;

    [Tooltip("Rarity tier for weighting")]
    public BonusRarityTier rarityTier = BonusRarityTier.Common;

    [Header("Visual")]
    public Sprite cardSprite;

    // Backward compatibility
    [System.Obsolete("Use staminaCost instead")]
    public int pipCost
    {
        get => staminaCost;
        set => staminaCost = value;
    }
}
