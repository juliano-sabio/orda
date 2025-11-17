using UnityEngine;

public class InimigoController : MonoBehaviour
{
    [Header("Dados do Inimigo")]
    public InimigoData dadosInimigo;

    [Header("Status Atuais")]
    public float vidaAtual;
    public float danoAtual;

    [Header("Sistema de Drop de XP")]
    public GameObject xpOrbPrefab;
    public int minOrbs = 1;
    public int maxOrbs = 3;
    public float xpPorOrbe = 5f;
    public float forcaDrop = 2f;

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

    // 🆕 MÉTODO PARA RECEBER DANO
    public void ReceberDano(float dano)
    {
        vidaAtual -= dano;
        Debug.Log($"💥 {dadosInimigo.nomeInimigo} recebeu {dano} de dano. Vida: {vidaAtual}");

        if (vidaAtual <= 0)
        {
            Morrer();
        }
    }

    // 🆕 MÉTODO CHAMADO QUANDO O INIMIGO MORRE
    public void Morrer()
    {
        Debug.Log($"☠️ {dadosInimigo.nomeInimigo} morreu!");

        // Dropar orbes de XP
        DroparOrbesXP();

        // Destruir o inimigo
        Destroy(gameObject);
    }

    // 🆕 MÉTODO PARA DROPAR ORBES DE XP
    public void DroparOrbesXP()
    {
        if (xpOrbPrefab == null)
        {
            Debug.LogWarning($"⚠️ {dadosInimigo.nomeInimigo} não tem xpOrbPrefab atribuído!");
            return;
        }

        int quantidadeOrbes = Random.Range(minOrbs, maxOrbs + 1);

        for (int i = 0; i < quantidadeOrbes; i++)
        {
            GameObject orbe = Instantiate(xpOrbPrefab, transform.position, Quaternion.identity);
            XPOrb xpOrb = orbe.GetComponent<XPOrb>();

            if (xpOrb != null)
            {
                xpOrb.xpValue = xpPorOrbe;
            }

            // Aplica uma força aleatória para espalhar as orbes
            Rigidbody2D rb = orbe.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 direcaoAleatoria = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
                rb.AddForce(direcaoAleatoria * forcaDrop, ForceMode2D.Impulse);
            }
        }

        Debug.Log($"💫 {dadosInimigo.nomeInimigo} dropou {quantidadeOrbes} orbes de XP!");
    }

    // 🆕 MÉTODO PARA CONFIGURAR DROP PERSONALIZADO
    public void ConfigurarDrop(int minOrbes, int maxOrbes, float xpPorOrbe)
    {
        this.minOrbs = minOrbes;
        this.maxOrbs = maxOrbes;
        this.xpPorOrbe = xpPorOrbe;
    }
}