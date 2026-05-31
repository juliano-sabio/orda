#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class CriarSkillSombrasCruz
{
    const string PASTA = "Assets/Skills";
    const string PATH  = "Assets/Skills/SombrasCruz.asset";

    [MenuItem("Tools/Skills/Criar Sombras em Cruz")]
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
        skill.skillName          = "Sombras em Cruz";
        skill.description        = "4 ondas de energia roxas disparam em + atravessando todos os inimigos no caminho.";
        skill.specificType       = SpecificSkillType.SombrasCruz;
        skill.rarity             = SkillRarity.Rare;
        skill.isPassive          = true;
        skill.attackBonus        = 25f;
        skill.activationInterval = 3f;
        skill.projectileSpeed    = 16f;
        skill.specialValue       = 8f;
        skill.elementColor       = new Color(0.55f, 0.25f, 1f);
        skill.element            = PlayerStats.Element.None;
        skill.isUnique           = true;
        skill.requiredLevel      = 1;

        AssetDatabase.CreateAsset(skill, PATH);
        AdicionarAoSkillManager(skill);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = skill;
        Debug.Log("✅ 'Sombras em Cruz' criado em: " + PATH);
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
        const string ICON_PATH = "Assets/Skills/SombrasCruzIcon.png";

        var existente = AssetDatabase.LoadAssetAtPath<Sprite>(ICON_PATH);
        if (existente != null) return existente;

        var tex    = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false);
        var pixels = new Color[SZ * SZ];
        float cx = SZ * 0.5f;

        // Fundo escuro roxo
        for (int y = 0; y < SZ; y++)
        for (int x = 0; x < SZ; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cx));
            float a = Mathf.Clamp01(1f - d / (cx * 1.05f));
            pixels[y * SZ + x] = new Color(0.1f, 0.04f, 0.2f, a);
        }

        Color cor = new Color(0.65f, 0.35f, 1f);

        // Cruz + horizontal e vertical
        DrawLinhaIcone(pixels, SZ, (int)cx, (int)cx, 1, 0, cor);  // direita
        DrawLinhaIcone(pixels, SZ, (int)cx, (int)cx, -1, 0, cor); // esquerda
        DrawLinhaIcone(pixels, SZ, (int)cx, (int)cx, 0, 1, cor);  // cima
        DrawLinhaIcone(pixels, SZ, (int)cx, (int)cx, 0, -1, cor); // baixo

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

    static void DrawLinhaIcone(Color[] pixels, int sz, int ox, int oy, int dx, int dy, Color cor)
    {
        for (int i = 4; i <= 26; i++)
        {
            int px = ox + dx * i;
            int py = oy + dy * i;
            float fade = Mathf.Lerp(1f, 0f, (i - 4) / 22f);
            for (int w = -2; w <= 2; w++)
            {
                int nx = px + (dy != 0 ? w : 0);
                int ny = py + (dx != 0 ? w : 0);
                if (nx < 0 || nx >= sz || ny < 0 || ny >= sz) continue;
                float a = fade * Mathf.Clamp01(1f - Mathf.Abs(w) * 0.45f);
                pixels[ny * sz + nx] = Color.Lerp(pixels[ny * sz + nx], cor, a);
            }
        }
    }
}
#endif
