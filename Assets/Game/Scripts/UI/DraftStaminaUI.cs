// ============================================================================
// DRAFT STAMINA UI - Displays stamina bar and counter
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Zenject;

public class DraftStaminaUI : MonoBehaviour
{
    [Inject] DraftCardManager cardManager;
    [Header("UI Elements")]
    [SerializeField] TextMeshProUGUI staminaText;
    [SerializeField] Image staminaFillBar;
    [SerializeField] GameObject lowStaminaWarning;

    [Header("Visual Settings")]
    [SerializeField] Color fullColor = new Color(0.2f, 0.8f, 0.3f); // Green
    [SerializeField] Color midColor = new Color(1f, 0.8f, 0f); // Yellow
    [SerializeField] Color lowColor = new Color(1f, 0.2f, 0.2f); // Red
    [SerializeField] float lowStaminaThreshold = 0.3f; // 30%

    [Header("Animation")]
    [SerializeField] float animationDuration = 0.3f;

    private void OnEnable()
    {
        RefreshDisplay(cardManager);
        DraftCardManager.OnStaminaChanged += UpdateStaminaDisplay;
    }

    private void OnDisable()
    {
        DraftCardManager.OnStaminaChanged -= UpdateStaminaDisplay;
    }

    private void UpdateStaminaDisplay(int current, int max)
    {
        // Update text
        if (staminaText != null)
        {
            staminaText.text = $"{current} / {max}";

            // Pulse animation on change
            staminaText.transform.DOKill();
            staminaText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 0.5f);
        }

        // Update fill bar with animation
        if (staminaFillBar != null)
        {
            float fillAmount = (float)current / max;

            staminaFillBar.DOKill();
            staminaFillBar.DOFillAmount(fillAmount, animationDuration)
                .SetEase(Ease.OutQuad);

            // Color based on amount
            Color targetColor;
            if (fillAmount >= 0.6f)
            {
                targetColor = fullColor;
            }
            else if (fillAmount >= lowStaminaThreshold)
            {
                targetColor = midColor;
            }
            else
            {
                targetColor = lowColor;
            }

            staminaFillBar.DOColor(targetColor, animationDuration);
        }

        // Show/hide warning
        if (lowStaminaWarning != null)
        {
            float fillAmount = (float)current / max;
            bool showWarning = fillAmount <= lowStaminaThreshold;

            if (lowStaminaWarning.activeSelf != showWarning)
            {
                lowStaminaWarning.SetActive(showWarning);

                if (showWarning)
                {
                    // Pulse warning
                    lowStaminaWarning.transform.localScale = Vector3.zero;
                    lowStaminaWarning.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
                }
            }
        }

        Debug.Log($"🎨 Stamina UI updated: {current}/{max}");
    }

    // Optional: Manual refresh
    public void RefreshDisplay(DraftCardManager manager)
    {
        if (manager != null)
        {
            UpdateStaminaDisplay(manager.CurrentStamina, manager.MaxStamina);
        }
    }
}