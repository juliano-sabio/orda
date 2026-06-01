using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SombrasCruzSkillBehavior : SkillBehavior
{
    float baseDano      = 12f;
    float multiplicador = 0.6f;
    float intervalo     = 6f;
    float velocidade    = 16f;
    float alcance       = 8f;

    float timer;

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

    Color CorElemento() {
        if (skillData != null && skillData.appliedElement != ElementType.None)
            return ElementRegistry.Instance?.GetCor(skillData.appliedElement) ?? Color.white;
        return Color.white;
    }

    void Update()
    {
        if (playerStats == null) return;
        timer -= Time.deltaTime;
        if (timer <= 0f) { timer = intervalo; Disparar(); }
    }

    public override void ApplyEffect() => Disparar();

    void Disparar()
    {
        // Pulsos expandem sempre a partir da posição atual do player
        Vector2[] direcoes = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        foreach (var dir in direcoes)
            StartCoroutine(PulsoBeam(dir));

        StartCoroutine(FlashPlayer());
    }

    IEnumerator PulsoBeam(Vector2 dir)
    {
        float dur = alcance / velocidade;

        var go = new GameObject("SombraCruzBeam");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 13;
        lr.numCapVertices = 4;

        var hashset = new System.Collections.Generic.HashSet<int>();

        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null || playerStats == null) yield break;

            float prog    = t / dur;
            float distAtual = Mathf.Lerp(0f, alcance, prog);

            // Origem SEMPRE na posição atual do player
            Vector2 origem = playerStats.transform.position;
            Vector2 ponta  = origem + dir * distAtual;

            lr.SetPosition(0, origem);
            lr.SetPosition(1, ponta);

            float larg  = Mathf.Lerp(0.35f, 0.05f, prog);
            Color ceB = CorElemento(); Color cor = new Color(ceB.r, ceB.g, ceB.b, Mathf.Lerp(1f, 0f, prog));
            lr.startWidth = larg; lr.endWidth = larg * 0.3f;
            lr.startColor = cor;  lr.endColor = new Color(cor.r, cor.g, cor.b, 0f);

            // Dano em colisão ao longo do beam
            var hits = Physics2D.OverlapCapsuleAll(
                origem + dir * distAtual * 0.5f,
                new Vector2(larg, distAtual),
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
                ic.ReceberDano(DanoAtual, false);
                SkillElementEffect.Aplicar(skillData, ic.gameObject, DanoAtual, this);
            }

            yield return null;
        }

        if (go != null) Destroy(go);
    }

    IEnumerator FlashPlayer()
    {
        var sr = playerStats?.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;
        Color orig = sr.color;
        { Color ceFl = CorElemento(); sr.color = new Color(ceFl.r, ceFl.g, ceFl.b); }
        yield return new WaitForSeconds(0.07f);
        if (sr != null) sr.color = orig;
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

    public void Iniciar(Vector2 direcao, float velocidade, float dmg, float alc)
    {
        dir    = direcao;
        vel    = velocidade;
        dano   = dmg;
        alcance = alc;
        origem = transform.position;

        // Rotaciona para a direção
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang - 90f);

        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite       = GerarCorte();
        sr.color        = new Color(0.55f, 0.25f, 1f);
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
            sr.color    = new Color(0.55f, 0.25f, 1f, Mathf.Lerp(1f, 0f, prog));
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
        SkillElementEffect.Aplicar(null, ic.gameObject, dano, this);
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
            psr.color  = new Color(0.55f, 0.25f, 1f);
            psr.sortingOrder = 12;
            p.transform.localScale = Vector3.one * Random.Range(0.1f, 0.22f);
            Vector2 v = dir * Random.Range(1f, 3f) + (Vector2)Random.insideUnitCircle * 1.5f;
            p.AddComponent<AutoDestroyFadeMove>().Iniciar(v, 0.3f);
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
