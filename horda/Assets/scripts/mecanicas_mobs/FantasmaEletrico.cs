using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Fantasma elemental Elétrico: movimento contínuo ondulante com instabilidade elétrica,
// paralisa o player ao tocar. Periodicamente dispara um raio de cadeia até o player.
// Ao morrer, descarga elétrica em área ao redor.
public class FantasmaEletrico : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidade      = 4f;
    public float amplitudeOnda   = 0.6f;
    public float frequenciaOnda  = 2.8f;

    [Header("Contato Base")]
    public float danoContato     = 11f;
    public float intervaloContato = 1.3f;

    [Header("Elemento: Elétrico")]
    public float duracaoParalisia = 0.9f;
    public float cooldownParalisia = 5f;

    [Header("Ataque: Raio de Cadeia")]
    public float danoRaio       = 30f;
    public float cooldownRaio   = 2.5f;
    public float alcanceRaio    = 18f;
    public float raioImpactoRaio = 1.2f;
    [Tooltip("Tempo de aviso (telegraph) antes do raio cair, dando tempo de desviar")]
    public float tempoTelegraphRaio = 0.5f;

    [Header("Morte: Descarga")]
    public float raioDescarga   = 3f;
    public float danoDescarga   = 18f;

    [Header("Brilho")]
    public Color corBrilho         = new Color(0.9f, 1f, 0.4f);
    public float intensidadeBrilho = 1.6f;
    public float raioInternoBrilho = 0.2f;
    public float raioExternoBrilho = 1.6f;

    Rigidbody2D    rb;
    SpriteRenderer sr;
    InimigoController inimigoCtrl;
    PlayerStats    player;
    Vector3        escalaBase;
    float          ondaFase;

    float proxParalisia;
    float proxRaio;
    float proxContato;

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

        escalaBase    = transform.localScale;
        ondaFase      = Random.Range(0f, Mathf.PI * 2f);
        proxParalisia = Random.Range(1f, 4f);
        proxRaio      = Random.Range(3f, 7f);

        player = PlayerStats.MaisProximo(transform.position);
        CriarLuz(transform, corBrilho, intensidadeBrilho, raioInternoBrilho, raioExternoBrilho);
        StartCoroutine(RastroEletrico());
        StartCoroutine(FlashPeriodicoEletrico());
        InimigoController.OnPreMorte += OnPreMorteHandler;
    }

    void OnDestroy() => InimigoController.OnPreMorte -= OnPreMorteHandler;

    void OnPreMorteHandler(InimigoController ic)
    {
        if (ic != inimigoCtrl) return;
        StartCoroutine(Descarga(transform.position));
    }

    bool Morto() => inimigoCtrl != null && inimigoCtrl.estaMorrendo;

    void Update()
    {
        if (Morto() || player == null) return;
        proxParalisia -= Time.deltaTime;
        proxRaio      -= Time.deltaTime;

        float dist = Vector2.Distance(transform.position, player.transform.position);
        if (proxRaio <= 0f && dist <= alcanceRaio)
        {
            proxRaio = cooldownRaio;
            StartCoroutine(RaioDeCadeia());
        }

        float t  = Time.time * 2.8f + ondaFase;
        float sx = 1f + Mathf.Sin(t)         * 0.08f;
        float sy = 1f + Mathf.Cos(t * 1.05f) * 0.11f;
        transform.localScale = new Vector3(escalaBase.x * sx, escalaBase.y * sy, escalaBase.z);
    }

    void FixedUpdate()
    {
        if (player == null || Morto()) { rb.linearVelocity = Vector2.zero; return; }
        Vector2 dir  = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
        Vector2 perp = new Vector2(-dir.y, dir.x);
        float   onda = Mathf.Sin(Time.time * frequenciaOnda + ondaFase) * amplitudeOnda;
        Vector2 vel  = (dir + perp * onda).normalized * velocidade;
        rb.linearVelocity = vel;
        if (Mathf.Abs(vel.x) > 0.05f) sr.flipX = vel.x < 0f;
    }

    void OnTriggerEnter2D(Collider2D other) => TentarAplicarEfeito(other);
    void OnTriggerStay2D(Collider2D other)  => TentarAplicarEfeito(other);

    void TentarAplicarEfeito(Collider2D other)
    {
        if (Morto() || !other.CompareTag("Player")) return;
        var ps = other.GetComponent<PlayerStats>();
        if (ps == null) return;

        if (Time.time >= proxContato)
        {
            ps.TakeDamage(danoContato);
            proxContato = Time.time + intervaloContato;
        }

        if (proxParalisia <= 0f)
        {
            ps.AplicarParalisiaPlayer(duracaoParalisia);
            proxParalisia = cooldownParalisia;
            StartCoroutine(BurstEletrico(transform.position));
        }
    }

    IEnumerator FlashPeriodicoEletrico()
    {
        while (!Morto())
        {
            yield return new WaitForSeconds(Random.Range(1.5f, 3.5f));
            if (Morto()) yield break;
            yield return StartCoroutine(FlashEletrico());
        }
    }

    IEnumerator FlashEletrico()
    {
        if (sr == null) yield break;
        Color orig = sr.color;
        sr.color = new Color(0.9f, 1f, 0.4f);
        yield return new WaitForSeconds(0.06f);
        if (!Morto() && sr != null) sr.color = orig;
    }

    // ─── Raio de cadeia: linha visual do ghost até o player ───────────────

    IEnumerator RaioDeCadeia()
    {
        if (player == null) yield break;

        SomSkill.Tocar(SomSkill.Tipo.FantasmaEletrico, transform.position, 0.5f);

        Vector2 origem  = transform.position;
        Vector2 destino = player.transform.position;

        // Telegraph: marca a área que será atingida e dá tempo do player desviar
        var marca  = new GameObject("AvisoRaio");
        marca.transform.position = destino;
        var srMarca = marca.AddComponent<SpriteRenderer>();
        srMarca.sprite       = GerarDisco(32, new Color(1f, 1f, 0.4f, 0.35f));
        srMarca.sortingOrder = 17;
        marca.transform.localScale = Vector3.one * raioImpactoRaio * 2f;
        Destroy(marca, tempoTelegraphRaio + 0.1f);

        float t = 0f;
        while (t < tempoTelegraphRaio)
        {
            t += Time.deltaTime;
            float pulso = Mathf.PingPong(t * 8f, 1f);
            var c = srMarca.color; c.a = Mathf.Lerp(0.2f, 0.55f, pulso); srMarca.color = c;
            yield return null;
        }
        Destroy(marca);

        // Impacto: aplica dano em área no ponto telegrafado
        var alvos = Physics2D.OverlapCircleAll(destino, raioImpactoRaio);
        foreach (var alvo in alvos)
        {
            if (!alvo.CompareTag("Player")) continue;
            var ps = alvo.GetComponent<PlayerStats>();
            if (ps != null) ps.TakeDamage(danoRaio);
        }

        var impacto  = new GameObject("ImpactoRaio");
        impacto.transform.position   = destino;
        var srImpacto = impacto.AddComponent<SpriteRenderer>();
        srImpacto.sprite       = GerarDisco(32, new Color(1f, 1f, 0.6f, 0.5f));
        srImpacto.sortingOrder = 17;
        impacto.transform.localScale = Vector3.one * raioImpactoRaio * 2f;
        impacto.AddComponent<EfeitoRunner>().Run(FadeOut(impacto, 0.35f));

        // Atualiza a origem para a posição atual do fantasma (ele se move durante o telegraph)
        origem = transform.position;

        const int SEGS = 8;
        var go = new GameObject("Raio");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = SEGS + 2;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 20;
        lr.startWidth    = lr.endWidth = 0.16f;
        lr.startColor    = lr.endColor = new Color(1f, 1f, 0.7f, 1f);

        CriarLuz(go.transform, new Color(0.9f, 1f, 0.3f), 2.5f, 0.1f, 1.2f);

        go.AddComponent<EfeitoRunner>().Run(AnimarRaioCadeia(go, lr, origem, destino, SEGS));
    }

    IEnumerator AnimarRaioCadeia(GameObject go, LineRenderer lr, Vector2 origem, Vector2 destino, int SEGS)
    {
        for (float t = 0f; t < 0.35f; t += Time.deltaTime)
        {
            lr.SetPosition(0, origem);
            for (int i = 1; i <= SEGS; i++)
            {
                float p = (float)i / (SEGS + 1);
                Vector2 mid = Vector2.Lerp(origem, destino, p);
                float desvio = Mathf.Sin(t * 40f + i * 1.3f) * (0.4f * (1f - Mathf.Abs(p - 0.5f) * 2f));
                Vector2 perp = new Vector2(-(destino - origem).y, (destino - origem).x).normalized;
                lr.SetPosition(i, mid + perp * desvio);
            }
            lr.SetPosition(SEGS + 1, destino);

            float progress = t / 0.35f;
            float alpha = progress < 0.3f ? progress / 0.3f : 1f - (progress - 0.3f) / 0.7f;
            lr.startColor = lr.endColor = new Color(1f, 1f, 0.7f, alpha);
            yield return null;
        }
        Destroy(go);
    }

    IEnumerator BurstEletrico(Vector2 pos)
    {
        for (int i = 0; i < 8; i++)
        {
            float ang = i * 45f * Mathf.Deg2Rad;
            var go  = new GameObject("Spark");
            var sr2 = go.AddComponent<SpriteRenderer>();
            sr2.sprite       = GerarDisco(8, new Color(0.9f, 1f, 0.4f, 0.9f));
            sr2.sortingOrder = 18;
            go.transform.position   = pos;
            go.transform.localScale = Vector3.one * 0.2f;
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            go.AddComponent<EfeitoRunner>().Run(Lancar(go, dir, Random.Range(2f, 4f), 0.3f));
        }
        yield break;
    }

    IEnumerator Descarga(Vector2 pos)
    {
        if (player != null && Vector2.Distance(player.transform.position, pos) <= raioDescarga)
        {
            player.TakeDamage(danoDescarga);
            player.AplicarParalisiaPlayer(duracaoParalisia * 1.5f);
        }

        for (int i = 0; i < 8; i++)
        {
            float ang  = i * 45f * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            RaioSolto(pos, pos + dir * raioDescarga);
        }
        yield break;
    }

    void RaioSolto(Vector2 origem, Vector2 destino)
    {
        var go = new GameObject("RaioSolto");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.positionCount = 4;
        lr.material      = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 18;
        lr.startWidth = lr.endWidth = 0.12f;
        lr.startColor = lr.endColor = new Color(1f, 1f, 0.7f, 1f);

        CriarLuz(go.transform, new Color(0.9f, 1f, 0.4f), 2f, 0.05f, 1f);

        go.AddComponent<EfeitoRunner>().Run(AnimarRaioSolto(go, lr, origem, destino));
    }

    IEnumerator AnimarRaioSolto(GameObject go, LineRenderer lr, Vector2 origem, Vector2 destino)
    {
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            lr.SetPosition(0, origem);
            for (int i = 1; i <= 2; i++)
            {
                float   p    = (float)i / 3f;
                Vector2 mid  = Vector2.Lerp(origem, destino, p);
                Vector2 perp = new Vector2(-(destino - origem).y, (destino - origem).x).normalized;
                float   dev  = Mathf.Sin(t * 50f + i * 2f) * 0.3f;
                lr.SetPosition(i, mid + perp * dev);
            }
            lr.SetPosition(3, destino);
            float alpha = 1f - t / 0.4f;
            lr.startColor = lr.endColor = new Color(1f, 1f, 0.7f, alpha);
            yield return null;
        }
        Destroy(go);
    }

    IEnumerator RastroEletrico()
    {
        while (!Morto())
        {
            var go  = new GameObject("EP");
            var sr2 = go.AddComponent<SpriteRenderer>();
            sr2.sprite       = GerarDisco(10, new Color(0.9f, 1f, 0.4f, 0.7f));
            sr2.sortingOrder = 7;
            float sz = Random.Range(0.12f, 0.28f);
            go.transform.position   = (Vector3)transform.position + (Vector3)Random.insideUnitCircle * 0.15f;
            go.transform.localScale = Vector3.one * sz;
            go.AddComponent<EfeitoRunner>().Run(FadeOut(go, 0.2f));
            yield return new WaitForSeconds(0.08f);
        }
    }

    IEnumerator Lancar(GameObject go, Vector2 dir, float spd, float vida)
    {
        var sr2 = go.GetComponent<SpriteRenderer>();
        Vector2 pos = go.transform.position;
        for (float t = 0f; t < vida && go != null; t += Time.deltaTime)
        {
            pos += dir * spd * Time.deltaTime;
            spd  = Mathf.Lerp(spd, 0f, Time.deltaTime * 5f);
            go.transform.position = pos;
            if (sr2 != null) { var c = sr2.color; c.a = 1f - t / vida; sr2.color = c; }
            yield return null;
        }
        if (go != null) Destroy(go);
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
