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
    public TextMeshProUGUI attackSpeedText;
    public TextMeshProUGUI elementInfoText;
    public TextMeshProUGUI inventoryText;
    public TextMeshProUGUI attackSkillsText;
    public TextMeshProUGUI defenseSkillsText;
    public TextMeshProUGUI ultimateSkillsText;
    public TextMeshProUGUI statusPointsText;
    public TextMeshProUGUI activeBonusesText;

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

        Debug.Log("✅ UIManager conectado ao sistema de skills equipadas");
    }

    // 🆕 MÉTODO PARA ENCONTRAR/CRIAR PAUSE MANAGER
    // 🆕 MÉTODO PARA ENCONTRAR/CRIAR PAUSE MANAGER
    private void FindPauseManager()
    {
        pauseManager = FindAnyObjectByType<PauseManager>();
        if (pauseManager == null)
        {
            Debug.Log("⏸️ PauseManager não encontrado. Criando automaticamente...");

            // Criar um GameObject para o PauseManager
            GameObject pauseManagerGO = new GameObject("PauseManager");
            pauseManager = pauseManagerGO.AddComponent<PauseManager>();

            // 🆕 AGORA SIM: Configurar DontDestroyOnLoad (apenas em runtime)
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(pauseManagerGO);
                Debug.Log("✅ PauseManager criado e configurado com DontDestroyOnLoad");
            }
            else
            {
                Debug.Log("✅ PauseManager criado (DontDestroyOnLoad será configurado em runtime)");
            }
        }
        else
        {
            // 🆕 Garantir DontDestroyOnLoad se já existir
            if (Application.isPlaying && !pauseManager.gameObject.scene.IsValid())
            {
                DontDestroyOnLoad(pauseManager.gameObject);
            }
            Debug.Log("✅ PauseManager encontrado na cena");
        }
    }
    void InitializeUI()
    {
        UpdatePlayerStatus();

        if (skillAcquiredPanel != null) skillAcquiredPanel.SetActive(false);
        if (statusPanel != null) statusPanel.SetActive(false);
        if (skillSelectionPanel != null) skillSelectionPanel.SetActive(false);
        if (statusCardPanel != null) statusCardPanel.SetActive(false);
        if (elementAdvantagePanel != null) elementAdvantagePanel.SetActive(false);
        if (xpGainText != null) xpGainText.gameObject.SetActive(false);

        Debug.Log("✅ UIManager inicializado com TextMeshPro!");
    }

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
        Debug.Log($"🔄 Skill equipada mudou: {equippedSkill?.skillName}");
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

        Debug.Log($"🎯 UI do Skill Manager conectada à: {equippedSkill.skillName}");
    }

    // 🆕 MÉTODO PARA ATUALIZAR O SKILL HUD COM A SKILL EQUIPADA
    public void UpdateSkillHUDWithEquippedSkill(SkillData equippedSkill)
    {
        if (equippedSkill == null) return;

        Debug.Log($"🎯 Atualizando Skill HUD com: {equippedSkill.skillName}");

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

        // 📝 ATUALIZAR TEXTO DE COOLDOWN (se aplicável)
        if (targetCooldown != null)
        {
            if (equippedSkill.cooldown > 0)
            {
                targetCooldown.text = $"{equippedSkill.cooldown}s";
                targetCooldown.color = Color.yellow;
            }
            else
            {
                targetCooldown.text = "PRONTO";
                targetCooldown.color = Color.green;
            }
        }

        // ✨ ATUALIZAR ELEMENTO PRINCIPAL NA HUD
        UpdateElementIcon();

        Debug.Log($"✅ Skill HUD atualizada com: {equippedSkill.skillName}");
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

        if (skill.attackBonus != 0) sb.AppendLine($"⚔️ Ataque: +{skill.attackBonus}");
        if (skill.defenseBonus != 0) sb.AppendLine($"🛡️ Defesa: +{skill.defenseBonus}");
        if (skill.healthBonus != 0) sb.AppendLine($"❤️ Vida: +{skill.healthBonus}");
        if (skill.speedBonus != 0) sb.AppendLine($"🏃 Velocidade: +{skill.speedBonus}");

        if (sb.Length == 0) sb.AppendLine("💎 Bônus Passivo");

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

    // 🎯 MÉTODOS EXISTENTES DO UIMANAGER (mantidos)
    public void UpdateSkillIcons()
    {
        if (playerStats == null) return;

        Debug.Log("🔄 Atualizando ícones da barra de habilidades...");

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
        var attackSkills = playerStats.GetAttackSkills();

        if (attackSkill1Icon != null)
        {
            if (attackSkills.Count > 0 && attackSkills[0].isActive)
            {
                attackSkill1Icon.sprite = GetSkillIcon(attackSkills[0].skillName);
                attackSkill1Icon.color = Color.white;
                attackSkill1Icon.gameObject.SetActive(true);

                if (attackSkill1ElementIcon != null)
                {
                    attackSkill1ElementIcon.color = GetElementColor(attackSkills[0].element);
                    attackSkill1ElementIcon.gameObject.SetActive(true);
                }
            }
            else
            {
                attackSkill1Icon.sprite = defaultSkillIcon;
                attackSkill1Icon.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                if (attackSkill1ElementIcon != null)
                    attackSkill1ElementIcon.gameObject.SetActive(false);
            }
        }

        if (attackSkill2Icon != null)
        {
            if (attackSkills.Count > 1 && attackSkills[1].isActive)
            {
                attackSkill2Icon.sprite = GetSkillIcon(attackSkills[1].skillName);
                attackSkill2Icon.color = Color.white;
                attackSkill2Icon.gameObject.SetActive(true);

                if (attackSkill2ElementIcon != null)
                {
                    attackSkill2ElementIcon.color = GetElementColor(attackSkills[1].element);
                    attackSkill2ElementIcon.gameObject.SetActive(true);
                }
            }
            else
            {
                attackSkill2Icon.sprite = defaultSkillIcon;
                attackSkill2Icon.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                if (attackSkill2ElementIcon != null)
                    attackSkill2ElementIcon.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateDefenseSkillIcons()
    {
        var defenseSkills = playerStats.GetDefenseSkills();

        if (defenseSkill1Icon != null)
        {
            if (defenseSkills.Count > 0 && defenseSkills[0].isActive)
            {
                defenseSkill1Icon.sprite = GetSkillIcon(defenseSkills[0].skillName);
                defenseSkill1Icon.color = Color.white;
                defenseSkill1Icon.gameObject.SetActive(true);

                if (defenseSkill1ElementIcon != null)
                {
                    defenseSkill1ElementIcon.color = GetElementColor(defenseSkills[0].element);
                    defenseSkill1ElementIcon.gameObject.SetActive(true);
                }
            }
            else
            {
                defenseSkill1Icon.sprite = defaultSkillIcon;
                defenseSkill1Icon.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                if (defenseSkill1ElementIcon != null)
                    defenseSkill1ElementIcon.gameObject.SetActive(false);
            }
        }

        if (defenseSkill2Icon != null)
        {
            if (defenseSkills.Count > 1 && defenseSkills[1].isActive)
            {
                defenseSkill2Icon.sprite = GetSkillIcon(defenseSkills[1].skillName);
                defenseSkill2Icon.color = Color.white;
                defenseSkill2Icon.gameObject.SetActive(true);

                if (defenseSkill2ElementIcon != null)
                {
                    defenseSkill2ElementIcon.color = GetElementColor(defenseSkills[1].element);
                    defenseSkill2ElementIcon.gameObject.SetActive(true);
                }
            }
            else
            {
                defenseSkill2Icon.sprite = defaultSkillIcon;
                defenseSkill2Icon.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                if (defenseSkill2ElementIcon != null)
                    defenseSkill2ElementIcon.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateUltimateSkillIcon()
    {
        var ultimateSkill = playerStats.GetUltimateSkill();

        if (ultimateSkillIcon != null)
        {
            if (ultimateSkill.isActive)
            {
                ultimateSkillIcon.sprite = GetSkillIcon(ultimateSkill.skillName);
                ultimateSkillIcon.color = playerStats.IsUltimateReady() ? Color.yellow : Color.white;
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
            healthText.text = $"{playerStats.GetCurrentHealth():F0}/{playerStats.GetMaxHealth():F0}";

        if (levelText != null)
            levelText.text = $"⭐ Level: {playerStats.GetLevel()}";

        if (xpText != null)
            xpText.text = $"📊 XP: {playerStats.GetCurrentXP():F0}/{playerStats.GetXPToNextLevel():F0}";

        if (ultimateChargeText != null)
        {
            float chargePercent = (playerStats.GetUltimateChargeTime() / playerStats.GetUltimateCooldown()) * 100f;
            ultimateChargeText.text = playerStats.IsUltimateReady() ?
                "🚀 ULTIMATE PRONTA!" :
                $"🚀 ULTIMATE: {chargePercent:F0}%";
        }

        if (currentElementText != null)
            currentElementText.text = $"⚡ Elemento: {playerStats.GetCurrentElement()}";

        if (ultimateReadyEffect != null)
            ultimateReadyEffect.SetActive(playerStats.IsUltimateReady());

        if (statusPanelVisible)
        {
            UpdateStatusPanel();
        }
    }

    public void UpdateSkillCooldowns()
    {
        if (playerStats == null) return;

        if (attackCooldownText1 != null)
        {
            float cooldown1 = playerStats.GetSkillCooldown("Ataque Automático");
            attackCooldownText1.text = cooldown1 > 0 ? $"{cooldown1:F1}s" : "PRONTO";
            attackCooldownText1.color = cooldown1 > 0 ? Color.red : Color.green;
        }

        if (attackCooldownText2 != null)
        {
            float cooldown2 = playerStats.GetSkillCooldown("Golpe Contínuo");
            attackCooldownText2.text = cooldown2 > 0 ? $"{cooldown2:F1}s" : "PRONTO";
            attackCooldownText2.color = cooldown2 > 0 ? Color.red : Color.green;
        }

        if (defenseCooldownText1 != null)
        {
            float cooldown1 = playerStats.GetSkillCooldown("Proteção Passiva");
            defenseCooldownText1.text = cooldown1 > 0 ? $"{cooldown1:F1}s" : "PRONTO";
            defenseCooldownText1.color = cooldown1 > 0 ? Color.red : Color.green;
        }

        if (defenseCooldownText2 != null)
        {
            float cooldown2 = playerStats.GetSkillCooldown("Escudo Automático");
            defenseCooldownText2.text = cooldown2 > 0 ? $"{cooldown2:F1}s" : "PRONTO";
            defenseCooldownText2.color = cooldown2 > 0 ? Color.red : Color.green;
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

        if (damageText != null)
            damageText.text = $"⚔️ Ataque: {playerStats.GetAttack():F1}";

        if (speedText != null)
            speedText.text = $"🏃 Velocidade: {playerStats.GetSpeed():F1}";

        if (defenseText != null)
            defenseText.text = $"🛡️ Defesa: {playerStats.GetDefense():F1}";

        if (attackSpeedText != null)
            attackSpeedText.text = $"⚡ Vel. Ataque: {playerStats.GetAttackActivationInterval():F1}s";

        if (elementInfoText != null)
            elementInfoText.text = $"⚡ Elemento: {playerStats.GetCurrentElement()}\n📈 Bônus: {playerStats.GetElementalBonus():F1}x";

        if (inventoryText != null)
        {
            var inventory = playerStats.GetInventory();
            string inventoryStr = inventory.Count > 0 ? string.Join(", ", inventory) : "Nenhum";
            inventoryText.text = $"🎒 Itens: {inventoryStr}";
        }

        if (attackSkillsText != null)
        {
            var attackSkills = playerStats.GetAttackSkills();
            string attackStr = "⚔️ Skills de Ataque:\n";
            foreach (var skill in attackSkills)
            {
                string status = skill.isActive ? "✅" : "❌";
                attackStr += $"{status} {skill.skillName} (Dano: {skill.baseDamage:F1})\n";
            }
            attackSkillsText.text = attackStr;
        }

        if (defenseSkillsText != null)
        {
            var defenseSkills = playerStats.GetDefenseSkills();
            string defenseStr = "🛡️ Skills de Defesa:\n";
            foreach (var skill in defenseSkills)
            {
                string status = skill.isActive ? "✅" : "❌";
                defenseStr += $"{status} {skill.skillName} (Defesa: {skill.baseDefense:F1})\n";
            }
            defenseSkillsText.text = defenseStr;
        }

        if (ultimateSkillsText != null)
        {
            var ultimate = playerStats.GetUltimateSkill();
            string ultimateStr = "🚀 Ultimate:\n";
            if (ultimate.isActive)
            {
                ultimateStr += $"✅ {ultimate.skillName}\n";
                ultimateStr += $"Dano: {ultimate.baseDamage:F1}\n";
                ultimateStr += playerStats.IsUltimateReady() ? "⭐ PRONTA PARA USAR!" : "⏳ CARREGANDO...";
            }
            else
            {
                ultimateStr += "🔒 Disponível no Level 5";
            }
            ultimateSkillsText.text = ultimateStr;
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
            availableSkillsText.text = "📚 Sistema de Skills - Use Q/E para trocar skills equipadas";
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
            statusPointsText.text = "🎯 Sistema de Cartas - Use C para abrir/fechar";
        }

        if (activeBonusesText != null)
        {
            activeBonusesText.text = "✅ BÔNUS ATIVOS:\nSistema de cartas de status";
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
        Debug.Log($"🃏 Carta clicada: {cardName}");
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
        ShowSkillAcquired($"⭐ {ultimateName}", description);
    }

    public void ShowModifierAcquired(string modifierName, string targetSkill)
    {
        ShowSkillAcquired($"✨ {modifierName}", $"Aplicado em: {targetSkill}");
    }

    public void ShowElementChanged(string elementName)
    {
        Debug.Log($"⚡ Elemento alterado para: {elementName}");
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

        UpdateSkillElementIcon(attackSkill1ElementIcon, playerStats.GetAttackSkills()[0]);
        UpdateSkillElementIcon(attackSkill2ElementIcon, playerStats.GetAttackSkills()[1]);
        UpdateSkillElementIcon(defenseSkill1ElementIcon, playerStats.GetDefenseSkills()[0]);
        UpdateSkillElementIcon(defenseSkill2ElementIcon, playerStats.GetDefenseSkills()[1]);

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

        Debug.Log("🧹 Todos os ícones de skills foram limpos");
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
        Debug.Log("🧪 Testando atualização da Skill HUD...");

        if (skillManager != null && skillManager.GetActiveSkills().Count > 0)
        {
            var testSkill = skillManager.GetActiveSkills()[0];
            UpdateSkillHUDWithEquippedSkill(testSkill);
            Debug.Log($"✅ HUD testada com: {testSkill.skillName}");
        }
        else
        {
            Debug.LogWarning("⚠️ Nenhuma skill disponível para testar a HUD");
        }
    }

    [ContextMenu("🔍 Verificar Estado da HUD")]
    public void DebugHUDState()
    {
        Debug.Log("🔍 DIAGNÓSTICO DA HUD:");

        Debug.Log($"• attackSkill1Icon: {(attackSkill1Icon != null ? "✅" : "❌")}");
        Debug.Log($"• attackSkill1ElementIcon: {(attackSkill1ElementIcon != null ? "✅" : "❌")}");
        Debug.Log($"• attackCooldownText1: {(attackCooldownText1 != null ? "✅" : "❌")}");

        if (attackSkill1Icon != null)
        {
            Debug.Log($"• Ícone sprite: {attackSkill1Icon.sprite?.name ?? "NULL"}");
            Debug.Log($"• Ícone cor: {attackSkill1Icon.color}");
            Debug.Log($"• Ícone ativo: {attackSkill1Icon.gameObject.activeInHierarchy}");
        }

        var equippedSkill = skillManager?.GetEquippedSkill();
        Debug.Log($"• Skill equipada: {equippedSkill?.skillName ?? "Nenhuma"}");
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
            Debug.Log($"⏸️ Estado do Pause: {(pauseManager.IsGamePaused() ? "PAUSADO" : "RODANDO")}");
            Debug.Log($"⏸️ TimeScale: {Time.timeScale}");
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