using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Exibe as opções de evolução após completar um evento.
// Adicionado automaticamente via GerenciadorEventos.
public class SkillEvolutionUI : MonoBehaviour
{
    public static SkillEvolutionUI Instance { get; private set; }

    public GameObject cardPrefab; // prefab da cartadeevolucao01

    Canvas          canvas;
    GameObject      painel;
    Transform       slotsContainer;
    TextMeshProUGUI tituloBanner;
    CanvasGroup     cg;

    bool visivel    = false;
    bool selecionou = false; // guard contra duplo disparo de listeners persistentes

    public bool Visivel => visivel;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        CriarUI();
    }

    // ── Exibição ─────────────────────────────────────────────────────────────

    public void MostrarOpcoes(List<SkillEvolutionData> opcoes, System.Action<SkillEvolutionData> callback)
    {
        if (opcoes == null || opcoes.Count == 0) return;
        StartCoroutine(AnimarEntrada(opcoes, callback));
    }

    IEnumerator AnimarEntrada(List<SkillEvolutionData> opcoes, System.Action<SkillEvolutionData> callback)
    {
        Time.timeScale = 0f;
        visivel    = true;
        selecionou = false;
        if (canvas != null) canvas.sortingOrder = 999;

        // Esconde temporariamente o painel do SkillChoiceUI para não cobrir as cartas
        var choiceUI = Object.FindFirstObjectByType<SkillChoiceUI>();
        if (choiceUI != null && choiceUI.choicePanel != null)
            choiceUI.choicePanel.SetActive(false);

        painel.SetActive(true);

        // Limpa slots antigos
        foreach (Transform c in slotsContainer) Destroy(c.gameObject);

        // Cria cards
        foreach (var op in opcoes)
        {
            var card = CriarCard(op);
            card.transform.SetParent(slotsContainer, false);

            // NUCLEAR: bloqueia raycast em TODOS os Graphics — nada do prefab intercepta
            foreach (var g in card.GetComponentsInChildren<Graphic>(true))
                g.raycastTarget = false;

            // Desativa todos os botões do prefab
            foreach (var childBtn in card.GetComponentsInChildren<Button>(true))
            {
                childBtn.onClick.RemoveAllListeners();
                childBtn.interactable = false;
            }

            // Overlay invisível cobrindo o card — único ponto de clique
            var overlay = new GameObject("EvoOverlay");
            overlay.transform.SetParent(card.transform, false);
            var rt = overlay.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = overlay.AddComponent<Image>();
            img.color = Color.clear;
            img.raycastTarget = true;
            var overlayBtn = overlay.AddComponent<Button>();
            var opcaoLocal = op;
            overlayBtn.onClick.AddListener(() => Selecionar(opcaoLocal, callback));
        }

        // Fade in — ativa raycasts ao mostrar
        cg.blocksRaycasts = true;
        for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
        { cg.alpha = t / 0.3f; yield return null; }
        cg.alpha = 1f;
    }

    void Selecionar(SkillEvolutionData opcao, System.Action<SkillEvolutionData> callback)
    {
        if (selecionou) return; // ignora se listener persistente disparar junto
        selecionou = true;
        StartCoroutine(AnimarSaida(opcao, callback));
    }

    IEnumerator AnimarSaida(SkillEvolutionData opcao, System.Action<SkillEvolutionData> callback)
    {
        for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime)
        { cg.alpha = 1f - t / 0.2f; yield return null; }
        cg.alpha          = 0f;
        cg.blocksRaycasts = false;
        painel.SetActive(false);
        visivel = false;
        Time.timeScale = 1f;

        // Reativa o painel do SkillChoiceUI se ele estava visível antes
        var choiceUI2 = Object.FindFirstObjectByType<SkillChoiceUI>();
        if (choiceUI2 != null && choiceUI2.choicePanel != null && choiceUI2.currentChoices != null && choiceUI2.currentChoices.Count > 0)
            choiceUI2.choicePanel.SetActive(true);

        callback?.Invoke(opcao);
    }

    // ── Criação de UI ─────────────────────────────────────────────────────────

    void CriarUI()
    {
        var go = gameObject;

        canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        cg = go.AddComponent<CanvasGroup>();
        cg.alpha          = 0f;
        cg.blocksRaycasts = false; // não bloqueia cliques quando invisível

        // Painel
        painel = new GameObject("PainelEvolucao");
        painel.transform.SetParent(go.transform, false);

        // Fundo escuro
        var bgRT = painel.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        var bgImg = painel.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.82f);
        bgImg.raycastTarget = true;

        // Título
        var tituloGO = new GameObject("Titulo");
        tituloGO.transform.SetParent(painel.transform, false);
        var tituloRT = tituloGO.AddComponent<RectTransform>();
        tituloRT.anchorMin = new Vector2(0.05f, 0.72f);
        tituloRT.anchorMax = new Vector2(0.95f, 0.88f);
        tituloRT.offsetMin = tituloRT.offsetMax = Vector2.zero;
        tituloBanner = tituloGO.AddComponent<TextMeshProUGUI>();
        tituloBanner.text      = "EVOLUCAO DE SKILL";
        tituloBanner.fontSize  = 52;
        tituloBanner.fontStyle = FontStyles.Bold;
        tituloBanner.alignment = TextAlignmentOptions.Center;
        tituloBanner.color     = new Color(1f, 0.85f, 0.2f);

        var subtituloGO = new GameObject("Sub");
        subtituloGO.transform.SetParent(painel.transform, false);
        var subRT = subtituloGO.AddComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0.1f, 0.66f);
        subRT.anchorMax = new Vector2(0.9f, 0.74f);
        subRT.offsetMin = subRT.offsetMax = Vector2.zero;
        var sub = subtituloGO.AddComponent<TextMeshProUGUI>();
        sub.text      = "Escolha uma evolução para sua skill";
        sub.fontSize  = 22;
        sub.alignment = TextAlignmentOptions.Center;
        sub.color     = new Color(0.85f, 0.85f, 0.85f);

        // Container de slots
        var contGO = new GameObject("Slots");
        contGO.transform.SetParent(painel.transform, false);
        var contRT = contGO.AddComponent<RectTransform>();
        contRT.anchorMin = new Vector2(0.05f, 0.08f);
        contRT.anchorMax = new Vector2(0.95f, 0.68f);
        contRT.offsetMin = contRT.offsetMax = Vector2.zero;
        var hLayout = contGO.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing              = 30f;
        hLayout.childAlignment       = TextAnchor.MiddleCenter;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = false;
        var fitter = contGO.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        slotsContainer = contGO.transform;

        painel.SetActive(false);
    }

    GameObject CriarCard(SkillEvolutionData data)
    {
        GameObject prefab = cardPrefab;
        if (prefab == null)
            prefab = Resources.Load<GameObject>("CartaEvolucao");

        GameObject card;
        if (prefab != null)
        {
            card = Instantiate(prefab);
            PopularCardComPrefab(card, data);

            // Garante que nenhum elemento do prefab está invisível
            foreach (var g in card.GetComponentsInChildren<Graphic>(true))
            {
                if (g.color.a < 0.05f) continue; // preserva elementos intencionalmente invisíveis
                var c = g.color; c.a = 1f; g.color = c;
            }
        }
        else
        {
            card = CriarCardManual(data);
        }

        var layoutEl = card.GetComponent<LayoutElement>() ?? card.AddComponent<LayoutElement>();
        layoutEl.preferredWidth  = 420f;
        layoutEl.preferredHeight = 560f;

        // Remove todos os listeners de TODOS os buttons (persistentes e runtime)
        // usando interactable=false + RemoveAllListeners na raiz
        var btn = card.GetComponent<Button>() ?? card.AddComponent<Button>();
        btn.onClick.RemoveAllListeners();

        return card;
    }

    void PopularCardComPrefab(GameObject card, SkillEvolutionData data)
    {
        // Garante que todos os textos ficam visíveis (não apagados)
        var textos = card.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (var txt in textos)
        {
            string n = txt.name.ToLower();
            if (n.Contains("name") || n.Contains("nome") || n.Contains("title"))
            {
                txt.text      = $"<b>{TextUtils.SemAcento(data.nomeEvolucao)}</b>";
                txt.color     = data.corDestaque;
                txt.fontSize  = 18;
            }
            else if (n.Contains("desc") || n.Contains("description") || n.Contains("detail"))
            {
                txt.text      = TextUtils.SemAcento(data.descricao);
                txt.color     = Color.white;
                txt.fontSize  = 13;
            }
            else if (n.Contains("stats") || n.Contains("status") || n.Contains("bonus"))
            {
                txt.text      = data.raridade.ToString().ToUpper();
                txt.color     = data.corDestaque;
                txt.fontSize  = 12;
            }
            txt.textWrappingMode = TMPro.TextWrappingModes.Normal;
            // Força alpha = 1
            { var c2 = txt.color; c2.a = 1f; txt.color = c2; }
        }

        // Ícone — só atribui se tiver icone e o sprite não for de inimigo
        var imagens = card.GetComponentsInChildren<Image>();
        foreach (var img in imagens)
        {
            if ((img.name.Contains("Icon") || img.name.Contains("Image")) && data.icone != null)
            {
                img.sprite = data.icone;
                img.color  = Color.white; // garante visível
            }
            // Garante fundo não muito escuro
            else if (img.name.Contains("Bg") || img.name.Contains("Background") || img.name == card.name)
            {
                Color c = img.color;
                if (c.a < 0.5f) { c.a = 0.95f; img.color = c; }
            }
        }

        // NÃO chama SkillCardRuntimeManager — ele escurece o fundo com elementColor*0.2
        // Para cartas de evolução, o visual é controlado apenas pelo PopularCardComPrefab
        var runtime = card.GetComponent<SkillCardRuntimeManager>();
        if (runtime != null) runtime.enabled = false;
    }

    GameObject CriarCardManual(SkillEvolutionData data)
    {
        var card = new GameObject($"Card_{TextUtils.SemAcento(data.nomeEvolucao)}");
        card.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.12f, 0.95f);

        var bordaGO = CriarFilho("Borda", card.transform);
        var bordaRT = bordaGO.GetComponent<RectTransform>();
        bordaRT.anchorMin = Vector2.zero; bordaRT.anchorMax = Vector2.one;
        bordaRT.offsetMin = new Vector2(-2,-2); bordaRT.offsetMax = new Vector2(2,2);
        bordaGO.AddComponent<Image>().color = new Color(data.corDestaque.r, data.corDestaque.g, data.corDestaque.b, 0.7f);

        if (data.icone != null)
        {
            var iconeGO = CriarFilho("Icon", card.transform);
            var rt = iconeGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.2f,0.62f); rt.anchorMax = new Vector2(0.8f,0.92f); rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = iconeGO.AddComponent<Image>(); img.sprite = data.icone; img.preserveAspect = true;
        }

        AdicionarTexto(card, "Name",   new Vector2(0.05f,0.50f), new Vector2(0.95f,0.64f), TextUtils.SemAcento(data.nomeEvolucao), 18, FontStyles.Bold,   data.corDestaque);
        AdicionarTexto(card, "Detail", new Vector2(0.05f,0.38f), new Vector2(0.95f,0.51f), data.raridade.ToString().ToUpper(),     12, FontStyles.Normal, data.corDestaque);
        AdicionarTexto(card, "Desc",   new Vector2(0.05f,0.06f), new Vector2(0.95f,0.38f), TextUtils.SemAcento(data.descricao),    13, FontStyles.Normal, Color.white);

        card.AddComponent<Button>();
        return card;
    }

    void AdicionarTexto(GameObject pai, string nome, Vector2 ancMin, Vector2 ancMax, string texto, float size, FontStyles style, Color cor)
    {
        var go = CriarFilho(nome, pai.transform);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text = texto; txt.fontSize = size; txt.fontStyle = style;
        txt.alignment = TextAlignmentOptions.Center; txt.color = cor;
        txt.textWrappingMode = TMPro.TextWrappingModes.Normal;
    }

    static GameObject CriarFilho(string nome, Transform pai)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static Color ObterCorRaridade(SkillRarity r)
    {
        switch (r)
        {
            case SkillRarity.Common:    return new Color(0.7f,0.7f,0.7f);
            case SkillRarity.Uncommon:  return new Color(0.2f,0.8f,0.2f);
            case SkillRarity.Rare:      return new Color(0.2f,0.4f,1f);
            case SkillRarity.Epic:      return new Color(0.6f,0.2f,0.8f);
            case SkillRarity.Legendary: return new Color(1f,0.5f,0f);
            default:                   return Color.white;
        }
    }
}
