using UnityEngine;

public enum StatusCardType
{
    Health,
    Attack,
    Defense,
    Speed,
    Regen,
    CriticalChance,
    AttackSpeed,
    Shield
}

public enum CardRarity
{
    Common,
    Rare,
    Mystic,
    Curse
}

// Runtime card data — generated procedurally, not a ScriptableObject
[System.Serializable]
public class StatusCardInfo
{
    public string cardName;
    public string description;
    public StatusCardType statType;
    public CardRarity rarity;
    public float bonus;
    // Curse cards also remove stats (future)
    public StatusCardType penaltyStatType;
    public float penalty;
    public bool HasPenalty => penalty > 0f;
}

// ScriptableObject kept for backwards compatibility with existing assets
[CreateAssetMenu(fileName = "New Status Card", menuName = "Game/Status Card")]
[System.Serializable]
public class StatusCardData : ScriptableObject
{
    public string cardName;
    [TextArea(3, 5)]
    public string description;
    public StatusCardType cardType;
    public CardRarity rarity;
    public float statBonus;
    public float secondaryBonus;
    public int cost;
    public Sprite icon;
    public int requiredLevel = 1;
}

[System.Serializable]
public class ActiveStatusBonus
{
    public StatusCardData cardData;
    public float appliedTime;
}
