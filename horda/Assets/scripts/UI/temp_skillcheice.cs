using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.IO;

public class SkillChoiceUICreator : EditorWindow
{
    [MenuItem("Tools/Skill System/Create Skill Choice UI")]
    public static void CreateSkillChoiceUI()
    {
        // Criar Canvas principal para Skill Choice
        GameObject canvasGO = new GameObject("SkillChoice_Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Adicionar o SkillChoiceUI component
        SkillChoiceUI skillChoiceUI = canvasGO.AddComponent<SkillChoiceUI>();

        // Criar todos os elementos da UI
        CreateChoicePanel(canvasGO, skillChoiceUI);

        // 🎯 CRIA O PREFAB FALLBACK SEM CONTROLLER
        CreateAutoCardPrefab(skillChoiceUI);

        // Configurar layout horizontal
        SetupHorizontalLayout(skillChoiceUI);

        // Selecionar o Canvas criado
        Selection.activeGameObject = canvasGO;

        Debug.Log("✅ Skill Choice UI criada com sistema de instâncias!");
    }

    private static void CreateChoicePanel(GameObject parent, SkillChoiceUI skillChoiceUI)
    {
        // Painel principal
        GameObject panel = CreatePanel(parent, "ChoicePanel", new Vector2(1300, 700));
        panel.SetActive(false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        // Background semi-transparente escuro
        Image bg = panel.GetComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.95f);

        // Título
        GameObject title = CreateTextTMP(panel, "TitleText", new Vector2(0, 280), new Vector2(1000, 80));
        TextMeshProUGUI titleText = title.GetComponent<TextMeshProUGUI>();
        titleText.text = "🎯 ESCOLHA UMA SKILL";
        titleText.color = Color.yellow;
        titleText.fontSize = 36;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;

        // 🎯 CONTAINER ATUALIZADO: SEM ContentSizeFitter
        GameObject container = new GameObject("SkillsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        container.transform.SetParent(panel.transform);

        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(1200, 500); // 🔥 TAMANHO FIXO
        containerRect.anchoredPosition = new Vector2(0, -30);
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);

        // 🎯 CONFIGURAÇÃO ATUALIZADA: Layout Group Destravado
        HorizontalLayoutGroup layout = container.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(30, 30, 20, 20);
        layout.spacing = 30f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false; // 🔥 DESTRAVADO
        layout.childControlHeight = false; // 🔥 DESTRAVADO
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        // Botão de confirmar
        GameObject confirmButton = CreateButton(panel, "ConfirmButton", new Vector2(0, -280), new Vector2(200, 60));
        confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "CONFIRMAR";
        confirmButton.GetComponent<Button>().onClick.AddListener(() => skillChoiceUI.ClosePanel());

        // Conectar referências ao SkillChoiceUI
        skillChoiceUI.choicePanel = panel;
        skillChoiceUI.titleTextTMP = titleText;
        skillChoiceUI.skillsContainer = container.transform;
        skillChoiceUI.confirmButton = confirmButton.GetComponent<Button>();
    }

    // 🎯 ATUALIZADO: Criar prefab FALLBACK SEM CONTROLLER
    private static void CreateAutoCardPrefab(SkillChoiceUI skillChoiceUI)
    {
        // Criar o prefab do card SEM CONTROLLER - só componentes básicos
        GameObject prefab = new GameObject("SkillCard_Auto",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement));
        // 🚫 NÃO adiciona SkillCardController!

        prefab.SetActive(false);

        // Configuração básica do card
        SetupCardComponents(prefab);

        // 🎯 CRIAR ESTRUTURA COMPLETA
        CreateAutoCardStructure(prefab);

        // Conectar ao SkillChoiceUI (APENAS COMO FALLBACK)
        skillChoiceUI.skillChoicePrefab = prefab;

        // Salvar como prefab
        SaveAutoCardPrefab(prefab, "SkillCard_Auto");

        Debug.Log("✅ Prefab fallback criado SEM Controller! Agora use o sistema de instâncias.");
    }

    private static void SetupCardComponents(GameObject card)
    {
        RectTransform rect = card.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 450);

        // Background do card
        Image bg = card.GetComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.25f, 1f);
        bg.type = Image.Type.Sliced;

        // Configurar Button
        Button button = card.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.15f, 0.15f, 0.25f, 1f);
        colors.highlightedColor = new Color(0.25f, 0.25f, 0.35f, 1f);
        colors.pressedColor = new Color(0.35f, 0.35f, 0.55f, 1f);
        colors.selectedColor = new Color(0.25f, 0.25f, 0.45f, 1f);
        button.colors = colors;

        // Layout Element
        LayoutElement layout = card.GetComponent<LayoutElement>();
        layout.preferredWidth = 300;
        layout.preferredHeight = 450;
        layout.flexibleWidth = 0;
        layout.flexibleHeight = 0;
    }

    private static void CreateAutoCardStructure(GameObject card)
    {
        // 🎯 ÁREA DO ÍCONE
        GameObject iconArea = CreateCardSection(card, "IconArea",
            new Vector2(0f, 0.75f), new Vector2(1f, 1f),
            new Vector2(15, 15), new Color(0.3f, 0.3f, 0.4f, 1f));

        // Slot da imagem do ícone
        GameObject iconSlot = CreateImageSlot(iconArea, "IconImageSlot",
            new Vector2(80, 80), Color.white);

        // 🎯 ÁREA DO NOME
        GameObject nameArea = CreateCardSection(card, "NameArea",
            new Vector2(0f, 0.6f), new Vector2(1f, 0.75f),
            new Vector2(10, 5), Color.clear);

        GameObject nameText = CreateTextElement(nameArea, "NameText",
            "<b>NOME DA SKILL</b>", 18, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);

        // 🎯 ÁREA DO ELEMENTO
        GameObject elementArea = CreateCardSection(card, "ElementArea",
            new Vector2(0f, 0.5f), new Vector2(1f, 0.6f),
            new Vector2(10, 2), Color.clear);

        GameObject elementText = CreateTextElement(elementArea, "ElementText",
            "🔥 Fire", 14, new Color(1f, 0.5f, 0.2f), TextAlignmentOptions.Center, FontStyles.Normal);

        // 🎯 ÁREA DA DESCRIÇÃO
        GameObject descArea = CreateCardSection(card, "DescArea",
            new Vector2(0f, 0.2f), new Vector2(1f, 0.5f),
            new Vector2(15, 10), Color.clear);

        GameObject descText = CreateTextElement(descArea, "DescText",
            "Descrição completa da skill aparecerá aqui automaticamente...",
            12, new Color(0.9f, 0.9f, 0.9f), TextAlignmentOptions.Center, FontStyles.Normal);

        // 🎯 ÁREA DE STATUS
        GameObject statsArea = CreateCardSection(card, "StatsArea",
            new Vector2(0f, 0.1f), new Vector2(1f, 0.2f),
            new Vector2(10, 5), Color.clear);

        GameObject statsText = CreateTextElement(statsArea, "StatsText",
            "❤️ +10 ⚔️ +5 🛡️ +3 🏃 +2",
            11, new Color(1f, 0.8f, 0.3f), TextAlignmentOptions.Center, FontStyles.Bold);

        // 🎯 ÁREA DE RARIDADE
        GameObject rarityArea = CreateCardSection(card, "RarityArea",
            new Vector2(0.7f, 0.65f), new Vector2(0.95f, 0.7f),
            new Vector2(2, 2), Color.clear);

        GameObject rarityText = CreateTextElement(rarityArea, "RarityText",
            "RARE", 10, Color.yellow, TextAlignmentOptions.Center, FontStyles.Bold);

        // 🎯 BORDA DE RARIDADE
        GameObject rarityBorder = CreateCardSection(card, "RarityBorder",
            new Vector2(0.68f, 0.63f), new Vector2(0.97f, 0.72f),
            new Vector2(0, 0), new Color(1f, 0.5f, 0f, 0.3f));
        rarityBorder.transform.SetAsFirstSibling();

        // Borda de seleção
        CreateSelectionBorder(card);

        Debug.Log("✅ Estrutura do card criada SEM Controller!");
    }

    private static GameObject CreateCardSection(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offset, Color color)
    {
        GameObject section = new GameObject(name, typeof(RectTransform));
        section.transform.SetParent(parent.transform);

        RectTransform rect = section.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = new Vector2(offset.x, offset.y);
        rect.offsetMax = new Vector2(-offset.x, -offset.y);
        rect.anchoredPosition = Vector2.zero;

        if (color != Color.clear)
        {
            Image bg = section.AddComponent<Image>();
            bg.color = color;
        }

        return section;
    }

    private static GameObject CreateImageSlot(GameObject parent, string name, Vector2 size, Color color)
    {
        GameObject imageSlot = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageSlot.transform.SetParent(parent.transform);

        RectTransform rect = imageSlot.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        Image image = imageSlot.GetComponent<Image>();
        image.color = color;

        return imageSlot;
    }

    private static GameObject CreateTextElement(GameObject parent, string name, string text, int fontSize, Color color, TextAlignmentOptions alignment, FontStyles fontStyle)
    {
        GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(parent.transform);

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI textComp = textObj.GetComponent<TextMeshProUGUI>();
        textComp.text = text;
        textComp.color = color;
        textComp.fontSize = fontSize;
        textComp.alignment = alignment;
        textComp.fontStyle = fontStyle;
        textComp.textWrappingMode = TextWrappingModes.Normal;

        return textObj;
    }

    private static void CreateSelectionBorder(GameObject card)
    {
        GameObject border = new GameObject("SelectionBorder", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(card.transform);
        border.transform.SetAsFirstSibling();

        RectTransform rect = border.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(-5, -5);
        rect.offsetMax = new Vector2(5, 5);

        Image borderImage = border.GetComponent<Image>();
        borderImage.color = new Color(1f, 0.9f, 0.2f, 0.4f);
        borderImage.type = Image.Type.Sliced;

        border.SetActive(false);
    }

    private static void SaveAutoCardPrefab(GameObject card, string prefabName)
    {
        // Criar pasta se não existir
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        if (!AssetDatabase.IsValidFolder("Assets/Resources/Cards"))
            AssetDatabase.CreateFolder("Assets/Resources", "Cards");

        // Salvar como prefab
        string prefabPath = $"Assets/Resources/Cards/{prefabName}.prefab";
        prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(card, prefabPath);
        DestroyImmediate(card);

        if (prefab != null)
        {
            Debug.Log($"✅ Prefab fallback salvo: {prefabPath}");
        }
        else
        {
            Debug.LogError($"❌ Erro ao salvar prefab: {prefabPath}");
        }
    }

    private static void SetupHorizontalLayout(SkillChoiceUI skillChoiceUI)
    {
        // Configurar o componente para layout horizontal
        skillChoiceUI.useHorizontalLayout = true;
        skillChoiceUI.cardSize = new Vector2(300, 450);
        skillChoiceUI.cardSpacing = 30f;
        skillChoiceUI.autoCloseDelay = 2f;
        skillChoiceUI.pauseGameDuringChoice = true;
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
        image.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        return panel;
    }

    private static GameObject CreateTextTMP(GameObject parent, string name, Vector2 position, Vector2 size)
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

    private static GameObject CreateButton(GameObject parent, string name, Vector2 position, Vector2 size)
    {
        GameObject button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        button.transform.SetParent(parent.transform);

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image image = button.GetComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.4f, 1f);

        // Texto do botão
        GameObject textGO = CreateTextTMP(button, "Text", Vector2.zero, size);
        TextMeshProUGUI textComponent = textGO.GetComponent<TextMeshProUGUI>();
        textComponent.text = "Botão";
        textComponent.color = Color.white;
        textComponent.fontSize = 16;
        textComponent.fontStyle = FontStyles.Bold;

        return button;
    }

    [MenuItem("Tools/Skill System/🔄 Atualizar para Sistema de Instâncias")]
    public static void UpdateToInstanceSystem()
    {
        SkillChoiceUI existingUI = FindAnyObjectByType<SkillChoiceUI>();

        if (existingUI != null)
        {
            // Atualizar configurações
            existingUI.useHorizontalLayout = true;
            existingUI.cardSize = new Vector2(300, 450);
            existingUI.cardSpacing = 30f;

            // Remover ContentSizeFitter se existir
            if (existingUI.skillsContainer != null)
            {
                ContentSizeFitter fitter = existingUI.skillsContainer.GetComponent<ContentSizeFitter>();
                if (fitter != null)
                {
                    DestroyImmediate(fitter);
                    Debug.Log("✅ ContentSizeFitter removido do container");
                }

                // Configurar Layout Group destravado
                HorizontalLayoutGroup layout = existingUI.skillsContainer.GetComponent<HorizontalLayoutGroup>();
                if (layout != null)
                {
                    layout.childControlWidth = false;
                    layout.childControlHeight = false;
                    Debug.Log("✅ Layout Group destravado");
                }

                // Configurar tamanho fixo do container
                RectTransform rect = existingUI.skillsContainer as RectTransform;
                if (rect != null)
                {
                    rect.sizeDelta = new Vector2(1200, 500);
                    Debug.Log("✅ Container com tamanho fixo configurado");
                }
            }

            Debug.Log("✅ SkillChoiceUI atualizado para sistema de instâncias!");
        }
        else
        {
            Debug.LogWarning("⚠️ Nenhum SkillChoiceUI encontrado na cena. Criando novo...");
            CreateSkillChoiceUI();
        }
    }

    [MenuItem("Tools/Skill System/🎯 Criar Template de Card Limpo")]
    public static void CreateCleanCardTemplate()
    {
        GameObject prefab = new GameObject("SkillCard_CleanTemplate",
            typeof(RectTransform), typeof(Image), typeof(Button),
            typeof(LayoutElement));
        // 🚫 SEM Controller!

        SetupCardComponents(prefab);
        CreateAutoCardStructure(prefab);

        string prefabPath = "Assets/SkillCard_CleanTemplate.prefab";
        prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
        DestroyImmediate(prefab);

        if (savedPrefab != null)
        {
            Selection.activeObject = savedPrefab;
            Debug.Log($"✅ Template de card LIMPO criado: {prefabPath}");
            Debug.Log("🎯 Agora você pode:");
            Debug.Log("1. Customizar este template visualmente");
            Debug.Log("2. Atribuí-lo no campo 'cardPrefab' do seu SkillData");
            Debug.Log("3. O sistema usará SkillCardInstance automaticamente!");
        }
    }

    [MenuItem("Tools/Skill System/🔧 Destravar Container Existente")]
    public static void UnlockExistingContainer()
    {
        SkillChoiceUI ui = FindAnyObjectByType<SkillChoiceUI>();
        if (ui != null)
        {
            ui.UnlockContainerManually();
        }
        else
        {
            Debug.LogWarning("⚠️ Nenhum SkillChoiceUI encontrado na cena.");
        }
    }

    [MenuItem("Tools/Skill System/📊 Verificar Configuração do Sistema")]
    public static void CheckSystemConfiguration()
    {
        SkillChoiceUI ui = FindAnyObjectByType<SkillChoiceUI>();
        if (ui != null)
        {
            ui.CheckConfiguration();
            ui.DebugContainerAndCards();
        }
        else
        {
            Debug.LogWarning("⚠️ Nenhum SkillChoiceUI encontrado na cena.");
        }
    }

    [MenuItem("Tools/Skill System/🧹 Limpar Prefabs de SkillCardController")]
    public static void CleanSkillCardControllers()
    {
        // Encontra todos os prefabs na pasta Cards
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources/Cards" });
        int cleanedCount = 0;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
            {
                // Verifica se tem SkillCardController (pode não existir mais)
                var controllerType = System.Type.GetType("SkillCardController");
                if (controllerType != null)
                {
                    Component controller = prefab.GetComponent(controllerType);
                    if (controller != null)
                    {
                        DestroyImmediate(controller, true);
                        cleanedCount++;
                        Debug.Log($"🧹 Removido SkillCardController de: {prefab.name}");
                    }
                }

                // Salva o prefab limpo
                PrefabUtility.SavePrefabAsset(prefab);
            }
        }

        if (cleanedCount > 0)
        {
            Debug.Log($"✅ {cleanedCount} prefabs limpos! Agora use o sistema de instâncias.");
        }
        else
        {
            Debug.Log("✅ Nenhum SkillCardController encontrado nos prefabs.");
        }

        AssetDatabase.Refresh();
    }
}