using UnityEngine;
using UnityEngine.SceneManagement;

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

    // 🎯 MÉTODOS PÚBLICOS PARA NAVEGAÇÃO
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

        // Garante que o PlayerStats será inicializado corretamente
        Invoke("InitializeGameplay", 0.1f);
    }

    void InitializeGameplay()
    {
        // 🆕 BUSCA O CHARACTER SELECTION MANAGER NA CENA ATUAL (não usa mais Instance)
        CharacterSelectionManager selectionManager = FindAnyObjectByType<CharacterSelectionManager>();
        PlayerStats playerStats = FindAnyObjectByType<PlayerStats>();
        SkillManager skillManager = FindAnyObjectByType<SkillManager>();

        if (playerStats != null && selectionManager != null)
        {
            selectionManager.ApplyCharacterToPlayerSystems(playerStats, skillManager);
            Debug.Log("✅ Personagem selecionado aplicado ao gameplay!");
        }
        else if (playerStats != null)
        {
            Debug.Log("⚠️ Iniciando sem personagem selecionado (modo direto)");
        }
    }

    // 🆕 MÉTODO PARA INICIAR GAMEPLAY COM PERSONAGEM ESPECÍFICO
    public void StartGameplayWithCharacter(int characterIndex)
    {
        Debug.Log($"🚀 Iniciando Gameplay com personagem índice: {characterIndex}");

        // Salva a seleção antes de mudar de cena
        PlayerPrefs.SetInt("SelectedCharacter", characterIndex);
        PlayerPrefs.Save();

        StartGameplay();
    }

    // 🆕 MÉTODO PARA VERIFICAR SE HÁ PERSONAGEM SELECIONADO
    public bool HasSelectedCharacter()
    {
        return PlayerPrefs.HasKey("SelectedCharacter");
    }

    // 🆕 MÉTODO PARA OBTER PERSONAGEM SELECIONADO
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

    // 🆕 MÉTODO PARA REINICIAR A CENA ATUAL
    public void RestartCurrentScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"🔄 Reiniciando cena: {currentScene}");
        SceneManager.LoadScene(currentScene);
    }

    // 🆕 MÉTODO PARA CARREGAR CENA POR NOME
    public void LoadSceneByName(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            Debug.Log($"📁 Carregando cena: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("❌ Nome da cena está vazio!");
        }
    }

    // 🆕 MÉTODO PARA VERIFICAR SE CENA EXISTE
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

    // 🆕 MÉTODO DE DEBUG
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

        // Verifica CharacterSelectionManager
        CharacterSelectionManager manager = FindAnyObjectByType<CharacterSelectionManager>();
        if (manager != null)
        {
            Debug.Log($"✅ CharacterSelectionManager encontrado na cena atual");
            Debug.Log($"Personagens carregados: {manager.characters.Count}");
        }
        else
        {
            Debug.Log("❌ CharacterSelectionManager não encontrado na cena atual");
        }
    }
}