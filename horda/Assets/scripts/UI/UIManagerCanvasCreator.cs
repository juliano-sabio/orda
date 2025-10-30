using UnityEngine;
using UnityEngine.UI;
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
        CreateElementAdvantagePanel(canvasGO, uiManager); // 🆕 NOVO
        CreateStatusPanel(canvasGO, uiManager);
        CreateSkillSelectionPanel(canvasGO, uiManager);

        // Selecionar o Canvas criado
        Selection.activeGameObject = canvasGO;

        Debug.Log("✅ Canvas UIManager COMPLETO criado com sucesso!");
    }

    private static void CreateSkillAcquiredPanel(GameObject parent, UIManager uiManager)
    {
        GameObject panel = CreatePanel(parent, "SkillAcquiredPanel", new Vector2(500, 200));
        panel.SetActive(false);

        Image bg = panel.GetComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.9f);

        // Skill Name Text
        GameObject nameText = CreateText(panel, "SkillNameText", new Vector2(0, 50), new Vector2(480, 60));
        Text nameTextComponent = nameText.GetComponent<Text>();
        nameTextComponent.text = "NOVA SKILL ADQUIRIDA!";
        nameTextComponent.color = Color.yellow;
        nameTextComponent.fontSize = 28;
        nameTextComponent.fontStyle = FontStyle.Bold;
        nameTextComponent.alignment = TextAnchor.MiddleCenter;

        // Skill Description Text
        GameObject descText = CreateText(panel, "SkillDescriptionText", new Vector2(0, -30), new Vector2(480, 80));
        Text descTextComponent = descText.GetComponent<Text>();
        descTextComponent.text = "Descrição detalhada da skill adquirida...";
        descTextComponent.color = Color.white;
        descTextComponent.fontSize = 18;
        descTextComponent.alignment = TextAnchor.UpperCenter;

        uiManager.skillAcquiredPanel = panel;
        uiManager.skillNameText = nameTextComponent;
        uiManager.skillDescriptionText = descTextComponent;
    }

    private static void CreateHUDSkills(GameObject parent, UIManager uiManager)
    {
        GameObject hud = CreatePanel(parent, "HUD_Skills", new Vector2(600, 150)); // 🆕 Aumentado para caber ícones de elemento
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
        uiManager.attackCooldownText1 = skill1.transform.Find("CooldownText").GetComponent<Text>();
        uiManager.attackSkill1ElementIcon = CreateElementIcon(skill1, "ElementIcon", new Vector2(0, -60)); // 🆕 Ícone de elemento

        // Attack Skill 2
        GameObject skill2 = CreateSkillSlot(attackSkills, "AttackSkill2", new Vector2(60, 0), uiManager);
        uiManager.attackSkill2Icon = skill2.transform.Find("Icon").GetComponent<Image>();
        uiManager.attackCooldownText2 = skill2.transform.Find("CooldownText").GetComponent<Text>();
        uiManager.attackSkill2ElementIcon = CreateElementIcon(skill2, "ElementIcon", new Vector2(0, -60)); // 🆕 Ícone de elemento

        // Defense Skills
        GameObject defenseSkills = CreatePanel(hud, "DefenseSkills", new Vector2(240, 150));
        RectTransform defenseRect = defenseSkills.GetComponent<RectTransform>();
        defenseRect.anchoredPosition = new Vector2(250, 0);

        // Defense Skill 1
        GameObject defense1 = CreateSkillSlot(defenseSkills, "DefenseSkill1", new Vector2(-60, 0), uiManager);
        uiManager.defenseSkill1Icon = defense1.transform.Find("Icon").GetComponent<Image>();
        uiManager.defenseCooldownText1 = defense1.transform.Find("CooldownText").GetComponent<Text>();
        uiManager.defenseSkill1ElementIcon = CreateElementIcon(defense1, "ElementIcon", new Vector2(0, -60)); // 🆕 Ícone de elemento

        // Defense Skill 2
        GameObject defense2 = CreateSkillSlot(defenseSkills, "DefenseSkill2", new Vector2(60, 0), uiManager);
        uiManager.defenseSkill2Icon = defense2.transform.Find("Icon").GetComponent<Image>();
        uiManager.defenseCooldownText2 = defense2.transform.Find("CooldownText").GetComponent<Text>();
        uiManager.defenseSkill2ElementIcon = CreateElementIcon(defense2, "ElementIcon", new Vector2(0, -60)); // 🆕 Ícone de elemento

        // Ultimate Skill (maior e centralizada)
        GameObject ultimate = CreateUltimateSkillSlot(hud, "UltimateSkill", new Vector2(480, 0), uiManager);
        uiManager.ultimateSkillIcon = ultimate.transform.Find("Icon").GetComponent<Image>();
        uiManager.ultimateCooldownText = ultimate.transform.Find("CooldownText").GetComponent<Text>();
        uiManager.ultimateSkillElementIcon = CreateElementIcon(ultimate, "ElementIcon", new Vector2(0, -70)); // 🆕 Ícone de elemento
    }

    // 🆕 MÉTODO PARA CRIAR ÍCONE DE ELEMENTO
    private static Image CreateElementIcon(GameObject parent, string name, Vector2 position)
    {
        GameObject elementIcon = new GameObject(name, typeof(Image));
        elementIcon.transform.SetParent(parent.transform);

        RectTransform rect = elementIcon.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(20, 20); // 🆕 Tamanho pequeno
        rect.anchoredPosition = position;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        Image iconImage = elementIcon.GetComponent<Image>();
        iconImage.color = Color.white;
        iconImage.gameObject.SetActive(false); // 🆕 Inicia oculto

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
        iconImage.color = Color.gray; // Placeholder

        // Cooldown Text
        GameObject cooldownText = CreateText(slot, "CooldownText", new Vector2(0, -45), new Vector2(100, 30));
        Text textComponent = cooldownText.GetComponent<Text>();
        textComponent.text = "PRONTO";
        textComponent.color = Color.green;
        textComponent.fontSize = 12;
        textComponent.fontStyle = FontStyle.Bold;
        textComponent.alignment = TextAnchor.MiddleCenter;

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
        iconImage.color = Color.red; // Destaque para ultimate

        // Cooldown Text
        GameObject cooldownText = CreateText(slot, "CooldownText", new Vector2(0, -55), new Vector2(120, 30));
        Text textComponent = cooldownText.GetComponent<Text>();
        textComponent.text = "BLOQUEADA";
        textComponent.color = Color.gray;
        textComponent.fontSize = 11;
        textComponent.fontStyle = FontStyle.Bold;
        textComponent.alignment = TextAnchor.MiddleCenter;

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

        // Health Text
        GameObject healthText = CreateText(healthHUD, "HealthText", new Vector2(0, -20), new Vector2(300, 30));
        Text textComponent = healthText.GetComponent<Text>();
        textComponent.text = "100/100";
        textComponent.color = Color.green;
        textComponent.fontSize = 16;
        textComponent.fontStyle = FontStyle.Bold;
        textComponent.alignment = TextAnchor.MiddleCenter;

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

        // Level Text
        GameObject levelText = CreateText(levelHUD, "LevelText", new Vector2(0, 30), new Vector2(280, 30));
        Text levelTextComponent = levelText.GetComponent<Text>();
        levelTextComponent.text = "⭐ Level: 1"; // 🆕 Com emoji
        levelTextComponent.color = Color.white;
        levelTextComponent.fontSize = 20;
        levelTextComponent.fontStyle = FontStyle.Bold;
        levelTextComponent.alignment = TextAnchor.UpperRight;

        // XP Text
        GameObject xpText = CreateText(levelHUD, "XPText", new Vector2(0, 0), new Vector2(280, 25));
        Text xpTextComponent = xpText.GetComponent<Text>();
        xpTextComponent.text = "📊 XP: 0/100"; // 🆕 Com emoji
        xpTextComponent.color = Color.cyan;
        xpTextComponent.fontSize = 14;
        xpTextComponent.alignment = TextAnchor.UpperRight;

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

        // Ultimate Charge Text
        GameObject chargeText = CreateText(ultimateHUD, "UltimateChargeText", new Vector2(0, -10), new Vector2(250, 30));
        Text chargeTextComponent = chargeText.GetComponent<Text>();
        chargeTextComponent.text = "🚀 ULTIMATE: 0%"; // 🆕 Com emoji
        chargeTextComponent.color = Color.white;
        chargeTextComponent.fontSize = 14;
        chargeTextComponent.alignment = TextAnchor.MiddleCenter;

        // Ultimate Ready Effect (placeholder)
        GameObject readyEffect = CreatePanel(ultimateHUD, "UltimateReadyEffect", new Vector2(50, 50));
        readyEffect.SetActive(false);
        Image effectImage = readyEffect.GetComponent<Image>();
        effectImage.color = new Color(1, 1, 0, 0.5f); // Amarelo semi-transparente

        uiManager.ultimateChargeText = chargeTextComponent;
        uiManager.ultimateReadyEffect = readyEffect;
    }

    private static void CreateHUDElement(GameObject parent, UIManager uiManager)
    {
        GameObject elementHUD = CreatePanel(parent, "HUD_Element", new Vector2(250, 80)); // 🆕 Aumentado
        RectTransform rect = elementHUD.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(-20, 20);

        // Element Icon
        GameObject elementIcon = new GameObject("ElementIcon", typeof(Image));
        elementIcon.transform.SetParent(elementHUD.transform);
        RectTransform iconRect = elementIcon.GetComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(50, 50); // 🆕 Aumentado
        iconRect.anchoredPosition = new Vector2(-40, 0);
        Image iconImage = elementIcon.GetComponent<Image>();
        iconImage.color = new Color(1, 1, 1, 0.3f); // 🆕 Inicia semi-transparente

        // Current Element Text
        GameObject elementText = CreateText(elementHUD, "CurrentElementText", new Vector2(70, 0), new Vector2(160, 40));
        Text textComponent = elementText.GetComponent<Text>();
        textComponent.text = "⚡ Elemento: None"; // 🆕 Com emoji
        textComponent.color = Color.white;
        textComponent.fontSize = 14;
        textComponent.alignment = TextAnchor.MiddleLeft;

        uiManager.elementIcon = iconImage;
        uiManager.currentElementText = textComponent;
    }

    // 🆕 NOVO: Painel de Vantagens/Desvantagens Elementais
    private static void CreateElementAdvantagePanel(GameObject parent, UIManager uiManager)
    {
        GameObject panel = CreatePanel(parent, "ElementAdvantagePanel", new Vector2(300, 80));
        panel.SetActive(false); // 🆕 Inicia oculto
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0.5f);
        rect.anchorMax = new Vector2(0, 0.5f);
        rect.pivot = new Vector2(0, 0.5f);
        rect.anchoredPosition = new Vector2(20, 0);

        Image bg = panel.GetComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.8f);

        // Advantage Text
        GameObject advantageText = CreateText(panel, "AdvantageText", new Vector2(0, 15), new Vector2(280, 25));
        Text advantageTextComponent = advantageText.GetComponent<Text>();
        advantageTextComponent.text = "Forte contra: ";
        advantageTextComponent.color = Color.green;
        advantageTextComponent.fontSize = 12;
        advantageTextComponent.alignment = TextAnchor.MiddleLeft;

        // Disadvantage Text
        GameObject disadvantageText = CreateText(panel, "DisadvantageText", new Vector2(0, -15), new Vector2(280, 25));
        Text disadvantageTextComponent = disadvantageText.GetComponent<Text>();
        disadvantageTextComponent.text = "Fraco contra: ";
        disadvantageTextComponent.color = Color.red;
        disadvantageTextComponent.fontSize = 12;
        disadvantageTextComponent.alignment = TextAnchor.MiddleLeft;

        uiManager.elementAdvantagePanel = panel;
        uiManager.advantageText = advantageTextComponent;
        uiManager.disadvantageText = disadvantageTextComponent;
    }

    private static void CreateStatusPanel(GameObject parent, UIManager uiManager)
    {
        GameObject statusPanel = CreatePanel(parent, "StatusPanel", new Vector2(450, 600)); // 🆕 Aumentado
        statusPanel.SetActive(false);
        RectTransform rect = statusPanel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        Image bg = statusPanel.GetComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.95f);

        // Título
        GameObject title = CreateText(statusPanel, "Title", new Vector2(0, 280), new Vector2(430, 40));
        Text titleText = title.GetComponent<Text>();
        titleText.text = "📊 STATUS DO JOGADOR";
        titleText.color = Color.yellow;
        titleText.fontSize = 24;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;

        // Criar todos os textos de status
        float startY = 230;
        float spacing = -30;

        uiManager.damageText = CreateStatusText(statusPanel, "DamageText", "⚔️ Ataque: 10.0", new Vector2(0, startY)); // 🆕 Com emoji
        uiManager.speedText = CreateStatusText(statusPanel, "SpeedText", "🏃 Velocidade: 8.0", new Vector2(0, startY + spacing)); // 🆕 Com emoji
        uiManager.defenseText = CreateStatusText(statusPanel, "DefenseText", "🛡️ Defesa: 5", new Vector2(0, startY + spacing * 2)); // 🆕 Com emoji
        uiManager.attackSpeedText = CreateStatusText(statusPanel, "AttackSpeedText", "⚡ Vel. Ataque: 2.0s", new Vector2(0, startY + spacing * 3)); // 🆕 Com emoji

        // 🆕 Element Info Text com mais espaço
        uiManager.elementInfoText = CreateStatusText(statusPanel, "ElementInfoText", "⚡ Elemento: None\n📈 Bônus: 1.2x", new Vector2(0, startY + spacing * 4), new Vector2(400, 60));

        uiManager.inventoryText = CreateStatusText(statusPanel, "InventoryText", "🎒 Itens: Nenhum", new Vector2(0, startY + spacing * 6)); // 🆕 Com emoji

        // 🆕 Skills com mais espaço para elementos
        uiManager.attackSkillsText = CreateStatusText(statusPanel, "AttackSkillsText", "⚔️ Skills de Ataque:\n- Ataque Automático", new Vector2(-200, startY + spacing * 7), new Vector2(180, 100));
        uiManager.defenseSkillsText = CreateStatusText(statusPanel, "DefenseSkillsText", "🛡️ Skills de Defesa:\n- Proteção Passiva", new Vector2(200, startY + spacing * 7), new Vector2(180, 100));

        uiManager.ultimateSkillsText = CreateStatusText(statusPanel, "UltimateSkillsText", "🚀 Ultimate:\n🔒 Disponível no Level 5", new Vector2(0, startY + spacing * 11), new Vector2(400, 80));

        uiManager.statusPanel = statusPanel;
    }

    private static Text CreateStatusText(GameObject parent, string name, string text, Vector2 position, Vector2? size = null)
    {
        Vector2 textSize = size ?? new Vector2(400, 25);
        GameObject textGO = CreateText(parent, name, position, textSize);
        Text textComponent = textGO.GetComponent<Text>();
        textComponent.text = text;
        textComponent.color = Color.white;
        textComponent.fontSize = 12;
        textComponent.alignment = TextAnchor.MiddleLeft;
        textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        textComponent.verticalOverflow = VerticalWrapMode.Overflow;

        return textComponent;
    }

    private static void CreateSkillSelectionPanel(GameObject parent, UIManager uiManager)
    {
        GameObject selectionPanel = CreatePanel(parent, "SkillSelectionPanel", new Vector2(700, 500)); // 🆕 Aumentado
        selectionPanel.SetActive(false);
        RectTransform rect = selectionPanel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        Image bg = selectionPanel.GetComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.95f);

        // Título
        GameObject title = CreateText(selectionPanel, "Title", new Vector2(0, 230), new Vector2(680, 40));
        Text titleText = title.GetComponent<Text>();
        titleText.text = "🎯 SELEÇÃO DE SKILLS";
        titleText.color = Color.yellow;
        titleText.fontSize = 28;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;

        // Available Skills Text
        GameObject availableText = CreateText(selectionPanel, "AvailableSkillsText", new Vector2(0, 190), new Vector2(680, 30));
        Text availableTextComponent = availableText.GetComponent<Text>();
        availableTextComponent.text = "📚 Skills Disponíveis: 0 | ✅ Skills Ativas: 0"; // 🆕 Com emojis
        availableTextComponent.color = Color.cyan;
        availableTextComponent.fontSize = 16;
        availableTextComponent.alignment = TextAnchor.MiddleCenter;

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

        // Criar prefab de botão placeholder
        GameObject buttonPrefab = CreateButton(container, "SkillButtonPrefab", new Vector2(600, 80)); // 🆕 Aumentado
        buttonPrefab.name = "SkillButtonPrefab";
        buttonPrefab.SetActive(false);

        uiManager.skillSelectionPanel = selectionPanel;
        uiManager.skillButtonContainer = container.transform;
        uiManager.skillButtonPrefab = buttonPrefab;
        uiManager.availableSkillsText = availableTextComponent;
    }

    private static GameObject CreateButton(GameObject parent, string name, Vector2 size)
    {
        GameObject button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        button.transform.SetParent(parent.transform);

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.sizeDelta = size;

        Image image = button.GetComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        // Texto do botão
        GameObject textGO = CreateText(button, "Text", Vector2.zero, size);
        Text textComponent = textGO.GetComponent<Text>();
        textComponent.text = "Nome da Skill\nDescrição da skill";
        textComponent.color = Color.white;
        textComponent.fontSize = 14; // 🆕 Aumentado
        textComponent.alignment = TextAnchor.MiddleCenter;
        textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        textComponent.verticalOverflow = VerticalWrapMode.Overflow;

        return button;
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

    private static GameObject CreateText(GameObject parent, string name, Vector2 position, Vector2 size)
    {
        GameObject textGO = new GameObject(name, typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(parent.transform);

        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Text text = textGO.GetComponent<Text>();
        text.color = Color.white;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 14;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

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