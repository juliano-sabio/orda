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
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        playerStats = FindAnyObjectByType<PlayerStats>();
        skillManager = FindAnyObjectByType<SkillManager>();
        cardSystem = FindAnyObjectByType<StatusCardSystem>();

        // 🆕 ENCONTRAR PAUSE MANAGER
        FindPauseManager();

        InitializeUI();
        UpdateSkillIcons();

        ConnectToEquippedSkill();

        // Subscreve ao evento de skill adquirida para atualizar os slots
        if (skillManager != null)
            skillManager.OnSkillAcquired += OnSkillAdquiridaHUD;

        if (passivaIcon == null)
            CriarSlotPassivaRuntime();
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
        passivaLabel.text = "Passiva";

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
            skillManagerSkillName.text = equippedSkill.skillName;
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

            // 🎨 ATUALIZAR COR DO ELEMENTO
            if (targetElementSlot != null)
            {
                targetElementSlot.color = GetElementColor(equippedSkill.element);
                targetElementSlot.gameObject.SetActive(true);
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
            slot.color = Color.Lerp(originalColor, Color.yellow, glow);

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
            cooldownText.text = "VAZIO";
            cooldownText.color = Color.gray;
        }
    }

    private string GetSkillStatsText(SkillData skill)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        if (skill.attackBonus != 0) sb.AppendLine($"ATQ: +{skill.attackBonus}");
        if (skill.defenseBonus != 0) sb.AppendLine($"DEF: +{skill.defenseBonus}");
        if (skill.healthBonus != 0) sb.AppendLine($"Vida: +{skill.healthBonus}");
        if (skill.speedBonus != 0) sb.AppendLine($"Vel: +{skill.speedBonus}");

        if (sb.Length == 0) sb.AppendLine("Bônus Passivo");

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
            skillManagerSkillName.text = "Nenhuma Skill Equipada";
            skillManagerSkillName.color = Color.gray;
        }

        if (skillManagerElementText != null)
        {
            skillManagerElementText.text = "⚪ Selecione uma Skill";
            skillManagerElementText.color = Color.gray;
        }

        if (equippedSkillStatsText != null)
        {
            equippedSkillStatsText.text = "Equipe uma skill para ver os status";
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

    public void SetPassivaIcon(Sprite icon, string nome)
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
        if (passivaLabel != null) passivaLabel.text = nome ?? "";
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

    private void UpdateAttackSkillIcons()
    {
        var skills = skillManager?.GetActiveSkills();
        if (skills == null) return;

        // Slot 1 (índice 0) = ataque → attackSkill1Icon
        // Slot 3 (índice 2) = ataque → attackSkill2Icon
        SetSkillSlot(attackSkill1Icon, attackSkill1ElementIcon, skills.Count > 0 ? skills[0] : null);
        SetSkillSlot(attackSkill2Icon, attackSkill2ElementIcon, skills.Count > 2 ? skills[2] : null);
    }

    private void UpdateDefenseSkillIcons()
    {
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

        if (skill != null)
        {
            icon.sprite = skill.icon != null ? skill.icon : defaultSkillIcon;
            icon.color = Color.white;
            icon.gameObject.SetActive(true);

            if (elementIcon != null)
            {
                elementIcon.color = GetElementColor(skill.element);
                elementIcon.gameObject.SetActive(true);
            }
        }
        else
        {
            icon.sprite = defaultSkillIcon;
            icon.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            if (elementIcon != null)
                elementIcon.gameObject.SetActive(false);
        }
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
                ultimateSkillIcon.color = playerStats.ultimateBloqueada ? Color.red
                                        : playerStats.IsUltimateReady() ? Color.yellow
                                        : Color.white;
                ultimateSkillIcon.gameObject.SetActive(true);

                if (ultimateSkillElementIcon != null)
                {
                    ultimateSkillElementIcon.color = GetElementColor(ultimateSkill.element);
                    ultimateSkillElementIcon.gameObject.SetActive(true);
                }
            }
            else
            {
                ultimateSkillIcon.sprite = defaultSkillIcon;
                ultimateSkillIcon.color = new Color(0.3f, 0.3f, 0.3f, 0.3f);
                if (ultimateSkillElementIcon != null)
                    ultimateSkillElementIcon.gameObject.SetActive(false);
            }
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

        return CreateFallbackIcon(skillName);
    }

    private Sprite CreateFallbackIcon(string skillName)
    {
        if (defaultSkillIcon != null)
            return defaultSkillIcon;

        Texture2D texture = new Texture2D(64, 64);
        Color[] pixels = new Color[64 * 64];
        Color baseColor = ColorForString(skillName);

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
            string novoLvl = $"Level: {playerStats.GetLevel()}";
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
            string novoTextoUlti = playerStats.ultimateBloqueada ? "ULTIMATE BLOQUEADA!" :
                playerStats.IsUltimateReady() ? "ULTIMATE PRONTA!" :
                $"ULTIMATE: {Mathf.FloorToInt(chargePercent)}%";
            if (ultimateChargeText.text != novoTextoUlti)
                ultimateChargeText.text = novoTextoUlti;
        }

        if (currentElementText != null)
            currentElementText.text = $"Elem: {playerStats.GetCurrentElement()}";

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
            attackCooldownText1.text  = remaining > 0.05f ? $"{remaining:F1}s" : "PRONTO";
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
                defenseCooldownText1.text  = skill.IsOnCooldown ? $"{skill.currentCooldown:F1}s" : "PRONTO";
                defenseCooldownText1.color = skill.IsOnCooldown ? Color.red : Color.green;
            }
        }

        if (defenseCooldownText2 != null)
        {
            if (playerStats.defenseSkills.Count > 1)
            {
                var skill = playerStats.defenseSkills[1];
                defenseCooldownText2.text  = skill.IsOnCooldown ? $"{skill.currentCooldown:F1}s" : "PRONTO";
                defenseCooldownText2.color = skill.IsOnCooldown ? Color.red : Color.green;
            }
        }

        if (ultimateCooldownText != null)
        {
            if (playerStats.HasUltimate())
            {
                ultimateCooldownText.text = playerStats.IsUltimateReady() ? "PRONTA!" : "CARREGINGO";
                ultimateCooldownText.color = playerStats.IsUltimateReady() ? Color.yellow : Color.gray;
            }
            else
            {
                ultimateCooldownText.text = "BLOQUEADA";
                ultimateCooldownText.color = Color.gray;
            }
        }
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
            string shieldStr = mp > 0f ? $" (+{sp:F0}/{mp:F0} escudo)" : "";
            maxHealthStatusText.text = $"Vida: {playerStats.GetCurrentHealth():F0} / {playerStats.GetMaxHealth():F0}{shieldStr}";
        }

        // ── Combate ───────────────────────────────────────────────────
        if (damageText != null)
            damageText.text = $"ATQ: {playerStats.GetAttack():F1}";

        if (defenseText != null)
        {
            float sp = playerStats.GetShieldPoints();
            float mp = playerStats.GetMaxShieldPoints();
            string shieldStr = mp > 0f ? $" | Escudo: {sp:F0}/{mp:F0}" : "";
            defenseText.text = $"DEF: {playerStats.GetDefense():F1}{shieldStr}";
        }

        if (critChanceText != null)
            critChanceText.text = $"Critico: {playerStats.GetCritChance() * 100f:F1}%";

        // ── Velocidade & Regeneração ──────────────────────────────────
        if (speedText != null)
            speedText.text = $"Vel: {playerStats.GetSpeed():F1}";

        if (attackSpeedText != null)
            attackSpeedText.text = $"Vel.Atq: {playerStats.GetAttackActivationInterval():F1}s";

        if (healthRegenText != null)
            healthRegenText.text = $"Regen: {playerStats.GetHealthRegen():F1}/s";

        // ── Level & XP ────────────────────────────────────────────────
        if (statusPointsText != null)
            statusPointsText.text = $"Level {playerStats.GetLevel()}  |  XP: {playerStats.GetCurrentXP():F0} / {playerStats.GetXPToNextLevel():F0}";

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
                sb.AppendLine($"Elem: {elem}  +{(bonus - 1f) * 100f:F0}%");

            // Regen ativa
            if (playerStats.IsRegeneratingHealth())
                sb.AppendLine("Regenerando vida...");

            activeBonusesText.text = sb.Length > 0 ? sb.ToString().TrimEnd() : "Nenhum bonus ativo";
        }

        // ── Elemento (resumo) ─────────────────────────────────────────
        if (elementInfoText != null)
            elementInfoText.text = $"Elem: {playerStats.GetCurrentElement()}\nBonus: {playerStats.GetElementalBonus():F1}x";

        // ── Inventário ────────────────────────────────────────────────
        if (inventoryText != null)
        {
            var inventory = playerStats.GetInventory();
            string inventoryStr = inventory.Count > 0 ? string.Join(", ", inventory) : "Nenhum";
            inventoryText.text = $"Itens: {inventoryStr}";
        }

        // ── Skills de Ataque ──────────────────────────────────────────
        if (attackSkillsText != null)
        {
            var attackSkills = playerStats.GetAttackSkills();
            if (attackSkills.Count == 0)
            {
                attackSkillsText.text = "Skills ATQ: Nenhuma";
            }
            else
            {
                var sb = new System.Text.StringBuilder("Skills ATQ:\n");
                foreach (var skill in attackSkills)
                {
                    string status = skill.isActive ? "[ON]" : "[OFF]";
                    sb.AppendLine($"{status} {skill.skillName}  Dano: {skill.baseDamage:F1}");
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
                defenseSkillsText.text = "Skills DEF: Nenhuma";
            }
            else
            {
                var sb = new System.Text.StringBuilder("Skills DEF:\n");
                foreach (var skill in defenseSkills)
                {
                    string status = skill.isActive ? "[ON]" : "[OFF]";
                    sb.AppendLine($"{status} {skill.skillName}  DEF: {skill.baseDefense:F1}");
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
                    ? "PRONTA!"
                    : $"Carregando... {playerStats.GetUltimateChargeTime():F1}s";
                ultimateSkillsText.text = $"Ultimate: {ultimate.skillName}\nDano: {ultimate.baseDamage:F1}  {readyStr}";
            }
            else
            {
                ultimateSkillsText.text = "Ultimate: disponivel no Level 5";
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
            availableSkillsText.text = "Sistema de Skills - Use Q/E para trocar skills equipadas";
        }

        CreateSkillSelectionPlaceholders();
    }

    private void CreateSkillSelectionPlaceholders()
    {
        GameObject button1 = CreateSkillButton("Trocar Skill (Q/E)", Color.blue);
        button1.GetComponent<Button>().onClick.AddListener(() => {
            NextSkill();
        });

        GameObject button2 = CreateSkillButton("Adicionar Skill (F7)", Color.green);
        button2.GetComponent<Button>().onClick.AddListener(() => {
            if (skillManager != null)
            {
                var method = skillManager.GetType().GetMethod("AddRandomSkill");
                if (method != null) method.Invoke(skillManager, null);
            }
        });

        GameObject button3 = CreateSkillButton("Skills de Teste (F9)", Color.yellow);
        button3.GetComponent<Button>().onClick.AddListener(() => {
            if (skillManager != null)
            {
                var method = skillManager.GetType().GetMethod("AddTestSkills");
                if (method != null) method.Invoke(skillManager, null);
            }
        });
    }

    private GameObject CreateSkillButton(string text, Color color)
    {
        GameObject buttonGO = Instantiate(skillButtonPrefab, skillButtonContainer);
        buttonGO.SetActive(true);

        Button button = buttonGO.GetComponent<Button>();
        TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();

        if (buttonText != null)
        {
            buttonText.text = text;
        }

        Image buttonImage = buttonGO.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = color;
        }

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
            statusPointsText.text = "Sistema de Cartas - Use C para abrir/fechar";
        }

        if (activeBonusesText != null)
        {
            activeBonusesText.text = "BONUS ATIVOS:\nSistema de cartas de status";
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
        rect.sizeDelta = new Vector2(150, 200);

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
        titleTextComp.fontSize = 12;
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
        descTextComp.fontSize = 10;
        descTextComp.alignment = TextAlignmentOptions.Center;
        descTextComp.textWrappingMode = TextWrappingModes.Normal;
    }

    private void OnStatusCardClicked(string cardName)
    {
        ShowSkillAcquired($"Carta {cardName}", "Recurso de cartas de status ativado!");
    }

    public void ShowSkillAcquired(string skillName, string description)
    {
        if (skillAcquiredPanel != null && skillNameText != null && skillDescriptionText != null)
        {
            skillNameText.text = skillName;
            skillDescriptionText.text = description;
            skillAcquiredPanel.SetActive(true);

            StartCoroutine(HideSkillAcquiredPanel());
        }
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

        if (playerStats.HasUltimate())
        {
            UpdateSkillElementIcon(ultimateSkillElementIcon, playerStats.GetUltimateSkill());
        }

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
            advantageText.text = $"Forte contra: {strongAgainst}";
            disadvantageText.text = $"Fraco contra: {weakAgainst}";
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
                targetIcon = ultimateSkillIcon;
                break;
        }

        if (targetIcon == null) yield break;

        float duration = 2f;
        float elapsed = 0f;
        Color originalColor = targetIcon.color;

        while (elapsed < duration)
        {
            float pulse = Mathf.PingPong(elapsed * 3f, 1f);
            targetIcon.color = Color.Lerp(originalColor, Color.yellow, pulse);
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
