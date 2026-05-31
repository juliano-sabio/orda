#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class CriarSkillGarrasAbismo
{
    const string PASTA = "Assets/Skills";
    const string PATH  = "Assets/Skills/GarrasAbismo.asset";

    [MenuItem("Tools/Skills/Criar Garras do Abismo")]
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
        skill.skillName          = "Garras do Abismo";
        skill.description        = "Garras surgem do chão nos inimigos mais próximos, causando dano e prendendo por 1s.";
        skill.specificType       = SpecificSkillType.GarrasAbismo;
        skill.rarity             = SkillRarity.Rare;
        skill.isPassive          = true;
        skill.attackBonus        = 25f;
        skill.activationInterval = 4f;
        skill.projectileCount    = 2;
        skill.duration           = 1.2f;
        skill.elementColor       = new Color(0.45f, 0.1f, 0.7f);
        skill.element            = PlayerStats.Element.None;
        skill.isUnique           = true;
        skill.requiredLevel      = 1;

        AssetDatabase.CreateAsset(skill, PATH);
        AdicionarAoSkillManager(skill);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = skill;
        Debug.Log("✅ SkillData 'Garras do Abismo' criado em: " + PATH);
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
        const string ICON_PATH = "Assets/Skills/GarrasAbismoIcon.png";

        var existente = AssetDatabase.LoadAssetAtPath<Sprite>(ICON_PATH);
        if (existente != null) return existente;

        var tex    = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false);
        var pixels = new Color[SZ * SZ];
        float cx = SZ * 0.5f;

        // Fundo circular escuro roxo
        for (int y = 0; y < SZ; y++)
        for (int x = 0; x < SZ; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cx));
            float a = Mathf.Clamp01(1f - d / (cx * 1.05f));
            pixels[y * SZ + x] = new Color(0.08f, 0.02f, 0.14f, a);
        }

        // 4 garras curvas
        DrawGarra(pixels, SZ, 32, 18, 0f);
        DrawGarra(pixels, SZ, 32, 18, 90f);
        DrawGarra(pixels, SZ, 32, 18, 180f);
        DrawGarra(pixels, SZ, 32, 18, 270f);

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

    static void DrawGarra(Color[] pixels, int sz, int cx, int cy, float angBase)
    {
        Color cor = new Color(0.65f, 0.3f, 1f);
        float rad = angBase * Mathf.Deg2Rad;
        // Linha base → ponta
        for (int i = 0; i <= 14; i++)
        {
            float t  = i / 14f;
            float ox = Mathf.Cos(rad + t * 0.4f) * t * 12f;
            float oy = Mathf.Sin(rad + t * 0.4f) * t * 12f + t * 8f;
            int px = Mathf.RoundToInt(cx + ox);
            int py = Mathf.RoundToInt(cy + oy);
            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int nx = px + dx, ny = py + dy;
                if (nx < 0 || nx >= sz || ny < 0 || ny >= sz) continue;
                float a = Mathf.Lerp(1f, 0.3f, t) * Mathf.Clamp01(1.2f - Mathf.Sqrt(dx * dx + dy * dy));
                pixels[ny * sz + nx] = Color.Lerp(pixels[ny * sz + nx], cor, a);
            }
        }
    }
}
#endif
