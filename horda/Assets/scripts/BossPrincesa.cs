using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(InimigoController))]
public class BossPrincesa : MonoBehaviour, IBoss
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
    public float escalaProjetilOrbita = 2f;

    [Header("Sequência de Projéteis")]
    [Tooltip("Quantidade de projéteis por canalização: 4 → 8 → 16 (e se repete em 16)")]
    public int[] sequenciaProjeteis = { 4, 8, 16 };

    [Header("Projéteis Especiais")]
    [Tooltip("Prefab do projétil de Queima — causa queima por 3s. Usa prefabProjetil se vazio.")]
    public GameObject prefabEspecialQueima;

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
    private BoxCollider2D        hitbox;

    private bool      fase2Ativada    = false;
    private int       ataqueCicloIdx  = 0;
    private float     velocidadeOrbita = 120f;
    private Coroutine loopCoroutine;

    // dash
    private bool estaDashando = false;

    private readonly List<InimigoController> inimigosBufados = new List<InimigoController>();

    // orbita durante canalização
    private readonly List<GameObject> projeteisCanalizando = new List<GameObject>();

    // UI
    private GameObject      bossCanvasGO;
    private Image           hpFill;
    private Image           hpFillGhost;
    private Image           _bordaImg;
    private TextMeshProUGUI faseText;
    private TextMeshProUGUI hpText;

    // ──────────────────────────────────────────────
    // LIFECYCLE
    // ──────────────────────────────────────────────

    void Start()
    {
        controller     = GetComponent<InimigoController>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator       = GetComponent<Animator>();
        rb             = GetComponent<Rigidbody2D>();
        hitbox         = GetComponent<BoxCollider2D>();

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
            rb.mass           = 10000f; // impede o player de empurrar
            rb.constraints    = RigidbodyConstraints2D.FreezeAll;
        }

        CriarBossUI();
        StartCoroutine(SequenciaEntrada());
    }

    void Update()
    {
        if (controller == null || controller.estaMorrendo) return;

        AtualizarUI();
        VerificarFase2();
        SincronizarHitbox();
    }

    void SincronizarHitbox()
    {
        if (hitbox == null || spriteRenderer == null) return;

        // Usa o bounds do renderer (espaço mundo) — já considera pivot, flipX e escala
        Bounds   wb  = spriteRenderer.bounds;
        Vector3  scl = transform.lossyScale;

        // Converte centro mundial para espaço local do collider
        hitbox.offset = transform.InverseTransformPoint(wb.center);

        // Converte tamanho mundial para espaço local e reduz 15% para não vazar
        hitbox.size = new Vector2(
            wb.size.x / Mathf.Abs(scl.x),
            wb.size.y / Mathf.Abs(scl.y)
        ) * 0.85f;
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
        inimigosBufados.Clear();
        foreach (var inimigo in FindObjectsByType<InimigoController>(FindObjectsSortMode.None))
        {
            if (inimigo == controller) continue;
            var movi = inimigo.GetComponent<movi_inimigo>();
            if (movi != null)
            {
                movi.velocidade *= buffVelocidadeInimigos;
                inimigosBufados.Add(inimigo);
            }
        }

        StartCoroutine(FlashFase2());

        if (faseText != null)
            faseText.text = Loc.T("boss.phase2");

        // Interrompe o loop normal, faz a explosão de entrada e reinicia
        if (loopCoroutine != null) StopCoroutine(loopCoroutine);
        loopCoroutine = StartCoroutine(EntradaFase2());
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

        loopCoroutine = StartCoroutine(LoopComportamento());
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
        txt.text      = Loc.T("boss.appeared") + "\n<size=60%>" + nomeBoss.ToUpper() + "</size>";
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
        int ciclo = 0;
        while (!controller.estaMorrendo)
        {
            yield return StartCoroutine(FaseVoo());
            yield return StartCoroutine(FaseParada());
            yield return StartCoroutine(FasePreparo());
            yield return StartCoroutine(FaseCanalização());

            if (fase2Ativada)
            {
                if (ciclo % 3 == 2)
                    yield return StartCoroutine(ExplosaoBulletHell());
                else if (ciclo % 2 == 1)
                    yield return StartCoroutine(FaseDash());
            }

            ciclo++;
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
        if (rb != null) rb.constraints = RigidbodyConstraints2D.FreezeRotation;

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
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.constraints    = RigidbodyConstraints2D.FreezeAll;
        }

        yield return new WaitForSeconds(tempoParada);
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

        // Scale-in + órbita simultâneos (0.3s surgimento)
        float surgirT = 0f;
        float anguloBase = 0f;
        int total = projeteisCanalizando.Count;
        while (surgirT < 0.3f)
        {
            surgirT    += Time.deltaTime;
            anguloBase += Time.deltaTime * velocidadeOrbita;
            float s     = Mathf.Clamp01(surgirT / 0.3f);

            for (int i = 0; i < total; i++)
            {
                var go = projeteisCanalizando[i];
                if (go == null) continue;
                float ang = anguloBase + (360f / total) * i;
                float rad = ang * Mathf.Deg2Rad;
                go.transform.position   = transform.position + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad)) * raioOrbita;
                go.transform.rotation   = Quaternion.Euler(0f, 0f, ang + 90f);
                go.transform.localScale = Vector3.one * (escalaProjetilOrbita * s);
            }
            yield return null;
        }

        foreach (var go in projeteisCanalizando)
            if (go != null) go.transform.localScale = Vector3.one * escalaProjetilOrbita;

        // Gira a órbita durante a canalização
        float elapsed = 0f;
        while (elapsed < duracaoCanalização && !controller.estaMorrendo)
        {
            elapsed    += Time.deltaTime;
            anguloBase += Time.deltaTime * velocidadeOrbita;

            for (int i = 0; i < total; i++)
            {
                var go = projeteisCanalizando[i];
                if (go == null) continue;

                float ang = anguloBase + (360f / total) * i;
                float rad = ang * Mathf.Deg2Rad;

                go.transform.position = transform.position + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad)) * raioOrbita;
                go.transform.rotation = Quaternion.Euler(0f, 0f, ang + 90f);
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
            go.transform.localScale = Vector3.zero; // começa invisível para o scale-in

            var rb2 = go.GetComponent<Rigidbody2D>();
            if (rb2 != null)
            {
                rb2.linearVelocity = Vector2.zero;
                rb2.simulated      = false;
            }

            // Remove da detecção de inimigos durante a órbita
            foreach (var col in go.GetComponentsInChildren<Collider2D>())
                col.enabled = false;
            go.tag = "Untagged";

            // Desliga TODOS os scripts — inclui qualquer script de movimento/homing
            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>())
                mb.enabled = false;

            // Pré-configura o homing para quando for lançado (mantém DESABILITADO agora)
            var homing = go.GetComponent<ProjetilHomingPrincesa>();
            if (homing == null) homing = go.AddComponent<ProjetilHomingPrincesa>();
            homing.enabled          = false; // garantir que não roda durante a órbita
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

            // Reativa colliders agora que o projétil está sendo lançado
            foreach (var col in go.GetComponentsInChildren<Collider2D>())
                col.enabled = true;

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
        if (player == null) return;

        GameObject prefab = prefabEspecialQueima != null ? prefabEspecialQueima : prefabProjetil;
        if (prefab == null) return;

        // Leque de 5 projéteis: centro + dois pares de ±15° e ±30°
        float[] offsets = { -30f, -15f, 0f, 15f, 30f };
        Vector2 dirBase = ((Vector2)player.position - (Vector2)transform.position).normalized;

        foreach (float offset in offsets)
        {
            Vector2 dir = Quaternion.Euler(0f, 0f, offset) * dirBase;

            GameObject go = Instantiate(prefab, transform.position, Quaternion.identity);

            foreach (var mb in go.GetComponents<MonoBehaviour>())
                mb.enabled = false;

            var col = go.GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;

            var especial = go.AddComponent<ProjetilEspecialPrincesa>();
            especial.tipo = ProjetilEspecialPrincesa.Tipo.Queima;
            especial.dano = danoProjetil;

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color   = new Color(1f, 0.4f, 0.1f);
                sr.enabled = true;
            }

            go.transform.localScale *= 1.6f;

            var rb2 = go.GetComponent<Rigidbody2D>();
            if (rb2 != null)
            {
                rb2.simulated      = true;
                rb2.linearVelocity = dir * velocidadeProjetil;
            }

            Destroy(go, duracaoProjetil);
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
        Vector2 centro = player != null ? (Vector2)player.position : Vector2.zero;
        float raioBody = Mathf.Abs(transform.localScale.x) * 0.22f + 0.15f;
        int mask = LayerMask.GetMask("obstacles");

        for (int tentativa = 0; tentativa < 30; tentativa++)
        {
            float ang  = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = Random.Range(raioPadraoVoo * 0.4f, raioPadraoVoo);
            Vector2 alvo = centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;

            if (!Physics2D.OverlapCircle(alvo, raioBody, mask))
                return alvo;
        }

        // Fallback: posição atual se nenhum ponto livre for encontrado
        return transform.position;
    }

    // ──────────────────────────────────────────────
    // UI
    // ──────────────────────────────────────────────

    void CriarBossUI()
    {
        bossCanvasGO = new GameObject("BossPrincesaCanvas");
        var canvas = bossCanvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = bossCanvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        bossCanvasGO.AddComponent<GraphicRaycaster>();

        // Painel central no topo — 70% de largura, altura dobrada
        var painelGO = new GameObject("PainelHP");
        painelGO.transform.SetParent(canvas.transform, false);
        var rt = painelGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.28f, 0.845f);
        rt.anchorMax = new Vector2(0.72f, 0.978f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // Borda brilhante externa
        var bordaGO = new GameObject("Borda");
        bordaGO.transform.SetParent(painelGO.transform, false);
        var bordaRT = bordaGO.AddComponent<RectTransform>();
        bordaRT.anchorMin = Vector2.zero; bordaRT.anchorMax = Vector2.one;
        bordaRT.offsetMin = new Vector2(-2f, -2f);
        bordaRT.offsetMax = new Vector2(2f, 2f);
        _bordaImg = bordaGO.AddComponent<Image>();
        _bordaImg.color = new Color(0.85f, 0.2f, 0.9f, 0.7f);

        // Fundo escuro
        var bg = new GameObject("BG");
        bg.transform.SetParent(painelGO.transform, false);
        var bgRT = bg.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.05f, 0.01f, 0.10f, 0.96f);

        // Nome do boss — metade superior esquerda
        var nomeGO = new GameObject("Nome");
        nomeGO.transform.SetParent(painelGO.transform, false);
        var nomeRT = nomeGO.AddComponent<RectTransform>();
        nomeRT.anchorMin = new Vector2(0f, 0.52f);
        nomeRT.anchorMax = new Vector2(0.72f, 1f);
        nomeRT.offsetMin = new Vector2(10f, 0f);
        nomeRT.offsetMax = new Vector2(0f, -3f);
        var nomeTxt = nomeGO.AddComponent<TextMeshProUGUI>();
        nomeTxt.text      = nomeBoss;
        nomeTxt.fontSize  = 17f;
        nomeTxt.fontStyle = FontStyles.Bold;
        nomeTxt.alignment = TextAlignmentOptions.BottomLeft;
        nomeTxt.color     = new Color(1f, 0.82f, 1f);

        // Texto de fase — metade superior direita
        var faseGO = new GameObject("Fase");
        faseGO.transform.SetParent(painelGO.transform, false);
        var faseRT = faseGO.AddComponent<RectTransform>();
        faseRT.anchorMin = new Vector2(0.72f, 0.52f);
        faseRT.anchorMax = new Vector2(1f, 1f);
        faseRT.offsetMin = new Vector2(0f, 0f);
        faseRT.offsetMax = new Vector2(-10f, -3f);
        faseText = faseGO.AddComponent<TextMeshProUGUI>();
        faseText.text      = Loc.T("boss.phase1");
        faseText.fontSize  = 13f;
        faseText.alignment = TextAlignmentOptions.BottomRight;
        faseText.color     = new Color(0.75f, 0.55f, 1f);

        // Linha separadora entre header e barra
        var sepGO = new GameObject("Separador");
        sepGO.transform.SetParent(painelGO.transform, false);
        var sepRT = sepGO.AddComponent<RectTransform>();
        sepRT.anchorMin = new Vector2(0f, 0.49f);
        sepRT.anchorMax = new Vector2(1f, 0.51f);
        sepRT.offsetMin = new Vector2(8f, 0f);
        sepRT.offsetMax = new Vector2(-8f, 0f);
        sepGO.AddComponent<Image>().color = new Color(0.7f, 0.2f, 0.85f, 0.45f);

        // Barra fantasma (amarela, desce devagar)
        var ghostGO = new GameObject("HP_Ghost");
        ghostGO.transform.SetParent(painelGO.transform, false);
        var ghostRT = ghostGO.AddComponent<RectTransform>();
        ghostRT.anchorMin = new Vector2(0f, 0.07f);
        ghostRT.anchorMax = new Vector2(1f, 0.46f);
        ghostRT.offsetMin = new Vector2(8f, 0f);
        ghostRT.offsetMax = new Vector2(-8f, 0f);
        hpFillGhost = ghostGO.AddComponent<Image>();
        hpFillGhost.color      = new Color(1f, 0.85f, 0.2f, 0.88f);
        hpFillGhost.type       = Image.Type.Filled;
        hpFillGhost.fillMethod = Image.FillMethod.Vertical;
        hpFillGhost.fillOrigin = 0;
        hpFillGhost.fillAmount = 1f;

        // Barra de HP principal (rosa → vermelho na fase 2)
        var barraGO = new GameObject("HP_Fill");
        barraGO.transform.SetParent(painelGO.transform, false);
        var barraRT = barraGO.AddComponent<RectTransform>();
        barraRT.anchorMin = new Vector2(0f, 0.07f);
        barraRT.anchorMax = new Vector2(1f, 0.46f);
        barraRT.offsetMin = new Vector2(8f, 0f);
        barraRT.offsetMax = new Vector2(-8f, 0f);
        hpFill = barraGO.AddComponent<Image>();
        hpFill.color      = new Color(0.92f, 0.28f, 0.86f);
        hpFill.type       = Image.Type.Filled;
        hpFill.fillMethod = Image.FillMethod.Vertical;
        hpFill.fillOrigin = 0;
        hpFill.fillAmount = 1f;

        // Faixa de brilho lateral esquerda (efeito de vidro em barra vertical)
        var shineGO = new GameObject("HP_Shine");
        shineGO.transform.SetParent(painelGO.transform, false);
        var shineRT = shineGO.AddComponent<RectTransform>();
        shineRT.anchorMin = new Vector2(0f, 0.07f);
        shineRT.anchorMax = new Vector2(0.06f, 0.46f);
        shineRT.offsetMin = new Vector2(8f, 0f);
        shineRT.offsetMax = new Vector2(0f, 0f);
        shineGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);

        // Percentual de HP (texto centralizado sobre a barra)
        var hpNumGO = new GameObject("HPText");
        hpNumGO.transform.SetParent(painelGO.transform, false);
        var hpNumRT = hpNumGO.AddComponent<RectTransform>();
        hpNumRT.anchorMin = new Vector2(0f, 0.07f);
        hpNumRT.anchorMax = new Vector2(1f, 0.46f);
        hpNumRT.offsetMin = new Vector2(8f, 0f);
        hpNumRT.offsetMax = new Vector2(-8f, 0f);
        hpText = hpNumGO.AddComponent<TextMeshProUGUI>();
        hpText.text      = "100%";
        hpText.fontSize  = 10f;
        hpText.fontStyle = FontStyles.Bold;
        hpText.alignment = TextAlignmentOptions.Center;
        hpText.color     = new Color(1f, 1f, 1f, 0.80f);
    }

    void AtualizarUI()
    {
        if (hpFill == null || controller == null) return;

        float pct = controller.vidaAtual / controller.vidaMaxima;

        hpFill.fillAmount = Mathf.Lerp(hpFill.fillAmount, pct, Time.deltaTime * 8f);

        if (hpFillGhost != null)
            hpFillGhost.fillAmount = Mathf.MoveTowards(hpFillGhost.fillAmount, pct, Time.deltaTime * 0.35f);

        hpFill.color = fase2Ativada
            ? new Color(1f, 0.18f, 0.12f)
            : new Color(0.92f, 0.28f, 0.86f);

        if (_bordaImg != null)
            _bordaImg.color = fase2Ativada
                ? new Color(1f, 0.15f, 0.08f, 0.7f + 0.2f * Mathf.Sin(Time.time * 4f))
                : new Color(0.85f, 0.2f, 0.9f, 0.7f);

        if (hpText != null)
            hpText.text = Mathf.CeilToInt(pct * 100f) + "%";
    }

    // ──────────────────────────────────────────────
    // FASE 2 — BULLET HELL / CLONE / POÇA
    // ──────────────────────────────────────────────

    IEnumerator ExplosaoBulletHell()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.constraints    = RigidbodyConstraints2D.FreezeAll;
        }

        // Flash vermelho intenso
        for (int i = 0; i < 8; i++)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = i % 2 == 0 ? new Color(1f, 0.08f, 0.08f) : Color.white;
            yield return new WaitForSeconds(0.075f);
        }
        if (spriteRenderer != null) spriteRenderer.color = Color.white;

        // Onda 1 — 16 projéteis retos
        LancarOnda(16, 0f);

        yield return new WaitForSeconds(0.45f);

        // Onda 2 — 16 projéteis defasados 11.25°
        LancarOnda(16, 360f / 32f);

        yield return new WaitForSeconds(0.4f);
    }

    IEnumerator EntradaFase2()
    {
        yield return StartCoroutine(ExplosaoBulletHell());
        loopCoroutine = StartCoroutine(LoopComportamento());
    }

    void LancarOnda(int quantidade, float offsetAngulo)
    {
        if (prefabProjetil == null) return;

        float passo = 360f / quantidade;
        for (int i = 0; i < quantidade; i++)
        {
            float ang = (passo * i + offsetAngulo) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));

            var go = Instantiate(prefabProjetil, transform.position, Quaternion.identity);

            foreach (var mb in go.GetComponents<MonoBehaviour>())
                mb.enabled = false;

            var homing = go.GetComponent<ProjetilHomingPrincesa>();
            if (homing != null)
            {
                homing.enabled     = true;
                homing.dano        = danoProjetil;
                homing.duracaoVida = duracaoProjetil;
                homing.Iniciar(null); // sem homing, mas ativo para causar dano
            }

            var rb2 = go.GetComponent<Rigidbody2D>();
            if (rb2 != null)
            {
                rb2.simulated      = true;
                rb2.linearVelocity = dir * velocidadeProjetil;
            }

            Destroy(go, duracaoProjetil);
        }
    }

    // ──────────────────────────────────────────────
    // DASH DE SLIME
    // ──────────────────────────────────────────────

    IEnumerator FaseDash()
    {
        if (player == null) yield break;

        // Para antes do dash
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.constraints = RigidbodyConstraints2D.FreezeAll; }

        Vector2 alvo = (Vector2)player.position;
        Vector2 dir  = (alvo - (Vector2)transform.position).normalized;

        if (spriteRenderer != null) spriteRenderer.flipX = dir.x < 0f;

        // Wind-up: pisca magenta
        for (int i = 0; i < 4; i++)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = i % 2 == 0 ? new Color(1f, 0.2f, 0.85f) : Color.white;
            yield return new WaitForSeconds(0.08f);
        }
        if (spriteRenderer != null) spriteRenderer.color = Color.white;

        // Dash
        estaDashando = true;
        if (rb != null) rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        float dashSpeed = velocidadeVoo * 4.5f;
        float dashDur   = 0.4f;
        float elapsed   = 0f;

        StartCoroutine(RastroTrail(dashDur));

        while (elapsed < dashDur && !controller.estaMorrendo)
        {
            elapsed += Time.deltaTime;
            if (rb != null)
                rb.linearVelocity = dir * Mathf.Lerp(dashSpeed, dashSpeed * 0.2f, elapsed / dashDur);
            yield return null;
        }

        estaDashando = false;
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.constraints = RigidbodyConstraints2D.FreezeAll; }

        yield return new WaitForSeconds(0.35f);
    }

    IEnumerator RastroTrail(float duracao)
    {
        float elapsed = 0f;
        while (elapsed < duracao && estaDashando)
        {
            elapsed += Time.deltaTime;
            CriarParticulaRastro();
            yield return new WaitForSeconds(0.045f);
        }
    }

    void CriarParticulaRastro()
    {
        var go  = new GameObject("RastroDash");
        go.transform.position   = transform.position;
        go.transform.localScale = transform.localScale * 0.75f;

        var sr2 = go.AddComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            sr2.sprite         = spriteRenderer.sprite;
            sr2.flipX          = spriteRenderer.flipX;
            sr2.sortingLayerID = spriteRenderer.sortingLayerID;
            sr2.sortingOrder   = spriteRenderer.sortingOrder - 1;
        }
        sr2.color = new Color(0.65f, 0.1f, 1f, 0.55f);

        StartCoroutine(FadeParticula(go, sr2, 0.22f));
    }

    IEnumerator FadeParticula(GameObject go, SpriteRenderer sr2, float dur)
    {
        float t = 0f;
        Color c = sr2 != null ? sr2.color : Color.clear;
        while (t < dur)
        {
            t += Time.deltaTime;
            if (sr2 != null) { Color nc = c; nc.a = Mathf.Lerp(c.a, 0f, t / dur); sr2.color = nc; }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (!estaDashando || !col.gameObject.CompareTag("Player")) return;

        var rb2  = col.gameObject.GetComponent<Rigidbody2D>();
        var movi = col.gameObject.GetComponent<moviment_player2>();
        if (rb2 == null) return;

        Vector2 empurrao = ((Vector2)col.transform.position - (Vector2)transform.position).normalized;
        if (movi != null) movi.enabled = false;
        rb2.linearVelocity = empurrao * 16f;
        StartCoroutine(RestaurarMovimentoPlayer(col.gameObject, movi));
    }

    IEnumerator RestaurarMovimentoPlayer(GameObject playerGO, moviment_player2 movi)
    {
        yield return new WaitForSeconds(0.3f);
        if (movi != null) movi.enabled = true;
    }

    // ──────────────────────────────────────────────
    // MORTE
    // ──────────────────────────────────────────────

    public void IniciarEfeitoMorte()
    {
        RemoverBuffInimigos();
        StartCoroutine(EfeitoMortePrincesa());
    }

    void RemoverBuffInimigos()
    {
        foreach (var inimigo in inimigosBufados)
        {
            if (inimigo == null) continue;
            var movi = inimigo.GetComponent<movi_inimigo>();
            if (movi != null)
                movi.velocidade /= buffVelocidadeInimigos;
        }
        inimigosBufados.Clear();
    }

    IEnumerator EfeitoMortePrincesa()
    {
        if (loopCoroutine != null) { StopCoroutine(loopCoroutine); loopCoroutine = null; }
        estaDashando = false;

        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.constraints = RigidbodyConstraints2D.FreezeAll; }

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (bossCanvasGO != null) { Destroy(bossCanvasGO); bossCanvasGO = null; }

        CameraShaker.Tremer(0.12f, 3.5f);

        int sortL = spriteRenderer != null ? spriteRenderer.sortingLayerID : 0;
        int sortO = spriteRenderer != null ? spriteRenderer.sortingOrder   : 0;

        // Flash rosa/branco rápido
        for (int i = 0; i < 12; i++)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = i % 2 == 0 ? Color.white : new Color(1f, 0.35f, 0.9f);
            yield return new WaitForSeconds(0.045f);
        }

        CriarAneisMortePrincesa(5, sortL, sortO);
        CriarParticulasMortePrincesa(40, sortL, sortO);

        // Cresce e dissolve
        Vector3 escBase = transform.localScale;
        float t = 0f, dur = 1.2f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            transform.localScale = escBase * Mathf.Lerp(1f, 3.5f, Mathf.Pow(p, 0.5f));
            if (spriteRenderer != null)
                spriteRenderer.color = new Color(1f, 0.6f, 1f, Mathf.Lerp(1f, 0f, p * p));
            yield return null;
        }

        BossMorteUI.Exibir("BOSS DERROTADO!", new Color(1f, 0.45f, 1f));
        Destroy(gameObject);
    }

    void CriarAneisMortePrincesa(int qtd, int sortL, int sortO)
    {
        const int SZ = 32;
        float cx  = SZ * 0.5f;
        var   tex = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < SZ; y++)
        for (int x = 0; x < SZ; x++)
        {
            float d  = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cx));
            float nt = d / cx;
            float a  = Mathf.Clamp01(1f - Mathf.Abs(nt - 0.75f) / 0.25f);
            tex.SetPixel(x, y, new Color(1f, 0.4f, 0.9f, a));
        }
        tex.Apply();
        Sprite spr = Sprite.Create(tex, new Rect(0, 0, SZ, SZ), new Vector2(0.5f, 0.5f), SZ * 0.5f);

        for (int i = 0; i < qtd; i++)
        {
            var ring = new GameObject("AnelMortePrincesa");
            ring.transform.position = transform.position;
            var sr = ring.AddComponent<SpriteRenderer>();
            sr.sprite         = spr;
            sr.sortingLayerID = sortL;
            sr.sortingOrder   = sortO - 1;

            var anim          = ring.AddComponent<AnelExpansaoAuto>();
            anim.delay        = i * 0.14f;
            anim.duracaoTotal = Random.Range(0.7f, 1.2f);
            anim.escalaFinal  = Random.Range(6f, 15f);
        }
    }

    void CriarParticulasMortePrincesa(int qtd, int sortL, int sortO)
    {
        const int SZ = 6;
        float cx  = SZ * 0.5f;
        var   tex = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false);
        for (int y = 0; y < SZ; y++)
        for (int x = 0; x < SZ; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cx));
            tex.SetPixel(x, y, new Color(1f, 0.5f, 1f, Mathf.Clamp01(1f - d / cx)));
        }
        tex.Apply();
        Sprite spr = Sprite.Create(tex, new Rect(0, 0, SZ, SZ), new Vector2(0.5f, 0.5f), 16f);

        Color[] cores = {
            new Color(1f, 0.5f, 1f), new Color(1f, 0.85f, 1f),
            new Color(0.75f, 0.15f, 1f), Color.white, new Color(1f, 0.3f, 0.65f)
        };

        for (int i = 0; i < qtd; i++)
        {
            float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float spd = Random.Range(0.8f, 3.5f);
            Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd;

            var go = new GameObject("PartMortePrincesa");
            go.transform.position   = transform.position + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.2f, 0.2f));
            float scl = Random.Range(0.08f, 0.3f);
            go.transform.localScale = new Vector3(scl, scl, 1f);

            var sr2            = go.AddComponent<SpriteRenderer>();
            sr2.sprite         = spr;
            sr2.color          = cores[Random.Range(0, cores.Length)];
            sr2.sortingLayerID = sortL;
            sr2.sortingOrder   = sortO + 1;

            EfeitoRunner.Criar().StartCoroutine(AnimarParticulaMorte(go, sr2, vel));
        }
    }

    static IEnumerator AnimarParticulaMorte(GameObject go, SpriteRenderer sr2, Vector2 vel)
    {
        float t = 0f, dur = Random.Range(0.6f, 1.3f);
        Color c = sr2 != null ? sr2.color : Color.white;
        while (t < dur && go != null)
        {
            t += Time.deltaTime;
            float p = t / dur;
            go.transform.position += new Vector3(vel.x, vel.y + 0.4f, 0f) * Time.deltaTime;
            vel *= 0.93f;
            if (sr2 != null) { Color nc = c; nc.a = 1f - p * p; sr2.color = nc; }
            yield return null;
        }
        if (go != null) Object.Destroy(go);
    }

    void OnDestroy()
    {
        if (bossCanvasGO != null) Destroy(bossCanvasGO);

        // Destrói projéteis em órbita se ainda existirem
        foreach (var go in projeteisCanalizando)
            if (go != null) Destroy(go);

        // Fallback: garante que o buff seja removido mesmo se IniciarEfeitoMorte não foi chamado
        RemoverBuffInimigos();
    }
}
