using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Fantasma elemental de Gelo (variante Atiradora): mantém distância do player,
// orbitando, e dispara projéteis de gelo que causam dano e aplicam lentidão.
// Periodicamente também cria uma zona de gelo no player. Ao morrer, cria uma
// zona de gelo maior e explode em cristais.
public class FantasmaGelo : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidade          = 3.5f;
    public float distanciaIdeal      = 6f;
    public float toleranciaDistancia = 1.5f;
    public float amplitudeOnda  = 0.5f;
    public float frequenciaOnda = 2.0f;

    [Header("Contato Base")]
    public float danoContato    = 10f;
    public float intervaloContato = 1.5f;

    [Header("Ataque: Projétil de Gelo")]
    public Sprite spriteProjetil;
    public float danoProjetil        = 15f;
    public float velocidadeProjetil  = 12f;
    public float cooldownTiro        = 1.2f;
    public float distanciaTiro       = 13f;
    public float raioImpactoProjetil = 1.2f;
    public float vidaMaximaProjetil  = 4f;

    [Header("Elemento: Gelo")]
    public float reducaoSlow    = 0.5f;
    public float duracaoSlow    = 3.5f;
    public float cooldownSlow   = 4f;

    [Header("Ataque: Zona de Gelo")]
    public float raioZonaGelo   = 2.8f;
    public float duracaoZonaGelo = 3f;
    public float danoZonaGelo   = 8f;

    [Header("Morte")]
    public float raioGeloMorte  = 3.5f;
    public float duracaoGeloMorte = 4f;

    Rigidbody2D    rb;
    SpriteRenderer sr;
    InimigoController inimigoCtrl;
    PlayerStats    player;
    Vector3        escalaBase;
    float          ondaFase;

    float   proxSlow;
    float   proxContato;
    float   proxTiro;
    Vector2 direcaoMovimento;

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
        proxSlow   = Random.Range(1f, 3f);
        proxTiro   = Random.Range(0.5f, cooldownTiro);

        player = PlayerStats.MaisProximo(transform.position);
        CriarLuz(transform, new Color(0.6f, 0.9f, 1f), 1.3f, 0.2f, 2f);
        StartCoroutine(RastroGelo());
        InimigoController.OnPreMorte += OnPreMorteHandler;
    }

    void OnDestroy() => InimigoController.OnPreMorte -= OnPreMorteHandler;

    void OnPreMorteHandler(InimigoController ic)
    {
        if (ic != inimigoCtrl) return;
        FxRunner.Instance.StartCoroutine(ZonaGelo(transform.position, raioGeloMorte, duracaoGeloMorte));
        FxRunner.Instance.StartCoroutine(ExplodirCristais(transform.position));
    }

    bool Morto() => inimigoCtrl != null && inimigoCtrl.estaMorrendo;

    void Update()
    {
        if (Morto() || player == null) return;
        proxSlow -= Time.deltaTime;
        proxTiro -= Time.deltaTime;

        float dist = Vector2.Distance(transform.position, player.transform.position);
        Vector2 dirParaPlayer = dist > 0.1f
            ? ((Vector2)player.transform.position - (Vector2)transform.position) / dist
            : Vector2.up;

        // Mantém distância: aproxima se longe, foge se perto, circula na distância ideal
        if (dist > distanciaIdeal + toleranciaDistancia)
            direcaoMovimento = Vector2.Lerp(direcaoMovimento, dirParaPlayer, Time.deltaTime * 6f);
        else if (dist < distanciaIdeal - toleranciaDistancia)
            direcaoMovimento = Vector2.Lerp(direcaoMovimento, -dirParaPlayer, Time.deltaTime * 6f);
        else
        {
            Vector2 lateral = new Vector2(-dirParaPlayer.y, dirParaPlayer.x);
            float onda = Mathf.Sin(Time.time * frequenciaOnda + ondaFase) * amplitudeOnda;
            direcaoMovimento = Vector2.Lerp(direcaoMovimento, lateral + dirParaPlayer * onda, Time.deltaTime * 6f);
        }

        if (proxTiro <= 0f && dist <= distanciaTiro)
        {
            proxTiro = cooldownTiro;
            SomSkill.Tocar(SomSkill.Tipo.FantasmaGelo, transform.position, 0.45f);
            FxRunner.Instance.StartCoroutine(ProjetilGelo(transform.position, dirParaPlayer));
        }

        // Deformação flutuante de fantasma
        float t  = Time.time * 2.2f + ondaFase;
        float sx = 1f + Mathf.Sin(t)         * 0.07f;
        float sy = 1f + Mathf.Cos(t * 1.2f)  * 0.10f;
        transform.localScale = new Vector3(escalaBase.x * sx, escalaBase.y * sy, escalaBase.z);
    }

    void FixedUpdate()
    {
        if (player == null || Morto()) { rb.linearVelocity = Vector2.zero; return; }
        rb.linearVelocity = direcaoMovimento.normalized * velocidade;
        if (Mathf.Abs(direcaoMovimento.x) > 0.05f) sr.flipX = direcaoMovimento.x > 0f;
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

        if (proxSlow <= 0f)
        {
            ps.AplicarSlow(reducaoSlow, duracaoSlow);
            proxSlow = cooldownSlow;
            StartCoroutine(FlashGelo());
        }
    }

    IEnumerator FlashGelo()
    {
        if (sr == null) yield break;
        Color orig = sr.color;
        sr.color = new Color(0.6f, 0.95f, 1f);
        yield return new WaitForSeconds(0.2f);
        if (!Morto() && sr != null) sr.color = orig;
    }

    IEnumerator ProjetilGelo(Vector2 origem, Vector2 dir)
    {
        var go = new GameObject("ProjetilGelo");
        go.transform.position = origem;
        var sr2 = go.AddComponent<SpriteRenderer>();
        sr2.sprite = spriteProjetil != null ? spriteProjetil : GerarCristal(10, new Color(0.6f, 0.9f, 1f));
        sr2.sortingOrder = 8;

        float angulo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        go.transform.rotation   = Quaternion.Euler(0f, 0f, angulo);
        go.transform.localScale = Vector3.one * 6f;
        CriarLuz(go.transform, new Color(0.6f, 0.9f, 1f), 1.4f, 0.05f, 1.2f);
        Destroy(go, vidaMaximaProjetil + 1f); // segurança: garante remoção mesmo se a corrotina for interrompida

        // Rigidbody2D + Collider2D: permite que a ultimate "Domo" detecte e desacelere o projétil
        var rbProjetil = go.AddComponent<Rigidbody2D>();
        rbProjetil.gravityScale = 0f;
        rbProjetil.constraints  = RigidbodyConstraints2D.FreezeRotation;
        rbProjetil.linearVelocity = dir * velocidadeProjetil;

        var colProjetil = go.AddComponent<CircleCollider2D>();
        colProjetil.isTrigger = true;
        colProjetil.radius    = raioImpactoProjetil / go.transform.localScale.x;

        float proxRastro = 0f;
        for (float t = 0f; t < vidaMaximaProjetil; t += Time.deltaTime)
        {
            if (go == null) yield break;
            Vector2 posAnterior = go.transform.position;

            proxRastro -= Time.deltaTime;
            if (proxRastro <= 0f)
            {
                proxRastro = 0.05f;
                SpawnParticula(go.transform.position, new Color(0.6f, 0.9f, 1f, 0.5f));
            }

            if (player != null)
            {
                Vector2 posJogador = player.transform.position;
                float distSegmento = DistanciaPontoSegmento(posJogador, posAnterior, go.transform.position);
                if (distSegmento <= raioImpactoProjetil)
                {
                    player.TakeDamage(danoProjetil);
                    player.AplicarSlow(reducaoSlow, duracaoSlow);
                    FxRunner.Instance.StartCoroutine(ZonaGelo(player.transform.position, raioZonaGelo, duracaoZonaGelo));
                    FxRunner.Instance.StartCoroutine(FadeOut(go, 0.15f));
                    yield break;
                }
            }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    static float DistanciaPontoSegmento(Vector2 ponto, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float comprimentoQuadrado = ab.sqrMagnitude;
        if (comprimentoQuadrado < 0.0001f) return Vector2.Distance(ponto, a);
        float t = Mathf.Clamp01(Vector2.Dot(ponto - a, ab) / comprimentoQuadrado);
        Vector2 projecao = a + ab * t;
        return Vector2.Distance(ponto, projecao);
    }

    IEnumerator ZonaGelo(Vector2 pos, float raio, float duracao)
    {
        var root = new GameObject("ZonaGelo");
        root.transform.position = pos;
        Destroy(root, duracao + 0.5f);

        const float alphaBase = 0.35f;

        // Disco e anel ficam num filho escalado, sem afetar os cristais orbitantes
        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);

        var sr2 = visual.AddComponent<SpriteRenderer>();
        sr2.sprite       = GerarDisco(64, new Color(0.55f, 0.9f, 1f, alphaBase));
        sr2.sortingOrder = 5;

        // Anel de borda mais brilhante
        var anel = new GameObject("Anel");
        anel.transform.SetParent(visual.transform, false);
        var anelSr = anel.AddComponent<SpriteRenderer>();
        anelSr.sprite       = GerarAnel(64, new Color(0.75f, 0.97f, 1f, 0.85f));
        anelSr.sortingOrder = 6;

        CriarLuz(root.transform, new Color(0.6f, 0.9f, 1f), 1.2f, 0.1f, raio);

        // Cristais orbitando
        var cristais = new Transform[8];
        for (int i = 0; i < cristais.Length; i++)
        {
            var c = new GameObject("Cristal");
            c.transform.SetParent(root.transform, false);
            var csr = c.AddComponent<SpriteRenderer>();
            csr.sprite       = GerarCristal(14, new Color(0.6f, 0.92f, 1f));
            csr.sortingOrder = 9;
            c.transform.localScale = Vector3.one * Random.Range(0.45f, 0.75f);
            cristais[i] = c.transform;
        }

        float rot = 0f;
        float tick = 0f;
        float entrada = 0.35f;

        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            if (root == null) yield break;

            // Animação de entrada (cresce e pulsa levemente)
            float crescer = Mathf.Clamp01(t / entrada);
            float pulso   = 1f + Mathf.Sin(Time.time * 4f) * 0.03f;
            visual.transform.localScale = Vector3.one * raio * 2f * crescer * pulso;

            rot += Time.deltaTime * 35f;
            for (int i = 0; i < cristais.Length; i++)
            {
                if (cristais[i] == null) continue;
                float ang = (360f / cristais.Length * i + rot) * Mathf.Deg2Rad;
                cristais[i].localPosition = new Vector3(Mathf.Cos(ang) * raio * crescer, Mathf.Sin(ang) * raio * crescer);
            }

            tick += Time.deltaTime;
            if (tick >= 0.8f && player != null)
            {
                tick = 0f;
                if (Vector2.Distance(player.transform.position, pos) <= raio)
                {
                    player.TakeDamage(danoZonaGelo);
                    player.AplicarSlow(reducaoSlow, duracaoSlow);
                }
            }

            float fade = t > duracao - 0.7f ? (duracao - t) / 0.7f : 1f;
            fade *= crescer;
            var c2 = sr2.color; c2.a = alphaBase * fade; sr2.color = c2;
            var c3 = anelSr.color; c3.a = 0.85f * fade; anelSr.color = c3;
            yield return null;
        }
        if (root != null) Destroy(root);
    }

    IEnumerator ExplodirCristais(Vector2 pos)
    {
        for (int i = 0; i < 12; i++)
        {
            float ang   = i * (360f / 12f) * Mathf.Deg2Rad;
            var go      = new GameObject("CristalMorte");
            go.transform.position   = pos;
            go.transform.localScale = Vector3.one * Random.Range(0.5f, 1.2f);
            var sr2 = go.AddComponent<SpriteRenderer>();
            sr2.sprite       = GerarCristal(16, new Color(0.6f + Random.Range(0f, 0.2f), 0.9f, 1f));
            sr2.sortingOrder = 14;
            Vector2 dir  = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            float   spd  = Random.Range(3f, 7f);
            Destroy(go, 2f); // segurança: garante remoção mesmo se a corrotina for interrompida
            FxRunner.Instance.StartCoroutine(LancarCristal(go, dir, spd));
        }
        yield break;
    }

    IEnumerator LancarCristal(GameObject go, Vector2 dir, float spd)
    {
        var sr2 = go.GetComponent<SpriteRenderer>();
        Vector2 pos   = go.transform.position;
        float   vida  = Random.Range(0.6f, 1.2f);

        for (float t = 0f; t < vida && go != null; t += Time.deltaTime)
        {
            pos += dir * spd * Time.deltaTime;
            spd  = Mathf.Lerp(spd, 0f, Time.deltaTime * 3f);
            go.transform.position = pos;
            go.transform.Rotate(0f, 0f, spd * 6f * Time.deltaTime);
            if (sr2 != null) { var c = sr2.color; c.a = 1f - t / vida; sr2.color = c; }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    IEnumerator RastroGelo()
    {
        while (!Morto())
        {
            var go  = new GameObject("GP");
            var sr2 = go.AddComponent<SpriteRenderer>();
            sr2.sprite       = GerarCristal(10, new Color(0.6f, 0.9f, 1f, 0.55f));
            sr2.sortingOrder = 7;
            float sz = Random.Range(0.15f, 0.3f);
            go.transform.position   = (Vector3)transform.position + (Vector3)Random.insideUnitCircle * 0.2f;
            go.transform.localScale = Vector3.one * sz;
            Destroy(go, 1.5f); // segurança: garante remoção mesmo se a corrotina for interrompida
            FxRunner.Instance.StartCoroutine(FadeOut(go, Random.Range(0.35f, 0.7f)));
            yield return new WaitForSeconds(0.12f);
        }
    }

    void SpawnParticula(Vector2 pos, Color cor)
    {
        var go  = new GameObject("GP2");
        var sr2 = go.AddComponent<SpriteRenderer>();
        sr2.sprite       = GerarCristal(8, cor);
        sr2.sortingOrder = 7;
        float sz = Random.Range(0.1f, 0.2f);
        go.transform.position   = pos;
        go.transform.localScale = Vector3.one * sz;
        Destroy(go, 1f); // segurança: garante remoção mesmo se a corrotina for interrompida
        FxRunner.Instance.StartCoroutine(FadeOut(go, Random.Range(0.2f, 0.35f)));
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

    static Sprite GerarAnel(int sz, Color cor)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        float cx = sz * 0.5f;
        const float espessura = 0.08f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cx)) / cx;
            float borda = 1f - Mathf.Abs(d - (1f - espessura)) / espessura;
            float a = Mathf.Clamp01(borda);
            tex.SetPixel(x, y, new Color(cor.r, cor.g, cor.b, a * cor.a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
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

    static Sprite GerarCristal(int sz, Color cor)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float nx = (x - cx) / cx;
            float ny = (y - cx) / cx;
            float d  = Mathf.Abs(nx) + Mathf.Abs(ny);
            float a  = Mathf.Clamp01(1f - d / 0.9f);
            tex.SetPixel(x, y, new Color(cor.r, cor.g, cor.b, a * cor.a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }
}
