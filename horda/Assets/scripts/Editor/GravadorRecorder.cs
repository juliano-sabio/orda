#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

// Grava clipe em alta qualidade via Unity Recorder: 1080p @ 60fps, MP4 H.264, color space correto,
// resolução FIXA (não depende do tamanho da Game view). Atalho: Ctrl+Shift+R (ou menu Horda).
// Precisa estar em PLAY (a captura é da Game view). Salva em <projeto>/Gravacoes/.
public static class GravadorRecorder
{
    static RecorderController _ctrl;

    [MenuItem("Horda/● Gravar Clipe (toggle) %#r")]
    public static void Toggle()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Gravador",
                "Entre em Play primeiro — a captura é da Game view.", "OK");
            return;
        }
        if (_ctrl != null && _ctrl.IsRecording()) Parar();
        else Iniciar();
    }

    static void Iniciar()
    {
        var rcs = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        rcs.SetRecordModeToManual();
        rcs.FrameRate = 60f;
        rcs.CapFrameRate = false;

        var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movie.name = "HordaClip";
        movie.Enabled = true;
        movie.EncoderSettings = new CoreEncoderSettings
        {
            EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
            Codec = CoreEncoderSettings.OutputCodec.MP4
        };
        movie.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = 1920,
            OutputHeight = 1080
        };

        string pasta = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Gravacoes");
        Directory.CreateDirectory(pasta);
        movie.OutputFile = Path.Combine(pasta, "clip_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));

        rcs.AddRecorderSettings(movie);

        _ctrl = new RecorderController(rcs);
        _ctrl.PrepareRecording();
        _ctrl.StartRecording();
        Debug.Log("[GravadorRecorder] ● Gravando 1080p/60fps → " + movie.OutputFile + ".mp4");
    }

    static void Parar()
    {
        if (_ctrl != null) { _ctrl.StopRecording(); _ctrl = null; }
        Debug.Log("[GravadorRecorder] ■ Parado. Vídeo na pasta Gravacoes/ (raiz do projeto).");
    }
}
#endif
