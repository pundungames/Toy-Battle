// ============================================================================
// SKILL SYSTEM - Skill kartlarını yönetir
// Turn 8, 16, 24'te 3 skill arasından seçim yapılır
// Skill sadece o battle'da aktiftir
// ============================================================================

using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class SkillSystem : MonoBehaviour
{
    [Inject] UIManager uiManager;
    
    [Header("Skill Pool")]
    [SerializeField] List<SkillCardData> allSkills;
    
    [Header("UI")]
    [SerializeField] List<SkillCardContent> skillCardSlots; // 3 slot
    
    [Header("Active Skill")]
    [SerializeField] SkillCardData activeSkill = null;
    
    // ===== SHOW SKILL SELECTION =====
    
    public void ShowSkillSelection()
    {
        uiManager.ShowSkillSelection();
        
        // Generate 3 random skills
        List<SkillCardData> skillOptions = GetRandomSkills(3);
        
        // Display in UI
        for (int i = 0; i < skillCardSlots.Count; i++)
        {
            if (i < skillOptions.Count)
            {
                skillCardSlots[i].SetContent(skillOptions[i], this);
                skillCardSlots[i].gameObject.SetActive(true);
            }
            else
            {
                skillCardSlots[i].gameObject.SetActive(false);
            }
        }
    }
    
    private List<SkillCardData> GetRandomSkills(int count)
    {
        List<SkillCardData> result = new List<SkillCardData>();
        List<SkillCardData> tempPool = new List<SkillCardData>(allSkills);
        
        for (int i = 0; i < count && tempPool.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, tempPool.Count);
            result.Add(tempPool[randomIndex]);
            tempPool.RemoveAt(randomIndex);
        }
        
        return result;
    }
    
    // ===== SELECT SKILL =====
    
    public void OnSkillSelected(SkillCardData skill)
    {
        activeSkill = skill;
        EventManager.OnSkillSelected(skill);
        
        Debug.Log($"Skill selected: {skill.skillName}");
        
        // Continue to next turn
        EventManager.OnDraftComplete();
    }
    
    // ===== APPLY SKILL BUFFS =====
    
    public void ApplyActiveSkillBuffs(List<RuntimeUnit> units)
    {
        if (activeSkill == null) return;
        
        foreach (var unit in units)
        {
            switch (activeSkill.skillType)
            {
                case SkillType.Attack:
                    unit.damageMultiplier += activeSkill.attackBonus; // +25%
                    break;
                
                case SkillType.Defense:
                    float shieldBonus = unit.currentHP * activeSkill.defenseBonus; // +25% HP
                    unit.shieldAmount += shieldBonus;
                    break;
                
                case SkillType.Special:
                    unit.damageMultiplier += activeSkill.attackBonus; // +10%
                    unit.currentHP += Mathf.RoundToInt(unit.currentHP * activeSkill.defenseBonus); // +10% HP
                    break;
            }
        }
        
        Debug.Log($"Skill buffs applied: {activeSkill.skillName}");
    }
    
    // ===== CLEAR ACTIVE SKILL =====
    
    public void ClearActiveSkill()
    {
        activeSkill = null;
    }
}