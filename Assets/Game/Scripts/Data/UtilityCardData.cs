// ============================================================================
// UTILITY CARD DATA - LevelUp, Plus, Multiplier cards
// ============================================================================

using UnityEngine;

[CreateAssetMenu(fileName = "UtilityCard", menuName = "CardGame/Utility Card Data")]
public class UtilityCardData : ScriptableObject
{
    [Header("Basic Info")]
    public string cardID;
    public string cardName;
    [TextArea] public string cardDescription;

    [Header("Utility Type")]
    public UtilityType utilityType;

    [Header("Effect Values")]
    [Tooltip("For LevelUp: target level (2 or 3). For Plus: unit count (+2, +3). For X2: multiplier (2)")]
    public int effectValue;

    [Header("Targeting")]
    [Tooltip("Target unit - REQUIRED! This utility is for this specific unit")]
    public ToyUnitData targetUnit;

    [Header("Availability")]
    [Tooltip("When this utility can appear (Early, Mid, Epic)")]
    public GamePhase gamePhase = GamePhase.Early;

    [Header("Economy")]
    [Tooltip("Stamina cost to select this card")]
    public int staminaCost = 1;

    [Header("Availability Gate")]
    [Tooltip("Minimum units owned before this card can appear")]
    public int minUnitsOwned = 0;

    [Header("Visual")]
    public Sprite cardSprite;

    [Header("Rarity")]
    public UtilityCardRarity rarity = UtilityCardRarity.Common;
}

// ============================================================================
// UTILITY CARD ENUMS
// ============================================================================

public enum UtilityType
{
    LevelUp,      // Increase unit level (Lv2, Lv3)
    CountAdd,     // Add units to slot (Plus2, Plus3)
    Multiplier    // Multiply unit count (X2)
}

public enum UtilityCardRarity
{
    Common,   // Plus2, LevelUp_Lv2
    Rare,     // Plus3, LevelUp_Lv3
    Epic      // X2
}