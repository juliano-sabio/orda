using UnityEngine;
using System.Collections;

public class movi_inimigo_manter_distancia : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidade = 3f;
    public float suavizacao = 8f;

    [Header("Manter Distância")]
    public float distanciaDesejada = 4f;
    public float toleranciaDistancia = 0.5f;
    public float distanciaFuga = 2.5f;
    public float velocidadeFuga = 4f;
    public bool orbitarAoRedor = true;
    public float forcaOrbital = 1.5f;

    [Header("Ataque")]
    public GameObject prefabProjetil;
    public Transform pontoDisparo;
    public float intervaloAtaque = 2f;
    public float distanciaAtaque = 5f;
    public float forcaDisparo = 8f;
    public bool podeAtirar = true;
    public float tempoRecargaAposFuga = 1f;

    [Header("Referências")]
    public Transform player;

    private Rigidbody2D rb;
    private Vector2 direcaoMovimento;
    private bool procurandoPlayer = false;
    private bool estaMuitoPerto = false;
    private float distanciaAtual;

    private float tempoUltimoAtaque;
    private float tempoUltimaFuga;
    private bool podeAtirarAgora = true;

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
        if (player == null && !procurandoPlayer) { EncontrarPlayer(); return; }
        if (player == null) return;

        distanciaAtual = Vector2.Distance(transform.position, player.position);

        Vector2 dir = CalcularDirecao();
        direcaoMovimento = Vector2.Lerp(direcaoMovimento, dir, Time.deltaTime * suavizacao);

        if (podeAtirar)
        {
            if (Time.time >= tempoUltimaFuga + tempoRecargaAposFuga)
                podeAtirarAgora = true;

            if (podeAtirarAgora && distanciaAtual <= distanciaAtaque &&
                Time.time >= tempoUltimoAtaque + intervaloAtaque)
            {
                AtirarNoPlayer();
                tempoUltimoAtaque = Time.time;
            }
        }
    }

    Vector2 CalcularDirecao()
    {
        Vector2 dirParaPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
        bool eraProximo = estaMuitoPerto;
        estaMuitoPerto = distanciaAtual < distanciaFuga;

        if (estaMuitoPerto && !eraProximo)
        {
            tempoUltimaFuga = Time.time;
            podeAtirarAgora = false;
        }

        // Fuga direta (sem FlowField — precisa sair rápido)
        if (estaMuitoPerto)
            return -dirParaPlayer;

        // Precisa se aproximar: usa FlowField para navegar até o player
        if (distanciaAtual > distanciaDesejada + toleranciaDistancia)
        {
            return FlowField.Instance != null
                ? FlowField.Instance.ObterDirecao(transform.position)
                : dirParaPlayer;
        }

        // Precisa recuar: afasta direto
        if (distanciaAtual < distanciaDesejada - toleranciaDistancia)
            return -dirParaPlayer;

        // Na distância ideal: orbita lateralmente
        if (orbitarAoRedor)
        {
            Vector2 lateral = new Vector2(-dirParaPlayer.y, dirParaPlayer.x);
            float t = Mathf.Sin(Time.time * 0.8f);
            Vector2 orb = (lateral * t * forcaOrbital).normalized;
            return orb == Vector2.zero ? lateral : orb;
        }

        return Vector2.zero;
    }

    void FixedUpdate()
    {
        if (player == null) return;

        float vel = estaMuitoPerto ? velocidadeFuga : velocidade;
        rb.linearVelocity = direcaoMovimento.sqrMagnitude > 0.001f
            ? direcaoMovimento.normalized * vel
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

    void AtirarNoPlayer()
    {
        if (prefabProjetil == null || pontoDisparo == null) return;

        Vector2 direcao = ((Vector2)player.position - (Vector2)pontoDisparo.position).normalized;
        GameObject projetilObj = Instantiate(prefabProjetil, pontoDisparo.position, Quaternion.identity);
        projetil_inimigo projetil = projetilObj.GetComponent<projetil_inimigo>();

        if (projetil != null)
            projetil.SetDirecao(direcao);
        else
        {
            Rigidbody2D projetilRb = projetilObj.GetComponent<Rigidbody2D>();
            if (projetilRb != null) projetilRb.linearVelocity = direcao * forcaDisparo;
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

    void OnDrawGizmos()
    {
        if (player == null) return;
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, distanciaDesejada);
        Gizmos.color = estaMuitoPerto ? Color.red : Color.yellow;
        Gizmos.DrawLine(transform.position, player.position);
    }
}
