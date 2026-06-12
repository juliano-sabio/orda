using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

public static class FantasmaAnimSetup
{
    struct GhostDef
    {
        public string spriteSheet;
        public string clipPath;
        public string controllerPath;
        public string prefabPath;
    }

    static readonly GhostDef[] Ghosts = new GhostDef[]
    {
        new GhostDef {
            spriteSheet    = "Assets/assets/mobs/fantasma1/fantasma 02.ase",
            clipPath       = "Assets/assets/mobs/fantasma1/fantasma02_flutuar.anim",
            controllerPath = "Assets/assets/mobs/fantasma1/fantasma02_anim.controller",
            prefabPath     = "Assets/prefebs/inimigos/fantasma_veneno.prefab",
        },
        new GhostDef {
            spriteSheet    = "Assets/assets/mobs/fantasma4/fantasmas 05.aseprite",
            clipPath       = "Assets/assets/mobs/fantasma4/fantasma05_flutuar.anim",
            controllerPath = "Assets/assets/mobs/fantasma4/fantasma05_anim.controller",
            prefabPath     = "Assets/prefebs/inimigos/fantasma_fogo.prefab",
        },
        new GhostDef {
            spriteSheet    = "Assets/assets/mobs/fantasma2/fantasma 03.ase",
            clipPath       = "Assets/assets/mobs/fantasma2/fantasma03_flutuar.anim",
            controllerPath = "Assets/assets/mobs/fantasma2/fantasma03_anim.controller",
            prefabPath     = "Assets/prefebs/inimigos/fantasma_gelo.prefab",
        },
        new GhostDef {
            spriteSheet    = "Assets/assets/mobs/fantasma3/fantasma 04.ase",
            clipPath       = "Assets/assets/mobs/fantasma3/fantasma04_flutuar.anim",
            controllerPath = "Assets/assets/mobs/fantasma3/fantasma04_anim.controller",
            prefabPath     = "Assets/prefebs/inimigos/fantasma_eletrico.prefab",
        },
    };

    [MenuItem("Tools/Criar Animações Fantasmas Elementais")]
    public static void CriarTodasAnimacoes()
    {
        int total = 0;
        foreach (var g in Ghosts)
            total += CriarAnimacaoParaFantasma(g);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Animações criadas para " + total + "/" + Ghosts.Length + " fantasmas.");
    }

    static int CriarAnimacaoParaFantasma(GhostDef g)
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(g.spriteSheet)
            .OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogWarning("Nenhum sprite em: " + g.spriteSheet);
            return 0;
        }

        var clip = new AnimationClip { frameRate = 8 };
        var binding = new EditorCurveBinding
        {
            path         = "",
            type         = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };

        var keyframes = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time  = i / clip.frameRate,
                value = sprites[i]           // propriedade correta
            };
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.DeleteAsset(g.clipPath);
        AssetDatabase.CreateAsset(clip, g.clipPath);

        AssetDatabase.DeleteAsset(g.controllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(g.controllerPath);
        controller.AddMotion(clip);

        var root = PrefabUtility.LoadPrefabContents(g.prefabPath);
        if (root == null)
        {
            Debug.LogWarning("Prefab não encontrado: " + g.prefabPath);
            return 0;
        }

        var animator = root.GetComponent<Animator>();
        if (animator == null) animator = root.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        PrefabUtility.SaveAsPrefabAsset(root, g.prefabPath);
        PrefabUtility.UnloadPrefabContents(root);

        Debug.Log(g.prefabPath + " — " + sprites.Length + " frames @ 8fps");
        return 1;
    }

    [MenuItem("Tools/Configurar Projétil Fantasma Gelo")]
    public static void ConfigurarProjetilFantasmaGelo()
    {
        const string spriteAsset = "Assets/assets/mobs/fantasma2/fantasma 03 projetil.ase";
        const string prefabPath  = "Assets/prefebs/inimigos/fantasma_gelo.prefab";

        var sprite = AssetDatabase.LoadAllAssetsAtPath(spriteAsset)
            .OfType<Sprite>()
            .FirstOrDefault();

        if (sprite == null)
        {
            Debug.LogWarning("Sprite de projétil não encontrado em: " + spriteAsset);
            return;
        }

        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null)
        {
            Debug.LogWarning("Prefab não encontrado: " + prefabPath);
            return;
        }

        var fantasmaGelo = root.GetComponent<FantasmaGelo>();
        if (fantasmaGelo == null)
        {
            Debug.LogWarning("Componente FantasmaGelo não encontrado em: " + prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            return;
        }

        var so = new SerializedObject(fantasmaGelo);
        so.FindProperty("spriteProjetil").objectReferenceValue = sprite;
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);

        AssetDatabase.SaveAssets();
        Debug.Log("Sprite de projétil configurado em " + prefabPath);
    }
}
