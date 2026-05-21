using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(InimigoController))]
public class BossController : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // IDENTIDADE
    // ──────────────────────────────────────────────
    [Header("Identidade")]
    public string nomeBoss = "Maga Slime";

    // ──────────────────────────────────────────────
    // FASES
    // ──────────────────────────────────────────────
    [Header("Fases")]
    [Tooltip("Porcentagem de vida para entrar na Fase 2 (0‑1)")]
    public float gatilhoFase2 = 0.5f;
    [Tooltip("Redução de dano recebido na Fase 2 (0.3 = 30% menos dano)")]
    public float reducaoDanoFase2 = 0.3f;

    // ──────────────────────────────────────────────
    // TELEPORTE
    // ──────────────────────────────────────────────
    [Header("Teleporte")]
    public float intervalTeleporteFase1 = 4f;
    public float intervalTeleporteFase2 = 2.5f;
    public float distMinTeleporte = 4f;
    public float distMaxTeleporte = 9f;
    public float duracaoFade = 0.25f;

    // ──────────────────────────────────────────────
    // ATAQUES
    // ──────────────────────────────────────────────
    [Header("Ataques")]
    public GameObject prefabProjetil;
    [Tooltip("Projéteis disparados por salva na Fase 1")]
    public int projeteisFase1 = 5;
    [Tooltip("Ângulo total do leque de projéteis (graus)")]
    public float anguloLeque = 70f;
    public float intervalAtaqueFase1 = 2.5f;
    public float intervalAtaqueFase2 = 1.4f;
    public float velocidadeProjetil = 7f;
    public float danoProjetil = 15f;

    [Header("Olho (ponto de spawn do projétil)")]
    [Tooltip("Offset do olho em relação ao pivot do boss (world units). Ajuste pelo gizmo na cena.")]
    public Vector2 offsetOlho = new Vector2(0.03f, 0.13f);

    // ──────────────────────────────────────────────
    // REFERÊNCIAS INTERNAS
    // ──────────────────────────────────────────────
    private InimigoController controller;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Transform player;

    private bool fase2Ativada = false;
    private int projeteis; // valor atual (muda na fase 2)

    // ──────────────────────────────────────────────
    // UI
    // ──────────────────────────────────────────────
    private GameObject bossCanvasGO;
    private Image hpFill;
    private Image hpFillGhost; // barra "fantasma" que atrasa
    private TextMeshProUGUI faseText;
    private TextMeshProUGUI hpText;

    // ──────────────────────────────────────────────────────────────
    // LIFECYCLE
    // ──────────────────────────────────────────────────────────────

    void Start()
    {
        controller    = GetComponent<InimigoController>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator      = GetComponent<Animator>();
        player        = GameObject.FindGameObjectWithTag("Player")?.transform;

        projeteis = projeteisFase1;

        CriarBossUI();
        StartCoroutine(SequenciaEntrada());
        StartCoroutine(LoopTeleporte());
        StartCoroutine(LoopAtaque());
    }

    void Update()
    {
        if (controller == null || controller.estaMorrendo) return;

        AtualizarUI();
        ChecarFase2();
    }

    void OnDrawGizmosSelected()
    {
        // Mostra o ponto de spawn do projétil (olho) em amarelo no editor
        Gizmos.color = Color.yellow;
        float sinalX = transform.localScale.x >= 0 ? 1f : -1f;
        Vector3 pos  = transform.position + new Vector3(offsetOlho.x * sinalX, offsetOlho.y, 0f);
        Gizmos.DrawWireSphere(pos, 0.08f);
        Gizmos.DrawLine(transform.position, pos);
    }

    void OnDestroy()
    {
        if (bossCanvasGO != null)
            Destroy(bossCanvasGO);
    }

    // ──────────────────────────────────────────────────────────────
    // VERIFICAÇÃO DE FASE
    // ──────────────────────────────────────────────────────────────

    void ChecarFase2()
    {
        if (fase2Ativada) return;
        if (controller.GetPorcentagemVida() <= gatilhoFase2)
        {
            fase2Ativada = true;
            StartCoroutine(TransicaoFase2());
        }
    }

    IEnumerator TransicaoFase2()
    {
        // Resistência a dano permanente
        controller.AplicarBuffDefesa(reducaoDanoFase2, 99999f);

        // Dobra os projéteis
        projeteis = projeteisFase1 * 2;

        // Muda animação (requer estado "Fase2" no AnimatorController)
        if (animator != null) animator.Play("Fase2");

        // Atualiza indicador de fase
        if (faseText != null)
        {
            faseText.text  = "FASE 2";
            faseText.color = new Color(1f, 0.4f, 0.1f);
        }

        // Flash de transição
        yield return StartCoroutine(FlashFase2());

        // Aviso de fase
        StartCoroutine(MostrarTextoTela("MODO FÚRIA ATIVADO!", new Color(1f, 0.3f, 0f), 2f));
    }

    IEnumerator FlashFase2()
    {
        Color corOriginal = spriteRenderer != null ? spriteRenderer.color : Color.white;
        for (int i = 0; i < 8; i++)
        {
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.07f);
            if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 0.3f, 0f);
            yield return new WaitForSeconds(0.07f);
        }
        if (spriteRenderer != null) spriteRenderer.color = corOriginal;
    }

    // ──────────────────────────────────────────────────────────────
    // TELEPORTE
    // ──────────────────────────────────────────────────────────────

    IEnumerator LoopTeleporte()
    {
        // Aguarda o boss aparecer antes de começar a se teleportar
        yield return new WaitForSeconds(intervalTeleporteFase1);

        while (!controller.estaMorrendo)
        {
            yield return StartCoroutine(Teleportar());
            float proximo = fase2Ativada ? intervalTeleporteFase2 : intervalTeleporteFase1;
            yield return new WaitForSeconds(proximo);
        }
    }

    IEnumerator Teleportar()
    {
        if (player == null || spriteRenderer == null) yield break;

        // Efeito de "carga" antes de sumir (pisca)
        for (int i = 0; i < 3; i++)
        {
            spriteRenderer.color = new Color(0.5f, 0f, 1f);
            yield return new WaitForSeconds(0.08f);
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.08f);
        }

        // Fade out
        yield return StartCoroutine(FadeAlpha(1f, 0f, duracaoFade));

        // Move
        transform.position = ObterPosicaoTeleporte();

        yield return null; // aguarda um frame para física resolver

        // Fade in
        yield return StartCoroutine(FadeAlpha(0f, 1f, duracaoFade));
    }

    Vector2 ObterPosicaoTeleporte()
    {
        if (player == null) return transform.position;

        for (int tentativa = 0; tentativa < 20; tentativa++)
        {
            float angulo   = Random.Range(0f, 360f);
            float dist     = Random.Range(distMinTeleporte, distMaxTeleporte);
            float rad      = angulo * Mathf.Deg2Rad;
            Vector2 alvo   = (Vector2)player.position + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * dist;

            if (!Physics2D.OverlapCircle(alvo, 0.6f, LayerMask.GetMask("Obstacles")))
                return alvo;
        }

        // Fallback: posição acima do player
        return (Vector2)player.position + Vector2.up * distMinTeleporte;
    }

    IEnumerator FadeAlpha(float de, float para, float duracao)
    {
        if (spriteRenderer == null) yield break;
        float t = 0f;
        Color c = spriteRenderer.color;
        while (t < duracao)
        {
            t   += Time.deltaTime;
            c.a  = Mathf.Lerp(de, para, t / duracao);
            spriteRenderer.color = c;
            yield return null;
        }
        c.a = para;
        spriteRenderer.color = c;
    }

    // ──────────────────────────────────────────────────────────────
    // ATAQUES
    // ──────────────────────────────────────────────────────────────

    IEnumerator LoopAtaque()
    {
        yield return new WaitForSeconds(2.5f); // delay inicial

        while (!controller.estaMorrendo)
        {
            float intervalo = fase2Ativada ? intervalAtaqueFase2 : intervalAtaqueFase1;
            yield return new WaitForSeconds(intervalo);

            if (!controller.estaMorrendo && player != null)
                Disparar();
        }
    }

    Vector3 PosicaoOlho()
    {
        float sinalX = spriteRenderer != null ? Mathf.Sign(transform.localScale.x) : 1f;
        return transform.position + new Vector3(offsetOlho.x * sinalX, offsetOlho.y, 0f);
    }

    void Disparar()
    {
        if (prefabProjetil == null || player == null) return;

        Vector3 spawnPos  = PosicaoOlho();
        Vector2 dirBase   = ((Vector2)player.position - (Vector2)spawnPos).normalized;
        float anguloBase  = Mathf.Atan2(dirBase.y, dirBase.x) * Mathf.Rad2Deg;

        for (int i = 0; i < projeteis; i++)
        {
            float t      = projeteis > 1 ? (float)i / (projeteis - 1) : 0.5f;
            float offset = Mathf.Lerp(-anguloLeque / 2f, anguloLeque / 2f, t);
            float ang    = (anguloBase + offset) * Mathf.Deg2Rad;
            Vector2 dir  = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));

            GameObject proj = Instantiate(prefabProjetil, spawnPos, Quaternion.identity);

            // Tenta ProjetilInimigoDano primeiro, depois projetil_inimigo como fallback
            ProjetilInimigoDano pid = proj.GetComponent<ProjetilInimigoDano>();
            if (pid != null)
            {
                pid.dano       = danoProjetil;
                pid.velocidade = velocidadeProjetil;
                pid.SetDirecao(dir);
            }
            else
            {
                projetil_inimigo pi = proj.GetComponent<projetil_inimigo>();
                if (pi != null)
                {
                    pi.velocidade = velocidadeProjetil;
                    pi.SetDirecao(dir);
                }
                else
                {
                    Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
                    if (rb != null) rb.linearVelocity = dir * velocidadeProjetil;
                }
            }
        }
    }

    // ──────────────────────────────────────────────────────────────
    // ENTRADA
    // ──────────────────────────────────────────────────────────────

    IEnumerator SequenciaEntrada()
    {
        // Boss começa invisível
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color; c.a = 0f;
            spriteRenderer.color = c;
        }

        // Exibe aviso antes de aparecer
        yield return StartCoroutine(MostrarAvisoBoss());

        // Aparece
        if (spriteRenderer != null)
            yield return StartCoroutine(FadeAlpha(0f, 1f, 0.6f));
    }

    IEnumerator MostrarAvisoBoss()
    {
        GameObject warnGO = new GameObject("BossWarning");
        Canvas cv = warnGO.AddComponent<Canvas>();
        cv.renderMode    = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder  = 200;
        warnGO.AddComponent<CanvasScaler>();

        // Fundo escuro
        GameObject bgGO = CriarUIGO("BG", warnGO.transform);
        ExpandirRect(bgGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0f);

        // Texto principal
        GameObject txtGO = CriarUIGO("WarnText", warnGO.transform);
        RectTransform tr = txtGO.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0.05f, 0.35f);
        tr.anchorMax = new Vector2(0.95f, 0.65f);
        tr.offsetMin = tr.offsetMax = Vector2.zero;
        TextMeshProUGUI txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = "⚠  BOSS APARECEU  ⚠\n<size=60%>" + nomeBoss.ToUpper() + "</size>";
        txt.fontSize  = 52;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color     = new Color(1f, 0.15f, 0.15f, 0f);

        // Fade in
        yield return StartCoroutine(FadeCanvasElements(bgImg, txt, 0f, 1f, 0.4f, maxAlphaBG: 0.65f));

        // Pulsa 3 vezes
        for (int i = 0; i < 3; i++)
        {
            txt.color = new Color(1f, 0.9f, 0.1f);
            yield return new WaitForSeconds(0.15f);
            txt.color = new Color(1f, 0.15f, 0.15f);
            yield return new WaitForSeconds(0.15f);
        }

        yield return new WaitForSeconds(0.5f);

        // Fade out
        yield return StartCoroutine(FadeCanvasElements(bgImg, txt, 1f, 0f, 0.4f, maxAlphaBG: 0.65f));

        Destroy(warnGO);
    }

    IEnumerator FadeCanvasElements(Image bg, TextMeshProUGUI txt, float de, float para, float dur, float maxAlphaBG = 0.65f)
    {
        float t = 0f;
        while (t < dur)
        {
            t  += Time.deltaTime;
            float a = Mathf.Lerp(de, para, t / dur);
            bg.color  = new Color(0f, 0f, 0f, a * maxAlphaBG);
            txt.color = new Color(txt.color.r, txt.color.g, txt.color.b, a);
            yield return null;
        }
    }

    // ──────────────────────────────────────────────────────────────
    // BOSS HEALTH BAR UI
    // ──────────────────────────────────────────────────────────────

    void CriarBossUI()
    {
        bossCanvasGO = new GameObject("BossCanvas");
        Canvas cv = bossCanvasGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 50;

        CanvasScaler cs = bossCanvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode            = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution    = new Vector2(1920, 1080);
        cs.matchWidthOrHeight     = 0.5f;
        bossCanvasGO.AddComponent<GraphicRaycaster>();

        // Painel de fundo — parte inferior da tela
        GameObject painel = CriarUIGO("BossPanel", bossCanvasGO.transform);
        RectTransform pr = painel.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.08f, 0.02f);
        pr.anchorMax = new Vector2(0.92f, 0.115f);
        pr.offsetMin = pr.offsetMax = Vector2.zero;
        Image painelImg = painel.AddComponent<Image>();
        painelImg.color = new Color(0.04f, 0.04f, 0.06f, 0.9f);

        // Linha decorativa no topo do painel
        GameObject linha = CriarUIGO("Linha", painel.transform);
        RectTransform lr = linha.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0f, 0.92f);
        lr.anchorMax = new Vector2(1f, 1f);
        lr.offsetMin = lr.offsetMax = Vector2.zero;
        Image linhaImg = linha.AddComponent<Image>();
        linhaImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);

        // Nome do boss
        GameObject nomeGO = CriarUIGO("NomeBoss", painel.transform);
        RectTransform nr = nomeGO.GetComponent<RectTransform>();
        nr.anchorMin = new Vector2(0.01f, 0.55f);
        nr.anchorMax = new Vector2(0.75f, 0.92f);
        nr.offsetMin = nr.offsetMax = Vector2.zero;
        TextMeshProUGUI nomeTxt = nomeGO.AddComponent<TextMeshProUGUI>();
        nomeTxt.text      = nomeBoss.ToUpper();
        nomeTxt.fontSize  = 20;
        nomeTxt.fontStyle = FontStyles.Bold;
        nomeTxt.color     = new Color(1f, 0.85f, 0.2f);
        nomeTxt.alignment = TextAlignmentOptions.MidlineLeft;

        // Indicador de fase
        GameObject faseGO = CriarUIGO("FaseBoss", painel.transform);
        RectTransform fr = faseGO.GetComponent<RectTransform>();
        fr.anchorMin = new Vector2(0.75f, 0.55f);
        fr.anchorMax = new Vector2(0.99f, 0.92f);
        fr.offsetMin = fr.offsetMax = Vector2.zero;
        faseText           = faseGO.AddComponent<TextMeshProUGUI>();
        faseText.text      = "FASE 1";
        faseText.fontSize  = 14;
        faseText.color     = new Color(0.75f, 0.75f, 0.75f);
        faseText.alignment = TextAlignmentOptions.MidlineRight;

        // Fundo da barra HP
        GameObject barBG = CriarUIGO("HPBarBG", painel.transform);
        RectTransform bbr = barBG.GetComponent<RectTransform>();
        bbr.anchorMin = new Vector2(0.01f, 0.08f);
        bbr.anchorMax = new Vector2(0.99f, 0.52f);
        bbr.offsetMin = bbr.offsetMax = Vector2.zero;
        barBG.AddComponent<Image>().color = new Color(0.12f, 0.04f, 0.04f);

        // Barra "fantasma" (atrasa para dar efeito de queima)
        GameObject ghostGO = CriarUIGO("HPGhost", barBG.transform);
        ExpandirRect(ghostGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        hpFillGhost      = ghostGO.AddComponent<Image>();
        hpFillGhost.type = Image.Type.Filled;
        hpFillGhost.fillMethod = Image.FillMethod.Horizontal;
        hpFillGhost.fillAmount = 1f;
        hpFillGhost.color      = new Color(0.9f, 0.6f, 0.1f, 0.7f);

        // Barra HP principal
        GameObject fillGO = CriarUIGO("HPFill", barBG.transform);
        ExpandirRect(fillGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        hpFill            = fillGO.AddComponent<Image>();
        hpFill.type       = Image.Type.Filled;
        hpFill.fillMethod = Image.FillMethod.Horizontal;
        hpFill.fillAmount = 1f;
        hpFill.color      = new Color(0.85f, 0.1f, 0.1f);

        // Texto HP (ex: 450 / 500)
        GameObject hpTextGO = CriarUIGO("HPText", barBG.transform);
        ExpandirRect(hpTextGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        hpText            = hpTextGO.AddComponent<TextMeshProUGUI>();
        hpText.fontSize   = 11;
        hpText.color      = Color.white;
        hpText.fontStyle  = FontStyles.Bold;
        hpText.alignment  = TextAlignmentOptions.Center;

        // Marcador de 50% (linha vertical no meio)
        GameObject marca = CriarUIGO("Marca50", barBG.transform);
        RectTransform mr = marca.GetComponent<RectTransform>();
        mr.anchorMin = new Vector2(0.5f, 0f);
        mr.anchorMax = new Vector2(0.5f, 1f);
        mr.sizeDelta = new Vector2(2f, 0f);
        mr.offsetMin = new Vector2(-1f, 0f);
        mr.offsetMax = new Vector2(1f, 0f);
        marca.AddComponent<Image>().color = new Color(1f, 1f, 0f, 0.6f);

        // Anima entrada do painel
        StartCoroutine(EntradaUI(painel));
    }

    IEnumerator EntradaUI(GameObject painel)
    {
        CanvasGroup cg = painel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        float t = 0f;
        while (t < 1f) { t += Time.deltaTime * 1.5f; cg.alpha = Mathf.Lerp(0f, 1f, t); yield return null; }
        cg.alpha = 1f;
    }

    void AtualizarUI()
    {
        if (hpFill == null || controller == null) return;

        float pct = controller.GetPorcentagemVida();

        // Barra principal atualiza instantaneamente
        hpFill.fillAmount = pct;

        // Cor: vermelho escuro → laranja → vermelho vivo conforme fase
        hpFill.color = fase2Ativada
            ? new Color(1f, 0.3f + pct * 0.2f, 0f)
            : Color.Lerp(new Color(0.85f, 0.1f, 0.1f), new Color(0.9f, 0.7f, 0f), pct);

        // Barra fantasma atrasa para dar efeito de "queima" da vida
        hpFillGhost.fillAmount = Mathf.MoveTowards(hpFillGhost.fillAmount, pct, Time.deltaTime * 0.6f);

        // Texto numérico de HP
        if (hpText != null)
            hpText.text = $"{Mathf.RoundToInt(controller.vidaAtual)} / {Mathf.RoundToInt(controller.vidaMaxima)}";
    }

    // ──────────────────────────────────────────────────────────────
    // TEXTO FLUTUANTE NA TELA
    // ──────────────────────────────────────────────────────────────

    IEnumerator MostrarTextoTela(string mensagem, Color cor, float duracao)
    {
        GameObject go = new GameObject("BossMsg");
        Canvas cv = go.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 150;
        go.AddComponent<CanvasScaler>();

        GameObject txtGO = CriarUIGO("Msg", go.transform);
        RectTransform tr = txtGO.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0.1f, 0.55f);
        tr.anchorMax = new Vector2(0.9f, 0.75f);
        tr.offsetMin = tr.offsetMax = Vector2.zero;
        TextMeshProUGUI txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = mensagem;
        txt.fontSize  = 38;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color     = new Color(cor.r, cor.g, cor.b, 0f);

        // Fade in
        float t = 0f;
        while (t < 0.3f) { t += Time.deltaTime; txt.color = new Color(cor.r, cor.g, cor.b, t / 0.3f); yield return null; }
        txt.color = new Color(cor.r, cor.g, cor.b, 1f);

        yield return new WaitForSeconds(duracao);

        // Fade out + sobe
        t = 0f;
        RectTransform msgRect = txtGO.GetComponent<RectTransform>();
        Vector2 posBase = msgRect.anchoredPosition;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            float a = 1f - (t / 0.5f);
            txt.color = new Color(cor.r, cor.g, cor.b, a);
            msgRect.anchoredPosition = posBase + Vector2.up * (t * 120f);
            yield return null;
        }

        Destroy(go);
    }

    // ──────────────────────────────────────────────────────────────
    // HELPERS DE UI
    // ──────────────────────────────────────────────────────────────

    static GameObject CriarUIGO(string nome, Transform pai)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static void ExpandirRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
