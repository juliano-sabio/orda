using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FuriaLaminasSkillBehavior : SkillBehavior, ISkillComRecarga
{
    float baseDano      = 8f;
    float multiplicador = 0.4f;
    float intervalo     = 5f;
    int   qtdLaminas    = 5;
    float velocidade    = 14f;
    float raioDeteccao  = 12f;

    float timer;
    public bool  EmRecarga    => timer > 0f;
    public float TimerRecarga => timer;
    public float RecargaTotal => intervalo;

    float DanoAtual => baseDano + (playerStats != null ? playerStats.attack * multiplicador : 0f);

    public override void Initialize(PlayerStats stats) => base.Initialize(stats);

    static readonly Color COR_ORIG = new Color(0.9f, 0.9f, 1f);
    Color CorElemento() {
        if (skillData != null && skillData.appliedElement != ElementType.None)
            return ElementRegistry.Instance?.GetCor(skillData.appliedElement) ?? COR_ORIG;
        return COR_ORIG;
    }

    public void ConfigurarDeSkillData(SkillData data)
    {
        this.skillData = data;
        baseDano    = data.attackBonus > 0f        ? data.attackBonus        : 30f;
        intervalo   = data.activationInterval > 0f ? data.activationInterval : 2.5f;
        qtdLaminas  = data.projectileCount > 0     ? data.projectileCount    : 5;
        velocidade  = data.projectileSpeed > 0f    ? data.projectileSpeed    : 14f;
        timer       = intervalo;
    }

    void Update()
    {
        if (playerStats == null) return;
        timer -= Time.deltaTime;
        if (timer <= 0f) { timer = intervalo; DispararLaminas(); }
    }

    public override void ApplyEffect() => DispararLaminas();

    // ── Lógica ────────────────────────────────────────────────────────────────

    void DispararLaminas()
    {
        int qtdReal = SkillEvolutionManager.Tem(SkillEvolutionType.LaminasDuplas) ? qtdLaminas * 2 : qtdLaminas;
        var alvos  = EncontrarAlvos(qtdReal);
        Vector2 origem = playerStats.transform.position;

        SomSkill.Tocar(SomSkill.Tipo.LaminaFuriaDark, origem, 0.5f); // co-op: toca tb na cópia cosmética (outro player ouve)

        for (int i = 0; i < alvos.Count; i++)
        {
            Vector2 dir = alvos[i] != null
                ? ((Vector2)alvos[i].transform.position - origem).normalized
                : AnguloAleatorio(i, alvos.Count);

            var go = new GameObject("Lamina");
            go.transform.position = origem;
            var lp = go.AddComponent<LaminaProjetil>();
            lp.skillDataRef = skillData;
            lp.cosmetico = cosmetico;
            lp.bumerangue = TemEvolucao(SkillEvolutionType.FuriaLaminasLend); // Lâminas Bumerangue (usa evolução replicada no co-op)
            lp.Iniciar(dir, velocidade, DanoAtual);
        }

        // Se há menos alvos que lâminas, completa com direções aleatórias
        for (int i = alvos.Count; i < qtdReal; i++)
        {
            Vector2 dir = AnguloAleatorio(i, qtdLaminas);
            var go = new GameObject("Lamina");
            go.transform.position = origem;
            var lp = go.AddComponent<LaminaProjetil>();
            lp.skillDataRef = skillData;
            lp.cosmetico = cosmetico;
            lp.bumerangue = TemEvolucao(SkillEvolutionType.FuriaLaminasLend); // Lâminas Bumerangue (usa evolução replicada no co-op)
            lp.Iniciar(dir, velocidade, DanoAtual);
        }

        StartCoroutine(FlashPlayer());
    }

    Vector2 AnguloAleatorio(int idx, int total)
    {
        float ang = (360f / total * idx + Random.Range(-15f, 15f)) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
    }

    IEnumerator FlashPlayer()
    {
        var sr = playerStats?.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;
        sr.color = new Color(0.9f, 0.9f, 1f);
        yield return new WaitForSeconds(0.06f);
        if (sr != null) sr.color = Color.white;
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

// ── Projétil da lâmina ────────────────────────────────────────────────────────

public class LaminaProjetil : MonoBehaviour
{
    public bool cosmetico; // co-op: fantasma do colega — só visual, sem dano
    public bool bumerangue; // Lâminas Bumerangue: volta ao fim do alcance, cortando de novo
    bool voltando;
    readonly System.Collections.Generic.HashSet<int> jaAtingidos = new System.Collections.Generic.HashSet<int>();

    public SkillData skillDataRef;
    Vector2 dir;
    float   vel;
    float   dano;
    float   rot;
    bool    atingiu;
    Vector2 origem;
    const float ALCANCE_MAX = 7f;

    SpriteRenderer sr;

    static readonly Color COR_ORIG = new Color(0.85f, 0.92f, 1f);
    Color CorEl()
    {
        if (skillDataRef != null && skillDataRef.appliedElement != ElementType.None)
            return ElementRegistry.Instance != null ? ElementRegistry.Instance.GetCor(skillDataRef.appliedElement) : COR_ORIG;
        return COR_ORIG;
    }

    public void Iniciar(Vector2 direcao, float velocidade, float dmg)
    {
        dir    = direcao;
        vel    = velocidade;
        dano   = dmg;
        origem = transform.position;

        Color cel = CorEl();
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite       = GerarLamina();
        sr.color        = new Color(cel.r, cel.g, cel.b);
        sr.sortingOrder = 12;
        transform.localScale = Vector3.one * 0.55f;

        // Rotaciona para apontar na direção
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang - 90f);

        var col = gameObject.AddComponent<CapsuleCollider2D>();
        col.isTrigger  = true;
        col.size       = new Vector2(0.2f, 0.6f);

        // Failsafe: destrói após um tempo independente de qualquer coisa (bumerangue precisa de mais)
        Destroy(gameObject, bumerangue ? 2.6f : 1.5f);
    }

    void Update()
    {
        if (atingiu) return;

        transform.position += (Vector3)(dir * vel * Time.deltaTime);

        rot += Time.deltaTime * 720f;
        transform.rotation = Quaternion.Euler(0f, 0f, rot);

        if (Time.frameCount % 3 == 0) SpawnRastro();

        float distOrigem = Vector2.Distance(transform.position, origem);
        if (bumerangue)
        {
            // No fim do alcance, inverte e volta cortando de novo (reseta quem já cortou nesta passada)
            if (!voltando && distOrigem >= ALCANCE_MAX) { voltando = true; dir = -dir; jaAtingidos.Clear(); }
            else if (voltando && distOrigem <= 0.6f) Destroy(gameObject);
        }
        else if (distOrigem >= ALCANCE_MAX)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (atingiu) return;
        var ic = other.GetComponent<InimigoController>()
              ?? other.GetComponentInParent<InimigoController>();
        if (ic == null) return;

        // Bumerangue: atravessa e corta na ida e na volta (sem repetir na mesma passada), não destrói
        if (bumerangue)
        {
            int id = ic.gameObject.GetInstanceID();
            if (jaAtingidos.Contains(id)) return;
            jaAtingidos.Add(id);
            if (!cosmetico)
            {
                ic.ReceberDano(dano, false);
                SkillElementEffect.Aplicar(skillDataRef, ic.gameObject, dano, this);
                if (SkillEvolutionManager.Tem(SkillEvolutionType.LaminasSangrentas))
                    EvolutionFX.AplicarVeneno(ic, dano * 0.25f, 2.5f);
                if (Random.value < 0.35f) SomSkill.Tocar(SomSkill.Tipo.LaminaImpactoDark, transform.position, 0.3f);
            }
            return;
        }

        if (!cosmetico) // co-op: cópia cosmética só faz o visual
        {
            ic.ReceberDano(dano, false);
            SkillElementEffect.Aplicar(skillDataRef, ic.gameObject, dano, this);
            if (SkillEvolutionManager.Tem(SkillEvolutionType.LaminasExplosivas))
                EvolutionFX.SpawnExplosao(transform.position, 1.5f, dano * 0.5f, new Color(0.85f, 0.92f, 1f), this);
            if (SkillEvolutionManager.Tem(SkillEvolutionType.LaminasSangrentas))
                EvolutionFX.AplicarVeneno(ic, dano * 0.25f, 2.5f); // Lâminas Sangrentas: sangramento (DoT)
            if (Random.value < 0.35f) SomSkill.Tocar(SomSkill.Tipo.LaminaImpactoDark, transform.position, 0.3f);
        }
        atingiu = true;
        StartCoroutine(EfeitoImpacto());
    }

    IEnumerator EfeitoImpacto()
    {
        // Faíscas — usam AutoDestroyFadeMove para não depender deste componente
        for (int i = 0; i < 5; i++)
        {
            var p = new GameObject("FaiscaLamina");
            p.transform.position = transform.position;
            var psr = p.AddComponent<SpriteRenderer>();
            psr.sprite = GerarDisco(6);
            psr.color  = new Color(0.85f, 0.92f, 1f);
            psr.sortingOrder = 13;
            p.transform.localScale = Vector3.one * Random.Range(0.1f, 0.2f);
            Vector2 v = Random.insideUnitCircle.normalized * Random.Range(2f, 5f);
            p.AddComponent<AutoDestroyFadeMove>().Iniciar(v, 0.3f);
            Destroy(p, 0.6f); // failsafe
        }

        if (sr != null)
        {
            sr.color = Color.white;
            float d = 0.15f;
            for (float t = 0f; t < d; t += Time.deltaTime)
            {
                if (sr == null) yield break;
                sr.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, t / d));
                transform.localScale = Vector3.one * Mathf.Lerp(0.55f, 0.9f, t / d);
                yield return null;
            }
        }
        Destroy(gameObject);
    }

    void SpawnRastro()
    {
        var p = new GameObject("RastroLamina");
        p.transform.position   = transform.position;
        p.transform.rotation   = transform.rotation;
        p.transform.localScale = transform.localScale * 0.7f;
        Color cel = CorEl();
        var psr = p.AddComponent<SpriteRenderer>();
        psr.sprite       = GerarLamina();
        psr.color        = new Color(cel.r * 0.7f, cel.g * 0.85f, cel.b, 0.45f);
        psr.sortingOrder = 11;
        // Usa componente self-managed para não depender do LaminaProjetil
        p.AddComponent<AutoDestroyFade>().Iniciar(0.12f);
    }


    // ── Sprites procedurais ───────────────────────────────────────────────────

    static Sprite GerarLamina()
    {
        const int W = 8, H = 24;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        float cx = W * 0.5f, cy = H * 0.5f;
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            float nx = Mathf.Abs(x + 0.5f - cx) / cx;       // 0=centro, 1=borda
            float ny = Mathf.Abs(y + 0.5f - cy) / cy;       // 0=meio, 1=ponta
            // Forma de lâmina: estreita nas pontas, larga no centro
            float larg = Mathf.Lerp(1f, 0f, Mathf.Pow(ny, 0.6f));
            float a = nx < larg ? Mathf.Lerp(1f, 0.3f, nx / larg) : 0f;
            // Brilho na aresta central
            a += Mathf.Max(0f, 0.8f - nx * 6f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), W);
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

// Self-managed: faz fade e se destrói sem depender de componente externo
public class AutoDestroyFade : MonoBehaviour
{
    public void Iniciar(float duracao) => StartCoroutine(Fade(duracao));

    System.Collections.IEnumerator Fade(float dur)
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) { Destroy(gameObject); yield break; }
        Color c = sr.color;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (sr == null) break;
            sr.color = new Color(c.r, c.g, c.b, Mathf.Lerp(c.a, 0f, t / dur));
            yield return null;
        }
        Destroy(gameObject);
    }
}

