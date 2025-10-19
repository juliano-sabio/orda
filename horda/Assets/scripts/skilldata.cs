using UnityEngine;

// 🎯 Categorias de Skills
public enum SkillCategory
{
    Ataque,     // Skills de dano, crítico, velocidade de ataque
    Defesa,     // Skills de vida, defesa, escudo, regeneração
    Ultimate    // Skills especiais com comportamentos únicos em área
}

// 🎯 Skill Data com Categoria
[CreateAssetMenu(fileName = "New Skill", menuName = "Skills/Skill Data")]
public class skilldata : ScriptableObject
{
    [Header("Informações Básicas")]
    public string skillName;
    public string description;
    public Sprite icon;
    public SkillCategory category;

    [Header("Modificadores de Status")]
    public float damageMultiplier = 1f;
    public float attackSpeedMultiplier = 1f;
    public float moveSpeedMultiplier = 1f;
    public float healthBonus = 0f;
    public float defenseBonus = 0f;

    [Header("Efeitos Visuais e Sonoros")]
    public GameObject visualEffect;
    public AudioClip soundEffect;

    [Header("Comportamento Especial")]
    public SkillBehavior behavior;

    [Header("Configurações Ultimate (apenas para categoria Ultimate)")]
    public UltimateBehavior ultimateBehavior;
    public float ultimateRadius = 5f;
    public float ultimateDuration = 3f;
    public float ultimateCooldown = 10f;
}