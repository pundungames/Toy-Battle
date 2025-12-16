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
    private readonly float selectedYOffset = 100f;
    private DraftCardManager manager;
    private bool isUnit = true;
    bool selected;
    private void Awake()
    {
        initialLocalPosition = transform.localPosition;
    }

    // ===== UNIT CARD SETUP =====

    public void SetUnitContent(ToyUnitData unitData, DraftCardManager cardManager, bool isShop)
    {
        manager = cardManager;
        isUnit = true;
        selected = false;

        cardName.text = unitData.toyName;
        cardInfo.text = unitData.toyInfo;
        cardImage.sprites = unitData.animationFrames.ToList();
        cardImage.StartAnim();
        typeIcon.sprite = unitData.typeSprite;

        // Shop mode
        if (isShop)
        {
            staminaText.text = unitData.toyStamina.ToString();
            staminaText.transform.parent.gameObject.SetActive(true);
        }
        else
        {
            staminaText.transform.parent.gameObject.SetActive(false);
        }

        // Pip cost visual (unit için yok)

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
        cardImage.image.sprite = bonusData.cardSprite;

        // Price (bonus için yok)
        staminaText.transform.parent.gameObject.SetActive(false);

        staminaText.text = bonusData.pipCost.ToString();

        // Check if affordable
        button.interactable = availablePips >= bonusData.pipCost;

        // Rarity (bonus kartlar rare sayýlabilir)
        ApplyRarityVisual(RarityType.Rare);

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

    // ===== SELECTION =====

    public void SelectCard()
    {
        if (selected) return;
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
        // Currency check logic
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