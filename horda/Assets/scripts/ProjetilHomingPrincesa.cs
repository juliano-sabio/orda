using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ProjetilHomingPrincesa : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidade      = 6f;
    public float velocidadeMaxima = 10f;
    public float fatorAceleracao = 2f;
    public float duracaoVida     = 8f;

    [Header("Dano")]
    public float dano = 20f;

    private Transform player;
    private float timerVida;
    private bool ativo;
    private Rigidbody2D rb;
    private Light2D luz;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Luz roxa
        luz = gameObject.AddComponent<Light2D>();
        luz.lightType             = Light2D.LightType.Point;
        luz.color                 = new Color(0.8f, 0.3f, 1f);
        luz.intensity             = 1.2f;
        luz.pointLightOuterRadius = 0.6f;
        luz.pointLightInnerRadius = 0.2f;
    }

    public void Iniciar(Transform alvoPlayer)
    {
        player    = alvoPlayer;
        timerVida = duracaoVida;
        ativo     = true;
    }

    void Update()
    {
        if (!ativo) return;

        timerVida -= Time.deltaTime;

        // Pisca nos últimos 2 segundos
        if (timerVida < 2f)
        {
            float pisca = Mathf.PingPong(Time.time * 6f, 1f);
            if (luz != null) luz.intensity = Mathf.Lerp(0.3f, 1.2f, pisca);
        }

        if (timerVida <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (player == null) return;

        Vector2 direcao = ((Vector2)player.position - rb.position).normalized;
        rb.linearVelocity = Vector2.MoveTowards(
            rb.linearVelocity,
            direcao * velocidadeMaxima,
            fatorAceleracao * Time.deltaTime * 60f
        );
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!ativo) return;
        if (!other.CompareTag("Player")) return;

        var ps = other.GetComponent<PlayerStats>();
        if (ps != null) ps.TakeDamage(dano);

        Destroy(gameObject);
    }
}
