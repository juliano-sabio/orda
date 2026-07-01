using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CorrentesInfernoUltimate : MonoBehaviour, IUltimateCosmetico
{
    bool cosmetico;
    public void ExecutarCosmetico() { if (ativo) return; cosmetico = true; StartCoroutine(CorotinaAtivacao()); }

    [Header("Configurações")]
    public float raio           = 10f;
    public float duracao        = 6f;
    public float cooldown       = 24f;
    public float danoPorSegundo = 15f;

    public float CooldownRestante => cooldownRestante;
    public bool  Ativo            => ativo;

    private float       cooldownRestante;
    private bool        ativo;
    private PlayerStats playerStats;

    readonly List<EstadoAcorrentado> acorrentados = new List<EstadoAcorrentado>();

    struct EstadoAcorrentado
    {
        public InimigoController ic;
        public GameObject        go;
        public MonoBehaviour[]   scripts;
        public Rigidbody2D       rb;
        public GameObject        correnteGO;
    }

    static readonly float[] Angulos8 = { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };

    static readonly Color COR_FUMO   = new Color(0.18f, 0.05f, 0.02f);
    static readonly Color COR_BRASA  = new Color(0.92f, 0.12f, 0f);
    static readonly Color COR_FOGO   = new Color(1f,    0.48f, 0.04f);
    static readonly Color COR_NUCLEO = new Color(1f,    0.92f, 0.55f);

    const int SEGS_CORRENTE = 18;

    void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            if (playerStats.ultimateSkill != null)
                playerStats.ultimateSkill.isActive = true;
            playerStats.ultimateCooldown   = cooldown;
            playerStats.ultimateChargeTime = 0f;
            playerStats.ultimateReady      = false;
        }
    }

    void Update()
    {
        if (cooldownRestante > 0f) cooldownRestante -= Time.deltaTime;
        if (playerStats != null && playerStats.IsLocalAuthority &&
            InputBindings.UltimateDown() && cooldownRestante <= 0f && !ativo && (playerStats == null || !playerStats.ultimateBloqueada))
            StartCoroutine(CorotinaAtivacao());
        SincronizarUI();
    }

    void FixedUpdate()
    {
        if (!ativo || acorrentados.Count == 0) return;
        foreach (var e in acorrentados)
        {
            if (e.rb == null || e.go == null) continue;
            Vector2 dir = (Vector2)transform.position - (Vector2)e.go.transform.position;
            e.rb.linearVelocity = dir.magnitude > 0.3f ? dir.normalized * 1.5f : Vector2.zero;
        }
    }

    void SincronizarUI()
    {
        if (playerStats == null) return;
        playerStats.ultimateChargeTime = cooldown - Mathf.Max(0f, cooldownRestante);
        playerStats.ultimateReady      = cooldownRestante <= 0f && !ativo;
    }

    // ─── COROUTINE PRINCIPAL ────────────────────────────────────────────────

    const float TICK_INTERVAL = 0.5f;

    IEnumerator CorotinaAtivacao()
    {
        ativo            = true;
        cooldownRestante = cooldown;

        yield return StartCoroutine(AnimarDisparo());

        AcorrentarInimigos();

        float elapsed   = 0f;
        float tickAccum = 0f;
        while (elapsed < duracao)
        {
            float dt = Time.deltaTime;
            elapsed  += dt;
            tickAccum += dt;
            if (tickAccum >= TICK_INTERVAL)
            {
                AplicarDano(tickAccum);
                tickAccum = 0f;
            }
            AtualizarVisuais(elapsed);
            yield return null;
        }
        if (tickAccum > 0f) AplicarDano(tickAccum);

        yield return StartCoroutine(LiberarTodos());
        ativo = false;
    }

    // ─── LÓGICA ─────────────────────────────────────────────────────────────

    void AcorrentarInimigos()
    {
        var candidatos = Physics2D.OverlapCircleAll(transform.position, raio);

        foreach (float ang in Angulos8)
        {
            Vector2 dir = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));

            InimigoController melhor    = null;
            float             menorDist = float.MaxValue;

            foreach (var col in candidatos)
            {
                if (col.gameObject == gameObject) continue;
                var root = ResolverRaiz(col.gameObject);
                if (root == null) continue;
                var ic = root.GetComponent<InimigoController>();
                if (ic == null || JaAcorrentado(ic)) continue;

                Vector2 toEnemy = (Vector2)root.transform.position - (Vector2)transform.position;
                float dist = toEnemy.magnitude;
                float dot  = Vector2.Dot(dir, toEnemy.normalized);

                if (dot > 0.6f && dist < menorDist)
                {
                    menorDist = dist;
                    melhor    = ic;
                }
            }

            if (melhor != null)
                Acorrentar(melhor, ang);
        }
    }

    void Acorrentar(InimigoController ic, float angDir)
    {
        var go      = ic.gameObject;
        var rb      = go.GetComponent<Rigidbody2D>() ?? go.GetComponentInParent<Rigidbody2D>();
        var scripts = new List<MonoBehaviour>();

        var mi = go.GetComponent<movi_inimigo>();
        if (mi != null) { mi.enabled = false; scripts.Add(mi); }

        var corrGO = CriarCorrenteGO();

        // Flash de encadeamento no inimigo
        StartCoroutine(AnelExpansao(go.transform.position, 1.5f, 0.3f, COR_BRASA, 16));
        for (int i = 0; i < 6; i++)
        {
            float a = (360f / 6) * i * Mathf.Deg2Rad;
            EmitirFaisca(go.transform.position,
                new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(1.5f, 3f));
        }

        acorrentados.Add(new EstadoAcorrentado
        {
            ic         = ic,
            go         = go,
            scripts    = scripts.ToArray(),
            rb         = rb,
            correnteGO = corrGO
        });
    }

    void AplicarDano(float dt)
    {
        float danoEfetivo = danoPorSegundo;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.InfernoIntensidade))
            danoEfetivo *= 2f;

        float danoTick = danoEfetivo * dt;

        var mortos = new List<EstadoAcorrentado>();
        foreach (var e in acorrentados)
        {
            if (e.ic == null || e.go == null) { mortos.Add(e); continue; }
            if (!cosmetico) e.ic.ReceberDano(danoTick, false, true);

            if (SkillEvolutionManager.Tem(SkillEvolutionType.InfernoPropagado))
            {
                foreach (var c in Physics2D.OverlapCircleAll(e.go.transform.position, 5f))
                {
                    var icViz = c.GetComponent<InimigoController>() ?? c.GetComponentInParent<InimigoController>();
                    if (icViz != null && icViz != e.ic)
                        if (!cosmetico) icViz.ReceberDano(danoTick * 0.35f, false, false);
                }
            }
        }
        foreach (var m in mortos)
        {
            if (m.correnteGO != null) Destroy(m.correnteGO);
            acorrentados.Remove(m);
        }
    }

    bool JaAcorrentado(InimigoController ic)
    {
        foreach (var e in acorrentados)
            if (e.ic == ic) return true;
        return false;
    }

    IEnumerator LiberarTodos()
    {
        // Libera scripts dos inimigos imediatamente
        foreach (var e in acorrentados)
            if (e.scripts != null)
                foreach (var s in e.scripts) if (s != null) s.enabled = true;

        // Correntes se quebram: faíscas ao longo de cada uma + anel no inimigo
        foreach (var e in acorrentados)
        {
            if (e.go == null || e.correnteGO == null) continue;
            Vector2 orig   = transform.position;
            Vector2 dest   = e.go.transform.position;
            float   len    = (dest - orig).magnitude;
            float   sagMax = Mathf.Max(0.35f, len * 0.07f);

            for (int i = 0; i < 7; i++)
            {
                float   pf  = (float)i / 6f;
                Vector2 pos = Vector2.Lerp(orig, dest, pf);
                pos.y -= Mathf.Sin(pf * Mathf.PI) * sagMax;
                EmitirFaisca(pos, new Vector2(Random.Range(-2.5f, 2.5f), Random.Range(-0.5f, 3f)));
            }
            StartCoroutine(AnelExpansao(dest, 1.2f, 0.3f, COR_FOGO, 14));
        }

        // Fade out de todas as camadas das correntes
        var lrs = new List<LineRenderer>();
        foreach (var e in acorrentados)
            if (e.correnteGO != null)
                lrs.AddRange(e.correnteGO.GetComponentsInChildren<LineRenderer>());

        float dur = 0.35f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            foreach (var lr in lrs)
            {
                if (lr == null) continue;
                Color col = lr.startColor;
                col.a = Mathf.Lerp(col.a, 0f, p * 2.2f);
                lr.startColor = lr.endColor = col;
            }
            yield return null;
        }

        foreach (var e in acorrentados)
            if (e.correnteGO != null) Destroy(e.correnteGO);
        acorrentados.Clear();
    }

    // ─── VISUAIS ────────────────────────────────────────────────────────────

    void AtualizarVisuais(float elapsed)
    {
        float flicker = Mathf.Sin(elapsed * 22f) * 0.15f + 0.85f;
        float tNorm   = Mathf.Clamp01(elapsed / duracao);

        for (int idx = 0; idx < acorrentados.Count; idx++)
        {
            var e = acorrentados[idx];
            if (e.go == null || e.correnteGO == null) continue;

            var tOuter = e.correnteGO.transform.Find("Outer");
            var tBody  = e.correnteGO.transform.Find("Body");
            var tCore  = e.correnteGO.transform.Find("Core");

            var lrOuter = tOuter != null ? tOuter.GetComponent<LineRenderer>() : null;
            var lrBody  = tBody  != null ? tBody .GetComponent<LineRenderer>() : null;
            var lrCore  = tCore  != null ? tCore .GetComponent<LineRenderer>() : null;

            Vector2 origem  = transform.position;
            Vector2 destino = e.go.transform.position;
            Vector2 chDir   = destino - origem;
            float   len     = chDir.magnitude;
            Vector2 perp    = len > 0.01f ? new Vector2(-chDir.y, chDir.x).normalized : Vector2.up;
            float   sagMax  = Mathf.Max(0.35f, len * 0.07f);

            for (int i = 0; i < SEGS_CORRENTE; i++)
            {
                float   p   = (float)i / (SEGS_CORRENTE - 1);
                Vector2 pos = Vector2.Lerp(origem, destino, p);
                float sag  = Mathf.Sin(p * Mathf.PI) * sagMax;
                float wav1 = Mathf.Sin(elapsed * 14f + p * 9f   + idx * 2.1f) * 0.16f;
                float wav2 = Mathf.Sin(elapsed *  5f + p * 3.5f + idx * 0.8f) * 0.09f;
                pos.y -= sag;
                pos   += perp * (wav1 + wav2);

                if (lrOuter != null) lrOuter.SetPosition(i, pos);
                if (lrBody  != null) lrBody .SetPosition(i, pos);
                if (lrCore  != null) lrCore .SetPosition(i, pos);
            }

            if (lrOuter != null)
            {
                Color co = COR_BRASA;
                co.a = (0.22f + flicker * 0.18f) * (1f - tNorm * 0.25f);
                lrOuter.startColor = lrOuter.endColor = co;
                lrOuter.startWidth = lrOuter.endWidth = 0.30f + flicker * 0.08f;
            }
            if (lrBody != null)
            {
                Color cb = Color.Lerp(COR_FOGO, COR_BRASA, tNorm);
                cb.a = 1f;
                lrBody.startColor = lrBody.endColor = cb;
                lrBody.startWidth = lrBody.endWidth = 0.12f + flicker * 0.03f;
            }
            if (lrCore != null)
            {
                Color cc = COR_NUCLEO;
                cc.a = flicker * (1f - tNorm * 0.3f);
                lrCore.startColor = lrCore.endColor = cc;
            }

            // Partículas de fogo esparsas ao longo da corrente
            if (Time.frameCount % 3 == idx % 3)
            {
                float   pPart   = Random.value;
                Vector2 posPart = Vector2.Lerp(origem, destino, pPart);
                posPart.y -= Mathf.Sin(pPart * Mathf.PI) * sagMax;
                EmitirFaisca(posPart, new Vector2(Random.Range(-0.8f, 0.8f), Random.Range(0.5f, 1.8f)));
            }
        }
    }

    GameObject CriarCorrenteGO()
    {
        var root = new GameObject("CorrenteInferno");
        root.transform.position = transform.position;

        // Glow largo e transparente
        Color glowStart = new Color(COR_BRASA.r, COR_BRASA.g, COR_BRASA.b, 0.35f);
        Color glowEnd   = new Color(COR_FOGO.r,  COR_FOGO.g,  COR_FOGO.b,  0.15f);
        CriarLR(root, "Outer", SEGS_CORRENTE, 20, glowStart, glowEnd, 0.32f, 0.22f);

        // Corrente de fogo sólida
        CriarLR(root, "Body", SEGS_CORRENTE, 21, COR_FOGO, COR_BRASA, 0.13f, 0.08f);

        // Núcleo branco-quente fino
        Color coreStart = new Color(COR_NUCLEO.r, COR_NUCLEO.g, COR_NUCLEO.b, 1f);
        Color coreEnd   = new Color(COR_FOGO.r,   COR_FOGO.g,   COR_FOGO.b,   0.7f);
        CriarLR(root, "Core", SEGS_CORRENTE, 22, coreStart, coreEnd, 0.045f, 0.02f);

        return root;
    }

    IEnumerator AnimarDisparo()
    {
        // Fase 1: Pré-carga — disco de brasa cresce no centro (0.22s)
        var centroGO = new GameObject("CentroInferno");
        centroGO.transform.position = transform.position;
        var centroSR = centroGO.AddComponent<SpriteRenderer>();
        centroSR.sprite       = GerarDisco(24);
        centroSR.color        = new Color(COR_BRASA.r, COR_BRASA.g, COR_BRASA.b, 0f);
        centroSR.sortingOrder = 18;
        centroGO.transform.localScale = Vector3.zero;

        float fase1 = 0.22f;
        for (float t = 0f; t < fase1; t += Time.deltaTime)
        {
            float p = t / fase1;
            centroGO.transform.localScale = Vector3.one * Mathf.Lerp(0f, 1.8f, p * p);
            Color c = centroSR.color; c.a = Mathf.Lerp(0f, 0.9f, p); centroSR.color = c;

            // Faíscas convergindo para o centro
            if (Time.frameCount % 3 == 0)
            {
                float ang  = Random.value * Mathf.PI * 2f;
                float dist = Random.Range(2f, 5f);
                Vector2 spawnPos = (Vector2)transform.position + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;
                EmitirFaisca(spawnPos, ((Vector2)transform.position - spawnPos).normalized * 3.5f);
            }
            yield return null;
        }

        // Fase 2: 8 rajadas de fogo expandindo (0.38s)
        var raioGOs  = new GameObject[8];
        var lrsSmoke = new LineRenderer[8];
        var lrsFire  = new LineRenderer[8];

        for (int i = 0; i < 8; i++)
        {
            var go = new GameObject($"RaioFogo{i}");
            go.transform.position = transform.position;
            lrsSmoke[i] = CriarLR(go, "Smoke", 2, 16,
                new Color(COR_FUMO.r, COR_FUMO.g, COR_FUMO.b, 0.5f),
                new Color(COR_FUMO.r, COR_FUMO.g, COR_FUMO.b, 0f),
                0.45f, 0.12f);
            lrsFire[i] = CriarLR(go, "Fire", 2, 17,
                COR_FOGO,
                new Color(COR_NUCLEO.r, COR_NUCLEO.g, COR_NUCLEO.b, 0f),
                0.18f, 0.05f);
            raioGOs[i] = go;
        }

        float fase2 = 0.38f;
        for (float t = 0f; t < fase2; t += Time.deltaTime)
        {
            float p  = t / fase2;
            float ep = Mathf.Sqrt(p);
            float r  = Mathf.Lerp(0.4f, raio, ep);

            for (int i = 0; i < 8; i++)
            {
                if (raioGOs[i] == null) continue;
                float   ang  = Angulos8[i] * Mathf.Deg2Rad;
                Vector2 dir  = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                Vector2 orig = transform.position;
                Vector2 tip  = orig + dir * r;

                lrsSmoke[i].SetPosition(0, orig); lrsSmoke[i].SetPosition(1, tip);
                lrsFire[i] .SetPosition(0, orig); lrsFire[i] .SetPosition(1, tip);

                Color cs = COR_FUMO; cs.a = 0.5f * (1f - p * 0.6f);
                lrsSmoke[i].startColor = lrsSmoke[i].endColor = cs;
                Color cf = COR_FOGO; cf.a = 1f; lrsFire[i].startColor = cf;
                lrsFire[i].startWidth = lrsFire[i].endWidth = 0.18f + Mathf.Sin(t * 40f) * 0.03f;

                if (Time.frameCount % 4 == i % 4)
                {
                    float pPart = Random.Range(0.3f, 1f);
                    EmitirFaisca(orig + dir * r * pPart,
                        new Vector2(Random.Range(-1.5f, 1.5f), Random.Range(0.3f, 2f)));
                }
            }
            yield return null;
        }

        foreach (var go in raioGOs) if (go != null) Destroy(go);
        StartCoroutine(FadeDestruir(centroGO, 0.15f));

        // Fase 3: Flash + 2 anéis expansivos + 16 faíscas radiais
        var flashGO = new GameObject("FlashInferno");
        flashGO.transform.position = transform.position;
        var flashSR = flashGO.AddComponent<SpriteRenderer>();
        flashSR.sprite       = GerarDisco(20);
        flashSR.color        = new Color(1f, 0.75f, 0.3f, 1f);
        flashSR.sortingOrder = 20;
        flashGO.transform.localScale = Vector3.one * 3f;

        StartCoroutine(AnelExpansao(transform.position, 5f,   0.45f, COR_FOGO,   24));
        StartCoroutine(AnelExpansao(transform.position, 2.5f, 0.28f, COR_NUCLEO, 16));

        for (int i = 0; i < 16; i++)
        {
            float ang = (360f / 16) * i * Mathf.Deg2Rad;
            EmitirFaisca(transform.position,
                new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(2f, 4.5f));
        }

        float fase3 = 0.18f;
        for (float t = 0f; t < fase3; t += Time.deltaTime)
        {
            if (flashGO == null) yield break;
            float p = t / fase3;
            Color c = flashSR.color; c.a = 1f - p; flashSR.color = c;
            yield return null;
        }
        if (flashGO != null) Destroy(flashGO);
    }

    IEnumerator AnelExpansao(Vector2 pos, float raioMax, float dur, Color cor, int segs)
    {
        var go = new GameObject("AnelInferno");
        go.transform.position = pos;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = segs;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 23;
        lr.startWidth    = lr.endWidth = 0.10f;
        lr.startColor    = lr.endColor = cor;

        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p = t / dur;
            float r = Mathf.Lerp(0f, raioMax, p);
            Color c = cor; c.a = Mathf.Lerp(1f, 0f, p * p); lr.startColor = lr.endColor = c;
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.14f, 0.03f, p);
            for (int i = 0; i < segs; i++)
            {
                float ang = (360f / segs) * i * Mathf.Deg2Rad;
                lr.SetPosition(i, (Vector2)go.transform.position + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r);
            }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    IEnumerator FadeDestruir(GameObject go, float dur)
    {
        if (go == null) yield break;
        var sr  = go.GetComponent<SpriteRenderer>();
        var lrs = go.GetComponentsInChildren<LineRenderer>();
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p = t / dur;
            if (sr != null) { Color c = sr.color; c.a = Mathf.Lerp(c.a, 0f, p * 2f); sr.color = c; }
            foreach (var lr in lrs)
                if (lr != null) { Color c = lr.startColor; c.a = Mathf.Lerp(c.a, 0f, p * 2f); lr.startColor = lr.endColor = c; }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    static LineRenderer CriarLR(GameObject parent, string nome, int segs, int order,
        Color cStart, Color cEnd, float wStart, float wEnd)
    {
        var child = new GameObject(nome);
        child.transform.SetParent(parent.transform, false);
        var lr = child.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = segs;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = order;
        lr.startColor    = cStart;
        lr.endColor      = cEnd;
        lr.startWidth    = wStart;
        lr.endWidth      = wEnd;
        return lr;
    }

    static void EmitirFaisca(Vector2 pos, Vector2 vel)
    {
        var go = new GameObject("FaiscaInferno");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = GerarDisco(6);
        sr.sortingOrder = 25;
        sr.color        = Color.Lerp(COR_NUCLEO, COR_FOGO, Random.value);
        go.transform.localScale = Vector3.one * Random.Range(0.07f, 0.17f);
        var fade = go.AddComponent<AutoDestroyFadeMove>();
        fade.Iniciar(vel + Random.insideUnitCircle * 0.4f, Random.Range(0.22f, 0.52f));
    }

    static Sprite GerarDisco(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        float c = sz * 0.5f;
        for (int x = 0; x < sz; x++)
            for (int y = 0; y < sz; y++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d / c)));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, sz);
    }

    static GameObject ResolverRaiz(GameObject go)
    {
        if (go.GetComponentInParent<ProjetilHomingPrincesa>(true)   != null) return null;
        if (go.GetComponentInParent<ProjetilEspecialPrincesa>(true) != null) return null;

        var ic = go.GetComponent<InimigoController>() ?? go.GetComponentInParent<InimigoController>();
        if (ic != null) return ic.gameObject;
        var mi = go.GetComponent<movi_inimigo>() ?? go.GetComponentInParent<movi_inimigo>();
        if (mi != null) return mi.gameObject;
        return null;
    }
}
