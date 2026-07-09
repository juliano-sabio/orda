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
    private List<int> botoesUltimateOrig = new List<int>(); // índice original de cada botão (exibição reordenada)
    private List<bool> botoesUltimateDisp = new List<bool>(); // se cada botão está disponível

    // só estas ultimates ficam disponíveis; as demais aparecem como indisponíveis
    static readonly string[] ultimatesLiberadasSel =
        { "raio certeiro", "domo retardante", "tempestade", "necropole", "drenagem" };

    // Painel de seleção de passiva
    [HideInInspector] public GameObject painelPassivas;
    private int passivaSelecionadaIndex = 0;
    private List<GameObject> botoesPassiva = new List<GameObject>();
    private List<bool> botoesPassivaDisp = new List<bool>();

    // quantas passivas (as primeiras) ficam disponíveis; o resto é bloqueado
    const int PASSIVAS_LIBERADAS_SEL = 4;

    [Header("📊 UI - Info")]
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI espiritosText;
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
    [HideInInspector] public TextMeshProUGUI[] statusTexts;

    [Header("+️ Sistema de Upgrades")]
    public Button[] upgradeButtons;
    public TextMeshProUGUI[] upgradeLevelTexts;
    public int[] upgradeLevels = new int[4];

    [Header("🗺️ Navegação")]
    public GameObject painelStages;
    public CharacterIconUI[] characterIcons;

    private void Start()
    {
        // Registra a lista de personagens (índice → CharacterData) pra o ApplyCharacterData resolver
        // o personagem escolhido POR ÍNDICE também no SP — senão o gameplay usava um characterData
        // stale (servo) mesmo escolhendo o lobo.
        if (characters != null && characters.Length > 0) PlayerStats.RegistroPersonagens = characters;

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

        if (GameSceneManager.Instance != null)
            GameSceneManager.Instance.selectedCharacterData = characters[index];

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

    public void ApplyCharacterToPlayerSystems(PlayerStats playerStats, SkillManager skillManager)
    {
        int index = PlayerPrefs.GetInt("SelectedCharacter", 0);
        if (characters == null || index >= characters.Length || playerStats == null) return;

        CharacterData data = characters[index];

        playerStats.characterData = data;
        playerStats.ApplyCharacterData();

        playerStats.maxHealth *= (1 + upgradeLevels[0] * 0.05f);
        playerStats.health     = playerStats.maxHealth;
        playerStats.attack    *= (1 + upgradeLevels[1] * 0.05f);
        playerStats.defense   *= (1 + upgradeLevels[2] * 0.05f);
        playerStats.speed     *= (1 + upgradeLevels[3] * 0.05f);
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

    public void UpgradeStatusComEspirito(int statIndex)
    {
        int charIndex = PlayerPrefs.GetInt("SelectedCharacter", 0);
        if (EspiritoUpgradeSystem.TryUpgrade(charIndex, statIndex))
        {
            UpdateStatusDisplay(characters[charIndex]);
            if (selectionUI != null) selectionUI.AtualizarGlowsEspirito();
        }
    }

    public void UpdateStatusDisplay(CharacterData data)
    {
        if (!data) return;

        characterNameText.text = data.GetDisplayName();
        characterDescriptionText.text = data.GetDisplayDescription();
        characterElementText.text = $"{CharacterData.GetElementIcon(data.baseElement)} {Loc.T("element." + data.baseElement.ToString().ToLower())}";
        characterElementText.color = CharacterData.GetElementColor(data.baseElement);
        elementBonusText.text = data.GetElementBonusDescription();

        if (statusSliders != null && statusSliders.Length >= 4 && statusSliders[0] != null)
        {
            statusSliders[0].value = (data.maxHealth * (1 + upgradeLevels[0] * 0.05f)) / 500f;
            statusSliders[1].value = (data.baseAttack * (1 + upgradeLevels[1] * 0.05f)) / 100f;
            statusSliders[2].value = (data.baseDefense * (1 + upgradeLevels[2] * 0.05f)) / 50f;
            statusSliders[3].value = (data.baseSpeed * (1 + upgradeLevels[3] * 0.05f)) / 50f;
        }

        int charIndex = PlayerPrefs.GetInt("SelectedCharacter", 0);

        if (statusTexts != null && statusTexts.Length >= 8)
        {
            float atq  = data.baseAttack   * (1 + upgradeLevels[1] * 0.05f) * EspiritoUpgradeSystem.GetMultiplicador(charIndex, 0);
            float def  = data.baseDefense  * (1 + upgradeLevels[2] * 0.05f) * EspiritoUpgradeSystem.GetMultiplicador(charIndex, 1);
            float vel  = data.baseSpeed    * (1 + upgradeLevels[3] * 0.05f) * EspiritoUpgradeSystem.GetMultiplicador(charIndex, 5);
            float vida = data.maxHealth    * (1 + upgradeLevels[0] * 0.05f) * EspiritoUpgradeSystem.GetMultiplicador(charIndex, 4);
            float regen     = data.baseHealthRegen     * EspiritoUpgradeSystem.GetMultiplicador(charIndex, 6);
            float velAtq    = data.baseAttackCooldown  * EspiritoUpgradeSystem.GetMultiplicador(charIndex, 3);
            float velEscudo = data.baseDefenseCooldown * EspiritoUpgradeSystem.GetMultiplicador(charIndex, 7);
            float critico   = 10f * EspiritoUpgradeSystem.GetMultiplicador(charIndex, 2); // base padrão

            if (statusTexts[0] != null) statusTexts[0].text = $"{Loc.T("stat.atk")}: {atq:F1}";
            if (statusTexts[1] != null) statusTexts[1].text = $"{Loc.T("stat.def")}: {def:F1}";
            if (statusTexts[2] != null) statusTexts[2].text = $"{Loc.T("stat.crit")}: {critico:F1}%";
            if (statusTexts[3] != null) statusTexts[3].text = $"{Loc.T("stat.atkspd")}: {velAtq:F1}s";
            if (statusTexts[4] != null) statusTexts[4].text = $"{Loc.T("stat.hp")}: {vida:F0}";
            if (statusTexts[5] != null) statusTexts[5].text = $"{Loc.T("stat.spd")}: {vel:F1}";
            if (statusTexts[6] != null) statusTexts[6].text = $"{Loc.T("stat.regen")}: {regen:F1}/s";
            if (statusTexts[7] != null) statusTexts[7].text = $"{Loc.T("stat.shield")}: {velEscudo:F1}s";
        }

        if (espiritosText != null)
            espiritosText.text = $"{EspiritoUpgradeSystem.GetEspiritos(charIndex)}";

        for (int i = 0; i < upgradeLevelTexts.Length; i++)
            if (upgradeLevelTexts[i]) upgradeLevelTexts[i].text = $"{Loc.T("ui.level_abbr")} {upgradeLevels[i]}";

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
            string elemName = u.element != PlayerStats.Element.None
                ? Loc.T("element." + u.element.ToString().ToLower()) : "";
            string elem = u.element != PlayerStats.Element.None
                ? $"{u.GetElementIcon()} {elemName}  |  " : "";
            string eff = u.GetDisplaySpecialEffect();
            ultimateInfoText.text =
                $"<b>{u.GetDisplayName()}</b>\n" +
                $"{elem}{Loc.T("ui.cd")}: {u.cooldown}s\n" +
                $"{Loc.T("ui.dmg")}: {u.baseDamage}  |  {Loc.T("ui.area")}: {u.areaOfEffect}m  |  {Loc.T("ui.dur")}: {u.duration}s\n\n" +
                u.GetDisplayDescription() +
                (string.IsNullOrEmpty(eff) ? "" : $"\n\n<i>{eff}</i>");
            ultimateInfoText.color = u.GetElementColor();
        }
        else
        {
            ultimateInfoText.text  = Loc.T("ui.no_ultimate");
            ultimateInfoText.color = new Color(0.5f, 0.5f, 0.5f);
        }
    }

    void AtualizarPassivasInfo(CharacterData data) { } // substituído por AtualizarPainelPassivas

    void AtualizarPainelPassivas(CharacterData data)
    {
        if (painelPassivas == null) return;

        foreach (var b in botoesPassiva) if (b) Destroy(b);
        botoesPassiva.Clear();
        botoesPassivaDisp.Clear();

        if (data.passivasDisponiveis == null || data.passivasDisponiveis.Length == 0)
        {
            CriarBotaoPassivaVazio();
            return;
        }

        // garante que a passiva selecionada esteja entre as 4 primeiras (liberadas)
        if (passivaSelecionadaIndex >= PASSIVAS_LIBERADAS_SEL)
        {
            passivaSelecionadaIndex = 0;
            int ci = PlayerPrefs.GetInt("SelectedCharacter", 0);
            PlayerPrefs.SetInt($"SelectedPassiva_{ci}", 0);
        }
        // Passiva bloqueada por missão selecionada e ainda não liberada: volta pra 0
        if (passivaSelecionadaIndex >= 0 && passivaSelecionadaIndex < data.passivasDisponiveis.Length
            && PassivaMissao(data.passivasDisponiveis[passivaSelecionadaIndex], out bool selPassDesb) && !selPassDesb)
        {
            passivaSelecionadaIndex = 0;
            int ci = PlayerPrefs.GetInt("SelectedCharacter", 0);
            PlayerPrefs.SetInt($"SelectedPassiva_{ci}", 0);
        }

        for (int i = 0; i < data.passivasDisponiveis.Length; i++)
        {
            PassiveData pd = data.passivasDisponiveis[i];
            if (pd == null) continue;
            int  idx = i;
            bool disponivel = (i < PASSIVAS_LIBERADAS_SEL);
            // Passivas bloqueadas por missão (Coração Robusto, Caçador) dependem da missão
            bool ehMissaoPass = PassivaMissao(pd, out bool passDesb);
            if (ehMissaoPass) disponivel = passDesb;
            var botao = CriarBotaoPassiva(pd);

            if (disponivel)
                botao.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => SelectPassiva(idx));
            else
                MarcarBotaoIndisponivel(botao, ehMissaoPass);

            botoesPassiva.Add(botao);
            botoesPassivaDisp.Add(disponivel);
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
        txt.text      = Loc.T("ui.no_passives");
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
        le.preferredHeight = 128f; // mais alto: cabe passiva com 3 linhas de bônus sem sobrepor
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
        rNome.anchorMin = new Vector2(nomeX, 0.83f); rNome.anchorMax = new Vector2(0.97f, 0.98f);
        rNome.offsetMin = rNome.offsetMax = Vector2.zero;
        var txtNome = goNome.AddComponent<TMPro.TextMeshProUGUI>();
        txtNome.text      = pd.GetDisplayName();
        txtNome.fontSize  = 12f;
        txtNome.fontStyle = TMPro.FontStyles.Bold;
        txtNome.color     = new Color(0.92f, 0.82f, 0.62f);
        txtNome.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
        txtNome.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        txtNome.overflowMode     = TMPro.TextOverflowModes.Ellipsis;

        var goBonus = new GameObject("Bonus");
        goBonus.transform.SetParent(go.transform, false);
        var rBonus = goBonus.AddComponent<RectTransform>();
        rBonus.anchorMin = new Vector2(nomeX, 0.46f); rBonus.anchorMax = new Vector2(0.97f, 0.81f);
        rBonus.offsetMin = rBonus.offsetMax = Vector2.zero;
        var txtBonus = goBonus.AddComponent<TMPro.TextMeshProUGUI>();
        txtBonus.text             = pd.GetBonusDescription();
        txtBonus.fontSize         = 9f;
        txtBonus.color            = new Color(0.90f, 0.82f, 0.65f);
        txtBonus.alignment        = TMPro.TextAlignmentOptions.TopLeft;
        txtBonus.textWrappingMode = TMPro.TextWrappingModes.Normal;
        txtBonus.richText         = true;

        float descX = temIcone ? 0.25f : 0.07f;
        var goDesc = new GameObject("Desc");
        goDesc.transform.SetParent(go.transform, false);
        var rDesc = goDesc.AddComponent<RectTransform>();
        rDesc.anchorMin = new Vector2(descX, 0.04f); rDesc.anchorMax = new Vector2(0.97f, 0.43f);
        rDesc.offsetMin = rDesc.offsetMax = Vector2.zero;
        var txtDesc = goDesc.AddComponent<TMPro.TextMeshProUGUI>();
        txtDesc.text             = pd.GetDisplayDescription();
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
            if (i < botoesPassivaDisp.Count && !botoesPassivaDisp[i]) continue; // indisponível: mantém apagado
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
        if (characterPreviewName != null) characterPreviewName.text = data.GetDisplayName();
    }

    string GetElementEffectDescription(PlayerStats.Element element)
    {
        switch (element)
        {
            case PlayerStats.Element.Fire:      return Loc.T("element.effect.fire");
            case PlayerStats.Element.Ice:       return Loc.T("element.effect.ice");
            case PlayerStats.Element.Lightning: return Loc.T("element.effect.lightning");
            case PlayerStats.Element.Poison:    return Loc.T("element.effect.poison");
            case PlayerStats.Element.Earth:     return Loc.T("element.effect.earth");
            case PlayerStats.Element.Wind:      return Loc.T("element.effect.wind");
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
        txtTitulo.text = Loc.T("ui.choose_ultimate");
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
        botoesUltimateOrig.Clear();
        botoesUltimateDisp.Clear();

        if (!data.HasUltimatesDisponiveis())
        {
            if (!painelEmbutido) painelUltimates.SetActive(false);
            return;
        }

        if (!painelEmbutido) painelUltimates.SetActive(true);

        int total = data.ultimatesDisponiveis.Length;

        // garante que a ultimate selecionada esteja entre as disponíveis
        if (!UltimateLiberadaSel(data.ultimatesDisponiveis[Mathf.Clamp(ultimateSelecionadaIndex, 0, total - 1)]))
        {
            for (int i = 0; i < total; i++)
                if (UltimateLiberadaSel(data.ultimatesDisponiveis[i]))
                {
                    ultimateSelecionadaIndex = i;
                    int ci = PlayerPrefs.GetInt("SelectedCharacter", 0);
                    PlayerPrefs.SetInt($"SelectedUltimate_{ci}", i);
                    break;
                }
        }

        // ordem de exibição fixa pros principais; o índice salvo continua o original.
        int disp = 0;
        foreach (int i in OrdenarUltimatesSel(data.ultimatesDisponiveis))
        {
            UltimateData ud = data.ultimatesDisponiveis[i];
            if (ud == null) continue;

            int  capturedIndex = i;
            bool disponivel    = UltimateLiberadaSel(ud);
            GameObject botao = painelEmbutido
                ? CriarBotaoUltimateEmbutido(ud, disp, total)
                : CriarBotaoUltimate(ud, CalcularPosXBotao(disp, total));

            if (disponivel)
                botao.GetComponent<Button>().onClick.AddListener(() => SelectUltimate(capturedIndex));
            else
            {
                // Ultimates bloqueadas por MISSÃO (Domo, Tempestade) mostram "BLOQUEADO";
                // as realmente indisponíveis continuam "INDISPONÍVEL".
                string nomeUlt = NormalizarUlt(ud.GetDisplayName() + " " + ud.ultimateName + " " + ud.name);
                bool ehMissao = nomeUlt.Contains("domo retardante") || nomeUlt.Contains("tempestade") || nomeUlt.Contains("necropole") || nomeUlt.Contains("drenagem");
                MarcarBotaoIndisponivel(botao, ehMissao);
            }

            botoesUltimate.Add(botao);
            botoesUltimateOrig.Add(capturedIndex);
            botoesUltimateDisp.Add(disponivel);
            disp++;
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
        txtNome.text      = ud.GetDisplayName();
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
        txtCD.text      = $"{Loc.T("ui.cd")}:{ud.cooldown}s  |  {ud.duration}s";
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
        txtDesc.text             = ud.GetDisplayDescription();
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
        txtNome.text = ud.GetDisplayName();
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
        txtElem.text = $"{ud.GetElementIcon()} {Loc.T("element." + ud.element.ToString().ToLower())}  |  {Loc.T("ui.cd")}: {ud.cooldown}s";
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
        txtDesc.text = ud.GetDisplayDescription();
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
            if (i < botoesUltimateDisp.Count && !botoesUltimateDisp[i]) continue; // indisponível: mantém apagado
            int orig = (i < botoesUltimateOrig.Count) ? botoesUltimateOrig[i] : i;
            bool sel = orig == ultimateSelecionadaIndex;

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

    // ultimate liberada se o nome casar com a lista permitida
    bool UltimateLiberadaSel(UltimateData u)
    {
        if (u == null) return false;
        string nome = NormalizarUlt(u.GetDisplayName() + " " + u.ultimateName + " " + u.name);
        // Domo Retardante fica BLOQUEADA até completar a missão (matar Princesa Slime 2x)
        if (nome.Contains("domo retardante")) return MissaoDomoManager.DomoDesbloqueado;
        // Tempestade Elétrica fica BLOQUEADA até concluir 3 eventos de Tempestade
        if (nome.Contains("tempestade")) return MissaoTempestadeManager.Desbloqueada;
        // Necrópole fica BLOQUEADA até eliminar 500 fantasmas
        if (nome.Contains("necropole")) return MissaoNecropoleManager.Desbloqueada;
        // Drenagem de Vida fica BLOQUEADA até eliminar 500 slimes curandeiras
        if (nome.Contains("drenagem")) return MissaoDrenagemManager.Desbloqueada;
        foreach (var kw in ultimatesLiberadasSel) if (nome.Contains(kw)) return true;
        return false;
    }

    // Passivas bloqueadas por MISSÃO. Retorna true se 'pd' é uma delas;
    // 'desbloqueada' informa se a missão já foi concluída.
    bool PassivaMissao(PassiveData pd, out bool desbloqueada)
    {
        desbloqueada = false;
        if (pd == null) return false;
        string n = NormalizarUlt(pd.GetDisplayName() + " " + pd.name);
        if (n.Contains("robusto")) { desbloqueada = MissaoCoracaoManager.Desbloqueada; return true; } // Coração Robusto: 150 inimigos
        if (n.Contains("cacador")) { desbloqueada = MissaoCacadorManager.Desbloqueada; return true; }  // Caçador: 200 slimes corrompidas
        if (n.Contains("asceta"))  { desbloqueada = MissaoAscetaManager.Desbloqueada;  return true; }  // Asceta: concluir a primeira área
        return false;
    }

    // Aplica visual de "indisponível" num botão (apagado + não clicável) + rótulo "INDISPONÍVEL".
    void MarcarBotaoIndisponivel(GameObject botao, bool bloqueadoPorMissao = false)
    {
        var btn = botao.GetComponent<Button>();
        if (btn != null) btn.interactable = false;
        var img = botao.GetComponent<Image>();
        if (img != null) img.color = new Color(0.07f, 0.06f, 0.09f, 1f);
        foreach (var t in botao.GetComponentsInChildren<TextMeshProUGUI>(true))
            t.color = new Color(0.34f, 0.32f, 0.37f, 1f);

        // overlay escuro + rótulo "INDISPONÍVEL" por cima
        var ov = new GameObject("OverlayIndisponivel");
        ov.transform.SetParent(botao.transform, false);
        var rov = ov.AddComponent<RectTransform>();
        rov.anchorMin = Vector2.zero; rov.anchorMax = Vector2.one;
        rov.offsetMin = rov.offsetMax = Vector2.zero;
        var ovImg = ov.AddComponent<Image>();
        ovImg.color = new Color(0.03f, 0.02f, 0.04f, 0.62f);
        ovImg.raycastTarget = false;

        var txtGO = new GameObject("Txt");
        txtGO.transform.SetParent(ov.transform, false);
        var rtx = txtGO.AddComponent<RectTransform>();
        rtx.anchorMin = Vector2.zero; rtx.anchorMax = Vector2.one;
        rtx.offsetMin = rtx.offsetMax = Vector2.zero;
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        // Traduzido: terrain.locked = "BLOQUEADO" (destrancável por missão, dourado);
        //            terrain.unavailable = "INDISPONÍVEL" (permanente, vermelho).
        tmp.text = bloqueadoPorMissao ? Loc.T("terrain.locked") : Loc.T("terrain.unavailable");
        tmp.fontSize = 15f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = bloqueadoPorMissao ? new Color(1f, 0.82f, 0.3f, 1f) : new Color(0.85f, 0.32f, 0.32f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
    }

    // Ordem de exibição das ultimates: primeiro a sequência pedida, depois o resto
    // (pseudo-aleatório estável). Retorna índices ORIGINAIS do array.
    List<int> OrdenarUltimatesSel(UltimateData[] ults)
    {
        string[] pref = ultimatesLiberadasSel;

        var restante = new List<int>();
        for (int i = 0; i < ults.Length; i++) restante.Add(i);

        var ordem = new List<int>();
        foreach (var kw in pref)
        {
            for (int j = 0; j < restante.Count; j++)
            {
                int idx = restante[j];
                var u = ults[idx];
                string nome = u != null ? NormalizarUlt(u.GetDisplayName() + " " + u.ultimateName + " " + u.name) : "";
                if (nome.Contains(kw)) { ordem.Add(idx); restante.RemoveAt(j); break; }
            }
        }

        var rng = new System.Random(7321);
        for (int i = restante.Count - 1; i > 0; i--)
        {
            int k = rng.Next(i + 1);
            int tmp = restante[i]; restante[i] = restante[k]; restante[k] = tmp;
        }
        ordem.AddRange(restante);
        return ordem;
    }

    static string NormalizarUlt(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.ToLowerInvariant();
        var sb = new System.Text.StringBuilder();
        foreach (char c in s.Normalize(System.Text.NormalizationForm.FormD))
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString();
    }
}
