using UnityEngine;

public class ProjetilInimigoDano : MonoBehaviour
{
    [Header("Configura��es de Movimento")]
    public float velocidade = 10f;
    public float tempoMaximoVida = 5f;

    [Header("Configura��es de Dano")]
    public float dano = 10f;

    [Header("Ajuste de Visual")]
    [Range(-360, 360)]
    public float ajusteAngular = 0f;

    private Rigidbody2D rb;
    private bool jaAcertou = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        Destroy(gameObject, tempoMaximoVida);
    }

    public void SetDirecao(Vector2 novaDirecao)
    {
        if (rb != null)
        {
            rb.linearVelocity = novaDirecao.normalized * velocidade;
        }
    }

    // Usamos LateUpdate para garantir que a rota��o aconte�a DEPOIS do movimento
    void LateUpdate()
    {
        if (rb != null && rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            // Calcula o �ngulo baseado na velocidade real do objeto
            float angulo = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;

            // Aplica a rota��o somando o seu ajuste
            transform.rotation = Quaternion.Euler(0, 0, angulo + ajusteAngular);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (jaAcertou) return;

        if (other.CompareTag("Player"))
        {
            jaAcertou = true;
            PlayerStats stats = other.GetComponent<PlayerStats>();
            if (stats != null) stats.TakeDamage(dano);
            Destroy(gameObject);
        }
        else if (other.gameObject.tag == "Chao" || other.gameObject.tag == "Obstacles")
        {
            Destroy(gameObject);
        }
    }
}