using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillChoiceUI : MonoBehaviour
{
    [Header("Referências UI")]
    public GameObject choicePanel;
    public Text titleText;
    public Transform skillsContainer;
    public GameObject skillChoicePrefab;
    public Button confirmButton;

    [Header("Configurações")]
    public float autoCloseDelay = 2f;

    private List<SkillData> currentChoices;
    private System.Action<SkillData> onSkillChosen;
    private List<GameObject> currentButtons = new List<GameObject>();

    void Start()
    {
        // Registra para receber eventos de escolha de skill
        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.OnSkillChoiceRequired += ShowSkillChoice;
        }
        else
        {
            Debug.LogError("SkillManager não encontrado!");
        }

        // Esconde o painel inicialmente
        if (choicePanel != null)
            choicePanel.SetActive(false);
        else
            Debug.LogError("ChoicePanel não atribuído!");
    }

    public void ShowSkillChoice(List<SkillData> skills, System.Action<SkillData> callback)
    {
        if (skills == null || skills.Count == 0)
        {
            Debug.LogError("Lista de skills vazia!");
            return;
        }

        currentChoices = skills;
        onSkillChosen = callback;

        // Limpa container anterior
        ClearSkillButtons();

        // Cria botões para cada skill
        for (int i = 0; i < skills.Count; i++)
        {
            CreateSkillChoiceButton(skills[i], i);
        }

        // Atualiza título
        if (titleText != null)
        {
            PlayerStats playerStats = FindAnyObjectByType<PlayerStats>();
            int currentLevel = playerStats != null ? playerStats.level : 1;
            titleText.text = $"🎯 ESCOLHA UMA SKILL (Nível {currentLevel})";
        }

        // Mostra o painel
        if (choicePanel != null)
        {
            choicePanel.SetActive(true);
            Debug.Log($"📋 Mostrando escolha de {skills.Count} skills");
        }
    }

    private void CreateSkillChoiceButton(SkillData skill, int index)
    {
        if (skillChoicePrefab == null)
        {
            Debug.LogError("SkillChoicePrefab não atribuído!");
            return;
        }

        GameObject buttonObj = Instantiate(skillChoicePrefab, skillsContainer);
        currentButtons.Add(buttonObj);

        Button button = buttonObj.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("Botão não encontrado no prefab!");
            return;
        }

        // Configura o texto do botão
        Text buttonText = buttonObj.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.text = $"{skill.skillName}\n" +
                             $"{GetElementIcon(skill.element)} Elemento: {skill.element}\n" +
                             $"{skill.GetFullDescription()}";
        }

        // Configura cor baseada no elemento
        Image buttonImage = buttonObj.GetComponent<Image>();
        if (buttonImage != null)
        {
            Color elementColor = GetElementColor(skill.element);
            buttonImage.color = new Color(elementColor.r * 0.3f, elementColor.g * 0.3f, elementColor.b * 0.3f, 1f);
        }

        // Configura o clique
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnSkillSelected(skill));

        // Posiciona o botão
        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0, -index * 120);

        // Ativa o botão
        buttonObj.SetActive(true);
    }

    private void OnSkillSelected(SkillData selectedSkill)
    {
        Debug.Log($"✅ Skill selecionada: {selectedSkill.skillName}");

        // Efeito visual de confirmação
        StartCoroutine(SelectionConfirmationEffect(selectedSkill));
    }

    private System.Collections.IEnumerator SelectionConfirmationEffect(SkillData selectedSkill)
    {
        // Pequeno delay para feedback visual
        yield return new WaitForSeconds(0.3f);

        // Executa o callback
        onSkillChosen?.Invoke(selectedSkill);

        // Fecha o painel após um delay
        yield return new WaitForSeconds(autoCloseDelay);

        ClosePanel();
    }

    private void ClearSkillButtons()
    {
        foreach (GameObject button in currentButtons)
        {
            if (button != null)
                Destroy(button);
        }
        currentButtons.Clear();

        // Limpa children do container também (backup)
        if (skillsContainer != null)
        {
            foreach (Transform child in skillsContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }

    public void ClosePanel()
    {
        if (choicePanel != null)
            choicePanel.SetActive(false);

        ClearSkillButtons();

        // Limpa referências
        currentChoices = null;
        onSkillChosen = null;

        Debug.Log("🔒 Painel de escolha de skill fechado");
    }

    // Métodos auxiliares para elementos
    private string GetElementIcon(PlayerStats.Element element)
    {
        switch (element)
        {
            case PlayerStats.Element.None: return "⚪";
            case PlayerStats.Element.Fire: return "🔥";
            case PlayerStats.Element.Ice: return "❄️";
            case PlayerStats.Element.Lightning: return "⚡";
            case PlayerStats.Element.Poison: return "☠️";
            case PlayerStats.Element.Earth: return "🌍";
            case PlayerStats.Element.Wind: return "💨";
            default: return "⚪";
        }
    }

    private Color GetElementColor(PlayerStats.Element element)
    {
        switch (element)
        {
            case PlayerStats.Element.None: return Color.white;
            case PlayerStats.Element.Fire: return new Color(1f, 0.3f, 0.1f);
            case PlayerStats.Element.Ice: return new Color(0.1f, 0.5f, 1f);
            case PlayerStats.Element.Lightning: return new Color(0.8f, 0.8f, 0.1f);
            case PlayerStats.Element.Poison: return new Color(0.5f, 0.1f, 0.8f);
            case PlayerStats.Element.Earth: return new Color(0.6f, 0.4f, 0.2f);
            case PlayerStats.Element.Wind: return new Color(0.4f, 0.8f, 0.9f);
            default: return Color.white;
        }
    }

    void OnDestroy()
    {
        // Desregistra do evento
        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.OnSkillChoiceRequired -= ShowSkillChoice;
        }
    }

    // Método para teste manual
    [ContextMenu("Testar Skill Choice UI")]
    public void TestSkillChoiceUI()
    {
        // Cria skills de teste
        List<SkillData> testSkills = new List<SkillData>();

        // Skill de teste 1
        SkillData testSkill1 = ScriptableObject.CreateInstance<SkillData>();
        testSkill1.skillName = "🔥 Fire Ball";
        testSkill1.description = "Uma bola de fogo que causa dano em área";
        testSkill1.element = PlayerStats.Element.Fire;
        testSkill1.attackBonus = 15f;
        testSkills.Add(testSkill1);

        // Skill de teste 2
        SkillData testSkill2 = ScriptableObject.CreateInstance<SkillData>();
        testSkill2.skillName = "❄️ Ice Spear";
        testSkill2.description = "Lança de gelo que causa lentidão";
        testSkill2.element = PlayerStats.Element.Ice;
        testSkill2.attackBonus = 12f;
        testSkills.Add(testSkill2);

        // Skill de teste 3
        SkillData testSkill3 = ScriptableObject.CreateInstance<SkillData>();
        testSkill3.skillName = "⚡ Lightning Strike";
        testSkill3.description = "Golpe elétrico com chance de atordoar";
        testSkill3.element = PlayerStats.Element.Lightning;
        testSkill3.attackBonus = 14f;
        testSkills.Add(testSkill3);

        // Mostra a UI de teste
        ShowSkillChoice(testSkills, (selectedSkill) => {
            Debug.Log($"🎯 Skill de teste selecionada: {selectedSkill.skillName}");
        });
    }
}