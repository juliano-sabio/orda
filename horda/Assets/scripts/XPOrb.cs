using UnityEngine;

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
    private PlayerStats playerStats;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player != null)
        {
            playerStats = player.GetComponent<PlayerStats>();
            Debug.Log($"🎯 Orbe criada. Player em: ({player.position.x:F1}, {player.position.y:F1})");
        }
    }

    void Update()
    {
        // 1. Rotação visual
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

        // 2. Verificar se deve mover
        if (!isAttracted && player != null)
        {
            CheckDistance();
        }

        // 3. MOVER se atraída
        if (isAttracted && player != null)
        {
            MoveToPlayer();
        }
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
            Debug.Log($"🎯 ATRAINDO! Distância: {distance:F1}");
        }
    }

    void MoveToPlayer()
    {
        // MOVIMENTO SIMPLES DIRETO
        Vector3 moveDirection = (player.position - transform.position).normalized;

        // DEBUG: Mostrar direção
        Debug.Log($"➡️ Movendo - Direção: ({moveDirection.x:F2}, {moveDirection.y:F2})");

        // APLICAR MOVIMENTO
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        // Verificar se chegou
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance < 0.5f)
        {
            Collect();
        }
    }

    void Collect()
    {
        if (playerStats != null)
        {
            playerStats.GainXP(xpValue);

            if (collectSound != null)
                AudioSource.PlayClipAtPoint(collectSound, transform.position);

            if (collectParticles != null)
            {
                Instantiate(collectParticles, transform.position, Quaternion.identity);
            }

            Debug.Log($"💫 Coletada! +{xpValue} XP");
            Destroy(gameObject);
        }
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