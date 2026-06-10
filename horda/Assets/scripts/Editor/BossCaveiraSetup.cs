#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class BossCaveiraSetup
{
    const string PASTA       = "Assets/prefebs/boss/bosscaveira";
    const string CONTROLLER  = "Assets/prefebs/boss/bosscaveira/bosscaveira.controller";
    const string PREFAB      = "Assets/prefebs/boss/bosscaveira/bosscaveira.prefab";
    const string ASE_BOSS    = "Assets/prefebs/boss/bosscaveira/criatura da noite.ase";
    const string ASE_PROJ    = "Assets/prefebs/boss/bosscaveira/projetil_noite.ase";
    const string PROJ_PREFAB = "Assets/prefebs/boss/bosscaveira/ProjetilNoite.prefab";

    [MenuItem("Tools/Boss/Configurar BossCaveira")]
    public static void Configurar()
    {
        if (!AssetDatabase.IsValidFolder(PASTA))
        {
            AssetDatabase.CreateFolder("Assets/prefebs/boss", "bosscaveira");
            AssetDatabase.Refresh();
        }

        // ── Animator Controller ─────────────────────────────────────
        var clips = CarregarTodosClips(ASE_BOSS);
        if (clips.Length == 0)
        {
            Debug.LogError("[BossCaveiraSetup] Nenhum clip encontrado em: " + ASE_BOSS);
            return;
        }
        AnimationClip clipFlutuar = clips[0];

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER) != null)
            AssetDatabase.DeleteAsset(CONTROLLER);

        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER);
        var sm = ctrl.layers[0].stateMachine;
        var sFlutuar = sm.AddState("Flutuar");
        sFlutuar.motion = clipFlutuar;
        sm.defaultState = sFlutuar;

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();

        // ── Projétil "Noite" ─────────────────────────────────────────
        Sprite sprProjetil = CarregarPrimeiroSprite(ASE_PROJ);
        GameObject prefabProjetil = ConfigurarProjetilNoite(sprProjetil);

        // ── Prefab do Boss ────────────────────────────────────────────
        bool prefabExiste = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB) != null;
        if (!prefabExiste)
        {
            var tmp = new GameObject("bosscaveira");
            PrefabUtility.SaveAsPrefabAsset(tmp, PREFAB);
            Object.DestroyImmediate(tmp);
            AssetDatabase.Refresh();
        }

        using (var scope = new PrefabUtility.EditPrefabContentsScope(PREFAB))
        {
            var root = scope.prefabContentsRoot;
            root.tag = "Enemy";

            SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
            if (sr == null) sr = root.AddComponent<SpriteRenderer>();

            Animator anim = root.GetComponent<Animator>();
            if (anim == null) anim = root.AddComponent<Animator>();

            CircleCollider2D trigger = root.GetComponent<CircleCollider2D>();
            if (trigger == null) trigger = root.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius    = 0.35f;
            // O pivot do sprite "criatura da noite" fica bem abaixo do desenho (canvas Aseprite),
            // então deslocamos o collider para cima para alinhar com o corpo. Ajuste fino no Inspector se preciso.
            trigger.offset    = new Vector2(0f, 1.2f);

            InimigoController ic = root.GetComponent<InimigoController>();
            if (ic == null) ic = root.AddComponent<InimigoController>();

            BossCaveira boss = root.GetComponent<BossCaveira>();
            if (boss == null) boss = root.AddComponent<BossCaveira>();

            // Dano de contato usado durante a Investida (fica desativado fora dela)
            DanoInimigo dano = root.GetComponent<DanoInimigo>();
            if (dano == null) dano = root.AddComponent<DanoInimigo>();
            dano.dano            = 18f;
            dano.intervaloAtaque = 1f;
            dano.enabled         = false;

            // Configura sprite
            var assets = AssetDatabase.LoadAllAssetsAtPath(ASE_BOSS);
            foreach (var a in assets)
                if (a is Sprite spr) { sr.sprite = spr; break; }
            sr.sortingLayerName = "Default";
            sr.sortingOrder     = 150;

            anim.runtimeAnimatorController = ctrl;

            ic.vidaMaxima = 2500f;
            ic.vidaAtual  = 2500f;

            if (prefabProjetil != null)
                boss.prefabProjetil = prefabProjetil;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("✅ BossCaveira configurado! Prefab em: " + PREFAB);
    }

    static GameObject ConfigurarProjetilNoite(Sprite sprite)
    {
        bool prefabExiste = AssetDatabase.LoadAssetAtPath<GameObject>(PROJ_PREFAB) != null;
        if (!prefabExiste)
        {
            var tmp = new GameObject("ProjetilNoite");
            PrefabUtility.SaveAsPrefabAsset(tmp, PROJ_PREFAB);
            Object.DestroyImmediate(tmp);
            AssetDatabase.Refresh();
        }

        using (var scope = new PrefabUtility.EditPrefabContentsScope(PROJ_PREFAB))
        {
            var root = scope.prefabContentsRoot;

            SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
            if (sr == null) sr = root.AddComponent<SpriteRenderer>();
            if (sprite != null) sr.sprite = sprite;
            sr.sortingLayerName = "Default";
            sr.sortingOrder     = 160;

            Rigidbody2D rb = root.GetComponent<Rigidbody2D>();
            if (rb == null) rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

            CircleCollider2D col = root.GetComponent<CircleCollider2D>();
            if (col == null) col = root.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius    = 0.1f;

            ProjetilInimigoDano pid = root.GetComponent<ProjetilInimigoDano>();
            if (pid == null) pid = root.AddComponent<ProjetilInimigoDano>();
            pid.corLuz = new Color(0.5f, 0.1f, 0.8f);
        }

        AssetDatabase.SaveAssets();
        return AssetDatabase.LoadAssetAtPath<GameObject>(PROJ_PREFAB);
    }

    static Sprite CarregarPrimeiroSprite(string path)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var a in assets)
            if (a is Sprite spr) return spr;
        return null;
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
}
#endif
