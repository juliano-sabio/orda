using Unity.Netcode;
using UnityEngine;

// Em inimigos/bosses. No CLIENTE, o inimigo é um fantoche movido pelo
// NetworkTransform (server authority): Rigidbody2D Kinematic e scripts de
// gameplay desligados (a IA roda só no host). No HOST, não mexe em nada.
public class EnemyNet : NetworkBehaviour
{
    // Escala do inimigo sincronizada do host. O NetworkTransform dos inimigos não sincroniza
    // scale, e a escala é setada no Awake (controlei_inimigo: dadosInimigo.tamanho) — que no
    // cliente pode divergir (ex.: SlimeColorida tem dadosInimigo==null → fica no default do
    // prefab). Sem isto o fantoche aparecia com "tamanho errado".
    readonly NetworkVariable<float> escalaNet = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Abs: movi_inimigo inverte o SINAL de localScale.x pra virar o inimigo (facing).
            // Sem Abs, se o host estivesse virado p/ esquerda no spawn, o fantoche recebia
            // escala negativa → espelhado/distorcido (parecia "tamanho errado").
            escalaNet.Value = Mathf.Abs(transform.localScale.x);
            return;
        }

        // cliente: aplica a escala REAL do host (corrige tamanho do fantoche)
        escalaNet.OnValueChanged += AoMudarEscala;
        AplicarEscala(escalaNet.Value);

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;

        // Desliga MonoBehaviours de gameplay no cliente. Visual (SpriteRenderer,
        // Animator) não são MonoBehaviour; NetworkBehaviours são preservados.
        foreach (var c in GetComponents<MonoBehaviour>())
        {
            if (c == this) continue;
            if (c is NetworkBehaviour) continue;
            c.enabled = false;
        }

        // Co-op: inimigos que criam efeitos visuais por script (luzes/partículas) ficariam
        // "pelados" no cliente (o script foi desligado acima). Deixa cada um montar só o visual.
        var cosm = GetComponent<IEnemyCosmetic>();
        if (cosm != null) cosm.SetupVisualCosmetico();
    }

    void AoMudarEscala(float _, float v) => AplicarEscala(v);
    void AplicarEscala(float s)
    {
        if (s > 0f) transform.localScale = new Vector3(s, s, transform.localScale.z);
    }

    // Qualquer cliente pode requisitar dano a qualquer inimigo (co-op de amigos).
    [ServerRpc(RequireOwnership = false)]
    public void ReceberDanoServerRpc(float dano, bool isCrit, bool mostrarNumero = true)
    {
        var ic = GetComponent<InimigoController>();
        if (ic != null) ic.ReceberDano(dano, isCrit, mostrarNumero); // roda no host -> aplica
    }

    // Co-op: o host mostra o número de dano (pós-mitigação) também nos clientes. Sem isto,
    // o cliente que bate via ServerRpc nunca vê o próprio número (o controller está
    // desligado no cliente, e o dano é processado só no host).
    public void ReplicarNumeroDano(float dano, bool isCrit)
    {
        if (IsServer && IsSpawned) MostrarNumeroDanoClientRpc(dano, isCrit);
    }

    [ClientRpc]
    void MostrarNumeroDanoClientRpc(float dano, bool isCrit)
    {
        if (IsServer) return; // o host já mostrou localmente
        if (DamageNumberManager.Instance != null)
            DamageNumberManager.Instance.ShowDamage(transform, dano, isCrit);
    }
}

// Inimigos que criam efeitos visuais por script (luzes, partículas, glows) implementam isto.
// No cliente co-op o EnemyNet desliga o gameplay e chama este método pra montar SÓ os visuais.
public interface IEnemyCosmetic { void SetupVisualCosmetico(); }
