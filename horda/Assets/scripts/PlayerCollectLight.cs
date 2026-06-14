using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class PlayerCollectLight : MonoBehaviour
{
    [Header("Luz")]
    public float intensidadeAtiva = 3f;
    public float raioAtivo = 5f;
    public Color corLuz = new Color(1f, 0.9f, 0.4f);
    public float fadeDuration = 0.3f;

    private Light2D luz;
    private Coroutine ativaCoroutine;

    void Awake()
    {
        luz = GetComponent<Light2D>();
        luz.lightType = Light2D.LightType.Point;
        luz.pointLightOuterRadius = raioAtivo;
        luz.color = corLuz;
        luz.intensity = 0f;
        luz.enabled = true;
    }

    public void AtualizarPorPercentual(float pct)
    {
        pct = Mathf.Clamp01(pct);
        if (ativaCoroutine != null) { StopCoroutine(ativaCoroutine); ativaCoroutine = null; }

        luz.color = corLuz;
        luz.intensity = intensidadeAtiva * pct;
        luz.pointLightOuterRadius = Mathf.Lerp(0.5f, raioAtivo, pct);
    }

    public void Ativar(float duracao)
    {
        if (ativaCoroutine != null)
            StopCoroutine(ativaCoroutine);
        ativaCoroutine = StartCoroutine(LuzCoroutine(duracao));
    }

    private IEnumerator LuzCoroutine(float duracao)
    {
        luz.color = corLuz;
        luz.pointLightOuterRadius = raioAtivo;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            luz.intensity = Mathf.Lerp(0f, intensidadeAtiva, t / fadeDuration);
            yield return null;
        }

        luz.intensity = intensidadeAtiva;

        float espera = duracao - fadeDuration * 2f;
        if (espera > 0f)
            yield return new WaitForSeconds(espera);

        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            luz.intensity = Mathf.Lerp(intensidadeAtiva, 0f, t / fadeDuration);
            yield return null;
        }

        luz.intensity = 0f;
    }
}
