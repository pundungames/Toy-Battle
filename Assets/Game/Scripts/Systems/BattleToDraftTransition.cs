// ============================================================================
// BATTLE TO DRAFT TRANSITION MANAGER
// ✅ Sequential unit spawning with VFX (puff puff puff!)
// ✅ Delayed draft panel opening
// ✅ Smooth, cinematic transitions
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Zenject;

public class BattleToDraftTransition : MonoBehaviour
{
    [Header("Spawn Animation Settings")]
    [SerializeField] float delayBetweenSpawns = 0.3f; // Time between each unit spawn
    [SerializeField] float spawnAnimationDuration = 0.5f; // Scale-in duration
    [SerializeField] AnimationCurve spawnScaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] float spawnStartScale = 0.3f; // Start at 30% size

    [Header("Draft Panel Settings")]
    [SerializeField] float draftPanelDelay = 1.5f; // Wait after all units spawn
    [SerializeField] float draftPanelFadeDuration = 0.5f;

    [Header("VFX/SFX")]
    [SerializeField] string spawnVFXName = "DeathVfx"; // Reuse death VFX (puff effect!)
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

        // Step 1: Spawn units sequentially with animations
        yield return StartCoroutine(SpawnUnitsSequentially(unitsToSpawn));

        // Step 2: Wait before opening draft panel
        Debug.Log($"⏳ Waiting {draftPanelDelay}s before opening draft panel...");
        yield return new WaitForSeconds(draftPanelDelay);

        // Step 3: Open draft panel with animation
        yield return StartCoroutine(OpenDraftPanel());

        // Step 4: Complete
        Debug.Log("✅ Battle to draft transition complete!");
        isTransitioning = false;
        onComplete?.Invoke();
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
        // Unit is already spawned by GridManager, just animate it
        Vector3 spawnPosition = unit.transform.position;

        // Play spawn VFX (puff effect!)
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
                Debug.Log($"✅ {unit.data.toyName} spawned with animation");
            });

        // Optional: Bounce effect
        Vector3 startPos = spawnPosition;
        startPos.y -= 0.5f;
        unit.transform.position = startPos;

        unit.transform.DOMoveY(spawnPosition.y, spawnAnimationDuration)
            .SetEase(Ease.OutBounce);
    }

    // ===== OPEN DRAFT PANEL =====

    private IEnumerator OpenDraftPanel()
    {
        Debug.Log("📋 Opening draft panel with animation...");

        // Find draft panel
        GameObject draftPanel = GameObject.Find("DraftPanel");
        if (draftPanel == null)
        {
            Debug.LogWarning("⚠️ Draft panel not found!");
            yield break;
        }

        CanvasGroup canvasGroup = draftPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = draftPanel.AddComponent<CanvasGroup>();
        }

        // Start hidden
        canvasGroup.alpha = 0f;
        draftPanel.SetActive(true);

        // Fade in
        canvasGroup.DOFade(1f, draftPanelFadeDuration)
            .SetEase(Ease.OutCubic);

        // Scale animation
        draftPanel.transform.localScale = Vector3.one * 0.8f;
        draftPanel.transform.DOScale(Vector3.one, draftPanelFadeDuration)
            .SetEase(Ease.OutBack);

        yield return new WaitForSeconds(draftPanelFadeDuration);

        Debug.Log("✅ Draft panel opened!");
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
            // Inject dependencies if needed
            if (container != null)
            {
                container.InjectGameObject(vfx);
            }

            // Auto-destroy after time
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

    public void SetSpawnDelay(float delay)
    {
        delayBetweenSpawns = Mathf.Max(0.1f, delay);
    }

    public void SetDraftPanelDelay(float delay)
    {
        draftPanelDelay = Mathf.Max(0f, delay);
    }
}