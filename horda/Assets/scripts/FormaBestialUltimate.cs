using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FormaBestialUltimate : MonoBehaviour
{
    [Header("Configurações")]
    public float cooldown               = 32f;
    public float duracao                = 6f;
    public float multiplicadorVelocidade = 2f;

    [Header("Melee")]
    public float danoMelee      = 22f;
    public float raioMelee      = 2f;
    public float intervaloMelee = 0.35f;

    [Header("Rugido")]
    public float danoRugido      = 14f;
    public float forcaRugido     = 16f;
    public float raioRugido      = 5.5f;
    public float intervaloRugido = 2f;

    public float CooldownRestante => cooldownRestante;
    public bool  Ativo            => ativo;

    private float        cooldownRestante;
    private bool         ativo;
    private PlayerStats  playerStats;
    private SpriteRenderer sr;
    private Color        corOriginal;
    private float        velocidadeOriginal;
    private GameObject   auraAtiva;

    void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        sr          = GetComponent<SpriteRenderer>();
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
        if (Input.GetKeyDown(KeyCode.R) && cooldownRestante <= 0f && !ativo && !playerStats.ultimateBloqueada)
            StartCoroutine(CorotinaAtivacao());
        SincronizarUI();
    }

    void SincronizarUI()
    {
        if (playerStats == null) return;
        playerStats.ultimateChargeTime = cooldown - Mathf.Max(0f, cooldownRestante);
        playerStats.ultimateReady      = cooldownRestante <= 0f && !ativo && !playerStats.ultimateBloqueada;
    }

    void OnDestroy()
    {
        if (!ativo) return;
        if (playerStats != null)
        {
            playerStats.speed = velocidadeOriginal;
            playerStats.CancelarTimerSlow();
        }
        if (sr != null) sr.color = corOriginal;
        var sm = SkillManager.Instance;
        if (sm != null) sm.enabled = true;
        if (auraAtiva != null) Destroy(auraAtiva);
    }

    // ── Coroutine principal ──────────────────────────────────────────────────

    IEnumerator CorotinaAtivacao()
    {
        ativo            = true;
        cooldownRestante = cooldown;

        velocidadeOriginal = playerStats.speed;
        playerStats.speed  = velocidadeOriginal * multiplicadorVelocidade;

        var sm = SkillManager.Instance;
        if (sm != null) sm.enabled = false;

        if (sr != null) { corOriginal = sr.color; sr.color = new Color(1f, 0.5f, 0.05f); }
        auraAtiva = CriarAura();

        // Rugido imediato ao ativar
        Rugido();

        float elapsed    = 0f;
        float proxMelee  = 0f;
        float proxRugido = intervaloRugido;

        while (elapsed < duracao)
        {
            elapsed    += Time.deltaTime;
            proxMelee  -= Time.deltaTime;
            proxRugido -= Time.deltaTime;

            if (auraAtiva != null) auraAtiva.transform.position = transform.position;

            if (proxMelee <= 0f)
            {
                proxMelee = intervaloMelee;
                AtaqueMelee();
            }

            if (proxRugido <= 0f)
            {
                proxRugido = intervaloRugido;
                Rugido();
            }

            yield return null;
        }

        // Burst final ao sair da forma
        BurstFinal();

        if (sr != null) sr.color = corOriginal;
        playerStats.speed = velocidadeOriginal;
        playerStats.CancelarTimerSlow();
        if (sm != null) sm.enabled = true;
        if (auraAtiva != null) { Destroy(auraAtiva); auraAtiva = null; }
        ativo = false;
    }

    // ── Lógica ──────────────────────────────────────────────────────────────

    void AtaqueMelee()
    {
        Vector2 centro = transform.position;
        foreach (var col in Physics2D.OverlapCircleAll(centro, raioMelee))
        {
            if (col.gameObject == gameObject) continue;
            var ic = col.GetComponent<InimigoController>()
                  ?? col.GetComponentInParent<InimigoController>();
            if (ic != null && !ic.estaMorrendo)
                ic.ReceberDano(danoMelee);
        }
    }

    void Rugido()
    {
        Vector2 centro = transform.position;
        foreach (var col in Physics2D.OverlapCircleAll(centro, raioRugido))
        {
            if (col.gameObject == gameObject) continue;
            var ic = col.GetComponent<InimigoController>()
                  ?? col.GetComponentInParent<InimigoController>();
            if (ic == null) continue;

            ic.ReceberDano(danoRugido);
            var rb = col.GetComponent<Rigidbody2D>() ?? ic.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 dir = ((Vector2)ic.transform.position - centro).normalized;
                if (dir == Vector2.zero) dir = Random.insideUnitCircle.normalized;
                rb.AddForce(dir * forcaRugido, ForceMode2D.Impulse);
            }
        }

        StartCoroutine(AnimarOndaRugido());
    }

    void BurstFinal()
    {
        float raioFinal = raioRugido * 1.6f;
        Vector2 centro  = transform.position;

        foreach (var col in Physics2D.OverlapCircleAll(centro, raioFinal))
        {
            if (col.gameObject == gameObject) continue;
            var ic = col.GetComponent<InimigoController>()
                  ?? col.GetComponentInParent<InimigoController>();
            if (ic == null) continue;

            ic.ReceberDano(danoMelee * 3f);
            var rb = col.GetComponent<Rigidbody2D>() ?? ic.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 dir = ((Vector2)ic.transform.position - centro).normalized;
                if (dir == Vector2.zero) dir = Random.insideUnitCircle.normalized;
                rb.AddForce(dir * forcaRugido * 2.2f, ForceMode2D.Impulse);
            }
        }

        StartCoroutine(AnimarBurstFinal(raioFinal));
    }

    // ── Visuais ──────────────────────────────────────────────────────────────

    GameObject CriarAura()
    {
        var root = new GameObject("AuraFormaBestial");
        root.transform.position = transform.position;

        CriarAnelLocal(root, 0.75f, new Color(1f, 0.6f,  0.1f, 0.60f), 0.10f, "AuraInt");
        CriarAnelLocal(root, 1.30f, new Color(1f, 0.4f,  0.0f, 0.38f), 0.07f, "AuraMed");
        CriarAnelLocal(root, 2.00f, new Color(1f, 0.25f, 0.0f, 0.20f), 0.05f, "AuraExt");

        StartCoroutine(AnimarAura(root));
        return root;
    }

    static void CriarAnelLocal(GameObject parent, float r, Color cor, float larg, string nome)
    {
        const int SEGS = 40;
        var go = new GameObject(nome);
        go.transform.SetParent(parent.transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 12;
        lr.startWidth    = lr.endWidth = larg;
        lr.startColor    = lr.endColor = cor;
        for (int i = 0; i < SEGS; i++)
        {
            float a = (360f / SEGS) * i * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
        }
    }

    IEnumerator AnimarAura(GameObject root)
    {
        float t = 0f;
        while (root != null)
        {
            t += Time.deltaTime;
            float pulso = Mathf.Sin(t * 6f) * 0.5f + 0.5f;

            root.transform.Rotate(0f, 0f, Time.deltaTime * 55f);

            var lrs = root.GetComponentsInChildren<LineRenderer>();
            foreach (var lr in lrs)
            {
                Color c = lr.startColor;
                c.a = Mathf.Clamp01(c.a * 0.75f + pulso * 0.25f);
                lr.startColor = lr.endColor = c;
            }

            yield return null;
        }
    }

    IEnumerator AnimarOndaRugido()
    {
        var go = new GameObject("OndaRugido");
        go.transform.position = transform.position;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = 48;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 14;

        Vector2 centro = go.transform.position;
        float   dur    = 0.5f;

        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p    = t / dur;
            float r    = Mathf.Lerp(0.1f, raioRugido, p);
            float a    = Mathf.Lerp(0.95f, 0f, p);
            float larg = Mathf.Lerp(0.38f, 0.04f, p);

            lr.startColor = lr.endColor = new Color(1f, 0.3f, 0f, a);
            lr.startWidth = lr.endWidth = larg;

            for (int i = 0; i < 48; i++)
            {
                float ang = i / 48f * Mathf.PI * 2f;
                lr.SetPosition(i, centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r);
            }
            yield return null;
        }

        if (go != null) Destroy(go);
    }

    IEnumerator AnimarBurstFinal(float raioFinal)
    {
        Vector3 centro = transform.position;

        // Anel principal
        var go = new GameObject("BurstFinal");
        go.transform.position = centro;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = 48;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 15;

        // 12 raios radiais
        const int QTD_RAIOS = 12;
        var raios = new List<(GameObject go, LineRenderer lr)>();
        for (int i = 0; i < QTD_RAIOS; i++)
        {
            var rGO = new GameObject($"RaioBestial_{i}");
            rGO.transform.position = centro;
            var rLR = rGO.AddComponent<LineRenderer>();
            rLR.useWorldSpace = true;
            rLR.positionCount = 2;
            rLR.material      = new Material(Shader.Find("Sprites/Default"));
            rLR.sortingOrder  = 14;
            raios.Add((rGO, rLR));
        }

        float dur = 0.7f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p    = t / dur;
            float r    = Mathf.Lerp(0.1f, raioFinal, Mathf.Pow(p, 0.5f));
            float a    = Mathf.Lerp(1f,   0f, p);
            float larg = Mathf.Lerp(0.55f, 0.02f, p);

            lr.startColor = lr.endColor = new Color(1f, 0.45f, 0.05f, a);
            lr.startWidth = lr.endWidth = larg;

            for (int i = 0; i < 48; i++)
            {
                float ang = i / 48f * Mathf.PI * 2f;
                lr.SetPosition(i, centro + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang)) * r);
            }

            for (int i = 0; i < QTD_RAIOS; i++)
            {
                var (rGO, rLR) = raios[i];
                if (rGO == null) continue;
                float ang     = (360f / QTD_RAIOS) * i * Mathf.Deg2Rad;
                Vector2 dir   = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                float inicio  = r * 0.8f;
                float fim     = r + raioFinal * 0.25f * p;
                rLR.SetPosition(0, (Vector3)((Vector2)centro + dir * inicio));
                rLR.SetPosition(1, (Vector3)((Vector2)centro + dir * fim));
                rLR.startWidth = rLR.endWidth = Mathf.Lerp(0.14f, 0.01f, p);
                rLR.startColor = rLR.endColor = new Color(1f, 0.6f, 0.1f, a * 0.8f);
            }

            yield return null;
        }

        if (go != null) Destroy(go);
        foreach (var (rGO, _) in raios)
            if (rGO != null) Destroy(rGO);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, raioMelee);
        Gizmos.color = new Color(1f, 0.25f, 0f, 0.20f);
        Gizmos.DrawWireSphere(transform.position, raioRugido);
    }
}
