using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlimeProtetoraInimiga : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidade          = 2.5f;
    public float velocidadeFuga      = 5f;
    public float distanciaDesejada   = 14f;
    public float toleranciaDistancia = 0.8f;
    public float distanciaFuga       = 8f;
    public float suavizacao          = 10f;
    public float raioAtaque          = 20f;

    [Header("Escudo (Ataque 1)")]
    public float vidaEscudo     = 80f;
    public float raioEscudo     = 14f;
    public float cooldownEscudo = 22f;

    [Header("Buff de Atributos (Ataque 2)")]
    public float bonusDefesa     = 0.3f;
    public float duracaoBuff     = 8f;
    public float raioBuff        = 12f;
    public float cooldownBuff    = 18f;

    [Header("Projetil Anti-Ultimate (Ataque 3)")]
    public float velocidadeProjetil  = 6f;
    public float duracaoBloqueioUlti = 5f;
    public float cooldownProjetil    = 25f;

    Rigidbody2D       rb;
    SpriteRenderer    sr;
    InimigoController inimigoCtrl;
    PlayerStats       player;

    float proxEscudo, proxBuff, proxProjetil;
    bool  atacando;

    List<InimigoController> alvosEscudados = new List<InimigoController>();
    List<InimigoController> alvosBuffados  = new List<InimigoController>();
    bool limpouAoMorrer;

    [Header("Escudo — máx. aliados por cast")]
    public int maxEscudosPorCast = 1;

    Vector2 direcaoMovimento;
    Vector2 ultimaDirFuga = Vector2.down;
    bool    estaMuitoPerto;

    // ── Init ─────────────────────────────────────────────────────────────────

    void Start()
    {
        rb          = GetComponent<Rigidbody2D>();
        sr          = GetComponent<SpriteRenderer>();
        inimigoCtrl = GetComponent<InimigoController>();
        BuscarPlayer();
        proxEscudo   = Random.Range(3f, 6f);
        proxBuff     = Random.Range(4f, 8f);
        proxProjetil = Random.Range(5f, 10f);

        InimigoController.OnPreMorte += OnPreMorteHandler;
    }

    void OnDestroy()
    {
        InimigoController.OnPreMorte -= OnPreMorteHandler;
        LimparEfeitosAoMorrer();
    }

    void OnPreMorteHandler(InimigoController ic)
    {
        if (ic == inimigoCtrl) LimparEfeitosAoMorrer();
    }

    void BuscarPlayer()
    {
        var ps = PlayerStats.MaisProximo(transform.position);
        if (ps != null) player = ps;
    }

    bool Morto() => inimigoCtrl != null && inimigoCtrl.estaMorrendo;

    void LimparEfeitosAoMorrer()
    {
        if (limpouAoMorrer) return;
        limpouAoMorrer = true;

        foreach (var ic in alvosEscudados)
        {
            if (ic == null) continue;
            var e = ic.GetComponent<Escudo>();
            if (e != null) e.ForcarRemover();
        }
        alvosEscudados.Clear();

        var meuEscudo = GetComponent<Escudo>();
        if (meuEscudo != null) meuEscudo.ForcarRemover();

        foreach (var ic in alvosBuffados)
        {
            if (ic == null || ic.estaMorrendo) continue;
            ic.RemoverBuff();
        }
        alvosBuffados.Clear();
    }

    // centro visual do sprite (compensa pivot na base)
    Vector2 CentroSprite => sr != null
        ? (Vector2)sr.bounds.center
        : (Vector2)transform.position;

    // ── Loop ─────────────────────────────────────────────────────────────────

    void Update()
    {
        if (Morto()) return;
        if (player == null) { BuscarPlayer(); return; }

        proxEscudo   -= Time.deltaTime;
        proxBuff     -= Time.deltaTime;
        proxProjetil -= Time.deltaTime;

        float dist = Vector2.Distance(transform.position, player.transform.position);

        if (!atacando)
        {
            Vector2 desejada = CalcularDirecao(dist);
            direcaoMovimento = estaMuitoPerto
                ? desejada
                : Vector2.Lerp(direcaoMovimento, desejada, Time.deltaTime * suavizacao);
        }

        if (!atacando && dist <= raioAtaque)
            TentarAtacar();
    }

    Vector2 CalcularDirecao(float dist)
    {
        Vector2 dirParaPlayer = dist > 0.4f
            ? ((Vector2)player.transform.position - (Vector2)transform.position) / dist
            : -ultimaDirFuga;

        if (dist > 0.4f) ultimaDirFuga = -dirParaPlayer;

        estaMuitoPerto = dist < distanciaFuga;
        if (estaMuitoPerto) return ultimaDirFuga;

        if (dist > distanciaDesejada + toleranciaDistancia)
            return FlowField.Instance != null
                ? FlowField.Instance.ObterDirecao(transform.position)
                : dirParaPlayer;

        if (dist < distanciaDesejada - toleranciaDistancia)
            return -dirParaPlayer;

        Vector2 lateral = new Vector2(-dirParaPlayer.y, dirParaPlayer.x);
        return lateral * (Mathf.Sin(Time.time * 0.7f) * 0.4f);
    }

    void FixedUpdate()
    {
        if (player == null || Morto()) { rb.linearVelocity = Vector2.zero; return; }
        if (atacando) { rb.linearVelocity = Vector2.zero; return; }

        float vel = estaMuitoPerto ? velocidadeFuga : velocidade;
        if (direcaoMovimento.sqrMagnitude > 0.001f)
        {
            float fator = Mathf.Clamp01(direcaoMovimento.magnitude);
            rb.linearVelocity = direcaoMovimento.normalized * vel * fator;
        }
        else rb.linearVelocity = Vector2.zero;

        if (sr != null && rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            if      (rb.linearVelocity.x >  0.05f) sr.flipX = false;
            else if (rb.linearVelocity.x < -0.05f) sr.flipX = true;
        }
    }

    // ── Seleção de ataque ─────────────────────────────────────────────────────

    void TentarAtacar()
    {
        int escolhido = -1;
        float menorCd = 0f;

        if (proxEscudo <= 0f)   { if (escolhido == -1 || proxEscudo   < menorCd) { menorCd = proxEscudo;   escolhido = 0; } }
        if (proxBuff <= 0f)     { if (escolhido == -1 || proxBuff     < menorCd) { menorCd = proxBuff;     escolhido = 1; } }
        if (proxProjetil <= 0f) { if (escolhido == -1 || proxProjetil < menorCd) { menorCd = proxProjetil; escolhido = 2; } }

        if (escolhido == -1) return;

        atacando = true;
        rb.linearVelocity = Vector2.zero;

        switch (escolhido)
        {
            case 0: StartCoroutine(AtaqueEscudo());   break;
            case 1: StartCoroutine(AtaqueBuff());     break;
            case 2: StartCoroutine(AtaqueProjetil()); break;
        }
    }

    // ── Ataque 1: Escudo ──────────────────────────────────────────────────────

    IEnumerator AtaqueEscudo()
    {
        proxEscudo = cooldownEscudo;
        yield return StartCoroutine(EfeitoCarga(new Color(0.6f, 0.2f, 1f), 0.65f));

        SomSkill.Tocar(SomSkill.Tipo.SlimeProtEscudo, CentroSprite, 0.55f);

        // Coleta aliados ordenados por distância
        var cols = Physics2D.OverlapCircleAll(CentroSprite, raioEscudo);
        var candidatos = new List<(InimigoController ic, float dist)>();
        var vistos = new HashSet<InimigoController>();
        foreach (var c in cols)
        {
            var ic = c.GetComponent<InimigoController>() ?? c.GetComponentInParent<InimigoController>();
            if (ic == null || ic == inimigoCtrl || ic.estaMorrendo || vistos.Contains(ic)) continue;
            vistos.Add(ic);
            candidatos.Add((ic, Vector2.Distance(CentroSprite, ic.transform.position)));
        }
        candidatos.Sort((a, b) => a.dist.CompareTo(b.dist));

        int concedidos = 0;
        foreach (var (ic, _) in candidatos)
        {
            if (concedidos >= maxEscudosPorCast) break;

            // Remove escudo existente antes de aplicar novo (sem acúmulo)
            var escudoExistente = ic.GetComponent<Escudo>();
            if (escudoExistente != null) escudoExistente.ForcarRemover();

            var escudo = ic.gameObject.AddComponent<Escudo>();
            escudo.Ativar(vidaEscudo);

            if (!alvosEscudados.Contains(ic)) alvosEscudados.Add(ic);
            StartCoroutine(RaioParaAlvo(ic.transform.position, new Color(0.7f, 0.3f, 1f)));
            concedidos++;
        }

        // Também escuda a si mesmo com menos vida
        var meuEscudo = GetComponent<Escudo>() ?? gameObject.AddComponent<Escudo>();
        meuEscudo.Ativar(vidaEscudo * 0.5f);

        StartCoroutine(OndaCircular(raioEscudo, new Color(0.6f, 0.2f, 1f), 0.6f));
        yield return new WaitForSeconds(0.4f);
        atacando = false;
    }

    // ── Ataque 2: Buff ────────────────────────────────────────────────────────

    IEnumerator AtaqueBuff()
    {
        proxBuff = cooldownBuff;
        yield return StartCoroutine(EfeitoCarga(new Color(1f, 0.85f, 0.1f), 0.55f));

        SomSkill.Tocar(SomSkill.Tipo.SlimeProtBuff, CentroSprite, 0.55f);

        var cols = Physics2D.OverlapCircleAll(CentroSprite, raioBuff);
        InimigoController alvoBuff = null;
        float menorDist = float.MaxValue;
        foreach (var c in cols)
        {
            var ic = c.GetComponent<InimigoController>() ?? c.GetComponentInParent<InimigoController>();
            if (ic == null || ic == inimigoCtrl || ic.estaMorrendo) continue;
            float d = Vector2.Distance(CentroSprite, ic.transform.position);
            if (d < menorDist) { menorDist = d; alvoBuff = ic; }
        }

        if (alvoBuff != null)
        {
            if (!alvosBuffados.Contains(alvoBuff)) alvosBuffados.Add(alvoBuff);
            alvoBuff.AplicarBuffDefesa(bonusDefesa, duracaoBuff);
            StartCoroutine(RaioParaAlvo(alvoBuff.transform.position, new Color(1f, 0.85f, 0.1f)));
            StartCoroutine(ParticulasSubindo(alvoBuff.transform));
        }

        StartCoroutine(OndaCircular(raioBuff, new Color(1f, 0.85f, 0.1f), 0.55f));
        yield return new WaitForSeconds(0.4f);
        atacando = false;
    }

    // ── Ataque 3: Projetil Anti-Ultimate ──────────────────────────────────────

    IEnumerator AtaqueProjetil()
    {
        proxProjetil = cooldownProjetil;
        yield return StartCoroutine(EfeitoCarga(new Color(0.25f, 0f, 0.6f), 0.75f));

        if (player == null) { atacando = false; yield break; }

        SomSkill.Tocar(SomSkill.Tipo.SlimeProtProjetil, CentroSprite, 0.55f);

        Vector2 dir = ((Vector2)player.transform.position - CentroSprite).normalized;

        var projGO = new GameObject("ProjetilAntiUlti");
        projGO.transform.position = CentroSprite;
        projGO.tag   = "Enemy";
        projGO.layer = LayerMask.NameToLayer("Enemy");

        var projSR = projGO.AddComponent<SpriteRenderer>();
        projSR.sprite       = GerarDisco(32, new Color(0.55f, 0.1f, 1f));
        projSR.sortingOrder = 13;
        projGO.transform.localScale = Vector3.one * 1.8f;

        var projCol = projGO.AddComponent<CircleCollider2D>();
        projCol.isTrigger = true;
        projCol.radius    = 0.5f;

        var projRb = projGO.AddComponent<Rigidbody2D>();
        projRb.gravityScale = 0f;
        projRb.constraints  = RigidbodyConstraints2D.FreezeRotation;
        projRb.linearVelocity = dir * velocidadeProjetil;

        var comp = projGO.AddComponent<ProjetilAntiUltiComp>();
        comp.duracaoBloqueio = duracaoBloqueioUlti;
        comp.player          = player;

        yield return new WaitForSeconds(0.3f);
        atacando = false;
    }

    // ── Visuais compartilhados ────────────────────────────────────────────────

    IEnumerator EfeitoCarga(Color cor, float duracao)
    {
        SomSkill.Tocar(SomSkill.Tipo.SlimeProtCarga, CentroSprite, 0.45f);

        var root = new GameObject("CargaVFX");
        root.transform.SetParent(transform);
        root.transform.position = CentroSprite;

        CriarAnelLocal(root, 1.2f, new Color(cor.r, cor.g, cor.b, 0.65f), 0.09f);
        CriarAnelLocal(root, 0.75f, new Color(cor.r, cor.g, cor.b, 0.45f), 0.06f);

        var orbPos = new SpriteRenderer[5];
        for (int i = 0; i < 5; i++)
        {
            var o  = new GameObject($"Orb{i}");
            o.transform.SetParent(root.transform, false);
            var oSR = o.AddComponent<SpriteRenderer>();
            oSR.sprite       = GerarDisco(8, cor);
            oSR.sortingOrder = 14;
            o.transform.localScale = Vector3.one * Random.Range(0.12f, 0.22f);
            orbPos[i] = oSR;
        }

        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            if (root == null) yield break;
            float p = t / duracao;
            root.transform.position = CentroSprite;
            root.transform.Rotate(0f, 0f, Time.deltaTime * 210f);

            float raioOrb = Mathf.Lerp(1.8f, 0.4f, p);
            for (int i = 0; i < orbPos.Length; i++)
            {
                if (orbPos[i] == null) continue;
                float ang = (360f / orbPos.Length * i) * Mathf.Deg2Rad;
                orbPos[i].transform.localPosition = new Vector3(Mathf.Cos(ang) * raioOrb, Mathf.Sin(ang) * raioOrb);
                Color c = orbPos[i].color; c.a = p; orbPos[i].color = c;
            }
            yield return null;
        }

        // Flash no sprite
        if (sr != null)
        {
            Color orig = sr.color;
            sr.color = Color.Lerp(orig, cor, 0.65f);
            yield return new WaitForSeconds(0.07f);
            if (sr != null) sr.color = orig;
        }

        if (root != null) Destroy(root);
    }

    IEnumerator OndaCircular(float raioFinal, Color cor, float duracao)
    {
        var go = new GameObject("OndaCircular");
        go.transform.SetParent(transform);
        go.transform.position = CentroSprite;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = 48;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 12;

        Vector2 centro = go.transform.position;
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p = t / duracao;
            float r = Mathf.Lerp(0.1f, raioFinal, p);
            lr.startColor = lr.endColor = new Color(cor.r, cor.g, cor.b, Mathf.Lerp(0.9f, 0f, p));
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.35f, 0.04f, p);
            for (int i = 0; i < 48; i++)
            {
                float ang = i / 48f * Mathf.PI * 2f;
                lr.SetPosition(i, centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r);
            }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    IEnumerator RaioParaAlvo(Vector3 posAlvo, Color cor)
    {
        var go = new GameObject("Raio");
        go.transform.SetParent(transform);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 11;

        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p = t / 0.4f;
            lr.startColor = lr.endColor = new Color(cor.r, cor.g, cor.b, Mathf.Lerp(0.85f, 0f, p));
            lr.startWidth = Mathf.Lerp(0.12f, 0.01f, p);
            lr.endWidth   = 0.01f;
            lr.SetPosition(0, CentroSprite);
            lr.SetPosition(1, posAlvo);
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    IEnumerator ParticulasSubindo(Transform alvo)
    {
        for (int k = 0; k < 8; k++)
        {
            if (alvo == null) yield break;
            var go  = new GameObject("Partic");
            var sr2 = go.AddComponent<SpriteRenderer>();
            sr2.sprite       = GerarDisco(8, new Color(1f, 0.9f, 0.2f));
            sr2.sortingOrder = 14;
            go.transform.localScale = Vector3.one * Random.Range(0.1f, 0.2f);
            go.transform.position   = (Vector3)alvo.position + new Vector3(Random.Range(-0.4f, 0.4f), 0f);
            StartCoroutine(SubirEDesvanecer(go));
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator SubirEDesvanecer(GameObject go)
    {
        var sr2 = go.GetComponent<SpriteRenderer>();
        for (float t = 0f; t < 1f; t += Time.deltaTime)
        {
            if (go == null) yield break;
            go.transform.position += Vector3.up * Time.deltaTime * 1.3f;
            if (sr2 != null) { Color c = sr2.color; c.a = 1f - t; sr2.color = c; }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    static void CriarAnelLocal(GameObject parent, float r, Color cor, float larg)
    {
        const int SEGS = 36;
        var go = new GameObject("Anel");
        go.transform.SetParent(parent.transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 13;
        lr.startWidth    = lr.endWidth = larg;
        lr.startColor    = lr.endColor = cor;
        for (int i = 0; i < SEGS; i++)
        {
            float a = (360f / SEGS) * i * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
        }
    }

    static Sprite GerarDisco(int sz, Color cor)
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.2f, 1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, raioEscudo);
        Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, raioBuff);
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, raioAtaque);
    }
}

// ── Componente do projetil ────────────────────────────────────────────────────

public class ProjetilAntiUltiComp : MonoBehaviour
{
    public float       duracaoBloqueio = 5f;
    public PlayerStats player;

    bool acertou;

    void Start()
    {
        Destroy(gameObject, 7f);
        StartCoroutine(Trail());
    }

    void Update()
    {
        if (acertou || player == null) return;
        if (Vector2.Distance(transform.position, player.transform.position) < 1.5f)
            Acertar(player);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (acertou) return;
        if (!other.CompareTag("Player")) return;
        var ps = other.GetComponent<PlayerStats>() ?? other.GetComponentInParent<PlayerStats>();
        if (ps != null) Acertar(ps);
    }

    void Acertar(PlayerStats ps)
    {
        if (acertou) return;
        acertou = true;

        // Esconde e para o projétil imediatamente
        var projSR = GetComponent<SpriteRenderer>();
        if (projSR != null) projSR.enabled = false;
        var projRb = GetComponent<Rigidbody2D>();
        if (projRb != null) projRb.linearVelocity = Vector2.zero;
        var projCol = GetComponent<CircleCollider2D>();
        if (projCol != null) projCol.enabled = false;

        // Bloqueio roteado pela rede: em co-op o host manda pro cliente DONO aplicar (é lá
        // que a ultimate é castada e o X do ícone é desenhado). Em SP roda direto.
        ps.BloquearUltimate(duracaoBloqueio);
        StartCoroutine(ExplosaoEDesaparecer());
    }

    IEnumerator ExplosaoEDesaparecer()
    {
        yield return StartCoroutine(Explodir());
        Destroy(gameObject);
    }

    IEnumerator Explodir()
    {
        var pos = (Vector2)transform.position;
        var go  = new GameObject("ExplosaoUlti");
        go.transform.position = pos;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = 48;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 15;

        float dur = 0.55f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p = t / dur;
            float r = Mathf.Lerp(0.05f, 2.2f, p);
            lr.startColor = lr.endColor = new Color(0.45f, 0f, 1f, Mathf.Lerp(1f, 0f, p));
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.35f, 0.02f, p);
            for (int i = 0; i < 48; i++)
            {
                float ang = i / 48f * Mathf.PI * 2f;
                lr.SetPosition(i, pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r);
            }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    IEnumerator Trail()
    {
        while (this != null && gameObject != null)
        {
            yield return new WaitForSeconds(0.04f);
            if (gameObject == null || acertou) yield break; // para o rastro ao acertar
            var go  = new GameObject("T");
            var sr2 = go.AddComponent<SpriteRenderer>();

            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            float cx = 4f;
            for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
            {
                float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(cx, cx));
                tex.SetPixel(x, y, new Color(0.3f, 0f, 0.8f, Mathf.Clamp01(1f - d / cx)));
            }
            tex.Apply();
            sr2.sprite       = Sprite.Create(tex, new Rect(0,0,8,8), new Vector2(.5f,.5f), 8f);
            sr2.sortingOrder = 12;
            go.transform.localScale = Vector3.one * Random.Range(0.6f, 1.1f);
            go.transform.position   = transform.position + (Vector3)Random.insideUnitCircle * 0.3f;
            Destroy(go, 0.35f);
            var c2 = sr2.color; c2.a = 0.6f; sr2.color = c2;
        }
    }
}
