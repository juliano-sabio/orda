using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    [Header("Status do Jogador")]
    public float health = 100f;
    public float maxHealth = 100f;
    public float attack = 10f;
    public float defense = 5f;
    public float speed = 8f;

    [Header("Sistema de Regeneração")]
    public float healthRegenRate = 1f; // Vida por segundo
    public float healthRegenDelay = 5f; // Tempo sem levar dano para começar regeneração
    private float timeSinceLastDamage = 0f;
    private bool isRegenerating = false;

    [Header("Sistema de Level")]
    public int level = 1;
    public float currentXP = 0f;
    public float xpToNextLevel = 100f;
    public float xpMultiplier = 1.5f;

    [Header("Skills de Ataque")]
    public List<AttackSkill> attackSkills = new List<AttackSkill>();

    [Header("Skills de Defesa")]
    public List<DefenseSkill> defenseSkills = new List<DefenseSkill>();

    [Header("🚀 Sistema de Ultimate")]
    public UltimateSkill ultimateSkill;
    public float ultimateCooldown = 30f;
    public float ultimateChargeTime = 0f;
    public bool ultimateReady = false;

    [Header("Configurações de Ativação")]
    public float attackActivationInterval = 2f;
    public float defenseActivationInterval = 3f;

    // 🆕 SISTEMA DE ELEMENTOS COMPLETO
    [Header("⚡ Sistema de Elementos")]
    public ElementSystem elementSystem = new ElementSystem();

    // 🆕 PROPRIEDADE PARA COMPATIBILIDADE
    public Element CurrentElement
    {
        get => elementSystem.currentElement;
        set => elementSystem.currentElement = value;
    }

    public float ElementalBonus => elementSystem.elementalBonus;

    private List<string> inventory = new List<string>();
    private Rigidbody2D rb;

    // Timers para ativação automática
    private float attackTimer = 0f;
    private float defenseTimer = 0f;
    private float currentDefenseBonus = 0f;

    // 🆕 Cooldowns individuais para skills
    private Dictionary<string, float> skillCooldowns = new Dictionary<string, float>();

    private UIManager uiManager;
    private SkillManager skillManager;

    // 🆕 ENUM DE ELEMENTOS
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

    // 🆕 SISTEMA DE ELEMENTOS (MELHORADO)
    [System.Serializable]
    public class ElementSystem
    {
        public Element currentElement = Element.None;
        public float elementalBonus = 1.2f;
        public Dictionary<Element, ElementAffinity> elementAffinities = new Dictionary<Element, ElementAffinity>();

        [Header("Elemental Effects")]
        public float burnDamagePerSecond = 5f;
        public float freezeSlowAmount = 0.5f;
        public float shockStunChance = 0.3f;
        public float poisonDuration = 3f;

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
                weakAgainst = new List<Element> { Element.Fire, Element.Ice }
            };

            elementAffinities[Element.Earth] = new ElementAffinity
            {
                strongAgainst = new List<Element> { Element.Lightning, Element.Fire },
                weakAgainst = new List<Element> { Element.Ice, Element.Poison }
            };

            elementAffinities[Element.Wind] = new ElementAffinity
            {
                strongAgainst = new List<Element> { Element.Poison, Element.Earth },
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
                    return elementalBonus; // 1.2x damage

                if (affinity.weakAgainst.Contains(targetElement))
                    return 1f / elementalBonus; // ~0.83x damage
            }

            return 1f; // Neutral damage
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
            }
        }

        private void ApplyBurnEffect(GameObject target)
        {
            Debug.Log($"🔥 Aplicando efeito de queimadura em {target.name}");
            // Implementar lógica de queimadura
        }

        private void ApplyFreezeEffect(GameObject target)
        {
            Debug.Log($"❄️ Aplicando efeito de congelamento em {target.name}");
            // Implementar lógica de congelamento
        }

        private void ApplyShockEffect(GameObject target)
        {
            Debug.Log($"⚡ Aplicando efeito de choque em {target.name}");
            // Implementar lógica de choque
        }

        private void ApplyPoisonEffect(GameObject target)
        {
            Debug.Log($"☠️ Aplicando efeito de veneno em {target.name}");
            // Implementar lógica de veneno
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
        public float baseDamage;
        public bool isActive;
        public float areaOfEffect;
        public float duration;
        public Element element = Element.None;
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

    // 🆕 INICIALIZAÇÃO MELHORADA
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        uiManager = UIManager.Instance;
        skillManager = SkillManager.Instance;

        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(0.1f);

        // 🆕 INICIALIZAÇÃO MAIS ROBUSTA
        yield return StartCoroutine(DelayedCharacterInitialization());

        UpdateUI();
        Debug.Log("✅ PlayerStats inicializado completamente!");
    }

    // 🆕 MÉTODO DE INICIALIZAÇÃO DO CHARACTER SELECTION (OTIMIZADO)
    public void InitializeFromCharacterSelection()
    {
        StartCoroutine(DelayedCharacterInitialization());
    }

    private IEnumerator DelayedCharacterInitialization()
    {
        yield return null;

        Debug.Log("🔍 Procurando CharacterSelectionManager...");
        CharacterSelectionManagerIntegrated selectionManager = FindAnyObjectByType<CharacterSelectionManagerIntegrated>();

        if (selectionManager != null && SkillManager.Instance != null)
        {
            Debug.Log("✅ CharacterSelectionManager encontrado!");
            yield return null;
            selectionManager.ApplyCharacterToPlayerSystems(this, SkillManager.Instance);
            Debug.Log("✅ Personagem selecionado aplicado ao PlayerStats!");
        }
        else
        {
            Debug.LogWarning("⚠️ CharacterSelectionManager não encontrado! Usando configurações padrão.");
            InitializeDefaultSkills();
        }

        UpdateUI();
    }

    void InitializeSkills()
    {
        // Skills de Ataque
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

        // Skills de Defesa
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

        // Ultimate Skill
        ultimateSkill = new UltimateSkill
        {
            skillName = "Fúria do Herói",
            baseDamage = 50f,
            isActive = false,
            areaOfEffect = 5f,
            duration = 3f,
            element = Element.None
        };
    }

    public void InitializeDefaultSkills()
    {
        Debug.Log("🔄 Inicializando skills padrão...");
        InitializeSkills();
    }

    void Update()
    {
        // Movimento
        HandleMovement();

        // 🆕 Sistema de regeneração
        HandleHealthRegeneration();

        // 🆕 Atualizar cooldowns das skills
        UpdateSkillCooldowns();

        // Ativação automática das skills
        HandlePassiveSkills();

        // Atualizar sistema de Ultimate
        UpdateUltimateSystem();

        // Input para Ultimate
        if (Input.GetKeyDown(KeyCode.R) && ultimateReady && ultimateSkill.isActive)
        {
            ActivateUltimate();
        }

        // Input para toggle de skills
        HandleSkillToggleInput();

        // 🆕 INPUT PARA TROCAR ELEMENTO
        HandleElementInput();

        // 🆕 INPUT PARA TESTAR SKILL MANAGER
        HandleSkillManagerInput();
    }

    // 🆕 SISTEMA DE REGENERAÇÃO DE VIDA
    void HandleHealthRegeneration()
    {
        timeSinceLastDamage += Time.deltaTime;

        // Só começa a regenerar depois de um tempo sem levar dano
        if (timeSinceLastDamage >= healthRegenDelay && health < maxHealth)
        {
            if (!isRegenerating)
            {
                isRegenerating = true;
                Debug.Log("💚 Regeneração de vida iniciada");
            }

            float regenAmount = healthRegenRate * Time.deltaTime;
            health = Mathf.Min(maxHealth, health + regenAmount);

            // Atualiza UI a cada 0.5 segundos durante regeneração
            if (Time.frameCount % 30 == 0) // Aproximadamente 0.5 segundos
            {
                UpdateUI();
            }
        }
        else if (isRegenerating && health >= maxHealth)
        {
            isRegenerating = false;
            Debug.Log("💚 Vida totalmente regenerada");
        }
    }

    // 🆕 ATUALIZAR COOLDOWNS DAS SKILLS
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

    // 🆕 MÉTODO PARA MUDAR ELEMENTO (OTIMIZADO)
    public void ChangeElement(Element newElement)
    {
        Element previousElement = CurrentElement;

        // Remove bônus do elemento anterior
        RemoveElementBonus(previousElement);

        // Aplica novo elemento
        CurrentElement = newElement;
        ApplyElementBonus(newElement);

        Debug.Log($"⚡ Elemento alterado: {previousElement} → {newElement}");

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
                Debug.Log("🔥 Bônus: +5 de Ataque");
                break;
            case Element.Ice:
                defense += 3f;
                Debug.Log("❄️ Bônus: +3 de Defesa");
                break;
            case Element.Lightning:
                attackActivationInterval *= 0.8f;
                Debug.Log("⚡ Bônus: +20% Velocidade de Ataque");
                break;
            case Element.Poison:
                Debug.Log("☠️ Bônus: Dano Contínuo Aplicado");
                break;
            case Element.Earth:
                defense += 5f;
                Debug.Log("🌍 Bônus: +5 de Defesa");
                break;
            case Element.Wind:
                speed += 2f;
                Debug.Log("💨 Bônus: +2 de Velocidade");
                break;
        }
    }

    // 🆕 MÉTODOS DE INPUT OTIMIZADOS
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

        if (Input.GetKeyDown(KeyCode.F7)) skillManager.AddRandomSkill();
        if (Input.GetKeyDown(KeyCode.F8)) skillManager.AddRandomModifier();
        if (Input.GetKeyDown(KeyCode.F9)) skillManager.AddTestSkills();
        if (Input.GetKeyDown(KeyCode.F10)) skillManager.CheckIntegrationStatus();
    }

    // ✅ MÉTODOS EXISTENTES (MANTIDOS E OTIMIZADOS)
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

                Debug.Log($"⚔️ {skill.skillName} ativada! Dano: {finalDamage} | Elemento: {attackElement}");
                ApplyAreaDamage(finalDamage, attackElement);

                // 🆕 Iniciar cooldown
                skill.StartCooldown();

                GainXP(2);
            }
        }
        UpdateUI();
    }

    void ActivatePassiveDefenseSkills()
    {
        float totalDefenseBonus = 0f;

        foreach (var skill in defenseSkills)
        {
            if (skill.isActive && !skill.IsOnCooldown)
            {
                float skillDefense = skill.CalculateTotalDefense();
                totalDefenseBonus += skillDefense;
                Debug.Log($"🛡️ {skill.skillName} ativada! Defesa: {skillDefense} | Elemento: {skill.element}");

                // 🆕 Iniciar cooldown
                skill.StartCooldown();

                GainXP(1);
            }
        }

        if (totalDefenseBonus > 0)
        {
            if (currentDefenseBonus > 0) defense -= currentDefenseBonus;
            defense += totalDefenseBonus;
            currentDefenseBonus = totalDefenseBonus;
            StartCoroutine(RemoveDefenseBonusAfterTime());
        }

        UpdateUI();
    }

    private IEnumerator RemoveDefenseBonusAfterTime()
    {
        yield return new WaitForSeconds(defenseActivationInterval);
        defense -= currentDefenseBonus;
        currentDefenseBonus = 0f;
        UpdateUI();
    }

    float CalculateElementalDamage(float baseDamage, Element attackElement, Element targetElement)
    {
        if (attackElement == Element.None || targetElement == Element.None)
            return baseDamage;

        float multiplier = elementSystem.CalculateElementalMultiplier(attackElement, targetElement);
        float finalDamage = baseDamage * multiplier;

        if (multiplier > 1f)
            Debug.Log($"🎯 VANTAGEM ELEMENTAL! Dano: {baseDamage} → {finalDamage} (x{multiplier})");
        else if (multiplier < 1f)
            Debug.Log($"⚠️ DESVANTAGEM ELEMENTAL! Dano: {baseDamage} → {finalDamage} (x{multiplier})");

        return finalDamage;
    }

    void ApplyAreaDamage(float damage, Element element)
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, 3f);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject == gameObject) continue;
            if (hitCollider.CompareTag("Enemy"))
            {
                elementSystem.ApplyElementalEffect(element, hitCollider.gameObject);
                Debug.Log($"💥 Dano {damage} aplicado no inimigo {hitCollider.name} | Elemento: {element}");
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

    // ✅ MÉTODOS RESTANTES MANTIDOS...
    // (GainXP, LevelUp, ActivateUltimate, TakeDamage, Heal, etc.)

    public void GainXP(float xpAmount)
    {
        currentXP += xpAmount;
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

        maxHealth += 10f;
        health = maxHealth;
        attack += 2f;
        defense += 1f;
        speed += 0.5f;

        // 🆕 Melhora regeneração a cada level
        healthRegenRate += 0.2f;

        if (level == 5 && !ultimateSkill.isActive)
        {
            LearnUltimate();
        }

        Debug.Log($"🎉 LEVEL UP! Agora é nível {level}!");

        if (skillManager != null)
        {
            skillManager.OnPlayerLevelUp(level);
        }

        if (uiManager != null)
            uiManager.ShowSkillAcquired($"Level {level}", "Novas habilidades disponíveis!");
    }

    private void LearnUltimate()
    {
        ultimateSkill.isActive = true;
        Debug.Log($"⭐ ULTIMATE APRENDIDA: {ultimateSkill.skillName}!");

        if (uiManager != null)
            uiManager.ShowUltimateAcquired(ultimateSkill.skillName, "Pressione R para ativar!");
    }

    private float CalculateXPForNextLevel()
    {
        return 100f * Mathf.Pow(xpMultiplier, level - 1);
    }

    public void TakeDamage(float damage)
    {
        float reducedDamage = Mathf.Max(0, damage - defense * 0.5f);
        health -= reducedDamage;

        // 🆕 Reinicia timer de regeneração quando leva dano
        timeSinceLastDamage = 0f;
        isRegenerating = false;

        if (health > 0)
        {
            GainXP(2);
            Debug.Log($"💢 Dano recebido: {reducedDamage} (original: {damage})");
        }

        UpdateUI();

        if (health <= 0)
        {
            Die();
        }
    }

    public void Heal(float healAmount)
    {
        health = Mathf.Min(maxHealth, health + healAmount);
        UpdateUI();
        Debug.Log($"💚 Curado em {healAmount}!");
    }

    public void ActivateUltimate()
    {
        if (!ultimateReady || !ultimateSkill.isActive) return;

        float totalDamage = ultimateSkill.CalculateTotalDamage();
        Element ultimateElement = ultimateSkill.GetEffectiveElement();
        float finalDamage = CalculateElementalDamage(totalDamage, ultimateElement, Element.None);

        Debug.Log($"🚀 ULTIMATE ATIVADA: {ultimateSkill.skillName}! Dano: {finalDamage} | Elemento: {ultimateElement}");

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, ultimateSkill.areaOfEffect);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject == gameObject) continue;
            if (hitCollider.CompareTag("Enemy"))
            {
                elementSystem.ApplyElementalEffect(ultimateElement, hitCollider.gameObject);
                Debug.Log($"💥 ULTIMATE: Dano {finalDamage} no inimigo {hitCollider.name} | Elemento: {ultimateElement}");
            }
        }

        StartCoroutine(UltimateEffects(ultimateSkill.duration, ultimateElement));
        ultimateReady = false;
        ultimateChargeTime = 0f;

        if (uiManager != null)
        {
            uiManager.OnUltimateActivated();
            uiManager.ShowUltimateAcquired(ultimateSkill.skillName, "Ultimate ativada!");
        }

        GainXP(15);
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

        Debug.Log($"💥 Efeito da Ultimate ativo! Elemento: {element}");
        yield return new WaitForSeconds(duration);

        attack = originalAttack;
        speed = originalSpeed;
        Debug.Log("🔚 Efeito da Ultimate terminou.");
    }

    void UpdateUltimateSystem()
    {
        if (!ultimateSkill.isActive) return;

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

    // 🆕 MÉTODO PARA FORÇAR ATUALIZAÇÃO DA UI
    public void ForceUIUpdate()
    {
        UpdateUI();
    }

    void UpdateUI()
    {
        if (uiManager != null)
        {
            uiManager.UpdatePlayerStatus();
        }
    }

    private void Die()
    {
        Debug.Log("💀 Jogador morreu!");
        Time.timeScale = 0f;
    }

    // 🆕 GETTERS E SETTERS COMPLETOS
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
    public Element GetCurrentElement() => CurrentElement;
    public float GetElementalBonus() => ElementalBonus;
    public ElementSystem GetElementSystem() => elementSystem;
    public float GetHealthRegenRate() => healthRegenRate;
    public bool IsRegeneratingHealth() => isRegenerating;

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
            UpdateUI();
        }
    }

    public void ApplyItemEffect(string itemName, string statType, float boostValue)
    {
        switch (statType.ToLower())
        {
            case "health":
                maxHealth += boostValue;
                health += boostValue;
                Debug.Log($"❤️ Vida aumentada em {boostValue}!");
                break;
            case "attack":
                attack += boostValue;
                Debug.Log($"⚔️ Ataque aumentado em {boostValue}!");
                break;
            case "defense":
                defense += boostValue;
                Debug.Log($"🛡️ Defesa aumentada em {boostValue}!");
                break;
            case "speed":
                speed += boostValue;
                Debug.Log($"🏃 Velocidade aumentada em {boostValue}!");
                break;
            case "regen":
                healthRegenRate += boostValue;
                Debug.Log($"💚 Regeneração aumentada em {boostValue}!");
                break;
        }

        GainXP(10);
        inventory.Add(itemName);
        UpdateUI();
    }

    public void AddSkillModifier(SkillModifier modifier)
    {
        bool applied = false;

        foreach (var skill in attackSkills)
        {
            if (skill.skillName == modifier.targetSkillName)
            {
                skill.modifiers.Add(modifier);

                // 🆕 Aplicar redução de cooldown se existir
                if (modifier.cooldownReduction > 0f)
                {
                    skill.cooldown = Mathf.Max(0.1f, skill.cooldown * (1f - modifier.cooldownReduction));
                }

                Debug.Log($"✨ Modificador {modifier.modifierName} aplicado em {skill.skillName}");
                applied = true;
                if (uiManager != null)
                    uiManager.ShowModifierAcquired(modifier.modifierName, skill.skillName);
            }
        }

        foreach (var skill in defenseSkills)
        {
            if (skill.skillName == modifier.targetSkillName)
            {
                skill.modifiers.Add(modifier);

                // 🆕 Aplicar redução de cooldown se existir
                if (modifier.cooldownReduction > 0f)
                {
                    skill.cooldown = Mathf.Max(0.1f, skill.cooldown * (1f - modifier.cooldownReduction));
                }

                Debug.Log($"✨ Modificador {modifier.modifierName} aplicado em {skill.skillName}");
                applied = true;
                if (uiManager != null)
                    uiManager.ShowModifierAcquired(modifier.modifierName, skill.skillName);
            }
        }

        if (ultimateSkill.skillName == modifier.targetSkillName)
        {
            ultimateSkill.modifiers.Add(modifier);
            Debug.Log($"✨ Modificador {modifier.modifierName} aplicado na ULTIMATE {ultimateSkill.skillName}");
            applied = true;
            if (uiManager != null)
                uiManager.ShowModifierAcquired(modifier.modifierName, ultimateSkill.skillName);
        }

        if (!applied)
        {
            Debug.LogWarning($"⚠️ Nenhuma skill encontrada com o nome: {modifier.targetSkillName}");
        }

        UpdateUI();
    }

    // 🆕 MÉTODOS PARA VERIFICAR COOLDOWNS
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
}