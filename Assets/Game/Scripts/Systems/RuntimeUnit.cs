// ============================================================================
// RUNTIME UNIT - 3D VERSION
// Battle sırasında aktif olan unit instance'ı
// IHealthProvider implement ediyor (HealthBarUI ile uyumlu)
// ============================================================================

using UnityEngine;

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
    public int currentHP => Mathf.RoundToInt(currentHealthValue); // Eski kodlar için
    public GameObject visualObject => gameObject; // Eski kodlar için

    // ===== DAMAGE =====
    public int currentDamage;

    // ===== BATTLE BUFFS =====
    public float damageMultiplier = 1f;
    public float shieldAmount = 0f;
    public bool hasFirstAttackCancel = false;
    public int poisonTicks = 0;

    // ===== REFERENCES =====
    public HealthBarUI healthBar; // Prefab'da olacak
    public Transform projectileSpawnPoint; // Ranged unitler için
    public Animator animator; // 3D animator
    public EnemyDamageText damageTextPrefab; // Damage text prefab

    [Header("Hit Feedback")]
    [SerializeField] float hitScaleFactor = 1.1f;
    [SerializeField] float hitDuration = 0.3f;

    private Vector3 originalScale;
    private bool isHitFeedbackActive = false;

    // ===== INITIALIZATION =====

    public void Initialize(ToyUnitData unitData, int slot, bool isPlayer)
    {
        data = unitData;
        gridSlot = slot;
        isPlayerUnit = isPlayer;

        // HP & Damage setup
        maxHealth = unitData.GetScaledHP();
        currentHealthValue = maxHealth;
        currentDamage = unitData.GetScaledDamage();

        // Save original scale for hit feedback
        originalScale = transform.localScale;

        // Health bar setup (eğer prefab'da varsa)
        if (healthBar != null)
        {
            // HealthBarUI otomatik olarak OnHealthChanged'e subscribe olacak
            OnHealthChanged?.Invoke(currentHealthValue, maxHealth);
        }

        // Animator setup
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    // ===== HEALTH INTERFACE =====

    public bool IsAlive() => currentHealthValue > 0;

    public void RestoreHealth(float amount)
    {
        currentHealthValue = Mathf.Min(currentHealthValue + amount, maxHealth);
        OnHealthChanged?.Invoke(currentHealthValue, maxHealth);
    }

    // ===== DAMAGE (Overloads for backward compatibility) =====

    public void TakeDamage(int damage) => TakeDamage((float)damage);

    public void TakeDamage(float damage)
    {
        // Shield check
        float actualDamage = Mathf.Max(0, damage - shieldAmount);
        currentHealthValue -= actualDamage;

        if (currentHealthValue <= 0)
        {
            currentHealthValue = 0;
            OnDeath();
        }

        // Notify health bar
        OnHealthChanged?.Invoke(currentHealthValue, maxHealth);

        // ===== DAMAGE TEXT =====
        if (damageTextPrefab != null)
        {
            Vector3 textPos = transform.position + Vector3.up * 2f;
            EnemyDamageText damageText = Instantiate(damageTextPrefab, textPos, Quaternion.identity);
            damageText.SetTextAnimation(Mathf.CeilToInt(actualDamage).ToString());
        }

        // ===== HIT FEEDBACK =====
        if (!isHitFeedbackActive)
        {
            StartCoroutine(HitFeedbackCoroutine());
        }

        // Hit animation
        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }

        // Taptic feedback
        Taptic.Light();
    }

    // ===== HIT FEEDBACK COROUTINE =====

    private System.Collections.IEnumerator HitFeedbackCoroutine()
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
        EventManager.OnUnitDeath(this);

        // ✅ GridManager'a slot'u temizle (sadece scene'deki obje)
        GridManager gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            gridManager.ClearSceneSlot(gridSlot, isPlayerUnit);
            Debug.Log($"💀 {data.toyName} died - slot {gridSlot} cleared (state preserved for respawn)");
        }

        // Death animation
        if (animator != null)
        {
            animator.SetTrigger("Death");
        }

        // Explosive unit check
        if (data.isExplosive)
        {
            Debug.Log($"{data.toyName} exploded!");
            // Explosion VFX buraya
        }

        // Destroy after animation (örnek: 1 saniye)
        Destroy(gameObject, 1f);
    }

    // ===== ATTACK ANIMATION =====

    public void PlayAttackAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
    }

    // ===== BATTLE RESET =====

    public void ResetBattleBuffs()
    {
        damageMultiplier = 1f;
        shieldAmount = 0f;
        hasFirstAttackCancel = false;
        poisonTicks = 0;
    }
}