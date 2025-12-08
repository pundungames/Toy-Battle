// ============================================================================
// SKILL CARD CONTENT - Skill kart UI component
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class SkillCardContent : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] TextMeshProUGUI skillName;
    [SerializeField] TextMeshProUGUI skillDescription;
    [SerializeField] Image skillIcon;
    [SerializeField] Button selectButton;
    
    private SkillCardData skillData;
    private SkillSystem skillSystem;
    
    public void SetContent(SkillCardData data, SkillSystem system)
    {
        skillData = data;
        skillSystem = system;
        
        skillName.text = data.skillName;
        skillDescription.text = data.description;
        skillIcon.sprite = data.cardSprite;
        
        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnSelectClick);
    }
    
    private void OnSelectClick()
    {
        Taptic.Medium();
        
        // Scale animation
        transform.DOScale(1.1f, 0.1f).OnComplete(() =>
        {
            transform.DOScale(1f, 0.1f);
        });
        
        skillSystem.OnSkillSelected(skillData);
    }
}