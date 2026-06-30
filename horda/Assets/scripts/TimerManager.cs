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
        currentTime = 0f; // cronometro CRESCENTE: comeca em 0 e sobe (sem fim)
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

        currentTime += Time.deltaTime; // sobe indefinidamente — sem "tempo para acabar"
        UpdateTimeBar();
        CheckEvents();
        CheckBossEvents();
    }

    bool HaBossVivo() => bossAtivo != null;

    void UpdateTimeBar()
    {
        // Cronometro crescente sem fim: o badge mostra apenas o tempo decorrido.
        if (timeText != null) timeText.text = FormatTime(currentTime);
    }

    // Reconstrói o cronômetro como um BADGE/pílula (moldura âmbar, fundo escuro, tempo branco
    // com brilho e sombra), reaproveitando a MESMA posição/canvas do cronômetro antigo.
    // Não há mais barra de progresso: o tempo é crescente e sem fim.
    void CriarTimerUINovo()
    {
        // paleta do tema (brasa/forja)
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

        // fallback: algumas cenas (ex.: segunda_fase) têm o cronômetro antigo solto
        // na UI sem estar referenciado nos campos acima — esconde por nome também.
        // ("timeText" é filho de "timeBar", então esconder a barra cobre os dois.)
        foreach (var nome in new[] { "timeBar", "timeText", "timeBarFill" })
        {
            var antigo = GameObject.Find(nome);
            if (antigo != null) antigo.SetActive(false);
        }

        // container transparente ocupando o MESMO slot do cronômetro antigo
        var cont = new GameObject("TimerBadge", typeof(RectTransform), typeof(Image));
        var rc = cont.GetComponent<RectTransform>();

        // posiciona SEMPRE no canto superior esquerdo (com uma margem), do tamanho do badge
        Vector2 cantoMargem = new Vector2(16f, -16f);
        Vector2 badgeSize   = new Vector2(220f, 54f);

        if (refRT != null && refRT.parent != null)
        {
            cont.transform.SetParent(refRT.parent, false);
            cont.transform.SetSiblingIndex(refRT.GetSiblingIndex());
        }
        else
        {
            // fallback: canvas próprio
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
        }
        // ancora no topo-esquerda
        rc.anchorMin = new Vector2(0f, 1f); rc.anchorMax = new Vector2(0f, 1f);
        rc.pivot = new Vector2(0f, 1f);
        rc.anchoredPosition = cantoMargem;
        rc.sizeDelta = badgeSize;
        rc.localScale = Vector3.one;
        cont.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f); // transparente

        // ── PÍLULA centralizada dentro do slot ───────────────────────────────
        // borda externa escura (contorno)
        var pill = new GameObject("Pill", typeof(RectTransform), typeof(Image));
        pill.transform.SetParent(cont.transform, false);
        var rpi = pill.GetComponent<RectTransform>();
        rpi.anchorMin = new Vector2(0.5f, 0.5f); rpi.anchorMax = new Vector2(0.5f, 0.5f);
        rpi.pivot = new Vector2(0.5f, 0.5f); rpi.anchoredPosition = Vector2.zero;
        rpi.sizeDelta = new Vector2(220f, 54f);
        pill.GetComponent<Image>().color = new Color(0.16f, 0.04f, 0.04f, 1f);

        // moldura âmbar
        var frame = new GameObject("Frame", typeof(RectTransform), typeof(Image));
        frame.transform.SetParent(pill.transform, false);
        var rfr = frame.GetComponent<RectTransform>();
        rfr.anchorMin = Vector2.zero; rfr.anchorMax = Vector2.one;
        rfr.offsetMin = new Vector2(3f, 3f); rfr.offsetMax = new Vector2(-3f, -3f);
        frame.GetComponent<Image>().color = normalColor;

        // fundo escuro interno
        var bg = new GameObject("BG", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(frame.transform, false);
        var rbg = bg.GetComponent<RectTransform>();
        rbg.anchorMin = Vector2.zero; rbg.anchorMax = Vector2.one;
        rbg.offsetMin = new Vector2(3f, 3f); rbg.offsetMax = new Vector2(-3f, -3f);
        bg.GetComponent<Image>().color = new Color(0.10f, 0.06f, 0.05f, 1f);

        // brilho superior (gloss)
        var gloss = new GameObject("Gloss", typeof(RectTransform), typeof(Image));
        gloss.transform.SetParent(bg.transform, false);
        var rgl = gloss.GetComponent<RectTransform>();
        rgl.anchorMin = new Vector2(0f, 0.55f); rgl.anchorMax = new Vector2(1f, 1f);
        rgl.offsetMin = Vector2.zero; rgl.offsetMax = Vector2.zero;
        gloss.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.05f);

        // sombra do texto (leve deslocamento pra baixo)
        var shGO = new GameObject("TimerTextShadow", typeof(RectTransform));
        shGO.transform.SetParent(bg.transform, false);
        var rsh = shGO.GetComponent<RectTransform>();
        rsh.anchorMin = Vector2.zero; rsh.anchorMax = Vector2.one;
        rsh.offsetMin = new Vector2(0f, -2f); rsh.offsetMax = new Vector2(0f, -2f);
        var sh = shGO.AddComponent<TextMeshProUGUI>();
        sh.text = "00:00";
        sh.enableAutoSizing = true; sh.fontSizeMin = 12f; sh.fontSizeMax = 28f;
        sh.fontStyle = FontStyles.Bold;
        sh.alignment = TextAlignmentOptions.Center;
        sh.characterSpacing = 4f;
        sh.color = new Color(0f, 0f, 0f, 0.55f);

        // texto do tempo (branco quente, negrito)
        var txtGO = new GameObject("TimerText", typeof(RectTransform));
        txtGO.transform.SetParent(bg.transform, false);
        var rt = txtGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "00:00";
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 12f;
        tmp.fontSizeMax = 28f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.characterSpacing = 4f;
        tmp.color = new Color(1f, 0.97f, 0.90f, 1f);

        // reaponta as referências para o novo cronômetro (sem barra)
        timeBar     = null;
        timeBarFill = null;
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
        // triggerTime continua sendo a "fracao restante" de referencia (compatibilidade);
        // como agora contamos pra cima, comparamos pela fracao restante = 1 - decorrido/levelDuration.
        if ((1f - currentTime / levelDuration) <= timedEvents[currentEventIndex].triggerTime)
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

        if ((1f - currentTime / levelDuration) <= be.triggerTime)
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
        currentTime = 0f; // recomeca a contagem crescente
        isRunning = true;
        UpdateTimeBar();
    }

    // Entra direto no modo infinito (usado pela tela de escolha / cena de sobrevivencia).
    public void ModoInfinitoDireto()
    {
        modoSobrevivencia = true;
        ReiniciarCiclo();
    }

    // currentTime agora e o tempo DECORRIDO; devolve a fracao "restante" so por compatibilidade.
    public float GetTimeRatio() => Mathf.Clamp01(1f - currentTime / levelDuration);
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
