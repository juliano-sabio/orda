using UnityEngine;
using System.Collections;

public class movi_inimigo : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidade = 3f;
    public float suavizacao = 8f;

    [Header("Referências")]
    public Transform player;

    private Rigidbody2D rb;
    private Vector2 direcaoMovimento;
    private bool procurandoPlayer = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        EncontrarPlayer();
        StartCoroutine(ProcurarPlayerPeriodicamente());
    }

    void Update()
    {
        if (player == null) { if (!procurandoPlayer) EncontrarPlayer(); return; }

        // FlowField dá a direção ótima desviando de obstáculos;
        // fallback para linha reta se o FlowField não estiver na cena
        Vector2 dir = FlowField.Instance != null
            ? FlowField.Instance.ObterDirecao(transform.position)
            : ((Vector2)player.position - (Vector2)transform.position).normalized;

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
