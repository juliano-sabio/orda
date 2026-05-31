#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class ConfigurarInimigosElite
{
    const float TEMPO_ELITE_SEGUNDOS   = 900f; // 15 minutos
    const float TEMPO_CURATIVA_SEGUNDOS = 780f; // 13 minutos

    [MenuItem("Tools/Inimigos/Configurar Slime Curativa (13 min)")]
    public static void ConfigurarSlimeCurativa()
    {
        ConfigurarPorNome("slime_curativa", TEMPO_CURATIVA_SEGUNDOS, "13 minutos");
    }

    static void ConfigurarPorNome(string nomePrefab, float tempoSegundos, string labelTempo)
    {
        var spawners = Object.FindObjectsByType<EnemySpawnerCompleto>(FindObjectsSortMode.None);
        if (spawners.Length == 0)
        {
            Debug.LogWarning("⚠️ Nenhum EnemySpawnerCompleto encontrado. Abra a cena de jogo antes.");
            return;
        }

        int total = 0;
        foreach (var spawner in spawners)
        {
            bool alterou = false;
            foreach (var tipo in spawner.tiposInimigos)
            {
                if (tipo.prefab == null) continue;
                if (!tipo.prefab.name.ToLower().Contains(nomePrefab.ToLower())) continue;
                if (tipo.tempoParaAparecer == (int)tempoSegundos) continue;

                tipo.tempoParaAparecer = (int)tempoSegundos;
                Debug.Log($"✅ [{tipo.prefab.name}] → tempoParaAparecer = {tempoSegundos}s ({labelTempo})");
                alterou = true;
                total++;
            }
            if (alterou) EditorUtility.SetDirty(spawner);
        }

        if (total > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            Debug.Log($"✅ {total} entrada(s) configurada(s) para aparecer após {labelTempo}.");
        }
        else
            Debug.Log($"ℹ️ '{nomePrefab}' não encontrado na lista do spawner ou já configurado.");
    }

    [MenuItem("Tools/Inimigos/Configurar Elite (15 min)")]
    public static void Configurar()
    {
        var spawners = Object.FindObjectsByType<EnemySpawnerCompleto>(FindObjectsSortMode.None);
        if (spawners.Length == 0)
        {
            Debug.LogWarning("⚠️ Nenhum EnemySpawnerCompleto encontrado na cena. Abra a cena de jogo antes.");
            return;
        }

        int totalAlterados = 0;

        foreach (var spawner in spawners)
        {
            bool alterou = false;
            foreach (var tipo in spawner.tiposInimigos)
            {
                if (tipo.prefab == null) continue;

                string caminho = AssetDatabase.GetAssetPath(tipo.prefab).ToLower();
                bool ehElite   = caminho.Contains("/elite/") || caminho.Contains("\\elite\\");

                if (ehElite && tipo.tempoParaAparecer < (int)TEMPO_ELITE_SEGUNDOS)
                {
                    tipo.tempoParaAparecer = (int)TEMPO_ELITE_SEGUNDOS;
                    Debug.Log($"✅ [{tipo.prefab.name}] → tempoParaAparecer = {TEMPO_ELITE_SEGUNDOS}s (15 min)");
                    alterou = true;
                    totalAlterados++;
                }
            }

            if (alterou) EditorUtility.SetDirty(spawner);
        }

        if (totalAlterados > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            Debug.Log($"✅ {totalAlterados} inimigo(s) elite configurado(s) para aparecer após 15 minutos.");
        }
        else
        {
            Debug.Log("ℹ️ Nenhum inimigo elite encontrado na lista do spawner, ou já estavam configurados.");
        }
    }
}
#endif
