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
    [Inject] GameManager gameManager;
    [Inject] PoolingSystem poolingSystem;
    [Inject] AudioManager audioManager;

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

        // ✅ CRITICAL: Clean up dead units before checking
        slot.units.RemoveAll(u => u == null || !u.IsAlive());

        // ✅ DEBUG: Log slot state
        Debug.Log($"🎯 SpawnUnit: {unitData.toyName} in slot {slotIndex}. Current units: {slot.units.Count}, IsEmpty: {slot.IsEmpty}");

        // Check if slot can accept
        if (!slot.CanAddUnit(unitData))
        {
            Debug.LogWarning($"❌ Slot {slotIndex} cannot accept {unitData.toyName}. Current: {slot.units.Count}/{unitData.maxStackPerSlot}, UnitType: {slot.unitType?.toyName}");
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

        // ✅ Current unit count (before adding new one)
        int currentUnitCount = slot.units.Count;
        int futureUnitCount = currentUnitCount + 1; // After adding new one

        // ✅ STEP 1: Re-arrange existing units if any exist
        if (currentUnitCount > 0)
        {
            Debug.Log($"📐 Re-arranging {currentUnitCount} existing units for future total: {futureUnitCount}");

            // Calculate new grid size based on future count
            int futureGridSize = Mathf.CeilToInt(Mathf.Sqrt(futureUnitCount));
            float spacing = slot.unitType.unitSpacing;
            float centerOffset = -((futureGridSize - 1) * spacing) / 2f;

            // Move existing units to their new positions
            for (int i = 0; i < slot.units.Count; i++)
            {
                RuntimeUnit existingUnit = slot.units[i];
                if (existingUnit == null || !existingUnit.IsAlive()) continue;

                int row = i / futureGridSize;
                int col = i % futureGridSize;

                float xPos = centerOffset + (col * spacing);
                float zPos = centerOffset + (row * spacing);

                Vector3 localPos = new Vector3(xPos, 0, zPos);
                Vector3 targetWorldPos = slotTransform.TransformPoint(localPos);

                // Smooth movement
                existingUnit.transform.DOMove(targetWorldPos, 0.3f).SetEase(Ease.OutQuad);

                Debug.Log($"   → Moving existing unit #{i} to [{row},{col}] at {targetWorldPos}");
            }
        }

        // ✅ STEP 2: Calculate spawn position for NEW unit
        int newUnitIndex = currentUnitCount; // This is the index where new unit will be
        int futureGridSize2 = Mathf.CeilToInt(Mathf.Sqrt(futureUnitCount));
        float spacing2 = (slot.unitType != null) ? slot.unitType.unitSpacing : unitData.unitSpacing;
        float centerOffset2 = -((futureGridSize2 - 1) * spacing2) / 2f;

        int newRow = newUnitIndex / futureGridSize2;
        int newCol = newUnitIndex % futureGridSize2;

        float newXPos = centerOffset2 + (newCol * spacing2);
        float newZPos = centerOffset2 + (newRow * spacing2);

        Vector3 newLocalPos = new Vector3(newXPos, 0, newZPos);
        Vector3 spawnPos = slotTransform.TransformPoint(newLocalPos);

        Debug.Log($"✅ New unit will spawn at index {newUnitIndex} → [{newRow},{newCol}] at {spawnPos}");

        // ✅ STEP 3: Instantiate new unit at correct position
        GameObject unitObj = Instantiate(unitPrefab, spawnPos, Quaternion.identity, slotTransform);
        unitObj.SetActive(false);
        RuntimeUnit runtimeUnit = unitObj.GetComponent<RuntimeUnit>();

        if (runtimeUnit == null)
        {
            Debug.LogError($"❌ RuntimeUnit component not found on prefab: {unitData.toyName}");
            Destroy(unitObj);
            return false;
        }

        runtimeUnit.Initialize(unitData, slotIndex, isPlayer);

        // ✅ ROTATION FIX
        if (!isPlayer)
        {
            unitObj.transform.rotation = Quaternion.Euler(0, 180, 0);
        }
        else
        {
            unitObj.transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        // ✅ STEP 4: Add to slot AFTER positioning
        slot.units.Add(runtimeUnit);

        // Set unit type if first unit
        if (slot.unitType == null)
        {
            slot.unitType = unitData;
        }

        container.InjectGameObject(unitObj);

        // ✅ Animate if in draft phase
        if (gameManager != null && gameManager.currentState == GameState.Draft)
        {
            DraftCardSpawnAnimation animator = FindObjectOfType<DraftCardSpawnAnimation>();
            if (animator != null)
            {
                unitObj.SetActive(false); // Hide first
                animator.SpawnUnitInSlot(runtimeUnit, null);
            }
        }

        UpdatePermanentState(slotIndex, isPlayer);
        EventManager.OnUnitSpawn(runtimeUnit);

        Debug.Log($"✅ Spawned: {unitData.toyName} at slot {slotIndex}, grid position [{newRow},{newCol}]");

        return true;
    }

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

        // ✅ Current and future counts
        int currentUnitCount = slot.units.Count;
        int futureUnitCount = currentUnitCount + unitsToSpawn;

        // ✅ STEP 1: Re-arrange existing units if any exist
        if (currentUnitCount > 0)
        {
            Debug.Log($"📐 Re-arranging {currentUnitCount} existing units for future total: {futureUnitCount}");

            int futureGridSize = Mathf.CeilToInt(Mathf.Sqrt(futureUnitCount));
            float spacing = slot.unitType.unitSpacing;
            float centerOffset = -((futureGridSize - 1) * spacing) / 2f;

            // Move existing units
            for (int i = 0; i < slot.units.Count; i++)
            {
                RuntimeUnit existingUnit = slot.units[i];
                if (existingUnit == null || !existingUnit.IsAlive()) continue;

                int row = i / futureGridSize;
                int col = i % futureGridSize;

                float xPos = centerOffset + (col * spacing);
                float zPos = centerOffset + (row * spacing);

                Vector3 localPos = new Vector3(xPos, 0, zPos);
                Vector3 targetWorldPos = slotTransform.TransformPoint(localPos);

                existingUnit.transform.DOMove(targetWorldPos, 0.3f).SetEase(Ease.OutQuad);

                Debug.Log($"   → Moving existing unit #{i} to [{row},{col}]");
            }
        }

        // ✅ STEP 2: Spawn new units at their correct positions
        int futureGridSize2 = Mathf.CeilToInt(Mathf.Sqrt(futureUnitCount));
        float spacing2 = (slot.unitType != null) ? slot.unitType.unitSpacing : unitData.unitSpacing;
        float centerOffset2 = -((futureGridSize2 - 1) * spacing2) / 2f;

        for (int i = 0; i < unitsToSpawn; i++)
        {
            // Calculate position for this new unit
            int newUnitIndex = currentUnitCount + i; // Current count + loop offset
            int row = newUnitIndex / futureGridSize2;
            int col = newUnitIndex % futureGridSize2;

            float xPos = centerOffset2 + (col * spacing2);
            float zPos = centerOffset2 + (row * spacing2);

            Vector3 localPos = new Vector3(xPos, 0, zPos);
            Vector3 spawnPos = slotTransform.TransformPoint(localPos);

            Debug.Log($"✅ Spawning unit #{i} at index {newUnitIndex} → [{row},{col}] at {spawnPos}");

            // Instantiate
            GameObject unitObj = Instantiate(unitPrefab, spawnPos, Quaternion.identity, slotTransform);
            unitObj.SetActive(false);
            RuntimeUnit unit = unitObj.GetComponent<RuntimeUnit>();

            if (unit == null)
            {
                Debug.LogError($"❌ RuntimeUnit component not found on prefab: {unitData.toyName}");
                Destroy(unitObj);
                continue;
            }

            unit.Initialize(unitData, slotIndex, isPlayer);

            // ✅ ROTATION FIX
            if (!isPlayer)
            {
                unitObj.transform.rotation = Quaternion.Euler(0, 180, 0);
            }
            else
            {
                unitObj.transform.rotation = Quaternion.Euler(0, 0, 0);
            }

            container.InjectGameObject(unitObj);

            spawnedUnits.Add(unit);
            slot.units.Add(unit);

            EventManager.OnUnitSpawn(unit);
        }

        // Set unit type if first spawn
        if (slot.unitType == null)
        {
            slot.unitType = unitData;
        }

        // ✅ Link twins if needed
        if (unitData.isTwinSystem && spawnedUnits.Count >= 2)
        {
            LinkTwinUnits(spawnedUnits);
        }

        // ✅ Animate if in draft phase
        if (gameManager != null && gameManager.currentState == GameState.Draft)
        {
            DraftCardSpawnAnimation animator = FindObjectOfType<DraftCardSpawnAnimation>();
            if (animator != null)
            {
                StartCoroutine(AnimateMultipleUnits(spawnedUnits, slotIndex, isPlayer, animator));
            }
        }

        UpdatePermanentState(slotIndex, isPlayer);

        Debug.Log($"✅ Spawned {spawnedUnits.Count} units for {unitData.toyName} at slot {slotIndex}");

        return spawnedUnits.Count > 0;
    }    // ✅ UPDATE AnimateMultipleUnits - no arrangement needed
    private IEnumerator AnimateMultipleUnits(List<RuntimeUnit> units, int slotIndex, bool isPlayer, DraftCardSpawnAnimation animator)
    {
        // Hide all units first
        foreach (var unit in units)
        {
            unit.gameObject.SetActive(false);
        }

        // Animate each unit with delay
        foreach (var unit in units)
        {
            animator.SpawnUnitInSlot(unit, null);
            yield return new WaitForSeconds(0.15f); // Small delay between units
        }

        // No need to arrange after animation - already at correct positions!
        //ArrangeUnitsInSlot(slotIndex, isPlayer); // ❌ REMOVE THIS
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

    private int FindSlotForUnit(ToyUnitData unitData, bool isPlayer)
    {
        GridSlot[] targetGrid = isPlayer ? playerGrid : enemyGrid;
        string team = isPlayer ? "PLAYER" : "ENEMY";

        Debug.Log($"🔍 FindSlotForUnit: Looking for slot for {unitData.toyName} ({team})");

        // ✅ STEP 1: Force refresh ALL slots
        for (int i = 0; i < targetGrid.Length; i++)
        {
            targetGrid[i].RefreshState(); // This will cleanup dead units
        }

        // ✅ STEP 2: Try to find existing slot with same unit type
        for (int i = 0; i < targetGrid.Length; i++)
        {
            if (!targetGrid[i].IsEmpty)
            {
                // Check if same unit type and has space
                if (targetGrid[i].unitType != null &&
                    targetGrid[i].unitType.unitID == unitData.unitID &&
                    targetGrid[i].units.Count < unitData.maxStackPerSlot)
                {
                    Debug.Log($"   ✅ Found MATCHING slot {i} for {unitData.toyName} (has {targetGrid[i].units.Count}/{unitData.maxStackPerSlot})");
                    return i;
                }
            }
        }

        // ✅ STEP 3: Find empty slot
        for (int i = 0; i < targetGrid.Length; i++)
        {
            if (targetGrid[i].IsEmpty)
            {
                Debug.Log($"   ✅ Found EMPTY slot {i} for {unitData.toyName}");
                return i;
            }
        }

        // ❌ No slot found
        Debug.LogWarning($"❌ NO SLOT FOUND for {unitData.toyName} ({team})! Grid state:");
        for (int i = 0; i < targetGrid.Length; i++)
        {
            if (!targetGrid[i].IsEmpty)
            {
                Debug.LogWarning($"   Slot {i}: {targetGrid[i].units.Count} units of {targetGrid[i].unitType?.toyName}");
            }
            else
            {
                Debug.Log($"   Slot {i}: EMPTY");
            }
        }

        return -1;
    }
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
    public GridSlot GetPlayerSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < playerGrid.Length)
        {
            return playerGrid[slotIndex];
        }
        return null;
    }

    public GridSlot GetEnemySlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < enemyGrid.Length)
        {
            return enemyGrid[slotIndex];
        }
        return null;
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
        StartCoroutine(ClearSceneObjectsWithEffect());
    }

    private IEnumerator ClearSceneObjectsWithEffect()
    {
        List<RuntimeUnit> allUnits = new List<RuntimeUnit>();

        // Collect all units
        foreach (var slot in playerGrid)
        {
            allUnits.AddRange(slot.units);
        }

        foreach (var slot in enemyGrid)
        {
            allUnits.AddRange(slot.units);
        }

        Debug.Log($"🧹 Clearing {allUnits.Count} units with poof effect...");

        // Destroy each unit with effect
        foreach (var unit in allUnits)
        {
            if (unit != null && unit.gameObject != null)
            {
                // Play poof VFX
                Vector3 poofPos = unit.transform.position;
                poofPos.y += 0.5f;

                if (poolingSystem != null)
                {
                    GameObject vfx = poolingSystem.InstantiateAPS("DeathVfx", poofPos);
                    if (vfx != null)
                    {
                        container.InjectGameObject(vfx);
                        poolingSystem.DestroyAPS(vfx, 1f);
                    }
                }

                // Play sound
                if (audioManager != null)
                {
                    audioManager.Play("unit_despawn");
                }

                // Destroy unit
                Destroy(unit.gameObject);

                // Small delay between each
                yield return new WaitForSeconds(0.05f);
            }
        }

        // Clear lists
        foreach (var slot in playerGrid)
        {
            slot.units.Clear();
        }

        foreach (var slot in enemyGrid)
        {
            slot.units.Clear();
        }

        Debug.Log("✅ All units cleared with poof effects!");
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
    public List<RuntimeUnit> GetPreviousUnits()
    {
        List<RuntimeUnit> unitsToAnimate = new List<RuntimeUnit>();

        // ✅ Don't spawn here! Just spawn and collect references
        // Make snapshot to avoid modification during enumeration
        var playerStateSnapshot = playerGridState.ToList();
        var enemyStateSnapshot = enemyGridState.ToList();

        // Spawn player units
        foreach (var kvp in playerStateSnapshot)
        {
            GridSlotData slotData = kvp.Value;
            if (slotData.isFilled && slotData.unitData != null)
            {
                for (int i = 0; i < slotData.unitCount; i++)
                {
                    // Spawn and get units
                    int slotIndex = kvp.Key;
                    GridSlot slot = playerGrid[slotIndex];
                    int beforeCount = slot.units.Count;

                    if (SpawnUnit(slotData.unitData, true, slotIndex))
                    {
                        // Add newly spawned units
                        for (int j = beforeCount; j < slot.units.Count; j++)
                        {
                            unitsToAnimate.Add(slot.units[j]);
                        }
                    }
                }
            }
        }

        // Spawn enemy units
        foreach (var kvp in enemyStateSnapshot)
        {
            GridSlotData slotData = kvp.Value;
            if (slotData.isFilled && slotData.unitData != null)
            {
                for (int i = 0; i < slotData.unitCount; i++)
                {
                    int slotIndex = kvp.Key;
                    GridSlot slot = enemyGrid[slotIndex];
                    int beforeCount = slot.units.Count;

                    if (SpawnUnit(slotData.unitData, false, slotIndex))
                    {
                        for (int j = beforeCount; j < slot.units.Count; j++)
                        {
                            unitsToAnimate.Add(slot.units[j]);
                        }
                    }
                }
            }
        }

        Debug.Log($"📋 GetPreviousUnits: {unitsToAnimate.Count} units spawned for animation");
        return unitsToAnimate;
    }
   /* public List<RuntimeUnit> GetPreviousUnits()
    {
        List<RuntimeUnit> unitsToAnimate = new List<RuntimeUnit>();

        Debug.Log("🔄 GetPreviousUnits: Starting respawn process");

        // ✅ CRITICAL: DESTROY old GameObjects physically first!
        for (int i = 0; i < playerGrid.Length; i++)
        {
            // Destroy actual GameObjects
            foreach (var unit in playerGrid[i].units)
            {
                if (unit != null && unit.gameObject != null)
                {
                    Debug.Log($"🗑️ Destroying old player unit: {unit.data.toyName}");
                    Destroy(unit.gameObject);
                }
            }
            playerGrid[i].Clear(); // Then clear the list
        }

        for (int i = 0; i < enemyGrid.Length; i++)
        {
            // Destroy actual GameObjects
            foreach (var unit in enemyGrid[i].units)
            {
                if (unit != null && unit.gameObject != null)
                {
                    Debug.Log($"🗑️ Destroying old enemy unit: {unit.data.toyName}");
                    Destroy(unit.gameObject);
                }
            }
            enemyGrid[i].Clear();
        }

        Debug.Log("🧹 Destroyed and cleared all old units");

        // Make snapshot to avoid modification during enumeration
        var playerStateSnapshot = playerGridState.ToList();
        var enemyStateSnapshot = enemyGridState.ToList();

        Debug.Log($"📊 State snapshot: {playerStateSnapshot.Count} player slots, {enemyStateSnapshot.Count} enemy slots");

        // Spawn player units
        foreach (var kvp in playerStateSnapshot)
        {
            GridSlotData slotData = kvp.Value;
            if (slotData.isFilled && slotData.unitData != null)
            {
                int slotIndex = kvp.Key;
                GridSlot slot = playerGrid[slotIndex];

                Debug.Log($"🔄 Respawning {slotData.unitCount}x {slotData.unitData.toyName} in PLAYER slot {slotIndex}");

                for (int i = 0; i < slotData.unitCount; i++)
                {
                    int beforeCount = slot.units.Count;

                    if (SpawnUnit(slotData.unitData, true, slotIndex))
                    {
                        // Add newly spawned units
                        for (int j = beforeCount; j < slot.units.Count; j++)
                        {
                            unitsToAnimate.Add(slot.units[j]);
                        }
                    }
                    else
                    {
                        Debug.LogError($"❌ Failed to respawn player unit #{i} in slot {slotIndex}!");
                    }
                }

                Debug.Log($"   ✅ Player slot {slotIndex} → {slot.units.Count} units, unitType={slot.unitType?.toyName}");
            }
        }

        // Spawn enemy units
        foreach (var kvp in enemyStateSnapshot)
        {
            GridSlotData slotData = kvp.Value;
            if (slotData.isFilled && slotData.unitData != null)
            {
                int slotIndex = kvp.Key;
                GridSlot slot = enemyGrid[slotIndex];

                Debug.Log($"🔄 Respawning {slotData.unitCount}x {slotData.unitData.toyName} in ENEMY slot {slotIndex}");

                for (int i = 0; i < slotData.unitCount; i++)
                {
                    int beforeCount = slot.units.Count;

                    if (SpawnUnit(slotData.unitData, false, slotIndex))
                    {
                        for (int j = beforeCount; j < slot.units.Count; j++)
                        {
                            unitsToAnimate.Add(slot.units[j]);
                        }
                    }
                    else
                    {
                        Debug.LogError($"❌ Failed to respawn enemy unit #{i} in slot {slotIndex}!");
                    }
                }

                Debug.Log($"   ✅ Enemy slot {slotIndex} → {slot.units.Count} units, unitType={slot.unitType?.toyName}");
            }
        }

        Debug.Log($"✅ GetPreviousUnits complete: {unitsToAnimate.Count} units ready for animation");

        PrintGridState();

        return unitsToAnimate;
    }
   */
    private void PrintGridState()
    {
        Debug.Log("📊 === FINAL GRID STATE ===");

        Debug.Log("🔵 PLAYER GRID:");
        for (int i = 0; i < playerGrid.Length; i++)
        {
            if (!playerGrid[i].IsEmpty)
            {
                Debug.Log($"   Slot {i}: {playerGrid[i].units.Count} units of {playerGrid[i].unitType?.toyName}");
            }
        }

        Debug.Log("🔴 ENEMY GRID:");
        for (int i = 0; i < enemyGrid.Length; i++)
        {
            if (!enemyGrid[i].IsEmpty)
            {
                Debug.Log($"   Slot {i}: {enemyGrid[i].units.Count} units of {enemyGrid[i].unitType?.toyName}");
            }
        }

        Debug.Log("========================");
    }
    // ✅ Helper: Count filled slots
    private int CountFilledSlots(GridSlot[] grid)
    {
        int count = 0;
        foreach (var slot in grid)
        {
            if (!slot.IsEmpty) count++;
        }
        return count;
    }
    public void ResetGridState()
    {
        ClearSceneObjects();
        playerGridState.Clear();
        enemyGridState.Clear();
        InitializeGrids();
    }
}