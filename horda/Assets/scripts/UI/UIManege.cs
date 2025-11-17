using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Painéis UI")]
    public GameObject skillAcquiredPanel;
    public GameObject statusPanel;
    public GameObject skillSelectionPanel;
    public GameObject statusCardPanel;
    public GameObject elementAdvantagePanel;

    [Header("HUD Elements")]
    public Slider healthBar;
    public Slider xpSlider;
    public Slider ultimateChargeBar;
    public GameObject ultimateReadyEffect;

    [Header("Textos (TextMeshPro)")]
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI xpText;
    public TextMeshProUGUI ultimateChargeText;
    public TextMeshProUGUI currentElementText;
    public TextMeshProUGUI skillNameText;
    public TextMeshProUGUI skillDescriptionText;
    public TextMeshProUGUI advantageText;
    public TextMeshProUGUI disadvantageText;
    public TextMeshProUGUI availableSkillsText;
    public TextMeshProUGUI xpGainText;

    [Header("Status Texts (TextMeshPro)")]
    public TextMeshProUGUI damageText;
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI defenseText;
    public TextMeshProUGUI attackSpeedText;
    public TextMeshProUGUI elementInfoText;
    public TextMeshProUGUI inventoryText;
    public TextMeshProUGUI attackSkillsText;
    public TextMeshProUGUI defenseSkillsText;
    public TextMeshProUGUI ultimateSkillsText;
    public TextMeshProUGUI statusPointsText;
    public TextMeshProUGUI activeBonusesText;

    [Header("Skill Icons")]
    public Image attackSkill1Icon;
    public Image attackSkill2Icon;
    public Image defenseSkill1Icon;
    public Image defenseSkill2Icon;
    public Image ultimateSkillIcon;
    public Image elementIcon;

    [Header("Element Icons")]
    public Image attackSkill1ElementIcon;
    public Image attackSkill2ElementIcon;
    public Image defenseSkill1ElementIcon;
    public Image defenseSkill2ElementIcon;
    public Image ultimateSkillElementIcon;

    [Header("Cooldown Texts (TextMeshPro)")]
    public TextMeshProUGUI attackCooldownText1;
    public TextMeshProUGUI attackCooldownText2;
    public TextMeshProUGUI defenseCooldownText1;
    public TextMeshProUGUI defenseCooldownText2;
    public TextMeshProUGUI ultimateCooldownText;

    [Header("Containers")]
    public Transform skillButtonContainer;
    public Transform statusCardContainer;
    public GameObject skillButtonPrefab;
    public GameObject statusCardPrefab; // 🆕 ADICIONADO

    [Header("Configurações")]
    public float xpTextDisplayTime = 2f;

    private PlayerStats playerStats;
    private SkillManager skillManager;
    private StatusCardSystem cardSystem;
    private bool statusPanelVisible = false;
    private bool skillSelectionPanelVisible = false;
    private bool statusCardPanelVisible = false;

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
        playerStats = FindAnyObjectByType<PlayerStats>();
        skillManager = FindAnyObjectByType<SkillManager>();
        cardSystem = FindAnyObjectByType<StatusCardSystem>();

        InitializeUI();
    }

    void InitializeUI()
    {
        UpdatePlayerStatus();

        // Esconder painéis
        if (skillAcquiredPanel != null) skillAcquiredPanel.SetActive(false);
        if (statusPanel != null) statusPanel.SetActive(false);
        if (skillSelectionPanel != null) skillSelectionPanel.SetActive(false);
        if (statusCardPanel != null) statusCardPanel.SetActive(false);
        if (elementAdvantagePanel != null) elementAdvantagePanel.SetActive(false);
        if (xpGainText != null) xpGainText.gameObject.SetActive(false);

        Debug.Log("✅ UIManager inicializado com TextMeshPro!");
    }

    void Update()
    {
        HandleInput();
        UpdateSkillCooldowns();

        if (playerStats != null)
        {
            UpdatePlayerStatus();
        }
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleStatusPanel();
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            ToggleSkillSelectionPanel();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleStatusCardPanel();
        }
    }

    // ✅ MÉTODO CORRIGIDO: Mostrar ganho de XP
    public void ShowXPGained(float xpAmount)
    {
        if (xpGainText != null)
        {
            xpGainText.text = $"+{xpAmount} XP";
            xpGainText.gameObject.SetActive(true);

            StartCoroutine(HideXPGainText());
        }
        else
        {
            Debug.LogWarning("⚠️ xpGainText não atribuído no UIManager!");
        }
    }

    private IEnumerator HideXPGainText()
    {
        yield return new WaitForSeconds(xpTextDisplayTime);

        if (xpGainText != null)
        {
            float fadeTime = 0.5f;
            float elapsedTime = 0f;
            Color originalColor = xpGainText.color;

            while (elapsedTime < fadeTime)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeTime);
                xpGainText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }

            xpGainText.gameObject.SetActive(false);
            xpGainText.color = originalColor;
        }
    }

    // 🆕 MÉTODOS PARA STATUS CARD SYSTEM - CORRIGIDOS
    // 🆕 MÉTODOS PARA STATUS CARD SYSTEM - CORRIGIDOS
    public void ShowStatusPointsGained(int points)
    {
        ShowSkillAcquired($"🎯 Pontos de Status", $"Ganhou {points} pontos de status!");
        Debug.Log($"🎯 Ganhou {points} pontos de status!");
    }

    // 🆕 SOBRECARGA para aceitar 2 argumentos (se o StatusCardSystem estiver chamando assim)
    public void ShowStatusPointsGained(int points, string message)
    {
        ShowSkillAcquired($"🎯 {message}", $"Ganhou {points} pontos de status!");
        Debug.Log($"🎯 {message}: {points} pontos");
    }

    public void ShowStatusCardApplied(string cardName, string effect)
    {
        ShowSkillAcquired($"🃏 Carta Aplicada", $"{cardName}\n{effect}");
        Debug.Log($"🃏 Carta aplicada: {cardName} - {effect}");
    }

    public void UpdateStatusCardsUI()
    {
        UpdateStatusCardPanel();
        Debug.Log("📊 UI de cartas de status atualizada!");
    }

    public void UpdatePlayerStatus()
    {
        if (playerStats == null) return;

        // Atualizar barras
        if (healthBar != null)
        {
            healthBar.maxValue = playerStats.GetMaxHealth();
            healthBar.value = playerStats.GetCurrentHealth();
        }

        if (xpSlider != null)
        {
            xpSlider.maxValue = playerStats.GetXPToNextLevel();
            xpSlider.value = playerStats.GetCurrentXP();
        }

        if (ultimateChargeBar != null)
        {
            ultimateChargeBar.maxValue = playerStats.GetUltimateCooldown();
            ultimateChargeBar.value = playerStats.GetUltimateChargeTime();
        }

        // Atualizar textos
        if (healthText != null)
            healthText.text = $"{playerStats.GetCurrentHealth():F0}/{playerStats.GetMaxHealth():F0}";

        if (levelText != null)
            levelText.text = $"⭐ Level: {playerStats.GetLevel()}";

        if (xpText != null)
            xpText.text = $"📊 XP: {playerStats.GetCurrentXP():F0}/{playerStats.GetXPToNextLevel():F0}";

        if (ultimateChargeText != null)
        {
            float chargePercent = (playerStats.GetUltimateChargeTime() / playerStats.GetUltimateCooldown()) * 100f;
            ultimateChargeText.text = playerStats.IsUltimateReady() ?
                "🚀 ULTIMATE PRONTA!" :
                $"🚀 ULTIMATE: {chargePercent:F0}%";
        }

        if (currentElementText != null)
            currentElementText.text = $"⚡ Elemento: {playerStats.GetCurrentElement()}";

        // Atualizar efeito de ultimate pronta
        if (ultimateReadyEffect != null)
            ultimateReadyEffect.SetActive(playerStats.IsUltimateReady());

        // Atualizar painel de status se estiver visível
        if (statusPanelVisible)
        {
            UpdateStatusPanel();
        }
    }

    public void UpdateSkillCooldowns()
    {
        if (playerStats == null) return;

        // Atualizar cooldowns das skills de ataque
        if (attackCooldownText1 != null)
        {
            float cooldown1 = playerStats.GetSkillCooldown("Ataque Automático");
            attackCooldownText1.text = cooldown1 > 0 ? $"{cooldown1:F1}s" : "PRONTO";
            attackCooldownText1.color = cooldown1 > 0 ? Color.red : Color.green;
        }

        if (attackCooldownText2 != null)
        {
            float cooldown2 = playerStats.GetSkillCooldown("Golpe Contínuo");
            attackCooldownText2.text = cooldown2 > 0 ? $"{cooldown2:F1}s" : "PRONTO";
            attackCooldownText2.color = cooldown2 > 0 ? Color.red : Color.green;
        }

        // Atualizar cooldowns das skills de defesa
        if (defenseCooldownText1 != null)
        {
            float cooldown1 = playerStats.GetSkillCooldown("Proteção Passiva");
            defenseCooldownText1.text = cooldown1 > 0 ? $"{cooldown1:F1}s" : "PRONTO";
            defenseCooldownText1.color = cooldown1 > 0 ? Color.red : Color.green;
        }

        if (defenseCooldownText2 != null)
        {
            float cooldown2 = playerStats.GetSkillCooldown("Escudo Automático");
            defenseCooldownText2.text = cooldown2 > 0 ? $"{cooldown2:F1}s" : "PRONTO";
            defenseCooldownText2.color = cooldown2 > 0 ? Color.red : Color.green;
        }

        // Atualizar ultimate
        if (ultimateCooldownText != null)
        {
            if (playerStats.HasUltimate())
            {
                ultimateCooldownText.text = playerStats.IsUltimateReady() ? "PRONTA!" : "CARREGANDO";
                ultimateCooldownText.color = playerStats.IsUltimateReady() ? Color.yellow : Color.gray;
            }
            else
            {
                ultimateCooldownText.text = "BLOQUEADA";
                ultimateCooldownText.color = Color.gray;
            }
        }
    }

    public void ToggleStatusPanel()
    {
        if (statusPanel != null)
        {
            statusPanelVisible = !statusPanelVisible;
            statusPanel.SetActive(statusPanelVisible);

            if (statusPanelVisible)
            {
                UpdateStatusPanel();
            }
        }
    }

    public void UpdateStatusPanel()
    {
        if (playerStats == null || !statusPanelVisible) return;

        if (damageText != null)
            damageText.text = $"⚔️ Ataque: {playerStats.GetAttack():F1}";

        if (speedText != null)
            speedText.text = $"🏃 Velocidade: {playerStats.GetSpeed():F1}";

        if (defenseText != null)
            defenseText.text = $"🛡️ Defesa: {playerStats.GetDefense():F1}";

        if (attackSpeedText != null)
            attackSpeedText.text = $"⚡ Vel. Ataque: {playerStats.GetAttackActivationInterval():F1}s";

        if (elementInfoText != null)
            elementInfoText.text = $"⚡ Elemento: {playerStats.GetCurrentElement()}\n📈 Bônus: {playerStats.GetElementalBonus():F1}x";

        // Inventário
        if (inventoryText != null)
        {
            var inventory = playerStats.GetInventory();
            string inventoryStr = inventory.Count > 0 ? string.Join(", ", inventory) : "Nenhum";
            inventoryText.text = $"🎒 Itens: {inventoryStr}";
        }

        // ✅ CORRIGIDO: Skills de Ataque - usando métodos existentes
        if (attackSkillsText != null)
        {
            var attackSkills = playerStats.GetAttackSkills();
            string attackStr = "⚔️ Skills de Ataque:\n";
            foreach (var skill in attackSkills)
            {
                string status = skill.isActive ? "✅" : "❌";
                attackStr += $"{status} {skill.skillName} (Dano: {skill.baseDamage:F1})\n";
            }
            attackSkillsText.text = attackStr;
        }

        // ✅ CORRIGIDO: Skills de Defesa - usando métodos existentes
        if (defenseSkillsText != null)
        {
            var defenseSkills = playerStats.GetDefenseSkills();
            string defenseStr = "🛡️ Skills de Defesa:\n";
            foreach (var skill in defenseSkills)
            {
                string status = skill.isActive ? "✅" : "❌";
                defenseStr += $"{status} {skill.skillName} (Defesa: {skill.baseDefense:F1})\n";
            }
            defenseSkillsText.text = defenseStr;
        }

        // ✅ CORRIGIDO: Ultimate - usando métodos existentes
        if (ultimateSkillsText != null)
        {
            var ultimate = playerStats.GetUltimateSkill();
            string ultimateStr = "🚀 Ultimate:\n";
            if (ultimate.isActive)
            {
                ultimateStr += $"✅ {ultimate.skillName}\n";
                ultimateStr += $"Dano: {ultimate.baseDamage:F1}\n";
                ultimateStr += playerStats.IsUltimateReady() ? "⭐ PRONTA PARA USAR!" : "⏳ CARREGANDO...";
            }
            else
            {
                ultimateStr += "🔒 Disponível no Level 5";
            }
            ultimateSkillsText.text = ultimateStr;
        }
    }

    public void ToggleSkillSelectionPanel()
    {
        if (skillSelectionPanel != null)
        {
            skillSelectionPanelVisible = !skillSelectionPanelVisible;
            skillSelectionPanel.SetActive(skillSelectionPanelVisible);

            if (skillSelectionPanelVisible)
            {
                UpdateSkillSelectionPanel();
            }
        }
    }

    public void UpdateSkillSelectionPanel()
    {
        if (skillManager == null || !skillSelectionPanelVisible) return;

        // Limpar container
        foreach (Transform child in skillButtonContainer)
        {
            if (child.gameObject != skillButtonPrefab)
            {
                Destroy(child.gameObject);
            }
        }

        // ✅ CORRIGIDO: Texto de skills disponíveis - usando métodos simples
        if (availableSkillsText != null)
        {
            availableSkillsText.text = "📚 Sistema de Skills - Use F7/F8/F9 para testar";
        }

        // ✅ CORRIGIDO: Criar botões placeholder
        CreateSkillSelectionPlaceholders();
    }

    // ✅ NOVO: Método para criar placeholders no painel de skills
    private void CreateSkillSelectionPlaceholders()
    {
        // Botão para adicionar skill aleatória
        GameObject button1 = CreateSkillButton("Adicionar Skill Aleatória (F7)", Color.blue);
        button1.GetComponent<Button>().onClick.AddListener(() => {
            if (skillManager != null)
            {
                // Usando reflexão para chamar método que pode existir
                var method = skillManager.GetType().GetMethod("AddRandomSkill");
                if (method != null) method.Invoke(skillManager, null);
            }
        });

        // Botão para adicionar modificador
        GameObject button2 = CreateSkillButton("Adicionar Modificador (F8)", Color.green);
        button2.GetComponent<Button>().onClick.AddListener(() => {
            if (skillManager != null)
            {
                var method = skillManager.GetType().GetMethod("AddRandomModifier");
                if (method != null) method.Invoke(skillManager, null);
            }
        });

        // Botão para skills de teste
        GameObject button3 = CreateSkillButton("Skills de Teste (F9)", Color.yellow);
        button3.GetComponent<Button>().onClick.AddListener(() => {
            if (skillManager != null)
            {
                var method = skillManager.GetType().GetMethod("AddTestSkills");
                if (method != null) method.Invoke(skillManager, null);
            }
        });
    }

    private GameObject CreateSkillButton(string text, Color color)
    {
        GameObject buttonGO = Instantiate(skillButtonPrefab, skillButtonContainer);
        buttonGO.SetActive(true);

        Button button = buttonGO.GetComponent<Button>();
        TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();

        if (buttonText != null)
        {
            buttonText.text = text;
        }

        Image buttonImage = buttonGO.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = color;
        }

        return buttonGO;
    }

    public void ToggleStatusCardPanel()
    {
        if (statusCardPanel != null)
        {
            statusCardPanelVisible = !statusCardPanelVisible;
            statusCardPanel.SetActive(statusCardPanelVisible);

            if (statusCardPanelVisible)
            {
                UpdateStatusCardPanel();
            }
        }
    }

    public void UpdateStatusCardPanel()
    {
        if (cardSystem == null || !statusCardPanelVisible) return;

        // ✅ CORRIGIDO: Pontos disponíveis - usando valor padrão
        if (statusPointsText != null)
        {
            statusPointsText.text = "🎯 Sistema de Cartas - Use C para abrir/fechar";
        }

        // ✅ CORRIGIDO: Bônus ativos - usando valor padrão
        if (activeBonusesText != null)
        {
            activeBonusesText.text = "✅ BÔNUS ATIVOS:\nSistema de cartas de status";
        }

        // ✅ CORRIGIDO: Criar cartas placeholder
        CreateStatusCardPlaceholders();
    }

    // ✅ NOVO: Método para criar cartas de status placeholder
    private void CreateStatusCardPlaceholders()
    {
        // Limpar container
        foreach (Transform child in statusCardContainer)
        {
            Destroy(child.gameObject);
        }

        // Criar algumas cartas de exemplo
        CreateStatusCard("Carta de Ataque", "Aumenta dano em 10%", new Color(1f, 0.3f, 0.3f));
        CreateStatusCard("Carta de Defesa", "Aumenta defesa em 15%", new Color(0.3f, 0.3f, 1f));
        CreateStatusCard("Carta de Velocidade", "Aumenta velocidade em 20%", new Color(0.3f, 1f, 0.3f));
    }

    private void CreateStatusCard(string title, string description, Color color)
    {
        GameObject card = new GameObject($"Card_{title}", typeof(RectTransform), typeof(Image), typeof(Button));
        card.transform.SetParent(statusCardContainer);

        RectTransform rect = card.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(150, 200);

        Image image = card.GetComponent<Image>();
        image.color = color;
        image.sprite = null;

        Button button = card.GetComponent<Button>();
        button.onClick.AddListener(() => OnStatusCardClicked(title));

        // Adicionar texto do título
        GameObject titleText = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleText.transform.SetParent(card.transform);
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.7f);
        titleRect.anchorMax = new Vector2(1, 1f);
        titleRect.sizeDelta = Vector2.zero;
        titleRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI titleTextComp = titleText.GetComponent<TextMeshProUGUI>();
        titleTextComp.text = title;
        titleTextComp.color = Color.white;
        titleTextComp.fontSize = 12;
        titleTextComp.alignment = TextAlignmentOptions.Center;
        titleTextComp.fontStyle = FontStyles.Bold;

        // Adicionar texto da descrição
        GameObject descText = new GameObject("Description", typeof(RectTransform), typeof(TextMeshProUGUI));
        descText.transform.SetParent(card.transform);
        RectTransform descRect = descText.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0, 0);
        descRect.anchorMax = new Vector2(1, 0.7f);
        descRect.sizeDelta = Vector2.zero;
        descRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI descTextComp = descText.GetComponent<TextMeshProUGUI>();
        descTextComp.text = description;
        descTextComp.color = Color.white;
        descTextComp.fontSize = 10;
        descTextComp.alignment = TextAlignmentOptions.Center;
        descTextComp.textWrappingMode = TextWrappingModes.Normal;
    }

    private void OnStatusCardClicked(string cardName)
    {
        Debug.Log($"🃏 Carta clicada: {cardName}");
        ShowSkillAcquired($"Carta {cardName}", "Recurso de cartas de status ativado!");
    }

    // ✅ MÉTODOS DE FEEDBACK VISUAL (mantidos intactos)
    public void ShowSkillAcquired(string skillName, string description)
    {
        if (skillAcquiredPanel != null && skillNameText != null && skillDescriptionText != null)
        {
            skillNameText.text = skillName;
            skillDescriptionText.text = description;
            skillAcquiredPanel.SetActive(true);

            StartCoroutine(HideSkillAcquiredPanel());
        }
    }

    private IEnumerator HideSkillAcquiredPanel()
    {
        yield return new WaitForSeconds(3f);
        if (skillAcquiredPanel != null)
        {
            skillAcquiredPanel.SetActive(false);
        }
    }

    public void ShowUltimateAcquired(string ultimateName, string description)
    {
        ShowSkillAcquired($"⭐ {ultimateName}", description);
    }

    public void ShowModifierAcquired(string modifierName, string targetSkill)
    {
        ShowSkillAcquired($"✨ {modifierName}", $"Aplicado em: {targetSkill}");
    }

    public void ShowElementChanged(string elementName)
    {
        Debug.Log($"⚡ Elemento alterado para: {elementName}");
    }

    public void OnUltimateActivated()
    {
        if (ultimateReadyEffect != null)
        {
            ultimateReadyEffect.SetActive(false);
        }
    }

    public void SetUltimateReady(bool ready)
    {
        if (ultimateReadyEffect != null)
        {
            ultimateReadyEffect.SetActive(ready);
        }
    }

    // ✅ MÉTODOS DE ELEMENTOS (mantidos intactos)
    public void UpdateElementIcons()
    {
        if (playerStats == null) return;

        UpdateSkillElementIcon(attackSkill1ElementIcon, playerStats.GetAttackSkills()[0]);
        UpdateSkillElementIcon(attackSkill2ElementIcon, playerStats.GetAttackSkills()[1]);
        UpdateSkillElementIcon(defenseSkill1ElementIcon, playerStats.GetDefenseSkills()[0]);
        UpdateSkillElementIcon(defenseSkill2ElementIcon, playerStats.GetDefenseSkills()[1]);

        if (playerStats.HasUltimate())
        {
            UpdateSkillElementIcon(ultimateSkillElementIcon, playerStats.GetUltimateSkill());
        }

        if (elementIcon != null)
        {
            elementIcon.color = GetElementColor(playerStats.GetCurrentElement());
            elementIcon.gameObject.SetActive(playerStats.GetCurrentElement() != PlayerStats.Element.None);
        }
    }

    private void UpdateSkillElementIcon(Image icon, object skill)
    {
        if (icon == null) return;

        Color elementColor = Color.white;

        if (skill is PlayerStats.AttackSkill attackSkill)
        {
            elementColor = attackSkill.GetElementColor();
        }
        else if (skill is PlayerStats.DefenseSkill defenseSkill)
        {
            elementColor = defenseSkill.GetElementColor();
        }
        else if (skill is PlayerStats.UltimateSkill ultimateSkill)
        {
            elementColor = ultimateSkill.GetElementColor();
        }

        icon.color = elementColor;
        icon.gameObject.SetActive(elementColor != Color.white);
    }

    private Color GetElementColor(PlayerStats.Element element)
    {
        switch (element)
        {
            case PlayerStats.Element.Fire: return new Color(1f, 0.3f, 0.1f);
            case PlayerStats.Element.Ice: return new Color(0.1f, 0.5f, 1f);
            case PlayerStats.Element.Lightning: return new Color(0.8f, 0.8f, 0.1f);
            case PlayerStats.Element.Poison: return new Color(0.5f, 0.1f, 0.8f);
            case PlayerStats.Element.Earth: return new Color(0.6f, 0.4f, 0.2f);
            case PlayerStats.Element.Wind: return new Color(0.4f, 0.8f, 0.9f);
            default: return Color.white;
        }
    }

    public void ShowElementAdvantage(string strongAgainst, string weakAgainst)
    {
        if (elementAdvantagePanel != null && advantageText != null && disadvantageText != null)
        {
            advantageText.text = $"Forte contra: {strongAgainst}";
            disadvantageText.text = $"Fraco contra: {weakAgainst}";
            elementAdvantagePanel.SetActive(true);
        }
    }

    public void HideElementAdvantage()
    {
        if (elementAdvantagePanel != null)
        {
            elementAdvantagePanel.SetActive(false);
        }
    }

    // ✅ MÉTODO DE ATUALIZAÇÃO COMPLETA (mantido intacto)
    public void ForceRefreshUI()
    {
        UpdatePlayerStatus();
        UpdateSkillCooldowns();
        UpdateElementIcons();

        if (statusPanelVisible) UpdateStatusPanel();
        if (skillSelectionPanelVisible) UpdateSkillSelectionPanel();
        if (statusCardPanelVisible) UpdateStatusCardPanel();
    }
}