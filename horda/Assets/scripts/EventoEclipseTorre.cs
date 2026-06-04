using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;

public class EventoEclipseTorre : MonoBehaviour
{
    [Header("Torre")]
    public float vidaTorre         = 800f;
    public float danoInimigoTorre  = 12f;

    [Header("Eclipse")]
    public float raioVisao         = 4.5f;
    public float intensidadeEclipse = 0.88f;

    PlayerStats player;
    float       duracao;

    TorreDefesa    torre;
    GameObject     eclipseCanvasGO;
    Image          overlayImg;
    Light2D        luzPlayer;
    Light2D        luzGlobal;
    float          intensidadeOriginal;
    IndicadorSlime indicadorTorre;

    Image             barraFill;
    TextMeshProUGUI   textoVidaTorre;
    Image             barraFillImg;
    Image             hudBorda;
    float             tempoFlavor;

    static readonly Color COR_BORDA_NORMAL  = new Color(0.3f, 0.6f, 1f,  0.5f);
    static readonly Color COR_BORDA_PERIGO  = new Color(1f,   0.1f, 0.1f, 0.9f);

    public bool TorreMorreu  { get; private set; }
    public bool EventoAtivo  { get; private set; }

    // ── Init ──────────────────────────────────────────────────────────────────

    public void Iniciar(PlayerStats ps, Vector2 centro, float dur)
    {
        player  = ps;
        duracao = dur;
        EventoAtivo = true;

        CriarTorre(centro);
        CriarEclipse();
        CriarHUDTorre();
        CriarIndicador();
        FlowField.AlvoOverride = torre.transform;

        torre.OnMorreu += () =>
        {
            TorreMorreu = true;
            EventoAtivo = false;
        };
    }

    void OnDestroy()
    {
        FlowField.AlvoOverride = null;
        if (indicadorTorre != null) Destroy(indicadorTorre.gameObject);

        // Fade suave — roda em GO independente para sobreviver ao Destroy
        var faderGO = new GameObject("EclipseFader");
        faderGO.AddComponent<EclipseFaderSaida>().Iniciar(
            luzGlobal, intensidadeOriginal, luzPlayer, overlayImg, eclipseCanvasGO);

        luzPlayer       = null;
        eclipseCanvasGO = null;
    }

    // ── Torre ─────────────────────────────────────────────────────────────────

    void CriarTorre(Vector2 pos)
    {
        var go = new GameObject("TorreDefesa");
        go.transform.position = pos;

        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = GerarSpriteTorre(48);
        sr.sortingOrder = 8;
        go.transform.localScale = Vector3.one * 1.5f;

        var col       = go.AddComponent<CircleCollider2D>();
        col.radius    = 0.5f;
        col.isTrigger = true;

        // Luz pulsante
        var luzGO = new GameObject("LuzTorre");
        luzGO.transform.SetParent(go.transform, false);
        var luz               = luzGO.AddComponent<Light2D>();
        luz.lightType         = Light2D.LightType.Point;
        luz.color             = new Color(0.55f, 0.85f, 1f);
        luz.intensity         = 5f;
        luz.pointLightOuterRadius = 8f;
        luz.pointLightInnerRadius = 1.5f;
        luz.shadowsEnabled    = false;

        torre             = go.AddComponent<TorreDefesa>();
        torre.vidaMaxima  = vidaTorre;
        torre.danoInimigo = danoInimigoTorre;
        torre.luz         = luz;
    }

    // ── Eclipse ───────────────────────────────────────────────────────────────

    void CriarEclipse()
    {
        // Overlay escuro em canvas
        eclipseCanvasGO = new GameObject("EclipseCanvas");
        var canvas          = eclipseCanvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5; // abaixo da UI do jogo
        eclipseCanvasGO.AddComponent<CanvasScaler>();

        var overlay       = new GameObject("Overlay");
        overlay.transform.SetParent(eclipseCanvasGO.transform, false);
        var rt            = overlay.AddComponent<RectTransform>();
        rt.anchorMin      = Vector2.zero;
        rt.anchorMax      = Vector2.one;
        rt.offsetMin      = rt.offsetMax = Vector2.zero;
        overlayImg        = overlay.AddComponent<Image>();
        overlayImg.color  = new Color(0f, 0f, 0.02f, intensidadeEclipse);

        // Luz no player
        if (player != null)
        {
            var luzGO = new GameObject("LuzEclipsePlayer");
            luzGO.transform.SetParent(player.transform, false);
            luzPlayer                     = luzGO.AddComponent<Light2D>();
            luzPlayer.lightType           = Light2D.LightType.Point;
            luzPlayer.color               = new Color(0.9f, 0.85f, 0.7f);
            luzPlayer.intensity           = 1.8f;
            luzPlayer.pointLightOuterRadius = raioVisao;
            luzPlayer.pointLightInnerRadius = raioVisao * 0.3f;
            luzPlayer.shadowsEnabled      = false;
        }

        // Reduz luz global se existir
        luzGlobal = Object.FindFirstObjectByType<Light2D>();
        if (luzGlobal != null && luzGlobal.lightType == Light2D.LightType.Global)
        {
            intensidadeOriginal  = luzGlobal.intensity;
            luzGlobal.intensity  = 0.08f;
        }
        else
        {
            luzGlobal = null;
        }
    }

    void CriarIndicador()
    {
        if (torre == null) return;
        var go         = new GameObject("IndicadorTorre");
        indicadorTorre = go.AddComponent<IndicadorSlime>();
        indicadorTorre.alvo    = torre.transform;
        indicadorTorre.corSeta = new Color(0.5f, 0.8f, 1f);
    }

    // ── HUD da Torre ─────────────────────────────────────────────────────────

    void CriarHUDTorre()
    {
        var canvasGO = new GameObject("HUDTorCanvas");
        canvasGO.transform.SetParent(eclipseCanvasGO.transform, false);
        // reusar o mesmo canvas eclipse (sorting já definido)

        var painelGO             = new GameObject("HUDTorre");
        painelGO.transform.SetParent(eclipseCanvasGO.transform, false);
        var painelRT             = painelGO.AddComponent<RectTransform>();
        painelRT.anchorMin       = new Vector2(0.5f, 1f);
        painelRT.anchorMax       = new Vector2(0.5f, 1f);
        painelRT.pivot           = new Vector2(0.5f, 1f);
        painelRT.sizeDelta       = new Vector2(360f, 52f);
        painelRT.anchoredPosition = new Vector2(0f, -70f);

        var bg      = painelGO.AddComponent<Image>();
        bg.color    = new Color(0.03f, 0.04f, 0.1f, 0.88f);

        // Borda que muda de cor quando player está em perigo
        var bordaGO  = new GameObject("Borda");
        bordaGO.transform.SetParent(painelGO.transform, false);
        var bordaRT  = bordaGO.AddComponent<RectTransform>();
        bordaRT.anchorMin = Vector2.zero; bordaRT.anchorMax = Vector2.one;
        bordaRT.offsetMin = new Vector2(-2f, -2f); bordaRT.offsetMax = new Vector2(2f, 2f);
        hudBorda       = bordaGO.AddComponent<Image>();
        hudBorda.color = COR_BORDA_NORMAL;

        // Título
        var tituloGO = new GameObject("Titulo");
        tituloGO.transform.SetParent(painelGO.transform, false);
        var tRT = tituloGO.AddComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f,0.52f); tRT.anchorMax = new Vector2(1f,1f);
        tRT.offsetMin = new Vector2(8f,0f);    tRT.offsetMax = new Vector2(-8f,0f);
        var tTMP = tituloGO.AddComponent<TextMeshProUGUI>();
        tTMP.text      = "[]  PROTEJA A TORRE";
        tTMP.fontSize  = 11f;
        tTMP.fontStyle = FontStyles.Bold;
        tTMP.alignment = TextAlignmentOptions.Center;
        tTMP.color     = new Color(0.5f, 0.8f, 1f);

        // Barra de HP
        var trackGO = new GameObject("Track");
        trackGO.transform.SetParent(painelGO.transform, false);
        var trackRT = trackGO.AddComponent<RectTransform>();
        trackRT.anchorMin = new Vector2(0f,0f); trackRT.anchorMax = new Vector2(1f,0.5f);
        trackRT.offsetMin = new Vector2(8f,4f); trackRT.offsetMax = new Vector2(-8f,0f);
        trackGO.AddComponent<Image>().color = new Color(0f,0.05f,0.12f);

        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(trackGO.transform, false);
        var fillRT = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(1f,1f); fillRT.offsetMax = new Vector2(-1f,-1f);
        barraFillImg             = fillGO.AddComponent<Image>();
        barraFillImg.type        = Image.Type.Filled;
        barraFillImg.fillMethod  = Image.FillMethod.Horizontal;
        barraFillImg.fillAmount  = 1f;
        barraFillImg.color       = new Color(0.3f, 0.7f, 1f);

        if (torre != null)
        {
            torre.barraFill = barraFillImg;
        }
    }

    // ── Update ────────────────────────────────────────────────────────────────

    void Update()
    {
        if (torre == null) return;

        // Pisca luz da torre conforme HP diminui
        if (torre.luz != null)
        {
            float pct   = torre.VidaAtual / vidaTorre;
            float pulso = Mathf.Sin(Time.time * (2f + (1f - pct) * 8f)) * 0.5f + 0.5f;
            torre.luz.intensity             = Mathf.Lerp(1f, 5f,  pulso) * Mathf.Lerp(0.4f, 1f, pct);
            torre.luz.pointLightOuterRadius = Mathf.Lerp(3f, 8f, pct);
            torre.luz.color                 = Color.Lerp(new Color(1f,0.3f,0.1f), new Color(0.55f,0.85f,1f), pct);
        }

        // Borda do HUD pisca vermelho quando player está longe da torre
        if (hudBorda != null && player != null && torre != null)
        {
            const float RAIO_SEGURO = 10f;
            float dist      = Vector2.Distance(player.transform.position, torre.transform.position);
            bool  emPerigo  = dist > RAIO_SEGURO;

            if (emPerigo)
            {
                tempoFlavor += Time.deltaTime * 5f;
                float alpha = Mathf.Abs(Mathf.Sin(tempoFlavor)) * 0.85f + 0.15f;
                hudBorda.color = new Color(COR_BORDA_PERIGO.r, COR_BORDA_PERIGO.g,
                                           COR_BORDA_PERIGO.b, alpha);
            }
            else
            {
                hudBorda.color = Color.Lerp(hudBorda.color, COR_BORDA_NORMAL, Time.deltaTime * 4f);
            }
        }
    }

    // ── Sprite da Torre ──────────────────────────────────────────────────────

    static Sprite GerarSpriteTorre(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        float cx = sz * 0.5f;

        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float nx = (x - cx) / cx;
            float ny = (y - cx) / cx;

            // Forma de cristal: losango esticado verticalmente
            float d = Mathf.Abs(nx) * 0.7f + Mathf.Abs(ny - 0.15f) * 0.55f;
            float a = Mathf.Clamp01(1f - d / 0.85f);

            // Gradiente de cor (azul claro → branco no topo)
            float bright = Mathf.Clamp01(1f - d * 0.8f);
            float b = Mathf.Lerp(0.6f, 1f, bright);
            float g = Mathf.Lerp(0.75f, 1f, bright);

            tex.SetPixel(x, y, new Color(b * 0.7f, g * 0.9f, 1f, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }
}

// ── Torre ────────────────────────────────────────────────────────────────────

public class TorreDefesa : MonoBehaviour
{
    public float vidaMaxima  = 800f;
    public float danoInimigo = 12f;
    public Light2D    luz;
    public Image      barraFill;

    public float VidaAtual { get; private set; }
    public event System.Action OnMorreu;

    bool morreu;

    void Start()
    {
        VidaAtual = vidaMaxima;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (morreu || !other.CompareTag("Enemy")) return;
        TakeDamage(danoInimigo * Time.deltaTime);
    }

    void TakeDamage(float d)
    {
        VidaAtual = Mathf.Max(0f, VidaAtual - d);
        AtualizarBarra();
        if (VidaAtual <= 0f && !morreu)
        {
            morreu = true;
            OnMorreu?.Invoke();
            StartCoroutine(EfeitoMorte());
        }
    }

    void AtualizarBarra()
    {
        if (barraFill == null) return;
        float pct = VidaAtual / vidaMaxima;
        barraFill.fillAmount = pct;
        barraFill.color      = Color.Lerp(new Color(1f,0.2f,0.1f), new Color(0.3f,0.7f,1f), pct);
    }

    IEnumerator EfeitoMorte()
    {
        var sr = GetComponent<SpriteRenderer>();
        for (float t = 0f; t < 0.6f; t += Time.deltaTime)
        {
            if (sr != null) sr.color = new Color(1f, 0.3f, 0.1f,
                Mathf.Lerp(1f, 0f, t / 0.6f));
            if (luz != null) luz.intensity = Mathf.Lerp(4f, 0f, t / 0.6f);
            yield return null;
        }
        Destroy(gameObject);
    }
}

// Fade de saída do eclipse — roda em GO independente
public class EclipseFaderSaida : MonoBehaviour
{
    public void Iniciar(Light2D luzGlobal, float intensidadeAlvo,
                        Light2D luzPlayer, Image overlay, GameObject canvas)
        => StartCoroutine(Fade(luzGlobal, intensidadeAlvo, luzPlayer, overlay, canvas));

    IEnumerator Fade(Light2D luzGlobal, float intensidadeAlvo,
                     Light2D luzPlayer, Image overlay, GameObject canvas)
    {
        float dur          = 1.5f;
        float startGlobal  = luzGlobal  != null ? luzGlobal.intensity  : 0f;
        float startPlayer  = luzPlayer  != null ? luzPlayer.intensity  : 0f;
        float startOverlay = overlay    != null ? overlay.color.a      : 0f;

        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;

            if (luzGlobal != null)
                luzGlobal.intensity = Mathf.Lerp(startGlobal, intensidadeAlvo, p);

            if (luzPlayer != null)
                luzPlayer.intensity = Mathf.Lerp(startPlayer, 0f, p);

            if (overlay != null)
            {
                Color c = overlay.color;
                c.a = Mathf.Lerp(startOverlay, 0f, p);
                overlay.color = c;
            }

            yield return null;
        }

        if (luzGlobal != null) luzGlobal.intensity = intensidadeAlvo;
        if (luzPlayer != null) Destroy(luzPlayer.gameObject);
        if (canvas    != null) Destroy(canvas);
        Destroy(gameObject);
    }
}
