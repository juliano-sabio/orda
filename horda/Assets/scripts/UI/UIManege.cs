using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Painéis UI")]
    public GameObject skillAcquiredPanel;
    public GameObject statusPanel;
    public GameObject skillSelectionPanel;
    public GameObject statusCardPanel;
    public GameObject elementAdvantagePanel;

    [Header("HUD Elements")]
    public Image dashIcon;
    public TextMeshProUGUI dashChargesText;
    public Slider healthBar;
    public Slider xpSlider;
    public Slider ultimateChargeBar;
    public GameObject ultimateReadyEffect;

    [Header("Textos (TextMeshPro)")]
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI xpText;
    public TextMeshProUGUI ultimateChargeText;
    public TextMeshProUGUI currentElementText;
    public TextMeshProUGUI skillNameText;
    public TextMeshProUGUI skillDescriptionText;
    public TextMeshProUGUI advantageText;
    public TextMeshProUGUI disadvantageText;
    public TextMeshProUGUI availableSkillsText;
    public TextMeshProUGUI xpGainText;

    [Header("Status Texts (TextMeshPro)")]
    public TextMeshProUGUI damageText;
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI defenseText;
    public TextMeshProUGUI maxHealthStatusText; // Vida Máxima no painel
    public TextMeshProUGUI critChanceText;      // Chance de Crítico
    public TextMeshProUGUI healthRegenText;
    public TextMeshProUGUI shieldCooldownText;
    public TextMeshProUGUI attackSpeedText;
    public TextMeshProUGUI elementInfoText;
    public TextMeshProUGUI inventoryText;
    public TextMeshProUGUI attackSkillsText;
    public TextMeshProUGUI defenseSkillsText;
    public TextMeshProUGUI ultimateSkillsText;
    public TextMeshProUGUI statusPointsText;
    public TextMeshProUGUI activeBonusesText;

    [Header("🛡 Passiva Equipada")]
    public Image         passivaIcon;
    public TextMeshProUGUI passivaLabel;

    [Header("🎯 BARRA DE HABILIDADES - Slots de Ícones")]
    public Image attackSkill1Icon;
    public Image attackSkill2Icon;
    public Image defenseSkill1Icon;
    public Image defenseSkill2Icon;
    public Image ultimateSkillIcon;
    public Image elementIcon;

    [Header("Element Icons")]
    public Image attackSkill1ElementIcon;
    public Image attackSkill2ElementIcon;
    public Image defenseSkill1ElementIcon;
    public Image defenseSkill2ElementIcon;
    public Image ultimateSkillElementIcon;

    [Header("Cooldown Texts (TextMeshPro)")]
    public TextMeshProUGUI attackCooldownText1;
    public TextMeshProUGUI attackCooldownText2;
    public TextMeshProUGUI defenseCooldownText1;
    public TextMeshProUGUI defenseCooldownText2;
    public TextMeshProUGUI ultimateCooldownText;

    [Header("🎯 SKILL MANAGER UI - Conexão com Skill Equipada")]
    public Image skillManagerMainIcon;
    public Image skillManagerElementBackground;
    public TextMeshProUGUI skillManagerSkillName;
    public TextMeshProUGUI skillManagerElementText;
    public GameObject equippedSkillHighlight;
    public TextMeshProUGUI equippedSkillStatsText;

    [Header("⏸️ Referência do Pause Manager")]
    public PauseManager pauseManager;

    [Header("Containers")]
    public Transform skillButtonContainer;
    public Transform statusCardContainer;
    public GameObject skillButtonPrefab;
    public GameObject statusCardPrefab;

    [Header("Configurações")]
    public float xpTextDisplayTime = 2f;
    public Sprite defaultSkillIcon;

    [Header("🎨 Cores por Elemento")]
    public Color fireColor = new Color(1f, 0.3f, 0.1f);
    public Color iceColor = new Color(0.1f, 0.5f, 1f);
    public Color lightningColor = new Color(0.8f, 0.8f, 0.1f);
    public Color poisonColor = new Color(0.5f, 0.1f, 0.8f);
    public Color earthColor = new Color(0.6f, 0.4f, 0.2f);
    public Color windColor = new Color(0.4f, 0.8f, 0.9f);
    public Color defaultColor = new Color(0.2f, 0.2f, 0.3f);

    private PlayerStats playerStats;
    private SkillManager skillManager;
    private StatusCardSystem cardSystem;
    private bool statusPanelVisible = false;
    private bool skillSelectionPanelVisible = false;
    private bool statusCardPanelVisible = false;
    private Coroutine currentPulseCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (gameObject.name == "UIManager_Canvas")
        {
            // UIManager_Canvas tem prioridade — destrói o singleton antigo
            Destroy(Instance.gameObject);
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        playerStats = PlayerStats.Local;
        skillManager = FindAnyObjectByType<SkillManager>();
        cardSystem = FindAnyObjectByType<StatusCardSystem>();

        // 🆕 ENCONTRAR PAUSE MANAGER
        FindPauseManager();

        InitializeUI();
        SetupStatusPanel();
        StartCoroutine(AtualizarIconesComDelay());

        ConnectToEquippedSkill();

        // Subscreve ao evento de skill adquirida para atualizar os slots
        if (skillManager != null)
            skillManager.OnSkillAcquired += OnSkillAdquiridaHUD;


        if (passivaIcon == null)
            CriarSlotPassivaRuntime();

        // Adiciona display de cooldown automaticamente
        if (GetComponent<SkillCooldownDisplay>() == null)
            gameObject.AddComponent<SkillCooldownDisplay>();

        SkillTooltipHUD.ObterOuCriar();

        // Melhoria visual da barra de vida
        if (healthBar != null && healthBar.GetComponent<PlayerHealthBarFX>() == null)
            healthBar.gameObject.AddComponent<PlayerHealthBarFX>();
    }

    IEnumerator AtualizarIconesComDelay()
    {
        yield return null; // aguarda 1 frame para PlayerStats.Start() terminar
        if (playerStats == null) playerStats = PlayerStats.Local;
        UpdateSkillIcons();
    }

    void CriarSlotPassivaRuntime()
    {
        if (attackSkill1Icon == null) return;

        Transform parent = attackSkill1Icon.transform.parent;
        if (parent == null) return;

        GameObject slot = new GameObject("SlotPassiva");
        slot.transform.SetParent(parent, false);

        RectTransform slotRect = slot.AddComponent<RectTransform>();
        slotRect.sizeDelta = new Vector2(52, 52);

        Vector2 basePos = attackSkill1Icon.rectTransform.anchoredPosition;
        slotRect.anchoredPosition = basePos + new Vector2(-130, 0);

        Image bg = slot.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.18f, 0.05f, 0.92f);

        GameObject border = new GameObject("Borda");
        border.transform.SetParent(slot.transform, false);
        RectTransform borderRect = border.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.sizeDelta = new Vector2(2, 2);
        borderRect.anchoredPosition = Vector2.zero;
        Image borderImg = border.AddComponent<Image>();
        borderImg.color = new Color(0.1f, 0.65f, 0.1f, 0.55f);

        GameObject iconGO = new GameObject("IconePassiva");
        iconGO.transform.SetParent(slot.transform, false);
        RectTransform iconRect = iconGO.AddComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(38, 38);
        iconRect.anchoredPosition = new Vector2(0, 5);
        passivaIcon = iconGO.AddComponent<Image>();
        passivaIcon.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        GameObject labelGO = new GameObject("LabelPassiva");
        labelGO.transform.SetParent(slot.transform, false);
        RectTransform labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(52, 12);
        labelRect.anchoredPosition = new Vector2(0, -20);
        passivaLabel = labelGO.AddComponent<TextMeshProUGUI>();
        passivaLabel.fontSize = 6.5f;
        passivaLabel.alignment = TextAlignmentOptions.Center;
        passivaLabel.color = new Color(0.5f, 1f, 0.5f, 1f);
        passivaLabel.text = Loc.T("ui.passive");

        if (dashIcon != null)
            StartCoroutine(AlinhararComDash(slotRect));
    }

    private IEnumerator AlinhararComDash(RectTransform slotRect)
    {
        yield return new WaitForEndOfFrame();
        if (dashIcon == null || slotRect == null) yield break;

        RectTransform parentRect = slotRect.parent as RectTransform;
        if (parentRect == null) yield break;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, dashIcon.transform.position, null, out Vector2 local);

        Vector2 pos = slotRect.anchoredPosition;
        slotRect.anchoredPosition = new Vector2(pos.x, local.y);
    }

    // 🆕 MÉTODO PARA ENCONTRAR/CRIAR PAUSE MANAGER
    // 🆕 MÉTODO PARA ENCONTRAR/CRIAR PAUSE MANAGER
    private void FindPauseManager()
    {
        pauseManager = FindAnyObjectByType<PauseManager>();
        if (pauseManager == null)
        {

            // Criar um GameObject para o PauseManager
            GameObject pauseManagerGO = new GameObject("PauseManager");
            pauseManager = pauseManagerGO.AddComponent<PauseManager>();

            // 🆕 AGORA SIM: Configurar DontDestroyOnLoad (apenas em runtime)
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(pauseManagerGO);
            }
            else
            {
            }
        }
        else
        {
            // 🆕 Garantir DontDestroyOnLoad se já existir
            if (Application.isPlaying && !pauseManager.gameObject.scene.IsValid())
            {
                DontDestroyOnLoad(pauseManager.gameObject);
            }
        }
    }
    // --- INICIALIZAÇÃO DA UI (CHAMADA NO START) ---
    void InitializeUI()
    {
        // Ícone do dash — carrega em runtime se não foi atribuído no editor
        if (dashIcon != null && dashIcon.sprite == null)
        {
            Sprite s = Resources.Load<Sprite>("DashIconBota");
            if (s == null) s = Resources.Load<Sprite>("DashIcon");
            if (s != null) { dashIcon.sprite = s; dashIcon.color = Color.white; dashIcon.preserveAspect = true; }
        }
        if (dashIcon != null)
            SkillTooltipHUD.AttachRawPublic(dashIcon, "Dash", "Movimentação rápida.\nConsome uma carga e recarrega com o tempo.",
                new Color(0.4f, 0.75f, 1f), "DASH");

        // Atualiza as barras de vida e XP logo no início
        UpdatePlayerStatus();

        // 1. Esconde todos os painéis para não começarem abertos na tela
        if (skillAcquiredPanel != null) skillAcquiredPanel.SetActive(false);
        if (statusPanel != null) statusPanel.SetActive(false);
        if (skillSelectionPanel != null) skillSelectionPanel.SetActive(false);
        if (statusCardPanel != null) statusCardPanel.SetActive(false);
        if (elementAdvantagePanel != null) elementAdvantagePanel.SetActive(false);
        if (xpGainText != null) xpGainText.gameObject.SetActive(false);

    } // Final da função

    void Update()
    {
        // 🆕 VERIFICAR SE O JOGO ESTÁ PAUSADO ANTES DE PROCESSAR INPUT
        if (IsGamePaused()) return;

        HandleInput();
        UpdateSkillCooldowns();

        if (playerStats != null)
        {
            UpdatePlayerStatus();
        }
    }

    // 🆕 MÉTODO PARA VERIFICAR SE O JOGO ESTÁ PAUSADO
    // Remove acentos para compatibilidade com fontes sem glifos portugueses
    static string RemoverAcentos(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return texto;
        var sb = new System.Text.StringBuilder(texto.Length);
        foreach (char c in texto)
        {
            switch (c)
            {
                case 'Á': case 'À': case 'Â': case 'Ã': sb.Append('A'); break;
                case 'É': case 'Ê':                     sb.Append('E'); break;
                case 'Í':                               sb.Append('I'); break;
                case 'Ó': case 'Ô': case 'Õ':           sb.Append('O'); break;
                case 'Ú':                               sb.Append('U'); break;
                case 'Ç':                               sb.Append('C'); break;
                case 'á': case 'à': case 'â': case 'ã': sb.Append('a'); break;
                case 'é': case 'ê':                     sb.Append('e'); break;
                case 'í':                               sb.Append('i'); break;
                case 'ó': case 'ô': case 'õ':           sb.Append('o'); break;
                case 'ú':                               sb.Append('u'); break;
                case 'ç':                               sb.Append('c'); break;
                default:                                sb.Append(c);  break;
            }
        }
        return sb.ToString();
    }

    private bool IsGamePaused()
    {
        return pauseManager != null && pauseManager.IsGamePaused();
    }

    // 🎮 CONTROLES DE SKILL EQUIPADA
    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleStatusPanel();
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            ToggleSkillSelectionPanel();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleStatusCardPanel();
        }

        // 🆕 CONTROLES PARA TROCAR SKILL EQUIPADA
        if (Input.GetKeyDown(KeyCode.Q))
        {
            PreviousSkill();
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            NextSkill();
        }
        if (Input.GetKeyDown(KeyCode.Alpha1) && skillManager != null && skillManager.GetActiveSkills().Count > 0)
        {
            EquipSkillByIndex(0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) && skillManager != null && skillManager.GetActiveSkills().Count > 1)
        {
            EquipSkillByIndex(1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3) && skillManager != null && skillManager.GetActiveSkills().Count > 2)
        {
            EquipSkillByIndex(2);
        }
    }

    // 🆕 SISTEMA DE SKILL EQUIPADA
    public void ConnectToEquippedSkill()
    {
        if (skillManager == null) return;

        skillManager.OnSkillEquippedChanged += OnSkillEquippedChanged;

        var equippedSkill = skillManager.GetEquippedSkill();
        if (equippedSkill != null)
        {
            UpdateSkillManagerWithEquippedSkill(equippedSkill);
        }
        else if (skillManager.GetActiveSkills().Count > 0)
        {
            skillManager.EquipSkill(skillManager.GetActiveSkills()[0]);
        }
    }

    private void OnSkillEquippedChanged(SkillData equippedSkill)
    {
        UpdateSkillManagerWithEquippedSkill(equippedSkill);
    }

    private void UpdateSkillManagerWithEquippedSkill(SkillData equippedSkill)
    {
        if (equippedSkill == null)
        {
            SetDefaultSkillManagerAppearance();
            return;
        }

        // 🎨 ATUALIZAR SKILL MANAGER UI
        if (skillManagerMainIcon != null)
        {
            skillManagerMainIcon.sprite = equippedSkill.icon ?? defaultSkillIcon;
            skillManagerMainIcon.color = Color.white;
            StartCoroutine(PulseIcon(skillManagerMainIcon));
        }

        if (skillManagerElementBackground != null)
        {
            Color elementColor = GetElementColor(equippedSkill.element);
            skillManagerElementBackground.color = elementColor;
        }

        if (skillManagerSkillName != null)
        {
            skillManagerSkillName.text = TextUtils.SemAcento(equippedSkill.GetDisplayName());
            skillManagerSkillName.color = GetElementColor(equippedSkill.element);
        }

        if (skillManagerElementText != null)
        {
            skillManagerElementText.text = $"{equippedSkill.GetElementIcon()} {equippedSkill.element}";
            skillManagerElementText.color = GetElementColor(equippedSkill.element);
        }

        if (equippedSkillStatsText != null)
        {
            equippedSkillStatsText.text = GetSkillStatsText(equippedSkill);
        }

        if (equippedSkillHighlight != null)
        {
            equippedSkillHighlight.SetActive(true);
            StartCoroutine(HighlightEffect(equippedSkillHighlight));
        }

        // 🆕 ATUALIZAR SKILL HUD!
        UpdateSkillHUDWithEquippedSkill(equippedSkill);

    }

    // 🆕 MÉTODO PARA ATUALIZAR O SKILL HUD COM A SKILL EQUIPADA
    public void UpdateSkillHUDWithEquippedSkill(SkillData equippedSkill)
    {
        if (equippedSkill == null) return;


        // 🎯 DEFINIR EM QUAL SLOT DA HUD COLOCAR A SKILL EQUIPADA
        Image targetSlot = attackSkill1Icon;
        Image targetElementSlot = attackSkill1ElementIcon;
        TextMeshProUGUI targetCooldown = attackCooldownText1;

        if (targetSlot != null)
        {
            // 🖼️ ATUALIZAR ÍCONE PRINCIPAL
            targetSlot.sprite = equippedSkill.icon ?? defaultSkillIcon;
            targetSlot.color = Color.white;
            targetSlot.gameObject.SetActive(true);

            // 🎨 ATUALIZAR COR DO ELEMENTO — só mostra o quadradinho quando há infusão
            // (antes forçava SetActive(true) com cor do elemento-base → quadrado branco sem infusão).
            if (targetElementSlot != null)
            {
                if (equippedSkill.appliedElement != ElementType.None)
                {
                    Color cor = ElementRegistry.Instance?.GetCor(equippedSkill.appliedElement) ?? Color.white;
                    targetElementSlot.color = cor;
                    var def = ElementRegistry.Instance?.Get(equippedSkill.appliedElement);
                    if (def?.icone != null) targetElementSlot.sprite = def.icone;
                    targetElementSlot.gameObject.SetActive(true);
                }
                else
                {
                    targetElementSlot.gameObject.SetActive(false);
                }
            }

            // ⚡ EFEITO VISUAL DE DESTAQUE
            StartCoroutine(HighlightSkillSlotCoroutine(targetSlot));
        }

        if (targetCooldown != null)
        {
            float cd = playerStats != null ? playerStats.GetAttackActivationInterval() : equippedSkill.cooldown;
            targetCooldown.text  = $"{cd:F1}s";
            targetCooldown.color = Color.yellow;
        }

        // ✨ ATUALIZAR ELEMENTO PRINCIPAL NA HUD
        UpdateElementIcon();

    }

    // 🆕 MÉTODO PARA DESTACAR O SLOT DA SKILL EQUIPADA
    private IEnumerator HighlightSkillSlotCoroutine(Image slot)
    {
        if (slot == null) yield break;

        float duration = 1.5f;
        float elapsed = 0f;
        Vector3 originalScale = slot.transform.localScale;
        Color originalColor = slot.color;

        while (elapsed < duration)
        {
            // Efeito de pulso
            float pulse = Mathf.PingPong(elapsed * 4f, 0.2f);
            slot.transform.localScale = originalScale * (1f + pulse);

            // Efeito de brilho
            float glow = Mathf.PingPong(elapsed * 3f, 0.3f);
            slot.color = Color.Lerp(originalColor, Color.white, glow);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Restaurar valores originais
        slot.transform.localScale = originalScale;
        slot.color = originalColor;
    }

    // 🆕 MÉTODO PARA LIMPAR SLOT DA HUD
    private void ClearSkillHUDSlot(Image mainIcon, Image elementIcon, TextMeshProUGUI cooldownText)
    {
        if (mainIcon != null)
        {
            mainIcon.sprite = defaultSkillIcon;
            mainIcon.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        }

        if (elementIcon != null)
        {
            elementIcon.gameObject.SetActive(false);
        }

        if (cooldownText != null)
        {
            cooldownText.text = Loc.T("ui.empty");
            cooldownText.color = Color.gray;
        }
    }

    private string GetSkillStatsText(SkillData skill)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        if (skill.attackBonus != 0) sb.AppendLine($"{Loc.T("stat.atk")}: +{skill.attackBonus}");
        if (skill.defenseBonus != 0) sb.AppendLine($"{Loc.T("stat.def")}: +{skill.defenseBonus}");
        if (skill.healthBonus != 0) sb.AppendLine($"{Loc.T("stat.hp")}: +{skill.healthBonus}");
        if (skill.speedBonus != 0) sb.AppendLine($"{Loc.T("stat.spd")}: +{skill.speedBonus}");

        if (sb.Length == 0) sb.AppendLine(Loc.T("ui.passive_bonus"));

        return sb.ToString();
    }

    private void SetDefaultSkillManagerAppearance()
    {
        if (skillManagerMainIcon != null)
        {
            skillManagerMainIcon.sprite = defaultSkillIcon;
            skillManagerMainIcon.color = Color.gray;
        }

        if (skillManagerElementBackground != null)
        {
            skillManagerElementBackground.color = defaultColor;
        }

        if (skillManagerSkillName != null)
        {
            skillManagerSkillName.text = Loc.T("ui.no_skill_equipped");
            skillManagerSkillName.color = Color.gray;
        }

        if (skillManagerElementText != null)
        {
            skillManagerElementText.text = "⚪ " + Loc.T("ui.select_skill");
            skillManagerElementText.color = Color.gray;
        }

        if (equippedSkillStatsText != null)
        {
            equippedSkillStatsText.text = Loc.T("ui.equip_skill_hint");
        }

        if (equippedSkillHighlight != null)
        {
            equippedSkillHighlight.SetActive(false);
        }

        // 🆕 LIMPAR SLOT DA HUD QUANDO NÃO HÁ SKILL EQUIPADA
        ClearSkillHUDSlot(attackSkill1Icon, attackSkill1ElementIcon, attackCooldownText1);
    }

    // 🎮 MÉTODOS DE CONTROLE
    public void NextSkill()
    {
        if (skillManager != null)
        {
            skillManager.CycleEquippedSkill();
        }
    }

    public void PreviousSkill()
    {
        if (skillManager != null && skillManager.GetActiveSkills().Count > 0)
        {
            skillManager.selectedSkillIndex = (skillManager.selectedSkillIndex - 1 + skillManager.GetActiveSkills().Count) % skillManager.GetActiveSkills().Count;
            skillManager.EquipSkill(skillManager.GetActiveSkills()[skillManager.selectedSkillIndex]);
        }
    }

    public void EquipSkillByIndex(int index)
    {
        if (skillManager != null && index >= 0 && index < skillManager.GetActiveSkills().Count)
        {
            skillManager.EquipSkill(skillManager.GetActiveSkills()[index]);
        }
    }

    // 🎭 EFEITOS VISUAIS
    private IEnumerator PulseIcon(Image icon)
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 originalScale = icon.transform.localScale;

        while (elapsed < duration)
        {
            float pulse = Mathf.PingPong(elapsed * 4f, 0.3f);
            icon.transform.localScale = originalScale * (1f + pulse);
            elapsed += Time.deltaTime;
            yield return null;
        }

        icon.transform.localScale = originalScale;
    }

    private IEnumerator HighlightEffect(GameObject highlight)
    {
        float duration = 1.5f;
        float elapsed = 0f;

        if (highlight.TryGetComponent<Image>(out var image))
        {
            Color originalColor = image.color;

            while (elapsed < duration)
            {
                float alpha = Mathf.PingPong(elapsed * 2f, 0.5f);
                image.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                elapsed += Time.deltaTime;
                yield return null;
            }

            image.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        }
    }

    public void SetPassivaIcon(Sprite icon, string nome, string desc = "")
    {
        if (passivaIcon == null) return;
        if (icon != null)
        {
            passivaIcon.sprite  = icon;
            passivaIcon.color   = Color.white;
            passivaIcon.gameObject.SetActive(true);
        }
        else
        {
            passivaIcon.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            passivaIcon.gameObject.SetActive(true);
        }
        if (passivaLabel != null) passivaLabel.text = RemoverAcentos(nome ?? "");
        SkillTooltipHUD.AttachPassiva(passivaIcon, nome ?? "", desc);
    }

    // 🎯 MÉTODOS EXISTENTES DO UIMANAGER (mantidos)
    public void UpdateSkillIcons()
    {
        if (playerStats == null) return;


        UpdateAttackSkillIcons();
        UpdateDefenseSkillIcons();
        UpdateUltimateSkillIcon();
        UpdateElementIcon();

        // 🆕 ATUALIZAR COM SKILL EQUIPADA SE EXISTIR
        var equippedSkill = skillManager?.GetEquippedSkill();
        if (equippedSkill != null)
        {
            UpdateSkillHUDWithEquippedSkill(equippedSkill);
        }
    }

    // Atualiza só os 4 slots de skill sem tocar na ultimate
    public void AtualizarSlotsSkill()
    {
        UpdateAttackSkillIcons();
        UpdateDefenseSkillIcons();
    }

    private void UpdateAttackSkillIcons()
    {
        if (skillManager == null) skillManager = FindAnyObjectByType<SkillManager>();
        var skills = skillManager?.GetActiveSkills();
        if (skills == null) return;

        // Slot 1 (índice 0) = ataque → attackSkill1Icon
        // Slot 3 (índice 2) = ataque → attackSkill2Icon
        SetSkillSlot(attackSkill1Icon, attackSkill1ElementIcon, skills.Count > 0 ? skills[0] : null);
        SetSkillSlot(attackSkill2Icon, attackSkill2ElementIcon, skills.Count > 2 ? skills[2] : null);
    }

    private void UpdateDefenseSkillIcons()
    {
        if (skillManager == null) skillManager = FindAnyObjectByType<SkillManager>();
        var skills = skillManager?.GetActiveSkills();
        if (skills == null) return;

        // Slot 2 (índice 1) = defesa → defenseSkill1Icon
        // Slot 4 (índice 3) = defesa → defenseSkill2Icon
        SetSkillSlot(defenseSkill1Icon, defenseSkill1ElementIcon, skills.Count > 1 ? skills[1] : null);
        SetSkillSlot(defenseSkill2Icon, defenseSkill2ElementIcon, skills.Count > 3 ? skills[3] : null);
    }

    private void OnSkillAdquiridaHUD(SkillData skill)
    {
        // Atualiza apenas os slots de ataque e defesa, não toca no ultimate
        UpdateAttackSkillIcons();
        UpdateDefenseSkillIcons();
    }

    private void SetSkillSlot(Image icon, Image elementIcon, SkillData skill)
    {
        if (icon == null) return;

        // Atualiza label de nome da skill no slot se existir
        var nameLabel = icon.transform.parent.Find("SkillNameText")
                        ?.GetComponent<TMPro.TextMeshProUGUI>();

        if (skill != null)
        {
            icon.sprite = skill.icon != null ? skill.icon : defaultSkillIcon;
            icon.color = Color.white;
            icon.gameObject.SetActive(true);
            if (nameLabel != null) nameLabel.text = skill.GetDisplayName();
            SkillTooltipHUD.Attach(icon, skill);

            if (elementIcon != null)
            {
                if (skill.appliedElement != ElementType.None)
                {
                    Color cor = ElementRegistry.Instance?.GetCor(skill.appliedElement) ?? Color.white;
                    elementIcon.color = cor;
                    var def = ElementRegistry.Instance?.Get(skill.appliedElement);
                    if (def?.icone != null) elementIcon.sprite = def.icone;
                    elementIcon.gameObject.SetActive(true);
                }
                else
                {
                    // Sem infusão → sem quadradinho (antes mostrava a cor do elemento-base,
                    // virando um quadrado branco quando a skill estava "sem elemento").
                    elementIcon.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            icon.sprite = defaultSkillIcon;
            icon.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            if (elementIcon != null) elementIcon.gameObject.SetActive(false);
            if (nameLabel != null) nameLabel.text = "";
        }
    }

    public void AtualizarElementoAplicado()
    {
        UpdateAttackSkillIcons();
        UpdateDefenseSkillIcons();
    }

    private void UpdateUltimateSkillIcon()
    {
        var ultimateSkill = playerStats?.GetUltimateSkill();
        if (ultimateSkill == null) return;

        if (ultimateSkillIcon != null)
        {
            if (ultimateSkill.isActive)
            {
                ultimateSkillIcon.sprite = ultimateSkill.icon != null
                    ? ultimateSkill.icon
                    : GetSkillIcon(ultimateSkill.skillName);
                ultimateSkillIcon.color = Color.white;
                ultimateSkillIcon.gameObject.SetActive(true);

                // Ultimate não é infundível (a infusão só atua em skills de ataque/defesa via
                // appliedElement) → nunca mostra o quadradinho de elemento (era o elemento-base).
                if (ultimateSkillElementIcon != null)
                    ultimateSkillElementIcon.gameObject.SetActive(false);
            }
            else
            {
                ultimateSkillIcon.sprite = defaultSkillIcon;
                ultimateSkillIcon.color = new Color(0.3f, 0.3f, 0.3f, 0.3f);
                if (ultimateSkillElementIcon != null)
                    ultimateSkillElementIcon.gameObject.SetActive(false);
            }

            SkillTooltipHUD.AttachUltimate(ultimateSkillIcon, Loc.SkillLabel(ultimateSkill.skillName),
                ultimateSkill.description ?? "", ultimateSkill.specificType);
        }
    }



    private void UpdateElementIcon()
    {
        if (elementIcon != null)
        {
            var currentElement = playerStats.GetCurrentElement();
            if (currentElement != PlayerStats.Element.None)
            {
                elementIcon.color = GetElementColor(currentElement);
                elementIcon.gameObject.SetActive(true);
            }
            else
            {
                elementIcon.gameObject.SetActive(false);
            }
        }
    }

    private Sprite GetSkillIcon(string skillName)
    {
        if (skillManager != null)
        {
            var method = skillManager.GetType().GetMethod("GetSkillIcon");
            if (method != null)
            {
                return method.Invoke(skillManager, new object[] { skillName }) as Sprite;
            }
        }

        return CreateFallbackIcon(TextUtils.SemAcento(skillName));
    }

    private Sprite CreateFallbackIcon(string skillName)
    {
        if (defaultSkillIcon != null)
            return defaultSkillIcon;

        Texture2D texture = new Texture2D(64, 64);
        Color[] pixels = new Color[64 * 64];
        Color baseColor = ColorForString(TextUtils.SemAcento(skillName));

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = baseColor;
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, 64, 64), Vector2.one * 0.5f);
    }

    private Color ColorForString(string text)
    {
        System.Random rand = new System.Random(text.GetHashCode());
        return new Color(
            (float)rand.NextDouble() * 0.7f + 0.3f,
            (float)rand.NextDouble() * 0.7f + 0.3f,
            (float)rand.NextDouble() * 0.7f + 0.3f
        );
    }

    private Color GetElementColor(PlayerStats.Element element)
    {
        switch (element)
        {
            case PlayerStats.Element.Fire: return fireColor;
            case PlayerStats.Element.Ice: return iceColor;
            case PlayerStats.Element.Lightning: return lightningColor;
            case PlayerStats.Element.Poison: return poisonColor;
            case PlayerStats.Element.Earth: return earthColor;
            case PlayerStats.Element.Wind: return windColor;
            default: return Color.white;
        }
    }

    // 📊 MÉTODOS DE UI EXISTENTES (mantidos por compatibilidade)
    public void ShowXPGained(float xpAmount)
    {
        if (xpGainText != null)
        {
            xpGainText.text = $"+{xpAmount} XP";
            xpGainText.gameObject.SetActive(true);
            StartCoroutine(HideXPGainText());
        }
    }

    private IEnumerator HideXPGainText()
    {
        yield return new WaitForSeconds(xpTextDisplayTime);

        if (xpGainText != null)
        {
            float fadeTime = 0.5f;
            float elapsedTime = 0f;
            Color originalColor = xpGainText.color;

            while (elapsedTime < fadeTime)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeTime);
                xpGainText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }

            xpGainText.gameObject.SetActive(false);
            xpGainText.color = originalColor;
        }
    }

    public void ShowStatusPointsGained(int points)
    {
        ShowSkillAcquired($"🎯 Pontos de Status", $"Ganhou {points} pontos de status!");
    }

    public void ShowStatusPointsGained(int points, string message)
    {
        ShowSkillAcquired($"🎯 {message}", $"Ganhou {points} pontos de status!");
    }

    public void ShowStatusCardApplied(string cardName, string effect)
    {
        ShowSkillAcquired($"🃏 Carta Aplicada", $"{cardName}\n{effect}");
    }

    public void UpdateStatusCardsUI()
    {
        UpdateStatusCardPanel();
    }

    public void UpdatePlayerStatus()
    {
        if (playerStats == null) return;

        if (healthBar != null)
        {
            healthBar.maxValue = playerStats.GetMaxHealth();
            healthBar.value = playerStats.GetCurrentHealth();
        }

        if (xpSlider != null)
        {
            xpSlider.maxValue = playerStats.GetXPToNextLevel();
            xpSlider.value = playerStats.GetCurrentXP();
        }

        if (ultimateChargeBar != null)
        {
            ultimateChargeBar.maxValue = playerStats.GetUltimateCooldown();
            ultimateChargeBar.value = playerStats.GetUltimateChargeTime();
        }

        if (healthText != null)
        {
            float sp = playerStats.GetShieldPoints();
            string shieldStr = sp > 0.5f ? $" +{sp:F0}" : "";
            string novoHp = $"{playerStats.GetCurrentHealth():F0}/{playerStats.GetMaxHealth():F0}{shieldStr}";
            if (healthText.text != novoHp) healthText.text = novoHp;
        }

        if (levelText != null)
        {
            string novoLvl = $"{Loc.T("ui.level_abbr")}: {playerStats.GetLevel()}";
            if (levelText.text != novoLvl) levelText.text = novoLvl;
        }

        if (xpText != null)
        {
            string novoXp = $"XP: {playerStats.GetCurrentXP():F0}/{playerStats.GetXPToNextLevel():F0}";
            if (xpText.text != novoXp) xpText.text = novoXp;
        }

        if (ultimateChargeText != null)
        {
            float chargePercent = (playerStats.GetUltimateChargeTime() / playerStats.GetUltimateCooldown()) * 100f;
            string novoTextoUlti = playerStats.ultimateBloqueada ? Loc.T("ui.ultimate_blocked") :
                playerStats.IsUltimateReady() ? Loc.T("ui.ultimate_ready") :
                $"{Loc.T("ui.ultimate")}: {Mathf.FloorToInt(chargePercent)}%";
            if (ultimateChargeText.text != novoTextoUlti)
                ultimateChargeText.text = novoTextoUlti;
        }

        if (currentElementText != null)
            currentElementText.text = $"{Loc.T("ui.elem")}: {playerStats.GetCurrentElement()}";

        if (ultimateReadyEffect != null)
            ultimateReadyEffect.SetActive(playerStats.IsUltimateReady() && !playerStats.ultimateBloqueada);

        if (dashChargesText != null)
        {
            int charges = playerStats.dashCharges;
            dashChargesText.text = charges.ToString();
            dashChargesText.color = charges > 0 ? new Color(0.4f, 0.9f, 1f) : new Color(0.5f, 0.5f, 0.5f);
        }

        if (dashIcon != null)
        {
            dashIcon.color = playerStats.dashCharges > 0
                ? Color.white
                : new Color(0.3f, 0.3f, 0.3f, 0.6f);
        }

        if (statusPanelVisible)
        {
            UpdateStatusPanel();
        }
    }

    public void UpdateSkillCooldowns()
    {
        if (playerStats == null) return;

        float remaining  = playerStats.GetAttackCooldownRemaining();
        float totalCD    = playerStats.GetAttackActivationInterval();

        if (attackCooldownText1 != null)
        {
            attackCooldownText1.text  = remaining > 0.05f ? $"{remaining:F1}s" : Loc.T("ui.ready");
            attackCooldownText1.color = remaining > 0.05f ? Color.red : Color.green;
        }

        if (attackCooldownText2 != null)
        {
            attackCooldownText2.text  = $"{totalCD:F1}s";
            attackCooldownText2.color = Color.yellow;
        }

        if (defenseCooldownText1 != null)
        {
            if (playerStats.defenseSkills.Count > 0)
            {
                var skill = playerStats.defenseSkills[0];
                defenseCooldownText1.text  = skill.IsOnCooldown ? $"{skill.currentCooldown:F1}s" : Loc.T("ui.ready");
                defenseCooldownText1.color = skill.IsOnCooldown ? Color.red : Color.green;
            }
        }

        if (defenseCooldownText2 != null)
        {
            if (playerStats.defenseSkills.Count > 1)
            {
                var skill = playerStats.defenseSkills[1];
                defenseCooldownText2.text  = skill.IsOnCooldown ? $"{skill.currentCooldown:F1}s" : Loc.T("ui.ready");
                defenseCooldownText2.color = skill.IsOnCooldown ? Color.red : Color.green;
            }
        }

        if (ultimateCooldownText != null)
        {
            if (playerStats.HasUltimate())
            {
                if (playerStats.IsUltimateReady())
                {
                    ultimateCooldownText.text      = "";   // pronta = sem texto
                    ultimateCooldownText.color     = Color.yellow;
                }
                else
                {
                    float restante = playerStats.ultimateCooldown - playerStats.ultimateChargeTime;
                    ultimateCooldownText.text  = Mathf.CeilToInt(restante).ToString();
                    ultimateCooldownText.color = restante <= 5f
                        ? new Color(1f, 0.4f, 0.1f)   // laranja quando quase pronta
                        : new Color(0.8f, 0.8f, 0.8f); // cinza claro
                    ultimateCooldownText.fontStyle = FontStyles.Bold;
                }
            }
            else
            {
                ultimateCooldownText.text  = "";
                ultimateCooldownText.color = Color.gray;
            }
        }
    }

    // ── Setup centralizado do StatusPanel ────────────────────────────────────
    private bool statusPanelSetupFeito = false;

    private void SetupStatusPanel()
    {
        if (statusPanel == null || statusPanelSetupFeito) return;
        statusPanelSetupFeito = true;

        foreach (Transform filho in statusPanel.transform)
            filho.gameObject.SetActive(false);

        var containerGO = new GameObject("StatsContent", typeof(RectTransform));
        containerGO.transform.SetParent(statusPanel.transform, false);
        var containerRT = containerGO.GetComponent<RectTransform>();
        containerRT.anchorMin = Vector2.zero;
        containerRT.anchorMax = Vector2.one;
        containerRT.offsetMin = new Vector2(20f, 18f);
        containerRT.offsetMax = new Vector2(-20f, -18f);

        var vl = containerGO.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        vl.spacing                = 8f;
        vl.childAlignment         = TextAnchor.UpperCenter;
        vl.childControlWidth      = true;
        vl.childControlHeight     = true;
        vl.childForceExpandWidth  = true;
        vl.childForceExpandHeight = false;
        vl.padding = new RectOffset(6, 6, 35, 8);

        CriarTextoStatusPanel(containerGO.transform, "STATUS", 15f,
            new Color(1f, 0.85f, 0.3f), bold: true, altura: 30f);

        CriarSeparadorStatusPanel(containerGO.transform, new Color(1f, 0.85f, 0.3f, 0.5f));

        damageText          = CriarLinhaStatusPanel(containerGO.transform, StatusCardType.Attack,         new Color(1.00f, 0.55f, 0.25f));
        defenseText         = CriarLinhaStatusPanel(containerGO.transform, StatusCardType.Defense,        new Color(0.35f, 0.75f, 1.00f));
        critChanceText      = CriarLinhaStatusPanel(containerGO.transform, StatusCardType.CriticalChance, new Color(1.00f, 0.85f, 0.20f));
        attackSpeedText     = CriarLinhaStatusPanel(containerGO.transform, StatusCardType.AttackSpeed,    new Color(0.80f, 0.40f, 1.00f));
        maxHealthStatusText = CriarLinhaStatusPanel(containerGO.transform, StatusCardType.Health,         new Color(0.30f, 1.00f, 0.45f));
        speedText           = CriarLinhaStatusPanel(containerGO.transform, StatusCardType.Speed,          new Color(0.30f, 0.90f, 1.00f));
        healthRegenText     = CriarLinhaStatusPanel(containerGO.transform, StatusCardType.Regen,          new Color(0.50f, 1.00f, 0.70f));
        shieldCooldownText  = CriarLinhaStatusPanel(containerGO.transform, StatusCardType.Shield,         new Color(0.30f, 0.85f, 1.00f));
    }

    private TextMeshProUGUI CriarLinhaStatusPanel(Transform pai, StatusCardType stat, Color cor)
    {
        var row = new GameObject("StatRow", typeof(RectTransform));
        row.transform.SetParent(pai, false);
        var le = row.AddComponent<UnityEngine.UI.LayoutElement>();
        le.preferredHeight = 30f;
        le.flexibleHeight  = 0f;

        var hl = row.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        hl.spacing                = 8f;
        hl.childAlignment         = TextAnchor.MiddleCenter;
        hl.childControlWidth      = false;
        hl.childControlHeight     = true;
        hl.childForceExpandWidth  = false;
        hl.childForceExpandHeight = true;
        hl.padding = new RectOffset(0, 0, 0, 0);

        var iconeGO = new GameObject("Icone", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        iconeGO.transform.SetParent(row.transform, false);
        iconeGO.GetComponent<RectTransform>().sizeDelta = new Vector2(24f, 24f);
        var iconeImg = iconeGO.GetComponent<UnityEngine.UI.Image>();
        iconeImg.preserveAspect = true;
        var sp = StatusCardIconGenerator.GetIcon(stat, cor);
        if (sp != null) iconeImg.sprite = sp;
        else            iconeImg.color  = cor;

        var txtGO = new GameObject("Texto", typeof(RectTransform));
        txtGO.transform.SetParent(row.transform, false);
        txtGO.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 30f);
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.fontSize  = 13f;
        txt.color     = cor;
        txt.alignment = TextAlignmentOptions.MidlineLeft;
        txt.text      = "---";

        return txt;
    }

    private void CriarTextoStatusPanel(Transform pai, string texto, float tamanho, Color cor, bool bold, float altura)
    {
        var go = new GameObject("Titulo", typeof(RectTransform));
        go.transform.SetParent(pai, false);
        var le = go.AddComponent<UnityEngine.UI.LayoutElement>();
        le.preferredHeight = altura;
        le.flexibleHeight  = 0f;
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text      = texto;
        txt.fontSize  = tamanho;
        txt.color     = cor;
        txt.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        txt.alignment = TextAlignmentOptions.Center;
    }

    private void CriarSeparadorStatusPanel(Transform pai, Color cor)
    {
        var go = new GameObject("Separador", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        go.transform.SetParent(pai, false);
        var le = go.AddComponent<UnityEngine.UI.LayoutElement>();
        le.preferredHeight = 2f;
        le.flexibleHeight  = 0f;
        go.GetComponent<UnityEngine.UI.Image>().color = cor;
    }

    public void ToggleStatusPanel()
    {
        if (statusPanel != null)
        {
            statusPanelVisible = !statusPanelVisible;
            statusPanel.SetActive(statusPanelVisible);

            if (statusPanelVisible)
            {
                UpdateStatusPanel();
            }
        }
    }

    public void UpdateStatusPanel()
    {
        if (playerStats == null || !statusPanelVisible) return;

        // ── Vida ──────────────────────────────────────────────────────
        if (maxHealthStatusText != null)
        {
            float sp = playerStats.GetShieldPoints();
            float mp = playerStats.GetMaxShieldPoints();
            string shieldStr = mp > 0f ? $" (+{sp:F0}/{mp:F0} {Loc.T("stat.shield")})" : "";
            maxHealthStatusText.text = $"{Loc.T("stat.hp")}: {playerStats.GetCurrentHealth():F0} / {playerStats.GetMaxHealth():F0}{shieldStr}";
        }

        // ── Combate ───────────────────────────────────────────────────
        if (damageText != null)
            damageText.text = $"{Loc.T("stat.atk")}: {playerStats.GetAttack():F1}";

        if (defenseText != null)
        {
            float sp = playerStats.GetShieldPoints();
            float mp = playerStats.GetMaxShieldPoints();
            string shieldStr = mp > 0f ? $" | {Loc.T("stat.shield")}: {sp:F0}/{mp:F0}" : "";
            defenseText.text = $"{Loc.T("stat.def")}: {playerStats.GetDefense():F1}{shieldStr}";
        }

        if (critChanceText != null)
            critChanceText.text = $"{Loc.T("stat.crit")}: {playerStats.GetCritChance() * 100f:F1}%";

        // ── Velocidade & Regeneração ──────────────────────────────────
        if (speedText != null)
            speedText.text = $"{Loc.T("stat.spd")}: {playerStats.GetSpeed():F1}";

        if (attackSpeedText != null)
            attackSpeedText.text = $"{Loc.T("stat.atkspd")}: {playerStats.GetAttackActivationInterval():F1}s";

        if (healthRegenText != null)
            healthRegenText.text = $"{Loc.T("stat.regen")}: {playerStats.GetHealthRegen():F1}/s";

        if (shieldCooldownText != null)
            shieldCooldownText.text = $"{Loc.T("stat.shield")}: {playerStats.GetDefenseActivationInterval():F1}s";

        // ── Level & XP ────────────────────────────────────────────────
        if (statusPointsText != null)
            statusPointsText.text = $"{Loc.T("ui.level_abbr")} {playerStats.GetLevel()}  |  XP: {playerStats.GetCurrentXP():F0} / {playerStats.GetXPToNextLevel():F0}";

        // ── Bônus Ativos ──────────────────────────────────────────────
        if (activeBonusesText != null)
        {
            var sb = new System.Text.StringBuilder();

            // Dash
            int dashes = playerStats.dashCharges;
            int maxDashes = playerStats.maxDashCharges;
            if (maxDashes > 0)
                sb.AppendLine($"Dash: {dashes}/{maxDashes}");

            // Elemento
            var elem = playerStats.GetCurrentElement();
            float bonus = playerStats.GetElementalBonus();
            if (elem.ToString() != "None")
                sb.AppendLine($"{Loc.T("ui.elem")}: {elem}  +{(bonus - 1f) * 100f:F0}%");

            // Regen ativa
            if (playerStats.IsRegeneratingHealth())
                sb.AppendLine(Loc.T("ui.regen_life"));

            activeBonusesText.text = sb.Length > 0 ? sb.ToString().TrimEnd() : Loc.T("ui.no_bonus");
        }

        // ── Elemento (resumo) ─────────────────────────────────────────
        if (elementInfoText != null)
            elementInfoText.text = $"{Loc.T("ui.elem")}: {playerStats.GetCurrentElement()}\n{Loc.T("ui.bonus")}: {playerStats.GetElementalBonus():F1}x";

        // ── Inventário ────────────────────────────────────────────────
        if (inventoryText != null)
        {
            var inventory = playerStats.GetInventory();
            string inventoryStr = inventory.Count > 0 ? string.Join(", ", inventory) : Loc.T("ui.active_bonuses_none");
            inventoryText.text = $"{Loc.T("ui.items")}: {inventoryStr}";
        }

        // ── Skills de Ataque ──────────────────────────────────────────
        if (attackSkillsText != null)
        {
            var attackSkills = playerStats.GetAttackSkills();
            if (attackSkills.Count == 0)
            {
                attackSkillsText.text = Loc.T("ui.skills_atq") + ": " + Loc.T("ui.active_bonuses_none");
            }
            else
            {
                var sb = new System.Text.StringBuilder(Loc.T("ui.skills_atq") + ":\n");
                foreach (var skill in attackSkills)
                {
                    string status = skill.isActive ? "[ON]" : "[OFF]";
                    sb.AppendLine($"{status} {TextUtils.SemAcento(Loc.SkillLabel(skill.skillName))}  {Loc.T("stat.dmg")}: {skill.baseDamage:F1}");
                }
                attackSkillsText.text = sb.ToString().TrimEnd();
            }
        }

        // ── Skills de Defesa ──────────────────────────────────────────
        if (defenseSkillsText != null)
        {
            var defenseSkills = playerStats.GetDefenseSkills();
            if (defenseSkills.Count == 0)
            {
                defenseSkillsText.text = Loc.T("ui.skills_def") + ": " + Loc.T("ui.active_bonuses_none");
            }
            else
            {
                var sb = new System.Text.StringBuilder(Loc.T("ui.skills_def") + ":\n");
                foreach (var skill in defenseSkills)
                {
                    string status = skill.isActive ? "[ON]" : "[OFF]";
                    sb.AppendLine($"{status} {TextUtils.SemAcento(Loc.SkillLabel(skill.skillName))}  {Loc.T("stat.def")}: {skill.baseDefense:F1}");
                }
                defenseSkillsText.text = sb.ToString().TrimEnd();
            }
        }

        // ── Ultimate ──────────────────────────────────────────────────
        if (ultimateSkillsText != null)
        {
            var ultimate = playerStats.GetUltimateSkill();
            if (ultimate.isActive)
            {
                string readyStr = playerStats.IsUltimateReady()
                    ? Loc.T("ui.ready")
                    : $"{Loc.T("ui.ult_loading")}... {playerStats.GetUltimateChargeTime():F1}s";
                ultimateSkillsText.text = $"{Loc.T("ui.ultimate")}: {TextUtils.SemAcento(Loc.SkillLabel(ultimate.skillName))}\n{Loc.T("stat.dmg")}: {ultimate.baseDamage:F1}  {readyStr}";
            }
            else
            {
                ultimateSkillsText.text = Loc.T("ui.ultimate_level_req");
            }
        }
    }

    public void ToggleSkillSelectionPanel()
    {
        if (skillSelectionPanel != null)
        {
            skillSelectionPanelVisible = !skillSelectionPanelVisible;
            skillSelectionPanel.SetActive(skillSelectionPanelVisible);

            if (skillSelectionPanelVisible)
            {
                UpdateSkillSelectionPanel();
            }
        }
    }

    public void UpdateSkillSelectionPanel()
    {
        if (skillManager == null || !skillSelectionPanelVisible) return;

        foreach (Transform child in skillButtonContainer)
        {
            if (child.gameObject != skillButtonPrefab)
            {
                Destroy(child.gameObject);
            }
        }

        if (availableSkillsText != null)
        {
            availableSkillsText.text = Loc.T("ui.skill_system_hint");
        }

        CreateSkillSelectionPlaceholders();
    }

    private void CreateSkillSelectionPlaceholders()
    {
        CreateSkillButton("Trocar Skill (Q/E)", Color.blue, () => NextSkill());

        CreateSkillButton("Adicionar Skill (F7)", Color.green, () => {
            if (skillManager != null)
            {
                var method = skillManager.GetType().GetMethod("AddRandomSkill");
                if (method != null) method.Invoke(skillManager, null);
            }
        });

        CreateSkillButton("Skills de Teste (F9)", Color.yellow, () => {
            if (skillManager != null)
            {
                var method = skillManager.GetType().GetMethod("AddTestSkills");
                if (method != null) method.Invoke(skillManager, null);
            }
        });
    }

    private GameObject CreateSkillButton(string text, Color color, System.Action callback)
    {
        GameObject buttonGO = Instantiate(skillButtonPrefab, skillButtonContainer);
        buttonGO.SetActive(true);

        // Bloqueia todos os botões do prefab — impede listeners persistentes
        foreach (var b in buttonGO.GetComponentsInChildren<Button>(true))
        {
            b.onClick.RemoveAllListeners();
            b.interactable = false;
            var g = b.GetComponent<UnityEngine.UI.Graphic>();
            if (g != null) g.raycastTarget = false;
        }

        TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
            buttonText.text = text;

        Image buttonImage = buttonGO.GetComponent<Image>();
        if (buttonImage != null)
            buttonImage.color = color;

        // Overlay invisível cobrindo o botão inteiro — único receptor de cliques
        var overlay = new GameObject("ClickOverlay");
        overlay.transform.SetParent(buttonGO.transform, false);
        var rt = overlay.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = overlay.AddComponent<Image>();
        img.color = Color.clear;
        img.raycastTarget = true;
        var overlayBtn = overlay.AddComponent<Button>();
        if (callback != null) overlayBtn.onClick.AddListener(() => callback());

        return buttonGO;
    }

    public void ToggleStatusCardPanel()
    {
        if (statusCardPanel != null)
        {
            statusCardPanelVisible = !statusCardPanelVisible;
            statusCardPanel.SetActive(statusCardPanelVisible);

            if (statusCardPanelVisible)
            {
                UpdateStatusCardPanel();
            }
        }
    }

    public void UpdateStatusCardPanel()
    {
        if (cardSystem == null || !statusCardPanelVisible) return;

        if (statusPointsText != null)
        {
            statusPointsText.text = Loc.T("ui.card_system_hint");
        }

        if (activeBonusesText != null)
        {
            activeBonusesText.text = $"{Loc.T("ui.active_bonuses")}:\n{Loc.T("ui.card_system")}";
        }

        CreateStatusCardPlaceholders();
    }

    private void CreateStatusCardPlaceholders()
    {
        foreach (Transform child in statusCardContainer)
        {
            Destroy(child.gameObject);
        }

        CreateStatusCard("Carta de Ataque", "Aumenta dano em 10%", new Color(1f, 0.3f, 0.3f));
        CreateStatusCard("Carta de Defesa", "Aumenta defesa em 15%", new Color(0.3f, 0.3f, 1f));
        CreateStatusCard("Carta de Velocidade", "Aumenta velocidade em 20%", new Color(0.3f, 1f, 0.3f));
    }

    private void CreateStatusCard(string title, string description, Color color)
    {
        GameObject card = new GameObject($"Card_{title}", typeof(RectTransform), typeof(Image), typeof(Button));
        card.transform.SetParent(statusCardContainer);

        RectTransform rect = card.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(68, 90);

        Image image = card.GetComponent<Image>();
        image.color = color;
        image.sprite = null;

        Button button = card.GetComponent<Button>();
        button.onClick.AddListener(() => OnStatusCardClicked(title));

        GameObject titleText = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleText.transform.SetParent(card.transform);
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.7f);
        titleRect.anchorMax = new Vector2(1, 1f);
        titleRect.sizeDelta = Vector2.zero;
        titleRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI titleTextComp = titleText.GetComponent<TextMeshProUGUI>();
        titleTextComp.text = title;
        titleTextComp.color = Color.white;
        titleTextComp.fontSize = 8;
        titleTextComp.alignment = TextAlignmentOptions.Center;
        titleTextComp.fontStyle = FontStyles.Bold;

        GameObject descText = new GameObject("Description", typeof(RectTransform), typeof(TextMeshProUGUI));
        descText.transform.SetParent(card.transform);
        RectTransform descRect = descText.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0, 0);
        descRect.anchorMax = new Vector2(1, 0.7f);
        descRect.sizeDelta = Vector2.zero;
        descRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI descTextComp = descText.GetComponent<TextMeshProUGUI>();
        descTextComp.text = description;
        descTextComp.color = Color.white;
        descTextComp.fontSize = 7;
        descTextComp.alignment = TextAlignmentOptions.Center;
        descTextComp.textWrappingMode = TextWrappingModes.Normal;
    }

    private void OnStatusCardClicked(string cardName)
    {
        ShowSkillAcquired($"Carta {cardName}", "Recurso de cartas de status ativado!");
    }

    public void ShowSkillAcquired(string skillName, string description)
    {
        // notificação desativada
    }

    private IEnumerator HideSkillAcquiredPanel()
    {
        yield return new WaitForSeconds(3f);
        if (skillAcquiredPanel != null)
        {
            skillAcquiredPanel.SetActive(false);
        }
    }

    public void ShowUltimateAcquired(string ultimateName, string description)
    {
        ShowSkillAcquired($"{ultimateName}", description);
    }

    public void ShowModifierAcquired(string modifierName, string targetSkill)
    {
        ShowSkillAcquired($"✨ {modifierName}", $"Aplicado em: {targetSkill}");
    }

    public void ShowElementChanged(string elementName)
    {
    }

    public void OnUltimateActivated()
    {
        if (ultimateReadyEffect != null)
        {
            ultimateReadyEffect.SetActive(false);
        }
    }

    public void SetUltimateReady(bool ready)
    {
        if (ultimateReadyEffect != null)
        {
            ultimateReadyEffect.SetActive(ready);
        }
    }

    public void UpdateElementIcons()
    {
        if (playerStats == null) return;

        var attackSkills = playerStats.GetAttackSkills();
        if (attackSkills.Count > 0) UpdateSkillElementIcon(attackSkill1ElementIcon, attackSkills[0]);
        if (attackSkills.Count > 1) UpdateSkillElementIcon(attackSkill2ElementIcon, attackSkills[1]);

        var defenseSkills = playerStats.GetDefenseSkills();
        if (defenseSkills.Count > 0) UpdateSkillElementIcon(defenseSkill1ElementIcon, defenseSkills[0]);
        if (defenseSkills.Count > 1) UpdateSkillElementIcon(defenseSkill2ElementIcon, defenseSkills[1]);

        // Ultimate não é infundível → sem quadradinho de elemento.
        if (ultimateSkillElementIcon != null)
            ultimateSkillElementIcon.gameObject.SetActive(false);

        if (elementIcon != null)
        {
            elementIcon.color = GetElementColor(playerStats.GetCurrentElement());
            elementIcon.gameObject.SetActive(playerStats.GetCurrentElement() != PlayerStats.Element.None);
        }
    }

    private void UpdateSkillElementIcon(Image icon, object skill)
    {
        if (icon == null) return;

        Color elementColor = Color.white;

        if (skill is PlayerStats.AttackSkill attackSkill)
        {
            elementColor = attackSkill.GetElementColor();
        }
        else if (skill is PlayerStats.DefenseSkill defenseSkill)
        {
            elementColor = defenseSkill.GetElementColor();
        }
        else if (skill is PlayerStats.UltimateSkill ultimateSkill)
        {
            elementColor = ultimateSkill.GetElementColor();
        }

        icon.color = elementColor;
        icon.gameObject.SetActive(elementColor != Color.white);
    }

    public void ShowElementAdvantage(string strongAgainst, string weakAgainst)
    {
        if (elementAdvantagePanel != null && advantageText != null && disadvantageText != null)
        {
            advantageText.text = $"{Loc.T("ui.strong_against")}: {strongAgainst}";
            disadvantageText.text = $"{Loc.T("ui.weak_against")}: {weakAgainst}";
            elementAdvantagePanel.SetActive(true);
        }
    }

    public void HideElementAdvantage()
    {
        if (elementAdvantagePanel != null)
        {
            elementAdvantagePanel.SetActive(false);
        }
    }

    public void ForceRefreshUI()
    {
        UpdatePlayerStatus();
        UpdateSkillCooldowns();
        UpdateSkillIcons();
        UpdateElementIcons();

        if (statusPanelVisible) UpdateStatusPanel();
        if (skillSelectionPanelVisible) UpdateSkillSelectionPanel();
        if (statusCardPanelVisible) UpdateStatusCardPanel();

        // 🆕 ATUALIZAR SKILL EQUIPADA
        var equippedSkill = skillManager?.GetEquippedSkill();
        if (equippedSkill != null)
        {
            UpdateSkillManagerWithEquippedSkill(equippedSkill);
        }
    }

    public void ClearAllSkillIcons()
    {
        ClearSkillIcon(attackSkill1Icon, attackSkill1ElementIcon);
        ClearSkillIcon(attackSkill2Icon, attackSkill2ElementIcon);
        ClearSkillIcon(defenseSkill1Icon, defenseSkill1ElementIcon);
        ClearSkillIcon(defenseSkill2Icon, defenseSkill2ElementIcon);
        ClearSkillIcon(ultimateSkillIcon, ultimateSkillElementIcon);

        if (elementIcon != null)
            elementIcon.gameObject.SetActive(false);

    }

    private void ClearSkillIcon(Image mainIcon, Image elementIcon)
    {
        if (mainIcon != null)
        {
            mainIcon.sprite = defaultSkillIcon;
            mainIcon.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        }

        if (elementIcon != null)
            elementIcon.gameObject.SetActive(false);
    }

    public void HighlightSkillSlot(string skillType, int slotIndex)
    {
        StartCoroutine(HighlightSlotCoroutine(skillType, slotIndex));
    }

    private IEnumerator HighlightSlotCoroutine(string skillType, int slotIndex)
    {
        Image targetIcon = null;

        switch (skillType.ToLower())
        {
            case "attack":
                targetIcon = slotIndex == 0 ? attackSkill1Icon : attackSkill2Icon;
                break;
            case "defense":
                targetIcon = slotIndex == 0 ? defenseSkill1Icon : defenseSkill2Icon;
                break;
            case "ultimate":
                yield break; // ícone da ultimate não participa de highlight de infusão

        }

        if (targetIcon == null) yield break;

        float duration = 2f;
        float elapsed = 0f;
        Color originalColor = targetIcon.color;

        while (elapsed < duration)
        {
            float pulse = Mathf.PingPong(elapsed * 3f, 1f);
            targetIcon.color = Color.Lerp(originalColor, Color.white, pulse);
            elapsed += Time.deltaTime;
            yield return null;
        }

        targetIcon.color = originalColor;
    }

    // 🆕 MÉTODOS DE CONTEXTO PARA TESTE
    [ContextMenu("🎯 Testar Skill HUD")]
    public void TestSkillHUD()
    {

        if (skillManager != null && skillManager.GetActiveSkills().Count > 0)
        {
            var testSkill = skillManager.GetActiveSkills()[0];
            UpdateSkillHUDWithEquippedSkill(testSkill);
        }
        else
        {
            Debug.LogWarning("⚠️ Nenhuma skill disponível para testar a HUD");
        }
    }

    [ContextMenu("🔍 Verificar Estado da HUD")]
    public void DebugHUDState()
    {


        if (attackSkill1Icon != null)
        {
        }

        var equippedSkill = skillManager?.GetEquippedSkill();
    }

    [ContextMenu("🔄 Forçar Atualização de Skill Equipada")]
    public void ForceEquippedSkillUpdate()
    {
        var equippedSkill = skillManager?.GetEquippedSkill();
        if (equippedSkill != null)
        {
            UpdateSkillManagerWithEquippedSkill(equippedSkill);
        }
    }

    [ContextMenu("⚡ Forçar Atualização Completa da HUD")]
    public void ForceCompleteHUDUpdate()
    {
        StartCoroutine(DelayedHUDUpdate());
    }

    private IEnumerator DelayedHUDUpdate()
    {
        yield return new WaitForEndOfFrame();

        // Forçar atualização de todos os componentes
        Canvas.ForceUpdateCanvases();

        var equippedSkill = skillManager?.GetEquippedSkill();
        if (equippedSkill != null)
        {
            UpdateSkillHUDWithEquippedSkill(equippedSkill);
        }

        // Forçar reconstrução de layout
        if (attackSkill1Icon != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(attackSkill1Icon.rectTransform);
            attackSkill1Icon.SetAllDirty();
        }
    }

    // 🆕 MÉTODOS DE CONTEXTO PARA O SISTEMA DE PAUSE
    [ContextMenu("⏸️ Criar Sistema de Pause Completo")]
    public void CreateCompletePauseSystem()
    {
        // Chamar o método estático do UIPauseCreator
        var method = System.Type.GetType("UIPauseCreator")?.GetMethod("CreateCompletePauseSystem",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        if (method != null)
        {
            method.Invoke(null, null);
            FindPauseManager(); // Recarregar referência
        }
        else
        {
            Debug.LogWarning("⚠️ UIPauseCreator não encontrado. Certifique-se de que o script está na pasta Editor/");
        }
    }

    [ContextMenu("⏸️ Testar Sistema de Pause")]
    public void TestPauseSystem()
    {
        if (pauseManager != null)
        {
            pauseManager.TestPause();
        }
        else
        {
            Debug.LogWarning("⚠️ PauseManager não encontrado. Use 'Criar Sistema de Pause Completo' primeiro.");
        }
    }

    [ContextMenu("⏸️ Verificar Estado do Pause")]
    public void DebugPauseState()
    {
        if (pauseManager != null)
        {
        }
        else
        {
            Debug.LogWarning("⚠️ PauseManager não encontrado");
        }
    }

    void OnDestroy()
    {
        if (skillManager != null)
        {
            skillManager.OnSkillEquippedChanged -= OnSkillEquippedChanged;
        }
    }
}
