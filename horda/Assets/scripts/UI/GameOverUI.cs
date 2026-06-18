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

    private readonly GameObject[] botoes = new GameObject[4];
    private readonly CanvasGroup[] botoesGroups = new CanvasGroup[4];

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
        cardRT.sizeDelta = new Vector2(560f, 520f); // mais alto p/ caber 4 botões
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

    private static readonly Color corBordaGO = new Color(0.62f, 0.11f, 0.11f); // vermelho escuro

    private GameObject CriarCard(Transform pai)
    {
        // container sem Image — irmãos controlam renderização
        GameObject go = new GameObject("Card");
        go.transform.SetParent(pai, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(560f, 440f);

        // irmão 0: borda dourada (2px ao redor)
        GameObject brd = new GameObject("Brd"); brd.transform.SetParent(go.transform, false);
        RectTransform rbrd = brd.AddComponent<RectTransform>();
        rbrd.anchorMin = Vector2.zero; rbrd.anchorMax = Vector2.one;
        rbrd.offsetMin = new Vector2(-2f,-2f); rbrd.offsetMax = new Vector2(2f,2f);
        brd.AddComponent<Image>().color = new Color(corBordaGO.r, corBordaGO.g, corBordaGO.b, 0.80f);

        // irmão 1: fundo escuro do card
        GameObject fundo = new GameObject("Fundo"); fundo.transform.SetParent(go.transform, false);
        RectTransform rf = fundo.AddComponent<RectTransform>();
        rf.anchorMin = Vector2.zero; rf.anchorMax = Vector2.one; rf.offsetMin = rf.offsetMax = Vector2.zero;
        fundo.AddComponent<Image>().color = new Color(0.06f, 0.03f, 0.03f, 0.97f);

        return go;
    }

    private void CriarConteudoPrincipal()
    {
        GameObject tituloGO = CriarTextoGO(painelPrincipal.transform, "GAME OVER",
            new Vector2(0f, 175f), new Vector2(500f, 90f),
            68, new Color(0.9f, 0.2f, 0.2f, 1f));
        tituloText = tituloGO.GetComponent<TextMeshProUGUI>();
        tituloRT = tituloGO.GetComponent<RectTransform>();
        tituloGroup = tituloGO.AddComponent<CanvasGroup>();

        CriarLinha(painelPrincipal.transform, new Vector2(0f, 115f), 420f,
            new Color(0.9f, 0.2f, 0.2f, 0.4f));

        Color corpoBtn = new Color(0.11f, 0.07f, 0.07f, 1f); // quase preto (tema vermelho/preto/branco)

        botoes[0] = CriarBotao(painelPrincipal.transform, Loc.T("ui.restart"),
            new Vector2(0f, 55f), new Vector2(380f, 60f),
            corpoBtn, Recomecar);
        botoesGroups[0] = botoes[0].AddComponent<CanvasGroup>();

        botoes[1] = CriarBotao(painelPrincipal.transform, "CONFIGURAÇÕES",
            new Vector2(0f, -23f), new Vector2(380f, 60f),
            corpoBtn, AbrirConfiguracoesMenu);
        botoesGroups[1] = botoes[1].AddComponent<CanvasGroup>();

        botoes[2] = CriarBotao(painelPrincipal.transform, "SELEÇÃO",
            new Vector2(0f, -101f), new Vector2(380f, 60f),
            corpoBtn, IrParaSelecao);
        botoesGroups[2] = botoes[2].AddComponent<CanvasGroup>();

        botoes[3] = CriarBotao(painelPrincipal.transform, Loc.T("ui.quit"),
            new Vector2(0f, -179f), new Vector2(380f, 60f),
            corpoBtn, Sair);
        botoesGroups[3] = botoes[3].AddComponent<CanvasGroup>();
    }

    private void CriarConteudoOpcoes()
    {
        CriarTexto(painelOpcoes.transform, Loc.T("ui.options"),
            new Vector2(0f, 158f), new Vector2(500f, 70f),
            46, new Color(0.8f, 0.82f, 0.95f, 1f));

        CriarLinha(painelOpcoes.transform, new Vector2(0f, 108f), 420f,
            new Color(0.8f, 0.82f, 0.95f, 0.3f));

        CriarTexto(painelOpcoes.transform, Loc.T("settings.music"),
            new Vector2(-115f, 58f), new Vector2(150f, 36f),
            24, new Color(0.78f, 0.78f, 0.9f, 1f));
        float musicVol = PlayerPrefs.GetFloat("MusicVolume", AudioListener.volume);
        CriarSlider(painelOpcoes.transform, new Vector2(100f, 58f), new Vector2(240f, 28f),
            musicVol, val => {
                AudioListener.volume = val;
                PlayerPrefs.SetFloat("MusicVolume", val);
            });

        CriarTexto(painelOpcoes.transform, Loc.T("settings.sfx"),
            new Vector2(-115f, 8f), new Vector2(150f, 36f),
            24, new Color(0.78f, 0.78f, 0.9f, 1f));
        float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 1f);
        CriarSlider(painelOpcoes.transform, new Vector2(100f, 8f), new Vector2(240f, 28f),
            sfxVol, val => PlayerPrefs.SetFloat("SFXVolume", val));

        bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
        GameObject btnFS = CriarBotao(painelOpcoes.transform,
            Loc.T("ui.fullscreen") + ": " + (isFullscreen ? "ON" : "OFF"),
            new Vector2(0f, -58f), new Vector2(340f, 52f),
            new Color(0.14f, 0.16f, 0.28f, 1f), ToggleFullscreen);
        labelFullscreen = btnFS.GetComponentInChildren<TextMeshProUGUI>();

        CriarBotao(painelOpcoes.transform, Loc.T("ui.back"),
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
        // container sem Image — mesma lógica de irmãos do MenuInicialUI
        GameObject go = new GameObject("Btn");
        go.transform.SetParent(pai, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicao;
        rt.sizeDelta = tamanho;

        // irmão 0: borda dourada (atrás)
        GameObject brd = new GameObject("Brd"); brd.transform.SetParent(go.transform, false);
        RectTransform rbrd = brd.AddComponent<RectTransform>();
        rbrd.anchorMin = Vector2.zero; rbrd.anchorMax = Vector2.one;
        rbrd.offsetMin = new Vector2(-1f,-1f); rbrd.offsetMax = new Vector2(1f,1f);
        brd.AddComponent<Image>().color = new Color(corBordaGO.r, corBordaGO.g, corBordaGO.b, 0.80f);

        // irmão 1: corpo (frente — cobre borda)
        bool isDanger = cor.r > 0.28f && cor.g < 0.15f && cor.b < 0.15f;
        GameObject corpo = new GameObject("Corpo"); corpo.transform.SetParent(go.transform, false);
        RectTransform rco = corpo.AddComponent<RectTransform>();
        rco.anchorMin = Vector2.zero; rco.anchorMax = Vector2.one; rco.offsetMin = rco.offsetMax = Vector2.zero;
        Image img = corpo.AddComponent<Image>(); img.color = cor;

        // irmão 2: bevel topo
        GameObject topo = new GameObject("HiT"); topo.transform.SetParent(go.transform, false);
        RectTransform rtopo = topo.AddComponent<RectTransform>();
        rtopo.anchorMin = new Vector2(0f,1f); rtopo.anchorMax = new Vector2(1f,1f);
        rtopo.offsetMin = new Vector2(0f,-2f); rtopo.offsetMax = Vector2.zero;
        topo.AddComponent<Image>().color = new Color(1f,1f,1f, 0.12f);

        // irmão 3: sombra base
        GameObject base2 = new GameObject("ShB"); base2.transform.SetParent(go.transform, false);
        RectTransform rbase = base2.AddComponent<RectTransform>();
        rbase.anchorMin = Vector2.zero; rbase.anchorMax = new Vector2(1f,0f);
        rbase.offsetMin = Vector2.zero; rbase.offsetMax = new Vector2(0f,2f);
        base2.AddComponent<Image>().color = new Color(0f,0f,0f,0.50f);

        // irmão 4: acento lateral
        GameObject ac = new GameObject("Ac"); ac.transform.SetParent(go.transform, false);
        RectTransform rac = ac.AddComponent<RectTransform>();
        rac.anchorMin = Vector2.zero; rac.anchorMax = new Vector2(0f,1f);
        rac.offsetMin = Vector2.zero; rac.offsetMax = new Vector2(4f,0f);
        ac.AddComponent<Image>().color = isDanger
            ? new Color(0.95f,0.18f,0.10f,1f)
            : new Color(corBordaGO.r,corBordaGO.g,corBordaGO.b,0.90f);

        // Button
        Button btn = go.AddComponent<Button>(); btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        Color corHov = new Color(Mathf.Min(cor.r+0.15f,1f),Mathf.Min(cor.g+0.10f,1f),Mathf.Min(cor.b+0.08f,1f),cor.a);
        Color corPrs = new Color(Mathf.Max(cor.r-0.06f,0f),Mathf.Max(cor.g-0.04f,0f),Mathf.Max(cor.b-0.03f,0f),cor.a);
        btn.colors = new ColorBlock{normalColor=cor,highlightedColor=corHov,pressedColor=corPrs,selectedColor=cor,disabledColor=new Color(cor.r*0.5f,cor.g*0.5f,cor.b*0.5f,0.5f),colorMultiplier=1f,fadeDuration=0.1f};
        btn.onClick.AddListener(acao);

        // texto
        GameObject txtGO = new GameObject("Lbl"); txtGO.transform.SetParent(go.transform, false);
        RectTransform rtTxt = txtGO.AddComponent<RectTransform>();
        rtTxt.anchorMin = new Vector2(0.04f,0f); rtTxt.anchorMax = Vector2.one; rtTxt.offsetMin = rtTxt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 22;
        tmp.color = new Color(0.95f,0.95f,0.95f);
        tmp.alignment = TextAlignmentOptions.Center; tmp.fontStyle = FontStyles.Bold;

        return go;
    }

    private void CriarSlider(Transform pai, Vector2 posicao, Vector2 tamanho,
        float valorInicial, UnityEngine.Events.UnityAction<float> onChange)
    {
        // mesma lógica do Slider redesenhado do MenuInicialUI
        GameObject sliderGO = new GameObject("Slider");
        sliderGO.transform.SetParent(pai, false);
        RectTransform rtSlider = sliderGO.AddComponent<RectTransform>();
        rtSlider.anchorMin = new Vector2(0.5f,0.5f); rtSlider.anchorMax = new Vector2(0.5f,0.5f);
        rtSlider.anchoredPosition = posicao; rtSlider.sizeDelta = tamanho;

        // irmão 0: borda dourada
        GameObject brd=new GameObject("Brd"); brd.transform.SetParent(sliderGO.transform,false);
        RectTransform rbrd=brd.AddComponent<RectTransform>(); rbrd.anchorMin=Vector2.zero; rbrd.anchorMax=Vector2.one;
        rbrd.offsetMin=new Vector2(-1f,-1f); rbrd.offsetMax=new Vector2(1f,1f);
        brd.AddComponent<Image>().color=new Color(corBordaGO.r,corBordaGO.g,corBordaGO.b,0.50f);

        // irmão 1: trilha escura com entalhe
        GameObject trk=new GameObject("Trk"); trk.transform.SetParent(sliderGO.transform,false);
        RectTransform rtrk=trk.AddComponent<RectTransform>(); rtrk.anchorMin=Vector2.zero; rtrk.anchorMax=Vector2.one; rtrk.offsetMin=rtrk.offsetMax=Vector2.zero;
        trk.AddComponent<Image>().color=new Color(0.07f,0.04f,0.03f);
        GameObject ist=new GameObject("InT"); ist.transform.SetParent(trk.transform,false);
        RectTransform rist=ist.AddComponent<RectTransform>(); rist.anchorMin=new Vector2(0f,1f); rist.anchorMax=new Vector2(1f,1f);
        rist.offsetMin=new Vector2(0f,-3f); rist.offsetMax=Vector2.zero;
        ist.AddComponent<Image>().color=new Color(0f,0f,0f,0.65f);

        // irmão 2: fill area
        GameObject fa=new GameObject("FA"); fa.transform.SetParent(sliderGO.transform,false);
        RectTransform rfa=fa.AddComponent<RectTransform>(); rfa.anchorMin=Vector2.zero; rfa.anchorMax=Vector2.one; rfa.offsetMin=rfa.offsetMax=Vector2.zero;
        GameObject fi=new GameObject("Fi"); fi.transform.SetParent(fa.transform,false);
        RectTransform rfi=fi.AddComponent<RectTransform>(); rfi.anchorMin=Vector2.zero; rfi.anchorMax=Vector2.one; rfi.offsetMin=rfi.offsetMax=Vector2.zero;
        fi.AddComponent<Image>().color=new Color(0.75f,0.45f,0.04f);
        GameObject fhl=new GameObject("FHl"); fhl.transform.SetParent(fi.transform,false);
        RectTransform rfhl=fhl.AddComponent<RectTransform>(); rfhl.anchorMin=new Vector2(0f,1f); rfhl.anchorMax=new Vector2(1f,1f);
        rfhl.offsetMin=new Vector2(0f,-1f); rfhl.offsetMax=Vector2.zero;
        fhl.AddComponent<Image>().color=new Color(1f,0.95f,0.50f,0.40f);

        // irmão 3: handle area
        GameObject ha=new GameObject("HA"); ha.transform.SetParent(sliderGO.transform,false);
        RectTransform rha=ha.AddComponent<RectTransform>(); rha.anchorMin=Vector2.zero; rha.anchorMax=Vector2.one; rha.offsetMin=rha.offsetMax=Vector2.zero;
        GameObject h=new GameObject("H"); h.transform.SetParent(ha.transform,false);
        RectTransform rh=h.AddComponent<RectTransform>(); rh.anchorMin=new Vector2(0f,0f); rh.anchorMax=new Vector2(0f,1f);
        rh.offsetMin=new Vector2(0f,-3f); rh.offsetMax=new Vector2(12f,3f);
        GameObject kSh=new GameObject("KSh"); kSh.transform.SetParent(h.transform,false);
        RectTransform rkSh=kSh.AddComponent<RectTransform>(); rkSh.anchorMin=Vector2.zero; rkSh.anchorMax=Vector2.one;
        rkSh.offsetMin=new Vector2(-1f,-1f); rkSh.offsetMax=new Vector2(1f,1f);
        kSh.AddComponent<Image>().color=new Color(0f,0f,0f,0.80f);
        GameObject kBd=new GameObject("KBd"); kBd.transform.SetParent(h.transform,false);
        RectTransform rkBd=kBd.AddComponent<RectTransform>(); rkBd.anchorMin=Vector2.zero; rkBd.anchorMax=Vector2.one; rkBd.offsetMin=rkBd.offsetMax=Vector2.zero;
        Image handleImg=kBd.AddComponent<Image>(); handleImg.color=new Color(0.94f,0.80f,0.28f);
        GameObject kHi=new GameObject("KHi"); kHi.transform.SetParent(h.transform,false);
        RectTransform rkHi=kHi.AddComponent<RectTransform>(); rkHi.anchorMin=new Vector2(0f,1f); rkHi.anchorMax=new Vector2(1f,1f);
        rkHi.offsetMin=new Vector2(1f,-2f); rkHi.offsetMax=new Vector2(-1f,0f);
        kHi.AddComponent<Image>().color=new Color(1f,0.98f,0.72f,0.65f);

        Slider slider=sliderGO.AddComponent<Slider>();
        slider.fillRect=rfi; slider.handleRect=rh; slider.targetGraphic=handleImg;
        slider.direction=Slider.Direction.LeftToRight; slider.minValue=0f; slider.maxValue=1f;
        slider.value=valorInicial; slider.onValueChanged.AddListener(onChange);
    }

    // ──────────────────── AÇÕES ────────────────────────────────────

    private GameObject opcoesInGameGO;

    // Abre o MESMO painel de opções da tela inicial (abas), igual ao pause.
    private void AbrirConfiguracoesMenu()
    {
        if (opcoesInGameGO != null) return;
        if (painelPrincipal != null) painelPrincipal.SetActive(false);

        opcoesInGameGO = new GameObject("OpcoesInGame");
        var menu = opcoesInGameGO.AddComponent<MenuInicialUI>();
        menu.modoSomenteOpcoes = true;
        menu.aoFecharOpcoes = () =>
        {
            opcoesInGameGO = null;
            if (painelPrincipal != null) painelPrincipal.SetActive(true);
        };
    }

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
            labelFullscreen.text = Loc.T("ui.fullscreen") + ": " + (novoEstado ? "ON" : "OFF");
    }

    private void Recomecar()
    {
        Time.timeScale = 1f;
        if (bgSnapshot != null) Destroy(bgSnapshot);
        LimparManagersPersistentes();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void IrParaSelecao()
    {
        Time.timeScale = 1f;
        if (bgSnapshot != null) Destroy(bgSnapshot);
        LimparManagersPersistentes();
        SceneManager.LoadScene("CharacterSelection");
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
