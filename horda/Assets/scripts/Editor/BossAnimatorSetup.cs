using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class BossAnimatorSetup
{
    [MenuItem("Tools/Boss/Configurar Animator do Boss")]
    public static void Configurar()
    {
        string controllerPath = "Assets/prefebs/boss/bossmaga.controller";
        string aseFase1       = "Assets/prefebs/boss/maga slime parada.ase";
        string aseFase2       = "Assets/prefebs/boss/maga slime modo 50%.ase";

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            Debug.Log("Animator Controller criado.");
        }

        AnimationClip clipFase1 = CarregarClip(aseFase1);
        AnimationClip clipFase2 = CarregarClip(aseFase2);

        if (clipFase1 == null) { Debug.LogError("Clip Fase1 não encontrado em: " + aseFase1); return; }
        if (clipFase2 == null) { Debug.LogError("Clip Fase2 não encontrado em: " + aseFase2); return; }

        // Recria o controller do zero para evitar transições corrompidas
        AssetDatabase.DeleteAsset(controllerPath);
        controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        AnimatorState estadoFase1 = sm.AddState("Fase1");
        estadoFase1.motion = clipFase1;
        sm.defaultState = estadoFase1;

        AnimatorState estadoFase2 = sm.AddState("Fase2");
        estadoFase2.motion = clipFase2;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        // Atribui ao prefab do boss
        string prefabPath = "Assets/prefebs/boss/boss.prefab";
        using (var editScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject root = editScope.prefabContentsRoot;

            // Garante SpriteRenderer
            SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = root.AddComponent<SpriteRenderer>();
                Debug.Log("SpriteRenderer adicionado ao prefab do boss.");
            }
            sr.sortingLayerName = "Default";
            sr.sortingOrder     = 150;

            // Garante Animator com controller
            Animator anim = root.GetComponent<Animator>();
            if (anim == null) anim = root.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;

            // Garante tag Enemy e Rigidbody2D
            root.tag = "Enemy";
            if (root.GetComponent<Rigidbody2D>() == null)
            {
                var rb = root.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
            }
            if (root.GetComponent<Collider2D>() == null)
            {
                var col = root.AddComponent<CircleCollider2D>();
                col.radius = 0.5f;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("✅ Animator do boss configurado: Fase1 = maga slime parada | Fase2 = maga slime modo 50%");
    }

    static AnimationClip CarregarClip(string asePath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(asePath);
        foreach (var a in assets)
            if (a is AnimationClip clip && !clip.name.StartsWith("__"))
                return clip;
        return null;
    }
}
