// ============================================================================
// DRAFT CARD CONTENT - Tek bir draft kartýný temsil eder
// Hem Unit hem Bonus kartlarý için kullanýlýr
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;
using System.Collections.Generic;

public class DraftCardContent : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] TextMeshProUGUI cardName;
    [SerializeField] TextMeshProUGUI cardInfo;
    [SerializeField] TextMeshProUGUI priceText;
    [SerializeField] Image cardImage;
    [SerializeField] Image borderImage;

    [Header("States")]
    [SerializeField] GameObject focusVisual;
    [SerializeField] GameObject soldVisual;
    [SerializeField] GameObject pipCostVisual;
    [SerializeField] TextMeshProUGUI pipCostText;

    [Header("Rarity Visuals")]
    [SerializeField] List<CardRarityVisual> rarityVisuals;

    [Header("Components")]
    [SerializeField] internal Button button;

    private Vector3 initialLocalPosition;
    private readonly float selectedYOffset = 100f;
    private DraftCardManager manager;
    private bool isUnit = true;

    private void Awake()
    {
        initialLocalPosition = transform.localPosition;
    }

    // ===== UNIT CARD SETUP =====

    public void SetUnitContent(ToyUnitData unitData, DraftCardManager cardManager, bool isShop)
    {
        manager = cardManager;
        isUnit = true;

        cardName.text = unitData.toyName;
        cardInfo.text = unitData.toyInfo;
        cardImage.sprite = unitData.toySprite;

        // Shop mode
        if (isShop)
        {
            priceText.text = unitData.toyPrice.ToString();
            priceText.transform.parent.gameObject.SetActive(true);
        }
        else
        {
            priceText.transform.parent.gameObject.SetActive(false);
        }

        // Pip cost visual (unit için yok)
        pipCostVisual.SetActive(false);

        // Rarity visual
        ApplyRarityVisual(unitData.toyRarityType);

        ResetCardVisuals();
    }

    // ===== BONUS CARD SETUP =====

    public void SetBonusContent(BonusCardData bonusData, DraftCardManager cardManager, int availablePips)
    {
        manager = cardManager;
        isUnit = false;

        cardName.text = bonusData.bonusName;
        cardInfo.text = bonusData.description;
        cardImage.sprite = bonusData.cardSprite;

        // Price (bonus için yok)
        priceText.transform.parent.gameObject.SetActive(false);

        // Pip cost
        pipCostVisual.SetActive(true);
        pipCostText.text = bonusData.pipCost.ToString();

        // Check if affordable
        button.interactable = availablePips >= bonusData.pipCost;

        // Rarity (bonus kartlar rare sayýlabilir)
        ApplyRarityVisual(RarityType.Uncommon);

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

            if (isActive && rarityVisuals[i].border != null)
            {
                borderImage.sprite = rarityVisuals[i].border;
            }
        }
    }

    // ===== SELECTION =====

    public void SelectCard()
    {
        if (soldVisual.activeSelf) return;

        transform.DOKill(true);

        if (manager.CanSelectCard())
        {
            focusVisual.SetActive(true);
            transform.DOLocalMoveY(initialLocalPosition.y + selectedYOffset, 0.3f).SetUpdate(true);
            manager.CardSelected(this);
        }
    }

    public void Placed()
    {
        soldVisual.SetActive(true);
        focusVisual.SetActive(false);
        transform.localPosition = initialLocalPosition;
        cardImage.enabled = true;
    }

    public void ResetCardVisuals()
    {
        soldVisual.SetActive(false);
        focusVisual.SetActive(false);
        transform.localPosition = initialLocalPosition;
        cardImage.enabled = true;
    }

    public void CheckCurrency()
    {
        // Currency check logic
        if (!soldVisual.activeSelf)
        {
            // Check gold for shop mode
        }
    }
}

[Serializable]
public class CardRarityVisual
{
    [SerializeField] internal List<GameObject> visuals;
    [SerializeField] internal Sprite border;
}