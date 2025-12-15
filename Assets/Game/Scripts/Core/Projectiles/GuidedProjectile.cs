// ============================================================================
// GUIDED PROJECTILE - HOMING MISSILE
// ✅ Follows moving targets
// ✅ Smooth tracking with LookAt
// ✅ Auto-destroy on hit or timeout
// Used by: Toy Soldier, Bone Mage
// ============================================================================

using System.Collections;
using TMPro;
using UnityEngine;
using Zenject;

public class GuidedProjectile : ProjectileBase
{
    [Header("Guided Settings")]
    [SerializeField] float projectileSpeed = 15f;
    [SerializeField] float rotationSpeed = 15f; // ✅ Increased from 10 for straighter path
    [SerializeField] float maxLifetime = 5f; // Auto-destroy after 5s
    [SerializeField] bool useDirectShot = false; // ✅ NEW: Fly straight to target position (no tracking)

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

        // ✅ FIX: Face target immediately at spawn
        if (targetUnit != null)
        {
            Vector3 direction = (targetUnit.transform.position - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

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
        // ✅ If direct shot mode, calculate initial direction and fly straight
        Vector3 fixedDirection = Vector3.zero;
        if (useDirectShot && targetUnit != null)
        {
            Vector3 targetPos = targetUnit.transform.position + Vector3.up * 1.2f;
            fixedDirection = (targetPos - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(fixedDirection);
        }

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

            if (useDirectShot)
            {
                // ✅ DIRECT SHOT: Fly straight (no tracking)
                transform.position += fixedDirection * projectileSpeed * Time.deltaTime;
            }
            else
            {
                // ✅ HOMING: Track target continuously
                Vector3 targetPosition = targetUnit.transform.position + Vector3.up * 1.2f;
                Vector3 direction = (targetPosition - transform.position).normalized;

                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

                transform.position += transform.forward * projectileSpeed * Time.deltaTime;
            }

            // Check distance to target
            float distance = Vector3.Distance(transform.position, targetUnit.transform.position + Vector3.up * 1.2f);
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