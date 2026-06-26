using System.Collections;
using UnityEngine;

public class BarreiraReflexivaSkillBehavior : SkillBehavior, ISkillComRecarga
{
    [Header("Configurações")]
    public float recarga       = 60f;
    public float duracao       = 4f;
    public float raio          = 3f;
    public float danoReflexao  = 0.8f;   // % do dano recebido que é refletido
    public float forcaEmpurrao = 6f;
    public GameObject prefabBarreira;

    float timerRecarga = 0f;
    bool  ativo        = false;

    public bool  EmRecarga    => timerRecarga > 0f;
    public float TimerRecarga => timerRecarga;
    public float RecargaTotal => recarga;

    // ── Visuais persistentes ─────────────────────────────────────────────────
    GameObject   rootVisual;
    LineRenderer lrHexExt, lrHexInt;
    SpriteRenderer srGlow;
    float        angRot;
    float        elapsed;
    Color COR_CIANO   = new Color(0.35f, 0.88f, 1f, 1f); // reflete elemento infundido (atualizado em AtualizarVisual)
    static readonly Color COR_RECARGA = new Color(0.35f, 0.88f, 1f, 0.15f);

    public override void Initialize(PlayerStats stats) => base.Initialize(stats);

    static readonly Color COR_ORIG = new Color(0.35f, 0.88f, 1f);
    Color CorElemento() {
        if (skillData != null && skillData.appliedElement != ElementType.None)
            return ElementRegistry.Instance?.GetCor(skillData.appliedElement) ?? COR_ORIG;
        return COR_ORIG;
    }

    public void ConfigurarDeSkillData(SkillData data)
    {
        this.skillData = data;
        if (data.cooldown > 0f)           recarga       = data.cooldown;
        if (data.activationInterval > 0f) duracao       = data.activationInterval;
        if (data.specialValue > 0f)       danoReflexao  = data.specialValue / 100f;
        if (data.attackBonus > 0f)        forcaEmpurrao = data.attackBonus;
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
    }

    void OnDestroy()
    {
        if (rootVisual != null) Destroy(rootVisual);
    }

    void Update()
    {
        if (timerRecarga > 0f) timerRecarga -= Time.deltaTime;
        elapsed += Time.deltaTime;
        angRot  += Time.deltaTime * 15f;

        // Cria visual se ainda não existe
        if (rootVisual == null && playerStats != null)
            CriarVisualPersistente();

        AtualizarVisual();

        if (ativo && playerStats != null) EmpurrarERefletir();
        if (ativo && playerStats != null && !cosmetico) RefletirProjeteis();
        if (ativo && playerStats != null && Time.frameCount % 30 == 0)
            SkillElementEffect.AplicarDefensivo(skillData, playerStats, DefensiveTrigger.AuraContinua, null, this);
    }

    // Chamado por PlayerStats.TakeDamage — busca o atacante mais próximo internamente
    public float AplicarReflexao(float dano)
    {
        if (!ativo) return dano;

        InimigoController atacante = null;
        if (playerStats != null)
        {
            float menorDist = float.MaxValue;
            Vector2 pos = playerStats.transform.position;
            foreach (var ic in FindObjectsByType<InimigoController>(FindObjectsSortMode.None))
            {
                if (ic == null || ic.estaMorrendo) continue;
                float d = Vector2.Distance(ic.transform.position, pos);
                if (d < menorDist) { menorDist = d; atacante = ic; }
            }
        }

        float mult    = SkillEvolutionManager.Tem(SkillEvolutionType.BarreiraTotal) ? 1.0f : danoReflexao;
        float danoRef = dano * mult;

        if (atacante != null)
        {
            if (!cosmetico) // co-op: cópia cosmética não reflete dano (só o visual)
            {
                atacante.ReceberDano(danoRef, false);
                SkillElementEffect.Aplicar(skillData, atacante.gameObject, danoRef, this);
                SkillElementEffect.AplicarDefensivo(skillData, playerStats, DefensiveTrigger.OnAtingido, atacante.gameObject, this);
                if (SkillEvolutionManager.Tem(SkillEvolutionType.BarreiraCongelante))
                    EvolutionFX.AplicarLentidao(atacante, 2f, 0.3f);
            }
            StartCoroutine(FlashReflexao(atacante.transform.position));
        }

        // BarreiraTotal anula o dano recebido
        return SkillEvolutionManager.Tem(SkillEvolutionType.BarreiraTotal) ? 0f : dano;
    }

    void EmpurrarERefletir()
    {
        var inimigos = FindObjectsByType<InimigoController>(FindObjectsSortMode.None);
        Vector2 centro = playerStats.transform.position;
        foreach (var ic in inimigos)
        {
            if (ic == null || ic.estaMorrendo) continue;
            float dist = Vector2.Distance(ic.transform.position, centro);
            if (dist > raio) continue;

            var rb = ic.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 dir = ((Vector2)ic.transform.position - centro).normalized;
                rb.AddForce(dir * forcaEmpurrao * (1f - dist / raio) * Time.deltaTime * 40f, ForceMode2D.Force);
            }

            // Dano de contato com a barreira (gateado em co-op)
            if (!cosmetico) ic.ReceberDano(5f * Time.deltaTime, false, false);

            // BarreiraCongelante — lentidão ao tocar
            if (SkillEvolutionManager.Tem(SkillEvolutionType.BarreiraCongelante) && Random.value < 0.02f)
                EvolutionFX.AplicarLentidao(ic, 1.5f, 0.4f);
        }
    }

    // Reflete projéteis inimigos que entram na barreira: inverte a direção e,
    // quando possível, faz o projétil passar a ferir os inimigos.
    void RefletirProjeteis()
    {
        Vector2 centro = playerStats.transform.position;
        var hits = Physics2D.OverlapCircleAll(centro, raio);
        foreach (var col in hits)
        {
            // ProjetilInimigoDano: tem o flag 'redirecionado' → volta ferindo inimigos.
            var pid = col.GetComponent<ProjetilInimigoDano>() ?? col.GetComponentInParent<ProjetilInimigoDano>();
            if (pid != null)
            {
                if (pid.redirecionado) continue; // já refletido
                var rbp = pid.GetComponent<Rigidbody2D>();
                if (rbp != null) rbp.linearVelocity = -rbp.linearVelocity;
                pid.redirecionado = true;
                StartCoroutine(FlashReflexao(pid.transform.position));
                continue;
            }

            // projetil_inimigo (lentidão/vinhas): não fere inimigos, mas é mandado de
            // volta — vira inofensivo ao player. Só reflete se ainda vier na direção dele.
            var pi = col.GetComponent<projetil_inimigo>() ?? col.GetComponentInParent<projetil_inimigo>();
            if (pi != null)
            {
                var rbp = pi.GetComponent<Rigidbody2D>();
                if (rbp == null || rbp.linearVelocity.sqrMagnitude < 0.01f) continue;
                Vector2 paraFora = (Vector2)pi.transform.position - centro;
                if (Vector2.Dot(rbp.linearVelocity, paraFora) < 0f) // vindo na direção do player
                {
                    rbp.linearVelocity = -rbp.linearVelocity;
                    pi.SetDirecao(rbp.linearVelocity.normalized);
                    StartCoroutine(FlashReflexao(pi.transform.position));
                }
            }
        }
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
        SkillElementEffect.AplicarDefensivo(skillData, playerStats, DefensiveTrigger.OnAtivar, null, this);
        yield return new WaitForSeconds(duracao);
        ativo = false;
    }

    // ── Criação visual persistente ────────────────────────────────────────────

    void CriarVisualPersistente()
    {
        if (playerStats == null) return;
        if (rootVisual != null) return; // já criado — evita anel duplicado por dupla criação

        rootVisual = new GameObject("BarreiraReflexivaVisual");
        // Parentar ao player e neutralizar a escala não-uniforme (ex: 3,3,1)
        // com localScale inverso — assim os LineRenderers useWorldSpace=false
        // recebem escala (1,1,1) no mundo e não ficam distorcidos nem atrasados.
        rootVisual.transform.SetParent(playerStats.transform, false);
        rootVisual.transform.localPosition = Vector3.zero;
        var ws = playerStats.transform.lossyScale;
        rootVisual.transform.localScale = new Vector3(
            Mathf.Abs(ws.x) > 0.001f ? 1f / ws.x : 1f,
            Mathf.Abs(ws.y) > 0.001f ? 1f / ws.y : 1f,
            1f
        );

        // Hexágono externo
        var goExt = new GameObject("HexExt");
        goExt.transform.SetParent(rootVisual.transform, false);
        lrHexExt = CriarHexagono(goExt, raio, 0.10f, 12);
        lrHexExt.startColor = lrHexExt.endColor = COR_CIANO;

        // Hexágono interno (gira no sentido oposto)
        var goInt = new GameObject("HexInt");
        goInt.transform.SetParent(rootVisual.transform, false);
        lrHexInt = CriarHexagono(goInt, raio * 0.65f, 0.06f, 11);
        lrHexInt.startColor = lrHexInt.endColor = new Color(COR_CIANO.r, COR_CIANO.g, COR_CIANO.b, 0.7f);

        // Glow disc
        var goGlow = new GameObject("Glow");
        goGlow.transform.SetParent(rootVisual.transform, false);
        srGlow = goGlow.AddComponent<SpriteRenderer>();
        srGlow.sprite = GerarDisco(64);
        srGlow.color = new Color(COR_CIANO.r, COR_CIANO.g, COR_CIANO.b, 0.04f);
        srGlow.sortingOrder = 9;
        goGlow.transform.localScale = Vector3.one * (raio * 2f);

        StartCoroutine(FaiscasNasBordas()); // inicia uma única vez, junto com o visual
    }

    LineRenderer CriarHexagono(GameObject go, float r, float larg, int order)
    {
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false; lr.loop = true; lr.positionCount = 6;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = order; lr.startWidth = lr.endWidth = larg;
        lr.numCapVertices = 4;
        for (int i = 0; i < 6; i++)
        {
            float a = (i / 6f + 0.0833f) * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f));
        }
        return lr;
    }

    void AtualizarVisual()
    {
        if (rootVisual == null || playerStats == null) return;

        bool emRecarga = timerRecarga > 0f && !ativo;

        // Em recarga: barreira some completamente do player; volta quando recarregar.
        rootVisual.SetActive(!emRecarga);
        if (emRecarga) return;

        COR_CIANO = CorElemento(); // reflete elemento infundido em tempo real
        float pulso  = Mathf.Sin(elapsed * 4f) * 0.5f + 0.5f;
        float pulso2 = Mathf.Sin(elapsed * 6f + 0.8f) * 0.5f + 0.5f;

        // Hexágono externo gira no sentido anti-horário
        rootVisual.transform.GetChild(0).localRotation = Quaternion.Euler(0f, 0f, -angRot);
        if (lrHexExt != null)
        {
            float alpha = emRecarga ? 0.12f : (ativo ? (0.7f + pulso * 0.3f) : (0.3f + pulso * 0.15f));
            lrHexExt.startColor = lrHexExt.endColor = new Color(COR_CIANO.r, COR_CIANO.g, COR_CIANO.b, alpha);
            lrHexExt.startWidth = lrHexExt.endWidth = emRecarga ? 0.04f : (ativo ? (0.09f + pulso * 0.05f) : (0.06f + pulso * 0.03f));
        }

        // Hexágono interno gira no sentido horário mais rápido
        rootVisual.transform.GetChild(1).localRotation = Quaternion.Euler(0f, 0f, angRot * 1.7f);
        if (lrHexInt != null)
        {
            float alpha = emRecarga ? 0.08f : (ativo ? (0.55f + pulso2 * 0.35f) : (0.2f + pulso2 * 0.12f));
            lrHexInt.startColor = lrHexInt.endColor = new Color(COR_CIANO.r, COR_CIANO.g, COR_CIANO.b, alpha);
            lrHexInt.startWidth = lrHexInt.endWidth = emRecarga ? 0.025f : (ativo ? (0.06f + pulso2 * 0.04f) : (0.04f + pulso2 * 0.02f));
        }

        // Glow
        if (srGlow != null)
        {
            float glowAlpha = emRecarga ? 0.01f : (ativo ? (0.08f + pulso * 0.05f) : 0.02f + pulso * 0.015f);
            srGlow.color = new Color(COR_CIANO.r, COR_CIANO.g, COR_CIANO.b, glowAlpha);
        }
    }

    // ── Faíscas ocasionais nas arestas ───────────────────────────────────────

    IEnumerator FaiscasNasBordas()
    {
        while (rootVisual != null && playerStats != null)
        {
            // Em recarga a barreira some — sem faíscas também.
            if (timerRecarga > 0f && !ativo) { yield return null; continue; }

            // Faíscas mais frequentes quando ativo
            int frameInterval = ativo ? 6 : 18;
            if (Time.frameCount % frameInterval == 0)
            {
                // Posição numa aresta do hexágono externo
                int vertIdx = Random.Range(0, 6);
                float angA = (vertIdx / 6f + 0.0833f) * Mathf.PI * 2f;
                float angB = ((vertIdx + 1) / 6f + 0.0833f) * Mathf.PI * 2f;
                float t = Random.value;
                Vector2 pA = new Vector2(Mathf.Cos(angA), Mathf.Sin(angA)) * raio;
                Vector2 pB = new Vector2(Mathf.Cos(angB), Mathf.Sin(angB)) * raio;
                Vector2 pAresta = Vector2.Lerp(pA, pB, t);

                // Rota o ponto com o ângulo atual do hexágono
                float rot = -angRot * Mathf.Deg2Rad;
                Vector2 pRotada = new Vector2(
                    pAresta.x * Mathf.Cos(rot) - pAresta.y * Mathf.Sin(rot),
                    pAresta.x * Mathf.Sin(rot) + pAresta.y * Mathf.Cos(rot));

                var p = new GameObject("FaisRefl");
                p.transform.position = (Vector2)playerStats.transform.position + pRotada;
                var psr = p.AddComponent<SpriteRenderer>();
                psr.sprite = GerarDisco(6);
                float alpha = ativo ? Random.Range(0.5f, 0.9f) : Random.Range(0.15f, 0.35f);
                psr.color = new Color(COR_CIANO.r, COR_CIANO.g, COR_CIANO.b, alpha);
                psr.sortingOrder = 14;
                p.transform.localScale = Vector3.one * Random.Range(0.05f, ativo ? 0.12f : 0.07f);
                Vector2 dir = pRotada.normalized * Random.Range(0.5f, ativo ? 2f : 0.8f);
                p.AddComponent<AutoDestroyFadeMove>().Iniciar(dir, 0.3f);
                Destroy(p, 0.5f);
            }
            yield return null;
        }
    }

    IEnumerator FlashReflexao(Vector2 pos)
    {
        Color cor = COR_CIANO;

        // Flash central brilhante
        var go = new GameObject("FlashRef");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GerarDisco(16);
        sr.color = new Color(0.6f, 0.95f, 1f, 1f); sr.sortingOrder = 16;
        go.transform.localScale = Vector3.one * 0.6f;
        go.AddComponent<AutoDestroyFade>().Iniciar(0.2f);
        Destroy(go, 0.4f);

        // Faíscas radiais saindo do inimigo (sensação de impacto)
        int qtd = Random.Range(6, 9);
        for (int i = 0; i < qtd; i++)
        {
            float ang = (i / (float)qtd) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));

            var fp = new GameObject("FaisRefImpacto");
            fp.transform.position = pos + dir * 0.15f;
            var fsr = fp.AddComponent<SpriteRenderer>();
            fsr.sprite = GerarDisco(6);
            fsr.color = new Color(cor.r, cor.g, cor.b, Random.Range(0.7f, 1f));
            fsr.sortingOrder = 17;
            fp.transform.localScale = Vector3.one * Random.Range(0.06f, 0.13f);
            fp.AddComponent<AutoDestroyFadeMove>().Iniciar(dir * Random.Range(1.5f, 3.5f), 0.25f);
            Destroy(fp, 0.45f);
        }

        // Anel de choque expandindo
        const int S = 20;
        var anel = new GameObject("AnelRefl");
        anel.transform.position = pos;
        var lr = anel.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = S;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = 16; lr.numCapVertices = 2;
        Destroy(anel, 0.4f); // failsafe

        float dur = 0.28f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (anel == null) yield break;
            float p = t / dur;
            float r = Mathf.Lerp(0.15f, 1.1f, p);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.12f, 0.01f, p);
            lr.startColor = lr.endColor = new Color(cor.r, cor.g, cor.b, Mathf.Lerp(0.9f, 0f, p));
            for (int i = 0; i < S; i++)
            {
                float a = 360f / S * i * Mathf.Deg2Rad;
                lr.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
            yield return null;
        }
        if (anel != null) Destroy(anel);
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
