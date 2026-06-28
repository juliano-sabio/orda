using System;
using System.Collections;
using System.Collections.Generic;
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
    Coroutine loopCo;
    readonly List<Task> tarefas = new List<Task>(); // escritas de PNG pendentes

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
        lock (tarefas) tarefas.Clear();
        gravando = true;
        loopCo = StartCoroutine(Loop());
        Debug.Log($"[GravadorClipe] ● Gravando ({larg}x{alt} @ {FPS}fps) → {pasta}");
    }

    void Parar()
    {
        if (!gravando) return;
        gravando = false;
        if (loopCo != null) { StopCoroutine(loopCo); loopCo = null; }
        AsyncGPUReadback.WaitAllRequests(); // garante que todos os readbacks viraram tasks de escrita
        if (rtFull != null) { rtFull.Release(); rtFull = null; }
        if (rtOut  != null) { rtOut.Release();  rtOut  = null; }
        EscreverScriptFfmpeg();
        Debug.Log($"[GravadorClipe] ■ Parado. {frame} frames. Encodando MP4...");
        FinalizarEEncodar(pasta);
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
                var tarefa = Task.Run(() =>
                {
                    try
                    {
                        var png = ImageConversion.EncodeArrayToPNG(
                            bytes, GraphicsFormat.R8G8B8A8_UNorm, (uint)w, (uint)h);
                        File.WriteAllBytes(destino, png);
                    }
                    catch (Exception e) { Debug.LogWarning("[GravadorClipe] encode falhou: " + e.Message); }
                });
                lock (tarefas) tarefas.Add(tarefa);
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

    // Espera os PNGs terminarem de gravar e roda o ffmpeg → clip.mp4 (tudo em background).
    void FinalizarEEncodar(string pastaClipe)
    {
        Task[] pendentes;
        lock (tarefas) { pendentes = tarefas.ToArray(); tarefas.Clear(); }
        string ff = AcharFfmpeg();

        Task.Run(async () =>
        {
            try
            {
                await Task.WhenAll(pendentes); // garante todos os frames no disco
                if (string.IsNullOrEmpty(ff))
                {
                    Debug.LogWarning("[GravadorClipe] ffmpeg não encontrado — rode o montar.bat manualmente.");
                    return;
                }
                var psi = new System.Diagnostics.ProcessStartInfo(ff,
                    $"-y -framerate {FPS} -i f_%05d.png -c:v libx264 -pix_fmt yuv420p -crf 18 clip.mp4")
                {
                    WorkingDirectory = pastaClipe,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                };
                using (var p = System.Diagnostics.Process.Start(psi))
                {
                    string err = p.StandardError.ReadToEnd();
                    p.WaitForExit();
                    if (p.ExitCode == 0)
                        Debug.Log("[GravadorClipe] ✅ MP4 pronto: " + Path.Combine(pastaClipe, "clip.mp4"));
                    else
                        Debug.LogWarning("[GravadorClipe] ffmpeg saiu " + p.ExitCode + ":\n" + err);
                }
            }
            catch (Exception e) { Debug.LogWarning("[GravadorClipe] encode MP4 falhou: " + e.Message); }
        });
    }

    // Acha o ffmpeg: PATH → WinGet Packages (Gyan.FFmpeg) → alias WindowsApps. Cacheado.
    static string _ff; static bool _ffBuscado;
    static string AcharFfmpeg()
    {
        if (_ffBuscado) return _ff;
        _ffBuscado = true;
        try
        {
            string path = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (var dir in path.Split(';'))
            {
                if (string.IsNullOrWhiteSpace(dir)) continue;
                try { string c = Path.Combine(dir.Trim(), "ffmpeg.exe"); if (File.Exists(c)) { _ff = c; return _ff; } }
                catch { }
            }
            string lad = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string wg = Path.Combine(lad, "Microsoft", "WinGet", "Packages");
            if (Directory.Exists(wg))
            {
                var achados = Directory.GetFiles(wg, "ffmpeg.exe", SearchOption.AllDirectories);
                if (achados.Length > 0) { _ff = achados[0]; return _ff; }
            }
            string alias = Path.Combine(lad, "Microsoft", "WindowsApps", "ffmpeg.exe");
            if (File.Exists(alias)) { _ff = alias; return _ff; }
        }
        catch { }
        return _ff;
    }
}
