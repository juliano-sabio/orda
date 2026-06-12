using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MissoesUI : MonoBehaviour
{
    [Header("Dados")]
    public CharacterData[] personagens;

    [Header("Sprites Dark Fantasy")]
    public Sprite spriteFundoPainel;  // bg_dungeon_esgoto
    public Sprite spriteBarraTopo;
    public Sprite spriteBotao;
    public Sprite spriteSlotPlayer;

    // Paleta alinhada com CharacterSelectionUI
    static readonly Color corFundo        = new Color(0.04f, 0.02f, 0.02f, 0.98f);
    static readonly Color corBarraTopo    = new Color(0.10f, 0.04f, 0.04f);
    static readonly Color corBorda        = new Color(0.78f, 0.66f, 0.35f);
    static readonly Color corAcento       = new Color(0.55f, 0.08f, 0.08f);
    static readonly Color corAba          = new Color(0.10f, 0.08f, 0.16f);
    static readonly Color corAbaAtiva     = new Color(0.32f, 0.26f, 0.10f);
    static readonly Color corAbaTexto     = new Color(0.65f, 0.60f, 0.50f);
    static readonly Color corAbaAtivaTxt  = new Color(0.95f, 0.80f, 0.40f);
    static readonly Color corCard         = new Color(0.10f, 0.08f, 0.16f);
    static readonly Color corTitulo       = new Color(0.95f, 0.80f, 0.40f);
    static readonly Color corTexto        = new Color(0.92f, 0.82f, 0.65f);
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

        var charUI = FindAnyObjectByType<CharacterSelectionUI>();
        if (charUI != null)
        {
            if (personagens == null || personagens.Length == 0)
                personagens = charUI.characters;
            if (spriteBarraTopo == null) spriteBarraTopo = charUI.spriteBarraTopo;
            if (spriteBotao     == null) spriteBotao     = charUI.spriteBotao;
            if (spriteSlotPlayer== null) spriteSlotPlayer= charUI.spriteSlotPlayer;
        }

#if UNITY_EDITOR
        if (spriteFundoPainel == null)
        {
            spriteFundoPainel = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/assets/UI/painelmissao.ase");
            if (spriteFundoPainel == null)
                foreach (var a in UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/assets/UI/painelmissao.ase"))
                    if (a is Sprite s && s.name == "painelmissao") { spriteFundoPainel = s; break; }
        }
        if (spriteBarraTopo == null)
            foreach (var a in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(
                "Assets/assets/UI/charselection/bar_charselect.ase"))
                if (a is Sprite s) { spriteBarraTopo = s; break; }
        if (spriteBotao == null)
            spriteBotao = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/assets/UI/charselection/btn_stone.png");
        if (spriteSlotPlayer == null)
            foreach (var a in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(
                "Assets/assets/UI/skill_card/slotplayer.ase"))
                if (a is Sprite s) { spriteSlotPlayer = s; break; }
#endif

        var canvasGO = GameObject.Find("CanvasPrincipal");
        if (canvasGO == null) yield break;

        CriarPainel(canvasGO);
    }

    void CriarPainel(GameObject canvas)
    {
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
        backdrop.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

        // Caixa central
        painelContent = new GameObject("painelmissao");
        painelContent.transform.SetParent(painelRaiz.transform, false);
        var rC = painelContent.AddComponent<RectTransform>();
        rC.anchorMin = new Vector2(0.08f, 0.05f);
        rC.anchorMax = new Vector2(0.92f, 0.95f);
        rC.offsetMin = rC.offsetMax = Vector2.zero;
        var imgContent = painelContent.AddComponent<Image>();
        if (spriteFundoPainel != null)
        {
            imgContent.sprite          = spriteFundoPainel;
            imgContent.type            = Image.Type.Simple;
            imgContent.color           = Color.white;
            imgContent.preserveAspect  = false;
        }
        else
        {
            imgContent.color = corFundo;
        }

        // Bordas douradas externas
        CriarBorda(painelContent, "BordaTopo", new Vector2(0f,1f), new Vector2(1f,1f), new Vector2(0f,-2f), Vector2.zero);
        CriarBorda(painelContent, "BordaBase", new Vector2(0f,0f), new Vector2(1f,0f), Vector2.zero, new Vector2(0f,2f));
        CriarBorda(painelContent, "BordaEsq",  new Vector2(0f,0f), new Vector2(0f,1f), Vector2.zero, new Vector2(2f,0f));
        CriarBorda(painelContent, "BordaDir",  new Vector2(1f,0f), new Vector2(1f,1f), new Vector2(-2f,0f), Vector2.zero);

        // Barra de topo escura (estilo CharacterSelection)
        var barraTopo = new GameObject("BarraTopo");
        barraTopo.transform.SetParent(painelContent.transform, false);
        var rBT = barraTopo.AddComponent<RectTransform>();
        rBT.anchorMin = new Vector2(0f, 0.92f);
        rBT.anchorMax = Vector2.one;
        rBT.offsetMin = rBT.offsetMax = Vector2.zero;
        var imgBarra = barraTopo.AddComponent<Image>();
        if (spriteBarraTopo != null)
        {
            imgBarra.sprite = spriteBarraTopo;
            imgBarra.type   = Image.Type.Simple;
            imgBarra.color  = new Color(0.50f, 0.40f, 0.40f, 1.0f);
        }
        else
        {
            imgBarra.color = corBarraTopo;
        }

        // Linha dourada separando barra do conteúdo
        CriarSeparador(painelContent, new Vector2(0f, 0.918f), new Vector2(1f, 0.922f));

        // Acento carmesim na borda esquerda da barra de topo
        var acento = new GameObject("AcentoTopo");
        acento.transform.SetParent(painelContent.transform, false);
        var rAc = acento.AddComponent<RectTransform>();
        rAc.anchorMin = new Vector2(0f, 0.92f);
        rAc.anchorMax = new Vector2(0.004f, 1f);
        rAc.offsetMin = rAc.offsetMax = Vector2.zero;
        acento.AddComponent<Image>().color = corAcento;

        // Título
        var titulo = CriarTexto(painelContent, "Titulo",
            new Vector2(0.03f, 0.92f), new Vector2(0.88f, 1f),
            "MISSOES DE DESBLOQUEIO", 17f, FontStyles.Bold, corTitulo);
        titulo.alignment = TextAlignmentOptions.MidlineLeft;

        // Botão fechar
        CriarBotaoFechar(painelContent);

        // Abas
        CriarAbas();

        // Separador abaixo das abas
        CriarSeparador(painelContent, new Vector2(0.01f, 0.835f), new Vector2(0.99f, 0.837f));

        // Área de conteúdo com scroll
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
        go.AddComponent<Image>().color = new Color(corBorda.r, corBorda.g, corBorda.b, 0.5f);
    }

    void CriarBotaoFechar(GameObject pai)
    {
        var go = new GameObject("BtnFechar");
        go.transform.SetParent(pai.transform, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(1f, 1f);
        r.anchoredPosition = new Vector2(-8f, -6f);
        r.sizeDelta = new Vector2(36f, 30f);

        // Borda dourada ao redor do botão
        var borda = new GameObject("Borda");
        borda.transform.SetParent(go.transform, false);
        var rb = borda.AddComponent<RectTransform>();
        rb.anchorMin = Vector2.zero; rb.anchorMax = Vector2.one;
        rb.offsetMin = new Vector2(-1f,-1f); rb.offsetMax = new Vector2(1f,1f);
        borda.AddComponent<Image>().color = new Color(corBorda.r, corBorda.g, corBorda.b, 0.7f);
        borda.transform.SetAsFirstSibling();

        var img = go.AddComponent<Image>();
        if (spriteBotao != null)
        {
            img.sprite = spriteBotao;
            img.type   = Image.Type.Simple;
            img.color  = new Color(0.85f, 0.35f, 0.30f, 0.95f); // tint vermelho escuro
        }
        else
        {
            img.color = new Color(0.28f, 0.06f, 0.06f);
        }

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
            float xMin = 0.02f + i * 0.32f;
            float xMax = xMin + 0.30f;
            int idx = i;

            var go = new GameObject($"Aba_{nomes[i]}");
            go.transform.SetParent(painelContent.transform, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(xMin, 0.840f);
            r.anchorMax = new Vector2(xMax, 0.920f);
            r.offsetMin = new Vector2(2f, 2f);
            r.offsetMax = new Vector2(-2f, 0f);

            var img = go.AddComponent<Image>();
            if (spriteBotao != null)
            {
                img.sprite = spriteBotao;
                img.type   = Image.Type.Simple;
                img.color  = new Color(0.50f, 0.40f, 0.55f, 0.80f); // pedra inativa
            }
            else
            {
                img.color = corAba;
            }

            // Borda superior da aba (aparece em destaque quando ativa)
            var bordaAba = new GameObject("BordaAba");
            bordaAba.transform.SetParent(go.transform, false);
            var rb = bordaAba.AddComponent<RectTransform>();
            rb.anchorMin = new Vector2(0f, 1f); rb.anchorMax = new Vector2(1f, 1f);
            rb.offsetMin = new Vector2(0f, -3f); rb.offsetMax = Vector2.zero;
            bordaAba.AddComponent<Image>().color = new Color(corBorda.r, corBorda.g, corBorda.b, 0.3f);

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
        mascara.AddComponent<Image>().color = Color.clear;
        mascara.AddComponent<Mask>().showMaskGraphic = false;

        areaConteudo = new GameObject("Conteudo");
        areaConteudo.transform.SetParent(mascara.transform, false);
        var rc = areaConteudo.AddComponent<RectTransform>();
        rc.anchorMin = new Vector2(0f, 1f);
        rc.anchorMax = Vector2.one;
        rc.pivot     = new Vector2(0.5f, 1f);
        rc.offsetMin = rc.offsetMax = Vector2.zero;

        var layout = areaConteudo.AddComponent<VerticalLayoutGroup>();
        layout.spacing                = 6f;
        layout.padding                = new RectOffset(6, 6, 8, 8);
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
            var imgAba = botoesAba[i].GetComponent<Image>();
            if (spriteBotao != null)
                imgAba.color = ativo
                    ? new Color(0.90f, 0.72f, 0.30f, 1.0f)     // dourado ativo
                    : new Color(0.50f, 0.40f, 0.55f, 0.80f);   // pedra inativa
            else
                imgAba.color = ativo ? corAbaAtiva : corAba;
            if (textosBotoesAba[i] != null)
                textosBotoesAba[i].color = ativo ? corAbaAtivaTxt : corAbaTexto;

            var bordaAba = botoesAba[i].transform.Find("BordaAba");
            if (bordaAba != null)
            {
                var imgBorda = bordaAba.GetComponent<Image>();
                if (imgBorda != null)
                    imgBorda.color = ativo
                        ? new Color(corBorda.r, corBorda.g, corBorda.b, 1f)
                        : new Color(corBorda.r, corBorda.g, corBorda.b, 0.3f);
            }
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
            string status = desbloqueado ? "DESBLOQUEADO" : $"Nivel {p.unlockLevel} necessario";
            CriarCard(p.characterName, status, p.missaoDesbloqueio,
                CharacterData.GetElementColor(p.baseElement), desbloqueado, p.icon);
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
                string status = desbloqueado ? "DESBLOQUEADO" : $"Nivel {u.requiredLevel} necessario";
                CriarCard(u.ultimateName, status,
                    $"[{p.characterName}]  {u.description}",
                    u.GetElementColor(), desbloqueado, u.ultimateIcon);
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
                    CharacterData.GetElementColor(p.baseElement), desbloqueado, passiva.passiveIcon);
            }
        }
        if (!alguma)
            CriarCardInfo("Passivas",
                "As passivas sao desbloqueadas ao subir de nivel e selecionar cartas de habilidade durante a partida.");
    }

    void CriarCard(string nome, string status, string descricao, Color corAccento, bool desbloqueado, Sprite icone = null)
    {
        var card = new GameObject($"Card_{nome}");
        card.transform.SetParent(areaConteudo.transform, false);

        var le = card.AddComponent<LayoutElement>();
        le.minHeight       = 100f;
        le.preferredHeight = 100f;

        card.AddComponent<Image>().color = corCard;

        // Borda superior fina (cor do acento)
        var bordaTopo = new GameObject("BordaTopo");
        bordaTopo.transform.SetParent(card.transform, false);
        var rbt = bordaTopo.AddComponent<RectTransform>();
        rbt.anchorMin = new Vector2(0f, 1f); rbt.anchorMax = new Vector2(1f, 1f);
        rbt.offsetMin = new Vector2(0f, -2f); rbt.offsetMax = Vector2.zero;
        bordaTopo.AddComponent<Image>().color = new Color(corAccento.r, corAccento.g, corAccento.b, 0.65f);

        // Borda esquerda grossa (cor do acento)
        var bordaEsq = new GameObject("BordaEsq");
        bordaEsq.transform.SetParent(card.transform, false);
        var rbe = bordaEsq.AddComponent<RectTransform>();
        rbe.anchorMin = Vector2.zero; rbe.anchorMax = new Vector2(0f, 1f);
        rbe.offsetMin = Vector2.zero; rbe.offsetMax = new Vector2(5f, 0f);
        bordaEsq.AddComponent<Image>().color = corAccento;

        // Barra inferior sutil
        var barraBase = new GameObject("BarraBase");
        barraBase.transform.SetParent(card.transform, false);
        var rBb = barraBase.AddComponent<RectTransform>();
        rBb.anchorMin = Vector2.zero; rBb.anchorMax = new Vector2(1f, 0f);
        rBb.offsetMin = Vector2.zero; rBb.offsetMax = new Vector2(0f, 2f);
        barraBase.AddComponent<Image>().color = new Color(corAccento.r, corAccento.g, corAccento.b, 0.2f);

        // Ícone (se disponível)
        bool temIcone = icone != null;
        if (temIcone)
        {
            // Slot de fundo do ícone
            if (spriteSlotPlayer != null)
            {
                var goSlot = new GameObject("Slot");
                goSlot.transform.SetParent(card.transform, false);
                var rSlot = goSlot.AddComponent<RectTransform>();
                rSlot.anchorMin = new Vector2(0.01f, 0.05f);
                rSlot.anchorMax = new Vector2(0.19f, 0.95f);
                rSlot.offsetMin = rSlot.offsetMax = Vector2.zero;
                var imgSlot = goSlot.AddComponent<Image>();
                imgSlot.sprite = spriteSlotPlayer;
                imgSlot.type   = Image.Type.Sliced;
                imgSlot.color  = Color.white;
                imgSlot.raycastTarget = false;
            }

            var goIcon = new GameObject("Icone");
            goIcon.transform.SetParent(card.transform, false);
            var rIcon = goIcon.AddComponent<RectTransform>();
            rIcon.anchorMin = new Vector2(0.02f, 0.08f);
            rIcon.anchorMax = new Vector2(0.18f, 0.92f);
            rIcon.offsetMin = rIcon.offsetMax = Vector2.zero;
            var imgIcon = goIcon.AddComponent<Image>();
            imgIcon.sprite         = icone;
            imgIcon.preserveAspect = true;
            imgIcon.color          = Color.white;
            imgIcon.raycastTarget  = false;
        }

        float textX = temIcone ? 0.22f : 0.07f;

        // Nome (bold, dourado claro)
        var txtNome = CriarTexto(card, "Nome",
            new Vector2(textX, 0.60f), new Vector2(0.97f, 0.96f),
            nome, 14f, FontStyles.Bold, corTexto);
        txtNome.alignment        = TextAlignmentOptions.MidlineLeft;
        txtNome.textWrappingMode = TMPro.TextWrappingModes.Normal;

        // Status (desbloqueado / bloqueado)
        var corSt = desbloqueado ? corDesbloqueado : corBloqueado;
        var txtStatus = CriarTexto(card, "Status",
            new Vector2(textX, 0.38f), new Vector2(0.97f, 0.60f),
            status, 9f, FontStyles.Bold, corSt);
        txtStatus.alignment = TextAlignmentOptions.MidlineLeft;

        // Descrição / missão de desbloqueio
        var txtDesc = CriarTexto(card, "Desc",
            new Vector2(textX, 0.02f), new Vector2(0.97f, 0.38f),
            descricao, 9f, FontStyles.Normal,
            new Color(0.60f, 0.55f, 0.45f));
        txtDesc.alignment        = TextAlignmentOptions.TopLeft;
        txtDesc.textWrappingMode = TMPro.TextWrappingModes.Normal;
        txtDesc.overflowMode     = TMPro.TextOverflowModes.Ellipsis;
    }

    void CriarCardInfo(string titulo, string texto)
    {
        var card = new GameObject("CardInfo");
        card.transform.SetParent(areaConteudo.transform, false);

        var le = card.AddComponent<LayoutElement>();
        le.minHeight       = 120f;
        le.preferredHeight = 120f;

        card.AddComponent<Image>().color = corCard;

        var bordaTopo = new GameObject("BordaTopo");
        bordaTopo.transform.SetParent(card.transform, false);
        var rbt = bordaTopo.AddComponent<RectTransform>();
        rbt.anchorMin = new Vector2(0f, 1f); rbt.anchorMax = new Vector2(1f, 1f);
        rbt.offsetMin = new Vector2(0f, -2f); rbt.offsetMax = Vector2.zero;
        bordaTopo.AddComponent<Image>().color = corBorda;

        var bordaEsq = new GameObject("BordaEsq");
        bordaEsq.transform.SetParent(card.transform, false);
        var rbe = bordaEsq.AddComponent<RectTransform>();
        rbe.anchorMin = Vector2.zero; rbe.anchorMax = new Vector2(0f, 1f);
        rbe.offsetMin = Vector2.zero; rbe.offsetMax = new Vector2(5f, 0f);
        bordaEsq.AddComponent<Image>().color = corBorda;

        var t = CriarTexto(card, "Txt",
            new Vector2(0.05f, 0.04f), new Vector2(0.96f, 0.96f),
            $"<b>{titulo}</b>\n\n{texto}", 11f, FontStyles.Normal, corTexto);
        t.textWrappingMode = TMPro.TextWrappingModes.Normal;
        t.alignment        = TextAlignmentOptions.TopLeft;
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
