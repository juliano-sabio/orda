using UnityEngine;
using Unity.Netcode;

public class AreaVeneno : MonoBehaviour
{
    private float dano;
    private float intervaloTick;
    private float duracao;
    private float raio;
    private LayerMask layerParaAcertar;
    private LayerMask layerInimigos = 1 << 7; // layer Enemy

    private float timerTick;
    private float timerVida;
    private bool estaFinalizando = false;

    private Animator anim;

    // Co-op: a poça é NetworkObject. O HOST conduz dano + tempo de vida; o cliente é fantoche
    // (só mostra o visual; o host despawna em todos os lados).
    bool EhClienteFantoche
    {
        get { var nm = NetworkManager.Singleton; return nm != null && nm.IsListening && !nm.IsServer; }
    }

    void Start()
    {
        // No cliente o Inicializar não roda (só o host) → toca a abertura aqui pro visual aparecer.
        if (EhClienteFantoche)
        {
            var a = GetComponent<Animator>();
            if (a != null) a.Play("abrir_veneno");
        }
    }

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
        if (EhClienteFantoche) return;   // cliente: fantoche — host conduz dano + lifetime
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

            // Despawna depois da animação de fechamento. Co-op: host remove em todos. SP: Destroy.
            float tempoFechamento = ObterTempoDaAnimacao("poça-fechamento");
            Invoke(nameof(DespawnPoca), tempoFechamento);
        }
        else
        {
            DespawnPoca();
        }
    }

    void DespawnPoca() => NetSpawn.Despawnar(gameObject);

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
        // Dano ao player
        Collider2D[] alvos = Physics2D.OverlapCircleAll(transform.position, raio, layerParaAcertar);
        foreach (Collider2D col in alvos)
        {
            if (col.TryGetComponent<PlayerStats>(out PlayerStats player))
            {
                player.TakeDamage(dano);
            }
        }

        // Dano aos inimigos que pisam na poça
        Collider2D[] inimigos = Physics2D.OverlapCircleAll(transform.position, raio, layerInimigos);
        foreach (Collider2D col in inimigos)
        {
            if (col.TryGetComponent<InimigoController>(out InimigoController inimigo) && !inimigo.estaMorrendo)
            {
                inimigo.ReceberDano(dano);
            }
        }
    }
}