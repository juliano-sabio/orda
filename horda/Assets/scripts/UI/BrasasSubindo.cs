using UnityEngine;
using UnityEngine.UI;

// Brasas/faíscas que sobem continuamente dentro do RectTransform onde está anexado.
// Usado como fundo decorativo do painel de configurações (estética de forja).
public class BrasasSubindo : MonoBehaviour
{
    public int   quantidade = 28;
    public Color cor        = new Color(1f, 0.55f, 0.15f);

    RectTransform[] rt;
    Image[]         img;
    float[]         baseX, velY, faseX, ampX, faseFlk;

    void Start()
    {
        rt      = new RectTransform[quantidade];
        img     = new Image[quantidade];
        baseX   = new float[quantidade];
        velY    = new float[quantidade];
        faseX   = new float[quantidade];
        ampX    = new float[quantidade];
        faseFlk = new float[quantidade];

        for (int i = 0; i < quantidade; i++)
            CriarBrasa(i, Random.value);   // distribui ao longo de toda a altura no início
    }

    void CriarBrasa(int i, float startY)
    {
        var go = new GameObject("Brasa" + i);
        go.transform.SetParent(transform, false);

        var r = go.AddComponent<RectTransform>();
        r.pivot = new Vector2(0.5f, 0.5f);
        float sz = Random.Range(2.5f, 6.5f);
        r.sizeDelta = new Vector2(sz, sz);

        var im = go.AddComponent<Image>();
        im.raycastTarget = false;
        im.color = cor;

        rt[i]      = r;
        img[i]     = im;
        baseX[i]   = Random.value;
        velY[i]    = Random.Range(0.05f, 0.15f);          // fração da altura por segundo
        faseX[i]   = Random.Range(0f, Mathf.PI * 2f);
        ampX[i]    = Random.Range(0.008f, 0.04f);
        faseFlk[i] = Random.Range(0f, Mathf.PI * 2f);

        Posicionar(i, startY, 0f);
    }

    void Posicionar(int i, float y, float t)
    {
        float x = Mathf.Clamp01(baseX[i] + Mathf.Sin(t * 0.9f + faseX[i]) * ampX[i]);
        rt[i].anchorMin = rt[i].anchorMax = new Vector2(x, y);
    }

    void Update()
    {
        if (rt == null) return;
        float t  = Time.unscaledTime;
        float dt = Time.unscaledDeltaTime;

        for (int i = 0; i < quantidade; i++)
        {
            if (rt[i] == null) continue;

            float y = rt[i].anchorMin.y + velY[i] * dt;
            if (y > 1.05f)                       // recicla pela base
            {
                y = -0.05f;
                baseX[i]   = Random.value;
                velY[i]    = Random.Range(0.05f, 0.15f);
                ampX[i]    = Random.Range(0.008f, 0.04f);
                faseFlk[i] = Random.Range(0f, Mathf.PI * 2f);
            }
            Posicionar(i, y, t);

            // brilho que pisca + esmaece nas extremidades (some perto do topo e da base)
            float flicker = 0.45f + 0.45f * Mathf.Abs(Mathf.Sin(t * 5f + faseFlk[i]));
            float fade    = Mathf.Clamp01(Mathf.Min(y, 1f - y) * 5f);
            var c = img[i].color;
            img[i].color = new Color(c.r, c.g, c.b, flicker * fade * 0.7f);
        }
    }
}
