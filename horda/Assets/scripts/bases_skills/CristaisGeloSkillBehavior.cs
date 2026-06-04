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

    public void ConfigurarDeSkillData(SkillData data)
    {
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
        go.AddComponent<ProjetilGelo>().Iniciar(alvo, velocProjetil, DanoAtual,
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
            c.Iniciar(playerStats.transform, raioOrbita, angOffset);
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
    static readonly Color COR_CRISTAL = new Color(0.45f, 0.88f, 1f);

    public void Iniciar(Transform player, float r, float angOffset)
    {
        alvo   = player;
        raio   = r;
        angulo = angOffset;

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
    InimigoController alvo;
    Vector2           dir;
    float             vel, dano;
    bool              atingiu, explosivo;
    SpriteRenderer    sr;

    static readonly Color COR_GELO = new Color(0.45f, 0.88f, 1f);

    public void Iniciar(InimigoController a, float v, float d, bool expl)
    {
        alvo     = a; vel = v; dano = d; explosivo = expl;
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
        ic.ReceberDano(dano, false);

        // Lentidão
        EvolutionFX.AplicarLentidao(ic, 2f, 0.45f);

        if (explosivo)
            EvolutionFX.SpawnExplosao(transform.position, 2f, dano * 0.5f, COR_GELO, this);

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
