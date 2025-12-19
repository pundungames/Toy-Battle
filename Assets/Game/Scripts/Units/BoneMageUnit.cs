// ============================================================================
// BONE MAGE UNIT - WITH COORDINATED HP BUFF
// ✅ Spawns homing skull projectile
// ✅ Battle start: ALL Bone Mages buff SAME random slot
// ✅ Level 1: +2.5% HP per Bone Mage
// ✅ Level 2: +5% HP per Bone Mage
// ✅ Level 3: +7.5% HP per Bone Mage
// ✅ Stacking: 3 Bone Mages = 3x buff on same slot
// ============================================================================

using UnityEngine;
using Zenject;
using System.Collections.Generic;

public class BoneMageUnit : RuntimeUnit
{
    [Header("Bone Mage Settings")]
    [SerializeField] string projectilePrefabID = "bone_mage_skull";
    [SerializeField] GameObject projectilePrefab; // Fallback

    [Header("HP Buff Ability")]
    [SerializeField] bool enableHPBuff = true;
    [SerializeField] float level1BuffPercent = 0.025f; // 2.5%
    [SerializeField] float level2BuffPercent = 0.05f; // 5%
    [SerializeField] float level3BuffPercent = 0.075f; // 7.5%

    private bool hasAppliedBuff = false;

    // ✅ STATIC: Shared target slot for all Bone Mages in same team
    private static int playerTargetSlot = -1;
    private static int enemyTargetSlot = -1;
    private static bool playerSlotSelected = false;
    private static bool enemySlotSelected = false;

    // ===== BATTLE START - APPLY HP BUFF =====

    public override void StartBattle()
    {
        ResetBoneMageTargets();
        base.StartBattle();

        // Apply HP buff when battle starts
        if (enableHPBuff && !hasAppliedBuff)
        {
            hasAppliedBuff = true;
            ApplyHPBuffToRandomSlot();
        }
    }

    // ✅ Reset static variables when battle starts (call from BattleManager)
    public static void ResetBoneMageTargets()
    {
        playerTargetSlot = -1;
        enemyTargetSlot = -1;
        playerSlotSelected = false;
        enemySlotSelected = false;
        Debug.Log("🔄 Bone Mage targets reset");
    }

    private void ApplyHPBuffToRandomSlot()
    {
        GridManager gridManager = FindObjectOfType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogWarning("⚠️ GridManager not found, cannot apply Bone Mage buff");
            return;
        }

        // ✅ Determine target slot (first Bone Mage picks, others follow)
        int targetSlotIndex = -1;
        bool isFirstBoneMage = false;

        if (isPlayerUnit)
        {
            if (!playerSlotSelected)
            {
                // First player Bone Mage - pick random slot
                isFirstBoneMage = true;
                playerSlotSelected = true;

                // Get filled slots
                List<int> filledSlots = new List<int>();
                for (int i = 0; i < 9; i++)
                {
                    GridSlot slot = gridManager.GetPlayerSlot(i);
                    if (slot != null && !slot.IsEmpty)
                    {
                        filledSlots.Add(i);
                    }
                }

                if (filledSlots.Count == 0)
                {
                    Debug.LogWarning("⚠️ No filled slots found for Player Bone Mage buff");
                    return;
                }

                // Pick random filled slot
                targetSlotIndex = filledSlots[Random.Range(0, filledSlots.Count)];
                playerTargetSlot = targetSlotIndex;

                Debug.Log($"💀 First PLAYER Bone Mage selected slot {targetSlotIndex} for buffing");
            }
            else
            {
                // Use previously selected slot
                targetSlotIndex = playerTargetSlot;
            }
        }
        else // Enemy
        {
            if (!enemySlotSelected)
            {
                // First enemy Bone Mage - pick random slot
                isFirstBoneMage = true;
                enemySlotSelected = true;

                // Get filled slots
                List<int> filledSlots = new List<int>();
                for (int i = 0; i < 9; i++)
                {
                    GridSlot slot = gridManager.GetEnemySlot(i);
                    if (slot != null && !slot.IsEmpty)
                    {
                        filledSlots.Add(i);
                    }
                }

                if (filledSlots.Count == 0)
                {
                    Debug.LogWarning("⚠️ No filled slots found for Enemy Bone Mage buff");
                    return;
                }

                // Pick random filled slot
                targetSlotIndex = filledSlots[Random.Range(0, filledSlots.Count)];
                enemyTargetSlot = targetSlotIndex;

                Debug.Log($"💀 First ENEMY Bone Mage selected slot {targetSlotIndex} for buffing");
            }
            else
            {
                // Use previously selected slot
                targetSlotIndex = enemyTargetSlot;
            }
        }

        // ✅ Get target slot
        GridSlot targetSlot = isPlayerUnit ?
            gridManager.GetPlayerSlot(targetSlotIndex) :
            gridManager.GetEnemySlot(targetSlotIndex);

        if (targetSlot == null || targetSlot.units.Count == 0)
        {
            Debug.LogWarning($"⚠️ Target slot {targetSlotIndex} is invalid");
            return;
        }

        // Calculate buff amount
        float buffPercent = GetBuffPercentForLevel();

        // Apply buff to ALL units in the slot
        int buffedCount = 0;
        foreach (RuntimeUnit unit in targetSlot.units)
        {
            if (unit != null && unit.IsAlive())
            {
                ApplyHPBuffToUnit(unit, buffPercent);
                buffedCount++;
            }
        }

        string teamName = isPlayerUnit ? "PLAYER" : "ENEMY";
        int boneMageIndex = CountBoneMagesProcessedSoFar();
        Debug.Log($"💀 {teamName} Bone Mage #{boneMageIndex}: Buffed {buffedCount} units in slot {targetSlotIndex} with {buffPercent * 100:F1}% HP each");

        // VFX only on first Bone Mage (to avoid spam)
        if (isFirstBoneMage)
        {
            foreach (var item in targetSlot.units)
            {
                if (item != null)
                {
                    PlayBuffVFX(item.transform);
                }
            }
        }
    }

    private float GetBuffPercentForLevel()
    {
        switch (data.level)
        {
            case 1: return level1BuffPercent;
            case 2: return level2BuffPercent;
            case 3: return level3BuffPercent;
            default: return level1BuffPercent;
        }
    }

    private int CountBoneMagesProcessedSoFar()
    {
        BattleManager battleManager = FindObjectOfType<BattleManager>();
        if (battleManager == null) return 1;

        List<RuntimeUnit> teamUnits = isPlayerUnit ?
            battleManager.GetPlayerUnits() :
            battleManager.GetEnemyUnits();

        int count = 0;
        foreach (RuntimeUnit unit in teamUnits)
        {
            BoneMageUnit boneMage = unit as BoneMageUnit;
            if (boneMage != null && boneMage.hasAppliedBuff)
            {
                count++;
            }
        }

        return count;
    }

    private void ApplyHPBuffToUnit(RuntimeUnit unit, float buffPercent)
    {
        // Calculate bonus HP
        float bonusHP = unit.MaxHealth * buffPercent;

        // Increase max HP
        unit.IncreaseMaxHP(bonusHP);

        Debug.Log($"   ✅ {unit.data.toyName}: +{bonusHP:F0} HP ({buffPercent * 100:F1}% buff)");
    }

    private void PlayBuffVFX(Transform t)
    {
        if (poolingSystem != null)
        {
            string vfxName = isPlayerUnit ? "bone_mage_buff_vfx" : "bone_mage_buff_vfx_enemy";
            GameObject vfx = poolingSystem.InstantiateAPS(vfxName, t.position + Vector3.up * .8f);
            if (vfx != null)
            {
                vfx.transform.parent = t;
                container.InjectGameObject(vfx);
            }
        }

        if (audioManager != null)
        {
            audioManager.Play("bone_mage_buff");
        }

        Taptic.Light();
    }

    // ===== ATTACK - SPAWN HOMING SKULL =====

    public override void ExecuteAttackEvent()
    {
        if (currentTarget != null && currentTarget.IsAlive())
        {
            SpawnHomingSkull(currentTarget);
        }

        PlayAttackVFX();
        PlayAttackSFX();
        UnlockAttack();
    }

    private void SpawnHomingSkull(RuntimeUnit target)
    {
        Vector3 spawnPos = projectileSpawnPoint != null ?
            projectileSpawnPoint.position :
            transform.position + Vector3.up * 1.5f;

        GameObject projectileObj = null;

        if (poolingSystem != null)
        {
            projectileObj = poolingSystem.InstantiateAPS(projectilePrefabID, spawnPos);
        }

        if (projectileObj == null && projectilePrefab != null)
        {
            projectileObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            Debug.LogWarning($"⚠️ Pooling failed for {projectilePrefabID}, using direct instantiation");
        }

        if (projectileObj == null)
        {
            Debug.LogError($"❌ Failed to spawn projectile: {projectilePrefabID}");
            return;
        }

        GuidedProjectile projectile = projectileObj.GetComponent<GuidedProjectile>();
        if (projectile != null)
        {
            projectile.SetTarget(target, GetFinalDamage(), data.attackRange);
            Debug.Log($"💀 Bone Mage fired homing skull at {target.data.toyName}");
        }
        else
        {
            Debug.LogError($"❌ GuidedProjectile component not found on {projectilePrefabID}");
            Destroy(projectileObj);
        }
    }
}