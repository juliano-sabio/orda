using UnityEngine;

// CLASSE ABSTRATA - outras skills vão herdar dela
public abstract class SkillBehavior : MonoBehaviour
{
    [Header("Referências")]
    public PlayerStats playerStats; // Mudei de protected para public

    // MÉTODO CHAMADO QUANDO A SKILL É ADQUIRIDA
    public virtual void Initialize(PlayerStats stats)
    {
        playerStats = stats;
        Debug.Log($"Inicializando comportamento: {GetType().Name}");
    }

    // MÉTODO OBRIGATÓRIO - aplica o efeito especial
    public abstract void ApplyEffect();

    // MÉTODO OPCIONAL - remove o efeito (para skills temporárias)
    public virtual void RemoveEffect() { }
}