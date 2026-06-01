#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class CriarSkillBarreiraEnergia
{
    const string PATH = "Assets/Skills/BarreiraEnergia.asset";

    [MenuItem("Tools/Skills/Criar Barreira de Energia")]
    public static void Criar()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Skills"))
        { AssetDatabase.CreateFolder("Assets", "Skills"); AssetDatabase.Refresh(); }

        var existente = AssetDatabase.LoadAssetAtPath<SkillData>(PATH);
        if (existente != null) { AdicionarAoSkillManager(existente); Selection.activeObject = existente; return; }

        var skill = ScriptableObject.CreateInstance<SkillData>();
        skill.skillName          = "Barreira de Energia";
        skill.description        = "Cria um escudo de energia com HP próprio que absorve dano antes da vida real. Se quebrar, regenera após 12s.";
        skill.specificType       = SpecificSkillType.BarreiraEnergia;
        skill.rarity             = SkillRarity.Rare;
        skill.isPassive          = true;
        skill.healthBonus        = 150f;   // HP do escudo
        skill.cooldown           = 12f;    // tempo de recarga após quebrar
        skill.elementColor       = new Color(0.2f, 0.6f, 1f);
        skill.element            = PlayerStats.Element.None;
        skill.isUnique           = true;
        skill.requiredLevel      = 1;
        skill.icon               = CriarIcone();

        AssetDatabase.CreateAsset(skill, PATH);
        AdicionarAoSkillManager(skill);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Selection.activeObject = skill;
        Debug.Log("✅ 'Barreira de Energia' criada.");
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
        const string ICON_PATH = "Assets/Skills/BarreiraEnergiaIcon.png";
        var existente = AssetDatabase.LoadAssetAtPath<Sprite>(ICON_PATH);
        if (existente != null) return existente;

        var tex = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false);
        var pixels = new Color[SZ * SZ];
        float cx = SZ * 0.5f;
        Color cor = new Color(0.2f, 0.6f, 1f);

        // Fundo escuro azul
        for (int y = 0; y < SZ; y++)
        for (int x = 0; x < SZ; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cx));
            pixels[y * SZ + x] = new Color(0.03f, 0.06f, 0.15f, Mathf.Clamp01(1f - d / (cx * 1.05f)));
        }

        // 3 anéis concêntricos azuis
        float[] raios = { cx * 0.4f, cx * 0.62f, cx * 0.82f };
        foreach (float r in raios)
        {
            int segs = 48;
            for (int i = 0; i < segs; i++)
            {
                float ang = i / (float)segs * Mathf.PI * 2f;
                int px = Mathf.RoundToInt(cx + Mathf.Cos(ang) * r);
                int py = Mathf.RoundToInt(cx + Mathf.Sin(ang) * r);
                for (int dw = -2; dw <= 2; dw++)
                for (int dh = -2; dh <= 2; dh++)
                {
                    int nx = px + dw, ny = py + dh;
                    if (nx < 0 || nx >= SZ || ny < 0 || ny >= SZ) continue;
                    float a = Mathf.Clamp01(1.5f - Mathf.Sqrt(dw * dw + dh * dh)) * (1f - r / (cx * 1.1f) * 0.3f);
                    pixels[ny * SZ + nx] = Color.Lerp(pixels[ny * SZ + nx], cor, a);
                }
            }
        }

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
}
#endif
