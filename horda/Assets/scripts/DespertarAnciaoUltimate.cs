using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DespertarAnciaoUltimate : MonoBehaviour
{
    [Header("Configurações")]
    public float raio              = 14f;
    public float duracao           = 8f;
    public float cooldown          = 28f;
    public float intervaloGolpe    = 0.55f;
    public float danoGolpe         = 28f;
    public float raioImpacto       = 2.5f;

    public float CooldownRestante => cooldownRestante;
    public bool  Ativo            => ativo;

    private float       cooldownRestante;
    private bool        ativo;
    private PlayerStats playerStats;

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
        if (Input.GetKeyDown(KeyCode.R) && cooldownRestante <= 0f && !ativo)
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

        var entidadeGO = CriarEntidade(posEntidade);
        yield return StartCoroutine(AnimarEntrada(entidadeGO, posEntidade));

        // DespertarFurioso: intervalo reduzido para 0.35s
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
                    StartCoroutine(AnimarTentaculo(posEntidade, alvo.Value));
            }

            yield return null;
        }

        ativo = false;
        StartCoroutine(AnimarSaida(entidadeGO));
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
        // DespertarGigante: +60% raio de impacto e +50% dano
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
            if (ic != null) ic.ReceberDano(danoEfetivo, Random.value < 0.2f);
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

    // ─── VISUAIS ────────────────────────────────────────────────────────────

    GameObject CriarEntidade(Vector2 pos)
    {
        var root = new GameObject("Anciao");
        root.transform.position = pos;

        // Corpo: anel irregular grande
        CriarCorpo(root, 3.5f, "CorpoExterno", new Color(0.15f, 0f, 0.25f, 0.9f), 0.22f, 28);
        CriarCorpo(root, 2.2f, "CorpoMedio",   new Color(0.25f, 0f, 0.4f,  0.7f), 0.15f, 20);
        CriarCorpo(root, 1.1f, "CorpoInterno", new Color(0.4f,  0f, 0.6f,  0.6f), 0.10f, 14);

        // Olhos
        CriarOlho(root, new Vector2(-0.6f, 0.2f));
        CriarOlho(root, new Vector2( 0.6f, 0.2f));
        CriarOlho(root, new Vector2( 0f,  -0.4f));

        return root;
    }

    void CriarCorpo(GameObject parent, float r, string nome, Color cor, float largura, int segs)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(parent.transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop          = true;
        lr.positionCount = segs;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 15;
        lr.startWidth    = lr.endWidth = largura;
        lr.startColor    = lr.endColor = cor;
        for (int i = 0; i < segs; i++)
        {
            float ang  = (360f / segs) * i * Mathf.Deg2Rad;
            float jit  = Random.Range(0.85f, 1.15f);
            lr.SetPosition(i, new Vector3(Mathf.Cos(ang) * r * jit, Mathf.Sin(ang) * r * jit));
        }
    }

    void CriarOlho(GameObject parent, Vector2 localPos)
    {
        var go = new GameObject("Olho");
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = GerarSprite(10, new Color(0.8f, 0f, 1f));
        sr.color        = new Color(0.8f, 0f, 1f, 1f);
        sr.sortingOrder = 17;
        go.transform.localScale = Vector3.one * 0.28f;
    }

    IEnumerator AnimarEntrada(GameObject root, Vector2 posAlvo)
    {
        Vector2 posInicio = posAlvo + Vector2.up * 6f;
        var lrs = root.GetComponentsInChildren<LineRenderer>();
        var srs = root.GetComponentsInChildren<SpriteRenderer>();

        float dur = 0.5f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            root.transform.position = Vector2.Lerp(posInicio, posAlvo, p);
            foreach (var lr in lrs) { Color c = lr.startColor; c.a *= p; lr.startColor = lr.endColor = c; }
            foreach (var sr in srs) { Color c = sr.color; c.a = p; sr.color = c; }
            yield return null;
        }
        root.transform.position = posAlvo;
    }

    void AnimarEntidade(GameObject root, Vector2 pos, float elapsed)
    {
        // Flutuação lenta
        root.transform.position = pos + Vector2.up * Mathf.Sin(elapsed * 1.2f) * 0.3f;

        // Pulso dos olhos
        float pulso = Mathf.Sin(elapsed * 5f) * 0.5f + 0.5f;
        foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>())
        {
            Color c = sr.color;
            c.a = 0.7f + pulso * 0.3f;
            sr.color = c;
            sr.transform.localScale = Vector3.one * (0.25f + pulso * 0.05f);
        }

        // Pulso dos anéis
        var lrs = root.GetComponentsInChildren<LineRenderer>();
        foreach (var lr in lrs)
        {
            Color c = lr.startColor;
            c.a = 0.5f + pulso * 0.4f;
            lr.startColor = lr.endColor = c;
        }
    }

    IEnumerator AnimarTentaculo(Vector2 origem, Vector2 destino)
    {
        // Marcador de alvo
        var marcador = CriarMarcador(destino);

        // Espera telegráfica
        yield return new WaitForSeconds(0.25f);

        Destroy(marcador);

        // Tentáculo desce rapidamente
        var go = new GameObject("Tentaculo");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 10;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 16;
        lr.startWidth    = 0.35f;
        lr.endWidth      = 0.12f;
        lr.startColor    = new Color(0.3f, 0f, 0.5f, 1f);
        lr.endColor      = new Color(0.6f, 0f, 0.9f, 0.8f);

        float dur = 0.18f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            AtualizarTentaculo(lr, origem, destino, p);
            yield return null;
        }

        AtualizarTentaculo(lr, origem, destino, 1f);
        AplicarDano(destino);
        StartCoroutine(AnimarImpacto(destino));

        // Fade out
        for (float t = 0f; t < 0.2f; t += Time.deltaTime)
        {
            float p = t / 0.2f;
            Color cs = lr.startColor; cs.a = Mathf.Lerp(1f, 0f, p); lr.startColor = cs;
            Color ce = lr.endColor;   ce.a = Mathf.Lerp(0.8f, 0f, p); lr.endColor = ce;
            yield return null;
        }
        Destroy(go);
    }

    void AtualizarTentaculo(LineRenderer lr, Vector2 origem, Vector2 destino, float p)
    {
        int segs = lr.positionCount;
        for (int i = 0; i < segs; i++)
        {
            float t    = i / (float)(segs - 1);
            Vector2 pt = Vector2.Lerp(origem, destino, t * p);
            // Ondulação lateral
            Vector2 perp = new Vector2(-(destino - origem).y, (destino - origem).x).normalized;
            float wave   = Mathf.Sin(t * Mathf.PI * 2f + p * 8f) * (1f - p) * 0.5f;
            pt += perp * wave;
            lr.SetPosition(i, pt);
        }
    }

    GameObject CriarMarcador(Vector2 pos)
    {
        const int SEGS = 20;
        var go = new GameObject("MarcadorAnciao");
        go.transform.position = pos;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 10;
        lr.startWidth    = lr.endWidth = 0.06f;
        lr.startColor    = lr.endColor = new Color(0.6f, 0f, 0.9f, 0.7f);
        for (int i = 0; i < SEGS; i++)
        {
            float a = (360f / SEGS) * i * Mathf.Deg2Rad;
            lr.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * raioImpacto);
        }
        return go;
    }

    IEnumerator AnimarImpacto(Vector2 pos)
    {
        const int SEGS = 24;
        var go = new GameObject("ImpactoAnciao");
        go.transform.position = pos;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 14;

        for (float t = 0f; t < 0.35f; t += Time.deltaTime)
        {
            float p = t / 0.35f;
            float r = Mathf.Lerp(0.1f, raioImpacto, p);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.3f, 0.04f, p);
            lr.startColor = lr.endColor = new Color(0.5f + (1f - p) * 0.4f, 0f, 0.8f, 1f - p);
            for (int i = 0; i < SEGS; i++)
            {
                float a = (360f / SEGS) * i * Mathf.Deg2Rad;
                lr.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
            yield return null;
        }
        Destroy(go);
    }

    IEnumerator AnimarSaida(GameObject root)
    {
        var lrs = root.GetComponentsInChildren<LineRenderer>();
        var srs = root.GetComponentsInChildren<SpriteRenderer>();
        Vector2 posAtual = root.transform.position;
        Vector2 posAlvo  = posAtual + Vector2.up * 6f;

        float dur = 0.5f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            root.transform.position = Vector2.Lerp(posAtual, posAlvo, p);
            foreach (var lr in lrs) { Color c = lr.startColor; c.a = Mathf.Lerp(c.a, 0f, p); lr.startColor = lr.endColor = c; }
            foreach (var sr in srs) { Color c = sr.color; c.a = Mathf.Lerp(c.a, 0f, p); sr.color = c; }
            yield return null;
        }
        Destroy(root);
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
            float a = Mathf.Clamp01(1f - d / cx);
            tex.SetPixel(x, y, new Color(cor.r, cor.g, cor.b, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }
}
