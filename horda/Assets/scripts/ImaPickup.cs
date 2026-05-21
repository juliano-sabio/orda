using UnityEngine;
using UnityEngine.Rendering.Universal;

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
    private Light2D luz;

    void Awake()
    {
        if (!TryGetComponent<Light2D>(out luz))
            luz = gameObject.AddComponent<Light2D>();
        luz.lightType             = Light2D.LightType.Point;
        luz.intensity             = 2f;
        luz.pointLightOuterRadius = 3f;
        luz.pointLightInnerRadius = 0.6f;
    }

    void Start()
    {
        luz.color = new Color(0.3f, 0.7f, 1f);

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player != null)
            playerStats = player.GetComponent<PlayerStats>();
        startPosition = transform.position;
    }

    void Update()
    {
        if (!isAttracted)
        {
            float sin = Mathf.Sin(Time.time * floatSpeed);
            float offsetY = sin * floatAmplitude;
            transform.position = startPosition + new Vector3(0f, offsetY, 0f);
            if (luz != null)
                luz.intensity = Mathf.Lerp(0.9f, 1.8f, (sin + 1f) * 0.5f);
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
