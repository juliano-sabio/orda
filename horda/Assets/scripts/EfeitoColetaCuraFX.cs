using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class EfeitoColetaCuraFX : MonoBehaviour
{
    void Start() => StartCoroutine(Animar());

    IEnumerator Animar()
    {
        const float DURACAO = 0.7f;

        // Anel vermelho que expande e some
        var ringGO = new GameObject("Ring");
        ringGO.transform.SetParent(transform, false);
        var sr = ringGO.AddComponent<SpriteRenderer>();
        sr.sprite       = CriarAnel();
        sr.color        = new Color(1f, 0.15f, 0.15f, 0.9f);
        sr.sortingOrder = 30;

        // Luz vermelha
        var luzGO = new GameObject("Luz");
        luzGO.transform.SetParent(transform, false);
        var luz = luzGO.AddComponent<Light2D>();
        luz.lightType             = Light2D.LightType.Point;
        luz.color                 = new Color(1f, 0.15f, 0.15f);
        luz.intensity             = 4f;
        luz.pointLightOuterRadius = 4f;
        luz.shadowsEnabled        = false;

        float t = 0f;
        while (t < DURACAO)
        {
            t += Time.deltaTime;
            float p = t / DURACAO;

            ringGO.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 6f, p);

            float alpha = p < 0.15f
                ? Mathf.Lerp(0f, 0.9f, p / 0.15f)
                : Mathf.Lerp(0.9f, 0f, (p - 0.15f) / 0.85f);
            sr.color      = new Color(1f, 0.15f, 0.15f, alpha);
            luz.intensity = alpha * 4f;

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
