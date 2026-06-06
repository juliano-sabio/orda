using System.Collections.Generic;
using UnityEngine;

// Gerencia quais evoluções estão ativas para o player (acumulativas).
public class SkillEvolutionManager : MonoBehaviour
{
    public static SkillEvolutionManager Instance { get; private set; }

    // Conjunto de evoluções ativas — acumulativo, sem chave de skill
    readonly HashSet<SkillEvolutionType> evolucoesAtivas = new HashSet<SkillEvolutionType>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── API pública ───────────────────────────────────────────────────────────

    /// <summary>Verifica se uma evolução específica está ativa.</summary>
    public bool EvolucaoAtiva(SkillEvolutionType tipo) => evolucoesAtivas.Contains(tipo);

    /// <summary>Atalho estático para verificação sem referência ao Instance.</summary>
    public static bool Tem(SkillEvolutionType tipo) => Instance != null && Instance.evolucoesAtivas.Contains(tipo);

    // ── Métodos deprecated (mantidos para compatibilidade de compilação) ──────

    /// <deprecated>Use EvolucaoAtiva(SkillEvolutionType) ou Tem(SkillEvolutionType).</deprecated>
    public bool TemEvolucao(SpecificSkillType skill) => false;

    /// <deprecated>Use Tem(SkillEvolutionType).</deprecated>
    public SkillEvolutionType ObterEvolucao(SpecificSkillType skill) => SkillEvolutionType.Nenhuma;

    /// <deprecated>Use Tem(SkillEvolutionType).</deprecated>
    public bool EhEvolucao(SpecificSkillType skill, SkillEvolutionType tipo) => false;

    // ── Aplicação e reset ─────────────────────────────────────────────────────

    public void AplicarEvolucao(SkillEvolutionData data)
    {
        if (data == null) return;
        evolucoesAtivas.Add(data.tipoEvolucao);
        Debug.Log($"[Evolução] +{data.tipoEvolucao} (total: {evolucoesAtivas.Count})");

        var player = FindFirstObjectByType<PlayerStats>();
        if (player == null) return;

        // Notifica behaviors com interface IEvoluivel
        foreach (var b in player.GetComponents<SkillBehavior>())
            if (b is IEvoluivel ev) ev.OnEvolucaoAplicada(data.tipoEvolucao);

        // Aplica evoluções em behaviors de projétil orbital (Espada Rotatória, Espiral)
        var orbital = player.GetComponent<OrbitingProjectileSkillBehavior>();
        if (orbital != null)
        {
            switch (data.tipoEvolucao)
            {
                case SkillEvolutionType.EspadaOrbitalDupla:
                    orbital.maxProjectiles = Mathf.Max(orbital.maxProjectiles, 2);
                    orbital.numberOfOrbits = 2;
                    break;
                case SkillEvolutionType.EspadaSonica:
                    orbital.orbitSpeed      *= 2f;
                    orbital.projectileDamage *= 1.5f;
                    break;
                case SkillEvolutionType.EspiralDupla:
                    orbital.maxProjectiles = Mathf.Max(orbital.maxProjectiles, 2);
                    break;
            }
        }

        // Aplica evoluções em behaviors de projétil normal (Spirit Ball, Homing)
        var projBehaviors = player.GetComponents<SkillBehavior>();
        foreach (var b in projBehaviors)
        {
            var proj = b as PassiveProjectileSkill2D;
            if (proj == null) continue;
            switch (data.tipoEvolucao)
            {
                case SkillEvolutionType.SpiritBallDupla:
                case SkillEvolutionType.HomingMultiplo:
                    // Modifica via SkillData se possível
                    break;
            }
        }
    }

    public void Resetar() => evolucoesAtivas.Clear();
}

// Interface opcional para behaviors que querem ser notificados imediatamente
public interface IEvoluivel
{
    void OnEvolucaoAplicada(SkillEvolutionType tipo);
}
