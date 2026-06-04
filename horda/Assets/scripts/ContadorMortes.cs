using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ContadorMortes : MonoBehaviour
{
    int mortes = 0;
    TextMeshProUGUI textoContador;
    static ContadorMortes _instance;
    public static ContadorMortes Instance => _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCriar()
    {
        if (_instance != null) return;
        var go = new GameObject("ContadorMortes");
        DontDestroyOnLoad(go);
        go.AddComponent<ContadorMortes>();
    }

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
    }

    void Start()
    {
        InimigoController.OnInimigoDerrotado += OnMorte;
        CriarTexto();
        Atualizar();
    }

    void OnDestroy()
    {
        InimigoController.OnInimigoDerrotado -= OnMorte;
        if (_instance == this) _instance = null;
    }

    void OnMorte()
    {
        mortes++;
        Atualizar();
    }

    public void ResetarContador() { mortes = 0; Atualizar(); }

    void Atualizar()
    {
        if (textoContador != null)
            textoContador.text = $"Mortes: {mortes}";
    }

    void CriarTexto()
    {
        // Canvas raiz próprio — não fica sob nenhum outro objeto
        var cvGO = new GameObject("ContadorMortesCanvas");
        DontDestroyOnLoad(cvGO);

        var cv = cvGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 55;

        var scaler = cvGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        cvGO.AddComponent<GraphicRaycaster>();

        var go = new GameObject("ContadorMortesTexto");
        go.transform.SetParent(cvGO.transform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.sizeDelta        = new Vector2(200f, 35f);
        rt.anchoredPosition = new Vector2(0f, -88f); // abaixo do relogio

        textoContador = go.AddComponent<TextMeshProUGUI>();
        textoContador.fontSize     = 20;
        textoContador.fontStyle    = FontStyles.Bold;
        textoContador.alignment    = TextAlignmentOptions.Center;
        textoContador.color        = new Color(1f, 0.9f, 0.2f);
        textoContador.outlineWidth = 0.25f;
        textoContador.outlineColor = new Color32(0, 0, 0, 220);
        textoContador.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
    }
}
