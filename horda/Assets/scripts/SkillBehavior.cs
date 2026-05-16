using UnityEngine;

public abstract class SkillBehavior : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerStats playerStats;

    public virtual void Initialize(PlayerStats stats)
    {
        playerStats = stats;
    }

    public abstract void ApplyEffect();

    public virtual void RemoveEffect() { }
}
