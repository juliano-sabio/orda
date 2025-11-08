using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static PlayerStats;

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance;

    // ✅ CORREÇÃO: Headers removidos - usando comentários normais
    // Sistema de Skills
    public List<SkillData> availableSkills = new List<SkillData>();
    public List<SkillData> activeSkills = new List<SkillData>();
    public List<SkillModifier> activeModifiers = new List<SkillModifier>();

    // Configurações de Progressão
    public int[] levelUpMilestones = { 3, 6, 10, 15, 20 };
    public int skillsPerChoice = 3;

    // Eventos
    public event Action<List<SkillData>, Action<SkillData>> OnSkillChoiceRequired;
    public event Action<SkillData> OnSkillAcquired;
    public event Action<SkillModifier> OnModifierAcquired;

    private PlayerStats playerStats;
    private SkillChoiceUI skillChoiceUI;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        playerStats = FindAnyObjectByType<PlayerStats>();
        skillChoiceUI = FindAnyObjectByType<SkillChoiceUI>();

        SceneManager.sceneLoaded += OnSceneLoaded;

        LoadSkillData();
        Debug.Log("✅ SkillManager inicializado!");
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        skillChoiceUI = FindAnyObjectByType<SkillChoiceUI>();
        playerStats = FindAnyObjectByType<PlayerStats>();
    }

    public void OnPlayerLevelUp(int newLevel)
    {
        Debug.Log($"📈 Player atingiu nível {newLevel}");

        if (Array.Exists(levelUpMilestones, milestone => milestone == newLevel))
        {
            OfferSkillChoice();
        }

        ApplyLevelBonusToSkills(newLevel);
    }

    void OfferSkillChoice()
    {
        List<SkillData> choices = GetRandomSkillChoices(skillsPerChoice);

        if (choices.Count > 0 && skillChoiceUI != null)
        {
            Debug.Log($"🎯 Oferecendo {choices.Count} skills para escolha");
            OnSkillChoiceRequired?.Invoke(choices, OnSkillSelectedFromChoice);
        }
    }

    void OnSkillSelectedFromChoice(SkillData selectedSkill)
    {
        if (selectedSkill != null)
        {
            AddSkill(selectedSkill);
        }
    }

    List<SkillData> GetRandomSkillChoices(int count)
    {
        List<SkillData> choices = new List<SkillData>();
        List<SkillData> availableChoices = new List<SkillData>(availableSkills);

        availableChoices.RemoveAll(skill => activeSkills.Exists(s => s.skillName == skill.skillName));

        if (playerStats != null)
        {
            availableChoices.RemoveAll(skill => !skill.MeetsRequirements(playerStats.level, activeSkills));
        }

        for (int i = 0; i < Mathf.Min(count, availableChoices.Count); i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, availableChoices.Count);
            choices.Add(availableChoices[randomIndex]);
            availableChoices.RemoveAt(randomIndex);
        }

        return choices;
    }

    public void AddSkill(SkillData skill)
    {
        if (skill == null) return;

        if (!HasSkill(skill))
        {
            if (playerStats != null && !skill.MeetsRequirements(playerStats.level, activeSkills))
            {
                Debug.LogWarning($"❌ Skill {skill.skillName} não atende aos requisitos!");
                return;
            }

            activeSkills.Add(skill);
            ApplySkillToPlayer(skill);
            OnSkillAcquired?.Invoke(skill);

            Debug.Log($"✅ Skill adquirida: {skill.skillName}");

            if (UIManager.Instance != null)
                UIManager.Instance.ShowSkillAcquired(skill.skillName, skill.GetFullDescription());
        }
    }

    void ApplySkillToPlayer(SkillData skill)
    {
        if (playerStats == null) return;

        try
        {
            skill.ApplyToPlayer(playerStats);
            ApplySkillModifiers(skill);

            if (skill.element != PlayerStats.Element.None)
            {
                ApplyElementalEffects(skill);
            }

            switch (skill.skillType)
            {
                case SkillType.Attack:
                    ConfigureAttackSkill(skill);
                    break;
                case SkillType.Defense:
                    ConfigureDefenseSkill(skill);
                    break;
                case SkillType.Ultimate:
                    ConfigureUltimateSkill(skill);
                    break;
                case SkillType.Aura:
                    ConfigureAuraSkill(skill);
                    break;
                case SkillType.Active:
                    ConfigureActiveSkill(skill);
                    break;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Erro ao aplicar skill {skill.skillName}: {e.Message}");
        }
    }

    void ApplySkillModifiers(SkillData skill)
    {
        foreach (var modifierData in skill.skillModifiers)
        {
            if (modifierData.IsValid())
            {
                AddSkillModifier(modifierData);
            }
        }
    }

    void ConfigureAttackSkill(SkillData skill)
    {
        if (playerStats.attackSkills != null)
        {
            PlayerStats.AttackSkill newAttackSkill = new PlayerStats.AttackSkill
            {
                skillName = skill.skillName,
                baseDamage = skill.attackBonus,
                isActive = true,
                cooldown = skill.cooldown,
                element = skill.element
            };

            playerStats.attackSkills.Add(newAttackSkill);
        }
    }

    void ConfigureDefenseSkill(SkillData skill)
    {
        if (playerStats.defenseSkills != null)
        {
            PlayerStats.DefenseSkill newDefenseSkill = new PlayerStats.DefenseSkill
            {
                skillName = skill.skillName,
                baseDefense = skill.defenseBonus,
                isActive = true,
                duration = skill.duration,
                element = skill.element
            };

            playerStats.defenseSkills.Add(newDefenseSkill);
        }
    }

    void ConfigureUltimateSkill(SkillData skill)
    {
        if (playerStats.ultimateSkill != null)
        {
            playerStats.ultimateSkill.baseDamage += skill.attackBonus;
            playerStats.ultimateSkill.areaOfEffect += skill.specialValue;
        }
    }

    void ConfigureAuraSkill(SkillData skill)
    {
        // Implementar lógica de aura
    }

    void ConfigureActiveSkill(SkillData skill)
    {
        // Implementar lógica para skills ativas
    }

    void ApplyElementalEffects(SkillData skill)
    {
        if (playerStats.GetCurrentElement() == skill.element)
        {
            float elementalBonus = skill.elementalBonus;
            playerStats.attack *= elementalBonus;
            playerStats.defense *= elementalBonus;
        }
    }

    void ApplyLevelBonusToSkills(int level)
    {
        float levelBonus = 1 + (level * 0.02f);

        foreach (var skill in activeSkills)
        {
            if (skill.stackable && skill.maxStacks > 1)
            {
                skill.attackBonus *= levelBonus;
                skill.defenseBonus *= levelBonus;
            }
        }
    }

    public void AddSkillModifier(SkillModifierData modifierData)
    {
        if (modifierData.IsValid())
        {
            PlayerStats.SkillModifier modifier = modifierData.ToPlayerStatsModifier();
            AddSkillModifier(modifier);
        }
    }

    public void AddSkillModifier(PlayerStats.SkillModifier modifier)
    {
        if (modifier == null) return;

        if (!activeModifiers.Exists(m => m.modifierName == modifier.modifierName && m.targetSkillName == modifier.targetSkillName))
        {
            activeModifiers.Add(modifier);
            ApplyModifierToPlayer(modifier);
            OnModifierAcquired?.Invoke(modifier);

            if (UIManager.Instance != null)
                UIManager.Instance.ShowModifierAcquired(modifier.modifierName, modifier.targetSkillName);
        }
    }

    void ApplyModifierToPlayer(PlayerStats.SkillModifier modifier)
    {
        if (playerStats == null) return;

        try
        {
            playerStats.AddSkillModifier(modifier);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Erro ao aplicar modificador {modifier.modifierName}: {e.Message}");
        }
    }

    void LoadSkillData()
    {
        SkillData[] loadedSkills = Resources.LoadAll<SkillData>("Skills");
        foreach (var skill in loadedSkills)
        {
            if (skill.IsValid())
            {
                availableSkills.Add(skill);
            }
        }

        LoadActiveSkills();
    }

    void LoadActiveSkills()
    {
        int savedSkillsCount = PlayerPrefs.GetInt("ActiveSkillsCount", 0);

        for (int i = 0; i < savedSkillsCount; i++)
        {
            string skillName = PlayerPrefs.GetString($"ActiveSkill_{i}", "");
            SkillData skill = availableSkills.Find(s => s.skillName == skillName);

            if (skill != null)
            {
                activeSkills.Add(skill);
                if (playerStats != null)
                {
                    skill.ApplyToPlayer(playerStats);
                }
            }
        }
    }

    void SaveActiveSkills()
    {
        PlayerPrefs.SetInt("ActiveSkillsCount", activeSkills.Count);

        for (int i = 0; i < activeSkills.Count; i++)
        {
            PlayerPrefs.SetString($"ActiveSkill_{i}", activeSkills[i].skillName);
        }

        PlayerPrefs.Save();
    }

    public List<SkillData> GetAvailableSkills()
    {
        return new List<SkillData>(availableSkills);
    }

    public List<SkillData> GetActiveSkills()
    {
        return new List<SkillData>(activeSkills);
    }

    public List<SkillModifier> GetActiveModifiers()
    {
        return new List<SkillModifier>(activeModifiers);
    }

    public bool HasSkill(SkillData skill)
    {
        return activeSkills.Exists(s => s.skillName == skill.skillName);
    }

    public int GetSkillCountByType(SkillType type)
    {
        return activeSkills.FindAll(skill => skill.skillType == type).Count;
    }

    public int GetSkillCountByElement(PlayerStats.Element element)
    {
        return activeSkills.FindAll(skill => skill.element == element).Count;
    }

    [ContextMenu("Adicionar Skill Aleatória")]
    public void AddRandomSkill()
    {
        if (availableSkills.Count > 0)
        {
            List<SkillData> validSkills = availableSkills.FindAll(skill =>
                skill.MeetsRequirements(playerStats != null ? playerStats.level : 1, activeSkills) &&
                !HasSkill(skill)
            );

            if (validSkills.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, validSkills.Count);
                AddSkill(validSkills[randomIndex]);
            }
        }
    }

    [ContextMenu("Adicionar Modificador Aleatório")]
    public void AddRandomModifier()
    {
        string[] modifierNames = { "Fogo Intenso", "Gelo Penetrante", "Raio Carregado" };
        string[] targetSkills = { "Ataque Automático", "Golpe Contínuo" };

        string randomName = modifierNames[UnityEngine.Random.Range(0, modifierNames.Length)];
        string randomTarget = targetSkills[UnityEngine.Random.Range(0, targetSkills.Length)];

        PlayerStats.SkillModifier modifier = new PlayerStats.SkillModifier
        {
            modifierName = randomName,
            targetSkillName = randomTarget,
            damageMultiplier = 1.3f
        };

        AddSkillModifier(modifier);
    }

    [ContextMenu("Verificar Status da Integração")]
    public void CheckIntegrationStatus()
    {
        Debug.Log("🔍 Status da Integração do SkillManager:");
        Debug.Log($"• Skills Disponíveis: {availableSkills.Count}");
        Debug.Log($"• Skills Ativas: {activeSkills.Count}");
        Debug.Log($"• PlayerStats: {(playerStats != null ? "✅ Conectado" : "❌ Não encontrado")}");
    }

    [ContextMenu("Adicionar Skills de Teste")]
    public void AddTestSkills()
    {
        SkillData basicSkill = availableSkills.Find(s => s.requiredLevel <= 1);
        if (basicSkill != null)
        {
            AddSkill(basicSkill);
        }
    }

    [ContextMenu("Limpar Todas as Skills")]
    public void ClearAllSkills()
    {
        foreach (var skill in activeSkills)
        {
            if (playerStats != null)
            {
                skill.RemoveFromPlayer(playerStats);
            }
        }

        activeSkills.Clear();
        activeModifiers.Clear();

        if (playerStats != null)
        {
            playerStats.InitializeDefaultSkills();
        }
    }

    void OnApplicationQuit()
    {
        SaveActiveSkills();
    }

    void OnDestroy()
    {
        OnSkillChoiceRequired = null;
        OnSkillAcquired = null;
        OnModifierAcquired = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}