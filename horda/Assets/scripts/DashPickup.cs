using UnityEngine;

public class DashPickup : MonoBehaviour
{
    [Header("Movimento de Atração")]
    public float moveSpeed = 5f;

    [Header("Efeitos")]
    public ParticleSystem collectParticles;
    public AudioClip collectSound;

    [Header("Flutuação")]
    public float floatAmplitude = 0.2f;
    public float floatSpeed = 2f;

    private Transform player;
    private PlayerStats playerStats;
    private Collider2D col;
    private bool isAttracted = false;
    private bool collected = false;
    private Vector3 startPosition;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player != null)
            playerStats = player.GetComponent<PlayerStats>();
        col = GetComponent<Collider2D>();
        startPosition = transform.position;
    }

    void Update()
    {
        if (playerStats != null && col != null)
            col.enabled = playerStats.dashCharges < playerStats.maxDashCharges;

        if (!isAttracted)
        {
            float offsetY = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.position = startPosition + new Vector3(0f, offsetY, 0f);
            CheckDistance();
        }
        else
        {
            MoveToPlayer();
        }
    }

    void CheckDistance()
    {
        if (playerStats == null || player == null) return;
        if (playerStats.dashCharges >= playerStats.maxDashCharges) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= playerStats.GetItemCollectionRadius())
            isAttracted = true;
    }

    void MoveToPlayer()
    {
        if (player == null) return;

        if (playerStats.dashCharges >= playerStats.maxDashCharges)
        {
            isAttracted = false;
            return;
        }

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
        if (playerStats.dashCharges >= playerStats.maxDashCharges) return;

        collected = true;

        playerStats.AddDashCharge();

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
