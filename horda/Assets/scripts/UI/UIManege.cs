using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("🔔 Popup de Skill")]
    public GameObject skillAcquiredPanel;
    public Text skillNameText;
    public Text skillDescriptionText;

    [Header("🎯 HUD de Skills")]
    public Image attackSkill1Icon;
    public Image attackSkill2Icon;
    public Image defenseSkill1Icon;
    public Image defenseSkill2Icon;
    public Image ultimateSkillIcon;

    public Text attackCooldownText1;
    public Text attackCooldownText2;
    public Text defenseCooldownText1;
    public Text defenseCooldownText2;
    public Text ultimateCooldownText;
    public Slider ultimateChargeBar;

    [Header("💚 Vida")]
    public Slider healthBar;
    public Text healthText;

    [Header("⭐ Level e XP")]
    public Text levelText;
    public Text xpText;
    public Slider xpSlider;

    [Header("🚀 Ultimate")]
    public Text ultimateChargeText;
    public GameObject ultimateReadyEffect;

    [Header("⚡ Elemento Atual (FUTURO)")]
    public Text currentElementText;
    public Image elementIcon;

    [Header("📊 Status Detalhados")]
    public GameObject statusPanel;
    public Text damageText;
    public Text speedText;
    public Text defenseText;
    public Text attackSpeedText;
    public Text inventoryText;
    public Text attackSkillsText;
    public Text defenseSkillsText;
    public Text ultimateSkillsText;
    public Text elementInfoText;

    [Header("🎯 Skill Manager UI")]
    public GameObject skillSelectionPanel;
    public Transform skillButtonContainer;
    public GameObject skillButtonPrefab;
    public Text availableSkillsText;

    [Header("Configurações")]
    public KeyCode toggleStatusKey = KeyCode.Tab;
    public KeyCode toggleSkillsKey = KeyCode.K;
    public float popupDisplayTime = 3f;

    private PlayerStats playerStats;
    private SkillManager skillManager;
    private float[] attackTimers = new float[2];
    private float[] defenseTimers = new float[2];
    private float ultimateTimer = 0f;
    private bool ultimateReady = false;

    private Dictionary<PlayerStats.Element, Color> elementColors = new Dictionary<PlayerStats.Element, Color>()
    {
        { PlayerStats.Element.None, Color.white },
        { PlayerStats.Element.Fire, new Color(1f, 0.3f, 0.1f) },
        { PlayerStats.Element.Ice, new Color(0.1f, 0.5f, 1f) },
        { PlayerStats.Element.Lightning, new Color(0.8f, 0.8f, 0.1f) },
        { PlayerStats.Element.Poison, new Color(0.5f, 0.1f, 0.8f) },
        { PlayerStats.Element.Earth, new Color(0.6f, 0.4f, 0.2f) },
        { PlayerStats.Element.Wind, new Color(0.4f, 0.8f, 0.9f) }
    };

    private void Awake()
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

    private void Start()
    {
        if (skillAcquiredPanel != null)
            skillAcquiredPanel.SetActive(false);

        if (statusPanel != null)
            statusPanel.SetActive(false);

        if (ultimateReadyEffect != null)
            ultimateReadyEffect.SetActive(false);

        if (skillSelectionPanel != null)
            skillSelectionPanel.SetActive(false);

        playerStats = FindAnyObjectByType<PlayerStats>();
        skillManager = SkillManager.Instance;

        for (int i = 0; i < 2; i++)
        {
            attackTimers[i] = 0f;
            defenseTimers[i] = 0f;
        }

        FindNullReference();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleStatusKey))
        {
            ToggleStatusPanel();
        }

        if (Input.GetKeyDown(toggleSkillsKey) && skillSelectionPanel != null)
        {
            ToggleSkillSelection();
        }

        if (playerStats != null)
        {
            UpdateCooldowns();
            UpdatePlayerStatus();
            UpdateSkillIcons();
            UpdateUltimateSystem();
            UpdateElementUI();
        }
    }

    private void FindNullReference()
    {
        Debug.Log("=== VERIFICAÇÃO DE COMPONENTES UI ===");

        if (attackSkill1Icon == null) Debug.LogError("attackSkill1Icon é NULL");
        if (attackSkill2Icon == null) Debug.LogError("attackSkill2Icon é NULL");
        if (defenseSkill1Icon == null) Debug.LogError("defenseSkill1Icon é NULL");
        if (defenseSkill2Icon == null) Debug.LogError("defenseSkill2Icon é NULL");
        if (ultimateSkillIcon == null) Debug.LogError("ultimateSkillIcon é NULL");

        if (attackCooldownText1 == null) Debug.LogError("attackCooldownText1 é NULL");
        if (attackCooldownText2 == null) Debug.LogError("attackCooldownText2 é NULL");
        if (defenseCooldownText1 == null) Debug.LogError("defenseCooldownText1 é NULL");
        if (defenseCooldownText2 == null) Debug.LogError("defenseCooldownText2 é NULL");
        if (ultimateCooldownText == null) Debug.LogError("ultimateCooldownText é NULL");

        Debug.Log("=== FIM DA VERIFICAÇÃO ===");
    }

    private void UpdateElementUI()
    {
        if (playerStats == null) return;

        if (currentElementText != null)
        {
            var currentElement = playerStats.GetCurrentElement();
            currentElementText.text = $"Elemento: {currentElement}";

            if (elementColors.ContainsKey(currentElement))
            {
                currentElementText.color = elementColors[currentElement];
            }
        }

        if (elementIcon != null)
        {
            var currentElement = playerStats.GetCurrentElement();
            elementIcon.color = currentElement == PlayerStats.Element.None ?
                new Color(1, 1, 1, 0.3f) : Color.white;
        }
    }

    private void UpdateCooldowns()
    {
        if (playerStats == null) return;

        for (int i = 0; i < 2; i++)
        {
            attackTimers[i] += Time.deltaTime;
            defenseTimers[i] += Time.deltaTime;

            if (attackTimers[i] >= playerStats.GetAttackActivationInterval())
                attackTimers[i] = 0f;

            if (defenseTimers[i] >= playerStats.GetDefenseActivationInterval())
                defenseTimers[i] = 0f;
        }
    }

    // ✅ VERSÃO COMPLETAMENTE PROTEGIDA DO UpdateSkillIcons
    private void UpdateSkillIcons()
    {
        if (playerStats == null)
        {
            return;
        }

        try
        {
            var attackSkills = playerStats.GetAttackSkills();
            var defenseSkills = playerStats.GetDefenseSkills();
            var ultimateSkill = playerStats.GetUltimateSkill();

            if (attackSkills == null || defenseSkills == null || ultimateSkill == null)
            {
                return;
            }

            // ✅ ATUALIZAR ÍCONES DE ATAQUE
            for (int i = 0; i < 2; i++)
            {
                Image attackIcon = i == 0 ? attackSkill1Icon : attackSkill2Icon;
                Text attackCooldown = i == 0 ? attackCooldownText1 : attackCooldownText2;

                if (attackIcon == null) continue;

                if (attackSkills.Count > i)
                {
                    var skill = attackSkills[i];
                    if (skill == null) continue;

                    attackIcon.color = skill.isActive ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);

                    if (attackCooldown != null)
                    {
                        float attackInterval = playerStats.GetAttackActivationInterval();
                        if (attackInterval <= 0) attackInterval = 1f;

                        float cooldownPercent = attackTimers[i] / attackInterval;

                        if (skill.isActive && cooldownPercent < 1f)
                        {
                            attackCooldown.text = $"{(1f - cooldownPercent) * 100f:F0}%";
                            attackCooldown.color = Color.yellow;
                        }
                        else
                        {
                            string elementInfo = "";
                            if (skill.GetEffectiveElement() != PlayerStats.Element.None)
                            {
                                elementInfo = $"[{skill.GetEffectiveElement()}]";
                            }
                            attackCooldown.text = skill.isActive ? $"PRONTO {elementInfo}" : "INATIVA";
                            attackCooldown.color = skill.isActive ? Color.green : Color.gray;
                        }
                    }
                }
                else
                {
                    attackIcon.color = new Color(0.3f, 0.3f, 0.3f, 0.3f);
                    if (attackCooldown != null)
                        attackCooldown.text = "VAZIO";
                }
            }

            // ✅ ATUALIZAR ÍCONES DE DEFESA
            for (int i = 0; i < 2; i++)
            {
                Image defenseIcon = i == 0 ? defenseSkill1Icon : defenseSkill2Icon;
                Text defenseCooldown = i == 0 ? defenseCooldownText1 : defenseCooldownText2;

                if (defenseIcon == null) continue;

                if (defenseSkills.Count > i)
                {
                    var skill = defenseSkills[i];
                    if (skill == null) continue;

                    defenseIcon.color = skill.isActive ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);

                    if (defenseCooldown != null)
                    {
                        float defenseInterval = playerStats.GetDefenseActivationInterval();
                        if (defenseInterval <= 0) defenseInterval = 1f;

                        float cooldownPercent = defenseTimers[i] / defenseInterval;

                        if (skill.isActive && cooldownPercent < 1f)
                        {
                            defenseCooldown.text = $"{(1f - cooldownPercent) * 100f:F0}%";
                            defenseCooldown.color = Color.yellow;
                        }
                        else
                        {
                            string elementInfo = "";
                            if (skill.element != PlayerStats.Element.None)
                            {
                                elementInfo = $"[{skill.element}]";
                            }
                            defenseCooldown.text = skill.isActive ? $"PRONTO {elementInfo}" : "INATIVA";
                            defenseCooldown.color = skill.isActive ? Color.green : Color.gray;
                        }
                    }
                }
                else
                {
                    defenseIcon.color = new Color(0.3f, 0.3f, 0.3f, 0.3f);
                    if (defenseCooldown != null)
                        defenseCooldown.text = "VAZIO";
                }
            }

            // ✅ ATUALIZAR ÍCONE DA ULTIMATE
            if (ultimateSkillIcon != null)
            {
                if (ultimateSkill.isActive)
                {
                    ultimateSkillIcon.color = ultimateReady ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.7f);

                    if (ultimateCooldownText != null)
                    {
                        string elementInfo = "";
                        if (ultimateSkill.GetEffectiveElement() != PlayerStats.Element.None)
                        {
                            elementInfo = $"[{ultimateSkill.GetEffectiveElement()}]";
                        }
                        ultimateCooldownText.text = ultimateReady ? $"PRONTO! {elementInfo}" : "CARREGANDO";
                        ultimateCooldownText.color = ultimateReady ? Color.yellow : Color.white;
                    }
                }
                else
                {
                    ultimateSkillIcon.color = new Color(0.3f, 0.3f, 0.3f, 0.3f);
                    if (ultimateCooldownText != null)
                        ultimateCooldownText.text = "BLOQUEADA";
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erro em UpdateSkillIcons: {e.Message}");
        }
    }

    private void UpdateUltimateSystem()
    {
        if (playerStats == null) return;

        ultimateTimer += Time.deltaTime;
        ultimateReady = ultimateTimer >= playerStats.GetUltimateCooldown();

        if (ultimateChargeBar != null)
        {
            float chargePercent = Mathf.Clamp01(ultimateTimer / playerStats.GetUltimateCooldown());
            ultimateChargeBar.value = chargePercent;
        }

        if (ultimateReadyEffect != null)
        {
            ultimateReadyEffect.SetActive(ultimateReady);
        }
    }

    public void UpdatePlayerStatus()
    {
        if (playerStats == null) return;

        if (healthBar != null)
        {
            healthBar.maxValue = playerStats.GetMaxHealth();
            healthBar.value = playerStats.GetCurrentHealth();
        }

        if (healthText != null)
        {
            healthText.text = $"{playerStats.GetCurrentHealth():F0}/{playerStats.GetMaxHealth():F0}";
        }

        if (levelText != null)
            levelText.text = $"Level: {playerStats.GetLevel()}";

        if (xpText != null)
            xpText.text = $"XP: {playerStats.GetCurrentXP():F0}/{playerStats.GetXPToNextLevel():F0}";

        if (xpSlider != null)
        {
            xpSlider.maxValue = playerStats.GetXPToNextLevel();
            xpSlider.value = playerStats.GetCurrentXP();
        }

        if (statusPanel != null && statusPanel.activeSelf)
        {
            if (damageText != null)
                damageText.text = $"Ataque: {playerStats.GetAttack():F1}";

            if (speedText != null)
                speedText.text = $"Velocidade: {playerStats.GetSpeed():F1}";

            if (defenseText != null)
                defenseText.text = $"Defesa: {playerStats.GetDefense():F0}";

            if (attackSpeedText != null)
                attackSpeedText.text = $"Vel. Ataque: {playerStats.GetAttackActivationInterval():F1}s";

            if (elementInfoText != null)
            {
                var currentElement = playerStats.GetCurrentElement();
                elementInfoText.text = $"Elemento Atual: {currentElement}\n" +
                                      $"Bônus Elemental: {playerStats.GetElementalBonus():F1}x";

                if (elementColors.ContainsKey(currentElement))
                {
                    elementInfoText.color = elementColors[currentElement];
                }
            }

            if (inventoryText != null)
            {
                var inventory = playerStats.GetInventory();
                inventoryText.text = $"Itens: {(inventory.Count > 0 ? string.Join(", ", inventory) : "Nenhum")}";
            }

            if (attackSkillsText != null)
            {
                string attackInfo = "Skills de Ataque:\n";
                var attackSkills = playerStats.GetAttackSkills();
                foreach (var skill in attackSkills)
                {
                    string status = skill.isActive ? "✅" : "❌";
                    string elementInfo = skill.GetEffectiveElement() != PlayerStats.Element.None ?
                        $"[{skill.GetEffectiveElement()}]" : "";
                    attackInfo += $"{status} {skill.skillName} {elementInfo}: {skill.CalculateTotalDamage()} dmg\n";
                }
                attackSkillsText.text = attackInfo;
            }

            if (defenseSkillsText != null)
            {
                string defenseInfo = "Skills de Defesa:\n";
                var defenseSkills = playerStats.GetDefenseSkills();
                foreach (var skill in defenseSkills)
                {
                    string status = skill.isActive ? "✅" : "❌";
                    string elementInfo = skill.element != PlayerStats.Element.None ?
                        $"[{skill.element}]" : "";
                    defenseInfo += $"{status} {skill.skillName} {elementInfo}: {skill.CalculateTotalDefense()} def\n";
                }
                defenseSkillsText.text = defenseInfo;
            }

            if (ultimateSkillsText != null)
            {
                var ultimate = playerStats.GetUltimateSkill();
                string ultimateInfo = "Ultimate:\n";
                if (ultimate.isActive)
                {
                    string status = ultimateReady ? "⭐ PRONTA ⭐" : "⏳ CARREGANDO";
                    string elementInfo = ultimate.GetEffectiveElement() != PlayerStats.Element.None ?
                        $"[{ultimate.GetEffectiveElement()}]" : "";
                    ultimateInfo += $"{status} {elementInfo}\n";
                    ultimateInfo += $"{ultimate.skillName}: {ultimate.CalculateTotalDamage()} dmg\n";
                    ultimateInfo += $"Área: {ultimate.areaOfEffect}m | Duração: {ultimate.duration}s";
                }
                else
                {
                    ultimateInfo += "🔒 Disponível no Level 5";
                }
                ultimateSkillsText.text = ultimateInfo;
            }
        }
    }

    public void ShowSkillAcquired(string skillName, string description)
    {
        if (skillAcquiredPanel == null) return;

        skillNameText.text = skillName;
        skillDescriptionText.text = description;
        skillAcquiredPanel.SetActive(true);
        StartCoroutine(HideSkillPopup());
    }

    public void ShowUltimateAcquired(string ultimateName, string description)
    {
        ShowSkillAcquired($"⭐ {ultimateName} ⭐", description);
        StartCoroutine(UltimateAcquiredEffect());
    }

    public void ShowModifierAcquired(string modifierName, string targetSkill)
    {
        ShowSkillAcquired(modifierName, $"Aplicado em: {targetSkill}");
    }

    public void ShowElementChanged(string elementName)
    {
        ShowSkillAcquired("⚡ Elemento Alterado", $"Elemento atual: {elementName}");
    }

    public void OnUltimateActivated()
    {
        ultimateTimer = 0f;
        ultimateReady = false;
    }

    public void OnAttackSkillActivated(int skillIndex)
    {
        if (skillIndex >= 0 && skillIndex < 2)
        {
            attackTimers[skillIndex] = 0f;
        }
    }

    public void OnDefenseSkillActivated(int skillIndex)
    {
        if (skillIndex >= 0 && skillIndex < 2)
        {
            defenseTimers[skillIndex] = 0f;
        }
    }

    public void SetUltimateReady(bool ready)
    {
        ultimateReady = ready;
        if (ready && playerStats != null)
            ultimateTimer = playerStats.GetUltimateCooldown();
    }

    public void ToggleSkillSelection()
    {
        if (skillSelectionPanel == null) return;

        bool newState = !skillSelectionPanel.activeSelf;
        skillSelectionPanel.SetActive(newState);

        if (newState)
        {
            RefreshSkillSelectionUI();
        }
    }

    private void RefreshSkillSelectionUI()
    {
        if (skillManager == null || skillButtonContainer == null) return;

        foreach (Transform child in skillButtonContainer)
        {
            Destroy(child.gameObject);
        }

        var availableSkills = skillManager.GetAvailableSkills();
        var activeSkills = skillManager.GetActiveSkills();

        if (availableSkillsText != null)
        {
            availableSkillsText.text = $"Skills Disponíveis: {availableSkills.Count}\nSkills Ativas: {activeSkills.Count}";
        }

        foreach (var skill in availableSkills)
        {
            if (skillButtonPrefab == null) continue;

            GameObject buttonObj = Instantiate(skillButtonPrefab, skillButtonContainer);
            Button button = buttonObj.GetComponent<Button>();
            Text buttonText = buttonObj.GetComponentInChildren<Text>();

            if (buttonText != null)
            {
                buttonText.text = $"{skill.skillName}\n{skill.description}";
            }

            bool hasSkill = skillManager.HasSkill(skill);
            button.image.color = hasSkill ? Color.gray : Color.white;

            if (button != null)
            {
                SkillData currentSkill = skill;
                button.onClick.AddListener(() => OnSkillSelected(currentSkill));
                button.interactable = !hasSkill;
            }
        }
    }

    private void OnSkillSelected(SkillData skill)
    {
        if (skillManager != null)
        {
            skillManager.AddSkill(skill);
            RefreshSkillSelectionUI();
        }
    }

    private void ToggleStatusPanel()
    {
        if (statusPanel != null)
        {
            bool newState = !statusPanel.activeSelf;
            statusPanel.SetActive(newState);
            if (newState) UpdatePlayerStatus();
        }
    }

    private IEnumerator HideSkillPopup()
    {
        yield return new WaitForSeconds(popupDisplayTime);
        if (skillAcquiredPanel != null)
            skillAcquiredPanel.SetActive(false);
    }

    private IEnumerator UltimateAcquiredEffect()
    {
        Debug.Log("🎆 ULTIMATE ADQUIRIDA!");
        yield return null;
    }
}