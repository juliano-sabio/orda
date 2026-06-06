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

    class FiltroLog : ILogHandler
    {
        readonly ILogHandler inner;
        public FiltroLog(ILogHandler handler) => inner = handler;

        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            try
            {
                string msg = string.Format(format, args);
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
            inner.LogException(exception, context);
        }
    }
}
