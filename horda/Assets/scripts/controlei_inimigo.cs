using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Dano Flutuante - Ajustes")]
    public float alturaDanoFlutuante = 2f;
    public bool mostrarDanoAposMorte = true;

    // Para controlar morte
    private bool estaMorto = false;
    private GameObject ultimoPopupDano;

    void Start()
    {
        InicializarComData();
    }

    public void InicializarComData()
    {
        if (dadosInimigo == null)
        {
            Debug.LogError($"❌ Inimigo {name} não tem dadosInimigo atribuído!");
            return;
        }

        vidaAtual = dadosInimigo.vidaBase;
        danoAtual = dadosInimigo.danoBase;

        DanoInimigo danoComponent = GetComponent<DanoInimigo>();
        if (danoComponent != null)
        {
            danoComponent.dano = dadosInimigo.danoBase;
            danoComponent.intervaloAtaque = dadosInimigo.intervaloAtaque;
        }

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && dadosInimigo.icon != null)
        {
            spriteRenderer.sprite = dadosInimigo.icon;
        }

        transform.localScale = Vector3.one * dadosInimigo.tamanho;
        gameObject.name = dadosInimigo.nomeInimigo;
    }

    // ✅ MÉTODO CORRIGIDO - Mostra dano mesmo em hit fatal
    public void ReceberDano(float dano, bool isCrit = false)
    {
        if (estaMorto) return; // Já está morto

        vidaAtual -= dano;

        Debug.Log($"💥 Dano recebido: {dano} | Vida restante: {vidaAtual} | Crítico: {isCrit}");

        // 🔥 MOSTRAR DANO FLUTUANTE (IMPORTANTE: antes de verificar morte)
        MostrarDanoFlutuante(dano, isCrit);

        // Verifica se morreu
        if (vidaAtual <= 0 && !estaMorto)
        {
            // 🔥 DANO FATAL - mostra dano especial
            MostrarDanoFatal(dano, isCrit);

            // Marca como morto e agenda destruição
            estaMorto = true;

            // Destrói após um pequeno delay para garantir que o dano apareça
            StartCoroutine(MorrerComDelay(0.3f));
        }
    }

    // ✅ MÉTODO PARA MOSTRAR DANO NORMAL
    private void MostrarDanoFlutuante(float dano, bool isCrit)
    {
        if (DamageNumberManager.Instance != null)
        {
            DamageNumberManager.Instance.ShowDamage(this.transform, dano, isCrit);
        }
        else
        {
            CriarDanoLocal(dano, isCrit, false);
        }
    }

    // ✅ MÉTODO PARA MOSTRAR DANO FATAL (especial)
    private void MostrarDanoFatal(float danoFinal, bool isCrit)
    {
        Debug.Log($"💀 DANO FATAL: {danoFinal} em {name}");

        if (mostrarDanoAposMorte)
        {
            if (DamageNumberManager.Instance != null)
            {
                // Usa o sistema principal com ajustes para dano fatal
                DamageNumberManager.Instance.ShowDamageFatal(
                    this.transform,
                    danoFinal,
                    isCrit
                );
            }
            else
            {
                // Cria dano fatal local
                CriarDanoLocal(danoFinal, isCrit, true);
            }
        }
    }

    // ✅ MÉTODO PARA CRIAR DANO LOCAL (fallback)
    private void CriarDanoLocal(float dano, bool isCrit, bool isFatal)
    {
        // Encontra ou cria Canvas
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            GameObject canvasObj = new GameObject("LocalDamageCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Cria o texto
        GameObject textObj = new GameObject(isFatal ? "DanoFatal" : "DanoNormal");
        textObj.transform.SetParent(canvas.transform, false);

        // Converte posição
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        screenPos.y += 60;
        textObj.transform.position = screenPos;

        // TextMeshProUGUI
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = Mathf.RoundToInt(dano).ToString();
        text.alignment = TextAlignmentOptions.Center;

        if (isFatal)
        {
            // Estilo para dano fatal
            text.fontSize = 48;
            text.color = Color.red;
            text.text = "💀 " + text.text + " 💀";
            text.fontStyle = FontStyles.Bold;
            text.outlineWidth = 0.4f;
            text.outlineColor = Color.black;
        }
        else if (isCrit)
        {
            text.fontSize = 36;
            text.color = Color.yellow;
            text.fontStyle = FontStyles.Bold;
            text.outlineWidth = 0.3f;
            text.outlineColor = Color.black;
        }
        else
        {
            text.fontSize = 28;
            text.color = Color.white;
            text.outlineWidth = 0.2f;
            text.outlineColor = Color.black;
        }

        // Animação
        textObj.AddComponent<LocalDamageAnimator>().Initialize(isFatal);

        Destroy(textObj, isFatal ? 1.5f : 1f);
    }

    // ✅ MÉTODO PARA MORRER COM DELAY (garante que dano apareça)
    private System.Collections.IEnumerator MorrerComDelay(float delay)
    {
        // Opcional: efeito visual de morte
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            Color originalColor = sprite.color;
            sprite.color = Color.red;
            yield return new WaitForSeconds(delay * 0.5f);
            sprite.color = originalColor;
        }

        yield return new WaitForSeconds(delay);

        // Agora sim morre
        Morrer();
    }

    // ✅ MÉTODO CHAMADO QUANDO O INIMIGO MORRE
    public void Morrer()
    {
        if (estaMorto) return; // Já está processando morte

        estaMorto = true;
        Debug.Log($"☠️ {dadosInimigo.nomeInimigo} morreu!");

        DroparOrbesXP();

        // Destrói o GameObject
        Destroy(gameObject);
    }

    // ✅ MÉTODO PARA DROPAR ORBES DE XP
    public void DroparOrbesXP()
    {
        if (xpOrbPrefab == null) return;

        int quantidadeOrbes = UnityEngine.Random.Range(minOrbs, maxOrbs + 1);

        for (int i = 0; i < quantidadeOrbes; i++)
        {
            GameObject orbe = Instantiate(xpOrbPrefab, transform.position, Quaternion.identity);
            XPOrb xpOrb = orbe.GetComponent<XPOrb>();

            if (xpOrb != null)
            {
                xpOrb.xpValue = xpPorOrbe;
            }

            Rigidbody2D rb = orbe.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 direcaoAleatoria = new Vector2(
                    UnityEngine.Random.Range(-1f, 1f),
                    UnityEngine.Random.Range(-1f, 1f)
                ).normalized;
                rb.AddForce(direcaoAleatoria * forcaDrop, ForceMode2D.Impulse);
            }
        }
    }

    // ✅ MÉTODO PARA CONFIGURAR DROP PERSONALIZADO
    public void ConfigurarDrop(int minOrbes, int maxOrbes, float xpPorOrbe)
    {
        this.minOrbs = minOrbes;
        this.maxOrbs = maxOrbes;
        this.xpPorOrbe = xpPorOrbe;
    }

    // ✅ MÉTODO COM VERIFICAÇÃO DE SEGURANÇA
    public void AplicarDificuldade(float multiplicador)
    {
        if (dadosInimigo == null) return;

        vidaAtual = dadosInimigo.vidaBase * multiplicador;
        danoAtual = dadosInimigo.danoBase * multiplicador;

        DanoInimigo danoComponent = GetComponent<DanoInimigo>();
        if (danoComponent != null)
        {
            danoComponent.dano = danoAtual;
        }
    }
}

// ✅ ANIMAÇÃO PARA DANO LOCAL
public class LocalDamageAnimator : MonoBehaviour
{
    private TextMeshProUGUI textMesh;
    private bool isFatal = false;
    private float timer = 0f;
    private Vector3 startPos;

    public void Initialize(bool fatal)
    {
        this.isFatal = fatal;
        textMesh = GetComponent<TextMeshProUGUI>();
        startPos = transform.position;
    }

    void Update()
    {
        if (textMesh == null) return;

        timer += Time.deltaTime;

        // Move para cima mais rápido se for fatal
        float speed = isFatal ? 120f : 80f;
        transform.position = startPos + new Vector3(0, speed * timer, 0);

        // Fade out
        Color color = textMesh.color;
        color.a = 1f - (timer / (isFatal ? 1.5f : 1f));
        textMesh.color = color;

        // Efeito especial para fatal
        if (isFatal)
        {
            float scale = 1f + Mathf.Sin(timer * 10f) * 0.2f;
            transform.localScale = Vector3.one * scale;
        }

        if (timer >= (isFatal ? 1.5f : 1f))
            Destroy(gameObject);
    }
}