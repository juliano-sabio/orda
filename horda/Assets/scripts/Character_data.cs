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

    [Header("🆕 Sistema de Regeneração")]
    [Range(0.5f, 5f)]
    public float baseHealthRegen = 1f;

    [Range(3f, 10f)]
    public float baseRegenDelay = 5f;

    [Header("🆕 Sistema de Cooldowns")]
    [Range(0.5f, 3f)]
    public float baseAttackCooldown = 2f;

    [Range(1f, 5f)]
    public float baseDefenseCooldown = 3f;

    [Header("Sistema de Elementos")]
    public PlayerStats.Element baseElement = PlayerStats.Element.None;
    public List<PlayerStats.Element> strongAgainst = new List<PlayerStats.Element>();
    public List<PlayerStats.Element> weakAgainst = new List<PlayerStats.Element>();

    [Header("Ultimate Específica")]
    public UltimateData ultimateSkill;

    [Header("Comportamentos Especiais")]
    public SkillBehavior specialSkillBehavior;
    public UltimateBehavior ultimateBehavior;

    [Header("Progressão")]
    public int unlockLevel = 1;
    public float xpMultiplier = 1.5f;

    [Header("🆕 Bônus por Elemento")]
    [Range(0f, 10f)]
    public float elementAttackBonus = 0f;

    [Range(0f, 10f)]
    public float elementDefenseBonus = 0f;

    [Range(0f, 5f)]
    public float elementSpeedBonus = 0f;

    [Range(0f, 2f)]
    public float elementCooldownReduction = 0f;

    [Header("Customização Visual")]
    public Color characterColor = Color.white;
    public ParticleSystem.MinMaxGradient auraColor;
    public RuntimeAnimatorController animatorController;

    // 🆕 MÉTODOS DE CONVENIÊNCIA ATUALIZADOS
    public string GetElementIcon()
    {
        return GetElementIcon(baseElement);
    }

    public static string GetElementIcon(PlayerStats.Element element)
    {
        switch (element)
        {
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
        return GetElementColor(baseElement);
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

    public bool HasUltimate()
    {
        return ultimateSkill != null;
    }

    // 🆕 MÉTODOS PARA BÔNUS DE ELEMENTO
    public float GetElementAttackBonus()
    {
        return elementAttackBonus;
    }

    public float GetElementDefenseBonus()
    {
        return elementDefenseBonus;
    }

    public float GetElementSpeedBonus()
    {
        return elementSpeedBonus;
    }

    public float GetElementCooldownReduction()
    {
        return elementCooldownReduction;
    }

    // 🆕 MÉTODO PARA OBTER TODOS OS STATUS BASE
    public Dictionary<string, float> GetBaseStats()
    {
        return new Dictionary<string, float>
        {
            { "Health", maxHealth },
            { "Attack", baseAttack },
            { "Defense", baseDefense },
            { "Speed", baseSpeed },
            { "HealthRegen", baseHealthRegen },
            { "AttackCooldown", baseAttackCooldown },
            { "DefenseCooldown", baseDefenseCooldown }
        };
    }

    // 🆕 MÉTODO PARA OBTER DESCRIÇÃO DOS BÔNUS DO ELEMENTO
    public string GetElementBonusDescription()
    {
        if (baseElement == PlayerStats.Element.None)
            return "Sem bônus elemental";

        List<string> bonuses = new List<string>();

        if (elementAttackBonus > 0)
            bonuses.Add($"+{elementAttackBonus} Ataque");
        if (elementDefenseBonus > 0)
            bonuses.Add($"+{elementDefenseBonus} Defesa");
        if (elementSpeedBonus > 0)
            bonuses.Add($"+{elementSpeedBonus} Velocidade");
        if (elementCooldownReduction > 0)
            bonuses.Add($"-{elementCooldownReduction * 100}% Cooldown");

        return bonuses.Count > 0 ? string.Join(" | ", bonuses) : "Bônus elemental passivo";
    }
}