using UnityEngine;
using System.Collections;

public class PassiveProjectileSkill2D : SkillBehavior
{
    [Header("Configurações 2D")]
    public GameObject projectilePrefab;
    public float activationInterval = 2.0f;
    public float searchRange = 10f;

    private SkillData skillData;
    private float activationTimer = 0f;

    public override void Initialize(PlayerStats stats)
    {
        base.Initialize(stats);
        FindSkillDataAlternative();
        SetupProjectilePrefab();
        Debug.Log($"✅ Projétil 2D Passivo inicializado: {skillData?.skillName ?? "Sem SkillData"}");
    }

    public void InitializeWithSkillData(PlayerStats stats, SkillData skill)
    {
        base.Initialize(stats);
        this.skillData = skill;
        SetupProjectilePrefab();
        Debug.Log($"✅ Projétil 2D Passivo inicializado: {skillData.skillName}");
    }

    private void FindSkillDataAlternative()
    {
        // Busca no SkillManager
        if (SkillManager.Instance != null)
        {
            var activeSkills = SkillManager.Instance.GetActiveSkills();
            skillData = activeSkills.Find(s => s.specificType == SpecificSkillType.Projectile);
        }

        // Busca no PlayerStats
        if (skillData == null && playerStats != null && playerStats.acquiredSkills != null)
        {
            skillData = playerStats.acquiredSkills.Find(s => s.specificType == SpecificSkillType.Projectile);
        }

        // Busca por nome
        if (skillData == null)
        {
            SkillData[] allSkills = Resources.FindObjectsOfTypeAll<SkillData>();
            skillData = System.Array.Find(allSkills, s => s.specificType == SpecificSkillType.Projectile);
        }
    }

    private void SetupProjectilePrefab()
    {
        Debug.Log("🔍 Buscando prefab do projétil...");

        // 🎯 PRIORIDADE 1: Prefab específico do SkillData (ProjectilePrefab2D)
        if (skillData != null && skillData.projectilePrefab2D != null)
        {
            projectilePrefab = skillData.projectilePrefab2D;
            Debug.Log($"✅ 🎯 Usando PREFAB ESPECÍFICO do SkillData: {skillData.projectilePrefab2D.name}");
            return; // Para aqui se encontrou o prefab específico
        }

        // 🎯 PRIORIDADE 2: Visual Effect do SkillData (fallback)
        if (skillData != null && skillData.visualEffect != null)
        {
            projectilePrefab = skillData.visualEffect;
            Debug.Log($"✅ 🎨 Usando VisualEffect do SkillData: {skillData.visualEffect.name}");
            return; // Para aqui se encontrou o visual effect
        }

        // 🎯 PRIORIDADE 3: Prefab carregado de Resources
        projectilePrefab = Resources.Load<GameObject>("Skills/ProjectileBase2D");
        if (projectilePrefab != null)
        {
            Debug.Log($"✅ 📁 Prefab carregado de Resources/Skills/ProjectileBase2D: {projectilePrefab.name}");
            return; // Para aqui se carregou do Resources
        }

        // 🎯 PRIORIDADE 4: Criar fallback automático (ÚLTIMA OPÇÃO)
        projectilePrefab = CreateFallbackProjectile();
        Debug.Log($"⚠️ 🔧 Prefab fallback criado automaticamente (nenhum prefab encontrado)");

        // Configura intervalo baseado na skill
        if (skillData != null && skillData.activationInterval > 0)
        {
            activationInterval = skillData.activationInterval;
            Debug.Log($"⏱️ Intervalo configurado para: {activationInterval}s");
        }
    }

    private GameObject CreateFallbackProjectile()
    {
        GameObject projectile = new GameObject("ProjectileFallback2D");

        // SpriteRenderer
        SpriteRenderer sprite = projectile.AddComponent<SpriteRenderer>();
        sprite.sprite = CreateFallbackSprite();
        sprite.color = skillData != null ? skillData.GetElementColor() : Color.red;

        // Rigidbody2D
        Rigidbody2D rb = projectile.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Collider
        CircleCollider2D collider = projectile.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.3f;

        // Controller
        projectile.AddComponent<ProjectileController2D>();

        projectile.SetActive(false);
        return projectile;
    }

    private Sprite CreateFallbackSprite()
    {
        int size = 32;
        Texture2D texture = new Texture2D(size, size);
        Vector2 center = new Vector2(size / 2, size / 2);
        float radius = size / 2 - 2;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                Color color = distance <= radius ? Color.white : Color.clear;
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    void Update()
    {
        if (playerStats == null || !playerStats.gameObject.activeInHierarchy) return;

        activationTimer += Time.deltaTime;
        if (activationTimer >= activationInterval)
        {
            TryActivateProjectile();
            activationTimer = 0f;
        }
    }

    private void TryActivateProjectile()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("⚠️ Prefab do projétil não atribuído!");
            return;
        }

        Transform target = FindClosestEnemy2D();
        if (target != null)
        {
            LaunchProjectile2D(target);
        }
    }

    private Transform FindClosestEnemy2D()
    {
        Transform closestEnemy = null;
        float closestDistance = Mathf.Infinity;
        Vector2 playerPosition = playerStats.transform.position;

        GameObject[] enemies = FindEnemiesByTags();

        if (enemies.Length == 0)
            enemies = FindEnemiesByLayer();

        if (enemies.Length == 0)
            enemies = FindEnemiesByComponent();

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null || !enemy.activeInHierarchy) continue;

            float distance = Vector2.Distance(playerPosition, (Vector2)enemy.transform.position);
            if (distance < closestDistance && distance <= searchRange)
            {
                closestDistance = distance;
                closestEnemy = enemy.transform;
            }
        }

        return closestEnemy;
    }

    private GameObject[] FindEnemiesByTags()
    {
        string[] possibleTags = { "Enemy", "enemy", "Enemies", "enemies" };

        foreach (string tag in possibleTags)
        {
            try
            {
                if (!string.IsNullOrEmpty(tag))
                {
                    GameObject[] enemies = GameObject.FindGameObjectsWithTag(tag);
                    if (enemies.Length > 0)
                    {
                        return enemies;
                    }
                }
            }
            catch (UnityException)
            {
                continue;
            }
        }
        return new GameObject[0];
    }

    private GameObject[] FindEnemiesByLayer()
    {
        try
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer != -1)
            {
                var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                System.Collections.Generic.List<GameObject> enemies = new System.Collections.Generic.List<GameObject>();

                foreach (GameObject obj in allObjects)
                {
                    if (obj.layer == enemyLayer && obj.activeInHierarchy)
                    {
                        enemies.Add(obj);
                    }
                }
                return enemies.ToArray();
            }
        }
        catch (System.Exception) { }

        return new GameObject[0];
    }

    private GameObject[] FindEnemiesByComponent()
    {
        try
        {
            MonoBehaviour[] enemyComponents = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            System.Collections.Generic.List<GameObject> enemies = new System.Collections.Generic.List<GameObject>();

            foreach (MonoBehaviour component in enemyComponents)
            {
                if (component == null) continue;

                string typeName = component.GetType().Name.ToLower();
                if (typeName.Contains("enemy") || typeName.Contains("inimigo"))
                {
                    if (!enemies.Contains(component.gameObject))
                    {
                        enemies.Add(component.gameObject);
                    }
                }
            }
            return enemies.ToArray();
        }
        catch (System.Exception) { }

        return new GameObject[0];
    }

    private void LaunchProjectile2D(Transform target)
    {
        if (target == null) return;

        Vector2 spawnOffset = Random.insideUnitCircle.normalized * 0.5f;
        Vector3 spawnPosition = playerStats.transform.position + (Vector3)spawnOffset;

        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);

        // 🎯 LOG DETALHADO SOBRE O PREFAB INSTANCIADO
        Debug.Log($"🚀 INSTANCIANDO: {projectilePrefab.name} | Skill: {skillData?.skillName ?? "Unknown"}");

        ProjectileController2D projectileController = projectile.GetComponent<ProjectileController2D>();
        if (projectileController != null)
        {
            float damage = CalculateDamage();
            PlayerStats.Element element = skillData != null ? skillData.element : PlayerStats.Element.None;

            projectileController.Initialize(
                target: target,
                damage: damage,
                speed: skillData != null ? skillData.projectileSpeed : 7f,
                lifeTime: skillData != null ? skillData.projectileLifeTime : 4f,
                element: element
            );

            ApplyVisualEffects(projectile);
            Debug.Log($"🚀 Projétil lançado! Prefab: {projectilePrefab.name} | Dano: {damage} | Elemento: {element}");
        }
        else
        {
            Debug.LogError($"❌ ProjectileController2D não encontrado no prefab: {projectilePrefab.name}");

            // Fallback básico
            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            if (rb != null && target != null)
            {
                Vector2 direction = ((Vector2)target.position - (Vector2)projectile.transform.position).normalized;
                rb.linearVelocity = direction * 7f;
                Destroy(projectile, 4f);
            }
        }
    }

    private float CalculateDamage()
    {
        float baseDamage = skillData != null ? skillData.attackBonus : 15f;
        float playerAttack = playerStats != null ? playerStats.attack : 10f;
        float elementalMultiplier = 1f;

        if (skillData != null && skillData.elementalBonus > 0)
        {
            elementalMultiplier = skillData.elementalBonus;
        }

        return (baseDamage + (playerAttack * 0.3f)) * elementalMultiplier;
    }

    private void ApplyVisualEffects(GameObject projectile)
    {
        if (skillData == null) return;

        SpriteRenderer sprite = projectile.GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            sprite.color = skillData.GetElementColor();
        }
    }

    public override void ApplyEffect()
    {
        Debug.Log($"🎯 Projétil 2D Passivo ativado: {skillData?.skillName} | Prefab: {projectilePrefab?.name}");
    }

    public override void RemoveEffect()
    {
        Debug.Log($"🔴 Projétil 2D Passivo desativado");
        activationTimer = 0f;
    }

    public void UpdateFromSkillData(SkillData newSkillData)
    {
        this.skillData = newSkillData;
        if (skillData != null)
        {
            activationInterval = skillData.activationInterval > 0 ? skillData.activationInterval : activationInterval;
            // 🎯 RECONFIGURA O PREFAB COM OS NOVOS DADOS
            SetupProjectilePrefab();
        }
    }

    // 🎯 MÉTODO PARA DEBUG - VERIFICAR CONFIGURAÇÃO ATUAL
    [ContextMenu("🔍 Verificar Configuração do Projétil")]
    public void DebugProjectileConfig()
    {
        Debug.Log("🔍 CONFIGURAÇÃO DO PROJÉTIL:");
        Debug.Log($"• Skill: {skillData?.skillName ?? "None"}");
        Debug.Log($"• Prefab Atual: {projectilePrefab?.name ?? "None"}");
        Debug.Log($"• Tem Prefab no SkillData: {skillData?.projectilePrefab2D?.name ?? "None"}");
        Debug.Log($"• Tem VisualEffect: {skillData?.visualEffect?.name ?? "None"}");
        Debug.Log($"• Intervalo: {activationInterval}s");
        Debug.Log($"• Alcance: {searchRange}m");
    }

    void OnDrawGizmosSelected()
    {
        if (playerStats != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerStats.transform.position, searchRange);
        }
    }
}