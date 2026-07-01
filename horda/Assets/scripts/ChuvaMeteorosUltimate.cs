using System.Collections;
using UnityEngine;

public class ChuvaMeteorosUltimate : MonoBehaviour, IUltimateCosmetico
{
    // Co-op: roda só o visual no fantoche do colega (sem dano).
    bool cosmetico;
    public void ExecutarCosmetico() { if (ativo) return; cosmetico = true; StartCoroutine(CorotinaAtivacao()); }

    [Header("Configurações")]
    public float raio             = 14f;
    public float duracao          = 8f;
    public float cooldown         = 25f;
    public float intervaloMeteoro = 0.65f;
    public float danoMeteoro      = 25f;
    public float raioImpacto      = 2.4f;

    public float CooldownRestante => cooldownRestante;
    public bool  Ativo            => ativo;

    private float cooldownRestante;
    private bool  ativo;
    private PlayerStats playerStats;

    static readonly Color COR_NUCLEO = new Color(1.00f, 0.95f, 0.72f);
    static readonly Color COR_FOGO   = new Color(1.00f, 0.44f, 0.08f);
    static readonly Color COR_BRASA  = new Color(1.00f, 0.12f, 0.00f);
    static readonly Color COR_FUMO   = new Color(0.22f, 0.10f, 0.05f);

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

    void SincronizarUI()
    {
        if (playerStats == null) return;
        playerStats.ultimateChargeTime = cooldown - Mathf.Max(0f, cooldownRestante);
        playerStats.ultimateReady      = cooldownRestante <= 0f && !ativo;
    }

    // ─── COROUTINE PRINCIPAL ────────────────────────────────────────────────

    IEnumerator CorotinaAtivacao()
    {
        ativo            = true;
        cooldownRestante = cooldown;

        float elapsed = 0f;
        float proximo  = 0f;
        while (elapsed < duracao)
        {
            elapsed += Time.deltaTime;
            proximo  -= Time.deltaTime;
            if (proximo <= 0f)
            {
                proximo = intervaloMeteoro;
                StartCoroutine(AnimarMeteoro(EscolherAlvo()));
            }
            yield return null;
        }

        ativo = false;
    }

    // ─── LÓGICA ─────────────────────────────────────────────────────────────

    Vector2 EscolherAlvo()
    {
        foreach (var c in Physics2D.OverlapCircleAll(transform.position, raio))
        {
            if (c.gameObject == gameObject) continue;
            var root = ResolverInimigo(c.gameObject);
            if (root != null) return root.transform.position;
        }
        return (Vector2)transform.position + Random.insideUnitCircle * raio;
    }

    static GameObject ResolverInimigo(GameObject go)
    {
        GameObject root = null;
        var ic = go.GetComponent<InimigoController>() ?? go.GetComponentInParent<InimigoController>();
        if (ic != null) root = ic.gameObject;
        if (root == null) { var mi = go.GetComponent<movi_inimigo>() ?? go.GetComponentInParent<movi_inimigo>(); if (mi != null) root = mi.gameObject; }
        if (root == null) { var bc = go.GetComponent<BossController>() ?? go.GetComponentInParent<BossController>(); if (bc != null) root = bc.gameObject; }
        if (root == null) { var bp = go.GetComponent<BossPrincesa>() ?? go.GetComponentInParent<BossPrincesa>(); if (bp != null) root = bp.gameObject; }
        if (root == null) return null;
        if (go.GetComponentInParent<ProjetilHomingPrincesa>(true)   != null) return null;
        if (go.GetComponentInParent<ProjetilEspecialPrincesa>(true) != null) return null;
        return root;
    }

    void AplicarDano(Vector2 centro)
    {
        float raioEfetivo = raioImpacto;
        float danoEfetivo = danoMeteoro;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.MeteorosMaior))
        {
            raioEfetivo *= 1.5f;
            danoEfetivo *= 1.3f;
        }
        foreach (var c in Physics2D.OverlapCircleAll(centro, raioEfetivo))
        {
            var ic = c.GetComponent<InimigoController>() ?? c.GetComponentInParent<InimigoController>();
            if (ic != null && !cosmetico) // co-op: cópia cosmética só faz o visual
            {
                ic.ReceberDano(danoEfetivo, false);
                if (SkillEvolutionManager.Tem(SkillEvolutionType.MeteorosDuploImpacto))
                    ic.ReceberDano(danoEfetivo, false);
            }
        }
    }

    // ─── METEORO ────────────────────────────────────────────────────────────

    IEnumerator AnimarMeteoro(Vector2 destino)
    {
        // Vem de cima com leve desvio lateral para parecer oblíquo
        float lateralOffset = Random.Range(-2.5f, 2.5f);
        Vector2 origem = destino + new Vector2(lateralOffset, 10f + Random.Range(0f, 3f));
        Vector2 dir    = (destino - origem).normalized;
        float ang      = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion rot = Quaternion.Euler(0f, 0f, ang - 90f);

        // Marcador
        var marcador = new GameObject("MarcadorMeteoro");
        marcador.transform.position = destino;
        StartCoroutine(AnimarMarcador(marcador, destino, 0.32f));

        // Cauda de fogo — 2 layers (núcleo brilhante + fumaça externa)
        const int TAIL = 12;
        var goFire  = CriarTailLR("TailFire",  TAIL, 12,
            new Color(COR_NUCLEO.r, COR_NUCLEO.g, COR_NUCLEO.b, 0.95f),
            new Color(COR_BRASA.r,  COR_BRASA.g,  COR_BRASA.b,  0f), 0.40f, 0.04f);
        var goSmoke = CriarTailLR("TailSmoke", TAIL, 11,
            new Color(COR_FOGO.r, COR_FOGO.g, COR_FOGO.b, 0.55f),
            new Color(COR_FUMO.r, COR_FUMO.g, COR_FUMO.b, 0f), 0.80f, 0.10f);

        // Cabeça do meteoro — disco laranja externo + núcleo branco
        var goGlow = CriarDiscoMeteoro("MeteoroGlow", 24, 13,
            new Color(COR_FOGO.r, COR_FOGO.g, COR_FOGO.b, 0.7f),
            new Vector3(1.0f, 1.3f, 1f));
        var goCore = CriarDiscoMeteoro("MeteoroCore", 16, 14,
            COR_NUCLEO,
            new Vector3(0.45f, 0.58f, 1f));

        float dur    = 0.30f;
        int   pFrame = 0;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p   = t / dur;
            Vector2 pos = Vector2.Lerp(origem, destino, p);

            // Cabeça
            goGlow.transform.position = pos; goGlow.transform.rotation = rot;
            goCore.transform.position = pos; goCore.transform.rotation = rot;

            // Cauda: ponto 0 na cabeça, ponto N na extremidade da cauda
            float tailLen = Mathf.Lerp(1.5f, 5.5f, p); // cresce enquanto cai
            for (int i = 0; i < TAIL; i++)
            {
                float ti   = i / (float)(TAIL - 1);
                float jit  = (i > 0 && i < TAIL - 1) ? Random.Range(-0.04f, 0.04f) : 0f;
                Vector2 pt = pos - dir * (ti * tailLen) + Vector2.right * jit;
                goFire.GetComponent<LineRenderer>().SetPosition(i, pt);
                goSmoke.GetComponent<LineRenderer>().SetPosition(i, pt + Random.insideUnitCircle * ti * 0.12f);
            }

            // Partículas de fogo escapando da cauda
            pFrame++;
            if (pFrame % 2 == 0)
            {
                var fp = new GameObject("FirePart");
                fp.transform.position = pos - (Vector2)(dir * Random.Range(0.3f, tailLen * 0.7f))
                                            + Random.insideUnitCircle * 0.18f;
                fp.transform.localScale = Vector3.one * Random.Range(0.04f, 0.15f);
                var fsr = fp.AddComponent<SpriteRenderer>();
                fsr.sprite = GerarDisco(8); fsr.sortingOrder = 10;
                fsr.color  = Random.value < 0.35f ? COR_NUCLEO : COR_FOGO;
                fp.AddComponent<AutoDestroyFadeMove>().Iniciar(
                    new Vector2(Random.Range(-0.6f, 0.6f), Random.Range(0.2f, 1.4f)) * (1f - p * 0.5f),
                    Random.Range(0.15f, 0.35f));
                Destroy(fp, 0.5f);
            }

            yield return null;
        }

        Destroy(goGlow); Destroy(goCore);
        Destroy(goFire); Destroy(goSmoke);
        Destroy(marcador);

        AplicarDano(destino);
        StartCoroutine(AnimarImpacto(destino));
    }

    static GameObject CriarTailLR(string nome, int n, int order, Color cStart, Color cEnd, float wStart, float wEnd)
    {
        var go = new GameObject(nome);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.positionCount = n;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = order;
        lr.startWidth = wStart; lr.endWidth = wEnd;
        lr.startColor = cStart; lr.endColor = cEnd;
        return go;
    }

    static GameObject CriarDiscoMeteoro(string nome, int sz, int order, Color cor, Vector3 escala)
    {
        var go = new GameObject(nome);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GerarDisco(sz); sr.sortingOrder = order; sr.color = cor;
        go.transform.localScale = escala;
        return go;
    }

    // ─── MARCADOR DE ALVO ───────────────────────────────────────────────────

    IEnumerator AnimarMarcador(GameObject root, Vector2 pos, float dur)
    {
        const int S = 28;
        var lr = root.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = S;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = 10;

        // Cruz X
        for (int xi = 0; xi < 2; xi++)
        {
            float cAng = (45f + xi * 90f) * Mathf.Deg2Rad;
            var cGO = new GameObject($"Cruz{xi}");
            cGO.transform.SetParent(root.transform);
            var clr = cGO.AddComponent<LineRenderer>();
            clr.useWorldSpace = true; clr.positionCount = 2;
            clr.material = new Material(Shader.Find("Sprites/Default"));
            clr.sortingOrder = 11; clr.startWidth = clr.endWidth = 0.05f;
            clr.startColor = clr.endColor = new Color(COR_BRASA.r, COR_BRASA.g, COR_BRASA.b, 0.75f);
            float d = raioImpacto * 0.65f;
            clr.SetPosition(0, pos + new Vector2( Mathf.Cos(cAng), Mathf.Sin(cAng)) * d);
            clr.SetPosition(1, pos + new Vector2(-Mathf.Cos(cAng),-Mathf.Sin(cAng)) * d);
        }

        // Sombra de chão
        var sGO = new GameObject("Sombra");
        sGO.transform.SetParent(root.transform);
        sGO.transform.position = pos;
        var ssr = sGO.AddComponent<SpriteRenderer>();
        ssr.sprite = GerarDisco(16); ssr.sortingOrder = 9;
        ssr.color  = new Color(COR_BRASA.r, COR_BRASA.g, COR_BRASA.b, 0.30f);
        sGO.transform.localScale = Vector3.one * raioImpacto * 1.9f;

        float r0 = raioImpacto * 2.1f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (root == null) yield break;
            float p     = t / dur;
            float r     = Mathf.Lerp(r0, raioImpacto, p);
            float pulso = Mathf.Sin(p * Mathf.PI * 14f) * 0.5f + 0.5f;
            lr.startWidth = lr.endWidth = 0.04f + pulso * 0.07f;
            lr.startColor = lr.endColor = new Color(COR_BRASA.r, COR_BRASA.g, COR_BRASA.b, 0.45f + pulso * 0.5f);
            for (int i = 0; i < S; i++)
            {
                float a = (360f / S) * i * Mathf.Deg2Rad;
                lr.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
            yield return null;
        }
    }

    // ─── IMPACTO ────────────────────────────────────────────────────────────

    IEnumerator AnimarImpacto(Vector2 pos)
    {
        // Flash central branco-laranja
        var flash = new GameObject("FlashImpacto");
        flash.transform.position = pos;
        var fsr = flash.AddComponent<SpriteRenderer>();
        fsr.sprite = GerarDisco(24); fsr.sortingOrder = 20;
        fsr.color  = new Color(1f, 0.92f, 0.70f, 0.95f);
        flash.transform.localScale = Vector3.one * 0.4f;
        float durFlash = 0.20f;
        for (float t = 0f; t < durFlash; t += Time.deltaTime)
        {
            float p = t / durFlash;
            flash.transform.localScale = Vector3.one * Mathf.Lerp(0.4f, raioImpacto * 2.4f, Mathf.Sqrt(p));
            fsr.color = new Color(1f, Mathf.Lerp(0.92f, 0.35f, p), 0f, Mathf.Lerp(0.95f, 0f, p * p));
            yield return null;
        }
        Destroy(flash);

        // 3 anéis concêntricos em tempos diferentes
        StartCoroutine(AnelExpansao(pos, raioImpacto * 1.2f, 0.32f, COR_NUCLEO, 28));
        StartCoroutine(AnelExpansao(pos, raioImpacto * 2.0f, 0.44f, COR_FOGO,   22));
        StartCoroutine(AnelExpansao(pos, raioImpacto * 2.8f, 0.60f, COR_BRASA,  16));

        // Estilhaços rochosos (10 fragmentos)
        for (int i = 0; i < 10; i++)
        {
            float ang   = i / 10f * Mathf.PI * 2f + Random.Range(-0.25f, 0.25f);
            float speed = Random.Range(4f, 11f);
            var dp = new GameObject("Estilhaco");
            dp.transform.position   = pos + Random.insideUnitCircle * 0.3f;
            dp.transform.localScale = Vector3.one * Random.Range(0.08f, 0.24f);
            var dsr = dp.AddComponent<SpriteRenderer>();
            dsr.sprite = GerarDisco(8); dsr.sortingOrder = 15;
            dsr.color  = i % 3 == 0 ? COR_NUCLEO : (i % 3 == 1 ? COR_FOGO : COR_BRASA);
            dp.AddComponent<AutoDestroyFadeMove>().Iniciar(
                new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * speed, Random.Range(0.22f, 0.42f));
            Destroy(dp, 0.6f);
        }

        // Brasas subindo (8)
        for (int i = 0; i < 8; i++)
        {
            var ep = new GameObject("Brasa");
            ep.transform.position   = pos + Random.insideUnitCircle * raioImpacto * 0.55f;
            ep.transform.localScale = Vector3.one * Random.Range(0.04f, 0.14f);
            var esr = ep.AddComponent<SpriteRenderer>();
            esr.sprite = GerarDisco(8); esr.sortingOrder = 14;
            esr.color  = Random.value < 0.45f ? COR_NUCLEO : COR_FOGO;
            ep.AddComponent<AutoDestroyFadeMove>().Iniciar(
                Vector2.up * Random.Range(1.2f, 4.5f) + Random.insideUnitCircle * 0.6f,
                Random.Range(0.6f, 1.4f));
            Destroy(ep, 1.6f);
        }

        // Marca de cratera no chão
        StartCoroutine(CraterChao(pos));
    }

    IEnumerator AnelExpansao(Vector2 pos, float raioMax, float dur, Color cor, int segs)
    {
        var go = new GameObject("Anel");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = segs;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = 16;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p  = t / dur;
            float pe = 1f - Mathf.Pow(1f - p, 2.2f);
            float r  = Mathf.Lerp(0.1f, raioMax, pe);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.42f, 0.02f, p);
            lr.startColor = lr.endColor = new Color(cor.r, cor.g, cor.b, 1f - p);
            for (int i = 0; i < segs; i++)
            {
                float a = (360f / segs) * i * Mathf.Deg2Rad;
                lr.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
            yield return null;
        }
        Destroy(go);
    }

    IEnumerator CraterChao(Vector2 pos)
    {
        var go = new GameObject("CraterMarca");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GerarDisco(24); sr.sortingOrder = 8;
        go.transform.localScale = Vector3.one * raioImpacto * 1.6f;
        float dur = 1.6f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            float a = p < 0.08f ? Mathf.Lerp(0f, 0.60f, p / 0.08f)
                                 : Mathf.Lerp(0.60f, 0f, (p - 0.08f) / 0.92f);
            sr.color = new Color(COR_FUMO.r, COR_FUMO.g, COR_FUMO.b, a);
            yield return null;
        }
        Destroy(go);
    }

    // ─── SPRITES PROCEDURAIS ─────────────────────────────────────────────────

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

    static Sprite GerarSprite(int sz, Color cor)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cx));
            tex.SetPixel(x, y, new Color(cor.r, cor.g, cor.b, Mathf.Clamp01(1f - d / cx)));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }
}
