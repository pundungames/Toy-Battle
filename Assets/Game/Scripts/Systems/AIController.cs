// ============================================================================
// AI CONTROLLER - Fake PvP AI sistemi
// Tutorial, Easy, Normal, Hard botlar
// ============================================================================

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

public class AIController : MonoBehaviour
{
    [Inject] GridManager gridManager;
    [Inject] BonusSystem bonusSystem;
    
    [Header("Settings")]
    [SerializeField] BotDifficulty difficulty = BotDifficulty.Normal;
    [SerializeField] float decisionDelay = 1f;
    
    // ===== SELECT CARD (AI Decision) =====
    
    public object SelectCard(List<object> availableCards)
    {
        switch (difficulty)
        {
            case BotDifficulty.Tutorial:
                return SelectTutorialCard(availableCards);
            
            case BotDifficulty.Easy:
                return SelectRandomCard(availableCards);
            
            case BotDifficulty.Normal:
                return SelectStatBasedCard(availableCards);
            
            case BotDifficulty.Hard:
                return SelectSynergyCard(availableCards);
            
            default:
                return availableCards[0];
        }
    }
    
    // ===== TUTORIAL BOT (Always Lose) =====
    
    private object SelectTutorialCard(List<object> cards)
    {
        // Select weakest card
        object weakestCard = cards[0];
        int lowestScore = int.MaxValue;
        
        foreach (var card in cards)
        {
            int score = EvaluateCard(card);
            if (score < lowestScore)
            {
                lowestScore = score;
                weakestCard = card;
            }
        }
        
        return weakestCard;
    }
    
    // ===== EASY BOT (Random) =====
    
    private object SelectRandomCard(List<object> cards)
    {
        return cards[Random.Range(0, cards.Count)];
    }
    
    // ===== NORMAL BOT (Stat-Based) =====
    
    private object SelectStatBasedCard(List<object> cards)
    {
        object bestCard = cards[0];
        int bestScore = 0;
        
        foreach (var card in cards)
        {
            int score = EvaluateCard(card);
            
            if (score > bestScore)
            {
                bestScore = score;
                bestCard = card;
            }
        }
        
        return bestCard;
    }
    
    private int EvaluateCard(object card)
    {
        int score = 0;
        
        if (card is ToyUnitData unitData)
        {
            score = unitData.GetScaledHP() + unitData.GetScaledDamage() * 2;
            
            // Bonus for special abilities
            if (unitData.hasTeleport) score += 20;
            if (unitData.isExplosive) score += 15;
            if (unitData.hasSupport) score += 10;
        }
        else if (card is BonusCardData bonusData)
        {
            // Medium priority for bonuses
            score = 50;
            
            if (bonusData.pipCost == 2) score += 20; // Higher value for 2-pip bonuses
        }
        
        return score;
    }
    
    // ===== HARD BOT (Synergy-Based) =====
    
    private object SelectSynergyCard(List<object> cards)
    {
        // Check current board composition
        List<RuntimeUnit> currentUnits = gridManager.GetEnemyUnits();
        
        // Count unit types
        Dictionary<UnitType, int> typeCount = new Dictionary<UnitType, int>();
        
        foreach (var unit in currentUnits)
        {
            if (!typeCount.ContainsKey(unit.data.unitType))
            {
                typeCount[unit.data.unitType] = 0;
            }
            typeCount[unit.data.unitType]++;
        }
        
        // Select card with synergy
        object bestCard = cards[0];
        int bestScore = 0;
        
        foreach (var card in cards)
        {
            int score = EvaluateCard(card);
            
            // Synergy bonus
            if (card is ToyUnitData unitData)
            {
                if (typeCount.ContainsKey(unitData.unitType))
                {
                    score += typeCount[unitData.unitType] * 30; // Big synergy bonus
                }
            }
            else if (card is BonusCardData bonusData)
            {
                // Prefer group buffs if we have many of that type
                if (bonusData.effectType == BonusEffectType.GroupBuff)
                {
                    if (typeCount.ContainsKey(bonusData.targetUnitType))
                    {
                        score += typeCount[bonusData.targetUnitType] * 40;
                    }
                }
            }
            
            if (score > bestScore)
            {
                bestScore = score;
                bestCard = card;
            }
        }
        
        return bestCard;
    }
    
    // ===== AI TURN (Called by GameManager) =====
    
    public void ExecuteAITurn(List<object> availableCards)
    {
        Invoke(nameof(DelayedAIDecision), decisionDelay);
    }
    
    private void DelayedAIDecision()
    {
        // AI makes decision here
        Debug.Log("AI is thinking...");
    }
    
    public void SetDifficulty(BotDifficulty newDifficulty)
    {
        difficulty = newDifficulty;
    }
}