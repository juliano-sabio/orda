using UnityEditor;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

// Menu: Tools/MP/Add Net Components To Enemies
// Adiciona NetworkObject + NetworkTransform(Server) + EnemyNet aos prefabs de
// inimigo/boss nas pastas alvo, idempotente.
public static class AddNetToEnemies
{
    static readonly string[] Pastas =
    {
        "Assets/prefebs/inimigos",
        "Assets/prefebs/boss",
        "Assets/prefebs/skill_mob",
    };

    [MenuItem("Tools/MP/Add Net Components To Enemies")]
    public static void Run()
    {
        int alterados = 0;
        var guids = AssetDatabase.FindAssets("t:Prefab", Pastas);
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var go = PrefabUtility.LoadPrefabContents(path);
            bool mudou = false;

            if (go.GetComponent<NetworkObject>() == null) { go.AddComponent<NetworkObject>(); mudou = true; }

            var nt = go.GetComponent<NetworkTransform>();
            if (nt == null) { nt = go.AddComponent<NetworkTransform>(); mudou = true; }
            nt.AuthorityMode = NetworkTransform.AuthorityModes.Server;
            nt.SyncPositionZ = false; nt.SyncRotAngleX = false; nt.SyncRotAngleY = false;
            nt.SyncScaleX = false; nt.SyncScaleY = false; nt.SyncScaleZ = false;

            if (go.GetComponent<EnemyNet>() == null) { go.AddComponent<EnemyNet>(); mudou = true; }

            if (mudou) { PrefabUtility.SaveAsPrefabAsset(go, path); alterados++; }
            PrefabUtility.UnloadPrefabContents(go);
        }
        Debug.Log($"[MP] Net components adicionados em {alterados} prefabs de inimigo.");
    }
}
