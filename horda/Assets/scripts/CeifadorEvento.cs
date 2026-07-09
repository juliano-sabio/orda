using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CeifadorEvento : MonoBehaviour, IEnemyCosmetic
{
    [Header("Movimento")]
    public float velocidadeWander = 1.8f;
    public float raioWander       = 5f;
    public float tempoMudanca     = 3f;

    [Header("Detecção e Dash")]
    public float raioDeteccao  = 6f;
    public float velocidadeDash = 14f;
    public float duracaoDash    = 0.35f;
    public float cooldownDash   = 1.8f;

    [Header("Animação")]
    public RuntimeAnimatorController controllerCeifador;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Transform player;

    private Vector2 direcaoWander;
    private float   proximaMudanca;
    private float   proximoDash;
    private bool    isDashing;
    private Vector2 dashDir;
    private bool    dashAcertouPlayer;

    [Header("Dano de Contato")]
    public float danoContato      = 60f;
    public float intervaloContato = 0.6f;
    public float raioContato      = 0.6f;
    float proxDanoContato;

    private enum Estado { Wander, Perseguindo, Dashing }
    private Estado estado = Estado.Wander;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    // Co-op (cliente): o EnemyNet desligou o gameplay; aqui montamos só os visuais
    // (controller de animação + luz vermelha + efeito de spawn), sem IA/dano.
    public void SetupVisualCosmetico()
    {
        if (controllerCeifador != null)
        {
            var anim = GetComponent<Animator>();
            if (anim != null) anim.runtimeAnimatorController = controllerCeifador;
        }
        AdicionarBrilhoVermelho();
        StartCoroutine(EfeitoSpawn());
    }

void Start()
{
    if (controllerCeifador != null)
    {
        var anim = GetComponent<Animator>();
        if (anim != null) anim.runtimeAnimatorController = controllerCeifador;
    }

    // co-op: mira o player mais próximo no spawn (inimigo host-autoritativo), não "o primeiro".
    var psAlvo = PlayerStats.MaisProximo(transform.position);
    if (psAlvo == null) psAlvo = FindFirstObjectByType<PlayerStats>(); // coop-local-ok: fallback p/ SP cedo
    if (psAlvo != null) player = psAlvo.transform;

    AdicionarBrilhoVermelho();
    EscolherNovaDirecao();
    StartCoroutine(EfeitoSpawn());

    // Desativa DanoInimigo — dano de contato é tratado manualmente por proximidade
    var danoComp = GetComponent<DanoInimigo>();
    if (danoComp != null) danoComp.enabled = false;

    var ic = GetComponent<InimigoController>();
    if (ic != null) ic.danoAtual *= 0.4f;

    IgnorarColisaoPlayer();
}

IEnumerator EfeitoSpawn()
{
    // Começa invisível
    if (sr != null) { Color c = sr.color; c.a = 0f; sr.color = c; }

    Vector2 pos = transform.position;

    // Portal escuro no chão
    StartCoroutine(PortalSpawn(pos));

    // Espera um pouco antes de aparecer
    yield return new WaitForSeconds(0.4f);

    // Fade in do ceifador
    float durFade = 0.35f;
    for (float t = 0f; t < durFade; t += Time.deltaTime)
    {
        if (sr == null) yield break;
        Color c = sr.color; c.a = Mathf.Lerp(0f, 1f, t / durFade); sr.color = c;
        yield return null;
    }
    if (sr != null) { Color c = sr.color; c.a = 1f; sr.color = c; }
}

IEnumerator PortalSpawn(Vector2 pos)
{
    const int SEGS = 40;

    // Anel externo vermelho escuro
    var goAnel = new GameObject("PortalCeifador");
    goAnel.transform.position = pos;
    var lrAnel = goAnel.AddComponent<LineRenderer>();
    lrAnel.useWorldSpace = true; lrAnel.loop = true; lrAnel.positionCount = SEGS;
    lrAnel.material = new Material(Shader.Find("Sprites/Default")); lrAnel.sortingOrder = 8;

    // Fill sombrio
    var goFill = new GameObject("PortalFill");
    goFill.transform.position = pos;
    var srFill = goFill.AddComponent<SpriteRenderer>();
    srFill.sprite = GerarDisco(64);
    srFill.color  = new Color(0.3f, 0f, 0f, 0f);
    srFill.sortingOrder = 7;

    // Fase 1: portal abre (expansão)
    float durAbrir = 0.45f;
    for (float t = 0f; t < durAbrir; t += Time.deltaTime)
    {
        if (goAnel == null) yield break;
        float p    = t / durAbrir;
        float raio = Mathf.Lerp(0.05f, 1.6f, Mathf.Pow(p, 0.4f));
        float pulso = Mathf.Sin(t * 20f) * 0.1f + 0.9f;

        // Anel
        for (int i = 0; i < SEGS; i++)
        {
            float ang = 360f / SEGS * i * Mathf.Deg2Rad;
            lrAnel.SetPosition(i, pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raio * pulso);
        }
        lrAnel.startWidth = lrAnel.endWidth = Mathf.Lerp(0.05f, 0.2f, p);
        lrAnel.startColor = lrAnel.endColor = new Color(0.8f, 0.05f, 0.05f, 0.9f);

        // Fill
        srFill.color = new Color(0.3f, 0f, 0f, Mathf.Lerp(0f, 0.5f, p));
        goFill.transform.localScale = Vector3.one * (raio * 2f);

        // Partículas de fumaça negra
        if (Time.frameCount % 3 == 0) SpawnParticulaPortal(pos, raio);

        yield return null;
    }

    // Fase 2: portal fecha com flash
    lrAnel.startColor = lrAnel.endColor = Color.white;
    srFill.color = new Color(1f, 0.3f, 0.3f, 0.7f);
    yield return null;

    float durFechar = 0.3f;
    for (float t = 0f; t < durFechar; t += Time.deltaTime)
    {
        if (goAnel == null) yield break;
        float p    = t / durFechar;
        float raio = Mathf.Lerp(1.6f, 0f, p * p);
        for (int i = 0; i < SEGS; i++)
        {
            float ang = 360f / SEGS * i * Mathf.Deg2Rad;
            lrAnel.SetPosition(i, pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raio);
        }
        lrAnel.startColor = lrAnel.endColor = new Color(0.8f, 0.05f, 0.05f, 1f - p);
        srFill.color = new Color(0.3f, 0f, 0f, Mathf.Lerp(0.5f, 0f, p));
        goFill.transform.localScale = Vector3.one * (raio * 2f);
        yield return null;
    }

    if (goAnel  != null) Destroy(goAnel);
    if (goFill  != null) Destroy(goFill);
}

void SpawnParticulaPortal(Vector2 centro, float raio)
{
    float ang = Random.Range(0f, Mathf.PI * 2f);
    Vector2 pos = centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raio;

    var go = new GameObject("FumacaPortal");
    go.transform.position = pos;
    var sr2 = go.AddComponent<SpriteRenderer>();
    sr2.sprite = GerarDisco(8);
    sr2.color  = new Color(0.15f, 0f, 0f, 0.7f);
    sr2.sortingOrder = 9;
    go.transform.localScale = Vector3.one * Random.Range(0.12f, 0.3f);

    Vector2 vel = (centro - pos).normalized * Random.Range(0.5f, 1.5f)
                + Vector2.up * Random.Range(0.3f, 1f);
    go.AddComponent<FumacaCeifadorFX>().Iniciar(vel, sr2.color);
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

void AdicionarBrilhoVermelho()
{
    var luzGO = new GameObject("BrilhoVermelho");
    luzGO.transform.SetParent(transform, false);
    luzGO.transform.localPosition = Vector3.zero;

    var luz = luzGO.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
    luz.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
    luz.color = new Color(1f, 0.05f, 0.05f, 1f);
    luz.intensity = 2.5f;
    luz.pointLightOuterRadius = 2.5f;
    luz.pointLightInnerRadius = 0.4f;
    luz.shadowsEnabled = false;
}

    void IgnorarColisaoPlayer()
    {
        var myCols = GetComponents<Collider2D>();
        // co-op: ignora colisão com TODOS os players, não só o primeiro.
        for (int i = 0; i < PlayerStats.All.Count; i++)
        {
            var ps = PlayerStats.All[i];
            if (ps == null) continue;
            var playerCols = ps.GetComponents<Collider2D>();
            foreach (var mc in myCols)
            foreach (var pc in playerCols)
                Physics2D.IgnoreCollision(mc, pc, true);
        }
    }

    void Update()
    {
        // Co-op: se o alvo caiu (downed), SOLTA e mira outro player válido — senão o ceifador
        // continuava perseguindo/atacando o player 2 morto. MaisProximo já ignora caídos;
        // se todos estão caídos, fica sem alvo (não persegue ninguém).
        var psAtual = player != null ? player.GetComponent<PlayerStats>() : null;
        if (player == null || (psAtual != null && psAtual.EstaCaido))
        {
            var novo = PlayerStats.MaisProximo(transform.position);
            player = novo != null ? novo.transform : null;
        }
        if (player == null) return;

        // Dano de contato por proximidade (sem colisão física)
        if (!isDashing && Time.time >= proxDanoContato)
        {
            float d = Vector2.Distance(transform.position, player.position);
            if (d <= raioContato)
            {
                var ps = player.GetComponent<PlayerStats>();
                if (ps != null) ps.TakeDamage(DanoCapado(ps, danoContato));
                proxDanoContato = Time.time + intervaloContato;
            }
        }

        float dist = Vector2.Distance(transform.position, player.position);

        switch (estado)
        {
            case Estado.Wander:
                if (dist <= raioDeteccao)
                    estado = Estado.Perseguindo;
                else if (Time.time >= proximaMudanca)
                    EscolherNovaDirecao();
                break;

            case Estado.Perseguindo:
                if (dist > raioDeteccao * 1.6f)
                {
                    estado = Estado.Wander;
                    EscolherNovaDirecao();
                }
                else if (Time.time >= proximoDash)
                {
                    StartCoroutine(Dash());
                }
                break;

            case Estado.Dashing:
                break;
        }

        // Flip sprite pela velocidade
        Vector2 vel = rb.linearVelocity;
        if (sr != null)
        {
            if (vel.x > 0.05f)       sr.flipX = false;
            else if (vel.x < -0.05f) sr.flipX = true;
        }
    }

    // O Ceifador nunca tira mais que 30% da vida MÁXIMA do player num hit (não pode quase
    // one-shotar) — cap pedido pelo design.
    static float DanoCapado(PlayerStats ps, float bruto)
        => ps == null ? bruto : Mathf.Min(bruto, 0.30f * ps.maxHealth);

    void FixedUpdate()
    {
        if (isDashing)
        {
            rb.linearVelocity = dashDir * velocidadeDash;
            return;
        }

        switch (estado)
        {
            case Estado.Wander:
                rb.linearVelocity = direcaoWander * velocidadeWander;
                break;

            case Estado.Perseguindo:
                if (player != null)
                {
                    Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
                    rb.linearVelocity = dir * velocidadeWander * 1.4f;
                }
                break;
        }
    }

    IEnumerator Dash()
    {
        if (player == null) yield break;

        estado            = Estado.Dashing;
        isDashing         = true;
        dashAcertouPlayer = false;
        dashDir           = ((Vector2)player.position - (Vector2)transform.position).normalized;

        float elapsed = 0f;
        while (elapsed < duracaoDash)
        {
            elapsed += Time.deltaTime;

            // Detecta passagem pelo player por proximidade
            if (!dashAcertouPlayer && player != null)
            {
                float d = Vector2.Distance(transform.position, player.position);
                if (d <= raioContato * 1.6f)
                {
                    dashAcertouPlayer = true;
                    EfeitoCorte((Vector2)player.position, dashDir);
                    var psDash = player.GetComponent<PlayerStats>();
                    if (psDash != null) psDash.TakeDamage(DanoCapado(psDash, danoContato * 2f));
                    proxDanoContato = Time.time + intervaloContato;
                }
            }

            yield return null;
        }

        isDashing         = false;
        dashAcertouPlayer = false;
        proximoDash       = Time.time + cooldownDash;
        estado            = Estado.Perseguindo;
    }

    void EscolherNovaDirecao()
    {
        var ge = GerenciadorEventos.Instance;
        for (int i = 0; i < 10; i++)
        {
            float   ang  = Random.Range(0f, Mathf.PI * 2f);
            Vector2 alvo = (Vector2)transform.position
                         + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raioWander;

            if (ge == null || ge.PosicaoValida(alvo))
            {
                direcaoWander   = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                proximaMudanca  = Time.time + tempoMudanca + Random.Range(-0.5f, 0.5f);
                return;
            }
        }
        direcaoWander  = -direcaoWander;
        proximaMudanca = Time.time + 1f;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        // Player não colide fisicamente — tratado via proximidade no Dash() e Update()
        if (col.gameObject.CompareTag("Player")) return;

        // Cancela dash ao bater em obstáculo
        if (isDashing)
        {
            StopAllCoroutines();
            isDashing         = false;
            dashAcertouPlayer = false;
            proximoDash       = Time.time + cooldownDash;
            estado            = Estado.Perseguindo;
        }
        else if (estado == Estado.Wander)
        {
            EscolherNovaDirecao();
        }
    }

    // ── Efeito de Corte ───────────────────────────────────────────────────────

    void EfeitoCorte(Vector2 pos, Vector2 dir)
    {
        // Flash de impacto
        var flash   = new GameObject("FlashCorte");
        flash.transform.position = pos;
        var flashSR = flash.AddComponent<SpriteRenderer>();
        flashSR.sprite       = GerarDisco(32);
        flashSR.color        = new Color(1f, 0.85f, 0.85f, 0.95f);
        flashSR.sortingOrder = 20;
        flash.transform.localScale = Vector3.one * 0.5f;
        StartCoroutine(AnimarFlash(flash));

        // 3 riscos em leque perpendiculares ao dash
        float angBase = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f;

        float[] angulos     = { angBase - 22f, angBase,       angBase + 22f };
        float[] comprimentos = { 0.85f,          1.15f,         0.85f         };
        Color[] cores = {
            new Color(0.85f, 0.20f, 0.20f, 0.90f),
            new Color(1.00f, 0.90f, 0.90f, 1.00f),
            new Color(0.85f, 0.20f, 0.20f, 0.90f),
        };

        for (int i = 0; i < 3; i++)
        {
            // Offset lateral para espaçar os riscos
            float lateralAng = angulos[i] * Mathf.Deg2Rad;
            Vector2 offset   = new Vector2(Mathf.Cos(lateralAng), Mathf.Sin(lateralAng))
                             * (i - 1) * 0.18f;

            var risco = new GameObject($"RiscoCorte_{i}");
            risco.transform.position = (Vector3)pos + (Vector3)offset;
            risco.transform.rotation = Quaternion.Euler(0f, 0f, angulos[i]);

            var sr2 = risco.AddComponent<SpriteRenderer>();
            sr2.sprite       = GerarLinhaCorte();
            sr2.color        = cores[i];
            sr2.sortingOrder = 20;
            risco.transform.localScale = new Vector3(comprimentos[i], 0.8f, 1f);

            StartCoroutine(AnimarRisco(sr2, i * 0.025f));
        }
    }

    IEnumerator AnimarFlash(GameObject go)
    {
        var sr2 = go.GetComponent<SpriteRenderer>();
        float dur = 0.18f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            go.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.2f, p);
            if (sr2) sr2.color = new Color(1f, 0.85f, 0.85f, 1f - p);
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    IEnumerator AnimarRisco(SpriteRenderer sr2, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (sr2 == null) yield break;

        Color corBase = sr2.color;
        float dur     = 0.32f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (sr2 == null) yield break;
            float p = t / dur;
            // Aparece instantâneo, some com ease-out
            float alpha = p < 0.06f
                ? p / 0.06f
                : 1f - Mathf.Pow((p - 0.06f) / 0.94f, 0.55f);
            sr2.color = new Color(corBase.r, corBase.g, corBase.b, corBase.a * alpha);
            yield return null;
        }
        if (sr2 != null) Destroy(sr2.gameObject);
    }

    static Sprite _linhaCorteSprite;
    static Sprite GerarLinhaCorte()
    {
        if (_linhaCorteSprite != null) return _linhaCorteSprite;
        int w = 128, h = 16;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = w * 0.5f, cy = h * 0.5f;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float ax = 1f - Mathf.Pow(Mathf.Abs(x - cx) / cx, 1.4f);
            float ay = 1f - Mathf.Pow(Mathf.Abs(y - cy) / cy, 0.7f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, ax * ay));
        }
        tex.Apply();
        _linhaCorteSprite = Sprite.Create(tex, new Rect(0, 0, w, h),
            new Vector2(0.5f, 0.5f), w);
        return _linhaCorteSprite;
    }
}

// Partícula auto-gerenciada: não depende do ceifador estar vivo
class FumacaCeifadorFX : MonoBehaviour
{
    public void Iniciar(Vector2 vel, Color cor) => StartCoroutine(Mover(vel, cor));

    System.Collections.IEnumerator Mover(Vector2 vel, Color cor)
    {
        var sr = GetComponent<SpriteRenderer>();
        float vida = Random.Range(0.4f, 0.8f);
        for (float t = 0f; t < vida; t += Time.deltaTime)
        {
            vel *= Mathf.Pow(0.92f, Time.deltaTime * 60f);
            if (sr != null)
            {
                transform.position   += (Vector3)(vel * Time.deltaTime);
                transform.localScale *= 1f + Time.deltaTime * 1.5f;
                sr.color = new Color(cor.r, cor.g, cor.b, Mathf.Lerp(cor.a, 0f, t / vida));
            }
            yield return null;
        }
        Destroy(gameObject);
    }
}
