// ============================================================================
// GRID SLOT - Multi-Unit Stack Container
// Her slot birden fazla ayný karakteri tutabilir
// ============================================================================

using System.Collections.Generic;

public class GridSlot
{
    public int slotIndex;
    public ToyUnitData unitType; // Bu slot'ta hangi karakter var
    public List<RuntimeUnit> units = new List<RuntimeUnit>();

    // ===== PROPERTIES =====

    public int UnitCount => units.Count;
    public bool IsEmpty => units.Count == 0;

    // ===== METHODS =====

    public bool CanAddUnit(ToyUnitData newUnit)
    {
        // Boþsa eklenebilir
        if (IsEmpty) return true;

        // Ayný karakterse VE limit dolmadýysa eklenebilir
        if (unitType.unitID == newUnit.unitID && units.Count < unitType.maxStackPerSlot)
            return true;

        return false;
    }

    public void Clear()
    {
        units.Clear();
        unitType = null;
    }
}