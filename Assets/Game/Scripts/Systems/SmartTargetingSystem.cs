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

    private FormationRow GetUnitRow(RuntimeUnit unit, bool isPlayerUnit)
    {
        float zPos = unit.transform.position.z;

        BattleManager battleManager = FindObjectOfType<BattleManager>();
        if (battleManager == null) return FormationRow.Front;

        List<RuntimeUnit> allUnits = isPlayerUnit ?
            battleManager.GetPlayerUnits() :
            battleManager.GetEnemyUnits();

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

        // Player moves Z+, Enemy moves Z-
        if (isPlayerUnit)
        {
            // Player: Front = higher Z (moving toward enemy)
            return zPos > avgZ + frontRowZThreshold ? FormationRow.Front : FormationRow.Back;
        }
        else
        {
            // Enemy: Front = lower Z (moving toward player)
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

    private RuntimeUnit SelectSmartTarget(RuntimeUnit attacker, List<RuntimeUnit> possibleTargets)
    {
        // Check cooldown
        if (lastSwitchTime.ContainsKey(attacker))
        {
            float timeSinceSwitch = Time.time - lastSwitchTime[attacker];
            if (timeSinceSwitch < targetSwitchCooldown)
            {
                return null;
            }
        }

        bool isAssassin = IsAssassin(attacker);

        // ✅ STEP 1: Filter to front row only (unless assassin or no front row exists)
        List<RuntimeUnit> frontRowTargets = new List<RuntimeUnit>();
        List<RuntimeUnit> backRowTargets = new List<RuntimeUnit>();

        foreach (var target in possibleTargets)
        {
            if (target == null || !target.IsAlive()) continue;

            FormationRow targetRow = GetUnitRow(target, target.isPlayerUnit);
            if (targetRow == FormationRow.Front)
            {
                frontRowTargets.Add(target);
            }
            else
            {
                backRowTargets.Add(target);
            }
        }

        // Choose which pool to target
        List<RuntimeUnit> targetPool;
        if (frontRowTargets.Count > 0 && !isAssassin)
        {
            // Normal units: MUST attack front row if exists
            targetPool = frontRowTargets;
            Debug.Log($"🎯 {attacker.data.toyName}: {frontRowTargets.Count} front row targets available");
        }
        else if (backRowTargets.Count > 0 && (isAssassin || frontRowTargets.Count == 0))
        {
            // Assassin can choose back row, OR no front row left
            targetPool = backRowTargets;
            if (isAssassin)
                Debug.Log($"🗡️ {attacker.data.toyName} (Assassin): Can target {backRowTargets.Count} back row enemies");
            else
                Debug.Log($"🎯 {attacker.data.toyName}: No front row left, targeting {backRowTargets.Count} back row enemies");
        }
        else
        {
            // No valid targets
            return null;
        }

        // ✅ STEP 2: Find nearest target (baseline)
        RuntimeUnit nearestTarget = null;
        float nearestDistance = float.MaxValue;

        foreach (var target in targetPool)
        {
            float distance = Vector3.Distance(attacker.transform.position, target.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestTarget = target;
            }
        }

        if (nearestTarget == null) return null;

        // ✅ STEP 3: Check if there are other targets CLOSE on X axis (horizontal proximity)
        List<RuntimeUnit> proximateTargets = new List<RuntimeUnit> { nearestTarget };

        foreach (var target in targetPool)
        {
            if (target == nearestTarget) continue;

            // ✅ Check HORIZONTAL distance (X axis only!)
            float xDistance = Mathf.Abs(target.transform.position.x - nearestTarget.transform.position.x);

            if (xDistance <= horizontalProximityThreshold)
            {
                proximateTargets.Add(target);
                if (debugProximityChecks)
                {
                    Debug.Log($"📏 {target.data.toyName} is close to {nearestTarget.data.toyName} (X distance: {xDistance:F1}m ≤ {horizontalProximityThreshold}m)");
                }
            }
            else
            {
                if (debugProximityChecks)
                {
                    Debug.Log($"📏 {target.data.toyName} is FAR from {nearestTarget.data.toyName} (X distance: {xDistance:F1}m > {horizontalProximityThreshold}m) - SKIP DISTRIBUTION");
                }
            }
        }

        // ✅ STEP 4: Decide distribution strategy
        RuntimeUnit finalTarget;

        if (proximateTargets.Count == 1)
        {
            // Only one target nearby, everyone attacks it!
            finalTarget = nearestTarget;
            Debug.Log($"🎯 {attacker.data.toyName}: Only 1 target available → {finalTarget.data.toyName}");
        }
        else
        {
            // Multiple targets close on X axis, distribute attacks!
            Debug.Log($"🎯 {attacker.data.toyName}: {proximateTargets.Count} targets close on X axis, distributing...");

            // Build scores for proximate targets only
            List<TargetScore> targetScores = new List<TargetScore>();

            foreach (var target in proximateTargets)
            {
                float distance = Vector3.Distance(attacker.transform.position, target.transform.position);
                int attackerCount = GetAttackerCount(target);

                // Score: Prioritize fewer attackers, then closer distance
                float score = (attackerCount * 100f) + distance;

                targetScores.Add(new TargetScore
                {
                    target = target,
                    distance = distance,
                    attackerCount = attackerCount,
                    score = score
                });
            }

            // Sort by score
            targetScores.Sort((a, b) => a.score.CompareTo(b.score));

            // Pick best target (fewest attackers)
            finalTarget = null;
            foreach (var targetScore in targetScores)
            {
                if (targetScore.attackerCount < maxAttackersPerTarget)
                {
                    finalTarget = targetScore.target;
                    Debug.Log($"✅ Selected {finalTarget.data.toyName} (attackers: {targetScore.attackerCount}, score: {targetScore.score:F1})");
                    break;
                }
            }

            // If all saturated, pick best score anyway
            if (finalTarget == null)
            {
                finalTarget = targetScores[0].target;
                Debug.Log($"⚠️ All targets saturated, picking best: {finalTarget.data.toyName}");
            }
        }

        // ✅ STEP 5: Register attacker and update
        RegisterAttacker(attacker, finalTarget);
        lastSwitchTime[attacker] = Time.time;

        Debug.Log($"🎯 FINAL: {attacker.data.toyName} → {finalTarget.data.toyName} (Distance: {Vector3.Distance(attacker.transform.position, finalTarget.transform.position):F1}m, Attackers: {GetAttackerCount(finalTarget)})");

        return finalTarget;
    }

    // ===== NORMAL TARGET (After 15 seconds) =====

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

        bool useSmartTargeting = (Time.time - battleStartTime) < smartTargetingDuration;

        BattleManager battleManager = FindObjectOfType<BattleManager>();
        if (battleManager == null) return;

        // Draw player formation
        var playerUnits = battleManager.GetPlayerUnits();
        DrawFormationGizmos(playerUnits, true);

        // Draw enemy formation
        var enemyUnits = battleManager.GetEnemyUnits();
        DrawFormationGizmos(enemyUnits, false);

        // Draw proximity zones (X axis)
        DrawProximityZones(enemyUnits);

#if UNITY_EDITOR
        Vector3 labelPos = new Vector3(0, 12, 0);
        string label = useSmartTargeting ?
            $"🎯 PROXIMITY TARGETING ({smartTargetingDuration - (Time.time - battleStartTime):F1}s) | X Threshold: {horizontalProximityThreshold}m" :
            "🎯 Normal Targeting";

        UnityEditor.Handles.Label(labelPos, label);
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