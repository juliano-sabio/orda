#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class CriarSkillFuriaLaminas
{
    const string PASTA = "Assets/Skills";
    const string PATH  = "Assets/Skills/FuriaLaminas.asset";

    [MenuItem("Tools/Skills/Criar Fúria de Lâminas")]
    public static void Criar()
    {
        if (!AssetDatabase.IsValidFolder(PASTA))
        { AssetDatabase.CreateFolder("Assets", "Skills"); AssetDatabase.Refresh(); }

        var existente = AssetDatabase.LoadAssetAtPath<SkillData>(PATH);
        if (existente != null)
        {
            if (existente.icon == null) { existente.icon = CriarIcone(); EditorUtility.SetDirty(existente); AssetDatabase.SaveAssets(); }
            AdicionarAoSkillManager(existente);
            Selection.activeObject = existente;
            return;
        }

        var skill = ScriptableObject.CreateInstance<SkillData>();
        skill.icon               = CriarIcone();
        skill.skillName          = "Fúria de Lâminas";
        skill.description        = "Dispara lâminas giratórias nos inimigos mais próximos causando dano puro.";
        skill.specificType       = SpecificSkillType.FuriaLaminas;
        skill.rarity             = SkillRarity.Uncommon;
        skill.isPassive          = true;
        skill.attackBonus        = 30f;
        skill.activationInterval = 2.5f;
        skill.projectileCount    = 5;
        skill.projectileSpeed    = 14f;
        skill.elementColor       = new Color(0.85f, 0.92f, 1f);
        skill.element            = PlayerStats.Element.None;
        skill.isUnique           = true;
        skill.requiredLevel      = 1;

        AssetDatabase.CreateAsset(skill, PATH);
        AdicionarAoSkillManager(skill);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = skill;
        Debug.Log("✅ SkillData 'Fúria de Lâminas' criado em: " + PATH);
    }

    static void AdicionarAoSkillManager(SkillData skill)
    {
        if (skill.cardPrefab == null)
        {
            var sm2 = Object.FindFirstObjectByType<SkillManager>();
            if (sm2 != null)
            {
                var ref_ = sm2.availableSkills.Find(s => s != null && s.cardPrefab != null);
                if (ref_ != null) { skill.cardPrefab = ref_.cardPrefab; EditorUtility.SetDirty(skill); }
            }
        }
        foreach (var sm in Object.FindObjectsByType<SkillManager>(FindObjectsSortMode.None))
        {
            if (sm.availableSkills.Exists(s => s != null && s.skillName == skill.skillName)) continue;
            sm.availableSkills.Add(skill);
            EditorUtility.SetDirty(sm);
        }
        AssetDatabase.SaveAssets();
    }

    static Sprite CriarIcone()
    {
        const int SZ = 64;
        const string ICON_PATH = "Assets/Skills/FuriaLaminasIcon.png";

        var existente = AssetDatabase.LoadAssetAtPath<Sprite>(ICON_PATH);
        if (existente != null) return existente;

        var tex    = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false);
        var pixels = new Color[SZ * SZ];
        float cx = SZ * 0.5f;

        // Fundo escuro azulado
        for (int y = 0; y < SZ; y++)
        for (int x = 0; x < SZ; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cx));
            float a = Mathf.Clamp01(1f - d / (cx * 1.05f));
            pixels[y * SZ + x] = new Color(0.05f, 0.07f, 0.18f, a);
        }

        // 5 lâminas em diferentes ângulos
        float[] angulos = { 0f, 72f, 144f, 216f, 288f };
        foreach (float ang in angulos)
            DrawLamina(pixels, SZ, (int)cx, (int)cx, ang);

        tex.SetPixels(pixels);
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        System.IO.File.WriteAllBytes(
            System.IO.Path.Combine(Application.dataPath, "../" + ICON_PATH), png);
        AssetDatabase.Refresh();

        var importer = AssetImporter.GetAtPath(ICON_PATH) as TextureImporter;
        if (importer != null)
        {
            importer.textureType         = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = SZ;
            importer.filterMode          = FilterMode.Point;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ICON_PATH);
    }

    static void DrawLamina(Color[] pixels, int sz, int cx, int cy, float angDeg)
    {
        float rad = angDeg * Mathf.Deg2Rad;
        Color cor = new Color(0.85f, 0.92f, 1f);
        for (int i = -12; i <= 12; i++)
        {
            float t  = i / 12f;
            float larg = Mathf.Max(0f, 1f - Mathf.Abs(t) * 0.9f);
            int px = Mathf.RoundToInt(cx + Mathf.Cos(rad) * i * 1.1f);
            int py = Mathf.RoundToInt(cy + Mathf.Sin(rad) * i * 1.1f);
            for (int w = -1; w <= 1; w++)
            {
                float wr = w / 2f;
                int nx = Mathf.RoundToInt(px + Mathf.Cos(rad + Mathf.PI * 0.5f) * wr * larg * 2f);
                int ny = Mathf.RoundToInt(py + Mathf.Sin(rad + Mathf.PI * 0.5f) * wr * larg * 2f);
                if (nx < 0 || nx >= sz || ny < 0 || ny >= sz) continue;
                float a = larg * (1f - Mathf.Abs(wr) * 0.6f);
                pixels[ny * sz + nx] = Color.Lerp(pixels[ny * sz + nx], cor, Mathf.Clamp01(a));
            }
        }
    }
}
#endif
