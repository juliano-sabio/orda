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

    // 🆕 SISTEMA DE SKILL EQUIPADA
    [Header("🎯 Skill Equipada Atual")]
    public SkillData currentlyEquippedSkill;
    public int selectedSkillIndex = 0;
    public event System.Action<SkillData> OnSkillEquippedChanged;

    // Configurações de Progressão
    public int[] levelUpMilestones = { 1, 3, 6, 10, 15, 20 };
    public int skillsPerChoice = 3;
    public bool allowDuplicateChoices = false;

    [Header("Configurações de Escolha Inicial")]
    public bool alwaysOfferInitialChoice = false;

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

            // 🎯 CONFIGURAR SKILLS NO SKILLCHOICEUI
            if (skillChoiceUI.allAvailableSkills == null || skillChoiceUI.allAvailableSkills.Count == 0)
            {
                Debug.Log("🔄 Configurando skills no SkillChoiceUI...");
                skillChoiceUI.allAvailableSkills = new List<SkillData>(availableSkills);
            }
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
                // 🎯 CONFIGURAR SKILLS NO SKILLCHOICEUI
                if (skillChoiceUI.allAvailableSkills == null || skillChoiceUI.allAvailableSkills.Count == 0)
                {
                    skillChoiceUI.allAvailableSkills = new List<SkillData>(availableSkills);
                }
            }
        }

        CheckForInitialSkillChoice();

        // 🚫 NÃO adicionar skills automaticamente - player começa SEM skills (exceto ultimate)
        Debug.Log("🎯 Player começa sem skills (ultimate já está configurada)");
    }

    private void CheckForInitialSkillChoice()
    {
        Debug.Log($"🔍 Verificando escolha inicial - Level: {playerStats?.level}, JáOferecida: {initialChoiceOffered}");

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

            // 🎯 CONFIGURAR SKILLS NO SKILLCHOICEUI
            if (skillChoiceUI.allAvailableSkills == null || skillChoiceUI.allAvailableSkills.Count == 0)
            {
                skillChoiceUI.allAvailableSkills = new List<SkillData>(availableSkills);
            }
        }

        // 🆕 RECONECTAR SKILL EQUIPADA APÓS CARREGAR CENA
        if (currentlyEquippedSkill != null && activeSkills.Contains(currentlyEquippedSkill))
        {
            OnSkillEquippedChanged?.Invoke(currentlyEquippedSkill);
        }
        else if (activeSkills.Count > 0)
        {
            EquipSkill(activeSkills[0]);
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

        // 🎯 MÉTODO CORRETO: Chamar ShowRandomSkillChoice DIRETAMENTE
        Debug.Log($"🎯 Chamando ShowRandomSkillChoice DIRETAMENTE - {skillChoiceUI.allAvailableSkills?.Count ?? 0} skills disponíveis");

        // 🎯 VERIFICAR E CONFIGURAR SKILLS SE NECESSÁRIO
        if (skillChoiceUI.allAvailableSkills == null || skillChoiceUI.allAvailableSkills.Count == 0)
        {
            Debug.Log("🔄 Configurando skills no SkillChoiceUI...");
            skillChoiceUI.allAvailableSkills = new List<SkillData>(availableSkills);
        }

        // 🎯 VERIFICAR SE TEM SKILLS SUFICIENTES
        if (skillChoiceUI.allAvailableSkills.Count < 3)
        {
            Debug.LogWarning($"⚠️ Apenas {skillChoiceUI.allAvailableSkills.Count} skills disponíveis! Criando skills de teste...");
            CreateEmergencyTestSkills();
        }

        // 🎯 CHAMAR O MÉTODO QUE MOSTRA 3 SKILLS ALEATÓRIAS
        skillChoiceUI.ShowRandomSkillChoice(OnSkillSelectedFromChoice);
    }

    void OnSkillSelectedFromChoice(SkillData selectedSkill)
    {
        if (selectedSkill != null)
        {
            Debug.Log($"✅ Skill selecionada: {selectedSkill.skillName}");
            AddSkill(selectedSkill);

            // 🆕 EQUIPAR AUTOMATICAMENTE A NOVA SKILL
            if (currentlyEquippedSkill == null)
            {
                EquipSkill(selectedSkill);
            }
        }
        else
        {
            Debug.LogError("❌ Skill selecionada é null!");
        }
    }

    // 🆕 MÉTODO PARA CRIAR SKILLS DE EMERGÊNCIA
    private void CreateEmergencyTestSkills()
    {
        Debug.Log("🚨 CRIANDO SKILLS DE EMERGÊNCIA!");

        skillChoiceUI.allAvailableSkills.Clear();

        string[] testNames = {
            "BOLA DE FOGO",
            "RAIO GELADO",
            "VENENO MORTAL",
            "TERREMOTO",
            "FUMAÇA VENENOSA"
        };

        PlayerStats.Element[] elements = {
            PlayerStats.Element.Fire,
            PlayerStats.Element.Ice,
            PlayerStats.Element.Poison,
            PlayerStats.Element.Earth,
            PlayerStats.Element.Poison
        };

        for (int i = 0; i < testNames.Length; i++)
        {
            SkillData testSkill = ScriptableObject.CreateInstance<SkillData>();
            testSkill.skillName = testNames[i];
            testSkill.description = $"Skill de emergência - {testNames[i]}";
            testSkill.attackBonus = UnityEngine.Random.Range(10, 30);
            testSkill.defenseBonus = UnityEngine.Random.Range(5, 15);
            testSkill.healthBonus = UnityEngine.Random.Range(10, 25);
            testSkill.element = elements[i];
            testSkill.specificType = SpecificSkillType.Projectile;
            testSkill.rarity = SkillRarity.Common;

            skillChoiceUI.allAvailableSkills.Add(testSkill);
        }

        Debug.Log($"🧪 {skillChoiceUI.allAvailableSkills.Count} skills de emergência criadas");
    }

    List<SkillData> GetRandomSkillChoices(int count)
    {
        List<SkillData> choices = new List<SkillData>();
        List<SkillData> availableChoices = new List<SkillData>(availableSkills);

        availableChoices.RemoveAll(skill => activeSkills.Exists(s => s.skillName == skill.skillName));

        if (allowDuplicateChoices == false && recentlyOfferedSkills.Count > 0)
        {
            availableChoices.RemoveAll(skill => recentlyOfferedSkills.Contains(skill));
        }

        if (availableChoices.Count < count && recentlyOfferedSkills.Count > 0)
        {
            Debug.Log("🔄 Poucas skills disponíveis - resetando lista de skills recentes");
            recentlyOfferedSkills.Clear();
            availableChoices = new List<SkillData>(availableSkills);
            availableChoices.RemoveAll(skill => activeSkills.Exists(s => s.skillName == skill.skillName));
        }

        for (int i = 0; i < Mathf.Min(count, availableChoices.Count); i++)
        {
            if (availableChoices.Count == 0) break;

            int randomIndex = UnityEngine.Random.Range(0, availableChoices.Count);
            SkillData chosenSkill = availableChoices[randomIndex];

            choices.Add(chosenSkill);
            availableChoices.RemoveAt(randomIndex);

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

        // ✅ VERIFICAÇÃO ESPECIAL PARA ULTIMATE
        bool isUltimateSkill = skill.skillName.ToLower().Contains("ultimate") ||
                              skill.specificType == SpecificSkillType.Ultimate;

        if (!HasSkill(skill))
        {
            if (playerStats != null && !skill.MeetsRequirements(playerStats.level, activeSkills))
            {
                Debug.LogWarning($"❌ Skill {skill.skillName} não atende aos requisitos!");
                return;
            }

            activeSkills.Add(skill);

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

            // 🆕 SE É A PRIMEIRA SKILL, EQUIPAR AUTOMATICAMENTE
            if (activeSkills.Count == 1 || currentlyEquippedSkill == null)
            {
                EquipSkill(skill);
            }

            if (UIManager.Instance != null)
                UIManager.Instance.ShowSkillAcquired(skill.skillName, skill.GetFullDescription());
        }
    }

    // 🆕 SISTEMA DE SKILL EQUIPADA
    public void EquipSkill(SkillData skill)
    {
        if (skill == null || !activeSkills.Contains(skill)) return;

        currentlyEquippedSkill = skill;
        selectedSkillIndex = activeSkills.IndexOf(skill);

        Debug.Log($"🎯 Skill equipada: {skill.skillName}");
        OnSkillEquippedChanged?.Invoke(skill);

        // Aplicar efeitos visuais da skill equipada
        ApplyEquippedSkillEffects(skill);
    }

    // 🆕 CICLAR ENTRE SKILLS
    public void CycleEquippedSkill()
    {
        if (activeSkills.Count == 0) return;

        selectedSkillIndex = (selectedSkillIndex + 1) % activeSkills.Count;
        EquipSkill(activeSkills[selectedSkillIndex]);
    }

    // 🆕 OBTER SKILL EQUIPADA
    public SkillData GetEquippedSkill()
    {
        return currentlyEquippedSkill;
    }

    // 🆕 OBTER SKILL POR ÍNDICE
    public SkillData GetSkillByIndex(int index)
    {
        if (index >= 0 && index < activeSkills.Count)
            return activeSkills[index];
        return null;
    }

    // 🆕 OBTER ÍNDICE DA SKILL EQUIPADA
    public int GetEquippedSkillIndex()
    {
        return selectedSkillIndex;
    }

    // 🆕 EFECTOS DA SKILL EQUIPADA
    private void ApplyEquippedSkillEffects(SkillData skill)
    {
        if (playerStats == null) return;

        // Aplicar bônus temporários ou efeitos da skill equipada
        StartCoroutine(EquippedSkillHighlight(skill));

        Debug.Log($"✨ Efeitos da skill equipada aplicados: {skill.skillName}");
    }

    private IEnumerator EquippedSkillHighlight(SkillData skill)
    {
        // Efeito visual temporário
        yield return new WaitForSeconds(2f);
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

    // 🆕 MÉTODO CONFIGURESKILLBEHAVIOR ATUALIZADO COM PROJÉTEIS ORBITAIS
    void ConfigureSkillBehavior(SkillData skill)
    {
        switch (skill.specificType)
        {
            case SpecificSkillType.Projectile:
                if (skill.ShouldUseOrbitalBehavior())
                {
                    AddOrbitingProjectileBehavior(skill);
                }
                else
                {
                    AddProjectileBehavior(skill);
                }
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

    // 🆕 MÉTODO PARA PROJÉTEIS ORBITAIS
    void AddOrbitingProjectileBehavior(SkillData skill)
    {
        var existingBehavior = GetComponent<OrbitingProjectileSkillBehavior>();
        if (existingBehavior != null)
        {
            existingBehavior.UpdateFromSkillData(skill);
            return;
        }

        OrbitingProjectileSkillBehavior orbitalBehavior = gameObject.AddComponent<OrbitingProjectileSkillBehavior>();
        orbitalBehavior.Initialize(playerStats);
        orbitalBehavior.UpdateFromSkillData(skill);

        Debug.Log($"🌀 Comportamento orbital adicionado: {skill.skillName}");
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

            if (playerStats != null)
            {
                playerStats.AddSkillModifier(modifier);
            }

            OnModifierAcquired?.Invoke(modifier);

            if (UIManager.Instance != null)
                UIManager.Instance.ShowModifierAcquired(modifier.modifierName, modifier.targetSkillName);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("🌀 Criar Skill Orbital de Teste")]
    public void CreateTestOrbitalSkill()
    {
        // Cria skill temporária apenas para teste
        SkillData orbitalSkill = ScriptableObject.CreateInstance<SkillData>();
        orbitalSkill.skillName = "🌀 Esfera Orbital (TESTE)";
        orbitalSkill.description = "ESFERA DE TESTE - Esta skill some ao reiniciar";
        orbitalSkill.specificType = SpecificSkillType.Projectile;
        orbitalSkill.isPassive = true;

        // Configurações orbitais
        orbitalSkill.isOrbitalProjectile = true;
        orbitalSkill.orbitRadius = 2f;
        orbitalSkill.orbitSpeed = 220f;
        orbitalSkill.numberOfOrbits = 1;
        orbitalSkill.continuousOrbitalSpawning = true;
        orbitalSkill.orbitalSpawnInterval = 2.5f;
        orbitalSkill.attackBonus = 25f;
        orbitalSkill.element = PlayerStats.Element.Lightning;

        // Adiciona TEMPORARIAMENTE
        AddSkill(orbitalSkill);
        Debug.Log("🌀 Skill orbital de TESTE criada (some ao reiniciar)");
    }

    [ContextMenu("🎯 Adicionar Skill de Teste")]
    public void AddTestSkills()
    {
        Debug.Log("🧪 Adicionando skill de TESTE (apenas desenvolvimento)");

        SkillData testSkill = ScriptableObject.CreateInstance<SkillData>();
        testSkill.skillName = "Projétil de Teste (EDITOR)";
        testSkill.description = "Projétil automático para teste - APENAS NO EDITOR";
        testSkill.attackBonus = 15f;
        testSkill.healthBonus = 10f;
        testSkill.specificType = SpecificSkillType.Projectile;
        testSkill.element = PlayerStats.Element.Fire;

        AddSkill(testSkill);
        Debug.Log("✅ Skill de teste adicionada (apenas editor)");
    }

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
#endif

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

        // 🆕 SALVAR SKILL EQUIPADA
        if (currentlyEquippedSkill != null)
        {
            PlayerPrefs.SetString("EquippedSkill", currentlyEquippedSkill.skillName);
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

#if UNITY_EDITOR
    [ContextMenu("🚨 DIAGNÓSTICO URGENTE: Verificar Sistema de 3 Skills")]
    public void UrgentDiagnosis()
    {
        Debug.Log("🚨 DIAGNÓSTICO URGENTE DO SISTEMA DE 3 SKILLS");

        // 1. Verificar SkillChoiceUI
        if (skillChoiceUI == null)
        {
            Debug.LogError("❌ SKILLCHOICEUI NULL!");
            skillChoiceUI = FindAnyObjectByType<SkillChoiceUI>();
            Debug.Log($"🔍 Tentativa de encontrar: {(skillChoiceUI != null ? "✅ Encontrado" : "❌ Não encontrado")}");
        }
        else
        {
            Debug.Log($"✅ SkillChoiceUI: {skillChoiceUI.gameObject.name}");
            Debug.Log($"📋 Skills no UI: {skillChoiceUI.allAvailableSkills?.Count ?? 0}");
            Debug.Log($"🎯 Método ShowRandomSkillChoice existe: {skillChoiceUI.GetType().GetMethod("ShowRandomSkillChoice") != null}");
        }

        // 2. Verificar availableSkills
        Debug.Log($"📚 AvailableSkills no Manager: {availableSkills.Count}");

        // 3. Forçar teste do sistema
        Debug.Log("🔄 FORÇANDO TESTE DO SISTEMA...");
        StartCoroutine(ForceThreeSkillsTest());
    }

    private IEnumerator ForceThreeSkillsTest()
    {
        yield return new WaitForSeconds(1f);

        // Chamar o sistema CORRETO
        if (skillChoiceUI != null)
        {
            Debug.Log("🎯 CHAMANDO ShowRandomSkillChoice DIRETAMENTE...");
            skillChoiceUI.ShowRandomSkillChoice((selectedSkill) => {
                Debug.Log($"✅ RESULTADO: Skill escolhida - {selectedSkill?.skillName ?? "NULL"}");
            });
        }
        else
        {
            Debug.LogError("❌ SkillChoiceUI não disponível!");
        }
    }

    [ContextMenu("🎯 Forçar Escolha de 3 Skills")]
    public void ForceThreeSkillsChoice()
    {
        Debug.Log("🎯 FORÇANDO ESCOLHA DE 3 SKILLS...");
        OfferSkillChoice();
    }

    [ContextMenu("🔧 Forçar Configuração de Skills")]
    public void ForceSkillConfiguration()
    {
        Debug.Log("🔧 Forçando configuração de skills...");

        if (skillChoiceUI != null)
        {
            skillChoiceUI.allAvailableSkills = new List<SkillData>(availableSkills);
            Debug.Log($"✅ Configuradas {skillChoiceUI.allAvailableSkills.Count} skills no SkillChoiceUI");
        }
        else
        {
            Debug.LogError("❌ SkillChoiceUI não encontrado!");
        }
    }

    [ContextMenu("🔍 Diagnosticar Problema de Escolha")]
    public void DiagnoseChoiceProblem()
    {
        Debug.Log("🔍 DIAGNÓSTICO DO PROBLEMA DE ESCOLHA:");

        Debug.Log($"1. PlayerStats: {(playerStats != null ? "✅ Encontrado" : "❌ Não encontrado")}");
        Debug.Log($"2. SkillChoiceUI: {(skillChoiceUI != null ? "✅ Encontrado" : "❌ Não encontrado")}");
        Debug.Log($"3. Skills disponíveis: {availableSkills.Count}");
        Debug.Log($"4. Skills ativas: {activeSkills.Count}");
        Debug.Log($"5. Milestones: {string.Join(", ", levelUpMilestones)}");
        Debug.Log($"6. Player Level: {playerStats?.level}");

        if (skillChoiceUI != null)
        {
            Debug.Log($"7. Skills no UI: {skillChoiceUI.allAvailableSkills?.Count ?? 0}");
            Debug.Log($"8. ShowRandomSkillChoice existe: {skillChoiceUI.GetType().GetMethod("ShowRandomSkillChoice") != null}");
        }

        Debug.Log("9. Testando oferta de escolha...");
        OfferSkillChoice();
    }

    [ContextMenu("🎮 Testar Troca de Skill Equipada")]
    public void TestEquippedSkillSystem()
    {
        if (activeSkills.Count == 0)
        {
            Debug.LogWarning("⚠️ Nenhuma skill ativa para testar");
            return;
        }

        CycleEquippedSkill();
        Debug.Log($"🎮 Skill equipada: {currentlyEquippedSkill?.skillName}");
    }

    [ContextMenu("🔄 Limpar Todas as Skills")]
    public void ClearAllSkills()
    {
        // Remove skills ativas
        foreach (var skill in activeSkills)
        {
            if (playerStats != null)
            {
                skill.RemoveFromPlayer(playerStats);
            }
        }

        activeSkills.Clear();
        currentlyEquippedSkill = null;
        selectedSkillIndex = 0;

        // Remove comportamentos
        var orbitalBehavior = GetComponent<OrbitingProjectileSkillBehavior>();
        if (orbitalBehavior != null) Destroy(orbitalBehavior);

        var projectileBehavior = GetComponent<PassiveProjectileSkill2D>();
        if (projectileBehavior != null) Destroy(projectileBehavior);

        Debug.Log("🧹 TODAS as skills foram removidas - player resetado");
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

    [ContextMenu("🔄 Simular Level Up para Próximo Milestone")]
    public void SimulateNextMilestoneLevelUp()
    {
        if (playerStats == null) return;

        int nextMilestone = GetNextMilestone();
        if (nextMilestone != -1)
        {
            Debug.Log($"🎯 Simulando level up para milestone {nextMilestone}");
            playerStats.level = nextMilestone - 1;
            OnPlayerLevelUp(nextMilestone);
        }
        else
        {
            Debug.Log("ℹ️ Não há mais milestones disponíveis");
        }
    }

    [ContextMenu("Verificar Status da Integração")]
    public void CheckIntegrationStatus()
    {
        Debug.Log("🔍 Status da Integração do SkillManager:");
        Debug.Log($"• Skills Disponíveis: {availableSkills.Count}");
        Debug.Log($"• Skills Ativas: {activeSkills.Count}");
        Debug.Log($"• Skill Equipada: {(currentlyEquippedSkill != null ? currentlyEquippedSkill.skillName : "Nenhuma")}");
        Debug.Log($"• PlayerStats: {(playerStats != null ? "✅ Conectado" : "❌ Não encontrado")}");
        Debug.Log($"• SkillChoiceUI: {(skillChoiceUI != null ? "✅ Conectado" : "❌ Não encontrado")}");
        Debug.Log($"• Skills no UI: {(skillChoiceUI != null ? skillChoiceUI.allAvailableSkills?.Count.ToString() ?? "0" : "N/A")}");
        Debug.Log($"• Milestones: {string.Join(", ", levelUpMilestones)}");
        Debug.Log($"• Próximo Milestone: {GetNextMilestone()}");
    }
#endif

    public int GetNextMilestone()
    {
        if (playerStats == null) return -1;

        foreach (int milestone in levelUpMilestones)
        {
            if (milestone > playerStats.level)
                return milestone;
        }
        return -1;
    }

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
        OnSkillEquippedChanged = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}