using UnityEngine;

public abstract class SkillBehavior : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerStats playerStats;
    public SkillData skillData;

    // Co-op: quando true, esta é uma cópia COSMÉTICA rodando no fantoche do colega —
    // gera o visual mas NÃO aplica dano (o dano é da skill real do dono, já host-roteado).
    [HideInInspector] public bool cosmetico = false;

    public virtual void Initialize(PlayerStats stats)
    {
        playerStats = stats;
    }

    public abstract void ApplyEffect();

    public virtual void RemoveEffect() { }

    public virtual void ReducirCooldown(float segundos) { }
}
