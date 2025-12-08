// ============================================================================
// TOY UNIT DATA - ScriptableObject
// Her oyuncak unit'in verilerini tutar (He-Man, Toy Soldier, vb.)
// ============================================================================

using UnityEngine;

[CreateAssetMenu(fileName = "ToyUnit", menuName = "CardGame/Toy Unit Data")]
public class ToyUnitData : ScriptableObject
{
    [Header("Basic Info")]
    public string unitID;
    public string toyName;
    [TextArea] public string toyInfo;
    public int level = 1; // 1, 2, 3

    [Header("Stats")]
    public int baseHP;
    public int baseDamage;

    [Header("Type & Rarity")]
    public UnitType unitType;
    public RarityType toyRarityType;

    [Header("Visual")]
    public Sprite toySprite; // Kart görseli
    public Sprite[] animationFrames = new Sprite[3]; // 3-frame GIF animasyon

    [Header("Special Abilities")]
    public bool hasTeleport; // Assassin (TMNT)
    public bool isExplosive; // Explosive Car
    public int explosionDamage; // AoE damage amount
    public bool hasSupport; // Support units (Skeletor)

    [Header("Economy")]
    public int toyPrice; // Draft'ta satýn alma fiyatý

    // Level scaling
    public int GetScaledHP()
    {
        return baseHP * level;
    }

    public int GetScaledDamage()
    {
        return baseDamage * level;
    }
}