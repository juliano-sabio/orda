using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.SceneManagement;

// Adicione este script a qualquer GameObject da cena CharacterSelection.
// Ele cria apenas os botoes VOLTAR e JOGAR no canvas existente.
public class BotoesSelecaoPersonagem : MonoBehaviour
{
    [Header("Nome das cenas")]
    public string cenaMenu = "menu_inicial";
    public string cenaEscolherTerreno = "escolher terreno";

    void Start()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[BotoesSelecao] Nenhum Canvas encontrado na cena!");
            return;
        }

        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        CriarBotaoVoltar(canvas.gameObject);
        CriarBotaoJogar(canvas.gameObject);
    }

    void CriarBotaoVoltar(GameObject canvas)
    {
        GameObject go = new GameObject("BotaoVoltar");
        go.transform.SetParent(canvas.transform, false);

        RectTransform r = go.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(0f, 0f);
        r.anchorMax = new Vector2(0f, 0f);
        r.pivot = new Vector2(0f, 0f);
        r.anchoredPosition = new Vector2(20f, 20f);
        r.sizeDelta = new Vector2(180f, 60f);

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.6f, 0.1f, 0.1f);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(Voltar);

        GameObject txtGO = new GameObject("Texto");
        txtGO.transform.SetParent(go.transform, false);
        RectTransform rTxt = txtGO.AddComponent<RectTransform>();
        rTxt.anchorMin = Vector2.zero;
        rTxt.anchorMax = Vector2.one;
        rTxt.offsetMin = Vector2.zero;
        rTxt.offsetMax = Vector2.zero;

        TextMeshProUGUI txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text = "← VOLTAR";
        txt.fontSize = 22f;
        txt.fontStyle = FontStyles.Bold;
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.Center;
    }

    void CriarBotaoJogar(GameObject canvas)
    {
        GameObject go = new GameObject("BotaoJogar");
        go.transform.SetParent(canvas.transform, false);

        RectTransform r = go.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(1f, 0f);
        r.anchorMax = new Vector2(1f, 0f);
        r.pivot = new Vector2(1f, 0f);
        r.anchoredPosition = new Vector2(-20f, 20f);
        r.sizeDelta = new Vector2(220f, 60f);

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.1f, 0.55f, 0.15f);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(Jogar);

        GameObject txtGO = new GameObject("Texto");
        txtGO.transform.SetParent(go.transform, false);
        RectTransform rTxt = txtGO.AddComponent<RectTransform>();
        rTxt.anchorMin = Vector2.zero;
        rTxt.anchorMax = Vector2.one;
        rTxt.offsetMin = Vector2.zero;
        rTxt.offsetMax = Vector2.zero;

        TextMeshProUGUI txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text = "JOGAR";
        txt.fontSize = 26f;
        txt.fontStyle = FontStyles.Bold;
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.Center;
    }

    void Voltar()
    {
        if (GameSceneManager.Instance != null)
            GameSceneManager.Instance.GoToMainMenu();
        else
            SceneManager.LoadScene(cenaMenu);
    }

    void Jogar()
    {
        SceneManager.LoadScene(cenaEscolherTerreno);
    }
}
