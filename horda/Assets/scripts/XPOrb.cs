using UnityEngine;
using Unity.Netcode;

public class XPOrb : MonoBehaviour
{
    [Header("Configurações")]
    public float xpValue = 10f;
    public float moveSpeed = 3f;
    public AudioClip collectSound;
    public ParticleSystem collectParticles;

    [Header("Visual")]
    public float rotationSpeed = 100f;

    private Transform player;
    private bool isAttracted = false;
    private bool coletado = false; // evita coleta dupla (MoveToPlayer + OnTrigger no mesmo frame)
    private PlayerStats playerStats;

    // Em co-op o orbe é NetworkObject: roda no host (autoritativo) e é fantoche no cliente
    // (o NetworkTransform move). A coleta soma no pool de XP COMPARTILHADO (CoopProgressao).
    bool EhClienteFantoche
    {
        get
        {
            var nm = NetworkManager.Singleton;
            return nm != null && nm.IsListening && !nm.IsServer;
        }
    }

    void Start()
    {
        // Single-player: alvo fixo achado por tag (em co-op miramos o mais próximo no Update).
        player = GameObject.FindGameObjectWithTag("Player")?.transform; // coop-local-ok: caminho SP (co-op usa MaisProximo no Update)
        if (player != null)
            playerStats = player.GetComponent<PlayerStats>();
    }

    void Update()
    {
        // 1. Rotação visual (em todos)
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

        // Cliente em co-op: fantoche — só o host atrai/coleta.
        if (EhClienteFantoche) return;

        // Co-op (host): mira o player mais próximo (pode mudar a cada frame).
        if (NetSpawn.EmRede)
        {
            var t = PlayerStats.MaisProximoTransform(transform.position);
            if (t != null) { player = t; playerStats = t.GetComponent<PlayerStats>(); }
        }

        // 2. Verificar se deve mover
        if (!isAttracted && player != null)
            CheckDistance();

        // 3. MOVER se atraída
        if (isAttracted && player != null)
            MoveToPlayer();
    }

    void CheckDistance()
    {
        if (playerStats == null) return;

        float distX = player.position.x - transform.position.x;
        float distY = player.position.y - transform.position.y;
        float distance = Mathf.Sqrt(distX * distX + distY * distY);

        if (distance <= playerStats.GetXpCollectionRadius())
        {
            isAttracted = true;
        }
    }

    void MoveToPlayer()
    {
        // MOVIMENTO SIMPLES DIRETO
        Vector3 moveDirection = (player.position - transform.position).normalized;

        // APLICAR MOVIMENTO
        transform.position += moveDirection * moveSpeed * playerStats.orbMoveSpeedMultiplier * Time.deltaTime;

        // Verificar se chegou
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance < 0.5f)
        {
            Collect();
        }
    }

    void Collect()
    {
        if (coletado) return;
        if (EhClienteFantoche) return; // só o host coleta em co-op
        coletado = true;

        // Co-op: soma no pool de XP compartilhado e despawna em todos.
        if (NetSpawn.EmRede)
        {
            if (CoopProgressao.Instance != null) CoopProgressao.Instance.AdicionarXP(xpValue);
            if (collectSound != null) AudioBus.PlaySfx(collectSound, transform.position);
            if (collectParticles != null) Instantiate(collectParticles, transform.position, Quaternion.identity);
            NetSpawn.Despawnar(gameObject);
            return;
        }

        // Single-player: XP vai pro player que coletou.
        if (playerStats != null)
        {
            playerStats.GainXP(xpValue);

            if (collectSound != null)
                AudioBus.PlaySfx(collectSound, transform.position);

            if (collectParticles != null)
                Instantiate(collectParticles, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        // Pop de coleta nos dois lados (host e cliente despawnam o orbe ao ser coletado).
        // scene.isLoaded evita disparar em troca de cena (teardown).
        if (!gameObject.scene.isLoaded) return;
        XpColetaVFX.Tocar(transform.position, new Color(0.41f, 0.95f, 0.96f));
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Collect();
        }
    }

    void OnDrawGizmos()
    {
        if (player != null)
        {
            Gizmos.color = isAttracted ? Color.green : Color.yellow;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}
