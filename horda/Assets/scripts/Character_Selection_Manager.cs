using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CharacterSelectionManagerIntegrated : MonoBehaviour
{
    [Header("🎯 Sistema de Seleção de Personagens")]
    public CharacterData[] characters;
    public CharacterIconUI[] characterIcons;
    public int selectedCharacterIndex = 0;

    [Header("💰 Sistema de Moedas")]
    public int playerCoins = 1000;
    public TextMeshProUGUI coinsText;

    [Header("🎯 Sistema de Upgrades Expandido")]
    public GameObject grupoUpgrades;
    public Button[] upgradeBotoes;
    public TextMeshProUGUI[] upgradeNiveis;
    public TextMeshProUGUI[] upgradeCustos;
    public string[] upgradeTypes = {
        "Health", "Attack", "Defense", "Speed",
        "HealthRegen", "AttackCooldown", "DefenseCooldown"
    };

    [Header("🎮 Sistema de Stages")]
    public StageData[] stages; // 🆕 AGORA USA StageData EXISTENTE
    public StageButtonUI[] stageButtons;
    public TextMeshProUGUI stageNameText;
    public TextMeshProUGUI stageDifficultyText;
    public int selectedStageIndex = 0;

    [Header("⏱️ Sistema de Cooldown e Regen")]
    public Slider healthRegenSlider;
    public TextMeshProUGUI healthRegenValueText;
    public Slider attackCooldownSlider;
    public TextMeshProUGUI attackCooldownValueText;
    public Slider defenseCooldownSlider;
    public TextMeshProUGUI defenseCooldownValueText;

    private int[] upgradeLevels = new int[7];

    [Header("📊 UI References")]
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI characterLevelText;
    public TextMeshProUGUI characterElementText;
    public TextMeshProUGUI characterDescriptionText;
    public Slider[] statusSliders;
    public TextMeshProUGUI[] statusValues;

    // 🆕 DADOS DO JOGADOR SALVOS
    private int playerLevel = 1;
    private int unlockedCharacters = 1;

    void Start()
    {
        LoadPlayerData();
        InitializeCharacterSystem();
        InitializeStageSystem();
        UpdateUI();
        UpdateUpgradesUI();
        UpdateCooldownAndRegenUI();
    }

    // 🆕 SISTEMA DE STAGES (ATUALIZADO PARA StageData)
    private void InitializeStageSystem()
    {
        if (stageButtons != null && stages != null)
        {
            for (int i = 0; i < stageButtons.Length && i < stages.Length; i++)
            {
                if (stageButtons[i] != null)
                {
                    int stageIndex = i;
                    stageButtons[i].Initialize(stages[i], stageIndex, this);
                }
            }

            if (stages.Length > 0)
            {
                OnStageSelected(0);
            }
        }
    }

    public void OnStageSelected(int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= stages.Length) return;

        selectedStageIndex = stageIndex;
        StageData selectedStage = stages[stageIndex];

        if (stageNameText != null)
            stageNameText.text = selectedStage.stageName;

        if (stageDifficultyText != null)
            stageDifficultyText.text = $"Dificuldade: {selectedStage.difficulty}/5";

        for (int i = 0; i < stageButtons.Length; i++)
        {
            if (stageButtons[i] != null)
            {
                stageButtons[i].SetSelected(i == stageIndex);
            }
        }

        Debug.Log($"🎯 Stage selecionado: {selectedStage.stageName}");
    }

    // 🆕 MÉTODOS DE DADOS DO JOGADOR (ATUALIZADOS)
    private void LoadPlayerData()
    {
        playerCoins = PlayerPrefs.GetInt("PlayerCoins", 1000);
        playerLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
        unlockedCharacters = PlayerPrefs.GetInt("UnlockedCharacters", 1);

        for (int i = 0; i < upgradeLevels.Length; i++)
        {
            upgradeLevels[i] = PlayerPrefs.GetInt($"UpgradeLevel_{i}", 1);
        }

        // 🆕 CARREGAR DESBLOQUEIO BASEADO NO unlockLevel DO CharacterData
        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] != null)
            {
                characters[i].unlocked = (playerLevel >= characters[i].unlockLevel) || (i < unlockedCharacters);
            }
        }

        // 🆕 CARREGAR STAGES DESBLOQUEADOS (usa 'unlocked' do StageData)
        for (int i = 0; i < stages.Length; i++)
        {
            if (i == 0) // Primeiro stage sempre desbloqueado
                stages[i].unlocked = true;
            else
                stages[i].unlocked = PlayerPrefs.GetInt($"StageUnlocked_{i}", 0) == 1;
        }
    }

    public void SavePlayerData()
    {
        PlayerPrefs.SetInt("PlayerCoins", playerCoins);
        PlayerPrefs.SetInt("PlayerLevel", playerLevel);
        PlayerPrefs.SetInt("UnlockedCharacters", unlockedCharacters);

        for (int i = 0; i < upgradeLevels.Length; i++)
        {
            PlayerPrefs.SetInt($"UpgradeLevel_{i}", upgradeLevels[i]);
        }

        // 🆕 SALVAR STAGES DESBLOQUEADOS
        for (int i = 0; i < stages.Length; i++)
        {
            PlayerPrefs.SetInt($"StageUnlocked_{i}", stages[i].unlocked ? 1 : 0);
        }

        PlayerPrefs.Save();
        Debug.Log("💾 Dados do jogador salvos!");
    }

    // 🆕 INICIALIZAÇÃO DO SISTEMA DE PERSONAGENS
    private void InitializeCharacterSystem()
    {
        for (int i = 0; i < characterIcons.Length && i < characters.Length; i++)
        {
            if (characterIcons[i] != null && characters[i] != null)
            {
                characterIcons[i].Initialize(characters[i], i, this);
            }
        }

        if (characters.Length > 0 && characters[0] != null)
        {
            SelectCharacter(0);
        }
    }

    // 🆕 MÉTODO DE UPGRADE
    public void UpgradeStat(int statIndex)
    {
        if (statIndex < 0 || statIndex >= upgradeLevels.Length) return;

        int upgradeCost = upgradeLevels[statIndex] * 100;

        if (playerCoins < upgradeCost)
        {
            Debug.Log("Moedas insuficientes!");
            return;
        }

        playerCoins -= upgradeCost;
        upgradeLevels[statIndex]++;

        SavePlayerData();
        UpdateUI();
        UpdateUpgradesUI();
        UpdateCooldownAndRegenUI();

        Debug.Log($"🆙 Upgrade aplicado! {upgradeTypes[statIndex]} Nível: {upgradeLevels[statIndex]}");
    }

    // 🆕 ATUALIZAR UI DE UPGRADES
    void UpdateUpgradesUI()
    {
        for (int i = 0; i < upgradeLevels.Length; i++)
        {
            if (upgradeNiveis != null && i < upgradeNiveis.Length)
                upgradeNiveis[i].text = $"Nv.{upgradeLevels[i]}";

            if (upgradeCustos != null && i < upgradeCustos.Length)
            {
                int custo = upgradeLevels[i] * 100;
                upgradeCustos[i].text = custo.ToString();
            }

            if (upgradeBotoes != null && i < upgradeBotoes.Length)
            {
                int custo = upgradeLevels[i] * 100;
                upgradeBotoes[i].interactable = playerCoins >= custo;
            }
        }
    }

    // 🆕 ATUALIZAR UI DE COOLDOWN E REGENERAÇÃO
    void UpdateCooldownAndRegenUI()
    {
        float healthRegenValue = 1f + (upgradeLevels[4] - 1) * 0.5f;
        float attackCooldownReduction = upgradeLevels[5] * 0.05f;
        float defenseCooldownReduction = upgradeLevels[6] * 0.05f;

        if (healthRegenSlider != null)
        {
            healthRegenSlider.value = healthRegenValue / 5f;
            if (healthRegenValueText != null)
                healthRegenValueText.text = $"{healthRegenValue:F1}/s";
        }

        if (attackCooldownSlider != null)
        {
            attackCooldownSlider.value = attackCooldownReduction / 0.5f;
            if (attackCooldownValueText != null)
                attackCooldownValueText.text = $"-{attackCooldownReduction * 100:F0}%";
        }

        if (defenseCooldownSlider != null)
        {
            defenseCooldownSlider.value = defenseCooldownReduction / 0.5f;
            if (defenseCooldownValueText != null)
                defenseCooldownValueText.text = $"-{defenseCooldownReduction * 100:F0}%";
        }
    }

    // 🆕 APLICAR UPGRADES AO PERSONAGEM
    public void ApplyUpgradesToCharacter(PlayerStats playerStats, CharacterData characterData)
    {
        if (playerStats == null || characterData == null) return;

        // 🆕 USA OS VALORES DO SEU CharacterData
        float healthBonus = characterData.maxHealth * (upgradeLevels[0] - 1) * 0.05f;
        float attackBonus = characterData.baseAttack * (upgradeLevels[1] - 1) * 0.05f;
        float defenseBonus = characterData.baseDefense * (upgradeLevels[2] - 1) * 0.05f;
        float speedBonus = characterData.baseSpeed * (upgradeLevels[3] - 1) * 0.03f;
        float regenBonus = characterData.baseHealthRegen * (upgradeLevels[4] - 1) * 0.5f;
        float attackCooldownReduction = upgradeLevels[5] * 0.05f;
        float defenseCooldownReduction = upgradeLevels[6] * 0.05f;

        // APLICA BÔNUS
        playerStats.maxHealth = characterData.maxHealth + healthBonus;
        playerStats.attack = characterData.baseAttack + attackBonus;
        playerStats.defense = characterData.baseDefense + defenseBonus;
        playerStats.speed = characterData.baseSpeed + speedBonus;
        playerStats.healthRegenRate = characterData.baseHealthRegen + regenBonus;
        playerStats.healthRegenDelay = characterData.baseRegenDelay;
        playerStats.attackActivationInterval = Mathf.Max(0.1f, characterData.baseAttackCooldown * (1f - attackCooldownReduction));
        playerStats.defenseActivationInterval = Mathf.Max(0.1f, characterData.baseDefenseCooldown * (1f - defenseCooldownReduction));

        // 🆕 APLICA BÔNUS DE ELEMENTO DO SEU CharacterData
        playerStats.CurrentElement = characterData.baseElement;
        if (characterData.baseElement != PlayerStats.Element.None)
        {
            playerStats.attack += characterData.elementAttackBonus;
            playerStats.defense += characterData.elementDefenseBonus;
            playerStats.speed += characterData.elementSpeedBonus;
            playerStats.attackActivationInterval *= (1f - characterData.elementCooldownReduction);
            playerStats.defenseActivationInterval *= (1f - characterData.elementCooldownReduction);
        }

        Debug.Log($"🎯 {characterData.characterName} - Upgrades aplicados");
    }

    // 🆕 MÉTODO PARA APLICAR PERSONAGEM AOS SISTEMAS (SIMPLIFICADO)
    public void ApplyCharacterToPlayerSystems(PlayerStats playerStats, SkillManager skillManager)
    {
        if (selectedCharacterIndex < 0 || selectedCharacterIndex >= characters.Length) return;
        if (characters[selectedCharacterIndex] == null) return;

        CharacterData selectedCharacter = characters[selectedCharacterIndex];

        ApplyCharacterBaseStats(playerStats, selectedCharacter);
        ApplyUpgradesToCharacter(playerStats, selectedCharacter);

        // 🆕 APLICA ULTIMATE DO SEU CharacterData (SIMPLIFICADO)
        if (selectedCharacter.ultimateSkill != null)
        {
            Debug.Log($"✨ Ultimate aplicada: {selectedCharacter.ultimateSkill.ultimateName}");
        }

        Debug.Log($"🎮 {selectedCharacter.characterName} aplicado ao PlayerStats!");
    }

    private void ApplyCharacterBaseStats(PlayerStats playerStats, CharacterData character)
    {
        playerStats.health = character.maxHealth;
        playerStats.maxHealth = character.maxHealth;
        playerStats.attack = character.baseAttack;
        playerStats.defense = character.baseDefense;
        playerStats.speed = character.baseSpeed;
        playerStats.healthRegenRate = character.baseHealthRegen;
        playerStats.healthRegenDelay = character.baseRegenDelay;
        playerStats.attackActivationInterval = character.baseAttackCooldown;
        playerStats.defenseActivationInterval = character.baseDefenseCooldown;
        playerStats.CurrentElement = character.baseElement;
    }

    // 🆕 ATUALIZAR DISPLAY DE STATUS
    public void UpdateStatusDisplay(CharacterData character)
    {
        if (character == null) return;

        // ATUALIZA TEXTO BÁSICO
        if (characterNameText != null)
            characterNameText.text = character.characterName;

        if (characterDescriptionText != null)
            characterDescriptionText.text = character.description;

        if (characterLevelText != null)
            characterLevelText.text = $"Nível {playerLevel}";

        if (characterElementText != null)
        {
            string elementIcon = character.GetElementIcon();
            characterElementText.text = $"Elemento: {elementIcon} {character.baseElement}";
            characterElementText.color = character.GetElementColor();
        }

        // 🆕 CALCULA BÔNUS COM UPGRADES
        float healthBonus = character.maxHealth * (upgradeLevels[0] - 1) * 0.05f;
        float attackBonus = character.baseAttack * (upgradeLevels[1] - 1) * 0.05f;
        float defenseBonus = character.baseDefense * (upgradeLevels[2] - 1) * 0.05f;
        float speedBonus = character.baseSpeed * (upgradeLevels[3] - 1) * 0.03f;
        float regenBonus = character.baseHealthRegen * (upgradeLevels[4] - 1) * 0.5f;

        // ATUALIZA SLIDERS
        UpdateStatusSlider(0, character.maxHealth + healthBonus, 200f, "Vida");
        UpdateStatusSlider(1, character.baseAttack + attackBonus, 50f, "Ataque");
        UpdateStatusSlider(2, character.baseDefense + defenseBonus, 30f, "Defesa");
        UpdateStatusSlider(3, character.baseSpeed + speedBonus, 20f, "Velocidade");
        UpdateStatusSlider(4, character.baseHealthRegen + regenBonus, 10f, "Regeneração");

        // 🆕 COOLDOWNS
        float attackCooldownReduction = upgradeLevels[5] * 5f;
        float defenseCooldownReduction = upgradeLevels[6] * 5f;

        UpdateStatusSlider(5, attackCooldownReduction, 50f, "Red. CD Ataque");
        UpdateStatusSlider(6, defenseCooldownReduction, 50f, "Red. CD Defesa");

        UpdateCooldownAndRegenUI();
    }

    private void UpdateStatusSlider(int index, float value, float maxValue, string label)
    {
        if (statusSliders != null && index < statusSliders.Length)
        {
            statusSliders[index].value = value / maxValue;
        }

        if (statusValues != null && index < statusValues.Length)
        {
            statusValues[index].text = $"{label}: {value:F1}";
        }
    }

    // 🆕 MÉTODOS DE SELEÇÃO DE PERSONAGEM
    public void OnCharacterIconClicked(int characterIndex)
    {
        SelectCharacter(characterIndex);
    }

    public void SelectCharacter(int characterIndex)
    {
        if (characterIndex < 0 || characterIndex >= characters.Length) return;
        if (characters[characterIndex] == null || !characters[characterIndex].unlocked) return;

        // DESMARCA PERSONAGEM ANTERIOR
        if (selectedCharacterIndex >= 0 && selectedCharacterIndex < characterIcons.Length)
        {
            if (characterIcons[selectedCharacterIndex] != null)
                characterIcons[selectedCharacterIndex].SetSelected(false);
        }

        // MARCA NOVO PERSONAGEM
        selectedCharacterIndex = characterIndex;

        if (characterIcons[characterIndex] != null)
            characterIcons[characterIndex].SetSelected(true);

        // ATUALIZA UI
        UpdateStatusDisplay(characters[characterIndex]);
        UpdateUI();

        Debug.Log($"🎯 Personagem selecionado: {characters[characterIndex].characterName}");
    }

    // 🆕 ATUALIZAR UI PRINCIPAL
    private void UpdateUI()
    {
        if (coinsText != null)
            coinsText.text = $"Moedas: {playerCoins}";

        if (selectedCharacterIndex >= 0 && selectedCharacterIndex < characters.Length)
        {
            UpdateStatusDisplay(characters[selectedCharacterIndex]);
        }
    }

    // 🆕 MÉTODOS ADICIONAIS
    public void AddCoins(int amount)
    {
        playerCoins += amount;
        SavePlayerData();
        UpdateUI();
        UpdateUpgradesUI();
        Debug.Log($"💰 +{amount} moedas! Total: {playerCoins}");
    }

    public void UnlockCharacter(int characterIndex)
    {
        if (characterIndex < 0 || characterIndex >= characters.Length) return;
        if (characters[characterIndex] == null) return;

        characters[characterIndex].unlocked = true;
        if (unlockedCharacters <= characterIndex)
            unlockedCharacters = characterIndex + 1;

        if (characterIcons[characterIndex] != null)
            characterIcons[characterIndex].RefreshStatus();

        SavePlayerData();
        Debug.Log($"🔓 {characters[characterIndex].characterName} desbloqueado!");
    }

    public void UnlockStage(int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= stages.Length) return;

        stages[stageIndex].unlocked = true;

        if (stageButtons[stageIndex] != null)
            stageButtons[stageIndex].RefreshStatus();

        SavePlayerData();
        Debug.Log($"🔓 Stage {stages[stageIndex].stageName} desbloqueado!");
    }

    public void StartGame()
    {
        if (selectedCharacterIndex < 0 || selectedCharacterIndex >= characters.Length || characters[selectedCharacterIndex] == null)
        {
            Debug.LogError("❌ Nenhum personagem selecionado!");
            return;
        }

        if (!characters[selectedCharacterIndex].unlocked)
        {
            Debug.LogError("❌ Personagem não desbloqueado!");
            return;
        }

        if (selectedStageIndex < 0 || selectedStageIndex >= stages.Length)
        {
            Debug.LogError("❌ Nenhum stage selecionado!");
            return;
        }

        if (!stages[selectedStageIndex].unlocked)
        {
            Debug.LogError("❌ Stage não desbloqueado!");
            return;
        }

        Debug.Log($"🚀 Iniciando jogo com {characters[selectedCharacterIndex].characterName} no stage {stages[selectedStageIndex].stageName}");
    }

    // 🆕 CONFIGURAÇÃO AUTOMÁTICA DE REFERÊNCIAS
    public void ConfigurarReferenciasAutomaticamente()
    {
        coinsText = GameObject.Find("TextoMoedas")?.GetComponent<TextMeshProUGUI>();
        grupoUpgrades = GameObject.Find("GrupoUpgrades");

        characterNameText = GameObject.Find("TituloDetalhes")?.GetComponent<TextMeshProUGUI>();
        characterDescriptionText = GameObject.Find("TextoDescricao")?.GetComponent<TextMeshProUGUI>();
        characterLevelText = GameObject.Find("TextoNivel")?.GetComponent<TextMeshProUGUI>();
        characterElementText = GameObject.Find("TextoElemento")?.GetComponent<TextMeshProUGUI>();

        // COOLDOWN/REGEN
        healthRegenSlider = GameObject.Find("HealthRegen_Slider")?.GetComponent<Slider>();
        healthRegenValueText = GameObject.Find("HealthRegen_Value")?.GetComponent<TextMeshProUGUI>();
        attackCooldownSlider = GameObject.Find("AttackCooldown_Slider")?.GetComponent<Slider>();
        attackCooldownValueText = GameObject.Find("AttackCooldown_Value")?.GetComponent<TextMeshProUGUI>();
        defenseCooldownSlider = GameObject.Find("DefenseCooldown_Slider")?.GetComponent<Slider>();
        defenseCooldownValueText = GameObject.Find("DefenseCooldown_Value")?.GetComponent<TextMeshProUGUI>();

        ConfigurarReferenciasUpgrades();
        Debug.Log("✅ Referências configuradas automaticamente!");
    }

    public void ConfigurarReferenciasUpgrades()
    {
        if (grupoUpgrades != null)
        {
            upgradeBotoes = new Button[7];
            upgradeNiveis = new TextMeshProUGUI[7];
            upgradeCustos = new TextMeshProUGUI[7];

            string[] upgrades = { "VIDA", "ATAQUE", "DEFESA", "VELOCIDADE", "REGENERACAO", "COOLDOWN_ATAQUE", "COOLDOWN_DEFESA" };
            for (int i = 0; i < 7; i++)
            {
                upgradeBotoes[i] = GameObject.Find($"Upgrade{upgrades[i]}_Btn")?.GetComponent<Button>();
                upgradeNiveis[i] = GameObject.Find($"Upgrade{upgrades[i]}_Nivel")?.GetComponent<TextMeshProUGUI>();
                upgradeCustos[i] = GameObject.Find($"Upgrade{upgrades[i]}_Custo")?.GetComponent<TextMeshProUGUI>();
            }
        }
    }
}