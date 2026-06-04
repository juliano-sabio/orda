#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public static class CriarCartaEvolucaoPrefab
{
    const string ASE_PATH    = "Assets/assets/skill/carta/cartadeevolução01.ase";
    const string PREFAB_PATH = "Assets/Resources/CartaEvolucao.prefab";

    [MenuItem("Tools/Evolucoes/Criar Prefab Carta de Evolucao")]
    public static void Criar()
    {
        // Carrega o sprite do .ase
        var sprites = AssetDatabase.LoadAllAssetsAtPath(ASE_PATH);
        Sprite sprite = null;
        foreach (var s in sprites)
            if (s is Sprite sp) { sprite = sp; break; }

        if (sprite == null)
        {
            Debug.LogError("❌ Sprite não encontrado em: " + ASE_PATH);
            return;
        }

        // Garante pasta Resources
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        // Cria o GameObject da carta
        var root = new GameObject("CartaEvolucao");
        var rootRT = root.AddComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(380f, 520f);

        // Imagem de fundo — o sprite da cartadeevolucao01
        var bg = root.AddComponent<Image>();
        bg.sprite      = sprite;
        bg.type        = Image.Type.Simple;
        bg.preserveAspect = false;
        bg.color       = Color.white;
        bg.raycastTarget = true;

        // Nome da evolução
        var nomeGO = new GameObject("Name");
        nomeGO.transform.SetParent(root.transform, false);
        var nomeRT = nomeGO.AddComponent<RectTransform>();
        nomeRT.anchorMin = new Vector2(0.05f, 0.60f);
        nomeRT.anchorMax = new Vector2(0.95f, 0.82f);
        nomeRT.offsetMin = nomeRT.offsetMax = Vector2.zero;
        var nomeTxt = nomeGO.AddComponent<TextMeshProUGUI>();
        nomeTxt.text      = "Nome da Evolução";
        nomeTxt.fontSize  = 14;
        nomeTxt.fontStyle = FontStyles.Bold;
        nomeTxt.alignment = TextAlignmentOptions.Center;
        nomeTxt.color     = new Color(1f, 0.85f, 0.2f);
        nomeTxt.textWrappingMode = TMPro.TextWrappingModes.Normal;

        // Skill alvo
        var skillGO = new GameObject("Detail");
        skillGO.transform.SetParent(root.transform, false);
        var skillRT = skillGO.AddComponent<RectTransform>();
        skillRT.anchorMin = new Vector2(0.05f, 0.50f);
        skillRT.anchorMax = new Vector2(0.95f, 0.61f);
        skillRT.offsetMin = skillRT.offsetMax = Vector2.zero;
        var skillTxt = skillGO.AddComponent<TextMeshProUGUI>();
        skillTxt.text      = "Skill";
        skillTxt.fontSize  = 10;
        skillTxt.fontStyle = FontStyles.Italic;
        skillTxt.alignment = TextAlignmentOptions.Center;
        skillTxt.color     = new Color(0.7f, 0.7f, 0.7f);

        // Descrição
        var descGO = new GameObject("Desc");
        descGO.transform.SetParent(root.transform, false);
        var descRT = descGO.AddComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0.07f, 0.22f);
        descRT.anchorMax = new Vector2(0.93f, 0.50f);
        descRT.offsetMin = descRT.offsetMax = Vector2.zero;
        var descTxt = descGO.AddComponent<TextMeshProUGUI>();
        descTxt.text      = "Descrição da evolução da skill.";
        descTxt.fontSize  = 10;
        descTxt.alignment = TextAlignmentOptions.Center;
        descTxt.color     = new Color(0.9f, 0.9f, 0.9f);
        descTxt.textWrappingMode = TMPro.TextWrappingModes.Normal;

        // Ícone
        var iconeGO = new GameObject("IconImageSlot");
        iconeGO.transform.SetParent(root.transform, false);
        var iconeRT = iconeGO.AddComponent<RectTransform>();
        iconeRT.anchorMin = new Vector2(0.2f, 0.82f);
        iconeRT.anchorMax = new Vector2(0.8f, 0.98f);
        iconeRT.offsetMin = iconeRT.offsetMax = Vector2.zero;
        var iconeImg = iconeGO.AddComponent<Image>();
        iconeImg.color = Color.white;
        iconeImg.preserveAspect = true;

        // Botão
        var btn = root.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
        colors.pressedColor     = new Color(0.85f, 0.85f, 0.85f);
        btn.colors = colors;

        // Salva como prefab
        bool sucesso;
        var prefabObj = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH, out sucesso);
        Object.DestroyImmediate(root);

        if (!sucesso) { Debug.LogError("❌ Falha ao salvar prefab."); return; }

        AssetDatabase.Refresh();
        Debug.Log("✅ Prefab criado em: " + PREFAB_PATH);

        // Atribui ao SkillEvolutionUI na cena
        var ui = Object.FindFirstObjectByType<SkillEvolutionUI>();
        if (ui != null)
        {
            ui.cardPrefab = prefabObj;
            EditorUtility.SetDirty(ui);
            Debug.Log("✅ cardPrefab atribuído ao SkillEvolutionUI.");
        }
        else
            Debug.Log("ℹ️ SkillEvolutionUI não encontrado na cena. Atribua o prefab manualmente.");

        Selection.activeObject = prefabObj;
    }
}
#endif
