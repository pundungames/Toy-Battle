// ============================================================================
// BLAST RACER UNIT - KAMIKAZE ATTACKER
// ✅ Rushes to target
// ✅ Explodes on arrival (0.5-1s delay)
// ✅ AOE damage in attack range
// ✅ Knockback (optional)
// Stats: Damage=80, Range=2.5, Speed=3.2, HP=20
// ============================================================================

using UnityEngine;
using DG.Tweening;
using System.Collections;
using Zenject;

public class BlastRacerUnit : RuntimeUnit
{
    [Header("Kamikaze Settings")]
    [SerializeField] float explosionDelay = 0.75f; // Delay after reaching range
    [SerializeField] float explosionRadius = 2.5f; // AOE range
    [SerializeField] string explosionVFX = "blast_racer_explosion";
    [SerializeField] string explosionSFX = "blast_racer_boom";
    [SerializeField] bool applyKnockback = true;
    [SerializeField] float knockbackForce = 2f;

    private bool hasExploded = false;
    private bool isPreparingExplosion = false;

    // ===== OVERRIDE ATTACK =====

    protected override void ExecuteAttack(RuntimeUnit target)
    {
        // Don't attack if already exploded or preparing
        if (hasExploded || isPreparingExplosion) return;

        // Start explosion sequence
        StartExplosionSequence();
    }

    // ===== EXPLOSION SEQUENCE =====

    private void StartExplosionSequence()
    {
        isPreparingExplosion = true;
        LockAttack();

        Debug.Log($"💣 Blast Racer preparing to explode in {explosionDelay}s...");

        // Play warning animation/VFX
        PlayWarningEffects();

        // Wait for explosion delay
        StartCoroutine(ExplosionCountdown());
    }

    private IEnumerator ExplosionCountdown()
    {
        yield return new WaitForSeconds(explosionDelay);

        // BOOM!
        Explode();
    }

    // ===== WARNING EFFECTS =====

    private void PlayWarningEffects()
    {
        // Flashing red effect
        StartCoroutine(FlashEffect());

        // Warning SFX
        if (audioManager != null)
        {
            audioManager.Play("blast_racer_warning");
        }

        // Stop moving
        if (agent != null && agent.enabled)
        {
            agent.ResetPath();
        }
    }

    private IEnumerator FlashEffect()
    {
        float flashDuration = explosionDelay;
        float flashSpeed = 5f;
        float timer = 0f;

        Vector3 originalScale = transform.localScale;

        while (timer < flashDuration)
        {
            float scale = 1f + Mathf.Sin(timer * flashSpeed) * 0.2f;
            transform.localScale = originalScale * scale;

            timer += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
    }

    // ===== EXPLODE =====

    private void Explode()
    {
        hasExploded = true;

        Debug.Log($"💥 Blast Racer EXPLODED!");

        // Find all enemies in explosion radius
        BattleManager battleManager = FindObjectOfType<BattleManager>();
        if (battleManager != null)
        {
            var enemies = isPlayerUnit ?
                battleManager.GetEnemyUnits() :
                battleManager.GetPlayerUnits();

            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive()) continue;

                float distance = Vector3.Distance(transform.position, enemy.transform.position);

                if (distance <= explosionRadius)
                {
                    // Deal damage
                    enemy.TakeDamage(GetFinalDamage());

                    // Apply knockback
                    if (applyKnockback)
                    {
                        ApplyExplosionKnockback(enemy);
                    }

                    Debug.Log($"💥 Blast Racer damaged {enemy.data.toyName} ({distance:F1}m away)");
                }
            }
        }

        // Play explosion VFX
        if (poolingSystem != null && !string.IsNullOrEmpty(explosionVFX))
        {
            GameObject vfx = poolingSystem.InstantiateAPS(explosionVFX, transform.position);
            if (vfx != null)
            {
                vfx.transform.localScale = Vector3.one * explosionRadius;
                poolingSystem.DestroyAPS(vfx, 3f);
            }
        }

        // Play explosion SFX
        if (audioManager != null && !string.IsNullOrEmpty(explosionSFX))
        {
            audioManager.Play(explosionSFX);
        }

        Taptic.Heavy();

        // Camera shake (optional)
        // EventManager.OnCamShake(5, isPlayerUnit);

        // Destroy self
        Destroy(gameObject, 0.1f);
    }

    // ===== KNOCKBACK =====

    private void ApplyExplosionKnockback(RuntimeUnit target)
    {
        // Calculate knockback direction (away from explosion)
        Vector3 knockbackDirection = (target.transform.position - transform.position).normalized;
        Vector3 knockbackTarget = target.transform.position + knockbackDirection * knockbackForce;

        // ✅ CRITICAL: Check if knockback position is on NavMesh
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(knockbackTarget, out hit, knockbackForce + 1f, UnityEngine.AI.NavMesh.AllAreas))
        {
            knockbackTarget = hit.position;
        }
        else
        {
            // No valid NavMesh, skip knockback
            return;
        }

        // Disable agent during knockback
        var targetAgent = target.GetComponent<UnityEngine.AI.NavMeshAgent>();
        bool wasEnabled = false;

        if (targetAgent != null && targetAgent.enabled)
        {
            wasEnabled = true;
            targetAgent.enabled = false;
        }

        // Apply knockback
        target.transform.DOMove(knockbackTarget, 0.3f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                if (targetAgent != null && wasEnabled && !targetAgent.enabled)
                {
                    targetAgent.enabled = true;
                }
            });
    }

    // ===== GIZMOS =====

    private void OnDrawGizmosSelected()
    {
        // Draw explosion radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }

    // ===== RESET ON DEATH =====

    private void OnDestroy()
    {
        StopAllCoroutines();
        transform.DOKill();
    }
}