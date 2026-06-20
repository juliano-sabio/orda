using Unity.Netcode;
using UnityEngine;

// Na fase co-op (carregada pelo lobby): desliga o estado de lobby, spawna o
// CoopPauseManager (host) e reposiciona os players spawnados em pontos separados.
public class FaseCoopBootstrap : MonoBehaviour
{
    public GameObject coopPauseManagerPrefab;

    void Start()
    {
        LobbyState.EmLobby = false;
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return; // só o host

        // Spawna o coordenador de pausa do grupo (uma vez).
        if (coopPauseManagerPrefab != null && CoopPauseManager.Instance == null)
        {
            var go = Instantiate(coopPauseManagerPrefab);
            var no = go.GetComponent<NetworkObject>();
            if (no != null) no.Spawn();
        }

        // Reposiciona os players (NetworkTransform replica).
        int i = 0;
        foreach (var ps in PlayerStats.All)
        {
            if (ps == null) continue;
            ps.transform.position = new Vector3((i - 0.5f) * 4f, 0f, ps.transform.position.z);
            i++;
        }
    }
}
