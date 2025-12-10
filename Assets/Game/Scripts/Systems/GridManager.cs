// ============================================================================
// GRID MANAGER - 3x2 Grid sistemini yönetir (6 slot)
// ✅ Multi-Unit Stack Support - Aynı karakterler aynı slot'ta toplanır
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
    [SerializeField] float unitSpacing = 0.25f; // Unit'ler arası mesafe (mini grid)

    // ===== GRID SLOTS (Multi-Unit) =====
    private GridSlot[] playerGrid = new GridSlot[GameConstants.GRID_SIZE];
    private GridSlot[] enemyGrid = new GridSlot[GameConstants.GRID_SIZE];

    // ===== GRID STATE - Persistent across battles =====
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
        // Player grid initialize
        for (int i = 0; i < playerGrid.Length; i++)
        {
            playerGrid[i] = new GridSlot { slotIndex = i };
        }

        // Enemy grid initialize
        for (int i = 0; i < enemyGrid.Length; i++)
        {
            enemyGrid[i] = new GridSlot { slotIndex = i };
        }

        Debug.Log("✅ Grid slots initialized");
    }

    // ===== SPAWN UNIT (MULTI-UNIT STACK SUPPORT) =====

    public bool SpawnUnit(ToyUnitData unitData, bool isPlayer, int slotIndex = -1)
    {
        GridSlot[] targetGrid = isPlayer ? playerGrid : enemyGrid;
        Transform[] targetSlots = isPlayer ? playerGridSlots : enemyGridSlots;

        // Check if grids are same reference (BUG!)
        if (playerGrid.GetHashCode() == enemyGrid.GetHashCode())
        {
            Debug.LogError("🚨 CRITICAL BUG: playerGrid and enemyGrid are SAME REFERENCE!");
        }

        // Slot bul
        if (slotIndex == -1)
        {
            slotIndex = FindSlotForUnit(unitData, isPlayer);
        }

        // Slot bulunamadı
        if (slotIndex == -1)
        {
            Debug.LogWarning($"❌ Cannot spawn {unitData.toyName} - No available slot!");
            Debug.Log($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            return false;
        }

        GridSlot slot = targetGrid[slotIndex];

        // Stack limit kontrolü
        if (!slot.CanAddUnit(unitData))
        {
            if (slot.units.Count >= unitData.maxStackPerSlot)
            {
                Debug.LogWarning($"❌ Slot {slotIndex} is full! (Max: {unitData.maxStackPerSlot})");
            }
            else
            {
                Debug.LogWarning($"❌ Slot {slotIndex} has different character! (Current: {slot.unitType.toyName})");
            }
            Debug.Log($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            return false;
        }

        // ===== 3D PREFAB SPAWN =====
        GameObject unitPrefab = LoadUnitPrefab(unitData);

        if (unitPrefab == null)
        {
            Debug.LogError($"❌ Unit prefab not found for: {unitData.toyName}");
            Debug.Log($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            return false;
        }

        // Instantiate 3D prefab (parent olarak slot transform)
        GameObject unitObj = Instantiate(unitPrefab, targetSlots[slotIndex]);

        // Get RuntimeUnit component
        RuntimeUnit runtimeUnit = unitObj.GetComponent<RuntimeUnit>();

        if (runtimeUnit == null)
        {
            Debug.LogError($"❌ RuntimeUnit component not found on prefab: {unitData.toyName}");
            Destroy(unitObj);
            Debug.Log($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            return false;
        }

        // Initialize runtime unit
        runtimeUnit.Initialize(unitData, slotIndex, isPlayer);

        // Slot'a ekle
        slot.units.Add(runtimeUnit);
        slot.unitType = unitData;

        // Zenject injection
        container.InjectGameObject(unitObj);

        // ✅ Layout'u güncelle (mini grid düzenle)
        ArrangeUnitsInSlot(slotIndex, isPlayer);

        // ✅ STATE KAYDET
        UpdateGridState(slotIndex, isPlayer);

        EventManager.OnUnitSpawn(runtimeUnit);

        return true;
    }
    // ===== FIND SLOT FOR UNIT =====

    private int FindSlotForUnit(ToyUnitData unitData, bool isPlayer)
    {
        GridSlot[] targetGrid = isPlayer ? playerGrid : enemyGrid;

        // 1. Önce aynı karakterin olduğu slot'u ara (stack yapılacak)
        for (int i = 0; i < targetGrid.Length; i++)
        {
            if (!targetGrid[i].IsEmpty)
            {
                Debug.Log($"   Slot {i}: {targetGrid[i].unitType.toyName} (Count: {targetGrid[i].units.Count}/{targetGrid[i].unitType.maxStackPerSlot})");

                if (targetGrid[i].unitType.unitID == unitData.unitID &&
                    targetGrid[i].units.Count < unitData.maxStackPerSlot)
                {
                    return i; // Aynı karakterin yanına ekle
                }
            }
            else
            {
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

        // 3. Hiç yer yok
        Debug.Log($"   ❌ No available slot!");
        return -1;
    }
    // ===== ARRANGE UNITS IN SLOT (MINI GRID LAYOUT) =====

    private void ArrangeUnitsInSlot(int slotIndex, bool isPlayer)
    {
        GridSlot slot = isPlayer ? playerGrid[slotIndex] : enemyGrid[slotIndex];
        int unitCount = slot.units.Count;

        if (unitCount == 0) return;

        // Grid boyutu hesapla (2×2, 3×3, 4×4...)
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(unitCount));

        // Merkez offset (grid'i ortala)
        float centerOffset = -((gridSize - 1) * unitSpacing) / 2f;

        // Her unit'i yerleştir
        int index = 0;
        for (int row = 0; row < gridSize && index < unitCount; row++)
        {
            for (int col = 0; col < gridSize && index < unitCount; col++)
            {
                RuntimeUnit unit = slot.units[index];

                // Pozisyon hesapla (X ve Z ekseninde grid)
                float xPos = centerOffset + (col * unit.data.unitSpacing);
                float zPos = centerOffset + (row * unit.data.unitSpacing);

                unit.transform.localPosition = new Vector3(xPos, 0, zPos);

                index++;
            }
        }

        Debug.Log($"📐 Arranged {unitCount} units in {gridSize}×{gridSize} grid (slot {slotIndex})");
    }

    // ===== UPDATE GRID STATE =====

    private void UpdateGridState(int slotIndex, bool isPlayer)
    {
        GridSlot slot = isPlayer ? playerGrid[slotIndex] : enemyGrid[slotIndex];
        var stateDict = isPlayer ? playerGridState : enemyGridState;

        if (slot.IsEmpty)
        {
            stateDict.Remove(slotIndex);
        }
        else
        {
            stateDict[slotIndex] = new GridSlotData(slot.unitType, slotIndex, slot.units.Count);
        }
    }

    // ===== LOAD UNIT PREFAB =====

    private GameObject LoadUnitPrefab(ToyUnitData unitData)
    {
        if (useResourcesFolder)
        {
            // Resources/Units/ klasöründen yükle
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
            Debug.LogError("Direct prefab reference not implemented. Use Resources folder!");
            return null;
        }
    }

    // ===== GET UNITS (Flatten all units from all slots) =====

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

    // ===== EXPAND SLOT (Bonus: +1 deploy limit) =====

    public void IncreaseDeployLimit()
    {
        maxDeployCount++;
        Debug.Log($"Deploy limit increased to {maxDeployCount}");
    }

    // ===== BATTLE STATE MANAGEMENT =====

    /// <summary>
    /// Unit öldüğünde slot'tan remove et AMA state'i koru
    /// </summary>
    public void ClearSceneSlot(int slotIndex, bool isPlayer, RuntimeUnit deadUnit)
    {
        GridSlot[] targetGrid = isPlayer ? playerGrid : enemyGrid;

        if (slotIndex >= 0 && slotIndex < targetGrid.Length)
        {
            GridSlot slot = targetGrid[slotIndex];
            slot.units.Remove(deadUnit);

            // Slot boşaldıysa temizle
            if (slot.IsEmpty)
            {
                slot.unitType = null;
            }
            else
            {
                // Kalan unit'leri yeniden düzenle
                ArrangeUnitsInSlot(slotIndex, isPlayer);
            }

            // State güncelle
            UpdateGridState(slotIndex, isPlayer);
        }
    }

    /// <summary>
    /// Battle sonrası scene'deki tüm GameObject'leri temizler AMA state'i korur
    /// </summary>
    public void ClearSceneObjects()
    {
        Debug.Log("🧹 Clearing scene objects (keeping state for next draft)");

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

        // Dictionary STATE KORUNUYOR - silmiyoruz!
        Debug.Log($"💾 State preserved: Player slots: {playerGridState.Count}, Enemy slots: {enemyGridState.Count}");
    }

    /// <summary>
    /// Bir sonraki draft'ta önceki karakterleri TAM CANLA geri getirir
    /// </summary>
    public void RespawnPreviousUnits()
    {
        Debug.Log("♻️ Respawning previous units with FULL HP");

        int playerRespawned = 0;
        int enemyRespawned = 0;

        // ✅ FIX: Dictionary'yi iterate ederken modify etmemek için ToList() kullan
        var playerStateSnapshot = playerGridState.ToList();
        var enemyStateSnapshot = enemyGridState.ToList();

        // Player unit'lerini respawn et
        foreach (var kvp in playerStateSnapshot)
        {
            int slot = kvp.Key;
            GridSlotData slotData = kvp.Value;

            if (slotData.isFilled && slotData.unitData != null)
            {
                // ✅ unitCount kadar spawn et (her biri tam canla)
                for (int i = 0; i < slotData.unitCount; i++)
                {
                    SpawnUnit(slotData.unitData, true, slot);
                    playerRespawned++;
                }
            }
        }

        // Enemy unit'lerini respawn et
        foreach (var kvp in enemyStateSnapshot)
        {
            int slot = kvp.Key;
            GridSlotData slotData = kvp.Value;

            if (slotData.isFilled && slotData.unitData != null)
            {
                // ✅ unitCount kadar spawn et
                for (int i = 0; i < slotData.unitCount; i++)
                {
                    SpawnUnit(slotData.unitData, false, slot);
                    enemyRespawned++;
                }
            }
        }

        Debug.Log($"✅ Respawned {playerRespawned} player units, {enemyRespawned} enemy units");
    }

    /// <summary>
    /// Yeni maç başlarken tüm state'i sıfırla
    /// </summary>
    public void ResetGridState()
    {
        Debug.Log("🔄 Resetting grid state - fresh start!");

        // Scene objeleri temizle
        ClearSceneObjects();

        // Dictionary'leri sıfırla
        playerGridState.Clear();
        enemyGridState.Clear();

        // Grid'leri yeniden initialize et
        InitializeGrids();

        Debug.Log("✅ Grid state reset complete");
    }
}