using UnityEngine;
using System.Collections;

public class PassiveProjectileSkill2D : SkillBehavior
{
    [Header("Configurações 2D")]
    public GameObject projectilePrefab;
    public float activationInterval = 2.0f;
    public float searchRange = 10f;

    private float activationTimer = 0f;

    public override void Initialize(PlayerStats stats)
    {
        base.Initialize(stats);
        FindSkillDataAlternative();
        SetupProjectilePrefab();
    }

    public void InitializeWithSkillData(PlayerStats stats, SkillData skill)
    {
        base.Initialize(stats);
        this.skillData = skill;
        SetupProjectilePrefab();
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

        // 🎯 PRIORIDADE 1: Prefab específico do SkillData (ProjectilePrefab2D)
        if (skillData != null && skillData.projectilePrefab2D != null)
        {
            projectilePrefab = skillData.projectilePrefab2D;
            return; // Para aqui se encontrou o prefab específico
        }

        // 🎯 PRIORIDADE 2: Visual Effect do SkillData (fallback)
        if (skillData != null && skillData.visualEffect != null)
        {
            projectilePrefab = skillData.visualEffect;
            return; // Para aqui se encontrou o visual effect
        }

        // 🎯 PRIORIDADE 3: Prefab carregado de Resources
        projectilePrefab = Resources.Load<GameObject>("Skills/ProjectileBase2D");
        if (projectilePrefab != null)
        {
            return; // Para aqui se carregou do Resources
        }

        // 🎯 PRIORIDADE 4: Criar fallback automático (ÚLTIMA OPÇÃO)
        projectilePrefab = CreateFallbackProjectile();

        // Configura intervalo baseado na skill
        if (skillData != null && skillData.activationInterval > 0)
        {
            activationInterval = skillData.activationInterval;
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
        if (projectilePrefab == null) return;

        bool isOrbitalPrefab = projectilePrefab.GetComponent<SwordOrbitalController>() != null;
        bool isOrbitalData   = skillData != null && skillData.isOrbitalProjectile;
        string nomeSkill     = skillData != null ? skillData.skillName.ToLower() : "";
        var evoMgr           = SkillEvolutionManager.Instance;

        if (isOrbitalPrefab || isOrbitalData)
        {
            // Espiral Dupla: 2 orbs
            int qtdOrbital = (evoMgr != null && evoMgr.EvolucaoAtiva(SkillEvolutionType.EspiralDupla)
                              && nomeSkill.Contains("spiral")) ? 2 : 1;
            for (int i = 0; i < qtdOrbital; i++)
                LaunchProjectile2D(null);
        }
        else
        {
            Transform target = FindClosestEnemy2D();
            if (target == null) return;

            // Spirit Ball Dupla: 2 projéteis
            bool ballDupla = evoMgr != null && evoMgr.EvolucaoAtiva(SkillEvolutionType.SpiritBallDupla)
                             && nomeSkill.Contains("ball");
            // Homing Múltiplo: 3 projéteis (Spirit Homing usa Boomerang type)
            bool homingMulti = evoMgr != null &&
                               (evoMgr.EvolucaoAtiva(SkillEvolutionType.HomingMultiplo) ||
                                evoMgr.EhEvolucao(SpecificSkillType.Boomerang, SkillEvolutionType.HomingMultiplo))
                               && nomeSkill.Contains("homing");

            int qtd = homingMulti ? 3 : ballDupla ? 2 : 1;
            for (int i = 0; i < qtd; i++)
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
            if (enemy.GetComponentInParent<ProjetilHomingPrincesa>(true)   != null) continue;
            if (enemy.GetComponentInParent<ProjetilEspecialPrincesa>(true) != null) continue;

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
                if (!component.enabled) continue; // ignora scripts desabilitados (ex: projéteis em órbita da Princesa)

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

        // Instancia no player
        GameObject projectile = Instantiate(projectilePrefab, playerStats.transform.position, Quaternion.identity);

        // Tenta pegar o controlador (seja orbital ou comum)
        ProjectileController2D pc = projectile.GetComponent<ProjectileController2D>();

        if (pc != null)
        {
            // Cor do elemento infundido sobrepõe a cor do elemento base do projétil
            if (skillData != null && skillData.appliedElement != ElementType.None && ElementRegistry.Instance != null)
                pc.infusedColorOverride = ElementRegistry.Instance.GetCor(skillData.appliedElement);

            pc.Initialize(
                target,
                CalculateDamage(),
                skillData?.projectileSpeed ?? 7f,
                skillData?.projectileLifeTime ?? 4f,
                skillData?.element ?? PlayerStats.Element.None
            );

            // Evoluções pós-lançamento
            var evoMgr   = SkillEvolutionManager.Instance;
            string nome  = skillData != null ? skillData.skillName.ToLower() : "";
            if (evoMgr != null)
            {
                // Espiral Furacão: zigzag ao sair da órbita
                if (evoMgr.EvolucaoAtiva(SkillEvolutionType.EspiralFuracao) && nome.Contains("spiral"))
                    projectile.AddComponent<ZigzagMoveFX>().Iniciar(pc);

                // Homing Eterno: relança 2x após hit
                if (evoMgr.EvolucaoAtiva(SkillEvolutionType.HomingEterno) && nome.Contains("homing"))
                    projectile.AddComponent<HomingEternoFX>().Iniciar(this, pc);
            }
        }
        else
        {
            Destroy(projectile);
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
    }

    public override void RemoveEffect()
    {
        activationTimer = 0f;
    }

    public override void ReducirCooldown(float segundos)
    {
        activationTimer += segundos;
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
