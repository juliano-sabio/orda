using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Exibe 3 cartas de status para o jogador escolher no level-up.
// Funciona com qualquer prefab de carta — encontra textos e imagens por nome.
public class StatusCardChoiceUI : MonoBehaviour
{
    [Header("Referências UI")]
    public GameObject choicePanel;
    public Transform  cardsContainer;

    [Header("Prefabs de Carta por Raridade")]
    public GameObject commonCardPrefab;
    public GameObject rareCardPrefab;
    public GameObject mysticCardPrefab;
    public GameObject curseCardPrefab;
    public GameObject fallbackCardPrefab;   // usado se o de raridade não estiver atribuído

    [Header("Icones por Tipo de Status")]
    public Sprite healthIcon;
    public Sprite attackIcon;
    public Sprite defenseIcon;
    public Sprite speedIcon;
    public Sprite regenIcon;
    public Sprite critIcon;
    public Sprite attackSpeedIcon;

    [Header("Título")]
    public TextMeshProUGUI titleText;
    public string titleMessage = "ESCOLHA UMA CARTA DE STATUS";

    [Header("Configurações")]
    public bool pauseGameDuringChoice = true;
    public float cardSpacing = 30f;
    public Vector2 cardSize = new Vector2(300f, 450f);

    // Cores de fundo por raridade (caso os prefabs usem a mesma Image)
    [Header("Cores de Raridade (fallback)")]
    public Color commonColor  = new Color(0.30f, 0.40f, 0.60f);
    public Color rareColor    = new Color(0.50f, 0.20f, 0.70f);
    public Color mysticColor  = new Color(0.70f, 0.55f, 0.10f);
    public Color curseColor   = new Color(0.55f, 0.10f, 0.10f);

    private System.Action<StatusCardInfo> onCardChosen;
    private List<GameObject> spawnedCards = new List<GameObject>();
    private float previousTimeScale;

    void Awake()
    {
        if (choicePanel != null) choicePanel.SetActive(false);
    }

    // ── API pública ──────────────────────────────────────────────────────────

    public void Show(List<StatusCardInfo> cards, System.Action<StatusCardInfo> callback)
    {
        onCardChosen = callback;

        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);

        if (choicePanel != null)
            choicePanel.SetActive(true);

        if (titleText != null)
            titleText.text = titleMessage;

        if (pauseGameDuringChoice)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        SetupLayout();
        StartCoroutine(SpawnCards(cards));
    }

    // ── Internos ─────────────────────────────────────────────────────────────

    private void SetupLayout()
    {
        if (cardsContainer == null) return;

        HorizontalLayoutGroup layout = cardsContainer.GetComponent<HorizontalLayoutGroup>();
        if (layout == null) layout = cardsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();

        layout.spacing           = cardSpacing;
        layout.padding           = new RectOffset(30, 30, 20, 20);
        layout.childAlignment    = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth  = false;
        layout.childForceExpandHeight = false;
    }

    private IEnumerator SpawnCards(List<StatusCardInfo> cards)
    {
        ClearCards();
        yield return new WaitForEndOfFrame();

        foreach (StatusCardInfo card in cards)
            SpawnCard(card);

        yield return StartCoroutine(RefreshLayout());
    }

    private void SpawnCard(StatusCardInfo card)
    {
        GameObject prefab = GetPrefabForRarity(card.rarity);
        if (prefab == null)
        {
            Debug.LogWarning($"[StatusCardChoiceUI] Sem prefab para raridade {card.rarity}, criando emergencial");
            SpawnEmergencyCard(card);
            return;
        }

        GameObject obj = Instantiate(prefab, cardsContainer);
        obj.name = $"StatusCard_{card.rarity}_{card.statType}";
        obj.SetActive(true);
        spawnedCards.Add(obj);

        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = cardSize;

        PopulateCard(obj, card);
        WireButton(obj, card);
    }

    private void PopulateCard(GameObject obj, StatusCardInfo card)
    {
        Color rarityColor = GetRarityColor(card.rarity);
        string rarityLabel = GetRarityLabel(card.rarity);

        // Textos — mesma estratégia de busca por nome que SkillChoiceUI
        foreach (TextMeshProUGUI text in obj.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            string n = text.name.ToLower();
            if (n.Contains("name") || n.Contains("nome") || n.Contains("title"))
                text.text = card.cardName;
            else if (n.Contains("desc") || n.Contains("detail"))
                text.text = card.description;
            else if (n.Contains("rarity") || n.Contains("rarid"))
                text.text = rarityLabel;
            else if (n.Contains("stat") || n.Contains("bonus") || n.Contains("status"))
                text.text = card.description;
            // Botão de raridade (ex.: "comun", "rare", "curse")
            else if (n.Contains("button") || n.Contains("comun") || n.Contains("rare")
                     || n.Contains("curse") || n.Contains("mistico"))
                text.text = rarityLabel;
        }

        // Também verifica textos Unity legados
        foreach (Text text in obj.GetComponentsInChildren<Text>(true))
        {
            string n = text.name.ToLower();
            if (n.Contains("name") || n.Contains("nome") || n.Contains("title"))
                text.text = card.cardName;
            else if (n.Contains("rarity") || n.Contains("comun") || n.Contains("rare") || n.Contains("curse"))
                text.text = rarityLabel;
        }

        // Ícone do tipo de status
        Sprite icon = GetIconForStat(card.statType);
        if (icon != null)
        {
            foreach (Image img in obj.GetComponentsInChildren<Image>(true))
            {
                string n = img.name.ToLower();
                if (n.Contains("icon") || n.Contains("icone") || n.Contains("image"))
                {
                    img.sprite = icon;
                    img.color  = Color.white;
                    break;
                }
            }
        }

    }

    private void WireButton(GameObject obj, StatusCardInfo card)
    {
        // Tenta o Button no root primeiro, depois qualquer filho
        Button btn = obj.GetComponent<Button>();
        if (btn == null) btn = obj.GetComponentInChildren<Button>();

        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnCardSelected(card));
        }
        else
        {
            // Sem Button — adiciona um transparente sobre o card inteiro
            Button newBtn = obj.AddComponent<Button>();
            Image btnImage = obj.GetComponent<Image>();
            if (btnImage == null) btnImage = obj.AddComponent<Image>();
            btnImage.color = new Color(0, 0, 0, 0);
            newBtn.targetGraphic = btnImage;
            newBtn.onClick.AddListener(() => OnCardSelected(card));
        }
    }

    private void OnCardSelected(StatusCardInfo card)
    {
        ResumeGame();
        if (choicePanel != null) choicePanel.SetActive(false);
        ClearCards();
        onCardChosen?.Invoke(card);
    }

    private void ClearCards()
    {
        foreach (GameObject g in spawnedCards)
            if (g != null) Destroy(g);
        spawnedCards.Clear();

        if (cardsContainer != null)
            foreach (Transform t in cardsContainer)
                Destroy(t.gameObject);
    }

    private void ResumeGame()
    {
        if (pauseGameDuringChoice)
            Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
    }

    private IEnumerator RefreshLayout()
    {
        if (cardsContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(cardsContainer as RectTransform);
        }
        yield return new WaitForEndOfFrame();
        if (cardsContainer != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(cardsContainer as RectTransform);
    }

    // ── Helpers de raridade ──────────────────────────────────────────────────

    private GameObject GetPrefabForRarity(CardRarity rarity)
    {
        GameObject prefab = rarity switch
        {
            CardRarity.Common => commonCardPrefab,
            CardRarity.Rare   => rareCardPrefab,
            CardRarity.Mystic => mysticCardPrefab,
            CardRarity.Curse  => curseCardPrefab,
            _                 => null
        };
        return prefab != null ? prefab : fallbackCardPrefab;
    }

    private Color GetRarityColor(CardRarity rarity) => rarity switch
    {
        CardRarity.Common => commonColor,
        CardRarity.Rare   => rareColor,
        CardRarity.Mystic => mysticColor,
        CardRarity.Curse  => curseColor,
        _                 => Color.white
    };

    private Sprite GetIconForStat(StatusCardType statType) => statType switch
    {
        StatusCardType.Health         => healthIcon,
        StatusCardType.Attack         => attackIcon,
        StatusCardType.Defense        => defenseIcon,
        StatusCardType.Speed          => speedIcon,
        StatusCardType.Regen          => regenIcon,
        StatusCardType.CriticalChance => critIcon,
        StatusCardType.AttackSpeed    => attackSpeedIcon,
        _                             => null
    };

    private string GetRarityLabel(CardRarity rarity) => rarity switch
    {
        CardRarity.Common => "Comum",
        CardRarity.Rare   => "Raro",
        CardRarity.Mystic => "Mistico",
        CardRarity.Curse  => "Amaldicoado",
        _                 => "Comum"
    };

    // ── Carta emergencial (nenhum prefab atribuído) ──────────────────────────

    private void SpawnEmergencyCard(StatusCardInfo card)
    {
        GameObject obj = new GameObject($"EmergencyCard_{card.statType}", typeof(RectTransform));
        obj.transform.SetParent(cardsContainer);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = cardSize;

        Image bg = obj.AddComponent<Image>();
        bg.color = GetRarityColor(card.rarity);

        GameObject textGO = new GameObject("Name", typeof(RectTransform));
        textGO.transform.SetParent(obj.transform);
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = $"{card.cardName}\n{card.description}\n[{GetRarityLabel(card.rarity)}]";
        tmp.fontSize = 14;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        RectTransform tr = textGO.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.sizeDelta = Vector2.zero;
        tr.anchoredPosition = Vector2.zero;

        spawnedCards.Add(obj);
        WireButton(obj, card);
    }
}
