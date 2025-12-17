// ============================================================================
// KABOOM TANKLET UNIT - ARTILLERY BOMBER
// ✅ Fires non-guided ballistic projectile
// ✅ AOE explosion on impact
// ✅ 100% damage in 1m radius, 60% damage in 2m radius
// Stats: Damage=50, Range=25, Cooldown=4s, Speed=1.3, HP=80
// ============================================================================

using UnityEngine;
using DG.Tweening;
using Zenject;

public class KaboomTankletUnit : RuntimeUnit
{
    [Header("Ballistic Settings")]
    [SerializeField] string projectilePrefabID = "kaboom_tanklet_missile";
    [SerializeField] GameObject projectilePrefab; // Fallback
    [SerializeField] float projectileSpeed = 20f;

    [Header("Barrel Rotation")]
    [SerializeField] Transform barrelTransform; // Namlu transform'u
    [SerializeField] float barrelDownAngle = -30f; // Aşağı açı (başlangıç)
    [SerializeField] float barrelUpAngle = 30f; // Yukarı açı (ateş pozisyonu)
    [SerializeField] bool rotateBarrelDuringCooldown = true;

    // ===== OVERRIDE UPDATE =====

    protected override void Update()
    {
        base.Update();

        // ✅ Rotate barrel based on cooldown progress
        if (rotateBarrelDuringCooldown && barrelTransform != null)
        {
            UpdateBarrelRotation();
        }
    }

    // ===== BARREL ROTATION =====

    private void UpdateBarrelRotation()
    {
        // Calculate cooldown progress (0 = just attacked, 1 = ready to attack)
        float timeSinceLastAttack = Time.time - lastAttackTime;
        float cooldownProgress = Mathf.Clamp01(timeSinceLastAttack / attackCooldown);

        // Interpolate angle (down → up during cooldown)
        float currentAngle = Mathf.Lerp(barrelDownAngle, barrelUpAngle, cooldownProgress);

        // Apply rotation (local X axis for barrel elevation)
        barrelTransform.localRotation = Quaternion.Euler(currentAngle, 0f, 0f);
    }

    // ===== OVERRIDE ANIMATION EVENT =====

    public override void ExecuteAttackEvent()
    {
        // Fire ballistic missile at exact animation frame
        if (currentTarget != null && currentTarget.IsAlive())
        {
            FireBallisticMissile(currentTarget);
        }

        // ✅ Reset barrel to down position after firing
        if (barrelTransform != null)
        {
            barrelTransform.DOLocalRotate(new Vector3(barrelDownAngle, 0f, 0f), 0.3f)
                .SetEase(Ease.OutQuad);
        }

        PlayAttackVFX();
        PlayAttackSFX();
        UnlockAttack();
    }

    // ===== FIRE BALLISTIC MISSILE =====

    private void FireBallisticMissile(RuntimeUnit target)
    {
        // Get spawn point
        Transform spawnPoint = projectileSpawnPoint != null ? projectileSpawnPoint : transform;
        Vector3 spawnPos = spawnPoint.position;

        // Get target position (snapshot - not tracking!)
        Vector3 targetPos = target.transform.position + Vector3.up * 1f;

        // Create projectile
        GameObject projectileObj = null;

        if (poolingSystem != null && !string.IsNullOrEmpty(projectilePrefabID))
        {
            projectileObj = poolingSystem.InstantiateAPS(projectilePrefabID, spawnPos);
        }
        else if (projectilePrefab != null)
        {
            projectileObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        }

        if (projectileObj == null)
        {
            Debug.LogWarning("⚠️ Kaboom Tanklet: Failed to spawn projectile!");
            return;
        }

        // Get BallisticProjectile component
        BallisticProjectile projectile = projectileObj.GetComponent<BallisticProjectile>();

        if (projectile == null)
        {
            Debug.LogWarning("⚠️ Kaboom Tanklet: Projectile missing BallisticProjectile component!");
            Destroy(projectileObj);
            return;
        }
        container.InjectGameObject(projectile.gameObject);
        // Calculate flight time
        float distance = Vector3.Distance(spawnPos, targetPos);
        float flightTime = distance / projectileSpeed;

        // ✅ Set target (BallisticProjectile handles the rest!)
        projectile.SetTarget(targetPos, GetFinalDamage(), flightTime, isPlayerUnit);

        Debug.Log($"🚀 Kaboom Tanklet fired missile at {target.data.toyName}");
    }

    // Note: AOE explosion is now handled by BallisticProjectile!

    // ===== RESET ON DEATH =====

    private void OnDestroy()
    {
        // Cleanup if needed
    }
}