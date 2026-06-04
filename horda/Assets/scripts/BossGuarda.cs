using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(InimigoController), typeof(Rigidbody2D))]
public class BossGuarda : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // IDENTIDADE
    // ──────────────────────────────────────────────
    [Header("Identidade")]
    public string nomeBoss = "Guardião";

    // ──────────────────────────────────────────────
    // MOVIMENTO / PATRULHA
    // ──────────────────────────────────────────────
    [Header("Movimento")]
    public float velocidadePatrulha  = 2.2f;
    public float velocidadeAproximar = 3.5f;
    public float distanciaDeteccao   = 8f;
    [Tooltip("Intervalo mínimo para mudar direção ao patrulhar")]
    public float minTempoWander      = 1.5f;
    [Tooltip("Intervalo máximo para mudar direção ao patrulhar")]
    public float maxTempoWander      = 3.5f;

    // ──────────────────────────────────────────────
    // CURA (50% HP)
    // ──────────────────────────────────────────────
    [Header("Cura (abaixo de 50%)")]
    public float curaGatilho         = 0.5f;
    public float curaPorTick         = 6f;
    public float intervaloCura       = 3f;

    // ──────────────────────────────────────────────
    // SPRITES / UI
    // ──────────────────────────────────────────────
    [Header("Sprites (opcional)")]
    public Sprite sprHpFrame;
    public Sprite sprHpFill;
    public Sprite sprHpBg;

    // ──────────────────────────────────────────────
    // INTERNOS
    // ──────────────────────────────────────────────
    enum Estado { Patrulha, Aproximar }

    InimigoController controller;
    Rigidbody2D       rb;
    SpriteRenderer    sr;
    Animator          anim;
    Transform         player;

    Estado estado = Estado.Patrulha;

    bool    curaAtivada = false;

    Vector2 dirWander   = Vector2.right;
    float   wanderTimer = 0f;

    // UI
    GameObject bossCanvasGO;
    Image      hpFill;
    Image      hpFillGhost;
    TextMeshProUGUI hpText;
    TextMeshProUGUI faseText;

    // ──────────────────────────────────────────────────────────────
    // LIFECYCLE
    // ──────────────────────────────────────────────────────────────

    void Start()
    {
        controller = GetComponent<InimigoController>();
        rb         = GetComponent<Rigidbody2D>();
        sr         = GetComponent<SpriteRenderer>();
        anim       = GetComponent<Animator>();

        rb.gravityScale = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        EscolherDirecaoWander();
        CriarBossUI();
        StartCoroutine(SequenciaEntrada());
    }

    void Update()
    {
        if (controller == null || controller.estaMorrendo) return;

        AtualizarUI();
        VerificarCura();

        float distPlayer = player != null ? Vector2.Distance(transform.position, player.position) : 999f;

        if (distPlayer <= distanciaDeteccao)
            estado = Estado.Aproximar;
        else
            estado = Estado.Patrulha;
    }

    void FixedUpdate()
    {
        if (controller == null || controller.estaMorrendo) { rb.linearVelocity = Vector2.zero; return; }

        switch (estado)
        {
            case Estado.Patrulha:  Mover(dirWander, velocidadePatrulha);  TickWander(); break;
            case Estado.Aproximar: MoverParaPlayer(velocidadeAproximar);  break;
        }
    }

    // ──────────────────────────────────────────────────────────────
    // MOVIMENTO
    // ──────────────────────────────────────────────────────────────

    void Mover(Vector2 dir, float vel)
    {
        rb.linearVelocity = dir.normalized * vel;
        VirarParaDirecao(dir);
    }

    void MoverParaPlayer(float vel)
    {
        if (player == null) return;
        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        rb.linearVelocity = dir * vel;
        VirarParaDirecao(dir);
    }

    void VirarParaDirecao(Vector2 dir)
    {
        if (sr == null || Mathf.Abs(dir.x) < 0.1f) return;
        sr.flipX = dir.x < 0f;
    }

    void TickWander()
    {
        wanderTimer -= Time.fixedDeltaTime;
        if (wanderTimer <= 0f) EscolherDirecaoWander();
    }

    void EscolherDirecaoWander()
    {
        float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        dirWander   = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
        wanderTimer = Random.Range(minTempoWander, maxTempoWander);
    }

    // ──────────────────────────────────────────────────────────────
    // CURA (50% HP)
    // ──────────────────────────────────────────────────────────────

    void VerificarCura()
    {
        if (curaAtivada || controller == null) return;
        if (controller.GetPorcentagemVida() <= curaGatilho)
        {
            curaAtivada = true;

            if (faseText != null)
            {
                faseText.text  = "FURIOSO";
                faseText.color = new Color(0.2f, 1f, 0.4f);
            }

            StartCoroutine(MostrarTextoTela("MODO FURIA — REGENERANDO!", new Color(0.2f, 1f, 0.4f), 2f));
            StartCoroutine(LoopCura());
        }
    }

    IEnumerator LoopCura()
    {
        anim?.SetBool("Curando", true);
        while (!controller.estaMorrendo)
        {
            yield return new WaitForSeconds(intervaloCura);
            if (controller.estaMorrendo) break;

            float cura = curaPorTick;
            controller.vidaAtual = Mathf.Min(controller.vidaAtual + cura, controller.vidaMaxima);

            StartCoroutine(EfeitoCura(cura));
        }
        anim?.SetBool("Curando", false);
    }

    IEnumerator EfeitoCura(float quantidade)
    {
        // Flash verde breve
        if (sr != null)
        {
            for (int i = 0; i < 3; i++)
            {
                sr.color = new Color(0.2f, 1f, 0.4f);
                yield return new WaitForSeconds(0.07f);
                sr.color = Color.white;
                yield return new WaitForSeconds(0.07f);
            }
        }

        // Número de cura flutuante
        SpawnNumeroCura(quantidade);

        // Partículas verdes subindo
        StartCoroutine(ParticulasCura());
    }

    void SpawnNumeroCura(float valor)
    {
        if (DamageNumberManager.Instance == null) return;

        GameObject numGO = new GameObject("NumCura");
        numGO.transform.position = transform.position + Vector3.up * 0.5f;

        Canvas cv = numGO.AddComponent<Canvas>();
        cv.renderMode  = RenderMode.WorldSpace;
        cv.sortingOrder = 50;

        GameObject txtGO = new GameObject("Txt");
        txtGO.transform.SetParent(numGO.transform, false);
        RectTransform rt = txtGO.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(2f, 0.6f);

        TextMeshProUGUI txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = $"+{valor:F0}";
        txt.fontSize  = 4f;
        txt.fontStyle = FontStyles.Bold;
        txt.color     = new Color(0.2f, 1f, 0.4f);
        txt.alignment = TextAlignmentOptions.Center;

        numGO.transform.localScale = Vector3.one * 0.5f;
        Destroy(numGO, 1.2f);
        StartCoroutine(AnimarNumCura(numGO.transform));
    }

    IEnumerator AnimarNumCura(Transform t)
    {
        float dur = 1.0f, elapsed = 0f;
        Vector3 posIni = t.position;
        while (elapsed < dur && t != null)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / dur;
            t.position = posIni + Vector3.up * (p * 0.8f);
            if (t.GetComponentInChildren<TextMeshProUGUI>() is TextMeshProUGUI txt)
            {
                Color c = txt.color; c.a = 1f - Mathf.Pow(p, 2f); txt.color = c;
            }
            yield return null;
        }
    }

    IEnumerator ParticulasCura()
    {
        int sortL = sr != null ? sr.sortingLayerID : 0;
        int sortO = sr != null ? sr.sortingOrder   : 0;

        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        for (int y = 0; y < 4; y++)
        for (int x = 0; x < 4; x++)
        {
            float d = Vector2.Distance(new Vector2(x+0.5f,y+0.5f), new Vector2(2f,2f));
            tex.SetPixel(x, y, new Color(0.3f, 1f, 0.5f, Mathf.Clamp01(1f - d/2f)));
        }
        tex.Apply();
        Sprite spr = Sprite.Create(tex, new Rect(0,0,4,4), new Vector2(0.5f,0.5f), 16f);

        int qtd = 6;
        var gos  = new GameObject[qtd];
        var vels = new Vector2[qtd];

        for (int i = 0; i < qtd; i++)
        {
            float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            vels[i] = new Vector2(Mathf.Cos(ang) * 0.4f, Mathf.Sin(ang) * 0.4f + 0.8f);

            GameObject p = new GameObject("PartCura");
            p.transform.position   = transform.position + new Vector3(Random.Range(-0.3f, 0.3f), 0f, 0f);
            p.transform.localScale = Vector3.one * Random.Range(0.08f, 0.15f);

            SpriteRenderer srP = p.AddComponent<SpriteRenderer>();
            srP.sprite         = spr;
            srP.sortingLayerID = sortL;
            srP.sortingOrder   = sortO + 1;
            gos[i] = p;
        }

        float t = 0f, dur = 0.9f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p01 = t / dur;
            for (int i = 0; i < qtd; i++)
            {
                if (gos[i] == null) continue;
                gos[i].transform.position += (Vector3)(vels[i] * Time.deltaTime);
                vels[i] *= 0.96f;
                SpriteRenderer srP = gos[i].GetComponent<SpriteRenderer>();
                if (srP != null) { Color c = srP.color; c.a = 1f - p01; srP.color = c; }
            }
            yield return null;
        }

        for (int i = 0; i < qtd; i++)
            if (gos[i] != null) Destroy(gos[i]);
    }

    // ──────────────────────────────────────────────────────────────
    // ENTRADA
    // ──────────────────────────────────────────────────────────────

    IEnumerator SequenciaEntrada()
    {
        if (sr != null) { Color c = sr.color; c.a = 0f; sr.color = c; }

        yield return StartCoroutine(MostrarAvisoBoss());
        CameraShaker.Tremer(0.05f, 1.5f);

        yield return StartCoroutine(FadeAlpha(0f, 1f, 0.5f));
    }

    IEnumerator FadeAlpha(float de, float para, float dur)
    {
        if (sr == null) yield break;
        float t = 0f;
        Color c = sr.color;
        while (t < dur)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(de, para, t / dur);
            sr.color = c;
            yield return null;
        }
        c.a = para; sr.color = c;
    }

    IEnumerator MostrarAvisoBoss()
    {
        GameObject warnGO = new GameObject("BossWarning");
        Canvas cv = warnGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 200;
        warnGO.AddComponent<CanvasScaler>();

        GameObject bgGO = CriarUIGO("BG", warnGO.transform);
        ExpandirRect(bgGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0f);

        GameObject txtGO = CriarUIGO("WarnText", warnGO.transform);
        RectTransform tr = txtGO.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0.05f, 0.35f);
        tr.anchorMax = new Vector2(0.95f, 0.65f);
        tr.offsetMin = tr.offsetMax = Vector2.zero;
        TextMeshProUGUI txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = $"!  BOSS APARECEU  !\n<size=60%>{nomeBoss.ToUpper()}</size>";
        txt.fontSize  = 52;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color     = new Color(1f, 0.6f, 0.1f, 0f);

        // Fade in
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            float a = t / 0.4f;
            bgImg.color = new Color(0f, 0f, 0f, a * 0.65f);
            txt.color   = new Color(1f, 0.6f, 0.1f, a);
            yield return null;
        }

        // Pulsa
        for (int i = 0; i < 3; i++)
        {
            txt.color = new Color(1f, 0.9f, 0.1f, 1f);
            yield return new WaitForSeconds(0.15f);
            txt.color = new Color(1f, 0.5f, 0.05f, 1f);
            yield return new WaitForSeconds(0.15f);
        }

        yield return new WaitForSeconds(0.4f);

        // Fade out
        t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            float a = 1f - t / 0.4f;
            bgImg.color = new Color(0f, 0f, 0f, a * 0.65f);
            txt.color   = new Color(1f, 0.6f, 0.1f, a);
            yield return null;
        }

        Destroy(warnGO);
    }

    // ──────────────────────────────────────────────────────────────
    // BOSS UI
    // ──────────────────────────────────────────────────────────────

    void CriarBossUI()
    {
        bossCanvasGO = new GameObject("BossGuardaCanvas");
        Canvas cv = bossCanvasGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 50;

        CanvasScaler cs = bossCanvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight  = 0.5f;
        bossCanvasGO.AddComponent<GraphicRaycaster>();

        GameObject painel = CriarUIGO("BossPanel", bossCanvasGO.transform);
        RectTransform pr = painel.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.2f, 0.825f);
        pr.anchorMax = new Vector2(0.8f, 0.935f);
        pr.offsetMin = pr.offsetMax = Vector2.zero;
        painel.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.06f, 0.9f);

        // Linha decorativa laranja (tema guardião)
        GameObject linha = CriarUIGO("Linha", painel.transform);
        RectTransform lr = linha.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0f, 0.92f); lr.anchorMax = new Vector2(1f, 1f);
        lr.offsetMin = lr.offsetMax = Vector2.zero;
        linha.AddComponent<Image>().color = new Color(0.9f, 0.55f, 0.1f);

        // Nome
        GameObject nomeGO = CriarUIGO("Nome", painel.transform);
        RectTransform nr = nomeGO.GetComponent<RectTransform>();
        nr.anchorMin = new Vector2(0.01f, 0.6f); nr.anchorMax = new Vector2(0.99f, 0.92f);
        nr.offsetMin = nr.offsetMax = Vector2.zero;
        TextMeshProUGUI nomeTxt = nomeGO.AddComponent<TextMeshProUGUI>();
        nomeTxt.text      = nomeBoss.ToUpper();
        nomeTxt.fontSize  = 20;
        nomeTxt.fontStyle = FontStyles.Bold;
        nomeTxt.color     = new Color(1f, 0.8f, 0.2f);
        nomeTxt.alignment = TextAlignmentOptions.Center;

        // Fase
        GameObject faseGO = CriarUIGO("Fase", painel.transform);
        RectTransform fr = faseGO.GetComponent<RectTransform>();
        fr.anchorMin = new Vector2(0.75f, 0.92f); fr.anchorMax = new Vector2(0.99f, 1f);
        fr.offsetMin = fr.offsetMax = Vector2.zero;
        faseText           = faseGO.AddComponent<TextMeshProUGUI>();
        faseText.text      = "PATRULHANDO";
        faseText.fontSize  = 13;
        faseText.color     = new Color(0.75f, 0.75f, 0.75f);
        faseText.alignment = TextAlignmentOptions.MidlineRight;

        // Barra HP
        GameObject barBG = CriarUIGO("HPBarBG", painel.transform);
        RectTransform bbr = barBG.GetComponent<RectTransform>();
        bbr.anchorMin = new Vector2(0.01f, 0.08f); bbr.anchorMax = new Vector2(0.99f, 0.52f);
        bbr.offsetMin = bbr.offsetMax = Vector2.zero;
        Image bgImg = barBG.AddComponent<Image>();
        if (sprHpBg != null) { bgImg.sprite = sprHpBg; bgImg.type = Image.Type.Tiled; }
        else bgImg.color = new Color(0.1f, 0.1f, 0.12f);

        GameObject ghostGO = CriarUIGO("HPGhost", barBG.transform);
        ExpandirRect(ghostGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        hpFillGhost            = ghostGO.AddComponent<Image>();
        hpFillGhost.type       = Image.Type.Filled;
        hpFillGhost.fillMethod = Image.FillMethod.Horizontal;
        hpFillGhost.fillAmount = 1f;
        hpFillGhost.color      = new Color(1f, 0.88f, 0.25f, 0.9f);

        GameObject fillGO = CriarUIGO("HPFill", barBG.transform);
        ExpandirRect(fillGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        hpFill            = fillGO.AddComponent<Image>();
        if (sprHpFill != null) { hpFill.sprite = sprHpFill; hpFill.color = Color.white; }
        else hpFill.color = new Color(0.9f, 0.55f, 0.1f);
        hpFill.type       = Image.Type.Filled;
        hpFill.fillMethod = Image.FillMethod.Horizontal;
        hpFill.fillAmount = 1f;

        GameObject hpTextGO = CriarUIGO("HPText", barBG.transform);
        ExpandirRect(hpTextGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        hpText           = hpTextGO.AddComponent<TextMeshProUGUI>();
        hpText.fontSize  = 11;
        hpText.color     = Color.white;
        hpText.fontStyle = FontStyles.Bold;
        hpText.alignment = TextAlignmentOptions.Center;

        if (sprHpFrame != null)
        {
            GameObject frameGO = CriarUIGO("HPFrame", barBG.transform);
            ExpandirRect(frameGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            Image frameImg = frameGO.AddComponent<Image>();
            frameImg.sprite        = sprHpFrame;
            frameImg.type          = Image.Type.Sliced;
            frameImg.raycastTarget = false;
        }

        StartCoroutine(EntradaUI(painel));
    }

    IEnumerator EntradaUI(GameObject painel)
    {
        CanvasGroup cg = painel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        float t = 0f;
        while (t < 1f) { t += Time.deltaTime * 1.5f; cg.alpha = t; yield return null; }
        cg.alpha = 1f;
    }

    void AtualizarUI()
    {
        if (hpFill == null || controller == null) return;

        float pct = controller.GetPorcentagemVida();

        hpFill.fillAmount = Mathf.Lerp(hpFill.fillAmount, pct, Time.deltaTime * 3f);

        hpFill.color = curaAtivada
            ? new Color(0.2f, 0.9f, 0.4f)
            : (sprHpFill != null ? Color.white : new Color(0.9f, 0.55f, 0.1f));

        hpFillGhost.fillAmount = Mathf.MoveTowards(hpFillGhost.fillAmount, pct, Time.deltaTime * 0.3f);

        if (hpText != null)
            hpText.text = $"{Mathf.RoundToInt(controller.vidaAtual)} / {Mathf.RoundToInt(controller.vidaMaxima)}";

        // Atualiza label de estado
        if (faseText != null && !curaAtivada)
        {
            faseText.text = estado switch
            {
                Estado.Patrulha  => "PATRULHANDO",
                Estado.Aproximar => "CAÇANDO",
                _                => faseText.text
            };
        }
    }

    // ──────────────────────────────────────────────────────────────
    // TEXTO NA TELA
    // ──────────────────────────────────────────────────────────────

    IEnumerator MostrarTextoTela(string mensagem, Color cor, float duracao)
    {
        GameObject go = new GameObject("BossGuardaMsg");
        Canvas cv = go.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 150;
        go.AddComponent<CanvasScaler>();

        GameObject txtGO = CriarUIGO("Msg", go.transform);
        RectTransform tr = txtGO.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0.1f, 0.55f); tr.anchorMax = new Vector2(0.9f, 0.75f);
        tr.offsetMin = tr.offsetMax = Vector2.zero;
        TextMeshProUGUI txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = mensagem;
        txt.fontSize  = 36;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color     = new Color(cor.r, cor.g, cor.b, 0f);

        float t = 0f;
        while (t < 0.3f) { t += Time.deltaTime; txt.color = new Color(cor.r, cor.g, cor.b, t / 0.3f); yield return null; }
        txt.color = new Color(cor.r, cor.g, cor.b, 1f);

        yield return new WaitForSeconds(duracao);

        t = 0f;
        RectTransform msgR = txtGO.GetComponent<RectTransform>();
        Vector2 posBase = msgR.anchoredPosition;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            txt.color = new Color(cor.r, cor.g, cor.b, 1f - t / 0.5f);
            msgR.anchoredPosition = posBase + Vector2.up * (t * 100f);
            yield return null;
        }

        Destroy(go);
    }

    // ──────────────────────────────────────────────────────────────
    // MORTE
    // ──────────────────────────────────────────────────────────────

    public void IniciarEfeitoMorte() => StartCoroutine(EfeitoMorte());

    IEnumerator EfeitoMorte()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        rb.linearVelocity = Vector2.zero;

        CameraShaker.Tremer(0.1f, 2.5f);

        for (int i = 0; i < 10; i++)
        {
            if (sr != null) sr.color = i % 2 == 0 ? Color.white : new Color(1f, 0.5f, 0.1f);
            yield return new WaitForSeconds(0.05f);
        }

        Vector3 escBase = transform.localScale;
        float t = 0f, dur = 0.9f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            transform.localScale = escBase * Mathf.Lerp(1f, 2.5f, Mathf.Pow(p, 0.5f));
            if (sr != null) sr.color = new Color(1f, 0.8f, 0.4f, Mathf.Lerp(1f, 0f, p * p));
            yield return null;
        }

        BossMorteUI.Exibir("GUARDIÃO DERROTADO!", new Color(1f, 0.8f, 0.2f));
        if (bossCanvasGO != null) Destroy(bossCanvasGO);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (bossCanvasGO != null) Destroy(bossCanvasGO);
    }

    // ──────────────────────────────────────────────────────────────
    // GIZMOS
    // ──────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaDeteccao);
    }

    // ──────────────────────────────────────────────────────────────
    // HELPERS UI
    // ──────────────────────────────────────────────────────────────

    static GameObject CriarUIGO(string nome, Transform pai)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static void ExpandirRect(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
