using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class movi_inimigo : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidade  = 3f;
    public float suavizacao  = 8f;

    [Header("Desvio Local de Paredes")]
    [Tooltip("Raio dos raycasts que repelem o inimigo de paredes próximas")]
    public float raioDesvioLocal = 0.7f;
    [Tooltip("Intensidade da repulsão ao detectar parede")]
    public float forcaDesvio     = 1.4f;

    [Header("Separação entre Inimigos")]
    [Tooltip("Raio para detectar outros inimigos próximos")]
    public float raioSeparacao  = 1.1f;
    [Tooltip("Intensidade do afastamento lateral")]
    public float forcaSeparacao = 0.5f;

    [Header("Referências")]
    public Transform player;

    private Rigidbody2D rb;
    private Vector2 direcaoMovimento;
    private bool procurandoPlayer = false;

    private static readonly List<Collider2D> _buf = new List<Collider2D>(12);

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.constraints           = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale          = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        EncontrarPlayer();
        StartCoroutine(ProcurarPlayerPeriodicamente());
    }

    void Update()
    {
        if (player == null) { if (!procurandoPlayer) EncontrarPlayer(); return; }

        Transform alvo = FlowField.AlvoOverride != null ? FlowField.AlvoOverride : player;
        Vector2 dir = FlowField.Instance != null
            ? FlowField.Instance.ObterDirecao(transform.position)
            : (alvo != null ? ((Vector2)alvo.position - (Vector2)transform.position).normalized : Vector2.zero);

        dir = AplicarDesvioLocal(dir);
        dir = AplicarSeparacao(dir);

        direcaoMovimento = Vector2.Lerp(direcaoMovimento, dir, Time.deltaTime * suavizacao);
    }

    void FixedUpdate()
    {
        if (player == null) return;

        rb.linearVelocity = direcaoMovimento.sqrMagnitude > 0.001f
            ? direcaoMovimento.normalized * velocidade
            : Vector2.zero;

        if (Mathf.Abs(direcaoMovimento.x) > 0.1f)
        {
            float escX = Mathf.Abs(transform.localScale.x);
            transform.localScale = new Vector3(
                Mathf.Sign(direcaoMovimento.x) * escX,
                transform.localScale.y,
                transform.localScale.z);
        }
    }

    // 8 raycasts em volta do inimigo — paredes próximas geram uma força de repulsão
    Vector2 AplicarDesvioLocal(Vector2 dirFlowField)
    {
        if (FlowField.Instance == null) return dirFlowField;
        LayerMask obs = FlowField.Instance.camadaObstaculos;

        Vector2 repulsao = Vector2.zero;
        for (int i = 0; i < 8; i++)
        {
            float ang    = i * 45f * Mathf.Deg2Rad;
            Vector2 raio = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            RaycastHit2D hit = Physics2D.Raycast(transform.position, raio, raioDesvioLocal, obs);
            if (hit.collider != null)
            {
                float peso = 1f - hit.distance / raioDesvioLocal;
                repulsao  -= raio * peso;
            }
        }

        if (repulsao.sqrMagnitude < 0.001f) return dirFlowField;
        return (dirFlowField + repulsao * forcaDesvio).normalized;
    }

    // Afasta inimigos que estejam muito próximos uns dos outros
    Vector2 AplicarSeparacao(Vector2 dir)
    {
        _buf.Clear();
        Physics2D.OverlapCircle(transform.position, raioSeparacao, ContactFilter2D.noFilter, _buf);

        Vector2 sep = Vector2.zero;
        for (int i = 0; i < _buf.Count; i++)
        {
            Collider2D col = _buf[i];
            if (col == null || col.gameObject == gameObject) continue;
            if (col.GetComponent<movi_inimigo>() == null)    continue;

            Vector2 diff = (Vector2)transform.position - (Vector2)col.transform.position;
            float   dist = diff.magnitude;
            if (dist > 0.01f)
                sep += diff.normalized * (1f - dist / raioSeparacao);
        }

        if (sep.sqrMagnitude < 0.001f) return dir;
        return (dir + sep * forcaSeparacao).normalized;
    }

    void EncontrarPlayer()
    {
        procurandoPlayer = true;
        GameObject obj = GameObject.FindGameObjectWithTag("Player");
        if (obj != null) player = obj.transform;
        procurandoPlayer = false;
    }

    IEnumerator ProcurarPlayerPeriodicamente()
    {
        while (true)
        {
            yield return new WaitForSeconds(3f);
            if (player == null) EncontrarPlayer();
        }
    }
}
