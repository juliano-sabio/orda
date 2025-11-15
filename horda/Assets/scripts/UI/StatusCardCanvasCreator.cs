using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

public class StatusCardCanvasCreator : EditorWindow
{
    [MenuItem("Tools/UI Manager/Create Status Card System")]
    public static void CreateCompleteSystem()
    {
        Debug.Log("🚀 Criando Sistema Completo de Cards com Scriptable Objects...");

        // 1. Criar ou encontrar o Canvas
        GameObject canvas = CreateOrFindCanvas();

        // 2. Adicionar UIManager se não existir
        UIManager uiManager = SetupUIManager(canvas);

        // 3. Criar o painel de Cards
        CreateStatusCardPanel(canvas, uiManager);

        // 4. Criar o prefab do Card
        GameObject cardPrefab = CreateCardPrefab();

        // 5. Criar o StatusCardSystem
        CreateStatusCardSystem(canvas, uiManager, cardPrefab);

        // 6. Criar alguns Scriptable Objects de exemplo
        CreateExampleScriptableObjects();

        // 7. Configurar todas as referências
        SetupAllReferences(canvas, uiManager, cardPrefab);

        Debug.Log("✅ SISTEMA COMPLETO CRIADO COM SUCESSO!");
        Debug.Log("🎮 Pressione C para abrir o menu de Cards");
        Debug.Log("📈 Ganhe pontos subindo de nível (2, 4, 6...)");
        Debug.Log("🃏 Scriptable Objects de exemplo criados na pasta Assets/Data/StatusCards/");

        // Selecionar o sistema criado para fácil acesso
        Selection.activeGameObject = GameObject.Find("StatusCardSystem");
    }

    private static GameObject CreateOrFindCanvas()
    {
        // Procurar Canvas existente
        Canvas existingCanvas = Object.FindAnyObjectByType<Canvas>();
        if (existingCanvas != null)
        {
            Debug.Log("✅ Canvas encontrado: " + existingCanvas.gameObject.name);
            return existingCanvas.gameObject;
        }

        // Criar novo Canvas
        Debug.Log("📝 Criando novo Canvas...");
        GameObject canvasGO = new GameObject("UIManager_Canvas");

        // Canvas
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Canvas Scaler
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Graphic Raycaster
        canvasGO.AddComponent<GraphicRaycaster>();

        Debug.Log("✅ Novo Canvas criado!");
        return canvasGO;
    }

    private static UIManager SetupUIManager(GameObject canvas)
    {
        UIManager uiManager = canvas.GetComponent<UIManager>();
        if (uiManager == null)
        {
            uiManager = canvas.AddComponent<UIManager>();
            Debug.Log("✅ UIManager adicionado ao Canvas");
        }
        else
        {
            Debug.Log("✅ UIManager já existe no Canvas");
        }
        return uiManager;
    }

    private static void CreateStatusCardPanel(GameObject canvas, UIManager uiManager)
    {
        // Remover painel existente se houver
        Transform oldPanel = canvas.transform.Find("StatusCardPanel");
        if (oldPanel != null)
        {
            DestroyImmediate(oldPanel.gameObject);
            Debug.Log("🗑️ Painel antigo removido");
        }

        // Criar o painel principal
        GameObject panel = new GameObject("StatusCardPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform);
        panel.SetActive(false);

        // Configurar RectTransform
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(900, 600);
        rect.anchoredPosition = Vector2.zero;

        // Background
        Image bg = panel.GetComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.95f);
        bg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        bg.type = Image.Type.Sliced;

        // Título
        CreateTextElement(panel, "Title", "🃏 CARTAS DE STATUS",
                         new Vector2(0, 250), new Vector2(600, 60),
                         Color.yellow, 32, FontStyles.Bold, TextAlignmentOptions.Center);

        // Pontos de Status
        CreateTextElement(panel, "StatusPointsText", "🎯 Pontos Disponíveis: 0",
                         new Vector2(300, 200), new Vector2(300, 40),
                         Color.white, 18, FontStyles.Bold, TextAlignmentOptions.Right);

        // Container dos Cards
        GameObject container = new GameObject("StatusCardContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        container.transform.SetParent(panel.transform);

        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(800, 300);
        containerRect.anchoredPosition = new Vector2(0, -50);

        // Layout dos Cards
        HorizontalLayoutGroup layout = container.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 20;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        // Bônus Ativos
        CreateTextElement(panel, "ActiveBonusesText", "✅ BÔNUS ATIVOS:\nNenhum",
                         new Vector2(-350, 0), new Vector2(250, 300),
                         Color.green, 14, FontStyles.Normal, TextAlignmentOptions.TopLeft);

        // Botão Fechar
        CreateCloseButton(panel);

        Debug.Log("✅ Painel de Cards criado!");
    }

    private static GameObject CreateCardPrefab()
    {
        // Verificar se já existe
        string[] prefabGuids = AssetDatabase.FindAssets("StatusCardPrefab");
        if (prefabGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[0]);
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existingPrefab != null)
            {
                Debug.Log("✅ Prefab encontrado: " + path);
                return existingPrefab;
            }
        }

        Debug.Log("📝 Criando novo Prefab de Card...");

        // Criar GameObject para o card
        GameObject card = new GameObject("StatusCardPrefab",
                                        typeof(RectTransform),
                                        typeof(Image),
                                        typeof(CanvasGroup),
                                        typeof(StatusCardUI));

        // Configurações básicas
        RectTransform rect = card.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(220, 280);

        Image bg = card.GetComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        bg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        bg.type = Image.Type.Sliced;

        // Canvas Group para efeitos de interação
        CanvasGroup canvasGroup = card.GetComponent<CanvasGroup>();
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // Background da raridade
        GameObject rarityBG = new GameObject("RarityBackground", typeof(RectTransform), typeof(Image));
        rarityBG.transform.SetParent(card.transform);
        RectTransform rarityRect = rarityBG.GetComponent<RectTransform>();
        rarityRect.anchorMin = new Vector2(0, 0.7f);
        rarityRect.anchorMax = new Vector2(1, 1);
        rarityRect.sizeDelta = Vector2.zero;
        rarityRect.anchoredPosition = Vector2.zero;
        rarityBG.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 0.8f);

        // Ícone do Card (agora suporta Sprite dos Scriptable Objects)
        GameObject icon = new GameObject("CardIcon", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(card.transform);
        RectTransform iconRect = icon.GetComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(80, 80);
        iconRect.anchoredPosition = new Vector2(0, 70);
        icon.GetComponent<Image>().color = Color.white;

        // Nome do Card
        GameObject nameText = CreateTextElement(card, "CardName", "Nome do Card",
                                               new Vector2(0, 30), new Vector2(200, 30),
                                               Color.white, 16, FontStyles.Bold, TextAlignmentOptions.Center);

        // Descrição
        GameObject descText = CreateTextElement(card, "Description", "Descrição do card que pode ser mais longa",
                                               new Vector2(0, -20), new Vector2(200, 60),
                                               new Color(0.8f, 0.8f, 0.8f), 12, FontStyles.Normal, TextAlignmentOptions.Top);

        // Painel de informações (rodapé)
        GameObject infoPanel = new GameObject("InfoPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        infoPanel.transform.SetParent(card.transform);
        RectTransform infoRect = infoPanel.GetComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0, 0);
        infoRect.anchorMax = new Vector2(1, 0.3f);
        infoRect.sizeDelta = Vector2.zero;
        infoRect.anchoredPosition = Vector2.zero;

        Image infoBg = infoPanel.GetComponent<Image>();
        infoBg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        VerticalLayoutGroup infoLayout = infoPanel.GetComponent<VerticalLayoutGroup>();
        infoLayout.padding = new RectOffset(5, 5, 5, 5);
        infoLayout.spacing = 2;
        infoLayout.childAlignment = TextAnchor.MiddleCenter;

        // Linha 1: Custo e Raridade
        GameObject row1 = new GameObject("Row1", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row1.transform.SetParent(infoPanel.transform);
        SetupInfoRow(row1);

        CreateTextElement(row1, "Cost", "💎 Custo: 1",
                         Vector2.zero, new Vector2(90, 20),
                         Color.yellow, 11, FontStyles.Bold, TextAlignmentOptions.Left);

        CreateTextElement(row1, "Rarity", "Comum",
                         Vector2.zero, new Vector2(90, 20),
                         Color.white, 11, FontStyles.Bold, TextAlignmentOptions.Right);

        // Linha 2: Nível Requerido
        GameObject row2 = new GameObject("Row2", typeof(RectTransform));
        row2.transform.SetParent(infoPanel.transform);
        CreateTextElement(row2, "LevelReq", "📊 Nível: 1",
                         Vector2.zero, new Vector2(180, 18),
                         Color.cyan, 10, FontStyles.Normal, TextAlignmentOptions.Center);

        // Botão de seleção (cobre todo o card)
        GameObject button = new GameObject("SelectButton", typeof(RectTransform), typeof(Image), typeof(Button));
        button.transform.SetParent(card.transform);
        RectTransform buttonRect = button.GetComponent<RectTransform>();
        buttonRect.anchorMin = Vector2.zero;
        buttonRect.anchorMax = Vector2.one;
        buttonRect.sizeDelta = Vector2.zero;
        buttonRect.anchoredPosition = Vector2.zero;

        Image buttonImage = button.GetComponent<Image>();
        buttonImage.color = new Color(0, 0, 0, 0); // Transparente

        Button buttonComp = button.GetComponent<Button>();
        ColorBlock colors = buttonComp.colors;
        colors.normalColor = new Color(1, 1, 1, 0);
        colors.highlightedColor = new Color(1, 1, 1, 0.1f);
        colors.pressedColor = new Color(1, 1, 1, 0.2f);
        buttonComp.colors = colors;

        // Configurar StatusCardUI
        StatusCardUI cardUI = card.GetComponent<StatusCardUI>();
        cardUI.cardNameText = nameText.GetComponent<TextMeshProUGUI>();
        cardUI.descriptionText = descText.GetComponent<TextMeshProUGUI>();
        cardUI.costText = row1.transform.Find("Cost").GetComponent<TextMeshProUGUI>();
        cardUI.rarityText = row1.transform.Find("Rarity").GetComponent<TextMeshProUGUI>();
        cardUI.levelRequirementText = row2.transform.Find("LevelReq").GetComponent<TextMeshProUGUI>();
        cardUI.cardBackground = bg;
        cardUI.cardIcon = icon.GetComponent<Image>(); // Nova referência para o ícone
        cardUI.selectButton = button.GetComponent<Button>();

        // Salvar prefab
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        string prefabPath = "Assets/Resources/StatusCardPrefab.prefab";
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(card, prefabPath);

        if (savedPrefab != null)
        {
            Debug.Log("✅ Prefab salvo: " + prefabPath);
            DestroyImmediate(card);
            return savedPrefab;
        }
        else
        {
            Debug.LogError("❌ Erro ao salvar prefab!");
            return card;
        }
    }

    private static void CreateStatusCardSystem(GameObject canvas, UIManager uiManager, GameObject cardPrefab)
    {
        // Remover sistema existente
        StatusCardSystem oldSystem = Object.FindAnyObjectByType<StatusCardSystem>();
        if (oldSystem != null)
        {
            DestroyImmediate(oldSystem.gameObject);
            Debug.Log("🗑️ Sistema antigo removido");
        }

        // Criar novo sistema
        GameObject systemGO = new GameObject("StatusCardSystem");
        StatusCardSystem system = systemGO.AddComponent<StatusCardSystem>();

        // Configurar referências básicas
        if (cardPrefab != null)
        {
            system.statusCardPrefab = cardPrefab;
        }

        // REMOVIDO: DontDestroyOnLoad não pode ser usado no Editor
        // DontDestroyOnLoad(systemGO);

        Debug.Log("✅ StatusCardSystem criado!");
    }

    private static void CreateExampleScriptableObjects()
    {
        // Criar pasta para os Scriptable Objects
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder("Assets/Data/StatusCards"))
            AssetDatabase.CreateFolder("Assets/Data", "StatusCards");

        // Criar alguns cards de exemplo
        CreateStatusCardSO("Card_Vida_Basico", "Vigor Básico", "Aumenta vida máxima em 15",
                          StatusCardType.Health, CardRarity.Common, 15f, 0, 1, 1);

        CreateStatusCardSO("Card_Ataque_Basico", "Força Básica", "Aumenta ataque em 5",
                          StatusCardType.Attack, CardRarity.Common, 5f, 0, 1, 1);

        CreateStatusCardSO("Card_Defesa_Avancada", "Escudo Avançado", "Aumenta defesa em 6",
                          StatusCardType.Defense, CardRarity.Rare, 6f, 0, 2, 3);

        CreateStatusCardSO("Card_Velocidade_Rara", "Agilidade Avançada", "Aumenta velocidade em 2",
                          StatusCardType.Speed, CardRarity.Rare, 2f, 0, 2, 4);

        Debug.Log("✅ Scriptable Objects de exemplo criados!");
    }

    private static void CreateStatusCardSO(string fileName, string cardName, string description,
                                         StatusCardType type, CardRarity rarity, float statBonus,
                                         float secondaryBonus, int cost, int requiredLevel)
    {
        StatusCardData card = ScriptableObject.CreateInstance<StatusCardData>();
        card.cardName = cardName;
        card.description = description;
        card.cardType = type;
        card.rarity = rarity;
        card.statBonus = statBonus;
        card.secondaryBonus = secondaryBonus;
        card.cost = cost;
        card.requiredLevel = requiredLevel;

        string path = $"Assets/Data/StatusCards/{fileName}.asset";
        AssetDatabase.CreateAsset(card, path);
        Debug.Log($"🃏 {cardName} criado em: {path}");
    }

    private static void SetupAllReferences(GameObject canvas, UIManager uiManager, GameObject cardPrefab)
    {
        // Encontrar componentes no Canvas
        Transform panel = canvas.transform.Find("StatusCardPanel");
        Transform container = panel?.Find("StatusCardContainer");
        Transform pointsText = panel?.Find("StatusPointsText");
        Transform bonusesText = panel?.Find("ActiveBonusesText");

        // Configurar UIManager
        if (panel != null) uiManager.statusCardPanel = panel.gameObject;
        if (container != null) uiManager.statusCardContainer = container;
        if (pointsText != null) uiManager.statusPointsText = pointsText.GetComponent<TextMeshProUGUI>();
        if (bonusesText != null) uiManager.activeBonusesText = bonusesText.GetComponent<TextMeshProUGUI>();
        if (cardPrefab != null) uiManager.statusCardPrefab = cardPrefab;

        // Configurar StatusCardSystem
        StatusCardSystem system = Object.FindAnyObjectByType<StatusCardSystem>();
        if (system != null && panel != null)
        {
            system.cardChoicePanel = panel.gameObject;
            system.cardsContainer = container;
            system.statusCardPrefab = cardPrefab;

            // Carregar automaticamente os Scriptable Objects criados
            string[] cardGuids = AssetDatabase.FindAssets("t:StatusCardData", new[] { "Assets/Data/StatusCards" });
            system.allStatusCards.Clear();

            foreach (string guid in cardGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                StatusCardData card = AssetDatabase.LoadAssetAtPath<StatusCardData>(path);
                if (card != null)
                {
                    system.allStatusCards.Add(card);
                }
            }

            Debug.Log($"✅ {system.allStatusCards.Count} Scriptable Objects carregados no sistema");
        }

        Debug.Log("✅ Todas as referências configuradas!");
    }

    // Método auxiliar para criar textos (COM CORREÇÃO DA PROPRIEDADE OBSOLETA)
    private static GameObject CreateTextElement(GameObject parent, string name, string text,
                                               Vector2 position, Vector2 size,
                                               Color color, int fontSize, FontStyles fontStyle,
                                               TextAlignmentOptions alignment)
    {
        GameObject textGO = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(parent.transform);

        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        TextMeshProUGUI textComp = textGO.GetComponent<TextMeshProUGUI>();
        textComp.text = text;
        textComp.color = color;
        textComp.fontSize = fontSize;
        textComp.fontStyle = fontStyle;
        textComp.alignment = alignment;

        // CORREÇÃO: Substituir enableWordWrapping por textWrappingMode
        textComp.textWrappingMode = TextWrappingModes.Normal; // Nova propriedade

        return textGO;
    }

    private static void SetupInfoRow(GameObject row)
    {
        RectTransform rect = row.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(180, 20);

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private static void CreateCloseButton(GameObject panel)
    {
        GameObject closeButton = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeButton.transform.SetParent(panel.transform);

        RectTransform rect = closeButton.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.95f, 0.95f);
        rect.anchorMax = new Vector2(0.95f, 0.95f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(30, 30);
        rect.anchoredPosition = Vector2.zero;

        Image bg = closeButton.GetComponent<Image>();
        bg.color = Color.red;
        bg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // Adicionar texto "X"
        GameObject text = CreateTextElement(closeButton, "Text", "X",
                                           Vector2.zero, new Vector2(30, 30),
                                           Color.white, 14, FontStyles.Bold, TextAlignmentOptions.Center);

        Button button = closeButton.GetComponent<Button>();

        // Configurar o onClick via script - será configurado em runtime
        // Não podemos adicionar listeners no Editor para objetos em runtime
    }

    [MenuItem("Tools/UI Manager/Test System")]
    public static void TestSystem()
    {
        // Este teste só funciona em Play Mode
        if (Application.isPlaying)
        {
            StatusCardSystem system = Object.FindAnyObjectByType<StatusCardSystem>();
            if (system != null)
            {
                system.AddTestPoints();
                system.ForceCardChoice();
                Debug.Log("🧪 Sistema testado - Cards devem aparecer!");
            }
            else
            {
                Debug.LogError("❌ Sistema não encontrado! Execute o setup primeiro.");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ O teste do sistema só funciona em Play Mode!");
            Debug.Log("🎮 Entre em Play Mode e pressione C para testar o sistema de cards");
        }
    }

    [MenuItem("Tools/UI Manager/Create More Example Cards")]
    public static void CreateMoreExamples()
    {
        CreateStatusCardSO("Card_Regen_Basico", "Cura Básica", "Aumenta regeneração de vida em 0.5",
                          StatusCardType.Regen, CardRarity.Common, 0.5f, 0, 1, 2);

        CreateStatusCardSO("Card_Ataque_Epico", "Fúria do Guerreiro", "Aumenta ataque em 15 e velocidade em 10%",
                          StatusCardType.Attack, CardRarity.Epic, 15f, 0.1f, 3, 5);

        CreateStatusCardSO("Card_Defesa_Lendaria", "Barreira Impenetrável", "Aumenta defesa em 10 e reduz dano em 5%",
                          StatusCardType.Defense, CardRarity.Legendary, 10f, 0.05f, 4, 8);

        AssetDatabase.Refresh();
        Debug.Log("✅ Cards adicionais criados!");
    }

    [MenuItem("Tools/UI Manager/Add DontDestroyOnLoad Script")]
    public static void AddDontDestroyScript()
    {
        // Adiciona um script component que aplica DontDestroyOnLoad em Play Mode
        StatusCardSystem system = Object.FindAnyObjectByType<StatusCardSystem>();
        if (system != null && system.gameObject.GetComponent<DontDestroyOnLoadComponent>() == null)
        {
            system.gameObject.AddComponent<DontDestroyOnLoadComponent>();
            Debug.Log("✅ Componente DontDestroyOnLoad adicionado (será ativado em Play Mode)");
        }
    }
}

// Script auxiliar para aplicar DontDestroyOnLoad apenas em Play Mode
public class DontDestroyOnLoadComponent : MonoBehaviour
{
    void Awake()
    {
        // Só aplica DontDestroyOnLoad durante o Play Mode
        if (Application.isPlaying)
        {
            DontDestroyOnLoad(gameObject);
            Debug.Log("🔄 StatusCardSystem marcado como DontDestroyOnLoad");
        }
    }
}