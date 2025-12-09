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

    [Header("Unit Prefabs - 3D")]
    [Tooltip("Resources/Units/ klasöründen prefab yükleme (true) veya direct reference (false)")]
    [SerializeField] bool useResourcesFolder = true;

    [Header("Settings")]
    [SerializeField] int maxDeployCount = GameConstants.GRID_SIZE;

    private RuntimeUnit[] playerGrid = new RuntimeUnit[GameConstants.GRID_SIZE];
    private RuntimeUnit[] enemyGrid = new RuntimeUnit[GameConstants.GRID_SIZE];

    // ===== SPAWN UNIT (3D PREFAB) =====

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

        // ===== 3D PREFAB SPAWN =====
        GameObject unitPrefab = LoadUnitPrefab(unitData);

        if (unitPrefab == null)
        {
            Debug.LogError($"Unit prefab not found for: {unitData.toyName}");
            return false;
        }

        // Instantiate 3D prefab
        GameObject unitObj = Instantiate(unitPrefab, targetSlots[slotIndex].position, targetSlots[slotIndex].rotation, targetSlots[slotIndex]);

        // Get RuntimeUnit component (prefab'da olmalı)
        RuntimeUnit runtimeUnit = unitObj.GetComponent<RuntimeUnit>();

        if (runtimeUnit == null)
        {
            Debug.LogError($"RuntimeUnit component not found on prefab: {unitData.toyName}");
            Destroy(unitObj);
            return false;
        }

        // Initialize runtime unit
        runtimeUnit.Initialize(unitData, slotIndex, isPlayer);

        // Add to grid
        targetGrid[slotIndex] = runtimeUnit;

        // Zenject injection (HealthBarUI vb. için)
        container.InjectGameObject(unitObj);

        EventManager.OnUnitSpawn(runtimeUnit);

        return true;
    }

    // ===== LOAD UNIT PREFAB =====

    private GameObject LoadUnitPrefab(ToyUnitData unitData)
    {
        if (useResourcesFolder)
        {
            // Resources/Units/ klasöründen yükle
            // Örnek: "Units/Common/He-Man"
            string prefabPath = $"Units/{unitData.toyRarityType}/{unitData.toyName}";
            GameObject prefab = Resources.Load<GameObject>(prefabPath);

            if (prefab == null)
            {
                // Alternatif: Sadece isimle dene
                prefab = Resources.Load<GameObject>($"Units/{unitData.toyName}");
            }

            return prefab;
        }
        else
        {
            // ToyUnitData içinde unitPrefab field'ı varsa kullan
            Debug.LogError("Direct prefab reference not implemented. Use Resources folder!");
            return null;
        }
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

    // ===== DESPAWN UNIT =====

    public void DespawnUnit(RuntimeUnit unit)
    {
        if (unit == null) return;

        RuntimeUnit[] targetGrid = unit.isPlayerUnit ? playerGrid : enemyGrid;

        // Destroy GameObject (RuntimeUnit artık MonoBehaviour)
        if (unit.gameObject != null)
        {
            Destroy(unit.gameObject);
        }

        // Clear grid slot
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