using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class EspiritoEventoSetup
{
    const string PREFAB     = "Assets/prefebs/eventos/espirito evento/espirito evento.prefab";
    const string CONTROLLER = "Assets/prefebs/eventos/espirito evento/espirito evento.controller";
    const string ASE        = "Assets/prefebs/eventos/espirito evento/espirito evento 02.ase";

    [MenuItem("Tools/Evento/Configurar Espirito Evento")]
    public static void Configurar()
    {
        AnimationClip clip = null;
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(ASE))
            if (a is AnimationClip c && !c.name.StartsWith("__")) { clip = c; break; }

        if (clip == null) { Debug.LogError("Clip não encontrado: " + ASE); return; }

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER) != null)
            AssetDatabase.DeleteAsset(CONTROLLER);

        var ctrl  = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER);
        var state = ctrl.layers[0].stateMachine.AddState("Flutuando");
        state.motion = clip;
        ctrl.layers[0].stateMachine.defaultState = state;
        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();

        // Cria prefab vazio se não existir
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB) == null)
        {
            var temp = new GameObject("espirito evento");
            PrefabUtility.SaveAsPrefabAsset(temp, PREFAB);
            Object.DestroyImmediate(temp);
        }

        using (var scope = new PrefabUtility.EditPrefabContentsScope(PREFAB))
        {
            var root = scope.prefabContentsRoot;

            var sr = root.GetComponent<SpriteRenderer>();
            if (sr == null) sr = root.AddComponent<SpriteRenderer>();
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(ASE))
                if (a is Sprite spr) { sr.sprite = spr; break; }
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 5;

            var anim = root.GetComponent<Animator>();
            if (anim == null) anim = root.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;

            var col = root.GetComponent<CircleCollider2D>();
            if (col == null) col = root.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.3f;

            var tipo = System.Type.GetType("EspiritoEvento");
            if (tipo != null && root.GetComponent(tipo) == null)
                root.AddComponent(tipo);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("✅ EspiritoEvento configurado: " + PREFAB);
    }
}
