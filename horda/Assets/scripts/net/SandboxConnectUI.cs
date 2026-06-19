using Unity.Netcode;
using UnityEngine;

// UI de teste (IMGUI) só para o sandbox: Host, campo de código, Join, status.
// Feia de propósito — não é UI de produção.
public class SandboxConnectUI : MonoBehaviour
{
    string joinCode = "";
    string status = "Pronto.";
    [SerializeField] int maxPlayers = 4;

    async void StartHost()
    {
        status = "Iniciando host...";
        try
        {
            await NetBootstrap.InitAsync();
            joinCode = await RelayConnector.HostAsync(maxPlayers);
            status = "Host no ar. Código: " + joinCode;
        }
        catch (System.Exception e) { status = "Falha no host: " + e.Message; }
    }

    async void StartJoin()
    {
        status = "Entrando...";
        try
        {
            await NetBootstrap.InitAsync();
            await RelayConnector.JoinAsync(joinCode.Trim());
            status = "Conectado.";
        }
        catch (System.Exception e) { status = "Falha ao entrar: " + e.Message; }
    }

    void OnGUI()
    {
        var nm = NetworkManager.Singleton;
        if (nm != null && (nm.IsListening || nm.IsConnectedClient))
        {
            GUI.Label(new Rect(10, 10, 700, 30), status);

            float y = 45f;
            if (!string.IsNullOrEmpty(joinCode))
            {
                GUI.Label(new Rect(10, y, 70, 28), "Código:");
                // campo selecionável (dá pra marcar e Ctrl+C também)
                GUI.TextField(new Rect(80, y, 150, 28), joinCode);
                if (GUI.Button(new Rect(240, y - 3, 150, 34), "Copiar código"))
                {
                    GUIUtility.systemCopyBuffer = joinCode;
                    status = "Código copiado: " + joinCode;
                }
                y += 42f;
            }

            if (GUI.Button(new Rect(10, y, 140, 36), "Desconectar")) nm.Shutdown();
            return;
        }

        if (GUI.Button(new Rect(10, 10, 140, 40), "Host")) StartHost();
        GUI.Label(new Rect(10, 60, 80, 30), "Código:");
        joinCode = GUI.TextField(new Rect(90, 60, 180, 30), joinCode);
        if (GUI.Button(new Rect(280, 56, 140, 40), "Join")) StartJoin();
        GUI.Label(new Rect(10, 110, 700, 60), status);
    }
}
