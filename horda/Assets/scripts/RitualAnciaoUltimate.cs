using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RitualAnciaoUltimate : MonoBehaviour, IUltimateCosmetico
{
    bool cosmetico;
    public void ExecutarCosmetico() { if (ativo) return; cosmetico = true; StartCoroutine(CorotinaAtivacao()); }

    [Header("Configurações")]
    public float raio          = 8f;
    public float duracao       = 9f;
    public float cooldown      = 26f;
    public float danoInicial   = 5f;
    public float danoFinal     = 50f;
    public float tempoRampa    = 6f;

    public float CooldownRestante => cooldownRestante;
    public bool  Ativo            => ativo;

    float       cooldownRestante;
    bool        ativo;
    Vector2     centroRitual;
    PlayerStats playerStats;

    readonly Dictionary<InimigoController, float> temposDentro = new Dictionary<InimigoController, float>();
    float proxTick;

    // ── Cores do ritual ──────────────────────────────────────────────────────
    static readonly Color COR_INICIO  = new Color(0.55f, 0.25f, 1.00f);
    static readonly Color COR_MEIO    = new Color(0.85f, 0.30f, 1.00f);
    static readonly Color COR_FINAL   = new Color(1.00f, 0.15f, 0.10f);
    static readonly Color COR_OURO    = new Color(1.00f, 0.85f, 0.20f);
    static readonly Color COR_ESPIRITO= new Color(0.75f, 0.50f, 1.00f);

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
            InputBindings.UltimateDown() && cooldownRestante <= 0f && !ativo)
            StartCoroutine(CorotinaAtivacao());
        SincronizarUI();
    }

    void SincronizarUI()
    {
        if (playerStats == null) return;
        playerStats.ultimateChargeTime = cooldown - Mathf.Max(0f, cooldownRestante);
        playerStats.ultimateReady      = cooldownRestante <= 0f && !ativo;
    }

    // ─── COROUTINE PRINCIPAL ─────────────────────────────────────────────────

    IEnumerator CorotinaAtivacao()
    {
        ativo            = true;
        cooldownRestante = cooldown;
        centroRitual     = transform.position;
        temposDentro.Clear();
        proxTick         = 0f;

        // RitualAmpliado: +40% de raio
        float raioOriginal = raio;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.RitualAmpliado))
            raio *= 1.4f;

        // Efeito de carga no player
        yield return StartCoroutine(EfeitoCarga());

        var ritualGO = CriarEstruturaRitual(centroRitual);
        yield return StartCoroutine(AnimarInvocacao(ritualGO));

        // Efeitos contínuos paralelos
        StartCoroutine(ParticulasOrbitando(centroRitual));
        StartCoroutine(PulsosRitmicos(centroRitual));
        StartCoroutine(AuraPlayer());
        StartCoroutine(EspiritosCentro(centroRitual));

        float elapsed = 0f;
        while (elapsed < duracao)
        {
            elapsed += Time.deltaTime;
            AtualizarInimigos(elapsed);
            AnimarRitual(ritualGO, elapsed);
            yield return null;
        }

        Explodir(ritualGO);
        temposDentro.Clear();
        raio = raioOriginal; // restaura raio base
        ativo = false;
        StartCoroutine(FadeOutDestruir(ritualGO, 0.6f));
    }

    // ─── LÓGICA ──────────────────────────────────────────────────────────────

    void AtualizarInimigos(float elapsed)
    {
        proxTick -= Time.deltaTime;
        bool tick = proxTick <= 0f;
        if (tick) proxTick = 0.5f;

        var dentroAgora = new HashSet<InimigoController>();

        foreach (var c in Physics2D.OverlapCircleAll(centroRitual, raio))
        {
            if (c.gameObject == gameObject) continue;
            var root = ResolverInimigo(c.gameObject);
            if (root == null) continue;
            var ic = root.GetComponent<InimigoController>();
            if (ic == null || ic.estaMorrendo) continue;
            if (!DentroPentagono(root.transform.position, centroRitual, raio)) continue;

            dentroAgora.Add(ic);
            if (!temposDentro.ContainsKey(ic)) temposDentro[ic] = 0f;
            temposDentro[ic] += Time.deltaTime;

            if (tick)
            {
                float t    = Mathf.Clamp01(temposDentro[ic] / tempoRampa);
                float dano = Mathf.Lerp(danoInicial, danoFinal, t) * 0.5f;
                if (!cosmetico) ic.ReceberDano(dano, false);
            }

            // Tentáculo visual ao alvo
            if (Random.value < 0.04f)
                StartCoroutine(TentaculoAlvo(centroRitual, root.transform.position));
        }

        var saíram = new List<InimigoController>();
        foreach (var kv in temposDentro)
            if (!dentroAgora.Contains(kv.Key)) saíram.Add(kv.Key);
        foreach (var ic in saíram) temposDentro.Remove(ic);
    }

    void Explodir(GameObject ritualGO)
    {
        // RitualExplosivo: explosão final tem 2x de raio e dano
        float multExplosivo = SkillEvolutionManager.Tem(SkillEvolutionType.RitualExplosivo) ? 2f : 1f;
        float raioExplosao  = raio * multExplosivo;

        foreach (var c in Physics2D.OverlapCircleAll(centroRitual, raioExplosao))
        {
            var ic = c.GetComponent<InimigoController>() ?? c.GetComponentInParent<InimigoController>();
            if (ic == null) continue;

            // Inimigos que estavam dentro do pentágono
            if (temposDentro.TryGetValue(ic, out float tempo))
            {
                float t = Mathf.Clamp01(tempo / tempoRampa);
                if (!cosmetico) ic.ReceberDano(Mathf.Lerp(danoInicial, danoFinal, t) * 2f * multExplosivo, true);
            }
            else if (multExplosivo > 1f)
            {
                // Inimigos na área expandida recebem dano base
                if (!cosmetico) ic.ReceberDano(danoFinal * multExplosivo, true);
            }
        }

        // Remove os que já foram processados via dicionário (evita duplo dano)
        StartCoroutine(AnimarExplosaoFinal(centroRitual));
    }

    // ─── VISUAIS — ESTRUTURA ─────────────────────────────────────────────────

    GameObject CriarEstruturaRitual(Vector2 centro)
    {
        var root = new GameObject("RitualAnciao");
        root.transform.position = centro;

        // Círculo externo (anel base)
        CriarAnel(root, "CirculoExt", raio * 1.08f, 64, 0.12f, new Color(COR_INICIO.r, COR_INICIO.g, COR_INICIO.b, 0f));
        CriarAnel(root, "CirculoInt", raio * 0.30f, 32, 0.06f, new Color(COR_OURO.r, COR_OURO.g, COR_OURO.b, 0f));

        // Pentágono
        CriarPoligonoFechado(root, "Pentagono", 5, raio, 0.14f, new Color(COR_INICIO.r, COR_INICIO.g, COR_INICIO.b, 0f));

        // Pentagrama (estrela de 5 pontas — conecta vértices alternados)
        CriarPentagrama(root, raio);

        // Runas nos vértices
        for (int i = 0; i < 5; i++)
        {
            float ang = (72f * i - 90f) * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Cos(ang) * raio, Mathf.Sin(ang) * raio);
            var runaGO = new GameObject($"Runa{i}");
            runaGO.transform.SetParent(root.transform, false);
            runaGO.transform.localPosition = pos;
            var sr = runaGO.AddComponent<SpriteRenderer>();
            sr.sprite = GerarEstrela(24, COR_ESPIRITO);
            sr.color  = new Color(COR_ESPIRITO.r, COR_ESPIRITO.g, COR_ESPIRITO.b, 0f);
            sr.sortingOrder = 14;
            runaGO.transform.localScale = Vector3.one * 0.35f;

            // Glow atrás da runa
            var glowGO = new GameObject("RunaGlow");
            glowGO.transform.SetParent(runaGO.transform, false);
            var gsr = glowGO.AddComponent<SpriteRenderer>();
            gsr.sprite = GerarDisco(16);
            gsr.color  = new Color(COR_ESPIRITO.r, COR_ESPIRITO.g, COR_ESPIRITO.b, 0f);
            gsr.sortingOrder = 13;
            glowGO.transform.localScale = Vector3.one * 2.2f;
        }

        // Glow do chão (disco central translúcido)
        var glowChaoGO = new GameObject("GlowChao");
        glowChaoGO.transform.SetParent(root.transform, false);
        var glowChaoSR = glowChaoGO.AddComponent<SpriteRenderer>();
        glowChaoSR.sprite = GerarDisco(64);
        glowChaoSR.color  = new Color(COR_INICIO.r, COR_INICIO.g, COR_INICIO.b, 0f);
        glowChaoSR.sortingOrder = 9;
        glowChaoGO.transform.localScale = Vector3.one * (raio * 2.1f);

        return root;
    }

    LineRenderer CriarAnel(GameObject root, string nome, float r, int segs, float larg, Color cor)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(root.transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false; lr.loop = true; lr.positionCount = segs;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = 12; lr.startWidth = lr.endWidth = larg;
        lr.startColor = lr.endColor = cor;
        for (int i = 0; i < segs; i++)
        {
            float a = i / (float)segs * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
        }
        return lr;
    }

    void CriarPoligonoFechado(GameObject root, string nome, int lados, float r, float larg, Color cor)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(root.transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false; lr.loop = true; lr.positionCount = lados;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = 12; lr.startWidth = lr.endWidth = larg;
        lr.numCapVertices = 4; lr.startColor = lr.endColor = cor;
        for (int i = 0; i < lados; i++)
        {
            float a = (360f / lados * i - 90f) * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
        }
    }

    void CriarPentagrama(GameObject root, float r)
    {
        // Pentagrama: conecta cada vértice ao 2° seguinte (pula 1)
        var verts = new Vector3[5];
        for (int i = 0; i < 5; i++)
        {
            float a = (72f * i - 90f) * Mathf.Deg2Rad;
            verts[i] = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
        }
        int[] ordem = { 0, 2, 4, 1, 3 };
        var go = new GameObject("Pentagrama");
        go.transform.SetParent(root.transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false; lr.loop = true; lr.positionCount = 5;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = 11; lr.startWidth = lr.endWidth = 0.07f;
        lr.numCapVertices = 4;
        lr.startColor = lr.endColor = new Color(COR_OURO.r, COR_OURO.g, COR_OURO.b, 0f);
        for (int i = 0; i < 5; i++) lr.SetPosition(i, verts[ordem[i]]);
    }

    // ─── ANIMAÇÃO DE INVOCAÇÃO ────────────────────────────────────────────────

    IEnumerator AnimarInvocacao(GameObject root)
    {
        var lrs = root.GetComponentsInChildren<LineRenderer>();
        var srs = root.GetComponentsInChildren<SpriteRenderer>();

        float dur = 1.2f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = Mathf.SmoothStep(0f, 1f, t / dur);
            float pulso = Mathf.Sin(t * 18f) * 0.5f + 0.5f;

            foreach (var lr in lrs)
            {
                Color c = lr.startColor;
                c.a = p * (0.7f + pulso * 0.3f);
                lr.startColor = lr.endColor = c;
            }
            foreach (var sr in srs)
            {
                Color c = sr.color;
                c.a = p * (0.6f + pulso * 0.4f);
                sr.color = c;
            }

            // Flash dourado no player ao invocar
            if (t < 0.15f)
            {
                var psr = playerStats?.GetComponent<SpriteRenderer>();
                if (psr != null)
                    psr.color = Color.Lerp(Color.white, COR_OURO, (0.15f - t) / 0.15f);
            }
            else
            {
                var psr = playerStats?.GetComponent<SpriteRenderer>();
                if (psr != null && psr.color != Color.white) psr.color = Color.white;
            }

            yield return null;
        }

        // Runas aparecem uma a uma com flash
        var runas = new List<SpriteRenderer>();
        foreach (var sr in srs)
            if (sr.gameObject.name.StartsWith("Runa") && !sr.gameObject.name.EndsWith("Glow")) runas.Add(sr);

        for (int i = 0; i < runas.Count; i++)
        {
            runas[i].color = new Color(1f, 1f, 1f, 1f);
            SpawnFlashRuna(runas[i].transform.position);
            yield return new WaitForSeconds(0.1f);
        }
    }

    // ─── ANIMAÇÃO CONTÍNUA ────────────────────────────────────────────────────

    void AnimarRitual(GameObject root, float elapsed)
    {
        float t     = Mathf.Clamp01(elapsed / duracao);
        float pulso = Mathf.Sin(elapsed * (4f + t * 8f)) * 0.5f + 0.5f;
        float rot   = elapsed * (25f + t * 35f);

        Color corBase     = Color.Lerp(COR_INICIO, COR_FINAL, t);
        Color corPentagrama = Color.Lerp(COR_OURO, COR_FINAL, t * 0.5f);

        var lrs = root.GetComponentsInChildren<LineRenderer>();
        int lrIdx = 0;
        foreach (var lr in lrs)
        {
            bool isPentagrama = lr.gameObject.name == "Pentagrama";
            Color c = isPentagrama ? corPentagrama : corBase;
            c.a = 0.6f + pulso * 0.4f;
            lr.startColor = lr.endColor = c;
            float larg = isPentagrama
                ? 0.06f + pulso * 0.05f + t * 0.05f
                : 0.10f + pulso * 0.08f + t * 0.07f;
            lr.startWidth = lr.endWidth = larg;
            lrIdx++;
        }

        // Anel externo gira
        var circExt = root.transform.Find("CirculoExt");
        if (circExt != null) circExt.Rotate(0f, 0f, Time.deltaTime * (20f + t * 30f));
        var circInt = root.transform.Find("CirculoInt");
        if (circInt != null) circInt.Rotate(0f, 0f, -Time.deltaTime * (30f + t * 40f));

        // Runas giram e pulsam
        var srs = root.GetComponentsInChildren<SpriteRenderer>();
        for (int i = 0; i < srs.Length; i++)
        {
            Color c = Color.Lerp(COR_ESPIRITO, COR_FINAL, t * 0.7f);
            c.a = 0.7f + pulso * 0.3f;
            srs[i].color = c;
            srs[i].transform.Rotate(0f, 0f, Time.deltaTime * (55f + i * 18f + t * 80f));
            float esc = 0.30f + pulso * 0.08f + t * 0.12f;
            // Glow atrás: apenas o filho "RunaGlow"
            if (srs[i].gameObject.name == "RunaGlow")
            {
                Color gc = new Color(c.r, c.g, c.b, 0.25f + pulso * 0.15f);
                srs[i].color = gc;
                srs[i].transform.localScale = Vector3.one * (2f + pulso * 0.5f);
            }
            else srs[i].transform.localScale = Vector3.one * esc;
        }

        // Glow do chão pulsa
        var glowChao = root.transform.Find("GlowChao");
        if (glowChao != null)
        {
            var gsr = glowChao.GetComponent<SpriteRenderer>();
            if (gsr != null)
            {
                Color gc = Color.Lerp(COR_INICIO, COR_FINAL, t);
                gc.a = 0.04f + pulso * 0.04f + t * 0.04f;
                gsr.color = gc;
            }
        }
    }

    // ─── EFEITOS PARALELOS ────────────────────────────────────────────────────

    IEnumerator EfeitoCarga()
    {
        float dur = 0.5f;
        var sr = playerStats?.GetComponent<SpriteRenderer>();
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            if (sr != null) sr.color = Color.Lerp(Color.white, COR_OURO, Mathf.Sin(p * Mathf.PI));
            // Partículas convergindo
            if (Time.frameCount % 2 == 0) SpawnParticulaCarga((Vector2)transform.position, Mathf.Lerp(5f, 1f, p));
            yield return null;
        }
        if (sr != null) sr.color = Color.white;
    }

    void SpawnParticulaCarga(Vector2 centro, float dist)
    {
        float ang = Random.Range(0f, Mathf.PI * 2f);
        Vector2 pos = centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;
        var go = new GameObject("PCarga");
        go.transform.position = pos;
        var psr = go.AddComponent<SpriteRenderer>();
        psr.sprite = GerarDisco(6);
        psr.color  = COR_OURO; psr.sortingOrder = 15;
        go.transform.localScale = Vector3.one * Random.Range(0.08f, 0.18f);
        go.AddComponent<AutoDestroyFadeMove>().Iniciar((centro - pos).normalized * Random.Range(4f, 9f), 0.25f);
        Destroy(go, 0.5f);
    }

    IEnumerator AuraPlayer()
    {
        // Anel pulsante ao redor do player durante o ritual
        const int S = 32;
        var go = new GameObject("AuraRitual");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = S;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 10;

        while (ativo && playerStats != null)
        {
            float t     = 1f - Mathf.Clamp01(cooldownRestante / cooldown);
            float prog  = Mathf.Clamp01((cooldown - cooldownRestante) / duracao);
            float pulso = Mathf.Sin(Time.time * 6f) * 0.5f + 0.5f;
            float r     = 0.7f + pulso * 0.15f;
            Color cor   = Color.Lerp(COR_INICIO, COR_FINAL, prog);

            lr.startWidth = lr.endWidth = 0.06f + pulso * 0.04f;
            lr.startColor = lr.endColor = new Color(cor.r, cor.g, cor.b, 0.55f + pulso * 0.35f);

            Vector2 playerPos = playerStats.transform.position;
            for (int i = 0; i < S; i++)
            {
                float a = i / (float)S * Mathf.PI * 2f;
                lr.SetPosition(i, playerPos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
            yield return null;
        }
        Destroy(go);
    }

    IEnumerator ParticulasOrbitando(Vector2 centro)
    {
        const int QTD = 8;
        var gos = new GameObject[QTD];
        var srs2 = new SpriteRenderer[QTD];
        for (int i = 0; i < QTD; i++)
        {
            gos[i] = new GameObject($"Espirito{i}");
            srs2[i] = gos[i].AddComponent<SpriteRenderer>();
            srs2[i].sprite = GerarEstrela(16, COR_ESPIRITO);
            srs2[i].sortingOrder = 15;
            gos[i].transform.localScale = Vector3.one * Random.Range(0.12f, 0.22f);
        }

        float ang = 0f;
        while (ativo)
        {
            ang += Time.deltaTime * 55f;
            float prog  = 1f - Mathf.Clamp01(cooldownRestante / cooldown);
            float pulso = Mathf.Sin(Time.time * 3.5f) * 0.5f + 0.5f;

            for (int i = 0; i < QTD; i++)
            {
                if (gos[i] == null) continue;
                float a = (ang + 360f / QTD * i) * Mathf.Deg2Rad;
                float r = raio * (0.65f + Mathf.Sin(Time.time * 2f + i) * 0.2f);
                gos[i].transform.position = centro + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                gos[i].transform.Rotate(0f, 0f, Time.deltaTime * 120f);

                Color c = Color.Lerp(COR_ESPIRITO, COR_FINAL, prog * 0.6f);
                c.a = 0.5f + pulso * 0.4f;
                srs2[i].color = c;
            }
            yield return null;
        }

        foreach (var go in gos)
        {
            if (go == null) continue;
            var s = go.GetComponent<SpriteRenderer>();
            if (s != null) go.AddComponent<AutoDestroyFade>().Iniciar(0.4f);
            Destroy(go, 0.6f);
        }
    }

    IEnumerator EspiritosCentro(Vector2 centro)
    {
        while (ativo)
        {
            yield return new WaitForSeconds(Random.Range(0.3f, 0.7f));
            if (!ativo) yield break;

            // Espírito sobe do centro
            var go = new GameObject("EspiritoSobe");
            go.transform.position = centro + Random.insideUnitCircle * 0.5f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GerarDisco(8);
            Color c = Color.Lerp(COR_ESPIRITO, COR_OURO, Random.value * 0.5f);
            sr.color = c; sr.sortingOrder = 16;
            go.transform.localScale = Vector3.one * Random.Range(0.1f, 0.22f);
            Vector2 vel = new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(1.5f, 3.5f));
            go.AddComponent<AutoDestroyFadeMove>().Iniciar(vel, Random.Range(0.4f, 0.9f));
            Destroy(go, 1.2f);
        }
    }

    IEnumerator PulsosRitmicos(Vector2 centro)
    {
        while (ativo)
        {
            float prog = 1f - Mathf.Clamp01(cooldownRestante / cooldown);
            yield return new WaitForSeconds(Mathf.Lerp(1.8f, 0.6f, prog));
            if (!ativo) yield break;
            StartCoroutine(PulsoExpandindo(centro, Color.Lerp(COR_INICIO, COR_FINAL, prog)));
        }
    }

    IEnumerator PulsoExpandindo(Vector2 centro, Color cor)
    {
        const int S = 48;
        var go = new GameObject("PulsoRitual");
        go.transform.position = centro;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = S;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 13;
        Destroy(go, 1f);

        float dur = 0.5f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p = t / dur;
            float r = Mathf.Lerp(0.3f, raio * 1.3f, p);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.2f, 0.02f, p);
            lr.startColor = lr.endColor = new Color(cor.r, cor.g, cor.b, Mathf.Lerp(0.9f, 0f, p));
            for (int i = 0; i < S; i++)
            {
                float a = i / (float)S * Mathf.PI * 2f;
                lr.SetPosition(i, centro + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    IEnumerator TentaculoAlvo(Vector2 centro, Vector2 alvo)
    {
        const int S = 8;
        var go = new GameObject("Tentaculo");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.positionCount = S;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 13;
        lr.startWidth = 0.08f; lr.endWidth = 0.02f;
        lr.startColor = new Color(COR_FINAL.r, COR_FINAL.g, COR_FINAL.b, 0.85f);
        lr.endColor   = new Color(COR_FINAL.r, COR_FINAL.g, COR_FINAL.b, 0.1f);
        Destroy(go, 0.5f);

        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            if (go == null) yield break;
            for (int i = 0; i < S; i++)
            {
                float p   = i / (float)(S - 1);
                Vector2 p2 = Vector2.Lerp(centro, alvo, p);
                float jitter = Mathf.Sin(t * 20f + i * 1.3f) * (0.3f * (1f - p));
                p2 += Random.insideUnitCircle * jitter;
                lr.SetPosition(i, p2);
            }
            Color c = lr.startColor; c.a = Mathf.Lerp(0.85f, 0f, t / 0.4f);
            lr.startColor = c; Color c2 = lr.endColor; c2.a = c.a * 0.2f; lr.endColor = c2;
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    // ─── EXPLOSÃO FINAL ───────────────────────────────────────────────────────

    IEnumerator AnimarExplosaoFinal(Vector2 centro)
    {
        // Flash branco no player
        var psr = playerStats?.GetComponent<SpriteRenderer>();
        if (psr != null) { psr.color = Color.white; yield return new WaitForSeconds(0.06f); if (psr != null) psr.color = Color.white; }

        // 3 anéis expansivos em sequência
        for (int ring = 0; ring < 3; ring++)
        {
            StartCoroutine(AnelExplosivo(centro, ring * 0.12f,
                ring == 0 ? COR_OURO : ring == 1 ? COR_MEIO : COR_FINAL,
                raio * (1.2f + ring * 0.4f)));
        }

        // Partículas em explosão radial
        for (int i = 0; i < 20; i++)
        {
            float a = i / 20f * Mathf.PI * 2f + Random.Range(-0.15f, 0.15f);
            var go = new GameObject("PartExp");
            go.transform.position = centro;
            var sr = go.AddComponent<SpriteRenderer>();
            bool ouro = i % 4 == 0;
            sr.sprite = ouro ? GerarEstrela(12, COR_OURO) : GerarDisco(8);
            sr.color  = ouro ? COR_OURO : Color.Lerp(COR_MEIO, COR_FINAL, Random.value);
            sr.sortingOrder = 17;
            go.transform.localScale = Vector3.one * Random.Range(0.12f, 0.28f);
            float speed = Random.Range(4f, 9f);
            go.AddComponent<AutoDestroyFadeMove>().Iniciar(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed, Random.Range(0.4f, 0.8f));
            Destroy(go, 1.2f);
        }

        // Símbolo dourado central (flash)
        var flash = new GameObject("FlashFinal");
        flash.transform.position = centro;
        var fsr = flash.AddComponent<SpriteRenderer>();
        fsr.sprite = GerarEstrela(32, COR_OURO); fsr.sortingOrder = 18;
        fsr.color  = new Color(COR_OURO.r, COR_OURO.g, COR_OURO.b, 1f);
        flash.transform.localScale = Vector3.one * 2f;
        flash.AddComponent<AutoDestroyFade>().Iniciar(0.5f);
        Destroy(flash, 0.8f);

        yield return null;
    }

    IEnumerator AnelExplosivo(Vector2 centro, float delay, Color cor, float raioFinal)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        const int S = 48;
        var go = new GameObject("AnelExp");
        go.transform.position = centro;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = S;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 16;
        Destroy(go, 1f);

        float dur = 0.55f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p = Mathf.SmoothStep(0f, 1f, t / dur);
            float r = Mathf.Lerp(0.2f, raioFinal, p);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.4f, 0.02f, p);
            lr.startColor = lr.endColor = new Color(cor.r, cor.g, cor.b, Mathf.Lerp(1f, 0f, p));
            for (int i = 0; i < S; i++)
            {
                float a = i / (float)S * Mathf.PI * 2f;
                lr.SetPosition(i, centro + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    void SpawnFlashRuna(Vector2 pos)
    {
        var go = new GameObject("FlashRuna");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GerarDisco(16); sr.color = Color.white; sr.sortingOrder = 16;
        go.transform.localScale = Vector3.one * 0.5f;
        go.AddComponent<AutoDestroyFade>().Iniciar(0.2f);
        Destroy(go, 0.4f);
    }

    IEnumerator FadeOutDestruir(GameObject go, float dur)
    {
        var lrs = go.GetComponentsInChildren<LineRenderer>();
        var srs = go.GetComponentsInChildren<SpriteRenderer>();
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            foreach (var lr in lrs) { Color c = lr.startColor; c.a = Mathf.Lerp(c.a, 0f, p * 2f); lr.startColor = lr.endColor = c; }
            foreach (var sr in srs) { Color c = sr.color; c.a = Mathf.Lerp(c.a, 0f, p * 2f); sr.color = c; }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    // ─── GEOMETRIA ───────────────────────────────────────────────────────────

    static bool DentroPentagono(Vector2 ponto, Vector2 centro, float r)
    {
        bool dentro = false; int j = 4;
        for (int i = 0; i < 5; i++)
        {
            float a1 = (72f * i - 90f) * Mathf.Deg2Rad;
            float a2 = (72f * j - 90f) * Mathf.Deg2Rad;
            Vector2 vi = centro + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * r;
            Vector2 vj = centro + new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)) * r;
            if ((vi.y > ponto.y) != (vj.y > ponto.y) &&
                ponto.x < (vj.x - vi.x) * (ponto.y - vi.y) / (vj.y - vi.y) + vi.x)
                dentro = !dentro;
            j = i;
        }
        return dentro;
    }

    static GameObject ResolverInimigo(GameObject go)
    {
        if (go.GetComponentInParent<ProjetilHomingPrincesa>(true)   != null) return null;
        if (go.GetComponentInParent<ProjetilEspecialPrincesa>(true) != null) return null;
        var ic = go.GetComponent<InimigoController>() ?? go.GetComponentInParent<InimigoController>();
        if (ic != null) return ic.gameObject;
        var mi = go.GetComponent<movi_inimigo>() ?? go.GetComponentInParent<movi_inimigo>();
        if (mi != null) return mi.gameObject;
        var bc = go.GetComponent<BossController>() ?? go.GetComponentInParent<BossController>();
        if (bc != null) return bc.gameObject;
        var bp = go.GetComponent<BossPrincesa>() ?? go.GetComponentInParent<BossPrincesa>();
        return bp != null ? bp.gameObject : null;
    }

    // ─── SPRITES PROCEDURAIS ─────────────────────────────────────────────────

    static Sprite GerarDisco(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear; float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++)
        { float d = Vector2.Distance(new Vector2(x+0.5f,y+0.5f),new Vector2(cx,cx)); tex.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(1f-d/cx))); }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,sz,sz), new Vector2(0.5f,0.5f), sz);
    }

    static Sprite GerarEstrela(int sz, Color cor)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color[sz * sz];
        float cx = sz * 0.5f;
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        int pts = 5;
        for (int s = 0; s < pts; s++)
        {
            float a1 = s / (float)pts * Mathf.PI * 2f - Mathf.PI * 0.5f;
            float a2 = (s + 0.5f) / (float)pts * Mathf.PI * 2f - Mathf.PI * 0.5f;
            float a3 = (s + 1f)   / (float)pts * Mathf.PI * 2f - Mathf.PI * 0.5f;
            Vector2 ext1 = new Vector2(cx + Mathf.Cos(a1) * (cx-1), cx + Mathf.Sin(a1) * (cx-1));
            Vector2 intr = new Vector2(cx + Mathf.Cos(a2) * cx*0.42f, cx + Mathf.Sin(a2) * cx*0.42f);
            Vector2 ext2 = new Vector2(cx + Mathf.Cos(a3) * (cx-1), cx + Mathf.Sin(a3) * (cx-1));
            DesenharLinhaPixel(pixels, sz, ext1, intr, cor, 1.8f);
            DesenharLinhaPixel(pixels, sz, intr, ext2, cor, 1.8f);
        }
        // Núcleo brilhante
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++)
        { float d = Vector2.Distance(new Vector2(x,y), new Vector2(cx,cx)); float a = Mathf.Clamp01(1f-d/(cx*0.35f)); if (a>0) pixels[y*sz+x] = Color.Lerp(pixels[y*sz+x], Color.white, a); }

        tex.SetPixels(pixels); tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,sz,sz), new Vector2(0.5f,0.5f), sz);
    }

    static void DesenharLinhaPixel(Color[] p, int sz, Vector2 de, Vector2 ate, Color cor, float esp)
    {
        int steps = Mathf.Max(1, (int)(Vector2.Distance(de, ate) * 2f));
        for (int i = 0; i <= steps; i++)
        {
            Vector2 pt = Vector2.Lerp(de, ate, i / (float)steps);
            int r = Mathf.CeilToInt(esp);
            for (int dy = -r; dy <= r; dy++) for (int dx = -r; dx <= r; dx++)
            {
                int px = (int)pt.x + dx, py = (int)pt.y + dy;
                if (px < 0 || px >= sz || py < 0 || py >= sz) continue;
                float f = Mathf.Clamp01(esp - Mathf.Sqrt(dx*dx+dy*dy));
                p[py*sz+px] = Color.Lerp(p[py*sz+px], cor, f);
            }
        }
    }
}
