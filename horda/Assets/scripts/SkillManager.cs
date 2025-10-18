using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class skillmanager : MonoBehaviour
{
    // ⚡ Singleton - acesso global fácil
    public static skillmanager Instance;

    [Header("🎯 Lista de Skills Disponíveis no Jogo")]
    [SerializeField] private List<skilldata> availableSkills = new List<skilldata>();

    // Listas internas de controle
    private List<skilldata> activeSkills = new List<skilldata>(); // Skills que o jogador tem
    private Dictionary<skilldata, SkillBehavior> activeBehaviors = new Dictionary<skilldata, SkillBehavior>();

    private PlayerStats playerStats;

    private void Awake()
    {
        // Configura o singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persiste entre cenas
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Encontra o PlayerStats automaticamente
        playerStats = FindAnyObjectByType<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats não encontrado na cena!");
        }
    }

    // 🎁 MÉTODO PRINCIPAL - ADICIONAR UMA SKILL AO JOGADOR
    public void AddSkill(skilldata skillData)
    {
        // Verifica se já tem a skill
        if (activeSkills.Contains(skillData))
        {
            Debug.LogWarning($"Jogador já tem a skill: {skillData.skillName}");
            return;
        }

        Debug.Log($"🎉 Adicionando skill: {skillData.skillName}");

        // Adiciona à lista de skills ativas
        activeSkills.Add(skillData);

        // Aplica os efeitos de status (dano, velocidade, etc)
        ApplySkillEffects(skillData);

        // Se tiver comportamento especial, instancia e configura
        if (skillData.behavior != null)
        {
            AddSkillBehavior(skillData);
        }

        // Toca efeito sonoro se existir
        if (skillData.soundEffect != null)
        {
            AudioSource.PlayClipAtPoint(skillData.soundEffect, playerStats.transform.position);
        }

        // Mostra na UI
        //UIManager.Instance?.ShowSkillAcquired(skillData);
    }

    // 🔧 ADICIONA COMPORTAMENTO ESPECIAL DA SKILL
    private void AddSkillBehavior(skilldata skillData)
    {
        // Adiciona o componente de comportamento ao GameObject do SkillManager
        SkillBehavior behavior = gameObject.AddComponent(skillData.behavior.GetType()) as SkillBehavior;

        // Inicializa com referência ao PlayerStats
        behavior.Initialize(playerStats);

        // Aplica o efeito do comportamento
        behavior.ApplyEffect();

        // Guarda referência para poder remover depois
        activeBehaviors.Add(skillData, behavior);

        Debug.Log($"Comportamento especial adicionado: {skillData.behavior.GetType()}");
    }

    // 📊 APLICA OS EFEITOS DE STATUS (DANO, VELOCIDADE, ETC)
    private void ApplySkillEffects(skilldata skillData)
    {
        playerStats.ApplySkillModifiers(skillData);

        // Instancia efeito visual se existir
        if (skillData.visualEffect != null)
        {
            Instantiate(skillData.visualEffect, playerStats.transform.position, Quaternion.identity);
        }
    }

    // 🗑️ REMOVER UMA SKILL (ÚTIL PARA POWER-UPS TEMPORÁRIOS)
    public void RemoveSkill(skilldata skillData)
    {
        if (!activeSkills.Contains(skillData)) return;

        Debug.Log($"Removendo skill: {skillData.skillName}");

        activeSkills.Remove(skillData);

        // Remove comportamento especial se existir
        if (activeBehaviors.ContainsKey(skillData))
        {
            activeBehaviors[skillData].RemoveEffect();
            Destroy(activeBehaviors[skillData]);
            activeBehaviors.Remove(skillData);
        }

        // Remove os modificadores de status
        playerStats.RemoveSkillModifiers(skillData);
    }

    // 🎲 PEGA SKILLS ALEATÓRIAS DISPONÍVEIS (PARA SELEÇÃO)
    public List<skilldata> GetRandomSkills(int count)
    {
        // Filtra apenas skills que o jogador ainda não tem
        var unacquired = availableSkills.Where(s => !activeSkills.Contains(s)).ToList();

        // Embaralha a lista
        var shuffled = unacquired.OrderBy(x => Random.value).ToList();

        // Pega a quantidade pedida (ou menos se não tiver muitas disponíveis)
        return shuffled.Take(Mathf.Min(count, shuffled.Count)).ToList();
    }

    // 🔍 VERIFICAR SE JOGADOR TEM UMA SKILL ESPECÍFICA
    public bool HasSkill(skilldata skillData)
    {
        return activeSkills.Contains(skillData);
    }
}