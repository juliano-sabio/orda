using UnityEngine;

[CreateAssetMenu(fileName = "New Ultimate", menuName = "Survivor/Ultimate Data")]
public class UltimateData : ScriptableObject
{
    [Header("Identificação")]
    public string ultimateName;
    [TextArea(2, 4)]
    public string description;
    public Sprite ultimateIcon;

    [Header("Configurações Básicas")]
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

    [Header("Configurações de Comportamento")]
    public string behaviorScriptName;
    public GameObject visualEffectPrefab;

    [Header("Requisitos")]
    public int requiredLevel = 1;
    public bool isUnlocked = true;

    // Métodos de conveniência
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
        sb.AppendLine(description);
        sb.AppendLine();
        sb.AppendLine($"Dano Base: {baseDamage}");
        sb.AppendLine($"Área de Efeito: {areaOfEffect}m");
        sb.AppendLine($"Duração: {duration}s");
        sb.AppendLine($"Recarga: {cooldown}s");

        if (element != PlayerStats.Element.None)
            sb.AppendLine($"Elemento: {element} {GetElementIcon()}");

        if (!string.IsNullOrEmpty(specialEffect))
            sb.AppendLine($"Efeito: {specialEffect}");

        return sb.ToString().Trim();
    }

    public bool MeetsRequirements(int playerLevel)
    {
        return playerLevel >= requiredLevel && isUnlocked;
    }
}