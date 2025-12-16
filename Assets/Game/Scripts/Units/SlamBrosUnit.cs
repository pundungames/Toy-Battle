// ============================================================================
// SLAM BROS UNIT - JUMP ATTACK FIGHTER (COROUTINE VERSION)
// ✅ Manual position tracking with coroutine (no ChangeEndValue)
// ✅ Compatible with all DOTween versions
// ✅ Knockback on landing
// ============================================================================

using UnityEngine;
using DG.Tweening;
using System.Collections;
using Zenject;

public class SlamBrosUnit : RuntimeUnit
{
    [Header("Jump Attack Settings")]
    [SerializeField] float jumpHeight = 3f;
    [SerializeField] float jumpSpeed = 8f;
    [SerializeField] float attackAnimationDuration = 0.5f;
    [SerializeField] bool applyKnockback = true;
    [SerializeField] float knockbackForce = 0.5f;
    [SerializeField] float knockbackDuration = 0.3f;
    [SerializeField] float trackingUpdateRate = 0.05f; // Update every 0.05s

    private RuntimeUnit jumpTarget;
    private bool isJumping = false;
    private Coroutine trackingCoroutine;

    protected override void ExecuteAttack(RuntimeUnit target)
    {
        if (isJumping) return;
        StartJumpAttack(target);
    }

    private void StartJumpAttack(RuntimeUnit target)
    {
        if (target == null || !target.IsAlive()) return;

        jumpTarget = target;
        isJumping = true;
        LockAttack();

        if (agent != null && agent.enabled)
        {
            agent.enabled = false;
        }

        if (animator != null)
        {
            animator.SetTrigger("Jump");
        }
    }

    // ===== ANIMATION EVENT: JUMP =====

    public void JumpEvent()
    {
        if (jumpTarget == null || !jumpTarget.IsAlive())
        {
            UnlockJump();
            return;
        }

        float distance = Vector3.Distance(transform.position, jumpTarget.transform.position);
        float jumpDuration = distance / jumpSpeed;

        // Start tracking coroutine
        trackingCoroutine = StartCoroutine(JumpWithTracking(jumpDuration));
    }

    // ===== JUMP WITH TRACKING COROUTINE =====

    private IEnumerator JumpWithTracking(float duration)
    {
        Vector3 startPos = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            // Check if target still alive
            if (jumpTarget == null || !jumpTarget.IsAlive())
            {
                // Target died, land at current position
                break;
            }

            // Get current target position
            Vector3 targetPos = jumpTarget.transform.position;

            // Validate NavMesh
            UnityEngine.AI.NavMeshHit hit;
            if (!UnityEngine.AI.NavMesh.SamplePosition(targetPos, out hit, 3f, UnityEngine.AI.NavMesh.AllAreas))
            {
                // Invalid position, use last valid position
                targetPos = transform.position + (targetPos - transform.position).normalized * 2f;
            }
            else
            {
                targetPos = hit.position;
            }

            // Calculate position with arc
            Vector3 horizontalPos = Vector3.Lerp(startPos, targetPos, progress);

            // Parabolic arc for height
            float heightProgress = 1 - Mathf.Pow(2 * progress - 1, 2); // Parabola
            float currentHeight = jumpHeight * heightProgress;

            // Apply position
            transform.position = new Vector3(
                horizontalPos.x,
                horizontalPos.y + currentHeight,
                horizontalPos.z
            );

            // Look at target
            Vector3 lookDirection = (targetPos - transform.position).normalized;
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }

            yield return null; // Update every frame for smooth tracking
        }

        // Jump complete
        OnJumpComplete();
    }

    private void OnJumpComplete()
    {
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        Vector3 finalLandingPos = transform.position;

        DealLandingDamage(finalLandingPos);
        finalLandingPos.y = 0;
        PlayLandingVFX(finalLandingPos);

        if (audioManager != null)
        {
            audioManager.Play("slam_bros_land");
        }

        Taptic.Heavy();

        if (agent != null && !agent.enabled)
        {
            agent.enabled = true;
        }

        Invoke(nameof(UnlockJump), attackAnimationDuration);
    }

    private void UnlockJump()
    {
        isJumping = false;
        UnlockAttack();
        jumpTarget = null;

        if (trackingCoroutine != null)
        {
            StopCoroutine(trackingCoroutine);
            trackingCoroutine = null;
        }
    }

    // ===== LANDING DAMAGE =====

    private void DealLandingDamage(Vector3 landingPos)
    {
        if (currentTarget != null && currentTarget.IsAlive())
        {
            float distance = Vector3.Distance(landingPos, currentTarget.transform.position);

            if (distance <= attackRange)
            {
                DealInstantDamage(currentTarget);

                if (applyKnockback)
                {
                    ApplyLandingKnockback(currentTarget, landingPos);
                }
            }
        }
    }

    // ===== KNOCKBACK =====

    private void ApplyLandingKnockback(RuntimeUnit target, Vector3 impactPosition)
    {
        Vector3 knockbackDirection = (target.transform.position - impactPosition).normalized;
        Vector3 knockbackTarget = target.transform.position + knockbackDirection * knockbackForce;

        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(knockbackTarget, out hit, knockbackForce + 1f, UnityEngine.AI.NavMesh.AllAreas))
        {
            knockbackTarget = hit.position;
        }
        else
        {
            Debug.LogWarning($"⚠️ Slam Bros: Knockback position outside NavMesh, skipping knockback for {target.data.toyName}");
            return;
        }

        UnityEngine.AI.NavMeshAgent targetAgent = target.GetComponent<UnityEngine.AI.NavMeshAgent>();
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

        Debug.Log($"💥 Slam Bros knocked back {target.data.toyName} to {knockbackTarget}");
    }

    // ===== VFX =====

    private void PlayLandingVFX(Vector3 position)
    {
        if (poolingSystem != null)
        {
            GameObject vfx = poolingSystem.InstantiateAPS("slam_bros_land_vfx", position);
            vfx.transform.rotation = Quaternion.Euler(-90, 0, 0);
            if (vfx != null)
            {
                poolingSystem.DestroyAPS(vfx, 2f);
            }
        }
    }

    public override void ExecuteAttackEvent()
    {
        if (currentTarget != null && currentTarget.IsAlive())
        {
            DealInstantDamage(currentTarget);
        }
    }

    private void OnDestroy()
    {
        transform.DOKill();
        isJumping = false;
        jumpTarget = null;

        if (trackingCoroutine != null)
        {
            StopCoroutine(trackingCoroutine);
        }

        CancelInvoke(nameof(UnlockJump));
    }
}