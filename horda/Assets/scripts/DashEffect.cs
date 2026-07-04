using System.Collections;
using UnityEngine;

public class DashEffect : MonoBehaviour
{
    [Header("Afterimage")]
    public float intervalo = 0.05f;
    public float duracaoFade = 0.14f;
    public Color corInicial = new Color(0.4f, 0.7f, 1f, 0.4f);

    [Header("Partículas")]
    public int quantidadeParticulas = 6;
    public float velocidadeParticula = 1.8f;
    public float tempoVidaParticula = 0.25f;
    public Color corParticula = new Color(0.4f, 0.8f, 1f, 0.7f);

    [Header("Sprite Dash")]
    public Color corDash = new Color(0.5f, 0.85f, 1f, 0.5f);
    public float duracaoDashSprite = 0.22f;

    private SpriteRenderer spriteRenderer;
    private Coroutine spawnarCoroutine;
    private Sprite spriteParticula;
    private Sprite spriteDash;

    void Awake()
    {
        spriteRenderer  = GetComponent<SpriteRenderer>();
        spriteParticula = CriarSpriteCirculo();
        spriteDash      = CriarSpriteDash();
    }

    private Sprite CriarSpriteDash()
    {
        int w = 128, h = 40;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode   = TextureWrapMode.Clamp;

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            // nx: 0=ponta traseira, 1=frente brilhante
            float nx = x / (float)(w - 1);
            // ny: 0=centro, 1=borda
            float ny = Mathf.Abs((y - h * 0.5f) / (h * 0.5f));

            // A largura do rastro cresce da ponta até a frente
            float halfW = Mathf.Lerp(0.05f, 1f, Mathf.Pow(nx, 0.6f));
            float inShape = ny < halfW ? 1f : 0f;

            float distRel = ny / Mathf.Max(halfW, 0.001f);
            float alpha = inShape
                * Mathf.Pow(1f - distRel * distRel, 1.5f)  // suaviza borda
                * Mathf.Pow(nx, 0.4f);                      // brilha mais na frente

            tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
        }

        tex.Apply();
        // Pivot na frente (x=1) para o sprite apontar na direção do dash
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(1f, 0.5f), 32f);
    }

    private void SpawnarSpriteDash(Vector2 direcao)
    {
        if (spriteDash == null) return;

        var go = new GameObject("DashSprite");
        go.transform.position = transform.position;

        float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angulo);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spriteDash;
        sr.color  = corDash;
        if (spriteRenderer != null)
        {
            sr.sortingLayerName = spriteRenderer.sortingLayerName;
            sr.sortingOrder     = spriteRenderer.sortingOrder - 1;
        }

        StartCoroutine(FadeDashSprite(sr));
    }

    private IEnumerator FadeDashSprite(SpriteRenderer sr)
    {
        if (sr == null) yield break;
        Color inicio = sr.color;
        for (float t = 0f; t < duracaoDashSprite; t += Time.deltaTime)
        {
            if (sr == null) yield break;
            float p = t / duracaoDashSprite;
            // Escala estica no início e encolhe no fim
            float escala = Mathf.Lerp(1f, 1.4f, p);
            sr.transform.localScale = new Vector3(escala, 1f - p * 0.3f, 1f);
            sr.color = new Color(inicio.r, inicio.g, inicio.b, Mathf.Lerp(inicio.a, 0f, p * p));
            yield return null;
        }
        if (sr != null) Destroy(sr.gameObject);
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

    public void IniciarEfeito() => IniciarEfeito(Vector2.right);

    public void IniciarEfeito(Vector2 direcao)
    {
        if (spawnarCoroutine != null)
            StopCoroutine(spawnarCoroutine);
        spawnarCoroutine = StartCoroutine(SpawnarAfterimages());
        EmitirParticulas();
        SpawnarSpriteDash(direcao);
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
