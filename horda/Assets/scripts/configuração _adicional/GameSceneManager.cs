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

    [HideInInspector] public CharacterData selectedCharacterData;

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
        SceneManager.LoadScene(mainMenuScene);
    }

    public void GoToCharacterSelection()
    {
        SceneManager.LoadScene(characterSelectionScene);
    }

    public void StartGameplay()
    {
        SceneManager.LoadScene(gameplayScene);

        // 🆕 CORREÇÃO: Inicialização mais robusta
        StartCoroutine(InitializeGameplayCoroutine());
    }

    private IEnumerator InitializeGameplayCoroutine()
    {
        yield return new WaitForSeconds(0.1f);

        PlayerStats playerStats = FindAnyObjectByType<PlayerStats>(); // coop-local-ok: fluxo SP de init de cena (co-op usa FaseCoopBootstrap+PlayerNet)

        if (playerStats != null)
        {
            if (selectedCharacterData != null)
            {
                playerStats.characterData = selectedCharacterData;
                playerStats.ApplyCharacterData();

                int u0 = PlayerPrefs.GetInt("Upgrade_0", 0);
                int u1 = PlayerPrefs.GetInt("Upgrade_1", 0);
                int u2 = PlayerPrefs.GetInt("Upgrade_2", 0);
                int u3 = PlayerPrefs.GetInt("Upgrade_3", 0);
                playerStats.maxHealth *= (1 + u0 * 0.05f);
                playerStats.health     = playerStats.maxHealth;
                playerStats.attack    *= (1 + u1 * 0.05f);
                playerStats.defense   *= (1 + u2 * 0.05f);
                playerStats.speed     *= (1 + u3 * 0.05f);
            }
            else
            {
                playerStats.InitializeDefaultSkills();
            }

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
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void RestartCurrentScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
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

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
        }

        // 🆕 CORREÇÃO: Usa CharacterSelectionManagerIntegrated
        PlayerStats playerStats = FindAnyObjectByType<PlayerStats>(); // coop-local-ok: fluxo SP de init de cena (co-op usa FaseCoopBootstrap+PlayerNet)
        CharacterSelectionManagerIntegrated manager = FindAnyObjectByType<CharacterSelectionManagerIntegrated>();
        SkillManager skillManager = SkillManager.Instance;

    }
}
