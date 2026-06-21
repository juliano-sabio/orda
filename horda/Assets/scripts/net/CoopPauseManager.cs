using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Vive na fase co-op (spawnado pelo host). Centraliza a pausa do grupo: qualquer
// escolha (skill/carta/evolução/elemento) ou o menu de pausa congela a horda + timer
// pra TODOS via Time.timeScale, dirigido por NetworkVariable host-autoritativo.
public class CoopPauseManager : NetworkBehaviour
{
    public static CoopPauseManager Instance { get; private set; }

    const ulong SemDono = ulong.MaxValue;

    // Estado só-no-host.
    readonly HashSet<ulong> retentoresEscolha = new HashSet<ulong>();
    bool menuAberto;

    // Sincronizado pra todos.
    public readonly NetworkVariable<bool> pausado = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // clientId de quem abriu o menu de pausa (SemDono = nenhum menu aberto).
    public readonly NetworkVariable<ulong> donoMenu = new NetworkVariable<ulong>(
        SemDono, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        Instance = this;
        pausado.OnValueChanged += AoMudarPausa;
        AplicarTimeScale(pausado.Value); // cliente que entra no meio respeita a pausa vigente
        if (IsServer && NetworkManager != null)
            NetworkManager.OnClientDisconnectCallback += AoDesconectar;
    }

    public override void OnNetworkDespawn()
    {
        pausado.OnValueChanged -= AoMudarPausa;
        if (IsServer && NetworkManager != null)
            NetworkManager.OnClientDisconnectCallback -= AoDesconectar;
        if (Instance == this) Instance = null;
        Time.timeScale = 1f; // não deixar a próxima cena congelada
    }

    void AoMudarPausa(bool _, bool novo) => AplicarTimeScale(novo);
    void AplicarTimeScale(bool p) => Time.timeScale = p ? 0f : 1f;

    [Rpc(SendTo.Server)]
    public void ReterEscolhaServerRpc(RpcParams rpc = default)
    {
        retentoresEscolha.Add(rpc.Receive.SenderClientId);
        Recomputar();
    }

    [Rpc(SendTo.Server)]
    public void LiberarEscolhaServerRpc(RpcParams rpc = default)
    {
        retentoresEscolha.Remove(rpc.Receive.SenderClientId);
        Recomputar();
    }

    [Rpc(SendTo.Server)]
    public void AbrirMenuServerRpc(RpcParams rpc = default)
    {
        menuAberto = true;
        donoMenu.Value = rpc.Receive.SenderClientId;
        Recomputar();
    }

    // Sem RpcParams de propósito: qualquer player pode fechar a pausa do menu (não só quem abriu).
    [Rpc(SendTo.Server)]
    public void FecharMenuServerRpc()
    {
        menuAberto = false;
        donoMenu.Value = SemDono;
        Recomputar();
    }

    void AoDesconectar(ulong id)
    {
        retentoresEscolha.Remove(id);
        if (menuAberto && donoMenu.Value == id) { menuAberto = false; donoMenu.Value = SemDono; }
        Recomputar();
    }

    void Recomputar()
    {
        pausado.Value = retentoresEscolha.Count > 0 || menuAberto;
    }

    GUIStyle estiloOverlay; // cacheado: OnGUI roda várias vezes por frame (sem alocar por frame)

    // Overlay IMGUI durante a pausa. Dois casos:
    //  - menu de pausa: pro player que NÃO abriu, "Fulano pausou o jogo".
    //  - pausa por escolha: pra quem JÁ terminou (não está escolhendo), "aguardando o outro".
    void OnGUI()
    {
        if (!pausado.Value) return;

        string texto;
        ulong dono = donoMenu.Value;
        if (dono != SemDono)
        {
            // Pausa por menu: só mostra pra quem não abriu.
            if (NetworkManager != null && dono == NetworkManager.LocalClientId) return;
            texto = NomeDe(dono) + " pausou o jogo";
        }
        else
        {
            // Pausa por escolha: quem ainda está escolhendo (ou tem escolha pendente, i.e.
            // subiu de nível e o painel ainda vai abrir) vê a própria UI; só mostra o aviso
            // pra quem já fechou a escolha e está de fato esperando o outro.
            if (CoopPause.EuEscolhendo || CoopPause.EscolhaPendente) return;
            texto = "Aguardando o outro jogador escolher...";
        }

        if (estiloOverlay == null)
            estiloOverlay = new GUIStyle(GUI.skin.box) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
        GUI.Box(new Rect(Screen.width / 2f - 220f, 40f, 440f, 50f), texto, estiloOverlay);
    }

    static string NomeDe(ulong clientId)
    {
        for (int i = 0; i < PlayerStats.All.Count; i++)
        {
            var pn = PlayerStats.All[i] != null ? PlayerStats.All[i].GetComponent<PlayerNet>() : null;
            if (pn != null && pn.OwnerClientId == clientId) return pn.Nome;
        }
        return "Jogador " + (clientId + 1);
    }
}
