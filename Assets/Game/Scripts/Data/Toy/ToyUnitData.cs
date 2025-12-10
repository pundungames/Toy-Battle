// ============================================================================
// TOY UNIT DATA - WITH COMBAT RANGE
// ✅ Each character has their own attack range and speed
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
    public Sprite[] animationFrames = new Sprite[3];

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

    // Recommended values:
    // MELEE (Guardian Golem, Maximus, Slam Bros): 
    //   - attackRange: 1.5f, moveSpeed: 2f, attackCooldown: 1f
    // RANGED (Toy Soldier, Gunners Brigade):
    //   - attackRange: 5f, moveSpeed: 1.5f, attackCooldown: 0.8f
    // ASSASSIN (Shell Ninja):
    //   - attackRange: 1.2f, moveSpeed: 3f, attackCooldown: 0.6f
    // EXPLOSIVE (Blast Racer, Kaboom Tanklet):
    //   - attackRange: 2f, moveSpeed: 1.8f, attackCooldown: 1.5f

    [Header("Stack Settings")]
    [Tooltip("Max number of this unit per slot")]
    public int maxStackPerSlot = 9;
    public float unitSpacing;

    [Header("Economy")]
    public int toyPrice;

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