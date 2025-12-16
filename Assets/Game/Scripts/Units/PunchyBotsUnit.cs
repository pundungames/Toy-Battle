// ============================================================================
// PUNCHY BOTS UNIT - TWIN FIGHTERS
// ✅ Two units spawn per slot (twin system)
// ✅ Both attack the same target
// ✅ 0.5s stun every 5 attacks (shared counter)
// Stats: Damage=15 (each), Range=2, Cooldown=1.6s, Speed=2.6, HP=40 (each)
// ============================================================================

using UnityEngine;
using DG.Tweening;
using System.Collections;
using Zenject;
using MK.Toon;

public class PunchyBotsUnit : RuntimeUnit
{
    [Header("Twin Settings")]
    public bool isPrimaryBot = true; // ✅ PUBLIC: Set by GridManager
    [SerializeField] PunchyBotsUnit twinBot; // Reference to twin

    [Header("Stun Settings")]
    [SerializeField] int attacksUntilStun = 5; // Stun every 5 attacks
    [SerializeField] float stunDuration = 0.5f;
    [SerializeField] string stunVFX = "punchy_stun_vfx";
    [SerializeField] string stunSFX = "punchy_stun";

    private static int sharedAttackCount = 0; // ✅ Shared between twins!
    private bool isStunning = false;
    [SerializeField] ParticleSystem rightAttackVfx;

    // ===== OVERRIDE INITIALIZE =====

    public override void Initialize(ToyUnitData unitData, int slot, bool isPlayer)
    {
        base.Initialize(unitData, slot, isPlayer);

        // Reset shared counter when first bot spawns
        if (isPrimaryBot)
        {
            sharedAttackCount = 0;
        }
    }
    public void RightAttackVfx()
    {
         rightAttackVfx.Play();
    }
    // ===== LINK TWIN =====

    public void LinkTwin(PunchyBotsUnit twin)
    {
        twinBot = twin;
    }

    // ===== OVERRIDE EXECUTE ATTACK EVENT =====

    public override void ExecuteAttackEvent()
    {
        if (currentTarget != null && currentTarget.IsAlive())
        {
            // Deal damage
            DealInstantDamage(currentTarget);

            // Increment shared attack count
            sharedAttackCount++;

            Debug.Log($"👊 Punchy Bot attacked! Shared count: {sharedAttackCount}/{attacksUntilStun}");

            // Check if stun should trigger
            if (sharedAttackCount >= attacksUntilStun)
            {
                // Reset counter
                sharedAttackCount = 0;

                // Apply stun
                ApplyStun(currentTarget);
            }
        }

        PlayAttackVFX();
        PlayAttackSFX();
        UnlockAttack();
    }

    // ===== APPLY STUN =====

    private void ApplyStun(RuntimeUnit target)
    {
        if (target == null || !target.IsAlive()) return;

        Debug.Log($"⚡ Punchy Bots stunned {target.data.toyName} for {stunDuration}s!");

        // Get target's PunchyBotsUnit component (if target is also Punchy Bots)
        // Otherwise, stun any RuntimeUnit
        StartCoroutine(StunTarget(target));

        // Stun VFX
        if (poolingSystem != null && !string.IsNullOrEmpty(stunVFX))
        {
            GameObject vfx = poolingSystem.InstantiateAPS(stunVFX, target.transform.position);
            if (vfx != null)
            {
                vfx.transform.SetParent(target.transform);
                poolingSystem.DestroyAPS(vfx, stunDuration + 0.5f);
            }
        }

        // Stun SFX
        if (audioManager != null && !string.IsNullOrEmpty(stunSFX))
        {
            audioManager.Play(stunSFX);
        }

        Taptic.Medium();
    }

    // ===== STUN TARGET COROUTINE =====

    private IEnumerator StunTarget(RuntimeUnit target)
    {
        // Disable target's agent
        UnityEngine.AI.NavMeshAgent targetAgent = target.GetComponent<UnityEngine.AI.NavMeshAgent>();
        bool wasAgentEnabled = false;

        if (targetAgent != null && targetAgent.enabled)
        {
            wasAgentEnabled = true;
            targetAgent.enabled = false;
        }

        // Visual effect: Shake
        target.transform.DOShakePosition(stunDuration, 0.1f, 20, 90, false);

        // Lock target's attack
        target.LockAttack();

        // Wait stun duration
        yield return new WaitForSeconds(stunDuration);

        // Re-enable agent
        if (targetAgent != null && wasAgentEnabled && !targetAgent.enabled)
        {
            targetAgent.enabled = true;
        }

        // Unlock target's attack
        target.UnlockAttack();

        Debug.Log($"⚡ Stun ended on {target.data.toyName}");
    }

    // ===== SYNC WITH TWIN =====

    protected override void Update()
    {
        base.Update();

        // Sync target with twin (both attack same target)
        if (twinBot != null && currentTarget != twinBot.currentTarget)
        {
            // Primary bot decides the target
            if (isPrimaryBot)
            {
                twinBot.currentTarget = currentTarget;
            }
            else
            {
                currentTarget = twinBot.currentTarget;
            }
        }
    }

    // ===== GIZMOS =====

    private void OnDrawGizmosSelected()
    {
        // Draw line to twin
        if (twinBot != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, twinBot.transform.position);
        }

        // Show attack count
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < sharedAttackCount; i++)
            {
                Vector3 pos = transform.position + Vector3.up * (2f + i * 0.3f);
                Gizmos.DrawSphere(pos, 0.1f);
            }
        }
    }

    // ===== RESET ON DEATH =====

    private void OnDestroy()
    {
        transform.DOKill();
        StopAllCoroutines();
    }
}