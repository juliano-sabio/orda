using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlimeElemental : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidade          = 3f;
    public float velocidadeFuga      = 5f;
    public float distanciaDesejada   = 9f;
    public float toleranciaDistancia = 0.8f;
    public float distanciaFuga       = 7f;
    public float suavizacao          = 12f;
    public float forcaOrbital        = 1.4f;
    public float raioAtaque          = 14f;

    [Header("Dano por Contato")]
    public float danoContato      = 18f;
    public float intervaloContato = 1.2f;

    [Header("Gelo")]
    public float danoGelo      = 28f;
    public float duracaoGelo   = 4f;
    public float raioGelo      = 5f;
    public float fatorLentidao = 0.5f;
    public float cooldownGelo  = 16f;

    [Header("Vento")]
    public float forcaVento    = 10f;
    public float duracaoVento  = 2.5f;
    public float raioVento     = 7f;
    public float cooldownVento = 14f;

    [Header("Fogo")]
    public float danoFogo     = 32f;
    public float raioExplosao = 5f;
    public float cooldownFogo = 18f;

    Rigidbody2D       rb;
    SpriteRenderer    sr;
    InimigoController inimigoCtrl;
    PlayerStats       player;
    Rigidbody2D       playerRb;

    float proxGelo, proxVento, proxFogo;
    float proxAtaque;
    float proxDanoContato;
    bool  atacando;

    // rastreamento dos GOs criados em EfeitoCarga/Vortice para cleanup na morte
    GameObject      cargaAnelExt;
    GameObject      cargaAnelInt;
    GameObject[]    cargaOrbs;
    GameObject      vorticeRoot;

    // movimento manter-distância
    Vector2 direcaoMovimento;
    Vector2 ultimaDirFuga = Vector2.down;
    bool    estaMuitoPerto;
    float   tempoUltimaFuga;

    // ─── Init ──────────────────────────────────────────────────────────────

    void Start()
    {
        rb          = GetComponent<Rigidbody2D>();
        sr          = GetComponent<SpriteRenderer>();
        inimigoCtrl = GetComponent<InimigoController>();
        BuscarPlayer();
        proxGelo  = Random.Range(4f, 8f);
        proxVento = Random.Range(5f, 9f);
        proxFogo  = Random.Range(7f, 12f);
        InimigoController.OnPreMorte += OnPreMorteHandler;
    }

    void OnDestroy()
    {
        InimigoController.OnPreMorte -= OnPreMorteHandler;
    }

    void OnPreMorteHandler(InimigoController ic)
    {
        if (ic != inimigoCtrl) return;
        LimparEfeitoCarga();
        CriarCacosGelo(transform.position);
        // Co-op: replica os cacos de gelo da morte pro P2.
        GetComponent<EnemyNet>()?.BroadcastCosmetico(MobCosmeticos.CacosGelo, transform.position);
    }

    void LimparEfeitoCarga()
    {
        if (cargaAnelExt != null) { Destroy(cargaAnelExt); cargaAnelExt = null; }
        if (cargaAnelInt != null) { Destroy(cargaAnelInt); cargaAnelInt = null; }
        if (cargaOrbs    != null)
        {
            foreach (var o in cargaOrbs) if (o != null) Destroy(o);
            cargaOrbs = null;
        }
        if (vorticeRoot  != null) { Destroy(vorticeRoot);  vorticeRoot  = null; }
    }

    public static void CriarCacosGelo(Vector3 pos)
    {
        int num = 14;
        for (int i = 0; i < num; i++)
        {
            var go  = new GameObject("CacoGelo");
            go.transform.position = pos;
            var sr2 = go.AddComponent<SpriteRenderer>();
            int sz  = Random.Range(12, 22);
            sr2.sprite       = GerarCristal(sz, new Color(0.55f + Random.Range(0f, 0.2f), 0.85f, 1f));
            sr2.sortingOrder = 13;
            float escala = Random.Range(0.5f, 1.4f);
            go.transform.localScale = Vector3.one * escala;

            float ang   = (360f / num * i + Random.Range(-20f, 20f)) * Mathf.Deg2Rad;
            float speed = Random.Range(4f, 10f);
            var vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * speed;

            var fx = go.AddComponent<CacoGeloFX>();
            fx.vel = vel;
        }
    }

    void BuscarPlayer()
    {
        player = PlayerStats.MaisProximo(transform.position);
        if (player != null) playerRb = player.GetComponent<Rigidbody2D>();
    }

    // ─── Loop ──────────────────────────────────────────────────────────────

    void Update()
    {
        if (Morto()) return;
        if (!PlayerStats.AlvoValido(player)) { BuscarPlayer(); if (!PlayerStats.AlvoValido(player)) return; }

        proxGelo   -= Time.deltaTime;
        proxVento  -= Time.deltaTime;
        proxFogo   -= Time.deltaTime;
        proxAtaque -= Time.deltaTime;

        float dist = Vector2.Distance(transform.position, player.transform.position);

        // calcula direção desejada (manter distância / fuga / orbitar)
        if (!atacando)
        {
            Vector2 direcaoDesejada = CalcularDirecao(dist);
            // fuga imediata: sem lerp para não girar após knockback do player
            if (estaMuitoPerto)
                direcaoMovimento = direcaoDesejada;
            else
                direcaoMovimento = Vector2.Lerp(direcaoMovimento, direcaoDesejada, Time.deltaTime * suavizacao);
        }

        if (!atacando && dist <= raioAtaque) TentarAtacar();
    }

    Vector2 CalcularDirecao(float dist)
    {
        Vector2 dirParaPlayer = dist > 0.4f
            ? ((Vector2)player.transform.position - (Vector2)transform.position) / dist
            : -ultimaDirFuga; // usa cached quando colado

        // só atualiza cache quando distância é estável
        if (dist > 0.4f)
            ultimaDirFuga = -dirParaPlayer;

        bool eraProximo = estaMuitoPerto;
        estaMuitoPerto = dist < distanciaFuga;

        if (estaMuitoPerto && !eraProximo)
            tempoUltimaFuga = Time.time;

        // fuga: usa direção cacheada — estável mesmo com dist ≈ 0
        if (estaMuitoPerto)
            return ultimaDirFuga;

        // muito longe: aproxima pelo FlowField
        if (dist > distanciaDesejada + toleranciaDistancia)
            return FlowField.Instance != null
                ? FlowField.Instance.ObterDirecao(transform.position)
                : dirParaPlayer;

        // muito perto (mas acima de distanciaFuga): recua
        if (dist < distanciaDesejada - toleranciaDistancia)
            return -dirParaPlayer;

        // na distância ideal: oscila levemente para os lados sem girar em círculo
        Vector2 lateral = new Vector2(-dirParaPlayer.y, dirParaPlayer.x);
        float oscilacao = Mathf.Sin(Time.time * 0.75f);
        // vetor NÃO normalizado — magnitude proporcional à oscilação (para quando cruza zero)
        return lateral * (oscilacao * forcaOrbital * 0.45f);
    }

    void FixedUpdate()
    {
        if (player == null || Morto()) { rb.linearVelocity = Vector2.zero; return; }
        if (atacando) { rb.linearVelocity = Vector2.zero; return; }

        float vel = estaMuitoPerto ? velocidadeFuga : velocidade;
        if (direcaoMovimento.sqrMagnitude > 0.001f)
        {
            // usa magnitude do vetor como fator de velocidade (0-1) para suavizar paradas
            float fator = Mathf.Clamp01(direcaoMovimento.magnitude);
            rb.linearVelocity = direcaoMovimento.normalized * vel * fator;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (sr != null && rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            if      (rb.linearVelocity.x >  0.05f) sr.flipX = false;
            else if (rb.linearVelocity.x < -0.05f) sr.flipX = true;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (Morto() || !other.CompareTag("Player")) return;
        var ps = other.GetComponent<PlayerStats>();
        if (ps != null && Time.time >= proxDanoContato)
        {
            ps.TakeDamage(danoContato);
            proxDanoContato = Time.time + intervaloContato;
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (Morto() || !other.CompareTag("Player") || Time.time < proxDanoContato) return;
        var ps = other.GetComponent<PlayerStats>();
        if (ps != null)
        {
            ps.TakeDamage(danoContato);
            proxDanoContato = Time.time + intervaloContato;
        }
    }

    bool Morto() => inimigoCtrl != null && inimigoCtrl.estaMorrendo;

    void TentarAtacar()
    {
        if (proxAtaque > 0f) return;

        var disp = new List<int>();
        if (proxGelo  <= 0f) disp.Add(0);
        if (proxVento <= 0f) disp.Add(1);
        if (proxFogo  <= 0f) disp.Add(2);
        if (disp.Count == 0) return;

        int tipo = disp[Random.Range(0, disp.Count)];
        switch (tipo)
        {
            case 0: proxGelo  = cooldownGelo;  proxAtaque = cooldownGelo;  StartCoroutine(AtaqueGelo());  break;
            case 1: proxVento = cooldownVento; proxAtaque = cooldownVento; StartCoroutine(AtaqueVento()); break;
            case 2: proxFogo  = cooldownFogo;  proxAtaque = cooldownFogo;  StartCoroutine(AtaqueFogo());  break;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ATAQUE GELO
    // ═══════════════════════════════════════════════════════════════════════

    // ── Entradas cosméticas (co-op, cliente): mesmo VFX, sem dano ────────────
    public void CosmeticoCarga(Color cor, float duracao) => StartCoroutine(EfeitoCarga(cor, duracao));
    public void CosmeticoZonaGelo(Vector2 centro) => StartCoroutine(ZonaGelo(centro, comDano: false));
    public void CosmeticoVortice(Vector2 centro)  => StartCoroutine(Vortice(centro, comDano: false));
    public void CosmeticoMeteoro(Vector2 destino) => StartCoroutine(Meteoro(destino, comDano: false));

    IEnumerator AtaqueGelo()
    {
        atacando = true;
        rb.linearVelocity = Vector2.zero;
        BroadcastCarga(new Color(0.45f, 0.8f, 1f), 1.1f);
        yield return StartCoroutine(EfeitoCarga(new Color(0.45f, 0.8f, 1f), 1.1f));

        Vector2 pos = player != null ? (Vector2)player.transform.position : (Vector2)transform.position;
        StartCoroutine(ZonaGelo(pos, comDano: true));
        atacando = false;
    }

    void BroadcastCarga(Color cor, float duracao)
    {
        GetComponent<EnemyNet>()?.BroadcastCosmetico(MobCosmeticos.CargaElemental,
            transform.position, cor.r, cor.g, cor.b, duracao);
    }

    IEnumerator ZonaGelo(Vector2 centro, bool comDano)
    {
        SomSkill.Tocar(SomSkill.Tipo.SlimeElemGelo, centro, 0.55f);

        var root = new GameObject("ZonaGelo");
        root.transform.position = centro;
        Destroy(root, duracaoGelo + 2f); // failsafe

        if (comDano)
        {
            // Controller on root handles damage & slow independently of this slime's lifetime
            var ctrl = root.AddComponent<ZonaGeloController>();
            ctrl.Iniciar(player, centro, raioGelo, duracaoGelo, danoGelo, fatorLentidao);

            // Co-op: replica o VFX completo da zona pro P2.
            GetComponent<EnemyNet>()?.BroadcastCosmetico(MobCosmeticos.ZonaGeloAoe, centro, raioGelo, duracaoGelo);
        }

        var anelExt = CriarAnel(root, raioGelo,         new Color(0.4f, 0.8f, 1f, 0.8f),  0.14f);
        var anelMed = CriarAnel(root, raioGelo * 0.65f, new Color(0.6f, 0.9f, 1f, 0.5f),  0.07f);
        var fill    = CriarSpriteFilho(root, GerarDisco(64, new Color(0.35f, 0.7f, 1f, 0.12f)), 6, raioGelo * 2f);

        var cristais = new Transform[8];
        for (int i = 0; i < 8; i++)
        {
            var c  = new GameObject("Cristal");
            c.transform.SetParent(root.transform, false);
            var csr        = c.AddComponent<SpriteRenderer>();
            csr.sprite     = GerarCristal(14, new Color(0.6f, 0.9f, 1f));
            csr.sortingOrder = 11;
            c.transform.localScale = Vector3.one * 0.55f;
            cristais[i] = c.transform;
        }

        StartCoroutine(NevoaGelo(root.transform, centro, raioGelo));

        for (float t = 0f; t < 0.35f; t += Time.deltaTime)
        {
            if (root == null) yield break;
            AlphaLR(anelExt, t / 0.35f * 0.8f);
            AlphaLR(anelMed, t / 0.35f * 0.5f);
            yield return null;
        }

        // Visual loop only — damage handled by ZonaGeloController
        float elapsed     = 0f;
        float rotCristais = 0f;

        while (elapsed < duracaoGelo)
        {
            if (root == null) yield break;
            elapsed     += Time.deltaTime;
            rotCristais += Time.deltaTime * 28f;

            for (int i = 0; i < cristais.Length; i++)
            {
                if (cristais[i] == null) continue;
                float ang = (360f / cristais.Length * i + rotCristais) * Mathf.Deg2Rad;
                cristais[i].localPosition = new Vector3(Mathf.Cos(ang) * raioGelo, Mathf.Sin(ang) * raioGelo);
                cristais[i].rotation      = Quaternion.Euler(0f, 0f, rotCristais * 1.5f + i * 45f);
            }

            float p = Mathf.Sin(elapsed * 3f) * 0.5f + 0.5f;
            AlphaLR(anelExt, 0.5f + p * 0.3f);
            AlphaLR(anelMed, 0.3f + p * 0.2f);
            if (fill != null) fill.localScale = Vector3.one * (raioGelo * 2f + p * 0.15f);
            yield return null;
        }

        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            if (root == null) yield break;
            float p = t / 0.5f;
            AlphaLR(anelExt, Mathf.Lerp(0.8f, 0f, p));
            AlphaLR(anelMed, Mathf.Lerp(0.5f, 0f, p));
            if (fill != null)
            {
                var fc = fill.GetComponent<SpriteRenderer>().color;
                fc.a = Mathf.Lerp(0.12f, 0f, p);
                fill.GetComponent<SpriteRenderer>().color = fc;
            }
            foreach (var c in cristais)
                if (c != null) { var csr = c.GetComponent<SpriteRenderer>(); var cc = csr.color; cc.a = 1f - p; csr.color = cc; }
            yield return null;
        }

        if (root != null) Destroy(root);
    }

    IEnumerator NevoaGelo(Transform parent, Vector2 centro, float raio)
    {
        // Lança flocos continuamente enquanto o parent existir
        while (parent != null)
        {
            StartCoroutine(Floco(centro, raio));
            yield return new WaitForSeconds(0.18f);
        }
    }

    IEnumerator Floco(Vector2 centro, float raio)
    {
        var go  = new GameObject("Floco");
        var sr2 = go.AddComponent<SpriteRenderer>();
        sr2.sprite       = GerarSprite(6, new Color(0.8f, 0.95f, 1f));
        sr2.sortingOrder = 9;
        float sz = Random.Range(0.12f, 0.32f);
        go.transform.localScale = Vector3.one * sz;

        Vector2 pos  = centro + Random.insideUnitCircle * raio * 0.85f;
        Vector2 vel  = Vector2.up * Random.Range(0.4f, 1f) + Random.insideUnitCircle * 0.3f;
        float   vida = Random.Range(1f, 2.2f);

        for (float t = 0f; t < vida; t += Time.deltaTime)
        {
            if (go == null) yield break;
            pos += vel * Time.deltaTime;
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            float a = t < 0.2f ? t / 0.2f : Mathf.Lerp(1f, 0f, (t - 0.2f) / (vida - 0.2f));
            sr2.color = new Color(0.8f, 0.95f, 1f, a);
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    IEnumerator BurstGelo(Vector2 pos)
    {
        for (int i = 0; i < 8; i++)
        {
            var go  = new GameObject("IceBurst");
            var sr2 = go.AddComponent<SpriteRenderer>();
            sr2.sprite       = GerarCristal(10, new Color(0.7f, 0.95f, 1f));
            sr2.sortingOrder = 14;
            go.transform.position   = pos;
            go.transform.localScale = Vector3.one * 0.5f;

            float ang = i * 45f * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            StartCoroutine(LancarParticula(go, dir, Random.Range(2f, 3.5f), 0.4f,
                new Color(0.7f, 0.95f, 1f)));
        }
        yield break;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ATAQUE VENTO
    // ═══════════════════════════════════════════════════════════════════════

    IEnumerator AtaqueVento()
    {
        atacando = true;
        rb.linearVelocity = Vector2.zero;
        BroadcastCarga(new Color(0.6f, 1f, 0.55f), 0.9f);
        yield return StartCoroutine(EfeitoCarga(new Color(0.6f, 1f, 0.55f), 0.9f));

        Vector2 pos = player != null
            ? Vector2.Lerp(transform.position, player.transform.position, 0.55f)
            : (Vector2)transform.position;

        StartCoroutine(Vortice(pos, comDano: true));
        atacando = false;
    }

    IEnumerator Vortice(Vector2 centro, bool comDano)
    {
        SomSkill.Tocar(SomSkill.Tipo.SlimeElemVento, centro, 0.55f);

        // Co-op: replica o VFX completo do vórtice pro P2.
        if (comDano)
            GetComponent<EnemyNet>()?.BroadcastCosmetico(MobCosmeticos.VorticeAoe, centro, raioVento, duracaoVento);

        vorticeRoot = new GameObject("Vortice");
        var root = vorticeRoot;
        root.transform.position = centro;

        var lr1 = CriarAnel(root, raioVento,        new Color(0.5f, 1f, 0.6f, 0.8f),  0.10f);
        var lr2 = CriarAnel(root, raioVento * 0.65f, new Color(0.4f, 0.9f, 0.5f, 0.6f), 0.07f);
        var lr3 = CriarAnel(root, raioVento * 0.3f, new Color(0.65f, 1f, 0.7f, 0.5f), 0.05f);

        // 14 detritos orbitando em espiral
        var detritos = new GameObject[14];
        for (int i = 0; i < detritos.Length; i++)
        {
            var d   = new GameObject("Detrito");
            d.transform.SetParent(root.transform, false);
            var dsr = d.AddComponent<SpriteRenderer>();
            dsr.sprite       = GerarSprite(Random.Range(5, 10), new Color(0.5f, 1f, 0.6f));
            dsr.sortingOrder = 12;
            d.transform.localScale = Vector3.one * Random.Range(0.1f, 0.22f);
            detritos[i] = d;
        }

        // Linhas de rajada saindo do centro e some
        StartCoroutine(RajadasVento(centro, raioVento, duracaoVento));

        float elapsed = 0f;
        float[] fases = new float[detritos.Length];
        for (int i = 0; i < fases.Length; i++) fases[i] = i * (360f / fases.Length);

        while (elapsed < duracaoVento)
        {
            elapsed += Time.deltaTime;
            root.transform.rotation = Quaternion.Euler(0f, 0f, elapsed * 100f);

            // Detritos em espiral com raio oscilante
            for (int i = 0; i < detritos.Length; i++)
            {
                if (detritos[i] == null) continue;
                fases[i] += Time.deltaTime * (180f + i * 10f);
                float r   = raioVento * (0.3f + 0.7f * Mathf.Abs(Mathf.Sin(elapsed * 0.7f + i)));
                float ang = fases[i] * Mathf.Deg2Rad;
                detritos[i].transform.localPosition = new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r);
                detritos[i].transform.rotation      = Quaternion.Euler(0f, 0f, fases[i] * 1.2f);
            }

            if (comDano && player != null && playerRb != null)
            {
                float dist = Vector2.Distance(player.transform.position, centro);
                if (dist <= raioVento * 1.6f && dist > 0.3f)
                {
                    Vector2 dir = (centro - (Vector2)player.transform.position).normalized;
                    playerRb.AddForce(dir * forcaVento, ForceMode2D.Force);
                }
            }

            float p = Mathf.Sin(elapsed * 4f) * 0.5f + 0.5f;
            AlphaLR(lr1, 0.5f + p * 0.3f);
            AlphaLR(lr2, 0.4f + p * 0.2f);
            AlphaLR(lr3, 0.3f + p * 0.2f);
            yield return null;
        }

        for (float t = 0f; t < 0.35f; t += Time.deltaTime)
        {
            float p = t / 0.35f;
            AlphaLR(lr1, Mathf.Lerp(0.8f, 0f, p));
            AlphaLR(lr2, Mathf.Lerp(0.6f, 0f, p));
            AlphaLR(lr3, Mathf.Lerp(0.5f, 0f, p));
            foreach (var d in detritos)
            {
                if (d == null) continue;
                var dsr  = d.GetComponent<SpriteRenderer>();
                var dcor = dsr.color; dcor.a = 1f - p; dsr.color = dcor;
            }
            yield return null;
        }
        vorticeRoot = null;
        Destroy(root);
    }

    IEnumerator RajadasVento(Vector2 centro, float raio, float durTotal)
    {
        float t = 0f;
        while (t < durTotal)
        {
            t += Time.deltaTime;
            if (Random.value < 0.06f)
                SpawnRajada(centro, raio);
            yield return null;
        }
    }

    // Criação inline — a animação roda no próprio GO via RajadaFX
    void SpawnRajada(Vector2 centro, float raio)
    {
        float ang    = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector2 dir  = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
        Vector2 orig = centro + dir * Random.Range(0.3f, raio * 0.5f);
        Vector2 fim  = centro + dir * raio;

        var go = new GameObject("Rajada");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 13;
        lr.SetPosition(0, orig);
        lr.SetPosition(1, fim);
        go.AddComponent<RajadaFX>();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ATAQUE FOGO
    // ═══════════════════════════════════════════════════════════════════════

    IEnumerator AtaqueFogo()
    {
        atacando = true;
        rb.linearVelocity = Vector2.zero;
        BroadcastCarga(new Color(1f, 0.42f, 0.05f), 1.3f);
        yield return StartCoroutine(EfeitoCarga(new Color(1f, 0.42f, 0.05f), 1.3f));

        Vector2 alvo = player != null ? (Vector2)player.transform.position : (Vector2)transform.position;
        StartCoroutine(Meteoro(alvo, comDano: true));
        atacando = false;
    }

    IEnumerator Meteoro(Vector2 destino, bool comDano)
    {
        // Co-op: replica o VFX completo do meteoro (marcador + queda + explosão) pro P2.
        if (comDano)
            GetComponent<EnemyNet>()?.BroadcastCosmetico(MobCosmeticos.MeteoroAoe, destino, raioExplosao, 1.2f);

        // Marcador de alvo — dois anéis pulsantes
        var marcGO  = new GameObject("MarcadorFogo");
        marcGO.transform.position = destino;
        Destroy(marcGO, 3f); // failsafe
        var lrM1 = CriarAnel(marcGO, raioExplosao,        new Color(1f, 0.3f, 0f, 0.9f), 0.10f);
        var lrM2 = CriarAnel(marcGO, raioExplosao * 0.5f, new Color(1f, 0.6f, 0.1f, 0.7f), 0.06f);

        // Cruz de mira no centro
        StartCoroutine(MiraCruz(destino));

        // Corpo do meteoro
        Vector2 origem = destino + new Vector2(Random.Range(-1.2f, 1.2f), 10f);
        var goM  = new GameObject("Meteoro");
        Destroy(goM, 2f); // failsafe
        var srM  = goM.AddComponent<SpriteRenderer>();
        srM.sprite       = GerarSprite(22, new Color(1f, 0.35f, 0.02f));
        srM.sortingOrder = 20;
        goM.transform.localScale = Vector3.one * 1.6f;

        float ang     = Mathf.Atan2(destino.y - origem.y, destino.x - origem.x) * Mathf.Rad2Deg;
        float durQueda = 0.6f;

        // Posições para rastro
        var rastroPos = new Queue<Vector2>();

        for (float t = 0f; t < durQueda; t += Time.deltaTime)
        {
            float p = t / durQueda;
            Vector2 posAtual = Vector2.Lerp(origem, destino, p);
            goM.transform.position = new Vector3(posAtual.x, posAtual.y, 0f);
            goM.transform.rotation = Quaternion.Euler(0f, 0f, ang - 90f);

            // Rastro de fogo a cada frame
            if (rastroPos.Count == 0 || Vector2.Distance(posAtual, rastroPos.Count > 0 ? rastroPos.Peek() : posAtual) > 0.2f)
            {
                rastroPos.Enqueue(posAtual);
                StartCoroutine(ParticulaRastro(posAtual));
            }

            AlphaLR(lrM1, 0.5f + Mathf.Sin(t * 22f) * 0.5f);
            AlphaLR(lrM2, 0.4f + Mathf.Sin(t * 22f + 1f) * 0.4f);
            yield return null;
        }

        Destroy(goM);
        Destroy(marcGO);

        if (comDano && player != null && Vector2.Distance(player.transform.position, destino) <= raioExplosao)
            player.TakeDamage(danoFogo);

        StartCoroutine(ExplosaoFogo(destino));
    }

    IEnumerator MiraCruz(Vector2 pos)
    {
        float sz  = 0.6f;
        var linhas = new LineRenderer[4];
        Vector2[] dirs = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        for (int i = 0; i < 4; i++)
        {
            var go = new GameObject("Cruz");
            Destroy(go, 1.5f); // failsafe
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.material      = new Material(Shader.Find("Sprites/Default"));
            lr.sortingOrder  = 16;
            lr.SetPosition(0, (Vector3)pos + (Vector3)(dirs[i] * 0.15f));
            lr.SetPosition(1, (Vector3)pos + (Vector3)(dirs[i] * sz));
            linhas[i] = lr;
        }

        for (float t = 0f; t < 0.65f; t += Time.deltaTime)
        {
            float p = Mathf.Sin(t / 0.65f * Mathf.PI);
            foreach (var lr in linhas)
            {
                lr.startWidth = lr.endWidth = 0.07f * p;
                lr.startColor = lr.endColor = new Color(1f, 0.5f, 0f, p);
            }
            yield return null;
        }
        foreach (var lr in linhas) if (lr != null) Destroy(lr.gameObject);
    }

    IEnumerator ParticulaRastro(Vector2 pos)
    {
        var go  = new GameObject("Rastro");
        var sr2 = go.AddComponent<SpriteRenderer>();
        sr2.sprite       = GerarSprite(10, new Color(1f, Random.Range(0.3f, 0.6f), 0f));
        sr2.sortingOrder = 18;
        float sz = Random.Range(0.3f, 0.7f);
        go.transform.localScale = Vector3.one * sz;
        go.transform.position   = new Vector3(pos.x + Random.Range(-0.15f, 0.15f), pos.y + Random.Range(-0.15f, 0.15f));

        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            if (go == null) yield break;
            go.transform.localScale = Vector3.one * sz * (1f - t / 0.4f);
            sr2.color = new Color(1f, 0.4f, 0f, 1f - t / 0.4f);
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    IEnumerator ExplosaoFogo(Vector2 pos)
    {
        SomSkill.Tocar(SomSkill.Tipo.SlimeElemFogo, pos, 0.6f);

        // 3 anéis expandindo em velocidades diferentes
        for (int r = 0; r < 3; r++)
        {
            float delay = r * 0.06f;
            float raioFinal = raioExplosao * (0.6f + r * 0.4f);
            StartCoroutine(AnelExpansao(pos, raioFinal, delay, new Color(1f, 0.5f - r * 0.15f, 0f)));
        }

        // 10 brasas voando para fora
        for (int i = 0; i < 10; i++)
        {
            float ang = i * 36f * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            var go  = new GameObject("Brasa");
            var sr2 = go.AddComponent<SpriteRenderer>();
            sr2.sprite       = GerarSprite(8, new Color(1f, Random.Range(0.3f, 0.7f), 0f));
            sr2.sortingOrder = 19;
            go.transform.position   = pos;
            go.transform.localScale = Vector3.one * Random.Range(0.2f, 0.45f);
            StartCoroutine(LancarParticula(go, dir, Random.Range(3f, 6f), Random.Range(0.5f, 0.9f),
                new Color(1f, 0.4f, 0f)));
        }

        // Flash central
        yield return StartCoroutine(FlashCentral(pos, new Color(1f, 0.7f, 0.2f), 1.6f, 0.3f));
    }

    IEnumerator AnelExpansao(Vector2 pos, float raioFinal, float delay, Color cor)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        const int SEGS = 24;
        var go = new GameObject("AnelExp");
        go.transform.position = pos;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 15;

        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            float p = t / 0.5f;
            float r = Mathf.Lerp(0f, raioFinal, p);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.38f, 0.03f, p);
            lr.startColor = lr.endColor = new Color(cor.r, cor.g, cor.b, 1f - p);
            for (int i = 0; i < SEGS; i++)
            {
                float a = (360f / SEGS) * i * Mathf.Deg2Rad;
                lr.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
            yield return null;
        }
        Destroy(go);
    }

    IEnumerator FlashCentral(Vector2 pos, Color cor, float raio, float dur)
    {
        var go  = new GameObject("Flash");
        var sr2 = go.AddComponent<SpriteRenderer>();
        sr2.sprite       = GerarDisco(64, cor);
        sr2.sortingOrder = 17;
        go.transform.position   = pos;
        go.transform.localScale = Vector3.one * raio;

        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            float sc = Mathf.Lerp(raio, raio * 0.2f, p);
            go.transform.localScale = Vector3.one * sc;
            var c = sr2.color; c.a = 1f - p; sr2.color = c;
            yield return null;
        }
        Destroy(go);
    }

    // ─── Helpers Gerais ────────────────────────────────────────────────────

    IEnumerator LancarParticula(GameObject go, Vector2 dir, float velocidadeP, float vida, Color cor)
    {
        float elapsed = 0f;
        Vector2 pos   = go.transform.position;
        var sr2 = go.GetComponent<SpriteRenderer>();
        while (elapsed < vida && go != null)
        {
            elapsed    += Time.deltaTime;
            pos        += dir * velocidadeP * Time.deltaTime;
            velocidadeP *= 0.92f; // desacelera
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            if (sr2 != null) sr2.color = new Color(cor.r, cor.g, cor.b, 1f - elapsed / vida);
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    // ─── Efeito de Carga Elemental ─────────────────────────────────────────

    IEnumerator EfeitoCarga(Color cor, float duracao)
    {
        SomSkill.Tocar(SomSkill.Tipo.SlimeElemCarga, transform.position, 0.45f);

        // Anel pulsante ao redor da slime — raio 2.0 para ficar fora do corpo (escala 6)
        cargaAnelExt = new GameObject("AnelCarga");
        cargaAnelExt.transform.position = transform.position;
        var lrCarga = CriarAnel(cargaAnelExt, 2.0f, new Color(cor.r, cor.g, cor.b, 0f), 0.18f);

        // segundo anel interno mais sutil
        cargaAnelInt = new GameObject("AnelCargaInt");
        cargaAnelInt.transform.position = transform.position;
        var lrIn = CriarAnel(cargaAnelInt, 1.4f, new Color(cor.r, cor.g, cor.b, 0f), 0.10f);

        // 6 orbs que orbitam e spiralam em direção ao corpo
        const int NUM_ORBS = 6;
        cargaOrbs = new GameObject[NUM_ORBS];
        var orbSRs = new SpriteRenderer[NUM_ORBS];
        for (int i = 0; i < NUM_ORBS; i++)
        {
            cargaOrbs[i] = new GameObject("OrbCarga");
            orbSRs[i] = cargaOrbs[i].AddComponent<SpriteRenderer>();
            orbSRs[i].sprite       = GerarSprite(16, cor);
            orbSRs[i].sortingOrder = 16;
            float sz = Random.Range(0.5f, 0.9f);
            cargaOrbs[i].transform.localScale = Vector3.one * sz;
        }

        Vector3 escOrig = transform.localScale;
        float   maxRaio = 5.5f; // world units, bem fora do corpo

        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            float p = t / duracao; // 0 → 1

            // anéis seguem a slime, pulsam
            if (cargaAnelExt != null) cargaAnelExt.transform.position = transform.position;
            if (cargaAnelInt != null) cargaAnelInt.transform.position = transform.position;
            float escAnel = 1f + Mathf.Sin(p * Mathf.PI * 6f) * 0.08f * (1f - p);
            if (cargaAnelExt != null) cargaAnelExt.transform.localScale = Vector3.one * escAnel;
            if (cargaAnelInt != null) cargaAnelInt.transform.localScale = Vector3.one * (escAnel * 0.95f);
            AlphaLR(lrCarga, Mathf.Sin(p * Mathf.PI) * 0.95f);
            AlphaLR(lrIn,    Mathf.Sin(p * Mathf.PI) * 0.55f);

            // orbs: raio diminui de maxRaio até centro (chegam ao corpo)
            float raioOrb = Mathf.Lerp(maxRaio, 1.2f, p * p);
            for (int i = 0; i < NUM_ORBS; i++)
            {
                if (cargaOrbs[i] == null || orbSRs[i] == null) continue;
                float ang = (360f / NUM_ORBS * i + t * 200f) * Mathf.Deg2Rad;
                cargaOrbs[i].transform.position = (Vector2)transform.position
                    + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raioOrb;
                var c = orbSRs[i].color;
                c.a = Mathf.Sin(p * Mathf.PI) * 0.95f;
                orbSRs[i].color = c;
            }

            // slime infla e treme
            float pulse = 1f + Mathf.Sin(p * Mathf.PI * 7f) * 0.04f * (1f - p * 0.6f);
            transform.localScale = escOrig * pulse;

            yield return null;
        }

        transform.localScale = escOrig;

        // burst de liberação
        StartCoroutine(BurstCarga(transform.position, cor));

        LimparEfeitoCarga();
    }

    IEnumerator BurstCarga(Vector3 pos, Color cor)
    {
        var go = new GameObject("BurstCarga");
        go.transform.position = pos;
        var lr = CriarAnel(go, 2.0f, new Color(cor.r, cor.g, cor.b, 1f), 0.20f);

        for (float t = 0f; t < 0.30f; t += Time.deltaTime)
        {
            float p = t / 0.30f;
            go.transform.localScale = Vector3.one * Mathf.Lerp(1f, 3.5f, p);
            AlphaLR(lr, Mathf.Lerp(1f, 0f, p));
            yield return null;
        }
        Destroy(go);
    }

    IEnumerator FlashCor(Color alvo, float duracao)
    {
        if (sr == null) { yield return new WaitForSeconds(duracao); yield break; }
        Color orig = sr.color;
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            sr.color = Color.Lerp(orig, alvo, Mathf.Sin(t * Mathf.PI / duracao));
            yield return null;
        }
        sr.color = orig;
    }

    LineRenderer CriarAnel(GameObject parent, float raio, Color cor, float largura)
    {
        const int SEGS = 32;
        var child = new GameObject("Anel");
        child.transform.SetParent(parent.transform, false);
        var lr = child.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 10;
        lr.startWidth    = lr.endWidth = largura;
        lr.startColor    = lr.endColor = cor;
        for (int i = 0; i < SEGS; i++)
        {
            float a = (360f / SEGS) * i * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * raio, Mathf.Sin(a) * raio));
        }
        return lr;
    }

    Transform CriarSpriteFilho(GameObject parent, Sprite sprite, int order, float escala)
    {
        var go  = new GameObject("Fill");
        go.transform.SetParent(parent.transform, false);
        var sr2 = go.AddComponent<SpriteRenderer>();
        sr2.sprite       = sprite;
        sr2.sortingOrder = order;
        go.transform.localScale = Vector3.one * escala;
        return go.transform;
    }

    void AlphaLR(LineRenderer lr, float a)
    {
        if (lr == null) return;
        Color c = lr.startColor; c.a = a;
        lr.startColor = lr.endColor = c;
    }

    // ─── Geração de Sprites ────────────────────────────────────────────────

    static Sprite GerarSprite(int sz, Color cor) => FXTexCache.Obter("SlimeElemental.Sprite", sz, cor, GerarSpriteRaw);

    static Sprite GerarSpriteRaw(int sz, Color cor)
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

    static Sprite GerarDisco(int sz, Color cor) => FXTexCache.Obter("SlimeElemental.Disco", sz, cor, GerarDiscoRaw);

    static Sprite GerarDiscoRaw(int sz, Color cor)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cx));
            tex.SetPixel(x, y, new Color(cor.r, cor.g, cor.b, d < cx ? cor.a : 0f));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }

    static Sprite GerarCristal(int sz, Color cor) => FXTexCache.Obter("SlimeElemental.Cristal", sz, cor, GerarCristalRaw);

    static Sprite GerarCristalRaw(int sz, Color cor)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float nx = (x - cx) / cx;
            float ny = (y - cx) / cx;
            // Forma de losango/cristal
            float d = Mathf.Abs(nx) + Mathf.Abs(ny);
            float a = Mathf.Clamp01(1f - d / 0.9f);
            tex.SetPixel(x, y, new Color(cor.r, cor.g, cor.b, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }
}

public class CacoGeloFX : MonoBehaviour
{
    public Vector2 vel;
    const float DURACAO = 1.8f;

    SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        StartCoroutine(Vida());
    }

    IEnumerator Vida()
    {
        float elapsed = 0f;
        while (elapsed < DURACAO)
        {
            transform.position += (Vector3)(vel * Time.deltaTime);
            vel              *= Mathf.Pow(0.88f, Time.deltaTime * 60f); // desacelera suavemente
            transform.Rotate(0f, 0f, vel.magnitude * 4f * Time.deltaTime);

            elapsed += Time.deltaTime;

            if (sr != null)
            {
                var c = sr.color;
                c.a      = Mathf.Clamp01(1f - elapsed / DURACAO);
                sr.color = c;
            }
            yield return null;
        }
        Destroy(gameObject);
    }
}

public class RajadaFX : MonoBehaviour
{
    void Start() => StartCoroutine(Animar());

    System.Collections.IEnumerator Animar()
    {
        var lr = GetComponent<LineRenderer>();
        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            if (lr == null) { Destroy(gameObject); yield break; }
            float p = t / 0.25f;
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.15f, 0f, p);
            lr.startColor = lr.endColor = new Color(0.6f, 1f, 0.65f, 1f - p);
            yield return null;
        }
        Destroy(gameObject);
    }
}
