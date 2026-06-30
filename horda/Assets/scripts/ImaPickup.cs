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

        if (NetSpawn.EmRede)
        {
            var t = PlayerStats.MaisProximoTransform(transform.position); // co-op: player mais próximo, não arbitrário
            player = t; playerStats = t != null ? t.GetComponent<PlayerStats>() : null;
        }
        else
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform; // coop-local-ok: caminho SP (co-op usa MaisProximo)
            if (player != null) playerStats = player.GetComponent<PlayerStats>();
        }
        startPosition = transform.position;
    }

    void Update()
    {
        if (NetSpawn.EmRede)
        {
            var t = PlayerStats.MaisProximoTransform(transform.position);
            if (t != null) { player = t; playerStats = t.GetComponent<PlayerStats>(); }
        }

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

        // Co-op: o bônus de raio vai pro DONO do player que coletou.
        if (NetSpawn.EmRede)
        {
            var pn = playerStats.GetComponent<PlayerNet>();
            if (pn != null) pn.AumentarRaioXpOwnerRpc(xpRadiusBonus);
            else playerStats.xpCollectionRadius += xpRadiusBonus;
        }
        else
        {
            playerStats.xpCollectionRadius += xpRadiusBonus;
        }

        if (collectSound != null)
            AudioBus.PlaySfx(collectSound, transform.position);

        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            Collect();
    }
}
