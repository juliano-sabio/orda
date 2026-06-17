using System.Collections;
using UnityEngine;

public class EscudoKarmaSkillBehavior : SkillBehavior, ISkillComRecarga
{
    float recarga    = 180f;
    int   maxHits    = 3;
    int   hitsRestantes;

    float timerRecarga = 0f;
    bool  ativo        = false;

    GameObject[] orbs;

    public bool  EmRecarga    => timerRecarga > 0f;
    public float TimerRecarga => timerRecarga;
    public float RecargaTotal => recarga;

    public override void Initialize(PlayerStats stats) => base.Initialize(stats);

    static readonly Color COR_ORIG = new Color(1f, 0.85f, 0.2f);
    Color CorElemento() {
        if (skillData != null && skillData.appliedElement != ElementType.None)
            return ElementRegistry.Instance?.GetCor(skillData.appliedElement) ?? COR_ORIG;
        return COR_ORIG;
    }

    public void ConfigurarDeSkillData(SkillData data)
    {
        this.skillData = data;
        if (data.cooldown > 0f)        recarga  = data.cooldown;
        if (data.projectileCount > 0)  maxHits  = data.projectileCount;
    }

    public override void ApplyEffect() { }

    void Start()
    {
        Ativar();
        CriarOrbs();
    }

    void OnDestroy()
    {
        LimparOrbs();
    }

    void Ativar()
    {
        // KarmaReforcado — 5 hits em vez de 3
        hitsRestantes = SkillEvolutionManager.Tem(SkillEvolutionType.KarmaReforcado)
            ? Mathf.Max(maxHits, 5) : maxHits;
        ativo = true;
        AtualizarOrbs();
        SkillElementEffect.AplicarDefensivo(skillData, playerStats, DefensiveTrigger.OnAtivar, null, this);
    }

    void Update()
    {
        if (timerRecarga > 0f)
        {
            timerRecarga -= Time.deltaTime;
            if (timerRecarga <= 0f) { Ativar(); StartCoroutine(EfeitoRecarga()); }
        }
    }

    void LateUpdate()
    {
        if (playerStats != null && orbs != null) AtualizarPosOrbs();
    }

    // Chamado por PlayerStats.TakeDamage para absorver hit
    public bool AbsorverHit(float danoAbsorvido = 0f)
    {
        if (!ativo || hitsRestantes <= 0) return false;

        hitsRestantes--;
        StartCoroutine(EfeitoAbsorcao());
        AtualizarOrbs();

        // KarmaRetribuicao — causa dobro do dano absorvido no atacante mais próximo
        if (SkillEvolutionManager.Tem(SkillEvolutionType.KarmaRetribuicao) && danoAbsorvido > 0f && playerStats != null)
        {
            var inimigos = FindObjectsByType<InimigoController>(FindObjectsSortMode.None);
            InimigoController alvo = null; float menorDist = float.MaxValue;
            Vector2 pos = playerStats.transform.position;
            foreach (var ic in inimigos)
            {
                if (ic == null || ic.estaMorrendo) continue;
                float d = Vector2.Distance(ic.transform.position, pos);
                if (d < menorDist) { menorDist = d; alvo = ic; }
            }
            if (alvo != null) { alvo.ReceberDano(danoAbsorvido * 2f, false); SkillElementEffect.Aplicar(skillData, alvo.gameObject, danoAbsorvido * 2f, this); }
        }

        if (hitsRestantes <= 0)
        {
            ativo = false; // shield quebrou — bloqueia novas absorcoes até recarregar
            timerRecarga = recarga;
            StartCoroutine(EfeitoQuebraTudo());
        }
        return true;
    }

    void CriarOrbs()
    {
        LimparOrbs();
        orbs = new GameObject[maxHits];
        Color ce = CorElemento();
        for (int i = 0; i < maxHits; i++)
        {
            var go = new GameObject($"KarmaOrb{i}");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GerarDisco(12); sr.color = ce; sr.sortingOrder = 14;
            go.transform.localScale = Vector3.one * 0.35f;
            orbs[i] = go;
        }
    }

    void AtualizarOrbs()
    {
        if (orbs == null) return;
        for (int i = 0; i < orbs.Length; i++)
        {
            if (orbs[i] == null) continue;
            bool visivel = i < hitsRestantes;
            orbs[i].SetActive(visivel);
        }
    }

    void AtualizarPosOrbs()
    {
        if (orbs == null || playerStats == null) return;
        float ang = Time.time * 120f;
        int ativos = 0;
        foreach (var o in orbs) if (o != null && o.activeSelf) ativos++;
        if (ativos == 0) return;

        int idx = 0;
        for (int i = 0; i < orbs.Length; i++)
        {
            if (orbs[i] == null || !orbs[i].activeSelf) continue;
            float a = (ang + 360f / ativos * idx) * Mathf.Deg2Rad;
            float pulso = 0.9f + Mathf.Sin(Time.time * 4f + i) * 0.1f;
            orbs[i].transform.position = (Vector2)playerStats.transform.position
                + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * pulso;
            idx++;
        }
    }

    void LimparOrbs()
    {
        if (orbs == null) return;
        foreach (var o in orbs) if (o != null) Destroy(o);
        orbs = null;
    }

    IEnumerator EfeitoAbsorcao()
    {
        // Flash dourado no player
        var sr = playerStats?.GetComponent<SpriteRenderer>();
        if (sr != null) { sr.color = new Color(1f, 0.85f, 0.2f); yield return new WaitForSecondsRealtime(0.1f); if (sr != null) sr.color = Color.white; }

        // Burst na orb destruída
        for (int i = 0; i < 6; i++)
        {
            var p = new GameObject("BurstKarma");
            p.transform.position = playerStats != null ? playerStats.transform.position : Vector3.zero;
            var s2 = p.AddComponent<SpriteRenderer>();
            s2.sprite = GerarDisco(6); s2.color = new Color(1f, 0.85f, 0.2f); s2.sortingOrder = 15;
            p.transform.localScale = Vector3.one * Random.Range(0.1f, 0.22f);
            p.AddComponent<AutoDestroyFadeMove>().Iniciar(Random.insideUnitCircle.normalized * Random.Range(2f, 5f), 0.3f);
            Destroy(p, 0.5f);
        }
    }

    IEnumerator EfeitoQuebraTudo()
    {
        // Anel de quebra
        if (playerStats == null) yield break;
        const int S = 32;
        var go = new GameObject("KarmaBreak");
        go.transform.position = playerStats.transform.position;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = S;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 13;
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p = t / 0.4f; float r = Mathf.Lerp(0.3f, 2.5f, p);
            Color ce = CorElemento();
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.25f, 0.02f, p);
            lr.startColor = lr.endColor = new Color(ce.r, ce.g, ce.b, Mathf.Lerp(1f, 0f, p));
            for (int i = 0; i < S; i++) { float a = 360f/S*i*Mathf.Deg2Rad; lr.SetPosition(i, (Vector2)playerStats.transform.position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r); }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    IEnumerator EfeitoRecarga()
    {
        CriarOrbs();
        AtualizarOrbs();

        // Orbs aparecem com animação
        for (int i = 0; i < maxHits; i++)
        {
            if (orbs != null && orbs[i] != null)
            {
                orbs[i].transform.localScale = Vector3.zero;
                StartCoroutine(PopOrb(orbs[i]));
                yield return new WaitForSeconds(0.15f);
            }
        }
    }

    IEnumerator PopOrb(GameObject go)
    {
        float dur = 0.25f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p = t / dur;
            float ease = 1f - Mathf.Pow(1f - p, 3f);
            go.transform.localScale = Vector3.one * (0.35f * ease * 1.2f);
            yield return null;
        }
        if (go != null) go.transform.localScale = Vector3.one * 0.35f;
    }

    static Sprite GerarDisco(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false); tex.filterMode = FilterMode.Bilinear; float cx = sz*0.5f;
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++) { float d = Vector2.Distance(new Vector2(x+0.5f,y+0.5f),new Vector2(cx,cx)); tex.SetPixel(x,y,new Color(1f,1f,1f,d<cx?1f:0f)); }
        tex.Apply(); return Sprite.Create(tex, new Rect(0,0,sz,sz), new Vector2(0.5f,0.5f), sz);
    }
}
