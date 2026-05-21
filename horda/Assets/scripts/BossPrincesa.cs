using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(InimigoController))]
public class BossPrincesa : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // IDENTIDADE
    // ──────────────────────────────────────────────
    [Header("Identidade")]
    public string nomeBoss = "Princesa";

    // ──────────────────────────────────────────────
    // VOO
    // ──────────────────────────────────────────────
    [Header("Voo")]
    public float velocidadeVoo     = 4f;
    public float tempoVooMin       = 2f;
    public float tempoVooMax       = 4.5f;
    public float tempoParada       = 1.5f;
    public float raioPadraoVoo     = 7f;    // raio ao redor do centro do mapa

    // ──────────────────────────────────────────────
    // ATAQUE / CANALIZAÇÃO
    // ──────────────────────────────────────────────
    [Header("Canalização")]
    public GameObject prefabProjetil;
    public float velocidadeProjetil = 6f;
    public float danoProjetil       = 20f;
    public float duracaoProjetil    = 8f;
    [Tooltip("Duração da canalização — deve bater com a animação de ataque")]
    public float duracaoCanalização = 2.5f;
    public float raioOrbita         = 1.4f;

    [Header("Sequência de Projéteis")]
    [Tooltip("Quantidade de projéteis por canalização: 4 → 8 → 16 (e se repete em 16)")]
    public int[] sequenciaProjeteis = { 4, 8, 16 };

    // ──────────────────────────────────────────────
    // FASE 2
    // ──────────────────────────────────────────────
    [Header("Fase 2 — 50% de vida")]
    public float gatilhoFase2              = 0.5f;
    public float buffVelocidadeInimigos    = 1.5f;  // multiplicador
    public float forcaOndaEmpurrao        = 12f;
    public float raioOnda                 = 5f;

    // ──────────────────────────────────────────────
    // REFERÊNCIAS
    // ──────────────────────────────────────────────
    private InimigoController controller;
    private SpriteRenderer      spriteRenderer;
    private Animator            animator;
    private Transform           player;
    private PlayerStats         playerStats;
    private Rigidbody2D         rb;

    private bool fase2Ativada    = false;
    private int  ataqueCicloIdx  = 0;
    private int  indiceEspecial  = 0;
    private float velocidadeOrbita = 120f;

    // orbita durante canalização
    private readonly List<GameObject> projeteisCanalizando = new List<GameObject>();

    // UI
    private GameObject bossCanvasGO;
    private Image       hpFill;
    private TextMeshProUGUI faseText;

    // ──────────────────────────────────────────────
    // LIFECYCLE
    // ──────────────────────────────────────────────

    void Start()
    {
        controller     = GetComponent<InimigoController>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator       = GetComponent<Animator>();
        rb             = GetComponent<Rigidbody2D>();

        var pGO = GameObject.FindGameObjectWithTag("Player");
        if (pGO != null)
        {
            player      = pGO.transform;
            playerStats = pGO.GetComponent<PlayerStats>();
        }

        // Desabilita movimento padrão do inimigo
        foreach (var m in GetComponents<MonoBehaviour>())
        {
            if (m == this) continue;
            if (m is InimigoController) continue;
            var t = m.GetType();
            if (t.Name.StartsWith("movi_") || t.Name.StartsWith("FlowField"))
                m.enabled = false;
        }

        if (rb != null)
        {
            rb.gravityScale   = 0f;
            rb.linearDamping  = 3f;
            rb.constraints    = RigidbodyConstraints2D.FreezeRotation;
        }

        CriarBossUI();
        StartCoroutine(SequenciaEntrada());
    }

    void Update()
    {
        if (controller == null || controller.estaMorrendo) return;

        AtualizarUI();
        VerificarFase2();
    }

    // ──────────────────────────────────────────────
    // FASE 2
    // ──────────────────────────────────────────────

    void VerificarFase2()
    {
        if (fase2Ativada) return;
        if (controller.vidaAtual <= controller.vidaMaxima * gatilhoFase2)
            AtivarFase2();
    }

    void AtivarFase2()
    {
        fase2Ativada = true;

        // Modo frenético — reduz tempos, aumenta velocidades
        tempoVooMin        *= 0.5f;
        tempoVooMax        *= 0.5f;
        tempoParada        *= 0.4f;
        duracaoCanalização *= 0.6f;
        velocidadeVoo      *= 1.6f;
        velocidadeProjetil *= 1.3f;
        velocidadeOrbita    = 260f;

        // Buff de velocidade em todos os inimigos vivos
        foreach (var inimigo in FindObjectsByType<InimigoController>(FindObjectsSortMode.None))
        {
            if (inimigo == controller) continue;
            inimigo.velocidadeAtual *= buffVelocidadeInimigos;
        }

        StartCoroutine(FlashFase2());

        if (faseText != null)
            faseText.text = "FASE 2";
    }

    IEnumerator FlashFase2()
    {
        for (int i = 0; i < 6; i++)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = i % 2 == 0 ? new Color(1f, 0.4f, 0.9f) : Color.white;
            yield return new WaitForSeconds(0.08f);
        }
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
    }

    // ──────────────────────────────────────────────
    // SEQUÊNCIA ENTRADA
    // ──────────────────────────────────────────────

    IEnumerator SequenciaEntrada()
    {
        // Esconde com enabled=false para não corromper corOriginal do InimigoController
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        yield return StartCoroutine(MostrarAvisoBoss());

        CameraShaker.Tremer(0.05f, 2.2f);
        StartCoroutine(ExplosaoEntrada(2.2f));

        // Reativa e faz fade-in sem tocar no alpha da corOriginal
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color   = new Color(1f, 1f, 1f, 0f);
        }
        yield return StartCoroutine(FadeAlpha(0f, 1f, 0.7f));

        StartCoroutine(LoopComportamento());
    }

    IEnumerator MostrarAvisoBoss()
    {
        GameObject warnGO = new GameObject("BossPrincesaWarning");
        Canvas cv = warnGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 200;
        warnGO.AddComponent<CanvasScaler>();

        // Fundo escuro
        GameObject bgGO = new GameObject("BG");
        bgGO.transform.SetParent(warnGO.transform, false);
        RectTransform bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0f);

        // Texto principal
        GameObject txtGO = new GameObject("WarnText");
        txtGO.transform.SetParent(warnGO.transform, false);
        RectTransform tr = txtGO.AddComponent<RectTransform>();
        tr.anchorMin = new Vector2(0.05f, 0.35f);
        tr.anchorMax = new Vector2(0.95f, 0.65f);
        tr.offsetMin = tr.offsetMax = Vector2.zero;
        TextMeshProUGUI txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = "⚠  BOSS APARECEU  ⚠\n<size=60%>" + nomeBoss.ToUpper() + "</size>";
        txt.fontSize  = 52;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color     = new Color(0.9f, 0.3f, 1f, 0f);

        // Fade in
        yield return StartCoroutine(FadeCanvasElements(bgImg, txt, 0f, 1f, 0.4f));

        // Pulsa rosa/roxo 3 vezes
        for (int i = 0; i < 3; i++)
        {
            txt.color = new Color(1f, 0.5f, 1f, 1f);
            yield return new WaitForSeconds(0.15f);
            txt.color = new Color(0.7f, 0.1f, 1f, 1f);
            yield return new WaitForSeconds(0.15f);
        }

        yield return new WaitForSeconds(0.5f);

        // Fade out
        yield return StartCoroutine(FadeCanvasElements(bgImg, txt, 1f, 0f, 0.4f));

        Destroy(warnGO);
    }

    IEnumerator FadeCanvasElements(Image bg, TextMeshProUGUI txt, float de, float para, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t  += Time.deltaTime;
            float a = Mathf.Lerp(de, para, t / dur);
            bg.color  = new Color(0f, 0f, 0f, a * 0.6f);
            txt.color = new Color(txt.color.r, txt.color.g, txt.color.b, a);
            yield return null;
        }
    }

    IEnumerator FadeAlpha(float de, float para, float duracao)
    {
        if (spriteRenderer == null) yield break;
        float t = 0f;
        while (t < duracao)
        {
            t += Time.deltaTime;
            Color c = spriteRenderer.color;
            c.a = Mathf.Lerp(de, para, t / duracao);
            spriteRenderer.color = c;
            yield return null;
        }
        Color cf = spriteRenderer.color; cf.a = para;
        spriteRenderer.color = cf;
    }

    IEnumerator ExplosaoEntrada(float duracao)
    {
        int sortLayer = spriteRenderer != null ? spriteRenderer.sortingLayerID : 0;
        int sortOrder = spriteRenderer != null ? spriteRenderer.sortingOrder   : 0;
        Vector3 origem = transform.position;

        // Textura de anel rosa/roxo
        int sz = 64;
        Texture2D texAnel = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        texAnel.filterMode = FilterMode.Bilinear;
        float cr = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d  = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cr, cr));
            float nt = d / cr;
            float a  = Mathf.Clamp01(1f - Mathf.Abs(nt - 0.72f) / 0.28f);
            texAnel.SetPixel(x, y, new Color(0.7f, 0.1f, 1f, a));
        }
        texAnel.Apply();
        Sprite sprAnel = Sprite.Create(texAnel, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz * 0.5f);

        GameObject anelGO = new GameObject("ShockwavePrincesa");
        anelGO.transform.position = origem;
        SpriteRenderer srAnel = anelGO.AddComponent<SpriteRenderer>();
        srAnel.sprite         = sprAnel;
        srAnel.sortingLayerID = sortLayer;
        srAnel.sortingOrder   = sortOrder - 1;

        // Textura de partícula 4×4
        Texture2D texP = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        for (int y = 0; y < 4; y++)
        for (int x = 0; x < 4; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(2f, 2f));
            texP.SetPixel(x, y, new Color(1f, 0.5f, 1f, Mathf.Clamp01(1f - d / 2f)));
        }
        texP.Apply();
        Sprite sprP = Sprite.Create(texP, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 16f);

        // Partículas
        int qtd = 20;
        var pGOs  = new GameObject[qtd];
        var pVels = new Vector2[qtd];
        Color[] cores = {
            new Color(1f, 0.5f, 1f), new Color(0.8f, 0.2f, 1f),
            new Color(0.5f, 0f, 0.9f), Color.white, new Color(1f, 0.8f, 1f)
        };

        for (int i = 0; i < qtd; i++)
        {
            float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float spd = Random.Range(0.5f, 1.8f);
            pVels[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd;

            GameObject p = new GameObject("PartPrincesa");
            p.transform.position   = origem + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.2f, 0.2f), 0f);
            float scl = Random.Range(0.1f, 0.25f);
            p.transform.localScale = new Vector3(scl, scl, 1f);
            SpriteRenderer srP = p.AddComponent<SpriteRenderer>();
            srP.sprite         = sprP;
            srP.color          = cores[Random.Range(0, cores.Length)];
            srP.sortingLayerID = sortLayer;
            srP.sortingOrder   = sortOrder + 1;
            pGOs[i] = p;
        }

        float t = 0f;
        while (t < duracao)
        {
            t += Time.deltaTime;
            float p01 = t / duracao;

            float sclAnel = Mathf.Lerp(0.05f, 3.5f, Mathf.Pow(p01, 0.5f));
            anelGO.transform.localScale = new Vector3(sclAnel, sclAnel * 0.3f, 1f);
            srAnel.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.85f, 0f, p01));

            for (int i = 0; i < qtd; i++)
            {
                if (pGOs[i] == null) continue;
                pGOs[i].transform.position += new Vector3(pVels[i].x, pVels[i].y + 0.25f, 0f) * Time.deltaTime;
                pVels[i] *= 0.97f;
                SpriteRenderer srP = pGOs[i].GetComponent<SpriteRenderer>();
                if (srP != null) { Color c = srP.color; c.a = Mathf.Lerp(0.9f, 0f, p01); srP.color = c; }
            }

            yield return null;
        }

        Destroy(anelGO);
        for (int i = 0; i < qtd; i++)
            if (pGOs[i] != null) Destroy(pGOs[i]);
    }

    // ──────────────────────────────────────────────
    // LOOP PRINCIPAL
    // ──────────────────────────────────────────────

    IEnumerator LoopComportamento()
    {
        while (!controller.estaMorrendo)
        {
            yield return StartCoroutine(FaseVoo());
            yield return StartCoroutine(FaseParada());
            yield return StartCoroutine(FasePreparo());
            yield return StartCoroutine(FaseCanalização());
        }
    }

    // ──────────────────────────────────────────────
    // FASES
    // ──────────────────────────────────────────────

    IEnumerator FaseVoo()
    {
        float duracaoVoo = Random.Range(tempoVooMin, tempoVooMax);
        float elapsed    = 0f;

        Vector2 destino = ObterDestino();

        if (animator != null) animator.SetBool("voando", true);

        while (elapsed < duracaoVoo && !controller.estaMorrendo)
        {
            elapsed += Time.deltaTime;

            if (rb != null)
            {
                Vector2 dir = ((Vector2)destino - rb.position);
                if (dir.sqrMagnitude < 0.5f)
                    destino = ObterDestino();

                rb.linearVelocity = Vector2.Lerp(
                    rb.linearVelocity,
                    dir.normalized * velocidadeVoo,
                    Time.deltaTime * 4f
                );

                // flip sprite
                if (spriteRenderer != null && Mathf.Abs(rb.linearVelocity.x) > 0.1f)
                    spriteRenderer.flipX = rb.linearVelocity.x < 0f;
            }

            yield return null;
        }
    }

    IEnumerator FaseParada()
    {
        if (animator != null) animator.SetBool("voando", false);

        float elapsed = 0f;
        while (elapsed < tempoParada && !controller.estaMorrendo)
        {
            elapsed += Time.deltaTime;
            if (rb != null)
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.deltaTime * 8f);
            yield return null;
        }
    }

    IEnumerator FasePreparo()
    {
        if (animator != null) animator.SetTrigger("prepararAtaque");
        yield return new WaitForSeconds(0.6f);
    }

    IEnumerator FaseCanalização()
    {
        if (animator != null) animator.SetTrigger("canalizar");

        int qtdProjeteis = sequenciaProjeteis[Mathf.Min(ataqueCicloIdx, sequenciaProjeteis.Length - 1)];
        ataqueCicloIdx   = Mathf.Min(ataqueCicloIdx + 1, sequenciaProjeteis.Length - 1);

        // Cria projéteis orbitando
        CriarOrbitaProjeteis(qtdProjeteis);

        // Onda de energia na Fase 2
        if (fase2Ativada)
            EmitirOndaEnergia();

        // Gira a órbita durante a canalização
        float elapsed = 0f;
        float anguloBase = 0f;
        while (elapsed < duracaoCanalização && !controller.estaMorrendo)
        {
            elapsed    += Time.deltaTime;
            anguloBase += Time.deltaTime * velocidadeOrbita;

            for (int i = 0; i < projeteisCanalizando.Count; i++)
            {
                if (projeteisCanalizando[i] == null) continue;
                float ang = anguloBase + (360f / projeteisCanalizando.Count) * i;
                float rad = ang * Mathf.Deg2Rad;
                projeteisCanalizando[i].transform.position =
                    transform.position + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad)) * raioOrbita;
            }

            yield return null;
        }

        // Captura centro antes de teleportar, depois lança
        Vector2 centroLancamento = transform.position;
        yield return StartCoroutine(Teleportar());
        LancarProjeteis(centroLancamento);
        LancarProjetilEspecial();
    }

    // ──────────────────────────────────────────────
    // PROJÉTEIS
    // ──────────────────────────────────────────────

    void CriarOrbitaProjeteis(int quantidade)
    {
        projeteisCanalizando.Clear();
        if (prefabProjetil == null) return;

        for (int i = 0; i < quantidade; i++)
        {
            float ang = (360f / quantidade) * i;
            float rad = ang * Mathf.Deg2Rad;
            Vector3 pos = transform.position + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad)) * raioOrbita;

            GameObject go = Instantiate(prefabProjetil, pos, Quaternion.identity);

            var rb2 = go.GetComponent<Rigidbody2D>();
            if (rb2 != null)
            {
                rb2.linearVelocity = Vector2.zero;
                rb2.simulated      = false; // remove da física; transform.position funciona livremente
            }

            foreach (var mb in go.GetComponents<MonoBehaviour>())
                mb.enabled = false; // desliga todos os scripts do projétil durante a órbita

            var homing = go.GetComponent<ProjetilHomingPrincesa>();
            if (homing == null) homing = go.AddComponent<ProjetilHomingPrincesa>();
            homing.velocidade       = velocidadeProjetil;
            homing.velocidadeMaxima = velocidadeProjetil + 4f;
            homing.dano             = danoProjetil;
            homing.duracaoVida      = duracaoProjetil;

            projeteisCanalizando.Add(go);
        }
    }

    void LancarProjeteis(Vector2 centro)
    {
        foreach (var go in projeteisCanalizando)
        {
            if (go == null) continue;

            Vector2 dir = ((Vector2)go.transform.position - centro).normalized;

            var rb2 = go.GetComponent<Rigidbody2D>();
            if (rb2 != null)
            {
                rb2.simulated      = true;
                rb2.linearVelocity = dir * velocidadeProjetil;
            }

            var homing = go.GetComponent<ProjetilHomingPrincesa>();
            if (homing != null)
            {
                homing.enabled = true;
                homing.Iniciar(null);
            }

            go.transform.SetParent(null);
        }

        projeteisCanalizando.Clear();
    }

    void LancarProjetilEspecial()
    {
        if (prefabProjetil == null || player == null) return;

        var tipo = (ProjetilEspecialPrincesa.Tipo)(indiceEspecial % 3);
        indiceEspecial++;

        GameObject go = Instantiate(prefabProjetil, transform.position, Quaternion.identity);

        // Desativa scripts padrão
        foreach (var mb in go.GetComponents<MonoBehaviour>())
            mb.enabled = false;

        // Garante que o collider seja trigger
        var col = go.GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        var especial = go.AddComponent<ProjetilEspecialPrincesa>();
        especial.tipo = tipo;
        especial.dano = danoProjetil;

        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = CorParaTipo(tipo);
            sr.enabled = true;
        }

        go.transform.localScale *= 1.6f;

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        var rb2 = go.GetComponent<Rigidbody2D>();
        if (rb2 != null)
        {
            rb2.simulated      = true;
            rb2.linearVelocity = dir * velocidadeProjetil;
        }

        Destroy(go, duracaoProjetil);
    }

    Color CorParaTipo(ProjetilEspecialPrincesa.Tipo tipo)
    {
        switch (tipo)
        {
            case ProjetilEspecialPrincesa.Tipo.Raiz:     return new Color(0.3f, 1f, 0.3f);
            case ProjetilEspecialPrincesa.Tipo.Queima:   return new Color(1f, 0.4f, 0.1f);
            case ProjetilEspecialPrincesa.Tipo.Empurrao: return new Color(0.2f, 0.85f, 1f);
            default:                                      return Color.white;
        }
    }

    IEnumerator Teleportar()
    {
        if (rb != null) rb.linearVelocity = Vector2.zero;

        yield return StartCoroutine(EfeitoTeleportSaida());

        Vector2 novaPos = ObterDestino();
        if (rb != null) rb.position = novaPos;
        else transform.position     = novaPos;

        yield return null; // garante que rb.position foi aplicado

        yield return StartCoroutine(EfeitoTeleportEntrada());
    }

    IEnumerator EfeitoTeleportSaida()
    {
        // Flashes roxo
        for (int i = 0; i < 4; i++)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = i % 2 == 0 ? new Color(0.7f, 0.1f, 1f) : Color.white;
            yield return new WaitForSeconds(0.055f);
        }

        // Anel saindo do ponto de partida
        StartCoroutine(AnimarAnel((Vector2)transform.position, new Color(0.8f, 0.2f, 1f, 0.9f), 0.5f));

        // Encolhe e desaparece
        Vector3 escalaBase = transform.localScale;
        float t = 0f, dur = 0.18f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            float s = 1f - p;
            transform.localScale = escalaBase * s;
            if (spriteRenderer != null)
                spriteRenderer.color = new Color(0.7f, 0.1f, 1f, 1f - p);
            yield return null;
        }

        transform.localScale = escalaBase;
        if (spriteRenderer != null)
        {
            spriteRenderer.color   = Color.white;
            spriteRenderer.enabled = false;
        }
    }

    IEnumerator EfeitoTeleportEntrada()
    {
        if (spriteRenderer != null) spriteRenderer.enabled = true;

        // Anel de chegada
        StartCoroutine(AnimarAnel((Vector2)transform.position, new Color(1f, 0.4f, 1f, 1f), 0.7f));

        // Pop: começa em zero, ultrapassa e volta ao normal
        Vector3 escalaBase = transform.localScale;
        transform.localScale = Vector3.zero;
        if (spriteRenderer != null) spriteRenderer.color = new Color(0.9f, 0.5f, 1f);

        float t = 0f, dur = 0.28f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            // overshoot suave
            float s = p < 0.55f
                ? Mathf.Lerp(0f, 1.25f, p / 0.55f)
                : Mathf.Lerp(1.25f, 1f, (p - 0.55f) / 0.45f);
            transform.localScale = escalaBase * s;
            if (spriteRenderer != null)
                spriteRenderer.color = Color.Lerp(new Color(0.9f, 0.5f, 1f), Color.white, p);
            yield return null;
        }

        transform.localScale = escalaBase;
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
    }

    IEnumerator AnimarAnel(Vector2 centro, Color cor, float duracao)
    {
        const int SEGS = 28;
        var go = new GameObject("TeleportRing");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace  = true;
        lr.loop           = true;
        lr.positionCount  = SEGS;
        lr.material       = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder   = 6;

        float t = 0f;
        while (t < duracao)
        {
            t += Time.deltaTime;
            float p    = t / duracao;
            float raio = Mathf.Lerp(0.05f, 4f, p);
            float a    = Mathf.Lerp(cor.a, 0f, p * p);

            for (int i = 0; i < SEGS; i++)
            {
                float ang = (360f / SEGS) * i * Mathf.Deg2Rad;
                lr.SetPosition(i, new Vector3(centro.x + Mathf.Cos(ang) * raio,
                                              centro.y + Mathf.Sin(ang) * raio, 0f));
            }
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.18f, 0.02f, p);
            lr.startColor = lr.endColor = new Color(cor.r, cor.g, cor.b, a);
            yield return null;
        }

        Destroy(go);
    }

    // ──────────────────────────────────────────────
    // FASE 2 — ONDA DE ENERGIA
    // ──────────────────────────────────────────────

    void EmitirOndaEnergia()
    {
        if (player == null || playerStats == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > raioOnda) return;

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        var rbPlayer = player.GetComponent<Rigidbody2D>();
        if (rbPlayer != null)
            rbPlayer.AddForce(dir * forcaOndaEmpurrao, ForceMode2D.Impulse);

        StartCoroutine(EfeitoOndaVisual());
    }

    IEnumerator EfeitoOndaVisual()
    {
        int segmentos = 32;
        GameObject go = new GameObject("OndaEnergia");
        go.transform.position = transform.position;

        LineRenderer lr  = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop          = true;
        lr.positionCount = segmentos;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 5;

        for (int i = 0; i < segmentos; i++)
        {
            float a = (360f / segmentos) * i * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a)));
        }

        float t = 0f, dur = 0.5f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            float r = Mathf.Lerp(0.1f, raioOnda, p);
            float a = Mathf.Lerp(0.8f, 0f, p);
            go.transform.localScale = Vector3.one * r;
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.18f, 0.02f, p);
            lr.startColor = lr.endColor = new Color(1f, 0.5f, 1f, a);
            yield return null;
        }

        Destroy(go);
    }

    // ──────────────────────────────────────────────
    // UTILIDADES
    // ──────────────────────────────────────────────

    Vector2 ObterDestino()
    {
        // Ponto aleatório ao redor do player (ou do centro se sem player)
        Vector2 centro = player != null ? (Vector2)player.position : Vector2.zero;
        float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float dist = Random.Range(raioPadraoVoo * 0.4f, raioPadraoVoo);
        return centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;
    }

    // ──────────────────────────────────────────────
    // UI
    // ──────────────────────────────────────────────

    void CriarBossUI()
    {
        bossCanvasGO = new GameObject("BossPrincesaCanvas");
        var canvas = bossCanvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        bossCanvasGO.AddComponent<CanvasScaler>();
        bossCanvasGO.AddComponent<GraphicRaycaster>();

        // Painel central no topo
        var painelGO = new GameObject("PainelHP");
        painelGO.transform.SetParent(canvas.transform, false);

        var rt = painelGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.2f, 0.92f);
        rt.anchorMax = new Vector2(0.8f, 0.99f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // Fundo
        var bg = new GameObject("BG");
        bg.transform.SetParent(painelGO.transform, false);
        var bgRT = bg.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.06f, 0.02f, 0.12f, 0.9f);

        // Barra de HP
        var barraGO = new GameObject("HP_Fill");
        barraGO.transform.SetParent(painelGO.transform, false);
        var barraRT = barraGO.AddComponent<RectTransform>();
        barraRT.anchorMin = new Vector2(0f, 0f);
        barraRT.anchorMax = new Vector2(1f, 1f);
        barraRT.offsetMin = new Vector2(4f, 4f);
        barraRT.offsetMax = new Vector2(-4f, -4f);

        hpFill = barraGO.AddComponent<Image>();
        hpFill.color = new Color(0.9f, 0.3f, 0.85f);
        hpFill.type  = Image.Type.Filled;
        hpFill.fillMethod  = Image.FillMethod.Horizontal;
        hpFill.fillOrigin  = 0;
        hpFill.fillAmount  = 1f;

        // Texto do nome
        var nomeGO = new GameObject("Nome");
        nomeGO.transform.SetParent(painelGO.transform, false);
        var nomeRT = nomeGO.AddComponent<RectTransform>();
        nomeRT.anchorMin = Vector2.zero; nomeRT.anchorMax = Vector2.one;
        nomeRT.offsetMin = nomeRT.offsetMax = Vector2.zero;

        var nomeTxt = nomeGO.AddComponent<TextMeshProUGUI>();
        nomeTxt.text      = nomeBoss;
        nomeTxt.fontSize  = 14f;
        nomeTxt.fontStyle = FontStyles.Bold;
        nomeTxt.alignment = TextAlignmentOptions.Center;
        nomeTxt.color     = new Color(1f, 0.8f, 1f);

        // Texto de fase
        var faseGO = new GameObject("Fase");
        faseGO.transform.SetParent(painelGO.transform, false);
        var faseRT = faseGO.AddComponent<RectTransform>();
        faseRT.anchorMin = new Vector2(0.75f, 0f);
        faseRT.anchorMax = new Vector2(1f, 1f);
        faseRT.offsetMin = faseRT.offsetMax = Vector2.zero;

        faseText = faseGO.AddComponent<TextMeshProUGUI>();
        faseText.text      = "FASE 1";
        faseText.fontSize  = 11f;
        faseText.alignment = TextAlignmentOptions.Right;
        faseText.color     = new Color(0.8f, 0.6f, 1f);
    }

    void AtualizarUI()
    {
        if (hpFill == null || controller == null) return;
        float pct = controller.vidaAtual / controller.vidaMaxima;
        hpFill.fillAmount = Mathf.Lerp(hpFill.fillAmount, pct, Time.deltaTime * 5f);
    }

    // ──────────────────────────────────────────────
    // MORTE
    // ──────────────────────────────────────────────

    void OnDestroy()
    {
        if (bossCanvasGO != null) Destroy(bossCanvasGO);

        // Destrói projéteis em órbita se ainda existirem
        foreach (var go in projeteisCanalizando)
            if (go != null) Destroy(go);
    }
}
