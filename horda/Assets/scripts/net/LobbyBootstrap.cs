using Unity.Netcode;
using UnityEngine;

// Na cena de lobby: quando o servidor (host) inicia, spawna o LobbyManager
// (NetworkObject) pra coordenar fase/início.
public class LobbyBootstrap : MonoBehaviour
{
    public GameObject lobbyManagerPrefab;

    void OnEnable()
    {
        var nm = NetworkManager.Singleton;
        if (nm != null) nm.OnServerStarted += SpawnLobbyManager;
    }

    void OnDisable()
    {
        var nm = NetworkManager.Singleton;
        if (nm != null) nm.OnServerStarted -= SpawnLobbyManager;
    }

    void SpawnLobbyManager()
    {
        if (lobbyManagerPrefab == null) return;
        if (LobbyManager.Instance != null) return;
        var go = Instantiate(lobbyManagerPrefab);
        var no = go.GetComponent<NetworkObject>();
        if (no != null) no.Spawn();
    }
}
