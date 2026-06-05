using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChuvaEstrelasSkillBehavior : SkillBehavior, ISkillComRecarga
{
    float baseDano       = 10f;
    float multiplicador  = 0.8f;
    float intervalo      = 6f;
    int   qtdEstrelas    = 3;
    float raioImpacto    = 1.5f;
    float alturaQueda    = 8f;

    float timer;
    public bool  EmRecarga    => timer > 0f;
    public float TimerRecarga => timer;
    public float RecargaTotal => intervalo;

    float DanoAtual => baseDano + (playerStats != null ? playerStats.attack * multiplicador : 0f);

    // ── Inicialização ─────────────────────────────────────────────────────────

    public override void Initialize(PlayerStats stats)
    {
        base.Initialize(stats);
    }

    static readonly Color COR_ORIG = new Color(1f, 0.9f, 0.2f);
    Color CorElemento() {
        if (skillData != null && skillData.appliedElement != ElementType.None)
            return ElementRegistry.Instance?.GetCor(skillData.appliedElement) ?? COR_ORIG;
        return COR_ORIG;
    }

    public void ConfigurarDeSkillData(SkillData data)
    {
        this.skillData = data;
        baseDano     = data.attackBonus > 0f          ? data.attackBonus        : 20f;
        intervalo    = data.activationInterval > 0f   ? data.activationInterval : 3f;
        qtdEstrelas  = data.projectileCount > 0       ? data.projectileCount    : 3;
        raioImpacto  = data.specialValue > 0f         ? data.specialValue       : 1.5f;
        timer        = intervalo;
    }

    // ── Update ────────────────────────────────────────────────────────────────

    void Update()
    {
        if (playerStats == null) return;
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = intervalo;
            StartCoroutine(DispararChuva());
        }
    }

    public override void ApplyEffect() => StartCoroutine(DispararChuva());

    // ── Lógica principal ──────────────────────────────────────────────────────

    IEnumerator QuedaEstrela(Vector2 alvo)
    {
        float tempoAviso = 0.75f;

        // Aviso no chão
        var avisoGO = CriarAvisoChao(alvo, raioImpacto);
        var avisoLR = avisoGO.GetComponent<LineRenderer>();

        for (float t = 0f; t < tempoAviso; t += Time.deltaTime)
        {
            if (avisoGO == null) yield break;
            float prog  = t / tempoAviso;
            float pulso = Mathf.Sin(t * 14f) * 0.5f + 0.5f;
            Color cor   = Color.Lerp(
                new Color(1f, 0.9f, 0.2f, 0.4f + pulso * 0.4f),
                new Color(1f, 0.4f, 0.1f, 0.6f + pulso * 0.3f),
                prog);
            if (avisoLR != null) avisoLR.startColor = avisoLR.endColor = cor;
            yield return null;
        }

        if (avisoGO != null) Destroy(avisoGO);

        // Projétil caindo
        Vector2 origem = alvo + Vector2.up * alturaQueda;
        StartCoroutine(AnimarEstrela(origem, alvo));

        // Impacto
        AplicarDanoArea(alvo);
        StartCoroutine(EfeitoImpacto(alvo));
    }

    void AplicarDanoArea(Vector2 pos)
    {
        var cols = Physics2D.OverlapCircleAll(pos, raioImpacto);
        foreach (var col in cols)
        {
            var ic = col.GetComponent<InimigoController>()
                  ?? col.GetComponentInParent<InimigoController>();
            if (ic != null) { ic.ReceberDano(DanoAtual, false); SkillElementEffect.Aplicar(skillData, ic.gameObject, DanoAtual, this); }
        }
        if (SkillEvolutionManager.Tem(SkillEvolutionType.ImpactoSismico))
            EvolutionFX.SpawnShockwave(pos, raioImpacto * 2.5f, DanoAtual * 0.5f, this);
    }

    IEnumerator DispararChuva()
    {
        int extra = SkillEvolutionManager.Tem(SkillEvolutionType.ChuvaIntensa) ? 2 : 0;
        var alvos = EncontrarAlvosMaisProximos(qtdEstrelas + extra);
        foreach (var alvo in alvos)
        {
            Vector2 pos = alvo != null ? (Vector2)alvo.transform.position
                        : (playerStats != null ? (Vector2)playerStats.transform.position : Vector2.zero);
            StartCoroutine(QuedaEstrela(pos));
            yield return new WaitForSeconds(0.15f);
        }
    }

    List<GameObject> EncontrarAlvosMaisProximos(int qtd)
    {
        var resultado  = new List<GameObject>();
        var inimigos   = FindObjectsByType<InimigoController>(FindObjectsSortMode.None);
        var candidatos = new List<InimigoController>(inimigos);

        Vector2 posPlayer = playerStats != null
            ? (Vector2)playerStats.transform.position
            : Vector2.zero;

        candidatos.Sort((a, b) =>
            Vector2.Distance(a.transform.position, posPlayer)
            .CompareTo(Vector2.Distance(b.transform.position, posPlayer)));

        for (int i = 0; i < Mathf.Min(qtd, candidatos.Count); i++)
            resultado.Add(candidatos[i].gameObject);

        // Se não há inimigos suficientes, cai em posições aleatórias perto do player
        while (resultado.Count < qtd)
            resultado.Add(null);

        return resultado;
    }

    // ── Visuais ───────────────────────────────────────────────────────────────

    GameObject CriarAvisoChao(Vector2 pos, float raio)
    {
        const int SEGS = 32;
        var go = new GameObject("AvisoEstrela");
        go.transform.position = pos;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 8;
        lr.startWidth    = lr.endWidth = 0.08f;
        lr.startColor    = lr.endColor = new Color(1f, 0.9f, 0.2f, 0.5f);
        for (int i = 0; i < SEGS; i++)
        {
            float ang = 360f / SEGS * i * Mathf.Deg2Rad;
            lr.SetPosition(i, pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raio);
        }

        // Cruz central
        var cruGO = new GameObject("Cruz");
        cruGO.transform.SetParent(go.transform, false);
        var lrCruz = cruGO.AddComponent<LineRenderer>();
        lrCruz.useWorldSpace = true;
        lrCruz.positionCount = 2;
        lrCruz.material      = new Material(Shader.Find("Sprites/Default"));
        lrCruz.sortingOrder  = 9;
        lrCruz.startWidth    = lrCruz.endWidth = 0.05f;
        lrCruz.startColor    = lrCruz.endColor = new Color(1f, 0.9f, 0.2f, 0.6f);
        lrCruz.SetPosition(0, pos + Vector2.left  * raio * 0.4f);
        lrCruz.SetPosition(1, pos + Vector2.right * raio * 0.4f);

        return go;
    }

    IEnumerator AnimarEstrela(Vector2 origem, Vector2 destino)
    {
        var go = new GameObject("EstrelaCaindo");
        go.transform.position = origem;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = GerarEstrela(16);
        sr.color        = new Color(1f, 0.9f, 0.3f);
        sr.sortingOrder = 14;
        go.transform.localScale = Vector3.one * 0.5f;

        float dur = 0.25f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p = t / dur;
            go.transform.position = Vector2.Lerp(origem, destino, Mathf.Pow(p, 0.6f));

            // Escala brilha mais ao aproximar
            float esc = Mathf.Lerp(0.4f, 0.9f, p);
            go.transform.localScale = Vector3.one * esc;
            sr.color = Color.Lerp(new Color(1f, 0.9f, 0.3f), Color.white, p);

            // Spawn partícula de rastro
            if (Time.frameCount % 2 == 0)
            {
                var t2 = new GameObject("Rastro");
                t2.transform.position = go.transform.position;
                var sr2 = t2.AddComponent<SpriteRenderer>();
                sr2.sprite = GerarDisco(6);
                sr2.color  = new Color(1f, 0.8f, 0.2f, 0.7f);
                sr2.sortingOrder = 13;
                t2.transform.localScale = Vector3.one * esc * 0.5f;
                StartCoroutine(FadeParticula(sr2, 0.15f));
                Destroy(t2, 0.4f); // failsafe
            }

            yield return null;
        }
        if (go != null) Destroy(go);
    }

    IEnumerator EfeitoImpacto(Vector2 pos)
    {
        // Anel expansivo
        const int SEGS = 40;
        var go = new GameObject("ImpactoEstrela");
        go.transform.position = pos;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = SEGS;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 11;

        float dur = 0.45f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p = t / dur;
            float r = Mathf.Lerp(0.1f, raioImpacto * 1.4f, p);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.25f, 0.02f, p);
            lr.startColor = lr.endColor = new Color(1f, 0.85f, 0.2f, Mathf.Lerp(1f, 0f, p));
            for (int i = 0; i < SEGS; i++)
            {
                float ang = 360f / SEGS * i * Mathf.Deg2Rad;
                lr.SetPosition(i, pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r);
            }
            yield return null;
        }
        if (go != null) Destroy(go);

        // Faíscas
        for (int i = 0; i < 8; i++)
        {
            var p = new GameObject("Faísca");
            p.transform.position = pos;
            var sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = GerarDisco(6); sr.color = new Color(1f, 0.9f, 0.2f); sr.sortingOrder = 12;
            p.transform.localScale = Vector3.one * Random.Range(0.1f, 0.22f);
            Vector2 vel = Random.insideUnitCircle.normalized * Random.Range(2f, 5f);
            StartCoroutine(Faisca(sr, vel));
        }
    }

    IEnumerator Faisca(SpriteRenderer sr, Vector2 vel)
    {
        Color cor = sr.color;
        float vida = Random.Range(0.25f, 0.5f);
        for (float t = 0f; t < vida; t += Time.deltaTime)
        {
            vel *= Mathf.Pow(0.85f, Time.deltaTime * 60f);
            if (sr != null)
            {
                sr.transform.position += (Vector3)(vel * Time.deltaTime);
                sr.color = new Color(cor.r, cor.g, cor.b, Mathf.Lerp(1f, 0f, t / vida));
            }
            yield return null;
        }
        if (sr != null) Destroy(sr.gameObject);
    }

    IEnumerator FadeParticula(SpriteRenderer sr, float dur)
    {
        Color c = sr.color;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (sr == null) yield break;
            sr.color = new Color(c.r, c.g, c.b, Mathf.Lerp(c.a, 0f, t / dur));
            yield return null;
        }
        if (sr != null) Destroy(sr.gameObject);
    }

    // ── Sprites procedurais ───────────────────────────────────────────────────

    static Sprite GerarEstrela(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float dx = Mathf.Abs(x + 0.5f - cx) / cx;
            float dy = Mathf.Abs(y + 0.5f - cx) / cx;
            float star = Mathf.Max(0f, 1f - (dx * dx + dy * dy) * 2f)
                       + Mathf.Max(0f, 1f - Mathf.Max(dx, dy) * 4f);
            float a = Mathf.Clamp01(star);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
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

