using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// Liga a RUN (RunState.Ligar) só quando TUDO está pronto: os players presentes (co-op: todos
// os clientes conectados já com seu PlayerStats na fase) e os managers inicializados. Enquanto
// não estiver pronto, spawner/eventos/timer ficam parados (consultam RunState.Ativo).
// Auto-criado em cenas de fase; some quando liga.
public class RunStarter : MonoBehaviour
{
    float _graca;      // pequena folga pra managers terminarem o Start
    float _esperando;  // tempo total esperando por "pronto" (fail-safe anti-softlock)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Auto()
    {
        SceneManager.sceneLoaded += (s, _) => { if (EhFase(s.name)) Criar(); };
        var atual = SceneManager.GetActiveScene();
        if (EhFase(atual.name)) Criar();
    }

    static void Criar()
    {
        if (FindFirstObjectByType<RunStarter>() != null) return;
        new GameObject("RunStarter").AddComponent<RunStarter>();
    }

    static bool EhFase(string nome) =>
        !string.IsNullOrEmpty(nome) &&
        (nome.Contains("fase") || nome.Contains("sobrevivencia") || nome.Contains("Modo_sobrevivencia"));

    void Update()
    {
        if (RunState.Ativo) { Destroy(gameObject); return; }

        _esperando += Time.unscaledDeltaTime;

        // Fail-safe anti-softlock: se por algum motivo o "pronto" nunca casar (ex.: cliente
        // conectado que não spawnou player), liga assim que EXISTIR ao menos um player e já
        // se passaram uns segundos — melhor uma run rodando do que uma fase morta pra sempre.
        bool falhaSegura = _esperando > 10f && PlayerStats.All.Count > 0;

        if (!ProntoParaLigar() && !falhaSegura) { _graca = 0f; return; }

        // Segura ~0.25s depois que os players chegam pra os managers (UI/eventos) assentarem.
        _graca += Time.unscaledDeltaTime;
        if (_graca < 0.25f && !falhaSegura) return;

        RunState.Ligar();
        Destroy(gameObject);
    }

    bool ProntoParaLigar()
    {
        int presentes = PlayerStats.All.Count;
        if (presentes == 0) return false; // nenhum player na fase ainda

        // Co-op: espera TODOS os clientes conectados terem seu PlayerStats spawnado na fase.
        // ConnectedClientsIds só é válido/seguro no SERVIDOR (no cliente pode lançar exceção ou
        // vir incompleto). Como quem spawna horda/eventos/timer é o host, o gate estrito roda
        // nele; no cliente basta o player local existir (já coberto por presentes > 0).
        var nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening && nm.IsServer)
        {
            int esperados = nm.ConnectedClientsIds.Count;
            if (esperados > 0 && presentes < esperados) return false;
        }
        return true;
    }
}
