using System.Collections;
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

    // ── Paleta dark fantasy ───────────────────────────────────────────
    static readonly Color corFundo     = new Color(0.05f, 0.02f, 0.02f);  // #0D0505 pedra funda
    static readonly Color corAcento    = new Color(0.78f, 0.66f, 0.25f);  // #C8A840 dourado
    static readonly Color corClaro     = new Color(0.94f, 0.88f, 0.75f);  // #F0E0C0 creme
    static readonly Color corPainel    = new Color(0.18f, 0.08f, 0.08f);  // #2D1515 pedra painel
    static readonly Color corSlotVazio = new Color(0.15f, 0.07f, 0.07f);  // #261212 slot vazio
    static readonly Color corSlotOcup  = new Color(0.23f, 0.13f, 0.13f);  // #3B2121 slot ocupado
    static readonly Color corVerde     = new Color(0.15f, 0.55f, 0.20f);  // verde pronto
    static readonly Color corCinza     = new Color(0.30f, 0.20f, 0.20f);  // pedra cinza

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

    // ── Refs de UI ────────────────────────────────────────────────────
    GameObject   canvasGO;
    GameObject[] slotGOs    = new GameObject[MAX_JOGADORES];
    Image[]      slotBG     = new Image[MAX_JOGADORES];
    TextMeshProUGUI[] slotNome   = new TextMeshProUGUI[MAX_JOGADORES];
    TextMeshProUGUI[] slotStatus = new TextMeshProUGUI[MAX_JOGADORES];
    Image[]      slotIndicador  = new Image[MAX_JOGADORES];

    Button btnPronto;
    Button btnIniciar;
    Image  imgBtnPronto;
    TextMeshProUGUI txtBtnPronto;
    TextMeshProUGUI txtContador;

    // ── Seleção de personagem ─────────────────────────────────────────
    [Header("Personagens")]
    public CharacterData[] characters;

    int charIdx = 0;

    GameObject painelSala;
    GameObject painelPersonagem;
    GameObject[] subPaineis  = new GameObject[3];
    Button[]     subAbaBtns  = new Button[3];
    Image[]      charIconBGs = new Image[0];
    TextMeshProUGUI txtInfoLobby, txtUltimateLobby, txtPassivasLobby;

    // ─────────────────────────────────────────────────────────────────
    void Start()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        // lê estado salvo
        souHost     = PlayerPrefs.GetInt("LobbyHost", 1) == 1;
        codigoSala  = PlayerPrefs.GetString("LobbyCode", GerarCodigo());
        PlayerPrefs.SetString("LobbyCode", codigoSala);

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

        // simula jogador entrando após 3s (para demo)
        StartCoroutine(SimularEntrada());
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
        Esticar(Img("Fundo", corFundo));

        var topo = Img("Topo", new Color(corAcento.r, corAcento.g, corAcento.b, 0.12f));
        var rt = topo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.92f); rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // ── Cabeçalho ─────────────────────────────────────────────────────
    void CriarCabecalho()
    {
        // título
        var t = Texto("Titulo", new Vector2(0f, 0.92f), Vector2.one,
            "LOBBY", 32f, FontStyles.Bold, Color.white);
        t.alignment = TextAlignmentOptions.Center;

        // código da sala + botão copiar
        var painelCodigo = new GameObject("PainelCodigo");
        painelCodigo.transform.SetParent(canvasGO.transform, false);
        var rpc = painelCodigo.AddComponent<RectTransform>();
        rpc.anchorMin = new Vector2(0.25f, 0.84f); rpc.anchorMax = new Vector2(0.75f, 0.92f);
        rpc.offsetMin = rpc.offsetMax = Vector2.zero;
        painelCodigo.AddComponent<Image>().color = corPainel;

        var txtCod = Texto("TxtCodigo",
            new Vector2(0.05f, 0f), new Vector2(0.78f, 1f),
            codigoSala, 22f, FontStyles.Bold,
            new Color(0.94f, 0.88f, 0.75f));
        txtCod.transform.SetParent(painelCodigo.transform, false);
        txtCod.alignment = TextAlignmentOptions.Left;

        // botão copiar
        var btnCop = new GameObject("BtnCopiar");
        btnCop.transform.SetParent(painelCodigo.transform, false);
        var rbc = btnCop.AddComponent<RectTransform>();
        rbc.anchorMin = new Vector2(0.80f, 0.10f); rbc.anchorMax = new Vector2(0.98f, 0.90f);
        rbc.offsetMin = rbc.offsetMax = Vector2.zero;
        var imgCop = btnCop.AddComponent<Image>();
        imgCop.color = new Color(corAcento.r, corAcento.g, corAcento.b, 0.60f);
        var bCop = btnCop.AddComponent<Button>();
        bCop.targetGraphic = imgCop; bCop.transition = Selectable.Transition.None;
        string codCap = codigoSala;
        bCop.onClick.AddListener(() =>
        {
            GUIUtility.systemCopyBuffer = codCap;
            StartCoroutine(FlashCopiar(imgCop));
        });
        var tCop = Texto("T", Vector2.zero, Vector2.one,
            "COPIAR", 12f, FontStyles.Bold, Color.white);
        tCop.transform.SetParent(btnCop.transform, false);
        tCop.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        tCop.GetComponent<RectTransform>().anchorMax = Vector2.one;

        // linha separadora
        var linha = Img("Linha", corAcento);
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
        painel.AddComponent<Image>().color = corPainel;

        // barra topo
        BarraTopo(painel, corAcento);
        var lblJ = Texto("LblJ",
            new Vector2(0f, 0.90f), new Vector2(1f, 1f),
            "JOGADORES", 15f, FontStyles.Bold, new Color(0.88f, 0.78f, 0.55f));
        lblJ.transform.SetParent(painel.transform, false);
        lblJ.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0.92f);
        lblJ.GetComponent<RectTransform>().anchorMax = Vector2.one;
        lblJ.alignment = TextAlignmentOptions.Center;

        // contador
        txtContador = Texto("Contador",
            new Vector2(0f, 0f), Vector2.one,
            "1 / 4", 13f, FontStyles.Normal, new Color(0.65f, 0.55f, 0.35f));
        txtContador.transform.SetParent(painel.transform, false);
        var rCont = txtContador.GetComponent<RectTransform>();
        rCont.anchorMin = new Vector2(0.70f, 0.92f); rCont.anchorMax = Vector2.one;
        rCont.offsetMin = rCont.offsetMax = Vector2.zero;
        txtContador.alignment = TextAlignmentOptions.Right;

        for (int i = 0; i < MAX_JOGADORES; i++)
        {
            float yMax = 0.88f - i * 0.22f;
            float yMin = yMax - 0.18f;

            var slot = new GameObject($"Slot{i}");
            slot.transform.SetParent(painel.transform, false);
            var rs = slot.AddComponent<RectTransform>();
            rs.anchorMin = new Vector2(0.04f, yMin); rs.anchorMax = new Vector2(0.96f, yMax);
            rs.offsetMin = rs.offsetMax = Vector2.zero;

            var bg = slot.AddComponent<Image>();
            bg.color = corSlotVazio;

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

            // ícone no avatar
            var ic = Texto($"Ic{i}", Vector2.zero, Vector2.one,
                "?", 18f, FontStyles.Bold, new Color(0.55f, 0.45f, 0.25f));
            ic.transform.SetParent(av.transform, false);
            ic.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            ic.GetComponent<RectTransform>().anchorMax = Vector2.one;
            ic.alignment = TextAlignmentOptions.Center;

            // nome
            var nome = Texto($"Nome{i}",
                new Vector2(0.20f, 0.50f), new Vector2(0.75f, 0.95f),
                i == 0 ? "Você" : "Aguardando...",
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
                i == 0 && souHost ? "HOST" : "ESPERANDO",
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
        painel.AddComponent<Image>().color = corPainel;
        BarraTopo(painel, new Color(0.15f, 0.08f, 0.05f));

        Color corAbaAtiva  = new Color(0.35f, 0.20f, 0.08f);
        Color corAbaInativa = new Color(0.10f, 0.05f, 0.05f);
        Image[] tabImgs = new Image[2];
        string[] tabNomes = { "SALA", "PERSONAGEM" };

        for (int i = 0; i < 2; i++)
        {
            int idx = i;
            var tabGO  = new GameObject($"TabMain_{tabNomes[i]}");
            tabGO.transform.SetParent(painel.transform, false);
            var tabImg = tabGO.AddComponent<Image>();
            tabImg.color = i == 0 ? corAbaAtiva : corAbaInativa;
            tabImgs[i] = tabImg;
            var rt = tabGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(i * 0.5f, 0.93f);
            rt.anchorMax = new Vector2((i + 1) * 0.5f, 1.00f);
            rt.offsetMin = new Vector2(i > 0 ? 1f : 0f, 0f);
            rt.offsetMax = new Vector2(i < 1 ? -1f : 0f, 0f);
            var btn = tabGO.AddComponent<Button>();
            btn.targetGraphic = tabImg;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() =>
            {
                painelSala.SetActive(idx == 0);
                painelPersonagem.SetActive(idx == 1);
                for (int j = 0; j < 2; j++)
                    tabImgs[j].color = j == idx ? corAbaAtiva : corAbaInativa;
            });
            TextoPN(tabGO, "T", Vector2.zero, Vector2.one,
                tabNomes[i], 11f, FontStyles.Bold, Color.white)
                .alignment = TextAlignmentOptions.Center;
        }

        // Conteúdo SALA
        painelSala = new GameObject("ConteudoSala");
        painelSala.transform.SetParent(painel.transform, false);
        painelSala.AddComponent<RectTransform>();
        SetAnchors(painelSala, Vector2.zero, new Vector2(1f, 0.93f));
        CriarConteudoSala(painelSala);

        // Conteúdo PERSONAGEM
        painelPersonagem = new GameObject("ConteudoPersonagem");
        painelPersonagem.transform.SetParent(painel.transform, false);
        painelPersonagem.AddComponent<RectTransform>();
        SetAnchors(painelPersonagem, Vector2.zero, new Vector2(1f, 0.93f));
        CriarConteudoPersonagem(painelPersonagem);

        painelSala.SetActive(true);
        painelPersonagem.SetActive(false);
    }

    void CriarConteudoSala(GameObject pai)
    {
        TextoPN(pai, "LblConf", new Vector2(0f, 0.92f), Vector2.one,
            "CONFIGURAÇÕES DA SALA", 13f, FontStyles.Bold, new Color(0.88f, 0.78f, 0.55f))
            .alignment = TextAlignmentOptions.Center;

        CriarOpcaoConf(pai, "Mapa", 0.72f, 0.87f,
            new[]{ "Fase 1", "Fase 2", "Fase 3", "Sobrev." });
        CriarOpcaoConf(pai, "Dificuldade", 0.52f, 0.67f,
            new[]{ "Fácil", "Normal", "Difícil" });
        CriarOpcaoConf(pai, "Máx. Jogadores", 0.32f, 0.47f,
            new[]{ "2", "3", "4" });

        TextoPN(pai, "LblVis", new Vector2(0.05f, 0.22f), new Vector2(0.50f, 0.31f),
            "Visibilidade", 12f, FontStyles.Bold, new Color(0.88f, 0.78f, 0.55f))
            .alignment = TextAlignmentOptions.Left;

        CriarBotaoToggle(pai, "Pública", new Vector2(0.05f, 0.12f), new Vector2(0.47f, 0.21f), true);
        CriarBotaoToggle(pai, "Privada", new Vector2(0.53f, 0.12f), new Vector2(0.95f, 0.21f), false);

        var aviso = TextoPN(pai, "Aviso", new Vector2(0.05f, 0.01f), new Vector2(0.95f, 0.11f),
            "⚠  Networking não implementado — aparência apenas",
            10f, FontStyles.Italic, new Color(0.70f, 0.60f, 0.30f));
        aviso.enableWordWrapping = true;
        aviso.alignment = TextAlignmentOptions.Center;
    }

    void CriarConteudoPersonagem(GameObject pai)
    {
        TextoPN(pai, "LblP", new Vector2(0f, 0.92f), Vector2.one,
            "ESCOLHA SEU PERSONAGEM", 12f, FontStyles.Bold, new Color(0.88f, 0.78f, 0.55f))
            .alignment = TextAlignmentOptions.Center;

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
            t.enableWordWrapping = true;
            return;
        }

        charIconBGs = new Image[total];
        int   cols    = Mathf.Min(total, 5);
        float largura = 0.90f / cols;
        float altura  = 0.11f;
        float yBase   = 0.88f;

        for (int i = 0; i < total; i++)
        {
            int   idx = i;
            int   col = i % cols;
            int   row = i / cols;
            float xMin = 0.05f + col * largura;
            float xMax = xMin + largura - 0.01f;
            float yMax = yBase - row * (altura + 0.02f);
            float yMin = yMax - altura;

            var go  = new GameObject($"CharBtn_{i}");
            go.transform.SetParent(pai.transform, false);
            var r   = go.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(xMin, yMin); r.anchorMax = new Vector2(xMax, yMax);
            r.offsetMin = r.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = i == charIdx
                ? new Color(0.35f, 0.20f, 0.08f)
                : new Color(0.15f, 0.08f, 0.08f);
            charIconBGs[i] = img;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => SelecionarPersonagemLobby(idx));

            string nm = characters[i] != null ? characters[i].characterName : "?";
            if (nm.Length > 7) nm = nm.Substring(0, 7);
            TextoPN(go, "T", Vector2.zero, Vector2.one,
                nm, 9f, FontStyles.Bold, Color.white)
                .alignment = TextAlignmentOptions.Center;
        }
    }

    void CriarSubAbasLobby(GameObject pai)
    {
        string[] nomes = { "INFO", "ULTIMATE", "PASSIVAS" };
        Color corAtiva   = new Color(0.35f, 0.20f, 0.08f);
        Color corInativa = new Color(0.10f, 0.05f, 0.05f);

        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            var tabGO  = new GameObject($"SubTab_{nomes[i]}");
            tabGO.transform.SetParent(pai.transform, false);
            var tabImg = tabGO.AddComponent<Image>();
            tabImg.color = i == 0 ? corAtiva : corInativa;
            var rt = tabGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(i * (1f/3f), 0.58f);
            rt.anchorMax = new Vector2((i+1) * (1f/3f), 0.64f);
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
            new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.98f),
            "—", 10f, FontStyles.Normal, new Color(0.88f, 0.82f, 0.70f));
        txtInfoLobby.enableWordWrapping = true;
        txtInfoLobby.alignment = TextAlignmentOptions.TopLeft;

        // ULTIMATE
        subPaineis[1] = new GameObject("SubUltimate");
        subPaineis[1].transform.SetParent(pai.transform, false);
        subPaineis[1].AddComponent<RectTransform>();
        SetAnchors(subPaineis[1], new Vector2(0f, 0.01f), new Vector2(1f, 0.58f));
        txtUltimateLobby = TextoPN(subPaineis[1], "Txt",
            new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.98f),
            "—", 10f, FontStyles.Normal, new Color(0.94f, 0.88f, 0.75f));
        txtUltimateLobby.enableWordWrapping = true;
        txtUltimateLobby.alignment = TextAlignmentOptions.TopLeft;

        // PASSIVAS
        subPaineis[2] = new GameObject("SubPassivas");
        subPaineis[2].transform.SetParent(pai.transform, false);
        subPaineis[2].AddComponent<RectTransform>();
        SetAnchors(subPaineis[2], new Vector2(0f, 0.01f), new Vector2(1f, 0.58f));
        txtPassivasLobby = TextoPN(subPaineis[2], "Txt",
            new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.98f),
            "—", 10f, FontStyles.Normal, new Color(0.72f, 0.88f, 0.68f));
        txtPassivasLobby.enableWordWrapping = true;
        txtPassivasLobby.alignment = TextAlignmentOptions.TopLeft;

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
                    charIconBGs[i].color = i == idx
                        ? new Color(0.35f, 0.20f, 0.08f)
                        : new Color(0.15f, 0.08f, 0.08f);

        var data = characters[idx];
        if (data == null) return;

        string efeito = GetElementEfeitoLobby(data.baseElement);

        txtInfoLobby.text =
            $"<b>{data.characterName}</b>\n" +
            $"{CharacterData.GetElementIcon(data.baseElement)} {data.baseElement}\n\n" +
            data.description +
            (string.IsNullOrEmpty(efeito) ? "" : $"\n\n<i>{efeito}</i>");

        var u = data.ultimateSkill;
        if (u == null && data.HasUltimatesDisponiveis()) u = data.ultimatesDisponiveis[0];
        if (u != null)
        {
            string elem = u.element != PlayerStats.Element.None
                ? $"{u.GetElementIcon()} {u.element}  |  " : "";
            txtUltimateLobby.text =
                $"<b>{u.ultimateName}</b>\n" +
                $"{elem}CD: {u.cooldown}s\n" +
                $"DMG: {u.baseDamage}  Área: {u.areaOfEffect}m\n\n" +
                u.description +
                (string.IsNullOrEmpty(u.specialEffect) ? "" : $"\n<i>{u.specialEffect}</i>");
        }
        else
            txtUltimateLobby.text = "Nenhuma ultimate disponível.";

        string bonuses = data.GetElementBonusDescription();
        string passivas = (bonuses != "Sem bônus elemental" ? bonuses + "\n\n" : "") +
            $"Regen HP: +{data.baseHealthRegen}/s  (delay {data.baseRegenDelay}s)";
        if (!string.IsNullOrEmpty(efeito)) passivas += $"\n\n<i>{efeito}</i>";
        txtPassivasLobby.text = passivas;
    }

    void MostrarSubAbaLobby(int idx)
    {
        Color corAtiva   = new Color(0.35f, 0.20f, 0.08f);
        Color corInativa = new Color(0.10f, 0.05f, 0.05f);
        for (int i = 0; i < 3; i++)
        {
            if (subPaineis[i] != null) subPaineis[i].SetActive(i == idx);
            if (subAbaBtns[i] != null)
                subAbaBtns[i].GetComponent<Image>().color = i == idx ? corAtiva : corInativa;
        }
    }

    string GetElementEfeitoLobby(PlayerStats.Element element)
    {
        switch (element)
        {
            case PlayerStats.Element.Fire:      return "Inimigos atingidos sofrem queimadura.";
            case PlayerStats.Element.Ice:       return "Chance de lentificar inimigos.";
            case PlayerStats.Element.Lightning: return "Dano em cadeia entre inimigos.";
            case PlayerStats.Element.Poison:    return "Aplica veneno com dano por segundo.";
            case PlayerStats.Element.Earth:     return "Chance de atordoar inimigos.";
            case PlayerStats.Element.Wind:      return "Bônus de velocidade e repulsão.";
            default:                            return "";
        }
    }

    // ── Rodapé com botões ─────────────────────────────────────────────
    void CriarRodape()
    {
        // botão VOLTAR
        CriarBotao("← SAIR DO LOBBY",
            new Vector2(0.04f, 0.02f), new Vector2(0.25f, 0.10f),
            new Color(0.35f, 0.06f, 0.06f),
            () => SceneManager.LoadScene(cenaMenu));

        // botão PRONTO / NÃO PRONTO
        var goPronto = new GameObject("BtnPronto");
        goPronto.transform.SetParent(canvasGO.transform, false);
        var rp = goPronto.AddComponent<RectTransform>();
        rp.anchorMin = new Vector2(0.30f, 0.02f); rp.anchorMax = new Vector2(0.60f, 0.10f);
        rp.offsetMin = rp.offsetMax = Vector2.zero;
        imgBtnPronto = goPronto.AddComponent<Image>();
        imgBtnPronto.color = corVerde;
        var bPronto = goPronto.AddComponent<Button>();
        bPronto.targetGraphic = imgBtnPronto; bPronto.transition = Selectable.Transition.None;
        bPronto.onClick.AddListener(TogglePronto);
        txtBtnPronto = Texto("T", Vector2.zero, Vector2.one,
            "✔  PRONTO", 18f, FontStyles.Bold, Color.white);
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
        imgInic.color = souHost ? corAcento : corCinza;
        var bInic = goInic.AddComponent<Button>();
        bInic.targetGraphic = imgInic; bInic.transition = Selectable.Transition.None;
        bInic.interactable = souHost;
        bInic.onClick.AddListener(IniciarJogo);
        Texto("T", Vector2.zero, Vector2.one,
            souHost ? "▶  INICIAR JOGO" : "Aguarde o host",
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
        estouPronto = !estouPronto;
        jogadores[0].pronto = estouPronto;

        if (estouPronto)
        {
            imgBtnPronto.color = corCinza;
            txtBtnPronto.text  = "✖  CANCELAR";
        }
        else
        {
            imgBtnPronto.color = corVerde;
            txtBtnPronto.text  = "✔  PRONTO";
        }

        AtualizarSlots();
    }

    void IniciarJogo()
    {
        // verifica se a cena existe antes de tentar carregar
        if (Application.CanStreamedLevelBeLoaded(cenaJogo))
        {
            PlayerPrefs.SetString("ProximaCena", cenaJogo);
            SceneManager.LoadScene("loading_screen");
        }
        else
        {
            Debug.LogError($"[Lobby] Cena '{cenaJogo}' nao esta no Build Profiles. Adicione em File > Build Profiles.");
            // fallback: vai para selecao de personagem que ja deve existir
            SceneManager.LoadScene("CharacterSelection");
        }
    }

    void AtualizarSlots()
    {
        int presentes = 0;
        Color[] cores = {
            corAcento,
            new Color(0.10f,0.55f,0.85f),
            new Color(0.85f,0.45f,0.10f),
            new Color(0.20f,0.70f,0.30f),
        };

        for (int i = 0; i < MAX_JOGADORES; i++)
        {
            bool ocup = jogadores[i].presente;
            if (ocup) presentes++;

            slotBG[i].color = ocup ? corSlotOcup : corSlotVazio;
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
                { statusTxt = "✔ PRONTO"; statusCor = corVerde; }
                else
                { statusTxt = "Aguardando..."; statusCor = corCinza; }

                slotStatus[i].text  = statusTxt;
                slotStatus[i].color = statusCor;
            }
            else
            {
                slotNome[i].text  = "Vaga livre";
                slotNome[i].color = new Color(0.40f, 0.28f, 0.28f);
                slotStatus[i].text  = "Aguardando jogador...";
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
            slotBG[idx].color = corAcento;
            yield return new WaitForSeconds(0.12f);
            slotBG[idx].color = corSlotOcup;
            yield return new WaitForSeconds(0.12f);
        }
    }

    IEnumerator FlashCopiar(Image img)
    {
        img.color = corVerde;
        yield return new WaitForSeconds(0.5f);
        img.color = new Color(corAcento.r, corAcento.g, corAcento.b, 0.60f);
    }

    // ── Helpers de construção ─────────────────────────────────────────
    void CriarOpcaoConf(GameObject pai, string label,
        float yMin, float yMax, string[] opcoes)
    {
        var lbl = Texto($"Lbl_{label}", Vector2.zero, Vector2.one,
            label, 13f, FontStyles.Bold, new Color(0.88f, 0.78f, 0.55f));
        lbl.transform.SetParent(pai.transform, false);
        AjustarAnchors(lbl, new Vector2(0.05f, yMax - 0.05f), new Vector2(0.95f, yMax + 0.04f));
        lbl.alignment = TextAlignmentOptions.Left;

        float largura = 0.88f / opcoes.Length;
        for (int i = 0; i < opcoes.Length; i++)
        {
            int idx   = i;
            float xMin = 0.05f + i * largura + 0.005f;
            float xMax = 0.05f + (i+1) * largura - 0.005f;

            var go = new GameObject($"Opt_{label}_{i}");
            go.transform.SetParent(pai.transform, false);
            var r  = go.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(xMin, yMin); r.anchorMax = new Vector2(xMax, yMax - 0.06f);
            r.offsetMin = r.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = i == 0 ? corAcento : new Color(0.18f, 0.10f, 0.10f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img; btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() =>
            {
                // recolore o grupo
                for (int j = 0; j < go.transform.parent.childCount; j++)
                {
                    var ch = go.transform.parent.GetChild(j);
                    if (ch.name.StartsWith($"Opt_{label}_"))
                    {
                        var ci = ch.GetComponent<Image>();
                        if (ci != null) ci.color = new Color(0.18f, 0.10f, 0.10f);
                    }
                }
                img.color = corAcento;
            });

            var t = Texto($"T{i}", Vector2.zero, Vector2.one,
                opcoes[i], 11f, FontStyles.Bold, Color.white);
            t.transform.SetParent(go.transform, false);
            AjustarAnchors(t, Vector2.zero, Vector2.one);
            t.alignment = TextAlignmentOptions.Center;
        }
    }

    void CriarBotaoToggle(GameObject pai, string label,
        Vector2 mn, Vector2 mx, bool ativo)
    {
        var go  = new GameObject($"BtnT_{label}");
        go.transform.SetParent(pai.transform, false);
        var r   = go.AddComponent<RectTransform>();
        r.anchorMin = mn; r.anchorMax = mx;
        r.offsetMin = r.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = ativo ? corAcento : new Color(0.18f, 0.10f, 0.10f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img; btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(() =>
        {
            img.color = corAcento;
            var irmao = go.transform.parent.Find(label == "Pública" ? "BtnT_Privada" : "BtnT_Pública");
            if (irmao != null) irmao.GetComponent<Image>().color = new Color(0.18f, 0.10f, 0.10f);
        });
        var t = Texto("T", Vector2.zero, Vector2.one,
            label, 12f, FontStyles.Bold, Color.white);
        t.transform.SetParent(go.transform, false);
        AjustarAnchors(t, Vector2.zero, Vector2.one);
        t.alignment = TextAlignmentOptions.Center;
    }

    Button CriarBotao(string label, Vector2 mn, Vector2 mx, Color cor, System.Action acao)
    {
        var go  = new GameObject($"Btn_{label}");
        go.transform.SetParent(canvasGO.transform, false);
        var r   = go.AddComponent<RectTransform>();
        r.anchorMin = mn; r.anchorMax = mx;
        r.offsetMin = r.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>(); img.color = cor;
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
