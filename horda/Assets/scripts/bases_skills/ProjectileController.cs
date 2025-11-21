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

        // 🎯 MÉTODO CORRIGIDO: Verificação robusta de inimigos
        if (IsEnemy(other.gameObject))
        {
            OnHitTarget(other.gameObject);
        }
    }

    // 🎯 MÉTODO NOVO: Verificação robusta de inimigos
    private bool IsEnemy(GameObject obj)
    {
        if (obj == null) return false;

        // Método 1: Verificação por tag (com try-catch)
        try
        {
            if (obj.CompareTag("Enemy") || obj.CompareTag("enemy"))
            {
                return true;
            }
        }
        catch (UnityException)
        {
            // Tags não existem, continuar para outros métodos
        }

        // Método 2: Verificação por componente
        if (obj.GetComponent<InimigoController>() != null)
        {
            return true;
        }

        // Método 3: Verificação por nome
        string objName = obj.name.ToLower();
        if (objName.Contains("enemy") || objName.Contains("inimigo"))
        {
            return true;
        }

        // Método 4: Verificação por layer
        if (obj.layer == LayerMask.NameToLayer("Enemy"))
        {
            return true;
        }

        return false;
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
            else
            {
                // Fallback: tenta encontrar qualquer componente de inimigo
                MonoBehaviour[] components = targetEnemy.GetComponents<MonoBehaviour>();
                foreach (var component in components)
                {
                    if (component.GetType().Name.ToLower().Contains("enemy") ||
                        component.GetType().Name.ToLower().Contains("inimigo"))
                    {
                        // Usa reflexão para chamar método de dano se existir
                        var damageMethod = component.GetType().GetMethod("ReceberDano");
                        if (damageMethod != null)
                        {
                            damageMethod.Invoke(component, new object[] { damage });
                            Debug.Log($"💥 Projétil acertou {targetEnemy.name} via reflexão");
                            break;
                        }
                    }
                }
            }

            // Aplica efeito elemental
            ApplyElementalEffect(targetEnemy);
        }
        else
        {
            Debug.Log("🎯 Projétil atingiu alvo (sem GameObject específico)");
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
        if (enemy == null) return;

        PlayerStats playerStats = FindAnyObjectByType<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.GetElementSystem().ApplyElementalEffect(element, enemy);
        }
        else
        {
            // Fallback: aplica efeito básico
            ApplyBasicElementalEffect(enemy);
        }
    }

    // 🎯 MÉTODO NOVO: Efeitos elementais básicos
    private void ApplyBasicElementalEffect(GameObject enemy)
    {
        switch (element)
        {
            case PlayerStats.Element.Fire:
                Debug.Log($"🔥 {enemy.name} está queimando!");
                break;
            case PlayerStats.Element.Ice:
                Debug.Log($"❄️ {enemy.name} está congelado!");
                break;
            case PlayerStats.Element.Lightning:
                Debug.Log($"⚡ {enemy.name} está eletrocutado!");
                break;
            case PlayerStats.Element.Poison:
                Debug.Log($"☠️ {enemy.name} está envenenado!");
                break;
            case PlayerStats.Element.Earth:
                Debug.Log($"🌍 {enemy.name} está atordoado!");
                break;
            case PlayerStats.Element.Wind:
                Debug.Log($"💨 {enemy.name} está sendo empurrado!");
                break;
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

    // 🎯 MÉTODO NOVO: Para debug
    void OnDrawGizmos()
    {
        if (hasHit) return;

        // Desenha linha para o alvo (apenas no editor)
        if (target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
}