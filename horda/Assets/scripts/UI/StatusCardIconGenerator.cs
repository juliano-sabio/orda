using UnityEngine;
using System.Collections.Generic;

public static class StatusCardIconGenerator
{
    static readonly Dictionary<StatusCardType, Sprite> cache = new();
    const int SZ = 64;

    public static Sprite GetIcon(StatusCardType stat, Color cor)
    {
        if (cache.TryGetValue(stat, out var cached) && cached != null) return cached;

        var pixels = new Color[SZ * SZ];
        float cx = SZ * 0.5f;

        GerarFundo(pixels, SZ, cx, cor);

        switch (stat)
        {
            case StatusCardType.Health:         DesenharCoracao(pixels, SZ, cx, cor);   break;
            case StatusCardType.Attack:         DesenharEspada(pixels, SZ, cx, cor);    break;
            case StatusCardType.Defense:        DesenharEscudo(pixels, SZ, cx, cor);    break;
            case StatusCardType.CriticalChance: DesenharEstrela(pixels, SZ, cx, cor);   break;
            case StatusCardType.AttackSpeed:    DesenharRelampago(pixels, SZ, cx, cor); break;
            case StatusCardType.Speed:          DesenharVento(pixels, SZ, cx, cor);     break;
            case StatusCardType.Regen:          DesenharRegen(pixels, SZ, cx, cor);     break;
            case StatusCardType.Shield:         DesenharEscudoEnergia(pixels, SZ, cx, cor); break;
        }

        var tex = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode   = TextureWrapMode.Clamp;
        tex.SetPixels(pixels);
        tex.Apply();

        var sprite = Sprite.Create(tex, new Rect(0, 0, SZ, SZ), new Vector2(0.5f, 0.5f), SZ);
        cache[stat] = sprite;
        return sprite;
    }

    public static void LimparCache() => cache.Clear();

    // ── FUNDO RADIAL ──────────────────────────────────────────────────────────

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

            Color bg = t < 0.45f
                ? Color.Lerp(c0, c1, t / 0.45f)
                : Color.Lerp(c1, c2, (t - 0.45f) / 0.55f);

            float sheen = Mathf.Clamp01(1f - (dx + 1.5f * dy + sz * 0.8f) / (sz * 1.2f)) * 0.13f;
            bg.r = Mathf.Clamp01(bg.r + sheen);
            bg.g = Mathf.Clamp01(bg.g + sheen);
            bg.b = Mathf.Clamp01(bg.b + sheen * 1.2f);

            float rim = Mathf.Clamp01((t - 0.80f) / 0.20f);
            Color rimC = new Color(cor.r * 0.55f, cor.g * 0.55f, cor.b * 0.75f);
            bg = Color.Lerp(bg, rimC, rim * 0.55f);

            p[y * sz + x] = new Color(bg.r, bg.g, bg.b, a);
        }
    }

    // ── HELPERS ───────────────────────────────────────────────────────────────

    static void Px(Color[] p, int sz, int x, int y, Color c, float f = 1f)
    {
        if (x < 0 || x >= sz || y < 0 || y >= sz) return;
        p[y * sz + x] = Color.Lerp(p[y * sz + x], c, Mathf.Clamp01(f));
    }

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
                float hl = Mathf.Clamp01(1f + (ecx - x + ecy - y) / (rx * 4f)) * 0.35f;
                Px(p, sz, x, y, Color.Lerp(cor, Color.white, hl), f);
            }
        }
    }

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

    static void DesenharEstrelaPt(Color[] p, int sz, Vector2 centro, float raio, Color cor, Color brilho)
    {
        for (int i = 0; i < 5; i++)
        {
            float angExt = i / 5f * Mathf.PI * 2f - Mathf.PI * 0.5f;
            float angInt = (i + 0.5f) / 5f * Mathf.PI * 2f - Mathf.PI * 0.5f;
            Vector2 ext  = centro + new Vector2(Mathf.Cos(angExt), Mathf.Sin(angExt)) * raio;
            Vector2 intr = centro + new Vector2(Mathf.Cos(angInt), Mathf.Sin(angInt)) * (raio * 0.42f);
            Vector2 ext2 = centro + new Vector2(Mathf.Cos(angExt + Mathf.PI * 2f / 5f), Mathf.Sin(angExt + Mathf.PI * 2f / 5f)) * raio;
            Linha(p, sz, ext, intr, Color.Lerp(cor, brilho, 0.4f), 1.8f);
            Linha(p, sz, intr, ext2, Color.Lerp(cor, brilho, 0.4f), 1.8f);
        }
        PintarElipse(p, sz, centro.x, centro.y, raio * 0.22f, raio * 0.22f, brilho, 3f);
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

    static void PreencherPoligono(Color[] p, int sz, Vector2[] pts, Color cor, float alpha = 0.92f)
    {
        float xMin = float.MaxValue, xMax = float.MinValue;
        float yMin = float.MaxValue, yMax = float.MinValue;
        foreach (var v in pts)
        {
            xMin = Mathf.Min(xMin, v.x); xMax = Mathf.Max(xMax, v.x);
            yMin = Mathf.Min(yMin, v.y); yMax = Mathf.Max(yMax, v.y);
        }

        for (int y = Mathf.Max(0, (int)yMin - 1); y <= Mathf.Min(sz - 1, (int)yMax + 1); y++)
        for (int x = Mathf.Max(0, (int)xMin - 1); x <= Mathf.Min(sz - 1, (int)xMax + 1); x++)
        {
            float cov = 0f;
            float[] off = { -0.33f, 0.33f };
            foreach (float ox in off)
            foreach (float oy in off)
                if (PontoDentroPoligono(pts, new Vector2(x + 0.5f + ox, y + 0.5f + oy)))
                    cov += 0.25f;
            if (cov > 0.01f)
                Px(p, sz, x, y, cor, cov * alpha);
        }
    }

    // ── FORMAS ────────────────────────────────────────────────────────────────

    // VIDA — coração com fórmula matemática paramétrica
    static void DesenharCoracao(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.55f);
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float nx = (x - cx) / (cx * 0.65f);
            float ny = (y - cx * 0.92f) / (cx * 0.65f);
            float h  = Mathf.Pow(nx * nx + ny * ny - 0.5f, 3f) - nx * nx * ny * ny * ny;
            if (h <= 0f)
            {
                float dist = Mathf.Sqrt(nx * nx + ny * ny);
                float b    = Mathf.Clamp01(1f - dist * 1.1f);
                Px(p, sz, x, y, Color.Lerp(cor, brilho, b * 0.5f), 0.92f);
            }
        }
        // Brilho central
        DesenharEstrelaPt(p, sz, new Vector2(cx, cx + 2), 5f, cor, Color.white);
    }

    // ATAQUE — espada vertical com guarda
    static void DesenharEspada(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.6f);
        Color sombra  = new Color(cor.r * 0.45f, cor.g * 0.45f, cor.b * 0.28f);
        // Cabo
        Linha(p, sz, new Vector2(cx, 8), new Vector2(cx, 40), sombra, 2.2f);
        // Lâmina que afunila até a ponta
        for (int y = 20; y <= 56; y++)
        {
            float ny   = (y - 20f) / 36f;
            float larg = Mathf.Lerp(6f, 0.4f, ny * ny);
            PintarElipse(p, sz, cx, y, larg, 1.1f, Color.Lerp(cor, brilho, ny * 0.5f), 2f);
        }
        // Guarda cruzada
        Linha(p, sz, new Vector2(cx - 11, 24), new Vector2(cx + 11, 24), cor, 2.2f);
        // Brilho na ponta
        PintarElipse(p, sz, cx, 56, 2.5f, 2.5f, Color.white, 3.5f);
        // Reflexo lateral na lâmina
        Linha(p, sz, new Vector2(cx + 2, 28), new Vector2(cx + 1, 52), Color.Lerp(cor, Color.white, 0.75f), 1.1f);
    }

    // DEFESA — escudo clássico com ponta
    static void DesenharEscudo(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.70f);
        float cy = cx;

        // Preenchimento
        for (int y = (int)(cy - 22); y <= (int)(cy + 22); y++)
        for (int x = (int)(cx - 19); x <= (int)(cx + 19); x++)
        {
            if (x < 0 || x >= sz || y < 0 || y >= sz) continue;
            float ny   = (y - (cy - 22f)) / 44f; // 0=topo, 1=ponta
            float larg = ny < 0.52f ? 19f : 19f * Mathf.Clamp01((1f - ny) / 0.48f);
            float d    = Mathf.Abs(x - cx) / Mathf.Max(larg, 0.01f);
            if (d > 1f) continue;

            float b  = 1f - d;
            float hl = Mathf.Clamp01((cx - x) / 30f + 0.15f) * 0.45f;
            Color c  = Color.Lerp(cor, Color.Lerp(brilho, Color.white, hl * 0.4f), b * 0.75f);
            Px(p, sz, x, y, c, b * 0.92f);
        }

        // Bordas
        Color bordaClara  = brilho;
        Color bordaEscura = new Color(cor.r * 0.5f, cor.g * 0.5f, cor.b * 0.7f);
        Linha(p, sz, new Vector2(cx - 19, cy - 22), new Vector2(cx + 19, cy - 22), bordaClara, 1.5f);
        Linha(p, sz, new Vector2(cx - 19, cy - 22), new Vector2(cx - 19, cy + 2),  bordaClara, 1.5f);
        Linha(p, sz, new Vector2(cx + 19, cy - 22), new Vector2(cx + 19, cy + 2),  bordaEscura, 1.5f);
        Linha(p, sz, new Vector2(cx - 19, cy + 2),  new Vector2(cx,     cy + 22),  bordaClara, 1.5f);
        Linha(p, sz, new Vector2(cx + 19, cy + 2),  new Vector2(cx,     cy + 22),  bordaEscura, 1.5f);

        // Detalhe: anel central + ponto brilhante
        Anel(p, sz, new Vector2(cx, cx), 9f, new Color(brilho.r, brilho.g, brilho.b, 0.55f), 1.3f);
        PintarElipse(p, sz, cx, cx, 3.5f, 3.5f, brilho, 3.5f);
        PintarElipse(p, sz, cx, cx, 1.5f, 1.5f, Color.white, 5f);
    }

    // CRÍTICO — estrela de 5 pontas grande com sparkles
    static void DesenharEstrela(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.5f);
        // Estrela principal
        DesenharEstrelaPt(p, sz, new Vector2(cx, cx), 21f, cor, Color.white);
        // Pequenos sparkles nos cantos
        PintarElipse(p, sz, cx - 16, cx - 16, 3.5f, 3.5f, brilho, 2.5f);
        PintarElipse(p, sz, cx + 17, cx - 13, 3f, 3f, brilho, 2.5f);
        PintarElipse(p, sz, cx - 18, cx + 17, 2.5f, 2.5f, brilho, 2.5f);
        PintarElipse(p, sz, cx + 15, cx + 18, 2f, 2f, brilho, 2.5f);
    }

    // VEL. ATQ — raio/relâmpago com polígono anti-aliased
    static void DesenharRelampago(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.65f);
        float cy = cx;

        // Polígono do raio: zigue-zague vertical clássico
        Vector2[] pts = {
            new Vector2(cx + 9f,  10f),
            new Vector2(cx + 2f,  cy + 2f),
            new Vector2(cx + 16f, cy + 2f),
            new Vector2(cx - 9f,  sz - 10f),
            new Vector2(cx - 2f,  cy - 2f),
            new Vector2(cx - 16f, cy - 2f),
        };

        // Corpo do raio
        PreencherPoligono(p, sz, pts, cor, 0.90f);

        // Shading: topo mais claro
        for (int y = 10; y < sz - 10; y++)
        for (int x = (int)(cx - 17); x <= (int)(cx + 17); x++)
        {
            if (x < 0 || x >= sz) continue;
            if (PontoDentroPoligono(pts, new Vector2(x + 0.5f, y + 0.5f)))
            {
                float ny = (y - 10f) / (sz - 20f);
                float hl = Mathf.Clamp01(1f - ny) * 0.45f;
                Px(p, sz, x, y, Color.Lerp(p[y * sz + x], brilho, hl), hl * 0.7f);
            }
        }

        // Brilho no ponto de quebra central
        PintarElipse(p, sz, cx, cy, 3f, 3f, Color.white, 3.5f);
        PintarElipse(p, sz, cx, cy, 1.5f, 1.5f, Color.white, 6f);
    }

    // VEL — seta com trilhas de vento
    static void DesenharVento(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.55f);
        float cy = cx;

        // Trilhas de vento (linhas horizontais com comprimentos e alturas diferentes)
        (float yOff, float xStart, float xEnd, float alpha)[] trilhas = {
            (-11f, cx - 20f, cx - 2f, 0.25f),
            ( -6f, cx - 22f, cx - 2f, 0.35f),
            ( -1f, cx - 18f, cx - 2f, 0.45f),
            (  4f, cx - 22f, cx - 2f, 0.35f),
            (  9f, cx - 20f, cx - 2f, 0.25f),
        };
        foreach (var (yOff, xStart, xEnd, alpha) in trilhas)
            Linha(p, sz,
                new Vector2(xStart, cy + yOff),
                new Vector2(xEnd,   cy + yOff),
                new Color(cor.r, cor.g, cor.b, alpha), 1.5f);

        // Haste da seta
        Linha(p, sz, new Vector2(cx - 8, cy), new Vector2(cx + 14, cy), brilho, 3f);

        // Cabeça da seta (triângulo apontando para direita)
        Vector2[] cabeca = {
            new Vector2(cx + 26f, cy),
            new Vector2(cx + 12f, cy - 11f),
            new Vector2(cx + 12f, cy + 11f),
        };
        PreencherPoligono(p, sz, cabeca, brilho, 0.92f);

        // Brilho na ponta
        PintarElipse(p, sz, cx + 25, cy, 2.5f, 2.5f, Color.white, 4f);
    }

    // REGEN — símbolo de ciclo com cruz de cura no centro
    static void DesenharRegen(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.6f);
        Vector2 centro = new Vector2(cx, cx);
        float raio = 19f;

        // Arco principal (315° — deixa abertura no canto inferior-direito)
        int segs = 220;
        for (int i = 0; i < segs; i++)
        {
            float t   = i / (float)(segs - 1);
            float ang = t * Mathf.PI * 1.75f + Mathf.PI * 0.5f;
            Vector2 pt = centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raio;

            float fadeIn = Mathf.Clamp01(t * 4f);
            Color arcCor = Color.Lerp(cor, brilho, t * 0.6f);
            int r2 = 2;
            for (int dy = -r2; dy <= r2; dy++)
            for (int dx = -r2; dx <= r2; dx++)
            {
                float f = Mathf.Clamp01(2.8f - Mathf.Sqrt(dx * dx + dy * dy));
                Px(p, sz, (int)pt.x + dx, (int)pt.y + dy, arcCor, f * fadeIn);
            }
        }

        // Seta na ponta do arco indicando rotação
        float angPonta = Mathf.PI * 1.75f + Mathf.PI * 0.5f;
        Vector2 ptPonta = centro + new Vector2(Mathf.Cos(angPonta), Mathf.Sin(angPonta)) * raio;
        // Tangente perpendicular ao raio
        Vector2 tang = new Vector2(-Mathf.Sin(angPonta), Mathf.Cos(angPonta)).normalized;
        Vector2 norm = new Vector2(tang.y, -tang.x);
        Linha(p, sz, ptPonta + tang * 8f, ptPonta - norm * 5f, brilho, 2f);
        Linha(p, sz, ptPonta + tang * 8f, ptPonta + norm * 5f, brilho, 2f);

        // Cruz de cura no centro
        Color branco = Color.Lerp(brilho, Color.white, 0.4f);
        Linha(p, sz, new Vector2(cx - 6, cx), new Vector2(cx + 6, cx), branco, 2.5f);
        Linha(p, sz, new Vector2(cx, cx - 6), new Vector2(cx, cx + 6), branco, 2.5f);
        PintarElipse(p, sz, cx, cx, 2f, 2f, Color.white, 4f);
    }

    // ESCUDO (energia) — barreira hexagonal com anel de energia pulsante
    static void DesenharEscudoEnergia(Color[] p, int sz, float cx, Color cor)
    {
        Color brilho = Color.Lerp(cor, Color.white, 0.65f);
        Vector2 centro = new Vector2(cx, cx);
        float raio = 18f;

        // Hexágono preenchido (barreira)
        Vector2[] hex = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            float ang = i / 6f * Mathf.PI * 2f - Mathf.PI * 0.5f;
            hex[i] = centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raio;
        }
        PreencherPoligono(p, sz, hex, cor, 0.55f);

        // Contorno do hexágono
        for (int i = 0; i < 6; i++)
            Linha(p, sz, hex[i], hex[(i + 1) % 6], brilho, 1.8f);

        // Anel de energia externo (pulso)
        Anel(p, sz, centro, raio + 4f, new Color(brilho.r, brilho.g, brilho.b, 0.55f), 1.3f);

        // Brilho central
        PintarElipse(p, sz, cx, cx, 3.5f, 3.5f, brilho, 3.5f);
        PintarElipse(p, sz, cx, cx, 1.5f, 1.5f, Color.white, 5f);
    }
}
