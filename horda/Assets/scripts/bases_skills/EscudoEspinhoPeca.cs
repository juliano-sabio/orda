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

        if (!controlador.cosmetico) // co-op: cópia cosmética não aplica dano (só o visual)
        {
            inimigo.ReceberDano(controlador.GetDano());

            // EspinhosVenenosos2 — aplica veneno ao acertar
            if (SkillEvolutionManager.Tem(SkillEvolutionType.EspinhosVenenosos2))
                EvolutionFX.AplicarVeneno(inimigo, 2f, 5f);
        }

        hitsRestantes--;
        if (hitsRestantes <= 0)
        {
            // EspinhosExplosivos — explosão ao esgotar hits
            if (SkillEvolutionManager.Tem(SkillEvolutionType.EspinhosExplosivos))
                EvolutionFX.SpawnExplosao(transform.position, 2.5f,
                    controlador.GetDano() * 1.5f, new Color(0.3f, 1f, 0.4f),
                    controlador);
            controlador.OnEnemyHit();
        }
    }
}
