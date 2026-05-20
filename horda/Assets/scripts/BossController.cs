using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossController : MonoBehaviour
{
    [Header("Configuração do Boss")]
    public string nomeBoss = "BOSS";
    public float vidaMaxima = 500f;
    public float danoBase = 20f;
    public float velocidadeBase = 2f;

    [Header("Animações")]
    public RuntimeAnimatorController controllerFase1;
    public RuntimeAnimatorController controllerFase2;

    [Header("Fase 2 — Efeito")]
    public GameObject efeitoFase2;

    [Header("Fase 2 — Minions")]
    public GameObject[] prefabsMinions;
    public int quantidadeMinions = 3;
    public float raioSpawnMinions = 2f;

    [Header("Fase 2 (% de vida restante)")]
    public float gatilhoFase2 = 0.5f;
    public float multiplicadorDanoFase2 = 1.5f;
    public float multiplicadorVelocidadeFase2 = 1.4f;
    public Color corFase2 = new Color(1f, 0.3f, 0.1f);

    [Header("Teleporte")]
    public float intervaloTeleporteFase1 = 8f;
    public float intervaloTeleporteFase2 = 4f;
    public float raioTeleporteMin = 4f;
    public float raioTeleporteMax = 9f;
    public GameObject efeitoTeleporte;

    [Header("Renderização")]
    public string sortingLayerName = "Default";
    public int sortingOrder = 150;

    [Header("Anuncio")]
    public float duracaoAnuncio = 3f;

    private Animator anim;
    private float vidaAtual;
    private bool fase2Ativa = false;
    private bool teleportando = false;
    private InimigoController inimigo;
    private MonoBehaviour movimentoComp;

    // UI da barra de vida
    private GameObject painelBoss;
    private RectTransform barraFill;
    private TextMeshProUGUI textoNome;
    private TextMeshProUGUI textoVida;
    private Image fillImg;

    private static readonly Color corNormal  = new Color(0.85f, 0.15f, 0.15f);
    private static readonly Color corFaseTwo = new Color(1f, 0.45f, 0.05f);

    void Awake()
    {
        inimigo = GetComponent<InimigoController>();
        if (inimigo != null)
        {
            inimigo.vidaMaxima      = vidaMaxima;
            inimigo.vidaAtual       = vidaMaxima;
            inimigo.danoAtual       = danoBase;
            inimigo.velocidadeBase  = velocidadeBase;
            inimigo.velocidadeAtual = velocidadeBase;
        }

        var danoComp = GetComponent<DanoInimigo>();
        if (danoComp != null) danoComp.dano = danoBase;

        gameObject.layer = 7;

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder     = sortingOrder;
        }

        // Guarda referência ao componente de movimento para pausar durante teleporte
        movimentoComp = GetComponent<movi_inimigo>() as MonoBehaviour;
    }

    void Start()
    {
        vidaAtual = vidaMaxima;
        anim = GetComponent<Animator>();

        if (anim != null && controllerFase1 != null)
            anim.runtimeAnimatorController = controllerFase1;

        CriarBarraUI();
        StartCoroutine(AnunciarBoss());
        StartCoroutine(RotinaTelepote());
    }

    void Update()
    {
        if (inimigo == null) return;

        vidaAtual = inimigo.vidaAtual;
        float pct = Mathf.Clamp01(vidaAtual / vidaMaxima);

        if (!fase2Ativa && pct <= gatilhoFase2)
            AtivarFase2();

        if (inimigo.estaMorrendo)
            DestruirBarraUI();

        if (painelBoss == null) return;

        if (barraFill != null)
            barraFill.anchorMax = new Vector2(pct, 1f);

        if (textoVida != null)
            textoVida.text = $"{Mathf.CeilToInt(vidaAtual)} / {Mathf.CeilToInt(vidaMaxima)}";
    }

    // ─── Fase 2 ──────────────────────────────────────────────────────────────

    void AtivarFase2()
    {
        fase2Ativa = true;
        Debug.Log("[Boss] Fase 2 ativada!");

        inimigo.danoAtual       *= multiplicadorDanoFase2;
        inimigo.velocidadeAtual *= multiplicadorVelocidadeFase2;

        var danoComp = GetComponent<DanoInimigo>();
        if (danoComp != null) danoComp.dano = inimigo.danoAtual;

        // Troca a animação pelo controller da fase 2
        if (anim != null && controllerFase2 != null)
            anim.runtimeAnimatorController = controllerFase2;

        // Tint laranja-avermelhado
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = corFase2;

        if (efeitoFase2 != null)
            Instantiate(efeitoFase2, transform.position, Quaternion.identity);

        // Spawn de minions ao redor
        if (prefabsMinions != null && prefabsMinions.Length > 0)
        {
            for (int i = 0; i < quantidadeMinions; i++)
            {
                GameObject prefab = prefabsMinions[Random.Range(0, prefabsMinions.Length)];
                if (prefab == null) continue;
                float angulo = i * (360f / quantidadeMinions) * Mathf.Deg2Rad;
                Vector2 pos = (Vector2)transform.position
                    + new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * raioSpawnMinions;
                Instantiate(prefab, pos, Quaternion.identity);
            }
        }

        if (fillImg != null) fillImg.color = corFaseTwo;

        if (textoNome != null)
            textoNome.text = $"⚡ {nomeBoss} ⚡";

        StartCoroutine(PiscarBoss());
    }

    // ─── Teleporte ───────────────────────────────────────────────────────────

    IEnumerator RotinaTelepote()
    {
        // Aguarda um momento inicial antes do primeiro teleporte
        yield return new WaitForSeconds(3f);

        while (inimigo == null || !inimigo.estaMorrendo)
        {
            float intervalo = fase2Ativa ? intervaloTeleporteFase2 : intervaloTeleporteFase1;
            yield return new WaitForSeconds(intervalo);

            if (inimigo != null && !inimigo.estaMorrendo)
                yield return StartCoroutine(Teleportar());
        }
    }

    IEnumerator Teleportar()
    {
        teleportando = true;

        // Pausa o movimento durante o teleporte
        if (movimentoComp != null) movimentoComp.enabled = false;

        var sr = GetComponent<SpriteRenderer>();
        var rb = GetComponent<Rigidbody2D>();

        // Efeito de saída
        if (efeitoTeleporte != null)
            Instantiate(efeitoTeleporte, transform.position, Quaternion.identity);

        // Pisca e some
        for (int i = 0; i < 3; i++)
        {
            if (sr != null) sr.enabled = false;
            yield return new WaitForSeconds(0.07f);
            if (sr != null) sr.enabled = true;
            yield return new WaitForSeconds(0.07f);
        }
        if (sr != null) sr.enabled = false;
        yield return new WaitForSeconds(0.15f);

        // Move para nova posição
        Vector2 novaPosicao = EscolherPosicaoTeleporte();
        if (rb != null)
            rb.MovePosition(novaPosicao);
        else
            transform.position = novaPosicao;

        yield return new WaitForSeconds(0.05f);

        // Efeito de chegada
        if (efeitoTeleporte != null)
            Instantiate(efeitoTeleporte, transform.position, Quaternion.identity);

        if (sr != null) sr.enabled = true;

        // Pisca na chegada
        for (int i = 0; i < 4; i++)
        {
            if (sr != null) sr.enabled = false;
            yield return new WaitForSeconds(0.06f);
            if (sr != null) sr.enabled = true;
            yield return new WaitForSeconds(0.06f);
        }

        // Retoma movimento
        if (movimentoComp != null) movimentoComp.enabled = true;
        teleportando = false;
    }

    Vector2 EscolherPosicaoTeleporte()
    {
        // Usa a posição do player como referência para teleportar ao redor dele
        var player = FindAnyObjectByType<PlayerStats>();
        Vector2 centro = player != null
            ? (Vector2)player.transform.position
            : (Vector2)transform.position;

        // Tenta até 10 vezes encontrar uma posição longe o suficiente do boss atual
        for (int tentativa = 0; tentativa < 10; tentativa++)
        {
            float angulo   = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distancia = Random.Range(raioTeleporteMin, raioTeleporteMax);
            Vector2 candidata = centro + new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * distancia;

            // Garante distância mínima da posição atual
            if (Vector2.Distance(candidata, transform.position) >= raioTeleporteMin)
                return candidata;
        }

        // Fallback: posição oposta ao boss em relação ao player
        Vector2 direcao = ((Vector2)transform.position - centro).normalized;
        return centro - direcao * raioTeleporteMin;
    }

    // ─── Efeitos Visuais ─────────────────────────────────────────────────────

    IEnumerator PiscarBoss()
    {
        var sr = GetComponent<SpriteRenderer>();
        for (int i = 0; i < 8; i++)
        {
            if (sr != null) sr.enabled = false;
            yield return new WaitForSeconds(0.07f);
            if (sr != null) sr.enabled = true;
            yield return new WaitForSeconds(0.07f);
        }
    }

    IEnumerator AnunciarBoss()
    {
        GameObject canvasGO = new GameObject("BossAnuncio");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        GameObject txtGO = new GameObject("Txt");
        txtGO.transform.SetParent(canvasGO.transform, false);
        var rt = txtGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.55f);
        rt.anchorMax = new Vector2(1f, 0.75f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = $"⚠ {nomeBoss} ⚠";
        tmp.fontSize  = 52f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = new Color(1f, 0.2f, 0.2f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;

        float t = 0f;
        while (t < duracaoAnuncio)
        {
            t += Time.deltaTime;
            float alpha = t < 0.4f
                ? t / 0.4f
                : (t > duracaoAnuncio - 0.5f ? (duracaoAnuncio - t) / 0.5f : 1f);
            float escala = 1f + Mathf.Sin(t * 8f) * 0.04f;
            tmp.color = new Color(1f, 0.2f, 0.2f, alpha);
            txtGO.transform.localScale = Vector3.one * escala;
            yield return null;
        }

        Destroy(canvasGO);
    }

    // ─── UI da Barra de Vida ─────────────────────────────────────────────────

    void CriarBarraUI()
    {
        GameObject canvasGO = new GameObject("BossBarCanvas");
        DontDestroyOnLoad(canvasGO);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        painelBoss = new GameObject("PainelBoss");
        painelBoss.transform.SetParent(canvasGO.transform, false);
        var rtPainel = painelBoss.AddComponent<RectTransform>();
        rtPainel.anchorMin        = new Vector2(0.15f, 0f);
        rtPainel.anchorMax        = new Vector2(0.85f, 0f);
        rtPainel.pivot            = new Vector2(0.5f, 0f);
        rtPainel.anchoredPosition = new Vector2(0f, 20f);
        rtPainel.sizeDelta        = new Vector2(0f, 54f);
        painelBoss.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.92f);

        textoNome = CriarTMP(painelBoss,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -4f), new Vector2(0f, 22f),
            nomeBoss, 15f, FontStyles.Bold, Color.white);

        GameObject barraBG = new GameObject("BarraBG");
        barraBG.transform.SetParent(painelBoss.transform, false);
        var rtBG = barraBG.AddComponent<RectTransform>();
        rtBG.anchorMin        = new Vector2(0f, 0f);
        rtBG.anchorMax        = new Vector2(1f, 0f);
        rtBG.pivot            = new Vector2(0.5f, 0f);
        rtBG.anchoredPosition = new Vector2(0f, 6f);
        rtBG.sizeDelta        = new Vector2(-16f, 14f);
        barraBG.AddComponent<Image>().color = new Color(0.15f, 0.05f, 0.05f);

        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(barraBG.transform, false);
        barraFill            = fillGO.AddComponent<RectTransform>();
        barraFill.anchorMin  = new Vector2(0f, 0f);
        barraFill.anchorMax  = new Vector2(1f, 1f);
        barraFill.offsetMin  = barraFill.offsetMax = Vector2.zero;
        fillImg              = fillGO.AddComponent<Image>();
        fillImg.color        = corNormal;

        textoVida = CriarTMP(painelBoss,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 22f), new Vector2(0f, 18f),
            "", 11f, FontStyles.Normal, new Color(0.8f, 0.8f, 0.8f));
    }

    void DestruirBarraUI()
    {
        if (painelBoss != null)
            StartCoroutine(FadeEDestruir());
    }

    IEnumerator FadeEDestruir()
    {
        var cg = painelBoss.GetComponent<CanvasGroup>()
               ?? painelBoss.AddComponent<CanvasGroup>();
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, t / 0.5f);
            yield return null;
        }
        Destroy(painelBoss.transform.parent.gameObject);
        painelBoss = null;
    }

    TextMeshProUGUI CriarTMP(GameObject parent,
        Vector2 ancMin, Vector2 ancMax, Vector2 pivot,
        Vector2 pos, Vector2 size,
        string texto, float fontSize, FontStyles style, Color cor)
    {
        var go = new GameObject("TMP");
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = size;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = texto;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = style;
        tmp.color     = cor;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }
}
