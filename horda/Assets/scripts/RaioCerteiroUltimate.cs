using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaioCerteiroUltimate : MonoBehaviour
{
    [Header("Configurações")]
    public int   maxRicochetes     = 5;
    public float danoPorRaio       = 60f;
    public float multiplicadorDano = 0.8f;   // cada bounce aplica 80% do anterior
    public float raioMaxBounce     = 8f;
    public float cooldown          = 25f;
    public float delayEntreBounces = 0.1f;

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
        if (Input.GetKeyDown(KeyCode.R) && cooldownRestante <= 0f && !ativo)
            StartCoroutine(CadeiaDeRaios());
        SincronizarUI();
    }

    void SincronizarUI()
    {
        if (playerStats == null) return;
        playerStats.ultimateChargeTime = cooldown - Mathf.Max(0f, cooldownRestante);
        playerStats.ultimateReady      = cooldownRestante <= 0f && !ativo;
    }

    // ─── LÓGICA PRINCIPAL ──────────────────────────────────────────────────────

    IEnumerator CadeiaDeRaios()
    {
        ativo            = true;
        cooldownRestante = cooldown;

        var    atingidos = new HashSet<GameObject>();
        Vector2 origem   = transform.position;
        float   dano     = danoPorRaio;

        for (int i = 0; i < maxRicochetes; i++)
        {
            GameObject alvo = EncontrarAlvoMaisProximo(origem, atingidos);
            if (alvo == null) break;

            atingidos.Add(alvo);
            AplicarDano(alvo, dano);
            StartCoroutine(AnimarRaio(origem, alvo.transform.position, i));

            origem = alvo.transform.position;
            dano  *= multiplicadorDano;

            yield return new WaitForSeconds(delayEntreBounces);
        }

        ativo = false;
    }

    void AplicarDano(GameObject alvo, float dano)
    {
        var ic = alvo.GetComponent<InimigoController>() ?? alvo.GetComponentInChildren<InimigoController>();
        ic?.ReceberDano(dano, false);
    }

    GameObject EncontrarAlvoMaisProximo(Vector2 origem, HashSet<GameObject> excluidos)
    {
        var     cols      = Physics2D.OverlapCircleAll(origem, raioMaxBounce);
        GameObject melhor = null;
        float   menorDist = float.MaxValue;

        foreach (var c in cols)
        {
            var root = ResolverInimigo(c.gameObject);
            if (root == null || excluidos.Contains(root)) continue;
            float dist = Vector2.Distance(origem, root.transform.position);
            if (dist < menorDist) { menorDist = dist; melhor = root; }
        }
        return melhor;
    }

    // ─── VISUAL ────────────────────────────────────────────────────────────────

    IEnumerator AnimarRaio(Vector2 de, Vector2 para, int bounce)
    {
        var go = new GameObject("RaioCerteiro");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 15;

        const int SEGS = 9;
        lr.positionCount = SEGS;
        for (int i = 0; i < SEGS; i++)
        {
            float   t = i / (float)(SEGS - 1);
            Vector2 p = Vector2.Lerp(de, para, t);
            if (i > 0 && i < SEGS - 1) p += Random.insideUnitCircle * 0.45f;
            lr.SetPosition(i, p);
        }

        float alpha = Mathf.Lerp(1f, 0.5f, bounce / (float)maxRicochetes);
        float width = Mathf.Lerp(0.22f, 0.08f, bounce / (float)maxRicochetes);
        lr.startWidth = lr.endWidth = width;
        lr.startColor = lr.endColor = new Color(0.75f, 0.95f, 1f, alpha);

        yield return null;

        for (float t = 0f; t < 0.2f; t += Time.deltaTime)
        {
            float p = t / 0.2f;
            Color c = Color.Lerp(new Color(0.75f, 0.95f, 1f, alpha), new Color(0.5f, 0.8f, 1f, 0f), p);
            lr.startColor = lr.endColor = c;
            lr.startWidth = lr.endWidth = Mathf.Lerp(width, 0.01f, p);
            yield return null;
        }

        Destroy(go);
    }

    // ─── HELPER ────────────────────────────────────────────────────────────────

    static GameObject ResolverInimigo(GameObject go)
    {
        var ic = go.GetComponent<InimigoController>() ?? go.GetComponentInParent<InimigoController>();
        if (ic != null) return ic.gameObject;
        var mi = go.GetComponent<movi_inimigo>() ?? go.GetComponentInParent<movi_inimigo>();
        if (mi != null) return mi.gameObject;
        if (go.GetComponentInParent<ProjetilHomingPrincesa>(true)   != null) return null;
        if (go.GetComponentInParent<ProjetilEspecialPrincesa>(true) != null) return null;
        return null;
    }
}
