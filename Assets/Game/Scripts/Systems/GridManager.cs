// ============================================================================
// GRID MANAGER - 3x2 Grid sistemini yönetir (6 slot)
// ✅ BATTLE DEATHS DON'T AFFECT DRAFT STATE
// ✅ State only changes during DRAFT (when player selects cards)
// ✅ Battle is temporary - all units respawn after
// ✅ FIX: Don't rearrange units during battle (they're fighting!)
// ============================================================================

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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

    [Header("Settings")]
    [SerializeField] int maxDeployCount = GameConstants.GRID_SIZE;

    // ===== GRID SLOTS (Multi-Unit) =====
    private GridSlot[] playerGrid = new GridSlot[GameConstants.GRID_SIZE];
    private GridSlot[] enemyGrid = new GridSlot[GameConstants.GRID_SIZE];

    // ===== PERMANENT STATE - Only changes during DRAFT =====
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

    // ✅ PERMANENT STATE - NEVER changes during battle
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

    // ===== SPAWN UNIT (ONLY DURING DRAFT) =====

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

        // ✅ Arrange units ONLY during draft (initial spawn)
        ArrangeUnitsInSlot(slotIndex, isPlayer);

        // ✅ Update PERMANENT state (only during draft)
        UpdatePermanentState(slotIndex, isPlayer);

        EventManager.OnUnitSpawn(runtimeUnit);

        return true;
    }

    // ===== FIND SLOT FOR UNIT =====

    private int FindSlotForUnit(ToyUnitData unitData, bool isPlayer)
    {
        GridSlot[] targetGrid = isPlayer ? playerGrid : enemyGrid;

        // 1. Aynı karakterin olduğu slot'u ara
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

        // 2. Boş slot bul
        for (int i = 0; i < targetGrid.Length; i++)
        {
            if (targetGrid[i].IsEmpty)
            {
                return i;
            }
        }

        return -1;
    }

    // ===== ARRANGE UNITS IN SLOT =====

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

    // ===== UPDATE PERMANENT STATE (ONLY DURING DRAFT) =====

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

    /// <summary>
    /// ✅ BATTLE DEATH - Only removes from BATTLE list, NOT from permanent state
    /// ✅ FIX: DON'T rearrange units during battle (they're fighting!)
    /// </summary>
    public void ClearSceneSlot(int slotIndex, bool isPlayer, RuntimeUnit deadUnit)
    {
        GridSlot[] targetGrid = isPlayer ? playerGrid : enemyGrid;

        if (slotIndex >= 0 && slotIndex < targetGrid.Length)
        {
            GridSlot slot = targetGrid[slotIndex];
            slot.units.Remove(deadUnit);

            Debug.Log($"💀 Battle death: Unit removed from slot {slotIndex}. Remaining in battle: {slot.units.Count}");
            Debug.Log($"💾 Permanent state UNCHANGED (battle deaths are temporary)");

            // ✅ FIX: DON'T call ArrangeUnitsInSlot during battle!
            // Units are in combat positions, they should stay where they are
            // Arrangement only happens during DRAFT when spawning

            // ✅ PERMANENT STATE NEVER CHANGES DURING BATTLE!
        }
    }

    /// <summary>
    /// Battle sonrası scene'deki tüm GameObject'leri temizler
    /// Permanent state NEVER touched
    /// </summary>
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
            // ✅ unitType is NOT cleared - it stays for respawn
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
            // ✅ unitType is NOT cleared - it stays for respawn
        }

        Debug.Log($"💾 PERMANENT State preserved:");
        foreach (var kvp in playerGridState)
        {
            Debug.Log($"   Player Slot {kvp.Key}: {kvp.Value.unitData.toyName} x{kvp.Value.unitCount}");
        }

        foreach (var kvp in enemyGridState)
        {
            Debug.Log($"   Enemy Slot {kvp.Key}: {kvp.Value.unitData.toyName} x{kvp.Value.unitCount}");
        }
    }

    /// <summary>
    /// ✅ RESPAWN - Always uses PERMANENT state (ignores battle deaths)
    /// </summary>
    public void RespawnPreviousUnits()
    {
        Debug.Log("♻️ Respawning from PERMANENT state (battle deaths ignored)");

        int playerRespawned = 0;
        int enemyRespawned = 0;

        var playerStateSnapshot = playerGridState.ToList();
        var enemyStateSnapshot = enemyGridState.ToList();

        // Player respawn
        foreach (var kvp in playerStateSnapshot)
        {
            int slot = kvp.Key;
            GridSlotData slotData = kvp.Value;

            if (slotData.isFilled && slotData.unitData != null)
            {
                Debug.Log($"♻️ Respawning {slotData.unitCount}x {slotData.unitData.toyName} in Player Slot {slot} (from permanent state)");

                for (int i = 0; i < slotData.unitCount; i++)
                {
                    SpawnUnit(slotData.unitData, true, slot);
                    playerRespawned++;
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
                Debug.Log($"♻️ Respawning {slotData.unitCount}x {slotData.unitData.toyName} in Enemy Slot {slot} (from permanent state)");

                for (int i = 0; i < slotData.unitCount; i++)
                {
                    SpawnUnit(slotData.unitData, false, slot);
                    enemyRespawned++;
                }
            }
        }

        Debug.Log($"✅ Respawned {playerRespawned} player units, {enemyRespawned} enemy units (all with FULL HP)");
    }

    /// <summary>
    /// Yeni maç başlarken tüm state'i sıfırla
    /// </summary>
    public void ResetGridState()
    {
        Debug.Log("🔄 Resetting grid state - fresh start!");

        ClearSceneObjects();
        playerGridState.Clear();
        enemyGridState.Clear();
        InitializeGrids();

        Debug.Log("✅ Grid state reset complete");
    }
}