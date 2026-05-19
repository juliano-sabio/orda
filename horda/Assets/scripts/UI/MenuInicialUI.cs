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

    // ── Refs ────────────────────────────────────────────────────────────
    GameObject canvasRef;
    GameObject painelOpcoes;
    GameObject painelMulti;
    GameObject[] painelAbas = new GameObject[3];
    Button[]     botoesAbas = new Button[3];

    // ────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        canvasRef = CriarCanvas();
        CriarFundo();
        CriarParticulas();
        var logo = CriarLogo();
        CriarBotoes();
        CriarRodape();

        StartCoroutine(AnimarParticulas());
        StartCoroutine(AnimarLogo(logo));
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

    // ── Fundo simples ───────────────────────────────────────────────────
    void CriarFundo()
    {
        // base
        Esticar(CriarImg("Fundo", corFundo));

        // brilho suave atrás do título (apenas 1 camada)
        var glow = CriarImg("GlowTitulo", new Color(corAcento.r, corAcento.g, corAcento.b, 0.08f));
        var rg = glow.GetComponent<RectTransform>();
        rg.anchorMin = new Vector2(0.1f, 0.60f); rg.anchorMax = new Vector2(0.9f, 1.0f);
        rg.offsetMin = rg.offsetMax = Vector2.zero;

        // painel escuro atrás dos botões
        var painel = CriarImg("PainelBotoes", new Color(0.06f, 0.04f, 0.13f, 0.85f));
        var rp = painel.GetComponent<RectTransform>();
        rp.anchorMin = new Vector2(0.25f, 0.20f); rp.anchorMax = new Vector2(0.75f, 0.78f);
        rp.offsetMin = rp.offsetMax = Vector2.zero;

        // linha roxa no topo do painel
        var linha = CriarImg("LinhaTopo", corAcento);
        var rl = linha.GetComponent<RectTransform>();
        rl.anchorMin = new Vector2(0.25f, 0.778f); rl.anchorMax = new Vector2(0.75f, 0.778f);
        rl.offsetMin = Vector2.zero; rl.offsetMax = new Vector2(0f, 2f);
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
        CriarBotaoMenu("▶  JOGAR",     0.63f, 0.74f, corAcento,                       28f, () => SceneManager.LoadScene(cenaSelecaoPersonagem));
        CriarBotaoMenu("MULTIJOGADOR", 0.50f, 0.61f, new Color(0.10f, 0.30f, 0.50f),  22f, AbrirMultijogador);
        CriarBotaoMenu("OPÇÕES",       0.37f, 0.48f, new Color(0.20f, 0.20f, 0.38f),  22f, AbrirOpcoes);
        CriarBotaoMenu("SAIR",         0.24f, 0.35f, new Color(0.40f, 0.06f, 0.06f),  22f, Sair);
    }

    void CriarBotaoMenu(string label, float yMin, float yMax,
        Color corBorda, float fontSize, System.Action acao)
    {
        var go = new GameObject($"Btn_{label.Trim()}");
        go.transform.SetParent(canvasRef.transform, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(0.30f, yMin);
        r.anchorMax = new Vector2(0.70f, yMax);
        r.offsetMin = r.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.color = corBotao;

        var borda = new GameObject("Borda");
        borda.transform.SetParent(go.transform, false);
        var rb = borda.AddComponent<RectTransform>();
        rb.anchorMin = Vector2.zero;
        rb.anchorMax = new Vector2(0f, 1f);
        rb.offsetMin = Vector2.zero;
        rb.offsetMax = new Vector2(5f, 0f);
        borda.AddComponent<Image>().color = corBorda;

        var txt = CriarTexto(go, "Txt", Vector2.zero, Vector2.one,
            label, fontSize, FontStyles.Bold, Color.white);
        txt.alignment = TextAlignmentOptions.Center;

        var btn = go.AddComponent<Button>();
        btn.transition    = Selectable.Transition.None;
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => acao());

        var hover = go.AddComponent<BotaoMenuHover>();
        hover.img       = img;
        hover.corNormal = corBotao;
        hover.corHover  = corBotaoHover;
        hover.corBorda  = corBorda;
        hover.bordaGO   = borda;
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
            $"Nível  {nivel}        Moedas  {moedas}",
            13f, FontStyles.Normal, new Color(0.55f, 0.45f, 0.70f))
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

        var painel = new GameObject("Painel");
        painel.transform.SetParent(painelMulti.transform, false);
        var rp = painel.AddComponent<RectTransform>();
        rp.anchorMin = new Vector2(0.25f, 0.20f); rp.anchorMax = new Vector2(0.75f, 0.82f);
        rp.offsetMin = rp.offsetMax = Vector2.zero;
        painel.AddComponent<Image>().color = new Color(0.05f, 0.08f, 0.16f);

        BarraTopo(painel, new Color(0.10f, 0.30f, 0.60f));
        CriarTexto(painel, "Titulo", new Vector2(0f,0.84f), new Vector2(1f,1f),
            "MULTIJOGADOR", 26f, FontStyles.Bold, Color.white);
        CriarTexto(painel, "Icone", new Vector2(0.3f,0.60f), new Vector2(0.7f,0.82f),
            "🌐", 42f, FontStyles.Normal, new Color(0.4f,0.7f,1f));

        CriarTexto(painel, "LblCodigo", new Vector2(0.05f,0.50f), new Vector2(0.95f,0.60f),
            "CÓDIGO DA SALA", 13f, FontStyles.Bold, new Color(0.6f,0.8f,1f));

        var campo = new GameObject("Campo");
        campo.transform.SetParent(painel.transform, false);
        var rca = campo.AddComponent<RectTransform>();
        rca.anchorMin = new Vector2(0.05f,0.39f); rca.anchorMax = new Vector2(0.95f,0.50f);
        rca.offsetMin = rca.offsetMax = Vector2.zero;
        campo.AddComponent<Image>().color = new Color(0.10f,0.14f,0.24f);
        var ph = CriarTexto(campo,"PH",new Vector2(0.03f,0f),new Vector2(0.97f,1f),
            "Ex: SPIRIT-1234",14f,FontStyles.Italic,new Color(0.4f,0.4f,0.5f));
        ph.alignment = TextAlignmentOptions.Left;

        BotaoSimples(painel,"CRIAR SALA",new Vector2(0.05f,0.23f),new Vector2(0.47f,0.37f),
            new Color(0.10f,0.35f,0.65f),()=>AvisoPopup(painel,"Em desenvolvimento..."));
        BotaoSimples(painel,"ENTRAR NA SALA",new Vector2(0.53f,0.23f),new Vector2(0.95f,0.37f),
            new Color(0.10f,0.45f,0.25f),()=>AvisoPopup(painel,"Em desenvolvimento..."));

        CriarTexto(painel,"Aviso",new Vector2(0.05f,0.10f),new Vector2(0.95f,0.22f),
            "⚠  Em breve disponível!",11f,FontStyles.Italic,new Color(0.8f,0.7f,0.3f));
        BotaoSimples(painel,"← VOLTAR",new Vector2(0.10f,0.02f),new Vector2(0.90f,0.10f),
            new Color(0.28f,0.08f,0.08f),()=>painelMulti.SetActive(false));
    }

    // ── Opções ───────────────────────────────────────────────────────────
    void AbrirOpcoes()
    {
        if (painelOpcoes == null) CriarPainelOpcoes();
        painelOpcoes.SetActive(true);
    }

    void CriarPainelOpcoes()
    {
        painelOpcoes = new GameObject("PainelOpcoes");
        painelOpcoes.transform.SetParent(canvasRef.transform, false);

        Esticar(AdicionarImagem(painelOpcoes, new Color(0f,0f,0f,0.80f)));

        var painel = new GameObject("Painel");
        painel.transform.SetParent(painelOpcoes.transform, false);
        var rp = painel.AddComponent<RectTransform>();
        rp.anchorMin = new Vector2(0.22f,0.10f); rp.anchorMax = new Vector2(0.78f,0.90f);
        rp.offsetMin = rp.offsetMax = Vector2.zero;
        painel.AddComponent<Image>().color = new Color(0.07f,0.05f,0.15f);

        BarraTopo(painel, corAcento);
        CriarTexto(painel,"Titulo",new Vector2(0f,0.90f),new Vector2(1f,1f),
            "CONFIGURAÇÕES",26f,FontStyles.Bold,Color.white);

        string[] nomes = {"ÁUDIO","VÍDEO","JOGO"};
        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            var abaGO = new GameObject($"Aba{i}");
            abaGO.transform.SetParent(painel.transform, false);
            var ra = abaGO.AddComponent<RectTransform>();
            ra.anchorMin = new Vector2(idx/3f+0.005f,0.82f);
            ra.anchorMax = new Vector2((idx+1)/3f-0.005f,0.91f);
            ra.offsetMin = ra.offsetMax = Vector2.zero;
            var imgA = abaGO.AddComponent<Image>();
            imgA.color = i==0 ? corAcento : new Color(0.14f,0.10f,0.28f);
            var btnA = abaGO.AddComponent<Button>();
            btnA.targetGraphic=imgA; btnA.transition=Selectable.Transition.None;
            btnA.onClick.AddListener(()=>MudarAba(idx));
            CriarTexto(abaGO,"T",Vector2.zero,Vector2.one,nomes[i],14f,FontStyles.Bold,Color.white);
            botoesAbas[i]=btnA;

            var cont = new GameObject($"Cont{i}");
            cont.transform.SetParent(painel.transform,false);
            var rc = cont.AddComponent<RectTransform>();
            rc.anchorMin=new Vector2(0f,0.12f); rc.anchorMax=new Vector2(1f,0.82f);
            rc.offsetMin=rc.offsetMax=Vector2.zero;
            cont.AddComponent<RectTransform>();
            painelAbas[i]=cont;
            cont.SetActive(i==0);
        }

        PopularAudio(painelAbas[0]);
        PopularVideo(painelAbas[1]);
        PopularJogo(painelAbas[2]);

        BotaoSimples(painel,"← VOLTAR",new Vector2(0.1f,0.02f),new Vector2(0.9f,0.11f),
            new Color(0.28f,0.08f,0.08f),()=>{PlayerPrefs.Save();painelOpcoes.SetActive(false);});
    }

    void MudarAba(int idx)
    {
        for (int i=0;i<3;i++)
        {
            painelAbas[i].SetActive(i==idx);
            botoesAbas[i].GetComponent<Image>().color=
                i==idx ? corAcento : new Color(0.14f,0.10f,0.28f);
        }
    }

    void PopularAudio(GameObject p)
    {
        Rotulo(p,"Volume Geral",0.80f,0.92f);
        var sv=Slider(p,"SV",new Vector2(0.05f,0.68f),new Vector2(0.95f,0.80f),PlayerPrefs.GetFloat("MasterVolume",1f));
        sv.onValueChanged.AddListener(v=>{AudioListener.volume=v;PlayerPrefs.SetFloat("MasterVolume",v);});

        Rotulo(p,"Música",0.56f,0.68f);
        var sm=Slider(p,"SM",new Vector2(0.05f,0.44f),new Vector2(0.95f,0.56f),PlayerPrefs.GetFloat("MusicVolume",0.8f));
        sm.onValueChanged.AddListener(v=>PlayerPrefs.SetFloat("MusicVolume",v));

        Rotulo(p,"Efeitos Sonoros",0.32f,0.44f);
        var ss=Slider(p,"SS",new Vector2(0.05f,0.20f),new Vector2(0.95f,0.32f),PlayerPrefs.GetFloat("SFXVolume",1f));
        ss.onValueChanged.AddListener(v=>PlayerPrefs.SetFloat("SFXVolume",v));
    }

    void PopularVideo(GameObject p)
    {
        Rotulo(p,"Tela Cheia",0.80f,0.92f);
        Toggle(p,"TF",new Vector2(0.70f,0.80f),new Vector2(0.90f,0.92f),
            Screen.fullScreen,v=>{Screen.fullScreen=v;PlayerPrefs.SetInt("Fullscreen",v?1:0);});

        Rotulo(p,"VSync",0.64f,0.76f);
        Toggle(p,"TV",new Vector2(0.70f,0.64f),new Vector2(0.90f,0.76f),
            QualitySettings.vSyncCount>0,v=>QualitySettings.vSyncCount=v?1:0);

        Rotulo(p,"Qualidade Gráfica",0.46f,0.60f);
        BotoesQualidade(p,new Vector2(0.05f,0.34f),new Vector2(0.95f,0.46f));

        Rotulo(p,"Limite de FPS",0.20f,0.32f);
        var sf=Slider(p,"SF",new Vector2(0.05f,0.08f),new Vector2(0.80f,0.20f),
            Mathf.InverseLerp(30f,240f,PlayerPrefs.GetInt("TargetFPS",60)));
        var tf=CriarTexto(p,"TF2",new Vector2(0.82f,0.08f),new Vector2(0.95f,0.20f),
            $"{PlayerPrefs.GetInt("TargetFPS",60)}",13f,FontStyles.Normal,new Color(0.65f,0.60f,0.75f));
        sf.onValueChanged.AddListener(v=>{
            int fps=Mathf.RoundToInt(Mathf.Lerp(30f,240f,v));
            Application.targetFrameRate=fps;
            PlayerPrefs.SetInt("TargetFPS",fps);
            tf.text=$"{fps}";
        });
    }

    void PopularJogo(GameObject p)
    {
        Rotulo(p,"Mostrar Tutorial",0.80f,0.92f);
        Toggle(p,"TT",new Vector2(0.70f,0.80f),new Vector2(0.90f,0.92f),
            PlayerPrefs.GetInt("TutorialVisto",0)==0,
            v=>PlayerPrefs.SetInt("TutorialVisto",v?0:1));

        Rotulo(p,"Mostrar FPS na Tela",0.64f,0.76f);
        Toggle(p,"TF",new Vector2(0.70f,0.64f),new Vector2(0.90f,0.76f),
            PlayerPrefs.GetInt("ShowFPS",0)==1,
            v=>PlayerPrefs.SetInt("ShowFPS",v?1:0));

        Rotulo(p,"Shake de Câmera",0.48f,0.60f);
        Toggle(p,"TS",new Vector2(0.70f,0.48f),new Vector2(0.90f,0.60f),
            PlayerPrefs.GetInt("CameraShake",1)==1,
            v=>PlayerPrefs.SetInt("CameraShake",v?1:0));

        BotaoSimples(p,"APAGAR PROGRESSO",new Vector2(0.10f,0.04f),new Vector2(0.90f,0.18f),
            new Color(0.5f,0.05f,0.05f),()=>{PlayerPrefs.DeleteAll();PlayerPrefs.Save();});
    }

    void BotoesQualidade(GameObject p, Vector2 mn, Vector2 mx)
    {
        string[] n={"Baixa","Média","Alta","Ultra"};
        int atual=QualitySettings.GetQualityLevel();
        for(int i=0;i<4;i++){
            int idx=i;
            float x0=mn.x+i*(mx.x-mn.x)/4f+0.005f;
            float x1=mn.x+(i+1)*(mx.x-mn.x)/4f-0.005f;
            var go=new GameObject($"Q{i}"); go.transform.SetParent(p.transform,false);
            var r=go.AddComponent<RectTransform>();
            r.anchorMin=new Vector2(x0,mn.y); r.anchorMax=new Vector2(x1,mx.y);
            r.offsetMin=r.offsetMax=Vector2.zero;
            var img=go.AddComponent<Image>(); img.color=i==atual?corAcento:new Color(0.14f,0.10f,0.28f);
            var btn=go.AddComponent<Button>(); btn.targetGraphic=img; btn.transition=Selectable.Transition.None;
            btn.onClick.AddListener(()=>{
                QualitySettings.SetQualityLevel(idx,true);
                for(int j=0;j<go.transform.parent.childCount;j++){
                    var c=go.transform.parent.GetChild(j).GetComponent<Image>();
                    if(c!=null)c.color=new Color(0.14f,0.10f,0.28f);
                }
                img.color=corAcento;
            });
            CriarTexto(go,"T",Vector2.zero,Vector2.one,n[i],12f,FontStyles.Bold,Color.white);
        }
    }

    // ── Sair ─────────────────────────────────────────────────────────────
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
        CriarTexto(p,$"L_{label}",new Vector2(0.05f,yMin),new Vector2(0.65f,yMax),
            label,15f,FontStyles.Bold,new Color(0.80f,0.70f,1.00f))
            .alignment=TextAlignmentOptions.Left;
    }

    Slider Slider(GameObject p, string nome, Vector2 mn, Vector2 mx, float val)
    {
        var go=new GameObject(nome); go.transform.SetParent(p.transform,false);
        var r=go.AddComponent<RectTransform>(); r.anchorMin=mn; r.anchorMax=mx; r.offsetMin=r.offsetMax=Vector2.zero;
        var bg=new GameObject("BG"); bg.transform.SetParent(go.transform,false);
        var rbg=bg.AddComponent<RectTransform>(); rbg.anchorMin=Vector2.zero; rbg.anchorMax=Vector2.one; rbg.offsetMin=rbg.offsetMax=Vector2.zero;
        bg.AddComponent<Image>().color=new Color(0.15f,0.10f,0.25f);
        var fa=new GameObject("FA"); fa.transform.SetParent(go.transform,false);
        var rfa=fa.AddComponent<RectTransform>(); rfa.anchorMin=new Vector2(0f,0.25f); rfa.anchorMax=new Vector2(1f,0.75f); rfa.offsetMin=new Vector2(5f,0f); rfa.offsetMax=new Vector2(-5f,0f);
        var fi=new GameObject("Fi"); fi.transform.SetParent(fa.transform,false);
        var rfi=fi.AddComponent<RectTransform>(); rfi.anchorMin=Vector2.zero; rfi.anchorMax=new Vector2(1f,1f); rfi.offsetMin=rfi.offsetMax=Vector2.zero;
        fi.AddComponent<Image>().color=corAcento;
        var ha=new GameObject("HA"); ha.transform.SetParent(go.transform,false);
        var rha=ha.AddComponent<RectTransform>(); rha.anchorMin=Vector2.zero; rha.anchorMax=Vector2.one; rha.offsetMin=rha.offsetMax=Vector2.zero;
        var h=new GameObject("H"); h.transform.SetParent(ha.transform,false);
        var rh=h.AddComponent<RectTransform>(); rh.sizeDelta=new Vector2(18f,0f);
        h.AddComponent<Image>().color=corClaro;
        var sl=go.AddComponent<UnityEngine.UI.Slider>();
        sl.fillRect=rfi; sl.handleRect=rh; sl.targetGraphic=h.GetComponent<Image>();
        sl.direction=UnityEngine.UI.Slider.Direction.LeftToRight; sl.minValue=0f; sl.maxValue=1f; sl.value=val;
        return sl;
    }

    void Toggle(GameObject p, string nome, Vector2 mn, Vector2 mx, bool val, System.Action<bool> cb)
    {
        var go=new GameObject(nome); go.transform.SetParent(p.transform,false);
        var r=go.AddComponent<RectTransform>(); r.anchorMin=mn; r.anchorMax=mx; r.offsetMin=r.offsetMax=Vector2.zero;
        var bg=go.AddComponent<Image>(); bg.color=new Color(0.15f,0.10f,0.25f);
        var ck=new GameObject("Ck"); ck.transform.SetParent(go.transform,false);
        var rc=ck.AddComponent<RectTransform>(); rc.anchorMin=new Vector2(0.1f,0.1f); rc.anchorMax=new Vector2(0.9f,0.9f); rc.offsetMin=rc.offsetMax=Vector2.zero;
        var ic=ck.AddComponent<Image>(); ic.color=corAcento;
        var tg=go.AddComponent<UnityEngine.UI.Toggle>();
        tg.targetGraphic=bg; tg.graphic=ic; tg.isOn=val;
        tg.onValueChanged.AddListener(v=>cb(v));
    }

    void BotaoSimples(GameObject pai, string label, Vector2 mn, Vector2 mx, Color cor, System.Action acao)
    {
        var go=new GameObject($"B_{label}"); go.transform.SetParent(pai.transform,false);
        var r=go.AddComponent<RectTransform>(); r.anchorMin=mn; r.anchorMax=mx; r.offsetMin=r.offsetMax=Vector2.zero;
        var img=go.AddComponent<Image>(); img.color=cor;
        var btn=go.AddComponent<Button>(); btn.targetGraphic=img; btn.transition=Selectable.Transition.None;
        btn.onClick.AddListener(()=>acao());
        CriarTexto(go,"T",Vector2.zero,Vector2.one,label,14f,FontStyles.Bold,Color.white)
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
    public Image  img;
    public Color  corNormal, corHover, corBorda;
    public GameObject bordaGO;

    bool  sobre = false;
    float escala = 1f, escalaAlvo = 1f;

    void Update()
    {
        escala = Mathf.Lerp(escala, escalaAlvo, Time.deltaTime * 12f);
        transform.localScale = Vector3.one * escala;
        if (img != null)
            img.color = Color.Lerp(img.color, sobre ? corHover : corNormal, Time.deltaTime * 10f);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        sobre = true; escalaAlvo = 1.04f;
        if (bordaGO != null)
            bordaGO.GetComponent<Image>().color = new Color(0.75f, 0.35f, 1.00f);
    }

    public void OnPointerExit(PointerEventData e)
    {
        sobre = false; escalaAlvo = 1f;
        if (bordaGO != null)
            bordaGO.GetComponent<Image>().color = corBorda;
    }

    public void OnPointerClick(PointerEventData e) => StartCoroutine(Flash());

    System.Collections.IEnumerator Flash()
    { escalaAlvo = 0.96f; yield return new WaitForSeconds(0.08f); escalaAlvo = 1.04f; }
}
