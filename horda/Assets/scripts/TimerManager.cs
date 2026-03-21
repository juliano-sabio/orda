using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using TMPro; // Adicionado para TextMeshPro

public class TimerManager : MonoBehaviour
{
    [Header("Configurações do Timer")]
    public float levelDuration = 180f;
    public float currentTime;

    [Header("Referências UI")]
    public Slider timeBar;
    public TextMeshProUGUI timeText; // Alterado para TextMeshProUGUI
    public Image timeBarFill;

    [Header("Eventos Temporizados")]
    public TimedEvent[] timedEvents;
    public BossEvent[] bossEvents;

    [Header("Cores da Barra")]
    public Color normalColor = Color.green;
    public Color warningColor = Color.yellow;
    public Color criticalColor = Color.red;
    public float warningThreshold = 0.3f;
    public float criticalThreshold = 0.1f;

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

        Array.Sort(timedEvents, (a, b) => b.triggerTime.CompareTo(a.triggerTime));
        Array.Sort(bossEvents, (a, b) => b.triggerTime.CompareTo(a.triggerTime));
    }

    void Update()
    {
        if (!isRunning) return;

        currentTime -= Time.deltaTime;
        UpdateTimeBar();
        CheckEvents();
        CheckBossEvents();

        if (currentTime <= 0) TimeUp();
    }

    void UpdateTimeBar()
    {
        float timeRatio = currentTime / levelDuration;
        if (timeBar != null) timeBar.value = timeRatio;
        if (timeText != null) timeText.text = FormatTime(currentTime);

        if (timeBarFill != null)
        {
            if (timeRatio <= criticalThreshold) timeBarFill.color = criticalColor;
            else if (timeRatio <= warningThreshold) timeBarFill.color = warningColor;
            else timeBarFill.color = normalColor;
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
        if ((currentTime / levelDuration) <= timedEvents[currentEventIndex].triggerTime)
        {
            TriggerEvent(timedEvents[currentEventIndex]);
            currentEventIndex++;
        }
    }

    void CheckBossEvents()
    {
        if (currentBossIndex >= bossEvents.Length) return;
        if ((currentTime / levelDuration) <= bossEvents[currentBossIndex].triggerTime)
        {
            TriggerBossEvent(bossEvents[currentBossIndex]);
            currentBossIndex++;
        }
    }

    void TriggerEvent(TimedEvent timedEvent)
    {
        if (timedEvent.onTrigger != null) timedEvent.onTrigger.Invoke();
        OnEventTriggered?.Invoke(timedEvent.eventName);
    }

    void TriggerBossEvent(BossEvent bossEvent)
    {
        if (bossEvent.bossPrefab != null) Instantiate(bossEvent.bossPrefab, bossEvent.spawnPosition, Quaternion.identity);
        OnBossSpawn?.Invoke(bossEvent.bossName);
    }

    void TimeUp()
    {
        currentTime = 0;
        isRunning = false;
        OnTimeUp?.Invoke();
    }

    public float GetTimeRatio() => currentTime / levelDuration;
}

// AS CLASSES ABAIXO DEVEM FICAR FORA DA CLASSE TIMERMANAGER, MAS NO MESMO ARQUIVO
[System.Serializable]
public class TimedEvent
{
    public string eventName;
    [Range(0f, 1f)] public float triggerTime;
    public UnityEngine.Events.UnityEvent onTrigger;
}

[System.Serializable]
public class BossEvent
{
    public string bossName;
    [Range(0f, 1f)] public float triggerTime;
    public GameObject bossPrefab;
    public Vector2 spawnPosition;
}