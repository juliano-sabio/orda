using UnityEngine;

public class ImaPickup : MonoBehaviour
{
    [Header("Bônus")]
    public float xpRadiusBonus = 2f;

    [Header("Movimento de Atração")]
    public float moveSpeed = 5f;

    [Header("Efeitos")]
    public AudioClip collectSound;

    [Header("Flutuação")]
    public float floatAmplitude = 0.2f;
    public float floatSpeed = 2f;

    private Transform player;
    private PlayerStats playerStats;
    private bool isAttracted = false;
    private bool collected = false;
    private Vector3 startPosition;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player != null)
            playerStats = player.GetComponent<PlayerStats>();
        startPosition = transform.position;
    }

    void Update()
    {
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

        playerStats.xpCollectionRadius += xpRadiusBonus;

        if (collectSound != null)
            AudioSource.PlayClipAtPoint(collectSound, transform.position);

        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            Collect();
    }
}
