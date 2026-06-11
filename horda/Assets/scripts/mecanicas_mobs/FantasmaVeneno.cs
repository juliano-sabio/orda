using System.Collections;
using UnityEngine;

// Fantasma elemental de Veneno: atravessa obstáculos, deixa rastro de nuvens tóxicas,
// aplica envenenamento ao tocar o player. Ao morrer, cria uma grande nuvem de veneno.
public class FantasmaVeneno : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidade      = 3f;
    public float amplitudeOnda   = 0.55f;
    public float frequenciaOnda  = 2.3f;

    [Header("Contato Base")]
    public float danoContato = 10f;
    public float intervaloContato = 1.2f;

    [Header("Elemento: Veneno")]
    public float danoVenenoTick = 4f;
    public float duracaoVeneno = 6f;
    public float cooldownVeneno = 7f;

    [Header("Rastro Tóxico")]
    public float intervaloRastro = 1.8f;
    public float danoRastroPorTick = 2f;
    public float duracaoNuvemRastro = 3.5f;
    public float raioNuvemRastro = 1.2f;

    [Header("Morte: Nuvem Grande")]
    public float raioNuvemMorte = 3f;
    public float duracaoNuvemMorte = 5f;
    public float danoNuvemMortePorTick = 3f;

    Rigidbody2D    rb;
    SpriteRenderer sr;
    InimigoController inimigoCtrl;
    PlayerStats    player;
    Vector3        escalaBase;
    float          ondaFase;

    float proxVeneno;
    float proxContato;
    float proxRastro;

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
        proxVeneno = Random.Range(2f, 5f);
        proxRastro = intervaloRastro;

        player = FindFirstObjectByType<PlayerStats>();
        StartCoroutine(RastroVeneno());
        InimigoController.OnPreMorte += OnPreMorteHandler;
    }

    void OnDestroy() => InimigoController.OnPreMorte -= OnPreMorteHandler;

    void OnPreMorteHandler(InimigoController ic)
    {
        if (ic != inimigoCtrl) return;
        StartCoroutine(NuvemDeVeneno(transform.position, raioNuvemMorte, duracaoNuvemMorte, danoNuvemMortePorTick));
    }

    bool Morto() => inimigoCtrl != null && inimigoCtrl.estaMorrendo;

    void Update()
    {
        if (Morto() || player == null) return;
        proxVeneno -= Time.deltaTime;
        proxRastro -= Time.deltaTime;

        if (proxRastro <= 0f)
        {
            proxRastro = intervaloRastro;
            StartCoroutine(NuvemDeVeneno(transform.position, raioNuvemRastro, duracaoNuvemRastro, danoRastroPorTick));
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

        // Dano de contato
        if (Time.time >= proxContato)
        {
            ps.TakeDamage(danoContato);
            proxContato = Time.time + intervaloContato;
        }

        // Veneno
        if (proxVeneno <= 0f)
        {
            ps.AplicarVenenoPlayer(danoVenenoTick, 1f, duracaoVeneno);
            proxVeneno = cooldownVeneno;
        }
    }

    IEnumerator RastroVeneno()
    {
        while (!Morto())
        {
            SpawnParticula(transform.position, new Color(0.3f, 1f, 0.4f, 0.65f));
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator NuvemDeVeneno(Vector2 pos, float raio, float duracao, float danoPorTick)
    {
        var root = new GameObject("NuvemVeneno");
        root.transform.position = pos;
        Destroy(root, duracao + 0.5f);

        var sr2 = root.AddComponent<SpriteRenderer>();
        sr2.sprite       = GerarDisco(64, new Color(0.3f, 0.85f, 0.3f, 0.28f));
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
        sr2.sprite       = GerarDisco(12, cor);
        sr2.sortingOrder = 7;
        float sz = Random.Range(0.15f, 0.35f);
        go.transform.position   = (Vector3)pos + (Vector3)Random.insideUnitCircle * 0.18f;
        go.transform.localScale = Vector3.one * sz;
        StartCoroutine(FadeOut(go, Random.Range(0.3f, 0.6f)));
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
