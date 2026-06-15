using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class MissaoEspiritoManager : MonoBehaviour
{
    const int META_KILLS = 50;
    const float DURACAO_CARD_CONCLUIDO = 10f;

    int mortes = 0;
    GameObject canvasGO;
    GameObject cardConcluido;
    Coroutine corCardConcluido;

    static MissaoEspiritoManager _instance;
    public static MissaoEspiritoManager Instance => _instance;

    static readonly string[] CENAS_JOGO =
        { "primeira_fase", "segunda_fase", "terceira_fase", "Modo_sobrevivencia" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCriar()
    {
        if (_instance != null) return;
        var go = new GameObject("MissaoEspiritoManager");
        DontDestroyOnLoad(go);
        go.AddComponent<MissaoEspiritoManager>();
    }

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
    }

    void Start()
    {
        mortes = PlayerPrefs.GetInt("MissaoEspiritoMortes", 0);
        CriarCanvas();
        CriarCardConcluido();
        InimigoController.OnInimigoDerrotado += OnMorteInimigo;
        SceneManager.sceneLoaded             += OnSceneLoaded;
        AtualizarVisibilidade(SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        InimigoController.OnInimigoDerrotado -= OnMorteInimigo;
        SceneManager.sceneLoaded             -= OnSceneLoaded;
        if (_instance == this) _instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AtualizarVisibilidade(scene.name);
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
        if (mortes >= META_KILLS)
        {
            mortes = 0;
            PlayerPrefs.SetInt("MissaoEspiritoPendente", 1);
            UIManager.Instance?.ShowSkillAcquired(Loc.T("mission.complete"), Loc.T("mission.spirit_reward"));
            MostrarCardConcluido();
        }
        PlayerPrefs.SetInt("MissaoEspiritoMortes", mortes);
        PlayerPrefs.Save();
    }

    [ContextMenu("Testar Card de Missao Concluida")]
    void TestarCardConcluido() => MostrarCardConcluido();

    void CriarCanvas()
    {
        canvasGO = new GameObject("MissaoEspiritoCanvas");
        DontDestroyOnLoad(canvasGO);

        var cv = canvasGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 55;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();
    }

    void CriarCardConcluido()
    {
        var go = new GameObject("CardMissaoConcluida");
        go.transform.SetParent(canvasGO.transform, false);

        // Mesmo alinhamento horizontal do ContadorMortesTexto, logo abaixo dele.
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.sizeDelta        = new Vector2(-1567f, 64f);
        rt.anchoredPosition = new Vector2(-753.5f, -120f);

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.03f, 0.08f, 0.94f);

        // borda externa dourada
        var borda = new GameObject("Borda");
        borda.transform.SetParent(go.transform, false);
        borda.transform.SetAsFirstSibling();
        var rb = borda.AddComponent<RectTransform>();
        rb.anchorMin = Vector2.zero; rb.anchorMax = Vector2.one;
        rb.offsetMin = new Vector2(-2f, -2f); rb.offsetMax = new Vector2(2f, 2f);
        borda.AddComponent<Image>().color = new Color(0.80f, 0.65f, 0.25f, 1f);

        // brilho pulsante por cima da borda
        var glowBorda = new GameObject("GlowBorda");
        glowBorda.transform.SetParent(go.transform, false);
        var rgb = glowBorda.AddComponent<RectTransform>();
        rgb.anchorMin = Vector2.zero; rgb.anchorMax = Vector2.one;
        rgb.offsetMin = new Vector2(-4f, -4f); rgb.offsetMax = new Vector2(4f, 4f);
        var imgGlowBorda = glowBorda.AddComponent<Image>();
        imgGlowBorda.color = new Color(1f, 0.85f, 0.35f, 0f);
        imgGlowBorda.raycastTarget = false;
        glowBorda.AddComponent<CardGlowEffect>().Setup(imgGlowBorda, new Color(1f, 0.85f, 0.35f));

        // faixa de destaque ciano à esquerda
        var faixa = new GameObject("Faixa");
        faixa.transform.SetParent(go.transform, false);
        var rFaixa = faixa.AddComponent<RectTransform>();
        rFaixa.anchorMin = new Vector2(0f, 0f); rFaixa.anchorMax = new Vector2(0f, 1f);
        rFaixa.offsetMin = Vector2.zero; rFaixa.offsetMax = new Vector2(5f, 0f);
        faixa.AddComponent<Image>().color = new Color(0.30f, 0.85f, 1.00f, 0.9f);

        // ícone do espírito
        var icone = new GameObject("Icone");
        icone.transform.SetParent(go.transform, false);
        var rIco = icone.AddComponent<RectTransform>();
        rIco.anchorMin = new Vector2(0f, 0f); rIco.anchorMax = new Vector2(0f, 1f);
        rIco.pivot     = new Vector2(0f, 0.5f);
        rIco.sizeDelta = new Vector2(48f, 0f);
        rIco.anchoredPosition = new Vector2(12f, 0f);
        var tIco = icone.AddComponent<TextMeshProUGUI>();
        tIco.text      = "✨";
        tIco.fontSize  = 30;
        tIco.alignment = TextAlignmentOptions.Center;
        tIco.color     = new Color(0.30f, 0.85f, 1.00f);

        var titulo = new GameObject("Titulo");
        titulo.transform.SetParent(go.transform, false);
        var rTit = titulo.AddComponent<RectTransform>();
        rTit.anchorMin = new Vector2(0f, 0.5f); rTit.anchorMax = new Vector2(1f, 1f);
        rTit.offsetMin = new Vector2(64f, 0f); rTit.offsetMax = new Vector2(-12f, -4f);
        var tTit = titulo.AddComponent<TextMeshProUGUI>();
        tTit.text      = Loc.T("mission.complete");
        tTit.fontSize  = 20;
        tTit.fontStyle = FontStyles.Bold;
        tTit.alignment = TextAlignmentOptions.MidlineLeft;
        tTit.color     = new Color(0.95f, 0.80f, 0.40f);
        tTit.outlineWidth = 0.2f;
        tTit.outlineColor = new Color32(0, 0, 0, 220);

        var desc = new GameObject("Desc");
        desc.transform.SetParent(go.transform, false);
        var rDesc = desc.AddComponent<RectTransform>();
        rDesc.anchorMin = new Vector2(0f, 0f); rDesc.anchorMax = new Vector2(1f, 0.5f);
        rDesc.offsetMin = new Vector2(64f, 4f); rDesc.offsetMax = new Vector2(-12f, 0f);
        var tDesc = desc.AddComponent<TextMeshProUGUI>();
        tDesc.text      = Loc.T("mission.spirit_reward");
        tDesc.fontSize  = 13;
        tDesc.alignment = TextAlignmentOptions.MidlineLeft;
        tDesc.color     = new Color(0.75f, 0.90f, 1.00f);

        cardConcluido = go;
        cardConcluido.SetActive(false);
    }

    void MostrarCardConcluido()
    {
        if (cardConcluido == null)
        {
            Debug.LogWarning("[MissaoEspiritoManager] cardConcluido nao foi criado.");
            return;
        }
        if (!canvasGO.activeSelf)
            Debug.LogWarning("[MissaoEspiritoManager] canvasGO inativo - card nao sera visivel.");
        if (corCardConcluido != null) StopCoroutine(corCardConcluido);
        corCardConcluido = StartCoroutine(EsconderCardConcluidoApos(DURACAO_CARD_CONCLUIDO));
    }

    IEnumerator EsconderCardConcluidoApos(float segundos)
    {
        cardConcluido.SetActive(true);
        yield return new WaitForSeconds(segundos);
        cardConcluido.SetActive(false);
    }
}
