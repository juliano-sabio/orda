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
        if (player == null) player = FindFirstObjectByType<PlayerStats>();

        if (ui == null || sm == null || player == null) return;
        if (ui.attackSkill1Icon == null && ui.defenseSkill1Icon == null) return;

        ovAtaque1 = CriarOverlay(ui.attackSkill1Icon);
        ovDefesa1 = CriarOverlay(ui.defenseSkill1Icon);
        ovAtaque2 = CriarOverlay(ui.attackSkill2Icon);
        ovDefesa2 = CriarOverlay(ui.defenseSkill2Icon);
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

        // Mapeia behaviors na ordem dos slots ativos (0=ataque1, 1=defesa1, 2=ataque2, 3=defesa2)
        // Os behaviors são adicionados na mesma ordem das skills ativas
        AtualizarSlot(ovAtaque1, lista.Count > 0 ? lista[0] : null);
        AtualizarSlot(ovDefesa1, lista.Count > 1 ? lista[1] : null);
        AtualizarSlot(ovAtaque2, lista.Count > 2 ? lista[2] : null);
        AtualizarSlot(ovDefesa2, lista.Count > 3 ? lista[3] : null);
    }

    void AtualizarSlot(CooldownOverlay ov, ISkillComRecarga skill)
    {
        if (ov == null) return;
        if (skill != null && skill.EmRecarga)
        {
            float pct = skill.RecargaTotal > 0f ? skill.TimerRecarga / skill.RecargaTotal : 0f;
            ov.Mostrar(pct, Mathf.CeilToInt(skill.TimerRecarga));
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
    Image           imgEscuro;
    Image           imgRadial;
    TextMeshProUGUI textoTimer;

    public void Construir()
    {
        // Fundo escuro
        var bgGO = new GameObject("BG");
        bgGO.transform.SetParent(transform, false);
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        imgEscuro = bgGO.AddComponent<Image>();
        imgEscuro.color = new Color(0f, 0f, 0f, 0.35f);
        imgEscuro.raycastTarget = false;

        // Fill radial
        var radGO = new GameObject("Radial");
        radGO.transform.SetParent(transform, false);
        var radRT = radGO.AddComponent<RectTransform>();
        radRT.anchorMin = Vector2.zero; radRT.anchorMax = Vector2.one;
        radRT.offsetMin = radRT.offsetMax = Vector2.zero;
        imgRadial = radGO.AddComponent<Image>();
        imgRadial.color = new Color(0.3f, 0.6f, 1f, 0.2f);
        imgRadial.type = Image.Type.Filled;
        imgRadial.fillMethod = Image.FillMethod.Radial360;
        imgRadial.fillOrigin = 2; // Top
        imgRadial.fillClockwise = true;
        imgRadial.fillAmount = 1f;
        imgRadial.raycastTarget = false;

        // Texto do timer
        var txtGO = new GameObject("Timer");
        txtGO.transform.SetParent(transform, false);
        var txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;
        textoTimer = txtGO.AddComponent<TextMeshProUGUI>();
        textoTimer.fontSize  = 14f;
        textoTimer.fontStyle = FontStyles.Bold;
        textoTimer.alignment = TextAlignmentOptions.Center;
        textoTimer.color     = Color.white;
        textoTimer.textWrappingMode = TMPro.TextWrappingModes.NoWrap;

        gameObject.SetActive(false);
    }

    public void Mostrar(float pct, int segundos)
    {
        if (!gameObject.activeInHierarchy) gameObject.SetActive(true);
        if (imgRadial  != null) imgRadial.fillAmount = pct;
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
