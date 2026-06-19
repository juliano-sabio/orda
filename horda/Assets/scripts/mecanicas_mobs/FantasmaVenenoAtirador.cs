using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Fantasma elemental de Veneno (variante Atiradora): mantém distância do player,
// orbitando, e dispara projéteis tóxicos que causam dano e aplicam envenenamento.
// Ao morrer, libera uma nuvem de veneno como o fantasma_veneno comum.
public class FantasmaVenenoAtirador : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidade          = 2.6f;
    public float distanciaIdeal      = 6f;
    public float toleranciaDistancia = 1.5f;

    [Header("Contato Base")]
    public float danoContato      = 8f;
    public float intervaloContato = 1.2f;

    [Header("Ataque: Projétil de Veneno")]
    public float danoProjetil       = 12f;
    public float velocidadeProjetil = 7f;
    public float cooldownTiro       = 2.5f;
    public float distanciaTiro      = 9f;
    public float raioImpactoProjetil = 0.5f;
    public float vidaMaximaProjetil = 4f;

    [Header("Elemento: Veneno")]
    public float danoVenenoTick = 4f;
    public float duracaoVeneno  = 6f;

    [Header("Morte: Nuvem de Veneno")]
    public float raioNuvemMorte         = 2.8f;
    public float duracaoNuvemMorte      = 5f;
    public float danoNuvemMortePorTick  = 3f;

    [Header("Brilho")]
    public Color corBrilho      = new Color(0.78f, 0.55f, 1f);
    public float intensidadeBrilho = 1.5f;
    public float raioInternoBrilho = 0.2f;
    public float raioExternoBrilho = 1.8f;

    Rigidbody2D    rb;
    SpriteRenderer sr;
    InimigoController inimigoCtrl;
    PlayerStats    player;
    Vector3        escalaBase;
    float          ondaFase;

    float   proxTiro;
    float   proxContato;
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
        proxTiro   = Random.Range(0.5f, cooldownTiro);

        player = PlayerStats.MaisProximo(transform.position);
        InimigoController.OnPreMorte += OnPreMorteHandler;

        CriarLuz(transform, corBrilho, intensidadeBrilho, raioInternoBrilho, raioExternoBrilho);
    }

    void OnDestroy() => InimigoController.OnPreMorte -= OnPreMorteHandler;

    void OnPreMorteHandler(InimigoController ic)
    {
        if (ic != inimigoCtrl) return;
        FxRunner.Instance.StartCoroutine(NuvemDeVeneno(transform.position, raioNuvemMorte, duracaoNuvemMorte, danoNuvemMortePorTick));
    }

    bool Morto() => inimigoCtrl != null && inimigoCtrl.estaMorrendo;

    void Update()
    {
        if (Morto() || player == null) return;
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
            direcaoMovimento = Vector2.Lerp(direcaoMovimento, lateral, Time.deltaTime * 6f);
        }

        if (proxTiro <= 0f && dist <= distanciaTiro)
        {
            proxTiro = cooldownTiro;
            FxRunner.Instance.StartCoroutine(ProjetilVeneno(transform.position, dirParaPlayer));
        }

        // Deformação flutuante de fantasma
        float t  = Time.time * 2.4f + ondaFase;
        float sx = 1f + Mathf.Sin(t)         * 0.07f;
        float sy = 1f + Mathf.Cos(t * 1.15f) * 0.10f;
        transform.localScale = new Vector3(escalaBase.x * sx, escalaBase.y * sy, escalaBase.z);
    }

    void FixedUpdate()
    {
        if (player == null || Morto()) { rb.linearVelocity = Vector2.zero; return; }
        rb.linearVelocity = direcaoMovimento.normalized * velocidade;
        if (Mathf.Abs(direcaoMovimento.x) > 0.05f) sr.flipX = direcaoMovimento.x > 0f;
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

    IEnumerator ProjetilVeneno(Vector2 origem, Vector2 dir)
    {
        var go = new GameObject("ProjetilVeneno");
        go.transform.position = origem;
        var sr2 = go.AddComponent<SpriteRenderer>();
        sr2.sprite       = GerarDisco(10, new Color(0.55f, 0.95f, 0.35f, 0.95f));
        sr2.sortingOrder = 8;
        go.transform.localScale = Vector3.one * 0.35f;
        CriarLuz(go.transform, new Color(0.4f, 1f, 0.3f), 1.2f, 0.05f, 0.6f);
        var slow = go.AddComponent<ProjetilFantasmaSlow>();
        var colSlow = go.AddComponent<CircleCollider2D>();
        colSlow.isTrigger = true;
        colSlow.radius    = 0.15f;
        Destroy(go, vidaMaximaProjetil + 1f);

        for (float t = 0f; t < vidaMaximaProjetil; t += Time.deltaTime)
        {
            if (go == null) yield break;
            go.transform.position += (Vector3)(dir * velocidadeProjetil * slow.fatorVelocidade * Time.deltaTime);
            SpawnParticula(go.transform.position, new Color(0.3f, 1f, 0.4f, 0.5f));

            if (player != null &&
                Vector2.Distance(go.transform.position, player.transform.position) <= raioImpactoProjetil)
            {
                player.TakeDamage(danoProjetil);
                player.AplicarVenenoPlayer(danoVenenoTick, 1f, duracaoVeneno);
                FxRunner.Instance.StartCoroutine(FadeOut(go, 0.15f));
                yield break;
            }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    IEnumerator NuvemDeVeneno(Vector2 pos, float raio, float duracao, float danoPorTick)
    {
        var root = new GameObject("NuvemVeneno");
        root.transform.position = pos;
        Destroy(root, duracao + 0.5f);

        var sr2 = root.AddComponent<SpriteRenderer>();
        sr2.sprite       = GerarDisco(64, new Color(0.55f, 0.4f, 0.85f, 0.28f));
        sr2.sortingOrder = 5;
        root.transform.localScale = Vector3.one * raio * 2f;

        float tick = 0f;
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            if (root == null) yield break;
            tick += Time.deltaTime;
            if (tick >= 1f && player != null)
            {
                tick = 0f;
                if (Vector2.Distance(player.transform.position, pos) <= raio)
                    player.AplicarVenenoPlayer(danoPorTick, 0.6f, 2.5f);
            }

            float puls = 0.25f + Mathf.Sin(t * 3.5f) * 0.04f;
            float fade = t > duracao - 0.8f ? (duracao - t) / 0.8f * puls : puls;
            var c = sr2.color; c.a = fade; sr2.color = c;
            yield return null;
        }
        if (root != null) Destroy(root);
    }

    void SpawnParticula(Vector2 pos, Color cor)
    {
        var go  = new GameObject("VP");
        var sr2 = go.AddComponent<SpriteRenderer>();
        sr2.sprite       = GerarDisco(10, cor);
        sr2.sortingOrder = 7;
        float sz = Random.Range(0.1f, 0.22f);
        go.transform.position   = pos;
        go.transform.localScale = Vector3.one * sz;
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
