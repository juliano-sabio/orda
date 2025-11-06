using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Painéis do Menu")]
    public GameObject mainMenuPanel;
    public GameObject optionsPanel;
    public GameObject characterSelectionPanel; // 🆕 Painel para seleção de personagens

    [Header("Botões do Menu Principal")]
    public Button playButton;
    public Button optionsButton;
    public Button exitButton;

    [Header("Botões do Painel de Opções")]
    public Button optionsBackButton;
    public Slider volumeSlider;
    public Toggle fullscreenToggle;

    [Header("Configurações de Cenas")]
    public string characterSelectionSceneName = "CharacterSelection"; // 🆕 Nome da cena do lobby
    public string gameSceneName = "Gameplay"; // Nome da cena do jogo

    void Start()
    {
        // Configurar listeners dos botões do menu principal
        playButton.onClick.AddListener(PlayGame);
        optionsButton.onClick.AddListener(ShowOptions);
        exitButton.onClick.AddListener(ExitGame);

        // Configurar listeners do painel de opções
        optionsBackButton.onClick.AddListener(ShowMainMenu);
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggle);

        // Carregar configurações salvas
        LoadSettings();

        // Garantir que o menu principal está visível
        ShowMainMenu();
    }

    void PlayGame()
    {
        Debug.Log("🎮 Indo para seleção de personagens...");

        // 🆕 VAI PARA O LOBBY (SELEÇÃO DE PERSONAGENS)
        if (!string.IsNullOrEmpty(characterSelectionSceneName))
        {
            SceneManager.LoadScene(characterSelectionSceneName);
        }
        else
        {
            // Fallback: vai direto para o gameplay
            Debug.LogWarning("⚠️ Nome da cena de seleção não configurado! Indo direto para gameplay...");
            SceneManager.LoadScene(gameSceneName);
        }
    }

    void ShowOptions()
    {
        Debug.Log("⚙️ Abrindo opções...");
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    void ShowMainMenu()
    {
        Debug.Log("🏠 Voltando ao menu principal...");
        optionsPanel.SetActive(false);
        if (characterSelectionPanel != null)
            characterSelectionPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    void ExitGame()
    {
        Debug.Log("👋 Saindo do jogo...");

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
        Debug.Log($"🔊 Volume alterado para: {value}");
    }

    void OnFullscreenToggle(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        Debug.Log($"🖥️ Tela cheia: {isFullscreen}");
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

    // 🆕 MÉTODO PARA INICIAR DIRETO NO GAMEPLAY (PARA TESTES)
    public void StartGameDirectly()
    {
        Debug.Log("🚀 Iniciando jogo diretamente...");
        SceneManager.LoadScene(gameSceneName);
    }

    // 🆕 MÉTODO PARA VOLTAR AO MENU PRINCIPAL
    public void BackToMainMenu()
    {
        Debug.Log("↩️ Voltando ao menu principal...");
        SceneManager.LoadScene("MainMenu"); // Ou o nome da sua cena de menu
    }
}