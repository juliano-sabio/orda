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
        string nome = skillData.GetDisplayName();
        skillName.text = TextUtils.SemAcento(nome);
        skillType.text = Loc.T($"skilltype.{skillData.skillType.ToString().ToLower()}");

        // Elemento
        elementIndicator.color = skillData.GetElementColor();
        elementText.text = Loc.T($"element.{skillData.element.ToString().ToLower()}");
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
        targetSkill.text = $"{Loc.T("ui.target_skill")}: {modifierData.targetSkillName}";
        modifierEffects.text = GetModifierEffectsText(modifierData);

        elementIndicator.color = modifierData.GetElementColor();
    }

    string GetModifierEffectsText(SkillModifierData modifier)
    {
        List<string> effects = new List<string>();

        if (modifier.damageMultiplier != 1f)
            effects.Add($"{Loc.T("mod.damage")}: {modifier.damageMultiplier}x");
        if (modifier.defenseMultiplier != 1f)
            effects.Add($"{Loc.T("mod.defense")}: {modifier.defenseMultiplier}x");
        if (modifier.element != PlayerStats.Element.None)
            effects.Add($"{Loc.T("skill.element")}: {Loc.T($"element.{modifier.element.ToString().ToLower()}")}");

        return string.Join(" | ", effects);
    }
}