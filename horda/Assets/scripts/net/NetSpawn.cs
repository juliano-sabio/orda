using Unity.Netcode;
using UnityEngine;

// Spawn dual-mode: single-player -> Instantiate; host em rede -> Instantiate+Spawn;
// cliente em rede -> nada (clientes não spawnam inimigos).
public static class NetSpawn
{
    public static bool EmRede
    {
        get { var nm = NetworkManager.Singleton; return nm != null && nm.IsListening; }
    }

    // true em single-player OU quando sou o host/server.
    public static bool PodeSpawnar
    {
        get { var nm = NetworkManager.Singleton; return nm == null || !nm.IsListening || nm.IsServer; }
    }

    public static GameObject Spawnar(GameObject prefab, Vector3 pos)
    {
        if (prefab == null) return null;
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening)
            return Object.Instantiate(prefab, pos, Quaternion.identity); // single-player
        if (!nm.IsServer) return null;                                   // cliente não spawna
        var go = Object.Instantiate(prefab, pos, Quaternion.identity);
        var no = go.GetComponent<NetworkObject>();
        if (no != null) no.Spawn();
        return go;
    }

    // Despawn dual-mode: host em rede -> NetworkObject.Despawn (destrói em todos);
    // cliente em rede -> nada; single-player -> Destroy.
    public static void Despawnar(GameObject go)
    {
        if (go == null) return;
        var nm = NetworkManager.Singleton;
        var no = go.GetComponent<NetworkObject>();
        if (nm != null && nm.IsListening && no != null && no.IsSpawned)
        {
            if (nm.IsServer) no.Despawn(); // destrói em todos os clientes
            return;                        // cliente não despawna
        }
        Object.Destroy(go); // single-player
    }
}
