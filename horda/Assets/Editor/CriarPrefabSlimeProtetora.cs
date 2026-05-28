using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class CriarPrefabSlimeProtetora
{
    const string ASE_PATH    = "Assets/prefebs/inimigos/elite/slime_protetora_inimiga.ase";
    const string PREFAB_PATH = "Assets/prefebs/inimigos/elite/SlimeProtetoraInimiga.prefab";

    static CriarPrefabSlimeProtetora()
    {
        EditorApplication.delayCall += Executar;
    }

    static void Executar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) != null) return;

        AssetDatabase.ImportAsset(ASE_PATH, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();

        RuntimeAnimatorController controller = null;
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(ASE_PATH))
            if (a is RuntimeAnimatorController rac) { controller = rac; break; }

        if (controller == null)
        {
            Debug.LogWarning("[SlimeProtetora] Controller não encontrado — aguardando import.");
            return;
        }

        var root = new GameObject("SlimeProtetoraInimiga");
        root.tag   = "Enemy";
        root.layer = LayerMask.NameToLayer("Enemy");

        var sr   = root.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;

        var anim = root.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;

        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var offset = new Vector2(0f, 0.185f);

        // Collider físico (bloqueia paredes)
        var colFisico = root.AddComponent<CircleCollider2D>();
        colFisico.radius    = 0.185f;
        colFisico.offset    = offset;
        colFisico.isTrigger = false;

        // Collider trigger (detecção de contato)
        var colTrigger = root.AddComponent<CircleCollider2D>();
        colTrigger.radius    = 0.185f;
        colTrigger.offset    = offset;
        colTrigger.isTrigger = true;

        var xpOrb = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefebs/XP/Xp_orb.prefab");

        var ic = root.AddComponent<InimigoController>();
        ic.vidaAtual            = 180f;
        ic.vidaMaxima           = 180f;
        ic.imuneAoDano          = false;
        ic.mostrarDanoFlutuante = true;
        ic.forcaDrop            = 3f;
        ic.xpOrbPrefab          = xpOrb;
        ic.minOrbs              = 4;
        ic.maxOrbs              = 8;
        ic.xpPorOrbe            = 10f;

        root.AddComponent<SlimeProtetoraInimiga>();

        root.transform.localScale = Vector3.one * 7f;

        bool ok;
        PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH, out ok);
        Object.DestroyImmediate(root);

        if (ok) Debug.Log($"[SlimeProtetora] Prefab criado em {PREFAB_PATH}");
        else    Debug.LogError("[SlimeProtetora] Falha ao salvar prefab.");
    }
}
