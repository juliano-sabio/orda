using System;
using UnityEngine;

public enum StatusCardType
{
    Health,
    Attack,
    Defense,
    Speed,
    Regen,
    Elemental,
    Cooldown
}

public enum CardRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}

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