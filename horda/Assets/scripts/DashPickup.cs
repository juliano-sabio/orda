using UnityEngine;
using Unity.Netcode;

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

    // Em co-op é NetworkObject: host atrai/coleta (autoritativo), cliente é fantoche.
    bool EhClienteFantoche
    {
        get { var nm = NetworkManager.Singleton; return nm != null && nm.IsListening && !nm.IsServer; }
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform; // coop-local-ok: caminho SP (co-op usa MaisProximo no Update)
        if (player != null)
            playerStats = player.GetComponent<PlayerStats>();
        col = GetComponent<Collider2D>();
        startPosition = transform.position;
    }

    void Update()
    {
        if (EhClienteFantoche) return; // cliente: NetworkTransform move

        if (NetSpawn.EmRede)
        {
            var t = PlayerStats.MaisProximoTransform(transform.position);
            if (t != null) { player = t; playerStats = t.GetComponent<PlayerStats>(); }
        }

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
        if (collected) return;
        if (EhClienteFantoche) return; // só o host coleta em co-op
        if (playerStats == null) return;
        if (playerStats.dashCharges >= playerStats.maxDashCharges) return;

        collected = true;

        // Co-op: carga de dash vai pro DONO do player que coletou.
        if (NetSpawn.EmRede)
        {
            var pn = playerStats.GetComponent<PlayerNet>();
            if (pn != null) pn.DashChargeOwnerRpc();
            else playerStats.AddDashCharge();
            Efeitos();
            NetSpawn.Despawnar(gameObject);
            return;
        }

        playerStats.AddDashCharge();
        Efeitos();
        Destroy(gameObject);
    }

    void Efeitos()
    {
        if (collectSound != null)
            AudioBus.PlaySfx(collectSound, transform.position);
        if (collectParticles != null)
            Instantiate(collectParticles, transform.position, Quaternion.identity);
    }

    void OnDestroy()
    {
        // Pop de coleta (azul-dash), nos dois lados em co-op (host e cliente despawnam o pickup).
        if (!gameObject.scene.isLoaded) return;
        XpColetaVFX.Tocar(transform.position, new Color(0.4f, 0.85f, 1f));
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            Collect();
    }
}
