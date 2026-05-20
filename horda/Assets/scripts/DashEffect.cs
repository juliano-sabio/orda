using System.Collections;
using UnityEngine;

public class DashEffect : MonoBehaviour
{
    [Header("Afterimage")]
    public float intervalo = 0.03f;
    public float duracaoFade = 0.2f;
    public Color corInicial = new Color(0.4f, 0.7f, 1f, 0.8f);

    [Header("Partículas")]
    public int quantidadeParticulas = 14;
    public float velocidadeParticula = 2.5f;
    public float tempoVidaParticula = 0.4f;
    public Color corParticula = new Color(0.4f, 0.8f, 1f, 1f);

    private SpriteRenderer spriteRenderer;
    private Coroutine spawnarCoroutine;
    private Sprite spriteParticula;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteParticula = CriarSpriteCirculo();
    }

    private Sprite CriarSpriteCirculo()
    {
        int sz = 32;
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        Vector2 centro = new Vector2(sz / 2f, sz / 2f);
        float raio = sz / 2f;
        Color[] pixels = new Color[sz * sz];

        for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), centro);
                float alpha = Mathf.Clamp01(1f - dist / raio);
                alpha = Mathf.Pow(alpha, 1.5f);
                pixels[y * sz + x] = new Color(1f, 1f, 1f, alpha);
            }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }

    private void EmitirParticulas()
    {
        for (int i = 0; i < quantidadeParticulas; i++)
        {
            float angulo = (360f / quantidadeParticulas) * i + Random.Range(-10f, 10f);
            Vector2 direcao = new Vector2(
                Mathf.Cos(angulo * Mathf.Deg2Rad),
                Mathf.Sin(angulo * Mathf.Deg2Rad)
            );
            float velocidade = velocidadeParticula * Random.Range(0.5f, 1f);
            float tamanho = Random.Range(0.06f, 0.16f);
            StartCoroutine(AnimarParticula(transform.position, direcao, velocidade, tamanho));
        }
    }

    private IEnumerator AnimarParticula(Vector3 origem, Vector2 direcao, float velocidade, float tamanho)
    {
        GameObject go = new GameObject("DashParticula");
        go.transform.position = origem;
        go.transform.localScale = Vector3.one * tamanho;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spriteParticula;
        sr.color = corParticula;
        sr.sortingLayerName = spriteRenderer != null ? spriteRenderer.sortingLayerName : "Default";
        sr.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder + 1 : 1;

        float t = 0f;
        while (t < tempoVidaParticula)
        {
            if (go == null) yield break;
            t += Time.deltaTime;
            float progresso = t / tempoVidaParticula;

            go.transform.position += (Vector3)(direcao * velocidade * Time.deltaTime);
            velocidade = Mathf.Lerp(velocidade, 0f, Time.deltaTime * 4f);

            float alpha = Mathf.Lerp(1f, 0f, progresso);
            float scale = Mathf.Lerp(tamanho, tamanho * 0.3f, progresso);
            sr.color = new Color(corParticula.r, corParticula.g, corParticula.b, alpha);
            go.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        if (go != null) Destroy(go);
    }

    public void IniciarEfeito()
    {
        if (spawnarCoroutine != null)
            StopCoroutine(spawnarCoroutine);
        spawnarCoroutine = StartCoroutine(SpawnarAfterimages());
        EmitirParticulas();
    }

    public void PararEfeito()
    {
        if (spawnarCoroutine != null)
        {
            StopCoroutine(spawnarCoroutine);
            spawnarCoroutine = null;
        }
    }

    private IEnumerator SpawnarAfterimages()
    {
        while (true)
        {
            SpawnarGhost();
            yield return new WaitForSeconds(intervalo);
        }
    }

    private void SpawnarGhost()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;

        GameObject ghost = new GameObject("DashGhost");
        ghost.transform.position = transform.position;
        ghost.transform.rotation = transform.rotation;
        ghost.transform.localScale = transform.localScale;

        SpriteRenderer ghostSR = ghost.AddComponent<SpriteRenderer>();
        ghostSR.sprite = spriteRenderer.sprite;
        ghostSR.sortingLayerName = spriteRenderer.sortingLayerName;
        ghostSR.sortingOrder = spriteRenderer.sortingOrder - 1;
        ghostSR.color = corInicial;

        StartCoroutine(FadeGhost(ghostSR));
    }

    private IEnumerator FadeGhost(SpriteRenderer ghostSR)
    {
        float t = 0f;
        Color inicio = ghostSR.color;
        Color fim = new Color(inicio.r, inicio.g, inicio.b, 0f);

        while (t < duracaoFade)
        {
            if (ghostSR == null) yield break;
            t += Time.deltaTime;
            ghostSR.color = Color.Lerp(inicio, fim, t / duracaoFade);
            yield return null;
        }

        if (ghostSR != null)
            Destroy(ghostSR.gameObject);
    }
}
