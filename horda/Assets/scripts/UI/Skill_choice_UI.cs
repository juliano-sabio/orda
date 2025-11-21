using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillChoiceUI : MonoBehaviour
{
    [Header("Referências UI")]
    public GameObject choicePanel;
    public Text titleText; // UI padrão
    public TextMeshProUGUI titleTextTMP; // TextMeshPro (opcional)
    public Transform skillsContainer;
    public GameObject skillChoicePrefab;
    public Button confirmButton;

    [Header("Configurações")]
    public float autoCloseDelay = 2f;

    private List<SkillData> currentChoices;
    private System.Action<SkillData> onSkillChosen;
    private List<GameObject> currentButtons = new List<GameObject>();

    void Awake()
    {
        // Garante que o painel está ativo para receber eventos
        if (choicePanel != null)
        {
            choicePanel.SetActive(false);
        }
    }

    void Start()
    {
        Debug.Log("🔄 SkillChoiceUI iniciando...");

        // 🆕 CORREÇÃO: Registro seguro no SkillManager
        RegisterWithSkillManager();

        // Garante que está ativo e bem configurado
        if (gameObject.activeInHierarchy)
        {
            Debug.Log("✅ SkillChoiceUI ativo na hierarquia");
        }
        else
        {
            Debug.LogError("❌ SkillChoiceUI INATIVO na hierarquia! Ativando...");
            gameObject.SetActive(true);
        }

        // Reposicionamento seguro
        StartCoroutine(InitializePanel());
    }

    // 🆕 CORREÇÃO: Método para registro seguro no SkillManager
    private void RegisterWithSkillManager()
    {
        if (SkillManager.Instance != null)
        {
            // 🆕 USA O MÉTODO PÚBLICO para registrar
            SkillManager.Instance.RegisterSkillChoiceListener(ShowSkillChoice);
            Debug.Log("✅ Registrado no SkillManager usando método público");
        }
        else
        {
            Debug.LogError("❌ SkillManager não encontrado no Start!");
            // Tenta encontrar novamente após delay
            StartCoroutine(RegisterWithSkillManagerDelayed());
        }
    }

    // 🆕 CORREÇÃO: Registro com delay se necessário
    private IEnumerator RegisterWithSkillManagerDelayed()
    {
        yield return new WaitForSeconds(1f);

        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.RegisterSkillChoiceListener(ShowSkillChoice);
            Debug.Log("✅ Registrado no SkillManager após delay");
        }
        else
        {
            Debug.LogError("❌ SkillManager ainda não encontrado após delay!");
        }
    }

    // 🆕 CORREÇÃO: Inicialização mais robusta do painel
    private IEnumerator InitializePanel()
    {
        yield return new WaitForEndOfFrame();

        if (choicePanel != null)
        {
            // Garante configuração correta do RectTransform
            RectTransform rect = choicePanel.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
            }

            Debug.Log("✅ Painel de escolha inicializado corretamente");
        }
    }

    // 🆕 CORREÇÃO: Método público para ser chamado pelo SkillManager
    public void ShowSkillChoice(List<SkillData> skills, System.Action<SkillData> callback)
    {
        Debug.Log("🎯 ShowSkillChoice chamado!");

        if (skills == null || skills.Count == 0)
        {
            Debug.LogError("❌ Lista de skills vazia!");
            return;
        }

        // Verifica se o GameObject está ativo
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogError("❌ SkillChoiceUI GameObject está INATIVO! Ativando...");
            gameObject.SetActive(true);
        }

        currentChoices = skills;
        onSkillChosen = callback;

        // Limpa container anterior
        ClearSkillButtons();

        // Ativa o painel ANTES de criar os botões
        if (choicePanel != null)
        {
            choicePanel.SetActive(true);
            Debug.Log("✅ Painel de escolha ativado");
        }
        else
        {
            Debug.LogError("❌ ChoicePanel não atribuído!");
            return;
        }

        // 🆕 CORREÇÃO: Atualiza título (compatível com Text e TextMeshPro)
        UpdateTitleText();

        Debug.Log($"📋 Mostrando escolha de {skills.Count} skills");

        // Usa Coroutine para criar botões de forma segura
        StartCoroutine(CreateSkillButtonsWithDelay(skills));
    }

    // 🆕 CORREÇÃO: Método para atualizar título compatível com ambos os sistemas
    private void UpdateTitleText()
    {
        PlayerStats playerStats = FindAnyObjectByType<PlayerStats>();
        int currentLevel = playerStats != null ? playerStats.level : 1;
        string title = $"🎯 ESCOLHA UMA SKILL (Nível {currentLevel})";

        // Tenta usar TextMeshPro primeiro, depois Text padrão
        if (titleTextTMP != null)
        {
            titleTextTMP.text = title;
            Debug.Log("✅ Título atualizado (TextMeshPro)");
        }
        else if (titleText != null)
        {
            titleText.text = title;
            Debug.Log("✅ Título atualizado (Text padrão)");
        }
        else
        {
            Debug.LogWarning("⚠️ Nenhum componente de texto atribuído para o título");
        }
    }

    // 🆕 CORREÇÃO: Cria botões com delay para garantir layout
    private IEnumerator CreateSkillButtonsWithDelay(List<SkillData> skills)
    {
        yield return new WaitForEndOfFrame();

        for (int i = 0; i < skills.Count; i++)
        {
            CreateSkillChoiceButton(skills[i], i);
        }

        Debug.Log($"✅ {skills.Count} botões de skill criados");
    }

    private void CreateSkillChoiceButton(SkillData skill, int index)
    {
        if (skillChoicePrefab == null)
        {
            Debug.LogError("❌ SkillChoicePrefab não atribuído!");
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

        // 🆕 CORREÇÃO: Configura texto compatível com ambos os sistemas
        SetupButtonText(buttonObj, skill);

        // Configura cor baseada no elemento
        Image buttonImage = buttonObj.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = GetElementColor(skill.element) * 0.8f;
        }

        // Configura o clique
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnSkillSelected(skill));

        // Posicionamento
        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = new Vector2(0, -index * 120);
        }

        buttonObj.SetActive(true);
        Debug.Log($"✅ Botão criado para: {skill.skillName}");
    }

    // 🆕 CORREÇÃO: Método para configurar texto compatível
    private void SetupButtonText(GameObject buttonObj, SkillData skill)
    {
        string buttonText = $"<b>{skill.skillName}</b>\n" +
                           $"{GetElementIcon(skill.element)} {skill.element}\n" +
                           $"{skill.description}";

        // Tenta TextMeshPro primeiro
        TextMeshProUGUI textTMP = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (textTMP != null)
        {
            textTMP.text = buttonText;
            return;
        }

        // Se não encontrar TextMeshPro, tenta Text padrão
        Text text = buttonObj.GetComponentInChildren<Text>();
        if (text != null)
        {
            // Remove tags HTML para Text padrão
            string plainText = buttonText.Replace("<b>", "").Replace("</b>", "");
            text.text = plainText;
            return;
        }

        Debug.LogWarning($"⚠️ Nenhum componente de texto encontrado no botão para: {skill.skillName}");
    }

    private void OnSkillSelected(SkillData selectedSkill)
    {
        Debug.Log($"🎯 Skill selecionada: {selectedSkill.skillName}");

        // Efeito visual de confirmação
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
        // Feedback visual
        foreach (var button in currentButtons)
        {
            if (button != null)
            {
                Button btn = button.GetComponent<Button>();
                if (btn != null) btn.interactable = false;
            }
        }

        yield return new WaitForSeconds(0.5f);

        // Executa o callback
        onSkillChosen?.Invoke(selectedSkill);

        // Fecha o painel
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

        // Limpa children do container também
        if (skillsContainer != null)
        {
            foreach (Transform child in skillsContainer)
            {
                if (child != null && child.gameObject != null)
                    Destroy(child.gameObject);
            }
        }
    }

    public void ClosePanel()
    {
        if (choicePanel != null)
        {
            choicePanel.SetActive(false);
            Debug.Log("🔒 Painel de escolha de skill fechado");
        }

        ClearSkillButtons();

        // Limpa referências
        currentChoices = null;
        onSkillChosen = null;
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
        // 🆕 CORREÇÃO: Desregistra usando método público
        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.UnregisterSkillChoiceListener(ShowSkillChoice);
            Debug.Log("🔒 Desregistrado do SkillManager");
        }
    }

    // 🆕 MÉTODO PARA VERIFICAR CONFIGURAÇÃO
    [ContextMenu("🔍 Verificar Configuração do SkillChoiceUI")]
    public void CheckConfiguration()
    {
        Debug.Log("🔍 CONFIGURAÇÃO DO SKILLCHOICEUI:");
        Debug.Log($"• GameObject ativo: {gameObject.activeInHierarchy}");
        Debug.Log($"• ChoicePanel atribuído: {choicePanel != null}");
        Debug.Log($"• ChoicePanel ativo: {choicePanel?.activeInHierarchy ?? false}");
        Debug.Log($"• SkillsContainer atribuído: {skillsContainer != null}");
        Debug.Log($"• SkillChoicePrefab atribuído: {skillChoicePrefab != null}");
        Debug.Log($"• TitleText (UI): {titleText != null}");
        Debug.Log($"• TitleTextTMP (TextMeshPro): {titleTextTMP != null}");
        Debug.Log($"• SkillManager disponível: {SkillManager.Instance != null}");

        if (SkillManager.Instance != null)
        {
            Debug.Log($"• Registrado no SkillManager: ✅");
        }
    }

    // 🆕 MÉTODO PARA ATIVAR MANUALMENTE
    [ContextMenu("🚀 Ativar SkillChoiceUI Manualmente")]
    public void ActivateManually()
    {
        gameObject.SetActive(true);
        if (choicePanel != null)
        {
            choicePanel.SetActive(true);
        }

        // 🆕 Re-registra no SkillManager
        RegisterWithSkillManager();

        Debug.Log("✅ SkillChoiceUI ativado e registrado manualmente");
    }

    // 🆕 MÉTODO PARA CONVERTER PARA TEXTMESHPRO (se necessário)
    [ContextMenu("🔄 Configurar para TextMeshPro")]
    public void SetupForTextMeshPro()
    {
        // Se estiver usando Text padrão, tenta encontrar/migrar para TextMeshPro
        if (titleText != null && titleTextTMP == null)
        {
            titleTextTMP = titleText.GetComponent<TextMeshProUGUI>();
            if (titleTextTMP == null)
            {
                Debug.LogWarning("⚠️ TextMeshProUGUI não encontrado no título. Considere migrar para TextMeshPro.");
            }
        }

        Debug.Log("✅ Configuração TextMeshPro verificada");
    }

    // Método para teste manual
    [ContextMenu("🎯 Testar Skill Choice UI")]
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

        Debug.Log("🎯 Iniciando teste manual...");
        ShowSkillChoice(testSkills, (selectedSkill) => {
            Debug.Log($"✅ Skill de teste selecionada: {selectedSkill.skillName}");
        });
    }
}