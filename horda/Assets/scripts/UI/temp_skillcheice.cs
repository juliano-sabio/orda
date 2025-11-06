using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

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

        // Selecionar o Canvas criado
        Selection.activeGameObject = canvasGO;

        Debug.Log("✅ Skill Choice UI criada com sucesso!");
    }

    private static void CreateChoicePanel(GameObject parent, SkillChoiceUI skillChoiceUI)
    {
        // Painel principal
        GameObject panel = CreatePanel(parent, "ChoicePanel", new Vector2(800, 600));
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
        GameObject title = CreateText(panel, "TitleText", new Vector2(0, 250), new Vector2(700, 60));
        Text titleText = title.GetComponent<Text>();
        titleText.text = "🎯 ESCOLHA UMA SKILL";
        titleText.color = Color.yellow;
        titleText.fontSize = 32;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;

        // Container para os botões de skill
        GameObject container = new GameObject("SkillsContainer", typeof(RectTransform));
        container.transform.SetParent(panel.transform);

        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(700, 400);
        containerRect.anchoredPosition = new Vector2(0, 0);
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);

        // Botão de confirmar (opcional - para fechar manualmente)
        GameObject confirmButton = CreateButton(panel, "ConfirmButton", new Vector2(0, -260), new Vector2(200, 50));
        confirmButton.GetComponentInChildren<Text>().text = "Fechar";
        confirmButton.GetComponent<Button>().onClick.AddListener(() => skillChoiceUI.ClosePanel());

        // Conectar referências ao SkillChoiceUI
        skillChoiceUI.choicePanel = panel;
        skillChoiceUI.titleText = titleText;
        skillChoiceUI.skillsContainer = container.transform;
        skillChoiceUI.confirmButton = confirmButton.GetComponent<Button>();
    }

    private static void CreateSkillChoicePrefab(GameObject parent, SkillChoiceUI skillChoiceUI)
    {
        // Criar o prefab do botão de escolha de skill
        GameObject prefab = new GameObject("SkillChoicePrefab", typeof(RectTransform), typeof(Image), typeof(Button));
        prefab.transform.SetParent(parent.transform);
        prefab.SetActive(false); // Prefab inicia oculto

        RectTransform rect = prefab.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(650, 100);

        // Background do botão
        Image bg = prefab.GetComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.3f, 1f);
        bg.type = Image.Type.Sliced;

        // Efeitos de hover do botão
        Button button = prefab.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.3f, 1f);
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.4f, 1f);
        colors.pressedColor = new Color(0.4f, 0.4f, 0.6f, 1f);
        colors.selectedColor = new Color(0.3f, 0.3f, 0.5f, 1f);
        button.colors = colors;

        // Texto do botão
        GameObject textGO = CreateText(prefab, "SkillText", Vector2.zero, new Vector2(630, 90));
        Text textComponent = textGO.GetComponent<Text>();
        textComponent.text = "Nome da Skill\n" +
                           "Elemento: None\n" +
                           "Descrição da skill aparecerá aqui...";
        textComponent.color = Color.white;
        textComponent.fontSize = 14;
        textComponent.alignment = TextAnchor.MiddleLeft;
        textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        textComponent.verticalOverflow = VerticalWrapMode.Overflow;
        textComponent.fontStyle = FontStyle.Bold;

        // Ícone de elemento (placeholder)
        GameObject elementIcon = new GameObject("ElementIcon", typeof(Image));
        elementIcon.transform.SetParent(prefab.transform);
        RectTransform iconRect = elementIcon.GetComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(30, 30);
        iconRect.anchoredPosition = new Vector2(-300, 0);
        elementIcon.GetComponent<Image>().color = Color.white;

        // Borda de seleção (aparece quando selecionado)
        GameObject selectionBorder = CreatePanel(prefab, "SelectionBorder", new Vector2(660, 110));
        selectionBorder.GetComponent<Image>().color = new Color(1, 1, 0, 0.3f);
        selectionBorder.SetActive(false);

        // Conectar ao SkillChoiceUI
        skillChoiceUI.skillChoicePrefab = prefab;

        // Salvar como prefab na pasta Resources
        SaveAsPrefab(prefab, "SkillChoiceButton");
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
        Debug.Log($"✅ Prefab salvo em: {localPath}");

        // Destrói a instância original (já temos o prefab)
        DestroyImmediate(gameObject);
    }

    // Métodos auxiliares (os mesmos do anterior)
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
        GameObject textGO = CreateText(button, "Text", Vector2.zero, size);
        Text textComponent = textGO.GetComponent<Text>();
        textComponent.text = "Botão";
        textComponent.color = Color.white;
        textComponent.fontSize = 16;
        textComponent.fontStyle = FontStyle.Bold;

        return button;
    }
}