using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ProjectileController2D : MonoBehaviour
{
    [Header("Configurações 2D")]
    public float speed = 8f;
    public float lifeTime = 5f;
    public float damage = 25f;
    public PlayerStats.Element element = PlayerStats.Element.None;

    [Header("🎯 Controle Orbital")]
    public bool isOrbiting = false;
    public bool allowMovement = true;
    public bool ignoreTargetsDuringOrbit = true; // 🆕 AGORA FALSE para causar dano orbital

    [Header("⚡ Dano Orbital Contínuo")]
    public bool orbitalDamageEnabled = true; // 🆕 Novo: ativar dano durante órbita
    public float orbitalDamageInterval = 0.3f; // 🆕 Intervalo entre danos no mesmo inimigo
    public float orbitalDamageRadius = 1.5f; // 🆕 Raio de detecção durante órbita

    [Header("🔧 Debug Dano")]
    public bool debugDamage = true;

    [Header("Efeitos Visuais")]
    public GameObject hitEffect;
    public TrailRenderer trailRenderer;

    private Transform target;
    private bool hasHit = false;
    private Rigidbody2D rb;
    private float spawnTime;
    private Dictionary<GameObject, float> lastDamageTime = new Dictionary<GameObject, float>(); // 🆕 Controlar intervalo de dano

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

        SetupVisuals();

        if (isOrbiting)
        {
            allowMovement = false;
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.gravityScale = 0f;
            }
        }

        spawnTime = Time.time;

        if (!isOrbiting)
        {
            Destroy(gameObject, lifeTime);
        }
    }

    public void SetAsOrbiting()
    {
        isOrbiting = true;
        allowMovement = false;
        ignoreTargetsDuringOrbit = false; // 🆕 AGORA FALSE para causar dano durante órbita

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
        }

        CancelInvoke("DestroyProjectile");
    }

    public void EnableMovement()
    {
        isOrbiting = false;
        allowMovement = true;
        ignoreTargetsDuringOrbit = false;

        spawnTime = Time.time;
        Destroy(gameObject, lifeTime);

        if (debugDamage) Debug.Log($"🎯 Projétil ativado - Movimento: {allowMovement}, Orbital: {isOrbiting}");
    }

    public void LaunchInDirection(Vector2 direction, float launchSpeed)
    {
        EnableMovement();

        if (rb != null)
        {
            rb.linearVelocity = direction * launchSpeed;
        }
        else
        {
            StartCoroutine(MoveInDirection(direction, launchSpeed));
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (debugDamage) Debug.Log($"🚀 Projétil lançado - Direção: {direction}, Velocidade: {launchSpeed}, Dano: {damage}");
    }

    private IEnumerator MoveInDirection(Vector2 direction, float moveSpeed)
    {
        while (!hasHit && Time.time - spawnTime < lifeTime)
        {
            if (allowMovement && !isOrbiting)
            {
                transform.position += (Vector3)(direction * moveSpeed * Time.deltaTime);
            }
            yield return null;
        }

        if (!hasHit)
        {
            DestroyProjectile();
        }
    }

    void Update()
    {
        if (isOrbiting && Time.time - spawnTime > lifeTime * 2f)
        {
            DestroyProjectile();
            return;
        }

        // 🆕 VERIFICAR DANO ORBITAL CONTINUAMENTE
        if (isOrbiting && orbitalDamageEnabled && !hasHit)
        {
            CheckOrbitalDamage();
        }

        if (!allowMovement || isOrbiting || hasHit) return;

        if (target != null)
        {
            Vector2 direction = ((Vector2)target.position - (Vector2)transform.position).normalized;

            if (rb != null)
            {
                rb.linearVelocity = direction * speed;
            }
            else
            {
                transform.position += (Vector3)direction * speed * Time.deltaTime;
            }

            if (direction != Vector2.zero)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }

            CheckCollision();
        }
        else
        {
            if (rb != null && rb.linearVelocity == Vector2.zero)
            {
                Vector2 currentDirection = transform.right;
                rb.linearVelocity = currentDirection * speed;
            }

            CheckCollision();
        }

        if (Time.time - spawnTime >= lifeTime && !isOrbiting)
        {
            DestroyProjectile();
        }
    }

    // 🆕 MÉTODO PARA VERIFICAR DANO DURANTE ÓRBITA
    private void CheckOrbitalDamage()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, orbitalDamageRadius);

        foreach (Collider2D enemyCollider in hitEnemies)
        {
            if (IsEnemy(enemyCollider.gameObject))
            {
                TryApplyOrbitalDamage(enemyCollider.gameObject);
            }
        }
    }

    // 🆕 MÉTODO PARA APLICAR DANO ORBITAL COM INTERVALO
    private void TryApplyOrbitalDamage(GameObject enemy)
    {
        if (enemy == null) return;

        // Verificar intervalo de dano
        if (lastDamageTime.ContainsKey(enemy))
        {
            if (Time.time - lastDamageTime[enemy] < orbitalDamageInterval)
            {
                return; // Ainda não pode dar dano novamente
            }
            lastDamageTime[enemy] = Time.time;
        }
        else
        {
            lastDamageTime.Add(enemy, Time.time);
        }

        // Aplicar dano
        ApplyDamageToEnemy(enemy, "ORBITAL");
    }

    void CheckCollision()
    {
        if (target == null) return;

        float distanceToTarget = Vector2.Distance(transform.position, target.position);
        if (distanceToTarget < 0.3f)
        {
            OnHitTarget();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        // 🆕 SEM IGNORE - sempre verifica colisão, mesmo durante órbita
        if (IsEnemy(other.gameObject))
        {
            if (isOrbiting)
            {
                // Durante órbita, aplica dano mas NÃO destrói o projétil
                TryApplyOrbitalDamage(other.gameObject);
            }
            else
            {
                // Durante lançamento, aplica dano e destrói
                OnHitTarget(other.gameObject);
            }
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (hasHit) return;

        // 🆕 DANO CONTÍNUO durante órbita
        if (isOrbiting && IsEnemy(other.gameObject))
        {
            TryApplyOrbitalDamage(other.gameObject);
        }
    }

    private bool IsEnemy(GameObject obj)
    {
        if (obj == null) return false;

        try
        {
            if (obj.CompareTag("Enemy") || obj.CompareTag("enemy"))
            {
                return true;
            }
        }
        catch (UnityException) { }

        if (obj.GetComponent<InimigoController>() != null)
        {
            return true;
        }

        string objName = obj.name.ToLower();
        if (objName.Contains("enemy") || objName.Contains("inimigo"))
        {
            return true;
        }

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
            ApplyDamageToEnemy(targetEnemy, "LANÇAMENTO");
        }
        else
        {
            if (debugDamage) Debug.Log("🎯 Projétil atingiu mas não encontrou GameObject de inimigo");
        }

        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }

        DestroyProjectile();
    }

    // 🆕 MÉTODO UNIFICADO PARA APLICAR DANO
    private void ApplyDamageToEnemy(GameObject enemy, string damageType)
    {
        if (debugDamage) Debug.Log($"🎯 Tentando causar {damage} de dano ({damageType}) em: {enemy.name}");

        InimigoController inimigo = enemy.GetComponent<InimigoController>();
        if (inimigo != null)
        {
            inimigo.ReceberDano(damage);
            if (debugDamage) Debug.Log($"💥 DANO {damageType}: {damage} em {enemy.name} | Elemento: {element}");
        }
        else
        {
            if (debugDamage) Debug.LogError($"❌ InimigoController não encontrado em: {enemy.name}");
        }

        ApplyElementalEffect(enemy);
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
            ApplyBasicElementalEffect(enemy);
        }
    }

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

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = elementColor;
        }

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

    private void DestroyProjectile()
    {
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    public bool ShouldDestroy()
    {
        return hasHit || (Time.time - spawnTime >= lifeTime && !isOrbiting);
    }

    void OnDrawGizmos()
    {
        if (hasHit) return;

        // 🆕 MOSTRAR RAIO DE DANO ORBITAL
        if (isOrbiting && orbitalDamageEnabled)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, orbitalDamageRadius);
        }

        if (target != null)
        {
            Gizmos.color = isOrbiting ? Color.blue : Color.red;
            Gizmos.DrawLine(transform.position, target.position);
        }

        if (isOrbiting)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }

    public string GetDebugInfo()
    {
        return $"🎯 Projétil - Orbital: {isOrbiting}, DanoOrbital: {orbitalDamageEnabled}, Dano: {damage}, InimigosAfetados: {lastDamageTime.Count}";
    }
}