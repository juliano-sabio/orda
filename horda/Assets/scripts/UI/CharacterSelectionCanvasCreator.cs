using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

public class CharacterSelectionCanvasCreator : EditorWindow
{
    private const int NUM_CHAR_SLOTS = 6;

    [MenuItem("Tools/UI Manager/Create Character Selection Canvas")]
    public static void CreateCanvas()
    {
        // Remove canvas anterior se existir
        GameObject old = GameObject.Find("CanvasPrincipal");
        if (old != null)
        {
            bool confirm = EditorUtility.DisplayDialog(
                "Canvas já existe",
                "Já existe um 'CanvasPrincipal' na cena. Deseja substituir?",
                "Sim", "Cancelar");
            if (!confirm) return;
            DestroyImmediate(old);
        }

        // ── Canvas ──────────────────────────────────────────────────
        GameObject canvasGO = new GameObject("CanvasPrincipal");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Manager
        CharacterSelectionManagerIntegrated manager =
            canvasGO.AddComponent<CharacterSelectionManagerIntegrated>();

        // ── Fundo ────────────────────────────────────────────────────
        GameObject fundo = CriarImagem(canvasGO, "Fundo",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0.06f, 0.06f, 0.12f, 1f));

        // ── Título ───────────────────────────────────────────────────
        GameObject titulo = CriarGO("Titulo", canvasGO);
        SetRect(titulo, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -20f), new Vector2(0f, -70f));
        TextMeshProUGUI txtTitulo = titulo.AddComponent<TextMeshProUGUI>();
        txtTitulo.text = "SELEÇÃO DE PERSONAGEM";
        txtTitulo.fontSize = 36;
        txtTitulo.fontStyle = FontStyles.Bold;
        txtTitulo.color = Color.white;
        txtTitulo.alignment = TextAlignmentOptions.Center;

        // ── Painel de ícones de personagem ───────────────────────────
        GameObject painelIcones = CriarImagem(canvasGO, "PainelIcones",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -80f), new Vector2(0f, -230f),
            new Color(0.08f, 0.08f, 0.18f, 0.9f));

        HorizontalLayoutGroup hlg = painelIcones.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 20f;
        hlg.padding = new RectOffset(20, 20, 10, 10);
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        CharacterIconUI[] iconesArray = new CharacterIconUI[NUM_CHAR_SLOTS];
        for (int i = 0; i < NUM_CHAR_SLOTS; i++)
            iconesArray[i] = CriarCharacterIcon(painelIcones, i);

        manager.characterIcons = iconesArray;

        // ── Painel central (info + status) ───────────────────────────
        GameObject painelCentro = CriarGO("PainelCentro", canvasGO);
        SetRect(painelCentro, new Vector2(0f, 0.2f), new Vector2(1f, 0.78f),
                new Vector2(20f, 0f), new Vector2(-20f, 0f));

        // Info do personagem (esquerda)
        GameObject painelInfo = CriarImagem(painelCentro, "PainelInfo",
            new Vector2(0f, 0f), new Vector2(0.45f, 1f),
            Vector2.zero, Vector2.zero,
            new Color(0.08f, 0.08f, 0.18f, 0.9f));

        TextMeshProUGUI txtNome = CriarTMP(painelInfo, "NomePersonagem",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -15f), new Vector2(-16f, 36f),
            26f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);

        TextMeshProUGUI txtElemento = CriarTMP(painelInfo, "ElementoPersonagem",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -60f), new Vector2(-16f, 28f),
            18f, FontStyles.Normal, Color.yellow, TextAlignmentOptions.Center);

        TextMeshProUGUI txtDesc = CriarTMP(painelInfo, "DescricaoPersonagem",
            new Vector2(0f, 0.35f), new Vector2(1f, 0.85f),
            new Vector2(0f, 0f), new Vector2(-20f, 0f),
            13f, FontStyles.Normal, new Color(0.8f, 0.8f, 0.8f), TextAlignmentOptions.Center);
        txtDesc.textWrappingMode = TMPro.TextWrappingModes.Normal;

        TextMeshProUGUI txtBonus = CriarTMP(painelInfo, "BonusElemento",
            new Vector2(0f, 0f), new Vector2(1f, 0.3f),
            new Vector2(0f, 0f), new Vector2(-16f, 0f),
            12f, FontStyles.Normal, new Color(0.6f, 1f, 0.6f), TextAlignmentOptions.Center);
        txtBonus.textWrappingMode = TMPro.TextWrappingModes.Normal;

        manager.characterNameText = txtNome;
        manager.characterElementText = txtElemento;
        manager.characterDescriptionText = txtDesc;
        manager.elementBonusText = txtBonus;

        // Status (direita)
        GameObject painelStatus = CriarImagem(painelCentro, "PainelStatus",
            new Vector2(0.55f, 0f), new Vector2(1f, 1f),
            Vector2.zero, Vector2.zero,
            new Color(0.08f, 0.08f, 0.18f, 0.9f));

        string[] statLabels = { "Vida", "Ataque", "Defesa", "Velocidade" };
        Slider[] sliders = new Slider[4];
        Button[] upgButtons = new Button[4];
        TextMeshProUGUI[] upgTexts = new TextMeshProUGUI[4];

        for (int i = 0; i < 4; i++)
        {
            float yAncMin = 0.75f - i * 0.25f;
            float yAncMax = 1f    - i * 0.25f;

            GameObject linha = CriarGO($"LinhaStatus_{i}", painelStatus);
            SetRect(linha, new Vector2(0f, yAncMin), new Vector2(1f, yAncMax),
                    new Vector2(10f, 4f), new Vector2(-10f, -4f));

            // Label
            TextMeshProUGUI lbl = CriarTMP(linha, "Label",
                new Vector2(0f, 0f), new Vector2(0.25f, 1f),
                Vector2.zero, Vector2.zero,
                13f, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft);
            lbl.text = statLabels[i];

            // Slider
            GameObject sliderGO = CriarGO("Slider", linha);
            SetRect(sliderGO, new Vector2(0.25f, 0.2f), new Vector2(0.7f, 0.8f),
                    Vector2.zero, Vector2.zero);
            sliders[i] = CriarSlider(sliderGO);

            // Nivel text
            TextMeshProUGUI nvText = CriarTMP(linha, "NivelText",
                new Vector2(0.7f, 0f), new Vector2(0.82f, 1f),
                Vector2.zero, Vector2.zero,
                12f, FontStyles.Normal, new Color(0.7f, 0.7f, 1f), TextAlignmentOptions.Center);
            nvText.text = "Nv. 0";
            upgTexts[i] = nvText;

            // Botão upgrade
            int capturedI = i;
            GameObject btnGO = CriarGO("BotaoUpgrade", linha);
            SetRect(btnGO, new Vector2(0.83f, 0.1f), new Vector2(1f, 0.9f),
                    Vector2.zero, Vector2.zero);
            Image btnImg = btnGO.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.5f, 0.2f, 1f);
            Button btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(() => manager.BuyUpgrade(capturedI));
            TextMeshProUGUI btnTxt = CriarTMP(btnGO, "Texto",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                14f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            btnTxt.text = "+";
            upgButtons[i] = btn;
        }

        manager.statusSliders = sliders;
        manager.upgradeButtons = upgButtons;
        manager.upgradeLevelTexts = upgTexts;

        // ── Moedas + Botão Jogar ─────────────────────────────────────
        GameObject painelRodape = CriarGO("PainelRodape", canvasGO);
        SetRect(painelRodape, new Vector2(0f, 0f), new Vector2(1f, 0.12f),
                new Vector2(20f, 10f), new Vector2(-20f, -10f));

        TextMeshProUGUI txtMoedas = CriarTMP(painelRodape, "TextoMoedas",
            new Vector2(0f, 0f), new Vector2(0.3f, 1f),
            Vector2.zero, Vector2.zero,
            22f, FontStyles.Bold, Color.yellow, TextAlignmentOptions.MidlineLeft);
        txtMoedas.text = "💰 1000";
        manager.coinsText = txtMoedas;

        // Botão Jogar
        GameObject btnJogarGO = CriarGO("BotaoJogar", painelRodape);
        SetRect(btnJogarGO, new Vector2(0.7f, 0.1f), new Vector2(1f, 0.9f),
                Vector2.zero, Vector2.zero);
        Image btnJogarImg = btnJogarGO.AddComponent<Image>();
        btnJogarImg.color = new Color(0.1f, 0.6f, 0.15f, 1f);
        Button btnJogar = btnJogarGO.AddComponent<Button>();
        btnJogar.targetGraphic = btnJogarImg;
        btnJogar.onClick.AddListener(() => {
            if (GameSceneManager.Instance != null)
                GameSceneManager.Instance.StartGameplay();
        });
        TextMeshProUGUI txtJogar = CriarTMP(btnJogarGO, "Texto",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            28f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        txtJogar.text = "JOGAR";

        // ── Botão Voltar ─────────────────────────────────────────────
        GameObject btnVoltarGO = CriarGO("BotaoVoltar", painelRodape);
        SetRect(btnVoltarGO, new Vector2(0f, 0.1f), new Vector2(0.15f, 0.9f),
                Vector2.zero, Vector2.zero);
        Image btnVoltarImg = btnVoltarGO.AddComponent<Image>();
        btnVoltarImg.color = new Color(0.5f, 0.1f, 0.1f, 1f);
        Button btnVoltar = btnVoltarGO.AddComponent<Button>();
        btnVoltar.targetGraphic = btnVoltarImg;
        btnVoltar.onClick.AddListener(() => {
            if (GameSceneManager.Instance != null)
                GameSceneManager.Instance.GoToMainMenu();
        });
        TextMeshProUGUI txtVoltar = CriarTMP(btnVoltarGO, "Texto",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            18f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        txtVoltar.text = "← VOLTAR";

        // ── Painel Stages (oculto) ────────────────────────────────────
        GameObject painelStages = CriarImagem(canvasGO, "PainelStages",
            new Vector2(0.25f, 0.1f), new Vector2(0.75f, 0.9f),
            Vector2.zero, Vector2.zero,
            new Color(0.05f, 0.05f, 0.15f, 0.97f));
        painelStages.SetActive(false);
        manager.painelStages = painelStages;

        TextMeshProUGUI txtStages = CriarTMP(painelStages, "Titulo",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -20f), new Vector2(0f, -60f),
            24f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        txtStages.text = "ESCOLHA A FASE";

        Button btnFecharStages = painelStages.AddComponent<Button>();
        btnFecharStages.onClick.AddListener(() => manager.ToggleStages(false));

        // Finaliza
        Selection.activeGameObject = canvasGO;
        EditorUtility.SetDirty(canvasGO);

        Debug.Log("✅ Canvas de Seleção de Personagem criado! Agora arraste seus CharacterData para o campo 'Characters' no Manager.");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    static CharacterIconUI CriarCharacterIcon(GameObject parent, int index)
    {
        GameObject go = CriarGO($"Icone_Personagem_{index}", parent);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 130f;
        le.preferredHeight = 130f;

        Image bgImg = go.AddComponent<Image>();
        bgImg.color = new Color(0.12f, 0.12f, 0.25f, 1f);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = bgImg;

        CharacterIconUI iconUI = go.AddComponent<CharacterIconUI>();

        // Ícone do personagem
        GameObject iconGO = CriarGO("Icon", go);
        SetRect(iconGO, new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.95f),
                Vector2.zero, Vector2.zero);
        Image iconImg = iconGO.AddComponent<Image>();
        iconImg.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        iconUI.characterIcon = iconImg;

        // Nome
        TextMeshProUGUI nomeText = CriarTMP(go, "Nome",
            new Vector2(0f, 0f), new Vector2(1f, 0.3f),
            Vector2.zero, Vector2.zero,
            11f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        iconUI.characterName = nomeText;

        // Elemento
        TextMeshProUGUI elemText = CriarTMP(go, "Elemento",
            new Vector2(0f, 0.85f), new Vector2(1f, 1f),
            Vector2.zero, Vector2.zero,
            12f, FontStyles.Normal, Color.yellow, TextAlignmentOptions.Center);
        iconUI.elementIconText = elemText;

        // Fundo do elemento
        GameObject elemBg = CriarGO("ElementoBG", go);
        SetRect(elemBg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image elemBgImg = elemBg.AddComponent<Image>();
        elemBgImg.color = new Color(1f, 1f, 1f, 0f);
        iconUI.elementBackground = elemBgImg;

        // Indicador de seleção
        GameObject selecionado = CriarGO("Selecionado", go);
        SetRect(selecionado, Vector2.zero, Vector2.one,
                new Vector2(-3f, -3f), new Vector2(3f, 3f));
        Image selImg = selecionado.AddComponent<Image>();
        selImg.color = new Color(0.2f, 1f, 0.4f, 0.4f);
        selecionado.SetActive(false);
        iconUI.selectedIndicator = selecionado;

        return iconUI;
    }

    static Slider CriarSlider(GameObject go)
    {
        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        GameObject fillArea = CriarGO("Fill Area", go);
        SetRect(fillArea, new Vector2(0f, 0.25f), new Vector2(1f, 0.75f),
                new Vector2(5f, 0f), new Vector2(-5f, 0f));

        GameObject fill = CriarGO("Fill", fillArea);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.sizeDelta = new Vector2(10f, 0f);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.7f, 1f, 1f);

        Slider slider = go.AddComponent<Slider>();
        slider.fillRect = fillRect;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.5f;
        slider.interactable = false;
        return slider;
    }

    static GameObject CriarImagem(GameObject parent, string nome,
        Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax, Color cor)
    {
        GameObject go = CriarGO(nome, parent);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = ancMin; r.anchorMax = ancMax;
        r.offsetMin = offMin; r.offsetMax = offMax;
        go.AddComponent<Image>().color = cor;
        return go;
    }

    static GameObject CriarGO(string nome, GameObject parent)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static void SetRect(GameObject go, Vector2 ancMin, Vector2 ancMax,
                        Vector2 offMin, Vector2 offMax)
    {
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = ancMin; r.anchorMax = ancMax;
        r.offsetMin = offMin; r.offsetMax = offMax;
    }

    static TextMeshProUGUI CriarTMP(GameObject parent, string nome,
        Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax,
        float size, FontStyles style, Color cor, TextAlignmentOptions align)
    {
        GameObject go = CriarGO(nome, parent);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = ancMin; r.anchorMax = ancMax;
        r.offsetMin = offMin; r.offsetMax = offMax;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = cor;
        tmp.alignment = align;
        tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        return tmp;
    }
}
