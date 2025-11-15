using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using System.Collections.Generic;

public class UIManagerCanvasCreator : EditorWindow
{
    [MenuItem("Tools/UI Manager/Create Complete UIManager Canvas")]
    public static void CreateCompleteUIManagerCanvas()
    {
        // Criar Canvas principal
        GameObject canvasGO = new GameObject("UIManager_Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Adicionar o UIManager
        UIManager uiManager = canvasGO.AddComponent<UIManager>();

        // Criar todos os painéis
        CreateSkillAcquiredPanel(canvasGO, uiManager);
        CreateHUDSkills(canvasGO, uiManager);
        CreateHUDHealth(canvasGO, uiManager);
        CreateHUDLevelXP(canvasGO, uiManager);
        CreateHUDUltimate(canvasGO, uiManager);
        CreateHUDElement(canvasGO, uiManager);
        CreateElementAdvantagePanel(canvasGO, uiManager);
        CreateStatusPanel(canvasGO, uiManager);
        CreateSkillSelectionPanel(canvasGO, uiManager);
        CreateStatusCardPanel(canvasGO, uiManager);

        // Selecionar o Canvas criado
        Selection.activeGameObject = canvasGO;

        Debug.Log("✅ Canvas UIManager COMPLETO criado com sucesso!");
        Debug.Log("🎮 Controles: Tab (Status), K (Skills), C (Cards), R (Ultimate)");
    }

    private static void CreateSkillAcquiredPanel(GameObject parent, UIManager uiManager)
    {
        GameObject panel = CreatePanel(parent, "SkillAcquiredPanel", new Vector2(500, 200));
        panel.SetActive(false);

        Image bg = panel.GetComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.9f);

        // Skill Name Text (TMP)
        GameObject nameText = CreateTMPText(panel, "SkillNameText", new Vector2(0, 50), new Vector2(480, 60));
        TextMeshProUGUI nameTextComponent = nameText.GetComponent<TextMeshProUGUI>();
        nameTextComponent.text = "NOVA SKILL ADQUIRIDA!";
        nameTextComponent.color = Color.yellow;
        nameTextComponent.fontSize = 28;
        nameTextComponent.fontStyle = FontStyles.Bold;
        nameTextComponent.alignment = TextAlignmentOptions.Center;

        // Skill Description Text (TMP)
        GameObject descText = CreateTMPText(panel, "SkillDescriptionText", new Vector2(0, -30), new Vector2(480, 80));
        TextMeshProUGUI descTextComponent = descText.GetComponent<TextMeshProUGUI>();
        descTextComponent.text = "Descrição detalhada da skill adquirida...";
        descTextComponent.color = Color.white;
        descTextComponent.fontSize = 18;
        descTextComponent.alignment = TextAlignmentOptions.Top;
        descTextComponent.textWrappingMode = TextWrappingModes.Normal;

        uiManager.skillAcquiredPanel = panel;
        uiManager.skillNameText = nameTextComponent;
        uiManager.skillDescriptionText = descTextComponent;
    }

    private static void CreateHUDSkills(GameObject parent, UIManager uiManager)
    {
        GameObject hud = CreatePanel(parent, "HUD_Skills", new Vector2(600, 150));
        RectTransform rect = hud.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
        rect.pivot = new Vector2(0, 0);
        rect.anchoredPosition = new Vector2(20, 20);

        // Attack Skills
        GameObject attackSkills = CreatePanel(hud, "AttackSkills", new Vector2(240, 150));

        // Attack Skill 1
        GameObject skill1 = CreateSkillSlot(attackSkills, "AttackSkill1", new Vector2(-60, 0), uiManager);
        uiManager.attackSkill1Icon = skill1.transform.Find("Icon").GetComponent<Image>();
        uiManager.attackCooldownText1 = skill1.transform.Find("CooldownText").GetComponent<TextMeshProUGUI>();
        uiManager.attackSkill1ElementIcon = CreateElementIcon(skill1, "ElementIcon", new Vector2(0, -60));

        // Attack Skill 2
        GameObject skill2 = CreateSkillSlot(attackSkills, "AttackSkill2", new Vector2(60, 0), uiManager);
        uiManager.attackSkill2Icon = skill2.transform.Find("Icon").GetComponent<Image>();
        uiManager.attackCooldownText2 = skill2.transform.Find("CooldownText").GetComponent<TextMeshProUGUI>();
        uiManager.attackSkill2ElementIcon = CreateElementIcon(skill2, "ElementIcon", new Vector2(0, -60));

        // Defense Skills
        GameObject defenseSkills = CreatePanel(hud, "DefenseSkills", new Vector2(240, 150));
        RectTransform defenseRect = defenseSkills.GetComponent<RectTransform>();
        defenseRect.anchoredPosition = new Vector2(250, 0);

        // Defense Skill 1
        GameObject defense1 = CreateSkillSlot(defenseSkills, "DefenseSkill1", new Vector2(-60, 0), uiManager);
        uiManager.defenseSkill1Icon = defense1.transform.Find("Icon").GetComponent<Image>();
        uiManager.defenseCooldownText1 = defense1.transform.Find("CooldownText").GetComponent<TextMeshProUGUI>();
        uiManager.defenseSkill1ElementIcon = CreateElementIcon(defense1, "ElementIcon", new Vector2(0, -60));

        // Defense Skill 2
        GameObject defense2 = CreateSkillSlot(defenseSkills, "DefenseSkill2", new Vector2(60, 0), uiManager);
        uiManager.defenseSkill2Icon = defense2.transform.Find("Icon").GetComponent<Image>();
        uiManager.defenseCooldownText2 = defense2.transform.Find("CooldownText").GetComponent<TextMeshProUGUI>();
        uiManager.defenseSkill2ElementIcon = CreateElementIcon(defense2, "ElementIcon", new Vector2(0, -60));

        // Ultimate Skill
        GameObject ultimate = CreateUltimateSkillSlot(hud, "UltimateSkill", new Vector2(480, 0), uiManager);
        uiManager.ultimateSkillIcon = ultimate.transform.Find("Icon").GetComponent<Image>();
        uiManager.ultimateCooldownText = ultimate.transform.Find("CooldownText").GetComponent<TextMeshProUGUI>();
        uiManager.ultimateSkillElementIcon = CreateElementIcon(ultimate, "ElementIcon", new Vector2(0, -70));
    }

    // 🆕 MÉTODO PARA CRIAR ÍCONE DE ELEMENTO
    private static Image CreateElementIcon(GameObject parent, string name, Vector2 position)
    {
        GameObject elementIcon = new GameObject(name, typeof(Image));
        elementIcon.transform.SetParent(parent.transform);

        RectTransform rect = elementIcon.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(20, 20);
        rect.anchoredPosition = position;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        Image iconImage = elementIcon.GetComponent<Image>();
        iconImage.color = Color.white;
        iconImage.gameObject.SetActive(false);

        return iconImage;
    }

    private static GameObject CreateSkillSlot(GameObject parent, string name, Vector2 position, UIManager uiManager)
    {
        GameObject slot = CreatePanel(parent, name, new Vector2(100, 100));
        RectTransform rect = slot.GetComponent<RectTransform>();
        rect.anchoredPosition = position;

        // Icon
        GameObject icon = new GameObject("Icon", typeof(Image));
        icon.transform.SetParent(slot.transform);
        RectTransform iconRect = icon.GetComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(80, 80);
        iconRect.anchoredPosition = Vector2.zero;

        Image iconImage = icon.GetComponent<Image>();
        iconImage.color = Color.gray;

        // Cooldown Text (TMP)
        GameObject cooldownText = CreateTMPText(slot, "CooldownText", new Vector2(0, -45), new Vector2(100, 30));
        TextMeshProUGUI textComponent = cooldownText.GetComponent<TextMeshProUGUI>();
        textComponent.text = "PRONTO";
        textComponent.color = Color.green;
        textComponent.fontSize = 12;
        textComponent.fontStyle = FontStyles.Bold;
        textComponent.alignment = TextAlignmentOptions.Center;

        return slot;
    }

    private static GameObject CreateUltimateSkillSlot(GameObject parent, string name, Vector2 position, UIManager uiManager)
    {
        GameObject slot = CreatePanel(parent, name, new Vector2(120, 120));
        RectTransform rect = slot.GetComponent<RectTransform>();
        rect.anchoredPosition = position;

        // Icon
        GameObject icon = new GameObject("Icon", typeof(Image));
        icon.transform.SetParent(slot.transform);
        RectTransform iconRect = icon.GetComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(100, 100);
        iconRect.anchoredPosition = Vector2.zero;

        Image iconImage = icon.GetComponent<Image>();
        iconImage.color = Color.red;

        // Cooldown Text (TMP)
        GameObject cooldownText = CreateTMPText(slot, "CooldownText", new Vector2(0, -55), new Vector2(120, 30));
        TextMeshProUGUI textComponent = cooldownText.GetComponent<TextMeshProUGUI>();
        textComponent.text = "BLOQUEADA";
        textComponent.color = Color.gray;
        textComponent.fontSize = 11;
        textComponent.fontStyle = FontStyles.Bold;
        textComponent.alignment = TextAlignmentOptions.Center;

        return slot;
    }

    private static void CreateHUDHealth(GameObject parent, UIManager uiManager)
    {
        GameObject healthHUD = CreatePanel(parent, "HUD_Health", new Vector2(350, 80));
        RectTransform rect = healthHUD.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1);
        rect.anchorMax = new Vector2(0.5f, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, -20);

        // Health Bar
        GameObject healthBar = CreateSlider(healthHUD, "HealthBar", new Vector2(0, 10), new Vector2(300, 30));
        uiManager.healthBar = healthBar.GetComponent<Slider>();

        // Health Text (TMP)
        GameObject healthText = CreateTMPText(healthHUD, "HealthText", new Vector2(0, -20), new Vector2(300, 30));
        TextMeshProUGUI textComponent = healthText.GetComponent<TextMeshProUGUI>();
        textComponent.text = "100/100";
        textComponent.color = Color.green;
        textComponent.fontSize = 16;
        textComponent.fontStyle = FontStyles.Bold;
        textComponent.alignment = TextAlignmentOptions.Center;

        uiManager.healthText = textComponent;
    }

    private static void CreateHUDLevelXP(GameObject parent, UIManager uiManager)
    {
        GameObject levelHUD = CreatePanel(parent, "HUD_LevelXP", new Vector2(300, 100));
        RectTransform rect = levelHUD.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-20, -20);

        // Level Text (TMP)
        GameObject levelText = CreateTMPText(levelHUD, "LevelText", new Vector2(0, 30), new Vector2(280, 30));
        TextMeshProUGUI levelTextComponent = levelText.GetComponent<TextMeshProUGUI>();
        levelTextComponent.text = "⭐ Level: 1";
        levelTextComponent.color = Color.white;
        levelTextComponent.fontSize = 20;
        levelTextComponent.fontStyle = FontStyles.Bold;
        levelTextComponent.alignment = TextAlignmentOptions.TopRight;

        // XP Text (TMP)
        GameObject xpText = CreateTMPText(levelHUD, "XPText", new Vector2(0, 0), new Vector2(280, 25));
        TextMeshProUGUI xpTextComponent = xpText.GetComponent<TextMeshProUGUI>();
        xpTextComponent.text = "📊 XP: 0/100";
        xpTextComponent.color = Color.cyan;
        xpTextComponent.fontSize = 14;
        xpTextComponent.alignment = TextAlignmentOptions.TopRight;

        // XP Slider
        GameObject xpSlider = CreateSlider(levelHUD, "XPSlider", new Vector2(0, -25), new Vector2(280, 15));
        uiManager.xpSlider = xpSlider.GetComponent<Slider>();

        uiManager.levelText = levelTextComponent;
        uiManager.xpText = xpTextComponent;
    }

    private static void CreateHUDUltimate(GameObject parent, UIManager uiManager)
    {
        GameObject ultimateHUD = CreatePanel(parent, "HUD_Ultimate", new Vector2(300, 80));
        RectTransform rect = ultimateHUD.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0);
        rect.anchorMax = new Vector2(0.5f, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.anchoredPosition = new Vector2(0, 20);

        // Ultimate Charge Bar
        GameObject ultimateBar = CreateSlider(ultimateHUD, "UltimateChargeBar", new Vector2(0, 20), new Vector2(250, 20));
        uiManager.ultimateChargeBar = ultimateBar.GetComponent<Slider>();

        // Ultimate Charge Text (TMP)
        GameObject chargeText = CreateTMPText(ultimateHUD, "UltimateChargeText", new Vector2(0, -10), new Vector2(250, 30));
        TextMeshProUGUI chargeTextComponent = chargeText.GetComponent<TextMeshProUGUI>();
        chargeTextComponent.text = "🚀 ULTIMATE: 0%";
        chargeTextComponent.color = Color.white;
        chargeTextComponent.fontSize = 14;
        chargeTextComponent.alignment = TextAlignmentOptions.Center;

        // Ultimate Ready Effect
        GameObject readyEffect = CreatePanel(ultimateHUD, "UltimateReadyEffect", new Vector2(50, 50));
        readyEffect.SetActive(false);
        Image effectImage = readyEffect.GetComponent<Image>();
        effectImage.color = new Color(1, 1, 0, 0.5f);

        uiManager.ultimateChargeText = chargeTextComponent;
        uiManager.ultimateReadyEffect = readyEffect;
    }

    private static void CreateHUDElement(GameObject parent, UIManager uiManager)
    {
        GameObject elementHUD = CreatePanel(parent, "HUD_Element", new Vector2(250, 80));
        RectTransform rect = elementHUD.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(-20, 20);

        // Element Icon
        GameObject elementIcon = new GameObject("ElementIcon", typeof(Image));
        elementIcon.transform.SetParent(elementHUD.transform);
        RectTransform iconRect = elementIcon.GetComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(50, 50);
        iconRect.anchoredPosition = new Vector2(-40, 0);
        Image iconImage = elementIcon.GetComponent<Image>();
        iconImage.color = new Color(1, 1, 1, 0.3f);

        // Current Element Text (TMP)
        GameObject elementText = CreateTMPText(elementHUD, "CurrentElementText", new Vector2(70, 0), new Vector2(160, 40));
        TextMeshProUGUI textComponent = elementText.GetComponent<TextMeshProUGUI>();
        textComponent.text = "⚡ Elemento: None";
        textComponent.color = Color.white;
        textComponent.fontSize = 14;
        textComponent.alignment = TextAlignmentOptions.Left;

        uiManager.elementIcon = iconImage;
        uiManager.currentElementText = textComponent;
    }

    private static void CreateElementAdvantagePanel(GameObject parent, UIManager uiManager)
    {
        GameObject panel = CreatePanel(parent, "ElementAdvantagePanel", new Vector2(300, 80));
        panel.SetActive(false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0.5f);
        rect.anchorMax = new Vector2(0, 0.5f);
        rect.pivot = new Vector2(0, 0.5f);
        rect.anchoredPosition = new Vector2(20, 0);

        Image bg = panel.GetComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.8f);

        // Advantage Text (TMP)
        GameObject advantageText = CreateTMPText(panel, "AdvantageText", new Vector2(0, 15), new Vector2(280, 25));
        TextMeshProUGUI advantageTextComponent = advantageText.GetComponent<TextMeshProUGUI>();
        advantageTextComponent.text = "Forte contra: ";
        advantageTextComponent.color = Color.green;
        advantageTextComponent.fontSize = 12;
        advantageTextComponent.alignment = TextAlignmentOptions.Left;

        // Disadvantage Text (TMP)
        GameObject disadvantageText = CreateTMPText(panel, "DisadvantageText", new Vector2(0, -15), new Vector2(280, 25));
        TextMeshProUGUI disadvantageTextComponent = disadvantageText.GetComponent<TextMeshProUGUI>();
        disadvantageTextComponent.text = "Fraco contra: ";
        disadvantageTextComponent.color = Color.red;
        disadvantageTextComponent.fontSize = 12;
        disadvantageTextComponent.alignment = TextAlignmentOptions.Left;

        uiManager.elementAdvantagePanel = panel;
        uiManager.advantageText = advantageTextComponent;
        uiManager.disadvantageText = disadvantageTextComponent;
    }

    private static void CreateStatusPanel(GameObject parent, UIManager uiManager)
    {
        GameObject statusPanel = CreatePanel(parent, "StatusPanel", new Vector2(450, 600));
        statusPanel.SetActive(false);
        RectTransform rect = statusPanel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        Image bg = statusPanel.GetComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.95f);

        // Título (TMP)
        GameObject title = CreateTMPText(statusPanel, "Title", new Vector2(0, 280), new Vector2(430, 40));
        TextMeshProUGUI titleText = title.GetComponent<TextMeshProUGUI>();
        titleText.text = "📊 STATUS DO JOGADOR";
        titleText.color = Color.yellow;
        titleText.fontSize = 24;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;

        // Criar todos os textos de status (TMP)
        float startY = 230;
        float spacing = -30;

        uiManager.damageText = CreateTMPStatusText(statusPanel, "DamageText", "⚔️ Ataque: 10.0", new Vector2(0, startY));
        uiManager.speedText = CreateTMPStatusText(statusPanel, "SpeedText", "🏃 Velocidade: 8.0", new Vector2(0, startY + spacing));
        uiManager.defenseText = CreateTMPStatusText(statusPanel, "DefenseText", "🛡️ Defesa: 5", new Vector2(0, startY + spacing * 2));
        uiManager.attackSpeedText = CreateTMPStatusText(statusPanel, "AttackSpeedText", "⚡ Vel. Ataque: 2.0s", new Vector2(0, startY + spacing * 3));
        uiManager.elementInfoText = CreateTMPStatusText(statusPanel, "ElementInfoText", "⚡ Elemento: None\n📈 Bônus: 1.2x", new Vector2(0, startY + spacing * 4), new Vector2(400, 60));
        uiManager.inventoryText = CreateTMPStatusText(statusPanel, "InventoryText", "🎒 Itens: Nenhum", new Vector2(0, startY + spacing * 6));
        uiManager.attackSkillsText = CreateTMPStatusText(statusPanel, "AttackSkillsText", "⚔️ Skills de Ataque:\n- Ataque Automático", new Vector2(-200, startY + spacing * 7), new Vector2(180, 100));
        uiManager.defenseSkillsText = CreateTMPStatusText(statusPanel, "DefenseSkillsText", "🛡️ Skills de Defesa:\n- Proteção Passiva", new Vector2(200, startY + spacing * 7), new Vector2(180, 100));
        uiManager.ultimateSkillsText = CreateTMPStatusText(statusPanel, "UltimateSkillsText", "🚀 Ultimate:\n🔒 Disponível no Level 5", new Vector2(0, startY + spacing * 11), new Vector2(400, 80));

        uiManager.statusPanel = statusPanel;
    }

    private static TextMeshProUGUI CreateTMPStatusText(GameObject parent, string name, string text, Vector2 position, Vector2? size = null)
    {
        Vector2 textSize = size ?? new Vector2(400, 25);
        GameObject textGO = CreateTMPText(parent, name, position, textSize);
        TextMeshProUGUI textComponent = textGO.GetComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.color = Color.white;
        textComponent.fontSize = 12;
        textComponent.alignment = TextAlignmentOptions.Left;
        textComponent.textWrappingMode = TextWrappingModes.Normal;

        return textComponent;
    }

    private static void CreateSkillSelectionPanel(GameObject parent, UIManager uiManager)
    {
        GameObject selectionPanel = CreatePanel(parent, "SkillSelectionPanel", new Vector2(700, 500));
        selectionPanel.SetActive(false);
        RectTransform rect = selectionPanel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        Image bg = selectionPanel.GetComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.95f);

        // Título (TMP)
        GameObject title = CreateTMPText(selectionPanel, "Title", new Vector2(0, 230), new Vector2(680, 40));
        TextMeshProUGUI titleText = title.GetComponent<TextMeshProUGUI>();
        titleText.text = "🎯 SELEÇÃO DE SKILLS";
        titleText.color = Color.yellow;
        titleText.fontSize = 28;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;

        // Available Skills Text (TMP)
        GameObject availableText = CreateTMPText(selectionPanel, "AvailableSkillsText", new Vector2(0, 190), new Vector2(680, 30));
        TextMeshProUGUI availableTextComponent = availableText.GetComponent<TextMeshProUGUI>();
        availableTextComponent.text = "📚 Skills Disponíveis: 0 | ✅ Skills Ativas: 0";
        availableTextComponent.color = Color.cyan;
        availableTextComponent.fontSize = 16;
        availableTextComponent.alignment = TextAlignmentOptions.Center;

        // Skill Button Container
        GameObject container = new GameObject("SkillButtonContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
        container.transform.SetParent(selectionPanel.transform);
        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(650, 350);
        containerRect.anchoredPosition = new Vector2(0, -50);

        VerticalLayoutGroup layout = container.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter sizeFitter = container.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Criar prefab de botão placeholder (TMP)
        GameObject buttonPrefab = CreateTMPButton(container, "SkillButtonPrefab", new Vector2(600, 80));
        buttonPrefab.name = "SkillButtonPrefab";
        buttonPrefab.SetActive(false);

        uiManager.skillSelectionPanel = selectionPanel;
        uiManager.skillButtonContainer = container.transform; // ✅ CORRIGIDO: Transform em vez de GameObject
        uiManager.skillButtonPrefab = buttonPrefab;
        uiManager.availableSkillsText = availableTextComponent;
    }

    // 🆕 NOVO: Painel de Status Cards
    private static void CreateStatusCardPanel(GameObject parent, UIManager uiManager)
    {
        GameObject panel = CreatePanel(parent, "StatusCardPanel", new Vector2(900, 600));
        panel.SetActive(false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        Image bg = panel.GetComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.95f);

        // Tentar carregar sprite de background (opcional)
        var backgroundSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        if (backgroundSprite != null)
        {
            bg.sprite = backgroundSprite;
            bg.type = Image.Type.Sliced;
        }

        // Título (TMP)
        GameObject title = CreateTMPText(panel, "Title", new Vector2(0, 250), new Vector2(600, 60));
        TextMeshProUGUI titleText = title.GetComponent<TextMeshProUGUI>();
        titleText.text = "🃏 CARTAS DE STATUS";
        titleText.color = Color.yellow;
        titleText.fontSize = 32;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;

        // Status Points Text (TMP)
        GameObject pointsText = CreateTMPText(panel, "StatusPointsText", new Vector2(300, 200), new Vector2(300, 40));
        TextMeshProUGUI pointsTextComponent = pointsText.GetComponent<TextMeshProUGUI>();
        pointsTextComponent.text = "🎯 Pontos Disponíveis: 0";
        pointsTextComponent.color = Color.white;
        pointsTextComponent.fontSize = 18;
        pointsTextComponent.fontStyle = FontStyles.Bold;
        pointsTextComponent.alignment = TextAlignmentOptions.Right;

        // Container dos Cards
        GameObject container = new GameObject("StatusCardContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        container.transform.SetParent(panel.transform);
        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(800, 300);
        containerRect.anchoredPosition = new Vector2(0, -50);

        HorizontalLayoutGroup layout = container.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 20;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        // Bônus Ativos (TMP)
        GameObject bonusesText = CreateTMPText(panel, "ActiveBonusesText", new Vector2(-350, 0), new Vector2(250, 300));
        TextMeshProUGUI bonusesTextComponent = bonusesText.GetComponent<TextMeshProUGUI>();
        bonusesTextComponent.text = "✅ BÔNUS ATIVOS:\nNenhum";
        bonusesTextComponent.color = Color.green;
        bonusesTextComponent.fontSize = 14;
        bonusesTextComponent.alignment = TextAlignmentOptions.TopLeft;
        bonusesTextComponent.textWrappingMode = TextWrappingModes.Normal;

        // Botão Fechar - ✅ CORRIGIDO: Chamada correta do método
        GameObject closeButton = CreateTMPButton(panel, "CloseButton", new Vector2(400, -250), new Vector2(100, 40));
        TextMeshProUGUI closeButtonText = closeButton.GetComponentInChildren<TextMeshProUGUI>();
        closeButtonText.text = "FECHAR (C)";
        closeButtonText.color = Color.white;
        closeButtonText.fontSize = 14;

        Button closeBtn = closeButton.GetComponent<Button>();
        // O listener será configurado em runtime

        uiManager.statusCardPanel = panel;
        uiManager.statusCardContainer = container.transform; // ✅ CORRIGIDO: Transform em vez de GameObject
        uiManager.statusPointsText = pointsTextComponent;
        uiManager.activeBonusesText = bonusesTextComponent;
    }

    // ✅ CORRIGIDO: Método CreateTMPButton com todos os parâmetros
    private static GameObject CreateTMPButton(GameObject parent, string name, Vector2 position, Vector2 size)
    {
        GameObject button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        button.transform.SetParent(parent.transform);

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image image = button.GetComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        // Tentar carregar sprite (opcional)
        var buttonSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        if (buttonSprite != null)
        {
            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;
        }

        // Texto do botão (TMP)
        GameObject textGO = CreateTMPText(button, "Text", Vector2.zero, size);
        TextMeshProUGUI textComponent = textGO.GetComponent<TextMeshProUGUI>();
        textComponent.text = "Botão";
        textComponent.color = Color.white;
        textComponent.fontSize = 14;
        textComponent.alignment = TextAlignmentOptions.Center;

        return button;
    }

    // ✅ CORRIGIDO: Método sobrecarregado para criar botão sem position/size (para SkillSelection)
    private static GameObject CreateTMPButton(GameObject parent, string name, Vector2 size)
    {
        return CreateTMPButton(parent, name, Vector2.zero, size);
    }

    // Métodos auxiliares
    private static GameObject CreatePanel(GameObject parent, string name, Vector2 size)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent.transform);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        Image image = panel.GetComponent<Image>();
        image.color = new Color(0.1f, 0.1f, 0.1f, 0.7f);

        return panel;
    }

    // 🆕 MÉTODO PARA CRIAR TEXTO TMP
    private static GameObject CreateTMPText(GameObject parent, string name, Vector2 position, Vector2 size)
    {
        GameObject textGO = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(parent.transform);

        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        TextMeshProUGUI text = textGO.GetComponent<TextMeshProUGUI>();
        text.color = Color.white;
        text.fontSize = 14;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;

        return textGO;
    }

    private static GameObject CreateSlider(GameObject parent, string name, Vector2 position, Vector2 size)
    {
        GameObject sliderGO = new GameObject(name, typeof(RectTransform), typeof(Slider));
        sliderGO.transform.SetParent(parent.transform);

        RectTransform rect = sliderGO.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Slider slider = sliderGO.GetComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 100;
        slider.value = 100;

        // Background
        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(sliderGO.transform);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 1f);

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGO.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1, 0.75f);
        fillAreaRect.sizeDelta = new Vector2(-20, 0);

        // Fill
        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fill.GetComponent<Image>().color = Color.green;

        slider.fillRect = fillRect;

        return sliderGO;
    }
}