using Unity.Netcode;
using UnityEngine;

// Na fase co-op (carregada pelo lobby): desliga o estado de lobby, spawna os managers
// host (pausa coordenada + progressão compartilhada) e reposiciona os players.
public class FaseCoopBootstrap : MonoBehaviour
{
    public GameObject coopPauseManagerPrefab;
    public GameObject coopProgressaoPrefab;

    void Start()
    {
        LobbyState.EmLobby = false;

        // [debug temp] botão de invocar boss pra testar bosses em co-op (em todos os clientes; só o host spawna).
        if (GetComponent<DebugChamarBoss>() == null) gameObject.AddComponent<DebugChamarBoss>();

        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return; // só o host

        // Spawna o coordenador de pausa do grupo (uma vez).
        if (coopPauseManagerPrefab != null && CoopPauseManager.Instance == null)
        {
            var go = Instantiate(coopPauseManagerPrefab);
            var no = go.GetComponent<NetworkObject>();
            if (no != null) no.Spawn();
        }

        // Spawna a progressão compartilhada (XP/nível do grupo).
        if (coopProgressaoPrefab != null && CoopProgressao.Instance == null)
        {
            var go = Instantiate(coopProgressaoPrefab);
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
