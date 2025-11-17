using UnityEngine;

public class XPOrb : MonoBehaviour
{
    [Header("Configurações da Orbe de XP")]
    public float xpValue = 10f;
    public float attractionSpeed = 5f;
    public float collectionRadius = 2f;
    public AudioClip collectSound;

    [Header("Efeitos Visuais")]
    public ParticleSystem collectParticles;
    public float rotationSpeed = 100f;
    public float floatAmplitude = 0.2f;
    public float floatFrequency = 2f;

    private Transform player;
    private bool isAttracted = false;
    private Vector3 startPosition;
    private float startTime;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        startPosition = transform.position;
        startTime = Time.time;
    }

    void Update()
    {
        // Animação de flutuação
        FloatAnimation();

        // Rotação
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);

        // Atração para o jogador
        if (isAttracted && player != null)
        {
            MoveTowardsPlayer();
        }
        else if (player != null)
        {
            CheckPlayerProximity();
        }
    }

    private void FloatAnimation()
    {
        float newY = startPosition.y + Mathf.Sin((Time.time - startTime) * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void CheckPlayerProximity()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        PlayerStats playerStats = player.GetComponent<PlayerStats>();

        if (playerStats != null && distanceToPlayer <= playerStats.GetXpCollectionRadius())
        {
            isAttracted = true;
        }
    }

    private void MoveTowardsPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        transform.position += direction * attractionSpeed * Time.deltaTime;

        // Verifica se chegou perto o suficiente do jogador para coletar
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= 0.5f)
        {
            Collect();
        }
    }

    private void Collect()
    {
        PlayerStats playerStats = player.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.GainXP(xpValue);

            if (collectSound != null)
                AudioSource.PlayClipAtPoint(collectSound, transform.position);

            if (collectParticles != null)
                Instantiate(collectParticles, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Visualização do raio de atração no Editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}