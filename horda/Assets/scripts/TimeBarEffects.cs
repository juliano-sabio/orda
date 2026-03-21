using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimeBarEffects : MonoBehaviour
{
    public TimerManager timerManager;
    public Image timeBarFill;

    [Header("TextMesh Pro UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI statusText;

    public GameObject eventIndicatorPrefab;
    public Transform eventsContainer;

    [Header("Efeitos Visuais")]
    public ParticleSystem criticalTimeParticles;
    public AnimationCurve pulseCurve;
    public float pulseSpeed = 2f;

    private void Start()
    {
        TimerManager.OnEventTriggered += OnEventTriggered;
        TimerManager.OnBossSpawn += OnBossSpawn;
        TimerManager.OnTimeUp += OnTimeUp;

        if (statusText != null) statusText.text = "";

        CreateEventIndicators();
    }

    private void Update()
    {
        if (timerText != null)
        {
            // Formata MM:SS ou apenas segundos com uma casa decimal
            timerText.text = timerManager.currentTime.ToString("F1") + "s";
        }

        if (timerManager.GetTimeRatio() <= timerManager.criticalThreshold)
        {
            PulseEffect();
            if (criticalTimeParticles != null && !criticalTimeParticles.isPlaying)
                criticalTimeParticles.Play();
        }
    }

    void CreateEventIndicators()
    {
        foreach (var timedEvent in timerManager.timedEvents)
        {
            GameObject indicator = Instantiate(eventIndicatorPrefab, eventsContainer);
            RectTransform rect = indicator.GetComponent<RectTransform>();
            // Posiciona baseado no triggerTime (0 a 1)
            rect.anchorMin = new Vector2(timedEvent.triggerTime, 0.5f);
            rect.anchorMax = new Vector2(timedEvent.triggerTime, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }
    }

    void PulseEffect()
    {
        float pulse = pulseCurve.Evaluate(Time.time * pulseSpeed);
        Color targetColor = Color.Lerp(timerManager.criticalColor, Color.white, pulse);
        timeBarFill.color = targetColor;
        if (timerText != null) timerText.color = targetColor;
    }

    void OnEventTriggered(string eventName)
    {
        if (statusText != null) statusText.text = $"Evento: {eventName}";
    }

    void OnBossSpawn(string bossName)
    {
        if (statusText != null)
        {
            statusText.text = $"CUIDADO: {bossName}!";
            statusText.color = Color.red;
        }
    }

    void OnTimeUp()
    {
        if (criticalTimeParticles != null) criticalTimeParticles.Stop();
        if (statusText != null) statusText.text = "TEMPO ESGOTADO!";
    }

    private void OnDestroy()
    {
        TimerManager.OnEventTriggered -= OnEventTriggered;
        TimerManager.OnBossSpawn -= OnBossSpawn;
        TimerManager.OnTimeUp -= OnTimeUp;
    }
}