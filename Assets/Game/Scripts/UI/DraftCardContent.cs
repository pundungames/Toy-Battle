// ============================================================================
// DRAFT CARD CONTENT - Tek bir draft kartını temsil eder
// Hem Unit hem Bonus hem Utility kartları için kullanılır
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;

public class DraftCardContent : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] TextMeshProUGUI cardName;
    [SerializeField] TextMeshProUGUI cardInfo;
    [SerializeField] TextMeshProUGUI staminaText;
    [SerializeField] SpriteImageAnimation cardImage;
    [SerializeField] Image typeIcon;
    [SerializeField] Image bg;
    [SerializeField] Image front;
    [SerializeField] Image typeBG;
    [Header("States")]
    [SerializeField] GameObject focusVisual;

    [Header("Rarity Visuals")]
    [SerializeField] List<CardRarityVisual> rarityVisuals;

    [Header("Components")]
    [SerializeField] internal Button button;

    private Vector3 initialLocalPosition;
    [SerializeField] float selectedYOffset = -100f;
    private DraftCardManager manager;
    private bool isUnit = true;
    bool selected;
    private ToyUnitData currentUnitData;
    private DraftCardManager draftManager;
    private int currentStaminaCost = 0; // For utility cards

    private void Awake()
    {
        initialLocalPosition = transform.localPosition;
    }

    // ===== UNIT CARD SETUP =====

    public void SetUnitContent(ToyUnitData unitData, DraftCardManager cardManager, bool isShop)
    {
        manager = cardManager;
        draftManager = cardManager;
        currentUnitData = unitData;
        currentStaminaCost = unitData.toyStamina;
        isUnit = true;
        selected = false;

        cardName.text = unitData.toyName;
        cardInfo.text = unitData.toyInfo;
        cardImage.sprites = unitData.animationFrames.ToList();
        cardImage.StartAnim();
        typeIcon.sprite = unitData.typeSprite;

        staminaText.text = unitData.toyStamina.ToString();

        ApplyRarityVisual(unitData.toyRarityType);

        UpdateAffordability();

        ResetCardVisuals();
    }

    // ===== BONUS CARD SETUP =====

    public void SetBonusContent(BonusCardData bonusData, DraftCardManager cardManager, int availablePips)
    {
        manager = cardManager;
        isUnit = false;

        cardName.text = bonusData.bonusName;
        cardInfo.text = bonusData.description;
        cardImage.image.sprite = bonusData.cardSprite;

        staminaText.transform.parent.gameObject.SetActive(false);

        staminaText.text = bonusData.pipCost.ToString();

        button.interactable = availablePips >= bonusData.pipCost;

        ApplyRarityVisual(RarityType.Rare);

        ResetCardVisuals();
    }

    // ===== UTILITY CARD SETUP =====

    public void SetUtilityContent(UtilityCardData utilityData, DraftCardManager cardManager)
    {
        manager = cardManager;
        draftManager = cardManager;
        currentUnitData = utilityData.targetUnit;
        currentStaminaCost = utilityData.staminaCost;
        isUnit = true; // Treat as unit for stamina check
        selected = false;

        cardName.text = utilityData.cardName;
        cardInfo.text = utilityData.cardDescription;

        // ✅ Use target unit's animation frames!
        if (utilityData.targetUnit != null && utilityData.targetUnit.animationFrames != null)
        {
            cardImage.sprites = utilityData.targetUnit.animationFrames.ToList();
            cardImage.StartAnim();
        }
        else if (utilityData.cardSprite != null)
        {
            cardImage.image.sprite = utilityData.cardSprite;
        }

        // ✅ Use target unit's type icon
        if (utilityData.targetUnit != null && utilityData.targetUnit.typeSprite != null)
        {
            typeIcon.sprite = utilityData.targetUnit.typeSprite;
        }

        staminaText.transform.parent.gameObject.SetActive(true);
        staminaText.text = utilityData.staminaCost.ToString();

        // Rarity visual
        RarityType displayRarity = utilityData.rarity switch
        {
            UtilityCardRarity.Common => RarityType.Common,
            UtilityCardRarity.Rare => RarityType.Rare,
            UtilityCardRarity.Epic => RarityType.Epic,
            _ => RarityType.Common
        };

        ApplyRarityVisual(displayRarity);

        UpdateAffordability();

        ResetCardVisuals();
    }

    // ===== RARITY VISUAL =====

    private void ApplyRarityVisual(RarityType rarity)
    {
        for (int i = 0; i < rarityVisuals.Count; i++)
        {
            bool isActive = i == (int)rarity;

            foreach (var visual in rarityVisuals[i].visuals)
            {
                visual.SetActive(isActive);
            }

            if (isActive && rarityVisuals[i].bg != null)
            {
                bg.sprite = rarityVisuals[i].bg;
                front.sprite = rarityVisuals[i].front;
                typeBG.sprite = rarityVisuals[i].typeBG;
            }
        }
    }

    // ===== STAMINA AFFORDABILITY =====

    private void OnEnable()
    {
        DraftCardManager.OnStaminaChanged += OnStaminaChanged;
    }

    private void OnDisable()
    {
        DraftCardManager.OnStaminaChanged -= OnStaminaChanged;
    }

    private void OnStaminaChanged(int current, int max)
    {
        UpdateAffordability();
    }

    private void UpdateAffordability()
    {
        if (draftManager == null || !isUnit) return;

        bool canAfford = draftManager.HasEnoughStamina(currentStaminaCost);

        button.interactable = canAfford;

        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = canAfford ? 1f : 0.5f;

        if (staminaText != null)
        {
            staminaText.color = canAfford ? Color.white : Color.red;
        }
    }

    // ===== STAMINA CHECK ON SELECT =====

    public void SelectCard()
    {
        if (selected) return;

        if (isUnit)
        {
            if (!draftManager.HasEnoughStamina(currentStaminaCost))
            {
                Debug.LogWarning($"⚠️ Not enough stamina! Need {currentStaminaCost}, have {draftManager.CurrentStamina}");
                transform.DOShakePosition(0.3f, 10f, 20, 90, false, true).SetUpdate(true);
                return;
            }
        }

        selected = true;
        transform.DOKill(true);

        if (manager.CanSelectCard())
        {
            focusVisual.SetActive(true);
            transform.DOLocalMoveY(initialLocalPosition.y + selectedYOffset, 0.5f).SetUpdate(true);
            manager.CardSelected(this);
        }
    }

    public void Placed()
    {
        focusVisual.SetActive(false);
        transform.localPosition = initialLocalPosition;
        cardImage.enabled = true;
    }

    public void ResetCardVisuals()
    {
        focusVisual.SetActive(false);
        transform.localPosition = initialLocalPosition;
        cardImage.enabled = true;
    }

    public void CheckCurrency()
    {
    }
}

[Serializable]
public class CardRarityVisual
{
    [SerializeField] internal List<GameObject> visuals;
    [SerializeField] internal Sprite bg;
    [SerializeField] internal Sprite front;
    [SerializeField] internal Sprite typeBG;
}