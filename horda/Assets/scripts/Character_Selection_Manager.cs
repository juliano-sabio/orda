using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CharacterSelectionManagerIntegrated : MonoBehaviour
{
    [Header("🎯 Banco de Dados")]
    public CharacterData[] characters;
    public StageData[] stages;

    // Painel de seleção de ultimate (criado em runtime)
    private GameObject painelUltimates;
    private int ultimateSelecionadaIndex = 0;
    private List<GameObject> botoesUltimate = new List<GameObject>();

    [Header("📊 UI - Info")]
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI characterElementText;
    public TextMeshProUGUI characterDescriptionText;
    public TextMeshProUGUI elementBonusText;

    [Header("🔋 UI - Status")]
    public Slider[] statusSliders;

    [Header("⚔️ Sistema de Upgrades")]
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
        UpdateStatusDisplay(characters[index]);
        AtualizarPainelUltimates(characters[index]);
    }

    public void SelectUltimate(int index)
    {
        int charIndex = PlayerPrefs.GetInt("SelectedCharacter", 0);
        ultimateSelecionadaIndex = index;
        PlayerPrefs.SetInt($"SelectedUltimate_{charIndex}", index);
        PlayerPrefs.Save();
        AtualizarDestaqueBotoes();
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
        characterDescriptionText.text = data.description;
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
    }

    public void ToggleStages(bool open) => painelStages.SetActive(open);

    private void LoadProgress()
    {
        for (int i = 0; i < 4; i++) upgradeLevels[i] = PlayerPrefs.GetInt($"Upgrade_{i}", 0);
    }

    private void UpdateCurrencyUI()
    {
        if (coinsText) coinsText.text = $"💰 {PlayerPrefs.GetInt("PlayerCoins", 1000)}";
    }

    // ─── Painel de Seleção de Ultimate ───────────────────────────────────────

    void CriarPainelUltimates()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        painelUltimates = new GameObject("PainelUltimates");
        painelUltimates.transform.SetParent(canvas.transform, false);

        RectTransform rect = painelUltimates.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot     = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 20f);
        rect.sizeDelta = new Vector2(700f, 160f);

        Image bg = painelUltimates.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.15f, 0.92f);

        // Título
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

        // Remove botões antigos
        foreach (var b in botoesUltimate) if (b) Destroy(b);
        botoesUltimate.Clear();

        if (!data.HasUltimatesDisponiveis())
        {
            painelUltimates.SetActive(false);
            return;
        }

        painelUltimates.SetActive(true);

        int total = data.ultimatesDisponiveis.Length;
        float larguraBotao = 200f;
        float espacamento = 20f;
        float totalWidth = total * larguraBotao + (total - 1) * espacamento;
        float startX = -totalWidth / 2f + larguraBotao / 2f;

        for (int i = 0; i < total; i++)
        {
            UltimateData ud = data.ultimatesDisponiveis[i];
            if (ud == null) continue;

            int capturedIndex = i;
            GameObject botao = CriarBotaoUltimate(ud, startX + i * (larguraBotao + espacamento));
            botao.GetComponent<Button>().onClick.AddListener(() => SelectUltimate(capturedIndex));
            botoesUltimate.Add(botao);
        }

        AtualizarDestaqueBotoes();
    }

    GameObject CriarBotaoUltimate(UltimateData ud, float posX)
    {
        GameObject go = new GameObject($"BotaoUltimate_{ud.ultimateName}");
        go.transform.SetParent(painelUltimates.transform, false);

        RectTransform r = go.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f, 0.5f); r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = new Vector2(posX, -15f);
        r.sizeDelta = new Vector2(195f, 110f);

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.25f, 1f);

        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.2f, 0.3f, 0.6f, 1f);
        cb.pressedColor = new Color(0.1f, 0.2f, 0.4f, 1f);
        btn.colors = cb;
        btn.targetGraphic = img;

        // Nome
        GameObject goNome = new GameObject("Nome");
        goNome.transform.SetParent(go.transform, false);
        RectTransform rNome = goNome.AddComponent<RectTransform>();
        rNome.anchorMin = new Vector2(0f, 1f); rNome.anchorMax = new Vector2(1f, 1f);
        rNome.pivot = new Vector2(0.5f, 1f);
        rNome.anchoredPosition = new Vector2(0f, -6f);
        rNome.sizeDelta = new Vector2(-8f, 22f);
        TextMeshProUGUI txtNome = goNome.AddComponent<TextMeshProUGUI>();
        txtNome.text = ud.ultimateName;
        txtNome.fontSize = 13f;
        txtNome.fontStyle = FontStyles.Bold;
        txtNome.color = ud.GetElementColor();
        txtNome.alignment = TextAlignmentOptions.Center;

        // Elemento
        GameObject goElem = new GameObject("Elemento");
        goElem.transform.SetParent(go.transform, false);
        RectTransform rElem = goElem.AddComponent<RectTransform>();
        rElem.anchorMin = new Vector2(0f, 1f); rElem.anchorMax = new Vector2(1f, 1f);
        rElem.pivot = new Vector2(0.5f, 1f);
        rElem.anchoredPosition = new Vector2(0f, -30f);
        rElem.sizeDelta = new Vector2(-8f, 18f);
        TextMeshProUGUI txtElem = goElem.AddComponent<TextMeshProUGUI>();
        txtElem.text = $"{ud.GetElementIcon()} {ud.element}  |  CD: {ud.cooldown}s";
        txtElem.fontSize = 10f;
        txtElem.color = new Color(0.8f, 0.8f, 0.8f);
        txtElem.alignment = TextAlignmentOptions.Center;

        // Descrição
        GameObject goDesc = new GameObject("Desc");
        goDesc.transform.SetParent(go.transform, false);
        RectTransform rDesc = goDesc.AddComponent<RectTransform>();
        rDesc.anchorMin = new Vector2(0f, 0f); rDesc.anchorMax = new Vector2(1f, 1f);
        rDesc.offsetMin = new Vector2(6f, 6f); rDesc.offsetMax = new Vector2(-6f, -52f);
        TextMeshProUGUI txtDesc = goDesc.AddComponent<TextMeshProUGUI>();
        txtDesc.text = ud.description;
        txtDesc.fontSize = 9.5f;
        txtDesc.color = new Color(0.75f, 0.75f, 0.75f);
        txtDesc.alignment = TextAlignmentOptions.Center;
        txtDesc.enableWordWrapping = true;
        txtDesc.overflowMode = TextOverflowModes.Truncate;

        return go;
    }

    void AtualizarDestaqueBotoes()
    {
        for (int i = 0; i < botoesUltimate.Count; i++)
        {
            if (botoesUltimate[i] == null) continue;
            Image img = botoesUltimate[i].GetComponent<Image>();
            if (img == null) continue;
            img.color = i == ultimateSelecionadaIndex
                ? new Color(0.15f, 0.35f, 0.7f, 1f)
                : new Color(0.1f, 0.1f, 0.25f, 1f);
        }
    }
}
