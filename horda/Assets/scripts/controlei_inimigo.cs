using UnityEngine;

public class InimigoController : MonoBehaviour
{
    [Header("Dados do Inimigo")]
    public InimigoData dadosInimigo;

    [Header("Status Atuais")]
    public float vidaAtual;
    public float danoAtual;

    void Start()
    {
        InicializarComData();
    }

    public void InicializarComData()
    {
        // ✅ VERIFICAÇÃO DE SEGURANÇA
        if (dadosInimigo == null)
        {
            Debug.LogError($"❌ Inimigo {name} não tem dadosInimigo atribuído!");
            return;
        }

        vidaAtual = dadosInimigo.vidaBase;
        danoAtual = dadosInimigo.danoBase;

        // Configura componentes baseados nos dados
        DanoInimigo danoComponent = GetComponent<DanoInimigo>();
        if (danoComponent != null)
        {
            danoComponent.dano = dadosInimigo.danoBase;
            danoComponent.intervaloAtaque = dadosInimigo.intervaloAtaque;
        }

        // Configura aparência
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && dadosInimigo.icon != null)
        {
            spriteRenderer.sprite = dadosInimigo.icon;
        }

        // Configura escala
        transform.localScale = Vector3.one * dadosInimigo.tamanho;

        // Configura nome
        gameObject.name = dadosInimigo.nomeInimigo;

        Debug.Log($"👹 Inimigo {dadosInimigo.nomeInimigo} inicializado! Vida: {vidaAtual}, Dano: {danoAtual}");
    }

    // ✅ MÉTODO COM VERIFICAÇÃO DE SEGURANÇA
    public void AplicarDificuldade(float multiplicador)
    {
        if (dadosInimigo == null)
        {
            Debug.LogError($"❌ Tentativa de aplicar dificuldade em inimigo sem dadosInimigo: {name}");
            return;
        }

        vidaAtual = dadosInimigo.vidaBase * multiplicador;
        danoAtual = dadosInimigo.danoBase * multiplicador;

        DanoInimigo danoComponent = GetComponent<DanoInimigo>();
        if (danoComponent != null)
        {
            danoComponent.dano = danoAtual;
        }

        Debug.Log($"📈 Inimigo {dadosInimigo.nomeInimigo} - Dificuldade: x{multiplicador}");
    }

    // ... resto do código
}