using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageButtonUI : MonoBehaviour
{
    [Header("Referências UI")]
    public TextMeshProUGUI stageName;
    public TextMeshProUGUI stageDescription;
    public Image stageImage;
    public GameObject selectedIndicator;
    public GameObject lockedOverlay;
    public TextMeshProUGUI requiredLevelText;

    [Header("Cores")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.green;
    public Color lockedColor = Color.red;

    public StageData stageData; // 🆕 AGORA USA StageData
    public int stageIndex;
    private CharacterSelectionManagerIntegrated characterSelectionManager;
    private Button button;
    private Image backgroundImage;

    public void Initialize(StageData data, int index, CharacterSelectionManagerIntegrated manager)
    {
        stageData = data;
        stageIndex = index;
        characterSelectionManager = manager;

        button = GetComponent<Button>();
        backgroundImage = GetComponent<Image>();

        // CONFIGURA UI
        if (stageName != null) stageName.text = data.stageName;
        if (stageDescription != null) stageDescription.text = data.description;
        if (stageImage != null && data.stageImage != null)
        {
            stageImage.sprite = data.stageImage;
            stageImage.preserveAspect = true;
        }

        UpdateUnlockStatus();
        ConfigureButton();
        UpdateVisuals();
    }

    void UpdateUnlockStatus()
    {
        if (lockedOverlay != null)
            lockedOverlay.SetActive(!stageData.unlocked);

        if (requiredLevelText != null)
        {
            requiredLevelText.text = $"Nv. {stageData.requiredLevel}";
            requiredLevelText.gameObject.SetActive(!stageData.unlocked);
        }
    }

    void ConfigureButton()
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            if (stageData.unlocked)
            {
                button.onClick.AddListener(OnClick);
            }
            else
            {
                button.interactable = false;
            }
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectedIndicator != null)
            selectedIndicator.SetActive(selected);

        if (backgroundImage != null)
        {
            backgroundImage.color = selected ? selectedColor : normalColor;
        }

        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localScale = selected ? Vector3.one * 1.05f : Vector3.one;
        }
    }

    void UpdateVisuals()
    {
        if (stageImage != null)
        {
            stageImage.color = stageData.unlocked ? normalColor : lockedColor;
        }
    }

    private void OnClick()
    {
        if (stageData.unlocked)
        {
            characterSelectionManager.OnStageSelected(stageIndex);
        }
    }

    public void RefreshStatus()
    {
        UpdateUnlockStatus();
        UpdateVisuals();
        ConfigureButton();
    }
}