using Unity.Netcode;
using UnityEngine;

// [debug temp] Botão IMGUI pra invocar bosses no co-op, lendo o TimerManager.bossEvents
// (mesmos prefabs configurados na fase). O host spawna via NetSpawn (replica pros clientes).
// NÃO é produção — remover junto com o resto do debug.
public class DebugChamarBoss : MonoBehaviour
{
    TimerManager tm;
    bool imortal;

    void Start() { tm = FindFirstObjectByType<TimerManager>(); }

    void Update()
    {
        // [debug temp] mantém o player local (host) invulnerável enquanto o toggle estiver ligado
        // (re-aplica todo frame porque outros sistemas resetam a flag).
        if (imortal && PlayerStats.Local != null)
            PlayerStats.Local.invulneravel = true;
    }

    void OnGUI()
    {
        var nm = NetworkManager.Singleton;
        bool host = nm == null || !nm.IsListening || nm.IsServer;

        int linhas = (tm != null && tm.bossEvents != null) ? tm.bossEvents.Length : 1;
        float w = 180f, x = Screen.width - w - 10f, y = 10f;
        GUI.Box(new Rect(x, y, w, 64f + linhas * 28f), "[debug] BOSSES");
        y += 30f;

        // toggle de imortalidade do player local (P1/host)
        if (GUI.Button(new Rect(x + 6f, y, w - 12f, 24f), imortal ? "P1 IMORTAL: ON" : "P1 imortal: off"))
        {
            imortal = !imortal;
            if (!imortal && PlayerStats.Local != null) PlayerStats.Local.invulneravel = false;
        }
        y += 30f;

        if (!host)
        {
            GUI.Label(new Rect(x + 8f, y, w - 16f, 24f), "só o host invoca");
            return;
        }
        if (tm == null || tm.bossEvents == null || tm.bossEvents.Length == 0)
        {
            GUI.Label(new Rect(x + 8f, y, w - 16f, 24f), "sem bosses na cena");
            return;
        }

        for (int i = 0; i < tm.bossEvents.Length; i++)
        {
            var be = tm.bossEvents[i];
            if (be == null || be.bossPrefab == null) continue;
            string nome = string.IsNullOrEmpty(be.bossName) ? be.bossPrefab.name : be.bossName;
            if (GUI.Button(new Rect(x + 6f, y, w - 12f, 24f), "Chamar: " + nome))
                NetSpawn.Spawnar(be.bossPrefab, PosDeSpawn());
            y += 28f;
        }
    }

    Vector3 PosDeSpawn()
    {
        Vector3 p = PlayerStats.Local != null ? PlayerStats.Local.transform.position : Vector3.zero;
        return p + (Vector3)(Random.insideUnitCircle.normalized * 6f);
    }
}
