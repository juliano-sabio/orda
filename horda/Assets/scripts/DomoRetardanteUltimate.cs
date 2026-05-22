using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Adicione este componente ao GameObject do Player.
/// Pressionar R ativa um domo que desacelera todos os projéteis
/// inimigos dentro do raio por <duracao> segundos.
/// Projéteis voltam à velocidade original ao sair.
/// </summary>
public class DomoRetardanteUltimate : MonoBehaviour
{
    [Header("Configurações")]
    public float raio            = 5f;
    public float duracao         = 5f;
    public float cooldown        = 20f;
    [Tooltip("Velocidade máxima dos projéteis dentro do domo")]
    public float velocidadeLenta = 1f;

    public float CooldownRestante => cooldownRestante;
    public bool  Ativo            => ativo;

    private float       cooldownRestante;
    private bool        ativo;
    private PlayerStats playerStats;

    // velocidade linear original de cada projétil ao entrar no domo
    private readonly Dictionary<Rigidbody2D, Vector2> velocidadeOriginal =
        new Dictionary<Rigidbody2D, Vector2>();

    // velocidadeMaxima original do ProjetilHomingPrincesa (só para projéteis homing)
    private readonly Dictionary<Rigidbody2D, float> homingMaxOriginal =
        new Dictionary<Rigidbody2D, float>();

    // ─── LIFECYCLE ──────────────────────────────────────────────────

    void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            // Garante que a UI reconheça a ultimate como ativa
            if (playerStats.ultimateSkill != null)
                playerStats.ultimateSkill.isActive = true;
            playerStats.ultimateCooldown   = cooldown;
            playerStats.ultimateChargeTime = 0f;
            playerStats.ultimateReady      = false;
        }
    }

    // ─── INPUT ──────────────────────────────────────────────────────

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
        // chargeTime vai de 0 (recarregando) até cooldown (pronto)
        playerStats.ultimateChargeTime = cooldown - Mathf.Max(0f, cooldownRestante);
        playerStats.ultimateReady      = cooldownRestante <= 0f && !ativo;
    }

    // ─── FÍSICA / DETECÇÃO ──────────────────────────────────────────

    void FixedUpdate()
    {
        if (!ativo) return;

        Vector2 centro = transform.position;

        // Registra projéteis que entraram no raio
        foreach (var col in Physics2D.OverlapCircleAll(centro, raio))
        {
            if (col.CompareTag("Player") || col.CompareTag("Enemy")) continue;
            if (col.GetComponent<InimigoController>() != null) continue;

            var rb = col.GetComponent<Rigidbody2D>();
            if (rb == null || velocidadeOriginal.ContainsKey(rb)) continue;

            // Guarda velocidade original
            velocidadeOriginal[rb] = rb.linearVelocity;

            // Se for homing, também reduz velocidadeMaxima
            var homing = rb.GetComponent<ProjetilHomingPrincesa>();
            if (homing != null && homing.enabled)
            {
                homingMaxOriginal[rb]   = homing.velocidadeMaxima;
                homing.velocidadeMaxima = velocidadeLenta;
            }
        }

        // Processa cada projétil registrado
        var sair = new List<Rigidbody2D>();
        foreach (var kv in velocidadeOriginal)
        {
            var rb = kv.Key;

            // Destruído
            if (rb == null) { sair.Add(rb); continue; }

            // Saiu do raio → restaura e marca para remoção
            if (Vector2.Distance(rb.position, centro) > raio + 0.3f)
            {
                RestaurarProjetil(rb);
                sair.Add(rb);
                continue;
            }

            // Dentro do domo → clamp de velocidade
            if (rb.linearVelocity.sqrMagnitude > velocidadeLenta * velocidadeLenta)
                rb.linearVelocity = rb.linearVelocity.normalized * velocidadeLenta;
        }

        foreach (var rb in sair)
        {
            velocidadeOriginal.Remove(rb);
            homingMaxOriginal.Remove(rb);
        }
    }

    void RestaurarProjetil(Rigidbody2D rb)
    {
        if (rb == null) return;

        // Sempre restaura a velocidade linear imediatamente
        if (velocidadeOriginal.TryGetValue(rb, out Vector2 vel))
            rb.linearVelocity = vel;

        // Para homing, também restaura velocidadeMaxima para que o script continue acelerando
        if (homingMaxOriginal.TryGetValue(rb, out float velMax))
        {
            var homing = rb.GetComponent<ProjetilHomingPrincesa>();
            if (homing != null) homing.velocidadeMaxima = velMax;
        }
    }

    // ─── COROUTINE PRINCIPAL ────────────────────────────────────────

    IEnumerator CorotinaAtivacao()
    {
        ativo            = true;
        cooldownRestante = cooldown;

        GameObject domoGO = CriarVisual();
        var lr = domoGO.GetComponentInChildren<LineRenderer>();
        var sr = domoGO.GetComponentInChildren<SpriteRenderer>();

        yield return StartCoroutine(AnimarEntrada(domoGO, lr));

        float elapsed = 0f;
        while (elapsed < duracao)
        {
            elapsed += Time.deltaTime;
            domoGO.transform.position = transform.position;

            if (lr != null)
            {
                float pulso = Mathf.Sin(elapsed * 3f) * 0.5f + 0.5f;
                Color cor   = Color.Lerp(new Color(0.2f, 0.7f, 1f), new Color(0.55f, 1f, 1f), pulso);
                float alpha = elapsed > duracao - 1f
                    ? Mathf.PingPong(elapsed * 10f, 1f) * 0.5f + 0.5f
                    : 0.9f;
                cor.a         = alpha;
                lr.startColor = lr.endColor = cor;
                float w       = 0.1f + pulso * 0.05f;
                lr.startWidth = lr.endWidth = w;
            }

            if (sr != null)
            {
                Color c = sr.color;
                c.a     = 0.1f + Mathf.Sin(elapsed * 2f) * 0.02f;
                sr.color = c;
            }

            yield return null;
        }

        // Restaura todos os projéteis ainda dentro do domo
        foreach (var rb in velocidadeOriginal.Keys)
            RestaurarProjetil(rb);
        velocidadeOriginal.Clear();
        homingMaxOriginal.Clear();

        ativo = false;
        StartCoroutine(FadeOutDomo(domoGO));
    }

    // ─── VISUAL ─────────────────────────────────────────────────────

    GameObject CriarVisual()
    {
        var root = new GameObject("DomoRetardante");
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
        lr.startColor    = lr.endColor = new Color(0.2f, 0.7f, 1f, 0.9f);
        for (int i = 0; i < SEGS; i++)
        {
            float ang = (360f / SEGS) * i * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(ang) * raio, Mathf.Sin(ang) * raio));
        }

        var discoGO         = new GameObject("Disco");
        discoGO.transform.SetParent(root.transform, false);
        var srDisco         = discoGO.AddComponent<SpriteRenderer>();
        srDisco.sprite      = GerarSpriteDisco(128);
        srDisco.color       = new Color(0.15f, 0.55f, 1f, 0.12f);
        srDisco.sortingOrder = 11;
        discoGO.transform.localScale = Vector3.one * (raio * 2f);

        return root;
    }

    IEnumerator AnimarEntrada(GameObject root, LineRenderer lr)
    {
        const int SEGS = 48;
        float t = 0f, dur = 0.22f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p   = Mathf.Clamp01(t / dur);
            float ovs = p < 0.65f
                ? Mathf.Lerp(0f, 1.12f, p / 0.65f)
                : Mathf.Lerp(1.12f, 1f, (p - 0.65f) / 0.35f);
            float r = raio * ovs;

            if (lr != null)
                for (int i = 0; i < SEGS; i++)
                {
                    float ang = (360f / SEGS) * i * Mathf.Deg2Rad;
                    lr.SetPosition(i, new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r));
                }

            root.transform.position = transform.position;
            yield return null;
        }

        if (lr != null)
            for (int i = 0; i < SEGS; i++)
            {
                float ang = (360f / SEGS) * i * Mathf.Deg2Rad;
                lr.SetPosition(i, new Vector3(Mathf.Cos(ang) * raio, Mathf.Sin(ang) * raio));
            }
    }

    IEnumerator FadeOutDomo(GameObject root)
    {
        var lr = root.GetComponentInChildren<LineRenderer>();
        var sr = root.GetComponentInChildren<SpriteRenderer>();

        Color corLR = lr != null ? lr.startColor : Color.clear;
        Color corSR = sr != null ? sr.color       : Color.clear;

        float t = 0f, dur = 0.4f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;

            if (lr != null) { Color c = corLR; c.a = Mathf.Lerp(corLR.a, 0f, p); lr.startColor = lr.endColor = c; }
            if (sr != null) { Color c = corSR; c.a = Mathf.Lerp(corSR.a, 0f, p); sr.color = c; }

            yield return null;
        }

        if (root != null) Destroy(root);
    }

    static Sprite GerarSpriteDisco(int sz)
    {
        float cx = sz * 0.5f;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d     = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cx));
            float t     = Mathf.Clamp01(d / cx);
            float borda = Mathf.Clamp01(1f - Mathf.Abs(t - 0.88f) / 0.12f);
            float meio  = Mathf.Pow(1f - t, 2f) * 0.3f;
            float a     = Mathf.Clamp01(borda + meio) * (t < 1f ? 1f : 0f);
            tex.SetPixel(x, y, new Color(0.35f, 0.75f, 1f, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }
}
