using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PatchPrefabEspritoCura
{
    const string PREFAB_PATH = "Assets/prefebs/poção de cura/espirito de cura fx.prefab";

    static PatchPrefabEspritoCura()
    {
        EditorApplication.delayCall += Executar;
    }

    static void Executar()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        if (prefab == null) return;
        if (prefab.GetComponent<PulsarLuzItem>() != null) return;

        using (var scope = new PrefabUtility.EditPrefabContentsScope(PREFAB_PATH))
        {
            scope.prefabContentsRoot.AddComponent<PulsarLuzItem>();
        }

        Debug.Log("[EspritoCura] PulsarLuzItem adicionado ao prefab.");
    }
}
