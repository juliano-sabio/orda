#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

public static class ConfigurarFantasminha02
{
    const string SPRITE_PATH     = "Assets/assets/mobs/fantasminha02/fantasminha02.ase";
    const string CLIP_PATH       = "Assets/assets/mobs/fantasminha02/fantasminha02_flutuar.anim";
    const string CONTROLLER_PATH = "Assets/assets/mobs/fantasminha02/fantasminha02_anim.controller";
    const string PREFAB_PATH     = "Assets/prefebs/inimigos/fantasminha02.prefab";
    const string DATA_PATH       = "Assets/scripts/scriptables_object/inimigos/fantasminha02.asset";

    [MenuItem("Tools/Inimigos/Configurar Fantasminha02 Completo")]
    public static void Configurar()
    {
        CriarAnimacao();
        CriarInimigoData();
        ConfigurarPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Fantasminha02] Configuração completa.");
    }

    static void CriarAnimacao()
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(SPRITE_PATH)
            .OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogWarning("[Fantasminha02] Nenhum sprite encontrado em: " + SPRITE_PATH);
            return;
        }

        // Cria e salva o clip primeiro
        var clip = new AnimationClip { frameRate = 8 };
        var binding = new UnityEditor.EditorCurveBinding
        {
            path         = "",
            type         = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };

        var keyframes = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            keyframes[i] = new ObjectReferenceKeyframe { time = i / clip.frameRate, value = sprites[i] };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.DeleteAsset(CLIP_PATH);
        AssetDatabase.CreateAsset(clip, CLIP_PATH);
        AssetDatabase.SaveAssets();

        // Recarrega o clip do disco para garantir referência válida no controller
        var clipSalvo = AssetDatabase.LoadAssetAtPath<AnimationClip>(CLIP_PATH);

        AssetDatabase.DeleteAsset(CONTROLLER_PATH);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);
        controller.AddMotion(clipSalvo);
        AssetDatabase.SaveAssets();

        Debug.Log("[Fantasminha02] Animação criada — " + sprites.Length + " frames @ 8fps");
    }

    static void CriarInimigoData()
    {
        var data = AssetDatabase.LoadAssetAtPath<InimigoData>(DATA_PATH);
        bool criando = data == null;
        if (criando) data = ScriptableObject.CreateInstance<InimigoData>();

        data.nomeInimigo          = "Fantasminha02";
        data.vidaBase             = 150f;
        data.danoBase             = 17f;
        data.velocidadeBase       = 4f;
        data.tamanho              = 7f;
        data.intervaloAtaque      = 1f;
        data.xpDrop               = 8;
        data.comportamento        = TipoComportamento.Fast;
        data.distanciaAtaque      = 1.5f;
        data.distanciaPerseguicao = 10f;
        data.chanceSpawn          = 1f;

        var firstSprite = AssetDatabase.LoadAllAssetsAtPath(SPRITE_PATH).OfType<Sprite>().FirstOrDefault();
        if (firstSprite != null) data.icon = firstSprite;

        if (criando) AssetDatabase.CreateAsset(data, DATA_PATH);
        else EditorUtility.SetDirty(data);

        Debug.Log("[Fantasminha02] InimigoData " + (criando ? "criado" : "atualizado") + " em: " + DATA_PATH);
    }

    static void ConfigurarPrefab()
    {
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CONTROLLER_PATH);
        var data       = AssetDatabase.LoadAssetAtPath<InimigoData>(DATA_PATH);
        var xpOrb      = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefebs/xp_orb.prefab");

        if (controller == null)
        {
            Debug.LogWarning("[Fantasminha02] Controller não encontrado, crie a animação primeiro.");
            return;
        }

        GameObject root;
        bool prefabExiste = System.IO.File.Exists(PREFAB_PATH);

        if (prefabExiste)
        {
            root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
        }
        else
        {
            root = new GameObject("fantasminha02");
        }

        // SpriteRenderer
        var sr = root.GetComponent<SpriteRenderer>();
        if (sr == null) sr = root.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;
        sr.flipX = true;
        var firstSprite = AssetDatabase.LoadAllAssetsAtPath(SPRITE_PATH).OfType<Sprite>().FirstOrDefault();
        if (firstSprite != null) sr.sprite = firstSprite;

        // Animator
        var anim = root.GetComponent<Animator>();
        if (anim == null) anim = root.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;
        anim.applyRootMotion = false;

        // Rigidbody2D
        var rb = root.GetComponent<Rigidbody2D>();
        if (rb == null) rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Collider2D
        var col = root.GetComponent<CircleCollider2D>();
        if (col == null) col = root.AddComponent<CircleCollider2D>();
        col.isTrigger = false;
        col.radius    = 0.1f;

        // movi_inimigo
        if (root.GetComponent<movi_inimigo>() == null)
            root.AddComponent<movi_inimigo>();

        // InimigoController
        var ic = root.GetComponent<InimigoController>();
        if (ic == null) ic = root.AddComponent<InimigoController>();
        ic.dadosInimigo = data;
        if (xpOrb != null) ic.xpOrbPrefab = xpOrb;
        ic.minOrbs   = 1;
        ic.maxOrbs   = 2;
        ic.xpPorOrbe = 5f;
        ic.drops     = new System.Collections.Generic.List<DropEntry>();

        // DanoInimigo
        var di = root.GetComponent<DanoInimigo>();
        if (di == null) di = root.AddComponent<DanoInimigo>();
        di.dano            = data != null ? data.danoBase : 8f;
        di.intervaloAtaque = data != null ? data.intervaloAtaque : 1f;

        PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);

        if (prefabExiste)
            PrefabUtility.UnloadPrefabContents(root);
        else
            Object.DestroyImmediate(root);

        Debug.Log("[Fantasminha02] Prefab configurado: " + PREFAB_PATH);
    }
}
#endif
