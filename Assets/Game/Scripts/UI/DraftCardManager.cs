// ============================================================================
// DRAFT CARD MANAGER - WITH OFFER GENERATOR INTEGRATION
// ✅ Stamina system preserved
// ✅ New offer system added
// ============================================================================

using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class DraftCardManager : MonoBehaviour
{
    [Inject] UIManager uiManager;
    [Inject] CurrencyManager currencyManager;
    [Inject] GridManager gridManager;
    [Inject] BonusSystem bonusSystem;
    [Inject] UnlockSystem unlockSystem;

    [Header("⚡ TEST MODE")]
    [SerializeField] bool testMode = false;
    [SerializeField] List<ToyUnitData> testCharacters = new List<ToyUnitData>();

    [Header("🎯 NEW OFFER SYSTEM")]
    [SerializeField] bool useNewOfferSystem = true;

    [Header("Card Pool")]
    [SerializeField] List<ToyUnitData> allToyUnits;
    [SerializeField] List<BonusCardData> allBonusCards;
    // NOTE: Utility cards now come from ToyUnitData.utilityCards

    [Header("Active Cards")]
    [SerializeField] List<DraftCardContent> activeCards = new List<DraftCardContent>();

    [Header("UI Elements")]
    [SerializeField] TextMeshProUGUI rerollPriceText;
    [SerializeField] Button rerollButton;

    [Header("Settings")]
    [SerializeField] bool isShopMode = false;
    [SerializeField] int rerollCost = 10;
    [SerializeField] int currentPips = 2;

    [Header("Rarity Weights")]
    [SerializeField] int commonWeight = 70;
    [SerializeField] int rareWeight = 27;
    [SerializeField] int epicWeight = 3;

    [Header("Stamina System")]
    [SerializeField] int maxStamina = 10;
    [SerializeField] int currentStamina = 10;

    public static event System.Action<int, int> OnStaminaChanged;

    private DraftCardContent selectedCard = null;
    private bool hasCardBeenChosen = false;
    private List<object> currentDraftPool = new List<object>();

    private OfferGenerator offerGenerator;

    private void OnEnable()
    {
        EventManager.onCardSelected += OnCardConfirmed;
    }

    private void OnDisable()
    {
        EventManager.onCardSelected -= OnCardConfirmed;
    }

    private void Start()
    {
        offerGenerator = new OfferGenerator(allToyUnits, allBonusCards);
        Debug.Log("✅ OfferGenerator initialized");
    }

    public void Open(bool shopMode)
    {
        if (unlockSystem == null)
        {
            Debug.LogError("❌ UnlockSystem is NULL!");
            return;
        }

        if (allToyUnits == null || allToyUnits.Count == 0)
        {
            Debug.LogError("❌ allToyUnits is empty!");
            return;
        }

        isShopMode = shopMode;
        currentPips = GameConstants.PIP_PER_TURN;

        GenerateDraftCards();
        DisplayCards();
        SetupUI();

        gameObject.SetActive(true);
    }

    private void SetupUI()
    {
        if (rerollButton != null)
        {
            rerollPriceText.text = rerollCost.ToString();
            rerollButton.gameObject.SetActive(true);
            rerollButton.interactable = currencyManager.HasGold(rerollCost);
            rerollButton.onClick.RemoveAllListeners();
            rerollButton.onClick.AddListener(OnRerollClick);
        }

        CheckCurrency();
    }

    private void GenerateDraftCards()
    {
        currentDraftPool.Clear();

        if (testMode && testCharacters.Count > 0)
        {
            Debug.Log("🧪 TEST MODE ACTIVE - Using test characters");
            int count = Mathf.Min(testCharacters.Count, 3);
            for (int i = 0; i < count; i++)
            {
                if (testCharacters[i] != null)
                {
                    currentDraftPool.Add(testCharacters[i]);
                }
            }
            return;
        }

        if (useNewOfferSystem && offerGenerator != null)
        {
            Debug.Log("🎯 Using NEW Offer Generator system");

            int roundIndex = 1;
            int loopIndex = 1;
            int remainingStamina = currentStamina;

            List<ToyUnitData> ownedUnits = gridManager.GetPlayerUnits()
                .Where(u => u != null && u.data != null)
                .Select(u => u.data)
                .Distinct()
                .ToList();

            List<object> unitOffer = offerGenerator.GenerateUnitOffer(
                isPlayer: true,
                roundIndex: roundIndex,
                loopIndex: loopIndex,
                remainingStamina: remainingStamina,
                ownedUnits: ownedUnits
            );

            currentDraftPool.AddRange(unitOffer);
            Debug.Log($"✅ Generated offer: {unitOffer.Count} cards");
            return;
        }

        // OLD SYSTEM
        List<ToyUnitData> unlockedUnits = unlockSystem.GetUnlockedUnits(allToyUnits);
        List<ToyUnitData> availableUnits = new List<ToyUnitData>(unlockedUnits);

        for (int i = 0; i < 2; i++)
        {
            if (availableUnits.Count == 0) break;
            ToyUnitData randomUnit = GetWeightedRandomUnit(availableUnits);
            if (randomUnit != null)
            {
                currentDraftPool.Add(randomUnit);
                availableUnits.Remove(randomUnit);
            }
        }

        if (Random.value < 0.15f && allBonusCards.Count > 0)
        {
            BonusCardData randomBonus = allBonusCards[Random.Range(0, allBonusCards.Count)];
            currentDraftPool.Add(randomBonus);
        }
        else
        {
            if (availableUnits.Count > 0)
            {
                ToyUnitData randomUnit = GetWeightedRandomUnit(availableUnits);
                if (randomUnit != null)
                {
                    currentDraftPool.Add(randomUnit);
                    availableUnits.Remove(randomUnit);
                }
            }
        }
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
                RarityType.Rare => rareWeight,
                RarityType.Epic => epicWeight,
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

    private void DisplayCards()
    {
        for (int i = 0; i < activeCards.Count; i++)
        {
            if (i < currentDraftPool.Count)
            {
                object cardData = currentDraftPool[i];

                if (cardData is ToyUnitData unitData)
                {
                    activeCards[i].SetUnitContent(unitData, this, isShopMode);
                }
                else if (cardData is BonusCardData bonusData)
                {
                    activeCards[i].SetBonusContent(bonusData, this, currentPips);
                }
                else if (cardData is UtilityCardData utilityData)
                {
                    activeCards[i].SetUtilityContent(utilityData, this);
                }

                activeCards[i].gameObject.SetActive(true);
            }
            else
            {
                activeCards[i].gameObject.SetActive(false);
            }
        }

        selectedCard = null;
        hasCardBeenChosen = false;
        SetAllCardsInteractable(true);
    }

    public bool CanSelectCard() => !hasCardBeenChosen;

    public void CardSelected(DraftCardContent card)
    {
        if (hasCardBeenChosen) return;

        selectedCard = card;
        hasCardBeenChosen = true;

        Taptic.Medium();
        rerollButton.gameObject.SetActive(false);

        SetAllCardsInteractable(false);

        Invoke(nameof(ConfirmSelection), 0.6f);
    }

    private void ConfirmSelection()
    {
        if (selectedCard == null) return;

        object cardData = currentDraftPool[activeCards.IndexOf(selectedCard)];

        if (cardData is ToyUnitData unitData)
        {
            if (!HasEnoughStamina(unitData.toyStamina))
            {
                Debug.LogWarning($"⚠️ Not enough stamina for {unitData.toyName}!");
                CancelSelection();
                return;
            }

            bool spawned = gridManager.SpawnUnit(unitData, true);

            if (spawned)
            {
                if (!TrySpendStamina(unitData.toyStamina))
                {
                    Debug.LogError($"❌ Failed to spend stamina for {unitData.toyName}!");
                }

                EventManager.OnCardSelected(unitData);
                CompleteSelection();
            }
            else
            {
                Debug.LogWarning("Grid is full! Cannot spawn unit.");
                CancelSelection();
            }
        }
        else if (cardData is BonusCardData bonusData)
        {
            if (currentPips >= bonusData.pipCost)
            {
                bonusSystem.ApplyBonus(bonusData);
                currentPips -= bonusData.pipCost;
                EventManager.OnBonusApplied(bonusData);
                CompleteSelection();
            }
            else
            {
                Debug.LogWarning("Not enough pips!");
                CancelSelection();
            }
        }
        else if (cardData is UtilityCardData utilityData)
        {
            if (!HasEnoughStamina(utilityData.staminaCost))
            {
                Debug.LogWarning($"⚠️ Not enough stamina for {utilityData.cardName}!");
                CancelSelection();
                return;
            }

            // Apply utility card effect
            bool success = ApplyUtilityCard(utilityData);

            if (success)
            {
                if (!TrySpendStamina(utilityData.staminaCost))
                {
                    Debug.LogError($"❌ Failed to spend stamina for {utilityData.cardName}!");
                }

                CompleteSelection();
            }
            else
            {
                Debug.LogWarning($"Failed to apply utility card: {utilityData.cardName}");
                CancelSelection();
            }
        }
    }

    private void CompleteSelection()
    {
        selectedCard.Placed();
        Taptic.Light();
        FinishDraft();
    }

    private void FinishDraft()
    {
        HideCards();
        Debug.Log("Player draft complete, waiting for AI...");
    }

    private void HideCards()
    {
        foreach (var card in activeCards)
        {
            card.gameObject.SetActive(false);
        }

        if (rerollButton != null) rerollButton.gameObject.SetActive(false);
    }

    public void CancelSelection()
    {
        if (selectedCard != null)
        {
            selectedCard.ResetCardVisuals();
            selectedCard = null;
        }

        hasCardBeenChosen = false;
        rerollButton.gameObject.SetActive(true);
        SetAllCardsInteractable(true);
    }

    private void SetAllCardsInteractable(bool interactable)
    {
        foreach (var card in activeCards)
        {
            if (card != selectedCard && card.gameObject.activeSelf)
            {
                card.button.interactable = interactable;
            }
        }
    }

    private void OnRerollClick()
    {
        Taptic.Light();

        if (!currencyManager.HasGold(rerollCost)) return;

        currencyManager.Payment(rerollCost);

        foreach (var card in activeCards)
        {
            card.ResetCardVisuals();
            card.transform.DOScale(Vector3.one * 1.1f, 0.1f)
                .SetUpdate(true)
                .OnComplete(() => card.transform.DOScale(Vector3.one, 0.1f).SetUpdate(true));
        }

        GenerateDraftCards();
        DisplayCards();
        SetupUI();

        EventManager.OnReroll();
    }

    private void CheckCurrency()
    {
        foreach (var card in activeCards)
        {
            card.CheckCurrency();
        }
    }

    private void OnCardConfirmed(ToyUnitData unitData)
    {
    }

    // STAMINA SYSTEM
    public int CurrentStamina => currentStamina;
    public int MaxStamina => maxStamina;

    public bool HasEnoughStamina(int cost)
    {
        return currentStamina >= cost;
    }

    public bool TrySpendStamina(int cost)
    {
        if (!HasEnoughStamina(cost))
        {
            Debug.LogWarning($"⚠️ Not enough stamina! Need {cost}, have {currentStamina}");
            return false;
        }

        currentStamina -= cost;
        Debug.Log($"💰 Spent {cost} stamina. Remaining: {currentStamina}/{maxStamina}");

        OnStaminaChanged?.Invoke(currentStamina, maxStamina);

        return true;
    }

    public void ResetStamina()
    {
        currentStamina = maxStamina;
        Debug.Log($"🔄 Stamina reset to {maxStamina}");
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    public void AddStamina(int amount)
    {
        currentStamina = Mathf.Min(currentStamina + amount, maxStamina);
        Debug.Log($"➕ Added {amount} stamina. Current: {currentStamina}/{maxStamina}");
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    // ===== UTILITY CARD APPLICATION =====

    private bool ApplyUtilityCard(UtilityCardData utilityData)
    {
        Debug.Log($"🛠️ Applying utility card: {utilityData.cardName}");

        switch (utilityData.utilityType)
        {
            case UtilityType.LevelUp:
                return ApplyLevelUp(utilityData);

            case UtilityType.CountAdd:
                return ApplyCountAdd(utilityData);

            case UtilityType.Multiplier:
                return ApplyMultiplier(utilityData);

            default:
                Debug.LogError($"Unknown utility type: {utilityData.utilityType}");
                return false;
        }
    }

    private bool ApplyLevelUp(UtilityCardData utilityData)
    {
        if (utilityData.targetUnit == null)
        {
            Debug.LogError("❌ LevelUp card has no target unit!");
            return false;
        }

        ToyUnitData targetUnitData = utilityData.targetUnit;
        int levelIncrease = utilityData.effectValue; // How much to increase (+1, +2, etc)

        Debug.Log($"⬆️ Leveling up ALL {targetUnitData.toyName} by +{levelIncrease}");

        // Get all units of this type on grid
        List<RuntimeUnit> playerUnits = gridManager.GetPlayerUnits();
        int upgradeCount = 0;

        foreach (var unit in playerUnits)
        {
            if (unit != null && unit.data != null && unit.data.unitID == targetUnitData.unitID)
            {
                // Increment level
                int oldLevel = unit.data.level;
                unit.data.level += levelIncrease;
                upgradeCount++;

                Debug.Log($"   ✅ Upgraded {unit.data.toyName}: Level {oldLevel} → Level {unit.data.level}");
            }
        }

        if (upgradeCount > 0)
        {
            Debug.Log($"✅ Level Up complete: {upgradeCount} units upgraded by +{levelIncrease}");
            return true;
        }
        else
        {
            Debug.LogWarning($"⚠️ No {targetUnitData.toyName} found on grid to upgrade!");
            return false;
        }
    }

    private bool ApplyCountAdd(UtilityCardData utilityData)
    {
        if (utilityData.targetUnit == null)
        {
            Debug.LogError("❌ CountAdd card has no target unit!");
            return false;
        }

        ToyUnitData targetUnitData = utilityData.targetUnit;
        int addCount = utilityData.effectValue;

        Debug.Log($"➕ Adding {addCount}x {targetUnitData.toyName} to grid");

        // Spawn units using normal spawn logic
        int successCount = 0;
        for (int i = 0; i < addCount; i++)
        {
            bool spawned = gridManager.SpawnUnit(targetUnitData, true);
            if (spawned)
            {
                successCount++;
            }
            else
            {
                Debug.LogWarning($"⚠️ Could only spawn {successCount}/{addCount} units (grid full or max reached)");
                break;
            }
        }

        if (successCount > 0)
        {
            Debug.Log($"✅ Added {successCount}x {targetUnitData.toyName}");
            return true;
        }
        else
        {
            Debug.LogWarning($"❌ Failed to add any units!");
            return false;
        }
    }

    private bool ApplyMultiplier(UtilityCardData utilityData)
    {
        if (utilityData.targetUnit == null)
        {
            Debug.LogError("❌ Multiplier card has no target unit!");
            return false;
        }

        ToyUnitData targetUnitData = utilityData.targetUnit;
        int multiplier = utilityData.effectValue;

        Debug.Log($"✖️ Multiplying {targetUnitData.toyName} by {multiplier}");

        // Get current count of this unit
        List<RuntimeUnit> playerUnits = gridManager.GetPlayerUnits();
        int currentCount = playerUnits.Count(u => u != null && u.data != null && u.data.unitID == targetUnitData.unitID);

        if (currentCount == 0)
        {
            Debug.LogWarning($"⚠️ No {targetUnitData.toyName} on grid to multiply!");
            return false;
        }

        // Spawn (multiplier - 1) * currentCount new units
        // e.g. X2: spawn 1x current count (doubles it)
        int toSpawn = (multiplier - 1) * currentCount;

        Debug.Log($"   Current: {currentCount}, Multiplier: {multiplier}, Will spawn: {toSpawn}");

        int successCount = 0;
        for (int i = 0; i < toSpawn; i++)
        {
            bool spawned = gridManager.SpawnUnit(targetUnitData, true);
            if (spawned)
            {
                successCount++;
            }
            else
            {
                Debug.LogWarning($"⚠️ Could only spawn {successCount}/{toSpawn} units (max reached)");
                break;
            }
        }

        if (successCount > 0)
        {
            int finalCount = currentCount + successCount;
            Debug.Log($"✅ Multiplied {targetUnitData.toyName}: {currentCount} → {finalCount}");
            return true;
        }
        else
        {
            Debug.LogWarning($"❌ Failed to multiply units!");
            return false;
        }
    }
}