using System.Collections;
using UnityEngine;

public class LancaLuzSkillBehavior : SkillBehavior, ISkillComRecarga, IEvoluivel
{
    float baseDano      = 30f;
    float multiplicador = 1.2f;
    float intervalo     = 8f;
    float velocidade    = 20f;
    float alcance       = 12f;
    float timer;
    public bool  EmRecarga    => timer > 0f;
    public float TimerRecarga => timer;
    public float RecargaTotal => intervalo;

    float DanoAtual => baseDano + (playerStats != null ? playerStats.attack * multiplicador : 0f);

    int qtdLancas = 1;
    public void OnEvolucaoAplicada(SkillEvolutionType tipo) { if (tipo == SkillEvolutionType.LancasMultiplas) qtdLancas = 3; }
    public override void Initialize(PlayerStats stats) => base.Initialize(stats);

    static readonly Color COR_ORIG = new Color(1f, 0.95f, 0.3f);
    Color CorElemento() {
        if (skillData != null && skillData.appliedElement != ElementType.None)
            return ElementRegistry.Instance?.GetCor(skillData.appliedElement) ?? COR_ORIG;
        return COR_ORIG;
    }

    public void ConfigurarDeSkillData(SkillData data)
    {
        this.skillData = data;
        baseDano   = data.attackBonus > 0f        ? data.attackBonus   : 30f;
        intervalo  = data.activationInterval > 0f ? data.activationInterval : 8f;
        velocidade = data.projectileSpeed > 0f    ? data.projectileSpeed    : 20f;
        alcance    = data.specialValue > 0f       ? data.specialValue       : 12f;
        timer      = intervalo;
    }

    void Update()
    {
        if (playerStats == null) return;
        timer -= Time.deltaTime;
        if (timer <= 0f) { timer = intervalo; StartCoroutine(DisparoComEfeito()); }
    }

    public override void ApplyEffect() => StartCoroutine(DisparoComEfeito());

    IEnumerator DisparoComEfeito()
    {
        // Flash dourado no player antes de disparar
        var sr = playerStats?.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = new Color(1f, 0.95f, 0.3f);
            yield return new WaitForSeconds(0.06f);
            if (sr != null) sr.color = Color.white;
        }

        Disparar();
    }

    void Disparar()
    {
        var alvo  = EncontrarMaisProximo();
        Vector2 origem = playerStats.transform.position;
        Vector2 dir = alvo != null
            ? ((Vector2)alvo.transform.position - origem).normalized
            : Vector2.up;

        float[] angulos = qtdLancas > 1
            ? new float[] { -20f, 0f, 20f }
            : new float[] { 0f };

        // Anel de disparo na origem
        StartCoroutine(AnelDisparo(origem));
        SomSkill.Tocar(SomSkill.Tipo.LancaDisparoDark, origem, 0.55f); // co-op: toca tb na cópia cosmética (outro player ouve)

        foreach (float angOffset in angulos)
        {
            float rad = angOffset * Mathf.Deg2Rad;
            Vector2 dirFinal = new Vector2(
                dir.x * Mathf.Cos(rad) - dir.y * Mathf.Sin(rad),
                dir.x * Mathf.Sin(rad) + dir.y * Mathf.Cos(rad));
            var go = new GameObject("LancaLuz");
            go.transform.position = origem;
            var lp = go.AddComponent<LancaLuzProjetil>();
            lp.skillDataRef = skillData;
            lp.cosmetico = cosmetico;
            // Lança do Juízo: +50% de dano (perfuração/explosão ficam no projétil)
            float danoUsar = SkillEvolutionManager.Tem(SkillEvolutionType.LancaLuzLend) ? DanoAtual * 1.5f : DanoAtual;
            lp.Iniciar(dirFinal, velocidade, danoUsar, alcance);
        }
    }

    IEnumerator AnelDisparo(Vector2 pos)
    {
        const int S = 24;
        var go = new GameObject("AnelLanca");
        go.transform.position = pos;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = S;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 10;
        Destroy(go, 0.5f); // failsafe
        float dur = 0.25f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p = t / dur; float r = Mathf.Lerp(0.5f, 0f, p);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.15f, 0.01f, p);
            lr.startColor = lr.endColor = new Color(1f, 0.9f, 0.2f, Mathf.Lerp(0.9f, 0f, p));
            for (int i = 0; i < S; i++) { float a = 360f / S * i * Mathf.Deg2Rad; lr.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r); }
            yield return null;
        }
        if (go != null) Destroy(go);
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
            if (d < menorDist) { menorDist = d; melhor = ic; }
        }
        return melhor;
    }
}

public class LancaLuzProjetil : MonoBehaviour
{
    public SkillData skillDataRef;
    public bool      cosmetico; // co-op: fantasma do colega — só visual, sem dano
    Vector2 dir, origem;
    float   vel, dano, alcance;
    bool    atingiu;
    float   rot;
    SpriteRenderer sr;
    SpriteRenderer srBrilho;

    static readonly Color COR_ORIG = new Color(1f, 0.95f, 0.4f);
    Color CorEl()
    {
        if (skillDataRef != null && skillDataRef.appliedElement != ElementType.None)
            return ElementRegistry.Instance != null ? ElementRegistry.Instance.GetCor(skillDataRef.appliedElement) : COR_ORIG;
        return COR_ORIG;
    }

    public void Iniciar(Vector2 d, float v, float dmg, float alc)
    {
        dir = d; vel = v; dano = dmg; alcance = alc; origem = transform.position;
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang - 90f);

        // Sprite principal da lança
        Color cel = CorEl();
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = GerarLanca(); sr.color = new Color(cel.r, cel.g, cel.b); sr.sortingOrder = 14;
        transform.localScale = new Vector3(0.55f, 1.4f, 1f);

        // Brilho interno branco
        var glowGO = new GameObject("Brilho");
        glowGO.transform.SetParent(transform, false);
        srBrilho = glowGO.AddComponent<SpriteRenderer>();
        srBrilho.sprite = GerarLanca(); srBrilho.color = new Color(1f, 1f, 0.9f, 0.6f); srBrilho.sortingOrder = 15;
        glowGO.transform.localScale = new Vector3(0.5f, 0.6f, 1f);

        var col = gameObject.AddComponent<CapsuleCollider2D>();
        col.isTrigger = true; col.size = new Vector2(0.25f, 1.4f);
        Destroy(gameObject, 1.2f);
        StartCoroutine(Trail());
        StartCoroutine(PulsoBrilho());
    }

    void Update()
    {
        if (atingiu) return;
        transform.position += (Vector3)(dir * vel * Time.deltaTime);

        // Partícula de energia ao longo do voo
        if (Time.frameCount % 4 == 0) SpawnParticulaVoo();

        if (Vector2.Distance(transform.position, origem) >= alcance)
        {
            SpawnImpacto();
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (atingiu) return;
        var ic = other.GetComponent<InimigoController>() ?? other.GetComponentInParent<InimigoController>();
        if (ic == null) return;
        if (!cosmetico) // co-op: cópia cosmética não aplica dano (só visual)
        {
            ic.ReceberDano(dano, false);
            SkillElementEffect.Aplicar(skillDataRef, ic.gameObject, dano, this);
            if (SkillEvolutionManager.Tem(SkillEvolutionType.LancaExplosiva)
                || SkillEvolutionManager.Tem(SkillEvolutionType.LancaLuzLend)) // Lança do Juízo: também explode
            {
                EvolutionFX.SpawnExplosao(transform.position, 2.5f, dano * 0.6f, new Color(1f, 0.9f, 0.3f), this);
                SomSkill.Tocar(SomSkill.Tipo.MissilExplosaoDark, transform.position, 0.55f);
            }
            else
                SomSkill.Tocar(SomSkill.Tipo.LancaImpactoDark, transform.position, 0.5f);
        }
        bool perfura = SkillEvolutionManager.Tem(SkillEvolutionType.LancaPerfurante)
                    || SkillEvolutionManager.Tem(SkillEvolutionType.LancaLuzLend); // Lança do Juízo: perfura
        if (!perfura) atingiu = true;
        SpawnImpacto();
        if (!perfura) StartCoroutine(FadeOut());
    }

    IEnumerator PulsoBrilho()
    {
        float t = 0f;
        while (!atingiu && gameObject != null)
        {
            t += Time.deltaTime * 8f;
            float p = Mathf.Sin(t) * 0.5f + 0.5f;
            Color cel = CorEl();
            if (sr != null) sr.color = Color.Lerp(cel, Color.white, p * 0.45f);
            if (srBrilho != null) srBrilho.color = new Color(1f, 1f, 0.9f, 0.3f + p * 0.4f);
            yield return null;
        }
    }

    IEnumerator FadeOut()
    {
        // Fade rápido no impacto
        for (float t = 0f; t < 0.08f; t += Time.deltaTime)
        {
            if (sr == null) yield break;
            float p = t / 0.08f;
            sr.color = new Color(1f, 0.95f, 0.4f, Mathf.Lerp(1f, 0f, p));
            yield return null;
        }
        Destroy(gameObject);
    }

    IEnumerator Trail()
    {
        while (!atingiu && gameObject != null)
        {
            // Trail duplo: lança + brilho
            var t = new GameObject("T");
            t.transform.position = transform.position;
            t.transform.rotation = transform.rotation;
            t.transform.localScale = transform.localScale;
            Color celT = CorEl();
            var tsr = t.AddComponent<SpriteRenderer>();
            tsr.sprite = GerarLanca();
            tsr.color = new Color(celT.r, celT.g * 0.85f, celT.b * 0.6f, 0.4f); tsr.sortingOrder = 13;
            t.AddComponent<AutoDestroyFade>().Iniciar(0.1f);

            // Trail de luz branca menor
            var t2 = new GameObject("T2");
            t2.transform.position = transform.position;
            t2.transform.rotation = transform.rotation;
            t2.transform.localScale = new Vector3(0.25f, 0.8f, 1f);
            var tsr2 = t2.AddComponent<SpriteRenderer>();
            tsr2.sprite = GerarLanca();
            tsr2.color = new Color(1f, 1f, 0.9f, 0.25f); tsr2.sortingOrder = 14;
            t2.AddComponent<AutoDestroyFade>().Iniciar(0.07f);

            yield return new WaitForSeconds(0.04f);
        }
    }

    void SpawnParticulaVoo()
    {
        var p = new GameObject("PVoo");
        p.transform.position = (Vector2)transform.position + Random.insideUnitCircle * 0.15f;
        var psr = p.AddComponent<SpriteRenderer>();
        psr.sprite = GerarDisco(5);
        psr.color = new Color(1f, 0.9f, 0.3f, 0.7f); psr.sortingOrder = 13;
        p.transform.localScale = Vector3.one * Random.Range(0.06f, 0.14f);
        p.AddComponent<AutoDestroyFadeMove>().Iniciar(Random.insideUnitCircle * 0.5f, 0.15f);
        Destroy(p, 0.3f);
    }

    void SpawnImpacto()
    {
        // Anel expansivo — animado no próprio GO (independente da vida da lança)
        var anelGO = new GameObject("AnelImpactoLanca");
        anelGO.transform.position = transform.position;
        anelGO.AddComponent<AnelExpansivoLancaInner>().Iniciar(new Color(1f, 0.9f, 0.2f), 0.35f, 2.5f);
        Destroy(anelGO, 0.5f); // failsafe

        // Cruz de luz
        for (int i = 0; i < 4; i++)
        {
            var ray = new GameObject("RayImpacto");
            ray.transform.position = transform.position;
            ray.transform.rotation = Quaternion.Euler(0f, 0f, i * 90f);
            var rsr = ray.AddComponent<SpriteRenderer>();
            rsr.sprite = GerarLanca(); rsr.color = new Color(1f, 0.95f, 0.4f, 0.9f); rsr.sortingOrder = 16;
            ray.transform.localScale = new Vector3(0.3f, 1.2f, 1f);
            ray.AddComponent<AutoDestroyFade>().Iniciar(0.2f);
            Destroy(ray, 0.4f);
        }

        // Partículas
        for (int i = 0; i < 12; i++)
        {
            var p = new GameObject("P");
            p.transform.position = transform.position;
            var psr = p.AddComponent<SpriteRenderer>();
            psr.sprite = GerarDisco(6);
            psr.color = i < 6 ? new Color(1f, 0.95f, 0.3f) : Color.white;
            psr.sortingOrder = 15;
            p.transform.localScale = Vector3.one * Random.Range(0.1f, 0.25f);
            p.AddComponent<AutoDestroyFadeMove>().Iniciar(Random.insideUnitCircle.normalized * Random.Range(3f, 7f), 0.4f);
            Destroy(p, 0.6f);
        }
    }

    // Anima o anel de impacto em seu próprio GameObject — não depende da lança
    public class AnelExpansivoLancaInner : MonoBehaviour
    {
        public void Iniciar(Color cor, float dur, float raioMax) => StartCoroutine(Animar(cor, dur, raioMax));
        IEnumerator Animar(Color cor, float dur, float raioMax)
        {
            const int S = 40;
            var lr = gameObject.AddComponent<LineRenderer>();
            lr.useWorldSpace = true; lr.loop = true; lr.positionCount = S;
            lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 13;
            Vector2 pos = transform.position;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                if (this == null) yield break;
                float p2 = t / dur; float r = Mathf.Lerp(0.2f, raioMax, p2);
                lr.startWidth = lr.endWidth = Mathf.Lerp(0.25f, 0.02f, p2);
                lr.startColor = lr.endColor = new Color(cor.r, cor.g, cor.b, Mathf.Lerp(1f, 0f, p2));
                for (int i = 0; i < S; i++) { float a = 360f / S * i * Mathf.Deg2Rad; lr.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r); }
                yield return null;
            }
            Destroy(gameObject);
        }
    }

    static Sprite GerarLanca()
    {
        int w = 10, h = 32; var tex = new Texture2D(w, h, TextureFormat.RGBA32, false); tex.filterMode = FilterMode.Bilinear;
        float cx = w * 0.5f;
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
        {
            float nx = Mathf.Abs(x + 0.5f - cx) / cx; float ny = y / (float)(h - 1);
            float larg = ny < 0.75f ? (1f - nx) * (1f - ny * 0.25f) : (1f - nx) * (1f - ny) * 4f;
            float brilho = Mathf.Max(0f, 0.8f - nx * 6f);
            float a = Mathf.Clamp01(larg * 1.3f + brilho);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.08f), w);
    }

    static Sprite GerarDisco(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false); tex.filterMode = FilterMode.Bilinear; float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++) { float d = Vector2.Distance(new Vector2(x+0.5f,y+0.5f),new Vector2(cx,cx)); tex.SetPixel(x,y,new Color(1f,1f,1f,d<cx?1f:0f)); }
        tex.Apply(); return Sprite.Create(tex, new Rect(0,0,sz,sz), new Vector2(0.5f,0.5f), sz);
    }
}
