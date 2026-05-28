using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PulsarLuzItem : MonoBehaviour
{
    public float intensidadeMin = 0.6f;
    public float intensidadeMax = 2.8f;
    public float velocidade     = 2.5f;

    Light2D luz;
    Animator anim;
    float fase;

    void Awake()
    {
        luz  = GetComponent<Light2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (luz == null) return;

        // Sincroniza com o tempo normalizado da animação quando possível
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            var info = anim.GetCurrentAnimatorStateInfo(0);
            fase = info.normalizedTime % 1f;
        }
        else
        {
            fase = Mathf.Repeat(Time.time * velocidade, 1f);
        }

        float t = Mathf.Sin(fase * Mathf.PI * 2f) * 0.5f + 0.5f;
        luz.intensity = Mathf.Lerp(intensidadeMin, intensidadeMax, t);
    }
}
