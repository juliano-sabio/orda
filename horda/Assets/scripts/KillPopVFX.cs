using System.Collections;
using UnityEngine;

// Kill juice: pop satisfatório na morte de inimigo normal (squash + estilhaços + anel + flash).
// Local e auto-destrutível; em co-op é tocado nos dois lados via EnemyNet.BroadcastMorteVFX.
public class KillPopVFX : MonoBehaviour
{
    // ── Tuning (dialar "explosivo vs limpo") ──
    const int   NUM_CACOS   = 5;
    const float DUR_TOTAL   = 0.42f;
    const float DUR_SQUASH  = 0.12f;
    const float DUR_ANEL    = 0.25f;
    const float DUR_FLASH   = 0.08f;
    const float RAIO_ANEL   = 0.9f;
    const float MULT_ESCALA = 0.34f; // ~1/3 (reduzido 2/3 a pedido — estava grande demais)
    const float VEL_CACO    = 3.5f;
    const int   SORT_BASE   = 50;

    public static void Tocar(Vector3 pos, Color cor, float escala)
    {
        var root = new GameObject("KillPop");
        root.transform.position = pos;
        root.AddComponent<KillPopVFX>().Iniciar(cor, Mathf.Max(0.1f, escala) * MULT_ESCALA);
    }

    void Iniciar(Color cor, float escala)
    {
        StartCoroutine(Squash(cor, escala));
        StartCoroutine(Flash(escala));
        StartCoroutine(Anel(cor, escala));
        for (int i = 0; i < NUM_CACOS; i++) StartCoroutine(Caco(cor, escala, i));
        Destroy(gameObject, DUR_TOTAL + 0.1f);
    }

    // 1. Squash-pop: blob na cor, esmaga (largo+baixo) → estoura + fade
    IEnumerator Squash(Color cor, float escala)
    {
        var go = NovoDisco(cor, SORT_BASE + 1);
        float t = 0f;
        while (t < DUR_SQUASH)
        {
            t += Time.deltaTime;
            float p = t / DUR_SQUASH;
            float sx = Mathf.Lerp(1.4f, 1.6f, p) * escala;
            float sy = Mathf.Lerp(0.5f, 1.6f, p) * escala;
            go.transform.localScale = new Vector3(sx, sy, 1f);
            SetAlpha(go, Mathf.Lerp(0.95f, 0f, p));
            yield return null;
        }
        Destroy(go);
    }

    // 4. Flash branco no 1º frame
    IEnumerator Flash(float escala)
    {
        var go = NovoDisco(Color.white, SORT_BASE + 3);
        go.transform.localScale = Vector3.one * escala * 1.1f;
        float t = 0f;
        while (t < DUR_FLASH)
        {
            t += Time.deltaTime;
            SetAlpha(go, Mathf.Lerp(0.9f, 0f, t / DUR_FLASH));
            yield return null;
        }
        Destroy(go);
    }

    // 3. Anel expandindo
    IEnumerator Anel(Color cor, float escala)
    {
        var go = new GameObject("Anel");
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        const int SEG = 28;
        lr.useWorldSpace = false; lr.loop = true; lr.positionCount = SEG;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = SORT_BASE + 2;
        float t = 0f;
        while (t < DUR_ANEL)
        {
            t += Time.deltaTime;
            float p = t / DUR_ANEL;
            float raio = Mathf.Lerp(0.1f, RAIO_ANEL * escala, p);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.12f * escala, 0.01f, p);
            Color c = cor; c.a = Mathf.Lerp(0.8f, 0f, p);
            lr.startColor = lr.endColor = c;
            for (int i = 0; i < SEG; i++)
            {
                float a = (360f / SEG) * i * Mathf.Deg2Rad;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * raio, Mathf.Sin(a) * raio, 0f));
            }
            yield return null;
        }
        Destroy(go);
    }

    // 2. Estilhaço: caco voando pra fora + caindo + sumindo
    IEnumerator Caco(Color cor, float escala, int idx)
    {
        var go = NovoDisco(cor, SORT_BASE + 2);
        float ang = (360f / NUM_CACOS) * idx + Random.Range(-25f, 25f);
        Vector2 dir = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));
        Vector2 vel = dir * VEL_CACO * Random.Range(0.7f, 1.3f) * escala;
        float tam = Random.Range(0.1f, 0.18f) * escala;
        go.transform.localScale = Vector3.one * tam;
        float dur = DUR_TOTAL * Random.Range(0.7f, 1f);
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            vel.y -= 6f * Time.deltaTime; // gravidade leve
            vel *= 0.92f;                 // arrasto
            go.transform.localPosition += (Vector3)(vel * Time.deltaTime);
            go.transform.localScale = Vector3.one * Mathf.Lerp(tam, tam * 0.3f, p);
            SetAlpha(go, Mathf.Lerp(0.9f, 0f, p * p));
            yield return null;
        }
        Destroy(go);
    }

    // ── helpers ──
    GameObject NovoDisco(Color cor, int sort)
    {
        var go = new GameObject("d");
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Disco();
        sr.color = cor;
        sr.sortingOrder = sort;
        return go;
    }
    static void SetAlpha(GameObject go, float a)
    {
        if (go == null) return;
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null) { Color c = sr.color; c.a = a; sr.color = c; }
    }
    static Sprite s_disco;
    static Sprite Disco()
    {
        if (s_disco != null) return s_disco;
        const int sz = 32;
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
