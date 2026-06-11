using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

public static class FantasmaAnimSetup
{
    const string SpriteSheetPath = "Assets/assets/mobs/fantasma1/fantasma 02.ase";
    const string ClipPath        = "Assets/assets/mobs/fantasma1/fantasma02_flutuar.anim";
    const string ControllerPath  = "Assets/assets/mobs/fantasma1/fantasma02_anim.controller";

    static readonly string[] PrefabPaths =
    {
        "Assets/prefebs/inimigos/fantasma_veneno_atirador.prefab",
    };

    [MenuItem("Tools/Criar Animação Fantasma 02")]
    public static void CriarAnimacao()
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(SpriteSheetPath)
            .OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogError("Nenhum sprite encontrado em " + SpriteSheetPath);
            return;
        }

        // Cria o AnimationClip com troca de sprite (Frame_0..Frame_N)
        var clip = new AnimationClip { frameRate = 6 };
        var binding = new EditorCurveBinding
        {
            path = "",
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };

        var keyframes = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / clip.frameRate,
                value = sprites[i]
            };
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.DeleteAsset(ClipPath);
        AssetDatabase.CreateAsset(clip, ClipPath);

        // Cria o AnimatorController com um único estado em loop
        AssetDatabase.DeleteAsset(ControllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddMotion(clip);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Adiciona/atualiza o Animator nos prefabs dos fantasmas
        foreach (var prefabPath in PrefabPaths)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                Debug.LogWarning("Prefab não encontrado: " + prefabPath);
                continue;
            }

            var animator = root.GetComponent<Animator>();
            if (animator == null) animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        Debug.Log($"Animação 'fantasma02_flutuar' criada com {sprites.Length} frames e aplicada aos fantasmas.");
    }
}
