// ============================================================================
// GRID MANAGER - 3x2 Grid sistemini yönetir (6 slot)
// Unit spawn/despawn işlemlerini kontrol eder
// ============================================================================

using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class GridManager : MonoBehaviour
{
    [Inject] DiContainer container;
    
    [Header("Grid Slots")]
    [SerializeField] Transform[] playerGridSlots = new Transform[GameConstants.GRID_SIZE];
    [SerializeField] Transform[] enemyGridSlots = new Transform[GameConstants.GRID_SIZE];
    
    [Header("Prefabs")]
    [SerializeField] GameObject unitVisualPrefab;
    
    [Header("Settings")]
    [SerializeField] int maxDeployCount = GameConstants.GRID_SIZE;
    
    private RuntimeUnit[] playerGrid = new RuntimeUnit[GameConstants.GRID_SIZE];
    private RuntimeUnit[] enemyGrid = new RuntimeUnit[GameConstants.GRID_SIZE];
    
    // ===== SPAWN UNIT =====
    
    public bool SpawnUnit(ToyUnitData unitData, bool isPlayer, int slotIndex = -1)
    {
        RuntimeUnit[] targetGrid = isPlayer ? playerGrid : enemyGrid;
        Transform[] targetSlots = isPlayer ? playerGridSlots : enemyGridSlots;
        
        // Find empty slot
        if (slotIndex == -1)
        {
            slotIndex = FindEmptySlot(targetGrid);
        }
        
        // Check if slot is available
        if (slotIndex == -1 || targetGrid[slotIndex] != null)
        {
            Debug.LogWarning($"Cannot spawn unit - Slot {slotIndex} is not available!");
            return false;
        }
        
        // Check deploy limit
        if (CountActiveUnits(targetGrid) >= maxDeployCount)
        {
            Debug.LogWarning("Deploy limit reached!");
            return false;
        }
        
        // Create runtime unit
        RuntimeUnit newUnit = new RuntimeUnit(unitData, slotIndex, isPlayer);
        targetGrid[slotIndex] = newUnit;
        
        // Create visual
        CreateUnitVisual(newUnit, targetSlots[slotIndex]);
        
        EventManager.OnUnitSpawn(newUnit);
        
        return true;
    }
    
    // ===== FIND EMPTY SLOT =====
    
    private int FindEmptySlot(RuntimeUnit[] grid)
    {
        for (int i = 0; i < grid.Length; i++)
        {
            if (grid[i] == null) return i;
        }
        return -1;
    }
    
    private int CountActiveUnits(RuntimeUnit[] grid)
    {
        int count = 0;
        foreach (var unit in grid)
        {
            if (unit != null) count++;
        }
        return count;
    }
    
    // ===== CREATE VISUAL =====
    
    private void CreateUnitVisual(RuntimeUnit unit, Transform slotTransform)
    {
        GameObject unitObj = new GameObject(unit.data.toyName);
        unitObj.transform.SetParent(slotTransform);
        unitObj.transform.localPosition = Vector3.zero;
        unitObj.transform.localScale = Vector3.one;
        
        // Add sprite renderer
        SpriteRenderer sr = unitObj.AddComponent<SpriteRenderer>();
        sr.sprite = unit.data.toySprite;
        
        // Add animator
        UnitSpriteAnimator animator = unitObj.AddComponent<UnitSpriteAnimator>();
        animator.SetFrames(unit.data.animationFrames);
        
        // Store reference
        unit.visualObject = unitObj;
    }
    
    // ===== DESPAWN UNIT =====
    
    public void DespawnUnit(RuntimeUnit unit)
    {
        if (unit == null) return;
        
        RuntimeUnit[] targetGrid = unit.isPlayerUnit ? playerGrid : enemyGrid;
        
        if (unit.visualObject != null)
        {
            Destroy(unit.visualObject);
        }
        
        targetGrid[unit.gridSlot] = null;
    }
    
    // ===== CLEAR GRID =====
    
    public void ClearGrid()
    {
        // Clear player grid
        for (int i = 0; i < playerGrid.Length; i++)
        {
            if (playerGrid[i] != null)
            {
                DespawnUnit(playerGrid[i]);
            }
        }
        
        // Clear enemy grid
        for (int i = 0; i < enemyGrid.Length; i++)
        {
            if (enemyGrid[i] != null)
            {
                DespawnUnit(enemyGrid[i]);
            }
        }
        
        playerGrid = new RuntimeUnit[GameConstants.GRID_SIZE];
        enemyGrid = new RuntimeUnit[GameConstants.GRID_SIZE];
    }
    
    // ===== GET UNITS =====
    
    public List<RuntimeUnit> GetPlayerUnits()
    {
        List<RuntimeUnit> units = new List<RuntimeUnit>();
        foreach (var unit in playerGrid)
        {
            if (unit != null && unit.IsAlive())
                units.Add(unit);
        }
        return units;
    }
    
    public List<RuntimeUnit> GetEnemyUnits()
    {
        List<RuntimeUnit> units = new List<RuntimeUnit>();
        foreach (var unit in enemyGrid)
        {
            if (unit != null && unit.IsAlive())
                units.Add(unit);
        }
        return units;
    }
    
    // ===== EXPAND SLOT (Bonus: +1 deploy limit) =====
    
    public void IncreaseDeployLimit()
    {
        maxDeployCount++;
        Debug.Log($"Deploy limit increased to {maxDeployCount}");
    }
}