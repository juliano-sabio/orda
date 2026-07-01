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
    UltimateBloqueioOverlay ovUltimateBloqueio;
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
        ovUltimateBloqueio = CriarBloqueioOverlay(ui.ultimateSkillIcon);
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

        // Bloqueio anti-ultimate (projétil da SlimeProtetora): X vermelho tem prioridade sobre tudo.
        if (player.ultimateBloqueada)
        {
            ovUltimateBloqueio?.Mostrar();
            ovUltimate.Esconder();
            return;
        }
        ovUltimateBloqueio?.Esconder();

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

    UltimateBloqueioOverlay CriarBloqueioOverlay(Image icone)
    {
        if (icone == null) return null;
        var go  = new GameObject("UltimateBloqueioOverlay");
        go.transform.SetParent(icone.transform, false);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var ov  = go.AddComponent<UltimateBloqueioOverlay>();
        ov.Construir();
        return ov;
    }
}

// ── X vermelho de "ultimate bloqueada" sobre o ícone ──────────────────────────

public class UltimateBloqueioOverlay : MonoBehaviour
{
    RectTransform meuRT;
    RectTransform bar1, bar2;

    public void Construir()
    {
        meuRT = GetComponent<RectTransform>();

        // Fundo escuro/avermelhado pra indicar bloqueio
        var bgGO = new GameObject("Bg");
        bgGO.transform.SetParent(transform, false);
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.18f, 0f, 0f, 0.55f);
        bgImg.raycastTarget = false;

        // Duas barras vermelhas formando o X
        bar1 = CriarBarra();
        bar2 = CriarBarra();

        gameObject.SetActive(false);
    }

    RectTransform CriarBarra()
    {
        var go = new GameObject("Barra");
        go.transform.SetParent(transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        var img = go.AddComponent<Image>();          // sem sprite = quad branco tingido
        img.color = new Color(1f, 0.12f, 0.12f, 0.95f);
        img.raycastTarget = false;
        return rt;
    }

    public void Mostrar()
    {
        if (!gameObject.activeInHierarchy) gameObject.SetActive(true);
        // Dimensiona o X conforme o tamanho atual do ícone
        var r = meuRT.rect;
        float diag = Mathf.Sqrt(r.width * r.width + r.height * r.height) * 0.82f;
        float esp  = Mathf.Max(3f, Mathf.Min(r.width, r.height) * 0.14f);
        if (bar1 != null) { bar1.sizeDelta = new Vector2(diag, esp); bar1.localEulerAngles = new Vector3(0f, 0f, 45f); }
        if (bar2 != null) { bar2.sizeDelta = new Vector2(diag, esp); bar2.localEulerAngles = new Vector3(0f, 0f, -45f); }
    }

    public void Esconder()
    {
        if (gameObject.activeInHierarchy) gameObject.SetActive(false);
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
