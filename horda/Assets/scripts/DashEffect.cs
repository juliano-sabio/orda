using System.Collections;
using UnityEngine;

public class DashEffect : MonoBehaviour
{
    [Header("Afterimage")]
    public float intervalo = 0.03f;
    public float duracaoFade = 0.2f;
    public Color corInicial = new Color(0.4f, 0.7f, 1f, 0.8f);

    private SpriteRenderer spriteRenderer;
    private Coroutine spawnarCoroutine;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void IniciarEfeito()
    {
        if (spawnarCoroutine != null)
            StopCoroutine(spawnarCoroutine);
        spawnarCoroutine = StartCoroutine(SpawnarAfterimages());
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
