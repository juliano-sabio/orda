using UnityEngine;

public class EscudoEspinhoPeca : MonoBehaviour
{
    private EscudoEspinhosoSkillBehavior controlador;
    private int hitsRestantes;

    public void Initialize(EscudoEspinhosoSkillBehavior controlador)
    {
        this.controlador = controlador;
        hitsRestantes = controlador.maxHits;
    }

    public void ResetarHits()
    {
        hitsRestantes = controlador != null ? controlador.maxHits : 3;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (controlador == null || other == null) return;

        if (other.GetComponentInParent<ProjetilHomingPrincesa>(true)   != null) return;
        if (other.GetComponentInParent<ProjetilEspecialPrincesa>(true) != null) return;

        InimigoController inimigo = other.GetComponent<InimigoController>();
        if (inimigo == null) inimigo = other.GetComponentInParent<InimigoController>();

        if (inimigo == null || inimigo.estaMorrendo) return;

        inimigo.ReceberDano(controlador.GetDano());

        hitsRestantes--;
        if (hitsRestantes <= 0)
            controlador.OnEnemyHit();
    }
}
