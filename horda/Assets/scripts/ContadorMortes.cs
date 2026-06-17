using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class ContadorMortes : MonoBehaviour
{
    int mortes = 0;
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
        mortes++;
        Atualizar();
    }

    public void ResetarContador() { mortes = 0; Atualizar(); }

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
        // Espelha o anchor do timeBar (stretch no topo) para ficar alinhado com ele.
        // O timeBar tem: anchorMin=(0,1) anchorMax=(1,1), anchoredPos=(-753.56, -55.44),
        // sizeDelta=(-1567.45, 43.16) → efetivamente x:30~383, bottom em y≈-77 do topo.
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.sizeDelta        = new Vector2(-1567f, 30f);
        rt.anchoredPosition = new Vector2(-753.5f, -82f); // 5px abaixo do bottom do timeBar (~-77)

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
