using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class ContadorMortes : MonoBehaviour
{
    int mortes = 0;
    public int Mortes => mortes;
    TextMeshProUGUI textoContador;
    GameObject canvasGO;

    static ContadorMortes _instance;
    public static ContadorMortes Instance => _instance;

    static readonly string[] CENAS_JOGO =
        { "primeira_fase", "segunda_fase", "terceira_fase", "Modo_sobrevivencia" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCriar()
    {
        if (_instance != null) return;
        var go = new GameObject("ContadorMortes");
        DontDestroyOnLoad(go);
        go.AddComponent<ContadorMortes>();
    }

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
    }

    void Start()
    {
        CriarTexto();
        InimigoController.OnInimigoDerrotado += OnMorteInimigo;
        PlayerStats.OnPlayerMorreu           += ResetarContador;
        SceneManager.sceneLoaded             += OnSceneLoaded;
        AtualizarVisibilidade(SceneManager.GetActiveScene().name);
        Atualizar();
    }

    void OnDestroy()
    {
        InimigoController.OnInimigoDerrotado -= OnMorteInimigo;
        PlayerStats.OnPlayerMorreu           -= ResetarContador;
        SceneManager.sceneLoaded             -= OnSceneLoaded;
        if (_instance == this) _instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool ehJogo = EhCenaJogo(scene.name);
        AtualizarVisibilidade(scene.name);
        if (ehJogo) ResetarContador();
    }

    static bool EhCenaJogo(string nome) =>
        System.Array.Exists(CENAS_JOGO, s => s == nome);

    void AtualizarVisibilidade(string sceneName)
    {
        if (canvasGO != null)
            canvasGO.SetActive(EhCenaJogo(sceneName));
    }

    void OnMorteInimigo()
    {
        // OnInimigoDerrotado só dispara no host (a morte do inimigo é host-autoritativa).
        // Co-op: o host soma no NetworkVariable compartilhado; as duas telas leem no Update().
        if (NetSpawn.EmRede)
        {
            var cp = CoopProgressao.Instance;
            if (cp != null && NetSpawn.PodeSpawnar) cp.abates.Value++;
            return;
        }
        mortes++;
        Atualizar();
    }

    // Co-op: espelha o total compartilhado (host e cliente exibem o MESMO número).
    void Update()
    {
        if (!NetSpawn.EmRede) return;
        var cp = CoopProgressao.Instance;
        if (cp == null) return;
        if (mortes != cp.abates.Value)
        {
            mortes = cp.abates.Value;
            Atualizar();
        }
    }

    public void ResetarContador()
    {
        mortes = 0;
        if (NetSpawn.EmRede && NetSpawn.PodeSpawnar && CoopProgressao.Instance != null)
            CoopProgressao.Instance.abates.Value = 0;
        Atualizar();
    }

    void Atualizar()
    {
        if (textoContador != null)
            textoContador.text = $"{Loc.T("ui.deaths")}: {mortes}";
    }

    void CriarTexto()
    {
        canvasGO = new GameObject("ContadorMortesCanvas");
        DontDestroyOnLoad(canvasGO);

        var cv = canvasGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 55;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        var go = new GameObject("ContadorMortesTexto");
        go.transform.SetParent(canvasGO.transform, false);

        var rt = go.AddComponent<RectTransform>();
        // Fica logo ABAIXO do badge do cronômetro (topo-esquerda).
        // Badge: anchor topo-esq, pivot (0,1), pos (16,-16), size (220,54) → bottom ≈ -70.
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(0f, 1f);
        rt.pivot            = new Vector2(0f, 1f);
        rt.sizeDelta        = new Vector2(220f, 28f);
        rt.anchoredPosition = new Vector2(16f, -74f); // ~4px abaixo do badge, alinhado à esquerda

        textoContador = go.AddComponent<TextMeshProUGUI>();
        textoContador.fontSize           = 20;
        textoContador.fontStyle          = FontStyles.Bold;
        textoContador.alignment          = TextAlignmentOptions.Center;
        textoContador.color              = new Color(1f, 0.9f, 0.2f);
        textoContador.outlineWidth       = 0.25f;
        textoContador.outlineColor       = new Color32(0, 0, 0, 220);
        textoContador.textWrappingMode   = TMPro.TextWrappingModes.NoWrap;
    }
}
