using UnityEngine;

[CreateAssetMenu(fileName = "New Ultimate", menuName = "Survivor/Ultimate Data")]
public class UltimateData : ScriptableObject
{
    [Header("Identificação")]
    public string ultimateName;
    [TextArea(2, 4)]
    public string description;
    public Sprite ultimateIcon;

    [Header("Localização (chaves GameStrings)")]
    public string nameKey        = "";
    public string descriptionKey = "";
    public string specialEffectKey = "";

    [Header("Configura��es B�sicas")]
    [Range(30, 60)]
    public float cooldown = 30f;

    [Range(20, 100)]
    public float baseDamage = 50f;

    [Range(3, 8)]
    public float areaOfEffect = 5f;

    [Range(2, 10)]
    public float duration = 3f;

    public PlayerStats.Element element;

    [Header("Efeitos Especiais")]
    [TextArea(1, 3)]
    public string specialEffect;
    public SpecificSkillType ultimateType = SpecificSkillType.None;
    public float specialValue = 0f;

    [Header("Configura��es de Comportamento")]
    public string behaviorScriptName;
    public GameObject visualEffectPrefab;

    [Header("Requisitos")]
    public int requiredLevel = 1;
    public bool isUnlocked = true;

    // Métodos de conveniência
    public string GetDisplayName()
    {
        return !string.IsNullOrEmpty(nameKey) ? Loc.T(nameKey) : ultimateName;
    }

    public string GetDisplayDescription()
    {
        return !string.IsNullOrEmpty(descriptionKey) ? Loc.T(descriptionKey) : description;
    }

    public string GetDisplaySpecialEffect()
    {
        if (!string.IsNullOrEmpty(specialEffectKey)) return Loc.T(specialEffectKey);
        return specialEffect;
    }

    public string GetElementIcon()
    {
        return SkillData.GetElementIcon(element);
    }

    public Color GetElementColor()
    {
        return SkillData.GetElementColor(element);
    }

    public string GetFullDescription()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine(GetDisplayDescription());
        sb.AppendLine();
        sb.AppendLine($"{Loc.T("ui.dmg")}: {baseDamage}");
        sb.AppendLine($"{Loc.T("ui.area")}: {areaOfEffect}m");
        sb.AppendLine($"{Loc.T("ui.dur")}: {duration}s");
        sb.AppendLine($"{Loc.T("ui.cd")}: {cooldown}s");

        if (element != PlayerStats.Element.None)
            sb.AppendLine($"{Loc.T("element." + element.ToString().ToLower())} {GetElementIcon()}");

        string eff = GetDisplaySpecialEffect();
        if (!string.IsNullOrEmpty(eff))
            sb.AppendLine(eff);

        return sb.ToString().Trim();
    }

    public bool MeetsRequirements(int playerLevel)
    {
        return playerLevel >= requiredLevel && isUnlocked;
    }
}