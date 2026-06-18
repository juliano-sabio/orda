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

        Selection.activeObject = pauseManagerGO;
    }

    [MenuItem("Tools/UI System/🎨 Criar UI do Pause Menu", false, 102)]
    public static void CreatePauseMenuUI()
    {
        Canvas canvas = FindOrCreateCanvas();

        // Guard: procura PausePanel em QUALQUER canvas da cena (inclusive inativos)
        bool existeAntigo = false;
        foreach (Canvas c in Resources.FindObjectsOfTypeAll<Canvas>())
            if (c.gameObject.scene.IsValid() && c.transform.Find("PausePanel") != null) { existeAntigo = true; break; }

        if (existeAntigo)
        {
            bool prosseguir = EditorUtility.DisplayDialog(
                "Pause UI já existe",
                "PausePanel já existe na cena.\n\nRecriar vai apagar edições manuais.\nUse '💾 Salvar como Prefabs' antes para preservar mudanças.",
                "Recriar mesmo assim", "Cancelar");
            if (!prosseguir) return;
            foreach (Canvas c in Resources.FindObjectsOfTypeAll<Canvas>())
            {
                if (!c.gameObject.scene.IsValid()) continue;
                var pp = c.transform.Find("PausePanel");
                var sp = c.transform.Find("SettingsPanel");
                if (pp != null) Object.DestroyImmediate(pp.gameObject);
                if (sp != null) Object.DestroyImmediate(sp.gameObject);
            }
        }

        // Criar Painel de Pause
        GameObject pausePanel = CreatePausePanel(canvas.transform);

        // Criar Painel de Configurações
        GameObject settingsPanel = CreateSettingsPanel(canvas.transform);

        // Configurar referências no PauseManager
        SetupPauseManagerReferences(pausePanel, settingsPanel);

    }

    private static Canvas FindOrCreateCanvas()
    {
        // Priorizar UIManager_Canvas (canvas principal do jogo)
        foreach (Canvas c in Resources.FindObjectsOfTypeAll<Canvas>())
            if (c.name == "UIManager_Canvas" && c.gameObject.scene.IsValid()) return c;

        // Fallback: qualquer canvas válido na cena
        foreach (Canvas c in Resources.FindObjectsOfTypeAll<Canvas>())
            if (c.gameObject.scene.IsValid()) return c;

        // Criar novo se não existir nenhum
        GameObject canvasGO = new GameObject("Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
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

        // Background dark (preto avermelhado) + borda vermelha
        Image bg = pausePanel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.02f, 0.02f, 0.95f);

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
        CreatePauseButton("SettingsButton", pausePanel.transform, "CONFIGURACOES", new Vector2(0f, -30f));
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

        // Background dark fantasy (preto avermelhado)
        Image bg = settingsPanel.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.03f, 0.03f, 0.97f);

        // Título
        GameObject title = CreateText("SettingsTitle", settingsPanel.transform, "CONFIGURACOES", 36, TextAlignmentOptions.Center);
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

    private static readonly Color corBordaPause = new Color(0.62f, 0.11f, 0.11f);   // vermelho escuro
    private static readonly Color corCorpoPause = new Color(0.11f, 0.07f, 0.07f, 1f); // quase preto

    private static GameObject CreatePauseButton(string name, Transform parent, string text, Vector2 position)
    {
        // container sem Image — irmãos controlam renderização
        GameObject buttonGO = CreateUIObject(name, parent);
        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f,0.5f); rect.anchorMax = new Vector2(0.5f,0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = new Vector2(300,60);

        // irmão 0: borda dourada (atrás)
        GameObject brd = CreateUIObject("Brd", buttonGO.transform);
        RectTransform rbrd = brd.GetComponent<RectTransform>();
        rbrd.anchorMin = Vector2.zero; rbrd.anchorMax = Vector2.one;
        rbrd.offsetMin = new Vector2(-1f,-1f); rbrd.offsetMax = new Vector2(1f,1f);
        brd.AddComponent<Image>().color = new Color(corBordaPause.r,corBordaPause.g,corBordaPause.b,0.80f);

        // irmão 1: corpo escuro (frente)
        GameObject corpo = CreateUIObject("Corpo", buttonGO.transform);
        RectTransform rco = corpo.GetComponent<RectTransform>();
        rco.anchorMin = Vector2.zero; rco.anchorMax = Vector2.one; rco.offsetMin = rco.offsetMax = Vector2.zero;
        Image image = corpo.AddComponent<Image>(); image.color = corCorpoPause;

        // irmão 2: bevel topo
        GameObject topo = CreateUIObject("HiT", buttonGO.transform);
        RectTransform rtopo = topo.GetComponent<RectTransform>();
        rtopo.anchorMin = new Vector2(0f,1f); rtopo.anchorMax = new Vector2(1f,1f);
        rtopo.offsetMin = new Vector2(0f,-2f); rtopo.offsetMax = Vector2.zero;
        topo.AddComponent<Image>().color = new Color(1f,1f,1f,0.12f);

        // irmão 3: sombra base
        GameObject shb = CreateUIObject("ShB", buttonGO.transform);
        RectTransform rshb = shb.GetComponent<RectTransform>();
        rshb.anchorMin = Vector2.zero; rshb.anchorMax = new Vector2(1f,0f);
        rshb.offsetMin = Vector2.zero; rshb.offsetMax = new Vector2(0f,2f);
        shb.AddComponent<Image>().color = new Color(0f,0f,0f,0.50f);

        // irmão 4: acento lateral dourado
        GameObject ac = CreateUIObject("Ac", buttonGO.transform);
        RectTransform rac = ac.GetComponent<RectTransform>();
        rac.anchorMin = Vector2.zero; rac.anchorMax = new Vector2(0f,1f);
        rac.offsetMin = Vector2.zero; rac.offsetMax = new Vector2(4f,0f);
        ac.AddComponent<Image>().color = new Color(corBordaPause.r,corBordaPause.g,corBordaPause.b,0.90f);

        // Button
        Button button = buttonGO.AddComponent<Button>(); button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        Color hov = new Color(0.42f,0.13f,0.13f,1f);
        button.colors = new ColorBlock{normalColor=corCorpoPause,highlightedColor=hov,pressedColor=new Color(0.04f,0.03f,0.08f,1f),selectedColor=corCorpoPause,disabledColor=new Color(0.04f,0.03f,0.08f,0.5f),colorMultiplier=1f,fadeDuration=0.1f};

        // Texto
        GameObject textGO = CreateText("Text", buttonGO.transform, text, 22, TextAlignmentOptions.Center);
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.04f,0f); textRect.anchorMax = Vector2.one; textRect.sizeDelta = Vector2.zero;
        textGO.GetComponent<TextMeshProUGUI>().color = new Color(0.95f,0.95f,0.95f);

        return buttonGO;
    }

    // Slider dark-fantasy: mesma lógica do Slider redesenhado do MenuInicialUI
    private static GameObject CreateSliderWithLabel(string name, Transform parent, string label, Vector2 position)
    {
        GameObject container = CreateUIObject(name, parent);
        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f,0.5f); containerRect.anchorMax = new Vector2(0.5f,0.5f);
        containerRect.anchoredPosition = position; containerRect.sizeDelta = new Vector2(400,50);

        // label à esquerda
        GameObject labelGO = CreateText("Label", container.transform, label, 18, TextAlignmentOptions.Left);
        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f,0f); labelRect.anchorMax = new Vector2(0.38f,1f);
        labelRect.sizeDelta = Vector2.zero; labelRect.anchoredPosition = Vector2.zero;
        labelGO.GetComponent<TextMeshProUGUI>().color = new Color(0.92f,0.90f,0.90f);

        // slider container (irmãos)
        GameObject sliderGO = CreateUIObject("Slider", container.transform);
        RectTransform sliderRect = sliderGO.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.40f,0f); sliderRect.anchorMax = new Vector2(1f,1f);
        sliderRect.sizeDelta = Vector2.zero; sliderRect.anchoredPosition = Vector2.zero;

        // irmão 0: borda
        GameObject brd = CreateUIObject("Brd", sliderGO.transform);
        RectTransform rbrd = brd.GetComponent<RectTransform>();
        rbrd.anchorMin = Vector2.zero; rbrd.anchorMax = Vector2.one;
        rbrd.offsetMin = new Vector2(-1f,-1f); rbrd.offsetMax = new Vector2(1f,1f);
        brd.AddComponent<Image>().color = new Color(corBordaPause.r,corBordaPause.g,corBordaPause.b,0.50f);

        // irmão 1: trilha escura
        GameObject trk = CreateUIObject("Trk", sliderGO.transform);
        RectTransform rtrk = trk.GetComponent<RectTransform>();
        rtrk.anchorMin = Vector2.zero; rtrk.anchorMax = Vector2.one; rtrk.offsetMin = rtrk.offsetMax = Vector2.zero;
        trk.AddComponent<Image>().color = new Color(0.07f,0.04f,0.03f);
        GameObject ist = CreateUIObject("InT", trk.transform);
        RectTransform rist = ist.GetComponent<RectTransform>();
        rist.anchorMin = new Vector2(0f,1f); rist.anchorMax = new Vector2(1f,1f);
        rist.offsetMin = new Vector2(0f,-3f); rist.offsetMax = Vector2.zero;
        ist.AddComponent<Image>().color = new Color(0f,0f,0f,0.65f);

        // irmão 2: fill area
        GameObject fillArea = CreateUIObject("FA", sliderGO.transform);
        RectTransform rfa = fillArea.GetComponent<RectTransform>();
        rfa.anchorMin = Vector2.zero; rfa.anchorMax = Vector2.one; rfa.offsetMin = rfa.offsetMax = Vector2.zero;
        GameObject fi = CreateUIObject("Fi", fillArea.transform);
        RectTransform rfi = fi.GetComponent<RectTransform>();
        rfi.anchorMin = Vector2.zero; rfi.anchorMax = Vector2.one; rfi.offsetMin = rfi.offsetMax = Vector2.zero;
        fi.AddComponent<Image>().color = new Color(0.85f,0.18f,0.15f);
        GameObject fhl = CreateUIObject("FHl", fi.transform);
        RectTransform rfhl = fhl.GetComponent<RectTransform>();
        rfhl.anchorMin = new Vector2(0f,1f); rfhl.anchorMax = new Vector2(1f,1f);
        rfhl.offsetMin = new Vector2(0f,-1f); rfhl.offsetMax = Vector2.zero;
        fhl.AddComponent<Image>().color = new Color(1f,0.55f,0.45f,0.40f);

        // irmão 3: handle area
        GameObject ha = CreateUIObject("HA", sliderGO.transform);
        RectTransform rha = ha.GetComponent<RectTransform>();
        rha.anchorMin = Vector2.zero; rha.anchorMax = Vector2.one; rha.offsetMin = rha.offsetMax = Vector2.zero;
        GameObject h = CreateUIObject("H", ha.transform);
        RectTransform rh = h.GetComponent<RectTransform>();
        rh.anchorMin = new Vector2(0f,0f); rh.anchorMax = new Vector2(0f,1f);
        rh.offsetMin = new Vector2(0f,-3f); rh.offsetMax = new Vector2(12f,3f);
        GameObject kSh = CreateUIObject("KSh", h.transform);
        RectTransform rkSh = kSh.GetComponent<RectTransform>();
        rkSh.anchorMin = Vector2.zero; rkSh.anchorMax = Vector2.one;
        rkSh.offsetMin = new Vector2(-1f,-1f); rkSh.offsetMax = new Vector2(1f,1f);
        kSh.AddComponent<Image>().color = new Color(0f,0f,0f,0.80f);
        GameObject kBd = CreateUIObject("KBd", h.transform);
        RectTransform rkBd = kBd.GetComponent<RectTransform>();
        rkBd.anchorMin = Vector2.zero; rkBd.anchorMax = Vector2.one; rkBd.offsetMin = rkBd.offsetMax = Vector2.zero;
        Image handleImage = kBd.AddComponent<Image>(); handleImage.color = new Color(0.80f,0.16f,0.14f);
        GameObject kHi = CreateUIObject("KHi", h.transform);
        RectTransform rkHi = kHi.GetComponent<RectTransform>();
        rkHi.anchorMin = new Vector2(0f,1f); rkHi.anchorMax = new Vector2(1f,1f);
        rkHi.offsetMin = new Vector2(1f,-2f); rkHi.offsetMax = new Vector2(-1f,0f);
        kHi.AddComponent<Image>().color = new Color(1f,0.70f,0.60f,0.65f);

        Slider slider = sliderGO.AddComponent<Slider>();
        slider.fillRect = rfi; slider.handleRect = rh; slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight; slider.minValue = 0f; slider.maxValue = 1f; slider.value = 0.8f;

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
    }

    private static void ShowInstructions()
    {
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

    [MenuItem("Tools/UI System/💾 Salvar Pause UI como Prefabs", false, 105)]
    public static void SavePauseUIAsPrefabs()
    {
        // Painéis ficam inativos — buscar em todos os canvases da cena
        GameObject pausePanel = null, settingsPanel = null;
        foreach (Canvas c in Resources.FindObjectsOfTypeAll<Canvas>())
        {
            if (!c.gameObject.scene.IsValid()) continue;
            if (pausePanel == null)   { var t = c.transform.Find("PausePanel");   if (t != null) pausePanel   = t.gameObject; }
            if (settingsPanel == null){ var t = c.transform.Find("SettingsPanel"); if (t != null) settingsPanel = t.gameObject; }
        }

        if (pausePanel == null && settingsPanel == null)
        {
            EditorUtility.DisplayDialog("Pause UI",
                "Nenhum painel encontrado na cena.\nExecute 'Criar UI do Pause Menu' primeiro.", "OK");
            return;
        }

        // Garantir que a pasta existe
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");

        int count = 0;
        if (pausePanel != null)
        {
            PrefabUtility.SaveAsPrefabAssetAndConnect(pausePanel, "Assets/Prefabs/UI/PausePanel.prefab", InteractionMode.UserAction);
            count++;
        }
        if (settingsPanel != null)
        {
            PrefabUtility.SaveAsPrefabAssetAndConnect(settingsPanel, "Assets/Prefabs/UI/SettingsPanel.prefab", InteractionMode.UserAction);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Pause UI",
            $"{count} prefab(s) salvos em Assets/Prefabs/UI/\n\nEdite diretamente no .prefab — mudanças refletem na cena.", "OK");
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
            }
        }

    }
}
