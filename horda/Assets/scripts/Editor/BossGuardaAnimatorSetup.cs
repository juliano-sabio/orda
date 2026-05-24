using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class BossGuardaAnimatorSetup
{
    const string CONTROLLER = "Assets/prefebs/boss/bossguarda/bossguarda.controller";
    const string PREFAB     = "Assets/prefebs/boss/bossguarda/bossguarda.prefab";
    const string ASE_MOV    = "Assets/prefebs/boss/bossguarda/slime guarda movmimento.ase";
    const string ASE_ATK    = "Assets/prefebs/boss/bossguarda/slime guarda ataque.ase";
    const string ASE_CURA   = "Assets/prefebs/boss/bossguarda/slime gurda cura.ase";

    [MenuItem("Tools/Boss/Configurar Animator BossGuarda")]
    public static void Configurar()
    {
        AnimationClip clipMov  = CarregarClip(ASE_MOV);
        AnimationClip clipAtk  = CarregarClip(ASE_ATK);
        AnimationClip clipCura = CarregarClip(ASE_CURA);

        if (clipMov  == null) { Debug.LogError("Clip Movimento não encontrado: " + ASE_MOV);  return; }
        if (clipAtk  == null) { Debug.LogError("Clip Ataque não encontrado: "   + ASE_ATK);   return; }
        if (clipCura == null) { Debug.LogError("Clip Cura não encontrado: "     + ASE_CURA);  return; }

        // Recria do zero
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER) != null)
            AssetDatabase.DeleteAsset(CONTROLLER);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER);

        // Parâmetros
        controller.AddParameter("Atacando", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Curando",  AnimatorControllerParameterType.Bool);

        var sm = controller.layers[0].stateMachine;

        // Estados
        var sMov  = sm.AddState("Movimento");  sMov.motion  = clipMov;
        var sAtk  = sm.AddState("Ataque");     sAtk.motion  = clipAtk;
        var sCura = sm.AddState("Cura");       sCura.motion = clipCura;
        sm.defaultState = sMov;

        // Transições Movimento → Ataque
        var tAtk = sMov.AddTransition(sAtk);
        tAtk.AddCondition(AnimatorConditionMode.If, 0, "Atacando");
        tAtk.hasExitTime = false; tAtk.duration = 0.05f;

        // Ataque → Movimento
        var tMov = sAtk.AddTransition(sMov);
        tMov.AddCondition(AnimatorConditionMode.IfNot, 0, "Atacando");
        tMov.hasExitTime = false; tMov.duration = 0.05f;

        // Movimento → Cura
        var tCura = sMov.AddTransition(sCura);
        tCura.AddCondition(AnimatorConditionMode.If, 0, "Curando");
        tCura.hasExitTime = false; tCura.duration = 0.05f;

        // Cura → Movimento
        var tMov2 = sCura.AddTransition(sMov);
        tMov2.AddCondition(AnimatorConditionMode.IfNot, 0, "Curando");
        tMov2.hasExitTime = false; tMov2.duration = 0.05f;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        // Aplica no prefab
        using (var scope = new PrefabUtility.EditPrefabContentsScope(PREFAB))
        {
            var root = scope.prefabContentsRoot;

            // SpriteRenderer
            var sr = root.GetComponent<SpriteRenderer>();
            if (sr == null) sr = root.AddComponent<SpriteRenderer>();
            // Pega o primeiro sprite do clip de movimento como pose padrão
            var sprites = AssetDatabase.LoadAllAssetsAtPath(ASE_MOV);
            foreach (var a in sprites)
            {
                if (a is Sprite spr) { sr.sprite = spr; break; }
            }
            sr.sortingLayerName = "Default";
            sr.sortingOrder     = 150;

            // Animator
            var anim = root.GetComponent<Animator>();
            if (anim == null) anim = root.AddComponent<Animator>();
            if (anim != null) anim.runtimeAnimatorController = controller;

            // Tag
            root.tag = "Enemy";
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("✅ BossGuarda configurado: Movimento | Ataque | Cura | controller salvo em " + CONTROLLER);
    }

    static AnimationClip CarregarClip(string asePath)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(asePath);
        foreach (var a in assets)
            if (a is AnimationClip clip && !clip.name.StartsWith("__"))
                return clip;
        return null;
    }
}
