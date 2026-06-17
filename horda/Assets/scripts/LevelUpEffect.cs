using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelUpEffect : MonoBehaviour
{
    [Header("Anel de luz")]
    public Color corAnel      = new Color(1f, 0.85f, 0.2f, 1f);
    public float raioFinal    = 3f;
    public float duracaoAnel  = 0.6f;

    [Header("Flash no sprite")]
    public Color corFlash     = new Color(1f, 0.9f, 0.3f, 1f);
    public int   qtdFlashes   = 3;
    public float duracaoFlash = 0.07f;

    [Header("Texto")]
    public float alturaTexto  = 1.8f;
    public float duracaoTexto = 1.2f;

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Executar(int novoLevel)
    {
        StartCoroutine(Anel());
        StartCoroutine(FlashSprite());
        StartCoroutine(TextoFlutuante(novoLevel));
    }

    // ── Anel dourado expandindo ──────────────────────────────────
    IEnumerator Anel()
    {
        int seg       = 40;
        GameObject go = new GameObject("LevelUpAnel");
        go.transform.position = transform.position;

        LineRenderer lr  = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop          = true;
        lr.positionCount = seg;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = sr != null ? sr.sortingOrder + 2 : 10;

        for (int i = 0; i < seg; i++)
        {
            float a = (360f / seg) * i * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f));
        }

        float t = 0f;
        while (t < duracaoAnel)
        {
            t += Time.deltaTime;
            float p    = Mathf.Clamp01(t / duracaoAnel);
            float raio = Mathf.Lerp(0.1f, raioFinal, Mathf.SmoothStep(0f, 1f, p));
            float alpha = Mathf.Lerp(1f, 0f, Mathf.Pow(p, 0.6f));

            go.transform.localScale = Vector3.one * raio;
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.22f, 0.02f, p);
            lr.startColor = lr.endColor = new Color(corAnel.r, corAnel.g, corAnel.b, alpha);
            yield return null;
        }

        Destroy(go);
    }

    // ── Sprite pisca dourado ─────────────────────────────────────
    IEnumerator FlashSprite()
    {
        if (sr == null) yield break;

        for (int i = 0; i < qtdFlashes; i++)
        {
            sr.color = corFlash;
            yield return new WaitForSeconds(duracaoFlash);
            sr.color = Color.white;
            yield return new WaitForSeconds(duracaoFlash);
        }
        sr.color = Color.white;
    }

    // ── "LEVEL UP!" sobe e some ──────────────────────────────────
    IEnumerator TextoFlutuante(int level)
    {
        // Canvas de overlay
        GameObject canvasGO   = new GameObject("LevelUpCanvas");
        Canvas canvas         = canvasGO.AddComponent<Canvas>();
        canvas.renderMode     = RenderMode.WorldSpace;
        canvas.sortingOrder   = 20;
        canvasGO.AddComponent<CanvasScaler>();

        // Texto
        GameObject txtGO      = new GameObject("Txt");
        txtGO.transform.SetParent(canvasGO.transform, false);

        RectTransform r       = txtGO.AddComponent<RectTransform>();
        r.sizeDelta           = new Vector2(4f, 1.5f);

        TextMeshProUGUI txt   = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text              = $"{Loc.T("ui.level_up")}\n{Loc.T("ui.level_abbr")} {level}";
        txt.fontSize          = 0.55f;
        txt.fontStyle         = FontStyles.Bold;
        txt.alignment         = TextAlignmentOptions.Center;
        txt.color             = new Color(1f, 0.9f, 0.2f, 1f);

        Vector3 posInicio = transform.position + Vector3.up * 0.5f;
        Vector3 posFim    = posInicio + Vector3.up * alturaTexto;

        float t = 0f;
        while (t < duracaoTexto)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duracaoTexto);

            canvasGO.transform.position = Vector3.Lerp(posInicio, posFim, Mathf.SmoothStep(0f, 1f, p));

            float alpha = p < 0.6f ? 1f : Mathf.Lerp(1f, 0f, (p - 0.6f) / 0.4f);
            txt.color = new Color(1f, 0.9f, 0.2f, alpha);

            yield return null;
        }

        Destroy(canvasGO);
    }
}
