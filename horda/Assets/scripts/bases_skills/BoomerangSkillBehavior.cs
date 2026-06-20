using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BoomerangSkillBehavior : SkillBehavior
{
    [Header("🌀 Configurações do Bumerangue")]
    public float damage = 25f;
    public float throwRange = 8f;
    public float throwSpeed = 15f;
    public float returnSpeed = 20f;
    public int maxTargets = 3;
    public float activationInterval = 2.5f;

    [Header("🌀 Efeitos")]
    public bool healOnReturn = true;
    public float healPercent = 0.1f;
    public PlayerStats.Element element = PlayerStats.Element.None;

    [Header("🌀 Debug")]
    public bool debugMode = true;
    public bool showGizmos = true;

    // Estado interno
    private float nextActivationTime = 0f;
    private bool isActive = true;
    private List<GameObject> activeBoomerangs = new List<GameObject>();
    private Transform playerTransform;
    private GameObject boomerangPrefab;

    public override void Initialize(PlayerStats stats)
    {
        base.Initialize(stats);

        if (playerStats != null)
        {
            playerTransform = playerStats.transform;

        }
        else
        {
            Debug.LogError("❌ [Boomerang] PlayerStats não encontrado na inicialização!");
        }
    }

    public void UpdateFromSkillData(SkillData skillData)
    {
        if (skillData == null)
        {
            Debug.LogError("❌ [Boomerang] SkillData é nulo!");
            return;
        }

        if (!skillData.IsBoomerangSkill())
        {
            Debug.LogError($"❌ [Boomerang] SkillData não é do tipo Boomerang: {skillData.specificType}");
            return;
        }

        // Configurar parâmetros
        damage = skillData.GetBoomerangDamage();
        throwRange = skillData.GetBoomerangThrowRange();
        throwSpeed = skillData.GetBoomerangThrowSpeed();
        maxTargets = skillData.GetBoomerangMaxTargets();
        activationInterval = skillData.activationInterval;
        element = skillData.element;
        healOnReturn = skillData.ShouldHealOnReturn();
        healPercent = skillData.boomerangHealPercent;

        // ✅ USAR PREFAB DO SKILLDATA
        if (skillData.projectilePrefab2D != null)
        {
            boomerangPrefab = skillData.projectilePrefab2D;

            // Garantir que o prefab tenha os componentes necessários
            PreparePrefab();
        }
        else
        {
            Debug.LogError("❌ [Boomerang] Nenhum prefab encontrado no SkillData!");
        }

    }

    private void PreparePrefab()
    {
        if (boomerangPrefab == null) return;

        // Criar uma instância temporária para verificar componentes
        GameObject tempInstance = Instantiate(boomerangPrefab);
        tempInstance.SetActive(false);

        bool hasController = tempInstance.GetComponent<BoomerangController>() != null;
        bool hasRigidbody = tempInstance.GetComponent<Rigidbody2D>() != null;
        bool hasCollider = tempInstance.GetComponent<Collider2D>() != null;


        // Destruir instância temporária
        Destroy(tempInstance);

        if (!hasController)
        {
            Debug.LogWarning($"⚠️ [Boomerang] O prefab {boomerangPrefab.name} não tem BoomerangController!");
        }
    }

    void Update()
    {
        if (!isActive || boomerangPrefab == null || playerTransform == null) return;

        if (Time.time >= nextActivationTime)
        {
            TryThrowBoomerang();
            nextActivationTime = Time.time + activationInterval;
        }

        activeBoomerangs.RemoveAll(item => item == null);
    }

    private void TryThrowBoomerang()
    {
        if (activeBoomerangs.Count >= 3)
        {
            return;
        }

        GameObject nearestEnemy = FindNearestEnemy();

        if (nearestEnemy != null)
        {
            ThrowBoomerang(nearestEnemy.transform);
        }
        else
        {
            Vector2 randomDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
            if (randomDirection == Vector2.zero) randomDirection = Vector2.right;

            ThrowBoomerangInDirection(randomDirection);
        }
    }

    private GameObject FindNearestEnemy()
    {
        if (playerTransform == null) return null;

        // Buscar inimigos por componente (mais seguro)
        InimigoController[] allEnemies = FindObjectsByType<InimigoController>(FindObjectsSortMode.None);

        GameObject nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (InimigoController enemy in allEnemies)
        {
            if (enemy == null || enemy.gameObject == playerTransform.gameObject) continue;
            if (!enemy.enabled) continue; // ignora scripts desabilitados (ex: projéteis em órbita da Princesa)
            if (enemy.GetComponentInParent<ProjetilHomingPrincesa>(true)   != null) continue;
            if (enemy.GetComponentInParent<ProjetilEspecialPrincesa>(true) != null) continue;

            float distance = Vector2.Distance(playerTransform.position, enemy.transform.position);
            if (distance < throwRange && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = enemy.gameObject;
            }
        }

        return nearest;
    }

    private void ThrowBoomerang(Transform target)
    {
        if (target == null || playerTransform == null || boomerangPrefab == null)
        {
            Debug.LogError($"❌ [Boomerang] Parâmetros inválidos para ThrowBoomerang!");
            return;
        }

        Vector2 direction = (target.position - playerTransform.position).normalized;

        // Instanciar bumerangue
        GameObject boomerangObj = Instantiate(boomerangPrefab,
            playerTransform.position + (Vector3)direction * 0.5f,
            Quaternion.identity);

        SetupBoomerangInstance(boomerangObj, direction);
    }

    private void ThrowBoomerangInDirection(Vector2 direction)
    {
        if (playerTransform == null || boomerangPrefab == null)
        {
            Debug.LogError($"❌ [Boomerang] Parâmetros inválidos para ThrowBoomerangInDirection!");
            return;
        }

        // Instanciar bumerangue
        GameObject boomerangObj = Instantiate(boomerangPrefab,
            playerTransform.position + (Vector3)direction * 0.5f,
            Quaternion.identity);

        SetupBoomerangInstance(boomerangObj, direction);
    }

    private void SetupBoomerangInstance(GameObject boomerangObj, Vector2 direction)
    {
        // ✅ VERIFICAÇÃO CRÍTICA: playerStats pode ser nulo?
        if (playerStats == null)
        {
            Debug.LogError("❌ [Boomerang] playerStats é NULO no SetupBoomerangInstance!");
            Destroy(boomerangObj);
            return;
        }

        // ✅ VERIFICAÇÃO CRÍTICA: playerTransform pode ser nulo?
        if (playerTransform == null)
        {
            Debug.LogError("❌ [Boomerang] playerTransform é NULO no SetupBoomerangInstance!");
            Destroy(boomerangObj);
            return;
        }

        // ✅ OBTER OU ADICIONAR BoomerangController
        BoomerangController controller = boomerangObj.GetComponent<BoomerangController>();

        if (controller == null)
        {
            Debug.LogWarning($"⚠️ [Boomerang] Adicionando BoomerangController dinamicamente...");
            controller = boomerangObj.AddComponent<BoomerangController>();
        }

        // ✅ ADICIONAR Rigidbody2D se não existir
        if (boomerangObj.GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D rb = boomerangObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
        }

        // ✅ ADICIONAR Collider2D se não existir
        if (boomerangObj.GetComponent<Collider2D>() == null)
        {
            CircleCollider2D collider = boomerangObj.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.5f;
        }

        // ✅ INICIALIZAR O CONTROLLER COM VERIFICAÇÕES
        if (controller != null)
        {
            controller.Initialize(
                player: playerTransform,
                throwDirection: direction,
                throwSpeed: throwSpeed,
                returnSpeed: returnSpeed,
                maxRange: throwRange,
                damage: damage,
                maxTargets: maxTargets,
                element: element,
                healOnReturn: healOnReturn,
                healPercent: healPercent,
                playerStats: playerStats
            );

            controller.OnBoomerangDestroyed += () =>
            {
                activeBoomerangs.Remove(boomerangObj);
            };

            // Co-op: transmite um fantasma do boomerang pro cliente do colega (mesma
            // trajetória, retorna ao mesmo player sincronizado, sem dano).
            if (NetSpawn.EmRede && SkillFxNet.Local != null && skillData != null)
            {
                int idx = SkillFxNet.Local.IndiceSkill(skillData);
                if (idx >= 0)
                {
                    ulong donoNetId = 0;
                    var no = playerTransform.GetComponentInParent<Unity.Netcode.NetworkObject>();
                    if (no != null) donoNetId = no.NetworkObjectId;
                    SkillFxNet.Local.ReplicarBoomerang(idx, boomerangObj.transform.position, direction,
                        donoNetId, throwSpeed, returnSpeed, throwRange, maxTargets, (int)element);
                }
            }
        }
        else
        {
            Debug.LogError($"❌ [Boomerang] Falha CRÍTICA: Não foi possível criar BoomerangController!");
            Destroy(boomerangObj);
            return;
        }

        // Adicionar à lista de ativos
        activeBoomerangs.Add(boomerangObj);

        // Ativar
        boomerangObj.SetActive(true);

        if (debugMode)
        {
        }
    }

    public override void ApplyEffect()
    {
        // Lançar bumerangue imediatamente
        TryThrowBoomerang();
    }

    public override void RemoveEffect()
    {
        // Destruir todos os bumerangues ativos
        foreach (GameObject boomerang in activeBoomerangs)
        {
            if (boomerang != null)
            {
                BoomerangController controller = boomerang.GetComponent<BoomerangController>();
                if (controller != null) controller.OnBoomerangDestroyed = null;
                Destroy(boomerang);
            }
        }
        activeBoomerangs.Clear();

    }

    // ✅ MÉTODO SetActive ADICIONADO
    public void SetActive(bool active)
    {
        isActive = active;
    }

    public override void ReducirCooldown(float segundos)
    {
        nextActivationTime -= segundos;
    }

    // ✅ MÉTODO TestBoomerang SEGURO
    [ContextMenu("🌀 Testar Bumerangue (Seguro)")]
    public void TestBoomerang()
    {
        if (playerStats == null)
        {
            Debug.LogError("❌ [Boomerang] PlayerStats não encontrado para teste");
            return;
        }

        if (playerTransform == null)
        {
            Debug.LogError("❌ [Boomerang] PlayerTransform não encontrado para teste");
            return;
        }

        if (boomerangPrefab == null)
        {
            Debug.LogError("❌ [Boomerang] Prefab não configurado para teste");
            return;
        }


        // Teste simples - lançar para a direita
        ThrowBoomerangInDirection(Vector2.right);

    }

    [ContextMenu("🔍 Diagnosticar Estado")]
    public void DiagnoseState()
    {

        if (playerStats != null)
        {
        }
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || playerTransform == null) return;

        // Mostrar alcance do bumerangue
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(playerTransform.position, throwRange);

        // Mostrar direções dos bumerangues ativos
        Gizmos.color = Color.yellow;
        foreach (GameObject boomerang in activeBoomerangs)
        {
            if (boomerang != null)
            {
                Gizmos.DrawLine(playerTransform.position, boomerang.transform.position);
            }
        }
    }
}
