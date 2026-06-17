using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrenagemDeVidaUltimate : MonoBehaviour
{
    [Header("Configurações")]
    public float raio            = 10f;
    public float duracao         = 7f;
    public float cooldown        = 26f;
    public float danoPorSegundo  = 18f;
    public float percentualCura  = 0.10f;

    public float CooldownRestante => cooldownRestante;
    public bool  Ativo            => ativo;

    private float       cooldownRestante;
    private bool        ativo;
    private PlayerStats playerStats;

    struct EstadoDrenado
    {
        public GameObject      go;
        public LineRenderer    feixe;
        public SpriteRenderer[] renderers;
        public Color[]          coresOriginais;
    }
    readonly List<EstadoDrenado> drenados = new List<EstadoDrenado>();

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
        ativo            = true;
        cooldownRestante = cooldown;

        // DrenagemMassiva: +50% de raio
        float raioOriginal = raio;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.DrenagemMassiva))
            raio *= 1.5f;

        var auraGO = CriarAura();
        yield return StartCoroutine(AnimarEntrada(auraGO));

        float elapsed = 0f;
        while (elapsed < duracao)
        {
            elapsed += Time.deltaTime;

            AtualizarDrenados();
            AplicarDrenagemECura();
            AtualizarFeixes();
            AtualizarTintDrenados(elapsed);
            AnimarAura(auraGO, elapsed);

            yield return null;
        }

        LimparDrenados();
        raio = raioOriginal; // restaura raio base
        ativo = false;
        StartCoroutine(FadeOutDestruir(auraGO, 0.35f));
    }

    // ─── LÓGICA ─────────────────────────────────────────────────────────────

    const int MAX_DRENADOS = 6;

    void AtualizarDrenados()
    {
        var noRaio = new HashSet<GameObject>();
        foreach (var c in Physics2D.OverlapCircleAll(transform.position, raio))
        {
            if (c.gameObject == gameObject) continue;
            var root = ResolverInimigo(c.gameObject);
            if (root != null) noRaio.Add(root);
        }

        foreach (var go in noRaio)
            if (!JaDrenado(go) && drenados.Count < MAX_DRENADOS) AdicionarDrenado(go);

        for (int i = drenados.Count - 1; i >= 0; i--)
        {
            if (drenados[i].go == null || !noRaio.Contains(drenados[i].go))
            {
                RestaurarCores(drenados[i]);
                if (drenados[i].feixe != null) Destroy(drenados[i].feixe.gameObject);
                drenados.RemoveAt(i);
            }
        }
    }

    void AdicionarDrenado(GameObject go)
    {
        var srs   = go.GetComponentsInChildren<SpriteRenderer>();
        var cores = new Color[srs.Length];
        for (int i = 0; i < srs.Length; i++) cores[i] = srs[i].color;

        drenados.Add(new EstadoDrenado
        {
            go             = go,
            feixe          = CriarFeixe(),
            renderers      = srs,
            coresOriginais = cores,
        });
    }

    bool JaDrenado(GameObject go)
    {
        foreach (var e in drenados) if (e.go == go) return true;
        return false;
    }

    void AplicarDrenagemECura()
    {
        // DrenagemTotal: percentual de cura para 25%
        float percentualEfetivo = percentualCura;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.DrenagemTotal))
            percentualEfetivo = 0.25f;

        float totalDano = 0f;
        foreach (var e in drenados)
        {
            if (e.go == null) continue;
            var ic = e.go.GetComponent<InimigoController>();
            if (ic == null) continue;
            float dano = danoPorSegundo * Time.deltaTime;
            ic.ReceberDano(dano, false, false);
            totalDano += dano;
        }

        if (totalDano > 0f && playerStats != null)
            playerStats.Heal(totalDano * percentualEfetivo);
    }

    void AtualizarTintDrenados(float elapsed)
    {
        float pulso = Mathf.Sin(elapsed * 8f) * 0.5f + 0.5f;
        Color tint  = new Color(1f, 0.3f + pulso * 0.1f, 0.4f + pulso * 0.1f);
        foreach (var e in drenados)
            for (int i = 0; i < e.renderers.Length; i++)
                if (e.renderers[i] != null)
                    e.renderers[i].color = Color.Lerp(e.coresOriginais[i], tint, 0.55f + pulso * 0.15f);
    }

    void RestaurarCores(EstadoDrenado e)
    {
        for (int i = 0; i < e.renderers.Length; i++)
            if (e.renderers[i] != null) e.renderers[i].color = e.coresOriginais[i];
    }

    void LimparDrenados()
    {
        foreach (var e in drenados)
        {
            RestaurarCores(e);
            if (e.feixe != null) Destroy(e.feixe.gameObject);
        }
        drenados.Clear();
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

    GameObject CriarAura()
    {
        var root = new GameObject("AuraDrenagem");
        root.transform.position = transform.position;

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
        lr.startColor    = lr.endColor = new Color(0.8f, 0.1f, 0.4f, 0.85f);
        for (int i = 0; i < SEGS; i++)
        {
            float a = (360f / SEGS) * i * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * raio, Mathf.Sin(a) * raio));
        }

        return root;
    }

    IEnumerator AnimarEntrada(GameObject root)
    {
        var lr = root.GetComponentInChildren<LineRenderer>();
        float dur = 0.22f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            root.transform.position = transform.position;
            float p = t / dur;
            if (lr != null) { Color c = lr.startColor; c.a = p * 0.85f; lr.startColor = lr.endColor = c; }
            yield return null;
        }
    }

    void AnimarAura(GameObject root, float elapsed)
    {
        root.transform.position = transform.position;
        var lr = root.GetComponentInChildren<LineRenderer>();
        if (lr == null) return;

        const int SEGS = 48;
        float pulso = Mathf.Sin(elapsed * 6f) * 0.5f + 0.5f;
        lr.startColor = lr.endColor = new Color(0.9f, 0.1f + pulso * 0.2f, 0.5f, 0.5f + pulso * 0.4f);
        lr.startWidth = lr.endWidth = 0.06f + pulso * 0.06f;

        // Jitter no anel
        for (int i = 0; i < SEGS; i++)
        {
            float a   = (360f / SEGS) * i * Mathf.Deg2Rad;
            float jit = Mathf.Sin(elapsed * 10f + i * 0.5f) * 0.07f;
            float r   = raio + jit;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
        }
    }

    LineRenderer CriarFeixe()
    {
        var go = new GameObject("FeixeDrenagem");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 8;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 14;
        lr.startWidth    = 0.07f;
        lr.endWidth      = 0.03f;
        lr.startColor    = new Color(0.9f, 0.1f, 0.4f, 0.9f);
        lr.endColor      = new Color(1f,   0.6f, 0.8f, 0.6f);
        return lr;
    }

    void AtualizarFeixes()
    {
        Vector3 destino = transform.position;
        foreach (var e in drenados)
        {
            if (e.feixe == null || e.go == null) continue;
            Vector3 origem = e.go.transform.position;
            int segs       = e.feixe.positionCount;
            for (int i = 0; i < segs; i++)
            {
                float t   = i / (float)(segs - 1);
                Vector3 p = Vector3.Lerp(origem, destino, t);
                Vector3 perp = new Vector3(-(destino - origem).y, (destino - origem).x, 0f).normalized;
                p += perp * Mathf.Sin(t * Mathf.PI) * Random.Range(-0.3f, 0.3f);
                e.feixe.SetPosition(i, p);
            }
            Color c = e.feixe.startColor;
            c.a = Random.value > 0.1f ? 0.9f : 0.3f;
            e.feixe.startColor = c;
        }
    }

    IEnumerator FadeOutDestruir(GameObject go, float dur)
    {
        var lrs = go.GetComponentsInChildren<LineRenderer>();
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            foreach (var lr in lrs) { Color c = lr.startColor; c.a = Mathf.Lerp(c.a, 0f, p); lr.startColor = lr.endColor = c; }
            yield return null;
        }
        if (go != null) Destroy(go);
    }
}
