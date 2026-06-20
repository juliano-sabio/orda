using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusCardSystem : MonoBehaviour
{
    public static StatusCardSystem Instance;

    // Bonus per stat per rarity: [statIndex][rarityIndex] — Common, Rare, Mystic, Curse
    private static readonly float[][] BonusTable =
    {
        new float[] { 15f,   30f,   50f,   80f   }, // Health
        new float[] { 3f,    6f,    12f,   20f   }, // Attack
        new float[] { 2f,    5f,    8f,    15f   }, // Defense
        new float[] { 0.3f,  0.7f,  1.2f,  2.0f  }, // Speed
        new float[] { 0.3f,  0.7f,  1.5f,  2.5f  }, // Regen
        new float[] { 0.02f, 0.05f, 0.10f, 0.20f }, // CriticalChance (+% por raridade)
        new float[] { 0.1f,  0.2f,  0.4f,  0.7f  }, // AttackSpeed (redução em segundos)
        new float[] { 10f,   20f,   35f,   60f   }, // Shield
    };

    // Rarity weights: Common 60%, Rare 30%, Mystic 10% — Curse excluded (future system)
    private static readonly float[] RarityWeights = { 60f, 30f, 10f };

    private static readonly string[] CardTitles =
    {
        "Vitalidade", "Forca", "Resistencia", "Agilidade",
        "Regeneracao", "Precisao", "Reflexos", "Escudo"
    };
    private static readonly string[] StatLabels =
    {
        "Vida", "ATQ", "DEF", "Vel",
        "Regen", "Critico", "Vel.Atq", "Escudo"
    };
    private static readonly string[] RarityLabels = { "Comum", "Raro", "Mistico", "Amaldicoado" };

    private PlayerStats playerStats;
    private StatusCardChoiceUI choiceUI;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    void Start()
    {
        playerStats = PlayerStats.Local;
        choiceUI    = FindAnyObjectByType<StatusCardChoiceUI>(FindObjectsInactive.Include);
    }

    public void OnPlayerLevelUp(int newLevel)
    {
        StartCoroutine(DelayedOffer());
    }

    private IEnumerator DelayedOffer()
    {
        yield return new WaitForSecondsRealtime(0.5f);

        // Aguarda outras UIs de seleção fecharem antes de abrir
        float timeout = 10f;
        while (timeout > 0f &&
               (SkillEvolutionUI.Instance != null && SkillEvolutionUI.Instance.Visivel))
        {
            yield return new WaitForSecondsRealtime(0.1f);
            timeout -= 0.1f;
        }

        OfferCardChoice();
    }

    public void OfferCardChoice()
    {
        // Não mostra status cards em níveis de skill
        var sm = SkillManager.Instance;
        if (sm != null)
        {
            int level = playerStats != null ? playerStats.level : 0;
            if (sm.IsSkillLevel(level)) return;
        }

        if (choiceUI == null) choiceUI = FindAnyObjectByType<StatusCardChoiceUI>(FindObjectsInactive.Include);
        if (choiceUI == null)
        {
            Debug.LogWarning("[StatusCardSystem] StatusCardChoiceUI nao encontrado na cena");
            return;
        }

        Debug.Log("[StatusCardSystem] Abrindo cartas de status...");
        choiceUI.Show(GenerateChoices(3), ApplyCard);
    }

    List<StatusCardInfo> GenerateChoices(int count)
    {
        var result    = new List<StatusCardInfo>();
        var usedStats = new List<int>();
        int statCount = Enum.GetValues(typeof(StatusCardType)).Length;

        for (int i = 0; i < count; i++)
        {
            CardRarity rarity  = RollRarity();
            int statIdx        = PickUnusedStat(usedStats, statCount);
            usedStats.Add(statIdx);
            result.Add(BuildCard((StatusCardType)statIdx, rarity));
        }

        return result;
    }

    CardRarity RollRarity()
    {
        float total = 0f;
        foreach (float w in RarityWeights) total += w;
        float roll = UnityEngine.Random.Range(0f, total);
        float cum  = 0f;
        for (int i = 0; i < RarityWeights.Length; i++)
        {
            cum += RarityWeights[i];
            if (roll < cum) return (CardRarity)i;
        }
        return CardRarity.Common;
    }

    int PickUnusedStat(List<int> used, int total)
    {
        var available = new List<int>();
        for (int i = 0; i < total; i++)
            if (!used.Contains(i)) available.Add(i);
        if (available.Count == 0) return UnityEngine.Random.Range(0, total);
        return available[UnityEngine.Random.Range(0, available.Count)];
    }

    StatusCardInfo BuildCard(StatusCardType statType, CardRarity rarity)
    {
        int si      = (int)statType;
        int ri      = (int)rarity;
        float bonus = BonusTable[si][ri];

        string desc = BuildDescription(statType, bonus);

        return new StatusCardInfo
        {
            cardName    = Loc.T($"card.title.{statType.ToString().ToLower()}"),
            description = desc,
            statType    = statType,
            rarity      = rarity,
            bonus       = bonus
        };
    }

    string BuildDescription(StatusCardType statType, float bonus)
    {
        if (playerStats == null)
            playerStats = PlayerStats.Local;

        if (playerStats == null)
            return $"{Loc.T($"card.title.{statType.ToString().ToLower()}")}: +{bonus}";

        switch (statType)
        {
            case StatusCardType.Health:
            {
                float atual = playerStats.maxHealth;
                return $"{Loc.T("stat.hp_max")}: {atual:F0} → {atual + bonus:F0}";
            }
            case StatusCardType.Attack:
            {
                float atual = playerStats.attack;
                return $"{Loc.T("stat.atk")}: {atual:F1} → {atual + bonus:F1}";
            }
            case StatusCardType.Defense:
            {
                float atual = playerStats.defense;
                return $"{Loc.T("stat.def")}: {atual:F1} → {atual + bonus:F1}";
            }
            case StatusCardType.Speed:
            {
                float atual = playerStats.speed;
                return $"{Loc.T("stat.spd")}: {atual:F1} → {atual + bonus:F1}";
            }
            case StatusCardType.Regen:
            {
                float atual = playerStats.healthRegenRate;
                return $"{Loc.T("stat.regen")}: {atual:F1}/s → {atual + bonus:F1}/s";
            }
            case StatusCardType.CriticalChance:
            {
                float atual = playerStats.critChance * 100f;
                float novo  = Mathf.Clamp(playerStats.critChance + bonus, 0f, 0.95f) * 100f;
                return $"{Loc.T("stat.crit")}: {atual:F0}% → {novo:F0}%";
            }
            case StatusCardType.AttackSpeed:
            {
                float atual = playerStats.attackActivationInterval;
                float novo  = Mathf.Max(0.2f, atual - bonus);
                return $"{Loc.T("stat.atkspd")}: {atual:F1}s → {novo:F1}s";
            }
            case StatusCardType.Shield:
            {
                float atual = playerStats.maxShieldPoints;
                return $"{Loc.T("stat.shield_max")}: {atual:F0} → {atual + bonus:F0}";
            }
            default:
                return $"{Loc.T($"card.title.{statType.ToString().ToLower()}")}: +{bonus}";
        }
    }

    public void ApplyCard(StatusCardInfo card)
    {
        if (playerStats == null) playerStats = PlayerStats.Local;
        if (playerStats == null) return;

        switch (card.statType)
        {
            case StatusCardType.Health:
                playerStats.maxHealth += card.bonus;
                playerStats.health    += card.bonus;
                break;
            case StatusCardType.Attack:  playerStats.attack          += card.bonus; break;
            case StatusCardType.Defense: playerStats.defense         += card.bonus; break;
            case StatusCardType.Speed:   playerStats.speed           += card.bonus; break;
            case StatusCardType.Regen:   playerStats.healthRegenRate += card.bonus; break;
            case StatusCardType.CriticalChance:
                playerStats.critChance = Mathf.Clamp(playerStats.critChance + card.bonus, 0f, 0.95f);
                break;
            case StatusCardType.AttackSpeed:
                playerStats.attackActivationInterval =
                    Mathf.Max(0.2f, playerStats.attackActivationInterval - card.bonus);
                break;
            case StatusCardType.Shield:
                playerStats.bonusShieldPoints += card.bonus;
                playerStats.maxShieldPoints   += card.bonus;
                playerStats.shieldPoints      += card.bonus;
                break;
        }

        playerStats.ForceUIUpdate();
    }

    // ── Context menu helpers ────────────────────────────────────────────────

    [ContextMenu("Forcar Escolha de Cards (Teste)")]
    public void ForceCardChoice() => OfferCardChoice();

    // ── Backwards-compat stubs (used by UIManege / StatusCardCanvasCreator) ─

    public bool CanAffordCard(StatusCardData card)    => false;
    public int  GetCurrentStatusPoints()              => 0;
    public List<StatusCardData>    GetAvailableCards() => new List<StatusCardData>();
    public List<ActiveStatusBonus> GetActiveBonuses()  => new List<ActiveStatusBonus>();
    public void AddTestPoints()        { }
    public void UpdateStatusCardsUI()  { }
    public void ShowStatusPointsGained(int v) { }
    public void ShowStatusCardApplied(string n, string d) { }
    public void ApplyStatusCard(StatusCardData c)        { }
    public void ApplyStatusCardManually(StatusCardData c) { }
    public void OpenManualPanel()  { }

    // Keep allStatusCards list so existing Inspector wiring doesn't break
    [HideInInspector] public List<StatusCardData> allStatusCards = new List<StatusCardData>();
    [HideInInspector] public GameObject statusCardPrefab;
    [HideInInspector] public Transform  cardsContainer;
    [HideInInspector] public GameObject cardChoicePanel;
}
