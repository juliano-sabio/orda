using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class EfeitoCuraFX : MonoBehaviour
{
    const float DURACAO = 1.4f;

    void Start() => StartCoroutine(Animar());

    IEnumerator Animar()
    {
        // Anel verde que expande e some
        var ringGO = new GameObject("Ring");
        ringGO.transform.SetParent(transform, false);
        var sr = ringGO.AddComponent<SpriteRenderer>();
        sr.sprite       = CriarAnel();
        sr.color        = new Color(0.2f, 1f, 0.45f, 0.9f);
        sr.sortingOrder = 20;

        // Luz
        var luzGO = new GameObject("Luz");
        luzGO.transform.SetParent(transform, false);
        var luz = luzGO.AddComponent<Light2D>();
        luz.lightType             = Light2D.LightType.Point;
        luz.color                 = new Color(0.2f, 1f, 0.45f);
        luz.intensity             = 3f;
        luz.pointLightOuterRadius = 3f;
        luz.shadowsEnabled        = false;

        float t = 0f;
        while (t < DURACAO)
        {
            t += Time.deltaTime;
            float p = t / DURACAO;

            float escala = Mathf.Lerp(0.5f, 5f, p);
            ringGO.transform.localScale = Vector3.one * escala;

            float alpha = p < 0.2f
                ? Mathf.Lerp(0f, 0.9f, p / 0.2f)
                : Mathf.Lerp(0.9f, 0f, (p - 0.2f) / 0.8f);
            sr.color  = new Color(0.2f, 1f, 0.45f, alpha);
            luz.intensity = alpha * 3f;

            yield return null;
        }

        Destroy(gameObject);
    }

    Sprite CriarAnel()
    {
        const int S = 64;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float c = S / 2f;
        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            float d  = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
            float nr = d / c;
            float a  = Mathf.SmoothStep(0.5f, 0.68f, nr) * (1f - Mathf.SmoothStep(0.82f, 1f, nr));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f));
    }
}
