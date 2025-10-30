﻿using UnityEngine;
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

    // 🆕 NOVO: Ícones de elemento para cada skill
    [Header("⚡ Ícones de Elemento das Skills")]
    public Image attackSkill1ElementIcon;
    public Image attackSkill2ElementIcon;
    public Image defenseSkill1ElementIcon;
    public Image defenseSkill2ElementIcon;
    public Image ultimateSkillElementIcon;

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

    [Header("⚡ Sistema de Elementos")]
    public Text currentElementText;
    public Image elementIcon;
    public GameObject elementAdvantagePanel;
    public Text advantageText;
    public Text disadvantageText;

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

    [Header("🎨 Sprites dos Elementos")]
    public Sprite fireElementSprite;
    public Sprite iceElementSprite;
    public Sprite lightningElementSprite;
    public Sprite poisonElementSprite;
    public Sprite earthElementSprite;
    public Sprite windElementSprite;
    public Sprite noneElementSprite;

    private PlayerStats playerStats;
    private SkillManager skillManager;
    private float[] attackTimers = new float[2];
    private float[] defenseTimers = new float[2];
    private float ultimateTimer = 0f;
    private bool ultimateReady = false;

    // 🆕 CORES PARA ELEMENTOS DE SKILLS INDIVIDUAIS
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

    // 🆕 CORES DE BACKGROUND POR ELEMENTO
    private Dictionary<PlayerStats.Element, Color> elementBackgroundColors = new Dictionary<PlayerStats.Element, Color>()
    {
        { PlayerStats.Element.None, new Color(0.3f, 0.3f, 0.3f, 0.7f) },
        { PlayerStats.Element.Fire, new Color(1f, 0.2f, 0.1f, 0.3f) },
        { PlayerStats.Element.Ice, new Color(0.1f, 0.4f, 1f, 0.3f) },
        { PlayerStats.Element.Lightning, new Color(0.9f, 0.9f, 0.1f, 0.3f) },
        { PlayerStats.Element.Poison, new Color(0.6f, 0.1f, 0.8f, 0.3f) },
        { PlayerStats.Element.Earth, new Color(0.5f, 0.3f, 0.1f, 0.3f) },
        { PlayerStats.Element.Wind, new Color(0.3f, 0.7f, 0.9f, 0.3f) }
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

        if (elementAdvantagePanel != null)
            elementAdvantagePanel.SetActive(false);

        playerStats = FindAnyObjectByType<PlayerStats>();
        skillManager = SkillManager.Instance;

        for (int i = 0; i < 2; i++)
        {
            attackTimers[i] = 0f;
            defenseTimers[i] = 0f;
        }

        // 🆕 INICIALIZAR ÍCONES DE ELEMENTO
        InitializeElementIcons();
    }

    // 🆕 INICIALIZAR ÍCONES DE ELEMENTO
    private void InitializeElementIcons()
    {
        // Esconde todos os ícones de elemento inicialmente
        SetElementIconVisibility(attackSkill1ElementIcon, false);
        SetElementIconVisibility(attackSkill2ElementIcon, false);
        SetElementIconVisibility(defenseSkill1ElementIcon, false);
        SetElementIconVisibility(defenseSkill2ElementIcon, false);
        SetElementIconVisibility(ultimateSkillElementIcon, false);
    }

    // 🆕 CONFIGURAR VISIBILIDADE DO ÍCONE DE ELEMENTO
    private void SetElementIconVisibility(Image elementIcon, bool visible)
    {
        if (elementIcon != null)
        {
            elementIcon.gameObject.SetActive(visible);
        }
    }

    // 🆕 OBTER SPRITE DO ELEMENTO
    private Sprite GetElementSprite(PlayerStats.Element element)
    {
        switch (element)
        {
            case PlayerStats.Element.Fire: return fireElementSprite;
            case PlayerStats.Element.Ice: return iceElementSprite;
            case PlayerStats.Element.Lightning: return lightningElementSprite;
            case PlayerStats.Element.Poison: return poisonElementSprite;
            case PlayerStats.Element.Earth: return earthElementSprite;
            case PlayerStats.Element.Wind: return windElementSprite;
            default: return noneElementSprite;
        }
    }

    // 🆕 ATUALIZAR ÍCONES DE ELEMENTO DAS SKILLS
    private void UpdateSkillElementIcons()
    {
        if (playerStats == null) return;

        var attackSkills = playerStats.GetAttackSkills();
        var defenseSkills = playerStats.GetDefenseSkills();
        var ultimateSkill = playerStats.GetUltimateSkill();

        // 🆕 ATUALIZAR ÍCONES DE ELEMENTO DAS SKILLS DE ATAQUE
        for (int i = 0; i < 2; i++)
        {
            Image elementIcon = i == 0 ? attackSkill1ElementIcon : attackSkill2ElementIcon;
            Image skillIcon = i == 0 ? attackSkill1Icon : attackSkill2Icon;

            if (elementIcon != null && skillIcon != null)
            {
                if (attackSkills.Count > i && attackSkills[i].isActive)
                {
                    PlayerStats.Element skillElement = attackSkills[i].GetEffectiveElement();
                    if (skillElement != PlayerStats.Element.None)
                    {
                        elementIcon.sprite = GetElementSprite(skillElement);
                        elementIcon.color = GetElementColor(skillElement);
                        SetElementIconVisibility(elementIcon, true);

                        // 🆕 POSICIONAR ÍCONE DE ELEMENTO ABAIXO DA SKILL
                        PositionElementIcon(elementIcon, skillIcon);
                    }
                    else
                    {
                        SetElementIconVisibility(elementIcon, false);
                    }
                }
                else
                {
                    SetElementIconVisibility(elementIcon, false);
                }
            }
        }

        // 🆕 ATUALIZAR ÍCONES DE ELEMENTO DAS SKILLS DE DEFESA
        for (int i = 0; i < 2; i++)
        {
            Image elementIcon = i == 0 ? defenseSkill1ElementIcon : defenseSkill2ElementIcon;
            Image skillIcon = i == 0 ? defenseSkill1Icon : defenseSkill2Icon;

            if (elementIcon != null && skillIcon != null)
            {
                if (defenseSkills.Count > i && defenseSkills[i].isActive)
                {
                    PlayerStats.Element skillElement = defenseSkills[i].element;
                    if (skillElement != PlayerStats.Element.None)
                    {
                        elementIcon.sprite = GetElementSprite(skillElement);
                        elementIcon.color = GetElementColor(skillElement);
                        SetElementIconVisibility(elementIcon, true);

                        // 🆕 POSICIONAR ÍCONE DE ELEMENTO ABAIXO DA SKILL
                        PositionElementIcon(elementIcon, skillIcon);
                    }
                    else
                    {
                        SetElementIconVisibility(elementIcon, false);
                    }
                }
                else
                {
                    SetElementIconVisibility(elementIcon, false);
                }
            }
        }

        // 🆕 ATUALIZAR ÍCONE DE ELEMENTO DA ULTIMATE
        if (ultimateSkillElementIcon != null && ultimateSkillIcon != null)
        {
            if (ultimateSkill.isActive)
            {
                PlayerStats.Element ultimateElement = ultimateSkill.GetEffectiveElement();
                if (ultimateElement != PlayerStats.Element.None)
                {
                    ultimateSkillElementIcon.sprite = GetElementSprite(ultimateElement);
                    ultimateSkillElementIcon.color = GetElementColor(ultimateElement);
                    SetElementIconVisibility(ultimateSkillElementIcon, true);

                    // 🆕 POSICIONAR ÍCONE DE ELEMENTO ABAIXO DA ULTIMATE
                    PositionElementIcon(ultimateSkillElementIcon, ultimateSkillIcon);
                }
                else
                {
                    SetElementIconVisibility(ultimateSkillElementIcon, false);
                }
            }
            else
            {
                SetElementIconVisibility(ultimateSkillElementIcon, false);
            }
        }
    }

    // 🆕 POSICIONAR ÍCONE DE ELEMENTO ABAIXO DA SKILL
    private void PositionElementIcon(Image elementIcon, Image skillIcon)
    {
        if (elementIcon != null && skillIcon != null)
        {
            RectTransform elementRect = elementIcon.GetComponent<RectTransform>();
            RectTransform skillRect = skillIcon.GetComponent<RectTransform>();

            if (elementRect != null && skillRect != null)
            {
                // Posiciona o ícone do elemento abaixo do ícone da skill
                elementRect.anchorMin = new Vector2(0.5f, 0f);
                elementRect.anchorMax = new Vector2(0.5f, 0f);
                elementRect.pivot = new Vector2(0.5f, 0.5f);

                // Ajuste de posição - abaixo do ícone da skill
                Vector2 skillPosition = skillRect.anchoredPosition;
                elementRect.anchoredPosition = new Vector2(
                    skillPosition.x,
                    skillPosition.y - skillRect.rect.height * 0.7f
                );

                // 🆕 TAMANHO PEQUENO DO ÍCONE DE ELEMENTO
                elementRect.sizeDelta = new Vector2(20f, 20f); // Ícone pequeno
            }
        }
    }

    private void Update()
    {
        // ✅ CORRIGIDO: Verificação de null antes de usar Input
        if (Input.GetKeyDown(toggleStatusKey))
        {
            ToggleStatusPanel();
        }

        if (Input.GetKeyDown(toggleSkillsKey) && skillSelectionPanel != null)
        {
            ToggleSkillSelection();
        }

        // ✅ CORRIGIDO: Verificação de null para playerStats
        if (playerStats != null)
        {
            UpdateCooldowns();
            UpdatePlayerStatus();
            UpdateSkillIcons();
            UpdateSkillElementIcons(); // 🆕 ATUALIZAR ÍCONES DE ELEMENTO
            UpdateUltimateSystem();
            UpdateElementUI();
        }
    }

    // 🆕 ATUALIZADO: UpdateSkillIcons - versão simplificada e corrigida
    private void UpdateSkillIcons()
    {
        if (playerStats == null) return;

        try
        {
            var attackSkills = playerStats.GetAttackSkills();
            var defenseSkills = playerStats.GetDefenseSkills();
            var ultimateSkill = playerStats.GetUltimateSkill();

            if (attackSkills == null || defenseSkills == null || ultimateSkill == null) return;

            // ATUALIZAR ÍCONES DE ATAQUE
            for (int i = 0; i < 2; i++)
            {
                Image attackIcon = i == 0 ? attackSkill1Icon : attackSkill2Icon;
                Text attackCooldown = i == 0 ? attackCooldownText1 : attackCooldownText2;

                if (attackIcon == null) continue;

                if (attackSkills.Count > i)
                {
                    var skill = attackSkills[i];
                    if (skill == null) continue;

                    // ✅ CORRIGIDO: Apenas aplica cor, não lida com elementos aqui
                    attackIcon.color = skill.isActive ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);

                    if (attackCooldown != null)
                    {
                        float attackInterval = playerStats.GetAttackActivationInterval();
                        if (attackInterval <= 0) attackInterval = 1f;

                        float cooldownPercent = attackTimers[i] / attackInterval;

                        if (skill.isActive && cooldownPercent < 1f)
                        {
                            attackCooldown.text = $"⏳{(1f - cooldownPercent) * 100f:F0}%";
                            attackCooldown.color = Color.yellow;
                        }
                        else
                        {
                            attackCooldown.text = skill.isActive ? $"✅" : "❌";
                            attackCooldown.color = skill.isActive ? Color.green : Color.red;
                        }
                    }
                }
                else
                {
                    attackIcon.color = new Color(0.3f, 0.3f, 0.3f, 0.3f);
                    if (attackCooldown != null)
                        attackCooldown.text = "🔘 VAZIO";
                }
            }

            // ATUALIZAR ÍCONES DE DEFESA
            for (int i = 0; i < 2; i++)
            {
                Image defenseIcon = i == 0 ? defenseSkill1Icon : defenseSkill2Icon;
                Text defenseCooldown = i == 0 ? defenseCooldownText1 : defenseCooldownText2;

                if (defenseIcon == null) continue;

                if (defenseSkills.Count > i)
                {
                    var skill = defenseSkills[i];
                    if (skill == null) continue;

                    // ✅ CORRIGIDO: Apenas aplica cor, não lida com elementos aqui
                    defenseIcon.color = skill.isActive ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);

                    if (defenseCooldown != null)
                    {
                        float defenseInterval = playerStats.GetDefenseActivationInterval();
                        if (defenseInterval <= 0) defenseInterval = 1f;

                        float cooldownPercent = defenseTimers[i] / defenseInterval;

                        if (skill.isActive && cooldownPercent < 1f)
                        {
                            defenseCooldown.text = $"⏳{(1f - cooldownPercent) * 100f:F0}%";
                            defenseCooldown.color = Color.yellow;
                        }
                        else
                        {
                            defenseCooldown.text = skill.isActive ? $"✅" : "❌";
                            defenseCooldown.color = skill.isActive ? Color.green : Color.red;
                        }
                    }
                }
                else
                {
                    defenseIcon.color = new Color(0.3f, 0.3f, 0.3f, 0.3f);
                    if (defenseCooldown != null)
                        defenseCooldown.text = "🔘 VAZIO";
                }
            }

            // ATUALIZAR ÍCONE DA ULTIMATE
            if (ultimateSkillIcon != null)
            {
                if (ultimateSkill.isActive)
                {
                    ultimateSkillIcon.color = Color.white;

                    if (ultimateCooldownText != null)
                    {
                        ultimateCooldownText.text = ultimateReady ? $"⭐ PRONTO!" : $"⏳ CARREGANDO";
                        ultimateCooldownText.color = ultimateReady ? Color.yellow : Color.white;
                    }
                }
                else
                {
                    ultimateSkillIcon.color = new Color(0.3f, 0.3f, 0.3f, 0.3f);
                    if (ultimateCooldownText != null)
                        ultimateCooldownText.text = "🔒 BLOQUEADA";
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erro em UpdateSkillIcons: {e.Message}");
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

    // 🆕 ATUALIZADO: UpdateElementUI simplificado
    private void UpdateElementUI()
    {
        if (playerStats == null) return;

        var currentElement = playerStats.GetCurrentElement();

        if (currentElementText != null)
        {
            currentElementText.text = $"Elemento: {currentElement}";

            if (elementColors.ContainsKey(currentElement))
            {
                currentElementText.color = elementColors[currentElement];
            }
        }

        if (elementIcon != null)
        {
            elementIcon.color = currentElement == PlayerStats.Element.None ?
                new Color(1, 1, 1, 0.3f) : elementColors[currentElement];
        }
    }

    // 🆕 MÉTODO PARA OBTER COR DO ELEMENTO
    private Color GetElementColor(PlayerStats.Element element)
    {
        if (elementColors.ContainsKey(element))
        {
            return elementColors[element];
        }
        return Color.white;
    }

    // ✅ MÉTODOS RESTANTES CORRIGIDOS E FUNCIONAIS
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
    }

    public void ShowModifierAcquired(string modifierName, string targetSkill)
    {
        ShowSkillAcquired($"✨ {modifierName}", $"Aplicado em: {targetSkill}");
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

    // ✅ CORRIGIDO: UpdatePlayerStatus simplificado
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
    }

    private IEnumerator HideSkillPopup()
    {
        yield return new WaitForSeconds(popupDisplayTime);
        if (skillAcquiredPanel != null)
            skillAcquiredPanel.SetActive(false);
    }

    // 🆕 MÉTODO PARA MOSTRAR INFORMAÇÕES DE SKILL
    public void ShowSkillInfo(SkillData skill)
    {
        if (skillAcquiredPanel == null) return;

        skillNameText.text = skill.skillName;
        skillDescriptionText.text = skill.description;
        skillAcquiredPanel.SetActive(true);
        StartCoroutine(HideSkillPopup());
    }
}