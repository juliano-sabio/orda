using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class CriarPrefabSlimeElemental
{
    const string ASE_PATH    = "Assets/prefebs/inimigos/slime_ellemental/slimeelemental.ase";
    const string PREFAB_PATH = "Assets/prefebs/inimigos/slime_ellemental/SlimeElemental.prefab";

    static CriarPrefabSlimeElemental()
    {
        EditorApplication.delayCall += Executar;
    }

    static void Executar()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) != null) return;

        AssetDatabase.ImportAsset(ASE_PATH, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();

        RuntimeAnimatorController controller = null;
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(ASE_PATH))
            if (a is RuntimeAnimatorController rac) { controller = rac; break; }

        if (controller == null)
        {
            Debug.LogWarning("[SlimeElemental] Controller não encontrado — aguardando import.");
            return;
        }

        var root = new GameObject("SlimeElemental");
        root.tag   = "Enemy";
        root.layer = LayerMask.NameToLayer("Enemy");

        // Sprite + Animator
        var sr   = root.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;

        var anim = root.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;

        // Física
        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Pivot do sprite = Bottom → sprite fica acima do transform
        // offset Y local = (altura/2) / PPU = (37/2) / 100 = 0.185
        var offset = new Vector2(0f, 0.185f);

        // Collider físico — bloqueia paredes, recebe dano do player
        var colFisico = root.AddComponent<CircleCollider2D>();
        colFisico.radius    = 0.185f;
        colFisico.offset    = offset;
        colFisico.isTrigger = false;

        // Collider trigger — detecta contato com o player (dano de toque)
        var col = root.AddComponent<CircleCollider2D>();
        col.radius    = 0.185f;
        col.offset    = offset;
        col.isTrigger = true;

        // Inimigo
        var xpOrb = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefebs/XP/Xp_orb.prefab");

        var ic = root.AddComponent<InimigoController>();
        ic.vidaAtual            = 250f;
        ic.vidaMaxima           = 250f;
        ic.imuneAoDano          = false;
        ic.mostrarDanoFlutuante = true;
        ic.forcaDrop            = 3f;
        ic.xpOrbPrefab          = xpOrb;
        ic.minOrbs              = 5;
        ic.maxOrbs              = 10;
        ic.xpPorOrbe            = 8f;

        var di = root.AddComponent<DanoInimigo>();
        di.dano            = 12f;
        di.intervaloAtaque = 1f;

        // Comportamento elemental
        root.AddComponent<SlimeElemental>();

        // Escala (sprite 43x37 px a 100 PPU → escala 8 ≈ 3.4 unidades)
        root.transform.localScale = Vector3.one * 8f;

        bool ok;
        PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH, out ok);
        Object.DestroyImmediate(root);

        if (ok) Debug.Log($"[SlimeElemental] Prefab criado em {PREFAB_PATH}");
        else    Debug.LogError("[SlimeElemental] Falha ao salvar prefab.");
    }
}
