// ============================================================================
// UNLOCK SYSTEM - Unit unlock progression sistemi
// Başlangıçta 4 unit açık, her 3 maçta 1 yeni unit açılır
// ============================================================================

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnlockSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] int startingUnits = GameConstants.STARTING_UNITS;
    [SerializeField] int winsPerUnlock = GameConstants.WINS_PER_UNLOCK;
    
    [Header("Unlock Order")]
    [SerializeField] List<string> unlockOrder = new List<string>()
    {
        // Starting 4 units (always unlocked)
        "heman",
        "toysoldier",
        "wrestlers",
        "toytank",
        
        // Unlock progression
        "explosivecar",  // After 3 wins
        "tmnt",          // After 6 wins
        "golem",         // After 9 wins
        "growingtoy",    // After 12 wins
        "skeletor",      // After 15 wins
        "rockemrobots"   // After 18 wins
    };
    
    private void Start()
    {
        InitializeUnlocks();
    }
    
    // ===== INITIALIZE =====
    
    private void InitializeUnlocks()
    {
        // Unlock starting units
        for (int i = 0; i < startingUnits && i < unlockOrder.Count; i++)
        {
            string unitID = unlockOrder[i];
            if (!IsUnlocked(unitID))
            {
                UnlockUnit(unitID);
            }
        }
    }
    
    // ===== CHECK UNLOCK =====
    
    public void CheckUnlockProgress(int totalWins)
    {
        int unlocksEarned = totalWins / winsPerUnlock;
        int totalUnlockedCount = startingUnits + unlocksEarned;
        
        // Unlock units up to this count
        for (int i = 0; i < totalUnlockedCount && i < unlockOrder.Count; i++)
        {
            string unitID = unlockOrder[i];
            if (!IsUnlocked(unitID))
            {
                UnlockUnit(unitID);
            }
        }
    }
    
    // ===== UNLOCK UNIT =====
    
    private void UnlockUnit(string unitID)
    {
        PlayerPrefs.SetInt($"Unlock_{unitID}", 1);
        PlayerPrefs.Save();
        Debug.Log($"Unit unlocked: {unitID}");
    }
    
    public bool IsUnlocked(string unitID)
    {
        return PlayerPrefs.GetInt($"Unlock_{unitID}", 0) == 1;
    }
    
    // ===== GET UNLOCKED UNITS =====
    
    public List<ToyUnitData> GetUnlockedUnits(List<ToyUnitData> allUnits)
    {
        return allUnits.Where(u => IsUnlocked(u.unitID)).ToList();
    }
    
    // ===== NEXT UNLOCK INFO =====
    
    public string GetNextUnlockInfo(int currentWins)
    {
        int unlocksEarned = currentWins / winsPerUnlock;
        int nextUnlockIndex = startingUnits + unlocksEarned;
        
        if (nextUnlockIndex >= unlockOrder.Count)
        {
            return "All units unlocked!";
        }
        
        int winsNeeded = ((unlocksEarned + 1) * winsPerUnlock) - currentWins;
        string nextUnit = unlockOrder[nextUnlockIndex];
        
        return $"{winsNeeded} wins until {nextUnit} unlocks";
    }
}