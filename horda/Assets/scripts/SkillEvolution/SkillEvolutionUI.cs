using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillEvolutionUI : MonoBehaviour
{
    public static SkillEvolutionUI Instance { get; private set; }
    public GameObject cardPrefab;
    public float tempoEscolha = 20f;

    bool      visivel    = false;
    bool      selecionou = false;
    Coroutine contadorCoroutine;
    List<GameObject> cartasCriadas = new List<GameObject>();
    GameObject painelEvo;       // container transparente criado em runtime

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
        if (visivel) return; // já está exibindo ou animando — ignora chamada duplicada
        StartCoroutine(Mostrar(opcoes, callback));
    }

    // ── Fluxo principal ───────────────────────────────────────────────────────

    IEnumerator Mostrar(List<SkillEvolutionData> opcoes, System.Action<SkillEvolutionData> callback)
    {
        var ui = Object.FindFirstObjectByType<SkillChoiceUI>();
        if (ui == null) yield break;

        Time.timeScale = 0f;
        AudioListener.pause = true;
        visivel    = true;
        selecionou = false;

        // Limpa sessão anterior
        LimparTudo();

        // Encontra o Canvas raiz do SkillChoiceUI para herdar escala/resolução
        var canvas = ui.GetComponentInParent<Canvas>();
        if (canvas == null) canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) yield break;

        // Cria container fullscreen com fundo preto semi-transparente
        painelEvo = new GameObject("EvoPanel", typeof(RectTransform), typeof(Image));
        painelEvo.transform.SetParent(canvas.transform, false);
        painelEvo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
        var painelRT = painelEvo.GetComponent<RectTransform>();
        painelRT.anchorMin = Vector2.zero;
        painelRT.anchorMax = Vector2.one;
        painelRT.offsetMin = Vector2.zero;
        painelRT.offsetMax = Vector2.zero;

        // Canvas proprio para garantir que fica na frente de tudo
        var evoCanvas = painelEvo.AddComponent<Canvas>();
        evoCanvas.overrideSorting = true;
        evoCanvas.sortingOrder    = 999;
        painelEvo.AddComponent<GraphicRaycaster>();

        // Container horizontal das cartas (centrado na tela)
        var containerGO = new GameObject("EvoCardsRow", typeof(RectTransform));
        containerGO.transform.SetParent(painelEvo.transform, false);
        var contRT = containerGO.GetComponent<RectTransform>();
        contRT.anchorMin = new Vector2(0.5f, 0.5f);
        contRT.anchorMax = new Vector2(0.5f, 0.5f);
        contRT.pivot     = new Vector2(0.5f, 0.5f);
        contRT.sizeDelta = new Vector2(1200f, 500f);
        contRT.anchoredPosition = Vector2.zero;

        var hLayout = containerGO.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing             = ui.cardSpacing;
        hLayout.childAlignment      = TextAnchor.MiddleCenter;
        hLayout.padding             = new RectOffset(30, 30, 20, 20);
        hLayout.childControlWidth   = false;
        hLayout.childControlHeight  = false;
        hLayout.childForceExpandWidth  = false;
        hLayout.childForceExpandHeight = false;

        // 1) Cria todas as cartas primeiro (sem animar ainda)
        yield return null;
        for (int i = 0; i < opcoes.Count; i++)
        {
            var card = CriarCarta(ui, containerGO.transform, opcoes[i], callback);
            if (card == null) continue;
            cartasCriadas.Add(card);
        }

        // 2) Reconstrói o layout — agora as cartas estão nas posições corretas
        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(contRT);
        Canvas.ForceUpdateCanvases();
        yield return null; // mais um frame para o layout aplicar

        // 3) Só agora inicia animações — capturam posição já calculada pelo layout
        for (int i = 0; i < cartasCriadas.Count; i++)
        {
            cartasCriadas[i].AddComponent<EvoCardAnimador>();
            StartCoroutine(AnimarEntrada(cartasCriadas[i], i * 0.12f));
        }

        // Contador
        if (contadorCoroutine != null) StopCoroutine(contadorCoroutine);
        contadorCoroutine = StartCoroutine(ContadorEscolha(opcoes, callback));
    }

    void Selecionar(SkillEvolutionData opcao, System.Action<SkillEvolutionData> callback, GameObject cartaSelecionada = null)
    {
        if (selecionou) return;
        selecionou = true;
        if (contadorCoroutine != null) { StopCoroutine(contadorCoroutine); contadorCoroutine = null; }

        foreach (var c in cartasCriadas)
        {
            if (c == null) continue;
            var btn = c.GetComponent<Button>();
            if (btn == null) btn = c.GetComponentInChildren<Button>();
            if (btn != null) btn.interactable = false;
        }

        if (cartaSelecionada != null)
            CartaSelecaoEfeito.Executar(cartaSelecionada, cartasCriadas,
                () => StartCoroutine(Fechar(opcao, callback)));
        else
            StartCoroutine(Fechar(opcao, callback));
    }

    IEnumerator Fechar(SkillEvolutionData opcao, System.Action<SkillEvolutionData> callback)
    {
        yield return new WaitForSecondsRealtime(0.35f);

        LimparTudo();

        visivel             = false;
        Time.timeScale      = 1f;
        AudioListener.pause = false;

        callback?.Invoke(opcao);
    }

    IEnumerator ContadorEscolha(List<SkillEvolutionData> opcoes, System.Action<SkillEvolutionData> callback)
    {
        if (painelEvo == null) yield break;

        var go = new GameObject("EvoTimer", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(painelEvo.transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 50f);
        rt.sizeDelta        = new Vector2(160f, 50f);

        var txt = go.GetComponent<TextMeshProUGUI>();
        txt.fontSize  = 28;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;

        float restante = tempoEscolha;
        while (restante > 0f)
        {
            if (go == null) yield break;
            restante -= Time.unscaledDeltaTime;
            int seg   = Mathf.CeilToInt(Mathf.Max(0f, restante));
            txt.text  = seg.ToString();
            txt.color = restante < 5f ? Color.red : Color.white;
            yield return null;
        }

        if (go != null) Destroy(go);
        contadorCoroutine = null;
        if (!selecionou && opcoes.Count > 0)
            Selecionar(opcoes[Random.Range(0, opcoes.Count)], callback);
    }

    // ── Criação de carta ──────────────────────────────────────────────────────

    GameObject CriarCarta(SkillChoiceUI ui, Transform container, SkillEvolutionData data, System.Action<SkillEvolutionData> callback)
    {
        // Prefab: cardPrefab local → skillChoicePrefab → qualquer skill disponível → Resources
        GameObject prefab = null;
        if (cardPrefab != null)               prefab = cardPrefab;
        else if (ui.skillChoicePrefab != null) prefab = ui.skillChoicePrefab;
        else if (ui.allAvailableSkills != null)
            foreach (var s in ui.allAvailableSkills)
                if (s != null && s.cardPrefab != null) { prefab = s.cardPrefab; break; }
        if (prefab == null) prefab = Resources.Load<GameObject>("Cards/SkillCard_Auto");

        if (prefab == null)
        {
            Debug.LogError("SkillEvolutionUI: nenhum prefab de carta encontrado!");
            return null;
        }

        var card = Object.Instantiate(prefab, container);
        card.name = $"{data.nomeEvolucao}_Evo";
        card.SetActive(true);

        // Mesmo SetupCardTransform do SkillChoiceUI
        var rect = card.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localScale       = Vector3.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta        = ui.cardSize;
        }
        var le = card.GetComponent<LayoutElement>();
        if (le == null) le = card.AddComponent<LayoutElement>();
        le.preferredWidth  = ui.cardSize.x;
        le.preferredHeight = ui.cardSize.y;
        le.flexibleWidth   = 0;
        le.flexibleHeight  = 0;

        // Inicializa via SkillCardRuntimeManager com SkillData temporário
        var rm = card.GetComponent<SkillCardRuntimeManager>();
        if (rm != null)
        {
            var tempSkill = ScriptableObject.CreateInstance<SkillData>();
            tempSkill.skillName   = data.nomeEvolucao;
            tempSkill.description = data.descricao;
            tempSkill.icon        = data.icone;
            tempSkill.rarity      = data.raridade;
            tempSkill.element     = PlayerStats.Element.None;
            rm.InitializeRuntime(tempSkill);
            Destroy(tempSkill);
        }
        else
        {
            // Fallback manual
            foreach (var t in card.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                string n = t.name.ToLower();
                if      (n.Contains("name")  || n.Contains("nome")  || n.Contains("title"))
                    { t.text = $"<b>{data.nomeEvolucao}</b>"; t.color = data.corDestaque; }
                else if (n.Contains("desc")  || n.Contains("detail"))
                    { t.text = data.descricao; t.color = Color.white; }
                else if (n.Contains("rarity") || n.Contains("rarid"))
                    { t.text = data.raridade.ToString().ToUpper(); t.color = data.corDestaque; }
                t.textWrappingMode = TMPro.TextWrappingModes.Normal;
            }
            foreach (var img in card.GetComponentsInChildren<Image>(true))
                if (img.name == "IconInner" && data.icone != null)
                    { img.sprite = data.icone; img.color = Color.white; break; }
        }

        // Fundo da carta
        var cardBg = card.GetComponent<Image>();
        if (cardBg != null)
        {
            Sprite spFundo = CarregarSprite("Assets/assets/UI/skill_card/cartaevolução.ase", "cartaevolução");
            if (spFundo != null) { cardBg.sprite = spFundo; cardBg.color = Color.white; cardBg.type = Image.Type.Simple; }
        }

        // Slot do ícone
        Image slotImg = null;
        var slotT = card.transform.Find("IconArea/IconImageSlot");
        if (slotT != null) slotImg = slotT.GetComponent<Image>();
        if (slotImg == null)
            foreach (var img in card.GetComponentsInChildren<Image>(true))
                if (img.name == "IconImageSlot") { slotImg = img; break; }
        if (slotImg != null)
        {
            Sprite spSlot = CarregarSprite("Assets/assets/UI/skill_card/slotevolução.ase", "slotevolução");
            if (spSlot != null) { slotImg.sprite = spSlot; slotImg.color = Color.white; slotImg.type = Image.Type.Simple; }
        }

        // IconInner (fundo interno do slot)
        Image innerImg = null;
        var innerT2 = card.transform.Find("IconArea/IconImageSlot/IconInner");
        if (innerT2 != null) innerImg = innerT2.GetComponent<Image>();
        if (innerImg == null)
            foreach (var img in card.GetComponentsInChildren<Image>(true))
                if (img.name == "IconInner") { innerImg = img; break; }
        if (innerImg != null && data.icone == null)
        {
            Sprite spSlot2 = CarregarSprite("Assets/assets/UI/skill_card/slotevolução.ase", "slotevolução");
            if (spSlot2 != null) { innerImg.sprite = spSlot2; innerImg.color = Color.white; innerImg.type = Image.Type.Simple; }
        }

        // Botão
        var button = card.GetComponent<Button>();
        if (button == null) button = card.GetComponentInChildren<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            var local      = data;
            var cardCapture = card;
            button.onClick.AddListener(() => Selecionar(local, callback, cardCapture));
            button.interactable = true;
        }

        return card;
    }

    Sprite CarregarSprite(string path, string spriteName)
    {
#if UNITY_EDITOR
        var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        Sprite primeiro = null;
        foreach (var a in all)
        {
            if (a is Sprite s)
            {
                if (s.name == spriteName) return s;
                if (primeiro == null) primeiro = s;
            }
        }
        if (primeiro != null) return primeiro;
#endif
        return null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    void LimparTudo()
    {
        foreach (var c in cartasCriadas) if (c != null) Destroy(c);
        cartasCriadas.Clear();

        if (painelEvo != null) { Destroy(painelEvo); painelEvo = null; }
    }

    IEnumerator AnimarEntrada(GameObject card, float delay)
    {
        // Sempre espera ao menos 1 frame + o stagger
        yield return null;
        for (float t = 0f; t < delay; t += Time.unscaledDeltaTime) yield return null;
        if (card == null) yield break;

        var rt = card.GetComponent<RectTransform>();
        Vector2 posAlvo = rt != null ? rt.anchoredPosition : Vector2.zero;

        card.transform.localScale = Vector3.zero;
        if (rt != null) rt.anchoredPosition = posAlvo + new Vector2(0f, -120f);

        for (float e = 0f; e < 0.4f; e += Time.unscaledDeltaTime)
        {
            if (card == null) yield break;
            float p = e / 0.4f;
            float ease = 1f - Mathf.Pow(1f - p, 3f);
            float bounce = 1f + Mathf.Sin(p * Mathf.PI) * 0.18f;
            card.transform.localScale = Vector3.one * (ease * bounce);
            if (rt != null) rt.anchoredPosition = Vector2.LerpUnclamped(posAlvo + new Vector2(0f, -120f), posAlvo, ease);
            yield return null;
        }
        if (card != null)
        {
            card.transform.localScale = Vector3.one;
            if (rt != null) rt.anchoredPosition = posAlvo;
            // Sinaliza ao animador que a entrada acabou
            var anim = card.GetComponent<EvoCardAnimador>();
            if (anim != null) anim.EntradaConcluida(posAlvo);
        }
    }
}

// ── Animador dinâmico exclusivo das cartas de evolução ────────────────────────
public class EvoCardAnimador : MonoBehaviour
{
    RectTransform rt;
    bool          entradaPronta;
    Vector2       posBase;
    float         floatFase;
    float         floatTimer;

    void Awake()
    {
        rt        = GetComponent<RectTransform>();
        floatFase = Random.Range(0f, Mathf.PI * 2f);
    }

    public void EntradaConcluida(Vector2 posicaoBase)
    {
        posBase      = posicaoBase;
        entradaPronta = true;
    }

    void Update()
    {
        if (!entradaPronta || rt == null) return;

        floatTimer += Time.unscaledDeltaTime;

        bool hover = RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, null);

        // ── Escala no hover ───────────────────────────────────────────────────
        float escalaAlvo  = hover ? 1.12f : 1f;
        float escalaAtual = transform.localScale.x;
        float novaEscala  = Mathf.Lerp(escalaAtual, escalaAlvo, Time.unscaledDeltaTime * 10f);
        transform.localScale = Vector3.one * novaEscala;

        // ── Flutuação vertical (pausa enquanto hovering) ──────────────────────
        if (!hover)
        {
            float yOffset = Mathf.Sin(floatTimer * 1.4f + floatFase) * 7f;
            rt.anchoredPosition = new Vector2(posBase.x, posBase.y + yOffset);
        }
        else
        {
            rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, posBase, Time.unscaledDeltaTime * 8f);
        }

        // ── Tilt 3D baseado na posição do mouse sobre a carta ─────────────────
        if (hover)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rt, Input.mousePosition, null, out var localPt);
            float tiltX = -(localPt.y / (rt.rect.height * 0.5f)) * 10f;
            float tiltY =  (localPt.x / (rt.rect.width  * 0.5f)) * 10f;
            var alvoRot = Quaternion.Euler(tiltX, tiltY, 0f);
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation, alvoRot, Time.unscaledDeltaTime * 9f);
        }
        else
        {
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation, Quaternion.identity, Time.unscaledDeltaTime * 6f);
        }
    }
}
