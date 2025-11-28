using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageButtonUI : MonoBehaviour
{
    [Header("🔹 Referências UI")]
    public Button stageButton;
    public TextMeshProUGUI stageNameText;
    public TextMeshProUGUI difficultyText;
    public TextMeshProUGUI rewardText;
    public TextMeshProUGUI levelRequirementText;
    public Image stageIcon;
    public Image backgroundImage;
    public GameObject lockedPanel;
    public GameObject completedPanel;
    public GameObject selectedPanel;
    public GameObject starsPanel;
    public Image[] starIcons;

    [Header("🔹 Cores de Dificuldade")]
    public Color easyColor = new Color(0.2f, 0.8f, 0.2f);    // Verde
    public Color mediumColor = new Color(0.9f, 0.7f, 0.1f); // Amarelo
    public Color hardColor = new Color(0.8f, 0.2f, 0.2f);   // Vermelho
    public Color expertColor = new Color(0.6f, 0.1f, 0.8f); // Roxo
    public Color masterColor = new Color(0.9f, 0.3f, 0.1f); // Laranja

    [Header("🔹 Configurações Visuais")]
    public Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 0.7f);
    public Color unlockedColor = Color.white;
    public Color completedColor = new Color(0.5f, 1f, 0.5f, 1f);

    private StageData stageData;
    private int stageIndex;
    private CharacterSelectionManagerIntegrated selectionManager;

    public void Initialize(StageData data, int index, CharacterSelectionManagerIntegrated manager)
    {
        stageData = data;
        stageIndex = index;
        selectionManager = manager;

        UpdateUI();

        // Configura o botão
        if (stageButton != null)
        {
            stageButton.onClick.RemoveAllListeners();
            stageButton.onClick.AddListener(OnStageButtonClicked);
        }

        Debug.Log($"🎯 Stage Button inicializado: {data.stageName}");
    }

    private void UpdateUI()
    {
        if (stageData == null) return;

        // Atualiza textos básicos
        if (stageNameText != null)
            stageNameText.text = stageData.stageName;

        if (difficultyText != null)
        {
            difficultyText.text = GetDifficultyText(stageData.difficulty);
            difficultyText.color = GetDifficultyColor(stageData.difficulty);
        }

        if (rewardText != null)
            rewardText.text = $"+{stageData.coinReward}";

        if (levelRequirementText != null)
        {
            levelRequirementText.text = $"Nv. {stageData.recommendedLevel}";
            levelRequirementText.gameObject.SetActive(!stageData.unlocked);
        }

        // Atualiza ícone do stage
        if (stageIcon != null && stageData.stagePreview != null)
        {
            stageIcon.sprite = stageData.stagePreview;
            stageIcon.color = stageData.unlocked ? unlockedColor : lockedColor;
        }

        // Atualiza background
        if (backgroundImage != null)
        {
            backgroundImage.color = stageData.unlocked ?
                (stageData.completed ? completedColor : unlockedColor) : lockedColor;
        }

        // Atualiza status panels
        if (lockedPanel != null)
            lockedPanel.SetActive(!stageData.unlocked);

        if (completedPanel != null)
            completedPanel.SetActive(stageData.completed);

        if (selectedPanel != null)
            selectedPanel.SetActive(false);

        // Atualiza estrelas
        UpdateStarsDisplay();

        // Atualiza interatividade do botão
        if (stageButton != null)
            stageButton.interactable = stageData.unlocked;
    }

    private void UpdateStarsDisplay()
    {
        if (starsPanel != null)
            starsPanel.SetActive(stageData.completed && stageData.starsEarned > 0);

        if (starIcons != null && starIcons.Length >= 3)
        {
            for (int i = 0; i < starIcons.Length; i++)
            {
                if (starIcons[i] != null)
                {
                    starIcons[i].color = (i < stageData.starsEarned) ? Color.yellow : new Color(0.3f, 0.3f, 0.3f, 0.5f);
                }
            }
        }
    }

    public void OnStageButtonClicked()
    {
        if (stageData != null && stageData.unlocked && selectionManager != null)
        {
            selectionManager.OnStageSelected(stageIndex);
            SetSelected(true);
            Debug.Log($"🎯 Stage selecionado: {stageData.stageName}");
        }
        else if (!stageData.unlocked)
        {
            Debug.Log($"🔒 Stage bloqueado: {stageData.stageName} - Requer Nv. {stageData.requiredLevel}");
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectedPanel != null)
            selectedPanel.SetActive(selected);

        // Efeito visual adicional quando selecionado
        if (backgroundImage != null && stageData != null && stageData.unlocked)
        {
            backgroundImage.color = selected ?
                new Color(0.8f, 0.9f, 1f, 1f) :
                (stageData.completed ? completedColor : unlockedColor);
        }
    }

    public void RefreshStatus()
    {
        UpdateUI();
        Debug.Log($"🔄 Stage Button atualizado: {stageData.stageName} - Unlocked: {stageData.unlocked}");
    }

    private string GetDifficultyText(int difficulty)
    {
        switch (difficulty)
        {
            case 1: return "FÁCIL";
            case 2: return "NORMAL";
            case 3: return "DIFÍCIL";
            case 4: return "EXPERT";
            case 5: return "MESTRE";
            default: return $"Nv. {difficulty}";
        }
    }

    private Color GetDifficultyColor(int difficulty)
    {
        switch (difficulty)
        {
            case 1: return easyColor;
            case 2: return mediumColor;
            case 3: return hardColor;
            case 4: return expertColor;
            case 5: return masterColor;
            default: return Color.white;
        }
    }

    // Método para forçar atualização visual
    public void ForceUpdateVisuals()
    {
        UpdateUI();
    }

    // Método para simular conclusão do stage (para teste)
    public void SimulateCompletion(int stars = 3)
    {
        if (stageData != null)
        {
            stageData.CompleteStage(120f, stars);
            RefreshStatus();
        }
    }

    // Método para desbloquear stage (para teste)
    public void UnlockStage()
    {
        if (stageData != null)
        {
            stageData.unlocked = true;
            RefreshStatus();
        }
    }
}