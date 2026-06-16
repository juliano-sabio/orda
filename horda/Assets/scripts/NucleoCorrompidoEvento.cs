using System;
using System.Collections;
using UnityEngine;

public class NucleoCorrompidoEvento : MonoBehaviour
{
    [HideInInspector] public float        vidaMaxima     = 2000f;
    [HideInInspector] public float        intervaloSpawn = 4f;
    [HideInInspector] public float        danoLaser      = 45f;
    [HideInInspector] public GameObject[] prefabsInimigos;

    public event Action OnDestruido;

    public float VidaPct => vidaMaxima > 0f ? vidaAtual / vidaMaxima : 0f;

    float vidaAtual;
    bool  destruido;
    float elapsed;
    int   onda;

    SpriteRenderer srCore, srGlow;
    LineRenderer   lrRingA, lrRingB, lrHpArc;
    GameObject[]   shards = new GameObject[6];
    readonly System.Collections.Generic.List<GameObject> _beamsAtivos = new();

    static Sprite _disco, _shard;

    public void Iniciar(float vida, GameObject[] prefabs, float intervalo)
    {
        vidaAtual = vidaMaxima = vida;
        prefabsInimigos = prefabs;
        intervaloSpawn  = intervalo;

        ConstruirVisual();
        StartCoroutine(PulsoLoop());
        StartCoroutine(SpawnLoop());
        StartCoroutine(TickDano());
        StartCoroutine(LaserLoop());

        FlowField.AlvoOverride = transform;
    }

    void OnDestroy()
    {
        if (FlowField.AlvoOverride == transform)
            FlowField.AlvoOverride = null;
        foreach (var b in _beamsAtivos)
            if (b != null) Destroy(b);
        _beamsAtivos.Clear();
    }

    void Update() => elapsed += Time.deltaTime;

    // ── Dano (tick periódico por inimigos em range) ───────────────────────────

    IEnumerator TickDano()
    {
        while (!destruido && this != null)
        {
            yield return new WaitForSeconds(0.8f);
            if (destruido || this == null) yield break;

            var cols = Physics2D.OverlapCircleAll(transform.position, 1.4f);
            float danoTotal = 0f;
            foreach (var col in cols)
            {
                var ic = col.GetComponent<InimigoController>()
                       ?? col.GetComponentInParent<InimigoController>();
                if (ic != null) danoTotal += ic.danoAtual;
            }
            if (danoTotal > 0f) ReceberDano(danoTotal);
        }
    }

    void ReceberDano(float dano)
    {
        if (destruido) return;
        vidaAtual = Mathf.Max(0f, vidaAtual - dano);
        AtualizarHpArc();
        StartCoroutine(FlashDano());
        if (vidaAtual <= 0f) IniciarDestruicao();
    }

    void IniciarDestruicao()
    {
        destruido = true;
        if (FlowField.AlvoOverride == transform) FlowField.AlvoOverride = null;
        StartCoroutine(AnimarDestruicao());
    }

    // ── Spawn de ondas ────────────────────────────────────────────────────────

    IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(2f);
        while (!destruido && this != null)
        {
            float intervalo = Mathf.Max(1.5f, intervaloSpawn - onda * 0.25f);
            yield return new WaitForSeconds(intervalo);
            if (destruido || this == null) yield break;
            int qtd = Mathf.Min(2 + onda / 3, 8);
            SpawnOnda(qtd);
            onda++;
        }
    }

    void SpawnOnda(int qtd)
    {
        if (prefabsInimigos == null || prefabsInimigos.Length == 0) return;
        var ge = GerenciadorEventos.Instance;
        Vector2 centro = transform.position;
        int maskObst = ge != null && ge.camadasObstaculo != 0 ? (int)ge.camadasObstaculo : (1 << 3);

        for (int i = 0; i < qtd; i++)
        {
            for (int t = 0; t < 40; t++)
            {
                float ang  = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                float dist = UnityEngine.Random.Range(14f, 22f);
                Vector2 pos = centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;
                if (ge != null && !ge.PosicaoValida(pos)) continue;
                if (Physics2D.OverlapCircle(pos, 1f, maskObst)) continue;
                var prefab = prefabsInimigos[UnityEngine.Random.Range(0, prefabsInimigos.Length)];
                if (prefab != null) Instantiate(prefab, new Vector3(pos.x, pos.y, 0f), Quaternion.identity);
                break;
            }
        }
    }

    // ── Visuais ───────────────────────────────────────────────────────────────

    void ConstruirVisual()
    {
        Vector2 pos = transform.position;

        // Glow externo
        var goGlow = new GameObject("NucleoGlow");
        goGlow.transform.SetParent(transform);
        goGlow.transform.position = pos;
        srGlow = goGlow.AddComponent<SpriteRenderer>();
        srGlow.sprite = GetDisco(); srGlow.color = new Color(0.1f, 1f, 0.55f, 0.15f);
        srGlow.sortingOrder = 5;
        goGlow.transform.localScale = Vector3.one * 6f;

        // Anéis giratórios
        lrRingA = CriarAnel(pos, 2.2f, 48, new Color(0.1f, 1f, 0.6f, 0.55f), 0.09f, 6);
        lrRingB = CriarAnel(pos, 1.5f, 32, new Color(0.05f, 0.8f, 0.45f, 0.35f), 0.05f, 6);

        // Shards (cristais em volta do core)
        for (int i = 0; i < 6; i++)
        {
            float ang2  = i * 60f * Mathf.Deg2Rad;
            Vector2 sPos = pos + new Vector2(Mathf.Cos(ang2), Mathf.Sin(ang2)) * 0.55f;
            var go = new GameObject($"Shard_{i}");
            go.transform.SetParent(transform);
            go.transform.position = sPos;
            go.transform.rotation = Quaternion.Euler(0f, 0f, i * 60f);
            var sr2 = go.AddComponent<SpriteRenderer>();
            sr2.sprite = GetShard(); sr2.color = new Color(0.05f, 0.9f, 0.55f, 0.85f);
            sr2.sortingOrder = 8;
            go.transform.localScale = new Vector3(0.28f, 1.0f, 1f);
            shards[i] = go;
        }

        // Core central
        var goCore = new GameObject("NucleoCore");
        goCore.transform.SetParent(transform);
        goCore.transform.position = pos;
        srCore = goCore.AddComponent<SpriteRenderer>();
        srCore.sprite = GetDisco(); srCore.color = new Color(0.15f, 1f, 0.6f, 0.95f);
        srCore.sortingOrder = 10;
        goCore.transform.localScale = Vector3.one * 0.5f;

        // Arco de HP ao redor do núcleo
        var goArc = new GameObject("HpArc");
        goArc.transform.SetParent(transform);
        goArc.transform.position = pos;
        lrHpArc = goArc.AddComponent<LineRenderer>();
        lrHpArc.useWorldSpace = true; lrHpArc.loop = false;
        lrHpArc.material = new Material(Shader.Find("Sprites/Default"));
        lrHpArc.sortingOrder = 11;
        lrHpArc.startWidth = lrHpArc.endWidth = 0.18f;
        AtualizarHpArc();
    }

    void AtualizarHpArc()
    {
        if (lrHpArc == null) return;
        float pct  = Mathf.Clamp01(VidaPct);
        int   segs = Mathf.Max(2, Mathf.RoundToInt(pct * 48));
        lrHpArc.positionCount = segs;
        const float RAIO = 2.7f;
        Vector2 centro = transform.position;
        for (int i = 0; i < segs; i++)
        {
            float ang = (i / (float)Mathf.Max(1, segs - 1)) * pct * 360f * Mathf.Deg2Rad;
            lrHpArc.SetPosition(i, centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * RAIO);
        }
        Color cor = Color.Lerp(new Color(1f, 0.15f, 0.1f, 0.9f), new Color(0.1f, 1f, 0.4f, 0.9f), pct);
        lrHpArc.startColor = lrHpArc.endColor = cor;
    }

    IEnumerator PulsoLoop()
    {
        float t = 0f;
        while (!destruido && this != null)
        {
            t += Time.deltaTime;
            float pulso = Mathf.Sin(t * 2.8f) * 0.5f + 0.5f;
            float pctHp = Mathf.Clamp01(VidaPct);

            if (srGlow != null) srGlow.color = new Color(0.1f, 1f, 0.55f, 0.1f + pulso * 0.12f);

            if (lrRingA != null) AtualizarAnelRot(lrRingA, transform.position, 2.2f, 48,  t * 45f);
            if (lrRingB != null) AtualizarAnelRot(lrRingB, transform.position, 1.5f, 32, -t * 30f);

            for (int i = 0; i < shards.Length; i++)
            {
                if (shards[i] == null) continue;
                float escY = 0.9f + Mathf.Sin(t * 3f + i * 1.05f) * 0.12f;
                shards[i].transform.localScale = new Vector3(0.28f, escY, 1f);
            }

            if (srCore != null)
            {
                Color cor = Color.Lerp(new Color(1f, 0.2f, 0.1f), new Color(0.15f, 1f, 0.6f), pctHp);
                srCore.color = new Color(cor.r, cor.g, cor.b, 0.8f + pulso * 0.15f);
                srCore.transform.localScale = Vector3.one * (0.45f + pulso * 0.1f);
            }

            yield return null;
        }
    }

    void AtualizarAnelRot(LineRenderer lr, Vector2 centro, float raio, int segs, float angOffset)
    {
        lr.positionCount = segs;
        for (int i = 0; i < segs; i++)
        {
            float ang = (360f / segs * i + angOffset) * Mathf.Deg2Rad;
            lr.SetPosition(i, centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raio);
        }
    }

    LineRenderer CriarAnel(Vector2 pos, float raio, int segs, Color cor, float larg, int order)
    {
        var go = new GameObject("Anel");
        go.transform.SetParent(transform);
        go.transform.position = pos;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true;
        lr.positionCount = segs;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = order;
        lr.startWidth = lr.endWidth = larg;
        lr.startColor = lr.endColor = cor;
        AtualizarAnelRot(lr, pos, raio, segs, 0f);
        return lr;
    }

    IEnumerator FlashDano()
    {
        if (srCore == null) yield break;
        Color orig = srCore.color;
        srCore.color = Color.red;
        yield return new WaitForSeconds(0.08f);
        if (srCore != null) srCore.color = orig;
    }

    IEnumerator AnimarDestruicao()
    {
        // Burst de partículas ao explodir
        for (int i = 0; i < 22; i++)
        {
            float ang = i / 22f * Mathf.PI * 2f;
            Vector2 bPos = (Vector2)transform.position + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 1.5f;
            var go = new GameObject("BurstNucleo");
            go.transform.position = bPos;
            var sr2 = go.AddComponent<SpriteRenderer>();
            sr2.sprite = GetDisco();
            Color c = Color.Lerp(new Color(0.1f, 1f, 0.5f), Color.red, UnityEngine.Random.value);
            sr2.color = c; sr2.sortingOrder = 13;
            go.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.1f, 0.35f);
            Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * UnityEngine.Random.Range(3f, 9f);
            go.AddComponent<ParticulaPortalFX>().Iniciar(vel, c);
        }

        // Implosão visual
        float dur = 0.65f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            if (srCore != null)
            {
                srCore.transform.localScale = Vector3.one * Mathf.Lerp(0.55f, 0f, p * p);
                srCore.color = new Color(1f, 0.1f, 0.05f, 1f - p);
            }
            if (srGlow  != null) srGlow.color = new Color(1f, 0.2f, 0.1f, Mathf.Lerp(0.3f, 0f, p));
            if (lrHpArc != null) { Color c = lrHpArc.startColor; c.a = Mathf.Lerp(1f, 0f, p); lrHpArc.startColor = lrHpArc.endColor = c; }
            yield return null;
        }

        OnDestruido?.Invoke();
    }

    // ── Laser ─────────────────────────────────────────────────────────────────

    IEnumerator LaserLoop()
    {
        yield return new WaitForSeconds(2f);
        while (!destruido && this != null)
        {
            yield return new WaitForSeconds(1.2f);
            if (destruido || this == null) yield break;

            // Acha o inimigo mais próximo dentro de 15 unidades
            var inimigos = UnityEngine.Object.FindObjectsByType<InimigoController>(FindObjectsSortMode.None);
            InimigoController alvo = null;
            float menorDist = float.MaxValue;
            foreach (var ic in inimigos)
            {
                if (ic == null) continue;
                float d = Vector2.Distance(transform.position, ic.transform.position);
                if (d < menorDist && d <= 15f) { menorDist = d; alvo = ic; }
            }

            if (alvo != null)
                StartCoroutine(DispararLaser(alvo));
        }
    }

    IEnumerator DispararLaser(InimigoController alvo)
    {
        if (alvo == null) yield break;

        // Carregamento: flash rápido no core
        if (srCore != null) { Color c = srCore.color; srCore.color = Color.white; yield return new WaitForSeconds(0.06f); if (srCore != null) srCore.color = c; }

        if (alvo == null || destruido) yield break;

        Vector2 origem  = transform.position;
        Vector2 destino = alvo.transform.position;

        // Glow externo (mais largo, semi-transparente)
        var goGlow = CriarBeam("LaserGlow", origem, destino,
            0.38f, new Color(0.1f, 1f, 0.55f, 0.3f),
            0.18f, new Color(0.1f, 1f, 0.55f, 0.1f), 14);
        // Feixe central (fino, brilhante)
        var goBeam = CriarBeam("LaserBeam", origem, destino,
            0.10f, new Color(0.85f, 1f, 0.92f, 1f),
            0.03f, new Color(0.1f,  1f, 0.5f,  0.8f), 15);
        _beamsAtivos.Add(goGlow); _beamsAtivos.Add(goBeam);

        // Dano + impacto
        alvo.ReceberDano(danoLaser, false);
        SpawnImpactoLaser(destino);

        // Fade out do feixe
        float dur = 0.28f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            float a = 1f - p;
            if (goGlow != null)
            {
                var lr = goGlow.GetComponent<LineRenderer>();
                lr.startColor = new Color(0.1f, 1f, 0.55f, 0.3f * a);
                lr.endColor   = new Color(0.1f, 1f, 0.55f, 0.1f * a);
                lr.startWidth = lr.endWidth = Mathf.Lerp(0.38f, 0.05f, p);
            }
            if (goBeam != null)
            {
                var lr = goBeam.GetComponent<LineRenderer>();
                lr.startColor = new Color(0.85f, 1f, 0.92f, a);
                lr.endColor   = new Color(0.1f,  1f, 0.5f,  0.8f * a);
                lr.startWidth = lr.endWidth = Mathf.Lerp(0.1f, 0.01f, p);
            }
            yield return null;
        }
        _beamsAtivos.Remove(goGlow); _beamsAtivos.Remove(goBeam);
        if (goGlow != null) Destroy(goGlow);
        if (goBeam != null) Destroy(goBeam);
    }

    GameObject CriarBeam(string nome, Vector2 a, Vector2 b,
                         float wS, Color cS, float wE, Color cE, int order)
    {
        var go = new GameObject(nome);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.SetPosition(0, a); lr.SetPosition(1, b);
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = order;
        lr.startWidth = wS; lr.endWidth   = wE;
        lr.startColor = cS; lr.endColor   = cE;
        return go;
    }

    void SpawnImpactoLaser(Vector2 pos)
    {
        for (int i = 0; i < 7; i++)
        {
            var go = new GameObject("SparkLaser");
            go.transform.position = pos;
            var sr2 = go.AddComponent<SpriteRenderer>();
            sr2.sprite = GetDisco();
            sr2.color = new Color(0.2f, 1f, 0.6f);
            sr2.sortingOrder = 16;
            go.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.05f, 0.16f);
            Vector2 vel = UnityEngine.Random.insideUnitCircle * UnityEngine.Random.Range(1.5f, 4f);
            go.AddComponent<ParticulaPortalFX>().Iniciar(vel, sr2.color);
        }
    }

    // ── Sprites procedurais ───────────────────────────────────────────────────

    static Sprite GetDisco()
    {
        if (_disco != null) return _disco;
        int sz = 64;
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
        _disco = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
        return _disco;
    }

    static Sprite GetShard()
    {
        if (_shard != null) return _shard;
        int w = 16, h = 64;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = w * 0.5f, cy = h * 0.5f;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float ax = 1f - Mathf.Pow(Mathf.Abs(x - cx) / cx, 0.6f);
            float ay = 1f - Mathf.Pow(Mathf.Abs(y - cy) / cy, 1.2f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, ax * ay));
        }
        tex.Apply();
        _shard = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), h);
        return _shard;
    }
}
