// ============================================================================
// GAME ENUMS & CONSTANTS
// Tüm game state'ler ve sabitler burada tanımlanır
// ============================================================================

using UnityEngine;

public static class GameConstants
{
    public const int TOTAL_TURNS = 30;
    public const int GRID_SIZE = 6;
    public const float COMBAT_TICK_INTERVAL = 0.5f;
    public const int CARDS_PER_DRAFT = 3;
    public const int STARTING_UNITS = 4;
    public const int WINS_PER_UNLOCK = 3;
    public const float CHEST_DROP_CHANCE = 0.4f;
    public const int CHEST_CARD_COUNT = 3;
    public const float RARE_CHANCE = 0.15f;
    public const int WIN_GOLD = 8;
    public const int LOSE_GOLD = 3;
    public const int CHEST_COST = 30;
    public const int PIP_PER_TURN = 2;
}

public enum GameState
{
    MainMenu,
    Draft,
    Battle,
    Reward,
    Chest,
    Progress
}

public enum UnitType
{
    Melee,
    Ranged,
    Assassin,
    Explosive,
    Support
}

public enum RarityType
{
    Common,
    Uncommon,
    Rare
}

public enum BotDifficulty
{
    Tutorial,
    Easy,
    Normal,
    Hard
}

public enum SkillType
{
    Attack,
    Defense,
    Special
}

public enum BonusEffectType
{
    DamageBoost,      // +20% damage
    Shield,           // +20% shield
    FirstAttackCancel,// Cancel first attack
    Poison,           // 5 tick poison
    DoubleDeploy,     // Deploy twice
    GroupBuff,        // Buff all of type
    UltimateShield,   // Team shield
    ExpandSlot        // +1 deploy limit
}