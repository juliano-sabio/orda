#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class CriarSkillCampoEspinhos
{
    const string PASTA = "Assets/Skills";
    const string PATH  = "Assets/Skills/CampoEspinhos.asset";

    [MenuItem("Tools/Skills/Criar Campo de Espinhos")]
    public static void Criar()
    {
        if (!AssetDatabase.IsValidFolder(PASTA))
        {
            AssetDatabase.CreateFolder("Assets", "Skills");
            AssetDatabase.Refresh();
        }

        var existente = AssetDatabase.LoadAssetAtPath<SkillData>(PATH);
        if (existente != null)
        {
            // Atualiza o ícone se estiver faltando
            if (existente.icon == null)
            {
                existente.icon = CriarIcone();
                EditorUtility.SetDirty(existente);
                AssetDatabase.SaveAssets();
                Debug.Log("✅ Ícone atribuído ao Campo de Espinhos existente.");
            }
            AdicionarAoSkillManager(existente);
            Selection.activeObject = existente;
            return;
        }

        var skill = ScriptableObject.CreateInstance<SkillData>();

        skill.icon               = CriarIcone();
        skill.skillName          = "Campo de Espinhos";
        skill.description        = "Cria uma aura de espinhos ao redor que causa dano contínuo a inimigos próximos.";
        skill.specificType       = SpecificSkillType.CampoEspinhos;
        skill.rarity             = SkillRarity.Uncommon;
        skill.isPassive          = true;
        skill.attackBonus        = 12f;      // dano por tick
        skill.specialValue       = 3f;       // raio da aura
        skill.activationInterval = 1.5f;     // intervalo entre ticks
        skill.elementColor       = new Color(0.2f, 1f, 0.3f); // verde
        skill.element            = PlayerStats.Element.None;
        skill.isUnique           = true;
        skill.requiredLevel      = 1;

        AssetDatabase.CreateAsset(skill, PATH);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        AdicionarAoSkillManager(skill);

        Selection.activeObject = skill;
        Debug.Log("✅ SkillData 'Campo de Espinhos' criado em: " + PATH);
    }

    static void AdicionarAoSkillManager(SkillData skill)
    {
        // Tenta pegar cardPrefab de uma skill existente
        if (skill.cardPrefab == null)
        {
            var skillChoiceUI = Object.FindFirstObjectByType<SkillChoiceUI>();
            if (skillChoiceUI != null && skillChoiceUI.skillChoicePrefab != null)
            {
                skill.cardPrefab = skillChoiceUI.skillChoicePrefab;
                EditorUtility.SetDirty(skill);
                Debug.Log($"✅ cardPrefab atribuído de SkillChoiceUI.");
            }
            else
            {
                // Busca nas skills existentes do SkillManager
                var sm2 = Object.FindFirstObjectByType<SkillManager>();
                if (sm2 != null)
                {
                    var ref_ = sm2.availableSkills.Find(s => s != null && s.cardPrefab != null);
                    if (ref_ != null)
                    {
                        skill.cardPrefab = ref_.cardPrefab;
                        EditorUtility.SetDirty(skill);
                        Debug.Log($"✅ cardPrefab herdado de '{ref_.skillName}'.");
                    }
                }
            }
        }

        var managers = Object.FindObjectsByType<SkillManager>(FindObjectsSortMode.None);
        foreach (var sm in managers)
        {
            if (sm.availableSkills.Exists(s => s != null && s.skillName == skill.skillName))
                continue;
            sm.availableSkills.Add(skill);
            EditorUtility.SetDirty(sm);
            Debug.Log($"✅ '{skill.skillName}' adicionada ao SkillManager na cena.");
        }
        if (managers.Length == 0)
            Debug.LogWarning("⚠️ Nenhum SkillManager encontrado. Abra a cena de jogo antes de rodar o menu.");

        AssetDatabase.SaveAssets();
    }

    static Sprite CriarIcone()
    {
        const int SZ = 64;
        const string ICON_PATH = "Assets/Skills/CampoEspinhosIcon.png";

        // Reutiliza se já existir
        var existente = AssetDatabase.LoadAssetAtPath<Sprite>(ICON_PATH);
        if (existente != null) return existente;

        var tex = new Texture2D(SZ, SZ, TextureFormat.RGBA32, false);
        var pixels = new Color[SZ * SZ];
        float cx = SZ * 0.5f;
        float raioExt = SZ * 0.48f;
        float raioInt = SZ * 0.28f;

        for (int y = 0; y < SZ; y++)
        for (int x = 0; x < SZ; x++)
        {
            float dx = x + 0.5f - cx;
            float dy = y + 0.5f - cx;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            float ang  = Mathf.Atan2(dy, dx); // -π a π

            // Fora do círculo externo → transparente
            if (dist > raioExt) { pixels[y * SZ + x] = Color.clear; continue; }

            // Número de espinhos
            const int ESPINHOS = 8;
            float angNorm   = (ang / (Mathf.PI * 2f) + 1f) % 1f; // 0..1
            float espinho   = Mathf.Abs(Mathf.Sin(angNorm * Mathf.PI * ESPINHOS));
            float raioLocal = Mathf.Lerp(raioInt, raioExt, espinho * 0.65f + 0.25f);

            if (dist > raioLocal) { pixels[y * SZ + x] = Color.clear; continue; }

            // Gradiente: centro brilhante → borda escura
            float t = dist / raioLocal;

            // Núcleo brilhante
            Color corCentro = new Color(0.5f, 1f, 0.5f);
            // Cor do espinho
            Color corEspinho = new Color(0.1f, 0.65f, 0.2f);
            // Borda escura
            Color corBorda = new Color(0.04f, 0.25f, 0.06f);

            Color c;
            if (t < 0.35f)
                c = Color.Lerp(corCentro, corEspinho, t / 0.35f);
            else
                c = Color.Lerp(corEspinho, corBorda, (t - 0.35f) / 0.65f);

            // Suaviza borda
            float alpha = dist < raioLocal - 1.5f ? 1f : Mathf.Clamp01((raioLocal - dist) / 1.5f);
            pixels[y * SZ + x] = new Color(c.r, c.g, c.b, alpha);
        }

        tex.SetPixels(pixels);
        tex.Apply();

        // Salva como PNG
        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        System.IO.File.WriteAllBytes(
            System.IO.Path.Combine(Application.dataPath, "../" + ICON_PATH), png);
        AssetDatabase.Refresh();

        // Configura como Sprite
        var importer = AssetImporter.GetAtPath(ICON_PATH) as TextureImporter;
        if (importer != null)
        {
            importer.textureType        = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = SZ;
            importer.filterMode         = FilterMode.Point;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(ICON_PATH);
    }
}
#endif
