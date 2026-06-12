#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public static class RegenerarIconesUltimates
{
    const string PASTA = "Assets/prefebs/ultimates";
    const int    SZ    = 64;

    // (asset de UltimateData, id do ícone/cor tema, forma)
    static readonly (string asset, string iconId, Color cor, string forma)[] MAPA =
    {
        ("raio_certeiro",       "raio_certeiro",       new Color(0.40f, 0.80f, 1.00f), "raio"),
        ("tempestade_eletrica", "tempestade_eletrica", new Color(0.30f, 0.65f, 1.00f), "tempestade"),
        ("chuva_meteoros",      "chuva_meteoros",      new Color(1.00f, 0.45f, 0.10f), "meteoros"),
        ("campo_de_gelo",       "campo_gelo",          new Color(0.55f, 0.92f, 1.00f), "gelo"),
        ("mare_implacavel",     "tsunami",             new Color(0.10f, 0.50f, 1.00f), "tsunami"),
        ("vortice",             "vortice",             new Color(0.60f, 0.20f, 1.00f), "vortice"),
        ("necropole",           "necropole",           new Color(0.25f, 0.75f, 0.35f), "necropole"),
        ("domo_retardante",     "domo",                new Color(0.20f, 0.70f, 1.00f), "domo"),
        ("escudo_sonico",       "escudo_sonico",       new Color(0.30f, 0.95f, 0.90f), "sonico"),
        ("correntes_inferno",   "correntes_inferno",   new Color(1.00f, 0.30f, 0.10f), "correntes"),
        ("punicao_divina",      "punicao_divina",      new Color(1.00f, 0.90f, 0.25f), "divina"),
        ("drenagem_vida",       "drenagem_vida",       new Color(0.85f, 0.15f, 0.40f), "drenagem"),
        ("pulso_magnetico",     "pulso_magnetico",     new Color(0.30f, 0.75f, 1.00f), "magnetico"),
        ("despertar_anciao",    "anciao",              new Color(1.00f, 0.80f, 0.25f), "anciao"),
        ("bencao_anciao",       "bencao_anciao",       new Color(1.00f, 0.90f, 0.40f), "bencao"),
        ("casulo_cristal",      "casulo_cristal",      new Color(0.60f, 0.95f, 1.00f), "casulo"),
        ("ritual_anciao",       "ritual_anciao",       new Color(0.80f, 0.50f, 1.00f), "ritual"),
    };

    [MenuItem("Tools/Ultimates/Regenerar Todos os Ícones")]
    public static void RegenerarTodos()
    {
        int ok = 0;
        foreach (var (asset, iconId, cor, forma) in MAPA)
        {
            string iconPath = $"{PASTA}/{iconId}_icon.png";
            if (File.Exists(Path.Combine(Application.dataPath, "../" + iconPath)))
            { AssetDatabase.DeleteAsset(iconPath); AssetDatabase.Refresh(); }

            var sprite = GerarIcone(iconId, cor, forma, iconPath);
            if (sprite == null) continue;

            string assetPath = $"{PASTA}/{asset}.asset";
            var ud = AssetDatabase.LoadAssetAtPath<UltimateData>(assetPath);
            if (ud != null)
            {
                ud.ultimateIcon = sprite;
                EditorUtility.SetDirty(ud);
                ok++;
            }
            else
            {
                Debug.LogWarning($"UltimateData não encontrado em {assetPath}");
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"✅ {ok}/{MAPA.Length} ícones de ultimate regenerados!");
    }

    // ── Geração ───────────────────────────────────────────────────────────────

    static Sprite GerarIcone(string id, Color cor, string forma, string iconPath)
    {
        var pixels = new Color[SZ * SZ];
        float cx = SZ * 0.5f;
        GerarFundo(pixels, SZ, cx, cor);

        switch (forma)
        {
            case "raio":        DesenharRaio(pixels, SZ, cx, cor);       break;
            case "tempestade":  DesenharTempestade(pixels, SZ, cx, cor); break;
            case "meteoros":    DesenharMeteoros(pixels, SZ, cx, cor);   break;
            case "gelo":        DesenharGelo(pixels, SZ, cx, cor);       break;
            case "tsunami":     DesenharTsunami(pixels, SZ, cx, cor);    break;
            case "vortice":     DesenharVortice(pixels, SZ, cx, cor);    break;
            case "clone":       DesenharClone(pixels, SZ, cx, cor);      break;
            case "necropole":   DesenharNecropole(pixels, SZ, cx, cor);  break;
            case "domo":        DesenharDomo(pixels, SZ, cx, cor);       break;
            case "sonico":      DesenharSonico(pixels, SZ, cx, cor);     break;
            case "correntes":   DesenharCorrente(pixels, SZ, cx, cor);   break;
            case "divina":      DesenharDivina(pixels, SZ, cx, cor);     break;
            case "fantasma":    DesenharFantasma(pixels, SZ, cx, cor);   break;
            case "drenagem":    DesenharDrenagem(pixels, SZ, cx, cor);   break;
            case "magnetico":   DesenharMagnetico(pixels, SZ, cx, cor);  break;
            case "coracao":     DesenharCoracao(pixels, SZ, cx, cor);    break;
            case "colheita":    DesenharColheita(pixels, SZ, cx, cor);   break;
            case "anciao":      DesenharAnciao(pixels, SZ, cx, cor);     break;
            case "bencao":      DesenharBencao(pixels, SZ, cx, cor);     break;
            case "cacador":     DesenharCacador(pixels, SZ, cx, cor);    break;
            case "casulo":      DesenharCasulo(pixels, SZ, cx, cor);     break;
            case "ritual":      DesenharRitual(pixels, SZ, cx, cor);     break;
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

    // ── FUNDO colorido (igual ao de skills) ───────────────────────────────────

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

    // ── HELPERS ───────────────────────────────────────────────────────────────

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
        int segs = Mathf.Max(24, (int)(r * Mathf.PI * 2f));
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
        for (int y = Mathf.Max(0, (int)(ey - ry - 2)); y <= Mathf.Min(sz - 1, (int)(ey + ry + 2)); y++)
        for (int x = Mathf.Max(0, (int)(ex - rx - 2)); x <= Mathf.Min(sz - 1, (int)(ex + rx + 2)); x++)
        {
            float ndx = (x - ex) / rx, ndy = (y - ey) / ry;
            float d = Mathf.Sqrt(ndx * ndx + ndy * ndy);
            float f = Mathf.Clamp01((1f - d) * borda);
            if (f > 0f)
            {
                float hl = Mathf.Clamp01(1f + (ex - x + ey - y) / (rx * 4f)) * 0.3f;
                Px(p, sz, x, y, Color.Lerp(cor, Color.white, hl), f);
            }
        }
    }

    static void Estrela(Color[] p, int sz, Vector2 c, float r, Color cor, int pts = 5)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.5f);
        for (int i = 0; i < pts; i++)
        {
            float a1 = i / (float)pts * Mathf.PI * 2f - Mathf.PI * 0.5f;
            float a2 = (i + 0.5f) / (float)pts * Mathf.PI * 2f - Mathf.PI * 0.5f;
            float a3 = (i + 1f) / (float)pts * Mathf.PI * 2f - Mathf.PI * 0.5f;
            Vector2 ext1 = c + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * r;
            Vector2 intr = c + new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)) * (r * 0.42f);
            Vector2 ext2 = c + new Vector2(Mathf.Cos(a3), Mathf.Sin(a3)) * r;
            Linha(p, sz, ext1, intr, Color.Lerp(cor, brilho, 0.4f), 1.5f);
            Linha(p, sz, intr, ext2, Color.Lerp(cor, brilho, 0.4f), 1.5f);
        }
        Elipse(p, sz, c.x, c.y, r * 0.22f, r * 0.22f, Color.white, 4f);
    }

    // ── FORMAS ────────────────────────────────────────────────────────────────

    // RAIO CERTEIRO — cadeia de raios em zigue-zague
    static void DesenharRaio(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.7f);
        // Raio principal em zigzag
        Vector2[] pts = {
            new Vector2(cx - 6, cx + 24), new Vector2(cx + 4, cx + 10),
            new Vector2(cx - 2, cx + 4),  new Vector2(cx + 8, cx - 10),
            new Vector2(cx,     cx - 24)
        };
        // Glow
        for (int i = 0; i < pts.Length - 1; i++) Linha(p, sz, pts[i], pts[i+1], new Color(cor.r, cor.g, cor.b, 0.4f), 4f);
        // Núcleo
        for (int i = 0; i < pts.Length - 1; i++) Linha(p, sz, pts[i], pts[i+1], brilho, 2f);
        // Fio branco central
        for (int i = 0; i < pts.Length - 1; i++) Linha(p, sz, pts[i], pts[i+1], Color.white, 0.8f);
        // Ponta brilhante
        Elipse(p, sz, pts[pts.Length-1].x, pts[pts.Length-1].y, 3f, 3f, Color.white, 4f);
        // Ramificações
        Linha(p, sz, pts[1], pts[1] + new Vector2(8, 5), new Color(cor.r, cor.g, cor.b, 0.6f), 1.5f);
        Linha(p, sz, pts[2], pts[2] + new Vector2(-6, 4), new Color(cor.r, cor.g, cor.b, 0.5f), 1.2f);
    }

    // TEMPESTADE ELÉTRICA — nuvem + raios
    static void DesenharTempestade(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.65f);
        // Nuvem (3 elipses sobrepostas)
        Elipse(p, sz, cx,      cx + 16, 14f, 8f, cor, 1.8f);
        Elipse(p, sz, cx - 9,  cx + 20, 9f,  6f, cor, 1.8f);
        Elipse(p, sz, cx + 9,  cx + 20, 8f,  5f, cor, 1.8f);
        // Raios saindo da nuvem
        Vector2[] rPts = { new Vector2(cx - 4, cx + 10), new Vector2(cx + 2, cx - 2), new Vector2(cx - 2, cx - 4), new Vector2(cx + 4, cx - 18) };
        for (int i = 0; i < rPts.Length - 1; i++) Linha(p, sz, rPts[i], rPts[i+1], new Color(cor.r, cor.g, cor.b, 0.4f), 3.5f);
        for (int i = 0; i < rPts.Length - 1; i++) Linha(p, sz, rPts[i], rPts[i+1], brilho, 1.8f);
        for (int i = 0; i < rPts.Length - 1; i++) Linha(p, sz, rPts[i], rPts[i+1], Color.white, 0.7f);
        Elipse(p, sz, rPts[rPts.Length-1].x, rPts[rPts.Length-1].y, 2.5f, 2.5f, Color.white, 4f);
    }

    // CHUVA DE METEOROS — meteoros caindo em diagonal
    static void DesenharMeteoros(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho  = Color.Lerp(cor, Color.white, 0.7f);
        Color cauda   = new Color(cor.r, cor.g * 0.5f, 0f, 0.5f);
        // 3 meteoros em diagonal
        Vector2[] mets = { new Vector2(cx + 14, cx + 20), new Vector2(cx, cx + 8), new Vector2(cx - 10, cx - 4) };
        float[] szs    = { 5f, 4f, 3f };
        for (int m = 0; m < 3; m++)
        {
            Vector2 mc = mets[m]; float ms = szs[m];
            // Cauda
            Linha(p, sz, mc, mc + new Vector2(-12f, 12f) * (ms / 4f), cauda, ms * 0.6f);
            // Corpo do meteoro
            Elipse(p, sz, mc.x, mc.y, ms, ms * 0.7f, brilho, 2.5f);
            // Núcleo
            Elipse(p, sz, mc.x, mc.y, ms * 0.5f, ms * 0.4f, Color.white, 4f);
        }
        // Pequenas centelhas
        for (int i = 0; i < 5; i++)
        {
            float ang = i / 5f * Mathf.PI * 2f;
            Vector2 sp = new Vector2(cx + Mathf.Cos(ang) * 16f, cx + Mathf.Sin(ang) * 14f);
            Elipse(p, sz, sp.x, sp.y, 1.2f, 1.2f, brilho, 3f);
        }
    }

    // CAMPO DE GELO — flocos de neve + cristais
    static void DesenharGelo(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.75f);
        // Floco de neve (6 eixos)
        for (int i = 0; i < 6; i++)
        {
            float ang = i / 6f * Mathf.PI * 2f;
            Vector2 ext = new Vector2(cx + Mathf.Cos(ang) * 22f, cx + Mathf.Sin(ang) * 22f);
            Linha(p, sz, new Vector2(cx, cx), ext, brilho, 1.8f);
            // Braços laterais
            Vector2 mid = new Vector2(cx + Mathf.Cos(ang) * 12f, cx + Mathf.Sin(ang) * 12f);
            float a2 = ang + Mathf.PI * 0.33f;
            Linha(p, sz, mid, mid + new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)) * 6f, brilho, 1.2f);
            Linha(p, sz, mid, mid - new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)) * 6f, brilho, 1.2f);
        }
        // Hexágono central
        for (int i = 0; i < 6; i++)
        {
            float a1 = i / 6f * Mathf.PI * 2f, a2 = (i + 1) / 6f * Mathf.PI * 2f;
            Linha(p, sz, new Vector2(cx + Mathf.Cos(a1) * 6f, cx + Mathf.Sin(a1) * 6f),
                         new Vector2(cx + Mathf.Cos(a2) * 6f, cx + Mathf.Sin(a2) * 6f), brilho, 1.5f);
        }
        Elipse(p, sz, cx, cx, 3f, 3f, Color.white, 4f);
    }

    // TSUNAMI — onda gigante
    static void DesenharTsunami(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.65f);
        Color espuma = Color.white;
        // Onda principal: senoide crescente
        int steps = 80;
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            float x = Mathf.Lerp(4f, sz - 4f, t);
            float baseY = cx - 4f;
            float amp   = Mathf.Lerp(4f, 18f, t);
            float y     = baseY + Mathf.Sin(t * Mathf.PI * 1.5f - 0.5f) * amp;
            // Fill abaixo da onda
            for (int fy = 4; fy <= (int)y; fy++)
                Px(p, sz, (int)x, fy, new Color(cor.r, cor.g, cor.b, Mathf.Lerp(0.15f, 0.5f, (y - fy) / amp)), 0.7f);
            // Borda da onda
            Elipse(p, sz, x, y, 2f, 2f, Color.Lerp(brilho, espuma, t * 0.6f), 2.5f);
        }
        // Espuma no topo da crista
        for (int i = 0; i < 6; i++)
        {
            float t = i / 5f;
            float x = Mathf.Lerp(sz - 16f, sz - 5f, t);
            float y = cx + 6f + Mathf.Sin(t * Mathf.PI) * 8f;
            Elipse(p, sz, x, y, 3f, 2f, Color.Lerp(brilho, espuma, 0.7f), 2f);
        }
    }

    // VÓRTICE — espiral de energia
    static void DesenharVortice(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.65f);
        int steps = 180;
        for (int i = 0; i < steps; i++)
        {
            float t   = i / (float)(steps - 1);
            float ang = t * Mathf.PI * 4f;
            float r   = Mathf.Lerp(22f, 2f, t);
            Vector2 pt = new Vector2(cx + Mathf.Cos(ang) * r, cx + Mathf.Sin(ang) * r);
            float esp = Mathf.Lerp(3f, 0.8f, t);
            Color c = Color.Lerp(cor, brilho, 1f - t);
            int ri = Mathf.CeilToInt(esp);
            for (int dy = -ri; dy <= ri; dy++)
            for (int dx = -ri; dx <= ri; dx++)
                Px(p, sz, (int)pt.x + dx, (int)pt.y + dy, c, Mathf.Clamp01(esp - Mathf.Sqrt(dx*dx + dy*dy)));
        }
        Elipse(p, sz, cx, cx, 3f, 3f, brilho, 3f);
        Elipse(p, sz, cx, cx, 1.5f, 1.5f, Color.white, 5f);
    }

    // CLONE DAS SOMBRAS — dois silouetas lado a lado
    static void DesenharClone(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.6f);
        Color sombra = new Color(cor.r * 0.4f, cor.g * 0.4f, cor.b * 0.5f, 0.6f);
        // Clone sombra (esquerda, translúcido)
        DesenharSilhueta(p, sz, cx - 9f, cx, sombra, 0.55f);
        // Player real (direita, brilhante)
        DesenharSilhueta(p, sz, cx + 6f, cx, brilho, 0.9f);
        // Rastro de energia entre eles
        Linha(p, sz, new Vector2(cx - 5f, cx), new Vector2(cx + 2f, cx),
              new Color(cor.r, cor.g, cor.b, 0.4f), 1.5f);
    }

    static void DesenharSilhueta(Color[] p, int sz, float x, float y, Color cor, float alpha)
    {
        Elipse(p, sz, x, y + 10f, 4f, 4f, cor, 1.8f);   // cabeça
        Elipse(p, sz, x, y,       3.5f, 6f, cor, 1.8f); // corpo
        Linha(p, sz, new Vector2(x - 3f, y - 4f), new Vector2(x - 6f, y - 10f), cor, 1.5f); // perna
        Linha(p, sz, new Vector2(x + 3f, y - 4f), new Vector2(x + 5f, y - 10f), cor, 1.5f);
    }

    // NECRÓPOLE — crânio com coroa
    static void DesenharNecropole(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.6f);
        // Crânio
        Elipse(p, sz, cx, cx + 4f, 14f, 13f, cor, 1.8f);
        // Mandíbula (semicírculo inferior)
        Elipse(p, sz, cx, cx - 8f, 10f, 7f, cor, 1.8f);
        // Olhos (elipses vazias = fundo mais escuro)
        Color olho = new Color(cor.r * 0.1f, cor.g * 0.15f, cor.b * 0.1f);
        Elipse(p, sz, cx - 5f, cx + 5f, 3.5f, 3.5f, olho, 3f);
        Elipse(p, sz, cx + 5f, cx + 5f, 3.5f, 3.5f, olho, 3f);
        // Nariz triangular
        Elipse(p, sz, cx, cx - 1f, 2f, 1.5f, olho, 3f);
        // Coroa de pontas
        for (int i = 0; i < 5; i++)
        {
            float t = (i - 2) / 2f;
            float px = cx + t * 10f;
            float py = cx + 16f + (Mathf.Abs(t) < 0.5f ? 5f : 2f);
            Linha(p, sz, new Vector2(px, cx + 18f), new Vector2(px, py), brilho, 2f);
        }
    }

    // DOMO — cúpula de proteção
    static void DesenharDomo(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.65f);
        // Arco da cúpula
        int segs = 60;
        for (int i = 0; i <= segs; i++)
        {
            float t   = i / (float)segs;
            float ang = Mathf.PI * t; // 0 a PI (semicírculo superior)
            Vector2 pt = new Vector2(cx + Mathf.Cos(ang) * 22f, cx + Mathf.Sin(ang) * 22f);
            Elipse(p, sz, pt.x, pt.y, 2f, 2f, Color.Lerp(cor, brilho, t * (1f - t) * 4f), 2f);
        }
        // Anéis internos paralelos
        for (float r2 = 10f; r2 <= 18f; r2 += 5f)
        {
            for (int i = 0; i <= 30; i++)
            {
                float t = i / 30f; float ang = Mathf.PI * t;
                Vector2 pt = new Vector2(cx + Mathf.Cos(ang) * r2, cx + Mathf.Sin(ang) * r2);
                Px(p, sz, (int)pt.x, (int)pt.y, new Color(brilho.r, brilho.g, brilho.b, 0.4f), 0.8f);
            }
        }
        // Base horizontal
        Linha(p, sz, new Vector2(cx - 22f, cx), new Vector2(cx + 22f, cx), brilho, 2f);
        // Ponto de glow no topo
        Elipse(p, sz, cx, cx + 22f, 3f, 3f, Color.white, 4f);
    }

    // ESCUDO SÔNICO — anéis sônicos expansivos
    static void DesenharSonico(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.7f);
        // Silhueta no centro
        DesenharSilhueta(p, sz, cx, cx + 2f, cor, 0.8f);
        // 3 anéis expansivos
        float[] rs = { 8f, 15f, 22f };
        float[] alphas = { 0.9f, 0.6f, 0.35f };
        for (int i = 0; i < rs.Length; i++)
        {
            float esp = Mathf.Lerp(2.5f, 1f, i / 2f);
            Anel(p, sz, new Vector2(cx, cx), rs[i],
                 new Color(brilho.r, brilho.g, brilho.b, alphas[i]), esp);
        }
    }

    // CORRENTES DO INFERNO — correntes em chamas
    static void DesenharCorrente(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.6f);
        Color chama  = new Color(1f, 0.6f, 0f);
        // 2 correntes diagonais cruzadas
        Linha(p, sz, new Vector2(cx - 20f, cx - 20f), new Vector2(cx + 20f, cx + 20f), cor, 3.5f);
        Linha(p, sz, new Vector2(cx + 20f, cx - 20f), new Vector2(cx - 20f, cx + 20f), cor, 3.5f);
        // Elos sobre as correntes
        for (int i = 0; i < 4; i++)
        {
            float t = (i + 0.5f) / 4f;
            Vector2 pos1 = Vector2.Lerp(new Vector2(cx - 20, cx - 20), new Vector2(cx + 20, cx + 20), t);
            Anel(p, sz, pos1, 4f, brilho, 1.5f);
        }
        // Chamas no centro
        Elipse(p, sz, cx, cx + 4f, 5f, 7f, chama, 2f);
        Elipse(p, sz, cx, cx + 8f, 3f, 4f, Color.Lerp(chama, Color.white, 0.5f), 2.5f);
        Elipse(p, sz, cx, cx + 11f, 1.5f, 2f, Color.white, 3f);
    }

    // PUNIÇÃO DIVINA — raio dourado + cruz de luz
    static void DesenharDivina(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.8f);
        // Raio dourado vertical
        for (int y = 4; y < sz - 4; y++)
        {
            float t    = (y - 4f) / (sz - 8f);
            float larg = Mathf.Lerp(3.5f, 1f, Mathf.Abs(t - 0.5f) * 2f);
            Elipse(p, sz, cx, y, larg, 0.8f, Color.Lerp(cor, brilho, t < 0.5f ? t * 2f : (1f - t) * 2f), 2.5f);
        }
        // Cruz de luz horizontal
        Linha(p, sz, new Vector2(cx - 18f, cx), new Vector2(cx + 18f, cx), brilho, 3f);
        Linha(p, sz, new Vector2(cx - 18f, cx), new Vector2(cx + 18f, cx), Color.white, 1f);
        // Glow central
        Elipse(p, sz, cx, cx, 5f, 5f, brilho, 2.5f);
        Elipse(p, sz, cx, cx, 2.5f, 2.5f, Color.white, 4f);
        // Raios de luz nos 4 cantos
        for (int i = 0; i < 4; i++)
        {
            float ang = (i / 4f + 0.125f) * Mathf.PI * 2f;
            Linha(p, sz, new Vector2(cx, cx),
                  new Vector2(cx + Mathf.Cos(ang) * 20f, cx + Mathf.Sin(ang) * 20f),
                  new Color(brilho.r, brilho.g, brilho.b, 0.5f), 1.2f);
        }
    }

    // FANTASMA — espírito ascendendo
    static void DesenharFantasma(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.7f);
        // Forma fantasma (gota invertida com cauda ondulada)
        for (int y = 4; y < sz - 4; y++)
        {
            float t = (y - 4f) / (sz - 8f);
            float larg;
            if (t > 0.5f)
                larg = (1f - (t - 0.5f) * 2f) * 12f; // corpo superior
            else
                larg = 4f + Mathf.Sin(t * Mathf.PI * 3f) * 4f; // cauda ondulada

            float alpha = Mathf.Lerp(0.3f, 0.85f, t);
            Elipse(p, sz, cx, y, larg, 0.7f, new Color(brilho.r, brilho.g, brilho.b, alpha), 2f);
        }
        // Olhos
        Elipse(p, sz, cx - 4f, cx + 14f, 2.5f, 2.5f, new Color(0.1f, 0.1f, 0.2f), 3f);
        Elipse(p, sz, cx + 4f, cx + 14f, 2.5f, 2.5f, new Color(0.1f, 0.1f, 0.2f), 3f);
        // Aura ao redor
        Anel(p, sz, new Vector2(cx, cx + 8f), 16f, new Color(cor.r, cor.g, cor.b, 0.25f), 1f);
    }

    // DRENAGEM DE VIDA — coração + seta drenando
    static void DesenharDrenagem(Color[] p, int sz, float cx, Color cor)
    {
        Color verde  = new Color(0.2f, 0.9f, 0.3f);
        Color brilho = Color.Lerp(cor, Color.white, 0.6f);
        // Coração (inimigo, esquerda — vermelho escuro)
        Color cEnimigo = new Color(0.6f, 0.1f, 0.1f);
        DesenharCoracaoShape(p, sz, cx - 12f, cx + 4f, 8f, cEnimigo, 0.75f);
        // Coração (player, direita — cheio, verde)
        DesenharCoracaoShape(p, sz, cx + 12f, cx + 4f, 8f, verde, 0.9f);
        // Seta de drenagem (da esquerda para a direita)
        Linha(p, sz, new Vector2(cx - 5f, cx + 4f), new Vector2(cx + 5f, cx + 4f), brilho, 2f);
        Linha(p, sz, new Vector2(cx + 5f, cx + 4f), new Vector2(cx + 2f, cx + 1f), brilho, 1.5f);
        Linha(p, sz, new Vector2(cx + 5f, cx + 4f), new Vector2(cx + 2f, cx + 7f), brilho, 1.5f);
    }

    static void DesenharCoracaoShape(Color[] p, int sz, float hx, float hy, float r, Color cor, float alpha)
    {
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float nx = (x - hx) / (r * 0.65f), ny = (y - hy * 0.92f) / (r * 0.65f);
            float h = Mathf.Pow(nx * nx + ny * ny - 0.5f, 3f) - nx * nx * ny * ny * ny;
            if (h <= 0f) Px(p, sz, x, y, cor, alpha * Mathf.Clamp01((-h) * 3f + 0.5f));
        }
    }

    // PULSO MAGNÉTICO — linhas de campo magnético
    static void DesenharMagnetico(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.65f);
        // Polo norte e sul
        Elipse(p, sz, cx, cx + 18f, 5f, 4f, brilho, 2.5f);
        Elipse(p, sz, cx, cx - 18f, 5f, 4f, cor, 2.5f);
        // Letras N/S simplificadas como marcas
        Linha(p, sz, new Vector2(cx - 2f, cx + 21f), new Vector2(cx - 2f, cx + 15f), Color.white, 1f);
        Linha(p, sz, new Vector2(cx - 2f, cx + 21f), new Vector2(cx + 2f, cx + 15f), Color.white, 1f);
        Linha(p, sz, new Vector2(cx + 2f, cx + 15f), new Vector2(cx + 2f, cx + 21f), Color.white, 1f);
        // Linhas de campo curvas (elipses achatadas ao redor)
        for (int i = 0; i < 4; i++)
        {
            float t   = (i + 1) / 4f;
            float rx2 = 8f + t * 14f, ry2 = 10f + t * 6f;
            Anel(p, sz, new Vector2(cx, cx), Mathf.Max(rx2, ry2) * 0.7f,
                 new Color(brilho.r, brilho.g, brilho.b, 0.4f - t * 0.1f), 1f);
        }
    }

    // CORAÇÃO ROBUSTO — coração com armadura
    static void DesenharCoracao(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho  = Color.Lerp(cor, Color.white, 0.6f);
        Color armadura = new Color(0.7f, 0.7f, 0.8f);
        // Coração base
        DesenharCoracaoShape(p, sz, cx, cx * 0.92f, sz * 0.42f, cor, 0.9f);
        // Placa de armadura no centro
        Elipse(p, sz, cx, cx - 2f, 8f, 10f, armadura, 2f);
        // Cruz na armadura
        Linha(p, sz, new Vector2(cx - 5f, cx - 2f), new Vector2(cx + 5f, cx - 2f), Color.white, 1.5f);
        Linha(p, sz, new Vector2(cx, cx + 4f),      new Vector2(cx, cx - 8f),      Color.white, 1.5f);
        // Brilho no topo
        Elipse(p, sz, cx - 4f, cx + 9f, 3f, 2.5f, new Color(brilho.r, brilho.g, brilho.b, 0.6f), 2f);
    }

    // COLHEITA — foice
    static void DesenharColheita(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.7f);
        Color cabo   = new Color(cor.r * 0.4f, cor.g * 0.3f, cor.b * 0.1f);
        // Cabo longo diagonal
        Linha(p, sz, new Vector2(cx - 18f, cx - 22f), new Vector2(cx + 10f, cx + 18f), cabo, 2.5f);
        // Lâmina da foice (arco)
        int steps = 40;
        Vector2 prev = Vector2.zero;
        for (int i = 0; i <= steps; i++)
        {
            float t   = i / (float)steps;
            float ang = Mathf.Lerp(Mathf.PI * 0.1f, Mathf.PI * 1.1f, t);
            float r2  = Mathf.Lerp(14f, 18f, Mathf.Sin(t * Mathf.PI));
            Vector2 pt = new Vector2(cx + 6f + Mathf.Cos(ang) * r2, cx + 4f + Mathf.Sin(ang) * r2);
            if (i > 0)
            {
                Linha(p, sz, prev, pt, new Color(brilho.r, brilho.g, brilho.b, 0.5f), 3.5f);
                Linha(p, sz, prev, pt, brilho, 2f);
                Linha(p, sz, prev, pt, Color.white, 0.8f);
            }
            prev = pt;
        }
    }

    // ANCIÃO — cajado com orbe
    static void DesenharAnciao(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.7f);
        Color madeira = new Color(0.6f, 0.45f, 0.2f);
        // Cajado
        Linha(p, sz, new Vector2(cx + 4f, cx - 24f), new Vector2(cx - 2f, cx + 24f), madeira, 2.5f);
        // Cruzeta no topo
        Linha(p, sz, new Vector2(cx - 4f, cx - 18f), new Vector2(cx + 12f, cx - 18f), madeira, 2f);
        // Orbe de energia no topo
        Elipse(p, sz, cx + 4f, cx - 22f, 7f, 7f, new Color(cor.r, cor.g, cor.b, 0.5f), 2f);
        Elipse(p, sz, cx + 4f, cx - 22f, 5f, 5f, brilho, 2.5f);
        Elipse(p, sz, cx + 4f, cx - 22f, 2.5f, 2.5f, Color.white, 4f);
        // Runas ao redor do cajado
        for (int i = 0; i < 3; i++)
        {
            float y = cx - 8f + i * 8f;
            Elipse(p, sz, cx + 2f, y, 1.5f, 1.5f, brilho, 3f);
        }
    }

    // BÊNÇÃO DO ANCIÃO — raios de bênção dourada
    static void DesenharBencao(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.75f);
        // Halo (anel dourado no topo)
        Anel(p, sz, new Vector2(cx, cx + 16f), 10f, brilho, 2.5f);
        Elipse(p, sz, cx, cx + 16f, 4f, 4f, new Color(brilho.r, brilho.g, brilho.b, 0.3f), 2f);
        // Raios de luz descendo
        for (int i = 0; i < 5; i++)
        {
            float x = cx - 16f + i * 8f;
            float alpha = i == 2 ? 0.9f : 0.55f;
            float larg  = i == 2 ? 2.5f : 1.5f;
            Linha(p, sz, new Vector2(x, cx + 10f), new Vector2(x + (i-2f)*2f, cx - 22f),
                  new Color(brilho.r, brilho.g, brilho.b, alpha), larg);
        }
        // Silhueta recebendo bênção
        DesenharSilhueta(p, sz, cx, cx - 6f, cor, 0.7f);
    }

    // CAÇADOR — arco e flecha
    static void DesenharCacador(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.65f);
        Color madeira = new Color(0.5f, 0.35f, 0.15f);
        // Arco (arco curvo)
        int steps = 30;
        Vector2 prev = Vector2.zero;
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            float ang = Mathf.Lerp(-Mathf.PI * 0.4f, Mathf.PI * 0.4f, t);
            float bow = 20f - Mathf.Cos(ang) * 5f;
            Vector2 pt = new Vector2(cx - 14f + Mathf.Sin(ang) * 2f, cx + Mathf.Sin(ang) * bow);
            if (i > 0) Linha(p, sz, prev, pt, madeira, 2.5f);
            prev = pt;
        }
        // Corda do arco
        Linha(p, sz, new Vector2(cx - 12f, cx - 20f), new Vector2(cx - 12f, cx + 20f), new Color(0.8f, 0.8f, 0.7f), 1f);
        // Flecha
        Linha(p, sz, new Vector2(cx - 12f, cx), new Vector2(cx + 20f, cx), brilho, 1.5f);
        // Ponta da flecha
        Linha(p, sz, new Vector2(cx + 20f, cx), new Vector2(cx + 14f, cx - 4f), brilho, 1.5f);
        Linha(p, sz, new Vector2(cx + 20f, cx), new Vector2(cx + 14f, cx + 4f), brilho, 1.5f);
        // Alvo (círculos concêntricos, direita)
        Anel(p, sz, new Vector2(cx + 18f, cx), 6f, new Color(cor.r, cor.g, cor.b, 0.5f), 1.2f);
        Anel(p, sz, new Vector2(cx + 18f, cx), 3f, new Color(cor.r, cor.g, cor.b, 0.7f), 1f);
    }

    // CASULO DE CRISTAL — casulo hexagonal de cristal
    static void DesenharCasulo(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.75f);
        // Casulo hexagonal (6 lados)
        Vector2[] hex = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            float ang = (i / 6f - 0.25f) * Mathf.PI * 2f;
            hex[i] = new Vector2(cx + Mathf.Cos(ang) * 20f, cx + Mathf.Sin(ang) * 20f);
        }
        for (int i = 0; i < 6; i++)
            Linha(p, sz, hex[i], hex[(i + 1) % 6], brilho, 2.5f);
        // Linhas internas do cristal
        for (int i = 0; i < 6; i += 2)
            Linha(p, sz, hex[i], new Vector2(cx, cx), new Color(brilho.r, brilho.g, brilho.b, 0.35f), 1f);
        // Silhueta interna
        DesenharSilhueta(p, sz, cx, cx - 2f, new Color(brilho.r, brilho.g, brilho.b, 0.6f), 0.7f);
        // Brilho de cristal (especular)
        Elipse(p, sz, cx + 10f, cx + 14f, 3f, 2f, Color.white, 2.5f);
        Elipse(p, sz, cx - 8f, cx + 10f,  2f, 1.5f, Color.white, 2f);
    }

    // RITUAL DO ANCIÃO — círculo mágico com runas
    static void DesenharRitual(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.65f);
        // Círculo externo
        Anel(p, sz, new Vector2(cx, cx), 22f, brilho, 2f);
        // Pentagrama interno
        for (int i = 0; i < 5; i++)
        {
            float a1 = (i / 5f - 0.25f) * Mathf.PI * 2f;
            float a2 = ((i + 2) / 5f - 0.25f) * Mathf.PI * 2f;
            Vector2 p1 = new Vector2(cx + Mathf.Cos(a1) * 16f, cx + Mathf.Sin(a1) * 16f);
            Vector2 p2 = new Vector2(cx + Mathf.Cos(a2) * 16f, cx + Mathf.Sin(a2) * 16f);
            Linha(p, sz, p1, p2, new Color(brilho.r, brilho.g, brilho.b, 0.7f), 1.5f);
        }
        // Runas (pontos nos vértices)
        for (int i = 0; i < 5; i++)
        {
            float a = (i / 5f - 0.25f) * Mathf.PI * 2f;
            Vector2 rp = new Vector2(cx + Mathf.Cos(a) * 16f, cx + Mathf.Sin(a) * 16f);
            Elipse(p, sz, rp.x, rp.y, 2.5f, 2.5f, brilho, 3f);
        }
        // Orbe central
        Elipse(p, sz, cx, cx, 5f, 5f, new Color(cor.r, cor.g, cor.b, 0.5f), 2.5f);
        Elipse(p, sz, cx, cx, 2.5f, 2.5f, Color.white, 4f);
    }

}
#endif
