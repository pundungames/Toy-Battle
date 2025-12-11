// ============================================================================
// GRID MANAGER - FORMATION SYSTEM
// ✅ Draft: Normal spawn (grid pattern in slots)
// ✅ Battle: ArrangeUnitsInFormation() (strategic positioning)
// ✅ arrangementIndex-based formation (higher = back)
// ✅ Multi-row overflow handling
// ✅ X-axis centering, Z-axis depth
// ============================================================================

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using Zenject;

public class GridManager : MonoBehaviour
{
    [Inject] DiContainer container;

    [Header("Grid Slots")]
    [SerializeField] Transform[] playerGridSlots = new Transform[GameConstants.GRID_SIZE];
    [SerializeField] Transform[] enemyGridSlots = new Transform[GameConstants.GRID_SIZE];

    [Header("Unit Prefabs - 3D")]
    [Tooltip("Resources/Units/ klasöründen prefab yükleme")]
    [SerializeField] bool useResourcesFolder = true;

    [Header("Formation Settings")]
    [Tooltip("Player formation base position (back)")]
    [SerializeField] float baseBackPositionZ = -3.6f;

    [Tooltip("Enemy formation base position (back)")]
    [SerializeField] float enemyBasePositionZ = 3.6f;

    [Tooltip("Spacing between rows of same unit type")]
    [SerializeField] float rowToRowOffset = 1.0f;

    [Tooltip("Formation animation duration")]
    [SerializeField] float formationAnimationDuration = 1.0f;

    [Header("Settings")]
    [SerializeField] int maxDeployCount = GameConstants.GRID_SIZE;

    // ===== GRID SLOTS (Multi-Unit) =====
    private GridSlot[] playerGrid = new GridSlot[GameConstants.GRID_SIZE];
    private GridSlot[] enemyGrid = new GridSlot[GameConstants.GRID_SIZE];

    // ===== PERMANENT STATE =====
    [System.Serializable]
    public class GridSlotData
    {
        public ToyUnitData unitData;
        public int slotIndex;
        public int unitCount;
        public bool isFilled;

        public GridSlotData(ToyUnitData data, int slot, int count)
        {
            unitData = data;
            slotIndex = slot;
            unitCount = count;
            isFilled = true;
        }
    }

    private Dictionary<int, GridSlotData> playerGridState = new Dictionary<int, GridSlotData>();
    private Dictionary<int, GridSlotData> enemyGridState = new Dictionary<int, GridSlotData>();

    // ===== INITIALIZATION =====

    private void Start()
    {
        InitializeGrids();
    }

    private void InitializeGrids()
    {
        for (int i = 0; i < playerGrid.Length; i++)
        {
            playerGrid[i] = new GridSlot { slotIndex = i };
        }

        for (int i = 0; i < enemyGrid.Length; i++)
        {
            enemyGrid[i] = new GridSlot { slotIndex = i };
        }

        Debug.Log("✅ Grid slots initialized");
    }

    // ===== SPAWN UNIT (DRAFT PHASE) =====

    public bool SpawnUnit(ToyUnitData unitData, bool isPlayer, int slotIndex = -1)
    {
        GridSlot[] targetGrid = isPlayer ? playerGrid : enemyGrid;
        Transform[] targetSlots = isPlayer ? playerGridSlots : enemyGridSlots;

        if (slotIndex == -1)
        {
            slotIndex = FindSlotForUnit(unitData, isPlayer);
        }

        if (slotIndex == -1)
        {
            Debug.LogWarning($"❌ Cannot spawn {unitData.toyName} - No available slot!");
            return false;
        }

        GridSlot slot = targetGrid[slotIndex];

        if (!slot.CanAddUnit(unitData))
        {
            Debug.LogWarning($"❌ Slot {slotIndex} cannot accept {unitData.toyName}");
            return false;
        }

        GameObject unitPrefab = LoadUnitPrefab(unitData);

        if (unitPrefab == null)
        {
            Debug.LogError($"❌ Unit prefab not found for: {unitData.toyName}");
            return false;
        }

        GameObject unitObj = Instantiate(unitPrefab, targetSlots[slotIndex]);
        RuntimeUnit runtimeUnit = unitObj.GetComponent<RuntimeUnit>();

        if (runtimeUnit == null)
        {
            Debug.LogError($"❌ RuntimeUnit component not found on prefab: {unitData.toyName}");
            Destroy(unitObj);
            return false;
        }

        runtimeUnit.Initialize(unitData, slotIndex, isPlayer);

        slot.units.Add(runtimeUnit);
        slot.unitType = unitData;

        container.InjectGameObject(unitObj);

        // ✅ DRAFT: Arrange in slot (grid pattern)
        ArrangeUnitsInSlot(slotIndex, isPlayer);

        // ✅ Update permanent state
        UpdatePermanentState(slotIndex, isPlayer);

        EventManager.OnUnitSpawn(runtimeUnit);

        return true;
    }

    // ===== FIND SLOT FOR UNIT =====

    private int FindSlotForUnit(ToyUnitData unitData, bool isPlayer)
    {
        GridSlot[] targetGrid = isPlayer ? playerGrid : enemyGrid;

        // 1. Same character slot
        for (int i = 0; i < targetGrid.Length; i++)
        {
            if (!targetGrid[i].IsEmpty)
            {
                if (targetGrid[i].unitType.unitID == unitData.unitID &&
                    targetGrid[i].units.Count < unitData.maxStackPerSlot)
                {
                    return i;
                }
            }
        }

        // 2. Empty slot
        for (int i = 0; i < targetGrid.Length; i++)
        {
            if (targetGrid[i].IsEmpty)
            {
                return i;
            }
        }

        return -1;
    }

    // ===== ARRANGE UNITS IN SLOT (DRAFT ONLY) =====

    private void ArrangeUnitsInSlot(int slotIndex, bool isPlayer)
    {
        GridSlot slot = isPlayer ? playerGrid[slotIndex] : enemyGrid[slotIndex];
        int unitCount = slot.units.Count;

        if (unitCount == 0) return;

        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(unitCount));
        float spacing = slot.unitType.unitSpacing;
        float centerOffset = -((gridSize - 1) * spacing) / 2f;

        int index = 0;
        for (int row = 0; row < gridSize && index < unitCount; row++)
        {
            for (int col = 0; col < gridSize && index < unitCount; col++)
            {
                RuntimeUnit unit = slot.units[index];
                float xPos = centerOffset + (col * spacing);
                float zPos = centerOffset + (row * spacing);
                unit.transform.localPosition = new Vector3(xPos, 0, zPos);
                index++;
            }
        }
    }

    // ===== ARRANGE UNITS IN FORMATION (BATTLE START) =====

    /// <summary>
    /// ✅ MAIN FORMATION METHOD
    /// Called at battle start to arrange all units in strategic formation
    /// </summary>
    public void ArrangeUnitsInFormation(bool isPlayer)
    {
        Debug.Log($"🎯 Arranging formation for {(isPlayer ? "PLAYER" : "ENEMY")}");

        GridSlot[] targetGrid = isPlayer ? playerGrid : enemyGrid;

        // 1. Collect all units grouped by unit type
        Dictionary<ToyUnitData, List<RuntimeUnit>> unitsByType = new Dictionary<ToyUnitData, List<RuntimeUnit>>();

        foreach (var slot in targetGrid)
        {
            if (slot.IsEmpty) continue;

            ToyUnitData unitType = slot.unitType;

            if (!unitsByType.ContainsKey(unitType))
            {
                unitsByType[unitType] = new List<RuntimeUnit>();
            }

            unitsByType[unitType].AddRange(slot.units);
        }

        // 2. Sort unit types by arrangementIndex (descending: 100 → 0)
        var sortedUnitTypes = unitsByType.Keys.OrderByDescending(u => u.arrangementIndex).ToList();

        Debug.Log($"📊 Unit types in formation order:");
        foreach (var unitType in sortedUnitTypes)
        {
            Debug.Log($"   - {unitType.toyName} (index: {unitType.arrangementIndex}, count: {unitsByType[unitType].Count})");
        }

        // 3. Calculate formation positions
        float currentZ = isPlayer ? baseBackPositionZ : enemyBasePositionZ;
        float zDirection = isPlayer ? 1f : -1f; // Player: +Z, Enemy: -Z

        foreach (var unitType in sortedUnitTypes)
        {
            List<RuntimeUnit> unitsOfType = unitsByType[unitType];
            int totalUnits = unitsOfType.Count;
            int maxPerRow = unitType.maxUnitsPerRow;
            float spacing = unitType.unitSpacing;

            // Calculate rows needed
            int rowsNeeded = Mathf.CeilToInt((float)totalUnits / maxPerRow);

            Debug.Log($"🔹 {unitType.toyName}: {totalUnits} units → {rowsNeeded} rows (max {maxPerRow}/row)");

            int unitIndex = 0;

            for (int row = 0; row < rowsNeeded; row++)
            {
                // Units in this row
                int unitsInRow = Mathf.Min(maxPerRow, totalUnits - unitIndex);

                // Z position for this row
                float rowZ = currentZ + (row * rowToRowOffset * zDirection);

                // X positions (centered at 0)
                List<Vector3> rowPositions = CalculateRowPositions(unitsInRow, spacing, rowZ);

                // Assign positions to units
                for (int i = 0; i < unitsInRow && unitIndex < totalUnits; i++)
                {
                    RuntimeUnit unit = unitsOfType[unitIndex];
                    Vector3 targetPosition = rowPositions[i];

                    // Animate to formation position
                    unit.transform.DOMove(targetPosition, formationAnimationDuration)
                        .SetEase(Ease.OutQuad);

                    unitIndex++;
                }

                Debug.Log($"   Row {row + 1}: {unitsInRow} units at Z = {rowZ:F2}");
            }

            // Move to next unit type zone
            currentZ += (rowsNeeded * rowToRowOffset + spacing) * zDirection;
        }

        Debug.Log($"✅ Formation complete for {(isPlayer ? "PLAYER" : "ENEMY")}");
    }

    /// <summary>
    /// Calculate X positions for a row (centered at X = 0)
    /// </summary>
    private List<Vector3> CalculateRowPositions(int unitCount, float spacing, float zPos)
    {
        List<Vector3> positions = new List<Vector3>();

        // Calculate half-width for centering
        float halfWidth = (unitCount - 1) * spacing / 2f;

        for (int i = 0; i < unitCount; i++)
        {
            float xPos = -halfWidth + (i * spacing);
            positions.Add(new Vector3(xPos, 0, zPos));
        }

        return positions;
    }

    // ===== UPDATE PERMANENT STATE =====

    private void UpdatePermanentState(int slotIndex, bool isPlayer)
    {
        GridSlot slot = isPlayer ? playerGrid[slotIndex] : enemyGrid[slotIndex];
        var stateDict = isPlayer ? playerGridState : enemyGridState;

        if (slot.IsEmpty)
        {
            stateDict.Remove(slotIndex);
            Debug.Log($"💾 PERMANENT State: Slot {slotIndex} EMPTY");
        }
        else
        {
            stateDict[slotIndex] = new GridSlotData(slot.unitType, slotIndex, slot.units.Count);
            Debug.Log($"💾 PERMANENT State: Slot {slotIndex} = {slot.unitType.toyName} x{slot.units.Count}");
        }
    }

    // ===== LOAD UNIT PREFAB =====

    private GameObject LoadUnitPrefab(ToyUnitData unitData)
    {
        if (useResourcesFolder)
        {
            string prefabPath = $"Units/{unitData.toyRarityType}/{unitData.toyName}";
            GameObject prefab = Resources.Load<GameObject>(prefabPath);

            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>($"Units/{unitData.toyName}");
            }

            return prefab;
        }
        else
        {
            Debug.LogError("Direct prefab reference not implemented. Use Resources folder!");
            return null;
        }
    }

    // ===== GET UNITS =====

    public List<RuntimeUnit> GetPlayerUnits()
    {
        List<RuntimeUnit> allUnits = new List<RuntimeUnit>();

        foreach (var slot in playerGrid)
        {
            foreach (var unit in slot.units)
            {
                if (unit != null && unit.IsAlive())
                {
                    allUnits.Add(unit);
                }
            }
        }

        return allUnits;
    }

    public List<RuntimeUnit> GetEnemyUnits()
    {
        List<RuntimeUnit> allUnits = new List<RuntimeUnit>();

        foreach (var slot in enemyGrid)
        {
            foreach (var unit in slot.units)
            {
                if (unit != null && unit.IsAlive())
                {
                    allUnits.Add(unit);
                }
            }
        }

        return allUnits;
    }

    // ===== EXPAND SLOT =====

    public void IncreaseDeployLimit()
    {
        maxDeployCount++;
        Debug.Log($"Deploy limit increased to {maxDeployCount}");
    }

    // ===== BATTLE STATE MANAGEMENT =====

    public void ClearSceneSlot(int slotIndex, bool isPlayer, RuntimeUnit deadUnit)
    {
        GridSlot[] targetGrid = isPlayer ? playerGrid : enemyGrid;

        if (slotIndex >= 0 && slotIndex < targetGrid.Length)
        {
            GridSlot slot = targetGrid[slotIndex];
            slot.units.Remove(deadUnit);

            Debug.Log($"💀 Battle death: Unit removed from slot {slotIndex}. Remaining: {slot.units.Count}");
        }
    }

    public void ClearSceneObjects()
    {
        Debug.Log("🧹 Clearing battle scene (permanent state preserved)");

        // Player units cleanup
        for (int i = 0; i < playerGrid.Length; i++)
        {
            foreach (var unit in playerGrid[i].units)
            {
                if (unit != null && unit.gameObject != null)
                {
                    Destroy(unit.gameObject);
                }
            }
            playerGrid[i].units.Clear();
        }

        // Enemy units cleanup
        for (int i = 0; i < enemyGrid.Length; i++)
        {
            foreach (var unit in enemyGrid[i].units)
            {
                if (unit != null && unit.gameObject != null)
                {
                    Destroy(unit.gameObject);
                }
            }
            enemyGrid[i].units.Clear();
        }
    }

    public void RespawnPreviousUnits()
    {
        Debug.Log("♻️ Respawning from PERMANENT state");

        var playerStateSnapshot = playerGridState.ToList();
        var enemyStateSnapshot = enemyGridState.ToList();

        // Player respawn
        foreach (var kvp in playerStateSnapshot)
        {
            int slot = kvp.Key;
            GridSlotData slotData = kvp.Value;

            if (slotData.isFilled && slotData.unitData != null)
            {
                for (int i = 0; i < slotData.unitCount; i++)
                {
                    SpawnUnit(slotData.unitData, true, slot);
                }
            }
        }

        // Enemy respawn
        foreach (var kvp in enemyStateSnapshot)
        {
            int slot = kvp.Key;
            GridSlotData slotData = kvp.Value;

            if (slotData.isFilled && slotData.unitData != null)
            {
                for (int i = 0; i < slotData.unitCount; i++)
                {
                    SpawnUnit(slotData.unitData, false, slot);
                }
            }
        }

        Debug.Log("✅ Respawn complete");
    }

    public void ResetGridState()
    {
        Debug.Log("🔄 Resetting grid state");

        ClearSceneObjects();
        playerGridState.Clear();
        enemyGridState.Clear();
        InitializeGrids();

        Debug.Log("✅ Grid reset complete");
    }
}