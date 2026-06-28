using System.Collections;
using UnityEngine;

// Pop pequeno ao coletar XP (disco + 3 faíscas, na cor do XP). Local e auto-destrutível;
// disparado no OnDestroy do orbe → aparece nos dois lados em co-op (sem RPC).
public class XpColetaVFX : MonoBehaviour
{
    public static void Tocar(Vector3 pos, Color cor)
    {
        var root = new GameObject("XpPop");
        root.transform.position = pos;
        root.AddComponent<XpColetaVFX>().Iniciar(cor);
    }

    void Iniciar(Color cor)
    {
        StartCoroutine(Disco(cor));
        StartCoroutine(Flash());                                  // núcleo branco rápido = "pop"
        for (int i = 0; i < 6; i++) StartCoroutine(Faisca(cor, i));
        Destroy(gameObject, 0.5f);
    }

    // Núcleo branco que estoura e some bem rápido — dá o "estalo" da coleta.
    IEnumerator Flash()
    {
        var go = NovoDisco(Color.white, 62);
        const float dur = 0.12f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            go.transform.localScale = Vector3.one * Mathf.Lerp(0.25f, 0.85f, Mathf.SmoothStep(0f, 1f, p));
            SetA(go, Mathf.Lerp(1f, 0f, p));
            yield return null;
        }
        Destroy(go);
    }

    IEnumerator Disco(Color cor)
    {
        var go = NovoDisco(cor, 60);
        const float dur = 0.26f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            go.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.3f, Mathf.SmoothStep(0f, 1f, p));
            SetA(go, Mathf.Lerp(1f, 0f, p));
            yield return null;
        }
        Destroy(go);
    }

    IEnumerator Faisca(Color cor, int i)
    {
        var go = NovoDisco(cor, 61);
        float ang = 60f * i + Random.Range(-25f, 25f);
        Vector2 vel = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad)) * Random.Range(2.5f, 4f);
        float tam = Random.Range(0.1f, 0.18f);
        go.transform.localScale = Vector3.one * tam;
        const float dur = 0.4f;
        float t = 0f;
        while (t < dur)
        {
            if (go == null) yield break;
            t += Time.deltaTime;
            float p = t / dur;
            vel *= 0.9f;
            go.transform.position += (Vector3)(vel * Time.deltaTime);
            go.transform.localScale = Vector3.one * Mathf.Lerp(tam, 0f, p);
            SetA(go, Mathf.Lerp(1f, 0f, p));
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    GameObject NovoDisco(Color cor, int sort)
    {
        var go = new GameObject("d");
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = Disco();
        sr.color        = cor;
        sr.sortingOrder = sort;
        return go;
    }
    static void SetA(GameObject go, float a)
    {
        if (go == null) return;
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null) { Color c = sr.color; c.a = a; sr.color = c; }
    }
    static Sprite s_disco;
    static Sprite Disco()
    {
        if (s_disco != null) return s_disco;
        const int sz = 16;
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
}
