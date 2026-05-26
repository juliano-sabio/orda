using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BordaSangueEvento : MonoBehaviour
{
    private CanvasGroup cg;

    void Awake()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 95;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha          = 0f;
        cg.interactable   = false;
        cg.blocksRaycasts = false;

        var imgGO = new GameObject("Vinheta");
        imgGO.transform.SetParent(transform, false);

        var rt = imgGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = imgGO.AddComponent<Image>();
        img.sprite        = CriarSpriteBorda();
        img.type          = Image.Type.Simple;
        img.preserveAspect = false;
        img.color         = Color.white;
        img.raycastTarget = false;
    }

    Sprite CriarSpriteBorda()
    {
        const int SIZE = 256;
        var tex = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode   = TextureWrapMode.Clamp;

        var pixels = new Color[SIZE * SIZE];
        for (int y = 0; y < SIZE; y++)
        for (int x = 0; x < SIZE; x++)
        {
            float nx   = Mathf.Abs((x / (float)(SIZE - 1)) * 2f - 1f);
            float ny   = Mathf.Abs((y / (float)(SIZE - 1)) * 2f - 1f);
            float dist = Mathf.Max(nx, ny); // borda retangular
            float alpha = Mathf.SmoothStep(0.42f, 0.88f, dist) * 0.65f;
            pixels[y * SIZE + x] = new Color(0.55f, 0f, 0f, alpha);
        }
        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, SIZE, SIZE), new Vector2(0.5f, 0.5f));
    }

    public void Mostrar() => StartCoroutine(FadeAlpha(0f, 1f, 0.6f));

    public void Esconder() => StartCoroutine(FadeAlpha(cg.alpha, 0f, 0.5f, destruirAoFim: true));

    IEnumerator FadeAlpha(float de, float ate, float dur, bool destruirAoFim = false)
    {
        float t = 0f;
        while (t < dur)
        {
            t     += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(de, ate, t / dur);
            yield return null;
        }
        cg.alpha = ate;
        if (destruirAoFim) Destroy(gameObject);
    }
}
