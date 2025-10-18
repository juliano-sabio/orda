
using UnityEngine;

// Cria um menu no Unity para criar novas skills
[CreateAssetMenu(fileName = "New Skill", menuName = "Skills/Skill Data")]
public class skilldata : ScriptableObject
{
    [Header("Informações Básicas")]
    public string skillName;          // Nome da skill
    public string description;        // Descrição que aparece na UI
    public Sprite icon;               // Ícone da skill

    [Header("Modificadores de Status")]
    [Tooltip("Multiplicador de dano (1 = normal, 2 = dobro)")]
    public float damageMultiplier = 1f;

    [Tooltip("Multiplicador de velocidade de ataque")]
    public float attackSpeedMultiplier = 1f;

    [Tooltip("Multiplicador de velocidade de movimento")]
    public float moveSpeedMultiplier = 1f;

    [Tooltip("Bonus de vida adicional")]
    public float healthBonus = 0f;

    [Tooltip("Bonus de defesa")]
    public float defenseBonus = 0f;

    [Header("Efeitos Visuais e Sonoros")]
    public GameObject visualEffect;   // Prefab de efeito visual
    public AudioClip soundEffect;     // Som quando pega a skill

    [Header("Comportamento Especial")]
    [Tooltip("Comportamento customizado da skill (opcional)")]
    public skilldata behavior;    // Script de comportamento especial
}