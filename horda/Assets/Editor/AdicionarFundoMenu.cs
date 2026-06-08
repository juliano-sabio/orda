using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class AdicionarFundoMenu
{
    [MenuItem("Tools/Menu/Adicionar Fundo no Menu Inicial")]
    static void Adicionar()
    {
        // 1. Abre a cena menu_inicial se necessário
        EditorSceneManager.OpenScene("Assets/Scenes/menu_inicial.unity");

        // 2. Carrega o sprite
        Sprite sprite = null;
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath("Assets/assets/UI/menu_inicial/IMG_6856 (1).png"))
            if (a is Sprite s) { sprite = s; break; }

        if (sprite == null)
        {
            Debug.LogError("[AdicionarFundoMenu] Sprite não encontrado!");
            EditorUtility.DisplayDialog("Erro", "Sprite IMG_6856 não encontrado!", "OK");
            return;
        }

        // 3. Cria Canvas se não existir
        var canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            var go = new GameObject("Canvas");
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var cs = go.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            cs.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
        }

        // 4. Remove fundo antigo se existir
        var t = canvas.transform.Find("MenuBgImage");
        if (t != null) Object.DestroyImmediate(t.gameObject);

        // 5. Cria a imagem de fundo
        var bgGO = new GameObject("MenuBgImage");
        bgGO.transform.SetParent(canvas.transform, false);
        bgGO.transform.SetAsFirstSibling();

        var rt = bgGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = bgGO.AddComponent<Image>();
        img.sprite = sprite;
        img.color  = Color.white;
        img.type   = Image.Type.Simple;
        img.preserveAspect = false;
        img.raycastTarget  = false;

        // 6. Salva a cena
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        Debug.Log("[AdicionarFundoMenu] Fundo adicionado com sucesso!");
        EditorUtility.DisplayDialog("Pronto", "Fundo adicionado à cena menu_inicial!", "OK");
    }
}
