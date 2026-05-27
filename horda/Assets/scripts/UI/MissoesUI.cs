using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class MissoesUI : MonoBehaviour
{
    [Header("Dados")]
    public CharacterData[] personagens;

    // ── Paleta dark fantasy ──────────────────────────────────────
    static readonly Color corFundo        = new Color(0.07f, 0.05f, 0.10f, 0.98f);
    static readonly Color corBorda        = new Color(0.78f, 0.66f, 0.35f);
    static readonly Color corAba          = new Color(0.12f, 0.10f, 0.18f);
    static readonly Color corAbaAtiva     = new Color(0.32f, 0.26f, 0.10f);
    static readonly Color corAbaTexto     = new Color(0.70f, 0.65f, 0.55f);
    static readonly Color corAbaAtivaTxt  = new Color(0.95f, 0.80f, 0.40f);
    static readonly Color corCard         = new Color(0.11f, 0.09f, 0.17f);
    static readonly Color corCardBorda    = new Color(0.22f, 0.18f, 0.30f);
    static readonly Color corTitulo       = new Color(0.95f, 0.80f, 0.40f);
    static readonly Color corTexto        = new Color(0.90f, 0.82f, 0.65f);
    static readonly Color corDesbloqueado = new Color(0.30f, 0.85f, 0.40f);
    static readonly Color corBloqueado    = new Color(0.80f, 0.28f, 0.22f);

    enum Aba { Personagens, Ultimates, Passivas }
    Aba abaAtiva = Aba.Personagens;

    GameObject painelRaiz;
    GameObject painelContent;
    GameObject areaConteudo;
    Button[] botoesAba = new Button[3];
    TextMeshProUGUI[] textosBotoesAba = new TextMeshProUGUI[3];
    bool aberto = false;

    IEnumerator Start()
    {
        yield return null;

        // Pega dados do CharacterSelectionUI se não foram atribuídos no Inspector
        if (personagens == null || personagens.Length == 0)
        {
            var charUI = FindObjectOfType<CharacterSelectionUI>();
            if (charUI != null) personagens = charUI.characters;
        }

        var canvasGO = GameObject.Find("CanvasPrincipal");
        if (canvasGO == null) yield break;

        CriarPainel(canvasGO);
    }

    void CriarPainel(GameObject canvas)
    {
        // Raiz: cobre a tela inteira
        painelRaiz = new GameObject("PainelMissoes");
        painelRaiz.transform.SetParent(canvas.transform, false);
        var rRaiz = painelRaiz.AddComponent<RectTransform>();
        rRaiz.anchorMin = Vector2.zero;
        rRaiz.anchorMax = Vector2.one;
        rRaiz.offsetMin = rRaiz.offsetMax = Vector2.zero;

        // Backdrop escuro
        var backdrop = new GameObject("Backdrop");
        backdrop.transform.SetParent(painelRaiz.transform, false);
        var rBack = backdrop.AddComponent<RectTransform>();
        rBack.anchorMin = Vector2.zero; rBack.anchorMax = Vector2.one;
        rBack.offsetMin = rBack.offsetMax = Vector2.zero;
        backdrop.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

        // Caixa central do painel
        painelContent = new GameObject("Content");
        painelContent.transform.SetParent(painelRaiz.transform, false);
        var rC = painelContent.AddComponent<RectTransform>();
        rC.anchorMin = new Vector2(0.08f, 0.05f);
        rC.anchorMax = new Vector2(0.92f, 0.95f);
        rC.offsetMin = rC.offsetMax = Vector2.zero;
        painelContent.AddComponent<Image>().color = corFundo;

        // Bordas douradas (topo, base, esq, dir)
        CriarBorda(painelContent, "BordaTopo",  new Vector2(0f,  1f), new Vector2(1f, 1f), new Vector2(0f, -2f), new Vector2(0f,  0f));
        CriarBorda(painelContent, "BordaBase",  new Vector2(0f,  0f), new Vector2(1f, 0f), new Vector2(0f,  0f), new Vector2(0f,  2f));
        CriarBorda(painelContent, "BordaEsq",   new Vector2(0f,  0f), new Vector2(0f, 1f), new Vector2(0f,  0f), new Vector2(2f,  0f));
        CriarBorda(painelContent, "BordaDir",   new Vector2(1f,  0f), new Vector2(1f, 1f), new Vector2(-2f, 0f), new Vector2(0f,  0f));

        // Título
        var titulo = CriarTexto(painelContent, "Titulo",
            new Vector2(0.04f, 0.92f), new Vector2(0.88f, 1f),
            "MISSOES DE DESBLOQUEIO", 18f, FontStyles.Bold, corTitulo);
        titulo.alignment = TextAlignmentOptions.MidlineLeft;

        // Botão fechar (X)
        CriarBotaoFechar(painelContent);

        // Linha separadora abaixo do título
        CriarSeparador(painelContent, new Vector2(0.02f, 0.905f), new Vector2(0.98f, 0.908f));

        // Abas
        CriarAbas();

        // Linha separadora abaixo das abas
        CriarSeparador(painelContent, new Vector2(0.02f, 0.835f), new Vector2(0.98f, 0.838f));

        // Área de conteúdo
        CriarAreaConteudo();

        painelRaiz.SetActive(false);
        MostrarAba(Aba.Personagens);
    }

    void CriarBorda(GameObject pai, string nome, Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai.transform, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = ancMin; r.anchorMax = ancMax;
        r.offsetMin = offMin; r.offsetMax = offMax;
        go.AddComponent<Image>().color = corBorda;
    }

    void CriarSeparador(GameObject pai, Vector2 ancMin, Vector2 ancMax)
    {
        var go = new GameObject("Sep");
        go.transform.SetParent(pai.transform, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = ancMin; r.anchorMax = ancMax;
        r.offsetMin = r.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = new Color(corBorda.r, corBorda.g, corBorda.b, 0.45f);
    }

    void CriarBotaoFechar(GameObject pai)
    {
        var go = new GameObject("BtnFechar");
        go.transform.SetParent(pai.transform, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(1f, 1f);
        r.anchoredPosition = new Vector2(-6f, -6f);
        r.sizeDelta = new Vector2(34f, 28f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.28f, 0.08f, 0.08f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition    = Selectable.Transition.None;
        btn.onClick.AddListener(TogglePainel);

        var txt = CriarTexto(go, "Txt", Vector2.zero, Vector2.one,
            "X", 14f, FontStyles.Bold, new Color(0.95f, 0.40f, 0.35f));
        txt.alignment = TextAlignmentOptions.Center;
    }

    void CriarAbas()
    {
        string[] nomes = { "PERSONAGENS", "ULTIMATES", "PASSIVAS" };
        for (int i = 0; i < 3; i++)
        {
            float xMin = 0.02f + i * (0.32f);
            float xMax = xMin + 0.30f;
            int idx = i;

            var go = new GameObject($"Aba_{nomes[i]}");
            go.transform.SetParent(painelContent.transform, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(xMin, 0.840f);
            r.anchorMax = new Vector2(xMax, 0.905f);
            r.offsetMin = new Vector2(2f, 2f);
            r.offsetMax = new Vector2(-2f, 0f);

            var img = go.AddComponent<Image>();
            img.color = corAba;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition    = Selectable.Transition.None;
            btn.onClick.AddListener(() => MostrarAba((Aba)idx));

            var txt = CriarTexto(go, "Txt", Vector2.zero, Vector2.one,
                nomes[i], 11f, FontStyles.Bold, corAbaTexto);
            txt.alignment = TextAlignmentOptions.Center;

            botoesAba[i] = btn;
            textosBotoesAba[i] = txt;
        }
    }

    void CriarAreaConteudo()
    {
        var mascara = new GameObject("Mascara");
        mascara.transform.SetParent(painelContent.transform, false);
        var rm = mascara.AddComponent<RectTransform>();
        rm.anchorMin = new Vector2(0.01f, 0.01f);
        rm.anchorMax = new Vector2(0.99f, 0.835f);
        rm.offsetMin = new Vector2(4f, 0f);
        rm.offsetMax = new Vector2(-4f, 0f);
        mascara.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        mascara.AddComponent<Mask>().showMaskGraphic = false;

        areaConteudo = new GameObject("Conteudo");
        areaConteudo.transform.SetParent(mascara.transform, false);
        var rc = areaConteudo.AddComponent<RectTransform>();
        rc.anchorMin = new Vector2(0f, 1f);
        rc.anchorMax = Vector2.one;
        rc.pivot     = new Vector2(0.5f, 1f);
        rc.offsetMin = rc.offsetMax = Vector2.zero;

        var layout = areaConteudo.AddComponent<VerticalLayoutGroup>();
        layout.spacing                = 5f;
        layout.padding                = new RectOffset(4, 4, 6, 6);
        layout.childForceExpandWidth  = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth      = true;
        layout.childControlHeight     = true;

        areaConteudo.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        var sr = mascara.AddComponent<ScrollRect>();
        sr.content           = rc;
        sr.vertical          = true;
        sr.horizontal        = false;
        sr.scrollSensitivity = 30f;
        sr.movementType      = ScrollRect.MovementType.Clamped;
    }

    void MostrarAba(Aba aba)
    {
        abaAtiva = aba;

        for (int i = 0; i < 3; i++)
        {
            if (botoesAba[i] == null) continue;
            bool ativo = i == (int)aba;
            botoesAba[i].GetComponent<Image>().color = ativo ? corAbaAtiva : corAba;
            if (textosBotoesAba[i] != null)
                textosBotoesAba[i].color = ativo ? corAbaAtivaTxt : corAbaTexto;
        }

        foreach (Transform filho in areaConteudo.transform)
            Destroy(filho.gameObject);

        int playerLevel = PlayerPrefs.GetInt("PlayerLevel", 1);

        switch (aba)
        {
            case Aba.Personagens: PopularPersonagens(playerLevel); break;
            case Aba.Ultimates:   PopularUltimates(playerLevel);   break;
            case Aba.Passivas:    PopularPassivas();                break;
        }

        areaConteudo.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
    }

    void PopularPersonagens(int playerLevel)
    {
        if (personagens == null) return;
        foreach (var p in personagens)
        {
            if (p == null) continue;
            bool desbloqueado = p.unlocked || playerLevel >= p.unlockLevel;
            string status  = desbloqueado ? "DESBLOQUEADO" : $"Nivel {p.unlockLevel} necessario";
            CriarCard(p.characterName, status, p.missaoDesbloqueio,
                CharacterData.GetElementColor(p.baseElement), desbloqueado);
        }
    }

    void PopularUltimates(int playerLevel)
    {
        if (personagens == null) return;
        foreach (var p in personagens)
        {
            if (p == null || p.ultimatesDisponiveis == null) continue;
            foreach (var u in p.ultimatesDisponiveis)
            {
                if (u == null) continue;
                bool desbloqueado = u.isUnlocked && playerLevel >= u.requiredLevel;
                string status = desbloqueado
                    ? "DESBLOQUEADO"
                    : $"Nivel {u.requiredLevel} necessario";
                CriarCard(u.ultimateName, status,
                    $"[{p.characterName}]  {u.description}",
                    u.GetElementColor(), desbloqueado);
            }
        }
    }

    void PopularPassivas()
    {
        if (personagens == null) return;
        int playerLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
        bool alguma = false;
        foreach (var p in personagens)
        {
            if (p == null || p.passivasDisponiveis == null) continue;
            foreach (var passiva in p.passivasDisponiveis)
            {
                if (passiva == null) continue;
                alguma = true;
                bool desbloqueado = passiva.isUnlocked && playerLevel >= passiva.requiredLevel;
                string status = desbloqueado ? "DESBLOQUEADO" : $"Nivel {passiva.requiredLevel} necessario";
                string bonus  = passiva.GetBonusDescription();
                string desc   = string.IsNullOrEmpty(bonus) ? passiva.description : bonus;
                CriarCard(passiva.passiveName, status, $"[{p.characterName}]  {desc}",
                    CharacterData.GetElementColor(p.baseElement), desbloqueado);
            }
        }
        if (!alguma)
            CriarCardInfo("Passivas",
                "As passivas sao desbloqueadas ao subir de nivel e selecionar cartas de habilidade durante a partida.");
    }

    void CriarCard(string nome, string status, string descricao, Color corAccento, bool desbloqueado)
    {
        var card = new GameObject($"Card_{nome}");
        card.transform.SetParent(areaConteudo.transform, false);

        var le = card.AddComponent<LayoutElement>();
        le.minHeight       = 80f;
        le.preferredHeight = 80f;

        var img = card.AddComponent<Image>();
        img.color = corCard;

        // Borda superior fina (cor do acento)
        var bordaTopo = new GameObject("BordaTopo");
        bordaTopo.transform.SetParent(card.transform, false);
        var rbt = bordaTopo.AddComponent<RectTransform>();
        rbt.anchorMin = new Vector2(0f, 1f); rbt.anchorMax = new Vector2(1f, 1f);
        rbt.offsetMin = new Vector2(0f, -2f); rbt.offsetMax = Vector2.zero;
        bordaTopo.AddComponent<Image>().color = corAccento;

        // Borda esquerda grossa colorida
        var bordaEsq = new GameObject("BordaEsq");
        bordaEsq.transform.SetParent(card.transform, false);
        var rbe = bordaEsq.AddComponent<RectTransform>();
        rbe.anchorMin = Vector2.zero; rbe.anchorMax = new Vector2(0f, 1f);
        rbe.offsetMin = Vector2.zero; rbe.offsetMax = new Vector2(5f, 0f);
        bordaEsq.AddComponent<Image>().color = corAccento;

        // Borda externa sutil
        var bordaExt = new GameObject("BordaExt");
        bordaExt.transform.SetParent(card.transform, false);
        var rbx = bordaExt.AddComponent<RectTransform>();
        rbx.anchorMin = Vector2.zero; rbx.anchorMax = Vector2.one;
        rbx.offsetMin = Vector2.zero; rbx.offsetMax = Vector2.zero;
        var imgBx = bordaExt.AddComponent<Image>();
        imgBx.color = corCardBorda;
        imgBx.raycastTarget = false;

        // Nome
        var txtNome = CriarTexto(card, "Nome",
            new Vector2(0.05f, 0.58f), new Vector2(0.82f, 0.96f),
            nome, 14f, FontStyles.Bold, corTexto);
        txtNome.alignment = TextAlignmentOptions.MidlineLeft;

        // Status
        var txtStatus = CriarTexto(card, "Status",
            new Vector2(0.05f, 0.36f), new Vector2(0.95f, 0.58f),
            status, 10f, FontStyles.Bold,
            desbloqueado ? corDesbloqueado : corBloqueado);
        txtStatus.alignment = TextAlignmentOptions.MidlineLeft;

        // Descrição
        var txtDesc = CriarTexto(card, "Desc",
            new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.38f),
            descricao, 9f, FontStyles.Normal,
            new Color(0.60f, 0.55f, 0.45f));
        txtDesc.alignment          = TextAlignmentOptions.MidlineLeft;
        txtDesc.enableWordWrapping = true;
    }

    void CriarCardInfo(string titulo, string texto)
    {
        var card = new GameObject("CardInfo");
        card.transform.SetParent(areaConteudo.transform, false);

        var le = card.AddComponent<LayoutElement>();
        le.minHeight       = 120f;
        le.preferredHeight = 120f;

        card.AddComponent<Image>().color = corCard;

        // Borda topo dourada
        var bordaTopo = new GameObject("BordaTopo");
        bordaTopo.transform.SetParent(card.transform, false);
        var rbt = bordaTopo.AddComponent<RectTransform>();
        rbt.anchorMin = new Vector2(0f, 1f); rbt.anchorMax = new Vector2(1f, 1f);
        rbt.offsetMin = new Vector2(0f, -2f); rbt.offsetMax = Vector2.zero;
        bordaTopo.AddComponent<Image>().color = corBorda;

        var t = CriarTexto(card, "Txt",
            new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f),
            $"<b>{titulo}</b>\n\n{texto}", 11f, FontStyles.Normal, corTexto);
        t.enableWordWrapping = true;
        t.alignment          = TextAlignmentOptions.TopLeft;
    }

    public void TogglePainel()
    {
        aberto = !aberto;
        painelRaiz.SetActive(aberto);
        if (aberto) MostrarAba(abaAtiva);
    }

    TextMeshProUGUI CriarTexto(GameObject parent, string nome,
        Vector2 ancMin, Vector2 ancMax,
        string texto, float size, FontStyles style, Color cor)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(parent.transform, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = ancMin; r.anchorMax = ancMax;
        r.offsetMin = r.offsetMax = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text      = texto;
        t.fontSize  = size;
        t.fontStyle = style;
        t.color     = cor;
        t.alignment = TextAlignmentOptions.Center;
        return t;
    }
}
