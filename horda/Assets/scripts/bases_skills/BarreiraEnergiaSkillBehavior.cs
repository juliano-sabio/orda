using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BarreiraEnergiaSkillBehavior : SkillBehavior, ISkillComRecarga
{
    float vidaEscudo   = 80f;   // reduzido de 150
    float tempoRecarga = 12f;
    float escudoMax    = 80f;
    bool  emRecarga    = false;
    float timerRecarga = 0f;

    public bool  EmRecarga    => emRecarga;
    public float TimerRecarga => timerRecarga;
    public float RecargaTotal => tempoRecarga;

    // Visuais
    GameObject   rootVisual;
    LineRenderer lrExt, lrInt, lrCore;
    SpriteRenderer srGlow;
    GameObject[] particulas;
    float        angRot;
    float        elapsed;

    static readonly Color COR_CHEIO   = new Color(0.2f, 0.7f, 1f, 0.95f);
    static readonly Color COR_FRACO   = new Color(1f, 0.4f, 0.1f, 0.9f);
    static readonly Color COR_RECARGA = new Color(0.3f, 0.3f, 0.4f, 0.5f);

    public override void Initialize(PlayerStats stats) => base.Initialize(stats);

    static readonly Color COR_ORIG = new Color(0.2f, 0.7f, 1f);
    Color CorElemento() {
        if (skillData != null && skillData.appliedElement != ElementType.None)
            return ElementRegistry.Instance?.GetCor(skillData.appliedElement) ?? COR_ORIG;
        return COR_ORIG;
    }

    public void ConfigurarDeSkillData(SkillData data)
    {
        this.skillData = data;
        vidaEscudo   = data.healthBonus > 0f ? data.healthBonus : 80f;
        tempoRecarga = data.cooldown > 0f    ? data.cooldown    : 12f;
        escudoMax    = vidaEscudo;
    }

    void Start()
    {
        AtivarEscudo();
        CriarVisual();
        StartCoroutine(ParticulasOrbitando());
    }

    void OnDestroy()
    {
        if (rootVisual != null) Destroy(rootVisual);
    }

    void AtivarEscudo()
    {
        if (playerStats == null) return;
        // BarreiraFortificada — +80% de vida do escudo
        float mult = SkillEvolutionManager.Tem(SkillEvolutionType.BarreiraFortificada) ? 1.80f : 1f;
        playerStats.shieldPoints = escudoMax * mult;
        emRecarga = false; timerRecarga = 0f;
    }

    void Update()
    {
        if (playerStats == null) return;
        elapsed  += Time.deltaTime;
        angRot   += Time.deltaTime * 70f;

        AtualizarVisual();

        // Detecta escudo quebrado
        if (!emRecarga && playerStats.shieldPoints <= 0f)
        {
            emRecarga = true; timerRecarga = tempoRecarga;
            StartCoroutine(EfeitoQuebrando());
        }

        // Recarga
        if (emRecarga)
        {
            timerRecarga -= Time.deltaTime;
            if (timerRecarga <= 0f) { AtivarEscudo(); StartCoroutine(EfeitoRecuperando()); }
        }
    }

    // ── Criação visual ────────────────────────────────────────────────────────

    void CriarVisual()
    {
        if (playerStats == null) return;

        rootVisual = new GameObject("BarreiraVisual");
        rootVisual.transform.SetParent(playerStats.transform, false);
        rootVisual.transform.localPosition = Vector3.zero;

        // Anel externo girando
        var goExt = new GameObject("AnelExt");
        goExt.transform.SetParent(rootVisual.transform, false);
        lrExt = CriarAnel(goExt, 48, 0.85f, 0.10f, 8);

        // Anel interno contra-girando
        var goInt = new GameObject("AnelInt");
        goInt.transform.SetParent(rootVisual.transform, false);
        lrInt = CriarAnel(goInt, 32, 0.60f, 0.05f, 7);

        // Anel pulsante fino no core
        var goCore = new GameObject("AnelCore");
        goCore.transform.SetParent(rootVisual.transform, false);
        lrCore = CriarAnel(goCore, 24, 0.35f, 0.035f, 9);

        // Glow disc
        var goGlow = new GameObject("Glow");
        goGlow.transform.SetParent(rootVisual.transform, false);
        srGlow = goGlow.AddComponent<SpriteRenderer>();
        srGlow.sprite       = GerarDisco(64);
        srGlow.color        = new Color(COR_CHEIO.r, COR_CHEIO.g, COR_CHEIO.b, 0.06f);
        srGlow.sortingOrder = 6;
        goGlow.transform.localScale = Vector3.one * 1.7f;
    }

    LineRenderer CriarAnel(GameObject go, int segs, float raio, float larg, int order)
    {
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false; lr.loop = true; lr.positionCount = segs;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = order; lr.startWidth = lr.endWidth = larg;
        for (int i = 0; i < segs; i++)
        {
            float ang = 360f / segs * i * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(ang) * raio, Mathf.Sin(ang) * raio, 0f));
        }
        return lr;
    }

    void AtualizarVisual()
    {
        if (rootVisual == null || playerStats == null) return;

        rootVisual.transform.position = playerStats.transform.position;

        // Escudo destruído: oculta tudo completamente
        if (emRecarga)
        {
            rootVisual.SetActive(false);
            return;
        }
        rootVisual.SetActive(true);

        float pct   = Mathf.Clamp01(playerStats.shieldPoints / escudoMax);
        float pulso = Mathf.Sin(elapsed * 4f) * 0.5f + 0.5f;
        float pulso2 = Mathf.Sin(elapsed * 7f + 1f) * 0.5f + 0.5f;

        Color cor = Color.Lerp(COR_FRACO, COR_CHEIO, pct);

        // Anel externo (gira CW)
        if (lrExt != null)
        {
            rootVisual.transform.GetChild(0).localRotation = Quaternion.Euler(0f, 0f, angRot);
            lrExt.startColor = lrExt.endColor = new Color(cor.r, cor.g, cor.b, (0.6f + pulso * 0.35f) * (emRecarga ? 0.3f : 1f));
            lrExt.startWidth = lrExt.endWidth = emRecarga ? 0.04f : (0.09f + pulso * 0.05f);
        }

        // Anel interno (gira CCW mais rápido)
        if (lrInt != null)
        {
            rootVisual.transform.GetChild(1).localRotation = Quaternion.Euler(0f, 0f, -angRot * 1.6f);
            lrInt.startColor = lrInt.endColor = new Color(cor.r, cor.g, cor.b, (0.4f + pulso2 * 0.3f) * (emRecarga ? 0.2f : 1f));
            lrInt.startWidth = lrInt.endWidth = emRecarga ? 0.03f : (0.05f + pulso2 * 0.04f);
        }

        // Core pulsante
        if (lrCore != null)
        {
            float coreAlpha = emRecarga ? 0.1f : (0.3f + pulso * 0.4f) * pct;
            lrCore.startColor = lrCore.endColor = new Color(1f, 1f, 1f, coreAlpha);
            lrCore.startWidth = lrCore.endWidth = 0.03f + pulso * 0.04f;
        }

        // Glow
        if (srGlow != null)
            srGlow.color = new Color(cor.r, cor.g, cor.b, emRecarga ? 0.01f : (0.04f + pulso * 0.04f) * pct);
    }

    // ── Partículas orbitando ──────────────────────────────────────────────────

    IEnumerator ParticulasOrbitando()
    {
        const int QTD = 6;
        particulas = new GameObject[QTD];

        for (int i = 0; i < QTD; i++)
        {
            var go = new GameObject($"PartBarreira{i}");
            var sr2 = go.AddComponent<SpriteRenderer>();
            sr2.sprite = GerarDisco(6);
            sr2.sortingOrder = 10;
            go.transform.localScale = Vector3.one * Random.Range(0.08f, 0.16f);
            particulas[i] = go;
        }

        float ang = 0f;
        while (rootVisual != null && playerStats != null)
        {
            ang += Time.deltaTime * 90f;
            float pct = emRecarga ? 0f : Mathf.Clamp01(playerStats.shieldPoints / escudoMax);

            for (int i = 0; i < QTD; i++)
            {
                if (particulas[i] == null) continue;
                float a = (ang + 360f / QTD * i) * Mathf.Deg2Rad;
                float raio = 0.72f + Mathf.Sin(elapsed * 3f + i) * 0.1f;
                particulas[i].transform.position = (Vector2)playerStats.transform.position
                    + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * raio;

                Color cor = emRecarga ? COR_RECARGA : Color.Lerp(COR_FRACO, COR_CHEIO, pct);
                var sr2 = particulas[i].GetComponent<SpriteRenderer>();
                if (sr2 != null)
                    sr2.color = new Color(cor.r, cor.g, cor.b, emRecarga ? 0.1f : (0.5f + Mathf.Sin(elapsed * 5f + i * 1.5f) * 0.3f) * pct);
            }
            yield return null;
        }

        // Cleanup
        if (particulas != null)
            foreach (var p in particulas)
                if (p != null) Destroy(p);
    }

    // ── Efeito de quebra ──────────────────────────────────────────────────────

    IEnumerator EfeitoQuebrando()
    {
        // BarreiraExplosiva — dano em área ao quebrar
        if (SkillEvolutionManager.Tem(SkillEvolutionType.BarreiraExplosiva) && playerStats != null)
        {
            float danoExp = escudoMax * 0.5f;
            foreach (var col in Physics2D.OverlapCircleAll(playerStats.transform.position, 4f))
            {
                var ic = col.GetComponent<InimigoController>() ?? col.GetComponentInParent<InimigoController>();
                if (ic != null && !ic.estaMorrendo) ic.ReceberDano(danoExp, false);
            }
            EvolutionFX.SpawnExplosao(playerStats.transform.position, 4f, danoExp,
                COR_CHEIO, this);
        }

        CameraShaker.Tremer(0.15f, 0.3f);

        // Flash branco no player
        var sr = playerStats?.GetComponent<SpriteRenderer>();
        if (sr != null) { sr.color = Color.white; yield return new WaitForSeconds(0.05f); if (sr != null) sr.color = Color.white; }

        // Fragmentos explodindo
        for (int i = 0; i < 14; i++)
        {
            float ang = i / 14f * Mathf.PI * 2f;
            var go = new GameObject("FragBarreira");
            go.transform.position = (Vector2)playerStats.transform.position + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 0.8f;
            var fsr = go.AddComponent<SpriteRenderer>();
            fsr.sprite = GerarDisco(6); fsr.color = COR_CHEIO; fsr.sortingOrder = 13;
            go.transform.localScale = Vector3.one * Random.Range(0.1f, 0.22f);
            Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(3f, 7f);
            go.AddComponent<AutoDestroyFadeMove>().Iniciar(vel, 0.5f);
            Destroy(go, 0.8f);
        }

        // Anel expansivo de quebra
        StartCoroutine(AnelExpansivo(playerStats.transform.position, COR_CHEIO, 2.5f, 0.4f));

        // Flash na tela
        StartCoroutine(FlashTela(new Color(0.2f, 0.7f, 1f, 0.3f), 0.3f));
    }

    IEnumerator EfeitoRecuperando()
    {
        // Anel de recarga
        StartCoroutine(AnelExpansivo(playerStats.transform.position, COR_CHEIO, 2.0f, 0.5f));

        // Partículas convergindo
        for (int i = 0; i < 10; i++)
        {
            float ang = i / 10f * Mathf.PI * 2f;
            float dist = Random.Range(2f, 4f);
            Vector2 origem = (Vector2)playerStats.transform.position + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;
            var go = new GameObject("PartRecarga");
            go.transform.position = origem;
            var sr2 = go.AddComponent<SpriteRenderer>();
            sr2.sprite = GerarDisco(8); sr2.color = COR_CHEIO; sr2.sortingOrder = 12;
            go.transform.localScale = Vector3.one * Random.Range(0.1f, 0.2f);
            Vector2 vel = ((Vector2)playerStats.transform.position - origem).normalized * Random.Range(4f, 8f);
            go.AddComponent<AutoDestroyFadeMove>().Iniciar(vel, 0.4f);
            Destroy(go, 0.6f);
        }
        yield return null;
    }

    // ── Helpers visuais ───────────────────────────────────────────────────────

    IEnumerator AnelExpansivo(Vector2 pos, Color cor, float raioFinal, float dur)
    {
        const int S = 40;
        var go = new GameObject("AnelBarreira");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = S;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 11;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p = t / dur; float r = Mathf.Lerp(0.2f, raioFinal, p);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.25f, 0.02f, p);
            lr.startColor = lr.endColor = new Color(cor.r, cor.g, cor.b, Mathf.Lerp(1f, 0f, p));
            for (int i = 0; i < S; i++) { float a = 360f / S * i * Mathf.Deg2Rad; lr.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r); }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    IEnumerator FlashTela(Color cor, float dur)
    {
        var go = new GameObject("FlashBarreira"); DontDestroyOnLoad(go);
        var cv = go.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 200; go.AddComponent<CanvasScaler>();
        var imgGO = new GameObject("F"); imgGO.transform.SetParent(go.transform, false);
        var rt = imgGO.AddComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = imgGO.AddComponent<UnityEngine.UI.Image>(); img.color = new Color(cor.r, cor.g, cor.b, 0f); img.raycastTarget = false;
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            float p = t / dur;
            img.color = new Color(cor.r, cor.g, cor.b, p < 0.3f ? Mathf.Lerp(0f, cor.a, p / 0.3f) : Mathf.Lerp(cor.a, 0f, (p - 0.3f) / 0.7f));
            yield return null;
        }
        Destroy(go);
    }

    public override void ApplyEffect() { }

    static Sprite GerarDisco(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false); tex.filterMode = FilterMode.Bilinear; float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++) { float d = Vector2.Distance(new Vector2(x+0.5f,y+0.5f),new Vector2(cx,cx)); tex.SetPixel(x,y,new Color(1f,1f,1f,d<cx?1f:0f)); }
        tex.Apply(); return Sprite.Create(tex, new Rect(0,0,sz,sz), new Vector2(0.5f,0.5f), sz);
    }
}
