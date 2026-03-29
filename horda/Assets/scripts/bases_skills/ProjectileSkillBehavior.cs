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

    private int swordCount = 0;
    private float lastActivationTime = 0f;
    private int spawnIndex = 0; // Para distribuir as espadas em círculo
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
        if (skillData == null && SkillManager.Instance != null)
        {
            var activeSkills = SkillManager.Instance.GetActiveSkills();
            // Tenta achar pelo nome "Espada" ou similar se o tipo não bater
            skillData = activeSkills.Find(s => s.specificType == SpecificSkillType.Projectile || s.skillName.Contains("Espada"));
        }
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
        if (playerStats == null || !playerStats.gameObject.activeInHierarchy) return;

        // 🎯 CONTROLE RÍGIDO DE COOLDOWN
        if (Time.time >= lastActivationTime + activationInterval)
        {
            TryActivateProjectile();
            lastActivationTime = Time.time; // Reseta o cronômetro baseado no tempo real do jogo
        }
        if (playerStats == null || !playerStats.gameObject.activeInHierarchy) return;

        // 🎯 COOLDOWN PRECISO: Só entra aqui no momento exato da recarga
        if (Time.time >= lastActivationTime + activationInterval)
        {
            // Só tenta ativar se houver inimigos (opcional, dependendo da sua regra)
            Transform target = FindClosestEnemy2D();

            // Se for a ESPADA, ela não precisa de alvo para nascer (ela orbita você!)
            if (projectilePrefab != null && projectilePrefab.GetComponent<SwordSpinSkillBehavior>() != null)
            {
                LaunchProjectile2D(null); // Passa null porque a espada foca no Player
                lastActivationTime = Time.time;
            }
            else if (target != null) // Projéteis comuns ainda precisam de alvo
            {
                LaunchProjectile2D(target);
                lastActivationTime = Time.time;
            }
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
        if (projectilePrefab == null) return;

        GameObject instance = Instantiate(projectilePrefab, playerStats.transform.position, Quaternion.identity);
        instance.SetActive(true);

        var sword = instance.GetComponent<SwordSpinSkillBehavior>();

        if (sword != null)
        {
            // 1. Inicializa a lógica de órbita
            sword.Initialize(this.playerStats);
            instance.transform.SetParent(playerStats.transform);

            // 2. DISTRIBUIÇÃO ESPACIAL: 
            // Cada espada nasce 90 graus à frente da anterior (0, 90, 180, 270...)
            float angle = spawnIndex * 90f;
            instance.transform.localRotation = Quaternion.Euler(0, 0, angle);
            spawnIndex++;

            // 3. DURAÇÃO GARANTIDA:
            // Pega o tempo de vida do SkillData ou usa 5 segundos como padrão
            float life = (skillData != null && skillData.projectileLifeTime > 0) ? skillData.projectileLifeTime : 5f;
            Destroy(instance, life);
        }
        else
        {
            // Lógica original de Projétil (Tiros/Magias)
            // ... (seu código de ProjectileController2D.Initialize) ...
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