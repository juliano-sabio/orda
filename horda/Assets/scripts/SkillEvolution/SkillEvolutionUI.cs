using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Reutiliza o painel, container e prefab do SkillChoiceUI para garantir visual idêntico.
public class SkillEvolutionUI : MonoBehaviour
{
    public static SkillEvolutionUI Instance { get; private set; }
    public GameObject cardPrefab;   // atribuído pelo CriarCartaEvolucaoPrefab (não usado diretamente — SkillChoiceUI.skillChoicePrefab tem prioridade)
    public float tempoEscolha = 20f;

    bool      visivel    = false;
    bool      selecionou = false;
    Coroutine contadorCoroutine;
    List<GameObject> cartasCriadas = new List<GameObject>();
    Canvas    painelCanvas;
    GameObject containerEvo;  // container de cartas criado por nós dentro do choicePanel

    public bool Visivel => visivel;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── API pública ──────────────────────────────────────────────────────────

    public void MostrarOpcoes(List<SkillEvolutionData> opcoes, System.Action<SkillEvolutionData> callback)
    {
        if (opcoes == null || opcoes.Count == 0) return;
        StartCoroutine(Mostrar(opcoes, callback));
    }

    // ── Fluxo ─────────────────────────────────────────────────────────────────

    IEnumerator Mostrar(List<SkillEvolutionData> opcoes, System.Action<SkillEvolutionData> callback)
    {
        var ui = Object.FindFirstObjectByType<SkillChoiceUI>();
        if (ui == null) yield break;

        Time.timeScale = 0f;
        AudioListener.pause = true;
        visivel    = true;
        selecionou = false;

        // Reutiliza o mesmo painel do SkillChoiceUI
        if (ui.choicePanel != null)
        {
            ui.choicePanel.SetActive(true);

            // Garante que o painel aparece na frente de tudo
            painelCanvas = ui.choicePanel.GetComponent<Canvas>();
            if (painelCanvas == null)
            {
                painelCanvas = ui.choicePanel.AddComponent<Canvas>();
                ui.choicePanel.AddComponent<GraphicRaycaster>();
            }
            painelCanvas.overrideSorting = true;
            painelCanvas.sortingOrder    = 999;
        }

        // Atualiza o título
        if (ui.titleTextTMP != null) ui.titleTextTMP.text = "ESCOLHA UMA EVOLUCAO";
        else if (ui.titleText != null) ui.titleText.text  = "ESCOLHA UMA EVOLUCAO";

        // Limpa cartas anteriores
        LimparCartas(ui);

        // Container próprio dentro do painel (não depende de skillsContainer)
        if (containerEvo != null) Destroy(containerEvo);
        containerEvo = new GameObject("EvoCardsContainer", typeof(RectTransform));
        containerEvo.transform.SetParent(ui.choicePanel.transform, false);
        var contRT = containerEvo.GetComponent<RectTransform>();
        contRT.anchorMin = new Vector2(0.5f, 0.5f); contRT.anchorMax = new Vector2(0.5f, 0.5f);
        contRT.pivot     = new Vector2(0.5f, 0.5f);
        contRT.sizeDelta = new Vector2(1200f, 500f);
        contRT.anchoredPosition = new Vector2(0f, -20f);
        var evoLayout = containerEvo.AddComponent<HorizontalLayoutGroup>();
        evoLayout.spacing = ui.cardSpacing; evoLayout.childAlignment = TextAnchor.MiddleCenter;
        evoLayout.padding = new RectOffset(30, 30, 20, 20);
        evoLayout.childControlWidth = false; evoLayout.childControlHeight = false;
        evoLayout.childForceExpandWidth = false; evoLayout.childForceExpandHeight = false;

        // Cria uma carta por opção
        int idx = 0;
        foreach (var op in opcoes)
        {
            var card = CriarCarta(ui, op);
            if (card == null) { idx++; continue; }

            card.transform.SetParent(containerEvo.transform, false);
            cartasCriadas.Add(card);

            // Bloqueia raycast nos filhos
            foreach (var g in card.GetComponentsInChildren<Graphic>(true))
                g.raycastTarget = false;
            foreach (var b in card.GetComponentsInChildren<Button>(true))
            { b.onClick.RemoveAllListeners(); b.interactable = false; }

            // Overlay de clique
            var ov = new GameObject("EvoOverlay", typeof(RectTransform), typeof(Image), typeof(Button));
            ov.transform.SetParent(card.transform, false);
            var ovRT = ov.GetComponent<RectTransform>();
            ovRT.anchorMin = Vector2.zero; ovRT.anchorMax = Vector2.one;
            ovRT.offsetMin = ovRT.offsetMax = Vector2.zero;
            ov.GetComponent<Image>().color = Color.clear;
            ov.GetComponent<Image>().raycastTarget = true;
            var local = op;
            ov.GetComponent<Button>().onClick.AddListener(() => Selecionar(local, callback, ui));

            // Hover + animação de entrada
            card.AddComponent<CartaSkillAnimador>().Iniciar(card);
            StartCoroutine(AnimarEntrada(card, idx * 0.1f));
            idx++;
        }

        yield return null;
        if (containerEvo != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerEvo.GetComponent<RectTransform>());

        // Contador
        if (contadorCoroutine != null) StopCoroutine(contadorCoroutine);
        contadorCoroutine = StartCoroutine(Contador(ui, opcoes, callback));
    }

    void Selecionar(SkillEvolutionData opcao, System.Action<SkillEvolutionData> callback, SkillChoiceUI ui)
    {
        if (selecionou) return;
        selecionou = true;
        if (contadorCoroutine != null) { StopCoroutine(contadorCoroutine); contadorCoroutine = null; }
        StartCoroutine(Fechar(opcao, callback, ui));
    }

    IEnumerator Fechar(SkillEvolutionData opcao, System.Action<SkillEvolutionData> callback, SkillChoiceUI ui)
    {
        yield return new WaitForSecondsRealtime(0.15f);

        LimparCartas(ui);

        // Remove o Canvas de override que adicionamos
        if (painelCanvas != null)
        {
            painelCanvas.overrideSorting = false;
            painelCanvas.sortingOrder    = 0;
        }

        if (ui.choicePanel != null) ui.choicePanel.SetActive(false);
        visivel = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        callback?.Invoke(opcao);
    }

    IEnumerator Contador(SkillChoiceUI ui, List<SkillEvolutionData> opcoes, System.Action<SkillEvolutionData> callback)
    {
        // Cria texto de contador no painel
        var go = new GameObject("EvoTimer", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(ui.choicePanel != null ? ui.choicePanel.transform : ui.skillsContainer, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f); rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 30f); rt.sizeDelta = new Vector2(120f, 45f);
        var txt = go.GetComponent<TextMeshProUGUI>();
        txt.fontSize = 28; txt.fontStyle = FontStyles.Bold; txt.alignment = TextAlignmentOptions.Center;

        for (float r = tempoEscolha; r > 0f; r -= Time.unscaledDeltaTime)
        {
            if (go == null) yield break;
            txt.text  = Mathf.CeilToInt(r).ToString();
            txt.color = r < 5f ? Color.red : Color.white;
            yield return null;
        }

        if (go != null) Destroy(go);
        contadorCoroutine = null;
        if (!selecionou && opcoes.Count > 0)
            Selecionar(opcoes[Random.Range(0, opcoes.Count)], callback, ui);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────


    GameObject CriarCarta(SkillChoiceUI ui, SkillEvolutionData data)
    {
        // Prioridade: skillChoicePrefab do SkillChoiceUI → cardPrefab local → Resources
        var prefab = ui.skillChoicePrefab
                  ?? cardPrefab
                  ?? Resources.Load<GameObject>("CartaEvolucao");

        GameObject card;
        if (prefab != null)
        {
            card = Object.Instantiate(prefab);

            // Desativa SkillCardRuntimeManager para não sobrescrever nossos dados
            var rm = card.GetComponent<SkillCardRuntimeManager>();
            if (rm != null) rm.enabled = false;

            // Força todos os gráficos visíveis
            foreach (var g in card.GetComponentsInChildren<Graphic>(true))
            {
                if (g.color.a < 0.05f) continue;
                var c = g.color; c.a = 1f; g.color = c;
            }

            // Popula textos
            foreach (var t in card.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                string n = t.name.ToLower();
                if      (n.Contains("name") || n.Contains("nome") || n.Contains("title"))
                    { t.text = $"<b>{TextUtils.SemAcento(data.nomeEvolucao)}</b>"; t.color = data.corDestaque; }
                else if (n.Contains("desc") || n.Contains("detail"))
                    { t.text = TextUtils.SemAcento(data.descricao); t.color = Color.white; }
                else if (n.Contains("stats") || n.Contains("bonus") || n.Contains("atq")
                      || n.Contains("rarity") || n.Contains("rarid"))
                    { t.text = data.raridade.ToString().ToUpper(); t.color = data.corDestaque; }
                t.textWrappingMode = TMPro.TextWrappingModes.Normal;
            }

            // Ícone
            Image innerImg = null;
            var found = card.transform.Find("IconArea/IconImageSlot/IconInner");
            if (found != null) innerImg = found.GetComponent<Image>();
            if (innerImg == null)
                foreach (var img in card.GetComponentsInChildren<Image>(true))
                    if (img.name == "IconInner") { innerImg = img; break; }
            if (innerImg != null && data.icone != null)
                { innerImg.sprite = data.icone; innerImg.color = Color.white; }
        }
        else
        {
            // Fallback mínimo
            card = new GameObject("EvoCard", typeof(RectTransform), typeof(Image), typeof(Button));
            card.GetComponent<Image>().color = new Color(0.08f, 0.06f, 0.12f, 1f);
        }

        var le = card.GetComponent<LayoutElement>() ?? card.AddComponent<LayoutElement>();
        le.preferredWidth  = ui.cardSize.x;
        le.preferredHeight = ui.cardSize.y;

        return card;
    }

    void LimparCartas(SkillChoiceUI ui)
    {
        foreach (var c in cartasCriadas) if (c != null) Destroy(c);
        cartasCriadas.Clear();

        if (containerEvo != null) { Destroy(containerEvo); containerEvo = null; }

        if (ui?.choicePanel != null)
        {
            var timer = ui.choicePanel.transform.Find("EvoTimer");
            if (timer != null) Destroy(timer.gameObject);
        }
    }

    IEnumerator AnimarEntrada(GameObject card, float delay)
    {
        for (float t = 0f; t < delay; t += Time.unscaledDeltaTime) yield return null;
        if (card == null) yield break;
        card.transform.localScale = Vector3.zero;
        for (float e = 0f; e < 0.28f; e += Time.unscaledDeltaTime)
        {
            if (card == null) yield break;
            float p = e / 0.28f;
            float s = (1f - Mathf.Pow(1f - p, 3f)) * (1f + Mathf.Sin(p * Mathf.PI) * 0.13f);
            card.transform.localScale = Vector3.one * s;
            yield return null;
        }
        if (card != null) card.transform.localScale = Vector3.one;
    }
}
