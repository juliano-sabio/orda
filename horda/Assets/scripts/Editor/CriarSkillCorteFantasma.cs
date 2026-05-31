#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class CriarSkillCorteFantasma
{
    const string PASTA = "Assets/Skills";
    const string PATH  = "Assets/Skills/CorteFantasma.asset";

    [MenuItem("Tools/Skills/Criar Corte Fantasma")]
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
        skill.skillName          = "Corte Fantasma";
        skill.description        = "Cortes fantasmagóricos rasgam em direção aos inimigos mais próximos causando dano puro.";
        skill.specificType       = SpecificSkillType.CorteFantasma;
        skill.rarity             = SkillRarity.Rare;
        skill.isPassive          = true;
        skill.attackBonus        = 35f;
        skill.activationInterval = 2.5f;
        skill.projectileCount    = 2;
        skill.projectileSpeed    = 18f;
        skill.elementColor       = new Color(0.7f, 0.9f, 1f);
        skill.element            = PlayerStats.Element.None;
        skill.isUnique           = true;
        skill.requiredLevel      = 1;

        AssetDatabase.CreateAsset(skill, PATH);
        AdicionarAoSkillManager(skill);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = skill;
        Debug.Log("✅ 'Corte Fantasma' criado em: " + PATH);
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
        const string ICON_PATH = "Assets/Skills/CorteFantasmaIcon.png";

        var existente = AssetDatabase.LoadAssetAtPath<Sprite>(ICON_PATH);
        if (existente != null) return existente;

        var tex    = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false);
        var pixels = new Color[SZ * SZ];
        float cx = SZ * 0.5f;

        // Fundo escuro azul-cinza
        for (int y = 0; y < SZ; y++)
        for (int x = 0; x < SZ; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cx));
            float a = Mathf.Clamp01(1f - d / (cx * 1.05f));
            pixels[y * SZ + x] = new Color(0.05f, 0.08f, 0.15f, a);
        }

        Color cor = new Color(0.7f, 0.92f, 1f);

        // 2 cortes diagonais cruzados
        DrawCorteIcone(pixels, SZ, 10, 10, 54, 54, cor, 3);
        DrawCorteIcone(pixels, SZ, 54, 10, 10, 54, new Color(0.5f, 0.8f, 1f, 0.7f), 2);

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

    static void DrawCorteIcone(Color[] pixels, int sz, int x0, int y0, int x1, int y1, Color cor, int larg)
    {
        int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
        for (int i = 0; i <= steps; i++)
        {
            float t  = i / (float)steps;
            int   px = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
            int   py = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
            float a  = Mathf.Sin(t * Mathf.PI) * cor.a;
            for (int w = -larg; w <= larg; w++)
            for (int h2 = -larg; h2 <= larg; h2++)
            {
                int nx = px + w, ny = py + h2;
                if (nx < 0 || nx >= sz || ny < 0 || ny >= sz) continue;
                float wa = a * Mathf.Clamp01(1f - (Mathf.Abs(w) + Mathf.Abs(h2)) / (float)(larg + 1));
                pixels[ny * sz + nx] = Color.Lerp(pixels[ny * sz + nx], cor, wa);
            }
        }
    }
}
#endif
