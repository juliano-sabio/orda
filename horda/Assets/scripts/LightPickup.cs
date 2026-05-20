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
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player != null)
            playerStats = player.GetComponent<PlayerStats>();
    }

    void Update()
    {
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

        playerStats.GetComponent<PlayerCollectLight>()?.Ativar(duracao);

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
