using UnityEngine;
using UnityEditor;
using System.IO;

public static class SetupDomoUltimate
{
    const string PASTA = "Assets/prefebs/ultimates";

    [MenuItem("Tools/Setup Domo Ultimate (gerar icone)")]
    static void Setup()
    {
        string iconPath = $"{PASTA}/domo_icon.png";
        GerarEScalvarIcone(iconPath);

        AssetDatabase.Refresh();
        AssetDatabase.ImportAsset(iconPath);

        ConfigurarImportacao(iconPath);

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        if (sprite == null)
        {
            Debug.LogError("Falha ao carregar sprite: " + iconPath);
            return;
        }

        string assetPath = $"{PASTA}/domo_retardante.asset";
        var ultimateData = AssetDatabase.LoadAssetAtPath<UltimateData>(assetPath);
        if (ultimateData == null)
        {
            Debug.LogError("UltimateData nao encontrado: " + assetPath);
            return;
        }

        ultimateData.ultimateIcon = sprite;
        EditorUtility.SetDirty(ultimateData);
        AssetDatabase.SaveAssets();

        Debug.Log("Icone do Domo criado e atribuido com sucesso!");
    }

    static void GerarEScalvarIcone(string assetPath)
    {
        const int SZ = 32;
        var tex = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false);

        // Cores
        Color bg        = new Color(10/255f,  10/255f,  26/255f,  1f);
        Color cyan      = new Color(0/255f,   229/255f, 255/255f, 1f);
        Color cyanGlow  = new Color(77/255f,  252/255f, 255/255f, 1f);
        Color cyanBase  = new Color(0/255f,   176/255f, 204/255f, 1f);
        Color cyanFill  = new Color(15/255f,  25/255f,  55/255f,  1f);
        Color purple    = new Color(200/255f, 130/255f, 255/255f, 1f);
        Color white     = Color.white;
        Color slow      = new Color(80/255f,  100/255f, 130/255f, 1f);
        Color enemy     = new Color(255/255f, 100/255f, 60/255f,  1f);

        // fundo
        for (int y = 0; y < SZ; y++)
            for (int x = 0; x < SZ; x++)
                tex.SetPixel(x, y, bg);

        int cx = 15, cy = 22, R = 11;

        // preenchimento interior do domo
        for (int y = 0; y <= cy; y++)
        {
            for (int x = 0; x < SZ; x++)
            {
                float dx = x - cx, dy = y - cy;
                float dist = Mathf.Sqrt(dx*dx + dy*dy);
                if (dist <= R)
                {
                    float t = 1f - dist / R;
                    Blend(tex, x, y, cyanFill, 0.25f + t * 0.18f);
                }
            }
        }

        // arco do domo (2px de espessura)
        for (int deg = 0; deg <= 180; deg++)
        {
            float a  = deg * Mathf.Deg2Rad;
            float px = cx + R       * Mathf.Cos(a);
            float py = cy - R       * Mathf.Sin(a);
            float qx = cx + (R - 1) * Mathf.Cos(a);
            float qy = cy - (R - 1) * Mathf.Sin(a);
            SetIfTop(tex, Mathf.RoundToInt(px), Mathf.RoundToInt(py), cy, cyan);
            SetIfTop(tex, Mathf.RoundToInt(qx), Mathf.RoundToInt(qy), cy, cyanGlow);
        }

        // glow interno do arco
        for (int deg = 6; deg < 175; deg += 4)
        {
            float a  = deg * Mathf.Deg2Rad;
            float px = cx + (R - 2) * Mathf.Cos(a);
            float py = cy - (R - 2) * Mathf.Sin(a);
            BlendIfTop(tex, Mathf.RoundToInt(px), Mathf.RoundToInt(py), cy, cyanGlow, 0.45f);
        }

        // linha de base
        for (int x = cx - R; x <= cx + R; x++)
        {
            SetPx(tex, x, cy,   cyanBase);
            SetPx(tex, x, cy+1, cyanBase);
        }
        SetPx(tex, cx - R, cy, cyan);
        SetPx(tex, cx + R, cy, cyan);

        // orbe lentificado dentro do domo
        int ox = 15, oy = 15;

        // glow externo (raio 3)
        for (int deg = 0; deg < 360; deg += 18)
        {
            float a = deg * Mathf.Deg2Rad;
            BlendIfTop(tex, Mathf.RoundToInt(ox + 3*Mathf.Cos(a)),
                            Mathf.RoundToInt(oy + 3*Mathf.Sin(a)), cy + 1, purple, 0.40f);
        }
        // anel médio (raio 2)
        for (int ddx = -2; ddx <= 2; ddx++)
            for (int ddy = -2; ddy <= 2; ddy++)
            {
                float d = Mathf.Sqrt(ddx*ddx + ddy*ddy);
                if (d > 1.3f && d < 2.6f)
                    Blend(tex, ox+ddx, oy+ddy, purple, 0.85f);
            }
        // núcleo
        for (int ddx = -1; ddx <= 1; ddx++)
            for (int ddy = -1; ddy <= 1; ddy++)
                if (Mathf.Abs(ddx) + Mathf.Abs(ddy) <= 1)
                    Blend(tex, ox+ddx, oy+ddy, purple, 1f);
        SetPx(tex, ox, oy, white);

        // linhas de motion blur (mostrando lentidão)
        for (int i = 0; i < 3; i++)
        {
            float a = (i * 120f + 60f) * Mathf.Deg2Rad;
            for (int dr = 3; dr < 6; dr++)
            {
                int lx = Mathf.RoundToInt(ox + dr * Mathf.Cos(a));
                int ly = Mathf.RoundToInt(oy + dr * Mathf.Sin(a));
                float alpha = Mathf.Max(0f, (160f - dr * 42f) / 255f);
                Blend(tex, lx, ly, slow, alpha);
            }
        }

        // projétil inimigo fora do domo (canto superior direito)
        int ex = 26, ey = 27; // y em textura = SZ-1-y_screen
        // converto: tela y=5 → textura y = SZ-1-5 = 26
        for (int i = 0; i < 3; i++)
            Blend(tex, ex - i, ey - i, enemy, Mathf.Max(0.3f, 1f - i * 0.28f));
        Blend(tex, ex+1, ey+1, enemy, 0.5f);
        Blend(tex, ex+2, ey+2, enemy, 0.28f);

        tex.Apply();

        string absPath = Application.dataPath + assetPath.Substring("Assets".Length);
        File.WriteAllBytes(absPath, tex.EncodeToPNG());
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

    // ── Helpers ─────────────────────────────────────────────────────────

    static void SetPx(Texture2D tex, int x, int y, Color c)
    {
        if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
            tex.SetPixel(x, y, c);
    }

    static void SetIfTop(Texture2D tex, int x, int y, int maxY, Color c)
    {
        if (y <= maxY) SetPx(tex, x, y, c);
    }

    static void Blend(Texture2D tex, int x, int y, Color c, float t)
    {
        if (x < 0 || x >= tex.width || y < 0 || y >= tex.height) return;
        Color base_ = tex.GetPixel(x, y);
        tex.SetPixel(x, y, Color.Lerp(base_, c, Mathf.Clamp01(t)));
    }

    static void BlendIfTop(Texture2D tex, int x, int y, int maxY, Color c, float t)
    {
        if (y <= maxY) Blend(tex, x, y, c, t);
    }
}
