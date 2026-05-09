using UnityEngine;

public class EscudoPeca : MonoBehaviour
{
    private EscudoRotativoSkillBehavior controlador;

    public void Initialize(EscudoRotativoSkillBehavior controlador)
    {
        this.controlador = controlador;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (controlador == null || other == null) return;

Vector2 direcao = Vector2.zero;
        bool ehProjetilInimigo = false;

        // Detectar projetil_inimigo (com efeitos)
        projetil_inimigo proj = other.GetComponent<projetil_inimigo>();
        if (proj != null)
        {
            // direcao pública no script
            direcao = proj.direcao.sqrMagnitude > 0.01f
                ? proj.direcao
                : ObterDirecaoDoRigidbody(other);
            ehProjetilInimigo = true;
        }

        // Detectar ProjetilInimigoDano (simples)
        if (!ehProjetilInimigo)
        {
            ProjetilInimigoDano projDano = other.GetComponent<ProjetilInimigoDano>();
            if (projDano != null)
            {
                direcao = ObterDirecaoDoRigidbody(other);
                ehProjetilInimigo = true;
            }
        }

        if (!ehProjetilInimigo) return;

        // Desativar imediatamente para não acertar o player no mesmo frame
        other.gameObject.SetActive(false);
        Destroy(other.gameObject);

        // Refletir na direção de onde o projétil veio
        if (direcao.sqrMagnitude > 0.01f)
        {
            controlador.ReflectirProjetil(direcao, transform.position);
        }
    }

    private Vector2 ObterDirecaoDoRigidbody(Collider2D col)
    {
        Rigidbody2D rb = col.attachedRigidbody;
        if (rb != null && rb.linearVelocity.sqrMagnitude > 0.01f)
            return rb.linearVelocity.normalized;
        return Vector2.zero;
    }
}
