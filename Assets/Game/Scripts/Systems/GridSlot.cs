// ============================================================================
// GRID SLOT - Multi-Unit Stack Container (FIXED)
// Her slot birden fazla aynı karakteri tutabilir
// ✅ FIX: Null/dead unit cleanup added
// ============================================================================

using System.Collections.Generic;

public class GridSlot
{
    public int slotIndex;
    public ToyUnitData unitType; // Bu slot'ta hangi karakter var
    public List<RuntimeUnit> units = new List<RuntimeUnit>();

    // ===== PROPERTIES (FIXED) =====

    public int UnitCount
    {
        get
        {
            // ✅ Clean up dead/null units before counting
            CleanupDeadUnits();
            return units.Count;
        }
    }

    public bool IsEmpty
    {
        get
        {
            // ✅ Clean up dead/null units before checking
            CleanupDeadUnits();
            return units.Count == 0;
        }
    }

    // ===== METHODS (FIXED) =====

    public bool CanAddUnit(ToyUnitData newUnit)
    {
        // ✅ Clean up dead/null units first
        CleanupDeadUnits();

        // Boşsa eklenebilir
        if (units.Count == 0)
        {
            return true;
        }

        // ✅ CRITICAL: Check if unitType is null (shouldn't happen but safety)
        if (unitType == null)
        {
            UnityEngine.Debug.LogWarning($"⚠️ Slot {slotIndex} has units but no unitType! Fixing...");
            // Try to recover unitType from existing units
            if (units.Count > 0 && units[0] != null)
            {
                unitType = units[0].data;
            }
            else
            {
                return true; // Allow spawn if can't determine type
            }
        }

        // Aynı karakterse VE limit dolmadıysa eklenebilir
        if (unitType.unitID == newUnit.unitID && units.Count < unitType.maxStackPerSlot)
        {
            return true;
        }

        return false;
    }

    public void Clear()
    {
        units.Clear();
        unitType = null;
    }

    // ✅ NEW: Cleanup method
    private void CleanupDeadUnits()
    {
        // Remove null and dead units
        units.RemoveAll(u => u == null || !u.IsAlive());

        // ✅ If no units left, clear unitType too
        if (units.Count == 0 && unitType != null)
        {
            unitType = null;
        }
    }

    // ✅ NEW: Force refresh method (call this from GridManager if needed)
    public void RefreshState()
    {
        CleanupDeadUnits();
    }
}