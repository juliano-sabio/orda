using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// [debug temp] Instrumentação do caminho de (des)conexão co-op. Loga POR QUÊ/QUANDO o cliente
// perde a conexão (item 5 do MP: "cliente desconecta no meio" → passa a rodar evento local).
// Auto-anexa (não precisa estar em cena). Não muda comportamento — só registra. REMOVER no release.
public class CoopNetDiag : MonoBehaviour
{
    static CoopNetDiag _i;
    NetworkManager nmInscrito;
    float tConectado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Auto()
    {
        if (_i != null) return;
        var go = new GameObject("CoopNetDiag");
        DontDestroyOnLoad(go);
        _i = go.AddComponent<CoopNetDiag>();
    }

    void Update()
    {
        var nm = NetworkManager.Singleton;
        if (nm == nmInscrito) return;     // já inscrito no NM atual (ou ambos null)
        Desinscrever();
        if (nm != null) Inscrever(nm);
    }

    void OnDestroy() => Desinscrever();

    void Inscrever(NetworkManager nm)
    {
        nmInscrito = nm;
        nm.OnClientConnectedCallback  += AoConectar;
        nm.OnClientDisconnectCallback += AoDesconectar;
        nm.OnTransportFailure         += AoFalhaTransporte;
        Debug.Log($"[CoopNetDiag] inscrito (IsServer={nm.IsServer} IsClient={nm.IsClient} cena={SceneManager.GetActiveScene().name})");
    }

    void Desinscrever()
    {
        if (nmInscrito == null) return;
        nmInscrito.OnClientConnectedCallback  -= AoConectar;
        nmInscrito.OnClientDisconnectCallback -= AoDesconectar;
        nmInscrito.OnTransportFailure         -= AoFalhaTransporte;
        nmInscrito = null;
    }

    void AoConectar(ulong id)
    {
        var nm = NetworkManager.Singleton;
        tConectado = Time.realtimeSinceStartup;
        Debug.Log($"[CoopNetDiag] CONECTOU clientId={id} local={nm?.LocalClientId} papel={(nm != null && nm.IsServer ? "HOST" : "CLIENTE")} cena={SceneManager.GetActiveScene().name}");
    }

    void AoDesconectar(ulong id)
    {
        var nm = NetworkManager.Singleton;
        string motivo = nm != null && !string.IsNullOrEmpty(nm.DisconnectReason) ? nm.DisconnectReason : "(vazio)";
        float dur = Time.realtimeSinceStartup - tConectado;
        bool souHost = nm != null && nm.IsServer;
        // No cliente, id == LocalClientId quando é ELE quem caiu. No host, id = o cliente que saiu.
        Debug.LogWarning($"[CoopNetDiag] DESCONECTOU id={id} papel={(souHost ? "HOST" : "CLIENTE")} local={nm?.LocalClientId} motivo='{motivo}' aindaConectado={nm?.IsConnectedClient} apos={dur:0.0}s cena={SceneManager.GetActiveScene().name}");
    }

    void AoFalhaTransporte()
    {
        Debug.LogError($"[CoopNetDiag] FALHA DE TRANSPORTE (UTP/Relay caiu) cena={SceneManager.GetActiveScene().name} apos={(Time.realtimeSinceStartup - tConectado):0.0}s");
    }
}
