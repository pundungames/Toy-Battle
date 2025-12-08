// ============================================================================
// MERGE SYSTEM - 3 duplicate kart → merge → level up
// Level 1 → 2 → 3 progression
// ============================================================================

using System.Collections.Generic;
using UnityEngine;

public class MergeSystem : MonoBehaviour
{
    [Header("Merge Requirements")]
    [SerializeField] int requiredDuplicates = 3;
    
    // ===== CHECK AUTO MERGE =====
    
    public void CheckAutoMerge()
    {
        // Get all unique cards
        List<string> allCardIDs = GetAllCardIDs();
        
        foreach (var cardID in allCardIDs)
        {
            CheckAndMergeCard(cardID);
        }
    }
    
    private List<string> GetAllCardIDs()
    {
        List<string> cardIDs = new List<string>();
        
        // Scan PlayerPrefs for all card counts
        // This is a simplified version - in production, you'd maintain a card database
        
        // Example card IDs (bu listeyi Resources'tan yükleyebilirsin)
        string[] knownCards = new string[]
        {
            "heman", "toysoldier", "wrestlers", "toytank", "explosivecar",
            "tmnt", "golem", "growingtoy", "skeletor", "rockemrobots"
        };
        
        foreach (var id in knownCards)
        {
            string key = $"Card_{id}_Count";
            if (PlayerPrefs.HasKey(key))
            {
                cardIDs.Add(id);
            }
        }
        
        return cardIDs;
    }
    
    // ===== MERGE CARD =====
    
    private void CheckAndMergeCard(string cardID)
    {
        string countKey = $"Card_{cardID}_Count";
        string levelKey = $"Card_{cardID}_Level";
        
        int count = PlayerPrefs.GetInt(countKey, 0);
        int level = PlayerPrefs.GetInt(levelKey, 1);
        
        // Check if we can merge
        if (count >= requiredDuplicates && level < 3)
        {
            // Merge!
            count -= requiredDuplicates;
            level++;
            
            PlayerPrefs.SetInt(countKey, count);
            PlayerPrefs.SetInt(levelKey, level);
            PlayerPrefs.Save();
            
            Debug.Log($"Merged {cardID} to level {level}!");
            
            // Notify
            // EventManager.OnCardMerged(cardData);
            
            // Check for another merge
            if (count >= requiredDuplicates && level < 3)
            {
                CheckAndMergeCard(cardID);
            }
        }
    }
    
    // ===== MANUAL MERGE =====
    
    public bool CanMerge(string cardID)
    {
        string countKey = $"Card_{cardID}_Count";
        string levelKey = $"Card_{cardID}_Level";
        
        int count = PlayerPrefs.GetInt(countKey, 0);
        int level = PlayerPrefs.GetInt(levelKey, 1);
        
        return count >= requiredDuplicates && level < 3;
    }
    
    public void MergeCard(string cardID)
    {
        if (CanMerge(cardID))
        {
            CheckAndMergeCard(cardID);
            Taptic.Success();
        }
    }
    
    // ===== GET CARD LEVEL =====
    
    public int GetCardLevel(string cardID)
    {
        string levelKey = $"Card_{cardID}_Level";
        return PlayerPrefs.GetInt(levelKey, 1);
    }
    
    public int GetCardCount(string cardID)
    {
        string countKey = $"Card_{cardID}_Count";
        return PlayerPrefs.GetInt(countKey, 0);
    }
}