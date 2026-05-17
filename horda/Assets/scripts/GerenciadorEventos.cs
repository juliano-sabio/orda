using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum TipoEvento
{
    MatarInimigos,
    NaoLevarDano,
    Sobreviver,
    ColetarXP,
    UsarUltimate,
}

[Serializable]
public class EventoAleatorio
{
    public string nome = "Evento";
    [TextArea] public string descricao = "Descrição do evento";
    public TipoEvento tipo = TipoEvento.MatarInimigos;
    public float duracao = 30f;
    public int quantidade = 5;
    public string recompensaDescricao = "+15% de vida recuperada!";
}

public class GerenciadorEventos : MonoBehaviour
{
    public static GerenciadorEventos Instance { get; private set; }

    [Header("Intervalos entre Eventos (segundos)")]
    public float intervaloMin = 40f;
    public float intervaloMax = 90f;

    [Header("Eventos Disponíveis")]
    public List<EventoAleatorio> eventos = new List<EventoAleatorio>();

    // Estado do evento
    private bool eventoAtivo;
    private EventoAleatorio eventoAtual;
    private float timerProximoEvento;
    private float timerContagem;
    private int progresso;
    private float xpAcumulada;

    // Referências
    private PlayerStats playerStats;
    private UIManager uiManager;

    // UI criada em runtime
    private GameObject painelEvento;
    private TextMeshProUGUI textoNome;
    private TextMeshProUGUI textoDesc;
    private TextMeshProUGUI textoTimer;
    private TextMeshProUGUI textoProgresso;
    private RectTransform barraFill;
    private Image barraFillImg;

    void Reset()
    {
        PopularEventosPadrao();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (eventos == null || eventos.Count == 0)
            PopularEventosPadrao();
    }

    void Start()
    {
        playerStats = FindObjectOfType<PlayerStats>();
        uiManager = FindObjectOfType<UIManager>();

        CriarPainelUI();
        AgendarProximoEvento();

        InimigoController.OnInimigoDerrotado += OnInimigoDerrotado;
        PlayerStats.OnDanoRecebido += OnDanoRecebido;
        PlayerStats.OnXPColetado += OnXPColetado;
        PlayerStats.OnUltimateAtivada += OnUltimateAtivada;
    }

    void OnDestroy()
    {
        InimigoController.OnInimigoDerrotado -= OnInimigoDerrotado;
        PlayerStats.OnDanoRecebido -= OnDanoRecebido;
        PlayerStats.OnXPColetado -= OnXPColetado;
        PlayerStats.OnUltimateAtivada -= OnUltimateAtivada;
    }

    void Update()
    {
        if (!eventoAtivo)
        {
            timerProximoEvento -= Time.deltaTime;
            if (timerProximoEvento <= 0f)
                TentarIniciarEvento();
            return;
        }

        timerContagem -= Time.deltaTime;
        AtualizarUI();

        if (timerContagem <= 0f)
            EncerrarEvento(eventoAtual.tipo == TipoEvento.Sobreviver);
    }

    // ──────────────────────────────────────────────────────────
    // Lógica do evento

    void TentarIniciarEvento()
    {
        if (eventos == null || eventos.Count == 0) return;

        if (painelEvento == null)
            CriarPainelUI();

        eventoAtual = eventos[UnityEngine.Random.Range(0, eventos.Count)];
        eventoAtivo = true;
        timerContagem = eventoAtual.duracao;
        progresso = 0;
        xpAcumulada = 0f;

        MostrarPainel(true);
        uiManager?.ShowSkillAcquired("⚡ EVENTO!", eventoAtual.nome);
    }

    void EncerrarEvento(bool sucesso)
    {
        eventoAtivo = false;

        if (sucesso && playerStats != null)
            playerStats.Heal(playerStats.maxHealth * 0.15f);

        StartCoroutine(MostrarResultado(sucesso));
        AgendarProximoEvento();
    }

    IEnumerator MostrarResultado(bool sucesso)
    {
        // Painel permanece visível mostrando o resultado
        painelEvento?.SetActive(true);
        if (textoNome != null) { textoNome.text = sucesso ? "✔ SUCESSO!" : "✘ FALHOU!"; textoNome.color = sucesso ? new Color(0.2f, 1f, 0.3f) : new Color(1f, 0.3f, 0.3f); }
        if (textoDesc != null) textoDesc.text = sucesso ? eventoAtual.recompensaDescricao : "Evento não completado.";
        if (textoTimer != null) { textoTimer.text = "0s"; textoTimer.color = Color.white; }
        if (textoProgresso != null) textoProgresso.text = "";
        if (barraFill != null) barraFill.anchorMax = new Vector2(sucesso ? 1f : 0f, 1f);
        if (barraFillImg != null) barraFillImg.color = sucesso ? new Color(0.2f, 0.9f, 0.3f) : new Color(0.9f, 0.2f, 0.2f);

        yield return new WaitForSeconds(3f);
        MostrarPainel(false);
    }

    void AgendarProximoEvento()
    {
        timerProximoEvento = UnityEngine.Random.Range(intervaloMin, intervaloMax);
    }

    void MostrarPainel(bool mostrar)
    {
        painelEvento?.SetActive(mostrar);

        if (mostrar && textoNome != null)
        {
            textoNome.text = eventoAtual.nome;
            textoNome.color = Color.yellow;
            if (textoDesc != null) textoDesc.text = eventoAtual.descricao;
            if (barraFillImg != null) barraFillImg.color = new Color(0.2f, 0.8f, 0.3f);
        }
    }

    void AtualizarUI()
    {
        float pct = Mathf.Clamp01(timerContagem / eventoAtual.duracao);

        if (barraFill != null)
            barraFill.anchorMax = new Vector2(pct, 1f);

        if (textoTimer != null)
            textoTimer.text = Mathf.CeilToInt(timerContagem) + "s";

        if (textoProgresso != null)
        {
            if (eventoAtual.quantidade > 0)
                textoProgresso.text = $"{progresso}/{eventoAtual.quantidade}";
            else
                textoProgresso.text = "";
        }

        // Timer fica vermelho nos últimos 5 segundos
        if (textoTimer != null)
            textoTimer.color = timerContagem <= 5f ? new Color(1f, 0.3f, 0.3f) : Color.white;
    }

    // ──────────────────────────────────────────────────────────
    // Callbacks de eventos estáticos

    void OnInimigoDerrotado()
    {
        if (!eventoAtivo || eventoAtual.tipo != TipoEvento.MatarInimigos) return;
        progresso++;
        if (progresso >= eventoAtual.quantidade)
            EncerrarEvento(true);
    }

    void OnDanoRecebido()
    {
        if (!eventoAtivo || eventoAtual.tipo != TipoEvento.NaoLevarDano) return;
        EncerrarEvento(false);
    }

    void OnXPColetado(float quantidade)
    {
        if (!eventoAtivo || eventoAtual.tipo != TipoEvento.ColetarXP) return;
        xpAcumulada += quantidade;
        progresso = Mathf.FloorToInt(xpAcumulada);
        if (xpAcumulada >= eventoAtual.quantidade)
            EncerrarEvento(true);
    }

    void OnUltimateAtivada()
    {
        if (!eventoAtivo || eventoAtual.tipo != TipoEvento.UsarUltimate) return;
        progresso++;
        if (progresso >= eventoAtual.quantidade)
            EncerrarEvento(true);
    }

    // ──────────────────────────────────────────────────────────
    // Eventos padrão

    [ContextMenu("Resetar Eventos Padrão")]
    void PopularEventosPadrao()
    {
        eventos = new List<EventoAleatorio>
        {
            new EventoAleatorio
            {
                nome = "Caçada",
                descricao = "Derrote 8 inimigos antes do tempo acabar!",
                tipo = TipoEvento.MatarInimigos,
                duracao = 20f,
                quantidade = 8,
                recompensaDescricao = "+15% de vida recuperada!"
            },
            new EventoAleatorio
            {
                nome = "Intocável",
                descricao = "Não leve nenhum dano por 20 segundos!",
                tipo = TipoEvento.NaoLevarDano,
                duracao = 20f,
                quantidade = 0,
                recompensaDescricao = "+15% de vida recuperada!"
            },
            new EventoAleatorio
            {
                nome = "Sobrevivente",
                descricao = "Sobreviva pelos próximos 20 segundos!",
                tipo = TipoEvento.Sobreviver,
                duracao = 20f,
                quantidade = 0,
                recompensaDescricao = "+15% de vida recuperada!"
            },
            new EventoAleatorio
            {
                nome = "Exterminador",
                descricao = "Derrote 10 inimigos antes do tempo acabar!",
                tipo = TipoEvento.MatarInimigos,
                duracao = 20f,
                quantidade = 10,
                recompensaDescricao = "+15% de vida recuperada!"
            },
            new EventoAleatorio
            {
                nome = "Esquiva Perfeita",
                descricao = "Fique 20 segundos sem levar dano!",
                tipo = TipoEvento.NaoLevarDano,
                duracao = 20f,
                quantidade = 0,
                recompensaDescricao = "+15% de vida recuperada!"
            },
            new EventoAleatorio
            {
                nome = "Coletor de Almas",
                descricao = "Colete 100 de XP em 20 segundos!",
                tipo = TipoEvento.ColetarXP,
                duracao = 20f,
                quantidade = 100,
                recompensaDescricao = "+15% de vida recuperada!"
            },
            new EventoAleatorio
            {
                nome = "Energia Máxima",
                descricao = "Colete 200 de XP em 20 segundos!",
                tipo = TipoEvento.ColetarXP,
                duracao = 20f,
                quantidade = 200,
                recompensaDescricao = "+15% de vida recuperada!"
            },
            new EventoAleatorio
            {
                nome = "Poder Supremo",
                descricao = "Use sua Ultimate 1 vez em 20 segundos!",
                tipo = TipoEvento.UsarUltimate,
                duracao = 20f,
                quantidade = 1,
                recompensaDescricao = "+15% de vida recuperada!"
            },
            new EventoAleatorio
            {
                nome = "Força Total",
                descricao = "Use sua Ultimate 2 vezes em 20 segundos!",
                tipo = TipoEvento.UsarUltimate,
                duracao = 20f,
                quantidade = 2,
                recompensaDescricao = "+15% de vida recuperada!"
            },
            new EventoAleatorio
            {
                nome = "Pilha de Corpos",
                descricao = "Derrote 5 inimigos antes do tempo acabar!",
                tipo = TipoEvento.MatarInimigos,
                duracao = 20f,
                quantidade = 5,
                recompensaDescricao = "+15% de vida recuperada!"
            },
            new EventoAleatorio
            {
                nome = "Chacina",
                descricao = "Derrote 12 inimigos antes do tempo acabar!",
                tipo = TipoEvento.MatarInimigos,
                duracao = 20f,
                quantidade = 12,
                recompensaDescricao = "+15% de vida recuperada!"
            },
        };
    }

    // ──────────────────────────────────────────────────────────
    // Criação do painel de UI em runtime

    void CriarPainelUI()
    {
        // Canvas dedicado — independente de qualquer outro canvas da cena
        GameObject canvasGO = new GameObject("EventoCanvas");
        DontDestroyOnLoad(canvasGO);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99; // sempre na frente

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Painel raiz
        painelEvento = new GameObject("EventoPainel");
        painelEvento.transform.SetParent(canvasGO.transform, false);

        RectTransform rectPainel = painelEvento.AddComponent<RectTransform>();
        rectPainel.anchorMin = new Vector2(1f, 1f);
        rectPainel.anchorMax = new Vector2(1f, 1f);
        rectPainel.pivot    = new Vector2(1f, 1f);
        rectPainel.anchoredPosition = new Vector2(-10f, -10f);
        rectPainel.sizeDelta = new Vector2(330f, 110f);

        Image fundo = painelEvento.AddComponent<Image>();
        fundo.color = new Color(0.04f, 0.04f, 0.12f, 0.92f);

        // Borda colorida (painel interno levemente menor)
        GameObject borda = new GameObject("Borda");
        borda.transform.SetParent(painelEvento.transform, false);
        RectTransform rectBorda = borda.AddComponent<RectTransform>();
        rectBorda.anchorMin = Vector2.zero;
        rectBorda.anchorMax = Vector2.one;
        rectBorda.offsetMin = new Vector2(2f, 2f);
        rectBorda.offsetMax = new Vector2(-2f, -2f);
        Image imgBorda = borda.AddComponent<Image>();
        imgBorda.color = new Color(0.3f, 0.5f, 1f, 0.4f);

        // Nome do evento
        textoNome = CriarTMP(painelEvento, "NomeEvento",
            anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
            pivot: new Vector2(0.5f, 1f),
            anchoredPos: new Vector2(0f, -6f), size: new Vector2(0f, 26f),
            fontSize: 16f, style: FontStyles.Bold,
            color: Color.yellow, align: TextAlignmentOptions.Center);

        // Descrição
        textoDesc = CriarTMP(painelEvento, "DescEvento",
            anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
            pivot: new Vector2(0.5f, 1f),
            anchoredPos: new Vector2(0f, -34f), size: new Vector2(-16f, 36f),
            fontSize: 11f, style: FontStyles.Normal,
            color: new Color(0.85f, 0.85f, 0.85f), align: TextAlignmentOptions.Center);

        // Timer e progresso (linha inferior)
        textoTimer = CriarTMP(painelEvento, "TimerEvento",
            anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(0.4f, 0f),
            pivot: new Vector2(0f, 0f),
            anchoredPos: new Vector2(10f, 22f), size: new Vector2(0f, 22f),
            fontSize: 14f, style: FontStyles.Bold,
            color: Color.white, align: TextAlignmentOptions.Left);

        textoProgresso = CriarTMP(painelEvento, "ProgressoEvento",
            anchorMin: new Vector2(0.6f, 0f), anchorMax: new Vector2(1f, 0f),
            pivot: new Vector2(1f, 0f),
            anchoredPos: new Vector2(-10f, 22f), size: new Vector2(0f, 22f),
            fontSize: 14f, style: FontStyles.Bold,
            color: Color.white, align: TextAlignmentOptions.Right);

        // Barra de progresso de tempo
        GameObject barraBG = new GameObject("BarraBG");
        barraBG.transform.SetParent(painelEvento.transform, false);
        RectTransform rectBG = barraBG.AddComponent<RectTransform>();
        rectBG.anchorMin = new Vector2(0f, 0f);
        rectBG.anchorMax = new Vector2(1f, 0f);
        rectBG.pivot = new Vector2(0.5f, 0f);
        rectBG.anchoredPosition = new Vector2(0f, 6f);
        rectBG.sizeDelta = new Vector2(-16f, 10f);
        Image imgBG = barraBG.AddComponent<Image>();
        imgBG.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        // Fill da barra
        GameObject barraFillGO = new GameObject("BarraFill");
        barraFillGO.transform.SetParent(barraBG.transform, false);
        barraFill = barraFillGO.AddComponent<RectTransform>();
        barraFill.anchorMin = new Vector2(0f, 0f);
        barraFill.anchorMax = new Vector2(1f, 1f);
        barraFill.sizeDelta = Vector2.zero;
        barraFill.offsetMin = Vector2.zero;
        barraFill.offsetMax = Vector2.zero;
        barraFillImg = barraFillGO.AddComponent<Image>();
        barraFillImg.color = new Color(0.2f, 0.8f, 0.3f, 1f);

        painelEvento.SetActive(false);
    }

    TextMeshProUGUI CriarTMP(
        GameObject parent, string nome,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 size,
        float fontSize, FontStyles style, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(parent.transform, false);

        RectTransform r = go.AddComponent<RectTransform>();
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.pivot = pivot;
        r.anchoredPosition = anchoredPos;
        r.sizeDelta = size;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = align;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Truncate;
        return tmp;
    }
}
