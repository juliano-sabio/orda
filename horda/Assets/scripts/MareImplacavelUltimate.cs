using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Adicione este componente ao GameObject do Player.
/// Pressionar R cria uma maré ao redor do jogador por <duracao> segundos.
/// Inimigos dentro da área ficam lentos e sofrem dano de afogamento periodicamente.
/// </summary>
public class MareImplacavelUltimate : MonoBehaviour, IUltimateCosmetico
{
    bool cosmetico;
    public void ExecutarCosmetico() { if (ativo) return; cosmetico = true; StartCoroutine(CorotinaAtivacao()); }

    [Header("Configurações")]
    public float raio            = 5.5f;
    public float duracao         = 5f;
    public float cooldown        = 22f;
    [Tooltip("Redução de velocidade aplicada aos inimigos dentro da maré (0-1)")]
    public float fatorLentidao   = 0.5f;
    [Tooltip("Dano de afogamento aplicado a cada tick")]
    public float danoPorTick     = 6f;
    [Tooltip("Intervalo entre os ticks de dano de afogamento")]
    public float intervaloDano   = 1f;

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

        if (playerStats != null && playerStats.IsLocalAuthority &&
            InputBindings.UltimateDown() && cooldownRestante <= 0f && !ativo && (playerStats == null || !playerStats.ultimateBloqueada))
            StartCoroutine(CorotinaAtivacao());

        SincronizarUI();
    }

    void SincronizarUI()
    {
        if (playerStats == null) return;
        playerStats.ultimateChargeTime = cooldown - Mathf.Max(0f, cooldownRestante);
        playerStats.ultimateReady      = cooldownRestante <= 0f && !ativo;
    }

    IEnumerator CorotinaAtivacao()
    {
        ativo            = true;
        cooldownRestante = cooldown;

        // MarePersistente: +3s de duração e +1.5 de raio
        float duracaoEfetiva = duracao;
        float raioEfetivo    = raio;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.MarePersistente))
        {
            duracaoEfetiva += 3f;
            raioEfetivo    += 1.5f;
        }

        // MareEletrica: dano de afogamento +50%
        float danoEfetivo = danoPorTick;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.MareEletrica))
            danoEfetivo *= 1.5f;

        GameObject mareGO = CriarVisual(raioEfetivo);
        var lr = mareGO.GetComponentInChildren<LineRenderer>();
        var sr = mareGO.GetComponentInChildren<SpriteRenderer>();

        float elapsed       = 0f;
        float proximoTick   = intervaloDano;

        while (elapsed < duracaoEfetiva)
        {
            elapsed += Time.deltaTime;
            mareGO.transform.position = transform.position;

            AtualizarVisual(elapsed, lr, sr, raioEfetivo);

            // Lentidão contínua para quem está dentro da área
            foreach (var col in Physics2D.OverlapCircleAll(transform.position, raioEfetivo))
            {
                var ic = ResolverInimigo(col.gameObject);
                if (ic != null) ic.AplicarSlow(fatorLentidao, 0.3f);
            }

            // Dano de afogamento periódico
            if (elapsed >= proximoTick)
            {
                proximoTick += intervaloDano;
                foreach (var col in Physics2D.OverlapCircleAll(transform.position, raioEfetivo))
                {
                    var ic = ResolverInimigo(col.gameObject);
                    if (ic != null && !cosmetico) ic.ReceberDano(danoEfetivo, false, true);
                }
            }

            yield return null;
        }

        ativo = false;
        StartCoroutine(FadeOutMare(mareGO));
    }

    InimigoController ResolverInimigo(GameObject go)
    {
        return go.GetComponent<InimigoController>() ?? go.GetComponentInParent<InimigoController>();
    }

    // ─── VISUAL ─────────────────────────────────────────────────────

    GameObject CriarVisual(float raioVisual)
    {
        var root = new GameObject("MareImplacavel");
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
        lr.startColor    = lr.endColor = new Color(0.1f, 0.5f, 1f, 0.9f);
        for (int i = 0; i < SEGS; i++)
        {
            float ang = (360f / SEGS) * i * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(ang) * raioVisual, Mathf.Sin(ang) * raioVisual));
        }

        var discoGO          = new GameObject("Disco");
        discoGO.transform.SetParent(root.transform, false);
        var srDisco          = discoGO.AddComponent<SpriteRenderer>();
        srDisco.sprite       = GerarSpriteDisco(128);
        srDisco.color        = new Color(0.1f, 0.45f, 1f, 0.14f);
        srDisco.sortingOrder = 11;
        discoGO.transform.localScale = Vector3.one * (raioVisual * 2f);

        return root;
    }

    void AtualizarVisual(float elapsed, LineRenderer lr, SpriteRenderer sr, float raioVisual)
    {
        float pulso = Mathf.Sin(elapsed * 3f) * 0.5f + 0.5f;

        if (lr != null)
        {
            Color cor = Color.Lerp(new Color(0.1f, 0.45f, 1f), new Color(0.5f, 0.85f, 1f), pulso);
            cor.a     = elapsed > duracao - 1f
                ? Mathf.PingPong(elapsed * 8f, 1f) * 0.4f + 0.5f
                : 0.9f;
            lr.startColor = lr.endColor = cor;

            // ondulação suave no raio do anel
            const int SEGS = 48;
            for (int i = 0; i < SEGS; i++)
            {
                float ang  = (360f / SEGS) * i * Mathf.Deg2Rad;
                float onda = 1f + Mathf.Sin(elapsed * 4f + ang * 3f) * 0.03f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(ang) * raioVisual * onda, Mathf.Sin(ang) * raioVisual * onda));
            }
        }

        if (sr != null)
        {
            Color c  = sr.color;
            c.a      = 0.12f + Mathf.Sin(elapsed * 2.5f) * 0.03f;
            sr.color = c;
        }
    }

    IEnumerator FadeOutMare(GameObject root)
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
            tex.SetPixel(x, y, new Color(0.15f, 0.55f, 1f, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }
}
