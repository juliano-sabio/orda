using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;

public static class UIPauseCreator
{
    [MenuItem("Tools/UI System/🎮 Criar Sistema de Pause Completo", false, 100)]
    public static void CreateCompletePauseSystem()
    {
        // 1. Criar PauseManager se não existir
        CreatePauseManager();

        // 2. Criar UI do Pause Menu
        CreatePauseMenuUI();

        // 3. Configurar Audio Sources
        SetupAudioSources();

        Debug.Log("✅ Sistema de Pause criado com sucesso!");

        // 4. Mostrar instruções
        ShowInstructions();
    }

    [MenuItem("Tools/UI System/⏸️ Criar Apenas PauseManager", false, 101)]
    public static void CreatePauseManager()
    {
        // Verificar se já existe um PauseManager
        PauseManager existingManager = Object.FindAnyObjectByType<PauseManager>();
        if (existingManager != null)
        {
            Debug.Log("ℹ️ PauseManager já existe na cena.");
            Selection.activeObject = existingManager.gameObject;
            return;
        }

        // Criar novo GameObject para o PauseManager
        GameObject pauseManagerGO = new GameObject("PauseManager");
        PauseManager pauseManager = pauseManagerGO.AddComponent<PauseManager>();

        // Adicionar Audio Sources
        AudioSource pauseSound = pauseManagerGO.AddComponent<AudioSource>();
        AudioSource unpauseSound = pauseManagerGO.AddComponent<AudioSource>();
        AudioSource buttonClickSound = pauseManagerGO.AddComponent<AudioSource>();

        // Configurar Audio Sources
        pauseSound.playOnAwake = false;
        unpauseSound.playOnAwake = false;
        buttonClickSound.playOnAwake = false;

        pauseManager.pauseSound = pauseSound;
        pauseManager.unpauseSound = unpauseSound;
        pauseManager.buttonClickSound = buttonClickSound;

        Debug.Log("✅ PauseManager criado com sucesso!");
        Selection.activeObject = pauseManagerGO;
    }

    [MenuItem("Tools/UI System/🎨 Criar UI do Pause Menu", false, 102)]
    public static void CreatePauseMenuUI()
    {
        // Encontrar ou criar Canvas
        Canvas canvas = FindOrCreateCanvas();

        // Criar Painel de Pause
        GameObject pausePanel = CreatePausePanel(canvas.transform);

        // Criar Painel de Configurações
        GameObject settingsPanel = CreateSettingsPanel(canvas.transform);

        // Configurar referências no PauseManager
        SetupPauseManagerReferences(pausePanel, settingsPanel);

        Debug.Log("✅ UI do Pause Menu criada com sucesso!");
    }

    private static Canvas FindOrCreateCanvas()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();

        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            // Configurar Canvas Scaler
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Debug.Log("✅ Canvas criado automaticamente");
        }

        return canvas;
    }

    private static GameObject CreatePausePanel(Transform parent)
    {
        // Criar Painel Principal
        GameObject pausePanel = CreateUIObject("PausePanel", parent);
        RectTransform pauseRect = pausePanel.GetComponent<RectTransform>();
        pauseRect.anchorMin = Vector2.zero;
        pauseRect.anchorMax = Vector2.one;
        pauseRect.sizeDelta = Vector2.zero;

        // Adicionar Background
        Image bg = pausePanel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.8f);

        // Adicionar Canvas Group para fade
        CanvasGroup canvasGroup = pausePanel.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        // Criar Título
        GameObject title = CreateText("Title", pausePanel.transform, "JOGO PAUSADO", 48, TextAlignmentOptions.Center);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.7f);
        titleRect.anchorMax = new Vector2(0.5f, 0.7f);
        titleRect.anchoredPosition = Vector2.zero;
        titleRect.sizeDelta = new Vector2(400, 80);

        // Criar Botões
        CreatePauseButton("ResumeButton", pausePanel.transform, "CONTINUAR", new Vector2(0f, 50f));
        CreatePauseButton("SettingsButton", pausePanel.transform, "CONFIGURAÇÕES", new Vector2(0f, -30f));
        CreatePauseButton("ExitButton", pausePanel.transform, "SAIR PARA MENU", new Vector2(0f, -110f));

        pausePanel.SetActive(false);
        return pausePanel;
    }

    private static GameObject CreateSettingsPanel(Transform parent)
    {
        // Criar Painel de Configurações
        GameObject settingsPanel = CreateUIObject("SettingsPanel", parent);
        RectTransform settingsRect = settingsPanel.GetComponent<RectTransform>();
        settingsRect.anchorMin = Vector2.zero;
        settingsRect.anchorMax = Vector2.one;
        settingsRect.sizeDelta = Vector2.zero;

        // Adicionar Background
        Image bg = settingsPanel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        // Título
        GameObject title = CreateText("SettingsTitle", settingsPanel.transform, "CONFIGURAÇÕES", 36, TextAlignmentOptions.Center);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.8f);
        titleRect.anchorMax = new Vector2(0.5f, 0.8f);
        titleRect.anchoredPosition = Vector2.zero;
        titleRect.sizeDelta = new Vector2(400, 60);

        // Sliders e Toggles - CORRIGIDO
        CreateSliderWithLabel("MusicSlider", settingsPanel.transform, "Volume Música", new Vector2(0f, 100f));
        CreateSliderWithLabel("SFXSlider", settingsPanel.transform, "Volume SFX", new Vector2(0f, 0f));
        CreateToggleWithLabel("FullscreenToggle", settingsPanel.transform, "Tela Cheia", new Vector2(0f, -100f));

        // Botão Voltar
        CreatePauseButton("BackButton", settingsPanel.transform, "VOLTAR", new Vector2(0f, -200f));

        settingsPanel.SetActive(false);
        return settingsPanel;
    }

    private static GameObject CreatePauseButton(string name, Transform parent, string text, Vector2 position)
    {
        GameObject buttonGO = CreateUIObject(name, parent);
        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(300, 60);

        // Image do Botão
        Image image = buttonGO.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        // Botão
        Button button = buttonGO.AddComponent<Button>();

        // Texto do Botão
        GameObject textGO = CreateText("Text", buttonGO.transform, text, 24, TextAlignmentOptions.Center);
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        return buttonGO;
    }

    // 🆕 MÉTODO CORRIGIDO PARA CRIAR SLIDER
    private static GameObject CreateSliderWithLabel(string name, Transform parent, string label, Vector2 position)
    {
        // Criar container para o slider e label
        GameObject container = CreateUIObject(name, parent);
        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = position;
        containerRect.sizeDelta = new Vector2(400, 60);

        // Criar Label
        GameObject labelGO = CreateText("Label", container.transform, label, 20, TextAlignmentOptions.Left);
        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0.4f, 0.5f);
        labelRect.sizeDelta = Vector2.zero;
        labelRect.anchoredPosition = Vector2.zero;

        // Criar Slider
        GameObject sliderGO = CreateUIObject("Slider", container.transform);
        RectTransform sliderRect = sliderGO.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.4f, 0f);
        sliderRect.anchorMax = new Vector2(1f, 1f);
        sliderRect.sizeDelta = Vector2.zero;
        sliderRect.anchoredPosition = Vector2.zero;

        // Adicionar componente Slider
        Slider slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.8f;

        // Criar Background
        GameObject backgroundGO = CreateUIObject("Background", sliderGO.transform);
        RectTransform backgroundRect = backgroundGO.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.sizeDelta = Vector2.zero;
        Image backgroundImage = backgroundGO.AddComponent<Image>();
        backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 1f);

        // Criar Fill Area
        GameObject fillAreaGO = CreateUIObject("Fill Area", sliderGO.transform);
        RectTransform fillAreaRect = fillAreaGO.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.sizeDelta = Vector2.zero;

        // Criar Fill
        GameObject fillGO = CreateUIObject("Fill", fillAreaGO.transform);
        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        Image fillImage = fillGO.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.6f, 1f, 1f);

        // Criar Handle Slide Area
        GameObject handleAreaGO = CreateUIObject("Handle Slide Area", sliderGO.transform);
        RectTransform handleAreaRect = handleAreaGO.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = new Vector2(0f, 0f);
        handleAreaRect.anchorMax = new Vector2(1f, 1f);
        handleAreaRect.sizeDelta = Vector2.zero;

        // Criar Handle
        GameObject handleGO = CreateUIObject("Handle", handleAreaGO.transform);
        RectTransform handleRect = handleGO.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0f, 0f);
        handleRect.anchorMax = new Vector2(0f, 1f);
        handleRect.sizeDelta = new Vector2(20, 0);
        Image handleImage = handleGO.AddComponent<Image>();
        handleImage.color = Color.white;

        // Configurar referências do Slider
        slider.targetGraphic = handleImage;
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;

        return container;
    }

    // 🆕 MÉTODO CORRIGIDO PARA CRIAR TOGGLE
    private static GameObject CreateToggleWithLabel(string name, Transform parent, string label, Vector2 position)
    {
        // Criar container para o toggle
        GameObject container = CreateUIObject(name, parent);
        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = position;
        containerRect.sizeDelta = new Vector2(200, 30);

        // Criar Toggle
        GameObject toggleGO = CreateUIObject("Toggle", container.transform);
        RectTransform toggleRect = toggleGO.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0f, 0f);
        toggleRect.anchorMax = new Vector2(0.3f, 1f);
        toggleRect.sizeDelta = Vector2.zero;

        Toggle toggle = toggleGO.AddComponent<Toggle>();
        toggle.isOn = true;

        // Criar Background
        GameObject backgroundGO = CreateUIObject("Background", toggleGO.transform);
        RectTransform backgroundRect = backgroundGO.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.sizeDelta = Vector2.zero;
        Image backgroundImage = backgroundGO.AddComponent<Image>();
        backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 1f);

        // Criar Checkmark
        GameObject checkmarkGO = CreateUIObject("Checkmark", backgroundGO.transform);
        RectTransform checkmarkRect = checkmarkGO.GetComponent<RectTransform>();
        checkmarkRect.anchorMin = new Vector2(0.1f, 0.1f);
        checkmarkRect.anchorMax = new Vector2(0.9f, 0.9f);
        checkmarkRect.sizeDelta = Vector2.zero;
        Image checkmarkImage = checkmarkGO.AddComponent<Image>();
        checkmarkImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);

        // Criar Label
        GameObject labelGO = CreateText("Label", container.transform, label, 18, TextAlignmentOptions.Left);
        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.4f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.sizeDelta = Vector2.zero;

        // Configurar referências do Toggle
        toggle.targetGraphic = backgroundImage;
        toggle.graphic = checkmarkImage;

        return container;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static GameObject CreateText(string name, Transform parent, string text, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject textGO = new GameObject(name);
        textGO.transform.SetParent(parent, false);

        TextMeshProUGUI textComponent = textGO.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.alignment = alignment;
        textComponent.color = Color.white;

        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(160, 30);

        return textGO;
    }

    private static void SetupPauseManagerReferences(GameObject pausePanel, GameObject settingsPanel)
    {
        PauseManager pauseManager = Object.FindAnyObjectByType<PauseManager>();
        if (pauseManager == null)
        {
            Debug.LogWarning("⚠️ PauseManager não encontrado para configurar referências.");
            Debug.Log("💡 Execute 'Criar Apenas PauseManager' primeiro.");
            return;
        }

        // Configurar referências
        pauseManager.pausePanel = pausePanel;
        pauseManager.settingsPanel = settingsPanel;

        CanvasGroup canvasGroup = pausePanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            pauseManager.pauseCanvasGroup = canvasGroup;
        }

        // Encontrar botões - CORRIGIDO
        pauseManager.resumeButton = FindButton(pausePanel, "ResumeButton");
        pauseManager.settingsButton = FindButton(pausePanel, "SettingsButton");
        pauseManager.exitButton = FindButton(pausePanel, "ExitButton");
        pauseManager.backButton = FindButton(settingsPanel, "BackButton");
        pauseManager.settingsBackButton = FindButton(settingsPanel, "BackButton");

        // Encontrar sliders e toggles - CORRIGIDO
        pauseManager.musicVolumeSlider = FindSlider(settingsPanel, "MusicSlider/Slider");
        pauseManager.sfxVolumeSlider = FindSlider(settingsPanel, "SFXSlider/Slider");
        pauseManager.fullscreenToggle = FindToggle(settingsPanel, "FullscreenToggle/Toggle");

        if (pauseManager.resumeButton == null) Debug.LogWarning("❌ ResumeButton não encontrado");
        if (pauseManager.settingsButton == null) Debug.LogWarning("❌ SettingsButton não encontrado");
        if (pauseManager.exitButton == null) Debug.LogWarning("❌ ExitButton não encontrado");
        if (pauseManager.musicVolumeSlider == null) Debug.LogWarning("❌ MusicVolumeSlider não encontrado");
        if (pauseManager.sfxVolumeSlider == null) Debug.LogWarning("❌ SFXVolumeSlider não encontrado");
        if (pauseManager.fullscreenToggle == null) Debug.LogWarning("❌ FullscreenToggle não encontrado");

        Debug.Log("✅ Referências do PauseManager configuradas!");
    }

    private static Button FindButton(GameObject parent, string path)
    {
        if (parent == null) return null;

        Transform buttonTransform = parent.transform.Find(path);
        if (buttonTransform != null)
        {
            Button button = buttonTransform.GetComponent<Button>();
            return button;
        }
        return null;
    }

    private static Slider FindSlider(GameObject parent, string path)
    {
        if (parent == null) return null;

        Transform sliderTransform = parent.transform.Find(path);
        if (sliderTransform != null)
        {
            Slider slider = sliderTransform.GetComponent<Slider>();
            return slider;
        }
        return null;
    }

    private static Toggle FindToggle(GameObject parent, string path)
    {
        if (parent == null) return null;

        Transform toggleTransform = parent.transform.Find(path);
        if (toggleTransform != null)
        {
            Toggle toggle = toggleTransform.GetComponent<Toggle>();
            return toggle;
        }
        return null;
    }

    private static void SetupAudioSources()
    {
        Debug.Log("🔊 Audio Sources configurados - Adicione seus AudioClips manualmente");
    }

    private static void ShowInstructions()
    {
        Debug.Log("🎮 INSTRUÇÕES DO SISTEMA DE PAUSE:");
        Debug.Log("1. Pressione ESC para pausar/despausar");
        Debug.Log("2. Configure os AudioClips no PauseManager");
        Debug.Log("3. Defina o nome da cena do menu principal no PauseManager");
        Debug.Log("4. Teste com '⏸️ Testar Sistema de Pause' no UIManager");
    }

    [MenuItem("Tools/UI System/🔧 Corrigir Referências do PauseManager", false, 103)]
    public static void FixPauseManagerReferences()
    {
        PauseManager pauseManager = Object.FindAnyObjectByType<PauseManager>();
        if (pauseManager == null)
        {
            Debug.LogError("❌ PauseManager não encontrado na cena!");
            return;
        }

        // Procurar os painéis automaticamente
        GameObject pausePanel = GameObject.Find("PausePanel");
        GameObject settingsPanel = GameObject.Find("SettingsPanel");

        if (pausePanel != null && settingsPanel != null)
        {
            SetupPauseManagerReferences(pausePanel, settingsPanel);
        }
        else
        {
            Debug.LogError("❌ Painéis de UI não encontrados. Execute 'Criar UI do Pause Menu' primeiro.");
        }
    }

    [MenuItem("Tools/UI System/🗑️ Limpar Sistema de Pause", false, 104)]
    public static void CleanPauseSystem()
    {
        // Limpar objetos do sistema de pause
        GameObject[] objectsToDelete = {
            GameObject.Find("PausePanel"),
            GameObject.Find("SettingsPanel"),
            GameObject.Find("PauseManager")
        };

        foreach (GameObject obj in objectsToDelete)
        {
            if (obj != null)
            {
                Object.DestroyImmediate(obj);
                Debug.Log($"🗑️ Deletado: {obj.name}");
            }
        }

        Debug.Log("✅ Sistema de Pause limpo!");
    }
}