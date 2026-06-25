using UnityEngine;

// Desabilita o render do Canvas enquanto o jogo está pausado (Time.timeScale == 0):
// pause, game over e telas de carta/evolução setam timeScale=0, então a barra de boss
// (ScreenSpaceOverlay) não fica aparecendo por cima dessas telas.
[RequireComponent(typeof(Canvas))]
public class OcultarCanvasNoPause : MonoBehaviour
{
    Canvas canvas;

    void Awake() => canvas = GetComponent<Canvas>();

    void Update()
    {
        if (canvas == null) return;
        bool pausado = Time.timeScale == 0f;
        if (canvas.enabled == pausado) canvas.enabled = !pausado;
    }
}
