using UnityEditor;
using UnityEngine;
using System.IO;

[InitializeOnLoad]
public static class GerarIconeFormaBestial
{
    const string ICON_PATH = "Assets/prefebs/ultimates/forma_bestial_icon.png";
    const string ASSET_PATH = "Assets/prefebs/ultimates/forma_bestial.asset";

    static GerarIconeFormaBestial()
    {
        EditorApplication.delayCall += Executar;
    }

    static void Executar()
    {
        if (File.Exists(Application.dataPath + "/../" + ICON_PATH))
        {
            VincularIcone();
            return;
        }

        var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);

        Color BG   = Hex("#0d0d1a");
        Color AURA = Hex("#7a1c00");
        Color AUR2 = Hex("#8c2800");
        Color BODY = Hex("#c84808");
        Color HIGH = Hex("#f07020");
        Color EYES = Hex("#ffcc00");
        Color EYE2 = Hex("#c89600");
        Color SHAD = Hex("#321000");
        Color CLAW = Hex("#ff5a08");
        Color NOSE = Hex("#461400");

        // Fundo
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
                tex.SetPixel(x, y, BG);

        float cx = 15f, cy = 15f;

        // Aura externa
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
                if (Elipse(x, y, cx, cy, 10.5f, 9.5f))
                    tex.SetPixel(x, y, AURA);

        // Aura interna
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
                if (Elipse(x, y, cx, cy, 8.2f, 7.2f))
                    tex.SetPixel(x, y, AUR2);

        // Cabeça
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
                if (Elipse(x, y, cx, 14f, 7f, 6f))
                    tex.SetPixel(x, y, BODY);

        // Highlight da bochecha
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
                if (Elipse(x, y, 13f, 12f, 3.5f, 2.5f) && tex.GetPixel(x, y) == BODY)
                    tex.SetPixel(x, y, HIGH);

        // Orelhas esquerda
        int[][] earL = { new[]{9,7}, new[]{10,6}, new[]{11,5}, new[]{10,7}, new[]{11,6}, new[]{11,7} };
        // Orelhas direita
        int[][] earR = { new[]{21,7}, new[]{20,6}, new[]{19,5}, new[]{20,7}, new[]{19,6}, new[]{19,7} };
        foreach (var e in earL) tex.SetPixel(e[0], e[1], BODY);
        foreach (var e in earR) tex.SetPixel(e[0], e[1], BODY);
        // Ponta das orelhas
        tex.SetPixel(10, 6, HIGH); tex.SetPixel(20, 6, HIGH);
        tex.SetPixel(11, 5, HIGH); tex.SetPixel(19, 5, HIGH);

        // Olhos (2x2)
        for (int dy = 0; dy < 2; dy++)
            for (int dx = 0; dx < 2; dx++)
            {
                tex.SetPixel(11 + dx, 13 + dy, EYES);
                tex.SetPixel(18 + dx, 13 + dy, EYES);
            }
        // Brilho ao redor dos olhos
        int[][] eyeGlow = new int[][] { new int[]{11,12},new int[]{12,12},new int[]{13,13},new int[]{18,12},new int[]{19,12},new int[]{17,13} };
        foreach (var pt in eyeGlow)
        {
            Color c = tex.GetPixel(pt[0], pt[1]);
            if (c == BODY || c == HIGH) tex.SetPixel(pt[0], pt[1], EYE2);
        }

        // Nariz
        tex.SetPixel(14, 17, NOSE); tex.SetPixel(15, 17, NOSE); tex.SetPixel(16, 17, NOSE);
        tex.SetPixel(15, 18, NOSE);

        // Focinho highlight
        for (int x = 13; x <= 17; x++)
        {
            Color c = tex.GetPixel(x, 16);
            if (c == BODY || c == NOSE) tex.SetPixel(x, 16, HIGH);
        }

        // Sombra no chão
        for (int x = 9; x < 23; x++)
        {
            Color c = tex.GetPixel(x, 22);
            if (c != BG) tex.SetPixel(x, 22, SHAD);
        }

        // Marcas de garra nos cantos (3 linhas diagonais)
        int[][] clawTL = new int[][] { new int[]{1,6},new int[]{2,5},new int[]{3,4}, new int[]{2,7},new int[]{3,6},new int[]{4,5}, new int[]{3,7},new int[]{4,6},new int[]{5,5} };
        int[][] clawTR = new int[][] { new int[]{30,6},new int[]{29,5},new int[]{28,4}, new int[]{29,7},new int[]{28,6},new int[]{27,5}, new int[]{28,7},new int[]{27,6},new int[]{26,5} };
        int[][] clawBL = new int[][] { new int[]{1,25},new int[]{2,26},new int[]{3,27}, new int[]{2,24},new int[]{3,25},new int[]{4,26}, new int[]{3,24},new int[]{4,25},new int[]{5,26} };
        int[][] clawBR = new int[][] { new int[]{30,25},new int[]{29,26},new int[]{28,27}, new int[]{29,24},new int[]{28,25},new int[]{27,26}, new int[]{28,24},new int[]{27,25},new int[]{26,26} };
        int[][][] allClaws = new int[][][] { clawTL, clawTR, clawBL, clawBR };
        foreach (var g in allClaws)
            foreach (var pt in g)
                if (pt[0] >= 0 && pt[0] < 32 && pt[1] >= 0 && pt[1] < 32)
                    tex.SetPixel(pt[0], pt[1], CLAW);

        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);

        string fullPath = Path.Combine(Application.dataPath, "../" + ICON_PATH);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        File.WriteAllBytes(fullPath, png);

        AssetDatabase.ImportAsset(ICON_PATH, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();

        Debug.Log("[FormaBestial] Ícone gerado em " + ICON_PATH);
        EditorApplication.delayCall += VincularIcone;
    }

    static void VincularIcone()
    {
        var asset = AssetDatabase.LoadAssetAtPath<UltimateData>(ASSET_PATH);
        if (asset == null) { Debug.LogWarning("[FormaBestial] Asset não encontrado: " + ASSET_PATH); return; }
        if (asset.ultimateIcon != null) return;

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ICON_PATH);
        if (sprite == null)
        {
            // Configura importação como sprite
            var ti = AssetImporter.GetAtPath(ICON_PATH) as TextureImporter;
            if (ti != null)
            {
                ti.textureType         = TextureImporterType.Sprite;
                ti.spritePixelsPerUnit = 32f;
                ti.filterMode         = FilterMode.Point;
                ti.textureCompression  = TextureImporterCompression.Uncompressed;
                ti.SaveAndReimport();
            }
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ICON_PATH);
        }

        if (sprite == null) { Debug.LogWarning("[FormaBestial] Sprite não carregado."); return; }

        asset.ultimateIcon = sprite;
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        Debug.Log("[FormaBestial] Ícone vinculado ao asset.");
    }

    static bool Elipse(int x, int y, float cx, float cy, float rx, float ry)
        => ((x - cx) / rx) * ((x - cx) / rx) + ((y - cy) / ry) * ((y - cy) / ry) <= 1f;

    static Color Hex(string h)
    {
        h = h.TrimStart('#');
        float r = System.Convert.ToInt32(h.Substring(0, 2), 16) / 255f;
        float g = System.Convert.ToInt32(h.Substring(2, 2), 16) / 255f;
        float b = System.Convert.ToInt32(h.Substring(4, 2), 16) / 255f;
        return new Color(r, g, b, 1f);
    }
}
