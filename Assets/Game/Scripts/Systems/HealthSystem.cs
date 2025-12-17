// ============================================================================
// HEALTH SYSTEM - FIXED VERSION WITH GAME OVER STOP
// ✅ Each side starts with 3 hearts
// ✅ Lose 1 heart when battle is lost
// ✅ Game stops when 0 hearts (no more drafts!)
// ============================================================================

using System;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] int maxHealth = 3; // 3 hearts each

    private int playerHealth;
    private int enemyHealth;
    private bool gameIsOver = false; // ✅ NEW: Track game over state

    // Events
    public event Action<int, int> OnHealthChanged; // (playerHealth, enemyHealth)
    public event Action<bool> OnGameOver; // true = player won, false = enemy won

    // ===== INITIALIZATION =====

    private void Awake()
    {
        ResetHealth();

        // Subscribe to battle end event
        EventManager.onBattleComplete += OnBattleEnd;
    }

    public void ResetHealth()
    {
        playerHealth = maxHealth;
        enemyHealth = maxHealth;
        gameIsOver = false; // ✅ Reset game over flag

        OnHealthChanged?.Invoke(playerHealth, enemyHealth);

        Debug.Log($"❤️ Health reset: Player {playerHealth}/{maxHealth}, Enemy {enemyHealth}/{maxHealth}");
    }

    // ===== BATTLE END LISTENER =====

    private void OnBattleEnd(bool playerWon)
    {
        // ✅ Don't process if game is already over
        if (gameIsOver)
        {
            Debug.LogWarning("⚠️ Game is already over, ignoring battle result");
            return;
        }

        Debug.Log($"⚔️ Battle ended! {(playerWon ? "Player" : "Enemy")} won");

        // Loser takes damage
        if (playerWon)
        {
            DamageEnemy();
        }
        else
        {
            DamagePlayer();
        }
    }

    // ===== DAMAGE SYSTEM =====

    private void DamagePlayer()
    {
        if (playerHealth <= 0 || gameIsOver) return;

        playerHealth--;
        OnHealthChanged?.Invoke(playerHealth, enemyHealth);

        Debug.Log($"💔 Player took damage! Health: {playerHealth}/{maxHealth}");

        if (playerHealth <= 0)
        {
            TriggerGameOver(false); // Enemy won
        }
    }

    private void DamageEnemy()
    {
        if (enemyHealth <= 0 || gameIsOver) return;

        enemyHealth--;
        OnHealthChanged?.Invoke(playerHealth, enemyHealth);

        Debug.Log($"💔 Enemy took damage! Health: {enemyHealth}/{maxHealth}");

        if (enemyHealth <= 0)
        {
            TriggerGameOver(true); // Player won
        }
    }

    // ===== GAME OVER =====

    private void TriggerGameOver(bool playerWon)
    {
        // ✅ Set game over flag FIRST
        gameIsOver = true;

        Debug.Log("=================================");
        if (playerWon)
        {
            Debug.Log("🎉 PLAYER WINS THE GAME!");
        }
        else
        {
            Debug.Log("💀 ENEMY WINS THE GAME!");
        }
        Debug.Log("=================================");

        // ✅ Trigger event - GameManager will handle state change
        OnGameOver?.Invoke(playerWon);
    }

    // ===== GETTERS =====

    public int GetPlayerHealth() => playerHealth;
    public int GetEnemyHealth() => enemyHealth;
    public int GetMaxHealth() => maxHealth;

    public bool IsGameOver() => gameIsOver;

    // ===== CLEANUP =====

    private void OnDestroy()
    {
        EventManager.onBattleComplete -= OnBattleEnd;
    }
}