// ============================================================================
// GOLEM SPLIT ON DEATH - SPAWNS MINI GOLEMS
// ✅ Level 1: Spawns 2 mini golems (10% stats each)
// ✅ Level 2-3: Spawns 4 mini golems (10% stats each)
// ✅ Mini golems inherit team and position
// ✅ Called from RuntimeUnit.OnDeath()
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class GolemSplitAbility : MonoBehaviour
{
   /* [Header("Split Settings")]
    [SerializeField] float miniGolemHealthPercent = 0.1f; // 10% of original
    [SerializeField] float miniGolemDamagePercent = 0.1f; // 10% of original
    [SerializeField] float miniGolemScale = 0.5f; // 50% size
    [SerializeField] float spawnRadius = 1.5f; // How far from death position
    [SerializeField] float spawnDelay = 0.3f; // Delay before spawning (for VFX)

    [Header("Mini Golem Prefab")]
    [SerializeField] string miniGolemPrefabName = "MiniGolem"; // Pool name
    [SerializeField] GameObject miniGolemPrefab; // Fallback if not pooled

    [Header("VFX/SFX")]
    [SerializeField] string splitVFXName = "GolemSplitVFX";
    [SerializeField] string splitSFXName = "golem_split";

    [Inject] PoolingSystem poolingSystem;
    [Inject] AudioManager audioManager;
    [Inject] DiContainer container;

    private RuntimeUnit parentGolem;
    private BattleManager battleManager;
    private GridManager gridManager;

    // ===== INITIALIZE =====

    private void Awake()
    {
        parentGolem = GetComponent<RuntimeUnit>();
        battleManager = FindObjectOfType<BattleManager>();
        gridManager = FindObjectOfType<GridManager>();
    }

    // ===== TRIGGER SPLIT ON DEATH =====

    public void TriggerSplit()
    {
        if (parentGolem == null || parentGolem.data == null)
        {
            Debug.LogWarning("⚠️ GolemSplitAbility: Missing parent golem or data!");
            return;
        }

        // Check if this is actually a golem
        if (!parentGolem.data.toyName.Contains("Golem"))
        {
            Debug.LogWarning("⚠️ GolemSplitAbility: Not a golem unit!");
            return;
        }

        Debug.Log($"💥 {parentGolem.data.toyName} splitting into mini golems!");

        // Play split VFX/SFX
        PlaySplitEffects();

        // Start spawn coroutine
        StartCoroutine(SpawnMiniGolems());
    }

    // ===== SPAWN MINI GOLEMS =====

    private IEnumerator SpawnMiniGolems()
    {
        // Wait for death animation/VFX
        yield return new WaitForSeconds(spawnDelay);

        // Determine number of mini golems based on level
        int miniGolemCount = GetMiniGolemCount();

        Debug.Log($"🪨 Spawning {miniGolemCount} mini golems (Level {parentGolem.data.level})");

        // Calculate mini golem stats
        float miniHealth = parentGolem.data.baseHP * miniGolemHealthPercent;
        float miniDamage = parentGolem.data.baseDamage * miniGolemDamagePercent;

        // Get parent's death position
        Vector3 deathPosition = transform.position;

        // Spawn mini golems in circle around death position
        for (int i = 0; i < miniGolemCount; i++)
        {
            SpawnMiniGolem(i, miniGolemCount, deathPosition, miniHealth, miniDamage);
            yield return new WaitForSeconds(0.1f); // Small delay between spawns
        }

        Debug.Log($"✅ {miniGolemCount} mini golems spawned successfully!");
    }

    // ===== SPAWN SINGLE MINI GOLEM =====

    private void SpawnMiniGolem(int index, int totalCount, Vector3 centerPos, float health, float damage)
    {
        // Calculate spawn position in circle
        float angle = (360f / totalCount) * index;
        float angleRad = angle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            Mathf.Cos(angleRad) * spawnRadius,
            0f,
            Mathf.Sin(angleRad) * spawnRadius
        );

        Vector3 spawnPosition = centerPos + offset;

        // Spawn mini golem
        GameObject miniGolemObj = null;

        if (poolingSystem != null && !string.IsNullOrEmpty(miniGolemPrefabName))
        {
            // Use pooling system
            miniGolemObj = poolingSystem.InstantiateAPS(miniGolemPrefabName, spawnPosition);
        }
        else if (miniGolemPrefab != null)
        {
            // Fallback: Direct instantiate
            miniGolemObj = Instantiate(miniGolemPrefab, spawnPosition, Quaternion.identity);
        }
        else
        {
            Debug.LogError("❌ GolemSplitAbility: No mini golem prefab configured!");
            return;
        }

        // Inject dependencies
        if (container != null)
        {
            container.InjectGameObject(miniGolemObj);
        }

        // Setup mini golem
        RuntimeUnit miniGolem = miniGolemObj.GetComponent<RuntimeUnit>();
        if (miniGolem != null)
        {
            // Copy parent's data
            miniGolem.data = ScriptableObject.CreateInstance<ToyUnitData>();
            CopyGolemData(parentGolem.data, miniGolem.data);

            // Override stats (10% of original)
            miniGolem.data.maxHealthValue = health;
            miniGolem.data.attackDamage = damage;

            // Set team
            miniGolem.isPlayerUnit = parentGolem.isPlayerUnit;

            // Scale down
            miniGolem.transform.localScale = Vector3.one * miniGolemScale;

            // Initialize
            miniGolem.Initialize();

            // Add to battle (if battle manager exists)
            if (battleManager != null)
            {
                if (parentGolem.isPlayerUnit)
                {
                    battleManager.AddPlayerUnit(miniGolem);
                }
                else
                {
                    battleManager.AddEnemyUnit(miniGolem);
                }
            }

            // Find available grid slot (if grid manager exists)
            if (gridManager != null)
            {
                int availableSlot = gridManager.FindNearestAvailableSlot(spawnPosition, parentGolem.isPlayerUnit);
                if (availableSlot >= 0)
                {
                    miniGolem.SetGridSlot(availableSlot);
                    gridManager.OccupySlot(availableSlot, miniGolem, parentGolem.isPlayerUnit);
                }
            }

            Debug.Log($"🪨 Mini Golem #{index + 1} spawned: HP={health:F0}, DMG={damage:F0}");
        }
        else
        {
            Debug.LogError("❌ Mini golem missing RuntimeUnit component!");
            Destroy(miniGolemObj);
        }
    }

    // ===== DETERMINE MINI GOLEM COUNT =====

    private int GetMiniGolemCount()
    {
        int level = parentGolem.data.level;

        if (level == 1)
        {
            return 2; // Level 1: 2 mini golems
        }
        else if (level == 2 || level == 3)
        {
            return 4; // Level 2-3: 4 mini golems
        }
        else
        {
            // Safety: Default to 2 for any other level
            return 2;
        }
    }

    // ===== COPY GOLEM DATA =====

    private void CopyGolemData(ToyUnitData source, ToyUnitData target)
    {
        // Copy essential fields
        target.toyName = "Mini " + source.toyName;
        target.level = source.level;
        target.attackCooldown = source.attackCooldown;
        target.attackRange = source.attackRange;
        target.moveSpeed = source.moveSpeed;
        target.unitType = source.unitType;
        target.unitType = source.unitType;

        // Stats are overridden after this
    }

    // ===== VFX/SFX =====

    private void PlaySplitEffects()
    {
        // Play split VFX
        if (poolingSystem != null && !string.IsNullOrEmpty(splitVFXName))
        {
            Vector3 vfxPos = transform.position;
            vfxPos.y += 0.5f;
            GameObject vfx = poolingSystem.InstantiateAPS(splitVFXName, vfxPos);
            if (vfx != null)
            {
                poolingSystem.DestroyAPS(vfx, 2f);
            }
        }

        // Play split SFX
        if (audioManager != null && !string.IsNullOrEmpty(splitSFXName))
        {
            audioManager.Play(splitSFXName);
        }

        Taptic.Medium();
    }*/
}