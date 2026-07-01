using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DespertarAnciaoUltimate : MonoBehaviour, IUltimateCosmetico
{
    bool cosmetico;
    public void ExecutarCosmetico() { if (ativo) return; cosmetico = true; StartCoroutine(CorotinaAtivacao()); }

    [Header("Configurações")]
    public float raio           = 14f;
    public float duracao        = 8f;
    public float cooldown       = 28f;
    public float intervaloGolpe = 0.55f;
    public float danoGolpe      = 28f;
    public float raioImpacto    = 2.5f;

    public float CooldownRestante => cooldownRestante;
    public bool  Ativo            => ativo;

    float       cooldownRestante;
    bool        ativo;
    PlayerStats playerStats;

    // Paleta do Ancião
    static readonly Color COR_VOID    = new Color(0.06f, 0f,    0.12f);
    static readonly Color COR_ROXO    = new Color(0.32f, 0f,    0.58f);
    static readonly Color COR_VIOLETA = new Color(0.52f, 0.02f, 0.90f);
    static readonly Color COR_GLOW    = new Color(0.72f, 0.15f, 1.00f);
    static readonly Color COR_PUPILA  = new Color(1.00f, 0.82f, 0.10f);

    // Parâmetros dos blobs (raio, segs, amplitude de respiração)
    static readonly float[] BLOB_R   = { 3.8f, 2.8f, 1.8f, 0.95f };
    static readonly int[]   BLOB_N   = { 40,   30,   22,   16    };
    static readonly float[] BLOB_AMP = { 0.12f, 0.10f, 0.08f, 0.05f };

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

        Vector2 centroCampo = transform.position;
        Vector2 posEntidade = centroCampo + Vector2.up * 9f;

        yield return StartCoroutine(AbrirPortal(posEntidade));

        var entidadeGO = CriarEntidade(posEntidade);
        StartCoroutine(AuraBruxulaLoop(entidadeGO));
        yield return StartCoroutine(AnimarEntrada(entidadeGO, posEntidade));

        float intervaloEfetivo = intervaloGolpe;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.DespertarFurioso))
            intervaloEfetivo = 0.35f;

        float elapsed = 0f;
        float proximo = 0f;
        while (elapsed < duracao)
        {
            elapsed += Time.deltaTime;
            proximo -= Time.deltaTime;

            AnimarEntidade(entidadeGO, posEntidade, elapsed);

            if (proximo <= 0f)
            {
                proximo = intervaloEfetivo;
                var alvo = EscolherAlvo(centroCampo);
                if (alvo.HasValue)
                    StartCoroutine(AnimarTentaculo(entidadeGO.transform.position, alvo.Value));
            }
            yield return null;
        }

        ativo = false;
        yield return StartCoroutine(AnimarSaida(entidadeGO));
    }

    // ─── LÓGICA ─────────────────────────────────────────────────────────────

    Vector2? EscolherAlvo(Vector2 centro)
    {
        var candidatos = new List<Vector2>();
        foreach (var c in Physics2D.OverlapCircleAll(centro, raio))
        {
            if (c.gameObject == gameObject) continue;
            var root = ResolverInimigo(c.gameObject);
            if (root != null) candidatos.Add(root.transform.position);
        }
        if (candidatos.Count == 0) return null;
        return candidatos[Random.Range(0, candidatos.Count)];
    }

    void AplicarDano(Vector2 centro)
    {
        float raioEfetivo = raioImpacto;
        float danoEfetivo = danoGolpe;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.DespertarGigante))
        {
            raioEfetivo *= 1.6f;
            danoEfetivo *= 1.5f;
        }
        foreach (var c in Physics2D.OverlapCircleAll(centro, raioEfetivo))
        {
            var ic = c.GetComponent<InimigoController>() ?? c.GetComponentInParent<InimigoController>();
            if (ic != null && !cosmetico) ic.ReceberDano(danoEfetivo, Random.value < 0.2f);
        }
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

    // ─── PORTAL DE ENTRADA ──────────────────────────────────────────────────

    IEnumerator AbrirPortal(Vector2 pos)
    {
        // Fissura vertical de luz abrindo
        var fissura = new GameObject("Fissura");
        fissura.transform.position = pos;
        var lr = fissura.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.positionCount = 2;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = 22;

        float durFissura = 0.3f;
        for (float t = 0f; t < durFissura; t += Time.deltaTime)
        {
            float p = t / durFissura;
            float h = Mathf.Lerp(0f, 5f, p);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.05f, 0.6f, p);
            lr.startColor = lr.endColor = new Color(COR_GLOW.r, COR_GLOW.g, COR_GLOW.b, p);
            lr.SetPosition(0, pos - Vector2.up * h);
            lr.SetPosition(1, pos + Vector2.up * h);
            yield return null;
        }

        // Expande para anel de portal
        const int S = 36;
        var portalGO = new GameObject("PortalAnel");
        portalGO.transform.position = pos;
        var plr = portalGO.AddComponent<LineRenderer>();
        plr.useWorldSpace = true; plr.loop = true; plr.positionCount = S;
        plr.material = new Material(Shader.Find("Sprites/Default"));
        plr.sortingOrder = 20;

        float durPortal = 0.4f;
        for (float t = 0f; t < durPortal; t += Time.deltaTime)
        {
            float p  = t / durPortal;
            float pe = 1f - Mathf.Pow(1f - p, 2f);
            float r  = Mathf.Lerp(0f, 3.5f, pe);
            plr.startWidth = plr.endWidth = Mathf.Lerp(0.6f, 0.12f, p);
            plr.startColor = plr.endColor = new Color(COR_GLOW.r, COR_GLOW.g, COR_GLOW.b, Mathf.Lerp(1f, 0.6f, p));
            for (int i = 0; i < S; i++)
            {
                float a = (360f / S) * i * Mathf.Deg2Rad;
                plr.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
            // Fissura encolhe
            Color cf = lr.startColor; cf.a = Mathf.Lerp(1f, 0f, p); lr.startColor = lr.endColor = cf;
            yield return null;
        }

        // Disco escuro de portal (buraco na realidade)
        var discoGO = new GameObject("PortalDisco");
        discoGO.transform.position = pos;
        var dsr = discoGO.AddComponent<SpriteRenderer>();
        dsr.sprite = GerarDisco(32); dsr.sortingOrder = 19;
        dsr.color  = new Color(COR_VOID.r, COR_VOID.g, COR_VOID.b, 0.85f);
        discoGO.transform.localScale = Vector3.one * 7f;

        // Partículas saindo do portal
        for (int i = 0; i < 16; i++)
        {
            float ang   = i / 16f * Mathf.PI * 2f;
            float speed = Random.Range(3f, 8f);
            var p = new GameObject("PortalPart");
            p.transform.position = pos + Random.insideUnitCircle * 1f;
            p.transform.localScale = Vector3.one * Random.Range(0.08f, 0.2f);
            var psr = p.AddComponent<SpriteRenderer>();
            psr.sprite = GerarDisco(8); psr.sortingOrder = 21;
            psr.color  = Random.value < 0.4f ? COR_PUPILA : COR_GLOW;
            p.AddComponent<AutoDestroyFadeMove>().Iniciar(
                new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * speed, Random.Range(0.3f, 0.6f));
            Destroy(p, 0.8f);
        }

        yield return new WaitForSeconds(0.25f);
        Destroy(fissura);
        Destroy(portalGO);
        Destroy(discoGO);
    }

    // ─── ENTIDADE — CRIAÇÃO ─────────────────────────────────────────────────

    GameObject CriarEntidade(Vector2 pos)
    {
        var root = new GameObject("Anciao");
        root.transform.position = pos;

        // 4 blobs concêntricos (respiram individualmente)
        Color[] blobCores = { COR_VOID, COR_ROXO, COR_VIOLETA, COR_GLOW };
        float[] blobLarg  = { 0.30f,    0.22f,     0.16f,       0.10f    };
        for (int i = 0; i < 4; i++)
        {
            var go = new GameObject($"Blob{i}");
            go.transform.SetParent(root.transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false; lr.loop = true; lr.positionCount = BLOB_N[i];
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.sortingOrder = 15 + i;
            lr.startWidth = lr.endWidth = blobLarg[i];
            lr.startColor = lr.endColor = new Color(blobCores[i].r, blobCores[i].g, blobCores[i].b, 0f);
            DefinirBlob(lr, BLOB_R[i], BLOB_N[i], 0f);
        }

        // 5 tentáculos orbitais
        Color[] tentCores = { COR_ROXO, COR_VIOLETA, COR_ROXO, COR_VIOLETA, COR_ROXO };
        for (int i = 0; i < 5; i++)
        {
            var go = new GameObject($"TentOrb{i}");
            go.transform.SetParent(root.transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false; lr.positionCount = 12;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.sortingOrder = 13;
            lr.startWidth = 0.22f; lr.endWidth = 0.04f;
            lr.startColor = new Color(tentCores[i].r, tentCores[i].g, tentCores[i].b, 0f);
            lr.endColor   = new Color(COR_GLOW.r, COR_GLOW.g, COR_GLOW.b, 0f);
        }

        // Olho central: íris elíptica + pupila
        var irisGO = new GameObject("IrisCentral");
        irisGO.transform.SetParent(root.transform, false);
        {
            const int S = 24;
            var lr = irisGO.AddComponent<LineRenderer>();
            lr.useWorldSpace = false; lr.loop = true; lr.positionCount = S;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.sortingOrder = 19; lr.startWidth = lr.endWidth = 0.14f;
            lr.startColor = lr.endColor = new Color(COR_GLOW.r, COR_GLOW.g, COR_GLOW.b, 0f);
            for (int i = 0; i < S; i++)
            {
                float a = (360f / S) * i * Mathf.Deg2Rad;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.72f, Mathf.Sin(a) * 0.45f));
            }
        }

        var pupilaGO = new GameObject("PupilaCentral");
        pupilaGO.transform.SetParent(root.transform, false);
        {
            var sr = pupilaGO.AddComponent<SpriteRenderer>();
            sr.sprite = GerarDisco(16); sr.sortingOrder = 20;
            sr.color  = new Color(COR_PUPILA.r, COR_PUPILA.g, COR_PUPILA.b, 0f);
            pupilaGO.transform.localScale = new Vector3(0.62f, 0.38f, 1f);
        }

        // 2 olhos satélites
        AdicionarOlhoPequeno(root, new Vector2(-1.15f,  0.45f), "OlhoE");
        AdicionarOlhoPequeno(root, new Vector2( 1.15f,  0.45f), "OlhoD");

        return root;
    }

    void AdicionarOlhoPequeno(GameObject parent, Vector2 lp, string nome)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = lp;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GerarDisco(10); sr.sortingOrder = 18;
        sr.color  = new Color(COR_GLOW.r, COR_GLOW.g, COR_GLOW.b, 0f);
        go.transform.localScale = Vector3.one * 0.32f;
    }

    // ─── ENTIDADE — ANIMAÇÃO ─────────────────────────────────────────────────

    void DefinirBlob(LineRenderer lr, float r, int n, float time)
    {
        int blobIdx = 0;
        // Identifica o blob pelo raio para pegar a amplitude certa
        for (int i = 0; i < BLOB_R.Length; i++) if (Mathf.Abs(BLOB_R[i] - r) < 0.1f) { blobIdx = i; break; }
        float amp = BLOB_AMP[blobIdx];
        for (int i = 0; i < n; i++)
        {
            float ang = (360f / n) * i * Mathf.Deg2Rad;
            float ri  = r * (1f + Mathf.Sin(i * 1.9f + time * 1.4f)          * amp
                               + Mathf.Sin(i * 0.8f + time * 0.85f + 2.1f)   * amp * 0.6f
                               + Mathf.Sin(i * 3.1f + time * 2.3f + 4.7f)    * amp * 0.3f);
            lr.SetPosition(i, new Vector3(Mathf.Cos(ang) * ri, Mathf.Sin(ang) * ri));
        }
    }

    void AnimarEntidade(GameObject root, Vector2 posBase, float elapsed)
    {
        if (root == null) return;
        // Flutuação suave com dois harmônicos
        root.transform.position = posBase
            + Vector2.up * (Mathf.Sin(elapsed * 1.1f) * 0.35f + Mathf.Sin(elapsed * 2.7f) * 0.1f);

        float pulso  = Mathf.Sin(elapsed * 5.2f) * 0.5f + 0.5f;
        float pulso2 = Mathf.Sin(elapsed * 3.1f) * 0.5f + 0.5f;

        var children = root.GetComponentsInChildren<Transform>();
        foreach (var child in children)
        {
            if (child == root.transform) continue;
            string nome = child.gameObject.name;

            // Blobs: respiram
            if (nome.StartsWith("Blob") && int.TryParse(nome.Substring(4), out int bi))
            {
                var lr = child.GetComponent<LineRenderer>();
                if (lr != null)
                {
                    DefinirBlob(lr, BLOB_R[bi], BLOB_N[bi], elapsed + bi * 1.5f);
                    float[] alphas = { 0.85f, 0.75f + pulso * 0.2f, 0.65f + pulso * 0.3f, 0.5f + pulso * 0.45f };
                    Color c = lr.startColor; c.a = alphas[bi]; lr.startColor = lr.endColor = c;
                }
            }

            // Tentáculos orbitais: ondulam
            if (nome.StartsWith("TentOrb") && int.TryParse(nome.Substring(7), out int ti))
            {
                var lr = child.GetComponent<LineRenderer>();
                if (lr != null) AnimarTentaculoOrbital(lr, (360f / 5f) * ti, elapsed, ti);
            }

            // Olhos: pulsam e escalam
            if (nome == "OlhoE" || nome == "OlhoD")
            {
                var sr = child.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    float a = 0.65f + pulso2 * 0.35f;
                    sr.color = new Color(COR_GLOW.r, COR_GLOW.g, COR_GLOW.b, a);
                    sr.transform.localScale = Vector3.one * (0.28f + pulso2 * 0.07f);
                }
            }

            // Pupila central: pulsa em amarelo-dourado
            if (nome == "PupilaCentral")
            {
                var sr = child.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = new Color(COR_PUPILA.r, COR_PUPILA.g, COR_PUPILA.b, 0.7f + pulso * 0.3f);
                    // Contrai/dilata como pupila real
                    float sz = 0.55f + pulso * 0.12f;
                    sr.transform.localScale = new Vector3(sz, sz * 0.62f, 1f);
                }
            }

            // Íris central
            if (nome == "IrisCentral")
            {
                var lr = child.GetComponent<LineRenderer>();
                if (lr != null)
                {
                    lr.startColor = lr.endColor = new Color(COR_GLOW.r, COR_GLOW.g, COR_GLOW.b, 0.7f + pulso * 0.3f);
                    lr.startWidth = lr.endWidth = 0.12f + pulso * 0.04f;
                }
            }
        }
    }

    void AnimarTentaculoOrbital(LineRenderer lr, float angBase, float elapsed, int idx)
    {
        const int N = 12;
        float angAtual = (angBase + elapsed * 24f + idx * 22f) * Mathf.Deg2Rad;
        Vector2 dir    = new Vector2(Mathf.Cos(angAtual), Mathf.Sin(angAtual));
        Vector2 perp   = new Vector2(-dir.y, dir.x);

        float r_corpo = BLOB_R[0]; // externo
        float comp    = r_corpo * 1.3f;

        lr.positionCount = N;
        for (int i = 0; i < N; i++)
        {
            float t    = i / (float)(N - 1);
            float dist = Mathf.Lerp(r_corpo * 0.5f, r_corpo + comp, t);
            float onda = Mathf.Sin(t * Mathf.PI * 3f - elapsed * 3.5f + idx * 1.4f) * 0.35f * Mathf.Sin(t * Mathf.PI);
            lr.SetPosition(i, dir * dist + perp * onda);
        }

        float alpha = 0.4f + Mathf.Sin(elapsed * 2f + idx) * 0.3f;
        Color cs = lr.startColor; cs.a = alpha; lr.startColor = cs;
        Color ce = lr.endColor;   ce.a = alpha * 0.5f; lr.endColor = ce;
    }

    // ─── ENTRADA E SAÍDA ─────────────────────────────────────────────────────

    IEnumerator AnimarEntrada(GameObject root, Vector2 posAlvo)
    {
        Vector2 posInicio = posAlvo + Vector2.up * 7f;
        var lrs = root.GetComponentsInChildren<LineRenderer>();
        var srs = root.GetComponentsInChildren<SpriteRenderer>();

        float dur = 0.55f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p  = t / dur;
            float pe = 1f - Mathf.Pow(1f - p, 2.5f); // ease-out pesado
            root.transform.position = Vector2.Lerp(posInicio, posAlvo, pe);
            foreach (var lr in lrs) { Color c = lr.startColor; c.a = pe * 0.9f; lr.startColor = lr.endColor = c; }
            foreach (var sr in srs) { Color c = sr.color;      c.a = pe; sr.color = c; }
            yield return null;
        }
        root.transform.position = posAlvo;

        // Aterrisagem: anel de choque + tremida
        StartCoroutine(AnelExpansao(posAlvo, 4f, 0.4f, COR_VIOLETA, 32));
        StartCoroutine(TremidaEntidade(root, 0.25f));
    }

    IEnumerator TremidaEntidade(GameObject root, float dur)
    {
        Vector2 pos = root.transform.position;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            root.transform.position = (Vector2)pos + Random.insideUnitCircle * Mathf.Lerp(0.18f, 0f, p);
            yield return null;
        }
        root.transform.position = pos;
    }

    IEnumerator AnimarSaida(GameObject root)
    {
        var lrs = root.GetComponentsInChildren<LineRenderer>();
        var srs = root.GetComponentsInChildren<SpriteRenderer>();
        Vector2 posAtual = root.transform.position;
        Vector2 posAlvo  = posAtual + Vector2.up * 7f;

        // Partículas explodindo para fora ao partir
        for (int i = 0; i < 14; i++)
        {
            float ang = i / 14f * Mathf.PI * 2f;
            var p = new GameObject("SaidaPart");
            p.transform.position = posAtual;
            p.transform.localScale = Vector3.one * Random.Range(0.08f, 0.22f);
            var psr = p.AddComponent<SpriteRenderer>();
            psr.sprite = GerarDisco(8); psr.sortingOrder = 20;
            psr.color  = Random.value < 0.4f ? COR_PUPILA : COR_VIOLETA;
            p.AddComponent<AutoDestroyFadeMove>().Iniciar(
                new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(3f, 8f), Random.Range(0.3f, 0.6f));
            Destroy(p, 0.8f);
        }

        float dur = 0.55f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            root.transform.position = Vector2.Lerp(posAtual, posAlvo, p * p); // ease-in
            foreach (var lr in lrs) { Color c = lr.startColor; c.a = Mathf.Lerp(c.a, 0f, p); lr.startColor = lr.endColor = c; }
            foreach (var sr in srs) { Color c = sr.color;      c.a = Mathf.Lerp(c.a, 0f, p); sr.color = c; }
            yield return null;
        }
        Destroy(root);
    }

    // ─── AURA CONTÍNUA (coroutine própria) ───────────────────────────────────

    IEnumerator AuraBruxulaLoop(GameObject root)
    {
        int frame = 0;
        while (root != null)
        {
            frame++;
            if (frame % 6 == 0 && root != null)
            {
                Vector2 pos = (Vector2)root.transform.position + Random.insideUnitCircle * BLOB_R[0] * 0.9f;
                var p = new GameObject("AuraPart");
                p.transform.position = pos;
                p.transform.localScale = Vector3.one * Random.Range(0.04f, 0.13f);
                var psr = p.AddComponent<SpriteRenderer>();
                psr.sprite = GerarDisco(8); psr.sortingOrder = 12;
                bool amarelo = Random.value < 0.2f;
                psr.color = amarelo ? new Color(COR_PUPILA.r,  COR_PUPILA.g,  COR_PUPILA.b,  0.8f)
                                    : new Color(COR_VIOLETA.r, COR_VIOLETA.g, COR_VIOLETA.b, 0.7f);
                p.AddComponent<AutoDestroyFadeMove>().Iniciar(
                    Vector2.up * Random.Range(0.8f, 2.2f) + Random.insideUnitCircle * 0.4f,
                    Random.Range(0.4f, 1.1f));
                Destroy(p, 1.4f);
            }
            yield return null;
        }
    }

    // ─── TENTÁCULO DE ATAQUE ─────────────────────────────────────────────────

    IEnumerator AnimarTentaculo(Vector2 origem, Vector2 destino)
    {
        // Marcador de alvo melhorado
        var marcador = CriarMarcador(destino);
        StartCoroutine(AnimarMarcador(marcador, 0.25f));

        // Carga no ponto de origem (glow pulsando)
        var cargaGO = new GameObject("CargaTentaculo");
        cargaGO.transform.position = origem;
        var csr = cargaGO.AddComponent<SpriteRenderer>();
        csr.sprite = GerarDisco(12); csr.sortingOrder = 22;
        csr.color  = new Color(COR_GLOW.r, COR_GLOW.g, COR_GLOW.b, 0f);
        cargaGO.transform.localScale = Vector3.one * 0.4f;

        float durCarga = 0.22f;
        for (float t = 0f; t < durCarga; t += Time.deltaTime)
        {
            float p = t / durCarga;
            csr.color = new Color(COR_GLOW.r, COR_GLOW.g, COR_GLOW.b, p * 0.9f);
            cargaGO.transform.localScale = Vector3.one * (0.4f + p * 0.3f);
            yield return null;
        }
        Destroy(cargaGO);
        Destroy(marcador);

        // Tentáculo de 2 camadas
        const int N = 20;

        var goOuter = new GameObject("TentOuter");
        var lrOuter = goOuter.AddComponent<LineRenderer>();
        lrOuter.useWorldSpace = true; lrOuter.positionCount = N;
        lrOuter.material = new Material(Shader.Find("Sprites/Default"));
        lrOuter.sortingOrder = 15; lrOuter.startWidth = 0.55f; lrOuter.endWidth = 0.18f;
        lrOuter.startColor = new Color(COR_VOID.r,  COR_VOID.g,  COR_VOID.b,  0.9f);
        lrOuter.endColor   = new Color(COR_ROXO.r,  COR_ROXO.g,  COR_ROXO.b,  0.7f);

        var goInner = new GameObject("TentInner");
        var lrInner = goInner.AddComponent<LineRenderer>();
        lrInner.useWorldSpace = true; lrInner.positionCount = N;
        lrInner.material = new Material(Shader.Find("Sprites/Default"));
        lrInner.sortingOrder = 16; lrInner.startWidth = 0.22f; lrInner.endWidth = 0.06f;
        lrInner.startColor = new Color(COR_GLOW.r, COR_GLOW.g, COR_GLOW.b, 1f);
        lrInner.endColor   = new Color(COR_VIOLETA.r, COR_VIOLETA.g, COR_VIOLETA.b, 0.8f);

        // Strike rápido
        float durStrike = 0.16f;
        for (float t = 0f; t < durStrike; t += Time.deltaTime)
        {
            float p = t / durStrike;
            AtualizarTentaculo(lrOuter, origem, destino, N, p, 1.2f);
            AtualizarTentaculo(lrInner, origem, destino, N, p, 0.6f);
            yield return null;
        }

        // Impacto completo
        AtualizarTentaculo(lrOuter, origem, destino, N, 1f, 0f);
        AtualizarTentaculo(lrInner, origem, destino, N, 1f, 0f);
        AplicarDano(destino);
        StartCoroutine(AnimarImpacto(destino));

        // Fade out
        float durFade = 0.22f;
        for (float t = 0f; t < durFade; t += Time.deltaTime)
        {
            float p = t / durFade;
            Color co = lrOuter.startColor; co.a = Mathf.Lerp(co.a, 0f, p); lrOuter.startColor = co;
            Color co2 = lrOuter.endColor;  co2.a = Mathf.Lerp(co2.a, 0f, p); lrOuter.endColor = co2;
            Color ci = lrInner.startColor; ci.a = Mathf.Lerp(ci.a, 0f, p); lrInner.startColor = ci;
            Color ci2 = lrInner.endColor;  ci2.a = Mathf.Lerp(ci2.a, 0f, p); lrInner.endColor = ci2;
            yield return null;
        }
        Destroy(goOuter); Destroy(goInner);
    }

    void AtualizarTentaculo(LineRenderer lr, Vector2 orig, Vector2 dest, int n, float p, float waveAmp)
    {
        Vector2 dir  = (dest - orig);
        Vector2 perp = new Vector2(-dir.y, dir.x).normalized;
        for (int i = 0; i < n; i++)
        {
            float t  = i / (float)(n - 1);
            Vector2 pt = Vector2.Lerp(orig, dest, t * p);
            if (waveAmp > 0.01f)
            {
                float wave = Mathf.Sin(t * Mathf.PI * 2.8f + p * 9f) * waveAmp * (1f - p) * Mathf.Sin(t * Mathf.PI);
                pt += perp * wave;
            }
            lr.SetPosition(i, pt);
        }
    }

    // ─── MARCADOR DE ALVO ────────────────────────────────────────────────────

    GameObject CriarMarcador(Vector2 pos)
    {
        var root = new GameObject("MarcadorAnciao");
        root.transform.position = pos;

        // Anel que vai contrair
        const int S = 24;
        var anelGO = new GameObject("AnelAlvo");
        anelGO.transform.SetParent(root.transform, false);
        var lr = anelGO.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = S;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = 10; lr.startWidth = lr.endWidth = 0.07f;
        lr.startColor = lr.endColor = new Color(COR_GLOW.r, COR_GLOW.g, COR_GLOW.b, 0.8f);
        float r = raioImpacto * 1.8f; // começa maior, vai contrair
        for (int i = 0; i < S; i++)
        {
            float a = (360f / S) * i * Mathf.Deg2Rad;
            lr.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
        }

        // X-mark (2 linhas cruzadas)
        for (int i = 0; i < 2; i++)
        {
            float ang = (45f + i * 90f) * Mathf.Deg2Rad;
            var linha = new GameObject($"Cruz{i}");
            linha.transform.SetParent(root.transform, false);
            var llr = linha.AddComponent<LineRenderer>();
            llr.useWorldSpace = true; llr.positionCount = 2;
            llr.material = new Material(Shader.Find("Sprites/Default"));
            llr.sortingOrder = 11; llr.startWidth = llr.endWidth = 0.05f;
            llr.startColor = llr.endColor = new Color(COR_GLOW.r, COR_GLOW.g, COR_GLOW.b, 0.7f);
            float d = raioImpacto * 0.8f;
            llr.SetPosition(0, pos + new Vector2( Mathf.Cos(ang), Mathf.Sin(ang)) * d);
            llr.SetPosition(1, pos + new Vector2(-Mathf.Cos(ang),-Mathf.Sin(ang)) * d);
        }

        // Sombra escura no chão
        var sombraGO = new GameObject("SombraAlvo");
        sombraGO.transform.SetParent(root.transform, false);
        sombraGO.transform.position = pos;
        var ssr = sombraGO.AddComponent<SpriteRenderer>();
        ssr.sprite = GerarDisco(16); ssr.sortingOrder = 9;
        ssr.color  = new Color(COR_VOID.r, COR_VOID.g, COR_VOID.b, 0.5f);
        sombraGO.transform.localScale = Vector3.one * raioImpacto * 2f;

        return root;
    }

    IEnumerator AnimarMarcador(GameObject root, float dur)
    {
        var lrs = root.GetComponentsInChildren<LineRenderer>();
        Vector2 pos = root.transform.position;
        float raioInicial = raioImpacto * 1.8f;

        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (root == null) yield break;

            float p     = t / dur;
            float pulso = Mathf.Sin(p * Mathf.PI * 10f) * 0.5f + 0.5f;

            foreach (var lr in lrs)
            {
                if (lr == null) continue;
                if (!lr.loop) continue;
                float r = Mathf.Lerp(raioInicial, raioImpacto, p);
                int   n = lr.positionCount;
                for (int i = 0; i < n; i++)
                {
                    float a = (360f / n) * i * Mathf.Deg2Rad;
                    lr.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
                }
                lr.startWidth = lr.endWidth = 0.06f + pulso * 0.06f;
            }
            yield return null;
        }
    }

    // ─── IMPACTO ─────────────────────────────────────────────────────────────

    IEnumerator AnimarImpacto(Vector2 pos)
    {
        // 3 anéis em velocidades/raios diferentes
        StartCoroutine(AnelExpansao(pos, raioImpacto * 1.5f, 0.38f, COR_GLOW,    32));
        StartCoroutine(AnelExpansao(pos, raioImpacto * 2.2f, 0.45f, COR_VIOLETA, 28));
        StartCoroutine(AnelExpansao(pos, raioImpacto * 3.0f, 0.55f, COR_ROXO,    22));

        // Disco flash central
        var disco = new GameObject("FlashImpacto");
        disco.transform.position = pos;
        var sr = disco.AddComponent<SpriteRenderer>();
        sr.sprite = GerarDisco(24); sr.sortingOrder = 20;
        sr.color  = new Color(COR_GLOW.r, COR_GLOW.g, COR_GLOW.b, 0.85f);
        disco.transform.localScale = Vector3.one * 0.2f;
        float durDisco = 0.28f;
        for (float t = 0f; t < durDisco; t += Time.deltaTime)
        {
            float p = t / durDisco;
            disco.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, raioImpacto * 2f, Mathf.Sqrt(p));
            sr.color = new Color(COR_GLOW.r, COR_GLOW.g, COR_GLOW.b, Mathf.Lerp(0.85f, 0f, p * p));
            yield return null;
        }
        Destroy(disco);

        // 12 partículas escuras voando
        for (int i = 0; i < 12; i++)
        {
            float ang   = i / 12f * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
            float speed = Random.Range(3f, 9f);
            var p = new GameObject("ImpactoPart");
            p.transform.position = pos;
            p.transform.localScale = Vector3.one * Random.Range(0.08f, 0.2f);
            var psr = p.AddComponent<SpriteRenderer>();
            psr.sprite = GerarDisco(8); psr.sortingOrder = 17;
            psr.color  = i % 3 == 0 ? COR_PUPILA : COR_VIOLETA;
            p.AddComponent<AutoDestroyFadeMove>().Iniciar(
                new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * speed, Random.Range(0.28f, 0.5f));
            Destroy(p, 0.65f);
        }

        // Marca escura no chão
        StartCoroutine(MarcaChao(pos));
    }

    IEnumerator MarcaChao(Vector2 pos)
    {
        var go = new GameObject("MarcaChaoAnciao");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GerarDisco(24); sr.sortingOrder = 8;
        go.transform.localScale = Vector3.one * raioImpacto * 1.6f;
        float dur = 1.2f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            float a = p < 0.1f ? Mathf.Lerp(0f, 0.5f, p / 0.1f) : Mathf.Lerp(0.5f, 0f, (p - 0.1f) / 0.9f);
            sr.color = new Color(COR_VOID.r, COR_VOID.g, COR_VOID.b, a);
            yield return null;
        }
        Destroy(go);
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
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.45f, 0.02f, p);
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

    // ─── SPRITES PROCEDURAIS ─────────────────────────────────────────────────

    static Sprite GerarDisco(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++)
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
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cx));
            tex.SetPixel(x, y, new Color(cor.r, cor.g, cor.b, Mathf.Clamp01(1f - d / cx)));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }
}
