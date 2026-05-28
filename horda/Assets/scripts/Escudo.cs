using System.Collections;
using UnityEngine;

public class Escudo : MonoBehaviour
{
    public float vidaMaxima = 200f;
    public float vidaAtual  { get; private set; }
    public bool  Ativo      => vidaAtual > 0 && !quebrando;

    private SpriteRenderer srVisual;
    private GameObject     visualGO;
    private bool           quebrando;
    private float          baseLocalScale;

    public void Ativar(float vida)
    {
        vidaAtual  = vida;
        vidaMaxima = vida;
        quebrando  = false;
        CriarVisual();
        StartCoroutine(PulsarEscudo());
    }

    public void Desativar()
    {
        if (!Ativo) return;
        StartCoroutine(QuebrarEscudo());
    }

    public void ForcarRemover()
    {
        StopAllCoroutines();
        vidaAtual = 0;
        if (visualGO != null) { Destroy(visualGO); visualGO = null; }
        Destroy(this);
    }

    void OnDestroy()
    {
        if (visualGO != null) Destroy(visualGO);
    }

    // Retorna o dano que passou pelo escudo (overflow)
    public float AbsorverDano(float dano)
    {
        if (!Ativo) return dano;

        float absorcao = Mathf.Min(dano, vidaAtual);
        vidaAtual -= absorcao;

        if (vidaAtual <= 0)
            StartCoroutine(QuebrarEscudo());
        else
            StartCoroutine(FlashDano());

        return dano - absorcao;
    }

    // ──────────────────────────────────────────────────────────────
    void CriarVisual()
    {
        int sz = 64;
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float c = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d  = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c));
            float nt = d / c;
            float a  = Mathf.Clamp01(1f - Mathf.Abs(nt - 0.82f) / 0.18f);
            a *= Mathf.Clamp01((nt - 0.5f) / 0.2f);
            tex.SetPixel(x, y, new Color(0.75f, 0.5f, 1f, a * 0.55f));
        }
        tex.Apply();
        Sprite spr = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), (float)sz);

        visualGO = new GameObject("EscudoVisual");
        visualGO.transform.SetParent(transform);

        SpriteRenderer bossSR = GetComponent<SpriteRenderer>();

        // Centraliza no meio do sprite (compensa pivot na base)
        if (bossSR != null)
            visualGO.transform.position = bossSR.bounds.center;
        else
            visualGO.transform.localPosition = Vector3.zero;

        // Compensa escala do boss para o escudo ter ~7 world units de diâmetro
        float avgParent = (Mathf.Abs(transform.localScale.x) + Mathf.Abs(transform.localScale.y)) * 0.5f;
        baseLocalScale  = avgParent > 0.001f ? 7f / avgParent : 0.7f;
        visualGO.transform.localScale = Vector3.one * baseLocalScale;

        srVisual = visualGO.AddComponent<SpriteRenderer>();
        srVisual.sprite = spr;

        if (bossSR != null)
        {
            srVisual.sortingLayerID = bossSR.sortingLayerID;
            srVisual.sortingOrder   = bossSR.sortingOrder + 2;
        }
    }

    IEnumerator PulsarEscudo()
    {
        while (Ativo && srVisual != null)
        {
            float t     = Time.time;
            float alpha = Mathf.Lerp(0.08f, 0.22f, (Mathf.Sin(t * 1.8f) + 1f) * 0.5f);
            float scl   = Mathf.Lerp(0.98f, 1.02f, (Mathf.Sin(t * 1.3f) + 1f) * 0.5f);

            srVisual.color = new Color(0.75f, 0.5f, 1f, alpha);
            if (visualGO != null)
                visualGO.transform.localScale = Vector3.one * (baseLocalScale * scl);

            yield return null;
        }
    }

    IEnumerator FlashDano()
    {
        if (srVisual == null) yield break;
        srVisual.color = Color.white;
        yield return new WaitForSeconds(0.1f);
    }

    IEnumerator QuebrarEscudo()
    {
        quebrando = true;
        if (visualGO == null) yield break;

        Vector3 escBase = visualGO.transform.localScale;
        float t = 0f;
        while (t < 0.45f)
        {
            t += Time.deltaTime;
            float p = t / 0.45f;
            if (visualGO != null)
                visualGO.transform.localScale = escBase * Mathf.Lerp(1f, 2.2f, Mathf.Pow(p, 0.5f));
            if (srVisual != null)
                srVisual.color = new Color(0.85f, 0.6f, 1f, Mathf.Lerp(0.9f, 0f, p));
            yield return null;
        }

        vidaAtual = 0;
        if (visualGO != null) Destroy(visualGO);
    }
}
