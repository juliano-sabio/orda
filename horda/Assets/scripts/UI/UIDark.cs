using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Helpers compactos pra montar painéis no estilo dark-fantasy (borda dourada + fundo escuro),
// reutilizados por telas criadas via código (EscolhaPosVitoriaUI, VitoriaUI).
public static class UIDark
{
    static readonly Color CorBorda = new Color(0.78f, 0.63f, 0.16f);
    static readonly Color CorFundo = new Color(0.06f, 0.04f, 0.11f, 0.97f);

    public static GameObject Card(Transform pai, Vector2 tamanho)
    {
        var go = new GameObject("Card");
        go.transform.SetParent(pai, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = tamanho;

        var brd = new GameObject("Brd"); brd.transform.SetParent(go.transform, false);
        var rbrd = brd.AddComponent<RectTransform>();
        rbrd.anchorMin = Vector2.zero; rbrd.anchorMax = Vector2.one;
        rbrd.offsetMin = new Vector2(-2f, -2f); rbrd.offsetMax = new Vector2(2f, 2f);
        brd.AddComponent<Image>().color = new Color(CorBorda.r, CorBorda.g, CorBorda.b, 0.80f);

        var fundo = new GameObject("Fundo"); fundo.transform.SetParent(go.transform, false);
        var rf = fundo.AddComponent<RectTransform>();
        rf.anchorMin = Vector2.zero; rf.anchorMax = Vector2.one; rf.offsetMin = rf.offsetMax = Vector2.zero;
        fundo.AddComponent<Image>().color = CorFundo;
        return go;
    }

    public static TextMeshProUGUI Titulo(Transform pai, string texto, Vector2 pos, Color cor)
    {
        var go = new GameObject("Titulo"); go.transform.SetParent(pai, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(500f, 80f);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = texto; tmp.fontSize = 52; tmp.color = cor;
        tmp.alignment = TextAlignmentOptions.Center; tmp.fontStyle = FontStyles.Bold;
        return tmp;
    }

    public static void Linha(Transform pai, Vector2 pos, float largura, Color cor)
    {
        var go = new GameObject("Sep"); go.transform.SetParent(pai, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(largura, 2f);
        go.AddComponent<Image>().color = cor;
    }

    public static GameObject Botao(Transform pai, string label, Vector2 pos, Vector2 tamanho,
        Color cor, UnityEngine.Events.UnityAction acao)
    {
        var go = new GameObject("Btn"); go.transform.SetParent(pai, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = tamanho;

        var brd = new GameObject("Brd"); brd.transform.SetParent(go.transform, false);
        var rbrd = brd.AddComponent<RectTransform>();
        rbrd.anchorMin = Vector2.zero; rbrd.anchorMax = Vector2.one;
        rbrd.offsetMin = new Vector2(-1f, -1f); rbrd.offsetMax = new Vector2(1f, 1f);
        brd.AddComponent<Image>().color = new Color(CorBorda.r, CorBorda.g, CorBorda.b, 0.80f);

        var corpo = new GameObject("Corpo"); corpo.transform.SetParent(go.transform, false);
        var rco = corpo.AddComponent<RectTransform>();
        rco.anchorMin = Vector2.zero; rco.anchorMax = Vector2.one; rco.offsetMin = rco.offsetMax = Vector2.zero;
        var img = corpo.AddComponent<Image>(); img.color = cor;

        var ac = new GameObject("Ac"); ac.transform.SetParent(go.transform, false);
        var rac = ac.AddComponent<RectTransform>();
        rac.anchorMin = Vector2.zero; rac.anchorMax = new Vector2(0f, 1f);
        rac.offsetMin = Vector2.zero; rac.offsetMax = new Vector2(4f, 0f);
        ac.AddComponent<Image>().color = new Color(CorBorda.r, CorBorda.g, CorBorda.b, 0.90f);

        var btn = go.AddComponent<Button>(); btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        Color hov = new Color(Mathf.Min(cor.r + 0.15f, 1f), Mathf.Min(cor.g + 0.10f, 1f), Mathf.Min(cor.b + 0.08f, 1f), cor.a);
        btn.colors = new ColorBlock { normalColor = cor, highlightedColor = hov, pressedColor = new Color(cor.r * 0.7f, cor.g * 0.7f, cor.b * 0.7f, cor.a), selectedColor = cor, disabledColor = new Color(cor.r * 0.5f, cor.g * 0.5f, cor.b * 0.5f, 0.5f), colorMultiplier = 1f, fadeDuration = 0.1f };
        btn.onClick.AddListener(acao);

        var txtGO = new GameObject("Lbl"); txtGO.transform.SetParent(go.transform, false);
        var rtTxt = txtGO.AddComponent<RectTransform>();
        rtTxt.anchorMin = new Vector2(0.04f, 0f); rtTxt.anchorMax = Vector2.one; rtTxt.offsetMin = rtTxt.offsetMax = Vector2.zero;
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 22; tmp.color = new Color(0.92f, 0.82f, 0.68f);
        tmp.alignment = TextAlignmentOptions.Center; tmp.fontStyle = FontStyles.Bold;
        return go;
    }
}
