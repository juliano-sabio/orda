using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VorticeDevoradorEvento : MonoBehaviour
{
    public float raioAtracao    = 6f;
    public float raioDevorar    = 0.6f;
    public float forcaAtracao   = 5f;
    public float danoPorSegundo = 12f;
    public Color corVortice     = new Color(0.5f, 0.1f, 0.7f);

    public event Action<int, int> OnProgresso;
    public event Action           OnConcluido;

    int  progresso;
    int  quantidadeNecessaria;
    bool concluido;

    const float TICK_INTERVALO = 0.5f;
    readonly HashSet<InimigoController>            emDevoracao = new HashSet<InimigoController>();
    readonly Dictionary<InimigoController, float>  tickTimers  = new Dictionary<InimigoController, float>();

    SpriteRenderer srCore, srSwirlA, srSwirlB;

    void OnEnable()  => InimigoController.OnPreMorte += OnInimigoMorreu;
    void OnDisable() => InimigoController.OnPreMorte -= OnInimigoMorreu;

    public void Iniciar(int quantidade)
    {
        quantidadeNecessaria = quantidade;
        ConstruirVisual();
        StartCoroutine(ParticulasLoop());
    }

    // ── Visual ───────────────────────────────────────────────────────────────

    void ConstruirVisual()
    {
        var goCore = new GameObject("VorticeCore");
        goCore.transform.SetParent(transform);
        goCore.transform.localPosition = Vector3.zero;
        srCore               = goCore.AddComponent<SpriteRenderer>();
        srCore.sprite        = GerarDisco();
        srCore.color         = new Color(corVortice.r * 0.3f, corVortice.g * 0.3f, corVortice.b * 0.3f, 0.85f);
        srCore.sortingOrder  = 4;
        goCore.transform.localScale = Vector3.one * (raioDevorar * 2.2f);

        var goSwirlA = new GameObject("VorticeSwirlA");
        goSwirlA.transform.SetParent(transform);
        goSwirlA.transform.localPosition = Vector3.zero;
        srSwirlA               = goSwirlA.AddComponent<SpriteRenderer>();
        srSwirlA.sprite        = GerarSwirl();
        srSwirlA.color         = new Color(corVortice.r, corVortice.g, corVortice.b, 0.55f);
        srSwirlA.sortingOrder  = 5;
        goSwirlA.transform.localScale = Vector3.one * (raioAtracao * 0.9f);

        var goSwirlB = new GameObject("VorticeSwirlB");
        goSwirlB.transform.SetParent(transform);
        goSwirlB.transform.localPosition = Vector3.zero;
        srSwirlB               = goSwirlB.AddComponent<SpriteRenderer>();
        srSwirlB.sprite        = GerarSwirl();
        srSwirlB.color         = new Color(corVortice.r, corVortice.g + 0.1f, corVortice.b, 0.30f);
        srSwirlB.sortingOrder  = 4;
        goSwirlB.transform.localScale = goSwirlA.transform.localScale * 0.65f;
    }

    // ── Atração / devoração ──────────────────────────────────────────────────

    void Update()
    {
        if (concluido) return;

        srSwirlA.transform.Rotate(0f, 0f,  35f * Time.deltaTime);
        srSwirlB.transform.Rotate(0f, 0f, -50f * Time.deltaTime);
        float pulso = Mathf.Sin(Time.time * 3f) * 0.5f + 0.5f;
        srCore.transform.localScale = Vector3.one * (raioDevorar * 2.2f) * (0.9f + pulso * 0.15f);

        var processados = new HashSet<InimigoController>();
        var hits = Physics2D.OverlapCircleAll(transform.position, raioAtracao);
        foreach (var col in hits)
        {
            var ic = col.GetComponent<InimigoController>() ?? col.GetComponentInParent<InimigoController>();
            if (ic == null || ic.estaMorrendo || processados.Contains(ic)) continue;
            processados.Add(ic);

            Vector2 pos      = ic.transform.position;
            Vector2 toCenter = (Vector2)transform.position - pos;
            float   dist     = toCenter.magnitude;

            float t     = 1f - Mathf.Clamp01(dist / raioAtracao);
            float forca = Mathf.Lerp(0.4f, forcaAtracao, t * t);
            if (dist > 0.05f)
                ic.transform.position += (Vector3)(toCenter.normalized * forca * Time.deltaTime);

            if (dist <= raioDevorar)
                AplicarDanoVortice(ic);
        }
    }

    // Dano contínuo (em ticks) enquanto o inimigo estiver dentro do raio de devoração.
    // A morte segue o fluxo normal de InimigoController (Morrer→drops/XP) — só contamos
    // o progresso quando o OnPreMorte confirma que esse inimigo específico morreu aqui dentro.
    void AplicarDanoVortice(InimigoController ic)
    {
        emDevoracao.Add(ic);

        float acumulado = tickTimers.TryGetValue(ic, out float v) ? v : 0f;
        acumulado += Time.deltaTime;
        if (acumulado >= TICK_INTERVALO)
        {
            acumulado -= TICK_INTERVALO;
            ic.ReceberDano(danoPorSegundo * TICK_INTERVALO);
            SpawnSuccao(ic.transform.position);
        }
        tickTimers[ic] = acumulado;
    }

    void OnInimigoMorreu(InimigoController ic)
    {
        if (!emDevoracao.Remove(ic)) return;
        tickTimers.Remove(ic);

        progresso++;
        OnProgresso?.Invoke(progresso, quantidadeNecessaria);
        if (progresso >= quantidadeNecessaria && !concluido)
        {
            concluido = true;
            OnConcluido?.Invoke();
        }
    }

    // ── Partículas ───────────────────────────────────────────────────────────

    void SpawnSuccao(Vector2 pos)
    {
        for (int i = 0; i < 4; i++)
        {
            var go = new GameObject("VorticeSuccao");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = GerarDisco();
            sr.color        = new Color(corVortice.r, corVortice.g, corVortice.b, 0.8f);
            sr.sortingOrder = 6;
            go.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.05f, 0.15f);

            float ang = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float r0  = UnityEngine.Random.Range(0.3f, 1f);
            go.AddComponent<VorticeParticulaAutoDestroy>().Iniciar(
                transform.position, r0, ang, UnityEngine.Random.Range(4f, 8f),
                UnityEngine.Random.Range(0.3f, 0.5f), sr.color);
        }
    }

    IEnumerator ParticulasLoop()
    {
        while (this != null && !concluido)
        {
            yield return new WaitForSeconds(0.12f);
            if (this == null || concluido) yield break;
            SpawnParticulaAmbiente();
        }
    }

    void SpawnParticulaAmbiente()
    {
        var go = new GameObject("VorticeParticula");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = GerarDisco();
        sr.color        = new Color(corVortice.r + 0.1f, corVortice.g, corVortice.b + 0.1f, 0.7f);
        sr.sortingOrder = 5;
        go.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.04f, 0.10f);

        float ang = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        go.AddComponent<VorticeParticulaAutoDestroy>().Iniciar(
            transform.position, raioAtracao * 0.95f, ang,
            UnityEngine.Random.Range(2f, 4f), UnityEngine.Random.Range(1.4f, 2.0f), sr.color);
    }

    // ── Sprites procedurais ──────────────────────────────────────────────────

    static Sprite _disco, _swirl;

    static Sprite GerarDisco()
    {
        if (_disco != null) return _disco;
        int sz = 64;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cx));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d / cx)));
        }
        tex.Apply();
        _disco = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
        return _disco;
    }

    // Espiral de 3 braços que desbota nas bordas — usado em duas camadas girando em sentidos opostos
    static Sprite GerarSwirl()
    {
        if (_swirl != null) return _swirl;
        int sz = 64;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float dx = x + 0.5f - cx, dy = y + 0.5f - cx;
            float r  = Mathf.Sqrt(dx * dx + dy * dy) / cx;
            if (r > 1f) { tex.SetPixel(x, y, Color.clear); continue; }

            float ang     = Mathf.Atan2(dy, dx);
            float spiral  = Mathf.Sin(ang * 3f + r * 9f);
            float band    = Mathf.Clamp01(1f - Mathf.Abs(spiral) * 2.2f);
            float fadeEdge = Mathf.Clamp01(1f - r);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, band * fadeEdge));
        }
        tex.Apply();
        _swirl = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
        return _swirl;
    }
}

// Anima partículas em espiral até o centro do vórtice, de forma independente
public class VorticeParticulaAutoDestroy : MonoBehaviour
{
    Vector2 centro;
    float r0, ang0, spin, dur, t;
    Color cor0;
    SpriteRenderer sr;

    public void Iniciar(Vector2 centroFixo, float raioInicial, float anguloInicial, float velocidadeSpin, float duracao, Color cor)
    {
        centro = centroFixo;
        r0     = raioInicial;
        ang0   = anguloInicial;
        spin   = velocidadeSpin;
        dur    = duracao;
        cor0   = cor;
        sr     = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        t += Time.deltaTime;
        float p   = Mathf.Clamp01(t / dur);
        float r   = r0 * (1f - p);
        float ang = ang0 + spin * t;
        transform.position = centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;

        if (sr != null)
        {
            Color c = cor0;
            c.a = Mathf.Lerp(cor0.a, 0f, Mathf.Clamp01((p - 0.6f) / 0.4f));
            sr.color = c;
        }
        if (t >= dur) Destroy(gameObject);
    }
}
