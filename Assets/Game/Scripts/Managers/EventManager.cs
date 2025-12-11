// ============================================================================
// EVENT MANAGER
// Tüm oyun eventleri burada tanýmlanýr - Event-driven architecture
// ============================================================================

using System;
using UnityEngine;
using UnityEngine.Events;
using Zenject.Asteroids;

public static class EventManager
{
    // Game State Events
    public static event Action<GameState> onGameStateChange;
    public static event Action<int> onTurnChange;

    // Draft Events
    public static event Action onDraftStart;
    public static event Action<ToyUnitData> onCardSelected;
    public static event Action onDraftComplete;

    // Battle Events
    public static event Action onBattleStart;
    public static event Action<bool> onBattleComplete; // bool: playerWon
    public static event Action<RuntimeUnit> onUnitSpawn;
    public static event Action<RuntimeUnit> onUnitDeath;

    // Bonus Events
    public static event Action<BonusCardData> onBonusApplied;

    // Chest & Merge Events
    public static event Action onChestOpen;
    public static event Action<ToyUnitData> onCardMerged;

    // Currency Events
    public static event Action<int> onGoldChanged;

    // Tutorial Events
    public static event Action onTutorialStart;
    public static event Action onTutorialComplete;

    // UI Events
    public static event Action onReroll;

    public static event UnityAction DailyRewardClaimed;
    public static void OnDailyRewardClaimed() => DailyRewardClaimed?.Invoke();


    public static event UnityAction levelStart;
    public static void OnLevelStart() => levelStart?.Invoke();

    public static event UnityAction levelFail;
    public static void OnLevelFail() => levelFail?.Invoke();

    public static event UnityAction levelComplete;
    public static void OnLevelComplete() => levelComplete?.Invoke();

    // ===== INVOKERS =====

    public static void OnGameStateChange(GameState newState)
    {
        onGameStateChange?.Invoke(newState);
    }

    public static void OnTurnChange(int turnNumber)
    {
        onTurnChange?.Invoke(turnNumber);
    }

    public static void OnDraftStart()
    {
        onDraftStart?.Invoke();
    }

    public static void OnCardSelected(ToyUnitData unitData)
    {
        onCardSelected?.Invoke(unitData);
    }

    public static void OnDraftComplete()
    {
        onDraftComplete?.Invoke();
    }

    public static void OnBattleStart()
    {
        onBattleStart?.Invoke();
    }

    public static void OnBattleComplete(bool playerWon)
    {
        onBattleComplete?.Invoke(playerWon);
    }

    public static void OnUnitSpawn(RuntimeUnit unit)
    {
        onUnitSpawn?.Invoke(unit);
    }

    public static void OnUnitDeath(RuntimeUnit unit)
    {
        onUnitDeath?.Invoke(unit);
    }

    public static void OnBonusApplied(BonusCardData bonus)
    {
        onBonusApplied?.Invoke(bonus);
    }

    public static void OnChestOpen()
    {
        onChestOpen?.Invoke();
    }

    public static void OnCardMerged(ToyUnitData mergedCard)
    {
        onCardMerged?.Invoke(mergedCard);
    }

    public static void OnGoldChanged(int newAmount)
    {
        onGoldChanged?.Invoke(newAmount);
    }

    public static void OnTutorialStart()
    {
        onTutorialStart?.Invoke();
    }

    public static void OnTutorialComplete()
    {
        onTutorialComplete?.Invoke();
    }

    public static void OnReroll()
    {
        onReroll?.Invoke();
    }
}