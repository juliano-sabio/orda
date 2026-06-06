using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatusCardChoiceUI : MonoBehaviour
{
    [Header("Referências UI")]
    public GameObject       choicePanel;
    public Transform        cardsContainer;
    public TextMeshProUGUI  titleText;

    [Header("Prefab de Carta (mesmo das skills)")]
    public GameObject cardPrefab;   // arraste aqui o mesmo prefab usado nas skill cards

    [Header("Sprites (override do carregamento automático)")]
    public Sprite fundoCarta;   // fundoteste.png → fundoteste_0
    public Sprite frameSlot;    // cartaskill.png → carta_frame

    [Header("Configurações")]
    public bool    pauseGameDuringChoice = true;
    public float   cardSpacing           = 30f;
    public Vector2 cardSize              = new Vector2(300f, 450f);
    public float   tempoEscolha          = 20f;
    public string  titleMessage          = "ESCOLHA UMA CARTA DE STATUS";

    private System.Action<StatusCardInfo> onCardChosen;
    private List<GameObject>    spawnedCards  = new List<GameObject>();
    private List<StatusCardInfo> cartasAtuais = new List<StatusCardInfo>();
    private int   cardIndex        = 0;
    private float previousTimeScale;
    private Coroutine contadorCoroutine;

    void Awake()
    {
        if (choicePanel != null) choicePanel.SetActive(false);
    }

    void Update()
    {
        if (spawnedCards.Count == 0) return;

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            cardIndex = (cardIndex - 1 + spawnedCards.Count) % spawnedCards.Count;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            cardIndex = (cardIndex + 1) % spawnedCards.Count;
        else if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            if (cardIndex >= 0 && cardIndex < cartasAtuais.Count)
                OnCardSelected(cartasAtuais[cardIndex]);
        }
    }


    // ── API pública ──────────────────────────────────────────────────────────

    public void Show(List<StatusCardInfo> cards, System.Action<StatusCardInfo> callback)
    {
        onCardChosen  = callback;
        cartasAtuais  = new List<StatusCardInfo>(cards);
        cardIndex     = 0;

        if (!gameObject.activeInHierarchy) gameObject.SetActive(true);

        if (choicePanel != null) choicePanel.SetActive(true);

        if (titleText != null) titleText.text = titleMessage;

        PauseGame();
        SetupLayout();

        if (contadorCoroutine != null) StopCoroutine(contadorCoroutine);
        contadorCoroutine = StartCoroutine(ContadorEscolha());

        StartCoroutine(SpawnCards(cards));
    }

    public void ClosePanel()
    {
        if (contadorCoroutine != null) { StopCoroutine(contadorCoroutine); contadorCoroutine = null; }
        ResumeGame();
        if (choicePanel != null) choicePanel.SetActive(false);
        ClearCards();
        onCardChosen = null;
        gameObject.SetActive(false);
    }

    // ── Layout ───────────────────────────────────────────────────────────────

    private void SetupLayout()
    {
        if (cardsContainer == null) return;
        var layout = cardsContainer.GetComponent<HorizontalLayoutGroup>()
                  ?? cardsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing            = cardSpacing;
        layout.padding            = new RectOffset(30, 30, 20, 20);
        layout.childAlignment     = TextAnchor.MiddleCenter;
        layout.childControlWidth  = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth  = false;
        layout.childForceExpandHeight = false;

        var containerRect = cardsContainer as RectTransform;
        if (containerRect != null)
        {
            containerRect.sizeDelta        = new Vector2(1200f, 500f);
            containerRect.anchoredPosition = new Vector2(0f, 3f);
            containerRect.anchorMin        = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax        = new Vector2(0.5f, 0.5f);
            containerRect.pivot            = new Vector2(0.5f, 0.5f);
        }
    }

    // ── Criação de cartas ────────────────────────────────────────────────────

    private IEnumerator SpawnCards(List<StatusCardInfo> cards)
    {
        ClearCards();
        yield return null;
        foreach (var card in cards) SpawnCard(card);
        yield return null;
        if (cardsContainer != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(cardsContainer as RectTransform);
    }

    private void SpawnCard(StatusCardInfo card)
    {
        // 1. Prefab manual no Inspector
        if (cardPrefab != null) { SpawnCardDoPrefab(card, cardPrefab); return; }

        // 2. Mesmo prefab genérico do SkillChoiceUI
        var skillUI = FindFirstObjectByType<SkillChoiceUI>();
        if (skillUI != null && skillUI.skillChoicePrefab != null)
        {
            SpawnCardDoPrefab(card, skillUI.skillChoicePrefab);
            return;
        }

        // 3. Template visual das skill cards (mesmo look garantido)
        string[] templatePaths = { "Cards/Aureola", "Cards/EscudoEspinhoso", "Cards/barreira_reflexiva" };
        foreach (var tp in templatePaths)
        {
            var t = Resources.Load<GameObject>(tp);
            if (t != null) { SpawnCardDoPrefab(card, t); return; }
        }

        // 4. Prefab por raridade na pasta Resources/card_status/
        string path = card.rarity switch
        {
            CardRarity.Common => "card_status/card_status_comun",
            CardRarity.Rare   => "card_status/card_status_rare",
            CardRarity.Mystic => "card_status/card_status_mystic",
            CardRarity.Curse  => "card_status/card_status_corrupted",
            _                 => "card_status/StatusCardPrefab"
        };
        var prefab = Resources.Load<GameObject>(path);
        if (prefab != null) { SpawnCardDoPrefab(card, prefab); return; }

        // 5. Totalmente programático
        SpawnCardProgramatico(card);
    }

    // Instancia o prefab e popula campos por nome
    private void SpawnCardDoPrefab(StatusCardInfo card, GameObject prefab)
    {
        var cardObj = Instantiate(prefab, cardsContainer);
        cardObj.name = $"StatusCard_{card.statType}";
        cardObj.SetActive(true);
        spawnedCards.Add(cardObj);

        // Tamanho
        var rt = cardObj.GetComponent<RectTransform>();
        if (rt != null) { rt.localScale = Vector3.one; rt.sizeDelta = cardSize; }
        var le = cardObj.GetComponent<LayoutElement>()
              ?? cardObj.AddComponent<LayoutElement>();
        le.preferredWidth = cardSize.x; le.preferredHeight = cardSize.y;
        le.flexibleWidth  = le.flexibleHeight = 0;

        Color rColor = GetRarityColor(card.rarity);

        // Fundo da carta — sobrescreve sprite do prefab com fundoteste
        var cardBg = cardObj.GetComponent<Image>();
        if (cardBg != null)
        {
            Sprite fundo = fundoCarta
                ?? CarregarSprite("Assets/assets/UI/charselection/fundoteste.png", "fundoteste_0");
            if (fundo != null) { cardBg.sprite = fundo; cardBg.color = Color.white; cardBg.type = Image.Type.Simple; }
        }

        // Textos por nome (mesma convenção das skill cards)
        foreach (var txt in cardObj.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            string n = txt.name.ToLower();
            if      (n.Contains("name")  || n.Contains("nome")  || n.Contains("title"))
                txt.text = card.cardName;
            else if (n.Contains("desc")  || n.Contains("detail"))
                txt.text = card.description;
            else if (n.Contains("stats") || n.Contains("bonus") || n.Contains("atq") || n.Contains("status"))
                txt.text = FormatarBonus(card);
            else if (n.Contains("rarity") || n.Contains("rarid") || n.Contains("rare")
                  || n.Contains("comun")  || n.Contains("curse") || n.Contains("mistico"))
                { txt.text = GetRarityLabel(card.rarity); txt.color = rColor; }
        }

        // Ícone — usa o ícone gerado pelo StatusCardIconGenerator
        var innerImg = cardObj.transform.Find("IconArea/IconImageSlot/IconInner")
                              ?.GetComponent<Image>();
        if (innerImg == null)
            foreach (var img in cardObj.GetComponentsInChildren<Image>(true))
                if (img.name == "IconInner") { innerImg = img; break; }
        if (innerImg != null)
        {
            var statIcon = StatusCardIconGenerator.GetIcon(card.statType, rColor);
            if (statIcon != null) { innerImg.sprite = statIcon; innerImg.color = Color.white; innerImg.preserveAspect = true; }
            else { innerImg.sprite = null; innerImg.color = rColor; }
        }

        // Botão
        var btn = cardObj.GetComponent<Button>()
               ?? cardObj.GetComponentInChildren<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnCardSelected(card));
        }

        // Hover igual às skill cards
        cardObj.AddComponent<CartaSkillAnimador>().Iniciar(cardObj);
    }

    // Fallback totalmente programático (sem prefab)
    private void SpawnCardProgramatico(StatusCardInfo card)
    {
        var cardObj = new GameObject($"StatusCard_{card.statType}",
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        cardObj.transform.SetParent(cardsContainer);
        cardObj.SetActive(true);
        spawnedCards.Add(cardObj);

        Color rColor = GetRarityColor(card.rarity);

        // Fundo da carta — fundoteste
        var bgImg = cardObj.GetComponent<Image>();
        Sprite spFundo = fundoCarta
            ?? CarregarSprite("Assets/assets/UI/charselection/fundoteste.png", "fundoteste_0");
        if (spFundo != null) { bgImg.sprite = spFundo; bgImg.color = Color.white; bgImg.type = Image.Type.Simple; }
        else bgImg.color = new Color(0.07f, 0.05f, 0.10f, 0.97f);
        bgImg.raycastTarget = true;

        var rt = cardObj.GetComponent<RectTransform>();
        rt.localScale = Vector3.one; rt.sizeDelta = cardSize;
        var le = cardObj.GetComponent<LayoutElement>();
        le.preferredWidth = cardSize.x; le.preferredHeight = cardSize.y;
        le.flexibleWidth = le.flexibleHeight = 0;

        // Borda de raridade
        var bordGO = new GameObject("RarityBorder", typeof(Image));
        bordGO.transform.SetParent(cardObj.transform, false);
        var bordImg = bordGO.GetComponent<Image>();
        bordImg.color = new Color(rColor.r, rColor.g, rColor.b, 0.7f);
        var bordRT = bordGO.GetComponent<RectTransform>();
        bordRT.anchorMin = Vector2.zero; bordRT.anchorMax = Vector2.one;
        bordRT.offsetMin = new Vector2(-2f, -2f); bordRT.offsetMax = new Vector2(2f, 2f);
        bordGO.transform.SetAsFirstSibling();

        // Área de ícone
        var iconArea = new GameObject("IconArea", typeof(RectTransform));
        iconArea.transform.SetParent(cardObj.transform, false);
        var iaRT = iconArea.GetComponent<RectTransform>();
        iaRT.anchorMin = new Vector2(0f, 0.68f); iaRT.anchorMax = new Vector2(1f, 0.97f);
        iaRT.anchoredPosition = Vector2.zero; iaRT.sizeDelta = Vector2.zero;

        // IconImageSlot — usa carta_frame do cartaskill.png
        var slotGO = new GameObject("IconImageSlot", typeof(RectTransform), typeof(Image));
        slotGO.transform.SetParent(iconArea.transform, false);
        var slotRT = slotGO.GetComponent<RectTransform>();
        slotRT.anchorMin = new Vector2(0.05f, 0.05f); slotRT.anchorMax = new Vector2(0.95f, 0.95f);
        slotRT.anchoredPosition = Vector2.zero; slotRT.sizeDelta = Vector2.zero;
        var slotImg = slotGO.GetComponent<Image>();
        Sprite spSlot = frameSlot
            ?? CarregarSprite("Assets/assets/UI/skill_card/cartaskill.png", "carta_frame");
        if (spSlot != null) { slotImg.sprite = spSlot; slotImg.color = Color.white; slotImg.type = Image.Type.Sliced; slotImg.fillCenter = true; }
        else slotImg.color = new Color(rColor.r * 0.4f, rColor.g * 0.4f, rColor.b * 0.4f, 0.6f);
        slotImg.raycastTarget = false;

        // IconInner — usa skill_slot_card.aseprite como fundo
        var innerGO = new GameObject("IconInner", typeof(RectTransform), typeof(Image));
        innerGO.transform.SetParent(slotGO.transform, false);
        var innerRT = innerGO.GetComponent<RectTransform>();
        innerRT.anchorMin = new Vector2(0.1f, 0.1f); innerRT.anchorMax = new Vector2(0.9f, 0.9f);
        innerRT.anchoredPosition = Vector2.zero; innerRT.sizeDelta = Vector2.zero;
        var innerImg = innerGO.GetComponent<Image>();
        Sprite spInner = CarregarSprite("Assets/assets/UI/skill_card/skill_slot_card.aseprite", "skill_slot_card");
        if (spInner != null) { innerImg.sprite = spInner; innerImg.color = Color.white; }
        else innerImg.color = rColor;
        innerImg.raycastTarget = false;

        // Ícone do stat por cima do slot
        var statIconGO = new GameObject("StatIcon", typeof(RectTransform), typeof(Image));
        statIconGO.transform.SetParent(innerGO.transform, false);
        var statIconRT = statIconGO.GetComponent<RectTransform>();
        statIconRT.anchorMin = new Vector2(0.1f, 0.1f); statIconRT.anchorMax = new Vector2(0.9f, 0.9f);
        statIconRT.anchoredPosition = Vector2.zero; statIconRT.sizeDelta = Vector2.zero;
        var statIconImg = statIconGO.GetComponent<Image>();
        var statSp = StatusCardIconGenerator.GetIcon(card.statType, rColor);
        if (statSp != null) { statIconImg.sprite = statSp; statIconImg.color = Color.white; statIconImg.preserveAspect = true; }
        else { statIconImg.sprite = null; statIconImg.color = new Color(rColor.r, rColor.g, rColor.b, 0.8f); }
        statIconImg.raycastTarget = false;

        CriarTextoArea(cardObj, "NameArea",   "NameText",  $"<b>{card.cardName}</b>",
            new Vector2(0f, 0.50f), new Vector2(1f, 0.68f), 14, new Color(0.95f, 0.82f, 0.40f), true);
        CriarTextoArea(cardObj, "DescArea",   "DescText",  card.description,
            new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.58f), 11, new Color(0.90f, 0.82f, 0.65f), false);
        CriarTextoArea(cardObj, "StatsArea",  "StatsText", FormatarBonus(card),
            new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.22f), 11, new Color(0.95f, 0.82f, 0.40f), false);
        CriarTextoArea(cardObj, "RarityArea", "RarityText", GetRarityLabel(card.rarity),
            new Vector2(0.2f, 0.13f), new Vector2(0.8f, 0.24f), 12, rColor, false);

        var btn = cardObj.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnCardSelected(card));

        cardObj.AddComponent<CartaSkillAnimador>().Iniciar(cardObj);
    }

    // ── Contador ─────────────────────────────────────────────────────────────

    private IEnumerator ContadorEscolha()
    {
        Transform pai = choicePanel != null ? choicePanel.transform : transform;
        var timerGO = new GameObject("TimerEscolha");
        timerGO.transform.SetParent(pai, false);

        var txt = timerGO.AddComponent<TextMeshProUGUI>();
        txt.fontSize  = 28; txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;

        var tr = timerGO.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0.5f, 0f); tr.anchorMax = new Vector2(0.5f, 0f);
        tr.pivot     = new Vector2(0.5f, 0f);
        tr.anchoredPosition = new Vector2(0f, 30f); tr.sizeDelta = new Vector2(160f, 50f);

        float restante = tempoEscolha;
        while (restante > 0f)
        {
            restante -= Time.unscaledDeltaTime;
            txt.text  = Mathf.CeilToInt(Mathf.Max(0f, restante)).ToString();
            txt.color = restante < 5f ? Color.red : Color.white;
            yield return null;
        }

        contadorCoroutine = null;
        ClosePanel();
    }

    // ── Seleção ──────────────────────────────────────────────────────────────

    private void OnCardSelected(StatusCardInfo card)
    {
        if (contadorCoroutine != null) { StopCoroutine(contadorCoroutine); contadorCoroutine = null; }
        ResumeGame();
        if (choicePanel != null) choicePanel.SetActive(false);
        ClearCards();
        onCardChosen?.Invoke(card);
        gameObject.SetActive(false);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void ClearCards()
    {
        foreach (var g in spawnedCards) if (g != null) Destroy(g);
        spawnedCards.Clear();
        if (cardsContainer != null)
            foreach (Transform t in cardsContainer) Destroy(t.gameObject);
    }

    private void PauseGame()
    {
        if (!pauseGameDuringChoice) return;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        AudioListener.pause = true;
    }

    private void ResumeGame()
    {
        if (!pauseGameDuringChoice) return;
        Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
        AudioListener.pause = false;
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

        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text     = content; txt.fontSize = fontSize; txt.color = cor;
        txt.alignment = TextAlignmentOptions.Center;
        txt.textWrappingMode = TextWrappingModes.Normal;
        if (bold) txt.fontStyle = FontStyles.Bold;
    }

    private string FormatarBonus(StatusCardInfo card)
    {
        string statNome = card.statType switch
        {
            StatusCardType.Health         => "Vida",
            StatusCardType.Attack         => "Ataque",
            StatusCardType.Defense        => "Defesa",
            StatusCardType.Speed          => "Velocidade",
            StatusCardType.Regen          => "Regeneracao",
            StatusCardType.CriticalChance => "Critico",
            StatusCardType.AttackSpeed    => "Vel. Ataque",
            _                             => card.statType.ToString()
        };
        string txt = $"+{card.bonus:F0} {statNome}";
        if (card.HasPenalty)
        {
            string penNome = card.penaltyStatType switch
            {
                StatusCardType.Health  => "Vida",
                StatusCardType.Attack  => "Ataque",
                StatusCardType.Defense => "Defesa",
                StatusCardType.Speed   => "Velocidade",
                _                      => card.penaltyStatType.ToString()
            };
            txt += $"\n-{card.penalty:F0} {penNome}";
        }
        return txt;
    }

    private Color GetRarityColor(CardRarity rarity) => rarity switch
    {
        CardRarity.Common => new Color(0.30f, 0.40f, 0.60f),
        CardRarity.Rare   => new Color(0.50f, 0.20f, 0.70f),
        CardRarity.Mystic => new Color(0.70f, 0.55f, 0.10f),
        CardRarity.Curse  => new Color(0.55f, 0.10f, 0.10f),
        _                 => Color.white
    };

    private string GetRarityLabel(CardRarity rarity) => rarity switch
    {
        CardRarity.Common => "Comum",
        CardRarity.Rare   => "Raro",
        CardRarity.Mystic => "Mistico",
        CardRarity.Curse  => "Amaldicoado",
        _                 => "Comum"
    };

    private Sprite CarregarSprite(string path, string spriteName)
    {
#if UNITY_EDITOR
        var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var a in all)
            if (a is Sprite s && s.name == spriteName) return s;
#endif
        return null;
    }
}
