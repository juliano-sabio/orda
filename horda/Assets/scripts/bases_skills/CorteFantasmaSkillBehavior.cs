using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CorteFantasmaSkillBehavior : SkillBehavior, ISkillComRecarga
{
    float baseDano      = 17f;
    float multiplicador = 0.8f;
    float intervalo     = 5f;
    int   qtdCortes     = 2;
    float velocidade    = 18f;
    float raioDeteccao  = 15f;

    float timer;
    public bool  EmRecarga    => timer > 0f;
    public float TimerRecarga => timer;
    public float RecargaTotal => intervalo;

    float DanoAtual => baseDano + (playerStats != null ? playerStats.attack * multiplicador : 0f);

    public override void Initialize(PlayerStats stats) => base.Initialize(stats);

    static readonly Color COR_ORIG = new Color(0.65f, 0.92f, 1f);
    Color CorElemento() {
        if (skillData != null && skillData.appliedElement != ElementType.None)
            return ElementRegistry.Instance?.GetCor(skillData.appliedElement) ?? COR_ORIG;
        return COR_ORIG;
    }

    public void ConfigurarDeSkillData(SkillData data)
    {
        this.skillData = data;
        baseDano    = data.attackBonus > 0f          ? data.attackBonus        : 35f;
        intervalo   = data.activationInterval > 0f   ? data.activationInterval : 2.5f;
        qtdCortes   = data.projectileCount > 0       ? data.projectileCount    : 2;
        velocidade  = data.projectileSpeed > 0f      ? data.projectileSpeed    : 18f;
        timer       = intervalo;
    }

    void Update()
    {
        if (playerStats == null) return;
        timer -= Time.deltaTime;
        if (timer <= 0f) { timer = intervalo; Disparar(); }
    }

    public override void ApplyEffect() => Disparar();

    // CorteLetal: rastreia hits por inimigo
    public readonly Dictionary<int, int> hitsInimigo = new Dictionary<int, int>();

    void Disparar()
    {
        int qtdReal = SkillEvolutionManager.Tem(SkillEvolutionType.CorteTriple) ? qtdCortes + 1 : qtdCortes;
        var alvos = EncontrarAlvos(qtdReal);
        Vector2 origem = playerStats.transform.position;

        StartCoroutine(EfeitoLancamento(origem));

        for (int i = 0; i < alvos.Count; i++)
        {
            var alvo = alvos[i];
            StartCoroutine(DisparararCorteComDelay(origem, alvo, i * 0.12f));
        }
    }

    IEnumerator EfeitoLancamento(Vector2 pos)
    {
        // Flash espectral rápido no player
        var sr = playerStats?.GetComponent<SpriteRenderer>();
        Color orig = sr != null ? sr.color : Color.white;
        if (sr != null) sr.color = new Color(0.6f, 0.95f, 1f);
        yield return new WaitForSeconds(0.07f);
        if (sr != null) sr.color = orig;

        // Burst de partículas espectrais saindo do player
        for (int i = 0; i < 8; i++)
        {
            float ang = i / 8f * Mathf.PI * 2f;
            var go = new GameObject("BurstEsp");
            go.transform.position = pos;
            var bsr = go.AddComponent<SpriteRenderer>();
            bsr.sprite = GerarDisco(6);
            bsr.color  = new Color(0.5f, 0.88f, 1f, 0.7f);
            bsr.sortingOrder = 13;
            go.transform.localScale = Vector3.one * Random.Range(0.1f, 0.2f);
            go.AddComponent<AutoDestroyFadeMove>().Iniciar(
                new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(2f, 5f), 0.22f);
            Destroy(go, 0.4f);
        }
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

    IEnumerator DisparararCorteComDelay(Vector2 origem, InimigoController alvo, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        var go = new GameObject("CorteFantasma");
        go.transform.position = origem;
        var proj = go.AddComponent<CorteFantasmaProjetil>();
        proj.skillDataRef = skillData;
        proj.Iniciar(alvo, velocidade, DanoAtual, this);
    }

    List<InimigoController> EncontrarAlvos(int qtd)
    {
        var todos = FindObjectsByType<InimigoController>(FindObjectsSortMode.None);
        var lista = new List<InimigoController>(todos);
        Vector2 orig = playerStats != null ? (Vector2)playerStats.transform.position : Vector2.zero;
        lista.RemoveAll(ic => ic.estaMorrendo ||
            Vector2.Distance(ic.transform.position, orig) > raioDeteccao);
        lista.Sort((a, b) =>
            Vector2.Distance(a.transform.position, orig)
            .CompareTo(Vector2.Distance(b.transform.position, orig)));
        if (lista.Count > qtd) lista.RemoveRange(qtd, lista.Count - qtd);
        return lista;
    }
}

// ── Projétil do corte fantasma ────────────────────────────────────────────────

public class CorteFantasmaProjetil : MonoBehaviour
{
    public SkillData           skillDataRef;
    InimigoController          alvo;
    CorteFantasmaSkillBehavior skillBehavior;
    Vector2                    dir;
    float                      vel;
    float                      dano;
    bool                       atingiu;
    float                      rot;

    SpriteRenderer    sr;

    public void Iniciar(InimigoController alvoIC, float velocidade, float dmg, CorteFantasmaSkillBehavior behavior)
    {
        alvo          = alvoIC;
        vel           = velocidade;
        dano          = dmg;
        skillBehavior = behavior;

        dir = alvo != null
            ? ((Vector2)alvo.transform.position - (Vector2)transform.position).normalized
            : Random.insideUnitCircle.normalized;

        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang - 45f);

        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite       = GerarCorteFantasma();
        sr.color        = new Color(0.65f, 0.92f, 1f, 0.95f);
        sr.sortingOrder = 14;
        transform.localScale = Vector3.one * 0.85f;

        // Glow translúcido maior por trás
        var glowGO = new GameObject("Glow");
        glowGO.transform.SetParent(transform, false);
        var gsr = glowGO.AddComponent<SpriteRenderer>();
        gsr.sprite       = GerarCorteFantasma();
        gsr.color        = new Color(0.4f, 0.75f, 1f, 0.28f);
        gsr.sortingOrder = 13;
        glowGO.transform.localScale = Vector3.one * 1.6f;

        var col = gameObject.AddComponent<PolygonCollider2D>();
        col.isTrigger = true;
        col.SetPath(0, new Vector2[] {
            new Vector2(-0.3f, -0.5f), new Vector2( 0.3f, -0.5f),
            new Vector2( 0.5f,  0.5f), new Vector2(-0.5f,  0.5f),
        });

        Destroy(gameObject, 2f);
        StartCoroutine(SpawnTrail());
        StartCoroutine(PulsarGlow(gsr));
    }

    void Update()
    {
        if (atingiu) return;

        if (alvo != null && !alvo.estaMorrendo)
        {
            Vector2 dirAlvo = ((Vector2)alvo.transform.position - (Vector2)transform.position).normalized;
            dir = Vector2.Lerp(dir, dirAlvo, Time.deltaTime * 8f).normalized;
        }

        transform.position += (Vector3)(dir * vel * Time.deltaTime);

        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang - 45f);

        rot += Time.deltaTime * 6f;
        if (sr != null)
        {
            float pulso = Mathf.Sin(rot) * 0.12f + 0.88f;
            sr.color = new Color(0.65f, 0.92f, 1f, pulso);
        }
    }

    IEnumerator PulsarGlow(SpriteRenderer gsr)
    {
        while (!atingiu && gsr != null)
        {
            float t = Time.time * 4f;
            float a = Mathf.Sin(t) * 0.08f + 0.25f;
            gsr.color = new Color(0.4f, 0.75f, 1f, a);
            yield return null;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (atingiu) return;
        var ic = other.GetComponent<InimigoController>()
              ?? other.GetComponentInParent<InimigoController>();
        if (ic == null) return;

        atingiu = true;
        ic.ReceberDano(dano, false);
        SkillElementEffect.Aplicar(skillDataRef, ic.gameObject, dano, this);

        // CorteLetal: 2 hits no mesmo inimigo = atordoamento
        if (SkillEvolutionManager.Tem(SkillEvolutionType.CorteLetal) && skillBehavior != null)
        {
            int id = ic.gameObject.GetInstanceID();
            skillBehavior.hitsInimigo.TryGetValue(id, out int hits);
            hits++;
            skillBehavior.hitsInimigo[id] = hits;
            if (hits >= 2)
            {
                skillBehavior.hitsInimigo.Remove(id);
                EvolutionFX.AplicarLentidao(ic, 1f, 0f); // velocidade 0 = atordoamento
            }
        }

        StartCoroutine(EfeitoImpacto(transform.position, transform.rotation));
    }

    IEnumerator SpawnTrail()
    {
        while (!atingiu && gameObject != null)
        {
            yield return new WaitForSeconds(0.035f);
            if (gameObject == null) yield break;

            // Trail principal
            var t = new GameObject("TrailCorte");
            t.transform.position   = transform.position;
            t.transform.rotation   = transform.rotation;
            t.transform.localScale = transform.localScale;
            var tsr = t.AddComponent<SpriteRenderer>();
            tsr.sprite       = GerarCorteFantasma();
            tsr.color        = new Color(0.45f, 0.82f, 1f, 0.45f);
            tsr.sortingOrder = 13;
            t.AddComponent<AutoDestroyFade>().Iniciar(0.18f);
            Destroy(t, 0.35f);

            // Trail difuso maior (glow)
            var g = new GameObject("TrailGlow");
            g.transform.position   = transform.position;
            g.transform.rotation   = transform.rotation;
            g.transform.localScale = transform.localScale * 1.5f;
            var gsr2 = g.AddComponent<SpriteRenderer>();
            gsr2.sprite       = GerarCorteFantasma();
            gsr2.color        = new Color(0.3f, 0.65f, 1f, 0.15f);
            gsr2.sortingOrder = 12;
            g.AddComponent<AutoDestroyFade>().Iniciar(0.25f);
            Destroy(g, 0.4f);

            // Partícula fantasma ocasional
            if (Random.value < 0.5f)
            {
                var p2 = new GameObject("PartTrail");
                p2.transform.position = (Vector2)transform.position + Random.insideUnitCircle * 0.2f;
                var psr2 = p2.AddComponent<SpriteRenderer>();
                psr2.sprite = GerarDisco(5);
                psr2.color  = new Color(0.5f, 0.9f, 1f, 0.5f);
                psr2.sortingOrder = 12;
                p2.transform.localScale = Vector3.one * Random.Range(0.06f, 0.14f);
                p2.AddComponent<AutoDestroyFadeMove>().Iniciar(
                    Random.insideUnitCircle * 0.5f, Random.Range(0.12f, 0.22f));
                Destroy(p2, 0.35f);
            }
        }
    }

    IEnumerator EfeitoImpacto(Vector3 pos, Quaternion rot_)
    {
        // Slash marks brancos escalonados
        for (int i = 0; i < 4; i++)
        {
            float delay2 = i * 0.04f;
            var slash = new GameObject("SlashMark");
            slash.transform.position   = pos + (Vector3)Random.insideUnitCircle * 0.35f;
            slash.transform.rotation   = Quaternion.Euler(0f, 0f,
                rot_.eulerAngles.z + Random.Range(-40f, 40f));
            slash.transform.localScale = Vector3.one * Random.Range(0.55f, 1.1f);
            var ssr = slash.AddComponent<SpriteRenderer>();
            ssr.sprite       = GerarCorteFantasma();
            ssr.color        = i == 0 ? Color.white : new Color(0.65f, 0.92f, 1f, 0.8f);
            ssr.sortingOrder = 15;
            slash.AddComponent<AutoDestroyFade>().Iniciar(0.18f + delay2);
            Destroy(slash, 0.5f);
        }

        // Anel espectral expansivo
        const int SEGS = 36;
        var anelGO = new GameObject("AnelEsp");
        anelGO.transform.position = pos;
        var lr = anelGO.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 13;
        Destroy(anelGO, 0.5f);

        Vector2 posc = pos;
        float dur = 0.35f;
        for (float tt = 0f; tt < dur; tt += Time.deltaTime)
        {
            if (anelGO == null) break;
            float prog = tt / dur;
            float r2   = Mathf.Lerp(0.1f, 2.2f, prog);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.22f, 0.01f, prog);
            lr.startColor = lr.endColor = new Color(0.55f, 0.9f, 1f, Mathf.Lerp(0.9f, 0f, prog));
            for (int i = 0; i < SEGS; i++)
            {
                float a = i / (float)SEGS * Mathf.PI * 2f;
                lr.SetPosition(i, posc + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r2);
            }
            yield return null;
        }
        if (anelGO != null) Destroy(anelGO);

        // Partículas espectrais em leque
        for (int i = 0; i < 10; i++)
        {
            float a = i / 10f * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
            var p3 = new GameObject("PartImpacto");
            p3.transform.position = pos;
            var psr3 = p3.AddComponent<SpriteRenderer>();
            psr3.sprite = GerarDisco(i < 5 ? 7 : 5);
            psr3.color  = i < 3
                ? Color.white
                : new Color(0.55f, 0.9f, 1f, 0.85f);
            psr3.sortingOrder = 15;
            p3.transform.localScale = Vector3.one * Random.Range(0.08f, 0.22f);
            p3.AddComponent<AutoDestroyFadeMove>().Iniciar(
                new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(2.5f, 6f),
                Random.Range(0.2f, 0.4f));
            Destroy(p3, 0.6f);
        }

        // Marca de corte persistente no chão (fade lento)
        var marca = new GameObject("MarcaCorte");
        marca.transform.position = pos;
        marca.transform.rotation = rot_;
        marca.transform.localScale = Vector3.one * 1.1f;
        var msr = marca.AddComponent<SpriteRenderer>();
        msr.sprite       = GerarCorteFantasma();
        msr.color        = new Color(0.5f, 0.85f, 1f, 0.5f);
        msr.sortingOrder = 11;
        marca.AddComponent<AutoDestroyFade>().Iniciar(0.6f);
        Destroy(marca, 1f);

        // Fade do projétil
        if (sr != null)
        {
            for (float t2 = 0f; t2 < 0.12f; t2 += Time.deltaTime)
            {
                if (sr == null) break;
                float prog = t2 / 0.12f;
                sr.color        = new Color(1f, 1f, 1f, Mathf.Lerp(0.95f, 0f, prog));
                transform.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.3f, prog);
                yield return null;
            }
        }
        if (gameObject != null) Destroy(gameObject);
    }

    // ── Sprites procedurais ───────────────────────────────────────────────────

    static Sprite GerarCorteFantasma()
    {
        const int W = 14, H = 36;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = W * 0.5f;

        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            float nx = Mathf.Abs(x + 0.5f - cx) / cx;
            float ny = y / (float)(H - 1); // 0=base, 1=ponta

            // Formato: mais largo no meio, afila nas pontas
            float larg = Mathf.Sin(ny * Mathf.PI) * (1f - nx);
            larg = Mathf.Max(0f, larg - 0.1f);

            // Brilho na aresta central
            float brilho = Mathf.Max(0f, 0.8f - nx * 4f);
            float a = Mathf.Clamp01(larg * 1.5f + brilho);

            // Gradient: brilhante na ponta, difuso na base
            float foco = Mathf.Lerp(0.6f, 1f, ny);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * foco));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.1f), W);
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
}

