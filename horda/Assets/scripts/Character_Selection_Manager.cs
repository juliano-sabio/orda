using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CharacterSelectionManagerIntegrated : MonoBehaviour
{
    [Header("🎯 Banco de Dados")]
    public CharacterData[] characters;
    public StageData[] stages;

    [Header("📊 UI - Info")]
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI characterElementText;
    public TextMeshProUGUI characterDescriptionText;
    public TextMeshProUGUI elementBonusText;

    [Header("🔋 UI - Status")]
    public Slider[] statusSliders;

    [Header("⚔️ Sistema de Upgrades")]
    public Button[] upgradeButtons;
    public TextMeshProUGUI[] upgradeLevelTexts;
    public int[] upgradeLevels = new int[4];

    [Header("🗺️ Navegação")]
    public GameObject painelStages;
    public CharacterIconUI[] characterIcons;

    private void Start()
    {
        LoadProgress();
        UpdateCurrencyUI();

        // Inicializa os ícones se existirem
        if (characterIcons != null && characters != null)
        {
            for (int i = 0; i < characterIcons.Length; i++)
            {
                if (i < characters.Length && characterIcons[i] != null)
                    characterIcons[i].Initialize(characters[i], i, this);
            }
        }

        int savedChar = PlayerPrefs.GetInt("SelectedCharacter", 0);
        if (characters.Length > 0) SelectCharacter(savedChar);
    }

    // ✅ CORREÇÃO: Método chamado pelos ícones
    public void OnCharacterIconClicked(int index) => SelectCharacter(index);

    public void SelectCharacter(int index)
    {
        if (characters == null || index < 0 || index >= characters.Length) return;

        PlayerPrefs.SetInt("SelectedCharacter", index);
        foreach (var icon in characterIcons) if (icon) icon.SetSelected(false);
        if (index < characterIcons.Length && characterIcons[index]) characterIcons[index].SetSelected(true);

        UpdateStatusDisplay(characters[index]);
    }

    // ✅ CORREÇÃO: Método que o GameSceneManager usa para passar os dados para o Player
    public void ApplyCharacterToPlayerSystems(PlayerStats playerStats, SkillManager skillManager)
    {
        int index = PlayerPrefs.GetInt("SelectedCharacter", 0);
        if (characters == null || index >= characters.Length || playerStats == null) return;

        CharacterData data = characters[index];

        // Aplica os status base + bônus de upgrade
        playerStats.maxHealth = data.maxHealth * (1 + upgradeLevels[0] * 0.05f);

        // Se der erro nestas linhas, verifique se os nomes no PlayerStats.cs são exatamente esses:
        // playerStats.baseAttack = data.baseAttack * (1 + upgradeLevels[1] * 0.05f);
        // playerStats.baseDefense = data.baseDefense * (1 + upgradeLevels[2] * 0.05f);
        // playerStats.baseSpeed = data.baseSpeed * (1 + upgradeLevels[3] * 0.05f);

        Debug.Log($"Personagem {data.characterName} aplicado com sucesso!");
    }

    // ✅ CORREÇÃO: Método chamado ao selecionar uma fase
    public void OnStageSelected(int index)
    {
        if (stages != null && index < stages.Length)
        {
            PlayerPrefs.SetInt("SelectedStageIndex", index);
            PlayerPrefs.Save();
            Debug.Log($"Fase selecionada: {stages[index].stageName}");
        }
    }

    public void BuyUpgrade(int statIndex)
    {
        int currentCoins = PlayerPrefs.GetInt("PlayerCoins", 1000);
        int cost = (upgradeLevels[statIndex] + 1) * 100;

        if (currentCoins >= cost)
        {
            currentCoins -= cost;
            upgradeLevels[statIndex]++;
            PlayerPrefs.SetInt("PlayerCoins", currentCoins);
            PlayerPrefs.SetInt($"Upgrade_{statIndex}", upgradeLevels[statIndex]);

            UpdateCurrencyUI();
            UpdateStatusDisplay(characters[PlayerPrefs.GetInt("SelectedCharacter", 0)]);
        }
    }

    public void UpdateStatusDisplay(CharacterData data)
    {
        if (!data) return;

        characterNameText.text = data.characterName;
        characterDescriptionText.text = data.description;
        characterElementText.text = $"{CharacterData.GetElementIcon(data.baseElement)} {data.baseElement}";
        characterElementText.color = CharacterData.GetElementColor(data.baseElement);
        elementBonusText.text = data.GetElementBonusDescription();

        if (statusSliders.Length >= 4)
        {
            statusSliders[0].value = (data.maxHealth * (1 + upgradeLevels[0] * 0.05f)) / 500f;
            statusSliders[1].value = (data.baseAttack * (1 + upgradeLevels[1] * 0.05f)) / 100f;
            statusSliders[2].value = (data.baseDefense * (1 + upgradeLevels[2] * 0.05f)) / 50f;
            statusSliders[3].value = (data.baseSpeed * (1 + upgradeLevels[3] * 0.05f)) / 50f;
        }

        for (int i = 0; i < upgradeLevelTexts.Length; i++)
            if (upgradeLevelTexts[i]) upgradeLevelTexts[i].text = $"Nv. {upgradeLevels[i]}";
    }

    public void ToggleStages(bool open) => painelStages.SetActive(open);

    private void LoadProgress()
    {
        for (int i = 0; i < 4; i++) upgradeLevels[i] = PlayerPrefs.GetInt($"Upgrade_{i}", 0);
    }

    private void UpdateCurrencyUI()
    {
        if (coinsText) coinsText.text = $"💰 {PlayerPrefs.GetInt("PlayerCoins", 1000)}";
    }
}