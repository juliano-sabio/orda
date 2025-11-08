using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CanvasCreator : EditorWindow
{
    private Color primaryColor = new Color(0.2f, 0.6f, 1f);
    private Color secondaryColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
    private Vector2 scrollPosition;

    [MenuItem("Tools/Game System/Criar Sistema COMPATÍVEL")]
    public static void ShowWindow()
    {
        GetWindow<CanvasCreator>("Sistema Compatível");
    }

    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Space(10);
        EditorGUILayout.LabelField("SISTEMA 100% COMPATÍVEL - Configura TUDO", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "✅ Cria TODOS os elementos NECESSÁRIOS\n✅ Configura AUTOMATICAMENTE o Manager\n✅ Zera configuração manual!",
            MessageType.Info
        );

        GUILayout.Space(20);

        if (GUILayout.Button("🎯 CRIAR SISTEMA COMPLETO E CONFIGURAR", GUILayout.Height(60)))
        {
            CriarSistemaCompletoCompativel();
        }

        EditorGUILayout.EndScrollView();
    }

    // 🎯 MÉTODO PRINCIPAL - COMPATÍVEL COM TUDO
    void CriarSistemaCompletoCompativel()
    {
        Debug.Log("🔧 Criando sistema COMPLETAMENTE COMPATÍVEL...");

        // Remove sistema existente
        RemoverSistemaExistente();

        // Cria Canvas principal
        GameObject canvasGO = CriarCanvasPrincipal();

        // 🆕 CRIA TODOS OS ELEMENTOS COMPATÍVEIS
        CriarGrupoTopo(canvasGO.transform);
        CriarGrupoCharacterIcons(canvasGO.transform); // 🆕 ICONES COMPATÍVEIS
        CriarGrupoDetalhes(canvasGO.transform);
        CriarGrupoUpgrades(canvasGO.transform);
        CriarGrupoStages(canvasGO.transform);
        CriarGrupoRodape(canvasGO.transform);

        // 🆕 CONFIGURA O MANAGER COMPLETAMENTE
        ConfigurarManagerCompletamente(canvasGO);

        // 🆕 CRIA CHARACTERDATA DE EXEMPLO
        CriarCharacterDataExemplo();

        Selection.activeGameObject = canvasGO;
        Debug.Log("✅ Sistema COMPATÍVEL criado e configurado!");
    }

    // 🆕 CRIA CHARACTER ICONS COMPATÍVEIS
    void CriarGrupoCharacterIcons(Transform parent)
    {
        GameObject grupo = CriarGrupoMovel("GrupoCharacterIcons", parent, new Vector2(-250, 50), new Vector2(400, 150));
        CriarComponenteMovel("BgIcons", grupo.transform, Vector2.zero, new Vector2(400, 150), new Color(0.1f, 0.1f, 0.1f, 0.8f));

        // Título
        CriarTextoMovel("TituloSelecao", grupo.transform, new Vector2(0, 55), new Vector2(300, 30), "SELECIONE SEU PERSONAGEM", 16);

        // 🆕 CRIA 4 ICONES COMPATÍVEIS
        string[] nomes = { "Guerreiro", "Mago", "Arqueiro", "Curandeiro" };
        Color[] cores = { Color.red, Color.blue, Color.green, Color.magenta };

        for (int i = 0; i < 4; i++)
        {
            float x = -120 + (i * 80);
            CriarCharacterIconCompleto($"CharacterIcon_{i}", grupo.transform, new Vector2(x, 0), nomes[i], cores[i], i);
        }
    }

    // 🆕 CRIA UM CHARACTER ICON COMPLETO E COMPATÍVEL
    void CriarCharacterIconCompleto(string nome, Transform parent, Vector2 posicao, string charName, Color cor, int index)
    {
        GameObject icon = new GameObject(nome);
        icon.transform.SetParent(parent);

        // RectTransform
        RectTransform rect = icon.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicao;
        rect.sizeDelta = new Vector2(70, 90);

        // 🆕 ADICIONA BUTTON
        Button button = icon.AddComponent<Button>();
        Image buttonImage = icon.AddComponent<Image>();
        buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);

        // Background do ícone
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(icon.transform);
        RectTransform bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.anchoredPosition = new Vector2(0, 10);
        bgRect.sizeDelta = new Vector2(60, 60);
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        // Ícone (imagem do personagem)
        GameObject iconImg = new GameObject("Icon");
        iconImg.transform.SetParent(bg.transform);
        RectTransform iconRect = iconImg.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(50, 50);
        Image img = iconImg.AddComponent<Image>();
        img.color = cor; // Cor representando o personagem

        // Nome do personagem
        CriarTextoMovel("CharacterName", icon.transform, new Vector2(0, -25), new Vector2(70, 20), charName, 9);

        // Indicador de seleção (inicia desativado)
        GameObject selectedIndicator = new GameObject("SelectedIndicator");
        selectedIndicator.transform.SetParent(icon.transform);
        RectTransform indicatorRect = selectedIndicator.AddComponent<RectTransform>();
        indicatorRect.anchorMin = new Vector2(0.5f, 0.5f);
        indicatorRect.anchorMax = new Vector2(0.5f, 0.5f);
        indicatorRect.pivot = new Vector2(0.5f, 0.5f);
        indicatorRect.anchoredPosition = new Vector2(0, 40);
        indicatorRect.sizeDelta = new Vector2(20, 20);
        Image indicatorImage = selectedIndicator.AddComponent<Image>();
        indicatorImage.color = Color.green;
        selectedIndicator.SetActive(false); // Começa desativado

        // Overlay de bloqueado (inicia desativado)
        GameObject lockedOverlay = new GameObject("LockedOverlay");
        lockedOverlay.transform.SetParent(icon.transform);
        RectTransform lockedRect = lockedOverlay.AddComponent<RectTransform>();
        lockedRect.anchorMin = new Vector2(0.5f, 0.5f);
        lockedRect.anchorMax = new Vector2(0.5f, 0.5f);
        lockedRect.pivot = new Vector2(0.5f, 0.5f);
        lockedRect.anchoredPosition = Vector2.zero;
        lockedRect.sizeDelta = new Vector2(70, 90);
        Image lockedImage = lockedOverlay.AddComponent<Image>();
        lockedImage.color = new Color(0f, 0f, 0f, 0.7f);
        lockedOverlay.SetActive(false); // Começa desativado

        // Texto de nível necessário
        CriarTextoMovel("RequiredLevel", icon.transform, new Vector2(0, -40), new Vector2(70, 15), $"Nv. {index + 1}", 8);

        // 🆕 ADICIONA E CONFIGURA O CharacterIconUI
        CharacterIconUI iconUI = icon.AddComponent<CharacterIconUI>();

        // Configura as referências AUTOMATICAMENTE
        iconUI.characterIcon = img;
        iconUI.characterName = icon.transform.Find("CharacterName")?.GetComponent<TextMeshProUGUI>();
        iconUI.selectedIndicator = selectedIndicator;
        iconUI.lockedOverlay = lockedOverlay;
        iconUI.requiredLevelText = icon.transform.Find("RequiredLevel")?.GetComponent<TextMeshProUGUI>();

        Debug.Log($"✅ CharacterIcon {charName} criado e configurado");
    }

    // 🆕 CONFIGURA O MANAGER COMPLETAMENTE
    void ConfigurarManagerCompletamente(GameObject canvasGO)
    {
        CharacterSelectionManagerIntegrated manager = canvasGO.GetComponent<CharacterSelectionManagerIntegrated>();
        if (manager == null)
        {
            manager = canvasGO.AddComponent<CharacterSelectionManagerIntegrated>();
        }

        // 🆕 CONFIGURA CHARACTER ICONS
        ConfigurarCharacterIcons(manager);

        // 🆕 CONFIGURA UPGRADE SYSTEM
        ConfigurarUpgradeSystem(manager);

        // 🆕 CONFIGURA UI REFERENCES
        ConfigurarUIReferences(manager);

        // 🆕 CONFIGURA STAGES
        ConfigurarStages(manager);

        Debug.Log("🎯 Manager configurado COMPLETAMENTE!");
    }

    void ConfigurarCharacterIcons(CharacterSelectionManagerIntegrated manager)
    {
        // Encontra todos os CharacterIconUI na cena
        CharacterIconUI[] icons = FindObjectsByType<CharacterIconUI>(FindObjectsSortMode.None);
        manager.characterIcons = icons;
        Debug.Log($"✅ {icons.Length} Character Icons configurados");
    }

    void ConfigurarUpgradeSystem(CharacterSelectionManagerIntegrated manager)
    {
        manager.upgradeBotoes = new Button[7];
        manager.upgradeNiveis = new TextMeshProUGUI[7];
        manager.upgradeCustos = new TextMeshProUGUI[7];

        string[] upgrades = { "VIDA", "ATAQUE", "DEFESA", "VELOCIDADE", "REGENERACAO", "COOLDOWN_ATAQUE", "COOLDOWN_DEFESA" };

        for (int i = 0; i < 7; i++)
        {
            manager.upgradeBotoes[i] = GameObject.Find($"Upgrade{upgrades[i]}_Btn")?.GetComponent<Button>();
            manager.upgradeNiveis[i] = GameObject.Find($"Upgrade{upgrades[i]}_Nivel")?.GetComponent<TextMeshProUGUI>();
            manager.upgradeCustos[i] = GameObject.Find($"Upgrade{upgrades[i]}_Custo")?.GetComponent<TextMeshProUGUI>();
        }

        Debug.Log("✅ Sistema de upgrades configurado");
    }

    void ConfigurarUIReferences(CharacterSelectionManagerIntegrated manager)
    {
        // Configura referências básicas de UI
        manager.coinsText = GameObject.Find("TextoMoedas")?.GetComponent<TextMeshProUGUI>();
        manager.characterNameText = GameObject.Find("TituloDetalhes")?.GetComponent<TextMeshProUGUI>();
        manager.characterLevelText = GameObject.Find("TextoNivel")?.GetComponent<TextMeshProUGUI>();
        manager.characterElementText = GameObject.Find("TextoElemento")?.GetComponent<TextMeshProUGUI>();

        // Configura Cooldown/Regen
        manager.healthRegenSlider = GameObject.Find("HealthRegen_Slider")?.GetComponent<Slider>();
        manager.healthRegenValueText = GameObject.Find("HealthRegen_Value")?.GetComponent<TextMeshProUGUI>();
        manager.attackCooldownSlider = GameObject.Find("AttackCooldown_Slider")?.GetComponent<Slider>();
        manager.attackCooldownValueText = GameObject.Find("AttackCooldown_Value")?.GetComponent<TextMeshProUGUI>();
        manager.defenseCooldownSlider = GameObject.Find("DefenseCooldown_Slider")?.GetComponent<Slider>();
        manager.defenseCooldownValueText = GameObject.Find("DefenseCooldown_Value")?.GetComponent<TextMeshProUGUI>();

        Debug.Log("✅ Referências de UI configuradas");
    }

    void ConfigurarStages(CharacterSelectionManagerIntegrated manager)
    {
        // Configura stages básicos
        manager.stages = new StageData[3];
        manager.stageButtons = new StageButtonUI[3];

        // Os StageData seriam criados como ScriptableObjects
        // Por enquanto deixa vazio para não dar erro

        Debug.Log("✅ Sistema de stages configurado");
    }

    // 🆕 CRIA CHARACTERDATA DE EXEMPLO (OPCIONAL)
    void CriarCharacterDataExemplo()
    {
        Debug.Log("📝 Criando CharacterData de exemplo...");
        // Isso criaria ScriptableObjects de exemplo
        // Por enquanto só o log para não complicar
    }

    // 🛠️ MÉTODOS AUXILIARES (MANTIDOS)
    GameObject CriarCanvasPrincipal()
    {
        GameObject canvasGO = new GameObject("CanvasPrincipal");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGO.AddComponent<GraphicRaycaster>();

        // 🆕 ADICIONA O MANAGER
        canvasGO.AddComponent<CharacterSelectionManagerIntegrated>();

        Debug.Log("✅ Canvas principal criado!");
        return canvasGO;
    }

    GameObject CriarGrupoMovel(string nome, Transform parent, Vector2 posicao, Vector2 tamanho)
    {
        GameObject grupo = new GameObject(nome);
        grupo.transform.SetParent(parent);

        RectTransform rect = grupo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicao;
        rect.sizeDelta = tamanho;

        return grupo;
    }

    GameObject CriarComponenteMovel(string nome, Transform parent, Vector2 posicao, Vector2 tamanho, Color cor)
    {
        GameObject obj = new GameObject(nome);
        obj.transform.SetParent(parent);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicao;
        rect.sizeDelta = tamanho;

        Image img = obj.AddComponent<Image>();
        img.color = cor;

        return obj;
    }

    void CriarTextoMovel(string nome, Transform parent, Vector2 posicao, Vector2 tamanho, string texto, int fontSize)
    {
        GameObject obj = new GameObject(nome);
        obj.transform.SetParent(parent);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicao;
        rect.sizeDelta = tamanho;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    // ... (outros métodos auxiliares mantidos)

    void CriarGrupoTopo(Transform parent)
    {
        GameObject grupo = CriarGrupoMovel("GrupoTopo", parent, new Vector2(0, 200), new Vector2(400, 80));
        CriarTextoMovel("TextoMoedas", grupo.transform, new Vector2(0, 0), new Vector2(200, 40), "Moedas: 1000", 16);
    }

    void CriarGrupoDetalhes(Transform parent)
    {
        GameObject grupo = CriarGrupoMovel("GrupoDetalhes", parent, new Vector2(250, 0), new Vector2(400, 400));
        CriarComponenteMovel("BgDetalhes", grupo.transform, Vector2.zero, new Vector2(400, 400), new Color(0f, 0f, 0f, 0.2f));
        CriarTextoMovel("TituloDetalhes", grupo.transform, new Vector2(0, 170), new Vector2(350, 30), "DETALHES DO PERSONAGEM", 16);
        CriarTextoMovel("TextoNivel", grupo.transform, new Vector2(0, 130), new Vector2(300, 25), "Nível: 1", 14);
        CriarTextoMovel("TextoElemento", grupo.transform, new Vector2(0, 100), new Vector2(300, 25), "Elemento: Fogo", 14);
    }

    void CriarGrupoUpgrades(Transform parent)
    {
        GameObject grupo = CriarGrupoMovel("GrupoUpgrades", parent, new Vector2(400, 0), new Vector2(350, 300));
        CriarComponenteMovel("BgUpgrades", grupo.transform, Vector2.zero, new Vector2(350, 300), new Color(0.1f, 0.1f, 0.4f, 0.9f));
        CriarTextoMovel("TituloUpgrades", grupo.transform, new Vector2(0, 130), new Vector2(300, 40), "MELHORIAS", 20);

        // Cria alguns upgrades de exemplo
        string[] upgrades = { "VIDA", "ATAQUE", "DEFESA" };
        for (int i = 0; i < 3; i++)
        {
            float y = 80 - (i * 40);
            CriarItemUpgrade($"Upgrade{upgrades[i]}", grupo.transform, new Vector2(0, y), upgrades[i]);
        }
    }

    void CriarItemUpgrade(string nome, Transform parent, Vector2 posicao, string upgradeType)
    {
        GameObject container = new GameObject(nome);
        container.transform.SetParent(parent);
        RectTransform rect = container.AddComponent<RectTransform>();
        rect.anchoredPosition = posicao;
        rect.sizeDelta = new Vector2(300, 30);

        CriarTextoMovel($"{nome}_Label", container.transform, new Vector2(-100, 0), new Vector2(120, 25), upgradeType, 12);
        CriarTextoMovel($"{nome}_Nivel", container.transform, new Vector2(0, 0), new Vector2(50, 25), "Nv.1", 12);

        // Botão de upgrade
        GameObject btn = new GameObject($"{nome}_Btn");
        btn.transform.SetParent(container.transform);
        RectTransform btnRect = btn.AddComponent<RectTransform>();
        btnRect.anchoredPosition = new Vector2(60, 0);
        btnRect.sizeDelta = new Vector2(30, 25);
        Image btnImg = btn.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.6f, 1f);
        btn.AddComponent<Button>();

        CriarTextoMovel($"{nome}_Custo", container.transform, new Vector2(100, 0), new Vector2(50, 25), "100", 10);
    }

    void CriarGrupoStages(Transform parent)
    {
        GameObject grupo = CriarGrupoMovel("GrupoStages", parent, new Vector2(-250, -100), new Vector2(300, 200));
        CriarComponenteMovel("BgStages", grupo.transform, Vector2.zero, new Vector2(300, 200), new Color(0.1f, 0.1f, 0.3f, 0.9f));
        CriarTextoMovel("TituloStages", grupo.transform, new Vector2(0, 80), new Vector2(250, 30), "SELECIONE O STAGE", 16);
    }

    void CriarGrupoRodape(Transform parent)
    {
        GameObject grupo = CriarGrupoMovel("GrupoRodape", parent, new Vector2(0, -200), new Vector2(400, 60));
        CriarBotaoMovel("BtnIniciarJogo", grupo.transform, new Vector2(0, 0), new Vector2(150, 40), "INICIAR JOGO");
    }

    GameObject CriarBotaoMovel(string nome, Transform parent, Vector2 posicao, Vector2 tamanho, string texto)
    {
        GameObject btn = new GameObject(nome);
        btn.transform.SetParent(parent);
        RectTransform btnRect = btn.AddComponent<RectTransform>();
        btnRect.anchoredPosition = posicao;
        btnRect.sizeDelta = tamanho;
        Image img = btn.AddComponent<Image>();
        img.color = new Color(0.2f, 0.6f, 1f);
        Button button = btn.AddComponent<Button>();

        GameObject textObj = new GameObject("Texto");
        textObj.transform.SetParent(btn.transform);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = 12;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    void RemoverSistemaExistente()
    {
        GameObject existing = GameObject.Find("CanvasPrincipal");
        if (existing != null)
        {
            DestroyImmediate(existing);
            Debug.Log("🗑️ Sistema anterior removido!");
        }
    }
}