using UnityEngine;
using System.Collections;

public class VinhasController : MonoBehaviour
{
    [Header("Configurações")]
    public float duracao = 2f;
    public bool fadeOut = true;
    public float tempoFadeOut = 0.5f;

    private float tempoInicio;
    private SpriteRenderer spriteRenderer;
    private ParticleSystem particleSystem;

    void Start()
    {
        tempoInicio = Time.time;
        spriteRenderer = GetComponent<SpriteRenderer>();
        particleSystem = GetComponent<ParticleSystem>();

        // Inicia partículas se existirem
        if (particleSystem != null)
        {
            particleSystem.Play();
            Debug.Log("🎆 Partículas iniciadas");
        }

        Debug.Log($"🌱 Vinhas criadas - duração: {duracao}s");
    }

    void Update()
    {
        // Verifica se já passou o tempo de duração
        if (Time.time >= tempoInicio + duracao)
        {
            if (fadeOut && spriteRenderer != null)
            {
                StartCoroutine(FadeOutEDestruir());
                fadeOut = false; // Evita múltiplas chamadas
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    IEnumerator FadeOutEDestruir()
    {
        float tempo = 0;
        Color corOriginal = spriteRenderer.color;

        while (tempo < tempoFadeOut)
        {
            tempo += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, tempo / tempoFadeOut);
            spriteRenderer.color = new Color(corOriginal.r, corOriginal.g, corOriginal.b, alpha);
            yield return null;
        }

        Destroy(gameObject);
    }

    public void Iniciar(float novaDuracao)
    {
        duracao = novaDuracao;
        tempoInicio = Time.time;
        Debug.Log($"🌱 Vinhas reiniciadas - nova duração: {duracao}s");
    }
}