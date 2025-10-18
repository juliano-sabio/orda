using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("? Status Base do Jogador")]
    public float baseDamage = 10f;
    public float baseAttackSpeed = 1f;
    public float baseMoveSpeed = 5f;
    public float baseMaxHealth = 100f;
    public float baseDefense = 0f;

    [Header("?? Status Atuais (Calculados)")]
    public float currentDamage { get; private set; }
    public float currentAttackSpeed { get; private set; }
    public float currentMoveSpeed { get; private set; }
    public float currentMaxHealth { get; private set; }
    public float currentDefense { get; private set; }

    [Header("?? Sistema de Vida")]
    public float Hp_atual { get; private set; }
    public float Hp_max { get; private set; }

    // Dicionário para guardar todas as modificações ativas
    private Dictionary<skilldata, SkillModifiers> activeModifiers = new Dictionary<skilldata, SkillModifiers>();

    private void Start()
    {
        // Inicializa sistema de vida
        Hp_max = baseMaxHealth;
        Hp_atual = Hp_max;

        // Inicializa com os status base
        CalculateFinalStats();
    }

    // ?? MÉTODO CHAMADO QUANDO O JOGADOR GANHA UMA SKILL
    public void ApplySkillModifiers(skilldata skillData)
    {
        Debug.Log($"Aplicando modificadores da skill: {skillData.skillName}");

        SkillModifiers modifiers = new SkillModifiers
        {
            damageMultiplier = skillData.damageMultiplier,
            attackSpeedMultiplier = skillData.attackSpeedMultiplier,
            moveSpeedMultiplier = skillData.moveSpeedMultiplier,
            healthBonus = skillData.healthBonus,
            defenseBonus = skillData.defenseBonus
        };

        activeModifiers.Add(skillData, modifiers);
        CalculateFinalStats();
    }

    // ??? MÉTODO CHAMADO QUANDO UMA SKILL É REMOVIDA
    public void RemoveSkillModifiers(skilldata skillData)
    {
        if (activeModifiers.ContainsKey(skillData))
        {
            Debug.Log($"Removendo modificadores da skill: {skillData.skillName}");
            activeModifiers.Remove(skillData);
            CalculateFinalStats();
        }
    }

    // ?? CALCULA OS STATUS FINAIS COMBINANDO TODAS AS SKILLS
    private void CalculateFinalStats()
    {
        float totalDamageMultiplier = 1f;
        float totalAttackSpeedMultiplier = 1f;
        float totalMoveSpeedMultiplier = 1f;
        float totalHealthBonus = 0f;
        float totalDefenseBonus = 0f;

        foreach (var modifier in activeModifiers.Values)
        {
            totalDamageMultiplier *= modifier.damageMultiplier;
            totalAttackSpeedMultiplier *= modifier.attackSpeedMultiplier;
            totalMoveSpeedMultiplier *= modifier.moveSpeedMultiplier;
            totalHealthBonus += modifier.healthBonus;
            totalDefenseBonus += modifier.defenseBonus;
        }

        currentDamage = baseDamage * totalDamageMultiplier;
        currentAttackSpeed = baseAttackSpeed * totalAttackSpeedMultiplier;
        currentMoveSpeed = baseMoveSpeed * totalMoveSpeedMultiplier;
        currentMaxHealth = baseMaxHealth + totalHealthBonus;
        currentDefense = baseDefense + totalDefenseBonus;

        // Atualiza vida mantendo a porcentagem
        float percentHealth = Hp_atual / Hp_max;
        Hp_max = currentMaxHealth;
        Hp_atual = Hp_max * percentHealth;

        Debug.Log($"Status atualizados - Dano: {currentDamage}, Velocidade: {currentMoveSpeed}");
    }

    // ?? MÉTODO PARA RECEBER DANO
    public void ReceberDano(float quantidade)
    {
        float danoFinal = Mathf.Max(quantidade - currentDefense, 1f);
        Hp_atual -= danoFinal;

        Debug.Log($"Player levou {danoFinal} de dano! Vida atual: {Hp_atual}");

        if (Hp_atual <= 0)
        {
            Morrer();
        }
    }

    // ?? MÉTODO PARA CURAR
    public void Curar(float quantidade)
    {
        Hp_atual = Mathf.Min(Hp_atual + quantidade, Hp_max);
        Debug.Log($"Player curado! Vida atual: {Hp_atual}");
    }

    // ?? MÉTODO CHAMADO QUANDO O PLAYER MORRE
    private void Morrer()
    {
        Debug.Log("Player morreu!");
        // Aqui você pode chamar o Game Manager
        // GameManager.Instance.GameOver();
    }

    // ?? MÉTODOS PARA OUTROS SCRIPTS CONSULTAREM OS STATUS
    public float GetDamage() => currentDamage;
    public float GetAttackSpeed() => currentAttackSpeed;
    public float GetMoveSpeed() => currentMoveSpeed;
    public float GetHealth() => Hp_atual;
    public float GetMaxHealth() => Hp_max;
    public float GetDefense() => currentDefense;

    // ?? MÉTODO PARA RESTAURAR VIDA COMPLETA
    public void RestaurarVidaCompleta()
    {
        Hp_atual = Hp_max;
    }
}

// ??? Estrutura para os modificadores de skills
[System.Serializable]
public struct SkillModifiers
{
    public float damageMultiplier;
    public float attackSpeedMultiplier;
    public float moveSpeedMultiplier;
    public float healthBonus;
    public float defenseBonus;
}