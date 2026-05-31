#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class CriarSkillSegundaChance
{
    const string PASTA = "Assets/Skills";
    const string PATH  = "Assets/Skills/SegundaChance.asset";

    [MenuItem("Tools/Skills/Criar Segunda Chance")]
    public static void Criar()
    {
        if (!AssetDatabase.IsValidFolder(PASTA))
        { AssetDatabase.CreateFolder("Assets", "Skills"); AssetDatabase.Refresh(); }

        var existente = AssetDatabase.LoadAssetAtPath<SkillData>(PATH);
        if (existente != null)
        {
            AdicionarAoSkillManager(existente);
            Selection.activeObject = existente;
            Debug.Log("✅ Segunda Chance já existe.");
            return;
        }

        var skill = ScriptableObject.CreateInstance<SkillData>();
        skill.icon               = CriarIcone();
        skill.skillName          = "Segunda Chance";
        skill.description        = "Ao morrer, revive automaticamente com 30% do HP. Funciona uma vez por partida.";
        skill.specificType       = SpecificSkillType.SegundaChance;
        skill.rarity             = SkillRarity.Epic;
        skill.isPassive          = true;
        skill.specialValue       = 30f; // 30% do HP máximo
        skill.elementColor       = new Color(1f, 0.85f, 0.1f);
        skill.element            = PlayerStats.Element.None;
        skill.isUnique           = true;
        skill.requiredLevel      = 1;

        AssetDatabase.CreateAsset(skill, PATH);
        AdicionarAoSkillManager(skill);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = skill;
        Debug.Log("✅ 'Segunda Chance' criada em: " + PATH);
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
        const string ICON_PATH = "Assets/Skills/SegundaChanceIcon.png";

        var existente = AssetDatabase.LoadAssetAtPath<Sprite>(ICON_PATH);
        if (existente != null) return existente;

        var tex    = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false);
        var pixels = new Color[SZ * SZ];
        float cx = SZ * 0.5f;
        Color cor = new Color(1f, 0.85f, 0.1f);

        // Fundo escuro dourado
        for (int y = 0; y < SZ; y++)
        for (int x = 0; x < SZ; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cx));
            float a = Mathf.Clamp01(1f - d / (cx * 1.05f));
            pixels[y * SZ + x] = new Color(0.12f, 0.09f, 0.02f, a);
        }

        // Coração dourado
        for (int y = 0; y < SZ; y++)
        for (int x = 0; x < SZ; x++)
        {
            float nx = (x - cx) / cx;
            float ny = (y - cx * 0.9f) / cx;
            // Equação do coração
            float heart = Mathf.Pow(nx * nx + ny * ny - 0.5f, 3f)
                         - nx * nx * ny * ny * ny;
            if (heart <= 0f)
            {
                float dist = Mathf.Sqrt(nx * nx + ny * ny);
                float brilho = Mathf.Clamp01(1f - dist * 1.2f);
                Color c = Color.Lerp(cor, Color.white, brilho * 0.4f);
                pixels[y * SZ + x] = Color.Lerp(pixels[y * SZ + x], c, Mathf.Clamp01(0.8f + brilho * 0.2f));
            }
        }

        // Seta de reviver (↑) no centro do coração
        for (int i = 0; i < 14; i++)
        {
            int px = (int)cx, py = (int)(cx * 0.75f) + i;
            if (px >= 0 && px < SZ && py >= 0 && py < SZ)
                pixels[py * SZ + px] = Color.white;
            if (i < 6)
            {
                // Ponta da seta
                for (int w = -i; w <= i; w++)
                {
                    int nx2 = px + w, ny2 = (int)(cx * 0.75f) + 13 - i;
                    if (nx2 >= 0 && nx2 < SZ && ny2 >= 0 && ny2 < SZ)
                        pixels[ny2 * SZ + nx2] = Color.white;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        System.IO.File.WriteAllBytes(
            System.IO.Path.Combine(Application.dataPath, "../" + ICON_PATH), png);
        AssetDatabase.Refresh();

        var imp = AssetImporter.GetAtPath(ICON_PATH) as TextureImporter;
        if (imp != null) { imp.textureType = TextureImporterType.Sprite; imp.spritePixelsPerUnit = SZ; imp.filterMode = FilterMode.Point; imp.SaveAndReimport(); }

        return AssetDatabase.LoadAssetAtPath<Sprite>(ICON_PATH);
    }
}
#endif
