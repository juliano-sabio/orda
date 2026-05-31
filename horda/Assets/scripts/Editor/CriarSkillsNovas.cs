#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class CriarSkillsNovas
{
    const string PASTA = "Assets/Skills";

    [MenuItem("Tools/Skills/Criar Lança de Luz")]
    public static void CriarLanca() => CriarSkill("LancaLuz", "Lança de Luz",
        "Uma lança de luz dispara no inimigo mais próximo causando dano alto.", SpecificSkillType.LancaLuz,
        SkillRarity.Rare, 60f, 4f, 0, 20f, 12f, new Color(1f, 0.95f, 0.4f));

    [MenuItem("Tools/Skills/Criar Chicote de Energia")]
    public static void CriarChicote() => CriarSkill("ChicoteEnergia", "Chicote de Energia",
        "Um chicote de energia varre 360° ao redor do player atingindo todos os inimigos.", SpecificSkillType.ChicoteEnergia,
        SkillRarity.Uncommon, 30f, 2f, 0, 0f, 4f, new Color(0.2f, 0.8f, 1f));

    [MenuItem("Tools/Skills/Criar Mísseis Teleguiados")]
    public static void CriarMisseis() => CriarSkill("MisseisTeleguiados", "Mísseis Teleguiados",
        "3 mísseis são lançados e perseguem os 3 inimigos mais próximos.", SpecificSkillType.MisseisTeleguiados,
        SkillRarity.Uncommon, 25f, 3f, 3, 10f, 0f, new Color(1f, 0.5f, 0.1f));

    [MenuItem("Tools/Skills/Criar Pulso Rítmico")]
    public static void CriarPulso() => CriarSkill("PulsoRitmico", "Pulso Rítmico",
        "A cada segundo emite um pulso circular de dano ao redor do player.", SpecificSkillType.PulsoRitmico,
        SkillRarity.Common, 15f, 1.2f, 0, 0f, 3.5f, new Color(0.3f, 0.9f, 0.5f));

    [MenuItem("Tools/Skills/Criar Espada Fantasma")]
    public static void CriarEspada() => CriarSkill("EspadaFantasma", "Espada Fantasma",
        "3 cortes rápidos em leque na direção do inimigo mais próximo.", SpecificSkillType.EspadaFantasma,
        SkillRarity.Uncommon, 20f, 2.5f, 0, 0f, 3f, new Color(0.85f, 0.85f, 1f));

    [MenuItem("Tools/Skills/Criar Corrente Sombria")]
    public static void CriarCorrente() => CriarSkill("CorrenteSombria", "Corrente Sombria",
        "Correntes sombrias envolvem os 3 inimigos mais próximos e pulsam dano por 3 segundos.", SpecificSkillType.CorrenteSombria,
        SkillRarity.Rare, 12f, 5f, 3, 0f, 12f, new Color(0.5f, 0.15f, 0.9f), 3f);

    // ─────────────────────────────────────────────────────────────────────────

    static void CriarSkill(string id, string nome, string desc, SpecificSkillType tipo,
        SkillRarity raridade, float atk, float interval, int projCount,
        float projSpeed, float specialVal, Color cor, float duration = 0f)
    {
        if (!AssetDatabase.IsValidFolder(PASTA))
        { AssetDatabase.CreateFolder("Assets", "Skills"); AssetDatabase.Refresh(); }

        string path = $"{PASTA}/{id}.asset";
        var existente = AssetDatabase.LoadAssetAtPath<SkillData>(path);
        if (existente != null)
        {
            if (existente.icon == null) { existente.icon = GerarIcone(id, cor, tipo); EditorUtility.SetDirty(existente); AssetDatabase.SaveAssets(); }
            AdicionarAoSkillManager(existente);
            Selection.activeObject = existente;
            Debug.Log($"✅ '{nome}' já existe.");
            return;
        }

        var skill = ScriptableObject.CreateInstance<SkillData>();
        skill.icon               = GerarIcone(id, cor, tipo);
        skill.skillName          = nome;
        skill.description        = desc;
        skill.specificType       = tipo;
        skill.rarity             = raridade;
        skill.isPassive          = true;
        skill.attackBonus        = atk;
        skill.activationInterval = interval;
        skill.projectileCount    = projCount;
        skill.projectileSpeed    = projSpeed;
        skill.specialValue       = specialVal;
        skill.elementColor       = cor;
        skill.duration           = duration;
        skill.element            = PlayerStats.Element.None;
        skill.isUnique           = true;
        skill.requiredLevel      = 1;

        AssetDatabase.CreateAsset(skill, path);
        AdicionarAoSkillManager(skill);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = skill;
        Debug.Log($"✅ '{nome}' criado em: {path}");
    }

    static Sprite GerarIcone(string id, Color cor, SpecificSkillType tipo)
    {
        const int SZ = 64;
        string iconPath = $"{PASTA}/{id}Icon.png";

        var existente = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        if (existente != null) return existente;

        var tex    = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false);
        var pixels = new Color[SZ * SZ];
        float cx = SZ * 0.5f;

        // Fundo circular baseado na cor
        for (int y = 0; y < SZ; y++)
        for (int x = 0; x < SZ; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cx));
            float a = Mathf.Clamp01(1f - d / (cx * 1.05f));
            pixels[y * SZ + x] = new Color(cor.r * 0.12f, cor.g * 0.12f, cor.b * 0.18f, a);
        }

        // Forma única por tipo
        switch (tipo)
        {
            case SpecificSkillType.LancaLuz:
                DesenharLanca(pixels, SZ, cx, cor); break;
            case SpecificSkillType.ChicoteEnergia:
                DesenharArco(pixels, SZ, cx, cor); break;
            case SpecificSkillType.MisseisTeleguiados:
                DesenharMisseis(pixels, SZ, cx, cor); break;
            case SpecificSkillType.PulsoRitmico:
                DesenharPulso(pixels, SZ, cx, cor); break;
            case SpecificSkillType.EspadaFantasma:
                DesenharEspada(pixels, SZ, cx, cor); break;
            case SpecificSkillType.CorrenteSombria:
                DesenharCorrente(pixels, SZ, cx, cor); break;
        }

        tex.SetPixels(pixels);
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        System.IO.File.WriteAllBytes(
            System.IO.Path.Combine(Application.dataPath, "../" + iconPath), png);
        AssetDatabase.Refresh();

        var imp = AssetImporter.GetAtPath(iconPath) as TextureImporter;
        if (imp != null) { imp.textureType = TextureImporterType.Sprite; imp.spritePixelsPerUnit = SZ; imp.filterMode = FilterMode.Point; imp.SaveAndReimport(); }

        return AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
    }

    // ── Formas dos ícones ─────────────────────────────────────────────────────

    static void DesenharLanca(Color[] p, int sz, float cx, Color cor)
    {
        // Lança vertical com ponta
        for (int y = 8; y < sz - 4; y++)
        for (int x = 0; x < sz; x++)
        {
            float nx = Mathf.Abs(x - cx) / cx;
            float ny = (y - 8f) / (sz - 12f);
            float larg = ny < 0.75f ? (1f - nx) * 0.35f : (1f - nx) * (1f - ny) * 1.5f;
            float a = larg > nx * 0.35f ? Mathf.Clamp01(larg * 3f) : 0f;
            if (a > 0) p[y * sz + x] = Color.Lerp(p[y * sz + x], cor, a);
        }
    }

    static void DesenharArco(Color[] p, int sz, float cx, Color cor)
    {
        int segs = 48;
        for (int i = 0; i < segs; i++)
        {
            float ang = (i / (float)segs) * Mathf.PI * 2f;
            float r = cx * 0.65f;
            int px = Mathf.RoundToInt(cx + Mathf.Cos(ang) * r);
            int py = Mathf.RoundToInt(cx + Mathf.Sin(ang) * r);
            for (int dw = -2; dw <= 2; dw++)
            for (int dh = -2; dh <= 2; dh++)
            {
                int nx = px + dw, ny = py + dh;
                if (nx < 0 || nx >= sz || ny < 0 || ny >= sz) continue;
                float a = Mathf.Clamp01(1.5f - Mathf.Sqrt(dw * dw + dh * dh));
                p[ny * sz + nx] = Color.Lerp(p[ny * sz + nx], cor, a);
            }
        }
    }

    static void DesenharMisseis(Color[] p, int sz, float cx, Color cor)
    {
        Vector2[] pos = { new Vector2(cx - 12, cx + 8), new Vector2(cx, cx - 4), new Vector2(cx + 12, cx + 8) };
        foreach (var mp in pos)
            for (int y = (int)mp.y - 10; y <= (int)mp.y + 4; y++)
            for (int x = (int)mp.x - 3; x <= (int)mp.x + 3; x++)
            {
                if (x < 0 || x >= sz || y < 0 || y >= sz) continue;
                float nx = Mathf.Abs(x - mp.x) / 3f;
                float ny = (y - (mp.y - 10f)) / 14f;
                float larg = ny < 0.7f ? (1f - nx) : (1f - nx) * (1f - ny) * 3f;
                float a = Mathf.Clamp01(larg * 2f);
                if (a > 0) p[y * sz + x] = Color.Lerp(p[y * sz + x], cor, a);
            }
    }

    static void DesenharPulso(Color[] p, int sz, float cx, Color cor)
    {
        float[] raios = { cx * 0.3f, cx * 0.55f, cx * 0.75f };
        foreach (float r in raios)
        {
            int segs = 32;
            for (int i = 0; i < segs; i++)
            {
                float ang = i / (float)segs * Mathf.PI * 2f;
                int px = Mathf.RoundToInt(cx + Mathf.Cos(ang) * r);
                int py = Mathf.RoundToInt(cx + Mathf.Sin(ang) * r);
                for (int dw = -1; dw <= 1; dw++)
                for (int dh = -1; dh <= 1; dh++)
                {
                    int nx = px + dw, ny = py + dh;
                    if (nx < 0 || nx >= sz || ny < 0 || ny >= sz) continue;
                    float a = Mathf.Clamp01(1.2f - Mathf.Sqrt(dw * dw + dh * dh)) * (1f - r / (cx * 1.1f) * 0.5f);
                    p[ny * sz + nx] = Color.Lerp(p[ny * sz + nx], cor, a);
                }
            }
        }
    }

    static void DesenharEspada(Color[] p, int sz, float cx, Color cor)
    {
        // 3 cortes diagonais
        float[][] cortes = {
            new float[]{ cx - 14, cx + 14, cx - 5, cx - 5 },
            new float[]{ cx - 14, cx + 14, cx + 2, cx + 2 },
            new float[]{ cx - 14, cx + 14, cx + 10, cx + 10 },
        };
        foreach (var c in cortes)
        {
            int steps = 40;
            for (int i = 0; i <= steps; i++)
            {
                float t  = i / (float)steps;
                int   px = Mathf.RoundToInt(Mathf.Lerp(c[0], c[1], t));
                int   py = Mathf.RoundToInt(Mathf.Lerp(c[2], c[3], t));
                for (int dw = -1; dw <= 1; dw++)
                for (int dh = -1; dh <= 1; dh++)
                {
                    int nx = px + dw, ny = py + dh;
                    if (nx < 0 || nx >= sz || ny < 0 || ny >= sz) continue;
                    float a = Mathf.Clamp01(1.3f - Mathf.Sqrt(dw * dw + dh * dh)) * Mathf.Sin(t * Mathf.PI) * 0.8f + 0.2f;
                    p[ny * sz + nx] = Color.Lerp(p[ny * sz + nx], cor, Mathf.Clamp01(a));
                }
            }
        }
    }

    static void DesenharCorrente(Color[] p, int sz, float cx, Color cor)
    {
        // 3 pontos conectados por linha ondulada
        Vector2[] pts = { new Vector2(cx - 14, cx + 10), new Vector2(cx + 14, cx), new Vector2(cx - 5, cx - 14) };
        for (int seg = 0; seg < 3; seg++)
        {
            Vector2 de = pts[seg], ate = pts[(seg + 1) % 3];
            int steps = 30;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int px = Mathf.RoundToInt(Mathf.Lerp(de.x, ate.x, t));
                int py = Mathf.RoundToInt(Mathf.Lerp(de.y, ate.y, t));
                for (int dw = -1; dw <= 1; dw++)
                for (int dh = -1; dh <= 1; dh++)
                {
                    int nx = px + dw, ny = py + dh;
                    if (nx < 0 || nx >= sz || ny < 0 || ny >= sz) continue;
                    float a = Mathf.Clamp01(1.2f - Mathf.Sqrt(dw * dw + dh * dh));
                    p[ny * sz + nx] = Color.Lerp(p[ny * sz + nx], cor, a);
                }
            }
            // Ponto
            for (int dw = -3; dw <= 3; dw++)
            for (int dh = -3; dh <= 3; dh++)
            {
                int nx = (int)de.x + dw, ny = (int)de.y + dh;
                if (nx < 0 || nx >= sz || ny < 0 || ny >= sz) continue;
                float a = Mathf.Clamp01(1.5f - Mathf.Sqrt(dw * dw + dh * dh) * 0.5f);
                p[ny * sz + nx] = Color.Lerp(p[ny * sz + nx], cor, a);
            }
        }
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

    [MenuItem("Tools/Skills/Criar TODAS as Skills Novas")]
    public static void CriarTodas()
    {
        CriarLanca(); CriarChicote(); CriarMisseis();
        CriarPulso(); CriarEspada(); CriarCorrente();
        Debug.Log("✅ Todas as 6 skills criadas!");
    }
}
#endif
