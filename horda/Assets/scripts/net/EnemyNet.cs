using Unity.Netcode;
using UnityEngine;

// Em inimigos/bosses. No CLIENTE, o inimigo é um fantoche movido pelo
// NetworkTransform (server authority): Rigidbody2D Kinematic e scripts de
// gameplay desligados (a IA roda só no host). No HOST, não mexe em nada.
public class EnemyNet : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (IsServer) return; // host roda a lógica normalmente

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
    }
}
