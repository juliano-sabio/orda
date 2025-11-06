using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CharacterSelectionManager : MonoBehaviour
{
    [Header("Configurações dos Personagens")]
    public List<CharacterData> characters;
    public int selectedCharacterIndex = 0;

    [Header("Referências UI - Sistema Completo")]
    public Image characterIcon;
    public Text characterName;
    public Text characterDescription;
    public GameObject selectionIndicator;
    public Button selectButton;
    public Button confirmButton;
    public Button startButton;

    [Header("Display de Status")]
    public Slider healthSlider;
    public Slider attackSlider;
    public Slider defenseSlider;
    public Slider speedSlider;
    public Text healthText;
    public Text attackText;
    public Text defenseText;
    public Text speedText;

    [Header("⚡ Sistema de Elementos")]
    public Image elementIcon;
    public Text elementText;
    public GameObject elementAdvantagePanel;
    public Text advantageText;
    public Text disadvantageText;
    public GameObject elementalBonusPanel;
    public Text elementalBonusText;

    [Header("🚀 Ultimate Display")]
    public Image ultimateIcon;
    public Text ultimateName;
    public Text ultimateDescription;
    public Text ultimateCooldown;
    public Text ultimateEffect;
    public Text ultimateType;

    [Header("🎯 Skills Iniciais")]
    public Transform skillsContainer;
    public GameObject skillSlotPrefab;
    public Text skillsCountText;

    [Header("🔧 Modificadores")]
    public Transform modifiersContainer;
    public GameObject modifierSlotPrefab;
    public Text modifiersCountText;

    [Header("💎 Informações de Progressão")]
    public Text unlockLevelText;
    public Text xpMultiplierText;

    private Dictionary<PlayerStats.Element, Color> elementColors;

    void Awake()
    {
        InitializeElementColors();
        Debug.Log("✅ CharacterSelectionManager inicializado para esta cena");
    }

    void Start()
    {
        InitializeSelection();
        UpdateUI();

        // Configura botões
        if (selectButton != null)
            selectButton.onClick.AddListener(SelectCurrentCharacter);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmSelection);

        if (startButton != null)
            startButton.onClick.AddListener(StartGame);
    }

    void InitializeElementColors()
    {
        elementColors = new Dictionary<PlayerStats.Element, Color>()
        {
            { PlayerStats.Element.None, Color.white },
            { PlayerStats.Element.Fire, new Color(1f, 0.3f, 0.1f) },
            { PlayerStats.Element.Ice, new Color(0.1f, 0.5f, 1f) },
            { PlayerStats.Element.Lightning, new Color(0.8f, 0.8f, 0.1f) },
            { PlayerStats.Element.Poison, new Color(0.5f, 0.1f, 0.8f) },
            { PlayerStats.Element.Earth, new Color(0.6f, 0.4f, 0.2f) },
            { PlayerStats.Element.Wind, new Color(0.4f, 0.8f, 0.9f) }
        };
    }

    void InitializeSelection()
    {
        selectedCharacterIndex = PlayerPrefs.GetInt("SelectedCharacter", 0);
        CheckCharacterUnlocks();
    }

    void CheckCharacterUnlocks()
    {
        int playerLevel = PlayerPrefs.GetInt("PlayerLevel", 1);

        foreach (var character in characters)
        {
            character.unlocked = playerLevel >= character.unlockLevel;
        }
    }

    public void NextCharacter()
    {
        selectedCharacterIndex++;
        if (selectedCharacterIndex >= characters.Count)
            selectedCharacterIndex = 0;

        UpdateUI();
    }

    public void PreviousCharacter()
    {
        selectedCharacterIndex--;
        if (selectedCharacterIndex < 0)
            selectedCharacterIndex = characters.Count - 1;

        UpdateUI();
    }

    void UpdateUI()
    {
        if (characters.Count == 0) return;

        CharacterData currentCharacter = characters[selectedCharacterIndex];

        // Informações básicas
        characterIcon.sprite = currentCharacter.icon;
        characterName.text = currentCharacter.characterName;
        characterDescription.text = currentCharacter.description;

        // Status base
        UpdateStatusDisplay(currentCharacter);

        // Sistema de elemento
        UpdateElementDisplay(currentCharacter);

        // Ultimate específica
        UpdateUltimateDisplay(currentCharacter);

        // Skills e modificadores
        UpdateSkillsDisplay(currentCharacter);
        UpdateModifiersDisplay(currentCharacter);

        // Informações de progressão
        UpdateProgressionDisplay(currentCharacter);

        // Estado dos botões
        UpdateButtonsState(currentCharacter);
    }

    void UpdateStatusDisplay(CharacterData character)
    {
        healthSlider.value = character.maxHealth / 200f;
        attackSlider.value = character.baseAttack / 30f;
        defenseSlider.value = character.baseDefense / 15f;
        speedSlider.value = character.baseSpeed / 12f;

        healthText.text = character.maxHealth.ToString();
        attackText.text = character.baseAttack.ToString();
        defenseText.text = character.baseDefense.ToString();
        speedText.text = character.baseSpeed.ToString();
    }

    void UpdateElementDisplay(CharacterData character)
    {
        elementIcon.color = elementColors[character.baseElement];
        elementText.text = character.baseElement.ToString();
        elementText.color = elementColors[character.baseElement];

        advantageText.text = "Fortes contra: " + string.Join(", ", character.strongAgainst);
        disadvantageText.text = "Fracos contra: " + string.Join(", ", character.weakAgainst);

        bool hasElement = character.baseElement != PlayerStats.Element.None;
        elementAdvantagePanel.SetActive(hasElement);
        elementalBonusPanel.SetActive(hasElement);

        if (hasElement)
        {
            elementalBonusText.text = GetElementalBonusDescription(character.baseElement);
        }
    }

    string GetElementalBonusDescription(PlayerStats.Element element)
    {
        switch (element)
        {
            case PlayerStats.Element.Fire: return "🔥 +5 Ataque | Dano em Área";
            case PlayerStats.Element.Ice: return "❄️ +3 Defesa | Lentidão em Inimigos";
            case PlayerStats.Element.Lightning: return "⚡ +20% Velocidade de Ataque";
            case PlayerStats.Element.Poison: return "☠️ Dano Contínuo | Redução de Cura";
            case PlayerStats.Element.Earth: return "🌍 +5 Defesa | Resistência a Knockback";
            case PlayerStats.Element.Wind: return "💨 +2 Velocidade | Esquiva Aumentada";
            default: return "Sem bônus elemental";
        }
    }

    void UpdateUltimateDisplay(CharacterData character)
    {
        if (character.ultimateSkill != null)
        {
            ultimateIcon.sprite = character.ultimateSkill.ultimateIcon;
            ultimateName.text = character.ultimateSkill.ultimateName;
            ultimateDescription.text = character.ultimateSkill.description;
            ultimateCooldown.text = $"Recarga: {character.ultimateSkill.cooldown}s";
            ultimateEffect.text = character.ultimateSkill.specialEffect;
            ultimateType.text = $"Tipo: {character.ultimateSkill.ultimateType}";

            ultimateIcon.color = elementColors[character.ultimateSkill.element];
        }
        else
        {
            ultimateName.text = "Nenhuma Ultimate";
            ultimateDescription.text = "Este personagem não possui uma ultimate específica";
            ultimateCooldown.text = "";
            ultimateEffect.text = "";
            ultimateType.text = "";
        }
    }

    void UpdateSkillsDisplay(CharacterData character)
    {
        if (skillsContainer == null)
        {
            Debug.LogError("❌ skillsContainer não atribuído!");
            return;
        }

        // Limpa container
        foreach (Transform child in skillsContainer)
            Destroy(child.gameObject);

        // Cria slots para cada skill
        foreach (var skill in character.startingSkills)
        {
            if (skillSlotPrefab != null)
            {
                GameObject slot = Instantiate(skillSlotPrefab, skillsContainer);
                SkillSelectionSlot slotUI = slot.GetComponent<SkillSelectionSlot>();
                if (slotUI != null)
                    slotUI.SetSkill(skill);
            }
        }

        if (skillsCountText != null)
            skillsCountText.text = $"Skills: {character.startingSkills.Count}";
    }

    void UpdateModifiersDisplay(CharacterData character)
    {
        if (modifiersContainer == null)
        {
            Debug.LogError("❌ modifiersContainer não atribuído!");
            return;
        }

        // Limpa container
        foreach (Transform child in modifiersContainer)
            Destroy(child.gameObject);

        // Cria slots para cada modificador
        foreach (var modifier in character.startingModifiers)
        {
            if (modifierSlotPrefab != null)
            {
                GameObject slot = Instantiate(modifierSlotPrefab, modifiersContainer);
                ModifierSelectionSlot slotUI = slot.GetComponent<ModifierSelectionSlot>();
                if (slotUI != null)
                    slotUI.SetModifier(modifier);
            }
        }

        if (modifiersCountText != null)
            modifiersCountText.text = $"Modificadores: {character.startingModifiers.Count}";
    }

    void UpdateProgressionDisplay(CharacterData character)
    {
        if (unlockLevelText != null)
            unlockLevelText.text = $"Desbloqueio: Nv. {character.unlockLevel}";

        if (xpMultiplierText != null)
            xpMultiplierText.text = $"Multiplicador de XP: {character.xpMultiplier}x";
    }

    void UpdateButtonsState(CharacterData character)
    {
        if (selectButton != null)
        {
            selectButton.interactable = character.unlocked;
            Text selectButtonText = selectButton.GetComponentInChildren<Text>();
            if (selectButtonText != null)
            {
                selectButtonText.text = character.unlocked ? "Selecionar" : $"Nv. {character.unlockLevel}";
            }
        }

        int savedSelection = PlayerPrefs.GetInt("SelectedCharacter", -1);
        if (confirmButton != null)
            confirmButton.interactable = (savedSelection != -1 && characters[savedSelection].unlocked);
    }

    void SelectCurrentCharacter()
    {
        if (characters[selectedCharacterIndex].unlocked)
        {
            PlayerPrefs.SetInt("SelectedCharacter", selectedCharacterIndex);
            PlayerPrefs.Save();

            if (selectionIndicator != null)
                selectionIndicator.SetActive(true);

            if (confirmButton != null)
                confirmButton.interactable = true;

            Debug.Log($"Personagem selecionado: {characters[selectedCharacterIndex].characterName}");
        }
    }

    void ConfirmSelection()
    {
        int savedSelection = PlayerPrefs.GetInt("SelectedCharacter", -1);
        if (savedSelection != -1 && characters[savedSelection].unlocked)
        {
            InitializePlayerWithSelectedCharacter();
            if (startButton != null) startButton.gameObject.SetActive(true);
            if (confirmButton != null) confirmButton.gameObject.SetActive(false);
        }
    }

    void StartGame()
    {
        Debug.Log("🚀 Iniciando jogo com personagem selecionado...");

        // Salva a seleção final
        PlayerPrefs.SetInt("SelectedCharacter", selectedCharacterIndex);
        PlayerPrefs.Save();

        // Carrega a cena de gameplay
        SceneManager.LoadScene("Gameplay");
    }

    void InitializePlayerWithSelectedCharacter()
    {
        CharacterData selectedChar = GetSelectedCharacter();

        // Salva dados para o PlayerStats
        PlayerPrefs.SetFloat("SelectedHealth", selectedChar.maxHealth);
        PlayerPrefs.SetFloat("SelectedAttack", selectedChar.baseAttack);
        PlayerPrefs.SetFloat("SelectedDefense", selectedChar.baseDefense);
        PlayerPrefs.SetFloat("SelectedSpeed", selectedChar.baseSpeed);
        PlayerPrefs.SetString("SelectedElement", selectedChar.baseElement.ToString());

        // Salva informações de skills
        SaveCharacterSkillsData(selectedChar);

        PlayerPrefs.Save();
    }

    void SaveCharacterSkillsData(CharacterData character)
    {
        // Salva informações da ultimate
        if (character.ultimateSkill != null)
        {
            PlayerPrefs.SetString("SelectedUltimate", JsonUtility.ToJson(character.ultimateSkill));
        }

        // Salva contagem de skills
        PlayerPrefs.SetInt("StartingSkillsCount", character.startingSkills.Count);
        PlayerPrefs.SetInt("StartingModifiersCount", character.startingModifiers.Count);
    }

    // 🎯 MÉTODO PRINCIPAL DE INTEGRAÇÃO
    public void ApplyCharacterToPlayerSystems(PlayerStats playerStats, SkillManager skillManager)
    {
        CharacterData characterData = GetSelectedCharacter();

        if (playerStats != null && characterData != null)
        {
            // 1. Aplica status base ao PlayerStats
            ApplyBaseStats(playerStats, characterData);

            // 2. Aplica elemento
            playerStats.ChangeElement(characterData.baseElement);

            // 3. Configura ultimate específica
            ApplyUltimateToPlayer(playerStats, characterData);

            // 4. Aplica skills iniciais via SkillManager
            if (skillManager != null)
                ApplyStartingSkills(skillManager, characterData);

            // 5. Aplica modificadores iniciais
            if (skillManager != null)
                ApplyStartingModifiers(skillManager, characterData);

            // 6. Configura comportamentos especiais
            ApplySpecialBehaviors(playerStats, characterData);

            Debug.Log($"✅ Personagem {characterData.characterName} aplicado aos sistemas do jogo!");
        }
    }

    void ApplyBaseStats(PlayerStats playerStats, CharacterData characterData)
    {
        playerStats.maxHealth = characterData.maxHealth;
        playerStats.health = characterData.maxHealth;
        playerStats.attack = characterData.baseAttack;
        playerStats.defense = characterData.baseDefense;
        playerStats.speed = characterData.baseSpeed;
    }

    void ApplyUltimateToPlayer(PlayerStats playerStats, CharacterData characterData)
    {
        if (characterData.ultimateSkill != null)
        {
            playerStats.ultimateSkill.skillName = characterData.ultimateSkill.ultimateName;
            playerStats.ultimateSkill.baseDamage = characterData.ultimateSkill.baseDamage;
            playerStats.ultimateSkill.areaOfEffect = characterData.ultimateSkill.areaOfEffect;
            playerStats.ultimateSkill.duration = characterData.ultimateSkill.duration;
            playerStats.ultimateSkill.element = characterData.ultimateSkill.element;
            playerStats.ultimateSkill.isActive = true;
            playerStats.ultimateCooldown = characterData.ultimateSkill.cooldown;
        }
    }

    void ApplyStartingSkills(SkillManager skillManager, CharacterData characterData)
    {
        // Adiciona skills iniciais do personagem
        foreach (var skill in characterData.startingSkills)
        {
            skillManager.AddSkill(skill);
        }
    }

    void ApplyStartingModifiers(SkillManager skillManager, CharacterData characterData)
    {
        foreach (var modifier in characterData.startingModifiers)
        {
            skillManager.AddSkillModifier(modifier);
        }
    }

    void ApplySpecialBehaviors(PlayerStats playerStats, CharacterData characterData)
    {
        // Adiciona componentes de comportamento especial se existirem
        if (characterData.specialSkillBehavior != null)
        {
            // Remove comportamentos existentes do mesmo tipo
            var existingBehavior = playerStats.GetComponent(characterData.specialSkillBehavior.GetType());
            if (existingBehavior != null)
                Destroy(existingBehavior);

            SkillBehavior behavior = playerStats.gameObject.AddComponent(characterData.specialSkillBehavior.GetType()) as SkillBehavior;
            if (behavior != null)
                behavior.Initialize(playerStats);
        }

        if (characterData.ultimateBehavior != null && characterData.ultimateSkill != null)
        {
            // Remove comportamentos existentes do mesmo tipo
            var existingUltimate = playerStats.GetComponent(characterData.ultimateBehavior.GetType());
            if (existingUltimate != null)
                Destroy(existingUltimate);

            UltimateBehavior ultimate = playerStats.gameObject.AddComponent(characterData.ultimateBehavior.GetType()) as UltimateBehavior;
            if (ultimate != null)
                ultimate.Initialize(playerStats, characterData.ultimateSkill.areaOfEffect, characterData.ultimateSkill.duration);
        }
    }

    public CharacterData GetSelectedCharacter()
    {
        int savedIndex = PlayerPrefs.GetInt("SelectedCharacter", 0);
        if (savedIndex < characters.Count && savedIndex >= 0)
            return characters[savedIndex];
        return characters[0];
    }
}