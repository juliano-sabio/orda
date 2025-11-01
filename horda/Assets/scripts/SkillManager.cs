using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    // ⚡ Singleton - acesso global fácil
    public static SkillManager Instance;

    [Header("🎯 Lista de Skills Disponíveis no Jogo")]
    [SerializeField] private List<SkillData> availableSkills = new List<SkillData>();

    [Header("🔧 Modificadores de Skills Disponíveis")]
    [SerializeField] private List<SkillModifierData> availableModifiers = new List<SkillModifierData>();

    // Listas internas de controle
    private List<SkillData> activeSkills = new List<SkillData>();
    private List<SkillModifierData> activeModifiers = new List<SkillModifierData>();

    private PlayerStats playerStats;

    // 🆕 DICIONÁRIO PARA SKILLS ESPECIAIS
    private Dictionary<SpecificSkillType, float> specialSkillValues = new Dictionary<SpecificSkillType, float>();

    // 🆕 EVENTOS PARA UI
    public System.Action<SkillData> OnSkillAdded;
    public System.Action<SkillModifierData> OnModifierAdded;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        playerStats = FindAnyObjectByType<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats não encontrado na cena!");
        }

        // 🆕 INICIALIZA DICIONÁRIO DE SKILLS ESPECIAIS
        InitializeSpecialSkillsDictionary();

        // 🆕 VERIFICA SE HÁ SKILLS DISPONÍVEIS
        CheckAvailableSkills();

        Debug.Log("✅ SkillManager inicializado com sistema de elementos!");
    }

    // 🆕 INICIALIZA DICIONÁRIO DE SKILLS ESPECIAIS
    private void InitializeSpecialSkillsDictionary()
    {
        foreach (SpecificSkillType type in System.Enum.GetValues(typeof(SpecificSkillType)))
        {
            if (type != SpecificSkillType.None)
            {
                specialSkillValues[type] = 0f;
            }
        }
    }

    // 🆕 VERIFICA SE HÁ SKILLS DISPONÍVEIS
    private void CheckAvailableSkills()
    {
        if (availableSkills.Count == 0)
        {
            Debug.LogWarning("⚠️ Lista de skills disponíveis está vazia! Adicione SkillData ScriptableObjects no Inspector.");
        }
        else
        {
            Debug.Log($"✅ {availableSkills.Count} skills disponíveis carregadas.");

            // 🆕 LOG DE SKILLS POR ELEMENTO
            var skillsByElement = availableSkills.GroupBy(s => s.element);
            foreach (var group in skillsByElement)
            {
                Debug.Log($"   {GetElementIcon(group.Key)} {group.Key}: {group.Count()} skills");
            }
        }
    }

    // 🎁 MÉTODO PRINCIPAL - ADICIONAR UMA SKILL AO JOGADOR
    public void AddSkill(SkillData skillData)
    {
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats não encontrado!");
            return;
        }

        if (activeSkills.Contains(skillData))
        {
            Debug.LogWarning($"Jogador já tem a skill: {skillData.skillName}");
            return;
        }

        Debug.Log($"🎉 Adicionando skill: {skillData.skillName} (Elemento: {skillData.element})");

        // Adiciona à lista de skills ativas
        activeSkills.Add(skillData);

        // Aplica os efeitos da skill
        ApplySkillEffects(skillData);

        // 🆕 APLICA EFEITOS ESPECIAIS BASEADOS NO TIPO
        ApplySpecialSkillEffects(skillData);

        // 🆕 NOTIFICA UI ATRAVÉS DE EVENTO
        OnSkillAdded?.Invoke(skillData);

        // Toca efeito sonoro
        if (skillData.soundEffect != null)
        {
            AudioSource.PlayClipAtPoint(skillData.soundEffect, playerStats.transform.position);
        }

        // NOTIFICA UI CORRETAMENTE
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowSkillAcquired(skillData.skillName, skillData.description);
        }
        else
        {
            Debug.LogWarning("UIManager não encontrado!");
        }

        // ATUALIZA UI DO PLAYERSTATS
        playerStats.ForceUIUpdate();

        Debug.Log($"✅ Skill {skillData.skillName} adicionada com sucesso!");
    }

    // 🆕 ATUALIZADO: ApplySkillEffects com sistema de elementos
    private void ApplySkillEffects(SkillData skillData)
    {
        // Aplica bônus de status básicos
        if (skillData.healthBonus > 0)
        {
            playerStats.maxHealth += skillData.healthBonus;
            playerStats.health += skillData.healthBonus;
            Debug.Log($"❤️ Vida aumentada em {skillData.healthBonus}");
        }

        if (skillData.attackBonus > 0)
        {
            playerStats.attack += skillData.attackBonus;
            Debug.Log($"⚔️ Ataque aumentado em {skillData.attackBonus}");
        }

        if (skillData.defenseBonus > 0)
        {
            playerStats.defense += skillData.defenseBonus;
            Debug.Log($"🛡️ Defesa aumentada em {skillData.defenseBonus}");
        }

        if (skillData.speedBonus > 0)
        {
            playerStats.speed += skillData.speedBonus;
            Debug.Log($"🏃 Velocidade aumentada em {skillData.speedBonus}");
        }

        // 🆕 MUDA ELEMENTO SE A SKILL TIVER ELEMENTO ESPECÍFICO
        if (skillData.element != PlayerStats.Element.None)
        {
            playerStats.ChangeElement(skillData.element);
            Debug.Log($"⚡ Elemento alterado para: {skillData.element}");
        }

        // APLICA MODIFICADORES DE SKILLS CORRETAMENTE
        foreach (var modifierData in skillData.skillModifiers)
        {
            AddSkillModifier(modifierData);
        }

        // Efeito visual
        if (skillData.visualEffect != null)
        {
            Instantiate(skillData.visualEffect, playerStats.transform.position, Quaternion.identity);
        }

        // 🆕 EFEITO VISUAL BASEADO NO ELEMENTO
        ApplyElementalVisualEffect(skillData.element, playerStats.transform.position);
    }

    // 🆕 NOVO: Aplica efeito visual baseado no elemento
    private void ApplyElementalVisualEffect(PlayerStats.Element element, Vector3 position)
    {
        // Aqui você pode adicionar partículas específicas para cada elemento
        switch (element)
        {
            case PlayerStats.Element.Fire:
                Debug.Log("🔥 Efeito visual de Fogo aplicado");
                break;
            case PlayerStats.Element.Ice:
                Debug.Log("❄️ Efeito visual de Gelo aplicado");
                break;
            case PlayerStats.Element.Lightning:
                Debug.Log("⚡ Efeito visual de Raio aplicado");
                break;
            case PlayerStats.Element.Poison:
                Debug.Log("☠️ Efeito visual de Veneno aplicado");
                break;
        }
    }

    // 🆕 APLICA EFEITOS ESPECIAIS BASEADOS NO TIPO DE SKILL
    private void ApplySpecialSkillEffects(SkillData skillData)
    {
        if (skillData.specificType != SpecificSkillType.None)
        {
            specialSkillValues[skillData.specificType] += skillData.specialValue;

            switch (skillData.specificType)
            {
                case SpecificSkillType.HealthRegen:
                    Debug.Log($"💚 Regeneração de vida aumentada: {skillData.specialValue}/s");
                    StartCoroutine(HealthRegenCoroutine(skillData.specialValue));
                    break;
                case SpecificSkillType.CriticalStrike:
                    Debug.Log($"🎯 Chance de crítico aumentada: {skillData.specialValue}%");
                    break;
                case SpecificSkillType.LifeSteal:
                    Debug.Log($"🩸 Life steal aumentado: {skillData.specialValue}%");
                    break;
                case SpecificSkillType.MovementSpeed:
                    Debug.Log($"🏃‍♂️ Velocidade de movimento aumentada: {skillData.specialValue}%");
                    playerStats.speed *= (1f + skillData.specialValue / 100f);
                    break;
                case SpecificSkillType.AttackSpeed:
                    Debug.Log($"⚡ Velocidade de ataque aumentada: {skillData.specialValue}%");
                    playerStats.attackActivationInterval /= (1f + skillData.specialValue / 100f);
                    break;
                case SpecificSkillType.AreaDamage:
                    Debug.Log($"💥 Área de dano aumentada: {skillData.specialValue}%");
                    break;
                case SpecificSkillType.Shield:
                    Debug.Log($"🛡️ Escudo ativado: {skillData.specialValue} de proteção");
                    playerStats.defense += skillData.specialValue;
                    break;
                case SpecificSkillType.ElementalMastery:
                    Debug.Log($"🎨 Mestre Elemental: {skillData.specialValue}% de bônus");
                    // Aumenta o bônus elemental
                    break;
            }
        }
    }

    // 🆕 CORROTINA PARA REGENERAÇÃO DE VIDA
    private System.Collections.IEnumerator HealthRegenCoroutine(float regenAmount)
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            if (playerStats != null && playerStats.health < playerStats.maxHealth)
            {
                playerStats.health = Mathf.Min(playerStats.maxHealth, playerStats.health + regenAmount);
                playerStats.ForceUIUpdate();
            }
        }
    }

    // 🆕 ATUALIZADO: AddSkillModifier com sistema de elementos
    public void AddSkillModifier(SkillModifierData modifierData)
    {
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats não encontrado!");
            return;
        }

        if (activeModifiers.Contains(modifierData))
        {
            Debug.LogWarning($"Jogador já tem o modificador: {modifierData.modifierName}");
            return;
        }

        Debug.Log($"✨ Adicionando modificador: {modifierData.modifierName} para {modifierData.targetSkillName}");

        // CONVERTE PARA O SkillModifier DO PlayerStats
        PlayerStats.SkillModifier playerStatsModifier = new PlayerStats.SkillModifier
        {
            modifierName = modifierData.modifierName,
            targetSkillName = modifierData.targetSkillName,
            damageMultiplier = modifierData.damageMultiplier,
            defenseMultiplier = modifierData.defenseMultiplier,
            element = modifierData.element,
            duration = modifierData.duration
        };

        activeModifiers.Add(modifierData);
        playerStats.AddSkillModifier(playerStatsModifier);

        // 🆕 NOTIFICA UI ATRAVÉS DE EVENTO
        OnModifierAdded?.Invoke(modifierData);

        // NOTIFICA UI CORRETAMENTE
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowModifierAcquired(modifierData.modifierName, modifierData.targetSkillName);
        }

        playerStats.ForceUIUpdate();

        Debug.Log($"✅ Modificador {modifierData.modifierName} aplicado em {modifierData.targetSkillName}");
    }

    // 🗑️ REMOVER UMA SKILL
    public void RemoveSkill(SkillData skillData)
    {
        if (playerStats == null || !activeSkills.Contains(skillData)) return;

        Debug.Log($"🗑️ Removendo skill: {skillData.skillName}");
        activeSkills.Remove(skillData);

        // Remove bônus de status
        if (skillData.healthBonus > 0)
        {
            playerStats.maxHealth -= skillData.healthBonus;
            playerStats.health = Mathf.Min(playerStats.health, playerStats.maxHealth);
        }

        if (skillData.attackBonus > 0)
        {
            playerStats.attack -= skillData.attackBonus;
        }

        if (skillData.defenseBonus > 0)
        {
            playerStats.defense -= skillData.defenseBonus;
        }

        if (skillData.speedBonus > 0)
        {
            playerStats.speed -= skillData.speedBonus;
        }

        // 🆕 REMOVE EFEITOS ESPECIAIS
        RemoveSpecialSkillEffects(skillData);

        // Remove modificadores
        foreach (var modifier in skillData.skillModifiers)
        {
            RemoveSkillModifier(modifier);
        }

        playerStats.ForceUIUpdate();

        Debug.Log($"✅ Skill {skillData.skillName} removida com sucesso!");
    }

    // 🆕 REMOVE EFEITOS ESPECIAIS
    private void RemoveSpecialSkillEffects(SkillData skillData)
    {
        if (skillData.specificType != SpecificSkillType.None)
        {
            specialSkillValues[skillData.specificType] -= skillData.specialValue;

            switch (skillData.specificType)
            {
                case SpecificSkillType.MovementSpeed:
                    playerStats.speed /= (1f + skillData.specialValue / 100f);
                    break;
                case SpecificSkillType.AttackSpeed:
                    playerStats.attackActivationInterval *= (1f + skillData.specialValue / 100f);
                    break;
                case SpecificSkillType.Shield:
                    playerStats.defense -= skillData.specialValue;
                    break;
            }
        }
    }

    // 🗑️ REMOVER MODIFICADOR
    public void RemoveSkillModifier(SkillModifierData modifierData)
    {
        if (!activeModifiers.Contains(modifierData)) return;

        Debug.Log($"🗑️ Removendo modificador: {modifierData.modifierName}");
        activeModifiers.Remove(modifierData);

        playerStats.ForceUIUpdate();

        Debug.Log($"✅ Modificador {modifierData.modifierName} removido com sucesso!");
    }

    // 🎲 PEGA SKILLS ALEATÓRIAS DISPONÍVEIS
    public List<SkillData> GetRandomSkills(int count)
    {
        var unacquired = availableSkills.Where(s => !activeSkills.Contains(s)).ToList();

        if (unacquired.Count == 0)
        {
            Debug.Log("Todas as skills já foram adquiridas!");
            return new List<SkillData>();
        }

        var shuffled = unacquired.OrderBy(x => Random.value).ToList();
        return shuffled.Take(Mathf.Min(count, shuffled.Count)).ToList();
    }

    // 🎲 PEGA MODIFICADORES ALEATÓRIOS DISPONÍVEIS
    public List<SkillModifierData> GetRandomModifiers(int count)
    {
        var unacquired = availableModifiers.Where(m => !activeModifiers.Contains(m)).ToList();

        if (unacquired.Count == 0)
        {
            Debug.Log("Todos os modificadores já foram adquiridos!");
            return new List<SkillModifierData>();
        }

        var shuffled = unacquired.OrderBy(x => Random.value).ToList();
        return shuffled.Take(Mathf.Min(count, shuffled.Count)).ToList();
    }

    // 🔍 VERIFICAR SE JOGADOR TEM UMA SKILL ESPECÍFICA
    public bool HasSkill(SkillData skillData)
    {
        return activeSkills.Contains(skillData);
    }

    // 🔍 VERIFICAR SE JOGADOR TEM UM MODIFICADOR ESPECÍFICO
    public bool HasModifier(SkillModifierData modifier)
    {
        return activeModifiers.Contains(modifier);
    }

    // 🔍 VERIFICAR SE JOGADOR TEM UMA SKILL PELO NOME
    public bool HasSkillByName(string skillName)
    {
        return activeSkills.Any(s => s.skillName == skillName);
    }

    // 🔍 VERIFICAR SE JOGADOR TEM UM MODIFICADOR PELO NOME
    public bool HasModifierByName(string modifierName)
    {
        return activeModifiers.Any(m => m.modifierName == modifierName);
    }

    // 🆕 VERIFICAR VALOR DE SKILL ESPECIAL
    public float GetSpecialSkillValue(SpecificSkillType skillType)
    {
        return specialSkillValues.ContainsKey(skillType) ? specialSkillValues[skillType] : 0f;
    }

    // 📊 GETTERS PARA INFORMAÇÕES
    public List<SkillData> GetActiveSkills() => new List<SkillData>(activeSkills);
    public List<SkillModifierData> GetActiveModifiers() => new List<SkillModifierData>(activeModifiers);
    public List<SkillData> GetAvailableSkills() => new List<SkillData>(availableSkills);
    public List<SkillModifierData> GetAvailableModifiers() => new List<SkillModifierData>(availableModifiers);

    // 🎯 MÉTODO PARA ADICIONAR SKILL POR NOME
    public bool AddSkillByName(string skillName)
    {
        var skill = availableSkills.FirstOrDefault(s => s.skillName == skillName);
        if (skill != null)
        {
            AddSkill(skill);
            return true;
        }

        Debug.LogWarning($"Skill não encontrada: {skillName}");
        return false;
    }

    // 🎯 MÉTODO PARA ADICIONAR MODIFICADOR POR NOME
    public bool AddModifierByName(string modifierName)
    {
        var modifier = availableModifiers.FirstOrDefault(m => m.modifierName == modifierName);
        if (modifier != null)
        {
            AddSkillModifier(modifier);
            return true;
        }

        Debug.LogWarning($"Modificador não encontrado: {modifierName}");
        return false;
    }

    // 🎲 MÉTODO PARA ADICIONAR SKILL ALEATÓRIA (PARA TESTE)
    public void AddRandomSkill()
    {
        var randomSkills = GetRandomSkills(1);
        if (randomSkills.Count > 0)
        {
            AddSkill(randomSkills[0]);
        }
        else
        {
            Debug.Log("Não há mais skills disponíveis!");
        }
    }

    // 🎲 MÉTODO PARA ADICIONAR MODIFICADOR ALEATÓRIO (PARA TESTE)
    public void AddRandomModifier()
    {
        var randomModifiers = GetRandomModifiers(1);
        if (randomModifiers.Count > 0)
        {
            AddSkillModifier(randomModifiers[0]);
        }
        else
        {
            Debug.Log("Não há mais modificadores disponíveis!");
        }
    }

    // 🆕 MÉTODO PARA ADICIONAR SKILLS DE TESTE (PARA DESENVOLVIMENTO)
    public void AddTestSkills()
    {
        Debug.Log("🧪 Adicionando skills de teste...");

        if (availableSkills.Count > 0)
        {
            // 🆕 TENTA ENCONTRAR SKILLS COM ELEMENTOS DIFERENTES
            var fireSkill = availableSkills.FirstOrDefault(s => s.element == PlayerStats.Element.Fire);
            var iceSkill = availableSkills.FirstOrDefault(s => s.element == PlayerStats.Element.Ice);

            if (fireSkill != null) AddSkill(fireSkill);
            if (iceSkill != null) AddSkill(iceSkill);

            // Se não encontrou skills com elementos, usa as primeiras disponíveis
            if (fireSkill == null && iceSkill == null && availableSkills.Count > 1)
            {
                AddSkill(availableSkills[0]);
                AddSkill(availableSkills[1]);
            }
        }
        else
        {
            Debug.LogWarning("Nenhuma skill disponível para teste!");
        }
    }

    // 🆕 ATUALIZADO: CheckIntegrationStatus com informações de elementos
    public void CheckIntegrationStatus()
    {
        Debug.Log("🔍 Verificando integração do SkillManager...");

        if (playerStats != null)
            Debug.Log("✅ PlayerStats: CONECTADO");
        else
            Debug.LogError("❌ PlayerStats: NÃO CONECTADO");

        if (UIManager.Instance != null)
            Debug.Log("✅ UIManager: CONECTADO");
        else
            Debug.LogError("❌ UIManager: NÃO CONECTADO");

        Debug.Log($"📊 Skills Ativas: {activeSkills.Count}");
        Debug.Log($"🔧 Modificadores Ativos: {activeModifiers.Count}");
        Debug.Log($"🎯 Skills Disponíveis: {availableSkills.Count}");

        // 🆕 INFORMAÇÕES DE ELEMENTOS
        var elementsCount = activeSkills.GroupBy(s => s.element);
        Debug.Log("🎨 Skills por Elemento:");
        foreach (var group in elementsCount)
        {
            Debug.Log($"   {GetElementIcon(group.Key)} {group.Key}: {group.Count()} skills");
        }
    }

    // 🗑️ MÉTODO PARA LIMPAR TODAS AS SKILLS (PARA DEBUG)
    public void ClearAllSkills()
    {
        foreach (var skill in activeSkills.ToList())
        {
            RemoveSkill(skill);
        }

        foreach (var modifier in activeModifiers.ToList())
        {
            RemoveSkillModifier(modifier);
        }

        Debug.Log("🧹 Todas as skills e modificadores foram removidos!");
    }

    // 🆕 MÉTODO PARA ATUALIZAR UI MANUALMENTE
    public void ForceUIUpdate()
    {
        if (playerStats != null)
        {
            playerStats.ForceUIUpdate();
        }
    }

    // 🆕 MÉTODO CHAMADO QUANDO O JOGADOR SOBE DE LEVEL
    public void OnPlayerLevelUp(int newLevel)
    {
        Debug.Log($"🎉 Player subiu para o nível {newLevel}! Verificando novas skills...");

        // Exemplo: Dar uma skill aleatória a cada 3 níveis
        if (newLevel % 3 == 0 && availableSkills.Count > 0)
        {
            Debug.Log($"🎁 Presente de level up! Adicionando skill aleatória...");
            AddRandomSkill();
        }

        // 🆕 DESBLOQUEIA SKILLS ESPECIAIS EM NÍVEIS ESPECÍFICOS
        if (newLevel == 5)
        {
            Debug.Log("⭐ Nível 5 alcançado! Skills especiais disponíveis.");
        }
        else if (newLevel == 10)
        {
            Debug.Log("🌟🌟 Nível 10 alcançado! Skills raras disponíveis.");
        }
    }

    // 🆕 MÉTODO PARA ADICIONAR SKILLS AO MANAGER DINAMICAMENTE
    public void AddAvailableSkill(SkillData skillData)
    {
        if (!availableSkills.Contains(skillData))
        {
            availableSkills.Add(skillData);
            Debug.Log($"✅ Skill {skillData.skillName} adicionada às disponíveis");
        }
    }

    // 🆕 MÉTODO PARA REMOVER SKILL DAS DISPONÍVEIS
    public void RemoveAvailableSkill(SkillData skillData)
    {
        if (availableSkills.Contains(skillData))
        {
            availableSkills.Remove(skillData);
            Debug.Log($"❌ Skill {skillData.skillName} removida das disponíveis");
        }
    }

    // 🆕 MÉTODO PARA OBTER ÍCONE DO ELEMENTO
    private string GetElementIcon(PlayerStats.Element element)
    {
        switch (element)
        {
            case PlayerStats.Element.None: return "⚪";
            case PlayerStats.Element.Fire: return "🔥";
            case PlayerStats.Element.Ice: return "❄️";
            case PlayerStats.Element.Lightning: return "⚡";
            case PlayerStats.Element.Poison: return "☠️";
            case PlayerStats.Element.Earth: return "🌍";
            case PlayerStats.Element.Wind: return "💨";
            default: return "⚪";
        }
    }
}