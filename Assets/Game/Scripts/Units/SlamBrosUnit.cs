// ============================================================================
// SLAM BROS - FIXED TARGET DEATH HANDLING
// ✅ Checks target alive during entire jump
// ✅ Lands at last valid position if target dies
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

    private RuntimeUnit jumpTarget; // ✅ Stored jump target
    private bool isJumping = false;
    private Coroutine trackingCoroutine;
    private Vector3 lastValidTargetPosition; // ✅ Store last valid position

    protected override void ExecuteAttack(RuntimeUnit target)
    {
        if (isJumping) return;

        // ✅ Store target at attack start
        jumpTarget = target;
        lastValidTargetPosition = target.transform.position;

        StartJumpAttack(target);
    }

    private void StartJumpAttack(RuntimeUnit target)
    {
        if (target == null || !target.IsAlive()) return;

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

    public void JumpEvent()
    {
        // ✅ Check if target still valid
        if (jumpTarget == null || !jumpTarget.IsAlive())
        {
            Debug.Log($"💀 Slam Bros: Jump target died before jump started, canceling");
            UnlockJump();
            return;
        }

        float distance = Vector3.Distance(transform.position, jumpTarget.transform.position);
        float jumpDuration = distance / jumpSpeed;

        trackingCoroutine = StartCoroutine(JumpWithTracking(jumpDuration));
    }

    private IEnumerator JumpWithTracking(float duration)
    {
        Vector3 startPos = transform.position;
        float elapsedTime = 0f;
        bool targetDiedDuringJump = false;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            // ✅ Check if target still alive
            if (jumpTarget == null || !jumpTarget.IsAlive())
            {
                if (!targetDiedDuringJump)
                {
                    Debug.Log($"💀 Slam Bros: Target died during jump, landing at last position");
                    targetDiedDuringJump = true;
                    // Continue to last valid position instead of stopping
                }
            }
            else
            {
                // Update last valid position
                lastValidTargetPosition = jumpTarget.transform.position;
            }

            // Use last valid position (either current or stored)
            Vector3 targetPos = lastValidTargetPosition;

            // Validate NavMesh
            UnityEngine.AI.NavMeshHit hit;
            if (!UnityEngine.AI.NavMesh.SamplePosition(targetPos, out hit, 3f, UnityEngine.AI.NavMesh.AllAreas))
            {
                targetPos = transform.position + (targetPos - transform.position).normalized * 2f;
            }
            else
            {
                targetPos = hit.position;
            }

            // Calculate position with arc
            Vector3 horizontalPos = Vector3.Lerp(startPos, targetPos, progress);
            float heightProgress = 1 - Mathf.Pow(2 * progress - 1, 2);
            float currentHeight = jumpHeight * heightProgress;

            transform.position = new Vector3(
                horizontalPos.x,
                horizontalPos.y + currentHeight,
                horizontalPos.z
            );

            // Look at target position
            Vector3 lookDirection = (targetPos - transform.position).normalized;
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }

            yield return null;
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
        finalLandingPos.y = 0;

        // ✅ Only deal damage if original target still alive and in range
        if (jumpTarget != null && jumpTarget.IsAlive())
        {
            float distance = Vector3.Distance(finalLandingPos, jumpTarget.transform.position);
            if (distance <= attackRange)
            {
                DealLandingDamage(finalLandingPos);
            }
            else
            {
                Debug.Log($"💀 Slam Bros: Target too far after landing ({distance:F1}m), no damage");
            }
        }
        else
        {
            Debug.Log($"💀 Slam Bros: Target died during jump, landing without damage");
        }

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

    private void DealLandingDamage(Vector3 landingPos)
    {
        if (jumpTarget != null && jumpTarget.IsAlive())
        {
            DealInstantDamage(jumpTarget);

            if (applyKnockback)
            {
                ApplyLandingKnockback(jumpTarget, landingPos);
            }
        }
    }

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
            Debug.LogWarning($"⚠️ Slam Bros: Knockback position outside NavMesh, skipping");
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
    }

    private void PlayLandingVFX(Vector3 position)
    {
        if (poolingSystem != null)
        {
            GameObject vfx = poolingSystem.InstantiateAPS("slam_bros_land_vfx", position);
            if (vfx != null)
            {
                vfx.transform.rotation = Quaternion.Euler(-90, 0, 0);
                poolingSystem.DestroyAPS(vfx, 2f);
            }
        }
    }

    public override void ExecuteAttackEvent()
    {
        if (jumpTarget != null && jumpTarget.IsAlive())
        {
            DealInstantDamage(jumpTarget);
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