// ============================================================================
// BATTLE RESULT UI - WIN/LOSE BANNER
// ✅ Shows animated banner after battle ends
// ✅ Different designs for win/lose
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class BattleResultUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] GameObject resultPanel;
    [SerializeField] TextMeshProUGUI resultText;
    [SerializeField] Image resultBackground;

    [Header("Win Settings")]
    [SerializeField] string winText = "VICTORY!";
    [SerializeField] Color winColor = Color.green;
    [SerializeField] Sprite winBackground;

    [Header("Lose Settings")]
    [SerializeField] string loseText = "DEFEAT";
    [SerializeField] Color loseColor = Color.red;
    [SerializeField] Sprite loseBackground;

    [Header("Animation")]
    [SerializeField] float scaleInDuration = 0.5f;
    [SerializeField] float shakeDuration = 0.3f;
    [SerializeField] float shakeStrength = 30f;

    private void Start()
    {
    }

    // ===== SHOW RESULT =====

    public void ShowResult(bool playerWon)
    {
        if (resultPanel == null || resultText == null)
        {
            Debug.LogWarning("⚠️ BattleResultUI: Missing UI elements!");
            return;
        }

        Debug.Log($"🎬 Showing battle result: {(playerWon ? "WIN" : "LOSE")}");

        // Setup UI based on result
        if (playerWon)
        {
            resultText.text = winText;
            resultText.color = winColor;
            if (resultBackground != null && winBackground != null)
            {
                resultBackground.sprite = winBackground;
            }
        }
        else
        {
            resultText.text = loseText;
            resultText.color = loseColor;
            if (resultBackground != null && loseBackground != null)
            {
                resultBackground.sprite = loseBackground;
            }
        }

        // Activate panel
        resultPanel.SetActive(true);

        // Animate in
        AnimateIn();

        // Play sound
        if (playerWon)
        {
            Taptic.Success();
            // AudioManager?.Play("battle_win");
        }
        else
        {
            Taptic.Heavy();
            // AudioManager?.Play("battle_lose");
        }
    }

    // ===== HIDE RESULT =====

    public void HideResult()
    {
        if (resultPanel == null) return;

        Debug.Log("👋 Hiding battle result UI");

        // Animate out
        resultPanel.transform.DOScale(0f, 0.3f)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                resultPanel.SetActive(false);
                resultPanel.transform.localScale = Vector3.one; // Reset for next time
            });
    }

    // ===== ANIMATIONS =====

    private void AnimateIn()
    {
        // Start small
        resultPanel.transform.localScale = Vector3.zero;

        // Scale up with bounce
        resultPanel.transform.DOScale(1f, scaleInDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                // Shake effect
                resultPanel.transform.DOShakeRotation(shakeDuration, shakeStrength, 10, 90);
            });

        // Text color pulse
        if (resultText != null)
        {
            Color originalColor = resultText.color;
            resultText.DOColor(Color.white, 0.3f)
                .SetLoops(4, LoopType.Yoyo)
                .OnComplete(() => resultText.color = originalColor);
        }
    }
}