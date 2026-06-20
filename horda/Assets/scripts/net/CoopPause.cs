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

    // Escolha de gameplay (skill/carta/evolução/elemento): segura a pausa enquanto
    // este player estiver escolhendo; libera quando fecha. Pausa fica ativa enquanto
    // QUALQUER player estiver segurando — ou seja, só roda quando todos terminaram.
    public static void ReterEscolha()
    {
        if (EmRede) CoopPauseManager.Instance.ReterEscolhaServerRpc();
        else Time.timeScale = 0f;
    }

    public static void LiberarEscolha()
    {
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
