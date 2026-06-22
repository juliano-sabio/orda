using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// [debug temp] Botão IMGUI pra invocar QUALQUER boss no co-op. Lista os prefabs de boss
// registrados na rede (NetworkConfig.Prefabs), não só os do bossEvents da fase — assim dá
// pra testar Caveira/Maga/Princesa/SlimeElite em qualquer cena. Host spawna via NetSpawn.
// NÃO é produção — remover junto com o resto do debug.
public class DebugChamarBoss : MonoBehaviour
{
    bool imortal;
    List<GameObject> bosses;

    void Update()
    {
        // [debug temp] mantém o player local (host) invulnerável enquanto o toggle estiver ligado.
        if (imortal && PlayerStats.Local != null)
            PlayerStats.Local.invulneravel = true;
    }

    void CarregarBosses()
    {
        bosses = new List<GameObject>();
        var nm = NetworkManager.Singleton;
        if (nm == null || nm.NetworkConfig == null || nm.NetworkConfig.Prefabs == null) return;

        var nomes = new HashSet<string>();
        foreach (var np in nm.NetworkConfig.Prefabs.Prefabs)
        {
            var go = np != null ? np.Prefab : null;
            if (go == null) continue;
            // só os prefabs de boss (nome começa com "boss"; exclui "Projetil...") que sejam inimigos.
            if (!go.name.ToLowerInvariant().StartsWith("boss")) continue;
            if (go.GetComponent<InimigoController>() == null) continue;
            if (nomes.Add(go.name)) bosses.Add(go);
        }
    }

    void OnGUI()
    {
        var nm = NetworkManager.Singleton;
        bool host = nm == null || !nm.IsListening || nm.IsServer;
        if (bosses == null && nm != null && nm.IsListening) CarregarBosses();

        int linhas = bosses != null ? bosses.Count : 1;
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
        if (bosses == null || bosses.Count == 0)
        {
            GUI.Label(new Rect(x + 8f, y, w - 16f, 24f), "sem bosses registrados");
            return;
        }

        foreach (var bp in bosses)
        {
            if (GUI.Button(new Rect(x + 6f, y, w - 12f, 24f), "Chamar: " + bp.name))
                NetSpawn.Spawnar(bp, PosDeSpawn());
            y += 28f;
        }
    }

    Vector3 PosDeSpawn()
    {
        Vector3 p = PlayerStats.Local != null ? PlayerStats.Local.transform.position : Vector3.zero;
        return p + (Vector3)(Random.insideUnitCircle.normalized * 6f);
    }
}
