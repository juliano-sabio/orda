using System.Collections;
using UnityEngine;

// ── Projétil ───────────────────────────────────────────────────────────────────

public class BolaDeFogoInimigo : MonoBehaviour
{
    float raioExp, danoExp, duracaoFogo, danoPorTick, intervaloTick;
    bool  explodiu;

    public void Inicializar(Vector2 dir, float vel, float raioExp, float danoExp,
        float duracaoFogo, float danoPorTick, float intervaloTick)
    {
        this.raioExp      = raioExp;
        this.danoExp      = danoExp;
        this.duracaoFogo  = duracaoFogo;
        this.danoPorTick  = danoPorTick;
        this.intervaloTick = intervaloTick;

        // Visual
        var sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite       = FogoSprites.Disco;
        sr.color        = new Color(1f, 0.45f, 0.05f, 1f);
        sr.sortingOrder = 14;
        transform.localScale = Vector3.one * 0.45f;

        // Trigger collider
        var col    = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.22f;

        // Movimento kinematic
        var rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType       = RigidbodyType2D.Kinematic;
        rb.gravityScale   = 0f;
        rb.linearVelocity = dir * vel;

        StartCoroutine(PulsarCor(sr));

        Destroy(gameObject, 5f); // timeout — OnDestroy chama Explodir se não acertou
    }

    IEnumerator PulsarCor(SpriteRenderer sr)
    {
        while (sr != null)
        {
            float p = Mathf.PingPong(Time.time * 5f, 1f);
            sr.color = Color.Lerp(new Color(1f, 0.25f, 0f), new Color(1f, 0.90f, 0.20f), p);
            yield return null;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (explodiu) return;

        bool ehPlayer    = other.CompareTag("Player");
        bool ehObstaculo = other.tag != "Enemy" && other.tag != "enemy" && !ehPlayer;

        if (ehPlayer || ehObstaculo)
        {
            explodiu = true;
            FazerExplosao();
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        // Timeout: explode na posição atual
        if (!explodiu && gameObject.scene.isLoaded)
        {
            explodiu = true;
            FazerExplosao();
        }
    }

    void FazerExplosao()
    {
        Vector3 pos = transform.position;

        // Dano direto ao player se estiver no raio
        var hits = Physics2D.OverlapCircleAll(pos, raioExp);
        foreach (var c in hits)
        {
            if (c.CompareTag("Player"))
            {
                c.GetComponent<PlayerStats>()?.TakeDamage(danoExp);
                break;
            }
        }

        // Área de fogo persistente
        var areaGO = new GameObject("AreaFogo");
        areaGO.transform.position = pos;
        areaGO.AddComponent<AreaFogoInimigo>().Inicializar(raioExp, duracaoFogo, danoPorTick, intervaloTick);

        // Anel de explosão
        var expGO = new GameObject("ExplosaoRing");
        expGO.transform.position = pos;
        expGO.AddComponent<ExplosaoVisual>().Iniciar(raioExp);
    }
}

// ── Área de Fogo ───────────────────────────────────────────────────────────────

public class AreaFogoInimigo : MonoBehaviour
{
    float danoPorTick, intervaloTick;
    float tickTimer;
    bool  encerrando;

    public void Inicializar(float raio, float duracao, float danoPorTick, float intervaloTick)
    {
        this.danoPorTick   = danoPorTick;
        this.intervaloTick = intervaloTick;

        var col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = raio;

        var rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType     = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        // Partículas de chamas
        for (int i = 0; i < 14; i++)
            CriarChama(raio, duracao);

        StartCoroutine(Vida(duracao));
    }

    void Update()
    {
        if (!encerrando) tickTimer += Time.deltaTime;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (encerrando || !other.CompareTag("Player")) return;
        if (tickTimer < intervaloTick) return;
        tickTimer = 0f;
        other.GetComponent<PlayerStats>()?.TakeDamage(danoPorTick);
    }

    IEnumerator Vida(float duracao)
    {
        yield return new WaitForSeconds(duracao);
        encerrando = true;
        Destroy(gameObject, 0.6f);
    }

    void CriarChama(float raio, float duracaoArea)
    {
        float ang    = Random.Range(0f, Mathf.PI * 2f);
        float dist   = Random.Range(0f, raio * 0.88f);
        Vector3 off  = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;

        var go = new GameObject("Chama");
        go.transform.position = transform.position + off;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = FogoSprites.Disco;
        sr.sortingOrder = 13;

        // Cores: amarelo, laranja, vermelho
        float hue  = Random.Range(0f, 0.12f); // 0=vermelho, 0.12=laranja-amarelo
        Color cor  = Color.HSVToRGB(hue, 1f, 1f);
        sr.color   = new Color(cor.r, cor.g, cor.b, Random.Range(0.65f, 0.92f));

        float esc  = Random.Range(raio * 0.30f, raio * 0.65f);
        go.transform.localScale = Vector3.one * esc;

        go.AddComponent<ChamaParticula>().Iniciar(
            duracaoArea,
            Random.Range(0.25f, 0.55f),
            Random.Range(1.5f, 3.5f));
    }
}

// ── Chama individual ──────────────────────────────────────────────────────────

public class ChamaParticula : MonoBehaviour
{
    public void Iniciar(float duracao, float velSubida, float freq) =>
        StartCoroutine(Animar(duracao, velSubida, freq));

    IEnumerator Animar(float duracao, float velSubida, float freq)
    {
        var    sr      = GetComponent<SpriteRenderer>();
        Color  corBase = sr != null ? sr.color : Color.white;
        float  amp     = Random.Range(0.05f, 0.12f);
        float  delay   = Random.Range(0f, duracao * 0.3f);
        Vector3 posBase = transform.position;
        float  escBase = transform.localScale.x;

        // Cada chama começa em momento diferente (evita todas aparecerem juntas)
        yield return new WaitForSeconds(delay);

        float t = 0f;
        float ciclo = Random.Range(0.8f, 1.6f); // duração do ciclo de subida
        while (t < duracao - delay)
        {
            t += Time.deltaTime;
            float tCiclo = t % ciclo;
            float pCiclo = tCiclo / ciclo;

            // Sobe e volta à base em loop
            float dy = Mathf.Sin(pCiclo * Mathf.PI) * velSubida * ciclo * 0.5f;
            float dx = Mathf.Sin(t * freq + GetInstanceID()) * amp;
            transform.position = posBase + new Vector3(dx, dy, 0f);

            // Escala: maior no meio do ciclo, menor nas pontas
            float esc = Mathf.Lerp(escBase * 0.4f, escBase, Mathf.Sin(pCiclo * Mathf.PI));
            transform.localScale = Vector3.one * esc;

            // Alpha geral: some quando a área está encerrando
            float tempoRestante = (duracao - delay) - t;
            float alpha = tempoRestante < 0.5f ? tempoRestante / 0.5f : 1f;
            if (sr != null)
                sr.color = new Color(corBase.r, corBase.g, corBase.b, corBase.a * alpha);

            yield return null;
        }
        Destroy(gameObject);
    }
}

// ── Anel de explosão ──────────────────────────────────────────────────────────

public class ExplosaoVisual : MonoBehaviour
{
    public void Iniciar(float raio) => StartCoroutine(Animar(raio));

    IEnumerator Animar(float raio)
    {
        var sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite       = FogoSprites.Anel;
        sr.sortingOrder = 15;

        float dur = 0.30f;
        float t   = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            transform.localScale = Vector3.one * (raio * 2.2f * p);
            sr.color = new Color(1f, Mathf.Lerp(0.85f, 0.15f, p), 0f, 1f - p);
            yield return null;
        }
        Destroy(gameObject);
    }
}

// ── Sprites de fogo (compartilhados, gerados uma vez) ─────────────────────────

public static class FogoSprites
{
    static Sprite _disco;
    static Sprite _anel;

    public static Sprite Disco => _disco != null ? _disco : (_disco = GerarDisco(64));
    public static Sprite Anel  => _anel  != null ? _anel  : (_anel  = GerarAnel(64, 7));

    static Sprite GerarDisco(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cx));
            float a = d < cx ? Mathf.Pow(1f - d / cx, 0.5f) : 0f;
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }

    static Sprite GerarAnel(int sz, int espessura)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx   = sz * 0.5f;
        float rExt = cx - 0.5f;
        float rInt = cx - espessura - 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cx));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, d <= rExt && d >= rInt ? 1f : 0f));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }
}
