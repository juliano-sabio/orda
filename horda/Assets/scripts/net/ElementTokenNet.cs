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
