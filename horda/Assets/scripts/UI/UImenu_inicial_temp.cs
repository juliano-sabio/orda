using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.EventSystems;

public class MenuManagerCanvasCreator : EditorWindow
{
    private string gameTitle = "MEU JOGO";
    private string playSceneName = "GameScene";

    [MenuItem("Tools/Menu Manager/Create MenuManager Canvas")]
    public static void ShowWindow()
    {
        GetWindow<MenuManagerCanvasCreator>("MenuManager Canvas Creator");
    }

    void OnGUI()
    {
        GUILayout.Label("Criar Canvas para MenuManager", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        gameTitle = EditorGUILayout.TextField("Título do Jogo:", gameTitle);
        playSceneName = EditorGUILayout.TextField("Nome da Cena:", playSceneName);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Este script criará um Canvas completo com todos os componentes que o MenuManager precisa.", MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Criar Canvas Completo", GUILayout.Height(40)))
        {
            CreateCompleteMenuManagerCanvas();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Apenas Verificar MenuManager"))
        {
            CheckMenuManager();
        }
    }

    void CreateCompleteMenuManagerCanvas()
    {
        // Verificar se MenuManager existe
        MenuManager menuManager = FindAnyObjectByType<MenuManager>();
        if (menuManager == null)
        {
            bool createManager = EditorUtility.DisplayDialog("MenuManager Não Encontrado",
                "MenuManager não encontrado na cena. Deseja criar um?", "Sim", "Não");

            if (createManager)
            {
                GameObject managerGO = new GameObject("MenuManager");
                menuManager = managerGO.AddComponent<MenuManager>();
            }
            else
            {
                return;
            }
        }

        // Verificar se já existe Canvas
        Canvas existingCanvas = FindAnyObjectByType<Canvas>();
        if (existingCanvas != null)
        {
            bool shouldReplace = EditorUtility.DisplayDialog("Canvas Existente",
                "Já existe um Canvas na cena. Deseja substituí-lo?", "Sim", "Não");

            if (shouldReplace)
            {
                DestroyImmediate(existingCanvas.gameObject);
            }
            else
            {
                return;
            }
        }

        // Criar Canvas e todos os componentes
        GameObject canvasGO = CreateCanvasWithMenuComponents();

        // Configurar as referências no MenuManager
        ConfigureMenuManagerReferences(menuManager, canvasGO);

        // Forçar o salvamento das mudanças
        EditorUtility.SetDirty(menuManager);

        Debug.Log("✅ Canvas do MenuManager criado com sucesso!");
        Debug.Log("📍 Todas as referências foram conectadas automaticamente");

        // Selecionar o Canvas criado
        Selection.activeGameObject = canvasGO;
    }

    GameObject CreateCanvasWithMenuComponents()
    {
        // Criar Canvas
        GameObject canvasGO = new GameObject("Menu_Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Configurar CanvasScaler (igual ao seu exemplo)
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Criar EventSystem se não existir
        CreateEventSystem();

        // Criar MainMenuPanel
        GameObject mainMenuPanel = CreateMainMenuPanel(canvasGO);

        // Criar OptionsPanel
        GameObject optionsPanel = CreateOptionsPanel(canvasGO);

        return canvasGO;
    }

    GameObject CreateMainMenuPanel(GameObject parent)
    {
        GameObject panel = CreatePanel(parent, "MainMenuPanel", new Vector2(600, 500));

        // Título do jogo
        GameObject title = CreateText(panel, "Title", new Vector2(0, 150), new Vector2(500, 80));
        Text titleText = title.GetComponent<Text>();
        titleText.text = gameTitle;
        titleText.color = Color.yellow;
        titleText.fontSize = 42;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;

        // Botão Play
        GameObject playButton = CreateMenuButton(panel, "PlayButton", "🎮 JOGAR", new Vector2(0, 50));

        // Botão Opções
        GameObject optionsButton = CreateMenuButton(panel, "OptionsButton", "⚙️ OPÇÕES", new Vector2(0, -50));

        // Botão Sair
        GameObject exitButton = CreateMenuButton(panel, "ExitButton", "🚪 SAIR", new Vector2(0, -150));

        return panel;
    }

    GameObject CreateOptionsPanel(GameObject parent)
    {
        GameObject panel = CreatePanel(parent, "OptionsPanel", new Vector2(600, 500));
        panel.SetActive(false); // Inicia oculto

        // Título das opções
        GameObject title = CreateText(panel, "Title", new Vector2(0, 180), new Vector2(500, 60));
        Text titleText = title.GetComponent<Text>();
        titleText.text = "⚙️ CONFIGURAÇÕES";
        titleText.color = Color.white;
        titleText.fontSize = 36;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;

        // Volume
        GameObject volumeText = CreateText(panel, "VolumeText", new Vector2(-150, 80), new Vector2(200, 30));
        volumeText.GetComponent<Text>().text = "🔊 VOLUME";
        volumeText.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        volumeText.GetComponent<Text>().fontSize = 18;

        GameObject volumeSlider = CreateSlider(panel, "VolumeSlider", new Vector2(50, 80), new Vector2(200, 20));

        // Tela Cheia
        GameObject fullscreenText = CreateText(panel, "FullscreenText", new Vector2(-150, 20), new Vector2(200, 30));
        fullscreenText.GetComponent<Text>().text = "🖥️ TELA CHEIA";
        fullscreenText.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        fullscreenText.GetComponent<Text>().fontSize = 18;

        GameObject fullscreenToggle = CreateToggle(panel, "FullscreenToggle", new Vector2(50, 20));

        // Botão Voltar
        GameObject backButton = CreateMenuButton(panel, "BackButton", "↩️ VOLTAR", new Vector2(0, -150));

        return panel;
    }

    GameObject CreateMenuButton(GameObject parent, string name, string text, Vector2 position)
    {
        GameObject button = CreateButton(parent, name, new Vector2(300, 70));
        button.GetComponent<RectTransform>().anchoredPosition = position;

        // Configurar texto do botão
        Text buttonText = button.transform.Find("Text").GetComponent<Text>();
        buttonText.text = text;
        buttonText.fontSize = 20;
        buttonText.color = Color.white;

        // Configurar cores do botão
        Button buttonComponent = button.GetComponent<Button>();
        ColorBlock colors = buttonComponent.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.4f, 1f);
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.6f, 1f);
        colors.pressedColor = new Color(0.1f, 0.1f, 0.3f, 1f);
        colors.selectedColor = new Color(0.25f, 0.25f, 0.5f, 1f);
        buttonComponent.colors = colors;

        return button;
    }

    void ConfigureMenuManagerReferences(MenuManager menuManager, GameObject canvasGO)
    {
        // Encontrar todos os componentes no Canvas
        Transform mainMenuPanel = canvasGO.transform.Find("MainMenuPanel");
        Transform optionsPanel = canvasGO.transform.Find("OptionsPanel");

        if (mainMenuPanel != null)
        {
            // Configurar referências do menu principal
            menuManager.mainMenuPanel = mainMenuPanel.gameObject;

            Button playButton = mainMenuPanel.Find("PlayButton")?.GetComponent<Button>();
            Button optionsButton = mainMenuPanel.Find("OptionsButton")?.GetComponent<Button>();
            Button exitButton = mainMenuPanel.Find("ExitButton")?.GetComponent<Button>();

            if (playButton != null) menuManager.playButton = playButton;
            if (optionsButton != null) menuManager.optionsButton = optionsButton;
            if (exitButton != null) menuManager.exitButton = exitButton;
        }

        if (optionsPanel != null)
        {
            // Configurar referências do painel de opções
            menuManager.optionsPanel = optionsPanel.gameObject;

            Button backButton = optionsPanel.Find("BackButton")?.GetComponent<Button>();
            Slider volumeSlider = optionsPanel.Find("VolumeSlider")?.GetComponent<Slider>();
            Toggle fullscreenToggle = optionsPanel.Find("FullscreenToggle")?.GetComponent<Toggle>();

            if (backButton != null) menuManager.optionsBackButton = backButton;
            if (volumeSlider != null) menuManager.volumeSlider = volumeSlider;
            if (fullscreenToggle != null) menuManager.fullscreenToggle = fullscreenToggle;
        }

        // Configurar nome da cena
        menuManager.gameSceneName = playSceneName;
    }

    void CheckMenuManager()
    {
        MenuManager menuManager = FindAnyObjectByType<MenuManager>();
        if (menuManager == null)
        {
            EditorUtility.DisplayDialog("MenuManager", "MenuManager não encontrado na cena!", "OK");
            return;
        }

        // Verificar referências
        string missingRefs = "";
        if (menuManager.mainMenuPanel == null) missingRefs += "• MainMenuPanel\n";
        if (menuManager.optionsPanel == null) missingRefs += "• OptionsPanel\n";
        if (menuManager.playButton == null) missingRefs += "• PlayButton\n";
        if (menuManager.optionsButton == null) missingRefs += "• OptionsButton\n";
        if (menuManager.exitButton == null) missingRefs += "• ExitButton\n";
        if (menuManager.optionsBackButton == null) missingRefs += "• OptionsBackButton\n";
        if (menuManager.volumeSlider == null) missingRefs += "• VolumeSlider\n";
        if (menuManager.fullscreenToggle == null) missingRefs += "• FullscreenToggle\n";

        if (string.IsNullOrEmpty(missingRefs))
        {
            EditorUtility.DisplayDialog("MenuManager", "✅ Todas as referências estão configuradas!", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("MenuManager - Referências Faltando",
                "As seguintes referências estão faltando:\n\n" + missingRefs, "OK");
        }
    }

    void CreateEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();
        }
    }

    // Métodos auxiliares para criar UI components
    GameObject CreatePanel(GameObject parent, string name, Vector2 size)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent.transform);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        Image image = panel.GetComponent<Image>();
        image.color = new Color(0.1f, 0.1f, 0.2f, 0.95f);

        return panel;
    }

    GameObject CreateButton(GameObject parent, string name, Vector2 size)
    {
        GameObject button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        button.transform.SetParent(parent.transform);

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.sizeDelta = size;

        Image image = button.GetComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.4f, 1f);

        // Texto do botão
        GameObject textGO = CreateText(button, "Text", Vector2.zero, size);
        Text textComponent = textGO.GetComponent<Text>();
        textComponent.text = name;
        textComponent.color = Color.white;
        textComponent.fontSize = 16;
        textComponent.alignment = TextAnchor.MiddleCenter;

        return button;
    }

    GameObject CreateText(GameObject parent, string name, Vector2 position, Vector2 size)
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

    GameObject CreateSlider(GameObject parent, string name, Vector2 position, Vector2 size)
    {
        GameObject sliderGO = new GameObject(name, typeof(RectTransform), typeof(Slider));
        sliderGO.transform.SetParent(parent.transform);

        RectTransform rect = sliderGO.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Slider slider = sliderGO.GetComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = 1;

        // Background
        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(sliderGO.transform);
        bg.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        bg.GetComponent<RectTransform>().anchorMax = Vector2.one;
        bg.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
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
        fill.GetComponent<Image>().color = new Color(0.2f, 0.6f, 1f, 1f);

        slider.fillRect = fillRect;

        return sliderGO;
    }

    GameObject CreateToggle(GameObject parent, string name, Vector2 position)
    {
        GameObject toggleGO = new GameObject(name, typeof(RectTransform), typeof(Toggle));
        toggleGO.transform.SetParent(parent.transform);

        RectTransform rect = toggleGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(30, 30);
        rect.anchoredPosition = position;

        Toggle toggle = toggleGO.GetComponent<Toggle>();
        toggle.isOn = Screen.fullScreen;

        // Background
        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(toggleGO.transform);
        bg.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        bg.GetComponent<RectTransform>().anchorMax = Vector2.one;
        bg.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 1f);

        // Checkmark
        GameObject checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkmark.transform.SetParent(bg.transform);
        checkmark.GetComponent<RectTransform>().anchorMin = new Vector2(0.2f, 0.2f);
        checkmark.GetComponent<RectTransform>().anchorMax = new Vector2(0.8f, 0.8f);
        checkmark.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        checkmark.GetComponent<Image>().color = new Color(0.2f, 0.8f, 0.2f, 1f);

        toggle.graphic = checkmark.GetComponent<Image>();
        toggle.targetGraphic = bg.GetComponent<Image>();

        return toggleGO;
    }
}