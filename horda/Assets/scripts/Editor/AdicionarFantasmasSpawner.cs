#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class AdicionarFantasmasSpawner
{
    static readonly (string path, string nome, int tempo, float peso)[] FANTASMAS =
    {
        ("Assets/prefebs/inimigos/fantasminha.prefab",        "Fantasminha",      0,   2.0f),
        ("Assets/prefebs/inimigos/fantasminha02.prefab",      "Fantasminha02",    180, 2.0f),
        ("Assets/prefebs/inimigos/fantasma_veneno.prefab",   "FantasmaVeneno",   0,   1.5f),
        ("Assets/prefebs/inimigos/fantasma_fogo.prefab",     "FantasmaFogo",     120, 1.2f),
        ("Assets/prefebs/inimigos/fantasma_gelo.prefab",     "FantasmaGelo",     120, 1.2f),
        ("Assets/prefebs/inimigos/fantasma_eletrico.prefab", "FantasmaEletrico", 180, 1.0f),
    };

    [MenuItem("Tools/Inimigos/Adicionar Fantasmas Elementais ao Spawner")]
    public static void Adicionar()
    {
        var spawners = Object.FindObjectsByType<EnemySpawnerCompleto>(FindObjectsSortMode.None);
        if (spawners.Length == 0)
        {
            Debug.LogWarning("Nenhum EnemySpawnerCompleto encontrado. Abra a cena de jogo antes.");
            return;
        }

        int totalAdicionados = 0;

        foreach (var spawner in spawners)
        {
            bool alterou = false;

            foreach (var (path, nome, tempo, peso) in FANTASMAS)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogWarning($"Prefab não encontrado: {path}");
                    continue;
                }

                bool jaExiste = false;
                foreach (var tipo in spawner.tiposInimigos)
                    if (tipo.prefab == prefab) { jaExiste = true; break; }

                if (jaExiste) continue;

                spawner.tiposInimigos.Add(new EnemySpawnerCompleto.TipoInimigo
                {
                    prefab            = prefab,
                    nome              = nome,
                    tempoParaAparecer = tempo,
                    peso              = peso,
                });

                Debug.Log($"[Spawner] {nome} adicionado — aparece após {tempo}s, peso {peso}");
                alterou = true;
                totalAdicionados++;
            }

            if (alterou)
            {
                EditorUtility.SetDirty(spawner);
                EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
            }
        }

        if (totalAdicionados > 0)
            Debug.Log($"{totalAdicionados} fantasma(s) adicionado(s) ao spawner. Salve a cena (Ctrl+S).");
        else
            Debug.Log("Todos os fantasmas já estavam no spawner.");
    }
}
#endif
