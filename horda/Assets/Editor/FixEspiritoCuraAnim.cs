using UnityEngine;
using UnityEditor;
using System.Linq;

public static class FixEspiritoCuraAnim
{
    [MenuItem("Tools/Fix Espirito de Cura Animation")]
    static void Fix()
    {
        string asePath  = "Assets/prefebs/poção de cura/espirito de cura.ase";
        string animPath = "Assets/prefebs/poção de cura/espirito_de_cura.anim";

        // Load all sprites from the .ase sub-assets
        var allAssets = AssetDatabase.LoadAllAssetsAtPath(asePath);
        var sprites   = allAssets.OfType<Sprite>().OrderBy(s => s.name).ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogError("Nenhum sprite encontrado em: " + asePath);
            return;
        }

        // Load (or create) the .anim clip
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animPath);
        if (clip == null)
        {
            Debug.LogError("Clip não encontrado em: " + animPath);
            return;
        }

        clip.frameRate = 8f;

        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");

        var keyframes = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time  = i / clip.frameRate,
                value = sprites[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();

        Debug.Log($"Animação atualizada com {sprites.Length} frames de '{asePath}'.");
    }
}
