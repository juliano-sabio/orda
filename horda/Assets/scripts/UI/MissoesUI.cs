using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// Adicione num GameObject vazio na cena CharacterSelection.
// Ele cria o painel de missões com 3 abas: Personagens, Ultimates, Passivas.
public class MissoesUI : MonoBehaviour
{
    [Header("Dados")]
    public CharacterData[] personagens;

    // ── Paleta ───────────────────────────────────────────────────
    static readonly Color corFundo        = new Color(0.05f, 0.04f, 0.12f, 0.97f);
    static readonly Color corAba          = new Color(0.10f, 0.08f, 0.22f);
    static readonly Color corAbaAtiva     = new Color(0.22f, 0.10f, 0.48f);
    static readonly Color corCard         = new Color(0.10f, 0.08f, 0.20f);
    static readonly Color corDesbloqueado = new Color(0.2f,  0.8f,  0.3f);
    static readonly Color corBloqueado    = new Color(0.7f,  0.2f,  0.2f);

    enum Aba { Personagens, Ultimates, Passivas }
    Aba abaAtiva = Aba.Personagens;

    GameObject painelPrincipal;
    GameObject areaConteudo;
    Button[] botoesAba = new Button[3];
    bool aberto = false;

    void Start()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        CriarBotaoAbrirFechar(canvas.gameObject);
        CriarPainel(canvas.gameObject);
    }

    // ── Botão flutuante para abrir ───────────────────────────────
    void CriarBotaoAbrirFechar(GameObject canvas)
    {
        var go = new GameObject("BtnMissoes");
        go.transform.SetParent(canvas.transform, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f, 0f);
        r.anchorMax = new Vector2(0.5f, 0f);
        r.pivot     = new Vector2(0.5f, 0f);
        r.anchoredPosition = new Vector2(0f, 16f);
        r.sizeDelta = new Vector2(180f, 48f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.10f, 0.48f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition    = Selectable.Transition.None;
        btn.onClick.AddListener(TogglePainel);

        var txt = CriarTexto(go, "Txt", Vector2.zero, Vector2.one,
            "MISSÕES", 18f, FontStyles.Bold, Color.white);
        txt.alignment = TextAlignmentOptions.Center;
    }

    // ── Painel principal ─────────────────────────────────────────
    void CriarPainel(GameObject canvas)
    {
        painelPrincipal = new GameObject("PainelMissoes");
        painelPrincipal.transform.SetParent(canvas.transform, false);
        var r = painelPrincipal.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(0.55f, 0.05f); r.anchorMax = new Vector2(0.93f, 0.95f);
        r.offsetMin = r.offsetMax = Vector2.zero;

        painelPrincipal.AddComponent<Image>().color = corFundo;

        // Título
        CriarTexto(painelPrincipal, "Titulo",
            new Vector2(0f, 0.92f), new Vector2(1f, 1f),
            "MISSÕES DE DESBLOQUEIO", 20f, FontStyles.Bold,
            new Color(0.8f, 0.6f, 1f));

        // Abas
        CriarAbas();

        // Área de conteúdo com scroll
        CriarAreaConteudo();

        painelPrincipal.SetActive(false);
        MostrarAba(Aba.Personagens);
    }

    void CriarAbas()
    {
        string[] nomes = { "Personagens", "Ultimates", "Passivas" };
        for (int i = 0; i < 3; i++)
        {
            float xMin = i * (1f / 3f);
            float xMax = (i + 1) * (1f / 3f);
            int idx    = i;

            var go = new GameObject($"Aba_{nomes[i]}");
            go.transform.SetParent(painelPrincipal.transform, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(xMin, 0.84f); r.anchorMax = new Vector2(xMax, 0.92f);
            r.offsetMin = new Vector2(2f, 0f); r.offsetMax = new Vector2(-2f, 0f);

            var img = go.AddComponent<Image>();
            img.color = corAba;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition    = Selectable.Transition.None;
            btn.onClick.AddListener(() => MostrarAba((Aba)idx));

            CriarTexto(go, "Txt", Vector2.zero, Vector2.one,
                nomes[i], 13f, FontStyles.Bold, Color.white);

            botoesAba[i] = btn;
        }
    }

    void CriarAreaConteudo()
    {
        // Máscara de scroll
        var mascara = new GameObject("Mascara");
        mascara.transform.SetParent(painelPrincipal.transform, false);
        var rm = mascara.AddComponent<RectTransform>();
        rm.anchorMin = new Vector2(0f, 0.02f); rm.anchorMax = new Vector2(1f, 0.84f);
        rm.offsetMin = new Vector2(6f, 0f); rm.offsetMax = new Vector2(-6f, 0f);
        mascara.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        mascara.AddComponent<Mask>().showMaskGraphic = false;

        // Conteúdo scrollável
        areaConteudo = new GameObject("Conteudo");
        areaConteudo.transform.SetParent(mascara.transform, false);
        var rc = areaConteudo.AddComponent<RectTransform>();
        rc.anchorMin = new Vector2(0f, 1f); rc.anchorMax = Vector2.one;
        rc.pivot     = new Vector2(0.5f, 1f);
        rc.offsetMin = rc.offsetMax = Vector2.zero;

        var layout = areaConteudo.AddComponent<VerticalLayoutGroup>();
        layout.spacing            = 6f;
        layout.padding            = new RectOffset(4, 4, 6, 6);
        layout.childForceExpandWidth  = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth      = true;
        layout.childControlHeight     = true;

        areaConteudo.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        // ScrollRect
        var sr = mascara.AddComponent<ScrollRect>();
        sr.content    = rc;
        sr.vertical   = true;
        sr.horizontal = false;
        sr.scrollSensitivity = 30f;
        sr.movementType = ScrollRect.MovementType.Clamped;
    }

    // ── Mostrar aba ──────────────────────────────────────────────
    void MostrarAba(Aba aba)
    {
        abaAtiva = aba;

        for (int i = 0; i < 3; i++)
        {
            if (botoesAba[i] == null) continue;
            botoesAba[i].GetComponent<Image>().color =
                i == (int)aba ? corAbaAtiva : corAba;
        }

        // Limpa conteúdo anterior
        foreach (Transform filho in areaConteudo.transform)
            Destroy(filho.gameObject);

        int playerLevel = PlayerPrefs.GetInt("PlayerLevel", 1);

        switch (aba)
        {
            case Aba.Personagens: PopularPersonagens(playerLevel); break;
            case Aba.Ultimates:   PopularUltimates(playerLevel);   break;
            case Aba.Passivas:    PopularPassivas();                break;
        }

        // Reset scroll
        var rc = areaConteudo.GetComponent<RectTransform>();
        rc.anchoredPosition = Vector2.zero;
    }

    // ── Cards de Personagens ─────────────────────────────────────
    void PopularPersonagens(int playerLevel)
    {
        if (personagens == null) return;
        foreach (var p in personagens)
        {
            if (p == null) continue;
            bool desbloqueado = p.unlocked || playerLevel >= p.unlockLevel;
            string status     = desbloqueado ? "✔ DESBLOQUEADO" : $"🔒 Nível {p.unlockLevel} necessário";
            string missao     = p.missaoDesbloqueio;
            CriarCard(p.characterName, status, missao,
                CharacterData.GetElementColor(p.baseElement), desbloqueado);
        }
    }

    // ── Cards de Ultimates ───────────────────────────────────────
    void PopularUltimates(int playerLevel)
    {
        if (personagens == null) return;
        foreach (var p in personagens)
        {
            if (p == null) continue;
            if (p.ultimatesDisponiveis == null) continue;

            foreach (var u in p.ultimatesDisponiveis)
            {
                if (u == null) continue;
                bool desbloqueado = u.isUnlocked && playerLevel >= u.requiredLevel;
                string status     = desbloqueado
                    ? "✔ DESBLOQUEADO"
                    : $"🔒 Nível {u.requiredLevel} necessário";
                string descricao  = $"[{p.characterName}]  {u.description}";
                CriarCard(u.ultimateName, status, descricao,
                    u.GetElementColor(), desbloqueado);
            }
        }
    }

    // ── Cards de Passivas ────────────────────────────────────────
    void PopularPassivas()
    {
        CriarCardInfo("Sistema de Passivas",
            "As passivas são desbloqueadas ao subir de nível e selecionar cartas de habilidade durante a partida.\n\nCada nível concede uma nova escolha de habilidade.");
    }

    // ── Card genérico ────────────────────────────────────────────
    void CriarCard(string nome, string status, string descricao, Color cor, bool desbloqueado)
    {
        var card = new GameObject($"Card_{nome}");
        card.transform.SetParent(areaConteudo.transform, false);

        var le = card.AddComponent<LayoutElement>();
        le.minHeight       = 90f;
        le.preferredHeight = 90f;

        var img = card.AddComponent<Image>();
        img.color = corCard;

        // Borda esquerda colorida
        var borda = new GameObject("Borda");
        borda.transform.SetParent(card.transform, false);
        var rb = borda.AddComponent<RectTransform>();
        rb.anchorMin = Vector2.zero; rb.anchorMax = new Vector2(0f, 1f);
        rb.offsetMin = Vector2.zero; rb.offsetMax  = new Vector2(4f, 0f);
        borda.AddComponent<Image>().color = cor;

        // Nome
        var txtNome = CriarTexto(card, "Nome",
            new Vector2(0.04f, 0.58f), new Vector2(0.80f, 0.96f),
            nome, 15f, FontStyles.Bold, Color.white);
        txtNome.alignment = TextAlignmentOptions.Left;

        // Status
        var txtStatus = CriarTexto(card, "Status",
            new Vector2(0.04f, 0.38f), new Vector2(0.96f, 0.60f),
            status, 11f, FontStyles.Bold,
            desbloqueado ? corDesbloqueado : corBloqueado);
        txtStatus.alignment = TextAlignmentOptions.Left;

        // Descrição / missão
        var txtDesc = CriarTexto(card, "Desc",
            new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.40f),
            descricao, 10f, FontStyles.Normal,
            new Color(0.7f, 0.7f, 0.7f));
        txtDesc.alignment        = TextAlignmentOptions.Left;
        txtDesc.enableWordWrapping = true;
    }

    void CriarCardInfo(string titulo, string texto)
    {
        var card = new GameObject("CardInfo");
        card.transform.SetParent(areaConteudo.transform, false);

        var le = card.AddComponent<LayoutElement>();
        le.minHeight       = 130f;
        le.preferredHeight = 130f;

        card.AddComponent<Image>().color = corCard;

        var t = CriarTexto(card, "Txt",
            new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f),
            $"<b>{titulo}</b>\n\n{texto}", 12f, FontStyles.Normal,
            new Color(0.8f, 0.8f, 0.85f));
        t.enableWordWrapping = true;
        t.alignment          = TextAlignmentOptions.TopLeft;
    }

    // ── Toggle ───────────────────────────────────────────────────
    void TogglePainel()
    {
        aberto = !aberto;
        painelPrincipal.SetActive(aberto);
        if (aberto) MostrarAba(abaAtiva);
    }

    // ── Helper ───────────────────────────────────────────────────
    TextMeshProUGUI CriarTexto(GameObject parent, string nome,
        Vector2 ancMin, Vector2 ancMax,
        string texto, float size, FontStyles style, Color cor)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(parent.transform, false);
        var r  = go.AddComponent<RectTransform>();
        r.anchorMin = ancMin; r.anchorMax = ancMax;
        r.offsetMin = r.offsetMax = Vector2.zero;
        var t  = go.AddComponent<TextMeshProUGUI>();
        t.text = texto; t.fontSize = size;
        t.fontStyle = style; t.color = cor;
        t.alignment = TextAlignmentOptions.Center;
        return t;
    }
}
