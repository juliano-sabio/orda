using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelUpEffect : MonoBehaviour
{
    [Header("Anel de luz")]
    public Color corAnel      = new Color(1f, 0.85f, 0.2f, 1f);
    public float raioFinal    = 3f;
    public float duracaoAnel  = 0.6f;

    [Header("Flash no sprite")]
    public Color corFlash     = new Color(1f, 0.9f, 0.3f, 1f);
    public int   qtdFlashes   = 3;
    public float duracaoFlash = 0.07f;

    [Header("Texto")]
    public float alturaTexto  = 1.8f;
    public float duracaoTexto = 1.2f;

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Executar(int novoLevel)
    {
        StartCoroutine(Anel());
        StartCoroutine(Burst());     // pancada central (o "punch" do level-up)
        StartCoroutine(Faiscas());   // faíscas douradas subindo
        StartCoroutine(FlashSprite());
        StartCoroutine(TextoFlutuante(novoLevel));
    }

    // ── Pancada central: disco branco-dourado que estoura e some ──
    IEnumerator Burst()
    {
        var go = new GameObject("LevelUpBurst");
        go.transform.position = transform.position;
        var sb = go.AddComponent<SpriteRenderer>();
        sb.sprite       = Disco();
        sb.sortingOrder = sr != null ? sr.sortingOrder + 3 : 11;
        const float dur = 0.18f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            go.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 2.4f, Mathf.SmoothStep(0f, 1f, p));
            Color c = Color.Lerp(new Color(1f, 1f, 0.85f), corAnel, p);
            c.a = Mathf.Lerp(0.9f, 0f, p);
            sb.color = c;
            yield return null;
        }
        Destroy(go);
    }

    // ── Faíscas douradas saindo do player + caindo ──
    IEnumerator Faiscas()
    {
        const int qtd = 6;
        for (int i = 0; i < qtd; i++)
        {
            var go = new GameObject("LevelUpFaisca");
            go.transform.position = transform.position;
            var s = go.AddComponent<SpriteRenderer>();
            s.sprite       = Disco();
            s.color        = corAnel;
            s.sortingOrder = sr != null ? sr.sortingOrder + 3 : 11;
            float tam = Random.Range(0.08f, 0.16f);
            go.transform.localScale = Vector3.one * tam;
            Vector2 vel = new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(2f, 3.5f));
            StartCoroutine(AnimarFaisca(go, vel, tam));
        }
        yield break;
    }

    IEnumerator AnimarFaisca(GameObject go, Vector2 vel, float tam)
    {
        var s = go.GetComponent<SpriteRenderer>();
        float dur = Random.Range(0.4f, 0.6f);
        float t = 0f;
        while (t < dur)
        {
            if (go == null) yield break;
            t += Time.deltaTime;
            float p = t / dur;
            vel.y -= 5f * Time.deltaTime;
            go.transform.position += (Vector3)(vel * Time.deltaTime);
            go.transform.localScale = Vector3.one * Mathf.Lerp(tam, tam * 0.2f, p);
            if (s != null) { Color c = s.color; c.a = Mathf.Lerp(1f, 0f, p * p); s.color = c; }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    // Disco radial cacheado (igual ao padrão dos outros efeitos do projeto)
    static Sprite s_disco;
    static Sprite Disco()
    {
        if (s_disco != null) return s_disco;
        const int sz = 32;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float c = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c)) / c;
            float a = Mathf.Clamp01(1f - d); a *= a;
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        s_disco = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
        return s_disco;
    }

    // ── Anel dourado expandindo ──────────────────────────────────
    IEnumerator Anel()
    {
        int seg       = 40;
        GameObject go = new GameObject("LevelUpAnel");
        go.transform.position = transform.position;

        LineRenderer lr  = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop          = true;
        lr.positionCount = seg;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = sr != null ? sr.sortingOrder + 2 : 10;

        for (int i = 0; i < seg; i++)
        {
            float a = (360f / seg) * i * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f));
        }

        float t = 0f;
        while (t < duracaoAnel)
        {
            t += Time.deltaTime;
            float p    = Mathf.Clamp01(t / duracaoAnel);
            float raio = Mathf.Lerp(0.1f, raioFinal, Mathf.Sqrt(p)); // snappy: estoura rápido e desacelera
            float alpha = Mathf.Lerp(1f, 0f, Mathf.Pow(p, 0.6f));

            go.transform.localScale = Vector3.one * raio;
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.3f, 0.02f, p);
            lr.startColor = lr.endColor = new Color(corAnel.r, corAnel.g, corAnel.b, alpha);
            yield return null;
        }

        Destroy(go);
    }

    // ── Sprite pisca dourado ─────────────────────────────────────
    IEnumerator FlashSprite()
    {
        if (sr == null) yield break;

        for (int i = 0; i < qtdFlashes; i++)
        {
            sr.color = corFlash;
            yield return new WaitForSeconds(duracaoFlash);
            sr.color = Color.white;
            yield return new WaitForSeconds(duracaoFlash);
        }
        sr.color = Color.white;
    }

    // ── "LEVEL UP!" sobe e some ──────────────────────────────────
    IEnumerator TextoFlutuante(int level)
    {
        // Canvas de overlay
        GameObject canvasGO   = new GameObject("LevelUpCanvas");
        Canvas canvas         = canvasGO.AddComponent<Canvas>();
        canvas.renderMode     = RenderMode.WorldSpace;
        canvas.sortingOrder   = 20;
        canvasGO.AddComponent<CanvasScaler>();

        // Texto
        GameObject txtGO      = new GameObject("Txt");
        txtGO.transform.SetParent(canvasGO.transform, false);

        RectTransform r       = txtGO.AddComponent<RectTransform>();
        r.sizeDelta           = new Vector2(4f, 1.5f);

        TextMeshProUGUI txt   = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text              = $"{Loc.T("ui.level_up")}\n{Loc.T("ui.level_abbr")} {level}";
        txt.fontSize          = 0.55f;
        txt.fontStyle         = FontStyles.Bold;
        txt.alignment         = TextAlignmentOptions.Center;
        txt.color             = new Color(1f, 0.9f, 0.2f, 1f);

        Vector3 posInicio = transform.position + Vector3.up * 0.5f;
        Vector3 posFim    = posInicio + Vector3.up * alturaTexto;

        float t = 0f;
        while (t < duracaoTexto)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duracaoTexto);

            canvasGO.transform.position = Vector3.Lerp(posInicio, posFim, Mathf.SmoothStep(0f, 1f, p));

            float alpha = p < 0.6f ? 1f : Mathf.Lerp(1f, 0f, (p - 0.6f) / 0.4f);
            txt.color = new Color(1f, 0.9f, 0.2f, alpha);

            yield return null;
        }

        Destroy(canvasGO);
    }
}
