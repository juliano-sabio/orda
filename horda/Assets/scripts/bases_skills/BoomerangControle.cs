using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BoomerangController : MonoBehaviour
{
    // Estado do bumerangue
    public enum BoomerangState
    {
        Throwing,    // Indo em direção ao alvo/alcance máximo
        ReachingMax, // Atingiu alcance máximo, pausa breve
        Returning    // Voltando para o jogador
    }

    [Header("🌀 Configurações")]
    [SerializeField] private float throwSpeed = 15f;
    [SerializeField] private float returnSpeed = 20f;
    [SerializeField] private float maxRange = 8f;
    [SerializeField] private float damage = 25f;
    [SerializeField] private int maxTargets = 3;
    [SerializeField] private PlayerStats.Element element = PlayerStats.Element.None;
    [SerializeField] private bool healOnReturn = false;
    [SerializeField] private float healPercent = 0.1f;

    [Header("🌀 Referências")]
    private Transform player;
    private PlayerStats playerStats;
    private Rigidbody2D rb;
    private Animator animator; // ✅ Referência ao Animator

    [Header("🌀 Estado")]
    public BoomerangState currentState = BoomerangState.Throwing;
    private List<GameObject> hitEnemies = new List<GameObject>();
    private int currentTargets = 0;
    private Vector2 startPosition;
    private Vector2 throwDirection;
    private float currentDistance = 0f;
    private bool hasHitEnemy = false;
    private bool isInitialized = false;

    // Eventos
    public System.Action OnBoomerangDestroyed;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>(); // ✅ Obter Animator

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        // Configuração do Rigidbody
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Garantir collider
        if (GetComponent<Collider2D>() == null)
        {
            CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.4f;
        }
    }

    public void Initialize(
        Transform player,
        Vector2 throwDirection,
        float throwSpeed,
        float returnSpeed,
        float maxRange,
        float damage,
        int maxTargets,
        PlayerStats.Element element,
        bool healOnReturn,
        float healPercent,
        PlayerStats playerStats)
    {
        // ✅ VERIFICAÇÕES DE SEGURANÇA CRÍTICAS
        if (player == null)
        {
            Debug.LogError("❌ [BoomerangController] Player é nulo! Destruindo...");
            Destroy(gameObject);
            return;
        }

        if (playerStats == null)
        {
            Debug.LogError("❌ [BoomerangController] PlayerStats é nulo! Destruindo...");
            Destroy(gameObject);
            return;
        }

        if (rb == null)
        {
            Debug.LogError("❌ [BoomerangController] Rigidbody2D é nulo! Adicionando...");
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
        }

        this.player = player;
        this.playerStats = playerStats;
        this.throwDirection = throwDirection.normalized;
        this.throwSpeed = Mathf.Max(1f, throwSpeed);
        this.returnSpeed = Mathf.Max(1f, returnSpeed);
        this.maxRange = Mathf.Max(1f, maxRange);
        this.damage = Mathf.Max(1f, damage);
        this.maxTargets = Mathf.Max(1, maxTargets);
        this.element = element;
        this.healOnReturn = healOnReturn;
        this.healPercent = Mathf.Clamp01(healPercent);

        startPosition = transform.position;
        isInitialized = true;

        // ✅ CONFIGURAR ANIMAÇÃO BASEADA NO ESTADO
        UpdateAnimation();

        // Configurar rotação inicial baseada na direção (apenas visual)
        float angle = Mathf.Atan2(throwDirection.y, throwDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f); // -90 para apontar para frente

        // Iniciar movimento
        rb.linearVelocity = this.throwDirection * this.throwSpeed;

        // Destruir após tempo limite (segurança)
        Destroy(gameObject, 15f);

        Debug.Log($"✅ [BoomerangController] Inicializado com sucesso!");
        Debug.Log($"   • Player: {player.name}");
        Debug.Log($"   • Direção: {throwDirection}");
        Debug.Log($"   • Velocidade: {throwSpeed}");
        Debug.Log($"   • Alcance: {maxRange}m");
        Debug.Log($"   • Estado inicial: {currentState}");
    }

    void Update()
    {
        if (!isInitialized) return;

        if (player == null)
        {
            Debug.LogWarning("⚠️ [BoomerangController] Player perdido durante Update");
            DestroyBoomerang();
            return;
        }

        // ❌ REMOVIDO: ROTAÇÃO POR CÓDIGO (agora só pelo Animator)
        // ✅ Atualizar animação baseada no estado
        UpdateAnimation();

        // Calcular distância percorrida
        currentDistance = Vector2.Distance(startPosition, transform.position);

        // Gerenciar estados
        switch (currentState)
        {
            case BoomerangState.Throwing:
                UpdateThrowingState();
                break;

            case BoomerangState.ReachingMax:
                UpdateReachingMaxState();
                break;

            case BoomerangState.Returning:
                UpdateReturningState();
                break;
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        // ✅ Atualizar parâmetros do Animator baseados no estado
        switch (currentState)
        {
            case BoomerangState.Throwing:
                animator.SetBool("IsReturning", false);
                animator.SetFloat("Speed", 1f);
                break;

            case BoomerangState.Returning:
                animator.SetBool("IsReturning", true);
                animator.SetFloat("Speed", 1.5f); // Mais rápido ao retornar
                break;

            case BoomerangState.ReachingMax:
                animator.SetFloat("Speed", 0.5f); // Mais lento no ponto máximo
                break;
        }
    }

    private void UpdateThrowingState()
    {
        // Verificar se atingiu alcance máximo
        if (currentDistance >= maxRange)
        {
            currentState = BoomerangState.ReachingMax;
            if (rb != null) rb.linearVelocity = Vector2.zero;

            Debug.Log($"🌀 [Boomerang] Alcance máximo atingido ({currentDistance:F1}/{maxRange}m)");

            // Iniciar retorno após breve pausa
            StartCoroutine(StartReturningAfterDelay(0.3f));
        }

        // Verificar se já atingiu número máximo de alvos
        if (currentTargets >= maxTargets)
        {
            currentState = BoomerangState.ReachingMax;
            if (rb != null) rb.linearVelocity = Vector2.zero;
            StartCoroutine(StartReturningAfterDelay(0.2f));
        }
    }

    private void UpdateReachingMaxState()
    {
        // Estado de transição - já tratado pela coroutine
    }

    private void UpdateReturningState()
    {
        if (player == null) return;

        // Calcular direção para o jogador
        Vector2 directionToPlayer = (player.position - transform.position).normalized;

        // Aplicar velocidade de retorno
        if (rb != null)
        {
            rb.linearVelocity = directionToPlayer * returnSpeed;
        }
        else
        {
            // Fallback se Rigidbody não existir
            transform.position = Vector3.MoveTowards(transform.position, player.position, returnSpeed * Time.deltaTime);
        }

        // Verificar se chegou perto do jogador
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer < 0.8f)
        {
            OnReturnToPlayer();
        }
    }

    private IEnumerator StartReturningAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (currentState == BoomerangState.ReachingMax)
        {
            currentState = BoomerangState.Returning;
            UpdateAnimation(); // ✅ Atualizar animação para estado de retorno
            Debug.Log($"↩️ [Boomerang] Iniciando retorno ao jogador");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isInitialized || other == null) return;

        // Ignorar colisão com o jogador durante o lançamento
        if (player != null && other.gameObject == player.gameObject)
        {
            if (currentState == BoomerangState.Returning)
            {
                OnReturnToPlayer();
            }
            return;
        }

        // Verificar se é um inimigo
        if (IsEnemy(other.gameObject) && !hitEnemies.Contains(other.gameObject))
        {
            HandleEnemyHit(other.gameObject);
        }
    }

    private bool IsEnemy(GameObject obj)
    {
        if (obj == null) return false;

        // ✅ MÉTODO SEGURO: Verificar por componente primeiro
        if (obj.GetComponent<InimigoController>() != null)
        {
            return true;
        }

        // ✅ VERIFICAÇÃO DE TAG SEGURA (com try-catch)
        try
        {
            if (!string.IsNullOrEmpty(obj.tag))
            {
                if (obj.CompareTag("Enemy") || obj.CompareTag("enemy"))
                {
                    return true;
                }
            }
        }
        catch (UnityException)
        {
            // Tag não existe, ignorar
        }

        // Verificar por nome (fallback)
        string objName = obj.name.ToLower();
        if (objName.Contains("enemy") || objName.Contains("inimigo"))
        {
            return true;
        }

        return false;
    }

    private void HandleEnemyHit(GameObject enemy)
    {
        if (enemy == null) return;

        // Aplicar dano
        ApplyDamage(enemy);

        // Registrar inimigo atingido
        hitEnemies.Add(enemy);
        currentTargets++;
        hasHitEnemy = true;

        Debug.Log($"🎯 [Boomerang] Acertou {enemy.name} ({currentTargets}/{maxTargets})");

        // Se atingiu número máximo de alvos durante o lançamento, iniciar retorno
        if (currentState == BoomerangState.Throwing && currentTargets >= maxTargets)
        {
            currentState = BoomerangState.ReachingMax;
            if (rb != null) rb.linearVelocity = Vector2.zero;
            StartCoroutine(StartReturningAfterDelay(0.2f));
        }
    }

    private void ApplyDamage(GameObject enemy)
    {
        if (enemy == null) return;

        InimigoController inimigo = enemy.GetComponent<InimigoController>();
        if (inimigo != null)
        {
            inimigo.ReceberDano(damage);
            Debug.Log($"💥 [Boomerang] {damage} de dano em {enemy.name}");

            // Aplicar efeito elemental
            ApplyElementalEffect(enemy);
        }
        else
        {
            Debug.LogWarning($"⚠️ [Boomerang] InimigoController não encontrado em {enemy.name}");
        }
    }

    private void ApplyElementalEffect(GameObject enemy)
    {
        if (enemy == null || playerStats == null) return;

        switch (element)
        {
            case PlayerStats.Element.Fire:
                Debug.Log($"🔥 {enemy.name} queimando");
                break;
            case PlayerStats.Element.Ice:
                Debug.Log($"❄️ {enemy.name} congelado");
                break;
            case PlayerStats.Element.Lightning:
                Debug.Log($"⚡ {enemy.name} eletrocutado");
                break;
            case PlayerStats.Element.Wind:
                // Empurrar inimigo
                Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>();
                if (enemyRb != null)
                {
                    Vector2 pushDir = (enemy.transform.position - transform.position).normalized;
                    enemyRb.AddForce(pushDir * 10f, ForceMode2D.Impulse);
                }
                break;
        }
    }

    private void OnReturnToPlayer()
    {
        Debug.Log($"↩️ [Boomerang] Retornou ao jogador");

        // Cura ao retornar
        if (healOnReturn && hasHitEnemy && playerStats != null && currentTargets > 0)
        {
            float healAmount = damage * healPercent * currentTargets;
            playerStats.health = Mathf.Min(playerStats.health + healAmount, playerStats.maxHealth);
            Debug.Log($"💚 [Boomerang] Curou {healAmount:F1} HP");
        }

        // ✅ Trigger de animação de retorno completo
        if (animator != null)
        {
            animator.SetTrigger("ReturnComplete");
        }

        // Destruir bumerangue
        DestroyBoomerang();
    }

    private void DestroyBoomerang()
    {
        if (OnBoomerangDestroyed != null)
        {
            OnBoomerangDestroyed.Invoke();
        }

        Destroy(gameObject);
    }

    // Método para debug
    public string GetDebugInfo()
    {
        return $"Bumerangue - Estado: {currentState}, Alvos: {currentTargets}/{maxTargets}, " +
               $"Distância: {currentDistance:F1}/{maxRange:F1}m";
    }
}