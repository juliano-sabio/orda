using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CeifadorEvento : MonoBehaviour
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

    private enum Estado { Wander, Perseguindo, Dashing }
    private Estado estado = Estado.Wander;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

void Start()
{
    if (controllerCeifador != null)
    {
        var anim = GetComponent<Animator>();
        if (anim != null) anim.runtimeAnimatorController = controllerCeifador;
    }

    var ps = FindFirstObjectByType<PlayerStats>();
    if (ps != null) player = ps.transform;

    AdicionarBrilhoVermelho();
    EscolherNovaDirecao();
    StartCoroutine(EfeitoSpawn());

    // Reduz o dano de contato do ceifador
    var danoComp = GetComponent<DanoInimigo>();
    if (danoComp != null) danoComp.dano *= 0.4f;

    var ic = GetComponent<InimigoController>();
    if (ic != null) ic.danoAtual *= 0.4f;
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
    StartCoroutine(AnimarParticula(sr2, vel));
}

IEnumerator AnimarParticula(SpriteRenderer sr2, Vector2 vel)
{
    Color cor = sr2.color;
    float vida = Random.Range(0.4f, 0.8f);
    for (float t = 0f; t < vida; t += Time.deltaTime)
    {
        vel *= Mathf.Pow(0.92f, Time.deltaTime * 60f);
        if (sr2 != null)
        {
            sr2.transform.position += (Vector3)(vel * Time.deltaTime);
            sr2.transform.localScale *= 1f + Time.deltaTime * 1.5f;
            sr2.color = new Color(cor.r, cor.g, cor.b, Mathf.Lerp(cor.a, 0f, t / vida));
        }
        yield return null;
    }
    if (sr2 != null) Destroy(sr2.gameObject);
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

    void Update()
    {
        if (player == null) return;

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

        estado    = Estado.Dashing;
        isDashing = true;
        dashDir   = ((Vector2)player.position - (Vector2)transform.position).normalized;

        yield return new WaitForSeconds(duracaoDash);

        isDashing  = false;
        proximoDash = Time.time + cooldownDash;
        estado     = Estado.Perseguindo;
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
        // Cancela dash ao bater em obstáculo
        if (isDashing && !col.gameObject.CompareTag("Player"))
        {
            StopAllCoroutines();
            isDashing   = false;
            proximoDash = Time.time + cooldownDash;
            estado      = Estado.Perseguindo;
        }
        else if (!isDashing && estado == Estado.Wander && !col.gameObject.CompareTag("Player"))
        {
            EscolherNovaDirecao();
        }
    }
}
