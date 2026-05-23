using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PunicaoDivinaUltimate : MonoBehaviour
{
    [Header("Configurações")]
    public float raioDeteccao    = 25f;
    public float danoImpacto     = 80f;
    public float danoSecundario  = 32f;
    public float raioExplosao    = 4f;
    public float cooldown        = 25f;
    public int   numSecundarios  = 3;

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

        // Encontra alvo principal
        var alvoGO = EncontrarAlvoPrincipal();
        if (alvoGO == null) { ativo = false; yield break; }

        Vector2 posAlvo = alvoGO.transform.position;

        // Telegráfico: marcador dourado sobre o alvo
        var marcador = CriarMarcador(posAlvo);
        yield return StartCoroutine(AnimarMarcador(marcador, 0.7f));
        Destroy(marcador);

        // Raio principal
        yield return StartCoroutine(AnimarRaioPrincipal(posAlvo));

        // Dano principal
        AplicarDano(posAlvo, danoImpacto, true);

        // Explosão de luz + onda de choque
        StartCoroutine(AnimarExplosao(posAlvo));
        StartCoroutine(AnimarOndaChoque(posAlvo));

        // Pequena pausa dramática
        yield return new WaitForSeconds(0.1f);

        // Raios secundários nos inimigos próximos
        var secundarios = EncontrarSecundarios(posAlvo, alvoGO);
        foreach (var sec in secundarios)
        {
            Vector2 posSec = sec.transform.position;
            var marcSec = CriarMarcadorPequeno(posSec);
            yield return new WaitForSeconds(0.18f);
            Destroy(marcSec);
            StartCoroutine(AnimarRaioSecundario(posSec));
            AplicarDano(posSec, danoSecundario, false);
            StartCoroutine(AnimarExplosaoSecundaria(posSec));
        }

        yield return new WaitForSeconds(0.5f);
        ativo = false;
    }

    // ─── LÓGICA ─────────────────────────────────────────────────────────────

    GameObject EncontrarAlvoPrincipal()
    {
        float distMin = float.MaxValue;
        GameObject melhor = null;
        foreach (var c in Physics2D.OverlapCircleAll(transform.position, raioDeteccao))
        {
            if (c.gameObject == gameObject) continue;
            var root = ResolverInimigo(c.gameObject);
            if (root == null) continue;
            float d = Vector2.Distance(transform.position, root.transform.position);
            if (d < distMin) { distMin = d; melhor = root; }
        }
        return melhor;
    }

    List<GameObject> EncontrarSecundarios(Vector2 centro, GameObject excluir)
    {
        var lista = new List<GameObject>();
        var vistos = new HashSet<GameObject> { excluir };
        foreach (var c in Physics2D.OverlapCircleAll(centro, raioDeteccao))
        {
            if (lista.Count >= numSecundarios) break;
            var root = ResolverInimigo(c.gameObject);
            if (root == null || vistos.Contains(root)) continue;
            vistos.Add(root);
            lista.Add(root);
        }
        return lista;
    }

    void AplicarDano(Vector2 centro, float dano, bool podeCrit)
    {
        foreach (var c in Physics2D.OverlapCircleAll(centro, raioExplosao))
        {
            var ic = c.GetComponent<InimigoController>() ?? c.GetComponentInParent<InimigoController>();
            if (ic != null)
            {
                bool crit = podeCrit && Random.value < 0.3f;
                ic.ReceberDano(crit ? dano * 1.5f : dano, crit);
            }
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
        if (root.GetComponent<ProjetilHomingPrincesa>()   != null) return null;
        if (root.GetComponent<ProjetilEspecialPrincesa>() != null) return null;
        return root;
    }

    // ─── VISUAIS ────────────────────────────────────────────────────────────

    GameObject CriarMarcador(Vector2 pos)
    {
        var root = new GameObject("MarcadorDivino");
        root.transform.position = pos;

        // Cruz dourada
        for (int i = 0; i < 4; i++)
        {
            float ang = i * 90f * Mathf.Deg2Rad;
            var linha = new GameObject($"Linha{i}");
            linha.transform.SetParent(root.transform, false);
            var lr = linha.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.material      = new Material(Shader.Find("Sprites/Default"));
            lr.sortingOrder  = 15;
            lr.startWidth    = lr.endWidth = 0.08f;
            lr.startColor    = lr.endColor = new Color(1f, 0.9f, 0.2f, 0.9f);
            lr.SetPosition(0, pos);
            lr.SetPosition(1, pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raioExplosao);
        }

        // Círculo pulsante
        const int SEGS = 24;
        var anel = new GameObject("Anel");
        anel.transform.SetParent(root.transform, false);
        var lrA = anel.AddComponent<LineRenderer>();
        lrA.useWorldSpace = true;
        lrA.loop          = true;
        lrA.positionCount = SEGS;
        lrA.material      = new Material(Shader.Find("Sprites/Default"));
        lrA.sortingOrder  = 14;
        lrA.startWidth    = lrA.endWidth = 0.07f;
        lrA.startColor    = lrA.endColor = new Color(1f, 0.85f, 0.1f, 0.8f);
        for (int i = 0; i < SEGS; i++)
        {
            float a = (360f / SEGS) * i * Mathf.Deg2Rad;
            lrA.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * raioExplosao);
        }

        return root;
    }

    IEnumerator AnimarMarcador(GameObject root, float dur)
    {
        var lrs = root.GetComponentsInChildren<LineRenderer>();
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p    = t / dur;
            float pulso = Mathf.Sin(p * Mathf.PI * 6f) * 0.5f + 0.5f;
            foreach (var lr in lrs)
            {
                Color c = lr.startColor;
                c.a = 0.5f + pulso * 0.5f;
                lr.startColor = lr.endColor = c;
                lr.startWidth = lr.endWidth = 0.06f + pulso * 0.04f;
            }
            yield return null;
        }
    }

    GameObject CriarMarcadorPequeno(Vector2 pos)
    {
        const int SEGS = 16;
        var go = new GameObject("MarcadorSec");
        go.transform.position = pos;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 13;
        lr.startWidth    = lr.endWidth = 0.05f;
        lr.startColor    = lr.endColor = new Color(1f, 0.8f, 0.1f, 0.7f);
        for (int i = 0; i < SEGS; i++)
        {
            float a = (360f / SEGS) * i * Mathf.Deg2Rad;
            lr.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * raioExplosao * 0.6f);
        }
        return go;
    }

    IEnumerator AnimarRaioPrincipal(Vector2 alvo)
    {
        Vector2 origem = alvo + Vector2.up * 14f;

        var go = new GameObject("RaioDivino");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 18;
        lr.SetPosition(0, origem);
        lr.SetPosition(1, alvo);

        // Flash branco instantâneo — largo
        lr.startWidth = 2.5f;
        lr.endWidth   = 0.4f;
        lr.startColor = lr.endColor = Color.white;
        yield return null;
        yield return null;

        // Fade dourado
        for (float t = 0f; t < 2.2f; t += Time.deltaTime)
        {
            float p = t / 0.35f;
            lr.startWidth = Mathf.Lerp(2.5f, 0.1f, p);
            lr.endWidth   = Mathf.Lerp(0.4f, 0.05f, p);
            lr.startColor = lr.endColor = Color.Lerp(Color.white, new Color(1f, 0.85f, 0.1f, 0f), p);
            lr.SetPosition(0, origem); lr.SetPosition(1, alvo);
            yield return null;
        }
        Destroy(go);
    }

    IEnumerator AnimarRaioSecundario(Vector2 alvo)
    {
        Vector2 origem = alvo + Vector2.up * 10f;

        var go = new GameObject("RaioSec");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 17;
        lr.SetPosition(0, origem);
        lr.SetPosition(1, alvo);

        lr.startWidth = 1.2f;
        lr.endWidth   = 0.2f;
        lr.startColor = lr.endColor = Color.white;
        yield return null;

        for (float t = 0f; t < 1.5f; t += Time.deltaTime)
        {
            float p = t / 1.5f;
            lr.startWidth = Mathf.Lerp(1.2f, 0.05f, p);
            lr.endWidth   = Mathf.Lerp(0.2f, 0.02f, p);
            lr.startColor = lr.endColor = Color.Lerp(new Color(1f, 0.9f, 0.3f, 1f), new Color(1f, 0.85f, 0.1f, 0f), p);
            lr.SetPosition(0, origem); lr.SetPosition(1, alvo);
            yield return null;
        }
        Destroy(go);
    }

    IEnumerator AnimarOndaChoque(Vector2 pos)
    {
        const int SEGS = 40;
        float maxR = raioExplosao * 4f;

        // Duas ondas concêntricas com leve delay entre elas
        for (int onda = 0; onda < 2; onda++)
        {
            if (onda == 1) yield return new WaitForSeconds(0.08f);

            var go = new GameObject($"OndaChoque{onda}");
            go.transform.position = pos;
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop          = true;
            lr.positionCount = SEGS;
            lr.material      = new Material(Shader.Find("Sprites/Default"));
            lr.sortingOrder  = 17;

            float dur = 0.45f;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                float p = t / dur;
                // Expansão rápida com ease-out
                float r = Mathf.Lerp(0.2f, maxR, 1f - Mathf.Pow(1f - p, 2f));
                lr.startWidth = lr.endWidth = Mathf.Lerp(0.45f, 0.02f, p);
                lr.startColor = lr.endColor = new Color(1f, Mathf.Lerp(1f, 0.6f, p), Mathf.Lerp(0.8f, 0f, p), 1f - p);
                for (int i = 0; i < SEGS; i++)
                {
                    float a = (360f / SEGS) * i * Mathf.Deg2Rad;
                    lr.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
                }
                yield return null;
            }
            Destroy(go);
        }
    }

    IEnumerator AnimarExplosao(Vector2 pos)
    {
        const int SEGS = 32;
        var go = new GameObject("ExplosaoDivina");
        go.transform.position = pos;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 16;

        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            float p = t / 0.5f;
            float r = Mathf.Lerp(0f, raioExplosao * 1.3f, p);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.5f, 0.03f, p);
            lr.startColor = lr.endColor = new Color(1f, Mathf.Lerp(1f, 0.7f, p), Mathf.Lerp(1f, 0f, p), 1f - p);
            for (int i = 0; i < SEGS; i++)
            {
                float a = (360f / SEGS) * i * Mathf.Deg2Rad;
                lr.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
            yield return null;
        }
        Destroy(go);
    }

    IEnumerator AnimarExplosaoSecundaria(Vector2 pos)
    {
        const int SEGS = 20;
        var go = new GameObject("ExplosaoSec");
        go.transform.position = pos;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 15;

        float maxR = raioExplosao * 0.8f;
        for (float t = 0f; t < 0.35f; t += Time.deltaTime)
        {
            float p = t / 0.35f;
            float r = Mathf.Lerp(0f, maxR, p);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.3f, 0.02f, p);
            lr.startColor = lr.endColor = new Color(1f, Mathf.Lerp(0.9f, 0.6f, p), 0f, 1f - p);
            for (int i = 0; i < SEGS; i++)
            {
                float a = (360f / SEGS) * i * Mathf.Deg2Rad;
                lr.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
            yield return null;
        }
        Destroy(go);
    }
}
