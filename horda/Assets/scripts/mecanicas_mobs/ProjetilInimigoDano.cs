using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ProjetilInimigoDano : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    public float velocidade = 10f;
    public float tempoMaximoVida = 5f;

    [Header("Configurações de Dano")]
    public float dano = 10f;

    [Header("Ajuste de Visual")]
    [Range(-360, 360)]
    public float ajusteAngular = 0f;

    [Header("Luz")]
    public bool usarLuz = true;
    public Color corLuz = new Color(0.4f, 0f, 1f);
    public float intensidadeLuz = 1.8f;
    public float raioExternoLuz = 2.5f;

    [HideInInspector] public bool redirecionado = false;

    private Rigidbody2D rb;
    private bool jaAcertou = false;
    private Light2D luz2D;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (usarLuz)
        {
            luz2D = gameObject.AddComponent<Light2D>();
            luz2D.lightType = Light2D.LightType.Point;
            luz2D.intensity = intensidadeLuz;
            luz2D.pointLightOuterRadius = raioExternoLuz;
            luz2D.pointLightInnerRadius = raioExternoLuz * 0.3f;
        }
    }

    void Start()
    {
        // Cor aplicada no Start para não ser sobrescrita pelo Awake interno do Light2D
        if (luz2D != null)
            luz2D.color = corLuz;

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

        if (redirecionado)
        {
            var ic = other.GetComponent<InimigoController>() ?? other.GetComponentInParent<InimigoController>();
            if (ic != null)
            {
                jaAcertou = true;
                ic.ReceberDano(dano);
                Destroy(gameObject);
            }
            else if (other.CompareTag("chao") || other.CompareTag("obstacles"))
                Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Player"))
        {
            jaAcertou = true;
            PlayerStats stats = other.GetComponent<PlayerStats>();
            if (stats != null) stats.TakeDamage(dano);
            Destroy(gameObject);
        }
        else if (other.gameObject.tag == "chao" || other.gameObject.tag == "obstacles")
        {
            Destroy(gameObject);
        }
    }
}