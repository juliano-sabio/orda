using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(InimigoController), typeof(Rigidbody2D))]
public class BossSlimeGuardaElite : MonoBehaviour, IBoss, IBossHud
{
    // ── IDENTIDADE ───────────────────────────────────────────────────────────────
    [Header("Identidade")]
    public string nomeBoss  = "Guardião Elite";
    public float  vidaBoss  = 3000f;

    // ── MOVIMENTO ────────────────────────────────────────────────────────────────
    [Header("Movimento")]
    public float velocidadePatrulha  = 2f;
    public float velocidadeChase     = 3.8f;
    public float distanciaDeteccao   = 10f;

    // ── DASH ─────────────────────────────────────────────────────────────────────
    [Header("Dash")]
    public float dashVelocidade = 22f;
    public float dashDuracao    = 0.5f;
    public float dashCooldown   = 4f;
    public float dashDano       = 25f;
    public float dashRaioHit    = 1.2f;

    // ── ATAQUE EM ÁREA ───────────────────────────────────────────────────────────
    [Header("Ataque em Área")]
    public float areaRaio     = 4f;
    public float areaDano     = 35f;
    public float areaCooldown = 8f;

    // ── ESCUDO ───────────────────────────────────────────────────────────────────
    [Header("Escudo")]
    public float escudoDuracao  = 3.5f;
    public float escudoCooldown = 14f;

    // ── INVOCAR ──────────────────────────────────────────────────────────────────
    [Header("Invocar Minions")]
    public GameObject[] prefabsMinions;
    public int   qtdMinions      = 3;
    public float invocarCooldown = 18f;

    // ── PROJÉTEIS EM LEQUE ───────────────────────────────────────────────────────
    [Header("Projéteis em Leque")]
    public int   qtdProjeteis       = 5;
    public float velocidadeProjetil = 8f;
    public float danoProjetil       = 15f;
    public float anguloLeque        = 60f;
    public float projeteisCooldown  = 5f;

    // ── GIRO DE ESPINHOS ─────────────────────────────────────────────────────────
    [Header("Giro de Espinhos")]
    public float giroRaio     = 3f;
    public float giroDano     = 20f;
    public float giroDuracao  = 2.5f;
    public float giroCooldown = 10f;

    // ── FASES ────────────────────────────────────────────────────────────────────
    [Header("Gatilhos de Fase (0–1)")]
    public float gatilhoFase2 = 0.75f;
    public float gatilhoFase3 = 0.50f;
    public float gatilhoFase4 = 0.25f;

    // ── ESTADO ───────────────────────────────────────────────────────────────────
    enum Estado { Idle, Patrulha, Perseguir, Dash, AtaqueArea, Escudo, Invocar }

    InimigoController controller;
    Rigidbody2D       rb;
    SpriteRenderer    sr;
    Animator          anim;
    Transform         player;

    Estado estado         = Estado.Idle;
    int    fase           = 1;
    bool   escudoAtivo    = false;
    bool   executandoAcao = false;

    float   timerDash, timerArea, timerEscudo, timerInvocar, timerProjeteis, timerGiro;
    Vector2 dirWander;
    float   wanderTimer;

    GameObject      bossCanvasGO;
    Image           hpFill, hpGhost;
    TextMeshProUGUI hpText, faseText;
    GameObject      escudoVisualGO;

    // ── LIFECYCLE ────────────────────────────────────────────────────────────────

    void Awake()
    {
        controller = GetComponent<InimigoController>();
        rb         = GetComponent<Rigidbody2D>();
        sr         = GetComponent<SpriteRenderer>();
        anim       = GetComponent<Animator>();

        // Define vida antes de qualquer Start() rodar
        if (controller != null)
        {
            controller.vidaMaxima = vidaBoss;
            controller.vidaAtual  = vidaBoss;
        }
    }

    // Co-op: re-mira o player mais próximo periodicamente (em SP = o único player).
    void AtualizarAlvoCoop()
    {
        var t = PlayerStats.MaisProximoTransform(transform.position);
        if (t != null) player = t;
    }

    void Start()
    {
        danoProjetil *= EnemyScaling.BossDanoMult(); // escala de dano do boss no spawn
        rb.gravityScale           = 0f;
        rb.mass                   = 1000f;
        rb.linearDamping          = 20f;
        rb.constraints            = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        AtualizarAlvoCoop();
        InvokeRepeating(nameof(AtualizarAlvoCoop), 0.5f, 0.5f);

        timerDash      = 3f;
        timerArea      = 7f;
        timerEscudo    = escudoCooldown;
        timerInvocar   = invocarCooldown;
        timerProjeteis = 4f;
        timerGiro      = 8f;

        EscolherDirecaoWander();
        CriarBossUI();
        StartCoroutine(IniciarComDelay());
        StartCoroutine(LoopAI());
    }

    void Update()
    {
        if (controller == null || controller.estaMorrendo) return;

        AtualizarUI();
        VerificarFase();

        if (!escudoAtivo && !executandoAcao)
        {
            timerDash      -= Time.deltaTime;
            timerArea      -= Time.deltaTime;
            timerEscudo    -= Time.deltaTime;
            timerInvocar   -= Time.deltaTime;
            timerProjeteis -= Time.deltaTime;
            timerGiro      -= Time.deltaTime;
        }
    }

    void FixedUpdate()
    {
        if (controller == null || controller.estaMorrendo) { rb.linearVelocity = Vector2.zero; return; }
        if (executandoAcao) return; // deixa o coroutine controlar a velocidade durante ações

        float vel = fase >= 4 ? velocidadeChase * 1.3f
                  : fase >= 2 ? velocidadeChase * 1.1f
                  : velocidadeChase;

        switch (estado)
        {
            case Estado.Patrulha:
                rb.linearVelocity = dirWander.normalized * velocidadePatrulha;
                VirarPara(dirWander);
                wanderTimer -= Time.fixedDeltaTime;
                if (wanderTimer <= 0f) EscolherDirecaoWander();
                break;
            case Estado.Perseguir:
                if (player != null)
                {
                    Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
                    rb.linearVelocity = dir * vel;
                    VirarPara(dir);
                }
                break;
        }
    }

    // ── AI LOOP ──────────────────────────────────────────────────────────────────

    IEnumerator LoopAI()
    {
        yield return new WaitForSeconds(2f);

        while (controller != null && !controller.estaMorrendo)
        {
            yield return new WaitForSeconds(0.25f);
            if (executandoAcao || escudoAtivo) continue;

            float dist = player != null
                ? Vector2.Distance(transform.position, player.position)
                : 999f;

            if (timerEscudo <= 0f && fase >= 3)
                yield return StartCoroutine(AcaoEscudo());
            else if (timerArea <= 0f && fase >= 2 && dist <= areaRaio * 2f)
                yield return StartCoroutine(AcaoArea());
            else if (timerGiro <= 0f && fase >= 2 && dist <= giroRaio * 1.8f)
                yield return StartCoroutine(AcaoGiro());
            else if (timerDash <= 0f && dist <= distanciaDeteccao && dist > 1.5f)
                yield return StartCoroutine(AcaoDash());
            else if (timerProjeteis <= 0f && dist <= distanciaDeteccao)
                yield return StartCoroutine(AcaoProjeteis());
            else if (timerInvocar <= 0f && fase >= 3)
                yield return StartCoroutine(AcaoInvocar());
            else if (dist <= distanciaDeteccao)
                estado = Estado.Perseguir;
            else
                estado = Estado.Patrulha;
        }
    }

    // ── FASES ────────────────────────────────────────────────────────────────────

    void VerificarFase()
    {
        if (controller == null) return;
        float pct = controller.GetPorcentagemVida();
        int novaFase = pct > gatilhoFase2 ? 1
                     : pct > gatilhoFase3 ? 2
                     : pct > gatilhoFase4 ? 3
                     : 4;

        if (novaFase <= fase) return;
        fase = novaFase;

        string msg; Color cor;
        switch (fase)
        {
            case 2: msg = "MODO AGRESSIVO!"; cor = new Color(1f, 0.7f, 0.1f); break;
            case 3: msg = "MODO FURIA!";     cor = new Color(1f, 0.3f, 0.1f); break;
            default: msg = "MODO DESESPERO!"; cor = new Color(0.8f, 0.1f, 1f); break;
        }

        if (fase >= 3) { dashCooldown *= 0.7f; areaCooldown *= 0.75f; }
        if (fase >= 4) { dashCooldown *= 0.7f; areaCooldown *= 0.75f; escudoCooldown *= 0.8f; }

        StartCoroutine(MostrarTextoTela(msg, cor, 2f));
        CameraShaker.Tremer(0.08f, 0.5f);
        if (faseText != null) { faseText.text = fase == 2 ? "AGRESSIVO" : fase == 3 ? "FURIA" : "DESESPERO"; faseText.color = cor; }
    }

    // ── AÇÕES ────────────────────────────────────────────────────────────────────

    IEnumerator AcaoDash()
    {
        if (player == null) yield break;
        executandoAcao = true;
        estado = Estado.Dash;
        timerDash = dashCooldown;

        rb.linearVelocity = Vector2.zero;
        anim?.SetBool("Atacando", true);
        yield return StartCoroutine(PiscaAviso(new Color(1f, 0.8f, 0.1f), 3, 0.1f));
        if (controller.estaMorrendo) { FinalizarAcao(); yield break; }

        Vector2 dir = player != null
            ? ((Vector2)player.position - (Vector2)transform.position).normalized
            : Vector2.right;
        VirarPara(dir);
        StartCoroutine(TrailDash());

        for (float t = 0f; t < dashDuracao; t += Time.deltaTime)
        {
            rb.linearVelocity = dir * dashVelocidade;
            if (player != null && Vector2.Distance(transform.position, player.position) < dashRaioHit)
            {
                player.GetComponent<PlayerStats>()?.TakeDamage(dashDano);
                CameraShaker.Tremer(0.12f, 0.2f);
                break;
            }
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        anim?.SetBool("Atacando", false);
        FinalizarAcao();
    }

    IEnumerator AcaoArea()
    {
        executandoAcao = true;
        estado = Estado.AtaqueArea;
        timerArea = areaCooldown;

        rb.linearVelocity = Vector2.zero;
        yield return StartCoroutine(AnimacaoSalto());
        if (controller.estaMorrendo) { FinalizarAcao(); yield break; }

        CameraShaker.Tremer(0.2f, 0.4f);

        if (player != null && Vector2.Distance(transform.position, player.position) <= areaRaio)
            player.GetComponent<PlayerStats>()?.TakeDamage(areaDano);

        StartCoroutine(VisualShockwave());
        yield return new WaitForSeconds(0.5f);
        FinalizarAcao();
    }

    IEnumerator AcaoEscudo()
    {
        executandoAcao = true;
        escudoAtivo    = true;
        estado         = Estado.Escudo;
        timerEscudo    = escudoCooldown;

        if (controller != null) controller.imuneAoDano = true;
        CriarVisualEscudo();
        StartCoroutine(MostrarTextoTela("ESCUDO ATIVADO!", new Color(0.3f, 0.7f, 1f), 1.5f));
        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(escudoDuracao);

        if (controller != null) controller.imuneAoDano = false;
        if (escudoVisualGO != null) StartCoroutine(FadeDestroyEscudo());
        escudoAtivo    = false;
        FinalizarAcao();
    }

    IEnumerator AcaoInvocar()
    {
        if (prefabsMinions == null || prefabsMinions.Length == 0) yield break;
        executandoAcao = true;
        estado         = Estado.Invocar;
        timerInvocar   = invocarCooldown;

        rb.linearVelocity = Vector2.zero;
        StartCoroutine(MostrarTextoTela("INVOCAR REFORÇOS!", new Color(1f, 0.4f, 0.8f), 1.5f));
        yield return StartCoroutine(PiscaAviso(new Color(1f, 0.4f, 0.8f), 4, 0.1f));
        if (controller.estaMorrendo) { FinalizarAcao(); yield break; }

        var ge = GerenciadorEventos.Instance;
        for (int i = 0; i < qtdMinions; i++)
        {
            var prefab = prefabsMinions[Random.Range(0, prefabsMinions.Length)];
            if (prefab == null) continue;

            Vector2 pos = (Vector2)transform.position + Random.insideUnitCircle * 3f;
            if (ge != null)
                for (int t = 0; t < 20; t++)
                {
                    Vector2 c = (Vector2)transform.position + Random.insideUnitCircle * 3f;
                    if (ge.PosicaoValida(c)) { pos = c; break; }
                }

            StartCoroutine(VisualSpawnMinion(pos));
            yield return new WaitForSeconds(0.35f);
            NetSpawn.Spawnar(prefab, new Vector3(pos.x, pos.y, 0f)); // co-op: minion replica
        }

        CameraShaker.Tremer(0.06f, 0.5f);
        FinalizarAcao();
    }

    IEnumerator AcaoProjeteis()
    {
        if (player == null) yield break;
        executandoAcao  = true;
        timerProjeteis  = projeteisCooldown;

        rb.linearVelocity = Vector2.zero;
        VirarPara(((Vector2)player.position - (Vector2)transform.position).normalized);
        yield return StartCoroutine(PiscaAviso(new Color(0.2f, 1f, 0.45f), 2, 0.12f));
        if (controller.estaMorrendo) { FinalizarAcao(); yield break; }

        Vector2 dirBase = ((Vector2)player.position - (Vector2)transform.position).normalized;
        float halfAng   = anguloLeque * 0.5f;
        float step      = qtdProjeteis > 1 ? anguloLeque / (qtdProjeteis - 1) : 0f;

        for (int i = 0; i < qtdProjeteis; i++)
        {
            float rad = (-halfAng + step * i) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(
                dirBase.x * Mathf.Cos(rad) - dirBase.y * Mathf.Sin(rad),
                dirBase.x * Mathf.Sin(rad) + dirBase.y * Mathf.Cos(rad));
            SpawnarProjetil(dir);
        }

        yield return new WaitForSeconds(0.4f);
        FinalizarAcao();
    }

    void SpawnarProjetil(Vector2 dir)
    {
        var go = new GameObject("ProjetilBossElite");
        go.transform.position = transform.position;
        go.AddComponent<ProjetilBossElite>().Iniciar(dir, velocidadeProjetil, danoProjetil);
    }

    IEnumerator AcaoGiro()
    {
        executandoAcao = true;
        timerGiro      = giroCooldown;

        rb.linearVelocity = Vector2.zero;
        StartCoroutine(MostrarTextoTela("GIRO DE ESPINHOS!", new Color(1f, 0.5f, 0.1f), 1f));

        // Cria 6 espinhos girando
        const int QTD_ESPINHOS = 6;
        var giroGO = new GameObject("GiroEspinhos");
        var lrs    = new LineRenderer[QTD_ESPINHOS];
        for (int i = 0; i < QTD_ESPINHOS; i++)
        {
            var sgo = new GameObject($"Spike{i}");
            sgo.transform.SetParent(giroGO.transform, false);
            var lr = sgo.AddComponent<LineRenderer>();
            lr.useWorldSpace = true; lr.positionCount = 2;
            lr.material      = new Material(Shader.Find("Sprites/Default"));
            lr.sortingOrder  = 11;
            lr.startWidth    = 0.14f; lr.endWidth = 0.02f;
            lr.startColor    = lr.endColor = new Color(1f, 0.65f, 0.1f, 0.95f);
            lrs[i] = lr;
        }

        float ang = 0f, proxDano = 0f;

        for (float t = 0f; t < giroDuracao; t += Time.deltaTime)
        {
            if (controller.estaMorrendo || giroGO == null) break;

            ang += Time.deltaTime * 300f;
            giroGO.transform.position = transform.position;

            float pulso = Mathf.Sin(t * 10f) * 0.5f + 0.5f;
            for (int i = 0; i < QTD_ESPINHOS; i++)
            {
                if (lrs[i] == null) continue;
                float a   = (ang + 360f / QTD_ESPINHOS * i) * Mathf.Deg2Rad;
                Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                lrs[i].SetPosition(0, (Vector2)transform.position + d * 0.3f);
                lrs[i].SetPosition(1, (Vector2)transform.position + d * giroRaio);
                lrs[i].startColor = lrs[i].endColor =
                    new Color(1f, 0.5f + pulso * 0.45f, 0.1f, 0.75f + pulso * 0.25f);
                lrs[i].startWidth = 0.1f + pulso * 0.08f;
            }

            proxDano -= Time.deltaTime;
            if (proxDano <= 0f && player != null &&
                Vector2.Distance(transform.position, player.position) <= giroRaio)
            {
                player.GetComponent<PlayerStats>()?.TakeDamage(giroDano);
                CameraShaker.Tremer(0.1f, 0.15f);
                proxDano = 0.5f;
            }

            yield return null;
        }

        if (giroGO != null) Destroy(giroGO);
        FinalizarAcao();
    }

    void FinalizarAcao()
    {
        executandoAcao = false;
        estado = Estado.Perseguir;
    }

    // ── VISUAIS ──────────────────────────────────────────────────────────────────

    IEnumerator TrailDash()
    {
        Color cor = new Color(0.9f, 0.55f, 0.1f, 0.65f);
        for (float t = 0f; t < dashDuracao; t += 0.04f)
        {
            var ghost = new GameObject("DashTrail");
            ghost.transform.SetPositionAndRotation(transform.position, transform.rotation);
            ghost.transform.localScale = transform.localScale;
            var gsr = ghost.AddComponent<SpriteRenderer>();
            if (sr != null) { gsr.sprite = sr.sprite; gsr.flipX = sr.flipX; gsr.sortingLayerName = sr.sortingLayerName; gsr.sortingOrder = sr.sortingOrder - 1; }
            gsr.color = cor;
            StartCoroutine(FadeGhost(gsr, 0.18f));
            yield return new WaitForSeconds(0.04f);
        }
    }

    IEnumerator FadeGhost(SpriteRenderer g, float dur)
    {
        Color inicio = g.color;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (g == null) yield break;
            g.color = new Color(inicio.r, inicio.g, inicio.b, Mathf.Lerp(inicio.a, 0f, t / dur));
            yield return null;
        }
        if (g != null) Destroy(g.gameObject);
    }

    IEnumerator AnimacaoSalto()
    {
        Vector3 base_ = transform.localScale;
        // Agacha
        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            float p = t / 0.25f;
            transform.localScale = new Vector3(base_.x * (1f + p * 0.3f), base_.y * (1f - p * 0.25f), 1f);
            yield return null;
        }
        // Estica
        for (float t = 0f; t < 0.2f; t += Time.deltaTime)
        {
            float p = t / 0.2f;
            transform.localScale = new Vector3(base_.x * (1.3f - p * 0.5f), base_.y * (0.75f + p * 0.75f), 1f);
            yield return null;
        }
        // Impacto (achata)
        transform.localScale = new Vector3(base_.x * 1.45f, base_.y * 0.55f, 1f);
        yield return new WaitForSeconds(0.06f);
        // Volta
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, base_, t / 0.3f);
            yield return null;
        }
        transform.localScale = base_;
    }

    IEnumerator VisualShockwave()
    {
        const int SEGS = 48;
        var go = new GameObject("Shockwave");
        go.transform.position = transform.position;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = SEGS;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 12;

        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p = t / 0.5f;
            float r = Mathf.Lerp(0.3f, areaRaio, p);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.35f, 0.02f, p);
            lr.startColor = lr.endColor = new Color(1f, 0.7f, 0.1f, Mathf.Lerp(1f, 0f, p));
            for (int i = 0; i < SEGS; i++)
            {
                float ang = 360f / SEGS * i * Mathf.Deg2Rad;
                lr.SetPosition(i, (Vector2)transform.position + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r);
            }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    void CriarVisualEscudo()
    {
        if (escudoVisualGO != null) Destroy(escudoVisualGO);
        escudoVisualGO = new GameObject("EscudoVisual");
        escudoVisualGO.transform.position = transform.position;
        var lr = escudoVisualGO.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = 40;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 13;
        StartCoroutine(AnimarEscudo(lr));
    }

    IEnumerator AnimarEscudo(LineRenderer lr)
    {
        float ang = 0f;
        while (escudoAtivo && lr != null)
        {
            ang += Time.deltaTime * 120f;
            for (int i = 0; i < 40; i++)
            {
                float a = (ang + 360f / 40 * i) * Mathf.Deg2Rad;
                lr.SetPosition(i, (Vector2)transform.position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 1.5f);
            }
            float pulso = Mathf.Sin(Time.time * 6f) * 0.5f + 0.5f;
            lr.startColor = lr.endColor = new Color(0.3f, 0.7f, 1f, 0.5f + pulso * 0.45f);
            lr.startWidth = lr.endWidth = 0.08f + pulso * 0.08f;
            yield return null;
        }
    }

    IEnumerator FadeDestroyEscudo()
    {
        var lr = escudoVisualGO?.GetComponent<LineRenderer>();
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            if (lr == null) yield break;
            Color c = lr.startColor; c.a = Mathf.Lerp(1f, 0f, t / 0.4f);
            lr.startColor = lr.endColor = c;
            yield return null;
        }
        if (escudoVisualGO != null) Destroy(escudoVisualGO);
    }

    IEnumerator VisualSpawnMinion(Vector2 pos)
    {
        const int SEGS = 24;
        var go = new GameObject("SpawnMinion");
        go.transform.position = pos;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = SEGS;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 10;

        for (float t = 0f; t < 0.35f; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p = t / 0.35f;
            float r = Mathf.Lerp(0.1f, 1.2f, p);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.2f, 0.02f, p);
            lr.startColor = lr.endColor = new Color(1f, 0.4f, 0.8f, Mathf.Lerp(1f, 0f, p));
            for (int i = 0; i < SEGS; i++)
            {
                float ang = 360f / SEGS * i * Mathf.Deg2Rad;
                lr.SetPosition(i, pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r);
            }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    // ── UTILIDADES ───────────────────────────────────────────────────────────────

    IEnumerator PiscaAviso(Color cor, int vezes, float intervalo)
    {
        if (sr == null) yield break;
        for (int i = 0; i < vezes; i++)
        {
            sr.color = cor; yield return new WaitForSeconds(intervalo);
            sr.color = Color.white; yield return new WaitForSeconds(intervalo);
        }
    }

    void VirarPara(Vector2 dir)
    {
        if (sr == null || Mathf.Abs(dir.x) < 0.1f) return;
        sr.flipX = dir.x < 0f;
    }

    void EscolherDirecaoWander()
    {
        float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        dirWander   = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
        wanderTimer = Random.Range(1.5f, 3.5f);
    }

    // ── ENTRADA ──────────────────────────────────────────────────────────────────

    // Aguarda um frame para o InimigoController.Start() capturar corOriginal antes de esconder o sprite
    IEnumerator IniciarComDelay()
    {
        yield return null;
        StartCoroutine(SequenciaEntrada());
    }

    IEnumerator SequenciaEntrada()
    {
        if (sr != null) { Color c = sr.color; c.a = 0f; sr.color = c; }
        yield return StartCoroutine(MostrarAvisoBoss());
        CameraShaker.Tremer(0.06f, 1.5f);
        yield return StartCoroutine(FadeAlpha(0f, 1f, 0.5f));
        // Garante cor totalmente opaca para que corOriginal do InimigoController fique correto
        if (sr != null) sr.color = Color.white;
        estado = Estado.Perseguir;
    }

    IEnumerator FadeAlpha(float de, float para, float dur)
    {
        if (sr == null) yield break;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            Color c = sr.color; c.a = Mathf.Lerp(de, para, t / dur); sr.color = c;
            yield return null;
        }
        Color cf = sr.color; cf.a = para; sr.color = cf;
    }

    IEnumerator MostrarAvisoBoss()
    {
        var warnGO = new GameObject("BossWarning");
        var cv = warnGO.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 200;
        warnGO.AddComponent<CanvasScaler>();

        var bgGO = CriarUIGO("BG", warnGO.transform);
        ExpandirRect(bgGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        var bgImg = bgGO.AddComponent<Image>(); bgImg.color = new Color(0, 0, 0, 0);

        var txtGO = CriarUIGO("WarnText", warnGO.transform);
        var tr = txtGO.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0.05f, 0.35f); tr.anchorMax = new Vector2(0.95f, 0.65f);
        tr.offsetMin = tr.offsetMax = Vector2.zero;
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text = $"{Loc.T("boss.appeared")}\n<size=60%>{nomeBoss.ToUpper()}</size>";
        txt.fontSize = 52; txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = new Color(0.9f, 0.55f, 0.1f, 0f);

        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        { float a = t / 0.4f; bgImg.color = new Color(0, 0, 0, a * 0.65f); txt.color = new Color(0.9f, 0.55f, 0.1f, a); yield return null; }

        for (int i = 0; i < 3; i++)
        { txt.color = new Color(1f, 0.85f, 0.1f, 1f); yield return new WaitForSeconds(0.15f); txt.color = new Color(0.9f, 0.4f, 0.05f, 1f); yield return new WaitForSeconds(0.15f); }

        yield return new WaitForSeconds(0.4f);

        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        { float a = 1f - t / 0.4f; bgImg.color = new Color(0, 0, 0, a * 0.65f); txt.color = new Color(0.9f, 0.55f, 0.1f, a); yield return null; }

        Destroy(warnGO);
    }

    // ── MORTE ────────────────────────────────────────────────────────────────────

    public void IniciarEfeitoMorte() => StartCoroutine(EfeitoMorte());

    IEnumerator EfeitoMorte()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        rb.linearVelocity = Vector2.zero;
        if (escudoVisualGO != null) Destroy(escudoVisualGO);
        CameraShaker.Tremer(0.15f, 2.5f);

        for (int i = 0; i < 12; i++)
        { if (sr != null) sr.color = i % 2 == 0 ? Color.white : new Color(0.9f, 0.5f, 0.1f); yield return new WaitForSeconds(0.05f); }

        Vector3 escBase = transform.localScale;
        for (float t = 0f; t < 0.9f; t += Time.deltaTime)
        {
            float p = t / 0.9f;
            transform.localScale = escBase * Mathf.Lerp(1f, 2.5f, Mathf.Pow(p, 0.5f));
            if (sr != null) sr.color = new Color(1f, 0.8f, 0.3f, Mathf.Lerp(1f, 0f, p * p));
            yield return null;
        }

        BossMorteUI.Exibir($"{nomeBoss.ToUpper()} DERROTADO!", new Color(1f, 0.8f, 0.2f));
        if (bossCanvasGO != null) Destroy(bossCanvasGO);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (bossCanvasGO   != null) Destroy(bossCanvasGO);
        if (escudoVisualGO != null) Destroy(escudoVisualGO);
    }

    // ── BOSS UI ──────────────────────────────────────────────────────────────────

    public void CriarBossUI()
    {
        if (controller == null) controller = GetComponent<InimigoController>(); // co-op: no cliente o Start não roda
        bossCanvasGO = new GameObject("BossEliteCanvas");
        var cv = bossCanvasGO.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 50;
        var cs = bossCanvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080); cs.matchWidthOrHeight = 0.5f;
        bossCanvasGO.AddComponent<GraphicRaycaster>();

        var painel = CriarUIGO("BossPanel", bossCanvasGO.transform);
        var pr = painel.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.2f, 0.825f); pr.anchorMax = new Vector2(0.8f, 0.935f);
        pr.offsetMin = pr.offsetMax = Vector2.zero;
        painel.AddComponent<Image>().color = new Color(0.03f, 0.03f, 0.06f, 0.92f);

        var linha = CriarUIGO("Linha", painel.transform);
        var lr_ = linha.GetComponent<RectTransform>();
        lr_.anchorMin = new Vector2(0f, 0.92f); lr_.anchorMax = new Vector2(1f, 1f);
        lr_.offsetMin = lr_.offsetMax = Vector2.zero;
        linha.AddComponent<Image>().color = new Color(0.9f, 0.55f, 0.1f);

        var nomeGO = CriarUIGO("Nome", painel.transform);
        var nr = nomeGO.GetComponent<RectTransform>();
        nr.anchorMin = new Vector2(0.01f, 0.6f); nr.anchorMax = new Vector2(0.99f, 0.92f);
        nr.offsetMin = nr.offsetMax = Vector2.zero;
        var nomeTxt = nomeGO.AddComponent<TextMeshProUGUI>();
        nomeTxt.text = nomeBoss.ToUpper(); nomeTxt.fontSize = 20; nomeTxt.fontStyle = FontStyles.Bold;
        nomeTxt.color = new Color(1f, 0.8f, 0.2f); nomeTxt.alignment = TextAlignmentOptions.Center;

        var faseGO = CriarUIGO("Fase", painel.transform);
        var fr = faseGO.GetComponent<RectTransform>();
        fr.anchorMin = new Vector2(0.75f, 0.92f); fr.anchorMax = new Vector2(0.99f, 1f);
        fr.offsetMin = fr.offsetMax = Vector2.zero;
        faseText = faseGO.AddComponent<TextMeshProUGUI>();
        faseText.text = Loc.T("boss.patrolling"); faseText.fontSize = 13;
        faseText.color = new Color(0.75f, 0.75f, 0.75f); faseText.alignment = TextAlignmentOptions.MidlineRight;

        var barBG = CriarUIGO("HPBarBG", painel.transform);
        var bbr = barBG.GetComponent<RectTransform>();
        bbr.anchorMin = new Vector2(0.01f, 0.08f); bbr.anchorMax = new Vector2(0.99f, 0.52f);
        bbr.offsetMin = bbr.offsetMax = Vector2.zero;
        barBG.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.12f);

        var ghostGO = CriarUIGO("HPGhost", barBG.transform);
        ExpandirRect(ghostGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        hpGhost = ghostGO.AddComponent<Image>();
        hpGhost.type = Image.Type.Filled; hpGhost.fillMethod = Image.FillMethod.Horizontal;
        hpGhost.fillAmount = 1f; hpGhost.color = new Color(1f, 0.88f, 0.25f, 0.9f);

        var fillGO = CriarUIGO("HPFill", barBG.transform);
        ExpandirRect(fillGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        hpFill = fillGO.AddComponent<Image>();
        hpFill.color = new Color(0.9f, 0.55f, 0.1f);
        hpFill.type = Image.Type.Filled; hpFill.fillMethod = Image.FillMethod.Horizontal; hpFill.fillAmount = 1f;

        var hpTextGO = CriarUIGO("HPText", barBG.transform);
        ExpandirRect(hpTextGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        hpText = hpTextGO.AddComponent<TextMeshProUGUI>();
        hpText.fontSize = 11; hpText.color = Color.white;
        hpText.fontStyle = FontStyles.Bold; hpText.alignment = TextAlignmentOptions.Center;

        StartCoroutine(EntradaUI(painel));
    }

    IEnumerator EntradaUI(GameObject painel)
    {
        var cg = painel.AddComponent<CanvasGroup>(); cg.alpha = 0f;
        for (float t = 0f; t < 1f; t += Time.deltaTime * 1.5f) { cg.alpha = t; yield return null; }
        cg.alpha = 1f;
    }

    public void AtualizarBarraUI() => AtualizarUI();   // IBossHud (cliente dirige com vida sincronizada)

    public int FaseUI { get => fase; set => fase = value; }

    void AtualizarUI()
    {
        if (hpFill == null || controller == null) return;
        float pct = controller.GetPorcentagemVida();
        hpFill.fillAmount  = Mathf.Lerp(hpFill.fillAmount, pct, Time.deltaTime * 3f);
        hpGhost.fillAmount = Mathf.MoveTowards(hpGhost.fillAmount, pct, Time.deltaTime * 0.3f);
        hpFill.color = escudoAtivo ? new Color(0.3f, 0.7f, 1f)
                     : fase >= 4   ? new Color(0.8f, 0.1f, 1f)
                     : fase >= 3   ? new Color(1f, 0.3f, 0.1f)
                     :               new Color(0.9f, 0.55f, 0.1f);
        if (hpText != null)
            hpText.text = $"{Mathf.RoundToInt(controller.vidaAtual)} / {Mathf.RoundToInt(controller.vidaMaxima)}";
        if (faseText != null && fase == 1)
            faseText.text = estado == Estado.Perseguir ? Loc.T("boss.hunting") : Loc.T("boss.patrolling");
    }

    IEnumerator MostrarTextoTela(string mensagem, Color cor, float duracao)
    {
        var go = new GameObject("BossEliteMsg");
        var cv = go.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 150;
        go.AddComponent<CanvasScaler>();
        var txtGO = CriarUIGO("Msg", go.transform);
        var tr = txtGO.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0.1f, 0.55f); tr.anchorMax = new Vector2(0.9f, 0.75f);
        tr.offsetMin = tr.offsetMax = Vector2.zero;
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text = mensagem; txt.fontSize = 36; txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center; txt.color = new Color(cor.r, cor.g, cor.b, 0f);

        for (float t = 0f; t < 0.3f; t += Time.deltaTime) { txt.color = new Color(cor.r, cor.g, cor.b, t / 0.3f); yield return null; }
        txt.color = new Color(cor.r, cor.g, cor.b, 1f);
        yield return new WaitForSeconds(duracao);

        var msgR = txtGO.GetComponent<RectTransform>();
        Vector2 posBase = msgR.anchoredPosition;
        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            txt.color = new Color(cor.r, cor.g, cor.b, 1f - t / 0.5f);
            msgR.anchoredPosition = posBase + Vector2.up * (t * 100f);
            yield return null;
        }
        Destroy(go);
    }

    // ── GIZMOS ───────────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, distanciaDeteccao);
        Gizmos.color = Color.red;    Gizmos.DrawWireSphere(transform.position, areaRaio);
        Gizmos.color = Color.cyan;   Gizmos.DrawWireSphere(transform.position, dashRaioHit);
    }

    // ── HELPERS UI ───────────────────────────────────────────────────────────────

    static GameObject CriarUIGO(string nome, Transform pai)
    { var go = new GameObject(nome); go.transform.SetParent(pai, false); go.AddComponent<RectTransform>(); return go; }

    static void ExpandirRect(RectTransform rt, Vector2 min, Vector2 max)
    { rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero; }
}

// ── Projétil do Boss Elite ────────────────────────────────────────────────────────

public class ProjetilBossElite : MonoBehaviour
{
    Vector2 dir;
    float   velocidade;
    float   dano;

    public void Iniciar(Vector2 direcao, float vel, float dmg)
    {
        dir        = direcao;
        velocidade = vel;
        dano       = dmg;

        var sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite       = GerarDisco(12);
        sr.color        = new Color(0.2f, 1f, 0.4f);
        sr.sortingOrder = 12;
        transform.localScale = Vector3.one * 0.35f;

        var col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.5f;

        Destroy(gameObject, 3.5f);
    }

    void Update()
    {
        transform.position += (Vector3)(dir * velocidade * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        other.GetComponent<PlayerStats>()?.TakeDamage(dano);
        Destroy(gameObject);
    }

    static Sprite GerarDisco(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cx));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, d < cx ? 1f : 0f));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }
}
