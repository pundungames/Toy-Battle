// ============================================================================
// DRAFT CARD MANAGER - Kart seçim sistemini yönetir
// Her turn 3 kart gösterir: 2 Toy Unit + 1 Bonus (veya 3 Unit)
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

    [Header("Card Pool")]
    [SerializeField] List<ToyUnitData> allToyUnits;
    [SerializeField] List<BonusCardData> allBonusCards;

    [Header("Active Cards")]
    [SerializeField] List<DraftCardContent> activeCards = new List<DraftCardContent>();

    [Header("UI Elements")]
    [SerializeField] GameObject ribbon;
    [SerializeField] TextMeshProUGUI rerollPriceText;
    [SerializeField] Button rerollButton;
    [SerializeField] Button confirmButton;

    [Header("Settings")]
    [SerializeField] bool isShopMode = false;
    [SerializeField] int rerollCost = 10;
    [SerializeField] int currentPips = 2;

    [Header("Rarity Weights")]
    [SerializeField] int commonWeight = 70;
    [SerializeField] int uncommonWeight = 25;
    [SerializeField] int rareWeight = 5;

    private DraftCardContent selectedCard = null;
    private bool hasCardBeenChosen = false;
    private List<object> currentDraftPool = new List<object>(); // ToyUnitData veya BonusCardData

    private void OnEnable()
    {
        EventManager.onCardSelected += OnCardConfirmed;
    }

    private void OnDisable()
    {
        EventManager.onCardSelected -= OnCardConfirmed;
    }

    public void Open(bool shopMode)
    {
        isShopMode = shopMode;
        currentPips = GameConstants.PIP_PER_TURN;

        GenerateDraftCards();
        DisplayCards();
        SetupUI();
    }

    private void SetupUI()
    {
        ribbon.SetActive(true);
        confirmButton.gameObject.SetActive(false);

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

    // ===== CARD GENERATION =====

    private void GenerateDraftCards()
    {
        currentDraftPool.Clear();

        // Get unlocked units
        List<ToyUnitData> unlockedUnits = unlockSystem.GetUnlockedUnits(allToyUnits);

        // Add 2 toy units
        for (int i = 0; i < 2; i++)
        {
            ToyUnitData randomUnit = GetWeightedRandomUnit(unlockedUnits);
            if (randomUnit != null)
            {
                currentDraftPool.Add(randomUnit);
            }
        }

        // 15% chance for bonus card, otherwise add third unit
        if (Random.value < 0.15f && allBonusCards.Count > 0)
        {
            BonusCardData randomBonus = allBonusCards[Random.Range(0, allBonusCards.Count)];
            currentDraftPool.Add(randomBonus);
        }
        else
        {
            ToyUnitData randomUnit = GetWeightedRandomUnit(unlockedUnits);
            if (randomUnit != null)
            {
                currentDraftPool.Add(randomUnit);
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

    // ===== DISPLAY =====

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

    // ===== SELECTION =====

    public bool CanSelectCard() => !hasCardBeenChosen;

    public void CardSelected(DraftCardContent card)
    {
        if (hasCardBeenChosen) return;

        selectedCard = card;
        hasCardBeenChosen = true;

        Taptic.Medium();
        ribbon.SetActive(false);
        rerollButton.gameObject.SetActive(false);
        confirmButton.gameObject.SetActive(true);

        SetAllCardsInteractable(false);

        // Auto-confirm after delay
        Invoke(nameof(ConfirmSelection), 0.5f);
    }

    private void ConfirmSelection()
    {
        if (selectedCard == null) return;

        object cardData = currentDraftPool[activeCards.IndexOf(selectedCard)];

        if (cardData is ToyUnitData unitData)
        {
            // Spawn unit
            bool spawned = gridManager.SpawnUnit(unitData, true);

            if (spawned)
            {
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
            // Apply bonus
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
    }

    private void CompleteSelection()
    {
        selectedCard.Placed();
        Taptic.Light();

        // Draft complete
        Invoke(nameof(FinishDraft), 0.3f);
    }

    private void FinishDraft()
    {
        EventManager.OnDraftComplete();
        gameObject.SetActive(false);
    }

    public void CancelSelection()
    {
        if (selectedCard != null)
        {
            selectedCard.ResetCardVisuals();
            selectedCard = null;
        }

        hasCardBeenChosen = false;
        ribbon.SetActive(true);
        rerollButton.gameObject.SetActive(true);
        confirmButton.gameObject.SetActive(false);
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

    // ===== REROLL =====

    private void OnRerollClick()
    {
        Taptic.Light();

        if (!currencyManager.HasGold(rerollCost)) return;

        currencyManager.Payment(rerollCost);

        // Animate cards
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
        // Additional logic if needed
    }
}