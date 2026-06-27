using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CristaisGeloSkillBehavior : SkillBehavior, ISkillComRecarga
{
    float baseDano      = 18f;
    float multiplicador = 0.5f;
    float intervaloTiro = 1.2f;   // segundos entre disparos por cristal
    int   qtdCristais   = 3;
    float raioOrbita    = 1.4f;
    float velocProjetil = 14f;
    float raioDeteccao  = 10f;

    float timer;
    public bool  EmRecarga    => timer > 0f;
    public float TimerRecarga => timer;
    public float RecargaTotal => intervaloTiro;

    float DanoAtual => baseDano + (playerStats != null ? playerStats.attack * multiplicador : 0f);

    readonly List<CristalOrbital> cristais = new List<CristalOrbital>();

    public override void Initialize(PlayerStats stats)
    {
        base.Initialize(stats);
        timer = intervaloTiro;
    }

    static readonly Color COR_ORIG = new Color(0.45f, 0.88f, 1f);
    Color CorElemento() {
        if (skillData != null && skillData.appliedElement != ElementType.None)
            return ElementRegistry.Instance?.GetCor(skillData.appliedElement) ?? COR_ORIG;
        return COR_ORIG;
    }

    public void ConfigurarDeSkillData(SkillData data)
    {
        this.skillData = data;
        baseDano       = data.attackBonus > 0f          ? data.attackBonus        : 18f;
        intervaloTiro  = data.activationInterval > 0f   ? data.activationInterval : 1.2f;
        qtdCristais    = data.projectileCount > 0        ? data.projectileCount    : 3;
        velocProjetil  = data.projectileSpeed > 0f       ? data.projectileSpeed    : 14f;
        raioDeteccao   = data.specialValue > 0f          ? data.specialValue       : 10f;
        timer          = intervaloTiro;
    }

    void Start()
    {
        StartCoroutine(CriarCristaisDelayed());
    }

    IEnumerator CriarCristaisDelayed()
    {
        yield return null;
        if (playerStats == null) yield break;
        CriarCristais();
    }

    void OnDestroy()
    {
        foreach (var c in cristais)
            if (c != null && c.gameObject != null) Destroy(c.gameObject);
        cristais.Clear();
    }

    public override void ApplyEffect() { }

    void Update()
    {
        if (playerStats == null) return;

        // Recria cristais se sumiu
        if (cristais.Count == 0) CriarCristais();

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = intervaloTiro;
            Disparar();
        }
    }

    void Disparar()
    {
        var alvo = EncontrarMaisProximo();
        if (alvo == null) return;

        if (!cosmetico && playerStats != null)
            SomSkill.Tocar(SomSkill.Tipo.GeloDisparoDark, playerStats.transform.position, 0.4f);

        int qtdDisparos = SkillEvolutionManager.Tem(SkillEvolutionType.CristaisDuplos)
            ? cristais.Count : Mathf.Min(1, cristais.Count);

        for (int i = 0; i < qtdDisparos && i < cristais.Count; i++)
        {
            if (cristais[i] == null) continue;
            Vector2 origem = cristais[i].transform.position;
            StartCoroutine(CristalDisparar(cristais[i], origem, alvo));
        }
    }

    IEnumerator CristalDisparar(CristalOrbital cristal, Vector2 origem, InimigoController alvo)
    {
        // Flash no cristal
        if (cristal != null) cristal.Flash();
        yield return new WaitForSeconds(0.06f);

        // Cria projétil
        var go = new GameObject("ProjetilGelo");
        go.transform.position = origem;
        var pg = go.AddComponent<ProjetilGelo>();
        pg.skillDataRef = skillData;
        pg.cosmetico = cosmetico;
        pg.Iniciar(alvo, velocProjetil, DanoAtual,
            SkillEvolutionManager.Tem(SkillEvolutionType.CristaisExplosivos));
    }

    void CriarCristais()
    {
        foreach (var c in cristais)
            if (c != null) Destroy(c.gameObject);
        cristais.Clear();

        int qtd = SkillEvolutionManager.Tem(SkillEvolutionType.CristaisDuplos) ? 5 : qtdCristais;

        for (int i = 0; i < qtd; i++)
        {
            float angOffset = i / (float)qtd * 360f;
            var go = new GameObject($"Cristal{i}");
            var c = go.AddComponent<CristalOrbital>();
            c.Iniciar(playerStats.transform, raioOrbita, angOffset, CorElemento());
            cristais.Add(c);
        }
    }

    InimigoController EncontrarMaisProximo()
    {
        var todos = FindObjectsByType<InimigoController>(FindObjectsSortMode.None);
        InimigoController melhor = null;
        float menorDist = float.MaxValue;
        Vector2 orig = playerStats.transform.position;
        foreach (var ic in todos)
        {
            if (ic.estaMorrendo) continue;
            float d = Vector2.Distance(ic.transform.position, orig);
            if (d < menorDist && d <= raioDeteccao) { menorDist = d; melhor = ic; }
        }
        return melhor;
    }
}

// ── Cristal Orbital ──────────────────────────────────────────────────────────

public class CristalOrbital : MonoBehaviour
{
    Transform alvo;
    float     raio;
    float     angulo;
    float     velocOrbita = 90f; // graus/s
    SpriteRenderer sr;
    Color COR_CRISTAL = new Color(0.45f, 0.88f, 1f);

    public void Iniciar(Transform player, float r, float angOffset, Color cor = default)
    {
        alvo   = player;
        raio   = r;
        angulo = angOffset;
        if (cor != default && cor.a > 0f) COR_CRISTAL = cor;

        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite       = GerarCristal();
        sr.color        = COR_CRISTAL;
        sr.sortingOrder = 13;
        transform.localScale = Vector3.one * 0.45f;

        // Glow
        var gGO = new GameObject("Glow");
        gGO.transform.SetParent(transform, false);
        var gsr = gGO.AddComponent<SpriteRenderer>();
        gsr.sprite = GerarDisco(16);
        gsr.color  = new Color(COR_CRISTAL.r, COR_CRISTAL.g, COR_CRISTAL.b, 0.3f);
        gsr.sortingOrder = 12;
        gGO.transform.localScale = Vector3.one * 1.8f;

        StartCoroutine(Pulsar(gsr));
    }

    void Update()
    {
        if (alvo == null) { Destroy(gameObject); return; }
        angulo += velocOrbita * Time.deltaTime;
        float rad = angulo * Mathf.Deg2Rad;
        float bob = Mathf.Sin(Time.time * 2.5f + angulo * 0.05f) * 0.12f;
        transform.position = (Vector2)alvo.position
            + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * (raio + bob);
        transform.rotation = Quaternion.Euler(0f, 0f, angulo * 1.3f);
    }

    public void Flash()
    {
        if (sr != null) StartCoroutine(FlashCor());
    }

    IEnumerator FlashCor()
    {
        if (sr == null) yield break;
        sr.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        if (sr != null) sr.color = COR_CRISTAL;
    }

    IEnumerator Pulsar(SpriteRenderer gsr)
    {
        while (gsr != null)
        {
            float a = Mathf.Sin(Time.time * 3f) * 0.12f + 0.28f;
            gsr.color = new Color(COR_CRISTAL.r, COR_CRISTAL.g, COR_CRISTAL.b, a);
            yield return null;
        }
    }

    static Sprite GerarCristal()
    {
        const int W = 12, H = 24;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = W * 0.5f;
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
        {
            float nx = Mathf.Abs(x + 0.5f - cx) / cx;
            float ny = y / (float)(H - 1);
            // Forma de cristal: rombo pontiagudo
            float larg = ny < 0.5f
                ? (1f - nx) * ny * 2f
                : (1f - nx) * (1f - ny) * 2f;
            float brilho = Mathf.Max(0f, 0.7f - nx * 5f);
            float a = Mathf.Clamp01(larg * 2f + brilho);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), W);
    }

    static Sprite GerarDisco(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear; float c = sz * 0.5f;
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++)
        { float d = Vector2.Distance(new Vector2(x+0.5f,y+0.5f), new Vector2(c,c)); tex.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(1f-d/c))); }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,sz,sz), new Vector2(0.5f,0.5f), sz);
    }
}

// ── Projétil de Gelo ─────────────────────────────────────────────────────────

public class ProjetilGelo : MonoBehaviour
{
    public SkillData  skillDataRef;
    public bool       cosmetico; // co-op: fantasma do colega — só visual, sem dano
    InimigoController alvo;
    Vector2           dir;
    float             vel, dano;
    bool              atingiu, explosivo;
    SpriteRenderer    sr;

    Color COR_GELO = new Color(0.45f, 0.88f, 1f);

    public void Iniciar(InimigoController a, float v, float d, bool expl)
    {
        alvo     = a; vel = v; dano = d; explosivo = expl;
        if (skillDataRef != null && skillDataRef.appliedElement != ElementType.None && ElementRegistry.Instance != null)
            COR_GELO = ElementRegistry.Instance.GetCor(skillDataRef.appliedElement);
        dir      = alvo != null
            ? ((Vector2)alvo.transform.position - (Vector2)transform.position).normalized
            : Vector2.up;

        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang - 90f);

        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = CristalOrbital_GerarCristalSmall();
        sr.color  = COR_GELO;
        sr.sortingOrder = 14;
        transform.localScale = Vector3.one * 0.3f;

        var col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true; col.radius = 0.25f;

        Destroy(gameObject, 2.5f);
        StartCoroutine(Trail());
    }

    void Update()
    {
        if (atingiu) return;
        if (alvo != null && !alvo.estaMorrendo)
        {
            Vector2 dirAlvo = ((Vector2)alvo.transform.position - (Vector2)transform.position).normalized;
            dir = Vector2.Lerp(dir, dirAlvo, Time.deltaTime * 6f).normalized;
        }
        transform.position += (Vector3)(dir * vel * Time.deltaTime);
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang - 90f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (atingiu) return;
        var ic = other.GetComponent<InimigoController>() ?? other.GetComponentInParent<InimigoController>();
        if (ic == null) return;
        atingiu = true;

        // Co-op: a cópia cosmética NÃO aplica dano/lentidão (só visual). O dano é do projétil
        // real do dono, já roteado pro host.
        if (!cosmetico)
        {
            ic.ReceberDano(dano, false);
            SkillElementEffect.Aplicar(skillDataRef, ic.gameObject, dano, this);
            EvolutionFX.AplicarLentidao(ic, 2f, 0.45f);
            GeloLentidaoFX.AplicarAo(ic.gameObject, 2f);
            SomSkill.Tocar(SomSkill.Tipo.GeloImpactoDark, transform.position, explosivo ? 0.5f : 0.4f);
        }

        if (explosivo)
        {
            if (!cosmetico)
            {
                // dano em área
                var hits = Physics2D.OverlapCircleAll(transform.position, 2f);
                foreach (var col in hits)
                {
                    var alvoArea = col.GetComponent<InimigoController>() ?? col.GetComponentInParent<InimigoController>();
                    if (alvoArea != null && !alvoArea.estaMorrendo && alvoArea != ic)
                    {
                        alvoArea.ReceberDano(dano * 0.5f, false);
                        EvolutionFX.AplicarLentidao(alvoArea, 2f, 0.45f);
                    }
                }
            }
            // visual de explosão de gelo (sempre, é só visual)
            var fxGO = new GameObject("ExplosaoGelo");
            fxGO.transform.position = transform.position;
            fxGO.AddComponent<ExplosaoGeloFX>().Iniciar(transform.position, 2f);
        }

        StartCoroutine(EfeitoImpacto());
    }

    IEnumerator EfeitoImpacto()
    {
        // Partículas de gelo
        for (int i = 0; i < 6; i++)
        {
            float ang = i / 6f * Mathf.PI * 2f;
            var p = new GameObject("PGelo");
            p.transform.position = transform.position;
            var psr = p.AddComponent<SpriteRenderer>();
            psr.sprite = GerarDisco(6); psr.color = COR_GELO; psr.sortingOrder = 15;
            p.transform.localScale = Vector3.one * Random.Range(0.08f, 0.18f);
            p.AddComponent<AutoDestroyFadeMove>().Iniciar(
                new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(1.5f, 4f), 0.3f);
            Destroy(p, 0.5f);
        }
        // Fade do projétil
        for (float t = 0f; t < 0.1f; t += Time.deltaTime)
        {
            if (sr == null) yield break;
            sr.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, t / 0.1f));
            yield return null;
        }
        Destroy(gameObject);
    }

    IEnumerator Trail()
    {
        while (!atingiu && gameObject != null)
        {
            var t = new GameObject("TGelo");
            t.transform.position = transform.position;
            t.transform.rotation = transform.rotation;
            t.transform.localScale = transform.localScale * 0.8f;
            var tsr = t.AddComponent<SpriteRenderer>();
            tsr.sprite = sr?.sprite; tsr.color = new Color(COR_GELO.r, COR_GELO.g, COR_GELO.b, 0.4f);
            tsr.sortingOrder = 13;
            t.AddComponent<AutoDestroyFade>().Iniciar(0.08f);
            Destroy(t, 0.2f);
            yield return new WaitForSeconds(0.04f);
        }
    }

    static Sprite CristalOrbital_GerarCristalSmall()
    {
        const int W = 8, H = 16;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear; float cx = W * 0.5f;
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
        {
            float nx = Mathf.Abs(x+0.5f-cx)/cx; float ny = y/(float)(H-1);
            float larg = ny<0.5f?(1f-nx)*ny*2f:(1f-nx)*(1f-ny)*2f;
            tex.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(larg*2f)));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,W,H), new Vector2(0.5f,0.5f), W);
    }

    static Sprite GerarDisco(int sz)
    {
        var tex = new Texture2D(sz,sz,TextureFormat.RGBA32,false); tex.filterMode=FilterMode.Bilinear; float c=sz*0.5f;
        for(int y=0;y<sz;y++) for(int x=0;x<sz;x++){float d=Vector2.Distance(new Vector2(x+0.5f,y+0.5f),new Vector2(c,c));tex.SetPixel(x,y,new Color(1,1,1,d<c?1:0));}
        tex.Apply(); return Sprite.Create(tex,new Rect(0,0,sz,sz),new Vector2(0.5f,0.5f),sz);
    }
}

// ── Efeito Visual de Lentidão (partículas de gelo contínuas no inimigo) ────────
public class GeloLentidaoFX : MonoBehaviour
{
    static readonly Color COR_GELO = new Color(0.45f, 0.88f, 1f);
    static readonly Color COR_NEVE = new Color(0.85f, 0.97f, 1f);

    Coroutine emissao;

    public static void AplicarAo(GameObject alvo, float dur)
    {
        var existente = alvo.GetComponent<GeloLentidaoFX>();
        if (existente != null) { existente.Refresh(dur); return; }
        alvo.AddComponent<GeloLentidaoFX>().Iniciar(dur);
    }

    void Iniciar(float dur)     => emissao = StartCoroutine(Emitir(dur));
    public void Refresh(float dur) { if (emissao != null) StopCoroutine(emissao); emissao = StartCoroutine(Emitir(dur)); }

    IEnumerator Emitir(float dur)
    {
        for (float t = 0f; t < dur; t += 0.08f)
        {
            SpawnParticula();
            if (Random.value < 0.45f) SpawnFloco();
            yield return new WaitForSeconds(0.08f);
        }
        Destroy(this);
    }

    void SpawnParticula()
    {
        float ang  = Random.Range(0f, Mathf.PI * 2f);
        Vector2 pos = (Vector2)transform.position
            + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(0.15f, 0.45f);

        var p   = new GameObject("GeloP");
        p.transform.position   = pos;
        p.transform.localScale = Vector3.one * Random.Range(0.05f, 0.13f);
        var psr = p.AddComponent<SpriteRenderer>();
        psr.sprite       = GerarDisco(8);
        psr.color        = new Color(COR_GELO.r, COR_GELO.g, COR_GELO.b, Random.Range(0.7f, 1f));
        psr.sortingOrder = 16;
        p.AddComponent<AutoDestroyFadeMove>().Iniciar(
            new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(0.6f, 1.8f)),
            Random.Range(0.25f, 0.5f));
        Destroy(p, 0.6f);
    }

    void SpawnFloco()
    {
        float ang  = Random.Range(0f, Mathf.PI * 2f);
        Vector2 pos = (Vector2)transform.position
            + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(0.05f, 0.35f);

        var f   = new GameObject("GeloFloco");
        f.transform.position   = pos;
        f.transform.localScale = Vector3.one * Random.Range(0.07f, 0.18f);
        f.transform.rotation   = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        var fsr = f.AddComponent<SpriteRenderer>();
        fsr.sprite       = GerarCristalMini();
        fsr.color        = new Color(COR_NEVE.r, COR_NEVE.g, COR_NEVE.b, Random.Range(0.5f, 0.85f));
        fsr.sortingOrder = 16;
        f.AddComponent<AutoDestroyFadeMove>().Iniciar(
            new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0.3f, 1.2f)),
            Random.Range(0.3f, 0.55f));
        Destroy(f, 0.7f);
    }

    static Sprite GerarDisco(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear; float c = sz * 0.5f;
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++)
        { float d = Vector2.Distance(new Vector2(x+0.5f,y+0.5f),new Vector2(c,c)); tex.SetPixel(x,y,new Color(1,1,1,d<c?1:0)); }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,sz,sz), new Vector2(0.5f,0.5f), sz);
    }

    static Sprite GerarCristalMini()
    {
        const int W = 6, H = 10;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear; float cx = W * 0.5f;
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
        {
            float nx = Mathf.Abs(x+0.5f-cx)/cx; float ny = y/(float)(H-1);
            float l  = ny < 0.5f ? (1f-nx)*ny*2f : (1f-nx)*(1f-ny)*2f;
            tex.SetPixel(x, y, new Color(1,1,1,Mathf.Clamp01(l*2.2f)));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,W,H), new Vector2(0.5f,0.5f), W);
    }
}

// ── Explosão de Gelo (visual dedicado para CristaisExplosivos) ────────────────
public class ExplosaoGeloFX : MonoBehaviour
{
    static readonly Color COR_GELO   = new Color(0.45f, 0.88f, 1f);
    static readonly Color COR_BRANCA = new Color(0.85f, 1f,    1f);

    public void Iniciar(Vector2 pos, float raio) => StartCoroutine(Animar(pos, raio));

    IEnumerator Animar(Vector2 pos, float raio)
    {
        // 1 — Flash branco central instantâneo
        SpawnFlashCentro(pos);

        // 2 — Anel de geada expandindo
        StartCoroutine(AnelGeada(pos, raio, 0.4f));

        // 3 — Segundo anel menor, mais rápido
        StartCoroutine(AnelGeada(pos, raio * 0.55f, 0.25f));

        // 4 — Fragmentos de cristal voando
        SpawnFragmentos(pos, raio);

        // 5 — Círculo de gelo no chão que some devagar
        StartCoroutine(CirculoChao(pos, raio));

        // Aguarda todas as animações encerrarem
        yield return new WaitForSeconds(0.8f);
        Destroy(gameObject);
    }

    // Flash branco no centro
    void SpawnFlashCentro(Vector2 pos)
    {
        var go = new GameObject("FlashGelo");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GerarDisco(32);
        sr.color  = COR_BRANCA;
        sr.sortingOrder = 17;
        go.transform.localScale = Vector3.one * 0.5f;
        go.AddComponent<AutoDestroyFade>().Iniciar(0.12f);
        Destroy(go, 0.25f);
    }

    // Anel de geada expandindo e desaparecendo
    IEnumerator AnelGeada(Vector2 pos, float raioMax, float dur)
    {
        const int S = 48;
        var go = new GameObject("AnelGelo");
        go.transform.position = pos;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace  = true; lr.loop = true; lr.positionCount = S;
        lr.material       = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder   = 14;

        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            float r = Mathf.Lerp(0.05f, raioMax, Mathf.Sqrt(p));
            float w = Mathf.Lerp(0.22f, 0.03f, p);
            float a = Mathf.Lerp(1f,    0f,     p * p);
            Color c = Color.Lerp(COR_BRANCA, COR_GELO, p);
            lr.startWidth = lr.endWidth = w;
            lr.startColor = lr.endColor = new Color(c.r, c.g, c.b, a);
            for (int i = 0; i < S; i++)
            {
                float ang = 360f / S * i * Mathf.Deg2Rad;
                lr.SetPosition(i, pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r);
            }
            yield return null;
        }
        Destroy(go);
    }

    // Fragmentos de cristal explodindo para fora
    void SpawnFragmentos(Vector2 pos, float raio)
    {
        int qtd = 12;
        for (int i = 0; i < qtd; i++)
        {
            float ang   = i / (float)qtd * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
            float speed = Random.Range(raio * 1.8f, raio * 3.5f);
            Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * speed;

            // Cristal principal
            var go = new GameObject("FragGelo");
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * Random.Range(0.18f, 0.38f);
            go.transform.rotation   = Quaternion.Euler(0f, 0f, ang * Mathf.Rad2Deg);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GerarCristalSmall();
            sr.color  = i % 3 == 0 ? COR_BRANCA : COR_GELO;
            sr.sortingOrder = 15;
            go.AddComponent<AutoDestroyFadeMove>().Iniciar(vel, Random.Range(0.25f, 0.45f));
            Destroy(go, 0.55f);

            // Rastro de partícula menor
            var p = new GameObject("FragTrail");
            p.transform.position = pos;
            p.transform.localScale = Vector3.one * Random.Range(0.08f, 0.15f);
            var psr = p.AddComponent<SpriteRenderer>();
            psr.sprite = GerarDisco(6);
            psr.color  = new Color(COR_GELO.r, COR_GELO.g, COR_GELO.b, 0.7f);
            psr.sortingOrder = 14;
            p.AddComponent<AutoDestroyFadeMove>().Iniciar(vel * 0.5f, 0.2f);
            Destroy(p, 0.35f);
        }
    }

    // Disco de gelo no chão que aparece e some
    IEnumerator CirculoChao(Vector2 pos, float raio)
    {
        var go = new GameObject("ChaoGelo");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GerarDisco(64);
        sr.sortingOrder = 10;
        float dur = 0.6f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            float escala = Mathf.Lerp(0.1f, raio * 2f, Mathf.Sqrt(p));
            float alpha  = p < 0.2f ? Mathf.Lerp(0f, 0.35f, p / 0.2f)
                                    : Mathf.Lerp(0.35f, 0f, (p - 0.2f) / 0.8f);
            sr.color = new Color(COR_GELO.r, COR_GELO.g, COR_GELO.b, alpha);
            go.transform.localScale = Vector3.one * escala;
            yield return null;
        }
        Destroy(go);
    }

    static Sprite GerarCristalSmall()
    {
        const int W = 6, H = 14;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = W * 0.5f;
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
        {
            float nx = Mathf.Abs(x + 0.5f - cx) / cx;
            float ny = y / (float)(H - 1);
            float larg = ny < 0.5f ? (1f - nx) * ny * 2f : (1f - nx) * (1f - ny) * 2f;
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(larg * 2.2f)));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), W);
    }

    static Sprite GerarDisco(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear; float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cx));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, d < cx ? 1f : 0f));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }
}
