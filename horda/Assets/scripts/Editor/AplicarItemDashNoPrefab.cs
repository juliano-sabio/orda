#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class AplicarItemDashNoPrefab
{
    const string ASE_PATH    = "Assets/prefebs/dash/itemdash.ase";
    const string PREFAB_PATH = "Assets/prefebs/dash/dash.prefab";

    [MenuItem("Tools/UI Manager/Aplicar ItemDash no Prefab")]
    public static void Aplicar()
    {
        // Carrega todos os sub-assets do .ase
        Sprite sprite = null;
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(ASE_PATH))
        {
            if (asset is Sprite s) { sprite = s; break; }
        }

        if (sprite == null)
        {
            Debug.LogError("Sprite nao encontrado em: " + ASE_PATH +
                           "\nVerifique se o arquivo foi importado corretamente pelo Aseprite Importer.");
            return;
        }

        using (var scope = new PrefabUtility.EditPrefabContentsScope(PREFAB_PATH))
        {
            var root = scope.prefabContentsRoot;
            var sr   = root.GetComponent<SpriteRenderer>();
            if (sr == null) sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("ItemDash aplicado no prefab: " + sprite.name);
    }
}
#endif
