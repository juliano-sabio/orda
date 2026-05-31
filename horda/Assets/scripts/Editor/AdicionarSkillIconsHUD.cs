#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class AdicionarSkillIconsHUD
{
    [MenuItem("Tools/UI Manager/Adicionar Skill Icons HUD")]
    public static void Adicionar()
    {
        var uiManager = Object.FindFirstObjectByType<UIManager>();
        if (uiManager == null)
        {
            Debug.LogError("❌ UIManager não encontrado. Abra a cena de jogo antes.");
            return;
        }

        if (uiManager.GetComponent<SkillIconsHUD>() != null)
        {
            Debug.Log("ℹ️ SkillIconsHUD já está no UIManager.");
            return;
        }

        uiManager.gameObject.AddComponent<SkillIconsHUD>();
        EditorUtility.SetDirty(uiManager.gameObject);
        Debug.Log("✅ SkillIconsHUD adicionado ao UIManager.");
    }
}
#endif
