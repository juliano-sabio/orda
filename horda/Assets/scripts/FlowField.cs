using System.Collections.Generic;
using UnityEngine;

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
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        ConstruirGrid();
        if (playerTransform != null) Recalcular();
        InvokeRepeating(nameof(Tick), intervaloAtualizacao, intervaloAtualizacao);
    }

    void ConstruirGrid()
    {
        Vector2 centro = Camera.main != null ? (Vector2)Camera.main.transform.position : Vector2.zero;
        ConstruirGridEmTorno(centro);
    }

    void ConstruirGridEmTorno(Vector2 centro)
    {
        float h = 100f, w = 100f;
        if (Camera.main != null)
        {
            Camera cam = Camera.main;
            h = cam.orthographicSize * 2f + padding * 2f;
            w = h * cam.aspect + padding * 2f;
        }

        gridOrigem = centro - new Vector2(w / 2f, h / 2f);
        gridCentroAtual = centro;
        gridTamanho = new Vector2Int(
            Mathf.CeilToInt(w / tamanhoCelula),
            Mathf.CeilToInt(h / tamanhoCelula));

        vetores    = new Vector2[gridTamanho.x, gridTamanho.y];
        caminhavel = new bool[gridTamanho.x, gridTamanho.y];

        Vector2 metadeCelula = Vector2.one * (tamanhoCelula * 0.85f);
        for (int x = 0; x < gridTamanho.x; x++)
            for (int y = 0; y < gridTamanho.y; y++)
            {
                Vector2 pos = CelulaPosicao(x, y);
                caminhavel[x, y] = !Physics2D.OverlapBox(pos, metadeCelula, 0f, camadaObstaculos);
            }
    }

    void Tick()
    {
        if (playerTransform == null)
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

        Transform alvo = AlvoOverride != null ? AlvoOverride : playerTransform;
        if (alvo == null) return;

        if (caminhavel == null
            || (!Valido(MundoParaCelula(alvo.position))
                && Time.time - tempoUltimaReconstrucao >= intervaloReconstrucao))
        {
            ConstruirGridEmTorno(alvo.position);
            tempoUltimaReconstrucao = Time.time;
        }

        Recalcular();
    }

    void Recalcular()
    {
        if (caminhavel == null) return;
        Transform alvo = AlvoOverride != null ? AlvoOverride : playerTransform;
        if (alvo == null) return;
        Vector2Int dest = MundoParaCelula(alvo.position);
        if (!Valido(dest)) return;

        // BFS a partir do player — propaga custo para toda a grade
        int[,] custo = new int[gridTamanho.x, gridTamanho.y];
        for (int x = 0; x < gridTamanho.x; x++)
            for (int y = 0; y < gridTamanho.y; y++)
                custo[x, y] = int.MaxValue;

        custo[dest.x, dest.y] = 0;
        var fila = new Queue<Vector2Int>();
        fila.Enqueue(dest);

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

    // Retorna a direção ótima para o alvo atual (AlvoOverride ou player) a partir de uma posição do mundo
    public Vector2 ObterDirecao(Vector2 pos)
    {
        Transform alvoFallback = AlvoOverride != null ? AlvoOverride : playerTransform;

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
