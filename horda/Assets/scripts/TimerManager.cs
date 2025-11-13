using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class TimerManager : MonoBehaviour
{
    [Header("Configurações do Timer")]
    public float levelDuration = 180f; // Duração total do level em segundos
    public float currentTime;

    [Header("Referências UI")]
    public Slider timeBar;
    public Text timeText;
    public Image timeBarFill;

    [Header("Eventos Temporizados")]
    public TimedEvent[] timedEvents;
    public BossEvent[] bossEvents;

    [Header("Cores da Barra")]
    public Color normalColor = Color.green;
    public Color warningColor = Color.yellow;
    public Color criticalColor = Color.red;
    public float warningThreshold = 0.3f; // 30% do tempo
    public float criticalThreshold = 0.1f; // 10% do tempo

    // Eventos
    public static event Action OnTimeUp;
    public static event Action<string> OnEventTriggered;
    public static event Action<string> OnBossSpawn;

    private bool isRunning = false;
    private int currentEventIndex = 0;
    private int currentBossIndex = 0;

    void Start()
    {
        InitializeTimer();
    }

    void InitializeTimer()
    {
        currentTime = levelDuration;
        UpdateTimeBar();
        isRunning = true;

        // Ordena eventos por tempo
        Array.Sort(timedEvents, (a, b) => a.triggerTime.CompareTo(b.triggerTime));
        Array.Sort(bossEvents, (a, b) => a.triggerTime.CompareTo(b.triggerTime));
    }

    void Update()
    {
        if (!isRunning) return;

        currentTime -= Time.deltaTime;
        UpdateTimeBar();
        CheckEvents();
        CheckBossEvents();

        if (currentTime <= 0)
        {
            TimeUp();
        }
    }

    void UpdateTimeBar()
    {
        float timeRatio = currentTime / levelDuration;

        // Atualiza slider
        timeBar.value = timeRatio;

        // Atualiza texto
        timeText.text = FormatTime(currentTime);

        // Muda cor baseada no tempo restante
        if (timeRatio <= criticalThreshold)
        {
            timeBarFill.color = criticalColor;
        }
        else if (timeRatio <= warningThreshold)
        {
            timeBarFill.color = warningColor;
        }
        else
        {
            timeBarFill.color = normalColor;
        }
    }

    string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    void CheckEvents()
    {
        if (currentEventIndex >= timedEvents.Length) return;

        TimedEvent nextEvent = timedEvents[currentEventIndex];
        float timeRatio = currentTime / levelDuration;

        if (timeRatio <= nextEvent.triggerTime)
        {
            TriggerEvent(nextEvent);
            currentEventIndex++;
        }
    }

    void CheckBossEvents()
    {
        if (currentBossIndex >= bossEvents.Length) return;

        BossEvent nextBoss = bossEvents[currentBossIndex];
        float timeRatio = currentTime / levelDuration;

        if (timeRatio <= nextBoss.triggerTime)
        {
            TriggerBossEvent(nextBoss);
            currentBossIndex++;
        }
    }

    void TriggerEvent(TimedEvent timedEvent)
    {
        // Executa o evento
        if (timedEvent.onTrigger != null)
        {
            timedEvent.onTrigger.Invoke();
        }

        // Dispara evento global
        OnEventTriggered?.Invoke(timedEvent.eventName);

        Debug.Log($"Evento disparado: {timedEvent.eventName}");
    }

    void TriggerBossEvent(BossEvent bossEvent)
    {
        // Instancia o boss
        if (bossEvent.bossPrefab != null)
        {
            Instantiate(bossEvent.bossPrefab, bossEvent.spawnPosition, Quaternion.identity);
        }

        // Dispara evento global
        OnBossSpawn?.Invoke(bossEvent.bossName);

        Debug.Log($"Boss apareceu: {bossEvent.bossName}");
    }

    void TimeUp()
    {
        currentTime = 0;
        isRunning = false;
        OnTimeUp?.Invoke();
        Debug.Log("Tempo esgotado!");
    }

    // Métodos públicos para controle externo
    public void AddTime(float seconds)
    {
        currentTime += seconds;
        if (currentTime > levelDuration) currentTime = levelDuration;
    }

    public void SubtractTime(float seconds)
    {
        currentTime -= seconds;
        if (currentTime < 0) currentTime = 0;
    }

    public void PauseTimer()
    {
        isRunning = false;
    }

    public void ResumeTimer()
    {
        isRunning = true;
    }

    public float GetTimeRatio()
    {
        return currentTime / levelDuration;
    }
}

[System.Serializable]
public class TimedEvent
{
    public string eventName;
    [Range(0f, 1f)]
    public float triggerTime; // 0-1 representando porcentagem do tempo total
    public UnityEngine.Events.UnityEvent onTrigger;
}

[System.Serializable]
public class BossEvent
{
    public string bossName;
    [Range(0f, 1f)]
    public float triggerTime; // 0-1 representando porcentagem do tempo total
    public GameObject bossPrefab;
    public Vector2 spawnPosition;
}