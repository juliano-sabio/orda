#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;

public static class AdicionarCaracteresPortugues
{
    const string FONT_PATH = "Assets/assets/fontes/Retro Gaming SDF.asset";

    [MenuItem("Tools/Font/Reverter para Static (para erros de atlas)")]
    public static void ReverterParaStatic()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
        if (font == null) { Debug.LogError("Fonte nao encontrada: " + FONT_PATH); return; }

        font.atlasPopulationMode = AtlasPopulationMode.Static;
        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssets();
        Debug.Log("Retro Gaming SDF voltou para Static. Warnings de atlas suprimidos no SilenciadorLog.");
    }
}
#endif
