using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class CriarPrefabEspritoCura
{
    const string ASE_PATH    = "Assets/prefebs/poção de cura/espirito de cura.ase";
    const string PREFAB_PATH = "Assets/prefebs/poção de cura/espirito de cura fx.prefab";

    static CriarPrefabEspritoCura()
    {
        EditorApplication.delayCall += Executar;
    }

    static void Executar()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) != null)
            return;

        AssetDatabase.ImportAsset(ASE_PATH, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();

        var allAssets = AssetDatabase.LoadAllAssetsAtPath(ASE_PATH);
        RuntimeAnimatorController controller = null;
        foreach (var a in allAssets)
        {
            if (a is RuntimeAnimatorController rac) { controller = rac; break; }
        }

        if (controller == null)
        {
            Debug.LogWarning("[EspritoCura] Controller não encontrado no .ase — aguardando próximo import.");
            return;
        }

        var root = new GameObject("EspritoCura");

        var sr = root.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 15;

        var anim = root.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;

        // Collider trigger para coleta
        var col = root.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.35f;

        // Script de drop de cura existente
        root.AddComponent<PocaoCura>();

        bool ok;
        PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH, out ok);
        Object.DestroyImmediate(root);

        if (ok)
            Debug.Log($"[EspritoCura] Prefab de drop criado em {PREFAB_PATH}");
        else
            Debug.LogError("[EspritoCura] Falha ao salvar prefab.");
    }
}
