// ============================================================================
// HEALTH UI - DISPLAYS PLAYER AND ENEMY HEALTH (HEARTS)
// ✅ Uses existing hearts in scene (don't create new ones!)
// ✅ Shows/hides hearts based on health
// ✅ Animates damage
// ============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Zenject;

public class HealthUI : MonoBehaviour
{
    [Inject] HealthSystem healthSystem;

    [Header("Player Health - Existing Hearts")]
    [SerializeField] List<GameObject> playerHeartsFull = new List<GameObject>(); // Dolu kalpler
    [SerializeField] List<GameObject> playerHeartsEmpty = new List<GameObject>(); // Boş kalpler (bg)

    [Header("Enemy Health - Existing Hearts")]
    [SerializeField] List<GameObject> enemyHeartsFull = new List<GameObject>(); // Dolu kalpler
    [SerializeField] List<GameObject> enemyHeartsEmpty = new List<GameObject>(); // Boş kalpler (bg)

    [Header("Animation")]
    [SerializeField] float heartScaleDamage = 1.3f;
    [SerializeField] float heartScaleDuration = 0.3f;

    // ===== INITIALIZATION =====

    private void Start()
    {
        // Subscribe to health changes
        healthSystem.OnHealthChanged += UpdateHealthDisplay;

        // Initial display
        UpdateHealthDisplay(healthSystem.GetPlayerHealth(), healthSystem.GetEnemyHealth());
    }

    // ===== UPDATE DISPLAY =====

    private void UpdateHealthDisplay(int playerHealth, int enemyHealth)
    {
        // Update player hearts
        for (int i = 0; i < playerHeartsFull.Count; i++)
        {
            bool shouldShow = i < playerHealth;

            if (playerHeartsFull[i].activeSelf != shouldShow)
            {
                playerHeartsFull[i].SetActive(shouldShow);

                // Animate if just lost (was active, now inactive)
                if (!shouldShow)
                {
                    AnimateHeartLoss(playerHeartsFull[i]);
                }
            }
        }

        // Update enemy hearts
        for (int i = 0; i < enemyHeartsFull.Count; i++)
        {
            bool shouldShow = i < enemyHealth;

            if (enemyHeartsFull[i].activeSelf != shouldShow)
            {
                enemyHeartsFull[i].SetActive(shouldShow);

                // Animate if just lost
                if (!shouldShow)
                {
                    AnimateHeartLoss(enemyHeartsFull[i]);
                }
            }
        }

        Debug.Log($"❤️ UI Updated: Player {playerHealth}/3, Enemy {enemyHealth}/3");
    }

    // ===== ANIMATIONS =====

    private void AnimateHeartLoss(GameObject heart)
    {
        // Scale animation before hiding
        heart.transform.DOKill();
        heart.transform.localScale = Vector3.one;

        heart.transform.DOScale(heartScaleDamage, heartScaleDuration / 2)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                heart.transform.DOScale(0f, heartScaleDuration / 2)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() =>
                    {
                        heart.SetActive(false);
                        heart.transform.localScale = Vector3.one; // Reset for next time
                    });
            });
    }

    // ===== CLEANUP =====

    private void OnDestroy()
    {
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= UpdateHealthDisplay;
        }
    }
}