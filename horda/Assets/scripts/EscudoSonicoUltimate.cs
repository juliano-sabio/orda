using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EscudoSonicoUltimate : MonoBehaviour, IUltimateCosmetico
{
    bool cosmetico;
    public void ExecutarCosmetico() { if (ativo) return; cosmetico = true; StartCoroutine(CorotinaAtivacao()); }

    [Header("Configurações")]
    public float cooldown          = 28f;
    public float duracao           = 5f;
    public float danoBasePorPulso  = 18f;
    public float raio              = 5f;
    public float intervaloPulso    = 0.8f;
    public float forcaEmpurrao     = 12f;

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
        ativo = true;
        cooldownRestante = cooldown;

        // Aura de escudo que fica no player durante toda a ult
        var aura = CriarAura();

        float elapsed    = 0f;
        float proxPulso  = 0f;
        int   numeroPulso = 0;

        while (elapsed < duracao)
        {
            elapsed    += Time.deltaTime;
            proxPulso  -= Time.deltaTime;

            // Segue o player
            if (aura != null) aura.transform.position = transform.position;

            if (proxPulso <= 0f)
            {
                numeroPulso++;
                proxPulso = intervaloPulso;

                // SonicoAmplificado: dano base dobrado
                float danoBase = danoBasePorPulso;
                if (SkillEvolutionManager.Tem(SkillEvolutionType.SonicoAmplificado))
                    danoBase *= 2f;

                float danoEscalado = danoBase * (1f + (numeroPulso - 1) * 0.25f);
                DispararPulso(numeroPulso, danoEscalado);
            }

            yield return null;
        }

        Destroy(aura);
        ativo = false;
    }

    // ─── LÓGICA ─────────────────────────────────────────────────────────────

    void DispararPulso(int numero, float dano)
    {
        Vector2 centro = transform.position;

        // Dano e knockback em inimigos
        foreach (var col in Physics2D.OverlapCircleAll(centro, raio))
        {
            if (col.gameObject == gameObject) continue;
            if (col.GetComponentInParent<ProjetilHomingPrincesa>(true)   != null) continue;
            if (col.GetComponentInParent<ProjetilEspecialPrincesa>(true) != null) continue;

            var ic = col.GetComponent<InimigoController>() ?? col.GetComponentInParent<InimigoController>();
            if (ic != null)
            {
                if (!cosmetico) ic.ReceberDano(dano);
                // Knockback
                var rb = col.GetComponent<Rigidbody2D>() ?? ic.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    Vector2 dir = ((Vector2)ic.transform.position - centro).normalized;
                    if (dir == Vector2.zero) dir = Random.insideUnitCircle.normalized;

                    // SonicoPercussao: +50% knockback
                    float forcaEfetiva = forcaEmpurrao;
                    if (SkillEvolutionManager.Tem(SkillEvolutionType.SonicoPercussao))
                        forcaEfetiva *= 1.5f;

                    rb.AddForce(dir * forcaEfetiva, ForceMode2D.Impulse);
                }

                // SonicoPercussao: atordoa por 0.5s (desativa movimento brevemente)
                if (SkillEvolutionManager.Tem(SkillEvolutionType.SonicoPercussao))
                    StartCoroutine(AtordoarInimigo(ic, 0.5f));
            }
        }

        // Empurra projéteis inimigos
        foreach (var col in Physics2D.OverlapCircleAll(centro, raio * 1.3f))
        {
            if (col.gameObject == gameObject) continue;
            if (col.GetComponent<InimigoController>() != null) continue;
            if (col.GetComponentInParent<InimigoController>() != null) continue;

            // É um projétil se tem Rigidbody2D mas não é o player nem um inimigo
            var rb = col.GetComponent<Rigidbody2D>();
            if (rb != null && rb.bodyType != RigidbodyType2D.Static && col.GetComponent<PlayerStats>() == null)
            {
                Vector2 dir = ((Vector2)col.transform.position - centro).normalized;
                if (dir == Vector2.zero) dir = Random.insideUnitCircle.normalized;
                rb.linearVelocity = dir * (rb.linearVelocity.magnitude + forcaEmpurrao * 0.5f);
            }
            else
            {
                // Projéteis-fantasma se movem via transform.position (sem Rigidbody2D),
                // então o empurrão é aplicado como deslocamento instantâneo.
                var slowProjetil = col.GetComponent<ProjetilFantasmaSlow>();
                if (slowProjetil != null)
                {
                    Vector2 dir = ((Vector2)col.transform.position - centro).normalized;
                    if (dir == Vector2.zero) dir = Random.insideUnitCircle.normalized;
                    col.transform.position += (Vector3)(dir * forcaEmpurrao * 0.25f);
                }
            }
        }

        // Visual da onda sônica
        StartCoroutine(AnimarOndaSonica(numero));
    }

    // ─── VISUAIS ────────────────────────────────────────────────────────────

    static readonly Color corSonico    = new Color(0.7f, 0.95f, 1f,  1f);
    static readonly Color corBrilho    = new Color(1f,   1f,    1f,  1f);
    static readonly Color corAura      = new Color(0.5f, 0.85f, 1f,  0.3f);

    // Aura constante ao redor do player
    GameObject CriarAura()
    {
        var root = new GameObject("AuraEscudoSonico");
        root.transform.position = transform.position;

        // 3 anéis de aura pulsantes
        CriarAnelAura(root, raio * 0.4f, new Color(0.6f, 0.9f, 1f, 0.25f), 0.08f, "AuraInterna");
        CriarAnelAura(root, raio * 0.7f, new Color(0.5f, 0.85f, 1f, 0.18f), 0.06f, "AuraMedia");
        CriarAnelAura(root, raio,         new Color(0.4f, 0.8f,  1f, 0.12f), 0.04f, "AuraExterna");

        StartCoroutine(AnimarAura(root));

        return root;
    }

    void CriarAnelAura(GameObject parent, float r, Color cor, float largura, string nome)
    {
        const int SEGS = 48;
        var go = new GameObject(nome);
        go.transform.SetParent(parent.transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 12;
        lr.startWidth    = lr.endWidth = largura;
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
            float pulso = Mathf.Sin(t * 4f) * 0.5f + 0.5f;

            var lrs = root.GetComponentsInChildren<LineRenderer>();
            foreach (var lr in lrs)
            {
                Color c = lr.startColor;
                c.a = c.a * 0.7f + pulso * 0.3f;
                lr.startColor = lr.endColor = c;
            }
            yield return null;
        }
    }

    IEnumerator AnimarOndaSonica(int numero)
    {
        Vector3 centro = transform.position;
        const int ANEIS = 3; // múltiplas ondas concêntricas por pulso

        var gos = new List<(GameObject go, LineRenderer lr, float delay)>();
        for (int a = 0; a < ANEIS; a++)
        {
            var go = new GameObject($"OndaSonica_{numero}_{a}");
            go.transform.position = centro;
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop          = true;
            lr.positionCount = 48;
            lr.material      = new Material(Shader.Find("Sprites/Default"));
            lr.sortingOrder  = 16;
            gos.Add((go, lr, a * 0.08f));
        }

        // Cor escala com o número do pulso (fica mais intensa)
        float intensidade = Mathf.Clamp01(0.5f + numero * 0.1f);
        Color corBase = Color.Lerp(corSonico, corBrilho, (numero - 1) / 6f);

        float dur = 0.55f;
        float raioMax = raio * (1f + numero * 0.08f); // raio cresce a cada pulso

        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            // Atualiza centro para seguir o player
            centro = transform.position;

            foreach (var (go, lr, delay) in gos)
            {
                if (go == null) continue;
                float tLocal = Mathf.Max(0f, t - delay);
                float p = tLocal / dur;
                float r = Mathf.Lerp(0.05f, raioMax, Mathf.Pow(p, 0.5f));

                lr.startWidth = lr.endWidth = Mathf.Lerp(0.22f, 0.02f, p) * intensidade;
                Color cor = corBase;
                cor.a = (1f - p) * intensidade;
                lr.startColor = lr.endColor = cor;

                const int SEGS = 48;
                for (int i = 0; i < SEGS; i++)
                {
                    float ang = (360f / SEGS) * i * Mathf.Deg2Rad;
                    // Leve distorção sônica
                    float dist = r + Mathf.Sin(ang * 6f + t * 20f) * r * 0.03f;
                    lr.SetPosition(i, centro + new Vector3(Mathf.Cos(ang) * dist, Mathf.Sin(ang) * dist));
                }
            }

            // Flash no player no primeiro frame de cada pulso
            if (t < Time.deltaTime * 2f)
                StartCoroutine(FlashPlayer());

            yield return null;
        }

        foreach (var (go, _, _) in gos)
            if (go != null) Destroy(go);

        // Raios radiais curtos no último pulso (mais impacto)
        if (numero >= 4)
            StartCoroutine(AnimarRaiosRadiais(centro, numero));
    }

    IEnumerator AnimarRaiosRadiais(Vector3 centro, int numero)
    {
        int qtdRaios = 8 + (numero - 4) * 2;
        var raios = new List<(GameObject go, LineRenderer lr, Vector2 dir)>();

        for (int i = 0; i < qtdRaios; i++)
        {
            float ang = (360f / qtdRaios) * i * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));

            var go = new GameObject($"RaioRadial_{i}");
            go.transform.position = centro;
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.material      = new Material(Shader.Find("Sprites/Default"));
            lr.sortingOrder  = 15;
            lr.startWidth    = 0.06f;
            lr.endWidth      = 0.01f;
            raios.Add((go, lr, dir));
        }

        float dur = 0.3f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            foreach (var (go, lr, dir) in raios)
            {
                if (go == null) continue;
                float comprimento = Mathf.Lerp(0f, raio * 0.6f, p);
                lr.SetPosition(0, (Vector3)((Vector2)centro + dir * raio * 0.9f));
                lr.SetPosition(1, (Vector3)((Vector2)centro + dir * (raio * 0.9f + comprimento)));
                Color c = corBrilho; c.a = 1f - p;
                lr.startColor = lr.endColor = c;
            }
            yield return null;
        }

        foreach (var (go, _, _) in raios)
            if (go != null) Destroy(go);
    }

    IEnumerator AtordoarInimigo(InimigoController ic, float duracao)
    {
        if (ic == null) yield break;
        var movi = ic.GetComponent<movi_inimigo>();
        var moviD = ic.GetComponent<movi_inimigo_manter_distancia>();
        if (movi   != null) movi.enabled   = false;
        if (moviD  != null) moviD.enabled  = false;
        yield return new WaitForSeconds(duracao);
        if (movi   != null) movi.enabled   = true;
        if (moviD  != null) moviD.enabled  = true;
    }

    IEnumerator FlashPlayer()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        Color original = sr.color;
        sr.color = Color.Lerp(original, corSonico, 0.6f);
        yield return new WaitForSeconds(0.06f);
        sr.color = original;
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
