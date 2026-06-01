using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillCooldownDisplay : MonoBehaviour
{
    UIManager   ui;
    SkillManager sm;
    PlayerStats  player;

    CooldownOverlay ovDefesa1;
    CooldownOverlay ovDefesa2;
    bool inicializado = false;

    void Update()
    {
        if (!inicializado) TentarInicializar();
        if (!inicializado) return;

        var lista = ObterSkillsComRecarga();
        AtualizarSlot(ovDefesa1, lista.Count > 0 ? lista[0] : null);
        AtualizarSlot(ovDefesa2, lista.Count > 1 ? lista[1] : null);
    }

    void TentarInicializar()
    {
        if (ui == null)     ui     = GetComponent<UIManager>();
        if (sm == null)     sm     = FindFirstObjectByType<SkillManager>();
        if (player == null) player = FindFirstObjectByType<PlayerStats>();

        if (ui == null || sm == null || player == null) return;
        if (ui.defenseSkill1Icon == null) return;

        ovDefesa1 = CriarOverlay(ui.defenseSkill1Icon);
        ovDefesa2 = CriarOverlay(ui.defenseSkill2Icon);
        inicializado = true;
    }

    List<ISkillComRecarga> ObterSkillsComRecarga()
    {
        var lista = new List<ISkillComRecarga>();
        var behaviors = player.GetComponents<SkillBehavior>();
        foreach (var b in behaviors)
        {
            ISkillComRecarga r = b as ISkillComRecarga;
            if (r != null) lista.Add(r);
        }
        return lista;
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
        var go = new GameObject("CooldownOverlay");
        go.transform.SetParent(icone.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var ov = go.AddComponent<CooldownOverlay>();
        ov.Construir();
        return ov;
    }
}

// ─────────────────────────────────────────────────────────────────────────────

public class CooldownOverlay : MonoBehaviour
{
    Image           imgEscuro;
    Image           imgRadial;
    TextMeshProUGUI textoTimer;

    public void Construir()
    {
        var bgGO = new GameObject("BG");
        bgGO.transform.SetParent(transform, false);
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        imgEscuro = bgGO.AddComponent<Image>();
        imgEscuro.color = new Color(0f, 0f, 0f, 0.65f);
        imgEscuro.raycastTarget = false;

        var radGO = new GameObject("Radial");
        radGO.transform.SetParent(transform, false);
        var radRT = radGO.AddComponent<RectTransform>();
        radRT.anchorMin = Vector2.zero; radRT.anchorMax = Vector2.one;
        radRT.offsetMin = radRT.offsetMax = Vector2.zero;
        imgRadial = radGO.AddComponent<Image>();
        imgRadial.color = new Color(0.3f, 0.6f, 1f, 0.35f);
        imgRadial.type = Image.Type.Filled;
        imgRadial.fillMethod = Image.FillMethod.Radial360;
        imgRadial.fillOrigin = 2; // Top
        imgRadial.fillClockwise = true;
        imgRadial.fillAmount = 1f;
        imgRadial.raycastTarget = false;

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
        textoTimer.enableWordWrapping = false;

        gameObject.SetActive(false);
    }

    public void Mostrar(float pct, int segundos)
    {
        if (!gameObject.activeInHierarchy) gameObject.SetActive(true);
        if (imgRadial != null)  imgRadial.fillAmount = pct;
        if (textoTimer != null)
            textoTimer.text = segundos >= 60 ? $"{segundos / 60}m{segundos % 60:D2}s" : $"{segundos}s";
    }

    public void Esconder()
    {
        if (gameObject.activeInHierarchy) gameObject.SetActive(false);
    }
}
