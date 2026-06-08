using UnityEngine;
using UnityEngine.UI;

public class MenuBackground : MonoBehaviour
{
    public Sprite backgroundSprite;

    void Awake()
    {
        Sprite sprite = backgroundSprite;

#if UNITY_EDITOR
        if (sprite == null)
        {
            var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(
                "Assets/assets/UI/menu_inicial/IMG_6856 (1).png");
            foreach (var a in all)
                if (a is Sprite s) { sprite = s; break; }
        }
#endif

        if (sprite == null) { Debug.LogWarning("[MenuBackground] Sprite não encontrado!"); return; }

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) { Debug.LogWarning("[MenuBackground] Canvas não encontrado!"); return; }

        // Encontra a maior Image opaca sem sprite — provavelmente o fundo
        Image fundo = null;
        float maiorArea = 0f;
        foreach (var img in canvas.GetComponentsInChildren<Image>(true))
        {
            if (img.sprite != null) continue;
            if (img.color.a < 0.3f) continue;
            var rt = img.rectTransform;
            float area = rt.rect.width * rt.rect.height;
            if (area > maiorArea)
            {
                maiorArea = area;
                fundo = img;
            }
        }

        if (fundo != null)
        {
            fundo.sprite           = sprite;
            fundo.color            = Color.white;
            fundo.type             = Image.Type.Simple;
            fundo.preserveAspect   = false;
            // Estica para cobrir tudo
            fundo.rectTransform.anchorMin = Vector2.zero;
            fundo.rectTransform.anchorMax = Vector2.one;
            fundo.rectTransform.offsetMin = Vector2.zero;
            fundo.rectTransform.offsetMax = Vector2.zero;
            fundo.transform.SetAsFirstSibling();
            Debug.Log($"[MenuBackground] Substituiu: {fundo.gameObject.name} (área={maiorArea})");
        }
        else
        {
            // Nenhum fundo encontrado — cria um novo atrás de tudo
            var bgGO = new GameObject("MenuBgImage", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(canvas.transform, false);
            bgGO.transform.SetAsFirstSibling();
            var rt = bgGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = bgGO.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = false;
            img.raycastTarget  = false;
            Debug.Log("[MenuBackground] Criou nova imagem de fundo.");
        }
    }
}
