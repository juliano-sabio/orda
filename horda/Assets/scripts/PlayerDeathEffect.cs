using System.Collections;
using UnityEngine;

public class PlayerDeathEffect : MonoBehaviour
{
    [Header("Fragmentos")]
    public int quantidadeFragmentos = 8;
    public float forcaFragmento = 4f;
    public float duracaoFragmento = 0.8f;

    [Header("Flash")]
    public float duracaoFlash = 0.1f;
    public int quantidadeFlashes = 3;
    public Color corFlash = Color.red;

    [Header("Escala")]
    public float duracaoEncolher = 0.5f;

    [Header("Marcador no Chão")]
    public Sprite spriteMarcador;
    public Color corMarcador = new Color(0.6f, 0f, 0f, 0.8f);
    public float tamanhoMarcador = 1.2f;
    public bool marcadorFade = false;
    public float duracaoFadeMarcador = 5f;

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Executar()
    {
        StartCoroutine(EfeiteMorte());
    }

    private IEnumerator EfeiteMorte()
    {
        // Flash vermelho
        for (int i = 0; i < quantidadeFlashes; i++)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = corFlash;
            yield return new WaitForSecondsRealtime(duracaoFlash);
            if (spriteRenderer != null)
                spriteRenderer.color = Color.white;
            yield return new WaitForSecondsRealtime(duracaoFlash);
        }

        // Marcador no chão
        SpawnarMarcador();

        // Fragmentos
        SpawnarFragmentos();

        // Encolher e sumir
        yield return StartCoroutine(EncolherESumir());
    }

    private void SpawnarMarcador()
    {
        Sprite sprite = spriteMarcador != null ? spriteMarcador : spriteRenderer?.sprite;
        if (sprite == null) return;

        GameObject marcador = new GameObject("MarcadorMorte");
        marcador.transform.position = transform.position;
        marcador.transform.localScale = Vector3.one * tamanhoMarcador;

        SpriteRenderer sr = marcador.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = corMarcador;
        sr.sortingLayerName = spriteRenderer != null ? spriteRenderer.sortingLayerName : "Default";
        sr.sortingOrder = -1;

        if (marcadorFade)
            StartCoroutine(FadeMarcador(sr, marcador));
    }

    private IEnumerator FadeMarcador(SpriteRenderer sr, GameObject obj)
    {
        yield return new WaitForSecondsRealtime(1f);

        float t = 0f;
        Color inicio = sr.color;

        while (t < duracaoFadeMarcador)
        {
            if (sr == null) yield break;
            t += Time.unscaledDeltaTime;
            sr.color = Color.Lerp(inicio, new Color(inicio.r, inicio.g, inicio.b, 0f), t / duracaoFadeMarcador);
            yield return null;
        }

        if (obj != null) Destroy(obj);
    }

    private void SpawnarFragmentos()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;

        for (int i = 0; i < quantidadeFragmentos; i++)
        {
            GameObject frag = new GameObject("Fragmento");
            frag.transform.position = transform.position;

            SpriteRenderer fragSR = frag.AddComponent<SpriteRenderer>();
            fragSR.sprite = spriteRenderer.sprite;
            fragSR.sortingLayerName = spriteRenderer.sortingLayerName;
            fragSR.sortingOrder = spriteRenderer.sortingOrder;
            fragSR.color = spriteRenderer.color;
            frag.transform.localScale = transform.localScale * 0.4f;

            Rigidbody2D fragRb = frag.AddComponent<Rigidbody2D>();
            fragRb.gravityScale = 0f;

            float angulo = (360f / quantidadeFragmentos) * i;
            Vector2 direcao = new Vector2(Mathf.Cos(angulo * Mathf.Deg2Rad), Mathf.Sin(angulo * Mathf.Deg2Rad));
            fragRb.AddForce(direcao * forcaFragmento, ForceMode2D.Impulse);

            StartCoroutine(FadeFrag(fragSR, frag));
        }
    }

    private IEnumerator FadeFrag(SpriteRenderer sr, GameObject obj)
    {
        float t = 0f;
        Color inicio = sr.color;

        while (t < duracaoFragmento)
        {
            if (sr == null) yield break;
            t += Time.unscaledDeltaTime;
            sr.color = Color.Lerp(inicio, new Color(inicio.r, inicio.g, inicio.b, 0f), t / duracaoFragmento);
            yield return null;
        }

        if (obj != null) Destroy(obj);
    }

    private IEnumerator EncolherESumir()
    {
        Vector3 escalaInicial = transform.localScale;
        float t = 0f;

        while (t < duracaoEncolher)
        {
            t += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(escalaInicial, Vector3.zero, t / duracaoEncolher);
            if (spriteRenderer != null)
                spriteRenderer.color = Color.Lerp(Color.white, new Color(1, 1, 1, 0), t / duracaoEncolher);
            yield return null;
        }
    }
}
