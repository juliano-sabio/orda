using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class CriarPrefabSlimeGuarda
{
    const string ASE_PATH    = "Assets/prefebs/inimigos/elite/slimeguarda.aseprite";
    const string PREFAB_PATH = "Assets/prefebs/inimigos/elite/SlimeGuarda.prefab";

    static CriarPrefabSlimeGuarda()
    {
        EditorApplication.delayCall += Executar;
    }

    static void Executar()
    {
        bool prefabJaExistia = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) != null;
        if (prefabJaExistia) return;

        AssetDatabase.ImportAsset(ASE_PATH, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();

        RuntimeAnimatorController controller = null;
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(ASE_PATH))
            if (a is RuntimeAnimatorController rac) { controller = rac; break; }

        if (controller == null)
        {
            Debug.LogWarning("[SlimeGuarda] Controller não encontrado — aguardando import.");
            return;
        }

        var root = new GameObject("SlimeGuarda");
        root.tag   = "Enemy";
        root.layer = LayerMask.NameToLayer("Enemy");

        var sr = root.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;

        var anim = root.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;

        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var offset = new Vector2(0f, 0.1f);

        var colFisico = root.AddComponent<CircleCollider2D>();
        colFisico.radius    = 0.1f;
        colFisico.offset    = offset;
        colFisico.isTrigger = false;

        var colTrigger = root.AddComponent<CircleCollider2D>();
        colTrigger.radius    = 0.1f;
        colTrigger.offset    = offset;
        colTrigger.isTrigger = true;

        var xpOrb = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefebs/XP/Xp_orb.prefab");

        var ic = root.AddComponent<InimigoController>();
        ic.vidaAtual            = 500f;
        ic.vidaMaxima           = 500f;
        ic.imuneAoDano          = false;
        ic.mostrarDanoFlutuante = true;
        ic.forcaDrop            = 3f;
        ic.xpOrbPrefab          = xpOrb;
        ic.minOrbs              = 3;
        ic.maxOrbs              = 6;
        ic.xpPorOrbe            = 10f;

        // DanoInimigo com dano 0 apenas para silenciar o fallback do InimigoController
        var di = root.AddComponent<DanoInimigo>();
        di.dano = 0f;

        root.AddComponent<SlimeGuarda>();

        root.transform.localScale = Vector3.one * 8f;

        bool ok;
        PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH, out ok);
        Object.DestroyImmediate(root);

        if (ok)
        {
            Debug.Log($"[SlimeGuarda] Prefab criado em {PREFAB_PATH}");
            AdicionarAoSpawner();
        }
        else
        {
            Debug.LogError("[SlimeGuarda] Falha ao salvar prefab.");
        }
    }

    static void AdicionarAoSpawner()
    {
        var prefabGO = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        if (prefabGO == null) return;

        var spawner = Object.FindFirstObjectByType<EnemySpawnerCompleto>(FindObjectsInactive.Include);
        if (spawner == null)
        {
            Debug.LogWarning("[SlimeGuarda] EnemySpawnerCompleto não encontrado na cena aberta. Adicione SlimeGuarda ao spawner manualmente.");
            return;
        }

        var so   = new SerializedObject(spawner);
        var list = so.FindProperty("tiposInimigos");

        // Verifica se já existe
        for (int i = 0; i < list.arraySize; i++)
        {
            var el = list.GetArrayElementAtIndex(i);
            if (el.FindPropertyRelative("prefab").objectReferenceValue == prefabGO) return;
        }

        int idx = list.arraySize;
        list.InsertArrayElementAtIndex(idx);
        var entry = list.GetArrayElementAtIndex(idx);
        entry.FindPropertyRelative("prefab").objectReferenceValue = prefabGO;
        entry.FindPropertyRelative("nome").stringValue            = "SlimeGuarda";
        entry.FindPropertyRelative("tempoParaAparecer").intValue  = 90;
        entry.FindPropertyRelative("peso").floatValue             = 1f;

        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);

        Debug.Log("[SlimeGuarda] Adicionado ao spawner com sucesso.");
    }
}
