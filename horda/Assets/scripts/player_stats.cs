using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("⭐ Status Base do Jogador")]
    public float baseDamage = 10f;
    public float baseAttackSpeed = 1f;
    public float baseMoveSpeed = 5f;
    public float baseMaxHealth = 100f;
    public float baseDefense = 0f;

    [Header("📊 Status Atuais (Calculados)")]
    public float currentDamage { get; private set; }
    public float currentAttackSpeed { get; private set; }
    public float currentMoveSpeed { get; private set; }
    public float currentMaxHealth { get; private set; }
    public float currentDefense { get; private set; }

    [Header("💚 Sistema de Vida")]
    public float Hp_atual { get; private set; }
    public float Hp_max { get; private set; }

    [Header("🎯 Skills Ativas por Categoria")]
    public skilldata attackSkill;
    public skilldata defenseSkill;
    public skilldata ultimateSkill;

    // Dicionários para controle
    private Dictionary<skilldata, SkillModifiers> activeModifiers = new Dictionary<skilldata, SkillModifiers>();
    private Dictionary<SkillCategory, skilldata> activeSkillsByCategory = new Dictionary<SkillCategory, skilldata>();
    private Dictionary<skilldata, UltimateBehavior> activeUltimates = new Dictionary<skilldata, UltimateBehavior>();

    // Controle de Ultimate
    private float ultimateCooldownTimer = 0f;
    private bool isUltimateReady = true;

    private void Start()
    {
        // Inicializa sistema de vida
        Hp_max = baseMaxHealth;
        Hp_atual = Hp_max;

        // Inicializa o dicionário de categorias
        activeSkillsByCategory[SkillCategory.Ataque] = null;
        activeSkillsByCategory[SkillCategory.Defesa] = null;
        activeSkillsByCategory[SkillCategory.Ultimate] = null;

        // Inicializa com os status base
        CalculateFinalStats();
    }

    private void Update()
    {
        // Atualiza cooldown da ultimate
        if (!isUltimateReady && ultimateSkill != null)
        {
            ultimateCooldownTimer -= Time.deltaTime;
            if (ultimateCooldownTimer <= 0f)
            {
                isUltimateReady = true;
                Debug.Log("Ultimate pronta!");
            }
        }

        // Input para ativar ultimate (exemplo: tecla Q)
        if (Input.GetKeyDown(KeyCode.Q) && isUltimateReady && ultimateSkill != null)
        {
            ActivateUltimate();
        }
    }

    // 🔧 MÉTODO PRINCIPAL - ADICIONAR SKILL COM LIMITAÇÃO DE CATEGORIA
    public void AddSkill(skilldata skillData)
    {
        // Verifica se já tem uma skill desta categoria
        if (activeSkillsByCategory[skillData.category] != null)
        {
            // Remove a skill antiga da mesma categoria
            RemoveSkill(activeSkillsByCategory[skillData.category]);
        }

        Debug.Log($"🎉 Adicionando skill {skillData.skillName} na categoria {skillData.category}");

        // Adiciona a nova skill
        activeSkillsByCategory[skillData.category] = skillData;

        // Atualiza as referências públicas
        UpdateCategoryReferences();

        // ✅ CORREÇÃO: Mudei para ApplySkillModifiers que já existe
        ApplySkillModifiers(skillData);

        // Se for ultimate, inicializa o comportamento
        if (skillData.category == SkillCategory.Ultimate && skillData.ultimateBehavior != null)
        {
            AddUltimateBehavior(skillData);
        }

        // ✅ CORREÇÃO: Adicionei efeitos visuais e sonoros aqui
        if (skillData.visualEffect != null)
        {
            Instantiate(skillData.visualEffect, transform.position, Quaternion.identity);
        }
        if (skillData.soundEffect != null)
        {
            AudioSource.PlayClipAtPoint(skillData.soundEffect, transform.position);
        }
    }

    // 🗑️ REMOVER UMA SKILL
    public void RemoveSkill(skilldata skillData)
    {
        if (skillData == null) return;

        Debug.Log($"Removendo skill: {skillData.skillName}");

        // Remove dos modificadores ativos
        if (activeModifiers.ContainsKey(skillData))
        {
            activeModifiers.Remove(skillData);
        }

        // Remove do dicionário de categorias
        if (activeSkillsByCategory[skillData.category] == skillData)
        {
            activeSkillsByCategory[skillData.category] = null;
        }

        // Remove comportamento ultimate se existir
        if (activeUltimates.ContainsKey(skillData))
        {
            activeUltimates[skillData].DeactivateUltimate();
            Destroy(activeUltimates[skillData]);
            activeUltimates.Remove(skillData);
        }

        // Atualiza referências e recalcula status
        UpdateCategoryReferences();
        CalculateFinalStats();
    }

    // ⚡ ATIVAR ULTIMATE
    public void ActivateUltimate()
    {
        if (ultimateSkill == null || !isUltimateReady) return;

        Debug.Log($"🔥 Ativando Ultimate: {ultimateSkill.skillName}");

        // Ativa o comportamento da ultimate
        if (activeUltimates.ContainsKey(ultimateSkill))
        {
            Vector2 playerPosition = transform.position;
            activeUltimates[ultimateSkill].ActivateUltimate(playerPosition);
        }

        // Inicia cooldown
        isUltimateReady = false;
        ultimateCooldownTimer = ultimateSkill.ultimateCooldown;

        // Efeito visual e sonoro
        if (ultimateSkill.visualEffect != null)
        {
            Instantiate(ultimateSkill.visualEffect, transform.position, Quaternion.identity);
        }
        if (ultimateSkill.soundEffect != null)
        {
            AudioSource.PlayClipAtPoint(ultimateSkill.soundEffect, transform.position);
        }
    }

    // 🔧 APLICA OS EFEITOS DE UMA SKILL 
    public void ApplySkillModifiers(skilldata skillData)
    {
        // Para skills de Ataque e Defesa, aplica os modificadores
        if (skillData.category != SkillCategory.Ultimate)
        {
            SkillModifiers modifiers = new SkillModifiers
            {
                damageMultiplier = skillData.damageMultiplier,
                attackSpeedMultiplier = skillData.attackSpeedMultiplier,
                moveSpeedMultiplier = skillData.moveSpeedMultiplier,
                healthBonus = skillData.healthBonus,
                defenseBonus = skillData.defenseBonus
            };

            activeModifiers.Add(skillData, modifiers);
        }

        CalculateFinalStats();
    }

    // 🗑️ MÉTODO CHAMADO QUANDO UMA SKILL É REMOVIDA
    public void RemoveSkillModifiers(skilldata skillData)
    {
        if (activeModifiers.ContainsKey(skillData))
        {
            Debug.Log($"Removendo modificadores da skill: {skillData.skillName}");
            activeModifiers.Remove(skillData);
            CalculateFinalStats();
        }
    }

    // 🎯 ADICIONA COMPORTAMENTO ULTIMATE
    private void AddUltimateBehavior(skilldata skillData)
    {
        UltimateBehavior behavior = gameObject.AddComponent(skillData.ultimateBehavior.GetType()) as UltimateBehavior;
        behavior.Initialize(this, skillData.ultimateRadius, skillData.ultimateDuration);
        activeUltimates.Add(skillData, behavior);
    }

    // 🔄 ATUALIZA REFERÊNCIAS PÚBLICAS
    private void UpdateCategoryReferences()
    {
        attackSkill = activeSkillsByCategory[SkillCategory.Ataque];
        defenseSkill = activeSkillsByCategory[SkillCategory.Defesa];
        ultimateSkill = activeSkillsByCategory[SkillCategory.Ultimate];
    }

    // 🧮 CALCULA OS STATUS FINAIS COMBINANDO TODAS AS SKILLS
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

    // 💥 MÉTODO PARA RECEBER DANO
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

    // 🏥 MÉTODO PARA CURAR
    public void Curar(float quantidade)
    {
        Hp_atual = Mathf.Min(Hp_atual + quantidade, Hp_max);
        Debug.Log($"Player curado! Vida atual: {Hp_atual}");
    }

    // 💀 MÉTODO CHAMADO QUANDO O PLAYER MORRE
    private void Morrer()
    {
        Debug.Log("Player morreu!");
        // Aqui você pode chamar o Game Manager
        // GameManager.Instance.GameOver();
    }

    // 📊 MÉTODOS PARA OUTROS SCRIPTS CONSULTAREM OS STATUS
    public float GetDamage() => currentDamage;
    public float GetAttackSpeed() => currentAttackSpeed;
    public float GetMoveSpeed() => currentMoveSpeed;
    public float GetHealth() => Hp_atual;
    public float GetMaxHealth() => Hp_max;
    public float GetDefense() => currentDefense;
    public bool IsUltimateReady() => isUltimateReady;
    public float GetUltimateCooldownPercent() => isUltimateReady ? 1f : 1f - (ultimateCooldownTimer / ultimateSkill.ultimateCooldown);

    // 🔄 MÉTODO PARA RESTAURAR VIDA COMPLETA
    public void RestaurarVidaCompleta()
    {
        Hp_atual = Hp_max;
    }

    // 🔍 VERIFICAR SE PODE ADICIONAR SKILL (útil para UI)
    public bool CanAddSkill(skilldata skillData)
    {
        // Sempre pode adicionar se não tiver skill da mesma categoria
        return activeSkillsByCategory[skillData.category] == null;
    }

    // 🔍 VERIFICAR SKILL ATUAL EM UMA CATEGORIA
    public skilldata GetCurrentSkill(SkillCategory category)
    {
        return activeSkillsByCategory[category];
    }
}

// 🏷️ Estrutura para os modificadores de skills
[System.Serializable]
public struct SkillModifiers
{
    public float damageMultiplier;
    public float attackSpeedMultiplier;
    public float moveSpeedMultiplier;
    public float healthBonus;
    public float defenseBonus;
}