// ============================================================================
// CHEST CARD SLOT - Chest içindeki kart gösterimi
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ChestCardSlot : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] Image cardImage;
    [SerializeField] TextMeshProUGUI cardName;
    [SerializeField] GameObject rarityGlow;
    
    public void SetCard(ToyUnitData cardData)
    {
        cardImage.sprite = cardData.toySprite;
        cardName.text = cardData.toyName;
        
        // Show rarity effect for rare cards
        if (cardData.toyRarityType == RarityType.Rare)
        {
            rarityGlow.SetActive(true);
        }
        else
        {
            rarityGlow.SetActive(false);
        }
        
        // Reveal animation
        AnimateReveal();
    }
    
    private void AnimateReveal()
    {
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.5f)
            .SetEase(Ease.OutBack)
            .SetDelay(Random.Range(0f, 0.3f));
    }
}