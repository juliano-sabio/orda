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
            }
            catch { }
            inner.LogFormat(logType, context, format, args);
        }

        public void LogException(Exception exception, UnityEngine.Object context)
        {
            if (exception?.StackTrace?.Contains("UnityEditor.Graphs") == true) return;
            if (exception?.Message?.Contains("UnityEditor.Graphs") == true) return;
            inner.LogException(exception, context);
        }
    }
}
