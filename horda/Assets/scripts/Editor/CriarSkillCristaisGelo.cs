#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class CriarSkillCristaisGelo
{
    const string PASTA = "Assets/Skills";

    [MenuItem("Tools/Skills/Criar Cristais de Gelo")]
    public static void Criar()
    {
        if (!AssetDatabase.IsValidFolder(PASTA))
        { AssetDatabase.CreateFolder("Assets", "Skills"); AssetDatabase.Refresh(); }

        string path = $"{PASTA}/CristaisGelo.asset";
        var existente = AssetDatabase.LoadAssetAtPath<SkillData>(path);
        if (existente != null)
        {
            // Já existe — só garante que cardPrefab está atribuído
            if (existente.cardPrefab == null)
            {
                var smRef = Object.FindFirstObjectByType<SkillManager>();
                if (smRef != null)
                {
                    var ref_ = smRef.availableSkills.Find(s => s != null && s.cardPrefab != null);
                    if (ref_ != null) { existente.cardPrefab = ref_.cardPrefab; EditorUtility.SetDirty(existente); AssetDatabase.SaveAssets(); Debug.Log("cardPrefab atribuido ao CristaisGelo!"); }
                }
            }
            Selection.activeObject = existente;
            return;
        }

        var skill = ScriptableObject.CreateInstance<SkillData>();
        skill.skillName          = "Cristais de Gelo";
        skill.description        = "3 cristais orbitam o player e disparam automaticamente nos inimigos proximos, aplicando lentidao.";
        skill.specificType       = SpecificSkillType.CristaisGelo;
        skill.rarity             = SkillRarity.Rare;
        skill.isPassive          = true;
        skill.attackBonus        = 18f;
        skill.activationInterval = 1.2f;
        skill.projectileCount    = 3;
        skill.projectileSpeed    = 14f;
        skill.specialValue       = 10f; // raio de deteccao
        skill.elementColor       = new Color(0.45f, 0.88f, 1f);
        skill.element            = PlayerStats.Element.Ice;
        skill.isUnique           = true;
        skill.requiredLevel      = 1;

        AssetDatabase.CreateAsset(skill, path);

        // Copia cardPrefab de outra skill existente
        var sm2 = Object.FindFirstObjectByType<SkillManager>();
        if (sm2 != null)
        {
            var ref_ = sm2.availableSkills.Find(s => s != null && s.cardPrefab != null);
            if (ref_ != null) skill.cardPrefab = ref_.cardPrefab;
        }

        // Adiciona ao SkillManager na cena
        foreach (var sm in Object.FindObjectsByType<SkillManager>(FindObjectsSortMode.None))
        {
            if (sm.availableSkills.Exists(s => s != null && s.skillName == skill.skillName)) continue;
            sm.availableSkills.Add(skill);
            EditorUtility.SetDirty(sm);
        }

        // Adiciona evolucoes ao CriarEvolucoes
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = skill;
        Debug.Log($"Cristais de Gelo criado em: {path}");
    }
}
#endif
