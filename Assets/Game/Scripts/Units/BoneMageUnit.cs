// ============================================================================
// BONE MAGE UNIT - SUPPORT RANGED ATTACKER
// ✅ Spawns homing skull projectile
// ✅ Long range support (18 units)
// Stats: Damage=12, Range=18, Cooldown=2.2s, Speed=1.4
// Future: Shield buff support ability
// ============================================================================

using UnityEngine;
using Zenject;

public class BoneMageUnit : RuntimeUnit
{
    [Header("Bone Mage Settings")]
    [SerializeField] string projectilePrefabID = "bone_mage_skull";
    [SerializeField] GameObject projectilePrefab; // Fallback

    [Header("Shield Support (Future)")]
    [SerializeField] bool hasShieldAbility = false;
    [SerializeField] float shieldCooldown = 10f;

    private float lastShieldTime = -999f;

    // ===== OVERRIDE ANIMATION EVENT =====

    /// <summary>
    /// ✅ Called by Animation Event at the moment of attack
    /// </summary>
    public override void ExecuteAttackEvent()
    {
        // Spawn homing skull at exact animation frame
        if (currentTarget != null && currentTarget.IsAlive())
        {
            SpawnHomingSkull(currentTarget);
        }

        // Play attack VFX
        PlayAttackVFX();

        // Play attack SFX
        PlayAttackSFX();

        // Unlock for next attack
        UnlockAttack();
    }

    // ===== SPAWN HOMING SKULL =====

    private void SpawnHomingSkull(RuntimeUnit target)
    {
        // Get spawn position
        Vector3 spawnPos = projectileSpawnPoint != null ?
            projectileSpawnPoint.position :
            transform.position + Vector3.up * 1.5f;

        // Try to get projectile from pool
        GameObject projectileObj = null;

        if (poolingSystem != null)
        {
            projectileObj = poolingSystem.InstantiateAPS(projectilePrefabID, spawnPos);
        }

        // Fallback: Instantiate directly if pool fails
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

        // Setup projectile
        GuidedProjectile projectile = projectileObj.GetComponent<GuidedProjectile>();
        if (projectile != null)
        {
            projectile.SetTarget(target, GetFinalDamage());
            Debug.Log($"💀 Bone Mage fired homing skull at {target.data.toyName}");
        }
        else
        {
            Debug.LogError($"❌ GuidedProjectile component not found on {projectilePrefabID}");
            Destroy(projectileObj);
        }
    }

    // ===== SHIELD SUPPORT (Future Implementation) =====

    protected override void Update()
    {
        base.Update();

        // Check if shield ability is enabled
        if (hasShieldAbility && Time.time >= lastShieldTime + shieldCooldown)
        {
            TryApplyShieldBuff();
        }
    }

    private void TryApplyShieldBuff()
    {
        // TODO: Implement shield buff logic
        // Options:
        // 1. Shield self
        // 2. Shield nearest ally
        // 3. Shield all allies in range

        // Example: Shield self
        if (shieldAmount > 0)
        {
            this.shieldAmount += shieldAmount;
            lastShieldTime = Time.time;

            Debug.Log($"🛡️ Bone Mage applied {shieldAmount} shield to self");

            // Play shield VFX
            if (poolingSystem != null)
            {
                GameObject vfx = poolingSystem.InstantiateAPS("shield_buff_vfx", transform.position);
                if (vfx != null)
                {
                    poolingSystem.DestroyAPS(vfx, 2f);
                }
            }

            Taptic.Light();
        }
    }
}