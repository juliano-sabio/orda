using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemySpawnerCompleto : MonoBehaviour
{
    [System.Serializable]
    public class TipoInimigo
    {
        public GameObject prefab;
        public string nome;
        public int tempoParaAparecer = 0;
        public float peso = 1f;
    }

    [System.Serializable]
    public class Wave
    {
        public string nome;
        public float duracao = 30f;
        public int maxInimigos = 20;
        public float intervaloSpaw = 2f;
    }

    [Header("REFERÊNCIAS")]
    public Transform player;

    [Header("CAMADAS PROIBIDAS (IMPORTANTE)")]
    [Tooltip("Selecione aqui as Layers que os inimigos NÃO podem nascer em cima (ex: Obstacles, Paredes, Agua)")]
    public LayerMask camadasBloqueadas;

    [Header("TERRENO VÁLIDO")]
    [Tooltip("Tilemaps onde os inimigos PODEM nascer (ex: terreno, ponte). Se vazio, a checagem de terreno é ignorada.")]
    public Tilemap[] tilemapsTerreno;
    [Tooltip("O tamanho do 'corpo' do inimigo para checar se cabe no lugar")]
    public float raioDeChecagem = 0.5f;
    [Tooltip("Quantas vezes ele tenta sortear um lugar novo se o primeiro estiver ocupado")]
    public int tentativasMaximas = 15;

    [Header("LISTA DE INIMIGOS")]
    public List<TipoInimigo> tiposInimigos = new List<TipoInimigo>();

    [Header("SISTEMA DE WAVES")]
    public List<Wave> waves = new List<Wave>();
    public bool usarWaves = true;
    public float tempoEntreWaves = 5f;

    [Header("CONFIGURAÇÕES DE DISTÂNCIA")]
    [SerializeField] private float distanciaMinima = 8f;
    [SerializeField] private float distanciaMaxima = 15f;
    [SerializeField] private int limiteInimigosGlobal = 30;

    [Header("ESCALADA DE DIFICULDADE")]
    [Tooltip("Ativa o aumento progressivo de dificuldade com o tempo")]
    public bool escalarDificuldade = true;
    [Tooltip("Intervalo de spawn no início do jogo (segundos)")]
    public float intervaloInicial   = 2.5f;
    [Tooltip("Intervalo mínimo de spawn (mais difícil possível)")]
    public float intervaloMinimo    = 0.35f;
    [Tooltip("Limite de inimigos simultâneos no início")]
    public int   limiteInicialInimigos = 15;
    [Tooltip("Limite de inimigos simultâneos no pico de dificuldade")]
    public int   limiteFinalInimigos   = 60;
    [Tooltip("(legado — não usado pela escala de spawn linear abaixo)")]
    public float tempoParaEscalar   = 360f;

    [Header("ESCALADA DE SPAWN (linear, sem teto de tempo)")]
    [Tooltip("Quanto o intervalo de spawn CAI por minuto (s/min). Tem piso em intervaloMinimo.")]
    public float intervaloQuedaPorMinuto = 0.25f;
    [Tooltip("Quantos inimigos a MAIS no limite simultâneo por minuto (cresce sem teto de tempo).")]
    public float limiteCrescePorMinuto = 4f;
    [Tooltip("Teto ABSOLUTO de inimigos simultâneos (segurança de performance).")]
    public int limiteMaximoAbsoluto = 120;

    // Variáveis de controle interno
    private float tempoTotalJogo = 0f;
    private float cronometroSpawn = 0f;
    private int waveAtualIndex = 0;
    private bool waveAtiva = false;
    private List<GameObject> inimigosAtivos = new List<GameObject>();
    private List<TipoInimigo> inimigosDisponiveis = new List<TipoInimigo>();

    void Start()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player")?.transform; // coop-local-ok: spawner host-only (Update gateado por NetSpawn.PodeSpawnar); ref p/ posicionamento

        if (usarWaves && waves.Count > 0) IniciarWave(0);
        StartCoroutine(LimpezaAutomatica());
    }

    void Update()
    {
        if (!RunState.Ativo) return;       // run não ligou (players não prontos) ou já acabou
        if (!NetSpawn.PodeSpawnar) return; // clientes não spawnam horda
        if (player == null && PlayerStats.All.Count == 0) return;

        tempoTotalJogo += Time.deltaTime;
        cronometroSpawn += Time.deltaTime;

        AtualizarInimigosDisponiveis();

        if (usarWaves) GerenciarSistemaWaves();
        else LógicaSpawnSimples();
    }

    float GetIntervaloAtual()
    {
        if (!escalarDificuldade) return usarWaves && waves.Count > 0 ? waves[waveAtualIndex].intervaloSpaw : 2f;
        // Cai linearmente por minuto; piso em intervaloMinimo (não dá pra spawnar infinitamente rápido).
        float min = tempoTotalJogo / 60f;
        float baseInicio = usarWaves && waves.Count > 0
            ? Mathf.Min(waves[waveAtualIndex].intervaloSpaw, intervaloInicial)
            : intervaloInicial;
        return Mathf.Max(intervaloMinimo, baseInicio - intervaloQuedaPorMinuto * min);
    }

    int GetLimiteAtual()
    {
        if (!escalarDificuldade) return limiteInimigosGlobal;
        // Cresce linearmente por minuto, SEM teto de tempo — só o teto absoluto de segurança.
        float min = tempoTotalJogo / 60f;
        int limite = limiteInicialInimigos + Mathf.RoundToInt(limiteCrescePorMinuto * min);
        return Mathf.Min(limite, limiteMaximoAbsoluto);
    }

    void GerenciarSistemaWaves()
    {
        if (waveAtiva)
        {
            if (cronometroSpawn >= GetIntervaloAtual())
            {
                TentarSpawnar();
                cronometroSpawn = 0;
            }
        }
    }

    void LógicaSpawnSimples()
    {
        if (cronometroSpawn >= GetIntervaloAtual())
        {
            TentarSpawnar();
            cronometroSpawn = 0;
        }
    }

    void TentarSpawnar()
    {
        if (inimigosAtivos.Count >= GetLimiteAtual() || inimigosDisponiveis.Count == 0) return;

        // Tenta encontrar uma posição válida
        Vector2? posicaoValida = ObterPosicaoLivre();

        if (posicaoValida.HasValue)
        {
            TipoInimigo tipo = EscolherInimigoPorPeso();
            if (tipo == null || tipo.prefab == null) return;
            GameObject novoInimigo = NetSpawn.Spawnar(tipo.prefab, posicaoValida.Value);
            if (novoInimigo != null) inimigosAtivos.Add(novoInimigo);
        }
    }

    bool ForaDaCamera(Vector2 pos)
    {
        if (Camera.main == null) return true;
        Vector3 vp = Camera.main.WorldToViewportPoint(pos);
        const float margem = 0.05f;
        return vp.x < -margem || vp.x > 1f + margem || vp.y < -margem || vp.y > 1f + margem;
    }

    Vector2? ObterPosicaoLivre()
    {
        // Co-op: spawna ao redor de um player escolhido entre todos. SP: o player único.
        Transform refPlayer = PlayerStats.All.Count > 0
            ? PlayerStats.All[Random.Range(0, PlayerStats.All.Count)].transform
            : player;
        if (refPlayer == null) return null;

        // Raio "fora da tela": gera o ponto já a essa distância do player escolhido e
        // exige a mesma distância de TODOS os players → fora da visão do P1 E do P2.
        float raioTela = RaioTela();
        float distMin  = Mathf.Max(distanciaMinima, raioTela);
        float distMax  = Mathf.Max(distanciaMaxima, distMin + 2f);

        for (int i = 0; i < tentativasMaximas; i++)
        {
            float angulo    = Random.Range(0f, 360f);
            Vector2 direcao = new Vector2(Mathf.Cos(angulo * Mathf.Deg2Rad), Mathf.Sin(angulo * Mathf.Deg2Rad));
            float distancia = Random.Range(distMin, distMax);
            Vector2 ponto   = (Vector2)refPlayer.position + direcao * distancia;

            if (!ForaDaVisaoDeTodos(ponto, raioTela)) continue;
            if (!EstaSobreTerreno(ponto)) continue;

            if (Physics2D.OverlapCircle(ponto, raioDeChecagem, camadasBloqueadas) == null)
                return ponto;
        }

        return null;
    }

    // Meia-diagonal da câmera (em unidades de mundo) — distância a partir do player além da
    // qual um ponto está garantidamente fora da tela. Usada como raio "fora de visão".
    //
    // Co-op: o spawner roda no HOST e só conhece a câmera DELE. O P2 pode ter tela mais larga
    // (aspect/resolução diferentes) → usando o aspect real do host, um ponto fora da tela do
    // host podia cair DENTRO da tela do P2. Por isso assumimos um aspect GENEROSO (cobre ultra-
    // wide) e uma margem maior — garante que o ponto fique fora da visão dos DOIS players.
    float RaioTela()
    {
        var cam = Camera.main;
        float h = (cam != null && cam.orthographic) ? cam.orthographicSize : 6f; // fallback razoável
        float aspectGeneroso = 2.1f;      // ~21:9; cobre telas mais largas que a do host
        float w = h * aspectGeneroso;
        return Mathf.Sqrt(h * h + w * w) + 2.5f; // meia-diagonal + margem folgada
    }

    // Em co-op o host não enxerga a câmera do P2, mas sabe a POSIÇÃO dele: um ponto a >= raioTela
    // de TODOS os players está fora da tela de ambos. SP cai no teste de câmera exato.
    bool ForaDaVisaoDeTodos(Vector2 ponto, float raioTela)
    {
        var todos = PlayerStats.All;
        if (todos == null || todos.Count == 0) return ForaDaCamera(ponto);
        for (int i = 0; i < todos.Count; i++)
        {
            var p = todos[i];
            if (p == null) continue;
            if (Vector2.Distance(ponto, p.transform.position) < raioTela) return false;
        }
        return true;
    }

    // Garante que o ponto caia sobre um tile real (terreno/ponte), nunca no vazio fora do mapa
    bool EstaSobreTerreno(Vector2 ponto)
    {
        if (tilemapsTerreno == null || tilemapsTerreno.Length == 0) return true;

        foreach (var tm in tilemapsTerreno)
        {
            if (tm == null) continue;
            Vector3Int celula = tm.WorldToCell(ponto);
            if (tm.HasTile(celula)) return true;
        }
        return false;
    }

    TipoInimigo EscolherInimigoPorPeso()
    {
        float pesoTotal = 0;
        foreach (var inimigo in inimigosDisponiveis) pesoTotal += inimigo.peso;
        float sorteio = Random.Range(0, pesoTotal);
        float acumulado = 0;
        foreach (var inimigo in inimigosDisponiveis)
        {
            acumulado += inimigo.peso;
            if (sorteio <= acumulado) return inimigo;
        }
        return inimigosDisponiveis[0];
    }

    void AtualizarInimigosDisponiveis()
    {
        inimigosDisponiveis.Clear();
        foreach (var inimigo in tiposInimigos)
            if (inimigo.prefab != null && tempoTotalJogo >= inimigo.tempoParaAparecer) inimigosDisponiveis.Add(inimigo);
    }

    void IniciarWave(int index)
    {
        waveAtualIndex = index;
        waveAtiva = true;
    }

    IEnumerator LimpezaAutomatica()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);
            inimigosAtivos.RemoveAll(item => item == null);
        }
    }

    // Visualização no Editor
    void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(player.position, distanciaMinima);
            Gizmos.DrawWireSphere(player.position, distanciaMaxima);
        }
    }
}
