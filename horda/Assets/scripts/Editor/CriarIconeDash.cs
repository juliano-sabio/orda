#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

public static class CriarIconeDash
{
    const string ICON_PATH   = "Assets/Skills/DashIcon.png";
    const string PREFAB_PATH = "Assets/prefebs/dash/dash.prefab";
    const string ASE_PATH    = "Assets/prefebs/dash/iconedash.ase";

    [MenuItem("Tools/UI Manager/Gerar e Adicionar Icone Dash")]
    public static void GerarEAdicionar()
    {
        // Tenta primeiro o .ase que o usuário criou
        Sprite sprite = TentarCarregarAse();

        // Fallback: gera proceduralmente
        if (sprite == null) sprite = GerarIconeBota();
        if (sprite == null) { Debug.LogError("❌ Falha ao carregar/gerar ícone."); return; }

        using (var scope = new PrefabUtility.EditPrefabContentsScope(PREFAB_PATH))
        {
            var root = scope.prefabContentsRoot;
            var sr   = root.GetComponent<SpriteRenderer>();
            if (sr == null) sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;

            // Remove luz antiga se existir
            var luzVelha = root.transform.Find("Luz");
            if (luzVelha != null) Object.DestroyImmediate(luzVelha.gameObject);

            // Cria filho "Luz" com Light2D
            var luzGO = new GameObject("Luz");
            luzGO.transform.SetParent(root.transform, false);
            luzGO.transform.localPosition = Vector3.zero;

            var luz = luzGO.AddComponent<Light2D>();
            luz.lightType             = Light2D.LightType.Point;
            luz.color                 = new Color(0.4f, 0.75f, 1f); // azul-ciano
            luz.intensity             = 1.8f;
            luz.pointLightOuterRadius = 2.5f;
            luz.pointLightInnerRadius = 0.4f;
            luz.blendStyleIndex       = 0; // Normal blend

            luzGO.AddComponent<LuzPulsante>();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Copia para Resources para o fallback de runtime funcionar
        CopiarParaResources(sprite);

        Debug.Log("✅ Ícone e luz adicionados no prefab do Dash!");
    }

    static void CopiarParaResources(Sprite sprite)
    {
        const string RES = "Assets/Resources/DashIconBota.png";
        string src = AssetDatabase.GetAssetPath(sprite);
        if (string.IsNullOrEmpty(src) || src == RES) return;

        if (!System.IO.Directory.Exists("Assets/Resources"))
            System.IO.Directory.CreateDirectory("Assets/Resources");

        AssetDatabase.CopyAsset(src, RES);
        AssetDatabase.Refresh();

        var imp = AssetImporter.GetAtPath(RES) as TextureImporter;
        if (imp != null)
        {
            imp.textureType         = TextureImporterType.Sprite;
            imp.alphaIsTransparency = true;
            imp.alphaSource         = TextureImporterAlphaSource.FromInput;
            imp.mipmapEnabled       = false;
            imp.SaveAndReimport();
        }
    }

    static Sprite TentarCarregarAse()
    {
        // Sprite direto
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(ASE_PATH);
        if (s != null) return s;

        // Sub-assets (Aseprite com múltiplos frames)
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(ASE_PATH))
            if (a is Sprite sp) return sp;

        return null;
    }

    static Sprite GerarIconeBota()
    {
        const int SZ = 64;
        string iconPath = "Assets/Skills/DashIconBota.png";

        if (AssetDatabase.LoadAssetAtPath<Sprite>(iconPath) != null)
            AssetDatabase.DeleteAsset(iconPath);

        var tex    = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false);
        var pixels = new Color[SZ * SZ];
        float cx   = SZ * 0.5f;
        Color cor  = new Color(0.3f, 0.65f, 1f);

        // Fundo circular escuro (igual aos outros ícones)
        for (int y = 0; y < SZ; y++)
        for (int x = 0; x < SZ; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cx));
            float a = Mathf.Clamp01(1f - d / (cx * 1.05f));
            pixels[y * SZ + x] = new Color(cor.r * 0.08f, cor.g * 0.1f, cor.b * 0.18f, a);
        }

        // Cano da bota (retângulo vertical, levemente arredondado no topo)
        for (int y = 8; y <= 40; y++)
        for (int x = 18; x <= 34; x++)
        {
            float nx = Mathf.Abs(x - 26f) / 8f;   // 0=centro, 1=borda
            float ny = (y - 8f) / 32f;             // 0=topo, 1=base
            // Arredonda o topo
            float topCurve = y < 14 ? Mathf.Sqrt(1f - Mathf.Pow(nx, 2f)) : 1f;
            if (nx > topCurve) continue;
            float brilho = Mathf.Clamp01(1f - nx * 1.2f) * Mathf.Lerp(1f, 0.5f, ny);
            Color c = Color.Lerp(cor, Color.white, brilho * 0.35f);
            pixels[y * SZ + x] = Color.Lerp(pixels[y * SZ + x], c, Mathf.Clamp01(brilho * 1.4f + 0.4f));
        }

        // Pé da bota (mais largo, bico apontando para direita)
        for (int y = 38; y <= 52; y++)
        for (int x = 12; x <= 52; x++)
        {
            float ny = (y - 38f) / 14f;           // 0=topo, 1=base
            // Bico: lado direito afunila de acordo com y
            float xMax = 52f - ny * 10f;
            if (x > xMax) continue;
            // Lado esquerdo do pé alinha com o cano
            if (x < 12) continue;
            // Parte sob o cano vs bico
            bool emCano = x >= 18 && x <= 34;
            bool emBico = x > 34;
            float nx = emBico ? (x - 34f) / (xMax - 34f) : Mathf.Abs(x - 23f) / 11f;
            float brilho = Mathf.Clamp01(1f - nx * 0.8f) * Mathf.Lerp(1f, 0.55f, ny);
            Color c = Color.Lerp(cor, Color.white, brilho * 0.3f);
            pixels[y * SZ + x] = Color.Lerp(pixels[y * SZ + x], c, Mathf.Clamp01(brilho * 1.3f + 0.35f));
        }

        // Brilho especular no cano (canto superior esquerdo)
        for (int y = 10; y <= 22; y++)
        for (int x = 20; x <= 25; x++)
        {
            if (pixels[y*SZ+x].a < 0.1f) continue;
            float dy = (y - 10f) / 12f; float dx = (x - 20f) / 5f;
            float b = Mathf.Clamp01(1f - (dx*dx + dy*dy)) * 0.55f;
            pixels[y*SZ+x] = Color.Lerp(pixels[y*SZ+x], Color.white, b);
        }

        // Brilho especular no bico do pé
        for (int y = 40; y <= 46; y++)
        for (int x = 36; x <= 46; x++)
        {
            if (pixels[y*SZ+x].a < 0.1f) continue;
            float dy = (y-40f)/6f; float dx = (x-36f)/10f;
            float b = Mathf.Clamp01(1f - (dx*dx*2f + dy*dy)) * 0.45f;
            pixels[y*SZ+x] = Color.Lerp(pixels[y*SZ+x], Color.white, b);
        }

        tex.SetPixels(pixels);
        tex.Apply();
        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "../" + iconPath), png);
        AssetDatabase.Refresh();

        var imp = AssetImporter.GetAtPath(iconPath) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite; imp.spritePixelsPerUnit = SZ;
            imp.filterMode = FilterMode.Bilinear; imp.alphaIsTransparency = true;
            imp.alphaSource = TextureImporterAlphaSource.FromInput; imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
    }

    [MenuItem("Tools/UI Manager/Atribuir Icone do Dash ao Prefab")]
    public static void Criar()
    {
        // Busca o ícone — qualquer sprite em Assets/Skills/DashIcon.*
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ICON_PATH);

        // Tenta variações de extensão
        if (sprite == null)
        {
            string[] variantes = {
                "Assets/Skills/DashIcon.ase",
                "Assets/Skills/DashIcon.aseprite",
                "Assets/Skills/dash_icon.png",
                "Assets/Skills/dash.png",
            };
            foreach (var v in variantes)
            {
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(v);
                if (sprite != null) break;
                // Tenta carregar sub-asset (Aseprite com múltiplos frames)
                var all = AssetDatabase.LoadAllAssetsAtPath(v);
                foreach (var a in all)
                    if (a is Sprite s) { sprite = s; break; }
                if (sprite != null) break;
            }
        }

        if (sprite == null)
        {
            Debug.LogWarning("⚠️ Ícone não encontrado. Crie o arquivo em:\n" +
                             "  Assets/Skills/DashIcon.png  (ou .ase / .aseprite)\n" +
                             "e execute este menu novamente.");
            return;
        }

        // Aplica no prefab
        using (var scope = new PrefabUtility.EditPrefabContentsScope(PREFAB_PATH))
        {
            var root = scope.prefabContentsRoot;
            var sr   = root.GetComponent<SpriteRenderer>();
            if (sr == null) sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"✅ Ícone '{sprite.name}' aplicado no prefab do Dash!");
    }

    static Sprite GerarIcone()
    {
        const int SZ = 32; // 32x32 pixel art limpo

        // Apaga existente para recriar
        if (AssetDatabase.LoadAssetAtPath<Sprite>(ICON_PATH) != null)
            AssetDatabase.DeleteAsset(ICON_PATH);

        var tex    = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false);
        var pixels = new Color[SZ * SZ];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        // Cores da bota
        Color sombra  = new Color(0.15f, 0.25f, 0.55f, 1f); // azul escuro (contorno/sombra)
        Color base_   = new Color(0.30f, 0.55f, 0.95f, 1f); // azul médio (corpo)
        Color brilho1 = new Color(0.60f, 0.82f, 1.00f, 1f); // brilho claro
        Color brilho2 = new Color(0.92f, 0.97f, 1.00f, 1f); // brilho máximo (especular)

        // ── BOTA pixel art 32x32, sem fundo ──
        // Formato: cano (perna) + bico (pé)
        // Cada linha: {x_inicio, x_fim, y}

        // Cano da bota (parte superior — retângulo vertical)
        int[,] cano = {
            {10,17, 3},{10,17, 4},{10,17, 5},{10,17, 6},
            {10,17, 7},{10,17, 8},{10,17, 9},{10,17,10},
            {10,17,11},{10,17,12},{10,17,13},{10,17,14},
        };

        // Pé da bota (parte inferior — mais larga, com bico)
        int[,] pe = {
            { 8,21,15},{ 8,21,16},{ 8,21,17},
            { 8,23,18},{ 8,24,19},{ 8,25,20},
            { 8,26,21},{ 8,26,22},
        };

        // Pinta cano
        for (int i = 0; i < cano.GetLength(0); i++)
            for (int x = cano[i,0]; x <= cano[i,1]; x++)
                Px(pixels, SZ, x, cano[i,2], base_);

        // Pinta pé
        for (int i = 0; i < pe.GetLength(0); i++)
            for (int x = pe[i,0]; x <= pe[i,1]; x++)
                Px(pixels, SZ, x, pe[i,2], base_);

        // Contorno escuro (1px ao redor)
        ContornoEscuro(pixels, SZ, sombra);

        // Brilho lateral esquerda do cano
        for (int y = 4; y <= 13; y++)  Px(pixels, SZ, 11, y, brilho1);
        for (int y = 4; y <= 10; y++)  Px(pixels, SZ, 12, y, brilho1);
        for (int y = 4; y <=  7; y++)  Px(pixels, SZ, 13, y, brilho2);

        // Brilho topo do pé
        for (int x =  9; x <= 14; x++) Px(pixels, SZ, x, 16, brilho1);
        for (int x =  9; x <= 12; x++) Px(pixels, SZ, x, 17, brilho2);

        // Brilho especular pequeno (ponto de luz)
        Px(pixels, SZ, 11,  4, brilho2);
        Px(pixels, SZ, 12,  4, brilho2);
        Px(pixels, SZ, 11,  5, brilho2);

        tex.SetPixels(pixels);
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        System.IO.File.WriteAllBytes(
            System.IO.Path.Combine(Application.dataPath, "../" + ICON_PATH), png);
        AssetDatabase.Refresh();

        var imp = AssetImporter.GetAtPath(ICON_PATH) as TextureImporter;
        if (imp != null)
        {
            imp.textureType         = TextureImporterType.Sprite;
            imp.spritePixelsPerUnit = SZ;
            imp.filterMode          = FilterMode.Point;
            imp.alphaIsTransparency = true;
            imp.alphaSource         = TextureImporterAlphaSource.FromInput;
            imp.mipmapEnabled       = false;
            imp.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(ICON_PATH);
    }

    static void Px(Color[] p, int sz, int x, int y, Color c)
    {
        if (x < 0 || x >= sz || y < 0 || y >= sz) return;
        p[y*sz+x] = c;
    }

    static void ContornoEscuro(Color[] p, int sz, Color sombra)
    {
        var copia = (Color[])p.Clone();
        int[] dx = {-1,1,0,0,-1,-1,1,1};
        int[] dy = {0,0,-1,1,-1,1,-1,1};
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            if (copia[y*sz+x].a < 0.01f) // pixel transparente
            {
                // verifica se algum vizinho é opaco
                for (int d = 0; d < 8; d++)
                {
                    int nx = x+dx[d], ny = y+dy[d];
                    if (nx >= 0 && nx < sz && ny >= 0 && ny < sz && copia[ny*sz+nx].a > 0.5f)
                    { p[y*sz+x] = sombra; break; }
                }
            }
        }
    }
}
#endif
