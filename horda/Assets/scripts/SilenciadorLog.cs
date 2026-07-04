using System;
using UnityEngine;

public class SilenciadorLog : MonoBehaviour
{
    static bool iniciado = false;

    void Awake()
    {
        if (iniciado) { Destroy(gameObject); return; }
        iniciado = true;
        DontDestroyOnLoad(gameObject);
        Debug.unityLogger.logHandler = new FiltroLog(Debug.unityLogger.logHandler);
    }

    void OnDestroy() => iniciado = false;

    class FiltroLog : ILogHandler
    {
        readonly ILogHandler inner;
        public FiltroLog(ILogHandler handler) => inner = handler;

        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            try
            {
                string msg = string.Format(format, args);
                // Diagnóstico co-op (temporário) — silencia o spam de console.
                if (msg.Contains("[Coop")) return; // [CoopEvt], [CoopBoss...], [CoopNetDiag]
                if (msg.Contains("Screen position out of view frustum")) return;
                if (msg.Contains("UnityEditor.Graphs")) return;
                if (msg.Contains("was not found in the") && msg.Contains("font asset")) return;
                if (msg.Contains("atlas texture. Please make the texture") && msg.Contains("readable")) return;
                if (msg.Contains("Unable to add the requested character to font asset")) return;
                if (msg.Contains("CharacterSelectionManager não encontrado")) return;
            }
            catch { }
            inner.LogFormat(logType, context, format, args);
        }

        public void LogException(Exception exception, UnityEngine.Object context)
        {
            if (exception?.StackTrace?.Contains("UnityEditor.Graphs") == true) return;
            if (exception?.Message?.Contains("UnityEditor.Graphs") == true) return;
            if (exception?.StackTrace?.Contains("ATGTextJobSystem") == true) return;
            if (exception?.StackTrace?.Contains("ConvertMeshInfoToUIRVertex") == true) return;
            if (exception is MissingReferenceException &&
                exception.StackTrace?.Contains("PunicaoDivinaUltimate") == true) return;
            // NGO: NREs internas no SHUTDOWN (NetworkManager/NetworkObject OnDestroy/Dispose ao parar o
            // play/fechar o app) — teardown do pacote netcode, não afeta gameplay. Só ruído de console.
            if (exception is NullReferenceException &&
                exception.StackTrace?.Contains("com.unity.netcode.gameobjects") == true &&
                (exception.StackTrace.Contains(".OnDestroy") ||
                 exception.StackTrace.Contains("ShutdownInternal") ||
                 exception.StackTrace.Contains(".Dispose"))) return;
            inner.LogException(exception, context);
        }
    }
}
