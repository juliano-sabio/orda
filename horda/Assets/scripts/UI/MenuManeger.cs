using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MenuManager : MonoBehaviour
{
    [Header("Painéis do Menu")]
    public GameObject mainMenuPanel;
    public GameObject optionsPanel;
    public GameObject characterSelectionPanel;

    [Header("Componentes de Controle")]
    [Tooltip("Arraste o Animator do MainMenuPanel aqui")]
    public Animator mainMenuAnimator;

    [Tooltip("Adicione um CanvasGroup ao MainMenuPanel e arraste aqui")]
    public CanvasGroup mainMenuCanvasGroup;

    [Header("Botões do Menu Principal")]
    public Button playButton;
    public Button optionsButton;
    public Button exitButton;

    [Header("Botões do Painel de Opções")]
    public Button optionsBackButton;
    public Slider volumeSlider;
    public Toggle fullscreenToggle;

    [Header("Configurações de Cenas")]
    public string characterSelectionSceneName = "CharacterSelection";
    public string gameSceneName = "Gameplay"; // Corrigido: Variável restaurada

    void Start()
    {
        // Configurar listeners dos botões
        if (playButton) playButton.onClick.AddListener(PlayGame);
        if (optionsButton) optionsButton.onClick.AddListener(ShowOptions);
        if (exitButton) exitButton.onClick.AddListener(ExitGame);

        if (optionsBackButton) optionsBackButton.onClick.AddListener(ShowMainMenu);
        if (volumeSlider) volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        if (fullscreenToggle) fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggle);

        LoadSettings();

        // Garante o estado inicial correto
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        Debug.Log("🏠 Mostrando Menu Principal...");

        // Fecha outros painéis
        if (optionsPanel) optionsPanel.SetActive(false);
        if (characterSelectionPanel) characterSelectionPanel.SetActive(false);

        // Ativa o painel principal
        mainMenuPanel.SetActive(true);

        // Reativa interação (cliques) no menu principal
        if (mainMenuCanvasGroup != null)
        {
            mainMenuCanvasGroup.interactable = true;
            mainMenuCanvasGroup.blocksRaycasts = true;
            mainMenuCanvasGroup.alpha = 1f;
        }

        // Dispara animação de entrada
        if (mainMenuAnimator != null)
        {
            mainMenuAnimator.SetTrigger("Entrar");
        }
    }

    public void ShowOptions()
    {
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(true);
            // Esta linha joga o objeto para o último lugar da hierarquia em tempo de execução
            optionsPanel.transform.SetAsLastSibling();
        }

        if (mainMenuCanvasGroup != null)
        {
            mainMenuCanvasGroup.interactable = false;
            mainMenuCanvasGroup.blocksRaycasts = false;
        }
    }

    public void PlayGame()
    {
        if (!string.IsNullOrEmpty(characterSelectionSceneName))
        {
            SceneManager.LoadScene(characterSelectionSceneName);
        }
        else
        {
            Debug.LogWarning("⚠️ Cena de seleção não definida. Indo para Gameplay...");
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void StartGameDirectly()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void ExitGame()
    {
        Debug.Log("👋 Saindo...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // --- Persistência e Configurações ---

    void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
    }

    void OnFullscreenToggle(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
    }

    void LoadSettings()
    {
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        if (volumeSlider) volumeSlider.value = savedVolume;
        AudioListener.volume = savedVolume;

        bool savedFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        if (fullscreenToggle) fullscreenToggle.isOn = savedFullscreen;
        Screen.fullScreen = savedFullscreen;
    }
}