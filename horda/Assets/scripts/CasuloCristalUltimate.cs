using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CasuloCristalUltimate : MonoBehaviour
{
    [Header("Configurações")]
    public float cooldown       = 32f;
    public float duracaoCasulo  = 2.5f;
    public float danoEstilhacos = 60f;
    public float raioExplosao   = 5f;

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
        if (InputBindings.UltimateDown() && cooldownRestante <= 0f && !ativo)
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
        ativo = true;
        cooldownRestante = cooldown;

        // Invulnerabilidade
        if (playerStats != null) playerStats.invulneravel = true;

        // Congelar movimento do player (opcional — mantém animações)
        var rb = GetComponent<Rigidbody2D>();
        Vector2 velAnterior = rb != null ? rb.linearVelocity : Vector2.zero;
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.constraints = RigidbodyConstraints2D.FreezeAll; }

        // Cristal surge ao redor do player
        var casulo = CriarCasulo();
        yield return StartCoroutine(AnimarSurgimento(casulo));

        // Pulsa durante a duração
        yield return StartCoroutine(AnimarPulso(casulo));

        // Quebra + dano
        if (playerStats != null)
        {
            playerStats.health = playerStats.maxHealth;
            playerStats.invulneravel = false;
        }

        if (rb != null) rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        yield return StartCoroutine(AnimarExplosao(casulo));

        AplicarDanoEstilhacos();

        ativo = false;
    }

    // ─── LÓGICA ─────────────────────────────────────────────────────────────

    void AplicarDanoEstilhacos()
    {
        // CasuloLetal: +75% dano e +50% raio
        float danoEfetivo = danoEstilhacos;
        float raioEfetivo = raioExplosao;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.CasuloLetal))
        {
            danoEfetivo *= 1.75f;
            raioEfetivo *= 1.5f;
        }

        foreach (var col in Physics2D.OverlapCircleAll(transform.position, raioEfetivo))
        {
            if (col.gameObject == gameObject) continue;
            if (col.GetComponentInParent<ProjetilHomingPrincesa>(true)   != null) continue;
            if (col.GetComponentInParent<ProjetilEspecialPrincesa>(true) != null) continue;
            var ic = col.GetComponent<InimigoController>() ?? col.GetComponentInParent<InimigoController>();
            if (ic != null) ic.ReceberDano(danoEfetivo);
        }
    }

    // ─── VISUAIS ────────────────────────────────────────────────────────────

    static readonly Color corCristal   = new Color(0.5f,  0.9f,  1f,   0.85f);
    static readonly Color corBrilho    = new Color(0.85f, 1f,    1f,   1f);
    static readonly Color corEstilhaco = new Color(0.6f,  0.95f, 1f,   1f);

    // Vértices de um hexágono irregular para o casulo
    static readonly Vector2[] vertsCasulo =
    {
        new Vector2( 0f,    2.2f),
        new Vector2( 1.9f,  1.1f),
        new Vector2( 1.9f, -1.1f),
        new Vector2( 0f,   -2.2f),
        new Vector2(-1.9f, -1.1f),
        new Vector2(-1.9f,  1.1f),
    };

    GameObject CriarCasulo()
    {
        var root = new GameObject("CasuloCristal");
        root.transform.position = transform.position;

        // Parede do cristal (hexágono principal)
        CriarFace(root, vertsCasulo, corCristal, 0.1f, 12);

        // Arestas internas (linhas cruzadas dando profundidade)
        CriarLinha(root, vertsCasulo[0], vertsCasulo[3], new Color(0.7f, 1f, 1f, 0.4f), 0.04f, 13);
        CriarLinha(root, vertsCasulo[1], vertsCasulo[4], new Color(0.7f, 1f, 1f, 0.35f), 0.04f, 13);
        CriarLinha(root, vertsCasulo[2], vertsCasulo[5], new Color(0.7f, 1f, 1f, 0.35f), 0.04f, 13);

        // Segundo anel externo levemente maior
        Vector2[] vertsExt = new Vector2[6];
        for (int i = 0; i < 6; i++) vertsExt[i] = vertsCasulo[i] * 1.25f;
        CriarFace(root, vertsExt, new Color(0.4f, 0.8f, 1f, 0.35f), 0.06f, 11);

        // Brilho central (sprite)
        var nucleo = new GameObject("Nucleo");
        nucleo.transform.SetParent(root.transform, false);
        var sr = nucleo.AddComponent<SpriteRenderer>();
        sr.sprite       = GerarSprite(16, corBrilho);
        sr.color        = new Color(corBrilho.r, corBrilho.g, corBrilho.b, 0.5f);
        sr.sortingOrder = 14;
        nucleo.transform.localScale = Vector3.one * 0.6f;

        return root;
    }

    void CriarFace(GameObject parent, Vector2[] verts, Color cor, float largura, int ordem)
    {
        var go = new GameObject("Face");
        go.transform.SetParent(parent.transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop          = true;
        lr.positionCount = verts.Length;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = ordem;
        lr.startWidth    = lr.endWidth = largura;
        lr.startColor    = lr.endColor = cor;
        for (int i = 0; i < verts.Length; i++)
            lr.SetPosition(i, verts[i]);
    }

    void CriarLinha(GameObject parent, Vector2 de, Vector2 ate, Color cor, float largura, int ordem)
    {
        var go = new GameObject("Linha");
        go.transform.SetParent(parent.transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.positionCount = 2;
        lr.SetPosition(0, de);
        lr.SetPosition(1, ate);
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = ordem;
        lr.startWidth    = lr.endWidth = largura;
        lr.startColor    = lr.endColor = cor;
    }

    IEnumerator AnimarSurgimento(GameObject casulo)
    {
        casulo.transform.localScale = Vector3.zero;
        float dur = 0.3f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = Mathf.SmoothStep(0f, 1f, t / dur);
            // Pop: ultrapassa 1 brevemente
            float s = p < 0.7f ? Mathf.Lerp(0f, 1.2f, p / 0.7f) : Mathf.Lerp(1.2f, 1f, (p - 0.7f) / 0.3f);
            casulo.transform.localScale = Vector3.one * s;
            casulo.transform.position   = transform.position;
            yield return null;
        }
        casulo.transform.localScale = Vector3.one;
    }

    IEnumerator AnimarPulso(GameObject casulo)
    {
        float elapsed = 0f;
        var lrs = casulo.GetComponentsInChildren<LineRenderer>();
        var srs = casulo.GetComponentsInChildren<SpriteRenderer>();

        while (elapsed < duracaoCasulo)
        {
            elapsed += Time.deltaTime;
            casulo.transform.position = transform.position;

            // Rotação lenta do casulo
            casulo.transform.rotation = Quaternion.Euler(0f, 0f, elapsed * 18f);

            // Pulso de brilho
            float pulso = Mathf.Sin(elapsed * 5f) * 0.5f + 0.5f;
            foreach (var lr in lrs)
            {
                Color c = lr.startColor;
                c.a = 0.5f + pulso * 0.45f;
                lr.startColor = lr.endColor = c;
            }
            foreach (var sr in srs)
            {
                Color c = sr.color;
                c.a = 0.3f + pulso * 0.4f;
                sr.color = c;
                sr.transform.localScale = Vector3.one * (0.5f + pulso * 0.15f);
            }

            yield return null;
        }
    }

    IEnumerator AnimarExplosao(GameObject casulo)
    {
        // Flash branco rápido
        var lrs = casulo.GetComponentsInChildren<LineRenderer>();
        foreach (var lr in lrs) lr.startColor = lr.endColor = Color.white;

        yield return new WaitForSeconds(0.05f);

        Destroy(casulo);

        // Lançar estilhaços em várias direções
        LancarEstilhacos();

        // Anel de choque expandindo
        yield return StartCoroutine(AnimarOndaChoque());

        // Flash no player
        yield return StartCoroutine(AnimarFlashPlayer());
    }

    void LancarEstilhacos()
    {
        // CasuloReforjado: +8 estilhaços extras
        int qtd = 16;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.CasuloReforjado))
            qtd += 8;

        for (int i = 0; i < qtd; i++)
        {
            float ang = (360f / qtd) * i + Random.Range(-10f, 10f);
            float rad = ang * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            StartCoroutine(AnimarEstilhaco(dir, Random.Range(0.6f, 1.2f)));
        }
    }

    IEnumerator AnimarEstilhaco(Vector2 dir, float distancia)
    {
        var go = new GameObject("Estilhaco");
        go.transform.position = transform.position;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 16;
        lr.startWidth    = 0.07f;
        lr.endWidth      = 0.02f;
        lr.startColor    = corBrilho;
        lr.endColor      = corCristal;

        float vel = Random.Range(6f, 11f);
        float dur = distancia / vel * raioExplosao * 0.3f;
        Vector3 pos = transform.position;

        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            Vector3 ponta = pos + (Vector3)(dir * vel * t);
            lr.SetPosition(0, pos);
            lr.SetPosition(1, ponta);
            Color c = corCristal; c.a = 1f - p;
            lr.startColor = c;
            lr.endColor   = new Color(1f, 1f, 1f, (1f - p) * 0.6f);
            yield return null;
        }
        Destroy(go);
    }

    IEnumerator AnimarOndaChoque()
    {
        const int SEGS = 36;
        var go = new GameObject("OndaChoque");
        go.transform.position = transform.position;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 15;

        float dur = 0.5f;
        Vector3 centro = transform.position;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            float r = Mathf.Lerp(0.1f, raioExplosao, Mathf.Pow(p, 0.6f));
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.25f, 0.03f, p);
            Color cor = Color.Lerp(Color.white, corCristal, p);
            cor.a = 1f - p;
            lr.startColor = lr.endColor = cor;
            for (int i = 0; i < SEGS; i++)
            {
                float a = (360f / SEGS) * i * Mathf.Deg2Rad;
                lr.SetPosition(i, centro + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
            }
            yield return null;
        }
        Destroy(go);
    }

    IEnumerator AnimarFlashPlayer()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        Color corOriginal = sr.color;
        float dur = 0.4f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            sr.color = Color.Lerp(Color.white, corOriginal, p);
            yield return null;
        }
        sr.color = corOriginal;
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
