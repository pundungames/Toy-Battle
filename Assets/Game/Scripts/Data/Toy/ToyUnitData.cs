// ============================================================================
// TOY UNIT DATA - WITH FORMATION SYSTEM
// ✅ arrangementIndex: Position priority (0-100, higher = back)
// ✅ maxUnitsPerRow: Row capacity for overflow handling
// ✅ unitSpacing: Already exists (inter-unit spacing)
// ============================================================================

using UnityEngine;

[CreateAssetMenu(fileName = "ToyUnit", menuName = "CardGame/Toy Unit Data")]
public class ToyUnitData : ScriptableObject
{
    [Header("Basic Info")]
    public string unitID;
    public string toyName;
    [TextArea] public string toyInfo;
    public int level = 1;

    [Header("Stats")]
    public int baseHP;
    public int baseDamage;

    [Header("Type & Rarity")]
    public UnitType unitType;
    public RarityType toyRarityType;

    [Header("Visual")]
    public Sprite toySprite;
    public Sprite[] animationFrames;

    [Header("Special Abilities")]
    public bool hasTeleport;
    public bool isExplosive;
    public int explosionDamage;
    public bool hasSupport;

    [Header("Combat Settings")]
    [Tooltip("Attack range in world units")]
    public float attackRange = 2f;
    [Tooltip("Movement speed")]
    public float moveSpeed = 2f;
    [Tooltip("Time between attacks (seconds)")]
    public float attackCooldown = 1f;

    [Header("Formation Settings")]
    [Tooltip("Position priority (0-100). Higher = Further back")]
    [Range(0, 100)]
    public int arrangementIndex = 50;

    [Tooltip("Maximum units in single horizontal row")]
    [Range(2, 30)]
    public int maxUnitsPerRow = 4;

    [Tooltip("Spacing between units of this type")]
    public float unitSpacing = 1f;

    // Recommended formation values by type:
    // TANK (Guardian Golem): arrangementIndex: 100, maxUnitsPerRow: 4, spacing: 1.5f
    // FIGHTER (Maximus): arrangementIndex: 60, maxUnitsPerRow: 4, spacing: 1.0f
    // RANGED (Toy Soldier): arrangementIndex: 50, maxUnitsPerRow: 6, spacing: 0.8f
    // ASSASSIN (Shell Ninja): arrangementIndex: 10, maxUnitsPerRow: 3, spacing: 1.0f

    [Header("Stack Settings (Legacy - Draft Only)")]
    [Tooltip("Max number of this unit per slot (used during draft)")]
    public int maxStackPerSlot = 9;

    [Header("Economy")]
    public int toyStamina;

    [Header("Multi-Unit System")]
    [Tooltip("If true, spawns multiple units per slot (Punchy Bots, Slam Bros, MiniBoy)")]
    public bool isMultiUnit = false;

    [Tooltip("How many units spawn per slot")]
    [Range(1, 3)]
    public int unitsPerSlot = 1;

    [Header("Twin System (for Punchy Bots)")]
    [Tooltip("If true, units share target and attack counter")]
    public bool isTwinSystem = false;

    [Header("UI")]
    public Sprite typeSprite;
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