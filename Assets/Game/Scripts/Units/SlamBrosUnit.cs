// ============================================================================
// SLAM BROS - FIXED Y POSITION & NAVMESH BUG
// ✅ Y position always reset to NavMesh height
// ✅ Agent properly disabled/enabled
// ✅ Safe NavMesh validation
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

    private RuntimeUnit jumpTarget;
    private bool isJumping = false;
    private Coroutine trackingCoroutine;
    private Vector3 lastValidTargetPosition;

    protected override void ExecuteAttack(RuntimeUnit target)
    {
        if (isJumping) return;

        jumpTarget = target;
        lastValidTargetPosition = target.transform.position;

        StartJumpAttack(target);
    }

    private void StartJumpAttack(RuntimeUnit target)
    {
        if (target == null || !target.IsAlive()) return;

        isJumping = true;
        LockAttack();

        // ✅ CRITICAL: Disable NavMeshAgent before jump
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

        // ✅ Force start Y to NavMesh height
        UnityEngine.AI.NavMeshHit navHit;
        if (UnityEngine.AI.NavMesh.SamplePosition(startPos, out navHit, 5f, UnityEngine.AI.NavMesh.AllAreas))
        {
            startPos.y = navHit.position.y;
            transform.position = startPos;
        }

        float elapsedTime = 0f;
        bool targetDiedDuringJump = false;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            // Check if target still alive
            if (jumpTarget == null || !jumpTarget.IsAlive())
            {
                if (!targetDiedDuringJump)
                {
                    Debug.Log($"💀 Slam Bros: Target died during jump, landing at last position");
                    targetDiedDuringJump = true;
                }
            }
            else
            {
                lastValidTargetPosition = jumpTarget.transform.position;
            }

            // ✅ Use last valid position and ensure NavMesh height
            Vector3 targetPos = lastValidTargetPosition;

            // ✅ CRITICAL: Sample NavMesh to get valid Y position
            if (UnityEngine.AI.NavMesh.SamplePosition(targetPos, out navHit, 5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                targetPos = navHit.position; // Use NavMesh Y
            }
            else
            {
                // Fallback: project forward from start
                targetPos = startPos + (targetPos - startPos).normalized * 2f;
                targetPos.y = startPos.y; // Keep original Y
            }

            // Calculate position with arc
            Vector3 horizontalPos = Vector3.Lerp(startPos, targetPos, progress);

            // ✅ CRITICAL: Base Y always from NavMesh
            float baseY = horizontalPos.y;

            // Add jump arc
            float heightProgress = 1 - Mathf.Pow(2 * progress - 1, 2);
            float currentHeight = jumpHeight * heightProgress;

            // ✅ Final position with controlled Y
            transform.position = new Vector3(
                horizontalPos.x,
                baseY + currentHeight, // NavMesh Y + arc height
                horizontalPos.z
            );

            // Look at target
            Vector3 lookDirection = (targetPos - transform.position).normalized;
            lookDirection.y = 0; // ✅ Keep horizontal rotation only

            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }

            yield return null;
        }

        OnJumpComplete();
    }

    private void OnJumpComplete()
    {
        // ✅ CRITICAL: Force position to NavMesh before re-enabling agent
        Vector3 currentPos = transform.position;
        UnityEngine.AI.NavMeshHit navHit;

        if (UnityEngine.AI.NavMesh.SamplePosition(currentPos, out navHit, 5f, UnityEngine.AI.NavMesh.AllAreas))
        {
            transform.position = navHit.position;
            Debug.Log($"✅ Slam Bros: Corrected landing position to NavMesh Y={navHit.position.y:F2}");
        }
        else
        {
            // Emergency fallback: snap to ground
            currentPos.y = 0;
            transform.position = currentPos;
            Debug.LogWarning($"⚠️ Slam Bros: NavMesh not found, forced Y to 0");
        }

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // Deal damage if target still valid
        Vector3 finalLandingPos = transform.position;

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

        // ✅ CRITICAL: Re-enable agent AFTER position is fixed
        if (agent != null)
        {
            // Warp agent to current position
            if (!agent.enabled)
            {
                agent.enabled = true;
            }

            // ✅ Warp prevents "not on NavMesh" error
            if (agent.isOnNavMesh)
            {
                agent.Warp(transform.position);
            }
            else
            {
                Debug.LogWarning($"⚠️ Slam Bros: Agent not on NavMesh after landing!");
            }
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

        // ✅ Final safety check: ensure agent is on NavMesh
        if (agent != null && agent.enabled)
        {
            if (!agent.isOnNavMesh)
            {
                Vector3 pos = transform.position;
                UnityEngine.AI.NavMeshHit hit;
                if (UnityEngine.AI.NavMesh.SamplePosition(pos, out hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    transform.position = hit.position;
                    agent.Warp(hit.position);
                    Debug.Log($"✅ Slam Bros: Fixed NavMesh position in UnlockJump");
                }
            }
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
        knockbackDirection.y = 0; // ✅ Horizontal only

        Vector3 knockbackTarget = target.transform.position + knockbackDirection * knockbackForce;

        // ✅ Ensure knockback target is on NavMesh
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
                if (targetAgent != null && wasAgentEnabled)
                {
                    // ✅ Warp agent to final position
                    if (!targetAgent.enabled)
                    {
                        targetAgent.enabled = true;
                    }

                    if (targetAgent.isOnNavMesh)
                    {
                        targetAgent.Warp(target.transform.position);
                    }
                }
            });
    }

    private void PlayLandingVFX(Vector3 position)
    {
        // ✅ VFX at ground level
        position.y = 0.1f;

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

    // ✅ Override Update to prevent movement during jump
    protected override void Update()
    {
        if (isJumping)
        {
            // Skip normal update during jump
            return;
        }

        base.Update();
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