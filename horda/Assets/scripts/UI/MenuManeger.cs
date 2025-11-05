using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Painéis do Menu")]
    public GameObject mainMenuPanel;
    public GameObject optionsPanel;

    [Header("Botões do Menu Principal")]
    public Button playButton;
    public Button optionsButton;
    public Button exitButton;

    [Header("Botões do Painel de Opções")]
    public Button optionsBackButton;
    public Slider volumeSlider;
    public Toggle fullscreenToggle;

    [Header("Configurações")]
    public string gameSceneName = "GameScene"; // Nome da cena do jogo

    void Start()
    {
        // Configurar listeners dos botões do menu principal
        playButton.onClick.AddListener(PlayGame);
        optionsButton.onClick.AddListener(ShowOptions);
        exitButton.onClick.AddListener(ExitGame);

        // Configurar listeners do painel de opções
        optionsBackButton.onClick.AddListener(ShowMainMenu);
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggle); // CORREÇÃO AQUI

        // Carregar configurações salvas
        LoadSettings();

        // Garantir que o menu principal está visível
        ShowMainMenu();
    }

    void PlayGame()
    {
        Debug.Log("Iniciando jogo...");
        SceneManager.LoadScene(gameSceneName);
    }

    void ShowOptions()
    {
        Debug.Log("Abrindo opções...");
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    void ShowMainMenu()
    {
        Debug.Log("Voltando ao menu principal...");
        optionsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    void ExitGame()
    {
        Debug.Log("Saindo do jogo...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
        Debug.Log($"Volume alterado para: {value}");
    }

    void OnFullscreenToggle(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        Debug.Log($"Tela cheia: {isFullscreen}");
    }

    void LoadSettings()
    {
        // Carregar volume
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        volumeSlider.value = savedVolume;
        AudioListener.volume = savedVolume;

        // Carregar configuração de tela cheia
        bool savedFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        fullscreenToggle.isOn = savedFullscreen;
        Screen.fullScreen = savedFullscreen;
    }
}