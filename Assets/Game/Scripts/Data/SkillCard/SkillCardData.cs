// ============================================================================
// SKILL CARD DATA - ScriptableObject
// Skill kartların verilerini tutar (Attack/Defense/Special)
// Turn 8, 16, 24'te seçilir
// ============================================================================

using UnityEngine;

[CreateAssetMenu(fileName = "SkillCard", menuName = "CardGame/Skill Card Data")]
public class SkillCardData : ScriptableObject
{
    [Header("Basic Info")]
    public string skillID;
    public string skillName;
    [TextArea] public string description;
    
    [Header("Type")]
    public SkillType skillType;
    
    [Header("Buffs")]
    [Range(0f, 1f)] public float attackBonus; // 0.25f = +25%
    [Range(0f, 1f)] public float defenseBonus; // 0.25f = +25%
    
    [Header("Visual")]
    public Sprite cardSprite;
}