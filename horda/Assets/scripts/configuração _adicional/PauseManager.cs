using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    [Header("🎯 Painel de Pause")]
    public GameObject pausePanel;
    public CanvasGroup pauseCanvasGroup;
    public float fadeDuration = 0.3f;

    [Header("🎮 Botões do Menu de Pause")]
    public Button resumeButton;
    public Button settingsButton;
    public Button exitButton;
    public Button backButton;

    [Header("⚙️ Painel de Configurações")]
    public GameObject settingsPanel;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Toggle fullscreenToggle;
    public Button settingsBackButton;

    [Header("🔊 Audio")]
    public AudioSource pauseSound;
    public AudioSource unpauseSound;
    public AudioSource buttonClickSound;

    [Header("🚪 Configurações de Saída")]
    public string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;
    private bool canPause = true;
    private GameObject timerBadge;   // cronômetro (escondido durante o pause)

    void Awake()
    {
        // A instância nova (desta cena) tem as referências de UI corretas
        // para esta cena. Se já existir uma instância persistida de uma
        // cena anterior, ela está com referências inválidas (PausePanel
        // antigo já destruído junto com o UIManager_Canvas anterior) —
        // então a instância antiga é descartada e esta assume.
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }

        Instance = this;

        if (Application.isPlaying)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    void Start()
    {
        // 🆕 PROCURAR REFERÊNCIAS AUTOMATICAMENTE SE NÃO ESTIVEREM ATRIBUÍDAS
        FindUIReferences();
        InitializePauseMenu();
        LoadSettings();
        Loc.OnLanguageChanged += OnLanguageChanged;
    }

    void Update()
    {
        HandlePauseInput();
    }

    // 🆕 MÉTODO PARA PROCURAR REFERÊNCIAS AUTOMATICAMENTE
    private void FindUIReferences()
    {
        // Procurar painéis (GameObject.Find não encontra objetos inativos,
        // e os painéis começam desativados na cena)
        if (pausePanel == null)
            pausePanel = FindInactiveObjectByName("PausePanel");

        if (settingsPanel == null)
            settingsPanel = FindInactiveObjectByName("SettingsPanel");

        // Fallback: instanciar dos prefabs em Resources quando não achar na cena
        if (pausePanel == null || settingsPanel == null)
        {
            Canvas canvas = EncontrarOuCriarPauseCanvas();
            if (pausePanel == null)
            {
                var prefab = Resources.Load<GameObject>("UI/Prefabs/PausePanel");
                if (prefab != null)
                {
                    pausePanel = Instantiate(prefab, canvas.transform);
                    pausePanel.name = "PausePanel";
                }
            }
            if (settingsPanel == null)
            {
                var prefab = Resources.Load<GameObject>("UI/Prefabs/SettingsPanel");
                if (prefab != null)
                {
                    settingsPanel = Instantiate(prefab, canvas.transform);
                    settingsPanel.name = "SettingsPanel";
                }
            }
        }

        // CanvasGroup para fade
        if (pauseCanvasGroup == null && pausePanel != null)
            pauseCanvasGroup = pausePanel.GetComponent<CanvasGroup>();

        // Procurar botões do pause
        if (resumeButton == null && pausePanel != null)
            resumeButton = FindButtonInChildren(pausePanel.transform, "ResumeButton");

        if (settingsButton == null && pausePanel != null)
            settingsButton = FindButtonInChildren(pausePanel.transform, "SettingsButton");

        if (exitButton == null && pausePanel != null)
            exitButton = FindButtonInChildren(pausePanel.transform, "ExitButton");

        // Procurar elementos das configurações
        if (backButton == null && settingsPanel != null)
            backButton = FindButtonInChildren(settingsPanel.transform, "BackButton");

        if (settingsBackButton == null && settingsPanel != null)
            settingsBackButton = FindButtonInChildren(settingsPanel.transform, "BackButton");

        if (musicVolumeSlider == null && settingsPanel != null)
            musicVolumeSlider = FindSliderInChildren(settingsPanel.transform, "MusicSlider/Slider");

        if (sfxVolumeSlider == null && settingsPanel != null)
            sfxVolumeSlider = FindSliderInChildren(settingsPanel.transform, "SFXSlider/Slider");

        if (fullscreenToggle == null && settingsPanel != null)
            fullscreenToggle = FindToggleInChildren(settingsPanel.transform, "FullscreenToggle/Toggle");

        // Aplicar traduções aos textos dos painéis
        ApplyTranslations();

        // ➕ Botão de sair do jogo
        CriarBotaoSair();

        // ➕ Botão "Voltar ao Lobby" (só quando o jogo foi iniciado pelo lobby)
        CriarBotaoVoltarLobby();

        // 🎨 Recolore a UI de pausa para o tema vermelho escuro / preto / branco
        RecolorPauseUI();

        // 🆕 VERIFICAR SE AINDA FALTAM REFERÊNCIAS
        CheckMissingReferences();
    }

    // Cria (uma vez) um botão "Sair" que fecha o jogo, clonando o estilo do ExitButton.
    private void CriarBotaoSair()
    {
        if (pausePanel == null) return;
        if (pausePanel.transform.Find("QuitButton") != null) return; // já criado

        Transform modelo = pausePanel.transform.Find("ExitButton");
        if (modelo == null) return;

        var go = Instantiate(modelo.gameObject, pausePanel.transform);
        go.name = "QuitButton";

        var rt = go.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = new Vector2(0f, -190f); // abaixo do "Seleção"

        var txt = go.transform.Find("Text");
        if (txt != null)
        {
            var tmp = txt.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmp != null) tmp.text = "SAIR";
        }

        var btn = go.GetComponent<UnityEngine.UI.Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(SairDoJogo);
        }
    }

    // Esconde/mostra o cronômetro (TimerBadge) — usado ao pausar/continuar.
    private void MostrarTimer(bool ativo)
    {
        if (timerBadge == null)
        {
            var go = GameObject.Find("TimerBadge");   // ativo no momento do 1º pause
            if (go != null) timerBadge = go;
        }
        if (timerBadge != null) timerBadge.SetActive(ativo);
    }

    private void SairDoJogo()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Cria (uma vez) o botão "Voltar ao Lobby" — só aparece se o jogo veio do lobby.
    private void CriarBotaoVoltarLobby()
    {
        if (pausePanel == null) return;
        if (PlayerPrefs.GetInt("VeioDoLobby", 0) != 1) return;     // só quando veio do lobby
        if (pausePanel.transform.Find("LobbyButton") != null) return; // já criado

        Transform modelo = pausePanel.transform.Find("ExitButton");
        if (modelo == null) return;

        var go = Instantiate(modelo.gameObject, pausePanel.transform);
        go.name = "LobbyButton";

        // espaçamento consistente com os demais botões (gap Configurações→Seleção)
        var exitRT = modelo.GetComponent<RectTransform>();
        float exitY = exitRT != null ? exitRT.anchoredPosition.y : -112f;
        float exitX = exitRT != null ? exitRT.anchoredPosition.x : 0f;
        float espac = 78f;
        var setRT = pausePanel.transform.Find("SettingsButton") is Transform st ? st.GetComponent<RectTransform>() : null;
        if (setRT != null) espac = Mathf.Abs(exitY - setRT.anchoredPosition.y);

        var rt = go.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = new Vector2(exitX, exitY - espac);

        // empurra o "SAIR" (QuitButton) para baixo, mantendo o mesmo espaçamento
        var quit = pausePanel.transform.Find("QuitButton");
        if (quit != null)
        {
            var qrt = quit.GetComponent<RectTransform>();
            if (qrt != null) qrt.anchoredPosition = new Vector2(exitX, exitY - espac * 2f);
        }

        var txt = go.transform.Find("Text");
        if (txt != null)
        {
            var tmp = txt.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmp != null) tmp.text = "VOLTAR AO LOBBY";
        }

        var btn = go.GetComponent<UnityEngine.UI.Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(VoltarAoLobby);
        }
    }

    // Volta para a cena do lobby (encerra os managers persistentes do gameplay).
    public void VoltarAoLobby()
    {
        PlayButtonClickSound();

        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);

        LimparManagersPersistentes();
        SceneManager.LoadScene("lobby");
    }

    // Tema vermelho escuro / preto / branco (igual ao menu de opções). A UI de pausa
    // é gerada por ferramenta de editor (cores antigas na cena), então recolorimos em runtime.
    private void RecolorPauseUI()
    {
        Color vermelho = new Color(0.62f, 0.11f, 0.11f);
        Color corpo    = new Color(0.11f, 0.07f, 0.07f, 1f);
        Color hover    = new Color(0.42f, 0.13f, 0.13f, 1f);

        foreach (var panel in new[] { pausePanel, settingsPanel })
        {
            if (panel == null) continue;

            foreach (var img in panel.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                switch (img.name)
                {
                    case "Brd":
                    case "Ac":  img.color = new Color(vermelho.r, vermelho.g, vermelho.b, img.color.a); break;
                    case "Corpo": img.color = corpo; break;
                    case "HiT":  img.color = new Color(1f, 1f, 1f, 0.12f); break;
                    case "Fi":   img.color = new Color(0.85f, 0.18f, 0.15f); break;            // fill slider
                    case "FHl":  img.color = new Color(1f, 0.55f, 0.45f, 0.40f); break;
                    case "KBd":  img.color = new Color(0.80f, 0.16f, 0.14f); break;            // knob slider
                    case "KHi":  img.color = new Color(1f, 0.70f, 0.60f, 0.65f); break;
                }
            }

            foreach (var btn in panel.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                var cb = btn.colors;
                cb.normalColor      = corpo;
                cb.selectedColor    = corpo;
                cb.highlightedColor = hover;
                cb.pressedColor     = new Color(0.06f, 0.03f, 0.03f, 1f);
                btn.colors = cb;

                // hover dinâmico (escala) — reusa o CardHover
                if (btn.GetComponent<CardHover>() == null) btn.gameObject.AddComponent<CardHover>();
            }

            foreach (var t in panel.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
                if (t.name == "Text") t.color = new Color(0.95f, 0.95f, 0.95f);
        }
    }

    private void ApplyTranslations()
    {
        ApplyTextInChildren(pausePanel,    "Title",                  "pause.title");
        ApplyTextInChildren(pausePanel,    "ResumeButton/Text",      "pause.resume");
        ApplyTextInChildren(pausePanel,    "SettingsButton/Text",    "pause.settings");
        ApplyTextInChildren(pausePanel,    "ExitButton/Text",        "pause.exit");
        // sobrescreve o rótulo do botão de sair para "Seleção"
        var exitTxt = pausePanel != null ? pausePanel.transform.Find("ExitButton/Text") : null;
        if (exitTxt != null)
        {
            var tmp = exitTxt.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmp != null) tmp.text = "SELEÇÃO";
        }
        ApplyTextInChildren(settingsPanel, "SettingsTitle",                "settings.title");
        ApplyTextInChildren(settingsPanel, "BackButton/Text",             "settings.back");
        ApplyTextInChildren(settingsPanel, "MusicSlider/Label",           "settings.music");
        ApplyTextInChildren(settingsPanel, "SFXSlider/Label",             "settings.sfx");
        ApplyTextInChildren(settingsPanel, "FullscreenToggle/Label",      "settings.fullscreen");
        ApplyTextInChildren(settingsPanel, "LanguageRow/Label",           "settings.language");
    }

    private void ApplyTextInChildren(GameObject root, string path, string key)
    {
        if (root == null) return;
        var t = root.transform.Find(path);
        if (t == null) return;
        var tmp = t.GetComponent<TMPro.TextMeshProUGUI>();
        if (tmp != null) tmp.text = Loc.T(key);
    }

    private Canvas EncontrarOuCriarPauseCanvas()
    {
        // Reutiliza o canvas persistente se já existir
        var existingGo = FindInactiveObjectByName("PauseCanvas");
        if (existingGo != null)
        {
            var c = existingGo.GetComponent<Canvas>();
            if (c != null) return c;
        }

        var go = new GameObject("PauseCanvas");
        DontDestroyOnLoad(go);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = go.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        return canvas;
    }

    private static GameObject FindInactiveObjectByName(string name)
    {
        // Procura entre todos os Transforms carregados (incluindo objetos
        // inativos e objetos persistidos com DontDestroyOnLoad), já que
        // GameObject.Find e GetRootGameObjects ignoram esses casos.
        var transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in transforms)
        {
            if (t.name == name && t.gameObject.scene.IsValid())
            {
                return t.gameObject;
            }
        }
        return null;
    }

    private Button FindButtonInChildren(Transform parent, string path)
    {
        Transform buttonTransform = parent.Find(path);
        return buttonTransform?.GetComponent<Button>();
    }

    private Slider FindSliderInChildren(Transform parent, string path)
    {
        Transform sliderTransform = parent.Find(path);
        return sliderTransform?.GetComponent<Slider>();
    }

    private Toggle FindToggleInChildren(Transform parent, string path)
    {
        Transform toggleTransform = parent.Find(path);
        return toggleTransform?.GetComponent<Toggle>();
    }

    private void CheckMissingReferences()
    {
        if (pausePanel == null) Debug.LogError("❌ PausePanel não encontrado!");
        if (settingsPanel == null) Debug.LogError("❌ SettingsPanel não encontrado!");
        if (resumeButton == null) Debug.LogError("❌ ResumeButton não encontrado!");
        if (settingsButton == null) Debug.LogError("❌ SettingsButton não encontrado!");
        if (exitButton == null) Debug.LogError("❌ ExitButton não encontrado!");
    }

    void InitializePauseMenu()
    {

        // 🎯 CONFIGURAR BOTÕES PRINCIPAIS
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OpenSettings);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(ExitToMenu);
        }

        // 🎯 CONFIGURAR BOTÕES DE CONFIGURAÇÕES
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(CloseSettings);
        }

        if (settingsBackButton != null)
        {
            settingsBackButton.onClick.RemoveAllListeners();
            settingsBackButton.onClick.AddListener(CloseSettings);
        }

        // 🎯 CONFIGURAR CONFIGURAÇÕES
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveAllListeners();
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveAllListeners();
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        // 🎯 ESCONDER PAINÉIS INICIALMENTE
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        else
        {
            Debug.LogError("❌ PausePanel é NULL - não pode esconder");
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("❌ SettingsPanel é NULL - não pode esconder");
        }

    }

    void HandlePauseInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && canPause)
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        if (isPaused) return;

        // 🆕 VERIFICAR SE O PAUSEPANEL EXISTE ANTES DE PAUSAR
        if (pausePanel == null)
        {
            Debug.LogError("❌ Não é possível pausar: PausePanel não encontrado!");
            return;
        }

        isPaused = true;
        CoopPause.AbrirMenu(); // SP: timeScale=0; co-op: pausa o grupo (qualquer um retoma)

        // ⏱️ esconde o cronômetro enquanto pausado
        MostrarTimer(false);

        // 🎵 SOM DE PAUSE
        if (pauseSound != null)
            pauseSound.Play();

        // 🎯 MOSTRAR MENU DE PAUSE
        ShowPauseMenu();

    }

    public void ResumeGame()
    {
        if (!isPaused) return;

        isPaused = false;
        CoopPause.FecharMenu(); // SP: timeScale=1; co-op: libera a pausa do grupo

        // ⏱️ mostra o cronômetro de volta
        MostrarTimer(true);

        // 🎵 SOM DE DESPAUSE
        if (unpauseSound != null)
            unpauseSound.Play();

        // 🎯 ESCONDER MENU DE PAUSE
        HidePauseMenu();

    }

    private void ShowPauseMenu()
    {
        if (pausePanel != null)
        {
            // 🎯 GARANTIR QUE CONFIGURAÇÕES ESTEJAM FECHADAS
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                settingsPanel.SetActive(false);
            }

            pausePanel.SetActive(true);

            // esconde HUD que renderiza por cima (contador de mortes / painel de evento)
            OcultarHUDExtra(true);

            // Fade in suave
            if (pauseCanvasGroup != null)
            {
                StartCoroutine(FadeCanvasGroup(pauseCanvasGroup, 0f, 1f, fadeDuration));
            }

            // entrada dinâmica dos botões/título
            StartCoroutine(AnimarEntradaPause());

            // 🎯 SELECIONAR PRIMEIRO BOTÃO AUTOMATICAMENTE
            StartCoroutine(SelectFirstButton());
        }
        else
        {
            Debug.LogError("❌ PausePanel não atribuído! Não é possível mostrar o menu de pause.");
        }
    }

    // Entrada dinâmica: título e botões "pulam" pra dentro de forma escalonada.
    private IEnumerator AnimarEntradaPause()
    {
        string[] nomes = { "Title", "ResumeButton", "SettingsButton", "ExitButton", "LobbyButton", "QuitButton" };
        var alvos = new System.Collections.Generic.List<Transform>();
        foreach (var n in nomes)
        {
            var t = pausePanel.transform.Find(n);
            if (t != null) { t.localScale = Vector3.one * 0.7f; alvos.Add(t); }
        }
        for (int i = 0; i < alvos.Count; i++)
            StartCoroutine(PopInUI(alvos[i], i * 0.06f));
        yield break;
    }

    private IEnumerator PopInUI(Transform t, float delay)
    {
        float e = 0f;
        while (e < delay) { e += Time.unscaledDeltaTime; yield return null; }
        const float dur = 0.22f;
        for (float x = 0f; x < dur; x += Time.unscaledDeltaTime)
        {
            float s = Mathf.Lerp(0.7f, 1f, EaseOutBackPause(x / dur));
            if (t != null) t.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        if (t != null) t.localScale = Vector3.one;
    }

    private static float EaseOutBackPause(float t)
    {
        const float c1 = 1.70158f, c3 = 2.70158f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    // Esconde/mostra os HUDs que renderizam acima do pause (contador de mortes e painel de evento).
    private void OcultarHUDExtra(bool ocultar)
    {
        var c = GameObject.Find("ContadorMortesCanvas");
        if (c != null) { var cv = c.GetComponent<Canvas>(); if (cv != null) cv.enabled = !ocultar; }
        var e = GameObject.Find("EventoCanvas");
        if (e != null) { var cv = e.GetComponent<Canvas>(); if (cv != null) cv.enabled = !ocultar; }
    }

    private void HidePauseMenu()
    {
        OcultarHUDExtra(false); // restaura o HUD ao sair do pause

        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            CloseSettings();
        }

        if (pausePanel != null)
        {
            // Fade out suave
            if (pauseCanvasGroup != null)
            {
                StartCoroutine(FadeOutAndHide(pauseCanvasGroup, fadeDuration));
            }
            else
            {
                pausePanel.SetActive(false);
            }
        }
    }

    private IEnumerator FadeOutAndHide(CanvasGroup canvasGroup, float duration)
    {
        yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 0f, duration));
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private IEnumerator SelectFirstButton()
    {
        yield return new WaitForSecondsRealtime(0.1f);

        // 🎯 TENTAR SELECIONAR RESUME BUTTON PRIMEIRO
        if (resumeButton != null && resumeButton.gameObject.activeInHierarchy)
        {
            resumeButton.Select();
            resumeButton.OnSelect(null);
        }
        else if (settingsButton != null)
        {
            settingsButton.Select();
            settingsButton.OnSelect(null);
        }
    }

    // 🎯 CONFIGURAÇÕES - AGORA FUNCIONANDO
    private GameObject opcoesInGame;

    public void OpenSettings()
    {
        PlayButtonClickSound();

        if (opcoesInGame != null) return; // já aberto

        // Abre o MESMO painel de opções da tela inicial (abas Áudio/Vídeo/Jogo/Controles).
        if (pausePanel != null) pausePanel.SetActive(false);

        opcoesInGame = new GameObject("OpcoesInGame");
        var menu = opcoesInGame.AddComponent<MenuInicialUI>();
        menu.modoSomenteOpcoes = true;
        menu.aoFecharOpcoes = () =>
        {
            opcoesInGame = null;
            if (pausePanel != null) pausePanel.SetActive(true);
        };
    }

    public void CloseSettings()
    {
        PlayButtonClickSound();

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            SaveSettings();

            // 🎯 VOLTAR A SELECIONAR O BOTÃO DE RESUME
            if (resumeButton != null)
            {
                resumeButton.Select();
                resumeButton.OnSelect(null);
            }

        }
    }

    // CONFIGURACOES DE AUDIO/VIDEO - AGORA FUNCIONANDO
    private void SetMusicVolume(float volume)
    {
        // Implementar seu sistema de audio aqui
        AudioListener.volume = volume;
    }

    private void SetSFXVolume(float volume)
    {
        // Implementar SFX separado se tiver
    }

    private void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    private void LoadSettings()
    {
        // 🎵 CARREGAR CONFIGURAÇÕES SALVAS
        try
        {
            if (musicVolumeSlider != null)
                musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.8f);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.8f);

            if (fullscreenToggle != null)
                fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", 1) == 1;

        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Erro ao carregar configurações: {e.Message}");
        }
    }

    private void SaveSettings()
    {
        // 💾 SALVAR CONFIGURAÇÕES
        try
        {
            if (musicVolumeSlider != null)
                PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);

            if (sfxVolumeSlider != null)
                PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);

            if (fullscreenToggle != null)
                PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);

            PlayerPrefs.Save();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Erro ao salvar configurações: {e.Message}");
        }
    }

    // 🚪 SAIR DO JOGO - AGORA FUNCIONANDO
    public void ExitToMenu()
    {
        PlayButtonClickSound();

        // Despausar antes de sair
        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);

        // Destrói todos os singletons persistentes antes de carregar o menu,
        // para que a UI do gameplay não vaze para outras cenas.
        LimparManagersPersistentes();

        // 🎯 CARREGAR CENA DO MENU PRINCIPAL
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            try
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Erro ao carregar cena {mainMenuSceneName}: {e.Message}");
            }
        }
        else
        {
            Debug.LogError("❌ Nome da cena do menu principal não configurado!");
        }
    }

    private void LimparManagersPersistentes()
    {
        GameObject eventoCanvas = GameObject.Find("EventoCanvas");
        if (eventoCanvas != null) Destroy(eventoCanvas);

        if (GerenciadorEventos.Instance != null) Destroy(GerenciadorEventos.Instance.gameObject);
        if (UIManager.Instance          != null) Destroy(UIManager.Instance.gameObject);
        if (SkillManager.Instance       != null) Destroy(SkillManager.Instance.gameObject);
        if (StatusCardSystem.Instance   != null) Destroy(StatusCardSystem.Instance.gameObject);

        // Destrói o próprio PauseManager por último
        Instance = null;
        Destroy(gameObject);
    }

    public void ExitGame()
    {
        PlayButtonClickSound();


#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    // 🎵 EFEITOS SONOROS
    private void PlayButtonClickSound()
    {
        if (buttonClickSound != null)
            buttonClickSound.Play();
    }

    // ✨ EFEITOS VISUAIS
    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        group.alpha = to;
    }

    // 🎯 MÉTODOS PÚBLICOS
    public bool IsGamePaused()
    {
        return isPaused;
    }

    public void SetPauseAbility(bool canPause)
    {
        this.canPause = canPause;
    }

    public void SetMainMenuScene(string sceneName)
    {
        mainMenuSceneName = sceneName;
    }

    [ContextMenu("⏸️ Testar Pause")]
    public void TestPause()
    {
        if (!isPaused)
        {
            PauseGame();
        }
        else
        {
            ResumeGame();
        }
    }

    [ContextMenu("⚙️ Abrir Configurações")]
    public void TestSettings()
    {
        if (!isPaused)
            PauseGame();

        OpenSettings();
    }

    [ContextMenu("🚪 Testar Saída para Menu")]
    public void TestExitToMenu()
    {
        // Não executar realmente, só mostrar que funciona
    }

    [ContextMenu("🔍 Verificar Referências")]
    public void DebugReferences()
    {
    }

    void OnLanguageChanged(Language _) => ApplyTranslations();

    void OnDestroy()
    {
        Loc.OnLanguageChanged -= OnLanguageChanged;
        if (isPaused)
            Time.timeScale = 1f;
    }
}
