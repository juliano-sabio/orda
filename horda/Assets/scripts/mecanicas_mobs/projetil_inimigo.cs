using UnityEngine;
using System.Collections;

public class projetil_inimigo : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidade = 8f;
    public Vector2 direcao;
    public float tempoMaximoVida = 5f; // Destrói após esse tempo se não acertar nada

    [Header("Efeito de Lentidão")]
    public float duracaoLentidao = 3f;
    public float fatorLentidao = 0.5f; // 0.5 = metade da velocidade
    public Color corEfeito = new Color(0.5f, 1f, 0.5f, 1f); // Verde claro

    [Header("Vinhas (Partículas)")]
    public GameObject prefabVinhas; // Prefab com sistema de partículas
    public float duracaoVinhas = 2f; // Quanto tempo as vinhas ficam no player
    public bool vinhasSeguemPlayer = true; // Se as vinhas seguem ou ficam no local

    [Header("Visual")]
    public SpriteRenderer spriteRenderer;
    public Animator animator; // Opcional, para animação do projétil
    public bool rotacionar = true; // Se o projétil rotaciona enquanto voa
    public float velocidadeRotacao = 360f; // Graus por segundo

    [Header("Áudio")]
    public AudioClip somImpacto;
    public AudioSource audioSource;

    [Header("Efeito de Charge")]
    public bool temEfeitoCharge = false;
    public float tempoCharge = 0.5f;
    public Color corCharge = Color.yellow;
    public int piscadasCharge = 3;

    private Rigidbody2D rb;
    private bool jaAcertou = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Se tiver efeito de charge, executa antes de configurar o movimento
        if (temEfeitoCharge)
        {
            StartCoroutine(EfeitoCharge());
        }
        else
        {
            ConfigurarMovimento();
        }

        // Destrói após tempo máximo
        Destroy(gameObject, tempoMaximoVida);
    }

    void ConfigurarMovimento()
    {
        // Configura velocidade inicial
        if (rb != null)
        {
            rb.linearVelocity = direcao * velocidade;
        }

        // Rotaciona para a direção do movimento
        if (rotacionar)
        {
            float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angulo, Vector3.forward);
        }
    }

    IEnumerator EfeitoCharge()
    {
        if (spriteRenderer == null) yield break;

        Color corOriginal = spriteRenderer.color;

        for (int i = 0; i < piscadasCharge; i++)
        {
            spriteRenderer.color = corCharge;
            yield return new WaitForSeconds(tempoCharge / (piscadasCharge * 2));
            spriteRenderer.color = corOriginal;
            yield return new WaitForSeconds(tempoCharge / (piscadasCharge * 2));
        }

        // Após o charge, configura o movimento
        ConfigurarMovimento();
    }

    void Update()
    {
        // Rotação contínua (opcional)
        if (rotacionar && rb != null && rb.linearVelocity.magnitude > 0.1f)
        {
            transform.Rotate(0, 0, velocidadeRotacao * Time.deltaTime);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (jaAcertou) return;

        if (other.CompareTag("Player"))
        {
            jaAcertou = true;

            // Aplica efeito de lentidão
            AplicarLentidao(other.gameObject);

            // Cria vinhas no player
            CriarVinhas(other.gameObject);

            // Toca som de impacto
            TocarSomImpacto();

            // Destrói o projétil
            Destroy(gameObject);
        }
        else if (other.CompareTag("Chao") || other.CompareTag("Obstaculo"))
        {
            // Opcional: destruir ao bater em paredes/chão
            Destroy(gameObject);
        }
    }

    void AplicarLentidao(GameObject player)
    {
        // Procura ou adiciona o componente de lentidão no player
        EfeitoLentidao efeito = player.GetComponent<EfeitoLentidao>();
        if (efeito == null)
        {
            efeito = player.AddComponent<EfeitoLentidao>();
        }

        // Aplica o efeito
        efeito.AplicarLentidao(duracaoLentidao, fatorLentidao, corEfeito);

        Debug.Log($"🐌 Lentidão aplicada ao player por {duracaoLentidao}s");
    }

    void CriarVinhas(GameObject player)
    {
        if (prefabVinhas == null)
        {
            Debug.LogWarning("⚠️ Prefab das vinhas não atribuído!");
            return;
        }

        GameObject vinhas;

        if (vinhasSeguemPlayer)
        {
            // Vinhas seguem o player
            vinhas = Instantiate(prefabVinhas, player.transform.position, Quaternion.identity, player.transform);
        }
        else
        {
            // Vinhas ficam no local do impacto
            vinhas = Instantiate(prefabVinhas, transform.position, Quaternion.identity);
        }

        // Configura e destrói após a duração
        VinhasController controller = vinhas.GetComponent<VinhasController>();
        if (controller != null)
        {
            controller.Iniciar(duracaoVinhas);
        }
        else
        {
            // Se não tiver controller, só destrói depois do tempo
            Destroy(vinhas, duracaoVinhas);
        }
    }

    void TocarSomImpacto()
    {
        if (somImpacto != null)
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(somImpacto);
            }
            else
            {
                // Cria um audio source temporário
                AudioSource.PlayClipAtPoint(somImpacto, transform.position);
            }
        }
    }

    // Método para definir a direção (chamado pelo inimigo)
    public void SetDirecao(Vector2 novaDirecao)
    {
        direcao = novaDirecao.normalized;

        if (rb != null && !temEfeitoCharge) // Só aplica velocidade se não tiver efeito de charge
        {
            rb.linearVelocity = direcao * velocidade;
        }
    }

    // Gizmos para debug
    void OnDrawGizmosSelected()
    {
        // Desenha a direção do projétil
        if (direcao != Vector2.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, direcao * 0.5f);
        }

        // Desenha área de colisão aproximada
        Gizmos.color = Color.yellow;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            if (col is BoxCollider2D)
            {
                BoxCollider2D box = (BoxCollider2D)col;
                Gizmos.DrawWireCube(transform.position + (Vector3)box.offset, box.size);
            }
            else if (col is CircleCollider2D)
            {
                CircleCollider2D circle = (CircleCollider2D)col;
                Gizmos.DrawWireSphere(transform.position + (Vector3)circle.offset, circle.radius);
            }
        }
    }
}