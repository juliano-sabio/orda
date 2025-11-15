using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatusCardUI : MonoBehaviour
{
    [Header("Componentes UI")]
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI rarityText;
    public TextMeshProUGUI levelRequirementText;
    public Image cardBackground;
    public Image cardIcon; // Novo campo para o ícone
    public Button selectButton;

    [Header("Cores por Raridade")]
    public Color commonColor = new Color(0.5f, 0.5f, 0.5f);
    public Color rareColor = new Color(0.2f, 0.4f, 1f);
    public Color epicColor = new Color(0.7f, 0.2f, 0.9f);
    public Color legendaryColor = new Color(1f, 0.8f, 0.2f);

    private StatusCardData cardData;
    private StatusCardSystem cardSystem;

    public void Initialize(StatusCardData data, StatusCardSystem system)
    {
        cardData = data;
        cardSystem = system;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (cardData == null) return;

        cardNameText.text = cardData.cardName;
        descriptionText.text = cardData.description;
        costText.text = $"Custo: {cardData.cost}";
        rarityText.text = cardData.rarity.ToString();
        levelRequirementText.text = $"Nv. {cardData.requiredLevel}";
        cardBackground.color = GetRarityColor(cardData.rarity);

        // Atualizar ícone se disponível
        if (cardIcon != null && cardData.icon != null)
        {
            cardIcon.sprite = cardData.icon;
            cardIcon.gameObject.SetActive(true);
        }

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnCardSelected);

        bool canAfford = cardSystem.CanAffordCard(cardData);
        selectButton.interactable = canAfford;

        // Feedback visual
        costText.color = canAfford ? Color.white : Color.red;
    }

    Color GetRarityColor(CardRarity rarity)
    {
        switch (rarity)
        {
            case CardRarity.Common: return commonColor;
            case CardRarity.Rare: return rareColor;
            case CardRarity.Epic: return epicColor;
            case CardRarity.Legendary: return legendaryColor;
            default: return Color.white;
        }
    }

    void OnCardSelected()
    {
        cardSystem?.ApplyStatusCard(cardData);
    }
}