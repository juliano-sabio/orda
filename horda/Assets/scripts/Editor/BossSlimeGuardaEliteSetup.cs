#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class BossSlimeGuardaEliteSetup
{
    const string PASTA      = "Assets/prefebs/boss/bossslimeguardaelite";
    const string CONTROLLER = "Assets/prefebs/boss/bossslimeguardaelite/bossslimeguardaelite.controller";
    const string PREFAB     = "Assets/prefebs/boss/bossslimeguardaelite/bossslimeguardaelite.prefab";
    const string ASE        = "Assets/prefebs/boss/bossguarda/slime guarda.ase";

    [MenuItem("Tools/Boss/Configurar BossSlimeGuardaElite")]
    public static void Configurar()
    {
        // Cria pasta se não existir
        if (!AssetDatabase.IsValidFolder(PASTA))
        {
            AssetDatabase.CreateFolder("Assets/prefebs/boss", "bossslimeguardaelite");
            AssetDatabase.Refresh();
        }

        // Carrega clips do .ase
        var clips = CarregarTodosClips(ASE);
        if (clips.Length == 0)
        {
            Debug.LogError("[BossEliteSetup] Nenhum clip encontrado em: " + ASE);
            return;
        }

        // Usa primeiro clip para movimento; tenta achar outros por nome
        AnimationClip clipMov  = EncontrarClip(clips, "mov",  "idle", "walk") ?? clips[0];
        AnimationClip clipAtk  = EncontrarClip(clips, "atk",  "ataque", "attack") ?? clips[0];
        AnimationClip clipDash = EncontrarClip(clips, "dash") ?? clips[0];

        // Cria/recria o AnimatorController
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER) != null)
            AssetDatabase.DeleteAsset(CONTROLLER);

        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER);
        ctrl.AddParameter("Atacando", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Dashando", AnimatorControllerParameterType.Bool);

        var sm = ctrl.layers[0].stateMachine;
        var sMov  = sm.AddState("Movimento"); sMov.motion  = clipMov;
        var sAtk  = sm.AddState("Ataque");    sAtk.motion  = clipAtk;
        var sDash = sm.AddState("Dash");      sDash.motion = clipDash;
        sm.defaultState = sMov;

        Transicao(sMov,  sAtk,  "Atacando", true,  false);
        Transicao(sAtk,  sMov,  "Atacando", false, false);
        Transicao(sMov,  sDash, "Dashando", true,  false);
        Transicao(sDash, sMov,  "Dashando", false, false);

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();

        // Cria prefab base se não existir
        bool prefabExiste = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB) != null;
        if (!prefabExiste)
        {
            var tmp = new GameObject("bossslimeguardaelite");
            PrefabUtility.SaveAsPrefabAsset(tmp, PREFAB);
            Object.DestroyImmediate(tmp);
            AssetDatabase.Refresh();
        }

        // Edita o prefab
        using (var scope = new PrefabUtility.EditPrefabContentsScope(PREFAB))
        {
            var root = scope.prefabContentsRoot;
            root.tag = "Enemy";

            // Adiciona todos os componentes primeiro
            SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
            if (sr == null) sr = root.AddComponent<SpriteRenderer>();

            Animator anim = root.GetComponent<Animator>();
            if (anim == null) anim = root.AddComponent<Animator>();

            Rigidbody2D rb = root.GetComponent<Rigidbody2D>();
            if (rb == null) rb = root.AddComponent<Rigidbody2D>();

            // Collider físico para movimento (não trigger)
            CapsuleCollider2D col = root.GetComponent<CapsuleCollider2D>();
            if (col == null) col = root.AddComponent<CapsuleCollider2D>();

            // Trigger separado para detecção de dano
            CircleCollider2D trigger = root.GetComponent<CircleCollider2D>();
            if (trigger == null) trigger = root.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius    = 0.5f;

            InimigoController ic = root.GetComponent<InimigoController>();
            if (ic == null) ic = root.AddComponent<InimigoController>();

            if (root.GetComponent<BossSlimeGuardaElite>() == null)
                root.AddComponent<BossSlimeGuardaElite>();

            // Configura após adicionar tudo
            if (sr != null)
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath(ASE);
                foreach (var a in assets)
                    if (a is Sprite spr) { sr.sprite = spr; break; }
                sr.sortingLayerName = "Default";
                sr.sortingOrder     = 150;
            }

            if (anim != null)
                anim.runtimeAnimatorController = ctrl;

            if (rb != null)
            {
                rb.gravityScale           = 0f;
                rb.mass                   = 1000f;
                rb.linearDamping          = 20f;
                rb.constraints            = RigidbodyConstraints2D.FreezeRotation;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }

            if (col != null)
            {
                col.size      = new Vector2(0.6f, 0.8f);
                col.offset    = new Vector2(0f, 0.1f);
                col.isTrigger = false;
            }

            DanoInimigo dano = root.GetComponent<DanoInimigo>();
            if (dano == null) dano = root.AddComponent<DanoInimigo>();
            if (dano != null)
            {
                dano.dano            = 20f;
                dano.intervaloAtaque = 1f;
            }

            if (ic != null)
            {
                ic.vidaMaxima = 3000f;
                ic.vidaAtual  = 3000f;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("✅ BossSlimeGuardaElite configurado! Prefab em: " + PREFAB);
    }

    static void Transicao(AnimatorState de, AnimatorState para, string param, bool valor, bool exitTime)
    {
        var t = de.AddTransition(para);
        t.AddCondition(valor ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, param);
        t.hasExitTime = exitTime;
        t.duration    = 0.05f;
    }

    static AnimationClip[] CarregarTodosClips(string path)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(path);
        var lista = new System.Collections.Generic.List<AnimationClip>();
        foreach (var a in assets)
            if (a is AnimationClip c && !c.name.StartsWith("__"))
                lista.Add(c);
        return lista.ToArray();
    }

    static AnimationClip EncontrarClip(AnimationClip[] clips, params string[] termos)
    {
        foreach (var clip in clips)
            foreach (var termo in termos)
                if (clip.name.ToLower().Contains(termo.ToLower()))
                    return clip;
        return null;
    }
}
#endif
