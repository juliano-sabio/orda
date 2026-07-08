using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

// Adicione num GameObject vazio na cena "lobby".
// Defina PlayerPrefs["LobbyHost"] = "1" ao criar sala, "0" ao entrar.
// Defina PlayerPrefs["LobbyCode"] com o código da sala.
public class LobbyUI : MonoBehaviour
{
    const string cenaMenu = "menu_inicial";
    const string cenaJogo = "escolher terreno";

    // ── Paleta forja (combina com Opções/Multijogador) ────────────────
    static readonly Color corFundo     = new Color(0.05f, 0.02f, 0.02f);  // #0D0505 pedra funda
    static readonly Color corAcento    = new Color(0.82f, 0.42f, 0.12f);  // âmbar-brasa (destaque/selecionado)
    static readonly Color corBorda     = new Color(0.62f, 0.11f, 0.11f);  // vermelho escuro (barras/bordas)
    static readonly Color corClaro     = new Color(0.94f, 0.88f, 0.75f);  // #F0E0C0 creme
    static readonly Color corPainel    = new Color(0.14f, 0.07f, 0.07f);  // pedra painel (preto avermelhado)
    static readonly Color corSlotVazio = new Color(0.04f, 0.02f, 0.02f, 0.55f);  // slot vazio (translúcido)
    static readonly Color corSlotOcup  = new Color(0.10f, 0.05f, 0.05f, 0.72f);  // slot ocupado
    static readonly Color corSlotHost  = new Color(0.17f, 0.09f, 0.03f, 0.74f);  // slot do HOST (âmbar escuro)
    static readonly Color corVerde     = new Color(0.16f, 0.42f, 0.20f);  // verde esmeralda escuro (pronto)
    static readonly Color corCinza     = new Color(0.28f, 0.18f, 0.18f);  // pedra cinza

    // ── Estado dos jogadores (simulado) ───────────────────────────────
    const int MAX_JOGADORES = 4;

    struct Jogador
    {
        public string nome;
        public bool   pronto;
        public bool   presente;
        public bool   isHost;
    }

    Jogador[] jogadores = new Jogador[MAX_JOGADORES];

    bool souHost   = false;
    bool estouPronto = false;
    string codigoSala = "SPIRIT-????";

    // ── Co-op (rede NGO) ──────────────────────────────────────────────
    // Quando rodando na cena de co-op (lobby_mp), o lobby deixa de ser simulado
    // e passa a refletir a sessão real (Relay + PlayerNet + LobbyManager). Na cena
    // single-player ("lobby") nada disso liga e o comportamento local é o de antes.
    bool emCoop = false;
    bool jaConectadoCoop = false; // true = voltou de uma run com a sessão NGO viva (não re-hostar/re-entrar)
    string statusCoop = "";
    TextMeshProUGUI txtCodigoSala;          // label do código (host mostra o real do Relay)
    float proxRefreshCoop = 0f;             // throttle do refresh do roster
    GameObject painelJoinCliente;           // overlay de digitar código (cliente)
    TMP_InputField campoCodigoInput;        // input do código (cliente)
    TextMeshProUGUI txtStatusCoop;          // status da conexão (cliente)
    // mapas do visual → cena co-op equivalente (só os com versão de rede entram)
    static readonly string[] mapaCoop = { "primeira_fase_mp", null, null, null };

    // ── Configuração da sala (selecionada nos botões) ─────────────────
    // Mapas na mesma ordem das opções de "Mapa" (terrain.p1/p2/p3/surv).
    // Espelha EscolherTerrenoUI: cena de destino, dificuldade e bloqueio.
    static readonly string[] mapaCenas       = { "primeira_fase", "segunda_fase", "terceira_fase", "Modo_sobrevivencia" };
    static readonly int[]    mapaDificuldade = { 1, 2, 3, 5 };
    static readonly bool[]   mapaBloqueado   = { false, false, true, true };
    int  mapaIdx        = 0;
    int  maxJogadoresSel = 2;
    bool salaPublica    = true;

    // ── Refs de UI ────────────────────────────────────────────────────
    GameObject   canvasGO;
    GameObject[] slotGOs    = new GameObject[MAX_JOGADORES];
    Image[]      slotBG     = new Image[MAX_JOGADORES];
    TextMeshProUGUI[] slotNome   = new TextMeshProUGUI[MAX_JOGADORES];
    TextMeshProUGUI[] slotStatus = new TextMeshProUGUI[MAX_JOGADORES];
    Image[]      slotIndicador  = new Image[MAX_JOGADORES];
    Image[]      slotAvatarIcon = new Image[MAX_JOGADORES];   // ícone do personagem no avatar
    TextMeshProUGUI[] slotAvatarQ = new TextMeshProUGUI[MAX_JOGADORES]; // "?" placeholder

    Button btnPronto;
    Button btnIniciar;
    Image  imgBtnPronto;
    LobbyBotaoHover hovPronto;
    TextMeshProUGUI txtBtnPronto;
    TextMeshProUGUI txtContador;

    // ── Sprites (mesmo kit da seleção de personagem) ──────────────────
    [Header("Sprites (kit seleção de personagem)")]
    [Tooltip("btn_stone — botões/abas/toggles/config.")]
    public Sprite botaoSprite;
    [Tooltip("panel_stone — fundo dos painéis e slots.")]
    public Sprite painelSprite;
    [Tooltip("bg_charselection — imagem de fundo da tela.")]
    public Sprite fundoSprite;
    [Tooltip("bar_charselect — barra horizontal (ex.: barra do código da sala).")]
    public Sprite barraSprite;
    [Tooltip("testecaractere04 — fundo dos painéis grandes (esticado, não 9-slice).")]
    public Sprite painelGrandeSprite;

    // ── Seleção de personagem ─────────────────────────────────────────
    [Header("Personagens")]
    public CharacterData[] characters;

    int charIdx = 0;

    GameObject painelSala;
    GameObject painelPersonagem;
    GameObject[] subPaineis  = new GameObject[3];
    Button[]     subAbaBtns  = new Button[3];
    Image[]      charIconBGs = new Image[0];
    TextMeshProUGUI txtInfoLobby;

    // Listas selecionáveis (ULTIMATE / PASSIVAS) com ícones
    GameObject ultiListContent, passListContent;
    Image[]    ultiItemBG = new Image[0];
    bool[]     ultiDisponivel = new bool[0];
    Image[]    passItemBG = new Image[0];
    bool[]     passDisponivel = new bool[0];

    // só estas ultimates ficam disponíveis; as demais aparecem como "Indisponível"
    static readonly string[] ultimatesLiberadas =
        { "raio certeiro", "domo retardante", "tempestade", "necropole", "drenagem" };

    // quantas passivas (as primeiras) ficam disponíveis; o resto é bloqueado
    const int PASSIVAS_LIBERADAS = 4;
    int        ultimateIdx = 0;
    int        passivaIdx  = 0;

    // ── Animação / dinâmica ───────────────────────────────────────────
    TextMeshProUGUI txtTitulo;     // título "LOBBY" (pulsa)
    Image           topoBar;       // barra de destaque do topo (brilho)
    const int QTD_BRASAS = 18;
    RectTransform[] brasaRT   = new RectTransform[QTD_BRASAS];
    float[]         brasaVel  = new float[QTD_BRASAS];
    float[]         brasaBaseX = new float[QTD_BRASAS];
    float[]         brasaAmp  = new float[QTD_BRASAS];
    float[]         brasaFase = new float[QTD_BRASAS];

    // ─────────────────────────────────────────────────────────────────
    void Start()
    {
        // Co-op: registra a lista de personagens pra a fase de rede resolver o CharacterData
        // pelo índice (sem CharacterSelectionManager). Corrige o ícone de ultimate branco em jogo.
        if (characters != null && characters.Length > 0) PlayerStats.RegistroPersonagens = characters;

        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        // lê estado salvo
        souHost     = PlayerPrefs.GetInt("LobbyHost", 1) == 1;
        codigoSala  = PlayerPrefs.GetString("LobbyCode", GerarCodigo());
        PlayerPrefs.SetString("LobbyCode", codigoSala);

        // co-op: detecta pela cena (lobby_mp = sessão real). Definido ANTES de montar
        // a UI porque a restrição de mapa do painel direito depende disso.
        emCoop = SceneManager.GetActiveScene().name == "lobby_mp";
        if (emCoop)
        {
            // souHost pelo PAPEL REAL do NGO (a sessão continua viva ao voltar da run).
            // Antes vinha do PlayerPrefs "LobbyHost", que ficava stale ao RETORNAR pro lobby
            // depois de uma run → o host aparecia como cliente e o botão START sumia.
            var nm = NetworkManager.Singleton;
            // Sessão NGO já viva (voltou de uma run pelo LoadScene em rede) → NÃO reconectar.
            jaConectadoCoop = nm != null && nm.IsListening && (nm.IsServer || nm.IsConnectedClient);
            if (nm != null && nm.IsListening) souHost = nm.IsServer;
            else                              souHost = PlayerPrefs.GetInt("LobbyHost", 1) == 1;
            // Ao voltar já conectado, reusa o código salvo; senão o host preenche com o real do Relay.
            codigoSala = jaConectadoCoop ? PlayerPrefs.GetString("LobbyCode", codigoSala)
                                         : (souHost ? "..." : "");
        }

        // popula jogadores simulados
        jogadores[0] = new Jogador { nome = "Você", pronto = false, presente = true, isHost = souHost };
        jogadores[1] = new Jogador { nome = "", pronto = false, presente = false };
        jogadores[2] = new Jogador { nome = "", pronto = false, presente = false };
        jogadores[3] = new Jogador { nome = "", pronto = false, presente = false };

        canvasGO = CriarCanvas();
        CriarFundo();
        CriarCabecalho();
        CriarSlots();
        CriarPainelDireito();
        CriarRodape();

        AtualizarSlots();

        // animação de entrada (painéis deslizam das laterais + fade), estilo seleção de personagem
        StartCoroutine(EntradaLobby());

        if (emCoop)
        {
            // sessão real: conecta via Relay e popula o roster pelo PlayerNet.
            LobbyState.EmLobby = true;
            // Cliente só precisa digitar o código numa conexão NOVA; ao voltar de uma run já está conectado.
            if (!souHost && !jaConectadoCoop) CriarPainelJoinCliente();
            ConectarCoop();
        }
        else
        {
            // single-player: simula jogador entrando após 3s (demo visual).
            StartCoroutine(SimularEntrada());
        }
    }

    // Entrada do lobby: fade geral + painéis esquerdo/direito deslizando das bordas.
    IEnumerator EntradaLobby()
    {
        var canvasCG = canvasGO.GetComponent<CanvasGroup>();
        if (canvasCG == null) canvasCG = canvasGO.AddComponent<CanvasGroup>();

        var esq = canvasGO.transform.Find("PainelJogadores") as RectTransform;
        var dir = canvasGO.transform.Find("PainelDireito")   as RectTransform;
        Vector2 e0 = esq != null ? esq.anchoredPosition : Vector2.zero;
        Vector2 d0 = dir != null ? dir.anchoredPosition : Vector2.zero;

        canvasCG.alpha = 0f;
        if (esq != null) esq.anchoredPosition = e0 + new Vector2(-180f, 0f);
        if (dir != null) dir.anchoredPosition = d0 + new Vector2( 180f, 0f);

        float dur = 0.45f, e = 0f;
        while (e < dur)
        {
            e += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(e / dur);
            float ease = 1f - Mathf.Pow(1f - k, 3f);   // easeOutCubic
            canvasCG.alpha = ease;
            if (esq != null) esq.anchoredPosition = e0 + new Vector2(Mathf.Lerp(-180f, 0f, ease), 0f);
            if (dir != null) dir.anchoredPosition = d0 + new Vector2(Mathf.Lerp( 180f, 0f, ease), 0f);
            yield return null;
        }

        canvasCG.alpha = 1f;
        if (esq != null) esq.anchoredPosition = e0;
        if (dir != null) dir.anchoredPosition = d0;
    }

    // ── Animações contínuas ───────────────────────────────────────────
    void Update()
    {
        float t = Time.unscaledTime;

        // título "LOBBY" pulsando
        if (txtTitulo != null)
        {
            float s = 1f + Mathf.Sin(t * 2f) * 0.04f;
            txtTitulo.transform.localScale = new Vector3(s, s, 1f);
        }

        // barra de destaque do topo com brilho
        if (topoBar != null)
        {
            var c = topoBar.color;
            c.a = 0.14f + (Mathf.Sin(t * 1.6f) * 0.5f + 0.5f) * 0.14f;
            topoBar.color = c;
        }

        // brasas subindo (recicla ao sair do topo)
        for (int i = 0; i < QTD_BRASAS; i++)
        {
            var rb = brasaRT[i];
            if (rb == null) continue;
            Vector2 p = rb.anchoredPosition;
            p.y += brasaVel[i] * Time.unscaledDeltaTime;
            p.x = brasaBaseX[i] + Mathf.Sin(t * 0.8f + brasaFase[i]) * brasaAmp[i];
            if (p.y > 1110f)
            {
                p.y = -20f;
                brasaBaseX[i] = Random.Range(0f, 1920f);
                p.x = brasaBaseX[i];
            }
            rb.anchoredPosition = p;
        }

        if (emCoop) AtualizarCoop();
    }

    // ── Canvas ────────────────────────────────────────────────────────
    GameObject CriarCanvas()
    {
        var go = new GameObject("Canvas_Lobby");
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

    // ── Fundo ─────────────────────────────────────────────────────────
    void CriarFundo()
    {
        var fundoGO = Img("Fundo", corFundo);
        Esticar(fundoGO);
        if (fundoSprite != null)
        {
            var fimg = fundoGO.GetComponent<Image>();
            fimg.sprite = fundoSprite;
            fimg.type   = Image.Type.Simple;
            fimg.color  = Color.white;
        }

        var topo = Img("Topo", new Color(corAcento.r, corAcento.g, corAcento.b, 0.12f));
        var rt = topo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.92f); rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        topoBar = topo.GetComponent<Image>();

        CriarBrasas();
    }

    // Brasas/partículas subindo no fundo (estilo forja).
    void CriarBrasas()
    {
        var cont = new GameObject("Brasas");
        cont.transform.SetParent(canvasGO.transform, false);
        var contRT = cont.AddComponent<RectTransform>();
        contRT.anchorMin = Vector2.zero; contRT.anchorMax = Vector2.one;
        contRT.offsetMin = contRT.offsetMax = Vector2.zero;
        var cg = cont.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false; cg.interactable = false;

        for (int i = 0; i < QTD_BRASAS; i++)
        {
            var b = new GameObject($"Brasa{i}");
            b.transform.SetParent(cont.transform, false);
            var rb = b.AddComponent<RectTransform>();
            rb.anchorMin = rb.anchorMax = Vector2.zero;
            float tam = Random.Range(3f, 7f);
            rb.sizeDelta = new Vector2(tam, tam);
            brasaBaseX[i] = Random.Range(0f, 1920f);
            float y = Random.Range(0f, 1080f);
            rb.anchoredPosition = new Vector2(brasaBaseX[i], y);
            var img = b.AddComponent<Image>();
            img.raycastTarget = false;
            img.color = new Color(corAcento.r, corAcento.g * 0.7f, corAcento.b * 0.3f,
                Random.Range(0.10f, 0.35f));
            brasaRT[i]   = rb;
            brasaVel[i]  = Random.Range(25f, 70f);
            brasaAmp[i]  = Random.Range(8f, 28f);
            brasaFase[i] = Random.Range(0f, 6.28f);
        }
    }

    // ── Cabeçalho ─────────────────────────────────────────────────────
    void CriarCabecalho()
    {
        // título
        var t = Texto("Titulo", new Vector2(0f, 0.92f), Vector2.one,
            "LOBBY", 32f, FontStyles.Bold, Color.white);
        t.alignment = TextAlignmentOptions.Center;
        txtTitulo = t;

        // código da sala + botão copiar
        var painelCodigo = new GameObject("PainelCodigo");
        painelCodigo.transform.SetParent(canvasGO.transform, false);
        var rpc = painelCodigo.AddComponent<RectTransform>();
        rpc.anchorMin = new Vector2(0.25f, 0.84f); rpc.anchorMax = new Vector2(0.75f, 0.92f);
        rpc.offsetMin = rpc.offsetMax = Vector2.zero;
        AplicarBarra(painelCodigo.AddComponent<Image>(), corPainel);

        var txtCod = Texto("TxtCodigo",
            new Vector2(0.09f, 0f), new Vector2(0.82f, 1f),
            codigoSala, 22f, FontStyles.Bold,
            new Color(0.94f, 0.88f, 0.75f));
        txtCod.transform.SetParent(painelCodigo.transform, false);
        txtCod.alignment = TextAlignmentOptions.Left;
        txtCodigoSala = txtCod;   // co-op: atualizado com o código real do Relay

        // botão copiar
        var btnCop = new GameObject("BtnCopiar");
        btnCop.transform.SetParent(painelCodigo.transform, false);
        var rbc = btnCop.AddComponent<RectTransform>();
        rbc.anchorMin = new Vector2(0.84f, 0.18f); rbc.anchorMax = new Vector2(0.98f, 0.74f);
        rbc.offsetMin = rbc.offsetMax = Vector2.zero;
        var imgCop = btnCop.AddComponent<Image>();
        AplicarSpriteBotao(imgCop, corAcento);
        var bCop = btnCop.AddComponent<Button>();
        bCop.targetGraphic = imgCop; bCop.transition = Selectable.Transition.None;
        bCop.onClick.AddListener(() =>
        {
            // Copia o código ATUAL (em co-op o código real do Relay chega async depois
            // da UI ser montada; capturar na montagem copiava o placeholder "...").
            if (string.IsNullOrEmpty(codigoSala) || codigoSala == "...") return;
            GUIUtility.systemCopyBuffer = codigoSala;
            StartCoroutine(FlashCopiar(imgCop));
        });
        var tCop = Texto("T", Vector2.zero, Vector2.one,
            Loc.T("lobby.copy"), 12f, FontStyles.Bold, Color.white);
        tCop.transform.SetParent(btnCop.transform, false);
        tCop.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        tCop.GetComponent<RectTransform>().anchorMax = Vector2.one;

        // linha separadora
        var linha = Img("Linha", corBorda);
        var rl = linha.GetComponent<RectTransform>();
        rl.anchorMin = new Vector2(0.05f, 0.837f); rl.anchorMax = new Vector2(0.95f, 0.837f);
        rl.offsetMin = Vector2.zero; rl.offsetMax = new Vector2(0f, 2f);
    }

    // ── Slots de jogadores ────────────────────────────────────────────
    void CriarSlots()
    {
        // painel esquerdo
        var painel = new GameObject("PainelJogadores");
        painel.transform.SetParent(canvasGO.transform, false);
        var rp = painel.AddComponent<RectTransform>();
        rp.anchorMin = new Vector2(0.04f, 0.12f); rp.anchorMax = new Vector2(0.52f, 0.84f);
        rp.offsetMin = rp.offsetMax = Vector2.zero;
        AplicarPainelGrande(painel.AddComponent<Image>(), corPainel);

        // barra topo
        BarraTopo(painel, corBorda);
        var lblJ = Texto("LblJ",
            new Vector2(0f, 0.90f), new Vector2(1f, 1f),
            Loc.T("lobby.players"), 15f, FontStyles.Bold, new Color(0.88f, 0.78f, 0.55f));
        lblJ.transform.SetParent(painel.transform, false);
        lblJ.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0.94f);
        lblJ.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 1.02f);
        lblJ.alignment = TextAlignmentOptions.Center;

        // contador
        txtContador = Texto("Contador",
            new Vector2(0f, 0f), Vector2.one,
            "1 / 4", 13f, FontStyles.Normal, new Color(0.65f, 0.55f, 0.35f));
        txtContador.transform.SetParent(painel.transform, false);
        var rCont = txtContador.GetComponent<RectTransform>();
        rCont.anchorMin = new Vector2(0.62f, 0.94f); rCont.anchorMax = new Vector2(0.85f, 1.02f);
        rCont.offsetMin = rCont.offsetMax = Vector2.zero;
        txtContador.alignment = TextAlignmentOptions.Left;

        for (int i = 0; i < MAX_JOGADORES; i++)
        {
            float yMax = 0.95f - i * 0.20f;
            float yMin = yMax - 0.18f;

            var slot = new GameObject($"Slot{i}");
            slot.transform.SetParent(painel.transform, false);
            var rs = slot.AddComponent<RectTransform>();
            rs.anchorMin = new Vector2(0.07f, yMin); rs.anchorMax = new Vector2(0.93f, yMax);
            rs.offsetMin = rs.offsetMax = Vector2.zero;

            // fundo liso translúcido (sem a moldura de pedra — evita "moldura dentro de moldura")
            var bg = slot.AddComponent<Image>();
            bg.color = corSlotVazio;

            // bisel sutil no topo (leve brilho)
            var bev = new GameObject("Bevel"); bev.transform.SetParent(slot.transform, false);
            var rbev = bev.AddComponent<RectTransform>();
            rbev.anchorMin = new Vector2(0f, 1f); rbev.anchorMax = new Vector2(1f, 1f);
            rbev.offsetMin = new Vector2(0f, -1.5f); rbev.offsetMax = Vector2.zero;
            var bevImg = bev.AddComponent<Image>(); bevImg.color = new Color(1f, 1f, 1f, 0.05f); bevImg.raycastTarget = false;

            // indicador de cor (borda esquerda)
            var ind = new GameObject("Ind");
            ind.transform.SetParent(slot.transform, false);
            var ri = ind.AddComponent<RectTransform>();
            ri.anchorMin = Vector2.zero; ri.anchorMax = new Vector2(0f, 1f);
            ri.offsetMin = Vector2.zero; ri.offsetMax = new Vector2(4f, 0f);
            var imgInd = ind.AddComponent<Image>();
            imgInd.color = corCinza;

            // avatar placeholder
            var av = new GameObject("Avatar");
            av.transform.SetParent(slot.transform, false);
            var rav = av.AddComponent<RectTransform>();
            rav.anchorMin = new Vector2(0.02f, 0.15f); rav.anchorMax = new Vector2(0.18f, 0.85f);
            rav.offsetMin = rav.offsetMax = Vector2.zero;
            av.AddComponent<Image>().color = new Color(0.20f, 0.10f, 0.10f);

            // ícone no avatar (placeholder "?" + sprite do personagem)
            var ic = Texto($"Ic{i}", Vector2.zero, Vector2.one,
                "?", 18f, FontStyles.Bold, new Color(0.55f, 0.45f, 0.25f));
            ic.transform.SetParent(av.transform, false);
            ic.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            ic.GetComponent<RectTransform>().anchorMax = Vector2.one;
            ic.alignment = TextAlignmentOptions.Center;
            slotAvatarQ[i] = ic;

            var avIco = new GameObject("AvatarIcone");
            avIco.transform.SetParent(av.transform, false);
            var avIcoRT = avIco.AddComponent<RectTransform>();
            avIcoRT.anchorMin = Vector2.zero;
            avIcoRT.anchorMax = Vector2.one;
            avIcoRT.offsetMin = avIcoRT.offsetMax = Vector2.zero;
            var avIcoImg = avIco.AddComponent<Image>();
            avIcoImg.raycastTarget = false;
            avIcoImg.preserveAspect = false;
            avIcoImg.enabled = false;   // só aparece quando há personagem
            slotAvatarIcon[i] = avIcoImg;

            // nome
            var nome = Texto($"Nome{i}",
                new Vector2(0.20f, 0.50f), new Vector2(0.75f, 0.95f),
                i == 0 ? "Você" : Loc.T("lobby.waiting_player"),
                14f, FontStyles.Bold,
                i == 0 ? corClaro : new Color(0.40f, 0.28f, 0.28f));
            nome.transform.SetParent(slot.transform, false);
            nome.GetComponent<RectTransform>().anchorMin = new Vector2(0.20f, 0.50f);
            nome.GetComponent<RectTransform>().anchorMax = new Vector2(0.75f, 0.95f);
            nome.GetComponent<RectTransform>().offsetMin = nome.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            nome.alignment = TextAlignmentOptions.Left;

            // status (PRONTO / HOST / ESPERANDO)
            var status = Texto($"Status{i}",
                new Vector2(0.20f, 0.05f), new Vector2(0.75f, 0.50f),
                i == 0 && souHost ? "HOST" : Loc.T("lobby.waiting"),
                11f, FontStyles.Bold,
                i == 0 && souHost ? new Color(1f, 0.8f, 0.2f) : corCinza);
            status.transform.SetParent(slot.transform, false);
            status.GetComponent<RectTransform>().anchorMin = new Vector2(0.20f, 0.05f);
            status.GetComponent<RectTransform>().anchorMax = new Vector2(0.75f, 0.50f);
            status.GetComponent<RectTransform>().offsetMin = status.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            status.alignment = TextAlignmentOptions.Left;

            // badge "PRONTO" à direita
            var badge = new GameObject("Badge");
            badge.transform.SetParent(slot.transform, false);
            var rbg2 = badge.AddComponent<RectTransform>();
            rbg2.anchorMin = new Vector2(0.76f, 0.20f); rbg2.anchorMax = new Vector2(0.97f, 0.80f);
            rbg2.offsetMin = rbg2.offsetMax = Vector2.zero;
            var imgBadge = badge.AddComponent<Image>();
            imgBadge.color = new Color(0f, 0f, 0f, 0f);
            var txtBadge = Texto($"Badge{i}", Vector2.zero, Vector2.one,
                "", 11f, FontStyles.Bold, corVerde);
            txtBadge.transform.SetParent(badge.transform, false);
            txtBadge.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            txtBadge.GetComponent<RectTransform>().anchorMax = Vector2.one;
            txtBadge.alignment = TextAlignmentOptions.Center;

            slotGOs[i]      = slot;
            slotBG[i]       = bg;
            slotNome[i]     = nome;
            slotStatus[i]   = status;
            slotIndicador[i] = imgInd;
        }
    }

    // ── Painel direito: SALA | PERSONAGEM ────────────────────────────
    void CriarPainelDireito()
    {
        var painel = new GameObject("PainelDireito");
        painel.transform.SetParent(canvasGO.transform, false);
        var rp = painel.AddComponent<RectTransform>();
        rp.anchorMin = new Vector2(0.55f, 0.12f); rp.anchorMax = new Vector2(0.96f, 0.84f);
        rp.offsetMin = rp.offsetMax = Vector2.zero;
        AplicarPainelGrande(painel.AddComponent<Image>(), corPainel);
        BarraTopo(painel, corBorda);

        // título permanente no topo do painel (acima das abas; muda conforme a aba)
        var lblTituloPainel = TextoPN(painel, "TituloPainel",
            new Vector2(0f, 0.87f), new Vector2(1f, 0.94f),
            Loc.T("lobby.room_config"), 13f, FontStyles.Bold, new Color(0.88f, 0.78f, 0.55f));
        lblTituloPainel.alignment = TextAlignmentOptions.Center;

        Color corAbaAtiva  = new Color(0.35f, 0.20f, 0.08f);
        Color corAbaInativa = new Color(0.10f, 0.05f, 0.05f);
        Image[] tabImgs = new Image[2];
        string[] tabNomes = { Loc.T("lobby.tab.room"), Loc.T("lobby.tab.character") };

        for (int i = 0; i < 2; i++)
        {
            int idx = i;
            var tabGO  = new GameObject($"TabMain_{tabNomes[i]}");
            tabGO.transform.SetParent(painel.transform, false);
            var tabImg = tabGO.AddComponent<Image>();
            AplicarSpriteBotao(tabImg, i == 0 ? corAbaAtiva : corAbaInativa);
            tabImgs[i] = tabImg;
            // recuadas pra dentro da linha cinza (alinhadas com os botões de config abaixo)
            var rt = tabGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(i == 0 ? 0.08f : 0.515f, 0.77f);
            rt.anchorMax = new Vector2(i == 0 ? 0.485f : 0.92f, 0.84f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var btn = tabGO.AddComponent<Button>();
            btn.targetGraphic = tabImg;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() =>
            {
                painelSala.SetActive(idx == 0);
                painelPersonagem.SetActive(idx == 1);
                AnimarEntrada(idx == 0 ? painelSala : painelPersonagem);
                lblTituloPainel.text = idx == 0
                    ? Loc.T("lobby.room_config")
                    : Loc.T("lobby.choose_char");
                for (int j = 0; j < 2; j++)
                    tabImgs[j].color = CorBotao(j == idx ? corAbaAtiva : corAbaInativa);
            });
            TextoPN(tabGO, "T", Vector2.zero, Vector2.one,
                tabNomes[i], 11f, FontStyles.Bold, Color.white)
                .alignment = TextAlignmentOptions.Center;
        }

        // Conteúdo SALA
        painelSala = new GameObject("ConteudoSala");
        painelSala.transform.SetParent(painel.transform, false);
        painelSala.AddComponent<RectTransform>();
        SetAnchors(painelSala, Vector2.zero, new Vector2(1f, 0.85f));
        CriarConteudoSala(painelSala);

        // Conteúdo PERSONAGEM
        painelPersonagem = new GameObject("ConteudoPersonagem");
        painelPersonagem.transform.SetParent(painel.transform, false);
        painelPersonagem.AddComponent<RectTransform>();
        SetAnchors(painelPersonagem, Vector2.zero, new Vector2(1f, 0.85f));
        CriarConteudoPersonagem(painelPersonagem);

        painelSala.SetActive(true);
        painelPersonagem.SetActive(false);
    }

    void CriarConteudoSala(GameObject pai)
    {
        // Sem a seção Dificuldade (definida pela fase em EscolherTerrenoUI).
        // Três seções distribuídas uniformemente: Mapa, Máx. Jogadores, Visibilidade.
        // co-op: só mapas com cena de rede ficam liberados (hoje, só o primeiro).
        bool[] bloqMapa = emCoop
            ? new bool[]{ mapaCoop[0] == null, mapaCoop[1] == null, mapaCoop[2] == null, mapaCoop[3] == null }
            : mapaBloqueado;
        CriarOpcaoConf(pai, Loc.T("lobby.map"), 0.70f, 0.87f,
            new[]{ Loc.T("terrain.p1.name"), Loc.T("terrain.p2.name"), Loc.T("terrain.p3.name"), Loc.T("terrain.surv.name") },
            idx => mapaIdx = idx, bloqMapa);
        // Co-op travado em 2 por enquanto (3–4 ainda não validado) → só a opção "2".
        // No modo simulado (single/local) mantém 2/3/4.
        if (emCoop)
        {
            maxJogadoresSel = 2;
            CriarOpcaoConf(pai, Loc.T("lobby.max_players"), 0.46f, 0.63f,
                new[]{ "2" },
                idx => maxJogadoresSel = 2);
        }
        else
        {
            CriarOpcaoConf(pai, Loc.T("lobby.max_players"), 0.46f, 0.63f,
                new[]{ "2", "3", "4" },
                idx => maxJogadoresSel = idx + 2);
        }

        TextoPN(pai, "LblVis", new Vector2(0.08f, 0.36f), new Vector2(0.50f, 0.45f),
            Loc.T("lobby.visibility"), 12f, FontStyles.Bold, new Color(0.88f, 0.78f, 0.55f))
            .alignment = TextAlignmentOptions.Left;

        CriarBotaoToggle(pai, "public", Loc.T("lobby.public"), new Vector2(0.08f, 0.24f), new Vector2(0.485f, 0.34f), true,
            () => salaPublica = true);
        CriarBotaoToggle(pai, "private", Loc.T("lobby.private"), new Vector2(0.515f, 0.24f), new Vector2(0.92f, 0.34f), false,
            () => salaPublica = false);

        var aviso = TextoPN(pai, "Aviso", new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.16f),
            Loc.T("lobby.notice"),
            10f, FontStyles.Italic, new Color(0.70f, 0.60f, 0.30f));
        aviso.textWrappingMode = TMPro.TextWrappingModes.Normal;
        aviso.alignment = TextAlignmentOptions.Center;
    }

    void CriarConteudoPersonagem(GameObject pai)
    {
        CriarGridPersonagens(pai);
        CriarSubAbasLobby(pai);
    }

    void CriarGridPersonagens(GameObject pai)
    {
        int total = characters != null ? characters.Length : 0;
        if (total == 0)
        {
            var t = TextoPN(pai, "SemChar",
                new Vector2(0.05f, 0.64f), new Vector2(0.95f, 0.90f),
                "Atribua o campo\n'characters' no Inspector.",
                11f, FontStyles.Italic, new Color(0.6f, 0.5f, 0.8f));
            t.textWrappingMode = TMPro.TextWrappingModes.Normal;
            return;
        }

        charIconBGs = new Image[total];
        int   slotsTotais = Mathf.Max(total, 5);   // sempre mostra ao menos 5 slots
        int   cols  = Mathf.Min(slotsTotais, 5);
        float slotW = 0.135f;     // largura do slot (quadrado-ish dada a proporção do painel)
        float slotH = 0.18f;
        float gap   = 0.02f;
        float yTop  = 0.84f;

        for (int i = 0; i < slotsTotais; i++)
        {
            int   idx = i;
            int   col = i % cols;
            int   row = i / cols;
            float xMin = 0.08f + col * (slotW + gap);
            float xMax = xMin + slotW;
            float yMax = yTop - row * (slotH + gap);
            float yMin = yMax - slotH;

            bool  temPersonagem = i < total && characters[i] != null;

            var go  = new GameObject(temPersonagem ? $"CharBtn_{i}" : $"SlotVazio_{i}");
            go.transform.SetParent(pai.transform, false);
            var r   = go.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(xMin, yMin); r.anchorMax = new Vector2(xMax, yMax);
            r.offsetMin = r.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();

            if (temPersonagem)
            {
                AplicarSpriteBotao(img, i == charIdx
                    ? new Color(0.35f, 0.20f, 0.08f)
                    : new Color(0.15f, 0.08f, 0.08f));
                charIconBGs[i] = img;
                var btn = go.AddComponent<Button>();
                btn.targetGraphic = img;
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => SelecionarPersonagemLobby(idx));

                // ícone do personagem centralizado no slot (sem texto)
                var icoGO = new GameObject("Icone");
                icoGO.transform.SetParent(go.transform, false);
                var icoRT = icoGO.AddComponent<RectTransform>();
                icoRT.anchorMin = new Vector2(0.14f, 0.12f);
                icoRT.anchorMax = new Vector2(0.86f, 0.88f);
                icoRT.offsetMin = icoRT.offsetMax = Vector2.zero;
                var icoImg = icoGO.AddComponent<Image>();
                icoImg.raycastTarget = false;
                icoImg.preserveAspect = true;
                var sp = characters[i].icon;
                if (sp != null) { icoImg.sprite = sp; icoImg.color = Color.white; }
                else            icoImg.color = new Color(1f, 1f, 1f, 0.06f);
            }
            else
            {
                // slot vazio (placeholder p/ futuros personagens)
                AplicarSpriteBotao(img, new Color(0.06f, 0.04f, 0.04f));
                TextoPN(go, "T", Vector2.zero, Vector2.one,
                    "?", 16f, FontStyles.Bold, new Color(0.40f, 0.32f, 0.30f))
                    .alignment = TextAlignmentOptions.Center;
            }
        }
    }

    void CriarSubAbasLobby(GameObject pai)
    {
        string[] nomes = { Loc.T("char.tab.info"), Loc.T("missions.tab.ultimates"), Loc.T("missions.tab.passives") };
        Color corAtiva   = new Color(0.35f, 0.20f, 0.08f);
        Color corInativa = new Color(0.10f, 0.05f, 0.05f);

        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            var tabGO  = new GameObject($"SubTab_{nomes[i]}");
            tabGO.transform.SetParent(pai.transform, false);
            var tabImg = tabGO.AddComponent<Image>();
            AplicarSpriteBotao(tabImg, i == 0 ? corAtiva : corInativa);
            var rt = tabGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.08f + i * (0.84f/3f), 0.58f);
            rt.anchorMax = new Vector2(0.08f + (i+1) * (0.84f/3f), 0.64f);
            rt.offsetMin = new Vector2(i > 0 ? 1f : 0f, 0f);
            rt.offsetMax = new Vector2(i < 2 ? -1f : 0f, 0f);
            var btn = tabGO.AddComponent<Button>();
            btn.targetGraphic = tabImg;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => MostrarSubAbaLobby(idx));
            subAbaBtns[i] = btn;
            TextoPN(tabGO, "T", Vector2.zero, Vector2.one,
                nomes[i], 9f, FontStyles.Bold, Color.white)
                .alignment = TextAlignmentOptions.Center;
        }

        // INFO
        subPaineis[0] = new GameObject("SubInfo");
        subPaineis[0].transform.SetParent(pai.transform, false);
        subPaineis[0].AddComponent<RectTransform>();
        SetAnchors(subPaineis[0], new Vector2(0f, 0.01f), new Vector2(1f, 0.58f));
        txtInfoLobby = TextoPN(subPaineis[0], "Txt",
            new Vector2(0.08f, 0.33f), new Vector2(0.92f, 0.98f),
            "—", 10f, FontStyles.Normal, new Color(0.88f, 0.82f, 0.70f));
        txtInfoLobby.textWrappingMode = TMPro.TextWrappingModes.Normal;
        txtInfoLobby.alignment = TextAlignmentOptions.TopLeft;

        // ULTIMATE (lista selecionável com ícones)
        subPaineis[1] = new GameObject("SubUltimate");
        subPaineis[1].transform.SetParent(pai.transform, false);
        subPaineis[1].AddComponent<RectTransform>();
        SetAnchors(subPaineis[1], new Vector2(0f, 0.01f), new Vector2(1f, 0.58f));
        ultiListContent = CriarListaScroll(subPaineis[1]);

        // PASSIVAS (lista selecionável com ícones)
        subPaineis[2] = new GameObject("SubPassivas");
        subPaineis[2].transform.SetParent(pai.transform, false);
        subPaineis[2].AddComponent<RectTransform>();
        SetAnchors(subPaineis[2], new Vector2(0f, 0.01f), new Vector2(1f, 0.58f));
        passListContent = CriarListaScroll(subPaineis[2]);

        MostrarSubAbaLobby(0);
        SelecionarPersonagemLobby(PlayerPrefs.GetInt("SelectedCharacter", 0));
    }

    void SelecionarPersonagemLobby(int idx)
    {
        if (characters == null || characters.Length == 0) return;
        idx = Mathf.Clamp(idx, 0, characters.Length - 1);
        charIdx = idx;
        PlayerPrefs.SetInt("SelectedCharacter", idx);

        if (charIconBGs != null)
            for (int i = 0; i < charIconBGs.Length; i++)
                if (charIconBGs[i] != null)
                    charIconBGs[i].color = CorBotao(i == idx
                        ? new Color(0.35f, 0.20f, 0.08f)
                        : new Color(0.15f, 0.08f, 0.08f));

        var data = characters[idx];
        if (data == null) return;

        // reflete o personagem escolhido no avatar do seu slot ("Você" = slot 0)
        if (slotAvatarIcon != null && slotAvatarIcon.Length > 0 && slotAvatarIcon[0] != null)
        {
            if (data.icon != null)
            {
                slotAvatarIcon[0].sprite  = data.icon;
                slotAvatarIcon[0].enabled = true;
                if (slotAvatarQ[0] != null) slotAvatarQ[0].gameObject.SetActive(false);
            }
            else
            {
                slotAvatarIcon[0].enabled = false;
                if (slotAvatarQ[0] != null) slotAvatarQ[0].gameObject.SetActive(true);
            }
        }

        string efeito = GetElementEfeitoLobby(data.baseElement);

        txtInfoLobby.text =
            $"<b>{data.GetDisplayName()}</b>\n" +
            $"{CharacterData.GetElementIcon(data.baseElement)} {data.baseElement}\n\n" +
            data.GetDisplayDescription() +
            (string.IsNullOrEmpty(efeito) ? "" : $"\n\n<i>{efeito}</i>");

        // carrega a seleção salva deste personagem e reconstrói as listas
        ultimateIdx = PlayerPrefs.GetInt($"SelectedUltimate_{idx}", 0);
        passivaIdx  = PlayerPrefs.GetInt($"SelectedPassiva_{idx}", 0);
        ConstruirListaUltimates(data);
        ConstruirListaPassivas(data);

        // co-op: propaga o personagem + ultimate/passiva POR JOGADOR (sincronizado).
        if (emCoop)
        {
            var m = MeuNet();
            if (m != null)
            {
                m.SetChar(idx);
                m.SetUltimate(ultimateIdx);
                m.SetPassiva(passivaIdx);
            }
        }
    }

    // ── Listas selecionáveis (ULTIMATE / PASSIVAS) ─────────────────────
    void ConstruirListaUltimates(CharacterData data)
    {
        if (ultiListContent == null) return;
        for (int i = ultiListContent.transform.childCount - 1; i >= 0; i--)
            Destroy(ultiListContent.transform.GetChild(i).gameObject);

        UltimateData[] ults = (data.ultimatesDisponiveis != null && data.ultimatesDisponiveis.Length > 0)
            ? data.ultimatesDisponiveis
            : (data.ultimateSkill != null ? new[] { data.ultimateSkill } : new UltimateData[0]);

        if (ults.Length == 0) { ultiItemBG = new Image[0]; ultiDisponivel = new bool[0]; return; }
        ultiItemBG = new Image[ults.Length];
        ultiDisponivel = new bool[ults.Length];
        for (int i = 0; i < ults.Length; i++) ultiDisponivel[i] = UltimateLiberada(ults[i]);

        // garante que a seleção atual seja uma ultimate disponível
        ultimateIdx = Mathf.Clamp(ultimateIdx, 0, ults.Length - 1);
        if (!ultiDisponivel[ultimateIdx])
        {
            int primeira = System.Array.FindIndex(ultiDisponivel, d => d);
            if (primeira >= 0)
            {
                ultimateIdx = primeira;
                PlayerPrefs.SetInt($"SelectedUltimate_{charIdx}", ultimateIdx);
            }
        }

        // ordem de exibição fixa pros principais; o índice salvo continua sendo o original.
        foreach (int oi in OrdenarUltimates(ults))
        {
            int   idx = oi;
            var   u   = ults[idx];
            string nome    = u != null ? u.GetDisplayName() : "?";
            string detalhe = u != null ? $"CD {u.cooldown:0}s · DMG {u.baseDamage:0} · {u.areaOfEffect:0}m" : "";
            Sprite ico     = u != null ? u.ultimateIcon : null;
            ultiItemBG[idx] = CriarItemLista(ultiListContent, nome, detalhe, ico,
                idx == ultimateIdx, () => SelecionarUltimateLobby(idx), ultiDisponivel[idx]);
        }
    }

    // ultimate liberada se o nome casar com a lista permitida
    bool UltimateLiberada(UltimateData u)
    {
        if (u == null) return false;
        string nome = Normalizar(u.GetDisplayName() + " " + u.ultimateName + " " + u.name);
        // Domo Retardante fica BLOQUEADA até completar a missão (matar Princesa Slime 2x)
        if (nome.Contains("domo retardante")) return MissaoDomoManager.DomoDesbloqueado;
        // Tempestade Elétrica fica BLOQUEADA até concluir 3 eventos de Tempestade
        if (nome.Contains("tempestade")) return MissaoTempestadeManager.Desbloqueada;
        // Necrópole fica BLOQUEADA até eliminar 500 fantasmas
        if (nome.Contains("necropole")) return MissaoNecropoleManager.Desbloqueada;
        // Drenagem de Vida fica BLOQUEADA até eliminar 500 slimes curandeiras
        if (nome.Contains("drenagem")) return MissaoDrenagemManager.Desbloqueada;
        foreach (var kw in ultimatesLiberadas) if (nome.Contains(kw)) return true;
        return false;
    }

    // Ordem de exibição das ultimates: primeiro a sequência pedida, depois o resto
    // (pseudo-aleatório estável). Retorna índices ORIGINAIS do array.
    System.Collections.Generic.List<int> OrdenarUltimates(UltimateData[] ults)
    {
        string[] pref = ultimatesLiberadas;

        var restante = new System.Collections.Generic.List<int>();
        for (int i = 0; i < ults.Length; i++) restante.Add(i);

        var ordem = new System.Collections.Generic.List<int>();
        foreach (var kw in pref)
        {
            for (int j = 0; j < restante.Count; j++)
            {
                int idx = restante[j];
                var u = ults[idx];
                string nome = u != null ? Normalizar(u.GetDisplayName() + " " + u.ultimateName + " " + u.name) : "";
                if (nome.Contains(kw)) { ordem.Add(idx); restante.RemoveAt(j); break; }
            }
        }

        // resto embaralhado com semente fixa (parece aleatório, mas estável entre aberturas)
        var rng = new System.Random(7321);
        for (int i = restante.Count - 1; i > 0; i--)
        {
            int k = rng.Next(i + 1);
            int tmp = restante[i]; restante[i] = restante[k]; restante[k] = tmp;
        }
        ordem.AddRange(restante);
        return ordem;
    }

    // minúsculas + sem acentos, pra casar nomes ("Tempestade Elétrica" → "tempestade eletrica")
    static string Normalizar(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.ToLowerInvariant();
        var sb = new System.Text.StringBuilder();
        foreach (char c in s.Normalize(System.Text.NormalizationForm.FormD))
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString();
    }

    void ConstruirListaPassivas(CharacterData data)
    {
        if (passListContent == null) return;
        for (int i = passListContent.transform.childCount - 1; i >= 0; i--)
            Destroy(passListContent.transform.GetChild(i).gameObject);

        PassiveData[] pass = data.passivasDisponiveis ?? new PassiveData[0];
        if (pass.Length == 0) { passItemBG = new Image[0]; passDisponivel = new bool[0]; return; }
        passItemBG = new Image[pass.Length];
        passDisponivel = new bool[pass.Length];
        for (int i = 0; i < pass.Length; i++)
        {
            passDisponivel[i] = (i < PASSIVAS_LIBERADAS);
            if (pass[i] == null) continue;
            string np = Normalizar(pass[i].GetDisplayName() + " " + pass[i].name);
            // Passivas bloqueadas por missão
            if (np.Contains("robusto")) passDisponivel[i] = MissaoCoracaoManager.Desbloqueada; // Coração Robusto: 150 inimigos
            if (np.Contains("cacador")) passDisponivel[i] = MissaoCacadorManager.Desbloqueada; // Caçador: 200 slimes corrompidas
            if (np.Contains("asceta"))  passDisponivel[i] = MissaoAscetaManager.Desbloqueada;  // Asceta: concluir a primeira área
        }

        // garante que a passiva selecionada esteja disponível
        passivaIdx = Mathf.Clamp(passivaIdx, 0, pass.Length - 1);
        if (!passDisponivel[passivaIdx])
        {
            int primeira = System.Array.FindIndex(passDisponivel, d => d);
            if (primeira >= 0)
            {
                passivaIdx = primeira;
                PlayerPrefs.SetInt($"SelectedPassiva_{charIdx}", passivaIdx);
            }
        }

        for (int i = 0; i < pass.Length; i++)
        {
            int   ii = i;
            var   p  = pass[i];
            string nome    = p != null ? p.GetDisplayName() : "?";
            string detalhe = p != null ? UmaLinha(p.GetBonusDescription()) : "";
            Sprite ico     = p != null ? p.passiveIcon : null;
            passItemBG[i]  = CriarItemLista(passListContent, nome, detalhe, ico,
                i == passivaIdx, () => SelecionarPassivaLobby(ii), passDisponivel[i]);
        }
    }

    void SelecionarUltimateLobby(int i)
    {
        if (i < ultiDisponivel.Length && !ultiDisponivel[i]) return; // indisponível: ignora
        ultimateIdx = i;
        PlayerPrefs.SetInt($"SelectedUltimate_{charIdx}", i);
        PlayerPrefs.Save();
        MeuNet()?.SetUltimate(i); // co-op: sincroniza POR JOGADOR (senão PlayerPrefs compartilhado deixa os dois iguais)
        for (int k = 0; k < ultiItemBG.Length; k++)
        {
            if (ultiItemBG[k] == null) continue;
            if (k < ultiDisponivel.Length && !ultiDisponivel[k]) continue; // mantém estilo "indisponível"
            EstiloItemLista(ultiItemBG[k], k == i);
        }
    }

    void SelecionarPassivaLobby(int i)
    {
        if (i < passDisponivel.Length && !passDisponivel[i]) return; // indisponível: ignora
        passivaIdx = i;
        PlayerPrefs.SetInt($"SelectedPassiva_{charIdx}", i);
        PlayerPrefs.Save();
        MeuNet()?.SetPassiva(i); // co-op: sincroniza POR JOGADOR
        for (int k = 0; k < passItemBG.Length; k++)
        {
            if (passItemBG[k] == null) continue;
            if (k < passDisponivel.Length && !passDisponivel[k]) continue; // mantém estilo "indisponível"
            EstiloItemLista(passItemBG[k], k == i);
        }
    }

    // remove tags e quebras de linha, deixando uma linha curta de bônus
    static string UmaLinha(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = System.Text.RegularExpressions.Regex.Replace(s, "<.*?>", "");
        s = s.Replace("\n", " · ").Replace("\r", "").Trim();
        return s;
    }

    void MostrarSubAbaLobby(int idx)
    {
        Color corAtiva   = new Color(0.35f, 0.20f, 0.08f);
        Color corInativa = new Color(0.10f, 0.05f, 0.05f);
        for (int i = 0; i < 3; i++)
        {
            if (subPaineis[i] != null) subPaineis[i].SetActive(i == idx);
            if (subAbaBtns[i] != null)
                subAbaBtns[i].GetComponent<Image>().color = CorBotao(i == idx ? corAtiva : corInativa);
        }
        if (idx >= 0 && idx < 3) AnimarEntrada(subPaineis[idx]);
    }

    // Animação de entrada: fade + leve escala (transição de aba/sub-aba).
    void AnimarEntrada(GameObject go)
    {
        if (go == null || !go.activeInHierarchy) return;
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        StartCoroutine(FadeEscala(go, cg));
    }

    IEnumerator FadeEscala(GameObject go, CanvasGroup cg)
    {
        var rt = go.GetComponent<RectTransform>();
        float dur = 0.18f, e = 0f;
        while (e < dur && go != null && cg != null)
        {
            e += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(e / dur);
            cg.alpha = k;
            if (rt != null) { float s = 0.97f + 0.03f * k; rt.localScale = new Vector3(s, s, 1f); }
            yield return null;
        }
        if (cg != null) cg.alpha = 1f;
        if (rt != null) rt.localScale = Vector3.one;
    }

    // Cria uma lista rolável vertical (ScrollRect) e devolve o Content onde
    // os itens são adicionados (com VerticalLayoutGroup + ContentSizeFitter).
    GameObject CriarListaScroll(GameObject pai)
    {
        var scrollRoot = new GameObject("Scroll");
        scrollRoot.transform.SetParent(pai.transform, false);
        var srRT = scrollRoot.AddComponent<RectTransform>();
        srRT.anchorMin = new Vector2(0.08f, 0.33f); srRT.anchorMax = new Vector2(0.92f, 0.98f);
        srRT.offsetMin = srRT.offsetMax = Vector2.zero;

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollRoot.transform, false);
        var vpRT = viewport.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;
        viewport.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.0001f);
        viewport.AddComponent<RectMask2D>();

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var cRT = content.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0f, 1f); cRT.anchorMax = new Vector2(1f, 1f);
        cRT.pivot = new Vector2(0.5f, 1f);
        cRT.anchoredPosition = Vector2.zero; cRT.sizeDelta = Vector2.zero;

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f; vlg.padding = new RectOffset(2, 2, 2, 2);
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childAlignment = TextAnchor.UpperCenter;

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var sr = scrollRoot.AddComponent<ScrollRect>();
        sr.content = cRT; sr.viewport = vpRT;
        sr.horizontal = false; sr.vertical = true;
        sr.scrollSensitivity = 25f;
        sr.movementType = ScrollRect.MovementType.Clamped;

        return content;
    }

    // Item de lista: ícone à esquerda + nome (negrito) + linha de detalhe.
    // Devolve o Image de fundo (para restilizar na seleção).
    Image CriarItemLista(GameObject content, string nome, string detalhe,
        Sprite icone, bool selecionado, System.Action onClick, bool disponivel = true)
    {
        var go = new GameObject("Item");
        go.transform.SetParent(content.transform, false);
        go.AddComponent<RectTransform>();
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 46f; le.preferredHeight = 46f;

        Color baseCor = !disponivel ? new Color(0.06f, 0.04f, 0.04f)
                                    : (selecionado ? corAcento : corOpcaoOff);
        var bg = go.AddComponent<Image>();
        AplicarSpriteBotao(bg, baseCor);
        var hov = go.AddComponent<LobbyBotaoHover>();
        hov.alvo = bg; hov.Definir(CorBotao(baseCor));
        hov.enabled = disponivel;     // sem brilho de hover se indisponível
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = bg; btn.transition = Selectable.Transition.None;
        btn.interactable = disponivel;
        if (disponivel) btn.onClick.AddListener(() => onClick());

        // ícone
        var icoGO = new GameObject("Icone");
        icoGO.transform.SetParent(go.transform, false);
        var icoRT = icoGO.AddComponent<RectTransform>();
        icoRT.anchorMin = new Vector2(0f, 0.5f); icoRT.anchorMax = new Vector2(0f, 0.5f);
        icoRT.pivot = new Vector2(0f, 0.5f);
        icoRT.anchoredPosition = new Vector2(8f, 0f); icoRT.sizeDelta = new Vector2(34f, 34f);
        var icoImg = icoGO.AddComponent<Image>();
        icoImg.raycastTarget = false; icoImg.preserveAspect = true;
        if (icone != null) { icoImg.sprite = icone; icoImg.color = disponivel ? Color.white : new Color(1f, 1f, 1f, 0.22f); }
        else                icoImg.color = new Color(1f, 1f, 1f, 0.06f);

        // nome
        var nm = TextoPN(go, "Nome", Vector2.zero, Vector2.one,
            nome, 11f, FontStyles.Bold,
            disponivel ? new Color(1f, 0.96f, 0.86f) : new Color(0.45f, 0.38f, 0.36f));
        nm.alignment = TextAlignmentOptions.BottomLeft;
        var nmRT = nm.GetComponent<RectTransform>();
        nmRT.anchorMin = new Vector2(0f, 0.45f); nmRT.anchorMax = Vector2.one;
        nmRT.offsetMin = new Vector2(50f, 0f); nmRT.offsetMax = new Vector2(-6f, -2f);

        // detalhe
        var dt = TextoPN(go, "Det", Vector2.zero, Vector2.one,
            detalhe, 8.5f, FontStyles.Normal,
            disponivel ? new Color(0.78f, 0.70f, 0.55f) : new Color(0.40f, 0.33f, 0.31f));
        dt.alignment = TextAlignmentOptions.TopLeft;
        dt.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        var dtRT = dt.GetComponent<RectTransform>();
        dtRT.anchorMin = Vector2.zero; dtRT.anchorMax = new Vector2(1f, 0.45f);
        dtRT.offsetMin = new Vector2(50f, 2f); dtRT.offsetMax = new Vector2(-6f, 0f);

        // overlay "INDISPONÍVEL" por cima do item bloqueado
        if (!disponivel)
        {
            var ov = new GameObject("OverlayIndisponivel");
            ov.transform.SetParent(go.transform, false);
            var rov = ov.AddComponent<RectTransform>();
            rov.anchorMin = Vector2.zero; rov.anchorMax = Vector2.one;
            rov.offsetMin = rov.offsetMax = Vector2.zero;
            var ovImg = ov.AddComponent<Image>();
            ovImg.color = new Color(0.03f, 0.02f, 0.04f, 0.62f);
            ovImg.raycastTarget = false;

            // Ultimates bloqueadas por MISSÃO (Domo, Tempestade) mostram "BLOQUEADO" dourado
            string nomeNorm = Normalizar(nome);
            bool ehBloqueado = nomeNorm.Contains("domo retardante") || nomeNorm.Contains("tempestade") || nomeNorm.Contains("necropole") || nomeNorm.Contains("drenagem") || nomeNorm.Contains("robusto") || nomeNorm.Contains("cacador") || nomeNorm.Contains("asceta");
            string rotuloBloq = ehBloqueado ? "BLOQUEADO" : "INDISPONÍVEL";
            Color  corBloq    = ehBloqueado ? new Color(1f, 0.82f, 0.3f) : new Color(0.85f, 0.32f, 0.32f);
            var tx = TextoPN(ov, "Txt", Vector2.zero, Vector2.one,
                rotuloBloq, 12f, FontStyles.Bold, corBloq);
            tx.alignment = TextAlignmentOptions.Center;
            tx.raycastTarget = false;
        }

        return bg;
    }

    void EstiloItemLista(Image bg, bool sel)
    {
        var hov = bg.GetComponent<LobbyBotaoHover>();
        Color baseCor = CorBotao(sel ? corAcento : corOpcaoOff);
        if (hov != null) hov.Definir(baseCor);
        else bg.color = baseCor;
    }

    string GetElementEfeitoLobby(PlayerStats.Element element)
    {
        switch (element)
        {
            case PlayerStats.Element.Fire:      return Loc.T("elem.fire.effect");
            case PlayerStats.Element.Ice:       return Loc.T("elem.ice.effect");
            case PlayerStats.Element.Lightning: return Loc.T("elem.lightning.effect");
            case PlayerStats.Element.Poison:    return Loc.T("elem.poison.effect");
            case PlayerStats.Element.Earth:     return Loc.T("elem.earth.effect");
            case PlayerStats.Element.Wind:      return Loc.T("elem.wind.effect");
            default:                            return "";
        }
    }

    // ── Rodapé com botões ─────────────────────────────────────────────
    void CriarRodape()
    {
        // botão VOLTAR
        CriarBotao(Loc.T("lobby.exit"),
            new Vector2(0.04f, 0.02f), new Vector2(0.25f, 0.10f),
            new Color(0.35f, 0.06f, 0.06f),
            SairLobby);

        // botão PRONTO / NÃO PRONTO
        var goPronto = new GameObject("BtnPronto");
        goPronto.transform.SetParent(canvasGO.transform, false);
        var rp = goPronto.AddComponent<RectTransform>();
        rp.anchorMin = new Vector2(0.30f, 0.02f); rp.anchorMax = new Vector2(0.60f, 0.10f);
        rp.offsetMin = rp.offsetMax = Vector2.zero;
        imgBtnPronto = goPronto.AddComponent<Image>();
        AplicarSpriteBotao(imgBtnPronto, corVerde);
        hovPronto = goPronto.AddComponent<LobbyBotaoHover>(); hovPronto.alvo = imgBtnPronto; hovPronto.Definir(CorBotao(corVerde));
        var bPronto = goPronto.AddComponent<Button>();
        bPronto.targetGraphic = imgBtnPronto; bPronto.transition = Selectable.Transition.None;
        bPronto.onClick.AddListener(TogglePronto);
        txtBtnPronto = Texto("T", Vector2.zero, Vector2.one,
            Loc.T("lobby.ready"), 18f, FontStyles.Bold, Color.white);
        txtBtnPronto.transform.SetParent(goPronto.transform, false);
        AjustarAnchors(txtBtnPronto, Vector2.zero, Vector2.one);
        txtBtnPronto.alignment = TextAlignmentOptions.Center;
        btnPronto = bPronto;

        // botão INICIAR (só host)
        var goInic = new GameObject("BtnIniciar");
        goInic.transform.SetParent(canvasGO.transform, false);
        var ri = goInic.AddComponent<RectTransform>();
        ri.anchorMin = new Vector2(0.65f, 0.02f); ri.anchorMax = new Vector2(0.96f, 0.10f);
        ri.offsetMin = ri.offsetMax = Vector2.zero;
        var imgInic = goInic.AddComponent<Image>();
        AplicarSpriteBotao(imgInic, souHost ? corAcento : corCinza);
        if (souHost) { var hi = goInic.AddComponent<LobbyBotaoHover>(); hi.alvo = imgInic; hi.Definir(CorBotao(corAcento)); }
        var bInic = goInic.AddComponent<Button>();
        bInic.targetGraphic = imgInic; bInic.transition = Selectable.Transition.None;
        bInic.interactable = souHost;
        bInic.onClick.AddListener(IniciarJogo);
        Texto("T", Vector2.zero, Vector2.one,
            souHost ? Loc.T("lobby.start_game") : Loc.T("lobby.wait_host"),
            18f, FontStyles.Bold, Color.white)
            .transform.SetParent(goInic.transform, false);
        goInic.transform.GetChild(0).GetComponent<RectTransform>().anchorMin = Vector2.zero;
        goInic.transform.GetChild(0).GetComponent<RectTransform>().anchorMax = Vector2.one;
        goInic.transform.GetChild(0).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        btnIniciar = bInic;

        // desabilita INICIAR se não é host
        goInic.SetActive(souHost);
    }

    // ── Lógica ────────────────────────────────────────────────────────
    void TogglePronto()
    {
        if (emCoop)
        {
            // rede: alterna meu ready; o visual do botão e dos slots vem do roster.
            var m = MeuNet();
            if (m != null) m.SetPronto(!m.Pronto);
            return;
        }

        estouPronto = !estouPronto;
        jogadores[0].pronto = estouPronto;

        if (estouPronto)
        {
            if (hovPronto != null) hovPronto.Definir(CorBotao(corCinza)); else imgBtnPronto.color = CorBotao(corCinza);
            txtBtnPronto.text  = Loc.T("lobby.cancel");
        }
        else
        {
            if (hovPronto != null) hovPronto.Definir(CorBotao(corVerde)); else imgBtnPronto.color = CorBotao(corVerde);
            txtBtnPronto.text  = Loc.T("lobby.ready");
        }

        AtualizarSlots();
    }

    void IniciarJogo()
    {
        if (emCoop)
        {
            // rede: só o host inicia, e só quando todos estão prontos. O NGO faz o
            // scene-load sincronizado pra todos (LobbyManager.IniciarServerRpc).
            if (!souHost || LobbyManager.Instance == null) return;
            if (!LobbyManager.Instance.TodosProntos()) return;
            LobbyManager.Instance.IniciarServerRpc();
            return;
        }

        // mapa escolhido no lobby define a cena de destino (pula "escolher terreno")
        string cena = (mapaIdx >= 0 && mapaIdx < mapaCenas.Length) ? mapaCenas[mapaIdx] : cenaJogo;
        int dif = (mapaIdx >= 0 && mapaIdx < mapaDificuldade.Length) ? mapaDificuldade[mapaIdx] : 1;

        if (Application.CanStreamedLevelBeLoaded(cena))
        {
            PlayerPrefs.SetString("ProximaCena", cena);
            PlayerPrefs.SetInt("Dificuldade", dif);
            PlayerPrefs.SetInt("VeioDoLobby", 1);   // permite "Voltar ao Lobby" no jogo
            PlayerPrefs.Save();
            SceneManager.LoadScene("loading_screen");
        }
        else
        {
            Debug.LogError($"[Lobby] Cena '{cena}' nao esta no Build Profiles. Adicione em File > Build Profiles.");
            // fallback: vai para selecao de terreno que ja deve existir
            SceneManager.LoadScene(cenaJogo);
        }
    }

    void AtualizarSlots()
    {
        int presentes = 0;
        Color[] cores = {
            corAcento,                       // P1 âmbar-brasa
            new Color(0.72f,0.16f,0.14f),    // P2 vermelho
            new Color(0.90f,0.62f,0.20f),    // P3 dourado
            new Color(0.60f,0.30f,0.10f),    // P4 bronze
        };

        for (int i = 0; i < MAX_JOGADORES; i++)
        {
            bool ocup = jogadores[i].presente;
            if (ocup) presentes++;

            slotBG[i].color = !ocup ? corSlotVazio
                            : (jogadores[i].isHost && i == 0 ? corSlotHost : corSlotOcup);
            slotIndicador[i].color = ocup ? cores[i] : corCinza;

            if (slotNome[i] == null) continue;

            if (ocup)
            {
                slotNome[i].text  = jogadores[i].nome;
                slotNome[i].color = Color.white;

                string statusTxt;
                Color  statusCor;
                if (jogadores[i].isHost && i == 0)
                { statusTxt = "HOST"; statusCor = new Color(1f,0.8f,0.2f); }
                else if (jogadores[i].pronto)
                { statusTxt = Loc.T("lobby.ready"); statusCor = corVerde; }
                else
                { statusTxt = Loc.T("lobby.waiting"); statusCor = corCinza; }

                slotStatus[i].text  = statusTxt;
                slotStatus[i].color = statusCor;
            }
            else
            {
                slotNome[i].text  = Loc.T("lobby.free_slot");
                slotNome[i].color = new Color(0.40f, 0.28f, 0.28f);
                slotStatus[i].text  = Loc.T("lobby.waiting_player");
                slotStatus[i].color = new Color(0.30f, 0.20f, 0.20f);
            }
        }

        if (txtContador != null)
            txtContador.text = $"{presentes} / {MAX_JOGADORES}";
    }

    // Simula um segundo jogador entrando (demonstração visual)
    IEnumerator SimularEntrada()
    {
        yield return new WaitForSeconds(3.5f);
        if (jogadores[1].presente) yield break;

        jogadores[1] = new Jogador
        {
            nome = "Jogador2",
            presente = true,
            pronto   = false,
            isHost   = false
        };
        AtualizarSlots();
        StartCoroutine(PiscarSlot(1));

        yield return new WaitForSeconds(4f);
        jogadores[1].pronto = true;
        AtualizarSlots();
    }

    IEnumerator PiscarSlot(int idx)
    {
        for (int i = 0; i < 3; i++)
        {
            slotBG[idx].color = new Color(corAcento.r, corAcento.g, corAcento.b, 0.55f);
            yield return new WaitForSeconds(0.12f);
            slotBG[idx].color = corSlotOcup;
            yield return new WaitForSeconds(0.12f);
        }
    }

    IEnumerator FlashCopiar(Image img)
    {
        img.color = CorBotao(corVerde);
        yield return new WaitForSeconds(0.5f);
        img.color = CorBotao(corAcento);
    }

    // ══ Co-op (rede NGO) ══════════════════════════════════════════════
    // Toda a lógica de rede vive aqui; o resto do LobbyUI continua sendo o visual
    // do single-player. Só roda quando emCoop (cena lobby_mp).

    PlayerNet MeuNet()
    {
        var l = PlayerStats.Local;
        return l != null ? l.GetComponent<PlayerNet>() : null;
    }

    async void ConectarCoop()
    {
        // Voltou de uma run com a sessão NGO ainda viva: re-hostar (nova alocação Relay) ou re-entrar
        // é o que forçava a reconexão. Aqui só reflete o estado atual e sai.
        if (jaConectadoCoop)
        {
            codigoSala = PlayerPrefs.GetString("LobbyCode", codigoSala);
            if (souHost && txtCodigoSala != null && !string.IsNullOrEmpty(codigoSala) && codigoSala != "...")
                txtCodigoSala.text = codigoSala;
            statusCoop = "Conectado.";
            AtualizarStatusCoop();
            return;
        }

        statusCoop = "Conectando...";
        AtualizarStatusCoop();
        try
        {
            await NetBootstrap.InitAsync();
            if (souHost)
            {
                codigoSala = await RelayConnector.HostAsync(RelayConnector.MaxJogadoresCoop);
                PlayerPrefs.SetString("LobbyCode", codigoSala);
                statusCoop = "Sala criada.";
                if (txtCodigoSala != null) txtCodigoSala.text = codigoSala;
            }
            else
            {
                statusCoop = "Digite o código e clique Entrar.";
            }
        }
        catch (System.Exception e) { statusCoop = "Falha: " + e.Message; }
        AtualizarStatusCoop();
    }

    async void EntrarCoop()
    {
        string code = campoCodigoInput != null ? campoCodigoInput.text.Trim() : "";
        if (string.IsNullOrEmpty(code)) { statusCoop = "Informe o código."; AtualizarStatusCoop(); return; }
        statusCoop = "Entrando...";
        AtualizarStatusCoop();
        try
        {
            await RelayConnector.JoinAsync(code);
            codigoSala = code;
            PlayerPrefs.SetString("LobbyCode", code);
            statusCoop = "Conectado.";
            if (txtCodigoSala != null) txtCodigoSala.text = code;
            if (painelJoinCliente != null) painelJoinCliente.SetActive(false);
        }
        catch (System.Exception e) { statusCoop = "Falha ao entrar: " + e.Message; }
        AtualizarStatusCoop();
    }

    void AtualizarStatusCoop()
    {
        if (txtStatusCoop != null) txtStatusCoop.text = statusCoop;
    }

    // Overlay simples (cliente): campo de código + botão Entrar, no topo do lobby.
    void CriarPainelJoinCliente()
    {
        var go = new GameObject("PainelJoinCliente");
        go.transform.SetParent(canvasGO.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.30f, 0.40f); rt.anchorMax = new Vector2(0.70f, 0.60f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        AplicarPainelGrande(go.AddComponent<Image>(), corPainel);
        painelJoinCliente = go;

        TextoPN(go, "Lbl", new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.96f),
            Loc.T("multi.room_code"), 15f, FontStyles.Bold, corClaro)
            .alignment = TextAlignmentOptions.Center;

        // campo de input
        var campo = new GameObject("Campo");
        campo.transform.SetParent(go.transform, false);
        var rc = campo.AddComponent<RectTransform>();
        rc.anchorMin = new Vector2(0.08f, 0.42f); rc.anchorMax = new Vector2(0.92f, 0.66f);
        rc.offsetMin = rc.offsetMax = Vector2.zero;
        var campoImg = campo.AddComponent<Image>();
        AplicarBarra(campoImg, new Color(0.10f, 0.06f, 0.055f));

        var area = new GameObject("Text Area");
        area.transform.SetParent(campo.transform, false);
        var ra = area.AddComponent<RectTransform>();
        ra.anchorMin = Vector2.zero; ra.anchorMax = Vector2.one;
        ra.offsetMin = new Vector2(10f, 4f); ra.offsetMax = new Vector2(-10f, -4f);
        area.AddComponent<RectMask2D>();

        var ph = TextoPN(area, "Placeholder", Vector2.zero, Vector2.one,
            "SPIRIT-1234", 16f, FontStyles.Italic, new Color(0.5f, 0.42f, 0.40f));
        ph.alignment = TextAlignmentOptions.Left;
        var txt = TextoPN(area, "Text", Vector2.zero, Vector2.one,
            "", 16f, FontStyles.Bold, corClaro);
        txt.alignment = TextAlignmentOptions.Left;

        campoCodigoInput = campo.AddComponent<TMP_InputField>();
        campoCodigoInput.targetGraphic = campoImg;
        campoCodigoInput.textViewport = ra;
        campoCodigoInput.textComponent = txt;
        campoCodigoInput.placeholder = ph;
        campoCodigoInput.text = PlayerPrefs.GetString("LobbyCode", "");

        // botão Entrar
        var btnGO = new GameObject("BtnEntrar");
        btnGO.transform.SetParent(go.transform, false);
        var rb = btnGO.AddComponent<RectTransform>();
        rb.anchorMin = new Vector2(0.30f, 0.10f); rb.anchorMax = new Vector2(0.70f, 0.36f);
        rb.offsetMin = rb.offsetMax = Vector2.zero;
        var bimg = btnGO.AddComponent<Image>();
        AplicarSpriteBotao(bimg, corVerde);
        var hv = btnGO.AddComponent<LobbyBotaoHover>(); hv.alvo = bimg; hv.Definir(CorBotao(corVerde));
        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = bimg; btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(EntrarCoop);
        TextoPN(btnGO, "T", Vector2.zero, Vector2.one,
            Loc.T("multi.join_room"), 14f, FontStyles.Bold, Color.white)
            .alignment = TextAlignmentOptions.Center;

        // status abaixo
        txtStatusCoop = TextoPN(go, "Status", new Vector2(0.05f, -0.02f), new Vector2(0.95f, 0.10f),
            statusCoop, 11f, FontStyles.Italic, new Color(0.85f, 0.66f, 0.30f));
        txtStatusCoop.alignment = TextAlignmentOptions.Center;
    }

    // Reflete o roster real (PlayerNet) nos slots e nos botões.
    void AtualizarCoop()
    {
        if (Time.unscaledTime < proxRefreshCoop) return;
        proxRefreshCoop = Time.unscaledTime + 0.25f;

        // código do host pode chegar depois da 1a montagem
        if (txtCodigoSala != null && souHost && !string.IsNullOrEmpty(codigoSala) && codigoSala != "...")
            txtCodigoSala.text = codigoSala;

        // limpa
        for (int i = 0; i < MAX_JOGADORES; i++)
        {
            jogadores[i].presente = false;
            jogadores[i].nome = "";
            jogadores[i].pronto = false;
            jogadores[i].isHost = false;
        }

        // monta o roster com o jogador local primeiro (slot 0 = "Você")
        var ordenados = new System.Collections.Generic.List<PlayerNet>();
        PlayerNet meu = MeuNet();
        if (meu != null) ordenados.Add(meu);
        for (int i = 0; i < PlayerStats.All.Count; i++)
        {
            var pn = PlayerStats.All[i] != null ? PlayerStats.All[i].GetComponent<PlayerNet>() : null;
            if (pn == null || pn == meu) continue;
            ordenados.Add(pn);
        }

        for (int s = 0; s < ordenados.Count && s < MAX_JOGADORES; s++)
        {
            var pn = ordenados[s];
            bool ehHost = pn.OwnerClientId == 0;
            string nome = pn.Nome;
            if (string.IsNullOrEmpty(nome)) nome = (pn == meu ? "Você" : ("Jogador" + (s + 1)));
            jogadores[s].presente = true;
            jogadores[s].nome = nome;
            jogadores[s].pronto = pn.Pronto;
            jogadores[s].isHost = ehHost;
        }

        AtualizarSlots();

        // avatar de cada slot ocupado = ícone do personagem escolhido
        for (int s = 0; s < ordenados.Count && s < MAX_JOGADORES; s++)
        {
            if (slotAvatarIcon == null || s >= slotAvatarIcon.Length || slotAvatarIcon[s] == null) continue;
            var pn = ordenados[s];
            CharacterData cd = (characters != null && pn.CharIndexLobby >= 0 && pn.CharIndexLobby < characters.Length)
                ? characters[pn.CharIndexLobby] : null;
            if (cd != null && cd.icon != null)
            {
                slotAvatarIcon[s].sprite = cd.icon;
                slotAvatarIcon[s].enabled = true;
                if (slotAvatarQ != null && s < slotAvatarQ.Length && slotAvatarQ[s] != null) slotAvatarQ[s].gameObject.SetActive(false);
            }
        }

        // botão PRONTO reflete meu estado
        if (txtBtnPronto != null && meu != null)
        {
            if (meu.Pronto)
            {
                if (hovPronto != null) hovPronto.Definir(CorBotao(corCinza)); else if (imgBtnPronto != null) imgBtnPronto.color = CorBotao(corCinza);
                txtBtnPronto.text = Loc.T("lobby.cancel");
            }
            else
            {
                if (hovPronto != null) hovPronto.Definir(CorBotao(corVerde)); else if (imgBtnPronto != null) imgBtnPronto.color = CorBotao(corVerde);
                txtBtnPronto.text = Loc.T("lobby.ready");
            }
        }

        // botão INICIAR (host) só habilita quando todos prontos
        if (souHost && btnIniciar != null)
            btnIniciar.interactable = LobbyManager.Instance != null && LobbyManager.Instance.TodosProntos();
    }

    void SairLobby()
    {
        if (emCoop)
        {
            CoopDesconexaoUI.SaidaIntencional = true; // saída voluntária → sem tela de "conexão perdida"
            var nm = NetworkManager.Singleton;
            if (nm != null) nm.Shutdown();
            LobbyState.EmLobby = false;
        }
        SceneManager.LoadScene(cenaMenu);
    }

    // ── Helpers de construção ─────────────────────────────────────────
    // onSelect: chamado com o índice ao selecionar (e uma vez na seleção inicial).
    // bloqueado: opções desabilitadas (ex.: mapas travados) — não selecionáveis.
    void CriarOpcaoConf(GameObject pai, string label,
        float yMin, float yMax, string[] opcoes,
        System.Action<int> onSelect = null, bool[] bloqueado = null)
    {
        var lbl = Texto($"Lbl_{label}", Vector2.zero, Vector2.one,
            label, 13f, FontStyles.Bold, new Color(0.88f, 0.78f, 0.55f));
        lbl.transform.SetParent(pai.transform, false);
        AjustarAnchors(lbl, new Vector2(0.08f, yMax - 0.05f), new Vector2(0.92f, yMax + 0.04f));
        lbl.alignment = TextAlignmentOptions.Left;

        // primeira opção não bloqueada = selecionada inicialmente
        int selInicial = 0;
        for (int i = 0; i < opcoes.Length; i++)
            if (!Trancada(bloqueado, i)) { selInicial = i; break; }

        float largura = 0.84f / opcoes.Length;
        for (int i = 0; i < opcoes.Length; i++)
        {
            int idx   = i;
            bool trancada = Trancada(bloqueado, i);
            float xMin = 0.08f + i * largura + 0.005f;
            float xMax = 0.08f + (i+1) * largura - 0.005f;

            var go = new GameObject($"Opt_{label}_{i}");
            go.transform.SetParent(pai.transform, false);
            var r  = go.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(xMin, yMin); r.anchorMax = new Vector2(xMax, yMax - 0.06f);
            r.offsetMin = r.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            if (botaoSprite != null) { img.sprite = botaoSprite; img.type = Image.Type.Sliced; }
            var hov = go.AddComponent<LobbyBotaoHover>(); hov.alvo = img;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img; btn.transition = Selectable.Transition.None;
            btn.interactable = !trancada;

            var t = Texto($"T{i}", Vector2.zero, Vector2.one,
                opcoes[i], 11f, FontStyles.Bold, Color.white);
            t.transform.SetParent(go.transform, false);
            AjustarAnchors(t, Vector2.zero, Vector2.one);
            t.alignment = TextAlignmentOptions.Center;

            if (trancada) EstiloOpcaoBloqueada(go);
            else          EstiloOpcao(go, i == selInicial);

            if (!trancada)
                btn.onClick.AddListener(() =>
                {
                    for (int j = 0; j < go.transform.parent.childCount; j++)
                    {
                        var ch = go.transform.parent.GetChild(j);
                        var chBtn = ch.GetComponent<Button>();
                        if (ch.name.StartsWith($"Opt_{label}_") && chBtn != null && chBtn.interactable)
                            EstiloOpcao(ch.gameObject, ch == go.transform);
                    }
                    if (onSelect != null) onSelect(idx);
                });
        }

        if (onSelect != null) onSelect(selInicial);
    }

    static bool Trancada(bool[] bloqueado, int i)
        => bloqueado != null && i < bloqueado.Length && bloqueado[i];

    // cor lógica de opção NÃO selecionada (pedra escura)
    static readonly Color corOpcaoOff = new Color(0.09f, 0.05f, 0.05f);

    // Visual de opção: selecionada = âmbar aceso + texto claro; senão = pedra escura + texto apagado.
    // Atualiza a cor-base do hover para o brilho do mouse ficar coerente.
    void EstiloOpcao(GameObject opt, bool sel)
    {
        Color baseCor = CorBotao(sel ? corAcento : corOpcaoOff);
        var hov = opt.GetComponent<LobbyBotaoHover>();
        if (hov != null) hov.Definir(baseCor);
        else { var im = opt.GetComponent<Image>(); if (im != null) im.color = baseCor; }

        var t = opt.GetComponentInChildren<TextMeshProUGUI>();
        if (t != null) t.color = sel ? new Color(1f, 0.97f, 0.88f) : new Color(0.60f, 0.53f, 0.50f);
    }

    // Visual de opção bloqueada (mapa travado): pedra apagada + texto cinza.
    void EstiloOpcaoBloqueada(GameObject opt)
    {
        Color baseCor = CorBotao(new Color(0.06f, 0.04f, 0.04f));
        var hov = opt.GetComponent<LobbyBotaoHover>();
        if (hov != null) hov.Definir(baseCor);
        else { var im = opt.GetComponent<Image>(); if (im != null) im.color = baseCor; }

        var t = opt.GetComponentInChildren<TextMeshProUGUI>();
        if (t != null) t.color = new Color(0.45f, 0.38f, 0.36f);
    }

    void CriarBotaoToggle(GameObject pai, string key, string displayText,
        Vector2 mn, Vector2 mx, bool ativo, System.Action onSelecionar = null)
    {
        var go  = new GameObject($"BtnT_{key}");
        go.transform.SetParent(pai.transform, false);
        var r   = go.AddComponent<RectTransform>();
        r.anchorMin = mn; r.anchorMax = mx;
        r.offsetMin = r.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        if (botaoSprite != null) { img.sprite = botaoSprite; img.type = Image.Type.Sliced; }
        var hov = go.AddComponent<LobbyBotaoHover>(); hov.alvo = img;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img; btn.transition = Selectable.Transition.None;

        var t = Texto("T", Vector2.zero, Vector2.one,
            displayText, 12f, FontStyles.Bold, Color.white);
        t.transform.SetParent(go.transform, false);
        AjustarAnchors(t, Vector2.zero, Vector2.one);
        t.alignment = TextAlignmentOptions.Center;

        EstiloOpcao(go, ativo);

        btn.onClick.AddListener(() =>
        {
            EstiloOpcao(go, true);
            var irmao = go.transform.parent.Find(key == "public" ? "BtnT_private" : "BtnT_public");
            if (irmao != null) EstiloOpcao(irmao.gameObject, false);
            if (onSelecionar != null) onSelecionar();
        });
    }

    // Cor exibida no botão: com sprite de pedra, clareia (+0.45) p/ a textura aparecer (igual à seleção).
    Color CorBotao(Color cor) => botaoSprite != null
        ? new Color(Mathf.Clamp01(cor.r + 0.45f), Mathf.Clamp01(cor.g + 0.45f), Mathf.Clamp01(cor.b + 0.45f))
        : cor;

    // Aplica o sprite de pedra (9-slice) + cor no Image de um botão.
    void AplicarSpriteBotao(Image img, Color cor)
    {
        if (botaoSprite != null)
        {
            img.sprite = botaoSprite;
            img.type   = Image.Type.Sliced;
        }
        img.color = CorBotao(cor);
    }

    // Painel de pedra (panel_stone), 9-slice, tom branco (igual à seleção de personagem).
    void AplicarPainel(Image img, Color cor)
    {
        if (painelSprite != null)
        {
            img.sprite = painelSprite;
            img.type   = Image.Type.Sliced;
            img.color  = Color.white;
        }
        else img.color = cor;
    }

    // Painel grande (testecaractere04), esticado (Simple), tom branco.
    void AplicarPainelGrande(Image img, Color cor)
    {
        if (painelGrandeSprite != null)
        {
            img.sprite = painelGrandeSprite;
            img.type   = Image.Type.Simple;
            img.color  = Color.white;
            img.preserveAspect = false;
        }
        else AplicarPainel(img, cor);
    }

    // Barra horizontal (bar_charselect), 9-slice, tom branco.
    void AplicarBarra(Image img, Color cor)
    {
        if (barraSprite != null)
        {
            img.sprite = barraSprite;
            img.type   = Image.Type.Sliced;
            img.color  = Color.white;
        }
        else AplicarPainel(img, cor);
    }


    Button CriarBotao(string label, Vector2 mn, Vector2 mx, Color cor, System.Action acao)
    {
        var go  = new GameObject($"Btn_{label}");
        go.transform.SetParent(canvasGO.transform, false);
        var r   = go.AddComponent<RectTransform>();
        r.anchorMin = mn; r.anchorMax = mx;
        r.offsetMin = r.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>(); AplicarSpriteBotao(img, cor);
        var hov = go.AddComponent<LobbyBotaoHover>(); hov.alvo = img; hov.Definir(CorBotao(cor));
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img; btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(() => acao());
        var t = Texto("T", Vector2.zero, Vector2.one,
            label, 16f, FontStyles.Bold, Color.white);
        t.transform.SetParent(go.transform, false);
        AjustarAnchors(t, Vector2.zero, Vector2.one);
        t.alignment = TextAlignmentOptions.Center;
        return btn;
    }

    void BarraTopo(GameObject pai, Color cor)
    {
        var b = new GameObject("BarraTopo"); b.transform.SetParent(pai.transform, false);
        var rb = b.AddComponent<RectTransform>();
        rb.anchorMin = new Vector2(0f,1f); rb.anchorMax = Vector2.one;
        rb.offsetMin = Vector2.zero; rb.offsetMax = new Vector2(0f,4f);
        b.AddComponent<Image>().color = cor;
    }

    void AjustarAnchors(TextMeshProUGUI t, Vector2 mn, Vector2 mx)
    {
        var r = t.GetComponent<RectTransform>();
        r.anchorMin = mn; r.anchorMax = mx;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    string GerarCodigo()
    {
        string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        string cod   = "SPIRIT-";
        for (int i = 0; i < 4; i++)
            cod += chars[Random.Range(0, chars.Length)];
        return cod;
    }

    // ── Helpers locais (parente específico) ──────────────────────────
    TextMeshProUGUI TextoPN(GameObject pai, string nome, Vector2 mn, Vector2 mx,
        string texto, float size, FontStyles style, Color cor)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai.transform, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = mn; r.anchorMax = mx;
        r.offsetMin = r.offsetMax = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = texto; t.fontSize = size;
        t.fontStyle = style; t.color = cor;
        t.alignment = TextAlignmentOptions.Center;
        return t;
    }

    void SetAnchors(GameObject go, Vector2 mn, Vector2 mx)
    {
        var r = go.GetComponent<RectTransform>();
        if (r == null) r = go.AddComponent<RectTransform>();
        r.anchorMin = mn; r.anchorMax = mx;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    // ── Helpers gráficos ──────────────────────────────────────────────
    GameObject Img(string nome, Color cor)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(canvasGO.transform, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = cor;
        return go;
    }

    TextMeshProUGUI Texto(string nome, Vector2 mn, Vector2 mx,
        string texto, float size, FontStyles style, Color cor)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(canvasGO.transform, false);
        var r  = go.AddComponent<RectTransform>();
        r.anchorMin = mn; r.anchorMax = mx;
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

// Brilho de botão ao passar o mouse: guarda a cor-base e clareia no hover, restaurando ao sair.
// Use Definir() para trocar a cor-base (ex.: quando o botão muda de estado selecionado).
public class LobbyBotaoHover : MonoBehaviour,
    UnityEngine.EventSystems.IPointerEnterHandler,
    UnityEngine.EventSystems.IPointerExitHandler
{
    public Image alvo;
    public float clarear = 0.16f;     // brilho mais vivo no hover
    public float escala  = 1.05f;     // leve aumento ao passar o mouse
    Color baseCor = Color.white;
    Vector3 escalaBase = Vector3.one;
    bool capturouEscala;

    void Awake()
    {
        if (alvo == null) alvo = GetComponent<Image>();
        escalaBase = transform.localScale;
        capturouEscala = true;
    }

    public void Definir(Color c)
    {
        baseCor = c;
        if (alvo == null) alvo = GetComponent<Image>();
        if (alvo != null) alvo.color = c;
    }

    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData e)
    {
        if (alvo != null)
            alvo.color = new Color(
                Mathf.Min(baseCor.r + clarear, 1f),
                Mathf.Min(baseCor.g + clarear, 1f),
                Mathf.Min(baseCor.b + clarear, 1f),
                baseCor.a);
        if (!capturouEscala) { escalaBase = transform.localScale; capturouEscala = true; }
        transform.localScale = escalaBase * escala;
    }

    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e)
    {
        if (alvo != null) alvo.color = baseCor;
        transform.localScale = escalaBase;
    }
}
