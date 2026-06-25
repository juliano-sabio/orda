using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CristalEvento : MonoBehaviour
{
    [HideInInspector] public float vidaBase = 500f;
    [HideInInspector] public Color corBase  = new Color(0.25f, 0.75f, 1f);

    // Co-op: no cliente é só visual (sem física/InimigoController/HP); o host roda o gameplay.
    [HideInInspector] public bool cosmetico;
    int objEvtId;

    public event Action OnDestruido;
    public bool EstaDestruido { get; private set; }

    InimigoController ic;
    SpriteRenderer srGlowOuter, srGlow, srCristal, srCore;
    SpriteRenderer[] srFrags;
    LineRenderer lrHpArc, lrBeam;
    float[] fragPhase;

    // ── Inicialização ─────────────────────────────────────────────────────────

    public void Iniciar()
    {
        // Tag para ser reconhecido como alvo pelas skills
        try { gameObject.tag = "Enemy"; }
        catch { try { gameObject.tag = "enemy"; } catch { } }

        // Layer: copia de um inimigo real para garantir que a matrix de colisão permita interação
        var anyEnemy = FindFirstObjectByType<InimigoController>();
        if (anyEnemy != null)
            gameObject.layer = anyEnemy.gameObject.layer;

        ConstruirFisica();
        ConstruirVisual();
        CriarHpArc();

        InimigoController.OnPreMorte += OnPreMorteHandler;
        StartCoroutine(PulsoLoop());
        StartCoroutine(ParticulasLoop());

        // co-op: host registra p/ o cliente reconstruir a cópia visual.
        if (NetSpawn.EmRede && NetSpawn.PodeSpawnar && CoopProgressao.Instance != null)
            objEvtId = CoopProgressao.Instance.RegistrarObjEvento(7, transform.position, 0f, 0f, corBase);
    }

    // Cliente co-op: só o visual (sem física/HP/morte — o host é autoritativo).
    void Start()
    {
        if (!cosmetico) return;
        ConstruirVisual();
        StartCoroutine(PulsoLoop());
        StartCoroutine(ParticulasLoop());
        IndicadorSlime.Criar(transform, corBase, "Cristal!");
    }

    void OnDestroy()
    {
        InimigoController.OnPreMorte -= OnPreMorteHandler;
        if (objEvtId != 0 && CoopProgressao.Instance != null)
            CoopProgressao.Instance.RemoverObjEvento(objEvtId);
    }

    // ── Física ───────────────────────────────────────────────────────────────

    void ConstruirFisica()
    {
        // Rigidbody kinematic: indispensável para OnTriggerEnter2D dos projéteis disparar
        var rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType       = RigidbodyType2D.Kinematic;
        rb.gravityScale   = 0f;
        rb.constraints    = RigidbodyConstraints2D.FreezeAll;

        // Trigger: permite que projéteis (que também são trigger) o detectem
        var col    = gameObject.AddComponent<CircleCollider2D>();
        col.radius    = 0.5f;
        col.isTrigger = true;

        ic                      = gameObject.AddComponent<InimigoController>();
        ic.vidaMaxima           = vidaBase;
        ic.vidaAtual            = vidaBase;
        ic.xpPorOrbe            = 0f;
        ic.minOrbs              = 0;
        ic.maxOrbs              = 0;
        ic.drops                = new List<DropEntry>();
        ic.mostrarDanoFlutuante = true;
        ic.mostrarDanoAposMorte = false;
    }

    // ── Visual ───────────────────────────────────────────────────────────────

    void ConstruirVisual()
    {
        // Halo externo no chão — respiração lenta
        {
            var go = Filho("GlowOuter", Vector3.zero, Vector3.one * 6.5f);
            srGlowOuter = go.AddComponent<SpriteRenderer>();
            srGlowOuter.sprite       = GerarDisco();
            srGlowOuter.color        = new Color(corBase.r, corBase.g, corBase.b, 0.05f);
            srGlowOuter.sortingOrder = 1;
        }

        // Glow interno — pulsação principal
        {
            var go = Filho("Glow", Vector3.zero, Vector3.one * 3f);
            srGlow = go.AddComponent<SpriteRenderer>();
            srGlow.sprite       = GerarDisco();
            srGlow.color        = new Color(corBase.r, corBase.g, corBase.b, 0.20f);
            srGlow.sortingOrder = 2;
        }

        // Raio de energia (sobe para o céu)
        CriarBeam();

        // 3 fragmentos secundários com rotações diferentes
        srFrags   = new SpriteRenderer[3];
        fragPhase = new float[] { 0f, 2.1f, 4.3f };

        var fragDados = new (Vector3 pos, float rot, Vector3 scale, float brilho, int order)[]
        {
            (new Vector3(-0.30f, -0.12f, 0f), -30f, new Vector3(0.48f, 0.82f, 1f), 0.70f, 4),
            (new Vector3( 0.32f, -0.08f, 0f),  26f, new Vector3(0.52f, 0.88f, 1f), 0.76f, 4),
            (new Vector3( 0.09f,  0.10f, 0f), -11f, new Vector3(0.36f, 0.60f, 1f), 0.84f, 6),
        };
        for (int i = 0; i < 3; i++)
        {
            var d  = fragDados[i];
            var go = Filho($"Frag{i}", d.pos, d.scale);
            go.transform.localEulerAngles = new Vector3(0f, 0f, d.rot);
            srFrags[i]               = go.AddComponent<SpriteRenderer>();
            srFrags[i].sprite        = GerarShard();
            srFrags[i].sortingOrder  = d.order;
            float b = d.brilho;
            srFrags[i].color = new Color(corBase.r * b + 0.12f, corBase.g * b + 0.04f, corBase.b * b);
        }

        // Cristal central principal
        {
            var go = Filho("CristalBody", new Vector3(0f, 0.06f, 0f), new Vector3(0.78f, 1.22f, 1f));
            srCristal               = go.AddComponent<SpriteRenderer>();
            srCristal.sprite        = GerarShard();
            srCristal.sortingOrder  = 5;
            srCristal.color = new Color(
                Mathf.Min(corBase.r * 0.88f + 0.22f, 1f),
                Mathf.Min(corBase.g * 0.94f + 0.04f, 1f),
                corBase.b
            );
        }

        // Núcleo brilhante — flicker rápido
        {
            var go = Filho("Core", Vector3.zero, Vector3.one * 0.42f);
            srCore               = go.AddComponent<SpriteRenderer>();
            srCore.sprite        = GerarDisco();
            srCore.color         = new Color(1f, 1f, 1f, 0.65f);
            srCore.sortingOrder  = 7;
        }
    }

    void CriarBeam()
    {
        var go  = Filho("Beam", Vector3.zero, Vector3.one);
        lrBeam  = go.AddComponent<LineRenderer>();
        lrBeam.useWorldSpace = false;
        lrBeam.loop          = false;
        lrBeam.material      = new Material(Shader.Find("Sprites/Default"));
        lrBeam.sortingOrder  = 3;

        float[] ys = { 0f, 0.35f, 0.85f, 1.55f, 2.30f, 3.10f };
        float[] ws = { 0.26f, 0.19f, 0.13f, 0.08f, 0.04f, 0.00f };
        lrBeam.positionCount = ys.Length;
        var curve = new AnimationCurve();
        for (int i = 0; i < ws.Length; i++)
            curve.AddKey(i / (float)(ws.Length - 1), ws[i]);
        lrBeam.widthCurve = curve;
        for (int i = 0; i < ys.Length; i++)
            lrBeam.SetPosition(i, new Vector3(0f, ys[i], 0f));

        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white,                                 0.00f),
                new GradientColorKey(new Color(0.72f, 0.92f, 1f),               0.10f),
                new GradientColorKey(new Color(corBase.r, corBase.g, corBase.b), 0.50f),
                new GradientColorKey(new Color(corBase.r, corBase.g, corBase.b), 1.00f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.85f, 0.00f),
                new GradientAlphaKey(0.55f, 0.20f),
                new GradientAlphaKey(0.22f, 0.70f),
                new GradientAlphaKey(0.00f, 1.00f),
            }
        );
        lrBeam.colorGradient = grad;
    }

    void CriarHpArc()
    {
        var go  = Filho("HpArc", Vector3.zero, Vector3.one);
        lrHpArc = go.AddComponent<LineRenderer>();
        lrHpArc.useWorldSpace = true;
        lrHpArc.loop          = false;
        lrHpArc.material      = new Material(Shader.Find("Sprites/Default"));
        lrHpArc.sortingOrder  = 8;
        lrHpArc.startWidth    = lrHpArc.endWidth = 0.10f;
        AtualizarHpArc();
    }

    void AtualizarHpArc()
    {
        if (lrHpArc == null || ic == null) return;
        float pct  = ic.vidaMaxima > 0f ? Mathf.Clamp01(ic.vidaAtual / ic.vidaMaxima) : 0f;
        int   segs = Mathf.Max(2, Mathf.RoundToInt(pct * 56));
        lrHpArc.positionCount = segs;
        const float RAIO = 1.1f;
        Vector2 centro = transform.position;
        for (int i = 0; i < segs; i++)
        {
            float ang = (i / (float)Mathf.Max(1, segs - 1)) * pct * 360f * Mathf.Deg2Rad - Mathf.PI * 0.5f;
            lrHpArc.SetPosition(i, centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * RAIO);
        }
        Color cor = Color.Lerp(new Color(1f, 0.2f, 0.1f, 0.9f), new Color(corBase.r, corBase.g, corBase.b, 0.9f), pct);
        lrHpArc.startColor = lrHpArc.endColor = cor;
    }

    GameObject Filho(string nome, Vector3 localPos, Vector3 localScale)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(transform);
        go.transform.localPosition = localPos;
        go.transform.localScale    = localScale;
        return go;
    }

    // ── Loops visuais ────────────────────────────────────────────────────────

    IEnumerator PulsoLoop()
    {
        float[] basAngles = { -30f, 26f, -11f };
        float[] baseTints = {  0.70f, 0.76f, 0.84f };
        float t = 0f;

        while (this != null && !EstaDestruido)
        {
            t += Time.deltaTime;

            float pulso   = Mathf.Sin(t * 2.4f) * 0.5f + 0.5f;
            float lento   = Mathf.Sin(t * 0.75f) * 0.5f + 0.5f;
            float flicker = Mathf.Sin(t * 9.0f)  * 0.5f + 0.5f;
            float sway    = Mathf.Sin(t * 1.1f)  * 0.05f;
            float bob     = Mathf.Sin(t * 1.4f)  * 0.045f;

            if (srGlowOuter != null)
                srGlowOuter.color = new Color(corBase.r, corBase.g, corBase.b, 0.03f + lento * 0.045f);

            if (srGlow != null)
            {
                srGlow.color = new Color(corBase.r, corBase.g, corBase.b, 0.09f + pulso * 0.19f);
                srGlow.transform.localScale = Vector3.one * (2.4f + pulso * 0.75f);
            }

            if (srFrags != null)
            {
                for (int i = 0; i < srFrags.Length; i++)
                {
                    if (srFrags[i] == null) continue;
                    float fp = Mathf.Sin(t * 1.65f + fragPhase[i]) * 0.5f + 0.5f;
                    var ea = srFrags[i].transform.localEulerAngles;
                    ea.z = basAngles[i] + Mathf.Sin(t * 0.7f + fragPhase[i]) * 5.5f;
                    srFrags[i].transform.localEulerAngles = ea;
                    float b = baseTints[i] + fp * 0.11f;
                    srFrags[i].color = new Color(corBase.r * b + 0.12f, corBase.g * b + 0.04f, corBase.b * b);
                }
            }

            if (srCristal != null)
            {
                float bright = 0.80f + pulso * 0.20f;
                srCristal.color = new Color(
                    Mathf.Min(corBase.r * bright + 0.15f, 1f),
                    Mathf.Min(corBase.g * bright + 0.02f, 1f),
                    corBase.b * bright
                );
                srCristal.transform.localPosition = new Vector3(0f, 0.06f + bob, 0f);
            }

            if (srCore != null)
                srCore.color = new Color(1f, 1f, 1f, 0.32f + flicker * 0.38f);

            if (lrBeam != null)
            {
                float[] ys = { 0f, 0.35f, 0.85f, 1.55f, 2.30f, 3.10f };
                for (int i = 0; i < lrBeam.positionCount; i++)
                {
                    float h = i / (float)(lrBeam.positionCount - 1);
                    lrBeam.SetPosition(i, new Vector3(sway * h * 1.6f, ys[i], 0f));
                }
            }

            AtualizarHpArc();
            yield return null;
        }
    }

    IEnumerator ParticulasLoop()
    {
        while (this != null && !EstaDestruido)
        {
            yield return new WaitForSeconds(0.18f);
            if (this == null || EstaDestruido) yield break;
            SpawnParticula();
            if (UnityEngine.Random.value < 0.12f)
                SpawnPulsoAnel();
        }
    }

    // Spawna a sparkle e delega a animação a um componente independente
    // (coroutines em "this" morrem instantaneamente quando o cristal é destruído,
    // o que deixava partículas congeladas na cena — o "rastro" reportado)
    void SpawnParticula()
    {
        var go = new GameObject("CristalSpark");
        var sr = go.AddComponent<SpriteRenderer>();

        bool isDiamond = UnityEngine.Random.value > 0.35f;
        sr.sprite = isDiamond ? GerarShard() : GerarDisco();

        Color cor = UnityEngine.Random.value > 0.30f
            ? new Color(corBase.r + 0.1f, corBase.g + 0.08f, corBase.b, 0.90f)
            : new Color(1f, 1f, 1f, 0.95f);
        sr.color        = cor;
        sr.sortingOrder = 9;

        float scale = UnityEngine.Random.Range(0.03f, 0.10f);
        go.transform.localScale = isDiamond
            ? new Vector3(scale, scale * UnityEngine.Random.Range(1.4f, 2.2f), 1f)
            : Vector3.one * scale;

        float ang  = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        float dist = UnityEngine.Random.Range(0.15f, 0.70f);
        go.transform.position    = (Vector2)transform.position + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;
        go.transform.eulerAngles = new Vector3(0f, 0f, UnityEngine.Random.Range(0f, 360f));

        Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * UnityEngine.Random.Range(0.2f, 0.8f)
                    + Vector2.up * UnityEngine.Random.Range(0.6f, 1.8f);
        float dur  = UnityEngine.Random.Range(0.7f, 1.3f);
        float spin = UnityEngine.Random.Range(-200f, 200f);

        go.AddComponent<CristalDriftAutoDestroy>().Iniciar(vel, cor, dur, spin);
    }

    void SpawnPulsoAnel()
    {
        var go = new GameObject("CristalRing");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 3;
        lr.startWidth    = lr.endWidth = 0.055f;
        lr.positionCount = 32;

        go.AddComponent<CristalRingAutoDestroy>().Iniciar(transform.position, new Color(corBase.r, corBase.g + 0.1f, corBase.b));
    }

    // ── Morte ────────────────────────────────────────────────────────────────

    void OnPreMorteHandler(InimigoController dying)
    {
        if (ic == null || dying != ic) return;
        EstaDestruido = true;
        SpawnBurstSincrono();
        OnDestruido?.Invoke();
    }

    void SpawnBurstSincrono()
    {
        for (int i = 0; i < 24; i++)
        {
            float ang = i / 24f * Mathf.PI * 2f;
            var go    = new GameObject("CristalBurst");
            go.transform.position    = transform.position;
            go.transform.eulerAngles = new Vector3(0f, 0f, UnityEngine.Random.Range(0f, 360f));

            bool useShard = i % 3 != 0;
            var sr2 = go.AddComponent<SpriteRenderer>();
            sr2.sprite       = useShard ? GerarShard() : GerarDisco();
            Color cor = i % 4 == 0
                ? new Color(1f, 1f, 1f, 1f)
                : Color.Lerp(corBase, Color.white, UnityEngine.Random.value * 0.4f);
            sr2.color        = cor;
            sr2.sortingOrder = 7;

            float scale = UnityEngine.Random.Range(0.08f, 0.28f);
            go.transform.localScale = useShard
                ? new Vector3(scale, scale * UnityEngine.Random.Range(1.5f, 2.5f), 1f)
                : Vector3.one * scale;

            float speed = UnityEngine.Random.Range(2.5f, 7.5f);
            Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * speed
                        + new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(0f, 2f));
            go.AddComponent<CristalDriftAutoDestroy>().Iniciar(vel, cor, UnityEngine.Random.Range(0.5f, 1.0f));
        }
    }

    // ── Sprites procedurais ──────────────────────────────────────────────────

    static Sprite _shard, _disco;

    // Shard facetado: ponto no topo, base menor, facetas esq/dir para simular 3D
    static Sprite GerarShard()
    {
        if (_shard != null) return _shard;
        int w = 40, h = 80;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        for (int py = 0; py < h; py++)
        for (int px = 0; px < w; px++)
        {
            float nx = (px + 0.5f - w * 0.5f) / (w * 0.5f);  // -1..1
            float ny = (py + 0.5f) / h;                        //  0..1 (baixo=0, cima=1)

            // Perfil de largura: base pequena → alarga até 30% → taper até ponta no topo
            float halfW;
            if (ny > 0.30f)
                halfW = 0.85f * (1f - (ny - 0.30f) / 0.70f);
            else
                halfW = 0.20f + 0.65f * (ny / 0.30f);

            float dist = Mathf.Abs(nx) - halfW;
            if (dist > 0f) { tex.SetPixel(px, py, Color.clear); continue; }

            float edgeFade = Mathf.Clamp01(-dist * (w * 0.6f));

            // Facetas: esquerda=clara, centro=highlight branco, direita=sombra
            float facet;
            if (nx < -0.08f)
                facet = Mathf.Lerp(0.88f, 0.72f, (-nx - 0.08f) / 0.92f);
            else if (nx <= 0.08f)
                facet = 1.00f;
            else
                facet = Mathf.Lerp(0.70f, 0.50f, (nx - 0.08f) / 0.92f);

            // Brilho extra perto da ponta
            float tipGlow = ny > 0.68f ? (ny - 0.68f) / 0.32f * 0.18f : 0f;
            facet = Mathf.Min(facet + tipGlow, 1f);

            float opacity = 0.70f + edgeFade * 0.30f;
            tex.SetPixel(px, py, new Color(facet, facet, facet, edgeFade * opacity));
        }

        tex.Apply();
        _shard = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), h);
        return _shard;
    }

    static Sprite GerarDisco()
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
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d / cx)));
        }
        tex.Apply();
        _disco = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
        return _disco;
    }
}

// Anima e destrói partículas do burst sem depender do MonoBehaviour do cristal
public class CristalDriftAutoDestroy : MonoBehaviour
{
    Vector2 vel;
    float duracao;
    float spin;
    Color cor0;
    SpriteRenderer sr;
    float t;

    public void Iniciar(Vector2 velocidade, Color corInicial, float dur, float rotacao = 0f)
    {
        vel     = velocidade;
        duracao = dur;
        spin    = rotacao;
        cor0    = corInicial;
        sr      = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        t += Time.deltaTime;
        if (spin != 0f) transform.Rotate(0f, 0f, spin * Time.deltaTime);
        transform.position += (Vector3)(vel * Time.deltaTime);
        vel *= Mathf.Pow(0.88f, Time.deltaTime * 60f);
        if (sr != null) { Color c = cor0; c.a = Mathf.Lerp(1f, 0f, t / duracao); sr.color = c; }
        if (t >= duracao) Destroy(gameObject);
    }
}

// Anima o anel expansivo e se autodestrói sem depender do MonoBehaviour do cristal
public class CristalRingAutoDestroy : MonoBehaviour
{
    Vector2 centro;
    Color corBase;
    LineRenderer lr;
    float duracao = 1.1f;
    float t;

    public void Iniciar(Vector2 centroFixo, Color cor)
    {
        centro  = centroFixo;
        corBase = cor;
        lr      = GetComponent<LineRenderer>();
    }

    void Update()
    {
        t += Time.deltaTime;
        if (t >= duracao) { Destroy(gameObject); return; }
        if (lr == null) return;

        float raio  = 0.4f + t * 2.2f;
        float alpha = Mathf.Lerp(0.55f, 0f, t / duracao);
        lr.startColor = lr.endColor = new Color(corBase.r, corBase.g, corBase.b, alpha);

        int segs = lr.positionCount;
        for (int i = 0; i < segs; i++)
        {
            float a = (i / (float)segs) * Mathf.PI * 2f;
            lr.SetPosition(i, centro + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * raio);
        }
    }
}
