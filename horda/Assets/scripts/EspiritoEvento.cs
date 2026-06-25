using UnityEngine;
using UnityEngine.Rendering.Universal;

public class EspiritoEvento : MonoBehaviour
{
    [Header("Flutuação")]
    public float amplitudeBob    = 0.25f;
    public float frequenciaBob   = 1.2f;
    public float velocidadeDrift = 1.0f;
    public float intervaloDrift  = 3.0f;

    [Header("Coleta")]
    public float raioAtracao       = 3.5f;
    public float velocidadeAtracao = 7f;

    [Header("Sombra")]
    public float offsetSombra = 0.35f;

    private Transform player;
    private bool atraido;
    private bool coletado;

    // Host roda a lógica (atração/coleta) e move o transform; o NetworkTransform
    // replica a posição pro cliente, que só renderiza o visual (sombra/brilho).
    bool Host => NetSpawn.PodeSpawnar;

    private Vector2 posBase;
    private Vector2 driftDir;
    private float timerDrift;
    private float tempoVida;

    private Transform sombraTransform;
    private SpriteRenderer sombraRenderer;
    private Light2D luzBrilho;

    void Start()
    {
        posBase = transform.position;
        MudarDrift();
        CriarSombra();
        CriarBrilho();
    }

    // Player mais próximo deste espírito (co-op: atrai p/ quem estiver perto).
    Transform AlvoMaisProximo()
    {
        var t = PlayerStats.MaisProximoTransform(transform.position);
        if (t != null) return t;
        var go = GameObject.FindGameObjectWithTag("Player"); // coop-local-ok: fallback (MaisProximo já tentado acima)
        return go != null ? go.transform : null;
    }

    void Update()
    {
        if (coletado) return;
        tempoVida += Time.deltaTime;

        // Gameplay (movimento/coleta) só no host; o cliente recebe a posição pelo NetworkTransform.
        if (Host)
        {
            if (!atraido)
            {
                Flutuar();
                VerificarAtracao();
            }
            else
            {
                Atrair();
            }
        }

        AtualizarSombra();
        AtualizarBrilho();
    }

    void Flutuar()
    {
        timerDrift += Time.deltaTime;
        if (timerDrift >= intervaloDrift)
            MudarDrift();

        Vector2 proxBase = posBase + driftDir * velocidadeDrift * Time.deltaTime;

        var ge = GerenciadorEventos.Instance;
        if (ge != null && !ge.PosicaoValida(proxBase))
            MudarDrift(); // bate em obstáculo ou sai do terreno → nova direção aleatória
        else
            posBase = proxBase;

        float bob = Mathf.Sin(tempoVida * frequenciaBob * Mathf.PI * 2f) * amplitudeBob;
        transform.position = new Vector3(posBase.x, posBase.y + bob, transform.position.z);
    }

    void VerificarAtracao()
    {
        player = AlvoMaisProximo();
        if (player == null) return;
        if (Vector2.Distance(transform.position, player.position) <= raioAtracao)
            atraido = true;
    }

    void Atrair()
    {
        var maisPerto = AlvoMaisProximo();
        if (maisPerto != null) player = maisPerto;
        if (player == null) return;
        Vector3 dir = (player.position - transform.position).normalized;
        transform.position += dir * velocidadeAtracao * Time.deltaTime;
        if (Vector2.Distance(transform.position, player.position) < 0.4f)
            Coletar();
    }

    void MudarDrift()
    {
        driftDir = Random.insideUnitCircle.normalized;
        timerDrift = 0f;
    }

    void CriarSombra()
    {
        int w = 32, h = 16;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = w * 0.5f, cy = h * 0.5f;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = (x - cx) / cx;
                float dy = (y - cy) / cy;
                float d  = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                float a  = (1f - d) * (1f - d) * 0.55f;
                tex.SetPixel(x, y, new Color(0f, 0f, 0f, a));
            }
        tex.Apply();

        Sprite spr = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);

        var go = new GameObject("sombra");
        sombraTransform = go.transform;
        sombraTransform.position = (Vector2)transform.position - Vector2.up * offsetSombra;

        sombraRenderer = go.AddComponent<SpriteRenderer>();
        sombraRenderer.sprite = spr;
        sombraRenderer.sortingLayerName = "Default";
        sombraRenderer.sortingOrder = 4;
    }

    void AtualizarSombra()
    {
        if (sombraTransform == null) return;

        // Base no chão: remove o "bob" da posição (sincronizada no client via NetworkTransform).
        float bobOff = Mathf.Sin(tempoVida * frequenciaBob * Mathf.PI * 2f) * amplitudeBob;
        float baseY  = atraido ? transform.position.y : transform.position.y - bobOff;
        sombraTransform.position = new Vector3(transform.position.x, baseY - offsetSombra, transform.position.z + 0.01f);

        float bob  = Mathf.Sin(tempoVida * frequenciaBob * Mathf.PI * 2f);
        float t    = (bob + 1f) * 0.5f;
        float esc  = Mathf.Lerp(0.9f, 0.55f, t);
        sombraTransform.localScale = new Vector3(esc, esc * 0.28f, 1f);
    }

    void CriarBrilho()
    {
        var go = new GameObject("brilho");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0.37f, 0f);

        luzBrilho = go.AddComponent<Light2D>();
        luzBrilho.lightType             = Light2D.LightType.Point;
        luzBrilho.color                 = new Color(0.65f, 0.1f, 1f);
        luzBrilho.intensity             = 1.2f;
        luzBrilho.pointLightOuterRadius = 2.4f;
        luzBrilho.pointLightInnerRadius = 0.2f;
    }

    void AtualizarBrilho()
    {
        if (luzBrilho == null) return;
        float pulse = 1f + Mathf.Sin(tempoVida * 2.5f) * 0.18f;
        luzBrilho.intensity = 1.2f * pulse;
    }

    void Coletar()
    {
        if (!Host) return;          // só o host coleta (autoridade)
        if (coletado) return;
        coletado = true;
        if (sombraTransform != null) Destroy(sombraTransform.gameObject);
        GerenciadorEventos.Instance?.RegistrarEspiritoColetado();
        NetSpawn.Despawnar(gameObject); // remove em todos os clientes
    }

    void OnDestroy()
    {
        if (sombraTransform != null) Destroy(sombraTransform.gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!Host) return;          // a coleta por contato é decidida no host
        if (other.CompareTag("Player"))
            Coletar();
    }
}
