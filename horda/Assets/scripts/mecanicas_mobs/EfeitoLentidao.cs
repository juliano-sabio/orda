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

    void Start()
    {
        // Tenta encontrar componentes
        movimentoPlayer = GetComponent<PlayerStats>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            corOriginal = spriteRenderer.color;
        }
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

        // Muda cor para indicar efeito
        if (spriteRenderer != null)
        {
            spriteRenderer.color = corEfeito;
        }

        Debug.Log($"🐌 Efeito de lentidão aplicado (fator: {fator})");
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

        Debug.Log("✅ Efeito de lentidão removido");
    }
}