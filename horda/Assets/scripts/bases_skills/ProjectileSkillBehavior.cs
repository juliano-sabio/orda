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
        skillData = GetComponent<SkillData>();

        // Carrega prefab 2D automaticamente
        if (projectilePrefab == null)
        {
            projectilePrefab = Resources.Load<GameObject>("Skills/ProjectileBase2D");
            if (projectilePrefab == null)
            {
                Debug.LogError("❌ Prefab do projétil 2D não encontrado!");
            }
        }

        Debug.Log($"✅ Projétil 2D Passivo inicializado: {skillData?.skillName}");
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

        Transform target = FindClosestEnemy2D();
        if (target != null)
        {
            LaunchProjectile2D(target);
        }
    }

    private Transform FindClosestEnemy2D()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform closestEnemy = null;
        float closestDistance = Mathf.Infinity;
        Vector2 playerPosition = playerStats.transform.position;

        foreach (GameObject enemy in enemies)
        {
            if (!enemy.activeInHierarchy) continue;

            // Usa Vector2.Distance para cálculo 2D
            float distance = Vector2.Distance(playerPosition, (Vector2)enemy.transform.position);
            if (distance < closestDistance && distance <= searchRange)
            {
                closestDistance = distance;
                closestEnemy = enemy.transform;
            }
        }

        return closestEnemy;
    }

    private void LaunchProjectile2D(Transform target)
    {
        // Posição de spawn ao redor do player (2D)
        Vector2 spawnOffset = Random.insideUnitCircle.normalized * 0.5f;
        Vector3 spawnPosition = playerStats.transform.position + (Vector3)spawnOffset;

        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);

        ProjectileController2D projectileController = projectile.GetComponent<ProjectileController2D>();
        if (projectileController != null)
        {
            float damage = CalculateDamage();
            PlayerStats.Element element = skillData != null ? skillData.element : PlayerStats.Element.None;

            projectileController.Initialize(
                target: target,
                damage: damage,
                speed: 7f,
                lifeTime: 4f,
                element: element
            );

            Debug.Log($"🚀 Projétil 2D lançado! Dano: {damage}");
        }
    }

    private float CalculateDamage()
    {
        float baseDamage = skillData != null ? skillData.attackBonus : 15f;
        float playerAttack = playerStats != null ? playerStats.attack : 10f;
        return baseDamage + (playerAttack * 0.3f);
    }

    public override void ApplyEffect()
    {
        Debug.Log($"🎯 Projétil 2D Passivo ativado: {skillData?.skillName}");
    }

    public override void RemoveEffect()
    {
        Debug.Log($"🔴 Projétil 2D Passivo desativado");
    }
}