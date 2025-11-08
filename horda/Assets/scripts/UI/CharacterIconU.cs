using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterIconUI : MonoBehaviour
{
    [Header("Referências UI")]
    public Image characterIcon;
    public TextMeshProUGUI characterName;
    public GameObject selectedIndicator;
    public GameObject lockedOverlay;
    public TextMeshProUGUI requiredLevelText;

    [Header("Cores")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.green;
    public Color lockedColor = Color.red;

    public CharacterData characterData; // 🆕 AGORA USA CharacterData
    public int characterIndex;
    private CharacterSelectionManagerIntegrated characterSelectionManager;
    private Button button;
    private Image backgroundImage;

    public void Initialize(CharacterData data, int index, CharacterSelectionManagerIntegrated manager)
    {
        characterData = data;
        characterIndex = index;
        characterSelectionManager = manager;

        button = GetComponent<Button>();
        backgroundImage = GetComponent<Image>();

        // CONFIGURA UI
        if (characterIcon != null && data.icon != null)
            characterIcon.sprite = data.icon;

        if (characterName != null)
            characterName.text = data.characterName;

        UpdateUnlockStatus();
        ConfigureButton();
        UpdateVisuals();
    }

    void UpdateUnlockStatus()
    {
        if (lockedOverlay != null)
            lockedOverlay.SetActive(!characterData.unlocked);

        if (requiredLevelText != null)
        {
            requiredLevelText.text = $"Nv. {characterData.unlockLevel}";
            requiredLevelText.gameObject.SetActive(!characterData.unlocked);
        }
    }

    void ConfigureButton()
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            if (characterData.unlocked)
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
        if (characterIcon != null)
        {
            characterIcon.color = characterData.unlocked ? normalColor : lockedColor;
        }
    }

    private void OnClick()
    {
        if (characterData.unlocked)
        {
            characterSelectionManager.OnCharacterIconClicked(characterIndex);
        }
    }

    public void RefreshStatus()
    {
        UpdateUnlockStatus();
        UpdateVisuals();
        ConfigureButton();
    }
}