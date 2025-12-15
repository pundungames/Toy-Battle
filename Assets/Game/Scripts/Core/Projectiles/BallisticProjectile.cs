// ============================================================================
// BALLISTIC PROJECTILE - NON-GUIDED MISSILE
// ✅ Flies to target position (snapshot, no tracking!)
// ✅ AOE explosion on impact
// ✅ Auto-destroy on impact or timeout
// Used by: Kaboom Tanklet
// ============================================================================

using System.Collections;
using UnityEngine;
using DG.Tweening;
using Zenject;

public class BallisticProjectile : ProjectileBase
{
    [Header("Ballistic Settings")]
    [SerializeField] float arcHeight = 5f; // Height of ballistic arc
    [SerializeField] float maxLifetime = 10f; // Auto-destroy after 10s

    [Header("AOE Settings")]
    [SerializeField] float fullDamageRadius = 1f; // 100% damage
    [SerializeField] float partialDamageRadius = 2f; // 60% damage
    [SerializeField] float partialDamageMultiplier = 0.6f;

    [Header("VFX/SFX")]
    [SerializeField] ParticleSystem explosionVFX;
    [SerializeField] protected string explosionSFX = "kaboom_boom";

    private Vector3 targetPosition;
    private float impactDamage;
    private bool hasExploded = false;
    private bool isPlayerProjectile;
    private Tween flightTween;

    // ===== SET TARGET (Non-tracking!) =====

    public void SetTarget(Vector3 targetPos, float damage, float flightTime, bool isPlayer)
    {
        targetPosition = targetPos;
        impactDamage = damage;
        isPlayerProjectile = isPlayer;
        hasExploded = false;

        // Look at target
        Vector3 direction = (targetPosition - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // Enable trail
        if (trail)
        {
            trail.enabled = false;
            Invoke("EnableTrail", 0.07f);
        }

        // Start ballistic flight
        StartBallisticFlight(flightTime);
    }

    private void EnableTrail()
    {
        if (trail) trail.enabled = true;
    }

    // ===== BALLISTIC FLIGHT =====

    private void StartBallisticFlight(float flightTime)
    {
        // ✅ DOTween's DOJump for perfect ballistic arc
        flightTween = transform.DOJump(targetPosition, arcHeight, 1, flightTime)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                // Impact!
                OnImpact();
            });

        // Rotate during flight (optional cool effect)
        transform.DORotate(new Vector3(0, 0, 360), flightTime, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear);

        // Auto-destroy after max lifetime
        Invoke(nameof(ForceDestroy), maxLifetime);
    }

    // ===== ON IMPACT =====

    private void OnImpact()
    {
        if (hasExploded) return;
        hasExploded = true;

        CancelInvoke(nameof(ForceDestroy));

        Debug.Log($"💥 Ballistic projectile impacted at {transform.position}");

        // AOE Explosion
        ExplodeAtPosition(transform.position);

        // Destroy projectile
        DestroyProjectile();
    }

    // ===== AOE EXPLOSION =====

    private void ExplodeAtPosition(Vector3 explosionCenter)
    {
        // Find all enemies in range
        BattleManager battleManager = FindObjectOfType<BattleManager>();
        if (battleManager == null) return;

        var enemies = isPlayerProjectile ?
            battleManager.GetEnemyUnits() :
            battleManager.GetPlayerUnits();

        // Damage all enemies in AOE
        foreach (var enemy in enemies)
        {
            if (enemy == null || !enemy.IsAlive()) continue;

            float distance = Vector3.Distance(explosionCenter, enemy.transform.position);

            float damage = 0f;

            // Full damage in inner radius
            if (distance <= fullDamageRadius)
            {
                damage = impactDamage;
                Debug.Log($"💥 Full damage ({damage}) to {enemy.data.toyName} (distance: {distance:F1}m)");
            }
            // Partial damage in outer radius
            else if (distance <= partialDamageRadius)
            {
                damage = impactDamage * partialDamageMultiplier;
                Debug.Log($"💥 Partial damage ({damage}) to {enemy.data.toyName} (distance: {distance:F1}m)");
            }

            if (damage > 0)
            {
                enemy.TakeDamage(damage);
            }
        }

        // Play explosion VFX
        PlayExplosionVFX(explosionCenter);

        // Play explosion SFX
        PlayExplosionSFX();

        Taptic.Heavy();
    }

    // ===== VFX/SFX =====

    private void PlayExplosionVFX(Vector3 position)
    {
        explosionVFX.transform.localScale = Vector3.one * partialDamageRadius;
        explosionVFX.Play();
    }

    private void PlayExplosionSFX()
    {
        if (audioManager != null && !string.IsNullOrEmpty(explosionSFX))
        {
            audioManager.Play(explosionSFX);
        }
    }

    // ===== COLLISION (Backup - in case hits something early) =====

    protected override void OnTriggerEnter(Collider other)
    {
        if (hasExploded) return;

        // Check if hit an enemy
        if (other.TryGetComponent<RuntimeUnit>(out RuntimeUnit unit))
        {
            // Explode early
            OnImpact();
        }
    }

    // ===== DESTROY =====

    private void ForceDestroy()
    {
        // Timeout - destroy without explosion
        if (!hasExploded)
        {
            Debug.LogWarning("⚠️ Ballistic projectile timeout - destroying");
            DestroyProjectile();
        }
    }

    private void DestroyProjectile()
    {
        // Kill tweens
        if (flightTween != null)
        {
            flightTween.Kill();
        }
        transform.DOKill();

        CancelInvoke();

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
        hasExploded = false;
        targetPosition = Vector3.zero;

        if (flightTween != null)
        {
            flightTween.Kill();
            flightTween = null;
        }

        transform.DOKill();
        CancelInvoke();
    }
}