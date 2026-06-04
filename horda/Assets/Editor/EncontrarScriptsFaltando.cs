#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public static class EncontrarScriptsFaltando
{
    [MenuItem("Tools/Debug/Encontrar Scripts Faltando na Cena")]
    static void EncontrarNaCena()
    {
        int total = 0;
        var todos = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var go in todos)
        {
            var comps = go.GetComponents<Component>();
            foreach (var c in comps)
            {
                if (c == null)
                {
                    Debug.LogWarning($"[Script Faltando] GameObject: '{go.name}' | Path: {GetPath(go)}", go);
                    total++;
                }
            }
        }
        if (total == 0) Debug.Log("Nenhum script faltando na cena!");
        else Debug.LogWarning($"Total: {total} script(s) faltando encontrados.");
    }

    [MenuItem("Tools/Debug/Encontrar e Limpar Scripts Faltando em Prefabs")]
    static void LimparEmPrefabs()
    {
        int totalPrefabs = 0;
        int totalScripts = 0;
        var guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            bool temFaltando = false;
            foreach (var c in prefab.GetComponentsInChildren<Component>(true))
                if (c == null) { temFaltando = true; break; }

            if (!temFaltando) continue;

            // Abre o prefab, limpa, salva
            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                var root = scope.prefabContentsRoot;
                foreach (var go in root.GetComponentsInChildren<Transform>(true))
                {
                    int r = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go.gameObject);
                    if (r > 0)
                    {
                        Debug.Log($"[Prefab Limpo] '{path}' — removidos {r} script(s) de '{go.name}'");
                        totalScripts += r;
                    }
                }
            }
            totalPrefabs++;
        }

        AssetDatabase.SaveAssets();
        if (totalPrefabs == 0) Debug.Log("Nenhum prefab com scripts faltando.");
        else Debug.Log($"Limpeza concluída: {totalScripts} script(s) removidos de {totalPrefabs} prefab(s).");
    }

    [MenuItem("Tools/Debug/Remover Scripts Faltando da Cena")]
    static void RemoverDaCena()
    {
        int total = 0;
        var todos = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var go in todos)
        {
            int removidos = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (removidos > 0)
            {
                Debug.Log($"[Limpeza] Removidos {removidos} script(s) de '{go.name}'");
                total += removidos;
                EditorUtility.SetDirty(go);
            }
        }
        if (total > 0)
        {
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"Total removido: {total}. Salve a cena (Ctrl+S).");
        }
        else Debug.Log("Nada para remover.");
    }

    static string GetPath(GameObject go)
    {
        string path = go.name;
        var t = go.transform.parent;
        while (t != null) { path = t.name + "/" + path; t = t.parent; }
        return path;
    }
}
#endif
