using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

// Gravador de clipe pra marketing: captura a tela em PNGs (AsyncGPUReadback, sem travar) numa
// pasta + gera um montar.bat (ffmpeg) que vira MP4. Botão "● Gravar" (ocioso) + atalho F9.
// Durante a gravação NÃO desenha overlay → clipe limpo. Só ativo no editor / dev build (não
// aparece pro jogador no build de release).
public class GravadorClipe : MonoBehaviour
{
    const int LADO_MAX = 1280;     // maior dimensão do clipe
    const int FPS = 30;
    const float DUR_MAX = 120f;    // auto-stop de segurança

    static GravadorClipe _i;
    bool gravando;
    string pasta;
    int frame;
    float tInicio, proxFrame;
    RenderTexture rtFull, rtOut;
    int larg, alt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (_i != null) return;
        if (!(Application.isEditor || Debug.isDebugBuild)) return; // só editor/dev build
        var go = new GameObject("GravadorClipe");
        DontDestroyOnLoad(go);
        _i = go.AddComponent<GravadorClipe>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9)) Alternar();
        if (gravando && Time.unscaledTime - tInicio > DUR_MAX) Parar();
    }

    void OnGUI()
    {
        if (gravando)
        {
            // indicador discreto e pulsante no canto (captura é tela cheia → fica pequeno no clipe).
            float pulso = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 4f);
            var st = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold };
            float t = Time.unscaledTime - tInicio;
            var cor = GUI.color;
            GUI.color = new Color(1f, 0.2f, 0.2f, pulso);
            GUI.Label(new Rect(12, 8, 30, 22), "●", st);
            GUI.color = new Color(1f, 1f, 1f, 0.85f);
            GUI.Label(new Rect(30, 8, 140, 22), $"REC  {t:00.0}s", st);
            GUI.color = cor;
            return;
        }
        var estilo = new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold };
        var r = new Rect(Screen.width - 150, 10, 140, 30);
        if (GUI.Button(r, "● Gravar (F9)", estilo)) Alternar();
    }

    void Alternar() { if (gravando) Parar(); else Iniciar(); }

    void Iniciar()
    {
        int sw = Screen.width, sh = Screen.height;
        float s = Mathf.Min(1f, (float)LADO_MAX / Mathf.Max(sw, sh));
        larg = Mathf.RoundToInt(sw * s); larg -= larg % 2;
        alt  = Mathf.RoundToInt(sh * s); alt  -= alt  % 2;
        if (larg < 2 || alt < 2) { Debug.LogWarning("[GravadorClipe] tela inválida"); return; }

        pasta = Path.Combine(Application.persistentDataPath, "captures",
                             "clip_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(pasta);

        rtFull = new RenderTexture(sw, sh, 0);
        rtOut  = new RenderTexture(larg, alt, 0);

        frame = 0;
        tInicio = Time.unscaledTime;
        proxFrame = 0f;
        gravando = true;
        StartCoroutine(Loop());
        Debug.Log($"[GravadorClipe] ● Gravando ({larg}x{alt} @ {FPS}fps) → {pasta}");
    }

    void Parar()
    {
        if (!gravando) return;
        gravando = false;
        StopCoroutine(Loop());
        if (rtFull != null) { rtFull.Release(); rtFull = null; }
        if (rtOut  != null) { rtOut.Release();  rtOut  = null; }
        EscreverScriptFfmpeg();
        Debug.Log($"[GravadorClipe] ■ Parado. {frame} frames em {pasta}\n" +
                  $"Rode o montar.bat (precisa do ffmpeg no PATH) pra gerar o clip.mp4.");
    }

    IEnumerator Loop()
    {
        var fimDeFrame = new WaitForEndOfFrame();
        while (gravando)
        {
            yield return fimDeFrame;
            if (Time.unscaledTime < proxFrame) continue;
            proxFrame = Time.unscaledTime + 1f / FPS;

            if (rtFull == null || rtOut == null) yield break;
            ScreenCapture.CaptureScreenshotIntoRenderTexture(rtFull);
            // copia escalando + invertendo Y (a captura vem de cabeça pra baixo)
            Graphics.Blit(rtFull, rtOut, new Vector2(1f, -1f), new Vector2(0f, 1f));

            int idx = frame++;
            int w = larg, h = alt;
            string destino = Path.Combine(pasta, $"f_{idx:00000}.png");
            AsyncGPUReadback.Request(rtOut, 0, TextureFormat.RGBA32, req =>
            {
                if (req.hasError) return;
                var bytes = req.GetData<byte>().ToArray(); // cópia fora do buffer nativo
                Task.Run(() =>
                {
                    try
                    {
                        var png = ImageConversion.EncodeArrayToPNG(
                            bytes, GraphicsFormat.R8G8B8A8_UNorm, (uint)w, (uint)h);
                        File.WriteAllBytes(destino, png);
                    }
                    catch (Exception e) { Debug.LogWarning("[GravadorClipe] encode falhou: " + e.Message); }
                });
            });
        }
    }

    void EscreverScriptFfmpeg()
    {
        try
        {
            string bat = Path.Combine(pasta, "montar.bat");
            File.WriteAllText(bat,
                "@echo off\r\n" +
                "REM Precisa do ffmpeg no PATH. Gera clip.mp4 a partir dos PNGs.\r\n" +
                $"ffmpeg -y -framerate {FPS} -i f_%05d.png -c:v libx264 -pix_fmt yuv420p -crf 18 clip.mp4\r\n" +
                "echo.\r\n" +
                "echo Pronto: clip.mp4\r\n" +
                "pause\r\n");

            string sh = Path.Combine(pasta, "montar.sh");
            File.WriteAllText(sh,
                "#!/bin/sh\n" +
                $"ffmpeg -y -framerate {FPS} -i f_%05d.png -c:v libx264 -pix_fmt yuv420p -crf 18 clip.mp4\n");
        }
        catch (Exception e) { Debug.LogWarning("[GravadorClipe] script ffmpeg falhou: " + e.Message); }
    }
}
