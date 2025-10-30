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

    [Header("⚡ Sistema de Elementos")]
    public PlayerStats.Element element = PlayerStats.Element.None;
    public float elementalBonus = 1.0f;
    public float elementalEffectChance = 0.2f;
    public float elementalEffectDuration = 3f;

    [Header("🎯 Modificadores de Skills")]
    public List<SkillModifierData> skillModifiers = new List<SkillModifierData>();

    [Header("🎭 Efeitos Visuais e Sonoros")]
    public GameObject visualEffect;
    public AudioClip soundEffect;

    [Header("🌈 Efeitos Elementais")]
    public GameObject elementalEffect;
    public Color elementColor = Color.white;
    public ParticleSystem.MinMaxGradient elementParticleColor;

    [Header("⚡ Configurações de Ativação")]
    public bool isPassive = true;
    public float activationInterval = 2f;
    public SkillType skillType = SkillType.Passive;
    public float cooldown = 0f;

    [Header("💎 Raridade e Progressão")]
    public SkillRarity rarity = SkillRarity.Common;
    public int requiredLevel = 1;
    public bool isUnique = false;
    public List<SkillData> requiredSkills = new List<SkillData>();

    [Header("🎯 Tipo de Skill Específica")]
    public SpecificSkillType specificType = SpecificSkillType.None;
    public float specialValue = 0f;

    [Header("🔧 Configurações Avançadas")]
    public bool stackable = true;
    public int maxStacks = 1;
    public float duration = 0f;
    public bool isToggleable = false;

    // 🆕 MÉTODO GetFullDescription() ADICIONADO PARA CORRIGIR O ERRO
    public string GetFullDescription()
    {
        string fullDescription = description;

        // Adiciona informações de elemento
        if (element != PlayerStats.Element.None)
        {
            fullDescription += $"\n\n{GetElementIcon()} Elemento: {element}";
            if (elementalBonus != 1.0f)
            {
                fullDescription += $"\nBônus Elemental: {elementalBonus}x";
            }
        }

        // Adiciona bônus de status
        if (healthBonus != 0) fullDescription += $"\n❤️ Vida: {(healthBonus > 0 ? "+" : "")}{healthBonus}";
        if (attackBonus != 0) fullDescription += $"\n⚔️ Ataque: {(attackBonus > 0 ? "+" : "")}{attackBonus}";
        if (defenseBonus != 0) fullDescription += $"\n🛡️ Defesa: {(defenseBonus > 0 ? "+" : "")}{defenseBonus}";
        if (speedBonus != 0) fullDescription += $"\n🏃 Velocidade: {(speedBonus > 0 ? "+" : "")}{speedBonus}";

        // Adiciona informações de tipo específico
        if (specificType != SpecificSkillType.None)
        {
            fullDescription += $"\n🎯 Efeito: {GetSpecificTypeDescription()}";
        }

        // Adiciona informações de raridade
        fullDescription += $"\n💎 Raridade: {rarity}";

        return fullDescription;
    }

    // 🆕 MÉTODO AUXILIAR PARA DESCRIÇÃO DO TIPO ESPECÍFICO
    private string GetSpecificTypeDescription()
    {
        switch (specificType)
        {
            case SpecificSkillType.HealthRegen:
                return $"Regeneração de Vida: {specialValue}/s";
            case SpecificSkillType.CriticalStrike:
                return $"Chance de Crítico: {specialValue}%";
            case SpecificSkillType.LifeSteal:
                return $"Roubo de Vida: {specialValue}%";
            case SpecificSkillType.MovementSpeed:
                return $"Velocidade de Movimento: +{specialValue}%";
            case SpecificSkillType.AttackSpeed:
                return $"Velocidade de Ataque: +{specialValue}%";
            case SpecificSkillType.AreaDamage:
                return $"Dano em Área: +{specialValue}%";
            case SpecificSkillType.Shield:
                return $"Escudo: {specialValue} de defesa";
            case SpecificSkillType.Heal:
                return $"Cura: {specialValue} de vida";
            case SpecificSkillType.Projectile:
                return $"Projéteis: {specialValue} adicionais";
            case SpecificSkillType.DamageReflection:
                return $"Reflexão de Dano: {specialValue}%";
            case SpecificSkillType.ElementalMastery:
                return $"Domínio Elemental: +{specialValue}% de dano elemental";
            case SpecificSkillType.ChainLightning:
                return $"Relâmpago em Cadeia: {specialValue} alvos";
            case SpecificSkillType.PoisonCloud:
                return $"Nuvem de Veneno: {specialValue} de dano por segundo";
            case SpecificSkillType.FireAura:
                return $"Aura de Fogo: {specialValue} de dano por segundo";
            case SpecificSkillType.IceBarrier:
                return $"Barreira de Gelo: {specialValue} de defesa";
            case SpecificSkillType.WindDash:
                return $"Dash de Vento: +{specialValue}% de velocidade";
            case SpecificSkillType.EarthStomp:
                return $"Pisada da Terra: {specialValue} de dano em área";
            default:
                return specificType.ToString();
        }
    }

    // 🆕 MÉTODOS DE CONVENIÊNCIA
    public string GetElementIcon()
    {
        return GetElementIcon(element);
    }

    public static string GetElementIcon(PlayerStats.Element element)
    {
        switch (element)
        {
            case PlayerStats.Element.None: return "⚪";
            case PlayerStats.Element.Fire: return "🔥";
            case PlayerStats.Element.Ice: return "❄️";
            case PlayerStats.Element.Lightning: return "⚡";
            case PlayerStats.Element.Poison: return "☠️";
            case PlayerStats.Element.Earth: return "🌍";
            case PlayerStats.Element.Wind: return "💨";
            default: return "⚪";
        }
    }

    public Color GetElementColor()
    {
        return GetElementColor(element);
    }

    public static Color GetElementColor(PlayerStats.Element element)
    {
        switch (element)
        {
            case PlayerStats.Element.None: return Color.white;
            case PlayerStats.Element.Fire: return new Color(1f, 0.3f, 0.1f);
            case PlayerStats.Element.Ice: return new Color(0.1f, 0.5f, 1f);
            case PlayerStats.Element.Lightning: return new Color(0.8f, 0.8f, 0.1f);
            case PlayerStats.Element.Poison: return new Color(0.5f, 0.1f, 0.8f);
            case PlayerStats.Element.Earth: return new Color(0.6f, 0.4f, 0.2f);
            case PlayerStats.Element.Wind: return new Color(0.4f, 0.8f, 0.9f);
            default: return Color.white;
        }
    }

    public bool MeetsRequirements(int playerLevel, List<SkillData> acquiredSkills)
    {
        // Verifica nível
        if (playerLevel < requiredLevel)
            return false;

        // Verifica skills requeridas
        foreach (var requiredSkill in requiredSkills)
        {
            if (!acquiredSkills.Contains(requiredSkill))
                return false;
        }

        return true;
    }

    // 🆕 MÉTODO PARA APLICAR EFEITOS ELEMENTAIS
    public void ApplyElementalEffects(GameObject target)
    {
        if (element == PlayerStats.Element.None || target == null) return;

        switch (element)
        {
            case PlayerStats.Element.Fire:
                ApplyFireEffect(target);
                break;
            case PlayerStats.Element.Ice:
                ApplyIceEffect(target);
                break;
            case PlayerStats.Element.Lightning:
                ApplyLightningEffect(target);
                break;
            case PlayerStats.Element.Poison:
                ApplyPoisonEffect(target);
                break;
            case PlayerStats.Element.Earth:
                ApplyEarthEffect(target);
                break;
            case PlayerStats.Element.Wind:
                ApplyWindEffect(target);
                break;
        }
    }

    private void ApplyFireEffect(GameObject target)
    {
        Debug.Log($"🔥 Aplicando efeito de Fogo em {target.name}");
        // Implementar lógica de queimadura
    }

    private void ApplyIceEffect(GameObject target)
    {
        Debug.Log($"❄️ Aplicando efeito de Gelo em {target.name}");
        // Implementar lógica de congelamento
    }

    private void ApplyLightningEffect(GameObject target)
    {
        Debug.Log($"⚡ Aplicando efeito de Raio em {target.name}");
        // Implementar lógica de choque
    }

    private void ApplyPoisonEffect(GameObject target)
    {
        Debug.Log($"☠️ Aplicando efeito de Veneno em {target.name}");
        // Implementar lógica de veneno
    }

    private void ApplyEarthEffect(GameObject target)
    {
        Debug.Log($"🌍 Aplicando efeito de Terra em {target.name}");
        // Implementar lógica de lentidão
    }

    private void ApplyWindEffect(GameObject target)
    {
        Debug.Log($"💨 Aplicando efeito de Vento em {target.name}");
        // Implementar lógica de empurrão
    }

    // 🆕 MÉTODO PARA VERIFICAR SE A SKILL É VÁLIDA
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(skillName) && !string.IsNullOrEmpty(description);
    }

    // 🆕 MÉTODO PARA OBTER COR DA RARIDADE
    public Color GetRarityColor()
    {
        switch (rarity)
        {
            case SkillRarity.Common: return Color.gray;
            case SkillRarity.Uncommon: return Color.green;
            case SkillRarity.Rare: return Color.blue;
            case SkillRarity.Epic: return new Color(0.5f, 0f, 0.5f); // Roxo
            case SkillRarity.Legendary: return new Color(1f, 0.5f, 0f); // Laranja
            case SkillRarity.Mythic: return Color.red;
            default: return Color.white;
        }
    }
}

public enum SkillRarity
{
    Common,      // Comum - Cinza
    Uncommon,    // Incomum - Verde
    Rare,        // Rara - Azul
    Epic,        // Épica - Roxo
    Legendary,   // Lendária - Laranja
    Mythic       // Mítica - Vermelho
}

public enum SkillType
{
    Passive,     // Passiva - Ativa automaticamente
    Active,      // Ativa - Requer ativação manual
    Ultimate,    // Ultimate - Habilidade suprema
    Aura,        // Aura - Afeta área ao redor
    Toggle       // Alternável - Liga/Desliga
}

public enum SpecificSkillType
{
    None,
    HealthRegen,         // Regeneração de vida
    CriticalStrike,      // Golpe crítico
    LifeSteal,           // Roubo de vida
    DamageReflection,    // Reflexão de dano
    MovementSpeed,       // Velocidade de movimento
    AttackSpeed,         // Velocidade de ataque
    AreaDamage,          // Dano em área
    Projectile,          // Projéteis
    Shield,              // Escudo
    Heal,                // Cura
    ElementalMastery,    // Domínio Elemental
    ChainLightning,      // Relâmpago em Cadeia
    PoisonCloud,         // Nuvem de Veneno
    FireAura,            // Aura de Fogo
    IceBarrier,          // Barreira de Gelo
    WindDash,            // Dash de Vento
    EarthStomp           // Pisada da Terra
}

// 🆕 CLASSE DE MODIFICADOR COMPATÍVEL COM PLAYERSTATS
[System.Serializable]
public class SkillModifierData
{
    [Header("🔤 Identificação")]
    public string modifierName;
    public string targetSkillName;

    [Header("📊 Modificadores de Status")]
    public float damageMultiplier = 1f;
    public float defenseMultiplier = 1f;
    public float speedMultiplier = 1f;
    public float healthMultiplier = 1f;

    [Header("⚡ Sistema de Elementos")]
    public PlayerStats.Element element = PlayerStats.Element.None;
    public float elementalEffectChance = 0f;
    public float elementalEffectDuration = 0f;

    [Header("⏱️ Configurações de Tempo")]
    public float duration = 0f;
    public float cooldownReduction = 0f;

    [Header("🎯 Configurações de Alcance")]
    public float areaOfEffect = 0f;
    public int additionalTargets = 0;

    [Header("💫 Efeitos Especiais")]
    public bool causesBurn = false;
    public bool causesFreeze = false;
    public bool causesStun = false;
    public bool causesPoison = false;
    public float specialEffectChance = 0f;

    // 🆕 MÉTODOS DE CONVENIÊNCIA
    public string GetElementIcon()
    {
        return SkillData.GetElementIcon(element);
    }

    public Color GetElementColor()
    {
        return SkillData.GetElementColor(element);
    }

    public string GetDescription()
    {
        string desc = $"{modifierName}\n";

        if (damageMultiplier != 1f)
            desc += $"Dano: {damageMultiplier}x ";
        if (defenseMultiplier != 1f)
            desc += $"Defesa: {defenseMultiplier}x ";
        if (element != PlayerStats.Element.None)
            desc += $"\nElemento: {element}";
        if (areaOfEffect > 0)
            desc += $"\nÁrea: +{areaOfEffect}m";
        if (cooldownReduction > 0)
            desc += $"\nRedução de Cooldown: {cooldownReduction}s";

        return desc;
    }

    // 🆕 CONVERSÃO PARA PlayerStats.SkillModifier
    public PlayerStats.SkillModifier ToPlayerStatsModifier()
    {
        return new PlayerStats.SkillModifier
        {
            modifierName = this.modifierName,
            targetSkillName = this.targetSkillName,
            damageMultiplier = this.damageMultiplier,
            defenseMultiplier = this.defenseMultiplier,
            element = this.element,
            duration = this.duration
        };
    }
}