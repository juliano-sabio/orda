using UnityEngine;

[CreateAssetMenu(fileName = "New Passive", menuName = "Character System/Passive Data")]
public class PassiveData : ScriptableObject
{
    [Header("Informações")]
    public string passiveName;
    [TextArea(2, 6)]
    public string description;
    public Sprite passiveIcon;

    [Header("Bônus Diretos")]
    [Range(0f, 1f)]   public float xpBonusPercent   = 0f;
    [Range(0f, 100f)] public float attackBonus       = 0f;
    [Range(0f, 50f)]  public float defenseBonus      = 0f;
    [Range(0f, 20f)]  public float speedBonus        = 0f;
    [Range(0f, 200f)] public float healthBonus       = 0f;
    [Range(0f, 5f)]   public float regenBonus        = 0f;
    [Range(0f, 0.5f)] public float cooldownReduction = 0f;

    [Header("Metadados")]
    public bool isUnlocked    = true;
    public int  requiredLevel = 1;

    public string GetBonusDescription()
    {
        var sb = new System.Text.StringBuilder();
        if (xpBonusPercent   > 0) sb.AppendLine($"<color=#ffe066>+{xpBonusPercent * 100:0}% XP ganho</color>");
        if (attackBonus      > 0) sb.AppendLine($"<color=#ff8866>+{attackBonus} ATK</color>");
        if (defenseBonus     > 0) sb.AppendLine($"<color=#66aaff>+{defenseBonus} DEF</color>");
        if (speedBonus       > 0) sb.AppendLine($"<color=#88ffcc>+{speedBonus} VEL</color>");
        if (healthBonus      > 0) sb.AppendLine($"<color=#88ff88>+{healthBonus} HP máx</color>");
        if (regenBonus       > 0) sb.AppendLine($"<color=#88ff88>+{regenBonus}/s regen HP</color>");
        if (cooldownReduction > 0) sb.AppendLine($"<color=#cc88ff>-{cooldownReduction * 100:0}% cooldowns</color>");
        return sb.ToString().TrimEnd('\n', '\r');
    }
}
