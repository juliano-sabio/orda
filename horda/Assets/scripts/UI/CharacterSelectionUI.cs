using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// Adicione este script a um GameObject vazio na cena CharacterSelection.
// Ele cria toda a UI de selecao de personagem em runtime automaticamente.
public class CharacterSelectionUI : MonoBehaviour
{
    [Header("Personagens disponíveis")]
    public CharacterData[] characters;

    [Header("Configurações")]
    public int numSlots = 6;

    // Referências internas
    private CharacterSelectionManagerIntegrated manager;
    private Canvas canvas;

    private TextMeshProUGUI txtNome, txtElemento, txtDesc, txtBonus, txtMoedas;
    private Slider[] sliders = new Slider[4];
    private Button[] upgradeButtons = new Button[4];
    private TextMeshProUGUI[] upgradeLevelTexts = new TextMeshProUGUI[4];
    private CharacterIconUI[] iconesArray;
    private GameObject painelUltimates;

    void Start()
    {
        CriarCanvas();
        CriarUI();
        ConectarManager();
    }

    // ─────────────────────────────────────────────────────────────
    // 1. Canvas

    void CriarCanvas()
    {
        GameObject canvasGO = new GameObject("CanvasPrincipal");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        manager = canvasGO.AddComponent<CharacterSelectionManagerIntegrated>();
        manager.characters = characters;
    }

    // ─────────────────────────────────────────────────────────────
    // 2. UI

    void CriarUI()
    {
        GameObject root = canvas.gameObject;

        // Fundo
        CriarImagem(root, "Fundo",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0.06f, 0.06f, 0.12f));

        // Título
        var titulo = CriarTMP(root, "Titulo",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -15f), new Vector2(0f, -65f),
            34f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        titulo.text = "SELEÇÃO DE PERSONAGEM";

        // Painel de ícones
        CriarPainelIcones(root);

        // Painel central
        CriarPainelCentral(root);

        // Rodapé (moedas + botões)
        CriarRodape(root);
    }

    void CriarPainelIcones(GameObject root)
    {
        GameObject painel = CriarImagem(root, "PainelIcones",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(10f, -75f), new Vector2(-10f, -225f),
            new Color(0.08f, 0.08f, 0.18f));

        HorizontalLayoutGroup hlg = painel.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 15f;
        hlg.padding = new RectOffset(15, 15, 8, 8);
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        int slots = Mathf.Max(numSlots, characters != null ? characters.Length : 1);
        iconesArray = new CharacterIconUI[slots];
        for (int i = 0; i < slots; i++)
            iconesArray[i] = CriarIconePersonagem(painel, i);
    }

    CharacterIconUI CriarIconePersonagem(GameObject parent, int index)
    {
        GameObject go = new GameObject($"Icone_{index}");
        go.transform.SetParent(parent.transform, false);

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 120f;
        le.preferredHeight = 120f;

        Image bgImg = go.AddComponent<Image>();
        bgImg.color = new Color(0.12f, 0.12f, 0.25f);
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = bgImg;

        CharacterIconUI iconUI = go.AddComponent<CharacterIconUI>();

        // Sprite do personagem
        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(go.transform, false);
        SetAnchors(iconGO, new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.95f));
        Image iconImg = iconGO.AddComponent<Image>();
        iconImg.color = new Color(0.5f, 0.5f, 0.5f, 0.4f);
        iconUI.characterIcon = iconImg;

        // Nome
        iconUI.characterName = CriarTMP(go, "Nome",
            new Vector2(0f, 0f), new Vector2(1f, 0.3f),
            Vector2.zero, Vector2.zero,
            11f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);

        // Elemento
        iconUI.elementIconText = CriarTMP(go, "Elem",
            new Vector2(0f, 0.85f), new Vector2(1f, 1f),
            Vector2.zero, Vector2.zero,
            13f, FontStyles.Normal, Color.yellow, TextAlignmentOptions.Center);

        // Fundo elemento
        GameObject elemBg = new GameObject("ElemBG");
        elemBg.transform.SetParent(go.transform, false);
        SetAnchors(elemBg, Vector2.zero, Vector2.one);
        Image elemBgImg = elemBg.AddComponent<Image>();
        elemBgImg.color = new Color(1f, 1f, 1f, 0f);
        iconUI.elementBackground = elemBgImg;

        // Indicador selecionado
        GameObject sel = new GameObject("Selecionado");
        sel.transform.SetParent(go.transform, false);
        RectTransform selR = sel.AddComponent<RectTransform>();
        selR.anchorMin = Vector2.zero;
        selR.anchorMax = Vector2.one;
        selR.offsetMin = new Vector2(-3f, -3f);
        selR.offsetMax = new Vector2(3f, 3f);
        sel.AddComponent<Image>().color = new Color(0.2f, 1f, 0.4f, 0.35f);
        sel.SetActive(false);
        iconUI.selectedIndicator = sel;

        return iconUI;
    }

    void CriarPainelCentral(GameObject root)
    {
        GameObject centro = new GameObject("PainelCentro");
        centro.transform.SetParent(root.transform, false);
        SetAnchors(centro, new Vector2(0f, 0.18f), new Vector2(1f, 0.78f),
                   new Vector2(10f, 4f), new Vector2(-10f, -4f));

        // Info (esquerda)
        GameObject info = CriarImagem(centro, "PainelInfo",
            new Vector2(0f, 0f), new Vector2(0.44f, 1f),
            Vector2.zero, new Vector2(-4f, 0f),
            new Color(0.08f, 0.08f, 0.18f));

        txtNome = CriarTMP(info, "Nome",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(8f, -12f), new Vector2(-8f, -46f),
            24f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);

        txtElemento = CriarTMP(info, "Elemento",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(8f, -52f), new Vector2(-8f, -82f),
            16f, FontStyles.Normal, Color.yellow, TextAlignmentOptions.Center);

        txtDesc = CriarTMP(info, "Descricao",
            new Vector2(0f, 0.3f), new Vector2(1f, 0.82f),
            new Vector2(10f, 0f), new Vector2(-10f, 0f),
            12f, FontStyles.Normal, new Color(0.8f, 0.8f, 0.8f), TextAlignmentOptions.Center);
        txtDesc.enableWordWrapping = true;

        txtBonus = CriarTMP(info, "Bonus",
            new Vector2(0f, 0f), new Vector2(1f, 0.28f),
            new Vector2(8f, 4f), new Vector2(-8f, -4f),
            11f, FontStyles.Normal, new Color(0.5f, 1f, 0.5f), TextAlignmentOptions.Center);
        txtBonus.enableWordWrapping = true;

        // Status (direita)
        GameObject status = CriarImagem(centro, "PainelStatus",
            new Vector2(0.56f, 0f), new Vector2(1f, 1f),
            new Vector2(4f, 0f), Vector2.zero,
            new Color(0.08f, 0.08f, 0.18f));

        string[] labels = { "Vida", "Ataque", "Defesa", "Velocidade" };
        for (int i = 0; i < 4; i++)
        {
            float yMin = 0.75f - i * 0.25f;
            float yMax = 1.00f - i * 0.25f;

            GameObject linha = new GameObject($"Linha_{i}");
            linha.transform.SetParent(status.transform, false);
            SetAnchors(linha, new Vector2(0f, yMin), new Vector2(1f, yMax),
                       new Vector2(10f, 4f), new Vector2(-10f, -4f));

            // Label
            var lbl = CriarTMP(linha, "Label",
                new Vector2(0f, 0f), new Vector2(0.22f, 1f),
                Vector2.zero, Vector2.zero,
                12f, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft);
            lbl.text = labels[i];

            // Slider
            GameObject slGO = new GameObject("Slider");
            slGO.transform.SetParent(linha.transform, false);
            SetAnchors(slGO, new Vector2(0.22f, 0.2f), new Vector2(0.72f, 0.8f));
            sliders[i] = CriarSlider(slGO);

            // Nível
            upgradeLevelTexts[i] = CriarTMP(linha, "Nivel",
                new Vector2(0.72f, 0f), new Vector2(0.85f, 1f),
                Vector2.zero, Vector2.zero,
                11f, FontStyles.Normal, new Color(0.7f, 0.7f, 1f), TextAlignmentOptions.Center);
            upgradeLevelTexts[i].text = "Nv.0";

            // Botão +
            int cap = i;
            GameObject btnGO = new GameObject("BtnUpgrade");
            btnGO.transform.SetParent(linha.transform, false);
            SetAnchors(btnGO, new Vector2(0.86f, 0.1f), new Vector2(1f, 0.9f));
            Image bImg = btnGO.AddComponent<Image>();
            bImg.color = new Color(0.15f, 0.5f, 0.15f);
            Button b = btnGO.AddComponent<Button>();
            b.targetGraphic = bImg;
            b.onClick.AddListener(() => manager?.BuyUpgrade(cap));
            var bTxt = CriarTMP(btnGO, "Txt",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                16f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            bTxt.text = "+";
            upgradeButtons[i] = b;
        }
    }

    void CriarRodape(GameObject root)
    {
        GameObject rodape = new GameObject("Rodape");
        rodape.transform.SetParent(root.transform, false);
        SetAnchors(rodape, new Vector2(0f, 0f), new Vector2(1f, 0.16f),
                   new Vector2(10f, 8f), new Vector2(-10f, -8f));

        // Moedas
        txtMoedas = CriarTMP(rodape, "Moedas",
            new Vector2(0f, 0f), new Vector2(0.25f, 1f),
            new Vector2(8f, 0f), Vector2.zero,
            22f, FontStyles.Bold, Color.yellow, TextAlignmentOptions.MidlineLeft);
        txtMoedas.text = "💰 0";

        // Botão VOLTAR
        CriarBotao(rodape, "BotaoVoltar",
            new Vector2(0f, 0.1f), new Vector2(0.18f, 0.9f),
            "← VOLTAR", new Color(0.5f, 0.1f, 0.1f),
            () => {
                if (GameSceneManager.Instance != null)
                    GameSceneManager.Instance.GoToMainMenu();
                else
                    UnityEngine.SceneManagement.SceneManager.LoadScene("menu_inicial");
            });

        // Botão JOGAR
        CriarBotao(rodape, "BotaoJogar",
            new Vector2(0.72f, 0.05f), new Vector2(1f, 0.95f),
            "JOGAR ▶", new Color(0.1f, 0.55f, 0.15f),
            () => {
                if (GameSceneManager.Instance != null)
                    GameSceneManager.Instance.StartGameplay();
                else
                    UnityEngine.SceneManagement.SceneManager.LoadScene("primeira_fase");
            }, 26f);
    }

    // ─────────────────────────────────────────────────────────────
    // 3. Conecta ao manager

    void ConectarManager()
    {
        manager.characterNameText    = txtNome;
        manager.characterElementText = txtElemento;
        manager.characterDescriptionText = txtDesc;
        manager.elementBonusText     = txtBonus;
        manager.coinsText            = txtMoedas;
        manager.statusSliders        = sliders;
        manager.upgradeButtons       = upgradeButtons;
        manager.upgradeLevelTexts    = upgradeLevelTexts;
        manager.characterIcons       = iconesArray;

        // Painel stages vazio (obrigatório para não dar null ref)
        GameObject stagePlaceholder = new GameObject("PainelStages");
        stagePlaceholder.transform.SetParent(canvas.transform, false);
        stagePlaceholder.AddComponent<RectTransform>();
        stagePlaceholder.SetActive(false);
        manager.painelStages = stagePlaceholder;

        // Inicia o manager (ele chama LoadProgress e SelectCharacter internamente)
        // O manager.Start() é chamado automaticamente pelo Unity
    }

    // ─────────────────────────────────────────────────────────────
    // Helpers

    Slider CriarSlider(GameObject go)
    {
        go.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f);

        GameObject fillArea = new GameObject("FillArea");
        fillArea.transform.SetParent(go.transform, false);
        SetAnchors(fillArea, new Vector2(0f, 0.2f), new Vector2(1f, 0.8f),
                   new Vector2(4f, 0f), new Vector2(-4f, 0f));

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRT = fill.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(0f, 1f);
        fillRT.sizeDelta = new Vector2(8f, 0f);
        fill.AddComponent<Image>().color = new Color(0.2f, 0.6f, 1f);

        Slider s = go.AddComponent<Slider>();
        s.fillRect = fillRT;
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = 0f;
        s.maxValue = 1f;
        s.value = 0.5f;
        s.interactable = false;
        return s;
    }

    void CriarBotao(GameObject parent, string nome,
        Vector2 ancMin, Vector2 ancMax,
        string texto, Color cor, UnityEngine.Events.UnityAction acao,
        float fontSize = 20f)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(parent.transform, false);
        SetAnchors(go, ancMin, ancMax);
        Image img = go.AddComponent<Image>();
        img.color = cor;
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(acao);
        var txt = CriarTMP(go, "Txt",
            Vector2.zero, Vector2.one, new Vector2(4f, 0f), new Vector2(-4f, 0f),
            fontSize, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        txt.text = texto;
    }

    GameObject CriarImagem(GameObject parent, string nome,
        Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax, Color cor)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(parent.transform, false);
        SetAnchors(go, ancMin, ancMax, offMin, offMax);
        go.AddComponent<Image>().color = cor;
        return go;
    }

    TextMeshProUGUI CriarTMP(GameObject parent, string nome,
        Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax,
        float size, FontStyles style, Color cor, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(parent.transform, false);
        SetAnchors(go, ancMin, ancMax, offMin, offMax);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.fontStyle = style;
        t.color = cor;
        t.alignment = align;
        t.enableWordWrapping = false;
        return t;
    }

    void SetAnchors(GameObject go, Vector2 ancMin, Vector2 ancMax,
                    Vector2 offMin = default, Vector2 offMax = default)
    {
        RectTransform r = go.GetComponent<RectTransform>();
        if (r == null) r = go.AddComponent<RectTransform>();
        r.anchorMin = ancMin;
        r.anchorMax = ancMax;
        r.offsetMin = offMin;
        r.offsetMax = offMax;
    }
}
