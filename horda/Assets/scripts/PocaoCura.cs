using UnityEngine;

public class PocaoCura : MonoBehaviour
{
    [Header("Cura")]
    public float quantidadeCura = 30f;
    [Range(0f, 1f)]
    public float curaEmPorcentagem = 0f;

    [Header("Atração")]
    public float raioAtracao = 4f;
    public float velocidadeAtracao = 6f;
    public float aceleracao = 4f;

    [Header("Visual")]
    public float bobAmplitude = 0.15f;
    public float bobVelocidade = 2f;

    private Transform player;
    private bool atraindo = false;
    private float velocidadeAtual = 0f;
    private Vector3 posicaoBase;

    void Start()
    {
        posicaoBase = transform.position;

        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) player = go.transform;
    }

    void Update()
    {
        if (player == null) return;

        float distancia = Vector2.Distance(transform.position, player.position);

        if (!atraindo && distancia <= raioAtracao)
            atraindo = true;

        if (atraindo)
        {
            velocidadeAtual = Mathf.MoveTowards(velocidadeAtual, velocidadeAtracao, aceleracao * Time.deltaTime);
            transform.position = Vector2.MoveTowards(transform.position, player.position, velocidadeAtual * Time.deltaTime);
        }
        else
        {
            // bob enquanto espera
            posicaoBase.x = transform.position.x;
            float y = posicaoBase.y + Mathf.Sin(Time.time * bobVelocidade) * bobAmplitude;
            transform.position = new Vector3(posicaoBase.x, y, posicaoBase.z);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerStats stats = other.GetComponent<PlayerStats>();
        if (stats == null) return;

        float cura = curaEmPorcentagem > 0f
            ? stats.maxHealth * curaEmPorcentagem
            : quantidadeCura;

        stats.Heal(cura);
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, raioAtracao);
    }
}
