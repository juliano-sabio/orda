#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class CriarSkillChuvaEstrelas
{
    const string PASTA = "Assets/Skills";
    const string PATH  = "Assets/Skills/ChuvaEstrelas.asset";

    [MenuItem("Tools/Skills/Criar Chuva de Estrelas")]
    public static void Criar()
    {
        if (!AssetDatabase.IsValidFolder(PASTA))
        {
            AssetDatabase.CreateFolder("Assets", "Skills");
            AssetDatabase.Refresh();
        }

        var existente = AssetDatabase.LoadAssetAtPath<SkillData>(PATH);
        if (existente != null)
        {
            if (existente.icon == null)
            {
                existente.icon = CriarIcone();
                EditorUtility.SetDirty(existente);
                AssetDatabase.SaveAssets();
            }
            AdicionarAoSkillManager(existente);
            Selection.activeObject = existente;
            return;
        }

        var skill = ScriptableObject.CreateInstance<SkillData>();

        skill.icon               = CriarIcone();
        skill.skillName          = "Chuva de Estrelas";
        skill.description        = "Estrelas caem do céu sobre os inimigos mais próximos causando dano em área.";
        skill.specificType       = SpecificSkillType.ChuvaEstrelas;
        skill.rarity             = SkillRarity.Rare;
        skill.isPassive          = true;
        skill.attackBonus        = 20f;
        skill.specialValue       = 1.5f;
        skill.activationInterval = 3f;
        skill.projectileCount    = 3;
        skill.elementColor       = new Color(1f, 0.9f, 0.2f);
        skill.element            = PlayerStats.Element.None;
        skill.isUnique           = true;
        skill.requiredLevel      = 1;

        AssetDatabase.CreateAsset(skill, PATH);
        AdicionarAoSkillManager(skill);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = skill;
        Debug.Log("✅ SkillData 'Chuva de Estrelas' criado em: " + PATH);
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

        var managers = Object.FindObjectsByType<SkillManager>(FindObjectsSortMode.None);
        foreach (var sm in managers)
        {
            if (sm.availableSkills.Exists(s => s != null && s.skillName == skill.skillName)) continue;
            sm.availableSkills.Add(skill);
            EditorUtility.SetDirty(sm);
            Debug.Log($"✅ '{skill.skillName}' adicionada ao SkillManager.");
        }
        AssetDatabase.SaveAssets();
    }

    static Sprite CriarIcone()
    {
        const int SZ = 64;
        const string ICON_PATH = "Assets/Skills/ChuvaEstrelasIcon.png";

        var existente = AssetDatabase.LoadAssetAtPath<Sprite>(ICON_PATH);
        if (existente != null) return existente;

        var tex    = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false);
        var pixels = new Color[SZ * SZ];
        float cx = SZ * 0.5f;

        // Fundo — gradiente escuro azul noturno
        for (int y = 0; y < SZ; y++)
        for (int x = 0; x < SZ; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cx));
            float a = Mathf.Clamp01(1f - d / (cx * 1.05f));
            pixels[y * SZ + x] = new Color(0.05f, 0.05f, 0.18f, a);
        }

        // Estrelas caindo
        DrawStar(pixels, SZ, 20, 48, 7);
        DrawStar(pixels, SZ, 32, 38, 9);
        DrawStar(pixels, SZ, 44, 52, 6);

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

    static void DrawStar(Color[] pixels, int sz, int cx, int cy, int r)
    {
        for (int y = cy - r; y <= cy + r; y++)
        for (int x = cx - r; x <= cx + r; x++)
        {
            if (x < 0 || x >= sz || y < 0 || y >= sz) continue;
            float dx = Mathf.Abs(x - cx) / (float)r;
            float dy = Mathf.Abs(y - cy) / (float)r;
            float v  = Mathf.Max(0f, 1f - (dx * dx + dy * dy) * 1.5f)
                     + Mathf.Max(0f, 1f - Mathf.Max(dx, dy) * 3.5f);
            float a  = Mathf.Clamp01(v);
            if (a > 0)
                pixels[y * sz + x] = Color.Lerp(pixels[y * sz + x],
                    new Color(1f, 0.95f, 0.35f), a);
        }
        // rastro
        for (int i = 1; i <= r + 2; i++)
        {
            int tx = cx - i, ty = cy + i;
            if (tx < 0 || tx >= sz || ty < 0 || ty >= sz) continue;
            float a = Mathf.Lerp(0.8f, 0f, i / (float)(r + 2));
            pixels[ty * sz + tx] = Color.Lerp(pixels[ty * sz + tx],
                new Color(1f, 0.8f, 0.2f), a);
        }
    }
}
#endif
