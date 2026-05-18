using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class MenuInicialUI : MonoBehaviour
{
    [Header("Cenas")]
    public string cenaSelecaoPersonagem = "CharacterSelection";

    [Header("Título")]
    public string tituloJogo = "ORDA";
    public string subtitulo  = "Sobreviva à horda";

    // ── paleta ──────────────────────────────────────────────────────
    static readonly Color corFundo       = new Color(0.04f, 0.04f, 0.10f);
    static readonly Color corAcento      = new Color(0.55f, 0.15f, 0.85f);
    static readonly Color corAcentoClaro = new Color(0.75f, 0.35f, 1.00f);
    static readonly Color corBotao       = new Color(0.12f, 0.08f, 0.22f);
    static readonly Color corBotaoHover  = new Color(0.28f, 0.12f, 0.50f);
    static readonly Color corBotaoBorda  = new Color(0.55f, 0.15f, 0.85f);

    // ──────────────────────────────────────────────────────────────
    void Start()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        var canvas = CriarCanvas();
        CriarFundo(canvas);
        CriarParticulas(canvas);
        CriarLinhaDivisoria(canvas);
        var titulo = CriarTitulo(canvas);
        CriarBotoes(canvas);
        CriarRodape(canvas);

        StartCoroutine(AnimarTitulo(titulo));
        StartCoroutine(AnimarParticulas(canvas));
    }

    // ── Canvas ──────────────────────────────────────────────────────
    GameObject CriarCanvas()
    {
        var go = new GameObject("Canvas_Menu");
        var c  = go.AddComponent<Canvas>();
        c.renderMode   = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 10;

        var cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    // ── Fundo ────────────────────────────────────────────────────────
    void CriarFundo(GameObject canvas)
    {
        Esticar(CriarImagem(canvas, "Fundo", corFundo));

        // faixa roxa no topo
        var topo = CriarImagem(canvas, "FundoTopo", new Color(corAcento.r, corAcento.g, corAcento.b, 0.18f));
        var r = topo.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0, 0.75f); r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;

        // faixa escura no centro
        var centro = CriarImagem(canvas, "FundoCentro", new Color(0.08f, 0.04f, 0.14f, 0.6f));
        var rc = centro.GetComponent<RectTransform>();
        rc.anchorMin = new Vector2(0.2f, 0.25f); rc.anchorMax = new Vector2(0.8f, 0.82f);
        rc.offsetMin = rc.offsetMax = Vector2.zero;
    }

    // ── Partículas (pontos flutuantes) ───────────────────────────────
    const int QTD_PARTICULAS = 18;
    GameObject[] particulas = new GameObject[QTD_PARTICULAS];

    void CriarParticulas(GameObject canvas)
    {
        for (int i = 0; i < QTD_PARTICULAS; i++)
        {
            var p = CriarImagem(canvas, $"P{i}", new Color(corAcento.r, corAcento.g, corAcento.b, Random.Range(0.08f, 0.30f)));
            float sz = Random.Range(4f, 14f);
            var r = p.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f));
            r.sizeDelta = new Vector2(sz, sz);
            particulas[i] = p;
        }
    }

    // ── Linha divisória ──────────────────────────────────────────────
    void CriarLinhaDivisoria(GameObject canvas)
    {
        var linha = CriarImagem(canvas, "Linha", corAcento);
        var r = linha.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.2f, 0.78f); r.anchorMax = new Vector2(0.8f, 0.78f);
        r.offsetMin = Vector2.zero; r.offsetMax = new Vector2(0, 2f);
    }

    // ── Título ───────────────────────────────────────────────────────
    GameObject CriarTitulo(GameObject canvas)
    {
        var container = new GameObject("ContainerTitulo");
        container.transform.SetParent(canvas.transform, false);
        var rc = container.AddComponent<RectTransform>();
        rc.anchorMin = new Vector2(0f, 0.78f); rc.anchorMax = new Vector2(1f, 0.97f);
        rc.offsetMin = rc.offsetMax = Vector2.zero;

        // título principal
        var t = CriarTexto(container, "Titulo", Vector2.zero, Vector2.one,
            tituloJogo, 90f, FontStyles.Bold, Color.white);
        t.alignment = TextAlignmentOptions.Bottom;

        // sombra
        var sombra = CriarTexto(container, "TituloSombra",
            new Vector2(0f, 0f), new Vector2(1f, 1f),
            tituloJogo, 90f, FontStyles.Bold, new Color(corAcento.r, corAcento.g, corAcento.b, 0.5f));
        sombra.alignment = TextAlignmentOptions.Bottom;
        sombra.GetComponent<RectTransform>().anchoredPosition = new Vector2(3f, -3f);
        sombra.transform.SetSiblingIndex(0);

        // subtítulo
        CriarTexto(container, "Sub", new Vector2(0f, -4f), Vector2.one,
            subtitulo, 18f, FontStyles.Italic, new Color(0.75f, 0.65f, 0.90f));

        return container;
    }

    // ── Botões ───────────────────────────────────────────────────────
    void CriarBotoes(GameObject canvas)
    {
        // yMin / yMax explícitos — sem overlap garantido
        // cada botão ocupa 0.10, gap de 0.04 entre eles
        CriarBotaoMenu(canvas, "▶  JOGAR", 0.58f, 0.70f, corAcento,                       28f, () => SceneManager.LoadScene(cenaSelecaoPersonagem));
        CriarBotaoMenu(canvas, "OPÇÕES",   0.44f, 0.54f, new Color(0.20f, 0.20f, 0.38f),  22f, AbrirOpcoes);
        CriarBotaoMenu(canvas, "SAIR",     0.30f, 0.40f, new Color(0.40f, 0.06f, 0.06f),  22f, Sair);
    }

    void CriarBotaoMenu(GameObject canvas, string label,
        float yMin, float yMax, Color corBorda, float fontSize,
        System.Action acao)
    {
        var go = new GameObject($"Btn_{label.Trim()}");
        go.transform.SetParent(canvas.transform, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(0.30f, yMin);
        r.anchorMax = new Vector2(0.70f, yMax);
        r.offsetMin = r.offsetMax = Vector2.zero;

        // fundo
        var img = go.AddComponent<Image>();
        img.color = corBotao;

        // borda esquerda colorida
        var borda = new GameObject("Borda");
        borda.transform.SetParent(go.transform, false);
        var rb = borda.AddComponent<RectTransform>();
        rb.anchorMin = Vector2.zero;
        rb.anchorMax = new Vector2(0f, 1f);
        rb.offsetMin = Vector2.zero;
        rb.offsetMax = new Vector2(6f, 0f);
        borda.AddComponent<Image>().color = corBorda;

        // texto
        var txt = CriarTexto(go, "Txt", Vector2.zero, Vector2.one,
            label, fontSize, FontStyles.Bold, Color.white);
        txt.alignment = TextAlignmentOptions.Center;

        // botão
        var btn = go.AddComponent<Button>();
        btn.transition    = Selectable.Transition.None;
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => acao());

        // hover
        var hover = go.AddComponent<BotaoMenuHover>();
        hover.img       = img;
        hover.corNormal = corBotao;
        hover.corHover  = corBotaoHover;
        hover.corBorda  = corBorda;
        hover.bordaGO   = borda;
    }

    // ── Rodapé ───────────────────────────────────────────────────────
    void CriarRodape(GameObject canvas)
    {
        var go = new GameObject("Rodape");
        go.transform.SetParent(canvas.transform, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(0f, 0f); r.anchorMax = new Vector2(1f, 0.06f);
        r.offsetMin = r.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

        int moedas = PlayerPrefs.GetInt("PlayerCoins", 0);
        int nivel  = PlayerPrefs.GetInt("PlayerLevel",  1);

        CriarTexto(go, "Info", Vector2.zero, Vector2.one,
            $"Nível  {nivel}        Moedas  {moedas}",
            14f, FontStyles.Normal, new Color(0.7f, 0.6f, 0.9f));
    }

    // ── Animações ────────────────────────────────────────────────────
    IEnumerator AnimarTitulo(GameObject container)
    {
        float t = 0f;
        var rt = container.GetComponent<RectTransform>();
        Vector3 base3 = Vector3.one;
        while (true)
        {
            t += Time.deltaTime * 1.2f;
            float s = 1f + Mathf.Sin(t) * 0.012f;
            rt.localScale = base3 * s;
            yield return null;
        }
    }

    IEnumerator AnimarParticulas(GameObject canvas)
    {
        float[] fases   = new float[QTD_PARTICULAS];
        float[] speeds  = new float[QTD_PARTICULAS];
        float[] ampX    = new float[QTD_PARTICULAS];
        float[] ampY    = new float[QTD_PARTICULAS];
        Vector2[] origens = new Vector2[QTD_PARTICULAS];

        for (int i = 0; i < QTD_PARTICULAS; i++)
        {
            fases[i]  = Random.Range(0f, Mathf.PI * 2f);
            speeds[i] = Random.Range(0.3f, 0.9f);
            ampX[i]   = Random.Range(0.01f, 0.04f);
            ampY[i]   = Random.Range(0.01f, 0.05f);
            origens[i] = particulas[i].GetComponent<RectTransform>().anchorMin;
        }

        float t = 0f;
        while (true)
        {
            t += Time.deltaTime;
            for (int i = 0; i < QTD_PARTICULAS; i++)
            {
                if (particulas[i] == null) { yield break; }
                var rt = particulas[i].GetComponent<RectTransform>();
                float ox = origens[i].x + Mathf.Sin(t * speeds[i] + fases[i]) * ampX[i];
                float oy = origens[i].y + Mathf.Cos(t * speeds[i] * 0.7f + fases[i]) * ampY[i];
                rt.anchorMin = rt.anchorMax = new Vector2(ox, oy);

                var img = particulas[i].GetComponent<Image>();
                img.color = new Color(corAcento.r, corAcento.g, corAcento.b,
                    Mathf.Abs(Mathf.Sin(t * speeds[i] + fases[i])) * 0.25f + 0.05f);
            }
            yield return null;
        }
    }

    // ── Ações ────────────────────────────────────────────────────────
    void AbrirOpcoes() { /* pode expandir depois */ }

    void Sair()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── Helpers ──────────────────────────────────────────────────────
    GameObject CriarImagem(GameObject parent, string nome, Color cor)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = cor;
        return go;
    }

    TextMeshProUGUI CriarTexto(GameObject parent, string nome,
        Vector2 ancMin, Vector2 ancMax,
        string texto, float size, FontStyles style, Color cor)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(parent.transform, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = ancMin; r.anchorMax = ancMax;
        r.offsetMin = r.offsetMax = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = texto; t.fontSize = size;
        t.fontStyle = style; t.color = cor;
        t.alignment = TextAlignmentOptions.Center;
        return t;
    }

    void Esticar(GameObject go)
    {
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }
}

// ── Componente de hover dos botões do menu ───────────────────────────
public class BotaoMenuHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Image  img;
    public Color  corNormal;
    public Color  corHover;
    public Color  corBorda;
    public GameObject bordaGO;

    private bool  sobre = false;
    private float escala = 1f;
    private float escalaAlvo = 1f;

    void Update()
    {
        escala = Mathf.Lerp(escala, escalaAlvo, Time.deltaTime * 12f);
        transform.localScale = Vector3.one * escala;

        if (img != null)
            img.color = Color.Lerp(img.color, sobre ? corHover : corNormal, Time.deltaTime * 10f);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        sobre = true;
        escalaAlvo = 1.04f;
        if (bordaGO != null)
            bordaGO.GetComponent<Image>().color = corAcentoClaro();
    }

    public void OnPointerExit(PointerEventData e)
    {
        sobre = false;
        escalaAlvo = 1f;
        if (bordaGO != null)
            bordaGO.GetComponent<Image>().color = corBorda;
    }

    public void OnPointerClick(PointerEventData e)
    {
        StartCoroutine(FlashClick());
    }

    System.Collections.IEnumerator FlashClick()
    {
        escalaAlvo = 0.96f;
        yield return new WaitForSeconds(0.08f);
        escalaAlvo = 1.04f;
    }

    Color corAcentoClaro() => new Color(0.75f, 0.35f, 1.00f);
}
