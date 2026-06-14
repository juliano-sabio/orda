using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class Fase2LuzManager : MonoBehaviour
{
    const string CENA_FASE2 = "segunda_fase";

    [Header("Drop de Espíritos de Luz")]
    public float chanceDropEspirito       = 0.50f;
    public float quantidadeLuzPorEspirito = 12f;

    static Fase2LuzManager _instance;
    static GameObject      canvasGO;
    static Transform       fillTransform;
    static Image           fillImg;
    static Image           glowImg;
    static Image           bordaImg;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCriar()
    {
        if (_instance != null) return;
        var go = new GameObject("Fase2LuzManager");
        DontDestroyOnLoad(go);
        go.AddComponent<Fase2LuzManager>();
    }

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
    }

    void Start()
    {
        CriarUI();
        InimigoController.OnPreMorte += OnPreMorteHandler;
    }

    void OnDestroy()
    {
        InimigoController.OnPreMorte -= OnPreMorteHandler;
        if (_instance == this) _instance = null;
    }

    void Update()
    {
        bool naFase2 = SceneManager.GetActiveScene().name == CENA_FASE2;
        if (canvasGO != null && canvasGO.activeSelf != naFase2)
            canvasGO.SetActive(naFase2);
    }

    static bool EhFantasma(InimigoController ic) =>
        ic.GetComponent<FantasmaVeneno>()         != null ||
        ic.GetComponent<FantasmaFogo>()           != null ||
        ic.GetComponent<FantasmaEletrico>()       != null ||
        ic.GetComponent<FantasmaGelo>()           != null ||
        ic.GetComponent<FantasmaVenenoAtirador>() != null;

    void OnPreMorteHandler(InimigoController ic)
    {
        if (SceneManager.GetActiveScene().name != CENA_FASE2) return;
        if (!EhFantasma(ic)) return;
        if (Random.value <= chanceDropEspirito)
            EspiritoDeLuz.Criar(ic.transform.position, quantidadeLuzPorEspirito);
    }

    public static void SincronizarUI(float pct)
    {
        pct = Mathf.Clamp01(pct);

        // Escala o fill pela base
        if (fillTransform != null)
            fillTransform.localScale = new Vector3(1f, pct, 1f);

        // Cor: ouro → laranja → vermelho conforme esvazia
        Color corFill = pct > 0.5f
            ? Color.Lerp(new Color(1f, 0.45f, 0.1f, 1f), new Color(1f, 0.88f, 0.2f, 1f), (pct - 0.5f) * 2f)
            : Color.Lerp(new Color(0.85f, 0.05f, 0.05f, 1f), new Color(1f, 0.45f, 0.1f, 1f), pct * 2f);

        if (fillImg  != null) fillImg.color  = corFill;
        if (glowImg  != null) glowImg.color  = new Color(corFill.r, corFill.g, corFill.b, 0.18f);

        // Borda pulsa quando baixa (< 25%)
        if (bordaImg != null)
        {
            float pulso  = pct < 0.25f ? 0.6f + Mathf.PingPong(Time.time * 4f, 0.4f) : 1f;
            Color corBorda = pct > 0.5f
                ? new Color(1f, 0.92f, 0.4f, pulso)
                : Color.Lerp(new Color(1f, 0.2f, 0.1f, pulso), new Color(1f, 0.6f, 0.1f, pulso), pct * 2f);
            bordaImg.color = corBorda;
        }

        bool naFase2 = SceneManager.GetActiveScene().name == CENA_FASE2;
        if (canvasGO != null && canvasGO.activeSelf != naFase2)
            canvasGO.SetActive(naFase2);
    }

    void CriarUI()
    {
        canvasGO = new GameObject("BarraDeLuzCanvas");
        DontDestroyOnLoad(canvasGO);

        var cv = canvasGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 300;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Container principal ──────────────────────────────────────
        var container = Filho(canvasGO, "LuzContainer");
        var rtC = container.AddComponent<RectTransform>();
        rtC.anchorMin        = new Vector2(1f, 0.5f);
        rtC.anchorMax        = new Vector2(1f, 0.5f);
        rtC.pivot            = new Vector2(1f, 0.5f);
        rtC.sizeDelta        = new Vector2(48f, 380f);
        rtC.anchoredPosition = new Vector2(-24f, 0f);

        // ── Ícone / label no topo ────────────────────────────────────
        var iconGO = Filho(container, "LuzIcone");
        var rtIcon = iconGO.AddComponent<RectTransform>();
        rtIcon.anchorMin        = new Vector2(0f, 1f);
        rtIcon.anchorMax        = new Vector2(1f, 1f);
        rtIcon.pivot            = new Vector2(0.5f, 0f);
        rtIcon.sizeDelta        = new Vector2(0f, 36f);
        rtIcon.anchoredPosition = new Vector2(0f, 6f);
        var txt = iconGO.AddComponent<TextMeshProUGUI>();
        txt.text      = "✦";
        txt.fontSize  = 22f;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color     = new Color(1f, 0.92f, 0.4f, 1f);

        // ── Glow externo (aura ao redor da barra) ───────────────────
        var glowExternoGO = Filho(container, "GlowExterno");
        var rtGE = glowExternoGO.AddComponent<RectTransform>();
        rtGE.anchorMin = new Vector2(0f, 0f);
        rtGE.anchorMax = new Vector2(1f, 1f);
        rtGE.offsetMin = new Vector2(-8f, -8f);
        rtGE.offsetMax = new Vector2(8f,  8f);
        glowImg = glowExternoGO.AddComponent<Image>();
        glowImg.color = new Color(1f, 0.88f, 0.2f, 0.18f);

        // ── Borda dourada ────────────────────────────────────────────
        var bordaGO = Filho(container, "Borda");
        var rtB = bordaGO.AddComponent<RectTransform>();
        rtB.anchorMin = Vector2.zero;
        rtB.anchorMax = Vector2.one;
        rtB.offsetMin = Vector2.zero;
        rtB.offsetMax = Vector2.zero;
        bordaImg = bordaGO.AddComponent<Image>();
        bordaImg.color = new Color(1f, 0.92f, 0.4f, 1f);

        // ── Fundo escuro interno ─────────────────────────────────────
        var fundoGO = Filho(bordaGO, "Fundo");
        var rtF = fundoGO.AddComponent<RectTransform>();
        rtF.anchorMin = Vector2.zero;
        rtF.anchorMax = Vector2.one;
        rtF.offsetMin = new Vector2(2f, 2f);
        rtF.offsetMax = new Vector2(-2f, -2f);
        var imgFundo = fundoGO.AddComponent<Image>();
        imgFundo.color = new Color(0.04f, 0.04f, 0.1f, 0.92f);

        // ── Fill com pivot na base ───────────────────────────────────
        var fillGO = Filho(fundoGO, "BarraDeLuzPreenchimento");
        var rtFill = fillGO.AddComponent<RectTransform>();
        rtFill.anchorMin = new Vector2(0f, 0f);
        rtFill.anchorMax = new Vector2(1f, 1f);
        rtFill.pivot     = new Vector2(0.5f, 0f);
        rtFill.offsetMin = new Vector2(3f, 3f);
        rtFill.offsetMax = new Vector2(-3f, -3f);
        fillImg = fillGO.AddComponent<Image>();
        fillImg.color = new Color(1f, 0.88f, 0.2f, 1f);
        fillTransform = fillGO.transform;

        // ── Brilho interno sobre o fill ──────────────────────────────
        var shineGO = Filho(fillGO, "Brilho");
        var rtS = shineGO.AddComponent<RectTransform>();
        rtS.anchorMin = new Vector2(0f, 0.6f);
        rtS.anchorMax = new Vector2(1f, 1f);
        rtS.offsetMin = Vector2.zero;
        rtS.offsetMax = Vector2.zero;
        var imgShine = shineGO.AddComponent<Image>();
        imgShine.color = new Color(1f, 1f, 1f, 0.12f);

        canvasGO.SetActive(false);
    }

    static GameObject Filho(GameObject pai, string nome)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai.transform, false);
        return go;
    }

    static GameObject Filho(Component pai, string nome) => Filho(pai.gameObject, nome);
}
