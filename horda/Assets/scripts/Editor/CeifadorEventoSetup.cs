using UnityEngine;
using UnityEditor;

public class CeifadorEventoSetup : AssetPostprocessor
{
    const string ASE_PATH    = "Assets/prefebs/eventos/ceifadorevento/ceifadorevento.ase";
    const string PREFAB_PATH = "Assets/prefebs/eventos/ceifadorevento/CeifadorEvento.prefab";

    static void OnPostprocessAllAssets(
        string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        foreach (var path in imported)
        {
            if (path == ASE_PATH)
            {
                AssignControllerToPrefab();
                return;
            }
        }
    }

    [MenuItem("Tools/Setup/Configurar CeifadorEvento")]
    static void AssignControllerToPrefab()
    {
        RuntimeAnimatorController controller = null;

        // Procura controller como sub-asset do .ase
        var subassets = AssetDatabase.LoadAllAssetsAtPath(ASE_PATH);
        foreach (var asset in subassets)
        {
            if (asset is RuntimeAnimatorController rac)
            {
                controller = rac;
                break;
            }
        }

        // Fallback: busca pelo nome na pasta
        if (controller == null)
        {
            var guids = AssetDatabase.FindAssets("t:AnimatorController",
                new[] { "Assets/prefebs/eventos/ceifadorevento" });
            foreach (var g in guids)
            {
                controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    AssetDatabase.GUIDToAssetPath(g));
                if (controller != null) break;
            }
        }

        if (controller == null)
        {
            Debug.LogWarning("[CeifadorEventoSetup] Controller não encontrado. Execute novamente após importar o .ase.");
            return;
        }

        using (var scope = new PrefabUtility.EditPrefabContentsScope(PREFAB_PATH))
        {
            var root = scope.prefabContentsRoot;
            var ce   = root.GetComponent<CeifadorEvento>();
            var anim = root.GetComponent<Animator>();

            if (ce != null)
            {
                ce.controllerCeifador = controller;
                EditorUtility.SetDirty(ce);
            }
            if (anim != null)
            {
                anim.runtimeAnimatorController = controller;
                EditorUtility.SetDirty(anim);
            }
        }

        Debug.Log($"[CeifadorEventoSetup] Controller '{controller.name}' atribuído ao prefab CeifadorEvento com sucesso!");
    }
}
