using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// Se InimigoData estiver em um namespace diferente, adicione:
// using Survivor; // ou o namespace onde InimigoData está

public class InimigoController : MonoBehaviour
{
    public static event System.Action OnInimigoDerrotado;

    [Header("Dados do Inimigo")]
    public InimigoData dadosInimigo;

    [Header("Status Atuais")]
    public float vidaAtual;
    public float danoAtual;
    public float vidaMaxima;

    [Header("Sistema de Drop de XP")]
    public GameObject xpOrbPrefab;
    public int minOrbs = 1;
    public int maxOrbs = 3;
    public float xpPorOrbe = 5f;
    public float forcaDrop = 2f;

    [Header("Drops Adicionais")]
    public List<DropEntry> drops = new List<DropEntry>();

    [Header("Dano Flutuante")]
    public float alturaDanoFlutuante = 2f;
    public bool mostrarDanoAposMorte = true;

    [Header("Sistema de Cura")]
    public bool podeReceberCura = true;
    public float multiplicadorCuraRecebida = 1f;
    public bool mostrarCuraFlutuante = true;
    public Color corCura = Color.green;
    private bool temInimigoSuporte = false;
    private InimigoSuporte suporteComponent;

    [Header("Efeitos de Status")]
    public bool temBuffDefesa = false;
    public float bonusDefesa = 0f;
    public float tempoRestanteBuff = 0f;
    private float danoOriginal;

    [Header("Configurações de Movimento")]
    public float velocidadeBase = 3f;
    public float velocidadeAtual;
    public bool estaAtordoado = false;
    public float tempoAtordoado = 0f;

    // ✅ Variável pública para acesso externo
    public bool estaMorrendo = false;

    private SpriteRenderer spriteRenderer;
    private Color corOriginal;
    private Rigidbody2D rb;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer != null)
        {
            corOriginal = spriteRenderer.color;
        }

        InicializarComData();
    }

    public void InicializarComData()
    {
        if (dadosInimigo == null)
        {
            // Boss e inimigos customizados podem não usar InimigoData; valores já definidos externamente.
            return;
        }

        vidaAtual = dadosInimigo.vidaBase;
        vidaMaxima = dadosInimigo.vidaBase;
        danoAtual = dadosInimigo.danoBase;
        danoOriginal = danoAtual;
        velocidadeBase = dadosInimigo.velocidadeBase;
        velocidadeAtual = velocidadeBase;

        DanoInimigo danoComponent = GetComponent<DanoInimigo>();
        if (danoComponent != null)
        {
            danoComponent.dano = dadosInimigo.danoBase;
            danoComponent.intervaloAtaque = dadosInimigo.intervaloAtaque;
        }

        if (spriteRenderer != null && dadosInimigo.icon != null)
        {
            spriteRenderer.sprite = dadosInimigo.icon;
        }

        transform.localScale = Vector3.one * dadosInimigo.tamanho;
        gameObject.name = dadosInimigo.nomeInimigo;

        suporteComponent = GetComponent<InimigoSuporte>();
        temInimigoSuporte = (suporteComponent != null);

    }

    public void ReceberDano(float dano, bool isCrit = false)
    {
        if (estaMorrendo) return;

        if (temBuffDefesa && bonusDefesa > 0)
        {
            float reducao = dano * bonusDefesa;
            dano -= reducao;
        }

        vidaAtual -= dano;


        MostrarDanoFlutuante(dano, isCrit);
        StartCoroutine(EfeitoVisualDano());

        if (vidaAtual <= 0)
        {
            vidaAtual = 0;
            estaMorrendo = true;

            MostrarDanoFatal(dano, isCrit);
            Morrer();
        }
    }

    public void ReceberCura(float quantidadeCura, bool mostrarEfeito = true)
    {
        if (estaMorrendo || !podeReceberCura || vidaAtual >= vidaMaxima) return;

        float curaFinal = quantidadeCura * multiplicadorCuraRecebida;
        float vidaAntes = vidaAtual;

        vidaAtual = Mathf.Min(vidaAtual + curaFinal, vidaMaxima);
        float curaReal = vidaAtual - vidaAntes;


        if (mostrarEfeito && mostrarCuraFlutuante)
        {
            MostrarCuraFlutuante(curaReal);
            StartCoroutine(EfeitoVisualCura());
        }
    }

    public void AplicarBuffDefesa(float bonus, float duracao)
    {
        if (estaMorrendo) return;

        bonusDefesa = bonus;
        tempoRestanteBuff = duracao;

        if (!temBuffDefesa)
        {
            temBuffDefesa = true;
            danoOriginal = danoAtual;
            danoAtual = danoOriginal * (1f - bonusDefesa);

            DanoInimigo danoComponent = GetComponent<DanoInimigo>();
            if (danoComponent != null)
            {
                danoComponent.SetDano(danoAtual);
            }
        }


        StopCoroutine("GerenciarBuffDefesa");
        StartCoroutine(GerenciarBuffDefesa(duracao));

        StartCoroutine(EfeitoVisualBuff());
    }

    // 🆕 NOVO MÉTODO: Aplicar Slow (redução de velocidade)
    public void AplicarSlow(float reducaoVelocidade, float duracao)
    {
        if (estaMorrendo || reducaoVelocidade <= 0) return;


        // Calcula a redução
        float reducaoAplicada = velocidadeAtual * reducaoVelocidade;
        velocidadeAtual -= reducaoAplicada;

        // Garante que não fique negativo
        velocidadeAtual = Mathf.Max(0.5f, velocidadeAtual);

        // Restaura após a duração
        StartCoroutine(RestaurarVelocidade(reducaoAplicada, duracao));

        // Efeito visual
        StartCoroutine(EfeitoVisualSlow());
    }

    private IEnumerator RestaurarVelocidade(float reducao, float duracao)
    {
        yield return new WaitForSeconds(duracao);
        velocidadeAtual += reducao;
    }

    private IEnumerator EfeitoVisualSlow()
    {
        if (spriteRenderer == null) yield break;

        Color corSlow = new Color(0.2f, 0.6f, 1f, 1f); // Azul para slow
        spriteRenderer.color = corSlow;
        yield return new WaitForSeconds(0.2f);

        if (!estaMorrendo)
        {
            spriteRenderer.color = corOriginal;
        }
    }

    // 🆕 NOVO MÉTODO: Aplicar Stun (atordoamento)
    public void AplicarStun(float duracao)
    {
        if (estaMorrendo) return;


        estaAtordoado = true;
        tempoAtordoado = duracao;

        // Pode adicionar lógica para parar movimento/ataques aqui
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        StartCoroutine(RemoverStun(duracao));
        StartCoroutine(EfeitoVisualStun());
    }

    private IEnumerator RemoverStun(float duracao)
    {
        yield return new WaitForSeconds(duracao);
        estaAtordoado = false;
        tempoAtordoado = 0f;
    }

    private IEnumerator EfeitoVisualStun()
    {
        if (spriteRenderer == null) yield break;

        while (estaAtordoado && tempoAtordoado > 0)
        {
            spriteRenderer.color = Color.yellow;
            yield return new WaitForSeconds(0.2f);
            spriteRenderer.color = corOriginal;
            yield return new WaitForSeconds(0.2f);
        }

        if (!estaMorrendo)
        {
            spriteRenderer.color = corOriginal;
        }
    }

    // 🆕 NOVO MÉTODO: Aplicar Veneno (dano contínuo)
    public void AplicarVeneno(float danoPorTick, float intervalo, int quantidadeTicks, Color corVeneno)
    {
        if (estaMorrendo) return;


        StartCoroutine(EfeitoVenenoCoroutine(danoPorTick, intervalo, quantidadeTicks, corVeneno));
    }

    private IEnumerator EfeitoVenenoCoroutine(float danoPorTick, float intervalo, int quantidadeTicks, Color corVeneno)
    {
        for (int i = 0; i < quantidadeTicks && !estaMorrendo; i++)
        {
            // Aplica dano
            ReceberDano(danoPorTick, false);

            // Efeito visual
            if (spriteRenderer != null)
            {
                spriteRenderer.color = corVeneno;
                yield return new WaitForSeconds(0.1f);
                spriteRenderer.color = corOriginal;
            }

            yield return new WaitForSeconds(intervalo);
        }
    }

    private IEnumerator GerenciarBuffDefesa(float duracao)
    {
        tempoRestanteBuff = duracao;

        while (tempoRestanteBuff > 0)
        {
            yield return null;
            tempoRestanteBuff -= Time.deltaTime;

            if (tempoRestanteBuff < 3f && tempoRestanteBuff > 0)
            {
                float pingPong = Mathf.PingPong(Time.time * 10f, 1f);
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.Lerp(corOriginal, new Color(0.5f, 0.5f, 1f, 1f), pingPong);
                }
            }
        }

        RemoverBuffDefesa();
    }

    private void RemoverBuffDefesa()
    {
        temBuffDefesa = false;
        bonusDefesa = 0f;
        tempoRestanteBuff = 0f;
        danoAtual = danoOriginal;

        DanoInimigo danoComponent = GetComponent<DanoInimigo>();
        if (danoComponent != null)
        {
            danoComponent.SetDano(danoAtual);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = corOriginal;
        }

    }

    private void MostrarCuraFlutuante(float quantidade)
    {
        if (DamageNumberManager.Instance != null && DamageNumberManager.Instance is DamageNumberManager manager)
        {
            var method = manager.GetType().GetMethod("ShowHeal");
            if (method != null)
            {
                method.Invoke(manager, new object[] { this.transform, quantidade });
                return;
            }
        }

        // Fallback
        CriarTextoFlutuante(quantidade, corCura, "+", 24);
    }

    private void MostrarDanoFlutuante(float dano, bool isCrit)
    {
        if (DamageNumberManager.Instance != null)
        {
            DamageNumberManager.Instance.ShowDamage(this.transform, dano, isCrit);
        }
        else
        {
            CriarTextoFlutuante(dano, isCrit ? Color.yellow : Color.white, "", 28);
        }
    }

    private void MostrarDanoFatal(float danoFinal, bool isCrit)
    {

        if (mostrarDanoAposMorte)
        {
            GameObject dummyTarget = new GameObject("DummyTarget");
            dummyTarget.transform.position = this.transform.position;

            if (DamageNumberManager.Instance != null)
            {
                DamageNumberManager.Instance.ShowDamageFatal(dummyTarget.transform, danoFinal, isCrit);
            }
            else
            {
                CriarTextoFlutuante(danoFinal, Color.red, "💀 ", 48);
            }

            Destroy(dummyTarget, 2f);
        }
    }

    private void CriarTextoFlutuante(float valor, Color cor, string prefixo = "", int fontSize = 24)
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("TempCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        GameObject textObj = new GameObject("TextoFlutuante");
        textObj.transform.SetParent(canvas.transform, false);

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        screenPos.y += 80;
        textObj.transform.position = screenPos;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = prefixo + Mathf.RoundToInt(valor).ToString();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = fontSize;
        text.color = cor;
        text.fontStyle = FontStyles.Bold;

        textObj.AddComponent<AnimacaoTextoFlutuante>().Initialize(screenPos);
        Destroy(textObj, 1.5f);
    }

    private IEnumerator EfeitoVisualDano()
    {
        if (spriteRenderer == null) yield break;

        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);

        if (!estaMorrendo)
        {
            spriteRenderer.color = corOriginal;
        }
    }

    private IEnumerator EfeitoVisualCura()
    {
        if (spriteRenderer == null) yield break;

        spriteRenderer.color = corCura;
        yield return new WaitForSeconds(0.2f);

        if (!estaMorrendo)
        {
            spriteRenderer.color = corOriginal;
        }
    }

    private IEnumerator EfeitoVisualBuff()
    {
        if (spriteRenderer == null) yield break;

        for (int i = 0; i < 3; i++)
        {
            spriteRenderer.color = new Color(0.5f, 0.5f, 1f, 1f);
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = corOriginal;
            yield return new WaitForSeconds(0.1f);
        }
    }

    public void Morrer()
    {
        if (estaMorrendo && vidaAtual <= 0)
        {

            if (suporteComponent != null)
            {
                suporteComponent.AtivarCura(false);
            }

            DroparOrbesXP();
            DroparPowerup();
            OnInimigoDerrotado?.Invoke();
            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning($"⚠️ Tentativa de Morrer() bloqueada. estaMorrendo: {estaMorrendo}, vidaAtual: {vidaAtual}");
        }
    }

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

    }

    private void DroparPowerup()
    {
        foreach (var drop in drops)
        {
            if (drop.prefab == null) continue;
            if (UnityEngine.Random.value <= drop.chance)
                Instantiate(drop.prefab, transform.position, Quaternion.identity);
        }

        if (dadosInimigo != null && dadosInimigo.dropsPossiveis != null)
        {
            foreach (var drop in dadosInimigo.dropsPossiveis)
            {
                if (drop.prefab == null) continue;
                if (UnityEngine.Random.value <= drop.chance)
                    Instantiate(drop.prefab, transform.position, Quaternion.identity);
            }
        }
    }

    public float GetPorcentagemVida()
    {
        if (vidaMaxima <= 0) return 0f;
        return vidaAtual / vidaMaxima;
    }

    // 🆕 GETTERS para status
    public float GetVelocidadeAtual() => velocidadeAtual;
    public bool EstaAtordoado() => estaAtordoado;
    public float GetTempoAtordoado() => tempoAtordoado;
    public bool TemBuffDefesaAtivo() => temBuffDefesa;
    public float GetBonusDefesa() => bonusDefesa;
}

[System.Serializable]
public class DropEntry
{
    public GameObject prefab;
    [Range(0f, 1f)] public float chance = 0.1f;
}

// ✅ Classe de animação de texto mantida no mesmo arquivo
public class AnimacaoTextoFlutuante : MonoBehaviour
{
    private TextMeshProUGUI textMesh;
    private Vector3 startPos;
    private float timer = 0f;

    public void Initialize(Vector3 startPosition)
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        startPos = startPosition;
    }

    void Update()
    {
        if (textMesh == null) return;

        timer += Time.deltaTime;

        float speed = 80f;
        transform.position = startPos + new Vector3(0, speed * timer, 0);

        Color color = textMesh.color;
        color.a = 1f - (timer / 1f);
        textMesh.color = color;

        if (timer >= 1f)
            Destroy(gameObject);
    }
}
