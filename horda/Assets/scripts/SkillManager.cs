using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static PlayerStats;

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance;

    // Sistema de Skills
    public List<SkillData> availableSkills = new List<SkillData>();
    public List<SkillData> activeSkills = new List<SkillData>();
    public List<SkillModifier> activeModifiers = new List<SkillModifier>();

    // Configurações de Progressão
    public int[] levelUpMilestones = { 1, 3, 6, 10, 15, 20 };
    public int skillsPerChoice = 3;
    public bool allowDuplicateChoices = false;

    [Header("Configurações de Escolha Inicial")]
    public bool alwaysOfferInitialChoice = false; // 🎯 Controlado pelos milestones agora

    // Eventos
    public event Action<List<SkillData>, Action<SkillData>> OnSkillChoiceRequired;
    public event Action<SkillData> OnSkillAcquired;
    public event Action<SkillModifier> OnModifierAcquired;

    private PlayerStats playerStats;
    private SkillChoiceUI skillChoiceUI;
    private bool initialChoiceOffered = false;
    private List<SkillData> recentlyOfferedSkills = new List<SkillData>();

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
        Debug.Log("🔄 SkillManager iniciando...");

        playerStats = FindAnyObjectByType<PlayerStats>();
        skillChoiceUI = FindSkillChoiceUIInUIManager();

        if (skillChoiceUI == null)
        {
            Debug.LogError("❌ SkillChoiceUI não encontrado no UIManager!");
            skillChoiceUI = FindAnyObjectByType<SkillChoiceUI>();
        }

        if (skillChoiceUI != null)
        {
            Debug.Log($"✅ SkillChoiceUI encontrado: {skillChoiceUI.gameObject.name}");
            RegisterSkillChoiceListener(skillChoiceUI.ShowSkillChoice);
        }
        else
        {
            Debug.LogError("❌ SkillChoiceUI não encontrado após todas as tentativas!");
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        LoadSkillData();

        Debug.Log("✅ SkillManager inicializado!");

        StartCoroutine(DelayedInitialCheck());
    }

    private IEnumerator DelayedInitialCheck()
    {
        yield return new WaitForSeconds(1.0f);

        if (playerStats == null)
        {
            playerStats = FindAnyObjectByType<PlayerStats>();
        }

        if (skillChoiceUI == null)
        {
            skillChoiceUI = FindSkillChoiceUIInUIManager();
            if (skillChoiceUI != null)
            {
                RegisterSkillChoiceListener(skillChoiceUI.ShowSkillChoice);
            }
        }

        CheckForInitialSkillChoice();
    }

    private void CheckForInitialSkillChoice()
    {
        Debug.Log($"🔍 Verificando escolha inicial - Level: {playerStats?.level}, JáOferecida: {initialChoiceOffered}");

        // 🎯 OFERECE APENAS SE LEVEL 1 FOR MILESTONE
        bool level1IsMilestone = Array.Exists(levelUpMilestones, milestone => milestone == 1);

        if (!initialChoiceOffered && playerStats != null && playerStats.level == 1 && level1IsMilestone)
        {
            Debug.Log("🎯 Player começou no nível 1 (milestone) - oferecendo escolha inicial de 3 skills!");
            initialChoiceOffered = true;

            StartCoroutine(DelayedInitialChoice());
        }
        else if (playerStats != null && playerStats.level == 1)
        {
            Debug.Log($"ℹ️ Level 1 não é milestone - sem escolha inicial. Milestones: {string.Join(", ", levelUpMilestones)}");
        }
    }

    private IEnumerator DelayedInitialChoice()
    {
        yield return new WaitForSeconds(1.5f);
        OfferSkillChoice();
    }

    private SkillChoiceUI FindSkillChoiceUIInUIManager()
    {
        UIManager uiManager = FindAnyObjectByType<UIManager>();

        if (uiManager != null)
        {
            Debug.Log($"✅ UIManager encontrado: {uiManager.gameObject.name}");

            SkillChoiceUI skillUI = uiManager.GetComponent<SkillChoiceUI>();
            if (skillUI != null)
            {
                Debug.Log("✅ SkillChoiceUI encontrado como componente do UIManager");
                return skillUI;
            }

            skillUI = uiManager.GetComponentInChildren<SkillChoiceUI>(true);
            if (skillUI != null)
            {
                Debug.Log("✅ SkillChoiceUI encontrado nos children do UIManager");
                return skillUI;
            }

            Debug.LogWarning("⚠️ SkillChoiceUI não encontrado como componente ou child do UIManager");
        }
        else
        {
            Debug.LogError("❌ UIManager não encontrado na cena!");
        }

        return null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"🔄 SkillManager: Cena carregada - {scene.name}");

        playerStats = FindAnyObjectByType<PlayerStats>();
        skillChoiceUI = FindSkillChoiceUIInUIManager();

        if (skillChoiceUI != null)
        {
            Debug.Log("✅ SkillChoiceUI reconectado após carregar cena");
            RegisterSkillChoiceListener(skillChoiceUI.ShowSkillChoice);
        }
    }

    public void OnPlayerLevelUp(int newLevel)
    {
        Debug.Log($"📈 Player atingiu nível {newLevel} - Verificando milestones...");

        if (playerStats == null)
        {
            playerStats = FindAnyObjectByType<PlayerStats>();
            if (playerStats == null)
            {
                Debug.LogError("❌ PlayerStats null no level up!");
                return;
            }
        }

        // 🎯 VERIFICA SE É UM MILESTONE - SÓ OFERECE NOS NÍVEIS CONFIGURADOS
        bool isMilestone = Array.Exists(levelUpMilestones, milestone => milestone == newLevel);
        Debug.Log($"🎯 Level {newLevel} é milestone: {isMilestone}");

        if (isMilestone)
        {
            Debug.Log($"🎯 Oferecendo escolha de 3 skills aleatórias para milestone nível {newLevel}");
            StartCoroutine(OfferSkillChoiceWithDelay());
        }
        else
        {
            Debug.Log($"ℹ️ Level {newLevel} não é milestone - sem escolha de skills");
        }

        ApplyLevelBonusToSkills(newLevel);
    }

    private IEnumerator OfferSkillChoiceWithDelay()
    {
        yield return new WaitForSeconds(0.5f);
        OfferSkillChoice();
    }

    void OfferSkillChoice()
    {
        Debug.Log("🎯 Iniciando OfferSkillChoice para milestone...");

        if (playerStats == null)
        {
            Debug.LogError("❌ PlayerStats null no OfferSkillChoice!");
            return;
        }

        if (skillChoiceUI == null)
        {
            Debug.LogError("❌ SkillChoiceUI null no OfferSkillChoice!");
            return;
        }

        // 🎯 SEMPRE 3 SKILLS ALEATÓRIAS
        List<SkillData> choices = GetRandomSkillChoices(skillsPerChoice);

        if (choices.Count == 0)
        {
            Debug.LogWarning("⚠️ Nenhuma skill disponível para escolha!");
            return;
        }

        Debug.Log($"🎯 Oferecendo {choices.Count} skills aleatórias no milestone nível {playerStats.level}");

        // Mostra no console as skills oferecidas
        foreach (var skill in choices)
        {
            Debug.Log($"   ➤ {skill.skillName} ({skill.element}) - {skill.specificType}");
        }

        if (OnSkillChoiceRequired != null)
        {
            Debug.Log($"📡 Evento OnSkillChoiceRequired tem {OnSkillChoiceRequired.GetInvocationList().Length} listeners");
            OnSkillChoiceRequired?.Invoke(choices, OnSkillSelectedFromChoice);
        }
        else
        {
            Debug.LogError("❌ Nenhum listener registrado no evento OnSkillChoiceRequired!");

            if (skillChoiceUI != null)
            {
                Debug.Log("🔄 Tentando fallback - chamando SkillChoiceUI diretamente");
                skillChoiceUI.ShowSkillChoice(choices, OnSkillSelectedFromChoice);
            }
            else
            {
                ShowFallbackSkillChoice(choices);
            }
        }
    }

    private void ShowFallbackSkillChoice(List<SkillData> choices)
    {
        Debug.Log("🎯 ESCOLHA UMA SKILL (Fallback - UI não disponível):");
        for (int i = 0; i < choices.Count; i++)
        {
            Debug.Log($"{i + 1}. {choices[i].skillName} - {choices[i].description}");
        }
        Debug.Log("💡 Use: SkillManager.Instance.AddSkill(choices[0])");
    }

    void OnSkillSelectedFromChoice(SkillData selectedSkill)
    {
        if (selectedSkill != null)
        {
            Debug.Log($"✅ Skill selecionada: {selectedSkill.skillName}");
            AddSkill(selectedSkill);
        }
        else
        {
            Debug.LogError("❌ Skill selecionada é null!");
        }
    }

    List<SkillData> GetRandomSkillChoices(int count)
    {
        List<SkillData> choices = new List<SkillData>();
        List<SkillData> availableChoices = new List<SkillData>(availableSkills);

        // Remove skills que o player JÁ TEM
        availableChoices.RemoveAll(skill => activeSkills.Exists(s => s.skillName == skill.skillName));

        // Remove skills recentemente oferecidas para variedade
        if (allowDuplicateChoices == false && recentlyOfferedSkills.Count > 0)
        {
            availableChoices.RemoveAll(skill => recentlyOfferedSkills.Contains(skill));
        }

        // Se não tem skills suficientes, reseta a lista de recentes
        if (availableChoices.Count < count && recentlyOfferedSkills.Count > 0)
        {
            Debug.Log("🔄 Poucas skills disponíveis - resetando lista de skills recentes");
            recentlyOfferedSkills.Clear();
            availableChoices = new List<SkillData>(availableSkills);
            availableChoices.RemoveAll(skill => activeSkills.Exists(s => s.skillName == skill.skillName));
        }

        // 🎯 ESCOLHE 3 SKILLS ALEATÓRIAS
        for (int i = 0; i < Mathf.Min(count, availableChoices.Count); i++)
        {
            if (availableChoices.Count == 0) break;

            int randomIndex = UnityEngine.Random.Range(0, availableChoices.Count);
            SkillData chosenSkill = availableChoices[randomIndex];

            choices.Add(chosenSkill);
            availableChoices.RemoveAt(randomIndex);

            // Adiciona às skills recentemente oferecidas
            if (!recentlyOfferedSkills.Contains(chosenSkill))
            {
                recentlyOfferedSkills.Add(chosenSkill);
            }
        }

        Debug.Log($"🔍 {choices.Count} skills aleatórias selecionadas de {availableSkills.Count} disponíveis");
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

            // Aplica a skill ao PlayerStats se ele existir
            if (playerStats != null)
            {
                playerStats.ApplyAcquiredSkill(skill);
            }
            else
            {
                Debug.LogWarning("⚠️ PlayerStats não encontrado para aplicar a skill");
            }

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
            ConfigureSkillBehavior(skill);

            if (skill.element != PlayerStats.Element.None)
            {
                ApplyElementalEffects(skill);
            }

            Debug.Log($"✨ Skill {skill.skillName} aplicada ao player");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Erro ao aplicar skill {skill.skillName}: {e.Message}");
        }
    }

    void ConfigureSkillBehavior(SkillData skill)
    {
        switch (skill.specificType)
        {
            case SpecificSkillType.Projectile:
                AddProjectileBehavior(skill);
                break;
            case SpecificSkillType.HealthRegen:
                Debug.Log($"🔄 Skill de regeneração configurada: {skill.skillName}");
                break;
            case SpecificSkillType.CriticalStrike:
                Debug.Log($"🎯 Skill de crítico configurada: {skill.skillName}");
                break;
        }

        if (skill.isPassive)
        {
            Debug.Log($"🔄 Skill Passiva configurada: {skill.skillName}");
        }
    }

    void AddProjectileBehavior(SkillData skill)
    {
        var existingBehavior = GetComponent<PassiveProjectileSkill2D>();
        if (existingBehavior != null)
        {
            Debug.Log($"⚡ Comportamento de projétil já existe - melhorando...");
            existingBehavior.activationInterval = Mathf.Max(0.5f, existingBehavior.activationInterval * 0.8f);
            return;
        }

        PassiveProjectileSkill2D projectileBehavior = gameObject.AddComponent<PassiveProjectileSkill2D>();

        // ✅ CORREÇÃO: Inicialização robusta
        bool initialized = false;

        try
        {
            var initializeMethod = projectileBehavior.GetType().GetMethod("InitializeWithSkillData");
            if (initializeMethod != null)
            {
                initializeMethod.Invoke(projectileBehavior, new object[] { playerStats, skill });
                initialized = true;
                Debug.Log($"✅ Comportamento inicializado com InitializeWithSkillData");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ InitializeWithSkillData falhou: {e.Message}");
        }

        if (!initialized)
        {
            try
            {
                projectileBehavior.Initialize(playerStats);

                var updateMethod = projectileBehavior.GetType().GetMethod("UpdateFromSkillData");
                if (updateMethod != null)
                {
                    updateMethod.Invoke(projectileBehavior, new object[] { skill });
                }

                initialized = true;
                Debug.Log($"✅ Comportamento inicializado com Initialize + UpdateFromSkillData");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"⚠️ Inicialização fallback falhou: {e.Message}");
            }
        }

        if (!initialized)
        {
            projectileBehavior.Initialize(playerStats);
            Debug.Log($"✅ Comportamento inicializado com método básico");
        }

        Debug.Log($"✅ Comportamento de projétil 2D adicionado: {skill.skillName}");
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

            // Aplica o modificador ao PlayerStats
            if (playerStats != null)
            {
                playerStats.AddSkillModifier(modifier);
            }

            OnModifierAcquired?.Invoke(modifier);

            if (UIManager.Instance != null)
                UIManager.Instance.ShowModifierAcquired(modifier.modifierName, modifier.targetSkillName);
        }
    }

    // 🆕 MÉTODO AddRandomModifier
    [ContextMenu("Adicionar Modificador Aleatório")]
    public void AddRandomModifier()
    {
        string[] modifierNames = { "Fogo Intenso", "Gelo Penetrante", "Raio Carregado", "Veneno Mortal" };
        string[] targetSkills = { "Ataque Automático", "Golpe Contínuo", "Proteção Passiva" };

        string randomName = modifierNames[UnityEngine.Random.Range(0, modifierNames.Length)];
        string randomTarget = targetSkills[UnityEngine.Random.Range(0, targetSkills.Length)];

        PlayerStats.SkillModifier modifier = new PlayerStats.SkillModifier
        {
            modifierName = randomName,
            targetSkillName = randomTarget,
            damageMultiplier = 1.3f,
            defenseMultiplier = 1.2f,
            element = (PlayerStats.Element)UnityEngine.Random.Range(1, 6)
        };

        AddSkillModifier(modifier);
        Debug.Log($"✨ Modificador aleatório adicionado: {randomName} para {randomTarget}");
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

        if (availableSkills.Count == 0)
        {
            Debug.LogWarning("⚠️ Nenhuma skill encontrada em Resources/Skills/");
        }
        else
        {
            Debug.Log($"✅ {availableSkills.Count} skills carregadas");
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

    // MÉTODOS PÚBLICOS PARA REGISTRO DE EVENTOS
    public void RegisterSkillChoiceListener(Action<List<SkillData>, Action<SkillData>> listener)
    {
        if (listener != null)
        {
            OnSkillChoiceRequired += listener;
            Debug.Log($"✅ Listener registrado no OnSkillChoiceRequired - Total: {OnSkillChoiceRequired?.GetInvocationList().Length ?? 0}");
        }
    }

    public void UnregisterSkillChoiceListener(Action<List<SkillData>, Action<SkillData>> listener)
    {
        if (listener != null)
        {
            OnSkillChoiceRequired -= listener;
            Debug.Log($"✅ Listener removido do OnSkillChoiceRequired - Total: {OnSkillChoiceRequired?.GetInvocationList().Length ?? 0}");
        }
    }

    [ContextMenu("🔧 Forçar Registro de Eventos")]
    public void ForceEventRegistration()
    {
        Debug.Log("🔧 Forçando registro de eventos...");

        SkillChoiceUI[] allSkillUIs = FindObjectsByType<SkillChoiceUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"🔍 Encontrados {allSkillUIs.Length} SkillChoiceUI na cena");

        foreach (var skillUI in allSkillUIs)
        {
            if (skillUI != null)
            {
                RegisterSkillChoiceListener(skillUI.ShowSkillChoice);

                if (!skillUI.gameObject.activeInHierarchy)
                {
                    skillUI.gameObject.SetActive(true);
                    Debug.Log($"✅ Ativado: {skillUI.gameObject.name}");
                }

                Debug.Log($"✅ Registrado: {skillUI.gameObject.name}");
            }
        }

        Debug.Log($"📡 Evento agora tem {OnSkillChoiceRequired?.GetInvocationList().Length ?? 0} listeners");
    }

    [ContextMenu("🎯 Forçar Escolha de Skill")]
    public void ForceSkillChoice()
    {
        Debug.Log("🎯 Forçando escolha de skill...");
        OfferSkillChoice();
    }

    [ContextMenu("🚀 Forçar Escolha Inicial para Nível 1")]
    public void ForceInitialChoiceForLevel1()
    {
        Debug.Log("🚀 Forçando escolha inicial para nível 1...");

        if (playerStats == null)
        {
            playerStats = FindAnyObjectByType<PlayerStats>();
        }

        if (playerStats != null && playerStats.level == 1)
        {
            initialChoiceOffered = true;
            OfferSkillChoice();
        }
        else
        {
            Debug.LogError($"❌ Player não está no nível 1 (está no nível {playerStats?.level})");
        }
    }

    [ContextMenu("🔍 Diagnosticar Problema de Escolha")]
    public void DiagnoseChoiceProblem()
    {
        Debug.Log("🔍 DIAGNÓSTICO DO PROBLEMA DE ESCOLHA:");

        Debug.Log($"1. PlayerStats: {(playerStats != null ? "✅ Encontrado" : "❌ Não encontrado")}");
        Debug.Log($"2. SkillChoiceUI: {(skillChoiceUI != null ? "✅ Encontrado" : "❌ Não encontrado")}");

        if (skillChoiceUI != null)
        {
            Debug.Log($"3. Registrado no evento: {(OnSkillChoiceRequired != null ? "✅ Sim" : "❌ Não")}");
        }

        Debug.Log($"4. Skills disponíveis: {availableSkills.Count}");
        Debug.Log($"5. Skills ativas: {activeSkills.Count}");
        Debug.Log($"6. Milestones: {string.Join(", ", levelUpMilestones)}");
        Debug.Log($"7. Player Level: {playerStats?.level}");
        Debug.Log("8. Testando oferta de escolha...");
        OfferSkillChoice();
    }

    [ContextMenu("🔧 Reconectar SkillChoiceUI")]
    public void ReconnectSkillChoiceUI()
    {
        skillChoiceUI = FindSkillChoiceUIInUIManager();
        if (skillChoiceUI != null)
        {
            OnSkillChoiceRequired -= skillChoiceUI.ShowSkillChoice;
            OnSkillChoiceRequired += skillChoiceUI.ShowSkillChoice;
            Debug.Log("✅ SkillChoiceUI reconectado manualmente");
        }
        else
        {
            Debug.LogError("❌ SkillChoiceUI não encontrado para reconexão");
        }
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

    [ContextMenu("Adicionar Skills de Teste")]
    public void AddTestSkills()
    {
        // Cria uma skill de projétil básica para teste
        SkillData testSkill = ScriptableObject.CreateInstance<SkillData>();
        testSkill.skillName = "Projétil de Teste";
        testSkill.description = "Projétil automático para teste";
        testSkill.attackBonus = 15f;
        testSkill.healthBonus = 10f;
        testSkill.specificType = SpecificSkillType.Projectile;
        testSkill.element = PlayerStats.Element.Fire;

        AddSkill(testSkill);
        Debug.Log("✅ Skill de teste adicionada");
    }

    [ContextMenu("Verificar Status da Integração")]
    public void CheckIntegrationStatus()
    {
        Debug.Log("🔍 Status da Integração do SkillManager:");
        Debug.Log($"• Skills Disponíveis: {availableSkills.Count}");
        Debug.Log($"• Skills Ativas: {activeSkills.Count}");
        Debug.Log($"• PlayerStats: {(playerStats != null ? "✅ Conectado" : "❌ Não encontrado")}");
        Debug.Log($"• SkillChoiceUI: {(skillChoiceUI != null ? "✅ Conectado" : "❌ Não encontrado")}");
        Debug.Log($"• Evento OnSkillChoiceRequired: {(OnSkillChoiceRequired != null ? "✅ Registrado" : "❌ Null")}");
        Debug.Log($"• Milestones: {string.Join(", ", levelUpMilestones)}");
        Debug.Log($"• Próximo Milestone: {GetNextMilestone()}");
    }

    // 🆕 MÉTODOS PARA O SISTEMA DE MILESTONES
    [ContextMenu("🎯 Testar Escolha de 3 Skills Aleatórias")]
    public void TestRandomSkillChoice()
    {
        Debug.Log("🎯 TESTE: Oferecendo 3 skills aleatórias...");
        OfferSkillChoice();
    }

    [ContextMenu("🔄 Limpar Skills do Player")]
    public void ClearPlayerSkills()
    {
        activeSkills.Clear();
        if (playerStats != null)
        {
            playerStats.acquiredSkills.Clear();
            playerStats.InitializeDefaultSkills();
        }
        ClearRecentlyOfferedSkills();
        Debug.Log("🔄 Todas as skills do player foram removidas");
    }

    [ContextMenu("🔍 Ver Skills Disponíveis")]
    public void LogAvailableSkills()
    {
        Debug.Log($"📚 Skills disponíveis no total: {availableSkills.Count}");
        foreach (var skill in availableSkills)
        {
            Debug.Log($"   • {skill.skillName} ({skill.element}) - {skill.specificType}");
        }
    }

    [ContextMenu("🔍 Ver Milestones Configurados")]
    public void LogMilestones()
    {
        Debug.Log($"🎯 Milestones configurados: {string.Join(", ", levelUpMilestones)}");
        Debug.Log($"🎯 Próximo milestone: {GetNextMilestone()}");
    }

    public int GetNextMilestone()
    {
        if (playerStats == null) return -1;

        foreach (int milestone in levelUpMilestones)
        {
            if (milestone > playerStats.level)
                return milestone;
        }
        return -1; // Não há mais milestones
    }

    [ContextMenu("🔄 Simular Level Up para Próximo Milestone")]
    public void SimulateNextMilestoneLevelUp()
    {
        if (playerStats == null) return;

        int nextMilestone = GetNextMilestone();
        if (nextMilestone != -1)
        {
            Debug.Log($"🎯 Simulando level up para milestone {nextMilestone}");
            playerStats.level = nextMilestone - 1; // Define um nível antes
            OnPlayerLevelUp(nextMilestone); // Chama o level up
        }
        else
        {
            Debug.Log("ℹ️ Não há mais milestones disponíveis");
        }
    }

    // 🆕 MÉTODO PARA LIMPAR SKILLS RECENTES
    public void ClearRecentlyOfferedSkills()
    {
        recentlyOfferedSkills.Clear();
        Debug.Log("🔄 Lista de skills recentemente oferecidas foi limpa");
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