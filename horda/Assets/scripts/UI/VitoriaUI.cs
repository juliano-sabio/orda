using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Tela de vitória ao escolher "Terminar Run". Botão volta ao menu.
public class VitoriaUI : MonoBehaviour
{
    public static void Mostrar()
    {
        var go = new GameObject("VitoriaUI");
        go.AddComponent<VitoriaUI>().Construir();
        Time.timeScale = 0f;
    }

    void Construir()
    {
        var canvasGO = new GameObject("CanvasVitoria");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 130;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        var bd = new GameObject("Backdrop"); bd.transform.SetParent(canvasGO.transform, false);
        var bdRT = bd.AddComponent<RectTransform>();
        bdRT.anchorMin = Vector2.zero; bdRT.anchorMax = Vector2.one; bdRT.offsetMin = bdRT.offsetMax = Vector2.zero;
        bd.AddComponent<Image>().color = new Color(0.05f, 0.04f, 0.02f, 0.90f);

        // CTA de fim-de-demo: cresce o card se houver botões de wishlist/Discord.
        bool cta = LinksJogo.TemWishlist || LinksJogo.TemDiscord;
        float altura = cta ? 470f : 380f;

        var card = UIDark.Card(canvasGO.transform, new Vector2(560f, altura));
        UIDark.Titulo(card.transform, Loc.T("ui.victory"), new Vector2(0f, altura / 2f - 70f), new Color(1f, 0.88f, 0.40f));
        UIDark.Linha(card.transform, new Vector2(0f, altura / 2f - 120f), 420f, new Color(0.78f, 0.63f, 0.16f, 0.4f));

        // Agradecimento de demo (sempre visível — vale mesmo sem links configurados).
        bool pt = Loc.Current == Language.PT_BR;
        var msgGO = new GameObject("MsgDemo"); msgGO.transform.SetParent(card.transform, false);
        var rt = msgGO.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, altura / 2f - 175f);
        rt.sizeDelta = new Vector2(480f, 70f);
        var tmp = msgGO.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = pt
            ? "Obrigado por jogar a demo!\nA versão completa está a caminho."
            : "Thanks for playing the demo!\nThe full version is on the way.";
        tmp.fontSize = 22; tmp.color = new Color(0.90f, 0.84f, 0.70f);
        tmp.alignment = TMPro.TextAlignmentOptions.Center;

        float y = cta ? 20f : -40f;

        if (LinksJogo.TemWishlist)
        {
            UIDark.Botao(card.transform, pt ? "⭐ WISHLIST NA STEAM" : "⭐ WISHLIST ON STEAM",
                new Vector2(0f, y), new Vector2(360f, 56f), new Color(0.10f, 0.20f, 0.34f, 1f),
                () => LinksJogo.Abrir(LinksJogo.SteamWishlist));
            y -= 66f;
        }
        if (LinksJogo.TemDiscord)
        {
            UIDark.Botao(card.transform, pt ? "ENTRAR NO DISCORD" : "JOIN THE DISCORD",
                new Vector2(0f, y), new Vector2(360f, 56f), new Color(0.20f, 0.16f, 0.38f, 1f),
                () => LinksJogo.Abrir(LinksJogo.Discord));
            y -= 66f;
        }

        UIDark.Botao(card.transform, Loc.T("ui.main_menu"), new Vector2(0f, cta ? y - 4f : -40f),
            new Vector2(340f, 56f), new Color(0.22f, 0.12f, 0.30f, 1f), IrMenu);
    }

    void IrMenu()
    {
        CoopDesconexaoUI.SaidaIntencional = true; // co-op: ida ao menu proposital, sem tela de queda
        Time.timeScale = 1f;
        LimparManagersPersistentes();
        SceneManager.LoadScene("menu_inicial");
    }

    void LimparManagersPersistentes()
    {
        var eventoCanvas = GameObject.Find("EventoCanvas");
        if (eventoCanvas != null) Destroy(eventoCanvas);
        if (GerenciadorEventos.Instance != null)    Destroy(GerenciadorEventos.Instance.gameObject);
        if (UIManager.Instance != null)             Destroy(UIManager.Instance.gameObject);
        if (PauseManager.Instance != null)          Destroy(PauseManager.Instance.gameObject);
        if (SkillManager.Instance != null)          Destroy(SkillManager.Instance.gameObject);
        if (StatusCardSystem.Instance != null)      Destroy(StatusCardSystem.Instance.gameObject);
        if (SkillEvolutionManager.Instance != null) Destroy(SkillEvolutionManager.Instance.gameObject);
        if (SkillEvolutionUI.Instance != null)      Destroy(SkillEvolutionUI.Instance.gameObject);
    }
}
