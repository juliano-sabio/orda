using UnityEngine;
using UnityEngine.SceneManagement;

// Salva, por fase, o melhor tempo sobrevivido e a maior quantidade de mortes já alcançados.
// Usado pela tela de seleção de fase para exibir "Recorde: MM:SS • N mortes" em cada card.
public class RecordeFaseManager : MonoBehaviour
{
    static RecordeFaseManager _instance;

    static readonly string[] CENAS_JOGO =
        { "primeira_fase", "segunda_fase", "terceira_fase", "Modo_sobrevivencia" };

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
        PlayerStats.OnPlayerMorreu += SalvarRecorde;
    }

    void OnDestroy()
    {
        PlayerStats.OnPlayerMorreu -= SalvarRecorde;
        if (_instance == this) _instance = null;
    }

    void SalvarRecorde()
    {
        string cena = SceneManager.GetActiveScene().name;
        if (!System.Array.Exists(CENAS_JOGO, s => s == cena)) return;

        var timer   = FindAnyObjectByType<TimerManager>();
        float tempo = timer != null
            ? Mathf.Max(0f, timer.levelDuration - timer.currentTime)
            : Time.timeSinceLevelLoad;
        int mortes  = ContadorMortes.Instance != null ? ContadorMortes.Instance.Mortes : 0;

        string keyTempo  = $"Recorde_{cena}_Tempo";
        string keyMortes = $"Recorde_{cena}_Mortes";

        if (tempo  > PlayerPrefs.GetFloat(keyTempo, 0f)) PlayerPrefs.SetFloat(keyTempo, tempo);
        if (mortes > PlayerPrefs.GetInt(keyMortes, 0))   PlayerPrefs.SetInt(keyMortes, mortes);

        PlayerPrefs.Save();
    }

    public static float ObterMelhorTempo(string cena)  => PlayerPrefs.GetFloat($"Recorde_{cena}_Tempo", 0f);
    public static int   ObterMelhorMortes(string cena) => PlayerPrefs.GetInt($"Recorde_{cena}_Mortes", 0);
}
