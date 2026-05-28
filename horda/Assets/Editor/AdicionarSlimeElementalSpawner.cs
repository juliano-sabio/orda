using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;

[InitializeOnLoad]
public static class AdicionarSlimeElementalSpawner
{
    const string PREFAB_PATH = "Assets/prefebs/inimigos/slime_ellemental/SlimeElemental.prefab";
    const string SCENE_PATH  = "Assets/Scenes/primeira_fase.unity";

    static AdicionarSlimeElementalSpawner()
    {
        EditorApplication.delayCall += Executar;
    }

    static void Executar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        if (prefab == null)
        {
            // prefab ainda não criado — aguarda próxima compilação
            return;
        }

        // Procura o spawner na cena aberta
        var spawner = Object.FindFirstObjectByType<EnemySpawnerCompleto>();
        if (spawner == null)
        {
            Debug.LogWarning("[AdicionarSlimeElemental] EnemySpawnerCompleto não encontrado na cena aberta.");
            return;
        }

        // Verifica se já está na lista
        foreach (var tipo in spawner.tiposInimigos)
            if (tipo.prefab == prefab) return;

        // Usa reflection para criar a instância interna (classe serializable aninhada)
        var tipoInimigo = new EnemySpawnerCompleto.TipoInimigo
        {
            prefab            = prefab,
            nome              = "SlimeElemental",
            tempoParaAparecer = 60,
            peso              = 1f
        };

        spawner.tiposInimigos.Add(tipoInimigo);

        EditorUtility.SetDirty(spawner);
        EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);

        Debug.Log("[AdicionarSlimeElemental] SlimeElemental adicionado ao spawner com sucesso.");
    }
}
