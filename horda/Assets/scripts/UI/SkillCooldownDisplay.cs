using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Adiciona overlays de cooldown nos ícones de skill do UIManager.
// É adicionado automaticamente pelo UIManager no Start().
public class SkillCooldownDisplay : MonoBehaviour
{
    UIManager    ui;
    SkillManager sm;
    PlayerStats  player;

    CooldownOverlay ovAtaque1;
    CooldownOverlay ovDefesa1;
    CooldownOverlay ovAtaque2;
    CooldownOverlay ovDefesa2;
    CooldownOverlay ovUltimate;
    bool ultimateJaUsada    = false;
    bool ultimateProntaAntes = false;

    bool inicializado = false;

    void Update()
    {
        if (!inicializado) TentarInicializar();
        if (!inicializado) return;

        AtualizarTodos();
    }

    void TentarInicializar()
    {
        if (ui     == null) ui     = GetComponent<UIManager>();
        if (sm     == null) sm     = FindFirstObjectByType<SkillManager>();
        if (player == null) player = PlayerStats.Local ?? FindFirstObjectByType<PlayerStats>(); // coop-local-ok: HUD pessoal (P4) do player local

        if (ui == null || sm == null || player == null) return;
        if (ui.attackSkill1Icon == null && ui.defenseSkill1Icon == null) return;

        ovAtaque1  = CriarOverlay(ui.attackSkill1Icon);
        ovDefesa1  = CriarOverlay(ui.defenseSkill1Icon);
        ovAtaque2  = CriarOverlay(ui.attackSkill2Icon);
        ovDefesa2  = CriarOverlay(ui.defenseSkill2Icon);
        ovUltimate = CriarOverlay(ui.ultimateSkillIcon);
        inicializado = true;
    }

    void AtualizarTodos()
    {
        if (player == null || sm == null) return;

        var behaviors = player.GetComponents<SkillBehavior>();
        var lista     = new List<ISkillComRecarga>();
        foreach (var b in behaviors)
        {
            var r = b as ISkillComRecarga;
            if (r != null) lista.Add(r);
        }

        AtualizarSlot(ovAtaque1, lista.Count > 0 ? lista[0] : null);
        AtualizarSlot(ovDefesa1, lista.Count > 1 ? lista[1] : null);
        AtualizarSlot(ovAtaque2, lista.Count > 2 ? lista[2] : null);
        AtualizarSlot(ovDefesa2, lista.Count > 3 ? lista[3] : null);
        AtualizarSlotUltimate();
    }

    void AtualizarSlotUltimate()
    {
        if (ovUltimate == null || player == null) return;
        bool pronta = player.IsUltimateReady();

        // Detecta uso da ultimate: estava pronta, agora não está
        if (!pronta && ultimateProntaAntes) ultimateJaUsada = true;
        // Ficou pronta de novo: reseta para próximo ciclo
        if (pronta) ultimateJaUsada = false;
        ultimateProntaAntes = pronta;

        // Só mostra o overlay após a ultimate ter sido usada ao menos uma vez
        if (!pronta && ultimateJaUsada)
        {
            float restante = player.ultimateCooldown - player.ultimateChargeTime;
            ovUltimate.Mostrar(Mathf.CeilToInt(restante));
        }
        else
            ovUltimate.Esconder();
    }

    void AtualizarSlot(CooldownOverlay ov, ISkillComRecarga skill)
    {
        if (ov == null) return;
        if (skill != null && skill.EmRecarga)
        {
            ov.Mostrar(Mathf.CeilToInt(skill.TimerRecarga));
        }
        else
            ov.Esconder();
    }

    CooldownOverlay CriarOverlay(Image icone)
    {
        if (icone == null) return null;
        var go  = new GameObject("CooldownOverlay");
        go.transform.SetParent(icone.transform, false);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var ov  = go.AddComponent<CooldownOverlay>();
        ov.Construir();
        return ov;
    }
}

// ── Overlay de um único slot ──────────────────────────────────────────────────

public class CooldownOverlay : MonoBehaviour
{
    TextMeshProUGUI textoTimer;

    public void Construir()
    {
        var txtGO = new GameObject("Timer");
        txtGO.transform.SetParent(transform, false);
        var txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;
        textoTimer = txtGO.AddComponent<TextMeshProUGUI>();
        textoTimer.fontSize     = 20f;
        textoTimer.fontStyle    = FontStyles.Bold;
        textoTimer.alignment    = TextAlignmentOptions.Center;
        textoTimer.color        = Color.white;
        textoTimer.outlineWidth = 0.4f;
        textoTimer.outlineColor = new Color32(0, 0, 0, 255);
        textoTimer.textWrappingMode = TMPro.TextWrappingModes.NoWrap;

        gameObject.SetActive(false);
    }

    public void Mostrar(int segundos)
    {
        if (!gameObject.activeInHierarchy) gameObject.SetActive(true);
        if (textoTimer != null)
            textoTimer.text = segundos >= 60
                ? $"{segundos / 60}m{segundos % 60:D2}s"
                : $"{segundos}s";
    }

    public void Esconder()
    {
        if (gameObject.activeInHierarchy) gameObject.SetActive(false);
    }
}
