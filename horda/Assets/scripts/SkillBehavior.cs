using UnityEngine;

// ?? CLASSE ABSTRATA - outras skills v�o herdar dela
public abstract class SkillBehavior : MonoBehaviour
{
    [Header("Refer�ncias")]
    protected PlayerStats playerStats;

    // ? M�TODO CHAMADO QUANDO A SKILL � ADQUIRIDA
    public virtual void Initialize(PlayerStats stats)
    {
        playerStats = stats;
        Debug.Log($"Inicializando comportamento: {GetType().Name}");
    }

    // ?? M�TODO OBRIGAT�RIO - aplica o efeito especial
    public abstract void ApplyEffect();

    // ??? M�TODO OPCIONAL - remove o efeito (para skills tempor�rias)
    public virtual void RemoveEffect() { }
}