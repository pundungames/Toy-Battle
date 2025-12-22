// ============================================================================
// RUNTIME UNIT - BASE CLASS WITH VIRTUAL ATTACK SYSTEM
// ✅ NavMeshAgent movement
// ✅ Virtual ExecuteAttack() for specialized units
// ✅ VFX/SFX integration ready
// ============================================================================

using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;
using System.Collections.Generic;
using Zenject;

public class RuntimeUnit : MonoBehaviour, IHealthProvider
{
    // ===== INJECTED DEPENDENCIES =====
    [Inject] protected DiContainer container;
    [Inject] protected PoolingSystem poolingSystem;
    [Inject] protected AudioManager audioManager;

    // ===== DATA =====
    public ToyUnitData data;
    public int gridSlot;
    public bool isPlayerUnit;

    // ===== HEALTH (IHealthProvider) =====
    protected float maxHealth;
    public float currentHealthValue;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealthValue;
    public event System.Action<float, float> OnHealthChanged;

    // ===== BACKWARD COMPATIBILITY =====
    public int currentHP => Mathf.RoundToInt(currentHealthValue);
    public GameObject visualObject => gameObject;

    // ===== DAMAGE =====
    public int currentDamage;

    // ===== BATTLE BUFFS =====
    public float damageMultiplier = 1f;
    public float shieldAmount = 0f;
    public bool hasFirstAttackCancel = false;
    public int poisonTicks = 0;

    // ===== REFERENCES =====
    public HealthBarUI healthBar;
    public Transform projectileSpawnPoint;
    public Animator animator;
    public EnemyDamageText damageTextPrefab;

    // ===== NAVMESHAGENT =====
    public NavMeshAgent agent;

    // ===== COMBAT SETTINGS =====
    [Header("Combat Settings (Auto-loaded from ScriptableObject)")]
    [SerializeField] protected float attackRange = 2f;
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected float attackCooldown = 1f;
    [SerializeField] protected ParticleSystem attackVfx;

    [Header("Hit Feedback")]
    [SerializeField] float hitScaleFactor = 1.1f;
    [SerializeField] float hitDuration = 0.3f;

    // ===== PRIVATE STATE =====
    protected RuntimeUnit currentTarget;
    protected float lastAttackTime = 0f;
    private bool isInBattle = false;
    [SerializeField] bool isExecutingAttack = false; // ✅ NEW: Prevent attack spam
    private Vector3 originalPosition;
    protected Vector3 originalScale;
    private bool isHitFeedbackActive = false;

    [Header("Enemy Material Settings")]
    [SerializeField] Material enemyMaterial;

    public virtual void Initialize(ToyUnitData unitData, int slot, bool isPlayer)
    {
        data = unitData;
        gridSlot = slot;
        isPlayerUnit = isPlayer;

        maxHealth = unitData.GetScaledHP();
        currentHealthValue = maxHealth;
        currentDamage = unitData.GetScaledDamage();

        originalPosition = transform.localPosition;
        originalScale = transform.localScale;

        // ✅ Load combat values from ToyUnitData
        attackRange = unitData.attackRange;
        moveSpeed = unitData.moveSpeed;
        attackCooldown = unitData.attackCooldown;

        // ✅ Setup NavMeshAgent
        SetupNavMeshAgent();

        if (healthBar != null)
        {
            OnHealthChanged?.Invoke(currentHealthValue, maxHealth);
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        // ✅ Setup projectile spawn point if not set
        if (projectileSpawnPoint == null)
        {
            GameObject spawnPoint = new GameObject("ProjectileSpawnPoint");
            spawnPoint.transform.SetParent(transform);
            spawnPoint.transform.localPosition = Vector3.up * 1f;
            projectileSpawnPoint = spawnPoint.transform;
        }

        // ✅ NEW: Apply enemy material if enemy unit
        if (!isPlayer)
        {
            ApplyEnemyMaterial();
        }

        Debug.Log($"✅ {unitData.toyName} initialized: Range={attackRange}, Speed={moveSpeed}, Agent={agent != null}, IsEnemy={!isPlayer}");
    }

    private void ApplyEnemyMaterial()
    {
        // ✅ Get all MeshRenderers and SkinnedMeshRenderers
        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
        SkinnedMeshRenderer[] skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();

        int changedCount = 0;

        // ✅ Change MeshRenderer materials
        foreach (var meshRenderer in meshRenderers)
        {
            if (enemyMaterial != null)
            {
                // Use custom enemy material
                Material[] newMaterials = new Material[meshRenderer.materials.Length];
                for (int i = 0; i < newMaterials.Length; i++)
                {
                    newMaterials[i] = enemyMaterial;
                }
                meshRenderer.materials = newMaterials;
            }
            changedCount++;
        }

        // ✅ Change SkinnedMeshRenderer materials
        foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
        {
            if (enemyMaterial != null)
            {
                // Use custom enemy material
                Material[] newMaterials = new Material[skinnedMeshRenderer.materials.Length];
                for (int i = 0; i < newMaterials.Length; i++)
                {
                    newMaterials[i] = enemyMaterial;
                }
                skinnedMeshRenderer.materials = newMaterials;
            }
            changedCount++;
        }

        Debug.Log($"🎨 Applied enemy materials to {changedCount} renderers on {data.toyName}");
    }
    // ===== NAVMESHAGENT SETUP =====

    private void SetupNavMeshAgent()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }

        agent.speed = moveSpeed;
        agent.acceleration = moveSpeed * 4f;
        agent.angularSpeed = 360f;
        agent.stoppingDistance = attackRange * 0.8f;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        agent.enabled = false;

        Debug.Log($"🗺️ NavMeshAgent configured: speed={agent.speed}, stoppingDistance={agent.stoppingDistance}");
    }

    // ===== BATTLE CONTROL =====

    public virtual void StartBattle()
    {
        isInBattle = true;
        lastAttackTime = Time.time;

        if (agent != null)
        {
            agent.enabled = true;
            Debug.Log($"✅ {data.toyName} NavMeshAgent enabled for battle");
        }
    }

    public virtual void StopBattle()
    {
        isInBattle = false;

        if (agent != null && agent.enabled)
        {
            agent.ResetPath();
            agent.enabled = false;
        }

        if (animator != null)
        {
            animator.SetBool("Move", false);
            // Attack is a trigger, no need to reset
        }
    }

    // ===== UPDATE - NAVMESHAGENT MOVEMENT =====

    protected virtual void Update()
    {
        if (!isInBattle || !IsAlive()) return;

        // 1. Find or validate target
        if (currentTarget == null || !currentTarget.IsAlive())
        {
            currentTarget = FindNearestEnemy();
        }

        if (currentTarget == null)
        {
            if (agent != null && agent.enabled)
            {
                agent.ResetPath();
            }

            if (animator != null)
            {
                animator.SetBool("Move", false);
                // Attack is a trigger, no need to reset
            }
            return;
        }

        // 2. Check distance to target
        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);

        // 3. If in range → Attack
        if (distanceToTarget <= attackRange)
        {
            if (agent != null && agent.enabled && agent.hasPath)
            {
                agent.ResetPath();
            }

            Vector3 lookDirection = (currentTarget.transform.position - transform.position).normalized;
            if (lookDirection != Vector3.zero)
            {
                //  transform.rotation = Quaternion.LookRotation(lookDirection);
            }

            if (animator != null)
            {
                animator.SetBool("Move", false);
            }

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                // ✅ Check if not already executing attack
                if (!isExecutingAttack)
                {
                    // ✅ Virtual attack method (triggers animation + locks)
                    ExecuteAttack(currentTarget);
                    lastAttackTime = Time.time;
                }
            }
        }
        // 4. If out of range → Navigate to target
        else
        {
            NavigateToTarget(currentTarget);
        }
    }

    // ===== VIRTUAL ATTACK METHOD =====

    /// <summary>
    /// ✅ VIRTUAL METHOD: Called by Update when attack cooldown ready
    /// Default: Trigger animation, actual damage dealt via animation event
    /// Override for: Custom attack logic (no animation)
    /// </summary>
    protected virtual void ExecuteAttack(RuntimeUnit target)
    {
        // Trigger attack animation
        if (animator != null)
        {
            animator.SetTrigger("Attack");
            LockAttack(); // Lock until animation event calls ExecuteAttackEvent()
        }
        else
        {
            // No animator, deal damage immediately
            ExecuteAttackEvent();
        }
    }

    /// <summary>
    /// ✅ ANIMATION EVENT: Called at the exact moment of attack
    /// This is where actual damage/projectile spawn happens
    /// Override this for specialized attacks
    /// </summary>
    public virtual void ExecuteAttackEvent()
    {
        // Default: Deal instant melee damage
        if (currentTarget != null && currentTarget.IsAlive())
        {
            DealInstantDamage(currentTarget);
        }

        // Unlock attack for next cycle
        UnlockAttack();
    }

    /// <summary>
    /// ✅ Helper: Lock attack state (for animation-based attacks)
    /// Call this at the start of attack
    /// </summary>
    public void LockAttack()
    {
        isExecutingAttack = true;
    }

    /// <summary>
    /// ✅ Helper: Unlock attack state
    /// Call this when attack completes
    /// </summary>
    public void UnlockAttack()
    {
        isExecutingAttack = false;
    }

    /// <summary>
    /// ✅ Helper: Instant damage (melee units)
    /// </summary>
    protected void DealInstantDamage(RuntimeUnit target)
    {
        if (target.hasFirstAttackCancel)
        {
            target.hasFirstAttackCancel = false;
            Debug.Log($"⚔️ {target.data.toyName} blocked first attack!");
            return;
        }

        target.TakeDamage(GetFinalDamage());

        // Play attack VFX
        PlayAttackVFX();

        // Play attack SFX
        PlayAttackSFX();

        Taptic.Light();
    }

    // ===== VFX/SFX HELPERS =====

    protected void PlayAttackVFX()
    {
        if (attackVfx)
        {
            attackVfx.Play();
        }
    }

    protected void PlayAttackSFX()
    {
        if (audioManager != null && !string.IsNullOrEmpty(data.unitID))
        {
            string sfxID = $"{data.unitID}_attack";
            audioManager.Play(sfxID);
        }
    }

    protected void PlayHitVFX(Vector3 position)
    {
        if (poolingSystem != null && !string.IsNullOrEmpty(data.unitID))
        {
            string vfxID = $"{data.unitID}_hit_vfx";
            GameObject vfx = poolingSystem.InstantiateAPS(vfxID, position);
            if (vfx != null)
            {
                poolingSystem.DestroyAPS(vfx, 2f);
            }
        }
    }

    // ===== FIND NEAREST ENEMY =====

    protected RuntimeUnit FindNearestEnemy()
    {
        BattleManager battleManager = FindObjectOfType<BattleManager>();
        if (battleManager == null) return null;

        List<RuntimeUnit> enemies = isPlayerUnit ?
            battleManager.GetEnemyUnits() :
            battleManager.GetPlayerUnits();

        // ✅ NEW: Use SmartTargetingSystem if available
        SmartTargetingSystem smartTargeting = SmartTargetingSystem.Instance;

        if (smartTargeting != null)
        {
            RuntimeUnit smartTarget = smartTargeting.SelectTarget(this, enemies);

            // If smart system returned a target, use it
            if (smartTarget != null)
            {
                return smartTarget;
            }
        }

        // ✅ FALLBACK: Normal nearest targeting
        // (Also used after smart targeting period ends)
        RuntimeUnit nearest = null;
        float minDistance = float.MaxValue;

        foreach (var enemy in enemies)
        {
            if (enemy == null || !enemy.IsAlive()) continue;

            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = enemy;
            }
        }

        return nearest;
    }
    // ===== NAVIGATE TO TARGET =====

    // RuntimeUnit.cs içindeki NavigateToTarget metodunu güncelle

    protected void NavigateToTarget(RuntimeUnit target)
    {
        if (target == null || !target.IsAlive())
        {
            currentTarget = null;
            if (agent != null && agent.enabled)
            {
                agent.ResetPath();
            }
            return;
        }

        // ✅ CRITICAL: Check if agent is valid and on NavMesh
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            // Try to fix position if agent exists but not on NavMesh
            if (agent != null && agent.enabled && !agent.isOnNavMesh)
            {
                Vector3 pos = transform.position;
                UnityEngine.AI.NavMeshHit hit;
                if (UnityEngine.AI.NavMesh.SamplePosition(pos, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    transform.position = hit.position;
                    agent.Warp(hit.position);
                    Debug.Log($"✅ {data.toyName}: Fixed NavMesh position");
                }
                else
                {
                    Debug.LogWarning($"⚠️ {data.toyName}: Cannot find NavMesh position, disabling navigation");
                    return;
                }
            }
            else
            {
                return;
            }
        }

        float distance = Vector3.Distance(transform.position, target.transform.position);

        if (distance > attackRange)
        {
            // ✅ Safe SetDestination call
            if (agent.isOnNavMesh && agent.enabled)
            {
                agent.SetDestination(target.transform.position);
                if (animator != null)
                {
                    animator.SetBool("Move", true); // Move stays as Bool (continuous state)
                }
            }
        }
        else
        {
            if (agent.hasPath)
            {
                agent.ResetPath();
                if (animator != null)
                {
                    animator.SetBool("Move", false); // Move stays as Bool (continuous state)
                }
            }
        }
    }
    // ===== HEALTH INTERFACE =====

    public bool IsAlive() => currentHealthValue > 0;

    public void RestoreHealth(float amount)
    {
        currentHealthValue = Mathf.Min(currentHealthValue + amount, maxHealth);
        OnHealthChanged?.Invoke(currentHealthValue, maxHealth);
    }

    // ===== DAMAGE =====

    public void TakeDamage(int damage) => TakeDamage((float)damage);

    public virtual void TakeDamage(float damage)
    {
        float actualDamage = Mathf.Max(0, damage - shieldAmount);
        currentHealthValue -= actualDamage;

        if (currentHealthValue <= 0)
        {
            currentHealthValue = 0;
            OnDeath();
        }

        OnHealthChanged?.Invoke(currentHealthValue, maxHealth);

        if (damageTextPrefab != null)
        {
            Vector3 textPos = transform.position + Vector3.up * 2f;
            EnemyDamageText damageText = Instantiate(damageTextPrefab, textPos, Quaternion.identity);
            damageText.SetTextAnimation(Mathf.CeilToInt(actualDamage).ToString());
        }

        if (!isHitFeedbackActive)
        {
            if (gameObject.activeInHierarchy)
                StartCoroutine(HitFeedbackCoroutine());
        }

        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }

        Taptic.Light();
    }

    // ===== HIT FEEDBACK =====

    private IEnumerator HitFeedbackCoroutine()
    {
        isHitFeedbackActive = true;
        float timer = 0f;

        while (timer < hitDuration)
        {
            float scalePop = 1 + Mathf.Sin(Mathf.PI * (timer / hitDuration)) * (hitScaleFactor - 1);
            transform.localScale = originalScale * scalePop;

            timer += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
        isHitFeedbackActive = false;
    }

    // ===== COMBAT =====

    public int GetFinalDamage()
    {
        return Mathf.RoundToInt(currentDamage * damageMultiplier);
    }

    public void ApplyBuff(float damageBonus, float shieldBonus)
    {
        damageMultiplier += damageBonus;
        shieldAmount += shieldBonus;
    }

    // ===== DEATH =====

    protected void OnDeath()
    {
        isInBattle = false;
        Debug.Log($"💀 {data.toyName} is dying...");

        OnUnitDeath();

        // ✅ Stop all coroutines and tweens FIRST
        StopAllCoroutines();
        transform.DOKill();
        CancelInvoke();

        // ✅ Disable agent immediately
        if (agent != null && agent.enabled)
        {
            agent.ResetPath();
            agent.enabled = false;
        }

        // ✅ CRITICAL FIX: Deactivate gameObject IMMEDIATELY
        // Bu sayede başka karakterler bu unite saldıramaz
        gameObject.SetActive(false);

        // ✅ Trigger death event
        EventManager.OnUnitDeath(this);

        // ✅ Clear from grid
        GridManager gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            gridManager.ClearSceneSlot(gridSlot, isPlayerUnit, this);
        }

        // Death VFX
        Vector3 deathPos = transform.position;
        deathPos.y += 0.5f;

        if (poolingSystem != null)
        {
            GameObject deathVfx = poolingSystem.InstantiateAPS("DeathVfx", deathPos);
            if (deathVfx != null)
            {
                container.InjectGameObject(deathVfx);
                poolingSystem.DestroyAPS(deathVfx, 1f);
            }
        }

        // ✅ CRITICAL: Destroy after small delay for VFX
        // GameObject zaten inactive, saldırı imkansız
        Destroy(gameObject, 0.1f);

        Debug.Log($"💀 {data.toyName} deactivated and scheduled for destruction");
    }
    /// <summary>
    /// Virtual method that can be overridden for special death behaviors (e.g., Golem split)
    /// Called BEFORE the unit is destroyed
    /// </summary>
    protected virtual void OnUnitDeath()
    {
        // Base implementation does nothing
        // Override in child classes for special death abilities
    }
    /// <summary>
    /// Set custom HP and Damage (used for mini golems with reduced stats)
    /// </summary>
    public void SetCustomStats(float hp, int damage)
    {
        maxHealth = hp;
        currentHealthValue = hp;
        currentDamage = damage;

        OnHealthChanged?.Invoke(currentHealthValue, maxHealth);

        Debug.Log($"✅ {data.toyName} custom stats: HP={hp}, Damage={damage}");
    }

    // ===== HP BUFF SUPPORT (for Bone Mage) =====

    /// <summary>
    /// Increase max HP and heal by the same amount (for buff abilities)
    /// </summary>
    public void IncreaseMaxHP(float amount)
    {
        maxHealth += amount;
        currentHealthValue += amount; // Also heal by buff amount

        OnHealthChanged?.Invoke(currentHealthValue, maxHealth);

        Debug.Log($"✅ {data.toyName} HP increased by {amount:F0}. New max: {maxHealth:F0}, Current: {currentHealthValue:F0}");
    }
    public void ResetBattleBuffs()
    {
        damageMultiplier = 1f;
        shieldAmount = 0f;
        hasFirstAttackCancel = false;
        poisonTicks = 0;
    }

    public void ResetPosition()
    {
        transform.localPosition = originalPosition;
        transform.localScale = originalScale;

        if (agent != null && agent.enabled)
        {
            agent.ResetPath();
            agent.enabled = false;
        }
    }

    // ===== FORMATION SUPPORT =====

    public void PrepareForFormation()
    {
        if (agent != null && agent.enabled)
        {
            agent.enabled = false;
        }
    }

    public void FormationComplete()
    {
        if (agent != null && !agent.enabled)
        {
            agent.enabled = true;
        }
    }

    // ===== GIZMOS =====

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (agent != null && agent.enabled && agent.hasPath)
        {
            Gizmos.color = Color.yellow;
            Vector3[] corners = agent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }
    }
}