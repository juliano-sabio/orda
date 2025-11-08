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
    public StageData[] stages;
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
        InitializeSystems();
    }

    // ✅ MÉTODO INICIALIZADOR CORRIGIDO
    private void InitializeSystems()
    {
        Debug.Log("🔧 Inicializando sistemas...");

        // ✅ INICIALIZA ARRAYS SE ESTIVEREM NULL
        InitializeArrays();

        LoadPlayerData();
        InitializeCharacterSystem();
        InitializeStageSystem();
        UpdateUI();
        UpdateUpgradesUI();
        UpdateCooldownAndRegenUI();

        Debug.Log("✅ Sistemas inicializados com sucesso!");
    }

    // ✅ NOVO MÉTODO PARA INICIALIZAR ARRAYS
    private void InitializeArrays()
    {
        // Inicializa arrays que podem estar null
        if (characters == null)
        {
            characters = new CharacterData[0];
            Debug.Log("⚠️ Array 'characters' inicializado como vazio");
        }

        if (characterIcons == null)
        {
            characterIcons = new CharacterIconUI[0];
            Debug.Log("⚠️ Array 'characterIcons' inicializado como vazio");
        }

        if (stages == null)
        {
            stages = new StageData[0];
            Debug.Log("⚠️ Array 'stages' inicializado como vazio");
        }

        if (stageButtons == null)
        {
            stageButtons = new StageButtonUI[0];
            Debug.Log("⚠️ Array 'stageButtons' inicializado como vazio");
        }

        if (upgradeBotoes == null)
        {
            upgradeBotoes = new Button[7];
            Debug.Log("⚠️ Array 'upgradeBotoes' inicializado");
        }

        if (upgradeNiveis == null)
        {
            upgradeNiveis = new TextMeshProUGUI[7];
            Debug.Log("⚠️ Array 'upgradeNiveis' inicializado");
        }

        if (upgradeCustos == null)
        {
            upgradeCustos = new TextMeshProUGUI[7];
            Debug.Log("⚠️ Array 'upgradeCustos' inicializado");
        }

        if (statusSliders == null)
        {
            statusSliders = new Slider[0];
            Debug.Log("⚠️ Array 'statusSliders' inicializado como vazio");
        }

        if (statusValues == null)
        {
            statusValues = new TextMeshProUGUI[0];
            Debug.Log("⚠️ Array 'statusValues' inicializado como vazio");
        }

        // Inicializa upgradeLevels
        if (upgradeLevels == null || upgradeLevels.Length == 0)
        {
            upgradeLevels = new int[7];
            for (int i = 0; i < upgradeLevels.Length; i++)
            {
                upgradeLevels[i] = 1;
            }
        }
    }

    // ✅ SISTEMA DE STAGES CORRIGIDO
    private void InitializeStageSystem()
    {
        if (stageButtons == null || stages == null)
        {
            Debug.Log("⚠️ Stage system não configurado - arrays null");
            return;
        }

        int minLength = Mathf.Min(stageButtons.Length, stages.Length);

        for (int i = 0; i < minLength; i++)
        {
            if (stageButtons[i] != null && stages[i] != null)
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

    public void OnStageSelected(int stageIndex)
    {
        if (stageIndex < 0 || stages == null || stageIndex >= stages.Length)
        {
            Debug.Log("⚠️ Stage selection inválido");
            return;
        }

        selectedStageIndex = stageIndex;
        StageData selectedStage = stages[stageIndex];

        if (stageNameText != null)
            stageNameText.text = selectedStage.stageName;

        if (stageDifficultyText != null)
            stageDifficultyText.text = $"Dificuldade: {selectedStage.difficulty}/5";

        // Atualiza botões de stage
        for (int i = 0; i < stageButtons.Length; i++)
        {
            if (stageButtons[i] != null)
            {
                stageButtons[i].SetSelected(i == stageIndex);
            }
        }

        Debug.Log($"🎯 Stage selecionado: {selectedStage.stageName}");
    }

    // ✅ MÉTODOS DE DADOS DO JOGADOR CORRIGIDOS
    private void LoadPlayerData()
    {
        Debug.Log("💾 Carregando dados do jogador...");

        playerCoins = PlayerPrefs.GetInt("PlayerCoins", 1000);
        playerLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
        unlockedCharacters = PlayerPrefs.GetInt("UnlockedCharacters", 1);

        // ✅ CARREGA UPGRADES COM VERIFICAÇÃO
        for (int i = 0; i < upgradeLevels.Length; i++)
        {
            upgradeLevels[i] = PlayerPrefs.GetInt($"UpgradeLevel_{i}", 1);
        }

        // ✅ CARREGA DESBLOQUEIO DE PERSONAGENS COM VERIFICAÇÃO
        if (characters != null)
        {
            for (int i = 0; i < characters.Length; i++)
            {
                if (characters[i] != null)
                {
                    characters[i].unlocked = (playerLevel >= characters[i].unlockLevel) || (i < unlockedCharacters);
                }
            }
        }

        // ✅ CARREGA STAGES DESBLOQUEADOS COM VERIFICAÇÃO
        if (stages != null)
        {
            for (int i = 0; i < stages.Length; i++)
            {
                if (stages[i] != null)
                {
                    if (i == 0) // Primeiro stage sempre desbloqueado
                        stages[i].unlocked = true;
                    else
                        stages[i].unlocked = PlayerPrefs.GetInt($"StageUnlocked_{i}", 0) == 1;
                }
            }
        }

        Debug.Log("✅ Dados do jogador carregados!");
    }

    public void SavePlayerData()
    {
        PlayerPrefs.SetInt("PlayerCoins", playerCoins);
        PlayerPrefs.SetInt("PlayerLevel", playerLevel);
        PlayerPrefs.SetInt("UnlockedCharacters", unlockedCharacters);

        // ✅ SALVA UPGRADES
        for (int i = 0; i < upgradeLevels.Length; i++)
        {
            PlayerPrefs.SetInt($"UpgradeLevel_{i}", upgradeLevels[i]);
        }

        // ✅ SALVA STAGES DESBLOQUEADOS
        if (stages != null)
        {
            for (int i = 0; i < stages.Length; i++)
            {
                if (stages[i] != null)
                {
                    PlayerPrefs.SetInt($"StageUnlocked_{i}", stages[i].unlocked ? 1 : 0);
                }
            }
        }

        PlayerPrefs.Save();
        Debug.Log("💾 Dados do jogador salvos!");
    }

    // ✅ SISTEMA DE PERSONAGENS CORRIGIDO
    private void InitializeCharacterSystem()
    {
        if (characterIcons == null || characters == null)
        {
            Debug.Log("⚠️ Character system não configurado - arrays null");
            return;
        }

        int minLength = Mathf.Min(characterIcons.Length, characters.Length);

        for (int i = 0; i < minLength; i++)
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

    // ✅ MÉTODO DE UPGRADE CORRIGIDO
    public void UpgradeStat(int statIndex)
    {
        if (statIndex < 0 || statIndex >= upgradeLevels.Length)
        {
            Debug.Log("❌ Índice de upgrade inválido");
            return;
        }

        int upgradeCost = upgradeLevels[statIndex] * 100;

        if (playerCoins < upgradeCost)
        {
            Debug.Log("💰 Moedas insuficientes!");
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

    // ✅ ATUALIZAR UI DE UPGRADES CORRIGIDO
    void UpdateUpgradesUI()
    {
        if (upgradeNiveis == null || upgradeCustos == null || upgradeBotoes == null)
        {
            Debug.Log("⚠️ Arrays de upgrade UI não configurados");
            return;
        }

        for (int i = 0; i < upgradeLevels.Length; i++)
        {
            if (i < upgradeNiveis.Length && upgradeNiveis[i] != null)
                upgradeNiveis[i].text = $"Nv.{upgradeLevels[i]}";

            if (i < upgradeCustos.Length && upgradeCustos[i] != null)
            {
                int custo = upgradeLevels[i] * 100;
                upgradeCustos[i].text = custo.ToString();
            }

            if (i < upgradeBotoes.Length && upgradeBotoes[i] != null)
            {
                int custo = upgradeLevels[i] * 100;
                upgradeBotoes[i].interactable = playerCoins >= custo;
            }
        }
    }

    // ✅ ATUALIZAR UI DE COOLDOWN E REGENERAÇÃO CORRIGIDO
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

    // ✅ APLICAR UPGRADES AO PERSONAGEM CORRIGIDO
    public void ApplyUpgradesToCharacter(PlayerStats playerStats, CharacterData characterData)
    {
        if (playerStats == null || characterData == null)
        {
            Debug.Log("❌ PlayerStats ou CharacterData null");
            return;
        }

        // Aplica bônus de upgrades
        float healthBonus = characterData.maxHealth * (upgradeLevels[0] - 1) * 0.05f;
        float attackBonus = characterData.baseAttack * (upgradeLevels[1] - 1) * 0.05f;
        float defenseBonus = characterData.baseDefense * (upgradeLevels[2] - 1) * 0.05f;
        float speedBonus = characterData.baseSpeed * (upgradeLevels[3] - 1) * 0.03f;
        float regenBonus = characterData.baseHealthRegen * (upgradeLevels[4] - 1) * 0.5f;
        float attackCooldownReduction = upgradeLevels[5] * 0.05f;
        float defenseCooldownReduction = upgradeLevels[6] * 0.05f;

        // Aplica valores base
        playerStats.maxHealth = characterData.maxHealth + healthBonus;
        playerStats.attack = characterData.baseAttack + attackBonus;
        playerStats.defense = characterData.baseDefense + defenseBonus;
        playerStats.speed = characterData.baseSpeed + speedBonus;
        playerStats.healthRegenRate = characterData.baseHealthRegen + regenBonus;

        // Aplica cooldowns com limites
        playerStats.attackActivationInterval = Mathf.Max(0.1f, characterData.baseAttackCooldown * (1f - attackCooldownReduction));
        playerStats.defenseActivationInterval = Mathf.Max(0.1f, characterData.baseDefenseCooldown * (1f - defenseCooldownReduction));

        Debug.Log($"🎯 {characterData.characterName} - Upgrades aplicados");
    }

    // ✅ MÉTODO PARA APLICAR PERSONAGEM AOS SISTEMAS
    public void ApplyCharacterToPlayerSystems(PlayerStats playerStats, SkillManager skillManager)
    {
        if (selectedCharacterIndex < 0 || selectedCharacterIndex >= characters.Length)
        {
            Debug.Log("❌ Índice de personagem inválido");
            return;
        }

        if (characters[selectedCharacterIndex] == null)
        {
            Debug.Log("❌ CharacterData null");
            return;
        }

        CharacterData selectedCharacter = characters[selectedCharacterIndex];

        ApplyCharacterBaseStats(playerStats, selectedCharacter);
        ApplyUpgradesToCharacter(playerStats, selectedCharacter);

        Debug.Log($"🎮 {selectedCharacter.characterName} aplicado ao PlayerStats!");
    }

    private void ApplyCharacterBaseStats(PlayerStats playerStats, CharacterData character)
    {
        // ✅ APLICA ESTATÍSTICAS BÁSICAS
        playerStats.health = character.maxHealth;
        playerStats.maxHealth = character.maxHealth;
        playerStats.attack = character.baseAttack;
        playerStats.defense = character.baseDefense;
        playerStats.speed = character.baseSpeed;
        playerStats.healthRegenRate = character.baseHealthRegen;
        playerStats.healthRegenDelay = character.baseRegenDelay;
        playerStats.attackActivationInterval = character.baseAttackCooldown;
        playerStats.defenseActivationInterval = character.baseDefenseCooldown;

        // ✅ CORREÇÃO: ENUM NUNCA É NULL, VERIFICAMOS SE É DIFERENTE DO VALOR PADRÃO
        // Assumindo que o valor padrão do enum é 0 (None/PrimeiroValor)
        if (character.baseElement != default(PlayerStats.Element))
        {
            playerStats.CurrentElement = character.baseElement;
            Debug.Log($"🎯 Elemento aplicado: {character.baseElement}");
        }
        else
        {
            Debug.Log("ℹ️ Usando elemento padrão do personagem");
        }
    }

    // ✅ ATUALIZAR DISPLAY DE STATUS CORRIGIDO
    public void UpdateStatusDisplay(CharacterData character)
    {
        if (character == null)
        {
            Debug.Log("❌ Character null no UpdateStatusDisplay");
            return;
        }

        // ATUALIZA TEXTO BÁSICO
        if (characterNameText != null)
            characterNameText.text = character.characterName;

        if (characterDescriptionText != null)
            characterDescriptionText.text = character.description;

        if (characterLevelText != null)
            characterLevelText.text = $"Nível {playerLevel}";

        if (characterElementText != null)
        {
            characterElementText.text = $"Elemento: {character.baseElement}";
        }

        // CALCULA BÔNUS COM UPGRADES
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

        UpdateCooldownAndRegenUI();
    }

    private void UpdateStatusSlider(int index, float value, float maxValue, string label)
    {
        if (statusSliders != null && index < statusSliders.Length && statusSliders[index] != null)
        {
            statusSliders[index].value = value / maxValue;
        }

        if (statusValues != null && index < statusValues.Length && statusValues[index] != null)
        {
            statusValues[index].text = $"{label}: {value:F1}";
        }
    }

    // ✅ MÉTODOS DE SELEÇÃO DE PERSONAGEM CORRIGIDOS
    public void OnCharacterIconClicked(int characterIndex)
    {
        SelectCharacter(characterIndex);
    }

    public void SelectCharacter(int characterIndex)
    {
        if (characterIndex < 0 || characterIndex >= characters.Length)
        {
            Debug.Log("❌ Índice de personagem inválido");
            return;
        }

        if (characters[characterIndex] == null || !characters[characterIndex].unlocked)
        {
            Debug.Log("❌ Personagem null ou bloqueado");
            return;
        }

        // DESMARCA PERSONAGEM ANTERIOR
        if (selectedCharacterIndex >= 0 && selectedCharacterIndex < characterIcons.Length)
        {
            if (characterIcons[selectedCharacterIndex] != null)
                characterIcons[selectedCharacterIndex].SetSelected(false);
        }

        // MARCA NOVO PERSONAGEM
        selectedCharacterIndex = characterIndex;

        if (characterIndex < characterIcons.Length && characterIcons[characterIndex] != null)
            characterIcons[characterIndex].SetSelected(true);

        // ATUALIZA UI
        UpdateStatusDisplay(characters[characterIndex]);
        UpdateUI();

        Debug.Log($"🎯 Personagem selecionado: {characters[characterIndex].characterName}");
    }

    // ✅ ATUALIZAR UI PRINCIPAL CORRIGIDO
    private void UpdateUI()
    {
        if (coinsText != null)
            coinsText.text = $"Moedas: {playerCoins}";

        if (selectedCharacterIndex >= 0 && selectedCharacterIndex < characters.Length && characters[selectedCharacterIndex] != null)
        {
            UpdateStatusDisplay(characters[selectedCharacterIndex]);
        }
    }

    // ✅ MÉTODOS ADICIONAIS CORRIGIDOS
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
        if (characterIndex < 0 || characterIndex >= characters.Length || characters[characterIndex] == null)
        {
            Debug.Log("❌ Índice de personagem inválido para desbloquear");
            return;
        }

        characters[characterIndex].unlocked = true;
        if (unlockedCharacters <= characterIndex)
            unlockedCharacters = characterIndex + 1;

        if (characterIndex < characterIcons.Length && characterIcons[characterIndex] != null)
            characterIcons[characterIndex].RefreshStatus();

        SavePlayerData();
        Debug.Log($"🔓 {characters[characterIndex].characterName} desbloqueado!");
    }

    public void UnlockStage(int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= stages.Length || stages[stageIndex] == null)
        {
            Debug.Log("❌ Índice de stage inválido para desbloquear");
            return;
        }

        stages[stageIndex].unlocked = true;

        if (stageIndex < stageButtons.Length && stageButtons[stageIndex] != null)
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

        if (selectedStageIndex < 0 || selectedStageIndex >= stages.Length || stages[selectedStageIndex] == null)
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

    // ✅ CONFIGURAÇÃO AUTOMÁTICA DE REFERÊNCIAS
    public void ConfigurarReferenciasAutomaticamente()
    {
        Debug.Log("🔧 Configurando referências automaticamente...");

        coinsText = GameObject.Find("TextoMoedas")?.GetComponent<TextMeshProUGUI>();
        grupoUpgrades = GameObject.Find("GrupoUpgrades");

        characterNameText = GameObject.Find("NomePersonagemSelecionado")?.GetComponent<TextMeshProUGUI>();
        characterDescriptionText = GameObject.Find("TextoDescricao")?.GetComponent<TextMeshProUGUI>();
        characterLevelText = GameObject.Find("NivelPersonagem")?.GetComponent<TextMeshProUGUI>();
        characterElementText = GameObject.Find("ElementoPersonagem")?.GetComponent<TextMeshProUGUI>();

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