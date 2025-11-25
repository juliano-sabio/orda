using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;

public class CardCreatorTool : EditorWindow
{
    [MenuItem("Tools/Skill System/🎯 Criar Prefab com Sistema de Instâncias")]
    public static void ShowWindow()
    {
        GetWindow<CardCreatorTool>("🎨 Criador com Sistema de Instâncias");
    }

    void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("🎯 Criar Prefab com Sistema de Instâncias", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Cria um card limpo - SkillCardInstance será adicionado automaticamente em runtime", EditorStyles.helpBox);

        GUILayout.Space(20);

        if (GUILayout.Button("🎯 CRIAR PREFAB LIMPO", GUILayout.Height(50)))
        {
            CreateCardWithInstanceSystem();
        }

        GUILayout.Space(10);

        EditorGUILayout.LabelField("Este sistema:", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField("• ✅ Prefab FICA INTOCADO - não modifica cores/textos", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("• ✅ SkillCardInstance adicionado AUTOMATICAMENTE", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("• ✅ Dados preenchidos apenas em RUNTIME", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("• ✅ 100% seguro para prefabs", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("• ✅ Sistema automático de referências", EditorStyles.miniLabel);
    }

    private void CreateCardWithInstanceSystem()
    {
        CreateCardsFolder();
        GameObject card = CreateCardStructureClean();
        SaveCardAsEditablePrefab(card, "SkillCard_Clean");

        Debug.Log("✅ PREFAB LIMPO CRIADO! O sistema adicionará SkillCardInstance automaticamente!");
    }

    private void CreateCardsFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        if (!AssetDatabase.IsValidFolder("Assets/Resources/Cards"))
            AssetDatabase.CreateFolder("Assets/Resources", "Cards");

        AssetDatabase.Refresh();
    }

    private GameObject CreateCardStructureClean()
    {
        // 🚫 SEM SkillCardController - só componentes básicos
        GameObject card = new GameObject("SkillCard_Clean",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement));
        // ✅ SkillCardInstance será adicionado AUTOMATICAMENTE em runtime

        SetupCardComponents(card);
        CreateAutoCardStructure(card);

        return card;
    }

    private void SetupCardComponents(GameObject card)
    {
        // RectTransform
        RectTransform rect = card.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(280, 380);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        // Image (Background)
        Image image = card.GetComponent<Image>();
        image.color = new Color(0.15f, 0.15f, 0.25f, 1f);
        image.type = Image.Type.Sliced;

        // Button
        Button button = card.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.15f, 0.15f, 0.25f, 1f);
        colors.highlightedColor = new Color(0.25f, 0.25f, 0.35f, 1f);
        colors.pressedColor = new Color(0.35f, 0.35f, 0.55f, 1f);
        colors.selectedColor = new Color(0.25f, 0.25f, 0.45f, 1f);
        button.colors = colors;

        // Layout Element
        LayoutElement layout = card.GetComponent<LayoutElement>();
        layout.preferredWidth = 280;
        layout.preferredHeight = 380;
        layout.flexibleWidth = 0;
        layout.flexibleHeight = 0;
    }

    private void CreateAutoCardStructure(GameObject card)
    {
        // Área do Ícone
        GameObject iconArea = CreateCardSection(card, "IconArea",
            new Vector2(0f, 0.75f), new Vector2(1f, 1f),
            new Vector2(10, 10), new Color(0.3f, 0.3f, 0.4f, 1f));

        // Slot da Imagem do Ícone
        GameObject iconSlot = CreateImageSlot(iconArea, "IconImageSlot",
            new Vector2(80, 80), Color.white);

        // Área do Nome
        GameObject nameArea = CreateCardSection(card, "NameArea",
            new Vector2(0f, 0.55f), new Vector2(1f, 0.75f),
            new Vector2(5, 2), Color.clear);

        GameObject nameText = CreateTextElement(nameArea, "NameText",
            "<b>NOME DA SKILL</b>", 16, Color.white, TextAlignmentOptions.Center);

        // Área da Descrição
        GameObject descArea = CreateCardSection(card, "DescArea",
            new Vector2(0f, 0.2f), new Vector2(1f, 0.55f),
            new Vector2(10, 5), Color.clear);

        GameObject descText = CreateTextElement(descArea, "DescText",
            "Descrição da skill aparecerá aqui automaticamente...",
            12, new Color(0.9f, 0.9f, 0.9f), TextAlignmentOptions.Center);

        // Área de Status
        GameObject statsArea = CreateCardSection(card, "StatsArea",
            new Vector2(0f, 0f), new Vector2(1f, 0.2f),
            new Vector2(5, 2), Color.clear);

        GameObject statsText = CreateTextElement(statsArea, "StatsText",
            "❤️ ⚔️ 🛡️ 🏃 💚",
            11, new Color(1f, 0.8f, 0.3f), TextAlignmentOptions.Center);

        // Área de Raridade
        GameObject rarityArea = CreateCardSection(card, "RarityArea",
            new Vector2(0.7f, 0.73f), new Vector2(0.95f, 0.77f),
            new Vector2(2, 2), Color.clear);

        GameObject rarityText = CreateTextElement(rarityArea, "RarityText",
            "RARE", 10, Color.yellow, TextAlignmentOptions.Center);

        // Borda de Raridade
        GameObject rarityBorder = CreateCardSection(card, "RarityBorder",
            new Vector2(0.68f, 0.71f), new Vector2(0.97f, 0.79f),
            new Vector2(0, 0), new Color(1f, 0.5f, 0f, 0.3f));
        rarityBorder.transform.SetAsFirstSibling();

        // Borda de Seleção
        CreateSelectionBorder(card);

        Debug.Log("✅ Estrutura do card criada - Pronta para SkillCardInstance automático!");
    }

    private GameObject CreateCardSection(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offset, Color color)
    {
        GameObject section = new GameObject(name, typeof(RectTransform));
        section.transform.SetParent(parent.transform);

        RectTransform rect = section.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = new Vector2(offset.x, offset.y);
        rect.offsetMax = new Vector2(-offset.x, -offset.y);
        rect.anchoredPosition = Vector2.zero;

        if (color != Color.clear)
        {
            Image bg = section.AddComponent<Image>();
            bg.color = color;
        }

        return section;
    }

    private GameObject CreateImageSlot(GameObject parent, string name, Vector2 size, Color color)
    {
        GameObject imageSlot = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageSlot.transform.SetParent(parent.transform);

        RectTransform rect = imageSlot.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        Image image = imageSlot.GetComponent<Image>();
        image.color = color;

        return imageSlot;
    }

    private GameObject CreateTextElement(GameObject parent, string name, string text, int fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(parent.transform);

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI textComp = textObj.GetComponent<TextMeshProUGUI>();
        textComp.text = text;
        textComp.color = color;
        textComp.fontSize = fontSize;
        textComp.alignment = alignment;
        textComp.fontStyle = FontStyles.Bold;
        textComp.textWrappingMode = TextWrappingModes.Normal;

        return textObj;
    }

    private void CreateSelectionBorder(GameObject card)
    {
        GameObject border = new GameObject("SelectionBorder", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(card.transform);
        border.transform.SetAsFirstSibling();

        RectTransform rect = border.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(-4, -4);
        rect.offsetMax = new Vector2(4, 4);

        Image borderImage = border.GetComponent<Image>();
        borderImage.color = new Color(1f, 0.9f, 0.2f, 0.4f);
        borderImage.type = Image.Type.Sliced;

        border.SetActive(false);
    }

    private void SaveCardAsEditablePrefab(GameObject card, string prefabName)
    {
        string prefabPath = $"Assets/Resources/Cards/{prefabName}.prefab";
        prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(card, prefabPath);

        if (prefab != null)
        {
            Selection.activeGameObject = card;
            Debug.Log($"✅ PREFAB LIMPO CRIADO: {prefabPath}");
            Debug.Log("🎯 SISTEMA DE INSTÂNCIAS:");
            Debug.Log("1. Use este prefab no campo 'cardPrefab' do SkillData");
            Debug.Log("2. SkillChoiceUI adicionará SkillCardInstance AUTOMATICAMENTE");
            Debug.Log("3. Dados preenchidos apenas em RUNTIME - Prefab SEGURO!");
            Debug.Log("4. Seu prefab original NUNCA será modificado!");
        }
        else
        {
            DestroyImmediate(card);
            Debug.LogError($"❌ Erro ao criar prefab: {prefabPath}");
        }
    }

    [MenuItem("Tools/Skill System/⚡ Criar Prefab Rápido Limpo")]
    public static void CreateQuickCleanPrefab()
    {
        CardCreatorTool creator = CreateInstance<CardCreatorTool>();
        creator.CreateQuickCleanPrefabInstance();
        DestroyImmediate(creator);
    }

    private void CreateQuickCleanPrefabInstance()
    {
        CreateCardsFolder();
        GameObject card = CreateCardStructureClean();
        SaveCardAsEditablePrefab(card, "SkillCard_Clean");
    }

    [MenuItem("Tools/Skill System/🧹 Verificar Prefabs Existentes")]
    public static void CheckExistingPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources/Cards" });

        if (prefabGuids.Length == 0)
        {
            Debug.Log("ℹ️ Nenhum prefab encontrado na pasta Cards");
            return;
        }

        Debug.Log($"🔍 Encontrados {prefabGuids.Length} prefabs:");

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
            {
                // Verifica se tem SkillCardController (pode não existir mais)
                var controllerType = System.Type.GetType("SkillCardController");
                bool hasController = controllerType != null && prefab.GetComponent(controllerType) != null;

                Debug.Log($"📁 {prefab.name}: Controller = {hasController}");
            }
        }

        Debug.Log("✅ Use 'Criar Prefab Limpo' para criar prefabs seguros!");
    }
}