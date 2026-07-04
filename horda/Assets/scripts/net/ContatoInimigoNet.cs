using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Co-op (só no CLIENTE dono do player): detecta o contato do PRÓPRIO player com os inimigos
// usando as posições que ESTE cliente vê (inimigos são fantoches sincronizados, o player é
// local e preciso) → o hitbox bate com a tela. Pede o dano de contato ao host, que aplica o
// valor real (escalado) — autoritativo. Sem isto, o host detectava o contato pelo fantoche
// defasado do P2 e ele tomava dano "sem encostar".
//
// No HOST o player local (P1) NÃO usa isto: o DanoInimigo do inimigo já detecta o contato de
// P1 com precisão (P1 é local, sem lag). Só o(s) cliente(s) precisam deste caminho.
[RequireComponent(typeof(PlayerStats))]
public class ContatoInimigoNet : MonoBehaviour
{
    const int   LAYER_ENEMY = 7;   // layer "Enemy"
    const float INTERVALO   = 0.5f; // mesmo ritmo do dano de contato do DanoInimigo
    const float POLL        = 0.1f;

    PlayerStats stats;
    PlayerNet   net;
    Collider2D  col;
    ContactFilter2D filtro;
    readonly Collider2D[] buf = new Collider2D[16];
    readonly Dictionary<EnemyNet, float> cooldown = new Dictionary<EnemyNet, float>();
    float prox;

    void Start()
    {
        stats = GetComponent<PlayerStats>();
        net   = GetComponent<PlayerNet>();
        col   = GetComponent<Collider2D>();
        filtro = new ContactFilter2D { useTriggers = true };
        filtro.SetLayerMask(1 << LAYER_ENEMY);
    }

    void Update()
    {
        if (stats == null || net == null || col == null) return;
        if (!net.IsSpawned || !net.IsOwner) return;   // só o dono
        if (stats.EstaCaido) return;                  // caído não toma dano
        if (Time.time < prox) return;
        prox = Time.time + POLL;

        int n = Physics2D.OverlapCollider(col, filtro, buf); // inimigos encostando NO player (visão do dono)
        for (int i = 0; i < n; i++)
        {
            var c = buf[i];
            if (c == null) continue;
            var en = c.GetComponent<EnemyNet>() ?? c.GetComponentInParent<EnemyNet>();
            if (en == null || !en.IsSpawned) continue;
            if (cooldown.TryGetValue(en, out var t) && Time.time < t) continue;
            cooldown[en] = Time.time + INTERVALO;
            en.ContatoDanoServerRpc(net.NetworkObjectId); // host aplica o dano real
        }
    }
}
