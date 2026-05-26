using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// Adicione este script a um GameObject vazio na cena CharacterSelection.
public class CharacterSelectionUI : MonoBehaviour
{
    [Header("Personagens disponíveis")]
    public CharacterData[] characters;

    [Header("Configurações")]
    public int numSlots = 6;

    // ── Refs do manager ────────────────────────────────────────────────
    CharacterSelectionManagerIntegrated manager;
    Canvas canvas;

    TextMeshProUGUI txtNome, txtElemento, txtDesc, txtBonus, txtMoedas;
    TextMeshProUGUI txtUltimateInfo, txtPassivasInfo, txtNomePreview;
    Slider[]         sliders         = new Slider[4];
    Button[]         upgradeButtons  = new Button[4];
    TextMeshProUGUI[] upgradeLevelTexts = new TextMeshProUGUI[4];
    CharacterIconUI[] iconesArray;
    GameObject[]     painelAbaInfo  = new GameObject[3];
    Button[]         botoesAbaInfo  = new Button[3];

    // ── Preview com RenderTexture ──────────────────────────────────────
    RawImage         previewRawImage;
    RenderTexture    previewRT;
    Camera           previewCamera;
    GameObject       previewPersonagem;
    static readonly Vector3 PREVIEW_POS = new Vector3(9999f, 0f, 0f);

    // ── Container de botões de seleção de ultimate / passiva ─────────
    GameObject painelSeleçaoUltimate;
    GameObject painelSeleçaoPassiva;

    // ── Refs de animação ───────────────────────────────────────────────
    Image   glowFundo;
    Image   glowPreview;
    GameObject painelPreview;

    // partículas de fundo
    const int QTD_P = 16;
    RectTransform[] pRT   = new RectTransform[QTD_P];
    Image[]         pImg  = new Image[QTD_P];
    Vector2[]       pOrig = new Vector2[QTD_P];
    float[]         pFase = new float[QTD_P];
    float[]         pVel  = new float[QTD_P];

    // partículas do preview (sobem ao redor do personagem)
    const int QTD_PP = 14;
    RectTransform[] ppRT  = new RectTransform[QTD_PP];
    Image[]         ppImg = new Image[QTD_PP];
    float[]         ppT   = new float[QTD_PP];
    float[]         ppDur = new float[QTD_PP];
    float[]         ppX   = new float[QTD_PP];
    float[]         ppDx  = new float[QTD_PP];

    Color corAtualGlow = new Color(0.55f, 0.08f, 0.08f);

    [Header("Sprites Dark Fantasy")]
    public Sprite spriteFundo;
    public Sprite spritePainel;
    public Sprite spriteMolduraPreview;
    public Sprite spriteBarraTopo;
    public Sprite spriteBotao;
    public Sprite[] spriteStatIcons; // [0]=vida [1]=atk [2]=def [3]=vel

    // ── Paleta dark fantasy ────────────────────────────────────────────
    static readonly Color corFundo  = new Color(0.03f, 0.01f, 0.01f);  // #080303
    static readonly Color corPainel = new Color(0.07f, 0.03f, 0.03f);  // #120808
    static readonly Color corAcento = new Color(0.55f, 0.08f, 0.08f);  // #8C1414 carmesim

    // ──────────────────────────────────────────────────────────────────
    void Start()
    {
        // Remove qualquer canvas pré-criado pelo editor tool
        var antigo = GameObject.Find("CanvasPrincipal");
        if (antigo != null) Destroy(antigo);

        CriarCanvas();
        CriarUI();
        ConectarManager();
        StartCoroutine(AnimarParticulas());
        StartCoroutine(AnimarGlow());
        StartCoroutine(AnimarParticulasPreview());
    }

    // ── Canvas ─────────────────────────────────────────────────────────
    void CriarCanvas()
    {
        var canvasGO = new GameObject("CanvasPrincipal");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;
        var cs = canvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
        manager = canvasGO.AddComponent<CharacterSelectionManagerIntegrated>();
        manager.characters = characters;
    }

    // ── UI principal ───────────────────────────────────────────────────
    void CriarUI()
    {
        var root = canvas.gameObject;

        CriarFundo(root);
        CriarParticulas(root);
        CriarTitulo(root);
        CriarPainelIcones(root);
        CriarAreaCentral(root);
        CriarRodape(root);
    }

    // ── Fundo com glow dinâmico ────────────────────────────────────────
    void CriarFundo(GameObject root)
    {
        var fundoGO = Img(root, "Fundo", Vector2.zero, Vector2.one, corFundo);
        if (spriteFundo != null)
        {
            var img = fundoGO.GetComponent<Image>();
            img.sprite = spriteFundo;
            img.type   = Image.Type.Simple;
            img.color  = Color.white;
        }

        // glow central (muda de cor com o elemento)
        var g = Img(root, "GlowFundo", new Vector2(0.15f,0.15f), new Vector2(0.85f,0.85f),
            new Color(corAcento.r, corAcento.g, corAcento.b, 0.07f));
        glowFundo = g.GetComponent<Image>();

        // faixa topo
        Img(root, "FaixaTopo", new Vector2(0f,0.92f), Vector2.one,
            new Color(corAcento.r, corAcento.g, corAcento.b, 0.12f));

        // faixa rodapé
        Img(root, "FaixaBot", Vector2.zero, new Vector2(1f, 0.08f),
            new Color(0f, 0f, 0f, 0.50f));
    }

    // ── Partículas flutuantes ──────────────────────────────────────────
    void CriarParticulas(GameObject root)
    {
        for (int i = 0; i < QTD_P; i++)
        {
            float sz = Random.Range(3f, 10f);
            var go   = Img(root, $"P{i}", Vector2.zero, Vector2.zero,
                new Color(corAcento.r, corAcento.g, corAcento.b, 0.12f));
            var rt   = go.GetComponent<RectTransform>();
            Vector2 pos = new Vector2(Random.Range(0f,1f), Random.Range(0.08f,0.92f));
            rt.anchorMin = rt.anchorMax = pos;
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(sz, sz);
            pRT[i]   = rt;
            pImg[i]  = go.GetComponent<Image>();
            pOrig[i] = pos;
            pFase[i] = Random.Range(0f, Mathf.PI * 2f);
            pVel[i]  = Random.Range(0.2f, 0.7f);
        }
    }

    // ── Título animado ─────────────────────────────────────────────────
    void CriarTitulo(GameObject root)
    {
        var linha = Img(root, "LinhaT",
            new Vector2(0.02f, 0.905f), new Vector2(0.98f, 0.905f),
            new Color(corAcento.r, corAcento.g, corAcento.b, 0.5f));
        linha.GetComponent<RectTransform>().offsetMax = new Vector2(0f, 2f);

        var t = TMP(root, "Titulo",
            new Vector2(0f,0.93f), new Vector2(1f, 1f),
            "SELECAO DE PERSONAGEM", 26f, FontStyles.Bold, Color.white);
        t.alignment = TextAlignmentOptions.Center;

        StartCoroutine(PulsarTitulo(t));
    }

    // ── Faixa de ícones ────────────────────────────────────────────────
    void CriarPainelIcones(GameObject root)
    {
        var painel = Img(root, "PainelIcones",
            new Vector2(0f, 0.78f), new Vector2(1f, 0.93f),
            corPainel);
        if (spriteBarraTopo != null)
        {
            var img = painel.GetComponent<Image>();
            img.sprite = spriteBarraTopo;
            img.type   = Image.Type.Sliced;
            img.color  = Color.white;
        }
        var rt = painel.GetComponent<RectTransform>();
        rt.offsetMin = new Vector2(8f, 0f); rt.offsetMax = new Vector2(-8f, 0f);

        var hlg = painel.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment       = TextAnchor.MiddleCenter;
        hlg.spacing              = 12f;
        hlg.padding              = new RectOffset(12, 12, 6, 6);
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;

        int slots = Mathf.Max(numSlots, characters != null ? characters.Length : 1);
        iconesArray = new CharacterIconUI[slots];
        for (int i = 0; i < slots; i++)
            iconesArray[i] = CriarIcone(painel, i);
    }

    CharacterIconUI CriarIcone(GameObject parent, int index)
    {
        var go = new GameObject($"Icone_{index}");
        go.transform.SetParent(parent.transform, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth  = 110f;
        le.preferredHeight = 110f;

        var bgImg = go.AddComponent<Image>();
        if (spritePainel != null)
        {
            bgImg.sprite = spritePainel;
            bgImg.type   = Image.Type.Sliced;
            bgImg.color  = new Color(0.6f, 0.55f, 0.55f);
        }
        else
        {
            bgImg.color = new Color(0.05f, 0.02f, 0.02f);
        }
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = bgImg;

        var iconUI = go.AddComponent<CharacterIconUI>();

        // ícone sprite
        var iconGO  = new GameObject("Icon");
        iconGO.transform.SetParent(go.transform, false);
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.color = new Color(0.5f,0.5f,0.5f,0.4f);
        Anchors(iconGO, new Vector2(0.08f,0.28f), new Vector2(0.92f,0.94f));
        iconUI.characterIcon = iconImg;

        // nome
        iconUI.characterName = TMP(go, "Nome",
            new Vector2(0f,0f), new Vector2(1f,0.28f),
            "", 10f, FontStyles.Bold, Color.white);
        iconUI.characterName.alignment = TextAlignmentOptions.Center;

        // elemento
        iconUI.elementIconText = TMP(go, "Elem",
            new Vector2(0f,0.84f), new Vector2(1f,1f),
            "", 13f, FontStyles.Normal, Color.yellow);
        iconUI.elementIconText.alignment = TextAlignmentOptions.Center;

        // bg elemento
        var elemBg = new GameObject("ElemBG");
        elemBg.transform.SetParent(go.transform, false);
        iconUI.elementBackground = elemBg.AddComponent<Image>();
        iconUI.elementBackground.color = new Color(1f,1f,1f,0f);
        Anchors(elemBg, Vector2.zero, Vector2.one);

        // indicador selecionado (borda colorida)
        var sel = new GameObject("Sel");
        sel.transform.SetParent(go.transform, false);
        var rs = sel.AddComponent<RectTransform>();
        rs.anchorMin = Vector2.zero; rs.anchorMax = Vector2.one;
        rs.offsetMin = new Vector2(-3f,-3f); rs.offsetMax = new Vector2(3f,3f);
        sel.AddComponent<Image>().color = new Color(corAcento.r, corAcento.g, corAcento.b, 0.75f);
        sel.SetActive(false);
        iconUI.selectedIndicator = sel;

        return iconUI;
    }

    // ── Área central: preview + info + status ──────────────────────────
    void CriarAreaCentral(GameObject root)
    {
        // ── preview (centro) ──
        painelPreview = new GameObject("Preview");
        painelPreview.transform.SetParent(root.transform, false);
        painelPreview.AddComponent<RectTransform>(); // cria RectTransform antes de Anchors
        Anchors(painelPreview, new Vector2(0.32f,0.10f), new Vector2(0.68f,0.78f));

        // glow atrás do preview
        var glowGO = Img(painelPreview, "GlowPreview",
            new Vector2(-0.2f,-0.1f), new Vector2(1.2f,1.1f),
            new Color(corAcento.r, corAcento.g, corAcento.b, 0.10f));
        glowPreview = glowGO.GetComponent<Image>();

        // RawImage que mostra o personagem via RenderTexture
        var prevGO = new GameObject("PrevRender");
        prevGO.transform.SetParent(painelPreview.transform, false);
        previewRawImage = prevGO.AddComponent<RawImage>();
        previewRawImage.color = Color.white;
        Anchors(prevGO, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.88f));
        ConfigurarCameraPreview();
        CriarParticulasPreview(painelPreview);

        if (spriteMolduraPreview != null)
        {
            // Moldura posicionada exatamente sobre o campo preto do personagem
            var molduraGO  = Img(painelPreview, "MolduraPreview",
                new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.92f), Color.white);
            var molduraImg = molduraGO.GetComponent<Image>();
            molduraImg.sprite     = spriteMolduraPreview;
            molduraImg.type       = Image.Type.Sliced;
            molduraImg.fillCenter = false;
            molduraImg.color      = Color.white;
            molduraGO.transform.SetAsLastSibling();
        }

        // nome grande no preview
        txtNomePreview = TMP(painelPreview, "NomePrev",
            new Vector2(0f,0.88f), new Vector2(1f,1f),
            "", 18f, FontStyles.Bold, Color.white);
        txtNomePreview.alignment = TextAlignmentOptions.Center;

        // ── info (esquerda) ──
        var info = Img(root, "PainelInfo",
            new Vector2(0.01f,0.10f), new Vector2(0.31f,0.78f),
            corPainel);
        if (spritePainel != null)
        {
            var img = info.GetComponent<Image>();
            img.sprite = spritePainel;
            img.type   = Image.Type.Sliced;
            img.color  = Color.white;
        }
        BarraTopo(info, corAcento);

        var lblInfo = TMP(info, "LblInfo",
            new Vector2(0f,0.93f), new Vector2(1f,1f),
            "PERSONAGEM", 12f, FontStyles.Bold,
            new Color(0.88f,0.80f,0.72f));
        lblInfo.alignment = TextAlignmentOptions.Center;

        txtNome = TMP(info, "Nome",
            new Vector2(0f,0.81f), new Vector2(1f,0.93f),
            "—", 20f, FontStyles.Bold, Color.white);
        txtNome.alignment = TextAlignmentOptions.Center;

        txtElemento = TMP(info, "Elemento",
            new Vector2(0f,0.71f), new Vector2(1f,0.81f),
            "—", 14f, FontStyles.Normal, Color.yellow);
        txtElemento.alignment = TextAlignmentOptions.Center;

        // linha divisória
        var ln = Img(info, "Ln",
            new Vector2(0.05f,0.705f), new Vector2(0.95f,0.705f),
            new Color(1f,1f,1f,0.08f));
        ln.GetComponent<RectTransform>().offsetMax = new Vector2(0f,1f);

        // Abas INFO / ULTIMATE / PASSIVAS
        CriarAbasInfo(info);

        // ── status (direita) ──
        var status = Img(root, "PainelStatus",
            new Vector2(0.69f,0.10f), new Vector2(0.99f,0.78f),
            corPainel);
        if (spritePainel != null)
        {
            var img = status.GetComponent<Image>();
            img.sprite = spritePainel;
            img.type   = Image.Type.Sliced;
            img.color  = Color.white;
        }
        BarraTopo(status, corAcento);

        var lblSt = TMP(status, "LblSt",
            new Vector2(0f,0.92f), new Vector2(1f,1f),
            "STATUS", 12f, FontStyles.Bold, new Color(0.88f,0.80f,0.72f));
        lblSt.alignment = TextAlignmentOptions.Center;

        string[] labels = { "VIDA", "ATK", "DEF", "VEL" };
        Color[]  cores  = {
            new Color(0.9f,0.3f,0.3f),
            new Color(1.0f,0.6f,0.1f),
            new Color(0.3f,0.6f,1.0f),
            new Color(0.3f,0.9f,0.5f),
        };

        for (int i = 0; i < 4; i++)
        {
            float yMax = 0.90f - i * 0.22f;
            float yMin = yMax  - 0.18f;

            var linha = new GameObject($"Linha_{i}");
            linha.transform.SetParent(status.transform, false);
            // Padded para não ultrapassar a moldura do painel (8% de cada lado)
            Anchors(linha, new Vector2(0.08f, yMin), new Vector2(0.82f, yMax));

            var linhaFundo = linha.AddComponent<Image>();
            linhaFundo.color = new Color(0f, 0f, 0f, 0f);

            var indGO = new GameObject("Ind");
            indGO.transform.SetParent(linha.transform, false);
            Anchors(indGO, new Vector2(0.03f, 0.06f), new Vector2(0.30f, 0.94f));
            var indImg = indGO.AddComponent<Image>();
            if (spriteStatIcons != null && i < spriteStatIcons.Length && spriteStatIcons[i] != null)
            {
                indImg.sprite = spriteStatIcons[i];
                indImg.color  = Color.white;
                indImg.preserveAspect = true;
            }
            else { indImg.color = cores[i]; }

            // label
            var lbl = TMP(linha, "Lbl",
                new Vector2(0.33f, 0.48f), new Vector2(0.90f, 1.0f),
                labels[i], 8f, FontStyles.Bold, cores[i]);
            lbl.alignment = TextAlignmentOptions.MidlineLeft;

            // slider
            var slGO = new GameObject("Slider");
            slGO.transform.SetParent(linha.transform, false);
            Anchors(slGO, new Vector2(0.33f, 0.05f), new Vector2(0.72f, 0.48f));
            sliders[i] = CriarSlider(slGO, cores[i]);

            // nível
            upgradeLevelTexts[i] = TMP(status, "Nv",
                new Vector2(0.81f, yMin + 0.02f), new Vector2(0.93f, yMax - 0.02f),
                "Nv.0", 8f, FontStyles.Normal, new Color(0.65f,0.55f,0.35f));
            upgradeLevelTexts[i].alignment = TextAlignmentOptions.Center;

            int cap = i;
            var btnGO = new GameObject("BtnUp");
            btnGO.transform.SetParent(status.transform, false);
            Anchors(btnGO, new Vector2(0.80f, yMin + 0.01f), new Vector2(0.97f, yMax - 0.01f));
            var bImg = btnGO.AddComponent<Image>();
            if (spriteBotao != null)
            {
                bImg.sprite = spriteBotao;
                bImg.type   = Image.Type.Sliced;
                bImg.color  = new Color(0.20f, 0.50f, 0.20f);
            }
            else
            {
                bImg.color = new Color(0.15f, 0.45f, 0.15f);
            }
            var b = btnGO.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.targetGraphic = bImg;
            b.onClick.AddListener(() => manager?.BuyUpgrade(cap));
            TMP(btnGO, "T",
                Vector2.zero, Vector2.one,
                "+", 14f, FontStyles.Bold, Color.white)
                .alignment = TextAlignmentOptions.Center;
            upgradeButtons[i] = b;
        }
    }

    // ── Rodapé ─────────────────────────────────────────────────────────
    void CriarRodape(GameObject root)
    {
        var rodape = new GameObject("Rodape");
        rodape.transform.SetParent(root.transform, false);
        Anchors(rodape, new Vector2(0f,0f), new Vector2(1f,0.10f),
                new Vector2(8f,6f), new Vector2(-8f,-6f));

        txtMoedas = TMP(rodape, "Moedas",
            new Vector2(0f,0f), new Vector2(0.22f,1f),
            "💰 0", 20f, FontStyles.Bold, Color.yellow);
        txtMoedas.alignment = TextAlignmentOptions.MidlineLeft;

        CriarBotao(rodape, "BtnVoltar",
            new Vector2(0f,0.08f), new Vector2(0.18f,0.92f),
            "< VOLTAR", new Color(0.45f,0.08f,0.08f), () =>
            {
                if (GameSceneManager.Instance != null)
                    GameSceneManager.Instance.GoToMainMenu();
                else
                    UnityEngine.SceneManagement.SceneManager.LoadScene("menu_inicial");
            });

        // botão MISSÕES
        CriarBotao(rodape, "BtnMissoes",
            new Vector2(0.40f,0.08f), new Vector2(0.60f,0.92f),
            "MISSOES", new Color(0.42f,0.32f,0.18f), () => { });

        CriarBotao(rodape, "BtnJogar",
            new Vector2(0.72f,0.04f), new Vector2(1f,0.96f),
            "JOGAR >", new Color(0.10f,0.50f,0.15f), () =>
            {
                if (GameSceneManager.Instance != null)
                    GameSceneManager.Instance.StartGameplay();
                else
                    UnityEngine.SceneManagement.SceneManager.LoadScene("escolher terreno");
            }, 24f);
    }

    // ── Abas do painel de info ─────────────────────────────────────────
    void CriarAbasInfo(GameObject info)
    {
        string[] nomes  = { "INFO", "ULTIMATE", "PASSIVAS" };
        Color corAtiva   = new Color(0.55f, 0.10f, 0.10f);
        Color corInativa = new Color(0.18f, 0.07f, 0.07f);
        float tabPadX = 0.07f;
        float tabW    = (1f - 2f * tabPadX) / 3f;

        for (int i = 0; i < 3; i++)
        {
            float xMin = tabPadX + i * tabW;
            float xMax = tabPadX + (i + 1) * tabW;
            int idx = i;

            var tabGO  = new GameObject($"Tab_{nomes[i]}");
            tabGO.transform.SetParent(info.transform, false);
            var tabImg = tabGO.AddComponent<Image>();
            if (spriteBotao != null)
            {
                tabImg.sprite = spriteBotao;
                tabImg.type   = Image.Type.Sliced;
            }
            tabImg.color = i == 0 ? corAtiva : corInativa;
            var tabRT  = tabGO.GetComponent<RectTransform>();
            tabRT.anchorMin = new Vector2(xMin, 0.63f);
            tabRT.anchorMax = new Vector2(xMax, 0.71f);
            tabRT.offsetMin = new Vector2(i > 0 ? 2f : 0f, 0f);
            tabRT.offsetMax = new Vector2(i < 2 ? -2f : 0f, 0f);
            var btn = tabGO.AddComponent<Button>();
            btn.targetGraphic = tabImg;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => MostrarAbaInfo(idx));
            TMP(tabGO, "T", Vector2.zero, Vector2.one,
                nomes[i], 9f, FontStyles.Bold, Color.white).alignment = TextAlignmentOptions.Center;
            botoesAbaInfo[i] = btn;
        }

        // ── Painel INFO ──────────────────────────────────────────────
        painelAbaInfo[0] = new GameObject("ConteudoInfo");
        painelAbaInfo[0].transform.SetParent(info.transform, false);
        painelAbaInfo[0].AddComponent<RectTransform>();
        Anchors(painelAbaInfo[0], Vector2.zero, new Vector2(1f, 0.62f));

        txtDesc = TMP(painelAbaInfo[0], "Desc",
            new Vector2(0.04f, 0.26f), new Vector2(0.96f, 0.96f),
            "—", 11f, FontStyles.Normal, new Color(0.88f, 0.82f, 0.70f));
        txtDesc.enableWordWrapping = true;
        txtDesc.alignment = TextAlignmentOptions.Top;

        txtBonus = TMP(painelAbaInfo[0], "Bonus",
            new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.24f),
            "—", 10f, FontStyles.Normal, new Color(0.5f, 1f, 0.6f));
        txtBonus.enableWordWrapping = true;
        txtBonus.alignment = TextAlignmentOptions.Center;

        // ── Painel ULTIMATE ──────────────────────────────────────────
        painelAbaInfo[1] = new GameObject("ConteudoUltimate");
        painelAbaInfo[1].transform.SetParent(info.transform, false);
        painelAbaInfo[1].AddComponent<RectTransform>();
        Anchors(painelAbaInfo[1], Vector2.zero, new Vector2(1f, 0.62f));

        // ScrollRect para lista de ultimates
        var scrollRoot = new GameObject("ScrollUltimates");
        scrollRoot.transform.SetParent(painelAbaInfo[1].transform, false);
        scrollRoot.AddComponent<RectTransform>();
        Anchors(scrollRoot, new Vector2(0f, 0.01f), new Vector2(1f, 0.98f));

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollRoot.transform, false);
        viewport.AddComponent<Image>().color = Color.clear;
        viewport.AddComponent<RectMask2D>();
        Anchors(viewport, Vector2.zero, Vector2.one);

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRT      = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot     = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta        = Vector2.zero;

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                = 5f;
        vlg.padding                = new RectOffset(4, 4, 4, 4);
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment         = TextAnchor.UpperCenter;

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var sr = scrollRoot.AddComponent<ScrollRect>();
        sr.content          = contentRT;
        sr.viewport         = viewport.GetComponent<RectTransform>();
        sr.horizontal       = false;
        sr.vertical         = true;
        sr.scrollSensitivity = 25f;
        sr.movementType     = ScrollRect.MovementType.Clamped;

        painelSeleçaoUltimate = content;

        // ── Painel PASSIVAS ──────────────────────────────────────────
        painelAbaInfo[2] = new GameObject("ConteudoPassivas");
        painelAbaInfo[2].transform.SetParent(info.transform, false);
        painelAbaInfo[2].AddComponent<RectTransform>();
        Anchors(painelAbaInfo[2], Vector2.zero, new Vector2(1f, 0.62f));

        // ScrollRect para lista de passivas (igual ao de ultimates)
        var scrollPassiva = new GameObject("ScrollPassivas");
        scrollPassiva.transform.SetParent(painelAbaInfo[2].transform, false);
        scrollPassiva.AddComponent<RectTransform>();
        Anchors(scrollPassiva, new Vector2(0f, 0.01f), new Vector2(1f, 0.98f));

        var vpPassiva = new GameObject("Viewport");
        vpPassiva.transform.SetParent(scrollPassiva.transform, false);
        vpPassiva.AddComponent<Image>().color = Color.clear;
        vpPassiva.AddComponent<RectMask2D>();
        Anchors(vpPassiva, Vector2.zero, Vector2.one);

        var contentPassiva = new GameObject("Content");
        contentPassiva.transform.SetParent(vpPassiva.transform, false);
        var cpRT        = contentPassiva.AddComponent<RectTransform>();
        cpRT.anchorMin  = new Vector2(0f, 1f);
        cpRT.anchorMax  = new Vector2(1f, 1f);
        cpRT.pivot      = new Vector2(0.5f, 1f);
        cpRT.anchoredPosition = Vector2.zero;
        cpRT.sizeDelta        = Vector2.zero;

        var vlgP = contentPassiva.AddComponent<VerticalLayoutGroup>();
        vlgP.spacing               = 5f;
        vlgP.padding               = new RectOffset(4, 4, 4, 4);
        vlgP.childForceExpandWidth = true;
        vlgP.childForceExpandHeight = false;
        vlgP.childAlignment        = TextAnchor.UpperCenter;

        var csfP = contentPassiva.AddComponent<ContentSizeFitter>();
        csfP.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var srP = scrollPassiva.AddComponent<ScrollRect>();
        srP.content          = cpRT;
        srP.viewport         = vpPassiva.GetComponent<RectTransform>();
        srP.horizontal       = false;
        srP.vertical         = true;
        srP.scrollSensitivity = 25f;
        srP.movementType     = ScrollRect.MovementType.Clamped;

        painelSeleçaoPassiva = contentPassiva;

        // txtPassivasInfo mantido para compatibilidade (não visível mas ainda referenciado)
        txtPassivasInfo = TMP(painelAbaInfo[2], "PassInfo",
            new Vector2(0f, 0f), new Vector2(0f, 0f),
            "", 10.5f, FontStyles.Normal, new Color(0.75f, 0.90f, 0.75f));
        txtPassivasInfo.enableWordWrapping = true;
        txtPassivasInfo.gameObject.SetActive(false);

        MostrarAbaInfo(0);
    }

    void MostrarAbaInfo(int idx)
    {
        Color corAtiva   = new Color(0.55f, 0.10f, 0.10f);
        Color corInativa = new Color(0.18f, 0.07f, 0.07f);
        for (int i = 0; i < 3; i++)
        {
            if (painelAbaInfo[i] != null) painelAbaInfo[i].SetActive(i == idx);
            if (botoesAbaInfo[i] != null)
                botoesAbaInfo[i].GetComponent<Image>().color = i == idx ? corAtiva : corInativa;
        }
    }

    // ── Animações ──────────────────────────────────────────────────────
    IEnumerator PulsarTitulo(TextMeshProUGUI t)
    {
        float tempo = 0f;
        while (t != null)
        {
            tempo += Time.deltaTime * 1.2f;
            float brilho = 0.88f + Mathf.Sin(tempo) * 0.12f;
            t.color = new Color(brilho, brilho, brilho, 1f);
            yield return null;
        }
    }

    IEnumerator AnimarParticulas()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime;
            for (int i = 0; i < QTD_P; i++)
            {
                if (pRT[i] == null) yield break;
                float ox = pOrig[i].x + Mathf.Sin(t * pVel[i] + pFase[i]) * 0.022f;
                float oy = pOrig[i].y + Mathf.Cos(t * pVel[i] * 0.7f + pFase[i]) * 0.028f;
                pRT[i].anchorMin = pRT[i].anchorMax = new Vector2(ox, oy);

                // cor segue o glow atual
                float a = Mathf.Abs(Mathf.Sin(t * pVel[i] + pFase[i])) * 0.18f + 0.03f;
                pImg[i].color = new Color(corAtualGlow.r, corAtualGlow.g, corAtualGlow.b, a);
            }
            yield return null;
        }
    }

    IEnumerator AnimarGlow()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * 0.8f;
            float a = 0.06f + Mathf.Sin(t) * 0.03f;
            if (glowFundo  != null) glowFundo.color  = new Color(corAtualGlow.r, corAtualGlow.g, corAtualGlow.b, a);
            if (glowPreview != null) glowPreview.color = new Color(corAtualGlow.r, corAtualGlow.g, corAtualGlow.b, a * 1.5f);
            yield return null;
        }
    }

    // Chamado pelo manager quando o personagem muda
    public void AtualizarCorElemento(Color cor)
    {
        corAtualGlow = cor;
        StartCoroutine(TransicaoCor(cor));
    }

    IEnumerator TransicaoCor(Color alvo)
    {
        Color inicio = corAtualGlow;
        float dur = 0.4f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            corAtualGlow = Color.Lerp(inicio, alvo, t / dur);
            yield return null;
        }
        corAtualGlow = alvo;
    }

    public IEnumerator AnimarBarras(float[] valores)
    {
        float dur = 0.45f;
        float[] origens = new float[4];
        for (int i = 0; i < 4; i++)
            origens[i] = sliders[i] != null ? sliders[i].value : 0f;

        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float ease = 1f - Mathf.Pow(1f - t / dur, 3f);
            for (int i = 0; i < 4; i++)
                if (sliders[i] != null)
                    sliders[i].value = Mathf.Lerp(origens[i], valores[i], ease);
            yield return null;
        }
        for (int i = 0; i < 4; i++)
            if (sliders[i] != null)
                sliders[i].value = valores[i];
    }

    // ── Manager connection ──────────────────────────────────────────────
    void ConectarManager()
    {
        manager.characterNameText        = txtNome;
        manager.characterElementText     = txtElemento;
        manager.characterDescriptionText = txtDesc;
        manager.elementBonusText         = txtBonus;
        manager.coinsText                = txtMoedas;
        manager.statusSliders            = sliders;
        manager.upgradeButtons           = upgradeButtons;
        manager.upgradeLevelTexts        = upgradeLevelTexts;
        manager.characterIcons           = iconesArray;
        manager.ultimateInfoText         = txtUltimateInfo;
        manager.passivasInfoText         = txtPassivasInfo;
        manager.painelUltimates          = painelSeleçaoUltimate;
        manager.painelPassivas           = painelSeleçaoPassiva;
        manager.characterPreviewName     = txtNomePreview;
        manager.selectionUI              = this;

        var stagePlaceholder = new GameObject("PainelStages");
        stagePlaceholder.transform.SetParent(canvas.transform, false);
        stagePlaceholder.AddComponent<RectTransform>();
        stagePlaceholder.SetActive(false);
        manager.painelStages = stagePlaceholder;
    }

    // ── Preview com RenderTexture ──────────────────────────────────────
    void ConfigurarCameraPreview()
    {
        previewRT = new RenderTexture(512, 768, 16);
        previewRT.antiAliasing = 2;
        previewRawImage.texture = previewRT;

        var camGO = new GameObject("PreviewCamera");
        previewCamera = camGO.AddComponent<Camera>();
        previewCamera.orthographic     = true;
        previewCamera.orthographicSize = 3.5f;
        previewCamera.clearFlags       = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor  = corFundo;
        previewCamera.targetTexture    = previewRT;
        previewCamera.transform.position = PREVIEW_POS + new Vector3(0f, 0f, -10f);
        previewCamera.depth = -5;
    }

    public void AtualizarPreviewPrefab(CharacterData data)
    {
        if (previewPersonagem != null) Destroy(previewPersonagem);
        if (data == null || data.characterPrefab == null) return;

        previewPersonagem = Instantiate(data.characterPrefab, PREVIEW_POS, Quaternion.identity);

        // Desativa físicas e scripts, mantém Renderer e Animator
        foreach (var rb  in previewPersonagem.GetComponentsInChildren<Rigidbody2D>())
            rb.simulated = false;
        foreach (var col in previewPersonagem.GetComponentsInChildren<Collider2D>())
            col.enabled = false;
        foreach (var mb  in previewPersonagem.GetComponentsInChildren<MonoBehaviour>())
        {
            if (mb is Animator) continue;
            mb.enabled = false;
        }

        // Aplica cor do personagem
        foreach (var sr in previewPersonagem.GetComponentsInChildren<SpriteRenderer>())
            sr.color = data.characterColor;

        AdicionarSombraBlob(previewPersonagem);
    }

    void AdicionarSombraBlob(GameObject personagem)
    {
        // Sombra elíptica forte nos pés — gradiente com borda dura no centro e suave nas bordas
        int sz = 128;
        var tex    = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        var pixels = new Color[sz * sz];
        var centro = new Vector2(sz * 0.5f, sz * 0.5f);
        float raio = sz * 0.5f;

        for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), centro) / raio;
                // Centro opaco, borda suave — curva acelerada para base mais sólida
                float a = Mathf.Clamp01(1f - d);
                a = Mathf.Pow(a, 1.1f) * 0.92f;
                pixels[y * sz + x] = new Color(0f, 0f, 0.02f, a);
            }
        tex.SetPixels(pixels);
        tex.Apply();

        var sombra = new GameObject("BlobShadow");
        sombra.transform.SetParent(personagem.transform, false);
        sombra.transform.localPosition = new Vector3(0f, -0.73f, 0.1f);
        sombra.transform.localScale    = new Vector3(2.2f, 0.38f, 1f);

        var sr = sombra.AddComponent<SpriteRenderer>();
        sr.sprite       = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
        sr.sortingOrder = -1;
        sr.color        = Color.white;
    }

    void CriarParticulasPreview(GameObject painel)
    {
        for (int i = 0; i < QTD_PP; i++)
        {
            float sz = Random.Range(2f, 5.5f);
            var go = new GameObject($"PP{i}");
            go.transform.SetParent(painel.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(sz, sz);
            rt.pivot = new Vector2(0.5f, 0.5f);

            ppX[i]   = Random.Range(0.08f, 0.92f);
            float startY = Random.Range(0.05f, 0.4f);
            rt.anchorMin = rt.anchorMax = new Vector2(ppX[i], startY);
            rt.anchoredPosition = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = new Color(corAcento.r, corAcento.g, corAcento.b, 0f);

            ppRT[i]  = rt;
            ppImg[i] = img;
            ppT[i]   = Random.Range(0f, 1f);
            ppDur[i] = Random.Range(2.8f, 5.5f);
            ppDx[i]  = Random.Range(-0.05f, 0.05f);
        }
    }

    IEnumerator AnimarParticulasPreview()
    {
        while (true)
        {
            for (int i = 0; i < QTD_PP; i++)
            {
                if (ppRT[i] == null) yield break;

                ppT[i] += Time.deltaTime / ppDur[i];
                if (ppT[i] >= 1f)
                {
                    ppT[i]   = 0f;
                    ppX[i]   = Random.Range(0.08f, 0.92f);
                    ppDur[i] = Random.Range(2.8f, 5.5f);
                    ppDx[i]  = Random.Range(-0.05f, 0.05f);
                }

                float t = ppT[i];
                float y = Mathf.Lerp(0.06f, 0.90f, t);
                float x = ppX[i] + ppDx[i] * t
                          + Mathf.Sin(t * 5f + ppX[i] * 8f) * 0.018f;

                ppRT[i].anchorMin = ppRT[i].anchorMax = new Vector2(x, y);
                ppRT[i].anchoredPosition = Vector2.zero;

                // fade in → peak → fade out ao longo do ciclo
                float alpha = Mathf.Sin(t * Mathf.PI) * 0.20f;
                ppImg[i].color = new Color(corAtualGlow.r, corAtualGlow.g,
                                           corAtualGlow.b, alpha);
            }
            yield return null;
        }
    }

    void OnDestroy()
    {
        if (previewPersonagem != null) Destroy(previewPersonagem);
        if (previewCamera    != null) Destroy(previewCamera.gameObject);
        if (previewRT        != null) { previewRT.Release(); Destroy(previewRT); }
    }

    // ── Helpers ────────────────────────────────────────────────────────
    Slider CriarSlider(GameObject go, Color corFill)
    {
        go.AddComponent<Image>().color = new Color(0.15f, 0.08f, 0.08f);

        var fillArea = new GameObject("FA"); fillArea.transform.SetParent(go.transform, false);
        fillArea.AddComponent<Image>().color = new Color(0f,0f,0f,0f); // cria RectTransform
        Anchors(fillArea, new Vector2(0f,0.15f), new Vector2(1f,0.85f),
                new Vector2(3f,0f), new Vector2(-3f,0f));

        var fill = new GameObject("Fi"); fill.transform.SetParent(fillArea.transform, false);
        fill.AddComponent<Image>().color = corFill; // cria RectTransform
        var rf = fill.GetComponent<RectTransform>();
        rf.anchorMin = Vector2.zero; rf.anchorMax = new Vector2(0f,1f);
        rf.sizeDelta = new Vector2(6f,0f);

        var s = go.AddComponent<Slider>();
        s.fillRect   = rf;
        s.direction  = Slider.Direction.LeftToRight;
        s.minValue   = 0f; s.maxValue = 1f; s.value = 0f;
        s.interactable = false;
        return s;
    }

    void CriarBotao(GameObject parent, string nome,
        Vector2 ancMin, Vector2 ancMax,
        string texto, Color cor, UnityEngine.Events.UnityAction acao,
        float fontSize = 18f)
    {
        var go  = new GameObject(nome);
        go.transform.SetParent(parent.transform, false);
        Anchors(go, ancMin, ancMax);
        var img = go.AddComponent<Image>();
        if (spriteBotao != null)
        {
            img.sprite = spriteBotao;
            img.type   = Image.Type.Sliced;
            // multiplicar cor pela textura de pedra (cor escura = pedra escura, verde = pedra verde)
            img.color  = new Color(
                Mathf.Clamp01(cor.r + 0.45f),
                Mathf.Clamp01(cor.g + 0.45f),
                Mathf.Clamp01(cor.b + 0.45f));
        }
        else
        {
            img.color = cor;
        }
        var btn = go.AddComponent<Button>(); btn.targetGraphic = img;
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(acao);
        TMP(go, "T", Vector2.zero, Vector2.one,
            texto, fontSize, FontStyles.Bold, Color.white)
            .alignment = TextAlignmentOptions.Center;
    }

    void BarraTopo(GameObject pai, Color cor)
    {
        var b = new GameObject("BT"); b.transform.SetParent(pai.transform, false);
        b.AddComponent<Image>().color = cor; // Image cria RectTransform
        var rb = b.GetComponent<RectTransform>();
        rb.anchorMin = new Vector2(0f,1f); rb.anchorMax = Vector2.one;
        rb.offsetMin = Vector2.zero; rb.offsetMax = new Vector2(0f,4f);
    }

    GameObject Img(GameObject parent, string nome,
        Vector2 ancMin, Vector2 ancMax, Color cor)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<Image>().color = cor; // Image cria RectTransform automaticamente
        Anchors(go, ancMin, ancMax);
        return go;
    }

    TextMeshProUGUI TMP(GameObject parent, string nome,
        Vector2 ancMin, Vector2 ancMax,
        string texto, float size, FontStyles style, Color cor)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(parent.transform, false);
        var t = go.AddComponent<TextMeshProUGUI>(); // TMP cria RectTransform automaticamente
        t.text = texto; t.fontSize = size;
        t.fontStyle = style; t.color = cor;
        t.enableWordWrapping = false;
        Anchors(go, ancMin, ancMax);
        return t;
    }

    void Anchors(GameObject go, Vector2 mn, Vector2 mx,
        Vector2 offMin = default, Vector2 offMax = default)
    {
        var r = go.GetComponent<RectTransform>();
        if (r == null) r = go.AddComponent<RectTransform>();
        r.anchorMin = mn; r.anchorMax = mx;
        r.offsetMin = offMin; r.offsetMax = offMax;
    }
}
