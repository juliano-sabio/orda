using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillChoiceUI : MonoBehaviour
{
    [Header("Referências UI")]
    public GameObject choicePanel;
    public Text titleText;
    public TextMeshProUGUI titleTextTMP;
    public Transform skillsContainer;
    public GameObject skillChoicePrefab; // 🎯 AGORA É FALLBACK
    public Button confirmButton;

    [Header("Configurações")]
    public float autoCloseDelay = 2f;
    public bool pauseGameDuringChoice = true;

    [Header("Layout Horizontal")]
    public bool useHorizontalLayout = true;
    public float cardSpacing = 20f;
    public Vector2 cardSize = new Vector2(200f, 250f);

    [Header("Fallbacks Automáticos")]
    public bool createFallbackUI = true;

    [Header("🎨 Templates de Card (Fallbacks)")]
    public GameObject defaultCardTemplate;
    public GameObject fireCardTemplate;
    public GameObject iceCardTemplate;
    public GameObject lightningCardTemplate;
    public GameObject poisonCardTemplate;
    public GameObject earthCardTemplate;
    public GameObject windCardTemplate;

    private List<SkillData> currentChoices;
    private System.Action<SkillData> onSkillChosen;
    private List<GameObject> currentButtons = new List<GameObject>();
    private float previousTimeScale;
    private GameObject fallbackPanel;

    void Awake()
    {
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("⚠️ SkillChoiceUI inativo no Awake! Ativando...");
            gameObject.SetActive(true);
        }

        if (choicePanel != null)
        {
            choicePanel.SetActive(false);
        }
    }

    void Start()
    {
        Debug.Log("🔄 SkillChoiceUI iniciando...");
        StartCoroutine(InitializeWithDelay());
    }

    private IEnumerator InitializeWithDelay()
    {
        yield return new WaitForSeconds(0.5f);
        CheckAndFixConfiguration();
        SetupHorizontalLayout();
        Debug.Log("✅ SkillChoiceUI inicializado completamente");
    }

    private void CheckAndFixConfiguration()
    {
        Debug.Log("🔧 Verificando configuração do SkillChoiceUI...");

        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
            Debug.Log("✅ GameObject ativado");
        }

        if (choicePanel == null)
        {
            Debug.LogWarning("⚠️ ChoicePanel não atribuído! Procurando automaticamente...");
            choicePanel = FindPanelInChildren();

            if (choicePanel == null && createFallbackUI)
            {
                choicePanel = CreateFallbackPanel();
                Debug.Log("✅ Painel fallback criado");
            }
        }

        // 🎯 AGORA VERIFICA TEMPLATES DE CARD
        CheckCardTemplates();

        if (skillsContainer == null && choicePanel != null)
        {
            skillsContainer = choicePanel.transform;
            Debug.Log("✅ Usando transform do painel como container");
        }

        Debug.Log("✅ Configuração verificada e corrigida");
    }

    // 🎯 NOVO: Verificar e carregar templates de card
    private void CheckCardTemplates()
    {
        // Carregar templates se não estiverem atribuídos
        if (defaultCardTemplate == null)
            defaultCardTemplate = Resources.Load<GameObject>("Cards/DefaultSkillCard");

        if (fireCardTemplate == null)
            fireCardTemplate = Resources.Load<GameObject>("Cards/ElementalCard_Fire");

        if (iceCardTemplate == null)
            iceCardTemplate = Resources.Load<GameObject>("Cards/ElementalCard_Ice");

        if (lightningCardTemplate == null)
            lightningCardTemplate = Resources.Load<GameObject>("Cards/ElementalCard_Lightning");

        if (poisonCardTemplate == null)
            poisonCardTemplate = Resources.Load<GameObject>("Cards/ElementalCard_Poison");

        if (earthCardTemplate == null)
            earthCardTemplate = Resources.Load<GameObject>("Cards/ElementalCard_Earth");

        if (windCardTemplate == null)
            windCardTemplate = Resources.Load<GameObject>("Cards/ElementalCard_Wind");

        // Fallback final
        if (skillChoicePrefab == null && defaultCardTemplate != null)
        {
            skillChoicePrefab = defaultCardTemplate;
            Debug.Log("✅ Usando DefaultCardTemplate como fallback");
        }
    }

    private void SetupHorizontalLayout()
    {
        if (skillsContainer == null || !useHorizontalLayout) return;

        // Adicionar Horizontal Layout Group
        HorizontalLayoutGroup layout = skillsContainer.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
            layout = skillsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();

        // Configurar layout horizontal
        layout.spacing = cardSpacing;
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        // Adicionar Content Size Fitter se necessário
        ContentSizeFitter sizeFitter = skillsContainer.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
            sizeFitter = skillsContainer.gameObject.AddComponent<ContentSizeFitter>();

        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

        Debug.Log("✅ Layout horizontal configurado");
    }

    private GameObject FindPanelInChildren()
    {
        foreach (Transform child in transform)
        {
            if (child.name.Contains("Panel") || child.name.Contains("painel", System.StringComparison.OrdinalIgnoreCase))
            {
                return child.gameObject;
            }
        }

        if (transform.childCount > 0)
        {
            return transform.GetChild(0).gameObject;
        }

        return null;
    }

    private GameObject CreateFallbackPanel()
    {
        GameObject panel = new GameObject("SkillChoicePanel_Fallback");
        panel.transform.SetParent(transform);

        RectTransform rect = panel.AddComponent<RectTransform>();
        Image image = panel.AddComponent<Image>();

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(800, 400); // Mais largo para layout horizontal
        rect.anchoredPosition = Vector2.zero;

        image.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        GameObject container = new GameObject("SkillsContainer");
        container.transform.SetParent(panel.transform);
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(700, 300);
        containerRect.anchoredPosition = Vector2.zero;

        skillsContainer = container.transform;

        return panel;
    }

    // 🎯 ATUALIZADO: Criar fallback apenas se necessário
    private GameObject CreateFallbackPrefab()
    {
        Debug.Log("🔧 Criando prefab fallback de emergência...");

        GameObject prefab = new GameObject("SkillCard_Fallback");

        RectTransform rect = prefab.AddComponent<RectTransform>();
        Image image = prefab.AddComponent<Image>();
        Button button = prefab.AddComponent<Button>();

        // Tamanho do card
        rect.sizeDelta = cardSize;
        image.color = new Color(0.2f, 0.2f, 0.3f);
        image.sprite = null;

        // Adicionar Layout Element para controle de tamanho
        LayoutElement layout = prefab.AddComponent<LayoutElement>();
        layout.preferredWidth = cardSize.x;
        layout.preferredHeight = cardSize.y;
        layout.flexibleWidth = 0;
        layout.flexibleHeight = 0;

        // Criar estrutura básica do card
        CreateCardStructure(prefab);

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.3f);
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.5f);
        colors.pressedColor = new Color(0.4f, 0.4f, 0.7f);
        colors.selectedColor = new Color(0.3f, 0.3f, 0.5f);
        button.colors = colors;

        prefab.SetActive(false);
        return prefab;
    }

    private void CreateCardStructure(GameObject card)
    {
        // Área do ícone (topo)
        GameObject iconArea = CreateUIElement("IconArea", card.transform,
            new Vector2(0f, 0.7f), new Vector2(1f, 1f), new Vector2(0, 0));
        Image iconImage = iconArea.AddComponent<Image>();
        iconImage.color = new Color(0.3f, 0.3f, 0.4f);

        // Área do nome da skill
        GameObject nameArea = CreateUIElement("NameArea", card.transform,
            new Vector2(0f, 0.5f), new Vector2(1f, 0.7f), new Vector2(0, 0));
        TextMeshProUGUI nameText = nameArea.AddComponent<TextMeshProUGUI>();
        nameText.text = "Skill Name";
        nameText.color = Color.white;
        nameText.fontSize = 16;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.fontStyle = FontStyles.Bold;

        // Área da descrição
        GameObject descArea = CreateUIElement("DescArea", card.transform,
            new Vector2(0f, 0.2f), new Vector2(1f, 0.5f), new Vector2(10, 10));
        TextMeshProUGUI descText = descArea.AddComponent<TextMeshProUGUI>();
        descText.text = "Skill description will appear here";
        descText.color = new Color(0.8f, 0.8f, 0.9f);
        descText.fontSize = 12;
        descText.alignment = TextAlignmentOptions.Center;
        descText.textWrappingMode = TextWrappingModes.Normal;

        // Área dos status/bônus
        GameObject statsArea = CreateUIElement("StatsArea", card.transform,
            new Vector2(0f, 0f), new Vector2(1f, 0.2f), new Vector2(5, 5));
        TextMeshProUGUI statsText = statsArea.AddComponent<TextMeshProUGUI>();
        statsText.text = "❤️+10 ⚔️+5";
        statsText.color = new Color(0.9f, 0.8f, 0.3f);
        statsText.fontSize = 11;
        statsText.alignment = TextAlignmentOptions.Center;
    }

    private GameObject CreateUIElement(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
    {
        GameObject element = new GameObject(name, typeof(RectTransform));
        element.transform.SetParent(parent);

        RectTransform rect = element.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = Vector2.zero;

        return element;
    }

    public void ShowSkillChoice(List<SkillData> skills, System.Action<SkillData> callback)
    {
        Debug.Log("🎯 ShowSkillChoice chamado!");

        if (skills == null || skills.Count == 0)
        {
            Debug.LogError("❌ Lista de skills vazia!");
            return;
        }

        CheckAndFixConfiguration();

        if (!gameObject.activeInHierarchy)
        {
            Debug.LogError("❌ SkillChoiceUI GameObject está INATIVO! Ativando...");
            gameObject.SetActive(true);
        }

        currentChoices = skills;
        onSkillChosen = callback;

        PauseGame();
        ClearSkillButtons();

        if (choicePanel != null)
        {
            choicePanel.SetActive(true);
            Debug.Log("✅ Painel de escolha ativado");
        }
        else
        {
            Debug.LogError("❌ ChoicePanel não atribuído mesmo após fallback!");
            ResumeGame();
            return;
        }

        UpdateTitleText();
        Debug.Log($"📋 Mostrando escolha de {skills.Count} skills");

        StartCoroutine(CreateSkillButtonsWithDelay(skills));
    }

    private void PauseGame()
    {
        if (pauseGameDuringChoice)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            Debug.Log("⏸️ Jogo pausado durante escolha de skill");
            AudioListener.pause = true;
        }
    }

    private void ResumeGame()
    {
        if (pauseGameDuringChoice)
        {
            Time.timeScale = previousTimeScale;
            AudioListener.pause = false;
            Debug.Log("▶️ Jogo despausado");
        }
    }

    private void UpdateTitleText()
    {
        PlayerStats playerStats = FindAnyObjectByType<PlayerStats>();
        int currentLevel = playerStats != null ? playerStats.level : 1;
        string title = $"🎯 ESCOLHA UMA SKILL (Nível {currentLevel})";

        if (titleTextTMP != null)
        {
            titleTextTMP.text = title;
        }
        else if (titleText != null)
        {
            titleText.text = title;
        }
        else
        {
            Debug.LogWarning("⚠️ Nenhum componente de texto atribuído para o título");
        }
    }

    private IEnumerator CreateSkillButtonsWithDelay(List<SkillData> skills)
    {
        yield return new WaitForEndOfFrame();

        for (int i = 0; i < skills.Count; i++)
        {
            CreateSkillChoiceButton(skills[i], i);
        }

        Debug.Log($"✅ {skills.Count} cards de skill criados");
    }

    private void ClearSkillButtons()
    {
        foreach (GameObject button in currentButtons)
        {
            if (button != null)
                Destroy(button);
        }
        currentButtons.Clear();

        if (skillsContainer != null)
        {
            foreach (Transform child in skillsContainer)
            {
                if (child != null && child.gameObject != null)
                    Destroy(child.gameObject);
            }
        }
    }

    // 🎯 MÉTODO PRINCIPAL ATUALIZADO: Hierarquia inteligente de prefabs
    private void CreateSkillChoiceButton(SkillData skill, int index)
    {
        GameObject cardPrefabToUse = GetCardPrefabForSkill(skill);

        if (cardPrefabToUse == null)
        {
            Debug.LogError($"❌ Não foi possível encontrar prefab para: {skill.skillName}");
            return;
        }

        GameObject cardObj = Instantiate(cardPrefabToUse, skillsContainer);
        currentButtons.Add(cardObj);

        Button button = cardObj.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError($"❌ Botão não encontrado no prefab para: {skill.skillName}");
            return;
        }

        SetupSkillCard(cardObj, skill);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnSkillSelected(skill));

        cardObj.SetActive(true);
        Debug.Log($"✅ Card criado para: {skill.skillName} (Prefab: {cardPrefabToUse.name})");
    }

    // 🎯 NOVO: Hierarquia inteligente para escolher o prefab do card
    private GameObject GetCardPrefabForSkill(SkillData skill)
    {
        // 1. 🥇 PRIORIDADE MÁXIMA: Prefab específico da skill
        if (skill.cardPrefab != null)
        {
            Debug.Log($"🎨 Usando card personalizado para: {skill.skillName}");
            return skill.cardPrefab;
        }

        // 2. 🥈 PRIORIDADE ALTA: Template por elemento
        GameObject elementalTemplate = GetElementalCardTemplate(skill.element);
        if (elementalTemplate != null)
        {
            Debug.Log($"⚡ Usando template elemental ({skill.element}) para: {skill.skillName}");
            return elementalTemplate;
        }

        // 3. 🥉 PRIORIDADE MÉDIA: Prefab geral do sistema
        if (skillChoicePrefab != null)
        {
            Debug.Log($"🎯 Usando card padrão do sistema para: {skill.skillName}");
            return skillChoicePrefab;
        }

        // 4. 🆘 EMERGÊNCIA: Criar fallback automático
        Debug.LogWarning($"⚠️ Criando card fallback de emergência para: {skill.skillName}");
        return CreateFallbackPrefab();
    }

    // 🎯 NOVO: Obter template baseado no elemento
    private GameObject GetElementalCardTemplate(PlayerStats.Element element)
    {
        switch (element)
        {
            case PlayerStats.Element.Fire: return fireCardTemplate;
            case PlayerStats.Element.Ice: return iceCardTemplate;
            case PlayerStats.Element.Lightning: return lightningCardTemplate;
            case PlayerStats.Element.Poison: return poisonCardTemplate;
            case PlayerStats.Element.Earth: return earthCardTemplate;
            case PlayerStats.Element.Wind: return windCardTemplate;
            default: return defaultCardTemplate;
        }
    }

    private void SetupSkillCard(GameObject cardObj, SkillData skill)
    {
        // Encontrar componentes do card
        Transform iconArea = cardObj.transform.Find("IconArea");
        Transform nameArea = cardObj.transform.Find("NameArea");
        Transform descArea = cardObj.transform.Find("DescArea");
        Transform statsArea = cardObj.transform.Find("StatsArea");

        // Configurar cor de fundo baseada no elemento (se não tiver prefab específico)
        if (skill.cardPrefab == null)
        {
            Image cardImage = cardObj.GetComponent<Image>();
            if (cardImage != null)
            {
                Color elementColor = GetElementColor(skill.element);
                cardImage.color = elementColor * 0.3f + new Color(0.1f, 0.1f, 0.1f);
            }
        }

        // Configurar ícone
        if (iconArea != null)
        {
            Image iconImage = iconArea.GetComponent<Image>();
            if (iconImage != null && skill.icon != null)
            {
                iconImage.sprite = skill.icon;
                iconImage.color = GetElementColor(skill.element);
            }
            else if (iconImage != null)
            {
                iconImage.color = GetElementColor(skill.element);

                // Adicionar texto do elemento se não tiver ícone
                TextMeshProUGUI elementText = iconArea.GetComponent<TextMeshProUGUI>();
                if (elementText == null)
                    elementText = iconArea.gameObject.AddComponent<TextMeshProUGUI>();

                elementText.text = GetElementIcon(skill.element);
                elementText.color = Color.white;
                elementText.fontSize = 24;
                elementText.alignment = TextAlignmentOptions.Center;
            }
        }

        // Configurar nome
        if (nameArea != null)
        {
            TextMeshProUGUI nameText = nameArea.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
            {
                nameText.text = $"<b>{skill.skillName}</b>\n{GetElementIcon(skill.element)} {skill.element}";
                nameText.color = GetElementColor(skill.element);
            }
        }

        // Configurar descrição
        if (descArea != null)
        {
            TextMeshProUGUI descText = descArea.GetComponent<TextMeshProUGUI>();
            if (descText != null)
            {
                descText.text = skill.description;
            }
        }

        // Configurar status
        if (statsArea != null)
        {
            TextMeshProUGUI statsText = statsArea.GetComponent<TextMeshProUGUI>();
            if (statsText != null)
            {
                statsText.text = GetSkillStatsText(skill);
            }
        }
    }

    private string GetSkillStatsText(SkillData skill)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        if (skill.healthBonus != 0) sb.Append($"❤️{skill.healthBonus} ");
        if (skill.attackBonus != 0) sb.Append($"⚔️{skill.attackBonus} ");
        if (skill.defenseBonus != 0) sb.Append($"🛡️{skill.defenseBonus} ");
        if (skill.speedBonus != 0) sb.Append($"🏃{skill.speedBonus} ");
        if (skill.healthRegenBonus != 0) sb.Append($"💚{skill.healthRegenBonus} ");

        if (sb.Length == 0) sb.Append("💎 Bônus Passivo");

        return sb.ToString();
    }

    private void OnSkillSelected(SkillData selectedSkill)
    {
        Debug.Log($"🎯 Skill selecionada: {selectedSkill.skillName}");

        if (selectedSkill != null)
        {
            StartCoroutine(SelectionConfirmationEffect(selectedSkill));
        }
        else
        {
            Debug.LogError("❌ Skill selecionada é null!");
            ClosePanel();
        }
    }

    private IEnumerator SelectionConfirmationEffect(SkillData selectedSkill)
    {
        foreach (var button in currentButtons)
        {
            if (button != null)
            {
                Button btn = button.GetComponent<Button>();
                if (btn != null) btn.interactable = false;
            }
        }

        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        onSkillChosen?.Invoke(selectedSkill);

        elapsed = 0f;
        while (elapsed < autoCloseDelay)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        ClosePanel();
    }

    public void ClosePanel()
    {
        if (choicePanel != null)
        {
            choicePanel.SetActive(false);
            Debug.Log("🔒 Painel de escolha de skill fechado");
        }

        ClearSkillButtons();
        ResumeGame();
        currentChoices = null;
        onSkillChosen = null;
    }

    [ContextMenu("🎯 Forçar Aparecimento da Escolha")]
    public void ForceShowChoice()
    {
        Debug.Log("🎯 Forçando aparecimento da escolha de skills...");

        CheckAndFixConfiguration();

        List<SkillData> testSkills = CreateTestSkills();

        ShowSkillChoice(testSkills, (selectedSkill) => {
            Debug.Log($"✅ Skill selecionada: {selectedSkill.skillName}");

            if (SkillManager.Instance != null)
            {
                SkillManager.Instance.AddSkill(selectedSkill);
            }

            // 🆕 ATUALIZAR UI APÓS ESCOLHA
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateSkillIcons();
                UIManager.Instance.ForceRefreshUI();
            }
        });
    }

    [ContextMenu("🔍 Verificar Sistema de Cards")]
    public void CheckCardSystem()
    {
        Debug.Log("🔍 Verificando sistema de cards...");

        int totalTemplates = 0;
        if (defaultCardTemplate != null) totalTemplates++;
        if (fireCardTemplate != null) totalTemplates++;
        if (iceCardTemplate != null) totalTemplates++;
        if (lightningCardTemplate != null) totalTemplates++;
        if (poisonCardTemplate != null) totalTemplates++;
        if (earthCardTemplate != null) totalTemplates++;
        if (windCardTemplate != null) totalTemplates++;

        Debug.Log($"📊 Templates carregados: {totalTemplates}/7");
        Debug.Log($"🎯 Prefab principal: {(skillChoicePrefab != null ? "✅" : "❌")}");
        Debug.Log($"📦 Container: {(skillsContainer != null ? "✅" : "❌")}");
    }

    private List<SkillData> CreateTestSkills()
    {
        List<SkillData> testSkills = new List<SkillData>();

        // Skill de teste 1 - Projétil
        SkillData testSkill1 = ScriptableObject.CreateInstance<SkillData>();
        testSkill1.skillName = "Projétil de Fogo";
        testSkill1.description = "Dispara projéteis de fogo que queimam inimigos";
        testSkill1.attackBonus = 15f;
        testSkill1.healthBonus = 10f;
        testSkill1.element = PlayerStats.Element.Fire;
        testSkill1.specificType = SpecificSkillType.Projectile;
        testSkills.Add(testSkill1);

        // Skill de teste 2 - Regeneração
        SkillData testSkill2 = ScriptableObject.CreateInstance<SkillData>();
        testSkill2.skillName = "Regeneração";
        testSkill2.description = "Regenera vida gradualmente durante a batalha";
        testSkill2.healthBonus = 20f;
        testSkill2.healthRegenBonus = 2f;
        testSkill2.specificType = SpecificSkillType.HealthRegen;
        testSkills.Add(testSkill2);

        // Skill de teste 3 - Velocidade
        SkillData testSkill3 = ScriptableObject.CreateInstance<SkillData>();
        testSkill3.skillName = "Velocidade do Vento";
        testSkill3.description = "Aumenta a velocidade de movimento e ataque";
        testSkill3.speedBonus = 2f;
        testSkill3.attackSpeedMultiplier = 0.8f;
        testSkill3.element = PlayerStats.Element.Wind;
        testSkills.Add(testSkill3);

        return testSkills;
    }

    private Color GetElementColor(PlayerStats.Element element)
    {
        switch (element)
        {
            case PlayerStats.Element.Fire: return Color.red;
            case PlayerStats.Element.Poison: return Color.blue;
            case PlayerStats.Element.Earth: return Color.green;
            case PlayerStats.Element.Wind: return Color.cyan;
            case PlayerStats.Element.Lightning: return Color.yellow;
            default: return Color.white;
        }
    }

    private string GetElementIcon(PlayerStats.Element element)
    {
        switch (element)
        {
            case PlayerStats.Element.Fire: return "🔥";
            case PlayerStats.Element.Poison: return "💧";
            case PlayerStats.Element.Earth: return "🌿";
            case PlayerStats.Element.Wind: return "💨";
            case PlayerStats.Element.Lightning: return "⚡";
            default: return "✨";
        }
    }

    void OnDestroy()
    {
        ResumeGame();
    }
}