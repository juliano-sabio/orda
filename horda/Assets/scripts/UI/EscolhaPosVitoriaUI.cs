using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Aparece ao derrotar o boss final: escolher Terminar a run ou seguir no Modo Infinito.
// Pausa o jogo (timeScale = 0) enquanto aberta. Botões funcionam em timeScale 0.
public class EscolhaPosVitoriaUI : MonoBehaviour
{
    TimerManager timer;

    public static void Mostrar(TimerManager tm)
    {
        var go = new GameObject("EscolhaPosVitoriaUI");
        var ui = go.AddComponent<EscolhaPosVitoriaUI>();
        ui.timer = tm;
        ui.Construir();
        Time.timeScale = 0f;
    }

    void Construir()
    {
        var canvasGO = new GameObject("CanvasEscolhaVitoria");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // backdrop escuro
        var bd = new GameObject("Backdrop"); bd.transform.SetParent(canvasGO.transform, false);
        var bdRT = bd.AddComponent<RectTransform>();
        bdRT.anchorMin = Vector2.zero; bdRT.anchorMax = Vector2.one; bdRT.offsetMin = bdRT.offsetMax = Vector2.zero;
        bd.AddComponent<Image>().color = new Color(0.04f, 0.03f, 0.07f, 0.85f);

        var card = UIDark.Card(canvasGO.transform, new Vector2(560f, 380f));
        UIDark.Titulo(card.transform, Loc.T("ui.victory_title"), new Vector2(0f, 130f),
            new Color(0.95f, 0.82f, 0.40f));
        UIDark.Linha(card.transform, new Vector2(0f, 80f), 420f, new Color(0.78f, 0.63f, 0.16f, 0.4f));

        UIDark.Botao(card.transform, Loc.T("ui.end_run"), new Vector2(0f, 10f), new Vector2(380f, 64f),
            new Color(0.10f, 0.38f, 0.16f, 1f), Terminar);
        UIDark.Botao(card.transform, Loc.T("ui.continue_infinite"), new Vector2(0f, -70f), new Vector2(380f, 64f),
            new Color(0.30f, 0.12f, 0.40f, 1f), ContinuarInfinito);
    }

    void Terminar()
    {
        Time.timeScale = 1f;
        Destroy(gameObject);
        VitoriaUI.Mostrar();
    }

    void ContinuarInfinito()
    {
        Time.timeScale = 1f;
        Destroy(gameObject);
        if (timer != null) timer.ReiniciarCiclo();
    }
}
