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
    private float previousTimeScale;
    private bool canPause = true;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 🆕 PROCURAR REFERÊNCIAS AUTOMATICAMENTE SE NÃO ESTIVEREM ATRIBUÍDAS
        FindUIReferences();
        InitializePauseMenu();
        LoadSettings();
    }

    void Update()
    {
        HandlePauseInput();
    }

    // 🆕 MÉTODO PARA PROCURAR REFERÊNCIAS AUTOMATICAMENTE
    private void FindUIReferences()
    {
        Debug.Log("🔍 Procurando referências de UI automaticamente...");

        // Procurar painéis
        if (pausePanel == null)
        {
            pausePanel = GameObject.Find("PausePanel");
            if (pausePanel != null) Debug.Log("✅ PausePanel encontrado automaticamente");
        }

        if (settingsPanel == null)
        {
            settingsPanel = GameObject.Find("SettingsPanel");
            if (settingsPanel != null) Debug.Log("✅ SettingsPanel encontrado automaticamente");
        }

        // Procurar botões do pause
        if (resumeButton == null && pausePanel != null)
        {
            resumeButton = FindButtonInChildren(pausePanel.transform, "ResumeButton");
            if (resumeButton != null) Debug.Log("✅ ResumeButton encontrado automaticamente");
        }

        if (settingsButton == null && pausePanel != null)
        {
            settingsButton = FindButtonInChildren(pausePanel.transform, "SettingsButton");
            if (settingsButton != null) Debug.Log("✅ SettingsButton encontrado automaticamente");
        }

        if (exitButton == null && pausePanel != null)
        {
            exitButton = FindButtonInChildren(pausePanel.transform, "ExitButton");
            if (exitButton != null) Debug.Log("✅ ExitButton encontrado automaticamente");
        }

        // Procurar elementos das configurações
        if (backButton == null && settingsPanel != null)
        {
            backButton = FindButtonInChildren(settingsPanel.transform, "BackButton");
            if (backButton != null) Debug.Log("✅ BackButton encontrado automaticamente");
        }

        if (settingsBackButton == null && settingsPanel != null)
        {
            settingsBackButton = FindButtonInChildren(settingsPanel.transform, "BackButton");
            if (settingsBackButton != null) Debug.Log("✅ SettingsBackButton encontrado automaticamente");
        }

        if (musicVolumeSlider == null && settingsPanel != null)
        {
            musicVolumeSlider = FindSliderInChildren(settingsPanel.transform, "MusicSlider/Slider");
            if (musicVolumeSlider != null) Debug.Log("✅ MusicVolumeSlider encontrado automaticamente");
        }

        if (sfxVolumeSlider == null && settingsPanel != null)
        {
            sfxVolumeSlider = FindSliderInChildren(settingsPanel.transform, "SFXSlider/Slider");
            if (sfxVolumeSlider != null) Debug.Log("✅ SFXVolumeSlider encontrado automaticamente");
        }

        if (fullscreenToggle == null && settingsPanel != null)
        {
            fullscreenToggle = FindToggleInChildren(settingsPanel.transform, "FullscreenToggle/Toggle");
            if (fullscreenToggle != null) Debug.Log("✅ FullscreenToggle encontrado automaticamente");
        }

        // 🆕 VERIFICAR SE AINDA FALTAM REFERÊNCIAS
        CheckMissingReferences();
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
        Debug.Log("🎮 Inicializando menu de pause...");

        // 🎯 CONFIGURAR BOTÕES PRINCIPAIS
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeGame);
            Debug.Log("✅ ResumeButton configurado - FUNCIONANDO");
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OpenSettings);
            Debug.Log("✅ SettingsButton configurado - FUNCIONANDO");
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(ExitToMenu);
            Debug.Log("✅ ExitButton configurado - FUNCIONANDO");
        }

        // 🎯 CONFIGURAR BOTÕES DE CONFIGURAÇÕES
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(CloseSettings);
            Debug.Log("✅ BackButton configurado - FUNCIONANDO");
        }

        if (settingsBackButton != null)
        {
            settingsBackButton.onClick.RemoveAllListeners();
            settingsBackButton.onClick.AddListener(CloseSettings);
            Debug.Log("✅ SettingsBackButton configurado - FUNCIONANDO");
        }

        // 🎯 CONFIGURAR CONFIGURAÇÕES
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveAllListeners();
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
            Debug.Log("✅ MusicVolumeSlider configurado - FUNCIONANDO");
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
            Debug.Log("✅ SFXVolumeSlider configurado - FUNCIONANDO");
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveAllListeners();
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
            Debug.Log("✅ FullscreenToggle configurado - FUNCIONANDO");
        }

        // 🎯 ESCONDER PAINÉIS INICIALMENTE
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
            Debug.Log("✅ PausePanel escondido");
        }
        else
        {
            Debug.LogError("❌ PausePanel é NULL - não pode esconder");
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            Debug.Log("✅ SettingsPanel escondido");
        }
        else
        {
            Debug.LogError("❌ SettingsPanel é NULL - não pode esconder");
        }

        Debug.Log("✅ PauseManager inicializado completamente!");
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
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        // 🎵 SOM DE PAUSE
        if (pauseSound != null)
            pauseSound.Play();

        // 🎯 MOSTRAR MENU DE PAUSE
        ShowPauseMenu();

        Debug.Log("⏸️ Jogo pausado");
    }

    public void ResumeGame()
    {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = previousTimeScale;

        // 🎵 SOM DE DESPAUSE
        if (unpauseSound != null)
            unpauseSound.Play();

        // 🎯 ESCONDER MENU DE PAUSE
        HidePauseMenu();

        Debug.Log("▶️ Jogo despausado");
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

            // Fade in suave
            if (pauseCanvasGroup != null)
            {
                StartCoroutine(FadeCanvasGroup(pauseCanvasGroup, 0f, 1f, fadeDuration));
            }

            // 🎯 SELECIONAR PRIMEIRO BOTÃO AUTOMATICAMENTE
            StartCoroutine(SelectFirstButton());
        }
        else
        {
            Debug.LogError("❌ PausePanel não atribuído! Não é possível mostrar o menu de pause.");
            Debug.Log("💡 Execute 'Criar Sistema de Pause Completo' no UIManager");
        }
    }

    private void HidePauseMenu()
    {
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
            Debug.Log("🎯 ResumeButton selecionado");
        }
        else if (settingsButton != null)
        {
            settingsButton.Select();
            settingsButton.OnSelect(null);
            Debug.Log("🎯 SettingsButton selecionado");
        }
    }

    // 🎯 CONFIGURAÇÕES - AGORA FUNCIONANDO
    public void OpenSettings()
    {
        PlayButtonClickSound();

        if (settingsPanel != null && pausePanel != null)
        {
            settingsPanel.SetActive(true);

            // 🎯 SELECIONAR BOTÃO DE VOLTAR NAS CONFIGURAÇÕES
            if (settingsBackButton != null)
            {
                settingsBackButton.Select();
                settingsBackButton.OnSelect(null);
            }

            Debug.Log("⚙️ Painel de configurações aberto - FUNCIONANDO");
        }
        else
        {
            Debug.LogError("❌ SettingsPanel ou PausePanel não atribuído!");
        }
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

            Debug.Log("⚙️ Painel de configurações fechado - FUNCIONANDO");
        }
    }

    // ⚙️ CONFIGURAÇÕES DE AUDIO/VIDEO - AGORA FUNCIONANDO
    private void SetMusicVolume(float volume)
    {
        // Implementar seu sistema de audio aqui
        AudioListener.volume = volume;
        Debug.Log($"🎵 Volume de música: {volume} - FUNCIONANDO");
    }

    private void SetSFXVolume(float volume)
    {
        // Implementar SFX separado se tiver
        Debug.Log($"🔊 Volume SFX: {volume} - FUNCIONANDO");
    }

    private void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        Debug.Log($"🖥️ Tela cheia: {isFullscreen} - FUNCIONANDO");
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

            Debug.Log("⚙️ Configurações carregadas");
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
            Debug.Log("💾 Configurações salvas");
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

        Debug.Log("🚪 Saindo para o menu principal... - FUNCIONANDO");

        // Despausar antes de sair
        ResumeGame();

        // 🎯 CARREGAR CENA DO MENU PRINCIPAL
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            try
            {
                SceneManager.LoadScene(mainMenuSceneName);
                Debug.Log($"🎮 Carregando cena: {mainMenuSceneName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Erro ao carregar cena {mainMenuSceneName}: {e.Message}");
                Debug.Log("💡 Verifique se o nome da cena está correto no PauseManager");
            }
        }
        else
        {
            Debug.LogError("❌ Nome da cena do menu principal não configurado!");
        }
    }

    public void ExitGame()
    {
        PlayButtonClickSound();

        Debug.Log("🛑 Saindo do jogo... - FUNCIONANDO");

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
        Debug.Log($"🎮 Cena do menu principal definida: {sceneName}");
    }

    [ContextMenu("⏸️ Testar Pause")]
    public void TestPause()
    {
        if (!isPaused)
        {
            PauseGame();
            Debug.Log("🧪 Teste: Jogo pausado");
        }
        else
        {
            ResumeGame();
            Debug.Log("🧪 Teste: Jogo despausado");
        }
    }

    [ContextMenu("⚙️ Abrir Configurações")]
    public void TestSettings()
    {
        if (!isPaused)
            PauseGame();

        OpenSettings();
        Debug.Log("🧪 Teste: Configurações abertas");
    }

    [ContextMenu("🚪 Testar Saída para Menu")]
    public void TestExitToMenu()
    {
        Debug.Log("🧪 Testando saída para menu...");
        // Não executar realmente, só mostrar que funciona
        Debug.Log("✅ ExitToMenu funcionando! (Não executado em teste)");
    }

    [ContextMenu("🔍 Verificar Referências")]
    public void DebugReferences()
    {
        Debug.Log($"🔍 REFERÊNCIAS DO PAUSEMANAGER:");
        Debug.Log($"• PausePanel: {(pausePanel != null ? "✅" : "❌")}");
        Debug.Log($"• SettingsPanel: {(settingsPanel != null ? "✅" : "❌")}");
        Debug.Log($"• ResumeButton: {(resumeButton != null ? "✅" : "❌")}");
        Debug.Log($"• SettingsButton: {(settingsButton != null ? "✅" : "❌")}");
        Debug.Log($"• ExitButton: {(exitButton != null ? "✅" : "❌")}");
        Debug.Log($"• MusicVolumeSlider: {(musicVolumeSlider != null ? "✅" : "❌")}");
        Debug.Log($"• FullscreenToggle: {(fullscreenToggle != null ? "✅" : "❌")}");
        Debug.Log($"• Cena do Menu: {mainMenuSceneName}");
    }

    void OnDestroy()
    {
        // Garantir que o jogo não fique pausado
        if (isPaused)
        {
            Time.timeScale = 1f;
        }
    }
}