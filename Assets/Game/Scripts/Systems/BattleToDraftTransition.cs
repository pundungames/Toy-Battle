// ============================================================================
// BATTLE TO DRAFT TRANSITION - OPTIMIZED
// ✅ Shorter waits, faster transition
// ✅ No camera changes (you'll handle that)
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Zenject;

public class BattleToDraftTransition : MonoBehaviour
{
    [Header("Spawn Animation Settings")]
    [SerializeField] float delayBetweenSpawns = 0.15f; // ✅ Faster (was 0.3f)
    [SerializeField] float spawnAnimationDuration = 0.4f; // ✅ Faster (was 0.5f)
    [SerializeField] float waitBeforeSpawn = 0.2f; // ✅ Shorter (was 0.5f)
    [SerializeField] AnimationCurve spawnScaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] float spawnStartScale = 0.3f;

    [Header("Draft Panel Settings")]
    [SerializeField] float draftPanelDelay = 0.3f; // ✅ Shorter (was 0.5f)

    [Header("VFX/SFX")]
    [SerializeField] string spawnVFXName = "DeathVfx";
    [SerializeField] string spawnSFXName = "unit_spawn";
    [SerializeField] float vfxOffsetY = 0.5f;

    [Inject] PoolingSystem poolingSystem;
    [Inject] AudioManager audioManager;
    [Inject] DiContainer container;

    private bool isTransitioning = false;

    // ===== TRIGGER TRANSITION =====

    public void StartTransition(List<RuntimeUnit> unitsToSpawn, System.Action onComplete = null)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("⚠️ Transition already in progress!");
            return;
        }

        Debug.Log($"🎬 Starting battle to draft transition with {unitsToSpawn.Count} units");
        StartCoroutine(TransitionSequence(unitsToSpawn, onComplete));
    }

    // ===== TRANSITION SEQUENCE =====

    private IEnumerator TransitionSequence(List<RuntimeUnit> unitsToSpawn, System.Action onComplete)
    {
        isTransitioning = true;

        yield return new WaitForSeconds(draftPanelDelay);
        onComplete?.Invoke();

        yield return new WaitForSeconds(waitBeforeSpawn);

        yield return StartCoroutine(SpawnUnitsSequentially(unitsToSpawn));
        isTransitioning = false;
    }

    // ===== SPAWN UNITS SEQUENTIALLY =====

    private IEnumerator SpawnUnitsSequentially(List<RuntimeUnit> units)
    {
        Debug.Log($"🎭 Animating {units.Count} units sequentially...");

        // First, hide all units
        foreach (var unit in units)
        {
            if (unit != null)
            {
                unit.gameObject.SetActive(false);
            }
        }

        // Then animate them one by one
        foreach (var unit in units)
        {
            if (unit == null) continue;

            // Spawn with animation
            SpawnUnitWithAnimation(unit);

            // Wait before next spawn
            yield return new WaitForSeconds(delayBetweenSpawns);
        }

        Debug.Log("✅ All units animated!");
    }

    // ===== SPAWN SINGLE UNIT WITH ANIMATION =====

    private void SpawnUnitWithAnimation(RuntimeUnit unit)
    {
        Vector3 spawnPosition = unit.transform.position;

        // Play spawn VFX
        PlaySpawnVFX(spawnPosition);

        // Play spawn SFX
        PlaySpawnSFX();

        // Start unit small
        unit.transform.localScale = Vector3.one * spawnStartScale;
        unit.gameObject.SetActive(true);

        // Animate scale-in
        unit.transform.DOScale(Vector3.one, spawnAnimationDuration)
            .SetEase(spawnScaleCurve)
            .OnComplete(() =>
            {
                Debug.Log($"✅ {unit.data.toyName} spawned");
            });
        // Bounce effect
        Vector3 startPos = spawnPosition;
        startPos.y -= 0.5f;
        unit.transform.position = startPos;

        unit.transform.DOMoveY(spawnPosition.y, spawnAnimationDuration)
            .SetEase(Ease.OutBounce);
    }

    // ===== VFX/SFX =====

    private void PlaySpawnVFX(Vector3 position)
    {
        if (poolingSystem == null || string.IsNullOrEmpty(spawnVFXName)) return;

        Vector3 vfxPos = position;
        vfxPos.y += vfxOffsetY;

        GameObject vfx = poolingSystem.InstantiateAPS(spawnVFXName, vfxPos);
        if (vfx != null)
        {
            if (container != null)
            {
                container.InjectGameObject(vfx);
            }

            poolingSystem.DestroyAPS(vfx, 1f);
        }
    }

    private void PlaySpawnSFX()
    {
        if (audioManager == null || string.IsNullOrEmpty(spawnSFXName)) return;

        audioManager.Play(spawnSFXName);
    }

    // ===== PUBLIC HELPERS =====

    public bool IsTransitioning => isTransitioning;
}