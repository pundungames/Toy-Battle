// ============================================================================
// SMART TARGETING SYSTEM - PROXIMITY-BASED DISTRIBUTION
// ✅ Always prioritize front row first (Z axis)
// ✅ Only distribute attacks if targets are close on X axis (2-3 units)
// ✅ If targets far apart on X: everyone attacks nearest
// ✅ Exception: Assassins can bypass front row
// ============================================================================

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SmartTargetingSystem : MonoBehaviour
{
    [Header("Smart Targeting Settings")]
    [SerializeField] float smartTargetingDuration = 15f; // First 15 seconds
    [SerializeField] int maxAttackersPerTarget = 2; // Max 2 units per enemy during smart phase
    [SerializeField] float targetSwitchCooldown = 0.5f; // Prevent rapid switching

    [Header("Proximity Settings")]
    [SerializeField] float horizontalProximityThreshold = 3f; // ✅ If targets within 3m on X axis, distribute attacks
    [SerializeField] bool debugProximityChecks = true; // Show proximity debug logs

    [Header("Formation Settings")]
    [SerializeField] float frontRowZThreshold = 2f; // Z position difference to consider front/back row
    [SerializeField] float backRowPenalty = 1000f; // HUGE penalty for attacking back row!

    [Header("Special Unit Types")]
    [SerializeField] List<string> assassinUnitNames = new List<string> { "Assassin", "Ninja", "Shadow" }; // Can bypass front row

    private static SmartTargetingSystem instance;
    private float battleStartTime = 0f;
    private bool isBattleActive = false;

    // Track how many units are attacking each enemy
    private Dictionary<RuntimeUnit, int> targetAttackerCount = new Dictionary<RuntimeUnit, int>();

    // Track last target switch time for each unit
    private Dictionary<RuntimeUnit, float> lastSwitchTime = new Dictionary<RuntimeUnit, float>();

    // ===== SINGLETON =====

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static SmartTargetingSystem Instance => instance;

    // ===== BATTLE START/END =====

    private void OnEnable()
    {
        EventManager.onBattleStart += OnBattleStart;
        EventManager.onBattleComplete += OnBattleComplete;
        EventManager.onUnitDeath += OnUnitDeath;
    }

    private void OnDisable()
    {
        EventManager.onBattleStart -= OnBattleStart;
        EventManager.onBattleComplete -= OnBattleComplete;
        EventManager.onUnitDeath -= OnUnitDeath;
    }

    private void OnBattleStart()
    {
        battleStartTime = Time.time;
        isBattleActive = true;
        targetAttackerCount.Clear();
        lastSwitchTime.Clear();

        Debug.Log("🎯 Smart Targeting System: ACTIVE - Front row priority + Proximity distribution!");
    }

    private void OnBattleComplete(bool playerWon)
    {
        isBattleActive = false;
        targetAttackerCount.Clear();
        lastSwitchTime.Clear();

        Debug.Log("🎯 Smart Targeting System: DEACTIVATED");
    }

    private void OnUnitDeath(RuntimeUnit deadUnit)
    {
        // Remove dead unit from attacker counts
        if (targetAttackerCount.ContainsKey(deadUnit))
        {
            targetAttackerCount.Remove(deadUnit);
        }
    }

    // ===== SMART TARGET SELECTION =====

    public RuntimeUnit SelectTarget(RuntimeUnit attacker, List<RuntimeUnit> possibleTargets)
    {
        if (possibleTargets == null || possibleTargets.Count == 0)
        {
            return null;
        }

        // ✅ Check if smart targeting is active
        bool useSmartTargeting = isBattleActive &&
                                  (Time.time - battleStartTime) < smartTargetingDuration;

        if (useSmartTargeting)
        {
            return SelectSmartTarget(attacker, possibleTargets);
        }
        else
        {
            return SelectNearestTarget(attacker, possibleTargets);
        }
    }

    // ===== FORMATION ROW DETECTION =====

    private enum FormationRow
    {
        Front,  // Closer to enemy (priority target!)
        Back    // Further from enemy (only target if front dead)
    }

    // ============================================================================
    // FIXED: GetUnitRow - Correct Z-axis logic
    // ============================================================================

    private FormationRow GetUnitRow(RuntimeUnit unit, bool isPlayerUnit)
    {
        float zPos = unit.transform.position.z;

        BattleManager battleManager = FindObjectOfType<BattleManager>();
        if (battleManager == null) return FormationRow.Front;

        List<RuntimeUnit> allUnits = isPlayerUnit ?
            battleManager.GetPlayerUnits() :
            battleManager.GetEnemyUnits();

        // Calculate average Z
        float avgZ = 0f;
        int count = 0;
        foreach (var u in allUnits)
        {
            if (u != null && u.IsAlive())
            {
                avgZ += u.transform.position.z;
                count++;
            }
        }

        if (count == 0) return FormationRow.Front;
        avgZ /= count;

        if (isPlayerUnit)
        {
            // ✅ Player: Front row = HIGHER Z (moving toward enemy/north)
            // If unit's Z > average Z → it's in front
            return zPos > avgZ + frontRowZThreshold ? FormationRow.Front : FormationRow.Back;
        }
        else
        {
            // ✅ Enemy: Front row = LOWER Z (moving toward player/south)
            // If unit's Z < average Z → it's in front
            return zPos < avgZ - frontRowZThreshold ? FormationRow.Front : FormationRow.Back;
        }
    }
    private bool IsAssassin(RuntimeUnit unit)
    {
        if (unit == null || unit.data == null) return false;

        string unitName = unit.data.toyName;
        foreach (string assassinName in assassinUnitNames)
        {
            if (unitName.Contains(assassinName))
            {
                return true;
            }
        }
        return false;
    }

    // ===== SMART TARGET (First 15 seconds) =====

    // SmartTargetingSystem.cs - SelectSmartTarget() metodunu basitleştir

    private RuntimeUnit SelectSmartTarget(RuntimeUnit attacker, List<RuntimeUnit> possibleTargets)
    {
        if (possibleTargets.Count == 0) return null;

        // ✅ STEP 1: Front row filtering
        List<RuntimeUnit> frontRowTargets = GetFrontRowTargets(possibleTargets);

        // ✅ Use front row if available (unless assassin)
        bool isAssassin = IsAssassin(attacker);
        List<RuntimeUnit> targetPool = (frontRowTargets.Count > 0 && !isAssassin) ?
            frontRowTargets : possibleTargets;

        if (targetPool.Count == 0) return null;

        // ✅ STEP 2: Find nearest target
        RuntimeUnit nearestTarget = null;
        float nearestDistance = float.MaxValue;

        foreach (var target in targetPool)
        {
            if (target == null || !target.IsAlive()) continue;

            float distance = Vector3.Distance(attacker.transform.position, target.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestTarget = target;
            }
        }

        if (nearestTarget == null) return null;

        // ✅ STEP 3: Simple load balancing
        // Count how many units already attacking this target
        int attackersOnNearest = GetAttackerCount(nearestTarget);

        // ✅ If target is overloaded, try to find alternative
        if (attackersOnNearest >= maxAttackersPerTarget && targetPool.Count > 1)
        {
            // Find least targeted enemy
            RuntimeUnit alternativeTarget = null;
            int minAttackers = int.MaxValue;

            foreach (var target in targetPool)
            {
                if (target == nearestTarget) continue; // Skip current nearest
                if (target == null || !target.IsAlive()) continue;

                int attackerCount = GetAttackerCount(target);
                if (attackerCount < minAttackers)
                {
                    minAttackers = attackerCount;
                    alternativeTarget = target;
                }
            }

            // Use alternative if found and less loaded
            if (alternativeTarget != null && minAttackers < attackersOnNearest)
            {
                nearestTarget = alternativeTarget;
                Debug.Log($"🎯 {attacker.data.toyName}: Switching to less targeted enemy (attackers: {minAttackers} vs {attackersOnNearest})");
            }
        }

        // ✅ Register attacker
        RegisterAttacker(attacker, nearestTarget);

        Debug.Log($"🎯 {attacker.data.toyName} → {nearestTarget.data.toyName} (Distance: {nearestDistance:F1}m, Attackers: {GetAttackerCount(nearestTarget)})");

        return nearestTarget;
    }

    // ✅ Helper: Get front row targets only
    // ============================================================================
    // FIXED: GetFrontRowTargets - Correct Z-axis logic
    // ============================================================================

    private List<RuntimeUnit> GetFrontRowTargets(List<RuntimeUnit> allTargets)
    {
        if (allTargets.Count == 0) return new List<RuntimeUnit>();

        // ✅ Determine which team we're targeting
        bool targetingPlayers = allTargets[0].isPlayerUnit;

        if (targetingPlayers)
        {
            // ✅ Targeting PLAYER units → front row has HIGHEST Z
            float maxZ = float.MinValue;

            foreach (var target in allTargets)
            {
                if (target != null && target.IsAlive())
                {
                    float z = target.transform.position.z;
                    if (z > maxZ) maxZ = z;
                }
            }

            // Get all units within threshold of maxZ
            List<RuntimeUnit> frontRow = new List<RuntimeUnit>();
            foreach (var target in allTargets)
            {
                if (target == null || !target.IsAlive()) continue;

                float z = target.transform.position.z;
                if (z >= maxZ - frontRowZThreshold) // Close to maxZ
                {
                    frontRow.Add(target);
                }
            }

            return frontRow;
        }
        else
        {
            // ✅ Targeting ENEMY units → front row has LOWEST Z
            float minZ = float.MaxValue;

            foreach (var target in allTargets)
            {
                if (target != null && target.IsAlive())
                {
                    float z = target.transform.position.z;
                    if (z < minZ) minZ = z;
                }
            }

            // Get all units within threshold of minZ
            List<RuntimeUnit> frontRow = new List<RuntimeUnit>();
            foreach (var target in allTargets)
            {
                if (target == null || !target.IsAlive()) continue;

                float z = target.transform.position.z;
                if (z <= minZ + frontRowZThreshold) // Close to minZ
                {
                    frontRow.Add(target);
                }
            }

            return frontRow;
        }
    }    // ===== NORMAL TARGET (After 15 seconds) =====

    private RuntimeUnit SelectNearestTarget(RuntimeUnit attacker, List<RuntimeUnit> possibleTargets)
    {
        RuntimeUnit nearest = null;
        float minDistance = float.MaxValue;

        foreach (var target in possibleTargets)
        {
            if (target == null || !target.IsAlive()) continue;

            float distance = Vector3.Distance(attacker.transform.position, target.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = target;
            }
        }

        return nearest;
    }

    // ===== ATTACKER COUNT TRACKING =====

    private int GetAttackerCount(RuntimeUnit target)
    {
        if (targetAttackerCount.ContainsKey(target))
        {
            return targetAttackerCount[target];
        }
        return 0;
    }

    private void RegisterAttacker(RuntimeUnit attacker, RuntimeUnit target)
    {
        if (!targetAttackerCount.ContainsKey(target))
        {
            targetAttackerCount[target] = 0;
        }

        targetAttackerCount[target]++;
    }

    // ===== HELPER CLASS =====

    private class TargetScore
    {
        public RuntimeUnit target;
        public float distance;
        public int attackerCount;
        public float score;
    }

    // ===== DEBUG =====

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !isBattleActive) return;

        BattleManager battleManager = FindObjectOfType<BattleManager>();
        if (battleManager == null) return;

        // ✅ Draw player front row line (highest Z)
        var playerUnits = battleManager.GetPlayerUnits();
        if (playerUnits.Count > 0)
        {
            float maxZ = float.MinValue;
            foreach (var unit in playerUnits)
            {
                if (unit != null && unit.IsAlive())
                {
                    float z = unit.transform.position.z;
                    if (z > maxZ) maxZ = z;
                }
            }

            // Draw green line at front
            Gizmos.color = Color.green;
            Gizmos.DrawLine(new Vector3(-10, 0.5f, maxZ), new Vector3(10, 0.5f, maxZ));

            // Draw yellow line at threshold
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(new Vector3(-10, 0.5f, maxZ - frontRowZThreshold),
                           new Vector3(10, 0.5f, maxZ - frontRowZThreshold));
        }

        // ✅ Draw enemy front row line (lowest Z)
        var enemyUnits = battleManager.GetEnemyUnits();
        if (enemyUnits.Count > 0)
        {
            float minZ = float.MaxValue;
            foreach (var unit in enemyUnits)
            {
                if (unit != null && unit.IsAlive())
                {
                    float z = unit.transform.position.z;
                    if (z < minZ) minZ = z;
                }
            }

            // Draw red line at front
            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(-10, 0.5f, minZ), new Vector3(10, 0.5f, minZ));

            // Draw orange line at threshold
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawLine(new Vector3(-10, 0.5f, minZ + frontRowZThreshold),
                           new Vector3(10, 0.5f, minZ + frontRowZThreshold));
        }

        // Draw labels
#if UNITY_EDITOR
        var playerFront = playerUnits.Count > 0 ?
            playerUnits.Max(u => u?.transform.position.z ?? float.MinValue) : 0;
        var enemyFront = enemyUnits.Count > 0 ?
            enemyUnits.Min(u => u?.transform.position.z ?? float.MaxValue) : 0;

        UnityEditor.Handles.Label(new Vector3(0, 2, playerFront),
            $"PLAYER FRONT (Z={playerFront:F1})");
        UnityEditor.Handles.Label(new Vector3(0, 2, enemyFront),
            $"ENEMY FRONT (Z={enemyFront:F1})");
#endif
    }
    private void DrawFormationGizmos(List<RuntimeUnit> units, bool isPlayerUnits)
    {
        if (units == null || units.Count == 0) return;

        foreach (var unit in units)
        {
            if (unit == null || !unit.IsAlive()) continue;

            FormationRow row = GetUnitRow(unit, isPlayerUnits);
            bool isAssassin = IsAssassin(unit);

            Color rowColor = Color.green;
            if (row == FormationRow.Back)
            {
                rowColor = Color.yellow;
            }
            if (isAssassin)
            {
                rowColor = new Color(0.5f, 0f, 1f); // Purple
            }

            Gizmos.color = rowColor;
            Gizmos.DrawWireSphere(unit.transform.position + Vector3.up * 2f, 0.3f);
        }
    }

    private void DrawProximityZones(List<RuntimeUnit> units)
    {
        if (units == null || units.Count == 0) return;

        // Draw horizontal proximity lines between close enemies
        for (int i = 0; i < units.Count; i++)
        {
            for (int j = i + 1; j < units.Count; j++)
            {
                if (units[i] == null || !units[i].IsAlive()) continue;
                if (units[j] == null || !units[j].IsAlive()) continue;

                float xDistance = Mathf.Abs(units[i].transform.position.x - units[j].transform.position.x);

                if (xDistance <= horizontalProximityThreshold)
                {
                    // Draw green line (within proximity)
                    Gizmos.color = Color.green;
                    Vector3 pos1 = units[i].transform.position + Vector3.up * 0.5f;
                    Vector3 pos2 = units[j].transform.position + Vector3.up * 0.5f;
                    Gizmos.DrawLine(pos1, pos2);
                }
            }
        }
    }
}