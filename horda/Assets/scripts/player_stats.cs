using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static PlayerStats;

public class PlayerStats : MonoBehaviour
{
    [Header("Status do Jogador")]
    public float health = 100f;
    public float maxHealth = 100f;
    public float attack = 10f;
    public float defense = 5f;
    public float speed = 8f;

    [Header("Sistema de Regeneração")]
    public float healthRegenRate = 1f;
    public float healthRegenDelay = 5f;
    private float timeSinceLastDamage = 0f;
    private bool isRegenerating = false;

    [Header("Sistema de Level")]
    public int level = 1;
    public float currentXP = 0f;
    public float xpToNextLevel = 100f;
    public float xpMultiplier = 1.5f;

    [Header("📦 Sistema de Coleta de XP")]
    public float xpCollectionRadius = 3f;
    public bool autoCollectXP = true;

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

    [Header("Configurações de Ativação")]
    public float attackActivationInterval = 2f;
    public float defenseActivationInterval = 3f;

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
    private float currentDefenseBonus = 0f;

    private UIManager uiManager;
    private SkillManager skillManager;
    private StatusCardSystem cardSystem;

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
            Debug.Log($"🔥 Aplicando efeito de queimadura em {target.name}");
        }

        private void ApplyFreezeEffect(GameObject target)
        {
            Debug.Log($"❄️ Aplicando efeito de congelamento em {target.name}");
        }

        private void ApplyShockEffect(GameObject target)
        {
            Debug.Log($"⚡ Aplicando efeito de choque em {target.name}");
        }

        private void ApplyPoisonEffect(GameObject target)
        {
            Debug.Log($"☠️ Aplicando efeito de veneno em {target.name}");
        }

        private void ApplySlowEffect(GameObject target)
        {
            Debug.Log($"🌍 Aplicando efeito de lentidão em {target.name}");
        }

        private void ApplyKnockbackEffect(GameObject target)
        {
            Debug.Log($"💨 Aplicando efeito de repulsão em {target.name}");
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

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        uiManager = UIManager.Instance;
        skillManager = SkillManager.Instance;
        cardSystem = StatusCardSystem.Instance;

        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(DelayedCharacterInitialization());
        UpdateUI();
        Debug.Log("✅ PlayerStats inicializado completamente!");
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

    public void InitializeDefaultSkills()
    {
        Debug.Log("🔄 Inicializando skills padrão...");
        InitializeSkills();

        // 🚫 LIMPAR skills adquiridas no início
        acquiredSkills.Clear();

        // ✅ MAS GARANTIR que a ULTIMATE esteja configurada e ATIVA
        if (ultimateSkill != null)
        {
            ultimateSkill.isActive = true;
            ultimateReady = false;
            ultimateChargeTime = 0f;
            Debug.Log("⭐ Ultimate configurada - player começa com ultimate!");
        }

        Debug.Log("🧹 Skills adquiridas limpas, mas ultimate mantida");
    }

    void Update()
    {
        HandleMovement();
        HandleHealthRegeneration();
        UpdateSkillCooldowns();
        HandlePassiveSkills();
        UpdateUltimateSystem();

        if (Input.GetKeyDown(KeyCode.R) && ultimateReady && ultimateSkill.isActive)
        {
            ActivateUltimate();
        }

        HandleSkillToggleInput();
        HandleElementInput();
        HandleSkillManagerInput();
    }

    void HandleHealthRegeneration()
    {
        timeSinceLastDamage += Time.deltaTime;

        if (timeSinceLastDamage >= healthRegenDelay && health < maxHealth)
        {
            if (!isRegenerating)
            {
                isRegenerating = true;
                Debug.Log("💚 Regeneração de vida iniciada");
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
            Debug.Log("💚 Vida totalmente regenerada");
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

                // APENAS ATIVA AS SKILLS MAS NÃO APLICA DANO EM ÁREA AUTOMÁTICO
                // O dano será aplicado pelos próprios comportamentos das skills (projéteis, etc.)

                skill.StartCooldown();
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

                skill.StartCooldown();
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

    // MÉTODO PARA APLICAR DANO (chamado por outras habilidades)
    public void ApplyDamageToTarget(GameObject target, float damage, Element element = Element.None, bool isCrit = false)
    {
        if (target == null || target == gameObject) return;

        if (target.CompareTag("Enemy"))
        {
            InimigoController inimigo = target.GetComponent<InimigoController>();
            if (inimigo != null)
            {
                // Aplica cálculo de crítico
                if (!isCrit)
                {
                    isCrit = UnityEngine.Random.value < 0.1f; // 10% chance de crítico
                }

                float finalDamage = isCrit ? damage * 2f : damage;
                inimigo.ReceberDano(finalDamage, isCrit);

                // Aplica efeito elemental
                if (element != Element.None)
                {
                    elementSystem.ApplyElementalEffect(element, target);
                }

                Debug.Log($"🎯 Dano aplicado: {finalDamage} no {target.name} | Crítico: {isCrit}");
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

        maxHealth += 10f;
        health = maxHealth;
        attack += 2f;
        defense += 1f;
        speed += 0.5f;

        healthRegenRate += 0.2f;

        Debug.Log($"🎉 LEVEL UP! Agora é nível {level}!");

        if (cardSystem != null)
        {
            cardSystem.OnPlayerLevelUp(level);
        }

        if (skillManager != null)
        {
            skillManager.OnPlayerLevelUp(level);
        }

        if (uiManager != null)
            uiManager.ShowSkillAcquired($"Level {level}", "Novas habilidades disponíveis!");
    }

    private float CalculateXPForNextLevel()
    {
        return 100f * Mathf.Pow(xpMultiplier, level - 1);
    }

    public void TakeDamage(float damage)
    {
        float reducedDamage = Mathf.Max(0, damage - defense * 0.5f);
        health -= reducedDamage;

        timeSinceLastDamage = 0f;
        isRegenerating = false;

        if (health > 0)
        {
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

    private void UpdateUltimateSystem()
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

    public void ActivateUltimate()
    {
        if (!ultimateReady || !ultimateSkill.isActive) return;

        float totalDamage = ultimateSkill.CalculateTotalDamage();
        Element ultimateElement = ultimateSkill.GetEffectiveElement();
        float finalDamage = CalculateElementalDamage(totalDamage, ultimateElement, Element.None);

        Debug.Log($"🚀 ULTIMATE ATIVADA: {ultimateSkill.skillName}! Dano: {finalDamage} | Elemento: {ultimateElement}");

        // APLICA DANO EM ÁREA NA ULTIMATE (isso é permitido pois é uma habilidade especial)
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, ultimateSkill.areaOfEffect);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject == gameObject) continue;
            if (hitCollider.CompareTag("Enemy"))
            {
                InimigoController inimigo = hitCollider.GetComponent<InimigoController>();
                if (inimigo != null)
                {
                    // Chance de crítico na ultimate
                    bool isCrit = UnityEngine.Random.value < 0.3f;
                    float ultimateDamage = isCrit ? finalDamage * 1.5f : finalDamage;
                    inimigo.ReceberDano(ultimateDamage, isCrit);
                }

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

            Debug.Log($"✨ Skill {skill.skillName} aplicada: " +
                     $"ATK+{skill.attackBonus}, DEF+{skill.defenseBonus}, HP+{skill.healthBonus}");

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
        }
    }

    // 🆕 MÉTODO PARA ADICIONAR COMPORTAMENTO DE PROJÉTIL ORBITAL
    private void AddOrbitalProjectileBehavior(SkillData skill)
    {
        var existingBehavior = GetComponent<OrbitingProjectileSkillBehavior>();
        if (existingBehavior != null)
        {
            existingBehavior.UpdateFromSkillData(skill);
            Debug.Log($"⚡ Comportamento orbital melhorado por {skill.skillName}");
            return;
        }

        OrbitingProjectileSkillBehavior orbitalBehavior = gameObject.AddComponent<OrbitingProjectileSkillBehavior>();
        orbitalBehavior.Initialize(this);
        orbitalBehavior.UpdateFromSkillData(skill);
        activeSkillBehaviors.Add(orbitalBehavior);

        Debug.Log($"🌀 Comportamento orbital adicionado: {skill.skillName}");
    }

    // 🆕 MÉTODO PARA ADICIONAR COMPORTAMENTO DE PROJÉTIL NORMAL
    private void AddProjectileBehavior(SkillData skill)
    {
        var existingBehavior = GetComponent<PassiveProjectileSkill2D>();
        if (existingBehavior != null)
        {
            existingBehavior.activationInterval *= 0.8f;
            Debug.Log($"⚡ Comportamento de projétil melhorado por {skill.skillName}");
            return;
        }

        PassiveProjectileSkill2D projectileBehavior = gameObject.AddComponent<PassiveProjectileSkill2D>();
        projectileBehavior.Initialize(this);
        activeSkillBehaviors.Add(projectileBehavior);

        Debug.Log($"✅ Comportamento de projétil adicionado: {skill.skillName}");
    }

    // 🆕 MÉTODO PARA ADICIONAR REGENERAÇÃO DE VIDA
    private void AddHealthRegenBehavior(SkillData skill)
    {
        healthRegenRate += skill.healthRegenBonus;
        healthRegenDelay = Mathf.Max(1f, healthRegenDelay * 0.8f);

        Debug.Log($"💚 Regeneração melhorada: +{skill.healthRegenBonus}/s");
    }

    // 🆕 MÉTODO PARA ADICIONAR GOLPE CRÍTICO
    private void AddCriticalStrikeBehavior(SkillData skill)
    {
        attack += skill.attackBonus * 0.5f;
        Debug.Log($"🎯 Chance de crítico aumentada por {skill.skillName}");
    }

    // 🆕 MÉTODO PARA VERIFICAR SE TEM UMA SKILL
    public bool HasSkill(string skillName)
    {
        return acquiredSkills.Exists(s => s.skillName == skillName);
    }

    public float GetXpCollectionRadius()
    {
        return xpCollectionRadius;
    }

    public void SetXpCollectionRadius(float newRadius)
    {
        xpCollectionRadius = Mathf.Max(0.5f, newRadius);
        Debug.Log($"📦 Raio de coleta ajustado para: {xpCollectionRadius}");
    }

    public void BoostCollectionRadius(float boostAmount, float duration = 0f)
    {
        xpCollectionRadius += boostAmount;
        Debug.Log($"📦 Raio de coleta aumentado para {xpCollectionRadius}");

        if (duration > 0f)
        {
            StartCoroutine(TemporaryRadiusBoost(boostAmount, duration));
        }
    }

    private IEnumerator TemporaryRadiusBoost(float boostAmount, float duration)
    {
        yield return new WaitForSeconds(duration);
        xpCollectionRadius -= boostAmount;
        Debug.Log($"📦 Raio de coleta voltou ao normal: {xpCollectionRadius}");
    }

    public void ForceUIUpdate()
    {
        UpdateUI();
    }

    private void Die()
    {
        Debug.Log("💀 Jogador morreu!");
        Time.timeScale = 0f;
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
    public float GetElementalBonus() => ElementalBonus;
    public float GetHealthRegenRate() => healthRegenRate;
    public bool IsRegeneratingHealth() => isRegenerating;

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
            case "collectionradius":
                BoostCollectionRadius(boostValue);
                Debug.Log($"📦 Raio de coleta aumentado em {boostValue}!");
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
        // Reduz a velocidade temporariamente
        float reducaoAplicada = speed * reducao;
        speed -= reducaoAplicada;

        Debug.Log($"🐌 Slow aplicado ao jogador: -{reducao * 100}% por {duracao}s");

        // Restaura após a duração
        StartCoroutine(RestaurarVelocidade(reducaoAplicada, duracao));
    }

    private IEnumerator RestaurarVelocidade(float reducao, float duracao)
    {
        yield return new WaitForSeconds(duracao);
        speed += reducao;
        Debug.Log($"🏃 Velocidade do jogador restaurada!");
    }
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