// ============================================================================
// HEALTH SYSTEM - SIMPLE VERSION
// ✅ Each side starts with 3 hearts
// ✅ Lose 1 heart when battle is lost
// ✅ Check if game over (0 hearts)
// ============================================================================

using System;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] int maxHealth = 3; // 3 hearts each

    private int playerHealth;
    private int enemyHealth;

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

        OnHealthChanged?.Invoke(playerHealth, enemyHealth);

        Debug.Log($"❤️ Health reset: Player {playerHealth}/{maxHealth}, Enemy {enemyHealth}/{maxHealth}");
    }

    // ===== BATTLE END LISTENER =====

    private void OnBattleEnd(bool playerWon)
    {
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
        if (playerHealth <= 0) return;

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
        if (enemyHealth <= 0) return;

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
        Debug.Log("=================================");
        if (playerWon)
        {
            EventManager.OnGameStateChange(GameState.Reward);
            Debug.Log("🎉 PLAYER WINS THE GAME!");
        }
        else
        {
            EventManager.OnGameStateChange(GameState.Lose);

            Debug.Log("💀 ENEMY WINS THE GAME!");
        }
        Debug.Log("=================================");

        OnGameOver?.Invoke(playerWon);

        // TODO: Show game over screen, stop drafts
        // EventManager.OnShowGameOver(playerWon);
    }

    // ===== GETTERS =====

    public int GetPlayerHealth() => playerHealth;
    public int GetEnemyHealth() => enemyHealth;
    public int GetMaxHealth() => maxHealth;

    public bool IsGameOver() => playerHealth <= 0 || enemyHealth <= 0;

    // ===== CLEANUP =====

    private void OnDestroy()
    {
        EventManager.onBattleComplete -= OnBattleEnd;
    }
}