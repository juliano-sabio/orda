using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Character", menuName = "Character System/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Informações Básicas")]
    public string characterName;
    public string description;
    public Sprite icon;
    public bool unlocked = true;
    public GameObject characterPrefab;

    [Header("Status Base")]
    [Range(50, 200)]
    public float maxHealth = 100f;

    [Range(5, 30)]
    public float baseAttack = 10f;

    [Range(1, 15)]
    public float baseDefense = 5f;

    [Range(5, 12)]
    public float baseSpeed = 8f;

    [Header("? Sistema de Elementos")]
    public PlayerStats.Element baseElement = PlayerStats.Element.None;
    public List<PlayerStats.Element> strongAgainst = new List<PlayerStats.Element>();
    public List<PlayerStats.Element> weakAgainst = new List<PlayerStats.Element>();

    [Header("?? Ultimate Específica")]
    public UltimateData ultimateSkill;

    [Header("?? Skills Iniciais do Personagem")]
    public List<SkillData> startingSkills = new List<SkillData>();
    public List<SkillModifierData> startingModifiers = new List<SkillModifierData>();

    [Header("?? Comportamentos Especiais")]
    public SkillBehavior specialSkillBehavior;
    public UltimateBehavior ultimateBehavior;

    [Header("?? Progressão")]
    public int unlockLevel = 1;
    public float xpMultiplier = 1.5f;

    [Header("?? Customização Visual")]
    public Color characterColor = Color.white;
    public ParticleSystem.MinMaxGradient auraColor;
    public RuntimeAnimatorController animatorController;

    // ?? MÉTODOS DE CONVENIÊNCIA
    public string GetElementIcon()
    {
        return SkillData.GetElementIcon(baseElement);
    }

    public Color GetElementColor()
    {
        return SkillData.GetElementColor(baseElement);
    }

    public bool HasUltimate()
    {
        return ultimateSkill != null;
    }

    public List<SkillData> GetSkillsByType(SkillType type)
    {
        return startingSkills.FindAll(skill => skill.skillType == type);
    }

    public List<SkillData> GetSkillsByElement(PlayerStats.Element element)
    {
        return startingSkills.FindAll(skill => skill.element == element);
    }
}

[System.Serializable]
public class UltimateData
{
    public string ultimateName;
    [TextArea(2, 4)]
    public string description;
    public Sprite ultimateIcon;

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
}