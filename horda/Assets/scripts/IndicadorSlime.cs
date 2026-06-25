using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IndicadorSlime : MonoBehaviour
{
    public Transform alvo;
    public Color     corSeta = new Color(1f, 0.25f, 0.9f);
    public string    label   = "Slime!";
    public bool      soForaDaTela; // se true, some quando o alvo está visível na tela (usado p/ aliado)

    private Canvas      canvas;
    private RectTransform seta;
    private TextMeshProUGUI texto;
    private CanvasGroup cg;
    private float       blink;
    private Camera      cam;

    const float MARGIN = 80f;

    // Co-op: cria um indicador apontando pra um alvo (usado pelos objetos de evento cosméticos no cliente).
    public static IndicadorSlime Criar(Transform alvo, Color cor, string label, bool soForaDaTela = false)
    {
        var go  = new GameObject("IndicadorCosmetico");
        var ind = go.AddComponent<IndicadorSlime>();
        ind.alvo         = alvo;
        ind.corSeta      = cor;
        ind.label        = label;
        ind.soForaDaTela = soForaDaTela;
        return ind;
    }

    void Start()
    {
        cam = Camera.main;

        var canvasGO = new GameObject("IndicadorSlimeCanvas");
        DontDestroyOnLoad(canvasGO);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Âncora no centro do canvas
        var root = new GameObject("IndicadorRoot");
        root.transform.SetParent(canvasGO.transform, false);
        seta = root.AddComponent<RectTransform>();
        seta.anchorMin = seta.anchorMax = new Vector2(0.5f, 0.5f);
        seta.sizeDelta = new Vector2(100, 100);
        cg = root.AddComponent<CanvasGroup>();
        cg.interactable    = false;
        cg.blocksRaycasts  = false;

        // Seta (▲ Unicode, rotacionada depois)
        var arrowGO  = new GameObject("Seta");
        arrowGO.transform.SetParent(root.transform, false);
        var arrowRT  = arrowGO.AddComponent<RectTransform>();
        arrowRT.sizeDelta        = new Vector2(60, 60);
        arrowRT.anchoredPosition = Vector2.zero;
        var arrowTxt = arrowGO.AddComponent<TextMeshProUGUI>();
        arrowTxt.text      = "^";
        arrowTxt.fontSize  = 48;
        arrowTxt.color     = corSeta;
        arrowTxt.alignment = TextAlignmentOptions.Center;
        arrowTxt.textWrappingMode = TMPro.TextWrappingModes.NoWrap;

        // Texto com distância
        var txtGO  = new GameObject("Texto");
        txtGO.transform.SetParent(root.transform, false);
        var txtRT  = txtGO.AddComponent<RectTransform>();
        txtRT.sizeDelta        = new Vector2(180, 30);
        txtRT.anchoredPosition = new Vector2(0, -52);
        texto = txtGO.AddComponent<TextMeshProUGUI>();
        texto.fontSize   = 15;
        texto.fontStyle  = FontStyles.Bold;
        texto.color      = Color.white;
        texto.alignment  = TextAlignmentOptions.Center;
        texto.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
    }

    void Update()
    {
        if (alvo == null) { Destroy(gameObject); return; }
        if (soForaDaTela && LobbyState.EmLobby) { if (cg != null) cg.alpha = 0f; return; } // aliado: não no lobby
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        // Pisca
        blink += Time.deltaTime * 4f;
        if (cg != null) cg.alpha = 0.65f + Mathf.Sin(blink) * 0.35f;

        var canvasRT = (RectTransform)canvas.transform;
        float hw = canvasRT.rect.width  * 0.5f;
        float hh = canvasRT.rect.height * 0.5f;

        Vector3 vp = cam.WorldToViewportPoint(alvo.position);
        bool nasTela = vp.z > 0 && vp.x >= 0.05f && vp.x <= 0.95f
                                 && vp.y >= 0.05f && vp.y <= 0.95f;

        // Aliado: só mostra a seta quando ele está FORA da tela (perto/visível não precisa).
        if (soForaDaTela && nasTela) { if (cg != null) cg.alpha = 0f; return; }

        Vector2 canvasPos;

        if (nasTela)
        {
            // Posiciona acima da slime na tela, clampado dentro do canvas
            canvasPos = new Vector2(
                Mathf.Clamp((vp.x - 0.5f) * canvasRT.rect.width,  -hw + MARGIN, hw - MARGIN),
                Mathf.Clamp((vp.y - 0.5f) * canvasRT.rect.height + 55f, -hh + MARGIN, hh - MARGIN)
            );
            seta.localRotation = Quaternion.identity;
        }
        else
        {
            // Direção do centro para a posição projetada
            Vector2 dir = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
            if (vp.z < 0) dir = -dir; // atrás da câmera
            dir.Normalize();

            float effHW = hw - MARGIN;
            float effHH = hh - MARGIN;
            float absX  = Mathf.Abs(dir.x);
            float absY  = Mathf.Abs(dir.y);
            float scaleX = absX > 0.001f ? effHW / absX : float.MaxValue;
            float scaleY = absY > 0.001f ? effHH / absY : float.MaxValue;
            canvasPos = dir * Mathf.Min(scaleX, scaleY);

            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            seta.localRotation = Quaternion.Euler(0, 0, ang - 90f);
        }

        seta.anchoredPosition = canvasPos;

        float dist = Vector2.Distance(
            new Vector2(cam.transform.position.x, cam.transform.position.y),
            new Vector2(alvo.position.x, alvo.position.y)
        );
        if (texto != null)
            texto.text = $"{label} {dist:F0}m";
    }

    void OnDestroy()
    {
        if (canvas != null) Destroy(canvas.gameObject);
    }
}
