#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public static class RegenerarIconesPassivas
{
    const string PASTA = "Assets/scripts/scriptables_object/passivas";
    const int    SZ    = 64;

    static readonly (string id, Color cor, string forma)[] PASSIVAS =
    {
        ("asceta",       new Color(1.00f, 0.82f, 0.22f), "asceta"),
        ("foco",         new Color(1.00f, 0.38f, 0.08f), "foco"),
        ("imposicao",    new Color(0.65f, 0.18f, 1.00f), "imposicao"),
        ("pulso_vital",  new Color(0.20f, 0.88f, 0.42f), "pulso_vital"),
        ("ressurgencia", new Color(0.25f, 0.85f, 1.00f), "ressurgencia"),
        ("sombra_veloz", new Color(0.50f, 0.10f, 0.90f), "sombra_veloz"),
        ("ultimo_folego",  new Color(1.00f, 0.28f, 0.12f), "ultimo_folego"),
        ("cacador",        new Color(0.35f, 0.80f, 0.30f), "cacador"),
        ("colheita",       new Color(0.90f, 0.75f, 0.15f), "colheita"),
        ("coracao_robusto",new Color(1.00f, 0.22f, 0.30f), "coracao_robusto"),
    };

    [MenuItem("Tools/Passivas/Regenerar Todos os Ícones")]
    public static void RegenerarTodos()
    {
        int ok = 0;
        foreach (var (id, cor, forma) in PASSIVAS)
        {
            string iconPath = $"{PASTA}/{id}_icon.png";
            if (File.Exists(Path.Combine(Application.dataPath, "../" + iconPath)))
            { AssetDatabase.DeleteAsset(iconPath); AssetDatabase.Refresh(); }

            var sprite = GerarIcone(id, cor, forma, iconPath);
            if (sprite == null) continue;

            // Atualiza o PassiveData se existir
            string assetPath = $"{PASTA}/{id}.asset";
            var pd = AssetDatabase.LoadAssetAtPath<PassiveData>(assetPath);
            if (pd != null) { pd.passiveIcon = sprite; EditorUtility.SetDirty(pd); }
            ok++;
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"✅ {ok}/{PASSIVAS.Length} ícones de passiva regenerados!");
    }

    // ── Geração de sprite ─────────────────────────────────────────────────────

    static Sprite GerarIcone(string id, Color cor, string forma, string iconPath)
    {
        var pixels = new Color[SZ * SZ];
        float cx = SZ * 0.5f;
        GerarFundo(pixels, SZ, cx, cor);

        switch (forma)
        {
            case "asceta":        DesenharAsceta(pixels, SZ, cx, cor);      break;
            case "foco":          DesenharFoco(pixels, SZ, cx, cor);        break;
            case "imposicao":     DesenharImposicao(pixels, SZ, cx, cor);   break;
            case "pulso_vital":   DesenharPulsoVital(pixels, SZ, cx, cor);  break;
            case "ressurgencia":  DesenharRessurgencia(pixels, SZ, cx, cor);break;
            case "sombra_veloz":  DesenharSombraVeloz(pixels, SZ, cx, cor); break;
            case "ultimo_folego":  DesenharUltimoFolego(pixels, SZ, cx, cor); break;
            case "cacador":        DesenharCacador(pixels, SZ, cx, cor);       break;
            case "colheita":       DesenharColheita(pixels, SZ, cx, cor);      break;
            case "coracao_robusto":DesenharCoracaoRobusto(pixels, SZ, cx, cor);break;
        }

        var tex = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false);
        tex.SetPixels(pixels); tex.Apply();
        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        File.WriteAllBytes(Path.Combine(Application.dataPath, "../" + iconPath), png);
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

    // ── FUNDO colorido ────────────────────────────────────────────────────────

    static void GerarFundo(Color[] p, int sz, float cx, Color cor)
    {
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float dx = x - cx, dy = y - cx;
            float d  = Mathf.Sqrt(dx * dx + dy * dy);
            float t  = Mathf.Clamp01(d / cx);
            float a  = Mathf.Clamp01(1f - d / (cx * 1.01f));
            if (a <= 0f) { p[y * sz + x] = Color.clear; continue; }

            Color c0 = new Color(cor.r * 0.55f, cor.g * 0.55f, cor.b * 0.65f);
            Color c1 = new Color(cor.r * 0.25f, cor.g * 0.25f, cor.b * 0.32f);
            Color c2 = new Color(cor.r * 0.08f, cor.g * 0.08f, cor.b * 0.13f);
            Color bg = t < 0.45f ? Color.Lerp(c0, c1, t / 0.45f) : Color.Lerp(c1, c2, (t - 0.45f) / 0.55f);

            float sheen = Mathf.Clamp01(1f - (dx + 1.5f * dy + sz * 0.8f) / (sz * 1.2f)) * 0.13f;
            bg.r = Mathf.Clamp01(bg.r + sheen); bg.g = Mathf.Clamp01(bg.g + sheen); bg.b = Mathf.Clamp01(bg.b + sheen * 1.2f);
            float rim = Mathf.Clamp01((t - 0.80f) / 0.20f);
            bg = Color.Lerp(bg, new Color(cor.r * 0.55f, cor.g * 0.55f, cor.b * 0.75f), rim * 0.55f);
            p[y * sz + x] = new Color(bg.r, bg.g, bg.b, a);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static void Px(Color[] p, int sz, int x, int y, Color c, float f = 1f)
    {
        if (x < 0 || x >= sz || y < 0 || y >= sz) return;
        p[y * sz + x] = Color.Lerp(p[y * sz + x], c, Mathf.Clamp01(f));
    }

    static void Linha(Color[] p, int sz, Vector2 de, Vector2 ate, Color cor, float esp = 2f)
    {
        int steps = Mathf.Max(1, (int)(Vector2.Distance(de, ate) * 2f));
        for (int i = 0; i <= steps; i++)
        {
            Vector2 pt = Vector2.Lerp(de, ate, i / (float)steps);
            int r = Mathf.CeilToInt(esp);
            for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
                Px(p, sz, (int)pt.x + dx, (int)pt.y + dy, cor, Mathf.Clamp01(esp - Mathf.Sqrt(dx * dx + dy * dy)));
        }
    }

    static void Anel(Color[] p, int sz, Vector2 c, float r, Color cor, float esp = 1.5f)
    {
        int segs = Mathf.Max(24, (int)(r * Mathf.PI * 2));
        for (int i = 0; i < segs; i++)
        {
            float a = i / (float)segs * Mathf.PI * 2f;
            Vector2 pt = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            int ri = Mathf.CeilToInt(esp);
            for (int dy = -ri; dy <= ri; dy++)
            for (int dx = -ri; dx <= ri; dx++)
                Px(p, sz, (int)pt.x + dx, (int)pt.y + dy, cor, Mathf.Clamp01(esp - Mathf.Sqrt(dx * dx + dy * dy)));
        }
    }

    static void Elipse(Color[] p, int sz, float ex, float ey, float rx, float ry, Color cor, float borda = 2f)
    {
        for (int y = Mathf.Max(0,(int)(ey-ry-2)); y <= Mathf.Min(sz-1,(int)(ey+ry+2)); y++)
        for (int x = Mathf.Max(0,(int)(ex-rx-2)); x <= Mathf.Min(sz-1,(int)(ex+rx+2)); x++)
        {
            float d = Mathf.Sqrt(Mathf.Pow((x-ex)/rx,2) + Mathf.Pow((y-ey)/ry,2));
            float f = Mathf.Clamp01((1f - d) * borda);
            if (f > 0f) { float hl = Mathf.Clamp01(1f+(ex-x+ey-y)/(rx*4f))*0.3f; Px(p, sz, x, y, Color.Lerp(cor, Color.white, hl), f); }
        }
    }

    static void Coracao(Color[] p, int sz, float hx, float hy, float r, Color cor, float alpha = 1f)
    {
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float nx = (x - hx) / (r * 0.65f), ny = (y - hy * 0.92f) / (r * 0.65f);
            float h = Mathf.Pow(nx*nx + ny*ny - 0.5f, 3f) - nx*nx*ny*ny*ny;
            if (h <= 0f) Px(p, sz, x, y, cor, alpha * Mathf.Clamp01((-h)*3f + 0.5f));
        }
    }

    static void Silhueta(Color[] p, int sz, float x, float y, Color cor, float alpha = 0.8f)
    {
        Elipse(p, sz, x, y + 10f, 4f, 4f, cor, 1.8f);
        Elipse(p, sz, x, y,       3.5f, 6f, cor, 1.8f);
        Linha(p, sz, new Vector2(x-3f, y-4f), new Vector2(x-5f, y-10f), cor, 1.5f);
        Linha(p, sz, new Vector2(x+3f, y-4f), new Vector2(x+4f, y-10f), cor, 1.5f);
    }

    // ── FORMAS ────────────────────────────────────────────────────────────────

    // ASCETA — triângulo de equilíbrio com 3 ícones (espada, escudo, coração)
    static void DesenharAsceta(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.65f);
        Color corEsc = new Color(0.7f, 0.85f, 1f);

        // Triângulo equilátero central
        Vector2[] tri = {
            new Vector2(cx,       cx + 20f),   // topo
            new Vector2(cx - 18f, cx - 12f),   // base esquerda
            new Vector2(cx + 18f, cx - 12f),   // base direita
        };
        for (int i = 0; i < 3; i++)
            Linha(p, sz, tri[i], tri[(i+1)%3], brilho, 2f);

        // Espada no topo (ATQ)
        Linha(p, sz, new Vector2(cx, cx + 28f), new Vector2(cx, cx + 16f), brilho, 1.8f);
        Linha(p, sz, new Vector2(cx-3f, cx+22f), new Vector2(cx+3f, cx+22f), brilho, 1.2f); // guarda
        Elipse(p, sz, cx, cx + 28f, 2f, 2f, Color.white, 3f);

        // Escudo na base esquerda (DEF)
        Vector2 bl = tri[1];
        Elipse(p, sz, bl.x, bl.y - 6f, 5f, 6f, corEsc, 1.8f);
        Linha(p, sz, bl + new Vector2(-3f,-10f), bl + new Vector2(3f,-10f), Color.white, 1f);

        // Coração na base direita (HP regen)
        Coracao(p, sz, tri[2].x, tri[2].y - 4f, sz * 0.12f, cor, 0.9f);

        // Estrela XP no centro
        Elipse(p, sz, cx, cx + 2f, 4f, 4f, Color.Lerp(cor, Color.white, 0.4f), 2f);
        Elipse(p, sz, cx, cx + 2f, 2f, 2f, Color.white, 4f);
    }

    // FOCO — mira de crítico com raio de energia
    static void DesenharFoco(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.7f);

        // Anel de mira externo
        Anel(p, sz, new Vector2(cx, cx), 20f, brilho, 2f);
        // Anel interno
        Anel(p, sz, new Vector2(cx, cx), 13f, new Color(brilho.r, brilho.g, brilho.b, 0.6f), 1.5f);

        // 4 linhas de mira (cruz)
        Linha(p, sz, new Vector2(cx - 28f, cx), new Vector2(cx - 22f, cx), brilho, 2f);
        Linha(p, sz, new Vector2(cx + 22f, cx), new Vector2(cx + 28f, cx), brilho, 2f);
        Linha(p, sz, new Vector2(cx, cx + 22f), new Vector2(cx, cx + 28f), brilho, 2f);
        Linha(p, sz, new Vector2(cx, cx - 28f), new Vector2(cx, cx - 22f), brilho, 2f);

        // Raio de crítico no centro (zigzag)
        Vector2[] rpts = { new Vector2(cx-5f,cx+8f), new Vector2(cx+2f,cx+2f), new Vector2(cx-2f,cx-2f), new Vector2(cx+5f,cx-8f) };
        for (int i = 0; i < rpts.Length-1; i++) Linha(p, sz, rpts[i], rpts[i+1], new Color(brilho.r,brilho.g,brilho.b,0.4f), 3.5f);
        for (int i = 0; i < rpts.Length-1; i++) Linha(p, sz, rpts[i], rpts[i+1], Color.white, 1.5f);

        // Brilho central
        Elipse(p, sz, cx, cx, 4f, 4f, Color.white, 3f);

        // Exclamação de crítico (!)
        Elipse(p, sz, cx + 14f, cx + 14f, 2.5f, 2.5f, brilho, 3f);
        Linha(p, sz, new Vector2(cx+14f, cx+10f), new Vector2(cx+14f, cx+4f), brilho, 1.8f);
    }

    // IMPOSIÇÃO — aura de dominação com coroa e ondas
    static void DesenharImposicao(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.6f);

        // Ondas de aura expandindo (4 anéis)
        float[] rs = { 8f, 13f, 18f, 23f };
        float[] as2 = { 0.9f, 0.65f, 0.4f, 0.2f };
        for (int i = 0; i < rs.Length; i++)
            Anel(p, sz, new Vector2(cx, cx), rs[i], new Color(brilho.r, brilho.g, brilho.b, as2[i]), Mathf.Lerp(2.5f, 1f, i / 3f));

        // Silhueta dominante no centro
        Silhueta(p, sz, cx, cx - 2f, brilho, 0.9f);

        // Coroa sobre a cabeça
        float ty = cx + 14f;
        Linha(p, sz, new Vector2(cx - 6f, ty), new Vector2(cx + 6f, ty), brilho, 1.8f); // base da coroa
        // 3 pontas
        Linha(p, sz, new Vector2(cx - 5f, ty), new Vector2(cx - 6f, ty + 5f), brilho, 1.5f);
        Linha(p, sz, new Vector2(cx,      ty), new Vector2(cx,      ty + 7f), brilho, 1.5f);
        Linha(p, sz, new Vector2(cx + 5f, ty), new Vector2(cx + 6f, ty + 5f), brilho, 1.5f);
        Elipse(p, sz, cx - 6f, ty + 5f, 1.5f, 1.5f, Color.white, 3f);
        Elipse(p, sz, cx,      ty + 7f, 1.5f, 1.5f, Color.white, 3f);
        Elipse(p, sz, cx + 6f, ty + 5f, 1.5f, 1.5f, Color.white, 3f);
    }

    // PULSO VITAL — coração grande com pulso + seta de dreno
    static void DesenharPulsoVital(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho   = Color.Lerp(cor, Color.white, 0.65f);
        Color corDreno = new Color(0.9f, 0.2f, 0.2f);

        // Coração principal (player)
        Coracao(p, sz, cx, cx * 0.95f, sz * 0.38f, cor, 0.92f);

        // Pulso cardíaco (linha ECG)
        Vector2[] ecg = {
            new Vector2(cx - 20f, cx - 8f), new Vector2(cx - 10f, cx - 8f),
            new Vector2(cx - 6f,  cx + 6f), new Vector2(cx - 2f,  cx - 16f),
            new Vector2(cx + 2f,  cx + 10f), new Vector2(cx + 6f,  cx - 8f),
            new Vector2(cx + 20f, cx - 8f)
        };
        for (int i = 0; i < ecg.Length - 1; i++)
            Linha(p, sz, ecg[i], ecg[i+1], Color.white, 1.8f);

        // Setas de dreno ao redor (inimigos → player)
        for (int i = 0; i < 4; i++)
        {
            float ang = (i / 4f + 0.125f) * Mathf.PI * 2f;
            Vector2 ext = new Vector2(cx + Mathf.Cos(ang) * 22f, cx + Mathf.Sin(ang) * 22f);
            Vector2 mid = new Vector2(cx + Mathf.Cos(ang) * 13f, cx + Mathf.Sin(ang) * 13f);
            Linha(p, sz, ext, mid, new Color(corDreno.r, corDreno.g, corDreno.b, 0.6f), 1.5f);
            // Ponta da seta
            Vector2 perp = new Vector2(-Mathf.Sin(ang), Mathf.Cos(ang));
            Linha(p, sz, mid, mid + (ext - mid).normalized * 4f + perp * 3f, new Color(corDreno.r, corDreno.g, corDreno.b, 0.6f), 1.2f);
            Linha(p, sz, mid, mid + (ext - mid).normalized * 4f - perp * 3f, new Color(corDreno.r, corDreno.g, corDreno.b, 0.6f), 1.2f);
        }
    }

    // RESSURGÊNCIA — raio de morte + brilho de vida + speed
    static void DesenharRessurgencia(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho   = Color.Lerp(cor, Color.white, 0.7f);
        Color corVida  = new Color(0.3f, 1f, 0.4f);
        Color corMorte = new Color(0.9f, 0.2f, 0.2f);

        // Silhueta correndo (inclinada para frente)
        Elipse(p, sz, cx + 4f, cx + 10f, 4f, 4f, brilho, 2f);   // cabeça
        Elipse(p, sz, cx + 2f, cx + 2f,  3.5f, 5f, brilho, 2f); // corpo inclinado
        Linha(p, sz, new Vector2(cx-2f, cx-4f), new Vector2(cx-5f, cx-12f), brilho, 2f); // perna traseira
        Linha(p, sz, new Vector2(cx+4f, cx-3f), new Vector2(cx+8f, cx-10f), brilho, 2f); // perna dianteira

        // Linhas de velocidade (atrás)
        for (int i = 0; i < 3; i++)
        {
            float oy = cx + (i - 1) * 7f;
            float larg = Mathf.Lerp(14f, 8f, i / 2f);
            Linha(p, sz, new Vector2(cx - 8f, oy), new Vector2(cx - 8f - larg, oy),
                  new Color(brilho.r, brilho.g, brilho.b, 0.5f - i * 0.1f), 1.5f);
        }

        // Coração pequeno de cura (acima)
        Coracao(p, sz, cx + 16f, cx + 18f, sz * 0.14f, corVida, 0.85f);

        // Asterisco de morte (inimigo) no canto inferior
        for (int i = 0; i < 4; i++)
        {
            float ang = i / 4f * Mathf.PI;
            Vector2 dir2 = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 6f;
            Linha(p, sz, new Vector2(cx - 16f, cx - 14f) - dir2, new Vector2(cx - 16f, cx - 14f) + dir2, corMorte, 1.5f);
        }
    }

    // SOMBRA VELOZ — 3 silhuetas em cascata com rastros roxos
    static void DesenharSombraVeloz(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.6f);

        // 3 silhuetas em cascata (mais atrás = mais translúcido)
        float[] offsets = { -16f, -8f, 0f };
        float[] alphas  = {  0.2f, 0.4f, 0.85f };
        for (int i = 0; i < 3; i++)
        {
            Color c = new Color(brilho.r, brilho.g, brilho.b, alphas[i]);
            Silhueta(p, sz, cx + offsets[i], cx - 2f, c, alphas[i]);
        }

        // Rastros de sombra (linhas atrás)
        for (int i = 0; i < 4; i++)
        {
            float oy = cx - 8f + i * 5f;
            Linha(p, sz, new Vector2(cx - 18f, oy), new Vector2(cx - 28f, oy),
                  new Color(cor.r, cor.g, cor.b, 0.35f - i * 0.05f), 1.2f);
        }

        // Marcas de velocidade (riscos diagonais)
        for (int i = 0; i < 3; i++)
        {
            float oy = cx - 4f + i * 7f;
            Linha(p, sz, new Vector2(cx + 6f, oy), new Vector2(cx + 20f, oy - 3f),
                  new Color(brilho.r, brilho.g, brilho.b, 0.6f - i * 0.1f), 1.5f);
        }

        // Olhos brilhantes na silhueta principal
        Elipse(p, sz, cx - 1.5f, cx + 8.5f, 1.5f, 1.5f, Color.white, 4f);
        Elipse(p, sz, cx + 2.5f, cx + 8.5f, 1.5f, 1.5f, Color.white, 4f);
    }

    // ÚLTIMO FÔLEGO — escudo rachado que se mantém + flash de invulnerabilidade
    static void DesenharUltimoFolego(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho   = Color.Lerp(cor, Color.white, 0.7f);
        Color rachado  = new Color(1f, 0.7f, 0.3f);

        // Escudo base
        int cy = (int)cx;
        for (int y = cy - 22; y <= cy + 18; y++)
        for (int x = (int)cx - 17; x <= (int)cx + 17; x++)
        {
            if (x < 0 || x >= sz || y < 0 || y >= sz) continue;
            float nx = Mathf.Abs(x - cx) / 17f;
            float ny = (y - cy) / 22f;
            float larg = ny < -0.5f ? (1f + ny*2f) : ny > 0.65f ? ((1f-ny)/0.35f) : 1f;
            float d = nx / Mathf.Max(larg, 0.01f);
            if (d <= 1f)
            {
                float b = Mathf.Clamp01(1f - d) * 0.9f;
                Px(p, sz, x, y, Color.Lerp(cor, brilho, b * 0.5f), Mathf.Clamp01(b * 1.6f));
            }
        }

        // Rachaduras diagonais (escudo danificado)
        Linha(p, sz, new Vector2(cx - 2f, cy + 10f), new Vector2(cx + 8f,  cy - 5f), rachado, 1.8f);
        Linha(p, sz, new Vector2(cx + 8f, cy - 5f),  new Vector2(cx + 3f,  cy - 18f), rachado, 1.5f);
        Linha(p, sz, new Vector2(cx - 5f, cy + 2f),  new Vector2(cx - 12f, cy - 8f),  rachado, 1.3f);

        // Brilho de resistência no centro (invulnerabilidade)
        Elipse(p, sz, cx, cy - 2f, 7f, 7f, Color.Lerp(cor, Color.white, 0.3f), 2f);
        Elipse(p, sz, cx, cy - 2f, 4f, 4f, brilho, 3f);
        Elipse(p, sz, cx, cy - 2f, 2f, 2f, Color.white, 5f);

        // Aura de última chance ao redor
        Anel(p, sz, new Vector2(cx, cx), 22f, new Color(brilho.r, brilho.g, brilho.b, 0.4f), 1.5f);

        // Faíscas de impacto
        for (int i = 0; i < 5; i++)
        {
            float ang = (i / 5f) * Mathf.PI * 2f;
            Vector2 sp = new Vector2(cx + Mathf.Cos(ang) * 19f, cx + Mathf.Sin(ang) * 19f);
            Elipse(p, sz, sp.x, sp.y, 2f, 2f, rachado, 2.5f);
        }
    }
    // CAÇADOR — garra/presa + bônus de XP (+5 ATQ, +10% XP)
    static void DesenharCacador(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.65f);
        Color xpCor  = new Color(1f, 0.9f, 0.2f);

        // 3 marcas de garra (predador)
        float[] gx = { cx - 10f, cx, cx + 10f };
        for (int i = 0; i < 3; i++)
        {
            float ox = gx[i];
            // Curva da garra: linha que curva para baixo
            for (int s = 0; s <= 20; s++)
            {
                float t   = s / 20f;
                float gY  = cx + 16f - t * t * 28f;
                float gX  = ox + Mathf.Sin(t * Mathf.PI) * (i == 1 ? 0f : (i == 0 ? -4f : 4f));
                float esp = Mathf.Lerp(2.2f, 0.5f, t);
                Elipse(p, sz, gX, gY, esp, esp, Color.Lerp(brilho, Color.white, t * 0.4f), 2f);
            }
        }

        // Estrela de XP (canto superior direito)
        Vector2 xpPos = new Vector2(cx + 14f, cx + 18f);
        for (int i = 0; i < 4; i++)
        {
            float ang = i / 4f * Mathf.PI * 2f - Mathf.PI * 0.25f;
            Linha(p, sz, xpPos, xpPos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 6f, xpCor, 1.5f);
        }
        Elipse(p, sz, xpPos.x, xpPos.y, 2.5f, 2.5f, Color.white, 3f);

        // Olho do predador (centro)
        Elipse(p, sz, cx, cx - 4f, 5f, 3.5f, new Color(cor.r * 0.4f, cor.g * 0.6f, cor.b * 0.2f), 2f);
        Elipse(p, sz, cx, cx - 4f, 2f, 3f, new Color(0.05f, 0.05f, 0.05f), 3f); // pupila
        Elipse(p, sz, cx + 1.5f, cx - 5.5f, 1f, 1f, Color.white, 4f); // reflexo
    }

    // COLHEITA — foice + estrelas de XP (25% mais XP)
    static void DesenharColheita(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.7f);
        Color cabo   = new Color(cor.r * 0.5f, cor.g * 0.35f, cor.b * 0.1f);
        Color xpCor  = new Color(1f, 0.95f, 0.3f);

        // Cabo diagonal
        Linha(p, sz, new Vector2(cx - 16f, cx - 20f), new Vector2(cx + 8f, cx + 16f), cabo, 2.5f);

        // Lâmina em arco (foice)
        int steps = 36;
        Vector2 prev = Vector2.zero;
        for (int i = 0; i <= steps; i++)
        {
            float t   = i / (float)steps;
            float ang = Mathf.Lerp(Mathf.PI * 0.15f, Mathf.PI * 1.05f, t);
            float r2  = Mathf.Lerp(13f, 17f, Mathf.Sin(t * Mathf.PI));
            Vector2 pt = new Vector2(cx + 5f + Mathf.Cos(ang) * r2, cx + 3f + Mathf.Sin(ang) * r2);
            if (i > 0)
            {
                Linha(p, sz, prev, pt, new Color(brilho.r, brilho.g, brilho.b, 0.4f), 3.5f);
                Linha(p, sz, prev, pt, brilho, 2f);
                if (i % 3 == 0) Linha(p, sz, prev, pt, Color.white, 0.7f);
            }
            prev = pt;
        }

        // 3 estrelas de XP flutuando
        Vector2[] xpPts = { new Vector2(cx + 16f, cx + 18f), new Vector2(cx + 20f, cx + 8f), new Vector2(cx + 15f, cx - 2f) };
        float[] xpSz    = { 5f, 4f, 3.5f };
        foreach (var (xp, xs) in System.Linq.Enumerable.Zip(xpPts, xpSz, (a, b) => (a, b)))
        {
            for (int i = 0; i < 4; i++)
            {
                float ang = i / 4f * Mathf.PI * 2f;
                Linha(p, sz, xp, xp + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * xs, xpCor, 1.3f);
            }
            Elipse(p, sz, xp.x, xp.y, xs * 0.4f, xs * 0.4f, Color.white, 3f);
        }
    }

    // CORAÇÃO ROBUSTO — coração grande blindado (+30 HP máx)
    static void DesenharCoracaoRobusto(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho   = Color.Lerp(cor, Color.white, 0.6f);
        Color armadura = new Color(0.75f, 0.75f, 0.85f);

        // Coração base (grande)
        Coracao(p, sz, cx, cx * 0.95f, sz * 0.42f, cor, 0.92f);

        // Placa de armadura central
        for (int y = (int)(cx - 12); y <= (int)(cx + 8); y++)
        for (int x = (int)(cx - 10); x <= (int)(cx + 10); x++)
        {
            if (x < 0 || x >= sz || y < 0 || y >= sz) continue;
            float nx = Mathf.Abs(x - cx) / 10f;
            float ny = (y - (cx - 2f)) / 10f;
            float d  = Mathf.Max(nx, Mathf.Abs(ny - 0.5f));
            if (d < 0.9f)
            {
                float b = Mathf.Clamp01(1f - d / 0.9f);
                Px(p, sz, x, y, Color.Lerp(armadura, Color.white, b * 0.35f), b * 0.9f);
            }
        }

        // Rebites da armadura
        float[] rby = { cx - 8f, cx - 2f, cx + 4f };
        foreach (float ry in rby)
        {
            Elipse(p, sz, cx - 5f, ry, 1.5f, 1.5f, Color.white, 3f);
            Elipse(p, sz, cx + 5f, ry, 1.5f, 1.5f, Color.white, 3f);
        }

        // Símbolo de + (saúde) no centro da armadura
        Linha(p, sz, new Vector2(cx - 5f, cx - 2f), new Vector2(cx + 5f, cx - 2f), Color.white, 2f);
        Linha(p, sz, new Vector2(cx, cx - 7f),      new Vector2(cx, cx + 3f),      Color.white, 2f);

        // Brilho especular no coração
        Elipse(p, sz, cx - 5f, cx + 9f, 3.5f, 2.5f, new Color(brilho.r, brilho.g, brilho.b, 0.5f), 2f);
    }
}
#endif
