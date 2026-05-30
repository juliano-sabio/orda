using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using TMPro;

public enum TipoEvento
{
    MatarInimigos,
    NaoLevarDano,
    Sobreviver,
    ColetarXP,
    UsarUltimate,
    ColetarEspirito,
    EliminarSlimeColorida,
    Ceifador,
    SlimePercurso,
    ZonaEliminacao,
    Colapso,
    TempestadeEletrica,
    Portal,
    Danca,
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
    [Tooltip("Quantos espíritos nascem no mapa (0 = usa quantidade)")]
    public int quantidadeSpawn = 0;
}

public class GerenciadorEventos : MonoBehaviour
{
    public static GerenciadorEventos Instance { get; private set; }

    [Header("Sincronização com TimerManager")]
    public float delayInicial     = 30f;
    public float intervaloEventos = 60f;

    [Header("Debug")]
    [Tooltip("-1 = aleatório | 0..N = força índice da lista eventos")]
    public int debugForcarEvento = -1;

    [Header("Eventos Disponíveis")]
    public List<EventoAleatorio> eventos = new List<EventoAleatorio>();

    [Header("Visual — Painel")]
    public Vector2 tamanhoDoPanel    = new Vector2(330f, 110f);
    public Vector2 posicaoVisivel    = new Vector2(-10f, -10f);
    public Color   corFundo          = new Color(0.04f, 0.04f, 0.12f, 0.92f);
    public Color   corBorda          = new Color(0.3f, 0.5f, 1f, 0.4f);

    [Header("Visual — Textos")]
    public float   tamanhoFonteNome  = 16f;
    public Color   corNome           = Color.yellow;
    public float   tamanhoFonteDesc  = 11f;
    public Color   corDesc           = new Color(0.85f, 0.85f, 0.85f);
    public float   tamanhoFonteTimer = 14f;

    [Header("Espíritos do Evento")]
    public GameObject espiritoEventoPrefab;
    public Tilemap    terrenoBase;

    [Header("Slime Colorida")]
    public GameObject slimeColoridaPrefab;

    [Header("Ceifador")]
    public GameObject ceifadorPrefab;

    [Header("Slime Percurso")]
    public GameObject slimePercursoPrefab;
    public GameObject[] prefabsInimigosPercurso;
    public int          qtdInimigosPercurso = 15;
    public float        insetCanto          = 6f;
    public float      distMinEspiritos    = 18f;
    public LayerMask  camadasObstaculo;
    public float      raioChecagemSpawn   = 0.5f;

    [Header("Zona de Eliminação")]
    public GameObject[] prefabsInimigosZona;
    public float        raioZona = 8f;

    [Header("Dança")]
    public int   dancaQuantidade = 8;
    public float dancaTempoZona  = 40f;
    public float dancaRaioZona   = 2.5f;

    [Header("Portal")]
    public float        portalRaioFechar     = 2.5f;
    public float        portalTempoFechar    = 3.5f;
    public float        portalIntervaloSpawn = 5f;
    public GameObject[] prefabsInimigosPortal;

    [Header("Tempestade Elétrica")]
    public float tempestadeDanoJogador = 20f;
    public float tempestadeDanoInimigo = 50f;
    public float tempestadeRaioImpacto = 3f;

    [Header("Drops Globais de Inimigos")]
    public List<DropEntry> dropsGlobais = new List<DropEntry>();

    [Header("Visual — Barra de Progresso")]
    public Color   corBarraAtiva     = new Color(0.2f, 0.8f, 0.3f);
    public Color   corBarraSucesso   = new Color(0.2f, 0.9f, 0.3f);
    public Color   corBarraFalha     = new Color(0.9f, 0.2f, 0.2f);

    // Estado do evento
    private readonly List<GameObject> espiritosMapa = new List<GameObject>();
    private GameObject    slimeColoridaAtiva;
    private IndicadorSlime indicadorSlime;
    private readonly List<GameObject> ceifadoresMapa = new List<GameObject>();
    private BordaSangueEvento bordaSangue;

    private ZonaEliminacaoEvento    zonaEliminacao;
    private SlimePercursoEvento     slimePercurso;
    private EventoColapso           colapsoAtivo;
    private TempestadeEletricaEvento tempestadeAtiva;
    private PortalEvento             portalAtivo;
    private DancaEvento              dancaAtiva;
    private readonly List<GameObject> inimigosPercurso = new List<GameObject>();
    private Coroutine corSpawnPercurso;
    private Tilemap[] tilemapsObstaculo;

    private bool eventoAtivo;
    private bool primeiroEventoDisparado;
    private EventoAleatorio eventoAtual;
    private float proximoEventoTempo;   // tempo absoluto do relógio para o próximo evento
    private float timerContagem;
    private int progresso;
    private float xpAcumulada;

    // Referências
    private PlayerStats playerStats;
    private UIManager uiManager;
    private TimerManager timerManager;

    // UI criada em runtime
    private GameObject painelEvento;
    private RectTransform painelRT;
    private CanvasGroup painelCG;
    private TextMeshProUGUI textoNome;
    private TextMeshProUGUI textoDesc;
    private TextMeshProUGUI textoTimer;
    private TextMeshProUGUI textoProgresso;
    private RectTransform barraFill;
    private Image barraFillImg;

    private Vector2 POS_VISIVEL  => posicaoVisivel;
    private static readonly Vector2 POS_ESCONDIDO = new Vector2(360f, -10f);

    [UnityEngine.ContextMenu("Adicionar Tempestade Elétrica")]
    void AdicionarTempestadeNoInspector()
    {
        if (eventos == null) eventos = new System.Collections.Generic.List<EventoAleatorio>();
        if (eventos.Exists(e => e.tipo == TipoEvento.TempestadeEletrica)) return;
        eventos.Insert(0, new EventoAleatorio
        {
            nome                = "⚡ Tempestade Elétrica",
            descricao           = "Raios caem pelo mapa! Fique de olho nos círculos de aviso e sobreviva!",
            tipo                = TipoEvento.TempestadeEletrica,
            duracao             = 240f,
            quantidade          = 0,
            recompensaDescricao = "+15% de vida recuperada!"
        });
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        playerStats   = FindObjectOfType<PlayerStats>();
        uiManager     = FindObjectOfType<UIManager>();
        timerManager  = FindObjectOfType<TimerManager>();

        CriarPainelUI();
        proximoEventoTempo = delayInicial; // não usa TempoDecorrido() aqui — TimerManager ainda não inicializou

        PopularEventosPadrao();

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
        float tempoDecorrido = TempoDecorrido();

        if (!eventoAtivo)
        {
            if (tempoDecorrido >= proximoEventoTempo)
                TentarIniciarEvento();
            return;
        }

        timerContagem -= Time.deltaTime;
        AtualizarUI();

        if (eventoAtual.tipo == TipoEvento.EliminarSlimeColorida
            && slimeColoridaAtiva != null
            && indicadorSlime == null
            && (eventoAtual.duracao - timerContagem) >= 180f)
        {
            var go = new GameObject("IndicadorSlime");
            indicadorSlime = go.AddComponent<IndicadorSlime>();
            indicadorSlime.alvo = slimeColoridaAtiva.transform;
        }

        if (eventoAtual.tipo == TipoEvento.SlimePercurso
            && slimePercurso != null
            && indicadorSlime == null
            && (eventoAtual.duracao - timerContagem) >= 15f)
        {
            var go = new GameObject("IndicadorSlimePercurso");
            indicadorSlime = go.AddComponent<IndicadorSlime>();
            indicadorSlime.alvo     = slimePercurso.transform;
            indicadorSlime.corSeta  = new Color(0.2f, 1f, 0.45f);
        }

        if (eventoAtual.tipo == TipoEvento.ZonaEliminacao
            && zonaEliminacao != null
            && indicadorSlime == null
            && (eventoAtual.duracao - timerContagem) >= 120f)
        {
            var go = new GameObject("IndicadorZona");
            indicadorSlime = go.AddComponent<IndicadorSlime>();
            indicadorSlime.alvo    = zonaEliminacao.transform;
            indicadorSlime.corSeta = new Color(0.25f, 0.85f, 1f);
        }

        if (timerContagem <= 0f)
            EncerrarEvento(eventoAtual.tipo == TipoEvento.Sobreviver
                        || eventoAtual.tipo == TipoEvento.Ceifador
                        || eventoAtual.tipo == TipoEvento.TempestadeEletrica
                        || eventoAtual.tipo == TipoEvento.Danca
                        || (eventoAtual.tipo == TipoEvento.SlimePercurso && slimePercurso != null && slimePercurso.Chegou)
                        || (eventoAtual.tipo == TipoEvento.Colapso && progresso >= eventoAtual.quantidade));
    }

    // Tempo decorrido desde o início — usa TimerManager se disponível
    float TempoDecorrido()
    {
        if (timerManager != null)
            return timerManager.levelDuration - timerManager.currentTime;
        return Time.timeSinceLevelLoad;
    }

    // ──────────────────────────────────────────────────────────
    // Lógica do evento

void TentarIniciarEvento()
{
    if (eventos == null || eventos.Count == 0) return;

    if (painelEvento == null)
        CriarPainelUI();

    int idx;
    if (debugForcarEvento >= 0 && debugForcarEvento < eventos.Count)
        idx = debugForcarEvento;
    else if (!primeiroEventoDisparado)
    {
        idx = eventos.FindIndex(e => e.tipo == TipoEvento.Danca);
        if (idx < 0) idx = 0;
    }
    else
        idx = UnityEngine.Random.Range(0, eventos.Count);
    primeiroEventoDisparado = true;
    eventoAtual = eventos[idx];
    eventoAtivo = true;
    timerContagem = eventoAtual.duracao;
    progresso = 0;
    xpAcumulada = 0f;

    Debug.Log($"[GerenciadorEventos] Iniciando evento: {eventoAtual.nome} (tipo={eventoAtual.tipo})");

    MostrarPainel(true);
    uiManager?.ShowSkillAcquired("⚡ EVENTO!", eventoAtual.nome);

    if (eventoAtual.tipo == TipoEvento.ColetarEspirito)
    {
        int spawn = eventoAtual.quantidadeSpawn > 0 ? eventoAtual.quantidadeSpawn : eventoAtual.quantidade;
        SpawnEspiritos(spawn);
    }
    else if (eventoAtual.tipo == TipoEvento.EliminarSlimeColorida)
    {
        SpawnSlimeColorida();
    }
    else if (eventoAtual.tipo == TipoEvento.Ceifador)
    {
        int qtd = eventoAtual.quantidade > 0 ? eventoAtual.quantidade : 6;
        SpawnCeifadores(qtd);
    }
    else if (eventoAtual.tipo == TipoEvento.SlimePercurso)
    {
        SpawnSlimePercurso();
    }
    else if (eventoAtual.tipo == TipoEvento.ZonaEliminacao)
    {
        SpawnZonaEliminacao();
    }
    else if (eventoAtual.tipo == TipoEvento.Colapso)
    {
        IniciarColapso();
    }
    else if (eventoAtual.tipo == TipoEvento.TempestadeEletrica)
    {
        IniciarTempestadeEletrica();
    }
    else if (eventoAtual.tipo == TipoEvento.Portal)
    {
        IniciarPortal();
    }
    else if (eventoAtual.tipo == TipoEvento.Danca)
    {
        IniciarDanca();
    }
}

    void EncerrarEvento(bool sucesso)
    {
        eventoAtivo = false;
        LimparEspiritos();

        if (slimeColoridaAtiva != null)
        {
            Destroy(slimeColoridaAtiva);
            slimeColoridaAtiva = null;
        }
        if (indicadorSlime != null)
        {
            Destroy(indicadorSlime.gameObject);
            indicadorSlime = null;
        }

        LimparCeifadores();
        LimparSlimePercurso();
        LimparZonaEliminacao();
        LimparColapso();
        LimparTempestadeEletrica();
        LimparPortal();
        if (dancaAtiva != null) dancaAtiva.Encerrar();
        LimparDanca();

        if (sucesso && playerStats != null)
        {
            float pctCura = eventoAtual.tipo == TipoEvento.SlimePercurso ? 0.40f : 0.15f;
            playerStats.Heal(playerStats.maxHealth * pctCura);
        }

        StartCoroutine(MostrarResultado(sucesso));
        AgendarProximoEvento();
    }

    IEnumerator MostrarResultado(bool sucesso)
    {
        // Painel já está visível — só atualiza o conteúdo
        if (textoNome   != null) { textoNome.text = sucesso ? "✔ SUCESSO!" : "✘ FALHOU!"; textoNome.color = sucesso ? new Color(0.2f, 1f, 0.3f) : new Color(1f, 0.3f, 0.3f); }
        if (textoDesc   != null) textoDesc.text = sucesso ? eventoAtual.recompensaDescricao : "Evento não completado.";
        if (textoTimer  != null) { textoTimer.text = ""; textoTimer.color = Color.white; }
        if (textoProgresso != null) textoProgresso.text = "";
        if (barraFill   != null) barraFill.anchorMax = new Vector2(sucesso ? 1f : 0f, 1f);
        if (barraFillImg != null) barraFillImg.color = sucesso ? corBarraSucesso : corBarraFalha;

        yield return new WaitForSeconds(3f);
        MostrarPainel(false);
    }

    void AgendarProximoEvento()
    {
        float agora = TempoDecorrido();
        // Primeira chamada (Start): espera só delayInicial
        // Após um evento encerrar: espera intervaloEventos a partir de agora
        proximoEventoTempo = agora < delayInicial
            ? delayInicial
            : agora + intervaloEventos;
    }

    void MostrarPainel(bool mostrar)
    {
        if (mostrar)
        {
            if (textoNome  != null) { textoNome.text = eventoAtual.nome; textoNome.color = Color.yellow; }
            if (textoDesc  != null) textoDesc.text = eventoAtual.descricao;
            if (barraFillImg != null) barraFillImg.color = corBarraAtiva; //new Color(0.2f, 0.8f, 0.3f);
            StopCoroutine("AnimarSaida");
            StartCoroutine("AnimarEntrada");
        }
        else
        {
            StopCoroutine("AnimarEntrada");
            StartCoroutine("AnimarSaida");
        }
    }

    IEnumerator AnimarEntrada()
    {
        painelEvento.SetActive(true);
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(t + Time.unscaledDeltaTime / 0.35f, 1f);
            float ease = EaseOutBack(t);
            painelRT.anchoredPosition = Vector2.Lerp(POS_ESCONDIDO, POS_VISIVEL, ease);
            painelCG.alpha = Mathf.Lerp(0f, 1f, t * 2f);
            yield return null;
        }
        painelRT.anchoredPosition = POS_VISIVEL;
        painelCG.alpha = 1f;
    }

    IEnumerator AnimarSaida()
    {
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(t + Time.unscaledDeltaTime / 0.25f, 1f);
            float ease = t * t;
            painelRT.anchoredPosition = Vector2.Lerp(POS_VISIVEL, POS_ESCONDIDO, ease);
            painelCG.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }
        painelEvento.SetActive(false);
        painelRT.anchoredPosition = POS_ESCONDIDO;
        painelCG.alpha = 0f;
    }

    float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
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
        if (!eventoAtivo) return;
        if (eventoAtual.tipo == TipoEvento.MatarInimigos || eventoAtual.tipo == TipoEvento.Colapso)
        {
            progresso++;
            if (progresso >= eventoAtual.quantidade)
                EncerrarEvento(true);
        }
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
    // Espíritos

    Bounds CalcularBoundsMapa()
    {
        var tilemaps = FindObjectsOfType<Tilemap>();
        if (tilemaps == null || tilemaps.Length == 0)
            return new Bounds(Vector3.zero, new Vector3(60f, 40f, 0f));

        bool primeiro = true;
        Bounds bounds = new Bounds();
        foreach (var tm in tilemaps)
        {
            if (!tm.gameObject.activeInHierarchy) continue;
            tm.CompressBounds();
            Bounds b = new Bounds();
            b.SetMinMax(
                tm.transform.TransformPoint(tm.localBounds.min),
                tm.transform.TransformPoint(tm.localBounds.max)
            );
            if (primeiro) { bounds = b; primeiro = false; }
            else bounds.Encapsulate(b);
        }
        return bounds;
    }

    bool PosicaoTemObstaculo(Vector2 pos)
    {
        // Cacheia na primeira chamada: layer 3 OU qualquer tilemap com TilemapCollider2D
        // Exclui o terrenoBase para não tratar o chão como obstáculo
        if (tilemapsObstaculo == null || tilemapsObstaculo.Length == 0)
        {
            var todos = FindObjectsOfType<Tilemap>();
            var lista = new List<Tilemap>();
            foreach (var tm in todos)
            {
                if (terrenoBase != null && tm == terrenoBase) continue;
                if (tm.gameObject.layer == 3 || tm.GetComponent<TilemapCollider2D>() != null)
                    lista.Add(tm);
            }
            tilemapsObstaculo = lista.ToArray();
        }

        // Margem de 2 células ao redor para cobrir bordas visuais
        foreach (var tm in tilemapsObstaculo)
        {
            Vector3Int celula = tm.WorldToCell(pos);
            for (int dx = -2; dx <= 2; dx++)
                for (int dy = -2; dy <= 2; dy++)
                    if (tm.GetTile(celula + new Vector3Int(dx, dy, 0)) != null)
                        return true;
        }

        // Checagem física como fallback
        return Physics2D.OverlapBox(pos, Vector2.one * 1.5f, 0f, camadasObstaculo) != null;
    }

    // Verifica se pos está completamente dentro do terreno_base (checa centro + 4 pontos cardeais)
    bool PosicaoNoTerreno(Vector2 pos)
    {
        if (terrenoBase == null) return true;
        const float r = 0.8f;
        Vector2[] pts = { pos, pos + Vector2.right * r, pos - Vector2.right * r,
                               pos + Vector2.up   * r, pos - Vector2.up   * r };
        foreach (var p in pts)
            if (terrenoBase.GetTile(terrenoBase.WorldToCell(p)) == null) return false;
        return true;
    }

    // Método público para EspiritoEvento verificar durante o drift
    public bool PosicaoValida(Vector2 pos)
        => PosicaoNoTerreno(pos) && !PosicaoTemObstaculo(pos);

    void SpawnEspiritos(int quantidade)
    {
        if (espiritoEventoPrefab == null) return;

        Bounds mapa;
        if (terrenoBase != null)
        {
            terrenoBase.CompressBounds();
            mapa = new Bounds();
            mapa.SetMinMax(
                terrenoBase.transform.TransformPoint(terrenoBase.localBounds.min),
                terrenoBase.transform.TransformPoint(terrenoBase.localBounds.max)
            );
        }
        else
        {
            mapa = CalcularBoundsMapa();
        }

        var   posicoes      = new List<Vector2>();
        const int tentativasMax = 60;
        float distAtual     = distMinEspiritos;

        for (int i = 0; i < quantidade; i++)
        {
            bool encontrou = false;

            for (int t = 0; t < tentativasMax; t++)
            {
                Vector2 candidato = new Vector2(
                    UnityEngine.Random.Range(mapa.min.x, mapa.max.x),
                    UnityEngine.Random.Range(mapa.min.y, mapa.max.y)
                );

                if (!PosicaoValida(candidato)) continue;

                bool longe = true;
                foreach (var p in posicoes)
                    if (Vector2.Distance(candidato, p) < distAtual)
                    { longe = false; break; }

                if (!longe) continue;

                posicoes.Add(candidato);
                var e = Instantiate(espiritoEventoPrefab, new Vector3(candidato.x, candidato.y, 0f), Quaternion.identity);
                espiritosMapa.Add(e);
                encontrou = true;
                break;
            }

            if (!encontrou && distAtual > 3f)
            {
                distAtual = Mathf.Max(distAtual - 2f, 3f);
                i--;
            }
        }
    }

    void SpawnSlimeColorida()
    {
        if (slimeColoridaPrefab == null) return;

        Bounds mapa;
        if (terrenoBase != null)
        {
            terrenoBase.CompressBounds();
            mapa = new Bounds();
            mapa.SetMinMax(
                terrenoBase.transform.TransformPoint(terrenoBase.localBounds.min),
                terrenoBase.transform.TransformPoint(terrenoBase.localBounds.max)
            );
        }
        else
        {
            mapa = CalcularBoundsMapa();
        }

        Vector2 posPlayer = playerStats != null
            ? (Vector2)playerStats.transform.position
            : Vector2.zero;

        for (int t = 0; t < 80; t++)
        {
            Vector2 candidato = new Vector2(
                UnityEngine.Random.Range(mapa.min.x, mapa.max.x),
                UnityEngine.Random.Range(mapa.min.y, mapa.max.y)
            );
            if (!PosicaoValida(candidato)) continue;
            if (Vector2.Distance(candidato, posPlayer) < 8f) continue;

            slimeColoridaAtiva = Instantiate(
                slimeColoridaPrefab,
                new Vector3(candidato.x, candidato.y, 0f),
                Quaternion.identity
            );
            return;
        }
    }

void SpawnCeifadores(int quantidade)
{
    if (ceifadorPrefab == null) { Debug.LogError("[Ceifador] ceifadorPrefab é null!"); return; }

    Bounds mapa;
    if (terrenoBase != null)
    {
        terrenoBase.CompressBounds();
        mapa = new Bounds();
        mapa.SetMinMax(
            terrenoBase.transform.TransformPoint(terrenoBase.localBounds.min),
            terrenoBase.transform.TransformPoint(terrenoBase.localBounds.max)
        );
    }
    else
    {
        mapa = CalcularBoundsMapa();
    }

    Debug.Log($"[Ceifador] Tentando spawnar {quantidade} ceifadores. Bounds: {mapa.min} a {mapa.max}");

    Vector2 posPlayer = playerStats != null
        ? (Vector2)playerStats.transform.position
        : Vector2.zero;

    var posicoes = new List<Vector2>();
    int spawned = 0;

    for (int i = 0; i < quantidade; i++)
    {
        bool encontrou = false;
        for (int t = 0; t < 80; t++)
        {
            Vector2 candidato = new Vector2(
                UnityEngine.Random.Range(mapa.min.x, mapa.max.x),
                UnityEngine.Random.Range(mapa.min.y, mapa.max.y)
            );
            if (!PosicaoValida(candidato)) continue;
            // raio físico do ceifador: escala 5 × collider 0.4 = 2.0 — verifica obstáculos na área real
            int maskObst = camadasObstaculo != 0 ? (int)camadasObstaculo : (1 << 3);
            if (Physics2D.OverlapCircle(candidato, 2.2f, maskObst)) continue;
            if (Vector2.Distance(candidato, posPlayer) < 8f) continue;

            bool longe = true;
            foreach (var p in posicoes)
                if (Vector2.Distance(candidato, p) < 5f) { longe = false; break; }
            if (!longe) continue;

            posicoes.Add(candidato);
            var go = Instantiate(ceifadorPrefab,
                new Vector3(candidato.x, candidato.y, 0f),
                Quaternion.identity);
            ceifadoresMapa.Add(go);
            spawned++;
            encontrou = true;
            break;
        }
        if (!encontrou) break;
    }
    Debug.Log($"[Ceifador] Spawnou {spawned}/{quantidade} ceifadores.");

    // Borda sangrenta
    if (bordaSangue == null)
    {
        var go = new GameObject("BordaSangue");
        bordaSangue = go.AddComponent<BordaSangueEvento>();
        bordaSangue.Mostrar();
    }
}

void LimparCeifadores()
{
    foreach (var c in ceifadoresMapa)
        if (c != null) Destroy(c);
    ceifadoresMapa.Clear();

    if (bordaSangue != null)
    {
        bordaSangue.Esconder();
        bordaSangue = null;
    }
}

// Busca posição válida partindo do canto em direção ao centro do mapa
Vector2 CantoDentroDoMapa(Vector2 canto, Bounds mapa, float raioMax = 60f)
{
    int maskObst  = camadasObstaculo != 0 ? (int)camadasObstaculo : (1 << 3);
    Vector2 dir   = ((Vector2)mapa.center - canto).normalized;

    for (float d = 0f; d <= raioMax; d += 0.5f)
    {
        Vector2 p = canto + dir * d;
        if (!mapa.Contains(new Vector3(p.x, p.y, 0f)))    continue;
        if (Physics2D.OverlapCircle(p, 2.2f, maskObst))   continue;
        if (!PosicaoValida(p))                             continue;
        return p;
    }

    // Fallback: amostragem aleatória dentro do mapa
    for (int i = 0; i < 300; i++)
    {
        Vector2 p = new Vector2(
            UnityEngine.Random.Range(mapa.min.x + 2f, mapa.max.x - 2f),
            UnityEngine.Random.Range(mapa.min.y + 2f, mapa.max.y - 2f)
        );
        if (Physics2D.OverlapCircle(p, 2.2f, maskObst)) continue;
        if (!PosicaoValida(p))                           continue;
        return p;
    }
    return (Vector2)mapa.center;
}

void SpawnSlimePercurso()
{
    if (slimePercursoPrefab == null) { Debug.LogError("[SlimePercurso] slimePercursoPrefab é null!"); return; }

    Bounds mapa;
    if (terrenoBase != null)
    {
        terrenoBase.CompressBounds();
        mapa = new Bounds();
        mapa.SetMinMax(
            terrenoBase.transform.TransformPoint(terrenoBase.localBounds.min),
            terrenoBase.transform.TransformPoint(terrenoBase.localBounds.max)
        );
    }
    else
    {
        mapa = CalcularBoundsMapa();
    }

    float ins = insetCanto > 0 ? insetCanto : 6f;

    // Escolhe 2 cantos diagonais aleatoriamente
    Vector2[] cantos = new Vector2[]
    {
        new Vector2(mapa.min.x + ins, mapa.min.y + ins),
        new Vector2(mapa.max.x - ins, mapa.min.y + ins),
        new Vector2(mapa.min.x + ins, mapa.max.y - ins),
        new Vector2(mapa.max.x - ins, mapa.max.y - ins),
    };
    int idxA = UnityEngine.Random.Range(0, 2);           // 0 ou 1
    int idxB = idxA == 0 ? 3 : 2;                        // diagonal oposta

    Vector2 origem  = CantoDentroDoMapa(cantos[idxA], mapa);
    Vector2 destino = CantoDentroDoMapa(cantos[idxB], mapa);

    // Se os dois cantos colapsaram no mesmo ponto, tenta o par alternativo
    if (Vector2.Distance(origem, destino) < 10f)
    {
        int idxA2 = idxA == 0 ? 1 : 0;
        int idxB2 = idxA2 == 0 ? 3 : 2;
        Vector2 alt1 = CantoDentroDoMapa(cantos[idxA2], mapa);
        Vector2 alt2 = CantoDentroDoMapa(cantos[idxB2], mapa);
        if (Vector2.Distance(alt1, alt2) > Vector2.Distance(origem, destino))
        { origem = alt1; destino = alt2; }
        Debug.LogWarning($"[SlimePercurso] Cantos muito próximos, usando alternativa: {origem} → {destino}");
    }

    var go = Instantiate(slimePercursoPrefab, new Vector3(origem.x, origem.y, 0f), Quaternion.identity);
    slimePercurso = go.GetComponent<SlimePercursoEvento>();
    if (slimePercurso == null) { Debug.LogError("[SlimePercurso] prefab sem SlimePercursoEvento!"); Destroy(go); return; }

    slimePercurso.OnChegou += () => { if (this != null && eventoAtivo) EncerrarEvento(true);  };
    slimePercurso.OnMorreu += () => { if (this != null && eventoAtivo) EncerrarEvento(false); };

    slimePercurso.IniciarPercurso(destino);

    // Redireciona FlowField para a slime
    FlowField.AlvoOverride = slimePercurso.transform;

    // Spawn de inimigos em ondas
    if (prefabsInimigosPercurso != null && prefabsInimigosPercurso.Length > 0)
        corSpawnPercurso = StartCoroutine(SpawnInimigosPercurso());

    Debug.Log($"[SlimePercurso] Slime spawnada em {origem} → destino {destino}");
}

IEnumerator SpawnInimigosPercurso()
{
    int spawned = 0;
    while (eventoAtivo && slimePercurso != null && !slimePercurso.Chegou && spawned < qtdInimigosPercurso)
    {
        yield return new WaitForSeconds(4f);
        if (!eventoAtivo || slimePercurso == null) yield break;

        Vector2 posPlayer = playerStats != null ? (Vector2)playerStats.transform.position : Vector2.zero;
        for (int t = 0; t < 30; t++)
        {
            float ang = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float dist = UnityEngine.Random.Range(10f, 20f);
            Vector2 pos = posPlayer + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;
            if (!PosicaoValida(pos)) continue;
            int maskObst = camadasObstaculo != 0 ? (int)camadasObstaculo : (1 << 3);
            if (Physics2D.OverlapCircle(pos, 1f, maskObst)) continue;

            var prefab = prefabsInimigosPercurso[UnityEngine.Random.Range(0, prefabsInimigosPercurso.Length)];
            var go = Instantiate(prefab, new Vector3(pos.x, pos.y, 0f), Quaternion.identity);
            inimigosPercurso.Add(go);
            spawned++;
            break;
        }
    }
}

    // ──────────────────────────────────────────────────────────
    // Colapso

    void IniciarColapso()
    {
        Bounds mapa    = CalcularBoundsMapa();
        Vector2 centro = mapa.center;

        var go = new GameObject("EventoColapso");
        colapsoAtivo = go.AddComponent<EventoColapso>();
        colapsoAtivo.Iniciar(playerStats, centro, eventoAtual.duracao, mapa);
    }

    void LimparColapso()
    {
        if (colapsoAtivo != null)
        {
            Destroy(colapsoAtivo.gameObject);
            colapsoAtivo = null;
        }
    }


void SpawnZonaEliminacao()
{
    Bounds mapa;
    if (terrenoBase != null)
    {
        terrenoBase.CompressBounds();
        mapa = new Bounds();
        mapa.SetMinMax(
            terrenoBase.transform.TransformPoint(terrenoBase.localBounds.min),
            terrenoBase.transform.TransformPoint(terrenoBase.localBounds.max));
    }
    else mapa = CalcularBoundsMapa();

    Vector2 posPlayer = playerStats != null ? (Vector2)playerStats.transform.position : Vector2.zero;
    int maskObst = camadasObstaculo != 0 ? (int)camadasObstaculo : (1 << 3);

    Vector2 centro = Vector2.zero;
    bool encontrou = false;
    for (int t = 0; t < 120; t++)
    {
        Vector2 c = new Vector2(
            UnityEngine.Random.Range(mapa.min.x + raioZona, mapa.max.x - raioZona),
            UnityEngine.Random.Range(mapa.min.y + raioZona, mapa.max.y - raioZona));

        if (!PosicaoValida(c)) continue;
        if (Vector2.Distance(c, posPlayer) < raioZona + 4f) continue;
        if (Physics2D.OverlapCircle(c, raioZona * 0.5f, maskObst)) continue;

        centro = c;
        encontrou = true;
        break;
    }

    if (!encontrou) centro = (Vector2)mapa.center;

    var go = new GameObject("ZonaEliminacao");
    go.transform.position = new Vector3(centro.x, centro.y, 0f);
    zonaEliminacao = go.AddComponent<ZonaEliminacaoEvento>();
    zonaEliminacao.raio           = raioZona;
    zonaEliminacao.quantidade     = eventoAtual.quantidade;
    zonaEliminacao.prefabsInimigos = prefabsInimigosZona;

    zonaEliminacao.OnProgresso += (atual, total) =>
    {
        progresso = atual;
    };
    zonaEliminacao.OnConcluido += () =>
    {
        if (eventoAtivo) EncerrarEvento(true);
    };

    Debug.Log($"[ZonaEliminacao] Zona criada em {centro}, raio={raioZona}, kills={eventoAtual.quantidade}");
}

void LimparZonaEliminacao()
{
    if (zonaEliminacao != null)
    {
        Destroy(zonaEliminacao.gameObject);
        zonaEliminacao = null;
    }
}

void LimparSlimePercurso()
{
    FlowField.AlvoOverride = null;

    if (corSpawnPercurso != null && this != null) { StopCoroutine(corSpawnPercurso); corSpawnPercurso = null; }

    if (slimePercurso != null)
    {
        Destroy(slimePercurso.gameObject);
        slimePercurso = null;
    }

    foreach (var e in inimigosPercurso)
        if (e != null) Destroy(e);
    inimigosPercurso.Clear();
}

    public void RegistrarSlimeColoridaEliminada()
    {
        if (!eventoAtivo || eventoAtual.tipo != TipoEvento.EliminarSlimeColorida) return;
        slimeColoridaAtiva = null;
        EncerrarEvento(true);
    }

    void LimparEspiritos()
    {
        foreach (var esp in espiritosMapa)
            if (esp != null) Destroy(esp);
        espiritosMapa.Clear();
    }

    public void RegistrarEspiritoColetado()
    {
        if (!eventoAtivo || eventoAtual.tipo != TipoEvento.ColetarEspirito) return;
        progresso++;
        if (progresso >= eventoAtual.quantidade)
            EncerrarEvento(true);
    }

    // ──────────────────────────────────────────────────────────
    // Inicialização dos eventos padrão

    void PopularEventosPadrao()
    {
        AdicionarSeAusente(new EventoAleatorio
        {
            nome = "⚡ Tempestade Elétrica",
            descricao = "Raios caem pelo mapa! Fique de olho nos círculos de aviso e sobreviva!",
            tipo = TipoEvento.TempestadeEletrica,
            duracao = 240f,
            quantidade = 0,
            recompensaDescricao = "+15% de vida recuperada!"
        }, primeiro: true);

        AdicionarSeAusente(new EventoAleatorio
        {
            nome = "Eliminar Slime Colorida",
            descricao = "Encontre e elimine a slime colorida!",
            tipo = TipoEvento.EliminarSlimeColorida,
            duracao = 60f,
            quantidade = 1,
            recompensaDescricao = "+15% de vida recuperada!"
        });

        AdicionarSeAusente(new EventoAleatorio
        {
            nome = "Ceifador",
            descricao = "Sobreviva ao ataque dos ceifadores!",
            tipo = TipoEvento.Ceifador,
            duracao = 30f,
            quantidade = 6,
            recompensaDescricao = "+15% de vida recuperada!"
        });

        AdicionarSeAusente(new EventoAleatorio
        {
            nome = "Slime Percurso",
            descricao = "Impeça a slime de atravessar o mapa!",
            tipo = TipoEvento.SlimePercurso,
            duracao = 60f,
            quantidade = 0,
            recompensaDescricao = "+40% de vida recuperada!"
        });

        AdicionarSeAusente(new EventoAleatorio
        {
            nome = "Zona de Eliminação",
            descricao = "Elimine inimigos dentro da zona marcada!",
            tipo = TipoEvento.ZonaEliminacao,
            duracao = 45f,
            quantidade = 10,
            recompensaDescricao = "+15% de vida recuperada!"
        });

        AdicionarSeAusente(new EventoAleatorio
        {
            nome = "Colapso",
            descricao = "A zona está fechando! Sobreviva e elimine inimigos!",
            tipo = TipoEvento.Colapso,
            duracao = 40f,
            quantidade = 15,
            recompensaDescricao = "+15% de vida recuperada!"
        });
    }

    void AdicionarSeAusente(EventoAleatorio evento, bool primeiro = false)
    {
        if (eventos.Exists(e => e.tipo == evento.tipo)) return;
        if (primeiro) eventos.Insert(0, evento);
        else eventos.Add(evento);
    }

    // ──────────────────────────────────────────────────────────
    // Dança

    void IniciarDanca()
    {
        var go = new GameObject("DancaEvento");
        dancaAtiva = go.AddComponent<DancaEvento>();
        dancaAtiva.quantidade = dancaQuantidade;
        dancaAtiva.tempoZona  = dancaTempoZona;
        dancaAtiva.raioZona   = dancaRaioZona;

        dancaAtiva.OnProgresso += (atual, total) => { progresso = atual; };

        dancaAtiva.Iniciar(playerStats, terrenoBase);
    }

    void LimparDanca()
    {
        if (dancaAtiva != null)
        {
            Destroy(dancaAtiva.gameObject);
            dancaAtiva = null;
        }
    }

    // ──────────────────────────────────────────────────────────
    // Portal

    void IniciarPortal()
    {
        Bounds mapa;
        if (terrenoBase != null)
        {
            terrenoBase.CompressBounds();
            mapa = new Bounds();
            mapa.SetMinMax(
                terrenoBase.transform.TransformPoint(terrenoBase.localBounds.min),
                terrenoBase.transform.TransformPoint(terrenoBase.localBounds.max));
        }
        else mapa = CalcularBoundsMapa();

        float ins = insetCanto > 0 ? insetCanto : 6f;
        Vector2[] cantos = new Vector2[]
        {
            new Vector2(mapa.min.x + ins, mapa.min.y + ins),
            new Vector2(mapa.max.x - ins, mapa.min.y + ins),
            new Vector2(mapa.min.x + ins, mapa.max.y - ins),
            new Vector2(mapa.max.x - ins, mapa.max.y - ins),
        };
        int idxA = UnityEngine.Random.Range(0, 2);
        int idxB = idxA == 0 ? 3 : 2;

        Vector2 posA = CantoDentroDoMapa(cantos[idxA], mapa);
        Vector2 posB = CantoDentroDoMapa(cantos[idxB], mapa);

        var go = new GameObject("PortalEvento");
        portalAtivo = go.AddComponent<PortalEvento>();
        portalAtivo.raioFechar     = portalRaioFechar;
        portalAtivo.tempoFechar    = portalTempoFechar;

        portalAtivo.OnConcluido  += () => { if (eventoAtivo) EncerrarEvento(true);  };
        portalAtivo.OnProgresso  += (atual, total) => { progresso = atual; };

        portalAtivo.Iniciar(playerStats, posA, posB, prefabsInimigosPortal, portalIntervaloSpawn);
    }

    void LimparPortal()
    {
        if (portalAtivo != null)
        {
            Destroy(portalAtivo.gameObject);
            portalAtivo = null;
        }
    }

    // ──────────────────────────────────────────────────────────
    // Tempestade Elétrica

    void IniciarTempestadeEletrica()
    {
        var go = new GameObject("TempestadeEletricaEvento");
        tempestadeAtiva = go.AddComponent<TempestadeEletricaEvento>();
        tempestadeAtiva.danoJogador = tempestadeDanoJogador;
        tempestadeAtiva.danoInimigo = tempestadeDanoInimigo;
        tempestadeAtiva.raioImpacto = tempestadeRaioImpacto;
        tempestadeAtiva.terrenoBase = terrenoBase;
        tempestadeAtiva.Iniciar(playerStats, eventoAtual.duracao);
    }

    void LimparTempestadeEletrica()
    {
        if (tempestadeAtiva != null)
        {
            Destroy(tempestadeAtiva.gameObject);
            tempestadeAtiva = null;
        }
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

        painelRT = painelEvento.AddComponent<RectTransform>();
        painelRT.anchorMin = new Vector2(1f, 1f);
        painelRT.anchorMax = new Vector2(1f, 1f);
        painelRT.pivot     = new Vector2(1f, 1f);
        painelRT.anchoredPosition = POS_ESCONDIDO;
        painelRT.sizeDelta = tamanhoDoPanel;

        painelCG = painelEvento.AddComponent<CanvasGroup>();
        painelCG.alpha = 0f;

        Image fundo = painelEvento.AddComponent<Image>();
        fundo.color = corFundo;

        // Borda colorida (painel interno levemente menor)
        GameObject borda = new GameObject("Borda");
        borda.transform.SetParent(painelEvento.transform, false);
        RectTransform rectBorda = borda.AddComponent<RectTransform>();
        rectBorda.anchorMin = Vector2.zero;
        rectBorda.anchorMax = Vector2.one;
        rectBorda.offsetMin = new Vector2(2f, 2f);
        rectBorda.offsetMax = new Vector2(-2f, -2f);
        Image imgBorda = borda.AddComponent<Image>();
        imgBorda.color = corBorda;

        // Nome do evento
        textoNome = CriarTMP(painelEvento, "NomeEvento",
            anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
            pivot: new Vector2(0.5f, 1f),
            anchoredPos: new Vector2(0f, -6f), size: new Vector2(0f, 26f),
            fontSize: tamanhoFonteNome, style: FontStyles.Bold,
            color: corNome, align: TextAlignmentOptions.Center);

        textoDesc = CriarTMP(painelEvento, "DescEvento",
            anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
            pivot: new Vector2(0.5f, 1f),
            anchoredPos: new Vector2(0f, -34f), size: new Vector2(-16f, 36f),
            fontSize: tamanhoFonteDesc, style: FontStyles.Normal,
            color: corDesc, align: TextAlignmentOptions.Center);

        textoTimer = CriarTMP(painelEvento, "TimerEvento",
            anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(0.4f, 0f),
            pivot: new Vector2(0f, 0f),
            anchoredPos: new Vector2(10f, 22f), size: new Vector2(0f, 22f),
            fontSize: tamanhoFonteTimer, style: FontStyles.Bold,
            color: Color.white, align: TextAlignmentOptions.Left);

        textoProgresso = CriarTMP(painelEvento, "ProgressoEvento",
            anchorMin: new Vector2(0.6f, 0f), anchorMax: new Vector2(1f, 0f),
            pivot: new Vector2(1f, 0f),
            anchoredPos: new Vector2(-10f, 22f), size: new Vector2(0f, 22f),
            fontSize: tamanhoFonteTimer, style: FontStyles.Bold,
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
        barraFillImg.color = corBarraAtiva;

        // painel começa fora da tela (POS_ESCONDIDO) e invisível — sem SetActive(false)
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
