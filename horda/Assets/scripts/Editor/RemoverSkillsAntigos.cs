#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class RemoverSkillsAntigos
{
    // Nomes das skills a remover
    static readonly string[] nomesParaRemover = {
        "spirit ball",
        "spirit spiral",
        "spirit homing",
        "espada rotatoria",
        "espada rotatória",
    };

    // Assets de evolução relacionados
    static readonly string[] evolucoesParaRemover = {
        "Assets/Evolucoes/EspadaOrbitalDupla.asset",
        "Assets/Evolucoes/EspadaSonica.asset",
        "Assets/Evolucoes/SpiritBallDupla.asset",
        "Assets/Evolucoes/SpiritBallExplosiva.asset",
        "Assets/Evolucoes/EspiralDupla.asset",
        "Assets/Evolucoes/EspiralFuracao.asset",
        "Assets/Evolucoes/HomingMultiplo.asset",
        "Assets/Evolucoes/HomingEterno.asset",
    };

    [MenuItem("Tools/Skills/Remover Skills Antigos (Espada Rot., Spirit Ball, Espiral, Homing)")]
    public static void Remover()
    {
        int removidos = 0;

        // Remove do SkillManager
        var managers = Object.FindObjectsByType<SkillManager>(FindObjectsSortMode.None);
        foreach (var sm in managers)
        {
            int antes = sm.availableSkills.Count;
            sm.availableSkills.RemoveAll(s =>
            {
                if (s == null) return true;
                string nome = s.skillName.ToLower();
                foreach (var n in nomesParaRemover)
                    if (nome.Contains(n)) return true;
                // Remove também por tipo orbital/projectile antigo
                if (s.specificType == SpecificSkillType.EscudoRotativo ||
                    s.isOrbitalProjectile) return true;
                return false;
            });
            int depois = sm.availableSkills.Count;
            removidos += antes - depois;
            if (antes != depois) EditorUtility.SetDirty(sm);
        }

        // Remove do GerenciadorEventos.todasEvolucoes
        var ge = Object.FindFirstObjectByType<GerenciadorEventos>();
        if (ge != null)
        {
            int antesEvo = ge.todasEvolucoes.Count;
            ge.todasEvolucoes.RemoveAll(e =>
            {
                if (e == null) return true;
                return e.skillAlvo == SpecificSkillType.EscudoRotativo ||
                       e.skillAlvo == SpecificSkillType.Boomerang ||
                       (e.skillAlvo == SpecificSkillType.Projectile &&
                        (e.tipoEvolucao == SkillEvolutionType.SpiritBallDupla    ||
                         e.tipoEvolucao == SkillEvolutionType.SpiritBallExplosiva||
                         e.tipoEvolucao == SkillEvolutionType.EspiralDupla       ||
                         e.tipoEvolucao == SkillEvolutionType.EspiralFuracao));
            });
            if (ge.todasEvolucoes.Count != antesEvo) EditorUtility.SetDirty(ge);
        }

        // Deleta assets de evolução
        foreach (var path in evolucoesParaRemover)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
                Debug.Log($"🗑 Deletado: {path}");
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ {removidos} skill(s) removida(s) do SkillManager. Evoluções relacionadas também removidas.");
    }
}
#endif
