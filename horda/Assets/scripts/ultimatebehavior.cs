using UnityEngine;

public abstract class UltimateBehavior : MonoBehaviour
{
    protected PlayerStats playerStats;
    protected float radius;
    protected float duration;

    public virtual void Initialize(PlayerStats stats, float effectRadius, float effectDuration)
    {
        playerStats = stats;
        radius = effectRadius;
        duration = effectDuration;
    }

    public abstract void ActivateUltimate(Vector2 position);
    public virtual void DeactivateUltimate() { }
}