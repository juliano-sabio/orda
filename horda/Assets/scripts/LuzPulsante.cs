using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LuzPulsante : MonoBehaviour
{
    [Header("Pulsação")]
    public float velocidadePulso = 2f;
    public float minIntens       = 0.5f;
    public float maxIntens       = 1.3f;
    public float variacaoRaio    = 0.1f;

    Light2D luz;
    float   raioBase;
    float   intensBase;
    Vector3 offsetLocal; // offset salvo na Start para manter centralização

    void Start()
    {
        luz = GetComponent<Light2D>();
        if (luz == null) return;
        intensBase  = luz.intensity;
        raioBase    = luz.pointLightOuterRadius;
        minIntens   = intensBase * 0.6f;
        maxIntens   = intensBase * 1.35f;
        offsetLocal = transform.localPosition; // preserva o offset do prefab
    }

    void LateUpdate()
    {
        if (luz == null) return;

        // Mantém o offset original (não reseta para zero)
        transform.localPosition = offsetLocal;

        float pulso = Mathf.Sin(Time.time * velocidadePulso) * 0.5f + 0.5f;
        luz.intensity             = Mathf.Lerp(minIntens, maxIntens, pulso);
        luz.pointLightOuterRadius = raioBase + Mathf.Sin(Time.time * velocidadePulso * 1.3f) * variacaoRaio;
    }
}
