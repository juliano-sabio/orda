using Unity.Netcode;
using UnityEngine;

// Vive SÓ na variante NetworkPlayer. Isola o NGO do player_stats.
// - sincroniza o índice de personagem (dono escreve; todos aplicam)
// - registra PlayerStats.Local no dono
// - implementa INetOwnership pro gating dual-mode
[RequireComponent(typeof(PlayerStats))]
public class PlayerNet : NetworkBehaviour, INetOwnership
{
    readonly NetworkVariable<int> charIndex = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    PlayerStats stats;

    public bool IsNetworked => IsSpawned;
    public bool IsLocalOwner => IsOwner;

    void Awake() { stats = GetComponent<PlayerStats>(); }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            PlayerStats.SetLocal(stats);
            charIndex.Value = PlayerPrefs.GetInt("SelectedCharacter", 0);

            // Separa os players por cliente pra não nascerem sobrepostos.
            // Owner-authoritative: setar a posição aqui replica pros demais.
            float x = ((int)OwnerClientId - 0.5f) * 4f; // host(0) -> -2, client(1) -> +2
            transform.position = new Vector3(x, 0f, transform.position.z);
        }
        else
        {
            // Cópia remota = fantoche controlado pelo NetworkTransform.
            // Só um AudioListener deve existir (o do dono local).
            var al = GetComponentInChildren<AudioListener>(true);
            if (al != null) al.enabled = false;

            // Rigidbody2D Kinematic: impede a física de brigar com o NetworkTransform
            // (sem isso, o corpo retém a última velocidade e arrasta/jittera).
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Aplica o personagem correto em TODAS as cópias (dono e remotas).
        stats.ApplyCharacterData(charIndex.Value);

        // Reaplica se o valor chegar/mudar depois (ordem de sincronização).
        charIndex.OnValueChanged += AoMudarPersonagem;
    }

    public override void OnNetworkDespawn()
    {
        charIndex.OnValueChanged -= AoMudarPersonagem;
        PlayerStats.ClearLocal(stats);
    }

    void AoMudarPersonagem(int anterior, int novo)
    {
        stats.ApplyCharacterData(novo);
    }
}
