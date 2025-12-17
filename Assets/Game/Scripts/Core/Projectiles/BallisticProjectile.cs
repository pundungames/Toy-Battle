// ============================================================================
// BALLISTIC PROJECTILE - NON-GUIDED MISSILE (FINAL FIXED VERSION)
// ✅ Non-tracking ballistic flight
// ✅ AOE explosion
// ✅ Pool-safe destroy
// ✅ Collider + DOTween bugları fix
// Used by: Kaboom Tanklet
// ============================================================================

using UnityEngine;
using DG.Tweening;
using Zenject;

public class BallisticProjectile : ProjectileBase
{
    [Inject] private BattleManager battleManager;

    [Header("Ballistic Settings")]
    [SerializeField] private float minArcHeight = 1f;
    [SerializeField] private float maxArcHeight = 5f;
    [SerializeField] private float maxLifetime = 10f;

    [Header("AOE Settings")]
    [SerializeField] private float fullDamageRadius = 1f;
    [SerializeField] private float partialDamageRadius = 2f;
    [SerializeField] private float partialDamageMultiplier = 0.6f;

    [Header("VFX / SFX")]
    [SerializeField] private string explosionVFX = "kaboom_explosion_vfx";
    [SerializeField] private string explosionSFX = "kaboom_boom";

    private Vector3 targetPosition;
    private Vector3 startPosition;
    private float impactDamage;
    private bool isPlayerProjectile;
    private bool hasExploded;

    private Tween flightTween;
    private Collider cachedCollider;

    // ========================================================================
    // SET TARGET
    // ========================================================================

    public void SetTarget(Vector3 targetPos, float damage, float flightTime, bool isPlayer)
    {
        cachedCollider ??= GetComponent<Collider>();
        if (cachedCollider) cachedCollider.enabled = true;

        hasExploded = false;

        targetPosition = targetPos;
        startPosition = transform.position;
        impactDamage = damage;
        isPlayerProjectile = isPlayer;

        Vector3 dir = (targetPosition - transform.position).normalized;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);

        if (trail)
        {
            trail.enabled = false;
            Invoke(nameof(EnableTrail), 0.07f);
        }

        StartBallisticFlight(flightTime);

        Invoke(nameof(ForceDestroy), maxLifetime);
    }

    private void EnableTrail()
    {
        if (trail) trail.enabled = true;
    }

    // ========================================================================
    // BALLISTIC FLIGHT
    // ========================================================================

    private void StartBallisticFlight(float flightTime)
    {
        float distance = Vector3.Distance(startPosition, targetPosition);
        float arcHeight = Mathf.Lerp(minArcHeight, maxArcHeight, Mathf.Clamp01(distance / 10f));

        flightTween = transform.DOJump(targetPosition, arcHeight, 1, flightTime)
            .SetEase(Ease.Linear)
            .OnComplete(OnImpact);

        transform.DORotate(new Vector3(0, 0, 360), flightTime, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear);
    }

    // ========================================================================
    // IMPACT
    // ========================================================================

    private void OnImpact()
    {
        if (hasExploded) return;
        hasExploded = true;

        if (cachedCollider) cachedCollider.enabled = false;

        ExplodeAtPosition(transform.position);
    }

    // ========================================================================
    // AOE EXPLOSION
    // ========================================================================

    private void ExplodeAtPosition(Vector3 center)
    {
        if (battleManager != null)
        {
            var targets = isPlayerProjectile
                ? battleManager.GetEnemyUnits()
                : battleManager.GetPlayerUnits();

            foreach (var unit in targets)
            {
                if (unit == null || !unit.IsAlive()) continue;

                float dist = Vector3.Distance(center, unit.transform.position);
                float dmg = 0f;

                if (dist <= fullDamageRadius)
                    dmg = impactDamage;
                else if (dist <= partialDamageRadius)
                    dmg = impactDamage * partialDamageMultiplier;

                if (dmg > 0)
                    unit.TakeDamage(dmg);
            }
        }

        Invoke(nameof(DestroyProjectile), 0.05f);
        PlayExplosionVFX(center);
        PlayExplosionSFX();
        Taptic.Heavy();

    }

    // ========================================================================
    // VFX / SFX
    // ========================================================================

    private void PlayExplosionVFX(Vector3 position)
    {
        if (poolingSystem == null || string.IsNullOrEmpty(explosionVFX)) return;

        GameObject vfx = poolingSystem.InstantiateAPS(explosionVFX, position);
        if (vfx == null) return;

        vfx.transform.localScale = Vector3.one * partialDamageRadius * 0.5f;

        var destroyer = vfx.GetComponent<VfxDestroyer>();
        if (destroyer != null)
            destroyer.DestroyObject(1f);
    }

    private void PlayExplosionSFX()
    {
        if (audioManager != null && !string.IsNullOrEmpty(explosionSFX))
            audioManager.Play(explosionSFX);
    }

    // ========================================================================
    // COLLISION BACKUP
    // ========================================================================

    protected override void OnTriggerEnter(Collider other)
    {
       /* if (hasExploded) return;

        if (other.TryGetComponent(out RuntimeUnit unit))
        {
            if (unit != null && unit.IsAlive())
                OnImpact();
        }*/
    }

    // ========================================================================
    // DESTROY / POOL RETURN
    // ========================================================================

    private void DestroyProjectile()
    {
        Debug.Log("DestroyProjectile");
        if (!gameObject.activeSelf) return;

        hasExploded = true;

        flightTween?.Kill();
        flightTween = null;

        transform.DOKill();
        CancelInvoke();

        if (trail) trail.enabled = false;

        gameObject.SetActive(false);

        if (poolingSystem != null)
            poolingSystem.DestroyAPS(gameObject);
        else
            Destroy(gameObject);
    }

    private void ForceDestroy()
    {
        if (!hasExploded)
            DestroyProjectile();
    }

    // ========================================================================
    // RESET ON DISABLE (POOL SAFE)
    // ========================================================================

    private void OnDisable()
    {
        hasExploded = false;

        flightTween?.Kill();
        flightTween = null;

        transform.DOKill();
        CancelInvoke();

        if (cachedCollider)
            cachedCollider.enabled = true;
    }
}
