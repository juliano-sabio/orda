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
    NucleoCorrompido,
    CristaisEnergia,
    VorticeDevorador,
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
    [Tooltip("Tempo (segundos) até o evento Cristal ser revelado na segunda fase")]
    public float delayCristal     = 15f;

    [Header("Debug")]
    [Tooltip("-1 = aleatório | 0..N = força índice da lista eventos")]
    public int debugForcarEvento = -1;

    [Header("Eventos Disponíveis")]
    public List<EventoAleatorio> eventos = new List<EventoAleatorio>();

    [Header("Visual — Painel")]
    public Vector2 tamanhoDoPanel    = new Vector2(370f, 130f);
    public Vector2 posicaoVisivel    = new Vector2(-10f, -10f);
    public Sprite  spritePainelEvento;
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

    [Header("Portal")]
    public float        portalRaioFechar     = 2.5f;
    public float        portalTempoFechar    = 3.5f;
    public float        portalIntervaloSpawn = 5f;
    public GameObject[] prefabsInimigosPortal;

    [Header("Cristais de Energia")]
    public float        cristalVidaBase      = 500f;
    public Color        cristalCor           = new Color(0.25f, 0.75f, 1f);

    [Header("Núcleo Corrompido")]
    public float        nucleoVidaBase       = 2000f;
    public float        nucleoIntervaloSpawn = 4f;
    public GameObject[] prefabsInimigosNucleo;

    [Header("Vórtice Devorador")]
    public float vorticeRaioAtracao    = 6f;
    public float vorticeRaioDevorar    = 0.6f;
    public float vorticeForcaAtracao   = 5f;
    public float vorticeDanoPorSegundo = 12f;
    public Color vorticeCor            = new Color(0.5f, 0.1f, 0.7f);

[Header("Tempestade Elétrica")]
    public float tempestadeDanoJogador = 50f;
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
    private NucleoCorrompidoEvento   nucleoAtivo;
    private VorticeDevoradorEvento   vorticeAtivo;
    private readonly List<CristalEvento>   cristaisAtivos      = new List<CristalEvento>();
    private readonly List<IndicadorSlime>  indicadoresCristais = new List<IndicadorSlime>();
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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        ReconectarReferencias();

#if UNITY_EDITOR
        if (spritePainelEvento == null)
        {
            foreach (var a in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(
                "Assets/assets/UI/evento/painel_evento.ase"))
            {
                if (a is Sprite s && a.name == "painel_evento")
                {
                    spritePainelEvento = s;
                    break;
                }
            }
        }
#endif

        if (painelEvento == null)
            CriarPainelUI();

        proximoEventoTempo = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "segunda_fase"
            ? delayCristal
            : delayInicial;

        PopularEventosPadrao();

        InimigoController.OnInimigoDerrotado += OnInimigoDerrotado;
        PlayerStats.OnDanoRecebido += OnDanoRecebido;
        PlayerStats.OnXPColetado += OnXPColetado;
        PlayerStats.OnUltimateAtivada += OnUltimateAtivada;
    }

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        ReconectarReferencias();
        proximoEventoTempo = scene.name == "segunda_fase" ? delayCristal : delayInicial;
        eventoAtivo = false;
    }

    void ReconectarReferencias()
    {
        playerStats  = FindAnyObjectByType<PlayerStats>();
        uiManager    = FindAnyObjectByType<UIManager>();
        timerManager = FindAnyObjectByType<TimerManager>();
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
        // Co-op: eventos são host-autoritativos. Clientes não rodam a lógica de
        // evento; os inimigos de evento (NetworkObject) spawnados no host replicam.
        if (!NetSpawn.PodeSpawnar) return;

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

        if (eventoAtual.tipo == TipoEvento.Ceifador && portalAtivo != null && timerContagem <= 180f)
            portalAtivo.MostrarIndicadores();

        if (eventoAtual.tipo == TipoEvento.Portal && portalAtivo != null)
            portalAtivo.MostrarIndicadores();

        if (eventoAtual.tipo == TipoEvento.CristaisEnergia
            && indicadoresCristais.Count == 0
            && (eventoAtual.duracao - timerContagem) >= 8f)
        {
            foreach (var cristal in cristaisAtivos)
            {
                if (cristal == null || cristal.EstaDestruido) continue;
                var goInd = new GameObject("IndicadorCristal");
                var ind   = goInd.AddComponent<IndicadorSlime>();
                ind.alvo    = cristal.transform;
                ind.corSeta = cristalCor;
                ind.label   = "Cristal!";
                indicadoresCristais.Add(ind);
            }
        }

        if (eventoAtual.tipo == TipoEvento.VorticeDevorador && vorticeAtivo != null && indicadorSlime == null)
        {
            var go = new GameObject("IndicadorVortice");
            indicadorSlime = go.AddComponent<IndicadorSlime>();
            indicadorSlime.alvo    = vorticeAtivo.transform;
            indicadorSlime.corSeta = vorticeCor;
            indicadorSlime.label   = "Vórtice!";
        }

        if (eventoAtual.tipo == TipoEvento.NucleoCorrompido && nucleoAtivo != null && indicadorSlime == null)
        {
            var go = new GameObject("IndicadorNucleo");
            indicadorSlime = go.AddComponent<IndicadorSlime>();
            indicadorSlime.alvo    = nucleoAtivo.transform;
            indicadorSlime.corSeta = new Color(0.1f, 1f, 0.5f);
        }

if (timerContagem <= 0f)
            EncerrarEvento(eventoAtual.tipo == TipoEvento.Sobreviver
                        || eventoAtual.tipo == TipoEvento.TempestadeEletrica
                        || eventoAtual.tipo == TipoEvento.NucleoCorrompido

                        || (eventoAtual.tipo == TipoEvento.SlimePercurso && slimePercurso != null && slimePercurso.Chegou)
                        || (eventoAtual.tipo == TipoEvento.Colapso && progresso >= eventoAtual.quantidade));
    }

    // Tempo decorrido desde o início — usa TimerManager se disponível
    float TempoDecorrido()
    {
        if (timerManager != null)
            return timerManager.currentTime; // currentTime agora ja e o tempo decorrido (crescente)
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
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "segunda_fase")
            idx = eventos.FindIndex(e => e.tipo == TipoEvento.VorticeDevorador);
        else
            idx = eventos.FindIndex(e => e.tipo == TipoEvento.Colapso);
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
    uiManager?.ShowSkillAcquired(Loc.T("event.banner_title"), Loc.T(EventoNomeKey(eventoAtual.tipo)));

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
        SpawnCeifadores(40);
        IniciarPortaisCeifador();
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
    else if (eventoAtual.tipo == TipoEvento.NucleoCorrompido)
    {
        IniciarNucleo();
    }
    else if (eventoAtual.tipo == TipoEvento.CristaisEnergia)
    {
        SpawnCristais();
    }
    else if (eventoAtual.tipo == TipoEvento.VorticeDevorador)
    {
        IniciarVortice();
    }
    else if (eventoAtual.tipo == TipoEvento.Portal)
    {
        IniciarPortal();
    }

}

    void EncerrarEvento(bool sucesso)
    {
        if (!eventoAtivo) return; // guard contra double-call
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
        LimparNucleo();
        LimparCristais();
        LimparVortice();

        if (sucesso && playerStats != null)
        {
            float pctCura = eventoAtual.tipo == TipoEvento.SlimePercurso ? 0.40f : 0.15f;
            playerStats.Heal(playerStats.maxHealth * pctCura);
        }

        StartCoroutine(MostrarResultado(sucesso));
        if (sucesso) StartCoroutine(OfertarEvolucaoAposEvento());
        AgendarProximoEvento();
    }

    IEnumerator MostrarResultado(bool sucesso)
    {
        // Painel já está visível — só atualiza o conteúdo
        if (textoNome   != null) { textoNome.text = sucesso ? Loc.T("event.success") : Loc.T("event.failure"); textoNome.color = sucesso ? new Color(0.2f, 1f, 0.3f) : new Color(1f, 0.3f, 0.3f); }
        if (textoDesc   != null) textoDesc.text = sucesso ? Loc.T(EventoRecompensaKey(eventoAtual.tipo)) : Loc.T("event.not_completed");
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
            if (textoNome  != null) { textoNome.text = Loc.T(EventoNomeKey(eventoAtual.tipo)); textoNome.color = Color.yellow; }
            if (textoDesc  != null) textoDesc.text = TextUtils.SemAcento(Loc.T(EventoDescKey(eventoAtual.tipo)));
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

    // Retorna true para eventos em que o progresso de kills/coleta deve aparecer no timer
    bool EventoMostraProgresso()
    {
        if (eventoAtual == null || eventoAtual.quantidade <= 0) return false;
        return eventoAtual.tipo == TipoEvento.MatarInimigos
            || eventoAtual.tipo == TipoEvento.Colapso
            || eventoAtual.tipo == TipoEvento.CristaisEnergia
            || eventoAtual.tipo == TipoEvento.VorticeDevorador
            || eventoAtual.tipo == TipoEvento.ZonaEliminacao
            || eventoAtual.tipo == TipoEvento.ColetarXP
            || eventoAtual.tipo == TipoEvento.UsarUltimate
            || eventoAtual.tipo == TipoEvento.ColetarEspirito
            || eventoAtual.tipo == TipoEvento.Ceifador
            || eventoAtual.tipo == TipoEvento.Portal;
    }

    void AtualizarUI()
    {
        float pct = Mathf.Clamp01(timerContagem / eventoAtual.duracao);

        if (barraFill != null)
            barraFill.anchorMax = new Vector2(pct, 1f);

        // Progresso integrado no timer para ficar bem visível
        if (textoProgresso != null)
            textoProgresso.text = "";

        if (textoTimer != null)
        {
            string timerStr = Mathf.CeilToInt(timerContagem) + "s";
            textoTimer.text = EventoMostraProgresso()
                ? $"{progresso}/{eventoAtual.quantidade}  •  {timerStr}"
                : timerStr;
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
        var tilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
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
            var todos = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
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

            slimeColoridaAtiva = NetSpawn.Spawnar(
                slimeColoridaPrefab,
                new Vector3(candidato.x, candidato.y, 0f)
            ); // host-only em rede
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
            var go = NetSpawn.Spawnar(ceifadorPrefab,
                new Vector3(candidato.x, candidato.y, 0f)); // host-only em rede
            if (go != null) ceifadoresMapa.Add(go);
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

    var go = NetSpawn.Spawnar(slimePercursoPrefab, new Vector3(origem.x, origem.y, 0f)); // host-only em rede
    slimePercurso = go != null ? go.GetComponent<SlimePercursoEvento>() : null;
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
        if (!eventoAtivo || slimePercurso == null || slimePercurso.Chegou) yield break;

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
            var go = NetSpawn.Spawnar(prefab, new Vector3(pos.x, pos.y, 0f)); // host-only em rede
            if (go != null) inimigosPercurso.Add(go);
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

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "segunda_fase")
        {
            AdicionarSeAusente(new EventoAleatorio
            {
                nome = "Eliminar Slime Colorida",
                descricao = "Encontre e elimine a slime colorida!",
                tipo = TipoEvento.EliminarSlimeColorida,
                duracao = 60f,
                quantidade = 1,
                recompensaDescricao = "+15% de vida recuperada!"
            });
        }

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "segunda_fase")
        {
            // Ceifador: sempre atualiza para garantir valores corretos mesmo se já serializado
            var ceifadorEvt = eventos.Find(e => e.tipo == TipoEvento.Ceifador);
            if (ceifadorEvt == null)
            {
                ceifadorEvt = new EventoAleatorio { tipo = TipoEvento.Ceifador };
                eventos.Add(ceifadorEvt);
            }
            ceifadorEvt.nome                = "Ceifador";
            ceifadorEvt.descricao           = "Feche os 6 portais espalhados pelo mapa! Os ceifadores vao te impedir!";
            ceifadorEvt.duracao             = 300f;
            ceifadorEvt.quantidade          = 6;
            ceifadorEvt.recompensaDescricao = "+15% de vida recuperada!";
        }

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "segunda_fase")
        {
            AdicionarSeAusente(new EventoAleatorio
            {
                nome = "Slime Percurso",
                descricao = "Impeça a slime de atravessar o mapa!",
                tipo = TipoEvento.SlimePercurso,
                duracao = 60f,
                quantidade = 0,
                recompensaDescricao = "+40% de vida recuperada!"
            });
        }

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
            nome = "Nucleo Corrompido",
            descricao = "Proteja o Nucleo! Inimigos ignoram voce e marcham para destrui-lo!",
            tipo = TipoEvento.NucleoCorrompido,
            duracao = 60f,
            quantidade = 0,
            recompensaDescricao = "+20% de vida recuperada!"
        });

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "segunda_fase")
        {
            AdicionarSeAusente(new EventoAleatorio
            {
                nome                = "Cristal",
                descricao           = "Destrua os 3 cristais espalhados pelo mapa antes do tempo acabar!",
                tipo                = TipoEvento.CristaisEnergia,
                duracao             = 300f,
                quantidade          = 3,
                recompensaDescricao = "+20% de vida recuperada!"
            });
        }

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "segunda_fase")
        {
            AdicionarSeAusente(new EventoAleatorio
            {
                nome                = "Vórtice Devorador",
                descricao           = "Um vortice apareceu no mapa! Atraia os inimigos para dentro dele antes do tempo acabar!",
                tipo                = TipoEvento.VorticeDevorador,
                duracao             = 75f,
                quantidade          = 20,
                recompensaDescricao = "+15% de vida recuperada!"
            });
        }

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

    // Nome/descrição exibidos ao jogador são sempre resolvidos via Loc.T() pelo
    // tipo do evento, ignorando os campos nome/descricao serializados (que
    // existem só como fallback de editor/debug e ficam em PT fixo).
    static string EventoNomeKey(TipoEvento tipo) => $"event.{tipo.ToString().ToLower()}.nome";
    static string EventoDescKey(TipoEvento tipo) => $"event.{tipo.ToString().ToLower()}.desc";

    static string EventoRecompensaKey(TipoEvento tipo) => tipo switch
    {
        TipoEvento.Ceifador       => "event.reward.heal20",
        TipoEvento.SlimePercurso  => "event.reward.heal20",
        TipoEvento.ZonaEliminacao => "event.reward.heal20",
        TipoEvento.NucleoCorrompido => "event.reward.heal20",
        _ => "event.reward.heal15",
    };

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

    void IniciarPortaisCeifador()
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

        int maskObst = camadasObstaculo != 0 ? (int)camadasObstaculo : (1 << 3);
        Vector2 posPlayer = playerStats != null ? (Vector2)playerStats.transform.position : Vector2.zero;

        var posicoes = new List<Vector2>();
        for (int i = 0; i < 6; i++)
        {
            bool ok = false;
            for (int t = 0; t < 200; t++)
            {
                Vector2 c = new Vector2(
                    UnityEngine.Random.Range(mapa.min.x + 5f, mapa.max.x - 5f),
                    UnityEngine.Random.Range(mapa.min.y + 5f, mapa.max.y - 5f));
                if (!PosicaoValida(c)) continue;
                if (Physics2D.OverlapCircle(c, 2f, maskObst)) continue;
                if (Vector2.Distance(c, posPlayer) < 10f) continue;
                bool longe = true;
                foreach (var p in posicoes)
                    if (Vector2.Distance(c, p) < 10f) { longe = false; break; }
                if (!longe) continue;
                posicoes.Add(c);
                ok = true;
                break;
            }
            if (!ok)
            {
                Vector2 fb = new Vector2(
                    UnityEngine.Random.Range(mapa.min.x + 5f, mapa.max.x - 5f),
                    UnityEngine.Random.Range(mapa.min.y + 5f, mapa.max.y - 5f));
                posicoes.Add(fb);
            }
        }

        var go = new GameObject("PortalEvento");
        portalAtivo = go.AddComponent<PortalEvento>();
        portalAtivo.raioFechar  = portalRaioFechar;
        portalAtivo.tempoFechar = portalTempoFechar;

        portalAtivo.OnConcluido += () => { if (eventoAtivo) EncerrarEvento(true); };
        portalAtivo.OnProgresso += (atual, total) => { progresso = atual; };

        portalAtivo.IniciarMultiplo(playerStats, posicoes.ToArray(), prefabsInimigosPortal, portalIntervaloSpawn);
        Debug.Log($"[Ceifador] {posicoes.Count} portais criados.");
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
    // Núcleo Corrompido

    void IniciarNucleo()
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

        Vector2 centro = (Vector2)mapa.center;
        int maskObstNucleo = camadasObstaculo != 0 ? (int)camadasObstaculo : (1 << 3);

        // Procura posição válida perto do centro do mapa
        Vector2 posNucleo = centro;
        bool achouNucleo = false;
        for (int t = 0; t < 300 && !achouNucleo; t++)
        {
            Vector2 candidato = t == 0 ? centro
                : centro + UnityEngine.Random.insideUnitCircle * (1f + t * 0.3f);
            if (!PosicaoValida(candidato)) continue;
            if (Physics2D.OverlapCircle(candidato, 1.5f, maskObstNucleo)) continue;
            posNucleo = candidato;
            achouNucleo = true;
        }

        var go = new GameObject("NucleoCorrompido");
        go.transform.position = new Vector3(posNucleo.x, posNucleo.y, 0f);
        nucleoAtivo = go.AddComponent<NucleoCorrompidoEvento>();

        nucleoAtivo.OnDestruido += () => { if (eventoAtivo) EncerrarEvento(false); };

        var prefabs = prefabsInimigosNucleo != null && prefabsInimigosNucleo.Length > 0
            ? prefabsInimigosNucleo
            : prefabsInimigosPortal;

        nucleoAtivo.Iniciar(nucleoVidaBase, prefabs, nucleoIntervaloSpawn);
        Debug.Log($"[Nucleo] Nucleo criado em {posNucleo} (centro={centro}, valido={achouNucleo})");
    }

    void LimparNucleo()
    {
        if (nucleoAtivo != null)
        {
            Destroy(nucleoAtivo.gameObject);
            nucleoAtivo = null;
        }
    }

    // ──────────────────────────────────────────────────────────
    // Vórtice Devorador

    void IniciarVortice()
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

        Vector2 centro = (Vector2)mapa.center;
        bool encontrou = false;
        for (int t = 0; t < 150; t++)
        {
            Vector2 c = new Vector2(
                UnityEngine.Random.Range(mapa.min.x + vorticeRaioAtracao, mapa.max.x - vorticeRaioAtracao),
                UnityEngine.Random.Range(mapa.min.y + vorticeRaioAtracao, mapa.max.y - vorticeRaioAtracao));

            if (!PosicaoValida(c)) continue;
            if (Vector2.Distance(c, posPlayer) < vorticeRaioAtracao + 5f) continue;
            if (Physics2D.OverlapCircle(c, vorticeRaioDevorar + 0.5f, maskObst)) continue;

            centro = c;
            encontrou = true;
            break;
        }
        if (!encontrou) centro = (Vector2)mapa.center;

        var go = new GameObject("VorticeDevorador");
        go.transform.position = new Vector3(centro.x, centro.y, 0f);
        vorticeAtivo = go.AddComponent<VorticeDevoradorEvento>();
        vorticeAtivo.raioAtracao    = vorticeRaioAtracao;
        vorticeAtivo.raioDevorar    = vorticeRaioDevorar;
        vorticeAtivo.forcaAtracao   = vorticeForcaAtracao;
        vorticeAtivo.danoPorSegundo = vorticeDanoPorSegundo;
        vorticeAtivo.corVortice     = vorticeCor;

        vorticeAtivo.OnProgresso += (atual, total) => { progresso = atual; };
        vorticeAtivo.OnConcluido += () => { if (eventoAtivo) EncerrarEvento(true); };

        vorticeAtivo.Iniciar(eventoAtual.quantidade);

        Debug.Log($"[VorticeDevorador] Vórtice criado em {centro} (válido={encontrou}), meta={eventoAtual.quantidade}");
    }

    void LimparVortice()
    {
        if (vorticeAtivo != null)
        {
            Destroy(vorticeAtivo.gameObject);
            vorticeAtivo = null;
        }
    }

    // ──────────────────────────────────────────────────────────
    // Cristais de Energia

    void SpawnCristais()
    {
        cristaisAtivos.Clear();
        progresso = 0;

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
        int maskObst      = camadasObstaculo != 0 ? (int)camadasObstaculo : (1 << 3);
        const int QTD     = 3;
        const float DIST_MINIMA_ENTRE = 15f;

        var posSelecionadas = new List<Vector2>();

        for (int i = 0; i < QTD; i++)
        {
            bool achou = false;
            for (int t = 0; t < 200; t++)
            {
                Vector2 c = new Vector2(
                    UnityEngine.Random.Range(mapa.min.x + 5f, mapa.max.x - 5f),
                    UnityEngine.Random.Range(mapa.min.y + 5f, mapa.max.y - 5f));

                if (!PosicaoValida(c)) continue;
                if (Physics2D.OverlapCircle(c, 1.5f, maskObst)) continue;
                if (Vector2.Distance(c, posPlayer) < 8f) continue;

                bool longe = true;
                foreach (var p in posSelecionadas)
                    if (Vector2.Distance(c, p) < DIST_MINIMA_ENTRE) { longe = false; break; }
                if (!longe) continue;

                posSelecionadas.Add(c);
                CriarCristal(c);
                achou = true;
                break;
            }

            if (!achou && posSelecionadas.Count > 0)
            {
                // Fallback: aceita posição próxima de outra já colocada
                for (int t = 0; t < 100; t++)
                {
                    Vector2 c = new Vector2(
                        UnityEngine.Random.Range(mapa.min.x + 5f, mapa.max.x - 5f),
                        UnityEngine.Random.Range(mapa.min.y + 5f, mapa.max.y - 5f));
                    if (!PosicaoValida(c)) continue;
                    if (Physics2D.OverlapCircle(c, 1.5f, maskObst)) continue;
                    posSelecionadas.Add(c);
                    CriarCristal(c);
                    break;
                }
            }
        }

        Debug.Log($"[CristaisEnergia] {cristaisAtivos.Count} cristais spawnados.");
    }

    void CriarCristal(Vector2 pos)
    {
        var go     = new GameObject("CristalEnergia");
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        var cristal = go.AddComponent<CristalEvento>();
        cristal.vidaBase = cristalVidaBase;
        cristal.corBase  = cristalCor;

        cristal.OnDestruido += () =>
        {
            progresso++;
            if (progresso >= eventoAtual.quantidade && eventoAtivo)
                EncerrarEvento(true);
        };

        cristal.Iniciar();
        cristaisAtivos.Add(cristal);
    }

    void LimparCristais()
    {
        foreach (var c in cristaisAtivos)
            if (c != null && !c.EstaDestruido) Destroy(c.gameObject);
        cristaisAtivos.Clear();

        foreach (var ind in indicadoresCristais)
            if (ind != null) Destroy(ind.gameObject);
        indicadoresCristais.Clear();
    }

// ──────────────────────────────────────────────────────────
    // Evolução de skill como recompensa de evento

    [Header("Evolução de Skills")]
    public List<SkillEvolutionData> todasEvolucoes = new List<SkillEvolutionData>();

    IEnumerator OfertarEvolucaoAposEvento()
    {
        // Aguarda o resultado do evento ser exibido
        yield return new WaitForSeconds(3.5f);

        var sm = SkillManager.Instance;
        if (sm == null) { Debug.LogWarning("[Evolução] SkillManager não encontrado."); yield break; }
        if (sm.activeSkills.Count == 0)
        {
            // Sem skills ativas → ofertar escolha inicial de skill em vez de pular
            Debug.LogWarning("[Evolução] Player não tem skills ativas — abrindo escolha inicial de skill.");
            var choiceUI = UnityEngine.Object.FindFirstObjectByType<SkillChoiceUI>(FindObjectsInactive.Include);
            if (choiceUI != null)
            {
                choiceUI.somenteSkillsDeAtaque = true;
                choiceUI.somenteSkillsDeDefesa = false;
                choiceUI.ShowRandomSkillChoice(skill => { sm.AddSkill(skill); });
            }
            yield break;
        }

        if (todasEvolucoes == null || todasEvolucoes.Count == 0)
        {
            Debug.LogWarning("[Evolução] todasEvolucoes está vazia! Execute Tools → Evolucoes → Criar Todas as Evolucoes com a cena aberta.");
            yield break;
        }

        Debug.Log($"[Evolução] Skills ativas: {sm.activeSkills.Count} | Evoluções cadastradas: {todasEvolucoes.Count}");

        // Filtra evoluções disponíveis para as skills ativas do player
        // Verifica se a evolução ESPECÍFICA já foi aplicada (não apenas se a skill tem qualquer evolução)
        var disponiveis = new List<SkillEvolutionData>();
        foreach (var evo in todasEvolucoes)
        {
            if (evo == null) continue;
            bool temSkill    = sm.activeSkills.Exists(s => s != null && s.specificType == evo.skillAlvo);
            bool jaTemEstaEvo = SkillEvolutionManager.Instance != null &&
                                SkillEvolutionManager.Instance.EvolucaoAtiva(evo.tipoEvolucao);
            if (temSkill && !jaTemEstaEvo) disponiveis.Add(evo);
        }

        // Verifica ultimates ativas do player
        var playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats != null)
        {
            // Mapeia component de ultimate para o SpecificSkillType correspondente
            var ultimateMap = new System.Collections.Generic.Dictionary<System.Type, SpecificSkillType>
            {
                { typeof(RaioCerteiroUltimate),      SpecificSkillType.UltRaioCerteiro      },
                { typeof(TempestadeEletricaUltimate),SpecificSkillType.UltTempestadeEletrica },
                { typeof(ChuvaMeteorosUltimate),     SpecificSkillType.UltChuvaMeteorosUlt   },
                { typeof(CampoDeGeloUltimate),       SpecificSkillType.UltCampoDeGelo        },
                { typeof(VorticeUltimate),           SpecificSkillType.UltVortice            },
                { typeof(NecropoleUltimate),         SpecificSkillType.UltNecropole          },
                { typeof(RitualAnciaoUltimate),      SpecificSkillType.UltRitualAnciao       },
                { typeof(BencaoAnciaoUltimate),      SpecificSkillType.UltBencaoAnciao       },
                { typeof(CasuloCristalUltimate),     SpecificSkillType.UltCasuloCristal      },
                { typeof(CorrentesInfernoUltimate),  SpecificSkillType.UltCorrentesInferno   },
                { typeof(DrenagemDeVidaUltimate),    SpecificSkillType.UltDrenagemDeVida     },
                { typeof(EscudoSonicoUltimate),      SpecificSkillType.UltEscudoSonico       },
                { typeof(PulsoMagneticoUltimate),    SpecificSkillType.UltPulsoMagnetico     },
                { typeof(PunicaoDivinaUltimate),     SpecificSkillType.UltPunicaoDivina      },
                { typeof(DomoRetardanteUltimate),    SpecificSkillType.UltDomoRetardante     },
                { typeof(DespertarAnciaoUltimate),   SpecificSkillType.UltDespertarAnciao    },
                { typeof(MareImplacavelUltimate),    SpecificSkillType.UltMareImplacavel     },
            };

            foreach (var kv in ultimateMap)
            {
                if (playerStats.GetComponent(kv.Key) == null) continue;
                SpecificSkillType ultimateType = kv.Value;

                foreach (var evo in todasEvolucoes)
                {
                    if (evo == null) continue;
                    if (evo.skillAlvo != ultimateType) continue;
                    bool jaTemEstaEvo = SkillEvolutionManager.Instance != null &&
                                       SkillEvolutionManager.Instance.EvolucaoAtiva(evo.tipoEvolucao);
                    if (!jaTemEstaEvo && !disponiveis.Contains(evo))
                        disponiveis.Add(evo);
                }
            }
        }

        Debug.Log($"[Evolução] Disponíveis para o player: {disponiveis.Count}");
        if (disponiveis.Count == 0) { Debug.LogWarning("[Evolução] Nenhuma evolução disponível para as skills atuais."); yield break; }

        // Sorteia 2 opções aleatórias
        var opcoes = new List<SkillEvolutionData>();
        var copia  = new List<SkillEvolutionData>(disponiveis);
        int qtd    = Mathf.Min(2, copia.Count);
        for (int i = 0; i < qtd; i++)
        {
            int idx = UnityEngine.Random.Range(0, copia.Count);
            opcoes.Add(copia[idx]);
            copia.RemoveAt(idx);
        }

        // Garante que o manager de evolução existe
        if (SkillEvolutionManager.Instance == null)
        {
            var go = new GameObject("SkillEvolutionManager");
            go.AddComponent<SkillEvolutionManager>();
        }

        // Cria UI se não existir
        if (SkillEvolutionUI.Instance == null)
        {
            var go = new GameObject("SkillEvolutionUI");
            go.AddComponent<SkillEvolutionUI>();
        }

        // Exibe as opções
        SkillEvolutionUI.Instance.MostrarOpcoes(opcoes, evo =>
        {
            SkillEvolutionManager.Instance?.AplicarEvolucao(evo);
            if (UIManager.Instance != null)
                UIManager.Instance.ShowSkillAcquired(evo.GetDisplayName(), evo.GetDisplayDescription());
        });
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
        canvas.sortingOrder = 99;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Painel raiz ──────────────────────────────────────────────────────
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

        // Fundo principal
        Image fundo = painelEvento.AddComponent<Image>();
        if (spritePainelEvento != null)
        {
            fundo.sprite = spritePainelEvento;
            fundo.type   = Image.Type.Simple;
            fundo.color  = Color.white;
        }
        else
        {
            fundo.color = corFundo;

            // ── Borda externa escura ─────────────────────────────────────────
            var bordaExt = new GameObject("BordaExterna");
            bordaExt.transform.SetParent(painelEvento.transform, false);
            bordaExt.transform.SetAsFirstSibling();
            var bordaExtRT = bordaExt.AddComponent<RectTransform>();
            bordaExtRT.anchorMin = Vector2.zero; bordaExtRT.anchorMax = Vector2.one;
            bordaExtRT.offsetMin = new Vector2(-2f, -2f); bordaExtRT.offsetMax = new Vector2(2f, 2f);
            bordaExt.AddComponent<Image>().color = new Color(0.02f, 0.02f, 0.08f, 1f);

            // ── Faixa de cor no topo ─────────────────────────────────────────
            var faixaTopo = new GameObject("FaixaTopo");
            faixaTopo.transform.SetParent(painelEvento.transform, false);
            var faixaTopoRT = faixaTopo.AddComponent<RectTransform>();
            faixaTopoRT.anchorMin = new Vector2(0f, 1f); faixaTopoRT.anchorMax = new Vector2(1f, 1f);
            faixaTopoRT.pivot = new Vector2(0.5f, 1f);
            faixaTopoRT.anchoredPosition = Vector2.zero; faixaTopoRT.sizeDelta = new Vector2(0f, 3f);
            faixaTopo.AddComponent<Image>().color = corBorda;

            // ── Faixa de cor na esquerda ─────────────────────────────────────
            var faixaEsq = new GameObject("FaixaEsquerda");
            faixaEsq.transform.SetParent(painelEvento.transform, false);
            var faixaEsqRT = faixaEsq.AddComponent<RectTransform>();
            faixaEsqRT.anchorMin = new Vector2(0f, 0f); faixaEsqRT.anchorMax = new Vector2(0f, 1f);
            faixaEsqRT.pivot = new Vector2(0f, 0.5f);
            faixaEsqRT.anchoredPosition = Vector2.zero; faixaEsqRT.sizeDelta = new Vector2(4f, 0f);
            faixaEsq.AddComponent<Image>().color = corBorda;
        }

        barraFill    = null;
        barraFillImg = null;

        // ── Textos ───────────────────────────────────────────────────────────

        // Nome do evento — topo (72–100%)
        textoNome = CriarTMP(painelEvento, "NomeEvento",
            anchorMin: new Vector2(0f, 0.70f), anchorMax: new Vector2(1f, 1f),
            pivot: new Vector2(0.5f, 1f),
            anchoredPos: new Vector2(0f, -32f), size: new Vector2(0f, 0f),
            fontSize: tamanhoFonteNome, style: FontStyles.Bold,
            color: corNome, align: TextAlignmentOptions.Center);
        textoNome.outlineWidth = 0.22f;
        textoNome.outlineColor = new Color32(0, 0, 0, 210);

        // Descrição — meio (28–70%)
        textoDesc = CriarTMP(painelEvento, "DescEvento",
            anchorMin: new Vector2(0.04f, 0.28f), anchorMax: new Vector2(0.96f, 0.70f),
            pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: new Vector2(0f, 0f), size: new Vector2(0f, 0f),
            fontSize: tamanhoFonteDesc, style: FontStyles.Normal,
            color: corDesc, align: TextAlignmentOptions.Center);

        // Timer — rodapé (7–28%), centralizado
        textoTimer = CriarTMP(painelEvento, "TimerEvento",
            anchorMin: new Vector2(0f, 0.14f), anchorMax: new Vector2(1f, 0.35f),
            pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: new Vector2(0f, 0f), size: new Vector2(0f, 0f),
            fontSize: tamanhoFonteTimer, style: FontStyles.Bold,
            color: Color.white, align: TextAlignmentOptions.Center);

        // Progresso — oculto (integrado no textoTimer)
        textoProgresso = CriarTMP(painelEvento, "ProgressoEvento",
            anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(0f, 0f),
            pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: new Vector2(0f, 0f), size: new Vector2(0f, 0f),
            fontSize: tamanhoFonteTimer - 2f, style: FontStyles.Normal,
            color: Color.white, align: TextAlignmentOptions.Center);

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
        tmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Truncate;
        return tmp;
    }
}
