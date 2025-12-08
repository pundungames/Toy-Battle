// ============================================================================
// CHEST SYSTEM - Sandık sistemi ve kart ödülleri
// Battle sonunda %40 chest drop şansı
// 3 kart içerir: %85 common, %15 rare
// ============================================================================

using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class ChestSystem : MonoBehaviour
{
    [Inject] UIManager uiManager;
    [Inject] MergeSystem mergeSystem;
    
    [Header("Card Pools")]
    [SerializeField] List<ToyUnitData> commonUnits;
    [SerializeField] List<ToyUnitData> rareUnits;
    
    [Header("UI")]
    [SerializeField] List<ChestCardSlot> chestCardSlots; // 3 slots
    
    [Header("Settings")]
    [SerializeField] bool isTutorialChest = false;
    
    private List<ToyUnitData> currentRewards = new List<ToyUnitData>();
    
    // ===== SHOW CHEST =====
    
    public void ShowChest(bool tutorial = false)
    {
        isTutorialChest = tutorial;
        uiManager.ShowChestPanel();
        GenerateChestRewards();
        DisplayRewards();
        EventManager.OnChestOpen();
    }
    
    // ===== GENERATE REWARDS =====
    
    private void GenerateChestRewards()
    {
        currentRewards.Clear();
        
        if (isTutorialChest)
        {
            // Tutorial chest: Guaranteed rare
            GenerateTutorialRewards();
        }
        else
        {
            // Normal chest: 85% common, 15% rare
            GenerateNormalRewards();
        }
    }
    
    private void GenerateTutorialRewards()
    {
        // 1 guaranteed rare + 2 common
        if (rareUnits.Count > 0)
        {
            currentRewards.Add(rareUnits[Random.Range(0, rareUnits.Count)]);
        }
        
        for (int i = 0; i < 2; i++)
        {
            if (commonUnits.Count > 0)
            {
                currentRewards.Add(commonUnits[Random.Range(0, commonUnits.Count)]);
            }
        }
    }
    
    private void GenerateNormalRewards()
    {
        for (int i = 0; i < GameConstants.CHEST_CARD_COUNT; i++)
        {
            bool isRare = Random.value < GameConstants.RARE_CHANCE;
            
            if (isRare && rareUnits.Count > 0)
            {
                currentRewards.Add(rareUnits[Random.Range(0, rareUnits.Count)]);
            }
            else if (commonUnits.Count > 0)
            {
                currentRewards.Add(commonUnits[Random.Range(0, commonUnits.Count)]);
            }
        }
    }
    
    // ===== DISPLAY REWARDS =====
    
    private void DisplayRewards()
    {
        for (int i = 0; i < chestCardSlots.Count; i++)
        {
            if (i < currentRewards.Count)
            {
                chestCardSlots[i].SetCard(currentRewards[i]);
                chestCardSlots[i].gameObject.SetActive(true);
            }
            else
            {
                chestCardSlots[i].gameObject.SetActive(false);
            }
        }
    }
    
    // ===== CLAIM REWARDS =====
    
    public void ClaimRewards()
    {
        foreach (var reward in currentRewards)
        {
            // Add to collection
            AddCardToCollection(reward);
        }
        
        Taptic.Success();
        
        // Check for auto-merge
        mergeSystem.CheckAutoMerge();
        
        // Continue to progress screen
        EventManager.OnGameStateChange(GameState.Progress);
    }
    
    private void AddCardToCollection(ToyUnitData card)
    {
        string key = $"Card_{card.unitID}_Count";
        int currentCount = PlayerPrefs.GetInt(key, 0);
        currentCount++;
        PlayerPrefs.SetInt(key, currentCount);
        PlayerPrefs.Save();
        
        Debug.Log($"Added {card.toyName} to collection. Total: {currentCount}");
    }
}