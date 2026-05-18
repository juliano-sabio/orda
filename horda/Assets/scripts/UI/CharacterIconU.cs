using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CharacterIconUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Referências UI Base")]
    public Image characterIcon;
    public TextMeshProUGUI characterName;
    public GameObject selectedIndicator;
    public GameObject lockedOverlay;
    public TextMeshProUGUI requiredLevelText;

    [Header("Referências Elementais (Novo)")]
    public TextMeshProUGUI elementIconText;
    public Image elementBackground;

    [Header("Cores do Sistema")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.green;
    public Color hoverColor = new Color(0.85f, 0.85f, 1f, 1f);
    public Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    [Header("Dados")]
    public CharacterData characterData;
    public int characterIndex;

    private CharacterSelectionManagerIntegrated characterSelectionManager;
    private Button button;
    private Image backgroundImage;
    private bool isSelected = false;

    public void Initialize(CharacterData data, int index, CharacterSelectionManagerIntegrated manager)
    {
        characterData = data;
        characterIndex = index;
        characterSelectionManager = manager;

        button = GetComponent<Button>();
        backgroundImage = GetComponent<Image>();

        // 1. Configura Nome e Ícone
        if (characterName != null) characterName.text = data.characterName;
        if (characterIcon != null && data.icon != null) characterIcon.sprite = data.icon;

        // 2. Configura Elemento (Usando seus novos métodos do CharacterData)
        if (elementIconText != null)
        {
            elementIconText.text = data.GetElementIcon();
            elementIconText.color = data.GetElementColor();
        }

        if (elementBackground != null)
        {
            elementBackground.color = data.GetElementColor();
        }

        // 3. Atualiza Status de Bloqueio e Botão
        UpdateUnlockStatus();
        ConfigureButton();
        UpdateVisuals();
    }

    void UpdateUnlockStatus()
    {
        bool isUnlocked = characterData.unlocked;

        if (lockedOverlay != null)
            lockedOverlay.SetActive(!isUnlocked);

        if (requiredLevelText != null)
        {
            requiredLevelText.text = $"Nv. {characterData.unlockLevel}";
            requiredLevelText.gameObject.SetActive(!isUnlocked);
        }
    }

    void ConfigureButton()
    {
        if (button != null)
        {
            button.transition = Selectable.Transition.None;
            button.onClick.RemoveAllListeners();

            if (characterData.unlocked)
            {
                button.interactable = true;
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
        isSelected = selected;

        if (selectedIndicator != null)
            selectedIndicator.SetActive(selected);

        if (backgroundImage != null)
            backgroundImage.color = selected ? selectedColor : normalColor;

        transform.localScale = selected ? Vector3.one * 1.1f : Vector3.one;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (characterData == null || !characterData.unlocked || isSelected) return;

        if (backgroundImage != null)
            backgroundImage.color = hoverColor;

        transform.localScale = Vector3.one * 1.05f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (characterData == null || !characterData.unlocked || isSelected) return;

        if (backgroundImage != null)
            backgroundImage.color = normalColor;

        transform.localScale = Vector3.one;
    }

    void UpdateVisuals()
    {
        if (characterIcon != null)
        {
            // Se estiver bloqueado, o ícone fica escurecido (lockedColor)
            characterIcon.color = characterData.unlocked ? Color.white : lockedColor;
        }
    }

    private void OnClick()
    {
        // Só dispara o evento se estiver desbloqueado (segurança extra)
        if (characterData.unlocked && characterSelectionManager != null)
        {
            characterSelectionManager.OnCharacterIconClicked(characterIndex);
        }
    }

    // Chamado pelo Manager se algo mudar globalmente (ex: jogador subiu de nível)
    public void RefreshStatus()
    {
        UpdateUnlockStatus();
        ConfigureButton();
        UpdateVisuals();
    }
}