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

        var card = UIDark.Card(canvasGO.transform, new Vector2(560f, 320f));
        UIDark.Titulo(card.transform, Loc.T("ui.victory"), new Vector2(0f, 90f), new Color(1f, 0.88f, 0.40f));
        UIDark.Linha(card.transform, new Vector2(0f, 40f), 420f, new Color(0.78f, 0.63f, 0.16f, 0.4f));
        UIDark.Botao(card.transform, Loc.T("ui.main_menu"), new Vector2(0f, -40f), new Vector2(340f, 60f),
            new Color(0.22f, 0.12f, 0.30f, 1f), IrMenu);
    }

    void IrMenu()
    {
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
