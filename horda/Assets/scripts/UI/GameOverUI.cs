using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    private GameObject painelPrincipal;
    private GameObject painelOpcoes;
    private TextMeshProUGUI labelFullscreen;
    private TextMeshProUGUI tituloText;
    private RectTransform tituloRT;
    private CanvasGroup tituloGroup;
    private RectTransform cardRT;
    private CanvasGroup cardGroup;
    private Image overlayImg;
    private Texture2D bgSnapshot;

    private readonly GameObject[] botoes = new GameObject[3];
    private readonly CanvasGroup[] botoesGroups = new CanvasGroup[3];

    public static void Mostrar(Texture2D snapshot = null)
    {
        GameObject go = new GameObject("GameOverUI");
        GameOverUI ui = go.AddComponent<GameOverUI>();
        ui.bgSnapshot = snapshot;
        ui.CriarUI();
    }

    private void CriarUI()
    {
        GameObject canvasGO = new GameObject("CanvasGameOver");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        bool screenshotValido = bgSnapshot != null && !SnapshotEhAzul(bgSnapshot);
        if (screenshotValido)
        {
            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(canvasGO.transform, false);
            RectTransform rtBg = bgGO.AddComponent<RectTransform>();
            rtBg.anchorMin = Vector2.zero;
            rtBg.anchorMax = Vector2.one;
            rtBg.offsetMin = Vector2.zero;
            rtBg.offsetMax = Vector2.zero;
            bgGO.AddComponent<RawImage>().texture = bgSnapshot;
        }

        float overlayTarget = screenshotValido ? 0.5f : 0.88f;
        GameObject overlayGO = new GameObject("Overlay");
        overlayGO.transform.SetParent(canvasGO.transform, false);
        RectTransform rtOverlay = overlayGO.AddComponent<RectTransform>();
        rtOverlay.anchorMin = Vector2.zero;
        rtOverlay.anchorMax = Vector2.one;
        rtOverlay.offsetMin = Vector2.zero;
        rtOverlay.offsetMax = Vector2.zero;
        overlayImg = overlayGO.AddComponent<Image>();
        overlayImg.color = new Color(0.04f, 0.03f, 0.07f, 0f);

        SpawnarParticulas(canvasGO.transform);

        painelPrincipal = CriarCard(canvasGO.transform);
        cardRT = painelPrincipal.GetComponent<RectTransform>();
        cardGroup = painelPrincipal.AddComponent<CanvasGroup>();
        cardGroup.alpha = 0f;
        cardRT.localScale = Vector3.one * 0.75f;
        CriarConteudoPrincipal();

        painelOpcoes = CriarCard(canvasGO.transform);
        CriarConteudoOpcoes();
        painelOpcoes.SetActive(false);

        StartCoroutine(AnimarEntrada(overlayTarget));
        StartCoroutine(PulsarTitulo());
    }

    // ──────────────────── ANIMAÇÕES ────────────────────────────────

    private IEnumerator AnimarEntrada(float overlayTarget)
    {
        Color corOverlay = overlayImg.color;

        // Fase 1: overlay fade in (0.35s)
        yield return StartCoroutine(Animar(0.35f, t =>
            overlayImg.color = new Color(corOverlay.r, corOverlay.g, corOverlay.b,
                Mathf.Lerp(0f, overlayTarget, EaseOut(t)))));

        // Fase 2: card pop in com spring (0.35s)
        yield return StartCoroutine(Animar(0.35f, t =>
        {
            cardRT.localScale = Vector3.one * Mathf.Lerp(0.75f, 1f, EaseOutBack(t));
            cardGroup.alpha = Mathf.Lerp(0f, 1f, EaseOut(t * 1.5f));
        }));

        // Fase 3: título desce de cima (0.3s)
        if (tituloRT != null && tituloGroup != null)
        {
            Vector2 posFinal = tituloRT.anchoredPosition;
            Vector2 posInicio = posFinal + new Vector2(0f, 80f);
            tituloRT.anchoredPosition = posInicio;
            tituloGroup.alpha = 0f;
            yield return StartCoroutine(Animar(0.3f, t =>
            {
                tituloRT.anchoredPosition = Vector2.Lerp(posInicio, posFinal, EaseOut(t));
                tituloGroup.alpha = Mathf.Lerp(0f, 1f, t * 2f);
            }));
        }

        // Fase 4: botões aparecem em sequência com spring
        for (int i = 0; i < botoes.Length; i++)
        {
            if (botoes[i] == null) continue;
            RectTransform rt = botoes[i].GetComponent<RectTransform>();
            CanvasGroup cg = botoesGroups[i];
            rt.localScale = Vector3.one * 0.5f;
            cg.alpha = 0f;
            int idx = i;
            StartCoroutine(Animar(0.22f, t =>
            {
                botoes[idx].GetComponent<RectTransform>().localScale =
                    Vector3.one * Mathf.Lerp(0.5f, 1f, EaseOutBack(t));
                botoesGroups[idx].alpha = Mathf.Lerp(0f, 1f, t * 2.5f);
            }));
            yield return new WaitForSecondsRealtime(0.1f);
        }
    }

    private IEnumerator PulsarTitulo()
    {
        yield return new WaitForSecondsRealtime(0.9f);
        Color corA = new Color(0.88f, 0.18f, 0.18f);
        Color corB = new Color(1f, 0.48f, 0.48f);
        while (tituloText != null)
        {
            float t = (Mathf.Sin(Time.unscaledTime * 1.3f * Mathf.PI) + 1f) * 0.5f;
            tituloText.color = Color.Lerp(corA, corB, t);
            yield return null;
        }
    }

    private IEnumerator AnimarParticula(RectTransform rt, Image img, Vector2 posBase, Color corBase)
    {
        yield return new WaitForSecondsRealtime(Random.Range(0f, 2f));
        while (rt != null)
        {
            float velocidade = Random.Range(25f, 60f);
            float vida = Random.Range(2f, 4.5f);
            float deriva = Random.Range(-20f, 20f);
            float t = 0f;
            while (t < vida && rt != null)
            {
                t += Time.unscaledDeltaTime;
                float prog = t / vida;
                rt.anchoredPosition = posBase + new Vector2(deriva * prog, velocidade * t);
                img.color = new Color(corBase.r, corBase.g, corBase.b,
                    corBase.a * (1f - prog * prog));
                yield return null;
            }
            if (rt != null)
            {
                rt.anchoredPosition = posBase;
                img.color = corBase;
                yield return new WaitForSecondsRealtime(Random.Range(0.1f, 0.8f));
            }
        }
    }

    private void SpawnarParticulas(Transform pai)
    {
        for (int i = 0; i < 12; i++)
        {
            GameObject p = new GameObject("P");
            p.transform.SetParent(pai, false);
            RectTransform rt = p.AddComponent<RectTransform>();
            float size = Random.Range(4f, 12f);
            rt.sizeDelta = new Vector2(size, size);
            Vector2 anchor = new Vector2(Random.Range(0.25f, 0.75f), Random.Range(0.15f, 0.55f));
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.anchoredPosition = Vector2.zero;
            Image img = p.AddComponent<Image>();
            Color cor = new Color(Random.Range(0.55f, 1f), Random.Range(0.05f, 0.25f),
                0.05f, Random.Range(0.5f, 0.85f));
            img.color = cor;
            StartCoroutine(AnimarParticula(rt, img, Vector2.zero, cor));
        }
    }

    // ──────────────────── EASING ───────────────────────────────────

    private IEnumerator Animar(float duracao, System.Action<float> update)
    {
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(t + Time.unscaledDeltaTime / duracao, 1f);
            update(t);
            yield return null;
        }
        update(1f);
    }

    private float EaseOut(float t) => 1f - (1f - Mathf.Clamp01(t)) * (1f - Mathf.Clamp01(t));

    private float EaseOutBack(float t)
    {
        t = Mathf.Clamp01(t);
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    // ──────────────────── CRIAÇÃO DE UI ────────────────────────────

    private GameObject CriarCard(Transform pai)
    {
        GameObject go = new GameObject("Card");
        go.transform.SetParent(pai, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(560f, 440f);
        go.AddComponent<Image>().color = new Color(0.07f, 0.05f, 0.13f, 0.96f);
        return go;
    }

    private void CriarConteudoPrincipal()
    {
        GameObject tituloGO = CriarTextoGO(painelPrincipal.transform, "GAME OVER",
            new Vector2(0f, 145f), new Vector2(500f, 90f),
            68, new Color(0.9f, 0.2f, 0.2f, 1f));
        tituloText = tituloGO.GetComponent<TextMeshProUGUI>();
        tituloRT = tituloGO.GetComponent<RectTransform>();
        tituloGroup = tituloGO.AddComponent<CanvasGroup>();

        CriarLinha(painelPrincipal.transform, new Vector2(0f, 85f), 420f,
            new Color(0.9f, 0.2f, 0.2f, 0.4f));

        botoes[0] = CriarBotao(painelPrincipal.transform, "RECOMEÇAR",
            new Vector2(0f, 20f), new Vector2(380f, 62f),
            new Color(0.1f, 0.38f, 0.16f, 1f), Recomecar);
        botoesGroups[0] = botoes[0].AddComponent<CanvasGroup>();

        botoes[1] = CriarBotao(painelPrincipal.transform, "OPÇÕES",
            new Vector2(0f, -58f), new Vector2(380f, 62f),
            new Color(0.14f, 0.16f, 0.28f, 1f), AbrirOpcoes);
        botoesGroups[1] = botoes[1].AddComponent<CanvasGroup>();

        botoes[2] = CriarBotao(painelPrincipal.transform, "SAIR",
            new Vector2(0f, -136f), new Vector2(380f, 62f),
            new Color(0.38f, 0.08f, 0.08f, 1f), Sair);
        botoesGroups[2] = botoes[2].AddComponent<CanvasGroup>();
    }

    private void CriarConteudoOpcoes()
    {
        CriarTexto(painelOpcoes.transform, "OPÇÕES",
            new Vector2(0f, 158f), new Vector2(500f, 70f),
            46, new Color(0.8f, 0.82f, 0.95f, 1f));

        CriarLinha(painelOpcoes.transform, new Vector2(0f, 108f), 420f,
            new Color(0.8f, 0.82f, 0.95f, 0.3f));

        CriarTexto(painelOpcoes.transform, "Música",
            new Vector2(-115f, 58f), new Vector2(150f, 36f),
            24, new Color(0.78f, 0.78f, 0.9f, 1f));
        float musicVol = PlayerPrefs.GetFloat("MusicVolume", AudioListener.volume);
        CriarSlider(painelOpcoes.transform, new Vector2(100f, 58f), new Vector2(240f, 28f),
            musicVol, val => {
                AudioListener.volume = val;
                PlayerPrefs.SetFloat("MusicVolume", val);
            });

        CriarTexto(painelOpcoes.transform, "Efeitos",
            new Vector2(-115f, 8f), new Vector2(150f, 36f),
            24, new Color(0.78f, 0.78f, 0.9f, 1f));
        float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 1f);
        CriarSlider(painelOpcoes.transform, new Vector2(100f, 8f), new Vector2(240f, 28f),
            sfxVol, val => PlayerPrefs.SetFloat("SFXVolume", val));

        bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
        GameObject btnFS = CriarBotao(painelOpcoes.transform,
            "Tela Cheia: " + (isFullscreen ? "ON" : "OFF"),
            new Vector2(0f, -58f), new Vector2(340f, 52f),
            new Color(0.14f, 0.16f, 0.28f, 1f), ToggleFullscreen);
        labelFullscreen = btnFS.GetComponentInChildren<TextMeshProUGUI>();

        CriarBotao(painelOpcoes.transform, "VOLTAR",
            new Vector2(0f, -150f), new Vector2(280f, 56f),
            new Color(0.22f, 0.12f, 0.3f, 1f), FecharOpcoes);
    }

    private GameObject CriarTextoGO(Transform pai, string texto, Vector2 posicao,
        Vector2 tamanho, int fontSize, Color cor)
    {
        GameObject go = new GameObject("Txt");
        go.transform.SetParent(pai, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicao;
        rt.sizeDelta = tamanho;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = fontSize;
        tmp.color = cor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        return go;
    }

    private void CriarTexto(Transform pai, string texto, Vector2 posicao,
        Vector2 tamanho, int fontSize, Color cor)
    {
        CriarTextoGO(pai, texto, posicao, tamanho, fontSize, cor);
    }

    private void CriarLinha(Transform pai, Vector2 posicao, float largura, Color cor)
    {
        GameObject go = new GameObject("Sep");
        go.transform.SetParent(pai, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicao;
        rt.sizeDelta = new Vector2(largura, 2f);
        go.AddComponent<Image>().color = cor;
    }

    private GameObject CriarBotao(Transform pai, string label, Vector2 posicao,
        Vector2 tamanho, Color cor, UnityEngine.Events.UnityAction acao)
    {
        GameObject go = new GameObject("Btn");
        go.transform.SetParent(pai, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicao;
        rt.sizeDelta = tamanho;
        Image img = go.AddComponent<Image>();
        img.color = cor;
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.highlightedColor = Color.Lerp(cor, Color.white, 0.25f);
        cb.pressedColor = Color.Lerp(cor, Color.black, 0.2f);
        cb.normalColor = cor;
        btn.colors = cb;
        btn.onClick.AddListener(acao);

        GameObject txtGO = new GameObject("Lbl");
        txtGO.transform.SetParent(go.transform, false);
        RectTransform rtTxt = txtGO.AddComponent<RectTransform>();
        rtTxt.anchorMin = Vector2.zero;
        rtTxt.anchorMax = Vector2.one;
        rtTxt.offsetMin = Vector2.zero;
        rtTxt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 26;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;

        return go;
    }

    private void CriarSlider(Transform pai, Vector2 posicao, Vector2 tamanho,
        float valorInicial, UnityEngine.Events.UnityAction<float> onChange)
    {
        GameObject sliderGO = new GameObject("Slider");
        sliderGO.transform.SetParent(pai, false);
        RectTransform rtSlider = sliderGO.AddComponent<RectTransform>();
        rtSlider.anchorMin = new Vector2(0.5f, 0.5f);
        rtSlider.anchorMax = new Vector2(0.5f, 0.5f);
        rtSlider.anchoredPosition = posicao;
        rtSlider.sizeDelta = tamanho;

        GameObject bg = new GameObject("Bg");
        bg.transform.SetParent(sliderGO.transform, false);
        RectTransform rtBg = bg.AddComponent<RectTransform>();
        rtBg.anchorMin = new Vector2(0f, 0.3f);
        rtBg.anchorMax = new Vector2(1f, 0.7f);
        rtBg.offsetMin = Vector2.zero;
        rtBg.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.28f, 1f);

        GameObject fillArea = new GameObject("FillArea");
        fillArea.transform.SetParent(sliderGO.transform, false);
        RectTransform rtFillArea = fillArea.AddComponent<RectTransform>();
        rtFillArea.anchorMin = new Vector2(0f, 0.3f);
        rtFillArea.anchorMax = new Vector2(1f, 0.7f);
        rtFillArea.offsetMin = new Vector2(5f, 0f);
        rtFillArea.offsetMax = new Vector2(-15f, 0f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform rtFill = fill.AddComponent<RectTransform>();
        rtFill.anchorMin = new Vector2(0f, 0f);
        rtFill.anchorMax = new Vector2(0f, 1f);
        rtFill.sizeDelta = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.28f, 0.56f, 0.92f, 1f);

        GameObject handleArea = new GameObject("HandleArea");
        handleArea.transform.SetParent(sliderGO.transform, false);
        RectTransform rtHandleArea = handleArea.AddComponent<RectTransform>();
        rtHandleArea.anchorMin = Vector2.zero;
        rtHandleArea.anchorMax = Vector2.one;
        rtHandleArea.offsetMin = new Vector2(10f, 0f);
        rtHandleArea.offsetMax = new Vector2(-10f, 0f);

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform rtHandle = handle.AddComponent<RectTransform>();
        rtHandle.sizeDelta = new Vector2(20f, 0f);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = new Color(0.45f, 0.75f, 1f, 1f);

        Slider slider = sliderGO.AddComponent<Slider>();
        slider.fillRect = rtFill;
        slider.handleRect = rtHandle;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = valorInicial;
        slider.onValueChanged.AddListener(onChange);
    }

    // ──────────────────── AÇÕES ────────────────────────────────────

    private void AbrirOpcoes()
    {
        painelPrincipal.SetActive(false);
        painelOpcoes.SetActive(true);
    }

    private void FecharOpcoes()
    {
        painelOpcoes.SetActive(false);
        painelPrincipal.SetActive(true);
    }

    private void ToggleFullscreen()
    {
        bool novoEstado = !Screen.fullScreen;
        Screen.fullScreen = novoEstado;
        PlayerPrefs.SetInt("Fullscreen", novoEstado ? 1 : 0);
        if (labelFullscreen != null)
            labelFullscreen.text = "Tela Cheia: " + (novoEstado ? "ON" : "OFF");
    }

    private void Recomecar()
    {
        Time.timeScale = 1f;
        if (bgSnapshot != null) Destroy(bgSnapshot);
        LimparManagersPersistentes();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void Sair()
    {
        Time.timeScale = 1f;
        if (bgSnapshot != null) Destroy(bgSnapshot);
        LimparManagersPersistentes();
        SceneManager.LoadScene("menu_inicial");
    }

    private void LimparManagersPersistentes()
    {
        // Canvas do sistema de eventos (criado com DontDestroyOnLoad)
        GameObject eventoCanvas = GameObject.Find("EventoCanvas");
        if (eventoCanvas != null) Destroy(eventoCanvas);

        // Borda sangrenta do evento Ceifador
        GameObject bordaSangue = GameObject.Find("BordaSangue");
        if (bordaSangue != null) Destroy(bordaSangue);

        if (GerenciadorEventos.Instance != null)      Destroy(GerenciadorEventos.Instance.gameObject);
        if (UIManager.Instance != null)               Destroy(UIManager.Instance.gameObject);
        if (PauseManager.Instance != null)            Destroy(PauseManager.Instance.gameObject);
        if (SkillManager.Instance != null)            Destroy(SkillManager.Instance.gameObject);
        if (StatusCardSystem.Instance != null)        Destroy(StatusCardSystem.Instance.gameObject);
        if (SkillEvolutionManager.Instance != null)   Destroy(SkillEvolutionManager.Instance.gameObject);
        if (SkillEvolutionUI.Instance != null)        Destroy(SkillEvolutionUI.Instance.gameObject);
    }

    private bool SnapshotEhAzul(Texture2D tex)
    {
        Color c = tex.GetPixel(tex.width / 2, tex.height / 2);
        return c.b > 0.35f && c.b > c.r + 0.1f && c.b > c.g + 0.05f;
    }
}
