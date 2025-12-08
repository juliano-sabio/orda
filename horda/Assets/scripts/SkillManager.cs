using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static PlayerStats;
using System.Linq; // ADICIONE ESTA LINHA PARA USAR .Count()

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance;

    // Sistema de Skills
    public List<SkillData> availableSkills = new List<SkillData>();
    public List<SkillData> activeSkills = new List<SkillData>();
    public List<SkillModifier> activeModifiers = new List<SkillModifier>();

    // SISTEMA DE SKILL EQUIPADA
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
    public event System.Action<List<SkillData>, System.Action<SkillData>> OnSkillChoiceRequired;
    public event System.Action<SkillData> OnSkillAcquired;
    public event System.Action<SkillModifier> OnModifierAcquired;

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

        playerStats = FindFirstObjectByType<PlayerStats>();
        skillChoiceUI = FindSkillChoiceUIInUIManager();

        if (skillChoiceUI == null)
        {
            Debug.LogError("❌ SkillChoiceUI não encontrado no UIManager!");
            skillChoiceUI = FindFirstObjectByType<SkillChoiceUI>();
        }

        if (skillChoiceUI != null)
        {
            Debug.Log($"✅ SkillChoiceUI encontrado: {skillChoiceUI.gameObject.name}");

            // CONFIGURAR SKILLS NO SKILLCHOICEUI
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
            playerStats = FindFirstObjectByType<PlayerStats>();
        }

        if (skillChoiceUI == null)
        {
            skillChoiceUI = FindSkillChoiceUIInUIManager();
            if (skillChoiceUI != null)
            {
                // CONFIGURAR SKILLS NO SKILLCHOICEUI
                if (skillChoiceUI.allAvailableSkills == null || skillChoiceUI.allAvailableSkills.Count == 0)
                {
                    skillChoiceUI.allAvailableSkills = new List<SkillData>(availableSkills);
                }
            }
        }

        CheckForInitialSkillChoice();

        // NÃO adicionar skills automaticamente - player começa SEM skills (exceto ultimate)
        Debug.Log("🎯 Player começa sem skills (ultimate já está configurada)");
    }

    private void CheckForInitialSkillChoice()
    {
        Debug.Log($"🔍 Verificando escolha inicial - Level: {playerStats?.level}, JáOferecida: {initialChoiceOffered}");

        bool level1IsMilestone = System.Array.Exists(levelUpMilestones, milestone => milestone == 1);

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
        UIManager uiManager = FindFirstObjectByType<UIManager>();

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

        playerStats = FindFirstObjectByType<PlayerStats>();
        skillChoiceUI = FindSkillChoiceUIInUIManager();

        if (skillChoiceUI != null)
        {
            Debug.Log("✅ SkillChoiceUI reconectado após carregar cena");

            // CONFIGURAR SKILLS NO SKILLCHOICEUI
            if (skillChoiceUI.allAvailableSkills == null || skillChoiceUI.allAvailableSkills.Count == 0)
            {
                skillChoiceUI.allAvailableSkills = new List<SkillData>(availableSkills);
            }
        }

        // RECONECTAR SKILL EQUIPADA APÓS CARREGAR CENA
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
            playerStats = FindFirstObjectByType<PlayerStats>();
            if (playerStats == null)
            {
                Debug.LogError("❌ PlayerStats null no level up!");
                return;
            }
        }

        bool isMilestone = System.Array.Exists(levelUpMilestones, milestone => milestone == newLevel);
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

        // MÉTODO CORRETO: Chamar ShowRandomSkillChoice DIRETAMENTE
        Debug.Log($"🎯 Chamando ShowRandomSkillChoice DIRETAMENTE - {skillChoiceUI.allAvailableSkills?.Count ?? 0} skills disponíveis");

        // VERIFICAR E CONFIGURAR SKILLS SE NECESSÁRIO
        if (skillChoiceUI.allAvailableSkills == null || skillChoiceUI.allAvailableSkills.Count == 0)
        {
            Debug.Log("🔄 Configurando skills no SkillChoiceUI...");
            skillChoiceUI.allAvailableSkills = new List<SkillData>(availableSkills);
        }

        // VERIFICAR SE TEM SKILLS SUFICIENTES
        if (skillChoiceUI.allAvailableSkills.Count < 3)
        {
            Debug.LogWarning($"⚠️ Apenas {skillChoiceUI.allAvailableSkills.Count} skills disponíveis! Criando skills de teste...");
            CreateEmergencyTestSkills();
        }

        // CHAMAR O MÉTODO QUE MOSTRA 3 SKILLS ALEATÓRIAS
        skillChoiceUI.ShowRandomSkillChoice(OnSkillSelectedFromChoice);
    }

    void OnSkillSelectedFromChoice(SkillData selectedSkill)
    {
        if (selectedSkill != null)
        {
            Debug.Log($"✅ Skill selecionada: {selectedSkill.skillName}");
            AddSkill(selectedSkill);

            // EQUIPAR AUTOMATICAMENTE A NOVA SKILL
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

    // MÉTODO PARA CRIAR SKILLS DE EMERGÊNCIA
    private void CreateEmergencyTestSkills()
    {
        Debug.Log("🚨 CRIANDO SKILLS DE EMERGÊNCIA!");

        skillChoiceUI.allAvailableSkills.Clear();

        string[] testNames = {
            "BOLA DE FOGO",
            "RAIO GELADO",
            "VENENO MORTAL",
            "TERREMOTO",
            "FUMAÇA VENENOSA",
            "🌀 BUMERANGUE DO VENTO"
        };

        PlayerStats.Element[] elements = {
            PlayerStats.Element.Fire,
            PlayerStats.Element.Ice,
            PlayerStats.Element.Poison,
            PlayerStats.Element.Earth,
            PlayerStats.Element.Poison,
            PlayerStats.Element.Wind
        };

        SpecificSkillType[] types = {
            SpecificSkillType.Projectile,
            SpecificSkillType.Projectile,
            SpecificSkillType.PoisonCloud,
            SpecificSkillType.EarthStomp,
            SpecificSkillType.PoisonCloud,
            SpecificSkillType.Boomerang
        };

        for (int i = 0; i < testNames.Length; i++)
        {
            SkillData testSkill = ScriptableObject.CreateInstance<SkillData>();
            testSkill.skillName = testNames[i];
            testSkill.description = $"Skill de emergência - {testNames[i]}";
            testSkill.attackBonus = Random.Range(10, 30);
            testSkill.defenseBonus = Random.Range(5, 15);
            testSkill.healthBonus = Random.Range(10, 25);
            testSkill.element = elements[i];
            testSkill.specificType = types[i];
            testSkill.rarity = SkillRarity.Common;

            if (types[i] == SpecificSkillType.Boomerang)
            {
                testSkill.specialValue = 8f;
                testSkill.activationInterval = 2.5f;
                testSkill.boomerangThrowRange = 8f;
                testSkill.boomerangThrowSpeed = 15f;
                testSkill.boomerangMaxTargets = 3;
                testSkill.boomerangHealOnReturn = true;
                testSkill.description = "Um bumerangue que vai e volta, causando dano a múltiplos inimigos";
            }

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

            int randomIndex = Random.Range(0, availableChoices.Count);
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

        // VERIFICAÇÃO MAIS RIGOROSA PARA DUPLICAÇÃO
        bool alreadyHasExactSkill = activeSkills.Exists(s =>
            s.skillName == skill.skillName &&
            s.specificType == skill.specificType &&
            s.element == skill.element
        );

        if (alreadyHasExactSkill)
        {
            Debug.LogWarning($"⚠️ Skill {skill.skillName} já foi adquirida anteriormente!");
            return;
        }

        // VERIFICAÇÃO ESPECIAL PARA ULTIMATE
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
                ConfigureSkillBehavior(skill); // ✅ ADICIONADO: Configurar comportamento após aplicar skill
            }
            else
            {
                Debug.LogWarning("⚠️ PlayerStats não encontrado para aplicar a skill");
            }

            OnSkillAcquired?.Invoke(skill);

            Debug.Log($"✅ Skill adquirida: {skill.skillName}");

            // SE É A PRIMEIRA SKILL, EQUIPAR AUTOMATICAMENTE
            if (activeSkills.Count == 1 || currentlyEquippedSkill == null)
            {
                EquipSkill(skill);
            }

            if (UIManager.Instance != null)
                UIManager.Instance.ShowSkillAcquired(skill.skillName, skill.GetFullDescription());
        }
    }

    // SISTEMA DE SKILL EQUIPADA
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

    // CICLAR ENTRE SKILLS
    public void CycleEquippedSkill()
    {
        if (activeSkills.Count == 0) return;

        selectedSkillIndex = (selectedSkillIndex + 1) % activeSkills.Count;
        EquipSkill(activeSkills[selectedSkillIndex]);
    }

    // OBTER SKILL EQUIPADA
    public SkillData GetEquippedSkill()
    {
        return currentlyEquippedSkill;
    }

    // OBTER SKILL POR ÍNDICE
    public SkillData GetSkillByIndex(int index)
    {
        if (index >= 0 && index < activeSkills.Count)
            return activeSkills[index];
        return null;
    }

    // OBTER ÍNDICE DA SKILL EQUIPADA
    public int GetEquippedSkillIndex()
    {
        return selectedSkillIndex;
    }

    // EFECTOS DA SKILL EQUIPADA
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
            Debug.Log($"✨ Skill {skill.skillName} aplicada ao player");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Erro ao aplicar skill {skill.skillName}: {e.Message}");
        }
    }

    // ✅ MÉTODO CONFIGURESKILLBEHAVIOR CORRIGIDO
    void ConfigureSkillBehavior(SkillData skill)
    {
        Debug.Log($"⚙️ [yEngine] Configurando comportamento: {skill.skillName} ({skill.specificType})");

        if (playerStats == null)
        {
            Debug.LogError("❌ [yEngine] PlayerStats é null!");
            return;
        }

        // VERIFICAR SE A SKILL JÁ FOI CONFIGURADA
        int sameSkillCount = activeSkills.Count(s => s.skillName == skill.skillName);
        if (sameSkillCount > 1)
        {
            Debug.LogWarning($"⚠️ [yEngine] Skill {skill.skillName} já foi configurada anteriormente! (aparece {sameSkillCount} vezes)");
            return;
        }

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

            case SpecificSkillType.Boomerang:
                // ✅ AGORA ADICIONA CORRETAMENTE AO PLAYER
                AddBoomerangBehaviorToPlayer(skill);
                break;

            case SpecificSkillType.HealthRegen:
                Debug.Log($"🔄 Skill de regeneração: {skill.skillName}");
                playerStats.healthRegenRate += skill.healthRegenBonus;
                break;

            case SpecificSkillType.CriticalStrike:
                Debug.Log($"🎯 Skill de crítico: {skill.skillName}");
                break;

            default:
                Debug.Log($"ℹ️ Skill genérica: {skill.skillName}");
                break;
        }
    }

    // ✅ MÉTODO NOVO: Adiciona bumerangue ao Player
    void AddBoomerangBehaviorToPlayer(SkillData skill)
    {
        Debug.Log($"🎯 [yEngine] Adicionando BoomerangSkillBehavior ao Player...");

        // 1. Verificar PlayerStats
        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<PlayerStats>();
            if (playerStats == null)
            {
                Debug.LogError("❌ [yEngine] PlayerStats não encontrado na cena!");
                return;
            }
        }

        // 2. Remover comportamento antigo se existir
        var oldBehaviors = playerStats.GetComponents<BoomerangSkillBehavior>();
        if (oldBehaviors.Length > 0)
        {
            Debug.Log($"🔄 [yEngine] Removendo {oldBehaviors.Length} comportamentos antigos de bumerangue");
            foreach (var oldBehavior in oldBehaviors)
            {
                Destroy(oldBehavior);
            }
        }

        // 3. Adicionar novo comportamento AO PLAYER
        BoomerangSkillBehavior newBehavior = playerStats.gameObject.AddComponent<BoomerangSkillBehavior>();

        if (newBehavior == null)
        {
            Debug.LogError("❌ [yEngine] Falha ao criar BoomerangSkillBehavior!");
            return;
        }

        // 4. Inicializar com PlayerStats
        newBehavior.Initialize(playerStats);

        // 5. Configurar com dados da skill
        newBehavior.UpdateFromSkillData(skill);

        // 6. Ativar
        newBehavior.SetActive(true);

        Debug.Log($"✅ [yEngine] Comportamento adicionado ao Player {playerStats.name}");
        Debug.Log($"   • Skill: {skill.skillName}");
        Debug.Log($"   • Dano: {newBehavior.damage}");
        Debug.Log($"   • Alcance: {newBehavior.throwRange}");
        Debug.Log($"   • Ativo: {newBehavior.enabled}");

        // 7. Testar imediatamente
        StartCoroutine(TestBoomerangAfterDelay(newBehavior));
    }

    private IEnumerator TestBoomerangAfterDelay(BoomerangSkillBehavior behavior)
    {
        yield return new WaitForSeconds(2f);

        if (behavior != null)
        {
            Debug.Log("🧪 [yEngine] Testando bumerangue...");

            // Usar reflexão para acessar método privado TestBoomerang
            var testMethod = typeof(BoomerangSkillBehavior).GetMethod("TestBoomerang",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

            if (testMethod != null)
            {
                testMethod.Invoke(behavior, null);
            }
            else
            {
                // Fallback: chamar ApplyEffect
                behavior.ApplyEffect();
            }
        }
    }

    // ✅ MÉTODO CORRIGIDO: PROJÉTEIS ORBITAIS
    void AddOrbitingProjectileBehavior(SkillData skill)
    {
        if (playerStats == null)
        {
            Debug.LogError("❌ PlayerStats não encontrado para orbital!");
            return;
        }

        var existingBehaviors = playerStats.GetComponents<OrbitingProjectileSkillBehavior>();
        if (existingBehaviors.Length > 0)
        {
            // Se já existe, apenas atualiza o primeiro e remove extras
            Debug.Log($"🌀 Comportamento orbital já existe - atualizando...");
            existingBehaviors[0].UpdateFromSkillData(skill);

            // Remove comportamentos duplicados
            for (int i = 1; i < existingBehaviors.Length; i++)
            {
                Destroy(existingBehaviors[i]);
                Debug.Log($"🧹 Removido comportamento orbital duplicado #{i}");
            }
            return;
        }

        OrbitingProjectileSkillBehavior orbitalBehavior = playerStats.gameObject.AddComponent<OrbitingProjectileSkillBehavior>();
        orbitalBehavior.Initialize(playerStats);
        orbitalBehavior.UpdateFromSkillData(skill);

        Debug.Log($"🌀 Comportamento orbital adicionado AO PLAYER: {skill.skillName}");
    }

    // ✅ MÉTODO CORRIGIDO: PROJÉTEIS NORMAIS
    void AddProjectileBehavior(SkillData skill)
    {
        // Verificar se já existe NO PLAYER, não no SkillManager
        if (playerStats == null)
        {
            Debug.LogError("❌ PlayerStats não encontrado!");
            return;
        }

        var existingBehaviors = playerStats.GetComponents<PassiveProjectileSkill2D>();
        if (existingBehaviors.Length > 0)
        {
            // Se já existe, apenas melhora o primeiro e remove extras
            Debug.Log($"⚡ Comportamento de projétil já existe no player - melhorando...");
            existingBehaviors[0].activationInterval = Mathf.Max(0.5f, existingBehaviors[0].activationInterval * 0.8f);

            // Remove comportamentos duplicados
            for (int i = 1; i < existingBehaviors.Length; i++)
            {
                Destroy(existingBehaviors[i]);
                Debug.Log($"🧹 Removido comportamento de projétil duplicado #{i}");
            }
            return;
        }

        PassiveProjectileSkill2D projectileBehavior = playerStats.gameObject.AddComponent<PassiveProjectileSkill2D>();

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

        Debug.Log($"✅ Comportamento de projétil 2D adicionado AO PLAYER: {skill.skillName}");
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

    // ADICIONE ESTE MÉTODO QUE ESTAVA FALTANDO:
    [ContextMenu("🌀 Criar Skill Orbital de Teste")]
    public void CreateTestOrbitalSkill()
    {
        Debug.Log("🌀 Criando Skill Orbital de Teste...");

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

            // Verificar se tem skills de bumerangue
            int boomerangCount = availableSkills.FindAll(s => s.specificType == SpecificSkillType.Boomerang).Count;
            if (boomerangCount > 0)
            {
                Debug.Log($"🌀 {boomerangCount} skills de bumerangue carregadas");
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

        // SALVAR SKILL EQUIPADA
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

    // MÉTODO PARA CONTAR SKILLS POR TIPO ESPECÍFICO
    public int GetSkillCountBySpecificType(SpecificSkillType specificType)
    {
        return activeSkills.FindAll(skill => skill.specificType == specificType).Count;
    }

    // yEngine - SISTEMA DE DIAGNÓSTICO
    public void yEngine()
    {
        Debug.Log("=== 🚀 yEngine DIAGNOSTICO COMPLETO ===");

        // 1. Verificar Player
        Debug.Log($"🎯 Player: {(playerStats != null ? playerStats.name : "NULL")}");

        // 2. Verificar Skills disponíveis
        Debug.Log($"📚 Skills disponíveis: {availableSkills.Count}");
        foreach (var skill in availableSkills)
        {
            Debug.Log($"   • {skill.skillName} - {skill.specificType}");
        }

        // 3. Verificar Skills ativas
        Debug.Log($"⚡ Skills ativas: {activeSkills.Count}");
        foreach (var skill in activeSkills)
        {
            Debug.Log($"   • {skill.skillName} - {skill.specificType}");
        }

        // 4. Verificar Skill equipada
        Debug.Log($"🎯 Skill equipada: {currentlyEquippedSkill?.skillName ?? "NENHUMA"}");

        // 5. Verificar componentes no Player
        if (playerStats != null)
        {
            CheckForDuplicateProjectiles(); // CHAMA A VERIFICAÇÃO DE DUPLICADOS
        }

        // 6. Forçar teste do sistema
        StartCoroutine(yEngineTest());
    }

    private IEnumerator yEngineTest()
    {
        yield return new WaitForSeconds(1f);

        // Criar skill de teste
        SkillData testSkill = ScriptableObject.CreateInstance<SkillData>();
        testSkill.skillName = "🌀 Bumerangue do yEngine";
        testSkill.description = "Bumerangue de teste gerado pelo yEngine";
        testSkill.specificType = SpecificSkillType.Boomerang;
        testSkill.attackBonus = 30f;
        testSkill.activationInterval = 2f;
        testSkill.element = PlayerStats.Element.Wind;

        // Configurações de bumerangue
        testSkill.boomerangThrowRange = 10f;
        testSkill.boomerangThrowSpeed = 20f;
        testSkill.boomerangHealOnReturn = true;
        testSkill.boomerangHealPercent = 0.15f;

        // Adicionar skill
        AddSkill(testSkill);

        Debug.Log("✅ yEngine: Skill de teste adicionada!");
    }

    // ✅ NOVO MÉTODO DE VERIFICAÇÃO DE DUPLICADOS
    [ContextMenu("🔍 Verificar Projéteis Duplicados")]
    public void CheckForDuplicateProjectiles()
    {
        Debug.Log("=== 🔍 VERIFICAÇÃO DE PROJÉTEIS DUPLICADOS ===");

        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<PlayerStats>();
        }

        if (playerStats != null)
        {
            // Verificar todos os componentes de projétil no player
            var projectileBehaviors = playerStats.GetComponents<PassiveProjectileSkill2D>();
            var orbitalBehaviors = playerStats.GetComponents<OrbitingProjectileSkillBehavior>();
            var boomerangBehaviors = playerStats.GetComponents<BoomerangSkillBehavior>();

            Debug.Log($"🎯 Player: {playerStats.name}");
            Debug.Log($"• PassiveProjectileSkill2D: {projectileBehaviors.Length} instância(s)");
            Debug.Log($"• OrbitingProjectileSkillBehavior: {orbitalBehaviors.Length} instância(s)");
            Debug.Log($"• BoomerangSkillBehavior: {boomerangBehaviors.Length} instância(s)");

            bool foundDuplicates = false;

            if (projectileBehaviors.Length > 1)
            {
                Debug.LogError($"❌ PROJÉTEIS DUPLICADOS DETECTADOS: {projectileBehaviors.Length} instâncias!");
                foundDuplicates = true;
            }

            if (orbitalBehaviors.Length > 1)
            {
                Debug.LogError($"❌ ORBITAIS DUPLICADOS DETECTADOS: {orbitalBehaviors.Length} instâncias!");
                foundDuplicates = true;
            }

            if (boomerangBehaviors.Length > 1)
            {
                Debug.LogError($"❌ BUMERANGUES DUPLICADOS DETECTADOS: {boomerangBehaviors.Length} instâncias!");
                foundDuplicates = true;
            }

            if (!foundDuplicates)
            {
                Debug.Log("✅ Nenhum comportamento duplicado encontrado!");
            }
        }
        else
        {
            Debug.LogError("❌ PlayerStats não encontrado!");
        }
    }

    [ContextMenu("🎯 Testar Adição de Bumerangue ao Player")]
    public void TestBoomerangOnPlayer()
    {
        Debug.Log("🧪 Testando adição de bumerangue ao Player...");

        // Criar skill de teste
        SkillData testSkill = ScriptableObject.CreateInstance<SkillData>();
        testSkill.skillName = "TESTE_BUMERANGUE";
        testSkill.specificType = SpecificSkillType.Boomerang;
        testSkill.attackBonus = 30f;
        testSkill.activationInterval = 2f;
        testSkill.boomerangThrowRange = 8f;
        testSkill.boomerangThrowSpeed = 15f;
        testSkill.boomerangMaxTargets = 3;

        // Adicionar ao Player
        AddBoomerangBehaviorToPlayer(testSkill);

        // Verificar resultado
        if (playerStats != null)
        {
            var behaviors = playerStats.GetComponents<BoomerangSkillBehavior>();
            if (behaviors.Length > 0)
            {
                Debug.Log($"✅ SUCESSO: {behaviors.Length} BoomerangSkillBehavior encontrado(s) no Player!");
                Debug.Log($"   • Componente ativo: {behaviors[0].enabled}");
                Debug.Log($"   • Dano configurado: {behaviors[0].damage}");

                // Testar lançamento
                behaviors[0].TestBoomerang();

                // Remover extras se houver
                if (behaviors.Length > 1)
                {
                    for (int i = 1; i < behaviors.Length; i++)
                    {
                        Destroy(behaviors[i]);
                        Debug.Log($"🧹 Removido bumerangue duplicado #{i}");
                    }
                }
            }
            else
            {
                Debug.LogError("❌ FALHA: BoomerangSkillBehavior NÃO encontrado no Player!");
            }
        }
    }

    [ContextMenu("🔍 Verificar Componentes no Player")]
    public void CheckPlayerComponents()
    {
        if (playerStats == null)
        {
            Debug.LogError("❌ PlayerStats não encontrado!");
            return;
        }

        Debug.Log("=== 🔍 COMPONENTES NO PLAYER ===");

        // Verificar todos os componentes
        var components = playerStats.GetComponents<Component>();
        foreach (var comp in components)
        {
            Debug.Log($"• {comp.GetType().Name}");
        }

        // Verificar específicos
        var boomerangs = playerStats.GetComponents<BoomerangSkillBehavior>();
        var projectiles = playerStats.GetComponents<PassiveProjectileSkill2D>();
        var orbitals = playerStats.GetComponents<OrbitingProjectileSkillBehavior>();

        Debug.Log($"=== ESPECÍFICOS ===");
        Debug.Log($"• BoomerangSkillBehavior: {boomerangs.Length} instância(s)");
        Debug.Log($"• PassiveProjectileSkill2D: {projectiles.Length} instância(s)");
        Debug.Log($"• OrbitingProjectileSkillBehavior: {orbitals.Length} instância(s)");
    }

#if UNITY_EDITOR
    [ContextMenu("🚀 yEngine - Diagnóstico Completo")]
    public void yEngineDiagnostic()
    {
        yEngine();
    }

    [ContextMenu("🌀 yEngine - Testar Bumerangue Direto")]
    public void yEngineTestBoomerangDirect()
    {
        // Criar skill de teste rapidamente
        SkillData testSkill = ScriptableObject.CreateInstance<SkillData>();
        testSkill.skillName = "TESTE_DIRETO_yEngine";
        testSkill.specificType = SpecificSkillType.Boomerang;
        testSkill.attackBonus = 35f;
        testSkill.activationInterval = 1.5f;

        AddBoomerangBehaviorToPlayer(testSkill);
    }

    [ContextMenu("🌀 Criar Skill de Bumerangue de Teste")]
    public void CreateTestBoomerangSkill()
    {
        SkillData boomerangSkill = ScriptableObject.CreateInstance<SkillData>();
        boomerangSkill.skillName = "🌀 Bumerangue do Vento (TESTE)";
        boomerangSkill.description = "Um bumerangue que vai e volta, causando dano a múltiplos inimigos\n\n• Dano base: 25\n• Alcance: 8 metros\n• Atinge até 3 inimigos\n• Retorna ao jogador\n• Cura 10% do dano causado ao retornar";
        boomerangSkill.attackBonus = 25f;
        boomerangSkill.activationInterval = 2.5f;
        boomerangSkill.specificType = SpecificSkillType.Boomerang;
        boomerangSkill.element = PlayerStats.Element.Wind;
        boomerangSkill.boomerangThrowRange = 8f;
        boomerangSkill.boomerangThrowSpeed = 15f;
        boomerangSkill.boomerangMaxTargets = 3;
        boomerangSkill.boomerangHealOnReturn = true;
        boomerangSkill.boomerangHealPercent = 0.1f;
        boomerangSkill.rarity = SkillRarity.Uncommon;
        boomerangSkill.isPassive = true;

        // Adiciona TEMPORARIAMENTE
        AddSkill(boomerangSkill);
        Debug.Log("🌀 Skill de bumerangue de TESTE criada (some ao reiniciar)");
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
        string[] modifierNames = { "Fogo Intenso", "Gelo Penetrante", "Raio Carregado", "Veneno Mortal", "Vento Cortante" };
        string[] targetSkills = { "Ataque Automático", "Golpe Contínuo", "Proteção Passiva", "🌀 Bumerangue do Vento" };

        string randomName = modifierNames[Random.Range(0, modifierNames.Length)];
        string randomTarget = targetSkills[Random.Range(0, targetSkills.Length)];

        PlayerStats.SkillModifier modifier = new PlayerStats.SkillModifier
        {
            modifierName = randomName,
            targetSkillName = randomTarget,
            damageMultiplier = 1.3f,
            defenseMultiplier = 1.2f,
            element = (PlayerStats.Element)Random.Range(1, 6)
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
                int randomIndex = Random.Range(0, validSkills.Count);
                AddSkill(validSkills[randomIndex]);
            }
        }
    }

    [ContextMenu("🚨 DIAGNÓSTICO URGENTE: Verificar Sistema de 3 Skills")]
    public void UrgentDiagnosis()
    {
        Debug.Log("🚨 DIAGNÓSTICO URGENTE DO SISTEMA DE 3 SKILLS");

        // 1. Verificar SkillChoiceUI
        if (skillChoiceUI == null)
        {
            Debug.LogError("❌ SKILLCHOICEUI NULL!");
            skillChoiceUI = FindFirstObjectByType<SkillChoiceUI>();
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

        // Verificar tipos específicos
        Debug.Log($"🌀 Skills de bumerangue disponíveis: {availableSkills.FindAll(s => s.specificType == SpecificSkillType.Boomerang).Count}");

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

        // Remove comportamentos DO PLAYER (não do SkillManager)
        if (playerStats != null)
        {
            var orbitalBehaviors = playerStats.GetComponents<OrbitingProjectileSkillBehavior>();
            foreach (var behavior in orbitalBehaviors) Destroy(behavior);

            var boomerangBehaviors = playerStats.GetComponents<BoomerangSkillBehavior>();
            foreach (var behavior in boomerangBehaviors) Destroy(behavior);

            var projectileBehaviors = playerStats.GetComponents<PassiveProjectileSkill2D>();
            foreach (var behavior in projectileBehaviors) Destroy(behavior);

            Debug.Log($"🧹 {orbitalBehaviors.Length + boomerangBehaviors.Length + projectileBehaviors.Length} comportamentos removidos do player");
        }

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

    [ContextMenu("🔍 Ver Skills Ativas")]
    public void LogActiveSkills()
    {
        Debug.Log($"📚 Skills ativas: {activeSkills.Count}");
        foreach (var skill in activeSkills)
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

        // Informações específicas
        Debug.Log($"• Skills de Bumerangue: {GetSkillCountBySpecificType(SpecificSkillType.Boomerang)}");
        Debug.Log($"• Comportamentos Ativos: {GetActiveBehaviorsCount()}");
    }

    [ContextMenu("🚨 DEBUG URGENTE - Verificar destruição")]
    public void UrgentDestructionDebug()
    {
        Debug.Log("=== 🚨 DEBUG URGENTE - VERIFICAÇÃO DE DESTRUIÇÃO ===");

        // 1. Criar objeto teste
        GameObject testObj = new GameObject("URGENT_TEST_OBJECT");
        testObj.transform.position = transform.position;

        // 2. Adicionar componente marcador
        testObj.AddComponent<DestructionMarker>();

        // 3. Verificar em intervalos
        StartCoroutine(DestructionMonitor(testObj));
    }

    [ContextMenu("🔍 ENCONTRAR DESTRUIDORES")]
    public void FindDestroyers()
    {
        Debug.Log("=== 🔍 BUSCANDO SCRIPTS DESTRUIDORES ===");

        var allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        var destroyers = new List<MonoBehaviour>();

        foreach (var behaviour in allBehaviours)
        {
            var type = behaviour.GetType();
            var methods = type.GetMethods(System.Reflection.BindingFlags.Instance |
                                         System.Reflection.BindingFlags.Public |
                                         System.Reflection.BindingFlags.NonPublic);

            foreach (var method in methods)
            {
                if (method.Name.Contains("Destroy") ||
                    method.Name.Contains("Clean") ||
                    method.Name.Contains("Remove"))
                {
                    if (!destroyers.Contains(behaviour))
                    {
                        destroyers.Add(behaviour);
                        Debug.LogWarning($"⚠️ Possível destruidor: {type.Name} em {behaviour.gameObject.name}");
                        Debug.Log($"   Método suspeito: {method.Name}");
                    }
                }
            }
        }

        if (destroyers.Count == 0)
        {
            Debug.Log("✅ Nenhum script destruidor óbvio encontrado");
        }
    }

    [ContextMenu("🎯 TESTE ULTRA SIMPLES")]
    public void UltraSimpleTest()
    {
        Debug.Log("=== 🎯 TESTE ULTRA SIMPLES ===");

        // 1. Criar objeto na posição do jogador
        GameObject testObj = new GameObject("TEST_DIRECT");
        testObj.transform.position = transform.position + Vector3.right * 2f;

        // 2. Adicionar sprite VISÍVEL
        SpriteRenderer sr = testObj.AddComponent<SpriteRenderer>();

        // Criar sprite 100% vermelho
        Texture2D tex = new Texture2D(64, 64);
        Color[] colors = new Color[64 * 64];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.red;
        }
        tex.SetPixels(colors);
        tex.Apply();

        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        sr.color = Color.red;
        sr.sortingOrder = 9999;

        // 3. Tornar grande
        testObj.transform.localScale = new Vector3(2f, 2f, 1f);

        // 4. Não destruir
        Debug.Log($"✅ Objeto criado em: {testObj.transform.position}");
        Debug.Log($"🎯 Nome: {testObj.name}");
        Debug.Log($"📏 Escala: {testObj.transform.localScale}");

        // 5. Verificar em 5 segundos
        StartCoroutine(CheckTestObject(testObj));
    }
#endif

    // CLASSE INTERNA PARA MARCAÇÃO DE DESTRUIÇÃO
    private class DestructionMarker : MonoBehaviour
    {
        void OnDestroy()
        {
            Debug.LogError($"💀 DESTRUIDO: {gameObject.name} às {Time.time:F2}s");
        }
    }

    private IEnumerator DestructionMonitor(GameObject obj)
    {
        float[] checkTimes = { 0.1f, 0.5f, 1f, 2f, 5f };

        foreach (float time in checkTimes)
        {
            yield return new WaitForSeconds(time);

            if (obj == null)
            {
                Debug.LogError($"❌ OBJETO DESTRUÍDO ANTES DE {time}s!");
                break;
            }
            else
            {
                Debug.Log($"✅ Objeto ainda existe após {time}s: {obj.name}");
            }
        }

        if (obj != null)
        {
            Debug.Log($"🎉 Objeto sobreviveu a todos os checks!");
            Destroy(obj, 10f); // Limpar depois
        }
    }

    private IEnumerator CheckTestObject(GameObject obj)
    {
        yield return new WaitForSeconds(5f);

        if (obj == null)
        {
            Debug.LogError("❌❌❌ OBJETO FOI DESTRUÍDO!");
        }
        else
        {
            Debug.Log($"✅ Objeto ainda existe: {obj.name}");
            Debug.Log($"📍 Posição: {obj.transform.position}");
        }
    }

    // MÉTODO PARA CONTAR COMPORTAMENTOS ATIVOS
    private int GetActiveBehaviorsCount()
    {
        int count = 0;
        if (playerStats != null)
        {
            if (playerStats.GetComponent<BoomerangSkillBehavior>() != null) count++;
            if (playerStats.GetComponent<OrbitingProjectileSkillBehavior>() != null) count++;
            if (playerStats.GetComponent<PassiveProjectileSkill2D>() != null) count++;
        }
        return count;
    }

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