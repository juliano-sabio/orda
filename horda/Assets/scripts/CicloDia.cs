using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CicloDia : MonoBehaviour
{
    [Header("Cores da Luz Global")]
    public Color corMeiodia      = new Color(1.00f, 0.98f, 0.90f);
    public Color corTardezinha   = new Color(1.00f, 0.62f, 0.28f);

    [Header("Intensidade")]
    public float intensidadeMeiodia    = 1.2f;
    public float intensidadeTardezinha = 0.65f;

    [Header("Duração da Transição (segundos)")]
    public float duracaoTransicao = 180f;

    [Header("Cor de Fundo da Câmera")]
    public Color fundoMeiodia    = new Color(0.40f, 0.65f, 0.90f);
    public Color fundoTardezinha = new Color(0.70f, 0.35f, 0.15f);

    private Light2D luzGlobal;
    private TimerManager timerManager;
    private float tempoDecorrido;

    void Start()
    {
        timerManager = FindAnyObjectByType<TimerManager>();

        // Procura Global Light 2D na cena
        foreach (var luz in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
        {
            if (luz.lightType == Light2D.LightType.Global)
            {
                luzGlobal = luz;
                break;
            }
        }

        if (luzGlobal == null)
        {
            GameObject go = new GameObject("GlobalLight2D");
            luzGlobal = go.AddComponent<Light2D>();
            luzGlobal.lightType = Light2D.LightType.Global;
        }

        luzGlobal.color     = corMeiodia;
        luzGlobal.intensity = intensidadeMeiodia;

        if (Camera.main != null)
            Camera.main.backgroundColor = fundoMeiodia;
    }

    void Update()
    {
        float duracao = duracaoTransicao;
        if (timerManager != null)
            duracao = timerManager.levelDuration;

        tempoDecorrido += Time.deltaTime;
        float t = Mathf.Clamp01(tempoDecorrido / duracao);
        float ease = Mathf.SmoothStep(0f, 1f, t);

        if (luzGlobal != null)
        {
            luzGlobal.color     = Color.Lerp(corMeiodia, corTardezinha, ease);
            luzGlobal.intensity = Mathf.Lerp(intensidadeMeiodia, intensidadeTardezinha, ease);
        }

        if (Camera.main != null)
            Camera.main.backgroundColor = Color.Lerp(fundoMeiodia, fundoTardezinha, ease);
    }
}
