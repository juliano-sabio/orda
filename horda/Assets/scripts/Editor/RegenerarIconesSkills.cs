#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Regenera todos os ícones de skill com fundo colorido e formas melhoradas.
/// Menu: Tools/Skills/Regenerar Todos os Ícones
/// </summary>
public static class RegenerarIconesSkills
{
    const string PASTA = "Assets/Skills";
    const int    SZ    = 64;

    // ── Mapa de todas as skills: (id, cor, tipo de forma) ─────────────────────
    static readonly (string id, Color cor, string forma)[] SKILLS =
    {
        // Ataque
        ("LancaLuz",           new Color(1.00f, 0.90f, 0.30f), "lanca"),
        ("ChicoteEnergia",     new Color(0.20f, 0.80f, 1.00f), "chicote"),
        ("MisseisTeleguiados", new Color(1.00f, 0.45f, 0.10f), "misseis"),
        ("PulsoRitmico",       new Color(0.25f, 0.90f, 0.45f), "pulso"),
        ("EspadaFantasma",     new Color(0.80f, 0.80f, 1.00f), "espada"),
        ("CorrenteSombria",    new Color(0.55f, 0.12f, 0.90f), "corrente"),
        ("CampoEspinhos",      new Color(0.30f, 0.80f, 0.25f), "espinhos"),
        ("ChuvaEstrelas",      new Color(0.75f, 0.75f, 1.00f), "estrelas"),
        ("FuriaLaminas",       new Color(0.95f, 0.20f, 0.20f), "laminas"),
        ("SombrasCruz",        new Color(0.45f, 0.10f, 0.85f), "cruz"),
        ("CorteFantasma",      new Color(0.55f, 0.90f, 0.90f), "corte"),
        ("GarrasAbismo",       new Color(0.70f, 0.10f, 0.50f), "garras"),
        // Defesa
        ("SegundaChance",         new Color(1.00f, 0.85f, 0.10f), "coracao"),
        ("FugaSombras",           new Color(0.50f, 0.10f, 0.90f), "seta"),
        ("BarreiraEnergia",       new Color(0.20f, 0.60f, 1.00f), "escudo"),
        ("TeiaProtecao",          new Color(0.30f, 1.00f, 0.50f), "teia"),
        ("InstintoSobrevivencia", new Color(1.00f, 0.55f, 0.10f), "chama"),
        ("EspelhoMagico",         new Color(0.60f, 0.90f, 1.00f), "espelho"),
        ("EscudoKarma",           new Color(1.00f, 0.85f, 0.20f), "karma"),
        // Nova skill de ataque
        ("CristaisGelo",          new Color(0.45f, 0.88f, 1.00f), "cristaisgelo"),
        // Novas skills de defesa
        ("EscudoEspinhoso",       new Color(0.25f, 0.90f, 0.30f), "escudoespinho"),
        ("Aureola",               new Color(1.00f, 0.88f, 0.22f), "aureola"),
        ("BarreiraReflexiva",     new Color(0.35f, 0.88f, 1.00f), "reflexiva"),
    };

    [MenuItem("Tools/Skills/Regenerar Todos os Ícones")]
    public static void RegenerarTodos()
    {
        int ok = 0;
        foreach (var (id, cor, forma) in SKILLS)
        {
            string iconPath = $"{PASTA}/{id}Icon.png";

            // Força deleção para recriar
            if (File.Exists(Path.Combine(Application.dataPath, "../" + iconPath)))
            {
                AssetDatabase.DeleteAsset(iconPath);
                AssetDatabase.Refresh();
            }

            var sprite = GerarIcone(id, cor, forma);
            if (sprite == null) continue;

            // Atualiza o SkillData se existir
            var skill = AssetDatabase.LoadAssetAtPath<SkillData>($"{PASTA}/{id}.asset");
            if (skill != null) { skill.icon = sprite; EditorUtility.SetDirty(skill); }
            ok++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"✅ {ok}/{SKILLS.Length} ícones regenerados com novo estilo!");
    }

    // ── Geração ───────────────────────────────────────────────────────────────

    static Sprite GerarIcone(string id, Color cor, string forma)
    {
        string iconPath = $"{PASTA}/{id}Icon.png";
        var pixels = new Color[SZ * SZ];
        float cx = SZ * 0.5f;

        GerarFundo(pixels, SZ, cx, cor);

        switch (forma)
        {
            case "lanca":    DesenharLanca(pixels, SZ, cx, cor);    break;
            case "chicote":  DesenharChicote(pixels, SZ, cx, cor);  break;
            case "misseis":  DesenharMisseis(pixels, SZ, cx, cor);  break;
            case "pulso":    DesenharPulso(pixels, SZ, cx, cor);    break;
            case "espada":   DesenharEspada(pixels, SZ, cx, cor);   break;
            case "corrente": DesenharCorrente(pixels, SZ, cx, cor); break;
            case "espinhos": DesenharEspinhos(pixels, SZ, cx, cor); break;
            case "estrelas": DesenharEstrelas(pixels, SZ, cx, cor); break;
            case "laminas":  DesenharLaminas(pixels, SZ, cx, cor);  break;
            case "cruz":     DesenharCruz(pixels, SZ, cx, cor);     break;
            case "corte":    DesenharCorte(pixels, SZ, cx, cor);    break;
            case "garras":   DesenharGarras(pixels, SZ, cx, cor);   break;
            case "coracao":  DesenharCoracao(pixels, SZ, cx, cor);  break;
            case "seta":     DesenharSeta(pixels, SZ, cx, cor);     break;
            case "escudo":   DesenharEscudo(pixels, SZ, cx, cor);   break;
            case "teia":     DesenharTeia(pixels, SZ, cx, cor);     break;
            case "chama":    DesenharChama(pixels, SZ, cx, cor);    break;
            case "espelho":  DesenharEspelho(pixels, SZ, cx, cor);  break;
            case "karma":         DesenharKarma(pixels, SZ, cx, cor);         break;
            case "escudoespinho": DesenharEscudoEspinho(pixels, SZ, cx, cor); break;
            case "aureola":       DesenharAureolaIcon(pixels, SZ, cx, cor);   break;
            case "reflexiva":     DesenharReflexiva(pixels, SZ, cx, cor);     break;
            case "cristaisgelo":  DesenharCristaisGelo(pixels, SZ, cx, cor);  break;
        }

        var tex = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false);
        tex.SetPixels(pixels);
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);

        if (!Directory.Exists(Application.dataPath + "/../" + PASTA))
            Directory.CreateDirectory(Application.dataPath + "/../" + PASTA);

        File.WriteAllBytes(Path.Combine(Application.dataPath, "../" + iconPath), png);
        AssetDatabase.Refresh();

        var imp = AssetImporter.GetAtPath(iconPath) as TextureImporter;
        if (imp != null)
        {
            imp.textureType         = TextureImporterType.Sprite;
            imp.spritePixelsPerUnit = SZ;
            imp.filterMode          = FilterMode.Bilinear;
            imp.alphaIsTransparency = true;
            imp.alphaSource         = TextureImporterAlphaSource.FromInput;
            imp.mipmapEnabled       = false;
            imp.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
    }

    // ── FUNDO COLORIDO ────────────────────────────────────────────────────────

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

            // Gradiente: centro rico → meio médio → borda escura (mas colorida)
            Color c0 = new Color(cor.r * 0.55f, cor.g * 0.55f, cor.b * 0.65f); // centro
            Color c1 = new Color(cor.r * 0.25f, cor.g * 0.25f, cor.b * 0.32f); // meio
            Color c2 = new Color(cor.r * 0.08f, cor.g * 0.08f, cor.b * 0.13f); // borda

            Color bg = t < 0.45f
                ? Color.Lerp(c0, c1, t / 0.45f)
                : Color.Lerp(c1, c2, (t - 0.45f) / 0.55f);

            // Brilho diagonal (superior esquerdo)
            float sheen = Mathf.Clamp01(1f - (dx + 1.5f * dy + sz * 0.8f) / (sz * 1.2f)) * 0.13f;
            bg.r = Mathf.Clamp01(bg.r + sheen);
            bg.g = Mathf.Clamp01(bg.g + sheen);
            bg.b = Mathf.Clamp01(bg.b + sheen * 1.2f);

            // Glow na borda do círculo
            float rim = Mathf.Clamp01((t - 0.80f) / 0.20f);
            Color rimC = new Color(cor.r * 0.55f, cor.g * 0.55f, cor.b * 0.75f);
            bg = Color.Lerp(bg, rimC, rim * 0.55f);

            p[y * sz + x] = new Color(bg.r, bg.g, bg.b, a);
        }
    }

    // ── HELPERS DE DESENHO ────────────────────────────────────────────────────

    static void Px(Color[] p, int sz, int x, int y, Color c, float f = 1f)
    {
        if (x < 0 || x >= sz || y < 0 || y >= sz) return;
        p[y * sz + x] = Color.Lerp(p[y * sz + x], c, Mathf.Clamp01(f));
    }

    // Pinta elipse suave (coordenadas: centro cx,cy; raios rx,ry)
    static void PintarElipse(Color[] p, int sz, float ecx, float ecy, float rx, float ry, Color cor, float borda = 1.5f)
    {
        int x0 = Mathf.Max(0, (int)(ecx - rx - 2));
        int x1 = Mathf.Min(sz - 1, (int)(ecx + rx + 2));
        int y0 = Mathf.Max(0, (int)(ecy - ry - 2));
        int y1 = Mathf.Min(sz - 1, (int)(ecy + ry + 2));
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            float ndx = (x - ecx) / rx, ndy = (y - ecy) / ry;
            float d = Mathf.Sqrt(ndx * ndx + ndy * ndy);
            float f = Mathf.Clamp01((1f - d) * borda);
            if (f > 0f)
            {
                // Highlight no canto superior esquerdo
                float hl = Mathf.Clamp01(1f + (ecx - x + ecy - y) / (rx * 4f)) * 0.35f;
                Px(p, sz, x, y, Color.Lerp(cor, Color.white, hl), f);
            }
        }
    }

    // Linha com espessura suave
    static void Linha(Color[] p, int sz, Vector2 de, Vector2 ate, Color cor, float espessura = 2f)
    {
        int steps = Mathf.Max(1, (int)(Vector2.Distance(de, ate) * 2f));
        for (int i = 0; i <= steps; i++)
        {
            float t  = i / (float)steps;
            Vector2 pt = Vector2.Lerp(de, ate, t);
            int raio = Mathf.CeilToInt(espessura);
            for (int dy = -raio; dy <= raio; dy++)
            for (int dx = -raio; dx <= raio; dx++)
            {
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float f = Mathf.Clamp01(espessura - dist);
                Px(p, sz, (int)pt.x + dx, (int)pt.y + dy, cor, f);
            }
        }
    }

    // Anel suave
    static void Anel(Color[] p, int sz, Vector2 centro, float raio, Color cor, float espessura = 2f)
    {
        int segs = Mathf.Max(32, (int)(raio * Mathf.PI * 2));
        for (int i = 0; i < segs; i++)
        {
            float ang = i / (float)segs * Mathf.PI * 2f;
            Vector2 pt = centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raio;
            int r2 = Mathf.CeilToInt(espessura);
            for (int dy = -r2; dy <= r2; dy++)
            for (int dx = -r2; dx <= r2; dx++)
            {
                float f = Mathf.Clamp01(espessura - Mathf.Sqrt(dx * dx + dy * dy));
                Px(p, sz, (int)pt.x + dx, (int)pt.y + dy, cor, f);
            }
        }
    }

    // ── FORMAS ────────────────────────────────────────────────────────────────

    // LANÇA DE LUZ — lança vertical com ponta brilhante
    static void DesenharLanca(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.6f);
        Color sombra  = new Color(cor.r * 0.5f, cor.g * 0.5f, cor.b * 0.3f);

        // Cabo
        Linha(p, sz, new Vector2(cx, 8), new Vector2(cx, 42), sombra, 2f);
        // Lâmina (corpo)
        for (int y = 20; y <= 56; y++)
        {
            float ny = (y - 20f) / 36f; // 0=base, 1=ponta
            float larg = Mathf.Lerp(6f, 0.5f, ny * ny);
            PintarElipse(p, sz, cx, y, larg, 1f, Color.Lerp(cor, brilho, ny * 0.5f), 2f);
        }
        // Guarda cruzada
        Linha(p, sz, new Vector2(cx - 10, 24), new Vector2(cx + 10, 24), cor, 2f);
        // Brilho na ponta
        PintarElipse(p, sz, cx, 56, 2.5f, 2.5f, Color.white, 3f);
        // Reflexo lateral
        Linha(p, sz, new Vector2(cx + 2, 30), new Vector2(cx + 1, 50), Color.Lerp(cor, Color.white, 0.7f), 1f);
    }

    // CHICOTE DE ENERGIA — espiral circular representando 360°
    static void DesenharChicote(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.5f);
        Vector2 centro = new Vector2(cx, cx);

        // Espiral externa (chicote)
        int segs = 120;
        for (int i = 0; i < segs; i++)
        {
            float t   = i / (float)(segs - 1);
            float ang = t * Mathf.PI * 2.2f - Mathf.PI * 0.5f;
            float r   = Mathf.Lerp(22f, 8f, t);
            Vector2 pt = centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
            float esp = Mathf.Lerp(3f, 1f, t);
            Color c   = Color.Lerp(cor, brilho, 1f - t);
            int ri = Mathf.CeilToInt(esp);
            for (int dy = -ri; dy <= ri; dy++)
            for (int dx = -ri; dx <= ri; dx++)
            {
                float f = Mathf.Clamp01(esp - Mathf.Sqrt(dx * dx + dy * dy));
                Px(p, sz, (int)pt.x + dx, (int)pt.y + dy, c, f);
            }
        }

        // Ponta brilhante
        Vector2 ponta = centro + new Vector2(Mathf.Cos(-Mathf.PI * 0.5f + Mathf.PI * 2.2f), Mathf.Sin(-Mathf.PI * 0.5f + Mathf.PI * 2.2f)) * 8f;
        PintarElipse(p, sz, ponta.x, ponta.y, 3f, 3f, Color.white, 3f);

        // Empunhadura
        PintarElipse(p, sz, cx, cx + 5, 4f, 4f, new Color(cor.r * 0.7f, cor.g * 0.7f, cor.b * 0.5f), 2f);
    }

    // MÍSSEIS — 3 foguetes em formação triangular
    static void DesenharMisseis(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho  = Color.Lerp(cor, Color.white, 0.55f);
        Color exaust  = new Color(1f, 0.7f, 0.1f);
        // Posições dos 3 mísseis
        Vector2[] pos = {
            new Vector2(cx - 13, cx - 5),
            new Vector2(cx + 13, cx - 5),
            new Vector2(cx,      cx + 12)
        };
        float[] angs = { -30f, 30f, 90f }; // ângulo de cada míssil (graus, sentido norte)

        for (int m = 0; m < 3; m++)
        {
            Vector2 mc  = pos[m];
            float   rad = angs[m] * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
            Vector2 perp = new Vector2(-dir.y, dir.x);

            // Corpo do foguete
            for (int i = -10; i <= 10; i++)
            {
                float t  = (i + 10f) / 20f; // 0=base, 1=ponta
                float larg = t > 0.7f ? (1f - t) / 0.3f * 3f : 3f;
                Vector2 cp = mc + dir * i;
                for (float f2 = -larg; f2 <= larg; f2 += 0.5f)
                {
                    Vector2 fp = cp + perp * f2;
                    float force = Mathf.Clamp01((larg - Mathf.Abs(f2)) * 0.8f);
                    Color c = Color.Lerp(cor, brilho, t * 0.6f);
                    Px(p, sz, (int)fp.x, (int)fp.y, c, force);
                }
            }

            // Chama de propulsão
            for (int i = -14; i <= -10; i++)
            {
                float t = (-i - 10) / 4f;
                float larg = (1f - t) * 2.5f;
                Vector2 cp = mc + dir * i;
                for (float f2 = -larg; f2 <= larg; f2 += 0.5f)
                {
                    Color c = Color.Lerp(exaust, Color.white, 0.3f);
                    Px(p, sz, (int)(cp.x + perp.x * f2), (int)(cp.y + perp.y * f2), c, 0.8f * (1f - t));
                }
            }
        }
    }

    // PULSO RÍTMICO — anéis concêntricos com brilho
    static void DesenharPulso(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.5f);
        Vector2 centro = new Vector2(cx, cx);

        float[] raios = { 7f, 14f, 21f };
        float[] esps  = { 3f, 2.5f, 2f };
        for (int i = 0; i < raios.Length; i++)
        {
            float forca = Mathf.Lerp(1f, 0.55f, i / 2f);
            Color c = Color.Lerp(brilho, cor, i / 2f);
            Anel(p, sz, centro, raios[i], c * forca, esps[i]);
        }
        // Núcleo central
        PintarElipse(p, sz, cx, cx, 5f, 5f, brilho, 3f);
        PintarElipse(p, sz, cx, cx, 2.5f, 2.5f, Color.white, 4f);
    }

    // ESPADA FANTASMA — lâmina longa com brilho espectral
    static void DesenharEspada(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.7f);
        Color sombra  = new Color(cor.r * 0.3f, cor.g * 0.3f, cor.b * 0.5f);

        // Lâmina principal (diagonal)
        Vector2 ponta = new Vector2(cx + 18, cx + 18);
        Vector2 base_ = new Vector2(cx - 18, cx - 18);
        Linha(p, sz, base_, ponta, brilho, 3.5f);
        // Fio da lâmina
        Linha(p, sz, new Vector2(cx - 16, cx - 17), new Vector2(cx + 18, cx + 18), Color.white, 1f);
        // Guarda
        Linha(p, sz, new Vector2(cx - 13, cx + 10), new Vector2(cx + 5, cx - 8), cor, 2.5f);
        // Cabo
        Linha(p, sz, new Vector2(cx - 18, cx - 18), new Vector2(cx - 24, cx - 24), sombra, 3f);
        // Efeito espectral (glow translúcido ao redor)
        for (int i = 0; i < 5; i++)
        {
            float t = i / 5f;
            float r = Mathf.Lerp(22f, 4f, t);
            float ang = Mathf.PI * 0.25f + t * 0.3f;
            Vector2 gp = new Vector2(cx + Mathf.Cos(ang) * r, cx + Mathf.Sin(ang) * r);
            PintarElipse(p, sz, gp.x, gp.y, 3f, 3f, new Color(cor.r, cor.g, cor.b, 0.3f), 1.5f);
        }
    }

    // CORRENTE SOMBRIA — 3 nós conectados por correntes
    static void DesenharCorrente(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.45f);
        Vector2[] nos = {
            new Vector2(cx - 16, cx + 12),
            new Vector2(cx + 16, cx + 4),
            new Vector2(cx - 2,  cx - 16)
        };

        // Correntes entre nós
        for (int s = 0; s < 3; s++)
        {
            Vector2 de = nos[s], ate = nos[(s + 1) % 3];
            int steps = 20;
            for (int i = 0; i <= steps; i++)
            {
                float t   = i / (float)steps;
                float desvio = Mathf.Sin(t * Mathf.PI) * 4f;
                Vector2 perp = Vector2.Perpendicular((ate - de).normalized);
                Vector2 pt   = Vector2.Lerp(de, ate, t) + perp * desvio;
                PintarElipse(p, sz, pt.x, pt.y, 2.2f, 2.2f, Color.Lerp(cor, brilho, t), 2f);
            }
        }
        // Elos (ovaletes)
        for (int s = 0; s < 3; s++)
        {
            Vector2 de = nos[s], ate = nos[(s + 1) % 3];
            for (int i = 1; i < 4; i++)
            {
                float t = i / 4f;
                Vector2 pt = Vector2.Lerp(de, ate, t);
                PintarElipse(p, sz, pt.x, pt.y, 4f, 2.5f, cor, 2f);
            }
        }
        // Nós
        foreach (var no in nos)
            PintarElipse(p, sz, no.x, no.y, 4.5f, 4.5f, brilho, 3f);
    }

    // CAMPO DE ESPINHOS — espinhos radiando do centro
    static void DesenharEspinhos(Color[] p, int sz, float cx, Color cor)
    {
        Color ponta = Color.Lerp(cor, Color.white, 0.6f);
        Vector2 centro = new Vector2(cx, cx);
        int qtd = 8;
        for (int i = 0; i < qtd; i++)
        {
            float ang = i / (float)qtd * Mathf.PI * 2f;
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            // Espinho: base larga → ponta fina
            for (float d = 4f; d <= 26f; d += 0.5f)
            {
                float t = (d - 4f) / 22f;
                float larg = Mathf.Lerp(3.5f, 0.3f, t * t);
                Vector2 pt = centro + dir * d;
                Vector2 perp = new Vector2(-dir.y, dir.x);
                for (float w = -larg; w <= larg; w += 0.5f)
                {
                    Vector2 fp = pt + perp * w;
                    float f = Mathf.Clamp01((larg - Mathf.Abs(w)) * 1.2f);
                    Px(p, sz, (int)fp.x, (int)fp.y, Color.Lerp(cor, ponta, t), f);
                }
            }
        }
        // Centro
        PintarElipse(p, sz, cx, cx, 5f, 5f, ponta, 3f);
    }

    // CHUVA DE ESTRELAS — estrelas caindo
    static void DesenharEstrelas(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.white;
        // Estrela grande central
        DesenharEstrelaPt(p, sz, new Vector2(cx, cx + 6), 11f, cor, brilho);
        // Estrelas pequenas
        DesenharEstrelaPt(p, sz, new Vector2(cx - 16, cx + 16), 5f, cor, brilho);
        DesenharEstrelaPt(p, sz, new Vector2(cx + 16, cx + 14), 4f, cor, brilho);
        // Rastros de queda (linhas diagonais)
        Linha(p, sz, new Vector2(cx - 6, cx - 6), new Vector2(cx + 4, cx + 10), new Color(cor.r, cor.g, cor.b, 0.4f), 1f);
        Linha(p, sz, new Vector2(cx - 18, cx - 4), new Vector2(cx - 12, cx + 4), new Color(cor.r, cor.g, cor.b, 0.4f), 1f);
        Linha(p, sz, new Vector2(cx + 10, cx - 8), new Vector2(cx + 16, cx + 2), new Color(cor.r, cor.g, cor.b, 0.35f), 1f);
    }

    static void DesenharEstrelaPt(Color[] p, int sz, Vector2 centro, float raio, Color cor, Color brilho)
    {
        int pts = 5;
        for (int i = 0; i < pts; i++)
        {
            float angExt = i / (float)pts * Mathf.PI * 2f - Mathf.PI * 0.5f;
            float angInt = (i + 0.5f) / (float)pts * Mathf.PI * 2f - Mathf.PI * 0.5f;
            Vector2 ext  = centro + new Vector2(Mathf.Cos(angExt), Mathf.Sin(angExt)) * raio;
            Vector2 intr = centro + new Vector2(Mathf.Cos(angInt), Mathf.Sin(angInt)) * (raio * 0.42f);
            Vector2 ext2 = centro + new Vector2(Mathf.Cos(angExt + Mathf.PI * 2f / pts), Mathf.Sin(angExt + Mathf.PI * 2f / pts)) * raio;
            Linha(p, sz, ext, intr, Color.Lerp(cor, brilho, 0.4f), 1.5f);
            Linha(p, sz, intr, ext2, Color.Lerp(cor, brilho, 0.4f), 1.5f);
        }
        PintarElipse(p, sz, centro.x, centro.y, raio * 0.25f, raio * 0.25f, brilho, 3f);
    }

    // FÚRIA DE LÂMINAS — 4 lâminas varridas rasterizadas por polígono
    static void DesenharLaminas(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.78f);
        Color sombra = new Color(cor.r * 0.28f, cor.g * 0.22f, cor.b * 0.12f);

        // Vértices da lâmina no espaço local
        // +Y = radial para fora,  +X = sentido anti-horário (leading edge)
        Vector2[] local = {
            new Vector2( 2.5f,  3.5f),  // 0 — base leading
            new Vector2(-2.5f,  3.5f),  // 1 — base trailing
            new Vector2(-7f,   13f),    // 2 — mid trailing (varre para trás)
            new Vector2(-5.5f, 22f),    // 3 — outer trailing
            new Vector2(-1f,   27f),    // 4 — tip (ponta)
            new Vector2( 5f,   22f),    // 5 — outer leading
            new Vector2( 5f,   12f),    // 6 — mid leading
        };

        const int N = 4;
        for (int blade = 0; blade < N; blade++)
        {
            float ang  = blade / (float)N * Mathf.PI * 2f;
            float cosA = Mathf.Cos(ang), sinA = Mathf.Sin(ang);

            // Vértices no espaço de mundo
            var world = new Vector2[local.Length];
            for (int k = 0; k < local.Length; k++)
                world[k] = new Vector2(
                    cx + local[k].x * cosA - local[k].y * sinA,
                    cx + local[k].x * sinA + local[k].y * cosA);

            // AABB
            float x0 = float.MaxValue, x1 = float.MinValue;
            float y0 = float.MaxValue, y1 = float.MinValue;
            foreach (var v in world)
            { x0 = Mathf.Min(x0,v.x); x1 = Mathf.Max(x1,v.x); y0 = Mathf.Min(y0,v.y); y1 = Mathf.Max(y1,v.y); }

            for (int py = Mathf.Max(0,(int)y0-1); py <= Mathf.Min(sz-1,(int)y1+1); py++)
            for (int px = Mathf.Max(0,(int)x0-1); px <= Mathf.Min(sz-1,(int)x1+1); px++)
            {
                // Subpixel 2×2 para anti-aliasing
                float cov = 0f;
                float[] off = { -0.33f, 0.33f };
                foreach (float ox in off)
                foreach (float oy in off)
                    if (PontoDentroPoligono(world, new Vector2(px+0.5f+ox, py+0.5f+oy)))
                        cov += 0.25f;
                if (cov < 0.01f) continue;

                // Posição no espaço local (para shading)
                float wdx = px+0.5f - cx, wdy = py+0.5f - cx;
                float lx2 =  wdx * cosA + wdy * sinA;  // tangencial (leading = positivo)
                float ly2 = -wdx * sinA + wdy * cosA;  // radial
                float tRad  = Mathf.Clamp01((ly2 - 3.5f) / 23.5f); // 0=base → 1=ponta
                float tAng  = Mathf.Clamp01(lx2 / 6f + 0.5f);      // 0=trailing → 1=leading

                // Shading: leading edge brilhante, trailing escuro, mais escuro na ponta
                float bright = tAng * (1f - tRad * 0.3f);
                Color c = Color.Lerp(
                    Color.Lerp(sombra, cor, tAng * 0.85f + 0.1f),
                    Color.Lerp(cor, brilho, bright),
                    Mathf.Clamp01(bright * 1.5f));
                Px(p, sz, px, py, c, cov);
            }

            // Fio de corte branco na leading edge (v6 → v5 → v4)
            Linha(p, sz, world[0], world[6], Color.Lerp(brilho, Color.white, 0.6f), 1.2f);
            Linha(p, sz, world[6], world[5], Color.white, 1.3f);
            Linha(p, sz, world[5], world[4], Color.white, 1.1f);
        }

        // Rastros de giro (arcos atrás de cada lâmina)
        for (int blade = 0; blade < N; blade++)
        {
            float baseAng = blade / (float)N * Mathf.PI * 2f;
            for (int s = 0; s < 18; s++)
            {
                float t    = s / 17f;
                float rr   = Mathf.Lerp(8f, 21f, t);
                float aRas = baseAng - Mathf.Lerp(0.35f, 0.9f, t);
                Px(p, sz,
                   Mathf.RoundToInt(cx + Mathf.Cos(aRas) * rr),
                   Mathf.RoundToInt(cx + Mathf.Sin(aRas) * rr),
                   new Color(cor.r, cor.g, cor.b, 0.28f * (1f - t)), 0.7f);
            }
        }

        // Hub central em 3 camadas
        PintarElipse(p, sz, cx, cx, 6f,   6f,   sombra,  3f);
        PintarElipse(p, sz, cx, cx, 4f,   4f,   cor,     3f);
        PintarElipse(p, sz, cx, cx, 2.2f, 2.2f, brilho,  4f);
        PintarElipse(p, sz, cx, cx, 1.1f, 1.1f, Color.white, 6f);
    }

    // ESCUDO ESPINHOSO — escudo hexagonal com espinhos nas bordas
    static void DesenharEscudoEspinho(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.65f);
        Color espinho = Color.Lerp(cor, Color.white, 0.8f);
        int cy = (int)cx;

        // Corpo do escudo (hexagonal)
        for (int y = cy - 20; y <= cy + 18; y++)
        for (int x = (int)cx - 16; x <= (int)cx + 16; x++)
        {
            if (x < 0 || x >= sz || y < 0 || y >= sz) continue;
            float nx = Mathf.Abs(x - cx) / 16f;
            float ny = (y - cy) / 20f;
            float larg = ny < -0.4f ? (1f + ny * 2.5f) : ny > 0.6f ? ((1f - ny) / 0.4f) : 1f;
            float d = nx / Mathf.Max(larg, 0.01f);
            if (d <= 1f)
            {
                float b = Mathf.Clamp01(1f - d) * Mathf.Lerp(1f, 0.45f, (y - cy + 20f) / 40f);
                Px(p, sz, x, y, Color.Lerp(cor, brilho, b * 0.5f), Mathf.Clamp01(b * 1.5f));
            }
        }

        // Espinhos nos vértices e arestas
        float[] angEspinhos = { -90f, -30f, 30f, 90f, 150f, 210f };
        foreach (float angDeg in angEspinhos)
        {
            float ang = angDeg * Mathf.Deg2Rad;
            Vector2 base1 = new Vector2(cx + Mathf.Cos(ang) * 14f, cy + Mathf.Sin(ang) * 17f);
            Vector2 ponta = new Vector2(cx + Mathf.Cos(ang) * 23f, cy + Mathf.Sin(ang) * 27f);
            Linha(p, sz, base1, ponta, espinho, 1.8f);
            PintarElipse(p, sz, ponta.x, ponta.y, 1.5f, 1.5f, Color.white, 3f);
        }

        // Cruz interna
        Linha(p, sz, new Vector2(cx - 6, cy - 2), new Vector2(cx + 6, cy - 2), Color.white, 1.3f);
        Linha(p, sz, new Vector2(cx, cy - 8),     new Vector2(cx, cy + 6),     Color.white, 1.3f);
    }

    // AUREOLA — halo dourado com raios de luz e núcleo brilhante
    static void DesenharAureolaIcon(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.7f);
        Vector2 centro = new Vector2(cx, cx);

        // Anel externo principal (halo)
        Anel(p, sz, centro, 21f, brilho, 3.5f);
        // Anel interno menor
        Anel(p, sz, centro, 14f, new Color(brilho.r, brilho.g, brilho.b, 0.55f), 1.5f);

        // 8 raios saindo do anel externo para fora
        for (int i = 0; i < 8; i++)
        {
            float ang = i / 8f * Mathf.PI * 2f;
            Vector2 inner = centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 21f;
            Vector2 outer = centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 28f;
            Linha(p, sz, inner, outer, brilho, Mathf.Lerp(2f, 0.5f, (i % 2 == 0) ? 0f : 0.5f));
        }

        // Silhueta mínima centralizada (sugere presença do player)
        PintarElipse(p, sz, cx, cx + 3, 3.5f, 4.5f, new Color(brilho.r, brilho.g, brilho.b, 0.6f), 2f);
        PintarElipse(p, sz, cx, cx + 10, 2.8f, 2.8f, new Color(brilho.r, brilho.g, brilho.b, 0.6f), 2f);

        // Núcleo brilhante central
        PintarElipse(p, sz, cx, cx, 4f, 4f, brilho, 2.5f);
        PintarElipse(p, sz, cx, cx, 2f, 2f, Color.white, 4f);
    }

    // BARREIRA REFLEXIVA — hexágono espelhado com reflexo diagonal
    static void DesenharReflexiva(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho  = Color.Lerp(cor, Color.white, 0.70f);
        Color reflexo = Color.white;
        Vector2 centro = new Vector2(cx, cx);

        // Hexágono externo
        for (int i = 0; i < 6; i++)
        {
            float a1 = (i / 6f + 0.0833f) * Mathf.PI * 2f;
            float a2 = ((i + 1) / 6f + 0.0833f) * Mathf.PI * 2f;
            Linha(p, sz,
                centro + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * 21f,
                centro + new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)) * 21f,
                brilho, 2.8f);
        }

        // Hexágono interno menor
        for (int i = 0; i < 6; i++)
        {
            float a1 = (i / 6f + 0.0833f) * Mathf.PI * 2f;
            float a2 = ((i + 1) / 6f + 0.0833f) * Mathf.PI * 2f;
            Linha(p, sz,
                centro + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * 12f,
                centro + new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)) * 12f,
                new Color(brilho.r, brilho.g, brilho.b, 0.5f), 1.5f);
        }

        // Linha diagonal de reflexo (simula raio sendo refletido)
        Linha(p, sz, new Vector2(cx - 18, cx - 18), new Vector2(cx + 5, cx + 5), brilho, 2.5f);
        Linha(p, sz, new Vector2(cx + 5,  cx + 5),  new Vector2(cx + 18, cx - 10), brilho, 2.5f);
        // Fio brilhante no reflexo
        Linha(p, sz, new Vector2(cx - 17, cx - 17), new Vector2(cx + 18, cx - 10), reflexo, 0.9f);

        // Ponto de impacto brilhante
        PintarElipse(p, sz, cx + 5, cx + 5, 3.5f, 3.5f, Color.white, 3.5f);

        // Faíscas nos cantos do hexágono
        for (int i = 0; i < 6; i += 2)
        {
            float a = (i / 6f + 0.0833f) * Mathf.PI * 2f;
            Vector2 v = centro + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 21f;
            PintarElipse(p, sz, v.x, v.y, 2f, 2f, brilho, 3f);
        }
    }

    // CRISTAIS DE GELO — 3 cristais em órbita ao redor de um centro brilhante
    static void DesenharCristaisGelo(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.75f);
        Color sombra = new Color(cor.r * 0.3f, cor.g * 0.4f, cor.b * 0.5f);
        Vector2 centro = new Vector2(cx, cx);

        // Anel de órbita (tracejado)
        Anel(p, sz, centro, 17f, new Color(cor.r, cor.g, cor.b, 0.35f), 0.8f);

        // 3 cristais em posições orbitais
        for (int i = 0; i < 3; i++)
        {
            float ang = (i / 3f * 360f - 90f) * Mathf.Deg2Rad;
            Vector2 pos = centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 17f;

            // Corpo do cristal (rombo)
            float cristalAng = ang + Mathf.PI * 0.25f;
            float cosA = Mathf.Cos(cristalAng), sinA = Mathf.Sin(cristalAng);
            Vector2[] cristalPts = {
                pos + new Vector2(cosA, sinA) * 7f,          // ponta sup
                pos + new Vector2(-sinA, cosA) * 3f,         // lado dir
                pos - new Vector2(cosA, sinA) * 5f,          // ponta inf
                pos - new Vector2(-sinA, cosA) * 3f,         // lado esq
            };
            // Preenche cristal como polígono
            for (int y2 = 0; y2 < sz; y2++) for (int x2 = 0; x2 < sz; x2++)
            {
                if (PontoDentroPoligono(cristalPts, new Vector2(x2+0.5f, y2+0.5f)))
                {
                    float dist = Vector2.Distance(new Vector2(x2+0.5f, y2+0.5f), pos);
                    float b = Mathf.Clamp01(1f - dist / 9f);
                    Px(p, sz, x2, y2, Color.Lerp(cor, brilho, b * 0.6f), 0.9f);
                }
            }
            // Reflexo/brilho no cristal
            PintarElipse(p, sz, pos.x - cosA*2f, pos.y - sinA*2f, 1.5f, 1f, Color.white, 3f);
        }

        // Núcleo central brilhante
        PintarElipse(p, sz, cx, cx, 5f, 5f, new Color(cor.r*0.5f, cor.g*0.7f, cor.b), 2.5f);
        PintarElipse(p, sz, cx, cx, 3f, 3f, brilho, 3f);
        PintarElipse(p, sz, cx, cx, 1.5f, 1.5f, Color.white, 5f);

        // Raios saindo do centro
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            Vector2 ext = centro + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 8f;
            Linha(p, sz, centro, ext, new Color(brilho.r, brilho.g, brilho.b, 0.6f), 1f);
        }
    }

    static bool PontoDentroPoligono(Vector2[] v, Vector2 pt)
    {
        bool inside = false;
        for (int i = 0, j = v.Length - 1; i < v.Length; j = i++)
        {
            if ((v[i].y > pt.y) != (v[j].y > pt.y) &&
                pt.x < (v[j].x - v[i].x) * (pt.y - v[i].y) / (v[j].y - v[i].y) + v[i].x)
                inside = !inside;
        }
        return inside;
    }

    // SOMBRAS EM CRUZ — X de sombra
    static void DesenharCruz(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.5f);
        // Dois cortes em X
        Linha(p, sz, new Vector2(cx - 20, cx - 20), new Vector2(cx + 20, cx + 20), brilho, 4f);
        Linha(p, sz, new Vector2(cx + 20, cx - 20), new Vector2(cx - 20, cx + 20), brilho, 4f);
        // Fio brilhante nos cortes
        Linha(p, sz, new Vector2(cx - 18, cx - 19), new Vector2(cx + 18, cx + 18), Color.white, 1f);
        Linha(p, sz, new Vector2(cx + 18, cx - 19), new Vector2(cx - 18, cx + 18), Color.white, 1f);
        // Sombra central
        PintarElipse(p, sz, cx, cx, 6f, 6f, new Color(cor.r * 0.4f, cor.g * 0.4f, cor.b * 0.6f), 2f);
    }

    // CORTE FANTASMA — slash diagonal principal + aura espectral + marcas de corte
    static void DesenharCorte(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho  = Color.Lerp(cor, Color.white, 0.75f);
        Color espectro = new Color(cor.r * 0.6f, cor.g * 0.85f, cor.b, 0.45f);

        // Aura espectral difusa por trás (glow fantasma)
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            // Distância à diagonal principal (y = -x + 2cx)
            float dist = Mathf.Abs(y - (-x + 2f * cx)) / Mathf.Sqrt(2f);
            float glow = Mathf.Clamp01(1f - dist / 14f) * 0.38f;
            if (glow > 0.01f && p[y * sz + x].a > 0f)
                p[y * sz + x] = Color.Lerp(p[y * sz + x],
                    new Color(espectro.r, espectro.g, espectro.b), glow);
        }

        // Corte secundário (mais fino, levemente deslocado)
        Linha(p, sz, new Vector2(cx - 20, cx + 14), new Vector2(cx + 12, cx - 18),
              new Color(cor.r, cor.g, cor.b, 0.55f), 1.8f);

        // Corte principal (largo, diagonal /)
        Linha(p, sz, new Vector2(cx - 22, cx + 20), new Vector2(cx + 20, cx - 22),
              brilho, 4.5f);

        // Fio de corte branco no centro do corte principal
        Linha(p, sz, new Vector2(cx - 21, cx + 19), new Vector2(cx + 19, cx - 21),
              Color.white, 1.3f);

        // Terceiro corte (tênue, abaixo)
        Linha(p, sz, new Vector2(cx - 14, cx + 26), new Vector2(cx + 4, cx + 8),
              new Color(cor.r, cor.g, cor.b, 0.35f), 1.2f);

        // Faíscas espectrais nos extremos do corte principal
        PintarElipse(p, sz, cx - 20, cx + 18, 3.5f, 3.5f, Color.white, 3f);
        PintarElipse(p, sz, cx + 18, cx - 20, 2.8f, 2.8f, brilho, 3f);

        // Anel espectral de impacto (centro)
        Anel(p, sz, new Vector2(cx, cx), 9f,  new Color(cor.r, cor.g, cor.b, 0.4f), 1.2f);
        Anel(p, sz, new Vector2(cx, cx), 14f, new Color(cor.r, cor.g, cor.b, 0.2f), 0.8f);
    }

    // GARRAS DO ABISMO — 3 marcas de garra curvas
    static void DesenharGarras(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.5f);
        float[] offsets = { -12f, 0f, 12f };
        foreach (float ox in offsets)
        {
            // Cada garra: curva de cima para baixo-diagonal
            int steps = 30;
            for (int i = 0; i <= steps; i++)
            {
                float t   = i / (float)steps;
                float x2  = cx + ox + t * 10f;
                float y2  = cx - 18f + t * t * 36f;
                float esp = Mathf.Lerp(2.5f, 0.8f, t);
                PintarElipse(p, sz, x2, y2, esp, esp, Color.Lerp(brilho, cor, t), 2.5f);
            }
        }
        PintarElipse(p, sz, cx, cx + 8, 5f, 3.5f, new Color(cor.r * 0.5f, 0f, cor.b * 0.3f), 2f);
    }

    // SEGUNDA CHANCE — coração com brilho dourado
    static void DesenharCoracao(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.55f);
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float nx = (x - cx) / (cx * 0.65f);
            float ny = (y - cx * 0.92f) / (cx * 0.65f);
            float h = Mathf.Pow(nx * nx + ny * ny - 0.5f, 3f) - nx * nx * ny * ny * ny;
            if (h <= 0f)
            {
                float dist = Mathf.Sqrt(nx * nx + ny * ny);
                float b = Mathf.Clamp01(1f - dist * 1.1f);
                Px(p, sz, x, y, Color.Lerp(cor, brilho, b * 0.5f), 0.92f);
            }
        }
        // Estrela/brilho central
        DesenharEstrelaPt(p, sz, new Vector2(cx, cx + 2), 6f, cor, Color.white);
    }

    // FUGA DAS SOMBRAS — corpo com rastro teleportando
    static void DesenharSeta(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.55f);
        Vector2 centro = new Vector2(cx, cx);
        // Rastro de sombra (antes)
        for (int i = 3; i >= 1; i--)
        {
            float alpha = i / 3f * 0.35f;
            PintarElipse(p, sz, cx - i * 7f, cx, 6f, 9f, new Color(cor.r, cor.g, cor.b, alpha), 1.5f);
        }
        // Corpo do player (destino)
        PintarElipse(p, sz, cx + 8, cx, 6f, 9f, brilho, 2.5f);
        // Seta de movimento
        Linha(p, sz, new Vector2(cx - 8, cx), new Vector2(cx + 4, cx), cor, 2.5f);
        Linha(p, sz, new Vector2(cx + 4, cx), new Vector2(cx - 2, cx - 5), cor, 2f);
        Linha(p, sz, new Vector2(cx + 4, cx), new Vector2(cx - 2, cx + 5), cor, 2f);
    }

    // BARREIRA DE ENERGIA — escudo com campo de energia
    static void DesenharEscudo(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho  = Color.Lerp(cor, Color.white, 0.70f);
        Color inner   = new Color(cor.r * 0.5f, cor.g * 0.7f, cor.b, 0.35f);
        Vector2 centro = new Vector2(cx, cx);

        // ── Cúpula de energia (semicírculo superior preenchido) ──────────────
        float rdomo = 20f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float dx = x - cx, dy = y - cx;
            float r  = Mathf.Sqrt(dx * dx + dy * dy);
            if (r > rdomo + 1.5f || r < 2f) continue;

            // Preenchimento interno translúcido
            if (r < rdomo - 1f && p[y * sz + x].a > 0f)
            {
                float fillAlpha = Mathf.Clamp01((rdomo - r) / rdomo) * 0.22f;
                p[y * sz + x] = Color.Lerp(p[y * sz + x], inner, fillAlpha);
            }

            // Borda da cúpula (anel)
            float bordaDist = Mathf.Abs(r - rdomo);
            float bordaF    = Mathf.Clamp01(1.8f - bordaDist);
            if (bordaF > 0.01f)
            {
                // Brilho maior no topo, menor na base
                float angNorm = (Mathf.Atan2(dy, dx) + Mathf.PI) / (Mathf.PI * 2f);
                float topGlow = Mathf.Clamp01(1f - Mathf.Abs(angNorm - 0.75f) * 4f); // brilha no topo
                Color bc = Color.Lerp(cor, brilho, 0.3f + topGlow * 0.6f);
                Px(p, sz, x, y, bc, bordaF * (0.75f + topGlow * 0.25f));
            }
        }

        // ── Anéis de energia internos (concêntricos) ─────────────────────────
        float[] raiosInt = { 8f, 14f };
        foreach (float ri in raiosInt)
        {
            float opac = ri < 10f ? 0.5f : 0.35f;
            Anel(p, sz, centro, ri, new Color(brilho.r, brilho.g, brilho.b, opac), 1f);
        }

        // ── Silhueta do player no centro (retângulo simples) ─────────────────
        // Corpo
        PintarElipse(p, sz, cx, cx + 2, 3.5f, 5f, Color.white, 2f);
        // Cabeça
        PintarElipse(p, sz, cx, cx + 9, 3f, 3f, Color.white, 2.5f);

        // ── Hexágonos de campo nas bordas (padrão de escudo de energia) ──────
        for (int i = 0; i < 6; i++)
        {
            float ang = i / 6f * Mathf.PI * 2f;
            Vector2 pos = centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 17f;
            PintarElipse(p, sz, pos.x, pos.y, 3f, 3f, new Color(brilho.r, brilho.g, brilho.b, 0.55f), 2f);
        }

        // ── Ponto de glow no topo da cúpula ──────────────────────────────────
        PintarElipse(p, sz, cx, cx + 20f, 3.5f, 3.5f, Color.white, 3f);
        PintarElipse(p, sz, cx, cx + 20f, 1.5f, 1.5f, Color.white, 5f);
    }

    // TEIA DE PROTEÇÃO — hexagono com linhas de teia
    static void DesenharTeia(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.5f);
        Vector2 centro = new Vector2(cx, cx);
        // 3 anéis concêntricos hexagonais
        float[] raios = { 8f, 16f, 24f };
        foreach (float r in raios)
        {
            for (int i = 0; i < 6; i++)
            {
                float a1 = i / 6f * Mathf.PI * 2f;
                float a2 = (i + 1) / 6f * Mathf.PI * 2f;
                Vector2 p1 = centro + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * r;
                Vector2 p2 = centro + new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)) * r;
                Linha(p, sz, p1, p2, Color.Lerp(cor, brilho, 0.3f), 1.5f);
            }
        }
        // Raios do centro
        for (int i = 0; i < 6; i++)
        {
            float ang = i / 6f * Mathf.PI * 2f;
            Vector2 ext = centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 24f;
            Linha(p, sz, centro, ext, cor, 1.5f);
        }
        PintarElipse(p, sz, cx, cx, 4f, 4f, brilho, 3f);
    }

    // INSTINTO DE SOBREVIVÊNCIA — chama viva
    static void DesenharChama(Color[] p, int sz, float cx, Color cor)
    {
        Color inner = Color.Lerp(cor, Color.white, 0.7f); // núcleo claro
        Color outer = new Color(cor.r * 0.7f, cor.g * 0.25f, 0f);  // borda escura

        for (int y = 8; y < sz - 4; y++)
        for (int x = 5; x < sz - 5; x++)
        {
            float nx = (x - cx) / (cx * 0.5f);
            float ny = (y - 10f) / (sz - 14f); // 0=base, 1=ponta

            // Perfil da chama: mais largo na base, diminui ao topo com oscilações
            float largBase = Mathf.Sqrt(Mathf.Max(0, 1f - ny * ny * 0.5f));
            float onda = Mathf.Sin(ny * Mathf.PI * 2.5f) * 0.18f * ny;
            float larg = (largBase + onda) * (1f - ny * 0.3f);
            float d = Mathf.Abs(nx) / Mathf.Max(larg, 0.01f);

            if (d < 1f)
            {
                float b = (1f - d) * (1f - ny * 0.5f);
                Color c = Color.Lerp(outer, Color.Lerp(cor, inner, (1f - ny) * (1f - d)), b);
                Px(p, sz, x, y, c, Mathf.Clamp01(b * 1.8f));
            }
        }
    }

    // ESPELHO MÁGICO — losango espelhado
    static void DesenharEspelho(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.7f);
        Color sombra  = new Color(cor.r * 0.4f, cor.g * 0.5f, cor.b * 0.6f);

        // Losango (quadrado rotacionado 45°)
        int cy = (int)cx;
        for (int y = cy - 20; y <= cy + 20; y++)
        for (int x = (int)cx - 18; x <= (int)cx + 18; x++)
        {
            if (x < 0 || x >= sz || y < 0 || y >= sz) continue;
            float d = Mathf.Abs(x - cx) / 18f + Mathf.Abs(y - cy) / 20f;
            if (d <= 1f)
            {
                float b = Mathf.Clamp01(1f - d);
                float hl = Mathf.Clamp01((cx - x + cy + y) / (sz * 1.2f));
                Px(p, sz, x, y, Color.Lerp(cor, Color.Lerp(brilho, Color.white, hl * 0.5f), b * 0.8f), b * 0.9f);
            }
        }
        // Reflexo (linha diagonal de luz)
        Linha(p, sz, new Vector2(cx - 10, cy + 12), new Vector2(cx + 10, cy - 8), Color.white, 1.5f);
        Linha(p, sz, new Vector2(cx - 4, cy + 16), new Vector2(cx + 4, cy), new Color(1f,1f,1f,0.5f), 1f);
        // Bordas brilhantes
        Linha(p, sz, new Vector2(cx, cy + 20), new Vector2(cx + 18, cy), brilho, 1.5f);
        Linha(p, sz, new Vector2(cx + 18, cy), new Vector2(cx, cy - 20), brilho, 1.5f);
        Linha(p, sz, new Vector2(cx, cy - 20), new Vector2(cx - 18, cy), sombra, 1.5f);
        Linha(p, sz, new Vector2(cx - 18, cy), new Vector2(cx, cy + 20), sombra, 1.5f);
    }

    // ESCUDO DE KARMA — escudo com 3 orbes ao redor
    static void DesenharKarma(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.6f);
        // Escudo base (menor)
        int cy = (int)cx;
        for (int y = cy - 15; y <= cy + 13; y++)
        for (int x = (int)cx - 12; x <= (int)cx + 12; x++)
        {
            if (x < 0 || x >= sz || y < 0 || y >= sz) continue;
            float nx = Mathf.Abs(x - cx) / 12f;
            float ny = (y - cy) / 15f;
            float larg = ny < -0.4f ? (1f + ny * 2.5f) : ny > 0.6f ? ((1f - ny) / 0.4f) : 1f;
            float d = nx / Mathf.Max(larg, 0.01f);
            if (d <= 1f)
            {
                float b = Mathf.Clamp01(1f - d);
                Px(p, sz, x, y, Color.Lerp(cor, brilho, b * 0.5f), Mathf.Clamp01(b * 1.5f));
            }
        }
        // 3 orbes ao redor
        for (int i = 0; i < 3; i++)
        {
            float ang = i / 3f * Mathf.PI * 2f - Mathf.PI * 0.5f;
            Vector2 orb = new Vector2(cx + Mathf.Cos(ang) * 19f, cx + Mathf.Sin(ang) * 19f);
            PintarElipse(p, sz, orb.x, orb.y, 4.5f, 4.5f, brilho, 3f);
            PintarElipse(p, sz, orb.x - 1, orb.y + 1, 2f, 2f, Color.white, 4f);
        }
    }
}
#endif
