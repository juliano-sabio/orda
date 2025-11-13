using UnityEngine;
using UnityEngine.UI;

public class TimeBarEffects : MonoBehaviour
{
    public TimerManager timerManager;
    public Image timeBarFill;
    public GameObject eventIndicatorPrefab;
    public Transform eventsContainer;

    [Header("Efeitos Visuais")]
    public ParticleSystem criticalTimeParticles;
    public AnimationCurve pulseCurve;
    public float pulseSpeed = 2f;

    private void Start()
    {
        // Inscreve nos eventos
        TimerManager.OnEventTriggered += OnEventTriggered;
        TimerManager.OnBossSpawn += OnBossSpawn;
        TimerManager.OnTimeUp += OnTimeUp;

        CreateEventIndicators();
    }

    private void Update()
    {
        if (timerManager.GetTimeRatio() <= timerManager.criticalThreshold)
        {
            PulseEffect();
        }
    }

    void CreateEventIndicators()
    {
        // Cria indicadores visuais na barra para eventos
        foreach (var timedEvent in timerManager.timedEvents)
        {
            GameObject indicator = Instantiate(eventIndicatorPrefab, eventsContainer);
            RectTransform rect = indicator.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(timedEvent.triggerTime, 0);
            rect.anchorMax = new Vector2(timedEvent.triggerTime, 1);
            rect.anchoredPosition = Vector2.zero;
        }
    }

    void PulseEffect()
    {
        float pulse = pulseCurve.Evaluate(Time.time * pulseSpeed);
        timeBarFill.color = Color.Lerp(timerManager.criticalColor, Color.white, pulse);
    }

    void OnEventTriggered(string eventName)
    {
        // Feedback visual/sonoro para evento
        Debug.Log($"Evento ocorreu: {eventName}");
    }

    void OnBossSpawn(string bossName)
    {
        // Feedback visual/sonoro para boss
        Debug.Log($"Cuidado! {bossName} apareceu!");
    }

    void OnTimeUp()
    {
        if (criticalTimeParticles != null)
            criticalTimeParticles.Stop();
    }

    private void OnDestroy()
    {
        // Remove as inscrições dos eventos
        TimerManager.OnEventTriggered -= OnEventTriggered;
        TimerManager.OnBossSpawn -= OnBossSpawn;
        TimerManager.OnTimeUp -= OnTimeUp;
    }
}