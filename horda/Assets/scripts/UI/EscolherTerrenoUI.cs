using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.SceneManagement;

public class EscolherTerrenoUI : MonoBehaviour
{
    [System.Serializable]
    public class ConfigFase
    {
        public string nome        = "Fase";
        public string nomeKey     = "";
        public string nomeCena    = "primeira_fase";
        public string descricao   = "Descrição da fase";
        public string descricaoKey = "";
        public int    dificuldade = 1;
        public bool   desbloqueada = true;
        public Color  cor         = new Color(0.2f, 0.5f, 0.8f);
    }

    [Header("Fases disponíveis")]
    public ConfigFase[] fases = new ConfigFase[]
    {
        new ConfigFase { nome = "Reino Slime",        nomeKey = "terrain.p1.name", nomeCena = "primeira_fase",      descricao = "O início da jornada.",          descricaoKey = "terrain.p1.desc", dificuldade = 1, desbloqueada = true, cor = new Color(0.15f, 0.6f, 0.15f) },
        new ConfigFase { nome = "Abismo",             nomeKey = "terrain.p2.name", nomeCena = "segunda_fase",       descricao = "Os inimigos ficam mais fortes.", descricaoKey = "terrain.p2.desc", dificuldade = 2, desbloqueada = true, cor = new Color(0.7f,  0.6f, 0.05f) },
        new ConfigFase { nome = "Caverna Aranha",     nomeKey = "terrain.p3.name", nomeCena = "terceira_fase",      descricao = "Apenas os mais corajosos.",      descricaoKey = "terrain.p3.desc", dificuldade = 3, desbloqueada = false, cor = new Color(0.7f,  0.2f, 0.05f) },
        new ConfigFase { nome = "Sobrevivência",      nomeKey = "terrain.surv.name", nomeCena = "Modo_sobrevivencia", descricao = "Sobreviva o máximo que puder!", descricaoKey = "terrain.surv.desc", dificuldade = 5, desbloqueada = false, cor = new Color(0.5f,  0.05f,0.7f) },
    };

    [Header("Cena de voltar")]
    public string cenaVoltar = "CharacterSelection";

    [Header("Assets")]
    public Sprite spriteCard;
    public Sprite spriteFundoImg;
    public Sprite spriteBotao;

    // ──────────────────────────────────────────────────────────────
    void Start()
    {
        GarantirEventSystem();
#if UNITY_EDITOR
        if (spriteCard == null)
            foreach (var a in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(
                "Assets/assets/UI/skill_card/cartaextra.ase"))
                if (a is Sprite s) { spriteCard = s; break; }
        if (spriteFundoImg == null)
            foreach (var a in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(
                "Assets/assets/UI/bg_dungeon_tocha.png"))
                if (a is Sprite s) { spriteFundoImg = s; break; }
        if (spriteBotao == null)
            spriteBotao = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/assets/UI/charselection/btn_stone.png");
#endif

        GameObject canvasGO = CriarCanvas();
        CriarFundo(canvasGO);
        CriarTitulo(canvasGO);
        CriarCards(canvasGO);
        CriarBotaoVoltar(canvasGO);
        Loc.OnLanguageChanged += OnIdiomaAlterado;
    }

    void OnDestroy()
    {
        Loc.OnLanguageChanged -= OnIdiomaAlterado;
    }

    void OnIdiomaAlterado(Language _)
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ──────────────────────────────────────────────────────────────
    // EventSystem

    void GarantirEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    // ──────────────────────────────────────────────────────────────
    // Canvas

    GameObject CriarCanvas()
    {
        GameObject go = new GameObject("Canvas_Terreno");
        Canvas c = go.AddComponent<Canvas>();
        c.renderMode  = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 10;

        CanvasScaler cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    // ──────────────────────────────────────────────────────────────
    // Elementos base

    void CriarFundo(GameObject canvas)
    {
        GameObject go = new GameObject("Fundo");
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsFirstSibling();
        Stretch(go);
        var img = go.AddComponent<Image>();
        if (spriteFundoImg != null)
        {
            img.sprite         = spriteFundoImg;
            img.type           = Image.Type.Simple;
            img.color          = Color.white;
            img.preserveAspect = false;
            img.raycastTarget  = false;
        }
        else
        {
            img.color = new Color(0.06f, 0.06f, 0.12f);
        }

        // Partículas de brasa
        GameObject brasas = new GameObject("Brasas");
        brasas.transform.SetParent(go.transform, false);
        Stretch(brasas);
        brasas.AddComponent<BrasasAnimador>();
    }

    void CriarTitulo(GameObject canvas)
    {
        GameObject go = new GameObject("Titulo");
        go.transform.SetParent(canvas.transform, false);
        RectTransform r = go.AddComponent<RectTransform>();
        r.anchorMin        = new Vector2(0f, 1f);
        r.anchorMax        = new Vector2(1f, 1f);
        r.pivot            = new Vector2(0.5f, 1f);
        r.anchoredPosition = new Vector2(0f, -10f);
        r.sizeDelta        = new Vector2(0f, 70f);

        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text      = Loc.T("terrain.title");
        t.fontSize  = 40f;
        t.fontStyle = FontStyles.Bold;
        t.color     = Color.white;
        t.alignment = TextAlignmentOptions.Center;
    }

    // ──────────────────────────────────────────────────────────────
    // Cards — 4 cartas lado a lado dividindo a tela em 4 colunas

    void CriarCards(GameObject canvas)
    {
        int total = fases.Length;
        float margem    = 0.015f;   // margem lateral total de cada card
        float largura   = (1f - margem * (total + 1)) / total;

        for (int i = 0; i < total; i++)
        {
            float xMin = margem + i * (largura + margem);
            float xMax = xMin + largura;
            CriarCard(canvas, fases[i], new Vector2(xMin, 0.1f), new Vector2(xMax, 0.88f));
        }
    }

    void CriarCard(GameObject canvas, ConfigFase fase, Vector2 ancMin, Vector2 ancMax)
    {
        GameObject card = new GameObject($"Card_{fase.nome}");
        card.transform.SetParent(canvas.transform, false);

        RectTransform r = card.AddComponent<RectTransform>();
        r.anchorMin  = ancMin;
        r.anchorMax  = ancMax;
        r.offsetMin  = new Vector2(0f, 0f);
        r.offsetMax  = new Vector2(0f, 0f);

        // Fundo do card
        Image bg = card.AddComponent<Image>();
        Sprite spriteEscolhido = spriteCard;
#if UNITY_EDITOR
        if (fase.nomeCena == "segunda_fase")
        {
            foreach (var a in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(
                "Assets/assets/UI/skill_card/cartastatusl.ase"))
                if (a is Sprite s) { spriteEscolhido = s; break; }
        }
        else if (fase.nomeCena == "terceira_fase")
        {
            foreach (var a in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(
                "Assets/assets/UI/skill_card/cartaevolução.ase"))
                if (a is Sprite s) { spriteEscolhido = s; break; }
        }
        else if (fase.nomeCena == "Modo_sobrevivencia")
        {
            foreach (var a in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(
                "Assets/assets/UI/skill_card/cartaskill.ase"))
                if (a is Sprite s) { spriteEscolhido = s; break; }
        }
#endif
        if (spriteEscolhido != null)
        {
            bg.sprite = spriteEscolhido;
            bg.type   = Image.Type.Simple;
            bg.preserveAspect = false;
            bg.color = fase.desbloqueada
                ? new Color(Mathf.Max(fase.cor.r, 0.35f), Mathf.Max(fase.cor.g, 0.35f), Mathf.Max(fase.cor.b, 0.35f), 1f)
                : new Color(0.45f, 0.40f, 0.40f, 1f);
        }
        else
        {
            bg.color = fase.desbloqueada
                ? new Color(fase.cor.r * 0.25f, fase.cor.g * 0.25f, fase.cor.b * 0.25f, 1f)
                : new Color(0.22f, 0.22f, 0.22f, 1f);
        }

        // Botão
        Button btn = card.AddComponent<Button>();
        btn.targetGraphic  = bg;
        btn.interactable   = fase.desbloqueada;
        card.AddComponent<CardHover>();

        if (fase.desbloqueada)
        {
            string cena = fase.nomeCena;  // captura local
            int dif = fase.dificuldade;   // captura local
            btn.onClick.AddListener(() =>
            {
                Debug.Log($"[EscolherTerreno] Indo para: {cena}");
                Time.timeScale = 1f;
                PlayerPrefs.SetString("ProximaCena", cena);
                PlayerPrefs.SetInt("Dificuldade", dif);
                SceneManager.LoadScene("loading_screen");
            });

            ColorBlock cb  = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = new Color(1.0f, 0.85f, 0.85f, 1f);
            cb.pressedColor     = new Color(fase.cor.r * 1.4f, fase.cor.g * 1.4f, fase.cor.b * 1.4f, 1f);
            cb.colorMultiplier  = 1f;
            btn.colors = cb;
        }

        // Barra colorida no topo
        GameObject barra = new GameObject("Barra");
        barra.transform.SetParent(card.transform, false);
        RectTransform rB = barra.AddComponent<RectTransform>();
        rB.anchorMin = new Vector2(0f, 1f); rB.anchorMax = new Vector2(1f, 1f);
        rB.pivot     = new Vector2(0.5f, 1f);
        rB.anchoredPosition = Vector2.zero;
        rB.sizeDelta        = new Vector2(0f, 10f);
        barra.AddComponent<Image>().color = fase.desbloqueada
            ? fase.cor
            : new Color(fase.cor.r * 0.7f + 0.25f, fase.cor.g * 0.7f + 0.25f, fase.cor.b * 0.7f + 0.25f);

        // Nome
        AdicionarTexto(card, "Nome",
            new Vector2(0f, 0.72f), new Vector2(1f, 0.95f),
            string.IsNullOrEmpty(fase.nomeKey) ? fase.nome : Loc.T(fase.nomeKey), 20f, FontStyles.Bold,
            fase.desbloqueada ? Color.white : new Color(0.85f, 0.85f, 0.85f));

        // Dificuldade
        string[] difs = { "", Loc.T("diff.easy"), Loc.T("diff.normal"), Loc.T("diff.hard"), Loc.T("diff.expert"), Loc.T("diff.master") };
        Color[]  cors = { Color.white,
            new Color(0.2f,0.9f,0.2f), new Color(0.9f,0.8f,0.1f),
            new Color(1f,0.4f,0.1f),   new Color(0.8f,0.1f,0.8f), new Color(1f,0.2f,0.2f) };
        int d = Mathf.Clamp(fase.dificuldade, 1, 5);
        AdicionarTexto(card, "Dif",
            new Vector2(0f, 0.58f), new Vector2(1f, 0.74f),
            difs[d], 14f, FontStyles.Bold,
            fase.desbloqueada ? cors[d] : new Color(0.75f, 0.75f, 0.75f));

        // Descrição
        var desc = AdicionarTexto(card, "Desc",
            new Vector2(0.05f, 0.32f), new Vector2(0.95f, 0.58f),
            string.IsNullOrEmpty(fase.descricaoKey) ? fase.descricao : Loc.T(fase.descricaoKey), 13f, FontStyles.Normal,
            fase.desbloqueada ? new Color(0.8f, 0.8f, 0.8f) : new Color(0.65f, 0.65f, 0.65f));
        desc.textWrappingMode = TMPro.TextWrappingModes.Normal;

        if (fase.desbloqueada)
        {
            // Recorde (melhor tempo sobrevivido + mortes)
            float melhorTempo  = RecordeFaseManager.ObterMelhorTempo(fase.nomeCena);
            int   melhorMortes = RecordeFaseManager.ObterMelhorMortes(fase.nomeCena);
            string textoRecorde = (melhorTempo > 0f || melhorMortes > 0)
                ? $"{Loc.T("terrain.record")} {FormatarTempo(melhorTempo)}  •  {melhorMortes} {Loc.T("terrain.kills")}"
                : Loc.T("terrain.no_record");

            AdicionarTexto(card, "Recorde",
                new Vector2(0f, 0.20f), new Vector2(1f, 0.32f),
                textoRecorde, 11f, FontStyles.Italic,
                new Color(0.85f, 0.85f, 0.55f));

            // Rodapé
            AdicionarTexto(card, "Jogar",
                new Vector2(0f, 0.02f), new Vector2(1f, 0.20f),
                Loc.T("ui.play"), 17f, FontStyles.Bold, fase.cor);
        }
        else
        {
            // Banner "INDISPONÍVEL" centralizado — substitui o rodapé de fases bloqueadas
            GameObject banner = new GameObject("Indisponivel");
            banner.transform.SetParent(card.transform, false);
            RectTransform rBanner = banner.AddComponent<RectTransform>();
            rBanner.anchorMin = new Vector2(0f, 0.40f);
            rBanner.anchorMax = new Vector2(1f, 0.62f);
            rBanner.offsetMin = Vector2.zero; rBanner.offsetMax = Vector2.zero;
            banner.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.35f);

            AdicionarTexto(banner, "Texto",
                Vector2.zero, Vector2.one,
                Loc.T("terrain.unavailable"), 22f, FontStyles.Bold, new Color(1f, 0.45f, 0.4f));
        }
    }

    string FormatarTempo(float segundos)
    {
        int m = Mathf.FloorToInt(segundos / 60f);
        int s = Mathf.FloorToInt(segundos % 60f);
        return $"{m:00}:{s:00}";
    }

    // ──────────────────────────────────────────────────────────────
    // Botão Voltar

    void CriarBotaoVoltar(GameObject canvas)
    {
        GameObject go = new GameObject("BotaoVoltar");
        go.transform.SetParent(canvas.transform, false);

        RectTransform r = go.AddComponent<RectTransform>();
        r.anchorMin        = new Vector2(0f, 0f);
        r.anchorMax        = new Vector2(0f, 0f);
        r.pivot            = new Vector2(0f, 0f);
        r.anchoredPosition = new Vector2(20f, 12f);
        r.sizeDelta        = new Vector2(200f, 55f);

        Image img = go.AddComponent<Image>();
        if (spriteBotao != null)
        {
            img.sprite = spriteBotao;
            img.type   = Image.Type.Sliced;
            img.color  = new Color(
                Mathf.Clamp01(0.45f + 0.45f),
                Mathf.Clamp01(0.08f + 0.45f),
                Mathf.Clamp01(0.08f + 0.45f));
        }
        else
        {
            img.color = new Color(0.45f, 0.08f, 0.08f);
        }

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(() => SceneManager.LoadScene(cenaVoltar));

        AdicionarTexto(go, "Txt",
            Vector2.zero, Vector2.one,
            "< " + Loc.T("ui.back"), 18f, FontStyles.Bold, Color.white);
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers

    TextMeshProUGUI AdicionarTexto(GameObject parent, string nome,
        Vector2 ancMin, Vector2 ancMax,
        string texto, float size, FontStyles style, Color cor)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(parent.transform, false);
        RectTransform r = go.AddComponent<RectTransform>();
        r.anchorMin = ancMin; r.anchorMax = ancMax;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;

        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text      = texto;
        t.fontSize  = size;
        t.fontStyle = style;
        t.color     = cor;
        t.alignment = TextAlignmentOptions.Center;
        return t;
    }

    void Stretch(GameObject go)
    {
        RectTransform r = go.GetComponent<RectTransform>();
        if (r == null) r = go.AddComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }
}

// ── Partículas de brasa ───────────────────────────────────────────────────────
public class BrasasAnimador : MonoBehaviour
{
    struct Brasa
    {
        public RectTransform rt;
        public Image         img;
        public float         velY;
        public float         velX;
        public float         vida;
        public float         vidaMax;
        public float         oscFase;
    }

    const int MAX = 18;
    Brasa[] brasas = new Brasa[MAX];
    RectTransform area;

    void Awake()
    {
        area = GetComponent<RectTransform>();
        for (int i = 0; i < MAX; i++)
            brasas[i] = CriarBrasa(Random.Range(0f, 1f));  // stagger inicial
    }

    Brasa CriarBrasa(float vidaInicial = 0f)
    {
        var go = new GameObject("Brasa", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(Random.Range(3f, 7f), Random.Range(3f, 7f));
        rt.anchorMin = rt.anchorMax = new Vector2(Random.Range(0.05f, 0.95f), 0f);
        rt.anchoredPosition = new Vector2(0f, Random.Range(0f, 80f));

        var img = go.GetComponent<Image>();
        img.color        = new Color(1f, Random.Range(0.3f, 0.7f), 0f, 0f);
        img.raycastTarget = false;

        float vidaMax = Random.Range(2.5f, 5f);
        return new Brasa
        {
            rt      = rt,
            img     = img,
            velY    = Random.Range(40f, 100f),
            velX    = Random.Range(-15f, 15f),
            vida    = vidaInicial * vidaMax,
            vidaMax = vidaMax,
            oscFase = Random.Range(0f, Mathf.PI * 2f),
        };
    }

    void Update()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < MAX; i++)
        {
            ref var b = ref brasas[i];
            if (b.rt == null) { brasas[i] = CriarBrasa(); continue; }

            b.vida += dt;
            if (b.vida >= b.vidaMax)
            {
                Destroy(b.rt.gameObject);
                brasas[i] = CriarBrasa();
                continue;
            }

            float prog  = b.vida / b.vidaMax;
            float alpha = prog < 0.2f
                ? Mathf.Lerp(0f, 0.9f, prog / 0.2f)
                : Mathf.Lerp(0.9f, 0f, (prog - 0.2f) / 0.8f);

            var c = b.img.color;
            c.a = alpha;
            b.img.color = c;

            float osc = Mathf.Sin(b.vida * 3f + b.oscFase) * 12f;
            b.rt.anchoredPosition += new Vector2((b.velX + osc) * dt, b.velY * dt);
        }
    }
}

public class CardHover : MonoBehaviour,
    UnityEngine.EventSystems.IPointerEnterHandler,
    UnityEngine.EventSystems.IPointerExitHandler,
    UnityEngine.EventSystems.IPointerDownHandler,
    UnityEngine.EventSystems.IPointerUpHandler
{
    Coroutine cor;

    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData _)
        => Animar(1.06f, 0.15f);

    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData _)
        => Animar(1.00f, 0.15f);

    public void OnPointerDown(UnityEngine.EventSystems.PointerEventData _)
        => Animar(0.97f, 0.08f);

    public void OnPointerUp(UnityEngine.EventSystems.PointerEventData _)
        => Animar(1.06f, 0.08f);

    void Animar(float alvo, float dur)
    {
        if (cor != null) StopCoroutine(cor);
        cor = StartCoroutine(EscalaParaAlvo(alvo, dur));
    }

    System.Collections.IEnumerator EscalaParaAlvo(float alvo, float dur)
    {
        float inicio = transform.localScale.x;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float e = 1f - Mathf.Pow(1f - t / dur, 2f);
            float s = Mathf.Lerp(inicio, alvo, e);
            transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        transform.localScale = new Vector3(alvo, alvo, 1f);
    }
}
