using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class TimerManager : MonoBehaviour
{
    [Header("Configuracoes do Timer")]
    public float levelDuration = 1800f; // 30 min (em segundos)
    public float currentTime;

    [Header("Modo")]
    [Tooltip("Sobrevivencia: comeca direto no ciclo infinito, sem tela de escolha pos-vitoria.")]
    public bool modoSobrevivencia = false;
    [Tooltip("Nome da cena que ja inicia em modo Sobrevivencia (infinito direto).")]
    public string nomeCenaSobrevivencia = "Modo_sobrevivencia";

    [Header("Referencias UI")]
    public Slider timeBar;
    public TextMeshProUGUI timeText;
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

    // Controle de ciclo / bosses
    private GameObject bossAtivo;        // boss intermediario vivo -> congela o contador
    private GameObject bossFinalAtivo;   // boss final vivo -> conclusao da run
    private bool aguardandoBossFinal = false;
    private int cicloAtual = 1;

    public int CicloAtual => cicloAtual;

    void Start()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == nomeCenaSobrevivencia)
            modoSobrevivencia = true;

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
        // Esperando o boss final morrer: nao conta tempo, so monitora.
        if (aguardandoBossFinal)
        {
            VerificarBossFinalMorto();
            return;
        }

        if (!isRunning) return;

        // Boss intermediario vivo congela o contador (a run "para" durante a luta).
        if (HaBossVivo())
        {
            UpdateTimeBar();
            return;
        }

        currentTime -= Time.deltaTime;
        UpdateTimeBar();
        CheckEvents();
        CheckBossEvents();

        if (currentTime <= 0) InvocarBossFinal();
    }

    bool HaBossVivo() => bossAtivo != null;

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

        var be = bossEvents[currentBossIndex];
        // O boss final nao e disparado por triggerTime; ele aparece quando o tempo zera.
        if (be.ehFinal) { currentBossIndex++; return; }

        if ((currentTime / levelDuration) <= be.triggerTime)
        {
            bossAtivo = TriggerBossEvent(be);
            currentBossIndex++;
        }
    }

    void TriggerEvent(TimedEvent timedEvent)
    {
        if (timedEvent.onTrigger != null) timedEvent.onTrigger.Invoke();
        OnEventTriggered?.Invoke(timedEvent.eventName);
    }

    GameObject TriggerBossEvent(BossEvent bossEvent)
    {
        GameObject inst = null;
        if (bossEvent.bossPrefab != null)
            inst = NetSpawn.Spawnar(bossEvent.bossPrefab, bossEvent.spawnPosition); // host-only em rede
        OnBossSpawn?.Invoke(bossEvent.bossName);
        return inst;
    }

    // Tempo zerou: aparece o boss final da fase e a run aguarda o desfecho.
    void InvocarBossFinal()
    {
        currentTime = 0;
        isRunning = false;
        UpdateTimeBar();
        OnTimeUp?.Invoke();

        BossEvent final = null;
        foreach (var be in bossEvents)
        {
            if (be.ehFinal) { final = be; break; }
        }
        if (final != null) bossFinalAtivo = TriggerBossEvent(final);

        aguardandoBossFinal = true;
    }

    void VerificarBossFinalMorto()
    {
        if (bossFinalAtivo != null) return; // ainda vivo (ou nunca existiu -> conclui imediato)
        aguardandoBossFinal = false;
        AbrirEscolhaVitoria();
    }

    void AbrirEscolhaVitoria()
    {
        if (modoSobrevivencia)
        {
            // No modo Sobrevivencia o jogador ja escolheu o infinito: apenas recicla.
            ReiniciarCiclo();
            return;
        }
        EscolhaPosVitoriaUI.Mostrar(this);
    }

    // Recomeca o ciclo da run SEM resetar status/skills/evolucoes do player.
    // A escala (EnemyScaling) usa Time.timeSinceLevelLoad, entao continua crescendo.
    public void ReiniciarCiclo()
    {
        cicloAtual++;
        currentEventIndex = 0;
        currentBossIndex = 0;
        bossAtivo = null;
        bossFinalAtivo = null;
        aguardandoBossFinal = false;
        currentTime = levelDuration;
        isRunning = true;
        UpdateTimeBar();
    }

    // Entra direto no modo infinito (usado pela tela de escolha / cena de sobrevivencia).
    public void ModoInfinitoDireto()
    {
        modoSobrevivencia = true;
        ReiniciarCiclo();
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
    [Tooltip("Se marcado, este e o boss FINAL da fase: aparece quando o tempo zera e encerra a run.")]
    public bool ehFinal = false;
}
