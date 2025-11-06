using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class temp_UI_lobMenu : MonoBehaviour
{
    [Header("Referências UI")]
    public Button playButton;
    public Button optionsButton;
    public Button exitButton;
    public Text playerLevelText;
    public Text selectedCharacterText;

    void Start()
    {
        // Configurar botões
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButtonClicked);

        if (optionsButton != null)
            optionsButton.onClick.AddListener(OnOptionsButtonClicked);

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitButtonClicked);

        // Atualizar informações do jogador
        UpdatePlayerInfo();
    }

    void UpdatePlayerInfo()
    {
        // Atualizar nível do jogador
        if (playerLevelText != null)
        {
            int playerLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
            playerLevelText.text = $"Nível: {playerLevel}";
        }

        // 🆕 ATUALIZADO: Buscar informações do personagem selecionado
        if (selectedCharacterText != null)
        {
            // 🆕 BUSCA O MANAGER NA CENA ATUAL
            CharacterSelectionManager manager = FindAnyObjectByType<CharacterSelectionManager>();

            if (manager != null)
            {
                int selectedIndex = PlayerPrefs.GetInt("SelectedCharacter", 0);
                if (selectedIndex < manager.characters.Count)
                {
                    string charName = manager.characters[selectedIndex].characterName;
                    selectedCharacterText.text = $"Personagem: {charName}";
                }
                else
                {
                    selectedCharacterText.text = "Personagem: Nenhum";
                }
            }
            else
            {
                selectedCharacterText.text = "Personagem: Não selecionado";
            }
        }
    }

    void OnPlayButtonClicked()
    {
        Debug.Log("🎮 Iniciando jogo...");

        // 🆕 ATUALIZADO: Usar GameSceneManager se disponível
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.GoToCharacterSelection();
        }
        else
        {
            SceneManager.LoadScene("CharacterSelection");
        }
    }

    void OnOptionsButtonClicked()
    {
        Debug.Log("⚙️ Abrindo opções...");
        // Sua lógica de opções aqui
    }

    void OnExitButtonClicked()
    {
        Debug.Log("👋 Saindo do jogo...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    // 🆕 MÉTODO PARA ATUALIZAR INFORMAÇÕES DINAMICAMENTE
    public void RefreshUI()
    {
        UpdatePlayerInfo();
    }

    // 🆕 MÉTODO PARA TESTE RÁPIDO
    [ContextMenu("Testar Navegação")]
    public void TestNavigation()
    {
        Debug.Log("🧪 Testando navegação...");

        // 🆕 BUSCA O MANAGER NA CENA ATUAL
        CharacterSelectionManager manager = FindAnyObjectByType<CharacterSelectionManager>();

        if (manager != null)
        {
            Debug.Log($"✅ CharacterSelectionManager encontrado com {manager.characters.Count} personagens");
        }
        else
        {
            Debug.Log("ℹ️ CharacterSelectionManager não está nesta cena");
        }
    }

    void Update()
    {
        // Atualizar dinamicamente (opcional)
        if (Input.GetKeyDown(KeyCode.F5))
        {
            RefreshUI();
        }
    }
}