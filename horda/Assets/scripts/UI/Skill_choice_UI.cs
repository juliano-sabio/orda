using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillChoiceUI : MonoBehaviour
{
    [Header("Referências UI")]
    public GameObject choicePanel;
    public Text titleText;
    public TextMeshProUGUI titleTextTMP;
    public Transform skillsContainer;
    public GameObject skillChoicePrefab;
    public Button confirmButton;

    [Header("Configurações")]
    public float autoCloseDelay = 2f;
    public bool pauseGameDuringChoice = true;

    [Header("Fallbacks Automáticos")]
    public bool createFallbackUI = true;

    private List<SkillData> currentChoices;
    private System.Action<SkillData> onSkillChosen;
    private List<GameObject> currentButtons = new List<GameObject>();
    private float previousTimeScale;
    private GameObject fallbackPanel;

    void Awake()
    {
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("⚠️ SkillChoiceUI inativo no Awake! Ativando...");
            gameObject.SetActive(true);
        }

        if (choicePanel != null)
        {
            choicePanel.SetActive(false);
        }
    }

    void Start()
    {
        Debug.Log("🔄 SkillChoiceUI iniciando...");
        StartCoroutine(InitializeWithDelay());
    }

    private IEnumerator InitializeWithDelay()
    {
        yield return new WaitForSeconds(0.5f);
        CheckAndFixConfiguration();
        Debug.Log("✅ SkillChoiceUI inicializado completamente");
    }

    private void CheckAndFixConfiguration()
    {
        Debug.Log("🔧 Verificando configuração do SkillChoiceUI...");

        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
            Debug.Log("✅ GameObject ativado");
        }

        if (choicePanel == null)
        {
            Debug.LogWarning("⚠️ ChoicePanel não atribuído! Procurando automaticamente...");
            choicePanel = FindPanelInChildren();

            if (choicePanel == null && createFallbackUI)
            {
                choicePanel = CreateFallbackPanel();
                Debug.Log("✅ Painel fallback criado");
            }
        }

        if (skillChoicePrefab == null)
        {
            Debug.LogWarning("⚠️ SkillChoicePrefab não atribuído! Criando fallback...");
            skillChoicePrefab = CreateFallbackPrefab();
        }

        if (skillsContainer == null && choicePanel != null)
        {
            skillsContainer = choicePanel.transform;
            Debug.Log("✅ Usando transform do painel como container");
        }

        Debug.Log("✅ Configuração verificada e corrigida");
    }

    private GameObject FindPanelInChildren()
    {
        foreach (Transform child in transform)
        {
            if (child.name.Contains("Panel") || child.name.Contains("painel", System.StringComparison.OrdinalIgnoreCase))
            {
                return child.gameObject;
            }
        }

        if (transform.childCount > 0)
        {
            return transform.GetChild(0).gameObject;
        }

        return null;
    }

    private GameObject CreateFallbackPanel()
    {
        GameObject panel = new GameObject("SkillChoicePanel_Fallback");
        panel.transform.SetParent(transform);

        RectTransform rect = panel.AddComponent<RectTransform>();
        Image image = panel.AddComponent<Image>();

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(600, 400);
        rect.anchoredPosition = Vector2.zero;

        image.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        GameObject container = new GameObject("SkillsContainer");
        container.transform.SetParent(panel.transform);
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(500, 300);
        containerRect.anchoredPosition = Vector2.zero;

        skillsContainer = container.transform;

        return panel;
    }

    private GameObject CreateFallbackPrefab()
    {
        GameObject prefab = new GameObject("SkillButton_Fallback");

        RectTransform rect = prefab.AddComponent<RectTransform>();
        Image image = prefab.AddComponent<Image>();
        Button button = prefab.AddComponent<Button>();

        rect.sizeDelta = new Vector2(400, 80);
        image.color = Color.gray;

        GameObject textObj = new GameObject("ButtonText");
        textObj.transform.SetParent(prefab.transform);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI textTMP = textObj.AddComponent<TextMeshProUGUI>();
        if (textTMP != null)
        {
            textTMP.text = "Skill Button";
            textTMP.color = Color.white;
            textTMP.alignment = TextAlignmentOptions.Center;
            textTMP.fontSize = 14;
        }
        else
        {
            Text text = textObj.AddComponent<Text>();
            text.text = "Skill Button";
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 14;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = Color.gray;
        colors.highlightedColor = Color.blue;
        colors.pressedColor = Color.cyan;
        colors.selectedColor = Color.blue;
        button.colors = colors;

        prefab.SetActive(false);
        return prefab;
    }

    public void ShowSkillChoice(List<SkillData> skills, System.Action<SkillData> callback)
    {
        Debug.Log("🎯 ShowSkillChoice chamado!");

        if (skills == null || skills.Count == 0)
        {
            Debug.LogError("❌ Lista de skills vazia!");
            return;
        }

        CheckAndFixConfiguration();

        if (!gameObject.activeInHierarchy)
        {
            Debug.LogError("❌ SkillChoiceUI GameObject está INATIVO! Ativando...");
            gameObject.SetActive(true);
        }

        currentChoices = skills;
        onSkillChosen = callback;

        PauseGame();
        ClearSkillButtons();

        if (choicePanel != null)
        {
            choicePanel.SetActive(true);
            Debug.Log("✅ Painel de escolha ativado");
        }
        else
        {
            Debug.LogError("❌ ChoicePanel não atribuído mesmo após fallback!");
            ResumeGame();
            return;
        }

        UpdateTitleText();
        Debug.Log($"📋 Mostrando escolha de {skills.Count} skills");

        StartCoroutine(CreateSkillButtonsWithDelay(skills));
    }

    private void PauseGame()
    {
        if (pauseGameDuringChoice)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            Debug.Log("⏸️ Jogo pausado durante escolha de skill");
            AudioListener.pause = true;
        }
    }

    private void ResumeGame()
    {
        if (pauseGameDuringChoice)
        {
            Time.timeScale = previousTimeScale;
            AudioListener.pause = false;
            Debug.Log("▶️ Jogo despausado");
        }
    }

    private void UpdateTitleText()
    {
        PlayerStats playerStats = FindAnyObjectByType<PlayerStats>();
        int currentLevel = playerStats != null ? playerStats.level : 1;
        string title = $"🎯 ESCOLHA UMA SKILL (Nível {currentLevel})";

        if (titleTextTMP != null)
        {
            titleTextTMP.text = title;
        }
        else if (titleText != null)
        {
            titleText.text = title;
        }
        else
        {
            Debug.LogWarning("⚠️ Nenhum componente de texto atribuído para o título");
        }
    }

    private IEnumerator CreateSkillButtonsWithDelay(List<SkillData> skills)
    {
        yield return new WaitForEndOfFrame();

        for (int i = 0; i < skills.Count; i++)
        {
            CreateSkillChoiceButton(skills[i], i);
        }

        Debug.Log($"✅ {skills.Count} botões de skill criados");
    }

    private void ClearSkillButtons()
    {
        foreach (GameObject button in currentButtons)
        {
            if (button != null)
                Destroy(button);
        }
        currentButtons.Clear();

        if (skillsContainer != null)
        {
            foreach (Transform child in skillsContainer)
            {
                if (child != null && child.gameObject != null)
                    Destroy(child.gameObject);
            }
        }
    }

    private void CreateSkillChoiceButton(SkillData skill, int index)
    {
        if (skillChoicePrefab == null)
        {
            Debug.LogError("❌ SkillChoicePrefab não atribuído mesmo após fallback!");
            return;
        }

        GameObject buttonObj = Instantiate(skillChoicePrefab, skillsContainer);
        currentButtons.Add(buttonObj);

        Button button = buttonObj.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("❌ Botão não encontrado no prefab!");
            return;
        }

        SetupButtonText(buttonObj, skill);

        Image buttonImage = buttonObj.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = GetElementColor(skill.element) * 0.8f;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnSkillSelected(skill));

        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = new Vector2(0, -index * 120);
        }

        buttonObj.SetActive(true);
        Debug.Log($"✅ Botão criado para: {skill.skillName}");
    }

    private void SetupButtonText(GameObject buttonObj, SkillData skill)
    {
        string buttonText = $"<b>{skill.skillName}</b>\n" +
                           $"{GetElementIcon(skill.element)} {skill.element}\n" +
                           $"{skill.description}";

        TextMeshProUGUI textTMP = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (textTMP != null)
        {
            textTMP.text = buttonText;
            return;
        }

        Text text = buttonObj.GetComponentInChildren<Text>();
        if (text != null)
        {
            string plainText = buttonText.Replace("<b>", "").Replace("</b>", "");
            text.text = plainText;
            return;
        }

        Debug.LogWarning($"⚠️ Nenhum componente de texto encontrado no botão para: {skill.skillName}");
    }

    private void OnSkillSelected(SkillData selectedSkill)
    {
        Debug.Log($"🎯 Skill selecionada: {selectedSkill.skillName}");

        if (selectedSkill != null)
        {
            StartCoroutine(SelectionConfirmationEffect(selectedSkill));
        }
        else
        {
            Debug.LogError("❌ Skill selecionada é null!");
            ClosePanel();
        }
    }

    private IEnumerator SelectionConfirmationEffect(SkillData selectedSkill)
    {
        foreach (var button in currentButtons)
        {
            if (button != null)
            {
                Button btn = button.GetComponent<Button>();
                if (btn != null) btn.interactable = false;
            }
        }

        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        onSkillChosen?.Invoke(selectedSkill);

        elapsed = 0f;
        while (elapsed < autoCloseDelay)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        ClosePanel();
    }

    public void ClosePanel()
    {
        if (choicePanel != null)
        {
            choicePanel.SetActive(false);
            Debug.Log("🔒 Painel de escolha de skill fechado");
        }

        ClearSkillButtons();
        ResumeGame();
        currentChoices = null;
        onSkillChosen = null;
    }

    [ContextMenu("🎯 Forçar Aparecimento da Escolha")]
    public void ForceShowChoice()
    {
        Debug.Log("🎯 Forçando aparecimento da escolha de skills...");

        CheckAndFixConfiguration();

        List<SkillData> testSkills = CreateTestSkills();

        ShowSkillChoice(testSkills, (selectedSkill) => {
            Debug.Log($"✅ Skill selecionada: {selectedSkill.skillName}");

            if (SkillManager.Instance != null)
            {
                SkillManager.Instance.AddSkill(selectedSkill);
            }
        });
    }

    private List<SkillData> CreateTestSkills()
    {
        List<SkillData> testSkills = new List<SkillData>();

        // Skill de teste 1 - Projétil
        SkillData testSkill1 = ScriptableObject.CreateInstance<SkillData>();
        testSkill1.skillName = "🔥 Projétil de Fogo";
        testSkill1.description = "Dispara projéteis de fogo que queimam inimigos";
        testSkill1.attackBonus = 15f;
        testSkill1.healthBonus = 10f;
        testSkill1.element = PlayerStats.Element.Fire;
        testSkill1.specificType = SpecificSkillType.Projectile;
        testSkills.Add(testSkill1);

        // Skill de teste 2 - Regeneração
        SkillData testSkill2 = ScriptableObject.CreateInstance<SkillData>();
        testSkill2.skillName = "💚 Regeneração";
        testSkill2.description = "Regenera vida gradualmente durante a batalha";
        testSkill2.healthBonus = 20f;
        testSkill2.healthRegenBonus = 2f;
        testSkill2.specificType = SpecificSkillType.HealthRegen;
        testSkills.Add(testSkill2);

        // Skill de teste 3 - Velocidade
        SkillData testSkill3 = ScriptableObject.CreateInstance<SkillData>();
        testSkill3.skillName = "💨 Velocidade do Vento";
        testSkill3.description = "Aumenta a velocidade de movimento e ataque";
        testSkill3.speedBonus = 2f;
        testSkill3.attackSpeedMultiplier = 0.8f;
        testSkill3.element = PlayerStats.Element.Wind;
        testSkills.Add(testSkill3);

        return testSkills;
    }

    private Color GetElementColor(PlayerStats.Element element)
    {
        switch (element)
        {
            case PlayerStats.Element.Fire: return Color.red;
            case PlayerStats.Element.Poison: return Color.blue;
            case PlayerStats.Element.Earth: return Color.green;
            case PlayerStats.Element.Wind: return Color.cyan;
            case PlayerStats.Element.Lightning: return Color.yellow;
            default: return Color.white;
        }
    }

    private string GetElementIcon(PlayerStats.Element element)
    {
        switch (element)
        {
            case PlayerStats.Element.Fire: return "🔥";
            case PlayerStats.Element.Poison: return "💧";
            case PlayerStats.Element.Earth: return "🌿";
            case PlayerStats.Element.Wind: return "💨";
            case PlayerStats.Element.Lightning: return "⚡";
            default: return "✨";
        }
    }

    void OnDestroy()
    {
        ResumeGame();
    }
}