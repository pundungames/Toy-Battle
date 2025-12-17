// ============================================================================
// GUARDIAN GOLEM - FIXED TARGET DEATH HANDLING
// ✅ Checks target alive before AND during dash
// ✅ Cancels dash if target dies mid-animation
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
    [SerializeField] float attackAnimationDuration = 1.5f;

    [Header("Animation Events")]
    [SerializeField] string dashEventName = "DashToTarget";

    private bool isDashing = false;
    private RuntimeUnit dashTarget; // ✅ Store dash target separately
    private Tween currentDashTween; // ✅ Track dash tween

    protected override void ExecuteAttack(RuntimeUnit target)
    {
        if (isDashing) return;

        // ✅ Store target at attack start
        dashTarget = target;

        if (animator != null)
        {
            animator.SetTrigger("Attack");
            LockAttack();
            Invoke(nameof(UnlockAttackDelayed), attackAnimationDuration);
        }
        else
        {
            ExecuteAttackEvent();
        }
    }

    public override void ExecuteAttackEvent()
    {
        DashToTarget();
    }

    private void UnlockAttackDelayed()
    {
        UnlockAttack();
    }

    public void DashToTarget()
    {
        // ✅ Check if stored dash target is still valid
        if (dashTarget == null || !dashTarget.IsAlive())
        {
            Debug.Log($"💀 Guardian Golem: Dash target died, canceling dash");
            isDashing = false;
            return;
        }

        isDashing = true;

        Vector3 dashDirection = (dashTarget.transform.position - transform.position).normalized;
        Vector3 dashTargetPos = dashTarget.transform.position - dashDirection * 1f;

        if (agent != null && agent.enabled)
        {
            agent.enabled = false;
        }

        // ✅ Store tween reference
        currentDashTween = transform.DOMove(dashTargetPos, dashDuration)
            .SetEase(Ease.OutQuad)
            .OnUpdate(() =>
            {
                // ✅ Check every frame if target died during dash
                if (dashTarget == null || !dashTarget.IsAlive())
                {
                    Debug.Log($"💀 Guardian Golem: Target died during dash, stopping");

                    // Stop dash immediately
                    if (currentDashTween != null)
                    {
                        currentDashTween.Kill();
                    }

                    // Re-enable agent
                    if (agent != null && !agent.enabled)
                    {
                        agent.enabled = true;
                    }

                    isDashing = false;
                }
            })
            .OnComplete(() =>
            {
                // ✅ Final check before damage
                if (dashTarget != null && dashTarget.IsAlive())
                {
                    DealDashDamage(dashTarget);
                }
                else
                {
                    Debug.Log($"💀 Guardian Golem: Target died before damage");
                }

                if (agent != null && !agent.enabled)
                {
                    agent.enabled = true;
                }

                isDashing = false;
                dashTarget = null; // ✅ Clear reference
            });

        PlayDashVFX();
        PlayDashSFX();
        Taptic.Medium();
    }

    private void DealDashDamage(RuntimeUnit target)
    {
        if (target == null || !target.IsAlive()) return;

        if (target.hasFirstAttackCancel)
        {
            target.hasFirstAttackCancel = false;
            Debug.Log($"⚔️ {target.data.toyName} blocked Guardian Golem's dash!");
            return;
        }

        target.TakeDamage(GetFinalDamage());
        ApplyKnockback(target);
        PlayHitVFX(target.transform.position);

        if (audioManager != null)
        {
            audioManager.Play("golem_hit");
        }

        Taptic.Heavy();
    }

    private void ApplyKnockback(RuntimeUnit target)
    {
        Vector3 knockbackDirection = (target.transform.position - transform.position).normalized;
        Vector3 knockbackTarget = target.transform.position + knockbackDirection * knockbackForce;

        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(knockbackTarget, out hit, knockbackForce + 1f, UnityEngine.AI.NavMesh.AllAreas))
        {
            knockbackTarget = hit.position;
        }
        else
        {
            Debug.LogWarning($"⚠️ Knockback position outside NavMesh, skipping knockback for {target.data.toyName}");
            return;
        }

        NavMeshAgent targetAgent = target.GetComponent<NavMeshAgent>();
        bool wasAgentEnabled = false;

        if (targetAgent != null && targetAgent.enabled)
        {
            wasAgentEnabled = true;
            targetAgent.enabled = false;
        }

        target.transform.DOMove(knockbackTarget, knockbackDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                if (targetAgent != null && wasAgentEnabled && !targetAgent.enabled)
                {
                    targetAgent.enabled = true;
                }
            });
    }

    private void PlayDashVFX()
    {
        if (poolingSystem != null)
        {
            GameObject vfx = poolingSystem.InstantiateAPS("golem_dash_vfx", transform.position);
            if (vfx != null)
            {
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

    // ✅ Clean up on death
    private void OnDestroy()
    {
        // Kill dash tween
        if (currentDashTween != null)
        {
            currentDashTween.Kill();
        }

        transform.DOKill();
        isDashing = false;
        dashTarget = null;

        CancelInvoke(nameof(UnlockAttackDelayed));
    }
}