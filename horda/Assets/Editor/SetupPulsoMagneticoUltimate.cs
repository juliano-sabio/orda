using UnityEngine;
using UnityEditor;
using System.IO;

public static class SetupPulsoMagneticoUltimate
{
    const string PASTA = "Assets/prefebs/ultimates";

    [MenuItem("Tools/Setup Pulso Magnético (gerar icone)")]
    static void Setup()
    {
        string iconPath = $"{PASTA}/pulso_magnetico_icon.png";
        GerarIcone(iconPath);
        AssetDatabase.Refresh();
        AssetDatabase.ImportAsset(iconPath);
        ConfigurarImportacao(iconPath);

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        if (sprite == null) { Debug.LogError("Falha ao carregar sprite: " + iconPath); return; }

        var data = AssetDatabase.LoadAssetAtPath<UltimateData>($"{PASTA}/pulso_magnetico.asset");
        if (data == null) { Debug.LogError("UltimateData nao encontrado"); return; }

        data.ultimateIcon = sprite;
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        Debug.Log("Icone do Pulso Magnético criado!");
    }

    static void GerarIcone(string assetPath)
    {
        const int SZ = 32;
        var tex = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false);

        Color bg      = new Color(0.05f, 0.02f, 0.12f, 1f);
        Color purple  = new Color(0.75f, 0.35f, 1f,   1f);
        Color purpleG = new Color(0.9f,  0.6f,  1f,   1f);
        Color yellow  = new Color(1f,    0.9f,  0.3f, 1f);
        Color white   = Color.white;

        for (int y = 0; y < SZ; y++)
            for (int x = 0; x < SZ; x++)
                tex.SetPixel(x, y, bg);

        int cx = 15, cy = 15;

        // Anéis concêntricos (campo magnético)
        int[] raios = { 12, 9, 6 };
        foreach (int R in raios)
        {
            for (int deg = 0; deg < 360; deg += 2)
            {
                float a = deg * Mathf.Deg2Rad;
                int px = Mathf.RoundToInt(cx + R * Mathf.Cos(a));
                int py = Mathf.RoundToInt(cy + R * Mathf.Sin(a));
                float fade = R == 12 ? 0.7f : R == 9 ? 0.85f : 1f;
                Blend(tex, px, py, purple, fade);
            }
        }

        // Raios elétricos (4 direções)
        int[][] raios2 = {
            new[]{cx, cy+1, cx, cy+5},
            new[]{cx, cy-1, cx, cy-5},
            new[]{cx+1, cy, cx+5, cy},
            new[]{cx-1, cy, cx-5, cy},
        };
        foreach (var r in raios2)
        {
            int x0=r[0], y0=r[1], x1=r[2], y1=r[3];
            int steps = Mathf.Max(Mathf.Abs(x1-x0), Mathf.Abs(y1-y0));
            for (int s = 0; s <= steps; s++)
            {
                float t = steps == 0 ? 0 : s / (float)steps;
                int lx = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                int ly = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                // pequeno jitter para parecer raio
                if (s > 0 && s < steps) lx += Random.Range(-1, 2);
                SetPx(tex, lx, ly, yellow);
            }
        }

        // Setas de atração (→ centro)
        int[][] setas = {
            new[]{3, 15}, new[]{27, 15}, new[]{15, 3}, new[]{15, 27},
            new[]{5, 5},  new[]{25, 5},  new[]{5, 25}, new[]{25, 25},
        };
        foreach (var s in setas)
        {
            int sx = s[0], sy = s[1];
            Vector2 dir = new Vector2(cx - sx, cy - sy).normalized;
            Blend(tex, sx, sy, purpleG, 0.9f);
            Blend(tex, Mathf.RoundToInt(sx + dir.x), Mathf.RoundToInt(sy + dir.y), purpleG, 0.6f);
        }

        // Núcleo brilhante
        for (int dx = -2; dx <= 2; dx++)
            for (int dy = -2; dy <= 2; dy++)
            {
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= 2f) Blend(tex, cx + dx, cy + dy, purpleG, 1f - d * 0.3f);
            }
        SetPx(tex, cx, cy, white);

        tex.Apply();
        string abs = Application.dataPath + assetPath.Substring("Assets".Length);
        File.WriteAllBytes(abs, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    static void ConfigurarImportacao(string assetPath)
    {
        var ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (ti == null) return;
        ti.textureType         = TextureImporterType.Sprite;
        ti.spriteImportMode    = SpriteImportMode.Single;
        ti.filterMode          = FilterMode.Point;
        ti.spritePixelsPerUnit = 32;
        ti.alphaIsTransparency = true;
        ti.SaveAndReimport();
    }

    static void SetPx(Texture2D t, int x, int y, Color c)
    {
        if (x >= 0 && x < t.width && y >= 0 && y < t.height) t.SetPixel(x, y, c);
    }

    static void Blend(Texture2D t, int x, int y, Color c, float alpha)
    {
        if (x < 0 || x >= t.width || y < 0 || y >= t.height) return;
        t.SetPixel(x, y, Color.Lerp(t.GetPixel(x, y), c, Mathf.Clamp01(alpha)));
    }
}
