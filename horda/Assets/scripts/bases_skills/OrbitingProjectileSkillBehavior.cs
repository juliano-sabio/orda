using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitingProjectileSkillBehavior : SkillBehavior
{
    [Header("🌀 Configurações Orbital")]
    public float orbitRadius = 2f;
    public float orbitSpeed = 180f;
    public int numberOfOrbits = 1;
    public bool continuousSpawning = false;
    public float spawnInterval = 2f;
    public int maxProjectiles = 3;

    [Header("🎯 Configurações de Disparo")]
    public float launchSpeed = 10f;
    public float maxLaunchDistance = 15f;

    [Header("💥 Dano e Efeitos")]
    public float projectileDamage = 20f;
    public PlayerStats.Element projectileElement = PlayerStats.Element.None;

    [Header("⚡ Dano Orbital")]
    public bool enableOrbitalDamage = true;
    public float orbitalDamageInterval = 0.2f;
    public float orbitalDamageRadius = 1.8f;

    [Header("🎯 Targeting Orbital")]
    public OrbitalTargetingMode orbitalTargetingMode = OrbitalTargetingMode.NearestEnemy;
    public bool autoAcquireTargets = true;
    public float targetAcquisitionRange = 8f;

    [Header("🔧 Debug")]
    public bool showDebugInfo = true;

    // Referências
    private Transform playerTransform;
    private List<OrbitingProjectile> activeOrbitals = new List<OrbitingProjectile>();
    private float spawnTimer = 0f;
    private SkillData currentSkillData;

    [System.Serializable]
    public class OrbitingProjectile
    {
        public GameObject projectileObject;
        public Transform transform;
        public ProjectileController2D projectileController;
        public float currentAngle = 0f;
        public float startAngle = 0f;
        public float orbitsCompleted = 0f;
        public bool isLaunching = false;
        public Vector2 launchDirection;
        public float totalRotation = 0f;
        public Transform target; // 🆕 Target específico para este projétil

        public OrbitingProjectile(GameObject projectile, float startAngle)
        {
            projectileObject = projectile;
            transform = projectile.transform;
            currentAngle = startAngle;
            this.startAngle = startAngle;
            projectileController = projectile.GetComponent<ProjectileController2D>();
        }
    }

    public override void Initialize(PlayerStats stats)
    {
        base.Initialize(stats);
        playerTransform = playerStats.transform;

        // 🆕 OBTER DADOS DA SKILL ATUAL
        currentSkillData = SkillManager.Instance?.GetEquippedSkill();
        if (currentSkillData != null && currentSkillData.IsOrbitalProjectileSkill())
        {
            UpdateFromSkillData(currentSkillData);
        }

        DebugLog($"🌀 Skill Orbital Inicializada: {orbitSpeed}°/s, {numberOfOrbits} voltas, {maxProjectiles} projéteis máx");
    }

    public override void ApplyEffect()
    {
        DebugLog($"🌀 Aplicando efeito orbital - Contínuo: {continuousSpawning}");

        // 🆕 ATUALIZAR CONFIGURAÇÕES DA SKILL
        if (currentSkillData != null && currentSkillData.IsOrbitalProjectileSkill())
        {
            UpdateFromSkillData(currentSkillData);
        }

        if (!continuousSpawning)
        {
            // Spawn único
            SpawnOrbitingProjectile();
        }
        else
        {
            // Iniciar spawn contínuo
            StartCoroutine(ContinuousSpawning());
        }
    }

    private IEnumerator ContinuousSpawning()
    {
        DebugLog("🔄 Iniciando spawn contínuo de projéteis orbitais");

        while (true)
        {
            if (activeOrbitals.Count < maxProjectiles)
            {
                SpawnOrbitingProjectile();
            }
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnOrbitingProjectile()
    {
        if (playerTransform == null)
        {
            Debug.LogError("❌ PlayerTransform é null!");
            return;
        }

        if (activeOrbitals.Count >= maxProjectiles)
        {
            DebugLog($"🎯 Máximo de {maxProjectiles} projéteis atingido");
            return;
        }

        // 🎯 POSIÇÃO ALEATÓRIA NA ÓRBITA
        float randomStartAngle = Random.Range(0f, 360f);
        Vector2 spawnPosition = CalculateOrbitPosition(randomStartAngle);

        DebugLog($"🎲 Spawnando projétil - Ângulo: {randomStartAngle}° | Posição: {spawnPosition} | Voltas necessárias: {numberOfOrbits}");

        // 🆕 USAR PREFAB DA SKILL DATA
        GameObject projectilePrefab = GetProjectilePrefab();
        if (projectilePrefab == null)
        {
            Debug.LogWarning("⚠️ Nenhum prefab de projétil encontrado!");
            return;
        }

        // Criar projétil na posição orbital
        GameObject newProjectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        newProjectile.name = $"OrbitalProjectile_{activeOrbitals.Count}";

        // 🔧 CONFIGURAÇÃO ORBITAL
        ProjectileController2D projectileController = newProjectile.GetComponent<ProjectileController2D>();
        if (projectileController != null)
        {
            // 🆕 ENCONTRAR TARGET BASEADO NO MODO DE TARGETING
            Transform target = FindTargetForOrbital();

            projectileController.SetAsOrbiting();
            projectileController.Initialize(
                target,
                projectileDamage,
                launchSpeed,
                lifeTime: 10f,
                projectileElement
            );

            // ⚡ CONFIGURAR DANO ORBITAL
            projectileController.orbitalDamageEnabled = enableOrbitalDamage;
            projectileController.orbitalDamageInterval = orbitalDamageInterval;
            projectileController.orbitalDamageRadius = orbitalDamageRadius;
            projectileController.debugDamage = showDebugInfo;

            DebugLog($"⚡ Projétil configurado para dano orbital - Raio: {orbitalDamageRadius}, Intervalo: {orbitalDamageInterval}s");
        }
        else
        {
            Debug.LogError("❌ ProjectileController2D não encontrado no prefab!");
        }

        // 🔧 CONFIGURAR FÍSICA
        Rigidbody2D rb = newProjectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        // 🎯 CRIAR OBJETO ORBITAL
        OrbitingProjectile orbital = new OrbitingProjectile(newProjectile, randomStartAngle);
        orbital.target = FindTargetForOrbital(); // 🆕 Atribuir target específico
        activeOrbitals.Add(orbital);

        DebugLog($"🌀 Projétil orbital spawnado - Ângulo: {randomStartAngle}° | Target: {orbital.target?.name ?? "None"} | Total: {activeOrbitals.Count}/{maxProjectiles}");
    }

    // 🆕 MÉTODO PARA ENCONTRAR TARGET BASEADO NO MODO CONFIGURADO
    private Transform FindTargetForOrbital()
    {
        if (!autoAcquireTargets) return null;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0) return null;

        switch (orbitalTargetingMode)
        {
            case OrbitalTargetingMode.NearestEnemy:
                return FindNearestEnemy(enemies);

            case OrbitalTargetingMode.RandomEnemy:
                return FindRandomEnemy(enemies);

            case OrbitalTargetingMode.FixedAngle:
                return null; // Sem target específico

            case OrbitalTargetingMode.PlayerDirection:
                return FindEnemyInPlayerDirection(enemies);

            default:
                return FindNearestEnemy(enemies);
        }
    }

    private Transform FindNearestEnemy(GameObject[] enemies)
    {
        Transform nearest = null;
        float nearestDistance = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector2.Distance(playerTransform.position, enemy.transform.position);
            if (distance < nearestDistance && distance <= targetAcquisitionRange)
            {
                nearestDistance = distance;
                nearest = enemy.transform;
            }
        }

        return nearest;
    }

    private Transform FindRandomEnemy(GameObject[] enemies)
    {
        List<GameObject> enemiesInRange = new List<GameObject>();

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector2.Distance(playerTransform.position, enemy.transform.position);
            if (distance <= targetAcquisitionRange)
            {
                enemiesInRange.Add(enemy);
            }
        }

        if (enemiesInRange.Count > 0)
        {
            return enemiesInRange[Random.Range(0, enemiesInRange.Count)].transform;
        }

        return null;
    }

    private Transform FindEnemyInPlayerDirection(GameObject[] enemies)
    {
        // Encontra inimigo na direção que o player está virado
        Vector2 playerDirection = playerTransform.right; // Assumindo que player olha para a direita

        Transform bestTarget = null;
        float bestDot = -1f;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector2.Distance(playerTransform.position, enemy.transform.position);
            if (distance <= targetAcquisitionRange)
            {
                Vector2 directionToEnemy = ((Vector2)enemy.transform.position - (Vector2)playerTransform.position).normalized;
                float dot = Vector2.Dot(playerDirection, directionToEnemy);

                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestTarget = enemy.transform;
                }
            }
        }

        return bestDot > 0.5f ? bestTarget : null; // Só retorna se estiver razoavelmente na frente
    }

    // 🎯 CALCULAR POSIÇÃO NA ÓRBITA
    private Vector2 CalculateOrbitPosition(float angle)
    {
        float radians = angle * Mathf.Deg2Rad;
        Vector2 playerPos = (Vector2)playerTransform.position;
        Vector2 orbitPos = playerPos + new Vector2(
            Mathf.Cos(radians) * orbitRadius,
            Mathf.Sin(radians) * orbitRadius
        );
        return orbitPos;
    }

    void Update()
    {
        if (playerTransform == null)
        {
            Debug.LogError("❌ PlayerTransform é null no Update!");
            return;
        }

        // 🆕 ATUALIZAR TARGETS DOS PROJÉTEIS ORBITAIS
        if (autoAcquireTargets && Time.frameCount % 30 == 0) // Atualiza a cada 30 frames para performance
        {
            UpdateOrbitalTargets();
        }

        // Atualizar projéteis orbitais
        for (int i = activeOrbitals.Count - 1; i >= 0; i--)
        {
            OrbitingProjectile orbital = activeOrbitals[i];

            if (orbital == null || orbital.projectileObject == null)
            {
                activeOrbitals.RemoveAt(i);
                continue;
            }

            if (!orbital.isLaunching)
            {
                UpdateOrbitalMovement(orbital);
            }
            else
            {
                CheckOrbitalDestruction(orbital, i);
            }
        }

        // Spawn contínuo
        if (continuousSpawning)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval && activeOrbitals.Count < maxProjectiles)
            {
                SpawnOrbitingProjectile();
                spawnTimer = 0f;
            }
        }
    }

    // 🆕 ATUALIZAR TARGETS DOS PROJÉTEIS EXISTENTES
    private void UpdateOrbitalTargets()
    {
        foreach (var orbital in activeOrbitals)
        {
            if (!orbital.isLaunching && orbital.projectileController != null)
            {
                // Atualiza target apenas se o atual for nulo ou destruído
                if (orbital.target == null || !orbital.target.gameObject.activeInHierarchy)
                {
                    orbital.target = FindTargetForOrbital();
                    orbital.projectileController.Initialize(
                        orbital.target,
                        projectileDamage,
                        launchSpeed,
                        10f,
                        projectileElement
                    );
                }
            }
        }
    }

    private void UpdateOrbitalMovement(OrbitingProjectile orbital)
    {
        // CALCULAR ROTAÇÃO TOTAL
        float rotationThisFrame = orbitSpeed * Time.deltaTime;
        orbital.totalRotation += rotationThisFrame;

        // Atualizar ângulo atual
        orbital.currentAngle += rotationThisFrame;
        if (orbital.currentAngle >= 360f)
        {
            orbital.currentAngle -= 360f;
        }

        // VERIFICAR SE COMPLETOU VOLTAS SUFICIENTES
        float completedOrbits = orbital.totalRotation / 360f;

        if (showDebugInfo && completedOrbits >= orbital.orbitsCompleted + 0.5f)
        {
            Debug.Log($"🔄 Projétil completou {completedOrbits:F1} voltas | Necessárias: {numberOfOrbits}");
        }

        // Calcular e aplicar posição orbital
        Vector2 orbitPosition = CalculateOrbitPosition(orbital.currentAngle);
        orbital.transform.position = orbitPosition;

        // Rotacionar projétil para fora do centro
        Vector2 directionToCenter = ((Vector2)playerTransform.position - orbitPosition).normalized;
        float rotationAngle = Mathf.Atan2(directionToCenter.y, directionToCenter.x) * Mathf.Rad2Deg + 90f;
        orbital.transform.rotation = Quaternion.Euler(0f, 0f, rotationAngle);

        // VERIFICAR SE COMPLETOU O NÚMERO REQUERIDO DE VOLTAS
        if (completedOrbits >= numberOfOrbits)
        {
            LaunchProjectile(orbital);
        }
    }

    private void LaunchProjectile(OrbitingProjectile orbital)
    {
        if (orbital.isLaunching) return;

        orbital.isLaunching = true;

        // 🎯 CALCULAR DIREÇÃO DE LANÇAMENTO
        Vector2 launchDir = CalculateLaunchDirection(orbital);

        DebugLog($"🚀 LANÇANDO PROJÉTIL - Voltas: {orbital.totalRotation / 360f:F1} | Direção: {launchDir} | Target: {orbital.target?.name ?? "None"}");

        // 🔧 CONFIGURAR PROJÉTIL PARA CAUSAR DANO
        if (orbital.projectileController != null)
        {
            orbital.projectileController.damage = projectileDamage;
            orbital.projectileController.element = projectileElement;
            orbital.projectileController.ignoreTargetsDuringOrbit = false;

            // 🎯 LANÇAR
            orbital.projectileController.LaunchInDirection(launchDir, launchSpeed);
        }
        else
        {
            Debug.LogError("❌ ProjectileController2D não encontrado ao lançar!");
            Rigidbody2D rb = orbital.projectileObject.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = launchDir * launchSpeed;
            }
        }

        // 🎯 ROTACIONAR NA DIREÇÃO DO LANÇAMENTO
        float launchAngle = Mathf.Atan2(launchDir.y, launchDir.x) * Mathf.Rad2Deg;
        orbital.transform.rotation = Quaternion.Euler(0f, 0f, launchAngle);
    }

    // 🆕 MÉTODO MELHORADO PARA CALCULAR DIREÇÃO DE LANÇAMENTO
    private Vector2 CalculateLaunchDirection(OrbitingProjectile orbital)
    {
        // Se tem target específico, lança na direção do target
        if (orbital.target != null)
        {
            Vector2 directionToTarget = ((Vector2)orbital.target.position - (Vector2)orbital.transform.position).normalized;
            return directionToTarget;
        }

        // Se não tem target, lança na direção tangente à órbita
        return CalculateLaunchDirectionFromOrbit(orbital.currentAngle);
    }

    // 🎯 CALCULAR DIREÇÃO DE LANÇAMENTO BASEADA NA ÓRBITA
    private Vector2 CalculateLaunchDirectionFromOrbit(float orbitAngle)
    {
        float launchAngle = orbitAngle + 90f;
        float radians = launchAngle * Mathf.Deg2Rad;

        Vector2 direction = new Vector2(
            Mathf.Cos(radians),
            Mathf.Sin(radians)
        ).normalized;

        return direction;
    }

    private void CheckOrbitalDestruction(OrbitingProjectile orbital, int index)
    {
        if (orbital.projectileObject == null ||
            Vector2.Distance(playerTransform.position, orbital.transform.position) > maxLaunchDistance)
        {
            activeOrbitals.RemoveAt(index);
            DebugLog("🗑️ Projétil orbital removido");
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

        return false;
    }

    private GameObject GetProjectilePrefab()
    {
        // 🆕 PRIORIDADE PARA PREFAB DA SKILL DATA
        if (currentSkillData != null)
        {
            if (currentSkillData.projectilePrefab2D != null)
                return currentSkillData.projectilePrefab2D;

            if (currentSkillData.visualEffect != null)
                return currentSkillData.visualEffect;
        }

        // Fallback para o método antigo
        SkillData equippedSkill = SkillManager.Instance?.GetEquippedSkill();
        if (equippedSkill != null)
        {
            if (equippedSkill.projectilePrefab2D != null)
                return equippedSkill.projectilePrefab2D;

            if (equippedSkill.visualEffect != null)
                return equippedSkill.visualEffect;
        }

        return CreateBasicProjectilePrefab();
    }

    private GameObject CreateBasicProjectilePrefab()
    {
        GameObject projectile = new GameObject("BasicOrbitalProjectile");

        SpriteRenderer sr = projectile.AddComponent<SpriteRenderer>();
        Rigidbody2D rb = projectile.AddComponent<Rigidbody2D>();
        CircleCollider2D collider = projectile.AddComponent<CircleCollider2D>();
        ProjectileController2D controller = projectile.AddComponent<ProjectileController2D>();

        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        collider.isTrigger = true;
        collider.radius = 0.3f;

        // Criar sprite básico com cor do elemento
        Texture2D texture = new Texture2D(32, 32);
        Color elementColor = currentSkillData?.GetElementColor() ?? Color.yellow;

        for (int x = 0; x < 32; x++)
        {
            for (int y = 0; y < 32; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(16, 16));
                texture.SetPixel(x, y, dist < 16 ? elementColor : Color.clear);
            }
        }
        texture.Apply();

        sr.sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), Vector2.one * 0.5f);
        sr.color = elementColor;

        return projectile;
    }

    public override void RemoveEffect()
    {
        foreach (var orbital in activeOrbitals)
        {
            if (orbital.projectileObject != null)
            {
                Destroy(orbital.projectileObject);
            }
        }
        activeOrbitals.Clear();

        StopAllCoroutines();

        Debug.Log("🌀 Skill Orbital removida");
    }

    // 🔧 MÉTODOS DE DEBUG
    private void DebugLog(string message)
    {
        if (showDebugInfo) Debug.Log(message);
    }

    public int GetActiveOrbitalsCount()
    {
        return activeOrbitals.Count;
    }

    [ContextMenu("🎯 Testar Spawn Orbital")]
    public void TestOrbitalSpawn()
    {
        Debug.Log("=== 🧪 TESTE DE SPAWN ORBITAL ===");
        Debug.Log($"📍 Posição do Player: {playerTransform.position}");
        Debug.Log($"🔄 Voltas necessárias: {numberOfOrbits}");
        Debug.Log($"⚡ Dano orbital: {enableOrbitalDamage} (Raio: {orbitalDamageRadius}, Intervalo: {orbitalDamageInterval}s)");
        Debug.Log($"🎯 Modo de Targeting: {orbitalTargetingMode}");

        SpawnOrbitingProjectile();
        Debug.Log("=== FIM DO TESTE ===");
    }

    [ContextMenu("🔍 Diagnóstico Completo Orbital")]
    public void CompleteOrbitalDiagnostic()
    {
        Debug.Log("=== 🔍 DIAGNÓSTICO COMPLETO ORBITAL ===");
        Debug.Log($"📊 Configurações - Raio: {orbitRadius}, Velocidade: {orbitSpeed}, Voltas: {numberOfOrbits}");
        Debug.Log($"⚡ Dano Orbital - Ativo: {enableOrbitalDamage}, Raio: {orbitalDamageRadius}, Intervalo: {orbitalDamageInterval}s");
        Debug.Log($"🎯 Targeting - Modo: {orbitalTargetingMode}, Alcance: {targetAcquisitionRange}, Auto: {autoAcquireTargets}");
        Debug.Log($"🎯 Projéteis ativos: {activeOrbitals.Count}");

        for (int i = 0; i < activeOrbitals.Count; i++)
        {
            var orbital = activeOrbitals[i];
            if (orbital != null && orbital.projectileObject != null)
            {
                float completedOrbits = orbital.totalRotation / 360f;
                Debug.Log($"   #{i} - Ângulo: {orbital.currentAngle:F1}° | Voltas: {completedOrbits:F1}/{numberOfOrbits} | Lançando: {orbital.isLaunching} | Target: {orbital.target?.name ?? "None"}");
            }
        }

        Debug.Log("=== FIM DO DIAGNÓSTICO ===");
    }

    [ContextMenu("🔄 Alterar Modo de Targeting")]
    public void CycleTargetingMode()
    {
        orbitalTargetingMode = (OrbitalTargetingMode)(((int)orbitalTargetingMode + 1) % 4);
        Debug.Log($"🎯 Modo de Targeting alterado para: {orbitalTargetingMode}");
    }

    [ContextMenu("⚡ Ativar/Desativar Dano Orbital")]
    public void ToggleOrbitalDamage()
    {
        enableOrbitalDamage = !enableOrbitalDamage;
        Debug.Log($"⚡ Dano orbital {(enableOrbitalDamage ? "ATIVADO" : "DESATIVADO")}");

        // Aplicar a todos os projéteis existentes
        foreach (var orbital in activeOrbitals)
        {
            if (orbital.projectileController != null)
            {
                orbital.projectileController.orbitalDamageEnabled = enableOrbitalDamage;
            }
        }
    }

    // 🆕 MÉTODO ATUALIZADO PARA INTEGRAÇÃO COM SKILLDATA
    public void UpdateFromSkillData(SkillData skillData)
    {
        if (!skillData.ShouldUseOrbitalBehavior()) return;

        currentSkillData = skillData;

        // Configurações básicas orbitais
        orbitRadius = skillData.orbitRadius;
        orbitSpeed = skillData.orbitSpeed;
        numberOfOrbits = skillData.numberOfOrbits;
        continuousSpawning = skillData.continuousOrbitalSpawning;
        spawnInterval = skillData.orbitalSpawnInterval;
        launchSpeed = skillData.orbitalLaunchSpeed;
        maxLaunchDistance = skillData.maxOrbitalLaunchDistance;
        maxProjectiles = skillData.maxOrbitalProjectiles;

        // Configurações de dano
        projectileDamage = skillData.GetOrbitalProjectileDamage();
        projectileElement = skillData.element;

        // 🆕 Configurações de targeting
        orbitalTargetingMode = skillData.orbitalTargetingMode;
        autoAcquireTargets = skillData.autoAcquireTargets;
        targetAcquisitionRange = skillData.targetAcquisitionRange;

        // 🆕 Configurações de dano orbital (se disponíveis no SkillData)
        // Nota: Você pode adicionar essas propriedades ao SkillData se quiser
        // enableOrbitalDamage = skillData.enableOrbitalDamage;
        // orbitalDamageInterval = skillData.orbitalDamageInterval;
        // orbitalDamageRadius = skillData.orbitalDamageRadius;

        DebugLog($"🔄 Comportamento orbital atualizado da SkillData: {skillData.skillName}");
        DebugLog($"📊 Raio: {orbitRadius}m, Velocidade: {orbitSpeed}°/s, Voltas: {numberOfOrbits}");
        DebugLog($"🎯 Targeting: {orbitalTargetingMode}, Alcance: {targetAcquisitionRange}");
        DebugLog($"💥 Dano: {projectileDamage}, Elemento: {projectileElement}");
    }
}