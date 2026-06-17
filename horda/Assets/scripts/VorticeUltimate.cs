using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VorticeUltimate : MonoBehaviour
{
    [Header("Configurações")]
    public float raio         = 7f;
    public float duracao      = 5f;
    public float cooldown     = 22f;
    public float forcaAtracao = 14f;
    public float velocidade   = 5f;
    public float raioDeteccao = 25f;

    public float CooldownRestante => cooldownRestante;
    public bool  Ativo            => ativo;

    private float       cooldownRestante;
    private bool        ativo;
    private Vector2     vortexPos;
    private PlayerStats playerStats;

    struct EstadoAtraido
    {
        public GameObject          go;
        public List<MonoBehaviour> scripts;
        public Rigidbody2D         rb;
    }
    readonly List<EstadoAtraido> atraidos = new List<EstadoAtraido>();

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

    void FixedUpdate()
    {
        if (!ativo || atraidos.Count == 0) return;
        Vector2 centro = vortexPos;
        foreach (var e in atraidos)
        {
            if (e.rb == null || e.go == null) continue;
            Vector2 dir = centro - (Vector2)e.go.transform.position;
            if (dir.magnitude < 0.1f) continue;
            Vector2 tangente = new Vector2(-dir.normalized.y, dir.normalized.x);
            e.rb.linearVelocity = dir.normalized * forcaAtracao + tangente * (forcaAtracao * 0.4f);
        }
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

        // VorticeExpansivo: +40% de raio
        float raioOriginal = raio;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.VorticeExpansivo))
            raio *= 1.4f;

        Vector2 posicaoCampo = transform.position;
        vortexPos            = posicaoCampo;
        Vector2 direcao      = EncontrarDirecaoInimigo(posicaoCampo);

        GameObject vfx = CriarVFX();
        vfx.transform.position = posicaoCampo;

        // Fase de viagem: vórtice se lança em direção ao inimigo mais próximo
        float tempoViagem = 3f;
        float t = 0f;
        while (t < tempoViagem)
        {
            t += Time.deltaTime;
            posicaoCampo += direcao * velocidade * Time.deltaTime;
            vortexPos     = posicaoCampo;
            vfx.transform.position = posicaoCampo;
            AtrairInimigos(posicaoCampo);
            yield return null;
        }

        // Fase de atração: fica no lugar e puxa inimigos para o centro
        yield return StartCoroutine(AnimarEntrada(vfx));

        // VorticeExpansivo: +2s de duração
        float duracaoEfetiva = duracao;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.VorticeExpansivo))
            duracaoEfetiva += 2f;

        float elapsed  = 0f;
        float rotacao  = 0f;
        float danoAcum = 0f;

        while (elapsed < duracaoEfetiva)
        {
            elapsed += Time.deltaTime;
            rotacao += Time.deltaTime * 280f;

            AnimarVortice(vfx, rotacao, elapsed);
            AtrairInimigos(posicaoCampo);

            // VorticeDestruidor: 15 dano/s nos inimigos dentro do vórtice
            if (SkillEvolutionManager.Tem(SkillEvolutionType.VorticeDestruidor))
            {
                danoAcum += 15f * Time.deltaTime;
                if (danoAcum >= 1f)
                {
                    float danoAplicar = Mathf.Floor(danoAcum);
                    danoAcum -= danoAplicar;
                    foreach (var e in atraidos)
                    {
                        if (e.go == null) continue;
                        var ic = e.go.GetComponent<InimigoController>();
                        if (ic != null) ic.ReceberDano(danoAplicar, false, false);
                    }
                }
            }

            yield return null;
        }

        LiberarTodos();
        raio = raioOriginal; // restaura raio base
        ativo = false;
        StartCoroutine(FadeOutDestruir(vfx, 0.35f));
    }

    Vector2 EncontrarDirecaoInimigo(Vector2 origem)
    {
        float   distMin = float.MaxValue;
        Vector2 melhor  = Vector2.right;
        foreach (var col in Physics2D.OverlapCircleAll(origem, raioDeteccao))
        {
            var root = ResolverInimigo(col.gameObject);
            if (root == null) continue;
            float d = Vector2.Distance(origem, root.transform.position);
            if (d < distMin) { distMin = d; melhor = ((Vector2)root.transform.position - origem).normalized; }
        }
        return melhor;
    }

    // ─── LÓGICA ─────────────────────────────────────────────────────────────

    void AtrairInimigos(Vector2 centro)
    {
        var noRaio = new System.Collections.Generic.HashSet<GameObject>();
        foreach (var c in Physics2D.OverlapCircleAll(centro, raio))
        {
            if (c.gameObject == gameObject) continue;
            var root = ResolverInimigo(c.gameObject);
            if (root != null) noRaio.Add(root);
        }

        foreach (var go in noRaio)
            if (!JaAtraido(go)) AdicionarAtraido(go);

        for (int i = atraidos.Count - 1; i >= 0; i--)
        {
            if (atraidos[i].go == null || !noRaio.Contains(atraidos[i].go))
            {
                LiberarUm(atraidos[i]);
                atraidos.RemoveAt(i);
            }
        }
    }

    bool JaAtraido(GameObject go)
    {
        foreach (var e in atraidos) if (e.go == go) return true;
        return false;
    }

    void AdicionarAtraido(GameObject go)
    {
        var scripts = new List<MonoBehaviour>();
        Desativar(go.GetComponent<movi_inimigo>(),                  scripts);
        Desativar(go.GetComponent<movi_inimigo_manter_distancia>(), scripts);
        Desativar(go.GetComponent<BossController>(),                scripts);
        Desativar(go.GetComponent<BossPrincesa>(),                  scripts);

        var rb = go.GetComponent<Rigidbody2D>();
        atraidos.Add(new EstadoAtraido { go = go, scripts = scripts, rb = rb });
    }

    void LiberarUm(EstadoAtraido e)
    {
        foreach (var s in e.scripts)
            if (s != null) s.enabled = true;
        if (e.rb != null) e.rb.linearVelocity = Vector2.zero;
    }

    void LiberarTodos()
    {
        foreach (var e in atraidos) LiberarUm(e);
        atraidos.Clear();
    }

    static void Desativar(MonoBehaviour mb, List<MonoBehaviour> lista)
    {
        if (mb == null) return;
        mb.enabled = false;
        lista.Add(mb);
    }

    static GameObject ResolverInimigo(GameObject go)
    {
        if (go.GetComponentInParent<ProjetilHomingPrincesa>(true)   != null) return null;
        if (go.GetComponentInParent<ProjetilEspecialPrincesa>(true) != null) return null;

        GameObject root = null;
        var ic = go.GetComponent<InimigoController>() ?? go.GetComponentInParent<InimigoController>();
        if (ic != null) root = ic.gameObject;
        if (root == null) { var mi = go.GetComponent<movi_inimigo>() ?? go.GetComponentInParent<movi_inimigo>(); if (mi != null) root = mi.gameObject; }
        if (root == null) return null;

        // Bosses não são afetados pelo vórtice
        if (root.GetComponent<BossController>() != null) return null;
        if (root.GetComponent<BossPrincesa>()   != null) return null;

        return root;
    }

    // ─── VISUAIS ────────────────────────────────────────────────────────────

    GameObject CriarVFX()
    {
        var root = new GameObject("VFXVortice");
        root.transform.position = transform.position;

        // Anel externo
        CriarAnelVFX(root, raio,       new Color(0.4f, 1f, 0.7f, 0.8f), 0.09f, "AnelExterno");
        // Anel médio
        CriarAnelVFX(root, raio * 0.6f, new Color(0.3f, 0.9f, 0.55f, 0.6f), 0.07f, "AnelMedio");
        // Anel interno
        CriarAnelVFX(root, raio * 0.3f, new Color(0.6f, 1f, 0.8f, 0.5f), 0.05f, "AnelInterno");

        // Partículas: 8 pontos girando
        CriarParticulas(root);

        return root;
    }

    void CriarAnelVFX(GameObject parent, float r, Color cor, float largura, string nome)
    {
        const int SEGS = 36;
        var child = new GameObject(nome);
        child.transform.SetParent(parent.transform, false);
        var lr = child.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 12;
        lr.startWidth    = lr.endWidth = largura;
        lr.startColor    = lr.endColor = cor;
        for (int i = 0; i < SEGS; i++)
        {
            float ang = (360f / SEGS) * i * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r));
        }
    }

    void CriarParticulas(GameObject parent)
    {
        // 8 sprites pequenos que vão girar
        for (int i = 0; i < 8; i++)
        {
            var p = new GameObject($"Particula{i}");
            p.transform.SetParent(parent.transform, false);
            var sr = p.AddComponent<SpriteRenderer>();
            sr.sprite       = GerarSprite(8, new Color(0.4f, 1f, 0.7f));
            sr.color        = new Color(0.4f, 1f, 0.7f, 0.9f);
            sr.sortingOrder = 13;
            p.transform.localScale = Vector3.one * 0.18f;
            float ang = (360f / 8f) * i * Mathf.Deg2Rad;
            p.transform.localPosition = new Vector3(Mathf.Cos(ang) * raio * 0.75f,
                                                    Mathf.Sin(ang) * raio * 0.75f);
        }
    }

    IEnumerator AnimarEntrada(GameObject root)
    {
        var lrs = root.GetComponentsInChildren<LineRenderer>();
        float dur = 0.25f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            foreach (var lr in lrs)
            {
                Color c = lr.startColor; c.a = p;
                lr.startColor = lr.endColor = c;
            }
            yield return null;
        }
    }

    void AnimarVortice(GameObject root, float rotacao, float elapsed)
    {
        var lrs = root.GetComponentsInChildren<LineRenderer>();
        float pulso = Mathf.Sin(elapsed * 4f) * 0.5f + 0.5f;
        foreach (var lr in lrs)
        {
            Color c = lr.startColor; c.a = 0.4f + pulso * 0.4f;
            lr.startColor = lr.endColor = c;
        }

        // Gira as partículas
        var particulas = root.GetComponentsInChildren<SpriteRenderer>();
        for (int i = 0; i < particulas.Length; i++)
        {
            float baseAng = (360f / particulas.Length) * i;
            float ang     = (baseAng + rotacao) * Mathf.Deg2Rad;
            // Raio pulsante
            float r = raio * 0.75f + Mathf.Sin(elapsed * 3f + i) * raio * 0.12f;
            particulas[i].transform.localPosition = new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r);
        }
    }

    IEnumerator FadeOutDestruir(GameObject go, float dur)
    {
        var lrs = go.GetComponentsInChildren<LineRenderer>();
        var srs = go.GetComponentsInChildren<SpriteRenderer>();
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            foreach (var lr in lrs) { Color c = lr.startColor; c.a = Mathf.Lerp(1f, 0f, p); lr.startColor = lr.endColor = c; }
            foreach (var sr in srs) { Color c = sr.color;       c.a = Mathf.Lerp(1f, 0f, p); sr.color = c; }
            yield return null;
        }
        if (go != null) Destroy(go);
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
