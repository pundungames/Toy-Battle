// ============================================================================
// GUIDED PROJECTILE - HOMING MISSILE
// ✅ Follows moving targets
// ✅ Smooth tracking with LookAt
// ✅ Auto-destroy on hit or timeout
// Used by: Toy Soldier, Bone Mage
// ============================================================================

using System.Collections;
using UnityEngine;
using Zenject;

public class GuidedProjectile : ProjectileBase
{
    [Header("Guided Settings")]
    [SerializeField] float projectileSpeed = 15f;
    [SerializeField] float rotationSpeed = 10f;
    [SerializeField] float maxLifetime = 5f; // Auto-destroy after 5s

    private RuntimeUnit targetUnit;
    private bool hasHit = false;
    private float lifetimeTimer = 0f;

    // ===== SET TARGET =====

    public void SetTarget(RuntimeUnit target, float damage)
    {
        targetUnit = target;
        attackDamage = damage;
        hasHit = false;
        lifetimeTimer = 0f;

        // Enable trail
        if (trail)
        {
            trail.enabled = false;
            Invoke("EnableTrail", 0.07f);
        }

        // Start tracking coroutine
        StartCoroutine(TrackTarget());
    }

    private void EnableTrail()
    {
        if (trail) trail.enabled = true;
    }

    // ===== TRACK TARGET =====

    private IEnumerator TrackTarget()
    {
        while (!hasHit && lifetimeTimer < maxLifetime)
        {
            lifetimeTimer += Time.deltaTime;

            // Check if target still exists and is alive
            if (targetUnit == null || !targetUnit.IsAlive())
            {
                // Target died, destroy projectile
                DestroyProjectile();
                yield break;
            }

            // Calculate direction to target
            Vector3 targetPosition = targetUnit.transform.position + Vector3.up * 1f; // Aim at center
            Vector3 direction = (targetPosition - transform.position).normalized;

            // Rotate towards target
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Move forward
            transform.position += transform.forward * projectileSpeed * Time.deltaTime;

            // Check distance to target
            float distance = Vector3.Distance(transform.position, targetPosition);
            if (distance < 0.5f)
            {
                // Close enough, trigger hit
                OnReachTarget();
                yield break;
            }

            yield return null;
        }

        // Timeout reached
        DestroyProjectile();
    }

    // ===== ON REACH TARGET =====

    private void OnReachTarget()
    {
        if (hasHit) return;
        hasHit = true;

        // Deal damage
        if (targetUnit != null && targetUnit.IsAlive())
        {
            targetUnit.TakeDamage(attackDamage);

            // Play hit VFX
            PlayHitVFX();

            // Play hit SFX
            PlayHitSFX();

            Taptic.Light();
        }

        DestroyProjectile();
    }

    // ===== VFX/SFX =====

    private void PlayHitVFX()
    {
        if (poolingSystem != null && !string.IsNullOrEmpty(hitVfxName))
        {
            GameObject vfx = poolingSystem.InstantiateAPS(hitVfxName, transform.position);
            if (vfx != null)
            {
                poolingSystem.DestroyAPS(vfx, 2f);
            }
        }
    }

    private void PlayHitSFX()
    {
        if (audioManager != null && !string.IsNullOrEmpty(hitSfxName))
        {
            audioManager.Play(hitSfxName);
        }
    }

    // ===== COLLISION (Backup) =====

    protected override void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        // Check if hit the target unit
        if (other.TryGetComponent<RuntimeUnit>(out RuntimeUnit unit))
        {
            if (unit == targetUnit)
            {
                OnReachTarget();
            }
        }
    }

    // ===== DESTROY =====

    private void DestroyProjectile()
    {
        StopAllCoroutines();

        if (poolingSystem != null)
        {
            poolingSystem.DestroyAPS(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ===== RESET ON DISABLE =====

    protected override void OnDisable()
    {
        base.OnDisable();
        hasHit = false;
        lifetimeTimer = 0f;
        targetUnit = null;
        StopAllCoroutines();
    }
}