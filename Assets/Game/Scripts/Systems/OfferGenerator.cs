// ============================================================================
// OFFER GENERATOR - 3-Card Draft System
// ✅ Slot templates (Frontline + Backline + Tempo Neutral)
// ✅ Pool gating (Early/Mid/Epic)
// ✅ Rarity odds per round
// ✅ Anti-spam rules
// ✅ Stamina filtering
// ============================================================================

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OfferGenerator
{
    // ===== CONFIGURATION =====

    private List<ToyUnitData> allUnits;
    private List<BonusCardData> allBonusCards;

    // NOTE: Utility cards are now pulled from ToyUnitData.utilityCards
    // No longer need separate allUtilityCards list

    // ===== OFFER HISTORY (per side) =====

    public class OfferHistory
    {
        public List<string> lastOfferedUnitIDs = new List<string>();
        public List<string> lastOfferedUtilityIDs = new List<string>();
        public List<string> lastOfferedBonusIDs = new List<string>();
        public HashSet<string> rareOfferedThisRun = new HashSet<string>();
        public int lastRareOfferedRound = -1;

        public void Clear()
        {
            lastOfferedUnitIDs.Clear();
            lastOfferedUtilityIDs.Clear();
            lastOfferedBonusIDs.Clear();
            rareOfferedThisRun.Clear();
            lastRareOfferedRound = -1;
        }
    }

    public OfferHistory playerHistory = new OfferHistory();
    public OfferHistory enemyHistory = new OfferHistory();

    // ===== CONSTRUCTOR =====

    public OfferGenerator(List<ToyUnitData> units, List<BonusCardData> bonuses)
    {
        allUnits = units;
        allBonusCards = bonuses;
    }

    // ===== GENERATE UNIT OFFER (Rounds 1-3) =====

    /// <summary>
    /// Generate 3-card unit offer with slot template
    /// </summary>
    /// <param name="isPlayer">Player or Enemy side</param>
    /// <param name="roundIndex">1, 2, or 3 (unit draft rounds)</param>
    /// <param name="loopIndex">1, 2, or 3 (match loop)</param>
    /// <param name="remainingStamina">Available stamina for filtering</param>
    /// <param name="ownedUnits">Units currently owned (for utility gates)</param>
    public List<object> GenerateUnitOffer(bool isPlayer, int roundIndex, int loopIndex, int remainingStamina, List<ToyUnitData> ownedUnits)
    {
        OfferHistory history = isPlayer ? playerHistory : enemyHistory;
        List<object> offer = new List<object>();

        Debug.Log($"🎴 Generating Unit Offer | Side: {(isPlayer ? "PLAYER" : "ENEMY")} | Round: {roundIndex} | Loop: {loopIndex} | Stamina: {remainingStamina}");

        // ===== CHECK IF GRID IS FULL WITH DIFFERENT UNITS =====
        // If all slots filled with different unit types, only offer utility cards
        bool gridFullWithDifferentUnits = CheckGridFullWithDifferentUnits(ownedUnits);

        if (gridFullWithDifferentUnits)
        {
            Debug.Log("⚠️ Grid full with different units → Offering ONLY utility cards");

            // Generate 3 utility cards
            for (int i = 0; i < 3; i++)
            {
                UtilityCardData utility = SelectUtilityCard(remainingStamina, history, ownedUnits, roundIndex, loopIndex);
                if (utility != null)
                {
                    offer.Add(utility);
                    Debug.Log($"  Slot {i + 1} (UTILITY): {utility.cardName} for {utility.targetUnit.toyName}");
                }
            }

            if (offer.Count == 0)
            {
                Debug.LogWarning("⚠️ No utility cards available! Grid full but can't offer utilities.");
            }

            UpdateHistoryAfterOffer(history, offer, roundIndex);
            Debug.Log($"✅ Utility-only offer generated: {offer.Count} cards");
            return offer;
        }

        // ===== DETERMINE SLOT TEMPLATE =====

        UnitRole slotARole, slotBRole;
        List<UnitRole> slotCRoles;

        if (roundIndex <= 2)
        {
            // Rounds 1-2: Stabilization
            slotARole = UnitRole.Frontline;
            slotBRole = UnitRole.Backline;
            slotCRoles = new List<UnitRole> { UnitRole.Frontline, UnitRole.Backline, UnitRole.Swarm }; // Tempo Neutral
        }
        else
        {
            // Round 3: Decision
            slotARole = UnitRole.Frontline; // or Swarm
            slotBRole = UnitRole.Backline; // or Support
            slotCRoles = new List<UnitRole> { UnitRole.Burst, UnitRole.AOE, UnitRole.Assassin, UnitRole.Support, UnitRole.Scaling }; // Rare Candidate
        }

        // ===== GENERATE SLOT A =====
        ToyUnitData slotA = SelectUnitForSlot(slotARole, roundIndex, loopIndex, remainingStamina, history, new List<UnitRole>());
        if (slotA != null)
        {
            offer.Add(slotA);
            Debug.Log($"  Slot A ({slotARole}): {slotA.toyName} [{slotA.toyRarityType}] ({slotA.toyStamina} stamina)");
        }

        // ===== GENERATE SLOT B =====
        List<UnitRole> usedRoles = new List<UnitRole> { slotA?.unitRole ?? UnitRole.Frontline };
        ToyUnitData slotB = SelectUnitForSlot(slotBRole, roundIndex, loopIndex, remainingStamina, history, usedRoles);
        if (slotB != null)
        {
            offer.Add(slotB);
            usedRoles.Add(slotB.unitRole);
            Debug.Log($"  Slot B ({slotBRole}): {slotB.toyName} [{slotB.toyRarityType}] ({slotB.toyStamina} stamina)");
        }

        // ===== GENERATE SLOT C (Unit or Utility) =====

        // Check if utility card should appear (ONLY FOR PLAYER)
        float utilityChance = isPlayer ? GetUtilityAppearanceChance(roundIndex, ownedUnits.Count) : 0.0f;
        bool shouldOfferUtility = Random.value < utilityChance;

        if (shouldOfferUtility)
        {
            UtilityCardData utility = SelectUtilityCard(remainingStamina, history, ownedUnits, roundIndex, loopIndex);
            if (utility != null)
            {
                offer.Add(utility);
                Debug.Log($"  Slot C (UTILITY): {utility.cardName} ({utility.staminaCost} stamina)");
            }
            else
            {
                // Fallback to unit if no valid utility
                ToyUnitData slotC = SelectUnitForSlot(slotCRoles[Random.Range(0, slotCRoles.Count)], roundIndex, loopIndex, remainingStamina, history, usedRoles);
                if (slotC != null)
                {
                    offer.Add(slotC);
                    Debug.Log($"  Slot C (Unit fallback): {slotC.toyName} [{slotC.toyRarityType}] ({slotC.toyStamina} stamina)");
                }
            }
        }
        else
        {
            // Normal unit in Slot C
            ToyUnitData slotC = SelectUnitForSlot(slotCRoles[Random.Range(0, slotCRoles.Count)], roundIndex, loopIndex, remainingStamina, history, usedRoles);
            if (slotC != null)
            {
                offer.Add(slotC);
                Debug.Log($"  Slot C ({slotC.unitRole}): {slotC.toyName} [{slotC.toyRarityType}] ({slotC.toyStamina} stamina)");
            }
        }

        // ===== UPDATE HISTORY =====
        UpdateHistoryAfterOffer(history, offer, roundIndex);

        Debug.Log($"✅ Unit offer generated: {offer.Count} cards");
        return offer;
    }

    // ===== GENERATE BONUS OFFER (Round 4) =====

    /// <summary>
    /// Generate 3-card bonus offer with category diversity
    /// </summary>
    public List<BonusCardData> GenerateBonusOffer(bool isPlayer, int loopIndex, int remainingStamina)
    {
        OfferHistory history = isPlayer ? playerHistory : enemyHistory;
        List<BonusCardData> offer = new List<BonusCardData>();

        Debug.Log($"🎴 Generating Bonus Offer | Side: {(isPlayer ? "PLAYER" : "ENEMY")} | Loop: {loopIndex} | Stamina: {remainingStamina}");

        // ===== SLOT TEMPLATE: Tempo + Power + Tech/Defense =====

        BonusCardData slotA = SelectBonusForCategory(BonusCategory.Tempo, loopIndex, remainingStamina, history, new List<BonusCategory>());
        if (slotA != null)
        {
            offer.Add(slotA);
            Debug.Log($"  Slot A (Tempo): {slotA.bonusName} [{slotA.rarityTier}] ({slotA.staminaCost} stamina)");
        }

        List<BonusCategory> usedCategories = new List<BonusCategory> { BonusCategory.Tempo };

        BonusCardData slotB = SelectBonusForCategory(BonusCategory.Power, loopIndex, remainingStamina, history, usedCategories);
        if (slotB != null)
        {
            offer.Add(slotB);
            usedCategories.Add(BonusCategory.Power);
            Debug.Log($"  Slot B (Power): {slotB.bonusName} [{slotB.rarityTier}] ({slotB.staminaCost} stamina)");
        }

        // Slot C: Tech or Defense
        BonusCategory slotCCategory = Random.value < 0.5f ? BonusCategory.Tech : BonusCategory.Defense;
        BonusCardData slotC = SelectBonusForCategory(slotCCategory, loopIndex, remainingStamina, history, usedCategories);
        if (slotC != null)
        {
            offer.Add(slotC);
            Debug.Log($"  Slot C ({slotCCategory}): {slotC.bonusName} [{slotC.rarityTier}] ({slotC.staminaCost} stamina)");
        }

        // ===== UPDATE HISTORY =====
        history.lastOfferedBonusIDs.Clear();
        foreach (var bonus in offer)
        {
            history.lastOfferedBonusIDs.Add(bonus.bonusID);
        }

        Debug.Log($"✅ Bonus offer generated: {offer.Count} cards");
        return offer;
    }

    // ===== UNIT SELECTION LOGIC =====

    private ToyUnitData SelectUnitForSlot(UnitRole targetRole, int roundIndex, int loopIndex, int remainingStamina, OfferHistory history, List<UnitRole> usedRoles)
    {
        // ===== STEP 1: Filter by phase =====
        List<ToyUnitData> phaseFiltered = FilterByPhase(allUnits, roundIndex, loopIndex);

        // ===== STEP 2: Filter by role =====
        List<ToyUnitData> roleFiltered = phaseFiltered.Where(u => u.unitRole == targetRole && !usedRoles.Contains(u.unitRole)).ToList();

        if (roleFiltered.Count == 0)
        {
            Debug.LogWarning($"⚠️ No units found for role {targetRole}, expanding search...");
            roleFiltered = phaseFiltered.Where(u => !usedRoles.Contains(u.unitRole)).ToList();
        }

        // ===== STEP 3: Filter by anti-spam rules =====
        List<ToyUnitData> antiSpamFiltered = roleFiltered.Where(u => !history.lastOfferedUnitIDs.Contains(u.unitID)).ToList();

        // Remove rares if rare was offered last round
        if (history.lastRareOfferedRound == roundIndex - 1)
        {
            antiSpamFiltered = antiSpamFiltered.Where(u => u.toyRarityType != RarityType.Rare).ToList();
        }

        // Remove rares already offered this run
        antiSpamFiltered = antiSpamFiltered.Where(u => u.toyRarityType != RarityType.Rare || !history.rareOfferedThisRun.Contains(u.unitID)).ToList();

        if (antiSpamFiltered.Count == 0)
        {
            Debug.LogWarning($"⚠️ Anti-spam filter too strict, relaxing...");
            antiSpamFiltered = roleFiltered;
        }

        // ===== STEP 4: Apply rarity odds =====
        ToyUnitData selected = SelectByRarityOdds(antiSpamFiltered, roundIndex);

        // ===== STEP 5: Stamina filter (prefer affordable) =====
        if (selected != null && selected.toyStamina > remainingStamina)
        {
            // Try to find affordable alternative
            ToyUnitData affordable = antiSpamFiltered.Where(u => u.toyStamina <= remainingStamina).OrderBy(x => Random.value).FirstOrDefault();
            if (affordable != null)
            {
                selected = affordable;
                Debug.Log($"  💰 Switched to affordable: {selected.toyName}");
            }
            else
            {
                Debug.LogWarning($"  ⚠️ No affordable units, keeping unaffordable: {selected.toyName}");
            }
        }

        return selected;
    }

    private ToyUnitData SelectByRarityOdds(List<ToyUnitData> candidates, int roundIndex)
    {
        if (candidates.Count == 0) return null;

        // Get rarity odds for this round
        float commonChance, rareChance, epicChance;

        if (roundIndex == 1)
        {
            commonChance = 0.95f;
            rareChance = 0.05f;
            epicChance = 0.0f;
        }
        else if (roundIndex == 2)
        {
            commonChance = 0.80f;
            rareChance = 0.20f;
            epicChance = 0.0f;
        }
        else // Round 3
        {
            commonChance = 0.60f;
            rareChance = 0.35f;
            epicChance = 0.05f;
        }

        // Roll for rarity
        float roll = Random.value;
        RarityType targetRarity;

        if (roll < epicChance)
            targetRarity = RarityType.Epic;
        else if (roll < epicChance + rareChance)
            targetRarity = RarityType.Rare;
        else
            targetRarity = RarityType.Common;

        // Try to find unit of target rarity
        List<ToyUnitData> rarityMatch = candidates.Where(u => u.toyRarityType == targetRarity).ToList();

        if (rarityMatch.Count > 0)
        {
            return rarityMatch[Random.Range(0, rarityMatch.Count)];
        }

        // Fallback: return random candidate
        return candidates[Random.Range(0, candidates.Count)];
    }

    // ===== UTILITY SELECTION LOGIC =====

    private UtilityCardData SelectUtilityCard(int remainingStamina, OfferHistory history, List<ToyUnitData> ownedUnits, int roundIndex, int loopIndex)
    {
        Debug.Log($"🔍 SelectUtilityCard called with {ownedUnits.Count} owned units, {remainingStamina} stamina, Round {roundIndex}");

        // Collect all utility cards from owned units
        List<UtilityCardData> allAvailableUtilities = new List<UtilityCardData>();

        foreach (var ownedUnit in ownedUnits)
        {
            if (ownedUnit == null)
            {
                Debug.LogWarning("⚠️ Null owned unit!");
                continue;
            }

            Debug.Log($"  Checking {ownedUnit.toyName} - has {(ownedUnit.utilityCards?.Count ?? 0)} utility cards");

            if (ownedUnit.utilityCards == null || ownedUnit.utilityCards.Count == 0)
            {
                Debug.LogWarning($"  ⚠️ {ownedUnit.toyName} has NO utility cards assigned!");
                continue;
            }

            // Get current count of this unit on grid
            int currentUnitCount = ownedUnits.Count(u => u != null && u.unitID == ownedUnit.unitID);
            int maxStackPerSlot = ownedUnit.maxStackPerSlot;

            foreach (var utility in ownedUnit.utilityCards)
            {
                if (utility == null)
                {
                    Debug.LogWarning($"  ⚠️ Null utility in {ownedUnit.toyName}.utilityCards");
                    continue;
                }

                Debug.Log($"    Found utility: {utility.cardName}");

                // Check if already offered recently
                if (history.lastOfferedUtilityIDs.Contains(utility.cardID))
                {
                    Debug.Log($"    ❌ Already offered recently");
                    continue;
                }

                // Check gates
                if (utility.minUnitsOwned > ownedUnits.Count)
                {
                    Debug.Log($"    ❌ Gate fail: needs {utility.minUnitsOwned} units, have {ownedUnits.Count}");
                    continue;
                }

                // ✅ CHECK PHASE GATING
                List<ToyUnitData> phaseFiltered = FilterByPhase(new List<ToyUnitData> { ownedUnit }, roundIndex, loopIndex);
                if (phaseFiltered.Count == 0)
                {
                    Debug.Log($"    ❌ Phase gate fail: utility is {utility.gamePhase}, not available in round {roundIndex}");
                    continue;
                }

                // ✅ CHECK MAX STACK
                if (utility.utilityType == UtilityType.CountAdd)
                {
                    int addAmount = utility.effectValue;
                    if (currentUnitCount + addAmount > maxStackPerSlot)
                    {
                        Debug.Log($"    ❌ Would exceed max stack: current {currentUnitCount} + {addAmount} > {maxStackPerSlot}");
                        continue;
                    }
                }
                else if (utility.utilityType == UtilityType.Multiplier)
                {
                    int newCount = currentUnitCount * utility.effectValue;
                    if (newCount > maxStackPerSlot)
                    {
                        Debug.Log($"    ❌ Would exceed max stack: {currentUnitCount} x {utility.effectValue} = {newCount} > {maxStackPerSlot}");
                        continue;
                    }
                }

                Debug.Log($"    ✅ Added to available list");
                allAvailableUtilities.Add(utility);
            }
        }

        Debug.Log($"📊 Total available utilities: {allAvailableUtilities.Count}");

        if (allAvailableUtilities.Count == 0)
        {
            Debug.LogWarning("⚠️ No utility cards available from owned units");
            return null;
        }

        // Prefer affordable
        List<UtilityCardData> affordable = allAvailableUtilities
            .Where(u => u.staminaCost <= remainingStamina)
            .ToList();

        Debug.Log($"💰 Affordable utilities: {affordable.Count}/{allAvailableUtilities.Count}");

        if (affordable.Count == 0)
        {
            Debug.LogWarning("⚠️ No affordable utility cards");
            return allAvailableUtilities[Random.Range(0, allAvailableUtilities.Count)];
        }

        // Weight by rarity (Common more likely)
        List<UtilityCardData> weighted = new List<UtilityCardData>();
        foreach (var card in affordable)
        {
            int weight = card.rarity == UtilityCardRarity.Common ? 3 :
                        card.rarity == UtilityCardRarity.Rare ? 2 : 1;
            for (int i = 0; i < weight; i++)
            {
                weighted.Add(card);
            }
        }

        UtilityCardData selected = weighted[Random.Range(0, weighted.Count)];
        Debug.Log($"✅ Selected utility: {selected.cardName} for {selected.targetUnit.toyName}");

        return selected;
    }

    // ===== BONUS SELECTION LOGIC =====

    private BonusCardData SelectBonusForCategory(BonusCategory category, int loopIndex, int remainingStamina, OfferHistory history, List<BonusCategory> usedCategories)
    {
        // Filter by category
        List<BonusCardData> categoryMatch = allBonusCards.Where(b =>
            b.bonusCategory == category &&
            !usedCategories.Contains(b.bonusCategory) &&
            !history.lastOfferedBonusIDs.Contains(b.bonusID)
        ).ToList();

        if (categoryMatch.Count == 0)
        {
            Debug.LogWarning($"⚠️ No bonuses for category {category}");
            return null;
        }

        // Apply epic chance multiplier based on loop
        float epicMultiplier = loopIndex == 1 ? 0.5f : loopIndex == 2 ? 1.0f : 1.5f;

        // Weight by rarity
        List<BonusCardData> weighted = new List<BonusCardData>();
        foreach (var bonus in categoryMatch)
        {
            float weight = bonus.rarityTier == BonusRarityTier.Common ? 1.0f :
                          bonus.rarityTier == BonusRarityTier.Rare ? 0.6f :
                          0.25f * epicMultiplier;

            int copies = Mathf.Max(1, Mathf.RoundToInt(weight * 10));
            for (int i = 0; i < copies; i++)
            {
                weighted.Add(bonus);
            }
        }

        // Select random weighted
        BonusCardData selected = weighted[Random.Range(0, weighted.Count)];

        // Prefer affordable
        if (selected.staminaCost > remainingStamina)
        {
            BonusCardData affordable = categoryMatch.Where(b => b.staminaCost <= remainingStamina).OrderBy(x => Random.value).FirstOrDefault();
            if (affordable != null)
            {
                selected = affordable;
                Debug.Log($"  💰 Switched to affordable bonus: {selected.bonusName}");
            }
        }

        return selected;
    }

    // ===== HELPER FUNCTIONS =====

    private List<ToyUnitData> FilterByPhase(List<ToyUnitData> units, int roundIndex, int loopIndex)
    {
        List<ToyUnitData> filtered = new List<ToyUnitData>();

        foreach (var unit in units)
        {
            if (unit.gamePhase == GamePhase.Early)
            {
                filtered.Add(unit); // Always available
            }
            else if (unit.gamePhase == GamePhase.Mid && roundIndex >= 3)
            {
                filtered.Add(unit); // Available from Round 3+
            }
            else if (unit.gamePhase == GamePhase.Epic && roundIndex == 3)
            {
                filtered.Add(unit); // Only in Round 3, very rare
            }
        }

        return filtered;
    }

    private float GetUtilityAppearanceChance(int roundIndex, int ownedUnitCount)
    {
        // 30% chance if you own any units
        if (ownedUnitCount > 0) return 0.3f;

        return 0.0f; // No utilities if no units owned
    }

    private void UpdateHistoryAfterOffer(OfferHistory history, List<object> offer, int roundIndex)
    {
        history.lastOfferedUnitIDs.Clear();

        foreach (var card in offer)
        {
            if (card is ToyUnitData unit)
            {
                history.lastOfferedUnitIDs.Add(unit.unitID);

                if (unit.toyRarityType == RarityType.Rare)
                {
                    history.rareOfferedThisRun.Add(unit.unitID);
                    history.lastRareOfferedRound = roundIndex;
                }
            }
            else if (card is UtilityCardData utility)
            {
                history.lastOfferedUtilityIDs.Clear();
                history.lastOfferedUtilityIDs.Add(utility.cardID);
            }
        }
    }

    // ===== GRID CHECK HELPER =====

    private bool CheckGridFullWithDifferentUnits(List<ToyUnitData> ownedUnits)
    {
        // Get unique unit types
        HashSet<string> uniqueUnitIDs = new HashSet<string>();

        foreach (var unit in ownedUnits)
        {
            if (unit != null)
            {
                uniqueUnitIDs.Add(unit.unitID);
            }
        }

        // If 9 unique units (full 3x3 grid with different types)
        // Note: Adjust this number based on your grid size
        int maxSlots = 9; // 3x3 grid

        bool isFull = uniqueUnitIDs.Count >= maxSlots;

        if (isFull)
        {
            Debug.Log($"🔒 Grid full check: {uniqueUnitIDs.Count} unique units (max {maxSlots})");
        }

        return isFull;
    }

    // ===== RESET =====

    public void ResetHistory(bool isPlayer)
    {
        if (isPlayer)
            playerHistory.Clear();
        else
            enemyHistory.Clear();
    }

    public void ResetAllHistory()
    {
        playerHistory.Clear();
        enemyHistory.Clear();
    }
}