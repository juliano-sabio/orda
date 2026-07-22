using System.Collections.Generic;
using UnityEngine;

public abstract class SkillBehavior : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerStats playerStats;
    public SkillData skillData;

    // Co-op: quando true, esta é uma cópia COSMÉTICA rodando no fantoche do colega —
    // gera o visual mas NÃO aplica dano (o dano é da skill real do dono, já host-roteado).
    [HideInInspector] public bool cosmetico = false;

    // Co-op: evoluções do DONO replicadas para esta cópia cosmética. A cópia NÃO pode ler o
    // SkillEvolutionManager local (que reflete as evoluções do jogador DESTA máquina), então o
    // que o dono evoluiu chega por rede e fica aqui. Assim o puppet desenha o visual lendário.
    readonly HashSet<SkillEvolutionType> evolucoesReplicadas = new HashSet<SkillEvolutionType>();

    public void MarcarEvolucaoReplicada(SkillEvolutionType tipo) => evolucoesReplicadas.Add(tipo);

    // Fonte CERTA de "tem esta evolução?": no dono usa o manager local; na cópia cosmética usa
    // o conjunto replicado do dono. Use isto no lugar de SkillEvolutionManager.Tem(...) sempre
    // que a evolução afetar o VISUAL (pra o efeito aparecer também na tela do colega).
    protected bool TemEvolucao(SkillEvolutionType tipo)
        => cosmetico ? evolucoesReplicadas.Contains(tipo) : SkillEvolutionManager.Tem(tipo);

    public virtual void Initialize(PlayerStats stats)
    {
        playerStats = stats;
    }

    public abstract void ApplyEffect();

    public virtual void RemoveEffect() { }

    public virtual void ReducirCooldown(float segundos) { }
}
