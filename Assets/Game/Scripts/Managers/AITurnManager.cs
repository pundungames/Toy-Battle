// ============================================================================
// AI TURN MANAGER - AI'ın draft turn'ünü yönetir
// Player seçim yaptıktan sonra AI otomatik seçim yapar
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class AITurnManager : MonoBehaviour
{
    [Inject] AIController aiController;
    [Inject] GridManager gridManager;
    [Inject] BonusSystem bonusSystem;
    [Inject] UnlockSystem unlockSystem;

    [Header("AI Settings")]
    [SerializeField] float aiThinkDelay = 1f; // AI karar verme süresi
    [SerializeField] float aiActionDelay = 0.5f; // AI action sonrası bekleme

    [Header("Card Pool")]
    [SerializeField] List<ToyUnitData> allToyUnits;
    [SerializeField] List<BonusCardData> allBonusCards;

    [Header("Rarity Weights")]
    [SerializeField] int commonWeight = 70;
    [SerializeField] int uncommonWeight = 25;
    [SerializeField] int rareWeight = 5;

    private bool isAITurnActive = false;

    // ===== START AI TURN =====

    public void StartAITurn()
    {
        if (isAITurnActive)
        {
            Debug.LogWarning("AI turn already active!");
            return;
        }

        isAITurnActive = true;
        StartCoroutine(ExecuteAITurnCoroutine());
    }

    private IEnumerator ExecuteAITurnCoroutine()
    {
        Debug.Log("🤖 AI is thinking...");

        // AI thinking delay
        yield return new WaitForSeconds(aiThinkDelay);

        // Generate AI draft cards (same logic as player)
        List<object> aiDraftCards = GenerateAIDraftCards();

        // AI makes decision
        object selectedCard = aiController.SelectCard(aiDraftCards);

        Debug.Log($"🤖 AI selected: {GetCardName(selectedCard)}");

        // Execute AI action
        ExecuteAIAction(selectedCard);

        // Wait before completing turn
        yield return new WaitForSeconds(aiActionDelay);

        // AI turn complete
        isAITurnActive = false;
        OnAITurnComplete();
    }

    // ===== GENERATE AI CARDS =====

    private List<object> GenerateAIDraftCards()
    {
        List<object> cards = new List<object>();

        // Get unlocked units
        List<ToyUnitData> unlockedUnits = unlockSystem.GetUnlockedUnits(allToyUnits);
        List<ToyUnitData> availableUnits = new List<ToyUnitData>(unlockedUnits);

        // Add 2 unique toy units
        for (int i = 0; i < 2; i++)
        {
            if (availableUnits.Count == 0) break;

            ToyUnitData randomUnit = GetWeightedRandomUnit(availableUnits);
            if (randomUnit != null)
            {
                cards.Add(randomUnit);
                availableUnits.Remove(randomUnit);
            }
        }

        // 15% chance for bonus card
        if (Random.value < 0.15f && allBonusCards.Count > 0)
        {
            BonusCardData randomBonus = allBonusCards[Random.Range(0, allBonusCards.Count)];
            cards.Add(randomBonus);
        }
        else
        {
            if (availableUnits.Count > 0)
            {
                ToyUnitData randomUnit = GetWeightedRandomUnit(availableUnits);
                if (randomUnit != null)
                {
                    cards.Add(randomUnit);
                }
            }
        }

        return cards;
    }

    private ToyUnitData GetWeightedRandomUnit(List<ToyUnitData> units)
    {
        if (units.Count == 0) return null;

        int totalWeight = 0;
        Dictionary<ToyUnitData, int> weights = new Dictionary<ToyUnitData, int>();

        foreach (var unit in units)
        {
            int weight = unit.toyRarityType switch
            {
                RarityType.Common => commonWeight,
                RarityType.Uncommon => uncommonWeight,
                RarityType.Rare => rareWeight,
                _ => 1
            };

            weights.Add(unit, weight);
            totalWeight += weight;
        }

        int randomValue = Random.Range(0, totalWeight);
        foreach (var kvp in weights)
        {
            randomValue -= kvp.Value;
            if (randomValue <= 0)
                return kvp.Key;
        }

        return units[0];
    }

    // ===== EXECUTE AI ACTION =====

    private void ExecuteAIAction(object selectedCard)
    {
        if (selectedCard is ToyUnitData unitData)
        {
            // Spawn unit for enemy (isPlayer = false)
            bool spawned = gridManager.SpawnUnit(unitData, false);

            if (!spawned)
            {
                Debug.LogWarning("🤖 AI: Grid is full! Cannot spawn unit.");
            }
        }
        else if (selectedCard is BonusCardData bonusData)
        {
            // AI uses bonus (we can add AI bonus logic later)
            Debug.Log($"🤖 AI used bonus: {bonusData.bonusName}");
            // bonusSystem.ApplyBonusToEnemy(bonusData);
        }
    }

    // ===== AI TURN COMPLETE =====

    private void OnAITurnComplete()
    {
        Debug.Log("🤖 AI turn complete!");

        // Notify that both turns are complete, game can continue
        EventManager.OnDraftComplete();
    }

    // ===== HELPERS =====

    private string GetCardName(object card)
    {
        if (card is ToyUnitData unitData)
            return unitData.toyName;
        else if (card is BonusCardData bonusData)
            return bonusData.bonusName;
        else
            return "Unknown";
    }

    public bool IsAITurnActive()
    {
        return isAITurnActive;
    }
}