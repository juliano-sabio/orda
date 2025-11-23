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
    [Header("🎨 Prefab do Card")]
    public GameObject cardPrefab;

    [Header("📊 Bônus de Status")]
    public float healthBonus = 0f;
    public float attackBonus = 0f;
    public float defenseBonus = 0f;
    public float speedBonus = 0f;
    public float healthRegenBonus = 0f;
    public float attackSpeedMultiplier = 1.0f;

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

    // 🆕 PROPRIEDADES ADICIONADAS PARA PROJÉTEIS
    [Header("🚀 Configurações de Projétil (2D)")]
    public GameObject projectilePrefab2D;
    public float projectileSpeed = 8f;
    public float projectileLifeTime = 4f;
    public int projectileCount = 1;
    public float projectileSpread = 0f;
    public bool homingProjectile = false;
    public float homingStrength = 2f;

    [Header("🎯 Comportamento de Projétil")]
    public bool pierceEnemies = false;
    public int pierceCount = 1;
    public bool bounceBetweenEnemies = false;
    public int bounceCount = 0;
    public bool explodeOnImpact = false;
    public float explosionRadius = 2f;

    // MÉTODOS
    public string GetElementIcon() { return GetElementIcon(this.element); }

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

    public Color GetElementColor() { return GetElementColor(this.element); }

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

    public string GetFullDescription()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine(description);
        sb.AppendLine();

        if (element != PlayerStats.Element.None)
        {
            sb.AppendLine($"{GetElementIcon()} Elemento: {element}");
            if (elementalBonus != 1.0f) sb.AppendLine($"Bônus Elemental: {elementalBonus}x");
        }

        if (healthBonus != 0) sb.AppendLine($"❤️ Vida: {FormatBonus(healthBonus)}");
        if (attackBonus != 0) sb.AppendLine($"⚔️ Ataque: {FormatBonus(attackBonus)}");
        if (defenseBonus != 0) sb.AppendLine($"🛡️ Defesa: {FormatBonus(defenseBonus)}");
        if (speedBonus != 0) sb.AppendLine($"🏃 Velocidade: {FormatBonus(speedBonus)}");
        if (healthRegenBonus != 0) sb.AppendLine($"💚 Regeneração: {FormatBonus(healthRegenBonus)}/s");
        if (attackSpeedMultiplier != 1.0f) sb.AppendLine($"⚡ Vel. Ataque: {attackSpeedMultiplier}x");

        // 🆕 DESCRIÇÕES PARA PROJÉTEIS
        if (specificType == SpecificSkillType.Projectile)
        {
            sb.AppendLine($"🎯 Projéteis: {projectileCount}");
            if (projectileSpeed != 8f) sb.AppendLine($"💨 Velocidade: {projectileSpeed}");
            if (pierceEnemies) sb.AppendLine($"🔪 Penetração: {pierceCount} inimigos");
            if (bounceBetweenEnemies) sb.AppendLine($"🔁 Ricochete: {bounceCount} vezes");
            if (explodeOnImpact) sb.AppendLine($"💥 Explosão: {explosionRadius}m de raio");
            if (homingProjectile) sb.AppendLine($"🎯 Projétil Guiado");
        }

        if (specificType != SpecificSkillType.None)
            sb.AppendLine($"🎯 Efeito: {GetSpecificTypeDescription()}");

        if (cooldown > 0) sb.AppendLine($"⏱️ Cooldown: {cooldown}s");
        if (duration > 0) sb.AppendLine($"⏰ Duração: {duration}s");

        sb.AppendLine($"💎 Raridade: {rarity}");

        return sb.ToString().Trim();
    }

    private string FormatBonus(float value) => value > 0 ? $"+{value}" : value.ToString();

    private string GetSpecificTypeDescription()
    {
        switch (specificType)
        {
            case SpecificSkillType.HealthRegen: return $"Regeneração de Vida: {specialValue}/s";
            case SpecificSkillType.CriticalStrike: return $"Chance de Crítico: {specialValue}%";
            case SpecificSkillType.LifeSteal: return $"Roubo de Vida: {specialValue}%";
            case SpecificSkillType.MovementSpeed: return $"Velocidade de Movimento: +{specialValue}%";
            case SpecificSkillType.AttackSpeed: return $"Velocidade de Ataque: +{specialValue}%";
            case SpecificSkillType.AreaDamage: return $"Dano em Área: +{specialValue}%";
            case SpecificSkillType.Shield: return $"Escudo: {specialValue} de defesa";
            case SpecificSkillType.Heal: return $"Cura: {specialValue} de vida";
            case SpecificSkillType.Projectile:
                return $"Projéteis: {projectileCount} | Vel: {projectileSpeed} | Dano: +{attackBonus}";
            case SpecificSkillType.DamageReflection: return $"Reflexão de Dano: {specialValue}%";
            case SpecificSkillType.ElementalMastery: return $"Domínio Elemental: +{specialValue}% de dano elemental";
            case SpecificSkillType.ChainLightning: return $"Relâmpago em Cadeia: {specialValue} alvos";
            case SpecificSkillType.PoisonCloud: return $"Nuvem de Veneno: {specialValue} de dano por segundo";
            case SpecificSkillType.FireAura: return $"Aura de Fogo: {specialValue} de dano por segundo";
            case SpecificSkillType.IceBarrier: return $"Barreira de Gelo: {specialValue} de defesa";
            case SpecificSkillType.WindDash: return $"Dash de Vento: +{specialValue}% de velocidade";
            case SpecificSkillType.EarthStomp: return $"Pisada da Terra: {specialValue} de dano em área";
            default: return specificType.ToString();
        }
    }

    public bool MeetsRequirements(int playerLevel, List<SkillData> acquiredSkills)
    {
        if (playerLevel < requiredLevel) return false;

        foreach (var requiredSkill in requiredSkills)
            if (!acquiredSkills.Contains(requiredSkill)) return false;

        if (isUnique && acquiredSkills.Contains(this)) return false;

        return true;
    }

    public void ApplyElementalEffects(GameObject target, PlayerStats playerStats = null)
    {
        if (element == PlayerStats.Element.None || target == null) return;

        switch (element)
        {
            case PlayerStats.Element.Fire: ApplyFireEffect(target, playerStats); break;
            case PlayerStats.Element.Ice: ApplyIceEffect(target, playerStats); break;
            case PlayerStats.Element.Lightning: ApplyLightningEffect(target, playerStats); break;
            case PlayerStats.Element.Poison: ApplyPoisonEffect(target, playerStats); break;
            case PlayerStats.Element.Earth: ApplyEarthEffect(target, playerStats); break;
            case PlayerStats.Element.Wind: ApplyWindEffect(target, playerStats); break;
        }
    }

    private void ApplyFireEffect(GameObject target, PlayerStats playerStats)
    {
        Debug.Log($"🔥 Aplicando efeito de Fogo em {target.name}");
        // Implementar queimadura contínua
    }

    private void ApplyIceEffect(GameObject target, PlayerStats playerStats)
    {
        Debug.Log($"❄️ Aplicando efeito de Gelo em {target.name}");
        // Implementar lentidão
    }

    private void ApplyLightningEffect(GameObject target, PlayerStats playerStats)
    {
        Debug.Log($"⚡ Aplicando efeito de Raio em {target.name}");
        // Implementar corrente elétrica
    }

    private void ApplyPoisonEffect(GameObject target, PlayerStats playerStats)
    {
        Debug.Log($"☠️ Aplicando efeito de Veneno em {target.name}");
        // Implementar dano contínuo
    }

    private void ApplyEarthEffect(GameObject target, PlayerStats playerStats)
    {
        Debug.Log($"🌍 Aplicando efeito de Terra em {target.name}");
        // Implementar atordoamento
    }

    private void ApplyWindEffect(GameObject target, PlayerStats playerStats)
    {
        Debug.Log($"💨 Aplicando efeito de Vento em {target.name}");
        // Implementar repulsão
    }

    public bool IsValid() => !string.IsNullOrEmpty(skillName);

    public Color GetRarityColor()
    {
        switch (rarity)
        {
            case SkillRarity.Common: return new Color(0.7f, 0.7f, 0.7f);
            case SkillRarity.Uncommon: return new Color(0.2f, 0.8f, 0.2f);
            case SkillRarity.Rare: return new Color(0.2f, 0.4f, 1f);
            case SkillRarity.Epic: return new Color(0.6f, 0.2f, 0.8f);
            case SkillRarity.Legendary: return new Color(1f, 0.5f, 0f);
            case SkillRarity.Mythic: return new Color(1f, 0.1f, 0.1f);
            default: return Color.white;
        }
    }

    public void ApplyToPlayer(PlayerStats playerStats)
    {
        if (playerStats == null) return;

        // Aplica bônus básicos
        playerStats.maxHealth += healthBonus;
        playerStats.health += healthBonus;
        playerStats.attack += attackBonus;
        playerStats.defense += defenseBonus;
        playerStats.speed += speedBonus;

        // Aplica as novas propriedades
        playerStats.healthRegenRate += healthRegenBonus;
        playerStats.attackActivationInterval *= attackSpeedMultiplier;

        Debug.Log($"✨ Skill {skillName} aplicada ao player");
    }

    public void RemoveFromPlayer(PlayerStats playerStats)
    {
        if (playerStats == null) return;

        // Remove bônus básicos
        playerStats.maxHealth -= healthBonus;
        playerStats.health = Mathf.Min(playerStats.health, playerStats.maxHealth);
        playerStats.attack -= attackBonus;
        playerStats.defense -= defenseBonus;
        playerStats.speed -= speedBonus;

        // Remove as novas propriedades
        playerStats.healthRegenRate -= healthRegenBonus;
        playerStats.attackActivationInterval /= attackSpeedMultiplier;

        Debug.Log($"🔴 Skill {skillName} removida do player");
    }

    // 🆕 MÉTODOS ESPECÍFICOS PARA PROJÉTEIS
    public bool IsProjectileSkill()
    {
        return specificType == SpecificSkillType.Projectile;
    }

    public bool HasProjectilePrefab()
    {
        return projectilePrefab2D != null || visualEffect != null;
    }

    public GameObject GetProjectilePrefab()
    {
        return projectilePrefab2D != null ? projectilePrefab2D : visualEffect;
    }

    public float GetProjectileDamage()
    {
        return attackBonus > 0 ? attackBonus : 15f;
    }
}

// Enums
public enum SkillType
{
    Passive,
    Active,
    Ultimate,
    Aura,
    Toggle,
    Attack,
    Defense
}

public enum SkillRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
    Mythic
}

public enum SpecificSkillType
{
    None,
    HealthRegen,
    CriticalStrike,
    LifeSteal,
    MovementSpeed,
    AttackSpeed,
    AreaDamage,
    Projectile,
    Shield,
    Heal,
    DamageReflection,
    ElementalMastery,
    ChainLightning,
    PoisonCloud,
    FireAura,
    IceBarrier,
    WindDash,
    EarthStomp
}

[System.Serializable]
public class SkillModifierData
{
    // Identificação
    public string modifierName;
    public string targetSkillName;

    // Modificadores de Status
    public float damageMultiplier = 1f;
    public float defenseMultiplier = 1f;
    public float speedMultiplier = 1f;
    public float healthMultiplier = 1f;

    // Sistema de Elementos
    public PlayerStats.Element element = PlayerStats.Element.None;
    public float elementalEffectChance = 0f;
    public float elementalEffectDuration = 0f;

    // Configurações de Tempo
    public float duration = 0f;
    public float cooldownReduction = 0f;

    // Configurações de Alcance
    public float areaOfEffect = 0f;
    public int additionalTargets = 0;

    // Efeitos Especiais
    public bool causesBurn = false;
    public bool causesFreeze = false;
    public bool causesStun = false;
    public bool causesPoison = false;
    public float specialEffectChance = 0f;

    public string GetElementIcon() => SkillData.GetElementIcon(element);
    public Color GetElementColor() => SkillData.GetElementColor(element);

    public string GetDescription()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine(modifierName);
        if (damageMultiplier != 1f) sb.AppendLine($"Dano: {damageMultiplier}x");
        if (defenseMultiplier != 1f) sb.AppendLine($"Defesa: {defenseMultiplier}x");
        if (element != PlayerStats.Element.None) sb.AppendLine($"Elemento: {element}");
        return sb.ToString().Trim();
    }

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

    public bool IsValid() => !string.IsNullOrEmpty(modifierName) && !string.IsNullOrEmpty(targetSkillName);
}