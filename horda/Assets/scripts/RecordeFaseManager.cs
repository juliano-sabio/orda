using UnityEngine;
using UnityEngine.SceneManagement;

// Salva, por fase, o melhor tempo sobrevivido e a maior quantidade de inimigos mortos.
// Conta os kills por conta própria (não depende do ContadorMortes, que zera no mesmo
// evento de morte do player) e mede o tempo com Time.timeSinceLevelLoad.
// Usado pela tela de seleção de fase para exibir "Recorde: MM:SS • N mortes" em cada card.
public class RecordeFaseManager : MonoBehaviour
{
    static RecordeFaseManager _instance;

    static readonly string[] CENAS_JOGO =
        { "primeira_fase", "segunda_fase", "terceira_fase", "Modo_sobrevivencia" };

    int killsAtuais;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCriar()
    {
        if (_instance != null) return;
        var go = new GameObject("RecordeFaseManager");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<RecordeFaseManager>();
    }

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
    }

    void Start()
    {
        PlayerStats.OnPlayerMorreu           += SalvarRecorde;
        InimigoController.OnInimigoDerrotado += OnInimigoMorto;
        SceneManager.sceneLoaded             += OnSceneLoaded;
    }

    void OnDestroy()
    {
        PlayerStats.OnPlayerMorreu           -= SalvarRecorde;
        InimigoController.OnInimigoDerrotado -= OnInimigoMorto;
        SceneManager.sceneLoaded             -= OnSceneLoaded;
        if (_instance == this) _instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (EhCenaJogo(scene.name)) killsAtuais = 0;
    }

    void OnInimigoMorto() => killsAtuais++;

    void SalvarRecorde()
    {
        string cena = SceneManager.GetActiveScene().name;
        if (!EhCenaJogo(cena)) return;

        float tempo = Time.timeSinceLevelLoad;
        int   mortes = killsAtuais;

        string keyTempo  = $"Recorde_{cena}_Tempo";
        string keyMortes = $"Recorde_{cena}_Mortes";

        bool novoTempo  = tempo  > PlayerPrefs.GetFloat(keyTempo, 0f);
        bool novoMortes = mortes > PlayerPrefs.GetInt(keyMortes, 0);

        if (novoTempo)  PlayerPrefs.SetFloat(keyTempo, tempo);
        if (novoMortes) PlayerPrefs.SetInt(keyMortes, mortes);
        PlayerPrefs.Save();

        Debug.Log($"[Recorde] {cena}: tempo={tempo:F1}s (recorde={novoTempo}), mortes={mortes} (recorde={novoMortes})");
    }

    static bool EhCenaJogo(string nome) =>
        System.Array.Exists(CENAS_JOGO, s => s == nome);

    public static float ObterMelhorTempo(string cena)  => PlayerPrefs.GetFloat($"Recorde_{cena}_Tempo", 0f);
    public static int   ObterMelhorMortes(string cena) => PlayerPrefs.GetInt($"Recorde_{cena}_Mortes", 0);
}
