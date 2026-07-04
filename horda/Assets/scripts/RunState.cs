using UnityEngine;
using UnityEngine.SceneManagement;

// Estado central da RUN (fase). A fase só "liga" (spawner, eventos, timer e inimigos agem)
// quando os players estão prontos, e "desliga" COMPLETAMENTE quando a run acaba (todos caem
// ou alguém sai, em co-op). Evita os bugs de "começou antes da hora" e de estado vazando
// entre runs. Os sistemas consultam RunState.Ativo (polling — sem eventos, sem leak).
public static class RunState
{
    public static bool Ativo { get; private set; }

    // Toda cena começa DESLIGADA; o RunStarter liga quando tudo está pronto.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void _Hook() => SceneManager.sceneLoaded += (_, __) => Ativo = false;

    public static void Ligar()
    {
        if (Ativo) return;
        Ativo = true;
    }

    public static void Desligar()
    {
        if (!Ativo) return;
        Ativo = false;
        CongelarInimigos();                             // inimigos que restaram param na hora
        GerenciadorEventos.Instance?.EncerrarPorFimDeRun(); // fecha o card de evento (não fica "rodando")
    }

    // Congela todos os inimigos vivos (para movimento/IA) — deixa a tela de fim de run limpa,
    // sem horda se mexendo durante o game over / transição pro lobby.
    static void CongelarInimigos()
    {
        var todos = Object.FindObjectsByType<InimigoController>(FindObjectsSortMode.None);
        foreach (var ic in todos)
        {
            if (ic == null) continue;
            var rb = ic.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
            foreach (var mb in ic.GetComponents<MonoBehaviour>())
            {
                if (mb == null || mb is InimigoController) continue;
                if (mb is Unity.Netcode.NetworkBehaviour) continue; // preserva a rede (EnemyNet)
                mb.enabled = false;
            }
        }
    }
}
