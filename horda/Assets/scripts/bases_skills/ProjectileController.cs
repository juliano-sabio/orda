using UnityEngine;
using System.Collections;

public class ProjectileController2D : MonoBehaviour
{
    [Header("Configurações 2D")]
    public float speed = 8f;
    public float lifeTime = 5f;
    public float damage = 25f;
    public PlayerStats.Element element = PlayerStats.Element.None;

    [Header("Efeitos Visuais")]
    public GameObject hitEffect;
    public TrailRenderer trailRenderer;

    private Transform target;
    private bool hasHit = false;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(Transform target, float damage, float speed, float lifeTime, PlayerStats.Element element)
    {
        this.target = target;
        this.damage = damage;
        this.speed = speed;
        this.lifeTime = lifeTime;
        this.element = element;

        // Configura efeitos visuais baseados no elemento
        SetupVisuals();

        // Destroi após tempo limite
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (hasHit || target == null) return;

        // Movimento 2D em direção ao alvo
        Vector2 direction = ((Vector2)target.position - (Vector2)transform.position).normalized;

        // Usa Rigidbody2D para movimento suave
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }
        else
        {
            // Fallback: movimento direto
            transform.position += (Vector3)direction * speed * Time.deltaTime;
        }

        // Rotação em direção ao alvo (opcional)
        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        CheckCollision();
    }

    void CheckCollision()
    {
        if (target == null) return;

        float distanceToTarget = Vector2.Distance(transform.position, target.position);
        if (distanceToTarget < 0.3f) // Distância de colisão menor para 2D
        {
            OnHitTarget();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        if (other.CompareTag("Enemy"))
        {
            OnHitTarget(other.gameObject);
        }
    }

    void OnHitTarget(GameObject enemy = null)
    {
        if (hasHit) return;
        hasHit = true;

        GameObject targetEnemy = enemy != null ? enemy : (target != null ? target.gameObject : null);

        if (targetEnemy != null)
        {
            // Causa dano no inimigo 2D
            InimigoController inimigo = targetEnemy.GetComponent<InimigoController>();
            if (inimigo != null)
            {
                inimigo.ReceberDano(damage);
                Debug.Log($"💥 Projétil 2D acertou {targetEnemy.name} com {damage} de dano {element}");
            }

            // Aplica efeito elemental
            ApplyElementalEffect(targetEnemy);
        }

        // Efeito de impacto
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    void ApplyElementalEffect(GameObject enemy)
    {
        PlayerStats playerStats = FindAnyObjectByType<PlayerStats>();
        if (playerStats != null && enemy != null)
        {
            playerStats.GetElementSystem().ApplyElementalEffect(element, enemy);
        }
    }

    void SetupVisuals()
    {
        Color elementColor = GetElementColor();

        // Configura cor do sprite
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = elementColor;
        }

        // Configura trail renderer se existir
        if (trailRenderer != null)
        {
            trailRenderer.startColor = elementColor;
            trailRenderer.endColor = new Color(elementColor.r, elementColor.g, elementColor.b, 0f);
        }
    }

    Color GetElementColor()
    {
        switch (element)
        {
            case PlayerStats.Element.Fire: return Color.red;
            case PlayerStats.Element.Ice: return Color.cyan;
            case PlayerStats.Element.Lightning: return Color.yellow;
            case PlayerStats.Element.Poison: return Color.green;
            case PlayerStats.Element.Earth: return new Color(0.6f, 0.3f, 0.1f);
            case PlayerStats.Element.Wind: return Color.white;
            default: return Color.white;
        }
    }
}