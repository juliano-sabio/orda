using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static PlayerStats;

public class PlayerStats : MonoBehaviour
{
    public static event System.Action OnDanoRecebido;
    public static event System.Action<float> OnXPColetado;
    public static event System.Action OnUltimateAtivada;
    public static event System.Action OnPlayerMorreu;

    [Header("Configuração de Dados (ScriptableObject)")]
    public CharacterData characterData;

    [Header("Status do Jogador")]
    public float health = 75f;
    public float maxHealth = 75f;
    public float attack = 7f;
    public float defense = 2f;
    public float speed = 8f;

    [Header("Sistema de Regeneração")]
    public float healthRegenRate = 0.4f;
    public float healthRegenDelay = 5f;
    private float timeSinceLastDamage = 0f;
    private bool isRegenerating = false;

    [Header("Sistema de Level")]
    public int level = 1;
    public float currentXP = 0f;
    public float xpToNextLevel = 100f;
    public float xpMultiplier = 1.35f;

    [Header("💨 Sistema de Dash")]
    public int dashCharges = 0;
    public int maxDashCharges = 3;

    [Header("📦 Sistema de Coleta de XP")]
    public float xpCollectionRadius = 3f;
    public float orbMoveSpeedMultiplier = 1f;
    public bool autoCollectXP = true;

    [Header("📦 Sistema de Coleta de Itens")]
    public float itemCollectionRadius = 2f;


    [Header("Visualização da Área de Coleta")]
    public bool showCollectionRadius = true;
    public Color collectionRadiusColor = new Color(0, 1, 0, 0.1f);

    [Header("Skills de Ataque")]
    public List<AttackSkill> attackSkills = new List<AttackSkill>();

    [Header("Skills de Defesa")]
    public List<DefenseSkill> defenseSkills = new List<DefenseSkill>();

    [Header("🚀 Sistema de Ultimate - JÁ COMEÇA COM")]
    public UltimateSkill ultimateSkill;
    public float ultimateCooldown = 30f;
    public float ultimateChargeTime = 0f;
    public bool ultimateReady = false;
    public bool ultimateBloqueada = false;

    [Header("🔵 Ultimate de Marcação de Posição")]
    public bool usarUltimateMarcacao = true;
    private bool temPosicaoMarcada = false;
    private Vector2 posicaoMarcada;
    private GameObject marcadorVisual;

    [Header("Configurações de Ativação")]
    public float attackActivationInterval = 2f;
    public float defenseActivationInterval = 3f;
    public float critChance = 0.1f;

    [Header("⚡ Sistema de Elementos")]
    public ElementSystem elementSystem = new ElementSystem();

    [Header("🎯 Sistema de Skills Adquiridas")]
    public List<SkillData> acquiredSkills = new List<SkillData>();

    public Element CurrentElement
    {
        get => elementSystem.currentElement;
        set => elementSystem.currentElement = value;
    }

    public float ElementalBonus => elementSystem.elementalBonus;

    private List<string> inventory = new List<string>();
    private Rigidbody2D rb;
    private List<SkillBehavior> activeSkillBehaviors = new List<SkillBehavior>();

    private float attackTimer = 0f;
    private float defenseTimer = 0f;
    public float shieldPoints = 0f;
    public float maxShieldPoints = 0f;
    public float bonusShieldPoints = 0f;
    private float shieldImmuneTimer = 0f;
    private const float ShieldBreakImmuneDuration = 0.3f;

    private UIManager uiManager;
    private DomoRetardanteUltimate domoUltimate;
    private bool ultimateBehaviorAtivo; // qualquer behavior de ultimate gerencia o próprio input
    private SkillManager skillManager;
    private StatusCardSystem cardSystem;
    private bool escolhaSkillEmAndamento;

    float       speedOriginalAntesSlow;
    bool        estaSlowado;
    Coroutine   corotinaRestaurarSlow;

    bool        estaEnvenenado;
    Coroutine   corotinaVeneno;
    Color       corOriginalAnteVeneno;
    float       tempoVenenoRestante;

    bool        estaQueimando;
    Coroutine   corotinaQueimadura;
    Color       corOriginalAnteQueimadura;
    float       tempoQueimaduraRestante;

    bool        estaParalizado;
    Coroutine   corotinaParalisia;
    Color       corOriginalAnteParalisia;

    [Header("Segunda Fase: Barra de Luz")]
    public float luzMaxima      = 100f;
    public float luzAtual       = 100f;
    public float taxaDrenagemLuz = 100f / 180f; // unidades por segundo; esvazia em 180s (3 min)

    bool        semLuzDebuffAtivo;
    Coroutine   corotinaDebuffLuz;

    void Awake()
    {
        if (characterData != null)
        {
            ApplyCharacterData();
        }
    }

    public void ApplyCharacterData()
    {
        if (characterData == null) return;

        // --- Espíritos de Evolução (upgrades permanentes por personagem) ---
        int espCharIndex = PlayerPrefs.GetInt("SelectedCharacter", 0);

        // --- Status Base ---
        maxHealth = characterData.maxHealth * EspiritoUpgradeSystem.GetMultiplicador(espCharIndex, 4);
        health = maxHealth;
        attack = characterData.baseAttack   * EspiritoUpgradeSystem.GetMultiplicador(espCharIndex, 0);
        defense = characterData.baseDefense * EspiritoUpgradeSystem.GetMultiplicador(espCharIndex, 1);
        speed = characterData.baseSpeed     * EspiritoUpgradeSystem.GetMultiplicador(espCharIndex, 5);

        // --- Sistema de Regeneração ---
        healthRegenRate = characterData.baseHealthRegen * EspiritoUpgradeSystem.GetMultiplicador(espCharIndex, 6);
        healthRegenDelay = characterData.baseRegenDelay;

        // --- Sistema de Cooldowns/Intervalos ---
        attackActivationInterval = characterData.baseAttackCooldown   * EspiritoUpgradeSystem.GetMultiplicador(espCharIndex, 3);
        defenseActivationInterval = characterData.baseDefenseCooldown * EspiritoUpgradeSystem.GetMultiplicador(espCharIndex, 7);

        // --- Crítico ---
        critChance = 0.1f * EspiritoUpgradeSystem.GetMultiplicador(espCharIndex, 2);

        // --- Progressão ---
        xpMultiplier = characterData.xpMultiplier;

        // --- Sistema de Elementos ---
        // Aqui usamos o ChangeElement para já aplicar os bônus que você configurou no script anterior
        ChangeElement(characterData.baseElement);

        // --- Ultimate ---
        int charIndex = PlayerPrefs.GetInt("SelectedCharacter", 0);
        int ultimateIndex = PlayerPrefs.GetInt($"SelectedUltimate_{charIndex}", 0);
        UltimateData ultimateData = characterData.GetUltimate(ultimateIndex);

        if (ultimateData != null)
        {
            ultimateSkill.skillName    = ultimateData.ultimateName;
            ultimateSkill.description  = ultimateData.description;
            ultimateSkill.specificType = ultimateData.ultimateType;
            ultimateSkill.baseDamage   = ultimateData.baseDamage;
            ultimateSkill.areaOfEffect = ultimateData.areaOfEffect;
            ultimateSkill.duration     = ultimateData.duration;
            ultimateSkill.element      = ultimateData.element;
            ultimateSkill.icon         = ultimateData.ultimateIcon;
            ultimateSkill.isActive     = true;

            if (!string.IsNullOrEmpty(ultimateData.behaviorScriptName))
                AplicarComportamentoUltimate(ultimateData);
        }

        // Aplica passiva selecionada
        if (characterData.passivasDisponiveis != null && characterData.passivasDisponiveis.Length > 0)
        {
            int passivaIndex = PlayerPrefs.GetInt($"SelectedPassiva_{charIndex}", 0);
            passivaIndex = Mathf.Clamp(passivaIndex, 0, characterData.passivasDisponiveis.Length - 1);
            PassiveData passiva = characterData.passivasDisponiveis[passivaIndex];
            if (passiva != null) AplicarPassiva(passiva);
        }

        UpdateUI();
    }
    public enum Element
    {
        None,
        Fire,
        Ice,
        Lightning,
        Poison,
        Earth,
        Wind
    }

    [System.Serializable]
    public class ElementSystem
    {
        public Element currentElement = Element.None;
        public float elementalBonus = 1.2f;
        public Dictionary<Element, ElementAffinity> elementAffinities = new Dictionary<Element, ElementAffinity>();

        public ElementSystem()
        {
            InitializeElementAffinities();
        }

        private void InitializeElementAffinities()
        {
            elementAffinities[Element.Fire] = new ElementAffinity
            {
                strongAgainst = new List<Element> { Element.Ice, Element.Poison },
                weakAgainst = new List<Element> { Element.Wind, Element.Earth }
            };

            elementAffinities[Element.Ice] = new ElementAffinity
            {
                strongAgainst = new List<Element> { Element.Wind, Element.Earth },
                weakAgainst = new List<Element> { Element.Fire, Element.Lightning }
            };

            elementAffinities[Element.Lightning] = new ElementAffinity
            {
                strongAgainst = new List<Element> { Element.Wind, Element.Poison },
                weakAgainst = new List<Element> { Element.Earth, Element.Fire }
            };

            elementAffinities[Element.Poison] = new ElementAffinity
            {
                strongAgainst = new List<Element> { Element.Earth, Element.Wind },
                weakAgainst = new List<Element> { Element.Fire, Element.Lightning }
            };

            elementAffinities[Element.Earth] = new ElementAffinity
            {
                strongAgainst = new List<Element> { Element.Lightning, Element.Poison },
                weakAgainst = new List<Element> { Element.Wind, Element.Ice }
            };

            elementAffinities[Element.Wind] = new ElementAffinity
            {
                strongAgainst = new List<Element> { Element.Fire, Element.Earth },
                weakAgainst = new List<Element> { Element.Ice, Element.Lightning }
            };
        }

        public float CalculateElementalMultiplier(Element attackElement, Element targetElement)
        {
            if (attackElement == Element.None || targetElement == Element.None)
                return 1f;

            if (elementAffinities.ContainsKey(attackElement))
            {
                var affinity = elementAffinities[attackElement];

                if (affinity.strongAgainst.Contains(targetElement))
                    return elementalBonus;
                if (affinity.weakAgainst.Contains(targetElement))
                    return 1f / elementalBonus;
            }

            return 1f;
        }

        public void ApplyElementalEffect(Element element, GameObject target)
        {
            if (target == null) return;

            switch (element)
            {
                case Element.Fire:
                    ApplyBurnEffect(target);
                    break;
                case Element.Ice:
                    ApplyFreezeEffect(target);
                    break;
                case Element.Lightning:
                    ApplyShockEffect(target);
                    break;
                case Element.Poison:
                    ApplyPoisonEffect(target);
                    break;
                case Element.Earth:
                    ApplySlowEffect(target);
                    break;
                case Element.Wind:
                    ApplyKnockbackEffect(target);
                    break;
            }
        }

        private void ApplyBurnEffect(GameObject target)
        {
        }

        private void ApplyFreezeEffect(GameObject target)
        {
        }

        private void ApplyShockEffect(GameObject target)
        {
        }

        private void ApplyPoisonEffect(GameObject target)
        {
        }

        private void ApplySlowEffect(GameObject target)
        {
        }

        private void ApplyKnockbackEffect(GameObject target)
        {
        }
    }

    [System.Serializable]
    public class ElementAffinity
    {
        public List<Element> strongAgainst = new List<Element>();
        public List<Element> weakAgainst = new List<Element>();
    }

    [System.Serializable]
    public class AttackSkill
    {
        public string skillName;
        public float baseDamage;
        public bool isActive;
        public float cooldown;
        public float currentCooldown = 0f;
        public Element element = Element.None;
        public List<SkillModifier> modifiers = new List<SkillModifier>();
        public float elementalEffectChance = 0.2f;
        public float elementalEffectDuration = 3f;

        public bool IsOnCooldown => currentCooldown > 0f;

        public float CalculateTotalDamage()
        {
            float totalDamage = baseDamage;
            foreach (var mod in modifiers)
            {
                totalDamage *= mod.damageMultiplier;
            }
            return totalDamage;
        }

        public Element GetEffectiveElement()
        {
            foreach (var mod in modifiers)
            {
                if (mod.element != Element.None)
                    return mod.element;
            }
            return element;
        }

        public Color GetElementColor()
        {
            return GetElementColor(GetEffectiveElement());
        }

        public static Color GetElementColor(Element element)
        {
            switch (element)
            {
                case Element.None: return Color.white;
                case Element.Fire: return new Color(1f, 0.3f, 0.1f);
                case Element.Ice: return new Color(0.1f, 0.5f, 1f);
                case Element.Lightning: return new Color(0.8f, 0.8f, 0.1f);
                case Element.Poison: return new Color(0.5f, 0.1f, 0.8f);
                case Element.Earth: return new Color(0.6f, 0.4f, 0.2f);
                case Element.Wind: return new Color(0.4f, 0.8f, 0.9f);
                default: return Color.white;
            }
        }

        public void UpdateCooldown(float deltaTime)
        {
            if (currentCooldown > 0f)
            {
                currentCooldown = Mathf.Max(0f, currentCooldown - deltaTime);
            }
        }

        public void StartCooldown()
        {
            currentCooldown = cooldown;
        }
    }

    [System.Serializable]
    public class DefenseSkill
    {
        public string skillName;
        public float baseDefense;
        public bool isActive;
        public float duration;
        public float cooldown;
        public float currentCooldown = 0f;
        public Element element = Element.None;
        public List<SkillModifier> modifiers = new List<SkillModifier>();

        public bool IsOnCooldown => currentCooldown > 0f;

        public float CalculateTotalDefense()
        {
            float totalDefense = baseDefense;
            foreach (var mod in modifiers)
            {
                totalDefense *= mod.defenseMultiplier;
            }
            return totalDefense;
        }

        public Color GetElementColor()
        {
            return AttackSkill.GetElementColor(element);
        }

        public void UpdateCooldown(float deltaTime)
        {
            if (currentCooldown > 0f)
            {
                currentCooldown = Mathf.Max(0f, currentCooldown - deltaTime);
            }
        }

        public void StartCooldown()
        {
            currentCooldown = cooldown;
        }
    }

    [System.Serializable]
    public class UltimateSkill
    {
        public string skillName;
        public string description;
        public SpecificSkillType specificType = SpecificSkillType.None;
        public float baseDamage;
        public bool isActive;
        public float areaOfEffect;
        public float duration;
        public Element element = Element.None;
        public UnityEngine.Sprite icon;
        public List<SkillModifier> modifiers = new List<SkillModifier>();

        public float CalculateTotalDamage()
        {
            float totalDamage = baseDamage;
            foreach (var mod in modifiers)
            {
                totalDamage *= mod.damageMultiplier;
            }
            return totalDamage;
        }

        public Element GetEffectiveElement()
        {
            foreach (var mod in modifiers)
            {
                if (mod.element != Element.None)
                    return mod.element;
            }
            return element;
        }

        public Color GetElementColor()
        {
            return AttackSkill.GetElementColor(GetEffectiveElement());
        }
    }

    [System.Serializable]
    public class SkillModifier
    {
        public string modifierName;
        public string targetSkillName;
        public float damageMultiplier = 1f;
        public float defenseMultiplier = 1f;
        public Element element = Element.None;
        public float duration = 0f;
        public float cooldownReduction = 0f;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        uiManager    = UIManager.Instance;
        domoUltimate = GetComponent<DomoRetardanteUltimate>();
        ultimateBehaviorAtivo = TemBehaviorUltimate();
        skillManager = SkillManager.Instance;
        cardSystem = StatusCardSystem.Instance;

        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(DelayedCharacterInitialization());
        UpdateUI();
        AtualizarIconePassivaUI();
    }

    void AtualizarIconePassivaUI()
    {
        if (uiManager == null || characterData == null) return;
        if (characterData.passivasDisponiveis == null || characterData.passivasDisponiveis.Length == 0) return;
        int charIdx    = PlayerPrefs.GetInt("SelectedCharacter", 0);
        int passIdx    = Mathf.Clamp(PlayerPrefs.GetInt($"SelectedPassiva_{charIdx}", 0), 0, characterData.passivasDisponiveis.Length - 1);
        PassiveData p  = characterData.passivasDisponiveis[passIdx];
        if (p != null) uiManager.SetPassivaIcon(p.passiveIcon, p.passiveName, p.description ?? "");
    }

    public void InitializeFromCharacterSelection()
    {
        StartCoroutine(DelayedCharacterInitialization());
    }

    private IEnumerator DelayedCharacterInitialization()
    {
        yield return null;

        CharacterSelectionManagerIntegrated selectionManager = FindAnyObjectByType<CharacterSelectionManagerIntegrated>();

        if (selectionManager != null && SkillManager.Instance != null)
        {
            yield return null;
            selectionManager.ApplyCharacterToPlayerSystems(this, SkillManager.Instance);
        }
        else
        {
            Debug.Log("CharacterSelectionManager não encontrado. Usando configurações padrão.");
            InitializeDefaultSkills();
        }

        UpdateUI();

        if (uiManager == null) uiManager = UIManager.Instance;
        if (uiManager != null) uiManager.UpdateSkillIcons();
    }

    void InitializeSkills()
    {
        attackSkills.Add(new AttackSkill
        {
            skillName = "Ataque Automático",
            baseDamage = 10f,
            isActive = true,
            cooldown = 2f,
            element = Element.None
        });

        attackSkills.Add(new AttackSkill
        {
            skillName = "Golpe Contínuo",
            baseDamage = 15f,
            isActive = true,
            cooldown = 3f,
            element = Element.None
        });

        defenseSkills.Add(new DefenseSkill
        {
            skillName = "Proteção Passiva",
            baseDefense = 5f,
            isActive = true,
            duration = 4f,
            cooldown = 5f,
            element = Element.None
        });

        defenseSkills.Add(new DefenseSkill
        {
            skillName = "Escudo Automático",
            baseDefense = 8f,
            isActive = true,
            duration = 5f,
            cooldown = 6f,
            element = Element.None
        });

        // ⭐ ULTIMATE - JÁ CONFIGURADA E ATIVA
        // Se o characterData já configurou a ultimate (via ApplyCharacterData no Awake),
        // não sobrescrever aqui - só usa o padrão "Fúria do Herói" como fallback.
        if (characterData == null || ultimateSkill == null || string.IsNullOrEmpty(ultimateSkill.skillName))
        {
            ultimateSkill = new UltimateSkill
            {
                skillName = "Fúria do Herói",
                baseDamage = 50f,
                isActive = true, // ← COMEÇA ATIVA
                areaOfEffect = 5f,
                duration = 3f,
                element = Element.None
            };
        }

        RecalcMaxShield();
    }

    public void InitializeDefaultSkills()
    {
        InitializeSkills();

        // 🚫 LIMPAR skills adquiridas no início
        acquiredSkills.Clear();

        // ✅ MAS GARANTIR que a ULTIMATE esteja configurada e ATIVA
        if (ultimateSkill != null)
        {
            ultimateSkill.isActive = true;
            ultimateReady = false;
            ultimateChargeTime = 0f;
        }

    }

    void Update()
    {
        HandleMovement();
        HandleHealthRegeneration();
        UpdateSkillCooldowns();
        HandlePassiveSkills();
        // Behaviors de ultimate gerenciam o próprio cooldown e input (tecla R)
        if (!ultimateBehaviorAtivo)
        {
            UpdateUltimateSystem();

            if (Input.GetKeyDown(KeyCode.R) && ultimateReady && ultimateSkill.isActive)
            {
                if (usarUltimateMarcacao)
                {
                    if (!temPosicaoMarcada)
                        MarcarPosicao();
                    else
                        TeleportarParaMarcacao();
                }
                else
                {
                    ActivateUltimate();
                }
            }
        }

        HandleSkillToggleInput();
        HandleElementInput();
        HandleSkillManagerInput();
        HandleLuzDrain();
    }

    void HandleLuzDrain()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "segunda_fase") return;
        if (Fase2LuzManager.AnimandoEntrada) return;
        DrenarLuz(taxaDrenagemLuz * Time.deltaTime);

        float pct = GetLuzPercentual();
        Fase2LuzManager.SincronizarUI(pct);

        var luz = GetComponent<PlayerCollectLight>();
        if (luz != null) luz.AtualizarPorPercentual(pct);
    }

    void HandleHealthRegeneration()
    {
        timeSinceLastDamage += Time.deltaTime;

        if (timeSinceLastDamage >= healthRegenDelay && health < maxHealth)
        {
            if (!isRegenerating)
            {
                isRegenerating = true;
            }

            float regenAmount = healthRegenRate * Time.deltaTime;
            health = Mathf.Min(maxHealth, health + regenAmount);

            if (Time.frameCount % 30 == 0)
            {
                UpdateUI();
            }
        }
        else if (isRegenerating && health >= maxHealth)
        {
            isRegenerating = false;
        }
    }

    void UpdateSkillCooldowns()
    {
        foreach (var skill in attackSkills)
        {
            skill.UpdateCooldown(Time.deltaTime);
        }

        foreach (var skill in defenseSkills)
        {
            skill.UpdateCooldown(Time.deltaTime);
        }
    }

    void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");
        Vector2 movement = new Vector2(moveX, moveY).normalized;

        if (rb != null)
            rb.linearVelocity = movement * speed * Time.deltaTime;
    }

    public void ChangeElement(Element newElement)
    {
        Element previousElement = CurrentElement;
        RemoveElementBonus(previousElement);
        CurrentElement = newElement;
        ApplyElementBonus(newElement);


        if (uiManager != null)
            uiManager.ShowElementChanged(newElement.ToString());

        UpdateUI();
    }

    private void RemoveElementBonus(Element element)
    {
        switch (element)
        {
            case Element.Fire: attack -= 5f; break;
            case Element.Ice: defense -= 3f; break;
            case Element.Earth: defense -= 5f; break;
            case Element.Wind: speed -= 2f; break;
        }
    }

    private void ApplyElementBonus(Element element)
    {
        switch (element)
        {
            case Element.Fire:
                attack += 5f;
                break;
            case Element.Ice:
                defense += 3f;
                break;
            case Element.Lightning:
                attackActivationInterval *= 0.8f;
                break;
            case Element.Poison:
                break;
            case Element.Earth:
                defense += 5f;
                break;
            case Element.Wind:
                speed += 2f;
                break;
        }
    }

    void HandleElementInput()
    {
        if (Input.GetKeyDown(KeyCode.F1)) ChangeElement(Element.Fire);
        else if (Input.GetKeyDown(KeyCode.F2)) ChangeElement(Element.Ice);
        else if (Input.GetKeyDown(KeyCode.F3)) ChangeElement(Element.Lightning);
        else if (Input.GetKeyDown(KeyCode.F4)) ChangeElement(Element.Poison);
        else if (Input.GetKeyDown(KeyCode.F5)) ChangeElement(Element.Earth);
        else if (Input.GetKeyDown(KeyCode.F6)) ChangeElement(Element.Wind);
        else if (Input.GetKeyDown(KeyCode.F12)) ChangeElement(Element.None);
    }

    void HandleSkillManagerInput()
    {
        if (skillManager == null) return;

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F7)) skillManager.AddRandomSkill();
        if (Input.GetKeyDown(KeyCode.F8)) skillManager.AddRandomModifier();
        if (Input.GetKeyDown(KeyCode.F9)) skillManager.CreateTestOrbitalSkill();
        if (Input.GetKeyDown(KeyCode.F10)) skillManager.CheckIntegrationStatus();
        if (Input.GetKeyDown(KeyCode.F11)) GainXP(xpToNextLevel - currentXP);
#endif
    }

    void HandlePassiveSkills()
    {
        attackTimer += Time.deltaTime;
        if (attackTimer >= attackActivationInterval)
        {
            ActivatePassiveAttackSkills();
            attackTimer = 0f;
        }

        defenseTimer += Time.deltaTime;
        if (defenseTimer >= defenseActivationInterval)
        {
            ActivatePassiveDefenseSkills();
            defenseTimer = 0f;
        }

        if (shieldImmuneTimer > 0f)
            shieldImmuneTimer -= Time.deltaTime;
    }

    void ActivatePassiveAttackSkills()
    {
        foreach (var skill in attackSkills)
        {
            if (skill.isActive && !skill.IsOnCooldown)
            {
                float totalDamage = skill.CalculateTotalDamage();
                Element attackElement = skill.GetEffectiveElement();
                float finalDamage = CalculateElementalDamage(totalDamage, attackElement, Element.None);


                // APENAS ATIVA AS SKILLS MAS NÃO APLICA DANO EM ÁREA AUTOMÁTICO
                // O dano será aplicado pelos próprios comportamentos das skills (projéteis, etc.)

                skill.StartCooldown();
            }
        }
        UpdateUI();
    }

    void ActivatePassiveDefenseSkills()
    {
        float toAdd = 0f;
        foreach (var skill in defenseSkills)
        {
            if (skill.isActive && !skill.IsOnCooldown)
            {
                toAdd += skill.CalculateTotalDefense();
                skill.StartCooldown();
            }
        }
        if (toAdd > 0f)
        {
            shieldPoints = Mathf.Min(shieldPoints + toAdd, maxShieldPoints);
            UpdateUI();
        }
    }

    void RecalcMaxShield()
    {
        maxShieldPoints = bonusShieldPoints;
        foreach (var s in defenseSkills)
            if (s.isActive) maxShieldPoints += s.CalculateTotalDefense();
    }

    float CalculateElementalDamage(float baseDamage, Element attackElement, Element targetElement)
    {
        if (attackElement == Element.None || targetElement == Element.None)
            return baseDamage;

        float multiplier = elementSystem.CalculateElementalMultiplier(attackElement, targetElement);
        float finalDamage = baseDamage * multiplier;

        return finalDamage;
    }

    // MÉTODO PARA APLICAR DANO (chamado por outras habilidades)
    public void ApplyDamageToTarget(GameObject target, float damage, Element element = Element.None, bool isCrit = false)
    {
        if (target == null || target == gameObject) return;

        if (target.CompareTag("Enemy") || target.CompareTag("enemy"))
        {
            InimigoController inimigo = target.GetComponent<InimigoController>();
            if (inimigo != null)
            {
                // Aplica cálculo de crítico
                if (!isCrit)
                {
                    isCrit = UnityEngine.Random.value < critChance;
                }

                float finalDamage = isCrit ? damage * 2f : damage;
                inimigo.ReceberDano(finalDamage, isCrit);

                // Aplica efeito elemental
                if (element != Element.None)
                {
                    elementSystem.ApplyElementalEffect(element, target);
                }

            }
        }
    }

    void HandleSkillToggleInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) && attackSkills.Count > 0)
            ToggleAttackSkill(0, !attackSkills[0].isActive);
        if (Input.GetKeyDown(KeyCode.Alpha2) && attackSkills.Count > 1)
            ToggleAttackSkill(1, !attackSkills[1].isActive);
        if (Input.GetKeyDown(KeyCode.Alpha3) && defenseSkills.Count > 0)
            ToggleDefenseSkill(0, !defenseSkills[0].isActive);
        if (Input.GetKeyDown(KeyCode.Alpha4) && defenseSkills.Count > 1)
            ToggleDefenseSkill(1, !defenseSkills[1].isActive);
    }

    public void GainXP(float xpAmount)
    {
        currentXP += xpAmount;
        OnXPColetado?.Invoke(xpAmount);

        if (uiManager != null)
            uiManager.ShowXPGained(xpAmount);

        while (currentXP >= xpToNextLevel)
        {
            LevelUp();
        }
        UpdateUI();
    }

    private void LevelUp()
    {
        level++;
        currentXP -= xpToNextLevel;
        xpToNextLevel = CalculateXPForNextLevel();

        GetComponent<LevelUpEffect>()?.Executar(level);


        // Garante referência atualizada ao SkillManager
        if (skillManager == null) skillManager = SkillManager.Instance;
        if (skillManager == null) skillManager = FindFirstObjectByType<SkillManager>();

        // Milestones fixos de skill (1, 3, 6, 10)
        bool isMilestone  = level == 1 || level == 3 || level == 6 || level == 10;
        bool maxSkills    = skillManager != null && skillManager.activeSkills.Count >= 4;
        bool isSkillLevel = isMilestone && !maxSkills;

        if (isSkillLevel && skillManager != null)
        {
            if (!escolhaSkillEmAndamento)
                StartCoroutine(AbrirEscolhaSkill());
        }
        else if (cardSystem != null)
        {
            cardSystem.OnPlayerLevelUp(level);
        }

        if (uiManager != null)
            uiManager.ShowSkillAcquired($"Level {level}", "Novas habilidades disponiveis!");
    }

    private IEnumerator AbrirEscolhaSkill()
    {
        escolhaSkillEmAndamento = true;

        if (UIManager.Instance != null && UIManager.Instance.skillAcquiredPanel != null)
            UIManager.Instance.skillAcquiredPanel.SetActive(false);

        yield return new WaitForSecondsRealtime(0.4f);

        if (skillManager == null) { escolhaSkillEmAndamento = false; yield break; }

        var choiceUI = FindFirstObjectByType<SkillChoiceUI>(FindObjectsInactive.Include);
        if (choiceUI == null) { escolhaSkillEmAndamento = false; yield break; }

        int proximoSlot = skillManager.activeSkills.Count + 1;
        bool ehAtaque   = (proximoSlot % 2 == 1);
        choiceUI.somenteSkillsDeAtaque = ehAtaque;
        choiceUI.somenteSkillsDeDefesa = !ehAtaque;

        choiceUI.ShowRandomSkillChoice(skill =>
        {
            if (skillManager != null) skillManager.AddSkill(skill);
            escolhaSkillEmAndamento = false;
        });
    }

    private float CalculateXPForNextLevel()
    {
        return 100f * Mathf.Pow(xpMultiplier, level - 1);
    }

    [HideInInspector] public bool invulneravel = false;

    public void TakeDamage(float damage)
    {
        if (invulneravel) return;

        // Esquiva Ventosa (infusão defensiva) — chance de evadir totalmente
        var esquivaMk = GetComponent<EsquivaMarker>();
        if (esquivaMk != null && UnityEngine.Random.value < esquivaMk.chance)
        {
            if (uiManager != null) uiManager.ShowElementChanged("ESQUIVA!");
            return;
        }

        ShieldAuraBehavior aura = GetComponentInChildren<ShieldAuraBehavior>();
        if (aura != null && aura.TryBlockDamage())
        {
            if (uiManager != null) uiManager.ShowElementChanged("BLOQUEADO!");
            return;
        }

        if (shieldImmuneTimer > 0f) return;

        float remaining = Mathf.Max(0f, damage - defense * 0.5f);

        // Pele de Pedra (infusão defensiva) — redução fixa enquanto ativa
        var peleMk = GetComponent<PeleDePedraMarker>();
        if (peleMk != null) remaining *= (1f - peleMk.reducao);

        // Espelho Mágico — reflete o dano ao atacante mais próximo
        var espelho = GetComponent<EspelhoMagicoSkillBehavior>();
        if (espelho != null && espelho.TentarRefletir(remaining))
        {
            UpdateUI();
            return;
        }

        // Escudo de Karma — absorve o hit completamente
        var karma = GetComponent<EscudoKarmaSkillBehavior>();
        if (karma != null && karma.AbsorverHit(damage))
        {
            UpdateUI();
            return;
        }

        // Barreira Reflexiva — reflete parte do dano
        var barrReflex = GetComponent<BarreiraReflexivaSkillBehavior>();
        if (barrReflex != null) remaining = barrReflex.AplicarReflexao(remaining);

        // Aureola — reduz dano enquanto ativa
        var aureola = GetComponent<AureolaSkillBehavior>();
        if (aureola != null) remaining = aureola.AplicarReducao(remaining);

        if (shieldPoints > 0f)
        {
            float absorbed = Mathf.Min(shieldPoints, remaining);
            shieldPoints -= absorbed;
            remaining -= absorbed;

            if (shieldPoints <= 0f)
                shieldImmuneTimer = ShieldBreakImmuneDuration;
        }

        health -= remaining;
        if (remaining > 0f)
            OnDanoRecebido?.Invoke();
        timeSinceLastDamage = 0f;
        isRegenerating = false;

        UpdateUI();

        if (health <= 0f)
        {
            var folego = GetComponent<UltimoFolegoPassiva>();
            if (folego != null && folego.TentarSobreviver())
            {
                UpdateUI();
                return;
            }

            var segundaChance = GetComponent<SegundaChanceSkillBehavior>();
            if (segundaChance != null && segundaChance.TentarReviver())
            {
                UpdateUI();
                return;
            }

            Die();
        }
    }

    public void Heal(float healAmount)
    {
        health = Mathf.Min(maxHealth, health + healAmount);
        UpdateUI();
    }

    private void UpdateUltimateSystem()
    {
        if (!ultimateSkill.isActive) return;
        if (ultimateBloqueada) return;

        if (!ultimateReady)
        {
            ultimateChargeTime += Time.deltaTime;
            if (ultimateChargeTime >= ultimateCooldown)
            {
                ultimateReady = true;
                ultimateChargeTime = ultimateCooldown;

                if (uiManager != null)
                    uiManager.SetUltimateReady(true);
            }
        }
    }

    public void ActivateUltimate()
    {
        if (!ultimateReady || !ultimateSkill.isActive || ultimateBloqueada) return;

        OnUltimateAtivada?.Invoke();
        float totalDamage = ultimateSkill.CalculateTotalDamage();
        Element ultimateElement = ultimateSkill.GetEffectiveElement();
        float finalDamage = CalculateElementalDamage(totalDamage, ultimateElement, Element.None);


        // Só aplica dano se a ultimate tiver baseDamage > 0
        if (ultimateSkill.baseDamage > 0)
        {
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, ultimateSkill.areaOfEffect);
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.gameObject == gameObject) continue;
                InimigoController inimigo = hitCollider.GetComponent<InimigoController>();
                if (inimigo != null)
                {
                    bool isCrit = UnityEngine.Random.value < 0.3f;
                    float ultimateDamage = isCrit ? finalDamage * 1.5f : finalDamage;
                    inimigo.ReceberDano(ultimateDamage, isCrit);
                    elementSystem.ApplyElementalEffect(ultimateElement, hitCollider.gameObject);
                }
            }
        }

        StartCoroutine(UltimateEffects(ultimateSkill.duration, ultimateElement));
        ultimateReady = false;
        ultimateChargeTime = 0f;

        if (uiManager != null)
        {
            uiManager.OnUltimateActivated();
            uiManager.ShowUltimateAcquired(Loc.SkillLabel(ultimateSkill.skillName), Loc.T("ui.ultimate_activated"));
        }
    }

    private void MarcarPosicao()
    {
        posicaoMarcada = transform.position;
        temPosicaoMarcada = true;
        ultimateReady = false;
        ultimateChargeTime = 0f;

        if (marcadorVisual != null)
            Destroy(marcadorVisual);

        marcadorVisual = new GameObject("MarcadorUltimate");
        marcadorVisual.transform.position = posicaoMarcada;
        marcadorVisual.AddComponent<MarcadorUltimate>();

        if (uiManager != null)
        {
            uiManager.OnUltimateActivated();
            uiManager.ShowUltimateAcquired("Ponto Marcado", "Recarregue e pressione R para retornar!");
        }
    }

    private void TeleportarParaMarcacao()
    {
        transform.position = posicaoMarcada;
        temPosicaoMarcada = false;
        ultimateReady = false;
        ultimateChargeTime = 0f;

        if (marcadorVisual != null)
        {
            Destroy(marcadorVisual);
            marcadorVisual = null;
        }

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (uiManager != null)
        {
            uiManager.OnUltimateActivated();
            uiManager.ShowUltimateAcquired("Retorno!", "Voltou ao ponto marcado!");
        }
    }

    private IEnumerator UltimateEffects(float duration, Element element)
    {
        float originalAttack = attack;
        float originalSpeed = speed;

        switch (element)
        {
            case Element.Fire: attack *= 1.5f; break;
            case Element.Ice: defense *= 1.3f; break;
            case Element.Lightning: attackActivationInterval *= 0.7f; break;
            case Element.Wind: speed *= 1.3f; break;
            default: attack *= 1.5f; speed *= 1.2f; break;
        }

        yield return new WaitForSeconds(duration);

        attack = originalAttack;
        speed = originalSpeed;
    }

    void AplicarPassiva(PassiveData p)
    {
        if (p.xpBonusPercent   > 0) xpMultiplier       *= (1f + p.xpBonusPercent);
        if (p.attackBonus      > 0) attack              += p.attackBonus;
        if (p.defenseBonus     > 0) defense             += p.defenseBonus;
        if (p.speedBonus       > 0) speed               += p.speedBonus;
        if (p.healthBonus      > 0) { maxHealth += p.healthBonus; health = maxHealth; }
        if (p.regenBonus       > 0) healthRegenRate     += p.regenBonus;
        if (p.cooldownReduction > 0)
        {
            attackActivationInterval  *= (1f - p.cooldownReduction);
            defenseActivationInterval *= (1f - p.cooldownReduction);
        }

        if (!string.IsNullOrEmpty(p.behaviorScriptName))
            AplicarComportamentoPassiva(p.behaviorScriptName);

        if (uiManager != null) uiManager.SetPassivaIcon(p.passiveIcon, p.passiveName, p.description ?? "");
    }

    void AplicarComportamentoPassiva(string behaviorName)
    {
        switch (behaviorName)
        {
            case "UltimoFolegoPassiva":
                if (GetComponent<UltimoFolegoPassiva>() == null)
                    gameObject.AddComponent<UltimoFolegoPassiva>();
                break;
            case "SombraVelozPassiva":
                if (GetComponent<SombraVelozPassiva>() == null)
                    gameObject.AddComponent<SombraVelozPassiva>();
                break;
            case "PulsoVitalPassiva":
                if (GetComponent<PulsoVitalPassiva>() == null)
                    gameObject.AddComponent<PulsoVitalPassiva>();
                break;
            case "ImposicaoPassiva":
                if (GetComponent<ImposicaoPassiva>() == null)
                    gameObject.AddComponent<ImposicaoPassiva>();
                break;
            case "FocoPassiva":
                if (GetComponent<FocoPassiva>() == null)
                    gameObject.AddComponent<FocoPassiva>();
                break;
            case "RessurgenciaPassiva":
                if (GetComponent<RessurgenciaPassiva>() == null)
                    gameObject.AddComponent<RessurgenciaPassiva>();
                break;
        }
    }

    void AplicarComportamentoUltimate(UltimateData ultimateData)
    {
        // DestroyImmediate garante remoção síncrona — Destroy() é deferido e
        // GetComponent ainda retornaria o componente antigo no mesmo frame.
        var domoAntigo      = GetComponent<DomoRetardanteUltimate>();    if (domoAntigo      != null) DestroyImmediate(domoAntigo);
        var geloAntigo      = GetComponent<CampoDeGeloUltimate>();       if (geloAntigo      != null) DestroyImmediate(geloAntigo);
        var pulsoAntigo     = GetComponent<PulsoMagneticoUltimate>();    if (pulsoAntigo     != null) DestroyImmediate(pulsoAntigo);
        var tempAntigo      = GetComponent<TempestadeEletricaUltimate>(); if (tempAntigo     != null) DestroyImmediate(tempAntigo);
        var meteorAntigo    = GetComponent<ChuvaMeteorosUltimate>();     if (meteorAntigo    != null) DestroyImmediate(meteorAntigo);
        var vorticeAntigo   = GetComponent<VorticeUltimate>();           if (vorticeAntigo   != null) DestroyImmediate(vorticeAntigo);
        var drenagemAntigo  = GetComponent<DrenagemDeVidaUltimate>();    if (drenagemAntigo  != null) DestroyImmediate(drenagemAntigo);
        var anciaoAntigo    = GetComponent<DespertarAnciaoUltimate>();   if (anciaoAntigo    != null) DestroyImmediate(anciaoAntigo);
        var necropoleAntigo  = GetComponent<NecropoleUltimate>();         if (necropoleAntigo  != null) DestroyImmediate(necropoleAntigo);
        var punicaoAntigo    = GetComponent<PunicaoDivinaUltimate>();     if (punicaoAntigo    != null) DestroyImmediate(punicaoAntigo);
        var ritualAntigo     = GetComponent<RitualAnciaoUltimate>();      if (ritualAntigo     != null) DestroyImmediate(ritualAntigo);
        var correntesAntigo  = GetComponent<CorrentesInfernoUltimate>(); if (correntesAntigo  != null) DestroyImmediate(correntesAntigo);
        var bencaoAntigo     = GetComponent<BencaoAnciaoUltimate>();     if (bencaoAntigo     != null) DestroyImmediate(bencaoAntigo);
        var casuloAntigo     = GetComponent<CasuloCristalUltimate>();    if (casuloAntigo     != null) DestroyImmediate(casuloAntigo);
        var escudoAntigo     = GetComponent<EscudoSonicoUltimate>();     if (escudoAntigo     != null) DestroyImmediate(escudoAntigo);
        var raioAntigo       = GetComponent<RaioCerteiroUltimate>();    if (raioAntigo       != null) DestroyImmediate(raioAntigo);
        var mareAntigo       = GetComponent<MareImplacavelUltimate>();  if (mareAntigo       != null) DestroyImmediate(mareAntigo);

        switch (ultimateData.behaviorScriptName)
        {
            case "DomoRetardanteUltimate":
            {
                var c      = gameObject.AddComponent<DomoRetardanteUltimate>();
                c.cooldown = ultimateData.cooldown;
                c.duracao  = ultimateData.duration;
                c.raio     = ultimateData.areaOfEffect;
                break;
            }
            case "CampoDeGeloUltimate":
            {
                var c      = gameObject.AddComponent<CampoDeGeloUltimate>();
                c.cooldown = ultimateData.cooldown;
                c.duracao  = ultimateData.duration;
                c.raio     = ultimateData.areaOfEffect;
                break;
            }
            case "PulsoMagneticoUltimate":
            {
                var c          = gameObject.AddComponent<PulsoMagneticoUltimate>();
                c.cooldown     = ultimateData.cooldown;
                c.duracao      = ultimateData.duration;
                c.raio         = ultimateData.areaOfEffect;
                c.danoRepulsao = ultimateData.baseDamage;
                break;
            }
            case "TempestadeEletricaUltimate":
            {
                var c         = gameObject.AddComponent<TempestadeEletricaUltimate>();
                c.cooldown    = ultimateData.cooldown;
                c.duracao     = ultimateData.duration;
                c.raio        = ultimateData.areaOfEffect;
                c.danoPorBolt = ultimateData.baseDamage;
                break;
            }
            case "ChuvaMeteorosUltimate":
            {
                var c          = gameObject.AddComponent<ChuvaMeteorosUltimate>();
                c.cooldown     = ultimateData.cooldown;
                c.duracao      = ultimateData.duration;
                c.raio         = ultimateData.areaOfEffect;
                c.danoMeteoro  = ultimateData.baseDamage;
                break;
            }
            case "VorticeUltimate":
            {
                var c      = gameObject.AddComponent<VorticeUltimate>();
                c.cooldown = ultimateData.cooldown;
                c.duracao  = ultimateData.duration;
                c.raio     = ultimateData.areaOfEffect;
                break;
            }
            case "DrenagemDeVidaUltimate":
            {
                var c               = gameObject.AddComponent<DrenagemDeVidaUltimate>();
                c.cooldown          = ultimateData.cooldown;
                c.duracao           = ultimateData.duration;
                c.raio              = ultimateData.areaOfEffect;
                c.danoPorSegundo    = ultimateData.baseDamage;
                break;
            }
            case "DespertarAnciaoUltimate":
            {
                var c          = gameObject.AddComponent<DespertarAnciaoUltimate>();
                c.cooldown     = ultimateData.cooldown;
                c.duracao      = ultimateData.duration;
                c.raio         = ultimateData.areaOfEffect;
                c.danoGolpe    = ultimateData.baseDamage;
                break;
            }
            case "NecropoleUltimate":
            {
                var c              = gameObject.AddComponent<NecropoleUltimate>();
                c.cooldown         = ultimateData.cooldown;
                c.duracao          = ultimateData.duration;
                c.raio             = ultimateData.areaOfEffect;
                c.danoFantasma     = ultimateData.baseDamage;
                break;
            }
            case "PunicaoDivinaUltimate":
            {
                var c             = gameObject.AddComponent<PunicaoDivinaUltimate>();
                c.cooldown        = ultimateData.cooldown;
                c.danoImpacto     = ultimateData.baseDamage;
                c.danoSecundario  = ultimateData.baseDamage * 0.4f;
                c.raioExplosao    = ultimateData.areaOfEffect;
                break;
            }
            case "RitualAnciaoUltimate":
            {
                var c         = gameObject.AddComponent<RitualAnciaoUltimate>();
                c.cooldown    = ultimateData.cooldown;
                c.duracao     = ultimateData.duration;
                c.raio        = ultimateData.areaOfEffect;
                c.danoFinal   = ultimateData.baseDamage;
                break;
            }
            case "CasuloCristalUltimate":
            {
                var c             = gameObject.AddComponent<CasuloCristalUltimate>();
                c.cooldown        = ultimateData.cooldown;
                c.duracaoCasulo   = ultimateData.duration;
                c.danoEstilhacos  = ultimateData.baseDamage;
                c.raioExplosao    = ultimateData.areaOfEffect;
                break;
            }
            case "BencaoAnciaoUltimate":
            {
                var c              = gameObject.AddComponent<BencaoAnciaoUltimate>();
                c.cooldown         = ultimateData.cooldown;
                c.duracao          = ultimateData.duration;
                c.curaPorPulso     = ultimateData.baseDamage > 0 ? ultimateData.baseDamage / 100f : 0.08f;
                break;
            }
            case "CorrentesInfernoUltimate":
            {
                var c               = gameObject.AddComponent<CorrentesInfernoUltimate>();
                c.cooldown          = ultimateData.cooldown;
                c.duracao           = ultimateData.duration;
                c.raio              = ultimateData.areaOfEffect;
                if (ultimateData.baseDamage > 0f)
                    c.danoPorSegundo = ultimateData.baseDamage;
                break;
            }
            case "EscudoSonicoUltimate":
            {
                var c              = gameObject.AddComponent<EscudoSonicoUltimate>();
                c.cooldown         = ultimateData.cooldown;
                c.duracao          = ultimateData.duration;
                c.danoBasePorPulso = ultimateData.baseDamage;
                c.raio             = ultimateData.areaOfEffect;
                break;
            }
            case "RaioCerteiroUltimate":
            {
                var c              = gameObject.AddComponent<RaioCerteiroUltimate>();
                c.cooldown         = ultimateData.cooldown;
                c.danoPorRaio      = ultimateData.baseDamage;
                c.raioMaxBounce    = ultimateData.areaOfEffect;
                c.maxRicochetes    = (int)ultimateData.specialValue;
                break;
            }
            case "MareImplacavelUltimate":
            {
                var c          = gameObject.AddComponent<MareImplacavelUltimate>();
                c.cooldown     = ultimateData.cooldown;
                c.duracao      = ultimateData.duration;
                c.raio         = ultimateData.areaOfEffect;
                c.danoPorTick  = ultimateData.baseDamage;
                break;
            }
        }

        // Atualiza a flag usada no Update
        ultimateBehaviorAtivo = TemBehaviorUltimate();
        domoUltimate = GetComponent<DomoRetardanteUltimate>();
    }

    bool TemBehaviorUltimate()
    {
        return GetComponent<DomoRetardanteUltimate>()     != null
            || GetComponent<CampoDeGeloUltimate>()        != null
            || GetComponent<PulsoMagneticoUltimate>()     != null
            || GetComponent<TempestadeEletricaUltimate>() != null
            || GetComponent<ChuvaMeteorosUltimate>()      != null
            || GetComponent<VorticeUltimate>()            != null
            || GetComponent<DrenagemDeVidaUltimate>()     != null
            || GetComponent<DespertarAnciaoUltimate>()   != null
            || GetComponent<NecropoleUltimate>()          != null
            || GetComponent<PunicaoDivinaUltimate>()      != null
            || GetComponent<RitualAnciaoUltimate>()       != null
            || GetComponent<CorrentesInfernoUltimate>()   != null
            || GetComponent<BencaoAnciaoUltimate>()       != null
            || GetComponent<CasuloCristalUltimate>()      != null
            || GetComponent<EscudoSonicoUltimate>()       != null
            || GetComponent<RaioCerteiroUltimate>()       != null
            || GetComponent<MareImplacavelUltimate>()     != null;
    }

    private void UpdateUI()
    {
        if (uiManager != null)
        {
            uiManager.UpdatePlayerStatus();
        }
    }

    public ElementSystem GetElementSystem()
    {
        return elementSystem;
    }

    public Element GetCurrentElement()
    {
        return CurrentElement;
    }

    public void AddSkillModifier(SkillModifier modifier)
    {
        bool applied = false;

        foreach (var skill in attackSkills)
        {
            if (skill.skillName == modifier.targetSkillName)
            {
                skill.modifiers.Add(modifier);
                if (modifier.cooldownReduction > 0f)
                {
                    skill.cooldown = Mathf.Max(0.1f, skill.cooldown * (1f - modifier.cooldownReduction));
                }
                applied = true;
                if (uiManager != null)
                    uiManager.ShowModifierAcquired(modifier.modifierName, Loc.SkillLabel(skill.skillName));
            }
        }

        foreach (var skill in defenseSkills)
        {
            if (skill.skillName == modifier.targetSkillName)
            {
                skill.modifiers.Add(modifier);
                if (modifier.cooldownReduction > 0f)
                {
                    skill.cooldown = Mathf.Max(0.1f, skill.cooldown * (1f - modifier.cooldownReduction));
                }
                applied = true;
                if (uiManager != null)
                    uiManager.ShowModifierAcquired(modifier.modifierName, Loc.SkillLabel(skill.skillName));
            }
        }

        if (ultimateSkill.skillName == modifier.targetSkillName)
        {
            ultimateSkill.modifiers.Add(modifier);
            applied = true;
            if (uiManager != null)
                uiManager.ShowModifierAcquired(modifier.modifierName, Loc.SkillLabel(ultimateSkill.skillName));
        }

        if (!applied)
        {
            Debug.LogWarning($"⚠️ Nenhuma skill encontrada com o nome: {modifier.targetSkillName}");
        }

        UpdateUI();
    }

    public void ToggleAttackSkill(int index, bool active)
    {
        if (index >= 0 && index < attackSkills.Count)
        {
            attackSkills[index].isActive = active;
            UpdateUI();
        }
    }

    public void ToggleDefenseSkill(int index, bool active)
    {
        if (index >= 0 && index < defenseSkills.Count)
        {
            defenseSkills[index].isActive = active;
            RecalcMaxShield();
            UpdateUI();
        }
    }

    // 🆕 MÉTODOS PARA RECEBER SKILLS DO SKILLMANAGER
    public void ApplyAcquiredSkill(SkillData skill)
    {
        if (skill == null) return;

        if (!acquiredSkills.Contains(skill))
        {
            acquiredSkills.Add(skill);

            // ✅ VERIFICAÇÃO: Não aplicar bônus se for ultimate
            bool isUltimateSkill = skill.skillName.ToLower().Contains("ultimate");

            if (!isUltimateSkill)
            {
                // Aplica os bônus da skill (APENAS se NÃO for ultimate)
                attack += skill.attackBonus;
                defense += skill.defenseBonus;
                maxHealth += skill.healthBonus;
                health += skill.healthBonus;
                speed += skill.speedBonus;
                healthRegenRate += skill.healthRegenBonus;
                attackActivationInterval *= skill.attackSpeedMultiplier;
            }

            // Configura comportamento específico
            ConfigureSkillBehavior(skill);

            UpdateUI();
        }
    }

    // 🆕 MÉTODO PARA CONFIGURAR COMPORTAMENTO DA SKILL
    private void ConfigureSkillBehavior(SkillData skill)
    {
        switch (skill.specificType)
        {
            case SpecificSkillType.Projectile:
                if (skill.ShouldUseOrbitalBehavior())
                {
                    AddOrbitalProjectileBehavior(skill);
                }
                else
                {
                    AddProjectileBehavior(skill);
                }
                break;

            case SpecificSkillType.HealthRegen:
                AddHealthRegenBehavior(skill);
                break;

            case SpecificSkillType.CriticalStrike:
                AddCriticalStrikeBehavior(skill);
                break;
            // 🆕 NOVO CASO:
            case SpecificSkillType.Shield:
                AddShieldAuraBehavior(skill);
                break;
        }
    }

    // 🆕 MÉTODO PARA ADICIONAR COMPORTAMENTO DE PROJÉTIL ORBITAL
    private void AddOrbitalProjectileBehavior(SkillData skill)
    {
        var existingBehavior = GetComponent<OrbitingProjectileSkillBehavior>();
        if (existingBehavior != null)
        {
            existingBehavior.UpdateFromSkillData(skill);
            return;
        }

        OrbitingProjectileSkillBehavior orbitalBehavior = gameObject.AddComponent<OrbitingProjectileSkillBehavior>();
        orbitalBehavior.Initialize(this);
        orbitalBehavior.UpdateFromSkillData(skill);
        activeSkillBehaviors.Add(orbitalBehavior);

    }

    // 🆕 MÉTODO PARA ADICIONAR COMPORTAMENTO DE PROJÉTIL NORMAL
    private void AddProjectileBehavior(SkillData skill)
    {
        var existingBehavior = GetComponent<PassiveProjectileSkill2D>();
        if (existingBehavior != null)
        {
            existingBehavior.activationInterval *= 0.8f;
            return;
        }

        PassiveProjectileSkill2D projectileBehavior = gameObject.AddComponent<PassiveProjectileSkill2D>();
        projectileBehavior.Initialize(this);
        activeSkillBehaviors.Add(projectileBehavior);

    }

    // 🆕 MÉTODO PARA ADICIONAR REGENERAÇÃO DE VIDA
    private void AddHealthRegenBehavior(SkillData skill)
    {
        healthRegenRate += skill.healthRegenBonus;
        healthRegenDelay = Mathf.Max(1f, healthRegenDelay * 0.8f);

    }

    // 🆕 MÉTODO PARA ADICIONAR GOLPE CRÍTICO
    private void AddCriticalStrikeBehavior(SkillData skill)
    {
        attack += skill.attackBonus * 0.5f;
    }

    // 🆕 MÉTODO PARA VERIFICAR SE TEM UMA SKILL
    public bool HasSkill(string skillName)
    {
        return acquiredSkills.Exists(s => s.skillName == skillName);
    }

    public float GetXpCollectionRadius() => xpCollectionRadius;
    public float GetItemCollectionRadius() => itemCollectionRadius;

    public void SetXpCollectionRadius(float newRadius)
    {
        xpCollectionRadius = Mathf.Max(0.5f, newRadius);
    }

    public void BoostCollectionRadius(float boostAmount, float duration = 0f)
    {
        xpCollectionRadius += boostAmount;

        if (duration > 0f)
        {
            StartCoroutine(TemporaryRadiusBoost(boostAmount, duration));
        }
    }

    private IEnumerator TemporaryRadiusBoost(float boostAmount, float duration)
    {
        yield return new WaitForSeconds(duration);
        xpCollectionRadius -= boostAmount;
    }

    public void BoostOrbSpeed(float multiplier, float duration)
    {
        StartCoroutine(TemporaryOrbSpeedBoost(multiplier, duration));
    }

    private IEnumerator TemporaryOrbSpeedBoost(float multiplier, float duration)
    {
        orbMoveSpeedMultiplier *= multiplier;
        yield return new WaitForSeconds(duration);
        orbMoveSpeedMultiplier /= multiplier;
    }

    public void ForceUIUpdate()
    {
        UpdateUI();
    }

    private void Die()
    {
        OnPlayerMorreu?.Invoke();
        GetComponent<PlayerDeathEffect>()?.Executar();
        StartCoroutine(MorteComEfeito());
    }

    private IEnumerator MorteComEfeito()
    {
        yield return new WaitForSecondsRealtime(1.2f);
        if (Camera.main != null)
            Camera.main.backgroundColor = new Color(0.04f, 0.03f, 0.07f);
        yield return new WaitForEndOfFrame();
        if (!Application.isPlaying) yield break;
        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        Time.timeScale = 0f;
        GameOverUI.Mostrar(screenshot);
    }

    public bool HasDashCharge() => dashCharges > 0;

    public bool AddDashCharge()
    {
        if (dashCharges >= maxDashCharges) return false;
        dashCharges++;
        UpdateUI();
        return true;
    }

    public bool ConsumeDashCharge()
    {
        if (dashCharges <= 0) return false;
        dashCharges--;
        UpdateUI();
        return true;
    }

    // GETTERS
    public float GetCurrentHealth() => health;
    public float GetMaxHealth() => maxHealth;
    public float GetAttack() => attack;
    public float GetDefense() => defense;
    public float GetSpeed() => speed;
    public int GetLevel() => level;
    public float GetCurrentXP() => currentXP;
    public float GetXPToNextLevel() => xpToNextLevel;
    public List<string> GetInventory() => new List<string>(inventory);
    public List<AttackSkill> GetAttackSkills() => attackSkills;
    public List<DefenseSkill> GetDefenseSkills() => defenseSkills;
    public UltimateSkill GetUltimateSkill() => ultimateSkill;
    public float GetUltimateCooldown() => ultimateCooldown;
    public float GetUltimateChargeTime() => ultimateChargeTime;
    public bool IsUltimateReady() => ultimateReady;
    public bool HasUltimate() => ultimateSkill.isActive;
    public float GetAttackActivationInterval() => attackActivationInterval;
    public float GetDefenseActivationInterval() => defenseActivationInterval;
    public float GetAttackCooldownRemaining() => Mathf.Max(0f, attackActivationInterval - attackTimer);
    public float GetAttackCooldownPercentage() => attackActivationInterval > 0f ? attackTimer / attackActivationInterval : 1f;
    public float GetElementalBonus() => ElementalBonus;
    public float GetHealthRegenRate() => healthRegenRate;
    public bool IsRegeneratingHealth() => isRegenerating;
    public float GetShieldPoints() => shieldPoints;
    public float GetMaxShieldPoints() => maxShieldPoints;

    public void ApplyItemEffect(string itemName, string statType, float boostValue)
    {
        switch (statType.ToLower())
        {
            case "health":
                maxHealth += boostValue;
                health += boostValue;
                break;
            case "attack":
                attack += boostValue;
                break;
            case "defense":
                defense += boostValue;
                break;
            case "speed":
                speed += boostValue;
                break;
            case "regen":
                healthRegenRate += boostValue;
                break;
            case "collectionradius":
                BoostCollectionRadius(boostValue);
                break;
        }

        inventory.Add(itemName);
        UpdateUI();
    }

    public float GetSkillCooldown(string skillName)
    {
        foreach (var skill in attackSkills)
        {
            if (skill.skillName == skillName)
                return skill.currentCooldown;
        }

        foreach (var skill in defenseSkills)
        {
            if (skill.skillName == skillName)
                return skill.currentCooldown;
        }

        return 0f;
    }

    public float GetSkillCooldownPercentage(string skillName)
    {
        foreach (var skill in attackSkills)
        {
            if (skill.skillName == skillName)
                return skill.currentCooldown / skill.cooldown;
        }

        foreach (var skill in defenseSkills)
        {
            if (skill.skillName == skillName)
                return skill.currentCooldown / skill.cooldown;
        }

        return 0f;
    }
    public void AplicarSlow(float reducao, float duracao)
    {
        if (!estaSlowado)
        {
            speedOriginalAntesSlow = speed;
            estaSlowado = true;
        }

        speed = speedOriginalAntesSlow * (1f - Mathf.Clamp01(reducao));

        if (corotinaRestaurarSlow != null) StopCoroutine(corotinaRestaurarSlow);
        corotinaRestaurarSlow = StartCoroutine(RestaurarVelocidade(duracao));
    }

    public void CancelarSlow()
    {
        if (!estaSlowado) return;
        if (corotinaRestaurarSlow != null) { StopCoroutine(corotinaRestaurarSlow); corotinaRestaurarSlow = null; }
        speed       = speedOriginalAntesSlow;
        estaSlowado = false;
    }

    // Cancela o timer do slow sem tocar na velocidade — usado quando outro sistema já restaura speed
    public void CancelarTimerSlow()
    {
        if (corotinaRestaurarSlow != null) { StopCoroutine(corotinaRestaurarSlow); corotinaRestaurarSlow = null; }
        estaSlowado            = false;
        speedOriginalAntesSlow = speed;
    }

    public void AplicarVenenoPlayer(float danoPorTick, float intervalo, float duracao)
    {
        if (estaEnvenenado)
        {
            // Renova a duração sem reiniciar o tick — evita reset constante pelo OnTriggerStay2D
            tempoVenenoRestante = Mathf.Max(tempoVenenoRestante, duracao);
            return;
        }
        if (corotinaVeneno != null) StopCoroutine(corotinaVeneno);
        corotinaVeneno = StartCoroutine(CorotinaVeneno(danoPorTick, intervalo, duracao));
    }

    System.Collections.IEnumerator CorotinaVeneno(float danoPorTick, float intervalo, float duracao)
    {
        var sr = GetComponent<SpriteRenderer>();
        corOriginalAnteVeneno = sr != null ? sr.color : Color.white;
        estaEnvenenado        = true;
        tempoVenenoRestante   = duracao;
        if (sr != null) sr.color = new Color(0.45f, 0.85f, 0.35f);

        float tickTimer = 0f;
        while (tempoVenenoRestante > 0f)
        {
            tempoVenenoRestante -= Time.deltaTime;
            tickTimer           += Time.deltaTime;
            if (tickTimer >= intervalo)
            {
                tickTimer -= intervalo;
                TakeDamage(danoPorTick);
            }
            yield return null;
        }

        estaEnvenenado = false;
        corotinaVeneno = null;
        if (sr != null) sr.color = corOriginalAnteVeneno;
    }

    public void AplicarQueimaduraPlayer(float danoPorTick, float intervalo, float duracao)
    {
        if (estaQueimando)
        {
            // Renova a duração sem reiniciar o tick — evita reset constante pelo OnTriggerStay2D
            tempoQueimaduraRestante = Mathf.Max(tempoQueimaduraRestante, duracao);
            return;
        }
        if (corotinaQueimadura != null) StopCoroutine(corotinaQueimadura);
        corotinaQueimadura = StartCoroutine(CorotinaQueimadura(danoPorTick, intervalo, duracao));
    }

    System.Collections.IEnumerator CorotinaQueimadura(float danoPorTick, float intervalo, float duracao)
    {
        var sr = GetComponent<SpriteRenderer>();
        corOriginalAnteQueimadura = sr != null ? sr.color : Color.white;
        estaQueimando             = true;
        tempoQueimaduraRestante   = duracao;
        if (sr != null) sr.color = new Color(1f, 0.5f, 0.3f);

        float tickTimer = 0f;
        while (tempoQueimaduraRestante > 0f)
        {
            tempoQueimaduraRestante -= Time.deltaTime;
            tickTimer               += Time.deltaTime;
            if (tickTimer >= intervalo)
            {
                tickTimer -= intervalo;
                TakeDamage(danoPorTick);
            }
            yield return null;
        }

        estaQueimando      = false;
        corotinaQueimadura = null;
        if (sr != null) sr.color = corOriginalAnteQueimadura;
    }

    public void AplicarParalisiaPlayer(float duracao)
    {
        if (corotinaParalisia != null) StopCoroutine(corotinaParalisia);
        corotinaParalisia = StartCoroutine(CorotinaParalisia(duracao));
    }

    System.Collections.IEnumerator CorotinaParalisia(float duracao)
    {
        var sr = GetComponent<SpriteRenderer>();
        if (!estaParalizado)
            corOriginalAnteParalisia = sr != null ? sr.color : Color.white;

        estaParalizado = true;
        if (sr != null) sr.color = new Color(0.55f, 0.55f, 0.8f);

        yield return new WaitForSeconds(duracao);

        estaParalizado    = false;
        corotinaParalisia = null;
        if (sr != null) sr.color = corOriginalAnteParalisia;
    }

    // ─── Segunda Fase: Barra de Luz ─────────────────────────────────

    public float GetLuzPercentual() => luzMaxima > 0f ? Mathf.Clamp01(luzAtual / luzMaxima) : 0f;

    public void AdicionarLuz(float qtd)
    {
        luzAtual = Mathf.Clamp(luzAtual + qtd, 0f, luzMaxima);

        if (luzAtual > 0f && semLuzDebuffAtivo)
        {
            semLuzDebuffAtivo = false;
            if (corotinaDebuffLuz != null) { StopCoroutine(corotinaDebuffLuz); corotinaDebuffLuz = null; }
            CancelarSlow();
        }
    }

    public void DrenarLuz(float qtd)
    {
        luzAtual = Mathf.Clamp(luzAtual - qtd, 0f, luzMaxima);

        if (luzAtual <= 0f && !semLuzDebuffAtivo)
            corotinaDebuffLuz = StartCoroutine(CorotinaDebuffSemLuz());
    }

    System.Collections.IEnumerator CorotinaDebuffSemLuz()
    {
        semLuzDebuffAtivo = true;
        AplicarSlow(0.3f, 9999f);

        float tickTimer = 0f;
        while (luzAtual <= 0f)
        {
            tickTimer += Time.deltaTime;
            if (tickTimer >= 1f)
            {
                tickTimer -= 1f;
                TakeDamage(7f);
            }
            yield return null;
        }

        semLuzDebuffAtivo = false;
        corotinaDebuffLuz = null;
        CancelarSlow();
    }

    public void AddShieldAuraBehavior(SkillData skill)
    {
        if (GetComponentInChildren<ShieldAuraBehavior>() != null) return;

        if (skill.visualEffect != null)
        {
            // 1. Instancia
            GameObject auraObj = Instantiate(skill.visualEffect);

            // 2. Torna filho do Player (this.transform)
            auraObj.transform.SetParent(this.transform, false);

            // --- AQUI ESTÁ O AJUSTE DE ALTURA ---
            // Ajuste o 1.5f para mais ou para menos até ficar perfeito na cabeça
            auraObj.transform.localPosition = new Vector3(1.5f, 1.2f, 1.8f);
            // ------------------------------------

            auraObj.transform.localRotation = Quaternion.identity;

            // Mantemos o reset da escala que funcionou para você
            auraObj.transform.localScale = Vector3.one;

            ShieldAuraBehavior behavior = auraObj.GetComponent<ShieldAuraBehavior>();
            if (behavior != null) behavior.Initialize(this);
        }
    }

    private IEnumerator RestaurarVelocidade(float duracao)
    {
        yield return new WaitForSeconds(duracao);
        speed = speedOriginalAntesSlow;
        estaSlowado = false;
        corotinaRestaurarSlow = null;
    }
    // --- NOVOS MÉTODOS PARA O UIMANAGER ---

    // Retorna a chance de crítico atual
    public float GetCritChance()
    {
        // Como o seu sistema de crítico usa um valor fixo de 0.1f (10%) no ApplyDamageToTarget,
        // retornamos esse valor aqui para a UI. 
        return critChance;
    }

    // Retorna a regeneração de vida (o UIManager está procurando por este nome específico)
    public float GetHealthRegen()
    {
        return healthRegenRate;
    }

    // Caso o UIManager peça o dano base do jogador:
    void OnDrawGizmosSelected()
    {
        if (showCollectionRadius)
        {
            Gizmos.color = collectionRadiusColor;
            Gizmos.DrawWireSphere(transform.position, xpCollectionRadius);

            Gizmos.color = new Color(collectionRadiusColor.r, collectionRadiusColor.g, collectionRadiusColor.b, 0.3f);
            Gizmos.DrawSphere(transform.position, xpCollectionRadius);
        }
    }

}
