using System.Collections;
using UnityEngine;

public class AureolaSkillBehavior : SkillBehavior, ISkillComRecarga
{
    [Header("Configurações")]
    public float recarga         = 180f;
    public float duracao         = 6f;
    public float reducaoDano     = 0.30f;  // 30% menos dano durante ativação
    public float regenPorSegundo = 3f;     // HP regenerado por segundo
    public float raioAura        = 2.5f;
    public GameObject prefabAureola;

    float timerRecarga = 0f;
    bool  ativo        = false;
    float regenAcum    = 0f;

    public bool  EmRecarga    => timerRecarga > 0f;
    public float TimerRecarga => timerRecarga;
    public float RecargaTotal => recarga;

    // ── Visuais persistentes ─────────────────────────────────────────────────
    GameObject   rootVisual;
    LineRenderer lrHaloExt, lrHaloInt, lrHaloExtra1, lrHaloExtra2;
    SpriteRenderer srGlow;
    float        angRot;
    float        elapsed;
    static readonly Color COR_DOURADO   = new Color(1f, 0.88f, 0.22f, 1f);
    static readonly Color COR_RECARGA   = new Color(1f, 0.88f, 0.22f, 0.15f);

    public override void Initialize(PlayerStats stats) => base.Initialize(stats);

    static readonly Color COR_ORIG = new Color(1f, 0.88f, 0.22f);
    Color CorElemento() {
        if (skillData != null && skillData.appliedElement != ElementType.None)
            return ElementRegistry.Instance?.GetCor(skillData.appliedElement) ?? COR_ORIG;
        return COR_ORIG;
    }

    public void ConfigurarDeSkillData(SkillData data)
    {
        this.skillData = data;
        if (data.cooldown > 0f)           recarga         = data.cooldown;
        if (data.activationInterval > 0f) duracao         = data.activationInterval;
        if (data.specialValue > 0f)       reducaoDano     = data.specialValue / 100f;
        if (data.healthRegenBonus > 0f)   regenPorSegundo = data.healthRegenBonus;
    }

    public override void ApplyEffect() => Ativar();

    void OnEnable()  => PlayerStats.OnDanoRecebido += OnDano;
    void OnDisable() => PlayerStats.OnDanoRecebido -= OnDano;

    void OnDano()
    {
        if (timerRecarga <= 0f && !ativo) Ativar();
    }

    System.Collections.IEnumerator Start()
    {
        yield return null; // aguarda Initialize() do SkillManager
        if (playerStats == null) playerStats = GetComponent<PlayerStats>();
        CriarVisualPersistente();
        StartCoroutine(ParticulasDouradas());
    }

    void OnDestroy()
    {
        if (rootVisual != null) Destroy(rootVisual);
    }

    void Update()
    {
        if (timerRecarga > 0f) timerRecarga -= Time.deltaTime;
        elapsed += Time.deltaTime;
        angRot  += Time.deltaTime * 25f;

        // Cria visual se ainda não existe (fallback para timing issues)
        if (rootVisual == null && playerStats != null)
        {
            CriarVisualPersistente();
            StartCoroutine(ParticulasDouradas());
        }

        AtualizarVisual();

        // Regen contínuo enquanto ativo
        if (ativo && playerStats != null)
        {
            float regenReal = regenPorSegundo *
                (SkillEvolutionManager.Tem(SkillEvolutionType.AureolaFortificada) ? 1.8f : 1f);
            regenAcum += regenReal * Time.deltaTime;
            if (regenAcum >= 1f)
            {
                playerStats.Heal(Mathf.Floor(regenAcum));
                regenAcum -= Mathf.Floor(regenAcum);
            }
        }
    }

    // Chamado pelo PlayerStats.TakeDamage para reduzir dano
    public float AplicarReducao(float dano)
    {
        if (!ativo) return dano;
        float mult = SkillEvolutionManager.Tem(SkillEvolutionType.AureolaFortificada) ? 0.50f : reducaoDano;
        return dano * (1f - mult);
    }

    void Ativar()
    {
        if (ativo) return;
        timerRecarga = recarga;
        StartCoroutine(CorotinaAtiva());
    }

    IEnumerator CorotinaAtiva()
    {
        ativo = true;
        regenAcum = 0f;

        // Anéis extras aparecem quando ativo
        if (lrHaloExtra1 != null) lrHaloExtra1.gameObject.SetActive(true);
        if (lrHaloExtra2 != null) lrHaloExtra2.gameObject.SetActive(true);

        yield return new WaitForSeconds(duracao);

        ativo = false;
        if (lrHaloExtra1 != null) lrHaloExtra1.gameObject.SetActive(false);
        if (lrHaloExtra2 != null) lrHaloExtra2.gameObject.SetActive(false);
    }

    // ── Criação visual persistente ────────────────────────────────────────────

    void CriarVisualPersistente()
    {
        if (playerStats == null) return;

        float raioReal = SkillEvolutionManager.Tem(SkillEvolutionType.AureolaExpansiva)
            ? raioAura * 1.6f : raioAura;

        rootVisual = new GameObject("AureolaVisual");
        // Não parentar ao player: a escala do player (ex: 3,3,1) distorceria
        // os LineRenderers em useWorldSpace=false. A posição é sincronizada em
        // AtualizarVisual() a cada frame.
        rootVisual.transform.position = playerStats.transform.position;

        // Halo externo — anel dourado fino girando devagar
        var goExt = new GameObject("HaloExt");
        goExt.transform.SetParent(rootVisual.transform, false);
        lrHaloExt = CriarAnel(goExt, 48, raioReal, 0.07f, 12);
        lrHaloExt.startColor = lrHaloExt.endColor = COR_DOURADO;

        // Halo interno — girando no sentido oposto
        var goInt = new GameObject("HaloInt");
        goInt.transform.SetParent(rootVisual.transform, false);
        lrHaloInt = CriarAnel(goInt, 32, raioReal * 0.6f, 0.04f, 11);
        lrHaloInt.startColor = lrHaloInt.endColor = new Color(COR_DOURADO.r, COR_DOURADO.g, COR_DOURADO.b, 0.7f);

        // Anéis extras (só visíveis quando ativo)
        var goEx1 = new GameObject("HaloExtra1");
        goEx1.transform.SetParent(rootVisual.transform, false);
        lrHaloExtra1 = CriarAnel(goEx1, 48, raioReal * 1.15f, 0.05f, 13);
        lrHaloExtra1.startColor = lrHaloExtra1.endColor = new Color(1f, 0.95f, 0.5f, 0.9f);
        goEx1.SetActive(false);

        var goEx2 = new GameObject("HaloExtra2");
        goEx2.transform.SetParent(rootVisual.transform, false);
        lrHaloExtra2 = CriarAnel(goEx2, 24, raioReal * 0.35f, 0.04f, 10);
        lrHaloExtra2.startColor = lrHaloExtra2.endColor = new Color(1f, 1f, 0.8f, 0.85f);
        goEx2.SetActive(false);

        // Glow disc
        var goGlow = new GameObject("Glow");
        goGlow.transform.SetParent(rootVisual.transform, false);
        srGlow = goGlow.AddComponent<SpriteRenderer>();
        srGlow.sprite = GerarDisco(64);
        srGlow.color = new Color(COR_DOURADO.r, COR_DOURADO.g, COR_DOURADO.b, 0.04f);
        srGlow.sortingOrder = 9;
        goGlow.transform.localScale = Vector3.one * (raioReal * 2f);
    }

    LineRenderer CriarAnel(GameObject go, int segs, float raio, float larg, int order)
    {
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false; lr.loop = true; lr.positionCount = segs;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = order; lr.startWidth = lr.endWidth = larg;
        lr.numCapVertices = 4;
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

        float pulso  = Mathf.Sin(elapsed * 3f) * 0.5f + 0.5f;
        float pulso2 = Mathf.Sin(elapsed * 5f + 1.2f) * 0.5f + 0.5f;

        bool emRecarga = timerRecarga > 0f && !ativo;

        // Halo externo gira no sentido horário devagar
        rootVisual.transform.GetChild(0).localRotation = Quaternion.Euler(0f, 0f, angRot);
        if (lrHaloExt != null)
        {
            float alpha = emRecarga ? (0.35f + pulso * 0.1f) : (ativo ? (0.7f + pulso * 0.3f) : (0.35f + pulso * 0.2f));
            lrHaloExt.startColor = lrHaloExt.endColor = new Color(COR_DOURADO.r, COR_DOURADO.g, COR_DOURADO.b, alpha);
            lrHaloExt.startWidth = lrHaloExt.endWidth = emRecarga ? 0.05f : (0.06f + pulso * 0.04f);
        }

        // Halo interno gira no sentido anti-horário
        rootVisual.transform.GetChild(1).localRotation = Quaternion.Euler(0f, 0f, -angRot * 1.4f);
        if (lrHaloInt != null)
        {
            float alpha = emRecarga ? (0.22f + pulso2 * 0.08f) : (ativo ? (0.55f + pulso2 * 0.3f) : (0.22f + pulso2 * 0.15f));
            lrHaloInt.startColor = lrHaloInt.endColor = new Color(COR_DOURADO.r, COR_DOURADO.g, COR_DOURADO.b, alpha);
            lrHaloInt.startWidth = lrHaloInt.endWidth = emRecarga ? 0.035f : (0.035f + pulso2 * 0.025f);
        }

        // Anéis extras (ativo)
        if (lrHaloExtra1 != null && lrHaloExtra1.gameObject.activeSelf)
        {
            rootVisual.transform.GetChild(2).localRotation = Quaternion.Euler(0f, 0f, angRot * 0.7f);
            lrHaloExtra1.startColor = lrHaloExtra1.endColor = new Color(1f, 0.95f, 0.5f, 0.6f + pulso * 0.35f);
            lrHaloExtra1.startWidth = lrHaloExtra1.endWidth = 0.05f + pulso * 0.04f;
        }
        if (lrHaloExtra2 != null && lrHaloExtra2.gameObject.activeSelf)
        {
            rootVisual.transform.GetChild(3).localRotation = Quaternion.Euler(0f, 0f, -angRot * 2f);
            lrHaloExtra2.startColor = lrHaloExtra2.endColor = new Color(1f, 1f, 0.8f, 0.5f + pulso2 * 0.4f);
            lrHaloExtra2.startWidth = lrHaloExtra2.endWidth = 0.04f + pulso2 * 0.03f;
        }

        // Glow
        if (srGlow != null)
        {
            float glowAlpha = emRecarga ? 0.025f : (ativo ? (0.07f + pulso * 0.05f) : 0.025f + pulso * 0.015f);
            srGlow.color = new Color(COR_DOURADO.r, COR_DOURADO.g, COR_DOURADO.b, glowAlpha);
        }
    }

    // ── Partículas douradas subindo ocasionalmente ────────────────────────────

    IEnumerator ParticulasDouradas()
    {
        float raioReal = SkillEvolutionManager.Tem(SkillEvolutionType.AureolaExpansiva)
            ? raioAura * 1.6f : raioAura;

        while (rootVisual != null && playerStats != null)
        {
            if (Time.timeScale == 0f) { yield return null; continue; }

            // Partículas mais frequentes quando ativo, ocasionais no idle
            int frameInterval = ativo ? 8 : 20;
            if (Time.frameCount % frameInterval == 0)
            {
                float ang = Random.Range(0f, Mathf.PI * 2f);
                float raioP = ativo
                    ? raioReal * Random.Range(0.2f, 1.1f)
                    : raioReal * Random.Range(0.4f, 1.0f);

                var p = new GameObject("PAureola");
                p.transform.position = (Vector2)playerStats.transform.position
                    + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raioP;
                var psr = p.AddComponent<SpriteRenderer>();
                psr.sprite = GerarDisco(6);
                float pAlpha = ativo ? Random.Range(0.6f, 0.85f) : Random.Range(0.2f, 0.4f);
                psr.color = new Color(COR_DOURADO.r, COR_DOURADO.g, COR_DOURADO.b, pAlpha);
                psr.sortingOrder = 14;
                p.transform.localScale = Vector3.one * Random.Range(0.05f, ativo ? 0.14f : 0.09f);
                p.AddComponent<AutoDestroyFadeMove>().Iniciar(Vector2.up * Random.Range(0.5f, ativo ? 2.5f : 1.2f), 0.5f);
                Destroy(p, 0.9f);
            }
            yield return null;
        }
    }

    static Sprite GerarDisco(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear; float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++)
        { float d = Vector2.Distance(new Vector2(x+0.5f,y+0.5f),new Vector2(cx,cx)); tex.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(1f-d/cx))); }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,sz,sz), new Vector2(0.5f,0.5f), sz);
    }
}
