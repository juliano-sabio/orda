using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.SceneManagement;

public class EscolherTerrenoUI : MonoBehaviour
{
    [System.Serializable]
    public class ConfigFase
    {
        public string nome        = "Fase";
        public string nomeCena    = "primeira_fase";
        public string descricao   = "Descrição da fase";
        public int    dificuldade = 1;
        public bool   desbloqueada = true;
        public Color  cor         = new Color(0.2f, 0.5f, 0.8f);
    }

    [Header("Fases disponíveis")]
    public ConfigFase[] fases = new ConfigFase[]
    {
        new ConfigFase { nome = "Primeira Fase",      nomeCena = "primeira_fase",      descricao = "O início da jornada.",          dificuldade = 1, desbloqueada = true, cor = new Color(0.15f, 0.6f, 0.15f) },
        new ConfigFase { nome = "Segunda Fase",       nomeCena = "segunda_fase",       descricao = "Os inimigos ficam mais fortes.", dificuldade = 2, desbloqueada = true, cor = new Color(0.7f,  0.6f, 0.05f) },
        new ConfigFase { nome = "Terceira Fase",      nomeCena = "terceira_fase",      descricao = "Apenas os mais corajosos.",      dificuldade = 3, desbloqueada = true, cor = new Color(0.7f,  0.2f, 0.05f) },
        new ConfigFase { nome = "Sobrevivência",      nomeCena = "Modo_sobrevivencia", descricao = "Sobreviva o máximo que puder!",  dificuldade = 5, desbloqueada = true, cor = new Color(0.5f,  0.05f,0.7f)  },
    };

    [Header("Cena de voltar")]
    public string cenaVoltar = "CharacterSelection";

    [Header("Assets")]
    public Sprite spriteCard;
    public Sprite spriteFundoImg;

    // ──────────────────────────────────────────────────────────────
    void Start()
    {
        GarantirEventSystem();
#if UNITY_EDITOR
        if (spriteCard == null)
            foreach (var a in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(
                "Assets/assets/UI/charselection/testecaractere.ase"))
                if (a is Sprite s) { spriteCard = s; break; }
        if (spriteFundoImg == null)
            spriteFundoImg = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/assets/UI/menu_inicial/IMG_6856 (1).png");
#endif

        GameObject canvasGO = CriarCanvas();
        CriarFundo(canvasGO);
        CriarTitulo(canvasGO);
        CriarCards(canvasGO);
        CriarBotaoVoltar(canvasGO);
    }

    // ──────────────────────────────────────────────────────────────
    // EventSystem

    void GarantirEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    // ──────────────────────────────────────────────────────────────
    // Canvas

    GameObject CriarCanvas()
    {
        GameObject go = new GameObject("Canvas_Terreno");
        Canvas c = go.AddComponent<Canvas>();
        c.renderMode  = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 10;

        CanvasScaler cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    // ──────────────────────────────────────────────────────────────
    // Elementos base

    void CriarFundo(GameObject canvas)
    {
        GameObject go = new GameObject("Fundo");
        go.transform.SetParent(canvas.transform, false);
        Stretch(go);
        var img = go.AddComponent<Image>();
        if (spriteFundoImg != null)
        {
            img.sprite = spriteFundoImg;
            img.type   = Image.Type.Simple;
            img.color  = Color.white;
            img.preserveAspect = false;
        }
        else
        {
            img.color = new Color(0.06f, 0.06f, 0.12f);
        }
    }

    void CriarTitulo(GameObject canvas)
    {
        GameObject go = new GameObject("Titulo");
        go.transform.SetParent(canvas.transform, false);
        RectTransform r = go.AddComponent<RectTransform>();
        r.anchorMin        = new Vector2(0f, 1f);
        r.anchorMax        = new Vector2(1f, 1f);
        r.pivot            = new Vector2(0.5f, 1f);
        r.anchoredPosition = new Vector2(0f, -10f);
        r.sizeDelta        = new Vector2(0f, 70f);

        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text      = "ESCOLHA O TERRENO";
        t.fontSize  = 40f;
        t.fontStyle = FontStyles.Bold;
        t.color     = Color.white;
        t.alignment = TextAlignmentOptions.Center;
    }

    // ──────────────────────────────────────────────────────────────
    // Cards — 4 cartas lado a lado dividindo a tela em 4 colunas

    void CriarCards(GameObject canvas)
    {
        int total = fases.Length;
        float margem    = 0.015f;   // margem lateral total de cada card
        float largura   = (1f - margem * (total + 1)) / total;

        for (int i = 0; i < total; i++)
        {
            float xMin = margem + i * (largura + margem);
            float xMax = xMin + largura;
            CriarCard(canvas, fases[i], new Vector2(xMin, 0.1f), new Vector2(xMax, 0.88f));
        }
    }

    void CriarCard(GameObject canvas, ConfigFase fase, Vector2 ancMin, Vector2 ancMax)
    {
        GameObject card = new GameObject($"Card_{fase.nome}");
        card.transform.SetParent(canvas.transform, false);

        RectTransform r = card.AddComponent<RectTransform>();
        r.anchorMin  = ancMin;
        r.anchorMax  = ancMax;
        r.offsetMin  = new Vector2(0f, 0f);
        r.offsetMax  = new Vector2(0f, 0f);

        // Fundo do card
        Image bg = card.AddComponent<Image>();
        if (spriteCard != null)
        {
            bg.sprite = spriteCard;
            bg.type   = Image.Type.Simple;
            bg.preserveAspect = false;
            bg.color = fase.desbloqueada
                ? new Color(Mathf.Max(fase.cor.r, 0.35f), Mathf.Max(fase.cor.g, 0.35f), Mathf.Max(fase.cor.b, 0.35f), 1f)
                : new Color(0.30f, 0.25f, 0.25f, 1f);
        }
        else
        {
            bg.color = fase.desbloqueada
                ? new Color(fase.cor.r * 0.25f, fase.cor.g * 0.25f, fase.cor.b * 0.25f, 1f)
                : new Color(0.12f, 0.12f, 0.12f, 1f);
        }

        // Botão
        Button btn = card.AddComponent<Button>();
        btn.targetGraphic  = bg;
        btn.interactable   = fase.desbloqueada;
        card.AddComponent<CardHover>();

        if (fase.desbloqueada)
        {
            string cena = fase.nomeCena;  // captura local
            btn.onClick.AddListener(() =>
            {
                Debug.Log($"[EscolherTerreno] Indo para: {cena}");
                Time.timeScale = 1f;
                PlayerPrefs.SetString("ProximaCena", cena);
                SceneManager.LoadScene("loading_screen");
            });

            ColorBlock cb  = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = new Color(1.0f, 0.85f, 0.85f, 1f);
            cb.pressedColor     = new Color(fase.cor.r * 1.4f, fase.cor.g * 1.4f, fase.cor.b * 1.4f, 1f);
            cb.colorMultiplier  = 1f;
            btn.colors = cb;
        }

        // Barra colorida no topo
        GameObject barra = new GameObject("Barra");
        barra.transform.SetParent(card.transform, false);
        RectTransform rB = barra.AddComponent<RectTransform>();
        rB.anchorMin = new Vector2(0f, 1f); rB.anchorMax = new Vector2(1f, 1f);
        rB.pivot     = new Vector2(0.5f, 1f);
        rB.anchoredPosition = Vector2.zero;
        rB.sizeDelta        = new Vector2(0f, 10f);
        barra.AddComponent<Image>().color = fase.desbloqueada ? fase.cor : Color.gray;

        // Nome
        AdicionarTexto(card, "Nome",
            new Vector2(0f, 0.72f), new Vector2(1f, 0.95f),
            fase.nome, 20f, FontStyles.Bold,
            fase.desbloqueada ? Color.white : Color.gray);

        // Dificuldade
        string[] difs = { "", "FÁCIL", "NORMAL", "DIFÍCIL", "ESPECIALISTA", "MESTRE" };
        Color[]  cors = { Color.white,
            new Color(0.2f,0.9f,0.2f), new Color(0.9f,0.8f,0.1f),
            new Color(1f,0.4f,0.1f),   new Color(0.8f,0.1f,0.8f), new Color(1f,0.2f,0.2f) };
        int d = Mathf.Clamp(fase.dificuldade, 1, 5);
        AdicionarTexto(card, "Dif",
            new Vector2(0f, 0.58f), new Vector2(1f, 0.74f),
            difs[d], 14f, FontStyles.Bold,
            fase.desbloqueada ? cors[d] : Color.gray);

        // Descrição
        var desc = AdicionarTexto(card, "Desc",
            new Vector2(0.05f, 0.25f), new Vector2(0.95f, 0.58f),
            fase.descricao, 13f, FontStyles.Normal,
            fase.desbloqueada ? new Color(0.8f, 0.8f, 0.8f) : new Color(0.4f, 0.4f, 0.4f));
        desc.textWrappingMode = TMPro.TextWrappingModes.Normal;

        // Rodapé do card
        if (fase.desbloqueada)
        {
            AdicionarTexto(card, "Jogar",
                new Vector2(0f, 0.02f), new Vector2(1f, 0.22f),
                "▶  JOGAR", 17f, FontStyles.Bold, fase.cor);
        }
        else
        {
            AdicionarTexto(card, "Lock",
                new Vector2(0f, 0.02f), new Vector2(1f, 0.22f),
                "🔒 BLOQUEADO", 15f, FontStyles.Bold, new Color(0.6f, 0.2f, 0.2f));
        }
    }

    // ──────────────────────────────────────────────────────────────
    // Botão Voltar

    void CriarBotaoVoltar(GameObject canvas)
    {
        GameObject go = new GameObject("BotaoVoltar");
        go.transform.SetParent(canvas.transform, false);

        RectTransform r = go.AddComponent<RectTransform>();
        r.anchorMin        = new Vector2(0f, 0f);
        r.anchorMax        = new Vector2(0f, 0f);
        r.pivot            = new Vector2(0f, 0f);
        r.anchoredPosition = new Vector2(20f, 12f);
        r.sizeDelta        = new Vector2(200f, 55f);

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.5f, 0.1f, 0.1f);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => SceneManager.LoadScene(cenaVoltar));

        AdicionarTexto(go, "Txt",
            Vector2.zero, Vector2.one,
            "← VOLTAR", 20f, FontStyles.Bold, Color.white);
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers

    TextMeshProUGUI AdicionarTexto(GameObject parent, string nome,
        Vector2 ancMin, Vector2 ancMax,
        string texto, float size, FontStyles style, Color cor)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(parent.transform, false);
        RectTransform r = go.AddComponent<RectTransform>();
        r.anchorMin = ancMin; r.anchorMax = ancMax;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;

        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text      = texto;
        t.fontSize  = size;
        t.fontStyle = style;
        t.color     = cor;
        t.alignment = TextAlignmentOptions.Center;
        return t;
    }

    void Stretch(GameObject go)
    {
        RectTransform r = go.GetComponent<RectTransform>();
        if (r == null) r = go.AddComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }
}

public class CardHover : MonoBehaviour,
    UnityEngine.EventSystems.IPointerEnterHandler,
    UnityEngine.EventSystems.IPointerExitHandler,
    UnityEngine.EventSystems.IPointerDownHandler,
    UnityEngine.EventSystems.IPointerUpHandler
{
    Coroutine cor;

    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData _)
        => Animar(1.06f, 0.15f);

    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData _)
        => Animar(1.00f, 0.15f);

    public void OnPointerDown(UnityEngine.EventSystems.PointerEventData _)
        => Animar(0.97f, 0.08f);

    public void OnPointerUp(UnityEngine.EventSystems.PointerEventData _)
        => Animar(1.06f, 0.08f);

    void Animar(float alvo, float dur)
    {
        if (cor != null) StopCoroutine(cor);
        cor = StartCoroutine(EscalaParaAlvo(alvo, dur));
    }

    System.Collections.IEnumerator EscalaParaAlvo(float alvo, float dur)
    {
        float inicio = transform.localScale.x;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float e = 1f - Mathf.Pow(1f - t / dur, 2f);
            float s = Mathf.Lerp(inicio, alvo, e);
            transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        transform.localScale = new Vector3(alvo, alvo, 1f);
    }
}
