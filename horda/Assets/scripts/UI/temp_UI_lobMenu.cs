using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class temp_UI_lobMenu : MonoBehaviour
{
    [Header("Referências UI Básicas")]
    public Button playButton;
    public Button optionsButton;
    public Button exitButton;
    public Text playerLevelText;
    public Text selectedCharacterText;
    public Text coinsText;

    void Start()
    {
        // Configurar botões básicos
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButtonClicked);

        if (optionsButton != null)
            optionsButton.onClick.AddListener(OnOptionsButtonClicked);

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitButtonClicked);

        UpdatePlayerInfo();
    }

    // 🆕 MUDAR DE private PARA public!
    public void UpdatePlayerInfo()
    {
        // Atualizar nível do jogador
        if (playerLevelText != null)
        {
            int playerLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
            playerLevelText.text = $"Nível: {playerLevel}";
        }

        // Atualizar personagem selecionado
        if (selectedCharacterText != null)
        {
            int selectedIndex = PlayerPrefs.GetInt("SelectedCharacter", 0);

            // 🆕 CORREÇÃO: Buscar CharacterSelectionManagerIntegrated
            CharacterSelectionManagerIntegrated charManager = FindAnyObjectByType<CharacterSelectionManagerIntegrated>();
            if (charManager != null && charManager.characters != null &&
                selectedIndex < charManager.characters.Length &&
                charManager.characters[selectedIndex] != null)
            {
                string charName = charManager.characters[selectedIndex].characterName;
                selectedCharacterText.text = $"Personagem: {charName}";
            }
            else
            {
                selectedCharacterText.text = "Personagem: Nenhum";
            }
        }

        // Atualizar moedas
        if (coinsText != null)
        {
            int coins = PlayerPrefs.GetInt("PlayerCoins", 1000);
            coinsText.text = $"Moedas: {coins}";
        }
    }

    void OnPlayButtonClicked()
    {
        Debug.Log("🎮 Indo para seleção de personagem...");
        SceneManager.LoadScene("CharacterSelection");
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

    // 🆕 MÉTODO PARA ATUALIZAR APENAS AS MOEDAS
    public void UpdateCoinsDisplay()
    {
        if (coinsText != null)
        {
            int coins = PlayerPrefs.GetInt("PlayerCoins", 1000);
            coinsText.text = $"Moedas: {coins}";
        }
    }

    // 🆕 MÉTODO PARA ATUALIZAR APENAS O PERSONAGEM (CORRIGIDO)
    public void UpdateCharacterDisplay()
    {
        if (selectedCharacterText != null)
        {
            int selectedIndex = PlayerPrefs.GetInt("SelectedCharacter", 0);

            // 🆕 CORREÇÃO: Usa CharacterSelectionManagerIntegrated
            CharacterSelectionManagerIntegrated charManager = FindAnyObjectByType<CharacterSelectionManagerIntegrated>();

            if (charManager != null && charManager.characters != null &&
                selectedIndex < charManager.characters.Length &&
                charManager.characters[selectedIndex] != null)
            {
                string charName = charManager.characters[selectedIndex].characterName;
                selectedCharacterText.text = $"Personagem: {charName}";
            }
            else
            {
                selectedCharacterText.text = "Personagem: Nenhum";
            }
        }
    }

    // 🆕 MÉTODO PARA REFRESCAR TODA A UI
    public void RefreshAllUI()
    {
        UpdatePlayerInfo();
    }

    // 🆕 MÉTODO PARA VERIFICAR STATUS (CORRIGIDO)
    [ContextMenu("Verificar Status do Lobby")]
    public void CheckLobbyStatus()
    {
        Debug.Log("🔍 Status do Lobby:");

        // 🆕 CORREÇÃO: Usa CharacterSelectionManagerIntegrated
        CharacterSelectionManagerIntegrated charManager = FindAnyObjectByType<CharacterSelectionManagerIntegrated>();

        Debug.Log($"CharacterSelectionManagerIntegrated: {(charManager != null ? "✅ Encontrado" : "❌ Não encontrado")}");

        if (charManager != null && charManager.characters != null)
        {
            Debug.Log($"Personagens disponíveis: {charManager.characters.Length}");
            for (int i = 0; i < charManager.characters.Length; i++)
            {
                if (charManager.characters[i] != null)
                {
                    Debug.Log($"• {i}: {charManager.characters[i].characterName} ({(charManager.characters[i].unlocked ? "🔓" : "🔒")})");
                }
            }
        }

        Debug.Log($"Moedas: {PlayerPrefs.GetInt("PlayerCoins", 1000)}");
        Debug.Log($"Nível: {PlayerPrefs.GetInt("PlayerLevel", 1)}");
        Debug.Log($"Personagem selecionado: {PlayerPrefs.GetInt("SelectedCharacter", 0)}");
    }
}