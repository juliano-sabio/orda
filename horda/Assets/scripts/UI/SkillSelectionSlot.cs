using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillSelectionSlot : MonoBehaviour
{
    public Image skillIcon;
    public Text skillName;
    public Text skillType;
    public Image elementIndicator;
    public Text elementText;

    public void SetSkill(SkillData skillData)
    {
        skillIcon.sprite = skillData.icon;
        skillName.text = skillData.skillName;
        skillType.text = skillData.skillType.ToString();

        // Elemento
        elementIndicator.color = skillData.GetElementColor();
        elementText.text = skillData.element.ToString();
        elementText.color = skillData.GetElementColor();
    }
}

public class ModifierSelectionSlot : MonoBehaviour
{
    public Text modifierName;
    public Text targetSkill;
    public Text modifierEffects;
    public Image elementIndicator;

    public void SetModifier(SkillModifierData modifierData)
    {
        modifierName.text = modifierData.modifierName;
        targetSkill.text = $"Para: {modifierData.targetSkillName}";
        modifierEffects.text = GetModifierEffectsText(modifierData);

        elementIndicator.color = modifierData.GetElementColor();
    }

    string GetModifierEffectsText(SkillModifierData modifier)
    {
        List<string> effects = new List<string>();

        if (modifier.damageMultiplier != 1f)
            effects.Add($"Dano: {modifier.damageMultiplier}x");
        if (modifier.defenseMultiplier != 1f)
            effects.Add($"Defesa: {modifier.defenseMultiplier}x");
        if (modifier.element != PlayerStats.Element.None)
            effects.Add($"Elemento: {modifier.element}");

        return string.Join(" | ", effects);
    }
}