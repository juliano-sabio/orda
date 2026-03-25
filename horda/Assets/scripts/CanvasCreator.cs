using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class CanvasCreator : EditorWindow
{
    [MenuItem("Tools/Game System/GERAR SISTEMA COMPLETO")]
    public static void ShowWindow() => GetWindow<CanvasCreator>("Gerador Total");

    void OnGUI()
    {
        GUILayout.Label("Configurações de Geração", EditorStyles.boldLabel);
        if (GUILayout.Button("🎯 GERAR TUDO (Slots + Detalhes + Upgrades + Stages)", GUILayout.Height(50)))
            CriarSistema();
    }

    void CriarSistema()
    {
        GameObject old = GameObject.Find("CanvasPrincipal");
        if (old) DestroyImmediate(old);

        // 1. Root Canvas
        GameObject canvasGO = new GameObject("CanvasPrincipal");
        canvasGO.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        var manager = canvasGO.AddComponent<CharacterSelectionManagerIntegrated>();

        // 2. PAINEL ESQUERDO (Lista de Ícones)
        GameObject pEsq = CriarPainel("PainelPersonagens", canvasGO.transform, new Vector2(-300, 0), new Vector2(400, 550));
        var grid = pEsq.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(110, 140);
        grid.spacing = new Vector2(15, 15);
        grid.padding = new RectOffset(20, 20, 20, 20);

        // Criar 6 slots vazios para personagens
        manager.characterIcons = new CharacterIconUI[6];
        for (int i = 0; i < 6; i++)
            manager.characterIcons[i] = CriarSlotIcone($"Slot_{i}", pEsq.transform);

        // 3. PAINEL DIREITO (Detalhes e Upgrades)
        GameObject pDir = CriarPainel("PainelDetalhes", canvasGO.transform, new Vector2(220, 0), new Vector2(460, 550));

        // Textos de Informação
        CriarTexto("NomePersonagemSelecionado", pDir.transform, new Vector2(0, 230), "HERÓI", 32, true);
        CriarTexto("ElementoPersonagem", pDir.transform, new Vector2(0, 195), "ELEMENTO", 22, false);
        CriarTexto("DescricaoPersonagem", pDir.transform, new Vector2(0, 130), "Descrição detalhada do herói...", 14, false, new Vector2(380, 80));

        // Upgrades e Status Sliders
        string[] statusNomes = { "Vida", "Ataque", "Defesa", "Velocidade" };
        manager.upgradeLevelTexts = new TextMeshProUGUI[4];
        manager.statusSliders = new Slider[4];
        manager.upgradeButtons = new Button[4];

        for (int i = 0; i < statusNomes.Length; i++)
        {
            float yPos = 50 - (i * 60);
            var refs = CriarLinhaStatusUpgrade(statusNomes[i], i, pDir.transform, yPos, manager);
            manager.statusSliders[i] = refs.slider;
            manager.upgradeLevelTexts[i] = refs.txtLvl;
            manager.upgradeButtons[i] = refs.btn;
        }

        CriarTexto("TextoBonusElemental", pDir.transform, new Vector2(0, -180), "Bônus Ativos...", 15, true);
        CriarTexto("TextoMoedas", canvasGO.transform, new Vector2(400, 280), "💰 1000", 26, true);

        // Botão para abrir fases
        GameObject btnPlay = CriarBotao("BtnAbrirFases", pDir.transform, new Vector2(0, -230), "SELECIONAR MISSÃO", Color.green, new Vector2(250, 50));
        btnPlay.GetComponent<Button>().onClick.AddListener(() => manager.ToggleStages(true));

        // 4. PAINEL DE FASES (Stages)
        GameObject pStages = CriarPainel("PainelStages", canvasGO.transform, Vector2.zero, new Vector2(900, 600));
        pStages.SetActive(false);
        manager.painelStages = pStages;
        CriarTexto("TituloStages", pStages.transform, new Vector2(0, 250), "MAPA DE MISSÕES", 35, true);
        CriarBotao("BtnVoltar", pStages.transform, new Vector2(0, -250), "VOLTAR", Color.red).GetComponent<Button>().onClick.AddListener(() => manager.ToggleStages(false));

        Selection.activeGameObject = canvasGO;
        Debug.Log("✅ TUDO GERADO: Slots, Detalhes, Upgrades e Fases prontos!");
    }

    // --- FUNÇÕES DE CONSTRUÇÃO ---

    CharacterIconUI CriarSlotIcone(string nome, Transform pai)
    {
        GameObject slot = new GameObject(nome);
        slot.transform.SetParent(pai);
        slot.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);
        slot.AddComponent<Button>();
        var script = slot.AddComponent<CharacterIconUI>();

        GameObject imgObj = new GameObject("Icone");
        imgObj.transform.SetParent(slot.transform);
        script.characterIcon = imgObj.AddComponent<Image>();
        script.characterIcon.rectTransform.sizeDelta = new Vector2(85, 85);

        GameObject txtObj = new GameObject("Emoji");
        txtObj.transform.SetParent(slot.transform);
        script.elementIconText = txtObj.AddComponent<TextMeshProUGUI>();
        script.elementIconText.fontSize = 22;
        script.elementIconText.alignment = TextAlignmentOptions.BottomRight;

        return script;
    }

    (Slider slider, TextMeshProUGUI txtLvl, Button btn) CriarLinhaStatusUpgrade(string label, int index, Transform p, float y, CharacterSelectionManagerIntegrated m)
    {
        GameObject container = new GameObject($"Linha_{label}");
        container.transform.SetParent(p);
        container.AddComponent<RectTransform>().anchoredPosition = new Vector2(0, y);

        CriarTexto(label, container.transform, new Vector2(-150, 0), label, 16, true, new Vector2(100, 30));

        GameObject sObj = new GameObject("Slider");
        sObj.transform.SetParent(container.transform);
        sObj.AddComponent<RectTransform>().sizeDelta = new Vector2(200, 18);
        sObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(20, 0);
        sObj.AddComponent<Image>().color = Color.black;
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(sObj.transform);
        fill.AddComponent<Image>().color = Color.cyan;
        Slider s = sObj.AddComponent<Slider>();
        s.fillRect = fill.GetComponent<RectTransform>();

        GameObject bObj = CriarBotao("Btn+", container.transform, new Vector2(150, 0), "+", Color.yellow, new Vector2(40, 40));
        Button b = bObj.GetComponent<Button>();
        b.onClick.AddListener(() => m.BuyUpgrade(index));

        TextMeshProUGUI tL = CriarTexto("Lvl", container.transform, new Vector2(205, 0), "Nv.0", 14, false, new Vector2(60, 30)).GetComponent<TextMeshProUGUI>();

        return (s, tL, b);
    }

    // --- AUXILIARES UI ---
    GameObject CriarPainel(string n, Transform p, Vector2 pos, Vector2 s)
    {
        GameObject obj = new GameObject(n);
        obj.transform.SetParent(p);
        obj.AddComponent<RectTransform>().anchoredPosition = pos;
        obj.GetComponent<RectTransform>().sizeDelta = s;
        obj.AddComponent<Image>().color = new Color(0, 0, 0, 0.85f);
        return obj;
    }

    GameObject CriarBotao(string n, Transform p, Vector2 pos, string t, Color c, Vector2? s = null)
    {
        GameObject b = new GameObject(n);
        b.transform.SetParent(p);
        b.AddComponent<RectTransform>().anchoredPosition = pos;
        b.GetComponent<RectTransform>().sizeDelta = s ?? new Vector2(180, 45);
        b.AddComponent<Image>().color = c;
        b.AddComponent<Button>();
        CriarTexto("Label", b.transform, Vector2.zero, t, 18, true, b.GetComponent<RectTransform>().sizeDelta);
        return b;
    }

    GameObject CriarTexto(string n, Transform p, Vector2 pos, string txt, int size, bool b, Vector2? s = null)
    {
        GameObject t = new GameObject(n);
        t.transform.SetParent(p);
        var tmp = t.AddComponent<TextMeshProUGUI>();
        tmp.text = txt; tmp.fontSize = size; tmp.alignment = TextAlignmentOptions.Center;
        if (b) tmp.fontStyle = FontStyles.Bold;
        t.GetComponent<RectTransform>().anchoredPosition = pos;
        t.GetComponent<RectTransform>().sizeDelta = s ?? new Vector2(300, 40);
        return t;
    }
}