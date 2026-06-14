using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Fantasma elemental de Fogo: orbita o player a média distância e executa um ataque
// de carga — para, carrega por 0.8s, depois dá um dash veloz em linha reta através do
// player causando dano de fogo alto. Ao morrer, explode em brasas.
public class FantasmaFogo : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidade        = 4f;
    public float distanciaOrbit    = 5f;

    [Header("Contato Base")]
    public float danoContato    = 12f;
    public float intervaloContato = 1.2f;

    [Header("Ataque: Carga de Fogo")]
    public float danoCarga      = 40f;
    public float cooldownCarga  = 6f;
    public float velocidadeCarga = 32f;
    public float duracaoCarga   = 0.6f;

    [Header("Ataque: Projéteis Fantasmas")]
    public int   quantidadeProjeteisFantasmas = 5;
    public float danoProjetilFantasma       = 18f;
    public float velocidadeProjetilFantasma = 6f;
    public float cooldownProjeteisFantasmas = 5f;
    public float distanciaTiroFantasmas     = 9f;
    public float anguloSpreadFantasmas      = 50f;
    public float raioImpactoProjetilFantasma = 0.5f;
    public float vidaMaximaProjetilFantasma  = 4f;

    [Header("Queimação do Projétil")]
    public float danoQueimaduraPorTick = 4f;
    public float intervaloQueimadura   = 1f;
    public float duracaoQueimadura     = 4f;

    [Header("Morte: Explosão")]
    public float raioExplosao   = 2.5f;
    public float danoExplosao   = 20f;

    [Header("Brilho")]
    public Color corBrilho         = new Color(1f, 0.2f, 0.1f);
    public float intensidadeBrilho = 1.6f;
    public float raioInternoBrilho = 0.2f;
    public float raioExternoBrilho = 1.8f;

    Rigidbody2D    rb;
    SpriteRenderer sr;
    InimigoController inimigoCtrl;
    PlayerStats    player;
    Vector3        escalaBase;
    float          ondaFase;

    float proxCarga;
    float proxContato;
    float proxProjeteisFantasmas;
    bool  atacando;
    Vector2 direcaoMovimento;
    readonly List<GameObject> projeteisAtivos = new List<GameObject>();
    GameObject anelCargaAtivo;

    void Start()
    {
        rb          = GetComponent<Rigidbody2D>();
        sr          = GetComponent<SpriteRenderer>();
        inimigoCtrl = GetComponent<InimigoController>();

        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        rb.gravityScale = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        if (inimigoCtrl != null) inimigoCtrl.danoAtual = danoContato;

        escalaBase = transform.localScale;
        ondaFase   = Random.Range(0f, Mathf.PI * 2f);
        proxCarga  = Random.Range(3f, 7f);
        proxProjeteisFantasmas = Random.Range(1f, cooldownProjeteisFantasmas);
        player     = FindFirstObjectByType<PlayerStats>();

        CriarLuz(transform, corBrilho, intensidadeBrilho, raioInternoBrilho, raioExternoBrilho);

        StartCoroutine(RastroFogo());
        InimigoController.OnPreMorte += OnPreMorteHandler;
    }

    void OnDestroy() => InimigoController.OnPreMorte -= OnPreMorteHandler;

    void OnPreMorteHandler(InimigoController ic)
    {
        if (ic != inimigoCtrl) return;

        // Destrói o anel de carregamento se o fantasma morrer durante a conjuração
        if (anelCargaAtivo != null) { Destroy(anelCargaAtivo); anelCargaAtivo = null; }

        // Remove projéteis ainda em voo para não deixarem rastro de brasas após a morte
        foreach (var p in projeteisAtivos) if (p != null) Destroy(p);
        projeteisAtivos.Clear();

        FxRunner.Instance.StartCoroutine(ExplosaoMorte(transform.position));
    }

    bool Morto() => inimigoCtrl != null && inimigoCtrl.estaMorrendo;

    void Update()
    {
        if (Morto() || player == null || atacando) return;

        proxCarga -= Time.deltaTime;
        proxProjeteisFantasmas -= Time.deltaTime;

        float dist = Vector2.Distance(transform.position, player.transform.position);
        Vector2 dirParaPlayer = dist > 0.1f
            ? ((Vector2)player.transform.position - (Vector2)transform.position) / dist
            : Vector2.up;

        // Orbita: se longe, aproxima; se perto demais, recua; na distância ideal, circula
        if (dist > distanciaOrbit + 1.5f)
            direcaoMovimento = Vector2.Lerp(direcaoMovimento, dirParaPlayer, Time.deltaTime * 7f);
        else if (dist < distanciaOrbit - 1.5f)
            direcaoMovimento = Vector2.Lerp(direcaoMovimento, -dirParaPlayer, Time.deltaTime * 7f);
        else
        {
            Vector2 lateral = new Vector2(-dirParaPlayer.y, dirParaPlayer.x);
            direcaoMovimento = Vector2.Lerp(direcaoMovimento, lateral, Time.deltaTime * 7f);
        }

        if (proxCarga <= 0f && dist <= distanciaOrbit + 4f)
        {
            proxCarga = cooldownCarga;
            StartCoroutine(CargaFogo());
        }

        if (proxProjeteisFantasmas <= 0f && dist <= distanciaTiroFantasmas)
        {
            proxProjeteisFantasmas = cooldownProjeteisFantasmas;
            StartCoroutine(DispararProjeteisFantasmas(dirParaPlayer));
        }

        float t  = Time.time * 2.6f + ondaFase;
        float sx = 1f + Mathf.Sin(t)         * 0.08f;
        float sy = 1f + Mathf.Cos(t * 1.1f)  * 0.10f;
        transform.localScale = new Vector3(escalaBase.x * sx, escalaBase.y * sy, escalaBase.z);
    }

    void FixedUpdate()
    {
        if (player == null || Morto() || atacando) { if (!atacando) rb.linearVelocity = Vector2.zero; return; }
        rb.linearVelocity = direcaoMovimento.normalized * velocidade;
        if (direcaoMovimento.sqrMagnitude > 0.001f && Mathf.Abs(direcaoMovimento.x) > 0.05f)
            sr.flipX = direcaoMovimento.x > 0f;
    }

    void OnTriggerEnter2D(Collider2D other) => TentarAplicarDano(other);
    void OnTriggerStay2D(Collider2D other)  => TentarAplicarDano(other);

    void TentarAplicarDano(Collider2D other)
    {
        if (Morto() || !other.CompareTag("Player") || Time.time < proxContato) return;
        var ps = other.GetComponent<PlayerStats>();
        if (ps == null) return;
        ps.TakeDamage(danoContato);
        proxContato = Time.time + intervaloContato;
    }

    IEnumerator CargaFogo()
    {
        atacando = true;
        rb.linearVelocity = Vector2.zero;

        Vector2 destino = player != null ? (Vector2)player.transform.position : (Vector2)transform.position;

        yield return StartCoroutine(EfeitoCarregamento(0.8f, destino));

        Vector2 dir = (destino - (Vector2)transform.position).normalized;
        bool    acertou = false;

        for (float t = 0f; t < duracaoCarga; t += Time.deltaTime)
        {
            rb.linearVelocity = dir * velocidadeCarga;
            SpawnBrasa(transform.position);

            if (!acertou && player != null &&
                Vector2.Distance(transform.position, player.transform.position) < 1.2f)
            {
                player.TakeDamage(danoCarga);
                StartCoroutine(FlashExplosao(transform.position, 1.4f));
                acertou = true;
            }
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        atacando = false;
    }

    IEnumerator DispararProjeteisFantasmas(Vector2 dirBase)
    {
        int qtd = Mathf.Max(1, quantidadeProjeteisFantasmas);
        for (int i = 0; i < qtd; i++)
        {
            if (Morto()) yield break;

            float t   = qtd > 1 ? (float)i / (qtd - 1) : 0.5f;
            float ang = Mathf.Lerp(-anguloSpreadFantasmas, anguloSpreadFantasmas, t) * Mathf.Deg2Rad;
            float cos = Mathf.Cos(ang), sen = Mathf.Sin(ang);
            Vector2 dir = new Vector2(
                dirBase.x * cos - dirBase.y * sen,
                dirBase.x * sen + dirBase.y * cos);

            StartCoroutine(ProjetilFantasma(transform.position, dir));
            yield return new WaitForSeconds(0.08f);
        }
    }

    IEnumerator ProjetilFantasma(Vector2 origem, Vector2 dir)
    {
        var go = new GameObject("ProjetilFantasma");
        go.transform.position = origem;
        var sr2 = go.AddComponent<SpriteRenderer>();
        sr2.sprite       = GerarDisco(10, new Color(1f, 0.35f, 0.05f, 0.95f));
        sr2.sortingOrder = 8;
        go.transform.localScale = Vector3.one * 0.35f;
        CriarLuz(go.transform, new Color(1f, 0.3f, 0.05f), 1.2f, 0.05f, 0.6f);
        var slow = go.AddComponent<ProjetilFantasmaSlow>();
        var colSlow = go.AddComponent<CircleCollider2D>();
        colSlow.isTrigger = true;
        colSlow.radius    = 0.15f;
        projeteisAtivos.Add(go);

        for (float t = 0f; t < vidaMaximaProjetilFantasma; t += Time.deltaTime)
        {
            if (go == null) yield break;
            go.transform.position += (Vector3)(dir * velocidadeProjetilFantasma * slow.fatorVelocidade * Time.deltaTime);
            SpawnBrasa(go.transform.position);

            var alvo = Physics2D.OverlapCircle(go.transform.position, raioImpactoProjetilFantasma);
            if (alvo != null && alvo.CompareTag("Player"))
            {
                var ps = alvo.GetComponent<PlayerStats>();
                if (ps != null)
                {
                    ps.TakeDamage(danoProjetilFantasma);
                    ps.AplicarQueimaduraPlayer(danoQueimaduraPorTick, intervaloQueimadura, duracaoQueimadura);
                }
                projeteisAtivos.Remove(go);
                StartCoroutine(FadeOut(go, 0.15f));
                yield break;
            }
            yield return null;
        }
        projeteisAtivos.Remove(go);
        if (go != null) Destroy(go);
    }

    IEnumerator EfeitoCarregamento(float dur, Vector2 alvo)
    {
        var go = new GameObject("AnelCarga");
        go.transform.position = transform.position;
        anelCargaAtivo = go;
        var lr = CriarAnel(go, 1.5f, new Color(1f, 0.4f, 0f, 0.9f), 0.14f);

        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null) yield break;
            go.transform.position = transform.position;
            float p = t / dur;
            go.transform.localScale = Vector3.one * (1f + Mathf.Sin(p * Mathf.PI * 6f) * 0.08f);
            AlphaLR(lr, Mathf.Sin(p * Mathf.PI) * 0.95f);
            SpawnBrasa(transform.position);
            yield return null;
        }
        anelCargaAtivo = null;
        if (go != null) Destroy(go);
    }

    IEnumerator FlashExplosao(Vector2 pos, float raio)
    {
        var go  = new GameObject("Flash");
        var sr2 = go.AddComponent<SpriteRenderer>();
        sr2.sprite       = GerarDisco(64, new Color(1f, 0.55f, 0.1f, 0.9f));
        sr2.sortingOrder = 17;
        go.transform.position   = pos;
        go.transform.localScale = Vector3.one * raio;
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p = t / 0.3f;
            go.transform.localScale = Vector3.one * Mathf.Lerp(raio, raio * 0.2f, p);
            var c = sr2.color; c.a = 1f - p; sr2.color = c;
            yield return null;
        }
        Destroy(go);
    }

    IEnumerator ExplosaoMorte(Vector2 pos)
    {
        if (player != null && Vector2.Distance(player.transform.position, pos) <= raioExplosao)
            player.TakeDamage(danoExplosao);

        FxRunner.Instance.StartCoroutine(FlashExplosao(pos, raioExplosao));

        for (int i = 0; i < 14; i++)
        {
            float ang = i * (360f / 14f) * Mathf.Deg2Rad;
            var go  = new GameObject("Brasa");
            var sr2 = go.AddComponent<SpriteRenderer>();
            sr2.sprite       = GerarDisco(12, new Color(1f, Random.Range(0.2f, 0.6f), 0f));
            sr2.sortingOrder = 15;
            go.transform.position   = pos;
            go.transform.localScale = Vector3.one * Random.Range(0.25f, 0.55f);
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            FxRunner.Instance.StartCoroutine(Lancar(go, dir, Random.Range(3f, 7f), Random.Range(0.5f, 0.9f)));
        }
        yield break;
    }

    IEnumerator Lancar(GameObject go, Vector2 dir, float spd, float vida)
    {
        var sr2 = go.GetComponent<SpriteRenderer>();
        Vector2 pos = go.transform.position;
        for (float t = 0f; t < vida && go != null; t += Time.deltaTime)
        {
            pos += dir * spd * Time.deltaTime;
            spd *= Mathf.Pow(0.88f, Time.deltaTime * 60f);
            go.transform.position = pos;
            if (sr2 != null) { var c = sr2.color; c.a = 1f - t / vida; sr2.color = c; }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    IEnumerator RastroFogo()
    {
        while (!Morto())
        {
            SpawnBrasa(transform.position);
            yield return new WaitForSeconds(0.08f);
        }
    }

    void SpawnBrasa(Vector2 pos)
    {
        var go  = new GameObject("Brasa");
        var sr2 = go.AddComponent<SpriteRenderer>();
        sr2.sprite       = GerarDisco(10, new Color(1f, Random.Range(0.3f, 0.6f), 0f, 0.8f));
        sr2.sortingOrder = 7;
        go.transform.position   = (Vector3)pos + (Vector3)Random.insideUnitCircle * 0.12f;
        go.transform.localScale = Vector3.one * Random.Range(0.15f, 0.35f);
        FxRunner.Instance.StartCoroutine(FadeOut(go, Random.Range(0.2f, 0.4f)));
    }

    IEnumerator FadeOut(GameObject go, float vida)
    {
        if (go == null) yield break;
        var sr2 = go.GetComponent<SpriteRenderer>();
        for (float t = 0f; t < vida; t += Time.deltaTime)
        {
            if (go == null || sr2 == null) yield break;
            var c = sr2.color; c.a = 1f - t / vida; sr2.color = c;
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    LineRenderer CriarAnel(GameObject parent, float raio, Color cor, float larg)
    {
        const int S = 24;
        var ch = new GameObject("Anel"); ch.transform.SetParent(parent.transform, false);
        var lr = ch.AddComponent<LineRenderer>();
        lr.useWorldSpace = false; lr.loop = true; lr.positionCount = S;
        lr.material      = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 12;
        lr.startWidth = lr.endWidth = larg; lr.startColor = lr.endColor = cor;
        for (int i = 0; i < S; i++)
        {
            float a = (360f / S) * i * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * raio, Mathf.Sin(a) * raio));
        }
        return lr;
    }

    void AlphaLR(LineRenderer lr, float a)
    {
        if (lr == null) return;
        Color c = lr.startColor; c.a = a; lr.startColor = lr.endColor = c;
    }

    static void CriarLuz(Transform parent, Color cor, float intensidade, float raioInterno, float raioExterno)
    {
        var go = new GameObject("brilho");
        go.transform.SetParent(parent, false);
        var light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = cor;
        light.intensity = intensidade;
        light.pointLightInnerRadius = raioInterno;
        light.pointLightOuterRadius = raioExterno;
        light.blendStyleIndex = 0;
    }

    static Sprite GerarDisco(int sz, Color cor)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cx));
            tex.SetPixel(x, y, new Color(cor.r, cor.g, cor.b, d < cx ? cor.a : 0f));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }
}
