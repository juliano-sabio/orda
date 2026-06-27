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

        // Co-op: a run nova começa LIMPA. Roda em cada máquina, pra todos os players (o dono
        // reseta as próprias skills/stats; nos fantoches limpa as cópias cosméticas). Sem isto,
        // as skills da run anterior carregavam (managers DontDestroyOnLoad + players persistem).
        foreach (var ps in PlayerStats.All)
        {
            var pn = ps != null ? ps.GetComponent<PlayerNet>() : null;
            if (pn != null) pn.ResetarParaNovaRun();
        }

        // [debug temp] botões de invocar boss / forçar evento pra testar em co-op (só o host dispara).
        if (GetComponent<DebugChamarBoss>() == null) gameObject.AddComponent<DebugChamarBoss>();
        if (GetComponent<DebugChamarEvento>() == null) gameObject.AddComponent<DebugChamarEvento>();

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
