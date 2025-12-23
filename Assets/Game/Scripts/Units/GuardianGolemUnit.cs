// ============================================================================
// GUARDIAN GOLEM - WITH SPLIT ON DEATH ABILITY
// ✅ Dash attack with target death handling
// ✅ Splits into mini golems on death (Level 1: 2 pieces, Level 2-3: 4 pieces)
// ✅ Mini golems inherit 10% HP/Damage
// ============================================================================

using UnityEngine;
using DG.Tweening;
using Zenject;
using UnityEngine.AI;
using System.Collections.Generic;

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

    [Header("Split on Death Settings")]
    [SerializeField] bool enableSplit = true;
    [SerializeField] GameObject miniGolemPrefab; // Optional: Use different prefab
    [SerializeField] float miniGolemScale = 0.5f; // Scale if using same prefab
    [SerializeField] float miniGolemHPPercent = 0.1f; // 10% of base HP
    [SerializeField] float miniGolemDamagePercent = 0.1f; // 10% of base damage
    [SerializeField] float spawnRadius = 1.5f;
    [SerializeField] float spawnDelay = 0.2f; // Delay between spawns

    private bool isDashing = false;
    private RuntimeUnit dashTarget;
    private Tween currentDashTween;
    private bool hasSplit = false; // Prevent multiple splits

    private void OnEnable()
    {
        EventManager.onBattleComplete += BattleEnd;
    }
    private void OnDisable()
    {
        EventManager.onBattleComplete -= BattleEnd;
    }
    void BattleEnd(bool won)
    {
        if (!enableSplit) OnDeath();
    }
    protected override void ExecuteAttack(RuntimeUnit target)
    {
        if (isDashing) return;

        dashTarget = target;

        // ✅ HEDEFE BAK (Anlık dönme için)
        Vector3 lookPos = target.transform.position;
        lookPos.y = transform.position.y; // Y ekseninde eğilmemesi için
        transform.LookAt(lookPos);

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

        currentDashTween = transform.DOMove(dashTargetPos, dashDuration)
            .SetEase(Ease.OutQuad)
            .OnUpdate(() =>
            {
                if (dashTarget == null || !dashTarget.IsAlive())
                {
                    Debug.Log($"💀 Guardian Golem: Target died during dash, stopping");

                    if (currentDashTween != null)
                    {
                        currentDashTween.Kill();
                    }

                    if (agent != null && !agent.enabled)
                    {
                        agent.enabled = true;
                    }

                    isDashing = false;
                }
            })
            .OnComplete(() =>
            {
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
                dashTarget = null;
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

    // ===== VFX/SFX =====

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

    // ===== SPLIT ON DEATH =====

    protected override void OnUnitDeath()
    {
        // Call base death logic first
        base.OnUnitDeath();

        // Then split if enabled and not already split
        if (enableSplit && !hasSplit)
        {
            hasSplit = true;
            SplitIntoMiniGolems();
        }
    }

    private void SplitIntoMiniGolems()
    {
        if (data == null)
        {
            Debug.LogWarning("⚠️ Golem data is null, cannot split!");
            return;
        }

        // Determine split count based on level
        int splitCount = data.level == 1 ? 2 : 4;

        Vector3 deathPosition = transform.position;
        deathPosition.y = 0; // Ground level

        Debug.Log($"🗿 Guardian Golem splitting into {splitCount} mini golems!");

        // Spawn mini golems
        for (int i = 0; i < splitCount; i++)
        {
            SpawnMiniGolem(deathPosition, i, splitCount);
        }

        // Play split VFX
        if (poolingSystem != null)
        {
            GameObject splitVfx = poolingSystem.InstantiateAPS("golem_split_vfx", deathPosition + Vector3.up * 0.5f);
            if (splitVfx != null)
            {
                container.InjectGameObject(splitVfx);
                poolingSystem.DestroyAPS(splitVfx, 2f);
            }
        }

        // Play split sound
        if (audioManager != null)
        {
            audioManager.Play("golem_split");
        }

        Taptic.Light();
    }

    private void SpawnMiniGolem(Vector3 centerPos, int index, int totalCount)
    {
        // Calculate spawn position in circle around death point
        float angle = (360f / totalCount) * index;
        float angleRad = angle * Mathf.Deg2Rad;

        Vector3 spawnOffset = new Vector3(
            Mathf.Cos(angleRad) * spawnRadius,
            0,
            Mathf.Sin(angleRad) * spawnRadius
        );

        Vector3 spawnPos = centerPos + spawnOffset;

        // Validate NavMesh position
        NavMeshHit navHit;
        if (!NavMesh.SamplePosition(spawnPos, out navHit, 3f, NavMesh.AllAreas))
        {
            Debug.LogWarning($"⚠️ Mini golem spawn position not on NavMesh, using center");
            spawnPos = centerPos;

            // Try center
            if (!NavMesh.SamplePosition(spawnPos, out navHit, 5f, NavMesh.AllAreas))
            {
                Debug.LogError($"❌ Cannot find valid NavMesh position for mini golem!");
                return;
            }
        }

        spawnPos = navHit.position;

        // Determine which prefab to use
        GameObject prefabToUse = miniGolemPrefab != null ? miniGolemPrefab : gameObject;

        // Instantiate mini golem
        GameObject miniGolemObj = Instantiate(prefabToUse, spawnPos, Quaternion.identity);

        // Scale down if using same prefab
        if (miniGolemPrefab == null)
        {
            miniGolemObj.transform.localScale = originalScale * miniGolemScale;
        }

        // Setup RuntimeUnit
        RuntimeUnit miniGolem = miniGolemObj.GetComponent<RuntimeUnit>();

        if (miniGolem == null)
        {
            Debug.LogError($"❌ Mini golem prefab missing RuntimeUnit component!");
            Destroy(miniGolemObj);
            return;
        }

        // Initialize with reduced stats
        miniGolem.Initialize(data, gridSlot, isPlayerUnit);

        // Override HP and Damage
        float miniHP = data.GetScaledHP() * miniGolemHPPercent;
        float miniDamage = data.GetScaledDamage() * miniGolemDamagePercent;

        miniGolem.SetCustomStats(miniHP, Mathf.RoundToInt(miniDamage));

        // Inject dependencies
        container.InjectGameObject(miniGolemObj);

        // Disable split on mini golems (prevent infinite splits)
        GuardianGolemUnit miniGolemScript = miniGolemObj.GetComponent<GuardianGolemUnit>();
        if (miniGolemScript != null)
        {
            miniGolemScript.enableSplit = false;
        }

        // Spawn animation
        miniGolemObj.transform.localScale = Vector3.zero;
        miniGolemObj.transform.DOScale(miniGolemPrefab == null ? originalScale * miniGolemScale : Vector3.one, 0.3f)
            .SetEase(Ease.OutBack)
            .SetDelay(spawnDelay * index);

        // ✅ Add to BattleManager lists (CRITICAL!)
        BattleManager battleManager = FindObjectOfType<BattleManager>();
        if (battleManager != null)
        {
            // Add to appropriate team list using new methods
            if (isPlayerUnit)
            {
                battleManager.AddPlayerUnit(miniGolem);
            }
            else
            {
                battleManager.AddEnemyUnit(miniGolem);
            }

            // Start battle behavior
            miniGolem.StartBattle();
        }
        else
        {
            Debug.LogWarning("⚠️ BattleManager not found, mini golem won't be targetable!");
        }

        Debug.Log($"✅ Mini golem {index + 1}/{totalCount} spawned with {miniHP} HP and {miniDamage} damage");
    }

    // ===== CLEANUP =====

    private void OnDestroy()
    {
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