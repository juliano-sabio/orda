using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

    [Header("Layout Horizontal")]
    public bool useHorizontalLayout = true;
    public float cardSpacing = 30f;
    public Vector2 cardSize = new Vector2(300f, 450f);

    private List<SkillData> currentChoices;
    private System.Action<SkillData> onSkillChosen;
    private List<GameObject> currentButtons = new List<GameObject>();
    private float previousTimeScale;

    void Awake()
    {
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
        SetupHorizontalLayout();
        Debug.Log("✅ SkillChoiceUI inicializado completamente");
    }

    private void SetupHorizontalLayout()
    {
        if (skillsContainer == null || !useHorizontalLayout) return;

        HorizontalLayoutGroup layout = skillsContainer.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
            layout = skillsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();

        layout.spacing = cardSpacing;
        layout.padding = new RectOffset(30, 30, 20, 20);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter sizeFitter = skillsContainer.GetComponent<ContentSizeFitter>();
        if (sizeFitter != null)
        {
            DestroyImmediate(sizeFitter);
        }

        RectTransform containerRect = skillsContainer as RectTransform;
        if (containerRect != null)
        {
            containerRect.sizeDelta = new Vector2(1200f, 500f);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
        }

        Debug.Log("✅ Layout horizontal configurado");
    }

    public void ShowSkillChoice(List<SkillData> skills, System.Action<SkillData> callback)
    {
        Debug.Log("🎯 ShowSkillChoice chamado!");

        if (skills == null || skills.Count == 0)
        {
            Debug.LogError("❌ Lista de skills vazia!");
            return;
        }

        // 🎯 GARANTIR que o GameObject está ATIVO
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("⚠️ SkillChoiceUI inativo! Ativando...");
            gameObject.SetActive(true);
        }

        // 🎯 GARANTIR que o Canvas está ATIVO
        if (choicePanel != null && !choicePanel.activeInHierarchy)
        {
            choicePanel.SetActive(true);
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

        UpdateTitleText();
        Debug.Log($"📋 Mostrando escolha de {skills.Count} skills");

        // 🎯 INICIAR COROUTINE COM SEGURANÇA
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(CreateSkillButtonsWithSpecificPrefabs(skills));
        }
        else
        {
            Debug.LogError("❌ Não pode iniciar coroutine - GameObject inativo!");
            // Fallback: executar imediatamente
            StartCoroutine(CreateSkillButtonsWithSpecificPrefabs(skills));
        }
    }
    private IEnumerator CreateSkillButtonsWithSpecificPrefabs(List<SkillData> skills)
    {
        yield return new WaitForEndOfFrame();

        for (int i = 0; i < skills.Count; i++)
        {
            CreateSkillButtonWithSpecificPrefab(skills[i], i);
        }

        Debug.Log($"✅ {skills.Count} cards criados com prefabs específicos");

        yield return StartCoroutine(ForceLayoutRefresh());
    }

    private void CreateSkillButtonWithSpecificPrefab(SkillData skill, int index)
    {
        GameObject cardPrefabToUse = skill.cardPrefab;

        if (cardPrefabToUse == null)
        {
            cardPrefabToUse = skillChoicePrefab;
            Debug.LogWarning($"⚠️ Skill {skill.skillName} não tem cardPrefab! Usando fallback genérico");
        }

        if (cardPrefabToUse == null)
        {
            cardPrefabToUse = Resources.Load<GameObject>("Cards/SkillCard_Auto");
            Debug.LogWarning($"⚠️ Usando card automático do Resources para: {skill.skillName}");
        }

        if (cardPrefabToUse == null)
        {
            Debug.LogError($"🚨 Criando card de emergência para: {skill.skillName}");
            CreateEmergencySkillButton(skill, index);
            return;
        }

        GameObject cardObj = Instantiate(cardPrefabToUse, skillsContainer);
        cardObj.name = $"{skill.skillName}_Instance";
        cardObj.SetActive(true);
        currentButtons.Add(cardObj);

        SetupCardTransform(cardObj);
        InitializeCardWithSkillData(cardObj, skill);

        Debug.Log($"✅ Card criado: {skill.skillName} | Prefab: {cardPrefabToUse.name}");
    }

    private void InitializeCardWithSkillData(GameObject cardObj, SkillData skill)
    {
        // 🚫 BLOQUEIO TOTAL - não modifica NADA se for prefab no Editor
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Debug.LogWarning($"🚨 BLOQUEADO: Tentativa de modificar fora do runtime - {skill.skillName}");
            return;
        }
#endif

        // 🎯 VERIFICAÇÃO EXTRA - só modifica em runtime
        if (!Application.isPlaying)
        {
            Debug.LogError($"❌ TENTATIVA PERIGOSA: Modificar em Editor - {skill.skillName}");
            return;
        }

        // 🎯 MÉTODO SEGURO - apenas textos, sem cores
        SetupCardTextsOnly(cardObj, skill);

        // Configura o botão (seguro)
        Button button = cardObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnSkillSelected(skill));
            button.interactable = true;
        }

        Debug.Log($"✅ Card configurado (APENAS TEXTO): {skill.skillName}");
    }

    private void SetupCardTextsOnly(GameObject cardObj, SkillData skill)
    {
        // 🎯 APENAS modifica textos - NUNCA cores

        // Encontra componentes de texto
        TextMeshProUGUI[] textComponents = cardObj.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (TextMeshProUGUI text in textComponents)
        {
            if (text.name.Contains("Name") || text.name.Contains("Nome") || text.name.Contains("Title"))
            {
                string elementIcon = skill.GetElementIcon();
                text.text = $"<b>{skill.skillName}</b>\n{elementIcon} {skill.element}";
            }
            else if (text.name.Contains("Desc") || text.name.Contains("Description") || text.name.Contains("Detail"))
            {
                text.text = skill.description;
            }
            else if (text.name.Contains("Stats") || text.name.Contains("Status") || text.name.Contains("Bonus"))
            {
                text.text = GetManualStatsText(skill);
            }
            // 🚫 NUNCA modifica text.color!
        }

        // 🎯 Ícone (apenas sprite, sem cor)
        Image[] images = cardObj.GetComponentsInChildren<Image>();
        foreach (Image img in images)
        {
            if ((img.name.Contains("Icon") || img.name.Contains("Image")) && skill.icon != null)
            {
                img.sprite = skill.icon;
                // 🚫 NUNCA modifica img.color!
            }
        }
    }

    private void SetupCardTransform(GameObject cardObj)
    {
        RectTransform rect = cardObj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localScale = Vector3.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = cardSize;
        }

        LayoutElement layoutElem = cardObj.GetComponent<LayoutElement>();
        if (layoutElem == null)
        {
            layoutElem = cardObj.AddComponent<LayoutElement>();
        }
        layoutElem.preferredWidth = cardSize.x;
        layoutElem.preferredHeight = cardSize.y;
        layoutElem.flexibleWidth = 0;
        layoutElem.flexibleHeight = 0;
    }

    private void CreateEmergencySkillButton(SkillData skill, int index)
    {
        GameObject cardObj = new GameObject($"EmergencyCard_{skill.skillName}",
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));

        cardObj.transform.SetParent(skillsContainer);
        cardObj.SetActive(true);
        currentButtons.Add(cardObj);

        Image image = cardObj.GetComponent<Image>();
        image.color = new Color(0.3f, 0.2f, 0.2f, 1f);

        SetupCardTransform(cardObj);
        SetupSkillCardManually(cardObj, skill);

        Button button = cardObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnSkillSelected(skill));
        }

        Debug.LogWarning($"🚨 Card de emergência criado para: {skill.skillName}");
    }

    private void SetupSkillCardManually(GameObject cardObj, SkillData skill)
    {
        // 🎯 MÉTODO APENAS PARA EMERGÊNCIA - também sem cores
        TextMeshProUGUI[] textComponents = cardObj.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (TextMeshProUGUI text in textComponents)
        {
            if (text.name.Contains("Name") || text.name.Contains("Nome") || text.name.Contains("Title"))
            {
                text.text = $"<b>{skill.skillName}</b>";
            }
            else if (text.name.Contains("Desc") || text.name.Contains("Description") || text.name.Contains("Detail"))
            {
                text.text = skill.description;
            }
            else if (text.name.Contains("Stats") || text.name.Contains("Status") || text.name.Contains("Bonus"))
            {
                text.text = GetManualStatsText(skill);
            }
        }

        Image[] images = cardObj.GetComponentsInChildren<Image>();
        foreach (Image img in images)
        {
            if ((img.name.Contains("Icon") || img.name.Contains("Image")) && skill.icon != null)
            {
                img.sprite = skill.icon;
                // 🚫 NUNCA modifica img.color!
            }
        }
    }

    private string GetManualStatsText(SkillData skill)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        if (skill.healthBonus != 0) sb.Append($"❤️{skill.healthBonus} ");
        if (skill.attackBonus != 0) sb.Append($"⚔️{skill.attackBonus} ");
        if (skill.defenseBonus != 0) sb.Append($"🛡️{skill.defenseBonus} ");
        if (skill.speedBonus != 0) sb.Append($"🏃{skill.speedBonus} ");
        if (skill.healthRegenBonus != 0) sb.Append($"💚{skill.healthRegenBonus} ");

        if (sb.Length == 0) sb.Append("💎 Bônus Passivo");

        return sb.ToString();
    }

    private IEnumerator ForceLayoutRefresh()
    {
        yield return new WaitForEndOfFrame();

        if (skillsContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(skillsContainer as RectTransform);
            Canvas.ForceUpdateCanvases();
        }
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

        yield return new WaitForSecondsRealtime(0.5f);

        onSkillChosen?.Invoke(selectedSkill);

        yield return new WaitForSecondsRealtime(autoCloseDelay);

        ClosePanel();
    }

    public void ClosePanel()
    {
        if (choicePanel != null)
        {
            choicePanel.SetActive(false);
        }

        ClearSkillButtons();
        ResumeGame();
        currentChoices = null;
        onSkillChosen = null;
    }

    private void PauseGame()
    {
        if (pauseGameDuringChoice)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            AudioListener.pause = true;
        }
    }

    private void ResumeGame()
    {
        if (pauseGameDuringChoice)
        {
            Time.timeScale = previousTimeScale;
            AudioListener.pause = false;
        }
    }

    private void UpdateTitleText()
    {
        PlayerStats playerStats = FindAnyObjectByType<PlayerStats>();
        int currentLevel = playerStats != null ? playerStats.level : 1;
        string title = $"SKILL (Nível {currentLevel})";

        if (titleTextTMP != null)
        {
            titleTextTMP.text = title;
        }
        else if (titleText != null)
        {
            titleText.text = title;
        }
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

    [ContextMenu("🎯 Testar Sistema de Cards")]
    public void TestAutoCardSystem()
    {
        Debug.Log("🧪 Testando sistema de cards automático...");

        List<SkillData> testSkills = CreateTestSkills();
        ShowSkillChoice(testSkills, (selectedSkill) => {
            Debug.Log($"✅ Sistema funcionando! Skill escolhida: {selectedSkill.skillName}");
        });
    }

    private List<SkillData> CreateTestSkills()
    {
        List<SkillData> testSkills = new List<SkillData>();

        SkillData testSkill1 = ScriptableObject.CreateInstance<SkillData>();
        testSkill1.skillName = "Bola de Fogo";
        testSkill1.description = "Lança uma poderosa bola de fogo que causa dano em área";
        testSkill1.attackBonus = 25f;
        testSkill1.healthBonus = 10f;
        testSkill1.element = PlayerStats.Element.Fire;
        testSkill1.rarity = SkillRarity.Rare;
        testSkills.Add(testSkill1);

        SkillData testSkill2 = ScriptableObject.CreateInstance<SkillData>();
        testSkill2.skillName = "Armadura de Gelo";
        testSkill2.description = "Cria uma armadura gelada que aumenta a defesa";
        testSkill2.defenseBonus = 15f;
        testSkill2.healthBonus = 20f;
        testSkill2.element = PlayerStats.Element.Ice;
        testSkill2.rarity = SkillRarity.Uncommon;
        testSkills.Add(testSkill2);

        return testSkills;
    }

    [ContextMenu("🔍 DEBUG: Verificar ONDE está modificando")]
    public void DebugWhereIsModifying()
    {
        Debug.Log("=== 🔍 INICIANDO DIAGNÓSTICO COMPLETO ===");

        // 1. Verificar se está no Editor ou Runtime
        Debug.Log($"🎯 Modo: {(Application.isPlaying ? "RUNTIME" : "EDITOR")}");

        // 2. Verificar todos os prefabs na pasta Resources
        GameObject[] allPrefabs = Resources.LoadAll<GameObject>("Cards");
        Debug.Log($"📁 Prefabs carregados: {allPrefabs.Length}");

        foreach (var prefab in allPrefabs)
        {
            Debug.Log($"\n🔍 Analisando prefab: {prefab.name}");

            // Verificar se tem scripts
            Component[] allComponents = prefab.GetComponents<Component>();
            foreach (var comp in allComponents)
            {
                if (comp == null) continue;
                string typeName = comp.GetType().Name;
                if (typeName.Contains("SkillCard") || typeName.Contains("Controller"))
                {
                    Debug.LogError($"   ❌ TEM SCRIPT: {typeName}");
                }
            }

            // Verificar propriedades modificadas
            CheckPrefabProperties(prefab);
        }

        // 3. Verificar instâncias atuais
        if (skillsContainer != null && skillsContainer.childCount > 0)
        {
            Debug.Log($"\n🎴 Instâncias atuais: {skillsContainer.childCount}");
            for (int i = 0; i < skillsContainer.childCount; i++)
            {
                Transform child = skillsContainer.GetChild(i);
                Debug.Log($"   📦 {child.name} (Instância)");
            }
        }

        Debug.Log("=== FIM DO DIAGNÓSTICO ===");
    }

    private void CheckPrefabProperties(GameObject prefab)
    {
        // Verifica cores e textos dos prefabs
        Image bg = prefab.GetComponent<Image>();
        if (bg != null)
        {
            Debug.Log($"   🎨 Background color: {bg.color}");
        }

        // Verifica textos nos filhos
        TextMeshProUGUI[] texts = prefab.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (var text in texts)
        {
            Debug.Log($"   📝 {text.name}: '{text.text}' | Cor: {text.color}");
        }

        // Verifica imagens nos filhos
        Image[] images = prefab.GetComponentsInChildren<Image>();
        foreach (var img in images)
        {
            if (img.sprite != null)
            {
                Debug.Log($"   🖼️ {img.name}: Sprite '{img.sprite.name}' | Cor: {img.color}");
            }
        }
    }

    [ContextMenu("🔧 Verificar Configuração")]
    public void CheckConfiguration()
    {
        Debug.Log("🔍 Verificando configuração do SkillChoiceUI...");
        Debug.Log($"📊 Choice Panel: {choicePanel != null}");
        Debug.Log($"📦 Skills Container: {skillsContainer != null}");
        Debug.Log($"🎯 Skill Prefab: {skillChoicePrefab != null}");
        Debug.Log($"🔄 Horizontal Layout: {useHorizontalLayout}");
        Debug.Log($"📐 Card Size: {cardSize}");
        Debug.Log($"📏 Card Spacing: {cardSpacing}");

        if (skillsContainer != null)
        {
            HorizontalLayoutGroup layout = skillsContainer.GetComponent<HorizontalLayoutGroup>();
            ContentSizeFitter fitter = skillsContainer.GetComponent<ContentSizeFitter>();

            Debug.Log($"📐 Layout Group: {layout != null}");
            Debug.Log($"🔒 ContentSizeFitter: {fitter != null} (deve ser NULL)");
        }
    }

    [ContextMenu("🔓 Destravar Container Manualmente")]
    public void UnlockContainerManually()
    {
        if (skillsContainer == null)
        {
            Debug.LogError("❌ Container não encontrado!");
            return;
        }

        ContentSizeFitter fitter = skillsContainer.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            DestroyImmediate(fitter);
            Debug.Log("✅ ContentSizeFitter removido!");
        }

        HorizontalLayoutGroup layout = skillsContainer.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
        {
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            Debug.Log("✅ Layout Group destravado!");
        }

        RectTransform rect = skillsContainer as RectTransform;
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(1200, 500);
            Debug.Log($"✅ Container configurado - Tamanho: {rect.sizeDelta}");
        }

        Debug.Log("🎯 Container destravado e configurado manualmente!");
    }
    [ContextMenu("🔍 Verificar Container e Cards")]
    public void DebugContainerAndCards()
    {
        Debug.Log("=== 🔍 VERIFICAÇÃO DO CONTAINER E CARDS ===");

        if (skillsContainer == null)
        {
            Debug.LogError("❌ skillsContainer é NULL!");
            return;
        }

        // Verificar container
        RectTransform containerRect = skillsContainer as RectTransform;
        if (containerRect != null)
        {
            Debug.Log($"📐 Container - Tamanho: {containerRect.sizeDelta}");
            Debug.Log($"📍 Container - Posição: {containerRect.anchoredPosition}");
            Debug.Log($"🎯 Container - Ativo: {containerRect.gameObject.activeInHierarchy}");
        }

        // Verificar Layout Group
        HorizontalLayoutGroup layout = skillsContainer.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
        {
            Debug.Log($"📏 Layout - Espaçamento: {layout.spacing}");
            Debug.Log($"🔧 Layout - Child Control: {layout.childControlWidth} x {layout.childControlHeight}");
        }

        // Verificar cards existentes
        int childCount = skillsContainer.childCount;
        Debug.Log($"🎴 Total de cards no container: {childCount}");

        for (int i = 0; i < childCount; i++)
        {
            Transform child = skillsContainer.GetChild(i);
            GameObject childObj = child.gameObject;

            Debug.Log($"\n🎴 Card {i}: {child.name}");
            Debug.Log($"   👀 Ativo: {childObj.activeSelf}");
            Debug.Log($"   🏷️ Tag: {childObj.tag}");
            Debug.Log($"   🎯 Layer: {childObj.layer}");

            // Verificar componentes
            RectTransform rect = child.GetComponent<RectTransform>();
            if (rect != null)
            {
                Debug.Log($"   📐 Tamanho: {rect.sizeDelta}");
                Debug.Log($"   📍 Posição: {rect.anchoredPosition}");
            }

            // Verificar se é prefab instance
#if UNITY_EDITOR
            bool isPrefabInstance = PrefabUtility.IsPartOfPrefabInstance(childObj);
            Debug.Log($"   📁 É Prefab Instance: {isPrefabInstance}");

            if (isPrefabInstance)
            {
                GameObject prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(childObj);
                Debug.Log($"   🔗 Prefab Original: {prefabSource?.name ?? "NONE"}");
            }
#endif

            // Verificar componentes de UI
            Image image = child.GetComponent<Image>();
            if (image != null)
            {
                Debug.Log($"   🎨 Image - Cor: {image.color}");
                Debug.Log($"   🖼️ Image - Sprite: {image.sprite?.name ?? "NULL"}");
            }

            Button button = child.GetComponent<Button>();
            if (button != null)
            {
                Debug.Log($"   🔘 Button - Interactable: {button.interactable}");
            }

            // Verificar textos nos filhos
            TextMeshProUGUI[] texts = child.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var text in texts)
            {
                Debug.Log($"   📝 {text.name}: '{text.text}' | Cor: {text.color}");
            }

            // Verificar imagens nos filhos
            Image[] images = child.GetComponentsInChildren<Image>();
            foreach (var img in images)
            {
                if (img.transform != child) // Não é o componente principal
                {
                    Debug.Log($"   🖼️ {img.name}: Sprite '{img.sprite?.name ?? "NULL"}' | Cor: {img.color}");
                }
            }
        }

        Debug.Log("=== FIM DA VERIFICAÇÃO ===");
    }
    [ContextMenu("🎯 Verificar Prefabs das Skills Atuais")]
    public void DebugCurrentSkillPrefabs()
    {
        if (currentChoices == null || currentChoices.Count == 0)
        {
            Debug.Log("ℹ️ Nenhuma skill carregada no momento");
            return;
        }

        Debug.Log("=== 🔍 VERIFICAÇÃO DE PREFABS DAS SKILLS ATUAIS ===");

        for (int i = 0; i < currentChoices.Count; i++)
        {
            SkillData skill = currentChoices[i];
            Debug.Log($"🎴 Skill {i}: {skill.skillName}");
            Debug.Log($"   📁 Prefab: {skill.cardPrefab?.name ?? "NULL"}");
            Debug.Log($"   🖼️ Ícone: {skill.icon?.name ?? "NULL"}");

            if (skill.cardPrefab == null)
            {
                Debug.LogWarning($"   ⚠️ Skill não tem cardPrefab! Usará fallback: {skillChoicePrefab?.name ?? "NENHUM"}");
            }
        }

        Debug.Log("=== FIM DA VERIFICAÇÃO ===");
    }
  
    void OnDestroy()
    {
        ResumeGame();
    }
}