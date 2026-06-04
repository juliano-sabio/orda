using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChicoteEnergiaSkillBehavior : SkillBehavior, ISkillComRecarga, IEvoluivel
{
    float baseDano      = 15f;
    float multiplicador = 0.5f;
    float intervalo     = 4f;
    float raio          = 4f;
    float timer;
    public bool  EmRecarga    => timer > 0f;
    public float TimerRecarga => timer;
    public float RecargaTotal => intervalo;

    float DanoAtual => baseDano + (playerStats != null ? playerStats.attack * multiplicador : 0f);

        public void OnEvolucaoAplicada(SkillEvolutionType tipo) { if (tipo == SkillEvolutionType.ChicoteAlcance) raio *= 1.5f; }
    public override void Initialize(PlayerStats stats) => base.Initialize(stats);

    public void ConfigurarDeSkillData(SkillData data)
    {
        baseDano  = data.attackBonus > 0f        ? data.attackBonus        : 30f;
        intervalo = data.activationInterval > 0f ? data.activationInterval : 2f;
        raio      = data.specialValue > 0f       ? data.specialValue       : 4f;
        timer     = intervalo;
    }

    void Update()
    {
        if (playerStats == null) return;
        timer -= Time.deltaTime;
        if (timer <= 0f) { timer = intervalo; StartCoroutine(ChicotearComEvo()); }
    }

    public override void ApplyEffect() => StartCoroutine(ChicotearComEvo());

    IEnumerator ChicotearComEvo()
    {
        yield return StartCoroutine(Chicotear());
        if (SkillEvolutionManager.Tem(SkillEvolutionType.DuplaRotacao))
        {
            yield return new WaitForSeconds(0.1f);
            yield return StartCoroutine(Chicotear());
        }
    }

    IEnumerator Chicotear()
    {
        if (playerStats == null) yield break;
        Vector2 centro = playerStats.transform.position;
        float   dur    = 0.5f;
        var atingidos  = new HashSet<int>();

        // ── Beam principal ───────────────────────────────────────────────────
        var go = new GameObject("ChicoteBeam");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = false;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = 13; lr.numCapVertices = 4;
        Destroy(go, dur + 0.1f);

        // ── Beam secundário (sombra) ─────────────────────────────────────────
        var goSombra = new GameObject("ChicoteSombra");
        var lrSombra = goSombra.AddComponent<LineRenderer>();
        lrSombra.useWorldSpace = true; lrSombra.loop = false;
        lrSombra.material = new Material(Shader.Find("Sprites/Default"));
        lrSombra.sortingOrder = 12; lrSombra.numCapVertices = 4;
        Destroy(goSombra, dur + 0.1f);

        // Anel de impacto inicial
        StartCoroutine(AnelImpacto(centro));

        float proxParticula = 0f;

        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null || playerStats == null) yield break;

            float prog   = t / dur;
            float angAtu = Mathf.Lerp(0f, 360f, prog);
            centro       = playerStats.transform.position;

            // Beam principal com ondulação intensa
            const int SEGS = 32;
            lr.positionCount = SEGS;
            lrSombra.positionCount = SEGS;
            for (int i = 0; i < SEGS; i++)
            {
                float a    = Mathf.Lerp(0f, angAtu, i / (float)(SEGS - 1)) * Mathf.Deg2Rad;
                float onda = Mathf.Sin(t * 25f + i * 0.7f) * 0.2f
                           + Mathf.Sin(t * 15f - i * 0.4f) * 0.1f;
                float r    = raio + onda;
                Vector2 pos = centro + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                lr.SetPosition(i, pos);
                // Sombra ligeiramente deslocada
                lrSombra.SetPosition(i, pos + new Vector2(0.08f, -0.08f));
            }

            // Estilo do beam principal
            float larg = Mathf.Lerp(0.3f, 0.05f, prog);
            Color cor  = new Color(0.1f, 0.85f, 1f, Mathf.Lerp(1f, 0f, prog));
            lr.startWidth = larg; lr.endWidth = larg * 0.2f;
            lr.startColor = cor; lr.endColor = new Color(cor.r, cor.g, cor.b, 0f);

            // Sombra escura semi-transparente
            float largS = larg * 0.6f;
            lrSombra.startWidth = largS; lrSombra.endWidth = largS * 0.15f;
            lrSombra.startColor = lrSombra.endColor = new Color(0f, 0.3f, 0.6f, Mathf.Lerp(0.4f, 0f, prog));

            // Partículas ao longo do chicote
            proxParticula -= Time.deltaTime;
            if (proxParticula <= 0f && SEGS > 0)
            {
                proxParticula = 0.06f;
                int idx = Random.Range(0, SEGS);
                SpawnParticulaChicote(lr.GetPosition(idx));
            }

            // Dano em área com detecção progressiva
            var hits = Physics2D.OverlapCircleAll(centro, raio + 0.4f);
            foreach (var col in hits)
            {
                var ic = col.GetComponent<InimigoController>() ?? col.GetComponentInParent<InimigoController>();
                if (ic == null || ic.estaMorrendo) continue;
                int id = ic.gameObject.GetInstanceID();
                if (atingidos.Contains(id)) continue;
                atingidos.Add(id);
                ic.ReceberDano(DanoAtual, false);
                if (SkillEvolutionManager.Tem(SkillEvolutionType.ChicoteEletrico)) EvolutionFX.AplicarLentidao(ic, 1f, 0.4f);
                StartCoroutine(FlashInimigo(ic));
                SpawnImpactoInimigo(ic.transform.position);
            }

            yield return null;
        }

        if (go != null) Destroy(go);
        if (goSombra != null) Destroy(goSombra);

        // Anel de dissolução final
        StartCoroutine(AnelDissolvendo(playerStats.transform.position));
    }

    // ── Aviso pré-chicote ─────────────────────────────────────────────────────

    // ── Efeitos visuais ───────────────────────────────────────────────────────

    IEnumerator AnelImpacto(Vector2 pos)
    {
        const int S = 40;
        var go = new GameObject("AnelChicote");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = S;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 11;
        float dur = 0.3f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p = t / dur; float r = Mathf.Lerp(0.2f, raio * 1.2f, p);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.22f, 0.01f, p);
            lr.startColor = lr.endColor = new Color(0.1f, 0.85f, 1f, Mathf.Lerp(1f, 0f, p));
            for (int i = 0; i < S; i++) { float ang = 360f / S * i * Mathf.Deg2Rad; lr.SetPosition(i, pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r); }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    IEnumerator AnelDissolvendo(Vector2 pos)
    {
        const int S = 32;
        var go = new GameObject("AnelDis");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = S;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 11;
        for (int i = 0; i < S; i++) { float ang = 360f / S * i * Mathf.Deg2Rad; lr.SetPosition(i, pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raio); }
        float dur = 0.25f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p = t / dur;
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.15f, 0f, p);
            lr.startColor = lr.endColor = new Color(0.1f, 0.85f, 1f, Mathf.Lerp(0.6f, 0f, p));
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    void SpawnParticulaChicote(Vector3 pos)
    {
        var go = new GameObject("PChicote");
        go.transform.position = pos;
        var sr2 = go.AddComponent<SpriteRenderer>();
        sr2.sprite = GerarDisco(6); sr2.color = new Color(0.2f, 0.85f, 1f, 0.8f);
        sr2.sortingOrder = 14;
        go.transform.localScale = Vector3.one * Random.Range(0.07f, 0.18f);
        go.AddComponent<AutoDestroyFadeMove>().Iniciar(Random.insideUnitCircle * Random.Range(0.5f, 2f), 0.2f);
        Destroy(go, 0.4f);
    }

    void SpawnImpactoInimigo(Vector2 pos)
    {
        for (int i = 0; i < 4; i++)
        {
            var go = new GameObject("PImpacto");
            go.transform.position = pos;
            var sr2 = go.AddComponent<SpriteRenderer>();
            sr2.sprite = GerarDisco(6); sr2.color = new Color(0.2f, 0.85f, 1f);
            sr2.sortingOrder = 15;
            go.transform.localScale = Vector3.one * Random.Range(0.1f, 0.2f);
            Vector2 vel = Random.insideUnitCircle.normalized * Random.Range(2f, 4f);
            go.AddComponent<AutoDestroyFadeMove>().Iniciar(vel, 0.25f);
            Destroy(go, 0.5f);
        }
    }

    IEnumerator FlashInimigo(InimigoController ic)
    {
        var sr = ic?.GetComponent<SpriteRenderer>(); if (sr == null) yield break;
        Color orig = sr.color; sr.color = new Color(0.3f, 0.9f, 1f);
        yield return new WaitForSeconds(0.08f);
        if (sr != null) sr.color = orig;
    }

    static Sprite GerarDisco(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false); tex.filterMode = FilterMode.Bilinear; float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++) { float d = Vector2.Distance(new Vector2(x+0.5f,y+0.5f),new Vector2(cx,cx)); tex.SetPixel(x,y,new Color(1f,1f,1f,d<cx?1f:0f)); }
        tex.Apply(); return Sprite.Create(tex, new Rect(0,0,sz,sz), new Vector2(0.5f,0.5f), sz);
    }
}


