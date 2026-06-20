using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SkillChoiceUI : MonoBehaviour
{
    [Header("Referências UI")]
    public GameObject choicePanel;
    public Text titleText;
    public TextMeshProUGUI titleTextTMP;
    public Transform skillsContainer;
    public GameObject skillChoicePrefab;
    public Button confirmButton;

    [Header("Configurações")]
    public float autoCloseDelay = 2f;
    public bool pauseGameDuringChoice = true;
    public int numberOfSkillsToShow = 3;

    [Header("Layout Horizontal")]
    public bool useHorizontalLayout = true;
    public float cardSpacing = 30f;
    public Vector2 cardSize = new Vector2(300f, 450f);

    [Header("Todas as Skills Disponíveis")]
    public List<SkillData> allAvailableSkills = new List<SkillData>();

    [Header("Tempo de Escolha")]
    [Tooltip("Segundos que o player tem para escolher antes de fechar automaticamente")]
    public float tempoEscolha = 20f;

    // Quando true, a próxima chamada mostra apenas skills de ataque (resetado automaticamente)
    [HideInInspector] public bool somenteSkillsDeAtaque = false;
    // Quando true, a próxima chamada mostra apenas skills de defesa/passiva (resetado automaticamente)
    [HideInInspector] public bool somenteSkillsDeDefesa = false;

    public List<SkillData> currentChoices;
    private System.Action<SkillData> onSkillChosen;
    private List<GameObject> currentButtons = new List<GameObject>();
    private float previousTimeScale;
    private Coroutine contadorCoroutine;
    private int skillCardIndex = 0;
    private GameObject overlayEscuro;

    void Awake()
    {
        if (choicePanel != null)
        {
            choicePanel.SetActive(false);
        }
    }

    void Start()
    {
        StartCoroutine(InitializeWithDelay());
    }

    void Update()
    {
        if (currentButtons.Count == 0 || choicePanel == null || !choicePanel.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            skillCardIndex = (skillCardIndex - 1 + currentButtons.Count) % currentButtons.Count;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            skillCardIndex = (skillCardIndex + 1) % currentButtons.Count;
        else if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            if (skillCardIndex >= 0 && skillCardIndex < currentChoices.Count)
                OnSkillSelected(currentChoices[skillCardIndex]);
        }
    }


    private IEnumerator InitializeWithDelay()
    {
        yield return new WaitForSeconds(0.5f);
        SetupHorizontalLayout();

        // 🎯 CARREGAR SKILLS DO SKILLMANAGER
        LoadSkillsFromSkillManager();

    }

    // 🆕 MÉTODO: Carregar skills do SkillManager
    private void LoadSkillsFromSkillManager()
    {
        SkillManager skillManager = SkillManager.Instance;
        if (skillManager == null)
            skillManager = FindFirstObjectByType<SkillManager>();

        if (skillManager != null)
        {
            List<SkillData> managerSkills = skillManager.GetAvailableSkills();
            if (managerSkills != null && managerSkills.Count > 0)
            {
                allAvailableSkills.Clear();
                allAvailableSkills.AddRange(managerSkills);
            }
        }
        else
        {
            Debug.LogError("❌ SkillManager não encontrado em nenhuma busca!");
        }
    }

    private void SetupHorizontalLayout()
    {
        if (skillsContainer == null || !useHorizontalLayout) return;

        HorizontalLayoutGroup layout = skillsContainer.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
            layout = skillsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();

        layout.spacing = cardSpacing;
        layout.padding = new RectOffset(30, 30, 20, 20);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter sizeFitter = skillsContainer.GetComponent<ContentSizeFitter>();
        if (sizeFitter != null)
        {
            DestroyImmediate(sizeFitter);
        }

        RectTransform containerRect = skillsContainer as RectTransform;
        if (containerRect != null)
        {
            containerRect.sizeDelta = new Vector2(1200f, 500f);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
        }

    }

    // 🆕 MÉTODO PRINCIPAL CORRIGIDO: Mostra 3 skills aleatórias
    public void ShowRandomSkillChoice(System.Action<SkillData> callback)
    {

        // 🎯 ATUALIZAR SKILLS DO SKILLMANAGER SEMPRE ANTES DE MOSTRAR
        LoadSkillsFromSkillManager();

        // Filtro de tipo de skill por slot
        // Remove entradas nulas antes de filtrar
        allAvailableSkills.RemoveAll(s => s == null);

        if (somenteSkillsDeAtaque)
        {
            var filtradas = allAvailableSkills.FindAll(s => s != null && s.EhSkillDeAtaque());
            if (filtradas.Count > 0) allAvailableSkills = filtradas;
            somenteSkillsDeAtaque = false;
        }
        else if (somenteSkillsDeDefesa)
        {
            var filtradas = allAvailableSkills.FindAll(s => s != null && !s.EhSkillDeAtaque());
            if (filtradas.Count > 0) allAvailableSkills = filtradas;
            somenteSkillsDeDefesa = false;
        }

        if (allAvailableSkills == null || allAvailableSkills.Count == 0)
        {
            Debug.LogError("❌ Lista de skills disponíveis vazia!");
            return;
        }

        // 🎯 SELECIONAR 3 SKILLS ALEATÓRIAS (máximo 3 independente do Inspector)
        List<SkillData> randomSkills = SelectRandomSkills(Mathf.Min(numberOfSkillsToShow, 3));

        foreach (var skill in randomSkills)
        {
        }

        // 🎯 AGORA mostra as 3 skills para o player escolher
        ShowSkillChoice(randomSkills, callback);
    }

    // 🆕 MÉTODO: Seleciona N skills aleatórias
    private List<SkillData> SelectRandomSkills(int count)
    {
        List<SkillData> selectedSkills = new List<SkillData>();

        // Se não tem skills suficientes, mostra todas
        if (allAvailableSkills.Count <= count)
        {
            selectedSkills.AddRange(allAvailableSkills);
        }
        else
        {
            // 🎯 Embaralhar e selecionar
            List<SkillData> shuffledSkills = new List<SkillData>(allAvailableSkills);
            ShuffleSkills(shuffledSkills);

            for (int i = 0; i < count; i++)
            {
                selectedSkills.Add(shuffledSkills[i]);
            }
        }

        return selectedSkills;
    }

    // 🆕 MÉTODO: Embaralhar skills
    private void ShuffleSkills(List<SkillData> skills)
    {
        for (int i = skills.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            SkillData temp = skills[i];
            skills[i] = skills[randomIndex];
            skills[randomIndex] = temp;
        }
    }

    // MÉTODO ORIGINAL (mantido para compatibilidade)
    public void ShowSkillChoice(List<SkillData> skills, System.Action<SkillData> callback)
    {

        if (skills == null || skills.Count == 0)
        {
            Debug.LogError("❌ Lista de skills vazia!");
            return;
        }

        // 🎯 GARANTIR que o GameObject está ATIVO
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("⚠️ SkillChoiceUI inativo! Ativando...");
            gameObject.SetActive(true);
        }

        // 🎯 GARANTIR que o Canvas está ATIVO
        if (choicePanel != null && !choicePanel.activeInHierarchy)
        {
            choicePanel.SetActive(true);
        }

        currentChoices  = skills;
        onSkillChosen   = callback;
        skillCardIndex  = 0;

        PauseGame();
        if (contadorCoroutine != null) StopCoroutine(contadorCoroutine);
        contadorCoroutine = StartCoroutine(ContadorEscolha());
        ClearSkillButtons();

        if (choicePanel != null)
        {
            choicePanel.SetActive(true);
        }

        GarantirOverlayEscuro();

        UpdateTitleText();

        // 🎯 INICIAR COROUTINE COM SEGURANÇA
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(CreateSkillButtonsWithSpecificPrefabs(skills));
        }
        else
        {
            Debug.LogError("❌ Não pode iniciar coroutine - GameObject inativo!");
            StartCoroutine(CreateSkillButtonsWithSpecificPrefabs(skills));
        }
    }

    private IEnumerator CreateSkillButtonsWithSpecificPrefabs(List<SkillData> skills)
    {
        yield return null; // aguarda 1 frame (não WaitForEndOfFrame para evitar conflito com TMPro)

        for (int i = 0; i < skills.Count; i++)
        {
            CreateSkillButtonWithSpecificPrefab(skills[i], i);
        }

        yield return null;
        yield return StartCoroutine(ForceLayoutRefresh());
    }

    private void CreateSkillButtonWithSpecificPrefab(SkillData skill, int index)
    {
        GameObject cardPrefabToUse = skill.cardPrefab;

        if (cardPrefabToUse == null)
        {
            cardPrefabToUse = skillChoicePrefab;
            Debug.LogWarning($"⚠️ Skill {skill.skillName} não tem cardPrefab! Usando fallback genérico");
        }

        if (cardPrefabToUse == null)
        {
            cardPrefabToUse = Resources.Load<GameObject>("Cards/SkillCard_Auto");
            Debug.LogWarning($"⚠️ Usando card automático do Resources para: {skill.skillName}");
        }

        if (cardPrefabToUse == null)
        {
            Debug.LogError($"🚨 Criando card de emergência para: {skill.skillName}");
            CreateEmergencySkillButton(skill, index);
            return;
        }

        GameObject cardObj = Instantiate(cardPrefabToUse, skillsContainer);
        cardObj.name = $"{skill.skillName}_Instance";
        cardObj.SetActive(true);
        currentButtons.Add(cardObj);

        SetupCardTransform(cardObj);
        InitializeCardWithSkillData(cardObj, skill);
        cardObj.AddComponent<CartaSkillAnimador>().Iniciar(cardObj);

    }

    private void InitializeCardWithSkillData(GameObject cardObj, SkillData skill)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Debug.LogWarning($"🚨 BLOQUEADO: Tentativa de modificar fora do runtime - {skill.skillName}");
            return;
        }
#endif

        if (!Application.isPlaying)
        {
            Debug.LogError($"❌ TENTATIVA PERIGOSA: Modificar em Editor - {skill.skillName}");
            return;
        }

        // Tenta usar o SkillCardRuntimeManager primeiro (mais robusto)
        var runtimeManager = cardObj.GetComponent<SkillCardRuntimeManager>();
        if (runtimeManager != null)
            runtimeManager.InitializeRuntime(skill);
        else
            SetupCardTextsOnly(cardObj, skill);

        // Conecta o botão de seleção
        Button button = cardObj.GetComponent<Button>();
        if (button == null) button = cardObj.GetComponentInChildren<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnSkillSelected(skill));
            button.interactable = true;
        }

    }

    private void SetupCardTextsOnly(GameObject cardObj, SkillData skill)
    {
        TextMeshProUGUI[] textComponents = cardObj.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (TextMeshProUGUI text in textComponents)
        {
            if (text.name.Contains("Name") || text.name.Contains("Nome") || text.name.Contains("Title"))
            {
                text.text = $"<b>{skill.GetDisplayName()}</b>";
            }
            else if (text.name.Contains("Desc") || text.name.Contains("Description") || text.name.Contains("Detail"))
            {
                text.text = skill.GetDisplayDescription();
                AjustarTextoParaCaber(text, 7f);
            }
            else if (text.name.Contains("Stats") || text.name.Contains("Status") || text.name.Contains("Bonus"))
            {
                text.text = GetManualStatsText(skill);
                AjustarTextoParaCaber(text, 7f);
            }
            else if (text.name.Contains("Rarity") || text.name.Contains("Rarid") || text.name.Contains("Rare"))
            {
                text.gameObject.SetActive(false); // badge de raridade removido dos cards de skill
            }
        }

        // esconde a borda/caixa laranja da raridade também
        foreach (var img in cardObj.GetComponentsInChildren<Image>(true))
            if (img.name.Contains("RarityBorder") || img.name.Contains("RarityArea"))
                img.gameObject.SetActive(false);

        if (skill.icon != null)
        {
            // Prioridade: IconInner (dentro do slot_frame), senão qualquer Image com "Icon"/"Image"
            var innerT = cardObj.transform.Find("IconArea/IconImageSlot/IconInner");
            Image iconImg = innerT != null ? innerT.GetComponent<Image>() : null;
            if (iconImg == null)
            {
                // Busca apenas "IconInner" — evita sobrescrever slot_frame em IconImageSlot
                foreach (var img in cardObj.GetComponentsInChildren<Image>())
                {
                    if (img.name == "IconInner") { iconImg = img; break; }
                }
            }
            if (iconImg != null)
            {
                iconImg.sprite = skill.icon;
                iconImg.type = Image.Type.Simple;
                iconImg.preserveAspect = true;
            }
        }
    }

    // 🆕 CORREÇÃO: Método para obter ícone do elemento baseado no PlayerStats.Element
    private string GetElementIcon(PlayerStats.Element element)
    {
        switch (element)
        {
            case PlayerStats.Element.Fire: return "🔥";
            case PlayerStats.Element.Ice: return "❄️";
            case PlayerStats.Element.Lightning: return "";
            case PlayerStats.Element.Poison: return "";
            case PlayerStats.Element.Earth: return "";
            case PlayerStats.Element.Wind: return "";
            case PlayerStats.Element.None: return "";
            default: return "*";
        }
    }

    private void AplicarEstileDarkFantasyCard(GameObject cardObj)
    {
        // Estilo dark fantasy nos textos
        foreach (var txt in cardObj.GetComponentsInChildren<TMPro.TextMeshProUGUI>())
        {
            if (txt.name.Contains("Name") || txt.name.Contains("Title"))
                txt.color = new Color(0.95f, 0.82f, 0.40f);
            else if (txt.name.Contains("Rarity") || txt.name.Contains("Raridade"))
                txt.color = new Color(0.80f, 0.55f, 0.25f);
            else
                txt.color = new Color(0.90f, 0.82f, 0.65f);
        }
        // Borda dourada fina no card
        var border = new GameObject("DarkBorder");
        border.transform.SetParent(cardObj.transform, false);
        var bImg = border.AddComponent<Image>();
        bImg.color = new Color(0.78f, 0.66f, 0.25f, 0.5f);
        var bRT = border.GetComponent<RectTransform>();
        bRT.anchorMin = Vector2.zero; bRT.anchorMax = Vector2.one;
        bRT.offsetMin = new Vector2(-2f, -2f); bRT.offsetMax = new Vector2(2f, 2f);
        border.transform.SetAsFirstSibling();
    }

    private void SetupCardTransform(GameObject cardObj)
    {
        RectTransform rect = cardObj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localScale = Vector3.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = cardSize;
        }

        LayoutElement layoutElem = cardObj.GetComponent<LayoutElement>();
        if (layoutElem == null)
        {
            layoutElem = cardObj.AddComponent<LayoutElement>();
        }
        layoutElem.preferredWidth = cardSize.x;
        layoutElem.preferredHeight = cardSize.y;
        layoutElem.flexibleWidth = 0;
        layoutElem.flexibleHeight = 0;
    }

    // Garante um fundo preto semi-transparente cobrindo a tela toda, atrás dos cards.
    private void GarantirOverlayEscuro()
    {
        if (choicePanel == null) return;

        Canvas canvas = choicePanel.GetComponentInParent<Canvas>();
        canvas = canvas != null ? canvas.rootCanvas : null;
        if (canvas == null) return;

        // ancestral do painel que fica logo abaixo do Canvas (para ordenar atrás dele)
        Transform topo = choicePanel.transform;
        while (topo.parent != null && topo.parent != canvas.transform) topo = topo.parent;

        if (overlayEscuro == null)
        {
            overlayEscuro = new GameObject("OverlayEscuro", typeof(RectTransform), typeof(Image));
            overlayEscuro.transform.SetParent(canvas.transform, false);
            var rt = overlayEscuro.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var img = overlayEscuro.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.6f);
            img.raycastTarget = true; // bloqueia cliques no gameplay atrás
        }

        overlayEscuro.transform.SetParent(canvas.transform, false);
        overlayEscuro.SetActive(true);
        // posiciona imediatamente atrás do painel de escolha
        int idxPainel = topo.GetSiblingIndex();
        overlayEscuro.transform.SetSiblingIndex(Mathf.Max(0, idxPainel));

        // desliga o backdrop escuro antigo do painel (era uma 2ª camada semi-transparente)
        var bgAntigo = choicePanel.transform.Find("background");
        if (bgAntigo != null && bgAntigo.gameObject.activeSelf)
            bgAntigo.gameObject.SetActive(false);
    }

    private void CreateEmergencySkillButton(SkillData skill, int index)
    {
        // ── Root card ──────────────────────────────────────────────────────────
        GameObject cardObj = new GameObject($"EmergencyCard_{skill.skillName}",
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        cardObj.transform.SetParent(skillsContainer);
        cardObj.SetActive(true);
        currentButtons.Add(cardObj);

        // Background: carta_frame simples esticado para preencher o componente
        Image bgImg = cardObj.GetComponent<Image>();
        Sprite cartaFrame = CarregarSprite("Assets/assets/UI/skill_card/cartaskill.png", "carta_frame");
        if (cartaFrame != null) { bgImg.sprite = cartaFrame; bgImg.color = Color.white; }
        else                    { bgImg.color = new Color(0.07f, 0.05f, 0.10f, 0.97f); }
        bgImg.type = Image.Type.Simple;
        bgImg.preserveAspect = false;
        bgImg.raycastTarget = true;

        SetupCardTransform(cardObj);

        // ── IconArea: faixa do topo que serve como container para o slot ────────
        var iconArea = new GameObject("IconArea", typeof(RectTransform));
        iconArea.transform.SetParent(cardObj.transform, false);
        var iaRT = iconArea.GetComponent<RectTransform>();
        iaRT.anchorMin = new Vector2(0f, 0.68f); iaRT.anchorMax = new Vector2(1f, 0.97f);
        iaRT.anchoredPosition = Vector2.zero; iaRT.sizeDelta = Vector2.zero;

        // IconImageSlot: stretch preenchendo a IconArea com margem pequena
        var slotGO = new GameObject("IconImageSlot", typeof(RectTransform), typeof(Image));
        slotGO.transform.SetParent(iconArea.transform, false);
        var slotRT = slotGO.GetComponent<RectTransform>();
        slotRT.anchorMin = new Vector2(0.05f, 0.05f); slotRT.anchorMax = new Vector2(0.95f, 0.95f);
        slotRT.pivot = new Vector2(0.5f, 0.5f);
        slotRT.anchoredPosition = Vector2.zero; slotRT.sizeDelta = Vector2.zero;
        var slotImg = slotGO.GetComponent<Image>();
        Sprite slotFrame = CarregarSprite("Assets/assets/UI/skill_card/cartaskill.png", "slot_frame");
        if (slotFrame != null) { slotImg.sprite = slotFrame; slotImg.type = Image.Type.Sliced; slotImg.fillCenter = false; }
        else { slotImg.color = new Color(0.6f, 0.1f, 0.1f, 0.5f); }
        slotImg.color = Color.white;
        slotImg.raycastTarget = false;

        // IconInner: ícone da skill preenchendo o interior do slot
        var innerGO = new GameObject("IconInner", typeof(RectTransform), typeof(Image));
        innerGO.transform.SetParent(slotGO.transform, false);
        var innerRT = innerGO.GetComponent<RectTransform>();
        innerRT.anchorMin = new Vector2(0.1f, 0.1f); innerRT.anchorMax = new Vector2(0.9f, 0.9f);
        innerRT.anchoredPosition = Vector2.zero; innerRT.sizeDelta = Vector2.zero;
        var innerImg = innerGO.GetComponent<Image>();
        innerImg.preserveAspect = true; innerImg.raycastTarget = false;
        if (skill.icon != null) { innerImg.sprite = skill.icon; }

        // ── NameArea ──────────────────────────────────────────────────────────
        CriarTextoArea(cardObj, "NameArea", "NameText", $"<b>{skill.GetDisplayName()}</b>",
            new Vector2(0f, 0.50f), new Vector2(1f, 0.68f), 14, new Color(0.95f, 0.82f, 0.40f), true);

        // ── DescArea ──────────────────────────────────────────────────────────
        CriarTextoArea(cardObj, "DescArea", "DescText", skill.GetDisplayDescription(),
            new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.58f), 11, new Color(0.90f, 0.82f, 0.65f), false);

        // ── StatsArea ─────────────────────────────────────────────────────────
        CriarTextoArea(cardObj, "StatsArea", "StatsText", GetManualStatsText(skill),
            new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.22f), 11, new Color(0.95f, 0.82f, 0.40f), false);

        // ── Raridade ── (badge removido dos cards de skill)

        Button button = cardObj.GetComponent<Button>();
        button.onClick.AddListener(() => OnSkillSelected(skill));

        Debug.LogWarning($"🚨 Card de emergência criado para: {skill.skillName}");
    }

    private Sprite CarregarSprite(string path, string spriteName)
    {
#if UNITY_EDITOR
        var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var a in all) if (a is Sprite s && s.name == spriteName) return s;
#endif
        return null;
    }

    // Faz o texto encolher para caber na sua área (não ultrapassa a borda do card).
    private static void AjustarTextoParaCaber(TMPro.TextMeshProUGUI t, float tamanhoMin)
    {
        float tamanhoMax = t.enableAutoSizing ? t.fontSizeMax : t.fontSize;
        tamanhoMax = Mathf.Min(tamanhoMax, 12f); // letra um pouco menor + margem pra não passar
        if (tamanhoMax < tamanhoMin) tamanhoMax = tamanhoMin;
        t.enableAutoSizing = true;
        t.fontSizeMin = tamanhoMin;
        t.fontSizeMax = tamanhoMax;
        t.textWrappingMode = TMPro.TextWrappingModes.Normal;
        t.overflowMode = TMPro.TextOverflowModes.Truncate;
    }

    private void CriarTextoArea(GameObject parent, string areaName, string textName,
        string content, Vector2 anchorMin, Vector2 anchorMax,
        float fontSize, Color cor, bool bold)
    {
        var area = new GameObject(areaName, typeof(RectTransform));
        area.transform.SetParent(parent.transform, false);
        var aRT = area.GetComponent<RectTransform>();
        aRT.anchorMin = anchorMin; aRT.anchorMax = anchorMax;
        aRT.anchoredPosition = Vector2.zero; aRT.sizeDelta = Vector2.zero;

        var txtGO = new GameObject(textName, typeof(RectTransform));
        txtGO.transform.SetParent(area.transform, false);
        var tRT = txtGO.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.anchoredPosition = Vector2.zero; tRT.sizeDelta = Vector2.zero;

        var txt = txtGO.AddComponent<TMPro.TextMeshProUGUI>();
        txt.text = content; txt.fontSize = fontSize; txt.color = cor;
        txt.alignment = TMPro.TextAlignmentOptions.Center;
        txt.textWrappingMode = TMPro.TextWrappingModes.Normal;
        if (bold) txt.fontStyle = TMPro.FontStyles.Bold;
        AjustarTextoParaCaber(txt, 7f);
    }

    private void SetupSkillCardManually(GameObject cardObj, SkillData skill)
    {
        TextMeshProUGUI[] textComponents = cardObj.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (TextMeshProUGUI text in textComponents)
        {
            if (text.name.Contains("Name") || text.name.Contains("Nome") || text.name.Contains("Title"))
            {
                text.text = $"<b>{skill.GetDisplayName()}</b>";
            }
            else if (text.name.Contains("Desc") || text.name.Contains("Description") || text.name.Contains("Detail"))
            {
                text.text = skill.GetDisplayDescription();
                AjustarTextoParaCaber(text, 7f);
            }
            else if (text.name.Contains("Stats") || text.name.Contains("Status") || text.name.Contains("Bonus"))
            {
                text.text = GetManualStatsText(skill);
                AjustarTextoParaCaber(text, 7f);
            }
        }

        if (skill.icon != null)
        {
            var innerT = cardObj.transform.Find("IconArea/IconImageSlot/IconInner");
            Image iconImg = innerT != null ? innerT.GetComponent<Image>() : null;
            if (iconImg == null)
            {
                foreach (var img in cardObj.GetComponentsInChildren<Image>())
                {
                    if (img.name == "IconInner" || img.name.Contains("Icon") || img.name.Contains("Image"))
                    { iconImg = img; break; }
                }
            }
            if (iconImg != null)
            {
                iconImg.sprite = skill.icon;
                iconImg.type = Image.Type.Simple;
                iconImg.preserveAspect = false;
            }
        }
    }

    private string GetManualStatsText(SkillData skill)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        if (skill.healthBonus != 0) sb.Append($"{Loc.T("stat.hp")}:{skill.healthBonus} ");
        if (skill.attackBonus != 0) sb.Append($"{Loc.T("stat.atk")}:{skill.attackBonus} ");
        if (skill.defenseBonus != 0) sb.Append($"{Loc.T("stat.def")}:{skill.defenseBonus} ");
        if (skill.speedBonus != 0) sb.Append($"{Loc.T("stat.spd")}:{skill.speedBonus} ");
        if (skill.healthRegenBonus != 0) sb.Append($"{Loc.T("stat.regen")}:{skill.healthRegenBonus} ");

        if (sb.Length == 0) sb.Append(Loc.T("ui.passive_bonus"));

        return sb.ToString();
    }

    private IEnumerator ForceLayoutRefresh()
    {
        yield return new WaitForEndOfFrame();

        if (skillsContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(skillsContainer as RectTransform);
            Canvas.ForceUpdateCanvases();
        }
    }

    private void OnSkillSelected(SkillData selectedSkill)
    {
        if (selectedSkill != null)
            StartCoroutine(SelectionConfirmationEffect(selectedSkill));
        else
            ClosePanel();
    }

    private IEnumerator SelectionConfirmationEffect(SkillData selectedSkill)
    {
        foreach (var button in currentButtons)
        {
            if (button != null)
            {
                Button btn = button.GetComponent<Button>();
                if (btn != null) btn.interactable = false;
            }
        }

        yield return new WaitForSecondsRealtime(0.5f);

        onSkillChosen?.Invoke(selectedSkill);

        yield return new WaitForSecondsRealtime(autoCloseDelay);

        ClosePanel();
    }

    public void ClosePanel()
    {
        if (contadorCoroutine != null)
        {
            StopCoroutine(contadorCoroutine);
            contadorCoroutine = null;
        }

        if (choicePanel != null)
        {
            choicePanel.SetActive(false);
        }

        if (overlayEscuro != null) overlayEscuro.SetActive(false);

        ClearSkillButtons();
        ResumeGame();
        currentChoices = null;
        onSkillChosen = null;
    }

    private IEnumerator ContadorEscolha()
    {
        Transform pai = choicePanel != null ? choicePanel.transform : transform;
        GameObject timerGO = new GameObject("TimerEscolha");
        timerGO.transform.SetParent(pai, false);

        TextMeshProUGUI txt = timerGO.AddComponent<TextMeshProUGUI>();
        txt.fontSize  = 28;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;

        RectTransform rt = timerGO.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 30f);
        rt.sizeDelta        = new Vector2(160f, 50f);

        float restante = tempoEscolha;
        while (restante > 0f)
        {
            restante -= Time.unscaledDeltaTime;
            int seg   = Mathf.CeilToInt(Mathf.Max(0f, restante));
            txt.text  = seg.ToString();
            txt.color = restante < 5f ? Color.red : Color.white;
            yield return null;
        }

        contadorCoroutine = null;
        if (currentChoices != null && currentChoices.Count > 0)
            OnSkillSelected(currentChoices[Random.Range(0, currentChoices.Count)]);
        else
            ClosePanel();
    }

    private void PauseGame()
    {
        if (pauseGameDuringChoice)
        {
            // Guarda 1 se o jogo já estava pausado por outra UI, para não travar ao fechar
            previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            CoopPause.ReterEscolha(); // SP: timeScale=0; co-op: pausa o grupo via host
            AudioListener.pause = true;
        }
    }

    private void ResumeGame()
    {
        if (pauseGameDuringChoice)
        {
            CoopPause.LiberarEscolha(); // SP: timeScale=1; co-op: libera a pausa do grupo
            AudioListener.pause = false;
        }
    }

    private void UpdateTitleText()
    {
        string title = "Cartas de Skill";

        if (titleTextTMP != null)
        {
            titleTextTMP.text = title;
            // visual limpo: dourado sólido, sem gradiente/negrito que empastavam
            titleTextTMP.enableVertexGradient = false;
            titleTextTMP.characterSpacing = 0f;
            titleTextTMP.color = new Color(1.00f, 0.80f, 0.35f);
        }
        else if (titleText != null)
        {
            titleText.text = title;
            titleText.color = new Color(1.00f, 0.82f, 0.35f);
            titleText.fontStyle = FontStyle.Bold;
        }
    }

    private void ClearSkillButtons()
    {
        foreach (GameObject button in currentButtons)
        {
            if (button != null)
                Destroy(button);
        }
        currentButtons.Clear();

        if (skillsContainer != null)
        {
            foreach (Transform child in skillsContainer)
            {
                if (child != null && child.gameObject != null)
                    Destroy(child.gameObject);
            }
        }
    }

    [ContextMenu("🎯 Testar Sistema de 3 Skills Aleatórias")]
    public void TestRandomThreeSkills()
    {

        // 🎯 ATUALIZAR SKILLS ANTES DO TESTE
        LoadSkillsFromSkillManager();

        ShowRandomSkillChoice((selectedSkill) => {
        });
    }

    [ContextMenu("🔍 Verificar Skills Disponíveis")]
    public void DebugAvailableSkills()
    {

        foreach (var skill in allAvailableSkills)
        {
        }
    }

    [ContextMenu("🔄 Atualizar Skills do SkillManager")]
    public void RefreshSkillsFromManager()
    {
        LoadSkillsFromSkillManager();
    }

    void OnDestroy()
    {
        ResumeGame();
    }

    [ContextMenu("🔧 Verificar Configuração")]
    public void CheckConfiguration()
    {

        // 🎯 VERIFICAR SKILLMANAGER
        SkillManager skillManager = SkillManager.Instance;
        if (skillManager != null)
        {
        }
        else
        {
            Debug.LogError("❌ SkillManager não encontrado!");
        }

        if (skillsContainer != null)
        {
            HorizontalLayoutGroup layout = skillsContainer.GetComponent<HorizontalLayoutGroup>();
            ContentSizeFitter fitter = skillsContainer.GetComponent<ContentSizeFitter>();

        }
    }

    [ContextMenu("🔓 Destravar Container Manualmente")]
    public void UnlockContainerManually()
    {
        if (skillsContainer == null)
        {
            Debug.LogError("❌ Container não encontrado!");
            return;
        }

        ContentSizeFitter fitter = skillsContainer.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            DestroyImmediate(fitter);
        }

        HorizontalLayoutGroup layout = skillsContainer.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
        {
            layout.childControlWidth = false;
            layout.childControlHeight = false;
        }

        RectTransform rect = skillsContainer as RectTransform;
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(1200, 500);
        }

    }

    [ContextMenu("🔍 Verificar Container e Cards")]
    public void DebugContainerAndCards()
    {

        if (skillsContainer == null)
        {
            Debug.LogError("❌ skillsContainer é NULL!");
            return;
        }

        // Verificar container
        RectTransform containerRect = skillsContainer as RectTransform;
        if (containerRect != null)
        {
        }

        // Verificar Layout Group
        HorizontalLayoutGroup layout = skillsContainer.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
        {
        }

        // Verificar cards existentes
        int childCount = skillsContainer.childCount;

        for (int i = 0; i < childCount; i++)
        {
            Transform child = skillsContainer.GetChild(i);
            GameObject childObj = child.gameObject;


            // Verificar componentes
            RectTransform rect = child.GetComponent<RectTransform>();
            if (rect != null)
            {
            }

            // Verificar se é prefab instance
#if UNITY_EDITOR
            bool isPrefabInstance = PrefabUtility.IsPartOfPrefabInstance(childObj);

            if (isPrefabInstance)
            {
                GameObject prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(childObj);
            }
#endif

            // Verificar componentes de UI
            Image image = child.GetComponent<Image>();
            if (image != null)
            {
            }

            Button button = child.GetComponent<Button>();
            if (button != null)
            {
            }

            // Verificar textos nos filhos
            TextMeshProUGUI[] texts = child.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var text in texts)
            {
            }

            // Verificar imagens nos filhos
            Image[] images = child.GetComponentsInChildren<Image>();
            foreach (var img in images)
            {
                if (img.transform != child) // Não é o componente principal
                {
                }
            }
        }

    }

    [ContextMenu("🎯 Verificar Prefabs das Skills Atuais")]
    public void DebugCurrentSkillPrefabs()
    {
        if (currentChoices == null || currentChoices.Count == 0)
        {
            return;
        }


        for (int i = 0; i < currentChoices.Count; i++)
        {
            SkillData skill = currentChoices[i];

            if (skill.cardPrefab == null)
            {
                Debug.LogWarning($"   ⚠️ Skill não tem cardPrefab! Usará fallback: {skillChoicePrefab?.name ?? "NENHUM"}");
            }
        }

    }
}

// ── Animador dinâmico das cartas de skill ─────────────────────────────────────
public class CartaSkillAnimador : MonoBehaviour
{
    GameObject card;
    bool hover;
    float t;
    Vector3 escalaOriginal;
    bool entrou;

    RectTransform cardRT;

    public void Iniciar(GameObject c)
    {
        card   = c;
        cardRT = c.GetComponent<RectTransform>();
        escalaOriginal = c.transform.localScale;
        entrou = true;
    }

    void Update()
    {
        if (!entrou || card == null || cardRT == null) return;

        // Detecta mouse sobre o card diretamente via RectTransform
        hover = RectTransformUtility.RectangleContainsScreenPoint(
            cardRT, Input.mousePosition, null);

        float escalaAlvo = hover ? 1.15f : 1f;
        float escalaAtual = card.transform.localScale.x / escalaOriginal.x;
        float novaEscala = Mathf.Lerp(escalaAtual, escalaAlvo, Time.unscaledDeltaTime * 12f);
        card.transform.localScale = escalaOriginal * novaEscala;
    }
}
