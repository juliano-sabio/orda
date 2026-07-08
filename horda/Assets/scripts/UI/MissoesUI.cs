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
    public Sprite spriteSkillSlotCard;
    public Sprite spriteEspiritoMissao;

    // Paleta alinhada com CharacterSelectionUI
    static readonly Color corFundo        = new Color(0f, 0f, 0f, 0.90f);
    static readonly Color corBarraTopo    = new Color(0.10f, 0.04f, 0.04f);
    static readonly Color corBorda        = new Color(0.35f, 0.05f, 0.05f);
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

    CharacterSelectionUI charSelUI;

    IEnumerator Start()
    {
        yield return null;

        var charUI = FindAnyObjectByType<CharacterSelectionUI>();
        charSelUI = charUI;
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
        if (spriteSkillSlotCard == null)
            foreach (var a in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(
                "Assets/assets/UI/skill_card/skill_slot_card.aseprite"))
                if (a is Sprite s) { spriteSkillSlotCard = s; break; }
        if (spriteEspiritoMissao == null)
            foreach (var a in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(
                "Assets/assets/UI/espiritoMissao.ase"))
                if (a is Sprite s) { spriteEspiritoMissao = s; break; }
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
        backdrop.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.96f);

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
            imgContent.color           = corFundo;
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
            Loc.T("missions.title"), 17f, FontStyles.Bold, corTitulo);
        titulo.alignment = TextAlignmentOptions.Center;

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
        r.offsetMin = new Vector2(2f, 0f); r.offsetMax = new Vector2(-2f, 0f);
        go.AddComponent<Image>().color = new Color(corBorda.r, corBorda.g, corBorda.b, 0.5f);
    }

    void CriarBotaoFechar(GameObject pai)
    {
        var go = new GameObject("BtnFechar");
        go.transform.SetParent(pai.transform, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(1f, 1f);
        r.anchoredPosition = new Vector2(-6f, -5f);
        r.sizeDelta = new Vector2(30f, 26f);

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
        string[] nomes = { Loc.T("missions.tab.chars"), Loc.T("missions.tab.ultimates"), Loc.T("missions.tab.passives") };
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
        mascara.AddComponent<Image>().color = Color.white;
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
        int charIndex = PlayerPrefs.GetInt("SelectedCharacter", 0);

        // Lista as 11 missões de Espírito de Evolução (marcos de kills one-time).
        CriarCardsMissoesEspirito(charIndex);

        if (personagens == null) return;

        foreach (var p in personagens)
        {
            if (p == null) continue;
            bool desbloqueado = p.unlocked || playerLevel >= p.unlockLevel;
            string status = desbloqueado ? Loc.T("missions.done") : Loc.T("missions.incomplete");
            CriarCard(p.GetDisplayName(), status, p.GetDisplayMissao(),
                CharacterData.GetElementColor(p.baseElement), desbloqueado, p.icon);
        }
    }

    // Cria um card por missão de Espírito (todas as 11), mostrando o estado de cada uma.
    void CriarCardsMissoesEspirito(int charIndex)
    {
        var   marcos    = MissaoEspiritoManager.Marcos;
        int   coletadas = MissaoEspiritoManager.Coletadas;
        int   total     = MissaoEspiritoManager.TotalKills;
        Color cor       = new Color(0.30f, 0.85f, 1.00f);
        string tituloBase = Loc.T("missions.spirit_mission.title");

        for (int i = 0; i < marcos.Length; i++)
        {
            int marco   = marcos[i];
            string nome = $"{tituloBase} ({i + 1}/{marcos.Length})";
            string desc = $"Derrote {marco} inimigos no total para ganhar +1 Espirito de Evolucao.";

            if (i < coletadas)
            {
                // já coletada — one-time, não pode ser refeita
                CriarCard(nome, Loc.T("missions.done"), desc, cor, true, spriteEspiritoMissao);
            }
            else if (i == coletadas && total >= marco)
            {
                // completa e pronta pra coletar
                CriarCard(nome, Loc.T("missions.spirit_mission.collect"), desc, cor, true,
                    spriteEspiritoMissao, brilhando: true,
                    onClick: () => ColetarMissaoEspirito(charIndex));
            }
            else if (i == coletadas)
            {
                // missão atual, em progresso
                CriarCard(nome, $"{total}/{marco}", desc, cor, false, spriteEspiritoMissao);
            }
            else
            {
                // missões futuras (bloqueadas)
                CriarCard(nome, Loc.T("missions.incomplete"), desc, cor, false,
                    spriteEspiritoMissao, apagado: true);
            }
        }
    }


    void ColetarMissaoEspirito(int charIndex)
    {
        EspiritoUpgradeSystem.AdicionarEspirito(charIndex);
        // Avança o marco: a missão coletada NÃO pode ser refeita (one-time).
        PlayerPrefs.SetInt("MissaoEspiritoColetadas", MissaoEspiritoManager.Coletadas + 1);
        PlayerPrefs.Save();
        charSelUI?.AtualizarEspiritos();
        MostrarAba(abaAtiva);
    }

    void PopularUltimates(int playerLevel)
    {
        if (personagens == null) return;
        foreach (var p in personagens)
        {
            if (p == null || p.ultimatesDisponiveis == null) continue;
            var ults = p.ultimatesDisponiveis;
            foreach (int i in OrdenarUltimatesMis(ults))
            {
                var u = ults[i];
                if (u == null) continue;

                // Raio Certeiro é item base (sem missão) — não aparece na lista de missões
                if (NormalizarMis(u.GetDisplayName() + " " + u.ultimateName + " " + u.name).Contains("raio certeiro"))
                    continue;

                // Domo Retardante: missão especial — matar a Princesa Slime 2x pra desbloquear
                if (EhDomoMis(u))
                {
                    bool   desb    = MissaoDomoManager.DomoDesbloqueado;
                    string statusD = desb ? Loc.T("missions.done")
                                          : $"{MissaoDomoManager.KillsPrincesa}/{MissaoDomoManager.META}";
                    string descD   = $"[{p.GetDisplayName()}]  " + string.Format(Loc.T("mission.desc.domo"), MissaoDomoManager.META);
                    CriarCard(u.GetDisplayName(), statusD, descD, u.GetElementColor(), desb, u.ultimateIcon, brilhando: desb);
                    continue;
                }

                // Tempestade Elétrica: missão especial — concluir 3 eventos de Tempestade
                if (EhTempestadeMis(u))
                {
                    bool   desb    = MissaoTempestadeManager.Desbloqueada;
                    string statusD = desb ? Loc.T("missions.done")
                                          : $"{MissaoTempestadeManager.Completas}/{MissaoTempestadeManager.META}";
                    string descD   = $"[{p.GetDisplayName()}]  " + string.Format(Loc.T("mission.desc.tempestade"), MissaoTempestadeManager.META);
                    CriarCard(u.GetDisplayName(), statusD, descD, u.GetElementColor(), desb, u.ultimateIcon, brilhando: desb);
                    continue;
                }

                // Necrópole: missão especial — eliminar 500 fantasmas
                if (EhNecropoleMis(u))
                {
                    bool   desb    = MissaoNecropoleManager.Desbloqueada;
                    string statusD = desb ? Loc.T("missions.done")
                                          : $"{MissaoNecropoleManager.Kills}/{MissaoNecropoleManager.META}";
                    string descD   = $"[{p.GetDisplayName()}]  " + string.Format(Loc.T("mission.desc.necropole"), MissaoNecropoleManager.META);
                    CriarCard(u.GetDisplayName(), statusD, descD, u.GetElementColor(), desb, u.ultimateIcon, brilhando: desb);
                    continue;
                }

                // Drenagem de Vida: missão especial — eliminar 500 slimes curandeiras
                if (EhDrenagemMis(u))
                {
                    bool   desb    = MissaoDrenagemManager.Desbloqueada;
                    string statusD = desb ? Loc.T("missions.done")
                                          : $"{MissaoDrenagemManager.Kills}/{MissaoDrenagemManager.META}";
                    string descD   = $"[{p.GetDisplayName()}]  " + string.Format(Loc.T("mission.desc.drenagem"), MissaoDrenagemManager.META);
                    CriarCard(u.GetDisplayName(), statusD, descD, u.GetElementColor(), desb, u.ultimateIcon, brilhando: desb);
                    continue;
                }

                if (UltimateLiberadaMis(u))
                {
                    bool desbloqueado = u.isUnlocked && playerLevel >= u.requiredLevel;
                    string status = desbloqueado ? Loc.T("missions.done") : Loc.T("missions.incomplete");
                    CriarCard(u.GetDisplayName(), status,
                        $"[{p.GetDisplayName()}]  {u.GetDisplayDescription()}",
                        u.GetElementColor(), desbloqueado, u.ultimateIcon);
                }
                else
                {
                    CriarCard(u.GetDisplayName(), Loc.T("terrain.unavailable"),
                        $"[{p.GetDisplayName()}]  {u.GetDisplayDescription()}",
                        u.GetElementColor(), false, u.ultimateIcon, apagado: true);
                }
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
            for (int i = 0; i < p.passivasDisponiveis.Length; i++)
            {
                var passiva = p.passivasDisponiveis[i];
                if (passiva == null) continue;

                // Colheita é item base (sem missão) — não aparece na lista de missões
                if (NormalizarMis(passiva.GetDisplayName() + " " + passiva.name).Contains("colheita"))
                    continue;

                alguma = true;
                string bonus  = passiva.GetBonusDescription();
                string desc   = string.IsNullOrEmpty(bonus) ? passiva.GetDisplayDescription() : bonus;

                // Coração Robusto: missão especial — eliminar 150 inimigos
                if (EhCoracaoMis(passiva))
                {
                    bool   desb    = MissaoCoracaoManager.Desbloqueada;
                    string statusD = desb ? Loc.T("missions.done")
                                          : $"{MissaoCoracaoManager.Kills}/{MissaoCoracaoManager.META}";
                    string descD   = $"[{p.GetDisplayName()}]  " + string.Format(Loc.T("mission.desc.coracao"), MissaoCoracaoManager.META);
                    CriarCard(passiva.GetDisplayName(), statusD, descD,
                        CharacterData.GetElementColor(p.baseElement), desb, passiva.passiveIcon, brilhando: desb);
                    continue;
                }

                // Caçador: missão especial — eliminar 200 slimes corrompidas
                if (EhCacadorMis(passiva))
                {
                    bool   desb    = MissaoCacadorManager.Desbloqueada;
                    string statusD = desb ? Loc.T("missions.done")
                                          : $"{MissaoCacadorManager.Kills}/{MissaoCacadorManager.META}";
                    string descD   = $"[{p.GetDisplayName()}]  " + string.Format(Loc.T("mission.desc.cacador"), MissaoCacadorManager.META);
                    CriarCard(passiva.GetDisplayName(), statusD, descD,
                        CharacterData.GetElementColor(p.baseElement), desb, passiva.passiveIcon, brilhando: desb);
                    continue;
                }

                // Asceta: missão especial (binária) — concluir a primeira área
                if (EhAscetaMis(passiva))
                {
                    bool   desb    = MissaoAscetaManager.Desbloqueada;
                    string statusD = desb ? Loc.T("missions.done") : "0/1";
                    string descD   = $"[{p.GetDisplayName()}]  " + Loc.T("mission.desc.asceta");
                    CriarCard(passiva.GetDisplayName(), statusD, descD,
                        CharacterData.GetElementColor(p.baseElement), desb, passiva.passiveIcon, brilhando: desb);
                    continue;
                }

                if (i < PASSIVAS_LIBERADAS_MIS)
                {
                    bool desbloqueado = passiva.isUnlocked && playerLevel >= passiva.requiredLevel;
                    string status = desbloqueado ? Loc.T("missions.done") : Loc.T("missions.incomplete");
                    CriarCard(passiva.GetDisplayName(), status, $"[{p.GetDisplayName()}]  {desc}",
                        CharacterData.GetElementColor(p.baseElement), desbloqueado, passiva.passiveIcon);
                }
                else
                {
                    CriarCard(passiva.GetDisplayName(), Loc.T("terrain.unavailable"), $"[{p.GetDisplayName()}]  {desc}",
                        CharacterData.GetElementColor(p.baseElement), false, passiva.passiveIcon, apagado: true);
                }
            }
        }
        if (!alguma)
            CriarCardInfo(Loc.T("missions.tab.passives"), Loc.T("ui.no_passives"));
    }

    // ── Regras de liberação (iguais ao lobby/seleção) ─────────────────
    static readonly string[] ultimatesLiberadasMis =
        { "raio certeiro", "domo retardante", "tempestade", "necropole", "drenagem" };
    const int PASSIVAS_LIBERADAS_MIS = 4;

    bool UltimateLiberadaMis(UltimateData u)
    {
        if (u == null) return false;
        string nome = NormalizarMis(u.GetDisplayName() + " " + u.ultimateName + " " + u.name);
        foreach (var kw in ultimatesLiberadasMis) if (nome.Contains(kw)) return true;
        return false;
    }

    // Domo Retardante tem card de missão próprio (desbloqueio por matar a Princesa Slime 2x)
    bool EhDomoMis(UltimateData u)
    {
        if (u == null) return false;
        return NormalizarMis(u.GetDisplayName() + " " + u.ultimateName + " " + u.name).Contains("domo retardante");
    }

    // Tempestade Elétrica tem card de missão próprio (desbloqueio por concluir 3 eventos de Tempestade)
    bool EhTempestadeMis(UltimateData u)
    {
        if (u == null) return false;
        return NormalizarMis(u.GetDisplayName() + " " + u.ultimateName + " " + u.name).Contains("tempestade");
    }

    // Necrópole tem card de missão próprio (desbloqueio por eliminar 500 fantasmas)
    bool EhNecropoleMis(UltimateData u)
    {
        if (u == null) return false;
        return NormalizarMis(u.GetDisplayName() + " " + u.ultimateName + " " + u.name).Contains("necropole");
    }

    // Drenagem de Vida tem card de missão próprio (desbloqueio por eliminar 500 slimes curandeiras)
    bool EhDrenagemMis(UltimateData u)
    {
        if (u == null) return false;
        return NormalizarMis(u.GetDisplayName() + " " + u.ultimateName + " " + u.name).Contains("drenagem");
    }

    // Coração Robusto (passiva) tem card de missão próprio (desbloqueio por eliminar 150 inimigos)
    bool EhCoracaoMis(PassiveData pd)
    {
        if (pd == null) return false;
        return NormalizarMis(pd.GetDisplayName() + " " + pd.name).Contains("robusto");
    }

    // Caçador (passiva) tem card de missão próprio (desbloqueio por eliminar 200 slimes corrompidas)
    bool EhCacadorMis(PassiveData pd)
    {
        if (pd == null) return false;
        return NormalizarMis(pd.GetDisplayName() + " " + pd.name).Contains("cacador");
    }

    // Asceta (passiva) tem card de missão próprio (desbloqueio por concluir a primeira área)
    bool EhAscetaMis(PassiveData pd)
    {
        if (pd == null) return false;
        return NormalizarMis(pd.GetDisplayName() + " " + pd.name).Contains("asceta");
    }

    // ordem: liberadas primeiro (sequência fixa), depois o resto (pseudo-aleatório estável)
    System.Collections.Generic.List<int> OrdenarUltimatesMis(UltimateData[] ults)
    {
        var restante = new System.Collections.Generic.List<int>();
        for (int i = 0; i < ults.Length; i++) restante.Add(i);

        var ordem = new System.Collections.Generic.List<int>();
        foreach (var kw in ultimatesLiberadasMis)
        {
            for (int j = 0; j < restante.Count; j++)
            {
                int idx = restante[j];
                var u = ults[idx];
                string nome = u != null ? NormalizarMis(u.GetDisplayName() + " " + u.ultimateName + " " + u.name) : "";
                if (nome.Contains(kw)) { ordem.Add(idx); restante.RemoveAt(j); break; }
            }
        }

        var rng = new System.Random(7321);
        for (int i = restante.Count - 1; i > 0; i--)
        {
            int k = rng.Next(i + 1);
            int tmp = restante[i]; restante[i] = restante[k]; restante[k] = tmp;
        }
        ordem.AddRange(restante);
        return ordem;
    }

    static string NormalizarMis(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.ToLowerInvariant();
        var sb = new System.Text.StringBuilder();
        foreach (char c in s.Normalize(System.Text.NormalizationForm.FormD))
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString();
    }

    void CriarCard(string nome, string status, string descricao, Color corAccento, bool desbloqueado, Sprite icone = null, bool brilhando = false, bool apagado = false, System.Action onClick = null)
    {
        var card = new GameObject($"Card_{nome}");
        card.transform.SetParent(areaConteudo.transform, false);

        var le = card.AddComponent<LayoutElement>();
        le.minHeight       = 100f;
        le.preferredHeight = 100f;

        var imgFundo = card.AddComponent<Image>();
        imgFundo.color = corCard;

        if (apagado)
            card.AddComponent<CanvasGroup>().alpha = 0.45f;

        if (onClick != null)
        {
            var btn = card.AddComponent<Button>();
            btn.targetGraphic = imgFundo;
            var colors = btn.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1.1f);
            colors.pressedColor     = new Color(0.85f, 0.85f, 0.85f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick());
        }

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
            // Slot de fundo do ícone (quadrado, ancorado na borda esquerda do card)
            if (spriteSkillSlotCard != null)
            {
                var goSlot = new GameObject("Slot");
                goSlot.transform.SetParent(card.transform, false);
                var rSlot = goSlot.AddComponent<RectTransform>();
                rSlot.anchorMin = new Vector2(0f, 0f);
                rSlot.anchorMax = new Vector2(0f, 1f);
                rSlot.offsetMin = new Vector2(5f, 5f);
                rSlot.offsetMax = new Vector2(95f, -5f);
                var imgSlot = goSlot.AddComponent<Image>();
                imgSlot.sprite = spriteSkillSlotCard;
                imgSlot.type   = Image.Type.Simple;
                imgSlot.color  = Color.white;
                imgSlot.raycastTarget = false;
            }

            var goIcon = new GameObject("Icone");
            goIcon.transform.SetParent(card.transform, false);
            var rIcon = goIcon.AddComponent<RectTransform>();
            rIcon.anchorMin = new Vector2(0f, 0f);
            rIcon.anchorMax = new Vector2(0f, 1f);
            rIcon.offsetMin = new Vector2(13f, 13f);
            rIcon.offsetMax = new Vector2(87f, -13f);
            var imgIcon = goIcon.AddComponent<Image>();
            imgIcon.sprite         = icone;
            imgIcon.preserveAspect = true;
            imgIcon.color          = Color.white;
            imgIcon.raycastTarget  = false;
        }

        float textX = temIcone ? 0.12f : 0.07f;

        // Descrição / missão de desbloqueio (título centralizado no topo)
        var txtDesc = CriarTexto(card, "Desc",
            new Vector2(textX, 0.36f), new Vector2(0.97f, 0.66f),
            descricao, 9f, FontStyles.Normal,
            new Color(0.60f, 0.55f, 0.45f));
        txtDesc.alignment        = TextAlignmentOptions.Center;
        txtDesc.textWrappingMode = TMPro.TextWrappingModes.Normal;
        txtDesc.overflowMode     = TMPro.TextOverflowModes.Ellipsis;

        // Nome (bold, dourado claro)
        var txtNome = CriarTexto(card, "Nome",
            new Vector2(textX, 0.36f), new Vector2(0.97f, 0.66f),
            nome, 14f, FontStyles.Bold, corTexto);
        txtNome.alignment        = TextAlignmentOptions.MidlineLeft;
        txtNome.textWrappingMode = TMPro.TextWrappingModes.Normal;

        // Status (desbloqueado / bloqueado)
        var corSt = desbloqueado ? corDesbloqueado : corBloqueado;
        var txtStatus = CriarTexto(card, "Status",
            new Vector2(0.78f, 0.36f), new Vector2(0.97f, 0.66f),
            status, 9f, FontStyles.Bold, corSt);
        txtStatus.alignment = TextAlignmentOptions.MidlineRight;

        // Brilho de missão concluída
        if (brilhando)
        {
            var glow = new GameObject("Glow");
            glow.transform.SetParent(card.transform, false);
            var rGlow = glow.AddComponent<RectTransform>();
            rGlow.anchorMin = Vector2.zero;
            rGlow.anchorMax = Vector2.one;
            rGlow.offsetMin = rGlow.offsetMax = Vector2.zero;
            var imgGlow = glow.AddComponent<Image>();
            imgGlow.color = new Color(corAccento.r, corAccento.g, corAccento.b, 0f);
            imgGlow.raycastTarget = false;
            glow.AddComponent<CardGlowEffect>().Setup(imgGlow, corAccento);
        }
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

// Pulso de brilho usado no card de missão recém-concluída
public class CardGlowEffect : MonoBehaviour
{
    Image img;
    Color corBase;
    const float velocidade = 2.5f;

    public void Setup(Image imagem, Color cor)
    {
        img = imagem;
        corBase = cor;
    }

    void Update()
    {
        if (img == null) return;
        float t = (Mathf.Sin(Time.unscaledTime * velocidade) + 1f) * 0.5f;
        float a = Mathf.Lerp(0.05f, 0.45f, t);
        img.color = new Color(corBase.r, corBase.g, corBase.b, a);
    }
}
