using System.Collections;
using UnityEngine;

public class GeloVisualInimigo : MonoBehaviour
{
    static readonly Color COR_GELO = new Color(0.45f, 0.88f, 1f);
    static readonly Color COR_ICE  = new Color(0.82f, 0.97f, 1f);

    float durRestante;
    float elapsed;
    GameObject anelGO;

    public static void Aplicar(InimigoController ic, float duracao)
    {
        if (ic == null || ic.estaMorrendo) return;
        var existing = ic.GetComponent<GeloVisualInimigo>();
        if (existing != null) { existing.durRestante = duracao; return; }
        ic.gameObject.AddComponent<GeloVisualInimigo>().Iniciar(duracao);
    }

    void Iniciar(float dur)
    {
        durRestante = dur;
        StartCoroutine(Run());
    }

    void OnDestroy()
    {
        if (anelGO != null) Destroy(anelGO);
    }

    IEnumerator Run()
    {
        anelGO = CriarAnel();

        while (durRestante > 0f)
        {
            durRestante -= Time.deltaTime;
            elapsed     += Time.deltaTime;

            if (anelGO != null) AtualizarAnel();

            if (Time.frameCount % 5 == 0)
                SpawnParticula(transform.position);

            yield return null;
        }

        if (anelGO != null) Destroy(anelGO);
        Destroy(this);
    }

    void AtualizarAnel()
    {
        var tOuter = anelGO.transform.Find("Outer");
        var tRing  = anelGO.transform.Find("Ring");
        var lrO = tOuter != null ? tOuter.GetComponent<LineRenderer>() : null;
        var lrR = tRing  != null ? tRing .GetComponent<LineRenderer>() : null;

        const int S = 24;
        float rotA =  elapsed * 32f * Mathf.Deg2Rad;
        float rotB = -elapsed * 48f * Mathf.Deg2Rad;
        float rO   = 0.55f + Mathf.Sin(elapsed * 6f)        * 0.05f;
        float rR   = 0.38f + Mathf.Sin(elapsed * 9f + 1.1f) * 0.04f;
        float pulse = Mathf.Sin(elapsed * 8f) * 0.12f + 0.88f;
        Vector2 center = transform.position;

        for (int i = 0; i < S; i++)
        {
            float aO = (Mathf.PI * 2f / S) * i + rotA;
            float aR = (Mathf.PI * 2f / S) * i + rotB;
            if (lrO != null) lrO.SetPosition(i, center + new Vector2(Mathf.Cos(aO), Mathf.Sin(aO)) * rO);
            if (lrR != null) lrR.SetPosition(i, center + new Vector2(Mathf.Cos(aR), Mathf.Sin(aR)) * rR);
        }

        if (lrO != null)
        {
            Color c = COR_GELO; c.a = 0.28f * pulse;
            lrO.startColor = lrO.endColor = c;
            lrO.startWidth = lrO.endWidth = 0.15f * pulse;
        }
        if (lrR != null)
        {
            Color c = COR_ICE; c.a = 0.80f * pulse;
            lrR.startColor = lrR.endColor = c;
            lrR.startWidth = lrR.endWidth = 0.055f * pulse;
        }
    }

    GameObject CriarAnel()
    {
        var root = new GameObject("GeloAura");
        root.transform.position = transform.position;

        const int S = 24;

        var lrO = CriarLR(root, "Outer", S, 11,
            new Color(COR_GELO.r, COR_GELO.g, COR_GELO.b, 0.28f),
            new Color(COR_ICE.r,  COR_ICE.g,  COR_ICE.b,  0.10f),
            0.15f, 0.08f);
        lrO.loop = true;

        var lrR = CriarLR(root, "Ring", S, 12,
            new Color(COR_ICE.r,  COR_ICE.g,  COR_ICE.b,  0.80f),
            new Color(COR_GELO.r, COR_GELO.g, COR_GELO.b, 0.55f),
            0.055f, 0.035f);
        lrR.loop = true;

        return root;
    }

    static void SpawnParticula(Vector2 pos)
    {
        float ang  = Random.value * Mathf.PI * 2f;
        float dist = Random.Range(0.15f, 0.5f);
        Vector2 spawn = pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;

        var go = new GameObject("GeloParticula");
        go.transform.position = spawn;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = GerarDisco(6);
        sr.color        = new Color(COR_ICE.r, COR_ICE.g, COR_ICE.b, 0.85f);
        sr.sortingOrder = 13;
        go.transform.localScale = Vector3.one * Random.Range(0.06f, 0.15f);
        go.AddComponent<AutoDestroyFadeMove>().Iniciar(
            new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0.5f, 1.5f)),
            Random.Range(0.3f, 0.55f));
    }

    static LineRenderer CriarLR(GameObject parent, string nome, int segs, int order,
        Color cStart, Color cEnd, float wStart, float wEnd)
    {
        var child = new GameObject(nome);
        child.transform.SetParent(parent.transform, false);
        var lr = child.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = segs;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = order;
        lr.startColor    = cStart;
        lr.endColor      = cEnd;
        lr.startWidth    = wStart;
        lr.endWidth      = wEnd;
        return lr;
    }

    static Sprite GerarDisco(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        float c = sz * 0.5f;
        for (int x = 0; x < sz; x++)
            for (int y = 0; y < sz; y++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d / c)));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, sz);
    }
}
