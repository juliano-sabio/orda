using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PulsoMagneticoUltimate : MonoBehaviour, IUltimateCosmetico
{
    bool cosmetico;
    public void ExecutarCosmetico() { if (ativo) return; cosmetico = true; StartCoroutine(CorotinaAtivacao()); }

    [Header("Configurações")]
    public float raio              = 8f;
    public float duracao           = 5f;
    public float cooldown          = 22f;
    public float velocidadeAtracao = 5f;
    public float forcaRepulsao     = 18f;
    public float danoRepulsao      = 40f;

    public float CooldownRestante => cooldownRestante;
    public bool  Ativo            => ativo;

    private float       cooldownRestante;
    private bool        ativo;
    private PlayerStats playerStats;

    struct EstadoInimigo
    {
        public GameObject          go;
        public List<MonoBehaviour> scripts;
        public Rigidbody2D         rb;
        public SpriteRenderer[]    renderers;
        public Color[]             coresOriginais;
        public LineRenderer        arco;
    }

    readonly List<EstadoInimigo> atraidos = new List<EstadoInimigo>();

    // ─── LIFECYCLE ──────────────────────────────────────────────────────

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

    void FixedUpdate()
    {
        if (!ativo) return;
        Vector2 centro = transform.position;
        foreach (var e in atraidos)
        {
            if (e.rb == null || e.go == null) continue;
            Vector2 dir = (centro - (Vector2)e.go.transform.position).normalized;
            e.rb.linearVelocity = dir * velocidadeAtracao;
        }
    }

    void SincronizarUI()
    {
        if (playerStats == null) return;
        playerStats.ultimateChargeTime = cooldown - Mathf.Max(0f, cooldownRestante);
        playerStats.ultimateReady      = cooldownRestante <= 0f && !ativo;
    }

    // ─── COROUTINE PRINCIPAL ────────────────────────────────────────────

    IEnumerator CorotinaAtivacao()
    {
        ativo            = true;
        cooldownRestante = cooldown;

        // MagneticoCentro: +1.5s de fase de atração
        float duracaoAtracao = duracao - 0.5f;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.MagneticoCentro))
            duracaoAtracao += 1.5f;

        var auraGO = CriarAura();

        // Fase atração
        float elapsed = 0f;
        while (elapsed < duracaoAtracao)
        {
            elapsed += Time.deltaTime;
            AtualizarAtraidos();
            AtualizarArcos();
            yield return null;
        }

        // Fase repulsão
        Repelir();
        ativo = false;

        StartCoroutine(AnimarOnda());
        StartCoroutine(FadeOut(auraGO));
    }

    // ─── ATRAÇÃO ────────────────────────────────────────────────────────

    void AtualizarAtraidos()
    {
        var noRaio = new HashSet<GameObject>();
        foreach (var col in Physics2D.OverlapCircleAll(transform.position, raio))
        {
            var root = ResolverRoot(col.gameObject);
            if (root != null) noRaio.Add(root);
        }

        // Adiciona novos
        foreach (var go in noRaio)
        {
            if (JaAtraido(go)) continue;
            AdicionarAtraido(go);
        }

        // Remove os que saíram
        for (int i = atraidos.Count - 1; i >= 0; i--)
        {
            if (atraidos[i].go == null || !noRaio.Contains(atraidos[i].go))
            {
                LiberarUm(atraidos[i], false);
                atraidos.RemoveAt(i);
            }
        }
    }

    void AdicionarAtraido(GameObject go)
    {
        var scripts = new List<MonoBehaviour>();
        Desativar(go.GetComponent<movi_inimigo>(),                   scripts);
        Desativar(go.GetComponent<movi_inimigo_manter_distancia>(),  scripts);
        Desativar(go.GetComponent<BossController>(),                 scripts);
        Desativar(go.GetComponent<BossPrincesa>(),                   scripts);

        var rb = go.GetComponent<Rigidbody2D>();

        Color tint = new Color(0.9f, 0.7f, 1f);
        var srs   = go.GetComponentsInChildren<SpriteRenderer>();
        var cores = new Color[srs.Length];
        for (int i = 0; i < srs.Length; i++)
        {
            cores[i]     = srs[i].color;
            srs[i].color = Color.Lerp(srs[i].color, tint, 0.55f);
        }

        atraidos.Add(new EstadoInimigo
        {
            go             = go,
            scripts        = scripts,
            rb             = rb,
            renderers      = srs,
            coresOriginais = cores,
            arco           = CriarArcoEletrico(go),
        });
    }

    void LiberarUm(EstadoInimigo e, bool aplicarImpulso)
    {
        foreach (var s in e.scripts)
            if (s != null) s.enabled = true;
        for (int i = 0; i < e.renderers.Length; i++)
            if (e.renderers[i] != null) e.renderers[i].color = e.coresOriginais[i];
        if (e.arco != null) Destroy(e.arco.gameObject);

        if (aplicarImpulso && e.rb != null && e.go != null)
        {
            Vector2 dir = ((Vector2)e.go.transform.position - (Vector2)transform.position).normalized;
            if (dir == Vector2.zero) dir = Random.insideUnitCircle.normalized;
            e.rb.linearVelocity = Vector2.zero;

            // MagneticoSupercarregado: +80% força de repulsão
            float forcaEfetiva = forcaRepulsao;
            if (SkillEvolutionManager.Tem(SkillEvolutionType.MagneticoSupercarregado))
                forcaEfetiva *= 1.8f;

            e.rb.AddForce(dir * forcaEfetiva, ForceMode2D.Impulse);

            var inimigo = e.go.GetComponent<InimigoController>();
            if (inimigo != null && danoRepulsao > 0f)
            {
                // MagneticoSupercarregado: +50% dano de repulsão
                float danoEfetivo = danoRepulsao;
                if (SkillEvolutionManager.Tem(SkillEvolutionType.MagneticoSupercarregado))
                    danoEfetivo *= 1.5f;

                bool crit = Random.value < 0.25f;
                if (!cosmetico) inimigo.ReceberDano(crit ? danoEfetivo * 1.5f : danoEfetivo, crit);
            }
        }
    }

    void Repelir()
    {
        foreach (var e in atraidos) LiberarUm(e, true);
        atraidos.Clear();
    }

    static void Desativar(MonoBehaviour mb, List<MonoBehaviour> lista)
    {
        if (mb == null) return;
        mb.enabled = false;
        lista.Add(mb);
    }

    bool JaAtraido(GameObject go)
    {
        foreach (var e in atraidos) if (e.go == go) return true;
        return false;
    }

    // ─── VISUAL ─────────────────────────────────────────────────────────

    GameObject CriarAura()
    {
        var root = new GameObject("AuraMagnetica");
        root.transform.position = transform.position;

        // Anel externo do campo
        const int SEGS = 48;
        var anelGO = new GameObject("Anel");
        anelGO.transform.SetParent(root.transform, false);
        var lr = anelGO.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 12;
        lr.startWidth    = lr.endWidth = 0.1f;
        lr.startColor    = lr.endColor = new Color(0.75f, 0.4f, 1f, 0.9f);
        for (int i = 0; i < SEGS; i++)
        {
            float a = (360f / SEGS) * i * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * raio, Mathf.Sin(a) * raio));
        }

        StartCoroutine(AnimarAura(root, lr));
        return root;
    }

    IEnumerator AnimarAura(GameObject root, LineRenderer lr)
    {
        const int SEGS = 48;
        float t = 0f;
        while (ativo && root != null)
        {
            t += Time.deltaTime;
            root.transform.position = transform.position;

            // Pulso elétrico no anel
            for (int i = 0; i < SEGS; i++)
            {
                float a     = (360f / SEGS) * i * Mathf.Deg2Rad;
                float jit   = Mathf.Sin(t * 12f + i * 0.7f) * 0.08f;
                float r     = raio + jit;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
            }

            float brilho   = 0.6f + Mathf.Sin(t * 5f) * 0.4f;
            lr.startColor  = lr.endColor = new Color(0.6f + brilho * 0.3f, 0.3f, 1f, 0.7f + brilho * 0.3f);
            yield return null;
        }
    }

    // Arco elétrico inimigo → jogador
    LineRenderer CriarArcoEletrico(GameObject inimigo)
    {
        var go = new GameObject("Arco");
        go.transform.SetParent(inimigo.transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 8;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 14;
        lr.startWidth    = 0.05f;
        lr.endWidth      = 0.02f;
        lr.startColor    = new Color(0.8f, 0.5f, 1f, 0.9f);
        lr.endColor      = new Color(1f, 0.9f, 0.4f, 0.6f);
        return lr;
    }

    void AtualizarArcos()
    {
        Vector3 dest = transform.position;
        foreach (var e in atraidos)
        {
            if (e.arco == null || e.go == null) continue;
            Vector3 origem = e.go.transform.position;
            int segs       = e.arco.positionCount;
            for (int i = 0; i < segs; i++)
            {
                float t    = i / (float)(segs - 1);
                Vector3 p  = Vector3.Lerp(origem, dest, t);
                // jitter perpendicular para parecer raio
                Vector3 perp = new Vector3(-(dest - origem).y, (dest - origem).x, 0f).normalized;
                p += perp * Mathf.Sin(t * Mathf.PI) * Random.Range(-0.25f, 0.25f);
                e.arco.SetPosition(i, p);
            }
            // pisca o arco
            Color c = e.arco.startColor;
            c.a     = Random.value > 0.15f ? 0.9f : 0.2f;
            e.arco.startColor = c;
        }
    }

    IEnumerator AnimarOnda()
    {
        var go = new GameObject("OndaRepulsao");
        go.transform.position = transform.position;

        const int SEGS = 48;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 13;
        lr.startColor    = lr.endColor = new Color(1f, 0.85f, 0.3f, 1f);

        float r = 0.2f, maxR = raio * 1.2f, dur = 0.5f, t = 0f;
        while (t < dur)
        {
            t    += Time.deltaTime;
            r     = Mathf.Lerp(0.2f, maxR, t / dur);
            float alpha = Mathf.Lerp(1f, 0f, t / dur);
            float w     = Mathf.Lerp(0.25f, 0.04f, t / dur);
            lr.startWidth = lr.endWidth = w;
            Color c = new Color(1f, 0.7f + (1f - t / dur) * 0.3f, 0.3f, alpha);
            lr.startColor = lr.endColor = c;
            for (int i = 0; i < SEGS; i++)
            {
                float a = (360f / SEGS) * i * Mathf.Deg2Rad;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
            }
            yield return null;
        }
        Destroy(go);
    }

    IEnumerator FadeOut(GameObject root)
    {
        if (root == null) yield break;
        var lr = root.GetComponentInChildren<LineRenderer>();
        Color cor = lr != null ? lr.startColor : Color.clear;
        float t = 0f, dur = 0.35f;
        while (t < dur && root != null)
        {
            t += Time.deltaTime;
            if (lr != null) { Color c = cor; c.a = Mathf.Lerp(cor.a, 0f, t / dur); lr.startColor = lr.endColor = c; }
            yield return null;
        }
        if (root != null) Destroy(root);
    }

    // ─── HELPER ─────────────────────────────────────────────────────────

    static GameObject ResolverRoot(GameObject go)
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
}
