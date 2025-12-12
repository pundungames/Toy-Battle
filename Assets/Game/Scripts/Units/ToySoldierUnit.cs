// ============================================================================
// TOY SOLDIER UNIT - RANGED HOMING ATTACKER
// ✅ Spawns guided projectile
// ✅ Basic ranged combat
// Stats: Damage=7, Range=10, Cooldown=0.7s, Speed=1.6
// ============================================================================

using UnityEngine;
using Zenject;

public class ToySoldierUnit : RuntimeUnit
{
    [Header("Toy Soldier Settings")]
    [SerializeField] string projectilePrefabID = "toy_soldier_bullet";
    [SerializeField] GameObject projectilePrefab; // Fallback if pooling fails

    // ===== OVERRIDE ANIMATION EVENT =====

    /// <summary>
    /// ✅ Called by Animation Event at the moment of attack
    /// This is when the projectile should spawn
    /// </summary>
    public override void ExecuteAttackEvent()
    {
        // Spawn homing projectile at exact animation frame
        if (currentTarget != null && currentTarget.IsAlive())
        {
            SpawnHomingProjectile(currentTarget);
        }

        // Play attack VFX
        PlayAttackVFX();

        // Play attack SFX
        PlayAttackSFX();

        // Unlock for next attack
        UnlockAttack();
    }

    // ===== SPAWN HOMING PROJECTILE =====

    private void SpawnHomingProjectile(RuntimeUnit target)
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
            Debug.Log($"🎯 Toy Soldier fired homing bullet at {target.data.toyName}");
        }
        else
        {
            Debug.LogError($"❌ GuidedProjectile component not found on {projectilePrefabID}");
            Destroy(projectileObj);
        }
    }
}