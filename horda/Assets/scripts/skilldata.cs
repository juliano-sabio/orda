using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Skill", menuName = "Survivor/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("🔤 Identificação")]
    public string skillName;
    [TextArea(3, 5)]
    public string description;
    public Sprite icon;

    [Header("📊 Bônus de Status")]
    public float healthBonus = 0f;
    public float attackBonus = 0f;
    public float defenseBonus = 0f;
    public float speedBonus = 0f;

    [Header("🎯 Modificadores de Skills")]
    public List<SkillModifierData> skillModifiers = new List<SkillModifierData>();

    [Header("🎭 Efeitos Visuais e Sonoros")]
    public GameObject visualEffect;
    public AudioClip soundEffect;

    [Header("⚡ Configurações de Ativação")]
    public bool isPassive = true;
    public float activationInterval = 2f;
    public SkillType skillType = SkillType.Passive;

    [Header("💎 Raridade")]
    public SkillRarity rarity = SkillRarity.Common;

    [Header("🎯 Tipo de Skill Específica")]
    public SpecificSkillType specificType = SpecificSkillType.None;
    public float specialValue = 0f; // Valor especial para tipos específicos
}

public enum SkillRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

public enum SkillType
{
    Passive,
    Active,
    Ultimate
}

public enum SpecificSkillType
{
    None,
    HealthRegen,
    CriticalStrike,
    LifeSteal,
    DamageReflection,
    MovementSpeed,
    AttackSpeed,
    AreaDamage,
    Projectile,
    Shield,
    Heal
}

// 🆕 CLASSE DE MODIFICADOR COMPATÍVEL COM PLAYERSTATS
[System.Serializable]
public class SkillModifierData
{
    public string modifierName;
    public string targetSkillName;
    public float damageMultiplier = 1f;
    public float defenseMultiplier = 1f;
    public PlayerStats.Element element = PlayerStats.Element.None;
    public float duration = 0f;
    public float cooldownReduction = 0f;
    public float areaOfEffect = 0f;
}