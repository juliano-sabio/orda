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

    [Header("🌐 Localização")]
    public string nameKey        = "";
    public string descriptionKey = "";

    public string GetDisplayName()        => !string.IsNullOrEmpty(nameKey)        ? Loc.T(nameKey)        : skillName;
    public string GetDisplayDescription() => !string.IsNullOrEmpty(descriptionKey) ? Loc.T(descriptionKey) : description;
    [Header("🎨 Prefab do Card")]
    public GameObject cardPrefab;

    [Header("Configurações de Visual/Prefab")]
    public GameObject projectilePrefab; //

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

    [Header("💎 Elemento Infundido (runtime)")]
    public ElementType appliedElement = ElementType.None;
    public int appliedCharacteristicIndex = -1; // -1 = sem característica

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

    // 🆕 PROPRIEDADES ADICIONADAS PARA PROJÉTEIS ORBITAIS
    [Header("🌀 Configurações de Projétil Orbital")]
    public bool isOrbitalProjectile = false;
    public float orbitRadius = 2f;
    public float orbitSpeed = 180f;
    public int numberOfOrbits = 1;
    public bool continuousOrbitalSpawning = false;
    public float orbitalSpawnInterval = 2f;
    public float orbitalLaunchSpeed = 10f;
    public float maxOrbitalLaunchDistance = 15f;
    public int maxOrbitalProjectiles = 3;

    [Header("🎯 Configurações de Targeting Orbital")]
    public OrbitalTargetingMode orbitalTargetingMode = OrbitalTargetingMode.NearestEnemy;
    public bool autoAcquireTargets = true;
    public float targetAcquisitionRange = 8f;

    // 🆕 🌀 PROPRIEDADES ESPECÍFICAS PARA BUMERANGUE
    [Header("🌀 Configurações de Bumerangue")]
    public float boomerangThrowRange = 8f;
    public float boomerangReturnRange = 1.5f;
    public float boomerangThrowSpeed = 15f;
    public float boomerangReturnSpeed = 20f;
    public int boomerangMaxTargets = 3;
    public float boomerangRotationSpeed = 720f;
    public bool boomerangPierceThrough = false;
    public float boomerangLifetime = 5f;

    [Header("🌀 Efeitos de Retorno do Bumerangue")]
    public bool boomerangHealOnReturn = true;
    public float boomerangHealPercent = 0.1f; // 10% do dano
    public bool boomerangBuffOnReturn = false;
    public float boomerangBuffDuration = 3f;
    public float boomerangBuffMultiplier = 1.2f;

    [Header("🌀 Comportamento de Bumerangue")]
    public BoomerangBehaviorType boomerangBehavior = BoomerangBehaviorType.ReturnToPlayer;
    public bool boomerangSeekNewTargets = true;
    public float boomerangSeekRadius = 5f;
    public int boomerangMaxHitsPerThrow = 5;

    [Header("🛡️ Configurações do Escudo Rotativo")]
    public GameObject escudoPrefabVisual;       // Sprite da parentese que orbita o player
    // projectilePrefab2D é usado como prefab do projétil refletido
    public float escudoVelocidadeRotacao = 120f;
    public float escudoRaioOrbita = 1.8f;
    public int escudoQuantidade = 1;

    // MÉTODOS EXISTENTES (mantidos intactos)
    public string GetElementIcon() { return GetElementIcon(this.element); }

    public static string GetElementIcon(PlayerStats.Element element)
    {
        return "";
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
            sb.AppendLine($"{Loc.T("skill.element")}: {element}");
            if (elementalBonus != 1.0f) sb.AppendLine($"{Loc.T("skill.elemental_bonus")}: {elementalBonus}x");
        }

        if (healthBonus != 0) sb.AppendLine($"{Loc.T("stat.hp")}: {FormatBonus(healthBonus)}");
        if (attackBonus != 0) sb.AppendLine($"{Loc.T("stat.atk")}: {FormatBonus(attackBonus)}");
        if (defenseBonus != 0) sb.AppendLine($"{Loc.T("stat.def")}: {FormatBonus(defenseBonus)}");
        if (speedBonus != 0) sb.AppendLine($"{Loc.T("stat.spd")}: {FormatBonus(speedBonus)}");
        if (healthRegenBonus != 0) sb.AppendLine($"{Loc.T("stat.regen")}: {FormatBonus(healthRegenBonus)}/s");
        if (attackSpeedMultiplier != 1.0f) sb.AppendLine($"{Loc.T("stat.atkspd")}: {attackSpeedMultiplier}x");
        if (specificType == SpecificSkillType.Shield)
        {
            sb.AppendLine(Loc.T("skill.shield_active"));
            if (cooldown > 0) sb.AppendLine($"{Loc.T("skill.cooldown")}: {cooldown}s");
        }

        if (specificType == SpecificSkillType.Boomerang)
        {
            sb.AppendLine($"{Loc.T("skill.boomerang_label")}: {attackBonus} {Loc.T("skill.damage")}");
            sb.AppendLine($"{Loc.T("skill.range")}: {boomerangThrowRange}m");
            sb.AppendLine($"{Loc.T("skill.targets")}: {boomerangMaxTargets}");
            sb.AppendLine($"{Loc.T("skill.speed_label")}: {boomerangThrowSpeed}");
            sb.AppendLine(Loc.T("skill.returns_to_player"));

            if (boomerangPierceThrough)
                sb.AppendLine(Loc.T("skill.pierces_enemies"));

            if (boomerangHealOnReturn)
                sb.AppendLine($"{Loc.T("skill.heals")} {boomerangHealPercent * 100}{Loc.T("skill.pct_dmg_on_return")}");

            if (boomerangSeekNewTargets)
                sb.AppendLine($"{Loc.T("skill.seek_new_targets")} ({Loc.T("skill.range")}: {boomerangSeekRadius}m)");
        }
        else if (specificType == SpecificSkillType.Projectile)
        {
            if (isOrbitalProjectile)
            {
                sb.AppendLine($"{Loc.T("skill.orbital_projectiles")}: {maxOrbitalProjectiles}");
                sb.AppendLine($"{Loc.T("skill.orbital_radius")}: {orbitRadius}m");
                sb.AppendLine($"{Loc.T("skill.orbital_speed")}: {orbitSpeed}/s");
                sb.AppendLine($"{Loc.T("skill.orbits")}: {numberOfOrbits}");
                if (continuousOrbitalSpawning) sb.AppendLine($"{Loc.T("skill.spawn_interval")}: {orbitalSpawnInterval}s");
            }
            else
            {
                sb.AppendLine($"{Loc.T("skill.projectiles")}: {projectileCount}");
                if (projectileSpeed != 8f) sb.AppendLine($"{Loc.T("stat.spd")}: {projectileSpeed}");
            }

            if (pierceEnemies) sb.AppendLine($"{Loc.T("skill.pierce_count")}: {pierceCount}");
            if (bounceBetweenEnemies) sb.AppendLine($"{Loc.T("skill.bounce_count")}: {bounceCount}");
            if (explodeOnImpact) sb.AppendLine($"{Loc.T("skill.explosion")}: {explosionRadius}m");
            if (homingProjectile) sb.AppendLine(Loc.T("skill.homing"));
        }

        if (specificType != SpecificSkillType.None && specificType != SpecificSkillType.Boomerang)
            sb.AppendLine($"{Loc.T("skill.effect")}: {GetSpecificTypeDescription()}");

        if (cooldown > 0) sb.AppendLine($"{Loc.T("skill.cooldown_label")}: {cooldown}s");
        if (duration > 0) sb.AppendLine($"{Loc.T("skill.duration_label")}: {duration}s");

        sb.AppendLine($"{Loc.T("skill.rarity_label")}: {rarity}");

        return sb.ToString().Trim();
    }

    private string FormatBonus(float value) => value > 0 ? $"+{value}" : value.ToString();

    private string GetSpecificTypeDescription()
    {
        switch (specificType)
        {
            case SpecificSkillType.HealthRegen: return $"{Loc.T("skill.type.health_regen")}: {specialValue}/s";
            case SpecificSkillType.CriticalStrike: return $"{Loc.T("skill.type.crit_chance")}: {specialValue}%";
            case SpecificSkillType.LifeSteal: return $"{Loc.T("skill.type.lifesteal")}: {specialValue}%";
            case SpecificSkillType.MovementSpeed: return $"{Loc.T("skill.type.movespeed")}: +{specialValue}%";
            case SpecificSkillType.AttackSpeed: return $"{Loc.T("skill.type.atkspd")}: +{specialValue}%";
            case SpecificSkillType.AreaDamage: return $"{Loc.T("skill.type.areadmg")}: +{specialValue}%";
            case SpecificSkillType.Heal: return $"{Loc.T("skill.type.heal")}: {specialValue} {Loc.T("stat.hp")}";
            case SpecificSkillType.Projectile:
                if (isOrbitalProjectile)
                    return $"{Loc.T("skill.orbital_projectiles")}: {maxOrbitalProjectiles} | {Loc.T("skill.range")}: {orbitRadius}m | {Loc.T("stat.atk")}: +{attackBonus}";
                else
                    return $"{Loc.T("skill.projectiles")}: {projectileCount} | {Loc.T("stat.spd")}: {projectileSpeed} | {Loc.T("stat.atk")}: +{attackBonus}";
            case SpecificSkillType.Boomerang:
                return $"{Loc.T("skill.boomerang_label")}: {boomerangThrowRange}m {Loc.T("skill.range")} | {attackBonus} {Loc.T("skill.damage")} | {boomerangMaxTargets} {Loc.T("skill.targets")}";
            case SpecificSkillType.DamageReflection: return $"{Loc.T("skill.type.dmg_reflection")}: {specialValue}%";
            case SpecificSkillType.ElementalMastery: return $"{Loc.T("skill.type.elemental_mastery")}: +{specialValue}%";
            case SpecificSkillType.ChainLightning: return $"{Loc.T("skill.type.chain_lightning")}: {specialValue} {Loc.T("skill.targets")}";
            case SpecificSkillType.PoisonCloud: return $"{Loc.T("skill.type.poison_cloud")}: {specialValue}/s";
            case SpecificSkillType.FireAura: return $"{Loc.T("skill.type.fire_aura")}: {specialValue}/s";
            case SpecificSkillType.IceBarrier: return $"{Loc.T("skill.type.ice_barrier")}: {specialValue} {Loc.T("stat.def")}";
            case SpecificSkillType.WindDash: return $"{Loc.T("skill.type.wind_dash")}: +{specialValue}%";
            case SpecificSkillType.EarthStomp: return $"{Loc.T("skill.type.earth_stomp")}: {specialValue}";
            case SpecificSkillType.Shield:
                return $"{Loc.T("skill.type.shield_block")} {cooldown}s";
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
        // Implementar queimadura contínua
    }

    private void ApplyIceEffect(GameObject target, PlayerStats playerStats)
    {
        // Implementar lentidão
    }

    private void ApplyLightningEffect(GameObject target, PlayerStats playerStats)
    {
        // Implementar corrente elétrica
    }

    private void ApplyPoisonEffect(GameObject target, PlayerStats playerStats)
    {
        // Implementar dano contínuo
    }

    private void ApplyEarthEffect(GameObject target, PlayerStats playerStats)
    {
        // Implementar atordoamento
    }

    private void ApplyWindEffect(GameObject target, PlayerStats playerStats)
    {
        // Implementar repulsão
    }

    public bool EhSkillDeAtaque()
    {
        // Skills defensivas nunca contam como ataque, mesmo com attackBonus
        switch (specificType)
        {
            case SpecificSkillType.EscudoRotativo:
            case SpecificSkillType.EscudoEspinhoso:
            case SpecificSkillType.Shield:
            case SpecificSkillType.IceBarrier:
            case SpecificSkillType.Heal:
            case SpecificSkillType.HealthRegen:
            case SpecificSkillType.Aureola:
            case SpecificSkillType.BarreiraReflexiva:
            case SpecificSkillType.BarreiraEnergia:
            case SpecificSkillType.TeiaProtecao:
            case SpecificSkillType.InstintoSobrevivencia:
            case SpecificSkillType.EspelhoMagico:
            case SpecificSkillType.EscudoKarma:
            case SpecificSkillType.SegundaChance:
            case SpecificSkillType.FugaSombras:
                return false;
        }

        if (attackBonus > 0) return true;

        switch (specificType)
        {
            case SpecificSkillType.Projectile:
            case SpecificSkillType.Boomerang:
            case SpecificSkillType.EarthStomp:
            case SpecificSkillType.ChainLightning:
            case SpecificSkillType.PoisonCloud:
            case SpecificSkillType.FireAura:
            case SpecificSkillType.AreaDamage:
                return true;
            default:
                return false;
        }
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

        // 🚫 Skills NÃO modificam mais os status base do player.
        // Os valores da skill (attackBonus, healthBonus, defenseBonus, speedBonus,
        // healthRegenBonus, attackSpeedMultiplier) são o "valor próprio da skill",
        // consumido pelos behaviors dela; o status do player é somado por cima no
        // momento do efeito (ex.: dano = attackBonus + player.attack × fração;
        // escudo da BarreiraEnergia = healthBonus → shieldPoints).
        // Antes, estes bônus eram somados ao player aqui, inflando os status base —
        // foi exatamente esse comportamento que invertemos.
    }

    public void RemoveFromPlayer(PlayerStats playerStats)
    {
        if (playerStats == null) return;

        // Nada a remover: ApplyToPlayer não altera mais os status base do player.
        // (Ver comentário em ApplyToPlayer.)
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

    // 🆕 MÉTODOS PARA PROJÉTEIS ORBITAIS
    public bool IsOrbitalProjectileSkill()
    {
        return IsProjectileSkill() && isOrbitalProjectile;
    }

    public float GetOrbitalProjectileDamage()
    {
        return GetProjectileDamage() * (isOrbitalProjectile ? 1.2f : 1f); // Dano extra para orbitais
    }

    public bool ShouldUseOrbitalBehavior()
    {
        return IsProjectileSkill() && isOrbitalProjectile;
    }

    // 🆕 🌀 MÉTODOS ESPECÍFICOS PARA BUMERANGUE
    public bool IsBoomerangSkill()
    {
        return specificType == SpecificSkillType.Boomerang;
    }

    public float GetBoomerangDamage()
    {
        return attackBonus > 0 ? attackBonus : 25f;
    }

    public float GetBoomerangThrowRange()
    {
        return boomerangThrowRange > 0 ? boomerangThrowRange : 8f;
    }

    public float GetBoomerangThrowSpeed()
    {
        return boomerangThrowSpeed > 0 ? boomerangThrowSpeed : 15f;
    }

    public int GetBoomerangMaxTargets()
    {
        return boomerangMaxTargets > 0 ? boomerangMaxTargets : 3;
    }

    public bool ShouldHealOnReturn()
    {
        return boomerangHealOnReturn;
    }

    public float GetHealAmount(float damageDealt)
    {
        return damageDealt * boomerangHealPercent;
    }
    // 🆕 ADICIONAR JUNTO AOS OUTROS MÉTODOS "Is..." (como IsProjectileSkill)
    public bool IsShieldSkill()
    {
        return specificType == SpecificSkillType.Shield;
    }
}

// 🆕 🌀 NOVO ENUM PARA COMPORTAMENTO DE BUMERANGUE
public enum BoomerangBehaviorType
{
    ReturnToPlayer,     // Retorna diretamente ao jogador
    SeekNewTargets,     // Busca novos alvos antes de retornar
    OrbitThenReturn,    // Órbita antes de retornar
    SplitOnReturn,      // Divide-se ao retornar
    ExplodeOnReturn     // Explode ao retornar
}

// Enums existentes
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
    EarthStomp,
    Ultimate,
    Boomerang, // 🆕 NOVO TIPO DE SKILL
    EscudoRotativo,
    EscudoEspinhoso,
    Aureola,
    BarreiraReflexiva,
    CampoEspinhos,
    ChuvaEstrelas,
    GarrasAbismo,
    FuriaLaminas,
    SombrasCruz,
    CorteFantasma,
    SegundaChance,
    FugaSombras,
    BarreiraEnergia,
    LancaLuz,
    ChicoteEnergia,
    MisseisTeleguiados,
    PulsoRitmico,
    EspadaFantasma,
    CorrenteSombria,
    TeiaProtecao,
    InstintoSobrevivencia,
    EspelhoMagico,
    EscudoKarma,

    // Ultimates
    UltRaioCerteiro,
    UltTempestadeEletrica,
    UltChuvaMeteorosUlt,
    UltCampoDeGelo,
    UltVortice,
    UltNecropole,
    UltRitualAnciao,
    UltBencaoAnciao,
    UltCasuloCristal,
    UltCorrentesInferno,
    UltDrenagemDeVida,
    UltEscudoSonico,
    UltFormaBestial, // removida do jogo - mantida para não deslocar os valores dos enums seguintes
    UltPulsoMagnetico,
    UltPunicaoDivina,
    UltDomoRetardante,
    UltDespertarAnciao,

    // Novas skills de ataque
    CristaisGelo,

    UltMareImplacavel,
}

// 🆕 NOVO ENUM PARA TARGETING ORBITAL
public enum OrbitalTargetingMode
{
    NearestEnemy,
    RandomEnemy,
    FixedAngle,
    PlayerDirection
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
