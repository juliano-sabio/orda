using UnityEngine;
using UnityEditor;

public class SlimeColoridaSetup : AssetPostprocessor
{
    const string ASEPRITE_PATH  = "Assets/prefebs/eventos/New Folder/evento slime colorida_01.aseprite";
    const string PREFAB_PATH    = "Assets/prefebs/eventos/SlimeColorida.prefab";

    static void OnPostprocessAllAssets(
        string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        foreach (var path in imported)
        {
            if (path == ASEPRITE_PATH)
            {
                AssignControllerToPrefab();
                return;
            }
        }
    }

    [MenuItem("Tools/Setup/Configurar SlimeColorida")]
    static void AssignControllerToPrefab()
    {
        // Carrega todos os sub-assets do aseprite
        var subassets = AssetDatabase.LoadAllAssetsAtPath(ASEPRITE_PATH);
        RuntimeAnimatorController controller = null;

        foreach (var asset in subassets)
        {
            if (asset is RuntimeAnimatorController rac)
            {
                controller = rac;
                break;
            }
        }

        if (controller == null)
        {
            // Fallback: procura controller pelo nome na mesma pasta
            var guids = AssetDatabase.FindAssets("t:AnimatorController", new[] { "Assets/prefebs/eventos" });
            foreach (var g in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (p.ToLower().Contains("colorida"))
                {
                    controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(p);
                    break;
                }
            }
        }

        if (controller == null)
        {
            Debug.LogWarning("[SlimeColoridaSetup] Controller da slime colorida não encontrado ainda. Rode novamente após importar o aseprite.");
            return;
        }

        // Abre o prefab, atualiza o campo e salva
        using (var scope = new PrefabUtility.EditPrefabContentsScope(PREFAB_PATH))
        {
            var root = scope.prefabContentsRoot;
            var sc   = root.GetComponent<SlimeColorida>();
            var anim = root.GetComponent<Animator>();

            if (sc != null)
            {
                sc.controllerColorida = controller;
                EditorUtility.SetDirty(sc);
            }
            if (anim != null)
            {
                anim.runtimeAnimatorController = controller;
                EditorUtility.SetDirty(anim);
            }
        }

        Debug.Log($"[SlimeColoridaSetup] Controller '{controller.name}' atribuído ao prefab SlimeColorida com sucesso!");
    }
}
