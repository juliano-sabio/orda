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

    // Overlay simples (IMGUI, como o lobby) pro player que NÃO abriu o menu.
    void OnGUI()
    {
        if (!pausado.Value) return;
        ulong dono = donoMenu.Value;
        if (dono == SemDono) return;                                   // pausa por escolha: sem overlay
        if (NetworkManager != null && dono == NetworkManager.LocalClientId) return; // eu mesmo abri
        var style = new GUIStyle(GUI.skin.box) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
        GUI.Box(new Rect(Screen.width / 2f - 200f, 40f, 400f, 50f), NomeDe(dono) + " pausou o jogo", style);
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
