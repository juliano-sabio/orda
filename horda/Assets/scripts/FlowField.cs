using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Coloque este componente em um GameObject vazio na cena (ex: "FlowField").
// Ele calcula automaticamente o melhor caminho até o player para toda a grade.
public class FlowField : MonoBehaviour
{
    public static FlowField Instance { get; private set; }

    [Header("Grade")]
    [Tooltip("Tamanho de cada célula em unidades. 1 = bom para a maioria dos jogos.")]
    public float tamanhoCelula = 1f;
    [Tooltip("Espaço extra além dos limites da câmera.")]
    public float padding = 8f;

    [Header("Obstáculos")]
    public LayerMask camadaObstaculos = 1 << 3;

    [Header("Atualização")]
    [Tooltip("Intervalo em segundos para recalcular o campo.")]
    public float intervaloAtualizacao = 0.2f;
    [Tooltip("Intervalo mínimo entre reconstruções do grid (quando player sai da área).")]
    public float intervaloReconstrucao = 0.5f;

    private Vector2[,] vetores;
    private bool[,] caminhavel;
    private Vector2Int gridTamanho;
    private Vector2 gridOrigem;
    private Vector2 gridCentroAtual;
    private float tempoUltimaReconstrucao = -999f;
    private Transform playerTransform;
    public static Transform AlvoOverride;

    private static readonly Vector2Int[] dirs8 = {
        new Vector2Int( 1, 0), new Vector2Int(-1, 0),
        new Vector2Int( 0, 1), new Vector2Int( 0,-1),
        new Vector2Int( 1, 1), new Vector2Int(-1, 1),
        new Vector2Int( 1,-1), new Vector2Int(-1,-1)
    };

    void Awake() => Instance = this;

    void Start()
    {
        playerTransform = (PlayerStats.All.Count > 0 ? PlayerStats.All[0].transform : null);
        ConstruirGridMapa();
        if (playerTransform != null) Recalcular();
        InvokeRepeating(nameof(Tick), intervaloAtualizacao, intervaloAtualizacao);
    }

    // Grid cobre o MAPA TODO (bounds dos tilemaps + padding), estático. Antes era do
    // tamanho da câmera e seguia o player — inimigos fora dele caíam na mira direta
    // (sem desvio de parede) e bugavam ao não achar rota. Cobrindo o mapa todo, todo
    // inimigo sempre tem pathfinding.
    void ConstruirGridMapa()
    {
        Bounds b = CalcularBoundsMapa();
        Vector2 min = (Vector2)b.min - Vector2.one * padding;
        Vector2 max = (Vector2)b.max + Vector2.one * padding;
        float w = Mathf.Max(1f, max.x - min.x);
        float h = Mathf.Max(1f, max.y - min.y);

        // cap de células pra perf: se o mapa for enorme, engrossa a célula efetiva.
        const int MAX_DIM = 320;
        float cel = Mathf.Max(tamanhoCelula, w / MAX_DIM, h / MAX_DIM);
        tamanhoCelula = cel;

        gridOrigem = min;
        gridCentroAtual = b.center;
        gridTamanho = new Vector2Int(Mathf.CeilToInt(w / cel), Mathf.CeilToInt(h / cel));

        vetores    = new Vector2[gridTamanho.x, gridTamanho.y];
        caminhavel = new bool[gridTamanho.x, gridTamanho.y];

        Vector2 metadeCelula = Vector2.one * (cel * 0.85f);
        for (int x = 0; x < gridTamanho.x; x++)
            for (int y = 0; y < gridTamanho.y; y++)
            {
                Vector2 pos = CelulaPosicao(x, y);
                caminhavel[x, y] = !Physics2D.OverlapBox(pos, metadeCelula, 0f, camadaObstaculos);
            }
    }

    // Bounds do mapa = encapsula todos os tilemaps ativos (fallback 60x40).
    Bounds CalcularBoundsMapa()
    {
        var tilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        if (tilemaps == null || tilemaps.Length == 0)
            return new Bounds(Vector3.zero, new Vector3(60f, 40f, 0f));

        bool primeiro = true;
        Bounds bounds = new Bounds();
        foreach (var tm in tilemaps)
        {
            if (tm == null || !tm.gameObject.activeInHierarchy) continue;
            tm.CompressBounds();
            Bounds bb = new Bounds();
            bb.SetMinMax(
                tm.transform.TransformPoint(tm.localBounds.min),
                tm.transform.TransformPoint(tm.localBounds.max));
            if (primeiro) { bounds = bb; primeiro = false; }
            else bounds.Encapsulate(bb);
        }
        if (primeiro) return new Bounds(Vector3.zero, new Vector3(60f, 40f, 0f));
        return bounds;
    }

    void Tick()
    {
        if (caminhavel == null) ConstruirGridMapa();
        if (playerTransform == null && PlayerStats.All.Count > 0)
            playerTransform = PlayerStats.All[0].transform;
        Recalcular();
    }

    void Recalcular()
    {
        if (caminhavel == null) return;

        // Origens do BFS: com AlvoOverride (eventos), um alvo só; senão TODOS os players
        // (BFS multi-origem → cada célula aponta pro player mais próximo em distância de
        // caminho, fazendo cada inimigo caçar o player mais perto — co-op).
        var origens = new List<Vector2Int>();
        if (AlvoOverride != null)
        {
            Vector2Int c = MundoParaCelula(AlvoOverride.position);
            if (Valido(c)) origens.Add(c);
        }
        else
        {
            for (int i = 0; i < PlayerStats.All.Count; i++)
            {
                var p = PlayerStats.All[i];
                if (p == null) continue;
                Vector2Int c = MundoParaCelula(p.transform.position);
                if (Valido(c)) origens.Add(c);
            }
        }
        if (origens.Count == 0) return;

        // BFS a partir dos players — propaga custo para toda a grade
        int[,] custo = new int[gridTamanho.x, gridTamanho.y];
        for (int x = 0; x < gridTamanho.x; x++)
            for (int y = 0; y < gridTamanho.y; y++)
                custo[x, y] = int.MaxValue;

        var fila = new Queue<Vector2Int>();
        foreach (var o in origens)
        {
            if (custo[o.x, o.y] == 0) continue;
            custo[o.x, o.y] = 0;
            fila.Enqueue(o);
        }

        while (fila.Count > 0)
        {
            Vector2Int cur = fila.Dequeue();
            int curCusto = custo[cur.x, cur.y];

            foreach (var d in dirs8)
            {
                Vector2Int nb = new Vector2Int(cur.x + d.x, cur.y + d.y);
                if (!Valido(nb) || !caminhavel[nb.x, nb.y]) continue;

                // Impede cortar cantos: diagonal só se os dois lados estiverem livres
                if (d.x != 0 && d.y != 0)
                {
                    if (!caminhavel[cur.x + d.x, cur.y]) continue;
                    if (!caminhavel[cur.x, cur.y + d.y]) continue;
                }

                int nc = curCusto + 1;
                if (nc < custo[nb.x, nb.y])
                {
                    custo[nb.x, nb.y] = nc;
                    fila.Enqueue(nb);
                }
            }
        }

        // Cada célula aponta para o vizinho de menor custo → direção ótima ao player
        for (int x = 0; x < gridTamanho.x; x++)
            for (int y = 0; y < gridTamanho.y; y++)
            {
                if (!caminhavel[x, y] || custo[x, y] == int.MaxValue)
                {
                    vetores[x, y] = Vector2.zero;
                    continue;
                }

                Vector2Int melhor = new Vector2Int(x, y);
                int min = custo[x, y];
                foreach (var d in dirs8)
                {
                    Vector2Int nb = new Vector2Int(x + d.x, y + d.y);
                    if (!Valido(nb) || custo[nb.x, nb.y] >= min) continue;
                    min = custo[nb.x, nb.y];
                    melhor = nb;
                }

                vetores[x, y] = (CelulaPosicao(melhor.x, melhor.y) - CelulaPosicao(x, y)).normalized;
            }
    }

    // Retorna a direção ótima para o alvo atual a partir de uma posição do mundo.
    // Fora do grid (ou célula sem fluxo), cai pra mira direta no player MAIS PRÓXIMO
    // desta posição (co-op: inimigos longe do host ainda caçam o player mais perto).
    public Vector2 ObterDirecao(Vector2 pos)
    {
        Transform alvoFallback = AlvoOverride != null
            ? AlvoOverride
            : (PlayerStats.MaisProximoTransform(pos) ?? playerTransform);

        Vector2Int c = MundoParaCelula(pos);
        if (!Valido(c))
            return alvoFallback != null ? ((Vector2)alvoFallback.position - pos).normalized : Vector2.zero;

        Vector2 v = vetores[c.x, c.y];
        if (v.sqrMagnitude < 0.01f && alvoFallback != null)
            return ((Vector2)alvoFallback.position - pos).normalized;
        return v;
    }

    Vector2 CelulaPosicao(int x, int y) =>
        gridOrigem + new Vector2((x + 0.5f) * tamanhoCelula, (y + 0.5f) * tamanhoCelula);

    Vector2Int MundoParaCelula(Vector2 p) => new Vector2Int(
        Mathf.FloorToInt((p.x - gridOrigem.x) / tamanhoCelula),
        Mathf.FloorToInt((p.y - gridOrigem.y) / tamanhoCelula));

    bool Valido(Vector2Int c) =>
        c.x >= 0 && c.y >= 0 && c.x < gridTamanho.x && c.y < gridTamanho.y;

    void OnDrawGizmos()
    {
        if (vetores == null) return;
        for (int x = 0; x < gridTamanho.x; x++)
            for (int y = 0; y < gridTamanho.y; y++)
            {
                Vector2 c = CelulaPosicao(x, y);
                if (!caminhavel[x, y])
                {
                    Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
                    Gizmos.DrawCube(c, Vector3.one * tamanhoCelula * 0.9f);
                    continue;
                }
                if (vetores[x, y].sqrMagnitude < 0.01f) continue;
                Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.5f);
                Gizmos.DrawRay(c, vetores[x, y] * tamanhoCelula * 0.4f);
            }
    }
}
