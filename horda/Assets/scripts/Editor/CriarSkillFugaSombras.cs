#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class CriarSkillFugaSombras
{
    const string PATH = "Assets/Skills/FugaSombras.asset";

    [MenuItem("Tools/Skills/Criar Fuga das Sombras")]
    public static void Criar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Skills"))
        { AssetDatabase.CreateFolder("Assets", "Skills"); AssetDatabase.Refresh(); }

        var existente = AssetDatabase.LoadAssetAtPath<SkillData>(PATH);
        if (existente != null) { AdicionarAoSkillManager(existente); Selection.activeObject = existente; return; }

        var skill = ScriptableObject.CreateInstance<SkillData>();
        skill.skillName          = "Fuga das Sombras";
        skill.description        = "Ao receber dano alto, teleporta automaticamente para uma posição segura próxima.";
        skill.specificType       = SpecificSkillType.FugaSombras;
        skill.rarity             = SkillRarity.Rare;
        skill.isPassive          = true;
        skill.specialValue       = 20f;   // 20% do HP — limiar de dano
        skill.cooldown           = 360f;  // 6 minutos
        skill.activationInterval = 5f;    // distância do teleporte
        skill.elementColor       = new Color(0.5f, 0.1f, 0.9f);
        skill.element            = PlayerStats.Element.None;
        skill.isUnique           = true;
        skill.requiredLevel      = 1;
        skill.icon               = CriarIcone();

        AssetDatabase.CreateAsset(skill, PATH);
        AdicionarAoSkillManager(skill);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Selection.activeObject = skill;
        Debug.Log("✅ 'Fuga das Sombras' criada.");
    }

    static void AdicionarAoSkillManager(SkillData skill)
    {
        if (skill.cardPrefab == null)
        {
            var sm2 = Object.FindFirstObjectByType<SkillManager>();
            if (sm2 != null) { var r = sm2.availableSkills.Find(s => s != null && s.cardPrefab != null); if (r != null) { skill.cardPrefab = r.cardPrefab; EditorUtility.SetDirty(skill); } }
        }
        foreach (var sm in Object.FindObjectsByType<SkillManager>(FindObjectsSortMode.None))
        { if (sm.availableSkills.Exists(s => s != null && s.skillName == skill.skillName)) continue; sm.availableSkills.Add(skill); EditorUtility.SetDirty(sm); }
        AssetDatabase.SaveAssets();
    }

    static Sprite CriarIcone()
    {
        const int SZ = 64;
        const string ICON_PATH = "Assets/Skills/FugaSombrasIcon.png";
        var existente = AssetDatabase.LoadAssetAtPath<Sprite>(ICON_PATH);
        if (existente != null) return existente;

        var tex = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false);
        var pixels = new Color[SZ * SZ];
        float cx = SZ * 0.5f;
        Color cor = new Color(0.5f, 0.1f, 0.9f);

        for (int y = 0; y < SZ; y++)
        for (int x = 0; x < SZ; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cx));
            pixels[y * SZ + x] = new Color(0.06f, 0.02f, 0.12f, Mathf.Clamp01(1f - d / (cx * 1.05f)));
        }

        // Silhueta do player + seta
        DrawSilhueta(pixels, SZ, (int)(cx - 8), (int)cx, cor);
        DrawSeta(pixels, SZ, (int)(cx + 8), (int)cx, cor);

        tex.SetPixels(pixels);
        tex.Apply();
        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "../" + ICON_PATH), png);
        AssetDatabase.Refresh();
        var imp = AssetImporter.GetAtPath(ICON_PATH) as TextureImporter;
        if (imp != null) { imp.textureType = TextureImporterType.Sprite; imp.spritePixelsPerUnit = SZ; imp.filterMode = FilterMode.Point; imp.SaveAndReimport(); }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ICON_PATH);
    }

    static void DrawSilhueta(Color[] p, int sz, int cx, int cy, Color cor)
    {
        // Oval (corpo)
        for (int y = cy - 10; y <= cy + 10; y++)
        for (int x = cx - 5; x <= cx + 5; x++)
        {
            if (x < 0 || x >= sz || y < 0 || y >= sz) continue;
            float nx = (x - cx) / 5f; float ny = (y - cy) / 10f;
            if (nx * nx + ny * ny <= 1f)
                p[y * sz + x] = Color.Lerp(p[y * sz + x], cor, 0.8f);
        }
    }

    static void DrawSeta(Color[] p, int sz, int cx, int cy, Color cor)
    {
        for (int i = -8; i <= 8; i++)
        {
            int px = cx + i, py = cy;
            if (px >= 0 && px < sz && py >= 0 && py < sz)
                p[py * sz + px] = Color.Lerp(p[py * sz + px], cor, 0.9f);
        }
        for (int i = 1; i <= 5; i++)
        {
            for (int j = -i; j <= i; j++)
            {
                int px = cx + 8 - i, py = cy + j;
                if (px >= 0 && px < sz && py >= 0 && py < sz)
                    p[py * sz + px] = Color.Lerp(p[py * sz + px], cor, 0.85f);
            }
        }
    }
}
#endif
