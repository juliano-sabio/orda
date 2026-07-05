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
            return;
        }

        // Limpa elementos aplicados nos ScriptableObjects ao iniciar
        // (evita que elementos de sessões de teste persistam)
        LimparElementosAplicados();
    }

    void LimparElementosAplicados()
    {
        foreach (var skill in availableSkills)
        {
            if (skill == null) continue;
            skill.appliedElement = ElementType.None;
            skill.appliedCharacteristicIndex = -1;
            skill.elementColor = Color.white;
        }
    }

    void Start()
    {
        playerStats = PlayerStats.Local;
        skillChoiceUI = FindSkillChoiceUIInUIManager();

        if (skillChoiceUI == null)
        {
            Debug.LogError("❌ SkillChoiceUI não encontrado no UIManager!");
            skillChoiceUI = FindFirstObjectByType<SkillChoiceUI>();
        }

        if (skillChoiceUI != null)
        {
            if (skillChoiceUI.allAvailableSkills == null || skillChoiceUI.allAvailableSkills.Count == 0)
                skillChoiceUI.allAvailableSkills = new List<SkillData>(availableSkills);
        }
        else
        {
            Debug.LogError("❌ SkillChoiceUI não encontrado após todas as tentativas!");
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        LoadSkillData();
        StartCoroutine(DelayedInitialCheck());
    }

    private IEnumerator DelayedInitialCheck()
    {
        yield return new WaitForSecondsRealtime(1.0f); // realtime: não congelar se o outro player pausar (co-op)

        if (playerStats == null)
        {
            playerStats = PlayerStats.Local;
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
    }

    private void CheckForInitialSkillChoice()
    {
        if (initialChoiceOffered) return;

        // Re-busca playerStats caso ainda não tenha sido encontrado
        if (playerStats == null)
            playerStats = PlayerStats.Local;
        if (playerStats == null) return;

        bool level1IsMilestone = System.Array.Exists(levelUpMilestones, milestone => milestone == 1);

        // Condição principal: level 1 é milestone e player não tem skills ainda
        bool deveOferecerEscolha = level1IsMilestone &&
                                   playerStats.level == 1 &&
                                   activeSkills.Count == 0;

        if (deveOferecerEscolha)
        {
            initialChoiceOffered = true;
            // Co-op: marca escolha pendente já agora, pra o overlay "aguardando" não piscar
            // no cliente enquanto o painel dele ainda vai abrir (o outro player pode ter pausado).
            if (NetSpawn.EmRede) CoopPause.MarcarEscolhaPendente();
            StartCoroutine(DelayedInitialChoice());
        }
    }

    private IEnumerator DelayedInitialChoice()
    {
        // Realtime: em co-op o painel do outro player pausa o jogo (timeScale=0); com WaitForSeconds
        // normal este timer congelaria e a escolha NUNCA abriria (cliente ficava preso no "aguardando").
        yield return new WaitForSecondsRealtime(1.5f);
        AplicarFiltroSlot();
        OfferSkillChoice();
    }

    // Slot 1=ataque, 2=defesa, 3=ataque, 4=defesa
    void AplicarFiltroSlot()
    {
        if (skillChoiceUI == null) return;
        int proximoSlot = activeSkills.Count + 1; // slot que será preenchido
        bool ehAtaque = (proximoSlot % 2 == 1);   // ímpares = ataque, pares = defesa
        skillChoiceUI.somenteSkillsDeAtaque = ehAtaque;
        skillChoiceUI.somenteSkillsDeDefesa = !ehAtaque;
    }

    private SkillChoiceUI FindSkillChoiceUIInUIManager()
    {
        UIManager uiManager = FindFirstObjectByType<UIManager>();

        if (uiManager != null)
        {
            SkillChoiceUI skillUI = uiManager.GetComponent<SkillChoiceUI>();
            if (skillUI != null) return skillUI;

            skillUI = uiManager.GetComponentInChildren<SkillChoiceUI>(true);
            if (skillUI != null) return skillUI;
        }
        else
        {
            Debug.LogError("❌ UIManager não encontrado na cena!");
        }

        // Busca no canvas dedicado ou em qualquer objeto da cena
        GameObject skillChoiceCanvas = GameObject.Find("SkillChoice_Canvas");
        if (skillChoiceCanvas != null)
        {
            SkillChoiceUI skillUI = skillChoiceCanvas.GetComponent<SkillChoiceUI>();
            if (skillUI != null) return skillUI;
            skillUI = skillChoiceCanvas.GetComponentInChildren<SkillChoiceUI>(true);
            if (skillUI != null) return skillUI;
        }

        return FindFirstObjectByType<SkillChoiceUI>(FindObjectsInactive.Include);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Aguardar 2 frames antes de buscar referências — garante que Awake/Start
        // de todos os objetos da nova cena já rodaram
        StartCoroutine(ConfigurarAposCarregarCena());
    }

    IEnumerator ConfigurarAposCarregarCena()
    {
        yield return null;
        yield return null;

        playerStats  = PlayerStats.Local;
        skillChoiceUI = FindSkillChoiceUIInUIManager();
        if (skillChoiceUI == null)
            skillChoiceUI = FindFirstObjectByType<SkillChoiceUI>(FindObjectsInactive.Include);

        if (skillChoiceUI != null &&
            (skillChoiceUI.allAvailableSkills == null || skillChoiceUI.allAvailableSkills.Count == 0))
            skillChoiceUI.allAvailableSkills = new List<SkillData>(availableSkills);

        // SP: cada carga de fase = RUN NOVA → começa com build LIMPA e re-oferece a escolha
        // inicial. O SkillManager é DontDestroyOnLoad e senão RECONECTAVA a build da run anterior
        // (skills + escolha inicial já "usada"). O player SP é recriado a cada reload (nasce limpo),
        // então basta zerar a LISTA — sem RemoveFromPlayer (nada a remover no player novo).
        // (Co-op reseta via FaseCoopBootstrap.ResetarParaNovaRun.)
        string cena = SceneManager.GetActiveScene().name;
        bool ehFase = !string.IsNullOrEmpty(cena) && (cena.Contains("fase") || cena.Contains("sobrevivencia"));
        if (ehFase)
        {
            // A escolha inicial de skill é re-oferecida em TODA run nova (SP e co-op). Sem isto,
            // ao recomeçar/voltar pro lobby+iniciar, a escolha inicial não aparecia de novo.
            initialChoiceOffered = false;

            // A LISTA de skills: SP zera aqui (player recriado, nada a remover). Co-op zera via
            // FaseCoopBootstrap.ResetarParaNovaRun (ClearAllSkills, por dono).
            if (!NetSpawn.EmRede)
            {
                activeSkills.Clear();
                currentlyEquippedSkill = null;
                selectedSkillIndex     = 0;
                SkillEvolutionManager.Instance?.Resetar();
            }
        }

        // Reconectar skill equipada após carregar cena
        if (currentlyEquippedSkill != null && activeSkills.Contains(currentlyEquippedSkill))
            OnSkillEquippedChanged?.Invoke(currentlyEquippedSkill);
        else if (activeSkills.Count > 0)
            EquipSkill(activeSkills[0]);

        // Retry escolha inicial: disparar mesmo que playerStats seja null agora
        // (DelayedInitialCheck buscará de novo depois de 1s)
        if (!initialChoiceOffered)
            StartCoroutine(DelayedInitialCheck());
    }

    public bool IsSkillLevel(int level)
    {
        if (activeSkills.Count >= 4) return false; // já tem o máximo de skills
        return System.Array.Exists(levelUpMilestones, m => m == level);
    }

    public void OnPlayerLevelUp(int newLevel)
    {
        if (playerStats == null)
        {
            playerStats = PlayerStats.Local;
            if (playerStats == null)
            {
                Debug.LogError("❌ PlayerStats null no level up!");
                return;
            }
        }

        if (IsSkillLevel(newLevel))
            StartCoroutine(OfferSkillChoiceWithDelay());

        ApplyLevelBonusToSkills(newLevel);
    }

    private IEnumerator OfferSkillChoiceWithDelay()
    {
        yield return null; // apenas 1 frame de delay
        OfferSkillChoice();
    }

    void OfferSkillChoice()
    {
        // Re-busca referências se nulas
        if (playerStats == null)
            playerStats = PlayerStats.Local;
        if (playerStats == null)
        {
            Debug.LogError("❌ PlayerStats não encontrado!");
            initialChoiceOffered = false; // permite retry na próxima cena
            return;
        }

        if (skillChoiceUI == null)
            skillChoiceUI = FindSkillChoiceUIInUIManager();
        if (skillChoiceUI == null)
            skillChoiceUI = FindFirstObjectByType<SkillChoiceUI>(FindObjectsInactive.Include);
        if (skillChoiceUI == null)
        {
            Debug.LogError("❌ SkillChoiceUI não encontrado!");
            initialChoiceOffered = false; // permite retry na próxima cena
            return;
        }

        if (skillChoiceUI.allAvailableSkills == null || skillChoiceUI.allAvailableSkills.Count == 0)
            skillChoiceUI.allAvailableSkills = new List<SkillData>(availableSkills);

        if (skillChoiceUI.allAvailableSkills.Count < 3)
        {
            Debug.LogWarning($"⚠️ Apenas {skillChoiceUI.allAvailableSkills.Count} skills disponíveis! Criando skills de teste...");
            CreateEmergencyTestSkills();
        }

        skillChoiceUI.ShowRandomSkillChoice(OnSkillSelectedFromChoice);
    }

    void OnSkillSelectedFromChoice(SkillData selectedSkill)
    {
        if (selectedSkill != null)
        {
            AddSkill(selectedSkill);
            if (currentlyEquippedSkill == null)
                EquipSkill(selectedSkill);
        }
        else
        {
            Debug.LogError("❌ Skill selecionada é null!");
        }
    }

    // MÉTODO PARA CRIAR SKILLS DE EMERGÊNCIA
    private void CreateEmergencyTestSkills()
    {

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

        return choices;
    }

    public void AddSkill(SkillData skill)
    {
        if (skill == null) return;
        if (activeSkills.Count >= 4) return; // máximo de 4 skills

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

                // Co-op: avisa o fantoche do colega pra rodar a versão COSMÉTICA desta skill
                // (gera o visual, sem dano). Só tipos suportados (dano já gateado).
                if (NetSpawn.EmRede && SkillFxCosmetico.EhSuportado(skill.specificType))
                {
                    var fx = playerStats.GetComponent<SkillFxNet>();
                    var pn = playerStats.GetComponent<PlayerNet>();
                    if (fx != null && pn != null)
                    {
                        int idx = fx.IndiceSkill(skill);
                        if (idx >= 0) pn.SincronizarSkillCosmetica(idx, (int)skill.appliedElement);
                    }
                }

                // Shield é prefab-based (não entra no SkillFxCosmetico): o fantoche instancia
                // a aura sustentada por caminho próprio.
                if (NetSpawn.EmRede && skill.specificType == SpecificSkillType.Shield)
                {
                    var fx = playerStats.GetComponent<SkillFxNet>();
                    var pn = playerStats.GetComponent<PlayerNet>();
                    if (fx != null && pn != null)
                    {
                        int idx = fx.IndiceSkill(skill);
                        if (idx >= 0) pn.SincronizarShieldEquip(idx);
                    }
                }
            }
            else
            {
                Debug.LogWarning("⚠️ PlayerStats não encontrado para aplicar a skill");
            }

            OnSkillAcquired?.Invoke(skill);

            // SE É A PRIMEIRA SKILL, EQUIPAR AUTOMATICAMENTE
            if (activeSkills.Count == 1 || currentlyEquippedSkill == null)
            {
                EquipSkill(skill);
            }

            if (UIManager.Instance != null)
                UIManager.Instance.ShowSkillAcquired(skill.GetDisplayName(), skill.GetFullDescription());
        }
    }

    // SISTEMA DE SKILL EQUIPADA
    public void EquipSkill(SkillData skill)
    {
        if (skill == null || !activeSkills.Contains(skill)) return;

        currentlyEquippedSkill = skill;
        selectedSkillIndex = activeSkills.IndexOf(skill);
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
        StartCoroutine(EquippedSkillHighlight(skill));
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
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Erro ao aplicar skill {skill.skillName}: {e.Message}");
        }
    }

    // ✅ MÉTODO CONFIGURESKILLBEHAVIOR CORRIGIDO
    void ConfigureSkillBehavior(SkillData skill)
    {
        if (playerStats == null)
        {
            Debug.LogError("❌ [yEngine] PlayerStats é null!");
            return;
        }

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

            case SpecificSkillType.EscudoRotativo:
                AddEscudoRotativoBehaviorToPlayer(skill);
                break;

            case SpecificSkillType.EscudoEspinhoso:
                AddEscudoEspinhosoBehaviorToPlayer(skill);
                break;

            case SpecificSkillType.Shield:
                if (playerStats == null)
                    playerStats = PlayerStats.Local;

                if (playerStats != null)
                    playerStats.AddShieldAuraBehavior(skill);
                else
                    Debug.LogError("❌ [SkillManager] Não consegui encontrar nenhum PlayerStats na cena!");
                break;

            case SpecificSkillType.CampoEspinhos:
                AddCampoEspinhosBehaviorToPlayer(skill);
                break;

            case SpecificSkillType.ChuvaEstrelas:
                AddChuvaEstrelasBehaviorToPlayer(skill);
                break;

            case SpecificSkillType.GarrasAbismo:
                AddGarrasAbismoBehaviorToPlayer(skill);
                break;

            case SpecificSkillType.FuriaLaminas:
                AddFuriaLaminasBehaviorToPlayer(skill);
                break;

            case SpecificSkillType.SombrasCruz:
                AddSombrasCruzBehaviorToPlayer(skill);
                break;

            case SpecificSkillType.CorteFantasma:
                AddCorteFantasmaBehaviorToPlayer(skill);
                break;

            case SpecificSkillType.SegundaChance:
                AddBehavior<SegundaChanceSkillBehavior>(skill);
                break;

            case SpecificSkillType.FugaSombras:
                AddBehavior<FugaSombrasSkillBehavior>(skill);
                break;

            case SpecificSkillType.BarreiraEnergia:
                AddBehavior<BarreiraEnergiaSkillBehavior>(skill);
                break;
            case SpecificSkillType.Aureola:
                AddBehavior<AureolaSkillBehavior>(skill);
                break;
            case SpecificSkillType.BarreiraReflexiva:
                AddBehavior<BarreiraReflexivaSkillBehavior>(skill);
                break;
            case SpecificSkillType.TeiaProtecao:
                AddBehavior<TeiaProtecaoSkillBehavior>(skill);
                break;
            case SpecificSkillType.InstintoSobrevivencia:
                AddBehavior<InstintoSobrevivenciaSkillBehavior>(skill);
                break;
            case SpecificSkillType.EspelhoMagico:
                AddBehavior<EspelhoMagicoSkillBehavior>(skill);
                break;
            case SpecificSkillType.EscudoKarma:
                AddBehavior<EscudoKarmaSkillBehavior>(skill);
                break;
            case SpecificSkillType.LancaLuz:
                AddBehavior<LancaLuzSkillBehavior>(skill);
                break;
            case SpecificSkillType.ChicoteEnergia:
                AddBehavior<ChicoteEnergiaSkillBehavior>(skill);
                break;
            case SpecificSkillType.MisseisTeleguiados:
                AddBehavior<MisseisTeleguiadosSkillBehavior>(skill);
                break;
            case SpecificSkillType.PulsoRitmico:
                AddBehavior<PulsoRitmicoSkillBehavior>(skill);
                break;
            case SpecificSkillType.EspadaFantasma:
                AddBehavior<EspadaFantasmaSkillBehavior>(skill);
                break;
            case SpecificSkillType.CorrenteSombria:
                AddBehavior<CorrenteSombriaSkillBehavior>(skill);
                break;
            case SpecificSkillType.CristaisGelo:
                AddBehavior<CristaisGeloSkillBehavior>(skill);
                break;

            case SpecificSkillType.HealthRegen:
                playerStats.healthRegenRate += skill.healthRegenBonus;
                break;

            case SpecificSkillType.CriticalStrike:
                break;

            default:
                break;
        }
    }

    void AddBehavior<T>(SkillData skill) where T : SkillBehavior
    {
        if (playerStats == null) playerStats = PlayerStats.Local;
        if (playerStats == null) return;
        var old = playerStats.GetComponent<T>();
        if (old != null) Destroy(old);
        var b = playerStats.gameObject.AddComponent<T>();
        b.Initialize(playerStats);

        // Chama ConfigurarDeSkillData via reflection se existir
        var m = typeof(T).GetMethod("ConfigurarDeSkillData");
        m?.Invoke(b, new object[] { skill });
    }

    void AddCorteFantasmaBehaviorToPlayer(SkillData skill)
    {
        if (playerStats == null) playerStats = PlayerStats.Local;
        if (playerStats == null) return;
        var old = playerStats.GetComponent<CorteFantasmaSkillBehavior>();
        if (old != null) Destroy(old);
        var behavior = playerStats.gameObject.AddComponent<CorteFantasmaSkillBehavior>();
        behavior.Initialize(playerStats);
        behavior.ConfigurarDeSkillData(skill);
    }

    void AddSombrasCruzBehaviorToPlayer(SkillData skill)
    {
        if (playerStats == null) playerStats = PlayerStats.Local;
        if (playerStats == null) return;
        var old = playerStats.GetComponent<SombrasCruzSkillBehavior>();
        if (old != null) Destroy(old);
        var behavior = playerStats.gameObject.AddComponent<SombrasCruzSkillBehavior>();
        behavior.Initialize(playerStats);
        behavior.ConfigurarDeSkillData(skill);
    }

    void AddFuriaLaminasBehaviorToPlayer(SkillData skill)
    {
        if (playerStats == null) playerStats = PlayerStats.Local;
        if (playerStats == null) return;
        var old = playerStats.GetComponent<FuriaLaminasSkillBehavior>();
        if (old != null) Destroy(old);
        var behavior = playerStats.gameObject.AddComponent<FuriaLaminasSkillBehavior>();
        behavior.Initialize(playerStats);
        behavior.ConfigurarDeSkillData(skill);
    }

    void AddGarrasAbismoBehaviorToPlayer(SkillData skill)
    {
        if (playerStats == null) playerStats = PlayerStats.Local;
        if (playerStats == null) return;
        var old = playerStats.GetComponent<GarrasAbismoSkillBehavior>();
        if (old != null) Destroy(old);
        var behavior = playerStats.gameObject.AddComponent<GarrasAbismoSkillBehavior>();
        behavior.Initialize(playerStats);
        behavior.ConfigurarDeSkillData(skill);
    }

    void AddChuvaEstrelasBehaviorToPlayer(SkillData skill)
    {
        if (playerStats == null) playerStats = PlayerStats.Local;
        if (playerStats == null) return;

        var old = playerStats.GetComponent<ChuvaEstrelasSkillBehavior>();
        if (old != null) Destroy(old);

        var behavior = playerStats.gameObject.AddComponent<ChuvaEstrelasSkillBehavior>();
        behavior.Initialize(playerStats);
        behavior.ConfigurarDeSkillData(skill);
    }

    void AddCampoEspinhosBehaviorToPlayer(SkillData skill)
    {
        if (playerStats == null)
        {
            playerStats = PlayerStats.Local;
            if (playerStats == null) return;
        }

        var old = playerStats.GetComponent<CampoEspinhosSkillBehavior>();
        if (old != null) Destroy(old);

        var behavior = playerStats.gameObject.AddComponent<CampoEspinhosSkillBehavior>();
        behavior.Initialize(playerStats);
        behavior.ConfigurarDeSkillData(skill);
    }
    // ✅ MÉTODO NOVO: Adiciona bumerangue ao Player
    void AddBoomerangBehaviorToPlayer(SkillData skill)
    {
        if (playerStats == null)
        {
            playerStats = PlayerStats.Local;
            if (playerStats == null)
            {
                Debug.LogError("❌ [yEngine] PlayerStats não encontrado na cena!");
                return;
            }
        }

        var oldBehaviors = playerStats.GetComponents<BoomerangSkillBehavior>();
        foreach (var oldBehavior in oldBehaviors)
            Destroy(oldBehavior);

        BoomerangSkillBehavior newBehavior = playerStats.gameObject.AddComponent<BoomerangSkillBehavior>();

        if (newBehavior == null)
        {
            Debug.LogError("❌ [yEngine] Falha ao criar BoomerangSkillBehavior!");
            return;
        }

        newBehavior.Initialize(playerStats);
        newBehavior.UpdateFromSkillData(skill);
        newBehavior.SetActive(true);
        StartCoroutine(TestBoomerangAfterDelay(newBehavior));
    }

    private IEnumerator TestBoomerangAfterDelay(BoomerangSkillBehavior behavior)
    {
        yield return new WaitForSeconds(2f);

        if (behavior != null)
        {
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

    void AddEscudoRotativoBehaviorToPlayer(SkillData skill)
    {
        if (playerStats == null)
        {
            playerStats = PlayerStats.Local;
            if (playerStats == null)
            {
                Debug.LogError("❌ [yEngine] PlayerStats não encontrado na cena!");
                return;
            }
        }

        var oldBehaviors = playerStats.GetComponents<EscudoRotativoSkillBehavior>();
        foreach (var old in oldBehaviors)
        {
            old.RemoveEffect();
            Destroy(old);
        }

        EscudoRotativoSkillBehavior behavior = playerStats.gameObject.AddComponent<EscudoRotativoSkillBehavior>();
        behavior.UpdateFromSkillData(skill);
        behavior.Initialize(playerStats);
    }

    void AddEscudoEspinhosoBehaviorToPlayer(SkillData skill)
    {
        if (playerStats == null)
        {
            playerStats = PlayerStats.Local;
            if (playerStats == null)
            {
                Debug.LogError("❌ [SkillManager] PlayerStats não encontrado para EscudoEspinhoso!");
                return;
            }
        }

        var old = playerStats.GetComponents<EscudoEspinhosoSkillBehavior>();
        foreach (var b in old) { b.RemoveEffect(); Destroy(b); }

        EscudoEspinhosoSkillBehavior behavior = playerStats.gameObject.AddComponent<EscudoEspinhosoSkillBehavior>();
        behavior.UpdateFromSkillData(skill);
        behavior.Initialize(playerStats);
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
            existingBehaviors[0].UpdateFromSkillData(skill);
            for (int i = 1; i < existingBehaviors.Length; i++)
                Destroy(existingBehaviors[i]);
            return;
        }

        OrbitingProjectileSkillBehavior orbitalBehavior = playerStats.gameObject.AddComponent<OrbitingProjectileSkillBehavior>();
        orbitalBehavior.Initialize(playerStats);
        orbitalBehavior.UpdateFromSkillData(skill);
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
            existingBehaviors[0].activationInterval = Mathf.Max(0.5f, existingBehaviors[0].activationInterval * 0.8f);
            for (int i = 1; i < existingBehaviors.Length; i++)
                Destroy(existingBehaviors[i]);
            return;
        }

        PassiveProjectileSkill2D projectileBehavior = playerStats.gameObject.AddComponent<PassiveProjectileSkill2D>();

        // Se for a espada, podemos configurar o comportamento direto aqui
        // sem precisar de classes novas.
        if (skill.skillName.Contains("Espada"))
        {
            projectileBehavior.activationInterval = skill.activationInterval;
            // Se o seu PassiveProjectile já tem um método Setup ou Initialize, use-o.
        }
        bool initialized = false;

        try
        {
            var initializeMethod = projectileBehavior.GetType().GetMethod("InitializeWithSkillData");
            if (initializeMethod != null)
            {
                initializeMethod.Invoke(projectileBehavior, new object[] { playerStats, skill });
                initialized = true;
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
                    updateMethod.Invoke(projectileBehavior, new object[] { skill });

                initialized = true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"⚠️ Inicialização fallback falhou: {e.Message}");
            }
        }

        if (!initialized)
            projectileBehavior.Initialize(playerStats);
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
            Debug.LogWarning("⚠️ Nenhuma skill encontrada em Resources/Skills/");
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

    public Sprite GetSkillIcon(string skillName)
    {
        var skill = activeSkills.Find(s => s.skillName == skillName);
        return skill?.icon;
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

        // 1. Verificar Player

        // 2. Verificar Skills disponíveis
        foreach (var skill in availableSkills)
        {
        }

        // 3. Verificar Skills ativas
        foreach (var skill in activeSkills)
        {
        }

        // 4. Verificar Skill equipada

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

    }

    // ✅ NOVO MÉTODO DE VERIFICAÇÃO DE DUPLICADOS
    [ContextMenu("🔍 Verificar Projéteis Duplicados")]
    public void CheckForDuplicateProjectiles()
    {

        if (playerStats == null)
        {
            playerStats = PlayerStats.Local;
        }

        if (playerStats != null)
        {
            // Verificar todos os componentes de projétil no player
            var projectileBehaviors = playerStats.GetComponents<PassiveProjectileSkill2D>();
            var orbitalBehaviors = playerStats.GetComponents<OrbitingProjectileSkillBehavior>();
            var boomerangBehaviors = playerStats.GetComponents<BoomerangSkillBehavior>();


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

                // Testar lançamento
                behaviors[0].TestBoomerang();

                // Remover extras se houver
                if (behaviors.Length > 1)
                {
                    for (int i = 1; i < behaviors.Length; i++)
                    {
                        Destroy(behaviors[i]);
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


        // Verificar todos os componentes
        var components = playerStats.GetComponents<Component>();
        foreach (var comp in components)
        {
        }

        // Verificar específicos
        var boomerangs = playerStats.GetComponents<BoomerangSkillBehavior>();
        var projectiles = playerStats.GetComponents<PassiveProjectileSkill2D>();
        var orbitals = playerStats.GetComponents<OrbitingProjectileSkillBehavior>();

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
    }

    [ContextMenu("🎯 Adicionar Skill de Teste")]
    public void AddTestSkills()
    {

        SkillData testSkill = ScriptableObject.CreateInstance<SkillData>();
        testSkill.skillName = "Projétil de Teste (EDITOR)";
        testSkill.description = "Projétil automático para teste - APENAS NO EDITOR";
        testSkill.attackBonus = 15f;
        testSkill.healthBonus = 10f;
        testSkill.specificType = SpecificSkillType.Projectile;
        testSkill.element = PlayerStats.Element.Fire;

        AddSkill(testSkill);
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

        // 1. Verificar SkillChoiceUI
        if (skillChoiceUI == null)
        {
            Debug.LogError("❌ SKILLCHOICEUI NULL!");
            skillChoiceUI = FindFirstObjectByType<SkillChoiceUI>();
        }
        else
        {
        }

        // 2. Verificar availableSkills

        // Verificar tipos específicos

        // 3. Forçar teste do sistema
        StartCoroutine(ForceThreeSkillsTest());
    }

    private IEnumerator ForceThreeSkillsTest()
    {
        yield return new WaitForSeconds(1f);

        // Chamar o sistema CORRETO
        if (skillChoiceUI != null)
        {
            skillChoiceUI.ShowRandomSkillChoice((selectedSkill) => {
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
        OfferSkillChoice();
    }

    [ContextMenu("🔧 Forçar Configuração de Skills")]
    public void ForceSkillConfiguration()
    {

        if (skillChoiceUI != null)
        {
            skillChoiceUI.allAvailableSkills = new List<SkillData>(availableSkills);
        }
        else
        {
            Debug.LogError("❌ SkillChoiceUI não encontrado!");
        }
    }

    [ContextMenu("🔍 Diagnosticar Problema de Escolha")]
    public void DiagnoseChoiceProblem()
    {


        if (skillChoiceUI != null)
        {
        }

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
    }
    // ✅ MÉTODO PARA SKILLS DE CORTE / MELEE (ESPADA)
   
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

        }

    }

    [ContextMenu("🔍 Ver Skills Disponíveis")]
    public void LogAvailableSkills()
    {
        foreach (var skill in availableSkills)
        {
        }
    }

    [ContextMenu("🔍 Ver Skills Ativas")]
    public void LogActiveSkills()
    {
        foreach (var skill in activeSkills)
        {
        }
    }

    [ContextMenu("🔍 Ver Milestones Configurados")]
    public void LogMilestones()
    {
    }

    [ContextMenu("🔄 Simular Level Up para Próximo Milestone")]
    public void SimulateNextMilestoneLevelUp()
    {
        if (playerStats == null) return;

        int nextMilestone = GetNextMilestone();
        if (nextMilestone != -1)
        {
            playerStats.level = nextMilestone - 1;
            OnPlayerLevelUp(nextMilestone);
        }
        else
        {
        }
    }

    [ContextMenu("Verificar Status da Integração")]
    public void CheckIntegrationStatus()
    {

        // Informações específicas
    }

    [ContextMenu("🚨 DEBUG URGENTE - Verificar destruição")]
    public void UrgentDestructionDebug()
    {

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
                    }
                }
            }
        }

        if (destroyers.Count == 0)
        {
        }
    }

    [ContextMenu("🎯 TESTE ULTRA SIMPLES")]
    public void UltraSimpleTest()
    {

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
            }
        }

        if (obj != null)
        {
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
    }

    void OnApplicationQuit()
    {
        SaveActiveSkills();
    }

    void OnDestroy()
    {
        OnSkillAcquired = null;
        OnModifierAcquired = null;
        OnSkillEquippedChanged = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
