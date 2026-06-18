using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// Adicione num GameObject vazio na cena de gameplay (primeira_fase).
// Aparece automaticamente apenas na primeira vez que o jogador entra.
public class TutorialUI : MonoBehaviour
{
    [Header("Configuração")]
    public bool mostrarSempre = false; // ative para testar sem apagar o PlayerPrefs

    static readonly string CHAVE = "TutorialVisto";

    // ── Passos do tutorial ───────────────────────────────────────
    struct Passo
    {
        public string titulo;
        public string icone;
        public string descricao;
        public Color  cor;
        public string tituloKey;
        public string iconeKey;
        public string descKey;
    }

    readonly Passo[] passos = new Passo[]
    {
        new Passo { titulo = "MOVIMENTO",   icone = "W A S D",         descricao = "Use WASD para mover o personagem em todas as direções.",                                       cor = new Color(0.90f, 0.40f, 0.22f), tituloKey = "tutorial.movement.title", descKey = "tutorial.movement.desc" },
        new Passo { titulo = "DASH",        icone = "SHIFT",            descricao = "Pressione SHIFT para dar um dash rápido e escapar dos inimigos.",                              cor = new Color(0.95f, 0.60f, 0.22f), tituloKey = "tutorial.dash.title",     descKey = "tutorial.dash.desc" },
        new Passo { titulo = "ATAQUE",      icone = "CLIQUE\nESQUERDO",descricao = "Clique com o botão esquerdo do mouse para atacar na direção do cursor.",                       cor = new Color(1.0f, 0.5f, 0.2f), tituloKey = "tutorial.attack.title",   descKey = "tutorial.attack.desc",   iconeKey = "tutorial.left_click" },
        new Passo { titulo = "HABILIDADES", icone = "Q  E",             descricao = "Use Q e E para ativar suas habilidades especiais. Cada uma tem um cooldown.",                 cor = new Color(0.88f, 0.32f, 0.40f), tituloKey = "tutorial.skills.title",   descKey = "tutorial.skills.desc" },
        new Passo { titulo = "ULTIMATE",    icone = "R",                descricao = "Pressione R para usar sua Ultimate. Ela carrega conforme você causa dano.",                    cor = new Color(1.0f, 0.8f, 0.1f), tituloKey = "tutorial.ultimate.title", descKey = "tutorial.ultimate.desc" },
        new Passo { titulo = "SOBREVIVA!",  icone = "♥",               descricao = "Derrote inimigos, colete XP e suba de nível. Se sua vida chegar a zero, é game over.",        cor = new Color(1.0f, 0.3f, 0.3f), tituloKey = "tutorial.survive.title",  descKey = "tutorial.survive.desc" },
    };

    // ── Estado ───────────────────────────────────────────────────
    int   passoAtual = 0;
    bool  ativo      = false;

    GameObject painelTutorial;
    TextMeshProUGUI txtTitulo, txtIcone, txtDescricao, txtContador;
    Button btnProximo, btnPular;
    GameObject[] indicadores;

    // ── Paleta ───────────────────────────────────────────────────
    static readonly Color corFundo   = new Color(0.05f, 0.02f, 0.02f, 0.96f);   // preto avermelhado
    static readonly Color corPainel  = new Color(0.10f, 0.06f, 0.06f, 1.00f);   // pedra escura
    static readonly Color corBordaTut = new Color(0.62f, 0.11f, 0.11f);          // vermelho escuro (moldura)

    void Start()
    {
        if (!mostrarSempre && PlayerPrefs.GetInt(CHAVE, 0) == 1) return;

        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        CriarUI();
        MostrarPasso(0);
        PausarJogo(true);
        ativo = true;
    }

    void Update()
    {
        if (!ativo) return;
        if (Input.GetKeyDown(KeyCode.Escape)) Fechar();
    }

    // ── Criação da UI ────────────────────────────────────────────
    void CriarUI()
    {
        // Canvas
        var canvasGO       = new GameObject("Canvas_Tutorial");
        var canvas         = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var cs             = canvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode     = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Overlay escuro atrás
        Esticar(CriarImg(canvasGO, "Overlay", new Color(0f, 0f, 0f, 0.75f)));

        // Painel central
        painelTutorial = new GameObject("Painel");
        painelTutorial.transform.SetParent(canvasGO.transform, false);
        var rp = painelTutorial.AddComponent<RectTransform>();
        rp.anchorMin = new Vector2(0.25f, 0.18f);
        rp.anchorMax = new Vector2(0.75f, 0.82f);
        rp.offsetMin = rp.offsetMax = Vector2.zero;
        painelTutorial.AddComponent<Image>().color = corPainel;

        // Moldura vermelha (laterais + base) — combina com opções/pause/game over
        var bBot = CriarImg(painelTutorial, "BordaBaixo", corBordaTut);
        var rBot = bBot.GetComponent<RectTransform>();
        rBot.anchorMin = Vector2.zero; rBot.anchorMax = new Vector2(1f, 0f);
        rBot.offsetMin = Vector2.zero; rBot.offsetMax = new Vector2(0f, 3f);
        var bEsq = CriarImg(painelTutorial, "BordaEsq", corBordaTut);
        var rEsq = bEsq.GetComponent<RectTransform>();
        rEsq.anchorMin = Vector2.zero; rEsq.anchorMax = new Vector2(0f, 1f);
        rEsq.offsetMin = Vector2.zero; rEsq.offsetMax = new Vector2(3f, 0f);
        var bDir = CriarImg(painelTutorial, "BordaDir", corBordaTut);
        var rDir = bDir.GetComponent<RectTransform>();
        rDir.anchorMin = new Vector2(1f, 0f); rDir.anchorMax = Vector2.one;
        rDir.offsetMin = new Vector2(-3f, 0f); rDir.offsetMax = Vector2.zero;

        // Borda superior colorida (cor dinâmica)
        var borda = CriarImg(painelTutorial, "Borda", Color.white);
        var rb    = borda.GetComponent<RectTransform>();
        rb.anchorMin = new Vector2(0f, 1f); rb.anchorMax = Vector2.one;
        rb.offsetMin = Vector2.zero; rb.offsetMax = new Vector2(0f, 5f);

        // Ícone / tecla grande
        txtIcone = CriarTexto(painelTutorial, "Icone",
            new Vector2(0.1f, 0.58f), new Vector2(0.9f, 0.90f),
            "", 52f, FontStyles.Bold, Color.white);

        // Título
        txtTitulo = CriarTexto(painelTutorial, "Titulo",
            new Vector2(0.05f, 0.46f), new Vector2(0.95f, 0.60f),
            "", 28f, FontStyles.Bold, Color.white);

        // Linha separadora
        var linha = CriarImg(painelTutorial, "Linha", new Color(1f, 1f, 1f, 0.1f));
        var rl    = linha.GetComponent<RectTransform>();
        rl.anchorMin = new Vector2(0.05f, 0.445f); rl.anchorMax = new Vector2(0.95f, 0.445f);
        rl.offsetMin = Vector2.zero; rl.offsetMax = new Vector2(0f, 1.5f);

        // Descrição
        txtDescricao = CriarTexto(painelTutorial, "Desc",
            new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.44f),
            "", 18f, FontStyles.Normal, new Color(0.85f, 0.85f, 0.85f));
        txtDescricao.textWrappingMode = TMPro.TextWrappingModes.Normal;

        // Indicadores de passo (bolinhas)
        CriarIndicadores(painelTutorial);

        // Botão PULAR
        btnPular = CriarBotao(painelTutorial, Loc.T("tutorial.skip"),
            new Vector2(0.05f, 0.04f), new Vector2(0.35f, 0.16f),
            new Color(0.20f, 0.08f, 0.08f), Fechar);

        // Botão PRÓXIMO
        btnProximo = CriarBotao(painelTutorial, Loc.T("tutorial.next"),
            new Vector2(0.55f, 0.04f), new Vector2(0.95f, 0.16f),
            new Color(0.60f, 0.13f, 0.13f), Avancar);
    }

    void CriarIndicadores(GameObject parent)
    {
        indicadores = new GameObject[passos.Length];
        float largTotal = passos.Length * 18f + (passos.Length - 1) * 8f;
        float startX    = -largTotal / 2f;

        for (int i = 0; i < passos.Length; i++)
        {
            var go = new GameObject($"Dot_{i}");
            go.transform.SetParent(parent.transform, false);
            var r    = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.19f);
            r.pivot     = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(14f, 14f);
            r.anchoredPosition = new Vector2(startX + i * 26f, 0f);
            go.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.25f);
            indicadores[i] = go;
        }
    }

    // ── Navegação ────────────────────────────────────────────────
    void MostrarPasso(int index)
    {
        passoAtual = index;
        var p = passos[index];

        txtIcone.text    = string.IsNullOrEmpty(p.iconeKey) ? p.icone : Loc.T(p.iconeKey);
        txtTitulo.text   = string.IsNullOrEmpty(p.tituloKey) ? p.titulo : Loc.T(p.tituloKey);
        txtDescricao.text = string.IsNullOrEmpty(p.descKey) ? p.descricao : Loc.T(p.descKey);
        txtIcone.color   = p.cor;
        txtTitulo.color  = p.cor;

        // borda superior com a cor do passo
        var borda = painelTutorial.transform.Find("Borda")?.GetComponent<Image>();
        if (borda != null) borda.color = p.cor;

        // indicadores
        for (int i = 0; i < indicadores.Length; i++)
        {
            var img = indicadores[i].GetComponent<Image>();
            img.color = i == index
                ? new Color(p.cor.r, p.cor.g, p.cor.b, 1f)
                : new Color(1f, 1f, 1f, 0.2f);
        }

        // botão próximo vira "JOGAR" no último passo
        bool ultimo = index == passos.Length - 1;
        var txtBtn  = btnProximo.transform.Find("Txt")?.GetComponent<TextMeshProUGUI>();
        if (txtBtn != null) txtBtn.text = ultimo ? Loc.T("ui.play") : Loc.T("tutorial.next");
        btnProximo.GetComponent<Image>().color = ultimo
            ? new Color(0.78f, 0.20f, 0.18f)   // JOGAR: vermelho mais vivo
            : new Color(0.55f, 0.12f, 0.12f);  // PRÓXIMO: vermelho escuro

        StartCoroutine(AnimarEntrada());
    }

    void Avancar()
    {
        if (passoAtual < passos.Length - 1)
            MostrarPasso(passoAtual + 1);
        else
            Fechar();
    }

    void Fechar()
    {
        PlayerPrefs.SetInt(CHAVE, 1);
        PlayerPrefs.Save();
        PausarJogo(false);
        ativo = false;

        var canvas = GameObject.Find("Canvas_Tutorial");
        if (canvas != null) Destroy(canvas);
        Destroy(gameObject);
    }

    IEnumerator AnimarEntrada()
    {
        var rt = painelTutorial.GetComponent<RectTransform>();
        Vector3 alvo = Vector3.one;

        painelTutorial.transform.localScale = Vector3.one * 0.92f;
        float t = 0f;
        while (t < 0.15f)
        {
            t += Time.unscaledDeltaTime;
            painelTutorial.transform.localScale = Vector3.Lerp(Vector3.one * 0.92f, alvo, t / 0.15f);
            yield return null;
        }
        painelTutorial.transform.localScale = alvo;
    }

    // ── Helpers ──────────────────────────────────────────────────
    void PausarJogo(bool pausar)
    {
        Time.timeScale = pausar ? 0f : 1f;
    }

    Button CriarBotao(GameObject parent, string label,
        Vector2 ancMin, Vector2 ancMax, Color cor, System.Action acao)
    {
        var go = new GameObject($"Btn_{label}");
        go.transform.SetParent(parent.transform, false);
        var r  = go.AddComponent<RectTransform>();
        r.anchorMin = ancMin; r.anchorMax = ancMax;
        r.offsetMin = r.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.color = cor;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition    = Selectable.Transition.None;
        btn.onClick.AddListener(() => acao());

        var txt = CriarTexto(go, "Txt", Vector2.zero, Vector2.one,
            label, 20f, FontStyles.Bold, Color.white);
        txt.alignment = TextAlignmentOptions.Center;

        // hover simples
        var h = go.AddComponent<BotaoMenuHover>();
        h.img       = img;
        h.corNormal = cor;
        h.corHover  = new Color(cor.r + 0.1f, cor.g + 0.1f, cor.b + 0.1f);
        h.corBorda  = cor;

        return btn;
    }

    GameObject CriarImg(GameObject parent, string nome, Color cor)
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
        var r  = go.AddComponent<RectTransform>();
        r.anchorMin = ancMin; r.anchorMax = ancMax;
        r.offsetMin = r.offsetMax = Vector2.zero;
        var t  = go.AddComponent<TextMeshProUGUI>();
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
