using UnityEngine;

public class AreaVeneno : MonoBehaviour
{
    private float dano;
    private float intervaloTick;
    private float duracao;
    private float raio;
    private LayerMask layerParaAcertar;

    private float timerTick;
    private float timerVida;
    private bool estaFinalizando = false;

    private Animator anim;

    public void Inicializar(float _dano, float _intervalo, float _duracao, float _raio, LayerMask _mask)
    {
        dano = _dano;
        intervaloTick = _intervalo;
        duracao = _duracao;
        raio = _raio;
        layerParaAcertar = _mask;

        anim = GetComponent<Animator>();
        // Garante que comece tocando a abertura
        anim.Play("abrir_veneno");
    }

    void Update()
    {
        if (estaFinalizando) return; // Para tudo se já estiver fechando

        timerVida += Time.deltaTime;
        timerTick += Time.deltaTime;

        // Sistema de Dano
        if (timerTick >= intervaloTick)
        {
            AplicarDano();
            timerTick = 0;
        }

        // Sistema de Duração
        if (timerVida >= duracao)
        {
            IniciarEncerramento();
        }
    }

    void IniciarEncerramento()
    {
        estaFinalizando = true;
        if (anim != null)
        {
            anim.SetTrigger("Finalizar");

            // Descobre o tempo da animação de fechamento para destruir o objeto depois dela
            float tempoFechamento = ObterTempoDaAnimacao("poça-fechamento");
            Destroy(gameObject, tempoFechamento);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    float ObterTempoDaAnimacao(string nome)
    {
        if (anim == null) return 0;
        RuntimeAnimatorController ac = anim.runtimeAnimatorController;
        foreach (AnimationClip clip in ac.animationClips)
        {
            if (clip.name == nome) return clip.length;
        }
        return 0.5f; // Valor padrão caso não encontre
    }

    void AplicarDano()
    {
        Collider2D[] alvos = Physics2D.OverlapCircleAll(transform.position, raio, layerParaAcertar);
        foreach (Collider2D col in alvos)
        {
            if (col.TryGetComponent<PlayerStats>(out PlayerStats player))
            {
                player.TakeDamage(dano);
                Debug.Log("Dano de veneno aplicado!");
            }
        }
    }
}