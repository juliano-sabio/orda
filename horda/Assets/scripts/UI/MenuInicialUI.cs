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

    // Modo "só opções": usado in-game (pause) para reaproveitar este mesmo painel de configurações.
    [HideInInspector] public bool modoSomenteOpcoes = false;
    [HideInInspector] public System.Action aoFecharOpcoes;

    // ── Paleta ─────────────────────────────────────────────────────────
    static readonly Color corFundo      = new Color(0.04f, 0.03f, 0.10f);
    static readonly Color corAcento     = new Color(0.55f, 0.15f, 0.85f);
    static readonly Color corClaro      = new Color(0.75f, 0.35f, 1.00f);
    static readonly Color corBotao      = new Color(0.10f, 0.07f, 0.20f);
    static readonly Color corBotaoHover = new Color(0.26f, 0.10f, 0.48f);

    // ── Partículas ─────────────────────────────────────────────────────
    const int QTD_P = 20;
    RectTransform[] pRT   = new RectTransform[QTD_P];
    Image[]         pImg  = new Image[QTD_P];
    Vector2[]       pOrig = new Vector2[QTD_P];
    float[]         pFase = new float[QTD_P];
    float[]         pVel  = new float[QTD_P];

    // ── Idioma ──────────────────────────────────────────────────────────
    TextMeshProUGUI _langDisplayTxt;
    int             _langPreviewIdx;

    // ── Refs ────────────────────────────────────────────────────────────
    Sprite     sprBotao;
    Sprite     sprFundoOpcoes;   // bg_dungeon_forja
    Sprite     sprBotaoOpcoes;   // btn_stone  (128×32, 9-slice 10px)
    Sprite     sprHudBar;        // hud_bar    (31×26, tiled)
    Sprite     sprSlotFrame;     // slot_frame (32×32, tiled — handle/knob)
    Sprite     sprSliderTrack;   // ui_slider_track (64×16, 9-slice)
    Sprite     sprSliderFill;    // ui_slider_fill  (64×16, 9-slice)
    Sprite     sprSliderKnob;    // ui_slider_knob  (14×26)
    Sprite     sprToggleOn;      // ui_toggle_on    (48×20)
    Sprite     sprToggleOff;     // ui_toggle_off   (48×20)
    GameObject canvasRef;
    GameObject painelOpcoes;
    GameObject painelMulti;
    GameObject[]   painelAbas = new GameObject[4];
    Button[]       botoesAbas = new Button[4];
    CanvasGroup[]  abasCG     = new CanvasGroup[4];

    // refs para animação de abertura do painel de opções
    RectTransform opcoesCaixaRT;
    CanvasGroup   opcoesCaixaCG;
    CanvasGroup   opcoesOverlayCG;

    // ────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        Loc.OnLanguageChanged += OnIdiomaAlterado;

        canvasRef = CriarCanvas();

#if UNITY_EDITOR
        foreach (var a in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(
            "Assets/assets/UI/charselection/bar_charselect.png"))
            if (a is Sprite s) { sprBotao = s; break; }
        sprFundoOpcoes = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/assets/UI/bg_dungeon_forja.png");
        sprBotaoOpcoes = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/assets/UI/charselection/btn_stone.png");
        sprHudBar = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/assets/UI/hud/hud_bar.png");
        sprSlotFrame = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/assets/UI/hud/slot_frame.png");
        sprSliderTrack = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/assets/UI/settings/ui_slider_track.png");
        sprSliderFill = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/assets/UI/settings/ui_slider_fill.png");
        sprSliderKnob = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/assets/UI/settings/ui_slider_knob.png");
        sprToggleOn = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/assets/UI/settings/ui_toggle_on.png");
        sprToggleOff = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/assets/UI/settings/ui_toggle_off.png");
#endif

        AplicarConfiguracoesSalvas();

        // In-game (pause): só monta e abre o painel de opções, por cima de tudo.
        if (modoSomenteOpcoes)
        {
            var cv = canvasRef.GetComponent<Canvas>();
            if (cv != null) cv.sortingOrder = 200; // acima do HUD/pause
            AbrirOpcoes();
            return;
        }

        CriarFundo();
        CriarParticulas();

        CriarBotoes();
        CriarRodape();

        StartCoroutine(AnimarParticulas());

    }

    // Fecha o painel de opções quando usado in-game e limpa o helper.
    void FecharOpcoesInGame()
    {
        PlayerPrefs.Save();
        if (painelOpcoes != null) painelOpcoes.SetActive(false);
        aoFecharOpcoes?.Invoke();
        if (canvasRef != null) Destroy(canvasRef);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        Loc.OnLanguageChanged -= OnIdiomaAlterado;
    }

    void OnIdiomaAlterado(Language _)
    {
        StopAllCoroutines();
        if (canvasRef != null) UnityEngine.Object.Destroy(canvasRef);
        painelOpcoes = null;
        painelMulti  = null;
        for (int i = 0; i < 4; i++) { painelAbas[i] = null; botoesAbas[i] = null; }
        _langDisplayTxt = null;

        canvasRef = CriarCanvas();

        // in-game: reconstrói só o painel de opções (nunca o menu principal)
        if (modoSomenteOpcoes)
        {
            var cv = canvasRef.GetComponent<Canvas>();
            if (cv != null) cv.sortingOrder = 200;
            AbrirOpcoes();
            return;
        }

        CriarFundo();
        CriarParticulas();
        CriarBotoes();
        CriarRodape();
        StartCoroutine(AnimarParticulas());
    }

    // ── Canvas ──────────────────────────────────────────────────────────
    GameObject CriarCanvas()
    {
        var go = new GameObject("Canvas_Menu");
        var c  = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 10;
        var cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight  = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    void AplicarConfiguracoesSalvas()
    {
        AudioListener.volume      = PlayerPrefs.GetFloat("MasterVolume", 1f);
        Application.targetFrameRate = PlayerPrefs.GetInt("TargetFPS", 60);
        if (PlayerPrefs.HasKey("Fullscreen"))
            Screen.fullScreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        QualitySettings.vSyncCount = PlayerPrefs.GetInt("VSync", 0);
        if (PlayerPrefs.HasKey("QualityLevel"))
            QualitySettings.SetQualityLevel(PlayerPrefs.GetInt("QualityLevel", 2), true);
    }

    // ── Fundo simples ───────────────────────────────────────────────────
    void CriarFundo()
    {
        // base — tenta usar a imagem de fundo, senão usa cor sólida
        var goFundo = new GameObject("Fundo");
        goFundo.transform.SetParent(canvasRef.transform, false);
        var rtFundo = goFundo.AddComponent<RectTransform>();
        rtFundo.anchorMin = Vector2.zero; rtFundo.anchorMax = Vector2.one;
        rtFundo.offsetMin = rtFundo.offsetMax = Vector2.zero;
        var imgFundo = goFundo.AddComponent<Image>();

        Sprite bgSprite = null;
#if UNITY_EDITOR
        foreach (var a in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(
            "Assets/assets/UI/menu_inicial/IMG_6856 (1).png"))
            if (a is Sprite s) { bgSprite = s; break; }
#endif
        if (bgSprite != null)
        {
            imgFundo.sprite = bgSprite;
            imgFundo.type   = Image.Type.Simple;
            imgFundo.color  = Color.white;
            imgFundo.preserveAspect = false;
        }
        else
        {
            imgFundo.color = corFundo;
        }

        // overlay escuro sobre a imagem para legibilidade
        var goOverlay = new GameObject("Overlay");
        goOverlay.transform.SetParent(canvasRef.transform, false);
        var rtO = goOverlay.AddComponent<RectTransform>();
        rtO.anchorMin = Vector2.zero; rtO.anchorMax = Vector2.one;
        rtO.offsetMin = rtO.offsetMax = Vector2.zero;
        goOverlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

    }

    // ── Partículas ──────────────────────────────────────────────────────
    void CriarParticulas()
    {
        for (int i = 0; i < QTD_P; i++)
        {
            float sz = Random.Range(3f, 10f);
            var go = CriarImg($"P{i}", new Color(corAcento.r, corAcento.g, corAcento.b,
                Random.Range(0.05f, 0.20f)));
            var rt = go.GetComponent<RectTransform>();
            Vector2 pos = new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f));
            rt.anchorMin = rt.anchorMax = pos;
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(sz, sz);
            pRT[i]   = rt;
            pImg[i]  = go.GetComponent<Image>();
            pOrig[i] = pos;
            pFase[i] = Random.Range(0f, Mathf.PI * 2f);
            pVel[i]  = Random.Range(0.20f, 0.65f);
        }
    }

    // ── Logo "Spirit Mask" ──────────────────────────────────────────────
    GameObject CriarLogo()
    {
        var container = new GameObject("Logo");
        container.transform.SetParent(canvasRef.transform, false);
        var rc = container.AddComponent<RectTransform>();
        rc.anchorMin = new Vector2(0f, 0.76f); rc.anchorMax = new Vector2(1f, 0.98f);
        rc.offsetMin = rc.offsetMax = Vector2.zero;

        // sombra
        var sombra = CriarTexto(container, "Sombra",
            new Vector2(0.005f, -0.02f), new Vector2(1.005f, 0.98f),
            "Spirit Mask", 74f, FontStyles.Bold,
            new Color(corAcento.r, corAcento.g, corAcento.b, 0.45f));
        sombra.alignment = TextAlignmentOptions.Center;
        sombra.transform.SetSiblingIndex(0);

        // título
        var titulo = CriarTexto(container, "Titulo",
            Vector2.zero, Vector2.one,
            "Spirit Mask", 74f, FontStyles.Bold, Color.white);
        titulo.alignment = TextAlignmentOptions.Center;

        // subtítulo
        var sub = CriarTexto(container, "Sub",
            new Vector2(0.2f, -0.05f), new Vector2(0.8f, 0.18f),
            "Sobreviva à horda", 16f, FontStyles.Italic,
            new Color(0.70f, 0.58f, 0.90f));
        sub.alignment = TextAlignmentOptions.Center;

        return container;
    }

    // ── Botões ──────────────────────────────────────────────────────────
    void CriarBotoes()
    {
        CriarBotaoMenu(Loc.T("ui.start"),       0.40f, 0.52f, corAcento,                      20f, () => SceneManager.LoadScene(cenaSelecaoPersonagem), 0);
        CriarBotaoMenu(Loc.T("ui.multiplayer"), 0.28f, 0.40f, new Color(0.10f, 0.30f, 0.50f), 16f, AbrirMultijogador, 1);
        CriarBotaoMenu(Loc.T("ui.options"),     0.16f, 0.28f, new Color(0.20f, 0.20f, 0.38f), 16f, AbrirOpcoes, 2);
        CriarBotaoMenu(Loc.T("ui.quit"),        0.04f, 0.16f, new Color(0.40f, 0.06f, 0.06f), 16f, Sair, 3);
    }

    void CriarBotaoMenu(string label, float yMin, float yMax,
        Color corBorda, float fontSize, System.Action acao, int indice = 0)
    {
        var go = new GameObject($"Btn_{label.Trim()}");
        go.transform.SetParent(canvasRef.transform, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(0.04f, yMin);
        r.anchorMax = new Vector2(0.26f, yMax);
        r.offsetMin = r.offsetMax = Vector2.zero;

        // barra de acento lateral (aparece no hover)
        var barra = new GameObject("Barra");
        barra.transform.SetParent(go.transform, false);
        var rBarra = barra.AddComponent<RectTransform>();
        rBarra.anchorMin = Vector2.zero;
        rBarra.anchorMax = new Vector2(0f, 1f);
        rBarra.offsetMin = Vector2.zero;
        rBarra.offsetMax = new Vector2(5f, 0f);
        var imgBarra = barra.AddComponent<Image>();
        imgBarra.color = new Color(corBorda.r, corBorda.g, corBorda.b, 0f);

        var img = go.AddComponent<Image>();
        if (sprBotao != null)
        {
            img.sprite = sprBotao;
            img.type   = Image.Type.Simple;
            img.color  = Color.white;
            img.preserveAspect = false;
        }
        else
        {
            img.color = corBotao;
        }

        var txt = CriarTexto(go, "Txt", Vector2.zero, Vector2.one,
            label, fontSize, FontStyles.Bold, Color.white);
        txt.alignment = TextAlignmentOptions.Center;

        var btn = go.AddComponent<Button>();
        btn.transition    = Selectable.Transition.None;
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => acao());

        var hover = go.AddComponent<BotaoMenuHover>();
        hover.img       = img;
        hover.barraImg  = imgBarra;
        hover.txt       = txt;
        hover.corNormal = sprBotao != null ? Color.white : corBotao;
        hover.corHover  = sprBotao != null ? new Color(0.82f, 0.82f, 0.82f) : corBotaoHover;
        hover.corBarra  = corBorda;
        hover.bordaGO   = null;

        StartCoroutine(EntradaBotao(r, indice));
    }

    IEnumerator EntradaBotao(RectTransform rt, int indice)
    {
        var cg = rt.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        yield return new WaitForSeconds(indice * 0.09f);
        float dur = 0.40f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float ease = 1f - Mathf.Pow(1f - t / dur, 3f);
            rt.anchoredPosition = new Vector2(Mathf.Lerp(-260f, 0f, ease), 0f);
            cg.alpha = ease;
            yield return null;
        }
        rt.anchoredPosition = Vector2.zero;
        cg.alpha = 1f;
    }

    // ── Rodapé ──────────────────────────────────────────────────────────
    void CriarRodape()
    {
        var go = new GameObject("Rodape");
        go.transform.SetParent(canvasRef.transform, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = new Vector2(1f, 0.05f);
        r.offsetMin = r.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

        int nivel  = PlayerPrefs.GetInt("PlayerLevel", 1);
        int moedas = PlayerPrefs.GetInt("PlayerCoins", 0);
        CriarTexto(go, "Info", Vector2.zero, Vector2.one,
            "SPIRIT MASK",
            13f, FontStyles.Bold, new Color(0.55f, 0.45f, 0.70f))
            .alignment = TextAlignmentOptions.Center;
    }

    // ── Animações ────────────────────────────────────────────────────────
    IEnumerator AnimarLogo(GameObject container)
    {
        var rt     = container.GetComponent<RectTransform>();
        var textos = container.GetComponentsInChildren<TextMeshProUGUI>();

        // entrada: slide de baixo + fade
        float dur = 0.7f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float ease = 1f - Mathf.Pow(1f - t / dur, 3f);
            rt.anchoredPosition = new Vector2(0f, Mathf.Lerp(-25f, 0f, ease));
            foreach (var tx in textos) tx.alpha = ease;
            yield return null;
        }
        rt.anchoredPosition = Vector2.zero;
        foreach (var tx in textos) tx.alpha = 1f;

        // pulso suave em loop
        float tempo = 0f;
        while (container != null)
        {
            tempo += Time.deltaTime * 1.0f;
            float s = 1f + Mathf.Sin(tempo) * 0.009f;
            rt.localScale = Vector3.one * s;
            yield return null;
        }
    }

    IEnumerator AnimarParticulas()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime;
            for (int i = 0; i < QTD_P; i++)
            {
                if (pRT[i] == null) yield break;
                float ox = pOrig[i].x + Mathf.Sin(t * pVel[i] + pFase[i]) * 0.025f;
                float oy = pOrig[i].y + Mathf.Cos(t * pVel[i] * 0.7f + pFase[i]) * 0.030f;
                pRT[i].anchorMin = pRT[i].anchorMax = new Vector2(ox, oy);
                float a = Mathf.Abs(Mathf.Sin(t * pVel[i] + pFase[i])) * 0.18f + 0.03f;
                var c = pImg[i].color;
                pImg[i].color = new Color(c.r, c.g, c.b, a);
            }
            yield return null;
        }
    }

    // ── Multijogador ─────────────────────────────────────────────────────
    void AbrirMultijogador()
    {
        if (painelMulti == null) CriarPainelMultijogador();
        painelMulti.SetActive(true);
    }

    void CriarPainelMultijogador()
    {
        painelMulti = new GameObject("PainelMulti");
        painelMulti.transform.SetParent(canvasRef.transform, false);

        Esticar(AdicionarImagem(painelMulti, new Color(0f, 0f, 0f, 0.82f)));

        // moldura vermelha — IRMÃO atrás do painel (o Image do pai fica atrás dos filhos,
        // então a borda precisa ser um irmão anterior, senão cobre todo o fundo).
        var moldura = new GameObject("Moldura");
        moldura.transform.SetParent(painelMulti.transform, false);
        var rmd = moldura.AddComponent<RectTransform>();
        rmd.anchorMin = new Vector2(0.25f, 0.20f); rmd.anchorMax = new Vector2(0.75f, 0.82f);
        rmd.offsetMin = new Vector2(-3f,-3f); rmd.offsetMax = new Vector2(3f,3f);
        moldura.AddComponent<Image>().color = new Color(dfCorBorda.r, dfCorBorda.g, dfCorBorda.b, 0.95f);

        var painel = new GameObject("Painel");
        painel.transform.SetParent(painelMulti.transform, false);
        var rp = painel.AddComponent<RectTransform>();
        rp.anchorMin = new Vector2(0.25f, 0.20f); rp.anchorMax = new Vector2(0.75f, 0.82f);
        rp.offsetMin = rp.offsetMax = Vector2.zero;
        painel.AddComponent<Image>().color = new Color(0.07f, 0.045f, 0.05f); // fundo forja (preto avermelhado)

        BarraTopo(painel, dfCorBorda);
        CriarTexto(painel, "Titulo", new Vector2(0f,0.84f), new Vector2(1f,1f),
            Loc.T("multi.title"), 26f, FontStyles.Bold, Color.white);

        // emblema (losango/gema) no lugar do emoji 🌐 que a fonte pixel não renderiza
        CriarEmblemaMulti(painel);

        CriarTexto(painel, "LblCodigo", new Vector2(0.05f,0.50f), new Vector2(0.95f,0.60f),
            Loc.T("multi.room_code"), 13f, FontStyles.Bold, new Color(0.92f,0.74f,0.40f));

        // moldura do campo (atrás)
        var campoBorda = new GameObject("CampoBorda");
        campoBorda.transform.SetParent(painel.transform, false);
        var rcb = campoBorda.AddComponent<RectTransform>();
        rcb.anchorMin = new Vector2(0.05f,0.39f); rcb.anchorMax = new Vector2(0.95f,0.50f);
        rcb.offsetMin = new Vector2(-2f,-2f); rcb.offsetMax = new Vector2(2f,2f);
        campoBorda.AddComponent<Image>().color = new Color(dfCorBorda.r,dfCorBorda.g,dfCorBorda.b,0.70f);

        var campo = new GameObject("Campo");
        campo.transform.SetParent(painel.transform, false);
        var rca = campo.AddComponent<RectTransform>();
        rca.anchorMin = new Vector2(0.05f,0.39f); rca.anchorMax = new Vector2(0.95f,0.50f);
        rca.offsetMin = rca.offsetMax = Vector2.zero;
        campo.AddComponent<Image>().color = new Color(0.10f,0.06f,0.055f);
        var ph = CriarTexto(campo,"PH",new Vector2(0.03f,0f),new Vector2(0.97f,1f),
            "Ex: SPIRIT-1234",14f,FontStyles.Italic,new Color(0.5f,0.42f,0.40f));
        ph.alignment = TextAlignmentOptions.Left;

        // Criar = laranja-brasa (ação primária); Entrar = vermelho (ação secundária)
        BotaoSimples(painel,Loc.T("multi.create_room"),new Vector2(0.05f,0.23f),new Vector2(0.47f,0.37f),
            new Color(0.72f,0.34f,0.10f),()=>{
                PlayerPrefs.SetInt("LobbyHost",1);
                PlayerPrefs.SetString("LobbyCode","SPIRIT-" + GerarCodigoSala());
                SceneManager.LoadScene("lobby");
            });
        BotaoSimples(painel,Loc.T("multi.join_room"),new Vector2(0.53f,0.23f),new Vector2(0.95f,0.37f),
            new Color(0.62f,0.16f,0.14f),()=>{
                PlayerPrefs.SetInt("LobbyHost",0);
                SceneManager.LoadScene("lobby");
            });

        CriarTexto(painel,"Aviso",new Vector2(0.05f,0.10f),new Vector2(0.95f,0.22f),
            Loc.T("multi.notice"),11f,FontStyles.Italic,new Color(0.85f,0.66f,0.30f));
        BotaoSimples(painel,"← " + Loc.T("ui.back"),new Vector2(0.10f,0.02f),new Vector2(0.90f,0.10f),
            new Color(0.28f,0.08f,0.08f),()=>painelMulti.SetActive(false));
    }

    // Emblema decorativo (losango em camadas) — substitui o emoji 🌐 não suportado pela fonte.
    void CriarEmblemaMulti(GameObject painel)
    {
        var emb = new GameObject("Emblema");
        emb.transform.SetParent(painel.transform, false);
        var re = emb.AddComponent<RectTransform>();
        re.anchorMin = new Vector2(0.5f,0.71f); re.anchorMax = new Vector2(0.5f,0.71f);
        re.pivot = new Vector2(0.5f,0.5f); re.anchoredPosition = Vector2.zero;
        re.sizeDelta = new Vector2(58f,58f);
        re.localRotation = Quaternion.Euler(0f,0f,45f); // vira losango
        emb.AddComponent<Image>().color = new Color(dfCorBorda.r,dfCorBorda.g,dfCorBorda.b,1f); // borda vermelha

        var inner = new GameObject("Inner");
        inner.transform.SetParent(emb.transform, false);
        var ri = inner.AddComponent<RectTransform>();
        ri.anchorMin = Vector2.zero; ri.anchorMax = Vector2.one;
        ri.offsetMin = new Vector2(4f,4f); ri.offsetMax = new Vector2(-4f,-4f);
        inner.AddComponent<Image>().color = new Color(0.10f,0.06f,0.05f,1f); // interior escuro

        var core = new GameObject("Core");
        core.transform.SetParent(inner.transform, false);
        var rco = core.AddComponent<RectTransform>();
        rco.anchorMin = new Vector2(0.5f,0.5f); rco.anchorMax = new Vector2(0.5f,0.5f);
        rco.pivot = new Vector2(0.5f,0.5f); rco.anchoredPosition = Vector2.zero;
        rco.sizeDelta = new Vector2(20f,20f);
        core.AddComponent<Image>().color = new Color(0.86f,0.40f,0.12f,1f); // núcleo brasa
    }

    // ── Opções ───────────────────────────────────────────────────────────
    void AbrirOpcoes()
    {
        if (painelOpcoes == null) CriarPainelOpcoes();
        painelOpcoes.SetActive(true);
        StartCoroutine(AnimarAberturaOpcoes());
    }

    IEnumerator AnimarAberturaOpcoes()
    {
        const float dur = 0.30f;
        if (opcoesCaixaRT != null) opcoesCaixaRT.localScale = Vector3.one * 0.82f;
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            float p = t / dur;
            if (opcoesOverlayCG != null) opcoesOverlayCG.alpha = Mathf.Lerp(0f, 1f, p);
            if (opcoesCaixaCG   != null) opcoesCaixaCG.alpha   = Mathf.Clamp01(p * 1.7f);
            if (opcoesCaixaRT   != null)
            {
                float s = Mathf.Lerp(0.82f, 1f, EaseOutBackDF(p));
                opcoesCaixaRT.localScale = new Vector3(s, s, 1f);
            }
            yield return null;
        }
        if (opcoesOverlayCG != null) opcoesOverlayCG.alpha = 1f;
        if (opcoesCaixaCG   != null) opcoesCaixaCG.alpha   = 1f;
        if (opcoesCaixaRT   != null) opcoesCaixaRT.localScale = Vector3.one;

        // anima a aba ativa logo após a entrada
        for (int i = 0; i < 4; i++)
            if (painelAbas[i] != null && painelAbas[i].activeSelf)
                StartCoroutine(AnimarConteudoAba(i));
    }

    static float EaseOutBackDF(float t)
    {
        const float c1 = 1.70158f, c3 = 1.70158f + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    IEnumerator AnimarConteudoAba(int idx)
    {
        var cg = abasCG[idx];
        var rt = painelAbas[idx] != null ? painelAbas[idx].GetComponent<RectTransform>() : null;
        if (cg == null) yield break;

        const float dur = 0.22f;
        Vector2 baseAnchored = rt != null ? rt.anchoredPosition : Vector2.zero;
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            float p = t / dur;
            float e = 1f - Mathf.Pow(1f - p, 3f);
            cg.alpha = e;
            if (rt != null) rt.anchoredPosition = baseAnchored + new Vector2(0f, Mathf.Lerp(14f, 0f, e));
            yield return null;
        }
        cg.alpha = 1f;
        if (rt != null) rt.anchoredPosition = baseAnchored;
    }

    // paleta vermelho escuro + preto + branco só para o painel de opções
    static readonly Color dfCorTopo    = new Color(0.06f, 0.02f, 0.02f);        // preto avermelhado
    static readonly Color dfCorBorda   = new Color(0.62f, 0.11f, 0.11f);        // vermelho escuro
    static readonly Color dfCorAbaAtv  = new Color(0.80f, 0.16f, 0.16f, 1.0f);  // aba ativa: vermelho vivo
    static readonly Color dfCorAbaInv  = new Color(0.11f, 0.07f, 0.07f, 0.90f); // aba inativa: quase preto
    static readonly Color dfCorTitulo  = new Color(1.00f, 1.00f, 1.00f);        // branco puro (selecionado)
    static readonly Color dfCorTexto   = new Color(0.92f, 0.90f, 0.90f);        // branco suave (rótulos)
    static readonly Color dfCorTxtInv  = new Color(0.55f, 0.48f, 0.48f);        // texto de aba não-selecionada (apagado)

    void CriarPainelOpcoes()
    {
        painelOpcoes = new GameObject("PainelOpcoes");
        painelOpcoes.transform.SetParent(canvasRef.transform, false);
        Esticar(AdicionarImagem(painelOpcoes, new Color(0f,0f,0f,0.78f)));
        opcoesOverlayCG = painelOpcoes.AddComponent<CanvasGroup>();

        // ── Caixa central ──
        var painel = new GameObject("Painel");
        painel.transform.SetParent(painelOpcoes.transform, false);
        var rp = painel.AddComponent<RectTransform>();
        rp.anchorMin = new Vector2(0.22f,0.08f); rp.anchorMax = new Vector2(0.78f,0.92f);
        rp.offsetMin = rp.offsetMax = Vector2.zero;
        opcoesCaixaRT = rp;
        opcoesCaixaCG = painel.AddComponent<CanvasGroup>();
        var imgPainel = painel.AddComponent<Image>();
        if (sprFundoOpcoes != null)
        {
            imgPainel.sprite = sprFundoOpcoes;
            imgPainel.type   = Image.Type.Simple;
            imgPainel.color  = new Color(0.88f, 0.82f, 0.80f, 0.99f);
        }
        else
        {
            imgPainel.color = new Color(0.04f, 0.02f, 0.02f, 0.98f);
        }

        // brasas subindo no fundo (atrás do conteúdo, sobre a imagem da forja)
        var brasasGO = new GameObject("Brasas");
        brasasGO.transform.SetParent(painel.transform, false);
        var rbr = brasasGO.AddComponent<RectTransform>();
        rbr.anchorMin = new Vector2(0.02f, 0.10f); rbr.anchorMax = new Vector2(0.98f, 0.80f);
        rbr.offsetMin = rbr.offsetMax = Vector2.zero;
        brasasGO.AddComponent<BrasasSubindo>();

        // bordas douradas
        DFBorda(painel,"BT",new Vector2(0f,1f),new Vector2(1f,1f),new Vector2(0f,-2f),Vector2.zero);
        DFBorda(painel,"BB",new Vector2(0f,0f),new Vector2(1f,0f),Vector2.zero,new Vector2(0f,2f));
        DFBorda(painel,"BE",new Vector2(0f,0f),new Vector2(0f,1f),Vector2.zero,new Vector2(2f,0f));
        DFBorda(painel,"BD",new Vector2(1f,0f),new Vector2(1f,1f),new Vector2(-2f,0f),Vector2.zero);

        // barra de topo
        var barraTopo = new GameObject("BarraTopoDF");
        barraTopo.transform.SetParent(painel.transform, false);
        var rbt = barraTopo.AddComponent<RectTransform>();
        rbt.anchorMin = new Vector2(0f,0.90f); rbt.anchorMax = Vector2.one;
        rbt.offsetMin = rbt.offsetMax = Vector2.zero;
        var imgBT = barraTopo.AddComponent<Image>();
        if (sprBotao != null)
        {
            imgBT.sprite = sprBotao; imgBT.type = Image.Type.Simple;
            imgBT.color  = new Color(0.50f, 0.40f, 0.38f, 1f);
        }
        else imgBT.color = dfCorTopo;

        // linha separadora dourada
        DFSep(painel, new Vector2(0f,0.898f), new Vector2(1f,0.902f));

        // acento carmesim esquerdo
        var ac = new GameObject("Acento");
        ac.transform.SetParent(painel.transform, false);
        var rac = ac.AddComponent<RectTransform>();
        rac.anchorMin=new Vector2(0f,0.90f); rac.anchorMax=new Vector2(0.005f,1f);
        rac.offsetMin=rac.offsetMax=Vector2.zero;
        ac.AddComponent<Image>().color = new Color(0.55f,0.08f,0.08f);

        // título
        var t = CriarTexto(painel,"Titulo",new Vector2(0f,0.90f),new Vector2(1f,1f),
            Loc.T("settings.title"),20f,FontStyles.Bold,dfCorTitulo);
        t.alignment = TextAlignmentOptions.Center;

        // ── Abas ──
        string[] nomes = {Loc.T("ui.audio"), Loc.T("ui.video"), Loc.T("ui.game"), Loc.T("settings.controls")};
        for (int i = 0; i < 4; i++)
        {
            int idx = i;
            var abaGO = new GameObject($"Aba{i}");
            abaGO.transform.SetParent(painel.transform, false);
            var ra = abaGO.AddComponent<RectTransform>();
            ra.anchorMin = new Vector2(idx/4f+0.004f,0.82f);
            ra.anchorMax = new Vector2((idx+1)/4f-0.004f,0.90f);
            ra.offsetMin = new Vector2(2f,2f); ra.offsetMax = new Vector2(-2f,0f);
            var imgA = abaGO.AddComponent<Image>();
            if (sprBotaoOpcoes != null)
            {
                imgA.sprite = sprBotaoOpcoes; imgA.type = Image.Type.Simple;
                imgA.color  = i==0 ? dfCorAbaAtv : dfCorAbaInv;
            }
            else imgA.color = i==0 ? corAcento : new Color(0.14f,0.10f,0.28f);

            // borda superior (destaque na aba ativa)
            var bordaAba = new GameObject("BordaAba");
            bordaAba.transform.SetParent(abaGO.transform, false);
            var rb = bordaAba.AddComponent<RectTransform>();
            rb.anchorMin=new Vector2(0f,1f); rb.anchorMax=new Vector2(1f,1f);
            rb.offsetMin=new Vector2(0f,-3f); rb.offsetMax=Vector2.zero;
            bordaAba.AddComponent<Image>().color = i==0
                ? new Color(dfCorBorda.r,dfCorBorda.g,dfCorBorda.b,1f)
                : new Color(dfCorBorda.r,dfCorBorda.g,dfCorBorda.b,0.25f);

            var btnA = abaGO.AddComponent<Button>();
            btnA.targetGraphic=imgA; btnA.transition=Selectable.Transition.None;
            btnA.onClick.AddListener(()=>MudarAba(idx));
            abaGO.AddComponent<CardHover>();
            var txA = CriarTexto(abaGO,"T",Vector2.zero,Vector2.one,nomes[i],13f,FontStyles.Bold,
                i==0 ? dfCorTitulo : dfCorTxtInv);
            txA.alignment = TextAlignmentOptions.Center;
            botoesAbas[i]=btnA;

            var cont = new GameObject($"Cont{i}");
            cont.transform.SetParent(painel.transform,false);
            var rc = cont.AddComponent<RectTransform>();
            rc.anchorMin=new Vector2(0.02f,0.13f); rc.anchorMax=new Vector2(0.98f,0.82f);
            rc.offsetMin=rc.offsetMax=Vector2.zero;
            abasCG[i]=cont.AddComponent<CanvasGroup>();
            painelAbas[i]=cont;
            cont.SetActive(i==0);
        }

        // separador abaixo das abas
        DFSep(painel, new Vector2(0.01f,0.820f), new Vector2(0.99f,0.822f));

        PopularAudio(painelAbas[0]);
        PopularVideo(painelAbas[1]);
        PopularJogo(painelAbas[2]);
        PopularControles(painelAbas[3]);

        BotaoSimples(painel,Loc.T("ui.back"),new Vector2(0.08f,0.02f),new Vector2(0.92f,0.12f),
            new Color(0.10f,0.07f,0.05f),()=>{
                if (modoSomenteOpcoes) { FecharOpcoesInGame(); }
                else { PlayerPrefs.Save(); painelOpcoes.SetActive(false); }
            });
    }

    void DFBorda(GameObject pai, string nome, Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax)
    {
        var go=new GameObject(nome); go.transform.SetParent(pai.transform,false);
        var r=go.AddComponent<RectTransform>();
        r.anchorMin=ancMin; r.anchorMax=ancMax; r.offsetMin=offMin; r.offsetMax=offMax;
        go.AddComponent<Image>().color=dfCorBorda;
    }

    void DFSep(GameObject pai, Vector2 ancMin, Vector2 ancMax)
    {
        var go=new GameObject("Sep"); go.transform.SetParent(pai.transform,false);
        var r=go.AddComponent<RectTransform>();
        r.anchorMin=ancMin; r.anchorMax=ancMax; r.offsetMin=r.offsetMax=Vector2.zero;
        go.AddComponent<Image>().color=new Color(dfCorBorda.r,dfCorBorda.g,dfCorBorda.b,0.5f);
    }

    void MudarAba(int idx)
    {
        for (int i=0;i<4;i++)
        {
            bool jaAtiva = painelAbas[i].activeSelf;
            painelAbas[i].SetActive(i==idx);
            if (i==idx && !jaAtiva) StartCoroutine(AnimarConteudoAba(i));
            var imgAba = botoesAbas[i].GetComponent<Image>();
            imgAba.color = i==idx ? dfCorAbaAtv : dfCorAbaInv;

            var bordaAba = botoesAbas[i].transform.Find("BordaAba");
            if (bordaAba != null)
            {
                var ib = bordaAba.GetComponent<Image>();
                if (ib != null) ib.color = i==idx
                    ? new Color(dfCorBorda.r,dfCorBorda.g,dfCorBorda.b,1f)
                    : new Color(dfCorBorda.r,dfCorBorda.g,dfCorBorda.b,0.25f);
            }

            var txt = botoesAbas[i].GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.color = i==idx ? dfCorTitulo : dfCorTxtInv;
        }
    }

    void PopularAudio(GameObject p)
    {
        Rotulo(p,Loc.T("settings.master"),0.80f,0.92f);
        var sv=Slider(p,"SV",new Vector2(0.05f,0.68f),new Vector2(0.95f,0.80f),PlayerPrefs.GetFloat("MasterVolume",1f));
        sv.onValueChanged.AddListener(v=>{AudioListener.volume=v;PlayerPrefs.SetFloat("MasterVolume",v);});

        Rotulo(p,Loc.T("settings.music"),0.56f,0.68f);
        var sm=Slider(p,"SM",new Vector2(0.05f,0.44f),new Vector2(0.95f,0.56f),PlayerPrefs.GetFloat("MusicVolume",0.8f));
        sm.onValueChanged.AddListener(v=>PlayerPrefs.SetFloat("MusicVolume",v));

        Rotulo(p,Loc.T("settings.sfx"),0.32f,0.44f);
        var ss=Slider(p,"SS",new Vector2(0.05f,0.20f),new Vector2(0.95f,0.32f),PlayerPrefs.GetFloat("SFXVolume",1f));
        ss.onValueChanged.AddListener(v=>PlayerPrefs.SetFloat("SFXVolume",v));
    }

    void PopularVideo(GameObject p)
    {
        Rotulo(p,Loc.T("settings.fullscreen"),0.80f,0.92f);
        Toggle(p,"TF",new Vector2(0.70f,0.80f),new Vector2(0.90f,0.92f),
            Screen.fullScreen,v=>{Screen.fullScreen=v;PlayerPrefs.SetInt("Fullscreen",v?1:0);});

        Rotulo(p,"VSync",0.64f,0.76f);
        Toggle(p,"TV",new Vector2(0.70f,0.64f),new Vector2(0.90f,0.76f),
            QualitySettings.vSyncCount>0,v=>{QualitySettings.vSyncCount=v?1:0;PlayerPrefs.SetInt("VSync",v?1:0);});

        Rotulo(p,Loc.T("settings.quality"),0.46f,0.60f);
        BotoesQualidade(p,new Vector2(0.05f,0.34f),new Vector2(0.95f,0.46f));

        Rotulo(p,Loc.T("settings.fps_limit"),0.20f,0.32f);
        var sf=Slider(p,"SF",new Vector2(0.05f,0.08f),new Vector2(0.80f,0.20f),
            Mathf.InverseLerp(30f,240f,PlayerPrefs.GetInt("TargetFPS",60)));
        var tf=CriarTexto(p,"TF2",new Vector2(0.82f,0.08f),new Vector2(0.95f,0.20f),
            $"{PlayerPrefs.GetInt("TargetFPS",60)}",13f,FontStyles.Normal,dfCorTitulo);
        sf.onValueChanged.AddListener(v=>{
            int fps=Mathf.RoundToInt(Mathf.Lerp(30f,240f,v));
            Application.targetFrameRate=fps;
            PlayerPrefs.SetInt("TargetFPS",fps);
            tf.text=$"{fps}";
        });
    }

    void PopularJogo(GameObject p)
    {
        // Idioma — topo da aba JOGO
        Rotulo(p, Loc.T("settings.language"), 0.82f, 0.94f);
        SeletorIdiomaRow(p, new Vector2(0.05f, 0.70f), new Vector2(0.95f, 0.82f));

        Rotulo(p,Loc.T("settings.show_tutorial"),0.54f,0.66f);
        Toggle(p,"TT",new Vector2(0.70f,0.54f),new Vector2(0.90f,0.66f),
            PlayerPrefs.GetInt("TutorialVisto",0)==0,
            v=>PlayerPrefs.SetInt("TutorialVisto",v?0:1));

        Rotulo(p,Loc.T("settings.show_fps"),0.38f,0.50f);
        Toggle(p,"TF",new Vector2(0.70f,0.38f),new Vector2(0.90f,0.50f),
            PlayerPrefs.GetInt("ShowFPS",0)==1,
            v=>PlayerPrefs.SetInt("ShowFPS",v?1:0));

        Rotulo(p,Loc.T("settings.camera_shake"),0.22f,0.34f);
        Toggle(p,"TS",new Vector2(0.70f,0.22f),new Vector2(0.90f,0.34f),
            PlayerPrefs.GetInt("CameraShake",1)==1,
            v=>PlayerPrefs.SetInt("CameraShake",v?1:0));
    }

    // ── Aba Controles (remapeamento de teclas) ──────────────────────────────
    InputBindings.Acao? rebindAlvo = null;
    int rebindArmadoFrame = -1;
    TextMeshProUGUI[] rebindTextos = new TextMeshProUGUI[6];

    void PopularControles(GameObject p)
    {
        LinhaRebind(p, InputBindings.Acao.Cima,     Loc.T("settings.move_up"),    0.86f, 0.97f);
        LinhaRebind(p, InputBindings.Acao.Baixo,    Loc.T("settings.move_down"),  0.72f, 0.83f);
        LinhaRebind(p, InputBindings.Acao.Esquerda, Loc.T("settings.move_left"),  0.58f, 0.69f);
        LinhaRebind(p, InputBindings.Acao.Direita,  Loc.T("settings.move_right"), 0.44f, 0.55f);
        LinhaRebind(p, InputBindings.Acao.Dash,     Loc.T("settings.dash"),       0.30f, 0.41f);
        LinhaRebind(p, InputBindings.Acao.Ultimate, Loc.T("settings.ultimate"),   0.16f, 0.27f);

        BotaoSimples(p, Loc.T("settings.reset_controls"),
            new Vector2(0.10f,0.03f), new Vector2(0.90f,0.14f),
            new Color(0.10f,0.07f,0.05f), () =>
            {
                InputBindings.ResetarPadrao();
                for (int i = 0; i < 6; i++) AtualizarTextoRebind((InputBindings.Acao)i);
            });
    }

    void LinhaRebind(GameObject p, InputBindings.Acao acao, string label, float yMin, float yMax)
    {
        Rotulo(p, label, yMin, yMax);

        // botão da tecla à direita (container sem Image; filhos renderizam por cima)
        var go = new GameObject("Key_" + acao); go.transform.SetParent(p.transform, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(0.66f, yMin + 0.005f); r.anchorMax = new Vector2(0.96f, yMax - 0.005f);
        r.offsetMin = r.offsetMax = Vector2.zero;

        var brd = new GameObject("Brd"); brd.transform.SetParent(go.transform, false);
        var rb = brd.AddComponent<RectTransform>(); rb.anchorMin = Vector2.zero; rb.anchorMax = Vector2.one;
        rb.offsetMin = new Vector2(-1f,-1f); rb.offsetMax = new Vector2(1f,1f);
        brd.AddComponent<Image>().color = new Color(dfCorBorda.r, dfCorBorda.g, dfCorBorda.b, 0.75f);

        var corpo = new GameObject("Corpo"); corpo.transform.SetParent(go.transform, false);
        var rc = corpo.AddComponent<RectTransform>(); rc.anchorMin = Vector2.zero; rc.anchorMax = Vector2.one;
        rc.offsetMin = rc.offsetMax = Vector2.zero;
        var img = corpo.AddComponent<Image>(); img.color = new Color(0.12f,0.08f,0.05f,0.96f);

        var txt = CriarTexto(go, "T", Vector2.zero, Vector2.one,
            InputBindings.NomeTecla(InputBindings.Get(acao)), 13f, FontStyles.Bold, dfCorTitulo);
        txt.alignment = TextAlignmentOptions.Center;
        rebindTextos[(int)acao] = txt;

        go.AddComponent<CardHover>();
        var btn = go.AddComponent<Button>(); btn.targetGraphic = img; btn.transition = Selectable.Transition.None;
        var acaoLocal = acao;
        btn.onClick.AddListener(() => IniciarRebind(acaoLocal));
    }

    void IniciarRebind(InputBindings.Acao acao)
    {
        rebindAlvo = acao;
        rebindArmadoFrame = Time.frameCount;
        if (rebindTextos[(int)acao] != null)
            rebindTextos[(int)acao].text = Loc.T("settings.press_key");
    }

    void AtualizarTextoRebind(InputBindings.Acao acao)
    {
        if (rebindTextos[(int)acao] != null)
            rebindTextos[(int)acao].text = InputBindings.NomeTecla(InputBindings.Get(acao));
    }

    void Update()
    {
        if (rebindAlvo == null) return;
        if (Time.frameCount <= rebindArmadoFrame) return; // ignora o frame do clique
        if (!Input.anyKeyDown) return;

        var acao = rebindAlvo.Value;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            AtualizarTextoRebind(acao);   // cancela
            rebindAlvo = null;
            return;
        }

        foreach (KeyCode kc in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (!InputBindings.EhTeclaValida(kc)) continue;
            if (Input.GetKeyDown(kc))
            {
                InputBindings.Set(acao, kc);
                AtualizarTextoRebind(acao);
                rebindAlvo = null;
                break;
            }
        }
    }

    void SeletorIdiomaRow(GameObject p, Vector2 mn, Vector2 mx)
    {
        _langPreviewIdx = (int)Loc.Current;

        var row = new GameObject("IdiomaRow");
        row.transform.SetParent(p.transform, false);
        var r = row.AddComponent<RectTransform>();
        r.anchorMin = mn; r.anchorMax = mx; r.offsetMin = r.offsetMax = Vector2.zero;

        // < (0-12%)
        BotaoSetaDF(row, "<", new Vector2(0f, 0f), new Vector2(0.12f, 1f), () => CiclarIdioma(-1));

        // Display (12-72%)
        var display = new GameObject("Display");
        display.transform.SetParent(row.transform, false);
        var rd = display.AddComponent<RectTransform>();
        rd.anchorMin = new Vector2(0.12f, 0f); rd.anchorMax = new Vector2(0.72f, 1f);
        rd.offsetMin = rd.offsetMax = Vector2.zero;
        display.AddComponent<Image>().color = new Color(0.04f, 0.02f, 0.02f, 0.60f);
        _langDisplayTxt = CriarTexto(display, "T", Vector2.zero, Vector2.one,
            Loc.NativeName(Loc.Current), 13f, FontStyles.Bold, dfCorTitulo);
        _langDisplayTxt.alignment = TextAlignmentOptions.Center;

        // > (72-84%)
        BotaoSetaDF(row, ">", new Vector2(0.72f, 0f), new Vector2(0.84f, 1f), () => CiclarIdioma(+1));

        // OK (84-100%) — verde âmbar para destacar
        var okGo = new GameObject("BtnOK");
        okGo.transform.SetParent(row.transform, false);
        var rokGo = okGo.AddComponent<RectTransform>();
        rokGo.anchorMin = new Vector2(0.84f, 0f); rokGo.anchorMax = Vector2.one;
        rokGo.offsetMin = rokGo.offsetMax = Vector2.zero;

        var okBorda = new GameObject("Borda"); okBorda.transform.SetParent(okGo.transform, false);
        var rOkB = okBorda.AddComponent<RectTransform>(); rOkB.anchorMin=Vector2.zero; rOkB.anchorMax=Vector2.one; rOkB.offsetMin=new Vector2(-1f,-1f); rOkB.offsetMax=new Vector2(1f,1f);
        okBorda.AddComponent<Image>().color = new Color(0.30f, 0.55f, 0.20f, 0.90f);

        var okCorpo = new GameObject("Corpo"); okCorpo.transform.SetParent(okGo.transform, false);
        var rOkC = okCorpo.AddComponent<RectTransform>(); rOkC.anchorMin=Vector2.zero; rOkC.anchorMax=Vector2.one; rOkC.offsetMin=rOkC.offsetMax=Vector2.zero;
        var okImg = okCorpo.AddComponent<Image>(); okImg.color = new Color(0.12f, 0.30f, 0.08f);

        CriarTexto(okGo, "T", Vector2.zero, Vector2.one, "OK", 13f, FontStyles.Bold,
            new Color(0.70f, 0.95f, 0.55f)).alignment = TextAlignmentOptions.Center;

        var okBtn = okGo.AddComponent<Button>(); okBtn.targetGraphic = okImg;
        var okColors = ColorBlock.defaultColorBlock;
        okColors.normalColor      = new Color(0.12f, 0.30f, 0.08f);
        okColors.highlightedColor = new Color(0.20f, 0.48f, 0.12f);
        okColors.pressedColor     = new Color(0.06f, 0.16f, 0.04f);
        okColors.colorMultiplier  = 1f;
        okBtn.colors = okColors;
        okBtn.onClick.AddListener(AplicarIdioma);
    }

    void CiclarIdioma(int dir)
    {
        int count = System.Enum.GetValues(typeof(Language)).Length;
        _langPreviewIdx = (_langPreviewIdx + dir + count) % count;
        if (_langDisplayTxt != null)
            _langDisplayTxt.text = Loc.NativeName((Language)_langPreviewIdx);
    }

    void AplicarIdioma()
    {
        Loc.Current = (Language)_langPreviewIdx; // dispara OnIdiomaAlterado -> reload
    }

    void BotaoSetaDF(GameObject pai, string seta, Vector2 mn, Vector2 mx, System.Action acao)
    {
        var go = new GameObject("Btn" + seta);
        go.transform.SetParent(pai.transform, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = mn; r.anchorMax = mx; r.offsetMin = r.offsetMax = Vector2.zero;

        var borda = new GameObject("Borda"); borda.transform.SetParent(go.transform, false);
        var rb = borda.AddComponent<RectTransform>(); rb.anchorMin = Vector2.zero; rb.anchorMax = Vector2.one;
        rb.offsetMin = new Vector2(-1f,-1f); rb.offsetMax = new Vector2(1f,1f);
        borda.AddComponent<Image>().color = new Color(dfCorBorda.r, dfCorBorda.g, dfCorBorda.b, 0.88f);

        var corpo = new GameObject("Corpo"); corpo.transform.SetParent(go.transform, false);
        var rc = corpo.AddComponent<RectTransform>(); rc.anchorMin = Vector2.zero; rc.anchorMax = Vector2.one;
        rc.offsetMin = rc.offsetMax = Vector2.zero;
        var img = corpo.AddComponent<Image>(); img.color = new Color(0.10f, 0.07f, 0.05f);

        CriarTexto(go, "T", Vector2.zero, Vector2.one, seta, 15f, FontStyles.Bold, dfCorTitulo)
            .alignment = TextAlignmentOptions.Center;

        var btn = go.AddComponent<Button>(); btn.targetGraphic = img;
        var cs = ColorBlock.defaultColorBlock;
        cs.normalColor     = new Color(0.10f, 0.07f, 0.05f);
        cs.highlightedColor = new Color(0.28f, 0.20f, 0.07f);
        cs.pressedColor    = new Color(0.05f, 0.03f, 0.01f);
        cs.colorMultiplier = 1f;
        btn.colors = cs;
        btn.onClick.AddListener(() => acao());
    }

    void BotoesQualidade(GameObject p, Vector2 mn, Vector2 mx)
    {
        string[] n={Loc.T("settings.quality.low"),Loc.T("settings.quality.medium"),Loc.T("settings.quality.high"),Loc.T("settings.quality.ultra")};
        int atual=QualitySettings.GetQualityLevel();
        Color corAtv  = new Color(0.52f,0.11f,0.11f);  // selecionado: vermelho vivo
        Color corInav = new Color(0.10f,0.06f,0.06f);  // não-selecionado: quase preto
        for(int i=0;i<4;i++){
            int idx=i;
            float x0=mn.x+i*(mx.x-mn.x)/4f+0.004f;
            float x1=mn.x+(i+1)*(mx.x-mn.x)/4f-0.004f;

            // container sem Image — mesma lógica de sibling do BotaoSimples
            var go=new GameObject($"Q{i}"); go.transform.SetParent(p.transform,false);
            var r=go.AddComponent<RectTransform>();
            r.anchorMin=new Vector2(x0,mn.y); r.anchorMax=new Vector2(x1,mx.y);
            r.offsetMin=r.offsetMax=Vector2.zero;

            // filho 0: borda dourada (atrás)
            var bordaQ=new GameObject("BQ"); bordaQ.transform.SetParent(go.transform,false);
            var rbq=bordaQ.AddComponent<RectTransform>(); rbq.anchorMin=Vector2.zero; rbq.anchorMax=Vector2.one;
            rbq.offsetMin=new Vector2(-1f,-1f); rbq.offsetMax=new Vector2(1f,1f);
            bordaQ.AddComponent<Image>().color=new Color(dfCorBorda.r,dfCorBorda.g,dfCorBorda.b,0.65f);

            // filho 1: corpo (frente — cobre a borda)
            var corpo=new GameObject("Corpo"); corpo.transform.SetParent(go.transform,false);
            var rco=corpo.AddComponent<RectTransform>(); rco.anchorMin=Vector2.zero; rco.anchorMax=Vector2.one; rco.offsetMin=rco.offsetMax=Vector2.zero;
            var img=corpo.AddComponent<Image>(); img.color=i==atual?corAtv:corInav;

            // filho 2: bevel topo
            var topQ=new GameObject("TQ"); topQ.transform.SetParent(go.transform,false);
            var rtq=topQ.AddComponent<RectTransform>(); rtq.anchorMin=new Vector2(0f,1f); rtq.anchorMax=new Vector2(1f,1f);
            rtq.offsetMin=new Vector2(1f,-2f); rtq.offsetMax=new Vector2(-1f,0f);
            topQ.AddComponent<Image>().color=new Color(1f,0.95f,0.95f,i==atual?0.25f:0.05f);

            // filho 3: acento esquerdo (ativo = dourado, inativo = sutil)
            var ac=new GameObject("Ac"); ac.transform.SetParent(go.transform,false);
            var rac=ac.AddComponent<RectTransform>(); rac.anchorMin=Vector2.zero; rac.anchorMax=new Vector2(0f,1f);
            rac.offsetMin=Vector2.zero; rac.offsetMax=new Vector2(3f,0f);
            ac.AddComponent<Image>().color=new Color(dfCorBorda.r,dfCorBorda.g,dfCorBorda.b,i==atual?0.90f:0.20f);

            var btn=go.AddComponent<Button>(); btn.targetGraphic=img; btn.transition=Selectable.Transition.ColorTint;
            Color hov=new Color(0.42f,0.13f,0.13f);
            btn.colors=new ColorBlock{normalColor=i==atual?corAtv:corInav,highlightedColor=hov,pressedColor=new Color(0.06f,0.03f,0.03f),selectedColor=corAtv,disabledColor=corInav,colorMultiplier=1f,fadeDuration=0.08f};
            btn.onClick.AddListener(()=>{
                QualitySettings.SetQualityLevel(idx,true);
                PlayerPrefs.SetInt("QualityLevel",idx);
                for(int j=0;j<go.transform.parent.childCount;j++){
                    var qgo=go.transform.parent.GetChild(j);
                    var c=qgo.Find("Corpo"); if(c!=null)c.GetComponent<Image>().color=corInav;
                    var th=qgo.Find("TQ"); if(th!=null)th.GetComponent<Image>().color=new Color(1f,0.95f,0.95f,0.05f);
                    var a=qgo.Find("Ac"); if(a!=null)a.GetComponent<Image>().color=new Color(dfCorBorda.r,dfCorBorda.g,dfCorBorda.b,0.20f);
                    var tx=qgo.Find("T"); if(tx!=null){var tt=tx.GetComponent<TextMeshProUGUI>(); if(tt!=null)tt.color=dfCorTxtInv;}
                }
                img.color=corAtv;
                topQ.GetComponent<Image>().color=new Color(1f,0.95f,0.95f,0.25f);
                ac.GetComponent<Image>().color=new Color(dfCorBorda.r,dfCorBorda.g,dfCorBorda.b,0.90f);
                var txAtv=go.transform.Find("T"); if(txAtv!=null){var tt=txAtv.GetComponent<TextMeshProUGUI>(); if(tt!=null)tt.color=dfCorTitulo;}
            });
            Color corTxtQ=i==atual?dfCorTitulo:dfCorTxtInv;
            CriarTexto(go,"T",Vector2.zero,Vector2.one,n[i],12f,FontStyles.Bold,corTxtQ)
                .alignment=TextAlignmentOptions.Center;
        }
    }

    // ── Sair ─────────────────────────────────────────────────────────────
    string GerarCodigoSala()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        string cod = "";
        for (int i = 0; i < 4; i++)
            cod += chars[Random.Range(0, chars.Length)];
        return cod;
    }

    void Sair()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── Helpers de UI ────────────────────────────────────────────────────
    void BarraTopo(GameObject pai, Color cor)
    {
        var b = new GameObject("BarraTopo"); b.transform.SetParent(pai.transform,false);
        var rb = b.AddComponent<RectTransform>();
        rb.anchorMin=new Vector2(0f,1f); rb.anchorMax=Vector2.one;
        rb.offsetMin=Vector2.zero; rb.offsetMax=new Vector2(0f,4f);
        b.AddComponent<Image>().color=cor;
    }

    void Rotulo(GameObject p, string label, float yMin, float yMax)
    {
        // linha separadora acima do rótulo
        DFSep(p, new Vector2(0.03f, yMax-0.001f), new Vector2(0.97f, yMax+0.001f));

        // ícone decorativo "▸" antes do texto
        var row = new GameObject($"Row_{label}"); row.transform.SetParent(p.transform, false);
        var rr = row.AddComponent<RectTransform>();
        rr.anchorMin = new Vector2(0.03f, yMin); rr.anchorMax = new Vector2(0.65f, yMax);
        rr.offsetMin = rr.offsetMax = Vector2.zero;

        var icon = CriarTexto(row, "Ic", Vector2.zero, new Vector2(0.08f, 1f),
            ">", 13f, FontStyles.Normal, new Color(dfCorBorda.r, dfCorBorda.g, dfCorBorda.b, 0.85f));
        icon.alignment = TextAlignmentOptions.MidlineLeft;

        var lbl = CriarTexto(row, "Lbl", new Vector2(0.08f, 0f), Vector2.one,
            label, 14f, FontStyles.Bold, dfCorTexto);
        lbl.alignment = TextAlignmentOptions.MidlineLeft;
    }

    Slider Slider(GameObject p, string nome, Vector2 mn, Vector2 mx, float val)
    {
        // container raiz — sem Image (estrutura de irmãos para renderização correta)
        var go=new GameObject(nome); go.transform.SetParent(p.transform,false);
        var r=go.AddComponent<RectTransform>(); r.anchorMin=mn; r.anchorMax=mx; r.offsetMin=r.offsetMax=Vector2.zero;

        // irmão 0: borda dourada externa (só quando não há sprite de trilha com moldura própria)
        if (sprSliderTrack == null)
        {
            var brd=new GameObject("Brd"); brd.transform.SetParent(go.transform,false);
            var rbrd=brd.AddComponent<RectTransform>(); rbrd.anchorMin=Vector2.zero; rbrd.anchorMax=Vector2.one;
            rbrd.offsetMin=new Vector2(-1f,-1f); rbrd.offsetMax=new Vector2(1f,1f);
            brd.AddComponent<Image>().color=new Color(dfCorBorda.r,dfCorBorda.g,dfCorBorda.b,0.50f);
        }

        // irmão 1: trilha (sprite pixel-art entalhado, com fallback procedural)
        var trk=new GameObject("Trk"); trk.transform.SetParent(go.transform,false);
        var rtrk=trk.AddComponent<RectTransform>(); rtrk.anchorMin=Vector2.zero; rtrk.anchorMax=Vector2.one; rtrk.offsetMin=rtrk.offsetMax=Vector2.zero;
        var imgTrk=trk.AddComponent<Image>();
        if (sprSliderTrack != null)
        {
            imgTrk.sprite=sprSliderTrack; imgTrk.type=Image.Type.Sliced; imgTrk.color=Color.white;
        }
        else
        {
            imgTrk.color=new Color(0.07f,0.04f,0.03f);
            // sombra interna topo (efeito de entalhe)
            var ist=new GameObject("InT"); ist.transform.SetParent(trk.transform,false);
            var rist=ist.AddComponent<RectTransform>(); rist.anchorMin=new Vector2(0f,1f); rist.anchorMax=new Vector2(1f,1f);
            rist.offsetMin=new Vector2(0f,-3f); rist.offsetMax=Vector2.zero;
            ist.AddComponent<Image>().color=new Color(0f,0f,0f,0.65f);
        }

        // irmão 2: fill area (Unity ajusta a largura do fill conforme o valor)
        // padding lateral pequeno para o preenchimento respeitar a moldura da trilha
        var fa=new GameObject("FA"); fa.transform.SetParent(go.transform,false);
        var rfa=fa.AddComponent<RectTransform>(); rfa.anchorMin=Vector2.zero; rfa.anchorMax=Vector2.one;
        rfa.offsetMin=new Vector2(3f,3f); rfa.offsetMax=new Vector2(-3f,-3f);
        // fill principal (sprite âmbar, com fallback procedural)
        var fi=new GameObject("Fi"); fi.transform.SetParent(fa.transform,false);
        var rfi=fi.AddComponent<RectTransform>(); rfi.anchorMin=Vector2.zero; rfi.anchorMax=Vector2.one; rfi.offsetMin=rfi.offsetMax=Vector2.zero;
        var imgFi=fi.AddComponent<Image>();
        if (sprSliderFill != null)
        {
            imgFi.sprite=sprSliderFill; imgFi.type=Image.Type.Sliced; imgFi.color=new Color(0.85f,0.18f,0.15f); // vermelho-brasa
        }
        else
        {
            imgFi.color=new Color(0.70f,0.12f,0.10f);
            var fhl=new GameObject("FHl"); fhl.transform.SetParent(fi.transform,false);
            var rfhl=fhl.AddComponent<RectTransform>(); rfhl.anchorMin=new Vector2(0f,1f); rfhl.anchorMax=new Vector2(1f,1f);
            rfhl.offsetMin=new Vector2(0f,-1f); rfhl.offsetMax=Vector2.zero;
            fhl.AddComponent<Image>().color=new Color(1f,0.55f,0.45f,0.45f);
        }

        // irmão 3: handle slide area
        var ha=new GameObject("HA"); ha.transform.SetParent(go.transform,false);
        var rha=ha.AddComponent<RectTransform>(); rha.anchorMin=Vector2.zero; rha.anchorMax=Vector2.one; rha.offsetMin=rha.offsetMax=Vector2.zero;

        // knob container (Unity posiciona anchorMin.x=anchorMax.x=valor)
        var h=new GameObject("H"); h.transform.SetParent(ha.transform,false);
        var rh=h.AddComponent<RectTransform>();
        rh.anchorMin=new Vector2(0f,0f); rh.anchorMax=new Vector2(0f,1f);
        rh.offsetMin=new Vector2(-8f,-5f); rh.offsetMax=new Vector2(8f,5f); // 16px largura, transborda para baixo/cima

        Image hImg;
        if (sprSliderKnob != null)
        {
            // sombra do knob (atrás)
            var kSh=new GameObject("KSh"); kSh.transform.SetParent(h.transform,false);
            var rkSh=kSh.AddComponent<RectTransform>(); rkSh.anchorMin=Vector2.zero; rkSh.anchorMax=Vector2.one;
            rkSh.offsetMin=new Vector2(1f,-2f); rkSh.offsetMax=new Vector2(3f,-2f);
            var imgKSh=kSh.AddComponent<Image>(); imgKSh.sprite=sprSliderKnob; imgKSh.type=Image.Type.Simple;
            imgKSh.color=new Color(0f,0f,0f,0.55f);

            // corpo do knob (sprite)
            var kBd=new GameObject("KBd"); kBd.transform.SetParent(h.transform,false);
            var rkBd=kBd.AddComponent<RectTransform>(); rkBd.anchorMin=Vector2.zero; rkBd.anchorMax=Vector2.one; rkBd.offsetMin=rkBd.offsetMax=Vector2.zero;
            hImg=kBd.AddComponent<Image>(); hImg.sprite=sprSliderKnob; hImg.type=Image.Type.Simple; hImg.color=new Color(1f,0.62f,0.50f); // brasa clara
        }
        else
        {
            // knob procedural: sombra + corpo dourado + highlight
            var kSh=new GameObject("KSh"); kSh.transform.SetParent(h.transform,false);
            var rkSh=kSh.AddComponent<RectTransform>(); rkSh.anchorMin=Vector2.zero; rkSh.anchorMax=Vector2.one;
            rkSh.offsetMin=new Vector2(-1f,-1f); rkSh.offsetMax=new Vector2(1f,1f);
            kSh.AddComponent<Image>().color=new Color(0f,0f,0f,0.80f);

            var kBd=new GameObject("KBd"); kBd.transform.SetParent(h.transform,false);
            var rkBd=kBd.AddComponent<RectTransform>(); rkBd.anchorMin=Vector2.zero; rkBd.anchorMax=Vector2.one; rkBd.offsetMin=rkBd.offsetMax=Vector2.zero;
            hImg=kBd.AddComponent<Image>(); hImg.color=new Color(0.80f,0.16f,0.14f);

            var kHi=new GameObject("KHi"); kHi.transform.SetParent(h.transform,false);
            var rkHi=kHi.AddComponent<RectTransform>(); rkHi.anchorMin=new Vector2(0f,1f); rkHi.anchorMax=new Vector2(1f,1f);
            rkHi.offsetMin=new Vector2(1f,-2f); rkHi.offsetMax=new Vector2(-1f,0f);
            kHi.AddComponent<Image>().color=new Color(1f,0.70f,0.60f,0.65f);
        }

        var sl=go.AddComponent<UnityEngine.UI.Slider>();
        sl.fillRect=rfi; sl.handleRect=rh; sl.targetGraphic=hImg;
        sl.direction=UnityEngine.UI.Slider.Direction.LeftToRight; sl.minValue=0f; sl.maxValue=1f; sl.value=val;
        return sl;
    }

    void Toggle(GameObject p, string nome, Vector2 mn, Vector2 mx, bool val, System.Action<bool> cb)
    {
        // container raiz — sem Image (mesma lógica do BotaoSimples: filhos renderizam sobre pai)
        var go=new GameObject(nome); go.transform.SetParent(p.transform,false);
        var r=go.AddComponent<RectTransform>(); r.anchorMin=mn; r.anchorMax=mx; r.offsetMin=r.offsetMax=Vector2.zero;

        // filho 0: borda (atrás)
        var brd=new GameObject("B"); brd.transform.SetParent(go.transform,false);
        var rbrd=brd.AddComponent<RectTransform>(); rbrd.anchorMin=Vector2.zero; rbrd.anchorMax=Vector2.one;
        rbrd.offsetMin=new Vector2(-1f,-1f); rbrd.offsetMax=new Vector2(1f,1f);
        brd.AddComponent<Image>().color=new Color(dfCorBorda.r,dfCorBorda.g,dfCorBorda.b,0.55f);

        // filho 1: sprite do toggle (frente — cobre a borda exceto 1px nas bordas)
        bool estado=val;
        var bgGo=new GameObject("BG"); bgGo.transform.SetParent(go.transform,false);
        var rbg=bgGo.AddComponent<RectTransform>(); rbg.anchorMin=Vector2.zero; rbg.anchorMax=Vector2.one; rbg.offsetMin=rbg.offsetMax=Vector2.zero;
        var bg=bgGo.AddComponent<Image>(); bg.type=Image.Type.Simple; bg.color=Color.white;
        bg.sprite = estado ? sprToggleOn : sprToggleOff;

        // label ON/OFF à esquerda
        Color corOn  = new Color(0.95f,0.80f,0.30f);
        Color corOff = new Color(0.50f,0.36f,0.34f);
        var lbl=CriarTexto(go,"Lbl",new Vector2(-0.75f,0.1f),new Vector2(0f,0.9f),
            estado?"ON":"OFF", 11f, FontStyles.Bold, estado?corOn:corOff);
        lbl.alignment=TextAlignmentOptions.MidlineRight;

        var btn=go.AddComponent<Button>();
        btn.targetGraphic=bg; btn.transition=Selectable.Transition.None;
        btn.onClick.AddListener(()=>{
            estado=!estado;
            bg.sprite=estado?sprToggleOn:sprToggleOff;
            lbl.text=estado?"ON":"OFF";
            lbl.color=estado?corOn:corOff;
            cb(estado);
        });
    }

    void BotaoSimples(GameObject pai, string label, Vector2 mn, Vector2 mx, Color cor, System.Action acao)
    {
        // Container raiz — sem Image (filhos renderizam sobre o pai, então img no pai = invisível sob filhos)
        var go=new GameObject($"B_{label}"); go.transform.SetParent(pai.transform,false);
        var r=go.AddComponent<RectTransform>(); r.anchorMin=mn; r.anchorMax=mx; r.offsetMin=r.offsetMax=Vector2.zero;

        // filho 0: borda dourada (1px fora) — renderiza primeiro (por baixo dos outros filhos)
        var borda=new GameObject("Borda"); borda.transform.SetParent(go.transform,false);
        var rb=borda.AddComponent<RectTransform>(); rb.anchorMin=Vector2.zero; rb.anchorMax=Vector2.one;
        rb.offsetMin=new Vector2(-1f,-1f); rb.offsetMax=new Vector2(1f,1f);
        borda.AddComponent<Image>().color=new Color(dfCorBorda.r,dfCorBorda.g,dfCorBorda.b,0.88f);

        // filho 1: corpo do botão (cobre a borda exceto 1px nas bordas) — É O targetGraphic
        bool isDanger = cor.r > 0.30f && cor.g < 0.15f && cor.b < 0.15f;
        var corpo=new GameObject("Corpo"); corpo.transform.SetParent(go.transform,false);
        var rc=corpo.AddComponent<RectTransform>(); rc.anchorMin=Vector2.zero; rc.anchorMax=Vector2.one; rc.offsetMin=rc.offsetMax=Vector2.zero;
        var img=corpo.AddComponent<Image>(); img.color=cor;

        // filho 2: brilho no topo
        var topo=new GameObject("HiTop"); topo.transform.SetParent(go.transform,false);
        var rt2=topo.AddComponent<RectTransform>(); rt2.anchorMin=new Vector2(0f,1f); rt2.anchorMax=new Vector2(1f,1f);
        rt2.offsetMin=new Vector2(0f,-2f); rt2.offsetMax=Vector2.zero;
        topo.AddComponent<Image>().color=new Color(1f,0.92f,0.65f,isDanger?0.08f:0.14f);

        // filho 3: sombra na base
        var base2=new GameObject("ShBase"); base2.transform.SetParent(go.transform,false);
        var rb2=base2.AddComponent<RectTransform>(); rb2.anchorMin=Vector2.zero; rb2.anchorMax=new Vector2(1f,0f);
        rb2.offsetMin=Vector2.zero; rb2.offsetMax=new Vector2(0f,2f);
        base2.AddComponent<Image>().color=new Color(0f,0f,0f,0.50f);

        // filho 4: acento lateral (tipo do botão)
        var ac=new GameObject("Ac"); ac.transform.SetParent(go.transform,false);
        var rac=ac.AddComponent<RectTransform>(); rac.anchorMin=Vector2.zero; rac.anchorMax=new Vector2(0f,1f);
        rac.offsetMin=Vector2.zero; rac.offsetMax=new Vector2(4f,0f);
        ac.AddComponent<Image>().color=isDanger ? new Color(0.95f,0.18f,0.10f,1f)
                                                : new Color(dfCorBorda.r,dfCorBorda.g,dfCorBorda.b,0.95f);

        go.AddComponent<CardHover>();

        // Button no container raiz, targetGraphic = img (corpo)
        var btn=go.AddComponent<Button>(); btn.targetGraphic=img;
        btn.transition=Selectable.Transition.ColorTint;
        Color corHover=new Color(Mathf.Min(cor.r+0.18f,1f),Mathf.Min(cor.g+0.12f,1f),Mathf.Min(cor.b+0.10f,1f),cor.a);
        Color corPress=new Color(Mathf.Max(cor.r-0.06f,0f),Mathf.Max(cor.g-0.04f,0f),Mathf.Max(cor.b-0.03f,0f),cor.a);
        btn.colors=new ColorBlock{normalColor=cor,highlightedColor=corHover,pressedColor=corPress,selectedColor=cor,disabledColor=new Color(cor.r*0.5f,cor.g*0.5f,cor.b*0.5f,0.5f),colorMultiplier=1f,fadeDuration=0.1f};
        btn.onClick.AddListener(()=>acao());

        Color corTxt=isDanger ? new Color(1f,0.72f,0.68f) : dfCorTexto;
        CriarTexto(go,"T",new Vector2(0.04f,0f),Vector2.one,label,14f,FontStyles.Bold,corTxt)
            .alignment=TextAlignmentOptions.Center;
    }

    void AvisoPopup(GameObject pai, string msg)
    {
        var old=pai.transform.Find("Popup"); if(old!=null) Destroy(old.gameObject);
        var go=new GameObject("Popup"); go.transform.SetParent(pai.transform,false);
        var r=go.AddComponent<RectTransform>(); r.anchorMin=new Vector2(0.1f,0.38f); r.anchorMax=new Vector2(0.9f,0.54f); r.offsetMin=r.offsetMax=Vector2.zero;
        go.AddComponent<Image>().color=new Color(0.15f,0.10f,0.05f);
        CriarTexto(go,"T",Vector2.zero,Vector2.one,msg,14f,FontStyles.Bold,new Color(1f,0.8f,0.3f))
            .alignment=TextAlignmentOptions.Center;
        StartCoroutine(DestruirApos(go,2f));
    }

    IEnumerator DestruirApos(GameObject go, float s)
    { yield return new WaitForSeconds(s); if(go!=null) Destroy(go); }

    Image AdicionarImagem(GameObject go, Color cor)
    { return go.AddComponent<Image>() is var img ? (img.color=cor, img).Item2 : null; }

    GameObject CriarImg(string nome, Color cor)
    {
        var go=new GameObject(nome); go.transform.SetParent(canvasRef.transform,false);
        go.AddComponent<RectTransform>(); go.AddComponent<Image>().color=cor;
        return go;
    }

    TextMeshProUGUI CriarTexto(GameObject parent, string nome,
        Vector2 ancMin, Vector2 ancMax,
        string texto, float size, FontStyles style, Color cor)
    {
        var go=new GameObject(nome); go.transform.SetParent(parent.transform,false);
        var r=go.AddComponent<RectTransform>(); r.anchorMin=ancMin; r.anchorMax=ancMax; r.offsetMin=r.offsetMax=Vector2.zero;
        var t=go.AddComponent<TextMeshProUGUI>(); t.text=texto; t.fontSize=size; t.fontStyle=style; t.color=cor; t.alignment=TextAlignmentOptions.Center;
        return t;
    }

    void Esticar(GameObject go)
    { var r=go.GetComponent<RectTransform>(); r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one; r.offsetMin=r.offsetMax=Vector2.zero; }

    void Esticar(Image img) => Esticar(img.gameObject);
}

// ── Hover dos botões ──────────────────────────────────────────────────────
public class BotaoMenuHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Image             img;
    public Image             barraImg;
    public TextMeshProUGUI   txt;
    public Color             corNormal, corHover, corBorda, corBarra;
    public GameObject        bordaGO;

    bool  sobre = false;
    float escala = 1f, escalaAlvo = 1f;
    float barraAlpha = 0f;

    void Update()
    {
        escala = Mathf.Lerp(escala, escalaAlvo, Time.deltaTime * 16f);
        transform.localScale = Vector3.one * escala;

        if (img != null)
            img.color = Color.Lerp(img.color, sobre ? corHover : corNormal, Time.deltaTime * 14f);

        // barra lateral aparece/desaparece
        if (barraImg != null)
        {
            barraAlpha = Mathf.Lerp(barraAlpha, sobre ? 1f : 0f, Time.deltaTime * 16f);
            var c = corBarra;
            barraImg.color = new Color(c.r, c.g, c.b, barraAlpha);
        }

        // texto fica levemente mais brilhante no hover
        if (txt != null)
            txt.color = Color.Lerp(txt.color, sobre ? new Color(1f, 0.95f, 0.80f) : Color.white, Time.deltaTime * 14f);
    }

    public void OnPointerEnter(PointerEventData e) { sobre = true;  escalaAlvo = 1.07f; }
    public void OnPointerExit (PointerEventData e) { sobre = false; escalaAlvo = 1.00f; }
    public void OnPointerClick(PointerEventData e) => StartCoroutine(Flash());

    System.Collections.IEnumerator Flash()
    {
        escalaAlvo = 0.93f;
        yield return new WaitForSeconds(0.06f);
        escalaAlvo = 1.10f;
        yield return new WaitForSeconds(0.08f);
        escalaAlvo = sobre ? 1.07f : 1.00f;
    }
}
