// ============================================================================
// SHELL NINJA UNIT - TELEPORT ASSASSIN
// ✅ Teleports to enemy backline at battle start (1s delay)
// ✅ Normal melee attack after teleport
// Stats: Damage=19, Range=2, Cooldown=1.1s, Speed=3.8, HP=40
// ============================================================================

using UnityEngine;
using DG.Tweening;
using System.Collections;
using Zenject;

public class ShellNinjaUnit : RuntimeUnit
{
    [Header("Teleport Settings")]
    [SerializeField] float teleportDelay = 1f; // Delay after battle starts
    [SerializeField] string teleportVFX = "ninja_teleport_vfx";
    [SerializeField] string teleportSFX = "ninja_teleport";

    private bool hasTeleported = false;

    // ===== BATTLE START OVERRIDE =====

    public override void Initialize(ToyUnitData unitData, int slot, bool isPlayer)
    {
        base.Initialize(unitData, slot, isPlayer);
        hasTeleported = false;
    }

    // ===== TELEPORT ON BATTLE START =====

    public override void StartBattle()
    {
        base.StartBattle();

        // Start teleport sequence after delay
        if (!hasTeleported)
        {
            StartCoroutine(TeleportSequence());
        }
    }

    private IEnumerator TeleportSequence()
    {
        // Wait for delay
        yield return new WaitForSeconds(teleportDelay);

        // Find backline enemy
        RuntimeUnit backlineTarget = FindBacklineEnemy();

        if (backlineTarget != null)
        {
            TeleportToTarget(backlineTarget);
        }
        else
        {
            Debug.LogWarning("⚠️ Shell Ninja: No backline target found");
            hasTeleported = true;
        }
    }

    // ===== FIND BACKLINE ENEMY =====

    private RuntimeUnit FindBacklineEnemy()
    {
        BattleManager battleManager = FindObjectOfType<BattleManager>();
        if (battleManager == null) return null;

        var enemies = isPlayerUnit ?
            battleManager.GetEnemyUnits() :
            battleManager.GetPlayerUnits();

        // Find enemy with highest Z (backline)
        RuntimeUnit backline = null;
        float maxZ = float.MinValue;

        foreach (var enemy in enemies)
        {
            if (enemy == null || !enemy.IsAlive()) continue;

            float z = isPlayerUnit ? enemy.transform.position.z : -enemy.transform.position.z;

            if (z > maxZ)
            {
                maxZ = z;
                backline = enemy;
            }
        }

        return backline;
    }

    // ===== TELEPORT TO TARGET =====

    private void TeleportToTarget(RuntimeUnit target)
    {
        // Calculate teleport position (behind target)
        Vector3 direction = isPlayerUnit ? Vector3.forward : Vector3.back;
        Vector3 teleportPos = target.transform.position + direction * 1.5f;

        // ✅ CRITICAL: Check if position is on NavMesh
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(teleportPos, out hit, 3f, UnityEngine.AI.NavMesh.AllAreas))
        {
            teleportPos = hit.position;
        }
        else
        {
            // Fallback: teleport to target position
            teleportPos = target.transform.position;
            Debug.LogWarning($"⚠️ Shell Ninja: Teleport position not on NavMesh, using target position");
        }

        // Disable agent during teleport
        if (agent != null && agent.enabled)
        {
            agent.enabled = false;
        }

        // Teleport VFX at origin
        if (poolingSystem != null && !string.IsNullOrEmpty(teleportVFX))
        {
            GameObject vfx = poolingSystem.InstantiateAPS(teleportVFX, transform.position);
            if (vfx != null)
            {
                poolingSystem.DestroyAPS(vfx, 2f);
            }
        }

        // Teleport with fade effect
        transform.DOScale(0f, 0.2f)
            .OnComplete(() =>
            {
                // Move to teleport position
                transform.position = teleportPos;

                // Teleport VFX at destination
                if (poolingSystem != null && !string.IsNullOrEmpty(teleportVFX))
                {
                    GameObject vfx = poolingSystem.InstantiateAPS(teleportVFX, teleportPos);
                    if (vfx != null)
                    {
                        poolingSystem.DestroyAPS(vfx, 2f);
                    }
                }

                // Fade in
                transform.DOScale(1f, 0.2f)
                    .OnComplete(() =>
                    {
                        // Re-enable agent
                        if (agent != null && !agent.enabled)
                        {
                            agent.enabled = true;
                        }

                        hasTeleported = true;
                        Debug.Log($"⚡ Shell Ninja teleported to backline!");
                    });
            });

        // Play teleport SFX
        if (audioManager != null && !string.IsNullOrEmpty(teleportSFX))
        {
            audioManager.Play(teleportSFX);
        }

        Taptic.Medium();
    }

    // ===== ATTACK (Standard Melee) =====
    // Base class ExecuteAttackEvent() handles melee damage
    // Animation event: "ExecuteAttackEvent" at hit frame

    // ===== RESET ON DEATH =====

    private void OnDestroy()
    {
        transform.DOKill();
        StopAllCoroutines();
    }
}