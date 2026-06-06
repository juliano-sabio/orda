using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CharacterSelectionManagerIntegrated : MonoBehaviour
{
    [Header("🎯 Banco de Dados")]
    public CharacterData[] characters;
    public StageData[] stages;

    // Painel de seleção de ultimate (pode ser fornecido externamente ou criado em runtime)
    [HideInInspector] public GameObject painelUltimates;
    private bool painelEmbutido = false;
    private int ultimateSelecionadaIndex = 0;
    private List<GameObject> botoesUltimate = new List<GameObject>();

    // Painel de seleção de passiva
    [HideInInspector] public GameObject painelPassivas;
    private int passivaSelecionadaIndex = 0;
    private List<GameObject> botoesPassiva = new List<GameObject>();

    [Header("📊 UI - Info")]
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI characterElementText;
    public TextMeshProUGUI characterDescriptionText;
    public TextMeshProUGUI elementBonusText;

    [Header("📊 UI - Habilidades")]
    public TextMeshProUGUI ultimateInfoText;
    public TextMeshProUGUI passivasInfoText;

    [Header("🖼️ UI - Preview")]
    public TextMeshProUGUI      characterPreviewName;
    public CharacterSelectionUI selectionUI;

    [Header("🔋 UI - Status")]
    public Slider[] statusSliders;

    [Header("+️ Sistema de Upgrades")]
    public Button[] upgradeButtons;
    public TextMeshProUGUI[] upgradeLevelTexts;
    public int[] upgradeLevels = new int[4];

    [Header("🗺️ Navegação")]
    public GameObject painelStages;
    public CharacterIconUI[] characterIcons;

    private void Start()
    {
        LoadProgress();
        UpdateCurrencyUI();
        CriarPainelUltimates();

        if (characterIcons != null && characters != null)
        {
            for (int i = 0; i < characterIcons.Length; i++)
            {
                if (i < characters.Length && characterIcons[i] != null)
                    characterIcons[i].Initialize(characters[i], i, this);
            }
        }

        int savedChar = PlayerPrefs.GetInt("SelectedCharacter", 0);
        if (characters.Length > 0) SelectCharacter(savedChar);
    }

    // ✅ CORREÇÃO: Método chamado pelos ícones
    public void OnCharacterIconClicked(int index) => SelectCharacter(index);

    public void SelectCharacter(int index)
    {
        if (characters == null || index < 0 || index >= characters.Length) return;

        PlayerPrefs.SetInt("SelectedCharacter", index);
        foreach (var icon in characterIcons) if (icon) icon.SetSelected(false);
        if (index < characterIcons.Length && characterIcons[index]) characterIcons[index].SetSelected(true);

        ultimateSelecionadaIndex = PlayerPrefs.GetInt($"SelectedUltimate_{index}", 0);
        passivaSelecionadaIndex  = PlayerPrefs.GetInt($"SelectedPassiva_{index}", 0);
        UpdateStatusDisplay(characters[index]);
        AtualizarPainelUltimates(characters[index]);
        AtualizarPainelPassivas(characters[index]);
    }

    public void SelectPassiva(int index)
    {
        int charIndex = PlayerPrefs.GetInt("SelectedCharacter", 0);
        passivaSelecionadaIndex = index;
        PlayerPrefs.SetInt($"SelectedPassiva_{charIndex}", index);
        PlayerPrefs.Save();
        AtualizarDestaqueBotoesPassiva();
    }

    public void SelectUltimate(int index)
    {
        int charIndex = PlayerPrefs.GetInt("SelectedCharacter", 0);
        ultimateSelecionadaIndex = index;
        PlayerPrefs.SetInt($"SelectedUltimate_{charIndex}", index);
        PlayerPrefs.Save();
        AtualizarDestaqueBotoes();
        if (characters != null && charIndex < characters.Length)
            AtualizarUltimateInfo(characters[charIndex]);
    }

    // ✅ CORREÇÃO: Método que o GameSceneManager usa para passar os dados para o Player
    public void ApplyCharacterToPlayerSystems(PlayerStats playerStats, SkillManager skillManager)
    {
        int index = PlayerPrefs.GetInt("SelectedCharacter", 0);
        if (characters == null || index >= characters.Length || playerStats == null) return;

        CharacterData data = characters[index];

        // Aplica os status base + bônus de upgrade
        playerStats.maxHealth = data.maxHealth * (1 + upgradeLevels[0] * 0.05f);

        // Se der erro nestas linhas, verifique se os nomes no PlayerStats.cs são exatamente esses:
        // playerStats.baseAttack = data.baseAttack * (1 + upgradeLevels[1] * 0.05f);
        // playerStats.baseDefense = data.baseDefense * (1 + upgradeLevels[2] * 0.05f);
        // playerStats.baseSpeed = data.baseSpeed * (1 + upgradeLevels[3] * 0.05f);

    }

    // ✅ CORREÇÃO: Método chamado ao selecionar uma fase
    public void OnStageSelected(int index)
    {
        if (stages != null && index < stages.Length)
        {
            PlayerPrefs.SetInt("SelectedStageIndex", index);
            PlayerPrefs.Save();
        }
    }

    public void BuyUpgrade(int statIndex)
    {
        int currentCoins = PlayerPrefs.GetInt("PlayerCoins", 1000);
        int cost = (upgradeLevels[statIndex] + 1) * 100;

        if (currentCoins >= cost)
        {
            currentCoins -= cost;
            upgradeLevels[statIndex]++;
            PlayerPrefs.SetInt("PlayerCoins", currentCoins);
            PlayerPrefs.SetInt($"Upgrade_{statIndex}", upgradeLevels[statIndex]);

            UpdateCurrencyUI();
            UpdateStatusDisplay(characters[PlayerPrefs.GetInt("SelectedCharacter", 0)]);
        }
    }

    public void UpdateStatusDisplay(CharacterData data)
    {
        if (!data) return;

        characterNameText.text = data.characterName;
        characterDescriptionText.text = TextUtils.SemAcento(data.description);
        characterElementText.text = $"{CharacterData.GetElementIcon(data.baseElement)} {data.baseElement}";
        characterElementText.color = CharacterData.GetElementColor(data.baseElement);
        elementBonusText.text = data.GetElementBonusDescription();

        if (statusSliders.Length >= 4)
        {
            statusSliders[0].value = (data.maxHealth * (1 + upgradeLevels[0] * 0.05f)) / 500f;
            statusSliders[1].value = (data.baseAttack * (1 + upgradeLevels[1] * 0.05f)) / 100f;
            statusSliders[2].value = (data.baseDefense * (1 + upgradeLevels[2] * 0.05f)) / 50f;
            statusSliders[3].value = (data.baseSpeed * (1 + upgradeLevels[3] * 0.05f)) / 50f;
        }

        for (int i = 0; i < upgradeLevelTexts.Length; i++)
            if (upgradeLevelTexts[i]) upgradeLevelTexts[i].text = $"Nv. {upgradeLevels[i]}";

        AtualizarUltimateInfo(data);
        AtualizarPassivasInfo(data);
        AtualizarPreview(data);
    }

    void AtualizarUltimateInfo(CharacterData data)
    {
        if (ultimateInfoText == null) return;

        UltimateData u = data.GetUltimate(ultimateSelecionadaIndex);

        if (u != null)
        {
            string elem = u.element != PlayerStats.Element.None
                ? $"{u.GetElementIcon()} {u.element}  |  " : "";
            ultimateInfoText.text =
                $"<b>{u.ultimateName}</b>\n" +
                $"{elem}CD: {u.cooldown}s\n" +
                $"DMG: {u.baseDamage}  |  Área: {u.areaOfEffect}m  |  Dur: {u.duration}s\n\n" +
                u.description +
                (string.IsNullOrEmpty(u.specialEffect) ? "" : $"\n\n<i>{u.specialEffect}</i>");
            ultimateInfoText.color = u.GetElementColor();
        }
        else
        {
            ultimateInfoText.text  = "Nenhuma ultimate\ndisponível para\neste personagem.";
            ultimateInfoText.color = new Color(0.5f, 0.5f, 0.5f);
        }
    }

    void AtualizarPassivasInfo(CharacterData data) { } // substituído por AtualizarPainelPassivas

    void AtualizarPainelPassivas(CharacterData data)
    {
        if (painelPassivas == null) return;

        foreach (var b in botoesPassiva) if (b) Destroy(b);
        botoesPassiva.Clear();

        if (data.passivasDisponiveis == null || data.passivasDisponiveis.Length == 0)
        {
            CriarBotaoPassivaVazio();
            return;
        }

        for (int i = 0; i < data.passivasDisponiveis.Length; i++)
        {
            PassiveData pd = data.passivasDisponiveis[i];
            if (pd == null) continue;
            int idx = i;
            var botao = CriarBotaoPassiva(pd);
            botao.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => SelectPassiva(idx));
            botoesPassiva.Add(botao);
        }

        AtualizarDestaqueBotoesPassiva();
    }

    void CriarBotaoPassivaVazio()
    {
        var go = new GameObject("SemPassivas");
        go.transform.SetParent(painelPassivas.transform, false);
        go.AddComponent<RectTransform>();
        var le = go.AddComponent<UnityEngine.UI.LayoutElement>();
        le.preferredHeight = 40f;
        le.flexibleWidth   = 1f;
        var txt = go.AddComponent<TMPro.TextMeshProUGUI>();
        txt.text      = "Nenhuma passiva disponível.";
        txt.fontSize  = 10f;
        txt.color     = new Color(0.5f, 0.5f, 0.5f);
        txt.alignment = TMPro.TextAlignmentOptions.Center;
        botoesPassiva.Add(go);
    }

    GameObject CriarBotaoPassiva(PassiveData pd)
    {
        var go = new GameObject($"BotaoPassiva_{TextUtils.SemAcento(pd.passiveName)}");
        go.transform.SetParent(painelPassivas.transform, false);
        go.AddComponent<RectTransform>();
        var le = go.AddComponent<UnityEngine.UI.LayoutElement>();
        le.preferredHeight = 100f;
        le.flexibleWidth   = 1f;

        var img = go.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.10f, 0.08f, 0.16f, 1f);

        var btn = go.AddComponent<UnityEngine.UI.Button>();
        btn.targetGraphic = img;
        btn.transition    = Selectable.Transition.None;

        // Borda esquerda dourada
        var goBordaE = new GameObject("BordaEsq");
        goBordaE.transform.SetParent(go.transform, false);
        var rBE = goBordaE.AddComponent<RectTransform>();
        rBE.anchorMin = Vector2.zero; rBE.anchorMax = new Vector2(0f, 1f);
        rBE.offsetMin = Vector2.zero; rBE.offsetMax = new Vector2(5f, 0f);
        goBordaE.AddComponent<UnityEngine.UI.Image>().color = new Color(0.78f, 0.66f, 0.35f);

        // Borda topo
        var goBordaT = new GameObject("BordaTopo");
        goBordaT.transform.SetParent(go.transform, false);
        var rBT = goBordaT.AddComponent<RectTransform>();
        rBT.anchorMin = new Vector2(0f, 1f); rBT.anchorMax = new Vector2(1f, 1f);
        rBT.offsetMin = new Vector2(0f, -2f); rBT.offsetMax = Vector2.zero;
        goBordaT.AddComponent<UnityEngine.UI.Image>().color = new Color(0.78f, 0.66f, 0.35f, 0.4f);

        bool temIcone = pd.passiveIcon != null;

        if (temIcone)
        {
            var goIcon = new GameObject("Icone");
            goIcon.transform.SetParent(go.transform, false);
            var rIcon = goIcon.AddComponent<RectTransform>();
            rIcon.anchorMin = new Vector2(0.02f, 0.06f);
            rIcon.anchorMax = new Vector2(0.20f, 0.94f);
            rIcon.offsetMin = rIcon.offsetMax = Vector2.zero;
            var imgIcon = goIcon.AddComponent<UnityEngine.UI.Image>();
            imgIcon.sprite         = pd.passiveIcon;
            imgIcon.preserveAspect = true;
            imgIcon.raycastTarget  = false;
        }

        float nomeX = temIcone ? 0.25f : 0.07f;

        var goNome = new GameObject("Nome");
        goNome.transform.SetParent(go.transform, false);
        var rNome = goNome.AddComponent<RectTransform>();
        rNome.anchorMin = new Vector2(nomeX, 0.70f); rNome.anchorMax = new Vector2(0.97f, 0.97f);
        rNome.offsetMin = rNome.offsetMax = Vector2.zero;
        var txtNome = goNome.AddComponent<TMPro.TextMeshProUGUI>();
        txtNome.text      = TextUtils.SemAcento(pd.passiveName);
        txtNome.fontSize  = 12f;
        txtNome.fontStyle = TMPro.FontStyles.Bold;
        txtNome.color     = new Color(0.92f, 0.82f, 0.62f);
        txtNome.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
        txtNome.textWrappingMode = TMPro.TextWrappingModes.Normal;

        var goBonus = new GameObject("Bonus");
        goBonus.transform.SetParent(go.transform, false);
        var rBonus = goBonus.AddComponent<RectTransform>();
        rBonus.anchorMin = new Vector2(nomeX, 0.48f); rBonus.anchorMax = new Vector2(0.97f, 0.68f);
        rBonus.offsetMin = rBonus.offsetMax = Vector2.zero;
        var txtBonus = goBonus.AddComponent<TMPro.TextMeshProUGUI>();
        txtBonus.text             = pd.GetBonusDescription();
        txtBonus.fontSize         = 9f;
        txtBonus.color            = new Color(0.90f, 0.82f, 0.65f);
        txtBonus.alignment        = TMPro.TextAlignmentOptions.MidlineLeft;
        txtBonus.textWrappingMode = TMPro.TextWrappingModes.Normal;
        txtBonus.richText         = true;

        float descX = temIcone ? 0.25f : 0.07f;
        var goDesc = new GameObject("Desc");
        goDesc.transform.SetParent(go.transform, false);
        var rDesc = goDesc.AddComponent<RectTransform>();
        rDesc.anchorMin = new Vector2(descX, 0.02f); rDesc.anchorMax = new Vector2(0.97f, 0.46f);
        rDesc.offsetMin = rDesc.offsetMax = Vector2.zero;
        var txtDesc = goDesc.AddComponent<TMPro.TextMeshProUGUI>();
        txtDesc.text             = pd.description;
        txtDesc.fontSize         = 9.5f;
        txtDesc.color            = new Color(0.60f, 0.55f, 0.45f);
        txtDesc.alignment        = TMPro.TextAlignmentOptions.TopLeft;
        txtDesc.textWrappingMode = TMPro.TextWrappingModes.Normal;
        txtDesc.overflowMode     = TMPro.TextOverflowModes.Ellipsis;

        var goSel = new GameObject("SelBar");
        goSel.transform.SetParent(go.transform, false);
        var rSel = goSel.AddComponent<RectTransform>();
        rSel.anchorMin = Vector2.zero; rSel.anchorMax = new Vector2(1f, 0f);
        rSel.offsetMin = Vector2.zero; rSel.offsetMax = new Vector2(0f, 3f);
        var imgSel = goSel.AddComponent<UnityEngine.UI.Image>();
        imgSel.color         = new Color(0.78f, 0.66f, 0.35f, 0f);
        imgSel.raycastTarget = false;

        return go;
    }

    void AtualizarDestaqueBotoesPassiva()
    {
        for (int i = 0; i < botoesPassiva.Count; i++)
        {
            if (botoesPassiva[i] == null) continue;
            bool sel = i == passivaSelecionadaIndex;
            var img = botoesPassiva[i].GetComponent<UnityEngine.UI.Image>();
            if (img != null)
                img.color = sel ? new Color(0.18f, 0.14f, 0.26f, 1f) : new Color(0.10f, 0.08f, 0.16f, 1f);
            var selBar = botoesPassiva[i].transform.Find("SelBar");
            if (selBar != null)
            {
                var imgBar = selBar.GetComponent<UnityEngine.UI.Image>();
                if (imgBar != null)
                    imgBar.color = sel ? new Color(0.78f, 0.66f, 0.35f, 1f) : new Color(0f, 0f, 0f, 0f);
            }
        }
    }

    void AtualizarPreview(CharacterData data)
    {
        if (selectionUI != null) selectionUI.AtualizarPreviewPrefab(data);
        if (characterPreviewName != null) characterPreviewName.text = data.characterName;
    }

    string GetElementEffectDescription(PlayerStats.Element element)
    {
        switch (element)
        {
            case PlayerStats.Element.Fire:      return "Inimigos atingidos sofrem queimadura contínua.";
            case PlayerStats.Element.Ice:       return "Ataques têm chance de lentificar inimigos.";
            case PlayerStats.Element.Lightning: return "Dano em cadeia entre inimigos próximos.";
            case PlayerStats.Element.Poison:    return "Aplica veneno com dano por segundo.";
            case PlayerStats.Element.Earth:     return "Ataques pesados têm chance de atordoar.";
            case PlayerStats.Element.Wind:      return "Bônus de velocidade e repulsão ao colidir.";
            default:                            return "";
        }
    }

    public void ToggleStages(bool open) => painelStages.SetActive(open);

    private void LoadProgress()
    {
        for (int i = 0; i < 4; i++) upgradeLevels[i] = PlayerPrefs.GetInt($"Upgrade_{i}", 0);
    }

    private void UpdateCurrencyUI()
    {
        if (coinsText) coinsText.text = $"$ {PlayerPrefs.GetInt("PlayerCoins", 1000)}";
    }

    // ─── Painel de Seleção de Ultimate ───────────────────────────────────────

    void CriarPainelUltimates()
    {
        // Se já foi fornecido externamente (embutido na aba ULTIMATE), só marca e sai
        if (painelUltimates != null)
        {
            painelEmbutido = true;
            return;
        }

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        painelUltimates = new GameObject("PainelUltimates");
        painelUltimates.transform.SetParent(canvas.transform, false);

        RectTransform rect = painelUltimates.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot     = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 20f);
        rect.sizeDelta = new Vector2(750f, 195f);

        Image bg = painelUltimates.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.15f, 0.92f);

        GameObject titulo = new GameObject("Titulo");
        titulo.transform.SetParent(painelUltimates.transform, false);
        RectTransform rTitulo = titulo.AddComponent<RectTransform>();
        rTitulo.anchorMin = new Vector2(0f, 1f); rTitulo.anchorMax = new Vector2(1f, 1f);
        rTitulo.pivot = new Vector2(0.5f, 1f);
        rTitulo.anchoredPosition = new Vector2(0f, -8f);
        rTitulo.sizeDelta = new Vector2(0f, 24f);
        TextMeshProUGUI txtTitulo = titulo.AddComponent<TextMeshProUGUI>();
        txtTitulo.text = "ESCOLHA SUA ULTIMATE";
        txtTitulo.fontSize = 14f;
        txtTitulo.fontStyle = FontStyles.Bold;
        txtTitulo.color = Color.yellow;
        txtTitulo.alignment = TextAlignmentOptions.Center;

        painelUltimates.SetActive(false);
    }

    void AtualizarPainelUltimates(CharacterData data)
    {
        if (painelUltimates == null) return;

        foreach (var b in botoesUltimate) if (b) Destroy(b);
        botoesUltimate.Clear();

        if (!data.HasUltimatesDisponiveis())
        {
            if (!painelEmbutido) painelUltimates.SetActive(false);
            return;
        }

        if (!painelEmbutido) painelUltimates.SetActive(true);

        int total = data.ultimatesDisponiveis.Length;

        for (int i = 0; i < total; i++)
        {
            UltimateData ud = data.ultimatesDisponiveis[i];
            if (ud == null) continue;

            int capturedIndex = i;
            GameObject botao = painelEmbutido
                ? CriarBotaoUltimateEmbutido(ud, i, total)
                : CriarBotaoUltimate(ud, CalcularPosXBotao(i, total));
            botao.GetComponent<Button>().onClick.AddListener(() => SelectUltimate(capturedIndex));
            botoesUltimate.Add(botao);
        }

        AtualizarDestaqueBotoes();
    }

    float CalcularPosXBotao(int index, int total)
    {
        float largura = 220f, espaco = 16f;
        float totalW  = total * largura + (total - 1) * espaco;
        return -totalW / 2f + largura / 2f + index * (largura + espaco);
    }

    // Versão empilhada com scroll para uso dentro da aba ULTIMATE
    GameObject CriarBotaoUltimateEmbutido(UltimateData ud, int index, int total)
    {
        GameObject go = new GameObject($"BotaoUlt_{TextUtils.SemAcento(ud.ultimateName)}");
        go.transform.SetParent(painelUltimates.transform, false);
        go.AddComponent<RectTransform>();
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 100f;
        le.flexibleWidth   = 1f;

        Color corAcento = ud.GetElementColor();

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.10f, 0.08f, 0.16f, 1f);
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition    = Selectable.Transition.None;

        // Borda esquerda colorida com cor do elemento
        var goBordaE = new GameObject("BordaEsq");
        goBordaE.transform.SetParent(go.transform, false);
        var rBE = goBordaE.AddComponent<RectTransform>();
        rBE.anchorMin = Vector2.zero; rBE.anchorMax = new Vector2(0f, 1f);
        rBE.offsetMin = Vector2.zero; rBE.offsetMax = new Vector2(5f, 0f);
        goBordaE.AddComponent<Image>().color = corAcento;

        // Borda topo sutil
        var goBordaT = new GameObject("BordaTopo");
        goBordaT.transform.SetParent(go.transform, false);
        var rBT = goBordaT.AddComponent<RectTransform>();
        rBT.anchorMin = new Vector2(0f, 1f); rBT.anchorMax = new Vector2(1f, 1f);
        rBT.offsetMin = new Vector2(0f, -2f); rBT.offsetMax = Vector2.zero;
        goBordaT.AddComponent<Image>().color = new Color(corAcento.r, corAcento.g, corAcento.b, 0.5f);

        bool temIcone = ud.ultimateIcon != null;

        if (temIcone)
        {
            var goIcon = new GameObject("Icone");
            goIcon.transform.SetParent(go.transform, false);
            var rIcon = goIcon.AddComponent<RectTransform>();
            rIcon.anchorMin = new Vector2(0.02f, 0.06f);
            rIcon.anchorMax = new Vector2(0.20f, 0.94f);
            rIcon.offsetMin = rIcon.offsetMax = Vector2.zero;
            var imgIcon = goIcon.AddComponent<Image>();
            imgIcon.sprite         = ud.ultimateIcon;
            imgIcon.preserveAspect = true;
            imgIcon.color          = Color.white;
            imgIcon.raycastTarget  = false;
        }

        float nomeX = temIcone ? 0.25f : 0.07f;
        var goNome = new GameObject("Nome");
        goNome.transform.SetParent(go.transform, false);
        var rNome = goNome.AddComponent<RectTransform>();
        rNome.anchorMin = new Vector2(nomeX, 0.70f); rNome.anchorMax = new Vector2(0.97f, 0.97f);
        rNome.offsetMin = rNome.offsetMax = Vector2.zero;
        var txtNome = goNome.AddComponent<TextMeshProUGUI>();
        txtNome.text      = TextUtils.SemAcento(ud.ultimateName);
        txtNome.fontSize  = 12f;
        txtNome.fontStyle = FontStyles.Bold;
        txtNome.color     = corAcento;
        txtNome.alignment = TextAlignmentOptions.MidlineLeft;
        txtNome.textWrappingMode = TMPro.TextWrappingModes.Normal;

        float cdX = temIcone ? 0.25f : 0.07f;
        var goCD = new GameObject("CD");
        goCD.transform.SetParent(go.transform, false);
        var rCD = goCD.AddComponent<RectTransform>();
        rCD.anchorMin = new Vector2(cdX, 0.48f); rCD.anchorMax = new Vector2(0.97f, 0.68f);
        rCD.offsetMin = rCD.offsetMax = Vector2.zero;
        var txtCD = goCD.AddComponent<TextMeshProUGUI>();
        txtCD.text      = $"CD:{ud.cooldown}s  |  {ud.duration}s";
        txtCD.fontSize  = 9f;
        txtCD.color     = new Color(0.75f, 0.70f, 0.55f);
        txtCD.alignment = TextAlignmentOptions.MidlineLeft;

        float descX = temIcone ? 0.25f : 0.07f;
        var goDesc = new GameObject("Desc");
        goDesc.transform.SetParent(go.transform, false);
        var rDesc = goDesc.AddComponent<RectTransform>();
        rDesc.anchorMin = new Vector2(descX, 0.02f); rDesc.anchorMax = new Vector2(0.97f, 0.46f);
        rDesc.offsetMin = rDesc.offsetMax = Vector2.zero;
        var txtDesc = goDesc.AddComponent<TextMeshProUGUI>();
        txtDesc.text             = TextUtils.SemAcento(ud.description);
        txtDesc.fontSize         = 9.5f;
        txtDesc.color            = new Color(0.60f, 0.55f, 0.45f);
        txtDesc.alignment        = TextAlignmentOptions.TopLeft;
        txtDesc.textWrappingMode = TMPro.TextWrappingModes.Normal;
        txtDesc.overflowMode     = TextOverflowModes.Ellipsis;

        var goSel = new GameObject("SelBar");
        goSel.transform.SetParent(go.transform, false);
        var rSel = goSel.AddComponent<RectTransform>();
        rSel.anchorMin = Vector2.zero; rSel.anchorMax = new Vector2(1f, 0f);
        rSel.offsetMin = Vector2.zero; rSel.offsetMax = new Vector2(0f, 3f);
        var imgSel = goSel.AddComponent<Image>();
        imgSel.color         = new Color(corAcento.r, corAcento.g, corAcento.b, 0f);
        imgSel.raycastTarget = false;

        return go;
    }

    GameObject CriarBotaoUltimate(UltimateData ud, float posX)
    {
        GameObject go = new GameObject($"BotaoUltimate_{TextUtils.SemAcento(ud.ultimateName)}");
        go.transform.SetParent(painelUltimates.transform, false);

        RectTransform r = go.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f, 0.5f); r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = new Vector2(posX, -15f);
        r.sizeDelta = new Vector2(220f, 150f);

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.25f, 1f);

        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.2f, 0.3f, 0.6f, 1f);
        cb.pressedColor = new Color(0.1f, 0.2f, 0.4f, 1f);
        btn.colors = cb;
        btn.targetGraphic = img;

        // Ícone
        bool temIcone = ud.ultimateIcon != null;
        if (temIcone)
        {
            var goIcon = new GameObject("Icone");
            goIcon.transform.SetParent(go.transform, false);
            var rIcon = goIcon.AddComponent<RectTransform>();
            rIcon.anchorMin = new Vector2(0.03f, 0.13f);
            rIcon.anchorMax = new Vector2(0.27f, 0.87f);
            rIcon.offsetMin = rIcon.offsetMax = Vector2.zero;
            var imgIcon = goIcon.AddComponent<Image>();
            imgIcon.sprite         = ud.ultimateIcon;
            imgIcon.preserveAspect = true;
            imgIcon.color          = Color.white;
            imgIcon.raycastTarget  = false;
        }

        float textX = temIcone ? 0.30f : 0f;

        // Nome
        GameObject goNome = new GameObject("Nome");
        goNome.transform.SetParent(go.transform, false);
        RectTransform rNome = goNome.AddComponent<RectTransform>();
        rNome.anchorMin = new Vector2(textX, 1f); rNome.anchorMax = new Vector2(1f, 1f);
        rNome.pivot = new Vector2(0.5f, 1f);
        rNome.anchoredPosition = new Vector2(0f, -6f);
        rNome.sizeDelta = new Vector2(-8f, 24f);
        TextMeshProUGUI txtNome = goNome.AddComponent<TextMeshProUGUI>();
        txtNome.text = TextUtils.SemAcento(ud.ultimateName);
        txtNome.fontSize = 12f;
        txtNome.fontStyle = FontStyles.Bold;
        txtNome.color = ud.GetElementColor();
        txtNome.alignment = TextAlignmentOptions.Center;
        txtNome.textWrappingMode = TMPro.TextWrappingModes.Normal;

        // Elemento
        GameObject goElem = new GameObject("Elemento");
        goElem.transform.SetParent(go.transform, false);
        RectTransform rElem = goElem.AddComponent<RectTransform>();
        rElem.anchorMin = new Vector2(textX, 1f); rElem.anchorMax = new Vector2(1f, 1f);
        rElem.pivot = new Vector2(0.5f, 1f);
        rElem.anchoredPosition = new Vector2(0f, -34f);
        rElem.sizeDelta = new Vector2(-8f, 18f);
        TextMeshProUGUI txtElem = goElem.AddComponent<TextMeshProUGUI>();
        txtElem.text = $"{ud.GetElementIcon()} {ud.element}  |  CD: {ud.cooldown}s";
        txtElem.fontSize = 10f;
        txtElem.color = new Color(0.8f, 0.8f, 0.8f);
        txtElem.alignment = TextAlignmentOptions.Center;

        // Descrição — ocupa a parte de baixo do card
        GameObject goDesc = new GameObject("Desc");
        goDesc.transform.SetParent(go.transform, false);
        RectTransform rDesc = goDesc.AddComponent<RectTransform>();
        rDesc.anchorMin = new Vector2(0.02f, 0f); rDesc.anchorMax = new Vector2(1f, 1f);
        rDesc.offsetMin = new Vector2(6f, 6f); rDesc.offsetMax = new Vector2(-6f, -58f);
        TextMeshProUGUI txtDesc = goDesc.AddComponent<TextMeshProUGUI>();
        txtDesc.text = TextUtils.SemAcento(ud.description);
        txtDesc.fontSize = 10f;
        txtDesc.color = new Color(0.75f, 0.75f, 0.75f);
        txtDesc.alignment = TextAlignmentOptions.Center;
        txtDesc.textWrappingMode = TMPro.TextWrappingModes.Normal;
        txtDesc.overflowMode = TextOverflowModes.Ellipsis;

        return go;
    }

    void AtualizarDestaqueBotoes()
    {
        for (int i = 0; i < botoesUltimate.Count; i++)
        {
            if (botoesUltimate[i] == null) continue;
            bool sel = i == ultimateSelecionadaIndex;

            Image img = botoesUltimate[i].GetComponent<Image>();
            if (img != null)
                img.color = sel
                    ? new Color(0.18f, 0.14f, 0.26f, 1f)
                    : new Color(0.10f, 0.08f, 0.16f, 1f);

            var selBar = botoesUltimate[i].transform.Find("SelBar");
            if (selBar != null)
            {
                var imgBar = selBar.GetComponent<Image>();
                if (imgBar != null)
                    imgBar.color = sel
                        ? new Color(0.78f, 0.66f, 0.35f, 1f)
                        : new Color(0f, 0f, 0f, 0f);
            }
        }
    }
}
