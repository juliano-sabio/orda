using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CorteFantasmaSkillBehavior : SkillBehavior
{
    float baseDano      = 17f;
    float multiplicador = 0.8f;
    float intervalo     = 5f;
    int   qtdCortes     = 2;
    float velocidade    = 18f;
    float raioDeteccao  = 15f;

    float timer;

    float DanoAtual => baseDano + (playerStats != null ? playerStats.attack * multiplicador : 0f);

    public override void Initialize(PlayerStats stats) => base.Initialize(stats);

    public void ConfigurarDeSkillData(SkillData data)
    {
        this.skillData = data;
        baseDano    = data.attackBonus > 0f          ? data.attackBonus        : 35f;
        intervalo   = data.activationInterval > 0f   ? data.activationInterval : 2.5f;
        qtdCortes   = data.projectileCount > 0       ? data.projectileCount    : 2;
        velocidade  = data.projectileSpeed > 0f      ? data.projectileSpeed    : 18f;
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
        var alvos = EncontrarAlvos(qtdCortes);
        Vector2 origem = playerStats.transform.position;

        Color corCorte = CorElemento();
        for (int i = 0; i < alvos.Count; i++)
        {
            var alvo = alvos[i];
            // Delay leve entre cortes
            StartCoroutine(DisparararCorteComDelay(origem, alvo, i * 0.1f, corCorte));
        }
    }

    IEnumerator DisparararCorteComDelay(Vector2 origem, InimigoController alvo, float delay, Color cor = default)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        var go = new GameObject("CorteFantasma");
        go.transform.position = origem;
        go.AddComponent<CorteFantasmaProjetil>().Iniciar(alvo, velocidade, DanoAtual, cor);
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
    InimigoController alvo;
    Vector2           dir;
    float             vel;
    float             dano;
    bool              atingiu;
    float             rot;
    Color             corBase = new Color(0.7f, 0.9f, 1f);

    SpriteRenderer    sr;

    public void Iniciar(InimigoController alvoIC, float velocidade, float dmg, Color cor = default)
    {
        alvo = alvoIC;
        vel  = velocidade;
        dano = dmg;
        if (cor != default && cor != Color.white) corBase = cor;

        if (alvo != null)
            dir = ((Vector2)alvo.transform.position - (Vector2)transform.position).normalized;
        else
            dir = Random.insideUnitCircle.normalized;

        // Rotaciona para a direção
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang - 45f);

        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite       = GerarCorteFantasma();
        sr.color        = new Color(corBase.r, corBase.g, corBase.b, 0.9f);
        sr.sortingOrder = 14;
        transform.localScale = Vector3.one * 0.7f;

        var col = gameObject.AddComponent<PolygonCollider2D>();
        col.isTrigger = true;
        col.SetPath(0, new Vector2[]
        {
            new Vector2(-0.3f, -0.5f),
            new Vector2( 0.3f, -0.5f),
            new Vector2( 0.5f,  0.5f),
            new Vector2(-0.5f,  0.5f),
        });

        Destroy(gameObject, 2f);
        StartCoroutine(SpawnTrail());
    }

    void Update()
    {
        if (atingiu) return;

        // Leve homing: recalcula direção se o alvo ainda existir
        if (alvo != null && !alvo.estaMorrendo)
        {
            Vector2 dirAlvo = ((Vector2)alvo.transform.position - (Vector2)transform.position).normalized;
            dir = Vector2.Lerp(dir, dirAlvo, Time.deltaTime * 8f).normalized;
        }

        transform.position += (Vector3)(dir * vel * Time.deltaTime);

        // Rotaciona para a direção de movimento
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang - 45f);

        // Pulso de opacidade — efeito fantasma
        rot += Time.deltaTime * 5f;
        if (sr != null)
        {
            float pulso = Mathf.Sin(rot) * 0.15f + 0.85f;
            sr.color = new Color(corBase.r, corBase.g, corBase.b, pulso);
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
        SkillElementEffect.Aplicar(null, ic.gameObject, dano, this);
        StartCoroutine(EfeitoImpacto(transform.position, transform.rotation));
    }

    IEnumerator SpawnTrail()
    {
        while (!atingiu && gameObject != null)
        {
            yield return new WaitForSeconds(0.04f);
            if (gameObject == null) yield break;

            var t = new GameObject("TrailCorte");
            t.transform.position   = transform.position;
            t.transform.rotation   = transform.rotation;
            t.transform.localScale = transform.localScale * 0.85f;
            var tsr = t.AddComponent<SpriteRenderer>();
            tsr.sprite       = GerarCorteFantasma();
            tsr.color        = new Color(corBase.r * 0.75f, corBase.g * 0.9f, corBase.b, 0.4f);
            tsr.sortingOrder = 13;
            t.AddComponent<AutoDestroyFade>().Iniciar(0.15f);
        }
    }

    IEnumerator EfeitoImpacto(Vector3 pos, Quaternion rot_)
    {
        // Slash marks — 3 linhas rápidas
        for (int i = 0; i < 3; i++)
        {
            var slash = new GameObject("SlashMark");
            slash.transform.position = pos + Random.insideUnitSphere * 0.3f;
            slash.transform.rotation = Quaternion.Euler(0f, 0f,
                rot_.eulerAngles.z + Random.Range(-30f, 30f));
            slash.transform.localScale = Vector3.one * Random.Range(0.5f, 1f);
            var ssr = slash.AddComponent<SpriteRenderer>();
            ssr.sprite = GerarCorteFantasma();
            ssr.color  = Color.white;
            ssr.sortingOrder = 15;
            slash.AddComponent<AutoDestroyFade>().Iniciar(0.2f);
        }

        // Partículas
        for (int i = 0; i < 6; i++)
        {
            var p = new GameObject("PartCorte");
            p.transform.position = pos;
            var psr = p.AddComponent<SpriteRenderer>();
            psr.sprite = GerarDisco(6);
            psr.color  = corBase;
            psr.sortingOrder = 14;
            p.transform.localScale = Vector3.one * Random.Range(0.08f, 0.18f);
            p.AddComponent<AutoDestroyFadeMove>().Iniciar(
                Random.insideUnitCircle.normalized * Random.Range(2f, 5f), 0.25f);
        }

        // Fade rápido do projétil
        if (sr != null)
        {
            for (float t = 0f; t < 0.15f; t += Time.deltaTime)
            {
                if (sr == null) break;
                sr.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.9f, 0f, t / 0.15f));
                transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.2f, t / 0.15f);
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
