using Unity.Netcode;
using UnityEngine;

// Co-op: quando o DONO dispara um projétil, transmite uma cópia COSMÉTICA pro cliente
// dos outros players. O fantasma é o MESMO prefab perseguindo o MESMO inimigo
// (NetworkObjectId, sincronizado) com os mesmos parâmetros → trajetória idêntica; só não
// causa dano (o dano é do projétil real do dono, já roteado pro host). Fantasma é
// Instantiate local (sem NetworkObject), auto-destrói no lifetime/hit.
public class SkillFxNet : NetworkBehaviour
{
    // Registro de prefabs replicáveis (igual em todos, pois é do prefab do player).
    public GameObject[] prefabsCosmeticos;

    // Registro GERAL de SkillData (todos), pra reconstruir o visual de qualquer skill no
    // cliente do colega por índice — sem depender de prefab limpo registrável. Atribuído
    // no prefab do player (igual em todos), então a referência fica carregada nos dois.
    public SkillData[] skillsRegistro;

    public static SkillFxNet Local { get; private set; }

    public override void OnNetworkSpawn() { if (IsOwner) Local = this; }
    public override void OnNetworkDespawn() { if (Local == this) Local = null; }

    public int IndiceDe(GameObject prefab)
    {
        if (prefab == null || prefabsCosmeticos == null) return -1;
        for (int i = 0; i < prefabsCosmeticos.Length; i++)
            if (prefabsCosmeticos[i] == prefab) return i;
        return -1;
    }

    // Chamado pelo dono ao lançar o projétil real (só faz algo em rede e se o prefab está no registro).
    public void Replicar(int idx, Vector3 pos, ulong alvoNetId, float speed, float lifeTime, int element, Color cor)
    {
        if (idx < 0) return;
        if (!IsSpawned) return;
        ReplicarServerRpc(idx, pos, alvoNetId, speed, lifeTime, element, cor.r, cor.g, cor.b, cor.a);
    }

    [Rpc(SendTo.Server)]
    void ReplicarServerRpc(int idx, Vector3 pos, ulong alvoNetId, float speed, float lifeTime, int element,
                           float r, float g, float b, float a)
    {
        SpawnFantasmaRpc(idx, pos, alvoNetId, speed, lifeTime, element, r, g, b, a);
    }

    // Roda em todos MENOS o dono (que já tem o projétil real).
    [Rpc(SendTo.NotOwner)]
    void SpawnFantasmaRpc(int idx, Vector3 pos, ulong alvoNetId, float speed, float lifeTime, int element,
                          float r, float g, float b, float a)
    {
        if (prefabsCosmeticos == null || idx < 0 || idx >= prefabsCosmeticos.Length) return;
        var prefab = prefabsCosmeticos[idx];
        if (prefab == null) return;

        var go = Instantiate(prefab, pos, Quaternion.identity);
        var pc = go.GetComponent<ProjectileController2D>();
        if (pc == null) { Destroy(go); return; }

        pc.cosmetico = true;
        var cor = new Color(r, g, b, a);
        if (cor.a > 0f) pc.infusedColorOverride = cor;
        Transform alvo = ObterAlvo(alvoNetId);
        pc.Initialize(alvo, 0f, speed, lifeTime, (PlayerStats.Element)element);
    }

    // ── Boomerang (controller próprio, retorna ao player) ─────────────────────────
    public int IndiceSkill(SkillData sd)
    {
        if (sd == null || skillsRegistro == null) return -1;
        for (int i = 0; i < skillsRegistro.Length; i++)
            if (skillsRegistro[i] == sd) return i;
        return -1;
    }

    public void ReplicarBoomerang(int skillIdx, Vector3 pos, Vector2 dir, ulong donoNetId,
                                  float throwSpeed, float returnSpeed, float maxRange, int maxTargets, int element)
    {
        if (skillIdx < 0 || !IsSpawned) return;
        ReplicarBoomerangServerRpc(skillIdx, pos, dir, donoNetId, throwSpeed, returnSpeed, maxRange, maxTargets, element);
    }

    [Rpc(SendTo.Server)]
    void ReplicarBoomerangServerRpc(int skillIdx, Vector3 pos, Vector2 dir, ulong donoNetId,
                                    float throwSpeed, float returnSpeed, float maxRange, int maxTargets, int element)
    {
        SpawnBoomerangRpc(skillIdx, pos, dir, donoNetId, throwSpeed, returnSpeed, maxRange, maxTargets, element);
    }

    [Rpc(SendTo.NotOwner)]
    void SpawnBoomerangRpc(int skillIdx, Vector3 pos, Vector2 dir, ulong donoNetId,
                           float throwSpeed, float returnSpeed, float maxRange, int maxTargets, int element)
    {
        if (skillsRegistro == null || skillIdx < 0 || skillIdx >= skillsRegistro.Length) return;
        var sd = skillsRegistro[skillIdx];
        if (sd == null || sd.projectilePrefab2D == null) return;

        var dono = ObterAlvo(donoNetId); // player que disparou (objeto sincronizado nos dois)
        if (dono == null) return;
        var donoStats = dono.GetComponent<PlayerStats>();
        if (donoStats == null) return;

        var go = Instantiate(sd.projectilePrefab2D, pos, Quaternion.identity);
        var bc = go.GetComponent<BoomerangController>();
        if (bc == null) bc = go.AddComponent<BoomerangController>();
        bc.cosmetico = true;
        bc.skillData = sd;
        bc.Initialize(dono, dir, throwSpeed, returnSpeed, maxRange, 1f, maxTargets,
                      (PlayerStats.Element)element, false, 0f, donoStats);
    }

    static Transform ObterAlvo(ulong netId)
    {
        if (netId == 0) return null;
        var nm = NetworkManager.Singleton;
        if (nm == null || nm.SpawnManager == null) return null;
        Unity.Netcode.NetworkObject no;
        if (nm.SpawnManager.SpawnedObjects.TryGetValue(netId, out no) && no != null)
            return no.transform;
        return null;
    }
}
