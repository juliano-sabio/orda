using System.Threading.Tasks;
using UnityEngine;
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
    // Limite de jogadores do co-op. Por enquanto travado em 2 (3–4 ainda não validado).
    // Para reabrir depois: suba este número e a alocação do Relay acompanha.
    public const int MaxJogadoresCoop = 2;

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
        // Trava em 2 por enquanto (independente do que a UI pedir).
        maxPlayers = Mathf.Clamp(maxPlayers, 2, MaxJogadoresCoop);

        Allocation alloc = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);
        Transport.SetRelayServerData(new RelayServerData(alloc, "dtls"));
        ConfigurarTransport();

        // Backstop autoritativo: mesmo que alguém entre por código, o host chuta quem
        // exceder o limite logo após conectar (não mexe no auto-spawn de player do NGO).
        var nm = NetworkManager.Singleton;
        nm.OnClientConnectedCallback -= AoClienteConectar;
        nm.OnClientConnectedCallback += AoClienteConectar;

        nm.StartHost();
        return joinCode;
    }

    static void AoClienteConectar(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;
        // ConnectedClientsIds inclui o host → count > MaxJogadoresCoop = excedente.
        if (nm.ConnectedClientsIds.Count > MaxJogadoresCoop)
            nm.DisconnectClient(clientId, "Sala cheia — o co-op está limitado a 2 jogadores por enquanto.");
    }

    public static async Task JoinAsync(string joinCode)
    {
        JoinAllocation alloc = await RelayService.Instance.JoinAllocationAsync(joinCode);
        Transport.SetRelayServerData(new RelayServerData(alloc, "dtls"));
        ConfigurarTransport();
        NetworkManager.Singleton.StartClient();
    }
}
