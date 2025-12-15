// ============================================================================
// GRID MANAGER - COMPLETE SYSTEM
// ✅ Multi-unit spawn support (Punchy Bots, Slam Bros, MiniBoy)
// ✅ Formation system with NavMeshAgent
// ✅ Twin linking system
// ============================================================================

using System.Collections;
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
    [SerializeField] bool useResourcesFolder = true;

    [Header("Formation Settings")]
    [SerializeField] float baseBackPositionZ = -3.6f;
    [SerializeField] float enemyBasePositionZ = 3.6f;
    [SerializeField] float rowToRowOffset = 1.0f;
    [SerializeField] float formationAnimationDuration = 1.0f;

    [Header("Settings")]
    [SerializeField] int maxDeployCount = GameConstants.GRID_SIZE;

    // ===== GRID SLOTS =====
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

    // ===== SPAWN UNIT (UPDATED FOR MULTI-UNIT) =====

    public bool SpawnUnit(ToyUnitData unitData, bool isPlayer, int slotIndex = -1)
    {
        // Find slot
        if (slotIndex == -1)
        {
            slotIndex = FindSlotForUnit(unitData, isPlayer);
        }

        if (slotIndex == -1)
        {
            Debug.LogWarning($"❌ Cannot spawn {unitData.toyName} - No available slot!");
            return false;
        }

        GridSlot slot = isPlayer ? playerGrid[slotIndex] : enemyGrid[slotIndex];
        Transform slotTransform = isPlayer ? playerGridSlots[slotIndex] : enemyGridSlots[slotIndex];

        // Check if slot can accept
        if (!slot.CanAddUnit(unitData))
        {
            Debug.LogWarning($"❌ Slot {slotIndex} cannot accept {unitData.toyName}");
            return false;
        }

        // ✅ Check if multi-unit
        if (unitData.isMultiUnit && unitData.unitsPerSlot > 1)
        {
            return SpawnMultipleUnits(unitData, isPlayer, slotIndex);
        }

        // Standard single unit spawn
        return SpawnSingleUnit(unitData, isPlayer, slotIndex);
    }

    // ===== SPAWN SINGLE UNIT =====

    private bool SpawnSingleUnit(ToyUnitData unitData, bool isPlayer, int slotIndex)
    {
        GridSlot slot = isPlayer ? playerGrid[slotIndex] : enemyGrid[slotIndex];
        Transform slotTransform = isPlayer ? playerGridSlots[slotIndex] : enemyGridSlots[slotIndex];

        GameObject unitPrefab = LoadUnitPrefab(unitData);

        if (unitPrefab == null)
        {
            Debug.LogError($"❌ Unit prefab not found for: {unitData.toyName}");
            return false;
        }

        GameObject unitObj = Instantiate(unitPrefab, slotTransform);
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

        ArrangeUnitsInSlot(slotIndex, isPlayer);
        UpdatePermanentState(slotIndex, isPlayer);

        EventManager.OnUnitSpawn(runtimeUnit);

        Debug.Log($"✅ Spawned single unit: {unitData.toyName} at slot {slotIndex}");

        return true;
    }

    // ===== SPAWN MULTIPLE UNITS (NEW!) =====

    private bool SpawnMultipleUnits(ToyUnitData unitData, bool isPlayer, int slotIndex)
    {
        GridSlot slot = isPlayer ? playerGrid[slotIndex] : enemyGrid[slotIndex];
        Transform slotTransform = isPlayer ? playerGridSlots[slotIndex] : enemyGridSlots[slotIndex];

        int unitsToSpawn = Mathf.Min(unitData.unitsPerSlot, unitData.maxStackPerSlot);
        List<RuntimeUnit> spawnedUnits = new List<RuntimeUnit>();

        Debug.Log($"🎯 Spawning {unitsToSpawn} units for {unitData.toyName}");

        GameObject unitPrefab = LoadUnitPrefab(unitData);

        if (unitPrefab == null)
        {
            Debug.LogError($"❌ Unit prefab not found for: {unitData.toyName}");
            return false;
        }

        // Spawn each unit
        for (int i = 0; i < unitsToSpawn; i++)
        {
            // Calculate position
            Vector3 spawnPos = slotTransform.position;

            if (i < unitData.unitOffsets.Length)
            {
                spawnPos += unitData.unitOffsets[i];
            }
            else
            {
                // Default offset (spread left-right)
                float offsetX = (i - (unitsToSpawn - 1) / 2f) * 0.8f;
                spawnPos += Vector3.right * offsetX;
            }

            // Instantiate
            GameObject unitObj = Instantiate(unitPrefab, spawnPos, Quaternion.identity, slotTransform);
            RuntimeUnit unit = unitObj.GetComponent<RuntimeUnit>();

            if (unit == null)
            {
                Debug.LogError($"❌ RuntimeUnit component not found on prefab: {unitData.toyName}");
                Destroy(unitObj);
                continue;
            }

            // Initialize
            unit.Initialize(unitData, slotIndex, isPlayer);

            // Inject dependencies
            container.InjectGameObject(unitObj);

            // Store
            spawnedUnits.Add(unit);
            slot.units.Add(unit);

            EventManager.OnUnitSpawn(unit);
        }

        // Set unit type
        if (slot.unitType == null)
        {
            slot.unitType = unitData;
        }

        // ✅ Link twins if this is a twin system (Punchy Bots)
        if (unitData.isTwinSystem && spawnedUnits.Count >= 2)
        {
            LinkTwinUnits(spawnedUnits);
        }

        UpdatePermanentState(slotIndex, isPlayer);

        Debug.Log($"✅ Spawned {spawnedUnits.Count} units for {unitData.toyName} at slot {slotIndex}");

        return spawnedUnits.Count > 0;
    }

    // ===== LINK TWIN UNITS =====

    private void LinkTwinUnits(List<RuntimeUnit> units)
    {
        // Set first unit as primary
        PunchyBotsUnit primary = units[0].GetComponent<PunchyBotsUnit>();
        if (primary != null)
        {
            primary.isPrimaryBot = true;
        }

        // Link all units to each other
        for (int i = 0; i < units.Count; i++)
        {
            PunchyBotsUnit currentBot = units[i].GetComponent<PunchyBotsUnit>();
            if (currentBot == null) continue;

            // Set secondary flag
            if (i > 0)
            {
                currentBot.isPrimaryBot = false;
            }

            // Link to all other bots
            for (int j = 0; j < units.Count; j++)
            {
                if (i == j) continue;

                PunchyBotsUnit otherBot = units[j].GetComponent<PunchyBotsUnit>();
                if (otherBot != null)
                {
                    currentBot.LinkTwin(otherBot);
                    break; // For now, just link to first twin
                }
            }
        }

        Debug.Log($"👊 Linked {units.Count} twin units");
    }

    // ===== FIND SLOT FOR UNIT =====

    private int FindSlotForUnit(ToyUnitData unitData, bool isPlayer)
    {
        GridSlot[] targetGrid = isPlayer ? playerGrid : enemyGrid;

        // Try to find existing slot with same unit type
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

        // Find empty slot
        for (int i = 0; i < targetGrid.Length; i++)
        {
            if (targetGrid[i].IsEmpty)
            {
                return i;
            }
        }

        return -1;
    }

    // ===== ARRANGE IN SLOT (DRAFT) =====

    private void ArrangeUnitsInSlot(int slotIndex, bool isPlayer)
    {
        GridSlot slot = isPlayer ? playerGrid[slotIndex] : enemyGrid[slotIndex];
        int unitCount = slot.units.Count;

        if (unitCount == 0) return;

        // Don't rearrange multi-unit spawns (they have custom offsets)
        if (slot.unitType != null && slot.unitType.isMultiUnit)
        {
            return;
        }

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

    // ===== ARRANGE FORMATION (BATTLE START) =====

    public IEnumerator ArrangeUnitsInFormationCoroutine(bool isPlayer)
    {
        Debug.Log($"🎯 Arranging formation for {(isPlayer ? "PLAYER" : "ENEMY")}");

        GridSlot[] targetGrid = isPlayer ? playerGrid : enemyGrid;

        // 1. Collect units by type
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

        // 2. Sort by arrangementIndex
        var sortedUnitTypes = unitsByType.Keys.OrderByDescending(u => u.arrangementIndex).ToList();

        // 3. Prepare units for formation (disable NavMeshAgent)
        foreach (var units in unitsByType.Values)
        {
            foreach (var unit in units)
            {
                unit.PrepareForFormation();
            }
        }

        // 4. Calculate positions and animate
        float currentZ = isPlayer ? baseBackPositionZ : enemyBasePositionZ;
        float zDirection = isPlayer ? 1f : -1f;

        foreach (var unitType in sortedUnitTypes)
        {
            List<RuntimeUnit> unitsOfType = unitsByType[unitType];
            int totalUnits = unitsOfType.Count;
            int maxPerRow = unitType.maxUnitsPerRow;
            float spacing = unitType.unitSpacing;

            int rowsNeeded = Mathf.CeilToInt((float)totalUnits / maxPerRow);

            int unitIndex = 0;

            for (int row = 0; row < rowsNeeded; row++)
            {
                int unitsInRow = Mathf.Min(maxPerRow, totalUnits - unitIndex);
                float rowZ = currentZ + (row * rowToRowOffset * zDirection);

                List<Vector3> rowPositions = CalculateRowPositions(unitsInRow, spacing, rowZ);

                for (int i = 0; i < unitsInRow && unitIndex < totalUnits; i++)
                {
                    RuntimeUnit unit = unitsOfType[unitIndex];
                    Vector3 targetPosition = rowPositions[i];

                    // ✅ DOTween animation (agent is disabled)
                    unit.transform.DOMove(targetPosition, formationAnimationDuration)
                        .SetEase(Ease.OutQuad);

                    unitIndex++;
                }
            }

            currentZ += (rowsNeeded * rowToRowOffset + spacing) * zDirection;
        }

        // 5. Wait for animation
        yield return new WaitForSeconds(formationAnimationDuration);

        // 6. Formation complete - enable agents
        foreach (var units in unitsByType.Values)
        {
            foreach (var unit in units)
            {
                unit.FormationComplete();
            }
        }

        Debug.Log($"✅ Formation complete for {(isPlayer ? "PLAYER" : "ENEMY")}");
    }

    /// <summary>
    /// Non-coroutine version for backward compatibility
    /// </summary>
    public void ArrangeUnitsInFormation(bool isPlayer)
    {
        StartCoroutine(ArrangeUnitsInFormationCoroutine(isPlayer));
    }

    private List<Vector3> CalculateRowPositions(int unitCount, float spacing, float zPos)
    {
        List<Vector3> positions = new List<Vector3>();
        float halfWidth = (unitCount - 1) * spacing / 2f;

        for (int i = 0; i < unitCount; i++)
        {
            float xPos = -halfWidth + (i * spacing);
            positions.Add(new Vector3(xPos, 0, zPos));
        }

        return positions;
    }

    // ===== PERMANENT STATE =====

    private void UpdatePermanentState(int slotIndex, bool isPlayer)
    {
        GridSlot slot = isPlayer ? playerGrid[slotIndex] : enemyGrid[slotIndex];
        var stateDict = isPlayer ? playerGridState : enemyGridState;

        if (slot.IsEmpty)
        {
            stateDict.Remove(slotIndex);
        }
        else
        {
            // ✅ For multi-units, save actual count but spawn will handle multiplication
            int stateCount = slot.units.Count;

            // ✅ CRITICAL: If this is a multi-unit, divide by unitsPerSlot to get "spawn count"
            // Example: 2 Punchy Bots in slot → Save as 1 spawn (will create 2 units)
            if (slot.unitType != null && slot.unitType.isMultiUnit && slot.unitType.unitsPerSlot > 1)
            {
                stateCount = Mathf.CeilToInt((float)slot.units.Count / slot.unitType.unitsPerSlot);
            }

            stateDict[slotIndex] = new GridSlotData(slot.unitType, slotIndex, stateCount);

            Debug.Log($"💾 Saved state: Slot {slotIndex}, UnitType: {slot.unitType.toyName}, StateCount: {stateCount}, ActualUnits: {slot.units.Count}");
        }
    }

    // ===== LOAD PREFAB =====

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
            Debug.LogError("Direct prefab reference not implemented!");
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

    // ===== UTILITY =====

    public void IncreaseDeployLimit()
    {
        maxDeployCount++;
        Debug.Log($"Deploy limit increased to {maxDeployCount}");
    }

    public void ClearSceneSlot(int slotIndex, bool isPlayer, RuntimeUnit deadUnit)
    {
        GridSlot[] targetGrid = isPlayer ? playerGrid : enemyGrid;

        if (slotIndex >= 0 && slotIndex < targetGrid.Length)
        {
            GridSlot slot = targetGrid[slotIndex];
            slot.units.Remove(deadUnit);
        }
    }

    public void ClearSceneObjects()
    {
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
        var playerStateSnapshot = playerGridState.ToList();
        var enemyStateSnapshot = enemyGridState.ToList();

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
    }

    public void ResetGridState()
    {
        ClearSceneObjects();
        playerGridState.Clear();
        enemyGridState.Clear();
        InitializeGrids();
    }
}