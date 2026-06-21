using Unity.Netcode;
using UnityEngine;

// Fachada de pausa dual-mode. Em single-player (sem rede) faz Time.timeScale
// direto (comportamento de hoje). Em co-op roteia pro host via CoopPauseManager,
// que sincroniza a pausa pra todos via NetworkVariable.
public static class CoopPause
{
    static bool EmRede
    {
        get
        {
            var nm = NetworkManager.Singleton;
            return nm != null && nm.IsListening && CoopPauseManager.Instance != null;
        }
    }

    // true enquanto ESTE cliente está com uma escolha aberta (intenção local). Usado
    // pelo overlay pra mostrar "aguardando o outro" só pra quem já terminou.
    public static bool EuEscolhendo { get; private set; }

    // true quando ESTE player tem uma escolha PENDENTE (subiu de nível, painel ainda vai
    // abrir). Suprime o overlay de "aguardando" prematuro entre o level-up e o painel abrir.
    public static bool EscolhaPendente { get; private set; }
    public static void MarcarEscolhaPendente() => EscolhaPendente = true;

    // Escolha de gameplay (skill/carta/evolução/elemento): segura a pausa enquanto
    // este player estiver escolhendo; libera quando fecha. Pausa fica ativa enquanto
    // QUALQUER player estiver segurando — ou seja, só roda quando todos terminaram.
    public static void ReterEscolha()
    {
        EuEscolhendo = true;
        if (EmRede) CoopPauseManager.Instance.ReterEscolhaServerRpc();
        else Time.timeScale = 0f;
    }

    public static void LiberarEscolha()
    {
        EuEscolhendo = false;
        EscolhaPendente = false;
        if (EmRede) CoopPauseManager.Instance.LiberarEscolhaServerRpc();
        else Time.timeScale = 1f;
    }

    // Menu de pausa do grupo: qualquer um abre (congela todos), qualquer um fecha.
    public static void AbrirMenu()
    {
        if (EmRede) CoopPauseManager.Instance.AbrirMenuServerRpc();
        else Time.timeScale = 0f;
    }

    public static void FecharMenu()
    {
        if (EmRede) CoopPauseManager.Instance.FecharMenuServerRpc();
        else Time.timeScale = 1f;
    }
}
