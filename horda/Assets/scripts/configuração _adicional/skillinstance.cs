using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

// 🚨 NUNCA coloque este script nos PREFABS! Só nas INSTÂNCIAS!
public class SkillCardInstance : MonoBehaviour
{
    [Header("Referências dos Componentes")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI statsText;
    public Image backgroundImage;
    public Image elementBackground;
    public TextMeshProUGUI rarityText;
    public Image rarityBorder;

    private SkillData currentSkillData;

    public void InitializeCard(SkillData skillData)
    {
        currentSkillData = skillData;

        if (skillData == null)
        {
            Debug.LogError("❌ SkillData é null!");
            return;
        }

        FillCardData();
        Debug.Log($"✅ Instância inicializada: {skillData.skillName}");
    }

    private void FillCardData()
    {
        // Nome
        if (nameText != null)
        {
            string elementIcon = currentSkillData.GetElementIcon();
            nameText.text = $"<b>{currentSkillData.skillName}</b>\n{elementIcon} {currentSkillData.element}";
        }

        // Descrição
        if (descriptionText != null)
        {
            descriptionText.text = currentSkillData.description;
        }

        // Stats
        if (statsText != null)
        {
            statsText.text = GetFormattedStats();
        }

        // Ícone
        if (iconImage != null && currentSkillData.icon != null)
        {
            iconImage.sprite = currentSkillData.icon;
            iconImage.color = Color.white;
        }

        // Cores
        ApplyElementColors();
        ApplyRarityColors();
    }

    private void ApplyElementColors()
    {
        if (currentSkillData == null) return;

        Color elementColor = GetElementColor(currentSkillData.element);

        if (backgroundImage != null)
            backgroundImage.color = elementColor * 0.2f + new Color(0.1f, 0.1f, 0.1f);

        if (elementBackground != null)
            elementBackground.color = elementColor * 0.6f;

        if (nameText != null)
            nameText.color = Color.Lerp(elementColor, Color.white, 0.3f);
    }

    private void ApplyRarityColors()
    {
        if (currentSkillData == null) return;

        Color rarityColor = GetRarityColor(currentSkillData.rarity);

        if (rarityText != null)
        {
            rarityText.text = currentSkillData.rarity.ToString().ToUpper();
            rarityText.color = rarityColor;
        }

        if (rarityBorder != null)
            rarityBorder.color = rarityColor;
    }

    private string GetFormattedStats()
    {
        StringBuilder sb = new StringBuilder();

        if (currentSkillData.healthBonus != 0) sb.Append($"❤️{currentSkillData.healthBonus} ");
        if (currentSkillData.attackBonus != 0) sb.Append($"⚔️{currentSkillData.attackBonus} ");
        if (currentSkillData.defenseBonus != 0) sb.Append($"🛡️{currentSkillData.defenseBonus} ");
        if (currentSkillData.speedBonus != 0) sb.Append($"🏃{currentSkillData.speedBonus} ");

        if (sb.Length == 0) sb.Append("💎 Bônus Passivo");

        return sb.ToString();
    }

    private Color GetElementColor(PlayerStats.Element element)
    {
        switch (element)
        {
            case PlayerStats.Element.Fire: return new Color(1f, 0.3f, 0.1f);
            case PlayerStats.Element.Ice: return new Color(0.1f, 0.5f, 1f);
            case PlayerStats.Element.Lightning: return new Color(0.8f, 0.8f, 0.1f);
            case PlayerStats.Element.Poison: return new Color(0.5f, 0.1f, 0.8f);
            default: return Color.white;
        }
    }

    private Color GetRarityColor(SkillRarity rarity)
    {
        switch (rarity)
        {
            case SkillRarity.Common: return new Color(0.7f, 0.7f, 0.7f);
            case SkillRarity.Uncommon: return new Color(0.2f, 0.8f, 0.2f);
            case SkillRarity.Rare: return new Color(0.2f, 0.4f, 1f);
            case SkillRarity.Epic: return new Color(0.6f, 0.2f, 0.8f);
            default: return Color.white;
        }
    }

    public SkillData GetSkillData()
    {
        return currentSkillData;
    }
}