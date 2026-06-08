using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FormaBestialUltimate : MonoBehaviour
{
    [Header("Configurações")]
    public float cooldown                = 32f;
    public float duracao                 = 6f;
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

    private float       cooldownRestante;
    private bool        ativo;
    private PlayerStats playerStats;
    private SpriteRenderer sr;
    private Color       corOriginal;
    private float       velocidadeOriginal;
    private GameObject  auraRoot;
    private Coroutine   corAura;

    static readonly Color COR_BESTA  = new Color(1.00f, 0.45f, 0.05f);
    static readonly Color COR_CHAMA  = new Color(1.00f, 0.72f, 0.10f);
    static readonly Color COR_BRASA  = new Color(1.00f, 0.18f, 0.02f);

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
        if (playerStats != null) { playerStats.speed = velocidadeOriginal; playerStats.CancelarTimerSlow(); }
        if (sr != null) sr.color = corOriginal;
        var sm = SkillManager.Instance;
        if (sm != null) sm.enabled = true;
        if (corAura != null) StopCoroutine(corAura);
        if (auraRoot != null) Destroy(auraRoot);
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

        if (sr != null) { corOriginal = sr.color; sr.color = COR_BESTA; }

        StartCoroutine(FlashAtivacao());

        auraRoot = new GameObject("AuraFormaBestial");
        corAura  = StartCoroutine(AuraLoop());

        Rugido();

        float intervaloMeleeEfetivo = intervaloMelee;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.BestialFrenesi))
            intervaloMeleeEfetivo = 0.2f;

        float elapsed    = 0f;
        float proxMelee  = 0f;
        float proxRugido = intervaloRugido;

        while (elapsed < duracao)
        {
            elapsed    += Time.deltaTime;
            proxMelee  -= Time.deltaTime;
            proxRugido -= Time.deltaTime;

            if (auraRoot != null) auraRoot.transform.position = transform.position;

            if (proxMelee <= 0f)  { proxMelee = intervaloMeleeEfetivo; AtaqueMelee(); }
            if (proxRugido <= 0f) { proxRugido = intervaloRugido;        Rugido();      }

            yield return null;
        }

        BurstFinal();

        if (sr != null) sr.color = corOriginal;
        playerStats.speed = velocidadeOriginal;
        playerStats.CancelarTimerSlow();
        if (sm != null) sm.enabled = true;
        if (corAura != null) { StopCoroutine(corAura); corAura = null; }
        if (auraRoot != null) { Destroy(auraRoot); auraRoot = null; }
        ativo = false;
    }

    // ── Lógica ───────────────────────────────────────────────────────────────

    void AtaqueMelee()
    {
        Vector2 centro = transform.position;
        bool acertou = false;
        foreach (var col in Physics2D.OverlapCircleAll(centro, raioMelee))
        {
            if (col.gameObject == gameObject) continue;
            var ic = col.GetComponent<InimigoController>()
                  ?? col.GetComponentInParent<InimigoController>();
            if (ic != null && !ic.estaMorrendo)
            {
                ic.ReceberDano(danoMelee);
                StartCoroutine(ParticulasImpacto(ic.transform.position));
                acertou = true;
            }
        }
        if (!acertou)
            StartCoroutine(ParticulasImpacto(centro + Random.insideUnitCircle * raioMelee * 0.5f));
    }

    void Rugido()
    {
        float danoEfetivo = danoRugido;
        if (SkillEvolutionManager.Tem(SkillEvolutionType.BestialRugidoMortal))
            danoEfetivo *= 1.8f;

        Vector2 centro = transform.position;
        foreach (var col in Physics2D.OverlapCircleAll(centro, raioRugido))
        {
            if (col.gameObject == gameObject) continue;
            var ic = col.GetComponent<InimigoController>()
                  ?? col.GetComponentInParent<InimigoController>();
            if (ic == null) continue;
            ic.ReceberDano(danoEfetivo);
            var rb = col.GetComponent<Rigidbody2D>() ?? ic.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 dir = ((Vector2)ic.transform.position - centro).normalized;
                if (dir == Vector2.zero) dir = Random.insideUnitCircle.normalized;
                rb.AddForce(dir * forcaRugido, ForceMode2D.Impulse);
            }
        }
        StartCoroutine(ParticulasRugido(centro, raioRugido));
    }

    void BurstFinal()
    {
        float raioFinal = raioRugido * 1.6f;
        Vector2 centro  = transform.position;
        float mult = SkillEvolutionManager.Tem(SkillEvolutionType.BestialRugidoMortal) ? 1.8f : 1f;

        foreach (var col in Physics2D.OverlapCircleAll(centro, raioFinal))
        {
            if (col.gameObject == gameObject) continue;
            var ic = col.GetComponent<InimigoController>()
                  ?? col.GetComponentInParent<InimigoController>();
            if (ic == null) continue;
            ic.ReceberDano(danoMelee * 3f * mult);
            var rb = col.GetComponent<Rigidbody2D>() ?? ic.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 dir = ((Vector2)ic.transform.position - centro).normalized;
                if (dir == Vector2.zero) dir = Random.insideUnitCircle.normalized;
                rb.AddForce(dir * forcaRugido * 2.2f, ForceMode2D.Impulse);
            }
        }
        StartCoroutine(ParticulasBurst(centro, raioFinal));
    }

    // ── Visuais ──────────────────────────────────────────────────────────────

    IEnumerator FlashAtivacao()
    {
        // Rajada de faíscas de ativação
        for (int i = 0; i < 40; i++)
        {
            Vector2 pos = (Vector2)transform.position + Random.insideUnitCircle * 0.5f;
            float sz    = Random.Range(0.18f, 0.55f);
            Color c     = Color.Lerp(COR_BESTA, COR_CHAMA, Random.value);
            c.a = 0.9f;
            Vector2 vel = Random.insideUnitCircle.normalized * Random.Range(3f, 7f);
            SpawnParticula(pos, sz, c, vel, Random.Range(0.2f, 0.45f));
        }
        yield return null;
    }

    IEnumerator AuraLoop()
    {
        float timerBrasa  = 0f;
        float timerFaisca = 0f;
        float t           = 0f;

        while (auraRoot != null)
        {
            float dt = Time.deltaTime;
            t           += dt;
            timerBrasa  += dt;
            timerFaisca += dt;

            Vector2 centro = transform.position;

            if (timerBrasa >= 0.055f)
            {
                timerBrasa = 0f;
                SpawnBrasa(centro);
                SpawnBrasa(centro);
            }

            if (timerFaisca >= 0.10f)
            {
                timerFaisca = 0f;
                SpawnFaisca(centro);
            }

            // Pulso no sprite
            if (sr != null)
            {
                float p = Mathf.Sin(t * 9f) * 0.12f + 0.88f;
                sr.color = new Color(
                    Mathf.Clamp01(COR_BESTA.r),
                    Mathf.Clamp01(COR_BESTA.g * p),
                    Mathf.Clamp01(COR_BESTA.b));
            }

            yield return null;
        }
    }

    void SpawnBrasa(Vector2 centro)
    {
        Vector2 offset = Random.insideUnitCircle * 0.7f;
        Color c = Color.Lerp(COR_BRASA, COR_CHAMA, Random.value);
        c.a = Random.Range(0.6f, 0.95f);
        Vector2 vel = (offset.normalized * 0.6f + Vector2.up * 0.4f + Random.insideUnitCircle * 0.4f).normalized
                      * Random.Range(1.0f, 2.5f);
        SpawnParticula(centro + offset, Random.Range(0.07f, 0.20f), c, vel, Random.Range(0.28f, 0.55f), ordem: 11);
    }

    void SpawnFaisca(Vector2 centro)
    {
        float ang  = Random.Range(0f, Mathf.PI * 2f);
        float dist = Random.Range(0.2f, 1.1f);
        Vector2 pos = centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;
        Color c = Color.Lerp(COR_CHAMA, Color.white, Random.value * 0.5f);
        c.a = Random.Range(0.75f, 1f);
        Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(0.6f, 1.8f);
        SpawnParticula(pos, Random.Range(0.04f, 0.11f), c, vel, Random.Range(0.12f, 0.25f), ordem: 12);
    }

    IEnumerator ParticulasImpacto(Vector2 pos)
    {
        for (int i = 0; i < 10; i++)
        {
            Color c = Color.Lerp(COR_BESTA, COR_CHAMA, Random.value);
            c.a = 0.9f;
            Vector2 vel = Random.insideUnitCircle.normalized * Random.Range(2f, 5f);
            SpawnParticula(pos + Random.insideUnitCircle * 0.25f,
                Random.Range(0.10f, 0.30f), c, vel, Random.Range(0.18f, 0.35f), ordem: 14);
        }
        yield return null;
    }

    IEnumerator ParticulasRugido(Vector2 centro, float raio)
    {
        int qtd = 56;
        for (int i = 0; i < qtd; i++)
        {
            float ang = i / (float)qtd * Mathf.PI * 2f + Random.Range(-0.12f, 0.12f);
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            Vector2 pos = centro + dir * Random.Range(0.2f, 0.5f);
            Color c = Color.Lerp(COR_BESTA, COR_CHAMA, Random.value);
            c.a = Random.Range(0.75f, 1f);
            float speed = Random.Range(5f, raio * 2.2f);
            SpawnParticula(pos, Random.Range(0.12f, 0.38f), c, dir * speed, Random.Range(0.30f, 0.55f), ordem: 14);
        }
        // Partículas de poeira no centro
        for (int i = 0; i < 14; i++)
        {
            Vector2 pos = centro + Random.insideUnitCircle * 0.4f;
            Color c = new Color(1f, 0.6f, 0.2f, Random.Range(0.5f, 0.8f));
            Vector2 vel = Random.insideUnitCircle.normalized * Random.Range(0.5f, 1.5f);
            SpawnParticula(pos, Random.Range(0.20f, 0.45f), c, vel, Random.Range(0.4f, 0.7f), ordem: 13);
        }
        yield return null;
    }

    IEnumerator ParticulasBurst(Vector2 centro, float raioFinal)
    {
        // Flash central
        for (int j = 0; j < 3; j++)
        {
            var flash = new GameObject("FlashBurst");
            flash.transform.position = centro;
            var fspr = flash.AddComponent<SpriteRenderer>();
            fspr.sprite = GerarDisco();
            fspr.sortingOrder = 16;
            float fs = (1.5f - j * 0.4f) * 2f * (1f / 100f);
            flash.transform.localScale = Vector3.one * fs;
            Color fc = j == 0 ? Color.white : COR_CHAMA;
            fc.a = 0.85f - j * 0.2f;
            fspr.color = fc;
            StartCoroutine(FadeOut(flash, 0.18f + j * 0.05f));
        }

        // Onda principal de partículas
        int qtd = 110;
        for (int i = 0; i < qtd; i++)
        {
            float ang = i / (float)qtd * Mathf.PI * 2f + Random.Range(-0.15f, 0.15f);
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            Vector2 pos = centro + dir * Random.Range(0.1f, 0.7f);
            Color c = Color.Lerp(COR_BESTA, Color.white, Random.value * 0.35f);
            c.a = Random.Range(0.8f, 1f);
            float speed = Random.Range(6f, raioFinal * 2.8f);
            SpawnParticula(pos, Random.Range(0.15f, 0.52f), c, dir * speed, Random.Range(0.35f, 0.75f), ordem: 15);
        }
        yield return null;
    }

    // ── Utilitários ───────────────────────────────────────────────────────────

    void SpawnParticula(Vector2 pos, float tamanho, Color cor, Vector2 vel, float dur, int ordem = 12)
    {
        var go = new GameObject("PBestial");
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * tamanho;
        var spr = go.AddComponent<SpriteRenderer>();
        spr.sprite = GerarDisco();
        spr.sortingOrder = ordem;
        spr.color = cor;
        StartCoroutine(MoverEFadeOut(go, spr, vel, dur));
    }

    IEnumerator MoverEFadeOut(GameObject go, SpriteRenderer spr, Vector2 vel, float dur)
    {
        if (go == null) yield break;
        float t = 0f;
        float a0 = spr.color.a;
        Color c  = spr.color;
        while (t < dur && go != null)
        {
            t += Time.deltaTime;
            float pct = t / dur;
            go.transform.position += (Vector3)(vel * Time.deltaTime);
            vel *= Mathf.Max(0f, 1f - Time.deltaTime * 4.5f);
            c.a = Mathf.Lerp(a0, 0f, pct * pct);
            if (spr != null) spr.color = c;
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    IEnumerator FadeOut(GameObject go, float dur)
    {
        if (go == null) yield break;
        var spr = go.GetComponent<SpriteRenderer>();
        float t = 0f;
        Color c = spr != null ? spr.color : Color.white;
        float a0 = c.a;
        while (t < dur && go != null)
        {
            t += Time.deltaTime;
            if (spr != null) { c.a = Mathf.Lerp(a0, 0f, t / dur); spr.color = c; }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    static Sprite _discoCache;
    static Sprite GerarDisco()
    {
        if (_discoCache != null) return _discoCache;
        const int SZ = 32;
        var tex = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float c = SZ / 2f;
        for (int y = 0; y < SZ; y++)
            for (int x = 0; x < SZ; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c));
                float a = Mathf.Clamp01(1f - d / (c - 0.5f));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        _discoCache = Sprite.Create(tex, new Rect(0, 0, SZ, SZ), Vector2.one * 0.5f, 100f);
        return _discoCache;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, raioMelee);
        Gizmos.color = new Color(1f, 0.25f, 0f, 0.20f);
        Gizmos.DrawWireSphere(transform.position, raioRugido);
    }
}
