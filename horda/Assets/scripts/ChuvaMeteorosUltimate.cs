using System.Collections;
using UnityEngine;

public class ChuvaMeteorosUltimate : MonoBehaviour
{
    [Header("Configurações")]
    public float raio              = 14f;
    public float duracao           = 8f;
    public float cooldown          = 25f;
    public float intervaloMeteoro  = 0.65f;
    public float danoMeteoro       = 25f;
    public float raioImpacto       = 2.4f;

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

        float elapsed = 0f;
        float proximo  = 0f;
        while (elapsed < duracao)
        {
            elapsed += Time.deltaTime;
            proximo  -= Time.deltaTime;
            if (proximo <= 0f)
            {
                proximo = intervaloMeteoro;
                StartCoroutine(AnimarMeteoro(EscolherAlvo()));
            }
            yield return null;
        }

        ativo = false;
    }

    // ─── LÓGICA ─────────────────────────────────────────────────────────────

    Vector2 EscolherAlvo()
    {
        foreach (var c in Physics2D.OverlapCircleAll(transform.position, raio))
        {
            if (c.gameObject == gameObject) continue;
            var root = ResolverInimigo(c.gameObject);
            if (root != null) return root.transform.position;
        }
        return (Vector2)transform.position + Random.insideUnitCircle * raio;
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

    void AplicarDano(Vector2 centro)
    {
        // MeteorosMaior: +30% dano e +50% raio
        float raioEfetivo = raioImpacto;
        float danoEfetivo = danoMeteoro;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.MeteorosMaior))
        {
            raioEfetivo *= 1.5f;
            danoEfetivo *= 1.3f;
        }

        foreach (var c in Physics2D.OverlapCircleAll(centro, raioEfetivo))
        {
            var ic = c.GetComponent<InimigoController>() ?? c.GetComponentInParent<InimigoController>();
            if (ic != null)
            {
                ic.ReceberDano(danoEfetivo, false);
                // MeteorosDuploImpacto: aplica dano duas vezes
                if (SkillEvolutionManager.Tem(SkillEvolutionType.MeteorosDuploImpacto))
                    ic.ReceberDano(danoEfetivo, false);
            }
        }
    }

    // ─── VISUAIS ────────────────────────────────────────────────────────────

    IEnumerator AnimarMeteoro(Vector2 destino)
    {
        Vector2 origem = destino + new Vector2(Random.Range(-0.8f, 0.8f), 7f);

        // Marcador de alvo (círculo vermelho)
        var marcador = CriarMarcadorAlvo(destino);

        // Corpo do meteoro
        var goM = new GameObject("Meteoro");
        var sr  = goM.AddComponent<SpriteRenderer>();
        sr.sprite       = GerarSprite(16, new Color(1f, 0.45f, 0.1f));
        sr.sortingOrder = 13;
        goM.transform.localScale = Vector3.one * 0.9f;

        float ang = Mathf.Atan2(destino.y - origem.y, destino.x - origem.x) * Mathf.Rad2Deg;

        float dur = 0.32f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            goM.transform.position = Vector2.Lerp(origem, destino, p);
            goM.transform.rotation = Quaternion.Euler(0f, 0f, ang - 90f);
            yield return null;
        }

        Destroy(goM);
        Destroy(marcador);
        AplicarDano(destino);
        StartCoroutine(AnimarExplosao(destino));
    }

    GameObject CriarMarcadorAlvo(Vector2 pos)
    {
        const int SEGS = 20;
        var root = new GameObject("Marcador");
        root.transform.position = pos;
        var lr = root.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.positionCount = SEGS;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = 10;
        lr.startWidth = lr.endWidth = 0.06f;
        lr.startColor = lr.endColor = new Color(1f, 0.3f, 0f, 0.7f);
        for (int i = 0; i < SEGS; i++)
        {
            float a = (360f / SEGS) * i * Mathf.Deg2Rad;
            lr.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * raioImpacto);
        }
        return root;
    }

    IEnumerator AnimarExplosao(Vector2 pos)
    {
        const int SEGS = 20;
        var go = new GameObject("Explosao");
        go.transform.position = pos;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 14;

        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            float p = t / 0.4f;
            float r = Mathf.Lerp(0f, raioImpacto, p);
            lr.startWidth  = lr.endWidth  = Mathf.Lerp(0.28f, 0.04f, p);
            lr.startColor  = lr.endColor  = new Color(1f, Mathf.Lerp(0.5f, 0.1f, p), 0f, 1f - p);
            for (int i = 0; i < SEGS; i++)
            {
                float a = (360f / SEGS) * i * Mathf.Deg2Rad;
                lr.SetPosition(i, pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
            yield return null;
        }
        Destroy(go);
    }

    static Sprite GerarSprite(int sz, Color cor)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
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
