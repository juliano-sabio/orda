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

    // 🆕 CONFIGURAÇÃO DE ESCOLHA INICIAL
    [Header("Configurações de Escolha Inicial")]
    public bool alwaysOfferInitialChoice = true;

    // Eventos
    public event Action<List<SkillData>, Action<SkillData>> OnSkillChoiceRequired;
    public event Action<SkillData> OnSkillAcquired;
    public event Action<SkillModifier> OnModifierAcquired;

    private PlayerStats playerStats;
    private SkillChoiceUI skillChoiceUI;
    private bool initialChoiceOffered = false;

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

        // 🆕 CORREÇÃO: Busca mais robusta do SkillChoiceUI
        skillChoiceUI = FindSkillChoiceUIInUIManager();

        if (skillChoiceUI == null)
        {
            Debug.LogError("❌ SkillChoiceUI não encontrado no UIManager!");
            skillChoiceUI = FindAnyObjectByType<SkillChoiceUI>();
        }

        if (skillChoiceUI != null)
        {
            Debug.Log($"✅ SkillChoiceUI encontrado: {skillChoiceUI.gameObject.name}");
            // 🆕 CORREÇÃO: Registra o evento usando método público
            RegisterSkillChoiceListener(skillChoiceUI.ShowSkillChoice);
        }
        else
        {
            Debug.LogError("❌ SkillChoiceUI não encontrado após todas as tentativas!");
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        LoadSkillData();

        Debug.Log("✅ SkillManager inicializado!");

        // 🆕 CORREÇÃO: Verificação melhorada para escolha inicial
        StartCoroutine(DelayedInitialCheck());
    }

    // 🆕 CORREÇÃO: Verificação com delay para garantir que tudo carregou
    private IEnumerator DelayedInitialCheck()
    {
        yield return new WaitForSeconds(1.0f);

        if (playerStats == null)
        {
            playerStats = FindAnyObjectByType<PlayerStats>();
            if (playerStats == null)
            {
                Debug.LogError("❌ PlayerStats ainda null após delay!");
                yield break;
            }
        }

        if (skillChoiceUI == null)
        {
            skillChoiceUI = FindSkillChoiceUIInUIManager();
            if (skillChoiceUI == null)
            {
                Debug.LogError("❌ SkillChoiceUI ainda null após delay!");
                yield break;
            }
            else
            {
                // 🆕 CORREÇÃO: Registra o evento se encontrado após delay
                RegisterSkillChoiceListener(skillChoiceUI.ShowSkillChoice);
            }
        }

        CheckForInitialSkillChoice();
    }

    // 🆕 CORREÇÃO: Método melhorado para verificação inicial
    private void CheckForInitialSkillChoice()
    {
        Debug.Log($"🔍 Verificando escolha inicial - Level: {playerStats?.level}, JáOferecida: {initialChoiceOffered}");

        if (!initialChoiceOffered && playerStats != null && playerStats.level == 1)
        {
            Debug.Log("🎯 Player começou no nível 1 - verificando escolha inicial...");

            bool level1IsMilestone = Array.Exists(levelUpMilestones, milestone => milestone == 1);
            bool shouldOffer = alwaysOfferInitialChoice || level1IsMilestone;

            if (shouldOffer)
            {
                Debug.Log("✅ Oferecendo escolha inicial!");
                initialChoiceOffered = true;
                OfferSkillChoice();
            }
            else
            {
                Debug.Log($"ℹ️ Sem escolha inicial - Level1IsMilestone: {level1IsMilestone}, AlwaysOffer: {alwaysOfferInitialChoice}");
            }
        }
    }

    private SkillChoiceUI FindSkillChoiceUIInUIManager()
    {
        UIManager uiManager = FindAnyObjectByType<UIManager>();

        if (uiManager != null)
        {
            Debug.Log($"✅ UIManager encontrado: {uiManager.gameObject.name}");

            // Procura como componente direto
            SkillChoiceUI skillUI = uiManager.GetComponent<SkillChoiceUI>();
            if (skillUI != null)
            {
                Debug.Log("✅ SkillChoiceUI encontrado como componente do UIManager");
                return skillUI;
            }

            // Procura nos children
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

        // 🆕 CORREÇÃO: Re-conectar eventos após carregar cena
        if (skillChoiceUI != null)
        {
            Debug.Log("✅ SkillChoiceUI reconectado após carregar cena");
            RegisterSkillChoiceListener(skillChoiceUI.ShowSkillChoice);
        }
    }

    // 🆕 CORREÇÃO: Método OnPlayerLevelUp mais robusto
    public void OnPlayerLevelUp(int newLevel)
    {
        Debug.Log($"📈 Player atingiu nível {newLevel} - Verificando milestones...");

        // 🆕 VERIFICAÇÃO DE SEGURANÇA
        if (playerStats == null)
        {
            playerStats = FindAnyObjectByType<PlayerStats>();
            if (playerStats == null)
            {
                Debug.LogError("❌ PlayerStats null no level up!");
                return;
            }
        }

        if (skillChoiceUI == null)
        {
            skillChoiceUI = FindSkillChoiceUIInUIManager();
            if (skillChoiceUI == null)
            {
                Debug.LogError("❌ SkillChoiceUI null no level up!");
                return;
            }
        }

        bool isMilestone = Array.Exists(levelUpMilestones, milestone => milestone == newLevel);
        Debug.Log($"🎯 Level {newLevel} é milestone: {isMilestone}");

        if (isMilestone)
        {
            Debug.Log($"🎯 Oferecendo escolha de skill para milestone nível {newLevel}");
            StartCoroutine(OfferSkillChoiceWithDelay());
        }
        else
        {
            Debug.Log($"ℹ️ Level {newLevel} não é milestone - sem escolha de skills");
        }

        ApplyLevelBonusToSkills(newLevel);
    }

    // 🆕 NOVO MÉTODO: Oferece escolha com pequeno delay
    private IEnumerator OfferSkillChoiceWithDelay()
    {
        yield return new WaitForSeconds(0.5f);
        OfferSkillChoice();
    }

    // 🆕 CORREÇÃO: Método OfferSkillChoice mais robusto
    void OfferSkillChoice()
    {
        Debug.Log("🎯 Iniciando OfferSkillChoice...");

        // 🆕 VERIFICAÇÕES DE SEGURANÇA
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

        List<SkillData> choices = GetRandomSkillChoices(skillsPerChoice);

        if (choices.Count == 0)
        {
            Debug.LogWarning("⚠️ Nenhuma skill disponível para escolha!");
            return;
        }

        Debug.Log($"🎯 Oferecendo {choices.Count} skills para escolha no nível {playerStats.level}");

        // 🆕 CORREÇÃO: Verifica se há listeners no evento
        if (OnSkillChoiceRequired != null)
        {
            Debug.Log($"📡 Evento OnSkillChoiceRequired tem {OnSkillChoiceRequired.GetInvocationList().Length} listeners");
            OnSkillChoiceRequired?.Invoke(choices, OnSkillSelectedFromChoice);
        }
        else
        {
            Debug.LogError("❌ Nenhum listener registrado no evento OnSkillChoiceRequired!");

            // 🆕 FALLBACK: Tenta chamar diretamente o SkillChoiceUI
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

        Debug.Log($"🔍 Skills disponíveis para escolha: {choices.Count}");
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
            ConfigureSkillBehavior(skill);
            ApplySkillModifiers(skill);

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
        PassiveProjectileSkill2D projectileBehavior = playerStats.gameObject.AddComponent<PassiveProjectileSkill2D>();
        Debug.Log($"✅ Comportamento de projétil 2D adicionado: {skill.skillName}");
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

        if (availableSkills.Count == 0)
        {
            Debug.LogWarning("⚠️ Nenhuma skill encontrada em Resources/Skills/");
        }
        else
        {
            Debug.Log($"✅ {availableSkills.Count} skills carregadas");
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
                    ConfigureSkillBehavior(skill);
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

    // 🆕 MÉTODOS PÚBLICOS PARA REGISTRO DE EVENTOS
    /// <summary>
    /// 🆕 Método público para registrar listeners no evento OnSkillChoiceRequired
    /// </summary>
    public void RegisterSkillChoiceListener(Action<List<SkillData>, Action<SkillData>> listener)
    {
        if (listener != null)
        {
            OnSkillChoiceRequired += listener;
            Debug.Log($"✅ Listener registrado no OnSkillChoiceRequired - Total: {OnSkillChoiceRequired?.GetInvocationList().Length ?? 0}");
        }
    }

    /// <summary>
    /// 🆕 Método público para remover listeners do evento OnSkillChoiceRequired
    /// </summary>
    public void UnregisterSkillChoiceListener(Action<List<SkillData>, Action<SkillData>> listener)
    {
        if (listener != null)
        {
            OnSkillChoiceRequired -= listener;
            Debug.Log($"✅ Listener removido do OnSkillChoiceRequired - Total: {OnSkillChoiceRequired?.GetInvocationList().Length ?? 0}");
        }
    }

    /// <summary>
    /// 🆕 Método para forçar o registro de todos os SkillChoiceUI encontrados
    /// </summary>
    [ContextMenu("🔧 Forçar Registro de Eventos")]
    public void ForceEventRegistration()
    {
        Debug.Log("🔧 Forçando registro de eventos...");

        // 🆕 CORREÇÃO: FindObjectsByType atualizado (sem depreciação)
        SkillChoiceUI[] allSkillUIs = FindObjectsByType<SkillChoiceUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"🔍 Encontrados {allSkillUIs.Length} SkillChoiceUI na cena");

        foreach (var skillUI in allSkillUIs)
        {
            if (skillUI != null)
            {
                // Usa o método público para registrar
                RegisterSkillChoiceListener(skillUI.ShowSkillChoice);

                // Ativa o GameObject se estiver inativo
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

    [ContextMenu("Verificar Configuração de Milestones")]
    public void CheckMilestonesConfiguration()
    {
        Debug.Log("🔍 Configuração de Milestones:");
        Debug.Log($"• Milestones: {string.Join(", ", levelUpMilestones)}");
        Debug.Log($"• Nível 1 é milestone: {Array.Exists(levelUpMilestones, milestone => milestone == 1)}");
        Debug.Log($"• Always Offer Initial Choice: {alwaysOfferInitialChoice}");
        Debug.Log($"• Skills por escolha: {skillsPerChoice}");

        int skillsForLevel1 = availableSkills.FindAll(skill => skill.requiredLevel <= 1).Count;
        Debug.Log($"• Skills disponíveis para nível 1: {skillsForLevel1}");
        Debug.Log($"• Skills ativas: {activeSkills.Count}");
        Debug.Log($"• Player nível: {playerStats?.level}");
        Debug.Log($"• Escolha inicial oferecida: {initialChoiceOffered}");
        Debug.Log($"• SkillChoiceUI conectado: {skillChoiceUI != null}");
        Debug.Log($"• Evento tem listeners: {OnSkillChoiceRequired?.GetInvocationList().Length ?? 0}");
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

    [ContextMenu("Limpar Todas as Skills")]
    public void ClearAllSkills()
    {
        foreach (var skill in activeSkills)
        {
            if (playerStats != null)
            {
                skill.RemoveFromPlayer(playerStats);

                var behaviors = playerStats.GetComponents<SkillBehavior>();
                foreach (var behavior in behaviors)
                {
                    Destroy(behavior);
                }
            }
        }

        activeSkills.Clear();
        activeModifiers.Clear();
        initialChoiceOffered = false;

        if (playerStats != null)
        {
            playerStats.InitializeDefaultSkills();
        }

        Debug.Log("🧹 Todas as skills foram removidas");
    }

    // 🆕 MÉTODOS DE DIAGNÓSTICO MELHORADOS
    [ContextMenu("🎯 DIAGNÓSTICO COMPLETO DA ESCOLHA INICIAL")]
    public void CompleteInitialChoiceDiagnostic()
    {
        Debug.Log("🎯 ========== DIAGNÓSTICO COMPLETO ==========");

        // 1. VERIFICA SE O EVENTO ESTÁ SENDO REGISTRADO
        Debug.Log("1. 📡 Verificando registro de eventos...");
        if (OnSkillChoiceRequired == null)
        {
            Debug.LogError("   ❌ OnSkillChoiceRequired event é NULL - ninguém se registrou!");
        }
        else
        {
            Debug.Log($"   ✅ Evento registrado - {OnSkillChoiceRequired.GetInvocationList().Length} listeners");
        }

        // 2. VERIFICA SE A VERIFICAÇÃO INICIAL ESTÁ ACONTECENDO
        Debug.Log("2. 🔍 Verificando verificação inicial...");
        Debug.Log($"   • Level Up Milestones: [{string.Join(", ", levelUpMilestones)}]");
        Debug.Log($"   • Level 1 é milestone: {Array.Exists(levelUpMilestones, milestone => milestone == 1)}");
        Debug.Log($"   • Always Offer Initial Choice: {alwaysOfferInitialChoice}");
        Debug.Log($"   • Player Level: {playerStats?.level}");
        Debug.Log($"   • Skills Ativas: {activeSkills.Count}");
        Debug.Log($"   • Escolha Inicial Já Oferecida: {initialChoiceOffered}");

        // 3. VERIFICA SE O MÉTODO OfferSkillChoice É CHAMADO
        Debug.Log("3. 🚀 Verificando chamada do OfferSkillChoice...");
        Debug.Log($"   • PlayerStats: {(playerStats != null ? "✅ Conectado" : "❌ Não encontrado")}");
        Debug.Log($"   • SkillChoiceUI: {(skillChoiceUI != null ? "✅ Conectado" : "❌ Não encontrado")}");
        Debug.Log($"   • Skills disponíveis: {availableSkills.Count}");

        // 4. TESTA OFERECER ESCOLHA AGORA
        Debug.Log("4. 🧪 Testando OfferSkillChoice agora...");
        bool canOfferNow = CanOfferSkillChoice();
        Debug.Log($"   • Pode oferecer agora: {canOfferNow}");

        if (canOfferNow)
        {
            Debug.Log("   🚀 Oferecendo escolha agora...");
            OfferSkillChoice();
        }

        Debug.Log("🎯 ========== FIM DO DIAGNÓSTICO ==========");
    }

    // 🆕 MÉTODO PARA FORÇAR ESCOLHA INICIAL
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

    // 🆕 MÉTODO PARA REINICIAR A ESCOLHA INICIAL
    [ContextMenu("🔄 Reiniciar Escolha Inicial")]
    public void ResetInitialChoice()
    {
        initialChoiceOffered = false;
        Debug.Log("🔄 Escolha inicial reiniciada - será oferecida novamente");
        StartCoroutine(DelayedInitialCheck());
    }

    // 🆕 MÉTODO PARA TESTE RÁPIDO DA UI
    [ContextMenu("🎯 TESTAR UI MANUALMENTE")]
    public void TestUIManually()
    {
        Debug.Log("🎯 TESTANDO UI MANUALMENTE...");

        // Cria skills de teste
        List<SkillData> testSkills = new List<SkillData>();

        for (int i = 1; i <= 3; i++)
        {
            SkillData testSkill = ScriptableObject.CreateInstance<SkillData>();
            testSkill.skillName = $"Skill Teste {i}";
            testSkill.description = $"Descrição da skill teste {i}";
            testSkill.element = (PlayerStats.Element)UnityEngine.Random.Range(0, 6);
            testSkill.requiredLevel = 1;
            testSkills.Add(testSkill);
        }

        // Força a oferta
        if (skillChoiceUI != null)
        {
            skillChoiceUI.ShowSkillChoice(testSkills, (selected) => {
                Debug.Log($"✅ Skill selecionada: {selected.skillName}");
            });
        }
        else
        {
            Debug.LogError("❌ SkillChoiceUI não encontrado!");
        }
    }

    [ContextMenu("Forçar Escolha de Skill")]
    public void ForceSkillChoice()
    {
        Debug.Log("🎯 Forçando escolha de skill...");
        OfferSkillChoice();
    }

    [ContextMenu("Verificar Status da Integração")]
    public void CheckIntegrationStatus()
    {
        Debug.Log("🔍 Status da Integração do SkillManager:");
        Debug.Log($"• Skills Disponíveis: {availableSkills.Count}");
        Debug.Log($"• Skills Ativas: {activeSkills.Count}");
        Debug.Log($"• PlayerStats: {(playerStats != null ? "✅ Conectado" : "❌ Não encontrado")}");
        Debug.Log($"• SkillChoiceUI: {(skillChoiceUI != null ? "✅ Conectado" : "❌ Não encontrado")}");
        Debug.Log($"• UIManager: {(FindAnyObjectByType<UIManager>() != null ? "✅ Encontrado" : "❌ Não encontrado")}");

        var projectileSkills = activeSkills.FindAll(s => s.specificType == SpecificSkillType.Projectile);
        Debug.Log($"• Skills de Projétil: {projectileSkills.Count}");
        Debug.Log($"• Escolha Inicial Oferecida: {initialChoiceOffered}");
        Debug.Log($"• Always Offer Initial Choice: {alwaysOfferInitialChoice}");
        Debug.Log($"• Evento OnSkillChoiceRequired: {(OnSkillChoiceRequired != null ? "✅ Registrado" : "❌ Null")}");
    }

    private bool CanOfferSkillChoice()
    {
        if (playerStats == null)
        {
            Debug.Log("❌ Cannot offer: PlayerStats null");
            return false;
        }

        if (skillChoiceUI == null)
        {
            Debug.Log("❌ Cannot offer: SkillChoiceUI null");
            return false;
        }

        List<SkillData> choices = GetRandomSkillChoices(skillsPerChoice);
        if (choices.Count == 0)
        {
            Debug.Log("❌ Cannot offer: No skills available for choice");
            return false;
        }

        Debug.Log("✅ Can offer skill choice!");
        return true;
    }

    void OnApplicationQuit()
    {
        SaveActiveSkills();
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

    [ContextMenu("Adicionar Skills de Teste")]
    public void AddTestSkills()
    {
        SkillData basicSkill = availableSkills.Find(s => s.requiredLevel <= 1 && s.specificType == SpecificSkillType.Projectile);
        if (basicSkill != null)
        {
            AddSkill(basicSkill);
        }
        else
        {
            Debug.LogWarning("⚠️ Nenhuma skill de teste encontrada para nível 1");
        }
    }

    void OnDestroy()
    {
        OnSkillChoiceRequired = null;
        OnSkillAcquired = null;
        OnModifierAcquired = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}