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
    [Inject] protected PoolingSystem poolingSystem;
    [Inject] protected AudioManager audioManager;

    // ===== DATA =====
    public ToyUnitData data;
    public int gridSlot;
    public bool isPlayerUnit;

    // ===== HEALTH (IHealthProvider) =====
    private float maxHealth;
    private float currentHealthValue;

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
    protected NavMeshAgent agent;

    // ===== COMBAT SETTINGS =====
    [Header("Combat Settings (Auto-loaded from ScriptableObject)")]
    [SerializeField] protected float attackRange = 2f;
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected float attackCooldown = 1f;

    [Header("Hit Feedback")]
    [SerializeField] float hitScaleFactor = 1.1f;
    [SerializeField] float hitDuration = 0.3f;

    // ===== PRIVATE STATE =====
    protected RuntimeUnit currentTarget;
    private float lastAttackTime = 0f;
    private bool isInBattle = false;
    protected bool isExecutingAttack = false; // ✅ NEW: Prevent attack spam
    private Vector3 originalPosition;
    private Vector3 originalScale;
    private bool isHitFeedbackActive = false;

    // ===== INITIALIZATION =====

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
            // Create default spawn point at unit position + up
            GameObject spawnPoint = new GameObject("ProjectileSpawnPoint");
            spawnPoint.transform.SetParent(transform);
            spawnPoint.transform.localPosition = Vector3.up * 1f;
            projectileSpawnPoint = spawnPoint.transform;
        }

        Debug.Log($"✅ {unitData.toyName} initialized: Range={attackRange}, Speed={moveSpeed}, Agent={agent != null}");
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
        agent.radius = 0.5f;
        agent.height = 2.0f;
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

    public void StopBattle()
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
    protected void LockAttack()
    {
        isExecutingAttack = true;
    }

    /// <summary>
    /// ✅ Helper: Unlock attack state
    /// Call this when attack completes
    /// </summary>
    protected void UnlockAttack()
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
        if (poolingSystem != null && !string.IsNullOrEmpty(data.unitID))
        {
            string vfxID = $"{data.unitID}_attack_vfx";
            GameObject vfx = poolingSystem.InstantiateAPS(vfxID, projectileSpawnPoint.position);
            if (vfx != null)
            {
                poolingSystem.DestroyAPS(vfx, 2f);
            }
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

    private void NavigateToTarget(RuntimeUnit target)
    {
        if (agent == null || !agent.enabled) return;

        agent.SetDestination(target.transform.position);

        bool isMoving = agent.velocity.sqrMagnitude > 0.1f;

        if (animator != null)
        {
            animator.SetBool("Move", isMoving); // Move stays as Bool (continuous state)
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

    public void TakeDamage(float damage)
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

    private void OnDeath()
    {
        isInBattle = false;
        EventManager.OnUnitDeath(this);

        if (agent != null && agent.enabled)
        {
            agent.ResetPath();
            agent.enabled = false;
        }

        GridManager gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            gridManager.ClearSceneSlot(gridSlot, isPlayerUnit, this);
            Debug.Log($"💀 {data.toyName} died");
        }

        if (animator != null)
        {
            animator.SetTrigger("Death");
        }

        if (data.isExplosive)
        {
            Debug.Log($"💥 {data.toyName} exploded!");
        }

        Destroy(gameObject, 1f);
    }

    // ===== BATTLE RESET =====

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