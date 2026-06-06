using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EscudoEspinhosoSkillBehavior : SkillBehavior, ISkillComRecarga
{
    public float dano    = 20f;
    public float cooldown = 3f;
    public int   maxHits  = 3;

    int   hitsRestantes;
    float timerRecarga;
    bool  ativo = true;

    // Guarda o frame em que cada inimigo foi atingido para evitar
    // que o mesmo inimigo seja processado mais de uma vez por ciclo de FixedUpdate.
    readonly Dictionary<int, int> ultimoFrameAcerto = new Dictionary<int, int>();

    public bool  EmRecarga    => timerRecarga > 0f;
    public float TimerRecarga => timerRecarga;
    public float RecargaTotal => cooldown;

    GameObject rootVisual;
    static readonly Color COR_ESPINHO = new Color(0.25f, 0.9f,  0.3f,  1f);
    static readonly Color COR_BRILHO  = new Color(0.65f, 1f,    0.65f, 1f);

    public override void Initialize(PlayerStats stats)
    {
        base.Initialize(stats);
        hitsRestantes = SkillEvolutionManager.Tem(SkillEvolutionType.KarmaReforcado) ? maxHits + 2 : maxHits;
        // Visual criado no Start() para garantir que o componente já está ativo
    }

    public void UpdateFromSkillData(SkillData skill)
    {
        if (skill.attackBonus > 0) dano    = skill.attackBonus;
        if (skill.cooldown    > 0) cooldown = skill.cooldown;
    }

    void Start()
    {
        // Garante que hitsRestantes foi inicializado (caso Initialize não tenha sido chamado antes)
        if (hitsRestantes == 0) hitsRestantes = SkillEvolutionManager.Tem(SkillEvolutionType.KarmaReforcado) ? maxHits + 2 : maxHits;
        CriarVisual();
    }

    void OnDestroy()
    {
        if (rootVisual != null) Destroy(rootVisual);
    }

    public override void ApplyEffect() { }
    public float GetDano() => dano;
    public void OnEnemyHit() => StartCoroutine(CooldownReativacao());

    IEnumerator CooldownReativacao()
    {
        ativo = false;
        timerRecarga = cooldown;
        AtualizarVisualAtivo(false);
        // Não chama Reativar() aqui — o Update() já monitora timerRecarga e
        // chamará Reativar() + EfeitoRecarga() quando chegar a zero, evitando
        // que ambos disparem ao mesmo tempo (double-call bug).
        yield return null;
    }

    void Update()
    {
        if (timerRecarga > 0f)
        {
            timerRecarga -= Time.deltaTime;
            if (timerRecarga <= 0f) { Reativar(); StartCoroutine(EfeitoRecarga()); }
        }

        if (rootVisual == null && playerStats != null)
            CriarVisual();
    }

    void LateUpdate()
    {
        if (rootVisual != null && playerStats != null)
            rootVisual.transform.position = playerStats.transform.position;
    }

    // ─── Detecção de contato com inimigos ────────────────────────────────────

    void FixedUpdate()
    {
        if (!ativo || playerStats == null) return;
        float raio = 0.75f;
        foreach (var col in Physics2D.OverlapCircleAll(playerStats.transform.position, raio))
        {
            var ic = col.GetComponent<InimigoController>() ?? col.GetComponentInParent<InimigoController>();
            if (ic == null || ic.estaMorrendo) continue;

            // Evita acertar o mesmo inimigo mais de uma vez por FixedUpdate-tick.
            int id = ic.GetInstanceID();
            if (ultimoFrameAcerto.TryGetValue(id, out int frameAnterior) && Time.frameCount == frameAnterior)
                continue;
            ultimoFrameAcerto[id] = Time.frameCount;

            ic.ReceberDano(dano, false);

            if (SkillEvolutionManager.Tem(SkillEvolutionType.EspinhosVenenosos2))
                EvolutionFX.AplicarVeneno(ic, 2f, 5f);

            hitsRestantes--;
            StartCoroutine(FlashEspinho(ic.transform.position));

            if (hitsRestantes <= 0)
            {
                if (SkillEvolutionManager.Tem(SkillEvolutionType.EspinhosExplosivos))
                    EvolutionFX.SpawnExplosao(playerStats.transform.position, 2.5f, dano * 1.5f, COR_ESPINHO, this);
                ativo = false;
                timerRecarga = cooldown;
                AtualizarVisualAtivo(false);
            }
            break;
        }
    }

    void Reativar()
    {
        hitsRestantes = SkillEvolutionManager.Tem(SkillEvolutionType.KarmaReforcado) ? maxHits + 2 : maxHits;
        ativo = true;
        AtualizarVisualAtivo(true);
    }

    // ─── Visual ──────────────────────────────────────────────────────────────

    void CriarVisual()
    {
        if (playerStats == null) return;
        if (rootVisual != null) Destroy(rootVisual); // limpa visual antigo se houver

        rootVisual = new GameObject("EscudoEspinhosoVisual");
        rootVisual.transform.position = playerStats.transform.position;

        // Anel base do escudo — raio 0.80 para ser claramente visível
        CriarAnel(48, 0.80f, 0.08f, COR_ESPINHO);

        // 8 espinhos em anel (mais do que 6 para visual mais denso)
        for (int i = 0; i < 8; i++)
        {
            float ang = i / 8f * Mathf.PI * 2f;
            CriarEspinho(ang);
        }

        StartCoroutine(AnimarVisual());
    }

    void CriarAnel(int segs, float raio, float larg, Color cor)
    {
        var go = new GameObject("Anel");
        go.transform.SetParent(rootVisual.transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false; lr.loop = true; lr.positionCount = segs;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 12;
        lr.startWidth = lr.endWidth = larg;
        lr.numCapVertices = 4;
        lr.startColor = lr.endColor = cor;
        for (int i = 0; i < segs; i++)
        {
            float a = i / (float)segs * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * raio, Mathf.Sin(a) * raio));
        }
    }

    void CriarEspinho(float angBase)
    {
        var go = new GameObject("Espinho");
        go.transform.SetParent(rootVisual.transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false; lr.positionCount = 2;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 13;
        lr.startWidth = 0.07f; lr.endWidth = 0.01f;
        lr.numCapVertices = 4;
        lr.startColor = COR_ESPINHO; lr.endColor = Color.white;
        lr.SetPosition(0, new Vector3(Mathf.Cos(angBase) * 0.72f, Mathf.Sin(angBase) * 0.72f));
        lr.SetPosition(1, new Vector3(Mathf.Cos(angBase) * 1.05f, Mathf.Sin(angBase) * 1.05f));
    }

    void AtualizarVisualAtivo(bool on)
    {
        if (rootVisual == null) return;
        foreach (var lr in rootVisual.GetComponentsInChildren<LineRenderer>())
        {
            Color c = lr.startColor; c.a = on ? 0.85f : 0.18f;
            lr.startColor = lr.endColor = c;
        }
    }

    IEnumerator AnimarVisual()
    {
        float t = 0f;
        while (rootVisual != null)
        {
            t += Time.deltaTime;
            float pulso = Mathf.Sin(t * 4f) * 0.5f + 0.5f;
            rootVisual.transform.rotation = Quaternion.Euler(0f, 0f, t * 30f);

            if (ativo)
            {
                float pct = maxHits > 0 ? Mathf.Clamp01((float)hitsRestantes / maxHits) : 1f;
                Color cor = Color.Lerp(new Color(1f, 0.4f, 0.1f), COR_ESPINHO, pct);
                foreach (var lr in rootVisual.GetComponentsInChildren<LineRenderer>())
                {
                    Color c = cor; c.a = 0.65f + pulso * 0.3f;
                    lr.startColor = lr.endColor = c;
                    lr.startWidth = lr.endWidth = 0.065f + pulso * 0.04f;
                }
            }
            // Quando inativo: AtualizarVisualAtivo(false) já definiu alpha baixo — apenas mantém
            yield return null;
        }
    }

    IEnumerator FlashEspinho(Vector2 pos)
    {
        for (int i = 0; i < 5; i++)
        {
            var go = new GameObject("FE");
            go.transform.position = pos + Random.insideUnitCircle * 0.3f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GerarDisco(6); sr.color = COR_BRILHO; sr.sortingOrder = 15;
            go.transform.localScale = Vector3.one * Random.Range(0.08f, 0.18f);
            go.AddComponent<AutoDestroyFadeMove>().Iniciar(Random.insideUnitCircle * 2f, 0.25f);
            Destroy(go, 0.4f);
        }
        yield return null;
    }

    IEnumerator EfeitoRecarga()
    {
        if (playerStats == null) yield break;
        // Anel expansivo verde de recarga
        const int S = 40; var go = new GameObject("RecargaAnel");
        go.transform.position = playerStats.transform.position;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = S;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 13;
        Destroy(go, 0.6f);
        for (float t2 = 0f; t2 < 0.5f; t2 += Time.deltaTime)
        {
            if (go == null) yield break;
            float prog = t2 / 0.5f;
            float r = Mathf.Lerp(0.1f, 1.4f, prog);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.22f, 0.01f, prog);
            lr.startColor = lr.endColor = new Color(COR_ESPINHO.r, COR_ESPINHO.g, COR_ESPINHO.b, Mathf.Lerp(0.9f, 0f, prog));
            for (int i = 0; i < S; i++) { float a = i / (float)S * Mathf.PI * 2f; lr.SetPosition(i, (Vector2)playerStats.transform.position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r); }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    static Sprite GerarDisco(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false); tex.filterMode = FilterMode.Bilinear; float c = sz * 0.5f;
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++) { float d = Vector2.Distance(new Vector2(x+0.5f,y+0.5f), new Vector2(c,c)); tex.SetPixel(x,y,new Color(1,1,1,d<c?1:0)); }
        tex.Apply(); return Sprite.Create(tex, new Rect(0,0,sz,sz), new Vector2(0.5f,0.5f), sz);
    }

    public override void RemoveEffect() { if (rootVisual != null) Destroy(rootVisual); }
}
