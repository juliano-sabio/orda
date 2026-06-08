using UnityEditor;
using UnityEngine;

public static class DesativarATG
{
    [MenuItem("Tools/Desativar Advanced Text Generator")]
    public static void Desativar()
    {
        // Tenta via EditorPrefs (onde Unity 6 guarda o ATG)
        bool tinha = EditorPrefs.GetBool("UseAdvancedTextGenerator", true);
        EditorPrefs.SetBool("UseAdvancedTextGenerator", false);
        EditorPrefs.SetBool("UI.EnableAdvancedTextGenerator", false);
        EditorPrefs.SetBool("enableAdvancedTextGenerator", false);
        Debug.Log("EditorPrefs ATG: era=" + tinha + " -> agora=false");

        // Tenta via PanelSettings se existir
        var panelGuids = AssetDatabase.FindAssets("t:PanelSettings");
        foreach (var g in panelGuids)
        {
            var so = new SerializedObject(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(g)));
            var p = so.FindProperty("m_EnableAdvancedTextGenerator");
            if (p != null) { p.boolValue = false; so.ApplyModifiedProperties(); Debug.Log("PanelSettings ATG desativado"); }
        }

        // Tenta via TextSettings
        var tsGuids = AssetDatabase.FindAssets("t:TextSettings");
        foreach (var g in tsGuids)
        {
            var so = new SerializedObject(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(g)));
            var p = so.FindProperty("m_EnableAdvancedTextGenerator");
            if (p == null) p = so.FindProperty("enableAdvancedTextGenerator");
            if (p != null) { p.boolValue = false; so.ApplyModifiedProperties(); Debug.Log("TextSettings ATG desativado"); }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Concluido. Reinicie o Editor para garantir efeito.");
    }
}
