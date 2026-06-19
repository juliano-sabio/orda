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

        CriarTimerUINovo();
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
            timeBarFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(timeRatio), 1f);
            if (timeRatio <= criticalThreshold) timeBarFill.color = criticalColor;
            else if (timeRatio <= warningThreshold) timeBarFill.color = warningColor;
            else timeBarFill.color = normalColor;
        }
    }

    // Reconstrói o cronômetro com visual do tema (moldura vermelha, trilha escura, tempo branco),
    // reaproveitando a MESMA posição/canvas do cronômetro antigo.
    void CriarTimerUINovo()
    {
        // paleta do tema (brasa/forja): âmbar → laranja → vermelho (substitui o verde/amarelo)
        normalColor   = new Color(0.86f, 0.60f, 0.18f);
        warningColor  = new Color(0.90f, 0.42f, 0.12f);
        criticalColor = new Color(0.82f, 0.16f, 0.14f);

        // referência de posição/tamanho do cronômetro antigo (antes de escondê-lo)
        RectTransform refRT = null;
        if (timeBar != null)          refRT = timeBar.GetComponent<RectTransform>();
        else if (timeBarFill != null) refRT = timeBarFill.rectTransform.parent as RectTransform;
        if (refRT == null && timeText != null) refRT = timeText.rectTransform.parent as RectTransform;

        // esconde o cronômetro antigo da cena (se houver)
        if (timeText    != null) timeText.gameObject.SetActive(false);
        if (timeBar     != null) timeBar.gameObject.SetActive(false);
        if (timeBarFill != null) timeBarFill.gameObject.SetActive(false);

        var cont = new GameObject("TimerBar", typeof(RectTransform), typeof(Image));
        var rc = cont.GetComponent<RectTransform>();

        if (refRT != null && refRT.parent != null)
        {
            // mesmo lugar do antigo
            cont.transform.SetParent(refRT.parent, false);
            rc.anchorMin        = refRT.anchorMin;
            rc.anchorMax        = refRT.anchorMax;
            rc.pivot            = refRT.pivot;
            rc.anchoredPosition = refRT.anchoredPosition;
            rc.sizeDelta        = refRT.sizeDelta;
            rc.localScale       = Vector3.one;
            cont.transform.SetSiblingIndex(refRT.GetSiblingIndex());
        }
        else
        {
            // fallback: canvas próprio no topo-centro
            var canvasGO = new GameObject("TimerCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            var cs = canvasGO.AddComponent<CanvasScaler>();
            cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920f, 1080f);
            cs.matchWidthOrHeight  = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();
            cont.transform.SetParent(canvasGO.transform, false);
            rc.anchorMin = new Vector2(0.5f, 1f); rc.anchorMax = new Vector2(0.5f, 1f);
            rc.pivot = new Vector2(0.5f, 1f);
            rc.anchoredPosition = new Vector2(0f, -20f);
            rc.sizeDelta = new Vector2(520f, 38f);
        }
        cont.GetComponent<Image>().color = new Color(0.62f, 0.11f, 0.11f, 1f);

        // trilha escura (dentro da moldura)
        var trk = new GameObject("Track", typeof(RectTransform), typeof(Image));
        trk.transform.SetParent(cont.transform, false);
        var rtk = trk.GetComponent<RectTransform>();
        rtk.anchorMin = Vector2.zero; rtk.anchorMax = Vector2.one;
        rtk.offsetMin = new Vector2(3f, 3f); rtk.offsetMax = new Vector2(-3f, -3f);
        trk.GetComponent<Image>().color = new Color(0.09f, 0.05f, 0.05f, 1f);

        // preenchimento (esvazia da esquerda; largura controlada por anchorMax.x)
        var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(trk.transform, false);
        var rfi = fillGO.GetComponent<RectTransform>();
        rfi.anchorMin = Vector2.zero; rfi.anchorMax = Vector2.one;
        rfi.offsetMin = Vector2.zero; rfi.offsetMax = Vector2.zero;
        var fillImg = fillGO.GetComponent<Image>();
        fillImg.color = normalColor;

        // brilho no topo do preenchimento (acompanha a largura)
        var hi = new GameObject("FillHi", typeof(RectTransform), typeof(Image));
        hi.transform.SetParent(fillGO.transform, false);
        var rhi = hi.GetComponent<RectTransform>();
        rhi.anchorMin = new Vector2(0f, 1f); rhi.anchorMax = new Vector2(1f, 1f);
        rhi.offsetMin = new Vector2(0f, -2f); rhi.offsetMax = Vector2.zero;
        hi.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.22f);

        // texto do tempo, centralizado e branco
        var txtGO = new GameObject("TimerText", typeof(RectTransform));
        txtGO.transform.SetParent(cont.transform, false);
        var rt = txtGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "00:00";
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 10f;
        tmp.fontSizeMax = 24f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        // reaponta as referências para o novo cronômetro
        timeBar     = null;
        timeBarFill = fillImg;
        timeText    = tmp;
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
