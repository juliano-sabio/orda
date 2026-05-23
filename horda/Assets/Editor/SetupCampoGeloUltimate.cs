using UnityEngine;
using UnityEditor;
using System.IO;

public static class SetupCampoGeloUltimate
{
    const string PASTA = "Assets/prefebs/ultimates";

    [MenuItem("Tools/Setup Campo de Gelo (gerar icone)")]
    static void Setup()
    {
        string iconPath = $"{PASTA}/campo_gelo_icon.png";
        GerarIcone(iconPath);

        AssetDatabase.Refresh();
        AssetDatabase.ImportAsset(iconPath);
        ConfigurarImportacao(iconPath);

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        if (sprite == null) { Debug.LogError("Falha ao carregar sprite: " + iconPath); return; }

        string assetPath = $"{PASTA}/campo_de_gelo.asset";
        var data = AssetDatabase.LoadAssetAtPath<UltimateData>(assetPath);
        if (data == null) { Debug.LogError("UltimateData nao encontrado: " + assetPath); return; }

        data.ultimateIcon = sprite;
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        Debug.Log("Icone do Campo de Gelo criado e atribuido!");
    }

    static void GerarIcone(string assetPath)
    {
        const int SZ = 32;
        var tex = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false);

        Color bg      = new Color(0.04f, 0.06f, 0.16f, 1f);
        Color iceBlue = new Color(0.55f, 0.88f, 1f,   1f);
        Color iceGlow = new Color(0.80f, 0.96f, 1f,   1f);
        Color white   = Color.white;
        Color dark    = new Color(0.10f, 0.18f, 0.38f, 1f);
        Color crystal = new Color(0.70f, 0.92f, 1f,   1f);

        for (int y = 0; y < SZ; y++)
            for (int x = 0; x < SZ; x++)
                tex.SetPixel(x, y, bg);

        // Anel externo do campo
        int cx = 15, cy = 15, R = 13;
        for (int deg = 0; deg < 360; deg++)
        {
            float a = deg * Mathf.Deg2Rad;
            int px = Mathf.RoundToInt(cx + R * Mathf.Cos(a));
            int py = Mathf.RoundToInt(cy + R * Mathf.Sin(a));
            SetPx(tex, px, py, iceBlue);
            px = Mathf.RoundToInt(cx + (R - 1) * Mathf.Cos(a));
            py = Mathf.RoundToInt(cy + (R - 1) * Mathf.Sin(a));
            Blend(tex, px, py, iceGlow, 0.5f);
        }

        // Preenchimento interior suave
        for (int y = 0; y < SZ; y++)
            for (int x = 0; x < SZ; x++)
            {
                float dx = x - cx, dy = y - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d < R - 1)
                    Blend(tex, x, y, dark, 0.18f + (1f - d / R) * 0.12f);
            }

        // Floco de neve central — 6 braços
        for (int arm = 0; arm < 6; arm++)
        {
            float ang = arm * 60f * Mathf.Deg2Rad;
            for (int step = 1; step <= 5; step++)
            {
                int px = Mathf.RoundToInt(cx + step * Mathf.Cos(ang));
                int py = Mathf.RoundToInt(cy + step * Mathf.Sin(ang));
                float t = 1f - step / 6f;
                Blend(tex, px, py, iceGlow, 0.7f + t * 0.3f);

                // ramificações nos braços maiores
                if (step == 3)
                {
                    float aL = (arm * 60f + 60f) * Mathf.Deg2Rad;
                    float aR = (arm * 60f - 60f) * Mathf.Deg2Rad;
                    Blend(tex, Mathf.RoundToInt(px + Mathf.Cos(aL)),
                               Mathf.RoundToInt(py + Mathf.Sin(aL)), iceBlue, 0.7f);
                    Blend(tex, Mathf.RoundToInt(px + Mathf.Cos(aR)),
                               Mathf.RoundToInt(py + Mathf.Sin(aR)), iceBlue, 0.7f);
                }
            }
        }
        // Centro brilhante
        SetPx(tex, cx, cy, white);
        Blend(tex, cx + 1, cy, crystal, 0.8f);
        Blend(tex, cx - 1, cy, crystal, 0.8f);
        Blend(tex, cx, cy + 1, crystal, 0.8f);
        Blend(tex, cx, cy - 1, crystal, 0.8f);

        // Cristais de gelo nos cantos internos
        int[][] cristais = { new[]{8,8}, new[]{22,8}, new[]{8,22}, new[]{22,22} };
        foreach (var c in cristais)
        {
            SetPx(tex, c[0], c[1], crystal);
            Blend(tex, c[0]+1, c[1], iceBlue, 0.6f);
            Blend(tex, c[0]-1, c[1], iceBlue, 0.6f);
            Blend(tex, c[0], c[1]+1, iceBlue, 0.6f);
            Blend(tex, c[0], c[1]-1, iceBlue, 0.6f);
        }

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
