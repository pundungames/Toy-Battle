// ============================================================================
// GUARDIAN GOLEM UNIT - DASH MELEE TANK
// ✅ Dash attack with DOTween
// ✅ Knockback on hit
// ✅ Animation event support
// Stats: Damage=20, Range=2, Cooldown=2.0s, Speed=1.8, HP=120
// ============================================================================

using UnityEngine;
using DG.Tweening;
using Zenject;
using UnityEngine.AI;

public class GuardianGolemUnit : RuntimeUnit
{
    [Header("Dash Attack Settings")]
    [SerializeField] float dashSpeed = 10f;
    [SerializeField] float dashDuration = 0.3f;
    [SerializeField] float knockbackForce = 3f;
    [SerializeField] float knockbackDuration = 0.2f;
    [SerializeField] float attackAnimationDuration = 1.5f; // ✅ Total attack animation length

    [Header("Animation Events")]
    [SerializeField] string dashEventName = "DashToTarget"; // Animation event name

    private bool isDashing = false;

    // ===== OVERRIDE EXECUTE ATTACK =====

    protected override void ExecuteAttack(RuntimeUnit target)
    {
        // ✅ Check dash state
        if (isDashing) return;

        // Trigger attack animation (base class handles lock)
        if (animator != null)
        {
            animator.SetTrigger("Attack");
            LockAttack();

            // ✅ Animation event will call ExecuteAttackEvent() → DashToTarget()
            // After animation completes, unlock
            Invoke(nameof(UnlockAttackDelayed), attackAnimationDuration);
        }
        else
        {
            // No animator, execute immediately
            ExecuteAttackEvent();
        }
    }

    // ===== ANIMATION EVENT: EXECUTE ATTACK =====

    /// <summary>
    /// ✅ Called by Animation Event at dash moment
    /// Alternative name for compatibility: DashToTarget()
    /// </summary>
    public override void ExecuteAttackEvent()
    {
        DashToTarget();
    }

    private void UnlockAttackDelayed()
    {
        UnlockAttack();
        Debug.Log("🔓 Guardian Golem attack unlocked");
    }

    // ===== ANIMATION EVENT: DASH TO TARGET =====

    /// <summary>
    /// Called by Animation Event OR ExecuteAttackEvent()
    /// </summary>
    public void DashToTarget()
    {
        if (currentTarget == null || !currentTarget.IsAlive())
        {
            isDashing = false;
            return;
        }

        isDashing = true;

        // Calculate dash position (slightly in front of target)
        Vector3 dashDirection = (currentTarget.transform.position - transform.position).normalized;
        Vector3 dashTarget = currentTarget.transform.position - dashDirection * 1f;

        // Store original position for potential retreat
        Vector3 originalPos = transform.position;

        // Disable NavMeshAgent during dash
        if (agent != null && agent.enabled)
        {
            agent.enabled = false;
        }

        // Dash with DOTween
        transform.DOMove(dashTarget, dashDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                // Deal damage on arrival
                if (currentTarget != null && currentTarget.IsAlive())
                {
                    DealDashDamage(currentTarget);
                }

                // Re-enable NavMeshAgent
                if (agent != null && !agent.enabled)
                {
                    agent.enabled = true;
                }

                isDashing = false;
            });

        // Play dash VFX
        PlayDashVFX();

        // Play dash SFX
        PlayDashSFX();

        Taptic.Medium();
    }

    // ===== DEAL DASH DAMAGE =====

    private void DealDashDamage(RuntimeUnit target)
    {
        if (target == null || !target.IsAlive()) return;

        // Check first attack cancel
        if (target.hasFirstAttackCancel)
        {
            target.hasFirstAttackCancel = false;
            Debug.Log($"⚔️ {target.data.toyName} blocked Guardian Golem's dash!");
            return;
        }

        // Deal damage
        target.TakeDamage(GetFinalDamage());

        // Apply knockback
        ApplyKnockback(target);

        // Play hit VFX
        PlayHitVFX(target.transform.position);

        // Play hit SFX
        if (audioManager != null)
        {
            audioManager.Play("golem_hit");
        }

        Taptic.Heavy();
    }

    // ===== KNOCKBACK =====

    private void ApplyKnockback(RuntimeUnit target)
    {
        // Calculate knockback direction (away from golem)
        Vector3 knockbackDirection = (target.transform.position - transform.position).normalized;
        Vector3 knockbackTarget = target.transform.position + knockbackDirection * knockbackForce;

        // ✅ CRITICAL: Check if knockback position is on NavMesh
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(knockbackTarget, out hit, knockbackForce + 1f, UnityEngine.AI.NavMesh.AllAreas))
        {
            // Valid NavMesh position found, use it
            knockbackTarget = hit.position;
        }
        else
        {
            // No valid NavMesh, don't knockback (just damage)
            Debug.LogWarning($"⚠️ Knockback position outside NavMesh, skipping knockback for {target.data.toyName}");
            return;
        }

        // Disable target's NavMeshAgent during knockback
        NavMeshAgent targetAgent = target.GetComponent<NavMeshAgent>();
        bool wasAgentEnabled = false;

        if (targetAgent != null && targetAgent.enabled)
        {
            wasAgentEnabled = true;
            targetAgent.enabled = false;
        }

        // Apply knockback with DOTween
        target.transform.DOMove(knockbackTarget, knockbackDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                // Re-enable agent
                if (targetAgent != null && wasAgentEnabled && !targetAgent.enabled)
                {
                    targetAgent.enabled = true;
                }
            });

        Debug.Log($"💥 Guardian Golem knocked back {target.data.toyName} to {knockbackTarget}");
    }

    // ===== VFX/SFX HELPERS =====

    private void PlayDashVFX()
    {
        if (poolingSystem != null)
        {
            GameObject vfx = poolingSystem.InstantiateAPS("golem_dash_vfx", transform.position);
            if (vfx != null)
            {
                // Follow golem during dash
                vfx.transform.SetParent(transform);
                poolingSystem.DestroyAPS(vfx, dashDuration + 0.5f);
            }
        }
    }

    private void PlayDashSFX()
    {
        if (audioManager != null)
        {
            audioManager.Play("golem_dash");
        }
    }

    // ===== RESET ON DEATH =====

    private void OnDestroy()
    {
        // Kill any active tweens
        transform.DOKill();
        isDashing = false;

        // ✅ Cancel pending invokes
        CancelInvoke(nameof(UnlockAttackDelayed));
    }
}