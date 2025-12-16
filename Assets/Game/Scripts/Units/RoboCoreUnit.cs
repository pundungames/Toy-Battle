// ============================================================================
// ROBOCORE UNIT - TRANSFORMER
// ✅ Starts as Mk-I (weak)
// ✅ Transforms to Mk-II after 5s survival (strong)
// Mk-I: Damage=10, Range=2, Cooldown=2.0s, Speed=2.0, HP=25
// Mk-II: Damage=50, Range=3, Cooldown=2.3s, Speed=1.5, HP=50
// ============================================================================

using UnityEngine;
using DG.Tweening;
using System.Collections;
using Zenject;

public class RoboCoreUnit : RuntimeUnit
{
    [Header("Transform Settings")]
    [SerializeField] float transformTime = 5f; // Survival time to transform
    [SerializeField] ToyUnitData mkIIData; // Mk-II stats
    [SerializeField] float mkIIScaleMultiplier = 1.5f; // ✅ Scale multiplier (configurable)
    [SerializeField] string transformVFX = "robocore_transform_vfx";
    [SerializeField] string transformSFX = "robocore_transform";
    [SerializeField] float transformAnimationDuration = 2f;
    [SerializeField] ParticleSystem mk2AttackVfx;

    private bool hasTransformed = false;
    private float survivalTimer = 0f;
    private bool isTransforming = false;
    private bool isBattleActive = false;

    // ===== OVERRIDE START BATTLE =====

    public override void StartBattle()
    {
        base.StartBattle();
        isBattleActive = true;
        survivalTimer = 0f;
        Debug.Log($"🤖 RoboCore: Battle started! Range={attackRange}, Damage={currentDamage}");
    }

    // ===== OVERRIDE UPDATE =====

    protected override void Update()
    {
        base.Update();

        if (!hasTransformed && !isTransforming && IsAlive() && isBattleActive)
        {
            survivalTimer += Time.deltaTime;

            if (survivalTimer >= transformTime)
            {
                StartTransform();
            }
        }
    }

    // ===== OVERRIDE STOP BATTLE =====

    public override void StopBattle()
    {
        base.StopBattle();
        isBattleActive = false;
    }

    // ===== START TRANSFORM =====

    private void StartTransform()
    {
        isTransforming = true;
        hasTransformed = true;

        Debug.Log($"⚡ RoboCore survived {transformTime}s! Transforming to Mk-II...");

        // Stop all actions during transform
        if (agent != null && agent.enabled)
        {
            agent.ResetPath();
            agent.enabled = false;
        }

        // Play transform animation
        if (animator != null)
        {
            animator.SetTrigger("Transform");
        }

        StartCoroutine(TransformSequence());
    }

    // ===== TRANSFORM SEQUENCE =====

    private IEnumerator TransformSequence()
    {
        // Transform VFX
        if (poolingSystem != null && !string.IsNullOrEmpty(transformVFX))
        {
            GameObject vfx = poolingSystem.InstantiateAPS(transformVFX, transform.position);
            if (vfx != null)
            {
                vfx.transform.SetParent(transform);
                poolingSystem.DestroyAPS(vfx, transformAnimationDuration + 1f);
            }
        }

        // Transform SFX
        if (audioManager != null && !string.IsNullOrEmpty(transformSFX))
        {
            audioManager.Play(transformSFX);
        }

        // Visual effect: Spin
        transform.DORotate(new Vector3(0, 720, 0), transformAnimationDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear);

        // Visual effect: Grow
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = startScale * mkIIScaleMultiplier;

        transform.DOScale(targetScale, transformAnimationDuration)
            .SetEase(Ease.InOutQuad);

        Taptic.Heavy();

        // Wait for animation
        yield return new WaitForSeconds(transformAnimationDuration);

        // ✅ CRITICAL: Update originalScale AFTER transform completes!
        originalScale = targetScale;

        // Apply Mk-II stats
        ApplyMkIIStats();

        // ✅ CRITICAL: Re-enable agent and force recalculation
        if (agent != null)
        {
            if (!agent.enabled)
            {
                agent.enabled = true;
            }

            // Force agent to recalculate with new size
            agent.ResetPath();

            // If we have a target, set destination again
            if (currentTarget != null && currentTarget.IsAlive())
            {
                agent.SetDestination(currentTarget.transform.position);
            }

            Debug.Log($"✅ Agent restarted: enabled={agent.enabled}, onNavMesh={agent.isOnNavMesh}");
        }

        isTransforming = false;

        Debug.Log($"✅ Mk-II Transform Complete! Scale={transform.localScale}, DMG={currentDamage}, HP={currentHealthValue}, Range={attackRange}");
    }

    // ===== APPLY MK-II STATS =====

    private void ApplyMkIIStats()
    {
        if (mkIIData == null)
        {
            Debug.LogWarning("⚠️ RoboCore: Mk-II data not assigned!");
            return;
        }

        Debug.Log($"📊 Before update: Range={attackRange}, Damage={currentDamage}, HP={currentHealthValue}");

        // ✅ Update data reference
        data = mkIIData;

        // ✅ Update stats from mkIIData
        currentHealthValue = mkIIData.GetScaledHP(); // Full heal
        maxHealth = mkIIData.GetScaledHP();
        currentDamage = mkIIData.GetScaledDamage();
        attackRange = mkIIData.attackRange;
        attackCooldown = mkIIData.attackCooldown;
        moveSpeed = mkIIData.moveSpeed;
        attackVfx = mk2AttackVfx;
        // ✅ CRITICAL: Update NavMeshAgent with scaled values!
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = attackRange * 0.8f;

            // ✅ SCALE AGENT SIZE!
            agent.radius = agent.radius * mkIIScaleMultiplier; // Scale radius
            agent.height = agent.height * mkIIScaleMultiplier; // Scale height

            Debug.Log($"🤖 Agent updated: radius={agent.radius}, height={agent.height}, stoppingDistance={agent.stoppingDistance}");
        }

        // Heal VFX
        PlayHealVFX();

        Debug.Log($"📊 After update: Range={attackRange}, Damage={currentDamage}, HP={currentHealthValue}/{maxHealth}");
    }

    // ===== HEAL VFX =====

    private void PlayHealVFX()
    {
        if (poolingSystem != null)
        {
            GameObject vfx = poolingSystem.InstantiateAPS("heal_vfx", transform.position);
            if (vfx != null)
            {
                poolingSystem.DestroyAPS(vfx, 2f);
            }
        }
    }

    // ===== OVERRIDE TAKE DAMAGE =====

    public override void TakeDamage(float damage)
    {
        if (isTransforming)
        {
            Debug.Log("🛡️ RoboCore is invincible during transformation!");
            return;
        }

        base.TakeDamage(damage);
    }

    // ===== GIZMOS =====

    private void OnDrawGizmosSelected()
    {
        // ✅ Always draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Show survival timer
        if (!hasTransformed && Application.isPlaying && isBattleActive)
        {
            float progress = survivalTimer / transformTime;
            Gizmos.color = Color.Lerp(Color.red, Color.green, progress);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.5f);
        }

        // Debug text
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 3f,
            $"Range: {attackRange:F1}\nDamage: {currentDamage}\nHP: {currentHealthValue:F0}");
#endif
    }

    // ===== RESET ON DEATH =====

    private void OnDestroy()
    {
        transform.DOKill();
        StopAllCoroutines();
    }
}