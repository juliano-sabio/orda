using Unity.Netcode;
using UnityEngine;

// Vive no lobby (spawnado pelo host). Sincroniza a fase escolhida e dispara
// o scene-load NGO quando o host inicia.
public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance { get; private set; }

    // Nomes das fases co-op disponíveis (ordem = índice).
    public static readonly string[] Fases = { "primeira_fase_mp" };

    public readonly NetworkVariable<int> faseEscolhida = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn() { Instance = this; }
    public override void OnNetworkDespawn() { if (Instance == this) Instance = null; }

    [Rpc(SendTo.Server)]
    public void EscolherFaseServerRpc(int idx)
    {
        if (idx >= 0 && idx < Fases.Length) faseEscolhida.Value = idx;
    }

    [Rpc(SendTo.Server)]
    public void IniciarServerRpc()
    {
        if (!TodosProntos()) return;
        LobbyState.EmLobby = false; // host sai do estado de lobby
        string fase = Fases[Mathf.Clamp(faseEscolhida.Value, 0, Fases.Length - 1)];
        NetworkManager.SceneManager.LoadScene(fase, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    public bool TodosProntos()
    {
        if (PlayerStats.All.Count == 0) return false;
        for (int i = 0; i < PlayerStats.All.Count; i++)
        {
            var pn = PlayerStats.All[i] != null ? PlayerStats.All[i].GetComponent<PlayerNet>() : null;
            if (pn == null || !pn.Pronto) return false;
        }
        return true;
    }
}
