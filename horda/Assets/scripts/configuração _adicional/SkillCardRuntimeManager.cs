using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

// 🚨 ESTE script NUNCA vai nos prefabs, só gerencia em runtime
public class SkillCardRuntimeManager : MonoBehaviour
{
    // 🎯 Dados da skill (SÓ LEITURA)
    public SkillData SkillData { get; private set; }

    // 🎯 Referências para os componentes de UI
    private Image iconImage;
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI descriptionText;
    private TextMeshProUGUI statsText;
    private Image backgroundImage;
    private Image elementBackground;
    private TextMeshProUGUI rarityText;
    private Image rarityBorder;

    // 🎯 INICIALIZAÇÃO SEGURA - só busca referências quando chamado
    public void InitializeRuntime(SkillData skillData)
    {
        SkillData = skillData;

        // 🎯 Busca referências APENAS quando necessário
        FindAllReferences();

        // 🎯 Aplica dados APENAS em runtime
        ApplySkillDataToUI();

        Debug.Log($"✅ Runtime Manager inicializado: {skillData.skillName}");
    }

    private void FindAllReferences()
    {
        // Busca TODAS as referências de uma vez
        iconImage = FindComponent<Image>("IconImageSlot", "IconArea");
        nameText = FindComponent<TextMeshProUGUI>("NameText", "NameArea");
        descriptionText = FindComponent<TextMeshProUGUI>("DescText", "DescArea");
        statsText = FindComponent<TextMeshProUGUI>("StatsText", "StatsArea");
        backgroundImage = GetComponent<Image>();
        elementBackground = FindComponent<Image>("IconArea");
        rarityText = FindComponent<TextMeshProUGUI>("RarityText");
        rarityBorder = FindComponent<Image>("RarityBorder");
    }

    private T FindComponent<T>(string childName, string parentName = null) where T : Component
    {
        Transform parent = transform;
        if (!string.IsNullOrEmpty(parentName))
        {
            parent = transform.Find(parentName);
            if (parent == null) return null;
        }

        Transform child = parent.Find(childName);
        return child?.GetComponent<T>();
    }

    private void ApplySkillDataToUI()
    {
        if (SkillData == null) return;

        // 🎯 APENAS em runtime - NUNCA no editor
        if (!Application.isPlaying) return;

        // Aplica dados
        if (nameText != null)
        {
            string elementIcon = SkillData.GetElementIcon();
            nameText.text = $"<b>{SkillData.skillName}</b>\n{elementIcon} {SkillData.element}";
        }

        if (descriptionText != null)
            descriptionText.text = SkillData.description;

        if (statsText != null)
            statsText.text = GetFormattedStats();

        if (iconImage != null && SkillData.icon != null)
        {
            iconImage.sprite = SkillData.icon;
            iconImage.color = Color.white;
        }

        // Aplica cores
        ApplyRuntimeColors();
    }

    private void ApplyRuntimeColors()
    {
        if (SkillData == null) return;

        Color elementColor = GetElementColor(SkillData.element);
        Color rarityColor = GetRarityColor(SkillData.rarity);

        // 🎯 Só aplica cores em runtime
        if (backgroundImage != null)
            backgroundImage.color = elementColor * 0.2f + new Color(0.1f, 0.1f, 0.1f);

        if (elementBackground != null)
            elementBackground.color = elementColor * 0.6f;

        if (nameText != null)
            nameText.color = Color.Lerp(elementColor, Color.white, 0.3f);

        if (rarityText != null)
        {
            rarityText.text = SkillData.rarity.ToString().ToUpper();
            rarityText.color = rarityColor;
        }

        if (rarityBorder != null)
            rarityBorder.color = rarityColor;
    }

    private string GetFormattedStats()
    {
        StringBuilder sb = new StringBuilder();

        if (SkillData.healthBonus != 0) sb.Append($"❤️{SkillData.healthBonus} ");
        if (SkillData.attackBonus != 0) sb.Append($"⚔️{SkillData.attackBonus} ");
        if (SkillData.defenseBonus != 0) sb.Append($"🛡️{SkillData.defenseBonus} ");
        if (SkillData.speedBonus != 0) sb.Append($"🏃{SkillData.speedBonus} ");

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
}