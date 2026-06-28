using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Tela de entrada do estúdio: fundo escuro, logo recortada em círculo + "axulote.dev" embaixo,
// com fade in → hold → fade out, antes do menu. Aparece UMA vez por execução. A logo é carregada
// de Resources/UI/axulote_logo (salve o PNG lá como Sprite). Sem a logo, mostra só o círculo+nome.
public class SplashScreen : MonoBehaviour
{
    static bool _jaMostrou;

    // Paleta da logo enviada (teal escuro sobre papel creme).
    static readonly Color creme    = new Color(0.937f, 0.902f, 0.816f, 1f);
    static readonly Color tealEscuro= new Color(0.243f, 0.314f, 0.376f, 1f);
    static readonly Color fundo    = new Color(0.043f, 0.035f, 0.063f, 1f); // quase preto roxo

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (_jaMostrou) return;
        _jaMostrou = true;
        var go = new GameObject("SplashScreen");
        DontDestroyOnLoad(go);
        go.AddComponent<SplashScreen>();
    }

    CanvasGroup cg;
    bool pular;

    void Start() => StartCoroutine(Rodar());

    void Update()
    {
        if (Input.anyKeyDown || Input.GetMouseButtonDown(0)) pular = true;
    }

    IEnumerator Rodar()
    {
        // ---- Canvas overlay no topo de tudo ----
        var canvasGO = new GameObject("SplashCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
        cg = canvasGO.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        // fundo escuro
        var bg = NovaImagem(canvasGO.transform, "BG", fundo);
        Esticar(bg.rectTransform);

        // ---- círculo (recorte) ----
        var maskGO = new GameObject("CircleMask", typeof(RectTransform), typeof(Image), typeof(Mask));
        maskGO.transform.SetParent(canvasGO.transform, false);
        var maskRT = maskGO.GetComponent<RectTransform>();
        maskRT.sizeDelta = new Vector2(320f, 320f);
        maskRT.anchoredPosition = new Vector2(0f, 60f);
        var maskImg = maskGO.GetComponent<Image>();
        maskImg.sprite = CirculoSprite();
        maskImg.color = creme;                 // miolo creme (= fundo da logo) → círculo uniforme
        maskGO.GetComponent<Mask>().showMaskGraphic = true;

        // logo dentro do círculo
        var logo = Resources.Load<Sprite>("ui/axulote_logo");
        if (logo != null)
        {
            var lg = NovaImagem(maskGO.transform, "Logo", Color.white);
            lg.sprite = logo;
            lg.preserveAspect = true;
            var lgRT = lg.rectTransform;
            lgRT.anchorMin = new Vector2(0.5f, 0.5f); lgRT.anchorMax = new Vector2(0.5f, 0.5f);
            lgRT.sizeDelta = new Vector2(300f, 300f);
            lgRT.anchoredPosition = Vector2.zero;
        }

        // ---- nome do estúdio ----
        var txtGO = new GameObject("Nome", typeof(RectTransform));
        txtGO.transform.SetParent(canvasGO.transform, false);
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text = "axulote.dev";
        txt.fontSize = 46f;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = creme;
        txt.characterSpacing = 6f;
        var txtRT = txt.rectTransform;
        txtRT.sizeDelta = new Vector2(700f, 80f);
        txtRT.anchoredPosition = new Vector2(0f, -180f);

        // ---- fade in → hold → fade out (tudo em tempo real) ----
        yield return Fade(0f, 1f, 0.5f);
        yield return Esperar(1.3f);
        yield return Fade(1f, 0f, 0.55f);

        Destroy(gameObject);
    }

    IEnumerator Fade(float de, float pra, float dur)
    {
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            if (pular) { cg.alpha = pra; yield break; }
            cg.alpha = Mathf.Lerp(de, pra, t / dur);
            yield return null;
        }
        cg.alpha = pra;
    }

    IEnumerator Esperar(float s)
    {
        for (float t = 0f; t < s; t += Time.unscaledDeltaTime)
        {
            if (pular) yield break;
            yield return null;
        }
    }

    static Image NovaImagem(Transform pai, string nome, Color cor)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);
        var img = go.AddComponent<Image>();
        img.color = cor;
        return img;
    }

    static void Esticar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static Sprite s_circ;
    static Sprite CirculoSprite()
    {
        if (s_circ != null) return s_circ;
        const int sz = 256;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float c = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c)) / c;
            float a = d <= 0.98f ? 1f : Mathf.Clamp01((1f - d) / 0.02f); // borda suave
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        s_circ = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
        return s_circ;
    }
}
