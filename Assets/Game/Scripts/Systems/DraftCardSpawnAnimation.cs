// ============================================================================
// DRAFT CARD SPAWN ANIMATION
// ✅ Card selected → Spawns in slot with scale animation
// ✅ Smooth scale-in with bounce
// ✅ VFX and SFX on spawn
// ============================================================================

using System.Collections;
using UnityEngine;
using DG.Tweening;
using Zenject;

public class DraftCardSpawnAnimation : MonoBehaviour
{
    [Header("Spawn Animation Settings")]
    [SerializeField] float spawnAnimationDuration = 0.6f;
    [SerializeField] float startScale = 0.2f; // Start at 20% size
    [SerializeField] AnimationCurve spawnScaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] float bounceHeight = 0.5f; // How high to bounce
    [SerializeField] int bounceCount = 1; // Number of bounces

    [Header("VFX/SFX")]
    [SerializeField] string spawnVFXName = "CardSelectVFX";
    [SerializeField] string spawnSFXName = "card_spawn";
    [SerializeField] float vfxOffsetY = 0.5f;

    [Inject] PoolingSystem poolingSystem;
    [Inject] AudioManager audioManager;
    [Inject] DiContainer container;

    // ===== SPAWN UNIT WITH ANIMATION =====

    public void SpawnUnitInSlot(RuntimeUnit unit, System.Action onComplete = null)
    {
        if (unit == null)
        {
            Debug.LogWarning("⚠️ DraftCardSpawnAnimation: Unit is null!");
            return;
        }

        Debug.Log($"🎴 Spawning {unit.data.toyName} with animation");

        StartCoroutine(SpawnSequence(unit, onComplete));
    }

    // ===== SPAWN SEQUENCE =====

    private IEnumerator SpawnSequence(RuntimeUnit unit, System.Action onComplete)
    {
        // Unit is already spawned by GridManager, just animate it!
        Vector3 targetPosition = unit.transform.position;
        // Step 1: Start invisible and small
        unit.transform.localScale = Vector3.one * startScale;
        unit.gameObject.SetActive(true);

        // Step 2: Play spawn VFX
        PlaySpawnVFX(targetPosition);

        // Step 3: Play spawn SFX
        PlaySpawnSFX();

        // Step 4: Scale animation with bounce
        Sequence spawnSequence = DOTween.Sequence();

        // Scale up
        spawnSequence.Append(
            unit.transform.DOScale(Vector3.one, spawnAnimationDuration)
                .SetEase(Ease.OutBack)
        );

        // Bounce up and down
        Vector3 startPos = targetPosition;
        Vector3 bouncePos = targetPosition + Vector3.up * bounceHeight;

        spawnSequence.Join(
            unit.transform.DOMove(bouncePos, spawnAnimationDuration * 0.5f)
                .SetEase(Ease.OutQuad)
        );

        spawnSequence.Append(
            unit.transform.DOMove(startPos, spawnAnimationDuration * 0.5f)
                .SetEase(Ease.InQuad)
        );

        // Step 6: Wait for animation
        yield return spawnSequence.WaitForCompletion();

        // Step 7: Haptic
        Taptic.Light();

        Debug.Log($"✅ {unit.data.toyName} spawn animation complete!");

        // Step 8: Callback
        onComplete?.Invoke();
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

            poolingSystem.DestroyAPS(vfx, 2f);
        }
    }

    private void PlaySpawnSFX()
    {
        if (audioManager == null || string.IsNullOrEmpty(spawnSFXName)) return;

        audioManager.Play(spawnSFXName);
    }

    // ===== INSTANT SPAWN (NO ANIMATION) =====

    public void SpawnUnitInstant(RuntimeUnit unit, Vector3 slotPosition)
    {
        if (unit == null) return;

        unit.transform.position = slotPosition;
        unit.transform.localScale = Vector3.one;
        unit.gameObject.SetActive(true);
    }
}