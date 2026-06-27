using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

// Cria/entra em uma sessão via Unity Relay e configura o UnityTransport.
// HostAsync devolve o join code REAL — é o que, no Sub-projeto 5, substitui
// o GerarCodigo() falso de LobbyUI.cs.
public static class RelayConnector
{
    static UnityTransport Transport =>
        NetworkManager.Singleton.GetComponent<UnityTransport>();

    // Headroom pra picos de tráfego (horde co-op: 40+ inimigos com NetworkTransform + RPCs +
    // NetVars num mesmo frame). O default 128 (send E receive queue) estoura nesses picos →
    // pacotes caem → o cliente desconecta no meio. 512 dá 4x de folga.
    static void ConfigurarTransport()
    {
        var t = Transport;
        if (t != null) t.MaxPacketQueueSize = 512;
    }

    public static async Task<string> HostAsync(int maxPlayers)
    {
        Allocation alloc = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);
        Transport.SetRelayServerData(new RelayServerData(alloc, "dtls"));
        ConfigurarTransport();
        NetworkManager.Singleton.StartHost();
        return joinCode;
    }

    public static async Task JoinAsync(string joinCode)
    {
        JoinAllocation alloc = await RelayService.Instance.JoinAllocationAsync(joinCode);
        Transport.SetRelayServerData(new RelayServerData(alloc, "dtls"));
        ConfigurarTransport();
        NetworkManager.Singleton.StartClient();
    }
}
