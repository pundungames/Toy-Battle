// ============================================================================
// SLAM BROS UNIT - JUMP ATTACK FIGHTER
// ✅ Jumps to target with DOTween
// ✅ Jump duration based on distance
// ✅ Attack on landing
// Stats: Damage=7, Range=5, Cooldown=1.3s, Speed=3.6, HP=30
// Note: 2 units spawn per slot (future implementation)
// ============================================================================

using UnityEngine;
using DG.Tweening;
using Zenject;

public class SlamBrosUnit : RuntimeUnit
{
    [Header("Jump Attack Settings")]
    [SerializeField] float jumpHeight = 3f; // Arc height
    [SerializeField] float jumpSpeed = 8f; // Units per second
    [SerializeField] float attackAnimationDuration = 0.5f; // Land attack anim

    private bool isJumping = false;

    // ===== OVERRIDE EXECUTE ATTACK =====

    protected override void ExecuteAttack(RuntimeUnit target)
    {
        if (isJumping) return;

        // Start jump attack
        StartJumpAttack(target);
    }

    // ===== JUMP ATTACK SEQUENCE =====

    private void StartJumpAttack(RuntimeUnit target)
    {
        if (target == null || !target.IsAlive()) return;

        isJumping = true;
        LockAttack();

        // Calculate jump duration based on distance
        float distance = Vector3.Distance(transform.position, target.transform.position);
        float jumpDuration = distance / jumpSpeed;

        // Disable NavMeshAgent during jump
        if (agent != null && agent.enabled)
        {
            agent.enabled = false;
        }

        // Play jump animation (rising)
        if (animator != null)
        {
            animator.SetTrigger("Jump");
        }

        // Calculate landing position
        Vector3 landingPos = target.transform.position;

        // ✅ CRITICAL: Check if landing position is on NavMesh
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(landingPos, out hit, 3f, UnityEngine.AI.NavMesh.AllAreas))
        {
            landingPos = hit.position;
        }
        else
        {
            Debug.LogWarning($"⚠️ Slam Bros: Landing position not on NavMesh, adjusting");
            landingPos = transform.position + (target.transform.position - transform.position).normalized * 2f;
        }

        // Jump with DOTween arc
        Sequence jumpSeq = DOTween.Sequence();

        // Jump to target
        jumpSeq.Append(transform.DOJump(landingPos, jumpHeight, 1, jumpDuration)
            .SetEase(Ease.Linear));

        // Rotate during jump (optional cool effect)
       /* jumpSeq.Join(transform.DORotate(new Vector3(0, 360, 0), jumpDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear));*/

        jumpSeq.OnComplete(() =>
        {
            // Landing! Play attack animation
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }

            // Deal damage to nearby enemies (landing impact)
            DealLandingDamage(landingPos);

            // Play landing VFX
            PlayLandingVFX(landingPos);

            // Play landing SFX
            if (audioManager != null)
            {
                audioManager.Play("slam_bros_land");
            }

            Taptic.Heavy();

            // Re-enable agent after landing
            if (agent != null && !agent.enabled)
            {
                agent.enabled = true;
            }

            // Unlock after attack animation
            Invoke(nameof(UnlockJump), attackAnimationDuration);
        });
    }

    private void UnlockJump()
    {
        isJumping = false;
        UnlockAttack();
    }

    // ===== LANDING DAMAGE =====

    private void DealLandingDamage(Vector3 landingPos)
    {
        // Find target (should still be current target)
        if (currentTarget != null && currentTarget.IsAlive())
        {
            float distance = Vector3.Distance(landingPos, currentTarget.transform.position);

            // If close enough, deal damage
            if (distance <= attackRange)
            {
                DealInstantDamage(currentTarget);
            }
        }
    }

    // ===== VFX =====

    private void PlayLandingVFX(Vector3 position)
    {
        if (poolingSystem != null)
        {
            GameObject vfx = poolingSystem.InstantiateAPS("slam_bros_land_vfx", position);
            if (vfx != null)
            {
                poolingSystem.DestroyAPS(vfx, 2f);
            }
        }
    }

    // ===== ANIMATION EVENT (Optional) =====

    /// <summary>
    /// If you want damage on animation event instead of landing, override this
    /// </summary>
    public override void ExecuteAttackEvent()
    {
        // Deal damage at animation frame (alternative to landing damage)
        if (currentTarget != null && currentTarget.IsAlive())
        {
            DealInstantDamage(currentTarget);
        }

        // Note: Don't unlock here, UnlockJump handles it
    }

    // ===== RESET ON DEATH =====

    private void OnDestroy()
    {
        transform.DOKill();
        isJumping = false;
        CancelInvoke(nameof(UnlockJump));
    }
}