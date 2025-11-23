using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class SkillChoiceUICreator : EditorWindow
{
    [MenuItem("Tools/Skill System/Create Skill Choice UI")]
    public static void CreateSkillChoiceUI()
    {
        // Criar Canvas principal para Skill Choice
        GameObject canvasGO = new GameObject("SkillChoice_Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Alta prioridade

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
        CreateSkillChoicePrefab(canvasGO, skillChoiceUI);

        // Configurar layout horizontal
        SetupHorizontalLayout(skillChoiceUI);

        // Selecionar o Canvas criado
        Selection.activeGameObject = canvasGO;

        Debug.Log("✅ Skill Choice UI criada com sucesso com layout horizontal!");
    }

    private static void CreateChoicePanel(GameObject parent, SkillChoiceUI skillChoiceUI)
    {
        // Painel principal
        GameObject panel = CreatePanel(parent, "ChoicePanel", new Vector2(1000, 500)); // Mais largo para cards horizontais
        panel.SetActive(false); // Inicia oculto

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        // Background semi-transparente escuro
        Image bg = panel.GetComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.95f);

        // Título
        GameObject title = CreateTextTMP(panel, "TitleText", new Vector2(0, 200), new Vector2(900, 60));
        TextMeshProUGUI titleText = title.GetComponent<TextMeshProUGUI>();
        titleText.text = "🎯 ESCOLHA UMA SKILL";
        titleText.color = Color.yellow;
        titleText.fontSize = 32;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;

        // Container para os cards de skill (AGORA COM LAYOUT HORIZONTAL)
        GameObject container = new GameObject("SkillsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        container.transform.SetParent(panel.transform);

        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(900, 300);
        containerRect.anchoredPosition = new Vector2(0, -20);
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);

        // Configurar Horizontal Layout Group
        HorizontalLayoutGroup layout = container.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 10, 10);
        layout.spacing = 25f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        // Configurar Content Size Fitter
        ContentSizeFitter fitter = container.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

        // Botão de confirmar (opcional - para fechar manualmente)
        GameObject confirmButton = CreateButton(panel, "ConfirmButton", new Vector2(0, -200), new Vector2(200, 50));
        confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "Fechar";
        confirmButton.GetComponent<Button>().onClick.AddListener(() => skillChoiceUI.ClosePanel());

        // Conectar referências ao SkillChoiceUI
        skillChoiceUI.choicePanel = panel;
        skillChoiceUI.titleTextTMP = titleText;
        skillChoiceUI.skillsContainer = container.transform;
        skillChoiceUI.confirmButton = confirmButton.GetComponent<Button>();
    }

    private static void CreateSkillChoicePrefab(GameObject parent, SkillChoiceUI skillChoiceUI)
    {
        // Criar o prefab do CARD de escolha de skill (AGORA É UM CARD)
        GameObject prefab = new GameObject("SkillCardPrefab", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        prefab.transform.SetParent(parent.transform);
        prefab.SetActive(false); // Prefab inicia oculto

        RectTransform rect = prefab.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(250, 350); // Tamanho de card

        // Configurar Layout Element para controle de tamanho
        LayoutElement layout = prefab.AddComponent<LayoutElement>();
        layout.preferredWidth = 250;
        layout.preferredHeight = 350;
        layout.flexibleWidth = 0;
        layout.flexibleHeight = 0;

        // Background do card
        Image bg = prefab.GetComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.25f, 1f);
        bg.type = Image.Type.Sliced;

        // Efeitos de hover do botão
        Button button = prefab.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.15f, 0.15f, 0.25f, 1f);
        colors.highlightedColor = new Color(0.25f, 0.25f, 0.35f, 1f);
        colors.pressedColor = new Color(0.35f, 0.35f, 0.55f, 1f);
        colors.selectedColor = new Color(0.25f, 0.25f, 0.45f, 1f);
        button.colors = colors;

        // 🎯 ESTRUTURA DO CARD - ÁREAS ORGANIZADAS

        // Área do Ícone (Topo - 30%)
        GameObject iconArea = CreateCardSection(prefab, "IconArea", new Vector2(0f, 0.7f), new Vector2(1f, 1f), new Color(0.2f, 0.2f, 0.3f));

        // Ícone do elemento
        GameObject elementIcon = new GameObject("ElementIcon", typeof(Image));
        elementIcon.transform.SetParent(iconArea.transform);
        RectTransform iconRect = elementIcon.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(60, 60);
        iconRect.anchoredPosition = Vector2.zero;
        elementIcon.GetComponent<Image>().color = Color.red; // Fogo como padrão

        // Texto do elemento no ícone
        GameObject iconText = CreateTextTMP(iconArea, "IconText", Vector2.zero, new Vector2(60, 60));
        TextMeshProUGUI iconTextComp = iconText.GetComponent<TextMeshProUGUI>();
        iconTextComp.text = "🔥";
        iconTextComp.color = Color.white;
        iconTextComp.fontSize = 24;
        iconTextComp.alignment = TextAlignmentOptions.Center;

        // Área do Nome (20%)
        GameObject nameArea = CreateCardSection(prefab, "NameArea", new Vector2(0f, 0.5f), new Vector2(1f, 0.7f), Color.clear);

        GameObject nameText = CreateTextTMP(nameArea, "NameText", Vector2.zero, Vector2.zero);
        TextMeshProUGUI nameTextComp = nameText.GetComponent<TextMeshProUGUI>();
        nameTextComp.text = "<b>NOME DA SKILL</b>\n⚡ Elemento";
        nameTextComp.color = Color.white;
        nameTextComp.fontSize = 14;
        nameTextComp.alignment = TextAlignmentOptions.Center;
        nameTextComp.fontStyle = FontStyles.Bold;

        // Área da Descrição (40%)
        GameObject descArea = CreateCardSection(prefab, "DescArea", new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.5f), Color.clear);

        GameObject descText = CreateTextTMP(descArea, "DescText", Vector2.zero, Vector2.zero);
        TextMeshProUGUI descTextComp = descText.GetComponent<TextMeshProUGUI>();
        descTextComp.text = "Descrição detalhada da skill aparecerá aqui com múltiplas linhas se necessário.";
        descTextComp.color = new Color(0.8f, 0.8f, 0.9f);
        descTextComp.fontSize = 11;
        descTextComp.alignment = TextAlignmentOptions.Center;
        descTextComp.textWrappingMode = TextWrappingModes.Normal;

        // Área dos Status (10% - Rodapé)
        GameObject statsArea = CreateCardSection(prefab, "StatsArea", new Vector2(0f, 0f), new Vector2(1f, 0.1f), Color.clear);

        GameObject statsText = CreateTextTMP(statsArea, "StatsText", Vector2.zero, Vector2.zero);
        TextMeshProUGUI statsTextComp = statsText.GetComponent<TextMeshProUGUI>();
        statsTextComp.text = "❤️+10 ⚔️+5 🛡️+3";
        statsTextComp.color = new Color(1f, 0.8f, 0.3f);
        statsTextComp.fontSize = 10;
        statsTextComp.alignment = TextAlignmentOptions.Center;

        // Borda de seleção (aparece quando selecionado)
        GameObject selectionBorder = CreatePanel(prefab, "SelectionBorder", new Vector2(260, 360));
        selectionBorder.transform.SetAsFirstSibling();
        selectionBorder.GetComponent<Image>().color = new Color(1, 1, 0, 0.4f);
        selectionBorder.SetActive(false);

        // Conectar ao SkillChoiceUI
        skillChoiceUI.skillChoicePrefab = prefab;

        // Configurar o componente para usar layout horizontal
        skillChoiceUI.useHorizontalLayout = true;
        skillChoiceUI.cardSize = new Vector2(250, 350);
        skillChoiceUI.cardSpacing = 25f;

        // Salvar como prefab na pasta Resources
        SaveAsPrefab(prefab, "SkillCardPrefab");
    }

    private static void SetupHorizontalLayout(SkillChoiceUI skillChoiceUI)
    {
        // Configurar o componente para layout horizontal
        skillChoiceUI.useHorizontalLayout = true;
        skillChoiceUI.cardSize = new Vector2(250, 350);
        skillChoiceUI.cardSpacing = 25f;
        skillChoiceUI.autoCloseDelay = 2f;
        skillChoiceUI.pauseGameDuringChoice = true;
        skillChoiceUI.createFallbackUI = true;
    }

    private static GameObject CreateCardSection(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color backgroundColor)
    {
        GameObject section = new GameObject(name, typeof(RectTransform));
        section.transform.SetParent(parent.transform);

        RectTransform rect = section.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        if (backgroundColor != Color.clear)
        {
            Image image = section.AddComponent<Image>();
            image.color = backgroundColor;
        }

        return section;
    }

    private static void SaveAsPrefab(GameObject gameObject, string prefabName)
    {
        // Cria uma pasta Resources se não existir
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        // Salva como prefab
        string localPath = "Assets/Resources/" + prefabName + ".prefab";
        localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);

        PrefabUtility.SaveAsPrefabAsset(gameObject, localPath);
        Debug.Log($"✅ Prefab de card salvo em: {localPath}");

        // Destrói a instância original (já temos o prefab)
        DestroyImmediate(gameObject);
    }

    // Métodos auxiliares atualizados para TextMeshPro
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

    private static GameObject CreateTextTMP(GameObject parent, string name, Vector2 position, Vector2 size)
    {
        GameObject textGO = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(parent.transform);

        RectTransform rect = textGO.GetComponent<RectTransform>();
        if (size != Vector2.zero)
        {
            rect.sizeDelta = size;
        }
        else
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
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

        // Texto do botão com TextMeshPro
        GameObject textGO = CreateTextTMP(button, "Text", Vector2.zero, size);
        TextMeshProUGUI textComponent = textGO.GetComponent<TextMeshProUGUI>();
        textComponent.text = "Botão";
        textComponent.color = Color.white;
        textComponent.fontSize = 16;
        textComponent.fontStyle = FontStyles.Bold;

        return button;
    }

    [MenuItem("Tools/Skill System/🔄 Atualizar para Cards Horizontais")]
    public static void UpdateToHorizontalCards()
    {
        SkillChoiceUI existingUI = FindAnyObjectByType<SkillChoiceUI>();

        if (existingUI != null)
        {
            // Atualizar configurações para layout horizontal
            existingUI.useHorizontalLayout = true;
            existingUI.cardSize = new Vector2(250, 350);
            existingUI.cardSpacing = 25f;

            // Verificar e adicionar HorizontalLayoutGroup se necessário
            if (existingUI.skillsContainer != null)
            {
                HorizontalLayoutGroup layout = existingUI.skillsContainer.GetComponent<HorizontalLayoutGroup>();
                if (layout == null)
                {
                    layout = existingUI.skillsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
                    layout.spacing = 25f;
                    layout.childAlignment = TextAnchor.MiddleCenter;
                    layout.childControlWidth = true;
                    layout.childControlHeight = true;
                }

                // Adicionar ContentSizeFitter
                ContentSizeFitter fitter = existingUI.skillsContainer.GetComponent<ContentSizeFitter>();
                if (fitter == null)
                {
                    fitter = existingUI.skillsContainer.gameObject.AddComponent<ContentSizeFitter>();
                    fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                    fitter.verticalFit = ContentSizeFitter.FitMode.MinSize;
                }
            }

            Debug.Log("✅ SkillChoiceUI atualizado para cards horizontais!");
        }
        else
        {
            Debug.LogWarning("⚠️ Nenhum SkillChoiceUI encontrado na cena. Criando novo...");
            CreateSkillChoiceUI();
        }
    }
}