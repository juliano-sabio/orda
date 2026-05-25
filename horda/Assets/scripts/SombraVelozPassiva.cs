using System.Collections;
using UnityEngine;

public class SombraVelozPassiva : MonoBehaviour
{
    public int   maxPilhas        = 3;
    public float bonusPorPilha    = 0.15f;
    public float duracaoPilha     = 3f;
    public float duracaoTurbo     = 5f;
    public float duracaoCooldown  = 25f;
    public float intervaloRastro  = 0.06f;

    private enum Estado { Normal, Cooldown }
    private Estado estado = Estado.Normal;

    private PlayerStats stats;
    private int   pilhasAtivas = 0;
    private float bonusPorPilhaFlat;
    private float proximoRastro  = 0f;
    private float timerTurbo     = 0f;
    private float timerCooldown  = 0f;
    private bool  emTurbo        = false;

    void Start()
    {
        stats = GetComponent<PlayerStats>();
        bonusPorPilhaFlat = stats.speed * bonusPorPilha;
        InimigoController.OnPreMorte += OnInimigoMorto;
    }

    void OnDestroy()
    {
        InimigoController.OnPreMorte -= OnInimigoMorto;
    }

    void OnInimigoMorto(InimigoController morto)
    {
        if (estado != Estado.Normal) return;
        if (pilhasAtivas >= maxPilhas) return;

        pilhasAtivas++;
        stats.speed += bonusPorPilhaFlat;

        if (pilhasAtivas == maxPilhas && !emTurbo)
        {
            emTurbo = true;
            StartCoroutine(EfeitoBurst());
        }

        StartCoroutine(ExpirarPilha());
    }

    IEnumerator ExpirarPilha()
    {
        yield return new WaitForSeconds(duracaoPilha);
        if (estado == Estado.Normal && pilhasAtivas > 0)
        {
            pilhasAtivas--;
            stats.speed -= bonusPorPilhaFlat;
        }
    }

    void Update()
    {
        if (estado == Estado.Cooldown)
        {
            timerCooldown -= Time.deltaTime;
            if (timerCooldown <= 0f)
            {
                estado     = Estado.Normal;
                timerTurbo = 0f;
                emTurbo    = false;
            }
            return;
        }

        // Normal
        if (pilhasAtivas >= maxPilhas)
        {
            timerTurbo += Time.deltaTime;
            if (timerTurbo >= duracaoTurbo)
            {
                EntrarCooldown();
                return;
            }

            if (Time.time >= proximoRastro)
            {
                proximoRastro = Time.time + intervaloRastro;
                CriarRastro();
            }
        }
        else
        {
            timerTurbo = 0f;
        }
    }

    void EntrarCooldown()
    {
        stats.speed -= bonusPorPilhaFlat * pilhasAtivas;
        pilhasAtivas  = 0;
        timerTurbo    = 0f;
        timerCooldown = duracaoCooldown;
        emTurbo       = false;
        estado        = Estado.Cooldown;

        StartCoroutine(EfeitoDissipacao());
    }

    // ── Rastro (3 pilhas ativas) ──────────────────────────────────────
    void CriarRastro()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;

        var go = new GameObject("Rastro");
        go.transform.position   = transform.position;
        go.transform.localScale = transform.localScale * 1.12f;

        var rsr              = go.AddComponent<SpriteRenderer>();
        rsr.sprite           = sr.sprite;
        rsr.color            = new Color(0.5f, 0.1f, 1f, 0.82f);
        rsr.sortingLayerName = sr.sortingLayerName;
        rsr.sortingOrder     = sr.sortingOrder - 1;
        rsr.flipX            = sr.flipX;

        StartCoroutine(FadeRastro(go, rsr));
    }

    IEnumerator FadeRastro(GameObject go, SpriteRenderer sr)
    {
        const float DUR = 0.55f;
        for (float t = 0f; t < DUR; t += Time.deltaTime)
        {
            if (sr == null) yield break;
            sr.color = new Color(0.5f, 0.1f, 1f, Mathf.Lerp(0.82f, 0f, t / DUR));
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    // ── Burst ao atingir 3 pilhas ─────────────────────────────────────
    IEnumerator EfeitoBurst()
    {
        const int   SEGS    = 36;
        const float DUR     = 0.45f;
        const float RAIO_MX = 2.2f;

        var go = new GameObject("BurstTurbo");
        go.transform.position = transform.position;
        var lr        = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 15;

        Vector2 centro = transform.position;
        for (float t = 0f; t < DUR; t += Time.deltaTime)
        {
            float p    = t / DUR;
            float raio = Mathf.Lerp(0f, RAIO_MX, p);
            float a    = Mathf.Lerp(1f, 0f, p);
            lr.startColor = lr.endColor = new Color(0.65f, 0.2f, 1f, a);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.35f, 0.03f, p);
            for (int i = 0; i < SEGS; i++)
            {
                float ang = i / (float)SEGS * Mathf.PI * 2f;
                lr.SetPosition(i, centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raio);
            }
            yield return null;
        }
        Destroy(go);
    }

    // ── Dissipação ao entrar em cooldown ─────────────────────────────
    IEnumerator EfeitoDissipacao()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) yield break;

        // Flash branco
        Color original = sr.color;
        sr.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        sr.color = original;

        // 8 fragmentos voando para fora
        const int COUNT = 4;
        for (int i = 0; i < COUNT; i++)
        {
            float ang = i / (float)COUNT * Mathf.PI * 2f;
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            StartCoroutine(Fragmento(sr, dir));
        }
    }

    IEnumerator Fragmento(SpriteRenderer originalSR, Vector2 dir)
    {
        var go = new GameObject("Fragmento");
        go.transform.position   = transform.position;
        go.transform.localScale = transform.localScale * 0.9f;

        var rsr              = go.AddComponent<SpriteRenderer>();
        rsr.sprite           = originalSR.sprite;
        rsr.color            = new Color(0.6f, 0.2f, 1f, 0.85f);
        rsr.sortingLayerName = originalSR.sortingLayerName;
        rsr.sortingOrder     = originalSR.sortingOrder - 1;
        rsr.flipX            = originalSR.flipX;

        Vector2 origem  = transform.position;
        const float DUR = 0.5f;
        const float VEL = 2.5f;

        for (float t = 0f; t < DUR; t += Time.deltaTime)
        {
            if (go == null) yield break;
            go.transform.position = origem + dir * VEL * t;
            rsr.color = new Color(0.6f, 0.2f, 1f, Mathf.Lerp(0.85f, 0f, t / DUR));
            yield return null;
        }
        if (go != null) Destroy(go);
    }
}
