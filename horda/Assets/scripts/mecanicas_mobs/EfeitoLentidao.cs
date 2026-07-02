using UnityEngine;
using System.Collections;

// ========== COMPONENTE DE EFEITO DE LENTIDÃO NO PLAYER ==========
public class EfeitoLentidao : MonoBehaviour
{
    private float duracao;
    private float fator;
    private float tempoRestante;
    private bool ativo = false;

    // Componentes originais para restaurar depois
    private PlayerStats movimentoPlayer; // Assumindo que seu script de movimento se chama MovimentoPlayer
    private float velocidadeOriginal;
    private Animator animator;
    private float animatorSpeedOriginal;
    private SpriteRenderer spriteRenderer;
    private Color corOriginal;

    void Start() => InicializarRefs();

    // O AplicarLentidao pode rodar no MESMO frame do AddComponent (antes do Start) —
    // sem isto a 1ª aplicação não pegava velocidade/animator/sprite (refs ainda nulas).
    void InicializarRefs()
    {
        if (movimentoPlayer == null) movimentoPlayer = GetComponent<PlayerStats>();
        if (animator == null)        animator        = GetComponent<Animator>();
        if (spriteRenderer == null)  spriteRenderer  = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (!ativo) return;

        tempoRestante -= Time.deltaTime;

        if (tempoRestante <= 0)
        {
            RemoverLentidao();
        }
    }

    public void AplicarLentidao(float duracao, float fator, Color corEfeito)
    {
        InicializarRefs();

        // Se já está lento, reinicia o tempo
        if (ativo)
        {
            tempoRestante = duracao;
            return;
        }

        this.duracao = duracao;
        this.fator = fator;
        tempoRestante = duracao;
        ativo = true;

        // Aplica lentidão no movimento
        if (movimentoPlayer != null)
        {
            velocidadeOriginal = movimentoPlayer.speed;
            movimentoPlayer. speed *= fator;
        }

        // Aplica lentidão na animação
        if (animator != null)
        {
            animatorSpeedOriginal = animator.speed;
            animator.speed *= fator;
        }

        // Muda cor para indicar efeito. Usa a cor VERDADEIRA do player (CorBasePlayer, capturada
        // 1x) em vez de sr.color na hora — senão, se já havia outro tint ativo, restaurava na cor
        // errada e o player ficava verde/tingido permanentemente.
        if (spriteRenderer != null)
        {
            corOriginal = CorBasePlayer.Obter(spriteRenderer);
            spriteRenderer.color = corEfeito;
        }

    }

    void RemoverLentidao()
    {
        ativo = false;

        // Restaura velocidade
        if (movimentoPlayer != null)
        {
            movimentoPlayer.speed = velocidadeOriginal;
        }

        // Restaura animação
        if (animator != null)
        {
            animator.speed = animatorSpeedOriginal;
        }

        // Restaura cor
        if (spriteRenderer != null)
        {
            spriteRenderer.color = corOriginal;
        }

    }
}
