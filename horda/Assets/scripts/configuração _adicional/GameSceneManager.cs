using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance;

    [Header("Nomes das Cenas")]
    public string mainMenuScene = "MainMenu";
    public string characterSelectionScene = "CharacterSelection";
    public string gameplayScene = "Gameplay";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 🎯 MÉTODOS PÚBLICOS PARA NAVEGAÇÃO - CORRIGIDOS
    public void GoToMainMenu()
    {
        Debug.Log("🏠 Indo para Menu Principal...");
        SceneManager.LoadScene(mainMenuScene);
    }

    public void GoToCharacterSelection()
    {
        Debug.Log("🎮 Indo para Seleção de Personagens...");
        SceneManager.LoadScene(characterSelectionScene);
    }

    public void StartGameplay()
    {
        Debug.Log("🚀 Iniciando Gameplay...");
        SceneManager.LoadScene(gameplayScene);

        // 🆕 CORREÇÃO: Inicialização mais robusta
        StartCoroutine(InitializeGameplayCoroutine());
    }

    private IEnumerator InitializeGameplayCoroutine()
    {
        // Espera a cena carregar completamente
        yield return new WaitForSeconds(0.1f);

        // 🆕 CORREÇÃO: Usa CharacterSelectionManagerIntegrated
        CharacterSelectionManagerIntegrated selectionManager = FindAnyObjectByType<CharacterSelectionManagerIntegrated>();
        PlayerStats playerStats = FindAnyObjectByType<PlayerStats>();
        SkillManager skillManager = SkillManager.Instance;

        if (playerStats != null)
        {
            if (selectionManager != null)
            {
                selectionManager.ApplyCharacterToPlayerSystems(playerStats, skillManager);
                Debug.Log("✅ Personagem selecionado aplicado ao gameplay!");
            }
            else
            {
                Debug.Log("⚠️ Iniciando sem seleção de personagem (modo padrão)");
                // Inicializa com stats padrão
                playerStats.InitializeDefaultSkills();
            }

            // Força atualização da UI
            playerStats.ForceUIUpdate();
        }
        else
        {
            Debug.LogError("❌ PlayerStats não encontrado na cena de gameplay!");
        }
    }

    // 🆕 MÉTODO MELHORADO PARA INICIAR COM PERSONAGEM ESPECÍFICO
    public void StartGameplayWithCharacter(int characterIndex)
    {
        Debug.Log($"🚀 Iniciando Gameplay com personagem índice: {characterIndex}");

        // 🆕 VALIDAÇÃO DE ÍNDICE
        if (characterIndex < 0)
        {
            Debug.LogError("❌ Índice de personagem inválido!");
            return;
        }

        // Salva a seleção antes de mudar de cena
        PlayerPrefs.SetInt("SelectedCharacter", characterIndex);
        PlayerPrefs.Save();

        StartGameplay();
    }

    // 🆕 MÉTODO PARA CARREGAR CENA COM VALIDAÇÃO
    public void LoadSceneByName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("❌ Nome da cena está vazio!");
            return;
        }

        if (!SceneExists(sceneName))
        {
            Debug.LogError($"❌ Cena '{sceneName}' não existe no build settings!");
            return;
        }

        Debug.Log($"📁 Carregando cena: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    // ✅ MÉTODOS EXISTENTES (MANTIDOS)
    public bool HasSelectedCharacter()
    {
        return PlayerPrefs.HasKey("SelectedCharacter");
    }

    public int GetSelectedCharacterIndex()
    {
        return PlayerPrefs.GetInt("SelectedCharacter", 0);
    }

    public void QuitGame()
    {
        Debug.Log("👋 Saindo do jogo...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void RestartCurrentScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"🔄 Reiniciando cena: {currentScene}");
        SceneManager.LoadScene(currentScene);
    }

    public bool SceneExists(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string scene = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (scene == sceneName)
                return true;
        }
        return false;
    }

    // 🆕 MÉTODO DE DEBUG MELHORADO
    [ContextMenu("Debug Scene Info")]
    public void DebugSceneInfo()
    {
        Debug.Log("🔍 Informações das Cenas:");
        Debug.Log($"Cena Atual: {SceneManager.GetActiveScene().name}");
        Debug.Log($"Total de Cenas no Build: {SceneManager.sceneCountInBuildSettings}");

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            Debug.Log($"Cena [{i}]: {sceneName}");
        }

        // 🆕 CORREÇÃO: Usa CharacterSelectionManagerIntegrated
        PlayerStats playerStats = FindAnyObjectByType<PlayerStats>();
        CharacterSelectionManagerIntegrated manager = FindAnyObjectByType<CharacterSelectionManagerIntegrated>();
        SkillManager skillManager = SkillManager.Instance;

        Debug.Log($"PlayerStats: {(playerStats != null ? "✅ Encontrado" : "❌ Não encontrado")}");
        Debug.Log($"CharacterSelectionManagerIntegrated: {(manager != null ? "✅ Encontrado" : "❌ Não encontrado")}");
        Debug.Log($"SkillManager: {(skillManager != null ? "✅ Encontrado" : "❌ Não encontrado")}");
    }
}