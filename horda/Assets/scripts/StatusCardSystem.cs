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

    // 🆕 Níveis que ativam a ESCOLHA DE CARDS
    public int[] cardChoiceLevels = { 2, 4, 6, 8, 10, 12, 14, 16, 18, 20 };

    [Header("Prefabs e Referências")]
    public GameObject statusCardPrefab;
    public Transform cardsContainer;
    public GameObject cardChoicePanel;

    [Header("Cards de Status Disponíveis")]
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
        availableCards = new List<StatusCardData>(allStatusCards);
        Debug.Log($"🎴 {allStatusCards.Count} cartas de status carregadas!");
    }

    // 🆕 MÉTODO ATUALIZADO: Ganha pontos sempre, cards só em níveis específicos
    public void OnPlayerLevelUp(int newLevel)
    {
        // 🆕 SEMPRE ganha pontos de status
        currentStatusPoints += statusPointsPerLevel;

        // 🆕 Verifica se este nível deve oferecer ESCOLHA DE CARDS
        bool shouldOfferCards = Array.Exists(cardChoiceLevels, level => level == newLevel);

        if (shouldOfferCards)
        {
            OfferStatusCardChoice();
        }

        Debug.Log($"🎯 Level {newLevel}: +{statusPointsPerLevel} pontos! Total: {currentStatusPoints}");

        if (shouldOfferCards)
        {
            Debug.Log($"🎴 Nível {newLevel} oferece escolha de cards!");
        }

        if (uiManager != null)
        {
            uiManager.ShowStatusPointsGained(statusPointsPerLevel);

            // 🆕 Atualiza a UI para mostrar pontos disponíveis
            uiManager.UpdateStatusCardsUI(); // ✅ AGORA FUNCIONA!
        }
    }

    // 🆕 OFERECER ESCOLHA DE CARDS (automático apenas nos níveis determinados)
    void OfferStatusCardChoice()
    {
        List<StatusCardData> choices = GetRandomStatusCardChoices(cardsPerChoice);

        if (choices.Count > 0 && cardChoicePanel != null)
        {
            DisplayCardChoice(choices);

            // Mostra automaticamente o painel
            cardChoicePanel.SetActive(true);

            Debug.Log($"🎴 Oferecendo {choices.Count} cards no nível {playerStats.GetLevel()}");
        }
        else
        {
            Debug.LogWarning("⚠️ Nenhum card disponível para escolha!");
        }
    }

    // 🆕 MÉTODO PÚBLICO para usar pontos manualmente (infinito)
    public void ApplyStatusCardManually(StatusCardData cardData)
    {
        if (cardData == null || currentStatusPoints < cardData.cost)
        {
            Debug.LogWarning($"❌ Pontos insuficientes! Necessário: {cardData.cost}, Disponível: {currentStatusPoints}");
            return;
        }

        currentStatusPoints -= cardData.cost;
        ApplyCardBonus(cardData);

        ActiveStatusBonus newBonus = new ActiveStatusBonus
        {
            cardData = cardData,
            appliedTime = Time.time
        };
        activeBonuses.Add(newBonus);

        Debug.Log($"✅ Card aplicado manualmente: {cardData.cardName} | Pontos restantes: {currentStatusPoints}");

        if (uiManager != null)
        {
            uiManager.ShowStatusCardApplied(cardData.cardName, cardData.description);
            uiManager.UpdateStatusCardsUI(); // ✅ AGORA FUNCIONA!
        }

        UpdatePlayerUI();
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
        // Limpa cards anteriores
        foreach (Transform child in cardsContainer)
        {
            Destroy(child.gameObject);
        }

        // Cria os novos cards
        foreach (var cardData in cards)
        {
            GameObject cardObj = Instantiate(statusCardPrefab, cardsContainer);
            StatusCardUI cardUI = cardObj.GetComponent<StatusCardUI>();

            if (cardUI != null)
            {
                cardUI.Initialize(cardData, this);
            }
        }

        Debug.Log($"🃏 Displaying {cards.Count} cards for player choice");
    }

    // 🆕 MÉTODO ATUALIZADO: Fecha automaticamente após escolha (apenas para escolha automática)
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

        Debug.Log($"✅ Card aplicado: {cardData.cardName} | Pontos restantes: {currentStatusPoints}");

        if (uiManager != null)
        {
            uiManager.ShowStatusCardApplied(cardData.cardName, cardData.description);
            uiManager.UpdateStatusCardsUI(); // ✅ AGORA FUNCIONA!
        }

        // 🆕 FECHA AUTOMATICAMENTE O PAINEL (apenas para escolha automática)
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
                Debug.Log($"❤️ Vida aumentada em {cardData.statBonus}");
                break;
            case StatusCardType.Attack:
                playerStats.attack += cardData.statBonus;
                Debug.Log($"⚔️ Ataque aumentado em {cardData.statBonus}");
                break;
            case StatusCardType.Defense:
                playerStats.defense += cardData.statBonus;
                Debug.Log($"🛡️ Defesa aumentada em {cardData.statBonus}");
                break;
            case StatusCardType.Speed:
                playerStats.speed += cardData.statBonus;
                Debug.Log($"🏃 Velocidade aumentada em {cardData.statBonus}");
                break;
            case StatusCardType.Regen:
                playerStats.healthRegenRate += cardData.statBonus;
                Debug.Log($"💚 Regeneração aumentada em {cardData.statBonus}");
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

    // 🆕 GETTER para verificar se pode comprar card
    public bool CanAffordCard(StatusCardData card)
    {
        return currentStatusPoints >= card.cost &&
               !activeBonuses.Exists(bonus => bonus.cardData.cardName == card.cardName);
    }

    public int GetCurrentStatusPoints() => currentStatusPoints;
    public List<ActiveStatusBonus> GetActiveBonuses() => new List<ActiveStatusBonus>(activeBonuses);
    public List<StatusCardData> GetAvailableCards() => new List<StatusCardData>(availableCards);

    [ContextMenu("Adicionar 5 Pontos de Status")]
    public void AddTestPoints()
    {
        currentStatusPoints += 5;
        Debug.Log($"🎯 +5 pontos de status! Total: {currentStatusPoints}");

        if (uiManager != null)
            uiManager.UpdateStatusCardsUI(); // ✅ AGORA FUNCIONA!
    }

    [ContextMenu("Forçar Escolha de Cards (Teste)")]
    public void ForceCardChoice()
    {
        OfferStatusCardChoice();
    }

    [ContextMenu("Abrir Painel Manual (Teste)")]
    public void OpenManualPanel()
    {
        if (cardChoicePanel != null)
        {
            List<StatusCardData> allAvailable = GetRandomStatusCardChoices(6);
            DisplayCardChoice(allAvailable);
            cardChoicePanel.SetActive(true);
        }
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