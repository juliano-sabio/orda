using UnityEngine;

public class LightPickup : MonoBehaviour
{
    [Header("Luz")]
    public float duracao = 5f;

    [Header("Movimento de Atração")]
    public float moveSpeed = 5f;

    [Header("Efeitos")]
    public ParticleSystem collectParticles;
    public AudioClip collectSound;

    private Transform player;
    private PlayerStats playerStats;
    private bool isAttracted = false;
    private bool collected = false;

    void Start()
    {
        if (NetSpawn.EmRede)
        {
            var t = PlayerStats.MaisProximoTransform(transform.position); // co-op: player mais próximo, não arbitrário
            player = t; playerStats = t != null ? t.GetComponent<PlayerStats>() : null;
        }
        else
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player != null) playerStats = player.GetComponent<PlayerStats>();
        }
    }

    void Update()
    {
        if (NetSpawn.EmRede)
        {
            var t = PlayerStats.MaisProximoTransform(transform.position);
            if (t != null) { player = t; playerStats = t.GetComponent<PlayerStats>(); }
        }

        if (!isAttracted)
            CheckDistance();
        else
            MoveToPlayer();
    }

    void CheckDistance()
    {
        if (playerStats == null || player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= playerStats.GetItemCollectionRadius())
            isAttracted = true;
    }

    void MoveToPlayer()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        float speed = moveSpeed * Mathf.Lerp(4f, 1f, distance / playerStats.GetItemCollectionRadius());

        Vector3 direction = (player.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        if (distance < 0.5f)
            Collect();
    }

    void Collect()
    {
        if (collected || playerStats == null) return;
        collected = true;

        // Co-op: o modo "coleta de luz" vai pro DONO do player que coletou.
        if (NetSpawn.EmRede)
        {
            var pn = playerStats.GetComponent<PlayerNet>();
            if (pn != null) pn.ColetaLuzOwnerRpc(duracao);
            else playerStats.GetComponent<PlayerCollectLight>()?.Ativar(duracao);
        }
        else
        {
            playerStats.GetComponent<PlayerCollectLight>()?.Ativar(duracao);
        }

        if (collectSound != null)
            AudioSource.PlayClipAtPoint(collectSound, transform.position);

        if (collectParticles != null)
            Instantiate(collectParticles, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            Collect();
    }
}
