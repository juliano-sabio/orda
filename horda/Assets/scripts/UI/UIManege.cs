using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("🔔 Popup de Skill Adquirida")]
    public GameObject skillAcquiredPanel;
    public Image skillIcon;
    public Text skillNameText;
    public Text skillDescriptionText;

    [Header("🎯 HUD de Skills Ativas")]
    public Image attackSkillIcon;
    public Image defenseSkillIcon;
    public Image ultimateSkillIcon;
    public Text ultimateCooldownText;
    public Slider ultimateCooldownSlider;

    [Header("📊 Painel de Status (TAB)")]
    public GameObject statusPanel;
    public Slider healthBar;
    public Text healthText;
    public Text damageText;
    public Text speedText;
    public Text defenseText;

    private void Awake()
    {
        // ⭐ Faz o UIManager acessível de qualquer lugar
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

    private void Start()
    {
        // Esconde o popup e status no início
        if (skillAcquiredPanel != null)
            skillAcquiredPanel.SetActive(false);

        if (statusPanel != null)
            statusPanel.SetActive(false);
    }

    private void Update()
    {
        // ⭐ Controle do TAB para mostrar/ocultar status
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleStatusPanel();
        }

        // Atualiza cooldown e status continuamente
        UpdateUltimateCooldown();
        UpdatePlayerStatus();
    }

    // 🔔 MOSTRAR POPUP QUANDO GANHAR SKILL
    public void ShowSkillAcquired(skilldata skillData)
    {
        if (skillAcquiredPanel == null) return;

        // Preenche os dados da skill
        skillIcon.sprite = skillData.icon;
        skillNameText.text = skillData.skillName;
        skillDescriptionText.text = skillData.description;

        // Mostra o popup
        skillAcquiredPanel.SetActive(true);

        // Esconde após 3 segundos
        StartCoroutine(HideSkillPopup());

        // Atualiza os ícones na HUD
        UpdateSkillIcons();
    }

    private IEnumerator HideSkillPopup()
    {
        yield return new WaitForSeconds(3f);
        if (skillAcquiredPanel != null)
            skillAcquiredPanel.SetActive(false);
    }

    // 🎯 ATUALIZAR ÍCONES DAS SKILLS ATIVAS
    private void UpdateSkillIcons()
    {
        PlayerStats playerStats = FindAnyObjectByType<PlayerStats>();
        if (playerStats == null) return;

        // Skill de Ataque
        if (playerStats.attackSkill != null)
        {
            attackSkillIcon.sprite = playerStats.attackSkill.icon;
            attackSkillIcon.color = Color.white;
        }
        else
        {
            attackSkillIcon.color = Color.clear;
        }

        // Skill de Defesa
        if (playerStats.defenseSkill != null)
        {
            defenseSkillIcon.sprite = playerStats.defenseSkill.icon;
            defenseSkillIcon.color = Color.white;
        }
        else
        {
            defenseSkillIcon.color = Color.clear;
        }

        // Skill Ultimate
        if (playerStats.ultimateSkill != null)
        {
            ultimateSkillIcon.sprite = playerStats.ultimateSkill.icon;
            ultimateSkillIcon.color = Color.white;
        }
        else
        {
            ultimateSkillIcon.color = Color.clear;
        }
    }

    // ⏰ ATUALIZAR COOLDOWN DA ULTIMATE
    private void UpdateUltimateCooldown()
    {
        PlayerStats playerStats = FindAnyObjectByType<PlayerStats>();
        if (playerStats == null || playerStats.ultimateSkill == null) return;

        if (playerStats.IsUltimateReady())
        {
            ultimateCooldownText.text = "PRONTA";
            ultimateCooldownText.color = Color.green;
            ultimateCooldownSlider.value = 1f;
            ultimateSkillIcon.color = Color.white;
        }
        else
        {
            float cooldownPercent = playerStats.GetUltimateCooldownPercent();
            int secondsLeft = Mathf.CeilToInt((1f - cooldownPercent) * playerStats.ultimateSkill.ultimateCooldown);

            ultimateCooldownText.text = $"{secondsLeft}s";
            ultimateCooldownText.color = Color.yellow;
            ultimateCooldownSlider.value = cooldownPercent;
            ultimateSkillIcon.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        }
    }

    // 📊 ATUALIZAR STATUS DO PLAYER
    private void UpdatePlayerStatus()
    {
        PlayerStats playerStats = FindAnyObjectByType<PlayerStats>();
        if (playerStats == null) return;

        // Atualiza barra de vida (sempre)
        healthBar.maxValue = playerStats.GetMaxHealth();
        healthBar.value = playerStats.GetHealth();
        healthText.text = $"{playerStats.GetHealth():F0}/{playerStats.GetMaxHealth():F0}";

        // Atualiza outros status apenas se o painel estiver aberto
        if (statusPanel.activeSelf)
        {
            damageText.text = $"Dano: {playerStats.GetDamage():F1}";
            speedText.text = $"Velocidade: {playerStats.GetMoveSpeed():F1}";
            defenseText.text = $"Defesa: {playerStats.GetDefense():F0}";
        }
    }

    // ⭐ MOSTRAR/OCULTAR PAINEL DE STATUS
    private void ToggleStatusPanel()
    {
        if (statusPanel != null)
        {
            bool newState = !statusPanel.activeSelf;
            statusPanel.SetActive(newState);
            Debug.Log($"Status panel: {(newState ? "ABERTO" : "FECHADO")}");
        }
    }
}