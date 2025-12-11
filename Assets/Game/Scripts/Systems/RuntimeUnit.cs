// ============================================================================
// RUNTIME UNIT - NAVMESHAGENT SYSTEM
// ✅ Professional pathfinding with collision avoidance
// ✅ Units never overlap or phase through each other
// ✅ Smooth RTS-style movement
// ✅ Formation → Navigate to target → Attack
// ============================================================================

using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;
using System.Collections.Generic;

public class RuntimeUnit : MonoBehaviour, IHealthProvider
{
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
    private NavMeshAgent agent;

    // ===== COMBAT SETTINGS =====
    [Header("Combat Settings (Auto-loaded from ScriptableObject)")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float attackCooldown = 1f;

    [Header("Hit Feedback")]
    [SerializeField] float hitScaleFactor = 1.1f;
    [SerializeField] float hitDuration = 0.3f;

    // ===== PRIVATE STATE =====
    private RuntimeUnit currentTarget;
    private float lastAttackTime = 0f;
    private bool isInBattle = false;
    private Vector3 originalPosition;
    private Vector3 originalScale;
    private bool isHitFeedbackActive = false;

    // ===== INITIALIZATION =====

    public void Initialize(ToyUnitData unitData, int slot, bool isPlayer)
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

        Debug.Log($"✅ {unitData.toyName} initialized: Range={attackRange}, Speed={moveSpeed}, Agent={agent != null}");
    }

    // ===== NAVMESHAGENT SETUP =====

    private void SetupNavMeshAgent()
    {
        // Get or add NavMeshAgent component
        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }

        // Configure agent
        agent.speed = moveSpeed;
        agent.acceleration = moveSpeed * 4f; // Quick acceleration
        agent.angularSpeed = 360f; // Fast rotation
        agent.stoppingDistance = attackRange * 0.8f; // Stop slightly before attack range
        agent.radius = 0.5f; // Unit collision radius
        agent.height = 2.0f; // Unit height
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        // ✅ CRITICAL: Disable agent initially (formation will move units with DOTween)
        agent.enabled = false;

        Debug.Log($"🗺️ NavMeshAgent configured: speed={agent.speed}, stoppingDistance={agent.stoppingDistance}");
    }

    // ===== BATTLE CONTROL =====

    public void StartBattle()
    {
        isInBattle = true;
        lastAttackTime = Time.time;

        // ✅ Enable NavMeshAgent for battle
        if (agent != null)
        {
            agent.enabled = true;
            Debug.Log($"✅ {data.toyName} NavMeshAgent enabled for battle");
        }
    }

    public void StopBattle()
    {
        isInBattle = false;

        // Disable agent
        if (agent != null && agent.enabled)
        {
            agent.ResetPath();
            agent.enabled = false;
        }

        if (animator != null)
        {
            animator.SetBool("Move", false);
            animator.SetBool("Attack", false);
        }
    }

    // ===== UPDATE - NAVMESHAGENT MOVEMENT =====

    private void Update()
    {
        if (!isInBattle || !IsAlive()) return;

        // 1. Find or validate target
        if (currentTarget == null || !currentTarget.IsAlive())
        {
            currentTarget = FindNearestEnemy();
        }

        if (currentTarget == null)
        {
            // No target, stop agent
            if (agent != null && agent.enabled)
            {
                agent.ResetPath();
            }

            if (animator != null)
            {
                animator.SetBool("Move", false);
                animator.SetBool("Attack", false);
            }
            return;
        }

        // 2. Check distance to target
        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);

        // 3. If in range → Attack
        if (distanceToTarget <= attackRange)
        {
            // Stop moving
            if (agent != null && agent.enabled && agent.hasPath)
            {
                agent.ResetPath();
            }

            // Face target
            Vector3 lookDirection = (currentTarget.transform.position - transform.position).normalized;
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }

            // Attack animation
            if (animator != null)
            {
                animator.SetBool("Move", false);
                animator.SetBool("Attack", true);
            }

            // Attack if cooldown ready
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                AttackTarget(currentTarget);
                lastAttackTime = Time.time;
            }
        }
        // 4. If out of range → Navigate to target
        else
        {
            NavigateToTarget(currentTarget);
        }
    }

    // ===== FIND NEAREST ENEMY =====

    private RuntimeUnit FindNearestEnemy()
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

    // ===== NAVIGATE TO TARGET (NavMeshAgent) =====

    private void NavigateToTarget(RuntimeUnit target)
    {
        if (agent == null || !agent.enabled) return;

        // Set destination
        agent.SetDestination(target.transform.position);

        // Check if agent is moving
        bool isMoving = agent.velocity.sqrMagnitude > 0.1f;

        // Update animation
        if (animator != null)
        {
            animator.SetBool("Move", isMoving);
            animator.SetBool("Attack", false);
        }
    }

    // ===== ATTACK TARGET =====

    private void AttackTarget(RuntimeUnit target)
    {
        // Check first attack cancel
        if (target.hasFirstAttackCancel)
        {
            target.hasFirstAttackCancel = false;
            Debug.Log($"⚔️ {target.data.toyName} blocked first attack!");
            return;
        }

        // Deal damage
        target.TakeDamage(GetFinalDamage());

        // Taptic feedback
        Taptic.Light();
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

        // Damage text
        if (damageTextPrefab != null)
        {
            Vector3 textPos = transform.position + Vector3.up * 2f;
            EnemyDamageText damageText = Instantiate(damageTextPrefab, textPos, Quaternion.identity);
            damageText.SetTextAnimation(Mathf.CeilToInt(actualDamage).ToString());
        }

        // Hit feedback
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

    // ===== HIT FEEDBACK COROUTINE =====

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

        // Disable agent
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
            // Explosion logic
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

        // Disable agent during draft
        if (agent != null && agent.enabled)
        {
            agent.ResetPath();
            agent.enabled = false;
        }
    }

    // ===== FORMATION SUPPORT =====

    /// <summary>
    /// Called before formation animation starts
    /// Disables agent so DOTween can move the unit
    /// </summary>
    public void PrepareForFormation()
    {
        if (agent != null && agent.enabled)
        {
            agent.enabled = false;
        }
    }

    /// <summary>
    /// Called after formation animation completes
    /// Enables agent for battle
    /// </summary>
    public void FormationComplete()
    {
        if (agent != null && !agent.enabled)
        {
            agent.enabled = true;
        }
    }

    // ===== GIZMOS (Debug) =====

    private void OnDrawGizmosSelected()
    {
        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Agent path
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