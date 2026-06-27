using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SombrasCruzSkillBehavior : SkillBehavior, ISkillComRecarga
{
    float baseDano      = 12f;
    float multiplicador = 0.6f;
    float intervalo     = 6f;
    float velocidade    = 16f;
    float alcance       = 8f;

    float timer;
    public bool  EmRecarga    => timer > 0f;
    public float TimerRecarga => timer;
    public float RecargaTotal => intervalo;

    float DanoAtual => baseDano + (playerStats != null ? playerStats.attack * multiplicador : 0f);

    public override void Initialize(PlayerStats stats) => base.Initialize(stats);

    public void ConfigurarDeSkillData(SkillData data)
    {
        this.skillData = data;
        baseDano    = data.attackBonus > 0f          ? data.attackBonus        : 25f;
        intervalo   = data.activationInterval > 0f   ? data.activationInterval : 3f;
        velocidade  = data.projectileSpeed > 0f      ? data.projectileSpeed    : 16f;
        alcance     = data.specialValue > 0f         ? data.specialValue       : 8f;
        timer       = intervalo;
    }

    static readonly Color COR_ORIG = new Color(0.55f, 0.25f, 1f);
    Color CorElemento() {
        if (skillData != null && skillData.appliedElement != ElementType.None)
            return ElementRegistry.Instance?.GetCor(skillData.appliedElement) ?? COR_ORIG;
        return COR_ORIG;
    }

    void Update()
    {
        if (playerStats == null) return;
        timer -= Time.deltaTime;
        if (timer <= 0f) { timer = intervalo; Disparar(); }
    }

    public override void ApplyEffect() => Disparar();

    bool somImpactoTocado; // limita 1 som de impacto por disparo (evita spam dos 4-8 feixes)

    void Disparar()
    {
        somImpactoTocado = false;
        if (!cosmetico && playerStats != null)
            SomSkill.Tocar(SomSkill.Tipo.SombraCruzDisparoDark, playerStats.transform.position, 0.5f);

        Vector2[] direcoes = SkillEvolutionManager.Tem(SkillEvolutionType.CruzDupla)
            ? new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right,
                      new Vector2(1,1).normalized, new Vector2(-1,1).normalized,
                      new Vector2(1,-1).normalized, new Vector2(-1,-1).normalized }
            : new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        foreach (var dir in direcoes)
            StartCoroutine(PulsoBeam(dir));

        StartCoroutine(FlashPlayer());
        StartCoroutine(AnelDisparo());
    }

    // Anel expansivo no player ao disparar
    IEnumerator AnelDisparo()
    {
        if (playerStats == null) yield break;
        Color ce = CorElemento();

        // Disco de impacto central
        var disco = new GameObject("SombraCruzDisco");
        var dsr = disco.AddComponent<SpriteRenderer>();
        dsr.sprite       = SombraCruzProjetil.GerarDiscoPub(24);
        dsr.sortingOrder = 12;
        disco.transform.position   = playerStats.transform.position;
        disco.transform.localScale = Vector3.one * 0.1f;

        for (float t = 0f; t < 0.22f; t += Time.deltaTime)
        {
            if (disco == null || playerStats == null) yield break;
            float p = t / 0.22f;
            float escala = Mathf.Lerp(0.1f, 2.2f, p);
            disco.transform.localScale    = Vector3.one * escala;
            disco.transform.position      = playerStats.transform.position;
            dsr.color = new Color(ce.r, ce.g, ce.b, Mathf.Lerp(0.7f, 0f, p));
            yield return null;
        }
        if (disco != null) Destroy(disco);

        // Faíscas radiais
        for (int i = 0; i < 8; i++)
        {
            float ang = i * 45f * Mathf.Deg2Rad;
            var p = new GameObject("SombraCruzFaisca");
            p.transform.position = (Vector3)playerStats.transform.position + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang)) * 0.3f;
            var psr = p.AddComponent<SpriteRenderer>();
            psr.sprite       = SombraCruzProjetil.GerarDiscoPub(8);
            psr.color        = new Color(ce.r, ce.g, ce.b, 0.9f);
            psr.sortingOrder = 13;
            p.transform.localScale = Vector3.one * Random.Range(0.15f, 0.28f);
            Vector2 v = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(3f, 6f);
            p.AddComponent<AutoDestroyFadeMove>().Iniciar(v, 0.35f);
        }
    }

    IEnumerator PulsoBeam(Vector2 dir)
    {
        float alcanceReal = SkillEvolutionManager.Tem(SkillEvolutionType.SombrasPerfurantes) ? alcance * 2f : alcance;
        float dur = alcanceReal / velocidade;

        // Glow externo (mais largo, semi-transparente)
        var goGlow = new GameObject("SombraCruzGlow");
        var lrG = goGlow.AddComponent<LineRenderer>();
        lrG.useWorldSpace  = true;
        lrG.positionCount  = 2;
        lrG.material       = new Material(Shader.Find("Sprites/Default"));
        lrG.sortingOrder   = 12;
        lrG.numCapVertices = 6;

        // Núcleo brilhante (fino e opaco)
        var go = new GameObject("SombraCruzBeam");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace  = true;
        lr.positionCount  = 2;
        lr.material       = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder   = 13;
        lr.numCapVertices = 6;

        var hashset = new System.Collections.Generic.HashSet<int>();
        float particleTimer = 0f;

        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null || playerStats == null) yield break;

            float prog      = t / dur;
            float distAtual = Mathf.Lerp(0f, alcanceReal, prog);
            // Curva de aceleração: rápido no início, desacelera no fim
            float ease      = 1f - Mathf.Pow(1f - prog, 2f);

            Vector2 origem = playerStats.transform.position;
            Vector2 ponta  = origem + dir * distAtual;

            // Núcleo
            lr.SetPosition(0, origem);
            lr.SetPosition(1, ponta);
            float largCore = Mathf.Lerp(0.22f, 0.04f, ease);
            Color ce = CorElemento();
            Color corCore = new Color(ce.r * 1.4f, ce.g * 1.4f, ce.b * 1.4f, Mathf.Lerp(1f, 0.2f, prog));
            lr.startWidth = largCore;
            lr.endWidth   = largCore * 0.15f;
            lr.startColor = corCore;
            lr.endColor   = new Color(1f, 1f, 1f, Mathf.Lerp(0.8f, 0f, prog));

            // Glow
            lrG.SetPosition(0, origem);
            lrG.SetPosition(1, ponta);
            float largGlow = largCore * 3.5f;
            Color corGlow = new Color(ce.r, ce.g, ce.b, Mathf.Lerp(0.35f, 0f, prog));
            lrG.startWidth = largGlow;
            lrG.endWidth   = largGlow * 0.2f;
            lrG.startColor = corGlow;
            lrG.endColor   = new Color(ce.r, ce.g, ce.b, 0f);

            // Partículas laterais periódicas
            particleTimer -= Time.deltaTime;
            if (particleTimer <= 0f && distAtual > 0.5f)
            {
                particleTimer = 0.04f;
                Vector2 perp = new Vector2(-dir.y, dir.x);
                for (int s = -1; s <= 1; s += 2)
                {
                    var pt = new GameObject("SombraCruzPart");
                    pt.transform.position = (Vector3)(origem + dir * distAtual * Random.Range(0.1f, 0.9f));
                    var ptsr = pt.AddComponent<SpriteRenderer>();
                    ptsr.sprite       = SombraCruzProjetil.GerarDiscoPub(6);
                    ptsr.color        = new Color(ce.r, ce.g, ce.b, Random.Range(0.5f, 0.85f));
                    ptsr.sortingOrder = 12;
                    pt.transform.localScale = Vector3.one * Random.Range(0.06f, 0.15f);
                    Vector2 v = perp * s * Random.Range(1.5f, 3.5f) + dir * Random.Range(-1f, 1f);
                    pt.AddComponent<AutoDestroyFadeMove>().Iniciar(v, Random.Range(0.18f, 0.32f));
                }
            }

            // Dano
            var hits = Physics2D.OverlapCapsuleAll(
                origem + dir * distAtual * 0.5f,
                new Vector2(largCore, distAtual),
                dir.x != 0 ? CapsuleDirection2D.Horizontal : CapsuleDirection2D.Vertical,
                dir.x != 0 ? 0f : 90f);
            foreach (var col in hits)
            {
                var ic = col.GetComponent<InimigoController>()
                      ?? col.GetComponentInParent<InimigoController>();
                if (ic == null || ic.estaMorrendo) continue;
                int id = ic.gameObject.GetInstanceID();
                if (hashset.Contains(id)) continue;
                hashset.Add(id);
                if (!cosmetico)
                {
                    ic.ReceberDano(DanoAtual, false);
                    SkillElementEffect.Aplicar(skillData, ic.gameObject, DanoAtual, this);
                    if (!somImpactoTocado) { somImpactoTocado = true; SomSkill.Tocar(SomSkill.Tipo.SombraCruzImpactoDark, ic.transform.position, 0.35f); }
                }
            }

            yield return null;
        }

        // Explosão no fim do raio
        if (playerStats != null)
            StartCoroutine(ExplosaoFim(playerStats.transform.position + (Vector3)(dir * alcanceReal), CorElemento()));

        if (go     != null) Destroy(go);
        if (goGlow != null) Destroy(goGlow);
    }

    IEnumerator ExplosaoFim(Vector3 pos, Color ce)
    {
        for (int i = 0; i < 6; i++)
        {
            float ang = i * 60f * Mathf.Deg2Rad + Random.Range(0f, 30f) * Mathf.Deg2Rad;
            var p = new GameObject("SombraCruzImpacto");
            p.transform.position = pos;
            var psr = p.AddComponent<SpriteRenderer>();
            psr.sprite       = SombraCruzProjetil.GerarDiscoPub(8);
            psr.color        = new Color(ce.r, ce.g, ce.b, 0.9f);
            psr.sortingOrder = 13;
            p.transform.localScale = Vector3.one * Random.Range(0.1f, 0.22f);
            Vector2 v = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(2f, 5f);
            p.AddComponent<AutoDestroyFadeMove>().Iniciar(v, 0.4f);
        }

        // Anel de impacto pequeno
        var anel = new GameObject("SombraCruzAnelFim");
        var asr = anel.AddComponent<SpriteRenderer>();
        asr.sprite       = SombraCruzProjetil.GerarDiscoPub(20);
        asr.sortingOrder = 12;
        anel.transform.position = pos;

        for (float t = 0f; t < 0.18f; t += Time.deltaTime)
        {
            if (anel == null) yield break;
            float p2 = t / 0.18f;
            anel.transform.localScale = Vector3.one * Mathf.Lerp(0.05f, 1.2f, p2);
            asr.color = new Color(ce.r, ce.g, ce.b, Mathf.Lerp(0.6f, 0f, p2));
            yield return null;
        }
        if (anel != null) Destroy(anel);
    }

    IEnumerator FlashPlayer()
    {
        var sr = playerStats?.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;
        Color orig = sr.color;
        Color ce = CorElemento();
        // Dois pulsos rápidos
        for (int i = 0; i < 2; i++)
        {
            if (sr != null) sr.color = new Color(ce.r * 1.5f, ce.g * 1.5f, ce.b * 1.5f);
            yield return new WaitForSeconds(0.05f);
            if (sr != null) sr.color = orig;
            yield return new WaitForSeconds(0.04f);
        }
    }
}

// ── Projétil que atravessa inimigos ──────────────────────────────────────────

public class SombraCruzProjetil : MonoBehaviour
{
    Vector2        dir;
    float          vel;
    float          dano;
    float          alcance;
    Vector2        origem;
    HashSet<int>   atingidos = new HashSet<int>();

    SpriteRenderer sr;
    SkillData      skillDataProj;
    Color          corBase = new Color(0.55f, 0.25f, 1f);

    public void Iniciar(Vector2 direcao, float velocidade, float dmg, float alc,
                        SkillData sd = null, Color cor = default)
    {
        dir     = direcao;
        vel     = velocidade;
        dano    = dmg;
        alcance = alc;
        origem  = transform.position;
        skillDataProj = sd;
        if (cor != default && cor != Color.white) corBase = cor;

        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang - 90f);

        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite       = GerarCorte();
        sr.color        = corBase;
        sr.sortingOrder = 13;

        var col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size      = new Vector2(0.5f, 1.2f);

        Destroy(gameObject, 2f);
    }

    void Update()
    {
        transform.position += (Vector3)(dir * vel * Time.deltaTime);

        // Fade progressivo
        if (sr != null)
        {
            float dist  = Vector2.Distance(transform.position, origem);
            float prog  = Mathf.Clamp01(dist / alcance);
            sr.color    = new Color(corBase.r, corBase.g, corBase.b, Mathf.Lerp(1f, 0f, prog));
            float escX  = Mathf.Lerp(1f, 0.3f, prog);
            transform.localScale = new Vector3(escX, 1f, 1f);
        }

        // Destrói ao atingir o alcance
        if (Vector2.Distance(transform.position, origem) >= alcance)
        {
            SpawnParticulasFim();
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var ic = other.GetComponent<InimigoController>()
              ?? other.GetComponentInParent<InimigoController>();
        if (ic == null) return;

        int id = ic.gameObject.GetInstanceID();
        if (atingidos.Contains(id)) return; // evita acertar o mesmo 2x
        atingidos.Add(id);

        ic.ReceberDano(dano, false);
        SkillElementEffect.Aplicar(skillDataProj, ic.gameObject, dano, this);
        StartCoroutine(FlashImpacto(ic));
    }

    IEnumerator FlashImpacto(InimigoController ic)
    {
        var isr = ic?.GetComponent<SpriteRenderer>();
        if (isr == null) yield break;
        Color orig = isr.color;
        isr.color = new Color(0.7f, 0.4f, 1f);
        yield return new WaitForSeconds(0.08f);
        if (isr != null) isr.color = orig;
    }

    void SpawnParticulasFim()
    {
        for (int i = 0; i < 4; i++)
        {
            var p = new GameObject("PartSombra");
            p.transform.position = transform.position;
            var psr = p.AddComponent<SpriteRenderer>();
            psr.sprite = GerarDisco(6);
            psr.color  = corBase;
            psr.sortingOrder = 12;
            p.transform.localScale = Vector3.one * Random.Range(0.1f, 0.22f);
            Vector2 v = dir * Random.Range(1f, 3f) + (Vector2)Random.insideUnitCircle * 1.5f;
            p.AddComponent<AutoDestroyFadeMove>().Iniciar(v, 0.3f);
            Destroy(p, 0.6f); // failsafe
        }
    }

    // ── Sprites procedurais ───────────────────────────────────────────────────

    static Sprite GerarCorte()
    {
        const int W = 10, H = 32;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = W * 0.5f, cy = H * 0.5f;
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            float nx = Mathf.Abs(x + 0.5f - cx) / cx;
            float ny = (y + 0.5f - cy) / cy; // -1=base, 1=ponta

            // Afila na frente (y positivo = ponta)
            float larg = Mathf.Clamp01(1f - Mathf.Pow(Mathf.Max(0f, ny), 0.5f));
            // Arredonda na base
            larg = Mathf.Min(larg, Mathf.Clamp01(1f - Mathf.Pow(Mathf.Max(0f, -ny - 0.5f), 1.5f)));

            float a = nx < larg ? (1f - nx / Mathf.Max(larg, 0.01f)) : 0f;
            // Brilho central
            a += Mathf.Max(0f, 0.6f - nx * 5f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.2f), W);
    }

    static Sprite GerarDisco(int sz) => GerarDiscoPub(sz);

    public static Sprite GerarDiscoPub(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cx));
            float a = d < cx ? Mathf.Clamp01(1f - (d / cx) * 0.6f) : 0f; // gradiente suave
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }
}

// Self-managed com movimento
public class AutoDestroyFadeMove : MonoBehaviour
{
    public void Iniciar(Vector2 vel, float dur) => StartCoroutine(Run(vel, dur));

    System.Collections.IEnumerator Run(Vector2 vel, float dur)
    {
        var sr = GetComponent<SpriteRenderer>();
        Color c = sr != null ? sr.color : Color.white;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            vel *= Mathf.Pow(0.85f, Time.deltaTime * 60f);
            transform.position += (Vector3)(vel * Time.deltaTime);
            if (sr != null) sr.color = new Color(c.r, c.g, c.b, Mathf.Lerp(1f, 0f, t / dur));
            yield return null;
        }
        Destroy(gameObject);
    }
}

