using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CanvasCreator : EditorWindow
{
    [MenuItem("Tools/Game System/Criar Sistema MESCLADO")]
    public static void ShowWindow()
    {
        GetWindow<CanvasCreator>("Sistema Mesclado");
    }

    void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("SISTEMA MESCLADO - Interface Limpa", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "✅ Interface MESCLADA mais bonita\n✅ Personagens + Detalhes juntos\n✅ Navegação simplificada\n✅ EMOJIS CORRIGIDOS",
            MessageType.Info
        );

        GUILayout.Space(20);

        if (GUILayout.Button("🎯 CRIAR SISTEMA MESCLADO", GUILayout.Height(60)))
        {
            CriarSistemaMesclado();
        }
    }

    // 🎯 MÉTODO PRINCIPAL - INTERFACE MESCLADA
    void CriarSistemaMesclado()
    {
        Debug.Log("🔧 Criando sistema MESCLADO...");

        // ✅ CONFIGURAR FONTES PRIMEIRO
        ConfigurarFontesParaEmojis();

        RemoverSistemaExistente();
        GameObject canvasGO = CriarCanvasPrincipal();

        // 🆕 CRIA APENAS 3 GRUPOS PRINCIPAIS
        CriarTelaPrincipalMesclada(canvasGO.transform);
        CriarGrupoUpgrades(canvasGO.transform);
        CriarGrupoStages(canvasGO.transform);

        ConfigurarManagerCompletamente(canvasGO);
        Selection.activeGameObject = canvasGO;
        Debug.Log("✅ Sistema MESCLADO criado!");
    }

    // ✅ NOVO MÉTODO PARA CONFIGURAR FONTES
    void ConfigurarFontesParaEmojis()
    {
        Debug.Log("🔧 Configurando fontes para emojis...");

        // Tenta encontrar fontes com suporte a emojis
        TMP_FontAsset[] todasFontes = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        TMP_FontAsset fonteEmoji = null;

        foreach (var fonte in todasFontes)
        {
            if (fonte != null &&
               (fonte.name.ToLower().Contains("emoji") ||
                fonte.name.ToLower().Contains("color") ||
                fonte.name.ToLower().Contains("segui")))
            {
                fonteEmoji = fonte;
                Debug.Log($"✅ Fonte de emoji encontrada: {fonte.name}");
                break;
            }
        }

        // Configura fallbacks na fonte padrão
        TMP_FontAsset fontePadrao = Resources.GetBuiltinResource<TMP_FontAsset>("LiberationSans SDF");
        if (fontePadrao != null && fonteEmoji != null)
        {
            if (!fontePadrao.fallbackFontAssetTable.Contains(fonteEmoji))
            {
                fontePadrao.fallbackFontAssetTable.Add(fonteEmoji);
                Debug.Log("✅ Fonte de emoji adicionada como fallback!");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ Nenhuma fonte de emoji encontrada. Os emojis serão substituídos por quadrados.");
        }
    }

    // 🆕 TELA PRINCIPAL MESCLADA - Personagens + Detalhes juntos
    void CriarTelaPrincipalMesclada(Transform parent)
    {
        GameObject telaPrincipal = CriarGrupoMovel("TelaPrincipal", parent, Vector2.zero, new Vector2(1000, 600));
        CriarComponenteMovel("BgPrincipal", telaPrincipal.transform, Vector2.zero, new Vector2(1000, 600), new Color(0.1f, 0.1f, 0.2f, 0.95f));

        // 🎯 CABEÇALHO
        CriarCabecalho(telaPrincipal.transform);

        // 🎯 LADO ESQUERDO - SELEÇÃO DE PERSONAGENS
        CriarPainelPersonagens(telaPrincipal.transform);

        // 🎯 LADO DIREITO - DETALHES DO PERSONAGEM
        CriarPainelDetalhes(telaPrincipal.transform);

        // 🎯 RODAPÉ - BOTÕES DE AÇÃO
        CriarRodapePrincipal(telaPrincipal.transform);

        Debug.Log("✅ Tela principal mesclada criada!");
    }

    // 🆕 CABEÇALHO COM TÍTULO E MOEDAS
    void CriarCabecalho(Transform parent)
    {
        GameObject cabecalho = CriarGrupoMovel("Cabecalho", parent, new Vector2(0, 250), new Vector2(900, 80));

        // Título do jogo
        CriarTextoMovel("TituloJogo", cabecalho.transform, new Vector2(0, 15), new Vector2(400, 40), "MEU JOGO ÉPICO", 28);

        // Moedas do jogador (lado direito) - ✅ EMOJI CORRIGIDO
        CriarTextoMovel("TextoMoedas", cabecalho.transform, new Vector2(350, 15), new Vector2(200, 30), "Moedas: 1.000", 16);

        // Nível do jogador
        CriarTextoMovel("TextoNivelJogador", cabecalho.transform, new Vector2(350, -10), new Vector2(200, 25), "Nível: 1", 14);
    }

    // 🆕 PAINEL DE SELEÇÃO DE PERSONAGENS (LADO ESQUERDO)
    void CriarPainelPersonagens(Transform parent)
    {
        GameObject painel = CriarGrupoMovel("PainelPersonagens", parent, new Vector2(-300, 0), new Vector2(500, 400));
        CriarComponenteMovel("BgPersonagens", painel.transform, Vector2.zero, new Vector2(500, 400), new Color(0.15f, 0.15f, 0.25f, 0.9f));

        // Título
        CriarTextoMovel("TituloPersonagens", painel.transform, new Vector2(0, 160), new Vector2(400, 30), "SELECIONE SEU PERSONAGEM", 18);

        // 🎯 GRID DE PERSONAGENS (2x2) - ✅ EMOJIS SUBSTITUÍDOS POR TEXTO
        string[] nomes = { "Guerreiro", "Mago", "Arqueiro", "Curandeiro" };
        Color[] cores = { Color.red, Color.blue, Color.green, Color.magenta };
        string[] elementos = { "[FOGO]", "[GELO]", "[RAIO]", "[NATUREZA]" };

        for (int row = 0; row < 2; row++)
        {
            for (int col = 0; col < 2; col++)
            {
                int index = row * 2 + col;
                float x = -100 + (col * 200);
                float y = 50 - (row * 120);

                CriarCardPersonagem($"CharacterCard_{index}", painel.transform,
                    new Vector2(x, y), nomes[index], elementos[index], cores[index], index);
            }
        }
    }

    // 🆕 CARD DE PERSONAGEM ESTILIZADO - ✅ EMOJIS CORRIGIDOS
    void CriarCardPersonagem(string nome, Transform parent, Vector2 posicao, string charName, string elemento, Color cor, int index)
    {
        GameObject card = new GameObject(nome);
        card.transform.SetParent(parent);

        // RectTransform do card
        RectTransform rect = card.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicao;
        rect.sizeDelta = new Vector2(160, 100);

        // Background do card
        GameObject bg = new GameObject("CardBackground");
        bg.transform.SetParent(card.transform);
        RectTransform bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.25f, 0.25f, 0.35f, 1f);
        bgImage.raycastTarget = true;

        // Botão do card
        Button button = card.AddComponent<Button>();
        button.targetGraphic = bgImage;

        // Elemento (canto superior esquerdo) - ✅ TEXTO SEM EMOJI
        CriarTextoMovel("Elemento", card.transform, new Vector2(-60, 30), new Vector2(40, 30), elemento, 12);

        // Ícone do personagem (centro)
        GameObject icon = new GameObject("Icon");
        icon.transform.SetParent(card.transform);
        RectTransform iconRect = icon.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(-30, 0);
        iconRect.sizeDelta = new Vector2(50, 50);
        Image iconImage = icon.AddComponent<Image>();
        iconImage.color = cor;

        // Nome do personagem
        CriarTextoMovel("NomePersonagem", card.transform, new Vector2(30, 10), new Vector2(80, 25), charName, 12);

        // Status (mini barras) - ✅ TEXTO SEM EMOJI
        CriarTextoMovel("Status", card.transform, new Vector2(30, -10), new Vector2(80, 40), "VIDA ATAQUE DEF", 8);

        // Nível necessário (se bloqueado)
        CriarTextoMovel("NivelNecessario", card.transform, new Vector2(0, -35), new Vector2(120, 20), $"Nível {index + 1}", 9);

        // Indicador de seleção (borda)
        GameObject selectedBorder = new GameObject("SelectedBorder");
        selectedBorder.transform.SetParent(card.transform);
        RectTransform borderRect = selectedBorder.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.sizeDelta = new Vector2(4, 4);
        Image borderImage = selectedBorder.AddComponent<Image>();
        borderImage.color = Color.yellow;
        selectedBorder.SetActive(false);

        // 🆕 CONFIGURA CharacterIconUI
        CharacterIconUI iconUI = card.AddComponent<CharacterIconUI>();
        iconUI.characterIcon = iconImage;
        iconUI.characterName = card.transform.Find("NomePersonagem")?.GetComponent<TextMeshProUGUI>();
        iconUI.selectedIndicator = selectedBorder;
        iconUI.requiredLevelText = card.transform.Find("NivelNecessario")?.GetComponent<TextMeshProUGUI>();

        // Overlay de bloqueado
        GameObject lockedOverlay = new GameObject("LockedOverlay");
        lockedOverlay.transform.SetParent(card.transform);
        RectTransform lockedRect = lockedOverlay.AddComponent<RectTransform>();
        lockedRect.anchorMin = Vector2.zero;
        lockedRect.anchorMax = Vector2.one;
        lockedRect.sizeDelta = Vector2.zero;
        Image lockedImage = lockedOverlay.AddComponent<Image>();
        lockedImage.color = new Color(0f, 0f, 0f, 0.6f);
        lockedOverlay.SetActive(false);
        iconUI.lockedOverlay = lockedOverlay;
    }

    // 🆕 PAINEL DE DETALHES (LADO DIREITO) - ✅ EMOJIS CORRIGIDOS
    void CriarPainelDetalhes(Transform parent)
    {
        GameObject painel = CriarGrupoMovel("PainelDetalhes", parent, new Vector2(300, 0), new Vector2(350, 400));
        CriarComponenteMovel("BgDetalhes", painel.transform, Vector2.zero, new Vector2(350, 400), new Color(0.15f, 0.15f, 0.25f, 0.9f));

        // Título
        CriarTextoMovel("TituloDetalhes", painel.transform, new Vector2(0, 160), new Vector2(300, 30), "DETALHES", 18);

        // 🎯 INFORMAÇÕES DO PERSONAGEM
        CriarTextoMovel("NomePersonagemSelecionado", painel.transform, new Vector2(0, 120), new Vector2(280, 25), "Guerreiro", 16);
        CriarTextoMovel("ElementoPersonagem", painel.transform, new Vector2(0, 90), new Vector2(280, 25), "Elemento: FOGO", 14);
        CriarTextoMovel("NivelPersonagem", painel.transform, new Vector2(0, 60), new Vector2(280, 25), "Nível: 1", 14);

        // 🎯 BARRAS DE STATUS - ✅ TEXTO SEM EMOJI
        CriarBarraStatus("Vida", painel.transform, new Vector2(0, 20), 85, "VIDA: 85/100");
        CriarBarraStatus("Ataque", painel.transform, new Vector2(0, -10), 70, "ATAQUE: 70");
        CriarBarraStatus("Defesa", painel.transform, new Vector2(0, -40), 60, "DEFESA: 60");
        CriarBarraStatus("Velocidade", painel.transform, new Vector2(0, -70), 90, "VELOCIDADE: 90");

        // 🎯 HABILIDADES ESPECIAIS
        CriarTextoMovel("TituloHabilidades", painel.transform, new Vector2(0, -110), new Vector2(280, 25), "HABILIDADES", 14);
        CriarTextoMovel("Habilidade1", painel.transform, new Vector2(0, -135), new Vector2(280, 20), "• Golpe Poderoso", 11);
        CriarTextoMovel("Habilidade2", painel.transform, new Vector2(0, -155), new Vector2(280, 20), "• Escudo Protetor", 11);
        CriarTextoMovel("Habilidade3", painel.transform, new Vector2(0, -175), new Vector2(280, 20), "• Fúria do Guerreiro", 11);
    }

    // 🆕 CRIA BARRA DE STATUS VISUAL
    void CriarBarraStatus(string nome, Transform parent, Vector2 posicao, int valor, string texto)
    {
        GameObject container = new GameObject($"Barra{nome}");
        container.transform.SetParent(parent);
        RectTransform rect = container.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicao;
        rect.sizeDelta = new Vector2(250, 20);

        // Texto do status
        CriarTextoMovel($"Texto{nome}", container.transform, new Vector2(-80, 0), new Vector2(120, 18), texto, 10);

        // Barra de progresso
        GameObject barra = new GameObject($"BarraProgresso{nome}");
        barra.transform.SetParent(container.transform);
        RectTransform barraRect = barra.AddComponent<RectTransform>();
        barraRect.anchorMin = new Vector2(0.5f, 0.5f);
        barraRect.anchorMax = new Vector2(0.5f, 0.5f);
        barraRect.pivot = new Vector2(0.5f, 0.5f);
        barraRect.anchoredPosition = new Vector2(50, 0);
        barraRect.sizeDelta = new Vector2(80, 12);

        Image barraBg = barra.AddComponent<Image>();
        barraBg.color = new Color(0.3f, 0.3f, 0.3f, 1f);

        // Preenchimento da barra
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(barra.transform);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(valor / 100f, 1f);
        fillRect.sizeDelta = Vector2.zero;

        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = nome == "Vida" ? Color.red :
                         nome == "Ataque" ? new Color(1f, 0.5f, 0f) :
                         nome == "Defesa" ? Color.blue : Color.green;
    }

    // 🆕 RODAPÉ COM BOTÕES DE AÇÃO - ✅ TEXTO SEM EMOJI
    void CriarRodapePrincipal(Transform parent)
    {
        GameObject rodape = CriarGrupoMovel("RodapePrincipal", parent, new Vector2(0, -220), new Vector2(800, 80));

        // Botões de ação
        CriarBotaoEstilizado("BtnMelhorias", rodape.transform, new Vector2(-150, 0), new Vector2(140, 45), "MELHORIAS", 14);
        CriarBotaoEstilizado("BtnStages", rodape.transform, new Vector2(0, 0), new Vector2(140, 45), "STAGES", 14);
        CriarBotaoEstilizado("BtnIniciar", rodape.transform, new Vector2(150, 0), new Vector2(140, 45), "INICIAR", 14);
    }

    // 🆕 BOTÃO ESTILIZADO
    GameObject CriarBotaoEstilizado(string nome, Transform parent, Vector2 posicao, Vector2 tamanho, string texto, int fontSize)
    {
        GameObject btn = CriarBotaoMovel(nome, parent, posicao, tamanho, texto);

        Image img = btn.GetComponent<Image>();
        img.color = new Color(0.2f, 0.4f, 0.8f, 1f);

        // Sombra/efeito
        btn.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.5f);

        TextMeshProUGUI textComp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (textComp != null)
        {
            textComp.fontSize = fontSize;
            textComp.fontStyle = FontStyles.Bold;
        }

        return btn;
    }

    // 🎯 GRUPOS SECUNDÁRIOS (mantidos separados)
    void CriarGrupoUpgrades(Transform parent)
    {
        GameObject grupo = CriarGrupoMovel("GrupoUpgrades", parent, new Vector2(0, 0), new Vector2(500, 500));
        grupo.SetActive(false);

        CriarComponenteMovel("BgUpgrades", grupo.transform, Vector2.zero, new Vector2(500, 500), new Color(0.1f, 0.1f, 0.4f, 0.95f));

        // Botão voltar
        CriarBotaoVoltar("BtnVoltarUpgrades", grupo.transform, new Vector2(-200, 200));

        CriarTextoMovel("TituloUpgrades", grupo.transform, new Vector2(0, 200), new Vector2(400, 40), "MELHORIAS", 20);

        // Exemplo de upgrades
        string[] upgrades = { "VIDA", "ATAQUE", "DEFESA", "VELOCIDADE" };
        for (int i = 0; i < upgrades.Length; i++)
        {
            float y = 120 - (i * 40);
            CriarItemUpgrade($"Upgrade{upgrades[i]}", grupo.transform, new Vector2(0, y), upgrades[i]);
        }
    }

    void CriarGrupoStages(Transform parent)
    {
        GameObject grupo = CriarGrupoMovel("GrupoStages", parent, new Vector2(0, 0), new Vector2(500, 400));
        grupo.SetActive(false);

        CriarComponenteMovel("BgStages", grupo.transform, Vector2.zero, new Vector2(500, 400), new Color(0.1f, 0.1f, 0.3f, 0.95f));

        // Botão voltar
        CriarBotaoVoltar("BtnVoltarStages", grupo.transform, new Vector2(-200, 150));

        CriarTextoMovel("TituloStages", grupo.transform, new Vector2(0, 150), new Vector2(400, 40), "SELECIONE O STAGE", 20);

        string[] stages = { "Floresta", "Deserto", "Caverna", "Gelo" };
        for (int i = 0; i < stages.Length; i++)
        {
            float y = 80 - (i * 35);
            CriarBotaoStage($"Stage{i}", grupo.transform, new Vector2(0, y), stages[i]);
        }
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

        canvasGO.AddComponent<CharacterSelectionManagerIntegrated>();

        Debug.Log("✅ Canvas principal criado!");
        return canvasGO;
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

        // ✅ CONFIGURAÇÃO ADICIONAL PARA EVITAR WARNINGS
        tmp.richText = false;
        tmp.parseCtrlCharacters = false;
    }

    GameObject CriarBotaoMovel(string nome, Transform parent, Vector2 posicao, Vector2 tamanho, string texto)
    {
        GameObject btn = new GameObject(nome);
        btn.transform.SetParent(parent);

        RectTransform btnRect = btn.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = posicao;
        btnRect.sizeDelta = tamanho;

        Image img = btn.AddComponent<Image>();
        img.color = new Color(0.2f, 0.6f, 1f);

        Button button = btn.AddComponent<Button>();

        // Texto do botão
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
        tmp.richText = false;

        return btn;
    }

    GameObject CriarBotaoVoltar(string nome, Transform parent, Vector2 posicao)
    {
        GameObject btn = CriarBotaoMovel(nome, parent, posicao, new Vector2(80, 30), "← Voltar");

        Button button = btn.GetComponent<Button>();
        button.onClick.AddListener(() => {
            btn.transform.parent.gameObject.SetActive(false);
            GameObject.Find("TelaPrincipal")?.SetActive(true);
        });

        return btn;
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

    void CriarBotaoStage(string nome, Transform parent, Vector2 posicao, string stageName)
    {
        GameObject btn = CriarBotaoMovel(nome, parent, posicao, new Vector2(200, 30), stageName);
    }

    // 🆕 CONFIGURA O MANAGER COMPLETAMENTE
    void ConfigurarManagerCompletamente(GameObject canvasGO)
    {
        CharacterSelectionManagerIntegrated manager = canvasGO.GetComponent<CharacterSelectionManagerIntegrated>();
        if (manager == null)
        {
            manager = canvasGO.AddComponent<CharacterSelectionManagerIntegrated>();
        }

        ConfigurarCharacterIcons(manager);
        ConfigurarUpgradeSystem(manager);
        ConfigurarUIReferences(manager);
        ConfigurarStages(manager);

        Debug.Log("🎯 Manager configurado COMPLETAMENTE!");
    }

    void ConfigurarCharacterIcons(CharacterSelectionManagerIntegrated manager)
    {
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
        manager.coinsText = GameObject.Find("TextoMoedas")?.GetComponent<TextMeshProUGUI>();
        manager.characterNameText = GameObject.Find("NomePersonagemSelecionado")?.GetComponent<TextMeshProUGUI>();
        manager.characterLevelText = GameObject.Find("NivelPersonagem")?.GetComponent<TextMeshProUGUI>();
        manager.characterElementText = GameObject.Find("ElementoPersonagem")?.GetComponent<TextMeshProUGUI>();

        Debug.Log("✅ Referências de UI configuradas");
    }

    void ConfigurarStages(CharacterSelectionManagerIntegrated manager)
    {
        manager.stages = new StageData[3];
        manager.stageButtons = new StageButtonUI[3];
        Debug.Log("✅ Sistema de stages configurado");
    }
}