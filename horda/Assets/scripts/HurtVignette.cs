using UnityEngine;
using UnityEngine.UI;

// Vinheta de dano / vida baixa: borda vermelha que PULSA ao apanhar e fica tensa com HP baixo.
// Player-local (lê PlayerStats.Local) → em co-op cada jogador vê a SUA própria vinheta na
// própria tela; sem RPC, sem conflito com transform sincronizado. Overlay screen-space próprio,
// auto-criado (não precisa estar em cena).
public class HurtVignette : MonoBehaviour
{
    static HurtVignette _inst;
    public static HurtVignette Instance
    {
        get
        {
            if (_inst == null)
            {
                var go = new GameObject("HurtVignette");
                DontDestroyOnLoad(go);
                _inst = go.AddComponent<HurtVignette>();
            }
            return _inst;
        }
    }

    // Chamado quando o player LOCAL apanha. 'intensidade' 0..1 (fração do dano levado).
    public static void Flash(float intensidade)
    {
        var i = Instance;
        float alvo = Mathf.Lerp(0.35f, 0.75f, Mathf.Clamp01(intensidade));
        if (alvo > i.flashAlpha) i.flashAlpha = alvo;
    }

    [Header("Vida baixa")]
    public float limiarHpBaixo = 0.35f;          // abaixo disso → pulso contínuo de tensão
    public Color cor = new Color(0.72f, 0.02f, 0.02f);

    Image img;
    float flashAlpha;

    void Awake()
    {
        var canvasGO = new GameObject("HurtVignetteCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;                // por cima do HUD
        canvasGO.AddComponent<CanvasScaler>();

        var imgGO = new GameObject("Vignette");
        imgGO.transform.SetParent(canvasGO.transform, false);
        img = imgGO.AddComponent<Image>();
        img.sprite = CriarVinheta();
        img.raycastTarget = false;                 // não bloqueia cliques
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        SetA(0f);
    }

    void Update()
    {
        // O flash decai em tempo REAL (não depende do timeScale do hit-stop).
        flashAlpha = Mathf.Max(0f, flashAlpha - Time.unscaledDeltaTime * 2.2f);

        float lowHp = 0f;
        var p = PlayerStats.Local;
        if (p != null && !p.EstaCaido)
        {
            float maxH = p.GetMaxHealth();
            float ratio = maxH > 0f ? Mathf.Clamp01(p.health / maxH) : 1f;
            if (ratio < limiarHpBaixo)
            {
                float t = 1f - ratio / limiarHpBaixo;                    // 0 no limiar → 1 quase morto
                float pulso = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.Lerp(3f, 7f, t));
                lowHp = Mathf.Lerp(0.1f, 0.5f, t) * pulso;
            }
        }

        SetA(Mathf.Max(flashAlpha, lowHp));
    }

    void SetA(float a)
    {
        if (img == null) return;
        img.color = new Color(cor.r, cor.g, cor.b, a);
    }

    // Vinheta radial: transparente no miolo, vermelha forte na borda da tela.
    static Sprite s_vinheta;
    static Sprite CriarVinheta()
    {
        if (s_vinheta != null) return s_vinheta;
        const int sz = 256;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        Vector2 c = new Vector2(sz / 2f, sz / 2f);
        float maxd = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) / maxd; // 0 centro → ~1.4 canto
            float a = Mathf.Clamp01((d - 0.55f) / 0.45f);
            a = Mathf.Pow(a, 1.6f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        s_vinheta = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
        return s_vinheta;
    }
}
