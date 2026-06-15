using UnityEngine;

[CreateAssetMenu(fileName = "New Passive", menuName = "Character System/Passive Data")]
public class PassiveData : ScriptableObject
{
    [Header("Informações")]
    public string passiveName;
    [TextArea(2, 6)]
    public string description;
    public Sprite passiveIcon;

    [Header("Localização (chaves GameStrings)")]
    public string nameKey        = "";
    public string descriptionKey = "";

    [Header("Bônus Diretos")]
    [Range(0f, 1f)]   public float xpBonusPercent   = 0f;
    [Range(0f, 100f)] public float attackBonus       = 0f;
    [Range(0f, 50f)]  public float defenseBonus      = 0f;
    [Range(0f, 20f)]  public float speedBonus        = 0f;
    [Range(0f, 200f)] public float healthBonus       = 0f;
    [Range(0f, 5f)]   public float regenBonus        = 0f;
    [Range(0f, 0.5f)] public float cooldownReduction = 0f;

    [Header("Comportamento Especial")]
    public string behaviorScriptName;

    [Header("Metadados")]
    public bool isUnlocked    = true;
    public int  requiredLevel = 1;

    public string GetDisplayName()
    {
        return !string.IsNullOrEmpty(nameKey) ? Loc.T(nameKey) : passiveName;
    }

    public string GetDisplayDescription()
    {
        return !string.IsNullOrEmpty(descriptionKey) ? Loc.T(descriptionKey) : description;
    }

    public string GetBonusDescription()
    {
        var sb = new System.Text.StringBuilder();
        if (xpBonusPercent   > 0) sb.AppendLine($"<color=#ffe066>+{xpBonusPercent * 100:0}{Loc.T("ui.xp_bonus")}</color>");
        if (attackBonus      > 0) sb.AppendLine($"<color=#ff8866>+{attackBonus} {Loc.T("stat.atk")}</color>");
        if (defenseBonus     > 0) sb.AppendLine($"<color=#66aaff>+{defenseBonus} {Loc.T("stat.def")}</color>");
        if (speedBonus       > 0) sb.AppendLine($"<color=#88ffcc>+{speedBonus} {Loc.T("stat.spd")}</color>");
        if (healthBonus      > 0) sb.AppendLine($"<color=#88ff88>+{healthBonus} {Loc.T("ui.hp_max")}</color>");
        if (regenBonus       > 0) sb.AppendLine($"<color=#88ff88>+{regenBonus}{Loc.T("ui.hp_regen")}</color>");
        if (cooldownReduction > 0) sb.AppendLine($"<color=#cc88ff>-{cooldownReduction * 100:0}{Loc.T("ui.cooldowns")}</color>");
        return sb.ToString().TrimEnd('\n', '\r');
    }
}
