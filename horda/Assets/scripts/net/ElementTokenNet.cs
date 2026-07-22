using Unity.Netcode;
using UnityEngine;

// Co-op: sincroniza QUAL elemento este token (dropado pelo boss) é, pra o ícone aparecer
// igual nos dois lados. A coleta em si é host-autoritativa (ElementDropToken). Em SP este
// componente fica inerte (não é spawnado via NGO) — aí o ElementRegistry seta o elemento direto.
public class ElementTokenNet : NetworkBehaviour
{
    readonly NetworkVariable<int> elementoNet = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public void DefinirElemento(int e) { if (IsServer) elementoNet.Value = e; }

    bool jaColetado; // guard server-side (evita dupla coleta se os dois encostarem ao mesmo tempo)

    // Pedido de coleta vindo da máquina do player que encostou. O host concede ao dono certo.
    public void SolicitarColeta()
    {
        if (IsServer) ConcederColeta(NetworkManager.LocalClientId);
        else SolicitarColetaServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void SolicitarColetaServerRpc(RpcParams p = default)
    {
        ConcederColeta(p.Receive.SenderClientId);
    }

    void ConcederColeta(ulong clientId)
    {
        if (!IsServer || jaColetado) return;
        jaColetado = true;
        int elem = elementoNet.Value >= 0
            ? elementoNet.Value
            : (int)(GetComponent<ElementDropToken>()?.elementType ?? ElementType.None);
        // abre a infusão SÓ pro player desse cliente (dono que pegou)
        foreach (var pn in FindObjectsByType<PlayerNet>(FindObjectsSortMode.None))
            if (pn.OwnerClientId == clientId) { pn.AbrirInfusaoOwnerRpc(elem); break; }
        if (NetworkObject != null && NetworkObject.IsSpawned) NetworkObject.Despawn(); // some em todos
    }

    public override void OnNetworkSpawn()
    {
        elementoNet.OnValueChanged += AoMudar;
        if (elementoNet.Value >= 0) Aplicar(elementoNet.Value);
    }

    public override void OnNetworkDespawn()
    {
        elementoNet.OnValueChanged -= AoMudar;
    }

    void AoMudar(int _, int v) => Aplicar(v);

    void Aplicar(int v)
    {
        if (v < 0) return;
        GetComponent<ElementDropToken>()?.Configurar((ElementType)v);
    }
}
