using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TempestadeEletricaUltimate : MonoBehaviour, IUltimateCosmetico
{
    bool cosmetico;
    public void ExecutarCosmetico() { if (ativo) return; cosmetico = true; StartCoroutine(CorotinaAtivacao()); }

    [Header("Configurações")]
    public float raio          = 10f;
    public float duracao       = 5f;
    public float cooldown      = 20f;
    public float intervaloBolt = 0.45f;
    public float danoPorBolt   = 20f;

    public float CooldownRestante => cooldownRestante;
    public bool  Ativo            => ativo;

    private float cooldownRestante;
    private bool  ativo;
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

    // ─── COROUTINE PRINCIPAL ────────────────────────────────────────────────

    IEnumerator CorotinaAtivacao()
    {
        ativo            = true;
        cooldownRestante = cooldown;

        Vector2 posicaoCampo = transform.position;

        // TempestadeIntensa: +50% raio
        float raioEfetivo = raio;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.TempestadeIntensa))
            raioEfetivo *= 1.5f;

        // TempestadeContinua: +3s de duração
        float duracaoEfetiva = duracao;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.TempestadeContinua))
            duracaoEfetiva += 3f;

        // Temporariamente aumenta o raio para criação do visual
        float raioOriginal = raio;
        raio = raioEfetivo;

        GameObject ringGO = CriarAnelEletrico(posicaoCampo);
        SomSkill.Tocar(SomSkill.Tipo.TempestadeInicio, posicaoCampo, 0.7f);
        yield return StartCoroutine(AnimarEntrada(ringGO));

        raio = raioOriginal;

        // Ronco ambiente da tempestade durante toda a duração (parentado ao anel = no centro do campo)
        var stormSrc = SomSkill.TocarLoop(SomSkill.Tipo.TempestadeLoop, ringGO.transform, 0.4f);

        float elapsed = 0f;
        float proximo = 0f;
        while (elapsed < duracaoEfetiva)
        {
            elapsed += Time.deltaTime;
            proximo -= Time.deltaTime;
            AnimarAnel(ringGO, elapsed);

            if (proximo <= 0f)
            {
                proximo = intervaloBolt;
                DispararRaio(posicaoCampo, raioEfetivo);
                // TempestadeIntensa: dispara 2 raios simultâneos
                if (SkillEvolutionManager.Tem(SkillEvolutionType.TempestadeIntensa))
                    DispararRaio(posicaoCampo, raioEfetivo);
            }
            yield return null;
        }

        ativo = false;
        if (stormSrc != null) Destroy(stormSrc.gameObject); // para o ronco ao fim da tempestade
        StartCoroutine(FadeOutDestruir(ringGO, 0.35f));
    }

    // ─── LÓGICA ─────────────────────────────────────────────────────────────

    void DispararRaio(Vector2 posicaoCampo, float raioEfetivo = -1f)
    {
        if (raioEfetivo < 0f) raioEfetivo = raio;
        var cols = Physics2D.OverlapCircleAll(posicaoCampo, raioEfetivo);
        var candidatos = new List<GameObject>();

        foreach (var c in cols)
        {
            if (c.gameObject == gameObject) continue;
            var root = ResolverInimigo(c.gameObject);
            if (root != null && !candidatos.Contains(root)) candidatos.Add(root);
        }

        if (candidatos.Count == 0) return;

        GameObject alvoGO = candidatos[Random.Range(0, candidatos.Count)];
        SomSkill.Tocar(SomSkill.Tipo.TempestadeRaio, alvoGO.transform.position, 0.45f);
        StartCoroutine(AnimarRaio(alvoGO.transform.position));

        var ic = alvoGO.GetComponent<InimigoController>();
        if (ic != null && !cosmetico) ic.ReceberDano(danoPorBolt, false);
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

    // ─── VISUAIS ─────────────────────────────────────────────────────────────

    IEnumerator AnimarRaio(Vector2 alvo)
    {
        var go = new GameObject("Raio");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace  = true;
        lr.material       = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder   = 15;
        lr.positionCount  = 7;

        Vector2 origem = alvo + Vector2.up * 7f;
        for (int i = 0; i < 7; i++)
        {
            float t = i / 6f;
            Vector2 p = Vector2.Lerp(origem, alvo, t);
            if (i > 0 && i < 6) p += Random.insideUnitCircle * 0.65f;
            lr.SetPosition(i, p);
        }

        // Flash branco instantâneo
        lr.startWidth = lr.endWidth = 0.18f;
        lr.startColor = lr.endColor = Color.white;
        yield return null;

        // Fade amarelo
        for (float t = 0f; t < 0.22f; t += Time.deltaTime)
        {
            float p = t / 0.22f;
            Color c = Color.Lerp(new Color(1f, 0.9f, 0.15f, 1f), new Color(1f, 0.9f, 0.1f, 0f), p);
            lr.startColor   = lr.endColor  = c;
            lr.startWidth   = lr.endWidth  = Mathf.Lerp(0.14f, 0.02f, p);
            yield return null;
        }
        Destroy(go);
    }

    GameObject CriarAnelEletrico(Vector2 posicao)
    {
        var root = new GameObject("AnelEletrico");
        root.transform.position = posicao;

        const int SEGS = 32;
        var child = new GameObject("Anel");
        child.transform.SetParent(root.transform, false);
        var lr = child.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 12;
        lr.startWidth    = lr.endWidth = 0.08f;
        lr.startColor    = lr.endColor = new Color(1f, 0.9f, 0.1f, 0.9f);
        for (int i = 0; i < SEGS; i++)
        {
            float ang = (360f / SEGS) * i * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(ang) * raio, Mathf.Sin(ang) * raio));
        }
        return root;
    }

    IEnumerator AnimarEntrada(GameObject root)
    {
        var lr = root.GetComponentInChildren<LineRenderer>();
        const int SEGS = 32;
        float t = 0f, dur = 0.2f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / dur);
            float r = raio * p;
            if (lr != null)
                for (int i = 0; i < SEGS; i++)
                {
                    float ang = (360f / SEGS) * i * Mathf.Deg2Rad;
                    lr.SetPosition(i, new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r));
                }
            yield return null;
        }
    }

    void AnimarAnel(GameObject root, float t)
    {
        var lr = root.GetComponentInChildren<LineRenderer>();
        if (lr == null) return;
        float pulso = Mathf.Sin(t * 9f) * 0.5f + 0.5f;
        lr.startColor = lr.endColor = new Color(1f, 0.9f, 0.1f, 0.45f + pulso * 0.45f);
        lr.startWidth = lr.endWidth = 0.05f + pulso * 0.07f;
    }

    IEnumerator FadeOutDestruir(GameObject go, float dur)
    {
        var lr = go.GetComponentInChildren<LineRenderer>();
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (lr != null)
            {
                Color c = lr.startColor; c.a = Mathf.Lerp(1f, 0f, t / dur);
                lr.startColor = lr.endColor = c;
            }
            yield return null;
        }
        if (go != null) Destroy(go);
    }
}
