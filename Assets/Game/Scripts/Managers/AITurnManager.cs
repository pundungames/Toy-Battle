// ============================================================================
// AI TURN MANAGER - WITH OFFER GENERATOR
// ✅ Phase 3: AI uses OfferGenerator like player
// ✅ Counter-based selection with stamina profiles
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

public class AITurnManager : MonoBehaviour
{
    [Inject] AIController aiController;
    [Inject] GridManager gridManager;
    [Inject] BonusSystem bonusSystem;
    [Inject] UnlockSystem unlockSystem;
    [Inject] GameManager gameManager;

    [Header("AI Settings")]
    [SerializeField] float aiThinkDelay = 1f;
    [SerializeField] float aiActionDelay = 0.5f;

    [Header("Card Pool")]
    [SerializeField] List<ToyUnitData> allToyUnits;
    [SerializeField] List<BonusCardData> allBonusCards;

    [Header("AI Difficulty")]
    [SerializeField] AIDifficulty difficulty = AIDifficulty.Normal;

    private bool isAITurnActive = false;
    private OfferGenerator offerGenerator;

    // ===== INITIALIZATION =====

    private void Start()
    {
        offerGenerator = new OfferGenerator(allToyUnits, allBonusCards, gridManager);
        Debug.Log("✅ AI OfferGenerator initialized");
    }

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

        yield return new WaitForSeconds(aiThinkDelay);

        // Generate AI draft cards using OfferGenerator
        List<object> aiDraftCards = GenerateAIDraftCards();

        if (aiDraftCards.Count == 0)
        {
            Debug.LogWarning("⚠️ AI: No cards available!");
            OnAITurnComplete();
            yield break;
        }

        // AI selects best card based on counter score
        object selectedCard = SelectBestCard(aiDraftCards);

        Debug.Log($"🤖 AI selected: {GetCardName(selectedCard)}");

        yield return new WaitForSeconds(aiActionDelay);

        // Execute AI action
        ExecuteAIAction(selectedCard);

        yield return new WaitForSeconds(aiActionDelay);

        OnAITurnComplete();
    }

    // ===== CARD GENERATION WITH OFFER GENERATOR =====

    private List<object> GenerateAIDraftCards()
    {
        if (offerGenerator == null)
        {
            Debug.LogError("❌ AI OfferGenerator is NULL!");
            return new List<object>();
        }

        int turn = gameManager.currentTurn;
        int battleTurn = gameManager.currentBattleTurn;

        int loopIndex = battleTurn - 1;
        int roundIndex = turn - (loopIndex * 5);
        if (roundIndex > 4) roundIndex = 4;

        Debug.Log($"📊 AI Draft: Turn {turn}, Round {roundIndex}/4, Loop {loopIndex}");

        // Get AI's owned units
        List<ToyUnitData> ownedUnits = gridManager.GetEnemyUnits()
            .Where(u => u != null && u.data != null)
            .Select(u => u.data)
            .Distinct()
            .ToList();

        // AI always has full stamina (doesn't carry over)
        int aiStamina = 10;

        List<object> offer = offerGenerator.GenerateUnitOffer(
            isPlayer: false,
            roundIndex: roundIndex,
            loopIndex: loopIndex,
            remainingStamina: aiStamina,
            ownedUnits: ownedUnits
        );

        Debug.Log($"✅ AI received {offer.Count} cards");

        return offer;
    }

    // ===== AI CARD SELECTION =====

    private object SelectBestCard(List<object> cards)
    {
        // Get player's units for counter calculation
        List<RuntimeUnit> playerUnits = gridManager.GetPlayerUnits();

        object bestCard = null;
        float bestScore = float.MinValue;

        foreach (var card in cards)
        {
            if (card is ToyUnitData unitData)
            {
                float score = CalculateUnitScore(unitData, playerUnits);

                // Add randomness based on difficulty
                float randomFactor = GetRandomFactor();
                score *= randomFactor;

                Debug.Log($"  AI Eval: {unitData.toyName} → Score: {score:F2}");

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCard = card;
                }
            }
            else if (card is BonusCardData bonusData)
            {
                // Bonus cards have fixed score
                float score = 5f * GetRandomFactor();

                Debug.Log($"  AI Eval: {bonusData.bonusName} → Score: {score:F2}");

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCard = card;
                }
            }
        }

        return bestCard;
    }

    private float CalculateUnitScore(ToyUnitData unitData, List<RuntimeUnit> playerUnits)
    {
        int counterScore = CounterMatrix.CalculateCounterScore(unitData.unitID, playerUnits);

        // Easy mode: Ignore counters completely
        if (difficulty == AIDifficulty.Easy)
        {
            counterScore = 5; // Neutral score for everyone
        }
        // Hard mode: Emphasize counters
        else if (difficulty == AIDifficulty.Hard)
        {
            counterScore = Mathf.RoundToInt(counterScore * 1.5f);
        }

        float rarityBonus = unitData.toyRarityType switch
        {
            RarityType.Common => 1.0f,
            RarityType.Rare => 1.5f,
            RarityType.Epic => 2.0f,
            _ => 1.0f
        };

        return counterScore * rarityBonus;
    }

    private float GetRandomFactor()
    {
        return difficulty switch
        {
            AIDifficulty.Easy => Random.Range(0.4f, 0.8f),    // Very random (tutorial)
            AIDifficulty.Normal => Random.Range(0.7f, 1.3f),  // Balanced
            AIDifficulty.Hard => Random.Range(0.9f, 1.1f),    // Nearly optimal
            _ => 1.0f
        };
    }

    // ===== EXECUTE AI ACTION =====

    private void ExecuteAIAction(object card)
    {
        if (card is ToyUnitData unitData)
        {
            bool spawned = gridManager.SpawnUnit(unitData, false);

            if (spawned)
            {
                Debug.Log($"✅ AI spawned: {unitData.toyName}");
            }
            else
            {
                Debug.LogWarning($"⚠️ AI failed to spawn: {unitData.toyName}");
            }

            FixEnemyUnitsRotation();
        }
        else if (card is BonusCardData bonusData)
        {
            bonusSystem.ApplyBonus(bonusData);
            Debug.Log($"✅ AI applied bonus: {bonusData.bonusName}");
        }
    }

    private void FixEnemyUnitsRotation()
    {
        List<RuntimeUnit> enemyUnits = gridManager.GetEnemyUnits();
        int fixedCount = 0;

        foreach (var unit in enemyUnits)
        {
            if (unit != null && unit.gameObject != null)
            {
                unit.transform.rotation = Quaternion.Euler(0, 180, 0);
                fixedCount++;
            }
        }

        Debug.Log($"🔄 Fixed rotation for {fixedCount} enemy units");
    }

    // ===== HELPERS =====

    private string GetCardName(object card)
    {
        if (card is ToyUnitData unit) return unit.toyName;
        if (card is BonusCardData bonus) return bonus.bonusName;
        return "Unknown";
    }

    private void OnAITurnComplete()
    {
        isAITurnActive = false;
        Debug.Log("🤖 AI turn complete!");
        EventManager.OnDraftComplete();
    }
}

public enum AIDifficulty
{
    Easy,
    Normal,
    Hard
}