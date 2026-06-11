using System.Collections;
using UnityEngine;

// Fantasma elemental de Gelo: se move lentamente mas aplica lentidão ao tocar o player.
// Periodicamente cria uma zona de gelo no player que persiste e continua aplicando slow.
// Ao morrer, cria uma zona de gelo maior e explode em cristais.
public class FantasmaGelo : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidade     = 2.5f;
    public float amplitudeOnda  = 0.5f;
    public float frequenciaOnda = 2.0f;

    [Header("Contato Base")]
    public float danoContato    = 10f;
    public float intervaloContato = 1.5f;

    [Header("Elemento: Gelo")]
    public float reducaoSlow    = 0.5f;
    public float duracaoSlow    = 3.5f;
    public float cooldownSlow   = 4f;

    [Header("Ataque: Zona de Gelo")]
    public float raioZonaGelo   = 2.8f;
    public float duracaoZonaGelo = 3f;
    public float danoZonaGelo   = 8f;
    public float cooldownZona   = 9f;

    [Header("Morte")]
    public float raioGeloMorte  = 3.5f;
    public float duracaoGeloMorte = 4f;

    Rigidbody2D    rb;
    SpriteRenderer sr;
    InimigoController inimigoCtrl;
    PlayerStats    player;
    Vector3        escalaBase;
    float          ondaFase;

    float proxSlow;
    float proxZona;
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

        escalaBase = transform.localScale;
        ondaFase   = Random.Range(0f, Mathf.PI * 2f);
        proxSlow = Random.Range(1f, 3f);
        proxZona = Random.Range(4f, 9f);

        player = FindFirstObjectByType<PlayerStats>();
        StartCoroutine(RastroGelo());
        InimigoController.OnPreMorte += OnPreMorteHandler;
    }

    void OnDestroy() => InimigoController.OnPreMorte -= OnPreMorteHandler;

    void OnPreMorteHandler(InimigoController ic)
    {
        if (ic != inimigoCtrl) return;
        StartCoroutine(ZonaGelo(transform.position, raioGeloMorte, duracaoGeloMorte));
        StartCoroutine(ExplodirCristais(transform.position));
    }

    bool Morto() => inimigoCtrl != null && inimigoCtrl.estaMorrendo;

    void Update()
    {
        if (Morto() || player == null) return;
        proxSlow -= Time.deltaTime;
        proxZona -= Time.deltaTime;

        if (proxZona <= 0f)
        {
            proxZona = cooldownZona;
            StartCoroutine(ZonaGelo(player.transform.position, raioZonaGelo, duracaoZonaGelo));
        }

        float t  = Time.time * 2.2f + ondaFase;
        float sx = 1f + Mathf.Sin(t)         * 0.07f;
        float sy = 1f + Mathf.Cos(t * 1.2f)  * 0.10f;
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

    IEnumerator ZonaGelo(Vector2 pos, float raio, float duracao)
    {
        var root = new GameObject("ZonaGelo");
        root.transform.position = pos;
        Destroy(root, duracao + 0.5f);

        var sr2 = root.AddComponent<SpriteRenderer>();
        sr2.sprite       = GerarDisco(64, new Color(0.5f, 0.85f, 1f, 0.22f));
        sr2.sortingOrder = 5;
        root.transform.localScale = Vector3.one * raio * 2f;

        // Cristais orbitando
        var cristais = new Transform[6];
        for (int i = 0; i < cristais.Length; i++)
        {
            var c = new GameObject("Cristal");
            c.transform.SetParent(root.transform, false);
            var csr = c.AddComponent<SpriteRenderer>();
            csr.sprite       = GerarCristal(14, new Color(0.6f, 0.9f, 1f));
            csr.sortingOrder = 9;
            c.transform.localScale = Vector3.one * 0.55f;
            cristais[i] = c.transform;
        }

        float rot = 0f;
        float tick = 0f;

        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            if (root == null) yield break;

            rot += Time.deltaTime * 35f;
            for (int i = 0; i < cristais.Length; i++)
            {
                if (cristais[i] == null) continue;
                float ang = (360f / cristais.Length * i + rot) * Mathf.Deg2Rad;
                cristais[i].localPosition = new Vector3(Mathf.Cos(ang) * raio, Mathf.Sin(ang) * raio);
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

            float fade = t > duracao - 0.7f ? (duracao - t) / 0.7f * 0.22f : 0.22f;
            var c2 = sr2.color; c2.a = fade; sr2.color = c2;
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
            StartCoroutine(LancarCristal(go, dir, spd));
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
            StartCoroutine(FadeOut(go, Random.Range(0.35f, 0.7f)));
            yield return new WaitForSeconds(0.12f);
        }
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
