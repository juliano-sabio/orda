using UnityEngine;

public class SwordOrbitalController : ProjectileController2D
{
    [Header("🌀 Configurações de Órbita")]
    public float rotationSpeed = 360f;
    public float orbitRadius = 2.5f;

    private float angleAccumulated = 0f;
    private Transform playerTransform;

    public override void Initialize(Transform target, float damage, float speed, float lifeTime, PlayerStats.Element element)
    {
        // 1. IGNORA TUDO: Forçamos a órbita e passamos 0 de velocidade para o pai
        this.isOrbiting = true;
        base.Initialize(null, damage, 0f, lifeTime, element);

        // 2. CONFIGURAÇÃO FÍSICA AGRESSIVA
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic; // Desativa forças físicas
            rb.linearVelocity = Vector2.zero;       // Zera qualquer impulso inicial
            rb.angularVelocity = 0f;
            rb.simulated = true;
        }

        // 3. VINCULAÇÃO AO PLAYER
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            transform.SetParent(playerTransform);
            transform.localPosition = new Vector3(orbitRadius, 0, 0);
            transform.localRotation = Quaternion.identity;
        }
    }

    // ⛔ ISSO É O MAIS IMPORTANTE:
    // Sobrescrevemos o Update do pai para que o código de movimento linear NÃO RODE.
    protected override void Update()
    {
        if (!isInitialized || playerTransform == null) return;

        // Forçamos a velocidade a ser zero TODO FRAME (Garante que nada a lance)
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Lógica de Órbita
        float frameRotation = rotationSpeed * Time.deltaTime;
        transform.RotateAround(playerTransform.position, Vector3.forward, frameRotation);

        // Destruição após uma volta (360 graus)
        angleAccumulated += Mathf.Abs(frameRotation);
        if (angleAccumulated >= 360f)
        {
            transform.SetParent(null);
            Destroy(gameObject);
        }
    }
}