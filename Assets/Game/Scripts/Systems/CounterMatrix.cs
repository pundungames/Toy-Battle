// ============================================================================
// COUNTER MATRIX - Unit counter relationships
// Uses unitID from ToyUnitData (NOT toyName)
// ============================================================================

using System.Collections.Generic;
using UnityEngine;

public class CounterMatrix
{
    // Key: Attacker unitID, Value: List of unitIDs that attacker counters
    private static Dictionary<string, List<string>> counterRelationships = new Dictionary<string, List<string>>()
    {
        // ToySoldier counters MaximusPuncher
    { "ToySoldier", new List<string> { "MaximusPuncher" } },

    // ShellNinja counters MaximusPuncher, ToySoldier, KaboomTanklet, RoboCoreMk1, BoneMage
    { "ShellNinja", new List<string> { "MaximusPuncher", "ToySoldier", "KaboomTanklet", "RoboCoreMk1", "BoneMage" } },

    // MiniBoys counters ToySoldier, ShellNinja
    { "MiniBoys", new List<string> { "ToySoldier", "ShellNinja" } },

    // SlamBros counters ToySoldier, ShellNinja
    { "SlamBros", new List<string> { "ToySoldier", "ShellNinja" } },

    // KaboomTanklet counters SlamBros, PunchyBots, MiniBoys
    { "KaboomTanklet", new List<string> { "SlamBros", "PunchyBots", "MiniBoys" } },

    // GuardianGolem counters SlamBros, BlastRacer
    { "GuardianGolem", new List<string> { "SlamBros", "BlastRacer" } },

    // BlastRacer counters KaboomTanklet, RoboCoreMk1, PunchyBots
    { "BlastRacer", new List<string> { "KaboomTanklet", "RoboCoreMk1", "PunchyBots" } },

    // MaximusPuncher counters BlastRacer
    { "MaximusPuncher", new List<string> { "BlastRacer" } },

    // BoneMage counters GuardianGolem
    { "BoneMage", new List<string> { "GuardianGolem" } },

    // PunchyBots counters nobody (counters-outgoing empty)
    { "PunchyBots", new List<string>() },

    // RoboCoreMk1 counters nobody (counters-outgoing empty)
    { "RoboCoreMk1", new List<string>() },

    // KaboomTanklet / RoboCore are Epic in your current roster, but counter logic is independent of rarity.
    };

    public static bool Counters(string attackerUnitID, string defenderUnitID)
    {
        if (counterRelationships.ContainsKey(attackerUnitID))
        {
            return counterRelationships[attackerUnitID].Contains(defenderUnitID);
        }
        return false;
    }

    public static bool IsCounteredBy(string attackerUnitID, string defenderUnitID)
    {
        return Counters(defenderUnitID, attackerUnitID);
    }

    public static List<string> GetCounteredUnits(string unitID)
    {
        if (counterRelationships.ContainsKey(unitID))
        {
            return new List<string>(counterRelationships[unitID]);
        }
        return new List<string>();
    }

    public static int CalculateCounterScore(string candidateUnitID, List<ToyUnitData> enemyUnits)
    {
        int score = 0;

        foreach (var enemyUnit in enemyUnits)
        {
            if (enemyUnit == null) continue;

            string enemyID = enemyUnit.unitID;

            if (Counters(candidateUnitID, enemyID))
            {
                score += 2;
            }
            else if (IsCounteredBy(candidateUnitID, enemyID))
            {
                score -= 2;
            }
            else
            {
                score += 1;
            }
        }

        return score;
    }

    public static int CalculateCounterScore(string candidateUnitID, List<RuntimeUnit> enemyUnits)
    {
        int score = 0;

        foreach (var enemyUnit in enemyUnits)
        {
            if (enemyUnit == null || enemyUnit.data == null) continue;

            string enemyID = enemyUnit.data.unitID;

            if (Counters(candidateUnitID, enemyID))
            {
                score += 2;
            }
            else if (IsCounteredBy(candidateUnitID, enemyID))
            {
                score -= 2;
            }
            else
            {
                score += 1;
            }
        }

        return score;
    }

    public static void PrintCounterRelationships()
    {
        Debug.Log("=== COUNTER MATRIX ===");

        foreach (var kvp in counterRelationships)
        {
            string attacker = kvp.Key;
            string targets = string.Join(", ", kvp.Value);

            Debug.Log($"{attacker} counters: {targets}");
        }
    }

    public static void PrintCounterScoreBreakdown(string candidateUnitID, List<RuntimeUnit> enemyUnits)
    {
        Debug.Log($"=== COUNTER SCORE BREAKDOWN for {candidateUnitID} ===");

        int totalScore = 0;

        foreach (var enemyUnit in enemyUnits)
        {
            if (enemyUnit == null || enemyUnit.data == null) continue;

            string enemyID = enemyUnit.data.unitID;
            int scoreChange = 0;
            string reason = "";

            if (Counters(candidateUnitID, enemyID))
            {
                scoreChange = 2;
                reason = "COUNTERS";
            }
            else if (IsCounteredBy(candidateUnitID, enemyID))
            {
                scoreChange = -2;
                reason = "COUNTERED BY";
            }
            else
            {
                scoreChange = 1;
                reason = "NEUTRAL";
            }

            totalScore += scoreChange;
            Debug.Log($"  vs {enemyID}: {scoreChange:+0;-0} ({reason}) | Running total: {totalScore}");
        }

        Debug.Log($"=== FINAL SCORE: {totalScore} ===");
    }
}