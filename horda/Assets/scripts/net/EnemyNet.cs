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
        // Host: a escala é publicada no Update (não aqui). InimigoController.InicializarComData
        // seta a escala no Start, que roda DEPOIS do OnNetworkSpawn → capturar aqui pegava a
        // escala default do prefab. No Update (Abs) o NGO só sincroniza quando o valor muda.
        if (IsServer) return;

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

    void Update()
    {
        // Host publica a escala REAL (Abs = magnitude; movi_inimigo inverte só o sinal p/ facing).
        // O NGO só envia quando o valor muda → barato; cobre a escala setada no Start do
        // InimigoController (que roda depois do OnNetworkSpawn) e qualquer mudança em runtime.
        if (IsServer) escalaNet.Value = Mathf.Abs(transform.localScale.x);
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

    // Co-op: o host replica o pop de morte (kill juice) nos clientes — o Morrer roda só no host.
    public void BroadcastMorteVFX(Vector2 pos, Color cor, float escala)
    {
        if (IsServer && IsSpawned) MorteVFXClientRpc(pos, cor.r, cor.g, cor.b, escala);
    }

    [Rpc(SendTo.NotServer)]
    void MorteVFXClientRpc(Vector2 pos, float r, float g, float b, float escala)
    {
        KillPopVFX.Tocar(pos, new Color(r, g, b), escala);
    }
}

// Inimigos que criam efeitos visuais por script (luzes, partículas, glows) implementam isto.
// No cliente co-op o EnemyNet desliga o gameplay e chama este método pra montar SÓ os visuais.
public interface IEnemyCosmetic { void SetupVisualCosmetico(); }
