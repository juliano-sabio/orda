using UnityEngine;
using UnityEngine.Rendering.Universal;
using Unity.Netcode;

public class RadiusBoostPickup : MonoBehaviour
{
    [Header("Configurações do Boost")]
    public float radiusBoostAmount = 5f;
    public float orbSpeedMultiplier = 3f;
    public float boostDuration = 5f;

    [Header("Movimento de Atração")]
    public float moveSpeed = 5f;
    [Tooltip("Distância para o ima começar a voar em direção ao player")]
    public float raioAtracao = 6f;

    [Header("Flutuação")]
    public float floatAmplitude = 0.15f;
    public float floatSpeed     = 2f;

    [Header("Efeitos")]
    public ParticleSystem collectParticles;
    public AudioClip collectSound;

    private Transform player;
    private PlayerStats playerStats;
    private bool isAttracted = false;
    private bool collected = false;
    private Vector3 startPosition;
    private Light2D luz;
    private GameObject luzGO;

    // Em co-op é NetworkObject: host atrai/coleta (autoritativo), cliente é fantoche.
    bool EhClienteFantoche
    {
        get { var nm = NetworkManager.Singleton; return nm != null && nm.IsListening && !nm.IsServer; }
    }

    void Awake()
    {
        luzGO = new GameObject("Luz");
        luzGO.transform.SetParent(transform);
        luzGO.transform.localPosition = Vector3.zero;

        luz = luzGO.AddComponent<Light2D>();
        luz.lightType             = Light2D.LightType.Point;
        luz.intensity             = 1.4f;
        luz.pointLightOuterRadius = 0.8f;
        luz.pointLightInnerRadius = 0.4f;
    }

    void Start()
    {
        luz.color = new Color(0.6f, 0.2f, 1f);

        // Centraliza a luz no centro visual da sprite
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
            luzGO.transform.localPosition = sr.sprite.bounds.center;

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player != null)
            playerStats = player.GetComponent<PlayerStats>();

        startPosition = transform.position;
    }

    void Update()
    {
        float sin = Mathf.Sin(Time.time * floatSpeed);
        if (luz != null)
            luz.intensity = Mathf.Lerp(1.0f, 1.8f, (sin + 1f) * 0.5f);

        if (EhClienteFantoche) return; // cliente: NetworkTransform move

        if (NetSpawn.EmRede)
        {
            var t = PlayerStats.MaisProximoTransform(transform.position);
            if (t != null) { player = t; playerStats = t.GetComponent<PlayerStats>(); }
        }

        if (!isAttracted)
        {
            // Flutuação — a luz segue automaticamente por ser filha
            transform.position = startPosition + new Vector3(0f, sin * floatAmplitude, 0f);
            CheckDistance();
        }
        else
        {
            MoveToPlayer();
        }
    }

    void CheckDistance()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= raioAtracao)
            isAttracted = true;
    }

    void MoveToPlayer()
    {
        if (player == null) return;

        float distance  = Vector3.Distance(transform.position, player.position);
        float speed     = moveSpeed * Mathf.Lerp(4f, 1f, distance / raioAtracao);
        Vector3 dir     = (player.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;

        if (distance < 0.5f)
            Collect();
    }

    void Collect()
    {
        if (collected) return;
        if (EhClienteFantoche) return; // só o host coleta em co-op
        if (playerStats == null) return;
        collected = true;

        // Co-op: o boost vai pro DONO do player que coletou.
        if (NetSpawn.EmRede)
        {
            var pn = playerStats.GetComponent<PlayerNet>();
            if (pn != null) pn.BoostColetaOwnerRpc(radiusBoostAmount, orbSpeedMultiplier, boostDuration);
            else { playerStats.BoostCollectionRadius(radiusBoostAmount, boostDuration); playerStats.BoostOrbSpeed(orbSpeedMultiplier, boostDuration); }
            Efeitos();
            NetSpawn.Despawnar(gameObject);
            return;
        }

        playerStats.BoostCollectionRadius(radiusBoostAmount, boostDuration);
        playerStats.BoostOrbSpeed(orbSpeedMultiplier, boostDuration);
        Efeitos();
        Destroy(gameObject);
    }

    void Efeitos()
    {
        if (collectSound != null)
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        if (collectParticles != null)
            Instantiate(collectParticles, transform.position, Quaternion.identity);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            Collect();
    }

    void OnDrawGizmos()
    {
        if (player != null)
        {
            Gizmos.color = isAttracted ? Color.cyan : Color.yellow;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}
