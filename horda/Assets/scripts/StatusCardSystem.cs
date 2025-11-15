using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusCardSystem : MonoBehaviour
{
    public static StatusCardSystem Instance;

    [Header("Configurações do Sistema")]
    public int statusPointsPerLevel = 2;
    public int currentStatusPoints = 0;
    public int cardsPerChoice = 3;
    public int[] levelUpMilestones = { 2, 4, 6, 8, 10, 12, 14, 16, 18, 20 };

    [Header("Prefabs e Referências")]
    public GameObject statusCardPrefab;
    public Transform cardsContainer;
    public GameObject cardChoicePanel;

    [Header("Cards de Status Disponíveis (Arraste os Assets aqui)")]
    public List<StatusCardData> allStatusCards = new List<StatusCardData>();
    private List<StatusCardData> availableCards = new List<StatusCardData>();

    [Header("Status Aplicados")]
    public List<ActiveStatusBonus> activeBonuses = new List<ActiveStatusBonus>();

    private PlayerStats playerStats;
    private UIManager uiManager;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        playerStats = FindAnyObjectByType<PlayerStats>();
        uiManager = UIManager.Instance;

        LoadStatusCards();
        Debug.Log("✅ StatusCardSystem inicializado!");
    }

    void LoadStatusCards()
    {
        // Agora os cards são carregados automaticamente pela lista no Inspector
        // Basta arrastar os Scriptable Objects criados para a lista 'allStatusCards'

        availableCards = new List<StatusCardData>(allStatusCards);
        Debug.Log($"🎴 {allStatusCards.Count} cartas de status carregadas!");

        // Log para debug - mostra os cards carregados
        foreach (var card in allStatusCards)
        {
            Debug.Log($"📋 Card: {card.cardName} | Tipo: {card.cardType} | Raridade: {card.rarity}");
        }
    }

    public void OnPlayerLevelUp(int newLevel)
    {
        currentStatusPoints += statusPointsPerLevel;

        if (Array.Exists(levelUpMilestones, milestone => milestone == newLevel))
        {
            OfferStatusCardChoice();
        }

        Debug.Log($"🎯 +{statusPointsPerLevel} pontos de status! Total: {currentStatusPoints}");

        if (uiManager != null)
        {
            uiManager.ShowStatusPointsGained(statusPointsPerLevel, currentStatusPoints);
        }
    }

    void OfferStatusCardChoice()
    {
        List<StatusCardData> choices = GetRandomStatusCardChoices(cardsPerChoice);

        if (choices.Count > 0 && cardChoicePanel != null)
        {
            DisplayCardChoice(choices);
        }
    }

    List<StatusCardData> GetRandomStatusCardChoices(int count)
    {
        List<StatusCardData> choices = new List<StatusCardData>();
        List<StatusCardData> availableChoices = new List<StatusCardData>();

        if (playerStats == null) return choices;

        foreach (var card in availableCards)
        {
            if (MeetsCardRequirements(card, playerStats.level, currentStatusPoints))
            {
                availableChoices.Add(card);
            }
        }

        availableChoices = ShuffleList(availableChoices);

        for (int i = 0; i < Mathf.Min(count, availableChoices.Count); i++)
        {
            choices.Add(availableChoices[i]);
        }

        return choices;
    }

    List<StatusCardData> ShuffleList(List<StatusCardData> list)
    {
        List<StatusCardData> shuffled = new List<StatusCardData>(list);

        for (int i = 0; i < shuffled.Count; i++)
        {
            StatusCardData temp = shuffled[i];
            int randomIndex = UnityEngine.Random.Range(i, shuffled.Count);
            shuffled[i] = shuffled[randomIndex];
            shuffled[randomIndex] = temp;
        }

        return shuffled;
    }

    bool MeetsCardRequirements(StatusCardData card, int playerLevel, int availablePoints)
    {
        return card.requiredLevel <= playerLevel &&
               card.cost <= availablePoints &&
               !activeBonuses.Exists(bonus => bonus.cardData.cardName == card.cardName);
    }

    void DisplayCardChoice(List<StatusCardData> cards)
    {
        foreach (Transform child in cardsContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var cardData in cards)
        {
            GameObject cardObj = Instantiate(statusCardPrefab, cardsContainer);
            StatusCardUI cardUI = cardObj.GetComponent<StatusCardUI>();

            if (cardUI != null)
            {
                cardUI.Initialize(cardData, this);
            }
        }

        cardChoicePanel.SetActive(true);
    }

    public void ApplyStatusCard(StatusCardData cardData)
    {
        if (cardData == null || currentStatusPoints < cardData.cost) return;

        currentStatusPoints -= cardData.cost;
        ApplyCardBonus(cardData);

        ActiveStatusBonus newBonus = new ActiveStatusBonus
        {
            cardData = cardData,
            appliedTime = Time.time
        };
        activeBonuses.Add(newBonus);

        Debug.Log($"✅ Card aplicado: {cardData.cardName}");

        if (uiManager != null)
        {
            uiManager.ShowStatusCardApplied(cardData.cardName, cardData.description);
        }

        if (cardChoicePanel != null)
        {
            cardChoicePanel.SetActive(false);
        }

        UpdatePlayerUI();
    }

    void ApplyCardBonus(StatusCardData cardData)
    {
        if (playerStats == null) return;

        switch (cardData.cardType)
        {
            case StatusCardType.Health:
                playerStats.maxHealth += cardData.statBonus;
                playerStats.health += cardData.statBonus;
                break;
            case StatusCardType.Attack:
                playerStats.attack += cardData.statBonus;
                break;
            case StatusCardType.Defense:
                playerStats.defense += cardData.statBonus;
                break;
            case StatusCardType.Speed:
                playerStats.speed += cardData.statBonus;
                break;
            case StatusCardType.Regen:
                playerStats.healthRegenRate += cardData.statBonus;
                break;
        }
    }

    void UpdatePlayerUI()
    {
        if (playerStats != null)
        {
            playerStats.ForceUIUpdate();
        }
    }

    public int GetCurrentStatusPoints() => currentStatusPoints;
    public List<ActiveStatusBonus> GetActiveBonuses() => new List<ActiveStatusBonus>(activeBonuses);
    public List<StatusCardData> GetAvailableCards() => new List<StatusCardData>(availableCards);
    public bool CanAffordCard(StatusCardData card) => currentStatusPoints >= card.cost;

    [ContextMenu("Adicionar 5 Pontos de Status")]
    public void AddTestPoints()
    {
        currentStatusPoints += 5;
        Debug.Log($"🎯 +5 pontos de status! Total: {currentStatusPoints}");
    }

    [ContextMenu("Forçar Escolha de Cards")]
    public void ForceCardChoice()
    {
        OfferStatusCardChoice();
    }

    [ContextMenu("Debug - Listar Cards Carregados")]
    public void DebugListCards()
    {
        Debug.Log($"📋 Total de cards carregados: {allStatusCards.Count}");
        foreach (var card in allStatusCards)
        {
            Debug.Log($"→ {card.cardName} | {card.cardType} | Nv.{card.requiredLevel} | Custo: {card.cost}");
        }
    }
}