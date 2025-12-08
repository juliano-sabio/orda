using System.Collections;
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

    [Header("Dano Flutuante")]
    public float alturaDanoFlutuante = 2f;
    public bool mostrarDanoAposMorte = true;

    private bool estaMorrendo = false;

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

    // ✅ MÉTODO PARA RECEBER DANO
    public void ReceberDano(float dano, bool isCrit = false)
    {
        if (estaMorrendo) return;

        vidaAtual -= dano;

        Debug.Log($"💥 Dano: {dano} | Vida: {vidaAtual} | Morrendo: {estaMorrendo}");

        // Mostrar dano flutuante
        MostrarDanoFlutuante(dano, isCrit);

        // Verificar morte
        if (vidaAtual <= 0)
        {
            estaMorrendo = true; // Setar como morrendo AQUI
            Debug.Log($"💀 Vida zerada! Chamando Morrer()...");

            // Mostrar dano fatal
            MostrarDanoFatal(dano, isCrit);

            // Morrer
            Morrer();
        }
    }

    // ✅ MOSTRAR DANO FLUTUANTE
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

    // ✅ MOSTRAR DANO FATAL
    private void MostrarDanoFatal(float danoFinal, bool isCrit)
    {
        Debug.Log($"💀 DANO FATAL: {danoFinal}");

        if (mostrarDanoAposMorte)
        {
            // Cria um alvo temporário
            GameObject dummyTarget = new GameObject("DummyTarget");
            dummyTarget.transform.position = this.transform.position;

            if (DamageNumberManager.Instance != null)
            {
                DamageNumberManager.Instance.ShowDamageFatal(
                    dummyTarget.transform,
                    danoFinal,
                    isCrit
                );
            }
            else
            {
                CriarDanoLocal(danoFinal, isCrit, true);
            }

            Destroy(dummyTarget, 2f);
        }
    }

    // ✅ CRIAR DANO LOCAL (fallback)
    private void CriarDanoLocal(float dano, bool isCrit, bool isFatal)
    {
        // Usando FindFirstObjectByType em vez do obsoleto
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            GameObject canvasObj = new GameObject("LocalDamageCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        GameObject textObj = new GameObject(isFatal ? "DanoFatal" : "DanoNormal");
        textObj.transform.SetParent(canvas.transform, false);

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        screenPos.y += 60;
        textObj.transform.position = screenPos;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = Mathf.RoundToInt(dano).ToString();
        text.alignment = TextAlignmentOptions.Center;

        if (isFatal)
        {
            text.fontSize = 48;
            text.color = Color.red;
            text.text = "💀 " + text.text;
            text.fontStyle = FontStyles.Bold;
        }
        else if (isCrit)
        {
            text.fontSize = 36;
            text.color = Color.yellow;
            text.fontStyle = FontStyles.Bold;
        }
        else
        {
            text.fontSize = 28;
            text.color = Color.white;
        }

        // Adiciona animação local
        textObj.AddComponent<DamageAnimatorLocal>().Initialize(isFatal);

        Destroy(textObj, isFatal ? 2f : 1f);
    }

    // ✅ MÉTODO PARA MORRER
    public void Morrer()
    {
        if (estaMorrendo && vidaAtual <= 0) // Adicionei verificação extra
        {
            Debug.Log($"☠️ {dadosInimigo?.nomeInimigo} MORTO! Destruindo...");

            // Dropar XP
            DroparOrbesXP();

            // Destruir
            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning($"⚠️ Tentativa de Morrer() bloqueada. estaMorrendo: {estaMorrendo}, vidaAtual: {vidaAtual}");
        }
    }

    // ✅ DROPAR ORBES DE XP
    private void DroparOrbesXP()
    {
        if (xpOrbPrefab == null)
        {
            Debug.LogWarning("⚠️ xpOrbPrefab não configurado!");
            return;
        }

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

        Debug.Log($"💫 Dropou {quantidadeOrbes} orbes de XP!");
    }

    // ✅ CONFIGURAR DROP
    public void ConfigurarDrop(int novoMinOrbes, int novoMaxOrbes, float novoXpPorOrbe)
    {
        this.minOrbs = novoMinOrbes;
        this.maxOrbs = novoMaxOrbes;
        this.xpPorOrbe = novoXpPorOrbe;
    }

    // ✅ APLICAR DIFICULDADE
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

// ✅ CLASSE DE ANIMAÇÃO LOCAL (nome corrigido)
public class DamageAnimatorLocal : MonoBehaviour
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

        // Move para cima
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